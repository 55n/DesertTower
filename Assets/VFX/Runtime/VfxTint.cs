using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>
    /// Recolours a set of particle systems at spawn time. The enemy-death effect uses it so the falling
    /// cubes take the dying character's colour: <c>GetComponent&lt;VfxTint&gt;().Apply(enemyColor)</c>
    /// right after instantiating, before the first frame plays.
    /// </summary>
    public sealed class VfxTint : MonoBehaviour
    {
        [Tooltip("Systems whose Start Color is replaced by Apply(). Colour-over-lifetime ramps still multiply on top. " +
                 "The prefab's authored Start Color is what plays when nothing calls Apply().")]
        public ParticleSystem[] Systems;

        public void Apply(Color color)
        {
            if (Systems == null) return;
            foreach (var ps in Systems)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.startColor = color;
            }
        }
    }
}
