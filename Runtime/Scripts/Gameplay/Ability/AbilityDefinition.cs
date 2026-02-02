namespace NobunAtelier
{
    public abstract class AbilityDefinition : DataDefinition
    {
        public abstract IAbilityInstance CreateAbilityInstance(AbilityController controller);
    }
}