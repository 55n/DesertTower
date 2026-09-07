using System;

/// <summary>선택 시점 스냅샷. 유효성/거리/시야/접근/추적 금지는 selector가 계산한다.</summary>
public readonly struct TargetCandidate
{
    public Guid EntityId { get; }
    public bool IsEligible { get; }
    public bool CanRetain { get; }
    // Lower values win AFTER valid retention (GDD 4.3 step 1).
    // Enemy: blocker 0 (step 2), player/minion 1, tower 2, core 3.
    // A blocker needs a reachable attack position; proximity is not eligibility.
    public int Priority { get; }
    public float DistanceSquared { get; }

    public TargetCandidate(Guid entityId, bool isEligible, bool canRetain,
        int priority, float distanceSquared)
    {
        if (entityId == Guid.Empty) throw new ArgumentException("Entity ID is required.", nameof(entityId));
        if (float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared) || distanceSquared < 0f)
            throw new ArgumentOutOfRangeException(nameof(distanceSquared));
        EntityId = entityId; IsEligible = isEligible; CanRetain = canRetain;
        Priority = priority; DistanceSquared = distanceSquared;
    }
}
