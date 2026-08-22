using System;
using System.Collections.Generic;
using System.IO;
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
            EnsureKoreanFallback(english, korean);
            LocalizationSettings.ProjectLocale = korean;
            LocalizationSettings.InitializeSynchronously = true;

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(PrototypeLocalization.TableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    PrototypeLocalization.TableName,
                    TableFolder,
                    new List<Locale> { korean, english });
            }

            EnsureTable(collection, korean);
            EnsureTable(collection, english);
            ImportSource(collection);
            EnsureTmpSettings();
            ConfigureFonts();

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(korean);
            EditorUtility.SetDirty(english);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Kim Survival Localization] Synced ko/en String Tables, Korean fallback, and locale font profile.");
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
            if (lines.Length < 2 || lines[0] != "Key\tko\ten")
            {
                throw new InvalidDataException("PrototypeStrings.tsv must start with: Key<TAB>ko<TAB>en");
            }

            StringTable korean = collection.GetTable(PrototypeLocalization.KoreanLocaleCode) as StringTable;
            StringTable english = collection.GetTable(PrototypeLocalization.EnglishLocaleCode) as StringTable;
            for (int i = 1; i < lines.Length; i += 1)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] columns = lines[i].Split(new[] { '\t' }, StringSplitOptions.None);
                if (columns.Length != 3 || string.IsNullOrWhiteSpace(columns[0]))
                {
                    throw new InvalidDataException("Invalid localization source row " + (i + 1));
                }

                SetEntry(korean, columns[0], Decode(columns[1]));
                SetEntry(english, columns[0], Decode(columns[2]));
            }

            LocalizationEditorSettings.SetPreloadTableFlag(korean, true);
            LocalizationEditorSettings.SetPreloadTableFlag(english, true);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(korean);
            EditorUtility.SetDirty(english);
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
