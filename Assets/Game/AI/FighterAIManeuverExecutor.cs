using UnityEngine;

namespace PacificCombat
{
    public enum ManeuverPhase { Idle, Entry, HalfLoop, InvertedRoll, RollingArc, ReverseRoll, Recover, Complete, Aborted }

    /// <summary>Finite virtual-pilot sequences. Nothing here changes an aircraft transform or Rigidbody state.</summary>
    public sealed class FighterAIManeuverExecutor
    {
        public FighterAIManeuver Kind { get; private set; }
        public ManeuverPhase Phase { get; private set; }
        public string AbortReason { get; private set; } = "";
        public bool Active => Phase != ManeuverPhase.Idle && Phase != ManeuverPhase.Complete && Phase != ManeuverPhase.Aborted;
        public float PitchTravel { get; private set; }
        public float RollTravel { get; private set; }
        float elapsed, phaseTime, phaseRoll;
        Vector3 entryHeading;
        int direction = 1;

        public bool Begin(FighterAIManeuver kind, AircraftController aircraft, float groundHeight, int turnDirection = 1)
        {
            if (Active || aircraft == null || aircraft.Data == null || aircraft.IsDestroyed) return false;
            if (kind != FighterAIManeuver.Immelmann && kind != FighterAIManeuver.SplitS
                && kind != FighterAIManeuver.RollingScissors && kind != FighterAIManeuver.BarrelRollDefense) return false;
            float speed = aircraft.Body.linearVelocity.magnitude;
            float clearance = aircraft.transform.position.y - groundHeight;
            if (aircraft.EngineHealth < 0.45f || aircraft.ControlHealth < 0.65f
                || Mathf.Min(aircraft.LeftWingHealth, aircraft.RightWingHealth) < 0.65f) return false;
            if (clearance < (kind == FighterAIManeuver.SplitS ? 1700f : 700f)) return false;
            float entrySpeed = kind == FighterAIManeuver.Immelmann ? Mathf.Max(125f, aircraft.Data.StallReferenceSpeed * 2.6f)
                : aircraft.Data.StallReferenceSpeed * 1.65f;
            if (speed < entrySpeed || speed > aircraft.Data.MaximumSafeDiveSpeed * 0.92f) return false;
            // A Split-S trades altitude for speed; reject an entry that leaves no dive margin.
            if (kind == FighterAIManeuver.SplitS && speed > aircraft.Data.MaximumSafeDiveSpeed * 0.68f) return false;
            Kind = kind;
            elapsed = phaseTime = phaseRoll = PitchTravel = RollTravel = 0f;
            entryHeading = Vector3.ProjectOnPlane(aircraft.transform.forward, Vector3.up).normalized;
            direction = turnDirection < 0 ? -1 : 1;
            AbortReason = "";
            SetPhase(ManeuverPhase.Entry);
            return true;
        }

        public void Abort(string reason)
        {
            if (!Active) return;
            AbortReason = reason;
            SetPhase(ManeuverPhase.Aborted);
        }

