using UnityEngine;
using NobunAtelier;
using System.Collections.Generic;
using NaughtyAttributes;

public class SplitScreenRatioDefinition : DataDefinition
{
    [InfoBox("The size of this array is procedurally generated through the collect." +
        " Please, don't modify the size of the array on the DataDefinition itself.", EInfoBoxType.Warning)]
    [SerializeField] private Rect[] m_viewportRect;

    public IReadOnlyList<Rect> Viewports => m_viewportRect;

#if UNITY_EDITOR
    public void ResizeArray(int size)
    {
        var values = new List<Rect>(m_viewportRect);
        if (m_viewportRect.Length != size)
        {
            m_viewportRect = new Rect[size];
        }

        for (int i = 0; i < m_viewportRect.Length; i++)
        {
            if (values.Count <= i)
            {
                break;
            }

            m_viewportRect[i] = values[i];
        }

        values.Clear();
    }
#endif
}