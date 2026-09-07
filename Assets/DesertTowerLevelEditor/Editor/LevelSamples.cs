using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesertTower.Levels.Editor
{
    public static class LevelSamples
    {
        static readonly Vector3[] Left={new Vector3(-26,2,30),new Vector3(-22,2,14),new Vector3(-17,2,1),new Vector3(0,2,-9),new Vector3(0,2,-28)};
        static readonly Vector3[] Right={new Vector3(26,5,30),new Vector3(24,5,14),new Vector3(23,5,5),new Vector3(14,2,-6),new Vector3(0,2,-9),new Vector3(0,2,-28)};
        static readonly Vector3[] Rear={new Vector3(-22,2,14),new Vector3(-28,2,-12),new Vector3(-17,2,-22),new Vector3(0,2,-20),new Vector3(18,2,-20),new Vector3(32,2,-10),new Vector3(32,5,14),new Vector3(24,5,14)};

        public static LevelRoot Create(bool canyon)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            string label=canyon ? "Red Canyon Sanctuary" : "Twin Gate Fortress";
            var root=LevelAuthoring.NewRoot(label);
            if(canyon) Canyon(root); else Fortress(root);
            SetupMarkers(root,canyon);
            var lightObject=new GameObject("Sun"); var sun=lightObject.AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=2.1f;
            lightObject.transform.rotation=Quaternion.Euler(42,-30,0); sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.42f,.48f,.56f);
            var cameraObject=new GameObject("Overview Camera"); var camera=cameraObject.AddComponent<Camera>();
            cameraObject.transform.position=new Vector3(66,70,-82); cameraObject.transform.LookAt(new Vector3(0,2,3));
            camera.farClipPlane=500; camera.nearClipPlane=.1f; camera.backgroundColor=new Color(.12f,.17f,.23f); camera.clearFlags=CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            LevelAuthoring.EnsureFolder(LevelAuthoring.GeneratedFolder+"/Scenes");
            string path=AssetDatabase.GenerateUniqueAssetPath(LevelAuthoring.GeneratedFolder+"/Scenes/"+(canyon ? "Canyon" : "Fortress")+".unity");
            EditorSceneManager.SaveScene(root.gameObject.scene,path);
            LevelAuthoring.Bake(root);
            EditorSceneManager.SaveScene(root.gameObject.scene);
            AssetDatabase.SaveAssets(); Selection.activeGameObject=root.gameObject;
            return root;
        }

        static void Fortress(LevelRoot root)
        {
            var sand=LevelAuthoring.Material("Sand",new Color(.66f,.48f,.29f));
            var stone=LevelAuthoring.Material("Sandstone",new Color(.56f,.39f,.22f));
            LevelAuthoring.Block(root,"Foundation",new Vector3(0,1,0),new Vector3(80,2,84),sand);
            LevelAuthoring.Block(root,"North rock / wall mass",new Vector3(0,7,23),new Vector3(20,10,28),stone);
            LevelAuthoring.Block(root,"East terrace",new Vector3(25,3.5f,19),new Vector3(18,3,32),stone);
            LevelAuthoring.Road(root,new[]{new Vector3(23,5.04f,5),new Vector3(14,2.04f,-6)},8,sand);
            LevelAuthoring.Road(root,new[]{new Vector3(32,5.04f,14),new Vector3(35,2.04f,-10)},6,sand);
            LevelAuthoring.Block(root,"West perimeter",new Vector3(-39,6,0),new Vector3(2,8,84),stone);
            LevelAuthoring.Block(root,"East perimeter",new Vector3(39,6,0),new Vector3(2,8,84),stone);
            LevelAuthoring.Block(root,"North perimeter",new Vector3(0,6,41),new Vector3(80,8,2),stone);
            LevelAuthoring.Block(root,"South perimeter",new Vector3(0,3,-41),new Vector3(80,2,2),stone);
            LevelAuthoring.Block(root,"West choke outer",new Vector3(-34,4,12),new Vector3(8,4,12),stone);
            LevelAuthoring.Block(root,"West choke inner",new Vector3(-11,4,12),new Vector3(8,4,12),stone);
            LevelAuthoring.Block(root,"Core alcove left",new Vector3(-9,4,-30),new Vector3(3,4,15),stone);
            LevelAuthoring.Block(root,"Core alcove right",new Vector3(9,4,-30),new Vector3(3,4,15),stone);
        }

        static float Corridor(Vector2 p,Vector3[] path,out float height)
        {
            float best=float.MaxValue; height=2;
            for(int i=0;i<path.Length-1;i++)
            {
                var a=new Vector2(path[i].x,path[i].z); var b=new Vector2(path[i+1].x,path[i+1].z);
                float t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);
                float d=Vector2.Distance(p,Vector2.Lerp(a,b,t));
                if(d<best) { best=d; height=Mathf.Lerp(path[i].y,path[i+1].y,t); }
            }
            return best;
        }
        public static float CanyonHeight(float x,float z)
        {
            var p=new Vector2(x,z);
            float h=12+Mathf.PerlinNoise((x+100)*.045f,(z+100)*.045f)*8;
            h+=10*Mathf.Exp(-(x*x+(z-18)*(z-18))/160);
            foreach(var path in new[]{Left,Right,Rear})
            {
                float distance=Corridor(p,path,out var target);
                float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.5f,10,distance));
                h=Mathf.Lerp(h,target,blend);
            }
            float plaza=Vector2.Distance(p,new Vector2(0,-9));
            h=Mathf.Lerp(h,2,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,18,plaza)));
            return h;
        }
        static void Canyon(LevelRoot root)
        {
            var terrain=LevelAuthoring.NewTerrain(root); var data=terrain.terrainData;
            data.size=new Vector3(96,36,96);
            int n=data.heightmapResolution; var heights=new float[n,n];
            for(int z=0;z<n;z++) for(int x=0;x<n;x++) heights[z,x]=CanyonHeight(x/(float)(n-1)*96-48,z/(float)(n-1)*96-48)/36;
            data.SetHeights(0,0,heights);
            data.terrainLayers=new[]{Layer("Sand surface",new Color(.68f,.49f,.29f),new Color(.76f,.58f,.36f)),Layer("Rock surface",new Color(.33f,.21f,.14f),new Color(.48f,.31f,.19f))};
            data.alphamapResolution=128; var weights=new float[128,128,2];
            for(int z=0;z<128;z++) for(int x=0;x<128;x++)
            { float steep=data.GetSteepness(x/127f,z/127f); float rock=Mathf.InverseLerp(12,43,steep); weights[z,x,0]=1-rock; weights[z,x,1]=rock; }
            data.SetAlphamaps(0,0,weights); EditorUtility.SetDirty(data);
        }
        static TerrainLayer Layer(string name,Color a,Color b)
        {
            LevelAuthoring.EnsureFolder(LevelAuthoring.GeneratedFolder+"/Terrain");
            string path=LevelAuthoring.GeneratedFolder+"/Terrain/"+name+".terrainlayer";
            var existing=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path); if(existing) return existing;
            var texture=new Texture2D(64,64,TextureFormat.RGB24,true) { name=name,wrapMode=TextureWrapMode.Repeat };
            var random=new System.Random(42); var pixels=new Color[64*64];
            for(int i=0;i<pixels.Length;i++) pixels[i]=Color.Lerp(a,b,(float)random.NextDouble());
            texture.SetPixels(pixels); texture.Apply();
            AssetDatabase.CreateAsset(texture,LevelAuthoring.GeneratedFolder+"/Terrain/"+name+".asset");
            var layer=new TerrainLayer { diffuseTexture=texture,tileSize=new Vector2(6,6) }; AssetDatabase.CreateAsset(layer,path); return layer;
        }

        static void SetupMarkers(LevelRoot root,bool canyon)
        {
            var core=LevelAuthoring.AddMarker(root,MarkerKind.Core,Left[Left.Length-1]); core.label="Core Sanctuary"; core.name=core.label;
            var start=LevelAuthoring.AddMarker(root,MarkerKind.PlayerStart,new Vector3(-4,2,-24)); start.label="Player Start";
            LevelAuthoring.AddMarker(root,MarkerKind.Respawn,new Vector3(4,2,-30)).label="Respawn";
            var left=LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,Left[0]); left.label="A · West Pass"; left.spawnRadius=2;
            var right=LevelAuthoring.AddMarker(root,MarkerKind.EnemySpawn,Right[0]); right.label="B · High Pass"; right.spawnRadius=2;
            left.transform.rotation=Quaternion.Euler(0,180,0); right.transform.rotation=Quaternion.Euler(0,180,0);
            var a=LevelAuthoring.AddRoute(root,left,core); a.label="A → Basin → Core";
            var b=LevelAuthoring.AddRoute(root,right,core); b.label="B → Ramp → Core";
            for(int i=1;i<Left.Length-1;i++) a.waypoints.Add(Left[i]);
            for(int i=1;i<Right.Length-1;i++) b.waypoints.Add(Right[i]);
            Rect(root,AreaKind.Buildable,new Vector3(-22,2,14),new Vector2(8,8),"West Defense");
            Rect(root,AreaKind.Buildable,new Vector3(24,5,14),new Vector2(8,8),"High Defense");
            Rect(root,AreaKind.Buildable,new Vector3(0,2,-14),new Vector2(16,6),"Fallback Defense");
            Rect(root,AreaKind.Combat,new Vector3(0,2,-8),new Vector2(24,18),"Battle Basin");
            Rect(root,AreaKind.NoBuild,core.transform.position,new Vector2(7,7),"Core Keep Clear");
            Rect(root,AreaKind.NoBuild,left.transform.position,new Vector2(6,6),"Spawn A Keep Clear");
            Rect(root,AreaKind.NoBuild,right.transform.position,new Vector2(6,6),"Spawn B Keep Clear");
            LevelAuthoring.AddMarker(root,MarkerKind.InitialFacility,new Vector3(-24,2,13)).label="Cobra Example";
            LevelAuthoring.AddMarker(root,MarkerKind.InitialFacility,new Vector3(26,5,13)).label="Cobra Example";
            LevelAuthoring.AddMarker(root,MarkerKind.InitialFacility,new Vector3(-5,2,-14)).label="Summon Example";
            LevelAuthoring.EnsureFolder(LevelAuthoring.GeneratedFolder+"/Data");
            var waves=ScriptableObject.CreateInstance<WaveSet>();
            for(int i=0;i<5;i++)
            {
                var wave=new Wave { label="Wave "+(i+1),preparationSeconds=i==0 ? 0 : 20 };
                wave.groups.Add(new SpawnGroup { spawnId=left.id,targetId=core.id,routeId=a.id,count=6+i*3,interval=1 });
                if(i>=2) wave.groups.Add(new SpawnGroup { spawnId=right.id,targetId=core.id,routeId=b.id,count=5+i*2,delay=3,interval=1.2f });
                waves.waves.Add(wave);
            }
            AssetDatabase.CreateAsset(waves,AssetDatabase.GenerateUniqueAssetPath(LevelAuthoring.GeneratedFolder+"/Data/"+(canyon ? "Canyon" : "Fortress")+"Waves.asset"));
            root.waves=waves;
        }
        static void Rect(LevelRoot root,AreaKind kind,Vector3 center,Vector2 size,string name)
        {
            var area=LevelAuthoring.AddArea(root,kind,new[]{center+new Vector3(-size.x/2,0,-size.y/2),center+new Vector3(-size.x/2,0,size.y/2),center+new Vector3(size.x/2,0,size.y/2),center+new Vector3(size.x/2,0,-size.y/2)});
            area.label=name; area.name=name;
        }
    }
}
