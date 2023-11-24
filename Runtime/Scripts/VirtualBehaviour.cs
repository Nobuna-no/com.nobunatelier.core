using UnityEngine;

public abstract class VirtualBehaviour : MonoBehaviour
{
    protected virtual void Awake()
    { }

    protected virtual void Start()
    { }

    protected virtual void OnEnable()
    { }

    protected virtual void OnDisable()
    { }

    protected virtual void OnDestroy()
    { }

    protected virtual void Update()
    { }

    protected virtual void FixedUpdate()
    { }
}