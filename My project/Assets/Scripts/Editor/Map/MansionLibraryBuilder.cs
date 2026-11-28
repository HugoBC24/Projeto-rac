using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using ProjectRac.Interaction;
using ProjectRac.Inventory;
using ProjectRac.Puzzle;
using ProjectRac.UI;

namespace ProjectRac.Editor.Map
{
    public static class MansionLibraryBuilder
    {
        public const string LibraryRootName = "Mansion_Library";

        private static readonly Color WoodDark = new Color(0.22f, 0.13f, 0.08f, 1f);
        private static readonly Color StoneDark = new Color(0.18f, 0.18f, 0.21f, 1f);
        private static readonly Color StoneTrim = new Color(0.30f, 0.29f, 0.32f, 1f);
        private static readonly Color RugCrimson = new Color(0.36f, 0.10f, 0.12f, 1f);
        private static readonly Color BrassGold = new Color(0.86f, 0.70f, 0.24f, 1f);
        private static readonly Color SealColor = new Color(0.12f, 0.14f, 0.18f, 1f);

        [MenuItem("Tools/Build Mansion Library & Geometry Puzzle")]
        public static GameObject BuildLibraryAndPuzzle()
        {
            GameObject existing = GameObject.Find(LibraryRootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(LibraryRootName);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Build Mansion Library & Geometry Puzzle");

            Material defaultMat = GetOrCreateHoloMaterial("Mat_Puzzle_Holo_Default",
                new Color(0.22f, 0.78f, 0.98f, 1f), new Color(0.10f, 0.45f, 0.85f) * 1.4f);
            Material highlightMat = GetOrCreateHoloMaterial("Mat_Puzzle_Holo_Highlight",
                new Color(1.00f, 0.82f, 0.25f, 1f), new Color(0.95f, 0.65f, 0.10f) * 1.8f);
            Material solvedMat = GetOrCreateHoloMaterial("Mat_Puzzle_Holo_Solved",
                new Color(0.30f, 0.95f, 0.52f, 1f), new Color(0.15f, 0.80f, 0.35f) * 1.6f);

            // Real world coordinates for Biblioteca (1st floor, upper right) under MansionMap scale (1.5, 1.0, 1.5):
            // X in [10.86, 22.29], Z in [3.51, 15.69], Door_Biblioteca at (10.65, 0, 5.10).
            BuildLibraryFurnishings(root.transform);

            GeometricStatue[] statues = BuildThreeStatues(root.transform, defaultMat, highlightMat, solvedMat);
            BuildEnigmaLectern(root.transform, new Vector3(16.00f, 0f, 11.20f));
            var (sealCover, clockPiece) = BuildClockPieceReliquary(root.transform, new Vector3(21.20f, 0f, 11.20f));
            BuildPuzzleAccentLight(root.transform, new Vector3(16.00f, 3.10f, 13.00f));

            var manager = root.AddComponent<GeometricPuzzleManager>();
            manager.Configure(statues, sealCover, clockPiece);
            EditorUtility.SetDirty(manager);

            EnsurePlayerComponentsWithoutTouchingHUD();
            RebakeMansionNavMesh();

            var scene = EditorSceneManager.GetActiveScene();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MansionLibraryBuilder] Biblioteca e Puzzle Geometrico (Verity) + Peca de Relogio montados sem alterar HUD ou dialogos.");
            return root;
        }

        private static GeometricStatue[] BuildThreeStatues(
            Transform parent, Material defaultMat, Material highlightMat, Material solvedMat)
        {
            float[] xPositions = { 13.50f, 16.00f, 18.50f };
            string[] labels = { "Estatua I (Esquerda)", "Estatua II (Centro)", "Estatua III (Direita)" };
            string[] names = { "Statue_Left", "Statue_Center", "Statue_Right" };
            var results = new GeometricStatue[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject ped = new GameObject($"Pedestal_{names[i]}");
                ped.transform.SetParent(parent, false);
                ped.transform.position = new Vector3(xPositions[i], 0f, 14.10f);

                MansionDoorFactory.CreateLocalBox(ped.transform, "Base_Plinth",
                    new Vector3(0f, 0.10f, 0f), new Vector3(1.05f, 0.20f, 1.05f), StoneTrim);
                MansionDoorFactory.CreateLocalBox(ped.transform, "Pillar_Body",
                    new Vector3(0f, 0.65f, 0f), new Vector3(0.84f, 0.90f, 0.84f), StoneDark);
                MansionDoorFactory.CreateLocalBox(ped.transform, "Top_Cap",
                    new Vector3(0f, 1.15f, 0f), new Vector3(0.96f, 0.10f, 0.96f), StoneTrim);

                // Floating 3D Shape Anchor
                GameObject anchor = new GameObject("ShapeAnchor");
                anchor.transform.SetParent(ped.transform, false);
                anchor.transform.localPosition = new Vector3(0f, 1.85f, 0f);

                var shapeMf = anchor.AddComponent<MeshFilter>();
                var shapeMr = anchor.AddComponent<MeshRenderer>();
                shapeMr.sharedMaterial = defaultMat;
                shapeMf.sharedMesh = StatueShapeMeshBuilder.GetShape3DMesh(
                    i == 0 ? GeometricShape3D.Pyramid : (i == 1 ? GeometricShape3D.Cylinder : GeometricShape3D.Prism));

                var statue = ped.AddComponent<GeometricStatue>();

                // Two clickable 2D component slots on the South (-Z) face of the pedestal
                var slot0 = BuildComponentSlot(ped.transform, statue, 0, new Vector3(-0.23f, 0.82f, -0.45f), defaultMat);
                var slot1 = BuildComponentSlot(ped.transform, statue, 1, new Vector3(0.23f, 0.82f, -0.45f), defaultMat);

                statue.ConfigureEditorReferences(
                    labels[i], anchor.transform, shapeMf, shapeMr,
                    slot0, slot1, defaultMat, highlightMat, solvedMat);

                results[i] = statue;
            }

            return results;
        }

