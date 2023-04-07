using System.Text;
using UnityEngine;
using UnityEngine.Pool;

// This component returns the particle system to the pool when the OnParticleSystemStopped event is received.
[RequireComponent(typeof(ParticleSystem))]
public class ReturnToPool : MonoBehaviour
{
    public ParticleSystem system;
    public IObjectPool<ParticleSystem> pool;

    void Start()
    {
        system = GetComponent<ParticleSystem>();
        var main = system.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    void OnParticleSystemStopped()
    {
        // Return to the pool
        pool.Release(system);
    }
}
