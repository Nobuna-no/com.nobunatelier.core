using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    [CustomEditor(typeof(AttackAnimationDefinition))]
    public class AttackAnimationDefinitionInspectorDrawer : UnityEditor.Editor
    {
        private UnityEditor.Editor m_sequenceEditor;
        private AnimMontageDefinition m_previousAnimSequence;
        private bool m_showAnimSequenceEditor = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AttackAnimationDefinition attackDefinition = (AttackAnimationDefinition)target;

            EditorGUILayout.Separator();

            if (attackDefinition.AnimMontage != null)
            {
                if (m_sequenceEditor == null || m_previousAnimSequence != attackDefinition.AnimMontage)
                {
                    m_sequenceEditor = UnityEditor.Editor.CreateEditor(attackDefinition.AnimMontage);
                    m_previousAnimSequence = attackDefinition.AnimMontage;
                }

                using (new EditorGUILayout.VerticalScope(GUI.skin.window))
                {
                    m_showAnimSequenceEditor = EditorGUILayout.InspectorTitlebar(m_showAnimSequenceEditor, m_previousAnimSequence);
                    if (m_showAnimSequenceEditor)
                    {
                        EditorGUI.indentLevel++;
                        m_sequenceEditor.OnInspectorGUI();
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
    }
}