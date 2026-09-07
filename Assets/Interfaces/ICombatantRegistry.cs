using System;

/// <summary>캐릭터나 시설을 전장 목록에 등록하고 뺀다.</summary>
/// <remarks>
/// 캐릭터나 시설을 만들거나 없애는 코드가 쓴다. 같은 대상을 두 번 등록하지 않는다.
/// 부활할 대상은 목록에 남겨 둔다. 목록에서 빼는 것만으로 대상을 죽이거나 없애지는 않는다.
/// </remarks>
public interface ICombatantRegistry
{
    /// <summary>대상을 전장 목록에 넣는다.</summary>
    /// <remarks>
    /// 같은 EntityId가 이미 있으면 false를 돌려주고 기존 대상을 바꾸지 않는다.
    /// </remarks>
    bool TryRegister(ICombatTarget target);
    /// <summary>이 ID의 대상을 전장 목록에서 뺀다.</summary>
    /// <remarks>
    /// 무력화되어 부활할 수 있는 대상은 남겨 두고, 게임에서 제거할 때 목록에서도 뺀다.
    /// </remarks>
    bool Unregister(Guid entityId);
}
