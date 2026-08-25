// Forge 0.4 - generated project adapter. Safe to customize after installation.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
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
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                throw new InvalidOperationException("Sprite editor data provider is unavailable for " + importer.assetPath);
            }
            provider.InitSpriteEditorDataProvider();

            Dictionary<string, GUID> existingIds = new Dictionary<string, GUID>(StringComparer.Ordinal);
            foreach (SpriteRect existing in provider.GetSpriteRects())
            {
                if (existing != null && !string.IsNullOrEmpty(existing.name))
                {
                    existingIds[existing.name] = existing.spriteID;
                }
            }

            int count = Mathf.Min(grid.frameCount, grid.columns * grid.rows);
            SpriteRect[] sprites = new SpriteRect[count];
            List<SpriteNameFileIdPair> nameFileIdPairs = new List<SpriteNameFileIdPair>(count);
            int textureHeight = grid.rows * grid.frameHeight;
            for (int index = 0; index < count; index++)
            {
                int column = index % grid.columns;
                int row = index / grid.columns;
                string name = "frame-" + (index + 1).ToString("D4");
                GUID spriteId;
                if (!existingIds.TryGetValue(name, out spriteId) || spriteId.Empty())
                {
                    spriteId = GUID.Generate();
                }
                sprites[index] = new SpriteRect
                {
                    name = name,
                    spriteID = spriteId,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(column * grid.frameWidth, textureHeight - (row + 1) * grid.frameHeight, grid.frameWidth, grid.frameHeight)
                };
                nameFileIdPairs.Add(new SpriteNameFileIdPair(name, spriteId));
            }

            provider.SetSpriteRects(sprites);
            ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProvider != null)
            {
                nameProvider.SetNameFileIdPairs(nameFileIdPairs);
            }
            provider.Apply();
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
