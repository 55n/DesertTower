using System;
using UnityEngine;

/// <summary>이동, 조준, 공격 등 플레이어가 하려는 행동을 알려 준다.</summary>
/// <remarks>
/// 플레이어 코드가 키보드나 게임패드의 버튼을 직접 확인하지 않도록 모아 둔다.
/// 버튼을 눌렀다고 행동이 바로 성공하는 것은 아니다. 마나나 게임 상태는 실행하는 쪽에서 확인한다.
/// </remarks>
public interface IPlayerInput
{
    /// <summary>플레이어가 이동하려는 방향이다.</summary>
    Vector2 Move { get; }
    /// <summary>플레이어가 시점을 움직이려는 입력이다.</summary>
    Vector2 Look { get; }
    /// <summary>기본 공격 버튼을 계속 누르고 있는지 알려 준다.</summary>
    bool PrimaryAttackHeld { get; }
    /// <summary>주 행동 버튼을 누르면 알린다. 현재 모드에서 무엇을 할지는 입력을 받는 코드가 정한다.</summary>
    event Action PrimaryActionPressed;
    /// <summary>마법 사용 버튼을 누르면 알린다.</summary>
    event Action SpellPressed;
    /// <summary>점프 버튼을 누르면 알린다. 추가 점프 가능 여부는 이동 코드가 확인한다.</summary>
    event Action JumpPressed;
    /// <summary>대시 버튼을 누르면 알린다. 마나와 대기 시간은 이동 코드가 확인한다.</summary>
    event Action DashPressed;
    /// <summary>핫바에서 선택한 칸 번호를 알린다. 0부터 시작하며 월드의 건설 슬롯 ID와는 다르다.</summary>
    event Action<int> SlotSelected;
    /// <summary>건설 모드 전환 버튼을 누르면 알린다.</summary>
    event Action BuildModeToggled;
    /// <summary>건설 미리보기를 몇 번 회전할지 알린다. 한 번은 90도이고 부호로 방향을 구분한다.</summary>
    event Action<int> BuildRotationRequested;
    /// <summary>취소 버튼을 누르면 알린다.</summary>
    event Action CancelPressed;
    /// <summary>일시정지 버튼을 누르면 알린다.</summary>
    event Action PausePressed;
}
