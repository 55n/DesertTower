using System.Collections.Generic;
using UnityEngine;

namespace DesertTower.Levels.Diagnostics
{
    public sealed class DiagnosticVisuals : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        public GameObject Create(PrimitiveType type,string label,Color color,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(type); go.name=label;
            go.transform.SetParent(parent ? parent : transform,false);
            go.GetComponent<Collider>().enabled=false;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color=color };
            materials.Add(material); go.GetComponent<Renderer>().sharedMaterial=material; return go;
        }
        public void ShowMarkers(LevelRoot level)
        {
            foreach(var marker in level.Markers)
            {
                if(marker.kind!=MarkerKind.Core && marker.kind!=MarkerKind.InitialFacility) continue;
                var color=marker.definition ? marker.definition.previewColor : marker.kind==MarkerKind.Core ? Color.cyan : new Color(1,.78f,.25f);
                var proxy=Create(PrimitiveType.Cube,marker.label,color);
                proxy.transform.SetPositionAndRotation(marker.transform.position+Vector3.up*marker.Footprint.y*.5f,marker.transform.rotation);
                proxy.transform.localScale=marker.Footprint;
            }
        }
        void OnDestroy() { foreach(var material in materials) if(material) Destroy(material); }
    }
}
