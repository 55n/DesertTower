using System;
using System.Collections.Generic;
using DesertTower.Levels;

/// <summary>레벨 슬롯을 읽고 어느 시설이 자리를 차지했는지 관리한다. Unity 주 스레드에서 쓴다.</summary>
/// <remarks>마나와 실제 시설 생성은 건설 서비스가 처리한다. 생성 실패 시 Release로 잡아 둔 자리를 돌려놓는다.</remarks>
public sealed class BuildSlotRegistry : IBuildSlotQuery
{
    private readonly LevelRoot root;
    private readonly Dictionary<string, Guid> occupants = new Dictionary<string, Guid>(StringComparer.Ordinal);
    public BuildSlotRegistry(LevelRoot root)
    { this.root = root ? root : throw new ArgumentNullException(nameof(root)); }

    public IReadOnlyList<BuildSlotState> GetSnapshot()
    {
        var result = new List<BuildSlotState>();
        if (root)
            foreach (var source in root.BuildSlots)
                if (TryGetSlot(source.id, out var slot)) result.Add(slot);
        return result.AsReadOnly();
    }
    public bool TryGetSlot(string slotId, out BuildSlotState slot)
    {
        slot = default;
        if (!root || string.IsNullOrWhiteSpace(slotId)) return false;
        LevelBuildSlot found = null;
        foreach (var source in root.BuildSlots)
        {
            if (source.id != slotId) continue;
            if (found) return false;
            found = source;
        }
        if (!found || !found.TryValidate(out _)) return false;
        Guid? owner = occupants.TryGetValue(slotId, out var id) ? id : (Guid?)null;
        slot = new BuildSlotState(found.id, found.occupancySurfaceId, found.transform.position,
            found.transform.rotation, found.footprint, found.available && found.isActiveAndEnabled,
            owner, found.allowedFacilityIds);
        return true;
    }
    /// <summary>빈 슬롯을 시설 하나의 자리로 잡는다. 실패하면 점유 정보를 바꾸지 않는다.</summary>
    public bool TryOccupy(string slotId, string facilityDefinitionId, Guid facilityId)
    {
        if (facilityId == Guid.Empty || occupants.ContainsValue(facilityId)
            || !TryGetSlot(slotId, out var slot) || !slot.IsAvailable
            || slot.OccupantId.HasValue || !slot.AllowsFacility(facilityDefinitionId)) return false;
        occupants.Add(slotId, facilityId);
        return true;
    }
    /// <summary>이 시설이 잡은 자리만 비운다. 다른 시설의 자리나 이미 빈 자리는 바꾸지 않는다.</summary>
    public bool Release(string slotId, Guid facilityId)
    {
        if (string.IsNullOrWhiteSpace(slotId) || !occupants.TryGetValue(slotId, out var owner) || owner != facilityId) return false;
        return occupants.Remove(slotId);
    }
}
