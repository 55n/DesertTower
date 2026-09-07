using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace DesertTower.Levels.Diagnostics
{
    [MovedFrom(true,"DesertTower.Levels","DesertTower.Levels.Runtime","LevelPlaytest")]
    public sealed class LevelPlaytest : MonoBehaviour
    {
        public LevelRoot level;
        public int waveIndex;
        public DiagnosticSettings settings=new DiagnosticSettings();
        DiagnosticWaveRunner runner;
        public int ArrivedCount => runner ? runner.Arrived : 0;
        public int FailedCount => runner ? runner.Failed : 0;
        public bool IsComplete => runner && runner.IsComplete;

        void Start()
        {
            if(!level) { Debug.LogError("A diagnostic session needs a level."); return; }
            var visuals=gameObject.AddComponent<DiagnosticVisuals>(); visuals.ShowMarkers(level);
            LevelMarker start=null;
            foreach(var marker in level.Markers) if(marker.kind==MarkerKind.PlayerStart) { start=marker; break; }
            var playerObject=new GameObject("Diagnostic player"); playerObject.transform.SetParent(transform,false);
            var player=playerObject.AddComponent<DiagnosticWalkthrough>();
            player.Initialize(start ? start.transform.position+Vector3.up*.1f : level.transform.position+Vector3.up*2,start ? start.transform.eulerAngles.y : 0,settings);
            var body=visuals.Create(PrimitiveType.Capsule,"Player scale proxy",new Color(.2f,.55f,.8f),player.transform);
            body.transform.localPosition=Vector3.up*.9f; body.transform.localScale=new Vector3(.7f,.9f,.7f);
            var cameraObject=new GameObject("Diagnostic camera"); cameraObject.transform.SetParent(transform,false);
            cameraObject.AddComponent<DiagnosticCamera>().Initialize(player,settings);
            runner=gameObject.AddComponent<DiagnosticWaveRunner>(); runner.Begin(level,waveIndex,settings,visuals);
        }
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16,16,480,170),GUI.skin.box);
            GUILayout.Label("DESERT TOWER | Optional navigation diagnostic");
            GUILayout.Label("WASD move | Hold RMB look | Shift run | R reset");
            if(runner) GUILayout.Label($"Spawned {runner.Spawned}   Active {runner.Active}   Arrived {runner.Arrived}   Failed {runner.Failed}");
            GUILayout.Label(IsComplete ? "Test complete. Exit Play mode to return to editing." : "Previewing selected wave...");
            GUILayout.Label("Preview capsules only. Game AI, movement and combat are external.");
            GUILayout.EndArea();
        }
    }
}
