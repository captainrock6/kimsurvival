using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        // Original icon-first affordance for O5. It deliberately carries no route or
        // object name: text is reserved for the nearest-object prompt and popup.
        private void CreateO5InteractableMarker(Transform parent, Vector2 localPosition, string semanticId, bool depleted = false)
        {
            GameObject root = new GameObject("상호작용 아이콘 · " + (semanticId ?? string.Empty));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            Color outer = depleted
                ? new Color(0.43f, 0.49f, 0.48f, 0.82f)
                : new Color(1f, 0.82f, 0.28f, 0.98f);
            Color inner = depleted
                ? new Color(0.14f, 0.18f, 0.18f, 0.9f)
                : new Color(0.025f, 0.15f, 0.17f, 0.98f);

            GameObject outerDiamond = CreateRect(root.transform, "아이콘 외곽", Vector2.zero, new Vector2(0.42f, 0.42f), outer, 18);
            outerDiamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject innerDiamond = CreateRect(root.transform, "아이콘 중심", Vector2.zero, new Vector2(0.29f, 0.29f), inner, 19);
            innerDiamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            CreateRect(root.transform, "상호작용 손잡이", new Vector2(0f, -0.34f), new Vector2(0.07f, 0.3f), outer, 17);
            if (depleted)
            {
                GameObject slash = CreateRect(root.transform, "고갈 표시", Vector2.zero, new Vector2(0.07f, 0.3f), outer, 20);
                slash.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            }
            else
            {
                CreateRect(root.transform, "상호작용 점", Vector2.zero, new Vector2(0.09f, 0.09f), outer, 20);
            }
        }

        public bool CaptureO5HumanCorrectionObservation(string evidenceFolder, out string detail)
        {
            Directory.CreateDirectory(evidenceFolder);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            campInteraction.ClosePopup();
            expeditionMapSelection.Close();
            RefreshAll();

            string[] escapeIds = { PrototypeRaftEscapeConfig.EscapeId, "escape.smoke", "escape.radio" };
            bool facilitiesAbsent = escapeIds.All(id => !hazardEscapeEndingRuntime.EscapeDirector.GetState(id).FacilityBuilt) &&
                                    worldRoot.GetComponentsInChildren<Transform>(true).All(view =>
                                        !view.name.StartsWith("facility.shore-launch", StringComparison.Ordinal) &&
                                        !view.name.StartsWith("Camp Signal Stack", StringComparison.Ordinal) &&
                                        !view.name.StartsWith("Camp Radio Bench", StringComparison.Ordinal));
            int markerCount = worldRoot.GetComponentsInChildren<Transform>(true)
                .Count(view => view.name.StartsWith("상호작용 아이콘", StringComparison.Ordinal));
            CaptureVerificationPng(Path.Combine(evidenceFolder, "o5-initial-camp-icon-first-ko-1280x800.png"), 1280, 800);

            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.StoragePlanning);
            bool buildChoicesVisible = smokeProjectButton.gameObject.activeSelf &&
                                       radioProjectButton.gameObject.activeSelf && raftProjectButton.gameObject.activeSelf;
            CaptureVerificationPng(Path.Combine(evidenceFolder, "o5-escape-build-choices-ko-1280x800.png"), 1280, 800);
            campInteraction.ClosePopup();

            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.ExpeditionMap));
            RefreshAll();
            UseNearestCampTarget();
            bool mapShowsPercent = expeditionMapSelection.IsOpen && expeditionMapPanel.activeSelf &&
                                   expeditionRegionButtons.All(button => button.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("%")) &&
                                   expeditionMapDetailText.text.Contains("%");
            CaptureVerificationPng(Path.Combine(evidenceFolder, "o5-map-resource-percent-ko-1280x800.png"), 1280, 800);
            expeditionMapSelection.Close();
            campInteraction.ClosePopup();

            GameSessionStableState resources = session.CaptureStableState();
            resources.Storage = GameSession.GetStableResourceCatalog().Select(entry => new StableResourceAmount(
                entry.StableResourceId,
                entry.LegacyKind,
                entry.StableResourceId == "resource.wood" || entry.StableResourceId == "resource.stone" ? 20 : entry.Amount)).ToArray();
            bool resourceFixture = session.RestoreStableState(resources);
            bool smokeBuilt = resourceFixture && hazardEscapeEndingRuntime.EscapeDirector.TryBuildFacility(session, "escape.smoke");
            RefreshAll();
            bool smokeVisible = worldRoot.GetComponentsInChildren<Transform>(true)
                .Any(view => view.name.StartsWith("Camp Signal Stack", StringComparison.Ordinal));
            bool persistentRouteTextAbsent = escapeRouteWorldLabels.Count == 0;
            CaptureVerificationPng(Path.Combine(evidenceFolder, "o5-built-smoke-icon-only-ko-1280x800.png"), 1280, 800);

            bool passed = facilitiesAbsent && markerCount >= 3 && buildChoicesVisible && mapShowsPercent &&
                          smokeBuilt && smokeVisible && persistentRouteTextAbsent;
            detail = "facilitiesAbsent=" + facilitiesAbsent +
                     " markers=" + markerCount +
                     " buildChoices=" + buildChoicesVisible +
                     " mapPercent=" + mapShowsPercent +
                     " smokeBuiltVisible=" + (smokeBuilt && smokeVisible) +
                     " persistentRouteTextAbsent=" + persistentRouteTextAbsent;
            return passed;
        }
    }
}
