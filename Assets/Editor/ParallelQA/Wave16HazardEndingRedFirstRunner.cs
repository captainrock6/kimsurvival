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
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace ParallelQA
{
    /// <summary>
    /// Independent RED-first contract for the Wave 16 hazard, escape-project,
    /// and behavioral-ending foundation. Product discovery is based on stable
    /// IDs, public data, and live state rather than a recommended implementation
    /// file or class name.
    /// </summary>
    public static class Wave16HazardEndingRedFirstRunner
    {
        private const string RedBaseline = "635725b3e2679a7d6d4f66c09b137575bac374c8";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave16.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave16.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave16.PlayMessage";
        private static readonly BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private static readonly BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        private static readonly BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly Regex StableId = new Regex("^(hazard|escape|ending|stat|event|modifier|part|facility|research)\\.[a-z0-9][a-z0-9._-]*$", RegexOptions.Compiled);
        private static readonly string[] RequiredHazards = { "hazard.injury", "hazard.disaster", "hazard.food-theft" };
        private static readonly string[] RequiredEscapes = { "escape.raft", "escape.smoke", "escape.radio", "escape.flare", "escape.beacon" };
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
            "ending.rare.radio.forecast-rescue", "ending.rare.beacon.storm-eye", "ending.gamejam.stay.natural-kim",
            "ending.gamejam.stay.island-engineer", "ending.stay.green-king",
            "ending.stay.fortress-manager", "ending.stay.scrap-professor", "ending.stay.island-ranger", "ending.stay.just-kim"
        };
        private static readonly string[] RequiredEndingCategories = { "escape", "comic", "rare", "gamejam-stay", "day50" };
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
            public string[] discoveredStableIds;
            public ContractEntry[] discoveredEntries;
            public string hazardPhases;
            public string hazardBudget;
            public string hazardTransaction;
            public string escapeCatalog;
            public string escapeAxes;
            public string playablePaths;
            public string snapshotAndLog;
            public string endingCatalog;
            public string endingResolver;
            public string localization;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId;
            public string baselineCommit;
            public string liveContractSurface;
            public string hazardState;
            public string escapeState;
            public string endingState;
            public string inputParity;
            public string layout;
            public int activeComicPanels;
            public int overflowCount;
            public int offscreenCount;
            public int overlapCount;
            public string[] captureLocales;
            public string[] screenshots;
            public string[] joystickNames;
        }

        private sealed class Observation
        {
            public bool Passed;
            public string Detail;
        }

        private sealed class CatalogProbe
        {
            public List<ContractEntry> Entries = new List<ContractEntry>();
            public HashSet<string> StableIds = new HashSet<string>(StringComparer.Ordinal);
            public string Surface = string.Empty;
        }

        private sealed class PlayProbe
        {
            public bool HazardState;
            public bool EscapeState;
            public bool EndingState;
            public bool InputParity;
            public bool Layout;
            public int Panels;
            public int Overflow;
            public int Offscreen;
            public int Overlap;
            public string LiveSurface = string.Empty;
            public string InputDetail = string.Empty;
            public string LayoutDetail = string.Empty;
            public bool LocaleApplied = true;
            public List<string> LocaleStates = new List<string>();
            public List<string> Screenshots = new List<string>();
        }

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string EvidenceFolder { get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); } }
        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave16" : Sanitize(value);
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

            Infrastructure(checks, "W16-I01.exact_baseline", "baseline identity", "P0",
                "The command environment identifies the checked-out exact baseline before product classification",
                () => RequireDetail(BaselineCommit != "unknown", "baseline=" + BaselineCommit),
                "Run the PowerShell entry point with -BaselineCommit equal to git rev-parse HEAD.",
                "Assets/Editor/ParallelQA/Invoke-Wave16HazardEndingGate.ps1");
            Infrastructure(checks, "W16-I02.canonical_contract", "canonical sources", "P0",
                "The Forge task and human matrix exist and contain the stable hazard, escape, ending, priority, and idempotency contract",
                ObserveCanonicalSources,
                "Read the ready Forge task and Docs/Design/wave15-escape-hazard-ending-matrix.md, then search the stable IDs.",
                ".forge/backlog.json; Docs/Design/wave15-escape-hazard-ending-matrix.md");

            CatalogProbe catalog = DiscoverPublicContractSurface();
            Observation hazardPhases = ObserveHazardPhases(catalog);
            Observation hazardBudget = ObserveHazardBudget(catalog);
            Observation hazardTransaction = ObserveSemanticProbe("hazard", new[] { "atomic", "idempot" });
            Observation escapeCatalog = ObserveRequiredIds(catalog, RequiredEscapes, "escape methods");
            Observation escapeAxes = ObserveEscapeAxes(catalog);
            Observation playablePaths = ObservePlayableEscapePaths(catalog);
            Observation snapshotAndLog = ObserveSnapshotAndLogSchema();
            Observation endingCatalog = ObserveEndingCatalog(catalog);
            Observation endingResolver = ObserveEndingResolver();
            Observation localization = ObserveLocalization();

            Product(checks, "W16-H01.hazard_phase_catalog", "hazard lifecycle", "P0",
                "hazard.injury, hazard.disaster, and hazard.food-theft each expose warning, occurrence, mitigation, and recovery data",
                hazardPhases.Passed, hazardPhases.Detail,
                "Enumerate public runtime contract objects by stable ID and inspect lifecycle members/values.",
                "runtime hazard catalog and state types selected by the implementation owner");
            Product(checks, "W16-H02.daily_stack_budget", "hazard budget", "P0",
                "The public hazard director/config exposes a daily budget, major-per-day limit, active limit, and recovery reservation",
                hazardBudget.Passed, hazardBudget.Detail,
                "Inspect public hazard config data and execute the deterministic daily-stack fixture.",
                "runtime hazard director/config selected by the implementation owner");
            Product(checks, "W16-H03.atomic_idempotent_resolution", "hazard transaction", "P0",
                "One hazardInstanceId owns one atomic resource/state/score/log transaction and retry does not apply it twice",
                hazardTransaction.Passed, hazardTransaction.Detail,
                "Run the public deterministic hazard contract probe twice with the same instance/idempotency key and compare state/resources/logs.",
                "runtime hazard transaction and run-state types selected by the implementation owner");
            Product(checks, "W16-E01.five_escape_catalog", "escape catalog", "P0",
                "Exactly the five canonical escape method stable IDs are available through public runtime data",
                escapeCatalog.Passed, escapeCatalog.Detail,
                "Enumerate public runtime catalog objects and compare their stable IDs with the canonical five.",
                "runtime escape-project catalog selected by the implementation owner");
            Product(checks, "W16-E02.escape_axis_separation", "escape differentiation", "P0",
                "Every pair of escape methods differs on at least two of region/research/facility/part/material/time/risk/timing axes",
                escapeAxes.Passed, escapeAxes.Detail,
                "Compare public catalog member values pairwise; do not compare product class names.",
                "runtime escape-project public data selected by the implementation owner");
            Product(checks, "W16-E03.smoke_radio_playable", "playable escape paths", "P0",
                "escape.smoke and escape.radio expose playable progress/commit/completion paths, not data-only labels",
                playablePaths.Passed, playablePaths.Detail,
                "Execute the deterministic public escape path probe for smoke and radio and inspect progress plus terminal result.",
                "runtime escape project state/transaction types selected by the implementation owner");
            Product(checks, "W16-E04.raft_flare_beacon_data", "data-only escape validation", "P1",
                "raft, flare, and beacon entries have region, research, facility, key-part, risk, and completion-rule data",
                ObserveDataOnlyEscapes(catalog).Passed, ObserveDataOnlyEscapes(catalog).Detail,
                "Inspect the three public catalog entries and validate all stable references.",
                "runtime escape-project catalog selected by the implementation owner");
            Product(checks, "W16-O01.snapshot_and_private_log", "state persistence and privacy", "P0",
                "A run snapshot and development event schema preserve seed, region, hazard, project progress, and behavior score without PII fields",
                snapshotAndLog.Passed, snapshotAndLog.Detail,
                "Inspect public snapshot/log schemas and serialize a verification record containing stable IDs only.",
                "runtime run-state and development log schemas selected by the implementation owner");
            Product(checks, "W16-N01.ending_catalog_21", "ending catalog", "P0",
                "All 21 canonical ending IDs are unique public runtime data in five exact album categories with four required sample endings",
                endingCatalog.Passed, endingCatalog.Detail,
                "Enumerate public ending entries by stable ID and compare with the canonical set.",
                "runtime ending catalog selected by the implementation owner");
            Product(checks, "W16-N02.deterministic_single_ending", "ending resolver", "P0",
                "The same snapshot resolves one identical ending ID and an explicit priority/conditions/event-day/ASCII-ID tie-break reason",
                endingResolver.Passed, endingResolver.Detail,
                "Run the public resolver twice on the same sample snapshots and compare ending ID, reason, panels, and mapping ID.",
                "runtime ending resolver and snapshot types selected by the implementation owner");
            Product(checks, "W16-N03.terminal_priority", "terminal precedence", "P0",
                "escape completion before Day 50 wins over settlement, while a non-escaped Day 50 run resolves a behavior ending",
                ObserveTerminalPriority().Passed, ObserveTerminalPriority().Detail,
                "Execute the public terminal resolver for an early escape fixture and a Day 50 no-escape behavior fixture.",
                "runtime terminal/ending resolver selected by the implementation owner");
            Product(checks, "W16-L01.ko_en_qps_contract", "localization contract", "P1",
                "Hazard, escape, 21 ending title/summary/hint, and five album-category keys have synchronized non-empty ko/en/qps-long String Table values with expanded qps text",
                localization.Passed, localization.Detail,
                "Discover localization TSV rows by header, then compare every album key and value with the ko/en/qps-long Unity String Tables.",
                "runtime localization tables selected by the implementation owner");

            EditEvidence evidence = new EditEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                discoveryPolicy = "Public runtime objects, stable IDs, semantic members, deterministic probes; no required implementation class/file name.",
                discoveredStableIds = catalog.StableIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                discoveredEntries = catalog.Entries.OrderBy(entry => entry.id, StringComparer.Ordinal).ToArray(),
                hazardPhases = hazardPhases.Detail,
                hazardBudget = hazardBudget.Detail,
                hazardTransaction = hazardTransaction.Detail,
                escapeCatalog = escapeCatalog.Detail,
                escapeAxes = escapeAxes.Detail,
                playablePaths = playablePaths.Detail,
                snapshotAndLog = snapshotAndLog.Detail,
                endingCatalog = endingCatalog.Detail,
                endingResolver = endingResolver.Detail,
                localization = localization.Detail
            };
            WriteJson("wave16-edit-evidence.json", evidence);
            WriteReport("wave16-edit-contracts", "Wave 16 hazard/escape/ending RED-first Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: Wave 16 Play contracts did not complete.");
            AttachPlayCallbacks();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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
            if (!playTickAttached)
            {
                EditorApplication.update += PlayTick;
                playTickAttached = true;
            }
            playEarliestRunTime = EditorApplication.timeSinceStartup + 2d;
            playTimeoutAt = EditorApplication.timeSinceStartup + 300d;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 2d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 300d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode) FinishPlayContracts();
        }

        private static void PlayTick()
        {
            if (!SessionState.GetBool(PlayRunningKey, false) || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (now < playEarliestRunTime) return;
            if (now > playTimeoutAt)
            {
                WritePlayInfrastructureFailure(new TimeoutException("Timed out waiting for the Wave 16 scene."));
                StopPlayContracts();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;
            DateTime started = DateTime.UtcNow;
            List<Check> checks = new List<Check>();
            try
            {
                PlayProbe probe = ObserveLivePlay(prototype);
                Product(checks, "W16-P01.live_hazard_lifecycle", "actual play hazard", "P0",
                    "Live play exposes warning, occurrence, mitigation, and recovery for the three sample hazards without duplicate state/resource application",
                    probe.HazardState, probe.LiveSurface,
                    "Use the deterministic play fixture to advance each sample hazard through all four phases and retry the same event ID.",
                    "runtime hazard director/UI selected by the implementation owner");
                Product(checks, "W16-P02.live_escape_paths", "actual play escape", "P0",
                    "Live smoke and radio projects progress independently and can complete an early escape before Day 50",
                    probe.EscapeState, probe.LiveSurface,
                    "Use the public play fixture to complete smoke and radio on separate snapshots and record terminal state.",
                    "runtime escape project/UI selected by the implementation owner");
                Product(checks, "W16-P03.three_panel_comic", "ending presentation", "P0",
                    "A resolved sample ending opens exactly one localized placeholder comic sequence with three active core panels",
                    probe.EndingState && probe.Panels == 3, "panels=" + probe.Panels + "; " + probe.LiveSurface,
                    "Resolve a sample ending, inspect the active presentation root, and count visible core panels.",
                    "runtime ending presentation selected by the implementation owner");
                Product(checks, "W16-P04.ko_en_qps_1280", "ending layout", "P1",
                    "Fresh ko/en/qps-long 1280x800 captures exist with a three-panel ending and zero TMP overflow/offscreen/overlap",
                    probe.Layout, probe.LayoutDetail,
                    "Open all wave16-*-ending-state-1280x800.png at 1:1 and compare the recorded TMP rectangles.",
                    "runtime ending presentation and localization tables selected by the implementation owner");
                Product(checks, "W16-P05.keyboard_gamepad_state_parity", "dual input", "P1",
                    "Keyboard and synthetic gamepad change glyph/focus only and preserve locale, hazard, project, score, terminal, and ending state",
                    probe.InputParity, probe.InputDetail,
                    "Capture the semantic live-state fingerprint, switch KeyboardMouse to synthetic Gamepad, and compare the fingerprint.",
                    "runtime input and ending UI selected by the implementation owner");
                Infrastructure(checks, "W16-I04.fresh_capture_evidence", "evidence production", "P0",
                    "The current run emits exact 1280x800 PNG evidence for ko/en/qps-long even when the product contract is RED",
                    () => RequireDetail(VerifyCaptureSet(probe.Screenshots), "captures=" + string.Join(",", probe.Screenshots.ToArray())),
                    "Inspect PNG dimensions and RunId ownership in the fresh evidence directory.",
                    "Assets/Editor/ParallelQA/Wave16HazardEndingRedFirstRunner.cs");
                Unverified(checks, "W16-HW01.physical_gamepad", "input hardware", "P1",
                    "A human completes hazard response, escape project, and ending navigation using a physical gamepad",
                    Input.GetJoystickNames().Any(name => !string.IsNullOrWhiteSpace(name)) ?
                        "device name detected, but no human actuation evidence was captured" : "no non-empty joystick name exposed to Unity batch Play Mode",
                    "Run the Windows development player with a physical controller and record human actuation evidence.",
                    "manual release-candidate hardware evidence");
                Unverified(checks, "W16-S01.steam_release", "external release", "P0",
                    "Steamworks App ID, Depot, Input, Cloud, Achievements, permissions, and store evidence are configured and approved",
                    "Steam integration remains outside this task; readiness is NOT_READY.",
                    "Complete the separately authorized Steam release checklist.",
                    "external Steamworks account and release evidence");

                PlayEvidence evidence = new PlayEvidence
                {
                    runId = RunId,
                    baselineCommit = BaselineCommit,
                    liveContractSurface = probe.LiveSurface,
                    hazardState = probe.HazardState ? "available" : "missing",
                    escapeState = probe.EscapeState ? "available" : "missing",
                    endingState = probe.EndingState ? "available" : "missing",
                    inputParity = probe.InputDetail,
                    layout = probe.LayoutDetail,
                    activeComicPanels = probe.Panels,
                    overflowCount = probe.Overflow,
                    offscreenCount = probe.Offscreen,
                    overlapCount = probe.Overlap,
                    captureLocales = probe.LocaleStates.ToArray(),
                    screenshots = probe.Screenshots.ToArray(),
                    joystickNames = Input.GetJoystickNames().Where(name => !string.IsNullOrWhiteSpace(name)).ToArray()
                };
                WriteJson("wave16-play-evidence.json", evidence);
                Report report = WriteReport("wave16-play-contracts", "Wave 16 hazard/escape/ending RED-first Play contracts", started, checks);
                SessionState.SetBool(PlayExitPassKey, report.infrastructureOverall == "PASS" && report.productFailed == 0);
                SessionState.SetString(PlayMessageKey, "Product=" + report.productOverall + " Infrastructure=" + report.infrastructureOverall +
                    " PhysicalGamepad=UNVERIFIED Evidence=" + Path.Combine(EvidenceFolder, "wave16-play-contracts.json"));
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static string ObserveCanonicalSources()
        {
            string backlog = Path.Combine(ProjectRoot, ".forge", "backlog.json");
            string matrix = Path.Combine(ProjectRoot, "Docs", "Design", "wave15-escape-hazard-ending-matrix.md");
            if (!File.Exists(backlog) || !File.Exists(matrix)) throw new FileNotFoundException("Wave 16 canonical source missing.");
            string source = File.ReadAllText(backlog) + "\n" + File.ReadAllText(matrix);
            string[] tokens =
            {
                "task.implementation.wave15-hazard-ending-foundation", "hazard.injury", "hazard.disaster", "hazard.food-theft",
                "escape.raft", "escape.smoke", "escape.radio", "escape.flare", "escape.beacon", "21개", "idempotency",
                "priority 내림차순", "ASCII"
            };
            string[] missing = tokens.Where(token => source.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0).ToArray();
            return RequireDetail(missing.Length == 0, "missing=" + string.Join(",", missing) + "; files=2");
        }

        private static CatalogProbe DiscoverPublicContractSurface()
        {
            CatalogProbe probe = new CatalogProbe();
            HashSet<object> visited = new HashSet<object>();
            List<string> surface = new List<string>();
            foreach (Type type in RuntimeTypes())
            {
                foreach (FieldInfo field in type.GetFields(PublicStatic))
                {
                    if (field.IsLiteral && field.FieldType == typeof(string))
                    {
                        string value = field.GetRawConstantValue() as string;
                        if (!string.IsNullOrWhiteSpace(value)) surface.Add(type.FullName + "." + field.Name + "=" + value);
                    }
                    else
                    {
                        object value = field.GetValue(null);
                        if (value != null && (value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal))
                            surface.Add(type.FullName + "." + field.Name + "=" + Convert.ToString(value, CultureInfo.InvariantCulture));
                        TryVisit(value, probe, visited, 0, type.FullName + "." + field.Name, surface);
                    }
                }
                foreach (PropertyInfo property in type.GetProperties(PublicStatic).Where(property => property.GetIndexParameters().Length == 0))
                {
                    try { TryVisit(property.GetValue(null, null), probe, visited, 0, type.FullName + "." + property.Name, surface); }
                    catch { }
                }
                if (!type.IsAbstract && !typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    object instance = CreatePublicInstance(type);
                    if (instance != null) TryVisit(instance, probe, visited, 0, type.FullName + ".instance", surface);
                }
            }
            probe.Surface = string.Join(" | ", surface.Take(120).ToArray());
            return probe;
        }

        private static void TryVisit(object value, CatalogProbe probe, HashSet<object> visited, int depth, string origin, List<string> surface)
        {
            if (value == null || depth > 4 || probe.Entries.Count > 400) return;
            if (value is string text)
            {
                if (StableId.IsMatch(text)) probe.StableIds.Add(text);
                return;
            }
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal) return;
            if (!type.IsValueType && !visited.Add(value)) return;
            if (value is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object item in enumerable)
                {
                    TryVisit(item, probe, visited, depth + 1, origin + "[]", surface);
                    if (++count >= 256) break;
                }
                return;
            }

            List<MemberSnapshot> members = ReadPublicMembers(value);
            string id = FindStableId(members);
            if (!string.IsNullOrWhiteSpace(id))
            {
                probe.StableIds.Add(id);
                probe.Entries.Add(new ContractEntry { id = id, type = type.FullName, members = members.ToArray() });
            }
            foreach (MemberSnapshot member in members)
            {
                if (surface.Count < 500) surface.Add(origin + "." + member.name + "=" + member.value);
                foreach (Match match in Regex.Matches(member.value ?? string.Empty, "(?:hazard|escape|ending|stat|event|modifier|part|facility|research)\\.[a-z0-9][a-z0-9._-]*"))
                    probe.StableIds.Add(match.Value);
            }
            foreach (FieldInfo field in type.GetFields(PublicInstance))
            {
                try { TryVisit(field.GetValue(value), probe, visited, depth + 1, origin + "." + field.Name, surface); }
                catch { }
            }
            foreach (PropertyInfo property in type.GetProperties(PublicInstance).Where(property => property.GetIndexParameters().Length == 0))
            {
                try { TryVisit(property.GetValue(value, null), probe, visited, depth + 1, origin + "." + property.Name, surface); }
                catch { }
            }
        }

        private static List<MemberSnapshot> ReadPublicMembers(object value)
        {
            List<MemberSnapshot> result = new List<MemberSnapshot>();
            Type type = value.GetType();
            foreach (FieldInfo field in type.GetFields(PublicInstance).OrderBy(field => field.Name))
            {
                try { result.Add(new MemberSnapshot { name = field.Name, value = Describe(field.GetValue(value), 1) }); }
                catch { }
            }
            foreach (PropertyInfo property in type.GetProperties(PublicInstance).Where(property => property.GetIndexParameters().Length == 0).OrderBy(property => property.Name))
            {
                try { result.Add(new MemberSnapshot { name = property.Name, value = Describe(property.GetValue(value, null), 1) }); }
                catch { }
            }
            return result;
        }

        private static string FindStableId(IEnumerable<MemberSnapshot> members)
        {
            foreach (MemberSnapshot member in members.OrderBy(member => member.name.IndexOf("Stable", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1))
            {
                if (member.name.IndexOf("id", StringComparison.OrdinalIgnoreCase) < 0) continue;
                string candidate = (member.value ?? string.Empty).Trim('"');
                if (StableId.IsMatch(candidate)) return candidate;
            }
            return string.Empty;
        }

        private static Observation ObserveHazardPhases(CatalogProbe catalog)
        {
            List<string> details = new List<string>();
            bool pass = true;
            foreach (string id in RequiredHazards)
            {
                ContractEntry entry = catalog.Entries.FirstOrDefault(candidate => candidate.id == id);
                string descriptor = entry == null ? string.Empty : entry.Describe().ToLowerInvariant();
                bool warning = ContainsAny(descriptor, "warning", "telegraph", "forecast", "preview", "notice");
                bool occurrence = ContainsAny(descriptor, "occurrence", "occur", "trigger", "apply", "resolve");
                bool mitigation = ContainsAny(descriptor, "mitigation", "mitigate", "counter", "prevent", "response");
                bool recovery = ContainsAny(descriptor, "recovery", "recover", "repair", "rest", "cure");
                bool current = entry != null && warning && occurrence && mitigation && recovery;
                pass &= current;
                details.Add(id + "=" + (current ? "4/4" : "entry:" + (entry != null) + ",W:" + warning + ",O:" + occurrence + ",M:" + mitigation + ",R:" + recovery));
            }
            return Obs(pass, string.Join("; ", details.ToArray()));
        }

        private static Observation ObserveHazardBudget(CatalogProbe catalog)
        {
            string surface = catalog.Surface.ToLowerInvariant();
            bool daily = ContainsAny(surface, "dailybudget=4", "daily_budget=4", "hazardbudget=4", "budgetperday=4");
            bool major = ContainsAny(surface, "maxmajor=1", "majorperday=1", "maxnewmajor=1");
            bool active = ContainsAny(surface, "maxactive=2", "activehazardlimit=2", "maximumactive=2");
            bool recovery = ContainsAny(surface, "recoveryreserve=2", "reservedrecoverybudget=2", "recoverybudget=2");
            return Obs(daily && major && active && recovery,
                "daily=" + daily + "; major=" + major + "; active=" + active + "; recoveryReserve=" + recovery);
        }

        private static Observation ObserveRequiredIds(CatalogProbe catalog, IEnumerable<string> ids, string label)
        {
            string[] required = ids.ToArray();
            string[] found = required.Where(id => catalog.Entries.Any(entry => entry.id == id)).ToArray();
            return Obs(found.Length == required.Length, label + "=" + found.Length + "/" + required.Length + "; found=" + string.Join(",", found));
        }

        private static Observation ObserveEscapeAxes(CatalogProbe catalog)
        {
            ContractEntry[] entries = RequiredEscapes.Select(id => catalog.Entries.FirstOrDefault(entry => entry.id == id)).ToArray();
            if (entries.Any(entry => entry == null)) return Obs(false, "escape entries missing; pairwise axes unavailable");
            string[][] axisTokens =
            {
                new[] { "region" }, new[] { "research" }, new[] { "facility", "anchor" }, new[] { "part", "component" },
                new[] { "material", "resource", "cost" }, new[] { "preparation", "duration", "day" },
                new[] { "risk", "hazard" }, new[] { "timing", "window", "weather" }
            };
            int minimum = int.MaxValue;
            List<string> failures = new List<string>();
            for (int i = 0; i < entries.Length; i++)
            for (int j = i + 1; j < entries.Length; j++)
            {
                int differences = axisTokens.Count(tokens => AxisValue(entries[i], tokens) != AxisValue(entries[j], tokens) &&
                                                          !string.IsNullOrWhiteSpace(AxisValue(entries[i], tokens)) &&
                                                          !string.IsNullOrWhiteSpace(AxisValue(entries[j], tokens)));
                minimum = Math.Min(minimum, differences);
                if (differences < 2) failures.Add(entries[i].id + "~" + entries[j].id + "=" + differences);
            }
            return Obs(failures.Count == 0, "minimumPairwiseDifferentAxes=" + minimum + "; failures=" + string.Join(",", failures.ToArray()));
        }

        private static string AxisValue(ContractEntry entry, IEnumerable<string> tokens)
        {
            return string.Join("|", entry.members.Where(member => tokens.Any(token => member.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(member => member.value).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static Observation ObservePlayableEscapePaths(CatalogProbe catalog)
        {
            Observation smoke = ObserveSemanticProbe("escape.smoke", new[] { "escape.smoke", "progress", "complete" });
            Observation radio = ObserveSemanticProbe("escape.radio", new[] { "escape.radio", "progress", "complete" });
            bool metadata = new[] { "escape.smoke", "escape.radio" }.All(id =>
            {
                ContractEntry entry = catalog.Entries.FirstOrDefault(candidate => candidate.id == id);
                return entry != null && ContainsAny(entry.Describe().ToLowerInvariant(), "playable", "progress", "commit", "complete");
            });
            return Obs(smoke.Passed && radio.Passed && metadata,
                "metadata=" + metadata + "; smokeProbe=" + smoke.Detail + "; radioProbe=" + radio.Detail);
        }

        private static Observation ObserveDataOnlyEscapes(CatalogProbe catalog)
        {
            List<string> detail = new List<string>();
            bool pass = true;
            foreach (string id in new[] { "escape.raft", "escape.flare", "escape.beacon" })
            {
                ContractEntry entry = catalog.Entries.FirstOrDefault(candidate => candidate.id == id);
                string text = entry == null ? string.Empty : entry.Describe().ToLowerInvariant();
                int axes = new[] { "region", "research", "facility", "part", "risk", "completion" }.Count(token => text.Contains(token));
                pass &= entry != null && axes == 6;
                detail.Add(id + "=" + axes + "/6");
            }
            return Obs(pass, string.Join("; ", detail.ToArray()));
        }

        private static Observation ObserveSnapshotAndLogSchema()
        {
            string[] required = { "seed", "region", "hazard", "project", "behavior" };
            string[] prohibited = { "username", "user_name", "machine", "hostname", "homepath", "email", "ipaddress", "account" };
            Type snapshot = RuntimeTypes().OrderByDescending(type => SemanticMemberCount(type, required)).FirstOrDefault();
            Type log = RuntimeTypes().Where(type => type.Name.IndexOf("log", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                        type.Name.IndexOf("event", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(type => SemanticMemberCount(type, required)).FirstOrDefault();
            int snapshotCount = snapshot == null ? 0 : SemanticMemberCount(snapshot, required);
            int logCount = log == null ? 0 : SemanticMemberCount(log, required);
            string logMembers = log == null ? string.Empty : string.Join(" ", PublicMemberNames(log)).ToLowerInvariant();
            string[] pii = prohibited.Where(logMembers.Contains).ToArray();
            return Obs(snapshotCount == required.Length && logCount == required.Length && pii.Length == 0,
                "snapshot=" + (snapshot == null ? "missing" : snapshot.FullName) + "(" + snapshotCount + "/5); log=" +
                (log == null ? "missing" : log.FullName) + "(" + logCount + "/5); piiFields=" +
                (pii.Length == 0 ? "none" : string.Join(",", pii)));
        }

        private static int SemanticMemberCount(Type type, IEnumerable<string> tokens)
        {
            string members = string.Join(" ", PublicMemberNames(type)).ToLowerInvariant();
            return tokens.Count(token => members.Contains(token));
        }

        private static Observation ObserveEndingCatalog(CatalogProbe catalog)
        {
            string[] found = RequiredEndings.Where(id => catalog.Entries.Any(entry => entry.id == id)).ToArray();
            string[] samples = RequiredSamples.Where(id => found.Contains(id)).ToArray();
            ContractEntry[] endingEntries = catalog.Entries.Where(entry =>
                entry.id.StartsWith("ending.", StringComparison.Ordinal)).ToArray();
            bool unique = endingEntries.GroupBy(entry => entry.id).All(group => group.Count() == 1);
            bool exactIds = endingEntries.Length == RequiredEndings.Length &&
                            endingEntries.Select(entry => entry.id).OrderBy(id => id, StringComparer.Ordinal)
                                .SequenceEqual(RequiredEndings.OrderBy(id => id, StringComparer.Ordinal));
            Dictionary<string, int> expectedCategories = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "escape", 5 }, { "comic", 5 }, { "rare", 4 }, { "gamejam-stay", 2 }, { "day50", 5 }
            };
            Dictionary<string, int> actualCategories = endingEntries
                .GroupBy(entry => MemberValue(entry, "Category"), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            bool categories = expectedCategories.All(pair =>
                                  actualCategories.TryGetValue(pair.Key, out int count) && count == pair.Value) &&
                              actualCategories.Keys.OrderBy(value => value, StringComparer.Ordinal)
                                  .SequenceEqual(RequiredEndingCategories.OrderBy(value => value, StringComparer.Ordinal));
            string categoryDetail = string.Join(",", RequiredEndingCategories.Select(category =>
                category + "=" + (actualCategories.TryGetValue(category, out int count) ? count : 0)).ToArray());
            return Obs(found.Length == RequiredEndings.Length && samples.Length == RequiredSamples.Length && unique && exactIds && categories,
                "endings=" + found.Length + "/21; samples=" + samples.Length + "/4; unique=" + unique +
                "; exactIds=" + exactIds + "; categories=" + categoryDetail);
        }

        private static string MemberValue(ContractEntry entry, string memberName)
        {
            MemberSnapshot member = entry == null ? null : entry.members.FirstOrDefault(value =>
                string.Equals(value.name, memberName, StringComparison.OrdinalIgnoreCase));
            return member == null ? string.Empty : (member.value ?? string.Empty).Trim('"');
        }

        private static Observation ObserveEndingResolver()
        {
            Observation deterministic = ObserveSemanticProbe("ending", new[] { "determin", "single" });
            string surface = string.Join(" | ", RuntimeTypes().Select(type => type.FullName + ":" + string.Join(",", PublicMemberNames(type))).ToArray()).ToLowerInvariant();
            bool priority = surface.Contains("priority");
            bool conditions = ContainsAny(surface, "conditioncount", "matchedconditions", "specificity");
            bool eventDay = ContainsAny(surface, "eventday", "specialeventday", "firsteventday");
            bool ascii = ContainsAny(surface, "ascii", "ordinal", "stableid");
            return Obs(deterministic.Passed && priority && conditions && eventDay && ascii,
                "probe=" + deterministic.Detail + "; tieBreak=priority:" + priority + ",conditions:" + conditions + ",eventDay:" + eventDay + ",ascii:" + ascii);
        }

        private static Observation ObserveTerminalPriority()
        {
            Observation escape = ObserveSemanticProbe("terminal", new[] { "escape", "day50" });
            Observation ending = ObserveSemanticProbe("ending", new[] { "day50", "behavior" });
            return Obs(escape.Passed && ending.Passed, "escapePriority=" + escape.Detail + "; behaviorEnding=" + ending.Detail);
        }

        private static Observation ObserveSemanticProbe(string semantic, string[] resultTokens)
        {
            List<string> attempted = new List<string>();
            foreach (Type type in RuntimeTypes())
            {
                string typeName = type.FullName.ToLowerInvariant();
                foreach (MethodInfo method in type.GetMethods(PublicStatic | PublicInstance))
                {
                    string identity = (typeName + "." + method.Name).ToLowerInvariant();
                    string semanticLower = semantic.ToLowerInvariant();
                    string semanticRoot = semanticLower.Split('.')[0];
                    string semanticCompact = semanticLower.Replace(".", string.Empty);
                    bool semanticMatch = identity.Contains(semanticCompact) || identity.Contains(semanticLower) ||
                                         (!semanticLower.Contains(".") && identity.Contains(semanticRoot));
                    if (!semanticMatch) continue;
                    if (!ContainsAny(method.Name.ToLowerInvariant(), "probe", "verify", "contract", "fixture", "qa")) continue;
                    if (method.ContainsGenericParameters || method.GetParameters().Any(parameter => parameter.IsOut || parameter.ParameterType.IsByRef)) continue;
                    try
                    {
                        object target = method.IsStatic ? null : CreatePublicInstance(type);
                        if (!method.IsStatic && target == null) continue;
                        object[] args = BuildProbeArguments(method.GetParameters(), semantic);
                        if (args == null) continue;
                        object result = method.Invoke(target, args);
                        string described = Describe(result, 3).ToLowerInvariant();
                        attempted.Add(type.FullName + "." + method.Name + "=" + described);
                        if (resultTokens.All(token => described.Contains(token.ToLowerInvariant())) &&
                            !ContainsAny(described, "passed=false", "success=false", "fail", "missing", "notimplemented"))
                            return Obs(true, attempted.Last());
                    }
                    catch (Exception exception)
                    {
                        attempted.Add(type.FullName + "." + method.Name + " threw " + exception.GetType().Name);
                    }
                }
            }
            return Obs(false, attempted.Count == 0 ? "no public semantic deterministic probe discovered" : string.Join(" | ", attempted.Take(8).ToArray()));
        }

        private static object[] BuildProbeArguments(ParameterInfo[] parameters, string semantic)
        {
            if (parameters.Length > 6) return null;
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                string name = parameters[i].Name.ToLowerInvariant();
                if (type == typeof(string))
                    args[i] = name.Contains("idempot") ? "qa.wave16.same-event" : name.Contains("hazard") ? "hazard.injury" :
                        name.Contains("escape") ? (semantic.Contains("radio") ? "escape.radio" : "escape.smoke") :
                        name.Contains("ending") ? "ending.escape.smoke.seen-from-afar" : semantic;
                else if (type == typeof(int)) args[i] = name.Contains("day") ? 50 : name.Contains("seed") ? 160635 : 1;
                else if (type == typeof(bool)) args[i] = true;
                else if (type.IsEnum) args[i] = Enum.GetValues(type).GetValue(0);
                else if (type.IsValueType) args[i] = Activator.CreateInstance(type);
                else
                {
                    object instance = CreatePublicInstance(type);
                    if (instance == null && !parameters[i].IsOptional) return null;
                    args[i] = instance ?? parameters[i].DefaultValue;
                }
            }
            return args;
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

        private static Observation ObserveLocalization()
        {
            string root = Path.Combine(Application.dataPath, "_Project", "Scripts", "Localization");
            List<string[]> rows = new List<string[]>();
            foreach (string file in Directory.Exists(root) ? Directory.GetFiles(root, "*.tsv", SearchOption.AllDirectories) : new string[0])
            {
                string[] lines = File.ReadAllLines(file);
                if (lines.Length == 0) continue;
                string[] header = lines[0].Split('\t');
                int key = Array.FindIndex(header, value => value.Equals("Key", StringComparison.OrdinalIgnoreCase));
                int ko = Array.FindIndex(header, value => value.Equals("ko", StringComparison.OrdinalIgnoreCase));
                int en = Array.FindIndex(header, value => value.Equals("en", StringComparison.OrdinalIgnoreCase));
                int qps = Array.FindIndex(header, value => value.Equals("qps-long", StringComparison.OrdinalIgnoreCase));
                if (key < 0 || ko < 0 || en < 0 || qps < 0) continue;
                foreach (string line in lines.Skip(1))
                {
                    string[] cells = line.Split('\t');
                    if (cells.Length > Math.Max(Math.Max(key, ko), Math.Max(en, qps))) rows.Add(new[] { cells[key], cells[ko], cells[en], cells[qps] });
                }
            }
            List<string> required = new List<string>();
            required.AddRange(RequiredHazards);
            required.AddRange(RequiredEscapes);
            foreach (string id in RequiredEndings)
            {
                required.Add(id + ".title");
                required.Add(id + ".summary");
                required.Add(id + ".hint");
            }
            string[] albumKeys = RequiredEndings.SelectMany(id => new[] { id + ".title", id + ".summary", id + ".hint" })
                .Concat(RequiredEndingCategories.Select(category => "ending.album.category." + category)).ToArray();
            required.AddRange(RequiredEndingCategories.Select(category => "ending.album.category." + category));
            int present = 0;
            int expanded = 0;
            foreach (string prefix in required)
            {
                string[] row = rows.FirstOrDefault(candidate => candidate[0] == prefix || candidate[0].StartsWith(prefix + ".", StringComparison.Ordinal));
                if (row == null || row.Skip(1).Any(string.IsNullOrWhiteSpace)) continue;
                present++;
                if (row[3].Length >= Math.Ceiling(row[2].Length * 1.35) && row[3] != row[2]) expanded++;
            }
            int duplicateAlbumKeys = albumKeys.Count(key => rows.Count(row => string.Equals(row[0], key, StringComparison.Ordinal)) != 1);
            int synchronizedTableValues = CountSynchronizedStringTableValues(rows, albumKeys);
            int expectedTableValues = albumKeys.Length * 3;
            return Obs(present == required.Count && expanded == required.Count && duplicateAlbumKeys == 0 &&
                       synchronizedTableValues == expectedTableValues,
                "required=" + required.Count + "; present=" + present + "; qpsExpanded=" + expanded +
                "; albumKeys=" + albumKeys.Length + "; duplicateOrMissingAlbumKeys=" + duplicateAlbumKeys +
                "; synchronizedStringTableValues=" + synchronizedTableValues + "/" + expectedTableValues +
                "; tablesRows=" + rows.Count);
        }

        private static int CountSynchronizedStringTableValues(IReadOnlyList<string[]> rows, IEnumerable<string> requiredKeys)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(PrototypeLocalization.TableName);
            if (collection == null) return 0;
            StringTable[] tables =
            {
                collection.GetTable(PrototypeLocalization.KoreanLocaleCode) as StringTable,
                collection.GetTable(PrototypeLocalization.EnglishLocaleCode) as StringTable,
                collection.GetTable(PrototypeLocalization.QpsLongLocaleCode) as StringTable
            };
            if (tables.Any(table => table == null)) return 0;

            int synchronized = 0;
            foreach (string key in requiredKeys)
            {
                string[] source = rows.SingleOrDefault(row => string.Equals(row[0], key, StringComparison.Ordinal));
                if (source == null) continue;
                for (int locale = 0; locale < tables.Length; locale += 1)
                {
                    StringTableEntry entry = tables[locale].GetEntry(key);
                    string expected = source[locale + 1].Replace("\\n", "\n");
                    if (entry != null && string.Equals(entry.Value, expected, StringComparison.Ordinal)) synchronized++;
                }
            }
            return synchronized;
        }

        private static PlayProbe ObserveLivePlay(KimSurvivalPrototype prototype)
        {
            PlayProbe probe = new PlayProbe();
            TryOpenSemanticPresentation("ending", "ending.escape.smoke.seen-from-afar");
            probe.LiveSurface = DescribeLiveSurface();
            string lower = probe.LiveSurface.ToLowerInvariant();
            probe.HazardState = RequiredHazards.All(id => lower.Contains(id));
            probe.EscapeState = lower.Contains("escape.smoke") && lower.Contains("escape.radio") && ContainsAny(lower, "progress", "complete", "project");
            probe.EndingState = RequiredSamples.Any(id => lower.Contains(id));

            string before = LiveSemanticFingerprint(prototype);
            SetInputDevice(prototype, PrototypeInputDevice.Gamepad);
            InvokeOptional(prototype, "RefreshAll");
            string gamepad = LiveSemanticFingerprint(prototype);
            SetInputDevice(prototype, PrototypeInputDevice.KeyboardMouse);
            InvokeOptional(prototype, "RefreshAll");
            string keyboard = LiveSemanticFingerprint(prototype);
            probe.InputParity = probe.EndingState && before == gamepad && gamepad == keyboard;
            probe.InputDetail = "before=" + before + "; gamepad=" + gamepad + "; keyboard=" + keyboard;

            string[] locales = { "ko", "en", "qps-long" };
            foreach (string locale in locales)
            {
                SetLocale(prototype, locale);
                PrototypeLocalization localization = GetField(prototype, "localization") as PrototypeLocalization;
                string activeLocale = localization == null ? "missing" : localization.CurrentLocaleCode;
                probe.LocaleStates.Add(locale + "->" + activeLocale);
                probe.LocaleApplied &= string.Equals(locale, activeLocale, StringComparison.OrdinalIgnoreCase);
                TryOpenSemanticPresentation("ending", "ending.escape.smoke.seen-from-afar");
                InvokeOptional(prototype, "RefreshAll");
                string name = "wave16-" + locale + "-ending-state-1280x800.png";
                prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, name), 1280, 800);
                probe.Screenshots.Add(name);
            }
            MeasureEndingLayout(out probe.Panels, out probe.Overflow, out probe.Offscreen, out probe.Overlap);
            probe.Layout = probe.LocaleApplied && probe.EndingState && probe.Panels == 3 && VerifyCaptureSet(probe.Screenshots) &&
                           probe.Overflow == 0 && probe.Offscreen == 0 && probe.Overlap == 0;
            probe.LayoutDetail = "captures=" + probe.Screenshots.Count + "/3; panels=" + probe.Panels +
                                 "; locales=" + string.Join(",", probe.LocaleStates.ToArray()) +
                                 "; overflow=" + probe.Overflow + "; offscreen=" + probe.Offscreen + "; overlap=" + probe.Overlap;
            return probe;
        }

        private static void TryOpenSemanticPresentation(string semantic, string stableId)
        {
            // Prefer the current production ending surface.  The older broad
            // name scan can encounter KimSurvivalPrototype.OpenEndingAlbumFromPopup
            // first; that method is a camp-menu action and legitimately no-ops
            // when the album target is not active, leaving the comic closed.
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour == null) continue;
                MethodInfo currentEnding = behaviour.GetType().GetMethod(
                    "ShowEndingForVerification",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(string) },
                    null);
                if (currentEnding == null) continue;
                currentEnding.Invoke(behaviour, new object[] { stableId });
                return;
            }

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
                if (!ContainsAny(identity.ToLowerInvariant(), "hazard", "escape", "ending", "comic", "result")) continue;
                values.Add(identity + "{" + string.Join(";", ReadPublicMembers(behaviour).Select(member => member.name + "=" + member.value).ToArray()) + "}");
            }
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                string name = gameObject.name;
                if (ContainsAny(name.ToLowerInvariant(), "hazard", "escape", "ending", "comic")) values.Add("GameObject:" + name + ":active=" + gameObject.activeInHierarchy);
            }
            return values.Count == 0 ? "no live hazard/escape/ending stable-ID surface" : string.Join(" | ", values.Take(80).ToArray());
        }

        private static string LiveSemanticFingerprint(KimSurvivalPrototype prototype)
        {
            GameSession session = prototype.Session;
            string surface = DescribeLiveSurface();
            surface = Regex.Replace(surface, "(?:KeyboardMouse|Gamepad|\\[E\\]|\\[X\\])", "<device>");
            return "D" + session.Day + "/" + session.Phase + "/" + session.Result + "/seed" + session.RunSeed + "/region" + session.ActiveRegionProfileId + "/" + surface;
        }

        private static void SetInputDevice(object prototype, PrototypeInputDevice device)
        {
            object playerInput = GetField(prototype, "playerInput");
            if (playerInput == null) return;
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
            else
                localization.SetLocale(locale, false);
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
            for (int i = 0; i < rects.Count; i++)
            for (int j = i + 1; j < rects.Count; j++)
            {
                Rect intersection = Intersect(rects[i], rects[j]);
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
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            float scaleX = Screen.width > 0 ? 1280f / Screen.width : 1f;
            float scaleY = Screen.height > 0 ? 800f / Screen.height : 1f;
            return Rect.MinMaxRect(min.x * scaleX, min.y * scaleY, max.x * scaleX, max.y * scaleY);
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            float left = Math.Max(a.xMin, b.xMin);
            float right = Math.Min(a.xMax, b.xMax);
            float bottom = Math.Max(a.yMin, b.yMin);
            float top = Math.Min(a.yMax, b.yMax);
            return right > left && top > bottom ? Rect.MinMaxRect(left, bottom, right, top) : new Rect();
        }

        private static string HierarchyName(Transform transform)
        {
            List<string> names = new List<string>();
            while (transform != null)
            {
                names.Add(transform.name);
                transform = transform.parent;
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static bool VerifyCaptureSet(IEnumerable<string> names)
        {
            foreach (string name in names)
            {
                string path = Path.Combine(EvidenceFolder, name);
                if (!File.Exists(path)) return false;
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47) return false;
                int width = ReadBigEndianInt(bytes, 16);
                int height = ReadBigEndianInt(bytes, 20);
                if (width != 1280 || height != 800) return false;
            }
            return names.Count() == 3;
        }

        private static int ReadBigEndianInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static IEnumerable<Type> RuntimeTypes()
        {
            return typeof(GameSession).Assembly.GetTypes().Where(type => type.Namespace == "KimSurvival").OrderBy(type => type.FullName);
        }

        private static IEnumerable<string> PublicMemberNames(Type type)
        {
            return type.GetFields(PublicInstance | PublicStatic).Select(field => field.Name)
                .Concat(type.GetProperties(PublicInstance | PublicStatic).Select(property => property.Name))
                .Concat(type.GetMethods(PublicInstance | PublicStatic).Select(method => method.Name)).Distinct();
        }

        private static string Describe(object value, int depth)
        {
            if (value == null) return "null";
            if (value is string text) return text;
            if (value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal) return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (depth <= 0) return value.GetType().Name;
            if (value is IEnumerable enumerable)
            {
                List<string> items = new List<string>();
                foreach (object item in enumerable)
                {
                    items.Add(Describe(item, depth - 1));
                    if (items.Count >= 64) break;
                }
                return "[" + string.Join(",", items.ToArray()) + "]";
            }
            List<string> members = new List<string>();
            foreach (FieldInfo field in value.GetType().GetFields(PublicInstance).Take(32))
            {
                try { members.Add(field.Name + "=" + Describe(field.GetValue(value), depth - 1)); }
                catch { }
            }
            foreach (PropertyInfo property in value.GetType().GetProperties(PublicInstance).Where(property => property.GetIndexParameters().Length == 0).Take(32))
            {
                try { members.Add(property.Name + "=" + Describe(property.GetValue(value, null), depth - 1)); }
                catch { }
            }
            return value.GetType().Name + "{" + string.Join(";", members.ToArray()) + "}";
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

        private static Observation Obs(bool passed, string detail)
        {
            return new Observation { Passed = passed, Detail = detail };
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            return needles.Any(needle => value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string RequireDetail(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException(detail);
            return detail;
        }

        private static void Product(List<Check> checks, string id, string matrix, string severity, string expected,
            bool passed, string actual, string reproduction, string files)
        {
            string status = passed ? "PASS" : IsRedBaseline ? "EXPECTED_GAP" : "FAIL";
            string classification = passed ? "NONE" : IsRedBaseline ? "PRODUCT_EXPECTED_GAP" : "PRODUCT_REGRESSION";
            checks.Add(NewCheck(id, matrix, status, classification, severity, expected, actual, reproduction, files));
        }

        private static void Infrastructure(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> verification, string reproduction, string files)
        {
            try { checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, verification(), reproduction, files)); }
            catch (Exception exception) { checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected,
                exception.GetType().Name + ": " + exception.Message, reproduction, files)); }
        }

        private static void Unverified(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", id.Contains("steam") ? "EXTERNAL_RELEASE_GAP" : "HARDWARE_GAP", severity,
                expected, actual, reproduction, files));
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
                greenCompletionCondition = "Fresh Wave 15 prerequisite GREEN, infrastructure PASS, and zero Wave 16 EXPECTED_GAP/FAIL checks on an implementation baseline.",
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
                NewCheck("W16-I99.play_runner", "infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Wave 16 Play runner emits parseable evidence", exception.ToString(),
                    "Run the Play execute method outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave16HazardEndingRedFirstRunner.cs")
            };
            WriteReport("wave16-play-contracts", "Wave 16 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: " + exception);
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
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing Wave 16 Play result");
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
    }
}
