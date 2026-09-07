using System;
using UnityEngine;

namespace DesertTower.Levels.Diagnostics
{
    [Serializable]
    public sealed class DiagnosticSettings
    {
        [Min(.1f)] public float probeSpeed=3;
        public float agentRadius=.35f;
        public float agentHeight=1.8f;
        public float segmentTimeout=90;
        public float walkSpeed=4;
        public float runSpeed=8;
        public float lookSensitivity=.12f;
        public bool followSuggestedRoute=true;
    }
}
