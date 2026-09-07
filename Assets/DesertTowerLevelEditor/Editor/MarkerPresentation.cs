using UnityEngine;

namespace DesertTower.Levels.Editor
{
    public static class MarkerPresentation
    {
        public static Color ColorFor(LevelMarker marker)
        {
            if (marker.definition) return marker.definition.previewColor;
            switch (marker.kind)
            {
                case MarkerKind.EnemySpawn: return new Color(1,.4f,.12f);
                case MarkerKind.Core: return new Color(.1f,1,.9f);
                case MarkerKind.InitialFacility: return new Color(1,.78f,.25f);
                default: return new Color(.45f,.7f,1);
            }
        }
    }
}
