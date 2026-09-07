2026-09-07 후속 결정: 단계별 슬롯/자유 배치, 포인트 선택형 성장, 코어 흡수/공격 전환은 [최신 결정 기록](E:/AI/Projects/DesertTower/Docs/Desert_Tower_결정기록_단계별배치_선택성장_코어전환_v0.1.md)을 따른다. 아래의 해당 항목은 이전 설계이며, 그 밖의 기존 계약은 유지한다.

2026-09-06 반영 안내: 예약·조회·이동·차단 계약은 승인안으로 갱신했다. 이 문서의 다른 초기 제안과 현재 코드가 다를 수 있으며, 최신 기준은 [인터페이스 개선 반영 v0.4](Desert_Tower_인터페이스_개선_반영_v0.4.md)다.

# DESERT TOWER 최적화를 위한 인터페이스 제안

버전: 0.1
작성일: 2026-09-06
기준 문서: Desert_Tower_컴포넌트_목록_v0.1, Desert_Tower_IDamageable_설계_v0.1
문서 성격: 구현 전 계약 제안. 시그니처만 있고 구현은 없다. 컴포넌트 목록의 이름을 그대로 쓴다.

---

## 0. 최적화의 두 축

이 문서에서 "최적화"는 다음 둘을 뜻한다.

| 축 | 문제 | 인터페이스로 해결하는 방식 |
|---|---|---|
| 실행 비용 | 적 50~100, 하수인 10, 투사체 수십, 타워 수십이 매 프레임 서로를 찾고, 레이캐스트하고, 경로를 계산한다 | 조회를 한 곳에 모아 공간 분할·캐시·예산으로 제어한다. 개체는 조회 결과만 받는다 |
| 변경 비용 | 적 AI, 타워, 하수인, 마법이 각자 Physics·NavMesh·GetComponent를 직접 호출하면 규칙을 바꿀 때 전부 고쳐야 하고 EditMode 테스트가 불가능하다 | Unity API를 만지는 자리를 인터페이스 뒤로 옮기고, 판단 로직은 순수 C#으로 남긴다 |

두 축은 같은 인터페이스로 해결된다. 조회를 한 곳에 모으면 그곳만 최적화하면 되고, 그곳만 가짜로 바꾸면 테스트가 된다.

공통 규칙:
- 목록을 돌려주는 메서드는 호출자가 준 List를 채운다. 새 배열·List를 만들지 않는다.
- 이벤트 인자는 struct다. 클래스 인자와 람다 캡처를 피한다.
- 인터페이스는 "읽기"와 "변경"을 나눈다. UI와 AI는 읽기 인터페이스만 받는다.
- 매 프레임 판단하지 않는다. 판단 주기는 스케줄러가 준다.

---

## 1. 전투 개체와 조회

### 1.1 ICombatant — 전장에 있는 것

IDamageable은 "피해를 받을 수 있는 것"이고, ICombatant는 "전장에서 찾아지는 것"이다. 조회 시스템은 ICombatant만 안다.

```csharp
public interface ICombatant
{
    int Id { get; }                     // 등록 시 부여. 딕셔너리·집합 키. 참조 비교보다 싸다
    FactionId Faction { get; }
    Vector3 HitPoint { get; }           // 프레임당 1회 갱신된 캐시 값
    bool IsTargetable { get; }          // 생존 + 보호 아님. 조회 필터의 첫 조건
    IDamageable Damageable { get; }     // 피해를 줄 때만 꺼낸다
    Transform Transform { get; }
}
```

Health가 ICombatant와 IDamageable을 함께 구현한다. 활성화 시 ICombatantRegistry에 등록하고 Died·비활성화 시 해제한다.

### 1.2 ICombatantRegistry — 전장 조회

```csharp
public interface ICombatantRegistry
{
    void Register(ICombatant c);
    void Unregister(ICombatant c);

    // 반경 안 적대 개체를 results에 채우고 개수를 돌려준다. 거리 오름차순 정렬은 옵션.
    int QueryHostiles(FactionId from, Vector3 center, float radius,
                      List<ICombatant> results, bool sortByDistance = false);

    // 가장 가까운 적대 개체. 없으면 null. 시야 필터는 predicate로 넘긴다.
    ICombatant FindNearestHostile(FactionId from, Vector3 center, float radius,
                                  Func<ICombatant, bool> filter = null);

    ICombatant Get(int id);             // 죽었거나 해제됐으면 null
    int Count(FactionId faction);       // WaveDirector의 남은 적 집계용
}
```

