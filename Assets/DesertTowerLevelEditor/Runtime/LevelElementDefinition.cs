using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace DesertTower.Levels
{
    [MovedFrom(true, "DesertTower.Levels", "DesertTower.Levels.Runtime", "ActorDefinition")]
    [CreateAssetMenu(menuName = "Desert Tower/Level Element", fileName = "LevelElement")]
    public sealed class LevelElementDefinition : ScriptableObject
    {
        public string displayName = "Element";
        [Tooltip("Opaque key resolved by the game's own catalog. No gameplay behavior is implemented here.")]
        public string gameKey;
        [FormerlySerializedAs("prefab"), Tooltip("Authoring reference only. The editor and diagnostics do not instantiate gameplay scripts.")]
        public GameObject previewPrefab;
        public Vector3 footprint = new Vector3(2, 2, 2);
        [FormerlySerializedAs("range"), Min(0), Tooltip("Visual guide only, not authoritative attack range.")]
        public float previewRange = 8;
        [FormerlySerializedAs("color")]
        public Color previewColor = new Color(0.1f, 0.8f, 0.85f);
    }
}
