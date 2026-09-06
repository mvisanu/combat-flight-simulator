using UnityEngine;

namespace PacificCombat
{
    public static class FighterAIFlightControl
    {
        public static FlightControls Fly(AircraftController aircraft, Vector3 waypoint, float throttle,
            bool recovering, float stallSpeed, bool preciseAim = false)
        {
            Transform frame = aircraft.transform;
            Vector3 desired = (waypoint - frame.position).normalized;
            Vector3 horizontalForward = Vector3.ProjectOnPlane(frame.forward, Vector3.up).normalized;
            Vector3 horizontalDesired = Vector3.ProjectOnPlane(desired, Vector3.up).normalized;
            float headingError = Vector3.SignedAngle(horizontalForward, horizontalDesired, Vector3.up);
            float speed = aircraft.Body.linearVelocity.magnitude;
            float bankLimit = recovering ? 25f : Mathf.Lerp(30f, 68f, Mathf.InverseLerp(stallSpeed, stallSpeed * 1.7f, speed));
            float desiredBank = Mathf.Clamp(headingError * 1.35f, -bankLimit, bankLimit);
            Vector3 levelUp = Vector3.ProjectOnPlane(Vector3.up, frame.forward).normalized;
            float currentBank = -Vector3.SignedAngle(levelUp, frame.up, frame.forward);
            float bankError = Mathf.DeltaAngle(currentBank, desiredBank);
            Vector3 rates = frame.InverseTransformDirection(aircraft.Body.angularVelocity);
            Vector3 localDesired = frame.InverseTransformDirection(desired);
            // Elevator rotates in the aircraft's own pitch plane, including during a banked turn.
            float pitchError = Mathf.Clamp(Mathf.Atan2(localDesired.y, localDesired.z) * Mathf.Rad2Deg,
                -22f, recovering ? 18f : 32f);
            if (!recovering && localDesired.z < 0f)
            {
                // A target behind the tail requires a banked reversal, not the negative-G
                // push-over produced by atan2 when the rear target sits just below us.
                pitchError = 12f * Mathf.Clamp01(1f - Mathf.Abs(bankError) / 90f);
            }
            float elevator = pitchError * (preciseAim ? 0.1f : 0.055f) + rates.x * 0.3f;
            // Cancel the shared aerodynamic restoring moment, rather than adding a fixed
            // pitch offset that leaves the gun axis permanently above the requested aim.
            if (aircraft.Physics != null && aircraft.Data != null)
            {
                AircraftData data = aircraft.Data;
                float compression = 1f - data.HighSpeedControlLoss * Mathf.InverseLerp(
                    data.MaximumRecommendedSpeed * 0.8f, data.MaximumSafeDiveSpeed, speed);
                float separation = Mathf.InverseLerp(data.StallAngle, data.StallAngle + 20f, Mathf.Abs(aircraft.Physics.AngleOfAttack));
                float authority = data.PitchAuthority * compression * Mathf.Lerp(1f, 0.3f, separation) * aircraft.ControlHealth;
                float trim = aircraft.Physics.AngleOfAttack * Mathf.Deg2Rad * 1.8f / Mathf.Max(0.1f, authority);
                elevator += Mathf.Clamp(trim, -0.45f, 0.45f);
            }
            if (Mathf.Abs(currentBank) > 95f) elevator = Mathf.Max(0f, elevator);
            if (speed < stallSpeed * 1.15f) elevator = Mathf.Min(elevator, -0.08f);
            if (aircraft.Physics != null && aircraft.Physics.AngleOfAttack > 13f) elevator = Mathf.Min(elevator, 0.05f);
            return new FlightControls
            {
                Pitch = Mathf.Clamp(elevator, -0.6f, 0.85f),
                Roll = Mathf.Clamp(bankError * 0.024f + rates.z * 0.3f, -1f, 1f),
                Yaw = preciseAim
                    ? Mathf.Clamp(Mathf.Atan2(localDesired.x, localDesired.z) * Mathf.Rad2Deg * 0.025f - rates.y * 0.4f, -0.35f, 0.35f)
                    : Mathf.Clamp(headingError * 0.004f - rates.y * 0.3f, -0.25f, 0.25f),
                Throttle = Mathf.Clamp01(throttle),
                Gear = false,
                Flaps = !recovering && speed > stallSpeed * 1.2f && speed < stallSpeed * 1.5f && Mathf.Abs(headingError) > 45f,
                Brakes = false,
                Fire = false
            };
        }
    }
}
