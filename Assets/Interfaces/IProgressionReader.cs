using System;

/// <summary>현재 레벨과 경험치, 다음 레벨까지 필요한 경험치를 알려 준다.</summary>
/// <remarks>
/// 경험치 막대를 표시하거나 레벨업을 확인할 때 쓴다.
/// 어떤 스킬을 골랐는지는 여기서 다루지 않는다. 포인트를 쓰는 스킬 선택은 스킬트리 쪽에서 맡는다.
/// </remarks>
public interface IProgressionReader
{
    /// <summary>현재 레벨이다.</summary>
    int Level { get; }
    /// <summary>현재 설정된 최대 레벨이다.</summary>
    int MaxLevel { get; }
    /// <summary>현재 레벨에서 모은 경험치다. 최대 레벨에서는 0이다.</summary>
    int ExperienceInLevel { get; }
    /// <summary>현재 레벨에서 다음 레벨이 되기 위해 필요한 경험치 기준이다. 최대 레벨에서는 0이다.</summary>
    int ExperienceToNextLevel { get; }
    /// <summary>최대 레벨에 도달했는지 알려 준다.</summary>
    bool IsMaxLevel { get; }
    /// <summary>레벨이나 경험치 정보가 바뀌면 알린다.</summary>
    event Action Changed;
    /// <summary>레벨이 올랐을 때 새 레벨을 알린다.</summary>
    event Action<int> LevelUp;
}
