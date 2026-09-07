using UnityEngine;

namespace DesertTower.Levels
{
    [DisallowMultipleComponent]
    public sealed class LevelRoot : MonoBehaviour
    {
        public string levelName = "New Level";
        [Min(0.1f)] public float gridSize = 1;
        public WaveSet waves;
        [Tooltip("Set after a navigation bake; geometry changes require another bake.")]
        public string lastBakeUtc;

        /// <summary>이 레벨에 속한 지정 건설 자리다. 자식 레벨의 슬롯은 포함하지 않는다.</summary>
        public LevelBuildSlot[] BuildSlots => System.Array.FindAll(
            GetComponentsInChildren<LevelBuildSlot>(true), s => s.GetComponentInParent<LevelRoot>() == this);

        public LevelMarker[] Markers => GetComponentsInChildren<LevelMarker>(true);
        public LevelArea[] Areas => GetComponentsInChildren<LevelArea>(true);
        public LevelRoute[] Routes => GetComponentsInChildren<LevelRoute>(true);

        public LevelRoute FindRoute(string id)
        {
            foreach (var route in Routes) if (route.id == id) return route;
            return null;
        }

        /// <summary>Point-in-zone query only. Does not check cost, occupancy or facility footprint.</summary>
        public bool IsPointInBuildableZone(Vector3 worldPoint)
        {
            bool allowed = false;
            foreach (var area in Areas)
            {
                if (!area.isActiveAndEnabled || !area.Contains(worldPoint)) continue;
                if (area.kind == AreaKind.NoBuild) return false;
                if (area.kind == AreaKind.Buildable) allowed = true;
            }
            return allowed;
        }

        public LevelMarker FindMarker(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var marker in Markers) if (marker.id == id) return marker;
            return null;
        }

        /// <summary>Resolve authored references only. The caller owns spawning, AI and movement.</summary>
        public bool TryResolveSpawnGroup(SpawnGroup group, out SpawnBinding binding, out string error)
        {
            binding=default; error=null;
            if (group==null) { error="출현 그룹이 없습니다."; return false; }
            LevelRoute guide=null;
            if (!string.IsNullOrWhiteSpace(group.routeId))
            {
                guide=FindRoute(group.routeId);
                if (!guide) { error="존재하지 않는 경로를 참조합니다."; return false; }
            }
            // Existing v0.1 wave assets resolve via routeId without a destructive migration.
            var spawn=string.IsNullOrWhiteSpace(group.spawnId) ? (guide ? guide.spawn : null) : FindMarker(group.spawnId);
            var target=string.IsNullOrWhiteSpace(group.targetId) ? (guide ? guide.core : null) : FindMarker(group.targetId);
            if (!spawn || spawn.kind!=MarkerKind.EnemySpawn || spawn.GetComponentInParent<LevelRoot>()!=this)
            { error="유효한 스폰 마커를 지정하세요."; return false; }
            if (!target || target.kind!=MarkerKind.Core || target.GetComponentInParent<LevelRoot>()!=this)
            { error="유효한 코어 마커를 지정하세요."; return false; }
            if (guide && (guide.spawn!=spawn || guide.core!=target))
            { error="스폰·목표와 선택한 경로의 양 끝이 다릅니다."; return false; }
            binding=new SpawnBinding(spawn,target,guide); return true;
        }
    }
}
