using System;

/// <summary>이번 작업에 쓸 마나를 잠깐 잡아 둔다.</summary>
/// <remarks>
/// 시설이나 마법을 준비하는 동안 다른 작업이 같은 마나를 먼저 쓰지 못하게 한다.
/// 작업이 성공하면 한 번만 차감한다. 취소하면 잡아 둔 마나를 풀고, 마나를 쓴 것으로 처리하지 않는다.
/// </remarks>
public interface IManaReservation : IDisposable
{
    /// <summary>잡아 둔 마나를 실제로 한 번 차감한다.</summary>
    /// <remarks>
    /// 살아 있는 예약은 잔액 부족 때문에 실패하지 않는다. 이미 사용·취소했거나 게임을 초기화했다면 false이며 추가 효과가 없다.
    /// 건설이나 마법 등 해당 작업의 다른 변경도 모두 끝낸 뒤 마나 변경을 알린다.
    /// 상속받은 Dispose는 예약을 정리할 때 쓴다. 여러 번 호출하거나 이미 사용·초기화한 뒤 호출해도 추가 효과가 없다.
    /// 사용 전 Dispose는 예약만 풀고, 이미 사용한 마나를 Gain으로 돌려주지 않는다.
    /// </remarks>
    bool TryCommit();
}
