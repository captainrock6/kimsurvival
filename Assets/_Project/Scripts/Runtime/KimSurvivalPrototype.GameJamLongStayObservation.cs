using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeGameJamLongStayBranchObservation
    {
        public string Kind = string.Empty;
        public string EvidenceSource = "production-live raw-input settlement branch";
        public string EndingId = string.Empty;
        public string RepeatedEndingId = string.Empty;
        public string EscapeId = string.Empty;
        public string Reason = string.Empty;
        public string InteractionTrace = string.Empty;
        public string SnapshotFingerprint = string.Empty;
        public string RepeatedSnapshotFingerprint = string.Empty;
        public int Day;
        public bool Terminal;
        public int EndingRecordCount;
        public int AlbumRecordCount;
        public int CommitCount;
        public int DuplicateAttemptCount;
        public int DuplicateEndingDelta;
        public int DuplicateAlbumDelta;
        public bool ExactlyOnce;
    }

    [Serializable]
    public sealed class PrototypeGameJamLongStayLayoutObservation
    {
        public string EndingId = string.Empty;
        public string Locale = string.Empty;
        public string Screenshot = string.Empty;
        public string RenderedTextFingerprint = string.Empty;
        public string StateFingerprint = string.Empty;
        public int Width = 1280;
        public int Height = 800;
        public int CorePanelCount;
        public int ModifierPanelCount;
        public int OverflowCount;
        public int OffscreenCount;
        public int ClippedRequiredActionCount;
    }

    [Serializable]
    public sealed class PrototypeGameJamLongStayEndingObservation
    {
        public string EvidenceSource = "production-live raw-input Game Jam long-stay observation";
        public string ObservationError = string.Empty;
        public int CatalogCount;
        public string[] CatalogIds = Array.Empty<string>();
        public int Day20Threshold;
        public int StandardFinalDay;
        public PrototypeGameJamLongStayBranchObservation Natural = new PrototypeGameJamLongStayBranchObservation();
        public PrototypeGameJamLongStayBranchObservation Engineer = new PrototypeGameJamLongStayBranchObservation();
        public PrototypeGameJamLongStayBranchObservation EarlyEscape = new PrototypeGameJamLongStayBranchObservation();
        public PrototypeGameJamLongStayBranchObservation Day50 = new PrototypeGameJamLongStayBranchObservation();
        public PrototypeGameJamLongStayBranchObservation[] Branches = Array.Empty<PrototypeGameJamLongStayBranchObservation>();
        public PrototypeGameJamLongStayLayoutObservation[] Layouts = Array.Empty<PrototypeGameJamLongStayLayoutObservation>();
        public int GrantCallCount;
        public int WarpCallCount;
        public int SkipCallCount;
    }

    public sealed partial class KimSurvivalPrototype
    {
        private static readonly string[] GameJamNaturalSearchNodeIds =
        {
            "node.coast.beach.drift-pile.01",
            "node.coast.beach.tree-hollow.01",
            "node.coast.beach.grass-patch.01"
        };

        public PrototypeGameJamLongStayEndingObservation CaptureGameJamLongStayEndingObservation()
        {
            var result = new PrototypeGameJamLongStayEndingObservation
            {
                CatalogCount = PrototypeEndingCatalog.All.Count,
                CatalogIds = PrototypeEndingCatalog.All.Select(value => value.StableId).ToArray(),
                Day20Threshold = PrototypeEndingResolver.GameJamSettlementDay,
                StandardFinalDay = GameSession.FinalDay
            };
            var layouts = new List<PrototypeGameJamLongStayLayoutObservation>();
            string originalLocale = localization == null
                ? PrototypeLocalization.KoreanLocaleCode
                : localization.CurrentLocaleCode;

            try
            {
                string runId = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                Require(!string.IsNullOrWhiteSpace(runId),
                    "KIM_PARALLEL_QA_RUN_ID is required for the destructive production long-stay observation.");
                PrototypeProductionActionCounters.Reset();

                result.Natural = CaptureProductionSettlementBranch(
                    "natural",
                    2020,
                    PrototypeSessionFlowProfileCatalog.GameJamProvisionalProfileId,
                    GameJamNaturalSearchNodeIds.Length,
                    true);
                layouts.AddRange(CaptureCurrentLongStayLayouts(
                    result.Natural.EndingId,
                    LongStayEvidenceFolder(runId, "natural")));

                result.Engineer = CaptureProductionSettlementBranch(
                    "engineer",
                    2021,
                    PrototypeSessionFlowProfileCatalog.GameJamProvisionalProfileId,
                    0,
                    true);
                layouts.AddRange(CaptureCurrentLongStayLayouts(
                    result.Engineer.EndingId,
                    LongStayEvidenceFolder(runId, "engineer")));

                PrototypeWaveCPlayObservation earlyRoute = CaptureWaveCPlayObservation();
                Require(string.IsNullOrWhiteSpace(earlyRoute.ObservationError),
                    "Wave C production early-escape route failed: " + earlyRoute.ObservationError);
                result.EarlyEscape = CaptureCurrentSettlementBranch(
                    "early-escape",
                    "production-live map > finite searches > camp projects > escape terminal",
                    false);

                result.Day50 = CaptureProductionSettlementBranch(
                    "day50",
                    2050,
                    PrototypeSessionFlowProfileCatalog.StandardProfileId,
                    0,
                    false);

                result.Branches = new[] { result.Natural, result.Engineer, result.EarlyEscape, result.Day50 };
                result.Layouts = layouts.ToArray();
                result.GrantCallCount = PrototypeProductionActionCounters.GrantCallCount;
                result.WarpCallCount = PrototypeProductionActionCounters.WarpCallCount;
                result.SkipCallCount = PrototypeProductionActionCounters.SkipCallCount;
                Require(result.GrantCallCount == 0 && result.WarpCallCount == 0 && result.SkipCallCount == 0,
                    "Production long-stay observation used a forbidden grant, warp, or skip path.");
            }
            catch (Exception exception)
            {
                result.ObservationError = exception.GetType().Name + ": " + exception.Message;
                result.Branches = new[] { result.Natural, result.Engineer, result.EarlyEscape, result.Day50 };
                result.Layouts = layouts.ToArray();
                result.GrantCallCount = PrototypeProductionActionCounters.GrantCallCount;
                result.WarpCallCount = PrototypeProductionActionCounters.WarpCallCount;
                result.SkipCallCount = PrototypeProductionActionCounters.SkipCallCount;
            }
            finally
            {
                if (localization != null && !string.IsNullOrWhiteSpace(originalLocale))
                {
                    if (string.Equals(originalLocale, PrototypeLocalization.QpsLongLocaleCode, StringComparison.Ordinal))
                    {
                        localization.SetQaLocale(originalLocale);
                    }
                    else
                    {
                        localization.SetLocale(originalLocale, false);
                    }
                }
            }

            return result;
        }

        private PrototypeGameJamLongStayBranchObservation CaptureProductionSettlementBranch(
            string kind,
            int seed,
            string sessionProfileId,
            int naturalSearchCount,
            bool duplicateTerminalInput)
        {
            ResetLongStayProductionRun(seed, sessionProfileId);
            BuildCampfireThroughProductionInput();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            int searchCount = 0;
            int safety = GameSession.FinalDay + 2;

            while (session.Result == RunResult.None && safety-- > 0)
            {
                if (searchCount < naturalSearchCount)
                {
                    PrototypeSearchNodeDefinition node = PrototypeSearchRegionCatalog.Nodes.First(value =>
                        string.Equals(value.NodeId, GameJamNaturalSearchNodeIds[searchCount], StringComparison.Ordinal));
                    SearchWaveCNodeAndReturn(node, visited, ref searchCount);
                }
                else
                {
                    BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId.Beach);
                    Require(ReturnToCampThroughRawInput(), kind + " production map departure and return");
                }

                PrepareCampfireThroughProductionInput();
                phaseButton.onClick.Invoke();
            }

            Require(safety >= 0 && session.Result == RunResult.Deadline,
                kind + " branch did not reach its production settlement deadline.");
            string trace = "production-live campfire placement > " + searchCount +
                           " finite search interactions > map departure/return per day > campfire preparation > phase settlement";
            return CaptureCurrentSettlementBranch(kind, trace, duplicateTerminalInput);
        }

        private void ResetLongStayProductionRun(int seed, string sessionProfileId)
        {
            session.Reset(seed, sessionProfileId);
            searchNodeRuntime.Reset(seed);
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            expeditionMapSelection.Close();
            endingAlbumSelection.Close();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            hazardEscapeEndingRuntime.ResetRuntime();
            RefreshAll();
        }

        private void BuildCampfireThroughProductionInput()
        {
            OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.StoragePlanning);
            ActuateCampPopupButtonThroughRawInput(
                campfireButton,
                new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 });
            Require(campPlacement.IsActive && campPlacement.CurrentValidity == CampPlacementValidity.Valid,
                "production campfire placement start");
            ProcessCampPlacementActions(
                PrototypeCampPlacementActions.FromRaw(
                    new PrototypeRawCampPlacementInput { KeyboardConfirm = true }),
                0f);
            Require(session.HasStructure(StructureKind.Campfire), "production campfire placement commit");
        }

        private void PrepareCampfireThroughProductionInput()
        {
            OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Campfire);
            ActuateCampPopupButtonThroughRawInput(
                prepareCampfireButton,
                new PrototypeRawInput { GamepadInteract = true, BagSlotIndex = -1 });
            Require(campUse.IsDayBenefitPrepared(StructureKind.Campfire),
                "production campfire day benefit preparation");
        }

        private PrototypeGameJamLongStayBranchObservation CaptureCurrentSettlementBranch(
            string kind,
            string interactionTrace,
            bool duplicateTerminalInput)
        {
            PrototypeRunSnapshot firstSnapshot = hazardEscapeEndingRuntime.CaptureRunSnapshot();
            PrototypeRunSnapshot repeatedSnapshot = hazardEscapeEndingRuntime.CaptureRunSnapshot();
            PrototypeEndingResolution firstResolution = PrototypeEndingResolver.ResolveEndingDeterministicSingle(firstSnapshot);
            PrototypeEndingResolution repeatedResolution = PrototypeEndingResolver.ResolveEndingDeterministicSingle(repeatedSnapshot);
            PrototypeTerminalEndingObservation beforeDuplicate = hazardEscapeEndingRuntime.CaptureTerminalEndingObservation();
            if (duplicateTerminalInput)
            {
                phaseButton.onClick.Invoke();
            }
            PrototypeTerminalEndingObservation afterDuplicate = hazardEscapeEndingRuntime.CaptureTerminalEndingObservation();
            string firstFingerprint = Hash128.Compute(JsonUtility.ToJson(firstSnapshot)).ToString();
            string repeatedFingerprint = Hash128.Compute(JsonUtility.ToJson(repeatedSnapshot)).ToString();

            return new PrototypeGameJamLongStayBranchObservation
            {
                Kind = kind,
                EndingId = firstResolution.StableId,
                RepeatedEndingId = repeatedResolution.StableId,
                EscapeId = firstSnapshot.escape_id,
                Reason = firstResolution.DeterministicSingleReason,
                InteractionTrace = interactionTrace,
                SnapshotFingerprint = firstFingerprint,
                RepeatedSnapshotFingerprint = repeatedFingerprint,
                Day = firstSnapshot.day,
                Terminal = afterDuplicate.Terminal,
                EndingRecordCount = afterDuplicate.EndingRecordCount,
                AlbumRecordCount = afterDuplicate.AlbumRecordCount,
                CommitCount = afterDuplicate.CommitCount,
                DuplicateAttemptCount = afterDuplicate.DuplicateAttemptCount,
                DuplicateEndingDelta = afterDuplicate.EndingRecordCount - beforeDuplicate.EndingRecordCount,
                DuplicateAlbumDelta = afterDuplicate.AlbumRecordCount - beforeDuplicate.AlbumRecordCount,
                ExactlyOnce = afterDuplicate.ExactlyOnce
            };
        }

        private PrototypeGameJamLongStayLayoutObservation[] CaptureCurrentLongStayLayouts(
            string endingId,
            string destination)
        {
            return hazardEscapeEndingRuntime.CaptureWaveCComicLayoutObservations(destination)
                .Select(value => new PrototypeGameJamLongStayLayoutObservation
                {
                    EndingId = endingId,
                    Locale = value.Locale,
                    Screenshot = value.Screenshot,
                    RenderedTextFingerprint = value.RenderedTextFingerprint,
                    StateFingerprint = value.StateFingerprint,
                    Width = 1280,
                    Height = 800,
                    CorePanelCount = value.CorePanelCount,
                    ModifierPanelCount = value.ModifierPanelCount,
                    OverflowCount = value.OverflowCount,
                    OffscreenCount = value.OffscreenCount,
                    ClippedRequiredActionCount = value.ClippedRequiredActionCount
                })
                .ToArray();
        }

        private static string LongStayEvidenceFolder(string runId, string branchId)
        {
            string safeRun = new string((runId ?? "long-stay")
                .Where(value => char.IsLetterOrDigit(value) || value == '-' || value == '_')
                .ToArray());
            return Path.Combine(
                Application.temporaryCachePath,
                "kim-survival-long-stay",
                string.IsNullOrEmpty(safeRun) ? "long-stay" : safeRun,
                branchId);
        }
    }
}
