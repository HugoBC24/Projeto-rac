using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Editor.Enemy;
using ProjectRac.Editor.UI;
using ProjectRac.Interaction;
using ProjectRac.Map;
using ProjectRac.UI;

namespace ProjectRac.Editor.Map
{
    public static class MansionLivingRoomBuilder
    {
        public const string FireplaceGroupName = "SalaDeEstar_LareiraEvent";

        // East wall of Sala de Estar (1st floor, right side) under MansionMap scale (1.5, 1.0, 1.5).
        // World East wall interior face is around X = 22.29; center Z of Sala de Estar is -6.30.
        private static readonly Vector3 FireplaceWorldPos = new Vector3(21.45f, 0f, -6.30f);
        private static readonly Vector3 AmbushFallbackWorldPos = new Vector3(14.5f, 0f, -6.30f);

        private static readonly Color StoneDark = new Color(0.20f, 0.19f, 0.19f, 1f);
        private static readonly Color StoneLight = new Color(0.32f, 0.30f, 0.29f, 1f);
        private static readonly Color FireboxBack = new Color(0.08f, 0.06f, 0.06f, 1f);
        private static readonly Color WoodMantel = new Color(0.28f, 0.17f, 0.10f, 1f);
        private static readonly Color LogDark = new Color(0.16f, 0.09f, 0.05f, 1f);
        private static readonly Color EmberGlow = new Color(0.95f, 0.36f, 0.08f, 1f);
        private static readonly Color PaperColor = new Color(0.93f, 0.89f, 0.78f, 1f);

        [MenuItem("Tools/Build Living Room Fireplace & Doll Ambush")]
        public static GameObject BuildLivingRoomFireplaceAndAmbush()
        {
            GameObject bonecaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonecaEnemyAssetSetup.PrefabPath)
                                   ?? BonecaEnemyAssetSetup.BuildAll();

            NoteInspectionUI noteUI = Object.FindAnyObjectByType<NoteInspectionUI>(FindObjectsInactive.Include);
            VisualNovelDialogueUI vnUI = Object.FindAnyObjectByType<VisualNovelDialogueUI>(FindObjectsInactive.Include);

            GameObject existing = GameObject.Find(FireplaceGroupName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(FireplaceGroupName);
            root.transform.position = FireplaceWorldPos;
            root.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Local +Z faces West into Sala de Estar
            Undo.RegisterCreatedObjectUndo(root, "Create Sala de Estar Fireplace & Ambush Note");

            BuildFireplaceGeometry(root.transform);
            BuildFireplaceNoteAndAmbush(root.transform, bonecaPrefab, noteUI, vnUI);

            UpgradeTestArenaMeleeEnemy(bonecaPrefab);
            RebakeMansionNavMesh();

            var scene = EditorSceneManager.GetActiveScene();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MansionLivingRoomBuilder] Lareira, Nota_Lareira e emboscada da Boneca montados na Sala de Estar.");
            return root;
        }

