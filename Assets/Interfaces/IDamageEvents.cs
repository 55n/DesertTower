using System;

/// <summary>대상이 실제로 피해를 받았을 때 알려 준다.</summary>
/// <remarks>
/// 피격 효과나 소리를 재생하는 코드가 쓴다. 이 알림을 받는 쪽이 직접 피해를 줄 필요는 없다.
/// 피해가 거부됐을 때는 알리지 않는다.
/// </remarks>
public interface IDamageEvents
{
    /// <summary>실제로 피해를 적용한 뒤 알린다. 피해 요청이 거부됐을 때는 알리지 않는다.</summary>
    event Action<DamageAppliedInfo> Damaged;
}
