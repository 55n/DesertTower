2026-09-07 후속 결정: 단계별 슬롯/자유 배치, 포인트 선택형 성장, 코어 흡수/공격 전환은 [최신 결정 기록](E:/AI/Projects/DesertTower/Docs/Desert_Tower_결정기록_단계별배치_선택성장_코어전환_v0.1.md)을 따른다. 아래의 해당 항목은 이전 설계이며, 그 밖의 기존 계약은 유지한다.

# DESERT TOWER 개별 컴포넌트 목록

버전: 0.1 (2026-09-06 예약·동기 조회·완전 차단 승인안 반영)
최신 계약: [인터페이스 개선 반영 v0.4](Desert_Tower_인터페이스_개선_반영_v0.4.md). 아래 실제 서비스는 구현 예정 책임이다.
작성일: 2026-09-06
기준 문서: Desert_Tower_상세_게임기획서_v0.2 (11장), Desert_Tower_IDamageable_설계_v0.1, DesertTowerLevelEditor ARCHITECTURE.md 0.2.0
문서 성격: 구현 전 분해 기준. 코드 없음. 각 컴포넌트는 책임 하나를 갖고, 다른 컴포넌트는 인터페이스·이벤트·데이터 자산으로만 안다.

표기:
- [있음] 이미 프로젝트에 존재한다.
- [설계됨] 설계 문서는 있고 코드는 없다.
- [필요] 설계·코드 모두 없다.
- 단계 열은 기획서 12장의 제작 단계다. 1단계 항목만으로 "벽을 세우면 적이 부수고 코어로 간다"가 성립해야 한다.

---

## 0. 어셈블리 구성

```
DesertTower.Levels.Runtime        [있음]   공간·배치 데이터. Unity 기본 타입만.
DesertTower.Combat.Runtime        [설계됨] 체력·피해·진영. Unity 기본 타입만.
DesertTower.Gameplay.Runtime      [필요]   플레이어·적·하수인·시설·설치·경제·성장·웨이브·게임 상태.
                                           참조: Levels.Runtime, Combat.Runtime, AI Navigation, Input System
DesertTower.UI.Runtime            [필요]   HUD·메뉴. Gameplay.Runtime의 읽기 전용 인터페이스와 이벤트만 참조.
DesertTower.Gameplay.Tests        [필요]   EditMode: 판정·계산·상태 전이. PlayMode: 통합 시나리오(13.1).
```

Combat과 Levels는 서로를 모른다. Gameplay가 둘을 잇는다. UI는 Gameplay를 읽기만 한다.

---

## 1. 공통 전투 기반 (Combat.Runtime)

| 컴포넌트 | 종류 | 책임 | 상태 | 단계 |
|---|---|---|---|---|
| IDamageable | 인터페이스 | 체력·생존·진영·피격 지점 읽기, TakeDamage, HealthChanged/Damaged/Died | 설계됨 | 1 |
| DamageInfo / DamageResult | 구조체 | 피해 요청과 처리 결과 | 설계됨 | 1 |
| IRepairable | 인터페이스 | Repair(양). 시설용 | 설계됨 | 2 |
| IRevivable | 인터페이스 | Revive(). 플레이어용 | 설계됨 | 3 |
| Health | MonoBehaviour | IDamageable 기본 구현. 사망 시 충돌·공격 기능 끄기, Destroy/Remain 처리 | 설계됨 | 1 |
| HealthDefinition | ScriptableObject | 최대 체력, 진영, 피격 오프셋, 사망 처리, 지연 | 설계됨 | 1 |
| FactionProfile | ScriptableObject | 진영 목록과 적대 표. IsHostile(a, b) | 설계됨 | 1 |

---

## 2. 공용 판정·질의 (Gameplay.Runtime / Shared)

