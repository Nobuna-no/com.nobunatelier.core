namespace NobunAtelier
{
    public abstract class AIControllerModuleBase : CharacterControllerModuleBase<AIController>
    {
        public abstract void EnableAIModule();

        public abstract void DisableAIModule();
    }
}