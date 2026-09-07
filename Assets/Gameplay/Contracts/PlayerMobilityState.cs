using System;

/// <summary>점프와 대시 화면에 필요한 값이다. 비용과 횟수는 게임 설정에서 받는다.</summary>
public readonly struct PlayerMobilityState
{
    public bool IsGrounded { get; }
    public int RemainingAirJumps { get; }
    public int DashManaCost { get; }
    public float DashCooldownRemaining { get; }
    public float DashCooldownDuration { get; }
    public ActionResult JumpAvailability { get; }
    public ActionResult DashAvailability { get; }

    public PlayerMobilityState(bool isGrounded, int remainingAirJumps, int dashManaCost,
        float dashCooldownRemaining, float dashCooldownDuration,
        ActionResult jumpAvailability, ActionResult dashAvailability)
    {
        if (remainingAirJumps < 0) throw new ArgumentOutOfRangeException(nameof(remainingAirJumps));
        if (dashManaCost < 0) throw new ArgumentOutOfRangeException(nameof(dashManaCost));
        if (!PerceptionContractValidation.IsFinite(dashCooldownDuration) || dashCooldownDuration < 0)
            throw new ArgumentOutOfRangeException(nameof(dashCooldownDuration));
        if (!PerceptionContractValidation.IsFinite(dashCooldownRemaining)
            || dashCooldownRemaining < 0 || dashCooldownRemaining > dashCooldownDuration)
            throw new ArgumentOutOfRangeException(nameof(dashCooldownRemaining));
        IsGrounded = isGrounded; RemainingAirJumps = remainingAirJumps; DashManaCost = dashManaCost;
        DashCooldownRemaining = dashCooldownRemaining; DashCooldownDuration = dashCooldownDuration;
        JumpAvailability = jumpAvailability; DashAvailability = dashAvailability;
    }
}
