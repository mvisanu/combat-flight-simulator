using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PacificCombat.Editor
{
    public static class AircraftRosterAcceptance
    {
        public static void Run()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            bool restore = setup.Length > 0;
            for (int i = 0; i < setup.Length; i++) restore &= !string.IsNullOrEmpty(setup[i].path);
            if (!Application.isBatchMode)
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scenes before roster acceptance.");
            var previousMode = UnityEngine.Physics.simulationMode;
            var data = new[] { AircraftData.CreateMustang(), AircraftData.CreateZero(), AircraftData.CreateBf109(), AircraftData.CreateLightning() };
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                UnityEngine.Physics.simulationMode = SimulationMode.Script;
                Require(data[2].Mass < data[0].Mass && data[3].Mass > data[0].Mass * 1.6f, "Distinct Bf109 and twin-engine mass profiles");
                Require(data[3].EnginePower > data[0].EnginePower * 1.7f && data[3].RollAuthority < data[0].RollAuthority, "P38 aggregate power and heavier roll response");
                for (int type = 0; type < data.Length; type++)
                {
                    Require((int)data[type].Type == type, "Explicit aircraft type");
                    for (int role = 0; role < 2; role++) TestAircraft(data[type], role == 0);
                }
                TestFiniteAmmo(WeaponData.CreateBf109());
                TestFiniteAmmo(WeaponData.CreateLightning());
                Debug.Log("ROSTER ACCEPTANCE PASSED: all four types in both player/enemy roles; distinct configs; model/collider mapping; twin propellers; nose armament; finite mixed ammo; new aircraft 30-second physics flights.");
            }
            finally
            {
                foreach (var item in data) Object.DestroyImmediate(item);
                UnityEngine.Physics.simulationMode = previousMode;
                if (restore) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        static void TestAircraft(AircraftData data, bool player)
        {
            var aircraft = AircraftFactory.Create(data, player, player ? 0 : 1, new Vector3(0, 3050, 0), Quaternion.identity, 112);
            try
            {
                aircraft.enabled = false;
                var input = aircraft.GetComponent<AircraftInput>(); if (input) input.enabled = false;
                aircraft.GetComponent<AircraftDamage>().enabled = false;
                var visuals = aircraft.GetComponent<AircraftVisuals>();
                var weapons = aircraft.GetComponent<AircraftWeaponSystem>();
                Require(aircraft.Data.Type == data.Type && aircraft.IsPlayer == player, "Factory preserves type/role independently");
                Require(visuals.DetailedAirframe, "Authored art loaded: " + data.Type);
                Require(Resources.Load<GameObject>(AircraftFactory.ArtPath(data.Type)), "Explicit model resource exists");
                int expectedGuns = data.Type == AircraftType.P51D ? 6 : data.Type == AircraftType.A6MZero ? 4 : data.Type == AircraftType.Bf109 ? 3 : 5;
                Require(weapons.Configuration.GunCount == expectedGuns, "Default armament follows aircraft type, not team");
                aircraft.Controls.Fire = true; weapons.Simulate(.02f); aircraft.Controls.Fire = false;
                Require(weapons.ShotsFired == expectedGuns && weapons.Ammo == weapons.Configuration.Ammunition - expectedGuns, "Every gun fires and consumes ammunition");
                if (data.Type == AircraftType.Bf109 || data.Type == AircraftType.P38Lightning)
                    for (int gun = 0; gun < expectedGuns; gun++) Require(Mathf.Abs(aircraft.transform.InverseTransformPoint(weapons.GunPosition(gun)).x) < .4f, "Central nose/hub muzzle placement");
                if (data.Type == AircraftType.P38Lightning)
                {
                    Require(visuals.Propellers != null && visuals.Propellers.Length == 2 && !visuals.Propeller, "Two P38 propellers, no central propeller");
                    Require(Mathf.Abs(visuals.Propellers[0].localPosition.x + 2.65f) < .01f && Mathf.Abs(visuals.Propellers[1].localPosition.x - 2.65f) < .01f, "Propellers align with nacelles");
                    Require(visuals.Gear.Length == 3 && visuals.Rudders.Length == 2, "Tricycle gear and paired rudders");
                    int engines = 0;
                    foreach (var zone in aircraft.GetComponentsInChildren<DamageZone>()) if (zone.Type == DamageZoneType.Engine) engines++;
                    Require(engines == 2, "Both engine nacelles have damage volumes");
                }
                // Only the new aircraft need another sustained flight run; existing
                // P51/Zero behavior has its own comprehensive FlightAcceptance suite.
                if (!player && (data.Type == AircraftType.Bf109 || data.Type == AircraftType.P38Lightning))
                {
                    aircraft.Body.interpolation = RigidbodyInterpolation.None;
                    for (int step = 0; step < 1500; step++)
                    {
                        UnityEngine.Physics.SyncTransforms();
                        aircraft.Engine.Simulate(.02f); aircraft.Physics.Simulate(.02f); UnityEngine.Physics.Simulate(.02f);
                    }
                    Require(float.IsFinite(aircraft.Body.linearVelocity.sqrMagnitude) && aircraft.Airspeed > 30 && aircraft.Airspeed < 300 && aircraft.Body.position.y > 500,
                        "Thirty-second stable airborne flight: " + data.Type);
                    Debug.Log($"ROSTER FLIGHT {data.Type}: {aircraft.Airspeed:F1} m/s, {aircraft.Body.position.y:F0}m altitude, {aircraft.Physics.AngleOfAttack:F1} deg AoA");
                }
            }
            finally { Object.DestroyImmediate(aircraft.gameObject); }
        }

        static void TestFiniteAmmo(WeaponData configuration)
        {
            var data = configuration.GunCount == 3 ? AircraftData.CreateBf109() : AircraftData.CreateLightning();
            var go = new GameObject("Mixed armament endurance fixture");
            try
            {
                go.transform.position = new Vector3(5000, 5000, 5000);
                var aircraft = go.AddComponent<AircraftController>(); aircraft.Initialize(data, false, 0, 0); aircraft.enabled = false;
                var guns = go.AddComponent<AircraftWeaponSystem>(); guns.Initialize(aircraft, configuration); aircraft.Controls.Fire = true;
                for (int i = 0; i < 7000; i++) guns.Simulate(.02f);
                Require(guns.Ammo == 0 && guns.CannonAmmo == 0 && guns.ShotsFired == configuration.Ammunition, "Both cannon and MG budgets exhaust without cross-consuming ammunition");
                int shots = guns.ShotsFired; guns.Simulate(1); Require(guns.ShotsFired == shots, "Empty mixed guns stop firing");
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(data); Object.DestroyImmediate(configuration); }
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("ROSTER ACCEPTANCE FAILED: " + message); }
    }
}
