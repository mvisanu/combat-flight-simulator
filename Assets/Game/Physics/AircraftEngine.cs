using UnityEngine;

namespace PacificCombat
{
    public sealed class AircraftEngine : MonoBehaviour
    {
        public float RPM { get; private set; }
        public float Thrust { get; private set; }
        public float Power { get; private set; }
        public AircraftFuel Fuel { get; private set; }
        public float CoolantTemperatureC { get; private set; } = 90;
        public float OilTemperatureC { get; private set; } = 80;
        public float OilPressureBar { get; private set; }
        public float ManifoldPressureInHg { get; private set; }
        public bool Overheating => aircraft && CoolantTemperatureC > aircraft.Data.CoolantLimitC;
        AircraftController aircraft;
        AircraftDamage damage;
        float spool;

        public void Initialize(AircraftController owner)
        {
            aircraft = owner; spool = owner.Controls.Throttle;
            Fuel = owner.GetComponent<AircraftFuel>();
            if (!Fuel) Fuel = owner.gameObject.AddComponent<AircraftFuel>();
            Fuel.Initialize(owner);
            damage = owner.GetComponent<AircraftDamage>();
            CoolantTemperatureC = 90; OilTemperatureC = 80;
        }

        public void Simulate(float dt)
        {
            if (aircraft == null || aircraft.Data == null) return;
            var data = aircraft.Data;
            float health = aircraft.IsDestroyed ? 0f : Mathf.Clamp01(aircraft.EngineHealth);
            Fuel.Simulate(dt, aircraft.Controls.Throttle, health > 0);
            if (!Fuel.HasFuel) health = 0;
            SimulateTemperatures(dt, health);
            spool = Mathf.MoveTowards(spool, aircraft.Controls.Throttle * health, dt * .5f);
            float altitudeLoss = Mathf.Exp(-Mathf.Max(0f, transform.position.y - data.SuperchargerAltitude) / 11000f);
            float thermalPower = 1 - Mathf.InverseLerp(data.CoolantLimitC, data.CriticalEngineTemperatureC, CoolantTemperatureC) * .35f;
            Power = data.EnginePower * spool * altitudeLoss * health * thermalPower;
            float speed = Mathf.Max(35f, Vector3.Dot(aircraft.Body.linearVelocity, transform.forward));
            Thrust = Mathf.Min(data.MaximumThrust * spool * health * thermalPower, Power * .83f / speed);
            RPM = Mathf.Lerp(650f, data.MaxRPM, Mathf.Sqrt(spool)) * health;
            OilPressureBar = Mathf.Lerp(0, 5.5f, RPM / Mathf.Max(1, data.MaxRPM)) * Mathf.Clamp01(1 - Mathf.Max(0, OilTemperatureC - 110) / 80);
            ManifoldPressureInHg = Mathf.Lerp(10, 58, spool * altitudeLoss) * (health > 0 ? 1 : 0);
            aircraft.Body.AddForce(transform.forward * Thrust, ForceMode.Force);
        }
        void SimulateTemperatures(float dt, float health)
        {
            float ambient = 15 - Mathf.Clamp(transform.position.y * .0065f, 0, 55);
            float cooling = Mathf.Clamp01(aircraft.Body.linearVelocity.magnitude / 140);
            bool onFire = damage && damage.Burning;
            float target = health > 0 ? ambient + 80 + aircraft.Controls.Throttle * 80 - cooling * 42 + (1 - health) * 55 : ambient;
            if (onFire) target += 140;
            float oilTarget = health > 0 ? ambient + 55 + aircraft.Controls.Throttle * 50 - cooling * 20 + (1 - health) * 30 : ambient;
            if (onFire) oilTarget += 85;
            float blend = 1 - Mathf.Exp(-Mathf.Max(0, dt) / Mathf.Max(5, aircraft.Data.CoolingTimeSeconds));
            CoolantTemperatureC = Mathf.Lerp(CoolantTemperatureC, target, blend);
            OilTemperatureC = Mathf.Lerp(OilTemperatureC, oilTarget, blend * .65f);
            if (damage && !aircraft.IsDestroyed && Overheating)
            {
                damage.ApplyDamage(DamageZoneType.Engine, (CoolantTemperatureC - aircraft.Data.CoolantLimitC) * .045f * dt, transform.position);
                if (CoolantTemperatureC > aircraft.Data.CriticalEngineTemperatureC) damage.Ignite();
            }
        }
    }
}
