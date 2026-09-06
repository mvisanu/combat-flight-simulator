using UnityEngine;

namespace PacificCombat
{
    [System.Serializable]
    public struct FlightControls
    {
        public float Pitch, Roll, Yaw, Throttle;
        public bool Fire, Flaps, Gear, Brakes;
    }

    [RequireComponent(typeof(Rigidbody), typeof(AircraftPhysics), typeof(AircraftEngine))]
    public sealed class AircraftController : MonoBehaviour
    {
        public AircraftData Data;
        public Rigidbody Body;
        public AircraftPhysics Physics;
        public AircraftEngine Engine;
        public FlightControls Controls;
        public bool IsPlayer;
        public int Team;
        [Range(0, 1)] public float EngineHealth = 1f, LeftWingHealth = 1f, RightWingHealth = 1f, ControlHealth = 1f;
        public bool IsDestroyed;
        public float GForce => Physics != null ? Physics.GForce : 1f;
        public float Airspeed => Physics != null ? Physics.Airspeed : 0f;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Physics = GetComponent<AircraftPhysics>();
            Engine = GetComponent<AircraftEngine>();
        }

        public void Initialize(AircraftData data, bool isPlayer, int team, float speed)
        {
            Data = data;
            IsPlayer = isPlayer;
            Team = team;
            Body = GetComponent<Rigidbody>();
            Physics = GetComponent<AircraftPhysics>();
            Engine = GetComponent<AircraftEngine>();
            Body.mass = data.Mass;
            Body.useGravity = true;
            Body.linearDamping = 0f;
            Body.angularDamping = .05f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxAngularVelocity = 4f;
            Body.centerOfMass = Vector3.zero;
            // Explicit inertia keeps visual mesh/collider revisions from changing handling.
            Body.inertiaTensorRotation = Quaternion.identity;
            Body.inertiaTensor = data.Mass * new Vector3(5f, 7f, 4f);
            Body.linearVelocity = transform.forward * speed;
            Body.angularVelocity = Vector3.zero;
            Controls = new FlightControls { Throttle = .78f };
            Engine.Initialize(this);
            Physics.Initialize(this);
        }

        void FixedUpdate()
        {
            if (Data == null) return;
            Controls.Pitch = Mathf.Clamp(Controls.Pitch, -1f, 1f);
            Controls.Roll = Mathf.Clamp(Controls.Roll, -1f, 1f);
            Controls.Yaw = Mathf.Clamp(Controls.Yaw, -1f, 1f);
            Controls.Throttle = Mathf.Clamp01(Controls.Throttle);
            if (IsDestroyed) Controls = default;
            Engine.Simulate(Time.fixedDeltaTime);
            Physics.Simulate(Time.fixedDeltaTime);
        }
    }
}
