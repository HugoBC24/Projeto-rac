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
        var pcRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        var ps2Feature = pcRenderer?.rendererFeatures
            .OfType<FullScreenPassRendererFeature>()
            .FirstOrDefault(f => f != null && f.name == "PS2RetroPostProcess");
        var vol = GameObject.Find("PS2_GlobalVolume")?.GetComponent<Volume>();

        return new
        {
            ok = true,
            cameraFov = cam?.fieldOfView,
            hasCameraFeel = cam?.GetComponent<PS2CameraFeel>() != null,
            hasFlashlight = cam?.GetComponent<PlayerFlashlight>() != null,
            hasFeature = ps2Feature != null && ps2Feature.isActive && ps2Feature.passMaterial != null,
            volumePriority = vol?.priority,
            fogDensity = RenderSettings.fogDensity
        };
    }
}
