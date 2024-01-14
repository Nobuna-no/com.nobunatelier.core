using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/Ability/AbilityModule: Event")]
    public class CharacterEventModule : CharacterAbilityModuleBase
    {
        public UnityEvent OnModuleInit;
        public UnityEvent OnModuleReset;
        public UnityEvent OnModuleStop;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            OnModuleInit?.Invoke();
        }

        public override void Reset()
        {
            OnModuleReset?.Invoke();
        }

        public override void ModuleStop()
        {
            OnModuleStop?.Invoke();
        }
    }
}