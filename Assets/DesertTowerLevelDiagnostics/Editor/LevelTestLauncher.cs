using UnityEditor;
using UnityEngine;
using DesertTower.Levels.Diagnostics;

namespace DesertTower.Levels.Editor
{
    [InitializeOnLoad]
    public static class LevelTestLauncher
    {
        const string Key="DesertTower.LevelTest";
        static LevelTestLauncher()
        {
            LevelDiagnosticService.StartHandler=Start;
            EditorApplication.playModeStateChanged+=Changed;
        }
        public static void Start(LevelRoot root,int wave)
        {
            SessionState.SetString(Key,GlobalObjectId.GetGlobalObjectIdSlow(root).ToString());
            SessionState.SetInt(Key+".wave",wave);
            EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode) SessionState.EraseString(Key);
            if(state!=PlayModeStateChange.EnteredPlayMode) return;
            string id=SessionState.GetString(Key,""); if(string.IsNullOrEmpty(id)) return;
            LevelRoot root=null;
            if(GlobalObjectId.TryParse(id,out var global)) root=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(global) as LevelRoot;
            if(!root) { Debug.LogError("Level diagnostic could not resolve its saved level root."); return; }
            var go=new GameObject("Desert Tower diagnostic (temporary)");
            var test=go.AddComponent<LevelPlaytest>();
            if(!test) { UnityEngine.Object.Destroy(go); Debug.LogError("Unable to create the optional diagnostic session."); return; }
            test.level=root; test.waveIndex=SessionState.GetInt(Key+".wave",0);
        }
    }
}
