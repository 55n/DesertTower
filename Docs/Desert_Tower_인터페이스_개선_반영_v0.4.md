2026-09-07 후속 결정: 단계별 슬롯/자유 배치, 포인트 선택형 성장, 코어 흡수/공격 전환은 [최신 결정 기록](E:/AI/Projects/DesertTower/Docs/Desert_Tower_결정기록_단계별배치_선택성장_코어전환_v0.1.md)을 따른다. 아래의 해당 항목은 이전 설계이며, 그 밖의 기존 계약은 유지한다.

# DESERT TOWER 인터페이스 개선 반영 v0.4

반영일: 2026-09-06. 사용자 승인에 따라 마나 원자성, 공개 Pending 제거, 이동 실패와 완전 차단 계약을 통합했다.
이 문서와 현재 코드가 해당 항목의 최신 기준이다. v0.1 제안 및 v0.2/v0.3 설명과 충돌하면 이 문서를 따른다.
입력 방식과 시설 배치 페이즈의 조작은 미확정이며 이번 변경에서 제외한다.

## 반영 범위와 구현 상태

- Assets/Interfaces: 34개. IManaReservation, IObstructionQuery 추가.
- Assets/Gameplay/Contracts: 판정 결과 정비, ObstructionRequest/ObstructionResult 추가.
- 기존 PriorityTargetPolicy의 현재 목표 유지 → 우선순위 → 거리 → ID 로직은 유지한다.
- ManaWallet, FacilityBuilder, 시야/경로 서비스, 이동 컨트롤러, BlockerDetector, WaveDirector, 감시자의 실제 구현은 아직 없다.
- 이번 반영은 계약·데이터·문서와 EditMode 계약 테스트다. 런타임 원자성, NavMesh 이동, 차단물 공격이나 웨이브 종료가 구현됐다는 뜻은 아니다.

## 마나 예약과 작업 확정

IManaWallet.TryReserve(amount, out reservation)를 추가했다. IManaReservation은 IDisposable과 TryCommit을 제공한다.

| 작업 | 잔액과 예약 | 이벤트 |
|---|---|---|
| 예약 성공 | CurrentMana 불변, 소비 가능액에서 예약액 제외 | 없음 |
| 예약 실패 | 모든 상태 불변, out은 null | 없음 |
| 확정 | 예약 해제와 실제 차감을 한 번 수행 | 전체 작업 확정 후 Changed |
| 확정 전 Dispose | 예약만 해제, Gain 호출 금지 | 없음 |
| 확정 후 Dispose 또는 반복 Dispose | 변화 없음 | 없음 |
| 재시작 | 기존 핸들 무효화 | 이전 핸들이 새 세션에 이벤트를 발행하지 않음 |

- 음수 예약/소비는 false, 0은 허용한다. Gain의 음수는 유효하지 않다.
- CurrentMana는 예약액을 포함한 아직 소비하지 않은 총액이다. TrySpend와 추가 예약은 총액에서 예약 합계를 뺀 범위만 사용한다.
- Gain은 예약액을 포함한 CurrentMana를 기준으로 상한을 적용한다. 예약이 보상 수령 공간을 만들지 않는다.
- 활성 예약의 TryCommit은 잔액 부족 때문에 실패하지 않는다. 이미 확정/취소/초기화된 핸들은 false이며 추가 효과가 없다.
- 예약은 한 동기 작업 안에서 사용한다. UI 견적을 예약으로 오래 유지하지 않는다. 호출자는 using/finally로 해제를 보장한다.

건설 순서: 최신 조건 검증 → 마나 예약 → 생성·점유·등록 준비 → 모든 상태 확정 → 이벤트 발행.
준비 상태는 조회 및 이벤트에 노출하지 않는다. 실패 가능한 검사를 확정 전에 끝내고, 확정 구간에서 뒤늦은 외부 작업을 시작하지 않는다.
실패 시 준비 개체/점유/등록을 폐기하고 예약을 해제한다. Gain을 복구 수단으로 쓰지 않는다.
지갑만 이벤트를 지연해서는 충분하지 않다. 건설 서비스가 관련 상태와 통지를 같은 작업 경계로 묶는다. 구현 조립 시 지갑과 관련 서비스에 동일한 내부 작업 경계/이벤트 발행기를 공급하며, 이번에는 범용 트랜잭션 공개 API를 추가하지 않는다.
이벤트 수신자가 요청한 후속 명령은 현재 작업을 중첩 실행하지 않고 직렬 처리한다.
확정 이후 이벤트 수신자의 예외를 건설 실패로 바꾸거나 이미 확정된 작업을 되돌리지 않는다.
수리·시전도 비용/효과/쿨다운을 함께 확정한다. 철거는 제거와 환급을 한 번만 반영한다.

