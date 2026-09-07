> 이 문서는 변경 전 검토 기록입니다. 이후 반영 내용은 [인터페이스 구조 개선 작업 기록](Desert_Tower_인터페이스_구조개선_작업기록_2026-09-08.md)을 참고하세요.

# 인터페이스 구성 검토와 메서드 주석 정리

검토일: 2026-09-08

Assets/Interfaces의 37개 인터페이스, 관련 데이터·판정 코드·레벨 데이터, 최신 PPTX와 후속 결정을 비교했다. 준비 단계는 슬롯, 웨이브는 설정에 따라 슬롯/자유 배치, 성장은 포인트 선택, 코어는 흡수 기본/공격 전환 가능이라는 사용자 결정을 우선한다.

이번에는 115개 메서드·속성·이벤트의 주석을 쉬운 한글로 정리했다. 편집기에서 설명을 확인할 수 있는 XML 문서 주석을 사용했다. 영어로 적혀 있던 실패 처리·중복 처리·순서 규칙도 옮겼다. 주석을 제외한 코드가 변경 전과 같은지, 설명이 빠진 항목은 없는지, XML 형식이 올바른지 확인했다. 함수 이름·인자·반환값·실제 동작은 변경하지 않았다.

**판정: 큰 구분은 괜찮다. 구현 전에 연결을 보완할 곳이 있다.**

37개 인터페이스가 37개 컴포넌트나 서비스를 뜻하지는 않는다. 예를 들어 Health 하나가 IDamageable·IHealth·IDamageEvents·ILifeState를 함께 맡아도 된다. UI는 체력만 읽고, 공격은 피해만 요청하고, 보상 코드는 사망만 듣도록 각자 필요한 부분을 나눈 것이다.

현재 Gameplay/Runtime에는 PlacementPhaseRule, CompositePlacementValidator, PriorityTargetPolicy가 있다. 건설·성장·코어·이동 등 실제 서비스 대부분은 아직 없다. 아래는 실행 중 발견한 버그 목록이 아니라 서로 다른 담당자가 구현을 연결하기 전에 정할 설계 항목이다.

**우선 보완할 부분**

| 순서 | 발견한 점 | 그대로 구현하면 생길 수 있는 문제 | 권장 방향 |
|---|---|---|---|
| 1 | 목표 선택 결과에 접근 위치가 없다 | 벽을 찾았어도 벽 중심으로 가거나 같은 검사를 다시 해야 함 | 선택 결과에 대상·접근 위치·길 정보 번호를 함께 담기 |
| 2 | 슬롯 ID를 실제 슬롯 정보로 연결하는 부분이 없다 | 미리보기·검사·건설이 위치와 빈자리를 따로 판단함 | 슬롯 데이터를 만들고 여러 코드가 같은 정보를 읽도록 연결 |
| 3 | 점프·대시 입력과 표시할 상태가 없다 | 입력·이동·HUD가 서로의 구현을 직접 뒤져야 함 | IPlayerInput 보완. HUD에 필요한 이동 능력 상태 제공 |
| 4 | 시설 강화 요청과 강화 상태가 없다 | 수리에 강화를 끼워 넣거나 UI가 시설 능력치를 직접 바꾸기 쉬움 | 시설 관리에 강화 요청·비용 확인 추가. 상태는 값으로 제공 |
| 5 | IFacility를 UI에 주면 수리·제거까지 호출할 수 있다 | 마나·환급·자리 정리를 맡는 서비스를 우회할 수 있음 | UI에는 필요한 값만 담은 시설 표시 데이터 전달 |

**1. 목표와 이동 위치가 이어지지 않는다**

[ITargetSelector.SelectTarget](E:/AI/Projects/DesertTower/Assets/Interfaces/ITargetSelector.cs:18) 는 목표 ICombatTarget만 돌려준다. 반면 [ObstructionResult](E:/AI/Projects/DesertTower/Assets/Gameplay/Contracts/ObstructionContracts.cs:35) 에는 차단물뿐 아니라 갈 수 있는 공격 위치와 NavigationRevision이 있다. 현재 설명대로 목표 선택 코드가 차단물을 찾는다면 이 추가 정보를 이동 담당 코드에 넘길 방법이 없다.

