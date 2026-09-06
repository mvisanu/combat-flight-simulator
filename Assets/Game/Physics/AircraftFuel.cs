using UnityEngine;

namespace PacificCombat
{
    /// <summary>Finite fuel supply; simulated once by the engine using SI seconds and litres.</summary>
    [DisallowMultipleComponent]
    public sealed class AircraftFuel : MonoBehaviour
    {
        public float FuelLitres { get; private set; }
        public float FuelFraction => capacity > 0 ? Mathf.Clamp01(FuelLitres / capacity) : 0;
        /// <summary>Current effective leak rate in litres/second; zero for empty tanks.</summary>
        public float LeakRate { get; private set; }
        public float ConsumptionRate { get; private set; }
        public bool HasFuel => FuelLitres > 0;
        AircraftController aircraft;
        float capacity, tankHealth = 1;
        bool burning;

        public void Initialize(AircraftController owner, float initialFraction = 1)
        {
            aircraft = owner;
            capacity = Mathf.Max(0, owner.Data.FuelCapacity);
            FuelLitres = capacity * Mathf.Clamp01(initialFraction);
            LeakRate = ConsumptionRate = 0;
            tankHealth = 1; burning = false;
            var damage = owner.GetComponent<AircraftDamage>();
            if (damage && damage.Aircraft) SetTankDamage(damage.Health(DamageZoneType.FuelTank), damage.Burning);
        }

        public void SetTankDamage(float health, bool onFire)
        {
            tankHealth = Mathf.Clamp01(health);
            burning = onFire;
        }

        public void Simulate(float dt, float throttle, bool engineRunning)
        {
            if (!aircraft || !aircraft.Data || dt <= 0) return;
            if (!HasFuel) { LeakRate = ConsumptionRate = 0; return; }
            var data = aircraft.Data;
            float severity = Mathf.Clamp01((.65f - tankHealth) / .65f);
            LeakRate = severity * Mathf.Max(0, data.MaximumFuelLeakRate);
            ConsumptionRate = engineRunning ? Mathf.Lerp(Mathf.Max(0, data.IdleFuelConsumption), Mathf.Max(0, data.FullPowerFuelConsumption), Mathf.Clamp01(throttle)) / 3600f : 0;
            if (burning) ConsumptionRate += Mathf.Max(0, data.FireFuelConsumption);
            FuelLitres = Mathf.Max(0, FuelLitres - (ConsumptionRate + LeakRate) * dt);
            if (!HasFuel) LeakRate = ConsumptionRate = 0;
        }
    }
}
