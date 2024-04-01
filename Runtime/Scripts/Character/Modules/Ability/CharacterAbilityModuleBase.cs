namespace NobunAtelier
{
    public abstract class CharacterAbilityModuleBase : CharacterModuleBase
    {
        internal void AbilityUpdate(float deltaTime)
        {
            OnAbilityUpdate(deltaTime);
        }

        protected virtual void OnAbilityUpdate(float deltaTime)
        { }
    }
}