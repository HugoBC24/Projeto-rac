using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectRac.Editor;
using ProjectRac.Weapons;

public static class ApplyPlayerArmSlingshotInScene
{
    public static object Apply()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(SlingshotPrefabGenerator.ArmFbxPath) as ModelImporter;
        if (importer != null)
        {
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        GameObject prefab = SlingshotPrefabGenerator.GeneratePrefab();
        if (prefab == null)
        {
            return new { ok = false, error = "Failed to generate Slingshot_Viewmodel prefab." };
        }

        var armChild = prefab.transform.Find("PlayerArm_Right");
        var armMesh = armChild != null ? armChild.GetComponent<MeshFilter>()?.sharedMesh : null;

        GameObject player = GameObject.Find("Player");
        SlingshotWeapon slingshot = player != null ? player.GetComponentInChildren<SlingshotWeapon>(true) : null;
        if (slingshot != null)
        {
            slingshot.SetCustomViewmodelPrefab(prefab);
            var so = new SerializedObject(slingshot);
            var offsetProp = so.FindProperty("viewmodelOffset");
            if (offsetProp != null) offsetProp.vector3Value = new Vector3(0.23f, -0.14f, 0.54f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slingshot);
        }

        Camera cam = player != null ? player.GetComponentInChildren<Camera>(true) : Camera.main;
        if (cam != null)
        {
            cam.nearClipPlane = 0.05f;
            EditorUtility.SetDirty(cam);
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return new
        {
            ok = armChild != null && armMesh != null,
            hasArmChild = armChild != null,
            armBoundsMin = armMesh != null ? armMesh.bounds.min.ToString("F3") : "null",
            armBoundsMax = armMesh != null ? armMesh.bounds.max.ToString("F3") : "null",
            armVertexCount = armMesh != null ? armMesh.vertexCount : 0,
            armSubMeshCount = armMesh != null ? armMesh.subMeshCount : 0,
            nearClipPlane = cam != null ? cam.nearClipPlane : -1f,
            slingshotBound = slingshot != null
        };
    }
}
