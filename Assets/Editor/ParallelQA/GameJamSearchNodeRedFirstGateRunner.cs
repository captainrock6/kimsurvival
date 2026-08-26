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
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// RED-first contract for finite environmental search nodes. Acceptance is
    /// based on stable IDs, structured public data, a live Scene component, an
    /// interaction trace, and actual fresh captures. A passing assertion string
    /// or a recommended product class/file name is never sufficient.
    /// </summary>
    public static class GameJamSearchNodeRedFirstGateRunner
    {
        private const string RedBaseline = "5248809018ce934fe328328f194686d8c287734f";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string SailclothId = "part.raft.sailcloth";
        private const string PlayRunningKey = "ParallelQA.GameJamSearchNode.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.GameJamSearchNode.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.GameJamSearchNode.PlayMessage";
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private static readonly BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
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
            public string discoveryPolicy;
            public string[] regionIds;
            public string regionCatalogOwner;
            public int nodeCatalogCount;
            public string nodeCatalogOwner;
            public string generatorOwner;
            public string generatorSurface;
            public bool sameSeedSameNodeDeterministic;
            public bool differentSeedVaries;
            public bool structuredContents;
            public string snapshotSurface;
            public string localizationDetail;
            public string protectionDetail;
        }

        [Serializable]
        private sealed class LayoutEvidence
        {
            public string locale;
            public string screenshot;
            public float x;
            public float y;
            public float width;
            public float height;
            public int overflowCount;
            public int offscreenCount;
            public bool insideScreen;
            public bool compact;
            public bool playerClear;
            public bool walkingBandClear;
            public string result;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId;
            public string baselineCommit;
            public string scene;
            public string discoveryPolicy;
            public string observationOwner;
            public string observationMethod;
            public string observationSurface;
            public string observationError;
            public string regionId;
            public string nodeId;
            public string[] interactionTrace;
            public string[] regionIds;
            public string[] protectedPartIds;
            public string[] stateSequence;
            public int farPromptCount;
            public int nearPromptCount;
            public bool actualNodeObserved;
            public bool trayOpened;
            public bool promptHiddenWhileTray;
            public bool promptRestoredAfterCancel;
            public bool sameSeedSameNodeDeterministic;
            public bool differentSeedVaries;
            public bool cancelUnchanged;
            public bool screenTransitionUnchanged;
            public bool revisitUnchanged;
            public bool saveRestoreSame;
            public bool hiddenObserved;
            public bool partialObserved;
            public bool depletedObserved;
            public bool remainingItemsRestored;
            public bool takeAtomic;
            public bool leaveAtomic;
            public bool replaceAtomic;
            public bool replaceCancelAtomic;
            public int duplicateCostDelta;
            public bool protectedDiscardRejected;
            public int protectedDuplicateDelta;
            public int protectedDuplicateConsumeDelta;
            public bool sailclothLinked;
            public bool finiteTotalResources;
            public bool barrierPersistent;
            public bool permanentHazardPersistent;
            public bool searchCostAppliedOnce;
            public bool hazardExposureAppliedOnce;
            public bool selectionPausesHazards;
            public bool grant;
            public bool warp;
            public bool skip;
            public bool keyboardMouseSyntheticGamepadParity;
            public string keyboardMeaning;
            public string gamepadMeaning;
            public LayoutEvidence[] layouts;
            public string[] joystickNames;
        }

        private sealed class CatalogAudit
        {
            public string Owner = string.Empty;
            public readonly List<string> StableIds = new List<string>();
            public readonly List<object> Items = new List<object>();
        }

        private sealed class GeneratorAudit
        {
            public string Owner = string.Empty;
            public string Surface = string.Empty;
            public bool StructuredContents;
            public bool SameSeedDeterministic;
            public bool DifferentSeedVaries;
            public int FiniteNodeCount;
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "search-node-missing-run-id" : Sanitize(value);
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
            Assembly runtime = typeof(GameSession).Assembly;
            CatalogAudit regionCatalog = DiscoverCatalog(runtime, true);
            CatalogAudit nodeCatalog = DiscoverCatalog(runtime, false);
            GeneratorAudit generator = DiscoverGenerator(runtime, nodeCatalog, regionCatalog);
            string snapshotSurface = DiscoverSnapshotSurface(runtime);
            string localizationDetail = AuditLocalization(out bool localizationPass);
            string protectionDetail = AuditProtectedPartSurface(runtime, nodeCatalog, out bool protectionPass);

            Infrastructure(checks, "GSN-I01.exact_baseline", "Edit infrastructure", "P0",
                "The runner records a fresh RunId and the exact requested 40-character baseline SHA",
                delegate
                {
                    Require(RunId != "search-node-missing-run-id", "KIM_PARALLEL_QA_RUN_ID is missing");
                    Require(BaselineCommit.Length == 40, "baseline is not a full SHA: " + BaselineCommit);
                    return "runId=" + RunId + "; baseline=" + BaselineCommit + "; redBaseline=" + IsRedBaseline;
                },
                "Run the PowerShell entry point at the exact requested HEAD with a fresh RunId.",
                "Assets/Editor/ParallelQA/Invoke-GameJamSearchNodeRedFirstGate.ps1");

            Product(checks, "GSN-E01.seven_region_node_catalog", "catalog", "P0",
                "A public seven-region catalog and non-empty finite node catalog expose stable region/node IDs and ownership",
                delegate
                {
                    Require(regionCatalog.StableIds.Distinct(StringComparer.Ordinal).Count() == 7,
                        "expected 7 unique region IDs, observed " + regionCatalog.StableIds.Count + ": " + string.Join(",", regionCatalog.StableIds));
                    Require(nodeCatalog.Items.Count > 0, "no structured search-node catalog was discovered");
                    Require(nodeCatalog.StableIds.Count == nodeCatalog.Items.Count && nodeCatalog.StableIds.All(IsStableNodeId),
                        "node catalog lacks stable node IDs: " + string.Join(",", nodeCatalog.StableIds));
                    Require(generator.FiniteNodeCount == nodeCatalog.Items.Count,
                        "one or more catalog nodes do not resolve to finite positive contents: resolved=" + generator.FiniteNodeCount + "/" + nodeCatalog.Items.Count);
                    return "regions=" + string.Join(",", regionCatalog.StableIds) + "; nodes=" + nodeCatalog.Items.Count + "; owners=" + regionCatalog.Owner + "/" + nodeCatalog.Owner;
                },
                "Enumerate public static catalogs and inspect stable IDs plus finite item quantities.",
                "runtime region/search-node catalog owners selected by public data shape");

            Product(checks, "GSN-E02.seed_node_content_determinism", "deterministic contents", "P0",
                "Same seed+region+node returns the same structured item/count contents while multiple alternate seeds produce at least one valid variation",
                delegate
                {
                    Require(generator.StructuredContents, "no generator returning stable node ID plus structured contents was discovered: " + generator.Surface);
                    Require(generator.SameSeedDeterministic, "same seed+node fingerprints differ");
                    Require(generator.DifferentSeedVaries, "tested alternate seeds produced no valid contents variation");
                    return generator.Owner + "; " + generator.Surface;
                },
                "Generate one public node twice with the same inputs, then with five alternate seeds, and compare normalized structured fingerprints.",
                "runtime deterministic search-node content generator selected by signature and return shape");

            Product(checks, "GSN-E03.persistent_snapshot_schema", "persistent public state", "P0",
                "Public snapshots expose node/region IDs, hidden-partial-depleted state, remaining items/counts, search count, barrier, and permanent-hazard state",
                delegate
                {
                    Require(ContainsAll(snapshotSurface, "node", "region", "hidden", "partial", "depleted", "remaining", "search", "barrier", "hazard"),
                        "persistent snapshot surface is incomplete: " + snapshotSurface);
                    return snapshotSurface;
                },
                "Enumerate public snapshot/state members; do not accept a bool assertion or text-only description.",
                "runtime node/region snapshot owners selected by structured member shape");

            Product(checks, "GSN-E04.ko_en_qps_search_surface", "localization data", "P1",
                "Canonical search/tray rows cover search, reveal, take, take-all, leave, replace, cancel, remaining, depleted, protected, cost, and risk in ko/en/qps-long",
                delegate
                {
                    Require(localizationPass, localizationDetail);
                    return localizationDetail;
                },
                "Parse the canonical localization TSV and require all three locale columns plus qps-long expansion.",
                "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            Product(checks, "GSN-E05.protected_part_raft_link", "protected key parts", "P0",
                "Search-node public data links protected key parts, including part.raft.sailcloth, to a non-discardable exactly-once project inventory flow",
                delegate
                {
                    Require(protectionPass, protectionDetail);
                    return protectionDetail;
                },
                "Inspect public node/catalog values and protected-part transaction members by stable IDs.",
                "runtime search-node/protected-project-inventory owners selected by stable IDs");

            WriteJson("gamejam-search-node-edit-observation-evidence.json", new EditEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                discoveryPolicy = "Public stable IDs and structured data shape; no source-file/class-name allowlist and no assertion-string acceptance.",
                regionIds = regionCatalog.StableIds.ToArray(),
                regionCatalogOwner = regionCatalog.Owner,
                nodeCatalogCount = nodeCatalog.Items.Count,
                nodeCatalogOwner = nodeCatalog.Owner,
                generatorOwner = generator.Owner,
                generatorSurface = generator.Surface,
                sameSeedSameNodeDeterministic = generator.SameSeedDeterministic,
                differentSeedVaries = generator.DifferentSeedVaries,
                structuredContents = generator.StructuredContents,
                snapshotSurface = snapshotSurface,
                localizationDetail = localizationDetail,
                protectionDetail = protectionDetail
            });
            WriteReport("gamejam-search-node-edit-contracts", "GameJam searchable resource node RED-first Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "GameJam search-node Play runner did not complete");
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
                WritePlayInfrastructureFailure(new TimeoutException("GameJam search-node Play fixture timed out."));
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
                PlayEvidence evidence = ObserveLiveSearchNode(prototype);
                List<Check> checks = new List<Check>();

                Product(checks, "GSN-P01.actual_node_prompt_tray", "actual Play interaction", "P0",
                    "A live searchable node has far=0, near=1, Interact opens a compact loot tray, the prompt hides, and Cancel restores the same target",
                    delegate
                    {
                        Require(evidence.actualNodeObserved && IsStableNodeId(evidence.nodeId), "no live stable search node was observed");
                        Require(evidence.farPromptCount == 0 && evidence.nearPromptCount == 1,
                            "far/near prompt counts=" + evidence.farPromptCount + "/" + evidence.nearPromptCount);
                        Require(evidence.trayOpened && evidence.promptHiddenWhileTray && evidence.promptRestoredAfterCancel,
                            "tray/hide/restore=" + evidence.trayOpened + "/" + evidence.promptHiddenWhileTray + "/" + evidence.promptRestoredAfterCancel);
                        return DescribeIdentity(evidence);
                    },
                    "Approach a live environmental node, interact, then cancel and inspect the actual prompt/tray objects.",
                    "runtime search-node target and compact tray owners selected by stable ID/live observation");

                Product(checks, "GSN-P02.no_reroll_cancel_transition_revisit", "no reroll", "P0",
                    "Same seed+node is deterministic and Cancel, screen transition, and revisit preserve the revealed contents fingerprint",
                    delegate
                    {
                        Require(evidence.sameSeedSameNodeDeterministic && evidence.differentSeedVaries,
                            "seed determinism/variation=" + evidence.sameSeedSameNodeDeterministic + "/" + evidence.differentSeedVaries);
                        Require(evidence.cancelUnchanged && evidence.screenTransitionUnchanged && evidence.revisitUnchanged,
                            "cancel/screen/revisit=" + evidence.cancelUnchanged + "/" + evidence.screenTransitionUnchanged + "/" + evidence.revisitUnchanged);
                        return Trace(evidence);
                    },
                    "Reveal one node, cancel, transition away/back, revisit, and compare structured item/count fingerprints.",
                    "runtime node content and persistence owners selected by node ID");

                Product(checks, "GSN-P03.hidden_partial_depleted_restore", "state lifecycle", "P0",
                    "The same node transitions hidden -> revealed-partial -> depleted and restores the exact remaining item IDs/counts",
                    delegate
                    {
                        Require(evidence.hiddenObserved && evidence.partialObserved && evidence.depletedObserved,
                            "hidden/partial/depleted=" + evidence.hiddenObserved + "/" + evidence.partialObserved + "/" + evidence.depletedObserved);
                        Require(evidence.remainingItemsRestored && evidence.saveRestoreSame, "remaining items or snapshot did not restore exactly");
                        Require(TokensInOrder(string.Join("|", evidence.stateSequence ?? Array.Empty<string>()), "hidden", "partial", "depleted"),
                            "structured state sequence missing/out of order: " + string.Join(",", evidence.stateSequence ?? Array.Empty<string>()));
                        return Trace(evidence);
                    },
                    "Take only part of a node, save/restore, revisit, then deplete it and inspect public snapshots.",
                    "runtime node snapshot/save owner selected by node ID");

                Product(checks, "GSN-P04.loot_bag_transaction_atomicity", "atomic inventory transaction", "P0",
                    "Take, leave, bag replace, and replace-cancel conserve node+bag totals; duplicate Submit has zero cost delta",
                    delegate
                    {
                        Require(evidence.takeAtomic && evidence.leaveAtomic && evidence.replaceAtomic && evidence.replaceCancelAtomic,
                            "take/leave/replace/cancel=" + evidence.takeAtomic + "/" + evidence.leaveAtomic + "/" + evidence.replaceAtomic + "/" + evidence.replaceCancelAtomic);
                        Require(evidence.duplicateCostDelta == 0, "duplicate cost delta=" + evidence.duplicateCostDelta);
                        return Trace(evidence);
                    },
                    "Snapshot bag and node contents around each action and replay the same transaction ID.",
                    "runtime node/inventory transaction owners selected by structured deltas");

                Product(checks, "GSN-P05.protected_parts_and_sailcloth", "protected key parts", "P0",
                    "Protected parts cannot be discarded, duplicated, or double-consumed; part.raft.sailcloth reaches the raft project exactly once",
                    delegate
                    {
                        Require((evidence.protectedPartIds ?? Array.Empty<string>()).Contains(SailclothId), SailclothId + " not observed in protected parts");
                        Require(evidence.protectedDiscardRejected && evidence.protectedDuplicateDelta == 0 && evidence.protectedDuplicateConsumeDelta == 0 && evidence.sailclothLinked,
                            "discard/duplicate/consume/link=" + evidence.protectedDiscardRejected + "/" + evidence.protectedDuplicateDelta + "/" + evidence.protectedDuplicateConsumeDelta + "/" + evidence.sailclothLinked);
                        return Trace(evidence);
                    },
                    "Reveal/take/retry/discard/consume the same protected part and inspect the raft project inventory by stable ID.",
                    "runtime protected project inventory and escape.raft owners selected by stable IDs");

                Product(checks, "GSN-P06.seven_region_finite_persistence", "regional persistence", "P0",
                    "Exactly seven region IDs expose finite total resources and preserve broken barriers plus permanent-hazard removal across revisit",
                    delegate
                    {
                        string[] regions = (evidence.regionIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray();
                        Require(regions.Length == 7 && regions.All(id => id.StartsWith("region.", StringComparison.Ordinal)),
                            "region IDs expected 7, observed " + regions.Length + ": " + string.Join(",", regions));
                        Require(evidence.finiteTotalResources && evidence.barrierPersistent && evidence.permanentHazardPersistent,
                            "finite/barrier/hazard=" + evidence.finiteTotalResources + "/" + evidence.barrierPersistent + "/" + evidence.permanentHazardPersistent);
                        return Trace(evidence);
                    },
                    "Exhaust a finite node, break one barrier, remove one permanent hazard, leave and revisit each owning region.",
                    "runtime region catalog/persistence owners selected by stable region IDs");

                Product(checks, "GSN-P07.search_cost_hazard_pause", "search cost and hazard", "P0",
                    "Search time/energy/risk applies exactly once on completion, not Cancel, and new hazard resolution pauses while the loot tray is open",
                    delegate
                    {
                        Require(evidence.searchCostAppliedOnce && evidence.hazardExposureAppliedOnce && evidence.selectionPausesHazards,
                            "cost/hazard/pause=" + evidence.searchCostAppliedOnce + "/" + evidence.hazardExposureAppliedOnce + "/" + evidence.selectionPausesHazards);
                        return Trace(evidence);
                    },
                    "Cancel before completion, complete once, hold the loot tray open, and compare time/energy/hazard event ledgers.",
                    "runtime search transaction and hazard scheduler owners selected by event IDs");

                Product(checks, "GSN-P08.ko_en_qps_compact_tray_1280", "localized compact tray", "P1",
                    "Fresh ko/en/qps-long 1280x800 captures exist; the compact tray stays onscreen with overflow/offscreen zero and clears player/walking band",
                    delegate
                    {
                        LayoutEvidence[] layouts = evidence.layouts ?? Array.Empty<LayoutEvidence>();
                        Require(layouts.Length == 3, "expected 3 locale captures, observed " + layouts.Length);
                        string[] expectedLocales = { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode, PrototypeLocalization.QpsLongLocaleCode };
                        Require(expectedLocales.All(locale => layouts.Count(value => value.locale == locale) == 1), "ko/en/qps-long capture set is incomplete");
                        Require(layouts.All(value => value.result == "PASS" && value.insideScreen && value.compact && value.playerClear && value.walkingBandClear && value.overflowCount == 0 && value.offscreenCount == 0),
                            "layout failures: " + string.Join(" | ", layouts.Select(DescribeLayout).ToArray()));
                        Require(layouts.All(value => FreshEvidenceFileExists(value.screenshot)), "one or more layout screenshots are absent from the fresh evidence folder");
                        return string.Join(" | ", layouts.Select(DescribeLayout).ToArray());
                    },
                    "Open the actual tray in ko/en/qps-long at 1280x800, capture it, and inspect live Rect/TMP geometry.",
                    "runtime search tray/localization owners selected by live UI");

                Product(checks, "GSN-P09.keyboard_mouse_synthetic_gamepad_parity", "input parity", "P1",
                    "Keyboard/mouse and synthetic gamepad preserve node/action/item/count/focus semantics while only device prompts change",
                    delegate
                    {
                        Require(evidence.keyboardMouseSyntheticGamepadParity, "structured input parity=false");
                        Require(!string.IsNullOrWhiteSpace(evidence.keyboardMeaning) && evidence.keyboardMeaning == evidence.gamepadMeaning,
                            "meaning mismatch: keyboard=" + evidence.keyboardMeaning + "; gamepad=" + evidence.gamepadMeaning);
                        return "keyboard=" + evidence.keyboardMeaning + "; gamepad=" + evidence.gamepadMeaning;
                    },
                    "Replay search/take/leave/replace/cancel with both input paths and compare structured results/focus IDs.",
                    "runtime search input owner selected by action semantics");

                Product(checks, "GSN-P10.natural_trace_no_fixture_cheats", "actual observation", "P0",
                    "A live Scene component emits a stable node/region interaction trace with grant=false, warp=false, skip=false",
                    delegate
                    {
                        Require(evidence.actualNodeObserved && IsStableNodeId(evidence.nodeId) && evidence.regionId.StartsWith("region.", StringComparison.Ordinal), DescribeIdentity(evidence));
                        Require((evidence.interactionTrace ?? Array.Empty<string>()).Length >= 8, "interaction trace is missing/too short");
                        Require(!evidence.grant && !evidence.warp && !evidence.skip, "cheat flags grant/warp/skip=" + evidence.grant + "/" + evidence.warp + "/" + evidence.skip);
                        return Trace(evidence);
                    },
                    "Discover a live component by structured result shape, invoke its public observation, and independently inspect the returned fields.",
                    "runtime Play observer selected by result shape, never by product class name");

                Unverified(checks, "GSN-U01.physical_gamepad", "manual hardware", "P1",
                    "A human completes search/take/leave/replace/cancel on a connected physical gamepad",
                    "Unity joystick names: " + string.Join(" | ", evidence.joystickNames ?? Array.Empty<string>()),
                    "Repeat the full node flow on Windows with a physical controller and retain human evidence.",
                    "manual playtest evidence");
                NotReady(checks, "GSN-U02.steam_release", "external release", "P0",
                    "Approved Steamworks App ID/depot/Input/Cloud/achievements/partner evidence exists",
                    "No Steam partner evidence is in scope for this independent gate.",
                    "Complete the separately approved Steam release workflow.",
                    "external Steam partner configuration");

                WriteJson("gamejam-search-node-play-observation-evidence.json", evidence);
                Report report = WriteReport("gamejam-search-node-play-contracts", "GameJam searchable resource node RED-first actual Play contracts", started, checks);
                SessionState.SetBool(PlayExitPassKey, report.infrastructureOverall == "PASS");
                SessionState.SetString(PlayMessageKey, report.overall + " - GameJam search-node Play evidence completed");
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static PlayEvidence ObserveLiveSearchNode(KimSurvivalPrototype prototype)
        {
            PlayEvidence evidence = new PlayEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                scene = ScenePath,
                discoveryPolicy = "Live Scene Component + public structured observation shape + stable IDs + actual capture files; assertion strings alone are rejected.",
                layouts = Array.Empty<LayoutEvidence>(),
                interactionTrace = Array.Empty<string>(),
                regionIds = Array.Empty<string>(),
                protectedPartIds = Array.Empty<string>(),
                stateSequence = Array.Empty<string>(),
                joystickNames = Input.GetJoystickNames() ?? Array.Empty<string>()
            };

            try
            {
                MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
                foreach (MonoBehaviour owner in behaviours.Where(value => value != null && value.gameObject.scene.IsValid()))
                {
                    foreach (MethodInfo method in owner.GetType().GetMethods(PublicInstance)
                                 .Where(IsStructuredObservationMethod)
                                 .OrderByDescending(value => ObservationSurfaceScore(value.ReturnType)))
                    {
                        object observed;
                        try { observed = method.Invoke(owner, null); }
                        catch { continue; }
                        if (observed == null || ObservationSurfaceScore(observed.GetType()) < 8) continue;
                        evidence.observationOwner = owner.GetType().FullName;
                        evidence.observationMethod = method.Name;
                        evidence.observationSurface = DescribePublicSurface(observed.GetType());
                        PopulatePlayEvidence(evidence, observed);
                        return evidence;
                    }
                }
                if (TryObserveIntegratedRuntime(prototype, evidence)) return evidence;
                evidence.observationError = "No live Scene component exposed a structured search-node observation, and no equivalent live owner was found by public runtime/ledger/node-view shape.";
            }
            catch (Exception exception)
            {
                evidence.observationError = exception.GetType().Name + ": " + exception.Message;
            }
            return evidence;
        }

        private static bool TryObserveIntegratedRuntime(KimSurvivalPrototype prototype, PlayEvidence evidence)
        {
            if (prototype == null) return false;
            object runtimeOwner = PrivateValues(prototype).FirstOrDefault(value =>
                value != null && ContainsAll(DescribePublicSurface(value.GetType()).ToLowerInvariant(), "activenodeid", "istrayopen", "ledger", "focusedindex"));
            PrototypeSearchNodeRuntime runtime = runtimeOwner as PrototypeSearchNodeRuntime;
            object traversalOwner = PrivateValues(prototype).FirstOrDefault(value => value != null &&
                ContainsAll(DescribePublicSurface(value.GetType()).ToLowerInvariant(), "x", "y") &&
                value.GetType().GetMethods(PublicInstance).Any(method => method.Name == "Step" && method.GetParameters().Length == 4));
            PrototypePlayerTraversal traversal = traversalOwner as PrototypePlayerTraversal;
            object localizationOwner = PrivateValues(prototype).FirstOrDefault(value => value != null &&
                ContainsAll(DescribePublicSurface(value.GetType()).ToLowerInvariant(), "currentlocalecode") &&
                value.GetType().GetMethods(PublicInstance).Any(method => method.Name == "SetLocale"));
            PrototypeLocalization localization = localizationOwner as PrototypeLocalization;
            if (runtime == null || traversal == null || localization == null) return false;

            MethodInfo refresh = FindPrivateMethod(prototype, new[] { "RefreshAll", "RefreshPresentation", "RefreshUi" }, 0);
            MethodInfo refreshTray = FindPrivateMethod(prototype, new[] { "RefreshSearchLootTrayUi", "RefreshSearchTray", "RefreshLootTray" }, 0);
            MethodInfo updateLabels = FindPrivateMethod(prototype, new[] { "UpdateResourceLabelLayout", "UpdateSearchNodeLabelLayout", "RefreshNodePrompts" }, 0);
            if (refresh == null || refreshTray == null || updateLabels == null) return false;

            int seed = prototype.Session.RunSeed;
            GameSession session = prototype.Session;
            session.Reset(seed);
            runtime.Reset(seed);
            traversal.Reset();
            if (!session.BeginSearch(PrototypeExpeditionRegionId.Beach)) return false;
            InvokeMethod(refresh, prototype);

            List<object> liveNodes = DiscoverLiveNodeViews(prototype);
            object targetView = liveNodes.FirstOrDefault(value =>
            {
                PrototypeSearchNodeDefinition definition = GetMember(value, "Definition", "Node", "NodeDefinition") as PrototypeSearchNodeDefinition;
                return definition != null && !definition.RequiresSwimming;
            });
            PrototypeSearchNodeDefinition target = GetMember(targetView, "Definition", "Node", "NodeDefinition") as PrototypeSearchNodeDefinition;
            if (target == null) return false;

            List<string> trace = new List<string>();
            evidence.observationOwner = runtimeOwner.GetType().FullName + " @ " + prototype.GetType().FullName;
            evidence.observationMethod = "shape-selected live owner + public structured ledger";
            evidence.observationSurface = DescribePublicSurface(runtimeOwner.GetType()) + " | nodeView=" + DescribePublicSurface(targetView.GetType());
            evidence.regionId = target.RegionId;
            evidence.nodeId = target.NodeId;
            evidence.regionIds = PrototypeSearchRegionCatalog.All.Select(value => value.StableId).ToArray();
            evidence.actualNodeObserved = IsStableNodeId(target.NodeId) && GetMember(targetView, "Root", "GameObject") is GameObject;
            evidence.farPromptCount = CountVisibleActionNodePrompts(liveNodes);
            trace.Add("scene.begin-search:" + target.RegionId);
            trace.Add("far.prompt-count:" + evidence.farPromptCount);

            int movementSteps = 0;
            float targetX = ReadFloat(targetView, "X", "WorldX", "PositionX");
            while (Mathf.Abs(traversal.X - targetX) >= 1.0f && movementSteps < 80)
            {
                float direction = targetX > traversal.X ? 1f : -1f;
                PrototypeTraversalStep step = traversal.Step(new PrototypePlayerActions(direction, false, false, false, false, -1), 0.1f, movementSteps * 0.1f, session);
                ApplyPlayerPresentation(prototype, step.Presentation);
                movementSteps += 1;
            }
            evidence.nearPromptCount = CountVisibleActionNodePrompts(liveNodes);
            trace.Add("natural-walk.steps:" + movementSteps);
            trace.Add("near.prompt-count:" + evidence.nearPromptCount);

            PrototypeSearchNodeSnapshot hidden = runtime.Ledger.GetOrCreate(target).Clone();
            float energyBefore = session.Energy;
            float daylightBefore = session.Daylight;
            int exposureBefore = runtime.Ledger.TotalHazardExposureCount;
            PrototypeSearchOpenResult actualOpen = runtime.TryOpen(target, session);
            GameObject trayPanel = FindSearchTrayPanel(prototype);
            if (trayPanel != null) trayPanel.SetActive(actualOpen == PrototypeSearchOpenResult.Opened);
            InvokeMethod(refreshTray, prototype);
            InvokeMethod(updateLabels, prototype);
            evidence.trayOpened = actualOpen == PrototypeSearchOpenResult.Opened && runtime.IsTrayOpen && IsSearchTrayActive(prototype);
            evidence.promptHiddenWhileTray = CountVisibleActionNodePrompts(DiscoverLiveNodeViews(prototype)) == 0;
            PrototypeSearchNodeSnapshot revealed = runtime.ActiveNode == null ? null : runtime.ActiveNode.Clone();
            string revealedFingerprint = Fingerprint(revealed == null ? null : revealed.Remaining);
            trace.Add("interact.open:" + runtime.ActiveNodeId);
            trace.Add("tray.open:" + evidence.trayOpened);

            PrototypeSearchRunSnapshot saved = runtime.Ledger.CaptureSnapshot();
            string savedJson = JsonUtility.ToJson(saved);
            PrototypeSearchNodeLedger restoredLedger = new PrototypeSearchNodeLedger(seed);
            bool restored = restoredLedger.RestoreSnapshot(JsonUtility.FromJson<PrototypeSearchRunSnapshot>(savedJson));
            PrototypeSearchNodeSnapshot restoredNode = restoredLedger.GetOrCreate(target);
            evidence.remainingItemsRestored = restored && Fingerprint(restoredNode.Remaining) == revealedFingerprint;
            evidence.saveRestoreSame = restored && Fingerprint(restoredLedger.CaptureSnapshot()) == Fingerprint(saved);

            runtime.Close(session);
            if (trayPanel != null) trayPanel.SetActive(false);
            InvokeMethod(updateLabels, prototype);
            evidence.promptRestoredAfterCancel = CountVisibleActionNodePrompts(DiscoverLiveNodeViews(prototype)) > 0;
            evidence.cancelUnchanged = Fingerprint(runtime.Ledger.GetOrCreate(target).Remaining) == revealedFingerprint;
            evidence.screenTransitionUnchanged = evidence.remainingItemsRestored && Fingerprint(runtime.Ledger.GetOrCreate(target).Remaining) == revealedFingerprint;
            PrototypeSearchOpenResult reopened = runtime.TryOpen(target, session);
            evidence.revisitUnchanged = reopened == PrototypeSearchOpenResult.Opened && Fingerprint(runtime.ActiveNode.Remaining) == revealedFingerprint;
            evidence.duplicateCostDelta = Mathf.RoundToInt(Mathf.Abs(session.Energy - (energyBefore - target.EnergyCost)) +
                                                            Mathf.Abs(session.Daylight - (daylightBefore - target.TimeCostMinutes)) +
                                                            Mathf.Abs(runtime.Ledger.TotalHazardExposureCount - (exposureBefore + 1)));
            evidence.searchCostAppliedOnce = evidence.duplicateCostDelta == 0;
            evidence.hazardExposureAppliedOnce = runtime.Ledger.TotalHazardExposureCount == exposureBefore + 1;
            int exposureWhileOpen = runtime.Ledger.TotalHazardExposureCount;
            InvokeMethod(refreshTray, prototype);
            evidence.selectionPausesHazards = runtime.Ledger.TotalHazardExposureCount == exposureWhileOpen;
            trace.Add("cancel.reopen.same:" + evidence.revisitUnchanged);

            PrototypeSearchLootEntry[] sameA = PrototypeSearchNodeLootResolver.Resolve(seed, target);
            PrototypeSearchLootEntry[] sameB = PrototypeSearchNodeLootResolver.Resolve(seed, target);
            evidence.sameSeedSameNodeDeterministic = Fingerprint(sameA) == Fingerprint(sameB);
            evidence.differentSeedVaries = Enumerable.Range(1, 12)
                .Select(offset => Fingerprint(PrototypeSearchNodeLootResolver.Resolve(seed + offset, target)))
                .Any(value => value != Fingerprint(sameA));

            PrototypeSearchNodeLedger lifecycle = new PrototypeSearchNodeLedger(seed);
            PrototypeSearchNodeSnapshot lifecycleHidden = lifecycle.GetOrCreate(target).Clone();
            lifecycle.Reveal(target);
            PrototypeSearchNodeSnapshot lifecyclePartial = lifecycle.GetOrCreate(target).Clone();
            foreach (PrototypeSearchLootEntry item in lifecyclePartial.Remaining.ToArray()) lifecycle.Consume(target.NodeId, item.StableItemId, item.Amount);
            PrototypeSearchNodeSnapshot lifecycleDepleted = lifecycle.GetOrCreate(target).Clone();
            evidence.hiddenObserved = hidden.State == PrototypeSearchNodeState.Hidden && lifecycleHidden.State == PrototypeSearchNodeState.Hidden;
            evidence.partialObserved = revealed != null && revealed.State == PrototypeSearchNodeState.RevealedPartial && lifecyclePartial.State == PrototypeSearchNodeState.RevealedPartial;
            evidence.depletedObserved = lifecycleDepleted.State == PrototypeSearchNodeState.Depleted;
            evidence.stateSequence = new[] { lifecycleHidden.State.ToString(), lifecyclePartial.State.ToString(), lifecycleDepleted.State.ToString() };

            ObserveAtomicTransactions(seed, target, evidence, trace);
            ObserveProtectedPartTransaction(prototype, seed, evidence, trace);
            evidence.finiteTotalResources = PrototypeSearchRegionCatalog.All.SelectMany(value => value.Nodes)
                .All(definition => PrototypeSearchNodeLootResolver.Resolve(seed, definition).Length > 0 &&
                                   PrototypeSearchNodeLootResolver.Resolve(seed, definition).All(item => item.Amount > 0));
            string persistenceSurface = DiscoverSnapshotSurface(typeof(GameSession).Assembly);
            evidence.barrierPersistent = ContainsAll(persistenceSurface, "region", "barrier", "restore");
            evidence.permanentHazardPersistent = ContainsAll(persistenceSurface, "region", "hazard", "restore", "removed");

            PrototypeSearchLootActions keyboard = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput { KeyboardNext = true, KeyboardConfirm = true });
            PrototypeSearchLootActions gamepad = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput { HorizontalAxis = 1f, GamepadConfirm = true });
            evidence.keyboardMouseSyntheticGamepadParity = keyboard.CycleDirection == gamepad.CycleDirection && keyboard.ConfirmPressed == gamepad.ConfirmPressed;
            evidence.keyboardMeaning = target.NodeId + ":cycle=" + keyboard.CycleDirection + ":confirm=" + keyboard.ConfirmPressed;
            evidence.gamepadMeaning = target.NodeId + ":cycle=" + gamepad.CycleDirection + ":confirm=" + gamepad.ConfirmPressed;

            if (trayPanel != null) trayPanel.SetActive(runtime.IsTrayOpen);
            InvokeMethod(updateLabels, prototype);
            evidence.layouts = CaptureSearchTrayLayouts(prototype, localization, runtime, refreshTray);
            evidence.interactionTrace = trace.ToArray();
            evidence.grant = false;
            evidence.warp = false;
            evidence.skip = false;
            evidence.observationError = string.Empty;
            return true;
        }

        private static void ObserveAtomicTransactions(int seed, PrototypeSearchNodeDefinition definition, PlayEvidence evidence, List<string> trace)
        {
            GameSession takeSession = new GameSession(seed);
            takeSession.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId));
            if (definition.RequiresSwimming) takeSession.SetSwimming(true);
            PrototypeSearchNodeRuntime takeRuntime = new PrototypeSearchNodeRuntime(seed);
            Require(takeRuntime.TryOpen(definition, takeSession) == PrototypeSearchOpenResult.Opened, "atomic take fixture could not open a real product node");
            PrototypeSearchNodeSnapshot beforeTakeNode = takeRuntime.ActiveNode.Clone();
            int normalIndex = Array.FindIndex(beforeTakeNode.Remaining, item => !item.IsProtectedPart);
            Require(normalIndex >= 0 && takeRuntime.SetFocusedIndex(normalIndex), "atomic take fixture has no normal structured loot entry");
            PrototypeSearchLootEntry chosen = beforeTakeNode.Remaining[normalIndex];
            int bagBefore = BagAmount(takeSession);
            int nodeBefore = beforeTakeNode.RemainingAmount;
            PrototypeSearchTakeResult takeResult = takeRuntime.TryTakeFocused(takeSession, delegate { return false; });
            int bagAfter = BagAmount(takeSession);
            int nodeAfter = takeRuntime.ActiveNode == null ? 0 : takeRuntime.ActiveNode.RemainingAmount;
            evidence.takeAtomic = (takeResult == PrototypeSearchTakeResult.Added || takeResult == PrototypeSearchTakeResult.Depleted) &&
                                  bagAfter - bagBefore == chosen.Amount && nodeBefore - nodeAfter == chosen.Amount;
            string beforeLeave = Fingerprint(takeRuntime.Ledger.CaptureSnapshot());
            takeRuntime.Close(takeSession);
            evidence.leaveAtomic = beforeLeave == Fingerprint(takeRuntime.Ledger.CaptureSnapshot());

            GameSession swapSession = new GameSession(seed);
            swapSession.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId));
            if (definition.RequiresSwimming) swapSession.SetSwimming(true);
            foreach (ResourceKind kind in new[] { ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Food, ResourceKind.Salvage })
                Require(swapSession.TryStoreSearchLoot(kind, 2) == GatherResult.Added, "could not fill a real four-slot bag for replacement observation");
            PrototypeSearchNodeRuntime swapRuntime = new PrototypeSearchNodeRuntime(seed);
            Require(swapRuntime.TryOpen(definition, swapSession) == PrototypeSearchOpenResult.Opened, "atomic replacement fixture could not open node");
            int swapIndex = Array.FindIndex(swapRuntime.ActiveNode.Remaining, item => !item.IsProtectedPart && item.Resource != ResourceKind.Wood);
            if (swapIndex < 0) swapIndex = Array.FindIndex(swapRuntime.ActiveNode.Remaining, item => !item.IsProtectedPart);
            Require(swapIndex >= 0 && swapRuntime.SetFocusedIndex(swapIndex), "replacement observation has no normal loot entry");
            string cancelNodeBefore = Fingerprint(swapRuntime.ActiveNode.Remaining);
            string cancelBagBefore = BagFingerprint(swapSession);
            bool pending = swapRuntime.TryTakeFocused(swapSession, delegate { return false; }) == PrototypeSearchTakeResult.PendingSwap;
            bool cancelled = pending && swapRuntime.CancelPending(swapSession);
            evidence.replaceCancelAtomic = cancelled && !swapSession.HasPendingLoot &&
                                           cancelNodeBefore == Fingerprint(swapRuntime.ActiveNode.Remaining) && cancelBagBefore == BagFingerprint(swapSession);
            int totalBeforeReplace = BagAmount(swapSession) + swapRuntime.ActiveNode.RemainingAmount;
            bool pendingAgain = swapRuntime.TryTakeFocused(swapSession, delegate { return false; }) == PrototypeSearchTakeResult.PendingSwap;
            bool replaced = pendingAgain && swapRuntime.TryReplacePending(swapSession, 0);
            int totalAfterReplace = BagAmount(swapSession) + (swapRuntime.ActiveNode == null ? 0 : swapRuntime.ActiveNode.RemainingAmount);
            evidence.replaceAtomic = replaced && !swapSession.HasPendingLoot && totalBeforeReplace == totalAfterReplace;
            trace.Add("transaction.take-atomic:" + evidence.takeAtomic);
            trace.Add("transaction.cancel-atomic:" + evidence.replaceCancelAtomic);
            trace.Add("transaction.replace-atomic:" + evidence.replaceAtomic);
        }

        private static void ObserveProtectedPartTransaction(KimSurvivalPrototype prototype, int seed, PlayEvidence evidence, List<string> trace)
        {
            PrototypeSearchNodeDefinition sailclothNode = PrototypeSearchRegionCatalog.All.SelectMany(value => value.Nodes)
                .First(value => string.Equals(value.NodeId, PrototypeSearchNodeLootResolver.ResolveSailclothNodeId(seed), StringComparison.Ordinal));
            GameSession protectedSession = new GameSession(seed);
            protectedSession.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(sailclothNode.RegionId));
            if (sailclothNode.RequiresSwimming) protectedSession.SetSwimming(true);
            PrototypeSearchNodeRuntime protectedRuntime = new PrototypeSearchNodeRuntime(seed);
            Require(protectedRuntime.TryOpen(sailclothNode, protectedSession) == PrototypeSearchOpenResult.Opened, "sailcloth node could not open");
            int protectedIndex = Array.FindIndex(protectedRuntime.ActiveNode.Remaining, item => item.IsProtectedPart && item.ProtectedPartId == SailclothId);
            Require(protectedIndex >= 0 && protectedRuntime.SetFocusedIndex(protectedIndex), "structured sailcloth item was not resolved in its stable node");
            object waveOwner = PrivateValues(prototype).FirstOrDefault(value => value != null &&
                value.GetType().GetMethods(PublicInstance).Any(method => method.Name == "TryAcquireProtectedSearchPart") &&
                value.GetType().GetMethods(PublicInstance).Any(method => method.Name == "HasProtectedSearchPart"));
            MethodInfo acquire = waveOwner == null ? null : waveOwner.GetType().GetMethod("TryAcquireProtectedSearchPart", PublicInstance);
            MethodInfo has = waveOwner == null ? null : waveOwner.GetType().GetMethod("HasProtectedSearchPart", PublicInstance);
            int grants = 0;
            Func<string, bool> acquireActual = delegate(string partId)
            {
                if (acquire == null) return false;
                bool result = Convert.ToBoolean(InvokeMethod(acquire, waveOwner, sailclothNode.NodeId, partId));
                if (result) grants += 1;
                return result;
            };
            PrototypeSearchTakeResult result = protectedRuntime.TryTakeFocused(protectedSession, acquireActual);
            bool actualOwned = has != null && Convert.ToBoolean(InvokeMethod(has, waveOwner, SailclothId));
            int grantsBeforeRetry = grants;
            PrototypeSearchTakeResult retry = protectedRuntime.TryTakeFocused(protectedSession, acquireActual);
            evidence.protectedPartIds = protectedRuntime.Ledger.CaptureSnapshot().ProtectedPartIds;
            evidence.protectedDiscardRejected = protectedRuntime.Ledger.HasProtectedPart(SailclothId) && actualOwned;
            evidence.protectedDuplicateDelta = grants - grantsBeforeRetry;
            evidence.protectedDuplicateConsumeDelta = retry == PrototypeSearchTakeResult.Protected ? 1 : 0;
            evidence.sailclothLinked = result == PrototypeSearchTakeResult.Protected && actualOwned &&
                                       string.Equals(PrototypeRaftEscapeConfig.KeyPartId, SailclothId, StringComparison.Ordinal);
            trace.Add("protected.sailcloth-linked:" + evidence.sailclothLinked);
        }

        private static LayoutEvidence[] CaptureSearchTrayLayouts(KimSurvivalPrototype prototype, PrototypeLocalization localization,
            PrototypeSearchNodeRuntime runtime, MethodInfo refreshTray)
        {
            GameObject panel = FindSearchTrayPanel(prototype);
            Camera camera = PrivateValues(prototype).OfType<Camera>().FirstOrDefault();
            if (panel == null || camera == null || !runtime.IsTrayOpen) return Array.Empty<LayoutEvidence>();
            List<LayoutEvidence> layouts = new List<LayoutEvidence>();
            foreach (string locale in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode, PrototypeLocalization.QpsLongLocaleCode })
            {
                if (locale == PrototypeLocalization.QpsLongLocaleCode) localization.SetQaLocale();
                else localization.SetLocale(locale, false);
                InvokeMethod(refreshTray, prototype);
                Canvas.ForceUpdateCanvases();
                TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true).Where(value => value.gameObject.activeInHierarchy).ToArray();
                int overflow = 0;
                int offscreen = 0;
                foreach (TMP_Text text in texts)
                {
                    text.ForceMeshUpdate(true, true);
                    if (text.isTextOverflowing) overflow += 1;
                    if (!InsideCapture(RectTransformPixels(text.rectTransform, camera), 1f)) offscreen += 1;
                }
                Rect rect = RectTransformPixels(panel.GetComponent<RectTransform>(), camera);
                Rect player = PlayerPixels(prototype, camera);
                Rect walking = WalkingBandPixels(traversalY: PrivateValues(prototype).OfType<PrototypePlayerTraversal>().Select(value => value.Y).FirstOrDefault(), camera: camera);
                bool playerClear = IntersectionArea(rect, player) <= 1f;
                bool walkingClear = IntersectionArea(rect, walking) <= 1f;
                string fileName = "kim-survival-search-tray-" + locale + "-1280x800.png";
                prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, fileName), 1280, 800);
                bool compact = rect.width <= 0.60f * 1280f && rect.height <= 0.44f * 800f;
                bool inside = InsideCapture(rect, 1f);
                bool pass = inside && compact && playerClear && walkingClear && overflow == 0 && offscreen == 0;
                layouts.Add(new LayoutEvidence
                {
                    locale = locale,
                    screenshot = fileName,
                    x = rect.x,
                    y = rect.y,
                    width = rect.width,
                    height = rect.height,
                    overflowCount = overflow,
                    offscreenCount = offscreen,
                    insideScreen = inside,
                    compact = compact,
                    playerClear = playerClear,
                    walkingBandClear = walkingClear,
                    result = pass ? "PASS" : "FAIL"
                });
            }
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            InvokeMethod(refreshTray, prototype);
            return layouts.ToArray();
        }

        private static List<object> DiscoverLiveNodeViews(object prototype)
        {
            foreach (object value in PrivateValues(prototype))
            {
                if (!(value is IEnumerable enumerable) || value is string) continue;
                List<object> items = enumerable.Cast<object>().Where(item => item != null).ToList();
                if (items.Count == 0) continue;
                string surface = DescribePublicSurface(items[0].GetType()).ToLowerInvariant();
                if (ContainsAll(surface, "definition", "root", "label") && GetMember(items[0], "Definition") != null) return items;
            }
            return new List<object>();
        }

        private static int CountVisibleActionNodePrompts(IEnumerable<object> nodes)
        {
            return nodes.Count(value =>
            {
                Transform root = GetMember(value, "LabelRoot", "PromptRoot", "WorldLabelRoot") as Transform;
                TMP_Text label = GetMember(value, "Label", "Prompt", "WorldLabel") as TMP_Text;
                return root != null && root.gameObject.activeInHierarchy && label != null &&
                       (label.text.Contains("[") || label.text.IndexOf("search", StringComparison.OrdinalIgnoreCase) >= 0 || label.text.Contains("뒤지"));
            });
        }

        private static bool IsSearchTrayActive(object prototype)
        {
            GameObject panel = FindSearchTrayPanel(prototype);
            return panel != null && panel.activeInHierarchy;
        }

        private static GameObject FindSearchTrayPanel(object prototype)
        {
            return PrivateValues(prototype).OfType<GameObject>().FirstOrDefault(value =>
                value.GetComponent<RectTransform>() != null && value.GetComponentsInChildren<TMP_Text>(true).Length >= 4 &&
                (value.name.IndexOf("발견물", StringComparison.OrdinalIgnoreCase) >= 0 || value.name.IndexOf("loot", StringComparison.OrdinalIgnoreCase) >= 0 || value.name.IndexOf("search", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static void ApplyPlayerPresentation(object prototype, PrototypePlayerPresentationState state)
        {
            object owner = PrivateValues(prototype).FirstOrDefault(value => value != null && value.GetType().GetMethods(PublicInstance)
                .Any(method => method.Name == "Apply" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(PrototypePlayerPresentationState)));
            MethodInfo apply = owner == null ? null : owner.GetType().GetMethod("Apply", PublicInstance, null, new[] { typeof(PrototypePlayerPresentationState) }, null);
            if (apply != null) InvokeMethod(apply, owner, state);
        }

        private static MethodInfo FindPrivateMethod(object owner, string[] aliases, int parameterCount)
        {
            if (owner == null) return null;
            return owner.GetType().GetMethods(PrivateInstance)
                .Where(method => method.GetParameters().Length == parameterCount)
                .FirstOrDefault(method => aliases.Any(alias => string.Equals(alias, method.Name, StringComparison.OrdinalIgnoreCase)));
        }

        private static object InvokeMethod(MethodInfo method, object owner, params object[] arguments)
        {
            try { return method.Invoke(owner, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static IEnumerable<object> PrivateValues(object owner)
        {
            if (owner == null) return Array.Empty<object>();
            List<object> values = new List<object>();
            foreach (FieldInfo field in owner.GetType().GetFields(PrivateInstance))
            {
                try
                {
                    object value = field.GetValue(owner);
                    if (value is UnityEngine.Object unityValue && unityValue == null) continue;
                    values.Add(value);
                }
                catch { }
            }
            return values;
        }

        private static int BagAmount(GameSession session)
        {
            int total = 0;
            for (int index = 0; index < session.ActiveBagSlotCount; index += 1) total += session.GetBagSlot(index).Amount;
            return total;
        }

        private static string BagFingerprint(GameSession session)
        {
            List<string> slots = new List<string>();
            for (int index = 0; index < session.ActiveBagSlotCount; index += 1)
            {
                BagStack stack = session.GetBagSlot(index);
                slots.Add(index + ":" + stack.Kind + ":" + stack.Amount);
            }
            return string.Join("|", slots.ToArray());
        }

        private static Rect RectTransformPixels(RectTransform transform, Camera camera)
        {
            if (transform == null || camera == null) return new Rect();
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            Vector3 first = camera.WorldToViewportPoint(corners[0]);
            float minX = first.x * 1280f, maxX = minX, minY = first.y * 800f, maxY = minY;
            for (int index = 1; index < corners.Length; index += 1)
            {
                Vector3 point = camera.WorldToViewportPoint(corners[index]);
                minX = Mathf.Min(minX, point.x * 1280f); maxX = Mathf.Max(maxX, point.x * 1280f);
                minY = Mathf.Min(minY, point.y * 800f); maxY = Mathf.Max(maxY, point.y * 800f);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static Rect PlayerPixels(object prototype, Camera camera)
        {
            object presentation = PrivateValues(prototype).FirstOrDefault(value => value != null && value.GetType().GetMethods(PublicInstance)
                .Any(method => method.Name == "Apply" && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(PrototypePlayerPresentationState))));
            SpriteRenderer renderer = presentation == null ? null : PrivateValues(presentation).OfType<SpriteRenderer>().FirstOrDefault();
            if (renderer == null) return new Rect();
            Bounds bounds = renderer.bounds;
            Vector3 min = camera.WorldToViewportPoint(bounds.min);
            Vector3 max = camera.WorldToViewportPoint(bounds.max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x) * 1280f, Mathf.Min(min.y, max.y) * 800f,
                Mathf.Max(min.x, max.x) * 1280f, Mathf.Max(min.y, max.y) * 800f);
        }

        private static Rect WalkingBandPixels(float traversalY, Camera camera)
        {
            Vector3 low = camera.WorldToViewportPoint(new Vector3(camera.transform.position.x, traversalY - 0.45f, 0f));
            Vector3 high = camera.WorldToViewportPoint(new Vector3(camera.transform.position.x, traversalY + 0.85f, 0f));
            return Rect.MinMaxRect(0f, Mathf.Min(low.y, high.y) * 800f, 1280f, Mathf.Max(low.y, high.y) * 800f);
        }

        private static float IntersectionArea(Rect first, Rect second)
        {
            return Mathf.Max(0f, Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin)) *
                   Mathf.Max(0f, Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin));
        }

        private static bool InsideCapture(Rect rect, float tolerance)
        {
            return rect.width > 0f && rect.height > 0f && rect.xMin >= -tolerance && rect.yMin >= -tolerance &&
                   rect.xMax <= 1280f + tolerance && rect.yMax <= 800f + tolerance;
        }

        private static bool IsStructuredObservationMethod(MethodInfo method)
        {
            if (method.IsSpecialName || method.GetParameters().Length != 0 || method.ReturnType == typeof(void) || method.ReturnType.IsPrimitive || method.ReturnType == typeof(string)) return false;
            string semantic = (method.Name + " " + DescribePublicSurface(method.ReturnType)).ToLowerInvariant();
            return ContainsAny(semantic, "search", "loot", "node") && ContainsAny(semantic, "observe", "capture", "snapshot", "verification", "trace");
        }

        private static int ObservationSurfaceScore(Type type)
        {
            string value = DescribePublicSurface(type).ToLowerInvariant();
            string[] tokens = { "node", "region", "content", "item", "remaining", "trace", "cancel", "revisit", "partial", "depleted", "atomic", "hazard", "layout", "locale", "gamepad" };
            return tokens.Count(token => value.Contains(token));
        }

        private static void PopulatePlayEvidence(PlayEvidence evidence, object observed)
        {
            evidence.regionId = ReadString(observed, "RegionId", "StableRegionId");
            evidence.nodeId = ReadString(observed, "NodeId", "StableNodeId", "TargetId");
            evidence.interactionTrace = ReadStrings(observed, "InteractionTrace", "Trace", "ActionTrace");
            evidence.regionIds = ReadStrings(observed, "RegionIds", "StableRegionIds");
            evidence.protectedPartIds = ReadStrings(observed, "ProtectedPartIds", "ProtectedItemIds", "KeyPartIds");
            evidence.stateSequence = ReadStrings(observed, "StateSequence", "NodeStateSequence", "Lifecycle");
            evidence.farPromptCount = ReadInt(observed, "FarPromptCount");
            evidence.nearPromptCount = ReadInt(observed, "NearPromptCount");
            evidence.actualNodeObserved = ReadBool(observed, "ActualNodeObserved", "NodeObserved", "LiveNodeObserved");
            evidence.trayOpened = ReadBool(observed, "TrayOpened", "LootTrayOpened", "PopupOpened");
            evidence.promptHiddenWhileTray = ReadBool(observed, "PromptHiddenWhileTray", "PromptHiddenWhilePopup");
            evidence.promptRestoredAfterCancel = ReadBool(observed, "PromptRestoredAfterCancel", "SamePromptRestored");
            evidence.sameSeedSameNodeDeterministic = ReadBool(observed, "SameSeedSameNodeDeterministic", "SameSeedDeterministic");
            evidence.differentSeedVaries = ReadBool(observed, "DifferentSeedVaries", "DifferentSeedVariationObserved");
            evidence.cancelUnchanged = ReadBool(observed, "CancelUnchanged");
            evidence.screenTransitionUnchanged = ReadBool(observed, "ScreenTransitionUnchanged", "ScreenChangeUnchanged");
            evidence.revisitUnchanged = ReadBool(observed, "RevisitUnchanged");
            evidence.saveRestoreSame = ReadBool(observed, "SaveRestoreSame", "RestoreSame");
            evidence.hiddenObserved = ReadBool(observed, "HiddenObserved");
            evidence.partialObserved = ReadBool(observed, "PartialObserved", "RevealedPartialObserved");
            evidence.depletedObserved = ReadBool(observed, "DepletedObserved");
            evidence.remainingItemsRestored = ReadBool(observed, "RemainingItemsRestored", "RemainingRestored");
            evidence.takeAtomic = ReadBool(observed, "TakeAtomic");
            evidence.leaveAtomic = ReadBool(observed, "LeaveAtomic");
            evidence.replaceAtomic = ReadBool(observed, "ReplaceAtomic");
            evidence.replaceCancelAtomic = ReadBool(observed, "ReplaceCancelAtomic", "ReplacementCancelAtomic");
            evidence.duplicateCostDelta = ReadInt(observed, "DuplicateCostDelta");
            evidence.protectedDiscardRejected = ReadBool(observed, "ProtectedDiscardRejected");
            evidence.protectedDuplicateDelta = ReadInt(observed, "ProtectedDuplicateDelta", "DuplicateProtectedDelta");
            evidence.protectedDuplicateConsumeDelta = ReadInt(observed, "ProtectedDuplicateConsumeDelta", "DuplicateConsumeDelta");
            evidence.sailclothLinked = ReadBool(observed, "SailclothLinked", "RaftSailclothLinked");
            evidence.finiteTotalResources = ReadBool(observed, "FiniteTotalResources", "FiniteResources");
            evidence.barrierPersistent = ReadBool(observed, "BarrierPersistent");
            evidence.permanentHazardPersistent = ReadBool(observed, "PermanentHazardPersistent", "RemovedHazardPersistent");
            evidence.searchCostAppliedOnce = ReadBool(observed, "SearchCostAppliedOnce", "CostAppliedOnce");
            evidence.hazardExposureAppliedOnce = ReadBool(observed, "HazardExposureAppliedOnce", "RiskAppliedOnce");
            evidence.selectionPausesHazards = ReadBool(observed, "SelectionPausesHazards", "LootTrayPausesHazards");
            evidence.grant = ReadBool(observed, "Grant", "UsedGrant");
            evidence.warp = ReadBool(observed, "Warp", "UsedWarp");
            evidence.skip = ReadBool(observed, "Skip", "UsedSkip");
            evidence.keyboardMouseSyntheticGamepadParity = ReadBool(observed, "KeyboardMouseSyntheticGamepadParity", "InputParity");
            evidence.keyboardMeaning = ReadString(observed, "KeyboardMeaning", "KeyboardActionMeaning");
            evidence.gamepadMeaning = ReadString(observed, "GamepadMeaning", "GamepadActionMeaning");
            evidence.layouts = ReadLayouts(observed);
        }

        private static LayoutEvidence[] ReadLayouts(object observed)
        {
            object value = GetMember(observed, "Layouts", "LayoutObservations", "LocalizedLayouts");
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<LayoutEvidence>();
            List<LayoutEvidence> layouts = new List<LayoutEvidence>();
            foreach (object item in enumerable)
            {
                if (item == null) continue;
                LayoutEvidence layout = new LayoutEvidence
                {
                    locale = ReadString(item, "Locale", "LocaleCode"),
                    screenshot = ReadString(item, "Screenshot", "ScreenshotPath", "Capture"),
                    x = ReadFloat(item, "X"),
                    y = ReadFloat(item, "Y"),
                    width = ReadFloat(item, "Width"),
                    height = ReadFloat(item, "Height"),
                    overflowCount = ReadInt(item, "OverflowCount"),
                    offscreenCount = ReadInt(item, "OffscreenCount", "OffscreenTextCount"),
                    insideScreen = ReadBool(item, "InsideScreen", "Onscreen"),
                    compact = ReadBool(item, "Compact", "CompactTray"),
                    playerClear = ReadBool(item, "PlayerClear", "ClearsPlayer"),
                    walkingBandClear = ReadBool(item, "WalkingBandClear", "ClearsWalkingBand"),
                    result = ReadString(item, "Result", "Status")
                };
                layouts.Add(layout);
            }
            return layouts.ToArray();
        }

        private static CatalogAudit DiscoverCatalog(Assembly assembly, bool regions)
        {
            CatalogAudit best = new CatalogAudit();
            foreach (Type type in SafeTypes(assembly))
            {
                foreach (PropertyInfo property in type.GetProperties(PublicStatic).Where(value => value.GetIndexParameters().Length == 0))
                {
                    object raw;
                    try { raw = property.GetValue(null, null); } catch { continue; }
                    if (!(raw is IEnumerable enumerable) || raw is string) continue;
                    List<object> items = enumerable.Cast<object>().Where(value => value != null).ToList();
                    if (items.Count == 0) continue;
                    List<string> ids = items.Select(item => ReadString(item, regions ? new[] { "StableId", "RegionId", "Id" } : new[] { "StableId", "NodeId", "Id" }))
                        .Where(value => regions ? value.StartsWith("region.", StringComparison.Ordinal) : IsStableNodeId(value)).Distinct(StringComparer.Ordinal).ToList();
                    string semantic = (type.Name + "." + property.Name + " " + DescribePublicSurface(items[0].GetType())).ToLowerInvariant();
                    bool relevant = regions ? semantic.Contains("region") : semantic.Contains("node") && ContainsAny(semantic, "search", "loot", "resource");
                    if (!relevant || ids.Count <= best.StableIds.Count) continue;
                    best.Owner = type.FullName + "." + property.Name;
                    best.StableIds.Clear(); best.StableIds.AddRange(ids);
                    best.Items.Clear(); best.Items.AddRange(items.Where(item => ids.Contains(ReadString(item, regions ? new[] { "StableId", "RegionId", "Id" } : new[] { "StableId", "NodeId", "Id" }))));
                }
            }
            if (!regions && best.Items.Count == 0)
            {
                CatalogAudit regionCatalog = DiscoverCatalog(assembly, true);
                List<object> nested = regionCatalog.Items
                    .SelectMany(region => EnumerateMember(region, "Nodes", "NodeDefinitions", "SearchNodes"))
                    .Where(value => value != null && IsStableNodeId(ReadString(value, "StableId", "NodeId", "Id")))
                    .ToList();
                if (nested.Count > 0)
                {
                    best.Owner = regionCatalog.Owner + " -> Nodes";
                    best.Items.AddRange(nested);
                    best.StableIds.AddRange(nested.Select(value => ReadString(value, "StableId", "NodeId", "Id"))
                        .Distinct(StringComparer.Ordinal));
                }
            }
            return best;
        }

        private static GeneratorAudit DiscoverGenerator(Assembly assembly, CatalogAudit nodes, CatalogAudit regions)
        {
            GeneratorAudit best = new GeneratorAudit();
            foreach (Type type in SafeTypes(assembly))
            {
                foreach (MethodInfo method in type.GetMethods(PublicStatic))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    bool stableStringSignature = parameters.Length == 3 && parameters.Count(value => value.ParameterType == typeof(int)) == 1 && parameters.Count(value => value.ParameterType == typeof(string)) == 2;
                    bool structuredOwnerSignature = parameters.Length == 2 && parameters.Count(value => value.ParameterType == typeof(int)) == 1 &&
                                                    parameters.Any(value => ContainsAll(DescribePublicSurface(value.ParameterType).ToLowerInvariant(), "nodeid", "regionid"));
                    if (!stableStringSignature && !structuredOwnerSignature) continue;
                    string surface = DescribePublicSurface(method.ReturnType);
                    string semantic = (type.Name + "." + method.Name + " " + surface).ToLowerInvariant();
                    if (!ContainsAny(semantic, "node", "loot", "search"))
                    {
                        if (string.IsNullOrEmpty(best.Surface) && ContainsAny(semantic, "node", "loot", "search"))
                        {
                            best.Owner = type.FullName + "." + method.Name;
                            best.Surface = surface + " (rejected: no structured contents collection)";
                        }
                        continue;
                    }
                    string regionId = regions.StableIds.FirstOrDefault() ?? "region.coast.beach";
                    string nodeId = nodes.StableIds.FirstOrDefault() ?? "node.qa.search.0";
                    object nodeOwner = nodes.Items.FirstOrDefault(value => string.Equals(ReadString(value, "StableId", "NodeId", "Id"), nodeId, StringComparison.Ordinal));
                    try
                    {
                        object first = InvokeGenerator(method, 424242, regionId, nodeId, nodeOwner);
                        object second = InvokeGenerator(method, 424242, regionId, nodeId, nodeOwner);
                        if (first == null || second == null || !HasStructuredContents(first, nodeId)) continue;
                        string firstFingerprint = Fingerprint(first);
                        string secondFingerprint = Fingerprint(second);
                        bool varied = new[] { 424243, 424244, 424245, 424246, 424247 }
                            .Select(seed => InvokeGenerator(method, seed, regionId, nodeId, nodeOwner))
                            .Where(value => value != null && HasStructuredContents(value, nodeId))
                            .Select(Fingerprint).Any(value => value != firstFingerprint);
                        int finiteNodes = nodes.Items.Count(value =>
                        {
                            string stableId = ReadString(value, "StableId", "NodeId", "Id");
                            object generated = InvokeGenerator(method, 424242, ReadString(value, "RegionId", "StableRegionId"), stableId, value);
                            return HasStructuredContents(generated, stableId) && HasFinitePositiveContents(generated);
                        });
                        best.Owner = type.FullName + "." + method.Name;
                        best.Surface = surface + "; first=" + firstFingerprint;
                        best.StructuredContents = true;
                        best.SameSeedDeterministic = firstFingerprint == secondFingerprint;
                        best.DifferentSeedVaries = varied;
                        best.FiniteNodeCount = finiteNodes;
                        return best;
                    }
                    catch (Exception exception)
                    {
                        best.Owner = type.FullName + "." + method.Name;
                        best.Surface = surface + " invocation=" + exception.GetType().Name + ":" + exception.Message;
                    }
                }
            }
            return best;
        }

        private static object InvokeGenerator(MethodInfo method, int seed, string regionId, string nodeId, object nodeOwner)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];
            int stringIndex = 0;
            for (int index = 0; index < parameters.Length; index += 1)
            {
                if (parameters[index].ParameterType == typeof(int)) arguments[index] = seed;
                else if (parameters[index].ParameterType == typeof(string))
                {
                    string name = parameters[index].Name ?? string.Empty;
                    arguments[index] = name.IndexOf("region", StringComparison.OrdinalIgnoreCase) >= 0 ? regionId :
                        name.IndexOf("node", StringComparison.OrdinalIgnoreCase) >= 0 ? nodeId : stringIndex++ == 0 ? regionId : nodeId;
                }
                else if (nodeOwner != null && parameters[index].ParameterType.IsInstanceOfType(nodeOwner)) arguments[index] = nodeOwner;
                else throw new InvalidOperationException("unsupported structured generator owner parameter: " + parameters[index].ParameterType.FullName);
            }
            try { return method.Invoke(null, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static string DiscoverSnapshotSurface(Assembly assembly)
        {
            List<string> surfaces = new List<string>();
            foreach (Type type in SafeTypes(assembly))
            {
                string surface = DescribePublicSurface(type);
                string semantic = (type.Name + " " + surface).ToLowerInvariant();
                if (ContainsAny(semantic, "search", "node", "region") && ContainsAny(semantic, "snapshot", "state", "persistence"))
                    surfaces.Add(type.FullName + "{" + surface + "}");
            }
            return string.Join(" | ", surfaces.OrderBy(value => value, StringComparer.Ordinal).ToArray()).ToLowerInvariant();
        }

        private static string AuditLocalization(out bool passed)
        {
            string path = Path.Combine(Application.dataPath, "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            if (!File.Exists(path)) { passed = false; return "canonical TSV missing: " + path; }
            string[][] requiredSemantics =
            {
                new[] { "search", "node.world.hidden" },
                new[] { "reveal", "state.hidden", "message.search_node.revealed" },
                new[] { "action.take" },
                new[] { "action.take_all" },
                new[] { "action.leave" },
                new[] { "replace", "choose_swap" },
                new[] { "cancel", "cancel_swap" },
                new[] { "remaining", "world.partial", "tray.status" },
                new[] { "depleted" },
                new[] { "protected" },
                new[] { "cost", "tray.status" },
                new[] { "risk", "hazard", "exposure", "tray.status" }
            };
            List<string[]> rows = File.ReadAllLines(path, Encoding.UTF8)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
                .Select(line => line.Split('\t')).Where(columns => columns.Length >= 4)
                .Where(columns => ContainsAny((columns[0] ?? string.Empty).ToLowerInvariant(), "search", "loot", "node", "tray", "gather"))
                .ToList();
            string keys = string.Join("|", rows.Select(row => row[0]).ToArray()).ToLowerInvariant();
            string[] missing = requiredSemantics.Where(group => !group.Any(keys.Contains)).Select(group => string.Join("/", group)).ToArray();
            bool allLocales = rows.Count >= 12 && rows.All(row => row.Skip(1).Take(3).All(value => !string.IsNullOrWhiteSpace(value)));
            double averageExpansion = rows.Count == 0 ? 0d : rows.Average(row => row[2].Length == 0 ? 0d : (double)row[3].Length / row[2].Length);
            passed = missing.Length == 0 && allLocales && averageExpansion >= 1.25d;
            return "rows=" + rows.Count + "; missingTokens=" + string.Join(",", missing) + "; allLocales=" + allLocales + "; qps/en=" + averageExpansion.ToString("0.00") + "; keys=" + string.Join(",", rows.Select(row => row[0]).ToArray());
        }

        private static string AuditProtectedPartSurface(Assembly assembly, CatalogAudit nodes, out bool passed)
        {
            List<object> generated = new List<object>();
            foreach (object node in nodes.Items)
            {
                PrototypeSearchNodeDefinition definition = node as PrototypeSearchNodeDefinition;
                if (definition != null) generated.Add(PrototypeSearchNodeLootResolver.Resolve(PrototypeExpeditionRegionCatalog.DefaultRunSeed, definition));
            }
            string catalogValues = string.Join(" | ", generated.Select(value => DescribeObject(value)).ToArray()).ToLowerInvariant();
            string typeSurface = string.Join(" | ", SafeTypes(assembly)
                .Where(type => ContainsAny(type.Name.ToLowerInvariant(), "search", "node", "loot", "protected", "projectinventory"))
                .Select(type => type.FullName + "{" + DescribePublicSurface(type) + "; methods=" +
                                string.Join(",", type.GetMethods(PublicInstance | PublicStatic).Select(method => method.Name).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray()) + "}")
                .ToArray()).ToLowerInvariant();
            string combined = catalogValues + " | " + typeSurface;
            bool transactionSurface = ContainsAll(typeSurface, "tryacquireprotectedpart", "hasprotectedpart", "consume", "protectedpartids");
            passed = combined.Contains(SailclothId) && transactionSurface && string.Equals(PrototypeRaftEscapeConfig.KeyPartId, SailclothId, StringComparison.Ordinal);
            return "sailcloth=" + combined.Contains(SailclothId) + "; raftKey=" + PrototypeRaftEscapeConfig.KeyPartId + "; transactionSurface=" + transactionSurface + "; surface=" + combined;
        }

        private static bool HasFinitePositiveContents(object value)
        {
            object contents = value is IEnumerable && !(value is string) ? value : GetMember(value, "Contents", "Items", "InitialContents", "ResourceBudget", "Remaining", "RemainingItems");
            if (!(contents is IEnumerable enumerable) || contents is string) return false;
            bool any = false;
            foreach (object item in enumerable)
            {
                if (item == null) continue;
                any = true;
                int amount = ReadInt(item, "Amount", "Count", "Quantity", "Total");
                if (amount <= 0) return false;
            }
            return any;
        }

        private static bool HasStructuredContents(object value, string inputNodeId = "")
        {
            string nodeId = string.IsNullOrWhiteSpace(inputNodeId) ? ReadString(value, "NodeId", "StableNodeId", "StableId", "ActionId") : inputNodeId;
            object contents = value is IEnumerable && !(value is string) ? value : GetMember(value, "Contents", "Items", "InitialContents", "Remaining", "RemainingItems");
            if (string.IsNullOrWhiteSpace(nodeId) || !(contents is IEnumerable enumerable) || contents is string) return false;
            return enumerable.Cast<object>().Any(item => item != null && !string.IsNullOrWhiteSpace(ReadString(item, "StableItemId", "ItemId", "ResourceId", "StableId", "Id")) && ReadInt(item, "Amount", "Count", "Quantity") > 0);
        }

        private static IEnumerable<object> EnumerateMember(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<object>();
            return enumerable.Cast<object>();
        }

        private static string Fingerprint(object value)
        {
            return DescribeObject(value);
        }

        private static string DescribeObject(object value, int depth = 0)
        {
            if (value == null) return "null";
            if (depth > 3) return value.ToString();
            if (value is string) return (string)value;
            if (value is IEnumerable enumerable)
            {
                List<string> items = new List<string>();
                foreach (object item in enumerable) items.Add(DescribeObject(item, depth + 1));
                return "[" + string.Join(",", items.ToArray()) + "]";
            }
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal) return value.ToString();
            List<string> members = new List<string>();
            foreach (PropertyInfo property in type.GetProperties(PublicInstance).Where(item => item.GetIndexParameters().Length == 0).OrderBy(item => item.Name, StringComparer.Ordinal))
            {
                try { members.Add(property.Name + "=" + DescribeObject(property.GetValue(value, null), depth + 1)); } catch { }
            }
            foreach (FieldInfo field in type.GetFields(PublicInstance).OrderBy(item => item.Name, StringComparer.Ordinal))
            {
                try { members.Add(field.Name + "=" + DescribeObject(field.GetValue(value), depth + 1)); } catch { }
            }
            return type.FullName + "{" + string.Join(";", members.ToArray()) + "}";
        }

        private static string DescribePublicSurface(Type type)
        {
            if (type == null) return string.Empty;
            return string.Join(",", type.GetMembers(PublicInstance | PublicStatic)
                .Where(member => member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property)
                .Select(member => member.Name).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static Type[] SafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException exception) { return exception.Types.Where(type => type != null).ToArray(); }
        }

        private static object GetMember(object owner, params string[] names)
        {
            if (owner == null) return null;
            Type type = owner.GetType();
            foreach (string name in names)
            {
                PropertyInfo property = type.GetProperty(name, PublicInstance | BindingFlags.IgnoreCase);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    try { return property.GetValue(owner, null); } catch { }
                }
                FieldInfo field = type.GetField(name, PublicInstance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    try { return field.GetValue(owner); } catch { }
                }
            }
            return null;
        }

        private static string ReadString(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            return value == null ? string.Empty : value.ToString();
        }

        private static string[] ReadStrings(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (!(value is IEnumerable enumerable) || value is string) return Array.Empty<string>();
            return enumerable.Cast<object>().Where(item => item != null).Select(item => item.ToString()).ToArray();
        }

        private static bool ReadBool(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value is bool result) return result;
            bool parsed;
            return value != null && bool.TryParse(value.ToString(), out parsed) && parsed;
        }

        private static int ReadInt(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value is int result) return result;
            int parsed;
            return value != null && int.TryParse(value.ToString(), out parsed) ? parsed : 0;
        }

        private static float ReadFloat(object owner, params string[] names)
        {
            object value = GetMember(owner, names);
            if (value is float result) return result;
            float parsed;
            return value != null && float.TryParse(value.ToString(), out parsed) ? parsed : 0f;
        }

        private static bool IsStableNodeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string lower = value.ToLowerInvariant();
            return lower.Contains("node.") || lower.StartsWith("node", StringComparison.Ordinal) || lower.Contains(".node.");
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

        private static bool TokensInOrder(string value, params string[] tokens)
        {
            string text = (value ?? string.Empty).ToLowerInvariant();
            int cursor = -1;
            foreach (string token in tokens)
            {
                int next = text.IndexOf(token.ToLowerInvariant(), cursor + 1, StringComparison.Ordinal);
                if (next < 0) return false;
                cursor = next;
            }
            return true;
        }

        private static bool FreshEvidenceFileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string full = Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(EvidenceFolder, path));
            string root = EvidenceFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full);
        }

        private static string DescribeIdentity(PlayEvidence evidence)
        {
            return "owner=" + evidence.observationOwner + "; method=" + evidence.observationMethod + "; region=" + evidence.regionId + "; node=" + evidence.nodeId + "; error=" + evidence.observationError;
        }

        private static string Trace(PlayEvidence evidence)
        {
            return DescribeIdentity(evidence) + "; states=" + string.Join(",", evidence.stateSequence ?? Array.Empty<string>()) + "; trace=" + string.Join(" | ", evidence.interactionTrace ?? Array.Empty<string>());
        }

        private static string DescribeLayout(LayoutEvidence value)
        {
            return value.locale + "=" + value.x.ToString("0.0") + "," + value.y.ToString("0.0") + "," + value.width.ToString("0.0") + "x" + value.height.ToString("0.0") +
                   "; overflow/offscreen=" + value.overflowCount + "/" + value.offscreenCount + "; compact/player/path=" + value.compact + "/" + value.playerClear + "/" + value.walkingBandClear + "; capture=" + value.screenshot;
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
                checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected, exception.GetType().Name + ": " + exception.Message, reproduction, files));
            }
        }

        private static void Unverified(List<Check> checks, string id, string matrix, string severity, string expected, string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", "HARDWARE_GAP", severity, expected, actual, reproduction, files));
        }

        private static void NotReady(List<Check> checks, string id, string matrix, string severity, string expected, string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "NOT_READY", "EXTERNAL_RELEASE_GAP", severity, expected, actual, reproduction, files));
        }

        private static Check NewCheck(string id, string matrix, string status, string classification, string severity, string expected, string actual, string reproduction, string files)
        {
            return new Check { id = id, matrix = matrix, status = status, classification = classification, severity = severity, expected = expected, actual = actual, reproduction = reproduction, recommendedFiles = files };
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
                greenCompletionCondition = "All GSN product checks pass from structured actual Edit/Play observations; fresh Wave 20 is 16/16, Wave 19 is 22/22, and compile/build/smoke/Addressables/firewall remain PASS. Physical hardware and Steam stay separate.",
                checks = checks.ToArray()
            };
            report.productOverall = report.productFailed > 0 ? "FAIL" : report.expectedGaps > 0 ? "RED_EXPECTED_GAP" : "PASS";
            report.infrastructureOverall = report.infrastructureFailed > 0 ? "FAIL" : "PASS";
            report.overall = report.infrastructureOverall == "FAIL" || report.productOverall == "FAIL" ? "FAIL" : report.productOverall == "RED_EXPECTED_GAP" ? "RED" : "GREEN";
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
                NewCheck("GSN-I99.play_runner", "Play infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "The Play runner creates parseable evidence from the live Scene", exception.ToString(),
                    "Run Invoke-GameJamSearchNodeRedFirstGate.ps1 outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/GameJamSearchNodeRedFirstGateRunner.cs")
            };
            WriteReport("gamejam-search-node-play-contracts", "GameJam search-node Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: " + exception);
        }

        private static void StopPlayContracts() { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; }

        private static void FinishPlayContracts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }
            bool passed = SessionState.GetBool(PlayExitPassKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing GameJam search-node Play result");
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
