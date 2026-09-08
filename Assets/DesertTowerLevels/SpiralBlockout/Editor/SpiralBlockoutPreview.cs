using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DesertTower.Levels.Editor
{
    /// <summary>Imports the authored Blender blockout into a self-contained inspection scene.</summary>
    public static class SpiralBlockoutPreview
    {
        const string Root = "Assets/DesertTowerLevels/SpiralBlockout";
        const string ModelPath = Root + "/Models/SandGuard_Spiral_Blockout.fbx";
        const string ScenePath = Root + "/Scenes/SandGuard_Spiral_Blockout.unity";
        const string PrefabPath = Root + "/Prefabs/SandGuard_Spiral_Blockout.prefab";
        const string PreviewPath = "Docs/LevelArt/Blender/Previews/SandGuard_Unity_Preview.png";

        [Serializable] class Mobility { public string name; public Vector3 takeoff; public Vector3 landing; }
        [Serializable] class Layout { public Vector3[] enemyRoute; public Mobility[] mobility; }
        [Serializable] class Report
        {
            public string scene, prefab, unityVersion, coordinateMapping;
            public int meshCount, buildSlots, routeNodes, missingMaterials;
            public Vector3 modelSize;
            public float coordinateError;
            public bool sceneReopened, previewRendered;
        }

        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            {"Sandstone_Support", new Color(.58f,.43f,.28f)},
            {"Road_Lower", new Color(.76f,.60f,.39f)},
            {"Road_Middle", new Color(.83f,.68f,.47f)},
            {"Road_Upper", new Color(.90f,.77f,.57f)},
            {"Connecting_Ramps", new Color(.75f,.49f,.25f)},
            {"Summit_Floor", new Color(.94f,.82f,.64f)},
            {"Dark_Stone", new Color(.25f,.27f,.28f)},
            {"Sand_Foundation", new Color(.55f,.45f,.32f)},
            {"Build_Slot_Markers", new Color(.19f,.28f,.29f)},
            {"Core_Crystal", new Color(.025f,.75f,.84f)}
        };

        [MenuItem("DesertTower/Levels/Open Spiral Blockout")]
        public static void OpenPreview()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = Object.FindFirstObjectByType<Camera>();
            var model = GameObject.Find("SandGuard Spiral Blockout");
            Selection.activeGameObject = model;
            var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            if (camera != null)
            {
                view.LookAt(camera.transform.position + camera.transform.forward * 95f,
                    camera.transform.rotation, 70f, true, true);
                if (model != null) view.FrameSelected();
            }
            view.Focus();
            view.Repaint();
            Debug.Log("SPIRAL_PREVIEW_OPENED " + ScenePath);
        }

        [MenuItem("DesertTower/Levels/Rebuild Spiral Blockout Preview")]
        static void BuildFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildAndRender();
        }

        // Explicit batch entry point; never runs automatically on project import.
        public static void BuildAndRender()
        {
            try { Build(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static Material MakeMaterial(string name, Color color, bool unlit = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Required URP shader is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) {name = name};
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
            if (name == "Core_Crystal")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .5f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Build()
        {
            foreach (string sub in new[]{"Materials","Scenes","Prefabs"}) Directory.CreateDirectory(Root+"/"+sub);
            Directory.CreateDirectory("Logs/SpiralBlockout");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var mats = Palette.ToDictionary(p=>p.Key,p=>MakeMaterial(p.Key,p.Value));
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("FBX could not be imported.");
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var pair in mats)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
            importer.SaveAndReimport();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (asset == null) throw new InvalidOperationException("Imported model asset is missing.");
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Root+"/Data/SpiralPreviewLayout.json").TrimStart('\uFEFF'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = "SandGuard Spiral Blockout";
            // Preserve the nested FBX prefab while adding usable collision and URP materials.
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                var assigned = renderer.sharedMaterials;
                for (int i=0;i<assigned.Length;i++)
                {
                    string name = assigned[i] ? assigned[i].name : "";
                    if (!mats.TryGetValue(name,out var mat))
                        throw new InvalidOperationException("Unknown imported material: "+name);
                    assigned[i]=mat;
                }
                renderer.sharedMaterials=assigned;
                renderer.gameObject.isStatic=true;
                if (!renderer.name.StartsWith("Slot_"))
                {
                    var collider=renderer.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                }
            }
            var transforms=model.GetComponentsInChildren<Transform>();
            var nodes=transforms.Where(t=>t.name.StartsWith("EnemyWaypoint_")).OrderBy(t=>t.name,StringComparer.Ordinal).ToArray();
            int slots=transforms.Count(t=>t.name.StartsWith("BuildSlot_"));
            if (slots!=8 || nodes.Length!=21 || layout.enemyRoute.Length!=21)
                throw new InvalidOperationException($"Unexpected layout: slots={slots}, route={nodes.Length}");

            // FBX handedness conversion may mirror an axis. Derive it from every authored
            // waypoint before placing guides, rather than guessing Unity's importer axes.
            Func<Vector3,Vector3> map=null;
            string mapping="";
            float best=float.MaxValue;
            foreach (bool swap in new[]{false,true}) foreach(int sx in new[]{1,-1}) foreach(int sz in new[]{1,-1})
            {
                Func<Vector3,Vector3> candidate=p=>swap ? new Vector3(sx*p.z,p.y,sz*p.x) : new Vector3(sx*p.x,p.y,sz*p.z);
                float error=0;
                for(int i=0;i<nodes.Length;i++) error=Mathf.Max(error,Vector3.Distance(candidate(layout.enemyRoute[i]),nodes[i].position));
                if(error<best) {best=error;map=candidate;mapping=$"swapXZ={swap}, signX={sx}, signZ={sz}";}
            }
            if(best>.02f) throw new InvalidOperationException("FBX coordinate/scale mismatch: "+best+" m");
            var renderers=model.GetComponentsInChildren<MeshRenderer>();
            Bounds bounds=renderers[0].bounds;
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            if(Mathf.Abs(bounds.size.y-12.7f)>.05f)
                throw new InvalidOperationException("Unexpected imported height: "+bounds.size);
            PrefabUtility.SaveAsPrefabAsset(model,PrefabPath);

            var guides=new GameObject("Inspection Guides - orange enemy route, cyan vertical movement");
            var orange=MakeMaterial("Guide_Enemy",new Color(1f,.43f,.035f),true);
            var cyan=MakeMaterial("Guide_Player",new Color(.025f,.7f,.83f),true);
            Line("Enemy route to core",nodes.Select(t=>t.position+Vector3.up*.08f).ToArray(),orange,.085f,guides.transform);
            foreach(var jump in layout.mobility)
            {
                Vector3 a=map(jump.takeoff),b=map(jump.landing);
                var arc=new Vector3[25];
                for(int i=0;i<arc.Length;i++)
                {
                    float t=i/(float)(arc.Length-1);
                    arc[i]=Vector3.Lerp(a,b,t)+Vector3.up*(.06f+1.35f*Mathf.Sin(t*Mathf.PI));
                }
                Line(jump.name,arc,cyan,.045f,guides.transform);
                foreach(var p in new[]{jump.takeoff,jump.landing})
                {
                    var outline=new[]{new Vector3(-.65f,0,-.5f),new Vector3(.65f,0,-.5f),new Vector3(.65f,0,.5f),new Vector3(-.65f,0,.5f),new Vector3(-.65f,0,-.5f)};
                    Line(jump.name+" landing",outline.Select(v=>map(p+v)+Vector3.up*.035f).ToArray(),cyan,.035f,guides.transform);
                }
            }
            var scale=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            scale.name="Human scale reference - 1.8m";
            scale.transform.position=Vector3.Lerp(nodes[1].position,nodes[2].position,.45f)+Vector3.up*.9f;
            scale.transform.localScale=new Vector3(.45f,.9f,.45f);
            Object.DestroyImmediate(scale.GetComponent<Collider>());
            scale.GetComponent<Renderer>().sharedMaterial=mats["Dark_Stone"];

            RenderSettings.skybox=null;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.7f,.74f,.8f);
            RenderSettings.ambientEquatorColor=new Color(.46f,.44f,.4f);
            RenderSettings.ambientGroundColor=new Color(.25f,.23f,.20f);
            RenderSettings.fog=false;
            var sun=new GameObject("Sun").AddComponent<Light>();
            sun.type=LightType.Directional;
            sun.intensity=1.25f;
            sun.color=new Color(1,.94f,.83f);
            sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-35,0);
            var cam=new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag="MainCamera";
            cam.transform.position=map(new Vector3(-58,58,72));
            cam.transform.LookAt(map(new Vector3(-2,3.4f,0)));
            cam.orthographic=true;
            cam.orthographicSize=33f;
            cam.nearClipPlane=.1f;
            cam.farClipPlane=300;
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.19f,.22f,.26f);
            cam.gameObject.AddComponent<AudioListener>();
            cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            cam=Object.FindFirstObjectByType<Camera>();
            if(cam==null || GameObject.Find("SandGuard Spiral Blockout")==null)
                throw new InvalidOperationException("Saved preview scene could not be reopened.");
            RenderPreview(cam);
            var report=new Report
            {
                scene=ScenePath,prefab=PrefabPath,unityVersion=Application.unityVersion,
                coordinateMapping=mapping,meshCount=renderers.Length,buildSlots=slots,routeNodes=nodes.Length,
                coordinateError=best,modelSize=bounds.size,missingMaterials=0,sceneReopened=true,previewRendered=true
            };
            File.WriteAllText("Logs/SpiralBlockout/verification.json",JsonUtility.ToJson(report,true));
            Debug.Log("SPIRAL_PREVIEW_READY "+JsonUtility.ToJson(report));
        }

        static void Line(string name,Vector3[] points,Material material,float width,Transform parent)
        {
            var go=new GameObject(name);
            go.transform.SetParent(parent,false);
            var line=go.AddComponent<LineRenderer>();
            line.useWorldSpace=true;
            line.positionCount=points.Length;
            line.SetPositions(points);
            line.widthMultiplier=width;
            line.sharedMaterial=material;
            line.numCornerVertices=3;
            line.numCapVertices=3;
            line.shadowCastingMode=ShadowCastingMode.Off;
            line.receiveShadows=false;
        }

        static void RenderPreview(Camera camera)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewPath));
            var rt=new RenderTexture(1600,1000,24);
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;
                camera.Render();
                RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,1600,1000),0,0);
                image.Apply();
                File.WriteAllBytes(PreviewPath,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=null;
                RenderTexture.active=previous;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(image);
            }
        }
    }
}
