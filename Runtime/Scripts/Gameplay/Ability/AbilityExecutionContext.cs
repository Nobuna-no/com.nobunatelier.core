namespace NobunAtelier
{
    /// <summary>
    /// Lightweight context passed from the Moveset layer to the Ability layer on TryExecute.
    /// Provides combo state that modules can query at runtime without coupling to MovesetController.
    /// </summary>
    public readonly struct AbilityExecutionContext
    {
        public readonly int ComboStep;
        public readonly int PathIndex;

        public AbilityExecutionContext(int comboStep, int pathIndex)
        {
            ComboStep = comboStep;
            PathIndex = pathIndex;
        }
    }
}
