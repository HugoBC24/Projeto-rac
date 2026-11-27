using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectRac.Editor;

public static class ApplySkyboxAndRangedSync
{
    private const string SkyboxTexPath = "Assets/Art/Skyboxes/NightMoon_Skybox.png";
    private const string SkyboxMatPath = "Assets/Materials/Skyboxes/Mat_NightMoon_Skybox.mat";

    public static object Apply()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(SkyboxTexPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Skyboxes")) AssetDatabase.CreateFolder("Assets/Materials", "Skyboxes");

        Cubemap cube = AssetDatabase.LoadAssetAtPath<Cubemap>(SkyboxTexPath);
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMatPath);
        if (skyMat == null)
        {
            Shader shader = Shader.Find("Skybox/Cubemap");
            skyMat = new Material(shader);
            AssetDatabase.CreateAsset(skyMat, SkyboxMatPath);
        }

        if (cube != null && skyMat.HasProperty("_Tex")) skyMat.SetTexture("_Tex", cube);
        if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", 0.90f);
        if (skyMat.HasProperty("_Tint")) skyMat.SetColor("_Tint", new Color(0.50f, 0.54f, 0.62f, 1f));
        EditorUtility.SetDirty(skyMat);

        PS2VisualSetupBuilder.ApplyPS2Style();
        RenderSettings.skybox = skyMat;
        DynamicGI.UpdateEnvironment();

        var scene = EditorSceneManager.GetActiveScene();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return new
        {
            ok = cube != null && RenderSettings.skybox == skyMat,
            hasCubemap = cube != null,
            skyboxName = RenderSettings.skybox != null ? RenderSettings.skybox.name : "null"
        };
    }
}
