using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    [CustomEditor(typeof(AbilityAction))]
    public class AbilityActionEditor : NaughtyInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var messages = serializedObject.FindProperty("m_ValidationMessages");
            if (messages != null && !string.IsNullOrEmpty(messages.stringValue))
            {
                EditorGUILayout.HelpBox(messages.stringValue, MessageType.Warning);
            }
        }
    }
}
