using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        public PrototypeWaveCPlayObservation CaptureWaveCPlayObservation()
        {
            var observation = new PrototypeWaveCPlayObservation();
            var events = new List<PrototypeWaveCProductionEvent>();
            var pitySequence = new List<int>();
            var reenteredRooms = new List<string>();
            var visitedNodeIds = new HashSet<string>(StringComparer.Ordinal);
            int expeditionCount = 0;
            int eventSequence = 0;
            bool raftFailureRecorded = false;
            string originalLocale = localization == null ? PrototypeLocalization.KoreanLocaleCode : localization.CurrentLocaleCode;

            try
            {
                string runId = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                Require(!string.IsNullOrWhiteSpace(runId),
                    "KIM_PARALLEL_QA_RUN_ID is required for destructive Wave C production observation.");

                int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
                PrototypeProductionActionCounters.Reset();
                session.Reset(seed);
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

                PrototypeProtectedPartAssignmentSnapshot[] assignments =
                    PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                        seed,
                        PrototypeSearchRegionCatalog.ContractRevision);
                observation.RepresentativeSeed = seed;
                observation.ProtectedAssignmentPairs = assignments
                    .OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => value.PartId + "=" + value.AssignedNodeId)
                    .ToArray();
                observation.EligibleAssignmentPairs = assignments
                    .Where(value => PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(value.PartId)
                        .Contains(value.AssignedNodeId))
                    .OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => value.PartId + "=" + value.AssignedNodeId)
                    .ToArray();

                RecordWaveCRouteFailureAndCancel(
                    "escape.smoke",
                    PrototypeCampInteractionTargetKind.SmokeBeacon,
                    smokeProjectButton,
                    "facility.smoke-beacon",
                    "ignition-fail",
                    events,
                    ref eventSequence);
                RecordWaveCRouteFailureAndCancel(
                    "escape.radio",
                    PrototypeCampInteractionTargetKind.RadioBench,
                    radioProjectButton,
                    "facility.radio-bench",
                    "repair-fail",
                    events,
                    ref eventSequence);

                string[] starterNodeIds =
                {
                    "node.sea.shallows.grass-patch.01",
                    "node.coast.beach.drift-pile.01",
                    "node.coast.beach.rock-crevice.01",
                    "node.coast.beach.rock-crevice.02"
                };
                PrototypeSearchNodeDefinition[] easyNodes = starterNodeIds
                    .Select(nodeId => PrototypeSearchRegionCatalog.Nodes.First(value => value.NodeId == nodeId))
                    .ToArray();
                for (int index = 0; index < easyNodes.Length && (!session.HasAxe || !session.HasRope); index += 1)
                {
                    SearchWaveCNodeAndReturn(easyNodes[index], visitedNodeIds, ref expeditionCount);
                    TryPrepareWaveCToolsThroughProductionInput();
                    if (!raftFailureRecorded && hazardEscapeEndingRuntime.IsRaftShoreLaunchDiscovered)
                    {
                        RecordWaveCRouteFailureAndCancel(
                            "escape.raft",
                            PrototypeCampInteractionTargetKind.ShoreLaunch,
                            raftProjectButton,
                            "facility.shore-launch",
                            "shore-resource-fail",
                            events,
                            ref eventSequence);
                        raftFailureRecorded = true;
                    }
                    RecordWaveCWaitForecasts(events, ref eventSequence);
                    AdvanceWaveCProductionDay();
                }
                Require(session.HasStructure(StructureKind.Workbench) && session.HasAxe && session.HasRope,
                    "Wave C production starter searches must build the workbench, stone axe, and rope.");
                Require(raftFailureRecorded,
                    "The production starter route must expose the shore launcher and record an atomic raft failure/cancel cycle.");

                PrototypeProtectedPartAssignmentSnapshot pityAssignment = assignments
                    .Where(value => !searchNodeRuntime.Ledger.HasProtectedPart(value.PartId))
                    .OrderBy(value => value.PartId == PrototypeSearchNodeLootResolver.FlintPartId ? 0 : 1)
                    .ThenBy(value => value.PartId, StringComparer.Ordinal)
                    .First();
                string[] pityEligibleIds = PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(pityAssignment.PartId)
                    .Where(value => !string.Equals(value, pityAssignment.AssignedNodeId, StringComparison.Ordinal))
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                foreach (string nodeId in pityEligibleIds)
                {
                    bool completedNow = false;
                    if (!visitedNodeIds.Contains(nodeId))
                    {
                        SearchWaveCNodeAndReturn(
                            PrototypeSearchRegionCatalog.Nodes.First(value => value.NodeId == nodeId),
                            visitedNodeIds,
                            ref expeditionCount);
                        completedNow = true;
                    }
                    PrototypeProtectedPartPitySnapshot pity = searchNodeRuntime.Ledger.ProtectedPartPity
                        .First(value => value.PartId == pityAssignment.PartId);
                    pitySequence.Add(pity.EligibleMissCount);
                    RecordWaveCWaitForecasts(events, ref eventSequence);
                    if (completedNow) AdvanceWaveCProductionDay();
                }
                PrototypeProtectedPartPitySnapshot armedPity = searchNodeRuntime.Ledger.ProtectedPartPity
                    .First(value => value.PartId == pityAssignment.PartId);
                Require(armedPity.HintRevealed && armedPity.GuaranteeArmed && armedPity.EligibleMissCount == 5,
                    "Natural protected-part pity must reveal at three and arm at five unique eligible completions.");

                SearchWaveCNodeAndReturn(
                    PrototypeSearchRegionCatalog.Nodes.First(value => value.NodeId == pityAssignment.AssignedNodeId),
                    visitedNodeIds,
                    ref expeditionCount);
                RecordWaveCWaitForecasts(events, ref eventSequence);
                AdvanceWaveCProductionDay();

                foreach (PrototypeProtectedPartAssignmentSnapshot assignment in assignments
                             .OrderBy(value => value.PartId, StringComparer.Ordinal))
                {
                    if (searchNodeRuntime.Ledger.HasProtectedPart(assignment.PartId)) continue;
                    SearchWaveCNodeAndReturn(
                        PrototypeSearchRegionCatalog.Nodes.First(value => value.NodeId == assignment.AssignedNodeId),
                        visitedNodeIds,
                        ref expeditionCount);
                    RecordWaveCWaitForecasts(events, ref eventSequence);
                    AdvanceWaveCProductionDay();
                }
                Require(PrototypeSearchNodeLootResolver.ProtectedPartIds.All(searchNodeRuntime.Ledger.HasProtectedPart),
                    "All five protected parts must be acquired through live searchable nodes.");
                observation.ProtectedPartIds = searchNodeRuntime.Ledger.CaptureSnapshot().ProtectedPartIds;
                observation.PityEligibleCountSequence = pitySequence.Distinct().OrderBy(value => value).ToArray();

                CommitWaveCCampModule(CampModuleArchetype.Upper);
                CommitWaveCCampModule(CampModuleArchetype.Basement);
                string[] committedRoomIds = GetCommittedCampRoomIds();
                Require(committedRoomIds.Any(value => value.IndexOf("upper", StringComparison.OrdinalIgnoreCase) >= 0) &&
                        committedRoomIds.Any(value => value.IndexOf("basement", StringComparison.OrdinalIgnoreCase) >= 0),
                    "Upper and basement must both be committed in the same production run.");
                TraverseWaveCCampRoom(CampModuleArchetype.Upper, reenteredRooms);
                TraverseWaveCCampRoom(CampModuleArchetype.Basement, reenteredRooms);

                while (expeditionCount < 14 && !CanCompleteAllWaveCRoutes())
                {
                    PrototypeSearchNodeDefinition next = SelectWaveCResourceNode(visitedNodeIds, seed);
                    Require(next != null, "Finite Wave C resource stock exhausted before three routes became completable.");
                    SearchWaveCNodeAndReturn(next, visitedNodeIds, ref expeditionCount);
                    RecordWaveCWaitForecasts(events, ref eventSequence);
                    if (!CanCompleteAllWaveCRoutes()) AdvanceWaveCProductionDay();
                }
                Require(CanCompleteAllWaveCRoutes(),
                    "Representative seed must expose raft, smoke, and radio as simultaneously completable in at most 14 searches. " +
                    DescribeWaveCRouteBudget(expeditionCount));
                string[] representativeEscapeIds = { "escape.raft", "escape.smoke", "escape.radio" };
                observation.CompletableEscapeIds = representativeEscapeIds
                    .Where(CanCompleteWaveCRoute)
                    .ToArray();
                Require(observation.CompletableEscapeIds.Length == representativeEscapeIds.Length,
                    "Completable escape IDs must be derived from live route predicates, not a fixed pass list.");

                string knownBefore = searchNodeRuntime.Ledger.NewGameStockFingerprint;
                string protectedBefore = WaveCProtectedFingerprint();
                string resourceBefore = WaveCEscapeResourceFingerprint();
                string saveJson = CaptureWaveCSaveJson();
                string saveBefore = CaptureWaveCSaveFingerprint();
                PrototypeWaveCTransactionState saveStateBefore = CaptureWaveCTransactionState();
                Require(TryRestoreWaveCSaveJson(saveJson), "Composite Wave C save restore must succeed atomically.");
                string saveAfter = CaptureWaveCSaveFingerprint();
                string resourceAfterRestore = WaveCEscapeResourceFingerprint();
                PrototypeWaveCTransactionState saveStateAfter = CaptureWaveCTransactionState();
                events.Add(PrototypeWaveCObservationRecorder.Event(
                    eventSequence++,
                    "camp.snapshot.restored",
                    string.Empty,
                    "wave-c.composite-save-root",
                    "save-restore",
                    "snapshot-restored",
                    saveStateBefore,
                    saveStateAfter));
                TraverseWaveCCampRoom(CampModuleArchetype.Upper, reenteredRooms);
                TraverseWaveCCampRoom(CampModuleArchetype.Basement, reenteredRooms);
                observation.KnownLootBeforeFingerprint = knownBefore;
                observation.KnownLootAfterFingerprint = searchNodeRuntime.Ledger.NewGameStockFingerprint;
                observation.ProtectedBeforeFingerprint = protectedBefore;
                observation.ProtectedAfterFingerprint = WaveCProtectedFingerprint();
                observation.CommittedRoomIds = committedRoomIds;
                observation.ReenteredRoomIds = reenteredRooms.Distinct(StringComparer.Ordinal).ToArray();
                observation.EscapeResourcesBeforeFingerprint = resourceBefore;
                observation.EscapeResourcesAfterFingerprint = resourceAfterRestore;
                observation.SaveBeforeFingerprint = saveBefore;
                observation.SaveAfterFingerprint = saveAfter;

                EnsureWaveCWaitEvents(events, ref eventSequence, visitedNodeIds, ref expeditionCount, seed);
                RecordWaveCRouteProgress(
                    "escape.smoke", PrototypeCampInteractionTargetKind.SmokeBeacon, smokeProjectButton,
                    "facility.smoke-beacon", "retry-ignition", events, ref eventSequence);
                RecordWaveCRouteProgress(
                    "escape.radio", PrototypeCampInteractionTargetKind.RadioBench, radioProjectButton,
                    "facility.radio-bench", "retry-repair", events, ref eventSequence);
                RecordWaveCRouteProgress(
                    "escape.raft", PrototypeCampInteractionTargetKind.ShoreLaunch, raftProjectButton,
                    "facility.shore-launch", "shore-retry-hull", events, ref eventSequence);

                if (!PrototypeSignalEscapeWindowResolver.Resolve("escape.smoke", session.RunSeed, session.Day).Allowed)
                {
                    Require(session.ExpeditionCompleted, "A completed search day is required before the smoke visibility retry.");
                    AdvanceWaveCProductionDay();
                }
                PrototypeWaveCTransactionState endingBefore = CaptureWaveCTransactionState();
                RecordWaveCRouteProgress(
                    "escape.smoke", PrototypeCampInteractionTargetKind.SmokeBeacon, smokeProjectButton,
                    "facility.smoke-beacon", "visibility-retry", events, ref eventSequence,
                    "ending.resolved");
                PrototypeWaveCTransactionState endingAfter = CaptureWaveCTransactionState();
                Require(session.Result == RunResult.Rescued && session.CompletedEscapeId == "escape.smoke",
                    "The production trace must finish exactly one smoke ending.");
                events.Add(PrototypeWaveCObservationRecorder.Event(
                    eventSequence++,
                    "ending.album.unlocked",
                    "escape.smoke",
                    "ui.ending-album",
                    "album-record",
                    "album-unlocked-once",
                    endingBefore,
                    endingAfter));
                PrototypeWaveCTransactionState duplicateBefore = CaptureWaveCTransactionState();
                smokeProjectButton.onClick.Invoke();
                PrototypeWaveCTransactionState duplicateAfter = CaptureWaveCTransactionState();
                events.Add(PrototypeWaveCObservationRecorder.Event(
                    eventSequence++,
                    "ending.terminal.duplicate",
                    "escape.smoke",
                    "facility.smoke-beacon",
                    "terminal-control-reactuated",
                    "duplicate-terminal-noop",
                    duplicateBefore,
                    duplicateAfter));

                string evidenceFolder = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "Artifacts",
                    "ParallelQA",
                    runId));
                Directory.CreateDirectory(evidenceFolder);
                observation.Layouts = hazardEscapeEndingRuntime.CaptureWaveCComicLayoutObservations(evidenceFolder);
                observation.KnownLootBeforeFingerprint = knownBefore;
                observation.KnownLootAfterFingerprint = searchNodeRuntime.Ledger.NewGameStockFingerprint;
                observation.ProtectedBeforeFingerprint = protectedBefore;
                observation.ProtectedAfterFingerprint = WaveCProtectedFingerprint();
                observation.CommittedRoomIds = committedRoomIds;
                observation.ReenteredRoomIds = reenteredRooms.Distinct(StringComparer.Ordinal).ToArray();
                observation.EscapeResourcesBeforeFingerprint = resourceBefore;
                observation.EscapeResourcesAfterFingerprint = resourceAfterRestore;
                observation.SaveBeforeFingerprint = saveBefore;
                observation.SaveAfterFingerprint = saveAfter;
                observation.ProductionEvents = events.ToArray();
                observation.GrantCallCount = PrototypeProductionActionCounters.GrantCallCount;
                observation.WarpCallCount = PrototypeProductionActionCounters.WarpCallCount;
                observation.SkipCallCount = PrototypeProductionActionCounters.SkipCallCount;
                observation.SyntheticMinutes = 3f + expeditionCount * 1.65f + events.Count * 0.18f;
                observation.ProfileResult = observation.SyntheticMinutes >= 25f && observation.SyntheticMinutes <= 35f &&
                                            observation.GrantCallCount == 0 && observation.WarpCallCount == 0 &&
                                            observation.SkipCallCount == 0 ? "PASS" : "FAIL";
                observation.HumanSessionCount = 0;
                observation.HumanGateStatus = "HUMAN_REQUIRED";
            }
            catch (Exception exception)
            {
                observation.ObservationError = exception.GetType().Name + ": " + exception.Message;
                observation.ProductionEvents = events.ToArray();
                observation.GrantCallCount = PrototypeProductionActionCounters.GrantCallCount;
                observation.WarpCallCount = PrototypeProductionActionCounters.WarpCallCount;
                observation.SkipCallCount = PrototypeProductionActionCounters.SkipCallCount;
            }
            finally
            {
                if (localization != null && !string.IsNullOrWhiteSpace(originalLocale))
                {
                    localization.SetLocale(originalLocale, false);
                }
            }
            return observation;
        }

        private PrototypeWaveCTransactionState CaptureWaveCTransactionState()
        {
            return PrototypeWaveCObservationRecorder.Capture(
                session,
                hazardEscapeEndingRuntime,
                searchNodeRuntime,
                endingAlbumCollection);
        }

        private void RecordWaveCRouteFailureAndCancel(
            string escapeId,
            PrototypeCampInteractionTargetKind kind,
            Button button,
            string targetId,
            string failureAction,
            ICollection<PrototypeWaveCProductionEvent> events,
            ref int sequence)
        {
            PrototypeWaveCTransactionState before = CaptureWaveCTransactionState();
            OpenCampTargetThroughProductionInput(kind);
            ActuateCampPopupButtonThroughRawInput(button, new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 });
            if (campInteraction.IsPopupOpen)
            {
                ProcessCampActions(playerInput.MapRawActions(new PrototypeRawInput { KeyboardCancel = true, BagSlotIndex = -1 }), 0f);
            }
            PrototypeWaveCTransactionState after = CaptureWaveCTransactionState();
            PrototypeEscapeProjectState state = hazardEscapeEndingRuntime.EscapeDirector.GetState(escapeId);
            Require(state.Progress == 0 && before.Fingerprint == after.Fingerprint,
                escapeId + " rejected production action must preserve the canonical transaction fingerprint.");
            events.Add(PrototypeWaveCObservationRecorder.Event(
                sequence++, "escape.interaction.failed", escapeId, targetId, failureAction,
                state.LastResultCode + ".fail", before, after));

            before = CaptureWaveCTransactionState();
            OpenCampTargetThroughProductionInput(kind);
            ProcessCampActions(playerInput.MapRawActions(new PrototypeRawInput { KeyboardCancel = true, BagSlotIndex = -1 }), 0f);
            after = CaptureWaveCTransactionState();
            events.Add(PrototypeWaveCObservationRecorder.Event(
                sequence++, "escape.interaction.cancelled", escapeId, targetId, "popup-cancel",
                "cancelled", before, after));
        }

        private void RecordWaveCRouteProgress(
            string escapeId,
            PrototypeCampInteractionTargetKind kind,
            Button button,
            string targetId,
            string actionId,
            ICollection<PrototypeWaveCProductionEvent> events,
            ref int sequence,
            string stableEventId = "escape.interaction.progressed")
        {
            PrototypeWaveCTransactionState before = CaptureWaveCTransactionState();
            OpenCampTargetThroughProductionInput(kind);
            ActuateCampPopupButtonThroughRawInput(button, new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 });
            PrototypeWaveCTransactionState after = CaptureWaveCTransactionState();
            PrototypeEscapeProjectState state = hazardEscapeEndingRuntime.EscapeDirector.GetState(escapeId);
            events.Add(PrototypeWaveCObservationRecorder.Event(
                sequence++, stableEventId, escapeId, targetId, actionId, state.LastResultCode, before, after));
        }

        private void RecordWaveCWaitForecasts(
            ICollection<PrototypeWaveCProductionEvent> events,
            ref int sequence)
        {
            string[] already = events.Where(value => value.ResultCode.IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(value => value.EscapeId).ToArray();
            foreach (string route in new[] { "escape.smoke", "escape.radio" })
            {
                if (already.Contains(route)) continue;
                PrototypeSignalEscapeWindow window = PrototypeSignalEscapeWindowResolver.Resolve(route, session.RunSeed, session.Day);
                if (window.Allowed) continue;
                PrototypeWaveCTransactionState state = CaptureWaveCTransactionState();
                events.Add(PrototypeWaveCObservationRecorder.Event(
                    sequence++, "escape.forecast.wait", route,
                    route == "escape.smoke" ? "facility.smoke-beacon" : "facility.radio-bench",
                    route == "escape.smoke" ? "visibility-wait" : "frequency-wait",
                    window.ResultCode,
                    state,
                    state));
            }
            if (!already.Contains("escape.raft"))
            {
                PrototypeRaftLaunchWindow raft = hazardEscapeEndingRuntime.CurrentRaftLaunchWindow;
                if (!raft.Allowed)
                {
                    PrototypeWaveCTransactionState state = CaptureWaveCTransactionState();
                    events.Add(PrototypeWaveCObservationRecorder.Event(
                        sequence++, "escape.forecast.wait", "escape.raft", "facility.shore-launch",
                        "shore-weather-wait", raft.ResultCode + ".wait", state, state));
                }
            }
        }

        private void EnsureWaveCWaitEvents(
            ICollection<PrototypeWaveCProductionEvent> events,
            ref int sequence,
            ISet<string> visitedNodeIds,
            ref int expeditionCount,
            int seed)
        {
            for (int guard = 0; guard < 3; guard += 1)
            {
                RecordWaveCWaitForecasts(events, ref sequence);
                bool complete = new[] { "escape.raft", "escape.smoke", "escape.radio" }.All(route =>
                    events.Any(value => value.EscapeId == route &&
                                        value.ResultCode.IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0));
                if (complete) return;
                if (!session.ExpeditionCompleted)
                {
                    PrototypeSearchNodeDefinition next = SelectWaveCResourceNode(visitedNodeIds, seed);
                    Require(next != null, "A finite search node is required to advance the production forecast day.");
                    SearchWaveCNodeAndReturn(next, visitedNodeIds, ref expeditionCount);
                }
                AdvanceWaveCProductionDay();
            }
            Require(false, "All three escape routes must expose a real zero-delta wait forecast within three days.");
        }

        private void SearchWaveCNodeAndReturn(
            PrototypeSearchNodeDefinition definition,
            ISet<string> visitedNodeIds,
            ref int expeditionCount)
        {
            Require(definition != null && !visitedNodeIds.Contains(definition.NodeId),
                "Wave C search node must be finite and unvisited.");
            BeginExpeditionThroughProductionMap(PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId));
            SearchAndTakeAllNodeThroughProductionInput(definition);
            Require(searchNodeRuntime.Ledger.GetOrCreate(definition).State == PrototypeSearchNodeState.Depleted,
                definition.NodeId + " must be depleted by the production loot tray.");
            Require(ReturnToCampThroughRawInput(), definition.NodeId + " production return");
            visitedNodeIds.Add(definition.NodeId);
            expeditionCount += 1;
        }

        private void AdvanceWaveCProductionDay()
        {
            int day = session.Day;
            Require(session.ExpeditionCompleted, "A completed expedition is required before production day settlement.");
            phaseButton.onClick.Invoke();
            Require(session.Result == RunResult.None && session.Day == day + 1,
                "Wave C production day settlement must preserve the live run.");
        }

        private void TryPrepareWaveCToolsThroughProductionInput()
        {
            if (!session.HasStructure(StructureKind.Workbench) && session.CanBuild(StructureKind.Workbench))
            {
                BuildWorkbenchThroughProductionPopup();
            }
            if (!session.HasStructure(StructureKind.Workbench)) return;
            if (!session.HasResearched(TechKind.StoneAxe) && session.CanResearch(TechKind.StoneAxe))
            {
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ActuateCampPopupButtonThroughRawInput(researchAxeButton,
                    new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 });
            }
            if (!session.HasAxe && session.CanCraft(TechKind.StoneAxe))
            {
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ActuateCampPopupButtonThroughRawInput(craftAxeButton,
                    new PrototypeRawInput { GamepadInteract = true, BagSlotIndex = -1 });
            }
            if (!session.HasResearched(TechKind.Rope) && session.CanResearch(TechKind.Rope))
            {
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ActuateCampPopupButtonThroughRawInput(researchRopeButton,
                    new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 });
            }
            if (!session.HasRope && session.CanCraft(TechKind.Rope))
            {
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ActuateCampPopupButtonThroughRawInput(craftRopeButton,
                    new PrototypeRawInput { GamepadInteract = true, BagSlotIndex = -1 });
            }
        }

        private void CommitWaveCCampModule(CampModuleArchetype archetype)
        {
            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(archetype);
            OpenWaveCCampTargetIdThroughProductionInput(definition.StartSlotId);
            ActuateCampPopupButtonThroughRawInput(modulePreviewButton,
                new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 });
            PrototypeCampModulePreviewActions actions = PrototypeCampModulePreviewActions.FromRaw(
                new PrototypeRawCampModulePreviewInput { KeyboardConfirm = true });
            Require(actions.ConfirmPressed && ConfirmCampModulePreview(), archetype + " production module commit");
        }

        private void TraverseWaveCCampRoom(CampModuleArchetype archetype, ICollection<string> reenteredRooms)
        {
            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(archetype);
            Require(CurrentCampRoomId == PrototypeCampModuleCatalog.StartRoomId,
                "Camp connector traversal must start in the start room.");
            OpenWaveCCampTargetIdThroughProductionInput(definition.StartSlotId);
            Require(CurrentCampRoomId == definition.RoomId, archetype + " production connector entry");
            reenteredRooms.Add(CurrentCampRoomId);
            OpenWaveCCampTargetIdThroughProductionInput(definition.ReciprocalSlotId);
            Require(CurrentCampRoomId == PrototypeCampModuleCatalog.StartRoomId,
                archetype + " production reciprocal connector return");
        }

        private void OpenWaveCCampTargetIdThroughProductionInput(string targetId)
        {
            PrototypeCampInteractionTarget target = campInteractionTargets.First(value =>
                string.Equals(value.Id, targetId, StringComparison.Ordinal));
            const float stepSeconds = 0.02f;
            int safety = 1200;
            while (Mathf.Abs(target.Position.x - campUse.PlayerPosition.x) > 0.05f && safety-- > 0)
            {
                float direction = target.Position.x < campUse.PlayerPosition.x ? -1f : 1f;
                ProcessCampActions(new PrototypePlayerActions(direction, false, false, false, false, -1), stepSeconds);
            }
            Require(Mathf.Abs(target.Position.x - campUse.PlayerPosition.x) <= 0.08f,
                targetId + " natural camp approach");
            ProcessCampActions(new PrototypePlayerActions(0f, false, true, false, false, -1), 0f);
            if (target.Kind == PrototypeCampInteractionTargetKind.ModuleConnector)
            {
                return;
            }
            Require(campInteraction.IsPopupOpen && string.Equals(campInteraction.OpenPopupTargetId, targetId, StringComparison.Ordinal),
                targetId + " production popup open");
        }

        private bool CanCompleteAllWaveCRoutes()
        {
            string[] escapeIds = { "escape.raft", "escape.smoke", "escape.radio" };
            bool simultaneousResources = GetWaveCCombinedRouteCosts()
                .All(group => session.GetStableStorage(group.Key) >= group.Sum(value => value.Amount));
            return simultaneousResources && escapeIds.All(CanCompleteWaveCRoute);
        }

        private bool CanCompleteWaveCRoute(string escapeId)
        {
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
            if (definition.RequiredKeyPartIds.Any(partId => !searchNodeRuntime.Ledger.HasProtectedPart(partId)))
            {
                return false;
            }
            if (string.Equals(escapeId, PrototypeRaftEscapeConfig.EscapeId, StringComparison.Ordinal))
            {
                return session.HasRope &&
                       session.GetStorage(ResourceKind.Wood) >=
                       PrototypeRaftEscapeConfig.HullWoodCost + PrototypeRaftEscapeConfig.SailWoodCost &&
                       session.GetStorage(ResourceKind.Salvage) >=
                       PrototypeRaftEscapeConfig.HullSalvageCost + PrototypeRaftEscapeConfig.SailSalvageCost &&
                       session.GetStorage(ResourceKind.Food) >=
                       PrototypeRaftEscapeConfig.SuppliesFoodCost + PrototypeRaftEscapeConfig.LaunchAttemptFoodCost;
            }
            bool researchReady = string.Equals(escapeId, "escape.smoke", StringComparison.Ordinal)
                ? session.HasRope
                : session.HasAxe;
            return researchReady && definition.StableCosts.All(cost =>
                session.GetStableStorage(cost.StableResourceId) >= cost.Amount);
        }

        private IEnumerable<IGrouping<string, PrototypeStableResourceCost>> GetWaveCCombinedRouteCosts()
        {
            PrototypeStableResourceCost[] raftCosts =
            {
                new PrototypeStableResourceCost(
                    PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(ResourceKind.Wood),
                    PrototypeRaftEscapeConfig.HullWoodCost + PrototypeRaftEscapeConfig.SailWoodCost),
                new PrototypeStableResourceCost(
                    PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(ResourceKind.Salvage),
                    PrototypeRaftEscapeConfig.HullSalvageCost + PrototypeRaftEscapeConfig.SailSalvageCost),
                new PrototypeStableResourceCost(
                    PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(ResourceKind.Food),
                    PrototypeRaftEscapeConfig.SuppliesFoodCost + PrototypeRaftEscapeConfig.LaunchAttemptFoodCost)
            };
            return PrototypeEscapeProjectCatalog.Get("escape.smoke").StableCosts
                .Concat(PrototypeEscapeProjectCatalog.Get("escape.radio").StableCosts)
                .Concat(raftCosts)
                .GroupBy(value => value.StableResourceId, StringComparer.Ordinal);
        }

        private string DescribeWaveCRouteBudget(int expeditionCount)
        {
            var budget = new List<string> { "searches=" + expeditionCount };
            foreach (IGrouping<string, PrototypeStableResourceCost> group in GetWaveCCombinedRouteCosts()
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                budget.Add(group.Key + "=" + session.GetStableStorage(group.Key) + "/" + group.Sum(value => value.Amount));
            }
            budget.Add("axe=" + session.HasAxe);
            budget.Add("rope=" + session.HasRope);
            budget.Add("completable=" + string.Join(",", new[] { "escape.raft", "escape.smoke", "escape.radio" }
                .Where(CanCompleteWaveCRoute).ToArray()));
            budget.Add("missing.parts=" + string.Join(",", PrototypeSearchNodeLootResolver.ProtectedPartIds
                .Where(value => !searchNodeRuntime.Ledger.HasProtectedPart(value)).ToArray()));
            return string.Join("; ", budget.ToArray());
        }

        private PrototypeSearchNodeDefinition SelectWaveCResourceNode(ISet<string> visitedNodeIds, int seed)
        {
            var deficits = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (IGrouping<string, PrototypeStableResourceCost> group in GetWaveCCombinedRouteCosts())
            {
                deficits[group.Key] = group.Sum(value => value.Amount);
            }
            foreach (string key in deficits.Keys.ToArray())
            {
                deficits[key] = Math.Max(0, deficits[key] - session.GetStableStorage(key));
            }
            return PrototypeSearchRegionCatalog.Nodes
                .Where(value => !visitedNodeIds.Contains(value.NodeId))
                .Select(value => new
                {
                    Definition = value,
                    Score = PrototypeSearchNodeLootResolver.Resolve(seed, value)
                        .Where(item => !item.IsProtectedPart && deficits.ContainsKey(item.StableResourceId))
                        .Sum(item => Math.Min(item.Amount, deficits[item.StableResourceId]))
                })
                .OrderByDescending(value => value.Score)
                .ThenBy(value => value.Definition.NodeId, StringComparer.Ordinal)
                .Select(value => value.Definition)
                .FirstOrDefault();
        }

        private string WaveCProtectedFingerprint()
        {
            return Hash128.Compute(string.Join("|", searchNodeRuntime.Ledger.CaptureSnapshot().ProtectedPartIds
                .OrderBy(value => value, StringComparer.Ordinal))).ToString();
        }

        private string WaveCEscapeResourceFingerprint()
        {
            string[] ids = PrototypeEscapeProjectCatalog.All.Where(value => !value.DataOnly)
                .SelectMany(value => value.MaterialIds)
                .Concat(new[]
                {
                    PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(ResourceKind.Salvage),
                    PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(ResourceKind.Food)
                })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            return Hash128.Compute(string.Join("|", ids.Select(id => id + "=" + session.GetStableStorage(id)))).ToString();
        }
    }
}
