using System;
using UnityEngine;

/// <summary>선택한 대상과 그 대상을 공격하러 갈 위치를 함께 전달한다.</summary>
public readonly struct TargetSelection
{
    /// <summary>공격할 대상이다. 선택하지 않았으면 null이다.</summary>
    public ICombatTarget Target { get; }
    /// <summary>길을 확인해서 찾은 접근 위치다. 없으면 이동 코드에서 따로 구한다.</summary>
    public Vector3? ApproachPosition { get; }
    /// <summary>접근 위치를 구할 때의 길 정보 번호다. 길이 바뀌면 다시 확인한다.</summary>
    public long? NavigationRevision { get; }
    public bool HasTarget => Target != null;

    private TargetSelection(ICombatTarget target, Vector3? position, long? revision)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ApproachPosition = position; NavigationRevision = revision;
    }
    /// <summary>일반 공격 대상을 선택한다. 접근 위치는 아직 정하지 않는다.</summary>
    public static TargetSelection ForTarget(ICombatTarget target)
        => new TargetSelection(target, null, null);
    /// <summary>길을 막은 시설과 확인된 접근 위치를 그대로 보관한다.</summary>
    public static TargetSelection FromObstruction(ObstructionResult obstruction)
    {
        if (obstruction.Blocker == null) throw new ArgumentException("차단 시설이 필요합니다.", nameof(obstruction));
        return new TargetSelection(obstruction.Blocker, obstruction.ApproachPosition, obstruction.NavigationRevision);
    }
    /// <summary>저장한 접근 위치가 현재 길 정보를 기준으로 계산됐는지 확인한다.</summary>
    public bool HasCurrentApproach(long navigationRevision)
        => HasTarget && ApproachPosition.HasValue && NavigationRevision == navigationRevision;
}
