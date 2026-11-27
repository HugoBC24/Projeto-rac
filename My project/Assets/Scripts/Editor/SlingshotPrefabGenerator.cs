using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProjectRac.Editor
{
    public static class SlingshotPrefabGenerator
    {
        public const string PrefabPath = "Assets/Prefabs/Slingshot_Viewmodel.prefab";
        public const string ArmFbxPath = "Assets/Models/Player/BracoProta_Arm.fbx";
        public const string HandTexturePath = "Assets/Models/Player/Textures/faceProta.png";

        private const string WeaponMaterialsFolder = "Assets/Materials/Weapons";
        private const string PlayerMaterialsFolder = "Assets/Materials/Player";

        [MenuItem("Tools/Generate Slingshot Prefab")]
        public static GameObject GeneratePrefab()
        {
            EnsureFolder("Assets/Prefabs");

            Material woodMat = GetOrCreateUrpMaterial($"{WeaponMaterialsFolder}/Mat_Slingshot_Wood.mat", new Color(0.48f, 0.28f, 0.15f), 0.18f);
            Material rubberMat = GetOrCreateUrpMaterial($"{WeaponMaterialsFolder}/Mat_Slingshot_Rubber.mat", new Color(0.84f, 0.68f, 0.26f), 0.22f);
            Material leatherMat = GetOrCreateUrpMaterial($"{WeaponMaterialsFolder}/Mat_Slingshot_Leather.mat", new Color(0.24f, 0.14f, 0.09f), 0.15f);

            GameObject root = new GameObject("Slingshot_Viewmodel");

            // Handle (Wooden)
            CreateSlingshotPart(root.transform, PrimitiveType.Cylinder, "Handle",
                new Vector3(0f, -0.10f, 0f), Quaternion.identity, new Vector3(0.045f, 0.12f, 0.045f), woodMat);

            // Left & Right Fork Arms
            CreateSlingshotPart(root.transform, PrimitiveType.Cylinder, "Arm_Left",
                new Vector3(-0.065f, 0.08f, 0f), Quaternion.Euler(0f, 0f, 28f), new Vector3(0.035f, 0.09f, 0.035f), woodMat);
            CreateSlingshotPart(root.transform, PrimitiveType.Cylinder, "Arm_Right",
                new Vector3(0.065f, 0.08f, 0f), Quaternion.Euler(0f, 0f, -28f), new Vector3(0.035f, 0.09f, 0.035f), woodMat);

            // Elastic Bands (Left & Right)
            CreateSlingshotPart(root.transform, PrimitiveType.Cube, "Band_Left",
                new Vector3(-0.06f, 0.14f, -0.06f), Quaternion.Euler(-18f, -14f, 0f), new Vector3(0.015f, 0.015f, 0.14f), rubberMat);
            CreateSlingshotPart(root.transform, PrimitiveType.Cube, "Band_Right",
                new Vector3(0.06f, 0.14f, -0.06f), Quaternion.Euler(-18f, 14f, 0f), new Vector3(0.015f, 0.015f, 0.14f), rubberMat);

            // Leather Pouch
            CreateSlingshotPart(root.transform, PrimitiveType.Cube, "LeatherPouch",
                new Vector3(0f, 0.12f, -0.12f), Quaternion.identity, new Vector3(0.045f, 0.035f, 0.02f), leatherMat);

            // Attach protagonist's right arm holding the wooden handle
            AttachPlayerArm(root.transform);

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SlingshotPrefabGenerator] Prefab atualizado com o braco do protagonista em: {PrefabPath}");
            return prefabAsset;
        }

        private static void AttachPlayerArm(Transform slingshotRoot)
        {
            GameObject armModel = AssetDatabase.LoadAssetAtPath<GameObject>(ArmFbxPath);
            if (armModel == null)
            {
                Debug.LogWarning($"[SlingshotPrefabGenerator] Modelo do braco nao encontrado em {ArmFbxPath}.");
                return;
            }

            MeshFilter srcFilter = armModel.GetComponentInChildren<MeshFilter>();
            if (srcFilter == null || srcFilter.sharedMesh == null)
            {
                Debug.LogWarning("[SlingshotPrefabGenerator] Nenhuma malha encontrada em BracoProta_Arm.fbx.");
                return;
            }

            Texture2D handTex = AssetDatabase.LoadAssetAtPath<Texture2D>(HandTexturePath);
            Material sleeveMat = GetOrCreateUrpMaterial($"{PlayerMaterialsFolder}/Mat_PlayerArm_Sleeve.mat", new Color(0.84f, 0.84f, 0.86f), 0.12f);
            Material handMat = GetOrCreateUrpMaterial($"{PlayerMaterialsFolder}/Mat_PlayerArm_Hand.mat", Color.white, 0.14f, handTex);

            GameObject armObj = new GameObject("PlayerArm_Right");
            armObj.transform.SetParent(slingshotRoot, false);
            // Hand wraps cleanly around the wooden Handle (0, -0.10, 0); white sleeve extends visibly to bottom-right.
            armObj.transform.localPosition = new Vector3(0.025f, -0.11f, -0.045f);
            armObj.transform.localRotation = Quaternion.Euler(-8f, -14f, 6f);
            armObj.transform.localScale = Vector3.one;

            var mf = armObj.AddComponent<MeshFilter>();
            mf.sharedMesh = srcFilter.sharedMesh;

            var mr = armObj.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { sleeveMat, handMat };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void CreateSlingshotPart(
            Transform parent, PrimitiveType type, string name,
            Vector3 localPos, Quaternion localRot, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = localRot;
            obj.transform.localScale = localScale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());

            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static Material GetOrCreateUrpMaterial(string assetPath, Color baseColor, float smoothness, Texture2D mainTex = null)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(folder)) EnsureFolder(folder);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            else mat.color = baseColor;

            if (mainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
