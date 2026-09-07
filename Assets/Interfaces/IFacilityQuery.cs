using System;
using System.Collections.Generic;

/// <summary>시설 목록과 시설 메뉴에 필요한 값을 조회한다.</summary>
/// <remarks>건설 코드가 관리하는 시설 정보를 화면에 제공한다. 받은 값으로 시설 자체를 바꿀 수는 없다.</remarks>
public interface IFacilityQuery
{
    /// <summary>현재 등록된 시설들의 표시 값을 복사해서 돌려준다. 나중에 시설이 바뀌어도 이 목록은 바뀌지 않는다.</summary>
    IReadOnlyList<FacilityViewData> GetSnapshot();
    /// <summary>ID에 맞는 시설 정보를 찾는다. 없으면 false와 기본값을 돌려준다.</summary>
    bool TryGetFacility(Guid facilityId, out FacilityViewData facility);
    /// <summary>시설이 생기거나 바뀌거나 사라지면 해당 ID를 알린다. 다시 조회해서 화면을 갱신한다.</summary>
    event Action<Guid> Changed;
}
