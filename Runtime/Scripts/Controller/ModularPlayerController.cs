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
            base.ControllerUpdate();

            foreach (var extension in m_extensions)
            {
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