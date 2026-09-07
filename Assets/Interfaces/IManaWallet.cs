/// <summary>마나를 쓰거나 채우고, 사용할 마나를 잡아 둔다.</summary>
/// <remarks>
/// 건설·마법·충전 코드가 쓴다. 화면에 잔액만 보여 줄 때는 IManaReader를 쓴다.
/// 마나 계산을 한곳에 모아 여러 기능이 같은 마나를 중복해서 쓰지 않게 한다.
/// </remarks>
public interface IManaWallet : IManaReader
{
    /// <summary>지정한 마나를 쓸 수 있으면 차감한다.</summary>
    /// <remarks>
    /// 다른 작업이 잡아 둔 마나는 쓸 수 없다. 음수는 거부하고 0은 허용한다. 실패하면 아무것도 바꾸지 않는다.
    /// </remarks>
    bool TrySpend(int amount);
    /// <summary>이 작업에 쓸 마나를 다른 작업이 먼저 쓰지 못하게 잡아 둔다.</summary>
    /// <remarks>
    /// 표시 잔액인 CurrentMana와 Changed 알림은 바뀌지 않는다.
    /// 음수이거나 쓸 마나가 부족하면 false를 돌려주며 reservation은 null이고 바뀌는 상태는 없다.
    /// 예약해도 마나를 더 받을 빈자리가 생기는 것은 아니다. Gain은 예약액을 포함한 잔액으로 최대치를 계산한다.
    /// 예약을 받은 쪽은 사용 후 Dispose로 정리해야 한다. 게임을 초기화하면 기존 예약은 쓸 수 없다.
    /// </remarks>
    bool TryReserve(int amount, out IManaReservation reservation);
    /// <summary>마나를 채우고 실제로 늘어난 양을 돌려준다. 최대치를 넘는 양은 버린다.</summary>
    /// <remarks>
    /// 음수는 허용하지 않는다. 잡아 둔 마나도 현재 잔액에 포함하므로 예약만으로 더 받을 수는 없다.
    /// </remarks>
    int Gain(int amount);
}
