using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectRac.Player;

namespace ProjectRac.Editor
{
    public static class PS2VisualSetupBuilder
    {
        private const string ShaderName = "ProjectRac/PS2RetroPostProcess";
        private const string PostProcessFolder = "Assets/Materials/PostProcess";
        private const string RetroMaterialPath = PostProcessFolder + "/Mat_PS2Retro.mat";
        private const string VolumeProfilePath = "Assets/Settings/PS2VolumeProfile.asset";
        private const string FeatureName = "PS2RetroPostProcess";

        [MenuItem("Tools/Apply PS2 Visual & Camera Style")]
        public static void ApplyPS2Style()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            Material retroMat = EnsureRetroMaterial();
            AttachFullScreenFeature("Assets/Settings/PC_Renderer.asset", retroMat);
            AttachFullScreenFeature("Assets/Settings/Mobile_Renderer.asset", retroMat);

            ConfigureURPAsset("Assets/Settings/PC_RPAsset.asset");
            ConfigureGlobalVolume();
            ConfigureBalancedAtmosphereAndLighting();
            ConfigurePS2CameraAndJump();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[PS2VisualSetupBuilder] Visual/camera PS2 suavizado, lanterna removida e pulo ajustado.");
        }

        private static Material EnsureRetroMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new System.Exception($"Shader '{ShaderName}' not found. Ensure PS2RetroPostProcess.shader compiled cleanly.");
            }

            EnsureFolder("Assets/Materials", "PostProcess");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(RetroMaterialPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, RetroMaterialPath);
            }
            else
            {
                mat.shader = shader;
            }

            // Kept deliberately restrained during normal stamina so StaminaExhaustionPostProcess (< 35)
            // stands out dramatically with its heavy vignette, film grain, and lens distortion.
            mat.SetFloat("_TargetWidth", 640f);
            mat.SetFloat("_TargetHeight", 360f);
            mat.SetFloat("_ColorSteps", 28f);
            mat.SetFloat("_DitherStrength", 0.45f);
            mat.SetFloat("_BlackCrush", 0.012f);
            mat.SetFloat("_ScanlineIntensity", 0.08f);
            mat.SetFloat("_ScanlineCount", 360f);
            mat.SetFloat("_ChromaOffset", 0.0005f);
            mat.SetFloat("_NoiseIntensity", 0.012f);
            mat.SetFloat("_EnablePillarbox", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AttachFullScreenFeature(string rendererAssetPath, Material retroMat)
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererAssetPath);
            if (rendererData == null) return;

            FullScreenPassRendererFeature feature = rendererData.rendererFeatures
                .OfType<FullScreenPassRendererFeature>()
                .FirstOrDefault(f => f != null && f.name == FeatureName);

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = FeatureName;
                AssetDatabase.AddObjectToAsset(feature, rendererData);

                var so = new SerializedObject(rendererData);
                var featuresProp = so.FindProperty("m_RendererFeatures");
                int idx = featuresProp.arraySize;
                featuresProp.InsertArrayElementAtIndex(idx);
                featuresProp.GetArrayElementAtIndex(idx).objectReferenceValue = feature;

                var mapProp = so.FindProperty("m_RendererFeatureMap");
                if (mapProp != null && mapProp.isArray)
                {
                    mapProp.InsertArrayElementAtIndex(idx);
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId))
                    {
                        mapProp.GetArrayElementAtIndex(idx).longValue = localId;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.passMaterial = retroMat;
            feature.passIndex = 0;
            feature.SetActive(true);

            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
            rendererData.SetDirty();
        }

        private static void ConfigureURPAsset(string rpAssetPath)
        {
            var rpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpAssetPath);
            if (rpAsset == null) return;

            rpAsset.msaaSampleCount = 1;
            EditorUtility.SetDirty(rpAsset);
        }

        private static void ConfigureGlobalVolume()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            var tonemap = profile.TryGet<Tonemapping>(out var existingTone) ? existingTone : profile.Add<Tonemapping>(true);
            tonemap.mode.overrideState = true;
            tonemap.mode.value = TonemappingMode.Neutral;

            var colorAdj = profile.TryGet<ColorAdjustments>(out var existingColor) ? existingColor : profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.05f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 10f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = -14f;
            colorAdj.colorFilter.overrideState = true;
            colorAdj.colorFilter.value = new Color(0.97f, 0.95f, 0.89f, 1f);

            // Light base vignette (0.14) so StaminaExhaustionPostProcess (0.48) reads unmistakably when exhausted.
            var vignette = profile.TryGet<Vignette>(out var existingVig) ? existingVig : profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.14f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;
            vignette.color.overrideState = true;
            vignette.color.value = Color.black;

            var bloom = profile.TryGet<Bloom>(out var existingBloom) ? existingBloom : profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.05f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.20f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.55f;

            EditorUtility.SetDirty(profile);

            GameObject volObj = GameObject.Find("PS2_GlobalVolume");
            if (volObj == null)
            {
                volObj = new GameObject("PS2_GlobalVolume");
                Undo.RegisterCreatedObjectUndo(volObj, "Create PS2 Global Volume");
            }

            volObj.layer = 0;
            var volume = volObj.GetComponent<Volume>();
            if (volume == null) volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 5f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            volume.enabled = true;
        }

        private static void ConfigureBalancedAtmosphereAndLighting()
        {
            // Balanced ambient and warm interior lights so hallways stay legible without a flashlight.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.165f, 0.18f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.040f, 0.048f, 1f);
            RenderSettings.fogDensity = 0.028f;

            GameObject dirLightObj = GameObject.Find("Directional Light");
            if (dirLightObj != null && dirLightObj.TryGetComponent<Light>(out var dirLight))
            {
                Undo.RecordObject(dirLight, "Tune Directional Light");
                dirLight.intensity = 0.45f;
                dirLight.color = new Color(0.68f, 0.72f, 0.80f, 1f);
            }

            GameObject mansion = GameObject.Find("MansionMap");
            Transform lightsRoot = mansion != null ? mansion.transform.Find("Lighting") : null;
            if (lightsRoot != null)
            {
                var lights = lightsRoot.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    Undo.RecordObject(lights[i], "Tune Mansion Interior Lights");
                    lights[i].color = new Color(1.0f, 0.91f, 0.74f, 1f);
                    lights[i].intensity = 8.5f;
                    lights[i].range = 14.0f;
                }
            }
        }

        private static void ConfigurePS2CameraAndJump()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam, "Configure Subtle PS2 Camera");
                cam.fieldOfView = 58f;

                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData != null)
                {
                    camData.renderPostProcessing = true;
                    camData.antialiasing = AntialiasingMode.None;
                }

                var feel = cam.GetComponent<PS2CameraFeel>();
                if (feel == null) feel = Undo.AddComponent<PS2CameraFeel>(cam.gameObject);
                Undo.RecordObject(feel, "Apply Subtle PS2 Camera Feel");
                feel.ApplySubtlePreset();

                var look = cam.GetComponent<FirstPersonCameraLook>();
                if (look != null)
                {
                    Undo.RecordObject(look, "Tune PS2 Look Smoothing");
                    look.SetLookSmoothing(24f);
                }

                // Remove flashlight pivot and scrub any missing PlayerFlashlight script cleanly.
                Transform pivot = cam.transform.Find("FlashlightPivot");
                if (pivot != null) Undo.DestroyObjectImmediate(pivot.gameObject);
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cam.gameObject);
            }

            var fps = Object.FindAnyObjectByType<FirstPersonController>();
            if (fps != null)
            {
                Undo.RecordObject(fps, "Lower Player Jump Height");
                fps.SetJumpHeight(0.85f);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Materials");
            string full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
