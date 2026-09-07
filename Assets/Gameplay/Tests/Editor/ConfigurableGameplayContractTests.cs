using System;
using NUnit.Framework;
using UnityEngine;

public sealed class ConfigurableGameplayContractTests
{
    private sealed class GameState : IGameStateReader
    {
        public GamePhase Phase { get; set; }
        public bool IsPaused { get; set; }
        public event Action Changed { add { } remove { } }
    }

    private static PlacementRequest FreeRequest()
        => new PlacementRequest("tower", new Vector3(1f, 2f, 3f), Quaternion.identity);

    [Test]
    public void PreparationOnlyAllowsSlotsEvenWhenCombatAllowsFreePlacement()
    {
        var state = new GameState { Phase = GamePhase.Preparation };
        var rule = new PlacementPhaseRule(state);
        Assert.That(rule.Evaluate(PlacementRequest.AtSlot("tower", "slot-a")).CanPlace, Is.True);
        Assert.That(rule.Evaluate(FreeRequest()).Failure, Is.EqualTo(PlacementFailure.PlacementModeNotAllowed));
    }

    [Test]
    public void CombatDefaultsToBothPlacementModes()
    {
        var rule = new PlacementPhaseRule(new GameState { Phase = GamePhase.Combat });
        Assert.That(rule.Evaluate(PlacementRequest.AtSlot("tower", "slot-a")).CanPlace, Is.True);
        Assert.That(rule.Evaluate(FreeRequest()).CanPlace, Is.True);
    }

    [TestCase(PlacementModes.None, false, false)]
    [TestCase(PlacementModes.DesignatedSlot, true, false)]
    [TestCase(PlacementModes.Free, false, true)]
    [TestCase(PlacementModes.DesignatedSlot | PlacementModes.Free, true, true)]
    public void CombatPermissionsCanChangeWithoutChangingRequestOrBuilderContracts(
        PlacementModes modes, bool slotAllowed, bool freeAllowed)
    {
        var rule = new PlacementPhaseRule(new GameState { Phase = GamePhase.Combat }, modes);
        Assert.That(rule.Evaluate(PlacementRequest.AtSlot("tower", "slot-a")).CanPlace, Is.EqualTo(slotAllowed));
        Assert.That(rule.Evaluate(FreeRequest()).CanPlace, Is.EqualTo(freeAllowed));
    }

    [Test]
    public void EarlierCombatPreviewDoesNotAuthorizeFreePlacementAfterPhaseChange()
    {
        var state = new GameState { Phase = GamePhase.Combat };
        var validator = new CompositePlacementValidator(new IPlacementRule[] { new PlacementPhaseRule(state) });
        var request = FreeRequest();
        Assert.That(validator.Validate(request).CanPlace, Is.True);
        state.Phase = GamePhase.Preparation;
        Assert.That(validator.Validate(request).Failure, Is.EqualTo(PlacementFailure.PlacementModeNotAllowed));
        Assert.That(request.Kind, Is.EqualTo(PlacementKind.Free));
    }

    [TestCase(GamePhase.Victory)]
    [TestCase(GamePhase.Defeat)]
    [TestCase((GamePhase)99)]
    public void NonBuildingPhasesRejectBothModes(GamePhase phase)
    {
        var rule = new PlacementPhaseRule(new GameState { Phase = phase });
        Assert.That(rule.Evaluate(FreeRequest()).Failure, Is.EqualTo(PlacementFailure.WrongPhase));
        Assert.That(rule.Evaluate(PlacementRequest.AtSlot("tower", "slot-a")).Failure,
            Is.EqualTo(PlacementFailure.WrongPhase));
    }

    [Test]
    public void PausingInvalidatesBothModesUntilResumed()
    {
        var state = new GameState { Phase = GamePhase.Combat, IsPaused = true };
        var rule = new PlacementPhaseRule(state);
        Assert.That(rule.Evaluate(FreeRequest()).Failure, Is.EqualTo(PlacementFailure.Paused));
        Assert.That(rule.Evaluate(PlacementRequest.AtSlot("tower", "slot-a")).Failure,
            Is.EqualTo(PlacementFailure.Paused));
        state.IsPaused = false;
        Assert.That(rule.Evaluate(FreeRequest()).CanPlace, Is.True);
    }

    [Test]
    public void InvalidRequestsCannotPassAnOtherwisePermissiveRuleChain()
    {
        var validator = new CompositePlacementValidator(new IPlacementRule[] {
            new PlacementPhaseRule(new GameState { Phase = GamePhase.Combat }) });
        Assert.That(validator.Validate(default).Failure, Is.EqualTo(PlacementFailure.InvalidRequest));
        Assert.That(validator.Validate(PlacementRequest.AtSlot("tower", " ")).Failure,
            Is.EqualTo(PlacementFailure.InvalidRequest));
        Assert.That(validator.Validate(PlacementRequest.AtSlot("", "slot-a")).CanPlace, Is.False);
        Assert.That(validator.Validate(new PlacementRequest("tower", Vector3.zero, default)).CanPlace, Is.False);
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void NonFiniteFreePlacementGeometryIsRejected(float value)
    {
        var rule = new PlacementPhaseRule(new GameState { Phase = GamePhase.Combat });
        Assert.That(rule.Evaluate(new PlacementRequest("tower", new Vector3(value, 0, 0), Quaternion.identity)).CanPlace,
            Is.False);
        Assert.That(rule.Evaluate(new PlacementRequest("tower", Vector3.zero, new Quaternion(0, value, 0, 1))).CanPlace,
            Is.False);
    }

    [Test]
    public void UnsupportedPermissionBitsAreRejectedAtComposition()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementPhaseRule(new GameState(), (PlacementModes)4));
    }

    [Test]
    public void SkillPrerequisiteSnapshotCannotBeChangedThroughItsSourceCollection()
    {
        var prerequisites = new[] { "root" };
        var snapshot = new SkillNodeState("dash", 1, false, prerequisites, ActionResult.Success());
        prerequisites[0] = "replacement";
        Assert.That(snapshot.PrerequisiteNodeIds, Is.EqualTo(new[] { "root" }));
        Assert.That(new SkillNodeState("dash", 1, true, Array.Empty<string>(), ActionResult.Success())
            .LearnAvailability.Failure, Is.EqualTo(ActionFailure.AlreadyLearned));
    }

    [Test]
    public void UninitializedOrFailedCoreArrivalCannotAuthorizeAbsorptionOrAttack()
    {
        var uninitialized = default(CoreArrivalResult);
        Assert.That(uninitialized.Outcome.Succeeded, Is.False);
        Assert.That(uninitialized.Disposition, Is.EqualTo(CoreArrivalDisposition.None));
        var failure = CoreArrivalResult.Failed(ActionFailure.NoChange);
        Assert.That(failure.Outcome.Succeeded, Is.False);
        Assert.That(failure.Disposition, Is.EqualTo(CoreArrivalDisposition.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => CoreArrivalResult.Failed(ActionFailure.None));
    }
}
