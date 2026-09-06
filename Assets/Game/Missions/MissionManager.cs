using UnityEngine;
using UnityEngine.SceneManagement;

namespace PacificCombat
{
    public enum MissionState { Briefing, Flying, Paused, Victory, Defeat }

    public sealed class MissionManager : MonoBehaviour
    {
        public MissionDefinition Definition;
        public Material[] IncludedShaderMaterials;
        public AircraftController Player { get; private set; }
        public AircraftController[] Enemies { get; private set; }
        public AircraftWeaponSystem Weapons { get; private set; }
        public AircraftDamage Damage { get; private set; }
        public AircraftInput Input { get; private set; }
        public CameraController FlightCamera { get; private set; }
        public MissionState State { get; private set; } = MissionState.Briefing;
        public AircraftController SelectedTarget { get; private set; }
        public int Remaining { get; private set; }
        public int Kills { get; private set; }
        public float MissionTime { get; private set; }
        public bool LeadIndicator;
        public bool ShowDebug;
        public bool ShowHUD = true;
        public FighterDifficulty Difficulty = FighterDifficulty.Regular;
        int targetIndex = -1;
        Transform world;
        bool ownsDefinition;
        static bool restartIntoFlight;

        void Start()
        {
            Time.fixedDeltaTime = .02f;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            bool automatedSettings = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
            GameSettings.BeginSession(automatedSettings);
            GameSettings.ApplyToMission(this);
            if (!Definition)
            {
                ownsDefinition = true;
                Definition = ScriptableObject.CreateInstance<MissionDefinition>();
                Definition.PlayerAircraft = AircraftData.CreateMustang();
                Definition.EnemyAircraft = AircraftData.CreateZero();
            }
            world = new GameObject("Pacific world").transform;
            world.gameObject.AddComponent<PacificEnvironment>().Build();
            Player = AircraftFactory.Create(Definition.PlayerAircraft, true, 0, new Vector3(0, Definition.StartingAltitude, 0), Quaternion.identity, Definition.StartingSpeed, Definition.PlayerWeapons);
            Weapons = Player.GetComponent<AircraftWeaponSystem>();
            Damage = Player.GetComponent<AircraftDamage>();
            Input = Player.GetComponent<AircraftInput>();
            Damage.Destroyed += OnPlayerDestroyed;
            int enemyCount = Definition.EnemyCount;
            foreach (string argument in System.Environment.GetCommandLineArgs())
                if (argument.StartsWith("--enemies=") && int.TryParse(argument.Substring(10), out int requested)) enemyCount = Mathf.Clamp(requested, 1, 16);
            Enemies = new AircraftController[enemyCount];
            Remaining = Enemies.Length;
            for (int i = 0; i < Enemies.Length; i++)
            {
                Enemies[i] = AircraftFactory.Create(Definition.EnemyAircraft, false, 1, new Vector3((i % 4 - 1.5f) * 180, Definition.StartingAltitude + 100 + i * 35, Definition.EnemyRange + (i / 4) * 250 + Mathf.Abs(i % 4 - 1.5f) * 80), Quaternion.Euler(0, 180, 0), 105, Definition.EnemyWeapons);
                Enemies[i].name = "Zero " + (i + 1);
                Enemies[i].GetComponent<AircraftDamage>().Destroyed += OnEnemyDestroyed;
                var ai = Enemies[i].gameObject.AddComponent<FighterAIController>();
                ai.Difficulty = Difficulty;
                ai.Initialize(Enemies[i], Player, i);
            }
            var cameraObject = new GameObject("Flight camera");
            GameSettings.ApplyToMission(this);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 65000;
            camera.backgroundColor = new Color(.43f, .65f, .79f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            FlightCamera = cameraObject.AddComponent<CameraController>();
            FlightCamera.Initialize(this);
            gameObject.AddComponent<FlightHUD>().Initialize(this);
            var origin = gameObject.AddComponent<FloatingOriginSystem>();
            origin.Initialize(this, world);
            CycleTarget();
            Time.timeScale = 0;
            if (restartIntoFlight) { restartIntoFlight = false; BeginMission(); }
            // Automation runs the same scene and components as the player build.
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
                if (arguments[i] == "--smoke-test") { BeginMission(); gameObject.AddComponent<RuntimeSmokeTest>().Initialize(this); }
        }

        void Update()
        {
            if (!Input) return;
            if (Input.TargetPressed) CycleTarget();
            if (Input.RestartPressed && !Input.ShowBindings) Restart();
            if (Input.PausePressed && !Input.ShowBindings)
            {
                if (State == MissionState.Flying) Pause();
                else if (State == MissionState.Paused) Resume();
                else if (State == MissionState.Briefing) BeginMission();
            }
            if (State == MissionState.Flying) MissionTime += Time.deltaTime;
            if (SelectedTarget && SelectedTarget.IsDestroyed) CycleTarget();
            if (UnityEngine.InputSystem.Keyboard.current?.f3Key.wasPressedThisFrame == true) ShowDebug = !ShowDebug;
        }

        public void BeginMission() { State = MissionState.Flying; Time.timeScale = 1; }
        public void Pause() { State = MissionState.Paused; Time.timeScale = 0; }
        public void Resume() { Input.ShowBindings = false; State = MissionState.Flying; Time.timeScale = 1; }
        public void Restart() { restartIntoFlight = true; ReloadScene(); }
        public void MainMenu() { restartIntoFlight = false; ReloadScene(); }
        void ReloadScene() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void Quit()
        {
            Time.timeScale = 1;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        public void SetDifficulty(FighterDifficulty difficulty)
        {
            Difficulty = difficulty;
            for (int i = 0; i < Enemies.Length; i++) Enemies[i].GetComponent<FighterAIController>().Difficulty = difficulty;
        }
        public void CycleTarget()
        {
            SelectedTarget = null;
            if (Enemies == null) return;
            for (int i = 0; i < Enemies.Length; i++)
            {
                targetIndex = (targetIndex + 1) % Enemies.Length;
                if (Enemies[targetIndex] && !Enemies[targetIndex].IsDestroyed) { SelectedTarget = Enemies[targetIndex]; return; }
            }
        }
        void OnEnemyDestroyed(AircraftDamage enemy)
        {
            Remaining = Mathf.Max(0, Remaining - 1); Kills++;
            if (Remaining == 0 && State == MissionState.Flying) { State = MissionState.Victory; Time.timeScale = .15f; }
        }
        void OnPlayerDestroyed(AircraftDamage player)
        {
            State = MissionState.Defeat; Time.timeScale = .15f;
        }
        void OnDestroy()
        {
            Time.timeScale = 1;
            if (ownsDefinition && Definition) { Destroy(Definition.PlayerAircraft); Destroy(Definition.EnemyAircraft); Destroy(Definition); }
        }
    }
}
