using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KimSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParallelQA
{
    /// <summary>
    /// Independent RED-first contract for the playable escape.raft route.
    /// Discovery is based on stable IDs, public state/result members, actual
    /// camp targets, and live UI geometry. Product source file/class names are
    /// deliberately not acceptance criteria.
    /// </summary>
    public static class Wave20RaftRedFirstGateRunner
    {
        private const string RedBaseline = "09ae2a6d578eb4dcbf11b9c571f57f640b88d969";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RaftId = "escape.raft";
        private const string LaunchId = "facility.shore-launch";
        private const string SailclothId = "part.raft.sailcloth";
        private const string RaftEndingId = "ending.escape.raft.open-water";
        private const string PlayRunningKey = "ParallelQA.Wave20.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave20.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave20.PlayMessage";
        private const float CaptureWidth = 1280f;
        private const float CaptureHeight = 800f;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static bool playTickAttached;
        private static double playEarliestRunTime;
        private static double playTimeoutAt;

        [Serializable]
        private sealed class Check
        {
            public string id;
            public string matrix;
            public string status;
            public string classification;
            public string severity;
            public string expected;
            public string actual;
            public string reproduction;
            public string recommendedFiles;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion = 1;
            public string title;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string overall;
            public string productOverall;
            public string infrastructureOverall;
            public int passed;
            public int expectedGaps;
            public int productFailed;
            public int infrastructureFailed;
            public int unverified;
            public int notReady;
            public string greenCompletionCondition;
            public string physicalGamepad = "UNVERIFIED";
            public string steamReadiness = "NOT_READY";
            public Check[] checks;
        }

        [Serializable]
        private sealed class EditEvidence
        {
            public string runId;
            public string baselineCommit;
            public string definition;
            public string semanticSurface;
            public string[] localizationKeys;
            public string endingResolution;
            public string albumSnapshot;
        }

        [Serializable]
        private sealed class PixelRectEvidence
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [Serializable]
        private sealed class LayoutEvidence
        {
            public string locale;
            public string state;
            public string screenshot;
            public PixelRectEvidence rect;
            public int activeTextCount;
            public int overflowCount;
            public int offscreenTextCount;
            public bool insideScreen;
            public bool compactPrompt;
            public string result;
            public string failureReason;
        }

        [Serializable]
        private sealed class RouteEvidence
        {
            public string runtimeType;
            public string stableId;
            public string escapeId;
            public string endingId;
            public string resultCode;
            public bool success;
            public bool completed;
            public bool terminal;
            public bool grant;
            public bool warp;
            public bool skip;
            public bool skipObserved;
            public int interactionCount;
            public int day;
            public bool unsafeWindowRejected;
            public bool unsafeWindowRejectedObserved;
            public bool allowedWindowLaunched;
            public bool allowedWindowLaunchedObserved;
            public bool cancelUnchanged;
            public bool cancelUnchangedObserved;
            public bool failureAtomic;
            public bool failureAtomicObserved;
            public int failureApplications;
            public bool failureApplicationsObserved;
            public int costCommitCount;
            public bool costCommitCountObserved;
            public int duplicateCostDelta;
            public bool duplicateCostDeltaObserved;
            public int duplicateTerminalDelta;
            public bool duplicateTerminalDeltaObserved;
            public bool earlyEscape;
            public bool earlyEscapeObserved;
            public bool restoreSame;
            public bool restoreSameObserved;
            public int albumUnlockDelta;
            public bool albumUnlockDeltaObserved;
            public int duplicateAlbumDelta;
            public bool duplicateAlbumDeltaObserved;
            public bool albumRestored;
            public bool albumRestoredObserved;
            public string[] protectedKeyPartIds;
            public string[] restoredStageIds;
            public string[] interactionTrace;
            public string publicMembers;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId;
            public string baselineCommit;
            public string scene;
            public string discoveryPolicy;
            public string[] targetIds;
            public bool launchTargetFound;
            public int farPromptCount;
            public int nearPromptCount;
            public bool popupOpened;
            public bool promptHiddenWhilePopup;
            public bool promptRestoredAfterCancel;
            public bool keyboardMouseSyntheticGamepadParity;
            public string keyboardActionText;
            public string gamepadActionText;
            public string observationError;
            public RouteEvidence route;
            public LayoutEvidence[] layouts;
            public string[] joystickNames;
        }

        private struct PixelRect
        {
            public float X;
            public float Y;
            public float Width;
            public float Height;
            public float Right { get { return X + Width; } }
            public float Top { get { return Y + Height; } }
            public PixelRectEvidence Evidence()
            {
                return new PixelRectEvidence { x = X, y = Y, width = Width, height = Height };
            }
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "wave20-missing-run-id" : Sanitize(value);
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

        private static bool IsRedBaseline { get { return string.Equals(BaselineCommit, RedBaseline, StringComparison.OrdinalIgnoreCase); } }
        private static string EvidenceFolder
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts", "ParallelQA", RunId)); }
        }

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<Check> checks = new List<Check>();
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(RaftId);
            string semanticSurface = DescribeSemanticSurface(definition);
            string[] localizationKeys = ReadRaftLocalizationKeys();
            string endingResolution = string.Empty;
            string albumSnapshot = string.Empty;

            Infrastructure(checks, "W20-I01.exact_baseline", "Edit infrastructure", "P0",
                "The runner records the exact requested baseline SHA and a fresh RunId",
                delegate
                {
                    Require(!string.IsNullOrWhiteSpace(RunId) && RunId != "wave20-missing-run-id", "KIM_PARALLEL_QA_RUN_ID is missing");
                    Require(BaselineCommit.Length == 40, "baseline is not a full SHA: " + BaselineCommit);
                    return "runId=" + RunId + "; baseline=" + BaselineCommit + "; redBaseline=" + IsRedBaseline;
                },
                "Invoke the PowerShell entry point with a fresh RunId and the exact HEAD.",
                "Assets/Editor/ParallelQA/Invoke-Wave20RaftRedFirstGate.ps1");

            Product(checks, "W20-E01.raft_canonical_data", "raft catalog", "P0",
                "escape.raft retains coast/shallows, shore-launch, sailcloth, material/risk/timing/completion, and raft ending IDs",
                delegate
                {
                    Require(definition != null && definition.StableId == RaftId, "escape.raft definition missing");
                    Require(definition.RegionIds.Contains("region.coast.beach") && definition.RegionIds.Contains("region.sea.shallows"), "coast/shallows regions missing");
                    Require(definition.FacilityId == LaunchId, "shore launch facility mismatch: " + definition.FacilityId);
                    Require(definition.KeyPartId == SailclothId, "sailcloth part mismatch: " + definition.KeyPartId);
                    Require(definition.MaterialIds.Contains("resource.wood") && definition.MaterialIds.Contains("resource.fabric"), "raft material categories incomplete");
                    Require(definition.RiskIds.Contains("hazard.disaster") && definition.RiskIds.Contains("hazard.injury"), "raft risks incomplete");
                    Require(ContainsAll(definition.TimingRule, "weather", "window"), "weather launch-window rule missing");
                    Require(ContainsAll(definition.CompletionRule, "hull", "sailcloth", "supplies", "once"), "three-part once-only completion rule missing");
                    Require(PrototypeEndingCatalog.All.Any(value => value.StableId == RaftEndingId && value.RequiredEscapeId == RaftId), "raft ending link missing");
                    return DescribeDefinition(definition);
                },
                "Inspect the public escape catalog by stable ID.",
                "runtime escape catalog/data owner selected by stable ID");

            Product(checks, "W20-E02.playable_three_stage_transition", "raft runtime schema", "P0",
                "escape.raft is playable and exposes ordered hull, sail, and voyage-supplies stage state",
                delegate
                {
                    Require(!definition.DataOnly && definition.PlayableState.StartsWith("playable", StringComparison.Ordinal), "escape.raft remains " + definition.PlayableState);
                    Require(definition.RequiredProgress >= 3 || ContainsAll(semanticSurface, "hull", "sail", "suppl"), "three-stage progress surface missing");
                    Require(ContainsAll(semanticSurface, "hull", "sail", "suppl"), "ordered stage semantics are not publicly inspectable");
                    return "playable=" + (!definition.DataOnly) + "; requiredProgress=" + definition.RequiredProgress + "; surface=" + semanticSurface;
                },
                "Query escape.raft and stable/public raft state members without depending on a product class name.",
                "runtime raft project state/catalog owner selected by stable IDs");

            Product(checks, "W20-E03.atomic_save_window_contract", "raft public contract", "P0",
                "Public raft state covers protected sailcloth, weather/current window, cancel/failure atomicity, duplicate suppression, and save/restore",
                delegate
                {
                    Require(ContainsAll(semanticSurface, "sailcloth", "weather", "current", "cancel", "failure", "duplicate", "save", "restore"),
                        "public raft semantic surface is incomplete: " + semanticSurface);
                    return semanticSurface;
                },
                "Enumerate public raft/stage/snapshot/result member names and stable values.",
                "runtime raft public data/state contract owner");

            Product(checks, "W20-E04.ko_en_qps_localization_surface", "localization data", "P1",
                "ko/en/qps-long contain raft launch, prompt, popup, hull, sail, supplies, weather/current, failure, cancel, and confirm rows",
                delegate
                {
                    string joined = string.Join("\n", localizationKeys);
                    Require(ContainsAll(joined, "raft", "launch", "prompt", "popup", "hull", "sail", "suppl", "weather", "current", "cancel", "confirm"),
                        "raft localization key surface incomplete; keys=" + string.Join(",", localizationKeys));
                    Require(LocalizationRowsHaveAllLocales(localizationKeys), "one or more raft localization rows lack ko/en/qps-long values");
                    return localizationKeys.Length + " raft-related rows: " + string.Join(",", localizationKeys);
                },
                "Parse the canonical localization TSV and inspect all three locale columns.",
                "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            Product(checks, "W20-E05.raft_ending_album_data_lock", "ending and album data", "P0",
                "A deterministic early escape.raft snapshot resolves the raft ending and its album unlock is idempotent/persistent",
                delegate
                {
                    PrototypeEndingResolution resolution = PrototypeEndingResolver.ResolveEndingDeterministicSingle(new PrototypeRunSnapshot
                    {
                        seed = 200020,
                        day = 20,
                        escape_id = RaftId,
                        result_code = "escape_complete"
                    });
                    endingResolution = resolution.StableId;
                    Require(resolution.StableId == RaftEndingId, "raft snapshot resolved " + resolution.StableId);
                    PrototypeEndingAlbumCollection album = PrototypeEndingAlbumCollection.CreateTransient();
                    bool first = album.UnlockForVerification(RaftEndingId, 20, "2026-08-25T00:00:00Z");
                    bool duplicate = album.UnlockForVerification(RaftEndingId, 20, "2026-08-25T00:00:00Z");
                    albumSnapshot = album.CaptureSnapshot();
                    PrototypeEndingAlbumCollection restored = PrototypeEndingAlbumCollection.CreateTransient(albumSnapshot);
                    Require(first && !duplicate && album.UnlockedCount == 1 && restored.UnlockedCount == 1 && restored.IsUnlocked(RaftEndingId),
                        "raft album unlock was not exactly-once across restore");
                    return "ending=" + resolution.StableId + "; first=" + first + "; duplicate=" + duplicate + "; restoredCount=" + restored.UnlockedCount;
                },
                "Resolve an early raft snapshot, unlock twice, serialize, and restore a transient album.",
                "runtime ending resolver/album data owners selected by stable IDs");

            WriteJson("wave20-edit-observation-evidence.json", new EditEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                definition = DescribeDefinition(definition),
                semanticSurface = semanticSurface,
                localizationKeys = localizationKeys,
                endingResolution = endingResolution,
                albumSnapshot = albumSnapshot
            });
            WriteReport("wave20-edit-contracts", "Wave 20 raft RED-first Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "Wave 20 Play runner did not complete");
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
            playTimeoutAt = EditorApplication.timeSinceStartup + 240d;
            if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 240d;
                if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
            }
            else if (state == PlayModeStateChange.EnteredEditMode) FinishPlayContracts();
        }

        private static void PlayTick()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup > playTimeoutAt)
            {
                WritePlayInfrastructureFailure(new TimeoutException("Wave 20 Play fixture timed out."));
                StopPlayContracts();
                return;
            }
            if (EditorApplication.timeSinceStartup < playEarliestRunTime) return;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }

            try
            {
                DateTime started = DateTime.UtcNow;
                KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
                if (prototype == null) throw new InvalidOperationException("No live KimSurvivalPrototype exists in the Play scene.");
                PlayEvidence evidence = ObserveLiveRaft(prototype);
                List<Check> checks = new List<Check>();

                Product(checks, "W20-P01.shore_launch_proximity_popup", "actual shore launch interaction", "P0",
                    "Far has zero prompt; near has exactly one compact shore-launch prompt; Interact opens its popup; Cancel restores the same prompt",
                    delegate
                    {
                        Require(evidence.launchTargetFound, LaunchId + " is absent from actual camp targets");
                        Require(evidence.farPromptCount == 0, "far prompt count=" + evidence.farPromptCount);
                        Require(evidence.nearPromptCount == 1, "near prompt count=" + evidence.nearPromptCount);
                        Require(evidence.popupOpened && evidence.promptHiddenWhilePopup && evidence.promptRestoredAfterCancel,
                            "popup/cancel states=" + evidence.popupOpened + "/" + evidence.promptHiddenWhilePopup + "/" + evidence.promptRestoredAfterCancel);
                        return "far/near=" + evidence.farPromptCount + "/" + evidence.nearPromptCount + "; popup/hide/restore=" +
                               evidence.popupOpened + "/" + evidence.promptHiddenWhilePopup + "/" + evidence.promptRestoredAfterCancel;
                    },
                    "Start the Play scene, walk outside/inside the shore-launch radius, Interact, then Cancel.",
                    "runtime camp target/UI owner selected by facility.shore-launch");

                Product(checks, "W20-P02.hull_sail_supplies_stages", "actual raft progression", "P0",
                    "A natural Play trace completes hull, sail, and voyage-supplies in order while sailcloth remains a protected key part",
                    delegate
                    {
                        string trace = TraceText(evidence.route);
                        Require(TokensInOrder(trace, "hull", "sail", "suppl"), "ordered hull/sail/supplies trace missing: " + trace);
                        Require(evidence.route != null && evidence.route.protectedKeyPartIds != null &&
                                evidence.route.protectedKeyPartIds.Contains(SailclothId),
                            "structured protected-key-part state does not contain " + SailclothId);
                        return trace;
                    },
                    "Progress the three raft stages only through the live shore-launch popup and inspect the resulting public trace/state.",
                    "runtime raft stage/inventory owners selected by stable IDs");

                Product(checks, "W20-P03.weather_current_launch_window", "actual launch window", "P0",
                    "Unsafe weather/current rejects launch without state loss; an allowed weather/current window permits launch",
                    delegate
                    {
                        string trace = TraceText(evidence.route);
                        Require(ContainsAll(trace, "weather", "current", "window"), "weather/current window trace missing: " + trace);
                        Require(evidence.route != null && evidence.route.unsafeWindowRejectedObserved && evidence.route.unsafeWindowRejected &&
                                evidence.route.allowedWindowLaunchedObserved && evidence.route.allowedWindowLaunched,
                            "structured unsafe-reject/allowed-launch results were not both true");
                        return trace;
                    },
                    "Observe one unsafe-window rejection and one allowed-window launch on the same prepared raft.",
                    "runtime forecast/current/raft-launch policy owners selected by stable IDs");

                Product(checks, "W20-P04.failure_cancel_atomicity", "raft transaction atomicity", "P0",
                    "Failure and Cancel preserve resources/stages; declared failure consequence applies at most once",
                    delegate
                    {
                        string trace = TraceText(evidence.route);
                        Require(evidence.route != null && evidence.route.cancelUnchangedObserved && evidence.route.cancelUnchanged,
                            "structured cancel-unchanged result missing/false");
                        Require(evidence.route.failureAtomicObserved && evidence.route.failureAtomic,
                            "structured failure-atomic result missing/false");
                        Require(evidence.route.failureApplicationsObserved && evidence.route.failureApplications == 1,
                            "failure applications expected 1, observed=" + evidence.route.failureApplications);
                        return trace;
                    },
                    "Snapshot resources/stages, Cancel and fail, retry the same event ID, then compare state/deltas.",
                    "runtime raft transaction/ledger owners selected by event and stage IDs");

                Product(checks, "W20-P05.duplicate_cost_terminal_zero", "raft idempotency", "P0",
                    "Successful stage costs commit once; duplicate Submit has zero cost delta and zero duplicate terminal",
                    delegate
                    {
                        string trace = TraceText(evidence.route);
                        Require(evidence.route != null && evidence.route.costCommitCountObserved && evidence.route.costCommitCount == 3,
                            "three stage cost commits expected, observed=" + evidence.route.costCommitCount);
                        Require(evidence.route.duplicateCostDeltaObserved && evidence.route.duplicateCostDelta == 0,
                            "duplicate cost delta missing/nonzero=" + evidence.route.duplicateCostDelta);
                        Require(evidence.route.duplicateTerminalDeltaObserved && evidence.route.duplicateTerminalDelta == 0,
                            "duplicate terminal delta missing/nonzero=" + evidence.route.duplicateTerminalDelta);
                        return trace;
                    },
                    "Submit each stage and terminal event twice with the same transaction ID and compare resource/terminal counters.",
                    "runtime raft transaction and terminal owners selected by stable event IDs");

                Product(checks, "W20-P06.early_escape_priority", "terminal priority", "P0",
                    "A natural raft completion before Day 50 terminates as escape.raft and is not overwritten by the Day 50 ending",
                    delegate
                    {
                        Require(evidence.route != null && evidence.route.success && evidence.route.completed && evidence.route.terminal, "live raft route did not complete/terminate");
                        Require(evidence.route.escapeId == RaftId, "route escaped as " + evidence.route.escapeId);
                        string trace = TraceText(evidence.route);
                        bool early = evidence.route.day > 0 ? evidence.route.day < GameSession.FinalDay :
                            evidence.route.earlyEscapeObserved && evidence.route.earlyEscape;
                        Require(early, "pre-Day50 completion was not observed");
                        Require(evidence.route.endingId == RaftEndingId || trace.Contains(RaftEndingId), "raft ending priority evidence missing");
                        return trace;
                    },
                    "Complete the route naturally before Day 50, then attempt Day 50 resolution and retain the first terminal ID.",
                    "runtime terminal/ending resolver owner selected by escape.raft");

                Product(checks, "W20-P07.save_restore_progress", "raft persistence", "P0",
                    "Save/restore preserves raft stage order, resources, protected sailcloth, window state, and terminal idempotency",
                    delegate
                    {
                        string trace = TraceText(evidence.route);
                        Require(evidence.route != null && evidence.route.restoreSameObserved && evidence.route.restoreSame,
                            "structured save/restore equality result missing/false");
                        string stages = string.Join(" | ", evidence.route.restoredStageIds ?? Array.Empty<string>()).ToLowerInvariant();
                        Require(TokensInOrder(stages, "hull", "sail", "suppl"), "restored ordered stage IDs missing: " + stages);
                        Require(evidence.route.protectedKeyPartIds != null && evidence.route.protectedKeyPartIds.Contains(SailclothId),
                            "restored protected sailcloth state missing");
                        return trace;
                    },
                    "Save after each stage, recreate the live runtime, restore, and compare stable state plus transaction IDs.",
                    "runtime snapshot/save owner selected by stable raft IDs");

                Product(checks, "W20-P08.ending_album_unlock_once", "raft ending collection", "P0",
                    "Natural escape.raft completion unlocks ending.escape.raft.open-water exactly once and survives restore",
                    delegate
                    {
                        string trace = TraceText(evidence.route);
                        Require(evidence.route != null && (evidence.route.endingId == RaftEndingId || trace.Contains(RaftEndingId)), "raft ending not observed");
                        Require(evidence.route.albumUnlockDeltaObserved && evidence.route.albumUnlockDelta == 1,
                            "first album unlock delta missing/not one=" + evidence.route.albumUnlockDelta);
                        Require(evidence.route.duplicateAlbumDeltaObserved && evidence.route.duplicateAlbumDelta == 0,
                            "duplicate album unlock delta missing/nonzero=" + evidence.route.duplicateAlbumDelta);
                        Require(evidence.route.albumRestoredObserved && evidence.route.albumRestored,
                            "structured album restore result missing/false");
                        return trace;
                    },
                    "Complete raft escape, retry terminal, serialize the album, and restore it.",
                    "runtime ending album owner selected by ending.escape.raft.open-water");

                Product(checks, "W20-P09.natural_trace_no_cheats", "natural interaction trace", "P0",
                    "Actual Play returns escape.raft with interaction trace and grant=false, warp=false, skip=false",
                    delegate
                    {
                        Require(evidence.route != null, "no live route result was observed");
                        Require(evidence.route.escapeId == RaftId && evidence.route.stableId.Contains("raft"), "route identity mismatch: " + TraceText(evidence.route));
                        Require(evidence.route.success && evidence.route.completed && evidence.route.terminal && evidence.route.interactionCount > 0,
                            "route success/completion/terminal/interactions mismatch: " + TraceText(evidence.route));
                        Require(!evidence.route.grant && !evidence.route.warp && evidence.route.skipObserved && !evidence.route.skip,
                            "cheat flags grant/warp/skip=" + evidence.route.grant + "/" + evidence.route.warp + "/" + evidence.route.skip + " observedSkip=" + evidence.route.skipObserved);
                        Require(evidence.route.interactionTrace != null && evidence.route.interactionTrace.Length >= 6, "natural interaction trace is too short");
                        return TraceText(evidence.route);
                    },
                    "Invoke the live public natural-route observation with escape.raft and independently inspect its fields/trace.",
                    "runtime Play route observer discovered by public result shape");

                Product(checks, "W20-P10.ko_en_qps_1280_layout", "localized raft UI", "P1",
                    "ko/en/qps-long 1280x800 near/popup captures remain onscreen with zero TMP overflow; prompt is at most 512x50",
                    delegate
                    {
                        Require(evidence.layouts != null && evidence.layouts.Length == 6, "expected six locale/state layout samples, observed " + (evidence.layouts == null ? 0 : evidence.layouts.Length));
                        LayoutEvidence[] failures = evidence.layouts.Where(value => value.result != "PASS").ToArray();
                        Require(failures.Length == 0, "layout failures: " + string.Join(" | ", failures.Select(value => value.locale + "/" + value.state + ":" + value.failureReason).ToArray()));
                        return string.Join(" | ", evidence.layouts.Select(DescribeLayout).ToArray());
                    },
                    "Capture the actual near prompt and popup at 1280x800 in ko, en, and qps-long and inspect active TMP/RectTransforms.",
                    "runtime raft prompt/popup/localization owners selected by live hierarchy");

                Product(checks, "W20-P11.keyboard_mouse_synthetic_gamepad_parity", "input parity", "P1",
                    "Keyboard/mouse and synthetic gamepad show different glyphs but the same shore-launch target/action meaning",
                    delegate
                    {
                        Require(evidence.keyboardMouseSyntheticGamepadParity, "input meaning mismatch: keyboard=" + evidence.keyboardActionText + "; gamepad=" + evidence.gamepadActionText);
                        return "keyboard=" + evidence.keyboardActionText + "; gamepad=" + evidence.gamepadActionText;
                    },
                    "Render the same live shore-launch prompt through keyboard/mouse and synthetic gamepad presentation paths.",
                    "runtime input prompt owner selected by facility.shore-launch");

                Unverified(checks, "W20-U01.physical_gamepad", "manual hardware", "P1",
                    "A human completes the full raft path on a connected physical gamepad",
                    "Unity joystick names: " + string.Join(" | ", evidence.joystickNames ?? Array.Empty<string>()),
                    "Repeat the route on Windows with a physical controller and retain human evidence.",
                    "manual playtest evidence");
                NotReady(checks, "W20-U02.steam_release", "external release", "P0",
                    "Steamworks App ID/depot/Input/Cloud/achievements/partner permissions have approved evidence",
                    "No Steam partner evidence is part of Wave 20.",
                    "Complete the separately approved Steam release workflow.",
                    "external Steam partner configuration");

                WriteJson("wave20-play-observation-evidence.json", evidence);
                Report report = WriteReport("wave20-play-contracts", "Wave 20 raft RED-first actual Play contracts", started, checks);
                SessionState.SetBool(PlayExitPassKey, report.infrastructureOverall == "PASS");
                SessionState.SetString(PlayMessageKey, report.overall + " · Wave 20 Play evidence completed");
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static PlayEvidence ObserveLiveRaft(KimSurvivalPrototype prototype)
        {
            PlayEvidence evidence = new PlayEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                scene = ScenePath,
                discoveryPolicy = "Stable facility/escape/part/ending IDs, active camp target list, actual prompt/popup objects, public live route result fields and interaction trace; assertion strings alone do not satisfy the gate.",
                route = ObserveNaturalRoute(),
                layouts = Array.Empty<LayoutEvidence>(),
                joystickNames = Input.GetJoystickNames() ?? Array.Empty<string>()
            };
            List<LayoutEvidence> layouts = new List<LayoutEvidence>();
            try
            {
                prototype.Session.Reset();
                InvokePrivate(prototype, "RefreshAll");
                PrototypeSearchNodePlayObservation discovery = prototype.CaptureSearchNodeVerificationObservation();
                Require(discovery != null && string.IsNullOrEmpty(discovery.ObservationError) &&
                        discovery.ActualNodeObserved && discovery.DepletedObserved && discovery.SailclothLinked &&
                        !discovery.Grant && !discovery.Warp && !discovery.Skip,
                    "shore-launch observation could not complete an actual protected-part search: " +
                    (discovery == null ? "missing observation" : discovery.ObservationError));
                Require(prototype.Session.ReturnToCamp(false),
                    "shore-launch observation could not return from the actual protected-part search");
                InvokePrivate(prototype, "RefreshAll");
                InvokePrivate(prototype, "RefreshCampInteractionSelection");
                IEnumerable targets = GetField(prototype, "campInteractionTargets") as IEnumerable;
                List<object> targetList = targets == null ? new List<object>() : targets.Cast<object>().ToList();
                evidence.targetIds = targetList.Select(value => ReadString(value, "Id")).Where(value => !string.IsNullOrEmpty(value)).ToArray();
                object launch = targetList.FirstOrDefault(value => ReadString(value, "Id") == LaunchId);
                evidence.launchTargetFound = launch != null;
                if (launch == null)
                {
                    evidence.observationError = LaunchId + " is not present in the active camp target list";
                    evidence.layouts = layouts.ToArray();
                    return evidence;
                }

                Vector2 position = ReadVector2(launch, "Position");
                object kind = GetMember(launch, "Kind");
                object campUse = GetField(prototype, "campUse");
                PrototypeCampUse typedCampUse = campUse as PrototypeCampUse;
                object interaction = GetField(prototype, "campInteraction");
                GameObject prompt = GetField(prototype, "campProximityPrompt") as GameObject;
                GameObject popup = GetField(prototype, "campInteractionPopup") as GameObject;
                Require(typedCampUse != null && interaction != null && prompt != null && popup != null, "camp interaction runtime objects missing");

                typedCampUse.Warp(position + Vector2.right * (PrototypeCampUse.UseRange + 0.5f));
                InvokePrivate(prototype, "RefreshAll");
                evidence.farPromptCount = prompt.activeSelf && ReadString(interaction, "ActiveTargetId") == LaunchId ? 1 : 0;

                typedCampUse.Warp(position + Vector2.left * 0.35f);
                InvokePrivate(prototype, "RefreshAll");
                string activeId = ReadString(interaction, "ActiveTargetId");
                evidence.nearPromptCount = prompt.activeSelf && activeId == LaunchId ? 1 : 0;

                string[] locales = { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode, PrototypeLocalization.QpsLongLocaleCode };
                PrototypeLocalization localization = GetField(prototype, "localization") as PrototypeLocalization;
                TMP_Text actionText = GetField(prototype, "campProximityText") as TMP_Text;
                TMP_Text glyphText = GetField(prototype, "campProximityGlyphText") as TMP_Text;
                Require(localization != null && actionText != null && glyphText != null, "localization/prompt TMP objects missing");

                InvokePrivate(prototype, "ApplyCampProximityPresentation", kind, LaunchId, PrototypeInputDevice.KeyboardMouse);
                string keyboardAction = actionText.text;
                string keyboardGlyph = glyphText.text;
                InvokePrivate(prototype, "ApplyCampProximityPresentation", kind, LaunchId, PrototypeInputDevice.Gamepad);
                string gamepadAction = actionText.text;
                string gamepadGlyph = glyphText.text;
                evidence.keyboardActionText = keyboardGlyph + " " + keyboardAction;
                evidence.gamepadActionText = gamepadGlyph + " " + gamepadAction;
                evidence.keyboardMouseSyntheticGamepadParity = !string.IsNullOrWhiteSpace(keyboardAction) && keyboardAction == gamepadAction &&
                    !string.IsNullOrWhiteSpace(keyboardGlyph) && !string.IsNullOrWhiteSpace(gamepadGlyph) && keyboardGlyph != gamepadGlyph;

                for (int index = 0; index < locales.Length; index += 1)
                {
                    string locale = locales[index];
                    SetLocale(localization, locale);
                    typedCampUse.Warp(position + Vector2.left * 0.35f);
                    InvokePrivate(prototype, "RefreshAll");
                    string nearFile = "wave20-raft-near-" + locale + "-1280x800.png";
                    prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, nearFile), 1280, 800);
                    layouts.Add(MeasureLayout(prototype, locale, "near", nearFile, prompt, true));

                    InvokePrivate(prototype, "UseNearestCampTarget");
                    InvokePrivate(prototype, "RefreshAll");
                    evidence.popupOpened = popup.activeSelf && ReadString(interaction, "OpenPopupTargetId") == LaunchId;
                    evidence.promptHiddenWhilePopup = !prompt.activeSelf;
                    string popupFile = "wave20-raft-popup-" + locale + "-1280x800.png";
                    prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, popupFile), 1280, 800);
                    layouts.Add(MeasureLayout(prototype, locale, "popup", popupFile, popup, false));
                    InvokePrivate(prototype, "CancelCampPopup");
                    InvokePrivate(prototype, "RefreshAll");
                    evidence.promptRestoredAfterCancel = prompt.activeSelf && ReadString(interaction, "ActiveTargetId") == LaunchId;
                }
            }
            catch (Exception exception)
            {
                evidence.observationError = exception.GetType().Name + ": " + exception.Message;
            }
            evidence.layouts = layouts.ToArray();
            if (evidence.targetIds == null) evidence.targetIds = Array.Empty<string>();
            return evidence;
        }

        private static RouteEvidence ObserveNaturalRoute()
        {
            RouteEvidence evidence = new RouteEvidence { interactionTrace = Array.Empty<string>() };
            MonoBehaviour runtime = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .FirstOrDefault(value => value != null && value.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Any(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string) &&
                                   method.ReturnType != typeof(void) && HasRouteResultShape(method.ReturnType)));
            if (runtime == null)
            {
                evidence.resultCode = "live route observer not found";
                return evidence;
            }
            MethodInfo routeMethod = runtime.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string) &&
                                 method.ReturnType != typeof(void) && HasRouteResultShape(method.ReturnType))
                .OrderByDescending(method => method.Name.IndexOf("Observe", StringComparison.OrdinalIgnoreCase) >= 0)
                .First();
            object result;
            try { result = routeMethod.Invoke(runtime, new object[] { RaftId }); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
            evidence.runtimeType = runtime.GetType().FullName + "." + routeMethod.Name;
            evidence.stableId = ReadString(result, "StableId");
            evidence.escapeId = ReadString(result, "EscapeId");
            evidence.endingId = FirstNonEmpty(ReadString(result, "EndingId"), ReadString(result, "ending_id"));
            evidence.resultCode = ReadString(result, "ResultCode");
            evidence.success = ReadBool(result, "Success");
            evidence.completed = ReadBool(result, "Completed");
            evidence.terminal = ReadBool(result, "Terminal");
            evidence.grant = ReadBool(result, "Grant");
            evidence.warp = ReadBool(result, "Warp");
            evidence.interactionCount = ReadInt(result, "InteractionCount");
            evidence.day = FirstNonZero(ReadInt(result, "Day"), ReadInt(result, "CompletionDay"));
            evidence.interactionTrace = ReadStringArray(result, "InteractionTrace");
            evidence.unsafeWindowRejected = ReadOptionalBool(result, out evidence.unsafeWindowRejectedObserved,
                "UnsafeWindowRejected", "unsafe_window_rejected");
            evidence.allowedWindowLaunched = ReadOptionalBool(result, out evidence.allowedWindowLaunchedObserved,
                "AllowedWindowLaunched", "allowed_window_launched");
            evidence.cancelUnchanged = ReadOptionalBool(result, out evidence.cancelUnchangedObserved,
                "CancelUnchanged", "cancel_unchanged");
            evidence.failureAtomic = ReadOptionalBool(result, out evidence.failureAtomicObserved,
                "FailureAtomic", "failure_atomic");
            evidence.failureApplications = ReadOptionalInt(result, out evidence.failureApplicationsObserved,
                "FailureApplications", "failure_applications");
            evidence.costCommitCount = ReadOptionalInt(result, out evidence.costCommitCountObserved,
                "CostCommitCount", "cost_commit_count");
            evidence.duplicateCostDelta = ReadOptionalInt(result, out evidence.duplicateCostDeltaObserved,
                "DuplicateCostDelta", "duplicate_cost_delta");
            evidence.duplicateTerminalDelta = ReadOptionalInt(result, out evidence.duplicateTerminalDeltaObserved,
                "DuplicateTerminalDelta", "duplicate_terminal_delta");
            evidence.earlyEscape = ReadOptionalBool(result, out evidence.earlyEscapeObserved,
                "EarlyEscape", "early_escape");
            evidence.restoreSame = ReadOptionalBool(result, out evidence.restoreSameObserved,
                "RestoreSame", "restore_same", "SaveRestoreEqual");
            evidence.albumUnlockDelta = ReadOptionalInt(result, out evidence.albumUnlockDeltaObserved,
                "AlbumUnlockDelta", "album_unlock_delta");
            evidence.duplicateAlbumDelta = ReadOptionalInt(result, out evidence.duplicateAlbumDeltaObserved,
                "DuplicateAlbumDelta", "duplicate_album_delta");
            evidence.albumRestored = ReadOptionalBool(result, out evidence.albumRestoredObserved,
                "AlbumRestored", "album_restored");
            evidence.protectedKeyPartIds = ReadFirstStringArray(result, "ProtectedKeyPartIds", "protected_key_part_ids");
            evidence.restoredStageIds = ReadFirstStringArray(result, "RestoredStageIds", "restored_stage_ids", "CompletedStageIds");
            object skip = GetMember(result, "Skip");
            evidence.skipObserved = skip != null;
            evidence.skip = skip is bool && (bool)skip;
            string trace = string.Join(" | ", evidence.interactionTrace ?? Array.Empty<string>()).ToLowerInvariant();
            if (!evidence.skipObserved && (trace.Contains("skip=false") || trace.Contains("skip=0"))) evidence.skipObserved = true;
            evidence.publicMembers = DescribeObject(result);
            return evidence;
        }

        private static bool HasRouteResultShape(Type type)
        {
            return HasMember(type, "InteractionTrace") && HasMember(type, "EscapeId") && HasMember(type, "Terminal") && HasMember(type, "Grant") && HasMember(type, "Warp");
        }

        private static LayoutEvidence MeasureLayout(KimSurvivalPrototype prototype, string locale, string state, string screenshot, GameObject target, bool compactPrompt)
        {
            Camera camera = GetField(prototype, "worldCamera") as Camera;
            Require(camera != null && target != null && target.activeSelf, state + " UI target is not active");
            Canvas.ForceUpdateCanvases();
            TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true).Where(value => value.gameObject.activeInHierarchy).ToArray();
            int overflow = 0;
            int offscreen = 0;
            foreach (TMP_Text text in texts)
            {
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing) overflow += 1;
                if (!InsideScreen(RectTransformPixels(text.rectTransform, camera), 1f)) offscreen += 1;
            }
            RectTransform transform = target.GetComponent<RectTransform>();
            Require(transform != null, state + " RectTransform missing");
            PixelRect rect = RectTransformPixels(transform, camera);
            bool inside = InsideScreen(rect, 1f);
            bool compact = !compactPrompt || (rect.Width <= 512.01f && rect.Height <= 50.01f);
            List<string> failures = new List<string>();
            if (!inside) failures.Add("surface offscreen");
            if (texts.Length == 0) failures.Add("no active TMP");
            if (overflow != 0) failures.Add("overflow=" + overflow);
            if (offscreen != 0) failures.Add("offscreenText=" + offscreen);
            if (!compact) failures.Add("prompt=" + rect.Width.ToString("0.0") + "x" + rect.Height.ToString("0.0") + ">512x50");
            return new LayoutEvidence
            {
                locale = locale,
                state = state,
                screenshot = screenshot,
                rect = rect.Evidence(),
                activeTextCount = texts.Length,
                overflowCount = overflow,
                offscreenTextCount = offscreen,
                insideScreen = inside,
                compactPrompt = compact,
                result = failures.Count == 0 ? "PASS" : "FAIL",
                failureReason = string.Join("; ", failures)
            };
        }

        private static PixelRect RectTransformPixels(RectTransform rect, Camera camera)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 first = camera.WorldToViewportPoint(corners[0]);
            float minX = first.x * CaptureWidth;
            float maxX = minX;
            float minY = first.y * CaptureHeight;
            float maxY = minY;
            for (int index = 1; index < corners.Length; index += 1)
            {
                Vector3 viewport = camera.WorldToViewportPoint(corners[index]);
                minX = Mathf.Min(minX, viewport.x * CaptureWidth);
                maxX = Mathf.Max(maxX, viewport.x * CaptureWidth);
                minY = Mathf.Min(minY, viewport.y * CaptureHeight);
                maxY = Mathf.Max(maxY, viewport.y * CaptureHeight);
            }
            return new PixelRect { X = minX, Y = minY, Width = maxX - minX, Height = maxY - minY };
        }

        private static bool InsideScreen(PixelRect rect, float tolerance)
        {
            return rect.Width > 0f && rect.Height > 0f && rect.X >= -tolerance && rect.Y >= -tolerance &&
                   rect.Right <= CaptureWidth + tolerance && rect.Top <= CaptureHeight + tolerance;
        }

        private static string DescribeLayout(LayoutEvidence value)
        {
            return value.locale + "/" + value.state + "=" + value.rect.x.ToString("0.0") + "," + value.rect.y.ToString("0.0") + "," +
                   value.rect.width.ToString("0.0") + "x" + value.rect.height.ToString("0.0") + "; overflow=" + value.overflowCount +
                   "; offscreen=" + value.offscreenTextCount;
        }

        private static void SetLocale(PrototypeLocalization localization, string locale)
        {
            bool changed = locale == PrototypeLocalization.QpsLongLocaleCode ? localization.SetQaLocale() : localization.SetLocale(locale, false);
            Require(changed && localization.CurrentLocaleCode == locale, "locale did not activate: " + locale);
        }

        private static string[] ReadRaftLocalizationKeys()
        {
            string path = Path.Combine(Application.dataPath, "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            if (!File.Exists(path)) return Array.Empty<string>();
            return File.ReadAllLines(path, Encoding.UTF8)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
                .Select(line => line.Split('\t'))
                .Where(columns => columns.Length >= 4 && IsRaftLocalizationKey(columns[0]))
                .Select(columns => string.Join("\t", columns))
                .ToArray();
        }

        private static bool IsRaftLocalizationKey(string key)
        {
            string value = (key ?? string.Empty).Trim().ToLowerInvariant();
            return value == RaftId || value.StartsWith("raft.", StringComparison.Ordinal) ||
                   value.Contains(".raft.") || value.EndsWith(".raft", StringComparison.Ordinal) ||
                   value.Contains("shore-launch") || value.Contains("shore.launch");
        }

        private static bool LocalizationRowsHaveAllLocales(IEnumerable<string> rows)
        {
            string[] values = rows.ToArray();
            return values.Length > 0 && values.All(row => row.Split('\t').Length >= 4 && row.Split('\t').Skip(1).Take(3).All(value => !string.IsNullOrWhiteSpace(value)));
        }

        private static string DescribeDefinition(PrototypeEscapeProjectDefinition definition)
        {
            if (definition == null) return "missing";
            return definition.StableId + " regions=" + string.Join(",", definition.RegionIds) + " research=" + string.Join(",", definition.ResearchIds) +
                   " facility=" + definition.FacilityId + " keyPart=" + definition.KeyPartId + " materials=" + string.Join(",", definition.MaterialIds) +
                   " risks=" + string.Join(",", definition.RiskIds) + " timing=" + definition.TimingRule + " completion=" + definition.CompletionRule +
                   " playableState=" + definition.PlayableState + " requiredProgress=" + definition.RequiredProgress;
        }

        private static string DescribeSemanticSurface(object definition)
        {
            Assembly assembly = typeof(PrototypeEscapeProjectDefinition).Assembly;
            List<string> parts = new List<string> { DescribeObject(definition) };
            foreach (Type type in assembly.GetTypes().Where(type => type.Name.IndexOf("Raft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                                  type == typeof(PrototypeEscapeProjectState) ||
                                                                  type == typeof(PrototypeRunSnapshot) ||
                                                                  type == typeof(PrototypeNaturalEscapeRouteResult)))
            {
                string members = string.Join(",", type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
                    .Where(member => member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property)
                    .Select(member => member.Name).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray());
                parts.Add(type.FullName + "{" + members + "}");
            }
            return string.Join(" | ", parts).ToLowerInvariant();
        }

        private static string DescribeObject(object value)
        {
            if (value == null) return "null";
            List<string> members = new List<string>();
            foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.GetIndexParameters().Length == 0))
            {
                try { members.Add(property.Name + "=" + DescribeValue(property.GetValue(value, null))); } catch { }
            }
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                try { members.Add(field.Name + "=" + DescribeValue(field.GetValue(value))); } catch { }
            }
            return value.GetType().FullName + "{" + string.Join(";", members.ToArray()) + "}";
        }

        private static string DescribeValue(object value)
        {
            if (value == null) return "null";
            if (value is string) return (string)value;
            if (value is IEnumerable && !(value is string))
            {
                List<string> items = new List<string>();
                foreach (object item in (IEnumerable)value) items.Add(item == null ? "null" : item.ToString());
                return "[" + string.Join(",", items.ToArray()) + "]";
            }
            return value.ToString();
        }

        private static string TraceText(RouteEvidence route)
        {
            if (route == null) return "route=null";
            return (route.publicMembers + " | trace=" + string.Join(" | ", route.interactionTrace ?? Array.Empty<string>())).ToLowerInvariant();
        }

        private static bool TokensInOrder(string value, params string[] tokens)
        {
            string text = (value ?? string.Empty).ToLowerInvariant();
            int cursor = -1;
            for (int index = 0; index < tokens.Length; index += 1)
            {
                cursor = text.IndexOf(tokens[index].ToLowerInvariant(), cursor + 1, StringComparison.Ordinal);
                if (cursor < 0) return false;
            }
            return true;
        }

        private static bool ContainsAll(string value, params string[] tokens)
        {
            string text = (value ?? string.Empty).ToLowerInvariant();
            return tokens.All(token => text.Contains(token.ToLowerInvariant()));
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            string text = (value ?? string.Empty).ToLowerInvariant();
            return tokens.Any(token => text.Contains(token.ToLowerInvariant()));
        }

        private static object GetField(object owner, string fieldName)
        {
            if (owner == null) return null;
            FieldInfo field = owner.GetType().GetField(fieldName, AllInstance);
            return field == null ? null : field.GetValue(owner);
        }

        private static object GetMember(object owner, string memberName)
        {
            if (owner == null) return null;
            PropertyInfo property = owner.GetType().GetProperty(memberName, AllInstance);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(owner, null);
            FieldInfo field = owner.GetType().GetField(memberName, AllInstance);
            return field == null ? null : field.GetValue(owner);
        }

        private static bool HasMember(Type type, string memberName)
        {
            return type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public) != null ||
                   type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public) != null;
        }

        private static string ReadString(object owner, string memberName)
        {
            object value = GetMember(owner, memberName);
            return value == null ? string.Empty : value.ToString();
        }

        private static bool ReadBool(object owner, string memberName)
        {
            object value = GetMember(owner, memberName);
            return value is bool && (bool)value;
        }

        private static int ReadInt(object owner, string memberName)
        {
            object value = GetMember(owner, memberName);
            if (value is int) return (int)value;
            int parsed;
            return value != null && int.TryParse(value.ToString(), out parsed) ? parsed : 0;
        }

        private static string[] ReadStringArray(object owner, string memberName)
        {
            object value = GetMember(owner, memberName);
            if (value is string[]) return (string[])value;
            IEnumerable enumerable = value as IEnumerable;
            return enumerable == null ? Array.Empty<string>() : enumerable.Cast<object>().Select(item => item == null ? string.Empty : item.ToString()).ToArray();
        }

        private static string[] ReadFirstStringArray(object owner, params string[] memberNames)
        {
            foreach (string memberName in memberNames)
            {
                if (GetMember(owner, memberName) != null) return ReadStringArray(owner, memberName);
            }
            return Array.Empty<string>();
        }

        private static bool ReadOptionalBool(object owner, out bool observed, params string[] memberNames)
        {
            foreach (string memberName in memberNames)
            {
                object value = GetMember(owner, memberName);
                if (value is bool)
                {
                    observed = true;
                    return (bool)value;
                }
            }
            observed = false;
            return false;
        }

        private static int ReadOptionalInt(object owner, out bool observed, params string[] memberNames)
        {
            foreach (string memberName in memberNames)
            {
                object value = GetMember(owner, memberName);
                if (value == null) continue;
                if (value is int)
                {
                    observed = true;
                    return (int)value;
                }
                int parsed;
                if (int.TryParse(value.ToString(), out parsed))
                {
                    observed = true;
                    return parsed;
                }
            }
            observed = false;
            return 0;
        }

        private static Vector2 ReadVector2(object owner, string memberName)
        {
            object value = GetMember(owner, memberName);
            Require(value is Vector2, memberName + " is not Vector2");
            return (Vector2)value;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static int FirstNonZero(params int[] values)
        {
            return values.FirstOrDefault(value => value != 0);
        }

        private static void InvokePrivate(object owner, string methodName, params object[] arguments)
        {
            Invoke(owner, methodName, BindingFlags.Instance | BindingFlags.NonPublic, arguments);
        }

        private static void InvokePublic(object owner, string methodName, params object[] arguments)
        {
            Invoke(owner, methodName, BindingFlags.Instance | BindingFlags.Public, arguments);
        }

        private static object Invoke(object owner, string methodName, BindingFlags flags, object[] arguments)
        {
            MethodInfo method = owner.GetType().GetMethods(flags).FirstOrDefault(value => value.Name == methodName && value.GetParameters().Length == arguments.Length);
            Require(method != null, owner.GetType().Name + "." + methodName + " is missing");
            try { return method.Invoke(owner, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static void Product(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> audit, string reproduction, string files)
        {
            try { checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, audit(), reproduction, files)); }
            catch (Exception exception)
            {
                string status = IsRedBaseline ? "EXPECTED_GAP" : "FAIL";
                string classification = IsRedBaseline ? "PRODUCT_EXPECTED_GAP" : "PRODUCT_REGRESSION";
                checks.Add(NewCheck(id, matrix, status, classification, severity, expected, exception.GetType().Name + ": " + exception.Message, reproduction, files));
            }
        }

        private static void Infrastructure(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> audit, string reproduction, string files)
        {
            try { checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, audit(), reproduction, files)); }
            catch (Exception exception)
            {
                checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected,
                    exception.GetType().Name + ": " + exception.Message, reproduction, files));
            }
        }

        private static void Unverified(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", "HARDWARE_GAP", severity, expected, actual, reproduction, files));
        }

        private static void NotReady(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "NOT_READY", "EXTERNAL_RELEASE_GAP", severity, expected, actual, reproduction, files));
        }

        private static Check NewCheck(string id, string matrix, string status, string classification, string severity,
            string expected, string actual, string reproduction, string files)
        {
            return new Check { id = id, matrix = matrix, status = status, classification = classification, severity = severity,
                expected = expected, actual = actual, reproduction = reproduction, recommendedFiles = files };
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
                passed = checks.Count(check => check.status == "PASS"),
                expectedGaps = checks.Count(check => check.status == "EXPECTED_GAP"),
                productFailed = checks.Count(check => check.status == "FAIL"),
                infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"),
                notReady = checks.Count(check => check.status == "NOT_READY"),
                greenCompletionCondition = "Fresh current Wave 19 21/21 and all Wave 20 checks pass with infrastructure PASS; the exact legacy raft-data-only and removed-world-badge locks may be explicitly superseded by the current contract.",
                checks = checks.ToArray()
            };
            report.productOverall = report.productFailed > 0 ? "FAIL" : report.expectedGaps > 0 ? "RED_EXPECTED_GAP" : "PASS";
            report.infrastructureOverall = report.infrastructureFailed > 0 ? "FAIL" : "PASS";
            report.overall = report.infrastructureOverall == "FAIL" || report.productOverall == "FAIL" ? "FAIL" :
                report.productOverall == "RED_EXPECTED_GAP" ? "RED" : "GREEN";
            WriteJson(stem + ".json", report);
            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Unity: " + Application.unityVersion);
            text.AppendLine("Overall/Product/Infrastructure: " + report.overall + "/" + report.productOverall + "/" + report.infrastructureOverall);
            text.AppendLine("PASS/EXPECTED_GAP/FAIL/INFRA_FAIL/UNVERIFIED/NOT_READY: " + report.passed + "/" + report.expectedGaps + "/" + report.productFailed + "/" + report.infrastructureFailed + "/" + report.unverified + "/" + report.notReady);
            foreach (Check check in checks) text.AppendLine(check.id + " | " + check.status + " | " + check.classification + " | " + check.actual);
            File.WriteAllText(Path.Combine(EvidenceFolder, stem + ".txt"), text.ToString(), Utf8NoBom);
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
                NewCheck("W20-I99.play_runner", "Play infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "The Wave 20 Play runner emits parseable evidence", exception.ToString(),
                    "Run Invoke-Wave20RaftRedFirstGate.ps1 outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave20RaftRedFirstGateRunner.cs")
            };
            WriteReport("wave20-play-contracts", "Wave 20 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: " + exception);
        }

        private static void StopPlayContracts() { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; }

        private static void FinishPlayContracts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }
            bool passed = SessionState.GetBool(PlayExitPassKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing Wave 20 Play result");
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayExitPassKey);
            SessionState.EraseString(PlayMessageKey);
            Debug.Log("[ParallelQA] " + message);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }
    }
}
