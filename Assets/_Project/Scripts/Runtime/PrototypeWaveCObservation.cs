using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeWaveCStateFingerprint
    {
        public string Fingerprint = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeWaveCProductionEvent
    {
        public int Sequence = -1;
        public string EventType = string.Empty;
        public string StableEventId = string.Empty;
        public string EscapeId = string.Empty;
        public string TargetId = string.Empty;
        public string ActionId = string.Empty;
        public string ResultCode = string.Empty;
        public string Source = "production-live";
        public PrototypeWaveCStateFingerprint StateBefore = new PrototypeWaveCStateFingerprint();
        public PrototypeWaveCStateFingerprint StateAfter = new PrototypeWaveCStateFingerprint();
        public int CostDelta;
        public int InventoryDelta;
        public int HealthDelta;
        public int ProjectProgressDelta;
        public int CompletedStageDelta;
        public int EndingDelta;
        public int AlbumDelta;
        public int AlbumRecordDelta;
    }

    [Serializable]
    public sealed class PrototypeWaveCComicLayoutObservation
    {
        public string Locale = string.Empty;
        public string Screenshot = string.Empty;
        public string RenderedTextFingerprint = string.Empty;
        public string StateFingerprint = string.Empty;
        public int CorePanelCount;
        public int ModifierPanelCount;
        public int OverflowCount;
        public int OffscreenCount;
        public int ClippedRequiredActionCount;
    }

    [Serializable]
    public sealed class PrototypeGameJamTerminalControlObservation
    {
        public string[] ActionIds = Array.Empty<string>();
        public string[] LocalizedLabels = Array.Empty<string>();
        public int SortingOrder;
        public bool ActiveAboveComic;
        public bool MouseRaycastReady;
        public bool ExplicitNavigationReady;
        public bool KeyboardSubmitObserved;
        public bool GamepadSubmitObserved;
        public bool BackTransitionObserved;
        public bool RestartTransitionObserved;
    }

    [Serializable]
    public sealed class PrototypeWaveCRouteBranchObservation
    {
        public string EscapeId = string.Empty;
        public string CompositeSaveFingerprint = string.Empty;
        public string RestoredStartFingerprint = string.Empty;
        public string TerminalStateFingerprint = string.Empty;
        public string CompletedEscapeId = string.Empty;
        public string TerminalEndingId = string.Empty;
        public bool TerminalReached;
        public PrototypeWaveCProductionEvent[] BranchEvents = Array.Empty<PrototypeWaveCProductionEvent>();
    }

    [Serializable]
    public sealed class PrototypeWaveCPlayObservation
    {
        public string EvidenceSource = "production-live input, scene objects, ledgers, snapshots, and rendered UI";
        public string ObservationError = string.Empty;
        public string[] ProtectedPartIds = Array.Empty<string>();
        public string[] ProtectedAssignmentPairs = Array.Empty<string>();
        public string[] EligibleAssignmentPairs = Array.Empty<string>();
        public int[] PityEligibleCountSequence = Array.Empty<int>();
        public string KnownLootBeforeFingerprint = string.Empty;
        public string KnownLootAfterFingerprint = string.Empty;
        public string ProtectedBeforeFingerprint = string.Empty;
        public string ProtectedAfterFingerprint = string.Empty;
        public string[] CompletableEscapeIds = Array.Empty<string>();
        public PrototypeWaveCProductionEvent[] ProductionEvents = Array.Empty<PrototypeWaveCProductionEvent>();
        public PrototypeWaveCRouteBranchObservation[] RouteBranches =
            Array.Empty<PrototypeWaveCRouteBranchObservation>();
        public string[] CommittedRoomIds = Array.Empty<string>();
        public string[] ReenteredRoomIds = Array.Empty<string>();
        public string[] FacilityPlacementRoomIds = Array.Empty<string>();
        public string[] FacilityUseRoomIds = Array.Empty<string>();
        public string[] StableResourceStockLocales = Array.Empty<string>();
        public string[] EscapeShortageLocales = Array.Empty<string>();
        public bool LegacyRescueSignalAvailable;
        public PrototypeGameJamTerminalControlObservation TerminalControls =
            new PrototypeGameJamTerminalControlObservation();
        public string EscapeResourcesBeforeFingerprint = string.Empty;
        public string EscapeResourcesAfterFingerprint = string.Empty;
        public string SaveBeforeFingerprint = string.Empty;
        public string SaveAfterFingerprint = string.Empty;
        public PrototypeWaveCComicLayoutObservation[] Layouts = Array.Empty<PrototypeWaveCComicLayoutObservation>();
        public int GrantCallCount;
        public int WarpCallCount;
        public int SkipCallCount;
        public int RepresentativeSeed;
        public float SyntheticMinutes;
        public string ProfileResult = string.Empty;
        public int HumanSessionCount;
        public string HumanGateStatus = "HUMAN_REQUIRED";
    }

    [Serializable]
    public sealed class PrototypeWaveCAtomicSnapshot
    {
        public PrototypeEscapeProjectState[] Projects = Array.Empty<PrototypeEscapeProjectState>();
        public int FailedResultCount;
        public int WaitResultCount;
        public int RetryResultCount;
        public int EndingCommitCount;
        public int EndingAlbumRecordCount;
    }

    internal sealed class PrototypeWaveCTransactionState
    {
        public string Fingerprint = string.Empty;
        public int ResourceUnits;
        public int Health;
        public Dictionary<string, int> ProjectProgressByEscapeId = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> CompletedStagesByEscapeId = new Dictionary<string, int>(StringComparer.Ordinal);
        public int EndingCount;
        public int AlbumCount;
        public int AlbumRecordCount;
    }

    internal static class PrototypeWaveCObservationRecorder
    {
        public static PrototypeWaveCTransactionState Capture(
            GameSession session,
            PrototypeWaveRuntime waveRuntime,
            PrototypeSearchNodeRuntime searchRuntime,
            PrototypeEndingAlbumCollection album)
        {
            string[] stableResourceIds = PrototypeSearchRegionCatalog.Nodes
                .SelectMany(node => node.FiniteYield ?? Array.Empty<PrototypeSearchLootEntry>())
                .Select(value => value.StableResourceId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            int resourceUnits = session == null
                ? 0
                : stableResourceIds.Sum(session.GetStableStorage) +
                  Enumerable.Range(0, session.ActiveBagSlotCount).Sum(index => session.GetBagSlot(index).Amount);
            string stableState = CaptureCanonicalSessionState(session);
            string waveState = CaptureCanonicalWaveState(waveRuntime);
            string searchState = searchRuntime == null ? string.Empty : JsonUtility.ToJson(searchRuntime.Ledger.CaptureSnapshot());
            string albumState = album == null ? string.Empty : JsonUtility.ToJson(album.CaptureSnapshot());
            PrototypeEscapeProjectState[] projectStates = waveRuntime == null
                ? Array.Empty<PrototypeEscapeProjectState>()
                : waveRuntime.EscapeDirector.States.ToArray();
            PrototypeWaveCAtomicSnapshot atomicSnapshot = waveRuntime == null
                ? new PrototypeWaveCAtomicSnapshot()
                : waveRuntime.CaptureWaveCFailCancelWaitRetryEndingAlbumSnapshot();
            return new PrototypeWaveCTransactionState
            {
                Fingerprint = Hash128.Compute(stableState + "|" + waveState + "|" + searchState + "|" + albumState).ToString(),
                ResourceUnits = resourceUnits,
                Health = session == null ? 0 : session.Health,
                ProjectProgressByEscapeId = projectStates.ToDictionary(
                    value => value.StableId,
                    value => Math.Max(0, value.Progress),
                    StringComparer.Ordinal),
                CompletedStagesByEscapeId = projectStates.ToDictionary(
                    value => value.StableId,
                    value => (value.CompletedStageIds ?? Array.Empty<string>()).Length,
                    StringComparer.Ordinal),
                EndingCount = waveRuntime == null || string.IsNullOrEmpty(waveRuntime.CurrentEndingStableId) ? 0 : 1,
                AlbumCount = album == null ? 0 : album.UnlockedCount,
                AlbumRecordCount = atomicSnapshot.EndingAlbumRecordCount
            };
        }

        private static string CaptureCanonicalSessionState(GameSession session)
        {
            if (session == null) return string.Empty;
            string structures = string.Join(",", Enum.GetValues(typeof(StructureKind)).Cast<StructureKind>()
                .Select(value => value + "=" + session.HasStructure(value)));
            string research = string.Join(",", Enum.GetValues(typeof(TechKind)).Cast<TechKind>()
                .Select(value => value + "=" + session.HasResearched(value) + "/" + session.HasCrafted(value)));
            return JsonUtility.ToJson(session.CaptureStableState()) +
                   "|day=" + session.Day +
                   "|hunger=" + session.Hunger.ToString("R") +
                   "|energy=" + session.Energy.ToString("R") +
                   "|daylight=" + session.Daylight.ToString("R") +
                   "|phase=" + session.Phase +
                   "|result=" + session.Result +
                   "|expedition=" + session.ExpeditionCompleted +
                   "|swimming=" + session.IsSwimming +
                   "|signal=" + session.SignalStage +
                   "|escape=" + (session.CompletedEscapeId ?? string.Empty) +
                   "|structures=" + structures +
                   "|research=" + research;
        }

        private static string CaptureCanonicalWaveState(PrototypeWaveRuntime waveRuntime)
        {
            if (waveRuntime == null) return string.Empty;
            PrototypeRunSnapshot run = waveRuntime.CaptureRunSnapshot();
            PrototypeEscapeProjectSaveSnapshot root = run.escape_project_snapshot ??
                                                      new PrototypeEscapeProjectSaveSnapshot();
            string projects = string.Join(";", (root.Projects ?? Array.Empty<PrototypeEscapeProjectState>())
                .OrderBy(value => value.StableId, StringComparer.Ordinal)
                .Select(value => string.Join(",", new[]
                {
                    value.StableId ?? string.Empty,
                    value.Progress.ToString(),
                    value.RequiredProgress.ToString(),
                    value.Complete.ToString(),
                    string.Join("+", value.CompletedStageIds ?? Array.Empty<string>()),
                    value.KeyPartProtected.ToString(),
                    value.LaunchState ?? string.Empty,
                    value.LaunchAttemptCount.ToString(),
                    value.LastLaunchDay.ToString(),
                    value.LastWeatherId ?? string.Empty,
                    value.LastCurrentId ?? string.Empty
                })));
            string pity = string.Join(";", (run.protected_part_pity ?? Array.Empty<PrototypeProtectedPartPitySnapshot>())
                .OrderBy(value => value.PartId, StringComparer.Ordinal)
                .Select(value => JsonUtility.ToJson(value)));
            string behavior = string.Join(";", (run.behavior_scores ?? Array.Empty<PrototypeBehaviorScore>())
                .OrderBy(value => value.StableId, StringComparer.Ordinal)
                .Select(value => value.StableId + "=" + value.Value));
            return string.Join("|", new[]
            {
                "seed=" + run.seed,
                "day=" + run.day,
                "pacing=" + (run.pacing_band_id ?? string.Empty),
                "region=" + (run.region_id ?? string.Empty),
                "forecast=" + (run.forecast_id ?? string.Empty),
                "hazards=" + string.Join(",", run.hazard_ids ?? Array.Empty<string>()),
                "parts=" + string.Join(",", run.key_part_state_ids ?? Array.Empty<string>()),
                "pity=" + pity,
                "behavior=" + behavior,
                "escape=" + (run.escape_id ?? string.Empty),
                "ending=" + (run.ending_id ?? string.Empty),
                "result=" + (run.result_code ?? string.Empty),
                "projects=" + projects,
                "committed=" + string.Join(",", root.CommittedEventKeys ?? Array.Empty<string>())
            });
        }

        public static PrototypeWaveCProductionEvent Event(
            int sequence,
            string stableEventId,
            string escapeId,
            string targetId,
            string actionId,
            string resultCode,
            PrototypeWaveCTransactionState before,
            PrototypeWaveCTransactionState after)
        {
            before = before ?? new PrototypeWaveCTransactionState();
            after = after ?? new PrototypeWaveCTransactionState();
            return new PrototypeWaveCProductionEvent
            {
                Sequence = sequence,
                EventType = stableEventId ?? string.Empty,
                StableEventId = stableEventId ?? string.Empty,
                EscapeId = escapeId ?? string.Empty,
                TargetId = targetId ?? string.Empty,
                ActionId = actionId ?? string.Empty,
                ResultCode = resultCode ?? string.Empty,
                StateBefore = new PrototypeWaveCStateFingerprint { Fingerprint = before.Fingerprint },
                StateAfter = new PrototypeWaveCStateFingerprint { Fingerprint = after.Fingerprint },
                CostDelta = Math.Max(0, before.ResourceUnits - after.ResourceUnits),
                InventoryDelta = after.ResourceUnits - before.ResourceUnits,
                HealthDelta = after.Health - before.Health,
                ProjectProgressDelta = Metric(after.ProjectProgressByEscapeId, escapeId) -
                                       Metric(before.ProjectProgressByEscapeId, escapeId),
                CompletedStageDelta = Metric(after.CompletedStagesByEscapeId, escapeId) -
                                      Metric(before.CompletedStagesByEscapeId, escapeId),
                EndingDelta = after.EndingCount - before.EndingCount,
                AlbumDelta = after.AlbumCount - before.AlbumCount,
                AlbumRecordDelta = after.AlbumRecordCount - before.AlbumRecordCount
            };
        }

        private static int Metric(IDictionary<string, int> values, string stableId)
        {
            return values != null && !string.IsNullOrWhiteSpace(stableId) && values.TryGetValue(stableId, out int value)
                ? value
                : 0;
        }
    }
}
