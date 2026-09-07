using System;

/// <summary>지금 준비 중인지 전투 중인지, 게임이 멈췄는지 알려 준다.</summary>
/// <remarks>
/// 화면 표시나 건설·공격 가능 여부를 확인하는 코드가 쓴다. 게임 상태를 바꾸는 기능은 없다.
/// 일시정지를 풀면 원래 준비 또는 전투 단계로 돌아갈 수 있도록 따로 기록한다.
/// </remarks>
public interface IGameStateReader
{
    /// <summary>준비·전투·승리·패배 중 현재 단계를 알려 준다.</summary>
    GamePhase Phase { get; }
    /// <summary>현재 일시정지 중인지 알려 준다. 게임 단계와 따로 관리하므로 재개할 때 원래 단계로 돌아간다.</summary>
    bool IsPaused { get; }
    /// <summary>게임 단계나 일시정지 상태가 바뀌면 알린다.</summary>
    event Action Changed;
}