구현 지침:
- 내부는 진영별 공간 해시 그리드(셀 크기 = 최대 조회 반경의 절반, 초기값 5m). 개체 이동 시 셀 갱신은 HitPoint 갱신과 같은 자리에서 한다.
- FindNearestHostile은 셀 확장 탐색으로 반경 전체를 훑지 않는다.
- Physics.OverlapSphere를 쓰지 않는다. 충돌체는 투사체 명중에만 쓴다.
- filter 델리게이트는 호출자가 필드에 캐시해 매번 할당하지 않는다.

### 1.3 IVisibilityService — 시야

```csharp
public interface IVisibilityService
{
    long WorldRevision { get; }
    VisibilityResult Query(VisibilityRequest request);
    void Invalidate();
}
```

현재 상태에서 동기로 판정한다. 캐시가 없으면 계산하고, 1단계에는 큐나 계산 예산이 없다.
기본값은 Invalid이며 Visible/Blocked와 구분한다. IsStale은 별도 메타데이터로 1단계는 항상 false다.
캐시 키에 위치·레이어·트리거 정책·관찰자/대상·월드 revision을 포함하고 이동 장애물 변화도 무효화한다.
명중 시 현재 시야를 즉시 확인한다. 위치 양자화나 오래된 긍정 결과만으로 명중을 승인하지 않는다.

### 1.4 IReachabilityService — 접근 가능성

```csharp
public interface IReachabilityService
{
    long NavigationRevision { get; }
    ReachabilityResult Query(ReachabilityRequest request);
    void Invalidate();
}
```

- Invalid/Reachable/Partial/Unreachable 판정과 EndPosition을 반환한다. 기본값은 Invalid다.
- 캐시가 없으면 동기 계산한다. RequestReach/요청 ID/공개 Pending은 없다.
- 캐시는 출발/도착, agent type, area mask, navigation revision에 의존한다.
- 내비게이션 변경이 실제 질의에 반영된 시점에 Invalidate하여 캐시를 비우고 revision을 올린다.
- 요청 revision 불일치는 현재 revision을 담은 Invalid이며 현재 입력으로 다시 질의한다.
- 1단계는 예산 없이 캐시·Think 분산으로 시작한다. IsStale은 항상 false다. 예산 도입은 프로파일 이후다.

---

## 2. 판단 주기

### 2.1 ITickable / ITickScheduler — Update 대체

MonoBehaviour Update 100개는 Unity 네이티브→관리 코드 호출 비용이 크다. 하나의 러너가 목록을 돈다.

```csharp
public interface ITickable      { void Tick(float dt); }
public interface IFixedTickable { void FixedTick(float dt); }

public interface ITickScheduler
{
    void Add(ITickable t);
    void Remove(ITickable t);
    void Add(IFixedTickable t);
    void Remove(IFixedTickable t);
}
```

Tick 도중 Add·Remove가 일어나면 다음 프레임에 반영한다(지연 목록).

### 2.2 IThinkScheduler — AI 판단 주기 분산

```csharp
public interface IThinker       { void Think(); }

public interface IThinkScheduler
{
    // interval 초마다 Think를 부른다. 등록 시 위상을 무작위로 흩어 같은 프레임에 몰리지 않게 한다.
    void Add(IThinker thinker, float interval);
    void Remove(IThinker thinker);
}
```

적용:
- EnemyBrain.Think 0.15초, TowerTargeting.Think 0.1초, MinionBrain.Think 0.2초, EnemyPerception 0.2초.
- Tick(매 프레임)에는 이동 갱신, 공격 타이머, HitPoint 캐시만 남긴다. 목표 선택·시야·경로 판단은 Think에서만 한다.
- 대상이 사망하는 등 즉시 반응이 필요한 사건은 이벤트로 받아 "다음 Think를 즉시 요청"하는 플래그만 세운다.

### 2.3 ITimeSource — 시간

```csharp
public interface ITimeSource
{
    float DeltaTime { get; }
    float Time { get; }
    bool IsPaused { get; }
}
```

