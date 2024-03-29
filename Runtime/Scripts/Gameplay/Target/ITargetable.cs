using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [System.Serializable]
    public class TargetChangedEvent : UnityEvent<ITargetable>
    { }

    public interface ITargetable
    {
        bool IsTargetable { get; }
        Transform Transform { get; }
        Vector3 Position { get; }
    }
}