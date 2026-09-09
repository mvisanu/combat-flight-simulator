using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Profiling;

namespace PacificCombat
{
    // Opt-in only via --smoke-test. Runs the real player scene, never enabled for ordinary play.
    public sealed class RuntimeSmokeTest : MonoBehaviour
    {
        static bool restarting;
        static AircraftType expectedPlayer, expectedEnemy;
        MissionManager mission;
        string output;
        bool failed;
        float requestedDuration = 35;
        bool soak;
        public void Initialize(MissionManager owner)
        {
            mission = owner;
            soak = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--soak-smoke") >= 0;
            foreach (string argument in System.Environment.GetCommandLineArgs())
                if (argument.StartsWith("--smoke-duration=") && float.TryParse(argument.Substring(17), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float duration)) requestedDuration = Mathf.Clamp(duration, 15, 600);
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SmokeTest" + mission.Enemies.Length));
            foreach (string argument in System.Environment.GetCommandLineArgs())
                if (argument.StartsWith("--smoke-label="))
                {
                    string label = argument.Substring(14);
                    if (System.Text.RegularExpressions.Regex.IsMatch(label, "^[a-z0-9-]{1,40}$")) output += "-" + label;
                }
            Directory.CreateDirectory(output);
            if (!restarting) File.WriteAllText(Path.Combine(output, "runtime-errors.txt"), "");
            Application.logMessageReceived += OnLog;
            StartCoroutine(Run());
        }
        void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            { failed = true; File.AppendAllText(Path.Combine(output, "runtime-errors.txt"), message + "\n" + trace + "\n"); }
        }
        void Require(bool condition, string message)
        {
            File.AppendAllText(Path.Combine(output, "results.txt"), (condition ? "PASS " : "FAIL ") + message + "\n");
            if (!condition) failed = true;
        }
        IEnumerator Run()
        {
            yield return null;
            if (restarting)
            {
                Require(mission.State == MissionState.Flying && mission.Remaining == mission.Enemies.Length && !mission.Player.IsDestroyed, "Scene restart creates fresh mission");
                Require(mission.PlayerAircraftType == expectedPlayer && mission.EnemyAircraftType == expectedEnemy,
                    "Restart retains both selected aircraft types");
                Require(mission.Weapons.ShotsFired == 0, "Restart resets ammunition and shot count");
                Require(mission.Player.Engine.Fuel.FuelFraction > .999f, "Restart fills fuel supply");
                mission.Damage.ApplyDamage(DamageZoneType.FuelTank, 70, mission.Player.Body.position);
                mission.Player.Engine.Fuel.Initialize(mission.Player, .000001f);
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Require(!mission.Player.Engine.Fuel.HasFuel && mission.Player.Engine.Thrust == 0, "Damaged tank drains and starves engine in live scene");
                yield return new WaitForSecondsRealtime(.15f);
                yield return Capture("fuel-empty.png");
                for (int i = 0; i < mission.Enemies.Length; i++) mission.Enemies[i].GetComponent<AircraftDamage>().DestroyAircraft();
                Require(mission.State == MissionState.Victory && mission.Remaining == 0, "All enemies destroyed triggers victory");
                mission.Damage.DestroyAircraft();
                Require(mission.State == MissionState.Defeat, "Player destroyed triggers defeat");
                File.AppendAllText(Path.Combine(output, "results.txt"), failed ? "SMOKE FAILED\n" : "SMOKE PASSED\n");
                Application.Quit(failed ? 1 : 0); yield break;
            }
            File.WriteAllText(Path.Combine(output, "results.txt"), "Pacific Fighter Sweep runtime smoke test\n");
            if (mission.State == MissionState.Briefing)
            {
                yield return Capture("briefing.png");
                mission.BeginMission();
            }
            Require(mission.Player && mission.Remaining == mission.Enemies.Length, "Aircraft and mission initialized");
            var requestedAircraft = new GameSettingsData();
            AircraftCatalog.ApplyCommandLine(requestedAircraft, System.Environment.GetCommandLineArgs());
            expectedPlayer = requestedAircraft.PlayerAircraft; expectedEnemy = requestedAircraft.EnemyAircraft;
            Require(mission.PlayerAircraftType == expectedPlayer && mission.EnemyAircraftType == expectedEnemy,
                "Requested player and enemy aircraft spawned");
            File.AppendAllText(Path.Combine(output, "results.txt"), $"AIRCRAFT {mission.Player.Data.AircraftName} versus {mission.Definition.EnemyAircraft.AircraftName}\n");
            Require(mission.Player.GetComponent<AircraftVisuals>().DetailedAirframe != null, "Blender aircraft art loaded");
            Require(mission.Player.GetComponent<AircraftVisuals>().Cockpit != null, "Functional cockpit instruments initialized");
            Require(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null, "URP active");
            string savedPreferences = PlayerPrefs.GetString(GameSettings.PreferenceKey, "");
            bool hadPreferences = PlayerPrefs.HasKey(GameSettings.PreferenceKey);
            GameSettings.Current.UnlimitedAmmo = true;
            GameSettings.SaveCurrent();
            Require(!GameSettings.PersistenceEnabled && PlayerPrefs.HasKey(GameSettings.PreferenceKey) == hadPreferences
                && PlayerPrefs.GetString(GameSettings.PreferenceKey, "") == savedPreferences, "Automation cannot overwrite saved user settings");
            GameSettings.Current.UnlimitedAmmo = false;
            GameSettings.ApplyToMission(mission);
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--radar-smoke") >= 0)
                yield return RadarRuntimeSmoke.Run(mission, Capture, Require);
            mission.Pause(); yield return new WaitForSecondsRealtime(.1f);
            Require(Time.timeScale == 0 && mission.State == MissionState.Paused, "Pause freezes simulation");
            yield return Capture("pause.png");
            var hud = mission.GetComponent<FlightHUD>();
            hud.SetSettingsVisible(true);
            yield return Capture("settings.png");
            mission.Input.ShowBindings = true;
            yield return Capture("controls.png");
            mission.Input.ShowBindings = false;
            hud.SetSettingsVisible(false);
            // Fixed viewpoints make authored geometry, cockpit and shoreline regressions reviewable.
            mission.FlightCamera.enabled = false; hud.enabled = false;
            var artCamera = mission.FlightCamera.Camera;
            artCamera.fieldOfView = 46;
            artCamera.transform.position = mission.Player.transform.TransformPoint(new Vector3(11, 6, 13));
            artCamera.transform.LookAt(mission.Player.transform.position + Vector3.up * .15f);
            yield return Capture("aircraft-detail.png");
            artCamera.transform.position = mission.Enemies[0].transform.TransformPoint(new Vector3(10, 4, 12));
            artCamera.transform.LookAt(mission.Enemies[0].transform.position);
            yield return Capture("enemy-detail.png");
            var island = GameObject.Find("Island 1");
            if (island)
            {
                artCamera.transform.position = island.transform.position + new Vector3(3300, 2100, -3600);
                artCamera.transform.LookAt(island.transform.position + Vector3.up * 250);
                yield return Capture("terrain-detail.png");
            }
            mission.FlightCamera.enabled = true; hud.enabled = true;
            mission.FlightCamera.Snap();
            yield return new WaitForSecondsRealtime(.3f);
            mission.Resume();
            mission.Input.enabled = false;
            mission.FlightCamera.SetMode(FlightCameraMode.Cockpit);
            yield return new WaitForSecondsRealtime(.2f);
            var instruments = mission.Player.GetComponent<CockpitInstruments>();
            Require(instruments.IsVisible && Mathf.Abs(instruments.IndicatedFuel - mission.Player.Engine.Fuel.FuelFraction * 100) < .1f
                && Mathf.Abs(instruments.IndicatedRPM - mission.Player.Engine.RPM) < 50
                && Mathf.Abs(instruments.IndicatedAltitude - mission.Player.Physics.Altitude * 3.28084f) < 10,
                "Cockpit instruments track live altitude, fuel and RPM");
            yield return Capture("cockpit.png");
            mission.FlightCamera.SetMode(FlightCameraMode.Chase);
            Require(!instruments.IsVisible, "Chase camera hides cockpit-only instruments");
            var origin = mission.GetComponent<FloatingOriginSystem>();
            origin.RecenterDistance = 1000;
            float minRange = float.MaxValue, frames = 0, seconds = 0;
            long maximumGC = 0;
            var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            var cpu = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            double cpuTotal = 0;
            double gpuTotal = 0;
            int gpuSamples = 0;
            var timings = new FrameTiming[1];
            var frameTimes = new float[72000];
            int frameSamples = 0;
            long startingMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            float started = Time.realtimeSinceStartup;
            if (soak)
            {
                mission.Damage.DiagnosticIgnoreDamage = true;
                foreach (var enemy in mission.Enemies) { enemy.GetComponent<AircraftDamage>().DiagnosticIgnoreDamage = true; enemy.GetComponent<AircraftWeaponSystem>().UnlimitedAmmo = true; }
                File.AppendAllText(Path.Combine(output,"results.txt"),"DIAGNOSTIC SOAK: damage ignored, enemy ammunition unlimited; actual flight, AI, ballistics and hit effects active.\n");
            }
            float startingFuel = mission.Player.Engine.Fuel.FuelLitres;
            bool screenshot = false;
            while (Time.realtimeSinceStartup - started < requestedDuration && mission.State == MissionState.Flying)
            {
                var controls = mission.Player.Controls;
                if (soak)
                {
                    float angle = (Time.realtimeSinceStartup-started)*.025f;
                    Vector3 waypoint = mission.Player.Body.position + new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*1800;
                    waypoint.y = 3048;
                    controls = FighterAIFlightControl.Fly(mission.Player,waypoint,.85f,false,mission.Player.Data.StallReferenceSpeed);
                }
                controls.Throttle = .8f;
                controls.Fire = Time.realtimeSinceStartup - started > 8 && Time.realtimeSinceStartup - started < 11;
                mission.Player.Controls = controls;
                for (int i = 0; i < mission.Enemies.Length; i++)
                    minRange = Mathf.Min(minRange, Vector3.Distance(mission.Player.transform.position, mission.Enemies[i].transform.position));
                frames++; seconds += Time.unscaledDeltaTime;
                if (Time.realtimeSinceStartup - started > 8) maximumGC = System.Math.Max(maximumGC, gc.LastValue);
                if (Time.realtimeSinceStartup - started > 8 && frameSamples < frameTimes.Length) frameTimes[frameSamples++] = Time.unscaledDeltaTime * 1000;
                cpuTotal += cpu.LastValue;
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
                { gpuTotal += timings[0].gpuFrameTime; gpuSamples++; }
                if (!screenshot && Time.realtimeSinceStartup - started > 5)
                { yield return Capture("flight.png"); screenshot = true; }
                yield return null;
            }
            gc.Dispose(); cpu.Dispose();
            if (soak) Require(seconds >= requestedDuration-.5f,"Sustained sample reaches requested duration");
            var hudTelemetry = mission.GetComponent<FlightHUD>();
            File.AppendAllText(Path.Combine(output,"results.txt"),$"HUD telemetry {hudTelemetry.TelemetryAllocatedBytes} bytes over {hudTelemetry.TelemetryRefreshCount} refreshes\n");
            Require(!float.IsNaN(mission.Player.Body.position.x) && mission.Player.Physics.Airspeed < 350, "Flight simulation remains finite");
            Require(mission.Weapons.ShotsFired > 0 && mission.Weapons.Ammo < mission.Weapons.Configuration.Ammunition, "Player guns fire and consume ammunition");
            Require(minRange < 1600, "AI approaches player: closest " + minRange.ToString("F0") + "m");
            int enemyShots = 0;
            for (int i = 0; i < mission.Enemies.Length; i++) enemyShots += mission.Enemies[i].GetComponent<AircraftWeaponSystem>().ShotsFired;
            Require(enemyShots > 0, "AI guns fire in rendered mission: " + enemyShots + " rounds");
            Require(origin.TotalOffset.sqrMagnitude > 0 && Mathf.Abs(mission.Player.Body.position.x) < 10000, "Floating origin shifts during active combat");
            Require(mission.Player.Engine.Fuel.FuelLitres < startingFuel && mission.Player.Engine.Fuel.HasFuel, "Flight consumes fuel without premature starvation");
            string gpu = gpuSamples > 0 ? (gpuTotal / gpuSamples).ToString("F2") + "ms" : "unavailable";
            File.AppendAllText(Path.Combine(output, "results.txt"), $"PERFORMANCE {frames / Mathf.Max(.01f, seconds):F1} average FPS, {cpuTotal / System.Math.Max(1, frames) / 1000000:F2}ms main-thread average, GPU {gpu}, {maximumGC}B peak GC/frame after warmup. {SystemInfo.graphicsDeviceName}; {SystemInfo.processorType}\n");
            System.Array.Sort(frameTimes, 0, frameSamples);
            if (frameSamples > 0)
                File.AppendAllText(Path.Combine(output, "results.txt"), $"FRAME PACING p95={frameTimes[Mathf.Min(frameSamples - 1, Mathf.FloorToInt(frameSamples * .95f))]:F2}ms p99={frameTimes[Mathf.Min(frameSamples - 1, Mathf.FloorToInt(frameSamples * .99f))]:F2}ms samples={frameSamples}; allocated memory start={startingMemory / 1048576f:F1}MiB end={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f:F1}MiB; observed={seconds:F1}s requested={requestedDuration:F0}s\n");
            yield return new WaitForSecondsRealtime(.3f);
            if (failed) { File.AppendAllText(Path.Combine(output, "results.txt"), "SMOKE FAILED\n"); Application.Quit(1); yield break; }
            restarting = true; mission.Restart();
        }
        IEnumerator Capture(string filename)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (!texture) { Require(false, "Rendered screenshot capture " + filename); yield break; }
            File.WriteAllBytes(Path.Combine(output, filename), texture.EncodeToPNG());
            Destroy(texture);
        }
        void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}