전투 기반이 "알지 않는 것"으로 명시한 항목을 담당한다. 적·타워·하수인·마법이 모두 같은 것을 쓴다.

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| CombatantRegistry | 서비스(씬 단일) | 전장에 있는 IDamageable 등록·해제, 진영별 조회. Died·소멸 시 자동 제거 | 1 |
| LineOfSight | 정적 유틸 | 공격 발생 지점 → 피격 지점 레이캐스트. 장애물 레이어 마스크 설정 | 1 |
| RangeQuery | 정적 유틸 | 반경 안 적대 개체 목록, 가장 가까운 개체 | 1 |
| Reachability | 정적 유틸 | NavMesh.CalculatePath로 접근 가능 여부(PathComplete/Partial) | 1 |
| AttackSlotAllocator | 컴포넌트(대상 개체에 부착) | 한 대상을 여러 적이 공격할 때 접근 위치를 분산 배정·반납 | 2 |
| DamageSource | 정적 유틸 | DamageInfo 구성 도우미. SourceFaction 누락 방지 | 1 |

---

## 3. 투사체·피해 전달

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| Projectile | MonoBehaviour | 직선 이동, 첫 충돌 대상에 TakeDamage 1회, 벽·지형 충돌 시 소멸, 수명 | 1 |
| ProjectileLauncher | MonoBehaviour | 발사 위치·방향으로 Projectile 생성, 발사 간격 관리. 플레이어·타워 공용 | 1 |
| AreaDamage | 정적 유틸 또는 컴포넌트 | 반경 안 적대 개체마다 TakeDamage 1회. 시야 차단 여부 옵션 | 2 |
| MeleeHit | MonoBehaviour | 준비 동작 후 사거리·시야 재확인하고 단일 대상 TakeDamage. 적·하수인 공용 | 1 |
| ProjectilePool | 서비스 | 투사체 재사용. 성능 문제 확인 후 도입 | 4 |

---

## 4. 플레이어

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| PlayerInputReader | MonoBehaviour | InputSystem_Actions를 읽어 의도(이동, 시점, 공격, 마법, 슬롯, 회전, 취소)로 변환. 다른 컴포넌트는 Input System을 모른다 | 1 |
| PlayerMotor | MonoBehaviour | CharacterController 이동, 중력, 시설·벽 통과 금지 | 1 |
| PlayerCameraRig | MonoBehaviour | 후방 3인칭, 캐릭터 왼쪽 오프셋, 장애물 시 거리 축소, 지형 침투 방지 | 1 |
| PlayerAimer | MonoBehaviour | 중앙 조준점 레이로 조준 지점 산출. 발사 위치→조준 지점 재검사로 카메라 위치 명중 방지 | 1 |
| PlayerBasicAttack | MonoBehaviour | 지팡이 ProjectileLauncher 구동. 마나 소비 없음 | 1 |
| SpellCaster | MonoBehaviour | 선택 SpellDefinition 시전: 마나 예약 후 비용·재사용 대기·효과 함께 확정, 전체 상태 갱신 후 이벤트 | 2 |
| PlayerRespawner | MonoBehaviour | Died 수신 → 5초 대기 → Respawn 마커 위치로 이동 → IRevivable.Revive | 3 |
| PlayerModeController | MonoBehaviour | 전투 모드 / 설치 모드 전환과 입력 라우팅 | 1 |
| PlayerAnimationDriver | MonoBehaviour | 이동·공격·시전·설치·피격·사망 파라미터 전달. 조준 유지 이동 | 3 |
| HotbarSelection | MonoBehaviour | 숫자 키 → 슬롯(시설/마법) 선택. 해금 여부는 UnlockRegistry에 질의 | 2 |

---

