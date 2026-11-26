using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using ProjectRac.Interaction;

namespace ProjectRac.Editor.Map
{
    public static class MansionDoorFactory
    {
        private const string MaterialsFolder = "Assets/Materials/Arena";
        private const float FrameThickness = 0.07f;

        private static readonly Color WallColor = new Color(0.68f, 0.67f, 0.64f, 1f);
        private static readonly Color FrameColor = new Color(0.24f, 0.17f, 0.12f, 1f);
        private static readonly Color DoorColor = new Color(0.46f, 0.29f, 0.16f, 1f);
        private static readonly Color HandleColor = new Color(0.82f, 0.72f, 0.36f, 1f);

        public static void BuildWall(Transform parent, WallDef def, float floorSurfaceY)
        {
            Vector2 delta = def.End - def.Start;
            float totalLength = delta.magnitude;
            if (totalLength < 0.05f) return;

            float yaw = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            // Local +X runs along the wall from Start to End; local +Z is perpendicular normal.
            Quaternion wallRot = Quaternion.Euler(0f, yaw - 90f, 0f);
            Vector3 startPos = new Vector3(def.Start.x, floorSurfaceY, def.Start.y);

            if (def.Opening == OpeningKind.None)
            {
                Vector3 center = startPos + (wallRot * new Vector3(totalLength * 0.5f, MansionLayoutData.FloorHeight * 0.5f, 0f));
                CreateBox(parent, def.Name, center, wallRot,
                    new Vector3(totalLength + MansionLayoutData.WallThickness, MansionLayoutData.FloorHeight, MansionLayoutData.WallThickness),
                    WallColor);
                return;
            }

            GameObject wallGroup = new GameObject(def.Name);
            wallGroup.transform.SetParent(parent, false);
            wallGroup.transform.SetLocalPositionAndRotation(startPos, wallRot);

            float halfOpen = def.OpeningWidth * 0.5f;
            float openStart = Mathf.Clamp(def.OpeningOffsetFromStart - halfOpen, 0.1f, totalLength - def.OpeningWidth - 0.1f);
            float openEnd = openStart + def.OpeningWidth;
            float wallH = MansionLayoutData.FloorHeight;
            float wallT = MansionLayoutData.WallThickness;
            float openH = def.Opening == OpeningKind.Archway ? 2.85f : MansionLayoutData.DoorHeight;

            if (openStart > 0.02f)
            {
                float len = openStart + wallT * 0.5f;
                CreateLocalBox(wallGroup.transform, def.Name + "_Left",
                    new Vector3(openStart - len * 0.5f, wallH * 0.5f, 0f),
                    new Vector3(len, wallH, wallT), WallColor);
            }

            float rightSpan = totalLength - openEnd;
            if (rightSpan > 0.02f)
            {
                float len = rightSpan + wallT * 0.5f;
                CreateLocalBox(wallGroup.transform, def.Name + "_Right",
                    new Vector3(openEnd + len * 0.5f, wallH * 0.5f, 0f),
                    new Vector3(len, wallH, wallT), WallColor);
            }

            float lintelH = wallH - openH;
            if (lintelH > 0.02f)
            {
                CreateLocalBox(wallGroup.transform, def.Name + "_Lintel",
                    new Vector3((openStart + openEnd) * 0.5f, openH + lintelH * 0.5f, 0f),
                    new Vector3(def.OpeningWidth, lintelH, wallT), WallColor);
            }

            if (def.Opening == OpeningKind.Door)
            {
                string doorName = string.IsNullOrEmpty(def.DoorName) ? def.Name + "_Door" : def.DoorName;
                BuildDoorUnit(wallGroup.transform, doorName, (openStart + openEnd) * 0.5f, def.OpeningWidth, openH, wallT);
            }
        }

        private static void BuildDoorUnit(Transform wallParent, string doorName, float localCenterX, float openW, float openH, float wallT)
        {
            GameObject doorRoot = new GameObject(doorName);
            doorRoot.transform.SetParent(wallParent, false);
            doorRoot.transform.localPosition = new Vector3(localCenterX, 0f, 0f);

            float frameDepth = wallT + 0.06f;
            float jambX = (openW * 0.5f) - (FrameThickness * 0.5f);

            CreateLocalBox(doorRoot.transform, "Frame_Left", new Vector3(-jambX, openH * 0.5f, 0f),
                new Vector3(FrameThickness, openH, frameDepth), FrameColor, removeCollider: true);
            CreateLocalBox(doorRoot.transform, "Frame_Right", new Vector3(jambX, openH * 0.5f, 0f),
                new Vector3(FrameThickness, openH, frameDepth), FrameColor, removeCollider: true);
            CreateLocalBox(doorRoot.transform, "Frame_Top", new Vector3(0f, openH - FrameThickness * 0.5f, 0f),
                new Vector3(openW, FrameThickness, frameDepth), FrameColor, removeCollider: true);

            float leafW = openW - (FrameThickness * 2f) - 0.02f;
            float leafH = openH - FrameThickness - 0.02f;
            float leafT = 0.10f;

            GameObject pivot = new GameObject("Door_Pivot");
            pivot.transform.SetParent(doorRoot.transform, false);
            pivot.transform.localPosition = new Vector3(-leafW * 0.5f, 0f, 0f);

            GameObject leaf = CreateLocalBox(pivot.transform, "Door_Leaf",
                new Vector3(leafW * 0.5f, leafH * 0.5f, 0f),
                new Vector3(leafW, leafH, leafT), DoorColor);

            var modifier = leaf.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;

            var obstacle = leaf.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;

            var controller = leaf.AddComponent<DoorController>();
            controller.Configure(pivot.transform, obstacle);

            float handleX = leafW - 0.14f;
            float handleZ = (leafT * 0.5f) + 0.04f;
            CreateHandle(pivot.transform, "Handle_Front", new Vector3(handleX, 1.02f, handleZ));
            CreateHandle(pivot.transform, "Handle_Back", new Vector3(handleX, 1.02f, -handleZ));
        }

        private static void CreateHandle(Transform parent, string name, Vector3 localPos)
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = name;
            handle.transform.SetParent(parent, false);
            handle.transform.localPosition = localPos;
            handle.transform.localScale = new Vector3(0.075f, 0.075f, 0.075f);
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            ApplyPersistentColor(handle, HandleColor);
        }

        public static GameObject CreateBox(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.SetLocalPositionAndRotation(pos, rot);
            obj.transform.localScale = scale;
            ApplyPersistentColor(obj, color);
            return obj;
        }

        public static GameObject CreateLocalBox(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 localScale,
            Color color,
            bool removeCollider = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            if (removeCollider) Object.DestroyImmediate(obj.GetComponent<Collider>());
            ApplyPersistentColor(obj, color);
            return obj;
        }

        public static void ApplyPersistentColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            string key = ColorUtility.ToHtmlStringRGB(color);
            string assetPath = $"{MaterialsFolder}/Mat_Arena_{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
                if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Arena");

                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            renderer.sharedMaterial = mat;
        }
    }
}
