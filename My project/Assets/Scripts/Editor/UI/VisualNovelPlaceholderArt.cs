using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectRac.Editor.UI
{
    public static class VisualNovelPlaceholderArt
    {
        public const string ArtFolder = "Assets/Art/UI/VN";
        public const string MaidSpritePath = ArtFolder + "/vn_placeholder_maid.png";
        public const string GuestSpritePath = ArtFolder + "/vn_placeholder_guest.png";
        public const string ArrowSpritePath = ArtFolder + "/vn_continue_arrow.png";

        public static Sprite EnsureMaidPlaceholder()
        {
            return EnsurePlaceholderPortrait(
                MaidSpritePath,
                new Color(0.14f, 0.13f, 0.16f, 1f),
                new Color(0.88f, 0.87f, 0.90f, 1f),
                new Color(0.45f, 0.22f, 0.56f, 1f));
        }

        public static Sprite EnsureGuestPlaceholder()
        {
            return EnsurePlaceholderPortrait(
                GuestSpritePath,
                new Color(0.16f, 0.18f, 0.22f, 1f),
                new Color(0.76f, 0.72f, 0.66f, 1f),
                new Color(0.48f, 0.30f, 0.20f, 1f));
        }

        public static Sprite EnsureContinueArrow()
        {
            if (!File.Exists(ArrowSpritePath))
            {
                EnsureFolder();
                const int w = 32;
                const int h = 20;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                {
                    float t = y / (float)(h - 1);
                    float halfW = t * (w * 0.45f);
                    for (int x = 0; x < w; x++)
                    {
                        bool inside = Mathf.Abs(x - (w - 1) * 0.5f) <= halfW && y >= 2 && y <= h - 2;
                        tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(ArrowSpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(ArrowSpritePath, ImportAssetOptions.ForceUpdate);
            }

            return ConfigureAndLoadSprite(ArrowSpritePath);
        }

        private static Sprite EnsurePlaceholderPortrait(string assetPath, Color coatColor, Color trimColor, Color accentColor)
        {
            if (!File.Exists(assetPath))
            {
                EnsureFolder();
                const int w = 320;
                const int h = 560;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var clear = new Color(0f, 0f, 0f, 0f);
                var skin = new Color(0.86f, 0.77f, 0.72f, 1f);
                var outline = new Color(0.05f, 0.05f, 0.07f, 1f);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float nx = (x - w * 0.5f) / (w * 0.5f);
                        float ny = y / (float)h;
                        Color c = clear;

                        float halfBodyW = ny < 0.35f ? Mathf.Lerp(0.82f, 0.44f, ny / 0.35f)
                                        : ny < 0.66f ? Mathf.Lerp(0.44f, 0.62f, (ny - 0.35f) / 0.31f)
                                        : 0f;
                        if (ny < 0.66f && Mathf.Abs(nx) < halfBodyW)
                        {
                            c = Mathf.Abs(nx) > halfBodyW - 0.035f ? outline : coatColor;
                            if (ny < 0.60f && Mathf.Abs(nx) < halfBodyW * 0.52f) c = trimColor;
                        }

                        if (ny >= 0.57f && ny < 0.67f && Mathf.Abs(nx) < 0.22f) c = accentColor;
                        if (ny >= 0.65f && ny < 0.73f && Mathf.Abs(nx) < 0.13f) c = skin;

                        float dx = nx / 0.32f;
                        float dy = (ny - 0.81f) / 0.115f;
                        float headDist = dx * dx + dy * dy;
                        if (headDist <= 1f) c = headDist > 0.86f ? outline : skin;

                        float hdx = nx / 0.37f;
                        float hdy = (ny - 0.87f) / 0.095f;
                        if (hdx * hdx + hdy * hdy <= 1f && ny > 0.82f) c = trimColor;

                        tex.SetPixel(x, y, c);
                    }
                }

                tex.Apply();
                File.WriteAllBytes(assetPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            return ConfigureAndLoadSprite(assetPath);
        }

        private static Sprite ConfigureAndLoadSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder("Assets/Art/UI")) AssetDatabase.CreateFolder("Assets/Art", "UI");
            if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder("Assets/Art/UI", "VN");
        }
    }
}