        private static StatueComponentSlot BuildComponentSlot(
            Transform parent, GeometricStatue owner, int slotIndex, Vector3 localPos, Material defaultMat)
        {
            GameObject slotObj = new GameObject($"ComponentSlot_{slotIndex}");
            slotObj.transform.SetParent(parent, false);
            slotObj.transform.localPosition = localPos;

            var col = slotObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.34f, 0.34f, 0.16f);

            // Recessed frame backing
            MansionDoorFactory.CreateLocalBox(slotObj.transform, "Slot_Frame",
                new Vector3(0f, 0f, 0.02f), new Vector3(0.28f, 0.28f, 0.03f), StoneTrim, removeCollider: true);

            GameObject symObj = new GameObject("Symbol_Mesh");
            symObj.transform.SetParent(slotObj.transform, false);
            symObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);

            var mf = symObj.AddComponent<MeshFilter>();
            var mr = symObj.AddComponent<MeshRenderer>();
            mf.sharedMesh = StatueShapeMeshBuilder.GetSymbol2DMesh(
                slotIndex == 0 ? ShapeComponent2D.Triangle : ShapeComponent2D.Square);
            mr.sharedMaterial = defaultMat;

            var slot = slotObj.AddComponent<StatueComponentSlot>();
            slot.Configure(owner, slotIndex, mf, mr);
            return slot;
        }

        private static void BuildEnigmaLectern(Transform parent, Vector3 pos)
        {
            GameObject lectern = new GameObject("Biblioteca_Enigma_Lectern");
            lectern.transform.SetParent(parent, false);
            lectern.transform.position = pos;

            MansionDoorFactory.CreateLocalBox(lectern.transform, "Pedestal",
                new Vector3(0f, 0.50f, 0f), new Vector3(0.35f, 1.00f, 0.35f), WoodDark);

            GameObject plaque = MansionDoorFactory.CreateLocalBox(lectern.transform, "Enigma_Plaque",
                new Vector3(0f, 1.04f, 0f), new Vector3(0.70f, 0.08f, 0.48f), StoneTrim);
            plaque.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

            NoteInspectionUI noteUI = Object.FindAnyObjectByType<NoteInspectionUI>(FindObjectsInactive.Include);
            var note = plaque.AddComponent<InspectableNote>();
            note.Configure(
                "Inscricao das Tres Estatuas",
                "Alinhe as tres formas sagradas da esquerda para a direita:\n\n" +
                "  I.   Piramide  (Triangulo + Triangulo)\n" +
                "  II.  Cilindro  (Quadrado + Circulo)\n" +
                "  III. Prisma    (Quadrado + Triangulo)\n\n" +
                "Leis da Forma:\n" +
                "  Esfera   = Circulo + Circulo\n" +
                "  Cubo     = Quadrado + Quadrado\n" +
                "  Piramide = Triangulo + Triangulo\n" +
                "  Cilindro = Quadrado + Circulo\n" +
                "  Cone     = Circulo + Triangulo\n" +
                "  Prisma   = Quadrado + Triangulo\n\n" +
                "Retire um simbolo de uma estatua e troque-o pelo simbolo de outra.",
                noteUI);
        }

        private static (GameObject sealCover, ClockPiecePickup pickup) BuildClockPieceReliquary(Transform parent, Vector3 pos)
        {
            GameObject reliquary = new GameObject("Reliquary_ClockPiece");
            reliquary.transform.SetParent(parent, false);
            reliquary.transform.position = pos;

            MansionDoorFactory.CreateLocalBox(reliquary.transform, "Pedestal_Base",
                new Vector3(0f, 0.52f, 0f), new Vector3(0.75f, 1.04f, 0.75f), StoneDark);

            // Seal cover hides the clock piece until the puzzle is solved
            GameObject sealCover = MansionDoorFactory.CreateLocalBox(reliquary.transform, "Reward_Seal_Cover",
                new Vector3(0f, 1.32f, 0f), new Vector3(0.66f, 0.56f, 0.66f), SealColor);

            // Golden Clock Piece Pickup inside
            GameObject pickupObj = new GameObject("ClockPiece_Library");
            pickupObj.transform.SetParent(reliquary.transform, false);
            pickupObj.transform.localPosition = new Vector3(0f, 1.28f, 0f);

            var col = pickupObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.45f, 0.45f, 0.45f);

            GameObject visual = new GameObject("GearVisual");
            visual.transform.SetParent(pickupObj.transform, false);
            BuildClockGearVisual(visual.transform);

            var pickup = pickupObj.AddComponent<ClockPiecePickup>();
            pickup.Configure("clock_piece_library", visual.transform);
            pickupObj.SetActive(false);

            return (sealCover, pickup);
        }

        private static void BuildClockGearVisual(Transform parent)
        {
            // Golden gear hub + 6 gear teeth + central axle
            MansionDoorFactory.CreateLocalBox(parent, "Gear_Core",
                Vector3.zero, new Vector3(0.24f, 0.24f, 0.05f), BrassGold, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(parent, "Gear_Core_Diag",
                Vector3.zero, new Vector3(0.24f, 0.24f, 0.05f), BrassGold, removeCollider: true)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                GameObject tooth = MansionDoorFactory.CreateLocalBox(parent, $"Tooth_{i}",
                    Vector3.zero, new Vector3(0.05f, 0.34f, 0.045f), BrassGold, removeCollider: true);
                tooth.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private static void BuildLibraryFurnishings(Transform parent)
        {
            // Crimson rug under the puzzle area
            MansionDoorFactory.CreateLocalBox(parent, "Rug_Library",
                new Vector3(16.00f, 0.01f, 12.20f), new Vector3(7.60f, 0.02f, 4.60f), RugCrimson, removeCollider: true);

            // Tall wooden bookcases along walls (leaving Door_Biblioteca at X=10.65, Z=5.10 clear)
            MansionDoorFactory.CreateLocalBox(parent, "Bookcase_North_W",
                new Vector3(11.35f, 1.50f, 15.40f), new Vector3(1.80f, 3.00f, 0.48f), WoodDark);
            MansionDoorFactory.CreateLocalBox(parent, "Bookcase_North_E",
                new Vector3(20.85f, 1.50f, 15.40f), new Vector3(2.20f, 3.00f, 0.48f), WoodDark);
            MansionDoorFactory.CreateLocalBox(parent, "Bookcase_East_S",
                new Vector3(21.95f, 1.50f, 6.20f), new Vector3(0.48f, 3.00f, 3.40f), WoodDark);
            MansionDoorFactory.CreateLocalBox(parent, "Bookcase_South_E",
                new Vector3(17.50f, 1.50f, 3.80f), new Vector3(3.60f, 3.00f, 0.48f), WoodDark);
        }

        private static void BuildPuzzleAccentLight(Transform parent, Vector3 pos)
        {
            GameObject lightObj = new GameObject("Library_Puzzle_Light");
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.position = pos;

            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 10f;
            l.intensity = 2.6f;
            l.color = new Color(1.0f, 0.90f, 0.74f);
            l.shadows = LightShadows.None;
        }

        private static void EnsurePlayerComponentsWithoutTouchingHUD()
        {
            GameObject player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player == null) return;

            bool dirty = false;
            if (player.GetComponent<PlayerPuzzleComponentHolder>() == null)
            {
                Undo.AddComponent<PlayerPuzzleComponentHolder>(player);
                dirty = true;
            }
            if (player.GetComponent<PlayerKeyItemInventory>() == null)
            {
                Undo.AddComponent<PlayerKeyItemInventory>(player);
                dirty = true;
            }
            if (dirty) EditorUtility.SetDirty(player);
        }

        private static void RebakeMansionNavMesh()
        {
            GameObject map = GameObject.Find(MansionMapBuilder.MansionRootName);
            if (map == null) return;
            var surface = map.GetComponent<NavMeshSurface>();
            if (surface != null) surface.BuildNavMesh();
        }

        private static Material GetOrCreateHoloMaterial(string assetName, Color baseColor, Color emissionColor)
        {
            string path = $"Assets/Materials/Arena/{assetName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { name = assetName };
            mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
