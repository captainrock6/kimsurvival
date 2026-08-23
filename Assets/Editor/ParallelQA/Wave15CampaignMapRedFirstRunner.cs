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
using UnityEngine.EventSystems;

namespace ParallelQA
{
    /// <summary>
    /// Evidence-only Wave 15 campaign/map contract. Product gaps on the exact
    /// 7796cf5 baseline are RED_EXPECTED_GAP; the same failed assertions become
    /// product regressions on every later baseline. No runtime state is persisted.
    /// </summary>
    public static class Wave15CampaignMapRedFirstRunner
    {
        private const string RedBaseline = "7796cf57568d0bad24595379e833e1dd9b4d8d3f";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave15.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave15.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave15.PlayMessage";
        private const float UseRange = 1.25f;
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
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
        private sealed class EditEvidence
        {
            public string runId;
            public string baselineCommit;
            public int observedFinalDay;
            public string dayOne;
            public string day49;
            public string day50;
            public string earlyEscape;
            public string catalog;
            public string rng;
            public string localization;
            public string logging;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId;
            public string baselineCommit;
            public string targetDiscovery;
            public string stateTransition;
            public string regionCards;
            public string inputParity;
            public string layout;
            public string progressFingerprint;
            public string[] screenshots;
            public string[] joystickNames;
        }

        private sealed class Observation
        {
            public bool Passed;
            public string Detail;
        }

