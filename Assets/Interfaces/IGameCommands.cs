/// <summary>웨이브를 일찍 시작하거나, 게임을 멈추거나, 처음부터 다시 시작한다.</summary>
/// <remarks>
/// 메뉴와 입력 코드가 쓴다. 요청을 받은 쪽이 지금 가능한 행동인지 확인한다.
/// 다시 시작하면 이전 게임에서 남은 예약이나 알림이 새 게임에 영향을 주지 않게 한다.
/// </remarks>
public interface IGameCommands
{
    /// <summary>준비 시간을 기다리지 않고 다음 웨이브를 시작하도록 요청한다. true면 요청이 받아들여졌고, false면 아무것도 바꾸지 않는다.</summary>
    bool TryStartWaveEarly();
    /// <summary>paused가 true면 일시정지, false면 재개를 요청한다.</summary>
    /// <remarks>이미 원하는 상태여도 true다. 상태가 실제로 바뀔 때만 알린다. false면 상태를 바꾸지 않는다.</remarks>
    bool TrySetPaused(bool paused);
    /// <summary>새 게임으로 다시 시작한다.</summary>
    /// <remarks>
    /// 이전 마나 예약을 무효로 만들고 웨이브의 적 기록을 초기화한다. 이전 게임의 알림이나 예약이 새 게임에 영향을 주면 안 된다.
    /// </remarks>
    void Restart();
}
