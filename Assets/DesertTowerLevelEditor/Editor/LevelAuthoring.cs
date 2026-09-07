using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

namespace DesertTower.Levels.Editor
{
    public static class LevelAuthoring
    {
        public const string GeneratedFolder = "Assets/DesertTowerLevels";
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        public static GameObject Child(LevelRoot root, string name)
        {
            var go = new GameObject(name);
            if (root) { SceneManager.MoveGameObjectToScene(go, root.gameObject.scene); go.transform.SetParent(root.transform, false); }
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        public static LevelRoot NewRoot(string name = "Desert Tower Level")
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create level");
            var root = Undo.AddComponent<LevelRoot>(go);
            root.levelName = name;
            Selection.activeGameObject = go;
            return root;
        }

        public static LevelMarker AddMarker(LevelRoot root, MarkerKind kind, Vector3 position, LevelElementDefinition definition = null)
        {
            var go = Child(root, kind.ToString());
            var marker = Undo.AddComponent<LevelMarker>(go);
            marker.kind = kind;
            marker.label = kind + " " + (root.Markers.Count(m => m.kind == kind));
            marker.definition = definition;
            marker.transform.position = position;
            marker.footprint = kind == MarkerKind.Core ? new Vector3(3,3,3) :
                kind == MarkerKind.PlayerStart || kind == MarkerKind.Respawn ? new Vector3(.8f,1.8f,.8f) : new Vector3(2,2,2);
            marker.gameObject.name = marker.label;
            Dirty(root);
            return marker;
        }

        public static LevelBuildSlot AddBuildSlot(LevelRoot root, Vector3 position)
        {
            var slot = Undo.AddComponent<LevelBuildSlot>(Child(root, "건설 슬롯"));
            slot.id = Guid.NewGuid().ToString("N");
            slot.transform.position = position;
            Dirty(root);
            return slot;
        }

        public static LevelArea AddArea(LevelRoot root, AreaKind kind, IList<Vector3> points)
        {
            var go = Child(root, kind.ToString());
            var area = Undo.AddComponent<LevelArea>(go);
            area.kind = kind;
            area.label = kind.ToString();
            go.transform.position = points[0];
            area.vertices.Clear();
            foreach (var p in points)
            {
                var local = go.transform.InverseTransformPoint(p);
                area.vertices.Add(new Vector2(local.x, local.z));
            }
            Dirty(root);
            return area;
        }

        public static LevelRoute AddRoute(LevelRoot root, LevelMarker spawn, LevelMarker core)
        {
            var route = Undo.AddComponent<LevelRoute>(Child(root, "Route"));
            route.spawn = spawn; route.core = core;
            route.label = spawn ? spawn.label + " → Core" : "Route";
            route.name = route.label;
            Dirty(root);
            return route;
        }

        public static void Dirty(LevelRoot root)
        {
            if (!root) return;
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            SceneView.RepaintAll();
        }

        public static Material Material(string name, Color color)
        {
            EnsureFolder(GeneratedFolder + "/Materials");
            string path = GeneratedFolder + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", .12f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        public static GameObject Block(LevelRoot root, string name, Vector3 center, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, root.gameObject.scene);
            go.name = name; go.transform.SetParent(root.transform, false);
            go.transform.position = center; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(go, "Create block");
            Dirty(root);
            return go;
        }

