using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    [CustomPropertyDrawer(typeof(AbilityActionReference))]
    public class AbilityActionReferenceDrawer : AssetOrInlineDrawer
    {
        protected override bool OnExtractToAsset(SerializedProperty property)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Extract AbilityAction",
                "New AbilityAction",
                "asset",
                "Save the extracted AbilityAction asset");

            if (string.IsNullOrEmpty(path))
                return false;

            var actionAsset = ScriptableObject.CreateInstance<AbilityAction>();
            AssetDatabase.CreateAsset(actionAsset, path);

            var srcInline = property.FindPropertyRelative("m_InlineData");
            var assetSO = new SerializedObject(actionAsset);
            var dstData = assetSO.FindProperty("m_Data");

            CopyPropertyTree(srcInline, dstData);
            assetSO.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(actionAsset);
            AssetDatabase.SaveAssetIfDirty(actionAsset);

            property.FindPropertyRelative("m_UseAsset").boolValue = true;
            property.FindPropertyRelative("m_Asset").objectReferenceValue = actionAsset;

            return true;
        }

        protected override bool OnInlineFromAsset(SerializedProperty property)
        {
            var assetProp = property.FindPropertyRelative("m_Asset");
            var actionAsset = assetProp.objectReferenceValue as AbilityAction;
            if (actionAsset == null)
                return false;

            var assetSO = new SerializedObject(actionAsset);
            var srcData = assetSO.FindProperty("m_Data");
            var dstInline = property.FindPropertyRelative("m_InlineData");

            CopyPropertyTree(srcData, dstInline);

            property.FindPropertyRelative("m_UseAsset").boolValue = false;

            return true;
        }
    }
}
