using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    // Draws a nice dropdown list of all available state definitions on the state components.
    // Also provide a Ping button to locate the references in the project as well with a button to create a new definition from the field.
    public class StateDefinitionPropertyDrawer<T, TCollection> : PropertyDrawer
        where T : StateDefinition
        where TCollection : DataCollection
    {
        private const int ButtonWidth = 20;

        private string m_localName;
        private bool m_creationMode = false;

        private StateComponent<T, TCollection> m_targetComponent;

        public bool m_isDataCollectionAvailable = true;
        private int m_definitionIndex = 0;

        private static List<string> s_names = null;
        private static int[] s_refCount = null;
        private static TCollection s_workingCollection = null;
        private StateMachineComponent<T, TCollection> m_stateMachine = null;

        private int m_currentRefFocus = 0;

        private void ResetWorkingCollection()
        {
            s_workingCollection = null;
            m_isDataCollectionAvailable = true;
            m_targetComponent = null;
            m_stateMachine = null;
            m_definitionIndex = 0;
            m_currentRefFocus = 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // First frame setup
            if (m_isDataCollectionAvailable && m_targetComponent == null)
            {
                m_currentRefFocus = 0;
                m_targetComponent = property.serializedObject.targetObject as StateComponent<T, TCollection>;

                m_isDataCollectionAvailable = m_targetComponent != null;

                if (m_targetComponent)
                {
                    m_stateMachine = m_targetComponent.ParentStateMachine;

                    if (m_stateMachine == null)
                    {
                        m_stateMachine = m_targetComponent as StateMachineComponent<T, TCollection>;
                    }

                    if (m_stateMachine)
                    {
                        var collection = m_stateMachine.ReferenceStateCollection as TCollection;
                        m_isDataCollectionAvailable = collection != null;

                        if (!m_isDataCollectionAvailable)
                        {
                            return;
                        }

                        if (collection != null && s_workingCollection != collection)
                        {
                            s_workingCollection = collection;
                            s_names = new List<string>(collection.EditorDataDefinitions.Length + 1)
                            {
                                "(none)",
                            };
                            s_names.AddRange(collection.EditorDataDefinitions.Select(x => x.name).ToArray());
                            s_refCount = new int[s_names.Count];
                        }

                        m_definitionIndex = property.objectReferenceValue != null ? -1 : 0;

                        for (int i = s_names.Count - 1; i >= 1; --i)
                        {
                            s_names[i] = s_names[i].Split(" [")[0];

                            if (m_definitionIndex == -1 && property.objectReferenceValue.name == s_names[i])
                            {
                                m_definitionIndex = i;
                            }

                            int useCount = m_stateMachine.GetStateDefinitionRefCount(m_targetComponent, s_workingCollection.GetDefinition(s_names[i]) as T);
                            s_refCount[i] = useCount;

                            if (useCount > 1)
                            {
                                s_names[i] += $" [{useCount} refs]";
                            }
                        }

                        if (m_definitionIndex == -1)
                        {
                            // Seems like we lost reference to the definition, regenerate the names array.
                            ResetWorkingCollection();
                            return;
                        }
                        else if (m_definitionIndex > 0)
                        {
                            if (m_targetComponent.StateDefinition == property.objectReferenceValue && m_targetComponent.gameObject.name != $"state-{property.objectReferenceValue.name}")
                            {
                                m_targetComponent.gameObject.name = $"state-{property.objectReferenceValue.name}";
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{m_targetComponent.name}: no state machine available.");
                        m_isDataCollectionAvailable = false;
                    }
                }
            }

            if (m_isDataCollectionAvailable)
            {
                if (!m_creationMode)
                {
                    var remainingPos = EditorGUI.PrefixLabel(position, label);

                    var workingPos = remainingPos;
                    workingPos.width -= ButtonWidth * 2;
                    // property.objectReferenceValue = EditorGUI.ObjectField(workingPos, property.objectReferenceValue, typeof(T), false);

                    var previousIndex = m_definitionIndex;
                    m_definitionIndex = EditorGUI.Popup(workingPos, m_definitionIndex, s_names.ToArray());
                    if (previousIndex != m_definitionIndex)
                    {
                        if (m_definitionIndex == 0)
                        {
                            property.objectReferenceValue = null;
                            ResetWorkingCollection();
                        }
                        else
                        {
                            property.objectReferenceValue = s_workingCollection.GetOrCreateDefinition(s_names[m_definitionIndex].Split(" [")[0]);
                            ResetWorkingCollection();
                        }
                    }

                    workingPos.x += workingPos.width;
                    workingPos.width = ButtonWidth;
                    using (new EditorGUI.DisabledGroupScope(property.objectReferenceValue == null))
                    {
                        if (GUI.Button(workingPos, "P"))
                        {
                            if (m_currentRefFocus == 0)
                            {
                                EditorGUIUtility.PingObject(property.objectReferenceValue);
                            }
                            else
                            {
                                m_stateMachine.PingStateDefinitionRef(property.objectReferenceValue as T, m_currentRefFocus - 1);
                            }

                            m_currentRefFocus = (int)Mathf.Repeat(m_currentRefFocus + 1, s_refCount[m_definitionIndex] + 1);
                        }
                    }

                    workingPos.x += workingPos.width;
                    if (GUI.Button(workingPos, "+"))
                    {
                        m_creationMode = true;
                    }
                }
                else
                {
                    var remainingPos = EditorGUI.PrefixLabel(position, label);

                    var workingPos = remainingPos;
                    workingPos.width -= ButtonWidth * 2;

                    m_localName = EditorGUI.TextField(workingPos, m_localName);

                    workingPos.x += workingPos.width;
                    workingPos.width = ButtonWidth;

                    using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(m_localName)))
                    {
                        if (GUI.Button(workingPos, "+"))
                        {
                            property.objectReferenceValue = s_workingCollection.GetOrCreateDefinition(m_localName);
                            ResetWorkingCollection();
                            m_creationMode = false;
                        }
                    }

                    workingPos.x += workingPos.width;
                    if (GUI.Button(workingPos, "x"))
                    {
                        m_creationMode = false;
                    }
                }
            }
            else
            {
                // Draw the default property field
                EditorGUI.PropertyField(position, property, label);
            }

            if (Event.current.type == EventType.DragUpdated)
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
            }
            // Handle drag-and-drop
            else if (Event.current.type == EventType.DragPerform)
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        GameObject gameObject = draggedObject as GameObject;
                        var draggedDataComponent = gameObject.GetComponent<StateComponent<T, TCollection>>();
                        if (draggedDataComponent != null)
                        {
                            property.objectReferenceValue = draggedDataComponent.GetStateDefinition();
                            property.serializedObject.ApplyModifiedProperties();
                            break;
                        }
                    }
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();
                }
            }

            EditorGUI.EndProperty();
        }
    }
}