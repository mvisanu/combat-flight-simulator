using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PacificCombat.Editor
{
    /// <summary>Deterministic empty-scene PhysX acceptance tests, also runnable using -executeMethod.</summary>
    public static class FlightAcceptance
    {
        const float StepSeconds = .02f;
        static int assertions;

        [MenuItem("Pacific Combat/Run Flight Acceptance")]
        public static void Run()
        {
            assertions = 0;
            var oldMode = UnityEngine.Physics.simulationMode;
            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            bool canRestore = sceneSetup.Length > 0;
            for (int i = 0; i < sceneSetup.Length; i++)
                canRestore &= !string.IsNullOrEmpty(sceneSetup[i].path);
            if (!Application.isBatchMode)
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).isDirty)
                        throw new InvalidOperationException("Save modified scenes before running flight acceptance tests.");
            try
            {
                // Editor-created scenes share the default PhysX scene. Temporarily unload
                // the user's saved scenes so the acceptance run cannot simulate their bodies.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                UnityEngine.Physics.simulationMode = SimulationMode.Script;
                TestNeutralFlight();
                TestThrottle();
                TestControlDirectionsAndAirflow();
                TestStallAndRecovery();
                TestAircraftDifferences();
                TestWingDamage();
                Debug.Log("FLIGHT ACCEPTANCE PASSED: " + assertions + " assertions; empty editor scenes, manual PhysX at 50 Hz.");
            }
            catch (Exception ex)
            {
                Debug.LogError("FLIGHT ACCEPTANCE FAILED: " + ex);
                throw;
            }
            finally
            {
                UnityEngine.Physics.simulationMode = oldMode;
                if (canRestore) EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        static void TestNeutralFlight()
        {
            using (var test = new FlightFixture(false))
            {
                float minSpeed = float.MaxValue, maxSpeed = 0, maxRate = 0;
                for (int i = 0; i < 9000; i++)
                {
                    test.Step();
                    var aircraft = test.Aircraft;
                    Vector3 velocity = aircraft.Body.linearVelocity;
                    if (float.IsNaN(velocity.sqrMagnitude) || float.IsInfinity(velocity.sqrMagnitude))
                        throw new Exception("Neutral flight produced a non-finite velocity at " + i * StepSeconds + " seconds.");
                    minSpeed = Mathf.Min(minSpeed, velocity.magnitude);
                    maxSpeed = Mathf.Max(maxSpeed, velocity.magnitude);
                    maxRate = Mathf.Max(maxRate, aircraft.Body.angularVelocity.magnitude);
                }
                var plane = test.Aircraft;
                Check(minSpeed > 30f && maxSpeed < 300f, "Three-minute neutral flight remains within a plausible speed envelope.");
                Check(maxRate < 2f, "Neutral flight does not develop uncontrolled angular oscillation.");
                Check(plane.Body.position.y > 500f, "Neutral aircraft remains airborne for three minutes.");
                Debug.Log($"FLIGHT neutral 180s: speed range {minSpeed:F1}–{maxSpeed:F1} m/s; altitude {plane.Body.position.y:F0} m; max angular rate {maxRate:F3} rad/s; final AoA {plane.Physics.AngleOfAttack:F1}°.");
            }
        }

        static void TestThrottle()
        {
            float powerOffSpeed, fullPowerSpeed, zeroThrust, fullThrust;
            using (var test = new FlightFixture(false))
            {
                test.Aircraft.Controls.Throttle = 0f;
                test.Simulate(20f);
                powerOffSpeed = test.Aircraft.Body.linearVelocity.magnitude;
                zeroThrust = test.Aircraft.Engine.Thrust;
            }
            using (var test = new FlightFixture(false))
            {
                test.Aircraft.Controls.Throttle = 1f;
                test.Simulate(20f);
                fullPowerSpeed = test.Aircraft.Body.linearVelocity.magnitude;
                fullThrust = test.Aircraft.Engine.Thrust;
            }
            Check(zeroThrust < 1f && fullThrust > 1000f, "Closed throttle eliminates propulsive thrust; open throttle produces thrust.");
            Check(fullPowerSpeed > powerOffSpeed + 3f, "Opening throttle increases airspeed over a matched 20-second trajectory.");
            Debug.Log($"FLIGHT throttle 20s: idle {powerOffSpeed:F1} m/s / {zeroThrust:F0} N; full {fullPowerSpeed:F1} m/s / {fullThrust:F0} N.");
        }

        static void TestControlDirectionsAndAirflow()
        {
            Vector3 pitchRate = ControlRate(new FlightControls { Pitch = 1f, Throttle = .78f }, 112f);
            Vector3 rollRate = ControlRate(new FlightControls { Roll = 1f, Throttle = .78f }, 112f);
            Vector3 yawRate = ControlRate(new FlightControls { Yaw = 1f, Throttle = .78f }, 112f);
            Vector3 slowRoll = ControlRate(new FlightControls { Roll = 1f, Throttle = .78f }, 25f);
            Check(pitchRate.x < -.001f, "Positive pitch rotates the nose up (negative local X).");
            Check(rollRate.z < -.001f, "Positive roll banks right (negative local Z).");
            Check(yawRate.y > .001f, "Positive yaw moves the nose right (positive local Y).");
            Check(Mathf.Abs(slowRoll.z) < Mathf.Abs(rollRate.z) * .15f, "Low airflow materially reduces control authority.");
            Debug.Log($"FLIGHT input impulse rad/s: pitchX {pitchRate.x:F4}, rollZ {rollRate.z:F4}, yawY {yawRate.y:F4}; 25m/s rollZ {slowRoll.z:F4}.");
        }

        static Vector3 ControlRate(FlightControls controls, float speed)
        {
            using (var test = new FlightFixture(false, speed))
            {
                test.Aircraft.Controls = controls;
                test.Step();
                return test.Aircraft.transform.InverseTransformDirection(test.Aircraft.Body.angularVelocity);
            }
        }

        static void TestStallAndRecovery()
        {
            using (var test = new FlightFixture(false, 55f))
            {
                var aircraft = test.Aircraft;
                float peak = AircraftPhysics.EvaluateLiftCoefficient(aircraft.Data.StallAngle, aircraft.Data, false);
                float separated = AircraftPhysics.EvaluateLiftCoefficient(45f, aircraft.Data, false);
                float recovered = AircraftPhysics.EvaluateLiftCoefficient(10f, aircraft.Data, false);
                Check(peak > 1f && separated < peak * .6f, "Separated flow loses lift beyond the stall angle.");
                Check(recovered > separated, "Returning to attached flow restores lift coefficient.");
                // In edit mode Rigidbody.rotation is not copied to Transform until
                // the next simulation. This is initial test placement, so set both
                // before aerodynamic sampling and synchronize the physics world.
                aircraft.transform.rotation = Quaternion.Euler(-35f, 0, 0);
                aircraft.Body.rotation = aircraft.transform.rotation;
                UnityEngine.Physics.SyncTransforms();
                aircraft.Body.linearVelocity = Vector3.forward * 55f;
                aircraft.Controls.Pitch = -.3f;
                aircraft.Controls.Throttle = 1f;
                test.Step();
                Debug.Log($"FLIGHT induced stall initial AoA {aircraft.Physics.AngleOfAttack:F2}°; pitch {aircraft.transform.eulerAngles.x:F2}°; velocity {aircraft.Body.linearVelocity}.");
                Check(aircraft.Physics.IsStalled, "A 35-degree AoA flight condition enters stall.");
                float startingAoA = aircraft.Physics.AngleOfAttack;
                test.Simulate(1.5f);
                aircraft.Controls.Pitch = 0f;
                test.Simulate(10f);
                Check(!aircraft.Physics.IsStalled && Mathf.Abs(aircraft.Physics.AngleOfAttack) < aircraft.Data.StallAngle,
                    "Reducing pitch and applying power recovers from the induced stall through physics.");
                Check(aircraft.Body.linearVelocity.magnitude > 35f, "Recovered aircraft retains usable airspeed.");
                Debug.Log($"FLIGHT stall recovery: AoA {startingAoA:F1}° → {aircraft.Physics.AngleOfAttack:F1}°; speed {aircraft.Body.linearVelocity.magnitude:F1} m/s; CL peak/separated/recovered {peak:F2}/{separated:F2}/{recovered:F2}.");
            }
        }

        static void TestAircraftDifferences()
        {
            float mustangSpeed, zeroSpeed, mustangLoading, zeroLoading;
            using (var test = new FlightFixture(false))
            {
                test.Aircraft.Body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
                test.Aircraft.Body.useGravity = false;
                test.Aircraft.Controls.Throttle = 1f;
                test.Simulate(120f);
                mustangSpeed = test.Aircraft.Body.linearVelocity.magnitude;
                mustangLoading = test.Aircraft.Data.Mass / (test.Aircraft.Data.WingArea * test.Aircraft.Data.MaximumLiftCoefficient);
            }
            using (var test = new FlightFixture(true))
            {
                test.Aircraft.Body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
                test.Aircraft.Body.useGravity = false;
                test.Aircraft.Controls.Throttle = 1f;
                test.Simulate(120f);
                zeroSpeed = test.Aircraft.Body.linearVelocity.magnitude;
                zeroLoading = test.Aircraft.Data.Mass / (test.Aircraft.Data.WingArea * test.Aircraft.Data.MaximumLiftCoefficient);
                Check(test.Aircraft.Data.MaximumSafeDiveSpeed < 200f && test.Aircraft.Data.HighSpeedControlLoss > .6f,
                    "Zero has a lower dive envelope and substantial high-speed control loss.");
            }
            Check(mustangSpeed > zeroSpeed * 1.1f, "Mustang has a higher force-balanced level airspeed than Zero.");
            Check(zeroLoading < mustangLoading * .7f, "Zero's available lift per mass supports tighter low-speed turns.");
            Debug.Log($"FLIGHT aircraft comparison: constrained level 120s P51 {mustangSpeed:F1} m/s, Zero {zeroSpeed:F1} m/s; mass/(area*CLmax) {mustangLoading:F1}/{zeroLoading:F1} kg/m². Constraints are test fixtures only.");
        }

        static void TestWingDamage()
        {
            float intactLift;
            using (var test = new FlightFixture(false))
            {
                test.Step();
                intactLift = test.Aircraft.Physics.Lift;
            }
            using (var test = new FlightFixture(false))
            {
                test.Aircraft.LeftWingHealth = 0f;
                test.Step();
                float damagedLift = test.Aircraft.Physics.Lift;
                Check(damagedLift < intactLift * .55f && damagedLift > intactLift * .45f,
                    "A lost wing removes half the initial lift.");
                Check(test.Aircraft.Body.angularVelocity.magnitude > .005f,
                    "Asymmetric wing loss produces an actual angular response.");
                Debug.Log($"FLIGHT wing loss: lift {intactLift:F0} → {damagedLift:F0} N; rate {test.Aircraft.Body.angularVelocity.magnitude:F3} rad/s.");
            }
        }

        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            assertions++;
        }

        sealed class FlightFixture : IDisposable
        {
            public AircraftController Aircraft { get; }
            readonly Scene scene;
            readonly PhysicsScene physicsScene;
            readonly AircraftData data;

            public FlightFixture(bool zero, float speed = 112f)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                physicsScene = scene.GetPhysicsScene();
                var aircraftObject = new GameObject("Acceptance aircraft");
                SceneManager.MoveGameObjectToScene(aircraftObject, scene);
                aircraftObject.transform.position = new Vector3(0, 3050f, 0);
                aircraftObject.AddComponent<BoxCollider>().size = new Vector3(8f, 1f, 8f);
                Aircraft = aircraftObject.AddComponent<AircraftController>();
                data = zero ? AircraftData.CreateZero() : AircraftData.CreateMustang();
                Aircraft.Initialize(data, false, 0, speed);
                Aircraft.enabled = false;
                Aircraft.Body.interpolation = RigidbodyInterpolation.None;
                UnityEngine.Physics.SyncTransforms();
            }

            public void Step()
            {
                UnityEngine.Physics.SyncTransforms();
                Aircraft.Engine.Simulate(StepSeconds);
                Aircraft.Physics.Simulate(StepSeconds);
                physicsScene.Simulate(StepSeconds);
            }

            public void Simulate(float seconds)
            {
                int steps = Mathf.RoundToInt(seconds / StepSeconds);
                for (int i = 0; i < steps; i++) Step();
            }

            public void Dispose()
            {
                // Unity cannot close its last open scene. Remove our fixture and let
                // the next sequential test replace the now-empty temporary scene.
                if (Aircraft != null) UnityEngine.Object.DestroyImmediate(Aircraft.gameObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }
    }
}
