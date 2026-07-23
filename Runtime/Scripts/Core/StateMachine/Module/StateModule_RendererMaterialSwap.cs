using System;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Swaps renderer materials on state enter and restores them on exit.
    /// Jam-friendly alternative to shader lerps; pair with a dedicated statue material asset.
    /// </summary>
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Renderer Material Swap")]
    public class StateModule_RendererMaterialSwap : StateComponentModule
    {
        [Serializable]
        private struct RendererSwap
        {
            public Renderer Renderer;
            [Tooltip("Optional override. Falls back to the module default statue material.")]
            public Material StatueMaterial;
        }

        [SerializeField] private Character m_Character;
        [SerializeField] private Transform m_RendererRoot;
        [SerializeField] private Material m_DefaultStatueMaterial;
        [SerializeField] private RendererSwap[] m_RendererSwaps;
        [SerializeField] private bool m_AutoCollectRenderersOnEnter = true;

        private readonly List<ActiveSwap> m_ActiveSwaps = new List<ActiveSwap>();

        private struct ActiveSwap
        {
            public Renderer Renderer;
            public Material[] SavedMaterials;
        }

        public override void Enter()
        {
            m_ActiveSwaps.Clear();

            if (m_DefaultStatueMaterial == null)
            {
                Debug.LogWarning($"[{nameof(StateModule_RendererMaterialSwap)}] No default statue material on {gameObject.name}", this);
                return;
            }

            var swaps = ResolveSwaps();
            for (int i = 0; i < swaps.Length; i++)
            {
                var renderer = swaps[i].Renderer;
                if (renderer == null)
                {
                    continue;
                }

                var statueMaterial = swaps[i].StatueMaterial != null
                    ? swaps[i].StatueMaterial
                    : m_DefaultStatueMaterial;

                var savedMaterials = renderer.sharedMaterials;
                var replacementMaterials = new Material[savedMaterials.Length];
                for (int slot = 0; slot < savedMaterials.Length; slot++)
                {
                    replacementMaterials[slot] = statueMaterial;
                }

                m_ActiveSwaps.Add(new ActiveSwap
                {
                    Renderer = renderer,
                    SavedMaterials = savedMaterials,
                });

                renderer.sharedMaterials = replacementMaterials;
            }
        }

        public override void Exit()
        {
            for (int i = 0; i < m_ActiveSwaps.Count; i++)
            {
                var swap = m_ActiveSwaps[i];
                if (swap.Renderer != null && swap.SavedMaterials != null)
                {
                    swap.Renderer.sharedMaterials = swap.SavedMaterials;
                }
            }

            m_ActiveSwaps.Clear();
        }

        private RendererSwap[] ResolveSwaps()
        {
            if (m_RendererSwaps != null && m_RendererSwaps.Length > 0)
            {
                return m_RendererSwaps;
            }

            if (!m_AutoCollectRenderersOnEnter)
            {
                return Array.Empty<RendererSwap>();
            }

            var root = m_RendererRoot;
            if (root == null && m_Character != null)
            {
                root = m_Character.transform;
            }

            if (root == null)
            {
                root = transform;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var swaps = new RendererSwap[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                swaps[i] = new RendererSwap { Renderer = renderers[i] };
            }

            return swaps;
        }

        private void Reset()
        {
            m_Character = GetComponentInParent<Character>();
            if (m_Character != null)
            {
                m_RendererRoot = m_Character.transform;
            }
        }
    }
}
