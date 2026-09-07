# 책임과 의존성 — 0.2.0

레벨은 공간과 배치 의도를 제공하고, 게임이 그것을 해석해 실행합니다. 에디터가 EnemyAI에 이동 명령을 보내지 않습니다.

| 구성 | 소유하는 책임 | 소유하지 않는 책임 |
|---|---|---|
| LevelRoot / Marker / Area / Route / Layout | 위치·방향·구역·식별자·선택적 경로, 배치 저장 | 생성, 이동, 공격, 자원 소비 |
| WaveSet / SpawnGroup | 출현 스케줄 데이터, 스폰·목표·게임 키 참조 | 시간 진행, 실제 적 생성, 웨이브 승패 |
| LevelElementDefinition | 게임 카탈로그 조회용 gameKey, 점유 크기, 편집용 외형·거리 표시 | 이동 속도, 공격력, 실제 사거리, EnemyAI |
| Editor | 편집·저장·NavMesh 진단·오류 표시 | 플레이 중 적 조종 |
| Optional Diagnostics | 임시 모형과 이동 테스트 | 실제 게임의 기본 구현 |
| 게임 프로젝트 (연결 예정) | WaveDirector, 생성기, EnemyAI, 이동 컴포넌트, 설치 시스템 | 편집기 내부 구현 |

## 어셈블리

```text
DesertTower.Levels.Runtime
  └─ Unity 기본 데이터 타입만 사용

DesertTower.Levels.Editor
  ├─ DesertTower.Levels.Runtime
  └─ AI Navigation (편집·진단)

DesertTower.Levels.Diagnostics                 [선택, UNITY_EDITOR 컴파일 조건]
  ├─ DesertTower.Levels.Runtime
  └─ Input System

DesertTower.Levels.Diagnostics.Editor          [선택]
  ├─ DesertTower.Levels.Diagnostics
  └─ DesertTower.Levels.Editor

게임의 EnemyAI / 생성기 / 설치 시스템
  └─ DesertTower.Levels.Runtime를 읽어 사용
```

Runtime 데이터는 Input System, NavMesh, UnityEditor, Diagnostics를 참조하지 않습니다. 에디터는 `LevelDiagnosticService`의 선택적 실행 핸들러만 알고, 진단 모듈의 타입을 참조하지 않습니다. 진단 모듈을 제거해도 데이터·편집·저장·NavMesh 검사는 유지됩니다. 진단 컴포넌트 어셈블리는 UNITY_EDITOR 컴파일 조건을 사용해 Play 모드에서 부착 가능하게 하고, 플레이어 빌드에서는 제외합니다. 진단 런처 어셈블리는 Editor 플랫폼 전용입니다.

## 게임에서 데이터 읽기

```csharp
// Game-owned wave/spawn code. This call resolves references; it creates no objects.
if (level.TryResolveSpawnGroup(group, out var binding, out var error))
{
    var spawnPosition = binding.Spawn.transform.position;
    var spawnRotation = binding.Spawn.transform.rotation;
    var spawnRadius = binding.Spawn.spawnRadius;
    var target = binding.Target;
    var optionalGuide = binding.SuggestedRoute;
    var gameKey = group.element ? group.element.gameKey : null;

    // Your catalog resolves gameKey to your enemy prefab/configuration.
    // Your factory spawns it, then your EnemyAI receives the target.
    // EnemyAI may consume optionalGuide or choose its own route.
}
```

`TryResolveSpawnGroup`는 기존 ID 참조를 해결하는 순수한 공간 데이터 조회입니다. NavMesh가 없어도 사용할 수 있습니다. 경로를 지정하지 않을 때는 `spawnId`와 `targetId`를 설정합니다. 기존 v0.1처럼 `routeId`만 있는 데이터는 해당 경로의 양 끝을 읽습니다. 직접 지정한 스폰·목표와 가이드가 충돌하면 오류를 반환하며 몰래 다른 입구로 바꾸지 않습니다.

EnemyAI는 코어 진격, 플레이어 추적, 차단 시설 공격을 판단합니다. 실제 위치 갱신은 게임이 선택한 이동 컴포넌트가 맡습니다. 이 패키지는 특정 EnemyAI 인터페이스나 NavMeshAgent 사용을 강제하지 않습니다.

