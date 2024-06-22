using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NobunAtelier.Editor
{
    [CustomEditor(typeof(AttackAnimationDefinition))]
    public class AttackAnimationDefinitionInspectorDrawer : NestedDataDefinitionEditor<AttackAnimationDefinition>
    {
        public override IReadOnlyList<DataDefinition> TargetDefinitions => m_dataDefinitions;
        private DataDefinition[] m_dataDefinitions;

        protected override void OnEnable()
        {
            AttackAnimationDefinition attackDefinition = target as AttackAnimationDefinition;
            m_dataDefinitions = new DataDefinition[] { attackDefinition.AnimMontage };

            base.OnEnable();
        }
    }
}