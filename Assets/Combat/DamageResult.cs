using System;

public enum DamageStatus { InvalidRequest, Applied, NotAlive, NonHostile, Protected }

/// <summary>적용과 거부를 구분한다. 적용량은 초과 피해를 제외한 실제 체력 감소량이다.</summary>
public readonly struct DamageResult
{
    public DamageStatus Status { get; }
    public bool WasApplied => Status == DamageStatus.Applied;
    public float AppliedDamage { get; }
    public bool WasKilled { get; }

    private DamageResult(DamageStatus status, float amount, bool killed)
    { Status = status; AppliedDamage = amount; WasKilled = killed; }

    public static DamageResult Applied(float amount, bool wasKilled = false)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (wasKilled && amount == 0f)
            throw new ArgumentException("A zero-damage application cannot kill.", nameof(wasKilled));
        return new DamageResult(DamageStatus.Applied, amount, wasKilled);
    }

    public static DamageResult Rejected(DamageStatus reason)
    {
        if (!Enum.IsDefined(typeof(DamageStatus), reason) || reason == DamageStatus.Applied)
            throw new ArgumentOutOfRangeException(nameof(reason));
        return new DamageResult(reason, 0f, false);
    }
}
