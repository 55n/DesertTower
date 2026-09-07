2026-09-07 후속 결정: 단계별 슬롯/자유 배치, 포인트 선택형 성장, 코어 흡수/공격 전환은 [최신 결정 기록](E:/AI/Projects/DesertTower/Docs/Desert_Tower_결정기록_단계별배치_선택성장_코어전환_v0.1.md)을 따른다. 아래의 해당 항목은 이전 설계이며, 그 밖의 기존 계약은 유지한다.

# DESERT TOWER 방어 장치 설계

버전: 0.1 (2026-09-06 이동·차단·집계 계약 반영)
관련 최신 계약: [인터페이스 개선 반영 v0.4](Desert_Tower_인터페이스_개선_반영_v0.4.md). 감시자는 아직 구현 전이다.
작성일: 2026-09-06
기준 문서: Desert_Tower_상세_게임기획서_v0.2 (13.1, 13.3), Desert_Tower_컴포넌트_목록_v0.1, Desert_Tower_인터페이스_제안_v0.1
문서 성격: 구현 전 설계 기준. 코드 없음. 물리·내비게이션·피해 처리에서 생기는 사고를 미리 막는 장치와, 그 장치를 맡는 컴포넌트를 정한다.

---

## 1. 원칙

사고의 대부분은 두 시스템이 같은 개체를 동시에 움직이거나, 피해가 명시적 호출이 아닌 물리 이벤트에서 나올 때 생긴다. 다음 원칙이 장치의 절반을 대신한다.

| 원칙 | 내용 |
|---|---|
| 이동 주체는 하나 | 적·하수인은 NavMeshAgent만, 플레이어는 CharacterController만 움직인다. 동적 Rigidbody는 어느 개체에도 없다 |
| 충돌체는 명중용 | 적·하수인·시설·플레이어의 충돌체는 투사체 명중과 조준 레이캐스트에만 쓴다. 개체 간 물리 충돌은 Layer Collision Matrix에서 끈다 |
| 피해는 호출로만 | TakeDamage 호출 외에 체력을 바꾸는 경로가 없다. 충돌 충격량, 트리거 체류 시간, 속도는 피해와 무관하다 |
| 위치는 표면에 묶임 | 적·하수인은 NavMesh 표면을 벗어날 수 없다. 플레이어는 벗어나면 되돌린다 |
| 감시자는 로그를 남김 | 되돌리는 장치가 작동하면 반드시 원인 추적용 로그를 남긴다. 조용히 고치지 않는다 |

---

## 2. 레이어와 충돌 행렬

| 레이어 | 대상 | 충돌 허용 |
|---|---|---|
| Ground | 지형, 성벽, 절벽, 계단 | Player, Projectile, CameraProbe |
| Facility | 벽, 타워, 코어 | Player, Projectile, CameraProbe, AimProbe |
| Enemy | 적 몸통 | Projectile, AimProbe |
| Ally | 하수인 몸통 | Projectile(적 투사체 도입 시), AimProbe |
| Player | 플레이어 캡슐 | Ground, Facility, PlayerBlocker |
| Projectile | 투사체 | Ground, Facility, Enemy, Ally |
| PlayerBlocker | 절벽 가장자리 보이지 않는 벽 | Player |
| Volume | 회수 볼륨, 출현 구역 | Player(트리거) |
| LosBlocker | 시야 차단 판정용. Ground·Facility와 겹쳐 지정 | 레이캐스트 전용 |

Enemy와 Enemy, Enemy와 Facility, Enemy와 Player는 충돌하지 않는다. 적끼리 겹침 방지는 NavMeshAgent avoidance가 맡는다.

---

## 3. 사고별 장치

### 3.1 적이 서로 밀려 벽 안으로 들어감