타이머(공격 간격, 재소환, 준비 시간, 부활)는 전부 이 값을 쓴다. PauseService가 구현을 바꾸고, 테스트는 수동으로 시간을 밀 수 있다.

---

## 3. 이동과 공격

### 3.1 IMovementController — 이동과 경로 계약

초기 제안의 INavMover는 현재 코드의 IMovementController로 대응한다.

```csharp
public interface IMovementController
{
    Vector3 Position { get; }
    bool HasDestination { get; }
    MovementPathState PathState { get; }
    Vector3 PathEndPosition { get; }
    bool HasArrived { get; }
    bool TrySetDestination(Vector3 destination);
    void Stop();
}
```

1단계는 경로 계산·적용을 동기로 마치고 true를 반환한다. Partial도 성공이고 false는 이전 상태를 유지한다.
공개 대기 상태와 '목적지 변경 뒤 한 주기 무시' 규칙은 없다.
0.5m 이내 목적지는 기존 목적지/경로가 현재 revision에서 유효할 때만 재계산을 생략한다.
Partial 끝점 도착은 HasArrived가 아니다. Stop은 이동·목적지·경로를 제거하며 반복 호출은 무해하다.

### 3.2 IAttacker — 근접·원거리 공용

```csharp
public interface IAttacker
{
    float Range { get; }
    Vector3 Origin { get; }             // 공격 발생 지점. 시야 판정의 from
    bool IsReady { get; }               // 간격 경과
    bool IsBusy { get; }                // 준비 동작 중
    bool CanAttack(ICombatant target);  // 사거리 + 시야. 내부에서 IVisibilityService 사용
    void Attack(ICombatant target);     // 준비 동작 시작. 실제 피해는 동작 끝에서 재검사 후 적용
    void Cancel();
}
```

MeleeHit(적·하수인)과 ProjectileLauncher(플레이어·타워)가 각각 구현한다. EnemyBrain은 근접인지 원거리인지 모른다.

### 3.3 IProjectile / IPoolable — 투사체

```csharp
public interface IPoolable
{
    void OnRent();                      // 풀에서 꺼낼 때. 상태 초기화
    void OnReturn();                    // 풀로 돌아갈 때. 이벤트 구독 해제, 참조 해제
}

public interface IProjectile : IPoolable
{
    void Launch(Vector3 origin, Vector3 direction, in DamageInfo damage, float speed, float lifetime);
}

public interface IPool<T> where T : class, IPoolable
{
    T Rent();
    void Return(T item);
    int Available { get; }
}
```

구현 지침:
- 투사체는 1단계부터 풀링한다. 프레임당 수십 개 Instantiate·Destroy는 GC 스파이크의 주범이다.
- 적·하수인도 IPoolable을 구현한다. Health는 OnRent에서 상태를 초기화하고 IDamageable 설계의 "제거됨 상태는 요청 무시" 규칙을 풀 반납 후에도 지킨다(반납 = 제거됨).
- 투사체 이동은 Rigidbody가 아니라 Tick에서 SphereCast로 한다. 빠른 탄의 관통 누락이 없고 물리 단계 비용이 없다.

---

## 4. 목표 선택

### 4.1 ITargetPolicy — 규칙과 실행의 분리

기획서 4.3의 우선순위는 순수 C# 규칙이다. Unity 없이 테스트한다.

```csharp
public readonly struct TargetContext
{
    public readonly ICombatant Self;
    public readonly ICombatant Current;         // 유지 후보
    public readonly ICombatant Blocker;         // BlockerDetector 결과
    public readonly IReadOnlyList<ICombatant> Perceived;   // EnemyPerception 결과. 이미 시야·거리 필터 통과
    public readonly IReadOnlyList<ICombatant> RoadsideTowers;
    public readonly ICombatant Core;
    public readonly Vector3 Position;
    public readonly float Time;
}

public interface ITargetPolicy
{
    // 새 목표. Current를 그대로 돌려주면 유지. 할당 없음.
    ICombatant Select(in TargetContext ctx, IReachabilityService reach);
}
```

EnemyTargetSelector는 Think에서 컨텍스트를 채우고 정책에 묻는다. 강한 적이 다른 우선순위를 가지려면 정책 자산만 바꾼다.

### 4.2 IChaseLimiter — 추적 종료 판단

