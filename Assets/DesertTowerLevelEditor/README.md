# Desert Tower Level Workshop — 0.2.0

Unity 6용 레벨 제작 도구입니다. 요새의 모듈형 구조와 협곡의 자연 지형 위에서 같은 마커·영역·경로·웨이브 데이터를 사용합니다.

**처음 사용하는 분은 [레벨 에디터 사용 설명서](USER_GUIDE.ko.md)를 먼저 읽으세요.** 지형 제작, 프리팹 준비·배치, 마커 6종, 설치 구역, 웨이브 설정과 협곡 맵 실습을 단계별로 설명합니다.

## 시작

1. `Tools > Desert Tower > Level Editor`를 엽니다.
2. **요새 예제** 또는 **협곡 예제**를 누릅니다. 기존 수정 장면이 있으면 Unity의 저장 안내가 표시됩니다. 예제는 `Assets/DesertTowerLevels/Scenes`에 새 파일로 저장됩니다.
3. **지형 / 환경**에서 Terrain을 선택하고 Unity Inspector의 높이·평탄화·표면 칠하기 도구를 사용합니다. 블록·길 메시·외부 프리팹도 배치할 수 있습니다.
4. **마커**에서 종류를 고르고 **Scene에서 클릭 배치**를 누릅니다. Collider가 있는 레벨 내부 지면에 배치됩니다.
5. **구역 / 경로**에서 영역의 꼭짓점을 클릭한 후 **영역 완성**을 누릅니다. 스폰과 코어를 연결해 경로를 만들고 경유점을 추가합니다.
6. **웨이브**에서 경로, 적 정의, 출현 수, 지연, 간격을 설정합니다.
7. **검사 / 저장**에서 **이동 영역 굽기 + 전체 검사**를 실행합니다. 오류의 **보기** 버튼으로 해당 요소를 선택합니다.
8. **저장 후 Play 테스트**로 지형 이동과 적의 코어 도달을 확인합니다. Unity Play 모드를 종료하면 편집 장면으로 돌아옵니다.

## 조작

| 조작 | 기능 |
|---|---|
| Scene 왼쪽 클릭 | 활성 도구의 마커·프리팹·블록 배치 또는 점 추가 |
| Q / E | 배치 방향을 15도씩 회전 |
| Backspace | 아직 완성하지 않은 다각형·길의 마지막 점 삭제 |
| Esc | 현재 배치 도구 취소 |
| Ctrl+Z / Ctrl+Y | 완성된 편집 작업 되돌리기 / 다시 실행 |
| Scene 기본 이동·회전 도구 | 선택 마커, 오브젝트 이동·회전 |
| 영역·경로 선택 | Scene 핸들로 꼭짓점·경유점 이동 |
| 테스트 WASD / 우클릭 드래그 | 이동 / 시점 회전 |
| 테스트 Shift / R | 달리기 / 시작점 복귀 |

Scene 뷰의 **Gizmos**가 켜져 있어야 마커·영역·경로가 보입니다. 선택은 마커의 박스나 Hierarchy, 편집 창의 목록에서 할 수 있습니다.

## 지형과 표면

- 자연 지형은 Unity Terrain의 높이맵과 TerrainLayer로 편집합니다. 협곡 예제에는 교체 가능한 모래·암반 표면을 제공합니다.
- 수직 절벽·돌출 지형·아치는 메시 프리팹으로 보완합니다. 에디터는 외부 모델을 특정 형식이나 블록 스타일에 묶지 않습니다.
- 블록은 위치와 크기를 검증하는 회색상자 도구입니다. 실제 제작용 건축 자산은 이후 교체합니다.
- 길 도구는 클릭한 점을 연결한 표면 메시와 MeshCollider를 생성합니다. Terrain을 자동으로 깎거나 페인트하지 않습니다. 벽·지면을 관통하거나 급하게 꺾이는 길은 지형을 먼저 다듬어야 합니다.
- 길 재질의 UV는 길이를 따라 반복되며 메시는 `Assets/DesertTowerLevels/Meshes`에 저장됩니다.
- Terrain을 여러 레벨에 공유하면 원본 TerrainData 편집이 모든 참조에 반영됩니다. 다른 지형을 만들려면 TerrainData를 복제하세요. 예제 생성은 매번 고유 TerrainData를 만듭니다.

## 마커·영역·경로

- **Core / EnemySpawn / PlayerStart / Respawn / InitialFacility / Landmark**를 지원합니다.
- 마커는 위치, Y 방향, ID, 점유 크기와 선택적 LevelElementDefinition을 갖습니다. 스폰은 반경이 있는 구역입니다.
- LevelElementDefinition에는 게임 키, 편집용 외형 참조, 점유 크기, 표시용 거리를 지정합니다. 게임 속도와 실제 공격 범위는 게임의 설정이 담당합니다. 프리팹은 필수가 아니며 편집 중 실제 게임 스크립트를 실행하지 않습니다.
- 초기 시설은 시작 배치 데이터입니다. 자유 건설 슬롯이 아닙니다.
- Buildable / NoBuild / Combat 영역은 평면 다각형입니다. Height 안에서만 적용되므로 다른 층으로 규칙이 새지 않습니다. NoBuild가 Buildable보다 우선합니다.
- 마커·영역의 Scale은 1로 유지하고 크기 필드·꼭짓점으로 편집하세요. 영역 회전은 Y축만 지원합니다.
- 경로의 점선은 의도한 경유 순서입니다. 이동 검사 후 표시하는 청록 실선은 NavMesh로 계산한 실제 경로입니다.
- 굽기는 레벨 루트 아래의 물리 Collider를 수집합니다. Collider 없는 외형만으로 이동을 차단할 수 없습니다.
- 플레이어 시설의 길 완전 차단은 허용된 게임 규칙입니다. 데이터 마커는 NavMesh 굽기에 포함하지 않습니다. 실제 시설 파괴와 경로 재개는 게임 쪽에서 구현해야 합니다.
- 마커를 복제하면 ID도 복제됩니다. 검사 결과에 중복이 표시되며 Inspector의 **복제한 마커의 ID 새로 만들기**로 해결합니다. 이미 연결한 경로·웨이브 ID를 바꿀 때는 참조를 함께 갱신하세요.

