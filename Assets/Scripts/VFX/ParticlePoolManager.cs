using System.Collections.Generic;
using UnityEngine;

public class ParticlePoolManager : MonoBehaviour
{
    public static ParticlePoolManager Instance { get; private set; }

    [SerializeField] private ParticlePoolConfig poolConfig;
    [SerializeField] private bool preloadAtSceneLoad = true;

    private readonly Dictionary<ParticleSystem, Queue<PooledParticle>> _availableByPrefab = new Dictionary<ParticleSystem, Queue<PooledParticle>>();
    private readonly Dictionary<ParticleSystem, Transform> _poolRoots = new Dictionary<ParticleSystem, Transform>();

    private bool _initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ParticlePoolManager detected. Destroying duplicate instance.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (preloadAtSceneLoad)
        {
            InitializePools();
        }
    }

    public static bool TryPlayFromPool(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        if (Instance == null)
        {
            Debug.LogWarning("No ParticlePoolManager in scene. Cannot play pooled particle effect.");
            return false;
        }

        return Instance.TryPlay(prefab, position, rotation);
    }

    public bool TryPlay(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return false;
        }

        if (!_initialized)
        {
            InitializePools();
        }

        if (!_availableByPrefab.TryGetValue(prefab, out Queue<PooledParticle> queue))
        {
            Debug.LogWarning($"Particle prefab '{prefab.name}' is not registered in the ParticlePoolConfig.", this);
            return false;
        }

        if (queue.Count == 0)
        {
            Debug.LogWarning($"Particle pool exhausted for '{prefab.name}'. Increase pool size in ParticlePoolConfig.", this);
            return false;
        }

        PooledParticle pooled = queue.Dequeue();
        pooled.Rent(position, rotation);
        return true;
    }

    public void ReturnToPool(PooledParticle pooled)
    {
        if (pooled == null || pooled.PrefabKey == null)
        {
            return;
        }

        if (!_availableByPrefab.TryGetValue(pooled.PrefabKey, out Queue<PooledParticle> queue))
        {
            Debug.LogWarning("Returned pooled particle is not tracked by this manager. Destroying instance.", pooled);
            Destroy(pooled.gameObject);
            return;
        }

        pooled.transform.SetParent(_poolRoots[pooled.PrefabKey], false);
        pooled.ResetState();
        queue.Enqueue(pooled);
    }

    [ContextMenu("Initialize Particle Pools")]
    public void InitializePools()
    {
        if (_initialized)
        {
            return;
        }

        if (poolConfig == null)
        {
            Debug.LogWarning("ParticlePoolManager is missing a ParticlePoolConfig reference.", this);
            return;
        }

        IReadOnlyList<ParticlePoolConfig.PoolEntry> entries = poolConfig.ParticleTypes;
        for (int i = 0; i < entries.Count; i++)
        {
            ParticlePoolConfig.PoolEntry entry = entries[i];
            if (entry.prefab == null)
            {
                continue;
            }

            int poolSize = entry.poolSize > 0 ? entry.poolSize : poolConfig.DefaultPoolSizePerType;
            if (poolSize <= 0)
            {
                poolSize = 1;
            }

            Transform root = new GameObject($"Pool_{entry.prefab.name}").transform;
            root.SetParent(transform, false);

            Queue<PooledParticle> queue = new Queue<PooledParticle>(poolSize);
            _availableByPrefab.Add(entry.prefab, queue);
            _poolRoots.Add(entry.prefab, root);

            for (int p = 0; p < poolSize; p++)
            {
                ParticleSystem instance = Instantiate(entry.prefab, root);
                PooledParticle pooled = instance.GetComponent<PooledParticle>();
                if (pooled == null)
                {
                    pooled = instance.gameObject.AddComponent<PooledParticle>();
                }

                pooled.Initialize(this, entry.prefab);
                queue.Enqueue(pooled);
            }
        }

        _initialized = true;
    }
}
