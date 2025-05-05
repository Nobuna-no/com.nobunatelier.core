using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NobunAtelier.Editor
{
    public class DataScriptGenerator : EditorWindow
    {
        private readonly string NamespacesString = "using UnityEngine;\nusing NobunAtelier;\n\n";
        private readonly string EditorNamespacesString = "using UnityEditor;\nusing NobunAtelier;\nusing NobunAtelier.Editor;\n\n";
        private readonly string DefinitionTemplateString = "public class {0}Definition : {1}\n";
        private readonly string CollectionTemplateString = "[CreateAssetMenu(fileName =\"[{0}]\", menuName = \"{1}/Collection/{0}\")]\npublic class {0}Collection : DataCollection<{0}Definition>\n";
        private readonly string CollectionEditorTemplateString = "[CustomEditor(typeof({0}Collection))]\npublic class {0}CollectionEditor : DataCollectionEditor\n";
        private readonly string DefinitionPropertyDrawerTemplateString = "[CustomPropertyDrawer(typeof({0}Definition))]\npublic class  {0}DefinitionPropertyDrawer : StateDefinitionPropertyDrawer<{0}Definition, {0}Collection>\n";
        private readonly string NamespaceStartTemplate = "namespace {0}\n{{\n";
        private readonly string NamespaceEndTemplate = "\n}\n";

        private readonly string StateTemplateString = "[AddComponentMenu(\"{2}/States/{0}\")]\npublic class {0}State : {1}<{0}Definition, {0}Collection>\n";
        private readonly string StateMachineTemplateString = "[AddComponentMenu(\"{2}/States/{0} Machine\")]\npublic class {0}StateMachine : {1}<{0}Definition, {0}Collection>\n";
        private readonly string EmptyMethodString = "{\n\n}";

        // UI Elements
        private TextField m_menuNameField;
        private TextField m_classNameField;
        private TextField m_namespaceField;
        private Toggle m_useNamespaceToggle;
        private DropdownField m_parentTypeDropdown;
        private Toggle m_generateStateMachineToggle;
        private DropdownField m_stateParentTypeDropdown;
        private DropdownField m_stateMachineParentTypeDropdown;
        private TextField m_savePathField;
        private TextField m_editorPathField;
        private Toggle m_useSplitPathsToggle;
        private Button m_browseFolderButton;
        private Button m_browseEditorFolderButton;
        private Button m_generatePreviewButton;
        private Button m_generateScriptsButton;
        private VisualElement m_previewContainer;
        private ScrollView m_previewScrollView;

        // Data
        private List<Type> m_dataDefinitionTypes = new List<Type>();
        private List<string> m_typeNames = new List<string>();
        private string m_className = "MyData";
        private string m_savePath = "";
        private string m_editorPath = "";
        private string m_menuName = "NobunAtelier";
        private string m_namespace = "NobunAtelier";
        private bool m_useNamespace = false;
        private bool m_useSplitPaths = false;
        private int m_selectedTypeIndex = -1;

        private string m_definitionScriptContent;
        private string m_definitionScriptPath;
        private string m_collectionScriptContent;
        private string m_collectionScriptPath;
        private string m_propertyDrawerScriptContent;
        private string m_propertyDrawerScriptPath;
        private string m_collectionEditorScriptContent;
        private string m_collectionEditorScriptPath;
        private string m_editorFolderPath;
        private bool m_isPreviewReady;

        private bool m_isStateDefinitionChild = false;
        private bool m_generateStateMachineAndComponent = false;
        private List<Type> m_stateTypes = new List<Type>();
        private List<string> m_stateTypeNames = new List<string>();
        private List<string> m_stateMachineTypeNames = new List<string>();
        private int m_selectedStateTypeIndex = 0;
        private int m_selectedStateMachineTypeIndex = 0;
        private string m_stateScriptContent;
        private string m_stateMachineScriptContent;
        private string m_stateScriptPath;
        private string m_stateMachineScriptPath;
        private bool IsSavePathReady => !string.IsNullOrEmpty(m_savePath);
        private bool IsEditorPathReady => !m_useSplitPaths || !string.IsNullOrEmpty(m_editorPath);

        [MenuItem("NobunAtelier/Script Generator")]
        public static void ShowWindow()
        {
            GetWindow<DataScriptGenerator>("NobunAtelier Script Generator");
        }

        private void OnEnable()
        {
            // Initialize data
            InitializeData();
            
            // Create UI
            CreateUI();
        }

        private void InitializeData()
        {
            m_dataDefinitionTypes.Clear();
            m_dataDefinitionTypes.Add(typeof(DataDefinition));
            m_dataDefinitionTypes.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(DataDefinition)) && !type.IsGenericType)
                .ToArray());

            m_typeNames = m_dataDefinitionTypes.Select(type => type.Name).ToList();
            if (m_selectedTypeIndex == -1)
            {
                m_selectedTypeIndex = m_typeNames.IndexOf(typeof(DataDefinition).Name);
                m_isStateDefinitionChild = false;
                m_generateStateMachineAndComponent = false;
            }

            m_stateTypes.Clear();
            m_stateTypes.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(StateComponent)) && type.IsGenericType)
                .ToArray());
            
            m_stateTypeNames = m_stateTypes
                .Select(type => type.Name.Replace("`2", ""))
                .Where(name => !name.Contains("StateMachine"))
                .ToList();
            
            m_stateMachineTypeNames = m_stateTypes
                .Select(type => type.Name.Replace("`2", ""))
                .Where(name => name.Contains("StateMachine"))
                .ToList();
        }

        private void CreateUI()
        {
            // Set up the root visual element
            var root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingRight = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;

            // Create title
            var titleLabel = new Label("Data Definition Generator")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            };
            root.Add(titleLabel);

            // Create input fields container
            var inputContainer = new VisualElement
            {
                style =
                {
                    marginBottom = 10
                }
            };
            root.Add(inputContainer);

            // Menu Name
            m_menuNameField = new TextField("Parent Menu Name")
            {
                value = m_menuName
            };
            m_menuNameField.RegisterValueChangedCallback(evt =>
            {
                m_menuName = evt.newValue;
                m_isPreviewReady = false;
            });
            inputContainer.Add(m_menuNameField);

            // Class Name
            m_classNameField = new TextField("Class Name")
            {
                value = m_className
            };
            m_classNameField.RegisterValueChangedCallback(evt =>
            {
                m_className = evt.newValue;
                m_isPreviewReady = false;
            });
            inputContainer.Add(m_classNameField);
            
            // Namespace options
            var namespaceContainer = new VisualElement();
            namespaceContainer.style.marginTop = 5;
            inputContainer.Add(namespaceContainer);
            
            m_useNamespaceToggle = new Toggle("Use Namespace")
            {
                value = m_useNamespace
            };
            m_useNamespaceToggle.RegisterValueChangedCallback(evt =>
            {
                m_useNamespace = evt.newValue;
                m_namespaceField.style.display = m_useNamespace ? DisplayStyle.Flex : DisplayStyle.None;
                m_isPreviewReady = false;
            });
            namespaceContainer.Add(m_useNamespaceToggle);
            
            m_namespaceField = new TextField("Namespace")
            {
                value = m_namespace
            };
            m_namespaceField.style.display = m_useNamespace ? DisplayStyle.Flex : DisplayStyle.None;
            m_namespaceField.RegisterValueChangedCallback(evt =>
            {
                m_namespace = evt.newValue;
                m_isPreviewReady = false;
            });
            namespaceContainer.Add(m_namespaceField);

            // Parent Type
            m_parentTypeDropdown = new DropdownField("Parent Type", m_typeNames, m_selectedTypeIndex);
            m_parentTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                m_selectedTypeIndex = m_typeNames.IndexOf(evt.newValue);
                Type type = m_dataDefinitionTypes.Find(t => t.Name == m_typeNames[m_selectedTypeIndex]);
                m_isStateDefinitionChild = type == typeof(StateDefinition) || type.IsSubclassOf(typeof(StateDefinition));
                m_isPreviewReady = false;
                UpdateStateOptions();
            });
            inputContainer.Add(m_parentTypeDropdown);

            // State Machine options container (conditionally visible)
            var stateOptionsContainer = new VisualElement();
            stateOptionsContainer.style.paddingLeft = 20;
            stateOptionsContainer.style.display = m_isStateDefinitionChild ? DisplayStyle.Flex : DisplayStyle.None;
            inputContainer.Add(stateOptionsContainer);

            // Generate State Machine toggle
            m_generateStateMachineToggle = new Toggle("Generate State Machine code")
            {
                value = m_generateStateMachineAndComponent
            };
            m_generateStateMachineToggle.RegisterValueChangedCallback(evt =>
            {
                m_generateStateMachineAndComponent = evt.newValue;
                m_isPreviewReady = false;
                UpdateStateTypeVisibility();
            });
            stateOptionsContainer.Add(m_generateStateMachineToggle);

            // State Parent Type dropdown
            var stateTypeContainer = new VisualElement();
            stateTypeContainer.style.display = m_generateStateMachineAndComponent ? DisplayStyle.Flex : DisplayStyle.None;
            stateOptionsContainer.Add(stateTypeContainer);

            if (m_stateTypeNames.Count > 0)
            {
                m_stateParentTypeDropdown = new DropdownField("State Parent Type", m_stateTypeNames, m_selectedStateTypeIndex);
                m_stateParentTypeDropdown.RegisterValueChangedCallback(evt =>
                {
                    m_selectedStateTypeIndex = m_stateTypeNames.IndexOf(evt.newValue);
                    m_isPreviewReady = false;
                });
                stateTypeContainer.Add(m_stateParentTypeDropdown);
            }

            // State Machine Parent Type dropdown
            if (m_stateMachineTypeNames.Count > 0)
            {
                m_stateMachineParentTypeDropdown = new DropdownField("StateMachine Parent Type", m_stateMachineTypeNames, m_selectedStateMachineTypeIndex);
                m_stateMachineParentTypeDropdown.RegisterValueChangedCallback(evt =>
                {
                    m_selectedStateMachineTypeIndex = m_stateMachineTypeNames.IndexOf(evt.newValue);
                    m_isPreviewReady = false;
                });
                stateTypeContainer.Add(m_stateMachineParentTypeDropdown);
            }

            // Path options
            var pathsContainer = new VisualElement();
            pathsContainer.style.marginTop = 10;
            inputContainer.Add(pathsContainer);
            
            // Split paths toggle
            m_useSplitPathsToggle = new Toggle("Use Separate Editor Path")
            {
                value = m_useSplitPaths
            };
            m_useSplitPathsToggle.RegisterValueChangedCallback(evt =>
            {
                m_useSplitPaths = evt.newValue;
                UpdatePathFieldsVisibility();
                m_isPreviewReady = false;
            });
            pathsContainer.Add(m_useSplitPathsToggle);

            // Regular Save Path
            var savePathContainer = new VisualElement();
            savePathContainer.style.flexDirection = FlexDirection.Row;
            savePathContainer.style.marginTop = 5;
            savePathContainer.style.marginBottom = 5;
            
            m_savePathField = new TextField("Save Path")
            {
                value = m_savePath
            };
            m_savePathField.style.flexGrow = 1;
            m_savePathField.style.marginRight = 5;
            m_savePathField.RegisterValueChangedCallback(evt =>
            {
                m_savePath = evt.newValue;
                UpdateGenerateButtonState();
            });
            savePathContainer.Add(m_savePathField);

            m_browseFolderButton = new Button(() => ChooseSavePath(false))
            {
                text = "Browse"
            };
            m_browseFolderButton.style.width = 80;
            savePathContainer.Add(m_browseFolderButton);
            
            pathsContainer.Add(savePathContainer);
            
            // Editor Save Path (conditionally visible)
            var editorPathContainer = new VisualElement();
            editorPathContainer.style.flexDirection = FlexDirection.Row;
            editorPathContainer.style.marginBottom = 5;
            editorPathContainer.style.display = m_useSplitPaths ? DisplayStyle.Flex : DisplayStyle.None;
            
            m_editorPathField = new TextField("Editor Path")
            {
                value = m_editorPath
            };
            m_editorPathField.style.flexGrow = 1;
            m_editorPathField.style.marginRight = 5;
            m_editorPathField.RegisterValueChangedCallback(evt =>
            {
                m_editorPath = evt.newValue;
                UpdateGenerateButtonState();
            });
            editorPathContainer.Add(m_editorPathField);

            m_browseEditorFolderButton = new Button(() => ChooseSavePath(true))
            {
                text = "Browse"
            };
            m_browseEditorFolderButton.style.width = 80;
            editorPathContainer.Add(m_browseEditorFolderButton);
            
            pathsContainer.Add(editorPathContainer);

            // Action buttons
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.FlexStart;
            buttonContainer.style.marginBottom = 10;

            m_generatePreviewButton = new Button(GeneratePreview)
            {
                text = "Generate Preview"
            };
            m_generatePreviewButton.style.marginRight = 5;
            buttonContainer.Add(m_generatePreviewButton);

            m_generateScriptsButton = new Button(GenerateScripts)
            {
                text = "Generate Scripts"
            };
            m_generateScriptsButton.SetEnabled(m_isPreviewReady && IsSavePathReady && IsEditorPathReady);
            buttonContainer.Add(m_generateScriptsButton);

            root.Add(buttonContainer);

            // Preview container
            m_previewContainer = new VisualElement();
            m_previewContainer.style.display = m_isPreviewReady ? DisplayStyle.Flex : DisplayStyle.None;
            m_previewContainer.style.borderLeftWidth = 1;
            m_previewContainer.style.borderRightWidth = 1;
            m_previewContainer.style.borderTopWidth = 1;
            m_previewContainer.style.borderBottomWidth = 1;
            m_previewContainer.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            m_previewContainer.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            m_previewContainer.style.borderTopColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            m_previewContainer.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            m_previewContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
            m_previewContainer.style.flexGrow = 1;
            
            var previewHeader = new Label("Preview");
            previewHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewHeader.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.2f);
            previewHeader.style.paddingLeft = 5;
            previewHeader.style.paddingTop = 3;
            previewHeader.style.paddingBottom = 3;
            previewHeader.style.marginBottom = 5;
            m_previewContainer.Add(previewHeader);

            m_previewScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_previewScrollView.style.flexGrow = 1;
            m_previewContainer.Add(m_previewScrollView);

            root.Add(m_previewContainer);

            // Register callback for state options visibility
            UpdateStateOptions();
        }

        private void UpdateStateOptions()
        {
            var stateOptionsContainer = rootVisualElement.Query<VisualElement>().Where(visualElement => 
                visualElement.childCount > 0 && 
                visualElement.Children().FirstOrDefault() is Toggle toggle && 
                toggle.label == "Generate State Machine code").First();

            if (stateOptionsContainer != null)
            {
                stateOptionsContainer.style.display = m_isStateDefinitionChild ? DisplayStyle.Flex : DisplayStyle.None;
                UpdateStateTypeVisibility();
            }
        }

        private void UpdateStateTypeVisibility()
        {
            var stateTypeContainer = rootVisualElement.Query<VisualElement>().Where(visualElement =>
                visualElement.childCount > 0 &&
                visualElement.Children().FirstOrDefault() is DropdownField dropdown &&
                dropdown.label == "State Parent Type").First();

            if (stateTypeContainer != null)
            {
                stateTypeContainer.style.display = m_generateStateMachineAndComponent ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdatePathFieldsVisibility()
        {
            // Find the editor path container
            var editorPathContainer = rootVisualElement.Query<VisualElement>().Where(element => 
                element.style.display != null && 
                element.Children().FirstOrDefault() is TextField field && 
                field.label == "Editor Path").First();

            if (editorPathContainer != null)
            {
                editorPathContainer.style.display = m_useSplitPaths ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdatePreviewContainer()
        {
            m_previewContainer.style.display = m_isPreviewReady ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (m_isPreviewReady)
            {
                m_previewScrollView.Clear();
                
                // Definition Script
                AddPreviewSection(m_definitionScriptPath, m_definitionScriptContent);
                
                // Collection Script
                AddPreviewSection(m_collectionScriptPath, m_collectionScriptContent);
                
                // Collection Editor Script
                AddPreviewSection(m_collectionEditorScriptPath, m_collectionEditorScriptContent);
                
                // State Definition related scripts
                if (m_isStateDefinitionChild)
                {
                    AddPreviewSection(m_propertyDrawerScriptPath, m_propertyDrawerScriptContent);
                    
                    if (m_generateStateMachineAndComponent)
                    {
                        AddPreviewSection(m_stateScriptPath, m_stateScriptContent);
                        AddPreviewSection(m_stateMachineScriptPath, m_stateMachineScriptContent);
                    }
                }
            }
        }

        private void AddPreviewSection(string path, string content)
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;
            
            var label = new Label(path);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 3;
            container.Add(label);
            
            var textField = new TextField
            {
                multiline = true,
                value = content
            };
            textField.style.whiteSpace = WhiteSpace.Normal;
            textField.style.flexGrow = 1;
            textField.style.minHeight = 150;
            
            // Update the script content when changed in the preview
            textField.RegisterValueChangedCallback(evt =>
            {
                if (path == m_definitionScriptPath)
                    m_definitionScriptContent = evt.newValue;
                else if (path == m_collectionScriptPath)
                    m_collectionScriptContent = evt.newValue;
                else if (path == m_collectionEditorScriptPath)
                    m_collectionEditorScriptContent = evt.newValue;
                else if (path == m_propertyDrawerScriptPath)
                    m_propertyDrawerScriptContent = evt.newValue;
                else if (path == m_stateScriptPath)
                    m_stateScriptContent = evt.newValue;
                else if (path == m_stateMachineScriptPath)
                    m_stateMachineScriptContent = evt.newValue;
            });
            
            container.Add(textField);
            m_previewScrollView.Add(container);
        }

        private void UpdateGenerateButtonState()
        {
            if (m_generateScriptsButton != null)
            {
                m_generateScriptsButton.SetEnabled(m_isPreviewReady && IsSavePathReady && IsEditorPathReady);
            }
        }

        private void ChooseSavePath(bool isEditorPath)
        {
            string defaultPath = Application.dataPath;
            string title = isEditorPath ? "Choose Editor Scripts Path" : "Choose Scripts Path";
            string selectedPath = EditorUtility.SaveFolderPanel(title, defaultPath, m_className);
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (isEditorPath)
                {
                    m_editorPath = selectedPath;
                    m_editorPathField.value = selectedPath;
                }
                else
                {
                    m_savePath = selectedPath;
                    m_savePathField.value = selectedPath;
                }
                UpdateGenerateButtonState();
            }
        }

        private void GenerateScripts()
        {
            if (!string.IsNullOrEmpty(m_savePath))
            {
                WriteScript(m_definitionScriptPath, m_definitionScriptContent);
                WriteScript(m_collectionScriptPath, m_collectionScriptContent);
                WriteScript(m_collectionEditorScriptPath, m_collectionEditorScriptContent);
                if (m_isStateDefinitionChild)
                {
                    WriteScript(m_propertyDrawerScriptPath, m_propertyDrawerScriptContent);
                    
                    if (m_generateStateMachineAndComponent)
                    {
                        WriteScript(m_stateScriptPath, m_stateScriptContent);
                        WriteScript(m_stateMachineScriptPath, m_stateMachineScriptContent);
                    }
                }
                AssetDatabase.Refresh();

                // Focus the generated script in the Project view if in the Assets folder.
                if (!m_definitionScriptPath.Contains("Assets"))
                {
                    return;
                }
                string assetPath = m_definitionScriptPath.Substring(m_definitionScriptPath.IndexOf("Assets"));
                UnityEngine.Object generatedScript = AssetDatabase.LoadAssetAtPath(assetPath, typeof(MonoScript));
                Selection.activeObject = generatedScript;
            }
            else
            {
                Debug.LogWarning("Save path is empty. Please choose a save path before generating the scripts.");
            }
        }

        private static void WriteScript(string dest, string content)
        {
            if (!string.IsNullOrEmpty(dest))
            {
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                
                if (!File.Exists(dest))
                {
                    File.WriteAllText(dest, content);
                }
                else
                {
                    Debug.Log($"{dest} already exists. Skipping code generation.");
                }
            }
        }

        private void GeneratePreview()
        {
            string namespaceFormat = string.Empty;
            try
            {
                namespaceFormat = string.Format(NamespaceStartTemplate, m_namespace);
            }
            catch (FormatException)
            {
                Debug.LogError("Invalid namespace format. Please check the namespace field.");
                return;
            }

            // Base scripts content with namespace if needed
            string namespacePrefix = m_useNamespace ? namespaceFormat : string.Empty;
            string namespaceSuffix = m_useNamespace ? NamespaceEndTemplate : string.Empty;
            
            string parentType = m_typeNames[m_selectedTypeIndex];
            string definitionCode = string.Format(DefinitionTemplateString, m_className, parentType) + EmptyMethodString;
            m_definitionScriptContent = NamespacesString + namespacePrefix + definitionCode + namespaceSuffix;
            
            string collectionCode = string.Format(CollectionTemplateString, m_className, m_menuName) + EmptyMethodString;
            m_collectionScriptContent = NamespacesString + namespacePrefix + collectionCode + namespaceSuffix;
            
            string collectionEditorCode = string.Format(CollectionEditorTemplateString, m_className) + EmptyMethodString;
            m_collectionEditorScriptContent = EditorNamespacesString + namespacePrefix + collectionEditorCode + namespaceSuffix;
            
            // Setup paths for regular scripts
            m_definitionScriptPath = Path.Combine(m_savePath, m_className + "Definition.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            m_collectionScriptPath = Path.Combine(m_savePath, m_className + "Collection.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // Setup paths for editor scripts
            if (m_useSplitPaths)
            {
                m_editorFolderPath = m_editorPath;
            }
            else
            {
                m_editorFolderPath = Path.Combine(m_savePath, "Editor").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            
            m_collectionEditorScriptPath = Path.Combine(m_editorFolderPath, m_className + "CollectionEditor.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Adding property drawer script if the parent type is a state definition for the state machine.
            if (m_isStateDefinitionChild)
            {
                // Generate custom property drawer for State Definition
                string propertyDrawerCode = string.Format(DefinitionPropertyDrawerTemplateString, m_className) + EmptyMethodString;
                m_propertyDrawerScriptContent = EditorNamespacesString + namespacePrefix + propertyDrawerCode + namespaceSuffix;
                m_propertyDrawerScriptPath = Path.Combine(m_editorFolderPath, m_className + "DefinitionPropertyDrawer.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (m_generateStateMachineAndComponent && m_stateTypeNames.Count > 0 && m_stateMachineTypeNames.Count > 0)
                {
                    string stateParentType = m_stateTypeNames[m_selectedStateTypeIndex];
                    string stateMachineParentType = m_stateMachineTypeNames[m_selectedStateMachineTypeIndex];
                    
                    string stateCode = string.Format(StateTemplateString, m_className, stateParentType, m_menuName) + EmptyMethodString;
                    m_stateScriptContent = NamespacesString + namespacePrefix + stateCode + namespaceSuffix;
                    m_stateScriptPath = Path.Combine(m_savePath, m_className + "State.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    string stateMachineCode = string.Format(StateMachineTemplateString, m_className, stateMachineParentType, m_menuName) + EmptyMethodString;
                    m_stateMachineScriptContent = NamespacesString + namespacePrefix + stateMachineCode + namespaceSuffix;
                    m_stateMachineScriptPath = Path.Combine(m_savePath, m_className + "StateMachine.cs").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
            }

            m_isPreviewReady = true;
            UpdatePreviewContainer();
            UpdateGenerateButtonState();
        }
    }
}