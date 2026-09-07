using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesertTower.Levels.Editor
{
    public static class LevelBatchVerification
    {
        public static void GenerateAndVerify()
        {
            try
            {
                Directory.CreateDirectory("Logs/LevelEditorVerification");
                var report=new System.Text.StringBuilder();
                foreach(bool canyon in new[]{false,true})
                {
                    var root=LevelSamples.Create(canyon);
                    var issues=LevelValidation.Check(root);
                    report.AppendLine(root.levelName);
                    foreach(var issue in issues) report.AppendLine(issue.severity+": "+issue.message+" ["+issue.context+"]");
                    foreach(var route in root.Routes)
                    {
                        if(!LevelValidation.TryPath(route,out var points,out var failure)) throw new Exception(root.levelName+" "+route.label+": "+failure);
                        report.AppendLine(route.label+": "+points.Count+" navigation corners");
                    }
                    string layoutPath=AssetDatabase.GenerateUniqueAssetPath(LevelAuthoring.GeneratedFolder+"/Data/"+(canyon ? "Canyon" : "Fortress")+"Layout.asset");
                    AssetDatabase.CreateAsset(LevelAuthoring.Capture(root),layoutPath);
                    if(issues.Any(i=>i.severity==IssueSeverity.Error)) throw new Exception("Sample validation contains errors.\n"+report);
                    if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) Render(root,canyon ? "Canyon" : "Fortress");
                    EditorSceneManager.SaveScene(root.gameObject.scene);
                }
                AssetDatabase.SaveAssets(); File.WriteAllText("Logs/LevelEditorVerification/samples.txt",report.ToString());
                Debug.Log("DESERT_TOWER_VERIFICATION_PASSED\n"+report);
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static void Render(LevelRoot root,string name)
        {
            var objects=new System.Collections.Generic.List<GameObject>();
            foreach(var marker in root.Markers)
            {
                var go=GameObject.CreatePrimitive(marker.kind==MarkerKind.Core ? PrimitiveType.Sphere : PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(marker.transform.position+Vector3.up*marker.Footprint.y*.5f,marker.transform.rotation); go.transform.localScale=marker.Footprint;
                go.GetComponent<Renderer>().sharedMaterial=LevelAuthoring.Material("Preview "+marker.kind,MarkerPresentation.ColorFor(marker)); objects.Add(go);
            }
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>(); var target=new RenderTexture(1400,1000,24);
            camera.targetTexture=target; camera.Render(); var previous=RenderTexture.active; RenderTexture.active=target;
            var texture=new Texture2D(1400,1000,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1400,1000),0,0); texture.Apply();
            File.WriteAllBytes("Logs/LevelEditorVerification/"+name+".png",texture.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
            foreach(var go in objects) UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
