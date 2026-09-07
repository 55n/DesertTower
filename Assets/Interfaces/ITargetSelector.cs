/// <summary>공격할 대상을 찾고, 현재 목표를 계속 따라가도 되는지 확인한다.</summary>
/// <remarks>
/// AI가 쓴다. 주변 대상의 시야와 길을 확인한 뒤 PriorityTargetPolicy로 대상을 고른다.
/// 길을 막은 시설을 공격하러 가더라도 원래 가려던 목적지는 따로 기억한다.
/// </remarks>
public interface ITargetSelector
{
    /// <summary>지금 선택한 대상과 접근 위치다. 대상이 없으면 HasTarget이 false다.</summary>
    TargetSelection CurrentSelection { get; }
    /// <summary>대상을 다시 판단하고 선택한다. 적절한 대상이 없으면 기본값을 돌려준다. 접근 위치와 길 정보 번호도 함께 전달한다.</summary>
    /// <remarks>
    /// 현재 목표는 계속 따라가거나 공격할 수 있을 때만 유지한다.
    /// 길이 중간에 막히면 IObstructionQuery로 길을 막는 적대 시설과 갈 수 있는 공격 위치를 찾는다. 가까운 시설이라는 이유만으로 고르지 않는다.
    /// 차단 시설을 상대하는 동안 원래 가려던 목적지는 따로 기억한다.
    /// 시설이 파괴되면 영향을 받은 저장 결과를 지우고 원래 목적지를 다시 판단한다.
    /// 유효한 현재 목표의 유지가 차단물 우선순위보다 먼저다. 살아 있기만 하고 접근할 수 없는 목표를 계속 붙잡지는 않는다.
    /// </remarks>
    TargetSelection SelectTarget();
    /// <summary>현재 선택한 목표를 지운다.</summary>
    void ClearTarget();
}