## 저장과 패키지

- **장면 저장:** 지형·오브젝트·마커·구역·경로 전체. TerrainData, 재질, NavMeshData, WaveSet은 참조 자산으로 저장됩니다.
- **배치 데이터 저장:** 루트 기준 위치의 마커·영역·경로와 웨이브 참조를 LevelLayout 자산에 저장합니다. 지형·메시는 포함하지 않습니다.
- **배치 교체:** 현재 레벨의 마커·영역·경로를 데이터로 교체하고 지형을 유지합니다. 한 번의 Undo로 되돌릴 수 있습니다.
- 편집기 코드는 `Assets/DesertTowerLevelEditor` 안에 있고, 생성된 레벨은 `Assets/DesertTowerLevels`에 분리됩니다.
- `Tools > Desert Tower > Export Editor Package…`로 편집기만 `.unitypackage`로 내보냅니다. 자동 테스트와 사용자 레벨은 포함하지 않습니다. 외부 자산과 프로젝트 설정을 무작정 끌어오지 않습니다.
- 기본 에디터는 **AI Navigation 2.0.9 이상 호환 버전**을 사용합니다. Input System은 선택적 진단 모듈에서만 필요합니다. `.unitypackage` 가져오기는 UPM 의존성을 자동 설치하지 않습니다.
- 나중에 UPM으로 배포하려면 이 폴더를 독립 패키지로 옮겨 포함된 `package.json`을 사용합니다. Assets와 Packages에 중복 설치하지 않습니다.

## 게임 연결 지점과 범위

Runtime 어셈블리는 UnityEditor를 참조하지 않습니다. 게임 코드는 LevelRoot의 Markers / Areas / Routes와 WaveSet을 읽어 실제 게임 오브젝트를 생성할 수 있습니다. `LevelRoot.IsPointInBuildableZone`는 **한 점**의 구역 허용 판정이며, 시설 전체 점유·충돌·비용 판정은 게임 설치 시스템에서 추가해야 합니다.

Play 테스트는 별도 `Assets/DesertTowerLevelDiagnostics` 모듈의 **이동·크기 진단**입니다. 기본 패키지만 가져오면 테스트 버튼은 비활성화되며 나머지 편집 기능은 동작합니다. 프리팹 대신 임시 모형을 사용하고 지정 웨이브의 출현 지연·간격·이동을 시험합니다. 준비 단계, 전투, 타워 공격, 시설 파괴, 승패, 마나, 성장 시스템은 포함하지 않습니다. 사거리 원은 거리 표시이며 시야나 공격 가능 판정이 아닙니다.

검사는 필수 마커, 중복 ID, 바닥 접촉, 간단한 점유 겹침, 영역 교차, 웨이브 참조, NavMesh 연결을 확인합니다. 모든 배치 가능성과 전체 지형의 고립 구역을 증명하는 검사는 아닙니다. 초기 시설 경계 검사는 표본 기반이고 마커 상호 겹침은 축 정렬 근사이므로 회전 배치는 직접 확인하세요. 최신 이동 결과가 필요하면 지형 수정 후 다시 구워야 합니다.

## 검증

Unity Test Runner의 EditMode에 구역 층 구분, 금지 우선순위, 오목 다각형, ID·웨이브 참조, 배치 왕복, 벽 우회, 끊긴 지형 테스트가 포함되어 있습니다.

2026-09-05 / Unity 6000.2.8f1 검증 결과: 컴파일 오류 없음, EditMode 테스트 12/12 통과, 요새·협곡 두 예제에서 총 4개 스폰→코어 경로 통과, 편집 창 5개 탭 실행 오류 없음, 실제 Play 모드에서 협곡 Wave 1의 테스트 적 6/6 도달·실패 0. 자동 UI 검사는 탭을 열어 실행 오류를 확인하는 범위이며 모든 마우스 배치 조합을 자동으로 조작한 것은 아닙니다.

프로젝트 검증 기록은 `Logs/LevelEditor-tests.xml`, `Logs/LevelEditorVerification/samples.txt`, `Logs/LevelEditorVerification/interaction.txt`에 있습니다.

자동 예제 검증 진입점: `DesertTower.Levels.Editor.LevelBatchVerification.GenerateAndVerify`.

참조한 Unity API: [SceneView 편집 이벤트](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneView-duringSceneGui.html), [Terrain 높이 데이터](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TerrainData.SetHeights.html), [NavMeshSurface](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshSurface.html).

## 책임 분리와 게임 연결

현재 구조와 0.1 데이터 호환 방식은 [ARCHITECTURE.md](ARCHITECTURE.md)를 참고하세요. 경로 가이드는 선택 사항이며, 게임 EnemyAI가 실제 행동과 이동을 결정합니다. 기본 패키지와 선택적 진단 모듈은 별도로 내보낼 수 있습니다.
