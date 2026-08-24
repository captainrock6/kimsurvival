using System;
using System.IO;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace KimSurvival.EditorTools
{
    public static class Wave18PresentationAssetBuilder
    {
        public const string HazardAtlasPath = "Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png";
        public const string EscapeFramePath = "Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-route-signature-a-1280x800.png";
        public const string EndingFramePath = "Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png";
        public const string BindingAssetPath = "Assets/_Project/Settings/Resources/Wave18PresentationAssets.asset";

        public static void ApplySelectedPresentationAssets()
        {
            ConfigureHazardAtlas();
            ConfigureSingleSprite(EscapeFramePath);
            ConfigureSingleSprite(EndingFramePath);
            EnsureFolder("Assets/_Project/Settings");
            EnsureFolder("Assets/_Project/Settings/Resources");

            PrototypeWave18PresentationAssets binding = AssetDatabase.LoadAssetAtPath<PrototypeWave18PresentationAssets>(BindingAssetPath);
            if (binding != null && new SerializedObject(binding).FindProperty("m_Script").objectReferenceValue == null)
            {
                AssetDatabase.DeleteAsset(BindingAssetPath);
                binding = null;
            }

            if (binding == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(BindingAssetPath) != null)
                {
                    AssetDatabase.DeleteAsset(BindingAssetPath);
                }

                binding = ScriptableObject.CreateInstance<PrototypeWave18PresentationAssets>();
                AssetDatabase.CreateAsset(binding, BindingAssetPath);
            }

            SerializedObject serialized = new SerializedObject(binding);
            serialized.FindProperty("hazardPhaseAtlas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(HazardAtlasPath);
            serialized.FindProperty("escapeProjectFrame").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(EscapeFramePath);
            serialized.FindProperty("endingComicFrame").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(EndingFramePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binding);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            VerifySelectedPresentationAssets();
        }

        public static void VerifySelectedPresentationAssets()
        {
            TextureImporter hazard = AssetImporter.GetAtPath(HazardAtlasPath) as TextureImporter;
            TextureImporter escape = AssetImporter.GetAtPath(EscapeFramePath) as TextureImporter;
            TextureImporter ending = AssetImporter.GetAtPath(EndingFramePath) as TextureImporter;
            PrototypeWave18PresentationAssets binding = AssetDatabase.LoadAssetAtPath<PrototypeWave18PresentationAssets>(BindingAssetPath);
            Require(hazard != null && hazard.textureType == TextureImporterType.Sprite &&
                    hazard.spriteImportMode == SpriteImportMode.Single && !hazard.mipmapEnabled,
                "hazard atlas import must be a non-mipmapped sprite atlas");
            Require(escape != null && escape.textureType == TextureImporterType.Sprite &&
                    escape.spriteImportMode == SpriteImportMode.Single && !escape.mipmapEnabled,
                "escape frame import must be a non-mipmapped single sprite");
            Require(ending != null && ending.textureType == TextureImporterType.Sprite &&
                    ending.spriteImportMode == SpriteImportMode.Single && !ending.mipmapEnabled,
                "ending frame import must be a non-mipmapped single sprite");
            Require(binding != null && binding.IsSelectedOnlyComplete,
                "selected-only presentation binding must reference all three adopted package files");
            Require(binding.HazardPhaseAtlas.width == 1024 && binding.HazardPhaseAtlas.height == 768,
                "hazard atlas must preserve 1024x768 source dimensions");
            Require(Mathf.Approximately(binding.EscapeProjectFrame.rect.width, 1280f) &&
                    Mathf.Approximately(binding.EscapeProjectFrame.rect.height, 800f) &&
                    Mathf.Approximately(binding.EndingComicFrame.rect.width, 1280f) &&
                    Mathf.Approximately(binding.EndingComicFrame.rect.height, 800f),
                "escape and ending frames must preserve their 1280x800 source dimensions");
            Debug.Log("[Wave18] selected-only presentation imports and runtime binding PASS");
        }

        private static void ConfigureHazardAtlas()
        {
            TextureImporter importer = RequireImporter(HazardAtlasPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
#pragma warning disable 618
            importer.spritesheet = Array.Empty<SpriteMetaData>();
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        private static void ConfigureSingleSprite(string path)
        {
            TextureImporter importer = RequireImporter(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static TextureImporter RequireImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Texture importer missing", path);
            return importer;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