```csharp
public interface IChaseLimiter
{
    void BeginChase(ICombatant target, Vector3 origin, float time);
    bool ShouldAbort(Vector3 position, bool targetVisible, float time);   // 15m, 3초, 접근 불가
    void EndChase();
    bool IsBanned(ICombatant target);   // 진격 경로 복귀 전까지 재추적 금지
    void ClearBans();                   // 경로 복귀 시
}
```

### 4.3 IObstructionQuery — 차단 시설 탐색

```csharp
public interface IObstructionQuery
{
    bool TryFindBlocker(ObstructionRequest request, out ObstructionResult result);
}
```

입력은 원래 진격 경로/부분 경로 끝점/진영/탐색 반경/공격 사거리다.
결과는 진격을 막는 적대 파괴 시설과 개체 쪽 접근 가능 영역에서 도달 가능한 공격 위치다.
근처의 시설을 무조건 차단물로 선택하지 않는다. 실제 명중 검사는 공격자가 수행한다.
선택기는 원래 진격 목적지를 유지하고 차단 시설 파괴 후 그 목적지로 재판단한다.
완전 차단 배치는 허용하며 차단물 공격 정책은 TargetSelector/Brain에 둔다.

---

## 5. 시설과 설치

### 5.1 IFacility

```csharp
public interface IFacility : ICombatant
{
    FacilityDefinition Definition { get; }
    IReadOnlyList<Vector2Int> Cells { get; }   // 점유 격자. 설치 시 확정, 이후 불변
    int Rotation { get; }                      // 0..3 (90도 단위)
    event Action<IFacility> Destroyed;         // Died 뒤. 점유·장애물 해제 완료 후
}
```

### 5.2 IFacilityRegistry

```csharp
public interface IFacilityRegistry
{
    void Add(IFacility f);
    void Remove(IFacility f);
    int QueryInRadius(Vector3 center, float radius, List<IFacility> results);
    IFacility GetAtCell(Vector2Int cell);
    IReadOnlyList<IFacility> All { get; }
}
```

### 5.3 IOccupancyGrid

```csharp
public interface IOccupancyGrid
{
    bool AreFree(IReadOnlyList<Vector2Int> cells);
    void Occupy(IReadOnlyList<Vector2Int> cells, int ownerId);
    void Release(IReadOnlyList<Vector2Int> cells);
    int OwnerAt(Vector2Int cell);       // 0이면 비어 있음
}
```

내부는 Dictionary<Vector2Int, int> 또는 맵 크기가 고정이면 int[,]. 설치 구역 밖 셀은 조회하지 않는다.

### 5.4 IPlacementRule — 검사 규칙 사슬

```csharp
public readonly struct PlacementRequest
{
    public readonly FacilityDefinition Definition;
    public readonly Vector3 WorldPosition;      // 스냅 후
    public readonly int Rotation;
    public readonly IReadOnlyList<Vector2Int> Cells;
}

public enum PlacementFailure
{
    None, OutsideBuildZone, Overlap, Forbidden, CharacterInside, NotFlat, NotEnoughMana, Locked
}

public interface IPlacementRule
{
    PlacementFailure Check(in PlacementRequest req);   // None이면 통과
    int Order { get; }                                 // 싼 검사가 먼저
}

public interface IPlacementValidator
{
    PlacementFailure Validate(in PlacementRequest req);   // 첫 실패에서 중단
}
```

규칙 순서(싼 것부터): Locked → OutsideBuildZone → Overlap → Forbidden → NotEnoughMana → CharacterInside → NotFlat.
CharacterInside와 NotFlat만 물리·레이캐스트를 쓴다. 미리보기 중에는 위치가 바뀔 때만 Validate를 부르고 결과를 캐시한다.

### 5.5 INavObstacle

```csharp
public interface INavObstacle
{
    void Enable();
    void Disable();     // 파괴 즉시. IReachabilityService.Invalidate를 함께 호출
}
```

---

## 6. 경제·성장 (읽기와 변경 분리)

