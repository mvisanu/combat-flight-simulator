using System;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PacificCombat.Editor
{
    public static class AIExpandedAcceptance
    {
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Expanded AI acceptance requires an isolated batch editor.");
            var report = new StringBuilder();
            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            SimulationMode previousMode = UnityEngine.Physics.simulationMode;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                UnityEngine.Physics.simulationMode = SimulationMode.Script;
                AircraftData[] data = { AircraftData.CreateZero(), AircraftData.CreateMustang(), AircraftData.CreateBf109(), AircraftData.CreateLightning() };
                try
                {
                    foreach (AircraftData aircraft in data)
                    {
                        foreach (FighterAIManeuver maneuver in new[] { FighterAIManeuver.Immelmann, FighterAIManeuver.SplitS,
                            FighterAIManeuver.RollingScissors, FighterAIManeuver.BarrelRollDefense }) CheckManeuver(aircraft, maneuver, report);
                        CheckDamageAndSpawn(aircraft, report);
                    }
                }
                finally { foreach (AircraftData aircraft in data) UnityEngine.Object.DestroyImmediate(aircraft); }
                report.AppendLine("PASS: completed phased maneuvers, abort safeguards, varied spawns and damaged flight for all aircraft.");
            }
            catch (Exception exception) { report.AppendLine("FAIL: " + exception); throw; }
            finally
            {
                UnityEngine.Physics.simulationMode = previousMode;
                bool saved = false;
                foreach (var scene in sceneSetup) saved |= !string.IsNullOrEmpty(scene.path);
                if (saved) EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/ai-expanded-acceptance.txt", report.ToString());
                Debug.Log(report.ToString());
            }
        }

        static AircraftController Spawn(AircraftData data, Vector3 position, Quaternion rotation, float speed)
        {
            var go = new GameObject("Expanded AI fixture");
            go.transform.SetPositionAndRotation(position, rotation);
            AircraftController aircraft = go.AddComponent<AircraftController>();
            aircraft.Initialize(data, false, 1, speed);
            aircraft.Body.interpolation = RigidbodyInterpolation.None;
            return aircraft;
        }

        static void CheckManeuver(AircraftData data, FighterAIManeuver kind, StringBuilder report)
        {
            float entrySpeed = kind == FighterAIManeuver.SplitS ? data.StallReferenceSpeed * 2f : data.MaximumSafeDiveSpeed * 0.82f;
            AircraftController aircraft = Spawn(data, new Vector3(0, 4000, 0), Quaternion.identity, entrySpeed);
            try
            {
                var maneuver = new FighterAIManeuverExecutor();
                Require(maneuver.Begin(kind, aircraft, 0), "Valid maneuver entry rejected");
                UnityEngine.Physics.SyncTransforms();
                float peakG = 0, minimumAltitude = 4000, maximumAltitude = 4000, maxSide = 0;
                const float dt = 0.02f;
                for (int tick = 0; tick < 2000 && maneuver.Active; tick++)
                {
                    if (maneuver.Step(aircraft, dt, 0, out FlightControls controls)) aircraft.Controls = controls;
                    Require(!aircraft.Controls.Fire && Mathf.Abs(aircraft.Controls.Roll) <= 1f && Mathf.Abs(aircraft.Controls.Pitch) <= 1f,
                        "Maneuver violated pilot control limits");
                    aircraft.Engine.Simulate(dt); aircraft.Physics.Simulate(dt);
                    UnityEngine.Physics.Simulate(dt);
                    peakG = Mathf.Max(peakG, aircraft.Physics.GForce);
                    minimumAltitude = Mathf.Min(minimumAltitude, aircraft.Body.position.y);
                    maximumAltitude = Mathf.Max(maximumAltitude, aircraft.Body.position.y);
                    maxSide = Mathf.Max(maxSide, Mathf.Abs(aircraft.Body.position.x));
                    Require(Finite(aircraft.Body.linearVelocity.magnitude) && minimumAltitude > 20f, "Maneuver physics diverged or hit ocean");
                }
                report.AppendLine($"{data.Type} {kind}: {maneuver.Phase} ({maneuver.AbortReason}), pitch travel={maneuver.PitchTravel*Mathf.Rad2Deg:F1} roll travel={maneuver.RollTravel*Mathf.Rad2Deg:F1} heading={aircraft.transform.eulerAngles.y:F1} altitude min/max={minimumAltitude:F0}/{maximumAltitude:F0} peakG={peakG:F1}");
                Require(maneuver.Phase == ManeuverPhase.Complete, data.Type + " " + kind + " did not complete: " + maneuver.AbortReason);
                Require(peakG < data.MaximumStructuralG * 1.1f, "Maneuver exceeded the airframe load margin");
                Require(Vector3.Dot(aircraft.transform.up, Vector3.up) > 0.9f, "Maneuver did not recover upright");
                if (kind == FighterAIManeuver.Immelmann || kind == FighterAIManeuver.SplitS)
                {
                    Require(Vector3.Dot(aircraft.transform.forward, Vector3.back) > 0.7f, "Vertical reversal did not reverse heading");
                    Require(kind == FighterAIManeuver.Immelmann ? maximumAltitude > 4200f : minimumAltitude < 3800f, "Vertical reversal did not change altitude");
                }
                else
                {
                    Require(maneuver.RollTravel > (kind == FighterAIManeuver.RollingScissors ? 11.5f : 5.8f), "Rolling maneuver did not complete its arcs");
                    Require(maxSide > 15f && maximumAltitude - minimumAltitude > 15f, "Rolling maneuver did not produce a three-dimensional flight path");
                }
                var safety = new FighterAIManeuverExecutor();
                aircraft.Body.position = new Vector3(0, 250, 0); aircraft.transform.position = aircraft.Body.position;
                Require(!safety.Begin(FighterAIManeuver.SplitS, aircraft, 0), "Unsafe low-altitude Split-S was accepted");
                aircraft.Body.position = new Vector3(0, 4000, 0); aircraft.transform.position = aircraft.Body.position;
                aircraft.Body.linearVelocity = aircraft.transform.forward * data.MaximumSafeDiveSpeed * 0.82f;
                Require(safety.Begin(FighterAIManeuver.BarrelRollDefense, aircraft, 0), "Safety fixture failed to enter maneuver");
                aircraft.ControlHealth = 0.2f;
                Require(!safety.Step(aircraft, dt, 0, out _) && safety.Phase == ManeuverPhase.Aborted, "Damage did not abort active maneuver");
            }
            finally { UnityEngine.Object.DestroyImmediate(aircraft.gameObject); }
        }

        static void CheckDamageAndSpawn(AircraftData data, StringBuilder report)
        {
            AircraftController target = Spawn(data, new Vector3(0, 2300, 1800), Quaternion.identity, 110f);
            AircraftController aircraft = Spawn(data, new Vector3(-900, 2500, 0), Quaternion.Euler(0, 55, -15), 105f);
            try
            {
                aircraft.EngineHealth = 0.22f; aircraft.ControlHealth = 0.7f;
                aircraft.LeftWingHealth = 0.78f; aircraft.RightWingHealth = 0.92f;
                FighterAIController ai = aircraft.gameObject.AddComponent<FighterAIController>();
                ai.Initialize(aircraft, target, 0);
                Require(!ai.RequestManeuver(FighterAIManeuver.Immelmann), "Damaged aircraft accepted advanced maneuver");
                bool damagedState = false;
                UnityEngine.Physics.SyncTransforms();
                for (int tick = 0; tick < 3000; tick++)
                {
                    ai.Simulate(0.02f);
                    damagedState |= ai.State == FighterAIState.Damaged;
                    Require(!aircraft.Controls.Fire, "Combat-ineffective aircraft continued firing");
                    aircraft.Engine.Simulate(0.02f); aircraft.Physics.Simulate(0.02f);
                    target.Controls = new FlightControls { Throttle = 0.65f };
                    target.Engine.Simulate(0.02f); target.Physics.Simulate(0.02f);
                    UnityEngine.Physics.Simulate(0.02f);
                    Require(Finite(aircraft.Body.linearVelocity.magnitude) && aircraft.Body.position.y > 20f, "Damaged AI crashed during recoverable offset-spawn fixture");
                }
                Require(damagedState, "Damaged behavior never activated");
                report.AppendLine($"{data.Type}: damaged offset/banked spawn survived60s at{aircraft.Body.position.y:F0}m; rejected unsafe aerobatics.");
            }
            finally { UnityEngine.Object.DestroyImmediate(aircraft.gameObject); UnityEngine.Object.DestroyImmediate(target.gameObject); }
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
