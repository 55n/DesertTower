/// <summary>무력화된 대상을 최대 체력으로 다시 일으킨다.</summary>
/// <remarks>
/// 부활을 처리하는 코드가 쓴다. 언제, 어디서 부활할지는 그 코드가 정한다.
/// 새 캐릭터를 만드는 것이 아니라 같은 대상을 다시 살리는 기능이다.
/// </remarks>
public interface IRevivable
{
    /// <summary>무력화된 대상을 최대 체력으로 부활시킨다. 부활 위치와 대기 시간은 이 함수를 부르는 쪽에서 정한다.</summary>
    bool TryRevive();
}
