using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParallelQA
{
    /// <summary>
    /// Independent RED-first gate for the two Game Jam day-20 long-stay endings.
    /// Play checks accept only a structured observation captured by the active
    /// production scene. Static fixtures and pass booleans are never Play evidence.
    /// </summary>
    public static class GameJamLongStayRedFirstGateRunner
    {
        private const string RedBaseline = "e4bbc03531d54e023f7a90f7a608871a47d26d55";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string ObservationMethod = "CaptureGameJamLongStayEndingObservation";
        private const string NaturalId = "ending.gamejam.stay.natural-kim";
        private const string EngineerId = "ending.gamejam.stay.island-engineer";
        private const string PlayRunningKey = "ParallelQA.GameJamLongStay.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.GameJamLongStay.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.GameJamLongStay.PlayMessage";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private static readonly string[] LongStayIds = { NaturalId, EngineerId };
        private static readonly string[] Locales = { "en", "ko", "qps-long" };
        private static readonly string[] StandardDay50Ids =
        {
            "ending.stay.green-king", "ending.stay.fortress-manager", "ending.stay.scrap-professor",
            "ending.stay.island-ranger", "ending.stay.just-kim"
        };

        private static bool playTickAttached;
        private static double playEarliestRunTime;
        private static double playTimeoutAt;

        [Serializable]
        private sealed class Check
        {
            public string id = string.Empty;
            public string gdd = string.Empty;
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
            public string greenCompletionCondition = string.Empty;
            public Check[] checks = Array.Empty<Check>();
        }

        [Serializable]
        private sealed class EndingDefinitionEvidence
        {
            public string stableId = string.Empty;
            public string category = string.Empty;
            public int priority = -1;
            public int conditionCount = -1;
            public string requiredEscapeId = string.Empty;
            public string requiredEventId = string.Empty;
            public string requiredBehaviorId = string.Empty;
            public string[] comicPanelKeys = Array.Empty<string>();
            public string[] comicPanelRoleIds = Array.Empty<string>();
            public string achievementMappingId = string.Empty;
        }

        [Serializable]
        private sealed class EditEvidence
        {
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public string discoveryPolicy = string.Empty;
            public int catalogCount;
            public string[] catalogIds = Array.Empty<string>();
            public EndingDefinitionEvidence[] longStayDefinitions = Array.Empty<EndingDefinitionEvidence>();
            public int day20Threshold = 20;
            public int standardFinalDay;
            public string[] standardDay50Ids = Array.Empty<string>();
            public string day50ProbeEndingId = string.Empty;
            public string sessionProfileId = string.Empty;
            public int settlementDay;
            public string settlementResultCode = string.Empty;
            public string naturalResolverEndingId = string.Empty;
            public string naturalReplayEndingId = string.Empty;
            public string engineerResolverEndingId = string.Empty;
            public string earlyEscapeResolverEndingId = string.Empty;
            public string observationOwner = string.Empty;
            public string observationMethod = string.Empty;
            public string observationSurface = string.Empty;
            public string localizationSurface = string.Empty;
        }

        [Serializable]
        private sealed class BranchEvidence
        {
            public string kind = string.Empty;
            public string evidenceSource = string.Empty;
            public string endingId = string.Empty;
            public string repeatedEndingId = string.Empty;
            public string escapeId = string.Empty;
            public string reason = string.Empty;
            public string interactionTrace = string.Empty;
            public string snapshotFingerprint = string.Empty;
            public string repeatedSnapshotFingerprint = string.Empty;
            public int day = -1;
            public bool terminal;
            public int endingRecordCount = -1;
            public int albumRecordCount = -1;
            public int commitCount = -1;
            public int duplicateAttemptCount = -1;
            public int duplicateEndingDelta = -1;
            public int duplicateAlbumDelta = -1;
            public bool exactlyOnce;
        }

        [Serializable]
        private sealed class LayoutEvidence
        {
            public string endingId = string.Empty;
            public string locale = string.Empty;
            public string screenshot = string.Empty;
            public string screenshotSha256 = string.Empty;
            public string renderedTextFingerprint = string.Empty;
            public string stateFingerprint = string.Empty;
            public int width = -1;
            public int height = -1;
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
        private sealed class PlayEvidence
        {
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public string scene = string.Empty;
            public string discoveryPolicy = string.Empty;
            public string expectedObservationSurface = string.Empty;
            public string observationOwner = string.Empty;
            public string observationMethod = string.Empty;
            public string observationError = string.Empty;
            public string evidenceSource = string.Empty;
            public int catalogCount = -1;
            public string[] catalogIds = Array.Empty<string>();
            public int day20Threshold = -1;
            public int standardFinalDay = -1;
            public BranchEvidence[] branches = Array.Empty<BranchEvidence>();
            public LayoutEvidence[] layouts = Array.Empty<LayoutEvidence>();
            public int grantCallCount = -1;
            public int warpCallCount = -1;
            public int skipCallCount = -1;
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "long-stay-missing-run-id" : Sanitize(value);
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
            PrototypeEndingDefinition[] catalog = PrototypeEndingCatalog.All.ToArray();
            PrototypeEndingDefinition[] longStay = catalog.Where(value => LongStayIds.Contains(value.StableId)).ToArray();
            MethodInfo observation = typeof(KimSurvivalPrototype).GetMethod(
                ObservationMethod, PublicInstance, null, Type.EmptyTypes, null);
            string localizationSurface = ObserveLocalization(out bool localizationPass);

            PrototypeRunSnapshot day50 = new PrototypeRunSnapshot
            {
                seed = 2050,
                day = GameSession.FinalDay,
                result_code = "day50.settlement",
                behavior_scores = new[] { new PrototypeBehaviorScore { StableId = "stat.building", Value = 12 } }
            };
            string day50ProbeId = PrototypeEndingResolver.ResolveEndingDeterministicSingle(day50).StableId;
            PrototypeRunSnapshot naturalSnapshot = GameJamSnapshot(2020,
                new PrototypeBehaviorScore { StableId = "stat.search", Value = 4 },
                new PrototypeBehaviorScore { StableId = "stat.farming", Value = 7 },
                new PrototypeBehaviorScore { StableId = "stat.building", Value = 5 });
            PrototypeRunSnapshot engineerSnapshot = GameJamSnapshot(2021,
                new PrototypeBehaviorScore { StableId = "stat.search", Value = 3 },
                new PrototypeBehaviorScore { StableId = "stat.building", Value = 6 },
                new PrototypeBehaviorScore { StableId = "stat.mechanics", Value = 5 });
            PrototypeRunSnapshot earlyEscapeSnapshot = GameJamSnapshot(2022,
                new PrototypeBehaviorScore { StableId = "stat.building", Value = 20 });
            earlyEscapeSnapshot.escape_id = "escape.raft";
            earlyEscapeSnapshot.result_code = "escape.complete";
            PrototypeEndingResolution naturalResolution = PrototypeEndingResolver.ResolveEndingDeterministicSingle(naturalSnapshot);
            PrototypeEndingResolution naturalReplay = PrototypeEndingResolver.ResolveEndingDeterministicSingle(naturalSnapshot);
            PrototypeEndingResolution engineerResolution = PrototypeEndingResolver.ResolveEndingDeterministicSingle(engineerSnapshot);
            PrototypeEndingResolution earlyEscapeResolution = PrototypeEndingResolver.ResolveEndingDeterministicSingle(earlyEscapeSnapshot);

            Infrastructure(checks, "GJLS-I01.exact_baseline", "gate identity", "P0",
                "A fresh RunId and exact full baseline SHA are recorded",
                delegate
                {
                    Require(RunId != "long-stay-missing-run-id", "KIM_PARALLEL_QA_RUN_ID is missing");
                    Require(BaselineCommit.Length == 40, "baseline is not a full SHA: " + BaselineCommit);
                    return "runId=" + RunId + "; baseline=" + BaselineCommit + "; redBaseline=" + IsRedBaseline;
                },
                "Invoke the independent PowerShell wrapper with a fresh RunId at the exact requested HEAD.",
                "Assets/Editor/ParallelQA/Invoke-GameJamLongStayRedFirstGate.ps1");

            Product(checks, "GJLS-E01.catalog_21_two_stable_ids", "integrated GDD ending catalog", "P0",
                "The runtime catalog contains exactly 21 distinct entries (the old 19-entry catalog is rejected) and each required stable ID exactly once",
                delegate
                {
                    Require(catalog.Length == 21, "catalog count=" + catalog.Length + "; expected=21; legacy count 19 is not accepted");
                    Require(catalog.Select(value => value.StableId).Distinct(StringComparer.Ordinal).Count() == 21,
                        "catalog IDs are not unique");
                    foreach (string id in LongStayIds)
                    {
                        Require(catalog.Count(value => value.StableId == id) == 1, id + " count is not 1");
                    }
                    return "catalog=21; IDs=" + string.Join(",", LongStayIds);
                },
                "Enumerate PrototypeEndingCatalog.All; do not count Forge or GDD rows.",
                "runtime ending catalog");

            Product(checks, "GJLS-E02.day20_definition_schema", "integrated GDD long-stay definitions", "P0",
                "Both IDs are day-20 no-escape definitions with three core comic panels and stable achievement mappings",
                delegate
                {
                    Require(longStay.Length == 2, "long-stay definitions=" + longStay.Length);
                    foreach (PrototypeEndingDefinition definition in longStay)
                    {
                        Require(string.Equals(definition.Category, "gamejam-stay", StringComparison.Ordinal),
                            definition.StableId + " category=" + definition.Category);
                        Require(string.IsNullOrEmpty(definition.RequiredEscapeId), definition.StableId + " unexpectedly requires escape");
                        Require(definition.ComicPanelKeys.Length == 3 && definition.ComicPanelRoleIds.Length == 3,
                            definition.StableId + " core panel schema is not 3/3");
                        Require(!string.IsNullOrWhiteSpace(definition.AchievementMappingId), definition.StableId + " achievement mapping is empty");
                    }
                    return string.Join(" | ", longStay.Select(DescribeDefinition).ToArray());
                },
                "Inspect the public runtime definitions; Play resolution remains independently required.",
                "runtime ending catalog/resolver");

            Product(checks, "GJLS-E03.public_resolver_snapshot_contract", "integrated GDD deterministic resolution", "P0",
                "The new public snapshot fields select natural-kim and island-engineer deterministically while early escape wins",
                delegate
                {
                    Require(naturalSnapshot.session_profile_id == PrototypeEndingResolver.GameJamSessionProfileId &&
                            naturalSnapshot.settlement_day == PrototypeEndingResolver.GameJamSettlementDay &&
                            naturalSnapshot.settlement_result_code == PrototypeEndingResolver.GameJamSettlementResultCode,
                        "natural snapshot is missing the public Game Jam profile/settlement tuple");
                    Require(naturalResolution.StableId == NaturalId && naturalReplay.StableId == NaturalId &&
                            naturalResolution.PanelKeys.SequenceEqual(naturalReplay.PanelKeys) &&
                            naturalResolution.AchievementMappingId == naturalReplay.AchievementMappingId,
                        "natural resolution/replay=" + naturalResolution.StableId + "/" + naturalReplay.StableId);
                    Require(engineerResolution.StableId == EngineerId,
                        "engineer resolution=" + engineerResolution.StableId);
                    Require(earlyEscapeResolution.StableId == "ending.escape.raft.open-water",
                        "early escape resolution=" + earlyEscapeResolution.StableId);
                    return "profile=" + naturalSnapshot.session_profile_id + "; day=" + naturalSnapshot.settlement_day +
                           "; result=" + naturalSnapshot.settlement_result_code + "; natural/replay=" +
                           naturalResolution.StableId + "/" + naturalReplay.StableId + "; engineer=" +
                           engineerResolution.StableId + "; early=" + earlyEscapeResolution.StableId;
                },
                "Resolve public PrototypeRunSnapshot instances carrying the Game Jam session-profile settlement tuple.",
                "runtime ending resolver and PrototypeRunSnapshot");

            Product(checks, "GJLS-E04.structured_live_observation_surface", "automated Play observability", "P0",
                "KimSurvivalPrototype exposes one public zero-argument structured observation for actual branches, replay, terminal/album, layouts, and cheat counters",
                delegate
                {
                    Require(observation != null, "public " + ObservationMethod + "() is missing");
                    Require(observation.ReturnType != typeof(void) && observation.ReturnType != typeof(bool) &&
                            observation.ReturnType != typeof(string) && !observation.ReturnType.IsPrimitive,
                        "observation must return structured data, not " + observation.ReturnType.FullName);
                    string surface = DescribeSurface(observation.ReturnType);
                    string lower = surface.ToLowerInvariant();
                    foreach (string token in new[] { "natural", "engineer", "escape", "day50", "layout", "grant", "warp", "skip" })
                    {
                        Require(lower.Contains(token), "observation surface lacks " + token + ": " + surface);
                    }
                    return surface;
                },
                "Reflect the production component's public structured observation surface; a static fixture is not accepted.",
                "runtime observation owner");

            Product(checks, "GJLS-E05.localization_ko_en_qps", "integrated GDD localized comic", "P1",
                "Each long-stay ending has non-empty title/summary/hint rows in KO, EN, and qps-long",
                delegate
                {
                    Require(localizationPass, localizationSurface);
                    return localizationSurface;
                },
                "Parse the canonical TSV and require every cell; Play still requires live rendering.",
                "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            Product(checks, "GJLS-E06.standard_day50_unchanged", "integrated GDD standard campaign invariant", "P0",
                "The standard campaign remains Day50 with its five stable endings and building dominance resolves fortress-manager",
                delegate
                {
                    Require(GameSession.FinalDay == 50, "GameSession.FinalDay=" + GameSession.FinalDay);
                    string[] observed = catalog.Where(value => value.Category == "day50").Select(value => value.StableId)
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                    Require(observed.SequenceEqual(StandardDay50Ids.OrderBy(value => value, StringComparer.Ordinal)),
                        "day50 IDs=" + string.Join(",", observed));
                    Require(day50ProbeId == "ending.stay.fortress-manager", "day50 probe=" + day50ProbeId);
                    return "FinalDay=50; IDs=5; probe=" + day50ProbeId;
                },
                "Resolve a real Day50 runtime snapshot with building dominance.",
                "GameSession and runtime ending resolver");

            EditEvidence evidence = new EditEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                discoveryPolicy = "Runtime catalog/resolver/reflection and canonical TSV only; fixture pass booleans are excluded.",
                catalogCount = catalog.Length,
                catalogIds = catalog.Select(value => value.StableId).ToArray(),
                longStayDefinitions = longStay.Select(ToDefinitionEvidence).ToArray(),
                standardFinalDay = GameSession.FinalDay,
                standardDay50Ids = catalog.Where(value => value.Category == "day50").Select(value => value.StableId).ToArray(),
                day50ProbeEndingId = day50ProbeId,
                sessionProfileId = naturalSnapshot.session_profile_id,
                settlementDay = naturalSnapshot.settlement_day,
                settlementResultCode = naturalSnapshot.settlement_result_code,
                naturalResolverEndingId = naturalResolution.StableId,
                naturalReplayEndingId = naturalReplay.StableId,
                engineerResolverEndingId = engineerResolution.StableId,
                earlyEscapeResolverEndingId = earlyEscapeResolution.StableId,
                observationOwner = typeof(KimSurvivalPrototype).FullName,
                observationMethod = ObservationMethod,
                observationSurface = observation == null ? "MISSING" : DescribeSurface(observation.ReturnType),
                localizationSurface = localizationSurface
            };
            WriteJson("gamejam-long-stay-edit-observation-evidence.json", evidence);
            WriteReport("gamejam-long-stay-edit-contracts", "GameJam long-stay endings RED-first Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "GameJam long-stay Play runner did not complete");
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
            if (playTickAttached) return;
            EditorApplication.update += PlayTick;
            playTickAttached = true;
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
                WritePlayInfrastructureFailure(new TimeoutException("GameJam long-stay Play runner timed out."));
                StopPlayContracts();
                return;
            }
            if (EditorApplication.timeSinceStartup < playEarliestRunTime) return;
            EditorApplication.update -= PlayTick;
            playTickAttached = false;

            try
            {
                DateTime started = DateTime.UtcNow;
                PlayEvidence evidence = ObservePlay();
                PreserveLongStayLayoutScreenshots(evidence);
                List<Check> checks = new List<Check>();

                Product(checks, "GJLS-P01.catalog_21_live", "integrated GDD ending catalog", "P0",
                    "The active production observation sees exactly 21 distinct catalog IDs including both long-stay IDs",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(evidence.catalogCount == 21 && evidence.catalogIds.Distinct(StringComparer.Ordinal).Count() == 21,
                            "catalog=" + evidence.catalogCount + "; distinct=" + evidence.catalogIds.Distinct(StringComparer.Ordinal).Count());
                        Require(LongStayIds.All(id => evidence.catalogIds.Count(value => value == id) == 1),
                            "required IDs=" + string.Join(",", evidence.catalogIds.Where(LongStayIds.Contains).ToArray()));
                        return "catalog=21; required IDs present once";
                    },
                    "Capture the catalog from the active runtime observation, not a serialized fixture.",
                    "runtime long-stay observation");

                Product(checks, "GJLS-P02.day20_natural_kim", "integrated GDD natural-kim ending", "P0",
                    "A production-input no-escape Day20 natural/survival profile terminates as natural-kim with a readable reason",
                    delegate
                    {
                        RequireLive(evidence);
                        BranchEvidence branch = RequireBranch(evidence, "natural");
                        Require(branch.day == 20 && branch.terminal && branch.endingId == NaturalId && string.IsNullOrEmpty(branch.escapeId),
                            DescribeBranch(branch));
                        Require(!string.IsNullOrWhiteSpace(branch.reason) && IsProductionTrace(branch), "reason or production trace is missing");
                        return DescribeBranch(branch);
                    },
                    "Play the no-escape natural/survival profile through production actions until Day20 settles.",
                    "runtime day20 resolver and production observation");

                Product(checks, "GJLS-P03.day20_island_engineer", "integrated GDD island-engineer ending", "P0",
                    "A production-input no-escape Day20 building/mechanics profile terminates as island-engineer with a readable reason",
                    delegate
                    {
                        RequireLive(evidence);
                        BranchEvidence branch = RequireBranch(evidence, "engineer");
                        Require(branch.day == 20 && branch.terminal && branch.endingId == EngineerId && string.IsNullOrEmpty(branch.escapeId),
                            DescribeBranch(branch));
                        Require(!string.IsNullOrWhiteSpace(branch.reason) && IsProductionTrace(branch), "reason or production trace is missing");
                        return DescribeBranch(branch);
                    },
                    "Play the no-escape building/mechanics profile through production actions until Day20 settles.",
                    "runtime day20 resolver and production observation");

                Product(checks, "GJLS-P04.early_escape_priority", "integrated GDD terminal precedence", "P0",
                    "A genuine early escape resolves an escape ending before the Day20 long-stay settlement",
                    delegate
                    {
                        RequireLive(evidence);
                        BranchEvidence branch = RequireBranch(evidence, "early-escape");
                        Require(branch.terminal && branch.day <= 20 && !string.IsNullOrWhiteSpace(branch.escapeId), DescribeBranch(branch));
                        Require(!LongStayIds.Contains(branch.endingId), "long-stay incorrectly won: " + branch.endingId);
                        Require(branch.endingId.StartsWith("ending.escape.", StringComparison.Ordinal) ||
                                branch.endingId.StartsWith("ending.comic.", StringComparison.Ordinal) ||
                                branch.endingId.StartsWith("ending.rare.", StringComparison.Ordinal),
                            "not an escape-family ending: " + branch.endingId);
                        Require(IsProductionTrace(branch), "production interaction trace is missing");
                        return DescribeBranch(branch);
                    },
                    "Complete an actual escape route before or at Day20 using production controls.",
                    "runtime terminal precedence and escape interaction");

                Product(checks, "GJLS-P05.same_snapshot_determinism", "integrated GDD deterministic settlement", "P0",
                    "Replaying the exact same captured snapshot resolves the same ID and fingerprint for both long-stay branches",
                    delegate
                    {
                        RequireLive(evidence);
                        foreach (string kind in new[] { "natural", "engineer" })
                        {
                            BranchEvidence branch = RequireBranch(evidence, kind);
                            Require(SameNonEmpty(branch.endingId, branch.repeatedEndingId), kind + " ending replay differs");
                            Require(SameNonEmpty(branch.snapshotFingerprint, branch.repeatedSnapshotFingerprint),
                                kind + " snapshot replay fingerprint differs");
                        }
                        return "natural+engineer same-snapshot replay stable";
                    },
                    "Capture each production snapshot, resolve it twice through the same public product path, and compare fingerprints.",
                    "runtime snapshot and deterministic resolver");

                Product(checks, "GJLS-P06.terminal_album_exactly_once", "integrated GDD terminal persistence", "P0",
                    "Both long-stay branches commit terminal and album records exactly once; duplicate actuation has zero record deltas",
                    delegate
                    {
                        RequireLive(evidence);
                        foreach (string kind in new[] { "natural", "engineer" })
                        {
                            BranchEvidence branch = RequireBranch(evidence, kind);
                            Require(branch.terminal && branch.exactlyOnce && branch.commitCount == 1 &&
                                    branch.endingRecordCount == 1 && branch.albumRecordCount == 1,
                                kind + " once contract: " + DescribeBranch(branch));
                            Require(branch.duplicateAttemptCount >= 1 && branch.duplicateEndingDelta == 0 && branch.duplicateAlbumDelta == 0,
                                kind + " duplicate deltas=" + branch.duplicateEndingDelta + "/" + branch.duplicateAlbumDelta);
                        }
                        return "terminal=1; ending record=1; album record=1; duplicate deltas=0 for both branches";
                    },
                    "Resolve each live terminal once, actuate it again, and observe recorder counts and deltas.",
                    "runtime terminal and ending album recorders");

                Product(checks, "GJLS-P07.live_comic_ko_en_qps", "integrated GDD localized comic", "P1",
                    "Both endings render 3 core panels plus at least 1 modifier in KO/EN/qps-long at 1280x800 with distinct localized text, clipping 0, no rendered text overlap, and every title/body inside its owning card",
                    delegate
                    {
                        RequireLive(evidence);
                        foreach (string endingId in LongStayIds)
                        {
                            LayoutEvidence[] layouts = evidence.layouts.Where(value => value.endingId == endingId).ToArray();
                            Require(layouts.Select(value => value.locale).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(Locales),
                                endingId + " locales=" + string.Join(",", layouts.Select(value => value.locale).ToArray()));
                            Require(layouts.All(value => value.width == 1280 && value.height == 800 && value.corePanelCount == 3 &&
                                                              value.modifierPanelCount >= 1 && value.overflowCount == 0 &&
                                                              value.offscreenCount == 0 && value.clippedRequiredActionCount == 0 &&
                                                              value.activeGeometryTextCount >= 8 && value.textTextOverlapCount == 0 &&
                                                              value.textCardBoundaryViolationCount == 0 && value.titleFontSize >= 18f &&
                                                              value.minimumCoreFontSize >= 12f && value.modifierFontSize >= 13f),
                                endingId + " layout=" + string.Join(" | ", layouts.Select(DescribeLayout).ToArray()));
                            Require(layouts.All(value => ScreenshotIs1280x800(value.screenshot)), endingId + " screenshot missing or wrong size");
                            Require(layouts.All(value => !string.IsNullOrWhiteSpace(value.renderedTextFingerprint)) &&
                                    layouts.Select(value => value.renderedTextFingerprint).Distinct(StringComparer.Ordinal).Count() == 3,
                                endingId + " rendered text is not distinct across locales");
                            Require(layouts.All(value => !string.IsNullOrWhiteSpace(value.stateFingerprint)) &&
                                    layouts.Select(value => value.stateFingerprint).Distinct(StringComparer.Ordinal).Count() == 1,
                                endingId + " locale captures do not share runtime state");
                        }
                        return "2 endings x 3 locales; 3+modifier; 1280x800; clipping=0";
                    },
                    "Capture each active terminal comic in KO, EN, and qps-long from one unchanged runtime state.",
                    "active ending renderer/localization observation");

                Product(checks, "GJLS-P08.standard_day50_live_unchanged", "integrated GDD standard campaign invariant", "P0",
                    "A production-input standard campaign still settles at Day50 to one of the original five Day50 endings",
                    delegate
                    {
                        RequireLive(evidence);
                        BranchEvidence branch = RequireBranch(evidence, "day50");
                        Require(evidence.standardFinalDay == 50 && branch.day == 50 && branch.terminal,
                            "standard final day=" + evidence.standardFinalDay + "; " + DescribeBranch(branch));
                        Require(StandardDay50Ids.Contains(branch.endingId), "nonstandard Day50 ending=" + branch.endingId);
                        Require(IsProductionTrace(branch), "Day50 production trace is missing");
                        return DescribeBranch(branch);
                    },
                    "Run the standard campaign path without a Day20 Game Jam settlement override.",
                    "GameSession final-day and production observation");

                Product(checks, "GJLS-P09.no_grant_warp_skip", "integrated GDD natural-path integrity", "P0",
                    "All observed branches and locale captures use grant/warp/skip counters of exactly zero",
                    delegate
                    {
                        RequireLive(evidence);
                        Require(evidence.grantCallCount == 0 && evidence.warpCallCount == 0 && evidence.skipCallCount == 0,
                            "grant/warp/skip=" + evidence.grantCallCount + "/" + evidence.warpCallCount + "/" + evidence.skipCallCount);
                        Require(evidence.branches.Length >= 4 && evidence.branches.All(IsProductionTrace),
                            "all four branches require production-live interaction traces");
                        return "grant=0; warp=0; skip=0; production branches=" + evidence.branches.Length;
                    },
                    "Record counters around every production-input branch; helper mutation is forbidden.",
                    "runtime production action recorder");

                WriteJson("gamejam-long-stay-play-observation-evidence.json", evidence);
                Report report = WriteReport("gamejam-long-stay-play-contracts",
                    "GameJam long-stay endings RED-first actual Play contracts", started, checks);
                SessionState.SetBool(PlayExitPassKey, report.infrastructureOverall == "PASS");
                SessionState.SetString(PlayMessageKey, "GameJam long-stay Play contracts: " + report.overall);
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static PlayEvidence ObservePlay()
        {
            PlayEvidence evidence = new PlayEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                scene = SceneManager.GetActiveScene().path,
                discoveryPolicy = "Active-scene public structured production observation only. Fixture/static bool/string, grant, warp, and skip are rejected.",
                expectedObservationSurface = "catalogCount/catalogIds/day20Threshold/standardFinalDay; natural/engineer/earlyEscape/day50 branches with replay and terminal counts; 2x KO/EN/qps layouts; grant/warp/skip counters",
                observationMethod = ObservationMethod
            };

            KimSurvivalPrototype prototype = Resources.FindObjectsOfTypeAll<KimSurvivalPrototype>()
                .FirstOrDefault(value => value != null && value.gameObject.scene.IsValid() && value.gameObject.activeInHierarchy);
            if (prototype == null)
            {
                evidence.observationError = "active KimSurvivalPrototype was not found";
                return evidence;
            }

            evidence.observationOwner = prototype.GetType().FullName;
            MethodInfo method = prototype.GetType().GetMethod(ObservationMethod, PublicInstance, null, Type.EmptyTypes, null);
            if (method == null)
            {
                evidence.observationError = "public structured observation method " + ObservationMethod + "() is missing";
                return evidence;
            }
            if (method.ReturnType == typeof(void) || method.ReturnType == typeof(bool) || method.ReturnType == typeof(string) || method.ReturnType.IsPrimitive)
            {
                evidence.observationError = "observation return type is not structured: " + method.ReturnType.FullName;
                return evidence;
            }

            try
            {
                object raw = method.Invoke(prototype, null);
                Require(raw != null, "observation returned null");
                evidence.observationError = ReadString(raw, "ObservationError", "observationError", "Error", "error");
                evidence.evidenceSource = ReadString(raw, "EvidenceSource", "evidenceSource", "Source", "source");
                evidence.catalogCount = ReadInt(raw, -1, "CatalogCount", "catalogCount");
                evidence.catalogIds = ReadStrings(raw, "CatalogIds", "catalogIds", "EndingIds", "endingIds");
                evidence.day20Threshold = ReadInt(raw, -1, "Day20Threshold", "day20Threshold", "SettlementDay", "settlementDay");
                evidence.standardFinalDay = ReadInt(raw, -1, "StandardFinalDay", "standardFinalDay", "FinalDay", "finalDay");
                evidence.grantCallCount = ReadInt(raw, -1, "GrantCallCount", "grantCallCount");
                evidence.warpCallCount = ReadInt(raw, -1, "WarpCallCount", "warpCallCount");
                evidence.skipCallCount = ReadInt(raw, -1, "SkipCallCount", "skipCallCount");

                var branches = new List<BranchEvidence>();
                AddBranch(branches, "natural", GetMember(raw, "Natural", "natural", "NaturalKim", "naturalKim", "NaturalBranch", "naturalBranch"));
                AddBranch(branches, "engineer", GetMember(raw, "Engineer", "engineer", "IslandEngineer", "islandEngineer", "EngineerBranch", "engineerBranch"));
                AddBranch(branches, "early-escape", GetMember(raw, "EarlyEscape", "earlyEscape", "EarlyEscapeBranch", "earlyEscapeBranch"));
                AddBranch(branches, "day50", GetMember(raw, "Day50", "day50", "StandardDay50", "standardDay50", "Day50Branch", "day50Branch"));
                object branchCollection = GetMember(raw, "Branches", "branches");
                if (branchCollection is IEnumerable enumerable && !(branchCollection is string))
                {
                    foreach (object item in enumerable)
                    {
                        if (item == null) continue;
                        string kind = ReadString(item, "Kind", "kind", "BranchId", "branchId");
                        if (!branches.Any(value => string.Equals(value.kind, kind, StringComparison.OrdinalIgnoreCase)))
                            AddBranch(branches, kind, item);
                    }
                }
                evidence.branches = branches.Where(value => !string.IsNullOrWhiteSpace(value.kind)).ToArray();

                object layoutCollection = GetMember(raw, "Layouts", "layouts", "LocaleLayouts", "localeLayouts");
                evidence.layouts = ReadObjects(layoutCollection).Select(ToLayoutEvidence).ToArray();
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                evidence.observationError = inner.GetType().Name + ": " + inner.Message;
            }
            catch (Exception exception)
            {
                evidence.observationError = exception.GetType().Name + ": " + exception.Message;
            }
            return evidence;
        }

        private static void AddBranch(List<BranchEvidence> branches, string kind, object raw)
        {
            if (raw == null || string.IsNullOrWhiteSpace(kind)) return;
            branches.Add(new BranchEvidence
            {
                kind = NormalizeKind(kind),
                evidenceSource = ReadString(raw, "EvidenceSource", "evidenceSource", "Source", "source"),
                endingId = ReadString(raw, "EndingId", "endingId"),
                repeatedEndingId = ReadString(raw, "RepeatedEndingId", "repeatedEndingId", "ReplayEndingId", "replayEndingId"),
                escapeId = ReadString(raw, "EscapeId", "escapeId"),
                reason = ReadString(raw, "Reason", "reason", "DeterministicReason", "deterministicReason"),
                interactionTrace = ReadString(raw, "InteractionTrace", "interactionTrace", "ProductionTrace", "productionTrace"),
                snapshotFingerprint = ReadString(raw, "SnapshotFingerprint", "snapshotFingerprint", "FirstSnapshotFingerprint", "firstSnapshotFingerprint"),
                repeatedSnapshotFingerprint = ReadString(raw, "RepeatedSnapshotFingerprint", "repeatedSnapshotFingerprint", "ReplaySnapshotFingerprint", "replaySnapshotFingerprint"),
                day = ReadInt(raw, -1, "Day", "day"),
                terminal = ReadBool(raw, false, "Terminal", "terminal"),
                endingRecordCount = ReadInt(raw, -1, "EndingRecordCount", "endingRecordCount"),
                albumRecordCount = ReadInt(raw, -1, "AlbumRecordCount", "albumRecordCount"),
                commitCount = ReadInt(raw, -1, "CommitCount", "commitCount", "TerminalCommitCount", "terminalCommitCount"),
                duplicateAttemptCount = ReadInt(raw, -1, "DuplicateAttemptCount", "duplicateAttemptCount"),
                duplicateEndingDelta = ReadInt(raw, -1, "DuplicateEndingDelta", "duplicateEndingDelta"),
                duplicateAlbumDelta = ReadInt(raw, -1, "DuplicateAlbumDelta", "duplicateAlbumDelta"),
                exactlyOnce = ReadBool(raw, false, "ExactlyOnce", "exactlyOnce")
            });
        }

        private static LayoutEvidence ToLayoutEvidence(object raw)
        {
            return new LayoutEvidence
            {
                endingId = ReadString(raw, "EndingId", "endingId"),
                locale = ReadString(raw, "Locale", "locale"),
                screenshot = ReadString(raw, "Screenshot", "screenshot", "ScreenshotPath", "screenshotPath"),
                renderedTextFingerprint = ReadString(raw, "RenderedTextFingerprint", "renderedTextFingerprint"),
                stateFingerprint = ReadString(raw, "StateFingerprint", "stateFingerprint"),
                width = ReadInt(raw, -1, "Width", "width"),
                height = ReadInt(raw, -1, "Height", "height"),
                corePanelCount = ReadInt(raw, -1, "CorePanelCount", "corePanelCount"),
                modifierPanelCount = ReadInt(raw, -1, "ModifierPanelCount", "modifierPanelCount"),
                overflowCount = ReadInt(raw, -1, "OverflowCount", "overflowCount"),
                offscreenCount = ReadInt(raw, -1, "OffscreenCount", "offscreenCount"),
                clippedRequiredActionCount = ReadInt(raw, -1, "ClippedRequiredActionCount", "clippedRequiredActionCount"),
                activeGeometryTextCount = ReadInt(raw, -1, "ActiveGeometryTextCount", "activeGeometryTextCount"),
                textTextOverlapCount = ReadInt(raw, -1, "TextTextOverlapCount", "textTextOverlapCount"),
                textCardBoundaryViolationCount = ReadInt(raw, -1, "TextCardBoundaryViolationCount", "textCardBoundaryViolationCount"),
                titleFontSize = ReadFloat(raw, -1f, "TitleFontSize", "titleFontSize"),
                minimumCoreFontSize = ReadFloat(raw, -1f, "MinimumCoreFontSize", "minimumCoreFontSize"),
                modifierFontSize = ReadFloat(raw, -1f, "ModifierFontSize", "modifierFontSize"),
                geometryViolations = ReadStrings(raw, "GeometryViolations", "geometryViolations", "Violations", "violations")
            };
        }

        private static EndingDefinitionEvidence ToDefinitionEvidence(PrototypeEndingDefinition definition)
        {
            return new EndingDefinitionEvidence
            {
                stableId = definition.StableId,
                category = definition.Category,
                priority = definition.Priority,
                conditionCount = definition.ConditionCount,
                requiredEscapeId = definition.RequiredEscapeId,
                requiredEventId = definition.RequiredEventId,
                requiredBehaviorId = definition.RequiredBehaviorId,
                comicPanelKeys = definition.ComicPanelKeys,
                comicPanelRoleIds = definition.ComicPanelRoleIds,
                achievementMappingId = definition.AchievementMappingId
            };
        }

        private static PrototypeRunSnapshot GameJamSnapshot(int seed, params PrototypeBehaviorScore[] scores)
        {
            return new PrototypeRunSnapshot
            {
                seed = seed,
                day = PrototypeEndingResolver.GameJamSettlementDay,
                session_profile_id = PrototypeEndingResolver.GameJamSessionProfileId,
                settlement_day = PrototypeEndingResolver.GameJamSettlementDay,
                settlement_result_code = PrototypeEndingResolver.GameJamSettlementResultCode,
                behavior_scores = scores ?? Array.Empty<PrototypeBehaviorScore>(),
                result_code = PrototypeEndingResolver.GameJamSettlementResultCode
            };
        }

        private static string ObserveLocalization(out bool pass)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "_Project", "Scripts", "Localization", "PrototypeStrings.tsv"));
            if (!File.Exists(path))
            {
                pass = false;
                return "canonical TSV is missing: " + path;
            }
            string[] lines = File.ReadAllLines(path);
            string[] suffixes = { ".title", ".summary", ".hint" };
            var missing = new List<string>();
            foreach (string id in LongStayIds)
            {
                foreach (string suffix in suffixes)
                {
                    string key = id + suffix;
                    string line = lines.FirstOrDefault(value => value.StartsWith(key + "\t", StringComparison.Ordinal));
                    if (line == null)
                    {
                        missing.Add(key);
                        continue;
                    }
                    string[] cells = line.Split('\t');
                    if (cells.Length < 4 || string.IsNullOrWhiteSpace(cells[1]) || string.IsNullOrWhiteSpace(cells[2]) || string.IsNullOrWhiteSpace(cells[3]))
                        missing.Add(key + "[ko/en/qps]");
                }
            }
            pass = missing.Count == 0;
            return pass ? "6 keys x KO/EN/qps-long non-empty" : "missing/empty=" + string.Join(",", missing.ToArray());
        }

        private static string DescribeSurface(Type type)
        {
            if (type == null) return "MISSING";
            string[] names = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(value => value.MemberType == MemberTypes.Field || value.MemberType == MemberTypes.Property)
                .Select(value => value.Name).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return type.FullName + "{" + string.Join(",", names) + "}";
        }

        private static object GetMember(object owner, params string[] names)
        {
            if (owner == null) return null;
            Type type = owner.GetType();
            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null) return field.GetValue(owner);
                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(owner, null);
            }
            return null;
        }

        private static IEnumerable<object> ReadObjects(object value)
        {
            if (!(value is IEnumerable enumerable) || value is string) return Enumerable.Empty<object>();
            return enumerable.Cast<object>().Where(item => item != null);
        }

        private static string ReadString(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            return value == null ? string.Empty : Convert.ToString(value) ?? string.Empty;
        }

        private static string[] ReadStrings(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<string>();
            return enumerable.Cast<object>().Where(item => item != null).Select(item => Convert.ToString(item) ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        }

        private static int ReadInt(object owner, int fallback, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value == null) return fallback;
            try { return Convert.ToInt32(value); } catch { return fallback; }
        }

        private static float ReadFloat(object owner, float fallback, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value == null) return fallback;
            try { return Convert.ToSingle(value); } catch { return fallback; }
        }

        private static bool ReadBool(object owner, bool fallback, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value == null) return fallback;
            try { return Convert.ToBoolean(value); } catch { return fallback; }
        }

        private static BranchEvidence RequireBranch(PlayEvidence evidence, string kind)
        {
            BranchEvidence branch = evidence.branches.FirstOrDefault(value =>
                string.Equals(NormalizeKind(value.kind), NormalizeKind(kind), StringComparison.Ordinal));
            Require(branch != null, "branch is missing: " + kind);
            return branch;
        }

        private static string NormalizeKind(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant().Replace("_", "-");
            if (normalized == "naturalkim" || normalized == "natural-kim") return "natural";
            if (normalized == "islandengineer" || normalized == "island-engineer") return "engineer";
            if (normalized == "earlyescape") return "early-escape";
            if (normalized == "standardday50" || normalized == "standard-day50") return "day50";
            return normalized;
        }

        private static void RequireLive(PlayEvidence evidence)
        {
            Require(string.IsNullOrWhiteSpace(evidence.observationError), "observationError=" + evidence.observationError);
            string source = evidence.evidenceSource.ToLowerInvariant();
            Require(source.Contains("production") && source.Contains("live") && !source.Contains("fixture"),
                "evidenceSource must be production-live and non-fixture: " + evidence.evidenceSource);
        }

        private static bool IsProductionTrace(BranchEvidence branch)
        {
            if (branch == null || string.IsNullOrWhiteSpace(branch.interactionTrace)) return false;
            string source = branch.evidenceSource.ToLowerInvariant();
            return source.Contains("production") && source.Contains("live") && !source.Contains("fixture") &&
                   !branch.interactionTrace.Contains("fixture", StringComparison.OrdinalIgnoreCase) &&
                   !branch.interactionTrace.Contains("grant", StringComparison.OrdinalIgnoreCase) &&
                   !branch.interactionTrace.Contains("warp", StringComparison.OrdinalIgnoreCase) &&
                   !branch.interactionTrace.Contains("skip", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ScreenshotIs1280x800(string value)
        {
            string path = ResolveEvidencePath(value);
            if (!File.Exists(path)) return false;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                return ImageConversion.LoadImage(texture, bytes, false) && texture.width == 1280 && texture.height == 800;
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static void PreserveLongStayLayoutScreenshots(PlayEvidence evidence)
        {
            Require(evidence != null, "long-stay play evidence is missing");
            Require(evidence.layouts != null && evidence.layouts.Length == LongStayIds.Length * Locales.Length,
                "long-stay layout count must be exactly 6, observed=" + (evidence.layouts == null ? -1 : evidence.layouts.Length));

            Directory.CreateDirectory(EvidenceFolder);
            var observedKeys = new HashSet<string>(StringComparer.Ordinal);
            var observedScreenshotShas = new HashSet<string>(StringComparer.Ordinal);
            foreach (LayoutEvidence layout in evidence.layouts)
            {
                Require(layout != null, "long-stay layout entry is null");
                Require(LongStayIds.Contains(layout.endingId), "non-long-stay layout cannot satisfy visual evidence: " + layout.endingId);
                Require(Locales.Contains(layout.locale), "unsupported long-stay layout locale: " + layout.locale);

                string branch = layout.endingId == NaturalId ? "natural" : "engineer";
                string endingSlug = layout.endingId == NaturalId ? "natural-kim" : "island-engineer";
                string key = branch + "/" + layout.locale;
                Require(observedKeys.Add(key), "duplicate long-stay layout evidence: " + key);
                Require(Path.IsPathRooted(layout.screenshot),
                    "production layout must expose its absolute staging capture before preservation: " + key);

                string sourcePath = Path.GetFullPath(layout.screenshot);
                RequireExpectedStagingCapturePath(sourcePath, branch, layout.locale);
                Require(ScreenshotIs1280x800(sourcePath), "staging screenshot missing or wrong size: " + sourcePath);

                string fileName = "gamejam-long-stay-" + endingSlug + "-" + layout.locale + "-1280x800.png";
                string destinationPath = Path.GetFullPath(Path.Combine(EvidenceFolder, fileName));
                Require(!File.Exists(destinationPath), "stable long-stay screenshot already exists: " + fileName);
                File.Copy(sourcePath, destinationPath, false);

                string sourceSha = ComputeSha256(sourcePath);
                string destinationSha = ComputeSha256(destinationPath);
                Require(string.Equals(sourceSha, destinationSha, StringComparison.Ordinal),
                    "preserved screenshot SHA mismatch: " + key);
                Require(ScreenshotIs1280x800(destinationPath), "preserved screenshot missing or wrong size: " + fileName);
                Require(observedScreenshotShas.Add(destinationSha),
                    "a long-stay screenshot was reused for more than one ending/locale: " + key);

                layout.screenshot = fileName;
                layout.screenshotSha256 = destinationSha;
            }

            string[] expectedKeys = LongStayIds.SelectMany(endingId =>
                    Locales.Select(locale => (endingId == NaturalId ? "natural" : "engineer") + "/" + locale))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Require(observedKeys.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(expectedKeys),
                "long-stay visual evidence matrix is incomplete: " + string.Join(",", observedKeys.ToArray()));
            Require(observedScreenshotShas.Count == LongStayIds.Length * Locales.Length,
                "long-stay visual evidence must contain 6 distinct captures, observed=" + observedScreenshotShas.Count);
        }

        private static void RequireExpectedStagingCapturePath(string sourcePath, string branch, string locale)
        {
            FileInfo file = new FileInfo(sourcePath);
            string expectedFileName = "terminal-ending-" + locale + "-1280x800.png";
            Require(file.Exists, "staging screenshot is missing: " + sourcePath);
            Require(string.Equals(file.Name, expectedFileName, StringComparison.Ordinal),
                "unexpected staging screenshot name: " + file.Name + " expected=" + expectedFileName);
            Require(file.Directory != null && string.Equals(file.Directory.Name, branch, StringComparison.Ordinal),
                "staging screenshot branch does not match layout: " + sourcePath);
            Require(file.Directory.Parent != null && string.Equals(file.Directory.Parent.Name, RunId, StringComparison.Ordinal),
                "staging screenshot run ID does not match current run: " + sourcePath);
            Require(file.Directory.Parent.Parent != null &&
                    string.Equals(file.Directory.Parent.Parent.Name, "kim-survival-long-stay", StringComparison.Ordinal),
                "staging screenshot is not from the long-stay capture root: " + sourcePath);

            string evidencePrefix = Path.GetFullPath(EvidenceFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Require(!sourcePath.StartsWith(evidencePrefix, StringComparison.OrdinalIgnoreCase),
                "staging screenshot unexpectedly aliases the evidence destination: " + sourcePath);
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }

        private static string ResolveEvidencePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Path.IsPathRooted(value) ? Path.GetFullPath(value) : Path.GetFullPath(Path.Combine(EvidenceFolder, value));
        }

        private static bool SameNonEmpty(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left) && string.Equals(left, right, StringComparison.Ordinal);
        }

        private static string DescribeDefinition(PrototypeEndingDefinition value)
        {
            return value.StableId + " category=" + value.Category + " priority=" + value.Priority +
                   " panels=" + value.ComicPanelKeys.Length;
        }

        private static string DescribeBranch(BranchEvidence value)
        {
            return value.kind + " day=" + value.day + " ending=" + value.endingId + " escape=" + value.escapeId +
                   " terminal=" + value.terminal + " records=" + value.endingRecordCount + "/" + value.albumRecordCount;
        }

        private static string DescribeLayout(LayoutEvidence value)
        {
            return value.endingId + "/" + value.locale + " " + value.width + "x" + value.height +
                   " core/mod=" + value.corePanelCount + "/" + value.modifierPanelCount +
                   " overflow/offscreen/clipped=" + value.overflowCount + "/" + value.offscreenCount + "/" + value.clippedRequiredActionCount +
                   " geometryTexts/textOverlap/cardBoundary=" + value.activeGeometryTextCount + "/" +
                   value.textTextOverlapCount + "/" + value.textCardBoundaryViolationCount +
                   " fonts(title/core/modifier)=" + value.titleFontSize + "/" + value.minimumCoreFontSize + "/" + value.modifierFontSize;
        }

        private static void Product(List<Check> checks, string id, string gdd, string severity, string expected,
            Func<string> action, string reproduction, string recommendedFiles)
        {
            try
            {
                checks.Add(NewCheck(id, gdd, "PASS", "NONE", severity, expected, action(), reproduction, recommendedFiles));
            }
            catch (Exception exception)
            {
                string status = IsRedBaseline ? "EXPECTED_GAP" : "FAIL";
                string classification = IsRedBaseline ? "PRODUCT_EXPECTED_GAP" : "PRODUCT_REGRESSION";
                checks.Add(NewCheck(id, gdd, status, classification, severity, expected,
                    exception.GetType().Name + ": " + exception.Message, reproduction, recommendedFiles));
            }
        }

        private static void Infrastructure(List<Check> checks, string id, string gdd, string severity, string expected,
            Func<string> action, string reproduction, string recommendedFiles)
        {
            try
            {
                checks.Add(NewCheck(id, gdd, "PASS", "NONE", severity, expected, action(), reproduction, recommendedFiles));
            }
            catch (Exception exception)
            {
                checks.Add(NewCheck(id, gdd, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected,
                    exception.GetType().Name + ": " + exception.Message, reproduction, recommendedFiles));
            }
        }

        private static Check NewCheck(string id, string gdd, string status, string classification, string severity,
            string expected, string actual, string reproduction, string recommendedFiles)
        {
            return new Check
            {
                id = id,
                gdd = gdd,
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
                greenCompletionCondition = "Catalog 21/two stable IDs, both natural Day20 branches, early escape precedence, same-snapshot determinism, exactly-once terminal+album, 2x KO/EN/qps 3+modifier clipping 0, unchanged Day50, and grant/warp/skip 0 all PASS from production-live evidence.",
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
                "PASS/EXPECTED_GAP/FAIL/INFRA_FAIL: " + report.passed + "/" + report.expectedGaps + "/" + report.productFailed + "/" + report.infrastructureFailed
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
            var checks = new List<Check>
            {
                NewCheck("GJLS-I02.play_runner", "Play infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Play runner emits fresh structured JSON/TXT evidence", exception.GetType().Name + ": " + exception.Message,
                    "Run the independent wrapper and inspect its isolated Unity log.",
                    "Assets/Editor/ParallelQA/GameJamLongStayRedFirstGateRunner.cs")
            };
            WriteReport("gamejam-long-stay-play-contracts", "GameJam long-stay endings RED-first actual Play contracts",
                DateTime.UtcNow, checks);
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
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing GameJam long-stay Play result");
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayExitPassKey);
            SessionState.EraseString(PlayMessageKey);
            Debug.Log("[ParallelQA] " + message);
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
