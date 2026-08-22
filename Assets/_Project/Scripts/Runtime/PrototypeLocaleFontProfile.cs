using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KimSurvival
{
    [CreateAssetMenu(menuName = "Kim Survival/Locale Font Profile", fileName = "PrototypeLocaleFontProfile")]
    public sealed class PrototypeLocaleFontProfile : ScriptableObject
    {
        [Serializable]
        public sealed class LocaleFontMapping
        {
            [SerializeField] private string localeCode;
            [SerializeField] private TMP_FontAsset primaryFont;
            [SerializeField] private List<TMP_FontAsset> fallbackFonts = new List<TMP_FontAsset>();
            [SerializeField] private string primarySystemFont;
            [SerializeField] private List<string> fallbackSystemFonts = new List<string>();
            [SerializeField] private float worldTextScale = 1f;

            public string LocaleCode { get { return localeCode; } }
            public TMP_FontAsset PrimaryFont { get { return primaryFont; } }
            public IReadOnlyList<TMP_FontAsset> FallbackFonts { get { return fallbackFonts; } }
            public string PrimarySystemFont { get { return primarySystemFont; } }
            public IReadOnlyList<string> FallbackSystemFonts { get { return fallbackSystemFonts; } }
            public float WorldTextScale { get { return worldTextScale > 0f ? worldTextScale : 1f; } }

            public LocaleFontMapping(string code, string systemFont, float worldScale, params string[] systemFallbacks)
            {
                localeCode = code;
                primarySystemFont = systemFont;
                worldTextScale = worldScale;
                fallbackSystemFonts = new List<string>(systemFallbacks ?? Array.Empty<string>());
            }
        }

        [SerializeField] private List<LocaleFontMapping> mappings = new List<LocaleFontMapping>();

        public LocaleFontMapping Find(string localeCode)
        {
            LocaleFontMapping fallback = null;
            for (int i = 0; i < mappings.Count; i += 1)
            {
                LocaleFontMapping mapping = mappings[i];
                if (mapping == null)
                {
                    continue;
                }

                if (mapping.LocaleCode == PrototypeLocalization.KoreanLocaleCode)
                {
                    fallback = mapping;
                }

                if (mapping.LocaleCode == localeCode)
                {
                    return mapping;
                }
            }

            return fallback;
        }

#if UNITY_EDITOR
        public void ConfigureForPrototype()
        {
            if (!Contains(PrototypeLocalization.KoreanLocaleCode))
            {
                mappings.Add(new LocaleFontMapping("ko", "Malgun Gothic", 1f, "Arial"));
            }

            if (!Contains(PrototypeLocalization.EnglishLocaleCode))
            {
                mappings.Add(new LocaleFontMapping("en", "Arial", 1.62f, "Malgun Gothic"));
            }
        }

        private bool Contains(string localeCode)
        {
            for (int i = 0; i < mappings.Count; i += 1)
            {
                if (mappings[i] != null && mappings[i].LocaleCode == localeCode)
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}
