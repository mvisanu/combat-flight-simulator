using UnityEngine;

namespace PacificCombat
{
    /// <summary>Reusable force surface. Surface up is the lift normal and forward is the chord.</summary>
    public sealed class AerodynamicSurface : MonoBehaviour
    {
        public float Area = 10f;
        [Range(0, 1)] public float DamageMultiplier = 1f;
        public float LiftMultiplier = 1f;
        public float DragMultiplier = 1f;
        public float LastLift { get; private set; }

        public Vector3 Apply(Rigidbody body, float density, float liftCoefficient, float dragCoefficient)
        {
            Vector3 velocity = body.GetPointVelocity(transform.position) - PacificEnvironment.WindVelocity(transform.position);
            float speedSquared = velocity.sqrMagnitude;
            if (speedSquared < 1f) { LastLift = 0f; return Vector3.zero; }
            Vector3 direction = velocity / Mathf.Sqrt(speedSquared);
            Vector3 normal = Vector3.ProjectOnPlane(transform.up, direction).normalized;
            float qArea = .5f * density * speedSquared * Area;
            LastLift = qArea * liftCoefficient * DamageMultiplier * LiftMultiplier;
            Vector3 force = normal * LastLift - direction * (qArea * dragCoefficient * DragMultiplier);
            body.AddForceAtPosition(force, transform.position, ForceMode.Force);
            return force;
        }
    }
}
