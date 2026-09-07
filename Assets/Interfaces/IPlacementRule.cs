/// <summary>시설을 놓을 수 있는 조건 하나를 확인한다.</summary>
/// <remarks>
/// 지을 수 있는 단계인지, 자리가 비었는지 같은 검사를 하나씩 나눠 두기 위해 쓴다.
/// 확인만 한다. 검사하는 동안 마나를 쓰거나 자리를 차지하지 않는다.
/// </remarks>
public interface IPlacementRule
{
    /// <summary>이 규칙 하나를 통과하는지 검사하고, 안 된다면 이유를 돌려준다. 마나·자리·시설 상태는 바꾸지 않는다.</summary>
    PlacementResult Evaluate(PlacementRequest request);
}
