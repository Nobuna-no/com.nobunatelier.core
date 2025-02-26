//using UnityEditor;
//using UnityEditorInternal;
//using UnityEngine;

//namespace NobunAtelier.Editor
//{
//    [CustomEditor(typeof(DataCollection))]
//    [CanEditMultipleObjects]
//    public class DataCollectionEditor : UnityEditor.Editor
//    {
//        protected virtual bool ShouldHandleDragAndDrop => true;
//        private static bool s_DirtyList = false;
//        private static string s_CollectionType = "Unknown";

//        private readonly GUIContent Title = new GUIContent("", "Show definition data");
//        private readonly GUIContent NoBasicDataAvailableMessage = new GUIContent("No available data");

//        protected DataCollection m_collection;
//        public ReorderableList list = null;

//        private DataDefinition m_currentElement;
//        private UnityEditor.Editor m_currentEditor;
//        private int m_workingIndex = -1;
//        private bool m_showBasicData = false;

//        private bool m_potentialDrag = false;
//        private bool hasAtLeastOneValidAssetType = false;

//        private void OnEnable()
//        {
//            m_collection = target as DataCollection;
//            s_CollectionType = m_collection.GetType().Name.Replace("Collection", " Collection");
//            m_workingIndex = -1;
//            GenerateReorderableList();
//        }

//        private void GenerateReorderableList()
//        {
//            list = new ReorderableList(m_collection.EditorDataDefinitions, typeof(DataDefinition), true, true, true, true);

//            list.drawHeaderCallback =
//                (Rect rect) =>
//                {
//                    Rect r1 = rect;
//                    r1.width -= 16f;
//                    EditorGUI.LabelField(rect, $"{s_CollectionType}");

//                    r1.x += r1.width;
//                    r1.width = 16f;
//                    EditorGUI.BeginChangeCheck();
//                    m_showBasicData = EditorGUI.Toggle(r1, Title, m_showBasicData);
//                    if (EditorGUI.EndChangeCheck())
//                    {
//                        list.elementHeight = EditorGUIUtility.singleLineHeight * (m_showBasicData ? 2f : 1f);
//                    }
//                };

//            list.drawElementCallback =
//                (Rect rect, int index, bool isActive, bool isFocused) =>
//                {
//                    if (s_DirtyList)
//                    {
//                        return;
//                    }

//                    var element = m_collection.EditorDataDefinitions[index];

//                    Rect r2 = rect;
//                    // r2.x += 16f;
//                    r2.y += 1.75f;
//                    // r2.width -= 16f;
//                    r2.height = EditorGUIUtility.singleLineHeight;
//                    // r2.height *= m_showBasicData ? 0.5f : 1f;

//                    if (m_showBasicData)
//                    {
//                        EditorGUI.BeginChangeCheck();
//                        var newObject = EditorGUI.ObjectField(r2, element, typeof(DataDefinition), false) as DataDefinition;
//                        if (EditorGUI.EndChangeCheck())
//                        {
//                            m_collection.ForceSetDataDefinition(index, newObject);
//                        }
//                    }
//                    else if (element)
//                    {
//                        EditorGUI.BeginChangeCheck();
//                        string newName = EditorGUI.TextField(r2, element.name);
//                        if (EditorGUI.EndChangeCheck())
//                        {
//                            m_collection.RenameDefinition(index, newName);
//                        }
//                    }
//                };

//            list.onAddCallback =
//                (ReorderableList list) =>
//                {
//                    m_collection.CreateDefinition();
//                    m_workingIndex = -1;
//                    s_DirtyList = true;
//                };

//            list.onRemoveCallback =
//                (ReorderableList list) =>
//                {
//                    int index = list.index;
//                    var data = list.list[index] as DataDefinition;
//                    m_collection.DeleteDefinition(index, data.name);
//                    m_workingIndex = -1;
//                    s_DirtyList = true;
//                };