## 동기 시야·접근성 조회

Request/TryGetResult/Release 및 요청 ID 수명 관리를 제거했다. 두 서비스는 Query(request)와 Invalidate()를 제공한다.
시야 서비스는 WorldRevision, 접근성 서비스는 NavigationRevision을 소유하고 읽기 속성으로 제공한다.

- 캐시가 없으면 같은 호출 안에서 계산한다. 1단계에는 계산 예산이나 요청 큐가 없다.
- VisibilityState: Invalid(기본값), Visible, Blocked.
- ReachabilityState: Invalid(기본값), Reachable, Unreachable, Partial.
- default 결과는 실패 판정도 성공 판정도 아닌 Invalid다. Invalid를 Blocked/Unreachable로 해석하지 않는다.
- IsStale은 판정과 독립된 메타데이터로 유지하며 1단계 서비스는 항상 false를 반환한다.
- Reachable.EndPosition은 목적지, Partial은 실제 도달 가능한 끝점이다. 끝점이 출발점과 같아도 Partial일 수 있다.
- Unreachable은 유효한 조건에서 쓸 수 있는 경로를 얻지 못한 판정이다. EndPosition은 유한한 출발점이다. Invalid의 끝점은 zero이며 이동에 쓰지 않는다.
- 요청의 revision이 서비스의 현재 revision과 다르면 현재 revision을 담은 Invalid를 반환한다. 호출자는 현재 위치·조건·revision으로 새 동기 질의를 만든다.
- 시야 캐시는 출발/도착 위치, 레이어, 트리거 정책, 관찰자/대상 ID와 월드 변화를 반영한다. 이동 장애물도 무효화 책임에 포함한다.
- 접근성 캐시는 출발/도착 위치, agent type, area mask와 navigation revision을 반영한다.
- 시설 생성/파괴뿐 아니라 문·링크·지형 변화 등 해당 판정에 영향을 주는 변경도 무효화한다.
- 내비게이션 변경이 실제 질의에 반영된 시점에 revision을 올린다. 시설 변경 요청만으로 새 내비게이션 결과가 준비됐다고 간주하지 않는다.
- 공격 적중 시 현재 위치·사거리·시야를 동기로 재검증한다. 오래된 긍정 결과나 위치 양자화만으로 명중을 승인하지 않는다.

캐시와 Think 위상 분산으로 시작한다. 프로파일에서 프레임 튐이 확인된 뒤 일반 AI 조회의 예산과 오래된 결과 허용 여부를 정한다.
첫 요청은 예산을 넘어도 동기 계산해야 하므로 이후의 예산 역시 절대 비용 상한은 아니다.
명중 검사는 향후 일반 조회의 최적화와 별도로 현재 상태의 정확성을 유지한다.

## 이동 요청과 정지

MovementPathState는 None, Complete, Partial, Invalid다. 공개 Pending과 '목적지 변경 뒤 한 Think 주기 무시'를 모두 제거한다.
기존 Complete=2, Partial=3, Invalid=4의 숫자는 유지하며 삭제된 대기 값 1은 재사용하지 않는다.

- false: 이전 목적지·경로·관찰 상태에 변화 없음. 잘못된 좌표, 비활성/내비게이션 밖 개체, 내비게이션 밖 목적지, 적용 불가 경로를 거부한다.
- true: 새 경로를 동기로 계산·적용한 뒤 반환한다. Partial도 성공이다.
- 현재 목적지와 0.5m 이내이면, 목적지가 존재하고 현재 revision에서 기존 경로가 유효할 때만 true/무변경이다.
- 벽 파괴 등 revision 변경 뒤에는 같은 목적지도 재탐색한다.
- HasDestination과 PathEndPosition을 추가했다. Complete의 끝점은 수락한 목적지, Partial은 도달 끝점, None/Invalid는 현재 Position이다.
- HasArrived는 Complete 경로로 요청 목적지에 도착한 경우만 true다. Partial 끝에서 멈춰도 false다.
- Stop은 이동·목적지·경로를 비우고 None/HasDestination=false/HasArrived=false로 만든다. 중복 호출은 무해하다.
- Stop 이후 낡은 내부 계산이 이동을 재개시키면 안 된다. 일시정지/재개 기능으로 Stop을 재해석하지 않는다.

## 완전 차단과 차단물 공격

시설로 적의 경로를 완전히 막는 배치는 허용한다. 이를 거부하는 PlacementRule/PlacementFailure를 추가하지 않는다.
IObstructionQuery.TryFindBlocker는 정책을 실행하지 않고 차단물과 접근 위치를 동기로 찾는다.

