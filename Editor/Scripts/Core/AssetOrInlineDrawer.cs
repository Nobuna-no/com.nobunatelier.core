using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    /// <summary>
    /// Base PropertyDrawer for dual-slot types following the AssetOrInline convention.
    /// Expects serialized fields: m_UseAsset (bool), m_Asset (SO ref), m_InlineData (inline data).
    /// Shows a gear popup to toggle between asset and inline modes.
    /// </summary>
    public class AssetOrInlineDrawer : PropertyDrawer
    {
        private static readonly string[] k_PopupOptions = { "Inline", "Asset" };
        private static GUIStyle s_PopupStyle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var useAsset = property.FindPropertyRelative("m_UseAsset");
            if (useAsset == null)
                return EditorGUI.GetPropertyHeight(property, label, true);

            if (useAsset.boolValue)
                return EditorGUIUtility.singleLineHeight;

            var inline = property.FindPropertyRelative("m_InlineData");
            if (inline == null)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing
                + EditorGUI.GetPropertyHeight(inline, GUIContent.none, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var useAsset = property.FindPropertyRelative("m_UseAsset");
            var asset = property.FindPropertyRelative("m_Asset");
            var inline = property.FindPropertyRelative("m_InlineData");

            if (useAsset == null || asset == null || inline == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (s_PopupStyle == null)
            {
                s_PopupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
                s_PopupStyle.imagePosition = ImagePosition.ImageOnly;
            }

            EditorGUI.BeginProperty(position, label, property);

            // First line: label + gear popup + asset picker (if asset mode)
            var firstLine = new Rect(position.x, position.y,
                position.width, EditorGUIUtility.singleLineHeight);

            var valueRect = EditorGUI.PrefixLabel(firstLine, label);

            // Gear popup at start of value area
            var gearRect = new Rect(valueRect);
            gearRect.yMin += s_PopupStyle.margin.top;
            gearRect.width = s_PopupStyle.fixedWidth + s_PopupStyle.margin.right;
            valueRect.xMin = gearRect.xMax;

            var savedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginChangeCheck();
            int result = EditorGUI.Popup(gearRect, useAsset.boolValue ? 1 : 0,
                k_PopupOptions, s_PopupStyle);
            if (EditorGUI.EndChangeCheck())
            {
                useAsset.boolValue = result == 1;
            }

            if (useAsset.boolValue)
            {
                EditorGUI.PropertyField(valueRect, asset, GUIContent.none);
            }

            EditorGUI.indentLevel = savedIndent;

            // Inline content below first line
            if (!useAsset.boolValue)
            {
                EditorGUI.indentLevel++;

                var inlineRect = new Rect(
                    position.x,
                    firstLine.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    EditorGUI.GetPropertyHeight(inline, GUIContent.none, true));

                EditorGUI.PropertyField(inlineRect, inline, new GUIContent("Data"), true);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
