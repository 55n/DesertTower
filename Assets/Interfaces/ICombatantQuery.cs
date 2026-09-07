using System;
using System.Collections.Generic;

/// <summary>전장에 등록된 캐릭터나 시설을 찾는다.</summary>
/// <remarks>
/// AI 등이 대상 목록을 읽거나 ID로 대상을 찾을 때 쓴다. 목록에 추가하거나 빼지는 않는다.
/// 목록에 있어도 지금 공격할 수 있는지는 따로 확인해야 한다.
/// </remarks>
public interface ICombatantQuery
{
    /// <summary>지금 등록된 대상 목록을 돌려준다. 이후 등록이나 해제가 이 목록을 바꾸지는 않는다.</summary>
    IReadOnlyList<ICombatTarget> GetSnapshot();
    /// <summary>ID로 등록된 대상을 찾는다. 찾으면 true, 없으면 false와 null을 돌려준다.</summary>
    bool TryGet(Guid entityId, out ICombatTarget target);
}
