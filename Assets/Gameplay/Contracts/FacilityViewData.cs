using System;

/// <summary>시설 메뉴에 보여 줄 값이다. 수리·강화·철거는 IFacilityMaintenance에 요청한다.</summary>
public readonly struct FacilityViewData
{
    public Guid EntityId { get; }
    public string DefinitionId { get; }
    public float CurrentHealth { get; }
    public float MaxHealth { get; }
    public int UpgradeLevel { get; }
    public int MaxUpgradeLevel { get; }
    public PlacementKind PlacementKind { get; }
    public string BuildSlotId { get; }
    public bool IsValid => EntityId != Guid.Empty && !string.IsNullOrWhiteSpace(DefinitionId);

    public FacilityViewData(Guid entityId, string definitionId, float currentHealth, float maxHealth,
        int upgradeLevel, int maxUpgradeLevel, PlacementKind placementKind, string buildSlotId = null)
    {
        if (entityId == Guid.Empty) throw new ArgumentException("시설 ID가 필요합니다.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("시설 종류가 필요합니다.", nameof(definitionId));
        if (!PerceptionContractValidation.IsFinite(maxHealth) || maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth));
        if (!PerceptionContractValidation.IsFinite(currentHealth) || currentHealth < 0 || currentHealth > maxHealth)
            throw new ArgumentOutOfRangeException(nameof(currentHealth));
        if (upgradeLevel < 0 || maxUpgradeLevel < upgradeLevel) throw new ArgumentOutOfRangeException(nameof(upgradeLevel));
        if (placementKind != PlacementKind.DesignatedSlot && placementKind != PlacementKind.Free)
            throw new ArgumentOutOfRangeException(nameof(placementKind));
        if (placementKind == PlacementKind.DesignatedSlot && string.IsNullOrWhiteSpace(buildSlotId))
            throw new ArgumentException("슬롯 ID가 필요합니다.", nameof(buildSlotId));
        if (placementKind == PlacementKind.Free && buildSlotId != null)
            throw new ArgumentException("자유 배치는 슬롯 ID를 쓰지 않습니다.", nameof(buildSlotId));
        EntityId = entityId; DefinitionId = definitionId; CurrentHealth = currentHealth; MaxHealth = maxHealth;
        UpgradeLevel = upgradeLevel; MaxUpgradeLevel = maxUpgradeLevel;
        PlacementKind = placementKind; BuildSlotId = buildSlotId;
    }
}
