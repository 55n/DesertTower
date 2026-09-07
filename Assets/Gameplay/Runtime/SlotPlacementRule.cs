using System;

/// <summary>지정 슬롯이 사용 가능하고, 비어 있고, 요청한 시설을 허용하는지 확인한다.</summary>
/// <remarks>단계·마나·실제 충돌은 다른 규칙에서 확인한다. 자유 배치 요청은 이 검사를 통과한다.</remarks>
public sealed class SlotPlacementRule : IPlacementRule
{
    private readonly IBuildSlotQuery slots;
    public SlotPlacementRule(IBuildSlotQuery slots)
    { this.slots = slots ?? throw new ArgumentNullException(nameof(slots)); }
    public PlacementResult Evaluate(PlacementRequest request)
    {
        if (!request.IsValid) return PlacementResult.Denied(PlacementFailure.InvalidRequest);
        if (request.Kind == PlacementKind.Free) return PlacementResult.Allowed();
        if (!slots.TryGetSlot(request.SlotId, out var slot)) return PlacementResult.Denied(PlacementFailure.SlotNotFound);
        if (!slot.IsAvailable) return PlacementResult.Denied(PlacementFailure.SlotUnavailable);
        if (slot.OccupantId.HasValue) return PlacementResult.Denied(PlacementFailure.Occupied);
        if (!slot.AllowsFacility(request.FacilityId)) return PlacementResult.Denied(PlacementFailure.FacilityNotAllowed);
        return PlacementResult.Allowed();
    }
}
