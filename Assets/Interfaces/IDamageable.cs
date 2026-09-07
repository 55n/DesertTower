/// <summary>대상에게 피해를 주고, 실제로 얼마나 적용됐는지 돌려받는다.</summary>
/// <remarks>
/// 근접 공격이나 투사체가 대상을 맞혔을 때 쓴다. 대상마다 체력을 다루는 코드를 몰라도 된다.
/// 피해가 거부될 수도 있으므로 요청만 하고 끝내지 말고 결과를 확인한다.
/// </remarks>
public interface IDamageable
{
    /// <summary>피해를 적용하고 실제 피해량과 처치 여부, 거부되었다면 그 이유를 돌려준다.</summary>
    DamageResult TakeDamage(DamageInfo damage);
}
