using System;
using UnityEngine;

/// <summary>공격 대상을 찾고 공격하는 데 필요한 정보를 모아 둔다.</summary>
/// <remarks>
/// 누구인지, 어느 편인지, 어디를 맞힐지, 지금 공격할 수 있는지 알려 준다.
/// 한 캐릭터의 몸과 방패가 따로 충돌해도 같은 ID를 써서 한 대상으로 다룬다.
/// </remarks>
public interface ICombatTarget
{
    /// <summary>대상을 구분하는 ID다. 몸의 여러 충돌체와 부활 전후에 같은 값을 쓴다.</summary>
    Guid EntityId { get; }
    /// <summary>어느 편에 속하는지 알려 준다.</summary>
    string FactionId { get; }
    /// <summary>플레이어, 적, 타워 등 대상의 종류를 알려 준다.</summary>
    CombatTargetKind Kind { get; }
    /// <summary>조준하거나 공격할 때 기준으로 삼는 위치다.</summary>
    Vector3 HitPosition { get; }
    /// <summary>지금 공격 대상으로 삼을 수 있는지 알려 준다.</summary>
    bool IsTargetable { get; }
    /// <summary>이 대상에게 실제 피해를 줄 때 사용한다.</summary>
    IDamageable DamageReceiver { get; }
    /// <summary>이 대상의 사망, 부활, 제거 상태를 확인할 때 사용한다.</summary>
    ILifeState LifeState { get; }
}
