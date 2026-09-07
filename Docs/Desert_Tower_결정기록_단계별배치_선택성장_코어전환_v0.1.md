# 단계별 배치·선택형 성장·코어 방식 전환 결정

기준일: 2026-09-07. 사용자 확인에 따라 기존 인터페이스를 확장했다.

이 문서는 배치 방식, 포인트 성장, 코어 도달 처리의 최신 기준이다. 기존 v0.4의 마나 예약·동기 조회·이동·완전 차단·미해결 적 집계 원칙은 유지한다. PPTX의 수치·개체 종류는 이 계약에 고정하지 않는다.

**확정된 방향과 기본 설정**

| 항목 | 기본 동작 | 변경 가능한 부분 |
|---|---|---|
| 준비 단계 건설 | 지정 슬롯만 허용 | 준비 단계의 자유 배치는 허용하지 않음 |
| 웨이브 단계 건설 | 지정 슬롯과 자유 배치 모두 허용 | 전투 설정으로 슬롯만/자유만/둘 다/건설 없음 선택 |
| 성장 | 포인트를 소비해 노드 선택 | 레벨 상한, 지급 포인트, 노드 비용·효과·선행 조건은 데이터 |
| 코어 | 도착한 적 즉시 흡수, 안정도 피해 | 세션 구성에서 지속 공격·파괴 방식으로 변경 가능 |

웨이브 단계에서 슬롯도 계속 허용하는 것은 자유 배치를 보존하면서 기존 슬롯도 활용할 수 있게 한 기본 설정이다. 전투를 자유 배치만 허용하도록 바꾸는 데 요청·건설 인터페이스 변경은 필요 없다. 기존 시설은 페이즈 전환 시 유지하며 새 건설 요청의 허용 여부만 바뀐다.

**반영한 코드의 범위**

- PlacementRequest에 PlacementKind와 SlotId, AtSlot 팩토리를 추가했다. 기존 위치·회전 생성자는 자유 배치로 유지한다.
- PlacementPhaseRule을 추가했다. 실제 현재 GamePhase/IsPaused를 읽고 배치 방식을 검사하는 순수 규칙이다. CompositePlacementValidator는 요청 형식·좌표·회전의 유효성을 먼저 검사한다.
- IFacility에 PlacementKind, BuildSlotId, OccupancySurfaceId를 추가했다. OccupiedCells는 표면 ID로 구분되는 월드 XZ 격자 스냅샷이다.
- ISkillTreeReader, ISkillTreeCommands, SkillNodeState를 추가했다. ActionFailure에는 포인트 부족·선행 미충족·이미 습득을 추가했다.
- ICoreInteraction, CoreInteractionMode, CoreArrivalResult를 추가했다. 흡수 완료와 일반 공격으로의 전환 지시를 구분한다.
- 기존 enum 항목 순서는 유지하고 새 실패 사유를 끝에 추가했다. Assets/Interfaces는 34개에서 37개로 늘었다.

실제 FacilityBuilder/슬롯 조회·점유 서비스, 스킬 습득 서비스, 코어 도달 서비스 및 EnemyBrain은 아직 없다. 이번 변경은 선언만으로 런타임 동작이 보장된다는 의미가 아니다. 레벨 저작·저장 스키마도 아직 변경하지 않았다. 슬롯 데이터와 OccupancySurfaceId의 실체 연결은 이들 구현 시 함께 해야 한다.

**건설의 공통 처리 경계**

슬롯 요청은 FacilityId와 SlotId를 전달한다. 요청의 Position/Rotation은 사용하지 않는다. 실행 시 권위 있는 슬롯 데이터에서 현재 위치·회전·허용 시설·사용 가능 상태를 해석한다. 자유 배치 요청은 기존처럼 월드 위치와 정규화된 회전을 전달한다. 요청자가 페이즈나 권한을 지정할 수 없다.

```csharp
var slotRequest = PlacementRequest.AtSlot("cobra", "middle-slot-02");
var freeRequest = new PlacementRequest("cobra", worldPosition, worldRotation);

// 준비 단계는 두 구성 모두 슬롯만 허용한다.
var bothDuringCombat = new PlacementPhaseRule(gameState);
var slotsOnlyDuringCombat = new PlacementPhaseRule(gameState, PlacementModes.DesignatedSlot);
var freeOnlyDuringCombat = new PlacementPhaseRule(gameState, PlacementModes.Free);
```

실제 건설 서비스는 PlacementPhaseRule을 검증 체인의 앞부분에 포함해야 한다. 이 규칙의 성공은 페이즈/방식 허용만 뜻한다. 슬롯 존재·슬롯의 시설 허용 목록·점유·건설 구역·시야/물리 조건·마나·해금 등 다른 규칙을 통과해야 건설할 수 있다. 미리보기 때 허용되었어도 확정 시 페이즈가 바뀌면 다시 거부한다.

두 방식 모두 최신 검증, 마나 예약, 생성·점유·등록 준비, 전체 상태 확정, 이벤트 발행의 기존 작업 경계를 공유한다. 실패 시 부분 생성·점유·차감·이벤트를 남기지 않는다. 철거 역시 건설 방식과 무관하게 같은 제거/환급 경계를 사용하고, 슬롯 시설이면 슬롯 연결도 함께 해제한다.

슬롯 점유 테이블과 자유 배치 격자만 따로 관리하면 서로 겹치는 시설을 놓을 수 있으므로 최종 공간 충돌 판정은 공유한다. 같은 층의 슬롯과 자유 배치는 동일한 OccupancySurfaceId를 사용한다. 겹치는 XZ의 다른 층은 다른 표면으로 구분하되 큰 시설이 다른 층까지 침범하는 실제 부피 충돌은 별도로 검사한다. 빈 슬롯 영역을 자유 배치에서 항상 예약할지는 콘텐츠 배치 규칙이며 이번 코드에서 임의로 고정하지 않는다.

