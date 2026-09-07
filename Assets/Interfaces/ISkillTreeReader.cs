using System;
using System.Collections.Generic;

/// <summary>남은 포인트와 배운 스킬, 지금 배울 수 있는 스킬을 알려 준다.</summary>
/// <remarks>
/// 스킬트리 화면을 그릴 때 쓴다. 어떤 스킬을 먼저 배워야 하는지도 알려 준다.
/// 정보를 보여 주는 기능만 둔다. 실제로 배우는 일은 ISkillTreeCommands에 요청한다.
/// </remarks>
public interface ISkillTreeReader
{
    /// <summary>스킬을 배우는 데 쓸 수 있는 남은 포인트다.</summary>
    int AvailablePoints { get; }
    /// <summary>화면에 표시할 현재 스킬 목록을 돌려준다. 이 목록을 바꿔서 실제 스킬 상태를 수정하면 안 된다.</summary>
    IReadOnlyList<SkillNodeState> GetSnapshot();
    /// <summary>지정한 스킬의 현재 상태를 찾는다. 없는 스킬이면 false와 default 상태를 돌려준다.</summary>
    /// <remarks>
    /// 화면에 보여 주기 위한 결과다. 실제로 배울 때는 조건을 다시 확인한다.
    /// </remarks>
    bool TryGetNodeState(string nodeId, out SkillNodeState state);
    /// <summary>포인트나 스킬 상태가 바뀌면 알린다. 포인트·배운 스킬·능력치 효과·해금을 모두 반영한 뒤 알린다.</summary>
    event Action Changed;
}
