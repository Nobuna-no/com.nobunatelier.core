using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITargetable
{
    bool IsTargetable { get; }
    Transform Transform { get; }
    Vector3 Position { get; }
}
