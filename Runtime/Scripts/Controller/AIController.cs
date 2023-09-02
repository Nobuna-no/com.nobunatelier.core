using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/AIController")]
    public class AIController : CharacterControllerBase<AIControllerModuleBase>
    {
        public override bool IsAI => true;

        public override void EnableInput()
        {
            foreach (var extension in m_modules)
            {
                extension.EnableAIModule();
            }
        }

        public override void DisableInput()
        {
            foreach (var extension in m_modules)
            {
                extension.DisableAIModule();
            }
        }
    }
}