using UnityEngine;

namespace PacificCombat
{
    public static class FighterAIAiming
    {
        // Solve interception in the shooter's moving frame; projectiles inherit its velocity.
        public static Vector3 Intercept(Vector3 origin, Vector3 shooterVelocity, Vector3 targetPosition,
            Vector3 targetVelocity, float muzzleVelocity, float maxTime = 3f)
        {
            Vector3 separation = targetPosition - origin;
            Vector3 velocity = targetVelocity - shooterVelocity;
            float a = Vector3.Dot(velocity, velocity) - muzzleVelocity * muzzleVelocity;
            float b = 2f * Vector3.Dot(separation, velocity);
            float c = separation.sqrMagnitude;
            float time = Mathf.Sqrt(c) / Mathf.Max(1f, muzzleVelocity);
            float discriminant = b * b - 4f * a * c;
            if (Mathf.Abs(a) > 0.001f && discriminant >= 0f)
            {
                float root = Mathf.Sqrt(discriminant);
                float first = (-b - root) / (2f * a);
                float second = (-b + root) / (2f * a);
                if (first > 0f && second > 0f) time = Mathf.Min(first, second);
                else if (first > 0f) time = first;
                else if (second > 0f) time = second;
            }
            else if (Mathf.Abs(b) > 0.001f && Mathf.Abs(a) <= 0.001f)
                time = Mathf.Max(0f, -c / b);
            time = Mathf.Clamp(time, 0f, maxTime);
            return targetPosition + velocity * time - Physics.gravity * (0.5f * time * time);
        }

        public static Vector3 AimError(float elapsed, int index, float degrees, Transform frame, float range)
        {
            float phase = index * 2.399963f;
            float scale = Mathf.Tan(degrees * Mathf.Deg2Rad) * range;
            return (frame.right * Mathf.Sin(elapsed * 0.71f + phase)
                    + frame.up * Mathf.Sin(elapsed * 0.53f + phase * 1.7f)) * scale;
        }
    }
}
