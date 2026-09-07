using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DesertTower.Levels
{
    public enum MarkerKind { Core, EnemySpawn, PlayerStart, Respawn, InitialFacility, Landmark }

    [DisallowMultipleComponent]
    public sealed class LevelMarker : MonoBehaviour
    {
        public string id;
        public string label = "Marker";
        public MarkerKind kind;
        public LevelElementDefinition definition;
        public Vector3 footprint = new Vector3(2, 2, 2);
        [FormerlySerializedAs("range"), Min(0), Tooltip("Editor guide only; gameplay range belongs to the game definition.")]
        public float previewRange = 8;
        [Min(0.1f)] public float spawnRadius = 2;

        public Vector3 Footprint => definition ? definition.footprint : footprint;
        public float PreviewRange => definition ? definition.previewRange : previewRange;

        void Reset() { id = Guid.NewGuid().ToString("N"); }
        void OnValidate() { if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N"); }
    }
}
