# 결정 기록: 이동 요청 실패 후 동작과 완전 차단 정책

버전: 0.1 (2026-09-06 승인안 반영)
상태: 인터페이스·데이터·관련 문서 반영 완료. 실제 이동/차단물 탐색/WaveDirector 구현은 아직 없다.
최신 통합 기준: [인터페이스 개선 반영 v0.4](Desert_Tower_인터페이스_개선_반영_v0.4.md).
관련 결정: [공개 Pending 제거](Desert_Tower_결정기록_Pending_제거_v0.1.md).

## 이동 실패와 성공

Try 계열의 실패는 기존 상태에 부작용이 없다는 원칙을 따른다.

| 상황 | 반환 | 상태 |
|---|---|---|
| 비정상 좌표, 목적지/개체가 NavMesh 밖, 컨트롤러 비활성, 새 경로 적용 불가 | false | 이전 목적지·경로·관찰 상태 유지 |
| 현재 목적지와 0.5m 이내이고 기존 경로가 현재 revision에서 유효 | true | 재계산 및 상태 변경 없음 |
| 그 외 수락 가능한 완전/부분 경로 | true | 경로를 동기로 계산·적용한 상태. Complete 또는 Partial |

Partial은 실패가 아니다. 요청에 실패한 적을 세울지 기존 경로로 계속 보낼지는 Brain이 결정한다.
이전 문서의 Pending 상태와 기존 속도로 계산 결과를 기다리는 동작은 폐기한다.
공개 Pending을 없애기 위해 고정된 한 Think 주기 동안 판단을 무시하는 규칙도 사용하지 않는다.
벽 파괴 등 revision이 변하면 목적지가 같아도 재탐색한다.

## 정지와 관찰 속성

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

- PathState: None/Complete/Partial/Invalid. 기존 완료 상태의 열거형 숫자는 유지한다.
- PathEndPosition: Complete는 수락 목적지, Partial은 가장 먼 도달점, 그 외는 Position.
- HasArrived: Complete 경로로 요청 목적지에 도착했을 때만 true. Partial 끝점 도착은 false.
- Stop: 이동·목적지·경로 제거. None, HasDestination=false, HasArrived=false. 반복 호출 무해.
- Stop 이후 낡은 내부 계산이 이동을 재개시키지 않게 한다. Stop은 일시정지/재개가 아니다.

## 완전 차단 정책

기획서 5.1은 설치물로 길을 완전히 막는 배치를 허용하고, 4.1은 차단 시설 공격 후 진격 재개를 요구한다.
완전 차단을 이유로 배치를 거부하는 규칙이나 실패 값을 추가하지 않는다.
ReachabilityResult는 Partial과 EndPosition을 제공해 접근 가능한 경계를 표현한다.

```csharp
public interface IObstructionQuery
{
    bool TryFindBlocker(ObstructionRequest request, out ObstructionResult result);
}
```

- 요청: 실제 개체 위치와 원래 진격 목적지가 담긴 ReachabilityRequest, 부분 경로 끝점, 진영, 탐색 반경, 공격 사거리.
- 결과: 경로를 막는 적대 파괴 시설, 같은 접근 가능 영역에서 도달 가능한 공격 위치, navigation revision.
- 시설은 공격 가능한 Wall/Tower여야 한다. 근처에 있다는 이유만으로 선택하지 않는다.
- 입력이 무효/오래됐거나 자격 있는 차단물이 없으면 false와 default 결과다.
- 질의는 탐색만 한다. 차단물 공격 정책과 목표 우선순위는 TargetSelector/Brain이 담당한다.
- 시설 중심을 목적지로 쓰지 않는다. 원래 목적지를 별도로 유지하고 결과의 공격 위치로 이동한다.
- 시설 파괴 후 관련 캐시를 무효화하고 원래 목적지로 다시 판단한다.

기획 4.3의 순서 1은 유효한 현재 목표 유지, 순서 2는 차단 시설이다.
코드 Priority=0은 유지 검사 이후 후보끼리의 최우선이다. PriorityTargetPolicy는 변경하지 않는다.
기존 목표가 살아 있어도 추적/공격할 수 없다면 IsEligible/CanRetain을 내려 차단물로 전환한다.

## 진행 감시와 웨이브 종료

1. 차단물 탐색 실패 시 반경을 2배로 한 번 확장한다. 경계에 유효 차단물이 반드시 있다는 가정은 하지 않는다.
2. 정상 시설 공격, 공격 위치 배정 대기, 일시정지는 고립 시간에서 제외한다. 실제 공격 가능성과 진행도 확인한다.
3. 진짜 진행 실패는 재경로 → 검증된 같은 접근 가능 영역의 안전 위치로 복구 → 최종 소멸 순으로 처리하고 기록한다.
4. 진격 경로의 최근접 지점으로 무조건 Warp해서 플레이어 차단 시설을 넘지 않는다.
5. 파괴 불가 지형만으로 끊긴 경로는 레벨 검증으로 잡고 런타임 감시자는 마지막 안전망으로 둔다.

WaveDirector는 웨이브별 미해결 적 ID 집합을 소유한다.
Died와 Despawned는 모두 집합 Remove(entityId)로 연결한다. 최초 제거만 카운트를 바꾼다.
죽은 뒤 제거되는 적을 두 번 차감하지 않으며 감시자가 직접 제거한 적도 웨이브를 붙잡지 않는다.
AliveEnemyCount는 집합 크기이고 Alive/Dying/Incapacitated 상태의 합계가 아니다.
출현 대기 수와 미해결 적 수가 모두 0일 때 완료 이벤트와 보상을 한 번 발행한다.
처치 보상은 별도 Died 처리이며 감시자 소멸에는 지급하지 않는다. 이전 웨이브/세션 이벤트는 무시한다.
1단계 웨이브 적은 부활하지 않는 기준이다. 적 부활을 추가할 때 재진입 규칙을 먼저 확정한다.

## 반영 파일과 남은 검증

IMovementController, IReachabilityService, IObstructionQuery, ITargetSelector, IWaveStateReader,
GameplayState, PerceptionContracts, ObstructionContracts, TargetCandidate, PlacementContracts를 갱신했다.
컴포넌트 목록의 BlockerDetector를 IObstructionQuery 구현 예정자로 명시하고 감시자/웨이브 설명을 맞췄다.

탐색 반경 2m와 목적지 재계산 생략 거리 0.5m는 초기 기준이다. 시설 크기·공격 사거리·맵을 포함한 통합 테스트로 조정한다.
현재 계약 테스트는 데이터와 순수 목표 정책만 검증한다. 실제 경로 이동/공격/복구/집계는 구현 후 통합 검증한다.
