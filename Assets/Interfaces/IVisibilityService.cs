/// <summary>두 위치 사이에 장애물이 있어 대상이 가려지는지 확인한다.</summary>
/// <remarks>
/// 적·타워·소환수가 대상을 보거나 공격할 수 있는지 판단할 때 쓴다.
/// 보이는지만 알려 준다. 사거리나 피해 계산은 따로 처리하고, 공격이 맞는 순간에는 현재 위치로 다시 확인한다.
/// </remarks>
public interface IVisibilityService
{
    /// <summary>시야에 영향을 주는 월드 정보가 몇 번째로 갱신되었는지 나타낸다.</summary>
    long WorldRevision { get; }
    /// <summary>두 위치 사이의 시야를 이 호출 안에서 확인한다.</summary>
    /// <remarks>
    /// 쓸 수 있는 저장 결과가 없으면 바로 계산한다. 현재 단계에서는 IsStale을 항상 false로 돌려준다.
    /// 잘못된 요청이나 현재와 다른 월드 정보 번호는 Invalid다. 장애물에 가려졌다는 Blocked와 구분한다.
    /// 저장 결과를 쓸 때 위치·장애물 변화·레이어·트리거 검사 여부·관찰자와 대상 ID·월드 정보 번호를 확인한다.
    /// 공격이 맞는지 판단할 때는 대략적인 위치나 오래된 결과를 쓰면 안 된다.
    /// </remarks>
    VisibilityResult Query(VisibilityRequest request);
    /// <summary>저장해 둔 시야 결과를 지우고 월드 정보 번호를 올린다.</summary>
    /// <remarks>
    /// 월드 변경이 새 조회에 반영된 뒤 호출한다. 다음 조회에서 변경 전 결과를 다시 쓰면 안 된다.
    /// </remarks>
    void Invalidate();
}
