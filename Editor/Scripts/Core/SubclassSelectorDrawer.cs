using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private struct TypeEntry
        {
            public string DisplayName;
            public Type Type;
        }

        // Cache per field type to avoid rebuilding every frame.
        private static readonly Dictionary<Type, TypeEntry[]> s_TypeCache = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Dropdown line + inner properties (if instance exists).
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null && property.isExpanded)
            {
                var iterator = property.Copy();
                var end = iterator.GetEndProperty();

                if (iterator.NextVisible(true)) // enter children, land on first child
                {
                    do
                    {
                        if (SerializedProperty.EqualContents(iterator, end))
                            break;

                        height += EditorGUI.GetPropertyHeight(iterator, true)
                            + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (iterator.NextVisible(false));
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // --- Type dropdown ---
            var dropdownRect = new Rect(position.x, position.y,
                position.width, EditorGUIUtility.singleLineHeight);

            var entries = GetTypeEntries(GetFieldType(property));
            var currentType = property.managedReferenceValue?.GetType();

            // Build display names with "(None)" as first entry.
            var displayNames = new string[entries.Length + 1];
            displayNames[0] = "(None)";
            int selectedIndex = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                displayNames[i + 1] = entries[i].DisplayName;
                if (entries[i].Type == currentType)
                {
                    selectedIndex = i + 1;
                }
            }

            // Split into label rect (foldout) and value rect (popup).
            // Foldout must NOT extend into value area or it eats popup clicks.
            var labelRect = new Rect(dropdownRect.x, dropdownRect.y,
                EditorGUIUtility.labelWidth, dropdownRect.height);
            var valueRect = new Rect(dropdownRect.x + EditorGUIUtility.labelWidth + 2f,
                dropdownRect.y,
                dropdownRect.width - EditorGUIUtility.labelWidth - 2f,
                dropdownRect.height);

            if (property.managedReferenceValue != null)
            {
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
            }
            else
            {
                EditorGUI.LabelField(labelRect, label);
            }

            // Reset indent so popup uses valueRect as-is (indent already baked into rect position).
            var savedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(valueRect, selectedIndex, displayNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    var selectedType = entries[newIndex - 1].Type;
                    if (selectedType != currentType)
                    {
                        property.managedReferenceValue = Activator.CreateInstance(selectedType);
                    }
                }
            }

            EditorGUI.indentLevel = savedIndent;

            // --- Draw inner properties ---
            if (property.managedReferenceValue != null && property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y = dropdownRect.yMax + EditorGUIUtility.standardVerticalSpacing;

                var iterator = property.Copy();
                var end = iterator.GetEndProperty();

                if (iterator.NextVisible(true)) // enter children, land on first child
                {
                    do
                    {
                        if (SerializedProperty.EqualContents(iterator, end))
                            break;

                        float h = EditorGUI.GetPropertyHeight(iterator, true);
                        var fieldRect = new Rect(position.x, y, position.width, h);
                        EditorGUI.PropertyField(fieldRect, iterator, true);
                        y += h + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (iterator.NextVisible(false));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static Type GetFieldType(SerializedProperty property)
        {
            // managedReferenceFieldTypename format: "assembly-name TypeNamespace.TypeName"
            var typeName = property.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(typeName))
                return null;

            var splitIndex = typeName.IndexOf(' ');
            if (splitIndex < 0)
                return null;

            var assemblyName = typeName.Substring(0, splitIndex);
            var className = typeName.Substring(splitIndex + 1);

            return Type.GetType($"{className}, {assemblyName}");
        }

        private static TypeEntry[] GetTypeEntries(Type fieldType)
        {
            if (fieldType == null)
                return Array.Empty<TypeEntry>();

            if (s_TypeCache.TryGetValue(fieldType, out var cached))
                return cached;

            var entries = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => fieldType.IsAssignableFrom(t)
                    && !t.IsAbstract
                    && !t.IsInterface
                    && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .Select(t => new TypeEntry
                {
                    DisplayName = FormatTypeName(t),
                    Type = t
                })
                .ToArray();

            s_TypeCache[fieldType] = entries;
            return entries;
        }

        private static string FormatTypeName(Type type)
        {
            // Show namespace-qualified name if outside NobunAtelier, otherwise short name.
            if (type.Namespace != null
                && !type.Namespace.StartsWith("NobunAtelier")
                && type.Namespace != "")
            {
                return $"{type.Namespace}.{type.Name}";
            }

            // Insert spaces before capitals for readability: "AwaitableDrivenAbilityAction" → "Awaitable Driven Ability Action"
            return System.Text.RegularExpressions.Regex.Replace(type.Name, "(?<!^)([A-Z])", " $1");
        }
    }
}
