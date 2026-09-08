using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PacificCombat.Editor
{
    public static class AIAcceptance
    {
        public static void RunAll()
        {
            RunRoster();
            Run();
        }

        public static void RunRoster()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Roster acceptance requires an isolated batch editor.");
            var report = new System.Text.StringBuilder();
            SimulationMode previousMode = UnityEngine.Physics.simulationMode;
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                UnityEngine.Physics.simulationMode = SimulationMode.Script;
                AircraftType[] players = { AircraftType.P38Lightning, AircraftType.A6MZero, AircraftType.P51D, AircraftType.Bf109 };
                AircraftType[] enemies = { AircraftType.Bf109, AircraftType.P51D, AircraftType.P38Lightning, AircraftType.A6MZero };
                bool allFired = true;
                for (int matchup = 0; matchup < players.Length; matchup++)
                {
                    AircraftData playerData = RosterData(players[matchup]), enemyData = RosterData(enemies[matchup]);
                    AircraftController player = Spawn("Roster player", playerData, new Vector3(0, 3050, 0), Quaternion.identity, true);
                    var fighters = new FighterAIController[4];
                    var weapons = new AircraftWeaponSystem[4];
                    var minimumAngle = new float[] {180f,180f,180f,180f};
                    var minimumRange = new float[] {float.MaxValue,float.MaxValue,float.MaxValue,float.MaxValue};
                    var bestAimInfo = new string[4];
                    try
                    {
                        for (int i = 0; i < fighters.Length; i++)
                        {
                            AircraftController aircraft = Spawn("Roster enemy " + i, enemyData,
                                new Vector3((i - 1.5f) * 180f, 3150f + i * 35f, 4000f + Mathf.Abs(i - 1.5f) * 80f), Quaternion.Euler(0, 180, 0), false);
                            aircraft.Body.linearVelocity = aircraft.transform.forward * 105f;
                            weapons[i] = aircraft.gameObject.AddComponent<AircraftWeaponSystem>();
                            weapons[i].Initialize(aircraft);
                            fighters[i] = aircraft.gameObject.AddComponent<FighterAIController>();
                            fighters[i].Initialize(aircraft, player, i);
                        }
                        UnityEngine.Physics.SyncTransforms();
                        const float dt = 0.02f;
                        for (int tick = 0; tick < 3000; tick++)
                        {
                            player.Controls = new FlightControls { Throttle = 0.8f };
                            StepAircraft(player, dt);
                            for (int i = 0; i < fighters.Length; i++)
                            {
                                FighterAIController ai = fighters[i];
                                ai.Simulate(dt);
                                weapons[i].Simulate(dt);
                                float range = Vector3.Distance(player.transform.position, ai.transform.position);
                                minimumRange[i] = Mathf.Min(minimumRange[i], range);
                                float angle = Vector3.Angle(ai.transform.forward, ai.AimPoint - ai.transform.position);
                                if (range < 850f && angle < minimumAngle[i])
                                {
                                    minimumAngle[i] = angle;
                                    bestAimInfo[i] = $"t={tick*dt:F2}s range={range:F1}m aim distance={Vector3.Distance(ai.AimPoint,ai.transform.position):F1}m state={ai.State} visible={ai.TargetVisible} clear={SquadronController.ClearFireLane(ai,ai.transform.forward,range)}";
                                }
                                StepAircraft(ai.Aircraft, dt);
                            }
                            UnityEngine.Physics.Simulate(dt);
                            for (int i = 0; i < fighters.Length; i++)
                            {
                                float speed = fighters[i].Aircraft.Body.linearVelocity.magnitude;
                                Require(Finite(speed) && speed < 400f && fighters[i].transform.position.y > 20f, "Roster aircraft crashed or diverged");
                            }
                        }
                        int rounds = 0;
                        for (int i = 0; i < fighters.Length; i++)
                        {
                            rounds += weapons[i].ShotsFired;
                            report.AppendLine($"{players[matchup]} vs {enemies[matchup]} AI{i}: rounds={weapons[i].ShotsFired}, closest={minimumRange[i]:F1}m, min aim within850m={minimumAngle[i]:F2}deg ({bestAimInfo[i]})");
                        }
                        allFired &= rounds > 0;
                    }
                    finally
                    {
                        for (int i = 0; i < fighters.Length; i++) if (fighters[i] != null) UnityEngine.Object.DestroyImmediate(fighters[i].gameObject);
                        UnityEngine.Object.DestroyImmediate(player.gameObject);
                        UnityEngine.Object.DestroyImmediate(playerData);
                        UnityEngine.Object.DestroyImmediate(enemyData);
                    }
                }
                Require(allFired, "One or more roster aircraft types never fired actual weapons in the head-on encounter");
                report.AppendLine("PASS: all four aircraft types flew and fired real weapons in60s head-on encounters.");
            }
            catch (Exception exception) { report.AppendLine("FAIL: " + exception); throw; }
            finally
            {
                UnityEngine.Physics.simulationMode = previousMode;
                bool saved = false;
                for (int i = 0; i < setup.Length; i++) saved |= !string.IsNullOrEmpty(setup[i].path);
                if (saved) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/ai-roster-acceptance.txt", report.ToString());
                Debug.Log(report.ToString());
            }
        }

        static AircraftData RosterData(AircraftType type)
        {
            switch (type)
            {
                case AircraftType.A6MZero: return AircraftData.CreateZero();
                case AircraftType.Bf109: return AircraftData.CreateBf109();
                case AircraftType.P38Lightning: return AircraftData.CreateLightning();
                default: return AircraftData.CreateMustang();
            }
        }

        [MenuItem("Pacific Combat/Run AI Acceptance")]
        public static void Run()
        {
            var report = new System.Text.StringBuilder();
            SimulationMode previousMode = UnityEngine.Physics.simulationMode;
            SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene previousScene = SceneManager.GetActiveScene();
            Scene testScene = default;
            bool replacedScenes = Application.isBatchMode;
            AircraftData mustang = null, zero = null;
            bool passed = false;
            try
            {
                CheckIntercept(report);
                // Batch mode opens an unsaved untitled scene, which Unity cannot retain additively.
                // Interactive execution preserves the open scene and requires a saved scene first.
                if (!Application.isBatchMode && string.IsNullOrEmpty(previousScene.path))
                    throw new InvalidOperationException("Save the current scene before running AI acceptance interactively.");
                testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    replacedScenes ? NewSceneMode.Single : NewSceneMode.Additive);
                SceneManager.SetActiveScene(testScene);
                UnityEngine.Physics.simulationMode = SimulationMode.Script;
                mustang = AircraftData.CreateMustang();
                zero = AircraftData.CreateZero();
                CheckFireGate(mustang, zero, report);
                AircraftController player = Spawn("Test Mustang", mustang, new Vector3(0, 3050, 0), Quaternion.identity, true);
                var fighters = new FighterAIController[4];
                var minimumRange = new float[4];
                var acquired = new bool[4];
                var fired = new bool[4];
                var minimumAimAngle = new float[] {180f,180f,180f,180f};
                float minimumAltitude = 3050f, maximumSpeed = 0f;
                for (int i = 0; i < fighters.Length; i++)
                {
                    AircraftController aircraft = Spawn("Test Zero " + i, zero,
                        new Vector3((i - 1.5f) * 180f, 3050f + i * 80f, 4000f), Quaternion.Euler(0, 180, 0), false);
                    fighters[i] = aircraft.gameObject.AddComponent<FighterAIController>();
                    fighters[i].Initialize(aircraft, player, i);
                    minimumRange[i] = float.MaxValue;
                }
                UnityEngine.Physics.SyncTransforms();
                const float dt = 0.02f;
                for (int tick = 0; tick < 6000; tick++)
                {
                    // The reference target also uses only virtual controls and the shared physics.
                    player.Controls = FighterAIFlightControl.Fly(player, new Vector3(0, 3050, 30000), 0.65f, false, mustang.StallReferenceSpeed);
                    StepAircraft(player, dt);
                    for (int i = 0; i < fighters.Length; i++)
                    {
                        FighterAIController ai = fighters[i];
                        ai.Simulate(dt);
                        FlightControls controls = ai.Aircraft.Controls;
                        Require(Finite(controls.Pitch) && Finite(controls.Roll) && Finite(controls.Yaw) && Finite(controls.Throttle), "Non-finite AI controls");
                        Require(Mathf.Abs(controls.Pitch) <= 1f && Mathf.Abs(controls.Roll) <= 1f && Mathf.Abs(controls.Yaw) <= 1f
                            && controls.Throttle >= 0f && controls.Throttle <= 1f, "AI exceeded control limits");
                        acquired[i] |= ai.TargetVisible;
                        fired[i] |= controls.Fire;
                        if (Vector3.Distance(ai.transform.position, player.transform.position) < 650f)
                            minimumAimAngle[i] = Mathf.Min(minimumAimAngle[i], Vector3.Angle(ai.transform.forward, ai.AimPoint - ai.transform.position));
                        if (i == 0 && tick % 25 == 0 && Vector3.Distance(ai.transform.position, player.transform.position) < 1000f)
                        {
                            Vector3 localAim = ai.transform.InverseTransformDirection((ai.AimPoint - ai.transform.position).normalized);
                            report.AppendLine($"Aim t={tick*dt:F2} range={Vector3.Distance(ai.transform.position, player.transform.position):F0} local={localAim} pitch={controls.Pitch:F2} roll={controls.Roll:F2} yaw={controls.Yaw:F2} AoA={ai.Aircraft.Physics.AngleOfAttack:F1} separation={SquadronController.Separation(ai).magnitude:F1}");
                        }
                        StepAircraft(ai.Aircraft, dt);
                    }
                    UnityEngine.Physics.Simulate(dt);
                    for (int i = 0; i < fighters.Length; i++)
                    {
                        AircraftController aircraft = fighters[i].Aircraft;
                        Vector3 position = aircraft.Body.position;
                        float speed = aircraft.Body.linearVelocity.magnitude;
                        Require(Finite(position.x) && Finite(position.y) && Finite(position.z) && Finite(speed), "Non-finite aircraft state");
                        Require(position.y > 20f, "AI reached ocean surface at " + tick * dt + " s");
                        Require(speed < 400f, "AI diverged above 400 m/s");
                        minimumRange[i] = Mathf.Min(minimumRange[i], Vector3.Distance(position, player.Body.position));
                        minimumAltitude = Mathf.Min(minimumAltitude, position.y);
                        maximumSpeed = Mathf.Max(maximumSpeed, speed);
                    }
                    if (tick % 1000 == 0)
                        report.AppendLine($"t={tick * dt:F0}s AI0 range={Vector3.Distance(fighters[0].transform.position, player.transform.position):F0}m state={fighters[0].State} altitude={fighters[0].transform.position.y:F0}m speed={fighters[0].Aircraft.Body.linearVelocity.magnitude:F1}m/s");
                }
                bool anyFired = false;
                for (int i = 0; i < fighters.Length; i++)
                {
                    Require(acquired[i], "AI " + i + " never detected its target");
                    Require(minimumRange[i] < 1200f, "AI " + i + " failed to approach within 1200 m: " + minimumRange[i]);
                    anyFired |= fired[i];
                    report.AppendLine($"AI{i}: acquired={acquired[i]}, closest={minimumRange[i]:F1}m, fired={fired[i]}, min aim angle within650m={minimumAimAngle[i]:F2}, final state={fighters[i].State}");
                }
                Require(anyFired, "No AI fired during the 120-second engagement");
                report.AppendLine($"120 s / four AI: finite bounded controls, no ocean impact. Minimum altitude={minimumAltitude:F1}m; maximum speed={maximumSpeed:F1}m/s.");
                for (int i = 0; i < fighters.Length; i++) UnityEngine.Object.DestroyImmediate(fighters[i].gameObject);
                UnityEngine.Object.DestroyImmediate(player.gameObject);
                CheckManeuveringEngagement(mustang, zero, report);
                report.AppendLine("PASS");
                passed = true;
            }
            catch (Exception exception)
            {
                report.AppendLine("FAIL: " + exception);
                throw;
            }
            finally
            {
                UnityEngine.Physics.simulationMode = previousMode;
                if (replacedScenes)
                {
                    bool hasSavedScene = false;
                    for (int i = 0; i < sceneSetup.Length; i++) hasSavedScene |= !string.IsNullOrEmpty(sceneSetup[i].path);
                    if (hasSavedScene) EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                    else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                else
                {
                    if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
                    if (testScene.IsValid()) EditorSceneManager.CloseScene(testScene, true);
                }
                if (mustang != null) UnityEngine.Object.DestroyImmediate(mustang);
                if (zero != null) UnityEngine.Object.DestroyImmediate(zero);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/ai-acceptance.txt", report.ToString());
                if (passed) Debug.Log(report.ToString()); else Debug.LogError(report.ToString());
            }
        }

        static AircraftController Spawn(string name, AircraftData data, Vector3 position, Quaternion rotation, bool player)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);
            AircraftController aircraft = go.AddComponent<AircraftController>();
            aircraft.Initialize(data, player, player ? 0 : 1, 112f);
            aircraft.Body.interpolation = RigidbodyInterpolation.None;
            return aircraft;
        }

        static void StepAircraft(AircraftController aircraft, float dt)
        {
            aircraft.Engine.Simulate(dt);
            aircraft.Physics.Simulate(dt);
        }

        static void CheckFireGate(AircraftData mustang, AircraftData zero, System.Text.StringBuilder report)
        {
            AircraftController shooter = Spawn("Fire gate shooter", zero, new Vector3(0, 3050, 0), Quaternion.identity, false);
            AircraftController target = Spawn("Fire gate target", mustang, new Vector3(0, 3050, 300), Quaternion.identity, true);
            try
            {
                FighterAIController ai = shooter.gameObject.AddComponent<FighterAIController>();
                ai.Difficulty = FighterDifficulty.Ace;
                ai.Initialize(shooter, target, 0);
                UnityEngine.Physics.SyncTransforms();
                ai.Simulate(0.2f);
                Require(ai.State == FighterAIState.Attack && shooter.Controls.Fire, "Aligned real AI controller did not command fire");
                // Fixture setup is allowed to change orientation; the pilot itself still only writes controls.
                shooter.Body.rotation = Quaternion.Euler(0, 25, 0);
                shooter.transform.rotation = shooter.Body.rotation;
                UnityEngine.Physics.SyncTransforms();
                ai.Simulate(0.2f);
                Require(!shooter.Controls.Fire, "Misaligned real AI controller continued firing");
                report.AppendLine("PASS: real controller fires at aligned 300m target and suppresses fire at 25-degree misalignment.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shooter.gameObject);
                UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }

        static void CheckManeuveringEngagement(AircraftData mustang, AircraftData zero, System.Text.StringBuilder report)
        {
            AircraftController player = Spawn("Maneuvering Mustang", mustang, new Vector3(0, 3050, 0), Quaternion.identity, true);
            var fighters = new FighterAIController[4];
            var shotFrames = new int[4];
            var latePasses = new int[4];
            var lastShot = new float[] {-100f, -100f, -100f, -100f};
            var roleMask = new int[4];
            var lateRange = new float[] {float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue};
            var lateAngle = new float[] {180f,180f,180f,180f};
            var lateInfo = new string[4];
            for (int i = 0; i < fighters.Length; i++)
            {
                AircraftController aircraft = Spawn("Extended Zero " + i, zero,
                    new Vector3((i - 1.5f) * 180f, 3050f + i * 80f, 4000f), Quaternion.Euler(0, 180, 0), false);
                fighters[i] = aircraft.gameObject.AddComponent<FighterAIController>();
                fighters[i].Initialize(aircraft, player, i);
            }
            Vector3[] route = { new Vector3(0, 3050, 2400), new Vector3(2200, 3150, 1200),
                new Vector3(1800, 2950, -1600), new Vector3(-1600, 3100, -1800), new Vector3(-2000, 3000, 1600) };
            int routeIndex = 0, completedLegs = 0;
            float minimumAltitude = 3050f;
            UnityEngine.Physics.SyncTransforms();
            const float dt = 0.02f;
            for (int tick = 0; tick < 15000; tick++)
            {
                float time = tick * dt;
                if (Vector3.Distance(player.Body.position, route[routeIndex]) < 450f)
                {
                    routeIndex = (routeIndex + 1) % route.Length;
                    completedLegs++;
                }
                // A repeatable maneuvering target flies a broad changing-altitude circuit through
                // the same controls/forces. No position, rotation, or velocity is forced in flight.
                player.Controls = FighterAIFlightControl.Fly(player, route[routeIndex], 0.68f, false, mustang.StallReferenceSpeed);
                StepAircraft(player, dt);
                for (int i = 0; i < fighters.Length; i++)
                {
                    FighterAIController ai = fighters[i];
                    ai.Simulate(dt);
                    FlightControls controls = ai.Aircraft.Controls;
                    Require(Finite(controls.Pitch) && Finite(controls.Roll) && Finite(controls.Yaw) && Finite(controls.Throttle)
                        && Mathf.Abs(controls.Pitch) <= 1f && Mathf.Abs(controls.Roll) <= 1f && Mathf.Abs(controls.Yaw) <= 1f
                        && controls.Throttle >= 0f && controls.Throttle <= 1f, "Extended AI controls invalid");
                    roleMask[i] |= 1 << (int)ai.Role;
                    float range = Vector3.Distance(ai.transform.position, player.transform.position);
                    if (time > 45f)
                    {
                        lateRange[i] = Mathf.Min(lateRange[i], range);
                        float angle = Vector3.Angle(ai.transform.forward, ai.AimPoint - ai.transform.position);
                        if (range < 850f && angle < lateAngle[i])
                        {
                            lateAngle[i] = angle;
                            lateInfo[i] = $"t={time:F1} range={range:F0} role={ai.Role} separation={SquadronController.Separation(ai).magnitude:F1}m localAim={ai.transform.InverseTransformDirection((ai.AimPoint-ai.transform.position).normalized)}";
                        }
                    }
                    if (controls.Fire)
                    {
                        shotFrames[i]++;
                        if (time > 45f && time - lastShot[i] > 3f) latePasses[i]++;
                        lastShot[i] = time;
                    }
                    StepAircraft(ai.Aircraft, dt);
                }
                UnityEngine.Physics.Simulate(dt);
                Require(player.Body.position.y > 20f && Finite(player.Body.linearVelocity.magnitude), "Maneuvering reference target crashed or diverged");
                for (int i = 0; i < fighters.Length; i++)
                {
                    Vector3 position = fighters[i].Aircraft.Body.position;
                    float speed = fighters[i].Aircraft.Body.linearVelocity.magnitude;
                    Require(Finite(position.x) && Finite(position.y) && Finite(position.z) && Finite(speed) && speed < 400f,
                        "Extended AI physics became invalid");
                    Require(position.y > 20f, "Extended AI " + i + " hit the ocean at " + time);
                    minimumAltitude = Mathf.Min(minimumAltitude, position.y);
                }
                if (tick % 1000 == 0)
                    report.AppendLine($"Extended t={time:F0}s route legs={completedLegs} AI0={fighters[0].State}/{fighters[0].Role} range={Vector3.Distance(player.Body.position, fighters[0].Aircraft.Body.position):F0}m speeds player/AI={player.Body.linearVelocity.magnitude:F1}/{fighters[0].Aircraft.Body.linearVelocity.magnitude:F1} altitude difference={fighters[0].transform.position.y-player.transform.position.y:F0}m AoA={fighters[0].Aircraft.Physics.AngleOfAttack:F1}");
            }
            int participants = 0, repeatedPasses = 0;
            for (int i = 0; i < fighters.Length; i++)
            {
                if (shotFrames[i] > 0) participants++;
                repeatedPasses += latePasses[i];
                report.AppendLine($"Extended AI{i}: firing time={shotFrames[i] * dt:F2}s, distinct firing passes after45s={latePasses[i]}, role mask={roleMask[i]}, late closest={lateRange[i]:F0}m / aim within850m={lateAngle[i]:F1}deg ({lateInfo[i]})");
                Require(roleMask[i] == 7, "AI " + i + " did not rotate through attacker, wingman, and support roles");
            }
            Require(completedLegs >= 3, "Reference pilot failed to maneuver through the circuit");
            Require(participants >= 2, "Fewer than two AI participated in firing");
            Require(repeatedPasses >= 2, "AI failed to execute repeated firing passes after the initial encounter");
            report.AppendLine($"PASS: 300s maneuvering fight; {participants} firing participants, {repeatedPasses} late passes, {completedLegs} target legs, minimum AI altitude={minimumAltitude:F1}m.");
        }

        static void CheckIntercept(System.Text.StringBuilder report)
        {
            Vector3 point = FighterAIAiming.Intercept(Vector3.zero, Vector3.zero, new Vector3(0, 0, 1000), new Vector3(100, 0, 0), 500);
            Require(Mathf.Abs(point.x - 204.1241f) < 0.02f && Mathf.Abs(point.z - 1000f) < 0.02f, "Crossing target quadratic intercept incorrect");
            float flightTime = point.x / 100f;
            Require(Mathf.Abs(point.y + UnityEngine.Physics.gravity.y * 0.5f * flightTime * flightTime) < 0.02f, "Gravity compensation incorrect");
            Vector3 sameVelocity = FighterAIAiming.Intercept(Vector3.zero, new Vector3(80, 0, 20), new Vector3(0, 0, 1000), new Vector3(80, 0, 20), 500);
            Require(Mathf.Abs(sameVelocity.x) < 0.001f && Mathf.Abs(sameVelocity.z - 1000f) < 0.001f, "Shooter velocity inheritance incorrect");
            Vector3 degenerate = FighterAIAiming.Intercept(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, 500);
            Require(Finite(degenerate.x) && Finite(degenerate.y) && Finite(degenerate.z), "Coincident intercept non-finite");
            report.AppendLine("PASS: quadratic crossing intercept, gravity correction, shooter velocity inheritance, coincident target.");
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
