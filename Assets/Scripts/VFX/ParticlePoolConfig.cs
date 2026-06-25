using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ParticlePoolConfig", menuName = "VFX/Particle Pool Config")]
public class ParticlePoolConfig : ScriptableObject
{
    [Serializable]
    public class PoolEntry
    {
        public string id;
        public ParticleSystem prefab;
        [Min(0)] public int poolSize;
    }

    [Min(1)]
    [SerializeField] private int defaultPoolSizePerType = 20;
    [SerializeField] private List<PoolEntry> particleTypes = new List<PoolEntry>();

    public int DefaultPoolSizePerType => defaultPoolSizePerType;
    public IReadOnlyList<PoolEntry> ParticleTypes => particleTypes;
}
