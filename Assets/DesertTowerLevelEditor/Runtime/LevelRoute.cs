using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesertTower.Levels
{
    [DisallowMultipleComponent]
    public sealed class LevelRoute : MonoBehaviour
    {
        public string id;
        public string label = "Route";
        public LevelMarker spawn;
        public LevelMarker core;
        [Tooltip("Optional navigation guides, in this object's local coordinates.")]
        public List<Vector3> waypoints = new List<Vector3>();

        public List<Vector3> WorldPoints()
        {
            var points = new List<Vector3>();
            if (spawn) points.Add(spawn.transform.position);
            foreach (var p in waypoints) points.Add(transform.TransformPoint(p));
            if (core) points.Add(core.transform.position);
            return points;
        }

        void Reset() { id = Guid.NewGuid().ToString("N"); }
        void OnValidate() { if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N"); }
    }
}
