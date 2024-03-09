﻿using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PoolableBehaviour : VirtualBehaviour
{
    // Called when the object is reset by the pool.
    public event Action onCreation = null;

    // Called each time the object is activated by the pool.
    public event Action onActivation = null;

    // Called each time the object is deactivated by the pool.
    public event Action onDeactivation = null;

    public int PrefabIndex
    {
        get; set;
    }

    // TODO: Need to add a release function
    // ideally getting closer to the IObjectPool API...

    // Use this value to activate and deactivate the object
    public bool IsActive
    {
        get
        {
            return gameObject.activeSelf;
        }
        set
        {
            if (IsActive == value)
                return;

            if (value)
            {
                onActivation?.Invoke();
            }
            else
            {
                onDeactivation?.Invoke();
            }

            gameObject.SetActive(value);
        }
    }

    public Vector3 Position
    {
        get
        {
            return transform.position;
        }
        set
        {
            transform.position = value;
        }
    }

    public void ResetObject()
    {
        onCreation?.Invoke();
        // Manually disable the object to not call OnDeactivation.
        gameObject.SetActive(false);
    }

    protected virtual void OnCreation()
    { }

    protected virtual void OnActivation()
    { }

    protected virtual void OnDeactivation()
    { }

    // Not reliable
    protected override void Awake()
    {
        onCreation += OnCreation;
        onActivation += OnActivation;
        onDeactivation += OnDeactivation;
    }

    // Not reliable
    // This is call when the object is instantiate.
    // We don't want to start doing it now as we don't know if the object is ready to be use...
    protected override sealed void Start()
    { }

    // Not reliable
    // This is call when the object is instantiate.
    // We don't want to start doing it now as we don't know if the object is ready to be use...
    protected override sealed void OnEnable()
    { }

    // Not reliable
    protected override sealed void OnDisable()
    { }
}