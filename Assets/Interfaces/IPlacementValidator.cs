/// <summary>여러 조건을 확인해서 시설을 놓을 수 있는지 알려 준다.</summary>
/// <remarks>
/// 건설 미리보기와 실제 건설 코드가 같은 검사 기준을 쓰기 위해 둔다.
/// 가능 여부와 안 되는 이유만 알려 준다. 시설을 짓는 일은 IFacilityBuilder가 맡는다.
/// </remarks>
public interface IPlacementValidator
{
    /// <summary>전체 배치 조건을 검사해서 가능 여부와 실패 이유를 돌려준다. 시설을 만들거나 비용을 쓰지는 않는다.</summary>
    PlacementResult Validate(PlacementRequest request);
}