| 원인 | 장치 | 담당 컴포넌트 |
|---|---|---|
| 동적 Rigidbody와 Agent의 위치 다툼 | 적·하수인에 Rigidbody를 두지 않는다. 필요하면 kinematic만 | 프리팹 규칙 |
| 적끼리 물리 충돌 | Enemy-Enemy 충돌 해제. avoidance는 Agent 설정(quality, priority) | 프리팹 규칙, EnemyMover |
| 벽 설치 순간 그 자리에 적이 있음 | PlacementValidator의 CharacterInside 규칙이 설치를 거부 | PlacementValidator |
| carving 지연 중 적이 벽 안에 남음 | NavMeshObstacle은 carveOnlyStationary 끔, moveThreshold 0.1. 그래도 남는 경우 3.5의 위치 감시자가 되돌림 | FacilityObstacle, NavSurfaceGuard |
| Agent 반경보다 좁은 틈에 끼임 | 격자 크기가 Agent 지름보다 크도록 LevelRoot.gridSize를 정한다. 초기값: 격자 1m, Agent 반경 0.35m | 레벨 데이터 규칙 |

### 3.2 시설이 충돌 판정으로 큰 피해를 한 번에 받음

| 원인 | 장치 | 담당 컴포넌트 |
|---|---|---|
| 충돌 충격량으로 피해 계산 | 그런 경로를 만들지 않는다. 피해량은 DamageInfo.Amount로만 온다 | 설계 원칙 |
| 한 개체의 충돌체 여러 개가 각각 명중 처리 | 명중 시 충돌체가 아니라 루트의 IDamageable 하나로 해석. 투사체는 첫 명중에서 소비됨 플래그를 세우고 즉시 비활성화 | Projectile |
| 투사체가 여러 프레임 동안 같은 대상에 체류 | SphereCast 이동이라 체류가 없다. OnTriggerStay를 쓰지 않는다 | Projectile |
| 범위 피해에서 같은 개체가 두 번 나옴 | 대상 목록을 CombatantRegistry에서 받는다. OverlapSphere를 쓰지 않는다 | AreaDamage |
| 근접 공격 한 번에 여러 번 적용 | 준비 동작 끝에 한 번만 TakeDamage. 애니메이션 이벤트가 중복 발생해도 스윙당 1회 플래그 | MeleeHit |
| 피해량 자체가 비정상 | Amount가 음수·NaN·무한이면 거부. 개발 빌드에서 최대 체력 × 10 초과 단일 피해는 Assert | Health |
| 시설 파괴 연출 중 추가 피해 | Died 시점에 충돌체를 끄므로 명중 자체가 없다. 들어와도 IsAlive 거짓으로 거부 | Health |

### 3.3 낙사

| 대상 | 원인 | 장치 | 담당 컴포넌트 |
|---|---|---|---|
| 적·하수인 | 물리 낙하 | Agent는 중력이 없고 NavMesh 밖으로 나가지 않는다. 절벽 가장자리는 베이크에서 제외. 장치 불필요 | 없음 |
| 플레이어 | 절벽에서 걸어 떨어짐 | 절벽 가장자리에 PlayerBlocker 레이어의 보이지 않는 충돌체. 레벨 에디터의 NoBuild 구역과 별개로 배치 | 레벨 데이터, LevelGizmos(편집 표시) |
| 플레이어 | 블로커 틈으로 떨어짐 | 맵 최저점 아래 회수 볼륨. 진입 시 마지막 안전 지면으로 복귀. 피해 없음(기획서 7.3 낙사 제외) | RecoveryVolume, PlayerMotor |
| 플레이어 | 안전 지면 기록이 없음 | 기록이 없으면 Respawn 마커로 복귀 | PlayerMotor, PlayerRespawner |
| 플레이어 | 적에게 밀림 | Agent는 CharacterController를 밀지 않는다. Enemy-Player 충돌 해제. 장치 불필요 | 없음 |

마지막 안전 지면: PlayerMotor가 착지 상태이고 경사가 걷기 가능 범위 안일 때 0.5초마다 위치를 기록한다. 링 버퍼 3개를 두고 가장 오래된 것으로 복귀해 "떨어지기 직전 가장자리"로 돌아가는 것을 피한다.

### 3.4 적이 멈춰서 웨이브가 끝나지 않음

기획서 8장은 "적이 남아 있는 한 준비 단계로 넘어가지 않는다"고 하므로 이 사고는 게임을 진행 불능으로 만든다.