        public bool Step(AircraftController aircraft, float dt, float groundHeight, out FlightControls controls)
        {
            controls = default;
            if (!Active) return false;
            elapsed += dt;
            phaseTime += dt;
            Transform frame = aircraft.transform;
            Vector3 rates = frame.InverseTransformDirection(aircraft.Body.angularVelocity);
            float speed = aircraft.Body.linearVelocity.magnitude;
            float clearance = frame.position.y - groundHeight;
            if (elapsed > 38f) Abort("Maneuver time limit");
            else if (aircraft.IsDestroyed || aircraft.ControlHealth < 0.45f || aircraft.EngineHealth < 0.2f
                || Mathf.Min(aircraft.LeftWingHealth, aircraft.RightWingHealth) < 0.45f) Abort("Airframe damage");
            else if (clearance < 300f + Mathf.Max(0f, -aircraft.Body.linearVelocity.y) * 6f) Abort("Terrain clearance");
            else if (speed < aircraft.Data.StallReferenceSpeed * 1.08f) Abort("Insufficient energy");
            else if (speed > aircraft.Data.MaximumSafeDiveSpeed * 0.98f) Abort("Dive limit");
            if (!Active) return false;
            float pitchStep = Mathf.Max(0f, -rates.x * dt);
            float rollStep = Mathf.Abs(rates.z * dt);
            PitchTravel += pitchStep;
            RollTravel += rollStep;
            int commandedRollDirection = Phase == ManeuverPhase.ReverseRoll ? -direction : direction;
            phaseRoll += Mathf.Max(0f, -rates.z * commandedRollDirection * dt);
            controls = new FlightControls { Throttle = Kind == FighterAIManeuver.SplitS ? 0.25f : 1f };
            if (Phase == ManeuverPhase.Entry)
            {
                controls = Level(aircraft, entryHeading);
                if (Mathf.Abs(frame.right.y) < 0.12f && Vector3.Dot(frame.up, Vector3.up) > 0.9f)
                {
                    if (Kind == FighterAIManeuver.SplitS) SetPhase(ManeuverPhase.InvertedRoll);
                    else if (Kind == FighterAIManeuver.Immelmann) { PitchTravel = 0f; SetPhase(ManeuverPhase.HalfLoop); }
                    else SetPhase(ManeuverPhase.RollingArc);
                }
            }
            else if (Phase == ManeuverPhase.HalfLoop)
            {
                controls.Pitch = Pull(aircraft, Mathf.Min(4.5f, aircraft.Data.MaximumStructuralG * 0.67f));
                controls.Roll = Mathf.Clamp(rates.z * 0.35f, -0.3f, 0.3f);
                if (Kind == FighterAIManeuver.Immelmann && PitchTravel > Mathf.PI * 0.86f && Mathf.Abs(frame.forward.y) < 0.45f)
                    SetPhase(ManeuverPhase.InvertedRoll);
                else if (Kind == FighterAIManeuver.SplitS && PitchTravel > Mathf.PI * 0.86f
                    && Vector3.Dot(frame.up, Vector3.up) > 0.65f && frame.forward.y > -0.25f)
                    SetPhase(ManeuverPhase.Recover);
            }
            else if (Phase == ManeuverPhase.InvertedRoll)
            {
                controls.Roll = direction * 0.9f;
                controls.Pitch = Pull(aircraft, 0.2f);
                if (Kind == FighterAIManeuver.SplitS && phaseRoll >= Mathf.PI * .96f)
                { PitchTravel = 0f; SetPhase(ManeuverPhase.HalfLoop); }
                else if (Kind == FighterAIManeuver.Immelmann && Vector3.Dot(frame.up, Vector3.up) > 0.96f && phaseRoll > 2f)
                    SetPhase(ManeuverPhase.Recover);
            }
            else if (Phase == ManeuverPhase.RollingArc || Phase == ManeuverPhase.ReverseRoll)
            {
                controls.Roll = direction * (Phase == ManeuverPhase.ReverseRoll ? -0.8f : 0.8f);
                controls.Pitch = Pull(aircraft, Kind == FighterAIManeuver.BarrelRollDefense ? 2.2f : 2.6f);
                controls.Throttle = Kind == FighterAIManeuver.RollingScissors ? 0.8f : 1f;
                if (phaseRoll >= 2f * Mathf.PI)
                {
                    if (Kind == FighterAIManeuver.RollingScissors && Phase == ManeuverPhase.RollingArc)
                        SetPhase(ManeuverPhase.ReverseRoll);
                    else SetPhase(ManeuverPhase.Recover);
                }
            }
            else if (Phase == ManeuverPhase.Recover)
            {
                Vector3 heading = Kind == FighterAIManeuver.Immelmann || Kind == FighterAIManeuver.SplitS ? -entryHeading
                    : Vector3.ProjectOnPlane(frame.forward, Vector3.up).normalized;
                controls = Level(aircraft, heading);
                if (phaseTime > 0.5f && Vector3.Dot(frame.up, Vector3.up) > 0.93f && Mathf.Abs(frame.forward.y) < 0.18f
                    && aircraft.Body.angularVelocity.magnitude < 0.5f) SetPhase(ManeuverPhase.Complete);
            }
            controls.Fire = false;
            controls.Gear = controls.Flaps = false;
            return true;
        }

        static FlightControls Level(AircraftController aircraft, Vector3 heading)
            => FighterAIFlightControl.Fly(aircraft, aircraft.transform.position + heading * 1800f,
                1f, true, aircraft.Data.StallReferenceSpeed);

        static float Pull(AircraftController aircraft, float load)
        {
            AircraftData data = aircraft.Data;
            float speed = aircraft.Body.linearVelocity.magnitude;
            float density = 1.225f * Mathf.Exp(-Mathf.Max(0f, aircraft.transform.position.y) / 8500f);
            float cl = load * data.Mass * 9.81f / Mathf.Max(1f, 0.5f * density * speed * speed * data.WingArea);
            float desiredAoA = Mathf.Clamp((cl - data.TrimLiftCoefficient) / data.LiftSlope * Mathf.Rad2Deg, -4f, data.StallAngle - 3f);
            float aoa = aircraft.Physics.AngleOfAttack;
            float compression = 1f - data.HighSpeedControlLoss * Mathf.InverseLerp(data.MaximumRecommendedSpeed * 0.8f, data.MaximumSafeDiveSpeed, speed);
            float trim = aoa * Mathf.Deg2Rad * 1.8f / Mathf.Max(0.1f, data.PitchAuthority * compression * aircraft.ControlHealth);
            float pitch = trim + (desiredAoA - aoa) * 0.09f;
            if (aircraft.Physics.GForce > data.MaximumStructuralG * 0.83f) pitch = Mathf.Min(pitch, 0f);
            if (aoa > data.StallAngle - 1f) pitch = Mathf.Min(pitch, -0.15f);
            return Mathf.Clamp(pitch, -0.5f, 0.85f);
        }

        void SetPhase(ManeuverPhase phase) { Phase = phase; phaseTime = phaseRoll = 0f; }
    }
}
