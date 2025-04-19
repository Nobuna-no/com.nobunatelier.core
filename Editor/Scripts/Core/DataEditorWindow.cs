using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using NobunAtelier.Editor;

namespace NobunAtelier.Editor
{
    public class DataEditorWindow : EditorWindow
    {
        private static DataEditorWindow s_EditorWindow;

        [MenuItem("Window/NobunAtelier/Data Explorer")]
        public static void ShowWindow()
        {
            s_EditorWindow = GetWindow<DataEditorWindow>();
            s_EditorWindow.titleContent = new GUIContent("Data Explorer");
            s_EditorWindow.minSize = new Vector2(650, 400);
        }

        // UITK elements
        private VisualElement m_RootElement;
        private VisualElement m_LeftPanel;
        private VisualElement m_RightPanel;
        private VisualElement m_InspectorContainer;
        private ToolbarSearchField m_SearchField;
        private TreeView m_TreeView;
        private ScrollView m_InspectorScrollView;

        // Data handling
        private List<DataTreeItem> m_TreeItems = new List<DataTreeItem>();
        private Dictionary<string, bool> m_FoldoutStates = new Dictionary<string, bool>();
        private UnityEditor.Editor m_CurrentEditor;
        private Object m_SelectedObject;

        private void CreateGUI()
        {
            m_RootElement = rootVisualElement;

            // Load stylesheet from package path
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.nobunatelier.core/Editor/UITK/DataEditorWindow.uss");
            if (styleSheet != null)
                m_RootElement.styleSheets.Add(styleSheet);

            // Create main layout
            CreateToolbar();
            CreateSplitView();

            // Initialize data
            RefreshData();
        }

        private void CreateToolbar()
        {
            var toolbar = new Toolbar();
            m_RootElement.Add(toolbar);

            var viewMenu = new ToolbarMenu { text = "View" };
            viewMenu.menu.AppendAction("Asset List", a => SetViewMode(DataExplorerSettings.ViewMode.Flat),
                a => GetViewModeStatus(DataExplorerSettings.ViewMode.Flat));
            viewMenu.menu.AppendAction("By Type", a => SetViewMode(DataExplorerSettings.ViewMode.ByType),
                a => GetViewModeStatus(DataExplorerSettings.ViewMode.ByType));
            toolbar.Add(viewMenu);

            var helpButton = new ToolbarMenu { text = "Help" };
            helpButton.menu.AppendAction("About", (a) =>
                EditorUtility.DisplayDialog("About", "Data Explorer Tool\nPart of NobunAtelier Framework", "Close"));
            toolbar.Add(helpButton);

            toolbar.Add(new ToolbarSpacer { flex = true });

            var refreshButton = new ToolbarButton(() => RefreshData()) { text = "Refresh" };
            toolbar.Add(refreshButton);

            m_SearchField = new ToolbarSearchField();
            m_SearchField.RegisterValueChangedCallback(OnSearchChanged);
            toolbar.Add(m_SearchField);
        }

        private DropdownMenuAction.Status GetViewModeStatus(DataExplorerSettings.ViewMode mode)
        {
            return DataExplorerSettings.instance.CurrentViewMode == mode
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal;
        }

        private void SetViewMode(DataExplorerSettings.ViewMode mode)
        {
            DataExplorerSettings.instance.CurrentViewMode = mode;
            UpdateHeaderText();
            RefreshData();
        }

        private void CreateSplitView()
        {
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            m_RootElement.Add(splitView);
            splitView.style.flexGrow = 1;

            m_LeftPanel = new VisualElement();
            m_LeftPanel.style.minWidth = 200;
            splitView.Add(m_LeftPanel);

            var treeHeader = new Label("Data Collections");
            treeHeader.name = "tree-header";
            treeHeader.AddToClassList("header-label");
            m_LeftPanel.Add(treeHeader);

            UpdateHeaderText();

            // Create TreeView instead of ListView
            m_TreeView = new TreeView();
            m_TreeView.viewDataKey = "DataExplorerTree";
            m_TreeView.selectionType = SelectionType.Single;
            m_TreeView.showBorder = true;
            m_TreeView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;

            // Set up item source
            m_TreeView.makeItem = MakeTreeItem;
            m_TreeView.bindItem = BindTreeItem;
            m_TreeView.unbindItem = UnbindTreeItem;

            // Handle selection change
            m_TreeView.selectionChanged += OnSelectionChanged;

            m_LeftPanel.Add(m_TreeView);
            m_TreeView.style.flexGrow = 1;

            m_RightPanel = new VisualElement();
            m_RightPanel.style.minWidth = 300;
            splitView.Add(m_RightPanel);

            var emptyStateLabel = new Label("Select a data to inspect");
            emptyStateLabel.AddToClassList("empty-state-label");
            m_RightPanel.Add(emptyStateLabel);

            m_InspectorContainer = new VisualElement();
            m_InspectorContainer.style.display = DisplayStyle.None;
            m_RightPanel.Add(m_InspectorContainer);
            m_InspectorContainer.style.flexGrow = 1;

            var inspectorHeader = new VisualElement();
            inspectorHeader.AddToClassList("inspector-header");
            m_InspectorContainer.Add(inspectorHeader);

            var inspectorTitleLabel = new Label();
            inspectorTitleLabel.name = "inspector-title";
            inspectorHeader.Add(inspectorTitleLabel);

            var pingButton = new Button(() => PingSelectedObject()) { text = "Ping" };
            pingButton.AddToClassList("ping-button");
            inspectorHeader.Add(pingButton);

            m_InspectorScrollView = new ScrollView();
            m_InspectorContainer.Add(m_InspectorScrollView);
            m_InspectorScrollView.style.flexGrow = 1;
        }

