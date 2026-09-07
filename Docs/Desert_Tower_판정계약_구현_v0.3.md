# 판정 계약과 어셈블리 분리 v0.3

2026-09-06: 승인된 동기 조회·예약·차단 계약으로 설명을 갱신했다. 최신 통합 기준은 [v0.4](Desert_Tower_인터페이스_개선_반영_v0.4.md)다.

## 추가된 계약
- IVisibilityService: 동기 시야 Query, 월드 revision 관찰, 캐시 무효화.
- IReachabilityService: 동기 접근성 Query, 부분 경로 끝점, navigation revision 관찰, 캐시 무효화.
- ITargetPolicy: 미리 판정된 후보에 대한 순수 목표 선택.
- IPlacementRule: 부작용 없는 배치 검사 하나.
- ISpatialCombatQuery: 재사용 목록을 채우는 반경/적대 조회.
- ICombatantRegistry: 개체 등록/해제. 조회와 변경 접근을 분리.

IMovementController의 동기 경로 적용/실패/정지 계약과 IAttackController의 현재 시야 재검증을 명시했다. IManaReservation과 IObstructionQuery를 추가했다.
현재 인터페이스는 총 34개다. 실제 Physics/NavMesh/공간 격자/캐시/풀링/예산 스케줄러 구현은 아직 없다.

## 구현된 순수 로직
PriorityTargetPolicy는 유효한 현재 목표 유지 후 작은 Priority, 가까운 DistanceSquared,
Guid 순으로 결정한다. selector는 매 판단마다 중복 없는 후보 스냅샷을 만들고
진영/사거리/시야/접근/추적 한계 및 재추적 금지를 계산한다.
적의 우선순위는 차단 시설 0, 플레이어/하수인 1, 길옆 타워 2, 코어 3으로 제공한다.
타워는 후보에 같은 우선순위를 주어 최근접 정책으로 사용할 수 있다.
이미 무효화된 목표는 IsEligible=false로 만들고, 현재 목표 유지가 불가능하면 CanRetain=false로 전달한다.
정책은 Guid만 반환한다. selector는 이를 조회하여 목표를 연결하고 공격 직전에 다시 유효성을 검사한다.

CompositePlacementValidator는 생성자에서 규칙 목록을 복사하며 등록 순서대로 평가하고
첫 실패에서 중단한다. 빈 규칙 목록과 null 규칙은 거부한다.
검사 순서는 조립 시 싼 검사부터 지정하며, 확정 시에도 전체 검사를 다시 수행한다.
개별 지형/점유/마나 규칙 구현은 아직 없다.

## 판정 결과와 무효화

두 서비스는 동기 Query(request), 현재 revision 속성, Invalidate()를 제공한다.
Request/TryGetResult/Release, 요청 ID, 공개 Pending은 제거했다. 캐시가 없으면 같은 호출 안에서 계산한다.
default 결과는 Invalid이며 이를 Blocked/Unreachable로 해석하지 않는다.
IsStale은 판정과 독립된 메타데이터이고 1단계 서비스에서는 항상 false다.
ReachabilityResult는 Partial과 EndPosition을 제공한다. Invalid 끝점은 zero이며 이동에 쓰지 않는다.

서비스가 WorldRevision/NavigationRevision을 공급한다. 요청 revision이 현재와 다르면 현재 revision을 담은 Invalid다.
호출자는 현재 입력과 revision으로 새 동기 질의를 만든다. 캐시 키는 모든 질의 조건과 관련 revision을 포함한다.
시설/문/링크/지형 및 이동 장애물처럼 판정에 영향을 주는 변화는 캐시를 무효화한다.
내비게이션 revision은 변경된 탐색 상태가 실제로 사용 가능한 시점에 갱신한다.
명중 시에는 현재 상태를 즉시 재검증하며 오래된 긍정 결과를 사용하지 않는다.
1단계는 캐시와 Think 분산만 사용한다. 예산은 측정 후 도입하며 명중 정확성을 낮추지 않는다.

## 어셈블리
- DesertTower.Combat.Runtime: Assets/Combat. 피해/체력/생명 이벤트 데이터. Unity 기본 타입만 참조.
- DesertTower.Gameplay.Abstractions: Assets/Interfaces 및 Assets/Gameplay/Contracts(asmref).
  Combat.Runtime만 프로젝트 참조하며 AI Navigation/Input System 의존성은 없다.
- DesertTower.Gameplay.Runtime: Assets/Gameplay/Runtime.
  Abstractions 및 Combat.Runtime 참조. 현재는 순수 정책/검사 조합만 포함한다.
- 기존 Levels 및 Diagnostics 어셈블리는 변경하지 않았다.

UI/테스트는 필요한 Abstractions와 Combat 계약을 참조하며 Gameplay 구현을 참조할 필요가 없다.
순수 정책 구현 자체를 테스트할 때만 Gameplay.Runtime을 참조한다.
v0.2 문서의 'Assembly-CSharp에서 컴파일/asmdef 미도입' 설명은 이 문서로 대체한다.
인터페이스 파일과 기존 데이터 파일의 위치는 유지했다.
