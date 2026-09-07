using System;
using UnityEngine;

/// <summary>피해량과 가해 진영은 필수. 발사 후 공격자가 제거되어도 진영 정보는 유지한다.</summary>
public readonly struct DamageInfo
{
    public float Amount { get; }
    public string SourceFactionId { get; }
    public Guid? SourceEntityId { get; }
    public string CauseId { get; }
    public Vector3? HitPosition { get; }
    public Vector3? HitDirection { get; }
    // default(struct) is invalid and must be rejected by the receiver.
    public bool IsValid => !string.IsNullOrWhiteSpace(SourceFactionId)
        && !float.IsNaN(Amount) && !float.IsInfinity(Amount) && Amount >= 0f;

    public DamageInfo(float amount, string sourceFactionId, Guid? sourceEntityId = null,
        string causeId = null, Vector3? hitPosition = null, Vector3? hitDirection = null)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (string.IsNullOrWhiteSpace(sourceFactionId))
            throw new ArgumentException("A source faction is required.", nameof(sourceFactionId));
        Amount = amount;
        SourceFactionId = sourceFactionId;
        SourceEntityId = sourceEntityId;
        CauseId = causeId;
        HitPosition = hitPosition;
        HitDirection = hitDirection;
    }
}