## 5. 적

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| EnemyDefinition | ScriptableObject | 체력 정의 참조, 이동 속도, 공격력, 공격 거리·준비·간격, 인식 거리 10m, 추적 한계 15m·3초, 마나·경험치 보상 | 1 |
| EnemyCatalog | ScriptableObject | gameKey → 적 프리팹·EnemyDefinition. LevelElementDefinition.gameKey를 해석 | 1 |
| EnemyFactory | 서비스 | 카탈로그로 프리팹 생성, 정의·목표·진영 주입, CombatantRegistry 등록 | 1 |
| EnemyMover | MonoBehaviour | IMovementController 구현. 동기 경로 계산·적용, 실패 시 이전 상태 유지, HasDestination/PathEndPosition. Partial 끝점 도착은 미도착. Stop은 경로 제거 | 1 |
| EnemyPerception | MonoBehaviour | 인식 거리 + LineOfSight로 플레이어·하수인 후보 수집. 시야 소실 시간 기록 | 2 |
| EnemyTargetSelector | MonoBehaviour | 기획서 4.3 우선순위: 현재 목표 유지 → 차단 시설 → 인식 대상 → 길옆 타워 → 코어. 재추적 금지 목록 | 1 (4.3의 1·2·5) / 2 (3·4) |
| BlockerDetector | MonoBehaviour | IObstructionQuery 구현. Partial 경로를 막는 적대 파괴 시설과 같은 접근 가능 영역의 공격 위치 탐색. 근접만으로 선택하지 않음 | 1 |
| EnemyChaseLimiter | MonoBehaviour | 교전 시작 위치 15m, 목표 미시야 3초, 접근 불가 시 추적 종료·복귀 신호 | 2 |
| EnemyBrain | MonoBehaviour | 상태 기계(진격 / 접근 / 공격 / 추적 / 복귀 / 사망). 위 컴포넌트를 조립 | 1 |
| EnemyAttack | MonoBehaviour | MeleeHit 구동. 사거리·시야 재확인, 대상 사망 시 재선택 요청 | 1 |
| EnemyDeathPresenter | MonoBehaviour | Died 수신 → 애니메이션·연출. 제거는 Health의 Destroy 처리에 맡김 | 3 |

강한 근접 적은 별도 컴포넌트 없이 EnemyDefinition 값과 프리팹 변형으로 만든다.

---

## 6. 시설 공통

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| FacilityDefinition | ScriptableObject | gameKey, 비용, 점유 크기(격자 단위), 체력 정의 참조, 수리 비율 50%, 환급 비율 50%, 프리팹, 미리보기 프리팹, 해금 레벨 | 1 |
| FacilityCatalog | ScriptableObject | gameKey → FacilityDefinition. 슬롯 순서 | 1 |
| Facility | MonoBehaviour | 시설 식별, 정의 참조, 점유 격자 목록, IRepairable 위임, 파괴 시 점유·장애물·기능 즉시 해제 | 1 |
| FacilityObstacle | MonoBehaviour | NavMeshObstacle carving 켜기·끄기. 파괴 직후 끄고 적 재경로 유도 | 1 |
| FacilityDestructionPresenter | MonoBehaviour | 짧은 잔해 연출 후 제거. 잔해는 충돌 없음 | 3 |
| FacilityDamageVisual | MonoBehaviour | HealthChanged 수신 → 재질 단계 변화 | 3 |
| FacilityRegistry | 서비스 | 살아 있는 시설 목록. 길옆 타워 탐색, 재시작 정리에 사용 | 1 |

### 6.1 벽

| 컴포넌트 | 책임 | 단계 |
|---|---|---|
| Wall | Facility + Health + FacilityObstacle 조합. 추가 로직 없음. 프리팹 구성으로 충분 | 1 |

### 6.2 공격 타워

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| AttackTowerDefinition | ScriptableObject | FacilityDefinition 확장: 사거리, 공격 간격, 피해량, 투사체 속도 | 1 |
| TowerTargeting | MonoBehaviour | 범위 안 가장 가까운 적 + LineOfSight. 범위 이탈·가려짐·사망 시 재탐색 | 1 |
| TowerAttack | MonoBehaviour | 대상이 유효할 때만 ProjectileLauncher 발사. 이미 나간 투사체는 관여하지 않음 | 1 |
| TowerTurret | MonoBehaviour | 머리 회전 연출. 선택 | 3 |

### 6.3 소환 타워

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| SummonTowerDefinition | ScriptableObject | FacilityDefinition 확장: 활동 반경, 하수인 수 2, 재소환 8초, 하수인 프리팹·정의 | 2 |
| MinionRoster | MonoBehaviour | 소속 하수인 슬롯 관리, Died 수신 → 8초 뒤 재소환, 유효 공간 없으면 대기, 타워 파괴·철거 시 전원 소멸 | 2 |
| MinionSpawnPointFinder | 정적 유틸 | 반경 안 NavMesh 위 빈 위치 탐색 | 2 |