적 앞에 두꺼운 벽이 있으면 적이 가야 할 곳은 벽 중심이 아니라 벽 앞의 공격 가능한 지점이다. 대상으로 벽만 넘기면 이 지점을 잃어버린다.

TargetSelection 같은 결과 데이터에 Target, 선택적인 ApproachPosition, NavigationRevision을 함께 담는 것을 권한다. 움직이지 않는 타워는 접근 위치를 쓰지 않아도 된다. 원래 진격 목적지는 따로 유지하고, 벽 파괴나 길 변경 후 결과를 다시 구한다. 이동·목표 선택을 하나의 큰 인터페이스로 합칠 필요는 없다.

**2. 슬롯 번호는 있지만 슬롯 정보가 없다**

[PlacementRequest](E:/AI/Projects/DesertTower/Assets/Gameplay/Contracts/PlacementContracts.cs:14) 은 SlotId를 받는다. 하지만 [LevelMarker](E:/AI/Projects/DesertTower/Assets/DesertTowerLevelEditor/Runtime/LevelMarker.cs:7) 와 [LevelLayout](E:/AI/Projects/DesertTower/Assets/DesertTowerLevelEditor/Runtime/LevelLayout.cs:35) 에는 지정 슬롯의 위치·회전·허용 시설·빈자리·바닥 ID를 다루는 전용 데이터와 연결이 없다. 전용 MarkerKind가 없다는 사실만으로 건설이 불가능하다는 뜻은 아니지만, 현재 코드에는 이를 대신하는 연결도 없다.

먼저 고정된 슬롯 데이터와 게임 중 사용 상태를 구분해 만든다. 미리보기·검사·건설이 함께 읽으므로 IBuildSlotQuery 같은 작은 조회 인터페이스는 도입할 이유가 있다. 슬롯 하나하나를 IBuildSlot로 만들거나 조회/검색/위치 계산마다 인터페이스를 나눌 필요는 없다. 슬롯 자체는 데이터로 충분하다.

공유할 정보는 슬롯 ID, 현재 위치·회전, 허용 시설, 사용 중인 시설 ID, OccupancySurfaceId 정도다. 건설에서는 마나 예약과 자리 확보를 같이 처리하고, 자유 배치 시설과의 실제 공간 겹침도 확인한다. 요청 형식과 준비/전투 배치 설정 자체는 이미 나뉘어 있으므로 유지할 수 있다.

**3. 점프·대시가 입력과 HUD까지 연결되지 않는다**

[IPlayerInput](E:/AI/Projects/DesertTower/Assets/Interfaces/IPlayerInput.cs:9) 에는 이동·시점·공격·마법 입력은 있지만 JumpPressed, DashPressed에 해당하는 알림이 없다. InputSystem_Actions 자산에는 Jump/Interact/Sprint가 있다. 입력 자산이 있는 것과 게임 코드로 전달하는 기능이 있는 것은 다르다. 근거 기획은 PPTX 5장과 7장이다.

점프·대시는 기존 IPlayerInput에 보완하는 편이 자연스럽다. 스킬 사용 가능 여부, 땅에 서 있는지, 추가 점프가 남았는지는 플레이어 이동 코드가 관리한다. HUD도 읽어야 하는 정보만 작은 읽기 인터페이스로 제공한다. SpellState는 비용·쿨다운을 표현하지만 공중 추가 점프 상태는 표현하지 않는다.

PlayerMotor 자체를 반드시 IPlayerMotor로 만들 필요는 없다. 입력을 받아 움직이는 컴포넌트로 시작해도 된다. IMovementController는 적·소환수의 목적지 이동에 맞으므로 플레이어 버튼 입력이나 점프를 섞지 않는다.

**4. 수리와 강화는 다르다**

