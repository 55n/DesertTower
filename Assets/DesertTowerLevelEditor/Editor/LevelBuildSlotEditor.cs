using System;
using UnityEditor;
using UnityEngine;

namespace DesertTower.Levels.Editor
{
    [CustomEditor(typeof(LevelBuildSlot))]
    public sealed class LevelBuildSlotEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("label"), new GUIContent("표시 이름"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), new GUIContent("슬롯 ID"));
            if (GUILayout.Button("이 슬롯에 새 ID 부여"))
                serializedObject.FindProperty("id").stringValue = Guid.NewGuid().ToString("N");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("occupancySurfaceId"), new GUIContent("바닥 ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowedFacilityIds"), new GUIContent("허용 시설 ID"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("footprint"), new GUIContent("자리 크기"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("available"), new GUIContent("건설 허용"));
            if (serializedObject.ApplyModifiedProperties())
                LevelAuthoring.Dirty(((LevelBuildSlot)target).GetComponentInParent<LevelRoot>());
            if (!((LevelBuildSlot)target).TryValidate(out var error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawSlot(LevelBuildSlot slot, GizmoType type)
        {
            using (new Handles.DrawingScope(new Color(.2f, .9f, 1f),
                Matrix4x4.TRS(slot.transform.position, slot.transform.rotation, Vector3.one)))
                Handles.DrawWireCube(new Vector3(0, .05f, 0), new Vector3(slot.footprint.x, .1f, slot.footprint.y));
            Handles.Label(slot.transform.position + Vector3.up * .2f, slot.label);
        }
    }
}