        private static void BuildFireplaceGeometry(Transform parent)
        {
            GameObject fp = new GameObject("Lareira_SalaDeEstar");
            fp.transform.SetParent(parent, false);

            // Raised hearth base
            MansionDoorFactory.CreateLocalBox(fp.transform, "Hearth_Base", new Vector3(0f, 0.09f, 0.12f), new Vector3(2.90f, 0.18f, 1.25f), StoneLight);

            // Left & right stone pillars framing the firebox
            MansionDoorFactory.CreateLocalBox(fp.transform, "Pillar_Left", new Vector3(-0.98f, 0.78f, -0.08f), new Vector3(0.68f, 1.38f, 0.88f), StoneDark);
            MansionDoorFactory.CreateLocalBox(fp.transform, "Pillar_Right", new Vector3(0.98f, 0.78f, -0.08f), new Vector3(0.68f, 1.38f, 0.88f), StoneDark);

            // Dark firebox back & header lintel
            MansionDoorFactory.CreateLocalBox(fp.transform, "Firebox_Back", new Vector3(0f, 0.78f, -0.40f), new Vector3(1.36f, 1.38f, 0.24f), FireboxBack);
            MansionDoorFactory.CreateLocalBox(fp.transform, "Header_Stone", new Vector3(0f, 1.30f, -0.08f), new Vector3(2.64f, 0.34f, 0.88f), StoneDark);

            // Wooden mantel shelf (note rests on top at Y = 1.54)
            MansionDoorFactory.CreateLocalBox(fp.transform, "Mantel_Wood", new Vector3(0f, 1.50f, -0.02f), new Vector3(2.96f, 0.10f, 1.04f), WoodMantel);

            // Chimney breast rising up to ceiling (Y = 3.60)
            MansionDoorFactory.CreateLocalBox(fp.transform, "Chimney_Breast", new Vector3(0f, 2.58f, -0.18f), new Vector3(1.96f, 2.08f, 0.68f), StoneLight);

            // Firewood logs & glowing embers
            GameObject log1 = MansionDoorFactory.CreateLocalBox(fp.transform, "Log_01", new Vector3(-0.18f, 0.26f, -0.06f), new Vector3(0.75f, 0.12f, 0.14f), LogDark, removeCollider: true);
            log1.transform.localRotation = Quaternion.Euler(0f, 18f, 8f);
            GameObject log2 = MansionDoorFactory.CreateLocalBox(fp.transform, "Log_02", new Vector3(0.18f, 0.28f, -0.08f), new Vector3(0.72f, 0.12f, 0.14f), LogDark, removeCollider: true);
            log2.transform.localRotation = Quaternion.Euler(0f, -24f, -6f);

            GameObject embers = MansionDoorFactory.CreateLocalBox(fp.transform, "Embers_Glow", new Vector3(0f, 0.22f, -0.05f), new Vector3(0.62f, 0.08f, 0.34f), EmberGlow, removeCollider: true);
            ApplyEmissiveEmberMaterial(embers);

            // Warm flickering fire light
            GameObject fireLightObj = new GameObject("Fireplace_Light");
            fireLightObj.transform.SetParent(fp.transform, false);
            fireLightObj.transform.localPosition = new Vector3(0f, 0.58f, 0.28f);
            var light = fireLightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.56f, 0.22f);
            light.intensity = 9.5f;
            light.range = 10.5f;
            light.shadows = LightShadows.None;
            fireLightObj.AddComponent<FireplaceFlicker>();
        }

        private static void BuildFireplaceNoteAndAmbush(
            Transform parent,
            GameObject bonecaPrefab,
            NoteInspectionUI noteUI,
            VisualNovelDialogueUI vnUI)
        {
            GameObject noteObj = MansionDoorFactory.CreateLocalBox(
                parent,
                "Nota_Lareira",
                new Vector3(0f, 1.57f, 0.24f),
                new Vector3(0.32f, 0.015f, 0.40f),
                PaperColor);
            noteObj.transform.localRotation = Quaternion.Euler(12f, 8f, 0f);

            var col = noteObj.GetComponent<BoxCollider>();
            if (col != null) col.size = new Vector3(1.5f, 8.0f, 1.5f);

            var note = noteObj.AddComponent<InspectableNote>();
            note.Configure(
                "Carta Queimada na Lareira",
                "19 de Outubro\n\n" +
                "Tentei queimar a boneca nesta lareira antes de fugir da sala de estar.\n" +
                "O fogo apagou sozinho... e quando virei de costas, ela ja nao estava mais la.\n\n" +
                "Se voce esta lendo isto agora, NAO olhe para tras.",
                noteUI);

            Sprite maidPlaceholder = VisualNovelPlaceholderArt.EnsureMaidPlaceholder();
            var ambushLines = new List<VisualNovelDialogueLine>
            {
                new VisualNovelDialogueLine
                {
                    speakerName = "Boneca",
                    speakerNameColor = new Color(0.95f, 0.52f, 0.58f, 1f),
                    text = "Voce encontrou a carta... Ninguem nunca termina de brincar comigo.",
                    characterSprite = maidPlaceholder,
                    slot = CharacterSlot.Right,
                    keepOtherCharacters = false
                },
                new VisualNovelDialogueLine
                {
                    speakerName = "",
                    speakerNameColor = Color.white,
                    text = "Ela comeca a correr na sua direcao!",
                    characterSprite = maidPlaceholder,
                    slot = CharacterSlot.Right,
                    keepOtherCharacters = false
                }
            };

            var ambush = noteObj.AddComponent<FireplaceNoteAmbushTrigger>();
            ambush.Configure(note, bonecaPrefab, vnUI, ambushLines, AmbushFallbackWorldPos);
        }

        private static void UpgradeTestArenaMeleeEnemy(GameObject bonecaPrefab)
        {
            if (bonecaPrefab == null) return;
            GameObject existing = GameObject.Find("MeleeEnemy");
            if (existing == null) return;

            Transform parent = existing.transform.parent;
            Vector3 pos = existing.transform.position;
            Quaternion rot = existing.transform.rotation;
            Undo.DestroyObjectImmediate(existing);

            GameObject upgraded = (GameObject)PrefabUtility.InstantiatePrefab(bonecaPrefab, parent);
            upgraded.name = "MeleeEnemy";
            upgraded.transform.SetPositionAndRotation(pos, rot);
            Undo.RegisterCreatedObjectUndo(upgraded, "Upgrade MeleeEnemy to Boneca");
        }

        private static void RebakeMansionNavMesh()
        {
            GameObject mansion = GameObject.Find(MansionMapBuilder.MansionRootName);
            if (mansion == null) return;
            Physics.SyncTransforms();

            var surface = mansion.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                // Include Living Room fireplace collider (separate scene object) when baking Mansion NavMesh.
                surface.collectObjects = CollectObjects.Volume;
                surface.center = new Vector3(0f, 3.8f, 0f);
                surface.size = new Vector3(48f, 10f, 36f);
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
            }
        }

        private static void ApplyEmissiveEmberMaterial(GameObject target)
        {
            var r = target.GetComponent<Renderer>();
            if (r == null) return;

            const string matPath = "Assets/Materials/Arena/Mat_Fireplace_Embers.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", EmberGlow);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", EmberGlow * 2.2f);
            }
            EditorUtility.SetDirty(mat);
            r.sharedMaterial = mat;
        }
    }
}
