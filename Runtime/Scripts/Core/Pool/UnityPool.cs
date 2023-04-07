using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace NobunAtelier
{

    // This example spans a random number of ParticleSystems using a pool so that old systems can be reused.
    public class UnityPool : MonoBehaviour
    {
        public enum PoolType
        {
            Stack,
            LinkedList
        }

        public PoolType poolType;

        // Collection checks will throw errors if we try to release an item that is already in the pool.
        public bool collectionChecks = true;
        public int maxPoolSize = 10;

        private IObjectPool<ParticleSystem> m_Pool;

        public IObjectPool<ParticleSystem> Pool
        {
            get
            {
                if (m_Pool == null)
                {
                    if (poolType == PoolType.Stack)
                    //{
                    //    m_Pool = new HashSetPool<IObjectPool<ParticleSystem>>();
                    //}
                       m_Pool = new ObjectPool<ParticleSystem>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionChecks, 10, maxPoolSize);
                    else
                       m_Pool = new LinkedPool<ParticleSystem>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionChecks, maxPoolSize);
                }
                return m_Pool;
            }
        }

        ParticleSystem CreatePooledItem()
        {
            var go = new GameObject("Pooled Particle System");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1;
            main.startLifetime = 1;
            main.loop = false;

            // This is used to return ParticleSystems to the pool when they have stopped.
            var returnToPool = go.AddComponent<ReturnToPool>();
            returnToPool.pool = Pool;

            return ps;
        }

        // Called when an item is returned to the pool using Release
        void OnReturnedToPool(ParticleSystem system)
        {
            system.gameObject.SetActive(false);
        }

        // Called when an item is taken from the pool using Get
        void OnTakeFromPool(ParticleSystem system)
        {
            system.gameObject.SetActive(true);
        }

        // If the pool capacity is reached then any items returned will be destroyed.
        // We can control what the destroy behavior does, here we destroy the GameObject.
        void OnDestroyPoolObject(ParticleSystem system)
        {
            Destroy(system.gameObject);
        }

        void OnGUI()
        {
            GUILayout.Label("Pool size: " + Pool.CountInactive);
            if (GUILayout.Button("Create Particles"))
            {
                var amount = Random.Range(1, 10);
                for (int i = 0; i < amount; ++i)
                {
                    var ps = Pool.Get();
                    ps.transform.position = Random.insideUnitSphere * 10;
                    ps.Play();
                }
            }
        }
    }
}