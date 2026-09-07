using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesertTower.Levels
{
    public enum AreaKind { Buildable, NoBuild, Combat }

    [DisallowMultipleComponent]
    public sealed class LevelArea : MonoBehaviour
    {
        public string id;
        public string label = "Area";
        public AreaKind kind;
        [Tooltip("Polygon in local XZ. Rotate around Y; keep scale at one.")]
        public List<Vector2> vertices = new List<Vector2> { new Vector2(-3,-3), new Vector2(-3,3), new Vector2(3,3), new Vector2(3,-3) };
        [Min(.1f), Tooltip("Vertical tolerance prevents a zone affecting another floor.")]
        public float height = 1;

        public bool Contains(Vector3 point)
        {
            var local = transform.InverseTransformPoint(point);
            return Mathf.Abs(local.y) <= height * .5f && ContainsXZ(vertices, new Vector2(local.x, local.z));
        }

        public static bool ContainsXZ(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            if (polygon == null || polygon.Count < 3) return false;
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[j]; var b = polygon[i];
                var ab = b - a;
                float t = ab.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point-a, ab) / ab.sqrMagnitude) : 0;
                if ((point - (a + ab*t)).sqrMagnitude < .000001f) return true;
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)
                    inside = !inside;
            }
            return inside;
        }

        public Vector3 WorldVertex(int index) => transform.TransformPoint(new Vector3(vertices[index].x, 0, vertices[index].y));
        void Reset() { id = Guid.NewGuid().ToString("N"); }
        void OnValidate() { if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N"); }
    }
}
