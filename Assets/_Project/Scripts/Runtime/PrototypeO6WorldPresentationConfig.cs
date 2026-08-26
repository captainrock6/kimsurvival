using System;
using UnityEngine;

namespace KimSurvival
{
    /// <summary>
    /// O6 keeps gameplay coordinates and UI scale stable while giving the world
    /// more breathing room.  World visuals are scaled independently of their
    /// fixed placement anchors and interaction positions.
    /// </summary>
    public static class PrototypeO6WorldPresentationConfig
    {
        public const string ContractId = "gamejam.o6.world-framing.v1";
        public const float LegacyOrthographicSize = 5.625f;
        public const float CampOrthographicSize = 6.65f;
        public const float ExpandedCampOrthographicSize = 6.65f;
        public const float ExplorationOrthographicSize = 6.35f;
        public const float CampBackgroundWorldWidth = 24.5f;
        public const float PlayerVisualScale = 0.82f;
        public const float StructureVisualScale = 0.84f;

        public static float ProjectedVerticalRatio(float worldHeight, float orthographicSize, float visualScale)
        {
            if (orthographicSize <= 0f) return 0f;
            return Mathf.Max(0f, worldHeight) * Mathf.Max(0f, visualScale) / (orthographicSize * 2f);
        }

        public static bool RunContractProbe(out string detail)
        {
            float legacyPlayerRatio = ProjectedVerticalRatio(2.45f, LegacyOrthographicSize, 1f);
            float o6PlayerRatio = ProjectedVerticalRatio(2.45f, CampOrthographicSize, PlayerVisualScale);
            float zoomOutGain = CampOrthographicSize / LegacyOrthographicSize;
            bool passed = CampOrthographicSize >= LegacyOrthographicSize * 1.15f &&
                          ExpandedCampOrthographicSize >= CampOrthographicSize &&
                          ExplorationOrthographicSize >= LegacyOrthographicSize * 1.10f &&
                          CampBackgroundWorldWidth >= CampOrthographicSize * 2f * (16f / 9f) &&
                          PlayerVisualScale >= 0.78f && PlayerVisualScale <= 0.9f &&
                          StructureVisualScale >= 0.8f && StructureVisualScale <= 0.9f &&
                          o6PlayerRatio <= legacyPlayerRatio * 0.72f &&
                          o6PlayerRatio >= 0.14f;
            detail = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "contract={0}; campOrtho={1:0.00}; expandedOrtho={2:0.00}; explorationOrtho={3:0.00}; backgroundWidth={4:0.00}; zoomOut={5:0.000}x; playerScale={6:0.00}; structureScale={7:0.00}; playerScreenRatio={8:0.000}->{9:0.000}",
                ContractId,
                CampOrthographicSize,
                ExpandedCampOrthographicSize,
                ExplorationOrthographicSize,
                CampBackgroundWorldWidth,
                zoomOutGain,
                PlayerVisualScale,
                StructureVisualScale,
                legacyPlayerRatio,
                o6PlayerRatio);
            return passed;
        }
    }
}
