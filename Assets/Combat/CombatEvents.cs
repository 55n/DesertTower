using System;

public enum LifeState { Alive, Dying, Incapacitated, Removed }
public enum CombatTargetKind { Player, Enemy, Wall, Tower, Minion, Core }

public readonly struct HealthChangedInfo
{
    public Guid EntityId { get; }
    public float PreviousHealth { get; }
    public float CurrentHealth { get; }
    public float PreviousMaxHealth { get; }
    public float MaxHealth { get; }
    public HealthChangedInfo(Guid entityId, float previousHealth, float currentHealth,
        float previousMaxHealth, float maxHealth)
    {
        EntityId = entityId; PreviousHealth = previousHealth; CurrentHealth = currentHealth;
        PreviousMaxHealth = previousMaxHealth; MaxHealth = maxHealth;
    }
}

public readonly struct DamageAppliedInfo
{
    public Guid EntityId { get; }
    public DamageInfo Damage { get; }
    public DamageResult Result { get; }
    public DamageAppliedInfo(Guid entityId, DamageInfo damage, DamageResult result)
    {
        if (!damage.IsValid || !result.WasApplied)
            throw new ArgumentException("Damage events require a valid applied request.");
        EntityId = entityId; Damage = damage; Result = result;
    }
}

public readonly struct LifeStateChangedInfo
{
    public Guid EntityId { get; }
    public LifeState PreviousState { get; }
    public LifeState CurrentState { get; }
    public LifeStateChangedInfo(Guid entityId, LifeState previousState, LifeState currentState)
    { EntityId = entityId; PreviousState = previousState; CurrentState = currentState; }
}

public readonly struct DeathInfo
{
    public Guid EntityId { get; }
    public string DefinitionId { get; }
    public string FactionId { get; }
    // Increment on each revival. Rewards can deduplicate by (EntityId, LifeSequence).
    public int LifeSequence { get; }
    public DamageInfo KillingDamage { get; }
    public DeathInfo(Guid entityId, string definitionId, string factionId,
        int lifeSequence, DamageInfo killingDamage)
    {
        EntityId = entityId; DefinitionId = definitionId; FactionId = factionId;
        LifeSequence = lifeSequence; KillingDamage = killingDamage;
    }
}
