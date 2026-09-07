using System;

/// <summary>현재 마나와 최대 마나를 알려 주고, 바뀌면 알린다.</summary>
/// <remarks>
/// 마나 막대와 비용 표시에서 쓴다. 마나를 쓰거나 늘리는 기능은 없다.
/// 다른 작업이 쓰려고 잡아 둔 마나도 표시값에 포함된다. 실제로 쓸 수 있는지는 실행할 때 확인한다.
/// </remarks>
public interface IManaReader
{
    /// <summary>아직 쓰지 않은 전체 마나다. 다른 작업이 잡아 둔 마나도 포함한다.</summary>
    /// <remarks>
    /// 예약하거나 예약을 취소하는 것만으로는 이 값이 바뀌지 않는다.
    /// </remarks>
    int CurrentMana { get; }
    /// <summary>가질 수 있는 최대 마나다.</summary>
    int MaxMana { get; }
    /// <summary>실제로 마나가 바뀌면 알린다. 건설이나 마법 등 해당 작업을 모두 끝낸 뒤 알린다.</summary>
    event Action<ManaChangedInfo> Changed;
}
