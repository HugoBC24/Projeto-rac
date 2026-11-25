using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectRac.Editor.Map
{
    public static class MansionColliderFixer
    {
        private const string MeshFolder = "Assets/Materials/Arena";
        private const string RampMeshPath = MeshFolder + "/StairRampMesh.asset";
        private const string HandrailMeshPath = MeshFolder + "/StairHandrailMesh.asset";

        [MenuItem("Tools/Fix Mansion Colliders After Resize")]
        public static bool FixMansionColliders()
        {
            GameObject mansion = GameObject.Find(MansionMapBuilder.MansionRootName);
            if (mansion == null)
            {
                Debug.LogWarning("[MansionColliderFixer] MansionMap nao encontrado na cena.");
                return false;
            }

            Transform stairs = mansion.transform.Find("Staircase");
            if (stairs == null)
            {
                Debug.LogWarning("[MansionColliderFixer] Staircase nao encontrado dentro de MansionMap.");
                return false;
            }

            // Compute exact local-space stair bounds from visual steps so the unrotated convex mesh
            // scales cleanly with ANY non-uniform parent scale (zero PhysX BoxCollider shear).
            if (!TryComputeLocalStairBounds(stairs, out Vector3 minLocal, out Vector3 maxLocal))
            {
                minLocal = new Vector3(
                    MansionLayoutData.StairCenterX - MansionLayoutData.StairWidth * 0.5f,
                    0f,
                    MansionLayoutData.StairStartZ);
                maxLocal = new Vector3(
                    MansionLayoutData.StairCenterX + MansionLayoutData.StairWidth * 0.5f,
                    MansionLayoutData.Floor2SurfaceY,
                    MansionLayoutData.StairEndZ);
            }

            RebuildShearFreeRampCollider(stairs, minLocal, maxLocal);
            RebuildShearFreeHandrail(stairs, minLocal, maxLocal);

            Physics.SyncTransforms();

            // Rebake MansionMap NavMeshSurface to match the resized scale and fixed stair ramp.
            var surface = mansion.GetComponent<NavMeshSurface>();
            if (surface == null) surface = mansion.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MansionColliderFixer] Colisores da escada e NavMesh ajustados sem cisalhamento.");
            return true;
        }

        private static bool TryComputeLocalStairBounds(Transform stairs, out Vector3 minLocal, out Vector3 maxLocal)
        {
            minLocal = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            maxLocal = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool foundAny = false;

            for (int i = 0; i < stairs.childCount; i++)
            {
                Transform child = stairs.GetChild(i);
                if (!child.name.StartsWith("Step_")) continue;

                Vector3 half = child.localScale * 0.5f;
                Vector3 c = child.localPosition;
                minLocal = Vector3.Min(minLocal, c - half);
                maxLocal = Vector3.Max(maxLocal, c + half);
                foundAny = true;
            }

            return foundAny;
        }

        private static void RebuildShearFreeRampCollider(Transform stairs, Vector3 minLocal, Vector3 maxLocal)
        {
            Transform existing = stairs.Find("Stair_RampCollider");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            Mesh wedgeMesh = CreateOrReplaceAssetMesh(RampMeshPath, BuildWedgeMesh(minLocal, maxLocal));

            GameObject rampObj = new GameObject("Stair_RampCollider");
            rampObj.transform.SetParent(stairs, false);
            rampObj.transform.localPosition = Vector3.zero;
            rampObj.transform.localRotation = Quaternion.identity;
            rampObj.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(rampObj, "Create Shear-Free Stair Ramp Collider");

            var meshCol = rampObj.AddComponent<MeshCollider>();
            meshCol.sharedMesh = wedgeMesh;
            meshCol.convex = true;
        }

        private static void RebuildShearFreeHandrail(Transform stairs, Vector3 minLocal, Vector3 maxLocal)
        {
            Transform existing = stairs.Find("Stair_Handrail");
            Material preservedMat = existing != null ? existing.GetComponent<Renderer>()?.sharedMaterial : null;
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            float railCenterX = maxLocal.x + 0.04f;
            float halfW = 0.04f;
            float railH = 0.10f;
            float railOffsetY = 0.88f;

            Mesh railMesh = CreateOrReplaceAssetMesh(
                HandrailMeshPath,
                BuildInclinedBeamMesh(railCenterX - halfW, railCenterX + halfW, minLocal.y + railOffsetY, maxLocal.y + railOffsetY, railH, minLocal.z, maxLocal.z));

            GameObject railObj = new GameObject("Stair_Handrail");
            railObj.transform.SetParent(stairs, false);
            railObj.transform.localPosition = Vector3.zero;
            railObj.transform.localRotation = Quaternion.identity;
            railObj.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(railObj, "Create Shear-Free Stair Handrail");

            railObj.AddComponent<MeshFilter>().sharedMesh = railMesh;
            var mr = railObj.AddComponent<MeshRenderer>();
            if (preservedMat != null) mr.sharedMaterial = preservedMat;
            else MansionDoorFactory.ApplyPersistentColor(railObj, new Color(0.24f, 0.17f, 0.12f, 1f));

            var mc = railObj.AddComponent<MeshCollider>();
            mc.sharedMesh = railMesh;
            mc.convex = true;
        }

        private static Mesh BuildWedgeMesh(Vector3 min, Vector3 max)
        {
            // 6-vertex solid right-triangular wedge aligned from bottom-front (min.z, min.y) to top-back (max.z, max.y).
            Mesh mesh = new Mesh { name = "StairRampMesh" };
            mesh.vertices = new[]
            {
                new Vector3(min.x, min.y, min.z), // 0: front-left bottom
                new Vector3(max.x, min.y, min.z), // 1: front-right bottom
                new Vector3(min.x, min.y, max.z), // 2: back-left bottom
                new Vector3(max.x, min.y, max.z), // 3: back-right bottom
                new Vector3(min.x, max.y, max.z), // 4: back-left top
                new Vector3(max.x, max.y, max.z)  // 5: back-right top
            };
            mesh.triangles = new[]
            {
                0, 4, 5,  0, 5, 1, // Top walkable slope
                0, 1, 3,  0, 3, 2, // Bottom
                2, 3, 5,  2, 5, 4, // Back vertical
                0, 2, 4,           // Left triangle
                1, 5, 3            // Right triangle
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildInclinedBeamMesh(float minX, float maxX, float startY, float endY, float h, float minZ, float maxZ)
        {
            Mesh mesh = new Mesh { name = "StairHandrailMesh" };
            mesh.vertices = new[]
            {
                new Vector3(minX, startY, minZ),     // 0
                new Vector3(maxX, startY, minZ),     // 1
                new Vector3(minX, startY + h, minZ), // 2
                new Vector3(maxX, startY + h, minZ), // 3
                new Vector3(minX, endY, maxZ),       // 4
                new Vector3(maxX, endY, maxZ),       // 5
                new Vector3(minX, endY + h, maxZ),   // 6
                new Vector3(maxX, endY + h, maxZ)    // 7
            };
            mesh.triangles = new[]
            {
                2, 6, 7,  2, 7, 3, // Top
                0, 1, 5,  0, 5, 4, // Bottom
                0, 2, 3,  0, 3, 1, // Front
                4, 5, 7,  4, 7, 6, // Back
                0, 4, 6,  0, 6, 2, // Left
                1, 3, 7,  1, 7, 5  // Right
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOrReplaceAssetMesh(string assetPath, Mesh mesh)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Arena");

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                existing.Clear();
                existing.vertices = mesh.vertices;
                existing.triangles = mesh.triangles;
                existing.RecalculateNormals();
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }
    }
}
