using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectRac.Player;

namespace ProjectRac.Editor.Map
{
    public static class MansionMapBuilder
    {
        public const string MansionRootName = "MansionMap";

        private static readonly Color Floor1Color = new Color(0.52f, 0.50f, 0.47f, 1f);
        private static readonly Color Floor2Color = new Color(0.56f, 0.49f, 0.41f, 1f);
        private static readonly Color RoofColor = new Color(0.36f, 0.36f, 0.39f, 1f);
        private static readonly Color StairColor = new Color(0.42f, 0.31f, 0.22f, 1f);
        private static readonly Color RailingColor = new Color(0.24f, 0.17f, 0.12f, 1f);

        [MenuItem("Tools/Build Mansion Map (Center)")]
        public static GameObject BuildAndRelocateTestArena()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Move existing TestArena cleanly to the side so the scene center is free.
            RelocateTestArena();

            // 2. Create fresh MansionMap centered at (0, 0, 0).
            GameObject existing = GameObject.Find(MansionRootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(MansionRootName);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Create Mansion Map");

            // 3. Floor 1 (ground level, y = 0)
            GameObject floor1Root = CreateGroup(root.transform, "Floor1");
            BuildSlabs(floor1Root.transform, MansionLayoutData.Floor1Slabs, -MansionLayoutData.SlabThickness * 0.5f, Floor1Color);
            foreach (var wall in MansionLayoutData.Floor1Walls)
            {
                MansionDoorFactory.BuildWall(floor1Root.transform, wall, 0f);
            }

            // 4. Staircase rising inside HALL from y = 0 to y = Floor2SurfaceY
            BuildStaircase(root.transform);

            // 5. Floor 2 (upper level, walkable surface at y = Floor2SurfaceY = 3.85m)
            GameObject floor2Root = CreateGroup(root.transform, "Floor2");
            float floor2SlabCenterY = MansionLayoutData.FloorHeight + (MansionLayoutData.SlabThickness * 0.5f);
            BuildSlabs(floor2Root.transform, MansionLayoutData.Floor2Slabs, floor2SlabCenterY, Floor2Color);
            foreach (var wall in MansionLayoutData.Floor2Walls)
            {
                MansionDoorFactory.BuildWall(floor2Root.transform, wall, MansionLayoutData.Floor2SurfaceY);
            }

            // 6. Separable Roof (easy to toggle off in Hierarchy for top-down inspection)
            GameObject roofRoot = CreateGroup(root.transform, "Roof");
            float roofCenterY = MansionLayoutData.Floor2SurfaceY + MansionLayoutData.FloorHeight + (MansionLayoutData.SlabThickness * 0.5f);
            BuildSlabs(roofRoot.transform, MansionLayoutData.RoofSlabs, roofCenterY, RoofColor);

            // 7. Invisible ceiling point lights (zero meshes/props inside rooms)
            BuildInteriorLighting(root.transform);

            // 8. Bake dedicated NavMesh on MansionMap (Children only, isolated from TestArena)
            BakeChildrenNavMesh(root);

            // 9. Position Player inside the 1st-floor HALL facing North (+Z)
            PositionPlayerInHall();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[MansionMapBuilder] Mansao de 2 andares montada em (0,0,0) e TestArena movida para (75,0,0).");
            return root;
        }

        private static void BuildSlabs(Transform parent, SlabRect[] rects, float centerY, Color color)
        {
            for (int i = 0; i < rects.Length; i++)
            {
                SlabRect r = rects[i];
                float sizeX = r.MaxX - r.MinX;
                float sizeZ = r.MaxZ - r.MinZ;
                Vector3 pos = new Vector3((r.MinX + r.MaxX) * 0.5f, centerY, (r.MinZ + r.MaxZ) * 0.5f);
                Vector3 scale = new Vector3(sizeX, MansionLayoutData.SlabThickness, sizeZ);
                MansionDoorFactory.CreateLocalBox(parent, r.Name, pos, scale, color);
            }
        }

