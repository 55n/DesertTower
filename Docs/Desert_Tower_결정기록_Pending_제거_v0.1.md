# 결정 기록: 공개 Pending 제거

버전: 0.1 (2026-09-06 승인안 반영)
상태: 인터페이스·데이터·관련 문서 반영 완료. 실제 서비스 구현은 아직 없다.
최신 통합 기준: [인터페이스 개선 반영 v0.4](Desert_Tower_인터페이스_개선_반영_v0.4.md).

## 배경

접근성 Request/TryGetResult/Release 방식은 '아직 모른다'는 상태와 요청 해제 책임을 AI에 넘겼다.
현재 코드는 시야 서비스에도 같은 요청 모델을 사용하고 있었다. 두 서비스를 함께 단순화한다.
초기 규모에서 예산은 측정 후 도입하고, 우선 캐시와 판단 주기 분산으로 시작한다.

## 확정 계약

```csharp
public interface IReachabilityService
{
    long NavigationRevision { get; }
    ReachabilityResult Query(ReachabilityRequest request);
    void Invalidate();
}

public interface IVisibilityService
{
    long WorldRevision { get; }
    VisibilityResult Query(VisibilityRequest request);
    void Invalidate();
}
```

- Request/TryGetResult/Release와 요청 ID를 제거한다. 캐시가 비었으면 같은 호출 안에서 계산한다.
- ReachabilityState는 Invalid/Reachable/Unreachable/Partial, VisibilityState는 Invalid/Visible/Blocked다.
- 기본 결과는 Invalid다. Invalid는 벽에 막힘이나 도달 불가로 바꾸어 해석하지 않는다.
- ReachabilityResult.EndPosition으로 부분 경로 끝점을 반환한다.
- Stale을 판정 열거형에서 제거하고 IsStale 메타데이터로 분리한다. 1단계 반환값은 항상 false다.
- 서비스가 revision을 소유한다. 요청 revision 불일치는 현재 revision을 담은 Invalid이며, 현재 입력으로 다시 질의한다.
- 캐시 무효화는 이동 장애물과 시설·문·링크·지형 변경 등 해당 판정의 모든 변화를 포함한다.
- 내비게이션 revision은 변경된 탐색 상태가 실제로 사용 가능한 시점에 갱신한다.
- 실제 적중은 현재 위치·사거리·시야를 즉시 재검사한다. 오래된 긍정 결과로 통과시키지 않는다.

## 이동과의 통일

이전 기록의 '엔진 계산 중 마지막 PathStatus 유지, 한 Think 주기 무시'안과
이동 실패 기록의 'MovementPathState.Pending 공개'안을 모두 대체한다.

1단계 IMovementController.TrySetDestination은 경로 계산·적용을 동기로 끝내고 true를 반환한다.
Partial도 수락 가능한 결과다. false이면 기존 경로/목적지/관찰 상태를 유지한다.
따라서 공개 Pending과 목적지 변경 후 고정 시간 동안 판단을 무시하는 규칙이 모두 필요 없다.
공통 내부 경로 계산기/캐시 재사용은 구현 시 결정하며, 엔진 고유 상태를 Brain 계약으로 다시 노출하지 않는다.

## 예산과 일시정지

- 1단계는 시야·경로 모두 예산 미적용. Think 위상을 분산하고 유효한 결과를 캐시한다.
- 64회 시야/8건 경로라는 과거 수치는 측정 전 후보값이며 성능 보장이 아니다.
- 프로파일로 프레임 튐이 확인되면 일반 AI 조회에 한해 이전 결과 + IsStale 방식의 예산을 검토한다.
- 첫 요청은 항상 동기 계산하므로 예산이 절대적인 프레임 비용 상한은 아니다.
- 향후 예산을 도입해도 적중 검사에는 현재 상태의 정확성을 유지한다.
- 일시정지 중 Think를 멈추면 경로 질의도 멈춘다. 요청 큐를 중단/재개할 책임은 없다.
- 스폰 대기 수(PendingEnemyCount)와 공격 준비 동작은 이번 판정 Pending 제거 대상이 아니다.

## 반영 파일

IReachabilityService, IVisibilityService, IMovementController, IAttackController,
PerceptionContracts, GameplayState와 인터페이스 제안/구현 문서를 갱신했다.
이동 실패·완전 차단 기록도 동일한 동기 계약으로 정리했다.
