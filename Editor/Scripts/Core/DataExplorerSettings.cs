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
            VirtualFolders
        }

        [SerializeField] private ViewMode m_CurrentViewMode = ViewMode.Flat;
        [SerializeField] private List<VirtualFolder> m_VirtualFolders = new List<VirtualFolder>();

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

        public List<VirtualFolder> VirtualFolders => m_VirtualFolders;

        public void AddVirtualFolder(string name)
        {
            m_VirtualFolders.Add(new VirtualFolder { Name = name });
            Save(true);
        }

        public void RemoveVirtualFolder(string name)
        {
            m_VirtualFolders.RemoveAll(f => f.Name == name);
            Save(true);
        }

        public void AddCollectionToFolder(string folderName, string collectionGuid)
        {
            var folder = m_VirtualFolders.Find(f => f.Name == folderName);
            if (folder != null && !folder.CollectionGuids.Contains(collectionGuid))
            {
                folder.CollectionGuids.Add(collectionGuid);
                Save(true);
            }
        }

        public void RemoveCollectionFromFolder(string folderName, string collectionGuid)
        {
            var folder = m_VirtualFolders.Find(f => f.Name == folderName);
            if (folder != null)
            {
                folder.CollectionGuids.Remove(collectionGuid);
                Save(true);
            }
        }

        public void Save()
        {
            Save(true);
        }
    }

    [System.Serializable]
    public class VirtualFolder
    {
        public string Name;
        public List<string> CollectionGuids = new List<string>();
    }
}
