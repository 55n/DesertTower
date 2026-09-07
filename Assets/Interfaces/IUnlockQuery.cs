using System;

/// <summary>이 시설이나 마법이 해금되었는지 알려 준다.</summary>
/// <remarks>
/// 건설 메뉴와 마법 사용 코드가 쓴다. 어떤 스킬을 거쳐 해금됐는지까지 알 필요는 없다.
/// 해금됐어도 마나가 부족하거나 자리가 막혀 있으면 사용할 수 없으므로 다른 조건은 따로 확인한다.
/// </remarks>
public interface IUnlockQuery
{
    /// <summary>이 시설이나 마법이 해금되었는지 확인한다. 레벨이 올랐다는 이유만으로 스킬을 배운 것으로 보지 않는다.</summary>
    bool IsUnlocked(string definitionId);
    /// <summary>새로 해금된 시설이나 마법의 ID를 알린다.</summary>
    event Action<string> Unlocked;
}
