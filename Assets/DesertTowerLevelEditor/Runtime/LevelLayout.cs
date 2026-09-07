using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DesertTower.Levels
{
    [Serializable]
    public sealed class MarkerRecord
    {
        public string id, label;
        public MarkerKind kind;
        public Vector3 position, rotation, footprint;
        [FormerlySerializedAs("range")] public float previewRange;
        public float spawnRadius;
        public LevelElementDefinition definition;
    }
    [Serializable]
    public sealed class AreaRecord
    {
        public string id, label;
        public AreaKind kind;
        public Vector3 position, rotation;
        public float height;
        public List<Vector2> vertices = new List<Vector2>();
    }
    [Serializable]
    public sealed class RouteRecord
    {
        public string id, label, spawnId, coreId;
        public List<Vector3> points = new List<Vector3>();
    }

    [Serializable]
    public sealed class BuildSlotRecord
    {
        public string id, label, occupancySurfaceId;
        public Vector3 position, rotation;
        public Vector2 footprint;
        public bool available = true;
        public List<string> allowedFacilityIds = new List<string>();
    }

    [CreateAssetMenu(menuName = "Desert Tower/Level Layout", fileName = "LevelLayout")]
    public sealed class LevelLayout : ScriptableObject
    {
        public int schemaVersion = 1;
        public string levelName;
        public float gridSize = 1;
        public WaveSet waves;
        public List<BuildSlotRecord> buildSlots = new List<BuildSlotRecord>();
        public List<MarkerRecord> markers = new List<MarkerRecord>();
        public List<AreaRecord> areas = new List<AreaRecord>();
        public List<RouteRecord> routes = new List<RouteRecord>();
    }
}
