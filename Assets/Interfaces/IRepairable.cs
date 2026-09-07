/// <summary>아직 파괴되지 않은 시설의 체력을 회복시킨다.</summary>
/// <remarks>
/// 수리 코드가 시설에 회복을 적용할 때 쓴다. 실제 회복한 양을 돌려준다.
/// 수리 비용과 지금 수리할 수 있는지는 IFacilityMaintenance에서 먼저 확인한다.
/// </remarks>
public interface IRepairable
{
    /// <summary>체력을 회복시키고 실제 회복한 양을 돌려준다. 비용과 수리 가능 여부는 이 함수를 부르는 서비스가 확인한다.</summary>
    float Repair(float amount);
}
