using System;
using UnityEditor;
using UnityEngine;

namespace DesertTower.Levels.Editor
{
    public static class LevelGizmos
    {
        public static bool showLabels=true, showAreas=true, showRoutes=true, showRanges=true;

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void Marker(LevelMarker marker, GizmoType type)
        {
            Color c=MarkerPresentation.ColorFor(marker); Gizmos.color=c;
            var old=Gizmos.matrix; Gizmos.matrix=marker.transform.localToWorldMatrix;
            Vector3 size=marker.Footprint;
            Gizmos.DrawWireCube(Vector3.up*size.y*.5f,size);
            Gizmos.color=new Color(c.r,c.g,c.b,.18f); Gizmos.DrawCube(Vector3.up*size.y*.5f,size);
            Gizmos.color=c; Gizmos.DrawSphere(Vector3.up*(size.y+.25f),.22f);
            Gizmos.matrix=old;
            Handles.color=c;
            Handles.ArrowHandleCap(0,marker.transform.position+Vector3.up*.15f,marker.transform.rotation,2,EventType.Repaint);
            if (showLabels) Handles.Label(marker.transform.position+Vector3.up*(size.y+.65f),marker.label);
            if (marker.kind==MarkerKind.EnemySpawn) Handles.DrawWireDisc(marker.transform.position+Vector3.up*.05f,Vector3.up,marker.spawnRadius);
            if (showRanges && (type&GizmoType.Selected)!=0 && marker.kind==MarkerKind.InitialFacility)
            {
                Handles.DrawWireDisc(marker.transform.position,Vector3.up,marker.PreviewRange);
                Handles.Label(marker.transform.position+Vector3.right*marker.PreviewRange,"거리 표시 · 시야는 별도 확인");
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void Area(LevelArea area, GizmoType type)
        {
            if (!showAreas || area.vertices.Count<3) return;
            Color c=area.kind==AreaKind.NoBuild ? new Color(1,.25f,.2f) : area.kind==AreaKind.Combat ? new Color(1,.8f,.2f) : new Color(.1f,.85f,.9f);
            Handles.color=c;
            for (int i=0;i<area.vertices.Count;i++)
            {
                var a=area.WorldVertex(i)+Vector3.up*.08f; var b=area.WorldVertex((i+1)%area.vertices.Count)+Vector3.up*.08f;
                Handles.DrawAAPolyLine(3,a,b);
                if ((type&GizmoType.Selected)!=0)
                { Handles.DrawDottedLine(a-Vector3.up*area.height*.5f,a+Vector3.up*area.height*.5f,4); }
            }
            var min=area.vertices[0]; var max=min;
            foreach (var v in area.vertices) { min=Vector2.Min(min,v); max=Vector2.Max(max,v); }
            float step=Mathf.Max(1,Mathf.Max(max.x-min.x,max.y-min.y)/30);
            for (float x=min.x;x<=max.x;x+=step) for (float z=min.y;z<=max.y;z+=step)
                if (LevelArea.ContainsXZ(area.vertices,new Vector2(x,z)))
                { var p=area.transform.TransformPoint(new Vector3(x,.08f,z)); Handles.DrawLine(p-Vector3.right*.12f,p+Vector3.right*.12f); }
            Gizmos.color=c; Gizmos.DrawSphere(area.transform.position,.2f);
            if (showLabels) Handles.Label(area.transform.position+Vector3.up*.4f,area.label);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void Route(LevelRoute route, GizmoType type)
        {
            if (!showRoutes) return;
            var points=route.WorldPoints(); Handles.color=new Color(1,.48f,.15f);
            for (int i=0;i<points.Count-1;i++)
            {
                var a=points[i]+Vector3.up*.3f; var b=points[i+1]+Vector3.up*.3f;
                Handles.DrawDottedLine(a,b,5);
                if ((b-a).sqrMagnitude>.01f) Handles.ArrowHandleCap(0,Vector3.Lerp(a,b,.55f),Quaternion.LookRotation(b-a),1.3f,EventType.Repaint);
            }
            Gizmos.color=Handles.color; Gizmos.DrawSphere(route.transform.position,.15f);
        }
    }

    [CustomEditor(typeof(LevelMarker))]
    public sealed class MarkerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var marker=(LevelMarker)target;
            EditorGUILayout.HelpBox("마커는 배치 데이터입니다. 프리팹을 할당하지 않아도 편집할 수 있습니다. 초기 시설은 자유 설치 슬롯과 별개입니다.",MessageType.Info);
            if (GUILayout.Button("지면에 맞추기"))
            {
                var root=marker.GetComponentInParent<LevelRoot>();
                if (root && LevelValidation.Ground(marker.transform.position,root,out var hit,50,100))
                { Undo.RecordObject(marker.transform,"Ground marker"); marker.transform.position=hit.point; }
            }
            if (GUILayout.Button("복제한 마커의 ID 새로 만들기"))
            { Undo.RecordObject(marker,"New marker ID"); marker.id=Guid.NewGuid().ToString("N"); }
        }
    }

    [CustomEditor(typeof(LevelArea))]
    public sealed class AreaInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI() { DrawDefaultInspector(); EditorGUILayout.HelpBox("Scene에서 꼭짓점을 이동할 수 있습니다. 영역은 평면이며 Height 안의 바닥에만 적용됩니다.",MessageType.Info); }
        void OnSceneGUI()
        {
            var area=(LevelArea)target;
            for (int i=0;i<area.vertices.Count;i++)
            {
                var world=area.WorldVertex(i); EditorGUI.BeginChangeCheck();
                var moved=Handles.PositionHandle(world,area.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                { Undo.RecordObject(area,"Move area vertex"); var p=area.transform.InverseTransformPoint(moved); area.vertices[i]=new Vector2(p.x,p.z); }
            }
        }
    }

    [CustomEditor(typeof(LevelRoute))]
    public sealed class RouteInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI() { DrawDefaultInspector(); EditorGUILayout.HelpBox("점선은 의도한 경로입니다. 실제 이동은 이동 영역 검사와 테스트에서 확인하세요.",MessageType.Info); }
        void OnSceneGUI()
        {
            var route=(LevelRoute)target;
            for(int i=0;i<route.waypoints.Count;i++)
            {
                EditorGUI.BeginChangeCheck(); var p=Handles.PositionHandle(route.transform.TransformPoint(route.waypoints[i]),Quaternion.identity);
                if(EditorGUI.EndChangeCheck()) { Undo.RecordObject(route,"Move waypoint"); route.waypoints[i]=route.transform.InverseTransformPoint(p); }
                Handles.Label(p+Vector3.up*.3f,(i+1).ToString());
            }
        }
    }
}
