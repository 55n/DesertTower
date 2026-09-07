using System;
using System.Collections.Generic;

/// <summary>몇 번째 웨이브인지, 적이 얼마나 남았는지, 준비 시간이 얼마인지 알려 준다.</summary>
/// <remarks>
/// 웨이브 안내 화면에서 쓴다. 적을 만들거나 다음 웨이브를 시작하는 기능은 없다.
/// 처치되거나 흡수된 적은 남은 수에서 한 번만 뺀다. 아직 등장하지 않은 적은 별도로 센다.
/// </remarks>
public interface IWaveStateReader
{
    /// <summary>현재 웨이브 번호다. 첫 준비 단계부터 1로 표시한다.</summary>
    int WaveNumber { get; }
    /// <summary>전체 웨이브 수다.</summary>
    int TotalWaves { get; }
    /// <summary>이번 웨이브에서 등장했지만 아직 처치되거나 제거되지 않은 적 수다.</summary>
    /// <remarks>
    /// 전체 전장의 살아 있는 대상 수와는 다르다. Died 또는 Despawned를 받으면 해당 ID를 한 번만 뺀다.
    /// 두 알림이 모두 와도 두 번 빼지 않는다. 현재 단계의 웨이브 적은 부활하지 않으며, 부활을 추가하면 다시 세는 규칙이 필요하다.
    /// </remarks>
    int AliveEnemyCount { get; }
    /// <summary>이번 웨이브에서 앞으로 등장할 적 수다. 시야나 경로 계산 대기와는 관계없다.</summary>
    /// <remarks>
    /// 이 수와 AliveEnemyCount가 모두 0이 되어야 웨이브를 한 번 완료 처리한다.
    /// </remarks>
    int PendingEnemyCount { get; }
    /// <summary>준비 시간이 몇 초 남았는지 알려 준다. null이면 첫 준비의 시간 제한이 없다는 뜻이고, 0이면 남은 준비 시간이 없다.</summary>
    float? PreparationSecondsRemaining { get; }
    /// <summary>다음 웨이브에서 사용할 진입로 ID 목록이다.</summary>
    IReadOnlyList<string> NextRouteIds { get; }
    /// <summary>웨이브 표시 정보가 바뀌면 알린다.</summary>
    event Action Changed;
}