---

## 7. 하수인

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| MinionDefinition | ScriptableObject | 체력 정의, 이동 속도, 공격력, 공격 거리·간격 | 2 |
| MinionBrain | MonoBehaviour | 상태 기계(대기 / 교전 / 복귀). 활동 반경 안 적만 대상 | 2 |
| MinionLeash | MonoBehaviour | 소속 타워 중심·반경 참조. 이탈 시 복귀 명령 | 2 |
| MinionAttack | MonoBehaviour | MeleeHit 구동 | 2 |

이동은 EnemyMover를 그대로 재사용한다(이름은 NavMover로 일반화 가능).

---

## 8. 설치 시스템

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| BuildModeController | MonoBehaviour | 설치 모드 진입·종료, 선택 시설, 회전 상태. 좌클릭 확정·우클릭/Esc 취소 | 1 |
| PlacementPreview | MonoBehaviour | 반투명 모형, 점유 면적, 방향, 사거리 원, 비용, 가능·불가 색상과 사유 표시 | 1 |
| GridSnapper | 정적 유틸 | LevelRoot.gridSize로 위치·회전(90도) 보정 | 1 |
| PlacementValidator | 서비스 | 순서대로 검사: 설치 구역(LevelRoot.IsPointInBuildableZone, 점유 격자 전부) → 점유 충돌(OccupancyGrid) → 코어·출현 지점·외곽 금지 → 살아 있는 캐릭터 겹침 → 평탄도 → 마나. 실패 사유 반환 | 1 |
| OccupancyGrid | 서비스 | 격자 점유 등록·해제·조회. 시설 파괴 시 해제 | 1 |
| FacilityBuilder | 서비스 | 최신 검증 → 마나 예약 → 생성·점유·등록 준비 → 전체 확정 → 이벤트. 실패 시 준비 상태/예약 폐기, Gain 환불 금지 | 1 |
| FacilityInspector | MonoBehaviour | 조준한 시설 식별, 체력·수리 비용·환급액 산출을 UI에 제공 | 2 |
| RepairService | 서비스 | 준비 단계에서만. 부족 체력 비율 × 설치 비용 × 50% 차감 후 IRepairable.Repair | 2 |
| DemolishService | 서비스 | 준비 단계에서만. 설치 비용 × 현재 체력 비율 × 50% 버림 환급 후 직접 제거(Died 없음) | 2 |

---

## 9. 경제·성장

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| ManaWallet | 서비스 | 현재·최대 마나, TrySpend, TryReserve/IManaReservation, Gain(예약 포함 잔액 기준 초과 버림). 전체 작업 확정 후 Changed, 재시작 시 예약 무효화 | 1 |
| KillRewardDispenser | 서비스 | 적 Died 수신 → 정의의 마나·경험치 지급. 개체당 1회 보장(처리한 개체 집합). 처치 주체 무관 | 1 |
| ExperienceTracker | 서비스 | 경험치 누적, ProgressionTable로 레벨 산출, LevelUp 이벤트, 상한 5 | 3 |
| ProgressionTable | ScriptableObject | 레벨별 필요 경험치, 레벨별 해금 gameKey 목록 | 3 |
| UnlockRegistry | 서비스 | 현재 해금 집합. IsUnlocked(gameKey). LevelUp 수신으로 갱신 | 3 |

1단계에서는 ExperienceTracker 없이 UnlockRegistry가 전부 해금된 상태로 시작한다.

---