`IsPointInBuildableZone(point)`는 설치 구역에 대한 **한 점의 포함 여부**만 반환합니다. 게임의 설치 시스템이 시설의 전체 크기, 점유, 캐릭터 충돌, 마나, 개수 제한 등을 추가로 검사해야 합니다.

## 선택적 진단 모듈

별도 폴더 `Assets/DesertTowerLevelDiagnostics`에 있습니다.

- `LevelPlaytest`: 진단 세션 구성과 결과 UI.
- `DiagnosticWaveRunner`: 테스트 출현 스케줄과 집계.
- `DiagnosticNavigator`: NavMesh 테스트 모형의 경유점 이동.
- `DiagnosticWalkthrough`: 검증용 플레이어 이동 입력.
- `DiagnosticCamera`: 검증용 카메라와 카메라 충돌.
- `DiagnosticVisuals`: 임시 표시 모형과 재질 수명.
- `DiagnosticSettings`: 테스트 모형의 속도·크기·시간 제한 등. 게임 설정과 별개.

생성기는 실제 프리팹 스크립트를 실행하지 않습니다. 게임 EnemyAI를 테스트하려면 게임이 준비된 뒤 그 생성기와 AI를 호출하는 별도의 통합 테스트를 연결하세요.

## 0.1 → 0.2 호환성

- 원본 씬과 웨이브 자산의 ID를 유지합니다. 기존 routeId만 있는 출현 그룹도 그대로 읽습니다.
- ActorDefinition은 LevelElementDefinition으로 변경했습니다. MonoScript GUID를 유지하고 MovedFrom을 지정했습니다.
- `prefab / range / color / actor` 직렬화 필드는 `previewPrefab / previewRange / previewColor / element`로 읽히도록 FormerlySerializedAs를 적용했습니다. 실제 Unity 자산·프리팹 재가져오기로 확인합니다.
- 기존 `moveSpeed`는 더 이상 레벨 요소의 속성이 아닙니다. 이 프로젝트에는 사용자 ActorDefinition 자산이 없었습니다. 외부 0.1 자산에 속도를 작성했다면 게임 설정으로 옮겨야 합니다. 진단 속도의 기본값은 3m/s입니다.
- C# API `CanBuild`는 `IsPointInBuildableZone`으로 변경했습니다. 이전 소스가 있다면 호출 이름을 수정합니다.
- LevelLayout의 기존 schemaVersion 1은 계속 지원합니다. 배치 데이터 범위는 이전과 같습니다.
- 기본 에디터 내보내기에는 진단 모듈이 포함되지 않습니다. 필요하면 선택적 진단 패키지를 별도로 내보냅니다.

## 0.2 검증 기록

Unity 6000.2.8f1 / 2026-09-05:

- EditMode 테스트 22/22 통과. 순수 공간 데이터 조회, 경로 없는 출현, 기존 routeId 호환, 잘못된 참조, Unity 프리팹·자산 필드 이름 변경, 어셈블리 의존성, 플레이어 빌드의 진단 제외를 포함합니다.
- 기본 에디터만 있는 별도 프로젝트에서 Input System 없이 컴파일하고 협곡 예제의 두 경로를 검사했습니다.
- 같은 별도 프로젝트에 선택 진단 소스가 있지만 Input System이 없는 경우에도 진단이 비활성화되고 기본 에디터 검사가 통과했습니다.
- 실제 Play 모드에서 협곡 Wave 1 테스트 모형 6/6 도달, 실패 0. 다섯 에디터 탭 실행 오류 없음.
- 변경 전 해시와 비교해 기존 레벨 관련 씬·자산 30개 파일의 내용이 유지됐습니다.

검증 결과: `Logs/LevelEditor-architecture-tests.xml`, `Logs/LevelEditor-architecture-play-v2.log`, `Logs/LevelEditor-core-only.log`, `Logs/LevelEditor-no-input-addon.log`. 독립 프로젝트는 `Logs/CoreOnlyValidation`이며 배포 패키지에 포함되지 않습니다.
