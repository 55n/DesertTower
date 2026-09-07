using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DesertTower.Levels
{
    [Serializable]
    public sealed class SpawnGroup
    {
        public string spawnId;
        public string targetId;
        [Tooltip("Optional guide. Legacy groups may leave spawn/target empty and resolve them from this guide.")]
        public string routeId;
        [FormerlySerializedAs("actor")] public LevelElementDefinition element;
        [Min(1)] public int count = 8;
        [Min(0)] public float delay;
        [Min(.05f)] public float interval = 1;
    }

    [Serializable]
    public sealed class Wave
    {
        public string label = "Wave";
        [Min(0)] public float preparationSeconds = 20;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }

    [CreateAssetMenu(menuName = "Desert Tower/Wave Set", fileName = "WaveSet")]
    public sealed class WaveSet : ScriptableObject
    {
        public List<Wave> waves = new List<Wave>();
    }
}
