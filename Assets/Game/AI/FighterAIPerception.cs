using UnityEngine;

namespace PacificCombat
{
    public sealed class FighterAIPerception
    {
        public bool Visible { get; private set; }
        public bool HasTrack { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float Range { get; private set; }
        public float ClosingSpeed { get; private set; }
        public bool ThreatBehind { get; private set; }
        float lastSeen = float.NegativeInfinity;
        Vector3 lastPosition;
        float turnRate;
        public Vector3 PredictPosition(float now) => HasTrack ? lastPosition + Velocity * Mathf.Clamp(now - lastSeen, 0f, 12f) : Position;

        public Vector3 PredictFlightPosition(float now, float secondsAhead)
        {
            float time = Mathf.Clamp(now - lastSeen + secondsAhead, 0f, 12f);
            if (Mathf.Abs(turnRate) < 0.002f) return lastPosition + Velocity * time;
            float angle = turnRate * time;
            Vector3 horizontal = Vector3.ProjectOnPlane(Velocity, Vector3.up);
            Vector3 sideways = Vector3.Cross(Vector3.up, horizontal);
            return lastPosition + horizontal * (Mathf.Sin(angle) / turnRate)
                + sideways * ((1f - Mathf.Cos(angle)) / turnRate) + Vector3.up * (Velocity.y * time);
        }

        public void ShiftOrigin(Vector3 offset)
        {
            lastPosition -= offset;
            Position -= offset;
        }

        public void Sample(AircraftController self, AircraftController target, float now, float detectionRange,
            LayerMask obstructionMask)
        {
            Visible = false;
            ThreatBehind = false;
            if (target == null || target.IsDestroyed) { HasTrack = false; return; }
            Vector3 separation = target.transform.position - self.transform.position;
            float actualRange = separation.magnitude;
            Vector3 direction = separation / Mathf.Max(1f, actualRange);
            float cone = now - lastSeen < 12f ? -0.98f : -0.35f;
            Visible = actualRange < detectionRange && Vector3.Dot(self.transform.forward, direction) > cone;
            if (Visible && EnvironmentVisibility.IsObscured(self.transform.position, target.transform.position)) Visible = false;
            // Start beyond our own airframe. Only the nearest hit can block visual contact.
            if (Visible && actualRange > 35f && Physics.Raycast(self.transform.position + direction * 18f,
                    direction, out RaycastHit hit, actualRange - 28f, obstructionMask, QueryTriggerInteraction.Ignore))
                Visible = hit.rigidbody == target.Body;
            if (Visible)
            {
                Vector3 newVelocity = target.Body.linearVelocity;
                if (now - lastSeen < 1f && now - lastSeen > 0.001f && Velocity.sqrMagnitude > 100f)
                {
                    float observedRate = Vector3.SignedAngle(Vector3.ProjectOnPlane(Velocity, Vector3.up),
                        Vector3.ProjectOnPlane(newVelocity, Vector3.up), Vector3.up) * Mathf.Deg2Rad / (now - lastSeen);
                    turnRate = Mathf.Lerp(turnRate, Mathf.Clamp(observedRate, -0.25f, 0.25f), 0.45f);
                }
                else turnRate = 0f;
                lastSeen = now;
                lastPosition = target.transform.position;
                Velocity = newVelocity;
            }
            HasTrack = now - lastSeen < 12f;
            Position = lastPosition + Velocity * Mathf.Min(12f, Mathf.Max(0f, now - lastSeen));
            Range = Vector3.Distance(Position, self.transform.position);
            ClosingSpeed = -Vector3.Dot(Velocity - self.Body.linearVelocity, direction);
            ThreatBehind = Visible && actualRange < 800f && Vector3.Dot(self.transform.forward, direction) < -0.35f
                && Vector3.Dot(target.transform.forward, -direction) > 0.8f;
        }
    }
}