        private sealed class MapProbe
        {
            public bool TargetFound;
            public object TargetKind;
            public Vector2 TargetPosition;
            public string TargetId = string.Empty;
            public bool FarHidden;
            public bool NearSinglePrompt;
            public bool PopupOpened;
            public bool CancelRestored;
            public bool RegionCardSemantics;
            public bool InputParity;
            public bool LayoutPass;
            public string Detail = string.Empty;
            public string InputDetail = string.Empty;
            public string LayoutDetail = string.Empty;
            public List<string> Screenshots = new List<string>();
        }

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave15" : Sanitize(value);
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
        private static string EvidenceFolder { get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); } }
        private static bool IsRedBaseline { get { return string.Equals(BaselineCommit, RedBaseline, StringComparison.Ordinal); } }

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<Check> checks = new List<Check>();
            EditEvidence evidence = new EditEvidence { runId = RunId, baselineCommit = BaselineCommit };

            Infrastructure(checks, "W15-I01.exact_baseline", "baseline identity", "P0",
                "The command environment identifies the checked out exact baseline",
                () => RequireDetail(!string.IsNullOrWhiteSpace(BaselineCommit) && BaselineCommit != "unknown", "baseline=" + BaselineCommit),
                "Run the PowerShell entry point with -BaselineCommit equal to git rev-parse HEAD.",
                "Assets/Editor/ParallelQA/Invoke-Wave15CampaignMapGate.ps1");

            Infrastructure(checks, "W15-I02.canonical_sources", "canonical contract", "P0",
                "All four Wave 15 canonical sources exist and identify Day 50, the expedition map, seed protection, and this QA task",
                ObserveCanonicalSources,
                "Open the four canonical sources and search their stable task/feature/system IDs.",
                ".forge/design/project.json; .forge/design/vertical-slice.json; .forge/packets/wave15-fifty-day-campaign-rebaseline.json; Docs/QA/wave15-fifty-day-rebaseline-integration.md");

            Observation deadline = ObserveDeadline(out string dayOne, out string day49, out string day50);
            evidence.observedFinalDay = GameSession.FinalDay;
            evidence.dayOne = dayOne;
            evidence.day49 = day49;
            evidence.day50 = day50;
            Product(checks, "W15-D01.day_1_of_50", "50-day phase flow", "P0",
                "A new standard run starts at Day 1 with canonical FinalDay=50",
                GameSession.FinalDay == 50 && dayOne.Contains("D1/50"), dayOne,
                "Instantiate GameSession and inspect Day plus FinalDay without grants or warp.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");
            Product(checks, "W15-D02.day49_continues", "50-day phase flow", "P0",
                "A non-terminal Day 49 settlement advances to playable Day 50",
                deadline.Passed && day49.Contains("D50/Camp/None"), day49,
                "Set a clean session to Day 49 camp-returned state and call EndDay once.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Runtime/PrototypeCampaignFlow.cs");
            Product(checks, "W15-D03.day50_terminal", "50-day phase flow", "P0",
                "A non-escaped Day 50 settlement resolves the terminal deadline exactly once",
                GameSession.FinalDay == 50 && day50.Contains("Result/Deadline"), day50,
                "Set a clean session to Day 50 camp-returned state and call EndDay once.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Runtime/PrototypeCampaignFlow.cs");

            Observation earlyEscape = ObserveEarlyEscapePriority();
            evidence.earlyEscape = earlyEscape.Detail;
            Product(checks, "W15-D04.early_escape_priority", "terminal priority", "P0",
                "A completed escape on the deadline day resolves Rescued before deadline failure",
                earlyEscape.Passed, earlyEscape.Detail,
                "Prepare the signal path on FinalDay, complete it, then verify Result=Rescued before settlement.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Runtime/PrototypeCampaignFlow.cs");

            Observation catalog = ObserveRegionCatalog();
            evidence.catalog = catalog.Detail;
            Product(checks, "W15-M01.three_region_forecast_catalog", "expedition region cards", "P0",
                "beach, forest, and shallow-sea definitions expose resource category, relative abundance, travel time, risk, weather, gear, special discovery, and unknown state without exact forecast quantities",
                catalog.Passed, catalog.Detail,
                "Reflect the runtime expedition/region catalog and inspect every public forecast member.",
                "Assets/_Project/Scripts/Runtime/PrototypeExpeditionRegionCatalog.cs; Assets/_Project/Scripts/Runtime/PrototypeExpeditionMap.cs");

            Observation rng = ObserveRngContract(out bool sameSeed, out bool differentSeed, out bool protectedRoutes);
            evidence.rng = rng.Detail;
            Product(checks, "W15-R01.same_seed_reproducible", "seeded loot", "P0",
                "The same seed + region + action produces the same machine-readable outcome",
                sameSeed, rng.Detail,
                "Invoke the runtime region-loot generator twice with seed=41017, region.beach, action.search.",
                "Assets/_Project/Scripts/Runtime/PrototypeRegionLootRng.cs");
            Product(checks, "W15-R02.different_seed_bounded_variation", "seeded loot", "P0",
                "At least one different seed changes the outcome while every result remains within its declared profile bounds",
                differentSeed, rng.Detail,
                "Compare seeds 41017, 41018, and 51017 for the same region/action and validate declared bounds.",
                "Assets/_Project/Scripts/Runtime/PrototypeRegionLootRng.cs; Assets/_Project/Scripts/Runtime/PrototypeExpeditionRegionCatalog.cs");
            Product(checks, "W15-R03.softlock_protection", "critical-part protection", "P0",
                "Critical-part guarantee/alternative/long-missing protection keeps at least three escape routes viable",
                protectedRoutes, rng.Detail,
                "Evaluate the runtime softlock summary across its deterministic QA seed matrix and require viableEscapeRouteCount>=3.",
                "Assets/_Project/Scripts/Runtime/PrototypeRegionLootRng.cs; Assets/_Project/Scripts/Runtime/PrototypeEscapeProjects.cs");

            Observation localization = ObserveLocalizationContract();
            evidence.localization = localization.Detail;
            Product(checks, "W15-L01.ko_en_qps_map_keys", "localization", "P1",
                "Map/three-region/card-state keys have aligned ko/en/qps-long columns and qps content is expanded rather than copied",
                localization.Passed, localization.Detail,
                "Parse PrototypeStrings.tsv and group map/region forecast keys across all three locale columns.",
                "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv; Assets/_Project/Scripts/Localization/Tables/**");

            Observation logging = ObservePrivacyLogContract();
            evidence.logging = logging.Detail;
            Product(checks, "W15-O01.seed_region_privacy_log", "development observability", "P1",
                "Development events serialize seed and stable region/action IDs but no user, machine, home path, email, IP, or account identifier",
                logging.Passed, logging.Detail,
                "Inspect the runtime playtest event schema and a verification JSONL record for seed/region/action fields and PII absence.",
                "Assets/_Project/Scripts/Runtime/PrototypePlaytestEventLog.cs; Assets/_Project/Scripts/Runtime/PrototypeExpeditionMap.cs");

            WriteJson("wave15-edit-evidence.json", evidence);
            WriteReport("wave15-edit-contracts", "Wave 15 campaign/map RED-first Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: Wave 15 Play contracts did not complete.");
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
                WritePlayInfrastructureFailure(new TimeoutException("Timed out waiting for the Wave 15 scene."));
                StopPlayContracts();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;
            DateTime started = DateTime.UtcNow;
            List<Check> checks = new List<Check>();
            try
            {
                MapProbe probe = ObserveMapRuntime(prototype);
                string progress = ProgressFingerprint(prototype.Session);
                PlayEvidence evidence = new PlayEvidence
                {
                    runId = RunId,
                    baselineCommit = BaselineCommit,
                    targetDiscovery = probe.TargetFound ? "found " + probe.TargetKind + "/" + probe.TargetId : "no expedition-map proximity target",
                    stateTransition = probe.Detail,
                    regionCards = probe.RegionCardSemantics ? "three localized forecast cards found" : "region forecast card semantics unavailable",
                    inputParity = probe.InputDetail,
                    layout = probe.LayoutDetail,
                    progressFingerprint = progress,
                    screenshots = probe.Screenshots.ToArray(),
                    joystickNames = Input.GetJoystickNames().Where(name => !string.IsNullOrWhiteSpace(name)).ToArray()
                };

                Product(checks, "W15-P01.map_proximity_state_machine", "map proximity", "P0",
                    "At >1.25m the map prompt is hidden; within 1.25m exactly one map prompt is active; Interact opens the map; Cancel restores the same player position and run snapshot",
                    probe.TargetFound && probe.FarHidden && probe.NearSinglePrompt && probe.PopupOpened && probe.CancelRestored,
                    probe.Detail,
                    "At the map target, test distance=1.26m, distance=1.00m, Interact, then Cancel while recording target/prompt/popup/player/session fingerprints.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeExpeditionMap.cs; Assets/_Project/Scripts/Runtime/PrototypeCampInteraction.cs");
                Product(checks, "W15-P02.region_card_player_semantics", "map card content", "P0",
                    "The opened map exposes beach/forest/shallow-sea cards with category/abundance/time/risk/weather/gear/special/unknown semantics and no exact reward amount",
                    probe.RegionCardSemantics, probe.Detail,
                    "Open the map in ko/en/qps-long and inspect active node/card TMP text plus stable semantic IDs.",
                    "Assets/_Project/Scripts/Runtime/PrototypeExpeditionMap.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");
                Product(checks, "W15-P03.keyboard_gamepad_focus_parity", "dual input", "P1",
                    "Keyboard/mouse and synthetic gamepad select the same region/action target and change only glyph/focus presentation",
                    probe.InputParity, probe.InputDetail,
                    "Latch one region, switch KeyboardMouse to synthetic Gamepad, and compare target, selected region, action, and session fingerprints.",
                    "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs; Assets/_Project/Scripts/Runtime/PrototypeExpeditionMap.cs");
                Product(checks, "W15-P04.ko_en_qps_1280_layout", "1280x800 map layout", "P1",
                    "Fresh ko/en/qps-long far/near/popup captures are exact 1280x800 with TMP overflow=0, no offscreen text, and no material text overlap",
                    probe.LayoutPass, probe.LayoutDetail,
                    "Open every wave15-*-1280x800.png at 1:1 and compare the recorded overflow/offscreen/overlap counts.",
                    "Assets/_Project/Scripts/Runtime/PrototypeExpeditionMap.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                Infrastructure(checks, "W15-I03.fresh_capture_evidence", "evidence production", "P0",
                    "The current run emits at least one exact 1280x800 PNG per locale even while the product contract is RED",
                    () => RequireDetail(VerifyCaptureSet(probe.Screenshots), "captures=" + string.Join(",", probe.Screenshots)),
                    "Inspect PNG headers and RunId ownership in the fresh evidence folder.",
                    "Assets/Editor/ParallelQA/Wave15CampaignMapRedFirstRunner.cs");

                Unverified(checks, "W15-HW01.physical_gamepad", "input hardware", "P1",
                    "A human completes map approach, region focus, confirm, and cancel using a physical gamepad",
                    evidence.joystickNames.Length == 0 ? "no non-empty joystick name exposed to Unity batch Play Mode" : "device name detected, but no human actuation evidence was captured",
                    "Run the Windows development player with an actual gamepad and record human actuation evidence.",
                    "manual release-candidate hardware evidence");
                Unverified(checks, "W15-S01.steam_release", "external release", "P0",
                    "Steamworks App ID, Depot, Input, Cloud, Achievements, permissions, and store evidence are configured and approved",
                    "Steam integration and release evidence remain outside this task; readiness is NOT_READY.",
                    "Complete the separately authorized Steam release checklist.",
                    "external Steamworks account and release evidence");

                WriteJson("wave15-play-evidence.json", evidence);
                Report report = WriteReport("wave15-play-contracts", "Wave 15 campaign/map RED-first Play contracts", started, checks);
                bool runnerPassed = report.infrastructureOverall == "PASS" && report.productFailed == 0;
                SessionState.SetBool(PlayExitPassKey, runnerPassed);
                SessionState.SetString(PlayMessageKey, "Product=" + report.productOverall + " Infrastructure=" + report.infrastructureOverall +
                    " PhysicalGamepad=UNVERIFIED Evidence=" + Path.Combine(EvidenceFolder, "wave15-play-contracts.json"));
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static Observation ObserveDeadline(out string dayOne, out string day49, out string day50)
        {
            GameSession fresh = new GameSession();
            dayOne = "D" + fresh.Day + "/" + GameSession.FinalDay + "/" + fresh.Phase + "/" + fresh.Result;

            GameSession beforeDeadline = new GameSession();
            SetSessionState(beforeDeadline, 49, true);
            bool day49Settled = beforeDeadline.EndDay(false, false);
            day49 = "settled=" + day49Settled + "; D" + beforeDeadline.Day + "/" + beforeDeadline.Phase + "/" + beforeDeadline.Result;

            GameSession deadline = new GameSession();
            SetSessionState(deadline, 50, true);
            bool day50Settled = deadline.EndDay(false, false);
            day50 = "settled=" + day50Settled + "; D" + deadline.Day + "/" + deadline.Phase + "/" + deadline.Result;
            return new Observation
            {
                Passed = GameSession.FinalDay == 50 && day49Settled && beforeDeadline.Day == 50 && beforeDeadline.Phase == GamePhase.Camp &&
                         beforeDeadline.Result == RunResult.None && day50Settled && deadline.Phase == GamePhase.Result && deadline.Result == RunResult.Deadline,
                Detail = dayOne + "; day49=" + day49 + "; day50=" + day50
            };
        }

        private static Observation ObserveEarlyEscapePriority()
        {
            GameSession session = new GameSession();
            SetPrivateProperty(session, "Day", GameSession.FinalDay);
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Salvage, 20);
            bool workbench = session.TryBuild(StructureKind.Workbench);
            bool ropeResearch = session.TryResearch(TechKind.Rope);
            bool ropeCraft = session.TryCraft(TechKind.Rope);
            bool stageOne = session.TryUpgradeSignal();
            bool stageTwo = session.TryUpgradeSignal();
            string detail = "FinalDay=" + GameSession.FinalDay + "; workbench=" + workbench + "; rope=" + ropeResearch + "/" + ropeCraft +
                            "; signal=" + stageOne + "/" + stageTwo + "; result=" + session.Result + "; phase=" + session.Phase;
            return new Observation { Passed = stageTwo && session.Result == RunResult.Rescued && session.Phase == GamePhase.Result, Detail = detail };
        }

        private static string ObserveCanonicalSources()
        {
            string[] relative =
            {
                ".forge/design/project.json",
                ".forge/design/vertical-slice.json",
                ".forge/packets/wave15-fifty-day-campaign-rebaseline.json",
                "Docs/QA/wave15-fifty-day-rebaseline-integration.md"
            };
            StringBuilder all = new StringBuilder();
            foreach (string item in relative)
            {
                string path = Path.Combine(ProjectRoot, item.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) throw new FileNotFoundException("Canonical source missing", path);
                all.AppendLine(File.ReadAllText(path, Encoding.UTF8));
            }
            string text = all.ToString();
            string[] tokens = { "Day 50", "feature.expedition-map", "system.region-loot-rng", "task.qa.wave15-campaign-map-redfirst", "softlock" };
            string[] missing = tokens.Where(token => text.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0).ToArray();
            if (missing.Length > 0) throw new InvalidDataException("missing canonical tokens: " + string.Join(",", missing));
            return "files=4; tokens=" + string.Join(",", tokens);
        }

        private static Observation ObserveRegionCatalog()
        {
            List<object> entries = DiscoverCatalogEntries();
            Dictionary<string, object> regionEntries = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (object entry in entries)
            {
                string description = Describe(entry, 2);
                if (ContainsAny(description, "region.beach", "beach")) regionEntries["beach"] = entry;
                if (ContainsAny(description, "region.forest", "forest")) regionEntries["forest"] = entry;
                if (ContainsAny(description, "region.shallow", "shallow_sea", "shallow-sea", "shallows")) regionEntries["shallow-sea"] = entry;
            }
            string[] semanticGroups = { "resource|category", "abundance|richness", "travel|duration|time", "risk|hazard", "weather", "gear|equipment", "special|discovery", "unknown|discovered|identified" };
            List<string> failures = new List<string>();
            foreach (string region in new[] { "beach", "forest", "shallow-sea" })
            {
                if (!regionEntries.TryGetValue(region, out object entry))
                {
                    failures.Add(region + ":missing");
                    continue;
                }
                string members = string.Join("|", PublicMemberNames(entry.GetType())).ToLowerInvariant();
                foreach (string group in semanticGroups)
                {
                    string[] alternatives = group.Split('|');
                    if (!alternatives.Any(members.Contains)) failures.Add(region + ":" + group);
                }
                if (Regex.IsMatch(members, @"exact.*(amount|quantity)|(amount|quantity).*exact")) failures.Add(region + ":exact-forecast-quantity-exposed");
            }
            return new Observation
            {
                Passed = failures.Count == 0 && regionEntries.Count == 3,
                Detail = "catalogEntries=" + entries.Count + "; regions=" + string.Join(",", regionEntries.Keys.OrderBy(key => key)) +
                         "; failures=" + (failures.Count == 0 ? "none" : string.Join(",", failures))
            };
        }

        private static Observation ObserveRngContract(out bool sameSeed, out bool differentSeed, out bool protectedRoutes)
        {
            sameSeed = false;
            differentSeed = false;
            protectedRoutes = false;
            List<string> attempts = new List<string>();
            foreach (Type type in RuntimeTypes().Where(type => Regex.IsMatch(type.Name, "(Region|Loot|Expedition).*(Rng|Random)|(?:Rng|Random).*(Region|Loot|Expedition)", RegexOptions.IgnoreCase)))
            {
                object instance = null;
                if (!type.IsAbstract && !type.IsSealed)
                {
                    try { instance = Activator.CreateInstance(type); } catch { }
                }
                else if (!type.IsAbstract)
                {
                    try { instance = Activator.CreateInstance(type); } catch { }
                }
                foreach (MethodInfo method in type.GetMethods(StaticFlags | InstanceFlags)
                    .Where(method => method.ReturnType != typeof(void) && Regex.IsMatch(method.Name, "Generate|Roll|Resolve|Create|Build", RegexOptions.IgnoreCase)))
                {
                    if (!method.IsStatic && instance == null) continue;
                    try
                    {
                        object[] a1 = BuildArguments(method, 41017);
                        object[] a2 = BuildArguments(method, 41017);
                        object[] b1 = BuildArguments(method, 41018);
                        object firstResult = method.Invoke(method.IsStatic ? null : instance, a1);
                        object repeatResult = method.Invoke(method.IsStatic ? null : instance, a2);
                        object otherResult = method.Invoke(method.IsStatic ? null : instance, b1);
                        string first = Describe(firstResult, 3);
                        string repeat = Describe(repeatResult, 3);
                        string other = Describe(otherResult, 3);
                        bool firstBounds = ValidateDeclaredBounds(firstResult, out string firstBoundsDetail);
                        bool otherBounds = ValidateDeclaredBounds(otherResult, out string otherBoundsDetail);
                        attempts.Add(type.Name + "." + method.Name + " same=" + (first == repeat) + " different=" + (first != other) +
                            " bounds=" + firstBounds + "/" + otherBounds + " [" + firstBoundsDetail + ";" + otherBoundsDetail + "]");
                        if (!string.IsNullOrWhiteSpace(first)) sameSeed |= first == repeat;
                        differentSeed |= !string.IsNullOrWhiteSpace(first) && first != other && firstBounds && otherBounds;
                        object protection = method.Invoke(method.IsStatic ? null : instance, BuildArguments(method, 51017));
                        int routes = ReadNamedInt(protection,
                            "ViableEscapeRouteCount", "ProtectedEscapeRouteCount", "GuaranteedEscapeRouteCount");
                        bool guarantee = ReadNamedBool(protection, "CriticalPartGuaranteed", "HasCriticalPartGuarantee");
                        bool alternative = ReadNamedBool(protection, "AlternativeAcquisitionAvailable", "HasAlternativeAcquisition");
                        bool longMissing = ReadNamedBool(protection, "LongMissingProtectionActive", "HasLongMissingProtection");
                        protectedRoutes |= routes >= 3 && guarantee && alternative && longMissing;
                    }
                    catch (Exception exception)
                    {
                        attempts.Add(type.Name + "." + method.Name + " error=" + exception.GetType().Name);
                    }
                }
            }
            string detail = attempts.Count == 0 ? "runtime seeded region-loot generator not discovered" : string.Join("; ", attempts);
            detail += "; same=" + sameSeed + "; different=" + differentSeed + "; routes>=3=" + protectedRoutes;
            return new Observation { Passed = sameSeed && differentSeed && protectedRoutes, Detail = detail };
        }

        private static Observation ObserveLocalizationContract()
        {
            string path = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            if (!File.Exists(path)) return new Observation { Detail = "PrototypeStrings.tsv missing" };
            string[][] rows = File.ReadAllLines(path, Encoding.UTF8).Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('\t')).Where(parts => parts.Length >= 4).ToArray();
            string[] regionTokens = { "beach", "forest", "shallow" };
            List<string> failures = new List<string>();
            foreach (string token in regionTokens)
            {
                string[][] matching = rows.Where(parts => parts[0].IndexOf("region", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                                           parts[0].IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (matching.Length == 0) failures.Add(token + ":keys-missing");
                else if (matching.Any(parts => string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]) || string.IsNullOrWhiteSpace(parts[3])))
                    failures.Add(token + ":locale-column-empty");
            }
            string[][] mapRows = rows.Where(parts => parts[0].IndexOf("expedition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     parts[0].IndexOf("map", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (mapRows.Length < 8) failures.Add("map-card-key-count=" + mapRows.Length);
            int qpsExpanded = mapRows.Count(parts => parts[3].Length > parts[2].Length && parts[3] != parts[2]);
            if (mapRows.Length > 0 && qpsExpanded < Math.Max(1, mapRows.Length / 2)) failures.Add("qps-not-expanded=" + qpsExpanded + "/" + mapRows.Length);
            return new Observation { Passed = failures.Count == 0, Detail = "rows=" + rows.Length + "; mapRows=" + mapRows.Length +
                "; qpsExpanded=" + qpsExpanded + "; failures=" + (failures.Count == 0 ? "none" : string.Join(",", failures)) };
        }

        private static Observation ObservePrivacyLogContract()
        {
            string path = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "PrototypePlaytestEventLog.cs");
            string text = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
            bool seed = Regex.IsMatch(text, @"\b(seed|run_seed)\b", RegexOptions.IgnoreCase);
            bool region = Regex.IsMatch(text, @"\b(region_id|region)\b", RegexOptions.IgnoreCase);
            bool piiField = Regex.IsMatch(text, @"public\s+string\s+(user(name|_name)?|machine(name|_name)?|email|ip(address|_address)?|account(_id)?)\b", RegexOptions.IgnoreCase);
            return new Observation { Passed = seed && region && !piiField,
                Detail = "seedField=" + seed + "; regionField=" + region + "; prohibitedPiiField=" + piiField };
        }

        private static MapProbe ObserveMapRuntime(KimSurvivalPrototype prototype)
        {
            MapProbe probe = new MapProbe();
            prototype.Session.Reset();
            InvokeOptional(prototype, "RefreshAll");
            Type enumType = typeof(PrototypeCampInteractionTargetKind);
            string name = Enum.GetNames(enumType).FirstOrDefault(value => Regex.IsMatch(value, "Expedition|Map", RegexOptions.IgnoreCase));
            if (string.IsNullOrWhiteSpace(name))
            {
                probe.Detail = "PrototypeCampInteractionTargetKind has no expedition-map member; values=" + string.Join(",", Enum.GetNames(enumType));
                CaptureAbsenceMatrix(prototype, probe);
                probe.LayoutDetail = "map absent; baseline camp captures only; map layout is RED_EXPECTED_GAP";
                probe.InputDetail = "map focus target absent; existing prompt input regression remains in Wave 12 prerequisite";
                return probe;
            }

            probe.TargetKind = Enum.Parse(enumType, name);
            probe.TargetFound = TryGetTargetPosition(prototype, probe.TargetKind, out Vector2 targetPosition);
            probe.TargetPosition = targetPosition;
            if (!probe.TargetFound)
            {
                probe.Detail = "map enum=" + name + " but no target position was discoverable";
                CaptureAbsenceMatrix(prototype, probe);
                return probe;
            }

            object campUse = GetField(prototype, "campUse");
            object interaction = GetField(prototype, "campInteraction");
            string before = ProgressFingerprint(prototype.Session);
            Vector2 original = ReadVector2(campUse, "PlayerPosition");
            Warp(campUse, targetPosition + Vector2.right * (UseRange + 0.01f));
            RefreshInteraction(prototype);
            string farKind = ReadProperty(interaction, "ActiveTargetKind");
            probe.FarHidden = !ReadBool(interaction, "HasProximityPrompt") || !string.Equals(farKind, name, StringComparison.OrdinalIgnoreCase);

            Warp(campUse, targetPosition + Vector2.right * 1.0f);
            RefreshInteraction(prototype);
            probe.TargetId = ReadProperty(interaction, "ActiveTargetId");
            probe.NearSinglePrompt = ReadBool(interaction, "HasProximityPrompt") &&
                                     string.Equals(ReadProperty(interaction, "ActiveTargetKind"), name, StringComparison.OrdinalIgnoreCase) &&
                                     CountActivePrompts(prototype) == 1;
            Vector2 beforeOpenPosition = ReadVector2(campUse, "PlayerPosition");
            probe.PopupOpened = Convert.ToBoolean(InvokeRequired(prototype, "TryOpenCampPopup"), CultureInfo.InvariantCulture) &&
                                (ReadBool(interaction, "IsPopupOpen") || FindActiveMapRoot(prototype) != null);
            GameObject mapRoot = FindActiveMapRoot(prototype);
            string cardDetail = "popup-not-open";
            probe.RegionCardSemantics = probe.PopupOpened && mapRoot != null && ObserveActiveMapText(mapRoot, out cardDetail);
            string afterOpen = ProgressFingerprint(prototype.Session);
            InvokeCancel(prototype);
            RefreshInteraction(prototype);
            Vector2 afterCancelPosition = ReadVector2(campUse, "PlayerPosition");
            string afterCancel = ProgressFingerprint(prototype.Session);
            probe.CancelRestored = Vector2.Distance(beforeOpenPosition, afterCancelPosition) <= 0.001f && before == afterOpen && before == afterCancel;
            probe.Detail = "target=" + name + "/" + probe.TargetId + " at=" + FormatVector(targetPosition) + "; farKind=" + farKind +
                           "; farHidden=" + probe.FarHidden + "; nearSingle=" + probe.NearSinglePrompt + "; opened=" + probe.PopupOpened +
                           "; cards=" + cardDetail + "; cancelRestored=" + probe.CancelRestored + "; fingerprint=" + before + "->" + afterCancel;

            probe.InputParity = ObserveInputParity(prototype, probe.TargetKind, probe.TargetId, out string inputDetail);
            probe.InputDetail = inputDetail;
            CaptureMapMatrix(prototype, probe, targetPosition);
            Warp(campUse, original);
            RefreshInteraction(prototype);
            return probe;
        }

        private static void CaptureAbsenceMatrix(KimSurvivalPrototype prototype, MapProbe probe)
        {
            foreach (string locale in new[] { "ko", "en", "qps-long" })
            {
                SetLocale(prototype, locale);
                InvokeOptional(prototype, "RefreshAll");
                string name = "wave15-" + locale + "-camp-map-absent-1280x800.png";
                prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, name), 1280, 800);
                probe.Screenshots.Add(name);
            }
            SetLocale(prototype, "ko");
        }

        private static void CaptureMapMatrix(KimSurvivalPrototype prototype, MapProbe probe, Vector2 targetPosition)
        {
            object campUse = GetField(prototype, "campUse");
            object interaction = GetField(prototype, "campInteraction");
            int overflow = 0;
            int offscreen = 0;
            int overlap = 0;
            foreach (string locale in new[] { "ko", "en", "qps-long" })
            {
                SetLocale(prototype, locale);
                Warp(campUse, targetPosition + Vector2.right * (UseRange + 0.01f));
                RefreshInteraction(prototype);
                CaptureNamed(prototype, probe, locale, "far");
                Warp(campUse, targetPosition + Vector2.right * 1.0f);
                RefreshInteraction(prototype);
                CaptureNamed(prototype, probe, locale, "near");
                InvokeRequired(prototype, "TryOpenCampPopup");
                InvokeOptional(prototype, "RefreshAll");
                CaptureNamed(prototype, probe, locale, "popup");
                GameObject mapRoot = FindActiveMapRoot(prototype);
                MeasureActiveMapText(prototype, mapRoot, out int localOverflow, out int localOffscreen, out int localOverlap);
                overflow += localOverflow;
                offscreen += localOffscreen;
                overlap += localOverlap;
                InvokeCancel(prototype);
                if (ReadBool(interaction, "IsPopupOpen")) InvokeOptional(interaction, "ClosePopup");
            }
            SetLocale(prototype, "ko");
            probe.LayoutPass = probe.Screenshots.Count == 9 && VerifyCaptureSet(probe.Screenshots) && overflow == 0 && offscreen == 0 && overlap == 0;
            probe.LayoutDetail = "captures=" + probe.Screenshots.Count + "/9; overflow=" + overflow + "; offscreen=" + offscreen + "; textOverlap=" + overlap;
        }

        private static bool ObserveInputParity(KimSurvivalPrototype prototype, object targetKind, string targetId, out string detail)
        {
            string before = ProgressFingerprint(prototype.Session);
            object interaction = GetField(prototype, "campInteraction");
            string targetBefore = ReadProperty(interaction, "ActiveTargetKind") + "/" + ReadProperty(interaction, "ActiveTargetId");
            string focusBefore = MapFocusFingerprint(prototype);
            object keyboard = Enum.Parse(typeof(PrototypeInputDevice), "KeyboardMouse");
            object gamepad = Enum.Parse(typeof(PrototypeInputDevice), "Gamepad");
            bool keyboardApplied = InvokeOptional(prototype, "ApplyCampProximityPresentation", targetKind, targetId, keyboard) != null;
            string keyboardGlyph = GetTextField(prototype, "campProximityGlyphText");
            bool gamepadApplied = InvokeOptional(prototype, "ApplyCampProximityPresentation", targetKind, targetId, gamepad) != null;
            string gamepadGlyph = GetTextField(prototype, "campProximityGlyphText");
            string after = ProgressFingerprint(prototype.Session);
            string targetAfter = ReadProperty(interaction, "ActiveTargetKind") + "/" + ReadProperty(interaction, "ActiveTargetId");
            string focusAfter = MapFocusFingerprint(prototype);
            bool passed = keyboardApplied && gamepadApplied && keyboardGlyph != gamepadGlyph && before == after && targetBefore == targetAfter &&
                          !string.IsNullOrWhiteSpace(focusBefore) && focusBefore == focusAfter;
            detail = "keyboard=" + keyboardGlyph + "; gamepad=" + gamepadGlyph + "; target=" + targetBefore + "->" + targetAfter +
                     "; focus=" + focusBefore + "->" + focusAfter + "; progress=" + before + "->" + after;
            return passed;
        }

        private static bool ObserveActiveMapText(GameObject mapRoot, out string detail)
        {
            TMP_Text[] texts = mapRoot.GetComponentsInChildren<TMP_Text>(false)
                .Where(text => text != null && text.gameObject.activeInHierarchy).ToArray();
            string combined = string.Join(" | ", texts.Select(text => text.text));
            bool regions = ContainsAny(combined, "해변", "Beach") && ContainsAny(combined, "숲", "Forest") && ContainsAny(combined, "얕은 바다", "얇은 바다", "Shallow");
            string[] groups = { "자원|resource", "풍부|abundan|rich", "시간|time|travel", "위험|risk|hazard", "날씨|weather", "장비|gear|equipment", "특별|special|discovery", @"미확인|unknown|\?" };
            bool semantics = groups.All(group => Regex.IsMatch(combined, group, RegexOptions.IgnoreCase));
            bool exactAmounts = Regex.IsMatch(combined, @"(?:wood|stone|food|salvage|나무|돌|식량|표류물)\s*[x×:]?\s*\d+", RegexOptions.IgnoreCase);
            detail = "activeTexts=" + texts.Length + "; regions=" + regions + "; semantics=" + semantics + "; exactForecastAmounts=" + exactAmounts;
            return regions && semantics && !exactAmounts;
        }

        private static void MeasureActiveMapText(KimSurvivalPrototype prototype, GameObject mapRoot, out int overflow, out int offscreen, out int overlap)
        {
            overflow = 0;
            offscreen = 0;
            overlap = 0;
            if (mapRoot == null)
            {
                offscreen = 1;
                return;
            }
            List<Rect> rects = new List<Rect>();
            Camera camera = GetField(prototype, "worldCamera") as Camera;
            RenderTexture previousTarget = camera == null ? null : camera.targetTexture;
            RenderTexture captureTarget = camera == null ? null : RenderTexture.GetTemporary(1280, 800, 24, RenderTextureFormat.ARGB32);
            try
            {
                if (camera != null) camera.targetTexture = captureTarget;
                Canvas.ForceUpdateCanvases();
                foreach (TMP_Text text in mapRoot.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text)) continue;
                    text.ForceMeshUpdate(true, true);
                    if (text.isTextOverflowing) overflow += 1;
                    Rect rect = WorldScreenRect(text.rectTransform);
                    if (rect.xMin < 4f || rect.yMin < 4f || rect.xMax > 1276f || rect.yMax > 796f) offscreen += 1;
                    rects.Add(rect);
                }
            }
            finally
            {
                if (camera != null) camera.targetTexture = previousTarget;
                if (captureTarget != null) RenderTexture.ReleaseTemporary(captureTarget);
                Canvas.ForceUpdateCanvases();
            }
            for (int i = 0; i < rects.Count; i += 1)
            {
                for (int j = i + 1; j < rects.Count; j += 1)
                {
                    Rect intersection = Intersect(rects[i], rects[j]);
                    float smaller = Mathf.Min(rects[i].width * rects[i].height, rects[j].width * rects[j].height);
                    if (smaller > 1f && intersection.width * intersection.height / smaller > 0.30f) overlap += 1;
                }
            }
        }

        private static List<object> DiscoverCatalogEntries()
        {
            List<object> result = new List<object>();
            foreach (Type type in RuntimeTypes().Where(type => Regex.IsMatch(type.Name, "Expedition|Region", RegexOptions.IgnoreCase)))
            {
                foreach (MemberInfo member in type.GetMembers(StaticFlags))
                {
                    object value = null;
                    try
                    {
                        FieldInfo field = member as FieldInfo;
                        PropertyInfo property = member as PropertyInfo;
                        MethodInfo method = member as MethodInfo;
                        if (field != null) value = field.GetValue(null);
                        else if (property != null && property.GetIndexParameters().Length == 0) value = property.GetValue(null, null);
                        else if (method != null && method.GetParameters().Length == 0 && method.ReturnType != typeof(void) && Regex.IsMatch(method.Name, "All|Catalog|Definitions|Regions", RegexOptions.IgnoreCase)) value = method.Invoke(null, null);
                    }
                    catch { }
                    if (value is IEnumerable enumerable && !(value is string))
                    {
                        int count = 0;
                        foreach (object entry in enumerable)
                        {
                            if (entry != null) result.Add(entry);
                            if (++count >= 64) break;
                        }
                    }
                }
            }
            return result.Distinct().ToList();
        }

        private static IEnumerable<Type> RuntimeTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException exception) { types = exception.Types.Where(type => type != null).ToArray(); }
                foreach (Type type in types)
                    if (type != null && type.Namespace != null && type.Namespace.StartsWith("KimSurvival", StringComparison.Ordinal)) yield return type;
            }
        }

        private static object[] BuildArguments(MethodInfo method, int seed)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] values = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index += 1)
            {
                ParameterInfo parameter = parameters[index];
                string name = parameter.Name ?? string.Empty;
                if (parameter.ParameterType == typeof(int)) values[index] = name.IndexOf("seed", StringComparison.OrdinalIgnoreCase) >= 0 ? seed : 0;
                else if (parameter.ParameterType == typeof(string)) values[index] = name.IndexOf("region", StringComparison.OrdinalIgnoreCase) >= 0 ? "region.beach" :
                    name.IndexOf("action", StringComparison.OrdinalIgnoreCase) >= 0 ? "action.search" : string.Empty;
                else if (parameter.ParameterType == typeof(bool)) values[index] = false;
                else if (parameter.ParameterType.IsEnum) values[index] = Enum.GetValues(parameter.ParameterType).GetValue(0);
                else if (parameter.HasDefaultValue) values[index] = parameter.DefaultValue;
                else values[index] = parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }
            return values;
        }

        private static string Describe(object value, int depth)
        {
            if (value == null) return "null";
            Type type = value.GetType();
            if (depth <= 0 || type.IsPrimitive || type.IsEnum || value is string || value is decimal)
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (value is IEnumerable enumerable)
            {
                List<string> items = new List<string>();
                int count = 0;
                foreach (object item in enumerable)
                {
                    items.Add(Describe(item, depth - 1));
                    if (++count >= 32) break;
                }
                return "[" + string.Join(",", items) + "]";
            }
            List<string> fields = new List<string>();
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.GetIndexParameters().Length == 0).OrderBy(property => property.Name))
            {
                try { fields.Add(property.Name + "=" + Describe(property.GetValue(value, null), depth - 1)); } catch { }
            }
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(field => field.Name))
            {
                try { fields.Add(field.Name + "=" + Describe(field.GetValue(value), depth - 1)); } catch { }
            }
            return type.Name + "{" + string.Join(";", fields) + "}";
        }

        private static IEnumerable<string> PublicMemberNames(Type type)
        {
            return type.GetMembers(BindingFlags.Instance | BindingFlags.Public).Select(member => member.Name).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static int ReadNamedInt(object value, params string[] names)
        {
            if (value == null) return -1;
            foreach (string name in names)
            {
                PropertyInfo property = value.GetType().GetProperty(name, InstanceFlags);
                if (property != null && property.PropertyType == typeof(int)) return (int)property.GetValue(value, null);
                FieldInfo field = value.GetType().GetField(name, InstanceFlags);
                if (field != null && field.FieldType == typeof(int)) return (int)field.GetValue(value);
            }
            return -1;
        }

        private static bool ReadNamedBool(object value, params string[] names)
        {
            if (value == null) return false;
            foreach (string name in names)
            {
                PropertyInfo property = value.GetType().GetProperty(name, InstanceFlags);
                if (property != null && property.PropertyType == typeof(bool)) return (bool)property.GetValue(value, null);
                FieldInfo field = value.GetType().GetField(name, InstanceFlags);
                if (field != null && field.FieldType == typeof(bool)) return (bool)field.GetValue(value);
            }
            return false;
        }

        private static bool ValidateDeclaredBounds(object value, out string detail)
        {
            if (value == null)
            {
                detail = "null-result";
                return false;
            }
            if (ReadNamedBool(value, "WithinDeclaredBounds", "IsWithinDeclaredBounds"))
            {
                detail = "explicit-within-bounds=true";
                return true;
            }
            int minimum = ReadNamedInt(value, "MinimumAmount", "MinAmount", "MinimumQuantity", "MinQuantity");
            int maximum = ReadNamedInt(value, "MaximumAmount", "MaxAmount", "MaximumQuantity", "MaxQuantity");
            int actual = ReadNamedInt(value, "Amount", "Quantity", "TotalAmount", "TotalQuantity");
            bool passed = minimum >= 0 && maximum >= minimum && actual >= minimum && actual <= maximum;
            detail = "actual/min/max=" + actual + "/" + minimum + "/" + maximum;
            return passed;
        }

        private static string MapFocusFingerprint(object prototype)
        {
            List<string> values = new List<string>();
            foreach (FieldInfo field in prototype.GetType().GetFields(InstanceFlags).Where(field => Regex.IsMatch(field.Name,
                "selected.*region|focused.*region|region.*focus|selected.*action|expedition.*selection", RegexOptions.IgnoreCase)))
            {
                object value = field.GetValue(prototype);
                values.Add(field.Name + "=" + Describe(value, 1));
            }
            foreach (PropertyInfo property in prototype.GetType().GetProperties(InstanceFlags).Where(property => property.GetIndexParameters().Length == 0 && Regex.IsMatch(property.Name,
                "Selected.*Region|Focused.*Region|Region.*Focus|Selected.*Action|Expedition.*Selection", RegexOptions.IgnoreCase)))
            {
                try { values.Add(property.Name + "=" + Describe(property.GetValue(prototype, null), 1)); } catch { }
            }
            return string.Join(";", values.OrderBy(value => value, StringComparer.Ordinal));
        }

        private static bool TryGetTargetPosition(object prototype, object targetKind, out Vector2 position)
        {
            position = Vector2.zero;
            MethodInfo method = prototype.GetType().GetMethods(InstanceFlags).FirstOrDefault(candidate =>
                Regex.IsMatch(candidate.Name, "Get.*InteractionTargetPosition|Get.*Map.*Position", RegexOptions.IgnoreCase) &&
                candidate.ReturnType == typeof(Vector2) && candidate.GetParameters().Length == 1);
            if (method == null) return false;
            try { position = (Vector2)method.Invoke(prototype, new[] { targetKind }); return true; }
            catch { return false; }
        }

        private static void RefreshInteraction(object prototype)
        {
            InvokeOptional(prototype, "RefreshCampInteractionSelection");
            InvokeOptional(prototype, "RefreshCampInteractionUi");
            InvokeOptional(prototype, "RefreshHud");
        }

        private static void InvokeCancel(object prototype)
        {
            foreach (string name in new[] { "CancelExpeditionMap", "CloseExpeditionMap", "CancelCampPopup" })
            {
                MethodInfo method = prototype.GetType().GetMethod(name, InstanceFlags);
                if (method == null) continue;
                method.Invoke(prototype, null);
                return;
            }
        }

        private static GameObject FindActiveMapRoot(object prototype)
        {
            IEnumerable<FieldInfo> fields = prototype.GetType().GetFields(InstanceFlags)
                .Where(field => Regex.IsMatch(field.Name, "expedition.*map|map.*popup|region.*select", RegexOptions.IgnoreCase))
                .OrderByDescending(field => field.FieldType == typeof(GameObject))
                .ThenByDescending(field => Regex.IsMatch(field.Name, "panel|popup|root", RegexOptions.IgnoreCase));
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(prototype);
                GameObject gameObject = value as GameObject;
                if (gameObject == null && value is Component component) gameObject = component.gameObject;
                if (gameObject != null && gameObject.activeInHierarchy) return gameObject;
            }
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude)
                .Select(transform => transform.gameObject)
                .FirstOrDefault(gameObject => gameObject.activeInHierarchy && Regex.IsMatch(gameObject.name, "expedition.*map|map.*popup|region.*select", RegexOptions.IgnoreCase));
        }

        private static int CountActivePrompts(object prototype)
        {
            int count = 0;
            foreach (FieldInfo field in prototype.GetType().GetFields(InstanceFlags).Where(field => field.FieldType == typeof(GameObject) && field.Name.IndexOf("Prompt", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                GameObject value = field.GetValue(prototype) as GameObject;
                if (value != null && value.activeInHierarchy) count += 1;
            }
            return count;
        }

        private static void SetLocale(object prototype, string locale)
        {
            PrototypeLocalization localization = GetField(prototype, "localization") as PrototypeLocalization;
            if (localization == null) return;
            if (string.Equals(locale, "qps-long", StringComparison.OrdinalIgnoreCase)) localization.SetQaLocale();
            else localization.SetLocale(locale, false);
            InvokeOptional(prototype, "RefreshAll");
        }

        private static void CaptureNamed(KimSurvivalPrototype prototype, MapProbe probe, string locale, string state)
        {
            string name = "wave15-" + locale + "-map-" + state + "-1280x800.png";
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, name), 1280, 800);
            probe.Screenshots.Add(name);
        }

        private static bool VerifyCaptureSet(IEnumerable<string> names)
        {
            string[] files = names.ToArray();
            if (files.Length < 3 || !new[] { "ko", "en", "qps-long" }.All(locale => files.Any(file => file.Contains("-" + locale + "-")))) return false;
            foreach (string name in files)
            {
                string path = Path.Combine(EvidenceFolder, name);
                if (!File.Exists(path) || new FileInfo(path).Length == 0) return false;
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try { if (!texture.LoadImage(File.ReadAllBytes(path), false) || texture.width != 1280 || texture.height != 800) return false; }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            return true;
        }

        private static Rect WorldScreenRect(RectTransform transform)
        {
            Canvas canvas = transform.GetComponentInParent<Canvas>();
            RectTransform canvasTransform = canvas == null ? null : canvas.transform as RectTransform;
            if (canvasTransform != null)
            {
                Vector3[] canvasCorners = new Vector3[4];
                Vector3[] textCorners = new Vector3[4];
                canvasTransform.GetWorldCorners(canvasCorners);
                transform.GetWorldCorners(textCorners);
                float canvasLeft = canvasCorners.Min(corner => corner.x);
                float canvasRight = canvasCorners.Max(corner => corner.x);
                float canvasBottom = canvasCorners.Min(corner => corner.y);
                float canvasTop = canvasCorners.Max(corner => corner.y);
                float canvasWidth = Mathf.Max(0.0001f, canvasRight - canvasLeft);
                float canvasHeight = Mathf.Max(0.0001f, canvasTop - canvasBottom);
                return Rect.MinMaxRect(
                    (textCorners.Min(corner => corner.x) - canvasLeft) * 1280f / canvasWidth,
                    (textCorners.Min(corner => corner.y) - canvasBottom) * 800f / canvasHeight,
                    (textCorners.Max(corner => corner.x) - canvasLeft) * 1280f / canvasWidth,
                    (textCorners.Max(corner => corner.y) - canvasBottom) * 800f / canvasHeight);
            }
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            Vector2[] points = corners.Select(corner => RectTransformUtility.WorldToScreenPoint(eventCamera, corner)).ToArray();
            float sourceWidth = Mathf.Max(1f, Screen.width);
            float sourceHeight = Mathf.Max(1f, Screen.height);
            return Rect.MinMaxRect(points.Min(point => point.x) * 1280f / sourceWidth, points.Min(point => point.y) * 800f / sourceHeight,
                points.Max(point => point.x) * 1280f / sourceWidth, points.Max(point => point.y) * 800f / sourceHeight);
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            float left = Mathf.Max(a.xMin, b.xMin);
            float right = Mathf.Min(a.xMax, b.xMax);
            float bottom = Mathf.Max(a.yMin, b.yMin);
            float top = Mathf.Min(a.yMax, b.yMax);
            return right > left && top > bottom ? Rect.MinMaxRect(left, bottom, right, top) : new Rect();
        }

        private static void SetSessionState(GameSession session, int day, bool expeditionCompleted)
        {
            SetPrivateProperty(session, "Day", day);
            SetPrivateProperty(session, "Hunger", 100f);
            SetPrivateProperty(session, "Energy", 100f);
            SetPrivateProperty(session, "Phase", GamePhase.Camp);
            SetPrivateProperty(session, "Result", RunResult.None);
            SetPrivateProperty(session, "ExpeditionCompleted", expeditionCompleted);
        }

        private static void SetPrivateProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, InstanceFlags);
            MethodInfo setter = property == null ? null : property.GetSetMethod(true);
            if (setter == null) throw new MissingMemberException(target.GetType().FullName, name);
            setter.Invoke(target, new[] { value });
        }

        private static object GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, InstanceFlags);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return field.GetValue(target);
        }

        private static object InvokeRequired(object target, string name, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), name, arguments);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            try { return method.Invoke(target, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static object InvokeOptional(object target, string name, params object[] arguments)
        {
            if (target == null) return null;
            MethodInfo method = FindMethod(target.GetType(), name, arguments);
            if (method == null) return null;
            try { method.Invoke(target, arguments); return method.ReturnType == typeof(void) ? Boolean.TrueString : null; }
            catch { return null; }
        }

        private static MethodInfo FindMethod(Type type, string name, object[] arguments)
        {
            return type.GetMethods(InstanceFlags).FirstOrDefault(method => method.Name == name && method.GetParameters().Length == arguments.Length &&
                method.GetParameters().Select((parameter, index) => arguments[index] == null || parameter.ParameterType.IsInstanceOfType(arguments[index]) ||
                    (parameter.ParameterType.IsEnum && arguments[index].GetType().IsEnum)).All(value => value));
        }

        private static string ReadProperty(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(name, InstanceFlags);
            object value = property == null ? null : property.GetValue(target, null);
            return value == null ? string.Empty : value.ToString();
        }

        private static bool ReadBool(object target, string name)
        {
            return string.Equals(ReadProperty(target, name), Boolean.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        private static Vector2 ReadVector2(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(name, InstanceFlags);
            return property != null && property.PropertyType == typeof(Vector2) ? (Vector2)property.GetValue(target, null) : Vector2.zero;
        }

        private static void Warp(object campUse, Vector2 position)
        {
            InvokeRequired(campUse, "Warp", position);
        }

        private static string GetTextField(object target, string name)
        {
            TMP_Text text = GetField(target, name) as TMP_Text;
            return text == null ? string.Empty : text.text;
        }

        private static string ProgressFingerprint(GameSession session)
        {
            return "D" + session.Day + "/W" + session.GetStorage(ResourceKind.Wood) + "/S" + session.GetStorage(ResourceKind.Stone) +
                   "/F" + session.GetStorage(ResourceKind.Food) + "/D" + session.GetStorage(ResourceKind.Salvage) +
                   "/signal" + session.SignalStage + "/bag" + session.ActiveBagSlotCount + "/" + session.Phase + "/" + session.Result;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            return needles.Any(needle => value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string FormatVector(Vector2 value)
        {
            return "(" + value.x.ToString("0.000", CultureInfo.InvariantCulture) + "," + value.y.ToString("0.000", CultureInfo.InvariantCulture) + ")";
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
                greenCompletionCondition = "Infrastructure PASS, zero EXPECTED_GAP/FAIL, Day 50/map/three-region/RNG/privacy contracts PASS, and the fresh Wave 14 prerequisite remains 10/10 GREEN.",
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
                NewCheck("W15-I99.play_runner", "infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Wave 15 Play runner emits parseable evidence", exception.ToString(),
                    "Run the Play execute method outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave15CampaignMapRedFirstRunner.cs")
            };
            WriteReport("wave15-play-contracts", "Wave 15 Play infrastructure failure", DateTime.UtcNow, checks);
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
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing Wave 15 Play result");
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
