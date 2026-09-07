/// <summary>대상을 처치한 것으로 처리하지 않고 게임에서 없앤다.</summary>
/// <remarks>
/// 시설 철거, 소환수 정리, 코어의 적 흡수 등에 쓴다.
/// 사망 알림은 만들지 않는다. 환급이나 코어 피해는 이 기능을 부르는 쪽에서 함께 처리한다.
/// </remarks>
public interface IDespawnable
{
    /// <summary>사망으로 처리하지 않고 대상을 제거한다.</summary>
    /// <remarks>
    /// 이미 제거한 대상이면 false를 돌려주고 추가 알림을 만들지 않는다.
    /// </remarks>
    bool TryDespawn();
}
