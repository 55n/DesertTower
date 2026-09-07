using System;

/// <summary>시설을 수리·강화·철거하고, 필요한 비용이나 환급액을 알려 준다.</summary>
/// <remarks>
/// 시설 메뉴에서 쓴다. 메뉴가 직접 체력이나 마나를 바꾸지 않고 이 기능에 맡긴다.
/// 표시된 금액은 실행할 때 다시 확인한다. 철거할 때는 시설이 차지했던 자리도 함께 비운다.
/// </remarks>
public interface IFacilityMaintenance
{
    /// <summary>화면에 보여 줄 수리 가능 여부와 비용을 구한다. 실행할 때는 현재 단계·체력·비용을 다시 확인한다.</summary>
    MaintenanceQuote GetRepairQuote(Guid facilityId);
    /// <summary>화면에 보여 줄 철거 가능 여부와 환급액을 구한다. 실행할 때는 현재 단계·체력·금액을 다시 확인한다.</summary>
    MaintenanceQuote GetDemolitionQuote(Guid facilityId);
    /// <summary>다음 강화의 가능 여부와 마나 비용을 구한다. 최대 단계면 UpgradeLimitReached로 알려 준다.</summary>
    FacilityUpgradeQuote GetUpgradeQuote(Guid facilityId);
    /// <summary>마나를 써서 시설을 한 단계 강화한다.</summary>
    /// <remarks>현재 단계·게임 상태·비용을 다시 확인한다. 실패하면 마나와 시설을 그대로 두고 알리지 않는다. 성공하면 비용과 강화 효과를 함께 반영한 뒤 알린다.</remarks>
    ActionResult TryUpgrade(Guid facilityId);
    /// <summary>마나를 써서 시설을 수리한다.</summary>
    /// <remarks>
    /// 필요한 마나를 잡아 둔 뒤 체력과 마나를 함께 반영하고 알린다.
    /// 실패하면 체력과 마나를 바꾸지 않고 변경 알림도 만들지 않는다.
    /// </remarks>
    ActionResult TryRepair(Guid facilityId);
    /// <summary>시설을 철거하고 환급액을 한 번만 지급한다.</summary>
    /// <remarks>
    /// 시설·차지한 자리·등록 정보를 함께 정리한다. 실패하면 환급하거나 일부만 정리하거나 알리지 않는다.
    /// </remarks>
    ActionResult TryDemolish(Guid facilityId);
}
