using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DesertTower.Levels.Editor
{
    public static class HiddenCanyonBuilder
    {
        const string Folder="Assets/DesertTowerLevels/HiddenCanyon";
        const string ScenePath=Folder+"/Scenes/HiddenPyramidCanyon.unity";
        const string Model="Assets/DesertTowerLevels/SpiralBlockout/Prefabs/SandGuard_Spiral_Blockout.prefab";
        const string Images="Docs/LevelArt/HiddenCanyon";
        static readonly Vector3[] Approach={new Vector3(53,0,83),new Vector3(49,0,65),
            new Vector3(46,0,53),new Vector3(39,0,43),new Vector3(34,0,33),new Vector3(29,0,24)};
        static readonly Vector3 Reveal=new Vector3(25,5.8f,31);
        static readonly Vector3 Arrival=new Vector3(49,3.2f,65);
        static Material[] stone;
        static Material sand, sandLight, sandDark, crystal, dark;
        static Mesh[] cliffMeshes;
        static System.Random Random=new System.Random(38201);
        static float Range(float a,float b)=>a+(b-a)*(float)Random.NextDouble();

        [MenuItem("Tools/Desert Tower/Open Hidden Pyramid Canyon",false,2)]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var root=Object.FindFirstObjectByType<LevelRoot>();
            Selection.activeGameObject=root.gameObject;
            LevelEditorWindow.Open();
            var view=SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            view.LookAt(new Vector3(5,8,10),Quaternion.Euler(36,215,0),110,true,true);
            view.Focus();view.Repaint();
            Debug.Log("HIDDEN_CANYON_OPENED "+ScenePath);
        }

        [MenuItem("Tools/Desert Tower/Rebuild Hidden Pyramid Canyon",false,3)]
        static void BuildFromMenu()
        {if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildAndRender();}

        public static void BuildAndRender()
        {
            try { Build(); }
            catch(Exception e) {Debug.LogException(e);if(Application.isBatchMode) EditorApplication.Exit(1);else throw;}
        }

        static Material Mat(string name,Color color)
        {
            string path=Folder+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.06f);
            EditorUtility.SetDirty(m);return m;
        }

        static T Persist<T>(T asset,string path) where T:Object
        {
            var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old) {EditorUtility.CopySerialized(asset,old);Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(asset,path);return asset;
        }

        static GameObject Group(string name,Transform parent)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}

        static Mesh CliffMesh(int variant)
        {
            var rng=new System.Random(800+variant);
            const int sides=10;
            float[] ys={0,.065f,.255f,.275f,.47f,.49f,.70f,.725f,.93f,1};
            float[] radii={1.04f,1.07f,.97f,1.01f,.85f,.90f,.75f,.80f,.62f,.57f};
            var points=new Vector3[ys.Length,sides];
            var wobble=Enumerable.Range(0,sides).Select(_=>(float)rng.NextDouble()*.16f+.92f).ToArray();
            for(int row=0;row<ys.Length;row++) for(int i=0;i<sides;i++)
            {
                float angle=(i/(float)sides)*Mathf.PI*2;
                float x=Mathf.Sign(Mathf.Cos(angle))*Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)),.63f);
                float z=Mathf.Sign(Mathf.Sin(angle))*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle)),.63f);
                float skew=(float)Math.Sin(row*.8+variant)*.08f;
                float y=ys[row]+(row==0?0:Mathf.Sin(i*2.6f+variant)*.014f);
                points[row,i]=new Vector3(x*radii[row]*wobble[i]+skew,y,z*radii[row]*wobble[i]);
            }
            var vertices=new List<Vector3>();var uv=new List<Vector2>();
            var triangles=Enumerable.Range(0,5).Select(_=>new List<int>()).ToArray();
            void Triangle(Vector3 a,Vector3 b,Vector3 c,int material)
            {
                int n=vertices.Count;vertices.AddRange(new[]{a,b,c});
                uv.AddRange(new[]{new Vector2(a.x,a.y),new Vector2(b.x,b.y),new Vector2(c.x,c.y)});
                triangles[material].AddRange(new[]{n,n+1,n+2});
            }
            for(int row=0;row<ys.Length-1;row++) for(int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;
                int mat=row==1||row==3||row==5?2:(row%3==0?1:0);
                Triangle(points[row,i],points[row+1,i],points[row+1,j],mat);
                Triangle(points[row,i],points[row+1,j],points[row,j],mat);
            }
            for(int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;
                Triangle(Vector3.up,points[ys.Length-1,j],points[ys.Length-1,i],3);
                Triangle(Vector3.zero,points[0,i],points[0,j],4);
            }
            var mesh=new Mesh{name="Layered Sandstone "+variant};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=5;
            for(int i=0;i<5;i++)mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            return Persist(mesh,Folder+"/Meshes/Cliff_"+variant+".asset");
        }

        static GameObject Rock(string name,Vector3 position,Vector3 size,float yaw,int variant,Transform parent,bool collision=true)
        {
            var go=Group(name,parent);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            go.transform.localScale=new Vector3(size.x*.5f,size.y,size.z*.5f);
            go.AddComponent<MeshFilter>().sharedMesh=cliffMeshes[variant%cliffMeshes.Length];
            go.AddComponent<MeshRenderer>().sharedMaterials=stone;
            if(collision) go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
            go.isStatic=true;return go;
        }

        static float PathDistance(Vector3 p)
        {
            float distance=1000;
            for(int i=0;i<Approach.Length-1;i++)
            {
                Vector3 a=Approach[i],b=Approach[i+1],d=b-a;
                var q=a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);
                distance=Mathf.Min(distance,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(q.x,q.z)));
            }
            return distance;
        }

        static float GroundHeight(float x,float z)
        {
            float basin=Mathf.SmoothStep(0,1,Mathf.InverseLerp(29,60,new Vector2(x,z).magnitude));
            float channel=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,15,PathDistance(new Vector3(x,0,z))));
            return basin*channel*(.4f+2.0f*Mathf.PerlinNoise(x*.035f+80,z*.028f+24));
        }

        static void Ground(Transform parent)
        {
            const int n=56;var vertices=new List<Vector3>();var triangles=new[]{new List<int>(),new List<int>(),new List<int>()};
            var uv=new List<Vector2>();
            for(int j=0;j<=n;j++)for(int i=0;i<=n;i++)
            {
                float x=-95+190f*i/n,z=-90+200f*j/n;
                vertices.Add(new Vector3(x,GroundHeight(x,z),z));uv.Add(new Vector2(x*.05f,z*.05f));
            }
            for(int j=0;j<n;j++)for(int i=0;i<n;i++)
            {
                int a=j*(n+1)+i,b=a+1,c=a+n+1,d=c+1;
                var p=vertices[a];float noise=Mathf.PerlinNoise(p.x*.017f+10,p.z*.017f+4);
                int k=noise>.62f?1:noise<.39f?2:0;
                triangles[k].AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=new Mesh{name="Canyon sand basin"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=3;
            for(int k=0;k<3;k++)mesh.SetTriangles(triangles[k],k);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Persist(mesh,Folder+"/Meshes/CanyonGround.asset");
            var go=Group("Continuous sand floor",parent);go.isStatic=true;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=new[]{sand,sandLight,sandDark};
            go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }

        static LevelMarker Marker(LevelRoot root,string label,MarkerKind kind,Vector3 p)
        {var m=LevelAuthoring.AddMarker(root,kind,p);m.label=label;m.name=label;m.spawnRadius=1.4f;return m;}

        static Camera Camera(string name,Vector3 p,Vector3 target,bool ortho,float size)
        {
            var c=new GameObject(name).AddComponent<Camera>();c.transform.position=p;c.transform.LookAt(target);
            c.orthographic=ortho;c.orthographicSize=size;c.fieldOfView=66;c.nearClipPlane=.15f;c.farClipPlane=400;
            c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.53f,.66f,.74f);
            c.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            c.enabled=false;return c;
        }

        static void Render(Camera camera,string name)
        {
            var rt=new RenderTexture(1800,1100,24);var tex=new Texture2D(1800,1100,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                tex.ReadPixels(new Rect(0,0,1800,1100),0,0);tex.Apply();File.WriteAllBytes(Images+"/"+name+".png",tex.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
        }

        static bool CliffsBlock(Vector3 eye,Vector3 target,Transform walls)
        {Vector3 d=target-eye;return Physics.RaycastAll(eye,d.normalized,d.magnitude).Any(h=>h.transform.IsChildOf(walls));}

        static bool CoreVisible(Vector3 eye,Transform root)
        {
            Vector3 delta=new Vector3(0,11.75f,0)-eye;
            return !Physics.RaycastAll(eye,delta.normalized,delta.magnitude-.1f).Any(h=>
                h.transform.IsChildOf(root)&&!h.transform.name.StartsWith("Core_Crystal"));
        }

        static void Build()
        {
            Random=new System.Random(38201);
            foreach(string sub in new[]{"Materials","Meshes","Scenes","Data","Prefabs"}) LevelAuthoring.EnsureFolder(Folder+"/"+sub);
            Directory.CreateDirectory(Images);Directory.CreateDirectory("Logs/HiddenCanyon");
            stone=new[]{Mat("Warm sandstone",new Color(.61f,.34f,.20f)),Mat("Pale strata",new Color(.79f,.52f,.31f)),
                Mat("Iron bands",new Color(.43f,.23f,.15f)),Mat("Sunlit cap",new Color(.87f,.65f,.40f)),Mat("Rock base",new Color(.48f,.32f,.23f))};
            sand=Mat("Basin sand",new Color(.74f,.59f,.40f));sandLight=Mat("Light sand",new Color(.76f,.61f,.42f));
            sandDark=Mat("Wind swept sand",new Color(.725f,.575f,.385f));dark=Mat("Weathered plinth",new Color(.43f,.35f,.25f));
            crystal=Mat("Small teal accent",new Color(.08f,.65f,.66f));
            cliffMeshes=Enumerable.Range(0,5).Select(CliffMesh).ToArray();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=LevelAuthoring.NewRoot("Hidden Pyramid Canyon");root.levelName="숨겨진 협곡의 피라미드";
            var walls=Group("01 Canyon walls - individually editable",root.transform);
            var modifier=walls.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
            var ground=Group("02 Sand basin",root.transform);Ground(ground.transform);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model),root.transform);
            model.name="03 Pyramid - original spiral route";
            var sourceTransforms=model.GetComponentsInChildren<Transform>(true);
            sourceTransforms.First(t=>t.name=="Ground_Foundation").gameObject.SetActive(false);
            var nodes=sourceTransforms.Where(t=>t.name.StartsWith("EnemyWaypoint_")).OrderBy(t=>t.name,StringComparer.Ordinal).ToArray();
            for(int i=0;i<24;i++)
            {
                if(i==3)continue; // one narrow entrance at the south-east corner
                float angle=i*15*Mathf.Deg2Rad;
                bool gateNeighbor=i==2||i==4;
                float rx=gateNeighbor?46:46+3*Mathf.Sin(angle*3+1)+2*Mathf.Cos(angle*5);
                float rz=gateNeighbor?47:49+4*Mathf.Sin(angle*2-.5f);
                var p=new Vector3(rx*Mathf.Cos(angle),0,rz*Mathf.Sin(angle));
                float h=Mathf.Lerp(37,21,(Mathf.Sin(angle)+1)*.5f)+Range(-5.5f,5.5f);
                p.y=GroundHeight(p.x,p.z)-1.2f;
                Rock("Cliff wall "+i.ToString("00"),p,new Vector3(gateNeighbor?17.5f:Range(22,28),h,Range(13,17)),90-i*15,i,walls.transform);
            }
            // Continue the opening into a bent slot canyon; overlapping buttresses screen the basin.
            for(int i=1;i<4;i++)
            {
                Vector3 center=Approach[i];Vector3 tangent=(Approach[i-1]-Approach[i+1]).normalized;
                Vector3 side=Vector3.Cross(Vector3.up,tangent).normalized;
                foreach(int sign in new[]{-1,1})
                {
                    var p=center+side*12.5f*sign;p.y=GroundHeight(p.x,p.z)-1.2f;
                    Rock("Approach wall "+i+" "+sign,p,new Vector3(24,Range(23,30),14),
                        Mathf.Atan2(tangent.x,tangent.z)*Mathf.Rad2Deg+90,i+sign+5,walls.transform);
                }
            }
            var scatter=Group("04 Talus and weathered stones",root.transform);
            var scatterMod=scatter.AddComponent<NavMeshModifier>();scatterMod.overrideArea=true;scatterMod.area=1;
            for(int i=0;i<48;i++)
            {
                float a=Range(0,Mathf.PI*2),r=Range(33,40);
                var p=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                if(PathDistance(p)<7 || Vector3.Distance(p,nodes[0].position)<8)continue;
                p.y=GroundHeight(p.x,p.z)-.15f;
                Rock("Talus "+i,p,new Vector3(Range(1.1f,3.5f),Range(.5f,2.2f),Range(1,3)),Range(0,360),i,scatter.transform);
            }
            // Distant escarpments give depth beyond the basin rim.
            for(int i=0;i<9;i++)
            {
                var p=new Vector3(-65+i*15,0,-64-Range(0,10));p.y=GroundHeight(p.x,p.z)-2;
                Rock("Far ridge "+i,p,new Vector3(23,Range(32,44),21),Range(-12,12),i,walls.transform);
            }
            for(int i=0;i<12;i++)
            {
                float angle=(95+i*22)*Mathf.Deg2Rad;
                var p=new Vector3(Mathf.Cos(angle)*55,0,Mathf.Sin(angle)*58);
                p.y=GroundHeight(p.x,p.z)-1;
                Rock("Outer weathered buttress "+i,p,new Vector3(Range(13,20),Range(9,18),Range(10,16)),90-angle*Mathf.Rad2Deg,i,walls.transform);
            }
            // A few existing desert props provide a human scale without crowding combat lanes.
            var props=Group("05 Sparse desert plants",root.transform);
            var cactus=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GeeZyyGames/Desert/prefab/cactus.prefab");
            if(cactus) foreach(var p in new[]{new Vector3(-29,0,12),new Vector3(30,0,-17),new Vector3(-24,0,-29)})
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(cactus,props.transform);
                instance.transform.position=p+Vector3.up*GroundHeight(p.x,p.z);
                var rs=instance.GetComponentsInChildren<Renderer>();
                if(rs.Length==0)continue;var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                instance.transform.localScale*=2.3f/Mathf.Max(.01f,bounds.size.y);
                foreach(var r in rs)r.sharedMaterial=Mat("Desert sage",new Color(.28f,.38f,.25f));
            }

            var spawn=Marker(root,"협곡 바깥 적 진입",MarkerKind.EnemySpawn,Approach[0]);
            var player=Marker(root,"협곡 입구 플레이어 시작",MarkerKind.PlayerStart,new Vector3(48,0,70));
            Marker(root,"분지 복귀 지점",MarkerKind.Respawn,new Vector3(29,0,30));
            var core=Marker(root,"정상 코어",MarkerKind.Core,new Vector3(-1.65f,9.6f,0));core.footprint=new Vector3(.6f,1,.6f);
            var route=LevelAuthoring.AddRoute(root,spawn,core);route.label="협곡 진입 → 피라미드 나선로 → 코어";route.name=route.label;
            route.waypoints.AddRange(Approach.Skip(1));
            route.waypoints.AddRange(nodes.Take(nodes.Length-1).Select(t=>t.position));
            foreach(var t in sourceTransforms.Where(t=>t.name.StartsWith("BuildSlot_")).OrderBy(t=>t.name))
            {
                var slot=LevelAuthoring.AddBuildSlot(root,t.position);slot.label=t.name;slot.name=t.name+" - Level slot";
                slot.footprint=new Vector2(1.4f,1.4f);slot.transform.rotation=t.rotation;
                slot.occupancySurfaceId=t.position.y<2?"pyramid.lower":t.position.y<5?"pyramid.middle":"pyramid.upper";
                // Facility catalog is not authored here: explicit IDs remain for the designer.
            }
            var area=LevelAuthoring.AddArea(root,AreaKind.Combat,new[]{new Vector3(-25,4,-25),new Vector3(25,4,-25),new Vector3(25,4,25),new Vector3(-25,4,25)});
            area.label="피라미드 수직 전투 구역";area.height=13;
            var waves=ScriptableObject.CreateInstance<WaveSet>();
            waves.waves.Add(new Wave{label="동선 확인용 웨이브",preparationSeconds=4,groups=new List<SpawnGroup>{new SpawnGroup{spawnId=spawn.id,targetId=core.id,routeId=route.id,count=6,interval=3}}});
            root.waves=Persist(waves,Folder+"/Data/CanyonPathPreviewWaves.asset");

            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.65f,.74f,.85f);RenderSettings.ambientEquatorColor=new Color(.39f,.37f,.36f);
            RenderSettings.ambientGroundColor=new Color(.22f,.17f,.14f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0022f;
            RenderSettings.fogColor=new Color(.61f,.64f,.63f);
            var light=new GameObject("Late afternoon sun").AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=1.25f;light.color=new Color(1,.87f,.68f);light.shadows=LightShadows.Soft;
            light.transform.rotation=Quaternion.Euler(44,-32,0);RenderSettings.sun=light;
            var fill=new GameObject("Cool sky fill").AddComponent<Light>();fill.type=LightType.Directional;
            fill.intensity=.38f;fill.color=new Color(.69f,.80f,1);fill.shadows=LightShadows.None;
            fill.transform.rotation=Quaternion.Euler(65,145,0);
            var hero=Camera("Main Camera - canyon overview",new Vector3(102,136,128),new Vector3(3,8,7),true,81);
            var entrance=Camera("View - narrow entrance",Arrival,new Vector3(33,6,30),false,1);
            var reveal=Camera("View - pyramid reveal",Reveal,new Vector3(0,7.8f,0),false,1);
            var plan=Camera("View - top plan",new Vector3(10,170,20),new Vector3(10,0,20),true,85);
            hero.enabled=true;hero.tag="MainCamera";hero.gameObject.AddComponent<AudioListener>();
            Physics.SyncTransforms();
            var surface=LevelAuthoring.Bake(root);
            if(!LevelValidation.TryPath(route,out var actualPath,out var failure)) throw new InvalidOperationException(failure);
            var issues=LevelValidation.Check(root);var errors=issues.Where(i=>i.severity==IssueSeverity.Error).ToArray();
            if(errors.Length>0)throw new InvalidOperationException(string.Join("; ",errors.Select(i=>i.message)));
            bool hidden=CliffsBlock(Arrival,new Vector3(0,11.5f,0),walls.transform);
            bool revealClear=CoreVisible(Reveal,root.transform);
            if(!hidden||!revealClear) throw new InvalidOperationException($"Reveal framing failed: hidden={hidden}, revealClear={revealClear}");
            var layout=LevelAuthoring.Capture(root);Persist(layout,Folder+"/Data/HiddenCanyonLayout.asset");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            Render(hero,"Canyon_Overview");Render(entrance,"Canyon_Entrance");Render(reveal,"Canyon_Reveal");Render(plan,"Canyon_Plan");
            File.WriteAllText("Logs/HiddenCanyon/verification.txt",
                $"SCENE={ScenePath}\nSLOTS={root.BuildSlots.Length}\nNAV_PATH=Complete\nPATH_CORNERS={actualPath.Count}\nENTRANCE_HIDES_CORE={hidden}\nREVEAL_SEES_CORE={revealClear}\nERRORS=0\n"+
                string.Join("\n",issues.Where(i=>i.severity==IssueSeverity.Warning).Select(i=>"PENDING: "+i.message)));
            Debug.Log("HIDDEN_CANYON_READY "+ScenePath);
        }
    }
}
