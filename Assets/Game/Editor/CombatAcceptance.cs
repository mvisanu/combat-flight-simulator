using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PacificCombat.Editor
{
    /// <summary>Editor integration checks: real colliders, raycasts, stepped bullets and component damage.</summary>
    public static class CombatAcceptance
    {
        public static void Run()
        {
            var root = new GameObject("Combat acceptance fixtures");
            AircraftData mustang = AircraftData.CreateMustang(), zero = AircraftData.CreateZero();
            WeaponData guns = WeaponData.CreateMustang();
            try
            {
                Vector3 origin = new Vector3(70000, 10000, 70000);
                AircraftController shooter = Aircraft(root.transform, mustang, origin, 0);
                AircraftController target = Aircraft(root.transform, zero, origin + Vector3.forward * 300, 1);
                var targetCollider = target.gameObject.AddComponent<BoxCollider>();
                targetCollider.size = new Vector3(8, 5, 2);
                var zone = target.gameObject.AddComponent<DamageZone>();
                zone.Owner = target.GetComponent<AircraftDamage>(); zone.Type = DamageZoneType.Engine;
                var ownCollider = shooter.gameObject.AddComponent<BoxCollider>();
                ownCollider.center = new Vector3(0, 0, 12); ownCollider.size = new Vector3(10, 5, 1);
                guns.SpreadDegrees = 0;
                var weapons = shooter.gameObject.AddComponent<AircraftWeaponSystem>();
                weapons.Initialize(shooter, guns);
                UnityEngine.Physics.SyncTransforms();

                shooter.Controls.Fire = true;
                weapons.Simulate(.02f);
                Require(weapons.ShotsFired == 6, "All six Mustang guns fire in first volley");
                Require(weapons.Ammo == guns.Ammunition - 6, "Ammo decreases by six");
                Require(weapons.Hits == 0, "Bullets have travel time");
                shooter.Controls.Fire = false;
                for (int i = 0; i < 25; i++) weapons.Simulate(.02f);
                Require(weapons.Hits == 6, "All six converged ballistics hit target and ignore owner collider");
                Require(target.EngineHealth < 1, "Raycast Engine zone damage degrades engine");
                Require(target.LeftWingHealth == 1, "Engine hits preserve wing state");
                for (int i = 0; i < 6; i++)
                {
                    Vector3 muzzle = weapons.GunPosition(i);
                    Vector3 convergence = shooter.transform.position + shooter.transform.forward * weapons.Convergence;
                    Vector3 direction = (convergence - muzzle).normalized;
                    Require(Mathf.Abs((muzzle + direction * ((convergence.z - muzzle.z) / direction.z)).x - convergence.x) < .01f,
                        "Wing gun convergence intersects configured range");
                }

                var damage = target.GetComponent<AircraftDamage>();
                damage.ApplyDamage(DamageZoneType.LeftWing, 30, target.transform.position);
                Require(target.LeftWingHealth < target.RightWingHealth, "Wing damage creates lift asymmetry");
                damage.ApplyDamage(DamageZoneType.Elevator, 20, target.transform.position);
                Require(target.ControlHealth < 1, "Elevator damage degrades flight control");
                float before = shooter.GetComponent<AircraftDamage>().Health(DamageZoneType.LeftWing);
                shooter.GetComponent<AircraftDamage>().ApplyDamage(DamageZoneType.LeftWing, 30, shooter.transform.position);
                Require(before - shooter.LeftWingHealth < 1 - target.LeftWingHealth, "Mustang more durable than Zero");
                int kills = 0;
                damage.Destroyed += _ => kills++;
                damage.ApplyDamage(DamageZoneType.Pilot, 1000, target.transform.position);
                damage.DestroyAircraft();
                damage.ApplyDamage(DamageZoneType.Fuselage, 1000, target.transform.position);
                Require(kills == 1 && target.IsDestroyed, "Destruction reported once only");
                Require(target.EngineHealth == 0 && target.ControlHealth == 0, "Destroyed aircraft loses propulsion and controls");

                // Exhaust actual ammo while no target is in line of fire, then verify training override.
                target.gameObject.SetActive(false);
                shooter.Controls.Fire = true;
                for (int i = 0; i < 1600; i++) weapons.Simulate(.025f);
                Require(weapons.Ammo == 0 && weapons.ShotsFired == guns.Ammunition, "Finite ammunition reaches zero without going negative");
                int exhaustedShots = weapons.ShotsFired;
                for (int i = 0; i < 20; i++) weapons.Simulate(.02f);
                Require(weapons.ShotsFired == exhaustedShots, "Empty guns cannot fire");
                weapons.UnlimitedAmmo = true;
                weapons.Simulate(.1f);
                Require(weapons.ShotsFired > exhaustedShots && weapons.Ammo == 0, "Training ammunition override fires without negative ammo");
                Debug.Log("COMBAT ACCEPTANCE PASSED: six guns, ammo, time of flight, convergence, raycast owner exclusion, damage zones, durability, finite ammo, training override, kill-once.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mustang); Object.DestroyImmediate(zero); Object.DestroyImmediate(guns);
            }
        }

        static AircraftController Aircraft(Transform parent, AircraftData data, Vector3 position, int team)
        {
            var go = new GameObject("Combat test aircraft"); go.transform.SetParent(parent); go.transform.position = position;
            var aircraft = go.AddComponent<AircraftController>(); aircraft.Initialize(data, false, team, 0);
            aircraft.Body.useGravity = false;
            aircraft.enabled = false;
            var damage = go.AddComponent<AircraftDamage>(); damage.Initialize(aircraft); damage.enabled = false;
            return aircraft;
        }
        static void Require(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("COMBAT ACCEPTANCE FAILED: " + description);
        }
    }
}
