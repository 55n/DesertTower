using System;

/// <summary>마법의 마나 비용과 다시 쓸 수 있을 때까지 남은 시간을 알려 준다.</summary>
/// <remarks>
/// 마법 아이콘이나 쿨다운 표시에서 쓴다. 실제로 마법을 사용하지는 않는다.
/// 마법을 쓸 수 있는지는 마나와 해금 상태도 함께 확인해야 한다.
/// </remarks>
public interface ISpellStateReader
{
    /// <summary>마법 ID로 비용과 쿨다운 상태를 찾는다. 찾으면 true, 없으면 false와 기본값을 돌려준다.</summary>
    bool TryGetState(string spellId, out SpellState state);
    /// <summary>상태가 바뀐 마법의 ID를 알린다.</summary>
    event Action<string> Changed;
}
