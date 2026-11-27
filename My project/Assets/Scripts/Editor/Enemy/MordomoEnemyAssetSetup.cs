using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Enemy;

namespace ProjectRac.Editor.Enemy
{
    public static class MordomoEnemyAssetSetup
    {
        public const string FbxPath = "Assets/Models/Enemies/MordomoEnemy.fbx";
        public const string TexturesFolder = "Assets/Models/Enemies/Textures";
        public const string MaterialsFolder = "Assets/Materials/Enemies/Mordomo";
        public const string ControllerPath = "Assets/Animations/Enemies/MordomoEnemy.controller";
        public const string PrefabPath = "Assets/Prefabs/Enemies/Mordomo_RangedEnemy.prefab";

        [MenuItem("Tools/Setup Mordomo Enemy Model & Animations")]
        public static GameObject BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureFbxImporterAndClips();
            CreateAndRemapMordomoMaterials();
            AnimatorController controller = BuildAnimatorController();
            GameObject prefab = BuildRangedEnemyPrefab(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MordomoEnemyAssetSetup] Modelo, clips (Idle 0-54, Attack 54-84, Walk 102-143), controller e prefab prontos em {PrefabPath}.");
            return prefab;
        }

        private static void ConfigureFbxImporterAndClips()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[MordomoEnemyAssetSetup] FBX nao encontrado em {FbxPath}.");
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
                : "RIGMORDOMO|RIGMORDOMOAction";

            importer.clipAnimations = new[]
            {
                MakeClip("Idle", takeName, 0f, 54f, loop: true),
                MakeClip("Attack", takeName, 54f, 84f, loop: true),
                MakeClip("Walk", takeName, 102f, 143f, loop: true)
            };

            importer.SaveAndReimport();
        }

        private static ModelImporterClipAnimation MakeClip(string name, string takeName, float firstFrame, float lastFrame, bool loop)
        {
            return new ModelImporterClipAnimation
            {
                name = name,
                takeName = takeName,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = loop,
                loopPose = loop,
                lockRootRotation = true,
                lockRootHeightY = true,
                lockRootPositionXZ = true
            };
        }

        private static void CreateAndRemapMordomoMaterials()
        {
            EnsureFolder(MaterialsFolder);
            Texture2D skinTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/peleMordomo.png");

            Material matTerno = CreateOrUpdateMat("Mat_Mordomo_Terno", new Color(0.11f, 0.12f, 0.12f, 1f), 0.14f);
            Material matBranco = CreateOrUpdateMat("Mat_Mordomo_Branco", new Color(0.84f, 0.84f, 0.86f, 1f), 0.12f);
            Material matSapato = CreateOrUpdateMat("Mat_Mordomo_Sapato", new Color(0.04f, 0.04f, 0.04f, 1f), 0.22f);
            Material matPele2 = CreateOrUpdateMat("Mat_Mordomo_Pele2", new Color(0.80f, 0.67f, 0.64f, 1f), 0.14f);
            Material matPele = CreateOrUpdateMat("Mat_Mordomo_Pele", Color.white, 0.14f, skinTex);
            Material matCabelo = CreateOrUpdateMat("Mat_Mordomo_Cabelo", new Color(0.15f, 0.11f, 0.09f, 1f), 0.14f);

            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer != null)
            {
                RemapMat(importer, "ternoMordomo", matTerno);
                RemapMat(importer, "branco", matBranco);
                RemapMat(importer, "sapatp1", matSapato);
                RemapMat(importer, "peleMordomo2", matPele2);
                RemapMat(importer, "peleMordomo", matPele);
                RemapMat(importer, "cabeloMordomo", matCabelo);
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController BuildAnimatorController()
        {
            EnsureFolder(Path.GetDirectoryName(ControllerPath)?.Replace("\\", "/"));
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimationClip idleClip = LoadClipByName("Idle");
            AnimationClip attackClip = LoadClipByName("Attack");
            AnimationClip walkClip = LoadClipByName("Walk");

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("AnimState", AnimatorControllerParameterType.Int);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;
            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walkClip;
            AnimatorState attackState = sm.AddState("Attack");
            attackState.motion = attackClip;
            sm.defaultState = idleState;

            AddAnyStateTransition(sm, idleState, EnemyRangedAnimator.StateIdle);
            AddAnyStateTransition(sm, walkState, EnemyRangedAnimator.StateWalk);
            AddAnyStateTransition(sm, attackState, EnemyRangedAnimator.StateAttack);

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

        private static GameObject BuildRangedEnemyPrefab(AnimatorController controller)
        {
            EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace("\\", "/"));
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxAsset == null) return null;

            GameObject root = new GameObject("Mordomo_RangedEnemy");

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.radius = 0.44f;
            col.height = 1.90f;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.44f;
            agent.height = 1.90f;
            agent.baseOffset = 0f;
            agent.speed = 2.6f;
            agent.acceleration = 12f;
            agent.angularSpeed = 300f;
            agent.stoppingDistance = 1.0f;

            var health = root.AddComponent<EnemyHealth>();
            health.SetMaxHealth(60f);

            root.AddComponent<EnemyRangedAI>();
            var animDriver = root.AddComponent<EnemyRangedAnimator>();

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(root.transform, false);
            firePoint.transform.localPosition = new Vector3(0.28f, 1.35f, 0.65f);

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            model.name = "MordomoModel";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

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