        private static void BuildStaircase(Transform root)
        {
            GameObject stairsRoot = CreateGroup(root, "Staircase");
            int steps = MansionLayoutData.StairStepCount;
            float totalRun = MansionLayoutData.StairEndZ - MansionLayoutData.StairStartZ;
            float totalRise = MansionLayoutData.Floor2SurfaceY;
            float stepDepth = totalRun / steps;
            float stepRise = totalRise / steps;
            float width = MansionLayoutData.StairWidth;
            float centerX = MansionLayoutData.StairCenterX;

            // Visual steps without individual colliders (smooth ramp collider below handles FPS movement).
            for (int i = 0; i < steps; i++)
            {
                float topY = (i + 1) * stepRise;
                float zCenter = MansionLayoutData.StairStartZ + (i + 0.5f) * stepDepth;
                MansionDoorFactory.CreateLocalBox(
                    stairsRoot.transform,
                    $"Step_{i + 1:00}",
                    new Vector3(centerX, topY * 0.5f, zCenter),
                    new Vector3(width, topY, stepDepth + 0.01f),
                    StairColor,
                    removeCollider: true);
            }

            // Continuous ramp collider aligned with the step nosings so walking/sprinting up has zero camera jitter.
            float slopeLength = Mathf.Sqrt(totalRun * totalRun + totalRise * totalRise);
            float pitchDeg = Mathf.Atan2(totalRise, totalRun) * Mathf.Rad2Deg;
            float rampThickness = 0.20f;

            GameObject rampColliderObj = new GameObject("Stair_RampCollider");
            rampColliderObj.transform.SetParent(stairsRoot.transform, false);
            Quaternion rampRot = Quaternion.Euler(-pitchDeg, 0f, 0f);
            Vector3 rampMid = new Vector3(centerX, totalRise * 0.5f, (MansionLayoutData.StairStartZ + MansionLayoutData.StairEndZ) * 0.5f);
            rampColliderObj.transform.SetPositionAndRotation(rampMid + (rampRot * new Vector3(0f, -rampThickness * 0.5f, 0f)), rampRot);
            var rampBox = rampColliderObj.AddComponent<BoxCollider>();
            rampBox.size = new Vector3(width, rampThickness, slopeLength);

            // Inner handrail along the open (East) side of the staircase.
            float railX = centerX + (width * 0.5f) + 0.04f;
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Stair_Handrail";
            rail.transform.SetParent(stairsRoot.transform, false);
            rail.transform.SetPositionAndRotation(
                new Vector3(railX, (totalRise * 0.5f) + 0.92f, (MansionLayoutData.StairStartZ + MansionLayoutData.StairEndZ) * 0.5f),
                rampRot);
            rail.transform.localScale = new Vector3(0.08f, 0.10f, slopeLength);
            MansionDoorFactory.ApplyPersistentColor(rail, RailingColor);
        }

        private static void BuildInteriorLighting(Transform root)
        {
            GameObject lightsRoot = CreateGroup(root, "Lighting");
            CreateLightGroup(lightsRoot.transform, "Light_F1_", MansionLayoutData.Floor1LightPositions);
            CreateLightGroup(lightsRoot.transform, "Light_F2_", MansionLayoutData.Floor2LightPositions);
        }

        private static void CreateLightGroup(Transform parent, string prefix, Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject lightObj = new GameObject($"{prefix}{i + 1:00}");
                lightObj.transform.SetParent(parent, false);
                lightObj.transform.localPosition = positions[i];

                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.95f, 0.86f);
                light.intensity = 10f;
                light.range = 14f;
                light.shadows = LightShadows.None;
            }
        }

        private static void RelocateTestArena()
        {
            GameObject arena = GameObject.Find("TestArena");
            if (arena == null) return;

            Undo.RecordObject(arena.transform, "Move TestArena Aside");
            arena.transform.position = MansionLayoutData.TestArenaRelocation;
            Physics.SyncTransforms();
            BakeChildrenNavMesh(arena);
        }

        private static void BakeChildrenNavMesh(GameObject targetRoot)
        {
            var surface = targetRoot.GetComponent<NavMeshSurface>();
            if (surface == null) surface = targetRoot.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }

        private static void PositionPlayerInHall()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) return;

            Undo.RecordObject(player.transform, "Position Player in Mansion Hall");
            player.transform.position = MansionLayoutData.PlayerHallSpawn;
            player.transform.rotation = Quaternion.identity;

            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                Undo.RecordObject(health, "Update Player Respawn Position");
                health.SetRespawnPosition(MansionLayoutData.PlayerHallSpawn);
            }
        }

        private static GameObject CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }
    }
}
