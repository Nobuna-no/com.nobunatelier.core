using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using NobunAtelier;

namespace NobunAtelier.Editor
{
    [CustomEditor(typeof(DataCollection))]
    [CanEditMultipleObjects]
    public class DataCollectionEditor : UnityEditor.Editor
    {
        protected virtual bool ShouldHandleDragAndDrop => true;
        private static string s_CollectionType = "Unknown";

        protected DataCollection m_Collection;
        private ListView m_DefinitionListView;
        private VisualElement m_InspectorContainer;
        private UnityEditor.Editor m_CurrentElementEditor;
        private Toggle m_ShowBasicDataToggle;
        private bool m_ShowBasicData;
        private Foldout m_InspectorFoldout;

        private void OnEnable()
        {
            // Register for hierarchy window change to catch when the user comes back to this view
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            // Unregister when the editor is disabled
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            // Only refresh if our list view exists
            if (m_DefinitionListView != null)
            {
                // Reset the ListView's data source
                m_DefinitionListView.itemsSource = null;
                m_DefinitionListView.itemsSource = m_Collection?.EditorDataDefinitions;
                m_DefinitionListView.Rebuild();
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Collection = target as DataCollection;
            s_CollectionType = m_Collection.GetType().Name.Replace("Collection", " Collection");

            // Create root container
            var root = new VisualElement();

            // Load and apply stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.nobunatelier.core/Editor/UITK/DataCollectionEditor.uss");
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            CreateListView(root);
            CreateDragAndDropArea(root);
            CreateInspectorArea(root);

            return root;
        }

        private void CreateListView(VisualElement root)
        {
            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("header-container");

            var headerLabel = new Label(s_CollectionType);
            headerLabel.AddToClassList("header-label");
            headerContainer.Add(headerLabel);

            m_ShowBasicDataToggle = new Toggle { tooltip = "Show definition data" };
            m_ShowBasicDataToggle.RegisterValueChangedCallback(evt =>
            {
                m_ShowBasicData = evt.newValue;
                m_DefinitionListView.Rebuild();
            });
            headerContainer.Add(m_ShowBasicDataToggle);

            root.Add(headerContainer);

            m_DefinitionListView = new ListView
            {
                reorderable = true,
                showAddRemoveFooter = true,
                showBorder = true,
                showFoldoutHeader = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                reorderMode = ListViewReorderMode.Animated
            };

            m_DefinitionListView.itemsSource = m_Collection.EditorDataDefinitions;
            m_DefinitionListView.makeItem = () => new VisualElement();
            m_DefinitionListView.bindItem = (element, index) => BindListItem(element, index);

            m_DefinitionListView.selectionChanged += OnSelectionChanged;
            m_DefinitionListView.itemsAdded += OnItemsAdded;
            m_DefinitionListView.itemsRemoved += OnItemsRemoved;
            m_DefinitionListView.itemIndexChanged += OnItemMoved;

            root.Add(m_DefinitionListView);
        }

        private void BindListItem(VisualElement element, int index)
        {
            element.Clear();
            var definition = m_DefinitionListView.itemsSource[index] as DataDefinition;

            if (m_ShowBasicData)
            {
                var objectField = new ObjectField
                {
                    objectType = typeof(DataDefinition),
                    value = definition
                };

                objectField.RegisterValueChangedCallback(evt =>
                {
                    m_Collection.ForceSetDataDefinition(index, evt.newValue as DataDefinition);
                });

                element.Add(objectField);
            }
            else if (definition != null)
            {
                var textField = new TextField
                {
                    value = definition.name
                };

                textField.RegisterValueChangedCallback(evt =>
                {
                    m_Collection.RenameDefinition(index, evt.newValue);
                });

                element.Add(textField);
            }
        }

        private void CreateDragAndDropArea(VisualElement root)
        {
            if (!ShouldHandleDragAndDrop)
                return;

            var dropArea = new VisualElement();
            dropArea.AddToClassList("drop-area");

            var dragAndDropInfo = new Label("Drag and drop assets here");
            dragAndDropInfo.AddToClassList("drop-area-label");
            dropArea.Add(dragAndDropInfo);

            dropArea.RegisterCallback<DragEnterEvent>(OnDragEnter);
            dropArea.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            dropArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            dropArea.RegisterCallback<DragPerformEvent>(OnDragPerform);

            root.Add(dropArea);
        }

        private void CreateInspectorArea(VisualElement root)
        {
            m_InspectorContainer = new VisualElement();
            m_InspectorContainer.AddToClassList("inspector-container");

            m_InspectorFoldout = new Foldout
            {
                text = "Definition Inspector",
                value = true
            };
            m_InspectorContainer.Add(m_InspectorFoldout);

            root.Add(m_InspectorContainer);
            m_InspectorContainer.style.display = DisplayStyle.None;
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (selectedItems.GetEnumerator().MoveNext())
            {
                var definition = selectedItems.First() as DataDefinition;
                if (definition != null)
                {
                    if (m_CurrentElementEditor != null)
                        Object.DestroyImmediate(m_CurrentElementEditor);

                    m_CurrentElementEditor = UnityEditor.Editor.CreateEditor(definition);
                    m_InspectorContainer.style.display = DisplayStyle.Flex;

                    var container = new IMGUIContainer(() =>
                    {
                        if (m_CurrentElementEditor != null)
                            m_CurrentElementEditor.OnInspectorGUI();
                    });

                    m_InspectorFoldout.Clear();
                    m_InspectorFoldout.Add(container);
                }
            }
            else
            {
                if (m_CurrentElementEditor != null)
                    Object.DestroyImmediate(m_CurrentElementEditor);
                m_InspectorContainer.style.display = DisplayStyle.None;
            }
        }

        private void OnItemsAdded(IEnumerable<int> indexes)
        {
            foreach (var index in indexes)
            {
                m_Collection.CreateDefinition();
            }

            // Force Unity to update serialization
            serializedObject.Update();
            
            // Ensure we refresh the view properly
            EditorUtility.SetDirty(m_Collection);
            AssetDatabase.SaveAssetIfDirty(m_Collection);
            
            // Reset the data source completely to ensure we're working with the updated instances
            // Then completely rebuild the list
            EditorApplication.delayCall += () => {
                // Reset the ListView's data source
                m_DefinitionListView.itemsSource = null;
                m_DefinitionListView.itemsSource = m_Collection.EditorDataDefinitions;
            };
        }

        private void OnItemsRemoved(IEnumerable<int> indexes)
        {
            foreach (var index in indexes.OrderByDescending(i => i))
            {
                var definition = m_Collection.EditorDataDefinitions[index];
                m_Collection.DeleteDefinition(index, definition.name);
            }
            
            // Force Unity to update serialization
            serializedObject.Update();
            
            // Ensure we refresh the view properly
            EditorUtility.SetDirty(m_Collection);
            AssetDatabase.SaveAssetIfDirty(m_Collection);
            
            // Reset the data source completely to ensure we're working with the updated instances
            // Then completely rebuild the list
            EditorApplication.delayCall += () => {
                // Reset the ListView's data source
                m_DefinitionListView.itemsSource = null;
                m_DefinitionListView.itemsSource = m_Collection.EditorDataDefinitions;
            };
        }

        private void OnItemMoved(int oldIndex, int newIndex)
        {
            m_Collection.MoveDefinition(oldIndex, newIndex);
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            var dropArea = evt.currentTarget as VisualElement;
            if (dropArea != null)
            {
                if (ValidateDraggedObjects())
                    dropArea.AddToClassList("drop-area-valid");
                else
                    dropArea.AddToClassList("drop-area-invalid");
            }
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            var dropArea = evt.currentTarget as VisualElement;
            if (dropArea != null)
            {
                dropArea.RemoveFromClassList("drop-area-valid");
                dropArea.RemoveFromClassList("drop-area-invalid");
            }
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = ValidateDraggedObjects()
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!ValidateDraggedObjects())
                return;

            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is DataDefinition definition)
                {
                    m_Collection.AddDefinitionAssetToCollection(definition);
                }
            }

            DragAndDrop.AcceptDrag();
            var dropArea = evt.currentTarget as VisualElement;
            dropArea.RemoveFromClassList("drop-area-valid");
        }

        private bool ValidateDraggedObjects()
        {
            return DragAndDrop.objectReferences.Any(obj =>
                obj.GetType().IsAssignableFrom(m_Collection.GetDefinitionType()) &&
                !m_Collection.EditorDataDefinitions.Contains(obj));
        }
    }
}
