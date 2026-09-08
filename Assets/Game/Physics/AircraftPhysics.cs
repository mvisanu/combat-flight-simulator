using UnityEngine;

namespace PacificCombat
{
    public sealed class AircraftPhysics : MonoBehaviour
    {
        public bool AdvancedFlight = true;
        public float Airspeed { get; private set; }
        public float Altitude { get; private set; }
        public float AngleOfAttack { get; private set; }
        public float GForce { get; private set; } = 1f;
        public float Lift { get; private set; }
        public float Drag { get; private set; }
        public bool IsStalled { get; private set; }
        public float AirDensity { get; private set; }
        AircraftController aircraft;
        AerodynamicSurface left, right;
        Vector3 previousVelocity;

        public void Initialize(AircraftController owner)
        {
            aircraft = owner;
            previousVelocity = owner.Body.linearVelocity;
            left = MakeWing("Left lifting surface", -owner.Data.WingSpan * .23f);
            right = MakeWing("Right lifting surface", owner.Data.WingSpan * .23f);
        }

        AerodynamicSurface MakeWing(string label, float x)
        {
            var child = transform.Find(label);
            if (child == null)
            {
                var go = new GameObject(label);
                child = go.transform;
                child.SetParent(transform, false);
                child.localPosition = new Vector3(x, 0, 0);
            }
            var surface = child.GetComponent<AerodynamicSurface>();
            if (surface == null) surface = child.gameObject.AddComponent<AerodynamicSurface>();
            surface.Area = aircraft.Data.WingArea * .5f;
            return surface;
        }

        public static float EvaluateLiftCoefficient(float angleDegrees, AircraftData data, bool flaps)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float attached = Mathf.Clamp(data.TrimLiftCoefficient + radians * data.LiftSlope + (flaps ? .35f : 0f), -data.MaximumLiftCoefficient, data.MaximumLiftCoefficient);
            float separation = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(data.StallAngle, data.StallAngle + 24f, Mathf.Abs(angleDegrees)));
            return Mathf.Lerp(attached, .65f * Mathf.Sin(2f * radians), separation);
        }

        public void Simulate(float dt)
        {
            if (aircraft == null) return;
            var body = aircraft.Body;
            var data = aircraft.Data;
            Vector3 groundVelocity = body.linearVelocity;
            Vector3 velocity = groundVelocity - PacificEnvironment.WindVelocity(transform.position);
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            Airspeed = velocity.magnitude;
            Altitude = transform.position.y;
            AirDensity = 1.225f * Mathf.Exp(-Mathf.Max(0f, Altitude) / 8500f);
            AngleOfAttack = Airspeed > 2f ? Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg : 0f;
            float beta = Mathf.Atan2(localVelocity.x, Mathf.Max(2f, Mathf.Abs(localVelocity.z)));
            float cl = EvaluateLiftCoefficient(AngleOfAttack, data, aircraft.Controls.Flaps);
            float separation = Mathf.InverseLerp(data.StallAngle, data.StallAngle + 20f, Mathf.Abs(AngleOfAttack));
            IsStalled = separation > .03f;
            if (!AdvancedFlight)
            {
                float attached = Mathf.Clamp(data.TrimLiftCoefficient + AngleOfAttack * Mathf.Deg2Rad * data.LiftSlope + (aircraft.Controls.Flaps ? .35f : 0), -data.MaximumLiftCoefficient, data.MaximumLiftCoefficient);
                cl = Mathf.Lerp(cl, attached, .65f);
                separation *= .25f;
            }
            float overspeed = Mathf.Max(0f, Airspeed / data.MaximumRecommendedSpeed - 1f);
            float cd = data.BaseDragCoefficient + data.InducedDragCoefficient * cl * cl + separation * .8f + overspeed * overspeed * .15f;
            if (aircraft.Controls.Flaps) cd += .028f;
            if (aircraft.Controls.Gear) cd += .035f;
            if (aircraft.Controls.Brakes) cd += .065f;
            left.DamageMultiplier = aircraft.LeftWingHealth;
            right.DamageMultiplier = aircraft.RightWingHealth;
            left.DragMultiplier = 1f + (1f - left.DamageMultiplier) * 1.5f;
            right.DragMultiplier = 1f + (1f - right.DamageMultiplier) * 1.5f;
            left.Apply(body, AirDensity, cl, cd);
            right.Apply(body, AirDensity, cl, cd);
            Lift = left.LastLift + right.LastLift;
            Drag = .5f * AirDensity * Airspeed * Airspeed * data.WingArea * cd;
            // Fin side force damps slip, allowing a banked lift vector to produce a coordinated turn.
            body.AddForce(-transform.right * (localVelocity.x * Airspeed * AirDensity * 2.8f), ForceMode.Force);
            float airflow = Mathf.Clamp(Airspeed * Airspeed / 10000f, .015f, 1.65f);
            float compression = 1f - data.HighSpeedControlLoss * Mathf.InverseLerp(data.MaximumRecommendedSpeed * .8f, data.MaximumSafeDiveSpeed, Airspeed);
            float authority = airflow * compression * Mathf.Lerp(1f, .3f, separation) * aircraft.ControlHealth;
            Vector3 rates = transform.InverseTransformDirection(body.angularVelocity);
            float aoaRadians = AngleOfAttack * Mathf.Deg2Rad;
            Vector3 acceleration = new Vector3(
                -aircraft.Controls.Pitch * data.PitchAuthority * authority + aoaRadians * airflow * 1.8f - rates.x * 2.4f,
                aircraft.Controls.Yaw * data.YawAuthority * authority + beta * airflow * 1.5f - rates.y * 1.8f,
                -aircraft.Controls.Roll * data.RollAuthority * authority - rates.z * 2.7f);
            // Separated flow plus sideslip produces a recoverable asymmetric wing drop/spin.
            if (AdvancedFlight)
            {
                acceleration.z += separation * (beta * 2f + Mathf.Sin(Time.fixedTime * 17f) * .07f);
                acceleration.y += separation * beta * .6f;
            }
            body.AddRelativeTorque(acceleration, ForceMode.Acceleration);
            Vector3 specificForce = (groundVelocity - previousVelocity) / Mathf.Max(.001f, dt) - UnityEngine.Physics.gravity;
            GForce = Mathf.Lerp(GForce, Vector3.Dot(specificForce, transform.up) / 9.81f, .2f);
            previousVelocity = groundVelocity;
        }
    }
}
