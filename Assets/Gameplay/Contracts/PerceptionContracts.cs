using System;
using UnityEngine;

// Default results are Invalid, never a completed positive or negative judgment.
public enum VisibilityState { Invalid = 0, Visible = 1, Blocked = 2 }
public enum ReachabilityState { Invalid = 0, Reachable = 1, Unreachable = 2, Partial = 3 }

public readonly struct VisibilityRequest
{
    public Vector3 Origin { get; }
    public Vector3 Destination { get; }
    public int ObstacleLayerMask { get; }
    public bool IncludeTriggers { get; }
    public Guid ObserverId { get; }
    public Guid TargetId { get; }
    public long WorldRevision { get; }
    public VisibilityRequest(Vector3 origin, Vector3 destination, int obstacleLayerMask,
        bool includeTriggers, Guid observerId, Guid targetId, long worldRevision)
    {
        Origin = origin; Destination = destination; ObstacleLayerMask = obstacleLayerMask;
        IncludeTriggers = includeTriggers; ObserverId = observerId; TargetId = targetId;
        WorldRevision = worldRevision;
    }
}

public readonly struct VisibilityResult
{
    public VisibilityState State { get; }
    // Revision actually used for the judgment, including Invalid results.
    public long WorldRevision { get; }
    // Independent of Visible/Blocked. Always false in stage 1; never permits a hit.
    public bool IsStale { get; }
    public VisibilityResult(VisibilityState state, long worldRevision, bool isStale = false)
    {
        if (!Enum.IsDefined(typeof(VisibilityState), state))
            throw new ArgumentOutOfRangeException(nameof(state));
        if (worldRevision < 0) throw new ArgumentOutOfRangeException(nameof(worldRevision));
        State = state; WorldRevision = worldRevision; IsStale = isStale;
    }
}

public readonly struct ReachabilityRequest
{
    public Vector3 Origin { get; }
    public Vector3 Destination { get; }
    public int AgentTypeId { get; }
    public int AreaMask { get; }
    public long NavigationRevision { get; }
    public ReachabilityRequest(Vector3 origin, Vector3 destination, int agentTypeId,
        int areaMask, long navigationRevision)
    {
        Origin = origin; Destination = destination; AgentTypeId = agentTypeId;
        AreaMask = areaMask; NavigationRevision = navigationRevision;
    }
}

public readonly struct ReachabilityResult
{
    public ReachabilityState State { get; }
    // Reachable: destination. Partial: farthest reachable point (possibly origin).
    // Unreachable: finite origin. Invalid: zero; never use it as a movement target.
    public Vector3 EndPosition { get; }
    public long NavigationRevision { get; }
    // Freshness is not reachability. Always false in stage 1.
    public bool IsStale { get; }
    public ReachabilityResult(ReachabilityState state, Vector3 endPosition,
        long navigationRevision, bool isStale = false)
    {
        if (!Enum.IsDefined(typeof(ReachabilityState), state))
            throw new ArgumentOutOfRangeException(nameof(state));
        if (!PerceptionContractValidation.IsFinite(endPosition))
            throw new ArgumentOutOfRangeException(nameof(endPosition));
        if (navigationRevision < 0) throw new ArgumentOutOfRangeException(nameof(navigationRevision));
        State = state;
        EndPosition = state == ReachabilityState.Invalid ? Vector3.zero : endPosition;
        NavigationRevision = navigationRevision; IsStale = isStale;
    }
}

internal static class PerceptionContractValidation
{
    internal static bool IsFinite(Vector3 value)
        => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
