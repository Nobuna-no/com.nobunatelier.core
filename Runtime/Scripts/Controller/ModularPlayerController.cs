using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class ModularPlayerController : PlayerController
    {
        [SerializeField]
        private PlayerControllerModule[] m_extensions;

        protected override void Awake()
        {
            base.Awake();

            foreach(var extension in m_extensions)
            {
                extension.PlayerControllerExtensionInit(this);
            }
        }

        public override void EnableInput()
        {
            Debug.Assert(m_controlledCharacter, "Enabling input but not character controlled!");

            base.EnableInput();

            foreach (var extension in m_extensions)
            {
                extension.PlayerControllerExtensionEnableInput(PlayerInput, ActiveActionMap);
            }
        }

        public override void DisableInput()
        {
            base.DisableInput();

            foreach (var extension in m_extensions)
            {
                extension.PlayerControllerExtensionDisableInput(PlayerInput, ActiveActionMap);
            }
        }

        protected override void ControllerUpdate()
        {
            if (m_controlledCharacter == null)
            {
                return;
            }

            foreach (var extension in m_extensions)
            {
                if (!extension.CanBeEvaluated())
                {
                    continue;
                }

                extension.PlayerControllerExtensionUpdate(Time.deltaTime);
            }
        }

        private void OnEnable()
        {
            EnableInput();

            foreach (var extension in m_extensions)
            {
                extension.enabled = true;
            }
        }

        private void OnDisable()
        {
            DisableInput();

            foreach (var extension in m_extensions)
            {
                extension.enabled = false;
            }
        }
    }
}