/// <summary>현재 위치에서 목적지까지 갈 수 있는 길이 있는지 확인한다.</summary>
/// <remarks>
/// AI가 추적할 대상이나 이동할 위치를 판단할 때 쓴다. 실제로 움직이지는 않는다.
/// 끝까지 갈 수 있는지, 중간까지만 갈 수 있는지 구분한다. 길이 바뀌면 이전 결과도 다시 확인한다.
/// </remarks>
public interface IReachabilityService
{
    /// <summary>길 정보가 몇 번째로 갱신되었는지 나타낸다. 이전 결과를 다시 써도 되는지 확인할 때 쓴다.</summary>
    long NavigationRevision { get; }
    /// <summary>현재 위치에서 목적지로 갈 수 있는지 이 호출 안에서 확인한다.</summary>
    /// <remarks>
    /// 쓸 수 있는 저장 결과가 없으면 바로 계산한다. 현재 단계에서는 IsStale을 항상 false로 돌려준다.
    /// 요청이 잘못됐거나 요청의 길 정보 번호가 현재와 다르면 Invalid다. 길이 없다는 Unreachable과 구분한다.
    /// 결과에는 서비스의 현재 길 정보 번호를 넣는다.
    /// </remarks>
    ReachabilityResult Query(ReachabilityRequest request);
    /// <summary>저장해 둔 경로 결과를 지우고 길 정보 번호를 올린다.</summary>
    /// <remarks>
    /// 실제 길 변경이 새 조회에 반영된 뒤 호출한다. 다음 조회에서 변경 전 결과를 다시 쓰면 안 된다.
    /// </remarks>
    void Invalidate();
}
