using System;
using UnityEngine;

public enum PlacementKind { Invalid = 0, DesignatedSlot = 1, Free = 2 }

[Flags]
public enum PlacementModes { None = 0, DesignatedSlot = 1, Free = 2 }

public readonly struct PlacementRequest
{
    public string FacilityId { get; }
    public PlacementKind Kind { get; }
    // 슬롯 요청은 ID만 보낸다. 실제 건설할 때 현재 위치를 조회한다.
    public string SlotId { get; }
    // 자유 배치에서만 쓰는 위치와 회전이다. 게임 단계는 실행 코드에서 확인한다.
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }

    public bool IsValid => !string.IsNullOrWhiteSpace(FacilityId)
        && ((Kind == PlacementKind.DesignatedSlot && !string.IsNullOrWhiteSpace(SlotId))
            || (Kind == PlacementKind.Free && IsFinite(Position.x) && IsFinite(Position.y)
                && IsFinite(Position.z) && IsFinite(Rotation.x) && IsFinite(Rotation.y)
                && IsFinite(Rotation.z) && IsFinite(Rotation.w)
                && Math.Abs((double)Rotation.x * Rotation.x + (double)Rotation.y * Rotation.y
                    + (double)Rotation.z * Rotation.z + (double)Rotation.w * Rotation.w - 1d) < 0.001d));

    // 원하는 월드 위치에 자유 배치를 요청한다.
    public PlacementRequest(string facilityId, Vector3 position, Quaternion rotation)
    {
        FacilityId = facilityId; Kind = PlacementKind.Free; SlotId = null;
        Position = position; Rotation = rotation;
    }

    private PlacementRequest(string facilityId, string slotId)
    {
        FacilityId = facilityId; Kind = PlacementKind.DesignatedSlot; SlotId = slotId;
        Position = Vector3.zero; Rotation = Quaternion.identity;
    }

    public static PlacementRequest AtSlot(string facilityId, string slotId)
        => new PlacementRequest(facilityId, slotId);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

public enum PlacementFailure
{
    // 부술 수 있는 시설로 적의 길을 막는 것은 허용한다.
    // 적은 길을 막은 시설을 공격한다. 길을 막았다는 이유만으로 건설을 거절하지 않는다.
    None, InvalidRequest, OutsideBuildZone, Occupied, RestrictedArea,
    CharacterOverlap, UnevenGround, InsufficientMana, Locked, WrongPhase, Paused,
    PlacementModeNotAllowed, SlotNotFound, SlotUnavailable, FacilityNotAllowed
}

public readonly struct PlacementResult
{
    public bool CanPlace { get; }
    private readonly PlacementFailure failure;
    public PlacementFailure Failure => !CanPlace && failure == PlacementFailure.None
        ? PlacementFailure.InvalidRequest : failure;
    private PlacementResult(bool allowed, PlacementFailure reason)
    { CanPlace = allowed; failure = reason; }
    public static PlacementResult Allowed() => new PlacementResult(true, PlacementFailure.None);
    public static PlacementResult Denied(PlacementFailure reason)
    {
        if (reason == PlacementFailure.None || !Enum.IsDefined(typeof(PlacementFailure), reason))
            throw new ArgumentOutOfRangeException(nameof(reason));
        return new PlacementResult(false, reason);
    }
}

public readonly struct BuildResult
{
    public ActionResult Outcome { get; }
    public PlacementResult Placement { get; }
    /// <summary>성공한 건설의 표시 정보다. 실패하면 null이다.</summary>
    public FacilityViewData? Facility { get; }
    private BuildResult(ActionResult outcome, PlacementResult placement, FacilityViewData? facility)
    { Outcome = outcome; Placement = placement; Facility = facility; }
    public static BuildResult Built(FacilityViewData facility)
    {
        if (!facility.IsValid) throw new ArgumentException("시설 표시 정보가 필요합니다.", nameof(facility));
        return new BuildResult(ActionResult.Success(), PlacementResult.Allowed(), facility);
    }
    public static BuildResult Failed(ActionFailure failure, PlacementResult placement)
        => new BuildResult(ActionResult.Fail(failure), placement, null);
}

public readonly struct MaintenanceQuote
{
    public ActionResult Availability { get; }
    // 수리 비용 또는 철거 환급액이다. 음수는 쓰지 않는다.
    public int ManaAmount { get; }
    public float HealthToRestore { get; }
    public MaintenanceQuote(ActionResult availability, int manaAmount, float healthToRestore = 0f)
    {
        if (manaAmount < 0) throw new ArgumentOutOfRangeException(nameof(manaAmount));
        if (float.IsNaN(healthToRestore) || float.IsInfinity(healthToRestore) || healthToRestore < 0f)
            throw new ArgumentOutOfRangeException(nameof(healthToRestore));
        Availability = availability; ManaAmount = manaAmount; HealthToRestore = healthToRestore;
    }
}
