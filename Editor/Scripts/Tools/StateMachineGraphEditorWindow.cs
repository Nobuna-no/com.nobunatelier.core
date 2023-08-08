using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace NobunAtelier
{
    public class StateMachineGraphEditorWindow : EditorWindow
    {
        private Vector2 graphCanvasOffset = Vector2.zero;
        private float graphCanvasZoom = 1f;
        private Vector2 graphCanvasMousePosition;
        private bool isDraggingCanvas = false;

        [MenuItem("NobunAtelier/State Machine Graph Editor")]
        public static void ShowWindow()
        {
            GetWindow<StateMachineGraphEditorWindow>("NobunAtelier State Machine Graph");
        }

        private void OnGUI()
        {
            HandleInputEvents();

            // Draw the background
            DrawGraphBackground();

            // Draw nodes and connections
            DrawNodesAndConnections();
        }

        private void HandleInputEvents()
        {
            Event currentEvent = Event.current;
            graphCanvasMousePosition = currentEvent.mousePosition;

            // Handle mouse events
            HandleMouseEvents(currentEvent);

            // Handle keyboard events if needed
            HandleKeyboardEvents(currentEvent);
        }

        private void HandleMouseEvents(Event currentEvent)
        {
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0 && !isDraggingCanvas)
                    {
                        isDraggingCanvas = true;
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (currentEvent.button == 0 && isDraggingCanvas)
                    {
                        isDraggingCanvas = false;
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (isDraggingCanvas)
                    {
                        graphCanvasOffset += currentEvent.delta / graphCanvasZoom;
                        currentEvent.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    graphCanvasZoom += currentEvent.delta.y * 0.01f;
                    graphCanvasZoom = Mathf.Clamp(graphCanvasZoom, 0.1f, 3f);
                    currentEvent.Use();
                    break;
            }
        }

        private void HandleKeyboardEvents(Event currentEvent)
        {
            // Handle keyboard events here if needed
        }

        private void DrawGraphBackground()
        {
            Rect backgroundRect = new Rect(Vector2.zero, position.size);
            GUI.Box(backgroundRect, GUIContent.none, EditorStyles.helpBox);

            EditorGUI.DrawRect(new Rect(graphCanvasOffset, position.size / graphCanvasZoom), Color.gray);

            DrawGridLines();
        }

        private void DrawGridLines()
        {
            float gridSize = 20f; // Size of each grid cell in pixels
            Vector2 gridOffset = new Vector2(graphCanvasOffset.x % gridSize, graphCanvasOffset.y % gridSize);

            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            for (float x = gridOffset.x; x < position.width; x += gridSize)
            {
                Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, position.height, 0));
            }

            for (float y = gridOffset.y; y < position.height; y += gridSize)
            {
                Handles.DrawLine(new Vector3(0, y, 0), new Vector3(position.width, y, 0));
            }

            Handles.color = Color.white;
        }


        private void DrawNodesAndConnections()
        {
            // Draw nodes and their connections here
        }
    }

}
