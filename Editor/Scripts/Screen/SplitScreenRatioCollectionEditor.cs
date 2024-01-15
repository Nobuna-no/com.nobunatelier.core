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
        for (int i = 0, c = m_collection.DataDefinitions.Length; i < c; i++)
        {
            var defintion = m_collection.DataDefinitions[i] as SplitScreenRatioDefinition;

            if (i == 0)
            {
                defintion.name = $"1 Participant";
            }
            else
            {
                defintion.name = $"{i + 1} Participants";
            }

            if (defintion.Viewports.Count != i + 1)
            {
                defintion.ResizeArray(i + 1);
            }
        }
    }
}