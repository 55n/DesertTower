using System.Collections.Generic;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using DesertTower.Levels.Editor;

namespace DesertTower.Levels.Tests
{
    public sealed class LevelAuthoringTests
    {
        LevelRoot root;
        [SetUp] public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            root=new GameObject("Test level").AddComponent<LevelRoot>();
        }
        [TearDown] public void Cleanup()
        {
            foreach(var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)) surface.RemoveData();
            Undo.ClearAll();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
        LevelArea Area(AreaKind kind, float y=0)
        {
            var go=new GameObject("Area"); go.transform.SetParent(root.transform); go.transform.position=new Vector3(0,y,0);
            var area=go.AddComponent<LevelArea>(); area.kind=kind; return area;
        }
        [Test] public void BuildAreaDoesNotLeakToAnotherFloor()
        {
            Area(AreaKind.Buildable,3);
            Assert.That(root.IsPointInBuildableZone(new Vector3(0,3,0)),Is.True);
            Assert.That(root.IsPointInBuildableZone(Vector3.zero),Is.False);
        }
        [Test] public void NoBuildWinsRegardlessOfHierarchyOrder()
        {
            var forbidden=Area(AreaKind.NoBuild); Area(AreaKind.Buildable);
            Assert.That(root.IsPointInBuildableZone(Vector3.zero),Is.False);
            forbidden.transform.SetAsLastSibling();
            Assert.That(root.IsPointInBuildableZone(Vector3.zero),Is.False);
        }
        [Test] public void DisabledBuildAreaDoesNotAuthorizePlacement()
        { var area=Area(AreaKind.Buildable); area.enabled=false; Assert.That(root.IsPointInBuildableZone(Vector3.zero),Is.False); }
        [Test] public void ConcaveAreaKeepsItsNotchOutside()
        {
            var area=Area(AreaKind.Buildable);
            area.vertices=new List<Vector2>{new Vector2(0,0),new Vector2(4,0),new Vector2(4,1),new Vector2(1,1),new Vector2(1,4),new Vector2(0,4)};
            Assert.That(area.Contains(new Vector3(.5f,0,3)),Is.True);
            Assert.That(area.Contains(new Vector3(3,0,3)),Is.False);
            Assert.That(area.Contains(new Vector3(4,0,.5f)),Is.True);
        }
        [Test] public void PolygonIntersectionIsDetected()
        { Assert.That(LevelValidation.SelfIntersects(new[]{new Vector2(0,0),new Vector2(3,3),new Vector2(0,3),new Vector2(3,0)}),Is.True); }
        [Test] public void RotatedAreaUsesLocalSpace()
        {
            var area=Area(AreaKind.Buildable); area.transform.SetPositionAndRotation(new Vector3(12,3,4),Quaternion.Euler(0,35,0));
            Assert.That(area.Contains(area.transform.TransformPoint(new Vector3(2,0,2))),Is.True);
            Assert.That(area.Contains(area.transform.TransformPoint(new Vector3(4,0,0))),Is.False);
        }
        [Test] public void DuplicateMarkerIdIsAnError()
        {
            var a=LevelAuthoring.AddMarker(root,MarkerKind.Core,Vector3.zero);
            var b=LevelAuthoring.AddMarker(root,MarkerKind.PlayerStart,Vector3.right*5); b.id=a.id;
            Assert.That(LevelValidation.Check(root,false).Any(i=>i.severity==IssueSeverity.Error && i.message.Contains("중복")),Is.True);
        }
        [Test] public void WaveCannotReferenceMissingRoute()
        {
            root.waves=ScriptableObject.CreateInstance<WaveSet>();
            root.waves.waves.Add(new Wave { groups=new List<SpawnGroup>{new SpawnGroup { routeId="missing" }} });
            Assert.That(LevelValidation.Check(root,false).Any(i=>i.message.Contains("존재하지 않는 경로")),Is.True);
            Object.DestroyImmediate(root.waves);
        }
        [Test] public void LayoutRoundTripPreservesIdsReferencesAndGeometry()
        {
            var ground=new GameObject("Keep geometry"); ground.transform.SetParent(root.transform);
            root.transform.position=new Vector3(12,0,-4);
            var spawn=LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,new Vector3(10,2,20));
            var core=LevelAuthoring.AddMarker(root,MarkerKind.Core,new Vector3(5,2,-12));
            var route=LevelAuthoring.AddRoute(root,spawn,core); route.waypoints.Add(new Vector3(1,2,3));
            var saved=LevelAuthoring.Capture(root); string id=spawn.id;
            LevelAuthoring.Restore(root,saved);
            Assert.That(ground,Is.Not.Null);
            Assert.That(root.Markers.Single(m=>m.id==id).transform.position,Is.EqualTo(new Vector3(10,2,20)));
            Assert.That(root.Routes[0].spawn.id,Is.EqualTo(id));
            Assert.That(root.Routes[0].core.kind,Is.EqualTo(MarkerKind.Core));
            Assert.That(root.Routes[0].waypoints[0],Is.EqualTo(new Vector3(1,2,3)));
            Object.DestroyImmediate(saved);
        }
        [Test] public void InvalidLayoutDoesNotEraseExistingMarkers()
        {
            LevelAuthoring.AddMarker(root,MarkerKind.Core,Vector3.zero);
            var saved=ScriptableObject.CreateInstance<LevelLayout>(); saved.markers.Add(new MarkerRecord { id="same" }); saved.markers.Add(new MarkerRecord { id="same" });
            Assert.Throws<System.ArgumentException>(()=>LevelAuthoring.Restore(root,saved));
            Assert.That(root.Markers.Length,Is.EqualTo(1)); Object.DestroyImmediate(saved);
        }
        [Test] public void PathRespectsSolidGeometry()
        {
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.SetParent(root.transform); ground.transform.position=new Vector3(0,-.5f,0); ground.transform.localScale=new Vector3(20,1,20);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(root.transform); wall.transform.position=new Vector3(0,2,0); wall.transform.localScale=new Vector3(2,4,12);
            Physics.SyncTransforms();
            var surface=root.gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects=CollectObjects.Children; surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
            var a=LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,new Vector3(-5,0,0));
            var b=LevelAuthoring.AddMarker(root,MarkerKind.Core,new Vector3(5,0,0));
            var route=LevelAuthoring.AddRoute(root,a,b);
            Assert.That(LevelValidation.TryPath(route,out var corners,out var failure),Is.True,failure);
            Assert.That(corners.Count,Is.GreaterThan(2));
            Assert.That(corners.Any(p=>Mathf.Abs(p.z)>5.5f),Is.True);
        }
        [Test] public void DisconnectedGroundFailsNavigation()
        {
            foreach(float x in new[]{-8f,8f}) { var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.SetParent(root.transform); ground.transform.position=new Vector3(x,-.5f,0); ground.transform.localScale=new Vector3(6,1,6); }
            Physics.SyncTransforms(); var surface=root.gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects=CollectObjects.Children; surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
            var route=LevelAuthoring.AddRoute(root,LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,new Vector3(-8,0,0)),LevelAuthoring.AddMarker(root,MarkerKind.Core,new Vector3(8,0,0)));
            Assert.That(LevelValidation.TryPath(route,out _,out _),Is.False);
        }

        [Test] public void SpawnBindingDoesNotRequireAnyRouteOrNavigator()
        {
            var spawn=LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,Vector3.zero);
            var core=LevelAuthoring.AddMarker(root,MarkerKind.Core,Vector3.forward*8);
            var group=new SpawnGroup { spawnId=spawn.id,targetId=core.id };
            Assert.That(root.TryResolveSpawnGroup(group,out var binding,out var error),Is.True,error);
            Assert.That(binding.Spawn,Is.SameAs(spawn)); Assert.That(binding.Target,Is.SameAs(core));
            Assert.That(binding.SuggestedRoute,Is.Null);
            Assert.That(root.GetComponentsInChildren<NavMeshAgent>(),Is.Empty);
        }
        [Test] public void LegacyWaveResolvesUnchangedRouteId()
        {
            var route=LevelAuthoring.AddRoute(root,LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,Vector3.zero),LevelAuthoring.AddMarker(root,MarkerKind.Core,Vector3.forward*8));
            var group=new SpawnGroup { routeId=route.id };
            Assert.That(root.TryResolveSpawnGroup(group,out var binding,out var error),Is.True,error);
            Assert.That(binding.Spawn,Is.SameAs(route.spawn)); Assert.That(binding.Target,Is.SameAs(route.core));
            Assert.That(group.spawnId,Is.Null); Assert.That(group.targetId,Is.Null);
        }
        [Test] public void ConflictingGuideAndExplicitSpawnIsRejected()
        {
            var a=LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,Vector3.zero);
            var b=LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,Vector3.right*8);
            var core=LevelAuthoring.AddMarker(root,MarkerKind.Core,Vector3.forward*8);
            var route=LevelAuthoring.AddRoute(root,a,core);
            Assert.That(root.TryResolveSpawnGroup(new SpawnGroup { spawnId=b.id,targetId=core.id,routeId=route.id },out _,out _),Is.False);
        }
        [Test] public void MissingExplicitSpawnDoesNotSilentlyFallBackToGuide()
        {
            var route=LevelAuthoring.AddRoute(root,LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,Vector3.zero),LevelAuthoring.AddMarker(root,MarkerKind.Core,Vector3.forward*8));
            Assert.That(root.TryResolveSpawnGroup(new SpawnGroup { spawnId="deleted",routeId=route.id },out _,out _),Is.False);
        }
        [Test] public void BuildZoneQueryDoesNotPretendToValidateOccupancy()
        {
            Area(AreaKind.Buildable);
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube); block.transform.SetParent(root.transform);
            Assert.That(root.IsPointInBuildableZone(Vector3.zero),Is.True,"Occupancy belongs to the game installation system.");
        }
        [Test] public void RuntimeAssemblyHasNoEditorInputOrMovementDependencies()
        {
            var references=typeof(LevelRoot).Assembly.GetReferencedAssemblies().Select(a=>a.Name).ToArray();
            Assert.That(references.Any(n=>n.StartsWith("UnityEditor") || n.Contains("InputSystem") || n.Contains("Diagnostics") || n.Contains("AIModule")),Is.False);
            Assert.That(typeof(LevelElementDefinition).GetField("moveSpeed"),Is.Null);
            Assert.That(typeof(LevelElementDefinition).GetField("previewRange"),Is.Not.Null);
        }
        [Test] public void GameBuildDoesNotCompileOptionalDiagnostics()
        {
            var playerAssemblies=UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player);
            Assert.That(playerAssemblies.Any(a=>a.name.StartsWith("DesertTower.Levels.Diagnostics")),Is.False);
            Assert.That(playerAssemblies.Any(a=>a.name=="DesertTower.Levels.Runtime"),Is.True);
        }
        [Test] public void OldMarkerRangeLoadsAsPreviewMetadata()
        {
            var marker=LevelAuthoring.AddMarker(root,MarkerKind.InitialFacility,Vector3.zero);
            marker.previewRange=17;
            string path=AssetDatabase.GenerateUniqueAssetPath("Assets/DesertTowerMigrationFixture.prefab");
            try
            {
                PrefabUtility.SaveAsPrefabAsset(marker.gameObject,path);
                File.WriteAllText(path,File.ReadAllText(path).Replace("previewRange:","range:"));
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LevelMarker>().previewRange,Is.EqualTo(17));
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }
        [Test] public void LegacyElementGuidAndPreviewFieldsSurviveRename()
        {
            string path=AssetDatabase.GUIDToAssetPath("e7ce5938058aa4245824ee870241d2fb");
            Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(path).GetClass(),Is.EqualTo(typeof(LevelElementDefinition)));
            var element=ScriptableObject.CreateInstance<LevelElementDefinition>();
            string fixture=AssetDatabase.GenerateUniqueAssetPath("Assets/DesertTowerElementMigrationFixture.asset");
            try
            {
                element.displayName="Legacy"; element.previewRange=13;
                AssetDatabase.CreateAsset(element,fixture); AssetDatabase.SaveAssets();
                string legacy=File.ReadAllText(fixture).Replace("previewRange:","range:").Replace("::DesertTower.Levels.LevelElementDefinition","::DesertTower.Levels.ActorDefinition");
                File.WriteAllText(fixture,legacy); AssetDatabase.ImportAsset(fixture,ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var loaded=AssetDatabase.LoadAssetAtPath<LevelElementDefinition>(fixture);
                Assert.That(loaded.displayName,Is.EqualTo("Legacy")); Assert.That(loaded.previewRange,Is.EqualTo(13));
            }
            finally { AssetDatabase.DeleteAsset(fixture); }
        }

        [Test] public void LegacyWaveActorReferenceSurvivesAssetImport()
        {
            string elementPath=AssetDatabase.GenerateUniqueAssetPath("Assets/DesertTowerWaveElementFixture.asset");
            string wavePath=AssetDatabase.GenerateUniqueAssetPath("Assets/DesertTowerWaveMigrationFixture.asset");
            try
            {
                var element=ScriptableObject.CreateInstance<LevelElementDefinition>(); element.gameKey="raider";
                AssetDatabase.CreateAsset(element,elementPath);
                var waves=ScriptableObject.CreateInstance<WaveSet>(); waves.waves.Add(new Wave { groups=new List<SpawnGroup>{new SpawnGroup { element=element,routeId="legacy-route" }} });
                AssetDatabase.CreateAsset(waves,wavePath); AssetDatabase.SaveAssets();
                File.WriteAllText(wavePath,File.ReadAllText(wavePath).Replace("element:","actor:"));
                AssetDatabase.ImportAsset(wavePath,ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var loaded=AssetDatabase.LoadAssetAtPath<WaveSet>(wavePath).waves[0].groups[0];
                Assert.That(loaded.element.gameKey,Is.EqualTo("raider")); Assert.That(loaded.routeId,Is.EqualTo("legacy-route"));
            }
            finally { AssetDatabase.DeleteAsset(wavePath); AssetDatabase.DeleteAsset(elementPath); }
        }
    }
}
