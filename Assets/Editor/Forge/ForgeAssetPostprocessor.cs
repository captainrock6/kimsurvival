// Forge 0.4 - generated project adapter. Safe to customize after installation.
#pragma warning disable 0618
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Forge.Editor
{
    [Serializable] internal sealed class ForgeImportSettings
    {
        public string textureType = "Sprite";
        public string spriteMode = "Single";
        public float pixelsPerUnit = 100;
        public string filterMode = "Bilinear";
        public string compression = "CompressedHQ";
        public int maxSize = 2048;
        public bool mipmaps;
        public bool alphaIsTransparency = true;
        public bool nineSliceHint;
    }

    [Serializable] internal sealed class ForgeSliceGrid
    {
        public int columns;
        public int rows;
        public int frameWidth;
        public int frameHeight;
        public int frameCount;
    }

    [Serializable] internal sealed class ForgeImportManifest
    {
        public string assetId;
        public string jobId;
        public string assetType;
        public string[] sourceFiles;
        public ForgeImportSettings import;
        public ForgeSliceGrid sliceGrid;
    }

    internal sealed class ForgeAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string manifestPath;
            ForgeImportManifest manifest = FindManifest(assetPath, out manifestPath);
            if (manifest == null || manifest.import == null || !ContainsFile(manifest, assetPath)) return;

            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null) return;
            importer.textureType = manifest.import.textureType == "Default" ? TextureImporterType.Default : TextureImporterType.Sprite;
            importer.alphaIsTransparency = manifest.import.alphaIsTransparency;
            importer.mipmapEnabled = manifest.import.mipmaps;
            importer.maxTextureSize = Mathf.Clamp(manifest.import.maxSize, 32, 8192);
            importer.spritePixelsPerUnit = Mathf.Max(1, manifest.import.pixelsPerUnit);
            importer.filterMode = manifest.import.filterMode == "Point" ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = manifest.import.compression == "Uncompressed"
                ? TextureImporterCompression.Uncompressed
                : manifest.import.compression == "CompressedLQ" ? TextureImporterCompression.CompressedLQ : TextureImporterCompression.CompressedHQ;

            if (importer.textureType == TextureImporterType.Sprite)
            {
                bool sliced = manifest.sliceGrid != null && manifest.sliceGrid.frameCount > 1 && Path.GetFileName(assetPath) == "sprite-sheet.png";
                importer.spriteImportMode = sliced ? SpriteImportMode.Multiple : SpriteImportMode.Single;
                if (sliced) ApplyGrid(importer, manifest.sliceGrid);
            }
        }

        private static void ApplyGrid(TextureImporter importer, ForgeSliceGrid grid)
        {
            int count = Mathf.Min(grid.frameCount, grid.columns * grid.rows);
            SpriteMetaData[] sprites = new SpriteMetaData[count];
            int textureHeight = grid.rows * grid.frameHeight;
            for (int index = 0; index < count; index++)
            {
                int column = index % grid.columns;
                int row = index / grid.columns;
                sprites[index] = new SpriteMetaData
                {
                    name = "frame-" + (index + 1).ToString("D4"),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(column * grid.frameWidth, textureHeight - (row + 1) * grid.frameHeight, grid.frameWidth, grid.frameHeight)
                };
            }
            importer.spritesheet = sprites;
        }

        private static bool ContainsFile(ForgeImportManifest manifest, string candidate)
        {
            if (manifest.sourceFiles == null) return false;
            string name = Path.GetFileName(candidate);
            foreach (string source in manifest.sourceFiles)
                if (string.Equals(Path.GetFileName(source), name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static ForgeImportManifest FindManifest(string candidate, out string manifestPath)
        {
            string directory = Path.GetDirectoryName(candidate);
            while (!string.IsNullOrEmpty(directory) && directory.Replace('\\', '/').StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                manifestPath = Path.Combine(directory, "forge-import.json");
                if (File.Exists(manifestPath))
                {
                    try { return JsonUtility.FromJson<ForgeImportManifest>(File.ReadAllText(manifestPath)); }
                    catch (Exception error) { Debug.LogWarning("Forge import manifest could not be read: " + error.Message); return null; }
                }
                directory = Path.GetDirectoryName(directory);
            }
            manifestPath = null;
            return null;
        }
    }
}
#pragma warning restore 0618
