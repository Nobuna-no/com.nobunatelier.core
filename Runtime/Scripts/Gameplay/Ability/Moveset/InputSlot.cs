using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Abstract input identifier that decouples the moveset from the Input System.
    /// Player controllers map their InputActions to these slots externally.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Moveset/Input Slot")]
    public class InputSlot : ScriptableObject
    {
        [TextArea, SerializeField] private string m_Description;
    }
}
