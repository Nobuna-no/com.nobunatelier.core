using System;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    [CustomPropertyDrawer(typeof(AbilityEffectReference))]
    public class AbilityEffectReferenceDrawer : AssetOrInlineDrawer
    {
        protected override bool OnExtractToAsset(SerializedProperty property)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Extract AbilityEffect",
                "New AbilityEffect",
                "asset",
                "Save the extracted AbilityEffect asset");

            if (string.IsNullOrEmpty(path))
                return false;

            var effectAsset = ScriptableObject.CreateInstance<AbilityEffectDefinition>();
            AssetDatabase.CreateAsset(effectAsset, path);

            var srcInline = property.FindPropertyRelative("m_InlineData");
            var assetSO = new SerializedObject(effectAsset);
            var dstDefinition = assetSO.FindProperty("m_Definition");

            // Both are [SerializeReference] — deep-copy via JSON
            var srcObj = srcInline.managedReferenceValue;
            if (srcObj != null)
            {
                var clone = Activator.CreateInstance(srcObj.GetType());
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(srcObj), clone);
                dstDefinition.managedReferenceValue = clone;
            }

            assetSO.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(effectAsset);
            AssetDatabase.SaveAssetIfDirty(effectAsset);

            property.FindPropertyRelative("m_UseAsset").boolValue = true;
            property.FindPropertyRelative("m_Asset").objectReferenceValue = effectAsset;

            return true;
        }

        protected override bool OnInlineFromAsset(SerializedProperty property)
        {
            var assetProp = property.FindPropertyRelative("m_Asset");
            var effectAsset = assetProp.objectReferenceValue as AbilityEffectDefinition;
            if (effectAsset == null)
                return false;

            var assetSO = new SerializedObject(effectAsset);
            var srcDefinition = assetSO.FindProperty("m_Definition");
            var dstInline = property.FindPropertyRelative("m_InlineData");

            // Both are [SerializeReference] — deep-copy via JSON
            var srcObj = srcDefinition.managedReferenceValue;
            if (srcObj != null)
            {
                var clone = Activator.CreateInstance(srcObj.GetType());
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(srcObj), clone);
                dstInline.managedReferenceValue = clone;
            }
            else
            {
                dstInline.managedReferenceValue = null;
            }

            property.FindPropertyRelative("m_UseAsset").boolValue = false;

            return true;
        }
    }
}
