using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Attribute for [SerializeReference] fields that adds a type-picker dropdown in the inspector.
    /// Shows all concrete types assignable to the field's declared type.
    /// </summary>
    /// <example>
    /// <code>
    /// [SerializeReference, SubclassSelector]
    /// private IAbilityActionDriver m_Driver;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class SubclassSelectorAttribute : PropertyAttribute { }
}
