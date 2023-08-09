using UnityEditor;

namespace NobunAtelier
{
    [CustomEditor(typeof(GameModeStateDefinition))]
    public class GameModeStateDefinitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Implement your custom GUI drawing logic here
            // You can use EditorGUILayout.PropertyField() to draw properties
            // You can also use EditorGUILayout.LabelField(), GUILayout.Button(), etc.

            // Call the base OnInspectorGUI to keep default drawing behavior
            base.OnInspectorGUI();
        }
    }
}