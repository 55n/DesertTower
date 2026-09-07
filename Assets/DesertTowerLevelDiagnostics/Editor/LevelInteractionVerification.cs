using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DesertTower.Levels.Diagnostics;

namespace DesertTower.Levels.Editor
{
    [InitializeOnLoad]
    public static class LevelInteractionVerification
    {
        const string Key="DesertTower.Verification";
        static double next;
        static LevelInteractionVerification()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=Log;
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/DesertTowerLevels/Scenes/Canyon.unity");
            var root=UnityEngine.Object.FindFirstObjectByType<LevelRoot>();
            Selection.activeGameObject=root.gameObject;
            SessionState.SetInt(Key,1); SessionState.SetInt(Key+".tab",0); SessionState.SetString(Key+".error","");
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+110);
            LevelEditorWindow.Open(); next=EditorApplication.timeSinceStartup+1;
        }
        static void Log(string condition,string stack,LogType type)
        {
            if(SessionState.GetInt(Key,0)>0 && (type==LogType.Exception || type==LogType.Error || type==LogType.Assert))
                SessionState.SetString(Key+".error",SessionState.GetString(Key+".error","")+condition+"\n"+stack+"\n");
        }
        static void Tick()
        {
            int phase=SessionState.GetInt(Key,0); if(phase==0 || EditorApplication.timeSinceStartup<next) return;
            next=EditorApplication.timeSinceStartup+.5;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0)) { Finish(false,"Timed out."); return; }
            if(phase==1)
            {
                var window=EditorWindow.GetWindow<LevelEditorWindow>();
                var serialized=new SerializedObject(window); int tab=SessionState.GetInt(Key+".tab",0);
                if(tab<5)
                {
                    serialized.FindProperty("tab").intValue=tab; serialized.ApplyModifiedPropertiesWithoutUndo(); window.Repaint();
                    SessionState.SetInt(Key+".tab",tab+1); return;
                }
                SessionState.SetInt(Key,2);
                LevelTestLauncher.Start(UnityEngine.Object.FindFirstObjectByType<LevelRoot>(),0);
            }
            else if(phase==2 && EditorApplication.isPlaying)
            {
                var test=UnityEngine.Object.FindFirstObjectByType<LevelPlaytest>();
                if(test && test.IsComplete)
                {
                    SessionState.SetString(Key+".result",$"Canyon Wave 1: arrived={test.ArrivedCount}, failed={test.FailedCount}. Five editor tabs exercised.");
                    SessionState.SetBool(Key+".passed",test.ArrivedCount==6 && test.FailedCount==0);
                    SessionState.SetInt(Key,3); EditorApplication.isPlaying=false;
                }
            }
            else if(phase==3 && !EditorApplication.isPlayingOrWillChangePlaymode)
                Finish(SessionState.GetBool(Key+".passed",false),SessionState.GetString(Key+".result",""));
        }
        static void Finish(bool passed,string result)
        {
            string errors=SessionState.GetString(Key+".error",""); SessionState.SetInt(Key,0);
            Directory.CreateDirectory("Logs/LevelEditorVerification");
            File.WriteAllText("Logs/LevelEditorVerification/interaction.txt",result+"\n"+errors);
            if(passed && string.IsNullOrEmpty(errors)) { Debug.Log("DESERT_TOWER_INTERACTION_PASSED "+result); EditorApplication.Exit(0); }
            else { Debug.LogError("DESERT_TOWER_INTERACTION_FAILED "+result+"\n"+errors); EditorApplication.Exit(1); }
        }
    }
}
