using UnityEngine;

namespace PacificCombat
{
    public sealed class AircraftEngine : MonoBehaviour
    {
        public float RPM { get; private set; }
        public float Thrust { get; private set; }
        public float Power { get; private set; }
        public AircraftFuel Fuel { get; private set; }
        AircraftController aircraft;
        float spool;

        public void Initialize(AircraftController owner)
        {
            aircraft = owner; spool = owner.Controls.Throttle;
            Fuel = owner.GetComponent<AircraftFuel>();
            if (!Fuel) Fuel = owner.gameObject.AddComponent<AircraftFuel>();
            Fuel.Initialize(owner);
        }

        public void Simulate(float dt)
        {
            if (aircraft == null || aircraft.Data == null) return;
            var data = aircraft.Data;
            float health = aircraft.IsDestroyed ? 0f : Mathf.Clamp01(aircraft.EngineHealth);
            Fuel.Simulate(dt, aircraft.Controls.Throttle, health > 0);
            if (!Fuel.HasFuel) health = 0;
            spool = Mathf.MoveTowards(spool, aircraft.Controls.Throttle * health, dt * .5f);
            float altitudeLoss = Mathf.Exp(-Mathf.Max(0f, transform.position.y - data.SuperchargerAltitude) / 11000f);
            Power = data.EnginePower * spool * altitudeLoss * health;
            float speed = Mathf.Max(35f, Vector3.Dot(aircraft.Body.linearVelocity, transform.forward));
            Thrust = Mathf.Min(data.MaximumThrust * spool * health, Power * .83f / speed);
            RPM = Mathf.Lerp(650f, data.MaxRPM, Mathf.Sqrt(spool)) * health;
            aircraft.Body.AddForce(transform.forward * Thrust, ForceMode.Force);
        }
    }
}
