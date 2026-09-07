using UnityEngine;

public enum GamePhase { Preparation, Combat, Victory, Defeat }
// Preserve completed-state values; 1 was the removed waiting state.
public enum MovementPathState { None = 0, Complete = 2, Partial = 3, Invalid = 4 }

public readonly struct ManaChangedInfo
{
    public int PreviousMana { get; }
    public int CurrentMana { get; }
    public int MaxMana { get; }
    public ManaChangedInfo(int previousMana, int currentMana, int maxMana)
    { PreviousMana = previousMana; CurrentMana = currentMana; MaxMana = maxMana; }
}

public readonly struct SpellCastRequest
{
    public string SpellId { get; }
    public Vector3 AimPosition { get; }
    public SpellCastRequest(string spellId, Vector3 aimPosition)
    { SpellId = spellId; AimPosition = aimPosition; }
}

public readonly struct SpellState
{
    public string SpellId { get; }
    public int ManaCost { get; }
    public float CooldownRemaining { get; }
    public float CooldownDuration { get; }
    public SpellState(string spellId, int manaCost, float cooldownRemaining, float cooldownDuration)
    {
        SpellId = spellId; ManaCost = manaCost;
        CooldownRemaining = cooldownRemaining; CooldownDuration = cooldownDuration;
    }
}