[IFacilityMaintenance](E:/AI/Projects/DesertTower/Assets/Interfaces/IFacilityMaintenance.cs:8) 에는 수리·철거만 있고 강화 요청·강화 비용·현재 강화 상태가 없다. 기획의 시설 강화는 체력 회복과 다른 동작이다. 횟수나 비용 수치를 고정하자는 문제가 아니라, 강화를 표현할 자리가 필요하다. 근거 기획은 PPTX 8장이다.

현 규모에서는 같은 시설 메뉴가 사용하는 IFacilityMaintenance에 GetUpgradeQuote/TryUpgrade 정도를 더하는 방식으로 시작할 수 있다. 강화 상태는 표시용 데이터로 전달한다. 별도 강화 모듈이나 여러 사용처가 필요해지면 분리해도 된다. 시설마다 IUpgradeable을 의무적으로 붙일 필요는 없다.

**5. 시설 정보와 시설을 바꾸는 기능이 한꺼번에 전달된다**

[IFacility.Repairable](E:/AI/Projects/DesertTower/Assets/Interfaces/IFacility.cs:25) 과 [IFacility.Despawnable](E:/AI/Projects/DesertTower/Assets/Interfaces/IFacility.cs:27) 은 IFacility를 받은 코드가 직접 회복·제거를 호출할 수 있게 한다. IFacility가 건설·수리·철거 서비스 내부에서만 쓰인다면 문제가 아니다. 하지만 BuildResult에서도 IFacility를 돌려주므로 UI가 이를 그대로 보관하기 쉬운 형태다.

UI에는 FacilityViewData 같은 값 묶음으로 ID·체력·배치 방식·강화 상태 등 필요한 정보만 주는 편을 권한다. 수리·철거는 IFacilityMaintenance에 요청한다. 이 표시 데이터는 인터페이스일 필요가 없다. IFacility 자체는 실제 기능을 연결하는 서비스 내부에서 유지할 수 있다.

OccupiedCells에는 격자로 자리를 관리하는 방식도 드러난다. 점유 서비스를 만들 때 이를 일반 UI와 전투 코드까지 전달하지 않는 편이 낫다. 슬롯/자유 배치의 공통 공간 검사와 여러 층 구분은 유지해야 한다.

**기획 기능을 구현할 때 연결할 부분**

| 기능 | 필요한 연결 | 지금 꼭 새 인터페이스가 필요한가? |
|---|---|---|
| 코어 마나 충전 | 충전 시작·중단·범위 확인과 마나 증가 | 자동 충전이면 충전 컴포넌트와 IManaWallet로 시작 가능. 버튼 조작/상태 표시가 필요할 때 해당 부분만 공개 |
| 층별 위험 표시 | 적 위치를 층별로 세고 UI에 전달 | 전투 서비스와 UI가 함께 쓰므로 작은 읽기 인터페이스가 유용. 전체 웨이브 적 수와는 구분 |
| 보스 체력 표시 | 현재 보스 연결과 등장·퇴장 처리 | HP는 IHealth 재사용. IBossHealth는 불필요 |
| 시설 일시 정지 | 정지 남은 시간과 가동 여부를 공격·소환·HUD에 전달 | 기믹을 유지하면 공유할 상태는 필요. 범용 IStatusEffect부터 만들 필요는 없음 |
| 소환 타워 귀속 | 소유 타워와 소환수 연결, 재소환, 타워 제거 시 정리 | MinionRoster 같은 클래스부터 구현 가능. 외부에서 읽거나 명령할 필요가 생길 때만 인터페이스 추가 |
| 경험치·포인트 지급 | 처치 보상과 레벨업을 성장 서비스에 연결 | Reader를 쓰기 기능으로 바꾸지 않음. 지급 담당 서비스의 내부 메서드로 시작 가능 |
| 보스 방패·경로 건너뛰기 | 피해 방향·공격 출처 규칙, 이동 구간 연결 | 공통 피해·이동 기반을 재사용하고 보스 전용 코드와 데이터에서 처리 |

실제 서비스가 아직 없는 것을 전부 인터페이스 누락으로 세지는 않았다. 서로 다른 코드 사이에서 무엇을 주고받을지가 정해질 때 필요한 부분만 추가하면 된다.

**인터페이스를 줄여도 되는 후보**