//            list.onSelectCallback =
//                (ReorderableList list) =>
//                {
//                    m_workingIndex = list.index;
//                    m_currentElement = m_collection.EditorDataDefinitions[m_workingIndex];
//                    m_currentEditor = UnityEditor.Editor.CreateEditor(m_currentElement);
//                };

//            list.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
//            {
//                m_collection.MoveDefinition(oldIndex, newIndex);
//                m_workingIndex = -1;
//            };

//            s_DirtyList = false;
//        }

//        private bool m_titleBarExpand = true;

//        public override void OnInspectorGUI()
//        {
//            serializedObject.Update();

//            if (m_collection == null)
//            {
//                Debug.LogError("ScriptableObject is null");
//            }
//            else if (list == null || s_DirtyList)
//            {
//                GenerateReorderableList();
//            }
//            else if (list.list == null)
//            {
//                Debug.LogError("ReorderableList points to a null list");
//            }
//            else
//            {
//                list.DoLayoutList();
//            }

//            if (ShouldHandleDragAndDrop)
//            {
//                HandleDragAndDrop();
//            }

//            if (m_workingIndex == -1)
//            {
//                serializedObject.ApplyModifiedProperties();
//                return;
//            }

//            if (m_currentEditor != null)
//            {
//                EditorGUILayout.Space();
//                EditorGUILayout.Space();
//                using (new EditorGUILayout.VerticalScope(GUI.skin.window))
//                {
//                    m_titleBarExpand = EditorGUILayout.InspectorTitlebar(m_titleBarExpand, m_currentEditor);
//                    if (m_titleBarExpand)
//                    {
//                        // Do not remove Morgan! The indent is for nested objects.
//                        EditorGUI.indentLevel++;
//                        m_currentEditor.OnInspectorGUI();
//                        EditorGUI.indentLevel--;
//                    }
//                }
//            }

//            serializedObject.ApplyModifiedProperties();
//        }

//        private void HandleDragAndDrop()
//        {
//            Event currentEvent = Event.current;
//            // Check if an object is dragged into the inspector
//            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
//            {
//                m_potentialDrag = true;
//                if (currentEvent.type == EventType.DragPerform)
//                {
//                    foreach (var item in DragAndDrop.objectReferences)
//                    {
//                        if (!item.GetType().IsAssignableFrom(m_collection.GetDefinitionType()))
//                        {
//                            continue;
//                        }

//                        m_collection.AddDefinitionAssetToCollection(item as DataDefinition);
//                    }

//                    m_potentialDrag = false;
//                    DragAndDrop.AcceptDrag();
//                }
//            }

//            if (m_potentialDrag)
//            {
//                if (currentEvent.type == EventType.DragUpdated)
//                {
//                    hasAtLeastOneValidAssetType = false;
//                    foreach (var item in DragAndDrop.objectReferences)
//                    {
//                        if (item.GetType().IsAssignableFrom(m_collection.GetDefinitionType()))
//                        {
//                            bool isAlreadyInCollection = false;
//                            foreach (var def in m_collection.EditorDataDefinitions)
//                            {
//                                if (def == item)
//                                {
//                                    isAlreadyInCollection = true;
//                                    break;
//                                }
//                            }

//                            hasAtLeastOneValidAssetType = !isAlreadyInCollection;
//                            break;
//                        }
//                    }
//                }
//                else if (currentEvent.type == EventType.DragExited)
//                {
//                    m_potentialDrag = false;
//                    return;
//                }

//                if (hasAtLeastOneValidAssetType)
//                {
//                    EditorGUILayout.HelpBox($"Drop here to add to the collection.", MessageType.Info);
//                    EditorGUILayout.HelpBox($"Original asset will be move to trash! Drag and dropping from another collection with move the entire collection to trash!", MessageType.Warning);
//                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
//                }
//            }
//        }
//    }
//}

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
        }

        private void OnItemsRemoved(IEnumerable<int> indexes)
        {
            foreach (var index in indexes.OrderByDescending(i => i))
            {
                var definition = m_Collection.EditorDataDefinitions[index];
                m_Collection.DeleteDefinition(index, definition.name);
            }
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
