using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>지어진 시설의 종류, 비용, 체력, 차지한 자리를 알려 준다.</summary>
/// <remarks>
/// 건설·수리·철거 코드가 타워와 소환진을 같은 방식으로 다루기 위해 쓴다.
/// 화면에는 이 객체 대신 FacilityViewData를 전달한다.
/// 슬롯에 지었는지 자유롭게 지었는지 기록한다. 같은 위치라도 층이 다르면 차지한 자리를 구분한다.
/// </remarks>
public interface IFacility
{
    /// <summary>지어진 시설 하나를 구분하는 ID다.</summary>
    Guid EntityId { get; }
    /// <summary>이 시설이 어떤 종류인지 찾는 ID다.</summary>
    string DefinitionId { get; }
    /// <summary>이 시설의 건설 비용이다.</summary>
    int BuildCost { get; }
    /// <summary>현재 강화 단계다. 강화하지 않았으면 0이다.</summary>
    int UpgradeLevel { get; }
    /// <summary>이 시설이 도달할 수 있는 마지막 강화 단계다.</summary>
    int MaxUpgradeLevel { get; }
    /// <summary>수리 비용 계산에 쓰는 비율이다.</summary>
    float RepairCostRatio { get; }
    /// <summary>철거 환급액 계산에 쓰는 비율이다.</summary>
    float RefundRatio { get; }
    /// <summary>시설의 현재 체력과 체력 변화를 확인한다.</summary>
    IHealth Health { get; }
    /// <summary>시설의 체력을 회복할 때 쓴다. 수리 비용과 허용 여부는 수리 서비스가 확인한다.</summary>
    IRepairable Repairable { get; }
    /// <summary>시설 자체를 제거할 때 쓴다. 환급과 자리 정리는 철거 서비스에서 함께 처리한다.</summary>
    IDespawnable Despawnable { get; }
    /// <summary>슬롯에 지었는지 자유 배치로 지었는지 알려 준다.</summary>
    PlacementKind PlacementKind { get; }
    /// <summary>슬롯 건설이면 해당 슬롯 ID, 자유 배치면 null이다. 단계가 바뀌어도 이 기록은 유지한다.</summary>
    string BuildSlotId { get; }
    /// <summary>시설이 놓인 바닥을 구분한다. 위아래 층을 같은 자리로 잘못 보지 않기 위해 쓴다.</summary>
    /// <remarks>
    /// 시설마다 다른 ID를 쓰는 것이 아니다. 같은 바닥의 슬롯 건설과 자유 배치는 같은 ID를 쓴다.
    /// </remarks>
    string OccupancySurfaceId { get; }
    /// <summary>이 시설이 차지한 월드 XZ 격자 칸을 알려 준다. 받은 목록은 바꾸지 않는다.</summary>
    /// <remarks>
    /// OccupancySurfaceId와 함께 자리를 구분한다. 다른 바닥 ID여도 시설의 실제 모양이 겹치는지는 별도로 확인한다.
    /// </remarks>
    IReadOnlyList<Vector2Int> OccupiedCells { get; }
}
