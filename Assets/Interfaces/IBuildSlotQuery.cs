using System.Collections.Generic;

/// <summary>레벨의 지정 건설 자리와 현재 점유 상태를 조회한다.</summary>
/// <remarks>건설 미리보기와 실제 건설 코드가 같은 조회 기능을 쓴다. 자유 배치의 충돌과 바닥 검사는 별도로 한다.</remarks>
public interface IBuildSlotQuery
{
    /// <summary>현재 슬롯 정보를 복사해서 돌려준다. 나중에 슬롯이 바뀌어도 받은 목록은 바뀌지 않는다.</summary>
    IReadOnlyList<BuildSlotState> GetSnapshot();
    /// <summary>슬롯을 찾아 현재 위치와 점유 상태를 돌려준다. 없거나 설정이 잘못됐으면 false와 기본값을 돌려준다.</summary>
    bool TryGetSlot(string slotId, out BuildSlotState slot);
}
