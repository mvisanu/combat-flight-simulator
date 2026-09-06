using System;
using UnityEngine;

namespace PacificCombat
{
    [DisallowMultipleComponent]
    public sealed class AircraftDamage : MonoBehaviour
    {
        public event Action<AircraftDamage> Destroyed;
        public event Action<Vector3, float> Hit;
        public AircraftController Aircraft { get; private set; }
        public bool Burning { get; private set; }
        public bool FuelLeaking => FuelAvailable && Health(DamageZoneType.FuelTank) < .65f;
        bool FuelAvailable => !Aircraft || !Aircraft.Engine || !Aircraft.Engine.Fuel || Aircraft.Engine.Fuel.HasFuel;
        public bool SimplifiedDamage;
        public float OverallHealth
        {
            get { float sum = 0; for (int i = 0; i < health.Length; i++) sum += health[i]; return sum / health.Length; }
        }
        readonly float[] health = new float[13];
        readonly float[] capacity = { 120, 75, 45, 100, 150, 150, 55, 55, 65, 65, 95, 90, 190 };
        bool killReported;
        float burnTime;
        float stress;

        public void Initialize(AircraftController aircraft)
        {
            Aircraft = aircraft;
            for (int i = 0; i < health.Length; i++) health[i] = 1;
            Burning = killReported = false;
            burnTime = stress = 0;
            if (aircraft.Engine && aircraft.Engine.Fuel) aircraft.Engine.Fuel.SetTankDamage(1, false);
        }

        public float Health(DamageZoneType zone) => health[(int)zone];

        public void ApplyDamage(DamageZoneType zone, float amount, Vector3 point)
        {
            if (!Aircraft || killReported || amount <= 0) return;
            if (SimplifiedDamage && zone == DamageZoneType.Pilot) zone = DamageZoneType.Fuselage;
            int index = (int)zone;
            health[index] = Mathf.Clamp01(health[index] - amount / (capacity[index] * Aircraft.Data.Durability));
            Hit?.Invoke(point, amount);
            if (FuelAvailable && ((zone == DamageZoneType.FuelTank && health[index] < .32f) ||
                (zone == DamageZoneType.Engine && health[index] < .15f))) Burning = true;
            UpdateFlightDamage();
            if (Health(DamageZoneType.Pilot) <= 0 || Health(DamageZoneType.Cockpit) <= 0 ||
                Health(DamageZoneType.LeftWing) <= 0 || Health(DamageZoneType.RightWing) <= 0 ||
                Health(DamageZoneType.Fuselage) <= 0 || Health(DamageZoneType.HorizontalStabilizer) <= 0)
                DestroyAircraft();
        }

        void UpdateFlightDamage()
        {
            if (Aircraft.Engine && Aircraft.Engine.Fuel)
                Aircraft.Engine.Fuel.SetTankDamage(Health(DamageZoneType.FuelTank), Burning);
            Aircraft.EngineHealth = Health(DamageZoneType.Engine);
            Aircraft.LeftWingHealth = Health(DamageZoneType.LeftWing);
            Aircraft.RightWingHealth = Health(DamageZoneType.RightWing);
            Aircraft.ControlHealth = Mathf.Min(Health(DamageZoneType.Elevator), Health(DamageZoneType.Rudder),
                (Health(DamageZoneType.LeftAileron) + Health(DamageZoneType.RightAileron)) * .5f,
                Health(DamageZoneType.HorizontalStabilizer), Health(DamageZoneType.VerticalStabilizer));
        }

        void FixedUpdate() => Simulate(Time.fixedDeltaTime);

        public void Simulate(float dt)
        {
            if (!Aircraft || dt <= 0) return;
            // Fuel-fed flames and vapour cease when the supply is exhausted. Already
            // failed engines, ruined structure and wreck state are deliberately preserved.
            if (Burning && !FuelAvailable)
            {
                Burning = false;
                Aircraft.Engine.Fuel.SetTankDamage(Health(DamageZoneType.FuelTank), false);
            }
            if (killReported) return;
            if (transform.position.y < 1) { DestroyAircraft(); return; }
            float overG = Mathf.Max(0, Mathf.Abs(Aircraft.GForce) - Aircraft.Data.MaximumStructuralG);
            float overSpeed = Mathf.Max(0, Aircraft.Airspeed / Aircraft.Data.MaximumDiveSpeed - 1);
            stress = Mathf.Max(0, stress + (overG * .25f + overSpeed * 4 - .15f) * dt);
            if (stress > 1)
            {
                var weakWing = Aircraft.LeftWingHealth <= Aircraft.RightWingHealth ? DamageZoneType.LeftWing : DamageZoneType.RightWing;
                ApplyDamage(weakWing, (overG * 7 + overSpeed * 60) * dt, transform.position);
            }
            if (Burning)
            {
                burnTime += dt;
                ApplyDamage(DamageZoneType.Engine, 5 * dt, transform.position);
                ApplyDamage(DamageZoneType.Fuselage, 3 * dt, transform.position);
                if (burnTime > 24 || Aircraft.EngineHealth <= 0) DestroyAircraft();
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!Aircraft || killReported) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed > 18) DestroyAircraft();
            else if (speed > 4) ApplyDamage(DamageZoneType.Fuselage, speed * 3, transform.position);
        }

        public void DestroyAircraft()
        {
            if (killReported || !Aircraft) return;
            killReported = true;
            Aircraft.IsDestroyed = true;
            Aircraft.EngineHealth = 0;
            Aircraft.ControlHealth = 0;
            Destroyed?.Invoke(this);
        }
    }
}
