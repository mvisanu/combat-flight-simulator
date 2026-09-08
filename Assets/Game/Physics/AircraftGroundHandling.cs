using UnityEngine;
using System.Collections.Generic;

namespace PacificCombat
{
    [DisallowMultipleComponent]
    public sealed class AircraftGroundHandling : MonoBehaviour
    {
        public bool Grounded { get; private set; }
        public int ContactCount { get; private set; }
        public float GroundSpeed { get; private set; }
        public float LastTouchdownSpeed { get; private set; }
        public float GearHealth { get; private set; } = 1;
        public float SurfaceHeight { get; private set; }
        AircraftController aircraft;
        AircraftDamage damage;
        Vector3[] mounts;
        float[] lengths;
        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly Dictionary<Collider, bool> waterCache = new Dictionary<Collider, bool>();
        float steeringSign;
        bool wasGrounded;

        public void Initialize(AircraftController owner)
        {
            aircraft = owner; damage = owner.GetComponent<AircraftDamage>();
            bool tricycle = owner.Data.Type == AircraftType.P38Lightning;
            steeringSign = tricycle ? 1 : -1;
            float mainX = tricycle ? 2.65f : owner.Data.Type == AircraftType.Bf109 ? 1.35f : 2.2f;
            mounts = new[] { new Vector3(-mainX, -.2f, tricycle ? -.8f : .6f), new Vector3(mainX, -.2f, tricycle ? -.8f : .6f), new Vector3(0, -.2f, tricycle ? 2.05f : -3.6f) };
            lengths = new[] { 1.45f, 1.45f, tricycle ? 1.4f : .8f };
            GearHealth = 1; Grounded = wasGrounded = false;
        }
        public void Simulate(float dt)
        {
            if (!aircraft || dt <= 0) return;
            ContactCount = 0;
            GroundSpeed = Vector3.ProjectOnPlane(aircraft.Body.linearVelocity, Vector3.up).magnitude;
            if (!aircraft.Controls.Gear || GearHealth <= 0 || aircraft.IsDestroyed) { Grounded = wasGrounded = false; return; }
            float massPerWheel = aircraft.Body.mass / 3;
            float spring = massPerWheel * 9.81f / .18f;
            float damping = 2 * Mathf.Sqrt(spring * massPerWheel) * .78f;
            for (int wheel = 0; wheel < mounts.Length; wheel++)
            {
                Vector3 start = transform.TransformPoint(mounts[wheel]);
                int count = UnityEngine.Physics.RaycastNonAlloc(start, -transform.up, hits, lengths[wheel] + .25f, ~0, QueryTriggerInteraction.Ignore);
                int nearest = -1; float distance = float.MaxValue;
                for (int i = 0; i < count; i++)
                    if (!hits[i].collider.transform.IsChildOf(transform) && !IsWater(hits[i].collider) && hits[i].normal.y > .45f && hits[i].distance < distance)
                    { nearest = i; distance = hits[i].distance; }
                if (nearest < 0) continue;
                RaycastHit hit = hits[nearest];
                float compression = lengths[wheel] - distance;
                if (compression < 0) continue;
                ContactCount++; SurfaceHeight = hit.point.y;
                Vector3 velocity = aircraft.Body.GetPointVelocity(hit.point);
                float sink = Vector3.Dot(velocity, hit.normal);
                float normalForce = Mathf.Clamp(compression * spring - sink * damping, 0, aircraft.Body.mass * 9.81f * 2.5f);
                aircraft.Body.AddForceAtPosition(hit.normal * normalForce, hit.point);
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                if (wheel == 2) forward = Quaternion.AngleAxis(steeringSign * aircraft.Controls.Yaw * Mathf.Lerp(28, 5, GroundSpeed / 35), hit.normal) * forward;
                Vector3 lateral = Vector3.Cross(hit.normal, forward);
                float sideSpeed = Vector3.Dot(velocity, lateral), forwardSpeed = Vector3.Dot(velocity, forward);
                float lateralForce = Mathf.Clamp(-sideSpeed * massPerWheel / Mathf.Max(.08f, dt), -normalForce * .8f, normalForce * .8f);
                float brakeGrip = aircraft.Controls.Brakes ? .68f : .018f;
                float braking = Mathf.Clamp(-forwardSpeed * massPerWheel / Mathf.Max(.08f, dt), -normalForce * brakeGrip, normalForce * brakeGrip);
                aircraft.Body.AddForceAtPosition(lateral * lateralForce + forward * braking, hit.point);
                if (!wasGrounded && sink < -aircraft.Data.MaximumLandingSinkSpeed)
                {
                    LastTouchdownSpeed = -sink;
                    float severity = -sink - aircraft.Data.MaximumLandingSinkSpeed;
                    GearHealth = Mathf.Clamp01(GearHealth - severity * .12f);
                    if (damage) damage.ApplyDamage(DamageZoneType.Fuselage, severity * 8, hit.point);
                }
            }
            Grounded = ContactCount > 0;
            if (Grounded && !wasGrounded) LastTouchdownSpeed = Mathf.Max(LastTouchdownSpeed, Mathf.Max(0, -aircraft.Body.linearVelocity.y));
            wasGrounded = Grounded;
        }
        bool IsWater(Collider collider)
        {
            if (waterCache.TryGetValue(collider, out bool water)) return water;
            water = collider.GetComponent<WaterSurface>() != null; waterCache.Add(collider, water); return water;
        }
    }
}
