/// <summary>세션 시작 시 선택한다. 기본값은 도착 즉시 흡수다.</summary>
public enum CoreInteractionMode { AbsorbOnArrival = 0, AttackUntilDestroyed = 1 }
public enum CoreArrivalDisposition { None = 0, Absorbed = 1, AttackRequired = 2 }

/// <summary>흡수 완료와 공격 전환 지시를 구분한다. default는 아무 행동도 승인하지 않는다.</summary>
public readonly struct CoreArrivalResult
{
    public ActionResult Outcome { get; }
    public CoreArrivalDisposition Disposition { get; }
    private CoreArrivalResult(ActionResult outcome, CoreArrivalDisposition disposition)
    { Outcome = outcome; Disposition = disposition; }
    public static CoreArrivalResult Absorbed()
        => new CoreArrivalResult(ActionResult.Success(), CoreArrivalDisposition.Absorbed);
    // Only a directive. Does not inflict damage, remove an enemy or resolve a wave ID.
    public static CoreArrivalResult AttackRequired()
        => new CoreArrivalResult(ActionResult.Success(), CoreArrivalDisposition.AttackRequired);
    public static CoreArrivalResult Failed(ActionFailure failure)
        => new CoreArrivalResult(ActionResult.Fail(failure), CoreArrivalDisposition.None);
}
