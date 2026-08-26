using System;
using System.Collections.Generic;
using UnityEngine;

namespace KimSurvival
{
    /// <summary>
    /// One canonical visual identity for every material that can move from a
    /// search node, through Mr. Kim's bag, and into camp storage.
    /// </summary>
    public readonly struct PrototypeResourceVisual
    {
        public PrototypeResourceVisual(string stableResourceId, ResourceKind legacyKind, Color accent)
        {
            StableResourceId = stableResourceId;
            LegacyKind = legacyKind;
            Accent = accent;
        }

        public string StableResourceId { get; }
        public ResourceKind LegacyKind { get; }
        public Color Accent { get; }
    }

    public static class PrototypeResourcePresentation
    {
        private static readonly PrototypeResourceVisual[] Catalog =
        {
            Visual("resource.wood", ResourceKind.Wood, 0.78f, 0.48f, 0.22f),
            Visual("resource.fiber", ResourceKind.Wood, 0.78f, 0.69f, 0.33f),
            Visual("resource.fabric", ResourceKind.Salvage, 0.92f, 0.78f, 0.57f),
            Visual("resource.food", ResourceKind.Food, 0.47f, 0.78f, 0.30f),
            Visual("resource.medicine", ResourceKind.Food, 0.34f, 0.84f, 0.68f),
            Visual("resource.stone", ResourceKind.Stone, 0.61f, 0.67f, 0.72f),
            Visual("resource.metal", ResourceKind.Stone, 0.42f, 0.64f, 0.78f),
            Visual("resource.wire", ResourceKind.Salvage, 0.35f, 0.78f, 0.88f),
            Visual("resource.fuel", ResourceKind.Salvage, 0.94f, 0.62f, 0.20f),
            Visual("resource.chemicals", ResourceKind.Salvage, 0.76f, 0.48f, 0.86f),
            Visual("resource.electronics", ResourceKind.Salvage, 0.34f, 0.58f, 0.94f),
            Visual("resource.salvage", ResourceKind.Salvage, 0.91f, 0.47f, 0.19f)
        };

        private static readonly Dictionary<string, PrototypeResourceVisual> ByStableId = BuildLookup();

        public static IReadOnlyList<PrototypeResourceVisual> All
        {
            get { return Catalog; }
        }

        public static string NormalizeStableId(string stableResourceId, ResourceKind legacyKind)
        {
            return string.IsNullOrWhiteSpace(stableResourceId)
                ? GameSession.StableResourceIdForLegacy(legacyKind)
                : stableResourceId;
        }

        public static PrototypeResourceVisual Get(string stableResourceId, ResourceKind legacyKind)
        {
            string canonicalId = NormalizeStableId(stableResourceId, legacyKind);
            return ByStableId.TryGetValue(canonicalId, out PrototypeResourceVisual visual)
                ? visual
                : new PrototypeResourceVisual(canonicalId, legacyKind, LegacyAccent(legacyKind));
        }

        public static Color Accent(string stableResourceId, ResourceKind legacyKind, float alpha = 1f)
        {
            Color accent = Get(stableResourceId, legacyKind).Accent;
            accent.a = alpha;
            return accent;
        }

        public static Color Surface(string stableResourceId, ResourceKind legacyKind, bool focused)
        {
            Color accent = Get(stableResourceId, legacyKind).Accent;
            float multiplier = focused ? 0.54f : 0.34f;
            return new Color(
                Mathf.Max(0.055f, accent.r * multiplier),
                Mathf.Max(0.065f, accent.g * multiplier),
                Mathf.Max(0.075f, accent.b * multiplier),
                0.98f);
        }

        public static string AccentHex(string stableResourceId, ResourceKind legacyKind)
        {
            return ColorUtility.ToHtmlStringRGB(Accent(stableResourceId, legacyKind));
        }

        public static bool RunContractProbe(out string detail)
        {
            if (Catalog.Length != 12 || ByStableId.Count != Catalog.Length)
            {
                detail = "Expected twelve unique stable material presentations.";
                return false;
            }

            var accents = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < Catalog.Length; index += 1)
            {
                PrototypeResourceVisual visual = Catalog[index];
                if (!GameSession.TryGetLegacyResourceKind(visual.StableResourceId, out ResourceKind expectedKind) ||
                    expectedKind != visual.LegacyKind)
                {
                    detail = visual.StableResourceId + " does not match the stable-storage catalog.";
                    return false;
                }

                if (!accents.Add(AccentHex(visual.StableResourceId, visual.LegacyKind)))
                {
                    detail = visual.StableResourceId + " does not have a distinct material accent.";
                    return false;
                }
            }

            detail = "PASS 12 stable IDs, legacy adapters, and distinct accents.";
            return true;
        }

        private static PrototypeResourceVisual Visual(
            string stableResourceId,
            ResourceKind legacyKind,
            float red,
            float green,
            float blue)
        {
            return new PrototypeResourceVisual(stableResourceId, legacyKind, new Color(red, green, blue, 1f));
        }

        private static Dictionary<string, PrototypeResourceVisual> BuildLookup()
        {
            var result = new Dictionary<string, PrototypeResourceVisual>(StringComparer.Ordinal);
            for (int index = 0; index < Catalog.Length; index += 1)
            {
                result.Add(Catalog[index].StableResourceId, Catalog[index]);
            }
            return result;
        }

        private static Color LegacyAccent(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Wood:
                    return new Color(0.78f, 0.48f, 0.22f, 1f);
                case ResourceKind.Stone:
                    return new Color(0.61f, 0.67f, 0.72f, 1f);
                case ResourceKind.Food:
                    return new Color(0.47f, 0.78f, 0.30f, 1f);
                case ResourceKind.Salvage:
                    return new Color(0.91f, 0.47f, 0.19f, 1f);
                default:
                    return Color.white;
            }
        }
    }
}
