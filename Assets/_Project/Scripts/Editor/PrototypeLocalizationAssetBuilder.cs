using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace KimSurvival.EditorTools
{
    public static class PrototypeLocalizationAssetBuilder
    {
        private const string RootFolder = "Assets/_Project/Scripts/Localization";
        private const string LocaleFolder = RootFolder + "/Locales";
        private const string TableFolder = RootFolder + "/Tables";
        private const string ResourceFolder = RootFolder + "/Resources";
        private const string SettingsPath = RootFolder + "/PrototypeLocalizationSettings.asset";
        private const string SourcePath = RootFolder + "/PrototypeStrings.tsv";
        private const string FontProfilePath = ResourceFolder + "/PrototypeLocaleFontProfile.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Kim Survival/Sync Localization Assets")]
        public static void SyncAssets()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(LocaleFolder);
            EnsureFolder(TableFolder);
            EnsureFolder(ResourceFolder);

            LocalizationSettings settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Kim Survival Localization Settings";
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            Locale korean = EnsureLocale(PrototypeLocalization.KoreanLocaleCode, "Korean (ko)");
            Locale english = EnsureLocale(PrototypeLocalization.EnglishLocaleCode, "English (en)");
            Locale qpsLong = EnsureLocale(PrototypeLocalization.QpsLongLocaleCode, "Pseudo Long (qps-long)");
            EnsureKoreanFallback(english, korean);
            EnsureKoreanFallback(qpsLong, korean);
            LocalizationSettings.ProjectLocale = korean;
            LocalizationSettings.InitializeSynchronously = true;

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(PrototypeLocalization.TableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    PrototypeLocalization.TableName,
                    TableFolder,
                    new List<Locale> { korean, english, qpsLong });
            }

            EnsureTable(collection, korean);
            EnsureTable(collection, english);
            EnsureTable(collection, qpsLong);
            ImportSource(collection);
            EnsureTmpSettings();
            ConfigureFonts();

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(korean);
            EditorUtility.SetDirty(english);
            EditorUtility.SetDirty(qpsLong);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Kim Survival Localization] Synced player ko/en plus non-shipping qps-long String Tables, Korean fallback, and locale font profile.");
        }

        private static Locale EnsureLocale(string code, string displayName)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(code);
            if (locale != null)
            {
                return locale;
            }

            string path = LocaleFolder + "/" + displayName + ".asset";
            locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(code);
                locale.name = displayName;
                locale.LocaleName = displayName;
                AssetDatabase.CreateAsset(locale, path);
            }

            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void EnsureKoreanFallback(Locale english, Locale korean)
        {
            FallbackLocale fallback = english.Metadata.GetMetadata<FallbackLocale>();
            if (fallback == null)
            {
                fallback = new FallbackLocale(korean);
                english.Metadata.AddMetadata(fallback);
            }
            else
            {
                fallback.Locale = korean;
            }
        }

        private static void EnsureTable(StringTableCollection collection, Locale locale)
        {
            if (collection.GetTable(locale.Identifier) == null)
            {
                collection.AddNewTable(locale.Identifier);
            }
        }

        private static void ImportSource(StringTableCollection collection)
        {
            if (!File.Exists(SourcePath))
            {
                throw new FileNotFoundException("Localization source file is missing.", SourcePath);
            }

            string[] lines = File.ReadAllLines(SourcePath);
            if (lines.Length < 2 || lines[0] != "Key\tko\ten\tqps-long")
            {
                throw new InvalidDataException("PrototypeStrings.tsv must start with: Key<TAB>ko<TAB>en<TAB>qps-long");
            }

            StringTable korean = collection.GetTable(PrototypeLocalization.KoreanLocaleCode) as StringTable;
            StringTable english = collection.GetTable(PrototypeLocalization.EnglishLocaleCode) as StringTable;
            StringTable qpsLong = collection.GetTable(PrototypeLocalization.QpsLongLocaleCode) as StringTable;
            string combinedQpsLong = string.Empty;
            for (int i = 1; i < lines.Length; i += 1)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] columns = lines[i].Split(new[] { '\t' }, StringSplitOptions.None);
                if (columns.Length != 4 || string.IsNullOrWhiteSpace(columns[0]))
                {
                    throw new InvalidDataException("Invalid localization source row " + (i + 1));
                }

                ValidateQpsLong(columns[0], columns[2], columns[3]);
                combinedQpsLong += columns[3];
                SetEntry(korean, columns[0], Decode(columns[1]));
                SetEntry(english, columns[0], Decode(columns[2]));
                SetEntry(qpsLong, columns[0], Decode(columns[3]));
            }

            const string requiredGlyphProbe = "áéíóúüñ¿¡";
            if (requiredGlyphProbe.Any(glyph => combinedQpsLong.IndexOf(glyph) < 0))
            {
                throw new InvalidDataException("qps-long source must contain the full extended glyph probe: " + requiredGlyphProbe);
            }

            LocalizationEditorSettings.SetPreloadTableFlag(korean, true);
            LocalizationEditorSettings.SetPreloadTableFlag(english, true);
            LocalizationEditorSettings.SetPreloadTableFlag(qpsLong, true);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(korean);
            EditorUtility.SetDirty(english);
            EditorUtility.SetDirty(qpsLong);
        }

        private static void ValidateQpsLong(string key, string english, string qpsLong)
        {
            if (string.IsNullOrEmpty(english) && key == "dev.fallback_probe")
            {
                if (!string.IsNullOrEmpty(qpsLong))
                {
                    throw new InvalidDataException("qps-long fallback probe must remain empty: " + key);
                }

                return;
            }

            if (string.IsNullOrEmpty(english) || string.IsNullOrEmpty(qpsLong))
            {
                throw new InvalidDataException("qps-long requires an English source and pseudo value: " + key);
            }

            float ratio = qpsLong.Length / (float)english.Length;
            if (ratio < 1.32f || ratio > 1.51f)
            {
                throw new InvalidDataException("qps-long must expand English by approximately 35-50%: " + key + " ratio=" + ratio.ToString("0.000"));
            }

            if (english.Length >= 4 && (!qpsLong.StartsWith("⟦", StringComparison.Ordinal) || !qpsLong.EndsWith("⟧", StringComparison.Ordinal)))
            {
                throw new InvalidDataException("qps-long values must use pseudo-locale wrappers: " + key);
            }

            string[] englishPlaceholders = Regex.Matches(english, @"\{[^{}]+\}").Cast<Match>().Select(match => match.Value).ToArray();
            string[] qpsPlaceholders = Regex.Matches(qpsLong, @"\{[^{}]+\}").Cast<Match>().Select(match => match.Value).ToArray();
            string[] englishDigits = Regex.Matches(english, @"\d+").Cast<Match>().Select(match => match.Value).ToArray();
            string[] qpsDigits = Regex.Matches(qpsLong, @"\d+").Cast<Match>().Select(match => match.Value).ToArray();
            string[] englishTags = Regex.Matches(english, @"<[^>]+>").Cast<Match>().Select(match => match.Value).ToArray();
            string[] qpsTags = Regex.Matches(qpsLong, @"<[^>]+>").Cast<Match>().Select(match => match.Value).ToArray();
            if (!englishPlaceholders.SequenceEqual(qpsPlaceholders) || !englishDigits.SequenceEqual(qpsDigits) || !englishTags.SequenceEqual(qpsTags))
            {
                throw new InvalidDataException("qps-long must preserve Smart String placeholders, digits, and rich-text tags: " + key);
            }
        }

        private static void SetEntry(StringTable table, string key, string value)
        {
            StringTableEntry entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;
            entry.IsSmart = value.Contains("{");
        }

        private static string Decode(string value)
        {
            return value.Replace("\\n", "\n");
        }

        private static void ConfigureFonts()
        {
            PrototypeLocaleFontProfile profile = AssetDatabase.LoadAssetAtPath<PrototypeLocaleFontProfile>(FontProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PrototypeLocaleFontProfile>();
                profile.name = "Prototype Locale Font Profile";
                AssetDatabase.CreateAsset(profile, FontProfilePath);
            }

            profile.ConfigureForPrototype();
            EditorUtility.SetDirty(profile);
        }

        private static void EnsureTmpSettings()
        {
            if (HasTmpEssentials())
            {
                return;
            }

            throw new InvalidOperationException("TMP Essential Resources are missing from Assets/TextMesh Pro.");
        }

        private static bool HasTmpEssentials()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null &&
                   Shader.Find("TextMeshPro/Mobile/Distance Field") != null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
