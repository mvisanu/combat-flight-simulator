using UnityEngine;

namespace PacificCombat
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    public sealed class FighterAIController : MonoBehaviour
    {
        [SerializeField] FighterDifficulty difficulty = FighterDifficulty.Regular;
        [SerializeField, Min(1000f)] float detectionRange = 6500f;
        [SerializeField, Min(0.08f)] float decisionInterval = 0.16f;
        [SerializeField, Min(100f)] float muzzleVelocity = 680f;
        [SerializeField, Min(50f)] float minimumClearance = 240f;
        [SerializeField] LayerMask visibilityMask = ~0;
        [SerializeField] LayerMask terrainMask = ~0;
        [SerializeField] bool drawDebug;
        readonly FighterAIPerception perception = new FighterAIPerception();
        readonly FighterAIStateMachine machine = new FighterAIStateMachine();
        readonly FighterAIManeuverExecutor maneuverExecutor = new FighterAIManeuverExecutor();
        float nextAdvancedManeuver;
        AircraftController target;
        int squadIndex;
        float nextDecision;
        float elapsed;
        float patrolAltitude;
        float terrainHeight;
        float stallSpeed = 30f;
        float cruiseSpeed = 145f;
        Vector3 waypoint;
        Vector3 aimPoint;
        float throttle = 1f;
        bool terrainRecovery;

        public AircraftController Aircraft { get; private set; }
        public int SquadIndex => squadIndex;
        public AircraftController Target => target;
        public float CombatTime => elapsed;
        public SquadronRole Role { get; private set; }
        public FighterAIState State => machine.State;
        public FighterAIManeuver Maneuver { get; private set; }
        public FighterDifficulty Difficulty { get => difficulty; set => difficulty = value; }
        public float MuzzleVelocity { get => muzzleVelocity; set => muzzleVelocity = Mathf.Max(100f, value); }
        public Vector3 AimPoint => aimPoint;
        public bool TargetVisible => perception.Visible;
        public ManeuverPhase ManeuverPhase => maneuverExecutor.Phase;
        public bool ManeuverActive => maneuverExecutor.Active;
        public string LastManeuverAbort => maneuverExecutor.AbortReason;
        public float CombatEnergy => Aircraft != null ? FighterAITactics.SpecificEnergy(Aircraft) : 0f;

        public bool RequestManeuver(FighterAIManeuver kind)
        {
            if (!maneuverExecutor.Begin(kind, Aircraft, terrainHeight, (squadIndex & 1) == 0 ? 1 : -1)) return false;
            nextAdvancedManeuver = elapsed + 45f;
            return true;
        }

        public void ShiftOrigin(Vector3 offset)
        {
            waypoint -= offset;
            aimPoint -= offset;
            patrolAltitude -= offset.y;
            terrainHeight -= offset.y;
            perception.ShiftOrigin(offset);
        }

        public void Initialize(AircraftController self, AircraftController enemy, int index)
        {
            Aircraft = self;
            target = enemy;
            squadIndex = index;
            if (self.Data != null)
            {
                stallSpeed = self.Data.StallReferenceSpeed;
                cruiseSpeed = self.Data.MaximumRecommendedSpeed;
            }
            AircraftWeaponSystem weapons = self.GetComponent<AircraftWeaponSystem>();
            if (weapons != null && weapons.Configuration != null)
            {
                WeaponData guns = weapons.Configuration;
                int cannonCount = guns.MixedCannon ? guns.CannonCount : 0;
                float machineRate = Mathf.Max(0, guns.GunCount - cannonCount) * guns.RoundsPerMinutePerGun;
                float cannonRate = cannonCount * guns.CannonRoundsPerMinute;
                muzzleVelocity = (guns.MuzzleVelocity * machineRate + guns.CannonMuzzleVelocity * cannonRate)
                    / Mathf.Max(1f, machineRate + cannonRate);
            }
            patrolAltitude = self.transform.position.y;
            waypoint = transform.position + transform.forward * 2500f;
            nextDecision = index * 0.031f;
            SquadronController.Register(this);
        }

        void OnDisable() => SquadronController.Unregister(this);
        void OnEnable() { if (Aircraft != null) SquadronController.Register(this); }

        void FixedUpdate()
        {
            Simulate(Time.fixedDeltaTime);
        }

        /// <summary>Advance the virtual pilot once before the shared aircraft physics step.</summary>
        public void Simulate(float deltaTime)
        {
            if (Aircraft == null || Aircraft.Body == null) return;
            elapsed += deltaTime;
            if (Aircraft.IsDestroyed)
            {
                maneuverExecutor.Abort("Aircraft destroyed");
                machine.Set(FighterAIState.Dead, elapsed);
                Aircraft.Controls = default;
                return;
            }
            if (elapsed >= nextDecision)
            {
                float reaction = difficulty == FighterDifficulty.Rookie ? 1.8f : difficulty == FighterDifficulty.Ace ? 0.65f : 1f;
                nextDecision = elapsed + decisionInterval * reaction * (perception.Range > 3500f ? 1.8f : 1f);
                Think();
            }
            bool recover = terrainRecovery || State == FighterAIState.RegainEnergy || State == FighterAIState.Damaged;
            UpdateAim();
            if (!recover && (State == FighterAIState.Attack || State == FighterAIState.Pursuit))
                waypoint = aimPoint + SquadronController.Separation(this);
            Vector3 shot = aimPoint - transform.position;
            Vector3 sightLineRate = Vector3.Cross(shot, perception.Velocity - Aircraft.Body.linearVelocity) / Mathf.Max(100f, shot.sqrMagnitude);
            FlightControls controls = FighterAIFlightControl.Fly(Aircraft, waypoint, throttle, recover, stallSpeed,
                !recover && perception.HasTrack && perception.Range < 1500f,
                State == FighterAIState.Attack || State == FighterAIState.Pursuit, sightLineRate);
            float distance = shot.magnitude;
            float alignment = Vector3.Dot(transform.forward, shot / Mathf.Max(1f, distance));
            // Gate on the actual gun axis, never on desired orientation or a hidden hit probability.
            controls.Fire = !terrainRecovery && perception.Visible && State == FighterAIState.Attack
                && distance > 100f && distance < 800f && alignment > Mathf.Cos(1.8f * Mathf.Deg2Rad)
                && SquadronController.ClearFireLane(this, transform.forward, distance);
            if (terrainRecovery) maneuverExecutor.Abort("Terrain recovery has priority");
            if (maneuverExecutor.Step(Aircraft, deltaTime, terrainHeight, out FlightControls maneuverControls))
            {
                controls = maneuverControls;
                Maneuver = maneuverExecutor.Kind;
            }
            Aircraft.Controls = controls;
        }

        void UpdateAim()
        {
            float aimError = difficulty == FighterDifficulty.Rookie ? 1.5f : difficulty == FighterDifficulty.Regular ? 0.65f
                : difficulty == FighterDifficulty.Veteran ? 0.3f : 0.12f;
            aimPoint = FighterAIAiming.Intercept(transform.position, Aircraft.Body.linearVelocity,
                perception.PredictPosition(elapsed), perception.Velocity, muzzleVelocity);
            aimPoint += FighterAIAiming.AimError(elapsed, squadIndex, aimError, transform, perception.Range);
        }

        void Think()
        {
            perception.Sample(Aircraft, target, elapsed, detectionRange, visibilityMask);
            int priority = SquadronController.AttackPriority(this);
            Role = priority == 0 ? SquadronRole.Attacker : priority == 1 ? SquadronRole.Wingman : SquadronRole.Support;
            machine.Set(FighterAITactics.Decide(Aircraft, perception, machine, elapsed, stallSpeed, cruiseSpeed, difficulty), elapsed);
            UpdateAim();
            waypoint = aimPoint;
            Maneuver = FighterAIManeuver.LeadPursuit;
            throttle = 1f;
            float side = (squadIndex & 1) == 0 ? -1f : 1f;
            switch (State)
            {
                case FighterAIState.Search:
                case FighterAIState.Patrol:
                    waypoint = transform.position + transform.forward * 1800f + transform.right * side * 600f;
                    waypoint.y = patrolAltitude;
                    Maneuver = FighterAIManeuver.LevelFlight;
                    throttle = 0.85f;
                    break;
                case FighterAIState.Intercept:
                    Vector3 approachDirection = (perception.Position - transform.position).normalized;
                    float interceptClosure = Mathf.Max(40f, Aircraft.Body.linearVelocity.magnitude - Vector3.Dot(perception.Velocity, approachDirection));
                    waypoint = perception.PredictFlightPosition(elapsed,
                        Mathf.Clamp(perception.Range / interceptClosure, 0f, 12f));
                    if (target != null) waypoint += SquadronController.ApproachOffset(target.transform, squadIndex, perception.Range);
                    if (Role == SquadronRole.Support && perception.Range > 1500f)
                    {
                        Vector3 targetDirection = perception.Velocity.normalized;
                        waypoint -= targetDirection * 350f;
                        waypoint.y = Mathf.Min(waypoint.y + 80f, transform.position.y);
                        Maneuver = FighterAIManeuver.LagPursuit;
                    }
                    break;
                case FighterAIState.DefensiveTurn:
                    float weave = Mathf.Sin(machine.Duration(elapsed) * 1.2f + squadIndex);
                    waypoint = transform.position + transform.forward * 300f + transform.right * side * 1000f
                        + Vector3.up * (weave * 80f);
                    throttle = 0.7f;
                    Maneuver = FighterAIManeuver.Scissors;
                    break;
                case FighterAIState.OvershootRecovery:
                    if (Aircraft.Body.linearVelocity.magnitude > cruiseSpeed * 0.9f)
                    {
                        waypoint = transform.position + transform.forward * 800f + Vector3.up * 350f + transform.right * side * 400f;
                        throttle = 0.8f;
                        Maneuver = FighterAIManeuver.HighYoYo;
                    }
                    else
                    {
                        waypoint = transform.position + Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * 350f
                            + transform.right * side * 1000f + Vector3.up * 60f;
                        Maneuver = FighterAIManeuver.BreakTurn;
                    }
                    break;
                case FighterAIState.RegainEnergy:
                    waypoint = transform.position + Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * 2000f - Vector3.up * 500f;
                    Maneuver = FighterAIManeuver.LowYoYo;
                    break;
                case FighterAIState.Reposition:
                    waypoint = transform.position + transform.forward * 1700f + Vector3.up * 450f + transform.right * side * 800f;
                    Maneuver = FighterAIManeuver.ClimbingReposition;
                    break;
                case FighterAIState.Damaged:
                    waypoint = transform.position + Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * 2500f - Vector3.up * 220f;
                    Maneuver = FighterAIManeuver.EnergyExtension;
                    break;
            }
            waypoint += SquadronController.Separation(this);
            // Sample terrain at the projected flight path, allowing time to roll level before the pull-up.
            Vector3 ahead = transform.position + Aircraft.Body.linearVelocity * 4f;
            terrainHeight = 0f;
            if (Physics.Raycast(ahead + Vector3.up * 3000f, Vector3.down, out RaycastHit ground, 10000f,
                terrainMask, QueryTriggerInteraction.Ignore) && ground.rigidbody == null)
                terrainHeight = Mathf.Max(0f, ground.point.y);
            float sinkAllowance = Mathf.Max(0f, -Aircraft.Body.linearVelocity.y) * 5f;
            terrainRecovery = transform.position.y < terrainHeight + minimumClearance + sinkAllowance;
            if (terrainRecovery)
            {
                Vector3 flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                waypoint = transform.position + flat * 1300f;
                waypoint.y = terrainHeight + minimumClearance + 500f;
                throttle = 1f;
                Maneuver = FighterAIManeuver.TerrainRecovery;
            }
            waypoint.y = Mathf.Max(terrainHeight + minimumClearance, waypoint.y);
            if (!terrainRecovery && Aircraft.Data != null && Aircraft.Body.linearVelocity.magnitude > Aircraft.Data.MaximumSafeDiveSpeed * 0.9f)
            {
                throttle = 0.25f;
                waypoint.y = Mathf.Max(waypoint.y, transform.position.y + 400f);
                Maneuver = FighterAIManeuver.ClimbingReposition;
            }
            if (difficulty >= FighterDifficulty.Veteran && !terrainRecovery && !maneuverExecutor.Active && elapsed >= nextAdvancedManeuver)
            {
                if (State == FighterAIState.DefensiveTurn)
                    RequestManeuver((squadIndex & 1) == 0 ? FighterAIManeuver.BarrelRollDefense : FighterAIManeuver.RollingScissors);
                else if (State == FighterAIState.OvershootRecovery)
                    RequestManeuver(perception.Position.y < transform.position.y - 250f ? FighterAIManeuver.SplitS : FighterAIManeuver.Immelmann);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!drawDebug || !Application.isPlaying) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, waypoint);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aimPoint, 12f);
        }
    }
}
