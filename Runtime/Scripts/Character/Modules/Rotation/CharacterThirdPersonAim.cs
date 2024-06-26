using Unity.Cinemachine;
using UnityEngine;

namespace NobunAtelier
{
    public class CharacterThirdPersonAim : CharacterRotationModuleBase
    {
        public enum CouplingMode
        { Coupled, CoupledWhenMoving, Decoupled }

        [Tooltip("How the player's rotation is coupled to the camera's rotation.  Three modes are available:\n"
            + "<b>Coupled</b>: The player rotates with the camera.  Sideways movement will result in strafing.\n"
            + "<b>Coupled When Moving</b>: Camera can rotate freely around the player when the player is stationary, "
                + "but the player will rotate to face camera forward when it starts moving.\n"
            + "<b>Decoupled</b>: The player's rotation is independent of the camera's rotation.")]
        public CouplingMode PlayerRotation;

        [Tooltip("How fast the player rotates to face the camera direction when the player starts moving.  "
            + "Only used when Player Rotation is Coupled When Moving.")]
        public float RotationDamping = 0.2f;

        private Quaternion m_DesiredWorldRotation;

        private CharacterGaitVelocity m_GaitVelocity;

        private Vector3 m_lastLookDir;

        private Transform m_aimProxy;

        [SerializeField] private Transform m_cameraTarget;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);
            var proxyGao = new GameObject("Proxy Test");
            proxyGao.transform.parent = ModuleOwner.Transform;
            m_aimProxy = proxyGao.transform;

            ModuleOwner.TryGetVelocityModule(out m_GaitVelocity);

            ModuleOwner.OnPostUpdate += PostUpdate;
        }


        public override void RotateInput(Vector3 normalizedDirection)
        {
            m_lastLookDir = normalizedDirection;
        }

        public override void RotationUpdate(float deltaTime)
        {
            var t = transform;
            t.localRotation = Quaternion.Euler(m_lastLookDir.x, m_lastLookDir.y, 0);
            m_DesiredWorldRotation = t.rotation;
            switch (PlayerRotation)
            {
                case CouplingMode.Coupled:
                    {
                        if (m_GaitVelocity)
                        {
                            m_GaitVelocity.Strafe = true;
                        }

                        RecenterPlayer();
                        break;
                    }
                case CouplingMode.CoupledWhenMoving:
                    {
                        // If the player is moving, rotate its yaw to match the camera direction,
                        // otherwise let the camera orbit
                        if (m_GaitVelocity)
                        {
                            m_GaitVelocity.Strafe = true;
                        }

                        if (ModuleOwner.GetMoveSpeed() > 0) // if moving
                            RecenterPlayer(RotationDamping);
                        break;
                    }
                case CouplingMode.Decoupled:
                    {
                        if (m_GaitVelocity)
                        {
                            m_GaitVelocity.Strafe = true;
                        }
                        break;
                    }
            }
        }


        /// <summary>Recenters the player to match my rotation</summary>
        /// <param name="damping">How long the recentering should take</param>
        public void RecenterPlayer(float damping = 0)
        {
            // Get my rotation relative to parent
            var rot = m_aimProxy.localRotation.eulerAngles;
            rot.y = NormalizeAngle(rot.y);
            var delta = rot.y;
            delta = Damper.Damp(delta, damping, Time.deltaTime);

            // Rotate the parent towards me
            ModuleOwner.Transform.rotation = Quaternion.AngleAxis(
                delta, ModuleOwner.Transform.up) * ModuleOwner.Transform.rotation;

            // Rotate me in the opposite direction
            // HorizontalLook.Value -= delta;
            rot.y -= delta;
            m_aimProxy.localRotation = Quaternion.Euler(rot);
        }

        // Callback for player controller to update our rotation after it has updated its own.
        private void PostUpdate(/*Vector3 vel, float speed*/)
        {
            if (PlayerRotation == CouplingMode.Decoupled)
            {
                // After player has been rotated, we subtract any rotation change
                // from our own transform, to maintain our world rotation
                m_aimProxy.rotation = m_DesiredWorldRotation;
                // var delta = (Quaternion.Inverse(m_ControllerTransform.rotation) * m_DesiredWorldRotation).eulerAngles;
                // VerticalLook.Value = NormalizeAngle(delta.x);
                // HorizontalLook.Value = NormalizeAngle(delta.y);
            }
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180)
                angle -= 360;
            while (angle < -180)
                angle += 360;
            return angle;
        }
    }
}
