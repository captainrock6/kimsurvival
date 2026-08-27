using System;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private PrototypeLocalizedText o6CampPopupResult = PrototypeLocalizedText.Empty;

        private void ExecuteConfirmedPlacementTransition(string actionName, StructureKind kind)
        {
            if (!campInteraction.TryConfirmAction())
            {
                return;
            }

            PrototypeCampInteractionTargetKind targetKind = campInteraction.OpenPopupKind;
            string targetId = campInteraction.OpenPopupTargetId;
            campFeedback = PrototypeLocalizedText.Empty;
            ClearO6CampPopupResult();
            bool began = playtestLog != null
                ? playtestLog.TrackFacilityAction(
                    targetKind,
                    targetId,
                    actionName,
                    delegate
                    {
                        BeginCampPlacement(kind);
                        return campPlacement.IsActive;
                    })
                : TryBeginO6CampPlacement(kind);

            if (began)
            {
                campInteraction.ClosePopup();
                if (playtestLog != null)
                {
                    playtestLog.RecordPopupClosed(targetKind, targetId, "placement_started");
                }
            }
            else
            {
                CaptureO6CampPopupResult(false);
                campInteraction.PrepareOpenPopupForReturn();
                if (playtestLog != null)
                {
                    playtestLog.ObserveState("camp.popup.result.rejected." + actionName);
                }
            }

            RefreshAll();
        }

        private bool TryBeginO6CampPlacement(StructureKind kind)
        {
            BeginCampPlacement(kind);
            return campPlacement.IsActive;
        }

        private void ClearO6CampPopupResult()
        {
            o6CampPopupResult = PrototypeLocalizedText.Empty;
            o7CampPopupResultSucceeded = false;
            if (o7CampResultBanner != null)
            {
                o7CampResultBanner.SetActive(false);
            }
        }

        private void CaptureO6CampPopupResult(bool succeeded)
        {
            o7CampPopupResultSucceeded = succeeded;
            if (!campFeedback.IsEmpty)
            {
                o6CampPopupResult = campFeedback;
                return;
            }

            if (session != null && !session.LastMessage.IsEmpty)
            {
                o6CampPopupResult = session.LastMessage;
                return;
            }

            o6CampPopupResult = new PrototypeLocalizedText(
                succeeded ? "camp.popup.result.success" : "camp.popup.result.failure");
        }

        private string AppendO6CampPopupResult(string baseDetail)
        {
            if (o6CampPopupResult.IsEmpty)
            {
                return baseDetail;
            }

            return baseDetail + "\n" + localization.Format(
                "camp.popup.result",
                localization.Format(o6CampPopupResult));
        }

        private string FormatO6FurniturePlacementButton(StructureKind kind)
        {
            string buildKey = kind == StructureKind.Bed ? "button.bed.build" : "button.sofa.build";
            string relocateKey = kind == StructureKind.Bed ? "button.bed.relocate" : "button.sofa.relocate";
            if (!session.HasStructure(kind))
            {
                return localization.Format(buildKey);
            }

            if (campPlacement.IsInstalledInRoom(kind, campUse.CurrentRoomId))
            {
                return localization.Format(relocateKey);
            }

            return localization.Format(
                "button.furniture.move_here",
                localization.Format(PrototypeCampPlacement.GetStructureId(kind)));
        }

        private CampModuleArchetype FirstO6UncommittedModuleArchetype()
        {
            CampModuleArchetype[] discoveryOrder =
            {
                CampModuleArchetype.Upper,
                CampModuleArchetype.Basement,
                CampModuleArchetype.Side
            };
            for (int index = 0; index < discoveryOrder.Length; index += 1)
            {
                if (!campModuleExpansion.IsCommitted(discoveryOrder[index]))
                {
                    return discoveryOrder[index];
                }
            }
            return CampModuleArchetype.Upper;
        }

        private static Vector2 GetO6ModulePreviewSize(CampModuleArchetype archetype)
        {
            return archetype == CampModuleArchetype.Side
                ? new Vector2(4.1f, 4.25f)
                : new Vector2(8.8f, 2.75f);
        }

        private void CreateO6VerticalAccessPreview(
            Transform previewRoot,
            CampModuleArchetype archetype,
            Vector2 roomCenter,
            bool valid)
        {
            CampVerticalLadderDefinition ladder = PrototypeCampVerticalLayout.Ladder(archetype);
            float height = ladder.UpperFloorY - ladder.LowerFloorY;
            Vector2 worldCenter = new Vector2(ladder.X, (ladder.LowerFloorY + ladder.UpperFloorY) * 0.5f);
            Vector2 localCenter = worldCenter - roomCenter;
            Color color = valid
                ? new Color(1f, 0.84f, 0.28f, 0.78f)
                : new Color(1f, 0.32f, 0.2f, 0.78f);
            CreateRect(previewRoot, "증축 출입 연결부 미리보기 · 왼쪽 난간", localCenter + new Vector2(-0.28f, 0f), new Vector2(0.12f, height + 0.34f), color, 11);
            CreateRect(previewRoot, "증축 출입 연결부 미리보기 · 오른쪽 난간", localCenter + new Vector2(0.28f, 0f), new Vector2(0.12f, height + 0.34f), color, 11);
            int rungCount = Mathf.Max(4, Mathf.CeilToInt(height / 0.42f));
            for (int index = 0; index <= rungCount; index += 1)
            {
                float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, index / (float)rungCount);
                CreateRect(
                    previewRoot,
                    "증축 출입 연결부 미리보기 · 발판 " + index,
                    localCenter + new Vector2(0f, y),
                    new Vector2(0.66f, 0.09f),
                    color,
                    12);
            }
            CreateCircle(previewRoot, "증축 출입 연결부 미리보기 · 시작점", localCenter + new Vector2(0f, -height * 0.5f), 0.28f, color, 13);
            CreateCircle(previewRoot, "증축 출입 연결부 미리보기 · 도착점", localCenter + new Vector2(0f, height * 0.5f), 0.28f, color, 13);
        }

        public static bool RunO6CampDomainContracts(out string detail)
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
            CampModuleReturnSnapshot returnSnapshot = new CampModuleReturnSnapshot(
                Vector2.zero,
                1f,
                PrototypeCampModuleCatalog.StartRoomId);
            if (!expansion.BeginPreview(returnSnapshot, CampModuleArchetype.Upper) ||
                expansion.TryCommit(session, new CampModuleValidationContext()) != CampModuleCommitStatus.Succeeded ||
                !expansion.BeginPreview(returnSnapshot, CampModuleArchetype.Basement) ||
                expansion.TryCommit(session, new CampModuleValidationContext()) != CampModuleCommitStatus.Succeeded)
            {
                detail = "Planning-point sequential upper/basement construction contract failed.";
                return false;
            }

            if (!session.TryBuild(StructureKind.Bed) || !session.TryBuild(StructureKind.Sofa))
            {
                detail = "Furniture fixtures could not be built.";
                return false;
            }

            var placement = new PrototypeCampPlacement();
            if (!PrototypeCampPlacement.TryGetRoomZone(
                    PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).RoomId,
                    out CampPlacementRoomZone upperZone) ||
                !PrototypeCampPlacement.TryGetRoomZone(
                    PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement).RoomId,
                    out CampPlacementRoomZone basementZone))
            {
                detail = "Expanded-room placement zones were unavailable.";
                return false;
            }

            placement.Begin(StructureKind.Bed, true, upperZone);
            bool bedMoved = placement.CurrentValidity == CampPlacementValidity.Valid && placement.Commit() &&
                            placement.IsInstalledInRoom(StructureKind.Bed, upperZone.RoomId);
            placement.Begin(StructureKind.Sofa, true, basementZone);
            bool sofaMoved = placement.CurrentValidity == CampPlacementValidity.Valid && placement.Commit() &&
                             placement.IsInstalledInRoom(StructureKind.Sofa, basementZone.RoomId);
            if (!bedMoved || !sofaMoved)
            {
                detail = "Bed/sofa expanded-room fixed-anchor relocation failed.";
                return false;
            }

            detail = "PASS sequential upper+basement planning and bed/sofa relocation to committed-room fixed anchors.";
            return true;
        }

        public bool CaptureO6CampModalObservation(string absolutePngPath, out string detail)
        {
            if (string.IsNullOrWhiteSpace(absolutePngPath))
            {
                detail = "Screenshot path is empty.";
                return false;
            }

            session.Reset(PrototypeExpeditionRegionCatalog.CreateRuntimeSeed());
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetO4CampVerticalSystems();
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            renderedPhase = (GamePhase)(-1);
            RefreshAll();

            PrototypeCampInteractionTarget planningTarget = new PrototypeCampInteractionTarget(
                "storage.planning",
                PrototypeCampInteractionTargetKind.StoragePlanning,
                new Vector2(StoragePlanningX, PrototypeCampUse.PlayerFloorY));
            if (!TryOpenO6VerificationPopup(planningTarget))
            {
                detail = "Planning popup could not be opened.";
                return false;
            }
            workbenchButton.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            bool buildPopupOpen = campInteraction.IsPopupOpen &&
                                  campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.StoragePlanning;
            bool buildPlacementInactive = !campPlacement.IsActive;
            bool buildMessageVisible = campPopupDetailText.text.Contains(localization.Format("message.build.materials"));
            bool buildFailureStayedOpen = buildPopupOpen && buildPlacementInactive && buildMessageVisible;

            CancelCampPopup();
            session.Grant(ResourceKind.Salvage, 1);
            if (!session.TryBuild(StructureKind.Workbench))
            {
                detail = "Workbench research fixture could not be built.";
                return false;
            }
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
            if (!TryOpenO6VerificationPopup(workbenchTarget))
            {
                detail = "Workbench popup could not be opened.";
                return false;
            }

            researchAxeButton.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            bool researchFailurePopupOpen = campInteraction.IsPopupOpen;
            bool researchFailureMessageVisible = campPopupDetailText.text.Contains(
                localization.Format("message.research.unavailable"));
            bool researchFailureStayedOpen = researchFailurePopupOpen &&
                                             !session.HasResearched(TechKind.StoneAxe) &&
                                             researchFailureMessageVisible;
            session.Grant(ResourceKind.Salvage, 1);
            researchAxeButton.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            bool researchSuccessPopupOpen = campInteraction.IsPopupOpen;
            bool researchSuccessMessageVisible = campPopupDetailText.text.Contains(localization.Format("message.research.axe"));
            bool researchSuccessStayedOpen = researchSuccessPopupOpen &&
                                             session.HasResearched(TechKind.StoneAxe) &&
                                             researchSuccessMessageVisible;

            CancelCampPopup();
            session.Grant(ResourceKind.Wood, 4);
            CampModuleReturnSnapshot returnSnapshot = new CampModuleReturnSnapshot(
                campUse.PlayerPosition,
                campUse.FacingDirection,
                PrototypeCampModuleCatalog.StartRoomId);
            if (!campModuleExpansion.BeginPreview(returnSnapshot, CampModuleArchetype.Upper) ||
                campModuleExpansion.TryCommit(session, campModuleValidation) != CampModuleCommitStatus.Succeeded)
            {
                detail = "Upper-room fixture could not be committed.";
                return false;
            }

            renderedPhase = (GamePhase)(-1);
            RefreshAll();
            if (!TryOpenO6VerificationPopup(planningTarget))
            {
                detail = "Planning popup could not be reopened after upper-room construction.";
                return false;
            }
            string modulePreviewLabel = modulePreviewButton.GetComponentInChildren<TMPro.TMP_Text>().text;
            bool planningButtonVisible = modulePreviewButton.gameObject.activeSelf;
            bool planningButtonEnabled = modulePreviewButton.interactable;
            bool planningStillOffersExpansion = planningButtonVisible &&
                                                planningButtonEnabled &&
                                                modulePreviewLabel.Contains("지하실");
            modulePreviewButton.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            bool basementPreview = campModuleExpansion.IsPreviewActive &&
                                   campModuleExpansion.SelectedArchetype == CampModuleArchetype.Basement &&
                                   modulePreviewGhost != null &&
                                   modulePreviewGhost.GetComponentsInChildren<Transform>(true)
                                       .Any(value => value.name.Contains("출입 연결부 미리보기"));

            Canvas.ForceUpdateCanvases();
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                CaptureVerificationPng(absolutePngPath, 1280, 800);
            }
            bool passed = buildFailureStayedOpen && researchFailureStayedOpen &&
                          researchSuccessStayedOpen && planningStillOffersExpansion && basementPreview;
            detail = (passed ? "PASS" : "FAIL") +
                     " buildFailurePopup=" + buildFailureStayedOpen +
                     " researchFailurePopup=" + researchFailureStayedOpen +
                     " researchSuccessPopup=" + researchSuccessStayedOpen +
                     " planningAfterUpper=" + planningStillOffersExpansion +
                     " basementAccessPreview=" + basementPreview +
                     (passed ? string.Empty :
                         "\nbuild(open=" + buildPopupOpen +
                         ",inactive=" + buildPlacementInactive +
                         ",message=" + buildMessageVisible + ")" +
                         " researchFail(open=" + researchFailurePopupOpen +
                         ",message=" + researchFailureMessageVisible + ")" +
                         " researchSuccess(open=" + researchSuccessPopupOpen +
                         ",researched=" + session.HasResearched(TechKind.StoneAxe) +
                         ",message=" + researchSuccessMessageVisible + ")" +
                         " planning(visible=" + planningButtonVisible +
                         ",enabled=" + planningButtonEnabled +
                         ",label=" + modulePreviewLabel.Replace('\n', ' ') + ")");
            return passed;
        }

        private bool TryOpenO6VerificationPopup(PrototypeCampInteractionTarget target)
        {
            campInteraction.Reset();
            campUse.EnterRoom(
                PrototypeCampModuleCatalog.StartRoomId,
                target.Position.x);
            campInteraction.UpdateSelection(
                target.Position,
                campUse.FacingDirection,
                new[] { target });
            return TryOpenCampPopup();
        }
    }
}
