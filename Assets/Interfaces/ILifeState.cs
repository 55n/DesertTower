using System;

/// <summary>대상이 살아 있는지, 죽었는지, 부활했는지, 게임에서 없어졌는지 알려 준다.</summary>
/// <remarks>
/// 처치 보상, 부활, 소환수 관리, 남은 적 수를 처리하는 코드가 쓴다.
/// 죽는 것과 그냥 없어지는 것을 구분한다. 그래야 철거나 흡수에 처치 보상이 잘못 붙지 않는다.
/// </remarks>
public interface ILifeState
{
    /// <summary>생존·사망 중·무력화·제거 중 현재 상태를 알려 준다.</summary>
    LifeState State { get; }
    /// <summary>생명 상태가 바뀌면 이전 상태와 새 상태를 알린다.</summary>
    event Action<LifeStateChangedInfo> StateChanged;
    /// <summary>살아 있던 대상의 체력이 0이 되는 순간 한 번 알린다.</summary>
    event Action<DeathInfo> Died;
    /// <summary>대상이 부활했을 때 그 대상의 ID를 알린다.</summary>
    event Action<Guid> Revived;
    /// <summary>대상이 게임에서 제거되면 한 번 알린다. 사망 알림과 구분한다.</summary>
    event Action<Guid> Despawned;
}
