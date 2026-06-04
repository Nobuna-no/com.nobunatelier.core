using System;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    /// <summary>
    /// Base PropertyDrawer for dual-slot types following the AssetOrInline convention.
    /// Expects serialized fields: m_UseAsset (bool), m_Asset (SO ref), m_InlineData (inline data).
    /// Shows a gear popup to toggle between asset and inline modes, with Extract/Inline actions.
    /// </summary>
    public class AssetOrInlineDrawer : PropertyDrawer
    {
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

            if (GUI.Button(gearRect, GUIContent.none, s_PopupStyle))
            {
                ShowContextMenu(property, useAsset, asset, inline);
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

        private void ShowContextMenu(SerializedProperty property,
            SerializedProperty useAsset, SerializedProperty asset, SerializedProperty inline)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Inline"), !useAsset.boolValue, () =>
            {
                useAsset.boolValue = false;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(new GUIContent("Asset"), useAsset.boolValue, () =>
            {
                useAsset.boolValue = true;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            bool hasInlineData = !useAsset.boolValue && HasInlineData(inline);
            bool hasAssetRef = useAsset.boolValue && asset.objectReferenceValue != null;

            if (hasInlineData)
            {
                menu.AddItem(new GUIContent("Extract to Asset..."), false, () =>
                {
                    if (OnExtractToAsset(property))
                    {
                        property.serializedObject.ApplyModifiedProperties();
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Extract to Asset..."));
            }

            if (hasAssetRef)
            {
                menu.AddItem(new GUIContent("Inline from Asset"), false, () =>
                {
                    if (OnInlineFromAsset(property))
                    {
                        property.serializedObject.ApplyModifiedProperties();
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Inline from Asset"));
            }

            menu.ShowAsContext();
        }

        private static bool HasInlineData(SerializedProperty inline)
        {
            if (inline.propertyType == SerializedPropertyType.ManagedReference)
                return inline.managedReferenceValue != null;

            return inline.hasVisibleChildren;
        }

        /// <summary>
        /// Override to implement Extract to Asset. Create a new SO, copy inline data to it,
        /// save to disk, then set m_UseAsset=true and m_Asset to the new asset.
        /// Return true if extraction succeeded.
        /// </summary>
        protected virtual bool OnExtractToAsset(SerializedProperty property) => false;

        /// <summary>
        /// Override to implement Inline from Asset. Copy asset data into m_InlineData,
        /// deep-copying any [SerializeReference] fields. Set m_UseAsset=false.
        /// Return true if inlining succeeded.
        /// </summary>
        protected virtual bool OnInlineFromAsset(SerializedProperty property) => false;

        /// <summary>
        /// Deep-copies a serialized property tree from source to destination.
        /// Handles [SerializeReference] fields via JSON clone to avoid shared references.
        /// Both properties must represent the same serializable type.
        /// </summary>
        protected static void CopyPropertyTree(SerializedProperty source, SerializedProperty dest)
        {
            var iter = source.Copy();
            var end = source.GetEndProperty();
            string srcRoot = source.propertyPath;
            string dstRoot = dest.propertyPath;

            bool enterChildren = true;
            while (iter.NextVisible(enterChildren))
            {
                if (SerializedProperty.EqualContents(iter, end))
                    break;

                enterChildren = true;

                string relativePath = iter.propertyPath[(srcRoot.Length + 1)..];
                var dstProp = dest.FindPropertyRelative(relativePath);
                if (dstProp == null)
                    continue;

                if (iter.propertyType == SerializedPropertyType.ManagedReference)
                {
                    var srcObj = iter.managedReferenceValue;
                    if (srcObj != null)
                    {
                        var clone = Activator.CreateInstance(srcObj.GetType());
                        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(srcObj), clone);
                        dstProp.managedReferenceValue = clone;
                    }
                    else
                    {
                        dstProp.managedReferenceValue = null;
                    }

                    enterChildren = false;
                    continue;
                }

                if (!iter.hasVisibleChildren)
                {
                    CopyLeafProperty(iter, dstProp);
                }
            }
        }

        private static void CopyLeafProperty(SerializedProperty src, SerializedProperty dst)
        {
            switch (src.propertyType)
            {
                case SerializedPropertyType.Integer:
                    dst.intValue = src.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    dst.boolValue = src.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    dst.floatValue = src.floatValue;
                    break;
                case SerializedPropertyType.String:
                    dst.stringValue = src.stringValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    dst.objectReferenceValue = src.objectReferenceValue;
                    break;
                case SerializedPropertyType.Enum:
                    dst.enumValueIndex = src.enumValueIndex;
                    break;
                case SerializedPropertyType.Color:
                    dst.colorValue = src.colorValue;
                    break;
                case SerializedPropertyType.Vector2:
                    dst.vector2Value = src.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    dst.vector3Value = src.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    dst.vector4Value = src.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    dst.rectValue = src.rectValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    dst.animationCurveValue = src.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    dst.boundsValue = src.boundsValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    dst.quaternionValue = src.quaternionValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    dst.vector2IntValue = src.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    dst.vector3IntValue = src.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    dst.rectIntValue = src.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    dst.boundsIntValue = src.boundsIntValue;
                    break;
                case SerializedPropertyType.Hash128:
                    dst.hash128Value = src.hash128Value;
                    break;
                case SerializedPropertyType.ArraySize:
                    dst.intValue = src.intValue;
                    break;
            }
        }
    }
}
