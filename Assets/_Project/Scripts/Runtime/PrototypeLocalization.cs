using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace KimSurvival
{
    public readonly struct PrototypeLocalizedText
    {
        public PrototypeLocalizedText(string key, params object[] arguments)
        {
            Key = key;
            Arguments = arguments ?? Array.Empty<object>();
        }

        public string Key { get; }
        public object[] Arguments { get; }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(Key); }
        }

        public static PrototypeLocalizedText Empty
        {
            get { return new PrototypeLocalizedText(string.Empty); }
        }
    }

    public sealed class PrototypeLocalization : IDisposable
    {
        public const string TableName = "Prototype Strings";
        public const string KoreanLocaleCode = "ko";
        public const string EnglishLocaleCode = "en";
        public const string QpsLongLocaleCode = "qps-long";
        public const string PreferenceKey = "kim_survival.locale";

        private readonly HashSet<string> reportedMissing = new HashSet<string>();
        private readonly Dictionary<string, TMP_FontAsset> generatedFonts = new Dictionary<string, TMP_FontAsset>();
        private readonly List<TMP_Text> registeredTexts = new List<TMP_Text>();
        private readonly PrototypeLocaleFontProfile fontProfile;
        private Locale koreanLocale;
        private bool disposed;

        public event Action LocaleChanged;

        public PrototypeLocalization()
        {
            LocalizationSettings.InitializeSynchronously = true;
            LocalizationSettings.InitializationOperation.WaitForCompletion();
            koreanLocale = LocalizationSettings.AvailableLocales.GetLocale(KoreanLocaleCode);
            string preferredCode = ResolveStartupLocale(PlayerPrefs.GetString(PreferenceKey, KoreanLocaleCode));
            Locale preferred = LocalizationSettings.AvailableLocales.GetLocale(preferredCode) ?? koreanLocale;
            LocalizationSettings.SelectedLocale = preferred;
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            fontProfile = Resources.Load<PrototypeLocaleFontProfile>("PrototypeLocaleFontProfile");
        }

        public string CurrentLocaleCode
        {
            get
            {
                Locale locale = LocalizationSettings.SelectedLocale;
                return locale == null ? KoreanLocaleCode : locale.Identifier.Code;
            }
        }

        public float CurrentWorldTextScale
        {
            get
            {
                PrototypeLocaleFontProfile.LocaleFontMapping mapping = fontProfile == null ? null : fontProfile.Find(CurrentLocaleCode);
                return mapping == null ? 1f : mapping.WorldTextScale;
            }
        }

        public string ResolveStartupLocale(string savedCode)
        {
            if (IsPlayerSelectableLocale(savedCode) && LocalizationSettings.AvailableLocales.GetLocale(savedCode) != null)
            {
                return savedCode;
            }

            return KoreanLocaleCode;
        }

        public bool SetLocale(string localeCode, bool persist = true)
        {
            Locale locale = IsPlayerSelectableLocale(localeCode)
                ? LocalizationSettings.AvailableLocales.GetLocale(localeCode)
                : null;
            if (locale == null)
            {
                ReportMissing("locale:" + localeCode);
                locale = koreanLocale;
            }

            if (locale == null)
            {
                return false;
            }

            LocalizationSettings.SelectedLocale = locale;
            if (persist)
            {
                PlayerPrefs.SetString(PreferenceKey, locale.Identifier.Code);
                PlayerPrefs.Save();
            }

            return true;
        }

        public bool SetQaLocale(string localeCode = QpsLongLocaleCode)
        {
            if (!string.Equals(localeCode, QpsLongLocaleCode, StringComparison.Ordinal))
            {
                return false;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
            {
                ReportMissing("qa-locale:" + localeCode);
                return false;
            }

            LocalizationSettings.SelectedLocale = locale;
            return true;
        }

        public static bool IsPlayerSelectableLocale(string localeCode)
        {
            return string.Equals(localeCode, KoreanLocaleCode, StringComparison.Ordinal) ||
                   string.Equals(localeCode, EnglishLocaleCode, StringComparison.Ordinal);
        }

        public void CycleLocale(bool persist = true)
        {
            SetLocale(CurrentLocaleCode == KoreanLocaleCode ? EnglishLocaleCode : KoreanLocaleCode, persist);
        }

        public string Format(PrototypeLocalizedText text)
        {
            return text.IsEmpty ? string.Empty : Format(text.Key, text.Arguments);
        }

        public string Format(string key, params object[] arguments)
        {
            object[] normalized = NormalizeArguments(arguments);
            Locale selected = LocalizationSettings.SelectedLocale ?? koreanLocale;
            if (TryFormat(selected, key, normalized, out string localized))
            {
                return localized;
            }

            ReportMissing((selected == null ? "none" : selected.Identifier.Code) + ":" + key);
            if (selected != koreanLocale && TryFormat(koreanLocale, key, normalized, out localized))
            {
                return localized;
            }

            ReportMissing(KoreanLocaleCode + ":" + key);
            return "⟦" + key + "⟧";
        }

        public string ResourceName(ResourceKind kind)
        {
            return Format("resource." + kind.ToString().ToLowerInvariant());
        }

        public string ResourceName(string stableResourceId, ResourceKind legacyKind)
        {
            string canonicalId = PrototypeResourcePresentation.NormalizeStableId(stableResourceId, legacyKind);
            return Format(canonicalId);
        }

        public string ResourceName(BagStack stack)
        {
            return ResourceName(stack.StableResourceId, stack.Kind);
        }

        public string StructureName(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire:
                    return Format("structure.campfire");
                case StructureKind.Workbench:
                    return Format("structure.workbench");
                case StructureKind.RainCollector:
                    return Format("structure.rain_collector");
                default:
                    return Format("structure.generic");
            }
        }

        public string DeviceName(PrototypeInputDevice device)
        {
            return Format(device == PrototypeInputDevice.Gamepad ? "device.gamepad" : "device.keyboard_mouse");
        }

        public void Register(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            registeredTexts.Add(text);
            ApplyFont(text);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
            foreach (TMP_FontAsset font in generatedFonts.Values)
            {
                if (font != null)
                {
                    UnityEngine.Object.Destroy(font);
                }
            }

            generatedFonts.Clear();
            registeredTexts.Clear();
        }

        private bool TryFormat(Locale locale, string key, object[] arguments, out string localized)
        {
            localized = null;
            if (locale == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            StringTable table = LocalizationSettings.StringDatabase.GetTable(TableName, locale);
            StringTableEntry entry = table == null ? null : table.GetEntry(key);
            if (entry == null || string.IsNullOrEmpty(entry.LocalizedValue))
            {
                return false;
            }

            localized = entry.GetLocalizedString(arguments);
            return !string.IsNullOrEmpty(localized);
        }

        private object[] NormalizeArguments(object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return Array.Empty<object>();
            }

            object[] normalized = new object[arguments.Length];
            for (int i = 0; i < arguments.Length; i += 1)
            {
                object value = arguments[i];
                if (value is ResourceKind resource)
                {
                    normalized[i] = ResourceName(resource);
                }
                else if (value is StructureKind structure)
                {
                    normalized[i] = StructureName(structure);
                }
                else if (value is PrototypeExpeditionRegionId region)
                {
                    normalized[i] = Format(PrototypeExpeditionRegionCatalog.Get(region).NameKey);
                }
                else
                {
                    normalized[i] = value;
                }
            }

            return normalized;
        }

        private void HandleLocaleChanged(Locale locale)
        {
            ApplyFonts();
            LocaleChanged?.Invoke();
        }

        private void ApplyFonts()
        {
            for (int i = registeredTexts.Count - 1; i >= 0; i -= 1)
            {
                TMP_Text text = registeredTexts[i];
                if (text == null)
                {
                    registeredTexts.RemoveAt(i);
                    continue;
                }

                ApplyFont(text);
            }
        }

        private void ApplyFont(TMP_Text text)
        {
            PrototypeLocaleFontProfile.LocaleFontMapping mapping = fontProfile == null ? null : fontProfile.Find(CurrentLocaleCode);
            if (mapping == null)
            {
                return;
            }

            TMP_FontAsset primary = mapping.PrimaryFont != null ? mapping.PrimaryFont : GetOrCreateSystemFont(mapping.PrimarySystemFont);
            if (primary == null)
            {
                return;
            }

            if (primary.fallbackFontAssetTable == null)
            {
                primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }
            else
            {
                primary.fallbackFontAssetTable.Clear();
            }
            for (int i = 0; i < mapping.FallbackFonts.Count; i += 1)
            {
                TMP_FontAsset fallback = mapping.FallbackFonts[i];
                if (fallback != null && fallback != primary && !primary.fallbackFontAssetTable.Contains(fallback))
                {
                    primary.fallbackFontAssetTable.Add(fallback);
                }
            }

            for (int i = 0; i < mapping.FallbackSystemFonts.Count; i += 1)
            {
                TMP_FontAsset fallback = GetOrCreateSystemFont(mapping.FallbackSystemFonts[i]);
                if (fallback != null && fallback != primary && !primary.fallbackFontAssetTable.Contains(fallback))
                {
                    primary.fallbackFontAssetTable.Add(fallback);
                }
            }

            text.font = primary;
        }

        private TMP_FontAsset GetOrCreateSystemFont(string family)
        {
            if (string.IsNullOrWhiteSpace(family))
            {
                return null;
            }

            if (generatedFonts.TryGetValue(family, out TMP_FontAsset cached))
            {
                return cached;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(family, string.Empty, 90);
            if (fontAsset != null)
            {
                fontAsset.name = "Runtime " + family;
            }
            else
            {
                ReportMissing("font:" + family);
            }

            generatedFonts[family] = fontAsset;
            return fontAsset;
        }

        private void ReportMissing(string identifier)
        {
            if (!reportedMissing.Add(identifier))
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[Kim Survival Localization] Missing localization data; Korean fallback requested: " + identifier);
#endif
        }
    }
}
