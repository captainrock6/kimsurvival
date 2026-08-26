using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParallelQA
{
    /// <summary>
    /// Wave C RED-first gate. Edit checks inspect public structured runtime data.
    /// Play checks accept only a rich observation returned by an active scene
    /// component; fixture probes, static methods, primitive pass flags, grant,
    /// warp, and skip are not acceptance evidence.
    /// </summary>
    public static class GameJamWaveCRedFirstGateRunner
    {
        private const string RedBaseline = "da7919ed7314b97865a7c8cebb738d420cfeb512";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.GameJamWaveC.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.GameJamWaveC.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.GameJamWaveC.PlayMessage";
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private static bool playTickAttached;
        private static double playEarliestRunTime;
        private static double playTimeoutAt;

        private static readonly string[] ProtectedPartIds =
        {
            "part.raft.sailcloth",
            "part.smoke.flint",
            "part.radio.transceiver",
            "part.radio.circuit-board",
            "part.radio.transistor"
        };

        private static readonly string[] CoreEscapeIds =
        {
            "escape.raft", "escape.smoke", "escape.radio"
        };

        private static readonly string[] Locales = { "en", "ko", "qps-long" };

        [Serializable]
        private sealed class Check
        {
            public string id = string.Empty;
            public string matrix = string.Empty;
            public string status = string.Empty;
            public string classification = string.Empty;
            public string severity = string.Empty;
            public string expected = string.Empty;
            public string actual = string.Empty;
            public string reproduction = string.Empty;
            public string recommendedFiles = string.Empty;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion = 1;
            public string title = string.Empty;
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public string unityVersion = string.Empty;
            public string startedUtc = string.Empty;
            public string completedUtc = string.Empty;
            public string overall = string.Empty;
            public string productOverall = string.Empty;
            public string infrastructureOverall = string.Empty;
            public int passed;
            public int expectedGaps;
            public int productFailed;
            public int infrastructureFailed;
            public int humanRequired;
            public string greenCompletionCondition = string.Empty;
            public string physicalGamepad = "HUMAN_REQUIRED";
            public Check[] checks = Array.Empty<Check>();
        }

        [Serializable]
        private sealed class AssignmentEvidence
        {
            public string partId = string.Empty;
            public string nodeId = string.Empty;
            public string regionId = string.Empty;
            public string[] eligibleNodeIds = Array.Empty<string>();
            public bool assignedToEligibleNode;
        }

        [Serializable]
        private sealed class RouteAuditEvidence
        {
            public int seed;
            public int day;
            public string auditPointId = string.Empty;
            public string[] completableEscapeIds = Array.Empty<string>();
        }

        [Serializable]
        private sealed class EditEvidence
        {
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public string discoveryPolicy = string.Empty;
            public AssignmentEvidence[] assignments = Array.Empty<AssignmentEvidence>();
            public int[] pityEligibleCountSequence = Array.Empty<int>();
            public bool cancelledIgnored;
            public bool failedIgnored;
            public bool ineligibleIgnored;
            public bool duplicateIgnored;
            public int hintAt;
            public int guaranteeAt;
            public RouteAuditEvidence[] routeAudits = Array.Empty<RouteAuditEvidence>();
            public string atomicRuntimeSurface = string.Empty;
            public string moduleSaveSurface = string.Empty;
            public string comicSurface = string.Empty;
            public string localizationSurface = string.Empty;
        }

        [Serializable]
        private sealed class EventEvidence
        {
            public int sequence = -1;
            public string eventType = string.Empty;
            public string stableEventId = string.Empty;
            public string escapeId = string.Empty;
            public string targetId = string.Empty;
            public string actionId = string.Empty;
            public string resultCode = string.Empty;
            public string source = string.Empty;
            public string beforeFingerprint = string.Empty;
            public string afterFingerprint = string.Empty;
            public int costDelta = int.MinValue;
            public int inventoryDelta = int.MinValue;
            public int healthDelta = int.MinValue;
            public int projectProgressDelta = int.MinValue;
            public int completedStageDelta = int.MinValue;
            public int endingDelta = int.MinValue;
            public int albumDelta = int.MinValue;
            public int albumRecordDelta = int.MinValue;
        }

        [Serializable]
        private sealed class LayoutEvidence
        {
            public string locale = string.Empty;
            public string screenshot = string.Empty;
            public string renderSha256 = string.Empty;
            public string renderedTextFingerprint = string.Empty;
            public string stateFingerprint = string.Empty;
            public int corePanelCount = -1;
            public int modifierPanelCount = -1;
            public int overflowCount = -1;
            public int offscreenCount = -1;
            public int clippedRequiredActionCount = -1;
            public int activeGeometryTextCount = -1;
            public int textTextOverlapCount = -1;
            public int textCardBoundaryViolationCount = -1;
            public float titleFontSize = -1f;
            public float minimumCoreFontSize = -1f;
            public float modifierFontSize = -1f;
            public string[] geometryViolations = Array.Empty<string>();
        }

        [Serializable]
        private sealed class RouteBranchEvidence
        {
            public string escapeId = string.Empty;
            public string compositeSaveFingerprint = string.Empty;
            public string restoredStartFingerprint = string.Empty;
            public string terminalStateFingerprint = string.Empty;
            public string completedEscapeId = string.Empty;
            public string terminalEndingId = string.Empty;
            public bool terminalReached;
            public EventEvidence[] events = Array.Empty<EventEvidence>();
        }

        [Serializable]
        private sealed class TerminalControlEvidence
        {
            public string[] actionIds = Array.Empty<string>();
            public string[] localizedLabels = Array.Empty<string>();
            public int sortingOrder = -1;
            public bool activeAboveComic;
            public bool mouseRaycastReady;
            public bool explicitNavigationReady;
            public bool keyboardSubmitObserved;
            public bool gamepadSubmitObserved;
            public bool backTransitionObserved;
            public bool restartTransitionObserved;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public string scene = string.Empty;
            public string discoveryPolicy = string.Empty;
            public string liveObservationOwner = string.Empty;
            public string liveObservationMethod = string.Empty;
            public string liveObservationSurface = string.Empty;
            public string observationError = string.Empty;
            public string evidenceSource = string.Empty;
            public string[] protectedPartIds = Array.Empty<string>();
            public string[] protectedAssignmentPairs = Array.Empty<string>();
            public string[] eligibleAssignmentPairs = Array.Empty<string>();
            public int[] pityEligibleCountSequence = Array.Empty<int>();
            public string knownLootBeforeFingerprint = string.Empty;
            public string knownLootAfterFingerprint = string.Empty;
            public string protectedBeforeFingerprint = string.Empty;
            public string protectedAfterFingerprint = string.Empty;
            public string[] completableEscapeIds = Array.Empty<string>();
            public EventEvidence[] events = Array.Empty<EventEvidence>();
            public RouteBranchEvidence[] routeBranches = Array.Empty<RouteBranchEvidence>();
            public string[] committedRoomIds = Array.Empty<string>();
            public string[] reenteredRoomIds = Array.Empty<string>();
            public string[] facilityPlacementRoomIds = Array.Empty<string>();
            public string[] facilityUseRoomIds = Array.Empty<string>();
            public string[] stableResourceStockLocales = Array.Empty<string>();
            public string[] escapeShortageLocales = Array.Empty<string>();
            public bool legacyRescueSignalAvailable;
            public TerminalControlEvidence terminalControls = new TerminalControlEvidence();
            public string escapeResourcesBeforeFingerprint = string.Empty;
            public string escapeResourcesAfterFingerprint = string.Empty;
            public string saveBeforeFingerprint = string.Empty;
            public string saveAfterFingerprint = string.Empty;
            public LayoutEvidence[] layouts = Array.Empty<LayoutEvidence>();
            public int grantCallCount = -1;
            public int warpCallCount = -1;
            public int skipCallCount = -1;
            public int representativeSeed;
            public float syntheticMinutes = -1f;
            public string profileResult = string.Empty;
            public int humanSessionCount = -1;
            public string humanGateStatus = string.Empty;
        }

        [Serializable]
        private sealed class PriorCheck
        {
            public string id = string.Empty;
            public string status = string.Empty;
        }

        [Serializable]
        private sealed class PriorReport
        {
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public PriorCheck[] checks = Array.Empty<PriorCheck>();
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "wave-c-missing-run-id" : Sanitize(value);
            }
        }

        private static string BaselineCommit
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_BASELINE");
                return string.IsNullOrWhiteSpace(value) ? RedBaseline : value.Trim();
            }
        }

        private static bool IsRedBaseline
        {
            get { return string.Equals(BaselineCommit, RedBaseline, StringComparison.OrdinalIgnoreCase); }
        }

        private static string EvidenceFolder
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts", "ParallelQA", RunId)); }
        }

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<Check> checks = new List<Check>();
            AssignmentEvidence[] assignments = ObserveAssignments();
            int[] pityCounts = ObservePity(out bool cancelledIgnored, out bool failedIgnored,
                out bool ineligibleIgnored, out bool duplicateIgnored, out int hintAt, out int guaranteeAt);
            RouteAuditEvidence[] routeAudits = ObserveRouteAudits();
            Type waveRuntimeType = typeof(PrototypeEndingCatalog).Assembly.GetType("KimSurvival.PrototypeWaveRuntime");
            string atomicSurface = DescribeSurface(waveRuntimeType,
                "Fail", "Cancel", "Wait", "Retry", "Ending", "Album", "Snapshot");
            string moduleSurface = DescribeSurface(typeof(PrototypeCampModuleExpansion),
                "CommittedRooms", "HasUpperAndBasementCommitted", "CaptureSnapshot", "RestoreSnapshot", "TryResolveConnectionDestination");
            string comicSurface = DescribeEndingSurface();
            string localizationSurface = DescribeEndingLocalization(out bool localizationPass);

            Infrastructure(checks, "GWC-I01.exact_baseline", "Wave C runner identity", "P0",
                "A fresh RunId and exact full baseline SHA are recorded",
                delegate
                {
                    Require(RunId != "wave-c-missing-run-id", "KIM_PARALLEL_QA_RUN_ID is missing");
                    Require(BaselineCommit.Length == 40, "baseline is not a full SHA: " + BaselineCommit);
                    return "runId=" + RunId + "; baseline=" + BaselineCommit + "; redBaseline=" + IsRedBaseline;
                },
                "Invoke the Wave C PowerShell entry point with a fresh RunId at the exact requested HEAD.",
                "Assets/Editor/ParallelQA/Invoke-GameJamWaveCRedFirstGate.ps1");

            Product(checks, "GWC-E01.protected_eligible_assignment", "matrix 134 criterion 1", "P0",
                "Sailcloth, flint, and all three radio parts have collision-free deterministic assignments limited to declared eligible nodes",
                delegate
                {
                    Require(assignments.Length == ProtectedPartIds.Length,
                        "protected assignments=" + assignments.Length + "; expected=" + ProtectedPartIds.Length);
                    Require(assignments.Select(value => value.partId).OrderBy(value => value, StringComparer.Ordinal)
                        .SequenceEqual(ProtectedPartIds.OrderBy(value => value, StringComparer.Ordinal)),
                        "protected IDs differ: " + string.Join(",", assignments.Select(value => value.partId).ToArray()));
                    Require(assignments.All(value => value.assignedToEligibleNode && value.eligibleNodeIds.Length > 0),
                        "one or more assignments are outside declared eligible nodes");
                    Require(assignments.Select(value => value.nodeId).Distinct(StringComparer.Ordinal).Count() == assignments.Length,
                        "protected assignments collide on one node");
                    return string.Join(" | ", assignments.Select(value => value.partId + "=" + value.nodeId).ToArray());
                },
                "Resolve protected assignments from the public resolver and compare each node with EligibleNodeIdsFor(partId).",
                "runtime protected-part resolver and catalog");

            Product(checks, "GWC-E02.eligible_completed_miss_pity_3_5", "matrix 135 criterion 2", "P0",
                "Only unique eligible completed misses count; cancelled, failed, ineligible, and duplicate searches do not; hint/guarantee occur at 3/5",
                delegate
                {
                    Require(cancelledIgnored && failedIgnored && ineligibleIgnored && duplicateIgnored,
                        "an excluded result changed the public pity state");
                    Require(pityCounts.SequenceEqual(new[] { 1, 2, 3, 4, 5 }),
                        "eligible count sequence=" + string.Join(",", pityCounts.Select(value => value.ToString()).ToArray()));
                    Require(hintAt == 3 && guaranteeAt == 5, "hint/guarantee=" + hintAt + "/" + guaranteeAt);
                    return "eligible=" + string.Join(",", pityCounts.Select(value => value.ToString()).ToArray()) +
                           "; hint=" + hintAt + "; guarantee=" + guaranteeAt;
                },
                "Drive the public pity state with distinct structured search outcomes; do not call a fixture probe.",
                "runtime protected-part pity state");

            Product(checks, "GWC-E03.three_route_seed_audit", "matrix 136 criterion 3", "P0",
                "Representative seed audits expose raft, smoke, and radio as at least three simultaneously completable routes",
                delegate
                {
                    Require(routeAudits.Length >= 3, "too few route audit points");
                    foreach (RouteAuditEvidence audit in routeAudits)
                    {
                        Require(CoreEscapeIds.All(id => audit.completableEscapeIds.Contains(id)),
                            "seed/day=" + audit.seed + "/" + audit.day + " routes=" + string.Join(",", audit.completableEscapeIds));
                    }
                    return string.Join(" | ", routeAudits.Select(value => value.seed + "/" + value.day + "=" +
                        string.Join(",", value.completableEscapeIds)).ToArray());
                },
                "Call the public route auditor at seed, expansion, and late-run audit points and enumerate returned IDs.",
                "runtime escape route auditor");

            Product(checks, "GWC-E04.atomic_retry_ending_schema", "matrix 138 criterion 5", "P0",
                "Public runtime state exposes fail/cancel/weather-wait/retry and single ending/album persistence data",
                delegate
                {
                    string lower = atomicSurface.ToLowerInvariant();
                    Require(new[] { "fail", "cancel", "wait", "retry", "ending", "album", "snapshot" }
                        .All(lower.Contains), "missing required atomic surface: " + atomicSurface);
                    return atomicSurface;
                },
                "Inspect the public runtime member surface; Play still requires observed zero-delta transactions and once-only records.",
                "runtime escape transaction and ending persistence owners");

            Product(checks, "GWC-E05.upper_basement_save_schema", "matrix 139 criterion 6", "P0",
                "Module expansion publicly stores multiple committed rooms, upper+basement state, re-entry resolution, capture, and restore",
                delegate
                {
                    string lower = moduleSurface.ToLowerInvariant();
                    Require(new[] { "committedrooms", "hasupperandbasementcommitted", "capturesnapshot", "restoresnapshot", "tryresolveconnectiondestination" }
                        .All(lower.Contains), "missing module save/re-entry member: " + moduleSurface);
                    return moduleSurface;
                },
                "Inspect public module snapshot and re-entry members; Play still requires one uninterrupted run and state fingerprints.",
                "runtime camp module expansion owner");

            Product(checks, "GWC-E06.comic_modifier_localization_schema", "matrix 140 criterion 7", "P1",
                "Comic endings expose at least three core panel keys plus an explicit modifier panel surface with KO/EN/qps-long rows",
                delegate
                {
                    Require(localizationPass, localizationSurface);
                    Require(comicSurface.IndexOf("modifier", StringComparison.OrdinalIgnoreCase) >= 0,
                        "no explicit modifier panel member: " + comicSurface);
                    return comicSurface + "; " + localizationSurface;
                },
                "Enumerate public comic definitions and canonical localization rows; static rows do not satisfy the Play rendering gate.",
                "runtime ending catalog and Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            EditEvidence evidence = new EditEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                discoveryPolicy = "Public structured resolver/auditor/snapshot values only. Fixture probe booleans and fixture text are excluded.",
                assignments = assignments,
                pityEligibleCountSequence = pityCounts,
                cancelledIgnored = cancelledIgnored,
                failedIgnored = failedIgnored,
                ineligibleIgnored = ineligibleIgnored,
                duplicateIgnored = duplicateIgnored,
                hintAt = hintAt,
                guaranteeAt = guaranteeAt,
                routeAudits = routeAudits,
                atomicRuntimeSurface = atomicSurface,
                moduleSaveSurface = moduleSurface,
                comicSurface = comicSurface,
                localizationSurface = localizationSurface
            };
            WriteJson("gamejam-wave-c-edit-observation-evidence.json", evidence);
            WriteReport("gamejam-wave-c-edit-contracts", "GameJam Wave C RED-first Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "GameJam Wave C Play runner did not complete");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AttachPlayCallbacks();
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void ResumePlayContracts()
        {
            if (SessionState.GetBool(PlayRunningKey, false)) AttachPlayCallbacks();
        }

        private static void AttachPlayCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (!EditorApplication.isPlaying) return;
            playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
            playTimeoutAt = EditorApplication.timeSinceStartup + 300d;
            if (!playTickAttached)
            {
                EditorApplication.update += PlayTick;
                playTickAttached = true;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 300d;
                if (!playTickAttached)
                {
                    EditorApplication.update += PlayTick;
                    playTickAttached = true;
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishPlayContracts();
            }
        }

        private static void PlayTick()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup > playTimeoutAt)
            {
                WritePlayInfrastructureFailure(new TimeoutException("GameJam Wave C Play runner timed out."));
                StopPlayContracts();
                return;
            }
            if (EditorApplication.timeSinceStartup < playEarliestRunTime) return;
            if (playTickAttached)
            {
                EditorApplication.update -= PlayTick;
                playTickAttached = false;
            }

            try
            {
                DateTime started = DateTime.UtcNow;
                PlayEvidence evidence = ObservePlay();
                List<Check> checks = new List<Check>();

                Product(checks, "GWC-P01.protected_eligible_pity_natural", "matrix 134-135 criteria 1-2", "P0",
                    "A live natural trace proves eligible-only protected discovery, 3/5 eligible-completed pity, protected survival, and known-loot preservation",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(evidence.protectedPartIds.OrderBy(value => value, StringComparer.Ordinal)
                            .SequenceEqual(ProtectedPartIds.OrderBy(value => value, StringComparer.Ordinal)),
                            "protected IDs=" + string.Join(",", evidence.protectedPartIds));
                        Require(evidence.protectedAssignmentPairs.Length == ProtectedPartIds.Length &&
                                evidence.protectedAssignmentPairs.OrderBy(value => value, StringComparer.Ordinal)
                                    .SequenceEqual(evidence.eligibleAssignmentPairs.OrderBy(value => value, StringComparer.Ordinal)),
                            "assigned/eligible pairs differ");
                        Require(evidence.pityEligibleCountSequence.Contains(3) && evidence.pityEligibleCountSequence.Contains(5),
                            "live pity sequence=" + string.Join(",", evidence.pityEligibleCountSequence.Select(value => value.ToString()).ToArray()));
                        Require(SameNonEmpty(evidence.knownLootBeforeFingerprint, evidence.knownLootAfterFingerprint),
                            "known loot was not fingerprint-preserved");
                        Require(SameNonEmpty(evidence.protectedBeforeFingerprint, evidence.protectedAfterFingerprint),
                            "protected inventory did not survive full-bag/theft/damage observations");
                        Require(EventsAreProduction(evidence.events), "events are missing, unordered, or not production-live");
                        return "parts=" + evidence.protectedPartIds.Length + "; events=" + evidence.events.Length;
                    },
                    "Play from a fresh save through eligible and ineligible completed/cancelled/failed searches, bag full, theft, and damage.",
                    "active production search ledger and event recorder observation surface");

                Product(checks, "GWC-P02.three_simultaneous_completable", "matrix 136 criterion 3", "P0",
                    "One live run exposes raft, smoke, and radio as simultaneously completable without injected inventory",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(CoreEscapeIds.All(id => evidence.completableEscapeIds.Contains(id)) &&
                                evidence.completableEscapeIds.Distinct(StringComparer.Ordinal).Count() >= 3,
                            "live completable routes=" + string.Join(",", evidence.completableEscapeIds));
                        Require(!evidence.legacyRescueSignalAvailable,
                            "GAME JAM live camp still exposes the legacy rescue-signal interaction path");
                        Require(evidence.stableResourceStockLocales.Length == 2 &&
                                evidence.escapeShortageLocales.Length == 4 &&
                                evidence.stableResourceStockLocales.All(IsLocalizedResourceEvidence) &&
                                evidence.escapeShortageLocales.All(IsLocalizedShortageEvidence),
                            "KO/EN exact stable stock or smoke/radio shortage evidence is incomplete");
                        Require(ZeroCheatCalls(evidence), DescribeCounters(evidence));
                        return "routes=" + string.Join(",", evidence.completableEscapeIds);
                    },
                    "Reach one natural run state and enumerate the active route runtime's completable IDs.",
                    "active production escape route runtime observation surface");

                Product(checks, "GWC-P03.distinct_natural_escape_interactions", "matrix 137 criterion 4", "P0",
                    "Raft hull+sail+supplies, smoke ignition+visibility, and radio repair+frequency are distinct ordered production interactions with positive route stage/resource deltas",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(evidence.routeBranches.Length == CoreEscapeIds.Length &&
                                evidence.routeBranches.Select(value => value.escapeId)
                                    .Distinct(StringComparer.Ordinal).Count() == CoreEscapeIds.Length,
                            "route branch IDs=" + string.Join(",", evidence.routeBranches.Select(value => value.escapeId).ToArray()));
                        Require(evidence.routeBranches.All(value => !string.IsNullOrWhiteSpace(value.compositeSaveFingerprint)) &&
                                evidence.routeBranches.Select(value => value.compositeSaveFingerprint)
                                    .Distinct(StringComparer.Ordinal).Count() == 1,
                            "all route branches must fork from one common composite save fingerprint");
                        RequireRouteBranch(evidence, "escape.raft", "facility.shore-launch",
                            new[] { "hull", "sail", "supplies" });
                        RequireRouteBranch(evidence, "escape.smoke", "facility.smoke-beacon",
                            new[] { "ignit", "visib" });
                        RequireRouteBranch(evidence, "escape.radio", "facility.radio-bench",
                            new[] { "repair", "frequen" });
                        Require(ZeroCheatCalls(evidence), DescribeCounters(evidence));
                        return "independent production branches=" + evidence.routeBranches.Length +
                               "; events=" + evidence.routeBranches.Sum(value => value.events.Length);
                    },
                    "Use the live shore launcher, smoke signal, and radio controls through their distinct visible actions.",
                    "active production escape interaction owners and event recorder");

                Product(checks, "GWC-P04.atomic_fail_cancel_wait_retry_ending_once", "matrix 138 criterion 5", "P0",
                    "Each route has exact zero-delta fail/cancel/wait and a positive retry/progress delta; terminal duplicate actuation is zero-delta; ending.resolved and the terminal album record each occur once while new/known collection semantics stay distinct",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(EventsAreProduction(evidence.events), "production-live ordered event records are missing");
                        foreach (string escapeId in CoreEscapeIds)
                        {
                            RequireAtomicRouteCycle(evidence.events, escapeId);
                        }
                        RequireTerminalEndingContract(evidence.events);
                        RequireTerminalControls(evidence.terminalControls);
                        return "atomic routes=3; terminal duplicate=zero; ending.resolved=1; albumRecordDelta=1; terminal controls=mouse+keyboard+gamepad";
                    },
                    "For each live route, attempt fail, cancel, weather wait, and positive retry/progress; resolve once, record terminal album new/known semantics, then actuate the terminal control again.",
                    "active production escape transaction and ending album recorders");

                Product(checks, "GWC-P05.same_run_upper_basement_save", "matrix 139 criterion 6", "P0",
                    "The same live run commits and re-enters upper+basement and preserves all three escape resources and save state",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(ContainsUpperAndBasement(evidence.committedRoomIds),
                            "committed rooms=" + string.Join(",", evidence.committedRoomIds));
                        Require(ContainsUpperAndBasement(evidence.reenteredRoomIds),
                            "reentered rooms=" + string.Join(",", evidence.reenteredRoomIds));
                        Require(ContainsUpperAndBasement(evidence.facilityPlacementRoomIds) &&
                                ContainsUpperAndBasement(evidence.facilityUseRoomIds),
                            "production facility placement/use rooms=" +
                            string.Join(",", evidence.facilityPlacementRoomIds) + " / " +
                            string.Join(",", evidence.facilityUseRoomIds));
                        Require(SameNonEmpty(evidence.escapeResourcesBeforeFingerprint, evidence.escapeResourcesAfterFingerprint),
                            "escape resource fingerprint changed");
                        Require(SameNonEmpty(evidence.saveBeforeFingerprint, evidence.saveAfterFingerprint),
                            "save fingerprint changed across restore/re-entry");
                        Require(HasEventToken(evidence.events, "snapshot", "restor") || HasEventToken(evidence.events, "save", "restor"),
                            "actual save/restore event is absent");
                        return "committed=" + string.Join(",", evidence.committedRoomIds) +
                               "; reentered=" + string.Join(",", evidence.reenteredRoomIds);
                    },
                    "In one uninterrupted production run, commit both modules, save/restore, and re-enter each room without helper warps.",
                    "active production module expansion, escape inventory, and save runtime observation surface");

                Product(checks, "GWC-P06.live_core_modifier_comic_locales", "matrix 140 criterion 7", "P1",
                    "KO/EN/qps-long each render at least three live core panels plus one modifier with distinct text/images, identical runtime state, no clipped required action, no rendered text overlap, and every title/body inside its owning card",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(evidence.layouts.Length == 3 && evidence.layouts.Select(value => value.locale)
                            .OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(Locales),
                            "locales=" + string.Join(",", evidence.layouts.Select(value => value.locale).ToArray()));
                        Require(evidence.layouts.All(value => value.corePanelCount >= 3 && value.modifierPanelCount >= 1 &&
                                                              value.overflowCount == 0 && value.offscreenCount == 0 &&
                                                              value.clippedRequiredActionCount == 0 &&
                                                              value.activeGeometryTextCount >= 8 &&
                                                              value.textTextOverlapCount == 0 &&
                                                              value.textCardBoundaryViolationCount == 0 &&
                                                              value.titleFontSize >= 18f &&
                                                              value.minimumCoreFontSize >= 12f &&
                                                              value.modifierFontSize >= 13f),
                            "panel/layout failure: " + string.Join(" | ", evidence.layouts.Select(DescribeLayout).ToArray()));
                        Require(evidence.layouts.All(value => !string.IsNullOrWhiteSpace(value.renderSha256)) &&
                                evidence.layouts.Select(value => value.renderSha256).Distinct(StringComparer.Ordinal).Count() == 3,
                            "three distinct captured image hashes are required");
                        Require(evidence.layouts.All(value => !string.IsNullOrWhiteSpace(value.renderedTextFingerprint)) &&
                                evidence.layouts.Select(value => value.renderedTextFingerprint).Distinct(StringComparer.Ordinal).Count() == 3,
                            "three distinct rendered text fingerprints are required");
                        Require(evidence.layouts.All(value => !string.IsNullOrWhiteSpace(value.stateFingerprint)) &&
                                evidence.layouts.Select(value => value.stateFingerprint).Distinct(StringComparer.Ordinal).Count() == 1,
                            "locale captures do not share one runtime state fingerprint");
                        return string.Join(" | ", evidence.layouts.Select(DescribeLayout).ToArray());
                    },
                    "Resolve one live comic ending and capture the same runtime state in KO, EN, and qps-long at 1280x800.",
                    "active ending renderer, localization runtime, and screenshot observation surface");

                Product(checks, "GWC-P07.no_cheats_25_35_profile", "matrix 141 criterion 8", "P1",
                    "A representative live-seeded synthetic profile finishes in 25-35 minutes with observed grant/warp/skip counters all zero",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(evidence.representativeSeed != 0, "representative seed is missing");
                        Require(evidence.syntheticMinutes >= 25f && evidence.syntheticMinutes <= 35f,
                            "synthetic minutes=" + evidence.syntheticMinutes);
                        Require(string.Equals(evidence.profileResult, "PASS", StringComparison.Ordinal),
                            "profile result=" + evidence.profileResult);
                        Require(ZeroCheatCalls(evidence), DescribeCounters(evidence));
                        Require(EventsAreProduction(evidence.events) && evidence.events.Length >= 12,
                            "profile lacks a substantial production interaction trace");
                        return "seed=" + evidence.representativeSeed + "; minutes=" + evidence.syntheticMinutes +
                               "; " + DescribeCounters(evidence);
                    },
                    "Run the representative seed via production input and record wall/profile time plus production action counters.",
                    "active production profile/event/counter observation surface");

                HumanRequired(checks, "GWC-H01.gjc_12_17_20_23_remain_human", "matrix human gates", "P1",
                    "GJC-12, GJC-17, GJC-20, and GJC-23 remain HUMAN_REQUIRED; synthetic timing does not claim human comprehension, measured human timing, six human sessions, or a physical gamepad",
                    "humanSessions=" + evidence.humanSessionCount + "; observationStatus=" + evidence.humanGateStatus,
                    "After Wave C automation is GREEN, run the same candidate build with KO 3 + EN 3 users and a physical gamepad.",
                    "Docs/Design/gamejam-completion-matrix.md");

                Product(checks, "GWC-P08.wave_c_green_closure", "matrix 145 closure", "P0",
                    "Fresh same-run GSN-E05/P05/P10 plus all Wave C natural trace, live comic, and same-run upper+basement checks are PASS",
                    delegate
                    {
                        RequirePriorPass("gamejam-search-node-edit-contracts.json", "GSN-E05");
                        RequirePriorPass("gamejam-search-node-play-contracts.json", "GSN-P05", "GSN-P10");
                        string[] required = { "GWC-P01", "GWC-P02", "GWC-P03", "GWC-P04", "GWC-P05", "GWC-P06", "GWC-P07" };
                        foreach (string prefix in required)
                        {
                            Require(checks.Any(value => value.id.StartsWith(prefix, StringComparison.Ordinal) && value.status == "PASS"),
                                prefix + " is not PASS in this run");
                        }
                        return "fresh GSN-E05/P05/P10 and Wave C P01-P07 PASS in RunId=" + RunId;
                    },
                    "Run the GSN prerequisite and Wave C gate under one fresh RunId without copying prior JSON files.",
                    "fresh same-run GSN and Wave C reports");

                WriteJson("gamejam-wave-c-play-observation-evidence.json", evidence);
                Report report = WriteReport("gamejam-wave-c-play-contracts", "GameJam Wave C RED-first actual Play contracts", started, checks);
                SessionState.SetBool(PlayExitPassKey, report.infrastructureOverall == "PASS");
                SessionState.SetString(PlayMessageKey, "GameJam Wave C Play contracts: " + report.overall);
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static AssignmentEvidence[] ObserveAssignments()
        {
            PrototypeProtectedPartAssignmentSnapshot[] values =
                PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                    PrototypeExpeditionRegionCatalog.DefaultRunSeed,
                    PrototypeSearchRegionCatalog.ContractRevision);
            return values.OrderBy(value => value.PartId, StringComparer.Ordinal).Select(value => new AssignmentEvidence
            {
                partId = value.PartId,
                nodeId = value.AssignedNodeId,
                regionId = value.SourceRegionId,
                eligibleNodeIds = PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(value.PartId)
                    .OrderBy(item => item, StringComparer.Ordinal).ToArray(),
                assignedToEligibleNode = PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(value.PartId)
                    .Contains(value.AssignedNodeId)
            }).ToArray();
        }

        private static int[] ObservePity(out bool cancelledIgnored, out bool failedIgnored,
            out bool ineligibleIgnored, out bool duplicateIgnored, out int hintAt, out int guaranteeAt)
        {
            PrototypeKeyPartPityState state = new PrototypeKeyPartPityState
            {
                KeyPartId = PrototypeSearchNodeLootResolver.RadioTransceiverPartId
            };
            int before = state.EligibleSearchCount;
            state.RecordSearch("wave-c.cancel", false, true, true, false, false);
            cancelledIgnored = state.EligibleSearchCount == before;
            state.RecordSearch("wave-c.fail", false, true, false, true, false);
            failedIgnored = state.EligibleSearchCount == before;
            state.RecordSearch("wave-c.ineligible", true, false, false, false, false);
            ineligibleIgnored = state.EligibleSearchCount == before;
            List<int> counts = new List<int>();
            hintAt = -1;
            guaranteeAt = -1;
            for (int index = 1; index <= 5; index += 1)
            {
                state.RecordSearch("wave-c.eligible." + index, true, true, false, false, false);
                counts.Add(state.EligibleSearchCount);
                if (state.HintVisible && hintAt < 0) hintAt = state.EligibleSearchCount;
                if (state.Guaranteed && guaranteeAt < 0) guaranteeAt = state.EligibleSearchCount;
            }
            int after = state.EligibleSearchCount;
            state.RecordSearch("wave-c.eligible.5", true, true, false, false, false);
            duplicateIgnored = state.EligibleSearchCount == after;
            return counts.ToArray();
        }

        private static RouteAuditEvidence[] ObserveRouteAudits()
        {
            int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            return new[]
            {
                PrototypeEscapeRouteAuditor.Audit(seed, "seed", 1),
                PrototypeEscapeRouteAuditor.Audit(seed, "expansion", 11),
                PrototypeEscapeRouteAuditor.Audit(seed, "late-run", 35)
            }.Select(value => new RouteAuditEvidence
            {
                seed = value.Seed,
                day = value.Day,
                auditPointId = value.AuditPointId,
                completableEscapeIds = value.CompletableEscapeIds == null
                    ? Array.Empty<string>()
                    : value.CompletableEscapeIds.ToArray()
            }).ToArray();
        }

        private static PlayEvidence ObservePlay()
        {
            PlayEvidence evidence = new PlayEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                scene = SceneManager.GetActiveScene().path,
                discoveryPolicy = "Active enabled scene MonoBehaviour; public instance parameterless WaveC observation; non-primitive rich structured return. Static/fixture/editor/bool/string evidence is rejected."
            };
            try
            {
                MonoBehaviour[] owners = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
                foreach (MonoBehaviour owner in owners.Where(value => value != null && value.isActiveAndEnabled))
                {
                    MethodInfo method = owner.GetType().GetMethods(PublicInstance)
                        .Where(IsWaveCObservationMethod)
                        .OrderBy(value => value.Name, StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (method == null) continue;
                    object observed = method.Invoke(owner, null);
                    if (observed == null) continue;
                    evidence.liveObservationOwner = owner.GetType().FullName;
                    evidence.liveObservationMethod = method.Name;
                    evidence.liveObservationSurface = DescribeSurface(observed.GetType());
                    CopyObservation(observed, evidence);
                    MergeTerminalComicGeometry(owner, evidence);
                    return evidence;
                }
                evidence.observationError = "No active production component exposes a qualifying rich Wave C observation method.";
            }
            catch (Exception exception)
            {
                evidence.observationError = exception.GetType().Name + ": " + exception.Message;
            }
            return evidence;
        }

        private static bool IsWaveCObservationMethod(MethodInfo method)
        {
            if (method == null || method.IsStatic || method.ContainsGenericParameters ||
                method.GetParameters().Length != 0 || method.ReturnType == typeof(void) ||
                method.ReturnType.IsPrimitive || method.ReturnType.IsEnum || method.ReturnType == typeof(string)) return false;
            string declaring = method.DeclaringType == null ? string.Empty : method.DeclaringType.FullName;
            if (declaring.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                declaring.IndexOf("Fixture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                declaring.IndexOf("ParallelQA", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (method.Name.IndexOf("WaveC", StringComparison.OrdinalIgnoreCase) < 0 ||
                method.Name.IndexOf("Observation", StringComparison.OrdinalIgnoreCase) < 0) return false;
            string surface = DescribeSurface(method.ReturnType).ToLowerInvariant();
            string[][] groups =
            {
                new[] { "protect", "pity" },
                new[] { "escape", "completable" },
                new[] { "event", "interaction" },
                new[] { "module", "upper", "basement", "save" },
                new[] { "layout", "comic", "modifier" },
                new[] { "minute", "profile" },
                new[] { "grant", "warp", "skip" }
            };
            return groups.All(group => group.Any(surface.Contains));
        }

        private static void CopyObservation(object observed, PlayEvidence evidence)
        {
            evidence.observationError = ReadString(observed, "ObservationError", "Error", "FailureReason");
            evidence.evidenceSource = ReadString(observed, "EvidenceSource", "Source", "ObservationSource");
            evidence.protectedPartIds = ReadStrings(observed, "ProtectedPartIds", "OwnedProtectedPartIds");
            evidence.protectedAssignmentPairs = ReadStrings(observed, "ProtectedAssignmentPairs", "AssignmentPairs");
            evidence.eligibleAssignmentPairs = ReadStrings(observed, "EligibleAssignmentPairs", "EligiblePairs");
            evidence.pityEligibleCountSequence = ReadInts(observed, "PityEligibleCountSequence", "PityCountSequence");
            evidence.knownLootBeforeFingerprint = ReadString(observed, "KnownLootBeforeFingerprint", "KnownBeforeFingerprint");
            evidence.knownLootAfterFingerprint = ReadString(observed, "KnownLootAfterFingerprint", "KnownAfterFingerprint");
            evidence.protectedBeforeFingerprint = ReadString(observed, "ProtectedBeforeFingerprint", "ProtectedInventoryBeforeFingerprint");
            evidence.protectedAfterFingerprint = ReadString(observed, "ProtectedAfterFingerprint", "ProtectedInventoryAfterFingerprint");
            evidence.completableEscapeIds = ReadStrings(observed, "CompletableEscapeIds", "LiveCompletableEscapeIds");
            evidence.events = ReadEvents(observed);
            evidence.routeBranches = ReadRouteBranches(observed);
            evidence.committedRoomIds = ReadStrings(observed, "CommittedRoomIds", "CommittedModuleRoomIds");
            evidence.reenteredRoomIds = ReadStrings(observed, "ReenteredRoomIds", "ReenteredModuleRoomIds");
            evidence.facilityPlacementRoomIds = ReadStrings(observed, "FacilityPlacementRoomIds");
            evidence.facilityUseRoomIds = ReadStrings(observed, "FacilityUseRoomIds");
            evidence.stableResourceStockLocales = ReadStrings(observed, "StableResourceStockLocales");
            evidence.escapeShortageLocales = ReadStrings(observed, "EscapeShortageLocales");
            evidence.legacyRescueSignalAvailable = ReadBool(observed, true, "LegacyRescueSignalAvailable");
            evidence.terminalControls = ReadTerminalControls(GetMember(observed, "TerminalControls"));
            evidence.escapeResourcesBeforeFingerprint = ReadString(observed, "EscapeResourcesBeforeFingerprint", "EscapeInventoryBeforeFingerprint");
            evidence.escapeResourcesAfterFingerprint = ReadString(observed, "EscapeResourcesAfterFingerprint", "EscapeInventoryAfterFingerprint");
            evidence.saveBeforeFingerprint = ReadString(observed, "SaveBeforeFingerprint", "SaveStateBeforeFingerprint");
            evidence.saveAfterFingerprint = ReadString(observed, "SaveAfterFingerprint", "SaveStateAfterFingerprint");
            evidence.layouts = ReadLayouts(observed);
            evidence.grantCallCount = ReadInt(observed, -1, "GrantCallCount", "GrantCalls");
            evidence.warpCallCount = ReadInt(observed, -1, "WarpCallCount", "WarpCalls");
            evidence.skipCallCount = ReadInt(observed, -1, "SkipCallCount", "SkipCalls");
            evidence.representativeSeed = ReadInt(observed, 0, "RepresentativeSeed", "ProfileSeed");
            evidence.syntheticMinutes = ReadFloat(observed, -1f, "SyntheticMinutes", "ProfileMinutes");
            evidence.profileResult = ReadString(observed, "ProfileResult", "SyntheticProfileResult");
            evidence.humanSessionCount = ReadInt(observed, -1, "HumanSessionCount", "HumanSessions");
            evidence.humanGateStatus = ReadString(observed, "HumanGateStatus", "HumanStatus");
        }

        private static EventEvidence[] ReadEvents(object observed)
        {
            object value = GetMember(observed, "ProductionEvents", "InteractionEvents", "PlaytestEventRecords", "Events");
            return ReadEventsFromValue(value);
        }

        private static EventEvidence[] ReadEventsFromValue(object value)
        {
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<EventEvidence>();
            List<EventEvidence> events = new List<EventEvidence>();
            foreach (object item in enumerable)
            {
                if (item == null || item is string) continue;
                object before = GetMember(item, "StateBefore", "Before", "state_before");
                object after = GetMember(item, "StateAfter", "After", "state_after");
                EventEvidence record = new EventEvidence
                {
                    sequence = ReadInt(item, -1, "Sequence", "sequence"),
                    eventType = ReadString(item, "EventType", "event_type", "Type"),
                    stableEventId = ReadString(item, "StableEventId", "stable_event_id", "EventId"),
                    escapeId = ReadString(item, "EscapeId", "escape_id", "ProjectId"),
                    targetId = ReadString(item, "TargetId", "target_id"),
                    actionId = ReadString(item, "ActionId", "Action", "action"),
                    resultCode = ReadString(item, "ResultCode", "Outcome", "result_code"),
                    source = ReadString(item, "Source", "EvidenceSource", "source"),
                    beforeFingerprint = ReadString(before, "Fingerprint", "fingerprint"),
                    afterFingerprint = ReadString(after, "Fingerprint", "fingerprint"),
                    costDelta = ReadInt(item, int.MinValue, "CostDelta", "cost_delta"),
                    inventoryDelta = ReadInt(item, int.MinValue, "InventoryDelta", "inventory_delta"),
                    healthDelta = ReadInt(item, int.MinValue, "HealthDelta", "health_delta"),
                    projectProgressDelta = ReadInt(item, int.MinValue, "ProjectProgressDelta", "project_progress_delta"),
                    completedStageDelta = ReadInt(item, int.MinValue, "CompletedStageDelta", "completed_stage_delta"),
                    endingDelta = ReadInt(item, int.MinValue, "EndingDelta", "ending_delta"),
                    albumDelta = ReadInt(item, int.MinValue, "AlbumDelta", "album_delta"),
                    albumRecordDelta = ReadInt(item, int.MinValue, "AlbumRecordDelta", "album_record_delta")
                };
                if (!string.IsNullOrWhiteSpace(record.stableEventId)) events.Add(record);
            }
            return events.OrderBy(value => value.sequence).ToArray();
        }

        private static RouteBranchEvidence[] ReadRouteBranches(object observed)
        {
            object value = GetMember(observed, "RouteBranches", "EscapeRouteBranches", "BranchObservations");
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<RouteBranchEvidence>();
            var branches = new List<RouteBranchEvidence>();
            foreach (object item in enumerable)
            {
                if (item == null || item is string) continue;
                branches.Add(new RouteBranchEvidence
                {
                    escapeId = ReadString(item, "EscapeId", "escape_id"),
                    compositeSaveFingerprint = ReadString(item, "CompositeSaveFingerprint", "SaveFingerprint"),
                    restoredStartFingerprint = ReadString(item, "RestoredStartFingerprint", "BranchStartFingerprint"),
                    terminalStateFingerprint = ReadString(item, "TerminalStateFingerprint", "TerminalFingerprint"),
                    completedEscapeId = ReadString(item, "CompletedEscapeId", "TerminalEscapeId"),
                    terminalEndingId = ReadString(item, "TerminalEndingId", "EndingId"),
                    terminalReached = ReadBool(item, false, "TerminalReached", "Terminal"),
                    events = ReadEventsFromValue(GetMember(item, "BranchEvents", "ProductionEvents", "Events"))
                });
            }
            return branches.ToArray();
        }

        private static TerminalControlEvidence ReadTerminalControls(object observed)
        {
            if (observed == null) return new TerminalControlEvidence();
            return new TerminalControlEvidence
            {
                actionIds = ReadStrings(observed, "ActionIds"),
                localizedLabels = ReadStrings(observed, "LocalizedLabels"),
                sortingOrder = ReadInt(observed, -1, "SortingOrder"),
                activeAboveComic = ReadBool(observed, false, "ActiveAboveComic"),
                mouseRaycastReady = ReadBool(observed, false, "MouseRaycastReady"),
                explicitNavigationReady = ReadBool(observed, false, "ExplicitNavigationReady"),
                keyboardSubmitObserved = ReadBool(observed, false, "KeyboardSubmitObserved"),
                gamepadSubmitObserved = ReadBool(observed, false, "GamepadSubmitObserved"),
                backTransitionObserved = ReadBool(observed, false, "BackTransitionObserved"),
                restartTransitionObserved = ReadBool(observed, false, "RestartTransitionObserved")
            };
        }

        private static LayoutEvidence[] ReadLayouts(object observed)
        {
            object value = GetMember(observed, "Layouts", "ComicLayouts", "LocalizedLayouts");
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<LayoutEvidence>();
            List<LayoutEvidence> layouts = new List<LayoutEvidence>();
            foreach (object item in enumerable)
            {
                if (item == null || item is string) continue;
                LayoutEvidence layout = new LayoutEvidence
                {
                    locale = ReadString(item, "Locale", "LocaleCode").ToLowerInvariant(),
                    screenshot = ResolveEvidencePath(ReadString(item, "Screenshot", "CapturePath", "Path")),
                    renderedTextFingerprint = ReadString(item, "RenderedTextFingerprint", "TextFingerprint"),
                    stateFingerprint = ReadString(item, "StateFingerprint", "RuntimeStateFingerprint"),
                    corePanelCount = ReadInt(item, -1, "CorePanelCount", "LiveCorePanelCount"),
                    modifierPanelCount = ReadInt(item, -1, "ModifierPanelCount", "LiveModifierPanelCount"),
                    overflowCount = ReadInt(item, -1, "OverflowCount", "TmpOverflowCount"),
                    offscreenCount = ReadInt(item, -1, "OffscreenCount", "OffscreenTextCount"),
                    clippedRequiredActionCount = ReadInt(item, -1, "ClippedRequiredActionCount", "ClippedActionCount")
                };
                if (!string.IsNullOrWhiteSpace(layout.screenshot) && File.Exists(layout.screenshot))
                {
                    layout.renderSha256 = Sha256(File.ReadAllBytes(layout.screenshot));
                }
                layouts.Add(layout);
            }
            return layouts.ToArray();
        }

        private static void MergeTerminalComicGeometry(MonoBehaviour owner, PlayEvidence evidence)
        {
            if (owner == null || evidence == null || evidence.layouts == null) return;
            MethodInfo method = owner.GetType().GetMethod(
                "CaptureTerminalComicGeometryAudit",
                PublicInstance,
                null,
                Type.EmptyTypes,
                null);
            if (method == null || method.ReturnType == typeof(void) || method.ReturnType.IsPrimitive ||
                method.ReturnType == typeof(string)) return;
            object observed = method.Invoke(owner, null);
            if (!(observed is IEnumerable values) || observed is string) return;
            foreach (object item in values)
            {
                if (item == null) continue;
                string locale = ReadString(item, "Locale", "LocaleCode").ToLowerInvariant();
                LayoutEvidence layout = evidence.layouts.FirstOrDefault(value =>
                    string.Equals(value.locale, locale, StringComparison.Ordinal));
                if (layout == null) continue;
                layout.activeGeometryTextCount = ReadInt(item, -1, "ActiveTextCount", "TextCount");
                layout.textTextOverlapCount = ReadInt(item, -1, "TextTextOverlapCount", "TextOverlapCount");
                layout.textCardBoundaryViolationCount = ReadInt(
                    item,
                    -1,
                    "TextCardBoundaryViolationCount",
                    "CardBoundaryViolationCount");
                layout.titleFontSize = ReadFloat(item, -1f, "TitleFontSize");
                layout.minimumCoreFontSize = ReadFloat(item, -1f, "MinimumCoreFontSize", "CoreFontSize");
                layout.modifierFontSize = ReadFloat(item, -1f, "ModifierFontSize");
                layout.geometryViolations = ReadStrings(item, "Violations", "GeometryViolations");
            }
        }

        private static bool EventsAreProduction(EventEvidence[] events)
        {
            if (events == null || events.Length == 0 || events.Any(value => value == null || value.sequence < 0 ||
                string.IsNullOrWhiteSpace(value.eventType) ||
                string.IsNullOrWhiteSpace(value.stableEventId) ||
                !string.Equals(value.eventType, value.stableEventId, StringComparison.Ordinal) ||
                value.source.IndexOf("production", StringComparison.OrdinalIgnoreCase) < 0)) return false;
            for (int index = 1; index < events.Length; index += 1)
            {
                if (events[index].sequence <= events[index - 1].sequence) return false;
            }
            return true;
        }

        private static void RequireOrderedPositiveRouteActions(
            EventEvidence[] events,
            string escapeId,
            string targetId,
            string[] tokens)
        {
            int cursor = -1;
            foreach (string token in tokens)
            {
                int next = Array.FindIndex(events, cursor + 1, value => value != null &&
                    string.Equals(value.escapeId, escapeId, StringComparison.Ordinal) &&
                    string.Equals(value.targetId, targetId, StringComparison.Ordinal) &&
                    EventText(value).IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    IsRouteProgressEventType(value) &&
                    IsPositiveRouteDelta(value));
                Require(next >= 0,
                    escapeId + " missing ordered positive route action '" + token + "' after sequence " + cursor +
                    "; observed=" + DescribeRouteEvents(events, escapeId));
                cursor = next;
            }
        }

        private static void RequireRouteBranch(
            PlayEvidence evidence,
            string escapeId,
            string targetId,
            string[] orderedActionTokens)
        {
            RouteBranchEvidence branch = evidence.routeBranches.SingleOrDefault(value => value != null &&
                string.Equals(value.escapeId, escapeId, StringComparison.Ordinal));
            Require(branch != null, escapeId + " independent composite-save branch is missing");
            Require(SameNonEmpty(branch.compositeSaveFingerprint, branch.restoredStartFingerprint),
                escapeId + " did not start from the common composite save fingerprint");
            Require(branch.terminalReached &&
                    string.Equals(branch.completedEscapeId, escapeId, StringComparison.Ordinal) &&
                    string.Equals(branch.terminalEndingId, ExpectedEndingForEscape(escapeId), StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(branch.terminalStateFingerprint),
                escapeId + " branch did not reach its exact terminal state: completed=" +
                branch.completedEscapeId + "; ending=" + branch.terminalEndingId);
            Require(EventsAreProduction(branch.events), escapeId + " branch production events are missing or unordered");
            Require(branch.events.All(value => string.Equals(value.escapeId, escapeId, StringComparison.Ordinal)),
                escapeId + " branch is contaminated by another escape route");
            RequireOrderedPositiveRouteActions(branch.events, escapeId, targetId, orderedActionTokens);
            EventEvidence[] terminalEvents = branch.events.Where(value => value != null &&
                string.Equals(value.eventType, "ending.resolved", StringComparison.Ordinal)).ToArray();
            Require(terminalEvents.Length == 1 && terminalEvents[0].endingDelta == 1 &&
                    terminalEvents[0].sequence == branch.events.Max(value => value.sequence),
                escapeId + " branch must end in exactly one terminal ending.resolved event: " +
                DescribeRouteEvents(branch.events, escapeId));
        }

        private static string ExpectedEndingForEscape(string escapeId)
        {
            switch (escapeId)
            {
                case "escape.raft":
                    return "ending.escape.raft.open-water";
                case "escape.smoke":
                    return "ending.escape.smoke.seen-from-afar";
                case "escape.radio":
                    return "ending.escape.radio.clear-signal";
                default:
                    return string.Empty;
            }
        }

        private static bool IsLocalizedResourceEvidence(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf("⟦", StringComparison.Ordinal) < 0 &&
                   value.IndexOf("resource.", StringComparison.Ordinal) < 0 &&
                   value.Contains("=") && value.Any(char.IsDigit);
        }

        private static bool IsLocalizedShortageEvidence(string value)
        {
            return IsLocalizedResourceEvidence(value) &&
                   (value.IndexOf("부족", StringComparison.Ordinal) >= 0 ||
                    value.IndexOf("Missing", StringComparison.OrdinalIgnoreCase) >= 0) &&
                   (value.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("radio", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("연기", StringComparison.Ordinal) >= 0 ||
                    value.IndexOf("무전", StringComparison.Ordinal) >= 0);
        }

        private static void RequireTerminalControls(TerminalControlEvidence controls)
        {
            Require(controls != null && controls.actionIds.Length == 2 &&
                    controls.actionIds.Contains("ending.back") && controls.actionIds.Contains("session.restart"),
                "terminal back/restart action IDs are missing");
            Require(controls.localizedLabels.Length == 2 &&
                    controls.localizedLabels.All(value => !string.IsNullOrWhiteSpace(value) &&
                                                          value.IndexOf("⟦", StringComparison.Ordinal) < 0),
                "terminal control labels are missing or unlocalized");
            Require(controls.sortingOrder > 120 && controls.activeAboveComic &&
                    controls.mouseRaycastReady && controls.explicitNavigationReady,
                "terminal controls are not visible above the comic with mouse raycast and explicit navigation");
            Require(controls.keyboardSubmitObserved && controls.gamepadSubmitObserved &&
                    controls.backTransitionObserved && controls.restartTransitionObserved,
                "terminal keyboard/gamepad submit did not execute real back and restart transitions");
        }

        private static void RequireAtomicRouteCycle(EventEvidence[] events, string escapeId)
        {
            EventEvidence fail = FindRouteEvent(events, escapeId, "escape.interaction.failed");
            EventEvidence cancel = FindRouteEvent(events, escapeId, "escape.interaction.cancelled");
            EventEvidence wait = FindRouteEvent(events, escapeId, "escape.forecast.wait");
            EventEvidence retry = events.FirstOrDefault(value => value != null &&
                string.Equals(value.escapeId, escapeId, StringComparison.Ordinal) &&
                EventText(value).IndexOf("retry", StringComparison.OrdinalIgnoreCase) >= 0 &&
                IsRouteProgressEventType(value) &&
                IsPositiveRouteDelta(value) && wait != null && value.sequence > wait.sequence);

            Require(IsZeroDelta(fail), escapeId + " exact fail event is missing or non-zero: " + DescribeEvent(fail));
            Require(IsZeroDelta(cancel), escapeId + " exact cancel event is missing or non-zero: " + DescribeEvent(cancel));
            Require(IsZeroDelta(wait), escapeId + " exact wait event is missing or non-zero: " + DescribeEvent(wait));
            Require(retry != null, escapeId + " retry lacks a positive project-stage/resource delta after wait; observed=" +
                                   DescribeRouteEvents(events, escapeId));
            Require(fail.sequence < cancel.sequence && cancel.sequence < wait.sequence && wait.sequence < retry.sequence,
                escapeId + " atomic event order must be fail<cancel<wait<positive retry; observed=" +
                DescribeRouteEvents(events, escapeId));
        }

        private static EventEvidence FindRouteEvent(EventEvidence[] events, string escapeId, string eventType)
        {
            return events.FirstOrDefault(value => value != null && string.Equals(value.escapeId, escapeId, StringComparison.Ordinal) &&
                string.Equals(value.eventType, eventType, StringComparison.Ordinal));
        }

        private static bool IsZeroDelta(EventEvidence value)
        {
            return value != null && value.costDelta == 0 && value.inventoryDelta == 0 && value.healthDelta == 0 &&
                   value.projectProgressDelta == 0 && value.completedStageDelta == 0 &&
                   value.endingDelta == 0 && value.albumDelta == 0 && value.albumRecordDelta == 0 &&
                   SameNonEmpty(value.beforeFingerprint, value.afterFingerprint);
        }

        private static bool IsPositiveRouteDelta(EventEvidence value)
        {
            return value != null &&
                   (value.projectProgressDelta > 0 || value.completedStageDelta > 0 ||
                    value.costDelta > 0 || value.inventoryDelta < 0) &&
                   !SameNonEmpty(value.beforeFingerprint, value.afterFingerprint);
        }

        private static bool IsRouteProgressEventType(EventEvidence value)
        {
            return value != null &&
                   (string.Equals(value.eventType, "escape.interaction.progressed", StringComparison.Ordinal) ||
                    string.Equals(value.eventType, "ending.resolved", StringComparison.Ordinal));
        }

        private static void RequireTerminalEndingContract(EventEvidence[] events)
        {
            EventEvidence[] endings = events.Where(value => value != null &&
                string.Equals(value.eventType, "ending.resolved", StringComparison.Ordinal)).ToArray();
            EventEvidence[] albums = events.Where(value => value != null &&
                string.Equals(value.eventType, "ending.album.recorded", StringComparison.Ordinal)).ToArray();
            EventEvidence[] duplicates = events.Where(value => value != null &&
                string.Equals(value.eventType, "ending.terminal.duplicate", StringComparison.Ordinal)).ToArray();

            Require(endings.Length == 1, "exact event type ending.resolved count=" + endings.Length);
            Require(albums.Length == 1, "exact event type ending.album.recorded count=" + albums.Length);
            Require(duplicates.Length == 1,
                "exact event type ending.terminal.duplicate count=" + duplicates.Length +
                "; root observation must actuate the terminal control twice and record the duplicate attempt");
            Require(endings[0].endingDelta == 1,
                "ending.resolved must have endingDelta=1: " + DescribeEvent(endings[0]));
            Require(albums[0].albumRecordDelta == 1,
                "ending.album.recorded must commit the terminal album record exactly once: " + DescribeEvent(albums[0]));
            bool newlyUnlocked = albums[0].resultCode.IndexOf(".new", StringComparison.OrdinalIgnoreCase) >= 0;
            bool alreadyKnown = albums[0].resultCode.IndexOf(".known", StringComparison.OrdinalIgnoreCase) >= 0;
            Require((newlyUnlocked && albums[0].albumDelta == 1) ||
                    (alreadyKnown && albums[0].albumDelta == 0),
                "ending album result must preserve new/known collection semantics: " + DescribeEvent(albums[0]));
            Require(endings[0].sequence != albums[0].sequence,
                "ending.resolved and ending.album.recorded must be distinct event records");
            Require(IsZeroDelta(duplicates[0]),
                "terminal duplicate actuation must be zero-delta: " + DescribeEvent(duplicates[0]));
            Require(duplicates[0].sequence > endings[0].sequence && duplicates[0].sequence > albums[0].sequence,
                "terminal duplicate actuation must occur after ending and album records: " + DescribeEvent(duplicates[0]));
        }

        private static string DescribeRouteEvents(IEnumerable<EventEvidence> events, string escapeId)
        {
            return string.Join(" || ", (events ?? Array.Empty<EventEvidence>())
                .Where(value => value != null && string.Equals(value.escapeId, escapeId, StringComparison.Ordinal))
                .Select(DescribeEvent).ToArray());
        }

        private static string DescribeEvent(EventEvidence value)
        {
            if (value == null) return "missing";
            return "seq=" + value.sequence + "; type=" + value.eventType + "; action=" + value.actionId +
                   "; result=" + value.resultCode + "; cost=" + value.costDelta +
                   "; inventory=" + value.inventoryDelta + "; health=" + value.healthDelta +
                   "; progress=" + value.projectProgressDelta + "; stages=" + value.completedStageDelta +
                   "; ending=" + value.endingDelta + "; album=" + value.albumDelta +
                   "; albumRecord=" + value.albumRecordDelta;
        }

        private static bool HasEventToken(IEnumerable<EventEvidence> events, string first, string second)
        {
            return (events ?? Array.Empty<EventEvidence>()).Any(value => value != null &&
                EventText(value).IndexOf(first, StringComparison.OrdinalIgnoreCase) >= 0 &&
                EventText(value).IndexOf(second, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string EventText(EventEvidence value)
        {
            if (value == null) return string.Empty;
            return value.eventType + "|" + value.stableEventId + "|" + value.actionId + "|" + value.resultCode + "|" + value.targetId;
        }

        private static bool ContainsUpperAndBasement(IEnumerable<string> values)
        {
            var source = new HashSet<string>(
                values ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            return source.Contains(PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).RoomId) &&
                   source.Contains(PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement).RoomId);
        }

        private static void RequireLive(PlayEvidence evidence)
        {
            Require(evidence != null && !string.IsNullOrWhiteSpace(evidence.liveObservationOwner) &&
                    !string.IsNullOrWhiteSpace(evidence.liveObservationMethod),
                "live Wave C observation missing: " + (evidence == null ? "null" : evidence.observationError));
            Require(evidence.evidenceSource.IndexOf("production", StringComparison.OrdinalIgnoreCase) >= 0,
                "observation source is not production-live: " + evidence.evidenceSource);
        }

        private static bool ZeroCheatCalls(PlayEvidence evidence)
        {
            return evidence != null && evidence.grantCallCount == 0 && evidence.warpCallCount == 0 && evidence.skipCallCount == 0;
        }

        private static string DescribeCounters(PlayEvidence evidence)
        {
            return evidence == null ? "counters=null" : "grant/warp/skip=" + evidence.grantCallCount + "/" +
                evidence.warpCallCount + "/" + evidence.skipCallCount;
        }

        private static string DescribeLayout(LayoutEvidence value)
        {
            return value.locale + " core/modifier=" + value.corePanelCount + "/" + value.modifierPanelCount +
                   " overflow/offscreen/clipped=" + value.overflowCount + "/" + value.offscreenCount + "/" +
                   value.clippedRequiredActionCount + " geometryTexts/textOverlap/cardBoundary=" +
                   value.activeGeometryTextCount + "/" + value.textTextOverlapCount + "/" +
                   value.textCardBoundaryViolationCount + " fonts(title/core/modifier)=" +
                   value.titleFontSize + "/" + value.minimumCoreFontSize + "/" + value.modifierFontSize +
                   (value.geometryViolations == null || value.geometryViolations.Length == 0
                       ? string.Empty
                       : " violations=" + string.Join(",", value.geometryViolations));
        }

        private static bool SameNonEmpty(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left) && string.Equals(left, right, StringComparison.Ordinal);
        }

        private static string DescribeEndingSurface()
        {
            string definition = DescribeSurface(typeof(PrototypeEndingDefinition));
            string runtime = DescribeSurface(
                typeof(PrototypeEndingCatalog).Assembly.GetType("KimSurvival.PrototypeWaveRuntime"));
            string comics = string.Join(",", PrototypeEndingCatalog.All.Where(value => value.Category == "comic")
                .Select(value => value.StableId + ":core=" + value.PanelKeys.Length).ToArray());
            return "definition=" + definition + "; runtime=" + runtime + "; comics=" + comics;
        }

        private static string DescribeEndingLocalization(out bool passed)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Project", "Scripts", "Localization", "PrototypeStrings.tsv"));
            if (!File.Exists(path))
            {
                passed = false;
                return "missing localization TSV: " + path;
            }
            string[] lines = File.ReadAllLines(path);
            bool header = lines.Length > 0 && lines[0].Split('\t').SequenceEqual(new[] { "Key", "ko", "en", "qps-long" });
            string[] comicIds = PrototypeEndingCatalog.All.Where(value => value.Category == "comic")
                .Select(value => value.StableId).ToArray();
            bool rows = comicIds.All(id => new[] { ".title", ".summary", ".hint" }.All(suffix =>
                lines.Any(line => line.StartsWith(id + suffix + "\t", StringComparison.Ordinal) &&
                                  line.Split('\t').Length >= 4 && line.Split('\t').Skip(1).All(cell => !string.IsNullOrWhiteSpace(cell)))));
            passed = header && rows;
            return "header=" + header + "; comicIds=" + comicIds.Length + "; localizedCoreRows=" + rows;
        }

        private static string DescribeSurface(Type type, params string[] preferredTokens)
        {
            if (type == null) return string.Empty;
            string[] names = type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                .Where(value => value.MemberType == MemberTypes.Field || value.MemberType == MemberTypes.Property || value.MemberType == MemberTypes.Method)
                .Select(value => value.Name).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (preferredTokens != null && preferredTokens.Length > 0)
            {
                string[] matched = names.Where(name => preferredTokens.Any(token => name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                return type.FullName + "{" + string.Join(",", matched) + "}";
            }
            return type.FullName + "{" + string.Join(",", names) + "}";
        }

        private static void RequirePriorPass(string fileName, params string[] idPrefixes)
        {
            string path = Path.Combine(EvidenceFolder, fileName);
            Require(File.Exists(path), "fresh same-run prerequisite missing: " + fileName);
            PriorReport report = JsonUtility.FromJson<PriorReport>(File.ReadAllText(path));
            Require(report != null && report.runId == RunId && report.baselineCommit == BaselineCommit,
                "prerequisite identity mismatch: " + fileName);
            foreach (string prefix in idPrefixes)
            {
                Require((report.checks ?? Array.Empty<PriorCheck>()).Any(value => value != null &&
                    value.id.StartsWith(prefix, StringComparison.Ordinal) && value.status == "PASS"),
                    prefix + " is not PASS in " + fileName);
            }
        }

        private static object GetMember(object owner, params string[] names)
        {
            if (owner == null) return null;
            Type type = owner.GetType();
            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, PublicInstance | BindingFlags.IgnoreCase);
                if (field != null) return field.GetValue(owner);
                PropertyInfo property = type.GetProperty(name, PublicInstance | BindingFlags.IgnoreCase);
                if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(owner, null);
            }
            return null;
        }

        private static string ReadString(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            return value == null ? string.Empty : Convert.ToString(value) ?? string.Empty;
        }

        private static int ReadInt(object owner, int fallback, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value == null) return fallback;
            try { return Convert.ToInt32(value); } catch { return fallback; }
        }

        private static bool ReadBool(object owner, bool fallback, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value == null) return fallback;
            try { return Convert.ToBoolean(value); } catch { return fallback; }
        }

        private static float ReadFloat(object owner, float fallback, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value == null) return fallback;
            try { return Convert.ToSingle(value); } catch { return fallback; }
        }

        private static string[] ReadStrings(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<string>();
            return enumerable.Cast<object>().Where(item => item != null).Select(item => Convert.ToString(item) ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        }

        private static int[] ReadInts(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<int>();
            List<int> result = new List<int>();
            foreach (object item in enumerable)
            {
                try { result.Add(Convert.ToInt32(item)); } catch { }
            }
            return result.ToArray();
        }

        private static string ResolveEvidencePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Path.IsPathRooted(value) ? Path.GetFullPath(value) : Path.GetFullPath(Path.Combine(EvidenceFolder, value));
        }

        private static string Sha256(byte[] bytes)
        {
            using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(bytes).Select(value => value.ToString("x2")).ToArray());
            }
        }

        private static void Product(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> action, string reproduction, string recommendedFiles)
        {
            try
            {
                checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, action(), reproduction, recommendedFiles));
            }
            catch (Exception exception)
            {
                string status = IsRedBaseline ? "EXPECTED_GAP" : "FAIL";
                string classification = IsRedBaseline ? "PRODUCT_EXPECTED_GAP" : "PRODUCT_REGRESSION";
                checks.Add(NewCheck(id, matrix, status, classification, severity, expected,
                    exception.GetType().Name + ": " + exception.Message, reproduction, recommendedFiles));
            }
        }

        private static void Infrastructure(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> action, string reproduction, string recommendedFiles)
        {
            try
            {
                checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, action(), reproduction, recommendedFiles));
            }
            catch (Exception exception)
            {
                checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected,
                    exception.GetType().Name + ": " + exception.Message, reproduction, recommendedFiles));
            }
        }

        private static void HumanRequired(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string recommendedFiles)
        {
            checks.Add(NewCheck(id, matrix, "HUMAN_REQUIRED", "MANUAL_ONLY", severity, expected, actual, reproduction, recommendedFiles));
        }

        private static Check NewCheck(string id, string matrix, string status, string classification, string severity,
            string expected, string actual, string reproduction, string recommendedFiles)
        {
            return new Check
            {
                id = id,
                matrix = matrix,
                status = status,
                classification = classification,
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = recommendedFiles
            };
        }

        private static Report WriteReport(string stem, string title, DateTime started, List<Check> checks)
        {
            Report report = new Report
            {
                title = title,
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                passed = checks.Count(value => value.status == "PASS"),
                expectedGaps = checks.Count(value => value.status == "EXPECTED_GAP"),
                productFailed = checks.Count(value => value.status == "FAIL"),
                infrastructureFailed = checks.Count(value => value.status == "INFRA_FAIL"),
                humanRequired = checks.Count(value => value.status == "HUMAN_REQUIRED"),
                greenCompletionCondition = "Fresh GSN-E05/P05/P10 plus Wave C protected/pity, three natural escape routes, atomic retry, same-run upper+basement save, live KO/EN/qps comic, and 25-35 minute zero-cheat profile all PASS. GJC-12/17/20/23 remain HUMAN_REQUIRED.",
                checks = checks.ToArray()
            };
            report.infrastructureOverall = report.infrastructureFailed == 0 ? "PASS" : "FAIL";
            report.productOverall = report.productFailed > 0 ? "FAIL" : report.expectedGaps > 0 ? "RED_EXPECTED_GAP" : "PASS";
            report.overall = report.infrastructureOverall == "FAIL" ? "FAIL" : report.productOverall == "PASS" ? "GREEN" : "RED";
            WriteJson(stem + ".json", report);
            string[] lines = new[]
            {
                title,
                "Run ID: " + RunId,
                "Baseline: " + BaselineCommit,
                "Unity: " + Application.unityVersion,
                "Overall/Product/Infrastructure: " + report.overall + "/" + report.productOverall + "/" + report.infrastructureOverall,
                "PASS/EXPECTED_GAP/FAIL/INFRA_FAIL/HUMAN_REQUIRED: " + report.passed + "/" + report.expectedGaps + "/" +
                report.productFailed + "/" + report.infrastructureFailed + "/" + report.humanRequired
            }.Concat(checks.Select(value => value.id + " | " + value.status + " | " + value.classification + " | " + value.actual)).ToArray();
            File.WriteAllText(Path.Combine(EvidenceFolder, stem + ".txt"),
                string.Join(Environment.NewLine, lines) + Environment.NewLine, Utf8NoBom);
            return report;
        }

        private static void WriteJson(string fileName, object value)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, fileName), JsonUtility.ToJson(value, true) + Environment.NewLine, Utf8NoBom);
        }

        private static void WritePlayInfrastructureFailure(Exception exception)
        {
            List<Check> checks = new List<Check>
            {
                NewCheck("GWC-I02.play_runner", "Wave C Play infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Play runner emits fresh structured JSON/TXT evidence", exception.GetType().Name + ": " + exception.Message,
                    "Run the Wave C PowerShell entry point and inspect the isolated Unity log.",
                    "Assets/Editor/ParallelQA/GameJamWaveCRedFirstGateRunner.cs")
            };
            WriteReport("gamejam-wave-c-play-contracts", "GameJam Wave C RED-first actual Play contracts", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: " + exception);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Sanitize(string value)
        {
            return new string(value.Select(character => char.IsLetterOrDigit(character) || character == '-' || character == '_'
                ? character : '_').ToArray());
        }

        private static void StopPlayContracts()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        private static void FinishPlayContracts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (playTickAttached)
            {
                EditorApplication.update -= PlayTick;
                playTickAttached = false;
            }
            bool passed = SessionState.GetBool(PlayExitPassKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing GameJam Wave C Play result");
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayExitPassKey);
            SessionState.EraseString(PlayMessageKey);
            Debug.Log("[ParallelQA] " + message);
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
