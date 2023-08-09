using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier
{
    // [CustomPropertyDrawer(typeof(StateDefinition))]
    public class StateDefinitionPropertyDrawer<T> : PropertyDrawer where T : StateDefinition
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the default property field
            EditorGUI.PropertyField(position, property, label);

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
                        var draggedDataComponent = gameObject.GetComponent<StateComponent<T>>();
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
