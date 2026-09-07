/// <summary>길을 막은 시설과, 그 시설을 공격하러 갈 수 있는 위치를 찾는다.</summary>
/// <remarks>
/// 적이 시설에 막혀 코어로 가지 못할 때 쓴다. 가까운 시설이라고 아무거나 고르지는 않는다.
/// 찾은 결과만 알려 준다. 실제로 이동하거나 공격하는 일은 AI가 맡는다.
/// </remarks>
public interface IObstructionQuery
{
    /// <summary>길을 막는 적대 시설과, 그 시설을 공격할 수 있는 접근 위치를 찾는다.</summary>
    /// <remarks>
    /// PathEndPosition 주변에서 원래 목적지 Route.Destination을 막는, 공격 가능한 적대 Wall/Tower만 찾는다. 가까운 것만으로는 부족하다.
    /// 이동 대상의 종류, 이동 허용 구역, 현재 길 정보 번호를 확인한다.
    /// 접근 위치는 Route.Origin 쪽 영역에서 실제로 갈 수 있고 AttackRange 안에 있어야 한다.
    /// 잘못됐거나 오래된 요청, 적절한 시설이 없는 경우 false와 default 결과를 돌려준다.
    /// 목표 우선순위를 정하거나 경로를 바꾸거나 이동·공격을 실행하지 않는다. 실제 명중 조건은 공격하는 쪽이 다시 확인한다.
    /// </remarks>
    bool TryFindBlocker(ObstructionRequest request, out ObstructionResult result);
}