## 10. 웨이브·게임 흐름

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| StageDefinition | ScriptableObject | 시작 마나, 최대 마나, 코어 체력 정의, 기본 준비 시간, WaveSet 참조, FactionProfile 참조 | 1 |
| GameStateMachine | 서비스 | 상태: 준비 / 전투 / 승리 / 패배 / 일시정지. 전이 이벤트. 다른 시스템은 상태를 묻기만 한다 | 1 |
| WaveDirector | 서비스 | WaveSet 순회·출현 예정 수 및 미해결 적 ID 집합 관리. Died/Despawned에서 ID를 한 번만 제거. 두 수가 0일 때 완료/보상 한 번, 과거 세션 이벤트 무시 | 1 |
| PreparationTimer | 서비스 | 첫 웨이브 무제한, 이후 20초, 조기 시작 | 3 |
| CoreController | MonoBehaviour | Core 마커 위치의 시설. Health의 Died 수신 → 패배 요청. 피격 방향 알림 이벤트 | 1 |
| PauseService | 서비스 | timeScale 0, 입력 차단, 준비 타이머 정지 | 3 |
| RestartService | 서비스 | 적·시설·투사체·하수인·경제·성장·상태 전부 초기화. 씬 재로드 방식이면 각 서비스 초기화만 검증 | 1 |
| RemainingEnemyLocator | 서비스 | 남은 적 위치를 HUD에 제공. 이동 오류 임시 대응용 | 3 |
| NextRouteAnnouncer | 서비스 | 다음 웨이브의 진입로 ID를 준비 단계에 알림 | 3 |

---

## 11. HUD·UI (UI.Runtime)

각 뷰는 하나의 이벤트 소스만 구독한다.

| 컴포넌트 | 구독 대상 | 단계 |
|---|---|---|
| PlayerHealthView | 플레이어 Health.HealthChanged | 1 |
| CoreHealthView | CoreController Health.HealthChanged | 1 |
| ManaView | ManaWallet.Changed | 1 |
| LevelExpView | ExperienceTracker | 3 |
| WaveInfoView | WaveDirector(웨이브 번호, 남은 적), PreparationTimer | 1 (번호·남은 적) / 3 (타이머) |
| CrosshairView | PlayerModeController(모드별 모양) | 1 |
| HotbarView | HotbarSelection, UnlockRegistry, SpellCaster(쿨다운), ManaWallet(비용 가능 여부) | 2 |
| PlacementInfoView | PlacementPreview(사유, 회전 안내) | 1 |
| FacilityInspectView | FacilityInspector(체력, 수리·철거 비용) | 2 |
| DirectionalAlertView | CoreController 피격, FacilityRegistry 파괴 이벤트. 화면 가장자리 방향 표시 | 3 |
| NotificationToast | UnlockRegistry, ExperienceTracker LevelUp | 3 |
| ResultScreen | GameStateMachine 승리·패배. 재시작 버튼 → RestartService | 1 |
| PauseMenu | PauseService | 3 |
| PreparationPanel | GameStateMachine 준비 상태. 웨이브 시작 버튼, 다음 진입로 | 3 |

---

## 12. 연출·음향 훅

전부 이벤트 구독형이다. 게임 로직 컴포넌트는 연출을 호출하지 않는다.

| 컴포넌트 | 책임 | 단계 |
|---|---|---|
| HitEffectSpawner | Damaged 수신 → HitPosition에 피격 효과 | 3 |
| DeathEffectSpawner | Died 수신 → 사망·파괴 효과 | 3 |
| AudioCuePlayer | 이벤트 → 효과음. 동시 재생 수 제한 | 3 |
| MusicController | GameStateMachine 준비·전투 전환 | 3 |
| RangeIndicator | 타워·소환 타워 활동 범위 바닥 투영. 미리보기와 조준 시 표시 | 1 |

---

## 13. 편집 가능한 데이터 자산 요약 (기획서 11.2 대응)

| 기획서 항목 | 자산 | 상태 |
|---|---|---|
| 적 | EnemyDefinition, HealthDefinition | 필요 |
| 시설 | FacilityDefinition, HealthDefinition | 필요 |
| 공격 타워 | AttackTowerDefinition | 필요 |
| 소환 타워 | SummonTowerDefinition, MinionDefinition | 필요 |
| 마법 | SpellDefinition(비용, 피해, 반경, 재사용, 해금 레벨) | 필요 |
| 성장 | ProgressionTable | 필요 |
| 웨이브 | WaveSet / Wave / SpawnGroup | 있음 |
| 스테이지 | StageDefinition | 필요 |
| 진영 | FactionProfile | 설계됨 |
| 카탈로그 | EnemyCatalog, FacilityCatalog (gameKey 해석) | 필요 |
| 레벨 공간 | LevelRoot, LevelMarker, LevelArea, LevelRoute, LevelElementDefinition | 있음 |

