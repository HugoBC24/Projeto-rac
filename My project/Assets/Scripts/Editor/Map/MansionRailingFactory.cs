using UnityEngine;

namespace ProjectRac.Editor.Map
{
    public static class MansionRailingFactory
    {
        private const float TopRailCenterY = 0.94f;
        private const float TopRailHeight = 0.08f;
        private const float TopRailThickness = 0.10f;
        private const float CurbHeight = 0.08f;
        private const float CurbThickness = 0.12f;
        private const float BalusterSpacing = 0.65f;
        private const float GuardColliderHeight = 1.15f;

        private static readonly Color RailingWoodColor = new Color(0.24f, 0.17f, 0.12f, 1f);
        private static readonly Color ColumnWoodColor = new Color(0.28f, 0.20f, 0.14f, 1f);

        public static void BuildRailing(Transform parent, RailingDef def, float floorSurfaceY)
        {
            Vector2 delta = def.End - def.Start;
            float totalLength = delta.magnitude;
            if (totalLength < 0.15f) return;

            float yaw = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, yaw - 90f, 0f);
            Vector3 startPos = new Vector3(def.Start.x, floorSurfaceY, def.Start.y);

            GameObject group = new GameObject(def.Name);
            group.transform.SetParent(parent, false);
            group.transform.SetLocalPositionAndRotation(startPos, rot);

            if (def.GapWidth <= 0.05f)
            {
                BuildSpan(group.transform, def.Name + "_Span", 0f, totalLength);
                return;
            }

            float halfGap = def.GapWidth * 0.5f;
            float gapStart = Mathf.Clamp(def.GapOffsetFromStart - halfGap, 0f, totalLength);
            float gapEnd = Mathf.Clamp(def.GapOffsetFromStart + halfGap, 0f, totalLength);

            // Skip tiny slivers next to stair exits so the landing stays completely unobstructed.
            if (gapStart >= 0.25f)
            {
                BuildSpan(group.transform, def.Name + "_Left", 0f, gapStart);
            }

            if (totalLength - gapEnd >= 0.25f)
            {
                BuildSpan(group.transform, def.Name + "_Right", gapEnd, totalLength);
            }
        }

        public static void BuildBalconyColumn(Transform parent, string name, Vector2 xz, float floorSurfaceY)
        {
            float height = MansionLayoutData.FloorHeight;
            MansionDoorFactory.CreateLocalBox(
                parent,
                name,
                new Vector3(xz.x, floorSurfaceY + height * 0.5f, xz.y),
                new Vector3(0.22f, height, 0.22f),
                ColumnWoodColor);
        }

        private static void BuildSpan(Transform parent, string spanName, float startX, float endX)
        {
            float length = endX - startX;
            if (length < 0.2f) return;

            float centerX = (startX + endX) * 0.5f;
            GameObject spanObj = new GameObject(spanName);
            spanObj.transform.SetParent(parent, false);

            // Top handrail bar matching Stair_Handrail dark wood style
            MansionDoorFactory.CreateLocalBox(
                spanObj.transform,
                "TopRail",
                new Vector3(centerX, TopRailCenterY, 0f),
                new Vector3(length + TopRailThickness, TopRailHeight, TopRailThickness),
                RailingWoodColor,
                removeCollider: true);

            // Bottom curb bar
            MansionDoorFactory.CreateLocalBox(
                spanObj.transform,
                "BottomCurb",
                new Vector3(centerX, CurbHeight * 0.5f, 0f),
                new Vector3(length + CurbThickness, CurbHeight, CurbThickness),
                RailingWoodColor,
                removeCollider: true);

            // End newel posts
            float postHeight = TopRailCenterY + TopRailHeight * 0.5f;
            MansionDoorFactory.CreateLocalBox(
                spanObj.transform,
                "Post_Start",
                new Vector3(startX, postHeight * 0.5f, 0f),
                new Vector3(0.10f, postHeight, 0.10f),
                RailingWoodColor,
                removeCollider: true);

            MansionDoorFactory.CreateLocalBox(
                spanObj.transform,
                "Post_End",
                new Vector3(endX, postHeight * 0.5f, 0f),
                new Vector3(0.10f, postHeight, 0.10f),
                RailingWoodColor,
                removeCollider: true);

            // Evenly spaced vertical balusters between end posts
            int intervals = Mathf.Max(2, Mathf.RoundToInt(length / BalusterSpacing));
            float balusterH = TopRailCenterY - CurbHeight;
            float balusterCenterY = CurbHeight + balusterH * 0.5f;
            for (int i = 1; i < intervals; i++)
            {
                float x = Mathf.Lerp(startX, endX, i / (float)intervals);
                MansionDoorFactory.CreateLocalBox(
                    spanObj.transform,
                    $"Baluster_{i:00}",
                    new Vector3(x, balusterCenterY, 0f),
                    new Vector3(0.055f, balusterH, 0.055f),
                    RailingWoodColor,
                    removeCollider: true);
            }

            // Single invisible guard BoxCollider prevents walking/falling over the railing without collider clutter.
            GameObject guard = new GameObject("Railing_GuardCollider");
            guard.transform.SetParent(spanObj.transform, false);
            guard.transform.localPosition = new Vector3(centerX, GuardColliderHeight * 0.5f, 0f);
            var box = guard.AddComponent<BoxCollider>();
            box.size = new Vector3(length + 0.08f, GuardColliderHeight, 0.14f);
        }
    }
}