현재 가장 분명한 선택적 후보는 [ITargetPolicy](E:/AI/Projects/DesertTower/Assets/Interfaces/ITargetPolicy.cs:9) 다. 실제 구현은 PriorityTargetPolicy 하나이고, 적별 우선순위 차이는 이미 TargetCandidate.Priority로 전달한다. 이런 계산 코드는 구체 클래스를 직접 테스트할 수 있다. 테스트한다는 이유만으로 인터페이스가 필수는 아니다.

대상을 고르는 알고리즘을 여러 종류로 바꿔 끼울 계획이 없다면 PriorityTargetPolicy 클래스만 사용해도 된다. 반대로 실제로 서로 다른 선택 규칙을 교체할 필요가 있으면 유지하면 된다. 유지 자체가 오류는 아니며 이번에는 삭제하지 않았다.

IManaReservation은 메서드가 적어도 지갑 내부를 드러내지 않고 예약의 사용/취소만 허용하는 이유가 있다. 단순히 메서드가 하나라는 이유로 없애지 않는다. ICoreInteraction도 사용자가 요청한 흡수/공격 전환 지점이라 유지할 이유가 분명하다.

비용표·스킬 정의·슬롯 위치·피해 요청·판정 결과·레벨 배치처럼 값이 중심인 것은 클래스/구조체/ScriptableObject가 알맞다. 현재 DamageInfo, PlacementRequest, SkillNodeState, CoreArrivalResult, LevelLayout을 데이터로 둔 방향은 적절하다. 모든 Player, Tower, Boss, Projectile에 기계적으로 I 접두사의 인터페이스를 추가하지 않는다.

**37개 인터페이스별 판단**

| 인터페이스 | 판단 | 이유 / 남은 점 |
|---|---|---|
| IAttackController | 유지 | 적·타워·소환수의 공격 요청. 대상 선택과 구분 |
| ICombatantQuery | 유지 | 등록 목록 읽기와 ID 검색. 등록 변경 권한 불필요 |
| ICombatantRegistry | 유지 | 생성·제거 담당 코드가 목록 변경. Query와 같은 클래스가 구현해도 됨 |
| ICombatTarget | 유지 | 공격에 필요한 정보와 피해 전달. 캐릭터 종류에 덜 의존 |
| ICoreInteraction | 유지 | 흡수/공격 방식 변경 지점. 내부 처리까지 잘게 나눌 필요 없음 |
| IDamageable | 유지 | 여러 공격 방식이 같은 피해 요청을 사용 |
| IDamageEvents | 유지 | 피격 효과를 듣는 쪽은 피해 요청 권한 불필요 |
| IDespawnable | 유지 | 처치와 다른 제거. 철거·흡수·소환수 정리에 재사용 |
| IFacility | 사용 범위 축소 권장 | 서비스 내부 연결용으로 유지 가능. UI에는 필요한 값만 전달 |
| IFacilityBuilder | 유지 | 슬롯/자유 배치의 공통 건설 요청. 비용과 자리 처리를 함께 담당 |
| IFacilityMaintenance | 강화 보완 | 수리/철거와 같은 시설 메뉴에서 강화도 다룰 수 있음 |
| IGameCommands | 유지 | UI/입력에서 게임 제어 요청 |
| IGameStateReader | 유지 | 다른 시스템이 단계와 일시정지를 읽음 |
| IHealth | 유지 | HP 표시와 피해 요청 권한을 구분 |
| ILifeState | 유지 | 사망·부활·최종 제거의 의미가 다름 |
| IManaReader | 유지 | 마나 표시와 소비·획득을 구분 |
| IManaReservation | 유지 | 비용을 잠시 확보하고 사용/취소하는 제한된 기능 |
| IManaWallet | 유지 | 충전·건설·마법의 공통 잔액 계산 |
| IMovementController | 유지 | 적·소환수 목적지 이동. 플레이어 입력 이동과 구분 |
| IObstructionQuery | 유지 | 길을 막는 시설과 공격 위치를 함께 찾음 |
| IPlacementRule | 유지 | 서로 다른 검사들을 조합. PhaseRule과 Composite가 실제로 사용 |
| IPlacementValidator | 유지 | 개별 검사들을 묶어 미리보기와 건설에 제공 |
| IPlayerInput | 이동 입력 보완 | 점프·대시를 전달할 항목이 빠짐 |
| IProgressionReader | 유지 | 경험치/레벨 표시. 선택 스킬과 분리 |
| IReachabilityService | 유지 | 길이 있는지 확인. 실제 이동과 구분 |
| IRepairable | 유지 | 체력 회복만 수행. 비용 계산은 서비스가 담당 |
| IRevivable | 유지 | 같은 대상의 부활. 새 소환이나 일반 회복과 다름 |
| ISkillTreeCommands | 유지 | 포인트 소비·선행 검사·습득을 함께 처리 |
| ISkillTreeReader | 유지 | 선택 화면에 필요한 정보. Commands와 같은 클래스가 구현해도 됨 |
| ISpatialCombatQuery | 유지 | 반경 탐색과 일반 ID 조회의 목적과 비용이 다름 |
| ISpellCaster | 유지 | 조준 위치로 마법 사용. 비용·쿨다운·효과를 함께 처리 |
| ISpellStateReader | 유지 | 마법 아이콘 표시. 시전 권한 불필요 |
| ITargetPolicy | 선택적으로 축소 | 알고리즘 교체가 필요 없으면 구체 클래스만으로 충분 |
| ITargetSelector | 결과 보완 | 선택 대상 외에 접근 위치를 이동 코드에 전달할 필요 |
| IUnlockQuery | 유지 | 다른 시스템이 스킬트리 구조를 몰라도 해금 여부 확인 |
| IVisibilityService | 유지 | 시야 검사 공유. 길 검사와 구분 |
| IWaveStateReader | 유지 | 이번 웨이브의 등장 예정/남은 적 표시. 전체 등록 목록과 구분 |

