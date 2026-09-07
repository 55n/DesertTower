2026-09-07 후속 결정: 단계별 슬롯/자유 배치, 포인트 선택형 성장, 코어 흡수/공격 전환은 [최신 결정 기록](E:/AI/Projects/DesertTower/Docs/Desert_Tower_결정기록_단계별배치_선택성장_코어전환_v0.1.md)을 따른다. 아래의 해당 항목은 이전 설계이며, 그 밖의 기존 계약은 유지한다.

# DESERT TOWER 인터페이스 구현 기준 v0.2

기준: 상세 게임기획서 v0.2, 컴포넌트 목록 v0.1 및 인터페이스 검토.
이 문서는 구현된 계약을 설명한다. 2026-09-06 승인안의 최신 기준은 [v0.4](Desert_Tower_인터페이스_개선_반영_v0.4.md)이며 관련 컴포넌트 목록도 갱신했다.

## 구현 범위
초기 26개에서 v0.3의 판정 계약 및 v0.4의 예약/차단 계약을 추가해 Assets/Interfaces에 인터페이스 34개를 둔다. 피해/체력/생명 이벤트 데이터는 Assets/Combat,
설치/경제/마법/진행 데이터는 Assets/Gameplay/Contracts에 둔다.
v0.3에서 어셈블리를 분리했다. 최신 구조와 추가 계약은 Desert_Tower_판정계약_구현_v0.3.md를 참조한다.
Health, AI, 경제, 설치 등의 실제 서비스 구현은 이 변경 범위가 아니다.

## 인터페이스 목록
- 전투: IDamageable, IHealth, IDamageEvents, ILifeState, ICombatTarget
- 개체 기능: IRepairable, IRevivable, IDespawnable, IFacility
- 입력과 AI: IPlayerInput, IMovementController, IAttackController, ITargetSelector, ICombatantQuery
- 설치: IPlacementValidator, IFacilityBuilder, IFacilityMaintenance
- 경제와 성장: IManaReader, IManaWallet, IManaReservation, IProgressionReader, IUnlockQuery
- 진행: IGameStateReader, IWaveStateReader, IGameCommands
- 마법: ISpellCaster, ISpellStateReader

## 공통 전투 계약
- IDamageable은 TakeDamage만 제공한다. 관찰은 IHealth, IDamageEvents, ILifeState로 분리한다.
- ICombatTarget은 개체 Guid, 진영 ID, 대상 분류, 피격 위치, 대상 가능 여부, 피해 수신부와 생명 상태를 연결한다.
- Guid는 한 개체의 모든 충돌체에서 같고 부활 시 유지한다. 새 생성/풀 재사용 개체에는 새 Guid를 부여한다.
- 범위 피해는 Guid별 한 번 적용한다.
- DamageInfo는 피해량과 가해 진영이 필수다. 공격자 Guid/원인/적중 위치/방향은 선택이다.
- default(DamageInfo)는 유효하지 않다. 수신부는 IsValid를 확인하고 InvalidRequest로 거부한다.
- 상태, 진영 관계, 보호 여부를 검사한 뒤 적용한다. 거부 요청은 이벤트를 내지 않는다.
- DamageResult.Applied의 양은 초과 피해를 뺀 실제 체력 감소량이다. 피해 0은 적용 가능하나 사망을 일으킬 수 없다.
- HealthChangedInfo는 이전/현재 최대 체력을 모두 포함하여 최대 체력 변경으로 인한 임계치 통과를 판단할 수 있다.
- 모든 이벤트는 체력/생존/대상 가능 여부/충돌/공격 상태가 최종 갱신된 후 발생한다.
- 체력 0: Alive에서 Dying 또는 Incapacitated로 전이한다. Died는 해당 전이마다 한 번이다.
- 부활: Incapacitated에서 Alive로 전이하며 최대 체력으로 회복한다. LifeSequence를 증가시킨다.
- 소멸: Died 없이 Removed로 전이하고 Despawned를 한 번 발행한다. Dying도 즉시 정리 요청을 받을 수 있다.
- 제거형 사망 연출 완료도 Removed 전이 및 Despawned를 한 번 통지한다. 이미 Removed이면 추가 이벤트가 없다.
- 이벤트 수신자 간 순서는 보장하지 않는다. 콜백 재진입에 의한 중복 처리도 구현에서 차단한다.
- CombatantRegistry는 존재하는 개체를 등록 유지하고 공격 조회에서 IsTargetable을 검사한다.
  Died 때 등록을 제거하지 않고 Despawned 때 제거한다. 따라서 부활 시 재등록 누락이 없다.
- 처치 보상은 적의 Died만 구독하며 (EntityId, LifeSequence)별 한 번 지급한다.
- MinionRoster는 사망 후 재소환을 예약하지만 소유 타워 소멸 시 예약을 취소하고 하수인을 소멸시킨다.
- 최종 제거의 단일 소유자는 개체 생명주기 구현이다. Presenter는 연출만 수행한다.

