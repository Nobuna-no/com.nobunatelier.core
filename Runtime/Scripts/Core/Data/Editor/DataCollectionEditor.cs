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
        private string[] m_names;

        private int m_workingIndex = -1;
        private bool m_showBasicData = false;

        private void OnEnable()
        {
            m_collection = target as DataCollection;
            s_CollectionType = m_collection.GetType().ToString().Replace("Collection", " Collection");
            m_names = new string[m_collection.DataDefinitions.Length];

            for (int i = 0; i < m_names.Length; i++)
            {
                m_names[i] = m_collection.DataDefinitions[i].name;
            }

            GenerateReorderableList();
        }

        private void GenerateReorderableList()
        {
            list = new ReorderableList(m_collection.DataDefinitions, typeof(DataDefinition), false, true, true, true);

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

                    Rect r1 = rect;
                    r1.width = 16f;
                    r1.height = 16f;
                    r1.x = rect.x;
                    r1.y = rect.y + 2f;
                    //if (m_showData)
                    //{
                    //    r1.height *= 0.5f;
                    //}

                    bool isWorkingOnCurrentElement = index == m_workingIndex;
                    if (EditorGUI.ToggleLeft(r1, "", isWorkingOnCurrentElement))
                    {
                        m_workingIndex = index;
                    }
                    else if (isWorkingOnCurrentElement)
                    {
                        m_workingIndex = -1;
                    }

                    Rect r2 = rect;
                    r2.x += 16f;
                    r2.width -= 16f;
                    r2.height *= m_showBasicData ? 0.5f : 1f;

                    if (index == m_workingIndex)
                    {
                        EditorGUI.BeginChangeCheck();
                        string newName = EditorGUI.TextField(r2, element.name);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_collection.RenameDefinition(index, newName);
                        }
                    }
                    else
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.ObjectField(r2, element, typeof(DataDefinition), false);
                        EditorGUI.EndDisabledGroup();
                    }

                    if (m_showBasicData)
                    {

                        Rect r3 = rect;
                        float tabWidth = r3.width * .05f;
                        r3.x += tabWidth;
                        r3.height *= 0.5f;
                        r3.y += r3.height;
                        r3.width -= tabWidth;

                        SerializedProperty p = new SerializedObject(element).FindProperty("m_data");
                        if (p == null)
                        {
                            EditorGUI.LabelField(r3, NoBasicDataAvailableMessage);
                        }
                        else
                        {
                            EditorGUI.PropertyField(r3, p);
                        }
                    }
                };

            list.onAddCallback =
                (ReorderableList list) =>
                {
                    m_collection.CreateDefinition();
                    s_DirtyList = true;
                };

            list.onRemoveCallback =
                (ReorderableList list) =>
                {
                    int index = list.index;
                    var data = list.list[index] as DataDefinition;
                    m_collection.DeleteDefinition(index, data.name);
                    s_DirtyList = true;
                };

            s_DirtyList = false;
        }

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
        }
    }
}