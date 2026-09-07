using System;

/// <summary>준비 단계는 슬롯만, 전투 단계는 설정된 배치 방식만 허용한다.</summary>
public sealed class PlacementPhaseRule : IPlacementRule
{
    private readonly IGameStateReader gameState;
    public PlacementModes CombatModes { get; }

    // Configure at session composition, not from a player-supplied build request.
    public PlacementPhaseRule(IGameStateReader gameState,
        PlacementModes combatModes = PlacementModes.DesignatedSlot | PlacementModes.Free)
    {
        this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        const PlacementModes supported = PlacementModes.DesignatedSlot | PlacementModes.Free;
        if ((combatModes & ~supported) != 0) throw new ArgumentOutOfRangeException(nameof(combatModes));
        CombatModes = combatModes;
    }

    public PlacementResult Evaluate(PlacementRequest request)
    {
        if (!request.IsValid) return PlacementResult.Denied(PlacementFailure.InvalidRequest);
        if (gameState.IsPaused) return PlacementResult.Denied(PlacementFailure.Paused);
        PlacementModes allowed;
        switch (gameState.Phase)
        {
            case GamePhase.Preparation: allowed = PlacementModes.DesignatedSlot; break;
            case GamePhase.Combat: allowed = CombatModes; break;
            default: return PlacementResult.Denied(PlacementFailure.WrongPhase);
        }
        var requested = request.Kind == PlacementKind.DesignatedSlot
            ? PlacementModes.DesignatedSlot : PlacementModes.Free;
        return (allowed & requested) != 0
            ? PlacementResult.Allowed()
            : PlacementResult.Denied(PlacementFailure.PlacementModeNotAllowed);
    }
}