        public static GameObject Road(LevelRoot root, IList<Vector3> points, float width, Material material)
        {
            if (points.Count < 2 || width <= 0) throw new ArgumentException("A road needs two points and a positive width.");
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            float distance = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0) distance += Vector3.Distance(points[i-1], points[i]);
                Vector3 tangent = points[Mathf.Min(i+1, points.Count-1)] - points[Mathf.Max(0, i-1)];
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized * width * .5f;
                if (side.sqrMagnitude < .001f) throw new ArgumentException("Road points must have horizontal separation.");
                vertices.Add(points[i] - side); vertices.Add(points[i] + side);
                uv.Add(new Vector2(0, distance*.25f)); uv.Add(new Vector2(width*.25f, distance*.25f));
                if (i > 0)
                {
                    int n = i*2;
                    triangles.AddRange(new[] { n-2, n, n-1, n-1, n, n+1 });
                }
            }
            var mesh = new Mesh { name = "Road Surface" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EnsureFolder(GeneratedFolder + "/Meshes");
            AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/Meshes/Road.asset"));
            var go = Child(root, "Road / Ramp");
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); go.transform.localScale = Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            Dirty(root); return go;
        }

        public static Terrain NewTerrain(LevelRoot root)
        {
            EnsureFolder(GeneratedFolder + "/Terrain");
            var data = new TerrainData { heightmapResolution = 257, size = new Vector3(96,24,96) };
            AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/Terrain/Terrain.asset"));
            var go = Terrain.CreateTerrainGameObject(data);
            SceneManager.MoveGameObjectToScene(go, root.gameObject.scene);
            go.name = "Landscape"; go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(-48,0,-48);
            Undo.RegisterCreatedObjectUndo(go, "Create terrain");
            Dirty(root); return go.GetComponent<Terrain>();
        }

        public static NavMeshSurface Bake(LevelRoot root)
        {
            var surface = root.GetComponent<NavMeshSurface>();
            if (!surface) surface = Undo.AddComponent<NavMeshSurface>(root.gameObject);
            Undo.RecordObject(surface, "Bake navigation");
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .15f;
            surface.BuildNavMesh();
            if (surface.navMeshData && !AssetDatabase.Contains(surface.navMeshData))
            {
                EnsureFolder(GeneratedFolder + "/Navigation");
                AssetDatabase.CreateAsset(surface.navMeshData, AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/Navigation/Navigation.asset"));
            }
            root.lastBakeUtc = DateTime.UtcNow.ToString("u");
            Dirty(root); AssetDatabase.SaveAssets(); return surface;
        }

        public static LevelLayout Capture(LevelRoot root)
        {
            var layout = ScriptableObject.CreateInstance<LevelLayout>();
            layout.schemaVersion = 2;
            layout.levelName = root.levelName; layout.gridSize = root.gridSize; layout.waves = root.waves;
            foreach (var slot in root.BuildSlots)
                layout.buildSlots.Add(new BuildSlotRecord {
                    id = slot.id, label = slot.label, occupancySurfaceId = slot.occupancySurfaceId,
                    position = root.transform.InverseTransformPoint(slot.transform.position),
                    rotation = (Quaternion.Inverse(root.transform.rotation) * slot.transform.rotation).eulerAngles,
                    footprint = slot.footprint, available = slot.available && slot.isActiveAndEnabled,
                    allowedFacilityIds = new List<string>(slot.allowedFacilityIds ?? new List<string>())
                });
            foreach (var m in root.Markers)
                layout.markers.Add(new MarkerRecord { id=m.id, label=m.label, kind=m.kind, position=root.transform.InverseTransformPoint(m.transform.position), rotation=(Quaternion.Inverse(root.transform.rotation)*m.transform.rotation).eulerAngles, footprint=m.footprint, previewRange=m.previewRange, spawnRadius=m.spawnRadius, definition=m.definition });
            foreach (var a in root.Areas)
                layout.areas.Add(new AreaRecord { id=a.id, label=a.label, kind=a.kind, position=root.transform.InverseTransformPoint(a.transform.position), rotation=(Quaternion.Inverse(root.transform.rotation)*a.transform.rotation).eulerAngles, height=a.height, vertices=new List<Vector2>(a.vertices) });
            foreach (var r in root.Routes)
                layout.routes.Add(new RouteRecord { id=r.id, label=r.label, spawnId=r.spawn ? r.spawn.id : null, coreId=r.core ? r.core.id : null, points=r.waypoints.Select(p=>root.transform.InverseTransformPoint(r.transform.TransformPoint(p))).ToList() });
            return layout;
        }

        public static void Restore(LevelRoot root, LevelLayout layout)
        {
            if (!layout || (layout.schemaVersion != 1 && layout.schemaVersion != 2)) throw new ArgumentException("Unsupported layout version.");
            var ids = new HashSet<string>();
            foreach (var m in layout.markers)
                if (string.IsNullOrEmpty(m.id) || !ids.Add(m.id)) throw new ArgumentException("Layout has duplicate or empty marker IDs.");
            // 잘못된 슬롯을 불러오다가 기존 레벨을 지우지 않도록 먼저 확인한다.
            var slots = layout.buildSlots ?? new List<BuildSlotRecord>();
            foreach (var slot in slots)
            {
                if (slot == null || !LevelBuildSlot.ValidData(slot.id, slot.occupancySurfaceId,
                    slot.footprint, slot.allowedFacilityIds, out _) || !ids.Add(slot.id)
                    || !LevelBuildSlot.Finite(slot.position) || !LevelBuildSlot.Finite(slot.rotation))
                    throw new ArgumentException("건설 슬롯의 ID, 바닥, 허용 시설, 위치와 크기를 확인하세요.");
            }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Restore layout");
            foreach (var slot in root.BuildSlots) Undo.DestroyObjectImmediate(slot.gameObject);
            foreach (var r in root.Routes) Undo.DestroyObjectImmediate(r.gameObject);
            foreach (var a in root.Areas) Undo.DestroyObjectImmediate(a.gameObject);
            foreach (var m in root.Markers) Undo.DestroyObjectImmediate(m.gameObject);
            Undo.RecordObject(root, "Restore level settings");
            root.levelName=layout.levelName; root.gridSize=layout.gridSize; root.waves=layout.waves;
            var markers = new Dictionary<string, LevelMarker>();
            foreach (var m in layout.markers)
            {
                var marker = AddMarker(root, m.kind, root.transform.TransformPoint(m.position), m.definition);
                marker.id=m.id; marker.label=m.label; marker.name=m.label; marker.footprint=m.footprint;
                marker.previewRange=m.previewRange; marker.spawnRadius=m.spawnRadius;
                marker.transform.rotation=root.transform.rotation*Quaternion.Euler(m.rotation); markers.Add(m.id, marker);
            }
            foreach (var saved in slots)
            {
                var slot = AddBuildSlot(root, root.transform.TransformPoint(saved.position));
                slot.id = saved.id; slot.label = saved.label; slot.name = saved.label;
                slot.occupancySurfaceId = saved.occupancySurfaceId;
                slot.footprint = saved.footprint; slot.available = saved.available;
                slot.allowedFacilityIds = new List<string>(saved.allowedFacilityIds);
                slot.transform.rotation = root.transform.rotation * Quaternion.Euler(saved.rotation);
            }
            foreach (var a in layout.areas)
            {
                var area=Undo.AddComponent<LevelArea>(Child(root, a.label));
                area.id=a.id; area.label=a.label; area.kind=a.kind; area.height=a.height; area.vertices=new List<Vector2>(a.vertices);
                area.transform.position=root.transform.TransformPoint(a.position); area.transform.rotation=root.transform.rotation*Quaternion.Euler(a.rotation);
            }
            foreach (var r in layout.routes)
            {
                markers.TryGetValue(r.spawnId ?? "", out var spawn); markers.TryGetValue(r.coreId ?? "", out var core);
                var route=AddRoute(root, spawn, core); route.id=r.id; route.label=r.label; route.name=r.label;
                route.waypoints=r.points.Select(p=>route.transform.InverseTransformPoint(root.transform.TransformPoint(p))).ToList();
            }
            Dirty(root); Undo.CollapseUndoOperations(group);
        }
    }
}
