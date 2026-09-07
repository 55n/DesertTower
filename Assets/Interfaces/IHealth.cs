using System;

/// <summary>현재 체력과 최대 체력을 알려 주고, 바뀌면 알린다.</summary>
/// <remarks>
/// 플레이어·보스 체력 막대나 코어 안정도를 표시할 때 쓴다.
/// 체력을 읽는 기능만 둔다. 피해를 주거나 회복시키는 일은 다른 기능에 맡긴다.
/// </remarks>
public interface IHealth
{
    /// <summary>현재 남은 체력이다.</summary>
    float CurrentHealth { get; }
    /// <summary>최대 체력이다.</summary>
    float MaxHealth { get; }
    /// <summary>체력이 바뀌면 이전 값과 새 값을 알린다. 관련 상태를 모두 바꾼 뒤 알린다.</summary>
    event Action<HealthChangedInfo> HealthChanged;
}
