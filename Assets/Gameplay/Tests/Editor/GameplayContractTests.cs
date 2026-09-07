using System;
using NUnit.Framework;
using UnityEngine;

public sealed class GameplayContractTests
{
    [Test]
    public void UninitializedPerceptionCannotAuthorizeAnAttackOrPath()
    {
        Assert.That(default(VisibilityResult).State, Is.EqualTo(VisibilityState.Invalid));
        Assert.That(default(ReachabilityResult).State, Is.EqualTo(ReachabilityState.Invalid));
        Assert.That(default(ObstructionRequest).IsValid, Is.False);
        Assert.That(default(ObstructionResult).Blocker, Is.Null);
    }

    [Test]
    public void StalePartialResultKeepsItsJudgmentAndReachableEndpoint()
    {
        var endpoint = new Vector3(3f, 0f, 2f);
        var result = new ReachabilityResult(ReachabilityState.Partial, endpoint, 7, true);
        Assert.That(result.State, Is.EqualTo(ReachabilityState.Partial));
        Assert.That(result.EndPosition, Is.EqualTo(endpoint));
        Assert.That(result.IsStale, Is.True);
        Assert.That(result.NavigationRevision, Is.EqualTo(7));
        Assert.That(new VisibilityResult(VisibilityState.Visible, 7, true).State,
            Is.EqualTo(VisibilityState.Visible));
    }

    [Test]
    public void InvalidPerceptionCannotSupplyAnApparentDestination()
    {
        var result = new ReachabilityResult(ReachabilityState.Invalid, Vector3.one * 100f, 0);
        Assert.That(result.EndPosition, Is.EqualTo(Vector3.zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReachabilityResult((ReachabilityState)99, Vector3.zero, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VisibilityResult((VisibilityState)99, 0));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void NonFiniteGeometryCannotBecomeAnObstructionOrMovementResult(float value)
    {
        var invalidPosition = new Vector3(value, 0f, 0f);
        var route = new ReachabilityRequest(Vector3.zero, Vector3.forward, 0, -1, 0);
        Assert.That(new ObstructionRequest(route, invalidPosition, "Enemy", 2f, 1f).IsValid,
            Is.False);
        Assert.That(new ObstructionRequest(route, Vector3.zero, "Enemy", value, 1f).IsValid,
            Is.False);
        Assert.That(new ObstructionRequest(route, Vector3.zero, "Enemy", 2f, value).IsValid,
            Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReachabilityResult(ReachabilityState.Partial, invalidPosition, 0));
    }

    [Test]
    public void ObstructionRequiresFactionUsableAreaAndSearchRadius()
    {
        var route = new ReachabilityRequest(Vector3.zero, Vector3.forward, 0, -1, 3);
        Assert.That(new ObstructionRequest(route, Vector3.zero, "Enemy", 2f, 1f).IsValid, Is.True);
        Assert.That(new ObstructionRequest(route, Vector3.zero, "", 2f, 1f).IsValid, Is.False);
        Assert.That(new ObstructionRequest(route, Vector3.zero, "Enemy", 0f, 1f).IsValid, Is.False);
        var noArea = new ReachabilityRequest(Vector3.zero, Vector3.forward, 0, 0, 3);
        Assert.That(new ObstructionRequest(noArea, Vector3.zero, "Enemy", 2f, 1f).IsValid, Is.False);
    }

    [Test]
    public void ValidCurrentTargetIsRetainedAheadOfBlockerPriority()
    {
        var current = Guid.NewGuid();
        var blocker = Guid.NewGuid();
        var candidates = new[]
        {
            new TargetCandidate(blocker, true, false, 0, 1f),
            new TargetCandidate(current, true, true, 3, 4f)
        };
        Assert.That(new PriorityTargetPolicy().Select(current, candidates), Is.EqualTo(current));
    }

    [Test]
    public void InaccessibleCurrentTargetYieldsToEligibleBlocker()
    {
        var current = Guid.NewGuid();
        var blocker = Guid.NewGuid();
        var candidates = new[]
        {
            new TargetCandidate(current, false, false, 1, 1f),
            new TargetCandidate(blocker, true, false, 0, 4f)
        };
        Assert.That(new PriorityTargetPolicy().Select(current, candidates), Is.EqualTo(blocker));
    }

    [Test]
    public void NearbyButIneligibleBlockerDoesNotOverrideReachableTarget()
    {
        var target = Guid.NewGuid();
        var candidates = new[]
        {
            new TargetCandidate(Guid.NewGuid(), false, false, 0, 0.1f),
            new TargetCandidate(target, true, false, 1, 4f)
        };
        Assert.That(new PriorityTargetPolicy().Select(null, candidates), Is.EqualTo(target));
    }
}