| 원인 | 장치 | 담당 컴포넌트 |
|---|---|---|
| 경로 Partial인데 차단 시설을 못 찾음 | IObstructionQuery 탐색 반경을 한 번 확장. 없으면 원래 목적지를 보존한 재경로 후 진행 감시자로 넘김 | EnemyBrain, BlockerDetector |
| 목적지 도착 판정이 안 됨(stoppingDistance 오차) | Complete 경로에서만 HasArrived를 평가. Partial 끝점 도착은 false | EnemyMover |
| 이유 불명 정지 | 정상 공격·유효한 공격 위치 대기·일시정지를 제외한 진행 실패만 감시. 3초 재경로, 6초 같은 접근 가능 영역의 검증된 안전 위치 복구, 최종 실패 소멸. 차단 시설 통과 금지, 로그 | StuckWatchdog |
| 출현 위치가 NavMesh 밖 | 스폰 반경 안에서 SamplePosition. 실패 시 출현을 다음 프레임으로 미루고 3회 실패 시 로그 | EnemyFactory |
| 그래도 남음 | 남은 적 위치를 HUD에 표시(기획서 8장) | RemainingEnemyLocator |

### 3.5 위치가 표면을 벗어남

| 대상 | 장치 | 담당 컴포넌트 |
|---|---|---|
| 적·하수인 | 1초마다 SamplePosition(반경 0.5m). 실패하면 반경 2m로 재시도해 Warp. 그것도 실패하면 소멸 후 로그. 소멸은 Died 없이 처리하며 WaveDirector는 Died/Despawned 중 최초 통지에서만 미해결 적 ID를 제거한다 | NavSurfaceGuard |
| 플레이어 | 3.3의 회수 볼륨 | RecoveryVolume |
| 투사체 | 수명 초과 또는 맵 경계 볼륨 이탈 시 풀 반납 | Projectile |

---

## 4. 피해 경로 무결성

TakeDamage 호출은 아래의 검증된 피해 경로로 제한한다. 코어 도달 처리는 2026-09-07 결정에 따라 흡수 모드에서만 추가 허용한다.

| 호출자 | 호출 시점 | 1회 보장 방식 |
|---|---|---|
| Projectile | SphereCast 첫 명중 | 소비됨 플래그, 즉시 비활성화 |
| MeleeHit | 준비 동작 종료, 재검증 통과 | 스윙당 플래그 |
| AreaDamage | 시전 시점 | 레지스트리 결과는 개체당 1개 |
| 코어 도달 처리 서비스(구현 예정) | 흡수 모드에서 실제 도달과 개체 유효성을 검증한 뒤 | 적 ID당 한 번 피해·제거·웨이브 해소. 공격 모드는 도달 피해 없이 기존 MeleeHit 사용. 원시 트리거의 반복 피해 금지 |

Health 쪽 방어:
- Amount < 0, NaN, Infinity → 거부. DamageResult에 RejectReason 추가값 "잘못된 요청"
- 개발 빌드: Amount > MaxHealth × 10 → Assert 후 적용. 밸런스 값이 아니라 버그로 본다
- 같은 프레임에 같은 Source에서 두 번 이상 → 허용하되 개발 빌드에서 경고 로그. 정당한 경우(범위 마법 + 투사체 동시 명중)가 있으므로 거부하지 않는다

---

## 5. 새로 필요한 컴포넌트

컴포넌트 목록 v0.1에 없던 것.

| 컴포넌트 | 종류 | 책임 | 단계 |
|---|---|---|---|
| NavSurfaceGuard | MonoBehaviour(적·하수인) | 주기적 SamplePosition, 이탈 시 Warp 또는 소멸, 로그 | 1 |
| StuckWatchdog | MonoBehaviour(적·하수인) | 진행 거리 감시, 재경로·Warp 단계 처리, 로그 | 1 |
| RecoveryVolume | MonoBehaviour(레벨) | 플레이어 진입 시 마지막 안전 지면 또는 Respawn 마커로 복귀 | 1 |
| SafeGroundRecorder | PlayerMotor 내부 또는 별도 | 착지 위치 링 버퍼 | 1 |
| BoundsVolume | MonoBehaviour(레벨) | 맵 경계. 투사체·소멸 판정 | 1 |
| DiagnosticLog | 서비스 | 방어 장치 작동 기록. 개체 Id, 위치, 원인. 개발 빌드에서 화면 표시 옵션 | 1 |