입력 ObstructionRequest:
- Route: 실제 개체 위치, 원래 진격 목적지, agent type, area mask, navigation revision.
- PathEndPosition, FactionId, SearchRadius, AttackRange.

출력 ObstructionResult:
- Blocker: 진격을 막는 공격 가능한 적대 파괴 시설(Wall/Tower).
- ApproachPosition: 개체 쪽 접근 가능 영역에서 도달 가능하고 공격 사거리에 들어오는 위치.
- NavigationRevision: 탐색에 사용한 revision. false의 out은 default다.

단순 근접 시설은 후보 자격이 아니다. 시설 중심으로 이동하라고 돌려주거나 비적대 시설을 차단물로 고르면 안 된다.
TargetSelector/Brain은 원래 진격 목적지를 별도로 유지하고 차단 시설의 접근 위치로 이동한다.
실제 공격 시 유효성·사거리·시야를 다시 검사한다. 파괴 후 영향받는 조회를 무효화하고 원래 목적지를 재판단한다.

기획 4.3의 '2)'는 현재 목표 유지 다음의 순서다. 코드 Priority=0은 유지 검사 이후 후보끼리의 최우선이므로 서로 충돌하지 않는다.
단, 생존만으로 현재 목표를 유지하지 않는다. 추적/공격이 성립하지 않는 기존 목표는 IsEligible/CanRetain을 내려 차단물로 전환할 수 있게 한다.

## 고립 복구와 웨이브 집계

- 차단물 탐색 실패 시 반경을 한 번 확장한다. 경계 근처에 항상 유효 차단물이 존재한다고 가정하지 않는다.
- 정상 공격/공격 위치 배정 대기/일시정지는 고립 시간에서 제외한다. IsAttacking 하나만 무기한 신뢰하지 않고 실제 공격 가능성과 진행을 확인한다.
- 진짜 진행 실패는 재경로 → 검증된 안전 위치로 복구 → 최종 소멸 순으로 처리하고 원인을 기록한다.
- 복구 위치는 마지막 안전 위치 등 같은 접근 가능 영역의 검증된 위치다. 경로의 최근접 지점을 무조건 택해 플레이어 차단 시설을 넘어가지 않는다.
- 웨이브별 생성된 미해결 적 ID 집합을 관리한다. Died와 Despawned 모두 Remove(entityId)를 시도하고 최초 제거만 집계를 바꾼다.
- AliveEnemyCount는 이 집합의 크기다. 전체 전투 레지스트리나 Alive/Dying/Incapacitated 상태를 단순 합산하지 않는다.
- PendingEnemyCount는 아직 출현하지 않은 수이며 판정 Pending과 관계없다. 두 수가 모두 0일 때 완료 신호/보상을 한 번만 발행한다.
- 오래된 웨이브/세션 이벤트는 현재 집계에 반영하지 않는다. 처치 보상은 별도로 Died만 처리하므로 감시자 소멸에는 처치 보상이 없다.
- 1단계 웨이브 적은 부활하지 않는 기준으로 집계한다. 플레이어 부활/하수인 재소환은 해당 집계에 넣지 않는다. 적 부활 추가 시 재진입 규칙을 먼저 확장한다.

## 검증 범위

GameplayContractTests: 기본값이 명중/이동을 허용하지 않는지, Invalid 끝점 정규화, 비정상 좌표 거부,
부분 경로와 신선도 분리, 차단물 요청 유효성, 현재 목표 유지와 차단물 자격의 우선순위를 검증한다.

구현 추가 후 통합 검증할 항목:
1. 예약 중 Gain/TrySpend, 취소, 이중 확정, 재시작 후 낡은 핸들.
2. 건설 각 준비 단계 실패 시 마나·생성·점유·등록·이벤트 흔적 없음. 수신자 재진입과 예외.
3. 같은 목적지에서 벽 파괴 후 재경로, 실패 시 이전 상태 유지, Stop 이후 이동 재개 없음.
4. 부분 경로 → 접근 가능한 차단 시설 공격 → 파괴 후 진격 재개. 가까운 비차단/비적대 시설 배제.
5. 정상 공격 중 감시자 미작동, 복구가 차단 시설을 넘지 않음.
6. Died 후 Despawned, 직접 Despawned, 오래된 세션 이벤트가 집계/완료/보상을 중복시키지 않음.


실행 결과 (2026-09-06): Unity 6000.2.8f1 배치 EditMode 전체 32개 통과, 실패/건너뜀 0.
결과: Logs/InterfaceRevision/editmode-results.xml, 컴파일/실행 로그: Logs/InterfaceRevision/unity-editmode.log.
