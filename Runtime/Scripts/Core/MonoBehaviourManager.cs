using System.Collections;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class MonoBehaviourManager : MonoBehaviour
    {
        protected virtual void Awake() { }
        internal abstract IEnumerator Initialize();
        internal abstract void Terminate();
    }
}
