using UnityEngine;

namespace PacificCombat
{
    public static class FighterAITactics
    {
        public static float SpecificEnergy(AircraftController aircraft)
            => aircraft.transform.position.y + aircraft.Body.linearVelocity.sqrMagnitude / (2f * 9.81f);

        public static FighterAIState Decide(AircraftController self, FighterAIPerception track,
            FighterAIStateMachine machine, float now, float stallSpeed, float cruiseSpeed, FighterDifficulty difficulty)
        {
            if (self.IsDestroyed) return FighterAIState.Dead;
            if (self.EngineHealth < 0.28f || self.ControlHealth < 0.3f) return FighterAIState.Damaged;
            float speed = self.Body.linearVelocity.magnitude;
            if (speed < stallSpeed * 1.22f || (machine.State == FighterAIState.RegainEnergy && speed < stallSpeed * 1.65f))
                return FighterAIState.RegainEnergy;
            if (!track.HasTrack) return FighterAIState.Search;
            // Hysteresis allows a maneuver to develop instead of oscillating every decision tick.
            if ((machine.State == FighterAIState.DefensiveTurn || machine.State == FighterAIState.OvershootRecovery)
                && machine.Duration(now) < 3.2f) return machine.State;
            if (track.ThreatBehind && difficulty != FighterDifficulty.Rookie) return FighterAIState.DefensiveTurn;
            if (track.Range < 170f && track.ClosingSpeed > 25f) return FighterAIState.OvershootRecovery;
            if (track.Range > 1600f && speed > cruiseSpeed * 0.9f && track.ClosingSpeed < -15f)
                return FighterAIState.Reposition;
            if (track.Range > 1000f) return FighterAIState.Intercept;
            if (track.Range < 850f && track.Visible) return FighterAIState.Attack;
            return FighterAIState.Pursuit;
        }
    }
}
