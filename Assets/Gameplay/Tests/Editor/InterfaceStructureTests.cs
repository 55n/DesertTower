using System;
using System.Collections.Generic;
using System.Linq;
using DesertTower.Levels;
using NUnit.Framework;
using UnityEngine;

public sealed class InterfaceStructureTests
{
    LevelRoot root;
    BuildSlotRegistry registry;
    [SetUp] public void Setup()
    {
        root = new GameObject("Slot test").AddComponent<LevelRoot>();
        registry = new BuildSlotRegistry(root);
    }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    LevelBuildSlot Slot(string id = "slot-a", string surface = "floor-1")
    {
        var go = new GameObject(id); go.transform.SetParent(root.transform, false);
        var slot = go.AddComponent<LevelBuildSlot>();
        slot.id = id; slot.occupancySurfaceId = surface; slot.allowedFacilityIds.Add("tower");
        return slot;
    }

    [Test] public void SlotRequestUsesCurrentWorldPoseAndPreservesFloorIdentity()
    {
        root.transform.SetPositionAndRotation(new Vector3(3, 4, 5), Quaternion.Euler(0, 50, 0));
        var slot = Slot(); slot.transform.localPosition = new Vector3(1, 0, 2);
        Assert.That(registry.TryGetSlot(slot.id, out var before), Is.True);
        slot.transform.position += Vector3.up * 3;
        Assert.That(registry.TryGetSlot(slot.id, out var after), Is.True);
        Assert.That(Vector3.Distance(after.Position, before.Position + Vector3.up * 3), Is.LessThan(.001f));
        Assert.That(after.OccupancySurfaceId, Is.EqualTo("floor-1"));
        Assert.That(Quaternion.Angle(after.Rotation, slot.transform.rotation), Is.LessThan(.001f));
        Slot("slot-b", "floor-2").transform.position = slot.transform.position;
        Assert.That(registry.GetSnapshot().Select(s => s.OccupancySurfaceId), Is.EquivalentTo(new[] { "floor-1", "floor-2" }));
    }
    [Test] public void SlotSnapshotsDoNotChangeWhenSourceOrOccupancyChanges()
    {
        var slot = Slot();
        var before = registry.GetSnapshot();
        slot.allowedFacilityIds.Add("summoner");
        var owner = Guid.NewGuid();
        Assert.That(registry.TryOccupy(slot.id, "summoner", owner), Is.True);
        Assert.That(before[0].AllowedFacilityIds, Is.EqualTo(new[] { "tower" }));
        Assert.That(before[0].OccupantId, Is.Null);
        Assert.That(registry.GetSnapshot()[0].OccupantId, Is.EqualTo(owner));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)before[0].AllowedFacilityIds).Add("other"));
    }
    [Test] public void OnlyOwnerCanReleaseAndCompetingBuildCannotStealSlot()
    {
        var slot = Slot(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        Assert.That(registry.TryOccupy(slot.id, "tower", first), Is.True);
        Assert.That(registry.TryOccupy(slot.id, "tower", second), Is.False);
        Assert.That(registry.Release(slot.id, second), Is.False);
        Assert.That(registry.GetSnapshot()[0].OccupantId, Is.EqualTo(first));
        Assert.That(registry.Release(slot.id, first), Is.True);
        Assert.That(registry.Release(slot.id, first), Is.False);
        Assert.That(registry.TryOccupy(slot.id, "tower", second), Is.True);
    }
    [Test] public void FacilityCannotOccupyTwoSlotsAndRemovalCanReleaseItsOldSlot()
    {
        var slot = Slot(); Slot("slot-b"); var owner = Guid.NewGuid();
        Assert.That(registry.TryOccupy(slot.id, "tower", owner), Is.True);
        Assert.That(registry.TryOccupy("slot-b", "tower", owner), Is.False);
        UnityEngine.Object.DestroyImmediate(slot.gameObject);
        Assert.That(registry.TryGetSlot("slot-a", out var missing), Is.False);
        Assert.That(missing.SlotId, Is.Null);
        Assert.That(registry.Release("slot-a", owner), Is.True);
        Assert.That(registry.TryOccupy("slot-b", "tower", owner), Is.True);
    }
    [Test] public void MissingDuplicateOrMalformedSlotsCannotBeBuilt()
    {
        var rule = new SlotPlacementRule(registry); var request = PlacementRequest.AtSlot("tower", "slot-a");
        Assert.That(rule.Evaluate(request).Failure, Is.EqualTo(PlacementFailure.SlotNotFound));
        var slot = Slot(); var duplicate = Slot();
        Assert.That(registry.GetSnapshot(), Is.Empty);
        Assert.That(registry.TryOccupy(slot.id, "tower", Guid.NewGuid()), Is.False);
        UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
        slot.occupancySurfaceId = "";
        Assert.That(rule.Evaluate(request).Failure, Is.EqualTo(PlacementFailure.SlotNotFound));
        slot.occupancySurfaceId = "floor"; slot.footprint = new Vector2(float.NaN, 1);
        Assert.That(rule.Evaluate(request).CanPlace, Is.False);
    }
    [Test] public void SlotRuleReportsAvailabilityTypeAndOccupancyWithoutChangingState()
    {
        var slot = Slot(); var rule = new SlotPlacementRule(registry); var request = PlacementRequest.AtSlot("tower", slot.id);
        slot.available = false;
        Assert.That(rule.Evaluate(request).Failure, Is.EqualTo(PlacementFailure.SlotUnavailable));
        slot.available = true; slot.gameObject.SetActive(false);
        Assert.That(registry.TryOccupy(slot.id, "tower", Guid.NewGuid()), Is.False);
        slot.gameObject.SetActive(true); slot.allowedFacilityIds.Clear();
        Assert.That(rule.Evaluate(request).Failure, Is.EqualTo(PlacementFailure.FacilityNotAllowed));
        slot.allowedFacilityIds.Add("tower");
        Assert.That(rule.Evaluate(request).CanPlace, Is.True);
        Assert.That(registry.GetSnapshot()[0].OccupantId, Is.Null);
        registry.TryOccupy(slot.id, "tower", Guid.NewGuid());
        Assert.That(rule.Evaluate(request).Failure, Is.EqualTo(PlacementFailure.Occupied));
    }
    [Test] public void SlotRuleLeavesFreePlacementForOtherRules()
    {
        var rule = new SlotPlacementRule(registry);
        Assert.That(rule.Evaluate(new PlacementRequest("tower", Vector3.zero, Quaternion.identity)).CanPlace, Is.True);
        Assert.That(rule.Evaluate(default).CanPlace, Is.False);
    }
    [Test] public void ParentLevelDoesNotExposeNestedLevelSlots()
    {
        var nested = new GameObject("Nested level").AddComponent<LevelRoot>();
        nested.transform.SetParent(root.transform);
        var slot = Slot(); slot.transform.SetParent(nested.transform);
        Assert.That(registry.GetSnapshot(), Is.Empty);
        Assert.That(new BuildSlotRegistry(nested).GetSnapshot().Count, Is.EqualTo(1));
    }

    sealed class Target : ICombatTarget
    {
        public Guid EntityId { get; } = Guid.NewGuid();
        public string FactionId => "Enemy";
        public CombatTargetKind Kind => default;
        public Vector3 HitPosition => Vector3.zero;
        public bool IsTargetable => true;
        public IDamageable DamageReceiver => null;
        public ILifeState LifeState => null;
    }
    [Test] public void ObstructionSelectionCarriesApproachAndRejectsItsUseAfterPathChanges()
    {
        var target = new Target(); var position = new Vector3(2, 0, 3);
        var selected = TargetSelection.FromObstruction(new ObstructionResult(target, position, 7));
        Assert.That(selected.Target, Is.SameAs(target));
        Assert.That(selected.ApproachPosition, Is.EqualTo(position));
        Assert.That(selected.HasCurrentApproach(7), Is.True);
        Assert.That(selected.HasCurrentApproach(8), Is.False);
        Assert.That(TargetSelection.ForTarget(target).HasCurrentApproach(7), Is.False);
        Assert.That(default(TargetSelection).HasTarget, Is.False);
        Assert.Throws<ArgumentException>(() => TargetSelection.FromObstruction(default));
    }
    [Test] public void BuildResultCarriesDisplayValuesAndNeverFacilityCommands()
    {
        var view = new FacilityViewData(Guid.NewGuid(), "tower", 70, 100, 1, 4, PlacementKind.Free);
        var result = BuildResult.Built(view);
        Assert.That(result.Outcome.Succeeded, Is.True);
        Assert.That(result.Facility.Value.CurrentHealth, Is.EqualTo(70));
        Assert.That(BuildResult.Failed(ActionFailure.CreationFailed,
            PlacementResult.Allowed()).Facility, Is.Null);
        Assert.Throws<ArgumentException>(() => BuildResult.Built(default));
        foreach (var property in typeof(FacilityViewData).GetProperties())
        {
            Assert.That(property.PropertyType.IsValueType || property.PropertyType == typeof(string), Is.True,
                property.Name + " must not expose a live facility capability");
            Assert.That(property.CanWrite, Is.False);
        }
    }
    [Test] public void FacilityDisplayCannotConfuseFreeAndSlotPlacement()
    {
        Assert.Throws<ArgumentException>(() => new FacilityViewData(Guid.NewGuid(), "tower", 1, 1, 0, 2, PlacementKind.DesignatedSlot));
        Assert.Throws<ArgumentException>(() => new FacilityViewData(Guid.NewGuid(), "tower", 1, 1, 0, 2, PlacementKind.Free, "slot-a"));
    }
    [Test] public void UpgradeQuoteHasNoNextLevelAtLimitAndCannotClaimSuccess()
    {
        var quote = new FacilityUpgradeQuote(ActionResult.Fail(ActionFailure.UpgradeLimitReached), 0, 3, 3);
        Assert.That(quote.NextLevel, Is.Null);
        Assert.Throws<ArgumentException>(() => new FacilityUpgradeQuote(ActionResult.Success(), 20, 3, 3));
        Assert.That(new FacilityUpgradeQuote(ActionResult.Success(), 17, 1, 5).NextLevel, Is.EqualTo(2));
    }
    [Test] public void MobilityStatePreservesTunableCostsAndRejectsImpossibleTimers()
    {
        var state = new PlayerMobilityState(false, 2, 17, .5f, 2, ActionResult.Success(), ActionResult.Fail(ActionFailure.Cooldown));
        Assert.That(state.RemainingAirJumps, Is.EqualTo(2));
        Assert.That(state.DashManaCost, Is.EqualTo(17));
        Assert.That(state.DashAvailability.Failure, Is.EqualTo(ActionFailure.Cooldown));
        Assert.That(default(PlayerMobilityState).DashAvailability.Succeeded, Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerMobilityState(false, 1, 10, 3, 2, default, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerMobilityState(false, 1, 10, float.NaN, 2, default, default));
    }
}
