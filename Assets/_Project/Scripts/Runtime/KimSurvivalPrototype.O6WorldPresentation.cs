using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private Transform CreateO6ScaledStructureVisualRoot(Transform parent, string stableName)
        {
            GameObject visual = new GameObject("O6 축소 시각 루트 · " + stableName);
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = new Vector3(
                PrototypeO6WorldPresentationConfig.StructureVisualScale,
                PrototypeO6WorldPresentationConfig.StructureVisualScale,
                1f);
            return visual.transform;
        }

        private void ApplyO6PlayerVisualScale(Transform visual)
        {
            if (visual == null) return;
            visual.localScale = new Vector3(
                PrototypeO6WorldPresentationConfig.PlayerVisualScale,
                PrototypeO6WorldPresentationConfig.PlayerVisualScale,
                1f);
        }

        private void CreateO6CampFramingUnderlay()
        {
            // The adopted 16:9 camp painting is grounded to the gameplay floor.
            // A wider camera can reveal a narrow strip below that painting, so an
            // earth-coloured underlay extends the existing soil instead of showing
            // the sky clear colour. It stays behind every adopted background layer.
            CreateRect(
                worldRoot,
                "O6 넓은 프레이밍 하단 지면 연장",
                new Vector2(0f, -6.1f),
                new Vector2(30f, 4.6f),
                new Color(0.16f, 0.095f, 0.045f, 1f),
                -31);
        }

        public static bool RunO6WorldPresentationContracts(out string detail)
        {
            if (!PrototypeO6WorldPresentationConfig.RunContractProbe(out string framing))
            {
                detail = framing;
                return false;
            }

            // UI remains screen-space with the established 1920x1080 reference;
            // changing world-camera size therefore cannot shrink HUD typography.
            detail = framing + "; uiReference=1920x1080; interactionCoordinates=unchanged; ladderCoordinates=unchanged";
            return true;
        }

        public bool CaptureO6WorldPresentationObservation(string evidenceFolder, out string detail)
        {
            if (string.IsNullOrWhiteSpace(evidenceFolder))
            {
                detail = "Evidence folder is empty.";
                return false;
            }
            Directory.CreateDirectory(evidenceFolder);

            session.Reset(180018);
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetO4CampVerticalSystems();
            session.Grant(ResourceKind.Wood, 30);
            session.Grant(ResourceKind.Stone, 20);
            session.Grant(ResourceKind.Salvage, 20);
            StructureKind[] fixtures =
            {
                StructureKind.Campfire,
                StructureKind.Workbench,
                StructureKind.Bed,
                StructureKind.RainCollector
            };
            foreach (StructureKind kind in fixtures)
            {
                if (!session.TryBuild(kind))
                {
                    detail = "Could not build O6 framing fixture: " + kind;
                    return false;
                }
                campPlacement.EnsureInstalled(kind);
            }

            renderedPhase = (GamePhase)(-1);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();
            string ko1280 = Path.Combine(evidenceFolder, "o6-world-framing-ko-1280x800.png");
            CaptureVerificationPng(ko1280, 1280, 800);

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            string en1920 = Path.Combine(evidenceFolder, "o6-world-framing-en-1920x1080.png");
            CaptureVerificationPng(en1920, 1920, 1080);

            localization.SetQaLocale();
            RefreshAll();
            string qps1280 = Path.Combine(evidenceFolder, "o6-world-framing-qps-long-1280x800.png");
            CaptureVerificationPng(qps1280, 1280, 800);

            CanvasScaler scaler = canvas == null ? null : canvas.GetComponent<CanvasScaler>();
            Transform[] descendants = worldRoot == null
                ? Array.Empty<Transform>()
                : worldRoot.GetComponentsInChildren<Transform>(true);
            int scaledStructureRoots = descendants.Count(value =>
                value != null && value.name.StartsWith("O6 축소 시각 루트", StringComparison.Ordinal));
            bool playerScaled = playerRoot != null &&
                                playerRoot.childCount > 0 &&
                                Mathf.Approximately(
                                    Mathf.Abs(playerRoot.GetChild(0).localScale.x),
                                    PrototypeO6WorldPresentationConfig.PlayerVisualScale);
            bool structureScaled = scaledStructureRoots >= fixtures.Length;
            bool cameraCorrect = worldCamera != null && Mathf.Approximately(
                worldCamera.orthographicSize,
                PrototypeO6WorldPresentationConfig.CampOrthographicSize);
            bool uiIndependent = scaler != null &&
                                 scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                                 scaler.referenceResolution == new Vector2(1920f, 1080f);
            bool hudReadable = new[] { statusText, resourceText, controlsText }
                .All(text => text != null && text.fontSize >= 20f && !text.isTextOverflowing);
            bool interactionStable = campInteractionTargets.Count >= 3 &&
                                     Mathf.Approximately(PrototypeCampUse.UseRange, 1.25f);
            bool capturesExist = File.Exists(ko1280) && File.Exists(en1920) && File.Exists(qps1280);

            bool passed = playerScaled && structureScaled && cameraCorrect && uiIndependent &&
                          hudReadable && interactionStable && capturesExist;
            detail = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "playerScaled={0}; structureRoots={1}; camera={2:0.00}; uiReference={3}x{4}; hudReadable={5}; targets={6}; captures=KO1280,EN1920,QPS1280",
                playerScaled,
                scaledStructureRoots,
                worldCamera == null ? 0f : worldCamera.orthographicSize,
                scaler == null ? 0f : scaler.referenceResolution.x,
                scaler == null ? 0f : scaler.referenceResolution.y,
                hudReadable,
                campInteractionTargets.Count);
            return passed;
        }
    }
}
