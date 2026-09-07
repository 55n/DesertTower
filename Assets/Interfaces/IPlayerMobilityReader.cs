using System;

/// <summary>화면에 보여 줄 점프와 대시 상태를 알려 준다.</summary>
/// <remarks>이동 코드가 값을 제공한다. 화면은 이 정보로 캐릭터를 움직이거나 마나를 쓰지 않는다.</remarks>
public interface IPlayerMobilityReader
{
    /// <summary>지금 남은 추가 점프와 대시 비용·대기 시간·사용 가능 여부다.</summary>
    PlayerMobilityState State { get; }
    /// <summary>화면에 표시할 이동 상태가 바뀌면 새 값을 알린다.</summary>
    event Action<PlayerMobilityState> Changed;
}
