using System;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PacificCombat.Editor
{
    /// <summary>Ground contact and extended aerodynamic checks, with explicit reference limitations.</summary>
    public static class AdvancedFlightAcceptance
    {
        static readonly StringBuilder Report = new StringBuilder();
        public static void RunLandingInPlayMode()
        {
            SessionState.SetBool("PacificLandingAcceptance", true);
            EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        static void RegisterLandingRun()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PacificLandingAcceptance", false)) return;
                SessionState.SetBool("PacificLandingAcceptance", false);
                Report.Clear();
                try
                {
                    foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
                    UnityEngine.Physics.simulationMode = SimulationMode.Script;
                    CheckFactoryLanding();
                    Report.AppendLine("PLAY MODE FACTORY LANDING ACCEPTANCE PASSED");
                    File.WriteAllText("Logs/landing-playmode-acceptance.txt", Report.ToString());
                    EditorApplication.Exit(0);
                }
                catch (Exception ex)
                {
                    Report.AppendLine("FAIL " + ex); File.WriteAllText("Logs/landing-playmode-acceptance.txt", Report.ToString());
                    Debug.LogException(ex); EditorApplication.Exit(1);
                }
            };
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run advanced acceptance in an isolated batch editor.");
            var setup = EditorSceneManager.GetSceneManagerSetup(); var previousMode = UnityEngine.Physics.simulationMode;
            Report.Clear();
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); UnityEngine.Physics.simulationMode = SimulationMode.Script;
                CheckGround(); CheckFactoryLanding(); CheckThermals(); CheckDebris(); CheckStallAndSpin(); CheckReferences();
                Report.AppendLine("ADVANCED FLIGHT ACCEPTANCE PASSED"); Debug.Log(Report.ToString());
            }
            catch (Exception ex) { Report.AppendLine("FAIL " + ex); throw; }
            finally
            {
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/advanced-flight-acceptance.txt", Report.ToString());
                UnityEngine.Physics.simulationMode = previousMode;
                bool restore = setup.Length > 0; foreach (var scene in setup) restore &= !string.IsNullOrEmpty(scene.path);
                if (restore) EditorSceneManager.RestoreSceneManagerSetup(setup); else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        static AircraftController Plane(AircraftData data, Vector3 position, float speed = 0)
        {
            var go = new GameObject("Advanced flight fixture"); go.transform.position = position;
            var aircraft = go.AddComponent<AircraftController>();
            var damage = go.AddComponent<AircraftDamage>(); damage.Initialize(aircraft); damage.enabled = false;
            aircraft.Initialize(data, false, 0, speed); aircraft.enabled = false; aircraft.Body.interpolation = RigidbodyInterpolation.None;
            return aircraft;
        }
        static void Step(AircraftController aircraft, bool aero = true)
        {
            UnityEngine.Physics.SyncTransforms();
            if (aero) { aircraft.Engine.Simulate(.02f); aircraft.Physics.Simulate(.02f); }
            aircraft.GroundHandling.Simulate(.02f); UnityEngine.Physics.Simulate(.02f);
        }
        static void CheckGround()
        {
            var runway = new GameObject("Test runway at eight metres"); runway.transform.position = new Vector3(0, 7.5f, 0); runway.AddComponent<BoxCollider>().size = new Vector3(200, 1, 2400);
            var data = AircraftData.CreateLightning();
            try
            {
                float coast = GroundRun(data, false), braking = GroundRun(data, true);
                Require(braking < coast * .5f, "Brakes shorten actual PhysX ground roll: " + braking.ToString("F2") + " vs " + coast.ToString("F2") + "m/s");
                var aircraft = Plane(data, new Vector3(0, 9.7f, 0));
                try
                {
                    aircraft.Controls.Gear = true;
                    for (int i = 0; i < 300; i++) Step(aircraft, false);
                    Require(aircraft.GroundHandling.Grounded && aircraft.Body.position.y > 8.5f && aircraft.Body.position.y < 10, "Suspension supports aircraft above runway without teleportation");
                    aircraft.Body.linearVelocity = Vector3.forward * 8; aircraft.Controls.Yaw = 1;
                    for (int i = 0; i < 80; i++) Step(aircraft, false);
                    Require(aircraft.Body.angularVelocity.y > .001f, "Rudder input steers nose-wheel ground trajectory right");
                    runway.AddComponent<WaterSurface>();
                    // New fixture ensures the terrain identity cache sees the surface marker.
                }
                finally { Object.DestroyImmediate(aircraft.gameObject); }
                aircraft = Plane(data, new Vector3(0, 9.5f, 0)); aircraft.Controls.Gear = true;
                try { aircraft.GroundHandling.Simulate(.02f); Require(!aircraft.GroundHandling.Grounded, "Water cannot support landing wheels"); }
                finally { Object.DestroyImmediate(aircraft.gameObject); }
            }
            finally { Object.DestroyImmediate(runway); Object.DestroyImmediate(data); }
        }
        static float GroundRun(AircraftData data, bool brake)
        {
            var aircraft = Plane(data, new Vector3(0, 9.7f, 0));
            try
            {
                aircraft.Controls.Gear = true; aircraft.Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                for (int i = 0; i < 150; i++) Step(aircraft, false);
                aircraft.Body.linearVelocity = Vector3.forward * 15; aircraft.Controls.Brakes = brake;
                for (int i = 0; i < 200; i++) Step(aircraft, false);
                return aircraft.Body.linearVelocity.magnitude;
            }
            finally { Object.DestroyImmediate(aircraft.gameObject); }
        }
        static void CheckFactoryLanding()
        {
            var runway = new GameObject("Factory touchdown runway"); runway.transform.position = new Vector3(0, 7.5f, 0);
            runway.AddComponent<BoxCollider>().size = new Vector3(200, 1, 2400);
            var roster = new[] { AircraftData.CreateMustang(), AircraftData.CreateZero(), AircraftData.CreateBf109(), AircraftData.CreateLightning() };
            try
            {
                foreach (var data in roster)
                {
                    var plane = AircraftFactory.Create(data, false, 0, new Vector3(0, 9.9f, 0), Quaternion.identity, 30);
                    try
                    {
                        plane.enabled = false; plane.GetComponent<AircraftDamage>().enabled = false;
                        plane.Body.interpolation = RigidbodyInterpolation.None;
                        plane.Controls = new FlightControls { Gear = true, Brakes = true };
                        plane.Body.linearVelocity += Vector3.down;
                        UnityEngine.Physics.SyncTransforms();
                        foreach (var collider in plane.GetComponentsInChildren<Collider>())
                            if (collider.enabled) Require(collider.bounds.min.y > 8.5f, data.AircraftName + " collider clears extended gear: " + collider.name);
                        for (int i = 0; i < 600; i++) Step(plane);
                        Require(!plane.IsDestroyed && plane.GroundHandling.Grounded && plane.Airspeed < 2 && plane.GroundHandling.GearHealth > .95f,
                            data.AircraftName + " real compound collider safe landing stops intact; speed=" + plane.Airspeed + " gear=" + plane.GroundHandling.GearHealth + " position=" + plane.Body.position);
                    }
                    finally { Object.DestroyImmediate(plane.gameObject); }
                }
                var hard = AircraftFactory.Create(roster[3], false, 0, new Vector3(0, 10, 0), Quaternion.identity, 30);
                try
                {
                    hard.enabled = false; hard.GetComponent<AircraftDamage>().enabled = false; hard.Controls.Gear = true;
                    hard.Body.linearVelocity = new Vector3(0, -8, 30);
                    for (int i = 0; i < 50; i++) Step(hard, false);
                    Require(hard.GroundHandling.GearHealth < .8f && hard.GetComponent<AircraftDamage>().OverallHealth < 1, "Real P38 hard touchdown damages gear and structure");
                }
                finally { Object.DestroyImmediate(hard.gameObject); }
                var wall = new GameObject("Impact wall"); wall.transform.position = new Vector3(0, 30, 12); wall.AddComponent<BoxCollider>().size = new Vector3(100, 50, 1);
                var impact = AircraftFactory.Create(roster[3], false, 0, new Vector3(0, 30, 0), Quaternion.identity, 30);
                try
                {
                    impact.enabled = false; impact.GetComponent<AircraftDamage>().enabled = false;
                    for (int i = 0; i < 30; i++) Step(impact, false);
                    Require(impact.Body.position.z < 9, "Real P38 collider blocks wall penetration");
                    if (Application.isPlaying) Require(impact.IsDestroyed, "Real P38 high-speed wall impact remains fatal");
                }
                finally { Object.DestroyImmediate(impact.gameObject); Object.DestroyImmediate(wall); }
            }
            finally { Object.DestroyImmediate(runway); foreach (var data in roster) Object.DestroyImmediate(data); }
        }
        static void CheckThermals()
        {
            var data = AircraftData.CreateMustang(); var stationary = Plane(data, new Vector3(0, 100, 0)); var moving = Plane(data, new Vector3(100, 100, 0), 140);
            try
            {
                stationary.Controls.Throttle = moving.Controls.Throttle = 1;
                for (int i = 0; i < 4500; i++) { stationary.Engine.Simulate(.02f); moving.Engine.Simulate(.02f); }
                Require(stationary.Engine.CoolantTemperatureC > moving.Engine.CoolantTemperatureC + 15, "Airspeed increases engine cooling");
                Require(stationary.Engine.Overheating && stationary.EngineHealth < 1, "Prolonged full-power stationary operation damages overheated engine");
                Require(moving.EngineHealth > .95f, "Healthy cruise airflow avoids thermal damage");
                moving.GetComponent<AircraftDamage>().Ignite();
                for (int i = 0; i < 1500; i++) moving.Engine.Simulate(.02f);
                Require(moving.Engine.CoolantTemperatureC > data.CoolantLimitC, "Fire increases engine temperature");
            }
            finally { Object.DestroyImmediate(stationary.gameObject); Object.DestroyImmediate(moving.gameObject); Object.DestroyImmediate(data); }
        }
        static void CheckDebris()
        {
            var data = AircraftData.CreateMustang(); var plane = AircraftFactory.Create(data, false, 0, new Vector3(0, 3000, 0), Quaternion.identity, 100);
            try
            {
                var pool = plane.GetComponent<StructuralDebris>(); var damage = plane.GetComponent<AircraftDamage>();
                damage.ApplyDamage(DamageZoneType.LeftWing, 1000, plane.transform.position);
                Require(pool && pool.ActiveCount == 1 && plane.LeftWingHealth == 0, "Wing failure releases prewarmed physical structure and removes lift");
                int objects = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                pool.Simulate(40); Require(pool.ActiveCount == 0, "Expired debris returns to inactive pool");
                pool.ResetState(); pool.Release(DamageZoneType.LeftWing);
                Require(pool.ActiveCount == 1 && Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == objects, "Reusing detached structure creates no new rigidbody");
                pool.ShiftOrigin(new Vector3(8000, 0, 0));
            }
            finally { Object.DestroyImmediate(plane.gameObject); Object.DestroyImmediate(data); }
        }
        static void CheckStallAndSpin()
        {
            var data = AircraftData.CreateMustang(); var plane = Plane(data, new Vector3(0, 5000, 0), 65);
            try
            {
                plane.transform.rotation = Quaternion.Euler(-35, 0, 0); plane.Body.rotation = plane.transform.rotation;
                plane.Body.linearVelocity = Vector3.forward * 65 + Vector3.right * 14;
                plane.Controls = new FlightControls { Pitch = .8f, Yaw = 1, Throttle = .25f };
                Step(plane);
                Require(plane.Physics.IsStalled && Mathf.Abs(plane.Body.angularVelocity.y) > .001f, "Stall plus yaw creates incipient spin rotation");
                for (int i = 0; i < 35; i++) Step(plane);
                plane.Controls = new FlightControls { Pitch = -.6f, Yaw = -1, Throttle = 0 };
                for (int i = 0; i < 100; i++) Step(plane);
                plane.Controls = new FlightControls { Throttle = .7f };
                for (int i = 0; i < 600; i++) Step(plane);
                Require(!plane.Physics.IsStalled && float.IsFinite(plane.Airspeed), "Reduced AoA and anti-yaw inputs recover incipient stall/spin condition");
                Report.AppendLine("FAA AFH ch5 pp11,21–24: critical AoA causes stall; stall+yaw permits spin; opposite rudder and forward elevator aid recovery. This checks incipient dynamics, not aircraft-specific developed-spin certification.");
                Report.AppendLine("https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/airplane_handbook/06_afh_ch5.pdf");
            }
            finally { Object.DestroyImmediate(plane.gameObject); Object.DestroyImmediate(data); }
        }
        static void CheckReferences()
        {
            var data = new[] { AircraftData.CreateMustang(), AircraftData.CreateZero(), AircraftData.CreateBf109(), AircraftData.CreateLightning() };
            float[] mph = { 437, 331, 385, 414 };
            string[] urls = {
                "https://www.nationalmuseum.af.mil/Visit/Museum-Exhibits/Fact-Sheets/Display/Article/196263/north-american-p-51d-mustang/",
                "https://www.history.navy.mil/content/history/museums/nnam/explore/collections/aircraft/a/a6m2-zero0.html",
                "https://www.rafmuseum.org.uk/documents/LargePrintGuides/BoB_Guides/LPG_Battle_of_Britain_Cosford_web.pdf",
                "https://www.nationalmuseum.af.mil/Visit/Museum-Exhibits/Fact-Sheets/Display/Article/196280/lockheed-p-38l-lightning/" };
            for (int i = 0; i < data.Length; i++)
            {
                var plane = Plane(data[i], new Vector3(0, data[i].SuperchargerAltitude, 0), 130);
                try
                {
                    plane.Body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation; plane.Body.useGravity = false; plane.Controls.Throttle = 1;
                    for (int tick = 0; tick < 6000; tick++) Step(plane);
                    float measured = plane.Airspeed * 2.236936f, error = (measured / mph[i] - 1) * 100;
                    Require(float.IsFinite(measured) && measured > mph[i] * .65f && measured < mph[i] * 1.35f, "Broad reference speed envelope: " + data[i].AircraftName);
                    Report.AppendLine($"{data[i].AircraftName}: constrained-axis simulation {measured:F1}mph at {plane.Body.position.y:F0}m; reference {mph[i]}mph; difference {error:+0.0;-0.0;0.0}%. {urls[i]}");
                }
                finally { Object.DestroyImmediate(plane.gameObject); Object.DestroyImmediate(data[i]); }
            }
            Report.AppendLine("Reference limitations: Bf109G-2 and P38L are comparison variants for game G-6/J; weights/altitudes/power conditions are not identical. ±35% is a regression sanity bound, not a historical accuracy claim. Exact stall speeds and developed-spin envelopes remain unverified without matching manuals/test data.");
        }
        static void Require(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); Report.AppendLine("PASS " + message); }
    }
}
