using UnityEditor;
using UnityEngine;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Builds every code-defined VFX prefab in one go. Menu: DesertTower > VFX > Build All.
    /// Batch: <c>Unity -batchmode -projectPath . -executeMethod DesertTower.VFX.Editor.VfxBatch.BuildAll -quit</c>
    /// (add <c>BuildAllAndRender</c> instead of <c>BuildAll</c> to also write preview frames; needs graphics).
    /// </summary>
    public static class VfxBatch
    {
        [MenuItem("DesertTower/VFX/Build All", priority = -100)]
        public static void BuildAll()
        {
            VfxBuildKit.BuildShared();
            foreach (var (spec, path) in ImpactVfxBuilder.All()) ImpactVfxBuilder.Build(spec, path);
            ManaBoltProjectileBuilder.Build();
            CoreAmbientBuilder.Build();
            FlameBreathBuilder.BuildBreath();
            FlameBreathBuilder.BuildBurning();
            GoldVfxBuilder.BuildLevelUp();
            GoldVfxBuilder.BuildWaveClear();
            LoopVfxBuilder.BuildSummonCircle();
            LoopVfxBuilder.BuildManaCharge();
            LoopVfxBuilder.BuildTorch();
            LoopVfxBuilder.BuildFloatingDust();
            BeamVfxBuilder.BuildPierceBeam();
            BeamVfxBuilder.BuildSummonPillar();
            AssetDatabase.SaveAssets();
            VfxShowcaseBuilder.Build();
            Debug.Log("[VFX] Build All done.");
        }

        public static void BuildAllAndRender()
        {
            BuildAll();
            VfxPreviewRenderer.RenderAll();
            VfxShowcaseBuilder.RenderCheck(VfxPreviewRenderer.OutputDir());
        }
    }
}
