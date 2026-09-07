using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace DesertTower.Levels.Editor
{
    public enum IssueSeverity { Info, Warning, Error }
    public sealed class LevelIssue
    {
        public IssueSeverity severity;
        public string message;
        public Object context;
        public LevelIssue(IssueSeverity severity, string message, Object context) { this.severity=severity; this.message=message; this.context=context; }
    }

    public static class LevelValidation
    {
        public static bool Ground(Vector3 point, LevelRoot root, out RaycastHit hit, float above = 2, float below = 4)
        {
            hit = default;
            var hits = Physics.RaycastAll(point + Vector3.up*above, Vector3.down, above+below, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            foreach (var h in hits)
            {
                if (!h.transform.IsChildOf(root.transform) || h.collider.GetComponentInParent<LevelMarker>()) continue;
                if (h.distance < nearest) { nearest=h.distance; hit=h; }
            }
            return nearest < float.MaxValue;
        }

        public static bool TryPath(LevelRoute route, out List<Vector3> corners, out string failure)
        {
            corners = new List<Vector3>(); failure = null;
            if (!route.spawn || !route.core) { failure="출현 지점 또는 코어가 없습니다."; return false; }
            return TryPath(route.WorldPoints(),out corners,out failure);
        }

        public static bool TryPath(IReadOnlyList<Vector3> points, out List<Vector3> corners, out string failure)
        {
            corners=new List<Vector3>(); failure=null;
            if(points==null || points.Count<2) { failure="시작과 목표 위치가 필요합니다."; return false; }
            for (int i=0; i<points.Count-1; i++)
            {
                if (!NavMesh.SamplePosition(points[i], out var a, 1.5f, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(points[i+1], out var b, 1.5f, NavMesh.AllAreas))
                { failure=$"구간 {i+1}: 이동 영역 밖의 경유점이 있습니다."; return false; }
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path) || path.status != NavMeshPathStatus.PathComplete)
                { failure=$"구간 {i+1}: 지형으로 길이 끊겼습니다."; return false; }
                foreach (var p in path.corners) if (corners.Count==0 || Vector3.Distance(corners[corners.Count-1],p)>.01f) corners.Add(p);
            }
            return true;
        }

        public static bool SelfIntersects(IReadOnlyList<Vector2> points)
        {
            for (int i=0;i<points.Count;i++)
                for (int j=i+1;j<points.Count;j++)
                {
                    int ni=(i+1)%points.Count, nj=(j+1)%points.Count;
                    if (i==j || ni==j || nj==i) continue;
                    var a=points[i]; var b=points[ni]; var c=points[j]; var d=points[nj];
                    float Cross(Vector2 p, Vector2 q) => p.x*q.y-p.y*q.x;
                    float abC=Cross(b-a,c-a), abD=Cross(b-a,d-a), cdA=Cross(d-c,a-c), cdB=Cross(d-c,b-c);
                    if (abC*abD<0 && cdA*cdB<0) return true;
                }
            return false;
        }

        public static List<LevelIssue> Check(LevelRoot root, bool navigation = true)
        {
            var issues=new List<LevelIssue>();
            void Add(IssueSeverity s,string m,Object obj=null) => issues.Add(new LevelIssue(s,m,obj ? obj : root));
            if (!root) { Add(IssueSeverity.Error,"레벨 루트를 선택하세요."); return issues; }
            Physics.SyncTransforms();
            var markers=root.Markers; var routes=root.Routes;
            if (root.transform.lossyScale != Vector3.one) Add(IssueSeverity.Error,"레벨 루트 Scale은 (1,1,1)이어야 합니다.");
            if (markers.Count(m=>m.kind==MarkerKind.Core)!=1) Add(IssueSeverity.Error,"코어는 정확히 1개 필요합니다.");
            if (markers.Count(m=>m.kind==MarkerKind.PlayerStart)!=1) Add(IssueSeverity.Error,"플레이어 시작점은 정확히 1개 필요합니다.");
            if (markers.Count(m=>m.kind==MarkerKind.EnemySpawn)<2) Add(IssueSeverity.Warning,"기획 기준 적 진입로는 2개입니다.");
            if (!markers.Any(m=>m.kind==MarkerKind.Respawn)) Add(IssueSeverity.Warning,"부활 지점이 없습니다.");
            var ids=new HashSet<string>();
            void Id(string id,Object obj) { if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) Add(IssueSeverity.Error,"빈 식별자 또는 중복된 식별자입니다. 복제한 요소의 ID를 갱신하세요.",obj); }
            foreach (var slot in root.BuildSlots)
            {
                Id(slot.id, slot);
                if (!slot.TryValidate(out var error)) Add(IssueSeverity.Error, error, slot);
                else if (slot.available && slot.allowedFacilityIds.Count == 0)
                    Add(IssueSeverity.Warning, "허용 시설 목록이 비어 있어 이 슬롯에는 건설할 수 없습니다.", slot);
            }
            foreach (var m in markers)
            {
                Id(m.id,m);
                if (m.transform.lossyScale != Vector3.one) Add(IssueSeverity.Error,"마커 Scale 대신 Footprint를 변경하세요.",m);
                if (m.Footprint.x<=0 || m.Footprint.y<=0 || m.Footprint.z<=0) Add(IssueSeverity.Error,"점유 크기는 양수여야 합니다.",m);
                if (!Ground(m.transform.position,root,out var hit) || Mathf.Abs(hit.point.y-m.transform.position.y)>.4f)
                    Add(IssueSeverity.Warning,"바닥과 떨어져 있거나 바닥에 파묻힌 마커입니다.",m);
                if (m.kind==MarkerKind.InitialFacility)
                {
                    bool valid=true;
                    var size=m.Footprint;
                    // Sample edges as well as corners: a footprint cannot cross a no-build stripe.
                    for (int x=0;x<=4;x++) for (int z=0;z<=4;z++)
                        valid &= root.IsPointInBuildableZone(m.transform.TransformPoint(new Vector3((x/4f-.5f)*size.x,0,(z/4f-.5f)*size.z)));
                    if (!valid) Add(IssueSeverity.Warning,"초기 시설이 설치 구역 밖이거나 금지 구역에 겹칩니다.",m);
                }
                if (m.kind==MarkerKind.EnemySpawn)
                {
                    for (int j=0;j<8;j++)
                    {
                        float angle=j*Mathf.PI/4;
                        var p=m.transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*m.spawnRadius;
                        if (!Ground(p,root,out var h) || Mathf.Abs(h.point.y-p.y)>1)
                        { Add(IssueSeverity.Warning,"스폰 영역 가장자리의 지면이 없거나 높이 차가 큽니다.",m); break; }
                    }
                }
                Vector3 half=m.Footprint*.45f;
                var overlaps=Physics.OverlapBox(m.transform.position+Vector3.up*(half.y+.15f),half,m.transform.rotation,~0,QueryTriggerInteraction.Ignore);
                if (overlaps.Any(c=>c.transform.IsChildOf(root.transform) && !(c is TerrainCollider) && c.bounds.max.y>m.transform.position.y+.3f && !c.GetComponentInParent<LevelMarker>()))
                    Add(IssueSeverity.Warning,"마커 점유 공간과 지형 구조물이 겹칩니다.",m);
            }
            for (int i=0;i<markers.Length;i++) for (int j=i+1;j<markers.Length;j++)
            {
                var a=markers[i]; var b=markers[j];
                if (a.kind==MarkerKind.Landmark || b.kind==MarkerKind.Landmark) continue;
                var boundsA=new Bounds(a.transform.position+Vector3.up*a.Footprint.y*.5f,a.Footprint);
                var boundsB=new Bounds(b.transform.position+Vector3.up*b.Footprint.y*.5f,b.Footprint);
                if (boundsA.Intersects(boundsB)) Add(IssueSeverity.Warning,$"마커 점유 공간 확인: {a.label} / {b.label} (축 정렬 근사 검사)",a);
            }
            foreach (var area in root.Areas)
            {
                Id(area.id,area);
                if (area.vertices.Count<3 || SelfIntersects(area.vertices)) Add(IssueSeverity.Error,"영역은 교차하지 않는 3개 이상의 꼭짓점이 필요합니다.",area);
                if (area.height<=0) Add(IssueSeverity.Error,"영역 높이는 양수여야 합니다.",area);
                if (area.transform.lossyScale!=Vector3.one || Vector3.Dot(area.transform.up,Vector3.up)<.999f)
                    Add(IssueSeverity.Error,"영역은 Y 회전만 사용하고 Scale을 1로 유지하세요.",area);
            }
            bool hasNav=root.GetComponent<NavMeshSurface>() && root.GetComponent<NavMeshSurface>().navMeshData;
            if (navigation && !hasNav) Add(IssueSeverity.Warning,"이동 영역을 먼저 굽고 경로 검사를 실행하세요.");
            foreach (var route in routes)
            {
                Id(route.id,route);
                if (!route.spawn || !route.core || !markers.Contains(route.spawn) || !markers.Contains(route.core))
                    Add(IssueSeverity.Error,"진격 경로의 스폰·코어가 없거나 다른 레벨 소속입니다.",route);
                else if (route.spawn.kind!=MarkerKind.EnemySpawn || route.core.kind!=MarkerKind.Core)
                    Add(IssueSeverity.Error,"경로는 EnemySpawn에서 Core로 연결해야 합니다.",route);
                else if (navigation && hasNav && !TryPath(route,out _,out var failure)) Add(IssueSeverity.Error,failure,route);
            }
            if (!root.waves || root.waves.waves.Count==0) Add(IssueSeverity.Warning,"웨이브 구성이 없습니다.");
            else foreach (var wave in root.waves.waves)
            {
                if (wave.groups.Count==0) Add(IssueSeverity.Warning,$"{wave.label}: 출현 그룹이 없습니다.",root.waves);
                foreach (var g in wave.groups)
                {
                    if (!root.TryResolveSpawnGroup(g,out var binding,out var error)) Add(IssueSeverity.Error,$"{wave.label}: {error}",root.waves);
                    else if (navigation && hasNav && !binding.SuggestedRoute &&
                        !TryPath(new[]{binding.Spawn.transform.position,binding.Target.transform.position},out _,out var failure))
                        Add(IssueSeverity.Error,$"{wave.label}: {failure}",binding.Spawn);
                    if (g.count<1 || g.delay<0 || g.interval<.05f) Add(IssueSeverity.Error,$"{wave.label}: 출현 수·시간 설정이 올바르지 않습니다.",root.waves);
                }
            }
            if (issues.Count==0) Add(IssueSeverity.Info,"검사 통과. 지형을 수정하면 이동 영역을 다시 구우세요.");
            return issues;
        }
    }
}
