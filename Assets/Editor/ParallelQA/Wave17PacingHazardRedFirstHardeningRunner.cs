using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using KimSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParallelQA
{
    /// <summary>
    /// Independent Wave 17 RED-first hardening gate. Product discovery uses
    /// stable IDs, public data, deterministic semantic probes, and live state.
    /// No recommended product class or file name is required.
    /// </summary>
    public static class Wave17PacingHazardRedFirstHardeningRunner
    {
        private const string RedBaseline = "a5403173f299abc71ed4724bdaaf30c31ce8cc94";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave17.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave17.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave17.PlayMessage";
        private static readonly BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private static readonly BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        private static readonly BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly Regex StableId = new Regex(
            "^(pacing|region|hazard|escape|ending|stat|event|modifier|part|facility|research|equipment|discovery|smoke)\\.[a-z0-9][a-z0-9._-]*$",
            RegexOptions.Compiled);

        private static readonly string[] RequiredBands =
        {
            "pacing.band.onboarding", "pacing.band.expansion", "pacing.band.compound-choice",
            "pacing.band.finish-pressure", "pacing.band.resolution"
        };

        private static readonly int[] RequiredBoundaryDays = { 1, 10, 11, 20, 21, 35, 36, 49, 50 };
        private static readonly string[] RequiredRegions =
        {
            "region.coast.beach", "region.forest.grove", "region.sea.shallows",
            "region.ridge.highland", "region.cove.wreck", "region.ruins.relay"
        };

        private static readonly string[] ExpansionRegions =
        {
            "region.ridge.highland", "region.cove.wreck", "region.ruins.relay"
        };

        private static readonly string[] RequiredHazards =
        {
            "hazard.injury", "hazard.disaster", "hazard.food-theft"
        };

        private static readonly string[] RequiredEscapes =
        {
            "escape.raft", "escape.smoke", "escape.radio", "escape.flare", "escape.beacon"
        };

        private static readonly string[] RequiredSamples =
        {
            "ending.escape.smoke.seen-from-afar", "ending.escape.radio.clear-signal",
            "ending.comic.radio.island-dj", "ending.stay.just-kim"
        };

        private static readonly string[] RequiredEndings =
        {
            "ending.escape.raft.open-water", "ending.escape.smoke.seen-from-afar", "ending.escape.radio.clear-signal",
            "ending.escape.flare.one-shot", "ending.escape.beacon.ridge-light", "ending.comic.raft.coconut-navy",
            "ending.comic.smoke.island-barbecue", "ending.comic.radio.island-dj", "ending.comic.flare.daylight-fireworks",
            "ending.comic.beacon.brightest-address", "ending.rare.raft.current-reader", "ending.rare.smoke.cloud-letter",
            "ending.rare.radio.forecast-rescue", "ending.rare.beacon.storm-eye", "ending.stay.green-king",
            "ending.stay.fortress-manager", "ending.stay.scrap-professor", "ending.stay.island-ranger", "ending.stay.just-kim"
        };

        private static readonly ReviewAsset[] ReviewAssets =
        {
            new ReviewAsset(
                "effect.survival-hazards.phase-silhouette-a", "job_20260823160305_ef04b0f3",
                "Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png",
                "Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-manifest.json"),
            new ReviewAsset(
                "ui.escape-project-progress.route-signature-a", "job_20260823160324_1de3b748",
                "Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-route-signature-a-1280x800.png",
                "Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-manifest.json"),
            new ReviewAsset(
                "ui.ending-comic.triptych-a", "job_20260823160342_eceb3933",
                "Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png",
                "Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-manifest.json")
        };

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
            public string greenCompletionCondition;
            public string physicalGamepad = "UNVERIFIED";
            public string steamReadiness = "NOT_READY";
            public Check[] checks;
        }

        [Serializable]
        private sealed class MemberSnapshot
        {
            public string name;
            public string value;
        }

        [Serializable]
        private sealed class ContractEntry
        {
            public string id;
            public string type;
            public MemberSnapshot[] members;

            public string Describe()
            {
                return type + "{" + string.Join(";", members.Select(member => member.name + "=" + member.value).ToArray()) + "}";
            }
        }

        [Serializable]
        private sealed class EditEvidence
        {
            public string runId;
            public string baselineCommit;
            public string discoveryPolicy;
            public string[] stableIds;
            public ContractEntry[] entries;
            public string[] semanticProbeResults;
            public string[] reviewAssetResults;
        }

        [Serializable]
        private sealed class LayoutSample
        {
            public string locale;
            public int panels;
            public int overflow;
            public int offscreen;
            public int overlap;
            public string screenshot;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId;
            public string baselineCommit;
            public string liveSurface;
            public string hazardProbe;
            public string smokeProbe;
            public string radioProbe;
            public string endingProbe;
            public string keyboardFingerprint;
            public string gamepadFingerprint;
            public string keyboardRestoredFingerprint;
            public string[] localeStates;
            public LayoutSample[] layouts;
            public string[] joystickNames;
        }

        private sealed class Observation
        {
            public bool Passed;
            public string Detail;
        }

        private sealed class CatalogProbe
        {
            public readonly List<ContractEntry> Entries = new List<ContractEntry>();
            public readonly HashSet<string> StableIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly List<string> ProbeResults = new List<string>();
            public string Surface = string.Empty;
        }

        private sealed class ReviewAsset
        {
            public readonly string CandidateId;
            public readonly string JobId;
            public readonly string PrimaryPath;
            public readonly string ManifestPath;

            public ReviewAsset(string candidateId, string jobId, string primaryPath, string manifestPath)
            {
                CandidateId = candidateId;
                JobId = jobId;
                PrimaryPath = primaryPath;
                ManifestPath = manifestPath;
            }
        }

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string EvidenceFolder { get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); } }
        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave17" : Sanitize(value);
            }
        }

        private static string BaselineCommit
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_BASELINE");
                return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
            }
        }

        private static bool IsRedBaseline { get { return string.Equals(BaselineCommit, RedBaseline, StringComparison.Ordinal); } }

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<Check> checks = new List<Check>();

            Infrastructure(checks, "W17-I01.exact_baseline", "baseline identity", "P0",
                "The command environment identifies the exact checked-out commit before product classification",
                () => RequireDetail(BaselineCommit != "unknown", "baseline=" + BaselineCommit),
                "Run the PowerShell entry point with -BaselineCommit equal to git rev-parse HEAD.",
                "Assets/Editor/ParallelQA/Invoke-Wave17PacingHazardGate.ps1");
            Infrastructure(checks, "W17-I02.canonical_sources", "canonical contract", "P0",
                "The Wave 16 pacing packet, human contract, ready QA task, and stable IDs are present",
                ObserveCanonicalSources,
                "Read the Wave 16 packet and human contract and locate the ready Forge QA task.",
                ".forge/packets/wave16-fifty-day-pacing.json; Docs/Design/wave16-fifty-day-pacing.md; .forge/backlog.json");

            CatalogProbe catalog = DiscoverPublicContractSurface();
            Observation bands = ObservePacingBands(catalog);
            Observation earlyEscape = ObserveEarlyEscape(catalog);
            Observation regionUnlocks = ObserveRegionUnlocks(catalog);
            Observation determinism = ObserveDeterminism(catalog);
            Observation pity = ObservePity(catalog);
            Observation routeAudit = ObserveRouteAudit(catalog);
            Observation lifecycle = ObserveHazardLifecycle(catalog);
            Observation cadence = ObserveHazardCadence(catalog);
            Observation atomicity = ObserveHazardAtomicity(catalog);
            Observation escapeCatalog = ObserveEscapeCatalogAndAxes(catalog);
            Observation naturalPaths = ObserveNaturalPaths(catalog);
            Observation dataOnly = ObserveDataOnlyRoutes(catalog);
            Observation persistence = ObservePersistenceAndPrivacy(catalog);
            Observation endingCatalog = ObserveEndingCatalog(catalog);
            Observation endingResolver = ObserveEndingPriorityAndHysteresis(catalog);

            Product(checks, "W17-T01.day_band_boundaries", "50-day pacing", "P0",
                "Public runtime data exposes Day 1/11/21/36/50 starts, Day 49 continuation, and Day 50 terminal without Day 51",
                bands.Passed, bands.Detail,
                "Enumerate public pacing entries by stable ID and inspect exact start/end fields.",
                "runtime pacing catalog/config selected by the implementation owner");
            Product(checks, "W17-T02.early_escape_no_hardlock", "early escape", "P0",
                "A deterministic public probe completes a fulfilled escape before Day 50 in every pacing band without a date hardlock",
                earlyEscape.Passed, earlyEscape.Detail,
                "Run the public pacing/escape deterministic probe across all band boundaries and compare terminal results.",
                "runtime pacing and escape resolver selected by the implementation owner");
            Product(checks, "W17-R01.six_region_primary_alternative", "region unlock", "P0",
                "All six region IDs exist and each expansion region exposes independently reachable primary and alternative unlock data",
                regionUnlocks.Passed, regionUnlocks.Detail,
                "Enumerate public region profiles and execute primary/alternative unlock fixtures without relying on dates.",
                "runtime region catalog/unlock resolver selected by the implementation owner");
            Product(checks, "W17-R02.seed_forecast_hazard_pity_determinism", "seed determinism", "P0",
                "Same seed+day+state repeats forecast, hazard, unlock, and pity results while another seed remains valid",
                determinism.Passed, determinism.Detail,
                "Execute a public deterministic pacing probe twice with the same seed and once with another seed.",
                "runtime seeded pacing/forecast/hazard resolver selected by the implementation owner");
            Product(checks, "W17-R03.eligible_search_hint3_guarantee5", "pity", "P0",
                "Only eligible completed searches count; search 3 exposes a hint and search 5 guarantees the next eligible result",
                pity.Passed, pity.Detail,
                "Run eligible, cancelled, failed, unrelated, duplicate, hint-3, and guarantee-5 fixtures.",
                "runtime key-part pity/loot transaction selected by the implementation owner");
            Product(checks, "W17-R04.minimum_three_completable_paths", "softlock audit", "P0",
                "Seed generation, expansion unlock, Day 35, and Day 49 audits each retain at least three completable escape methods",
                routeAudit.Passed, routeAudit.Detail,
                "Execute the public route audit at all four audit points and record stable escape IDs.",
                "runtime escape reachability auditor selected by the implementation owner");
            Product(checks, "W17-H01.three_hazard_four_phase_lifecycle", "hazard lifecycle", "P0",
                "injury, disaster, and food-theft expose telegraph, occurrence, mitigation, and recovery with stable instance IDs",
                lifecycle.Passed, lifecycle.Detail,
                "Enumerate public hazard data and execute each four-phase fixture.",
                "runtime hazard catalog/state machine selected by the implementation owner");
            Product(checks, "W17-H02.rolling_calm_and_major_recovery", "hazard fairness", "P0",
                "Every rolling five-day window has a calm day; a resolved major reserves next-day recovery and forbids same-family major",
                cadence.Passed, cadence.Detail,
                "Run at least ten deterministic days and inspect every rolling window plus post-major reservation.",
                "runtime hazard cadence/director selected by the implementation owner");
            Product(checks, "W17-H03.atomic_retry_loss_and_keypart_protection", "hazard atomicity", "P0",
                "Same event retry is idempotent; one theft/damage transaction applies once and cannot delete key parts/completed stages",
                atomicity.Passed, atomicity.Detail,
                "Resolve the same hazardInstanceId twice and compare resource, facility, protected-part, stage, score, and log snapshots.",
                "runtime hazard transaction and protected project inventory selected by the implementation owner");
            Product(checks, "W17-E01.five_escape_ids_and_two_axes", "escape catalog", "P0",
                "Exactly five canonical escape IDs exist and every pair differs on at least two public requirement axes",
                escapeCatalog.Passed, escapeCatalog.Detail,
                "Enumerate stable escape entries and compare region/research/facility/part/material/time/risk/timing axes pairwise.",
                "runtime escape-project catalog selected by the implementation owner");
            Product(checks, "W17-E02.smoke_radio_natural_interaction_routes", "playable escape routes", "P0",
                "Smoke and radio deterministic routes complete through actual interactions with grant=false and warp=false",
                naturalPaths.Passed, naturalPaths.Detail,
                "Execute separate public smoke/radio natural-route probes and inspect interaction count, grant, warp, and terminal result.",
                "runtime smoke/radio project fixtures and public interaction surface selected by the implementation owner");
            Product(checks, "W17-E03.raft_flare_beacon_data_only", "data-only escape routes", "P1",
                "Raft, flare, and beacon expose complete catalog/graph/snapshot/atomic result data while remaining explicitly data-only",
                dataOnly.Passed, dataOnly.Detail,
                "Inspect public entries and deterministic data validators; do not claim playable PASS.",
                "runtime escape-project catalog/data validators selected by the implementation owner");
            Product(checks, "W17-O01.snapshot_and_private_log", "persistence and privacy", "P0",
                "Snapshot preserves seed, region, hazard, project progress, behavior scores; logs contain stable fields but no PII/free text",
                persistence.Passed, persistence.Detail,
                "Inspect public snapshot/log schemas and serialize a stable-ID-only verification record.",
                "runtime run snapshot and development telemetry schemas selected by the implementation owner");
            Product(checks, "W17-N01.ending_catalog_19_and_samples", "ending catalog", "P0",
                "All 19 canonical ending IDs and four samples are unique public data",
                endingCatalog.Passed, endingCatalog.Detail,
                "Enumerate public ending catalog objects by stable ID.",
                "runtime ending catalog selected by the implementation owner");
            Product(checks, "W17-N02.priority_tiebreak_and_hysteresis", "ending resolver", "P0",
                "Early escape outranks settlement, Day 50 no-escape resolves once, tie-break is deterministic, and one 2-point action cannot flip established identity",
                endingResolver.Passed, endingResolver.Detail,
                "Run identical snapshots twice plus the established-identity challenger fixture and compare ending/reason/identity.",
                "runtime terminal/ending/behavior resolver selected by the implementation owner");

            List<string> reviewResults = new List<string>();
            for (int index = 0; index < ReviewAssets.Length; index++)
            {
                ReviewAsset asset = ReviewAssets[index];
                Observation review = ObserveReviewAsset(asset);
                reviewResults.Add(asset.CandidateId + ": " + review.Detail);
                Guard(checks, "W17-A0" + (index + 1) + ".selection_gate_not_runtime_referenced", "human adoption", "P0",
                    asset.CandidateId + " is either review-only or explicitly adopted, and remains absent from runtime/scene/Addressables references until connection work",
                    review.Passed, review.Detail,
                    "Inspect the review index/manifest or explicit adoption record/package, then search runtime scripts, scenes, and Addressables for candidate/job/file/GUID references.",
                    "Docs/Art/Wave16/wave16-art-review-index.json, Docs/Art/Wave17/wave17-adoption-record.json, .forge/feedback.json, and the selected-only forge-import.json");
            }

            Unverified(checks, "W17-HW01.physical_gamepad", "hardware", "P1",
                "A person completes the full route with a physically connected gamepad",
                "No physical-controller evidence is generated by this automated runner.",
                "Run the manual physical gamepad checklist with device identity and observer notes.",
                "Docs/QA manual hardware evidence");
            Unverified(checks, "W17-S01.steam_release", "external release", "P0",
                "Steam App ID, SDK, depot, Input, Cloud, achievements, and account permissions have independent evidence",
                "Steam integration and release configuration are outside this gate and remain NOT_READY.",
                "Run a separately authorized Steam release audit.",
                "Steamworks configuration and partner account evidence");

            WriteJson("wave17-edit-evidence.json", new EditEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                discoveryPolicy = "Stable IDs, public data, public deterministic probe results, and live state; no implementation class/file-name requirement.",
                stableIds = catalog.StableIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                entries = catalog.Entries.OrderBy(entry => entry.id, StringComparer.Ordinal).ToArray(),
                semanticProbeResults = catalog.ProbeResults.ToArray(),
                reviewAssetResults = reviewResults.ToArray()
            });
            WriteReport("wave17-edit-contracts", "Wave 17 pacing/hazard RED-first hardening Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "Wave 17 Play runner did not complete");
            Directory.CreateDirectory(EvidenceFolder);
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
            if (EditorApplication.isPlaying)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 25d;
                if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 25d;
                if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
            }
            else if (state == PlayModeStateChange.EnteredEditMode) FinishPlayContracts();
        }

        private static void PlayTick()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup > playTimeoutAt)
            {
                WritePlayInfrastructureFailure(new TimeoutException("Wave 17 Play fixture timed out."));
                StopPlayContracts();
                return;
            }
            if (EditorApplication.timeSinceStartup < playEarliestRunTime) return;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }

            try
            {
                DateTime started = DateTime.UtcNow;
                KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
                if (prototype == null) throw new InvalidOperationException("KimSurvivalPrototype was not found in Play Mode.");

                string surface = DescribeLiveSurface();
                string hazardProbe = InvokeLiveSemanticProbe("hazard", null);
                string smokeProbe = InvokeLiveSemanticProbe("escape", "smoke.route.smoke");
                string radioProbe = InvokeLiveSemanticProbe("escape", "smoke.route.radio");
                string endingProbe = InvokeLiveSemanticProbe("ending", "ending.escape.smoke.seen-from-afar");
                string combinedHazard = (surface + " | " + hazardProbe).ToLowerInvariant();

                Observation liveHazards = Obs(
                    RequiredHazards.All(id => combinedHazard.Contains(id)) &&
                    HasAll(combinedHazard, "telegraph", "occurrence", "mitigation", "recovery") && HasPassSignal(hazardProbe),
                    "surface/probe=" + TrimEvidence(surface + " | " + hazardProbe));
                Observation liveNatural = ObserveNaturalRouteText(smokeProbe, radioProbe);

                List<string> localeStates = new List<string>();
                List<LayoutSample> layouts = new List<LayoutSample>();
                foreach (string locale in new[] { "ko", "en", "qps-long" })
                {
                    SetLocale(prototype, locale);
                    TryOpenSemanticPresentation("ending", "ending.escape.smoke.seen-from-afar");
                    InvokeOptional(prototype, "RefreshAll");
                    string activeLocale = CurrentLocale(prototype);
                    localeStates.Add(locale + "->" + activeLocale);
                    string screenshot = "wave17-" + locale + "-ending-state-1280x800.png";
                    prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, screenshot), 1280, 800);
                    int panels, overflow, offscreen, overlap;
                    MeasureEndingLayout(out panels, out overflow, out offscreen, out overlap);
                    layouts.Add(new LayoutSample
                    {
                        locale = locale,
                        panels = panels,
                        overflow = overflow,
                        offscreen = offscreen,
                        overlap = overlap,
                        screenshot = screenshot
                    });
                }

                bool localeApplied = localeStates.SequenceEqual(new[] { "ko->ko", "en->en", "qps-long->qps-long" });
                bool endingPresent = RequiredSamples.Any(id => (surface + endingProbe).Contains(id));
                bool layoutPass = localeApplied && endingPresent && layouts.All(sample =>
                    sample.panels == 3 && sample.overflow == 0 && sample.offscreen == 0 && sample.overlap == 0 && VerifyPng(sample.screenshot));
                Observation endingPriority = Obs(endingPresent && HasPassSignal(endingProbe) &&
                    HasAny(endingProbe, "escape_complete", "escapecomplete", "early escape") &&
                    HasAny(endingProbe, "day50", "day 50", "settlement"),
                    "probe=" + TrimEvidence(endingProbe));

                SetInputDevice(prototype, PrototypeInputDevice.KeyboardMouse);
                string keyboard = LiveSemanticFingerprint(prototype);
                SetInputDevice(prototype, PrototypeInputDevice.Gamepad);
                string gamepad = LiveSemanticFingerprint(prototype);
                SetInputDevice(prototype, PrototypeInputDevice.KeyboardMouse);
                string restored = LiveSemanticFingerprint(prototype);
                bool inputParity = endingPresent && keyboard == gamepad && gamepad == restored;

                List<Check> checks = new List<Check>();
                Product(checks, "W17-P01.live_hazard_lifecycle", "actual Play hazard", "P0",
                    "All three sample hazards execute four lifecycle phases in actual Play state with one idempotent instance",
                    liveHazards.Passed, liveHazards.Detail,
                    "Run Play Mode and execute each public deterministic hazard fixture.",
                    "runtime hazard director/presentation selected by the implementation owner");
                Product(checks, "W17-P02.live_smoke_radio_natural_paths", "actual Play escape", "P0",
                    "Separate smoke/radio routes use actual interactions, complete terminally, and report grant=false/warp=false",
                    liveNatural.Passed, liveNatural.Detail,
                    "Run the two natural-route fixtures in Play Mode and inspect interaction trace and terminal state.",
                    "runtime escape project fixtures/UI selected by the implementation owner");
                Product(checks, "W17-P03.live_terminal_priority_and_three_panels", "ending presentation", "P0",
                    "Early escape and Day 50 priority resolve deterministically and show exactly three core panels",
                    endingPriority.Passed && layouts.All(sample => sample.panels == 3),
                    endingPriority.Detail + "; panels=" + string.Join(",", layouts.Select(sample => sample.locale + ":" + sample.panels).ToArray()),
                    "Resolve the four sample snapshots and inspect the active ending presentation hierarchy.",
                    "runtime terminal resolver/ending presentation selected by the implementation owner");
                Product(checks, "W17-P04.ko_en_qps_1280_layout", "localized ending layout", "P1",
                    "KO/EN/qps-long each render 1280x800 with three panels and zero TMP overflow/offscreen/overlap",
                    layoutPass, "locales=" + string.Join(",", localeStates.ToArray()) + "; " +
                                string.Join("; ", layouts.Select(sample => sample.locale + ":panels=" + sample.panels +
                                    ",overflow=" + sample.overflow + ",offscreen=" + sample.offscreen + ",overlap=" + sample.overlap).ToArray()),
                    "Open each wave17-*-ending-state-1280x800.png at 1:1 and compare the recorded metrics.",
                    "runtime ending presentation/localization tables selected by the implementation owner");
                Product(checks, "W17-P05.keyboard_synthetic_gamepad_parity", "input parity", "P1",
                    "Keyboard and synthetic gamepad preserve identical locale/progression/hazard/escape/ending semantics",
                    inputParity, "keyboard=" + TrimEvidence(keyboard) + "; gamepad=" + TrimEvidence(gamepad) + "; restored=" + TrimEvidence(restored),
                    "Switch the prototype input tracker KeyboardMouse->Gamepad->KeyboardMouse and compare semantic fingerprints.",
                    "runtime input and ending UI selected by the implementation owner");

                WriteJson("wave17-play-evidence.json", new PlayEvidence
                {
                    runId = RunId,
                    baselineCommit = BaselineCommit,
                    liveSurface = surface,
                    hazardProbe = hazardProbe,
                    smokeProbe = smokeProbe,
                    radioProbe = radioProbe,
                    endingProbe = endingProbe,
                    keyboardFingerprint = keyboard,
                    gamepadFingerprint = gamepad,
                    keyboardRestoredFingerprint = restored,
                    localeStates = localeStates.ToArray(),
                    layouts = layouts.ToArray(),
                    joystickNames = Input.GetJoystickNames() ?? new string[0]
                });
                Report report = WriteReport("wave17-play-contracts", "Wave 17 pacing/hazard RED-first hardening Play contracts", started, checks);
                SessionState.SetBool(PlayExitPassKey, report.infrastructureOverall == "PASS" && report.productOverall != "FAIL");
                SessionState.SetString(PlayMessageKey, report.overall + ": product=" + report.productOverall + ", infrastructure=" + report.infrastructureOverall);
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static string ObserveCanonicalSources()
        {
            string packetPath = Path.Combine(ProjectRoot, ".forge", "packets", "wave16-fifty-day-pacing.json");
            string designPath = Path.Combine(ProjectRoot, "Docs", "Design", "wave16-fifty-day-pacing.md");
            string backlogPath = Path.Combine(ProjectRoot, ".forge", "backlog.json");
            if (!File.Exists(packetPath) || !File.Exists(designPath) || !File.Exists(backlogPath))
                throw new FileNotFoundException("A canonical Wave 16 source is missing.");
            string canonical = File.ReadAllText(packetPath) + File.ReadAllText(designPath) + File.ReadAllText(backlogPath);
            string[] required = RequiredBands.Concat(RequiredRegions).Concat(RequiredHazards).Concat(RequiredEscapes)
                .Concat(new[] { "task.qa.wave15-hazard-ending-redfirst", "eligible-search", "rollingWindowDays", "sampleSwitchLead" }).ToArray();
            string[] missing = required.Where(value => canonical.IndexOf(value, StringComparison.Ordinal) < 0).ToArray();
            return RequireDetail(missing.Length == 0, "required=" + required.Length + "; missing=" + string.Join(",", missing));
        }

        private static CatalogProbe DiscoverPublicContractSurface()
        {
            CatalogProbe probe = new CatalogProbe();
            HashSet<object> visited = new HashSet<object>(ReferenceComparer.Instance);
            List<string> surface = new List<string>();
            foreach (Type type in RuntimeTypes())
            {
                foreach (FieldInfo field in type.GetFields(PublicStatic))
                {
                    try { Visit(field.GetValue(null), probe, visited, 5, type.FullName + "." + field.Name, surface); } catch { }
                }
                foreach (PropertyInfo property in type.GetProperties(PublicStatic).Where(property => property.GetIndexParameters().Length == 0 && property.CanRead))
                {
                    try { Visit(property.GetValue(null, null), probe, visited, 5, type.FullName + "." + property.Name, surface); } catch { }
                }
                if (ContainsAny(type.Name.ToLowerInvariant(), "pacing", "region", "hazard", "escape", "ending", "campaign", "contract", "catalog"))
                {
                    object instance = CreatePublicInstance(type);
                    if (instance != null) Visit(instance, probe, visited, 5, type.FullName, surface);
                }
            }
            probe.ProbeResults.AddRange(InvokePublicSemanticProbes());
            surface.AddRange(probe.ProbeResults);
            probe.Surface = string.Join(" | ", surface.Take(700).ToArray());
            return probe;
        }

        private static void Visit(object value, CatalogProbe probe, HashSet<object> visited, int depth, string origin, List<string> surface)
        {
            if (value == null || depth < 0) return;
            Type type = value.GetType();
            if (!type.IsValueType && !(value is string) && !visited.Add(value)) return;
            if (value is string text)
            {
                if (StableId.IsMatch(text)) probe.StableIds.Add(text);
                surface.Add(origin + "=" + text);
                return;
            }
            if (type.IsPrimitive || type.IsEnum || value is decimal)
            {
                surface.Add(origin + "=" + Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }
            if (value is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object item in enumerable)
                {
                    Visit(item, probe, visited, depth - 1, origin + "[" + count + "]", surface);
                    if (++count >= 256) break;
                }
                return;
            }

            List<MemberSnapshot> members = ReadPublicMembers(value);
            string id = FindStableId(members);
            if (!string.IsNullOrWhiteSpace(id))
            {
                probe.StableIds.Add(id);
                if (!probe.Entries.Any(entry => entry.id == id && entry.type == type.FullName))
                    probe.Entries.Add(new ContractEntry { id = id, type = type.FullName, members = members.ToArray() });
            }
            surface.Add(origin + "=" + type.FullName + "{" + string.Join(";", members.Select(member => member.name + "=" + member.value).ToArray()) + "}");
            foreach (FieldInfo field in type.GetFields(PublicInstance))
            {
                try { Visit(field.GetValue(value), probe, visited, depth - 1, origin + "." + field.Name, surface); } catch { }
            }
            foreach (PropertyInfo property in type.GetProperties(PublicInstance).Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
            {
                try { Visit(property.GetValue(value, null), probe, visited, depth - 1, origin + "." + property.Name, surface); } catch { }
            }
        }

        private static List<MemberSnapshot> ReadPublicMembers(object value)
        {
            List<MemberSnapshot> members = new List<MemberSnapshot>();
            foreach (FieldInfo field in value.GetType().GetFields(PublicInstance).Take(64))
            {
                try { members.Add(new MemberSnapshot { name = field.Name, value = Describe(field.GetValue(value), 2) }); } catch { }
            }
            foreach (PropertyInfo property in value.GetType().GetProperties(PublicInstance).Where(property => property.CanRead && property.GetIndexParameters().Length == 0).Take(64))
            {
                try { members.Add(new MemberSnapshot { name = property.Name, value = Describe(property.GetValue(value, null), 2) }); } catch { }
            }
            return members;
        }

        private static string FindStableId(IEnumerable<MemberSnapshot> members)
        {
            foreach (MemberSnapshot member in members)
            {
                string value = member.value == null ? string.Empty : member.value.Trim();
                if (StableId.IsMatch(value) && ContainsAny(member.name.ToLowerInvariant(), "id", "key")) return value;
            }
            return null;
        }

        private static IEnumerable<string> InvokePublicSemanticProbes()
        {
            List<string> results = new List<string>();
            foreach (Type type in RuntimeTypes())
            foreach (MethodInfo method in type.GetMethods(PublicStatic | PublicInstance))
            {
                string name = method.Name.ToLowerInvariant();
                if (!ContainsAny(name, "probe", "verify", "fixture", "simulate", "contract")) continue;
                if (!ContainsAny(name, "pacing", "forecast", "hazard", "pity", "route", "escape", "ending", "identity", "softlock")) continue;
                object target = method.IsStatic ? null : CreatePublicInstance(type);
                if (!method.IsStatic && target == null) continue;
                object[] arguments = BuildProbeArguments(method.GetParameters(), name);
                if (arguments == null) continue;
                try
                {
                    object result = method.Invoke(target, arguments);
                    if (result != null) results.Add(type.FullName + "." + method.Name + "=>" + Describe(result, 5));
                }
                catch (TargetInvocationException exception)
                {
                    results.Add(type.FullName + "." + method.Name + "=>THREW:" + exception.InnerException.GetType().Name);
                }
                catch { }
            }
            return results.Distinct().Take(256).ToArray();
        }

        private static object[] BuildProbeArguments(ParameterInfo[] parameters, string semantic)
        {
            object[] values = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                ParameterInfo parameter = parameters[index];
                Type type = parameter.ParameterType;
                string name = parameter.Name == null ? string.Empty : parameter.Name.ToLowerInvariant();
                if (type == typeof(string))
                {
                    values[index] = name.Contains("route") ? (semantic.Contains("radio") ? "smoke.route.radio" : "smoke.route.smoke") :
                        name.Contains("escape") ? (semantic.Contains("radio") ? "escape.radio" : "escape.smoke") :
                        name.Contains("region") ? "region.forest.grove" : name.Contains("hazard") ? "hazard.food-theft" : semantic;
                }
                else if (type == typeof(int)) values[index] = name.Contains("seed") ? 170017 : name.Contains("day") ? 49 : name.Contains("count") ? 5 : 1;
                else if (type == typeof(long)) values[index] = name.Contains("seed") ? 170017L : 1L;
                else if (type == typeof(bool)) values[index] = true;
                else if (type.IsEnum) values[index] = Enum.GetValues(type).GetValue(0);
                else if (type.IsValueType) values[index] = Activator.CreateInstance(type);
                else
                {
                    object instance = CreatePublicInstance(type);
                    if (instance == null && !parameter.IsOptional) return null;
                    values[index] = instance ?? parameter.DefaultValue;
                }
            }
            return values;
        }

        private static Observation ObservePacingBands(CatalogProbe catalog)
        {
            List<string> details = new List<string>();
            bool passed = true;
            foreach (string id in RequiredBands)
            {
                ContractEntry entry = catalog.Entries.FirstOrDefault(candidate => candidate.id == id);
                string detail = entry == null ? "missing" : entry.Describe();
                details.Add(id + "=" + detail);
                passed &= entry != null;
            }
            string surface = string.Join(" | ", details).ToLowerInvariant();
            passed &= RequiredBoundaryDays.All(day => Regex.IsMatch(surface, "(?:^|[^0-9])" + day + "(?:[^0-9]|$)"));
            return Obs(passed, TrimEvidence(string.Join("; ", details.ToArray())));
        }

        private static Observation ObserveEarlyEscape(CatalogProbe catalog)
        {
            string probe = ProbeResults(catalog, "escape", "pacing");
            bool passed = HasPassSignal(probe) && HasAny(probe, "earlyescape", "early escape", "beforedeadline") &&
                          HasAny(probe, "hardlock=false", "datehardlock=false", "nothardlocked", "blockedbyday=false") &&
                          RequiredBands.All(id => probe.Contains(id));
            return Obs(passed, TrimEvidence(probe));
        }

        private static Observation ObserveRegionUnlocks(CatalogProbe catalog)
        {
            List<string> details = new List<string>();
            bool passed = RequiredRegions.All(id => catalog.StableIds.Contains(id));
            foreach (string id in ExpansionRegions)
            {
                ContractEntry entry = catalog.Entries.FirstOrDefault(candidate => candidate.id == id);
                string detail = entry == null ? "missing" : entry.Describe();
                details.Add(id + "=" + detail);
                passed &= entry != null && HasAll(detail.ToLowerInvariant(), "primary", "alternative") &&
                          HasAny(detail.ToLowerInvariant(), "requirement", "condition", "unlock");
            }
            return Obs(passed, "regions=" + string.Join(",", RequiredRegions.Where(catalog.StableIds.Contains).ToArray()) + "; " + TrimEvidence(string.Join(";", details.ToArray())));
        }

        private static Observation ObserveDeterminism(CatalogProbe catalog)
        {
            string probe = ProbeResults(catalog, "determin", "forecast", "pacing");
            bool passed = HasPassSignal(probe) && HasAll(probe.ToLowerInvariant(), "seed", "forecast", "hazard", "pity") &&
                          HasAny(probe, "same=true", "repeated=true", "deterministic=true", "sameSeedMatch=true");
            return Obs(passed, TrimEvidence(probe));
        }

        private static Observation ObservePity(CatalogProbe catalog)
        {
            string probe = ProbeResults(catalog, "pity", "eligible");
            bool data = catalog.Surface.IndexOf("eligible", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        Regex.IsMatch(catalog.Surface, "(?:hint|search)[^|]{0,80}3", RegexOptions.IgnoreCase) &&
                        Regex.IsMatch(catalog.Surface, "(?:guarantee|search)[^|]{0,80}5", RegexOptions.IgnoreCase);
            bool passed = data && HasPassSignal(probe) && HasAll(probe.ToLowerInvariant(), "eligible", "hint", "guarantee") &&
                          Regex.IsMatch(probe, "(?:hint|count)[^|]{0,80}3", RegexOptions.IgnoreCase) &&
                          Regex.IsMatch(probe, "(?:guarantee|count)[^|]{0,80}5", RegexOptions.IgnoreCase);
            return Obs(passed, "data=" + data + "; probe=" + TrimEvidence(probe));
        }

        private static Observation ObserveRouteAudit(CatalogProbe catalog)
        {
            string probe = ProbeResults(catalog, "softlock", "route", "audit");
            bool passed = HasPassSignal(probe) && HasAny(probe, "minimum=3", "min=3", "completable=3", "routeCount=3") &&
                          HasAll(probe.ToLowerInvariant(), "seed", "expansion", "35", "49");
            return Obs(passed, TrimEvidence(probe));
        }

        private static Observation ObserveHazardLifecycle(CatalogProbe catalog)
        {
            List<string> details = new List<string>();
            bool passed = true;
            foreach (string id in RequiredHazards)
            {
                ContractEntry entry = catalog.Entries.FirstOrDefault(candidate => candidate.id == id);
                string detail = entry == null ? string.Empty : entry.Describe().ToLowerInvariant();
                details.Add(id + "=" + (entry == null ? "missing" : detail));
                passed &= entry != null && HasAll(detail, "telegraph", "occurrence", "mitigation", "recovery");
            }
            return Obs(passed, TrimEvidence(string.Join("; ", details.ToArray())));
        }

        private static Observation ObserveHazardCadence(CatalogProbe catalog)
        {
            string probe = ProbeResults(catalog, "hazard", "calm", "recovery");
            bool data = HasAll(catalog.Surface.ToLowerInvariant(), "rolling", "5", "calm", "recovery", "major", "reserved") ||
                        HasAll(catalog.Surface.ToLowerInvariant(), "windowdays=5", "minimumcount=1", "reservedbudget=2");
            bool passed = data && HasPassSignal(probe) && HasAll(probe.ToLowerInvariant(), "calm", "major", "recovery") &&
                          HasAny(probe, "rolling5=true", "window=5", "calmdaycount=1") &&
                          HasAny(probe, "reservedbudget=2", "recoveryreserve=2");
            return Obs(passed, "data=" + data + "; probe=" + TrimEvidence(probe));
        }

        private static Observation ObserveHazardAtomicity(CatalogProbe catalog)
        {
            string probe = ProbeResults(catalog, "hazard", "atomic", "idempot");
            bool passed = HasPassSignal(probe) && HasAny(probe, "idempotent=true", "retryunchanged=true", "duplicateapplied=false") &&
                          HasAny(probe, "keypartprotected=true", "protectedpartunchanged=true") &&
                          HasAny(probe, "singleloss=true", "lossapplications=1", "transactioncount=1");
            return Obs(passed, TrimEvidence(probe));
        }

        private static Observation ObserveEscapeCatalogAndAxes(CatalogProbe catalog)
        {
            ContractEntry[] entries = RequiredEscapes.Select(id => catalog.Entries.FirstOrDefault(entry => entry.id == id)).ToArray();
            bool passed = entries.All(entry => entry != null);
            List<string> pairs = new List<string>();
            string[][] axes =
            {
                new[] { "region" }, new[] { "research" }, new[] { "facility", "anchor" }, new[] { "part", "component" },
                new[] { "material", "resource" }, new[] { "time", "travel", "duration" }, new[] { "risk", "hazard" }, new[] { "timing", "window", "weather" }
            };
            for (int left = 0; left < entries.Length; left++)
            for (int right = left + 1; right < entries.Length; right++)
            {
                if (entries[left] == null || entries[right] == null) { passed = false; continue; }
                int differences = axes.Count(axis => !string.Equals(AxisValue(entries[left], axis), AxisValue(entries[right], axis), StringComparison.Ordinal));
                pairs.Add(entries[left].id + "/" + entries[right].id + "=" + differences);
                passed &= differences >= 2;
            }
            return Obs(passed, "found=" + string.Join(",", RequiredEscapes.Where(id => catalog.StableIds.Contains(id)).ToArray()) + "; pairwiseDifferences=" + string.Join(",", pairs.ToArray()));
        }

        private static string AxisValue(ContractEntry entry, IEnumerable<string> tokens)
        {
            return string.Join("|", entry.members.Where(member => tokens.Any(token => member.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(member => member.value).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static Observation ObserveNaturalPaths(CatalogProbe catalog)
        {
            string smoke = ProbeResults(catalog, "smoke.route.smoke", "escape.smoke");
            string radio = ProbeResults(catalog, "smoke.route.radio", "escape.radio");
            return ObserveNaturalRouteText(smoke, radio);
        }

        private static Observation ObserveNaturalRouteText(string smoke, string radio)
        {
            bool smokePass = NaturalRoutePass(smoke, "smoke");
            bool radioPass = NaturalRoutePass(radio, "radio");
            return Obs(smokePass && radioPass, "smoke=" + TrimEvidence(smoke) + "; radio=" + TrimEvidence(radio));
        }

        private static bool NaturalRoutePass(string value, string route)
        {
            string lower = value.ToLowerInvariant();
            return HasPassSignal(value) && lower.Contains(route) &&
                   HasAny(lower, "grant=false", "usedgrant=false", "grantcount=0") &&
                   HasAny(lower, "warp=false", "usedwarp=false", "warpcount=0") &&
                   HasAny(lower, "completed=true", "terminal=true", "escapecomplete=true") &&
                   Regex.IsMatch(lower, "(?:interactioncount|interactions|actioncount)=[1-9][0-9]*");
        }

        private static Observation ObserveDataOnlyRoutes(CatalogProbe catalog)
        {
            List<string> details = new List<string>();
            bool passed = true;
            foreach (string id in new[] { "escape.raft", "escape.flare", "escape.beacon" })
            {
                ContractEntry entry = catalog.Entries.FirstOrDefault(candidate => candidate.id == id);
                string detail = entry == null ? string.Empty : entry.Describe().ToLowerInvariant();
                details.Add(id + "=" + (entry == null ? "missing" : detail));
                passed &= entry != null && HasAny(detail, "dataonly=true", "validation", "catalog") &&
                          HasAny(detail, "primary", "alternative") && HasAny(detail, "snapshot", "stage", "progress") &&
                          HasAny(detail, "atomic", "transaction", "resolver");
            }
            return Obs(passed, TrimEvidence(string.Join("; ", details.ToArray())));
        }

        private static Observation ObservePersistenceAndPrivacy(CatalogProbe catalog)
        {
            string surface = catalog.Surface.ToLowerInvariant();
            bool snapshot = HasAll(surface, "seed", "region", "hazard") && HasAny(surface, "project", "escape") && HasAny(surface, "behavior", "score", "stat");
            bool log = HasAll(surface, "day", "stable") && HasAny(surface, "resultcode", "outcome") && HasAny(surface, "pacingband", "bandid");
            bool pii = Regex.IsMatch(surface, "(?:^|[.;{])(?:name|account|email|ipaddress|hostname|freetext)=", RegexOptions.IgnoreCase);
            return Obs(snapshot && log && !pii, "snapshot=" + snapshot + "; log=" + log + "; piiFields=" + (pii ? "present" : "none"));
        }

        private static Observation ObserveEndingCatalog(CatalogProbe catalog)
        {
            string[] found = RequiredEndings.Where(id => catalog.StableIds.Contains(id)).ToArray();
            string[] samples = RequiredSamples.Where(id => catalog.StableIds.Contains(id)).ToArray();
            return Obs(found.Length == 19 && samples.Length == 4 && found.Distinct(StringComparer.Ordinal).Count() == 19,
                "endings=" + found.Length + "/19; samples=" + samples.Length + "/4; ids=" + string.Join(",", found));
        }

        private static Observation ObserveEndingPriorityAndHysteresis(CatalogProbe catalog)
        {
            string probe = ProbeResults(catalog, "ending", "identity", "terminal");
            bool passed = HasPassSignal(probe) && HasAny(probe, "escape_complete", "escapecomplete", "earlyescape") &&
                          HasAny(probe, "day50", "day 50") && HasAny(probe, "tiebreak", "ascii") &&
                          HasAny(probe, "hysteresis", "switchlead=6", "identityunchanged=true") &&
                          HasAny(probe, "deterministic=true", "sameending=true");
            return Obs(passed, TrimEvidence(probe));
        }

        private static Observation ObserveReviewAsset(ReviewAsset asset)
        {
            string indexPath = Path.Combine(ProjectRoot, "Docs", "Art", "Wave16", "wave16-art-review-index.json");
            string adoptionPath = Path.Combine(ProjectRoot, "Docs", "Art", "Wave17", "wave17-adoption-record.json");
            string feedbackPath = Path.Combine(ProjectRoot, ".forge", "feedback.json");
            string manifestPath = Path.Combine(ProjectRoot, asset.ManifestPath.Replace('/', Path.DirectorySeparatorChar));
            string primaryPath = Path.Combine(ProjectRoot, asset.PrimaryPath.Replace('/', Path.DirectorySeparatorChar));
            string packagePath = Path.Combine(Path.GetDirectoryName(primaryPath), "forge-import.json");
            if (!File.Exists(indexPath) || !File.Exists(manifestPath) || !File.Exists(primaryPath))
                return Obs(false, "missing index, manifest, or primary file");
            string index = File.ReadAllText(indexPath);
            string manifest = File.ReadAllText(manifestPath);
            bool review = index.Contains("\"status\": \"review\"") && index.Contains("\"selectedCandidate\": null") &&
                          index.Contains("\"runtimeAllowlist\": []") && manifest.Contains("\"status\": \"review\"") &&
                          manifest.Contains("\"selectedCandidate\": null") && manifest.Contains("\"runtimeConnectAllowed\": false") &&
                          index.Contains(asset.CandidateId) && index.Contains(asset.JobId);
            bool adopted = false;
            if (File.Exists(adoptionPath) && File.Exists(feedbackPath) && File.Exists(packagePath))
            {
                string adoption = File.ReadAllText(adoptionPath);
                string feedback = File.ReadAllText(feedbackPath);
                string package = File.ReadAllText(packagePath);
                adopted = adoption.Contains("\"decisionSource\": \"explicit-user-message\"") &&
                          adoption.Contains(asset.CandidateId) && adoption.Contains(asset.JobId) &&
                          adoption.Contains("\"decision\": \"adopted\"") && adoption.Contains("\"forgeStatus\": \"engine_ready\"") &&
                          adoption.Contains("\"runtimeConnectAllowed\": false") && adoption.Contains("\"runtimeConnected\": false") &&
                          feedback.Contains(asset.JobId) && feedback.Contains("\"decision\": \"adopted\"") &&
                          package.Contains(asset.JobId) && package.Contains(Path.GetFileName(asset.PrimaryPath)) &&
                          package.Contains("\"runtimeAllowlist\": []") && package.Contains("\"runtimeConnectAllowed\": false") &&
                          package.Contains("\"runtimeConnected\": false");
            }
            string guid = AssetDatabase.AssetPathToGUID(asset.PrimaryPath);
            List<string> needles = new List<string> { asset.CandidateId, asset.JobId, Path.GetFileName(asset.PrimaryPath) };
            if (!string.IsNullOrWhiteSpace(guid)) needles.Add(guid);
            List<string> references = FindProductReferences(needles);
            return Obs((review || adopted) && references.Count == 0,
                "review=" + review + "; adopted=" + adopted + "; guid=" + guid + "; runtime/scene/addressablesReferences=" +
                (references.Count == 0 ? "none" : string.Join(",", references.ToArray())));
        }

        private static List<string> FindProductReferences(IEnumerable<string> needles)
        {
            string[] roots =
            {
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime"),
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scenes"),
                Path.Combine(ProjectRoot, "Assets", "AddressableAssetsData")
            };
            HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (!new[] { ".cs", ".unity", ".asset", ".json", ".xml" }.Contains(extension)) continue;
                    string text;
                    try { text = File.ReadAllText(file); } catch { continue; }
                    if (needles.Any(needle => !string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0))
                        results.Add(file.Substring(ProjectRoot.Length + 1).Replace('\\', '/'));
                }
            }
            return results.OrderBy(value => value, StringComparer.Ordinal).ToList();
        }

        private static string ProbeResults(CatalogProbe catalog, params string[] needles)
        {
            string[] matches = catalog.ProbeResults.Where(result => needles.Any(needle => result.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            return matches.Length == 0 ? "no public deterministic semantic probe discovered" : string.Join(" | ", matches);
        }

        private static string InvokeLiveSemanticProbe(string semantic, string stableId)
        {
            List<string> results = new List<string>();
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour == null) continue;
                foreach (MethodInfo method in behaviour.GetType().GetMethods(PublicInstance))
                {
                    string name = method.Name.ToLowerInvariant();
                    if (!name.Contains(semantic) || !ContainsAny(name, "probe", "verify", "fixture")) continue;
                    object[] arguments = BuildLiveArguments(method.GetParameters(), stableId, semantic);
                    if (arguments == null) continue;
                    try
                    {
                        object result = method.Invoke(behaviour, arguments);
                        if (result != null) results.Add(behaviour.GetType().FullName + "." + method.Name + "=>" + Describe(result, 5));
                    }
                    catch (TargetInvocationException exception)
                    {
                        results.Add(behaviour.GetType().FullName + "." + method.Name + "=>THREW:" + exception.InnerException.GetType().Name);
                    }
                    catch { }
                }
            }
            return results.Count == 0 ? "no public live semantic probe discovered" : string.Join(" | ", results.ToArray());
        }

        private static object[] BuildLiveArguments(ParameterInfo[] parameters, string stableId, string semantic)
        {
            object[] values = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                Type type = parameters[index].ParameterType;
                string name = parameters[index].Name == null ? string.Empty : parameters[index].Name.ToLowerInvariant();
                if (type == typeof(string)) values[index] = name.Contains("route") || name.Contains("id") ? (stableId ?? semantic) : semantic;
                else if (type == typeof(int)) values[index] = name.Contains("seed") ? 170017 : name.Contains("day") ? 49 : 1;
                else if (type == typeof(bool)) values[index] = true;
                else if (type.IsEnum) values[index] = Enum.GetValues(type).GetValue(0);
                else if (type.IsValueType) values[index] = Activator.CreateInstance(type);
                else return null;
            }
            return values;
        }

        private static void TryOpenSemanticPresentation(string semantic, string stableId)
        {
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour == null) continue;
                foreach (MethodInfo method in behaviour.GetType().GetMethods(AllInstance))
                {
                    string name = method.Name.ToLowerInvariant();
                    if (!name.Contains(semantic) || !ContainsAny(name, "open", "show", "present", "preview", "resolve", "verify")) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    try
                    {
                        if (parameters.Length == 0) method.Invoke(behaviour, null);
                        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string)) method.Invoke(behaviour, new object[] { stableId });
                        else continue;
                        return;
                    }
                    catch { }
                }
            }
        }

        private static string DescribeLiveSurface()
        {
            List<string> values = new List<string>();
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour == null) continue;
                string identity = behaviour.GetType().FullName + "/" + behaviour.gameObject.name;
                if (!ContainsAny(identity.ToLowerInvariant(), "pacing", "region", "hazard", "escape", "ending", "comic", "result")) continue;
                values.Add(identity + "{" + string.Join(";", ReadPublicMembers(behaviour).Select(member => member.name + "=" + member.value).ToArray()) + "}");
            }
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                string name = gameObject.name;
                if (ContainsAny(name.ToLowerInvariant(), "pacing", "hazard", "escape", "ending", "comic"))
                    values.Add("GameObject:" + name + ":active=" + gameObject.activeInHierarchy);
            }
            return values.Count == 0 ? "no live pacing/hazard/escape/ending stable-ID surface" : string.Join(" | ", values.Take(100).ToArray());
        }

        private static string LiveSemanticFingerprint(KimSurvivalPrototype prototype)
        {
            GameSession session = prototype.Session;
            string surface = DescribeLiveSurface();
            surface = Regex.Replace(surface, "(?:KeyboardMouse|Gamepad|\\[E\\]|\\[X\\])", "<device>");
            return "D" + session.Day + "/" + session.Phase + "/" + session.Result + "/seed" + session.RunSeed +
                   "/region" + session.ActiveRegionProfileId + "/locale" + CurrentLocale(prototype) + "/" + surface;
        }

        private static void SetInputDevice(object prototype, PrototypeInputDevice device)
        {
            object playerInput = GetField(prototype, "playerInput");
            object tracker = GetField(playerInput, "deviceTracker");
            if (tracker == null) return;
            MethodInfo update = tracker.GetType().GetMethod("Update", AllInstance);
            if (update != null) update.Invoke(tracker, new object[] { new PrototypeInputActivity(device == PrototypeInputDevice.KeyboardMouse, device == PrototypeInputDevice.Gamepad) });
        }

        private static void SetLocale(object prototype, string locale)
        {
            PrototypeLocalization localization = GetField(prototype, "localization") as PrototypeLocalization;
            if (localization == null) return;
            if (string.Equals(locale, PrototypeLocalization.QpsLongLocaleCode, StringComparison.OrdinalIgnoreCase))
                localization.SetQaLocale(PrototypeLocalization.QpsLongLocaleCode);
            else localization.SetLocale(locale, false);
        }

        private static string CurrentLocale(object prototype)
        {
            PrototypeLocalization localization = GetField(prototype, "localization") as PrototypeLocalization;
            return localization == null ? "missing" : localization.CurrentLocaleCode;
        }

        private static void MeasureEndingLayout(out int panels, out int overflow, out int offscreen, out int overlap)
        {
            List<Rect> rects = new List<Rect>();
            panels = 0;
            overflow = 0;
            offscreen = 0;
            overlap = 0;
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (!gameObject.activeInHierarchy) continue;
                string hierarchy = HierarchyName(gameObject.transform).ToLowerInvariant();
                if (!ContainsAny(hierarchy, "ending", "comic")) continue;
                if (ContainsAny(gameObject.name.ToLowerInvariant(), "panel", "beat", "frame")) panels++;
                TMP_Text text = gameObject.GetComponent<TMP_Text>();
                if (text == null || string.IsNullOrWhiteSpace(text.text)) continue;
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) overflow++;
                Rect rect = WorldScreenRect(text.rectTransform);
                if (rect.xMin < 4f || rect.yMin < 4f || rect.xMax > 1276f || rect.yMax > 796f) offscreen++;
                rects.Add(rect);
            }
            for (int left = 0; left < rects.Count; left++)
            for (int right = left + 1; right < rects.Count; right++)
            {
                Rect intersection = Intersect(rects[left], rects[right]);
                if (intersection.width > 4f && intersection.height > 4f) overlap++;
            }
        }

        private static Rect WorldScreenRect(RectTransform transform)
        {
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            Canvas canvas = transform.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = min;
            for (int index = 1; index < corners.Length; index++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[index]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            float scaleX = Screen.width > 0 ? 1280f / Screen.width : 1f;
            float scaleY = Screen.height > 0 ? 800f / Screen.height : 1f;
            return Rect.MinMaxRect(min.x * scaleX, min.y * scaleY, max.x * scaleX, max.y * scaleY);
        }

        private static Rect Intersect(Rect left, Rect right)
        {
            float xMin = Math.Max(left.xMin, right.xMin);
            float xMax = Math.Min(left.xMax, right.xMax);
            float yMin = Math.Max(left.yMin, right.yMin);
            float yMax = Math.Min(left.yMax, right.yMax);
            return xMax > xMin && yMax > yMin ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : new Rect();
        }

        private static string HierarchyName(Transform transform)
        {
            List<string> names = new List<string>();
            while (transform != null) { names.Add(transform.name); transform = transform.parent; }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static bool VerifyPng(string fileName)
        {
            string path = Path.Combine(EvidenceFolder, fileName);
            if (!File.Exists(path)) return false;
            byte[] bytes = File.ReadAllBytes(path);
            return bytes.Length >= 24 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                   ReadBigEndianInt(bytes, 16) == 1280 && ReadBigEndianInt(bytes, 20) == 800;
        }

        private static int ReadBigEndianInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static IEnumerable<Type> RuntimeTypes()
        {
            return typeof(GameSession).Assembly.GetTypes().Where(type => type.Namespace == "KimSurvival").OrderBy(type => type.FullName);
        }

        private static object CreatePublicInstance(Type type)
        {
            try
            {
                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                return constructor == null ? null : constructor.Invoke(null);
            }
            catch { return null; }
        }

        private static string Describe(object value, int depth)
        {
            if (value == null) return "null";
            if (value is string text) return text;
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal) return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (depth <= 0) return type.Name;
            if (value is IEnumerable enumerable)
            {
                List<string> items = new List<string>();
                foreach (object item in enumerable) { items.Add(Describe(item, depth - 1)); if (items.Count >= 128) break; }
                return "[" + string.Join(",", items.ToArray()) + "]";
            }
            List<string> members = new List<string>();
            foreach (FieldInfo field in type.GetFields(PublicInstance).Take(64))
            {
                try { members.Add(field.Name + "=" + Describe(field.GetValue(value), depth - 1)); } catch { }
            }
            foreach (PropertyInfo property in type.GetProperties(PublicInstance).Where(property => property.CanRead && property.GetIndexParameters().Length == 0).Take(64))
            {
                try { members.Add(property.Name + "=" + Describe(property.GetValue(value, null), depth - 1)); } catch { }
            }
            return type.Name + "{" + string.Join(";", members.ToArray()) + "}";
        }

        private static object GetField(object target, string name)
        {
            if (target == null) return null;
            FieldInfo field = target.GetType().GetField(name, AllInstance);
            return field == null ? null : field.GetValue(target);
        }

        private static object InvokeOptional(object target, string name, params object[] arguments)
        {
            if (target == null) return null;
            MethodInfo method = target.GetType().GetMethods(AllInstance).FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
            return method == null ? null : method.Invoke(target, arguments);
        }

        private static Observation Obs(bool passed, string detail) { return new Observation { Passed = passed, Detail = detail }; }
        private static bool ContainsAny(string value, params string[] needles) { return needles.Any(needle => value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0); }
        private static bool HasAny(string value, params string[] needles) { return ContainsAny(value ?? string.Empty, needles); }
        private static bool HasAll(string value, params string[] needles) { return needles.All(needle => (value ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0); }
        private static bool HasPassSignal(string value)
        {
            return HasAny(value, "passed=true", "pass=true", "success=true", "isvalid=true", "overall=pass", "result=pass");
        }

        private static string RequireDetail(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException(detail);
            return detail;
        }

        private static string TrimEvidence(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "missing";
            return value.Length <= 1600 ? value : value.Substring(0, 1600) + "...[truncated]";
        }

        private static void Product(List<Check> checks, string id, string matrix, string severity, string expected,
            bool passed, string actual, string reproduction, string files)
        {
            string status = passed ? "PASS" : IsRedBaseline ? "EXPECTED_GAP" : "FAIL";
            string classification = passed ? "NONE" : IsRedBaseline ? "PRODUCT_EXPECTED_GAP" : "PRODUCT_REGRESSION";
            checks.Add(NewCheck(id, matrix, status, classification, severity, expected, actual, reproduction, files));
        }

        private static void Guard(List<Check> checks, string id, string matrix, string severity, string expected,
            bool passed, string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, passed ? "PASS" : "FAIL", passed ? "NONE" : "HUMAN_ADOPTION_REGRESSION",
                severity, expected, actual, reproduction, files));
        }

        private static void Infrastructure(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> verification, string reproduction, string files)
        {
            try { checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, verification(), reproduction, files)); }
            catch (Exception exception)
            {
                checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected,
                    exception.GetType().Name + ": " + exception.Message, reproduction, files));
            }
        }

        private static void Unverified(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", id.Contains("steam") ? "EXTERNAL_RELEASE_GAP" : "HARDWARE_GAP",
                severity, expected, actual, reproduction, files));
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
                greenCompletionCondition = "Fresh Wave 15 GREEN, frozen Wave 16 foundation GREEN after implementation, infrastructure PASS, and zero Wave 17 EXPECTED_GAP/FAIL checks.",
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
            text.AppendLine("PASS/EXPECTED_GAP/FAIL/INFRA_FAIL/UNVERIFIED: " + report.passed + "/" + report.expectedGaps + "/" + report.productFailed + "/" + report.infrastructureFailed + "/" + report.unverified);
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
                NewCheck("W17-I99.play_runner", "infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Wave 17 Play runner emits parseable evidence", exception.ToString(),
                    "Run the Play execute method outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave17PacingHazardRedFirstHardeningRunner.cs")
            };
            WriteReport("wave17-play-contracts", "Wave 17 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: " + exception);
        }

        private static void StopPlayContracts() { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; }

        private static void FinishPlayContracts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }
            bool passed = SessionState.GetBool(PlayExitPassKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing Wave 17 Play result");
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayExitPassKey);
            SessionState.EraseString(PlayMessageKey);
            Debug.Log(message);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value); }
        }
    }
}
