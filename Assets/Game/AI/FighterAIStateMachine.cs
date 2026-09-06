namespace PacificCombat
{
    public enum FighterDifficulty { Rookie, Regular, Veteran, Ace }
    public enum FighterAIState { Patrol, Search, Intercept, Attack, Pursuit, DefensiveTurn,
        OvershootRecovery, RegainEnergy, Reposition, Damaged, Disengage, Dead }
    public enum FighterAIManeuver { LeadPursuit, LagPursuit, BreakTurn, Scissors, HighYoYo,
        LowYoYo, EnergyExtension, ClimbingReposition, TerrainRecovery, LevelFlight }

    public sealed class FighterAIStateMachine
    {
        public FighterAIState State { get; private set; } = FighterAIState.Patrol;
        public float EnteredAt { get; private set; }
        public void Set(FighterAIState next, float now)
        {
            if (State == next) return;
            State = next;
            EnteredAt = now;
        }
        public float Duration(float now) => now - EnteredAt;
    }
}
