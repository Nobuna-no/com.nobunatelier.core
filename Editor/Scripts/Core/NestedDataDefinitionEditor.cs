using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    public abstract class NestedDataDefinitionEditor<CustomEditorT> : UnityEditor.Editor
        where CustomEditorT : DataDefinition
    {
        public abstract IReadOnlyList<DataDefinition> TargetDefinitions { get; }
        private List<UnityEditor.Editor> m_nestedEditors;
        private List<bool> m_expandNestedEditor;
        private List<DataDefinition> m_activeTargetDefinition;
        private bool m_hasAnyTarget = false;

        protected virtual bool IsDatasetDirty()
        {
            return TargetDefinitions.Count != m_nestedEditors.Count;
        }

        protected virtual void OnEnable()
        {
            RefreshDataset();
        }

        protected virtual void OnDisable()
        {
            m_hasAnyTarget = false;
            m_activeTargetDefinition.Clear();
            m_nestedEditors.Clear();
            m_expandNestedEditor.Clear();
        }

        protected virtual void RefreshDataset()
        {
            m_hasAnyTarget = TargetDefinitions.Count > 0;
            m_activeTargetDefinition = new List<DataDefinition>(TargetDefinitions.Count);
            m_nestedEditors = new List<UnityEditor.Editor>(TargetDefinitions.Count);
            m_expandNestedEditor = new List<bool>(TargetDefinitions.Count);

            for (int i = 0; i < TargetDefinitions.Count; i++)
            {
                m_activeTargetDefinition.Add(null);
                m_nestedEditors.Add(null);
                m_expandNestedEditor.Add(false);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!m_hasAnyTarget)
            {
                // EditorGUILayout.HelpBox($"NestedDefinitionEditor: No nested definition specified.", MessageType.Warning);
                return;
            }

            if (IsDatasetDirty())
            {
                RefreshDataset();
            }

            for (int i = 0; i < TargetDefinitions.Count; i++)
            {
                DataDefinition item = TargetDefinitions[i];
                EditorGUILayout.Separator();

                if (item == null)
                {
                    continue;
                }

                if (m_nestedEditors[i] == null || m_activeTargetDefinition[i] != item)
                {
                    m_nestedEditors[i] = UnityEditor.Editor.CreateEditor(item);
                    m_activeTargetDefinition[i] = item;
                }

                using (new EditorGUILayout.VerticalScope(GUI.skin.window))
                {
                    m_expandNestedEditor[i] = EditorGUILayout.InspectorTitlebar(m_expandNestedEditor[i], item);
                    if (m_expandNestedEditor[i])
                    {
                        EditorGUI.indentLevel++;
                        m_nestedEditors[i].OnInspectorGUI();
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
    }
}