```csharp
public interface IManaReader
{
    int CurrentMana { get; } // 예약을 포함한 아직 소비하지 않은 총액
    int MaxMana { get; }
    event Action<ManaChangedInfo> Changed;
}

public interface IManaWallet : IManaReader
{
    bool TrySpend(int amount); // 예약액은 침범하지 않음
    bool TryReserve(int amount, out IManaReservation reservation);
    int Gain(int amount); // 예약 포함 잔액 기준 상한, 실제 증가량 반환
}

public interface IManaReservation : IDisposable
{
    bool TryCommit(); // 확정 전 Dispose는 예약만 해제, Gain 환불 금지
}

public interface IExperienceReader
{
    int Level { get; }
    int Current { get; }
    int NextLevelAt { get; }
    event Action<LevelChanged> LevelUp;
}

public interface IExperienceTracker : IExperienceReader
{
    void Add(int amount);
    void Reset();
}

public interface IUnlockReader
{
    bool IsUnlocked(string gameKey);
    event Action<string> Unlocked;
}

public interface IRewardSink
{
    // KillRewardDispenser만 구현. 개체 Id로 중복 지급을 막는다.
    void GrantKill(int combatantId, int mana, int experience);
    void GrantWave(int mana);
}
```

UI는 IManaReader·IExperienceReader·IUnlockReader만 받는다. HotbarView가 마나를 차감하는 코드를 쓸 수 없다.

---

## 7. 게임 흐름

```csharp
public enum GamePhase { Preparation, Combat, Victory, Defeat }

public interface IGameStateReader
{
    GamePhase Phase { get; }
    bool IsPaused { get; }
    event Action<PhaseChanged> PhaseChanged;   // struct(previous, current)
}

public interface IGameStateController : IGameStateReader
{
    void StartWave();
    void EndWave();
    void Win();
    void Lose();
    void SetPaused(bool paused);
    void Reset();
}

public interface IWaveReader
{
    int WaveIndex { get; }
    int WaveCount { get; }
    int PendingSpawns { get; }
    int AliveEnemies { get; }
    float PreparationRemaining { get; }        // 무제한이면 음수
    string NextRouteId { get; }
    event Action<WaveEvent> WaveStarted;
    event Action<WaveEvent> WaveCompleted;
}

public interface IEnemyFactory
{
    ICombatant Spawn(string gameKey, Vector3 position, Quaternion rotation, ICombatant core);
    void DespawnAll();                         // 재시작
}

public interface IResettable
{
    void ResetState();                         // RestartService가 순서대로 호출
}
```

RestartService는 IResettable 목록을 갖고 정해진 순서(적·투사체·하수인·시설·경제·성장·상태)로 부른다. 씬 재로드 없이 재시작할 수 있고, 13.1의 "이전 상태가 남지 않는가"를 EditMode에서 검증할 수 있다.

---

## 8. 이벤트 인자 구조체

전부 readonly struct. 클래스 인자를 쓰지 않는다.

| 구조체 | 필드 |
|---|---|
| HealthChanged | Combatant, Previous, Current, Max |
| Damaged | Combatant, DamageInfo, AppliedAmount |
| Died | Combatant, LastDamage |
| ManaChanged | Previous, Current, Max |
| LevelChanged | Previous, Current |
| PhaseChanged | Previous, Current |
| WaveEvent | WaveIndex, RouteIds |
| FacilityEvent | Facility, Position |

이벤트 구독은 Awake/OnEnable에서 한 번, 해제는 OnDisable/OnReturn에서 한다. Think나 Tick 안에서 구독·해제하지 않는다.

---

## 9. 어셈블리 배치

```
DesertTower.Combat.Runtime
  ├─ IDamageable, IRepairable, IRevivable, DamageInfo, DamageResult
  ├─ ICombatant, FactionId, FactionProfile
  └─ Health, HealthDefinition

DesertTower.Gameplay.Abstractions     [새 어셈블리, Unity 기본 타입만]
  ├─ ICombatantRegistry, IVisibilityService, IReachabilityService
  ├─ ITickable, IFixedTickable, ITickScheduler, IThinker, IThinkScheduler, ITimeSource
  ├─ IMovementController, IAttacker, IProjectile, IPoolable, IPool<T>
  ├─ ITargetPolicy, TargetContext, IChaseLimiter, IObstructionQuery
  ├─ IFacility, IFacilityRegistry, IOccupancyGrid, IPlacementRule, IPlacementValidator, INavObstacle
  ├─ IManaReader/IManaWallet, IExperienceReader/IExperienceTracker, IUnlockReader, IRewardSink
  ├─ IGameStateReader/IGameStateController, IWaveReader, IEnemyFactory, IResettable
  └─ 이벤트 구조체

DesertTower.Gameplay.Runtime          [구현. AI Navigation, Input System, Levels.Runtime 참조]
DesertTower.UI.Runtime                [Abstractions의 *Reader 인터페이스만 참조]
DesertTower.Gameplay.Tests            [Abstractions + 가짜 구현. Runtime 참조 없이 판단 로직 검증]
```

