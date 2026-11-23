using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using ProjectRac.Interaction;

namespace ProjectRac.Editor
{
    public static class DoorArenaBuilder
    {
        public const string WallRootName = "WallWithDoor";

        private static readonly Vector3 DefaultCenterPosition = new Vector3(0f, 0f, 6f);
        private const float TotalWallWidth = 10f;
        private const float WallHeight = 4f;
        private const float WallThickness = 0.4f;
        private const float DoorOpeningWidth = 1.6f;
        private const float DoorOpeningHeight = 2.6f;
        private const float FrameThickness = 0.08f;

        public static GameObject BuildWallWithDoor(Transform arenaRoot, Vector3? positionOverride = null)
        {
            Transform existing = arenaRoot.Find(WallRootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject root = new GameObject(WallRootName);
            root.transform.SetParent(arenaRoot, false);
            root.transform.position = positionOverride ?? DefaultCenterPosition;
            Undo.RegisterCreatedObjectUndo(root, "Create Wall With Door");

            Color wallColor = new Color(0.55f, 0.56f, 0.59f, 1f);
            Color frameColor = new Color(0.24f, 0.17f, 0.12f, 1f);
            Color doorColor = new Color(0.46f, 0.29f, 0.16f, 1f);
            Color handleColor = new Color(0.82f, 0.72f, 0.36f, 1f);

            float sideWidth = (TotalWallWidth - DoorOpeningWidth) * 0.5f;
            float sideCenterX = (DoorOpeningWidth * 0.5f) + (sideWidth * 0.5f);

            CreateSegment(
                root.transform,
                "Wall_Left",
                new Vector3(-sideCenterX, WallHeight * 0.5f, 0f),
                new Vector3(sideWidth, WallHeight, WallThickness),
                wallColor);

            CreateSegment(
                root.transform,
                "Wall_Right",
                new Vector3(sideCenterX, WallHeight * 0.5f, 0f),
                new Vector3(sideWidth, WallHeight, WallThickness),
                wallColor);

            float lintelHeight = WallHeight - DoorOpeningHeight;
            CreateSegment(
                root.transform,
                "Wall_Lintel",
                new Vector3(0f, DoorOpeningHeight + lintelHeight * 0.5f, 0f),
                new Vector3(DoorOpeningWidth, lintelHeight, WallThickness),
                wallColor);

            BuildDoorFrame(root.transform, frameColor);
            BuildDoorAssembly(root.transform, doorColor, handleColor);

            return root;
        }

        private static void BuildDoorFrame(Transform parent, Color frameColor)
        {
            GameObject frameRoot = new GameObject("DoorFrame");
            frameRoot.transform.SetParent(parent, false);

            float frameDepth = WallThickness + 0.06f;
            float jambX = (DoorOpeningWidth * 0.5f) - (FrameThickness * 0.5f);

            CreateSegment(
                frameRoot.transform,
                "Frame_Left",
                new Vector3(-jambX, DoorOpeningHeight * 0.5f, 0f),
                new Vector3(FrameThickness, DoorOpeningHeight, frameDepth),
                frameColor,
                removeCollider: true);

            CreateSegment(
                frameRoot.transform,
                "Frame_Right",
                new Vector3(jambX, DoorOpeningHeight * 0.5f, 0f),
                new Vector3(FrameThickness, DoorOpeningHeight, frameDepth),
                frameColor,
                removeCollider: true);

            CreateSegment(
                frameRoot.transform,
                "Frame_Top",
                new Vector3(0f, DoorOpeningHeight - (FrameThickness * 0.5f), 0f),
                new Vector3(DoorOpeningWidth, FrameThickness, frameDepth),
                frameColor,
                removeCollider: true);
        }

        private static void BuildDoorAssembly(Transform parent, Color doorColor, Color handleColor)
        {
            float leafWidth = DoorOpeningWidth - (FrameThickness * 2f) - 0.02f;
            float leafHeight = DoorOpeningHeight - FrameThickness - 0.02f;
            float leafThickness = 0.12f;

            // Pivot sits at the inner edge of the left jamb so rotation swings cleanly around the hinge.
            GameObject pivot = new GameObject("Door_Pivot");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = new Vector3(-(leafWidth * 0.5f), 0f, 0f);

            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Door_Leaf";
            leaf.transform.SetParent(pivot.transform, false);
            leaf.transform.localPosition = new Vector3(leafWidth * 0.5f, leafHeight * 0.5f, 0f);
            leaf.transform.localScale = new Vector3(leafWidth, leafHeight, leafThickness);
            ApplyColor(leaf, doorColor);

            // Exclude the moving door leaf from static NavMesh baking so the doorway stays walkable
            // while NavMeshObstacle handles dynamic carving whenever the door is closed.
            var navModifier = leaf.AddComponent<NavMeshModifier>();
            navModifier.ignoreFromBuild = true;

            var obstacle = leaf.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;

            var doorController = leaf.AddComponent<DoorController>();
            doorController.Configure(pivot.transform, obstacle);

            // Place handles on the latch side (opposite the hinge) on both faces of the leaf.
            float handleLocalX = leafWidth - 0.16f;
            float handleLocalY = 1.05f;
            float handleOffsetZ = (leafThickness * 0.5f) + 0.045f;

            CreateHandle(pivot.transform, "Door_Handle_Front", new Vector3(handleLocalX, handleLocalY, handleOffsetZ), handleColor);
            CreateHandle(pivot.transform, "Door_Handle_Back", new Vector3(handleLocalX, handleLocalY, -handleOffsetZ), handleColor);
        }

        private static void CreateHandle(Transform parent, string name, Vector3 localPos, Color color)
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = name;
            handle.transform.SetParent(parent, false);
            handle.transform.localPosition = localPos;
            handle.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            ApplyColor(handle, color);
        }

        private static GameObject CreateSegment(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 localScale,
            Color color,
            bool removeCollider = false)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.SetParent(parent, false);
            segment.transform.localPosition = localPos;
            segment.transform.localScale = localScale;

            if (removeCollider)
            {
                Object.DestroyImmediate(segment.GetComponent<Collider>());
            }

            ApplyColor(segment, color);
            return segment;
        }

        private const string MaterialsFolder = "Assets/Materials/Arena";

        private static void ApplyColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            string key = ColorUtility.ToHtmlStringRGB(color);
            renderer.sharedMaterial = GetOrCreateMaterial($"Mat_Arena_{key}", color);
        }

        private static Material GetOrCreateMaterial(string matName, Color color)
        {
            string assetPath = $"{MaterialsFolder}/{matName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;

            EnsureFolder(MaterialsFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;

            AssetDatabase.CreateAsset(mat, assetPath);
            return mat;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            AssetDatabase.CreateFolder("Assets/Materials", "Arena");
        }
    }
}