        private void UpdateHeaderText()
        {
            var header = m_RootElement.Q<Label>("tree-header");
            if (header != null)
            {
                if (DataExplorerSettings.instance.CurrentViewMode == DataExplorerSettings.ViewMode.ByType)
                {
                    header.text = $"Data Collections (By Type)";
                }
                else
                {
                    header.text = $"Data Collections";
                }
            }
        }

        private VisualElement MakeTreeItem()
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;

            // Icon element
            var icon = new Image();
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 4;
            item.Add(icon);

            // Label for the item name
            var label = new Label();
            item.Add(label);

            return item;
        }

        private void BindTreeItem(VisualElement element, int index)
        {
            var item = m_TreeView.GetItemDataForIndex<DataTreeItem>(index);
            if (item == null) return;

            // var foldout = element.Q<Foldout>();
            var icon = element.Q<Image>();
            var label = element.Q<Label>();

            // Set up foldout visibility based on item type
            if (item.IsCollection)
            {
                label.text = item.DisplayName;
                label.AddToClassList("collection-item");

                // Set collection icon
                icon.image = EditorGUIUtility.IconContent("Folder Icon").image;
            }
            else
            {
                // foldout.style.display = DisplayStyle.None;
                label.text = item.DisplayName;
                label.RemoveFromClassList("collection-item");

                // Set item icon
                if (item.Object != null)
                {
                    icon.image = AssetPreview.GetMiniThumbnail(item.Object);
                }
                else
                {
                    icon.image = null;
                }
            }
        }

        private void UnbindTreeItem(VisualElement element, int index)
        {
            var item = m_TreeView.GetItemDataForIndex<DataTreeItem>(index);
            if (item == null) return;

            var foldout = element.Q<Foldout>();
            if (foldout != null)
            {
                foldout.UnregisterValueChangedCallback(evt => {
                    SetFoldoutState(item.DisplayName, evt.newValue);
                });
            }
        }

        private bool GetFoldoutState(string itemName)
        {
            if (m_FoldoutStates.TryGetValue(itemName, out bool state))
            {
                return state;
            }

            // Default to collapsed
            return false;
        }

        private void SetFoldoutState(string itemName, bool state)
        {
            m_FoldoutStates[itemName] = state;
        }

        private void RefreshData()
        {
            // Clear existing items
            m_TreeItems.Clear();

            switch (DataExplorerSettings.instance.CurrentViewMode)
            {
                case DataExplorerSettings.ViewMode.Flat:
                    LoadFlatView();
                    break;
                case DataExplorerSettings.ViewMode.ByType:
                    LoadTypeView();
                    break;
            }

            // Update tree view
            var rootItems = GenerateTreeViewItems(m_TreeItems);
            m_TreeView.SetRootItems(rootItems);
            m_TreeView.Rebuild();
        }

        private void LoadTypeView()
        {
            // Create type folders
            Dictionary<string, DataTreeItem> typeFolders = new Dictionary<string, DataTreeItem>();

            string[] collectionGuids = AssetDatabase.FindAssets("t:DataCollection");
            foreach (string guid in collectionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DataCollection collection = AssetDatabase.LoadAssetAtPath<DataCollection>(path);
                if (collection == null) continue;

                string typeName = collection.GetType().Name.Replace("Collection", "");

                if (!typeFolders.TryGetValue(typeName, out var typeFolder))
                {
                    typeFolder = new DataTreeItem
                    {
                        ID = System.Guid.NewGuid().ToString(),
                        DisplayName = typeName,
                        IsCollection = true,
                        Children = new List<DataTreeItem>()
                    };
                    typeFolders[typeName] = typeFolder;
                    m_TreeItems.Add(typeFolder);
                }

                AddCollectionToTree(collection, typeFolder);
            }
        }

        private void LoadFlatView()
        {
            string[] collectionGuids = AssetDatabase.FindAssets("t:DataCollection");
            foreach (string guid in collectionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DataCollection collection = AssetDatabase.LoadAssetAtPath<DataCollection>(path);
                if (collection != null)
                {
                    AddCollectionToTree(collection);
                }
            }
        }

