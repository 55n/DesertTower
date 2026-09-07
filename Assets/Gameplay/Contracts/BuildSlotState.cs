using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>슬롯의 위치, 허용 시설, 점유 상태를 복사한 값이다.</summary>
public readonly struct BuildSlotState
{
    public string SlotId { get; }
    public string OccupancySurfaceId { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector2 Footprint { get; }
    public bool IsAvailable { get; }
    public Guid? OccupantId { get; }
    private readonly IReadOnlyList<string> allowedFacilityIds;
    public IReadOnlyList<string> AllowedFacilityIds => allowedFacilityIds ?? Array.Empty<string>();

    public BuildSlotState(string slotId, string occupancySurfaceId, Vector3 position, Quaternion rotation,
        Vector2 footprint, bool isAvailable, Guid? occupantId, IEnumerable<string> allowedFacilityIds)
    {
        if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("슬롯 ID가 필요합니다.", nameof(slotId));
        if (string.IsNullOrWhiteSpace(occupancySurfaceId)) throw new ArgumentException("바닥 ID가 필요합니다.", nameof(occupancySurfaceId));
        if (!new PlacementRequest("slot", position, rotation).IsValid) throw new ArgumentException("잘못된 슬롯 위치 또는 회전입니다.");
        if (!PerceptionContractValidation.IsFinite(footprint.x) || !PerceptionContractValidation.IsFinite(footprint.y)
            || footprint.x <= 0 || footprint.y <= 0) throw new ArgumentOutOfRangeException(nameof(footprint));
        if (occupantId == Guid.Empty) throw new ArgumentException("빈 시설 ID는 사용할 수 없습니다.", nameof(occupantId));
        if (allowedFacilityIds == null) throw new ArgumentNullException(nameof(allowedFacilityIds));
        var copy = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in allowedFacilityIds)
        {
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) throw new ArgumentException("허용 시설 ID를 확인하세요.", nameof(allowedFacilityIds));
            copy.Add(id);
        }
        SlotId = slotId; OccupancySurfaceId = occupancySurfaceId; Position = position; Rotation = rotation;
        Footprint = footprint; IsAvailable = isAvailable; OccupantId = occupantId;
        this.allowedFacilityIds = copy.AsReadOnly();
    }
    /// <summary>이 슬롯에 지정한 종류의 시설을 지을 수 있는지 확인한다. 목록이 비었으면 허용하지 않는다.</summary>
    public bool AllowsFacility(string facilityId)
    {
        foreach (var id in AllowedFacilityIds) if (id == facilityId) return true;
        return false;
    }
}
