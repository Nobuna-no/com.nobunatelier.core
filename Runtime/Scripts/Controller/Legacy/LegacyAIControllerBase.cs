using NaughtyAttributes;

namespace NobunAtelier
{
    public abstract class LegacyAIControllerBase : LegacyCharacterControllerBase
    {
        public override bool IsAI => true;

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void EnableAI()
        {
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void DisableAI()
        {
        }
    }
}