        private void AddCollectionToTree(DataCollection collection, DataTreeItem parent = null)
        {
            var collectionItem = new DataTreeItem
            {
                ID = System.Guid.NewGuid().ToString(),
                Object = collection,
                DisplayName = collection.name,
                IsCollection = true,
                Children = new List<DataTreeItem>()
            };

            foreach (var definition in collection.EditorDataDefinitions)
            {
                if (definition != null)
                {
                    var defItem = new DataTreeItem
                    {
                        ID = System.Guid.NewGuid().ToString(),
                        Object = definition,
                        DisplayName = definition.name,
                        IsCollection = false,
                        Parent = collectionItem
                    };
                    collectionItem.Children.Add(defItem);
                }
            }

            if (parent != null)
            {
                parent.Children.Add(collectionItem);
                collectionItem.Parent = parent;
            }
            else
            {
                m_TreeItems.Add(collectionItem);
            }
        }
        private List<TreeViewItemData<DataTreeItem>> GenerateTreeViewItems(List<DataTreeItem> items)
        {
            var result = new List<TreeViewItemData<DataTreeItem>>();

            foreach (var item in items)
            {
                if (item.Children != null && item.Children.Count > 0)
                {
                    var childItems = item.Children.Select(child =>
                        new TreeViewItemData<DataTreeItem>(child.ID.GetHashCode(), child)).ToList();

                    result.Add(new TreeViewItemData<DataTreeItem>(
                        item.ID.GetHashCode(),
                        item,
                        childItems));
                }
                else
                {
                    result.Add(new TreeViewItemData<DataTreeItem>(
                        item.ID.GetHashCode(),
                        item));
                }
            }

            return result;
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            string searchText = evt.newValue.ToLowerInvariant();

            if (string.IsNullOrEmpty(searchText))
            {
                // Show all items
                RefreshData();
            }
            else
            {
                // Filter items and expand collections that contain matches
                var filteredCollections = new List<DataTreeItem>();

                foreach (var collection in m_TreeItems)
                {
                    bool collectionMatches = collection.DisplayName.ToLowerInvariant().Contains(searchText);
                    var matchingChildren = collection.Children
                        .Where(child => child.DisplayName.ToLowerInvariant().Contains(searchText))
                        .ToList();

                    if (collectionMatches || matchingChildren.Count > 0)
                    {
                        // Clone the collection to avoid modifying the original
                        var filteredCollection = new DataTreeItem
                        {
                            ID = collection.ID,
                            Object = collection.Object,
                            DisplayName = collection.DisplayName,
                            IsCollection = true,
                            Children = matchingChildren.Count > 0 ? matchingChildren : new List<DataTreeItem>()
                        };

                        filteredCollections.Add(filteredCollection);

                        // Auto-expand collections with matching children
                        if (matchingChildren.Count > 0)
                        {
                            SetFoldoutState(collection.DisplayName, true);
                        }
                    }
                }

                // Update tree view
                var rootItems = GenerateTreeViewItems(filteredCollections);
                m_TreeView.SetRootItems(rootItems);
                m_TreeView.Rebuild();
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (selectedItems != null && selectedItems.Count() > 0)
            {
                var selectedItem = selectedItems.First() as DataTreeItem;
                if (selectedItem != null && selectedItem.Object != null)
                {
                    UpdateInspector(selectedItem.Object);
                }
            }
            else
            {
                ClearInspector();
            }
        }

        private void UpdateInspector(Object selectedObject)
        {
            m_SelectedObject = selectedObject;

            if (m_SelectedObject == null)
            {
                ClearInspector();
                return;
            }

            // Update inspector title
            var titleLabel = m_InspectorContainer.Q<Label>("inspector-title");
            titleLabel.text = m_SelectedObject.name;

            // Clear previous inspector content
            m_InspectorScrollView.Clear();

            // Create inspector element - it will automatically use custom editor if one exists
            var inspectorElement = new InspectorElement(m_SelectedObject);
            m_InspectorScrollView.Add(inspectorElement);

            // Show inspector container
            m_InspectorContainer.style.display = DisplayStyle.Flex;
            m_RightPanel.Q<Label>().style.display = DisplayStyle.None;
        }

        private void ClearInspector()
        {
            if (m_CurrentEditor != null)
            {
                Object.DestroyImmediate(m_CurrentEditor);
                m_CurrentEditor = null;
            }

            m_SelectedObject = null;

            // Hide inspector container
            m_InspectorContainer.style.display = DisplayStyle.None;
            m_RightPanel.Q<Label>().style.display = DisplayStyle.Flex;
        }

        private void PingSelectedObject()
        {
            if (m_SelectedObject != null)
            {
                EditorGUIUtility.PingObject(m_SelectedObject);
            }
        }

        private void OnDisable()
        {
            if (m_CurrentEditor != null)
            {
                DestroyImmediate(m_CurrentEditor);
                m_CurrentEditor = null;
            }
        }

        // Data structure for tree items
        private class DataTreeItem
        {
            public string ID;
            public Object Object;
            public string DisplayName;
            public bool IsCollection;
            public List<DataTreeItem> Children;
            public DataTreeItem Parent;
        }
    }
}