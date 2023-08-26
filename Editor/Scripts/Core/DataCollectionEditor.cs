using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NobunAtelier
{
    [CustomEditor(typeof(DataCollection))]
    [CanEditMultipleObjects]
    public class DataCollectionEditor : Editor
    {
        private static bool s_DirtyList = false;
        private static string s_CollectionType = "Unknown";

        private readonly GUIContent Title = new GUIContent("", "Show definition data");
        private readonly GUIContent NoBasicDataAvailableMessage = new GUIContent("No available data");

        private DataCollection m_collection;
        public ReorderableList list = null;

        private DataDefinition m_currentElement;
        private Editor m_currentEditor;
        private int m_workingIndex = -1;
        private bool m_showBasicData = false;

        private void OnEnable()
        {
            m_collection = target as DataCollection;
            s_CollectionType = m_collection.GetType().Name.Replace("Collection", " Collection");
            m_workingIndex = -1;
            GenerateReorderableList();
        }

        private void GenerateReorderableList()
        {
            list = new ReorderableList(m_collection.DataDefinitions, typeof(DataDefinition), true, true, true, true);

            list.drawHeaderCallback =
                (Rect rect) =>
                {
                    Rect r1 = rect;
                    r1.width -= 16f;
                    EditorGUI.LabelField(rect, $"{s_CollectionType}");

                    r1.x += r1.width;
                    r1.width = 16f;
                    EditorGUI.BeginChangeCheck();
                    m_showBasicData = EditorGUI.Toggle(r1, Title, m_showBasicData);
                    if (EditorGUI.EndChangeCheck())
                    {
                        list.elementHeight = EditorGUIUtility.singleLineHeight * (m_showBasicData ? 2f : 1f);
                    }
                };

            list.drawElementCallback =
                (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    if (s_DirtyList)
                    {
                        return;
                    }

                    var element = m_collection.DataDefinitions[index];

                    Rect r2 = rect;
                    // r2.x += 16f;
                    r2.y += 1.75f;
                    // r2.width -= 16f;
                    r2.height = EditorGUIUtility.singleLineHeight;
                    // r2.height *= m_showBasicData ? 0.5f : 1f;

                    if (m_showBasicData)
                    {
                        EditorGUI.BeginChangeCheck();
                        var newObject = EditorGUI.ObjectField(r2, element, typeof(DataDefinition), false) as DataDefinition;
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_collection.ForceSetDataDefinition(index, newObject);
                        }
                    }
                    else if (element)
                    {
                        EditorGUI.BeginChangeCheck();
                        string newName = EditorGUI.TextField(r2, element.name);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_collection.RenameDefinition(index, newName);
                        }
                    }
                };

            list.onAddCallback =
                (ReorderableList list) =>
                {
                    m_collection.CreateDefinition();
                    m_workingIndex = -1;
                    s_DirtyList = true;
                };

            list.onRemoveCallback =
                (ReorderableList list) =>
                {
                    int index = list.index;
                    var data = list.list[index] as DataDefinition;
                    m_collection.DeleteDefinition(index, data.name);
                    m_workingIndex = -1;
                    s_DirtyList = true;
                };

            list.onSelectCallback =
                (ReorderableList list) =>
                {
                    m_workingIndex = list.index;
                    m_currentElement = m_collection.DataDefinitions[m_workingIndex];
                    m_currentEditor = Editor.CreateEditor(m_currentElement);
                };

            list.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
            {
                m_collection.MoveDefinition(oldIndex, newIndex);
                m_workingIndex = -1;
            };

            s_DirtyList = false;
        }

        private bool m_titleBarExpand = true;

        public override void OnInspectorGUI()
        {
            if (m_collection == null)
            {
                Debug.LogError("ScriptableObject is null");
            }
            else if (list == null || s_DirtyList)
            {
                GenerateReorderableList();
            }
            else if (list.list == null)
            {
                Debug.LogError("ReorderableList points to a null list");
            }
            else
            {
                list.DoLayoutList();
            }

            if (m_workingIndex != -1)
            {
                if (m_currentEditor != null)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.window))
                    {
                        m_titleBarExpand = EditorGUILayout.InspectorTitlebar(m_titleBarExpand, m_currentEditor);

                        if (m_titleBarExpand)
                        {
                            EditorGUI.indentLevel++;
                            m_currentEditor.OnInspectorGUI();
                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }
        }
    }
}