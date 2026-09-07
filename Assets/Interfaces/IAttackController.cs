/// <summary>정해진 대상을 공격하거나 공격을 멈춘다.</summary>
/// <remarks>
/// 적·타워·소환수가 공격할 때 쓴다. 누구를 공격할지는 다른 코드가 정한다.
/// 실제로 맞히는 순간에도 거리와 장애물을 다시 확인한다. 이미 날아간 투사체는 취소하지 않는다.
/// </remarks>
public interface IAttackController
{
    /// <summary>지금 공격 동작을 진행 중인지 알려 준다.</summary>
    bool IsAttacking { get; }
    /// <summary>다음 공격까지 남은 시간을 알려 준다.</summary>
    float CooldownRemaining { get; }
    /// <summary>대상에게 공격을 시도한다.</summary>
    /// <remarks>
    /// true는 공격 동작을 시작했다는 뜻이다. 아직 맞히기 전일 수 있다. false면 새 공격을 시작하지 않는다.
    /// 공격이 맞는 순간에 대상, 거리, 현재 시야를 다시 확인한다.
    /// 잘못됐거나 오래된 시야 결과로 명중을 인정하지 않는다. 이미 발사한 투사체는 따로 움직인다.
    /// </remarks>
    bool TryAttack(ICombatTarget target);
    /// <summary>아직 끝나지 않은 공격을 취소한다. 이미 발사한 투사체는 없애지 않는다.</summary>
    void Cancel();
}