Abstractions를 분리하면 UI와 테스트가 AI Navigation·Input System을 끌어오지 않는다. 컴파일 시간도 준다.

---

## 10. 예산 초기값 요약

| 항목 | 초기값 | 근거 |
|---|---|---|
| 공간 해시 셀 | 5m | 최대 조회 반경(타워 사거리·인식 거리 10m)의 절반 |
| 시야 레이캐스트 | 1단계 예산 미적용 | 동기 조회·캐시·Think 분산. 64회/프레임은 프로파일 후 검토할 후보값 |
| 경로 계산 | 1단계 예산 미적용 | 캐시 미적중은 동기 계산. 8건/프레임은 측정 후 검토할 후보값이며 절대 비용 상한이 아님 |
| Think 주기 | 적 0.15s, 타워 0.1s, 하수인 0.2s | 인간이 지연을 느끼지 않는 상한 |
| 투사체 풀 | 64 | 타워 10 × 초당 2발 × 수명 3초 |
| 적 풀 | 웨이브 최대 동시 수 × 1.2 | WaveSet에서 계산 |

전부 StageDefinition 또는 별도 PerformanceSettings 자산에 둔다. 코드 상수로 박지 않는다.

---

## 11. 인터페이스가 해결하는 검증 항목 (기획서 13.1)

| 검증 항목 | 관련 인터페이스 | 테스트 방식 |
|---|---|---|
| 막힌 길 앞에서 시설 공격 | IMovementController.PathState·PathEndPosition, IObstructionQuery, ITargetPolicy | 부분 경로에서 적대 차단물과 접근 가능한 공격 위치 선택, 파괴 후 진격 재개 |
| 파괴 직후 이동 재개 | INavObstacle.Disable → IReachabilityService.Invalidate | Invalidate 뒤 현재 revision으로 Query하면 새 경로가 반영되는지 |
| 무한 추적 금지 | IChaseLimiter | 시간·거리 값만으로 EditMode |
| 벽 너머 공격 금지 | IAttacker.CanAttack, IVisibilityService | 가짜 시야 서비스로 false 주입 |
| 하수인 범위 이탈 금지 | MinionLeash(IMovementController 기반) | 가짜 mover 위치로 검증 |
| 설치 실패 시 마나 유지 | IFacilityBuilder → IManaWallet.TryReserve | 준비 단계별 실패를 주입하고 자원·생성·점유·등록·이벤트 흔적이 없는지 |
| 보상 중복 방지 | IRewardSink.GrantKill(id) | 같은 id 2회 호출 시 1회만 지급 |
| 재시작 시 잔여 상태 없음 | IResettable | 모든 구현을 호출 후 레지스트리 Count 0 |

---

## 12. 남은 결정

- 시야/경로 예산은 프로파일에서 병목이 확인된 뒤 도입한다. 첫 요청 동기 계산 및 명중 정확성을 유지한다.
- 웨이브 적 부활 도입 시 미해결 적 ID 집합 재진입 규칙을 먼저 정한다. 현재 1단계는 적 부활 없음.

- ICombatant.Id 부여 방식: 레지스트리 증가 카운터로 시작한다. 풀 재사용 시 새 Id를 받아 이전 참조가 무효가 되도록 한다.
- IVisibilityService 캐시 격자 크기 0.25m가 벽 두께(1m 격자)에 비해 충분히 작은지 실제 맵에서 확인한다.
- ITargetPolicy를 ScriptableObject로 둘지 순수 클래스 + 설정 자산으로 둘지. 초기값은 순수 클래스이며 EnemyDefinition이 정책 종류를 열거형으로 고른다.
- Tick 러너를 하나로 둘지 카테고리별(이동·투사체·타이머)로 나눌지. 초기값은 하나이며 프로파일 후 분리한다.
- IPool의 사전 확보(warm-up) 시점: 웨이브 시작 전 준비 단계에서 WaveSet 기준으로 채운다.

문서 끝.