---

## 14. 1단계 최소 집합

기획서 12장 1단계("벽을 설치하면 적이 벽을 파괴하고 코어로 전진, 공격 타워 지원, 최소 HUD와 승패")에 필요한 것만 추린다.

```
Combat:    IDamageable, DamageInfo/Result, Health, HealthDefinition, FactionProfile
Shared:    CombatantRegistry, LineOfSight, RangeQuery, Reachability, DamageSource
Damage:    Projectile, ProjectileLauncher, MeleeHit
Player:    PlayerInputReader, PlayerMotor, PlayerCameraRig, PlayerAimer, PlayerBasicAttack, PlayerModeController
Enemy:     EnemyDefinition, EnemyCatalog, EnemyFactory, EnemyMover, EnemyTargetSelector(유지·차단·코어),
           BlockerDetector, EnemyBrain, EnemyAttack
Facility:  FacilityDefinition, FacilityCatalog, Facility, FacilityObstacle, FacilityRegistry,
           Wall(프리팹), AttackTowerDefinition, TowerTargeting, TowerAttack
Build:     BuildModeController, PlacementPreview, GridSnapper, PlacementValidator, OccupancyGrid, FacilityBuilder
Economy:   ManaWallet, KillRewardDispenser
Flow:      StageDefinition, GameStateMachine, WaveDirector, CoreController, RestartService
UI:        PlayerHealthView, CoreHealthView, ManaView, WaveInfoView, CrosshairView, PlacementInfoView, ResultScreen
Visual:    RangeIndicator
```

총 54개. 이 중 Health·FactionProfile·BlockerDetector·FacilityObstacle·PlacementValidator·WaveDirector가 13.1 검증 항목의 대부분을 결정한다.

---

## 15. 컴포넌트 간 의존 규칙

- 피해를 주는 쪽(Projectile, MeleeHit, AreaDamage)은 IDamageable만 안다.
- AI(EnemyBrain, MinionBrain, TowerTargeting)는 CombatantRegistry·LineOfSight·RangeQuery·Reachability로만 전장을 본다. 구체 타입 조회 금지.
- 마나·경험치는 KillRewardDispenser 한 곳에서만 지급한다. 적·타워·플레이어는 보상을 모른다.
- 설치는 FacilityBuilder 한 곳에서만 생성한다. 마나 차감은 생성 성공 뒤에만 일어난다.
- 레벨 데이터(LevelRoot 등)는 WaveDirector, PlacementValidator, PlayerRespawner, CoreController만 읽는다.
- UI는 이벤트 구독과 읽기 속성만 쓴다. UI에서 게임 상태를 바꾸는 경로는 버튼 → 서비스 호출뿐이다.
- 시설·하수인·투사체를 피해 없이 없애는 경로(철거, 소멸, 재시작)는 Died를 내지 않는다.

---

## 16. 남은 결정

- 적이 "막힌 길"을 아는 방식: NavMeshObstacle carving 후 경로 Partial 판정으로 시작한다. 벽이 길을 완전히 막을 때 NavMesh가 끊기므로 Partial 종점 근처의 시설을 BlockerDetector가 찾는다. carving 갱신 지연이 문제가 되면 런타임 NavMesh 재구성으로 바꾼다.
- EnemyMover와 하수인 이동의 통합 이름(NavMover) 여부.
- 서비스의 수명 형태: 씬 단일 MonoBehaviour로 시작하고, 재시작을 씬 재로드로 할지 각 서비스 Reset으로 할지는 RestartService 검증 후 정한다.
- SpellDefinition을 FacilityDefinition과 같은 슬롯 카탈로그에 넣을지(HotbarView가 두 종류를 한 목록으로 다룰지).
- 진영 값: Player / Enemy / Facility를 별도로 둘지, 플레이어·시설·하수인을 한 진영(Ally)으로 둘지. 초기값은 Ally / Enemy 두 진영.

문서 끝.
