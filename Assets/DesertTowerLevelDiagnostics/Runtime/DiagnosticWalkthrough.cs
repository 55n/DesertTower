using UnityEngine;
using UnityEngine.InputSystem;

namespace DesertTower.Levels.Diagnostics
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DiagnosticWalkthrough : MonoBehaviour
    {
        CharacterController controller;
        DiagnosticSettings settings;
        Vector3 start;
        float velocityY;
        public float Yaw { get; private set; }
        public void Initialize(Vector3 position,float yaw,DiagnosticSettings options)
        {
            start=position; Yaw=yaw; settings=options; transform.position=position;
            controller=GetComponent<CharacterController>(); controller.height=1.8f; controller.radius=.35f; controller.center=Vector3.up*.9f; controller.stepOffset=.35f;
        }
        public void SetYaw(float value) { Yaw=value; }
        void Update()
        {
            if(!controller || settings==null) return;
            var keyboard=Keyboard.current; var move=Vector3.zero;
            if(keyboard!=null)
            {
                if(keyboard.wKey.isPressed) move.z++; if(keyboard.sKey.isPressed) move.z--;
                if(keyboard.dKey.isPressed) move.x++; if(keyboard.aKey.isPressed) move.x--;
                if(keyboard.rKey.wasPressedThisFrame) { controller.enabled=false; transform.position=start; controller.enabled=true; velocityY=0; }
            }
            if(controller.isGrounded && velocityY<0) velocityY=-2; else velocityY-=20*Time.deltaTime;
            float speed=keyboard!=null && keyboard.leftShiftKey.isPressed ? settings.runSpeed : settings.walkSpeed;
            controller.Move((Quaternion.Euler(0,Yaw,0)*move.normalized*speed+Vector3.up*velocityY)*Time.deltaTime);
        }
    }
}
