using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    public class DataScriptGenerator : EditorWindow
    {
        private readonly string NamespacesString = "using UnityEngine;\nusing NobunAtelier;\n\n";
        private readonly string EditorNamespacesString = "using UnityEditor;\nusing NobunAtelier;\nusing NobunAtelier.Editor;\n\n";
        private readonly string DefinitionTemplateString = "public class {0}Definition : {1}\n";
        private readonly string CollectionTemplateString = "[CreateAssetMenu(fileName =\"DC_{0}\", menuName = \"NobunAtelier/Collection/{0}\")]\npublic class {0}Collection : DataCollection<{0}Definition>\n";
        private readonly string CollectionEditorTemplateString = "[CustomEditor(typeof({0}Collection))]\npublic class {0}CollectionEditor : DataCollectionEditor\n";
        private readonly string DefinitionPropertyDrawerTemplateString = "[CustomPropertyDrawer(typeof({0}Definition))]\npublic class StateDefinitionPropertyDrawer : {0}DefinitionPropertyDrawer<{0}Definition, {0}Collection>\n";
        private readonly string EmptyMethodString = "{\n\n}";

        private List<Type> m_dataDefinitionTypes = new List<Type>();
        private string[] m_typeNames;
        private string m_className = "MyData";
        private string m_savePath = "";
        private int m_selectedTypeIndex = 0;

        private string m_scriptContent;
        private string m_scriptPath;
        private string m_collectionScriptContent;
        private string m_collectionScriptPath;
        private string m_propertyDrawerScriptContent;
        private string m_propertyDrawerScriptPath;
        private string m_collectionEditorScriptContent;
        private string m_collectionEditorScriptPath;
        private string m_editorFolderPath;
        private bool m_isPreviewReady;
        private Vector2 m_scrollviewPosition = Vector2.zero;

        private bool IsSavePathReady => !string.IsNullOrEmpty(m_savePath);

        [MenuItem("NobunAtelier/Script Generator")]
        public static void ShowWindow()
        {
            GetWindow<DataScriptGenerator>("NobunAtelier Script Generator");
        }

        private void OnEnable()
        {
            m_dataDefinitionTypes.Add(typeof(DataDefinition));
            m_dataDefinitionTypes.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(DataDefinition)) && !type.IsGenericType)
                .ToArray());

            m_typeNames = m_dataDefinitionTypes.Select(type => type.Name).ToArray();
            m_selectedTypeIndex = Array.IndexOf(m_typeNames, typeof(DataDefinition).Name);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Data Definition Generator", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            {
                m_className = EditorGUILayout.TextField("Class Name", m_className);
                m_selectedTypeIndex = EditorGUILayout.Popup("Parent Type", m_selectedTypeIndex, m_typeNames);
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_savePath = EditorGUILayout.TextField("Save Path", m_savePath, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
                    {
                        ChooseSavePath();
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_isPreviewReady = false;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Preview"))
                {
                    GeneratePreview();
                }

                using (new EditorGUI.DisabledScope(!m_isPreviewReady || !IsSavePathReady))
                {
                    if (GUILayout.Button("Generate Scripts"))
                    {
                        GenerateScripts();
                    }
                }
            }

            if (m_isPreviewReady)
            {
                m_scrollviewPosition = EditorGUILayout.BeginScrollView(m_scrollviewPosition);
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        EditorGUILayout.LabelField(@"Preview", EditorStyles.helpBox);

                        EditorGUI.indentLevel++;

                        EditorGUILayout.LabelField(m_scriptPath, EditorStyles.boldLabel);
                        m_scriptContent = EditorGUILayout.TextArea(m_scriptContent);
                        GUILayout.Space(10);

                        EditorGUILayout.LabelField(m_collectionScriptPath, EditorStyles.boldLabel);
                        m_collectionScriptContent = EditorGUILayout.TextArea(m_collectionScriptContent);

                        GUILayout.Space(10);
                        EditorGUILayout.LabelField(m_collectionEditorScriptPath, EditorStyles.boldLabel);
                        m_collectionEditorScriptContent = EditorGUILayout.TextArea(m_collectionEditorScriptContent);

                        if (!string.IsNullOrEmpty(m_propertyDrawerScriptPath))
                        {
                            GUILayout.Space(10);
                            EditorGUILayout.LabelField(m_propertyDrawerScriptPath, EditorStyles.boldLabel);
                            m_propertyDrawerScriptContent = EditorGUILayout.TextArea(m_propertyDrawerScriptContent);
                        }

                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void ChooseSavePath()
        {
            string defaultPath = Application.dataPath;
            m_savePath = EditorUtility.SaveFolderPanel("Choose Save Path", defaultPath, m_className);
        }

        private void GenerateScripts()
        {
            if (!string.IsNullOrEmpty(m_savePath))
            {
                File.WriteAllText(m_scriptPath, m_scriptContent);
                File.WriteAllText(m_collectionScriptPath, m_collectionScriptContent);
                File.WriteAllText(m_collectionEditorScriptPath, m_collectionEditorScriptContent);
                if (!string.IsNullOrEmpty(m_propertyDrawerScriptPath))
                {
                    File.WriteAllText(m_propertyDrawerScriptPath, m_propertyDrawerScriptContent);
                }
                AssetDatabase.Refresh();

                // Focus the generated script in the Project view if in the Assets folder.
                if (!m_scriptPath.Contains("Assets"))
                {
                    return;
                }   
                string assetPath = m_scriptPath.Substring(m_scriptPath.IndexOf("Assets"));
                UnityEngine.Object generatedScript = AssetDatabase.LoadAssetAtPath(assetPath, typeof(MonoScript));
                Selection.activeObject = generatedScript;
            }
            else
            {
                Debug.LogWarning("Save path is empty. Please choose a save path before generating the scripts.");
            }
        }

        private void GeneratePreview()
        {
            string parentType = m_typeNames[m_selectedTypeIndex];
            m_scriptContent = NamespacesString +
                string.Format(DefinitionTemplateString, m_className, parentType) + EmptyMethodString;
            m_collectionScriptContent = NamespacesString +
                string.Format(CollectionTemplateString, m_className) + EmptyMethodString;
            m_collectionEditorScriptContent = EditorNamespacesString +
                string.Format(CollectionEditorTemplateString, m_className) + EmptyMethodString;
            m_scriptPath = Path.Combine(m_savePath, m_className + "Definition.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            m_collectionScriptPath = Path.Combine(m_savePath, m_className + "Collection.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            m_editorFolderPath = Path.Combine(m_savePath, "Editor").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Directory.CreateDirectory(m_editorFolderPath);
            m_collectionEditorScriptPath = Path.Combine(m_editorFolderPath, m_className + "CollectionEditor.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Adding property drawer script if the parent type is a state definition for the state machine.
            m_propertyDrawerScriptPath = string.Empty;
            Type type = m_dataDefinitionTypes.Find(type => type.Name == parentType);
            if (type == typeof(StateDefinition) || type.IsSubclassOf(typeof(StateDefinition)))
            {
                m_propertyDrawerScriptContent = NamespacesString +
                    string.Format(DefinitionPropertyDrawerTemplateString, m_className) + EmptyMethodString;
                m_propertyDrawerScriptPath = Path.Combine(m_editorFolderPath, m_className + "DefinitionPropertyDrawer.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            m_isPreviewReady = true;
        }
    }
}