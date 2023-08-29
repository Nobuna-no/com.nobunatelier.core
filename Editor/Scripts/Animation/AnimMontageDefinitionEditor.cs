using UnityEditor;
using UnityEngine;

namespace NobunAtelier
{
    [CustomEditor(typeof(AnimMontageDefinition))]
    public class AnimMontageDefinitionInspectorDrawer : Editor
    {
        private Editor m_sequenceEditor;
        private AnimSequenceDefinition m_previousAnimSequence;
        private bool m_showAnimSequenceEditor = true;

        //public override void OnInspectorGUI()
        //{
        //    DrawDefaultInspector();

        //    AnimMontageDefinition montageDefinition = (AnimMontageDefinition)target;

        //    EditorGUILayout.Separator();

        //    if (montageDefinition.AnimSequence != null)
        //    {
        //        if (m_sequenceEditor == null || m_previousAnimSequence != montageDefinition.AnimSequence)
        //        {
        //            m_sequenceEditor = Editor.CreateEditor(montageDefinition.AnimSequence);
        //            m_previousAnimSequence = montageDefinition.AnimSequence;
        //        }

        //        using (new EditorGUILayout.VerticalScope(GUI.skin.window))
        //        {
        //            m_showAnimSequenceEditor = EditorGUILayout.InspectorTitlebar(m_showAnimSequenceEditor, m_previousAnimSequence);
        //            if (m_showAnimSequenceEditor)
        //            {
        //                EditorGUI.indentLevel++;
        //                m_sequenceEditor.OnInspectorGUI();
        //                EditorGUI.indentLevel--;
        //            }
        //        }
        //    }
        //}
    }
}