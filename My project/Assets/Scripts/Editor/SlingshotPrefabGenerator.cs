using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProjectRac.Editor
{
    public static class SlingshotPrefabGenerator
    {
        public const string PrefabPath = "Assets/Prefabs/Slingshot_Viewmodel.prefab";

        [MenuItem("Tools/Generate Slingshot Prefab")]
        public static GameObject GeneratePrefab()
        {
            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
                AssetDatabase.Refresh();
            }

            GameObject root = new GameObject("Slingshot_Viewmodel");

            // Handle (Wooden)
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            handle.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f);
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            // Left Fork Arm
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftArm.name = "Arm_Left";
            leftArm.transform.SetParent(root.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.065f, 0.08f, 0f);
            leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            leftArm.transform.localScale = new Vector3(0.035f, 0.09f, 0.035f);
            Object.DestroyImmediate(leftArm.GetComponent<Collider>());

            // Right Fork Arm
            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightArm.name = "Arm_Right";
            rightArm.transform.SetParent(root.transform, false);
            rightArm.transform.localPosition = new Vector3(0.065f, 0.08f, 0f);
            rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            rightArm.transform.localScale = new Vector3(0.035f, 0.09f, 0.035f);
            Object.DestroyImmediate(rightArm.GetComponent<Collider>());

            // Elastic Bands (Left & Right)
            GameObject leftBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftBand.name = "Band_Left";
            leftBand.transform.SetParent(root.transform, false);
            leftBand.transform.localPosition = new Vector3(-0.06f, 0.14f, -0.06f);
            leftBand.transform.localRotation = Quaternion.Euler(-18f, -14f, 0f);
            leftBand.transform.localScale = new Vector3(0.015f, 0.015f, 0.14f);
            Object.DestroyImmediate(leftBand.GetComponent<Collider>());

            GameObject rightBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightBand.name = "Band_Right";
            rightBand.transform.SetParent(root.transform, false);
            rightBand.transform.localPosition = new Vector3(0.06f, 0.14f, -0.06f);
            rightBand.transform.localRotation = Quaternion.Euler(-18f, 14f, 0f);
            rightBand.transform.localScale = new Vector3(0.015f, 0.015f, 0.14f);
            Object.DestroyImmediate(rightBand.GetComponent<Collider>());

            // Leather Pouch
            GameObject pouch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pouch.name = "LeatherPouch";
            pouch.transform.SetParent(root.transform, false);
            pouch.transform.localPosition = new Vector3(0f, 0.12f, -0.12f);
            pouch.transform.localScale = new Vector3(0.045f, 0.035f, 0.02f);
            Object.DestroyImmediate(pouch.GetComponent<Collider>());

            // Materials & Colors
            var woodBlock = new MaterialPropertyBlock();
            woodBlock.SetColor("_BaseColor", new Color(0.55f, 0.32f, 0.17f));
            handle.GetComponent<Renderer>()?.SetPropertyBlock(woodBlock);
            leftArm.GetComponent<Renderer>()?.SetPropertyBlock(woodBlock);
            rightArm.GetComponent<Renderer>()?.SetPropertyBlock(woodBlock);

            var rubberBlock = new MaterialPropertyBlock();
            rubberBlock.SetColor("_BaseColor", new Color(0.88f, 0.72f, 0.28f));
            leftBand.GetComponent<Renderer>()?.SetPropertyBlock(rubberBlock);
            rightBand.GetComponent<Renderer>()?.SetPropertyBlock(rubberBlock);

            var leatherBlock = new MaterialPropertyBlock();
            leatherBlock.SetColor("_BaseColor", new Color(0.28f, 0.16f, 0.10f));
            pouch.GetComponent<Renderer>()?.SetPropertyBlock(leatherBlock);

            // Save as Prefab Asset
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SlingshotPrefabGenerator] Prefab criado com sucesso em: {PrefabPath}");
            return prefabAsset;
        }
    }
}
