using UnityEngine;

/// <summary>적과 소환수를 목적지로 이동시키거나 멈춘다.</summary>
/// <remarks>
/// AI가 목적지만 알려 주면 이동 코드가 길을 찾아 움직이게 한다.
/// 중간에 길이 막혀 멈춘 것과 목적지에 도착한 것을 구분한다. 플레이어의 조작 이동이나 점프는 따로 다룬다.
/// </remarks>
public interface IMovementController
{
    /// <summary>지금 이동 중인 대상의 위치다.</summary>
    Vector3 Position { get; }
    /// <summary>현재 이동 목적지가 있는지 알려 준다.</summary>
    bool HasDestination { get; }
    /// <summary>목적지까지 길이 있는지, 중간까지만 갈 수 있는지 등의 경로 상태다.</summary>
    MovementPathState PathState { get; }
    /// <summary>현재 경로로 갈 수 있는 끝 위치다.</summary>
    /// <remarks>
    /// Complete면 요청한 목적지, Partial이면 실제로 갈 수 있는 끝이다. Partial의 끝이 현재 위치와 같을 수도 있다.
    /// None 또는 Invalid면 현재 Position을 돌려준다.
    /// </remarks>
    Vector3 PathEndPosition { get; }
    /// <summary>Complete 경로로 요청한 목적지에 도착했을 때만 true다. 길이 막힌 중간 지점에 도착한 것은 false다.</summary>
    bool HasArrived { get; }
    /// <summary>새 목적지까지 길을 계산하고 적용한다. 적용을 끝낸 뒤 true를 돌려준다.</summary>
    /// <remarks>
    /// 현재 단계에서는 이 호출 안에서 계산을 끝낸다. 중간까지만 갈 수 있는 Partial 경로도 수락할 수 있다.
    /// 잘못된 좌표, 이동 영역 밖 목적지, 비활성 상태이거나 이동 영역 밖인 대상, 쓸 수 없는 경로는 거부한다.
    /// false면 이전 목적지·경로·상태를 그대로 둔다.
    /// 현재 목적지와 0.5m 이내이면 기존 목적지가 있고 현재 길 정보에서도 경로가 유효할 때만 그대로 true를 돌려준다.
    /// 길 정보가 바뀌었으면 같은 목적지라도 다시 계산한다.
    /// </remarks>
    bool TrySetDestination(Vector3 destination);
    /// <summary>이동을 멈추고 목적지와 경로를 지운다. 일시정지 기능으로 쓰지 않는다.</summary>
    /// <remarks>
    /// PathState는 None, HasDestination과 HasArrived는 false가 된다. 여러 번 호출해도 괜찮다.
    /// 이전에 시작한 오래된 작업이 뒤늦게 끝나도 이동을 다시 시작하면 안 된다.
    /// </remarks>
    void Stop();
}
