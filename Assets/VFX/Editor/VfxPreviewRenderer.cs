using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Renders a VFX prefab at several simulation times into PNG files, in a throw-away scene
    /// with a URP camera, Bloom volume and a dark ground plane. Used for headless review
    /// (batch mode without -nographics) and from the menu for quick checks.
    /// Output directory: env VFX_PREVIEW_DIR, else &lt;project&gt;/Temp/VfxPreview.
    /// Prefab path: env VFX_PREVIEW_PREFAB, else the Mana Bolt Impact prefab.
    /// </summary>
    public static class VfxPreviewRenderer
    {
        const string DefaultPrefab = "Assets/VFX/Prefabs/VFX_ManaBolt_Impact.prefab";
        static readonly float[] SampleTimes = { 0.04f, 0.12f, 0.25f, 0.45f, 0.8f, 1.2f };

        [MenuItem("DesertTower/VFX/Render Preview Frames (Mana Bolt Impact)")]
        public static void RenderFromMenu() => Render();

        /// <summary>Batch entry point: rebuild the prefab, then render preview frames.</summary>
        public static void BuildAndRender()
        {
            ManaBoltImpactBuilder.Build();
            Render();
        }

        public static void Render()
        {
            string prefabPath = System.Environment.GetEnvironmentVariable("VFX_PREVIEW_PREFAB");
            if (string.IsNullOrEmpty(prefabPath)) prefabPath = DefaultPrefab;
            string outDir = System.Environment.GetEnvironmentVariable("VFX_PREVIEW_DIR");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "VfxPreview");
            Directory.CreateDirectory(outDir);

            float[] times = SampleTimes;
            string timesEnv = System.Environment.GetEnvironmentVariable("VFX_PREVIEW_TIMES");
            if (!string.IsNullOrEmpty(timesEnv))
                times = System.Array.ConvertAll(timesEnv.Split(','), s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogError($"[VFX] Prefab not found: {prefabPath}"); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            // Ground: dark sandstone-ish plane so the teal reads against something
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 2f;
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.SetColor("_BaseColor", new Color(0.30f, 0.24f, 0.16f));
            groundMat.SetFloat("_Smoothness", 0.1f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // Key light (dim, so emissive particles dominate)
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.6f;
            sun.color = new Color(1f, 0.92f, 0.8f);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.12f, 0.16f);

            // Scale reference: a 1.8 unit "character" box beside the impact
            var refBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            refBox.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
            refBox.transform.position = new Vector3(1.6f, 0.9f, 0.4f);
            var refMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            refMat.SetColor("_BaseColor", new Color(0.55f, 0.50f, 0.42f));
            refBox.GetComponent<Renderer>().sharedMaterial = refMat;

            // Post-processing volume with Bloom (project profile values: threshold 1, intensity 0.25 → boosted here to spec)
            var volumeGo = new GameObject("Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 0.8f;
            bloom.scatter.value = 0.6f;
            volume.sharedProfile = profile;

            // Camera
            var camGo = new GameObject("PreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.allowHDR = true;
            camGo.transform.position = new Vector3(0f, 2.2f, -4.2f);
            camGo.transform.LookAt(new Vector3(0f, 0.5f, 0f));
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(0f, 0.6f, 0f); // chest height of a 1.8 unit enemy
            var ps = instance.GetComponent<ParticleSystem>();

            const int w = 960, h = 540;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf);
            cam.targetTexture = rt;
            var readback = new Texture2D(w, h, TextureFormat.RGBA32, false);

            foreach (var t in times)
            {
                ps.Simulate(t, withChildren: true, restart: true, fixedTimeStep: false);
                foreach (var sub in instance.GetComponentsInChildren<ParticleSystem>())
                    Debug.Log($"[VFX] t={t:0.000} {sub.name}: alive={sub.particleCount}");
                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                readback.Apply();
                RenderTexture.active = prev;

                var file = Path.Combine(outDir, $"impact_t{t:0.00}.png");
                File.WriteAllBytes(file, readback.EncodeToPNG());
                Debug.Log($"[VFX] wrote {file}");
            }

            cam.targetTexture = null;
            Object.DestroyImmediate(readback);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(groundMat);
            Object.DestroyImmediate(refMat);
            Object.DestroyImmediate(profile);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log($"[VFX] Preview frames written to {outDir}");
        }
    }
}
