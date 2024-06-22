using NobunAtelier.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplitScreenRatioCollection))]
public class SplitScreenRatioCollectionEditor : DataCollectionEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUI.changed)
        {
            RefreshSize();
        }
    }

    private void RefreshSize()
    {
        for (int i = 0, c = m_collection.EditorDataDefinitions.Length; i < c; i++)
        {
            var ssDef = m_collection.EditorDataDefinitions[i] as SplitScreenRatioDefinition;

            if (i == 0)
            {
                ssDef.name = $"1 Participant";
            }
            else
            {
                ssDef.name = $"{i + 1} Participants";
            }

            if (ssDef.Viewports.Count != i + 1)
            {
                ssDef.ResizeArray(i + 1);
            }
        }
    }
}