/// <summary>마나를 써서 선택한 마법을 사용한다.</summary>
/// <remarks>
/// 플레이어 행동 코드가 마법 종류와 조준 위치를 알려 줄 때 쓴다.
/// 실패하면 마나가 줄거나 쿨다운이 시작되면 안 된다. 성공했을 때 비용과 마법 효과를 함께 적용한다.
/// </remarks>
public interface ISpellCaster
{
    /// <summary>지정한 마법을 사용한다.</summary>
    /// <remarks>
    /// 현재 조건을 다시 확인하고 필요한 마나를 잡아 둔 뒤 마법 효과를 준비한다.
    /// 마나·쿨다운·효과를 모두 반영한 뒤 알린다. 실패하면 이 상태들을 바꾸지 않는다.
    /// </remarks>
    ActionResult TryCast(SpellCastRequest request);
}
