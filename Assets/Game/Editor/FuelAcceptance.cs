using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PacificCombat.Editor
{
    public static class FuelAcceptance
    {
        public static void Run()
        {
            var root = new GameObject("Fuel acceptance fixtures");
            var mustang = AircraftData.CreateMustang();
            var zero = AircraftData.CreateZero();
            try
            {
                var aircraft = CreateAircraft(root.transform, mustang);
                var fuel = aircraft.Engine.Fuel;
                Require(fuel && Mathf.Approximately(fuel.FuelLitres, 1020) && fuel.FuelFraction == 1, "Mustang starts with a full 1020 litre supply");
                fuel.Simulate(60, .2f, true);
                float lowThrottleDrain = mustang.FuelCapacity - fuel.FuelLitres;
                fuel.Initialize(aircraft);
                fuel.Simulate(60, 1, true);
                float fullThrottleDrain = mustang.FuelCapacity - fuel.FuelLitres;
                Require(fullThrottleDrain > lowThrottleDrain * 2, "Consumption increases with throttle");
                Require(Mathf.Abs(fullThrottleDrain - mustang.FullPowerFuelConsumption / 60) < .01f, "Consumption units are litres/hour converted to seconds");

                fuel.Initialize(aircraft);
                fuel.Simulate(60, 1, false);
                Require(fuel.FuelLitres == mustang.FuelCapacity, "Stopped healthy engine does not consume fuel");
                var damage = aircraft.GetComponent<AircraftDamage>();
                damage.ApplyDamage(DamageZoneType.FuelTank, 45, aircraft.transform.position);
                fuel.Simulate(10, 1, false);
                Require(damage.FuelLeaking && fuel.LeakRate > 0 && fuel.FuelLitres < mustang.FuelCapacity, "Actual tank damage leaks even with engine stopped");
                float minorLeak = fuel.LeakRate;
                damage.ApplyDamage(DamageZoneType.FuelTank, 30, aircraft.transform.position);
                fuel.Simulate(1, 0, false);
                Require(fuel.LeakRate > minorLeak && damage.Burning && fuel.ConsumptionRate > 0, "Severe tank damage increases leakage and fire consumes fuel");

                damage.Initialize(aircraft);
                fuel.Initialize(aircraft);
                aircraft.Controls.Throttle = 1;
                aircraft.Engine.Simulate(.02f);
                Require(aircraft.Engine.Thrust > 0 && aircraft.Engine.Power > 0, "Fueled engine creates thrust");
                fuel.Initialize(aircraft, .000001f);
                aircraft.Engine.Simulate(.02f);
                Require(!fuel.HasFuel && fuel.FuelLitres == 0 && aircraft.Engine.Thrust == 0 && aircraft.Engine.Power == 0, "Exhaustion clamps fuel and immediately cuts thrust/power");
                Require(!aircraft.IsDestroyed, "Fuel starvation preserves gliding aircraft");
                for (int i = 0; i < 10; i++) aircraft.Engine.Simulate(.02f);
                Require(fuel.FuelLitres == 0 && fuel.LeakRate == 0 && aircraft.Engine.Thrust == 0, "Empty supply remains finite and cannot create propulsion");

                CheckEndurance(aircraft);
                var enemy = CreateAircraft(root.transform, zero);
                Require(enemy.Engine.Fuel.FuelLitres == 520, "Zero starts with 520 litres");
                CheckEndurance(enemy);
                var failedAircraft = CreateAircraft(root.transform, mustang);
                var failedDamage = failedAircraft.GetComponent<AircraftDamage>();
                failedDamage.ApplyDamage(DamageZoneType.FuelTank, 80, failedAircraft.transform.position);
                failedDamage.ApplyDamage(DamageZoneType.Engine, 150, failedAircraft.transform.position);
                Require(failedDamage.Burning && failedAircraft.EngineHealth == 0, "Severe damage starts fire and fails engine");
                float damagedTankHealth = failedDamage.Health(DamageZoneType.FuelTank);
                failedAircraft.Engine.Fuel.Initialize(failedAircraft, .000001f);
                failedAircraft.Engine.Simulate(.02f);
                failedDamage.Simulate(.02f);
                Require(!failedDamage.Burning && !failedDamage.FuelLeaking, "Empty fuel supply extinguishes flames and leaking vapour");
                Require(failedAircraft.EngineHealth == 0 && failedAircraft.Engine.Thrust == 0 && failedDamage.Health(DamageZoneType.FuelTank) == damagedTankHealth,
                    "Extinguishing preserves failed engine and damaged tank");
                failedDamage.ApplyDamage(DamageZoneType.Engine, 1, failedAircraft.transform.position);
                Require(!failedDamage.Burning, "Empty tank cannot reignite after further hits");
                Debug.Log("FUEL ACCEPTANCE PASSED: full initial supply, throttle-scaled consumption, units, leak progression, stopped-engine leaks, fire drain, starvation, sustained default endurance.");
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(mustang); Object.DestroyImmediate(zero);
            }
        }

        static void CheckEndurance(AircraftController aircraft)
        {
            aircraft.GetComponent<AircraftDamage>().Initialize(aircraft);
            var fuel = aircraft.Engine.Fuel;
            fuel.Initialize(aircraft);
            for (int i = 0; i < 600; i++) fuel.Simulate(1, .78f, true);
            Require(fuel.FuelFraction > .9f && fuel.FuelFraction < 1, "Ten-minute default mission consumes fuel without early starvation");
        }
        static AircraftController CreateAircraft(Transform parent, AircraftData data)
        {
            var go = new GameObject(data.AircraftName + " fuel fixture"); go.transform.SetParent(parent); go.transform.position = new Vector3(0, 3000, 0);
            var aircraft = go.AddComponent<AircraftController>();
            var damage = go.AddComponent<AircraftDamage>(); damage.Initialize(aircraft); damage.enabled = false;
            aircraft.Initialize(data, false, 0, 100); aircraft.enabled = false;
            return aircraft;
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FUEL ACCEPTANCE FAILED: " + message);
        }
    }
}
