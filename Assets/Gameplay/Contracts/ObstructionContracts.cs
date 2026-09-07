using System;
using UnityEngine;

/// <summary>원래 진격 경로, 부분 경로 끝점, 공격자의 진영과 이동·공격 조건 스냅샷.</summary>
public readonly struct ObstructionRequest
{
    // Origin is the mover's current position; Destination is the original goal,
    // not the blocker center. Also carries agent type, area mask and nav revision.
    public ReachabilityRequest Route { get; }
    public Vector3 PathEndPosition { get; }
    public string FactionId { get; }
    public float SearchRadius { get; }
    public float AttackRange { get; }
    // Checks data shape only; the service validates the actual agent type/revision.
    public bool IsValid => !string.IsNullOrWhiteSpace(FactionId)
        && PerceptionContractValidation.IsFinite(Route.Origin)
        && PerceptionContractValidation.IsFinite(Route.Destination)
        && PerceptionContractValidation.IsFinite(PathEndPosition)
        && Route.AreaMask != 0 && Route.NavigationRevision >= 0
        && PerceptionContractValidation.IsFinite(SearchRadius) && SearchRadius > 0f
        && PerceptionContractValidation.IsFinite(AttackRange) && AttackRange >= 0f;

    public ObstructionRequest(ReachabilityRequest route, Vector3 pathEndPosition,
        string factionId, float searchRadius, float attackRange)
    {
        Route = route; PathEndPosition = pathEndPosition; FactionId = factionId;
        SearchRadius = searchRadius; AttackRange = attackRange;
    }
}

/// <summary>선택 가능한 차단물과 같은 접근 가능 영역의 공격 위치. default는 결과 없음이다.</summary>
public readonly struct ObstructionResult
{
    public ICombatTarget Blocker { get; }
    public Vector3 ApproachPosition { get; }
    public long NavigationRevision { get; }
    public ObstructionResult(ICombatTarget blocker, Vector3 approachPosition, long navigationRevision)
    {
        if (blocker == null) throw new ArgumentNullException(nameof(blocker));
        if (!PerceptionContractValidation.IsFinite(approachPosition))
            throw new ArgumentOutOfRangeException(nameof(approachPosition));
        if (navigationRevision < 0) throw new ArgumentOutOfRangeException(nameof(navigationRevision));
        Blocker = blocker; ApproachPosition = approachPosition; NavigationRevision = navigationRevision;
    }
}
