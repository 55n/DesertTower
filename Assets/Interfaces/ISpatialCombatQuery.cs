using System.Collections.Generic;
using UnityEngine;

/// <summary>주변에서 공격 대상으로 삼을 수 있는 적대 대상을 찾는다.</summary>
/// <remarks>
/// 적·타워·소환수가 주변을 살필 때 쓴다. 한 대상을 여러 번 목록에 넣지 않는다.
/// 후보를 모아 줄 뿐이다. 보이는지, 갈 수 있는지, 누구를 먼저 공격할지는 따로 확인한다.
/// </remarks>
public interface ISpatialCombatQuery
{
    /// <summary>지정 위치 주변에서 공격 가능한 적대 대상을 results에 담는다.</summary>
    /// <remarks>
    /// 먼저 results를 비우고 같은 EntityId는 한 번만 넣는다. 이 목록을 나중에 쓰려고 보관하지 않는다.
    /// 결과의 순서, 시야가 열려 있는지, 길이 있는지는 보장하지 않는다.
    /// </remarks>
    void QueryCandidates(Vector3 center, float radius, string hostileToFactionId,
        List<ICombatTarget> results);
}
