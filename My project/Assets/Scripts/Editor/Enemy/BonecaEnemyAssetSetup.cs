using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Enemy;

namespace ProjectRac.Editor.Enemy
{
    public static class BonecaEnemyAssetSetup
    {
        public const string FbxPath = "Assets/Models/Enemies/BonecaEnemy.fbx";
        public const string TexturesFolder = "Assets/Models/Enemies/Textures";
        public const string MaterialsFolder = "Assets/Materials/Enemies/Boneca";
        public const string ControllerPath = "Assets/Animations/Enemies/BonecaEnemy.controller";
        public const string PrefabPath = "Assets/Prefabs/Enemies/Boneca_MeleeEnemy.prefab";

        [MenuItem("Tools/Setup Boneca Enemy Model & Animations")]
        public static GameObject BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureFbxImporterAndClips();
            Material[] bonecaMats = CreateAndRemapBonecaMaterials();
            AnimatorController controller = BuildAnimatorController();
            GameObject prefab = BuildMeleeEnemyPrefab(controller, bonecaMats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BonecaEnemyAssetSetup] Modelo, clips (Walk 1-55, Run 55-120, Idle 120-165), controller e prefab prontos em {PrefabPath}.");
            return prefab;
        }

        private static void ConfigureFbxImporterAndClips()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[BonecaEnemyAssetSetup] FBX nao encontrado em {FbxPath}.");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.addCollider = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

            string takeName = importer.defaultClipAnimations != null && importer.defaultClipAnimations.Length > 0
                ? importer.defaultClipAnimations[0].takeName
                : "RIGBONECA|RIGBONECAAction";

            importer.clipAnimations = new[]
            {
                MakeLoopClip("Walk", takeName, 1f, 55f),
                MakeLoopClip("Run", takeName, 55f, 120f),
                MakeLoopClip("Idle", takeName, 120f, 165f)
            };

            importer.SaveAndReimport();
        }

        private static ModelImporterClipAnimation MakeLoopClip(string name, string takeName, float firstFrame, float lastFrame)
        {
            return new ModelImporterClipAnimation
            {
                name = name,
                takeName = takeName,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = true,
                loopPose = true,
                lockRootRotation = true,
                lockRootHeightY = true,
                lockRootPositionXZ = true
            };
        }

        private static Material[] CreateAndRemapBonecaMaterials()
        {
            EnsureFolder(MaterialsFolder);

            Texture2D skinTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/skinBoneca.png");
            Texture2D eyesTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Eyes.png");
            Texture2D faceTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/faceProta.png");

            // Order matches BONECA material slots: ['pele boneca', 'vestido boneca', 'branco', 'faceProta', 'cabelo', 'olhos']
            Material matPele = CreateOrUpdateMat("Mat_Boneca_Pele", Color.white, 0.14f, skinTex);
            Material matVestido = CreateOrUpdateMat("Mat_Boneca_Vestido", new Color(0.38f, 0.21f, 0.24f, 1f), 0.12f);
            Material matBranco = CreateOrUpdateMat("Mat_Boneca_Branco", new Color(0.82f, 0.82f, 0.84f, 1f), 0.12f);
            Material matFace = CreateOrUpdateMat("Mat_Boneca_Face", Color.white, 0.14f, faceTex);
            Material matCabelo = CreateOrUpdateMat("Mat_Boneca_Cabelo", new Color(0.22f, 0.10f, 0.08f, 1f), 0.16f);
            Material matOlhos = CreateOrUpdateMat("Mat_Boneca_Olhos", Color.white, 0.25f, eyesTex);

            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer != null)
            {
                RemapMat(importer, "pele boneca", matPele);
                RemapMat(importer, "vestido boneca", matVestido);
                RemapMat(importer, "branco", matBranco);
                RemapMat(importer, "faceProta", matFace);
                RemapMat(importer, "cabelo", matCabelo);
                RemapMat(importer, "olhos", matOlhos);
                importer.SaveAndReimport();
            }

            return new[] { matPele, matVestido, matBranco, matFace, matCabelo, matOlhos };
        }

        private static AnimatorController BuildAnimatorController()
        {
            EnsureFolder(Path.GetDirectoryName(ControllerPath)?.Replace("\\", "/"));

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimationClip walkClip = LoadClipByName("Walk");
            AnimationClip runClip = LoadClipByName("Run");
            AnimationClip idleClip = LoadClipByName("Idle");

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("AnimState", AnimatorControllerParameterType.Int);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;
            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walkClip;
            AnimatorState runState = sm.AddState("Run");
            runState.motion = runClip;
            sm.defaultState = idleState;

            AddAnyStateTransition(sm, idleState, EnemyMeleeAnimator.StateIdle);
            AddAnyStateTransition(sm, walkState, EnemyMeleeAnimator.StateWalk);
            AddAnyStateTransition(sm, runState, EnemyMeleeAnimator.StateRun);

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState dest, int stateValue)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(dest);
            t.hasExitTime = false;
            t.duration = 0.12f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.Equals, stateValue, "AnimState");
        }

        private static GameObject BuildMeleeEnemyPrefab(AnimatorController controller, Material[] mats)
        {
            EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace("\\", "/"));
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxAsset == null) return null;

            GameObject root = new GameObject("Boneca_MeleeEnemy");

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.90f, 0f);
            col.radius = 0.42f;
            col.height = 1.80f;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.42f;
            agent.height = 1.80f;
            agent.baseOffset = 0f;
            agent.speed = 2.6f;
            agent.acceleration = 14f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = 1.5f;

            var health = root.AddComponent<EnemyHealth>();
            health.SetMaxHealth(80f);

            root.AddComponent<EnemyMeleeAI>();
            var animDriver = root.AddComponent<EnemyMeleeAnimator>();

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            model.name = "BonecaModel";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null && mats != null && smr.sharedMaterials.Length == mats.Length)
            {
                smr.sharedMaterials = mats;
            }

            animDriver.BindAnimator(animator);

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return savedPrefab;
        }

        private static AnimationClip LoadClipByName(string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && clip.name == clipName && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void RemapMat(ModelImporter importer, string sourceName, Material targetMat)
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName), targetMat);
        }

        private static Material CreateOrUpdateMat(string matName, Color color, float smoothness, Texture2D tex = null)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath)) return;
            string[] parts = folderPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