Think·Tick 부하: NavSurfaceGuard와 StuckWatchdog는 IThinkScheduler에 1초 주기로 등록한다. 적 100명 기준 프레임당 약 2건이다.

---

## 6. 프리팹·자산 규칙

체크리스트. 프리팹을 만들 때 확인한다.

적·하수인:
- Rigidbody 없음. 있다면 kinematic + useGravity 끔
- NavMeshAgent: radius 0.35, avoidance quality Medium, priority는 일반 50, 강한 적 30
- 충돌체 1개(캡슐), 레이어 Enemy 또는 Ally, isTrigger 아님
- NavSurfaceGuard, StuckWatchdog 부착

시설:
- Rigidbody 없음
- 충돌체 1개, 레이어 Facility
- NavMeshObstacle: carve 켬, carveOnlyStationary 끔
- 격자 점유 크기와 충돌체 크기가 일치

플레이어:
- CharacterController만. Rigidbody 없음
- 레이어 Player
- SafeGroundRecorder 활성

투사체:
- Rigidbody 없음. 충돌체 없음. SphereCast로 명중
- 레이어 Projectile은 레이캐스트 마스크에만 쓴다

레벨:
- 절벽 가장자리 PlayerBlocker 배치. LevelGizmos로 편집 시 표시
- RecoveryVolume 1개, BoundsVolume 1개
- NavMesh 베이크 설정의 agent radius가 프리팹의 Agent radius와 같음

---

## 7. 검증 시나리오

기획서 13.1에 추가할 PlayMode 테스트.

| 시나리오 | 기대 결과 |
|---|---|
| 좁은 통로에 적 20명을 밀어 넣고 통로 중간에 벽 설치 | 벽 안에 적 0명. CharacterInside 거부 또는 NavSurfaceGuard 작동 로그 |
| 적 10명이 같은 벽을 동시에 공격 | 벽 체력 감소량 = 공격 횟수 × 공격력. 초과 없음 |
| 투사체가 충돌체 2개짜리 적을 관통 방향으로 명중 | Damaged 1회 |
| 범위 마법 안에 적 5명 | Damaged 5회, 개체당 1회 |
| 플레이어를 절벽으로 강제 이동 | PlayerBlocker에 막힘. 블로커 제거 후 강제 이동 시 RecoveryVolume이 마지막 안전 지면으로 복귀, 체력 불변 |
| 적을 NavMesh 밖 좌표로 강제 Warp | 1초 안에 표면으로 복귀 또는 소멸, 로그 1건 |
| 적을 이동 불가 상태로 강제(속도 0) | 정상 공격/대기/일시정지가 아닐 때 3초 재경로. 6초 검증된 동일 영역 복구, 차단 시설 통과 없음. 최종 실패는 소멸 및 1회 집계 |
| 적 출현 지점을 NavMesh 밖으로 옮김 | 출현 지연 후 로그, 게임 진행 불능 없음 |
| 벽 파괴 직후 | 그 벽을 공격하던 적 전원이 1 Think 주기 안에 이동 재개 |
| 웨이브 중 재시작 | 감시자 로그 0건으로 새 웨이브 시작 |

---

## 8. 남은 결정

- NavSurfaceGuard의 소멸 판정 시 마나·경험치를 지급할지. 초기값은 지급하지 않음. 플레이어가 처치하지 않은 적이므로 보상은 없고, 웨이브 집계에서만 제외한다.
- 회수 볼륨 복귀 시 짧은 화면 전환을 넣을지. 초기값은 즉시 이동.
- Assert를 릴리스 빌드에서 어떻게 처리할지. 초기값은 로그만 남기고 적용.
- 강한 적의 avoidance priority를 낮춰 일반 적을 밀어내게 할지. 초기값은 30으로 두고 밀집 상황을 본 뒤 조정.
- StuckWatchdog의 Warp가 플레이어 눈앞에서 일어나면 순간이동으로 보인다. Warp 대신 소멸 후 출현 지점 재출현으로 바꿀지는 발생 빈도를 본 뒤 정한다.

문서 끝.
