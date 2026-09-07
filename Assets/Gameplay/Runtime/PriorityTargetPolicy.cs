using System;
using System.Collections.Generic;

/// <summary>현재 목표를 계속 상대할 수 있으면 유지한다. 아니면 우선순위, 거리, ID 순으로 고른다.</summary>
public sealed class PriorityTargetPolicy
{
    /// <summary>후보 중 하나의 ID를 고른다. 공격 가능한 후보가 없으면 null이다.</summary>
    public Guid? Select(Guid? currentTargetId, IReadOnlyList<TargetCandidate> candidates)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        TargetCandidate? best = null;
        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (!candidate.IsEligible || candidate.EntityId == Guid.Empty) continue;
            if (candidate.EntityId == currentTargetId && candidate.CanRetain)
                return candidate.EntityId;
            if (!best.HasValue || candidate.Priority < best.Value.Priority
                || (candidate.Priority == best.Value.Priority
                    && (candidate.DistanceSquared < best.Value.DistanceSquared
                        || (candidate.DistanceSquared == best.Value.DistanceSquared
                            && candidate.EntityId.CompareTo(best.Value.EntityId) < 0))))
                best = candidate;
        }
        return best?.EntityId;
    }
}
