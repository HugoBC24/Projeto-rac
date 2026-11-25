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
            ConfigureHorrorAtmosphereAndLighting();
            ConfigurePS2CameraAndFlashlight();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[PS2VisualSetupBuilder] Visual e camera estilo PS2 (Stay Out of the House / Haneko) aplicados!");
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

            mat.SetFloat("_TargetWidth", 480f);
            mat.SetFloat("_TargetHeight", 270f);
            mat.SetFloat("_ColorSteps", 20f);
            mat.SetFloat("_DitherStrength", 0.85f);
            mat.SetFloat("_BlackCrush", 0.025f);
            mat.SetFloat("_ScanlineIntensity", 0.15f);
            mat.SetFloat("_ScanlineCount", 270f);
            mat.SetFloat("_ChromaOffset", 0.0010f);
            mat.SetFloat("_NoiseIntensity", 0.045f);
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
            colorAdj.postExposure.value = 0.15f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 18f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = -20f;
            colorAdj.colorFilter.overrideState = true;
            colorAdj.colorFilter.value = new Color(0.95f, 0.92f, 0.82f, 1f);

            var vignette = profile.TryGet<Vignette>(out var existingVig) ? existingVig : profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.30f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.42f;
            vignette.color.overrideState = true;
            vignette.color.value = Color.black;

            var bloom = profile.TryGet<Bloom>(out var existingBloom) ? existingBloom : profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.95f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.35f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.60f;

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
            // Strictly lower than StaminaExhaustionPostProcess (priority 25) so exhaustion effects override cleanly.
            volume.priority = 5f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            volume.enabled = true;
        }

        private static void ConfigureHorrorAtmosphereAndLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.085f, 0.090f, 0.100f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.025f, 0.028f, 0.036f, 1f);
            RenderSettings.fogDensity = 0.045f;

            GameObject dirLightObj = GameObject.Find("Directional Light");
            if (dirLightObj != null && dirLightObj.TryGetComponent<Light>(out var dirLight))
            {
                Undo.RecordObject(dirLight, "Tune Nocturnal Directional Light");
                dirLight.intensity = 0.25f;
                dirLight.color = new Color(0.45f, 0.52f, 0.65f, 1f);
            }

            // Tune MansionMap interior lights into moody warm/amber pools surrounded by dark corners.
            GameObject mansion = GameObject.Find("MansionMap");
            Transform lightsRoot = mansion != null ? mansion.transform.Find("Lighting") : null;
            if (lightsRoot != null)
            {
                var lights = lightsRoot.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    Undo.RecordObject(lights[i], "Tune Mansion Horror Lights");
                    lights[i].color = new Color(1.0f, 0.87f, 0.58f, 1f);
                    lights[i].intensity = 5.5f;
                    lights[i].range = 11.5f;
                }
            }
        }

        private static void ConfigurePS2CameraAndFlashlight()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Undo.RecordObject(cam, "Configure PS2 Camera");
            cam.fieldOfView = 55f;

            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.None;
            }

            var feel = cam.GetComponent<PS2CameraFeel>();
            if (feel == null) feel = Undo.AddComponent<PS2CameraFeel>(cam.gameObject);
            feel.SetRetroFov(55f);

            var look = cam.GetComponent<FirstPersonCameraLook>();
            if (look != null)
            {
                Undo.RecordObject(look, "Configure PS2 Look Smoothing");
                look.SetLookSmoothing(16f);
            }

            Transform pivot = cam.transform.Find("FlashlightPivot");
            if (pivot == null)
            {
                GameObject pivotObj = new GameObject("FlashlightPivot");
                pivotObj.transform.SetParent(cam.transform, false);
                Undo.RegisterCreatedObjectUndo(pivotObj, "Create Flashlight Pivot");
                pivot = pivotObj.transform;
            }
            // Place slightly ahead of weapon viewmodels so they never cast giant shadows onto nearby walls.
            pivot.localPosition = new Vector3(0.0f, -0.04f, 0.62f);

            Light spot = pivot.GetComponent<Light>();
            if (spot == null) spot = Undo.AddComponent<Light>(pivot.gameObject);
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.945f, 0.84f, 1f);
            spot.intensity = 9.5f;
            spot.range = 20f;
            spot.spotAngle = 62f;
            spot.innerSpotAngle = 22f;
            spot.shadows = LightShadows.Soft;

            var flashlight = cam.GetComponent<PlayerFlashlight>();
            if (flashlight == null) flashlight = Undo.AddComponent<PlayerFlashlight>(cam.gameObject);
            flashlight.Configure(spot, pivot, enabledByDefault: true);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Materials");
            string full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
