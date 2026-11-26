using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProjectRac.Editor;
using ProjectRac.Player;

public static class ApplyPS2StyleInScene
{
    public static object Apply()
    {
        PS2VisualSetupBuilder.ApplyPS2Style();

        var cam = Camera.main;
        var fps = Object.FindAnyObjectByType<FirstPersonController>();
        var pcRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        var ps2Feature = pcRenderer?.rendererFeatures
            .OfType<FullScreenPassRendererFeature>()
            .FirstOrDefault(f => f != null && f.name == "PS2RetroPostProcess");
        var vol = GameObject.Find("PS2_GlobalVolume")?.GetComponent<Volume>();
        int missingOnCam = cam != null ? GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(cam.gameObject) : -1;

        return new
        {
            ok = true,
            cameraFov = cam?.fieldOfView,
            hasCameraFeel = cam?.GetComponent<PS2CameraFeel>() != null,
            hasFlashlightPivot = cam?.transform.Find("FlashlightPivot") != null,
            missingScriptsOnCam = missingOnCam,
            jumpHeight = fps?.JumpHeight,
            hasFeature = ps2Feature != null && ps2Feature.isActive && ps2Feature.passMaterial != null,
            volumePriority = vol?.priority,
            fogDensity = RenderSettings.fogDensity
        };
    }
}
