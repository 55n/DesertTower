using System;

public enum ActionFailure
{
    None, InvalidRequest, NotFound, WrongPhase, Paused, NotAlive,
    InsufficientMana, Locked, Cooldown, InvalidPlacement, NoChange, CreationFailed,
    InsufficientSkillPoints, PrerequisiteNotMet, AlreadyLearned, UpgradeLimitReached
}

/// <summary>default는 실패다. 실패 이유의 None은 성공에서만 사용한다.</summary>
public readonly struct ActionResult
{
    public bool Succeeded { get; }
    private readonly ActionFailure failure;
    public ActionFailure Failure => !Succeeded && failure == ActionFailure.None
        ? ActionFailure.InvalidRequest : failure;
    private ActionResult(bool succeeded, ActionFailure reason)
    { Succeeded = succeeded; failure = reason; }
    public static ActionResult Success() => new ActionResult(true, ActionFailure.None);
    public static ActionResult Fail(ActionFailure reason)
    {
        if (reason == ActionFailure.None || !Enum.IsDefined(typeof(ActionFailure), reason))
            throw new ArgumentOutOfRangeException(nameof(reason));
        return new ActionResult(false, reason);
    }
}