## 입력/AI 계약
- IMovementController는 적/하수인의 목적지 이동용이며 경로 계산·적용을 동기로 마친 뒤 성공을 반환한다.
- 실패는 이전 상태를 보존한다. Stop은 목적지/경로를 제거한다. Partial 끝점 도착은 HasArrived가 아니다.
- HasDestination/PathEndPosition을 제공하고 공개 대기 상태는 제거했다. 같은 목적지도 navigation revision 변경 시 재계산한다.
- IObstructionQuery는 적대 차단 시설과 접근 가능한 공격 위치를 반환한다. 원래 진격 목적지는 선택기가 별도로 유지한다.
- 공격 시작과 실제 적중 시 유효성/거리/시야를 재검사한다. Cancel은 발사 완료한 투사체에 영향이 없다.
- IPlayerInput.PrimaryActionPressed는 모드에 따라 공격 또는 설치 확정으로 라우팅한다.
  PrimaryAttackHeld는 전투 모드에서만 소비한다. 사망/일시정지 시 게임 입력을 차단한다.
- 슬롯 번호는 0부터, 회전 요청은 부호 있는 90도 단계 수다.
- 현재 목표가 없으면 CurrentTarget 및 SelectTarget은 null을 반환한다.
- ICombatantQuery.GetSnapshot의 목록은 호출 후 등록 변경에 영향받지 않는다.
- FactionProfile, LineOfSight, RangeQuery, Reachability는 데이터/유틸로 유지한다.

## 설치/수리/철거 계약
- 미리보기 검사는 참고값이다. TryBuild는 같은 요청으로 조건을 다시 확인한다.
- 성공하면 개체 생성, 격자 점유, 등록, 마나 차감을 모두 반영한다.
- 검증 후 마나를 예약하고 생성/점유/등록을 외부에 노출하지 않은 채 준비한다. 전체 확정 후 이벤트를 발행한다.
- 실패하면 준비 상태와 예약을 폐기한다. Gain으로 환불하지 않는다. 수신자 재진입은 작업 경계에서 직렬 처리한다.
- 잠금, 마나, 살아 있는 캐릭터 겹침, 금지 구역, 평탄도 등을 확정 시 검사한다.
- IFacilityMaintenance의 견적은 표시용이다. 실행 시 시설 존재/생존/준비 단계/일시정지/최신 체력과 비용을 재검사한다.
- 수리 비용은 복구 체력 비율 × 설치 비용 × 수리 비율. 정수 마나로 변환할 때 올림을 사용한다(초기 구현 기준).
- 철거 환급은 설치 비용 × 현재 체력 비율 × 환급 비율의 내림. 철거 실패 시 환급하지 않는다.
- 수리 및 철거도 실패 시 비용/체력/개체/점유를 부분 반영하지 않는다.
- 시설의 OccupiedCells는 변경 불가능한 스냅샷이다.

## UI/진행 계약
- UI는 IManaReader, IHealth, IProgressionReader, IUnlockQuery, IGameStateReader, IWaveStateReader,
  ISpellStateReader 등을 구독한다. 실행은 IGameCommands/IFacilityMaintenance 등 명시적 요청 창구를 쓴다.
- 여러 읽기 소스를 구독해도 된다. 뷰마다 반드시 하나라는 기존 규칙은 적용하지 않는다.
- IWaveStateReader 구현은 WaveDirector/PreparationTimer/NextRouteAnnouncer를 집계하는 표시용 객체로 둔다.
- WaveNumber는 첫 준비부터 1, PendingEnemyCount는 아직 출현하지 않은 수, AliveEnemyCount는 해당 웨이브의 미해결 적 ID 집합 크기다.
  Died/Despawned에서 같은 ID를 한 번만 제거한다. 1단계 웨이브 적은 부활하지 않으며 적 부활 추가 시 재진입 규칙이 필요하다.
  준비로 넘어가려면 두 수가 모두 0이어야 한다.
- 준비 시간 null은 첫 준비의 무제한, 전투 중에는 0이다.
- 일시정지는 GamePhase와 별도 값으로 두어 준비/전투 복귀 상태를 보존한다.
- 최대 레벨에서는 ExperienceToNextLevel과 ExperienceInLevel을 0으로 제공한다.
- TryCast는 최신 해금/생존/모드/마나/쿨다운을 재검사하고 성공 시에만 자원과 쿨다운을 반영한다.
- Restart는 씬 재로드를 초기 구현 기준으로 한다. 전장/경제/해금/타이머/예약을 모두 초기화한다.

## 기존 API 변경
- new DamageInfo(amount) -> new DamageInfo(amount, sourceFactionId, ...)
- new DamageResult(...) -> DamageResult.Applied(...) 또는 DamageResult.Rejected(...)
- 기존 타입 이름과 전역 네임스페이스를 유지한다.
- 구현 예정 서비스의 규칙은 계약이며 인터페이스 선언만으로 런타임 동작이 자동 보장되지는 않는다.
