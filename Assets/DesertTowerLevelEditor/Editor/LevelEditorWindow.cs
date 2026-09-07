using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesertTower.Levels.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        enum ToolMode { Select, Marker, Area, Waypoint, Road, Prefab, Block, BuildSlot }
        [SerializeField] LevelRoot root;
        [SerializeField] int tab;
        [SerializeField] MarkerKind markerKind;
        [SerializeField] AreaKind areaKind;
        [SerializeField] LevelElementDefinition definition;
        [SerializeField] GameObject prefab;
        [SerializeField] Material material;
        [SerializeField] Vector3 blockSize=new Vector3(8,1,8);
        [SerializeField] float roadWidth=6, yaw, surfaceOffset=.03f;
        [SerializeField] bool snap=true;
        [SerializeField] LevelRoute route;
        [SerializeField] LevelMarker spawn, core;
        [SerializeField] LevelLayout layout;
        [SerializeField] int waveIndex;
        Vector2 scroll;
        ToolMode mode;
        readonly List<Vector3> draft=new List<Vector3>();
        List<LevelIssue> issues=new List<LevelIssue>();
        List<Vector3> pathPreview;
        UnityEditor.Editor inspector;
        UnityEngine.Object inspected;
        string status="레벨을 선택하거나 예제를 열어 시작하세요.";
        static readonly string[] Tabs={"지형 / 환경","마커","구역 / 경로","웨이브","검사 / 저장"};
        static readonly string[] MarkerLabels={"코어","적 스폰","플레이어 시작","부활","초기 시설","랜드마크"};

        [MenuItem("Tools/Desert Tower/Level Editor",false,1)]
        public static void Open() { var window=GetWindow<LevelEditorWindow>("Desert Tower"); window.minSize=new Vector2(430,580); window.Show(); }
        void OnEnable() { SceneView.duringSceneGui+=SceneGUI; Selection.selectionChanged+=SelectionChanged; Undo.undoRedoPerformed+=RepaintAll; SelectionChanged(); }
        void OnDisable() { SceneView.duringSceneGui-=SceneGUI; Selection.selectionChanged-=SelectionChanged; Undo.undoRedoPerformed-=RepaintAll; if(inspector) DestroyImmediate(inspector); }
        void RepaintAll() { Repaint(); SceneView.RepaintAll(); }
        void SelectionChanged()
        {
            var go=Selection.activeGameObject;
            if(go) { var selected=go.GetComponentInParent<LevelRoot>(); if(selected) root=selected; }
            Repaint();
        }
        void SetMode(ToolMode next) { mode=next; draft.Clear(); status=next==ToolMode.Select ? "선택 모드" : "Scene 바닥을 클릭 · Q/E 회전 · Backspace 마지막 점 삭제 · Esc 취소"; SceneView.RepaintAll(); }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            var title=new GUIStyle(EditorStyles.boldLabel) { fontSize=21 };
            EditorGUILayout.LabelField("DESERT TOWER",title,GUILayout.Height(30));
            EditorGUILayout.LabelField("LEVEL WORKSHOP  /  지형과 게임 규칙을 한 장면에서",EditorStyles.miniLabel);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                root=(LevelRoot)EditorGUILayout.ObjectField("편집 레벨",root,typeof(LevelRoot),true);
                if(root && EditorUtility.IsPersistent(root)) root=null;
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button("새 레벨 루트")) root=LevelAuthoring.NewRoot();
                    if(GUILayout.Button("요새 예제")) CreateExample(false);
                    if(GUILayout.Button("협곡 예제")) CreateExample(true);
                }
                if(!root)
                {
                    EditorGUILayout.HelpBox("Terrain·메시로 공간을 만들고, 마커·영역·경로로 게임 규칙을 지정합니다. 예제는 별도 장면으로 생성됩니다.",MessageType.Info);
                    return;
                }
                using(new EditorGUILayout.HorizontalScope())
                {
                    snap=GUILayout.Toggle(snap,"격자 정렬",GUILayout.Width(85));
                    EditorGUI.BeginChangeCheck(); float grid=EditorGUILayout.FloatField(root.gridSize,GUILayout.Width(50));
                    if(EditorGUI.EndChangeCheck()) { Undo.RecordObject(root,"Grid size"); root.gridSize=Mathf.Max(.1f,grid); }
                    GUILayout.Label("m",GUILayout.Width(20));
                    if(GUILayout.Button("전체 보기")) FrameRoot();
                    if(GUILayout.Button("선택 모드")) SetMode(ToolMode.Select);
                }
                int next=GUILayout.Toolbar(tab,Tabs); if(next!=tab) { tab=next; SetMode(ToolMode.Select); }
                scroll=EditorGUILayout.BeginScrollView(scroll);
                EditorGUILayout.Space(10);
                switch(tab) { case 0: TerrainPanel(); break; case 1: MarkerPanel(); break; case 2: AreasPanel(); break; case 3: WavesPanel(); break; case 4: ValidationPanel(); break; }
                EditorGUILayout.Space(14);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox(status,MessageType.None);
            }
        }

        void Heading(string title,string description)
        { EditorGUILayout.LabelField(title,EditorStyles.boldLabel); EditorGUILayout.LabelField(description,EditorStyles.wordWrappedMiniLabel); EditorGUILayout.Space(5); }
        void TerrainPanel()
        {
            Heading("01  자연 지형","산세·평탄화·표면 칠하기는 Unity Terrain의 기존 도구를 사용합니다.");
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Terrain 만들기")) Selection.activeGameObject=LevelAuthoring.NewTerrain(root).gameObject;
                if(GUILayout.Button("Terrain 편집하기"))
                {
                    var terrain=root.GetComponentInChildren<Terrain>();
                    if(terrain) { Selection.activeGameObject=terrain.gameObject; EditorGUIUtility.PingObject(terrain); }
                    else status="레벨 안에 Terrain이 없습니다. 먼저 만들어 주세요.";
                }
            }
            EditorGUILayout.Space(12);
            Heading("02  바닥 · 벽 · 단상","충돌 가능한 블록으로 공간과 카메라 여유를 먼저 확인합니다.");
            blockSize=EditorGUILayout.Vector3Field("폭 / 높이 / 길이 (m)",blockSize);
            material=(Material)EditorGUILayout.ObjectField("표면 재질",material,typeof(Material),false);
            yaw=EditorGUILayout.FloatField("회전 Y",yaw);
            if(GUILayout.Button("블록 클릭 배치")) SetMode(ToolMode.Block);
            EditorGUILayout.Space(12);
            Heading("03  길 · 경사로","지면에 점을 찍어 실제 충돌 가능한 길 메시를 만듭니다. 생성 뒤 기본 이동 영역을 다시 구우세요.");
            roadWidth=EditorGUILayout.Slider("길 폭 (m)",roadWidth,1,16);
            surfaceOffset=EditorGUILayout.Slider("지면 위 간격",surfaceOffset,.01f,.3f);
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("길 점 찍기")) SetMode(ToolMode.Road);
                using(new EditorGUI.DisabledScope(mode!=ToolMode.Road || draft.Count<2))
                    if(GUILayout.Button($"길 생성 ({draft.Count}점)")) FinishDraft();
            }
            EditorGUILayout.HelpBox("이 도구는 연결 메시를 만듭니다. Terrain을 자동으로 깎지는 않습니다. 절벽을 관통하는 길은 먼저 Terrain에서 다듬으세요. 점선은 적 경로와 별개입니다.",MessageType.Info);
            EditorGUILayout.Space(12);
            Heading("04  구조물 · 절벽 자산","블렌더 등에서 만든 프리팹을 지면에 반복 배치할 수 있습니다.");
            prefab=(GameObject)EditorGUILayout.ObjectField("배치할 프리팹",prefab,typeof(GameObject),false);
            using(new EditorGUI.DisabledScope(!prefab)) if(GUILayout.Button("프리팹 클릭 배치")) SetMode(ToolMode.Prefab);
        }

        void MarkerPanel()
        {
            Heading("위치와 역할 지정","마커는 원점이 바닥에 놓입니다. 박스는 실제 점유 크기, 화살표는 방향입니다.");
            markerKind=(MarkerKind)EditorGUILayout.Popup("종류",(int)markerKind,MarkerLabels);
            definition=(LevelElementDefinition)EditorGUILayout.ObjectField("요소 / 표시 정의 (선택)",definition,typeof(LevelElementDefinition),false);
            yaw=EditorGUILayout.FloatField("방향 Y",yaw);
            if(GUILayout.Button("Scene에서 클릭 배치",GUILayout.Height(30))) SetMode(ToolMode.Marker);
            EditorGUILayout.HelpBox("초기 시설 마커는 시작 배치용입니다. 플레이어의 자유 건설은 설치 가능 구역으로 지정합니다.",MessageType.Info);
            EditorGUILayout.Space(8);
            foreach(var marker in root.Markers)
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(MarkerLabels[(int)marker.kind],GUILayout.Width(90));
                    if(GUILayout.Button(marker.label,EditorStyles.linkLabel)) { Selection.activeGameObject=marker.gameObject; SceneView.lastActiveSceneView?.FrameSelected(); }
                }
            }
            EditorGUILayout.Space(10);
            if (GUILayout.Button("건설 슬롯 클릭 배치", GUILayout.Height(30))) SetMode(ToolMode.BuildSlot);
            EditorGUILayout.HelpBox("슬롯을 선택해 바닥 ID와 허용 시설 ID를 입력하세요. 위아래 층은 서로 다른 바닥 ID를 씁니다.", MessageType.Info);
            foreach (var slot in root.BuildSlots)
                if (GUILayout.Button(slot.label, EditorStyles.linkLabel))
                { Selection.activeGameObject = slot.gameObject; SceneView.lastActiveSceneView?.FrameSelected(); }
            SelectedInspector();
        }

        void AreasPanel()
        {
            Heading("영역 그리기","Scene 바닥을 순서대로 클릭합니다. 교차하지 않는 평면 다각형으로 마무리하세요.");
            areaKind=(AreaKind)EditorGUILayout.Popup("영역 종류",(int)areaKind,new[]{"설치 가능","설치 금지","전투 구역"});
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("영역 점 찍기")) SetMode(ToolMode.Area);
                using(new EditorGUI.DisabledScope(mode!=ToolMode.Area || draft.Count<3)) if(GUILayout.Button($"영역 완성 ({draft.Count}점)")) FinishDraft();
            }
            EditorGUILayout.Space(12);
            Heading("진격 경로","스폰과 코어를 연결한 뒤 선택 경로에 경유점을 추가합니다.");
            spawn=(LevelMarker)EditorGUILayout.ObjectField("스폰",spawn,typeof(LevelMarker),true);
            core=(LevelMarker)EditorGUILayout.ObjectField("코어",core,typeof(LevelMarker),true);
            using(new EditorGUI.DisabledScope(!spawn || !core || spawn.kind!=MarkerKind.EnemySpawn || core.kind!=MarkerKind.Core || spawn.GetComponentInParent<LevelRoot>()!=root || core.GetComponentInParent<LevelRoot>()!=root))
                if(GUILayout.Button("진격 경로 만들기")) { route=LevelAuthoring.AddRoute(root,spawn,core); Selection.activeGameObject=route.gameObject; }
            route=(LevelRoute)EditorGUILayout.ObjectField("편집 경로",route,typeof(LevelRoute),true);
            using(new EditorGUI.DisabledScope(!route || route.GetComponentInParent<LevelRoot>()!=root))
                if(GUILayout.Button("경유점 클릭 추가")) SetMode(ToolMode.Waypoint);
            EditorGUILayout.Space(8);
            foreach(var r in root.Routes)
                if(GUILayout.Button(r.label,EditorStyles.linkLabel)) { route=r; Selection.activeGameObject=r.gameObject; }
            foreach(var area in root.Areas)
                if(GUILayout.Button(area.label+"  /  "+area.kind,EditorStyles.linkLabel)) Selection.activeGameObject=area.gameObject;
            SelectedInspector();
        }

        void SelectedInspector()
        {
            var go=Selection.activeGameObject; if(!go || !go.transform.IsChildOf(root.transform)) return;
            UnityEngine.Object selected=(UnityEngine.Object)go.GetComponent<LevelMarker>() ?? go.GetComponent<LevelArea>() ?? (UnityEngine.Object)go.GetComponent<LevelRoute>() ?? go.GetComponent<LevelBuildSlot>();
            if(!selected) return;
            if(inspected!=selected) { if(inspector) DestroyImmediate(inspector); inspector=UnityEditor.Editor.CreateEditor(selected); inspected=selected; }
            EditorGUILayout.Space(10); EditorGUILayout.LabelField("선택 항목",EditorStyles.boldLabel);
            if(inspector) inspector.OnInspectorGUI();
        }

        void WavesPanel()
        {
            Heading("웨이브 구성","스폰과 목표를 지정합니다. 경로 가이드는 선택 사항이며, 실제 행동과 이동은 게임 쪽에서 결정합니다.");
            EditorGUI.BeginChangeCheck(); var waves=(WaveSet)EditorGUILayout.ObjectField("웨이브 데이터",root.waves,typeof(WaveSet),false);
            if(EditorGUI.EndChangeCheck()) { Undo.RecordObject(root,"Assign wave set"); root.waves=waves; LevelAuthoring.Dirty(root); }
            if(!root.waves)
            {
                if(GUILayout.Button("웨이브 데이터 만들기"))
                {
                    string path=EditorUtility.SaveFilePanelInProject("웨이브 저장","WaveSet","asset","저장 위치를 선택하세요.");
                    if(!string.IsNullOrEmpty(path)) { var set=CreateInstance<WaveSet>(); set.waves.Add(new Wave { label="Wave 1" }); AssetDatabase.CreateAsset(set,path); Undo.RecordObject(root,"Assign waves"); root.waves=set; LevelAuthoring.Dirty(root); }
                }
                return;
            }
            var serialized=new SerializedObject(root.waves); serialized.Update(); var array=serialized.FindProperty("waves");
            for(int i=0;i<array.arraySize;i++)
            {
                var wave=array.GetArrayElementAtIndex(i);
                using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(wave.FindPropertyRelative("label"),GUIContent.none);
                        if(GUILayout.Button("삭제",GUILayout.Width(45))) { array.DeleteArrayElementAtIndex(i); break; }
                    }
                    EditorGUILayout.PropertyField(wave.FindPropertyRelative("preparationSeconds"),new GUIContent("준비 시간"));
                    var groups=wave.FindPropertyRelative("groups");
                    for(int j=0;j<groups.arraySize;j++)
                    {
                        var group=groups.GetArrayElementAtIndex(j);
                        MarkerReference(group.FindPropertyRelative("spawnId"),MarkerKind.EnemySpawn,"스폰");
                        MarkerReference(group.FindPropertyRelative("targetId"),MarkerKind.Core,"목표 코어");
                        var available=root.Routes; var names=new List<string>{"가이드 없음 (AI가 경로 결정)"}; names.AddRange(available.Select(r=>r.label));
                        var routeProperty=group.FindPropertyRelative("routeId"); int index=Array.FindIndex(available,r=>r.id==routeProperty.stringValue)+1;
                        EditorGUI.BeginChangeCheck(); int selected=EditorGUILayout.Popup("경로 가이드 (선택)",index,names.ToArray());
                        if(EditorGUI.EndChangeCheck())
                        {
                            // Resolve legacy route-only data before clearing its guide.
                            var endpointRoute=selected>0 ? available[selected-1] : index>0 ? available[index-1] : null;
                            if(endpointRoute)
                            {
                                group.FindPropertyRelative("spawnId").stringValue=endpointRoute.spawn ? endpointRoute.spawn.id : "";
                                group.FindPropertyRelative("targetId").stringValue=endpointRoute.core ? endpointRoute.core.id : "";
                            }
                            routeProperty.stringValue=selected>0 ? available[selected-1].id : "";
                        }
                        EditorGUILayout.PropertyField(group.FindPropertyRelative("element"),new GUIContent("게임 키 / 표시 정의 (선택)"));
                        EditorGUILayout.PropertyField(group.FindPropertyRelative("count"),new GUIContent("출현 수"));
                        EditorGUILayout.PropertyField(group.FindPropertyRelative("delay"),new GUIContent("시작 지연 (초)"));
                        EditorGUILayout.PropertyField(group.FindPropertyRelative("interval"),new GUIContent("출현 간격 (초)"));
                        if(GUILayout.Button("이 그룹 삭제")) { groups.DeleteArrayElementAtIndex(j); break; }
                        EditorGUILayout.Space(6);
                    }
                    if(GUILayout.Button("출현 그룹 추가"))
                    {
                        groups.InsertArrayElementAtIndex(groups.arraySize); var g=groups.GetArrayElementAtIndex(groups.arraySize-1);
                        g.FindPropertyRelative("spawnId").stringValue=root.Markers.FirstOrDefault(m=>m.kind==MarkerKind.EnemySpawn)?.id ?? "";
                        g.FindPropertyRelative("targetId").stringValue=root.Markers.FirstOrDefault(m=>m.kind==MarkerKind.Core)?.id ?? "";
                        g.FindPropertyRelative("routeId").stringValue="";
                        g.FindPropertyRelative("element").objectReferenceValue=null; g.FindPropertyRelative("count").intValue=8;
                        g.FindPropertyRelative("delay").floatValue=0; g.FindPropertyRelative("interval").floatValue=1;
                    }
                }
            }
            if(GUILayout.Button("웨이브 추가"))
            {
                array.InsertArrayElementAtIndex(array.arraySize); var w=array.GetArrayElementAtIndex(array.arraySize-1);
                w.FindPropertyRelative("label").stringValue="Wave "+array.arraySize; w.FindPropertyRelative("preparationSeconds").floatValue=20; w.FindPropertyRelative("groups").ClearArray();
            }
            serialized.ApplyModifiedProperties();
        }

        void MarkerReference(SerializedProperty property,MarkerKind kind,string label)
        {
            var markers=root.Markers.Where(m=>m.kind==kind).ToArray();
            var names=new List<string>{string.IsNullOrEmpty(property.stringValue) ? "미지정 (기존 경로에서 읽기)" : "참조 없음: "+property.stringValue};
            names.AddRange(markers.Select(m=>m.label));
            int index=Array.FindIndex(markers,m=>m.id==property.stringValue)+1;
            EditorGUI.BeginChangeCheck(); int selected=EditorGUILayout.Popup(label,index,names.ToArray());
            if(EditorGUI.EndChangeCheck()) property.stringValue=selected>0 ? markers[selected-1].id : "";
        }

        void ValidationPanel()
        {
            Heading("이동 영역과 맵 검사","지형 변경 뒤 다시 구워 주세요. 초기 시설 마커는 굽기에 포함되지 않아 시설 차단과 기본 지형 단절을 구분합니다.");
            if(GUILayout.Button("이동 영역 굽기 + 전체 검사",GUILayout.Height(30)))
            { LevelAuthoring.Bake(root); issues=LevelValidation.Check(root); status="이동 영역과 검사를 갱신했습니다."; }
            if(GUILayout.Button("맵 검사")) issues=LevelValidation.Check(root);
            EditorGUILayout.LabelField("마지막 굽기: "+(string.IsNullOrEmpty(root.lastBakeUtc) ? "없음" : root.lastBakeUtc),EditorStyles.miniLabel);
            foreach(var issue in issues)
                using(new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(issue.message,issue.severity==IssueSeverity.Error ? MessageType.Error : issue.severity==IssueSeverity.Warning ? MessageType.Warning : MessageType.Info);
                    if(GUILayout.Button("보기",GUILayout.Width(42),GUILayout.Height(35))) { Selection.activeObject=issue.context; EditorGUIUtility.PingObject(issue.context); SceneView.lastActiveSceneView?.FrameSelected(); }
                }
            EditorGUILayout.Space(10);
            Heading("경로 미리보기","주황 점선은 경로 가이드, 청록 실선은 실제 이동 영역에서 계산한 경로입니다.");
            foreach(var r in root.Routes)
                if(GUILayout.Button(r.label+" 실제 경로 표시"))
                { if(LevelValidation.TryPath(r,out var points,out var failure)) { pathPreview=points; status="실제 경로를 표시했습니다. 지형 변경 후 다시 검사하세요."; } else { pathPreview=null; status=failure; } SceneView.RepaintAll(); }
            if(GUILayout.Button("실제 경로 표시 지우기")) { pathPreview=null; SceneView.RepaintAll(); }
            EditorGUILayout.Space(10);
            Heading("직접 이동 테스트","WASD 이동 · 우클릭 드래그 시점 · Shift 달리기 · R 시작점 복귀. 전투·시설 파괴는 별도 게임 구현 범위입니다.");
            int count=root.waves ? root.waves.waves.Count : 0;
            if(count>0) waveIndex=EditorGUILayout.Popup("테스트 웨이브",Mathf.Clamp(waveIndex,0,count-1),root.waves.waves.Select(w=>w.label).ToArray());
            if(!LevelDiagnosticService.IsAvailable) EditorGUILayout.HelpBox("이동 테스트는 선택 모듈 DesertTowerLevelDiagnostics에서 제공합니다. 에디터 배치·저장·검사는 독립적으로 사용할 수 있습니다.",MessageType.Info);
            using(new EditorGUI.DisabledScope(!LevelDiagnosticService.IsAvailable))
            if(GUILayout.Button("저장 후 Play 테스트"))
            {
                issues=LevelValidation.Check(root);
                if(issues.Any(i=>i.severity==IssueSeverity.Error) || !root.GetComponent<Unity.AI.Navigation.NavMeshSurface>() || !root.GetComponent<Unity.AI.Navigation.NavMeshSurface>().navMeshData)
                    status="오류를 수정하고 이동 영역을 구운 뒤 실행하세요.";
                else if(EditorSceneManager.SaveScene(root.gameObject.scene)) LevelDiagnosticService.TryStart(root,waveIndex);
            }
            EditorGUILayout.Space(10);
            Heading("저장과 배치 데이터","장면 저장은 지형과 마커 전체를 보관합니다. 배치 데이터는 마커·영역·경로·웨이브 참조만 보관합니다.");
            if(GUILayout.Button("장면 저장")) { EditorSceneManager.SaveScene(root.gameObject.scene); AssetDatabase.SaveAssets(); }
            if(GUILayout.Button("배치 데이터 저장"))
            {
                string path=EditorUtility.SaveFilePanelInProject("배치 데이터 저장","LevelLayout","asset","지형은 장면에 별도로 저장됩니다.");
                if(!string.IsNullOrEmpty(path)) { var data=LevelAuthoring.Capture(root); AssetDatabase.CreateAsset(data,AssetDatabase.GenerateUniqueAssetPath(path)); AssetDatabase.SaveAssets(); layout=data; }
            }
            layout=(LevelLayout)EditorGUILayout.ObjectField("가져올 배치 데이터",layout,typeof(LevelLayout),false);
            using(new EditorGUI.DisabledScope(!layout))
                if(GUILayout.Button("현재 마커 구성을 데이터로 교체 (Undo 가능)"))
                { LevelAuthoring.Restore(root,layout); status="배치를 가져왔습니다. 지형은 유지됩니다. Ctrl+Z로 되돌릴 수 있습니다."; }
            EditorGUILayout.Space(10);
            LevelGizmos.showLabels=EditorGUILayout.Toggle("이름 표시",LevelGizmos.showLabels);
            LevelGizmos.showAreas=EditorGUILayout.Toggle("구역 표시",LevelGizmos.showAreas);
            LevelGizmos.showRoutes=EditorGUILayout.Toggle("경로 가이드 표시",LevelGizmos.showRoutes);
            LevelGizmos.showRanges=EditorGUILayout.Toggle("선택 시설 사거리 표시",LevelGizmos.showRanges);
            if(GUI.changed) SceneView.RepaintAll();
            if(GUILayout.Button("에디터 Unity 패키지 내보내기…")) ExportPackage();
        }

        void FinishDraft()
        {
            if(mode==ToolMode.Area && draft.Count>=3)
            {
                if(LevelValidation.SelfIntersects(draft.Select(p=>new Vector2(p.x,p.z)).ToList())) { status="변이 서로 교차합니다. Backspace로 마지막 점을 지우세요."; return; }
                if(draft.Any(p=>Mathf.Abs(p.y-draft[0].y)>.5f)) { status="높이가 다른 바닥입니다. 평탄한 구역별로 따로 그려 주세요."; return; }
                Selection.activeGameObject=LevelAuthoring.AddArea(root,areaKind,draft).gameObject;
            }
            if(mode==ToolMode.Road && draft.Count>=2)
                Selection.activeGameObject=LevelAuthoring.Road(root,draft,roadWidth,material ? material : LevelAuthoring.Material("Sandstone",new Color(.62f,.45f,.27f))).gameObject;
            SetMode(ToolMode.Select);
        }

        bool HitGround(Vector2 mouse,out Vector3 point)
        {
            point=default; var ray=HandleUtility.GUIPointToWorldRay(mouse);
            var hits=Physics.RaycastAll(ray,2000,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            foreach(var hit in hits)
            {
                if(!hit.transform.IsChildOf(root.transform) || hit.collider.GetComponentInParent<LevelMarker>()) continue;
                point=hit.point;
                if(snap)
                {
                    point.x=Mathf.Round(point.x/root.gridSize)*root.gridSize; point.z=Mathf.Round(point.z/root.gridSize)*root.gridSize;
                    if(LevelValidation.Ground(point,root,out var aligned,1,2)) point.y=aligned.point.y;
                }
                return true;
            }
            return false;
        }

        void SceneGUI(SceneView view)
        {
            if(!root || EditorApplication.isPlaying) return;
            if(pathPreview!=null && pathPreview.Count>1) { Handles.color=Color.cyan; Handles.DrawAAPolyLine(4,pathPreview.Select(p=>p+Vector3.up*.25f).ToArray()); }
            if(mode==ToolMode.Select) return;
            Event e=Event.current;
            if(e.type==EventType.KeyDown)
            {
                if(e.keyCode==KeyCode.Escape) { SetMode(ToolMode.Select); e.Use(); Repaint(); return; }
                if(e.keyCode==KeyCode.Backspace && draft.Count>0) { draft.RemoveAt(draft.Count-1); e.Use(); RepaintAll(); }
                if(e.keyCode==KeyCode.Q || e.keyCode==KeyCode.E) { yaw+=e.keyCode==KeyCode.Q ? -15 : 15; e.Use(); RepaintAll(); }
            }
            if(e.alt || e.button==1 || e.button==2) return;
            if(e.type==EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Handles.BeginGUI(); GUI.Box(new Rect(12,12,460,28),$"{mode} · 바닥 클릭 / Esc 취소 / Backspace 점 삭제"); Handles.EndGUI();
            bool hit=HitGround(e.mousePosition,out var point);
            Handles.color=mode==ToolMode.Area ? Color.cyan : new Color(1,.7f,.2f);
            for(int i=0;i<draft.Count;i++)
            {
                Handles.SphereHandleCap(0,draft[i]+Vector3.up*.12f,Quaternion.identity,.3f,EventType.Repaint);
                if(i>0) Handles.DrawAAPolyLine(3,draft[i-1]+Vector3.up*.1f,draft[i]+Vector3.up*.1f);
            }
            if(hit)
            {
                Handles.DrawWireDisc(point+Vector3.up*.05f,Vector3.up,mode==ToolMode.Road ? roadWidth*.5f : .6f);
                if(draft.Count>0) Handles.DrawDottedLine(draft[draft.Count-1],point,4);
                if(mode==ToolMode.Marker || mode==ToolMode.Block)
                {
                    var size=mode==ToolMode.Block ? blockSize : definition ? definition.footprint : new Vector3(2,2,2);
                    using(new Handles.DrawingScope(Matrix4x4.TRS(point,Quaternion.Euler(0,yaw,0),Vector3.one))) Handles.DrawWireCube(Vector3.up*size.y*.5f,size);
                }
            }
            if(e.type==EventType.MouseMove) view.Repaint();
            if(e.type!=EventType.MouseDown || e.button!=0 || !hit) return;
            e.Use();
            switch(mode)
            {
                case ToolMode.BuildSlot:
                    var slot = LevelAuthoring.AddBuildSlot(root, point);
                    slot.transform.rotation = Quaternion.Euler(0, yaw, 0);
                    Selection.activeGameObject = slot.gameObject; break;
                case ToolMode.Marker:
                    var m=LevelAuthoring.AddMarker(root,markerKind,point,definition); m.transform.rotation=Quaternion.Euler(0,yaw,0); Selection.activeGameObject=m.gameObject; break;
                case ToolMode.Area: case ToolMode.Road:
                    if(draft.Count==0 || Vector3.Distance(draft[draft.Count-1],point)>.2f) draft.Add(point+(mode==ToolMode.Road ? Vector3.up*surfaceOffset : Vector3.zero)); break;
                case ToolMode.Waypoint:
                    if(route) { Undo.RecordObject(route,"Add waypoint"); route.waypoints.Add(route.transform.InverseTransformPoint(point)); LevelAuthoring.Dirty(root); } break;
                case ToolMode.Block:
                    if(blockSize.x<=0 || blockSize.y<=0 || blockSize.z<=0) { status="블록 크기는 양수여야 합니다."; break; }
                    var block=LevelAuthoring.Block(root,"Block",point+Vector3.up*blockSize.y*.5f,blockSize,material ? material : LevelAuthoring.Material("Sandstone",new Color(.62f,.45f,.27f)));
                    block.transform.rotation=Quaternion.Euler(0,yaw,0); break;
                case ToolMode.Prefab:
                    if(prefab) { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform); Undo.RegisterCreatedObjectUndo(go,"Place prefab"); go.transform.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0)); LevelAuthoring.Dirty(root); } break;
            }
            RepaintAll();
        }

        void FrameRoot()
        {
            var renderers=root.GetComponentsInChildren<Renderer>(); var bounds=new Bounds(root.transform.position,Vector3.one*20);
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            foreach(var t in root.GetComponentsInChildren<Terrain>()) bounds.Encapsulate(new Bounds(t.transform.position+t.terrainData.size*.5f,t.terrainData.size));
            SceneView.lastActiveSceneView?.Frame(bounds,false);
        }
        void CreateExample(bool canyon)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            root=LevelSamples.Create(canyon); SetMode(ToolMode.Select); FrameRoot();
            status="예제를 별도 장면으로 생성했습니다. 지형과 마커를 수정해 보세요.";
        }
        [MenuItem("Tools/Desert Tower/Export Editor Package…",false,50)]
        static void ExportPackage()
        {
            string path=EditorUtility.SaveFilePanel("Export level editor","","DesertTowerLevelEditor-0.2.0","unitypackage");
            if(string.IsNullOrEmpty(path)) return;
            string source=AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:MonoScript LevelEditorWindow").First());
            string folder=source.Substring(0,source.LastIndexOf("/Editor/",StringComparison.Ordinal));
            var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(folder+"/",StringComparison.Ordinal) && !p.Contains("/Tests/") && !AssetDatabase.IsValidFolder(p)).ToArray();
            AssetDatabase.ExportPackage(paths,path,ExportPackageOptions.Default);
            EditorUtility.RevealInFinder(path);
        }
    }
}
