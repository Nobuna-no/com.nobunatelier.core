using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NobunAtelier.Editor
{
    [FilePath("UserSettings/NobunAtelier/DataExplorerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class DataExplorerSettings : ScriptableSingleton<DataExplorerSettings>
    {
        public enum ViewMode
        {
            Flat,
            ByType,
        }

        [SerializeField] private ViewMode m_CurrentViewMode = ViewMode.Flat;

        public ViewMode CurrentViewMode
        {
            get => m_CurrentViewMode;
            set
            {
                if (m_CurrentViewMode != value)
                {
                    m_CurrentViewMode = value;
                    Save(true);
                }
            }
        }

        public void Save()
        {
            Save(true);
        }
    }
}