인터페이스를 나눴다고 내부 데이터와 처리를 이중으로 만들 필요는 없다. 특히 IHealth/ILifeState, IManaReader/IManaWallet, ISkillTreeReader/ISkillTreeCommands는 같은 실제 객체가 함께 맡을 수 있다. IPlacementRule과 IPlacementValidator도 함수 모양은 비슷하지만 전자는 검사 하나, 후자는 검사 묶음이다.

**구현 전에 더 명확히 적을 반환 규칙**

- IAttackController.TryAttack의 true가 공격을 시작했다는 뜻인지, 준비 동작까지 예약했다는 뜻인지 정해야 한다. 이미 명중했다는 뜻으로 해석하면 안 된다.
- ICombatantQuery.TryGet과 ISpellStateReader.TryGetState는 실패했을 때 out에 무엇을 넣는지 통일해야 한다. null/default를 권하지만 이번 번역에서 새 규칙으로 확정하지 않았다.
- IGameCommands의 bool 결과는 이미 일시정지한 상태에서 같은 요청을 다시 받았을 때의 의미 등을 명시해야 한다.
- 대상 없는 상태, 최대 레벨, 제거/재시작 후 오래된 객체 참조를 구현 테스트에서 확인해야 한다. 기존 default 실패·중복 방지·재시작 규칙은 유지한다.

**권장 작업 순서**

1. 목표와 접근 위치를 함께 넘기는 결과, 슬롯을 읽는 연결을 먼저 정한다.
2. 점프·대시 입력, 시설 강화와 표시 데이터를 보완한다.
3. UI가 시설의 수리·제거를 직접 호출하지 않게 전달 범위를 정한다.
4. 실제 서비스를 구현하면서 층별 위험·시설 정지 등의 필요한 정보만 추가한다.
5. ITargetPolicy 축소는 필요하다면 정리한다. 개수를 줄이는 것 자체는 우선 과제가 아니다.

구조 변경 권고는 아직 API에 적용하지 않았다. 이번 변경은 주석 정리와 검토 기록이다. 동작 코드 변경이 없어 Unity 테스트는 다시 실행하지 않았고, 기존 50개 테스트 통과 기록을 새 기능의 구현 완료 근거로 사용하지 않았다.
