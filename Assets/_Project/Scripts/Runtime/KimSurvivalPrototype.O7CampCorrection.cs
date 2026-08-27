using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KimSurvival
{
    /// <summary>
    /// O8 correction policy shared by the production UI and hybrid placement
    /// placement domain. Keeping the rules here makes the human-test contract
    /// independently testable instead of relying on screen coordinates.
    /// </summary>
    public static class PrototypeO7CampCorrectionPolicy
    {
        public const string ContractId = "gamejam.o8-camp-hybrid-free-placement.v1";

        public static bool CanOwnExpansionPreview(
            PrototypeCampInteractionTargetKind targetKind,
            string stableTargetId,
            string currentRoomId)
        {
            return targetKind == PrototypeCampInteractionTargetKind.StoragePlanning &&
                   string.Equals(stableTargetId, PrototypeCampModuleCatalog.VisiblePlanningPointId, StringComparison.Ordinal) &&
                   string.Equals(currentRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal);
        }

        public static bool IsPortableMultiFloorFacility(StructureKind kind)
        {
            return kind == StructureKind.Workbench ||
                   kind == StructureKind.Bed ||
                   kind == StructureKind.Sofa ||
                   kind == StructureKind.RainCollector;
        }

        public static CampPlacementZone RequiredAnchorZone(StructureKind kind, string roomId)
        {
            // O8: ordinary facilities use collision-checked coordinate placement on
            // every completed floor. Campfire/escape/connector fixtures remain fixed.
            return CampPlacementZone.GeneralGround;
        }

        public static bool IsCompletedCompatibleRoom(
            StructureKind kind,
            string roomId,
            PrototypeCampModuleExpansion expansion)
        {
            if (!IsPortableMultiFloorFacility(kind) || string.IsNullOrWhiteSpace(roomId))
            {
                return false;
            }

            if (string.Equals(roomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
            {
                return true;
            }

            return expansion != null &&
                   expansion.IsRoomCommitted(roomId) &&
                   PrototypeCampPlacement.TryGetRoomZone(roomId, out _);
        }
    }

    public sealed partial class KimSurvivalPrototype
    {
        private static readonly Vector2 O7CampResultAnchorMin = new Vector2(0.25f, 0.69f);
        private static readonly Vector2 O7CampResultAnchorMax = new Vector2(0.75f, 0.79f);

        private GameObject o7CampResultBanner;
        private Image o7CampResultBannerImage;
        private TMP_Text o7CampResultBannerText;
        private bool o7CampPopupResultSucceeded;

        /// <summary>
        /// Production selection hook. Call after all camp targets have been
        /// collected and before PrototypeCampInteraction.UpdateSelection.
        /// </summary>
        private void ApplyO7VisibleExpansionPlanningPolicy()
        {
            campInteractionTargets.RemoveAll(target =>
                target.Kind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot);
        }

        /// <summary>
        /// Production preview hook. This must be checked before beginning any
        /// expansion preview transition.
        /// </summary>
        private bool IsO7ExpansionPreviewOriginAllowed()
        {
            return PrototypeO7CampCorrectionPolicy.CanOwnExpansionPreview(
                campInteraction.OpenPopupKind,
                campInteraction.OpenPopupTargetId,
                campUse.CurrentRoomId);
        }

        private bool BeginO7CampModulePreviewFromVisiblePlanningPoint(
            CampModuleReturnSnapshot snapshot,
            CampModuleArchetype initialArchetype)
        {
            return campModuleExpansion.BeginPreviewFromVisiblePlanningPoint(
                snapshot,
                campInteraction.OpenPopupTargetId,
                initialArchetype);
        }

        /// <summary>
        /// Production popup hook. Call on every RefreshCampInteractionUi pass,
        /// before its early return when no popup is open.
        /// </summary>
        private void RefreshO7CampPopupResultBanner()
        {
            EnsureO7CampPopupResultBanner();
            if (o7CampResultBanner == null)
            {
                return;
            }

            bool visible = session != null &&
                           session.Phase == GamePhase.Camp &&
                           campInteraction != null &&
                           campInteraction.IsPopupOpen &&
                           !o6CampPopupResult.IsEmpty;
            o7CampResultBanner.SetActive(visible);
            if (!visible)
            {
                return;
            }

            bool qpsLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;
            string prefix = localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode
                ? (o7CampPopupResultSucceeded ? "완료" : "불가")
                : (o7CampPopupResultSucceeded ? "DONE" : "UNAVAILABLE");
            o7CampResultBannerText.text = prefix + " · " + localization.Format(o6CampPopupResult);
            o7CampResultBannerText.fontSizeMin = qpsLong ? 16f : 20f;
            o7CampResultBannerText.fontSizeMax = qpsLong ? 22f : 28f;
            o7CampResultBannerText.maxVisibleLines = 2;
            o7CampResultBannerImage.color = o7CampPopupResultSucceeded
                ? new Color(0.015f, 0.29f, 0.16f, 0.985f)
                : new Color(0.48f, 0.045f, 0.035f, 0.985f);
            o7CampResultBannerText.ForceMeshUpdate(true, true);
        }

        private void EnsureO7CampPopupResultBanner()
        {
            if (o7CampResultBanner != null || canvas == null || localization == null)
            {
                return;
            }

            RectTransform panel = CreatePanel(
                "O7 중앙 상단 설비 행동 결과",
                canvas.transform,
                O7CampResultAnchorMin,
                O7CampResultAnchorMax,
                Vector2.zero,
                Vector2.zero,
                new Color(0.48f, 0.045f, 0.035f, 0.985f));
            o7CampResultBanner = panel.gameObject;
            o7CampResultBannerImage = panel.GetComponent<Image>();
            o7CampResultBannerImage.raycastTarget = false;
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.86f, 0.48f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            o7CampResultBannerText = CreateText(
                "O7 설비 행동 성공·실패 이유",
                panel,
                Vector2.zero,
                Vector2.one,
                new Vector2(22f, 8f),
                new Vector2(-22f, -8f),
                28,
                TextAnchor.MiddleCenter,
                Color.white);
            o7CampResultBannerText.fontStyle = FontStyles.Bold;
            o7CampResultBannerText.enableAutoSizing = true;
            o7CampResultBannerText.textWrappingMode = TextWrappingModes.Normal;
            o7CampResultBannerText.overflowMode = TextOverflowModes.Overflow;
            o7CampResultBannerText.raycastTarget = false;
            o7CampResultBanner.SetActive(false);
        }

        private bool IsO7CampPopupResultBannerReadable()
        {
            if (o7CampResultBanner == null || !o7CampResultBanner.activeSelf ||
                o7CampResultBannerText == null || string.IsNullOrWhiteSpace(o7CampResultBannerText.text))
            {
                return false;
            }

            RectTransform rect = o7CampResultBanner.GetComponent<RectTransform>();
            return rect.anchorMin == O7CampResultAnchorMin &&
                   rect.anchorMax == O7CampResultAnchorMax &&
                   !o7CampResultBannerText.isTextOverflowing &&
                   o7CampResultBannerText.color.grayscale > 0.9f &&
                   o7CampResultBannerImage.color.a >= 0.98f;
        }

        public static bool RunO7CampCorrectionDomainContracts(out string detail)
        {
            var session = new GameSession();
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Salvage, 12);
            if (!session.TryBuild(StructureKind.Workbench))
            {
                detail = "Workbench prerequisite could not be built.";
                return false;
            }

            var expansion = new PrototypeCampModuleExpansion(
                PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance());
            CampModuleReturnSnapshot startSnapshot = new CampModuleReturnSnapshot(
                Vector2.zero,
                1f,
                PrototypeCampModuleCatalog.StartRoomId);
            CampModuleReturnSnapshot upperSnapshot = new CampModuleReturnSnapshot(
                Vector2.zero,
                1f,
                PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).RoomId);

            bool rejectedInvisibleSlot = !expansion.BeginPreviewFromVisiblePlanningPoint(
                startSnapshot,
                "slot.start.upper",
                CampModuleArchetype.Upper);
            bool rejectedBlankPoint = !expansion.BeginPreviewFromVisiblePlanningPoint(
                startSnapshot,
                string.Empty,
                CampModuleArchetype.Upper);
            bool rejectedRemoteRoom = !expansion.BeginPreviewFromVisiblePlanningPoint(
                upperSnapshot,
                PrototypeCampModuleCatalog.VisiblePlanningPointId,
                CampModuleArchetype.Basement);
            if (!rejectedInvisibleSlot || !rejectedBlankPoint || !rejectedRemoteRoom || expansion.IsPreviewActive)
            {
                detail = "Expansion preview accepted a hidden slot, blank point, or non-start-room origin.";
                return false;
            }

            if (!expansion.BeginPreviewFromVisiblePlanningPoint(
                    startSnapshot,
                    PrototypeCampModuleCatalog.VisiblePlanningPointId,
                    CampModuleArchetype.Upper) ||
                expansion.TryCommit(session, new CampModuleValidationContext()) != CampModuleCommitStatus.Succeeded ||
                !expansion.BeginPreviewFromVisiblePlanningPoint(
                    startSnapshot,
                    PrototypeCampModuleCatalog.VisiblePlanningPointId,
                    CampModuleArchetype.Basement) ||
                expansion.TryCommit(session, new CampModuleValidationContext()) != CampModuleCommitStatus.Succeeded)
            {
                detail = "Visible planning point could not build both vertical rooms.";
                return false;
            }

            string[] rooms =
            {
                PrototypeCampModuleCatalog.StartRoomId,
                PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).RoomId,
                PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement).RoomId
            };
            StructureKind[] portable =
            {
                StructureKind.Workbench,
                StructureKind.Bed,
                StructureKind.Sofa,
                StructureKind.RainCollector
            };

            foreach (StructureKind kind in portable)
            {
                foreach (string roomId in rooms)
                {
                    if (!PrototypeO7CampCorrectionPolicy.IsCompletedCompatibleRoom(kind, roomId, expansion) ||
                        !PrototypeCampPlacement.TryGetRoomZone(roomId, out CampPlacementRoomZone zone))
                    {
                        detail = kind + " did not recognize completed room " + roomId + ".";
                        return false;
                    }

                    var placement = new PrototypeCampPlacement();
                    placement.Begin(kind, false, zone);
                    if (placement.CurrentValidity != CampPlacementValidity.Valid || !placement.Commit() ||
                        !placement.IsInstalledInRoom(kind, roomId) ||
                        string.IsNullOrWhiteSpace(placement.GetInstalledAnchorId(kind)))
                    {
                        detail = kind + " could not commit to a compatible free coordinate in " + roomId + ".";
                        return false;
                    }

                    PrototypeCampPlacementSnapshot snapshot = placement.CaptureSnapshot();
                    string expectedZoneId = PrototypeCampPlacement.GetZoneId(
                        PrototypeO7CampCorrectionPolicy.RequiredAnchorZone(kind, roomId));
                    if (snapshot.Installed.Length != 1 ||
                        !string.Equals(snapshot.Installed[0].StablePlacementZoneId, expectedZoneId, StringComparison.Ordinal))
                    {
                        detail = kind + " used an unstable or incompatible anchor zone in " + roomId + ".";
                        return false;
                    }
                    var restored = new PrototypeCampPlacement();
                    if (!restored.RestoreSnapshot(
                            JsonUtility.FromJson<PrototypeCampPlacementSnapshot>(JsonUtility.ToJson(snapshot))) ||
                        restored.InstalledCount != 1 ||
                        !restored.IsInstalledInRoom(kind, roomId) ||
                        !string.Equals(
                            restored.GetInstalledAnchorId(kind),
                            placement.GetInstalledAnchorId(kind),
                            StringComparison.Ordinal))
                    {
                        detail = kind + " free-coordinate save/restore failed in " + roomId + ".";
                        return false;
                    }
                }
            }

            detail = "PASS · visible planning-marker-only preview; hidden/blank/remote origins rejected; " +
                     "workbench, bed, sofa and rain collector free-coordinate placement/save/restore across start, upper and basement rooms.";
            return true;
        }

        public bool CaptureO7CampCorrectionObservation(string evidenceFolder, out string detail)
        {
            if (string.IsNullOrWhiteSpace(evidenceFolder))
            {
                detail = "Evidence folder is empty.";
                return false;
            }

            Directory.CreateDirectory(evidenceFolder);
            session.Reset(PrototypeExpeditionRegionCatalog.CreateRuntimeSeed());
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetO4CampVerticalSystems();
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            ClearO6CampPopupResult();
            renderedPhase = (GamePhase)(-1);
            RefreshAll();
            EnsureO7SurvivalGuidanceUi();
            o7InitialGuideDismissed = true;
            if (o7SurvivalHelpPanel != null)
            {
                o7SurvivalHelpPanel.SetActive(false);
            }

            RefreshCampInteractionSelection();
            bool hiddenHotspotsRemoved = campInteractionTargets.All(target =>
                target.Kind != PrototypeCampInteractionTargetKind.ModuleExpansionSlot);
            bool markerVisible = worldRoot != null && worldRoot.GetComponentsInChildren<Transform>(true).Any(value =>
                value.name.Contains("현장형 창고·증축 계획 지점"));

            PrototypeCampInteractionTarget planningTarget = new PrototypeCampInteractionTarget(
                PrototypeCampModuleCatalog.VisiblePlanningPointId,
                PrototypeCampInteractionTargetKind.StoragePlanning,
                new Vector2(StoragePlanningX, PrototypeCampUse.PlayerFloorY));
            if (!TryOpenO6VerificationPopup(planningTarget))
            {
                detail = "Visible planning marker popup could not be opened.";
                return false;
            }
            RefreshO7SurvivalGuidanceUi();

            int woodBefore = session.GetStorage(ResourceKind.Wood);
            int salvageBefore = session.GetStorage(ResourceKind.Salvage);
            workbenchButton.onClick.Invoke();
            RefreshO7CampPopupResultBanner();
            Canvas.ForceUpdateCanvases();
            bool failedBuildStayedOpen = campInteraction.IsPopupOpen && !campPlacement.IsActive;
            bool failureChargedNothing = session.GetStorage(ResourceKind.Wood) == woodBefore &&
                                        session.GetStorage(ResourceKind.Salvage) == salvageBefore;
            bool koReadable = IsO7CampPopupResultBannerReadable();
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                CaptureVerificationPng(Path.Combine(evidenceFolder, "o7-camp-result-ko-1280x800.png"), 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            RefreshO7CampPopupResultBanner();
            Canvas.ForceUpdateCanvases();
            bool enReadable = IsO7CampPopupResultBannerReadable();
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                CaptureVerificationPng(Path.Combine(evidenceFolder, "o7-camp-result-en-1280x800.png"), 1280, 800);
            }

            bool qpsSelected = localization.SetQaLocale();
            RefreshAll();
            RefreshO7CampPopupResultBanner();
            Canvas.ForceUpdateCanvases();
            bool qpsReadable = qpsSelected && IsO7CampPopupResultBannerReadable();
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                CaptureVerificationPng(Path.Combine(evidenceFolder, "o7-camp-result-qps-long-1280x800.png"), 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            CancelCampPopup();
            session.Grant(ResourceKind.Salvage, 1);
            bool workbenchBuilt = session.TryBuild(StructureKind.Workbench);
            campPlacement.EnsureInstalled(StructureKind.Workbench);
            Vector2 workbenchPosition = O4WorldRoomPosition(
                PrototypeCampModuleCatalog.StartRoomId,
                campPlacement.GetInstalledPosition(StructureKind.Workbench));
            PrototypeCampInteractionTarget workbenchTarget = new PrototypeCampInteractionTarget(
                "camp.Workbench",
                PrototypeCampInteractionTargetKind.Workbench,
                workbenchPosition,
                true,
                InstalledFacilityInteractionPriority);
            bool workbenchPopupOpened = workbenchBuilt && TryOpenO6VerificationPopup(workbenchTarget);
            int stoneBeforeResearchFailure = session.GetStorage(ResourceKind.Stone);
            int salvageBeforeResearchFailure = session.GetStorage(ResourceKind.Salvage);
            researchAxeButton.onClick.Invoke();
            RefreshO7CampPopupResultBanner();
            Canvas.ForceUpdateCanvases();
            bool researchFailureStayedOpen = workbenchPopupOpened && campInteraction.IsPopupOpen &&
                                             !session.HasResearched(TechKind.StoneAxe) &&
                                             !o7CampPopupResultSucceeded &&
                                             IsO7CampPopupResultBannerReadable() &&
                                             session.GetStorage(ResourceKind.Stone) == stoneBeforeResearchFailure &&
                                             session.GetStorage(ResourceKind.Salvage) == salvageBeforeResearchFailure;
            session.Grant(ResourceKind.Salvage, 1);
            researchAxeButton.onClick.Invoke();
            RefreshO7CampPopupResultBanner();
            Canvas.ForceUpdateCanvases();
            bool researchSuccessStayedOpen = campInteraction.IsPopupOpen &&
                                             session.HasResearched(TechKind.StoneAxe) &&
                                             o7CampPopupResultSucceeded &&
                                             IsO7CampPopupResultBannerReadable();

            bool passed = hiddenHotspotsRemoved && markerVisible && failedBuildStayedOpen &&
                          failureChargedNothing && koReadable && enReadable && qpsReadable &&
                          researchFailureStayedOpen && researchSuccessStayedOpen;
            detail = (passed ? "PASS" : "FAIL") +
                     " hiddenHotspotsRemoved=" + hiddenHotspotsRemoved +
                     " markerVisible=" + markerVisible +
                     " buildFailureStayedOpen=" + failedBuildStayedOpen +
                     " noDuplicateCharge=" + failureChargedNothing +
                     " ko=" + koReadable +
                     " en=" + enReadable +
                     " qps-long=" + qpsReadable +
                     " researchFailureStayedOpen=" + researchFailureStayedOpen +
                     " researchSuccessStayedOpen=" + researchSuccessStayedOpen;
            return passed;
        }
    }
}
