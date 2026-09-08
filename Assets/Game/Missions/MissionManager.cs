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
        public AircraftCatalog Catalog => Definition ? Definition.Catalog : null;
        public AircraftType PlayerAircraftType => Definition.PlayerAircraft.Type;
        public AircraftType EnemyAircraftType => Definition.EnemyAircraft.Type;
        int targetIndex = -1;
        Transform world;
        bool ownsDefinition;
        static bool restartIntoFlight;
        static bool launchSelectionApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMissionSession() { restartIntoFlight = false; launchSelectionApplied = false; }

        void Start()
        {
            Time.fixedDeltaTime = .02f;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            bool selectionSmoke = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--selection-smoke") >= 0;
            bool worldSmoke = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--world-smoke") >= 0;
            bool automatedSettings = worldSmoke || selectionSmoke || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
            GameSettings.BeginSession(automatedSettings);
            if (worldSmoke) WorldRuntimeSmoke.ConfigureSettings();
            GameSettings.ApplyToMission(this);
            if (!launchSelectionApplied)
            {
                if (selectionSmoke)
                {
                    // Dedicated UI-path regression starts from a known matchup;
                    // other aircraft CLI options belong to the ordinary smoke harness.
                    GameSettings.Current.PlayerAircraft = AircraftType.P51D;
                    GameSettings.Current.EnemyAircraft = AircraftType.A6MZero;
                }
                else AircraftCatalog.ApplyCommandLine(GameSettings.Current, System.Environment.GetCommandLineArgs());
                launchSelectionApplied = true;
            }
            // Aircraft choices belong to this mission instance, not the authored asset.
            Definition = Definition ? Instantiate(Definition) : ScriptableObject.CreateInstance<MissionDefinition>();
            ownsDefinition = true;
            var catalog = Definition.Catalog ? Definition.Catalog : Resources.Load<AircraftCatalog>(AircraftCatalog.ResourcePath);
            Definition.ConfigureAircraft(catalog, GameSettings.Current.PlayerAircraft, GameSettings.Current.EnemyAircraft);
            ScenarioCatalog.Apply(Definition, GameSettings.Current.Mission);
            Definition.Weather = GameSettings.Current.Weather;
            world = new GameObject("Pacific world").transform;
            world.gameObject.AddComponent<PacificEnvironment>().Build(Definition.Weather);
            Vector3 playerStart = new Vector3(0, Definition.StartingAltitude, Definition.Scenario == MissionPreset.LandingPractice ? -6500 : 0);
            Player = AircraftFactory.Create(Definition.PlayerAircraft, true, 0, playerStart, Quaternion.identity, Definition.StartingSpeed, Definition.PlayerWeapons);
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
                Enemies[i].name = Catalog.Get(EnemyAircraftType).ShortName + " " + (i + 1);
                if (Definition.Scenario == MissionPreset.Intercept)
                {
                    Enemies[i].Body.position = new Vector3(Definition.EnemyRange + i * 160, Definition.StartingAltitude + 700, 600 + i * 180);
                    Enemies[i].Body.rotation = Quaternion.Euler(0, -90, 0);
                    Enemies[i].Body.linearVelocity = Vector3.left * 110;
                }
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
            camera.clearFlags = CameraClearFlags.Skybox;
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
            if (worldSmoke) gameObject.AddComponent<WorldRuntimeSmoke>().Initialize(this);
            else if (selectionSmoke) SelectionRuntimeSmokeTest.InitializeMission(this);
            else for (int i = 0; i < arguments.Length; i++)
                if (arguments[i] == "--smoke-test") gameObject.AddComponent<RuntimeSmokeTest>().Initialize(this);
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
            if (State == MissionState.Flying && Definition.Scenario == MissionPreset.LandingPractice && MissionTime > 3
                && Player.Controls.Gear && Player.Body.linearVelocity.magnitude < 2 && !Player.IsDestroyed)
            {
                Vector3 relative = world.InverseTransformPoint(Player.Body.position);
                if (Mathf.Abs(relative.x) < 32 && Mathf.Abs(relative.z + 3500) < 1200 && relative.y > 8 && relative.y < 13)
                { State = MissionState.Victory; Time.timeScale = .15f; }
            }
            if (SelectedTarget && SelectedTarget.IsDestroyed) CycleTarget();
            if (UnityEngine.InputSystem.Keyboard.current?.f3Key.wasPressedThisFrame == true) ShowDebug = !ShowDebug;
        }

        public void BeginMission() { State = MissionState.Flying; Time.timeScale = 1; }
        public void Pause() { State = MissionState.Paused; Time.timeScale = 0; }
        public void Resume() { Input.ShowBindings = false; State = MissionState.Flying; Time.timeScale = 1; }
        public void Restart() { restartIntoFlight = true; ReloadScene(); }
        public void MainMenu() { restartIntoFlight = false; ReloadScene(); }
        public void SelectScenario(MissionPreset preset)
        {
            if (State != MissionState.Briefing || !System.Enum.IsDefined(typeof(MissionPreset), preset)) return;
            GameSettings.Current.Mission = preset; GameSettings.SaveCurrent(); MainMenu();
        }
        public void SelectWeather(WeatherPreset preset)
        {
            if (State != MissionState.Briefing || !System.Enum.IsDefined(typeof(WeatherPreset), preset)) return;
            GameSettings.Current.Weather = preset; GameSettings.SaveCurrent(); MainMenu();
        }
        public void SelectAircraft(AircraftType type, bool enemy)
        {
            if (State != MissionState.Briefing || !AircraftCatalog.IsValidType(type)) return;
            Catalog.Get(type); // Reject an incomplete roster before changing saved preferences.
            if (enemy ? type == EnemyAircraftType : type == PlayerAircraftType) return;
            if (enemy) GameSettings.Current.EnemyAircraft = type; else GameSettings.Current.PlayerAircraft = type;
            GameSettings.SaveCurrent();
            restartIntoFlight = false;
            ReloadScene();
        }
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
            if (ownsDefinition && Definition) Destroy(Definition);
        }
    }
}
