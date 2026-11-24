using UnityEngine;

namespace ProjectRac.Enemy
{
    public static class EnemyRangedVisuals
    {
        public struct VisualHandles
        {
            public Transform FirePoint;
            public Renderer MuzzleRenderer;
        }

        public static readonly Color IdleMuzzleColor = new Color(0.95f, 0.55f, 0.12f, 1f);
        public static readonly Color ChargingMuzzleColor = new Color(1f, 0.95f, 0.35f, 1f);

        private static readonly Color BodyColor = new Color(0.14f, 0.28f, 0.36f, 1f);
        private static readonly Color TrimColor = new Color(0.22f, 0.24f, 0.27f, 1f);
        private static readonly Color VisorColor = new Color(1f, 0.65f, 0.1f, 1f);

        public static VisualHandles EnsureVisuals(Transform enemyRoot, GameObject customPrefab = null)
        {
            Transform existingMesh = enemyRoot.Find("RangedEnemyMesh");
            if (existingMesh != null)
            {
                return ResolveHandles(existingMesh);
            }

            if (customPrefab != null)
            {
                GameObject custom = Object.Instantiate(customPrefab, enemyRoot);
                custom.name = "RangedEnemyMesh";
                return EnsureFirePoint(custom.transform);
            }

            GameObject meshRoot = new GameObject("RangedEnemyMesh");
            meshRoot.transform.SetParent(enemyRoot, false);

            // Distinct silhouette from MeleeEnemy: slimmer slate-teal torso, shoulder rig, and right-arm blaster.
            CreatePart(
                meshRoot.transform,
                "Body",
                PrimitiveType.Capsule,
                new Vector3(0f, 1.0f, 0f),
                Vector3.zero,
                new Vector3(0.68f, 1.0f, 0.68f),
                BodyColor);

            CreatePart(
                meshRoot.transform,
                "Visor",
                PrimitiveType.Cube,
                new Vector3(0f, 1.52f, 0.28f),
                Vector3.zero,
                new Vector3(0.46f, 0.12f, 0.2f),
                VisorColor);

            CreatePart(
                meshRoot.transform,
                "ShoulderPack",
                PrimitiveType.Cube,
                new Vector3(0f, 1.32f, -0.26f),
                Vector3.zero,
                new Vector3(0.54f, 0.52f, 0.24f),
                TrimColor);

            CreatePart(
                meshRoot.transform,
                "BlasterBody",
                PrimitiveType.Cube,
                new Vector3(0.46f, 1.24f, 0.34f),
                Vector3.zero,
                new Vector3(0.18f, 0.18f, 0.72f),
                TrimColor);

            GameObject muzzle = CreatePart(
                meshRoot.transform,
                "MuzzleGlow",
                PrimitiveType.Cylinder,
                new Vector3(0.46f, 1.24f, 0.72f),
                new Vector3(90f, 0f, 0f),
                new Vector3(0.13f, 0.08f, 0.13f),
                IdleMuzzleColor);

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(meshRoot.transform, false);
            firePoint.transform.localPosition = new Vector3(0.46f, 1.24f, 0.88f);

            return new VisualHandles
            {
                FirePoint = firePoint.transform,
                MuzzleRenderer = muzzle.GetComponent<Renderer>()
            };
        }

        private static VisualHandles ResolveHandles(Transform meshRoot)
        {
            Transform firePoint = meshRoot.Find("FirePoint");
            Transform muzzle = meshRoot.Find("MuzzleGlow");

            if (firePoint == null)
            {
                return EnsureFirePoint(meshRoot);
            }

            return new VisualHandles
            {
                FirePoint = firePoint,
                MuzzleRenderer = muzzle != null ? muzzle.GetComponent<Renderer>() : null
            };
        }

        private static VisualHandles EnsureFirePoint(Transform meshRoot)
        {
            Transform fp = meshRoot.Find("FirePoint");
            if (fp == null)
            {
                GameObject fpObj = new GameObject("FirePoint");
                fpObj.transform.SetParent(meshRoot, false);
                fpObj.transform.localPosition = new Vector3(0.46f, 1.24f, 0.88f);
                fp = fpObj.transform;
            }

            Transform muzzle = meshRoot.Find("MuzzleGlow");
            return new VisualHandles
            {
                FirePoint = fp,
                MuzzleRenderer = muzzle != null ? muzzle.GetComponent<Renderer>() : null
            };
        }

        public static void SetMuzzleColor(Renderer muzzleRenderer, MaterialPropertyBlock block, Color color)
        {
            if (muzzleRenderer == null || block == null) return;
            muzzleRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            muzzleRenderer.SetPropertyBlock(block);
        }

        private static GameObject CreatePart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPos,
            Vector3 localEuler,
            Vector3 localScale,
            Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;

            var col = part.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
            }

            return part;
        }
    }
}