**포인트 선택형 성장**

IProgressionReader는 경험치·레벨, ISkillTreeReader는 남은 포인트·노드 상태·선행 조건·현재 습득 가능 여부, IUnlockQuery는 최종 사용 가능 기능의 조회를 담당한다. 레벨업만으로 모든 해당 레벨 노드를 자동 해금하지 않는다. 포인트 지급은 성장 서비스와 데이터에서 담당한다.

ISkillTreeCommands.TryLearn(nodeId)가 유일한 공개 습득 명령이다. 실행 시 노드 존재, 선행 습득, 포인트, 중복 습득, 게임 상태 정책을 재검증한다. 포인트 차감·노드 습득·능력치 효과·기능 해금을 모두 확정한 뒤 통지한다. 실패와 중복 요청은 추가 비용·효과·이벤트를 만들지 않는다. 이전 세션 작업과 이벤트는 재시작 후 상태를 바꾸지 못한다.

현재 스냅샷의 선행 목록은 모두 충족해야 하는 방식이다. 실제 노드 데이터는 존재하지 않는 선행 ID와 순환 참조를 검증해야 한다. 습득 가능 페이즈, 시작 포인트와 레벨업 지급량은 아직 확정하지 않았으며 인터페이스에 상수로 넣지 않았다. 스킬 초기화 기능은 추가하지 않았다.

**코어 방식을 바꾸는 경계**

EnemyBrain은 코어에 유효하게 도달했을 때 코어에 결합된 ICoreInteraction.TryHandleArrival(enemy)를 호출한다. 실제 서비스가 CoreInteractionMode를 세션 시작 시 주입받도록 구현한다. 기본 enum 값은 AbsorbOnArrival이다. 진행 중인 세션에서 모드를 바꾸는 기능은 이번 범위에 넣지 않는다.

| 모드 | 도달 처리 | 이후 적 동작 |
|---|---|---|
| AbsorbOnArrival | 해당 적의 도달을 한 번 확정하고 설정된 코어 피해·적 제거·웨이브 해소를 함께 반영 | Absorbed 결과. 일반 코어 공격을 실행하지 않음 |
| AttackUntilDestroyed | 도달 자체로 피해·제거·웨이브 해소를 하지 않음 | AttackRequired 결과. 기존 IAttackController로 코어 공격 상태에 진입 |

이렇게 하면 코어 방식 변경이 피해·체력·마나·UI 인터페이스에 퍼지지 않는다. 공격 모드에서는 코어의 공격 대상 등록/IsTargetable과 공격 접근 위치도 실제 모드에 맞게 조립해야 한다. 두 모드의 도달 처리 분기는 구현 예정 코어 서비스에 둔다.

도달 판정은 실제 위치와 현재 세션·적 ID·생명/진영·코어 상태를 검사한다. Partial 경로 끝이나 검증되지 않은 충돌 콜백만으로 흡수를 허용하지 않는다. 여러 콜라이더·중복 호출에도 코어 피해와 적 해소는 한 번이다. 흡수는 Died를 위조하지 않고 기존 Despawned 경로와 웨이브 ID 중복 방지 규칙을 활용한다. 별도 흡수 보상 여부는 미정이다.

안정도·적 상태·제거를 확정한 뒤 통지하고, 마지막 적 흡수로 안정도가 0이 되면 패배를 승리보다 우선한다. 공격 모드의 AttackRequired는 공격 허가나 명중 확정이 아니며 실제 공격은 현재 사거리·시야·대상 유효성을 다시 검사한다. 코어 피해량, 최대 안정도, 공격 간격 등은 데이터로 둔다.

**기존 문서와의 관계**

- 자유 배치의 전면 삭제 권고는 적용하지 않는다. 준비 단계만 슬롯으로 제한하며 전투 자유 배치 계약을 보존한다.
- 레벨 자동 해금 설계는 포인트 지급과 명시적 노드 습득으로 대체한다.
- 방어장치 문서의 코어 접촉 피해 전면 금지는 흡수 모드의 검증된 도달 처리에 한해 대체한다. 원시 OnTriggerStay에서 반복 피해를 주는 것은 여전히 허용하지 않는다.
- 마나 충전, 기본 공격 비용, 강화/수리/환급, 보스 기믹과 이동 능력의 나머지 검토 항목은 이번 변경에 포함하지 않는다.

**검증**

ConfigurableGameplayContractTests는 준비/전투 방식 허용, 전투 설정 조합, 페이즈 변경 후 이전 요청 거부, 일시정지·종료 상태 차단, 비정상 요청/좌표·회전 거부, 스킬 선행 목록 스냅샷 보존, 실패한 코어 결과의 무동작을 검증한다.

건설의 실제 점유/마나 원자성, 스킬 실제 포인트 소비, 두 모드의 실제 코어 피해·적 제거·공격 전환·승패 순서는 서비스 구현 후 통합 검증해야 한다. 이번 테스트가 그 구현 완료를 뜻하지 않는다.

실행 결과: Unity 6000.2.8f1 EditMode 전체 50개 통과, 실패/건너뜀 0. 신규 ConfigurableGameplayContractTests 18개 포함. [결과 XML](E:/AI/Projects/DesertTower/Logs/GameplayModes/editmode-results.xml), [실행 로그](E:/AI/Projects/DesertTower/Logs/GameplayModes/unity-editmode.log).
