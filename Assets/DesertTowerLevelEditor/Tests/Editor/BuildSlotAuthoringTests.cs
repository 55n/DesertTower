using System;
using System.Linq;
using DesertTower.Levels.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DesertTower.Levels.Tests
{
    public sealed class BuildSlotAuthoringTests
    {
        LevelRoot root;
        LevelLayout layout;
        [SetUp] public void Setup() { root = new GameObject("Slot authoring").AddComponent<LevelRoot>(); }
        [TearDown] public void Cleanup()
        {
            Undo.ClearAll();
            if (layout) UnityEngine.Object.DestroyImmediate(layout);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
        LevelBuildSlot Slot()
        {
            var slot = LevelAuthoring.AddBuildSlot(root, new Vector3(2, 3, 4));
            slot.occupancySurfaceId = "floor-2"; slot.allowedFacilityIds.Add("tower");
            slot.footprint = new Vector2(4, 3); slot.transform.rotation = Quaternion.Euler(0, 35, 0);
            return slot;
        }
        [Test] public void SlotsRoundTripWithWorldPoseStableIdsAndIndependentLists()
        {
            root.transform.SetPositionAndRotation(new Vector3(10, 5, 2), Quaternion.Euler(0, 70, 0));
            var slot = Slot(); var id = slot.id; var position = slot.transform.position; var rotation = slot.transform.rotation;
            slot.available = false;
            layout = LevelAuthoring.Capture(root);
            Assert.That(layout.schemaVersion, Is.EqualTo(2));
            slot.allowedFacilityIds.Add("summoner");
            Assert.That(layout.buildSlots[0].allowedFacilityIds, Is.EqualTo(new[] { "tower" }));
            LevelAuthoring.Restore(root, layout);
            var restored = root.BuildSlots.Single();
            Assert.That(restored.id, Is.EqualTo(id));
            Assert.That(Vector3.Distance(restored.transform.position, position), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(restored.transform.rotation, rotation), Is.LessThan(.01f));
            Assert.That(restored.occupancySurfaceId, Is.EqualTo("floor-2"));
            Assert.That(restored.footprint, Is.EqualTo(new Vector2(4, 3)));
            Assert.That(restored.available, Is.False);
            restored.allowedFacilityIds.Clear();
            Assert.That(layout.buildSlots[0].allowedFacilityIds, Is.EqualTo(new[] { "tower" }));
        }
        [TestCase(1)] [TestCase(2)]
        public void LayoutWithoutSlotListStillLoads(int version)
        {
            Slot(); layout = ScriptableObject.CreateInstance<LevelLayout>(); layout.schemaVersion = version;
            layout.buildSlots = null;
            LevelAuthoring.Restore(root, layout);
            Assert.That(root.BuildSlots, Is.Empty);
        }
        [TestCase("duplicate")] [TestCase("surface")] [TestCase("size")] [TestCase("pose")] [TestCase("null")]
        public void InvalidSlotLayoutDoesNotDestroyExistingLevel(string problem)
        {
            var slot = Slot(); layout = LevelAuthoring.Capture(root);
            switch (problem)
            {
                case "duplicate": layout.buildSlots.Add(layout.buildSlots[0]); break;
                case "surface": layout.buildSlots[0].occupancySurfaceId = ""; break;
                case "size": layout.buildSlots[0].footprint = new Vector2(-1, 2); break;
                case "pose": layout.buildSlots[0].position = new Vector3(float.NaN, 0, 0); break;
                case "null": layout.buildSlots.Add(null); break;
            }
            Assert.Throws<ArgumentException>(() => LevelAuthoring.Restore(root, layout));
            Assert.That(root.BuildSlots.Single(), Is.SameAs(slot));
        }
        [Test] public void ValidationReportsDuplicateSlotsAndMissingSurfaceWithoutNavigation()
        {
            var slot = Slot(); var second = Slot(); second.id = slot.id; second.occupancySurfaceId = "";
            var issues = LevelValidation.Check(root, false);
            Assert.That(issues.Any(i => i.severity == IssueSeverity.Error && i.context == second && i.message.Contains("중복")), Is.True);
            Assert.That(issues.Any(i => i.severity == IssueSeverity.Error && i.context == second && i.message.Contains("바닥")), Is.True);
        }
        [Test] public void RestoreCanBeUndoneWithoutLosingPreviousSlot()
        {
            var slot = Slot(); var id = slot.id;
            layout = LevelAuthoring.Capture(root); layout.buildSlots.Clear();
            Undo.ClearAll();
            LevelAuthoring.Restore(root, layout);
            Assert.That(root.BuildSlots, Is.Empty);
            Undo.PerformUndo();
            Assert.That(root.BuildSlots.Single().id, Is.EqualTo(id));
        }
    }
}
