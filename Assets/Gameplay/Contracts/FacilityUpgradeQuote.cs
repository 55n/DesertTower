using System;

/// <summary>다음 시설 강화에 필요한 비용과 현재 단계다. 표시 후 실행할 때 다시 확인한다.</summary>
public readonly struct FacilityUpgradeQuote
{
    public ActionResult Availability { get; }
    public int ManaAmount { get; }
    public int CurrentLevel { get; }
    public int MaxLevel { get; }
    /// <summary>더 강화할 수 없으면 다음 단계는 없다.</summary>
    public int? NextLevel => CurrentLevel < MaxLevel ? CurrentLevel + 1 : (int?)null;
    public FacilityUpgradeQuote(ActionResult availability, int manaAmount, int currentLevel, int maxLevel)
    {
        if (manaAmount < 0) throw new ArgumentOutOfRangeException(nameof(manaAmount));
        if (currentLevel < 0 || maxLevel < currentLevel) throw new ArgumentOutOfRangeException(nameof(currentLevel));
        if (availability.Succeeded && currentLevel == maxLevel)
            throw new ArgumentException("최대 단계에서는 강화할 수 없습니다.", nameof(availability));
        Availability = availability; ManaAmount = manaAmount; CurrentLevel = currentLevel; MaxLevel = maxLevel;
    }
}
