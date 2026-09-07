using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DesertTower.Levels.Diagnostics
{
    [RequireComponent(typeof(Camera))]
    public sealed class DiagnosticCamera : MonoBehaviour
    {
        DiagnosticWalkthrough player;
        DiagnosticSettings settings;
        readonly List<Camera> disabled=new List<Camera>();
        float pitch=12;
        public void Initialize(DiagnosticWalkthrough target,DiagnosticSettings options)
        {
            player=target; settings=options;
            var view=GetComponent<Camera>(); view.nearClipPlane=.1f; view.farClipPlane=500;
            foreach(var camera in Camera.allCameras) if(camera!=view && camera.enabled) { disabled.Add(camera); camera.enabled=false; }
        }
        void LateUpdate()
        {
            if(!player) return;
            var mouse=Mouse.current;
            if(mouse!=null && mouse.rightButton.isPressed)
            { var delta=mouse.delta.ReadValue(); player.SetYaw(player.Yaw+delta.x*settings.lookSensitivity); pitch=Mathf.Clamp(pitch-delta.y*settings.lookSensitivity,-65,75); }
            var rotation=Quaternion.Euler(pitch,player.Yaw,0); var pivot=player.transform.position+Vector3.up*1.6f;
            var offset=rotation*new Vector3(.65f,.25f,-3.5f); float distance=offset.magnitude;
            if(Physics.SphereCast(pivot,.15f,offset.normalized,out var hit,distance,~0,QueryTriggerInteraction.Ignore)) distance=Mathf.Max(.25f,hit.distance-.1f);
            transform.SetPositionAndRotation(pivot+offset.normalized*distance,rotation);
        }
        void OnDestroy() { foreach(var camera in disabled) if(camera) camera.enabled=true; }
    }
}
