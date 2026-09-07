using System;
using System.Collections.Generic;

/// <summary>스킬 선택 UI용 불변 스냅샷. 실제 습득 명령은 최신 조건을 다시 검사한다.</summary>
public readonly struct SkillNodeState
{
    public string NodeId { get; }
    public int PointCost { get; }
    public bool IsLearned { get; }
    public ActionResult LearnAvailability { get; }
    private readonly IReadOnlyList<string> prerequisiteNodeIds;
    // All listed prerequisite nodes must be learned. Empty means a root node.
    public IReadOnlyList<string> PrerequisiteNodeIds => prerequisiteNodeIds ?? Array.Empty<string>();

    public SkillNodeState(string nodeId, int pointCost, bool isLearned,
        IEnumerable<string> prerequisiteNodeIds, ActionResult learnAvailability)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("A node ID is required.", nameof(nodeId));
        if (pointCost < 0) throw new ArgumentOutOfRangeException(nameof(pointCost));
        if (prerequisiteNodeIds == null) throw new ArgumentNullException(nameof(prerequisiteNodeIds));
        var copy = new List<string>();
        foreach (var prerequisite in prerequisiteNodeIds)
        {
            if (string.IsNullOrWhiteSpace(prerequisite) || prerequisite == nodeId || copy.Contains(prerequisite))
                throw new ArgumentException("Prerequisites must be distinct, nonempty other node IDs.", nameof(prerequisiteNodeIds));
            copy.Add(prerequisite);
        }
        NodeId = nodeId; PointCost = pointCost; IsLearned = isLearned;
        this.prerequisiteNodeIds = copy.AsReadOnly();
        LearnAvailability = isLearned ? ActionResult.Fail(ActionFailure.AlreadyLearned) : learnAvailability;
    }
}
