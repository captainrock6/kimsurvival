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

namespace ParallelQA
{
    /// <summary>
    /// O11 independent RED-first gate. Product acceptance is derived from a
    /// structured live observation, never from a product-owned pass flag,
    /// fixture string, class-name allowlist, grant, warp, or skip trace.
    /// </summary>
    [InitializeOnLoad]
    public static class O11IntegrationGateRunner
    {
        private const string RedBaseline = "aa67a12bb38180f7cf2635a2a2bca3c403b5248a";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RunningKey = "ParallelQA.O11.Running";
        private const string PassedKey = "ParallelQA.O11.InfrastructurePassed";
        private const string MessageKey = "ParallelQA.O11.Message";
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static bool tickAttached;
        private static double earliestRunTime;
        private static double timeoutAt;

        [Serializable]
        private sealed class Check
        {
            public string id = string.Empty;
            public string severity = string.Empty;
            public string status = string.Empty;
            public string classification = "PRODUCT";
            public string expected = string.Empty;
            public string actual = string.Empty;
            public string reproduction = string.Empty;
            public string greenCondition = string.Empty;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion = 1;
            public string title = "O11 independent integration RED-first gate";
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public string unityVersion = string.Empty;
            public string completedUtc = string.Empty;
            public string overall = string.Empty;
            public string productOverall = string.Empty;
            public string infrastructureOverall = "PASS";
            public int passed;
            public int expectedGaps;
            public int failed;
            public string physicalGamepad = "UNVERIFIED";
            public string steamReadiness = "NOT_READY";
            public string observationOwner = string.Empty;
            public string observationMethod = string.Empty;
            public Check[] checks = Array.Empty<Check>();
        }

        [Serializable]
        private sealed class EditEvidence
        {
            public string runId = string.Empty;
            public string baselineCommit = string.Empty;
            public string unityVersion = string.Empty;
            public string scene = ScenePath;
            public string o10HumanResultPath = "Docs/Design/Playtest/Sessions/O10-H1-2026-08-28.md";
            public string v2AssetId = "ui.gamejam.style-benchmark";
            public string v2JobId = "job_20260828122852_c9ccf2aa";
            public bool v2RegistryEngineReady;
            public bool v2RegistryAdopted;
            public bool v2PackagePresent;
            public string v2ImageGuid = string.Empty;
            public int sceneV2DependencyCount;
            public int projectAnimationClipCount;
            public string[] projectAnimationClipPaths = Array.Empty<string>();
            public string[] requiredFacilities = { "structure.workbench", "structure.rain_collector", "structure.bed", "structure.sofa" };
            public string[] requiredRooms = { "room.start", "room.upper.standard", "room.basement.standard" };
            public string[] requiredCharacterStates = { "kim.idle", "kim.walk", "kim.search", "kim.ladder", "kim.swim" };
            public string note = "Static evidence is diagnostic only; GREEN requires the live structured Play observation.";
        }

        [Serializable]
        private sealed class O11Observation
        {
            public string ContractId = string.Empty;
            public string[] Trace = Array.Empty<string>();
            public bool Grant;
            public bool Warp;
            public bool Skip;
            public PlacementObservation[] Placements = Array.Empty<PlacementObservation>();
            public PlacementRejectionObservation[] PlacementRejections = Array.Empty<PlacementRejectionObservation>();
            public LaunchObservation[] Launches = Array.Empty<LaunchObservation>();
            public ReactionObservation[] Reactions = Array.Empty<ReactionObservation>();
            public LayoutObservation[] Layouts = Array.Empty<LayoutObservation>();
            public PacingObservation[] Pacing = Array.Empty<PacingObservation>();
            public RouteBurdenObservation[] RouteBurdens = Array.Empty<RouteBurdenObservation>();
            public AssetBindingObservation[] AssetBindings = Array.Empty<AssetBindingObservation>();
            public string PacingFirstFingerprint = string.Empty;
            public string PacingRepeatFingerprint = string.Empty;
        }

        [Serializable]
        private sealed class PlacementObservation
        {
            public string FacilityId = string.Empty;
            public string StableRoomId = string.Empty;
            public float InitialX;
            public float RelocatedX;
            public float RestoredX;
            public bool PlacementCommitted;
            public bool RelocationCommitted;
            public bool SnapshotRestored;
        }

        [Serializable]
        private sealed class PlacementRejectionObservation
        {
            public string StableRoomId = string.Empty;
            public string ReasonId = string.Empty;
            public bool Rejected;
            public string BeforeFingerprint = string.Empty;
            public string AfterFingerprint = string.Empty;
        }

        [Serializable]
        private sealed class LaunchObservation
        {
            public string CaseId = string.Empty;
            public bool AvailabilityDisplayed;
            public bool ButtonInteractable;
            public string ReasonId = string.Empty;
            public bool Confirmed;
            public bool SameTransaction;
            public int FoodBefore;
            public int FoodAfter;
            public string ResourcesBefore = string.Empty;
            public string ResourcesAfter = string.Empty;
            public string ProgressBefore = string.Empty;
            public string ProgressAfter = string.Empty;
            public int CommitCount;
            public int TerminalCount;
        }

        [Serializable]
        private sealed class ReactionObservation
        {
            public string StableRoomId = string.Empty;
            public string[] StateSequence = Array.Empty<string>();
            public bool MovementObservedAfterReaction;
        }

        [Serializable]
        private sealed class LayoutObservation
        {
            public string Locale = string.Empty;
            public int Width;
            public int Height;
            public string Screenshot = string.Empty;
            public int OverflowCount;
            public int OffscreenCount;
            public int BagPopupOverlapCount;
            public int WorldOcclusionCount;
        }

        [Serializable]
        private sealed class PacingObservation
        {
            public int SearchIndex;
            public float EnergyBefore;
            public float EnergyAfter;
            public string RecoveryMethodId = string.Empty;
            public float RecoveryAmount;
            public float NextSearchAvailableSeconds;
        }

        [Serializable]
        private sealed class RouteBurdenObservation
        {
            public int Seed;
            public string EscapeId = string.Empty;
            public bool Feasible;
            public float BurdenScore;
            public string ResourceFingerprint = string.Empty;
            public string ProtectedPartId = string.Empty;
        }

        [Serializable]
        private sealed class AssetBindingObservation
        {
            public string StableId = string.Empty;
            public string Guid = string.Empty;
            public string AssetPath = string.Empty;
            public string ClipName = string.Empty;
            public bool RuntimeObserved;
            public bool Placeholder;
            public bool ReviewOnly;
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "O11_missing_run_id" : value.Trim();
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

        static O11IntegrationGateRunner()
        {
            if (SessionState.GetBool(RunningKey, false)) Attach();
        }

        public static void RunEditContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            Require(RunId.StartsWith("O11", StringComparison.Ordinal), "evidence RunId must start with O11");
            Require(BaselineCommit.Length == 40, "full baseline SHA is required");
            Require(File.Exists(ScenePath), "playable scene missing: " + ScenePath);

            const string registryPath = ".forge/assets.json";
            const string jobFolder = "Assets/_Project/Art/Generated/ui_set/job_20260828122852_c9ccf2aa";
            string registry = File.Exists(registryPath) ? File.ReadAllText(registryPath) : string.Empty;
            int assetIndex = registry.IndexOf("\"id\": \"ui.gamejam.style-benchmark\"", StringComparison.Ordinal);
            string record = assetIndex < 0 ? string.Empty : registry.Substring(assetIndex, Math.Min(7000, registry.Length - assetIndex));
            string imagePath = Directory.Exists(jobFolder)
                ? Directory.GetFiles(jobFolder, "*.png", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? string.Empty
                : string.Empty;
            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/_Project" });
            string[] clipPaths = clipGuids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] dependencies = AssetDatabase.GetDependencies(ScenePath, true);

            EditEvidence evidence = new EditEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                v2RegistryEngineReady = record.IndexOf("\"status\": \"engine_ready\"", StringComparison.Ordinal) >= 0,
                v2RegistryAdopted = record.IndexOf("\"status\": \"adopted\"", StringComparison.Ordinal) >= 0,
                v2PackagePresent = File.Exists(Path.Combine(jobFolder, "forge-import.json")) && !string.IsNullOrEmpty(imagePath),
                v2ImageGuid = string.IsNullOrEmpty(imagePath) ? string.Empty : AssetDatabase.AssetPathToGUID(imagePath.Replace('\\', '/')),
                sceneV2DependencyCount = dependencies.Count(path => path.Replace('\\', '/').StartsWith(jobFolder + "/", StringComparison.Ordinal)),
                projectAnimationClipCount = clipPaths.Length,
                projectAnimationClipPaths = clipPaths
            };
            WriteJson("O11-edit-evidence.json", evidence);
            File.WriteAllText(Path.Combine(EvidenceFolder, "O11-edit-result.txt"),
                "INFRASTRUCTURE=PASS" + Environment.NewLine +
                "Baseline=" + BaselineCommit + Environment.NewLine +
                "Unity=" + Application.unityVersion + Environment.NewLine +
                "V2 adopted/engine-ready/package/scene-dependencies=" + evidence.v2RegistryAdopted + "/" +
                evidence.v2RegistryEngineReady + "/" + evidence.v2PackagePresent + "/" + evidence.sceneV2DependencyCount + Environment.NewLine +
                "AnimationClip assets=" + evidence.projectAnimationClipCount + Environment.NewLine,
                Utf8NoBom);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "O11 Play verification did not finish.");
            Attach();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void Attach()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (!tickAttached)
            {
                EditorApplication.update += Tick;
                tickAttached = true;
            }
            earliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
            timeoutAt = EditorApplication.timeSinceStartup + 90d;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                earliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                timeoutAt = EditorApplication.timeSinceStartup + 90d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode) FinishAndExit();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup < earliestRunTime) return;
            if (EditorApplication.timeSinceStartup > timeoutAt)
            {
                SessionState.SetString(MessageKey, "Timed out waiting for the playable O11 observation.");
                StopPlayMode();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;
            try
            {
                CaptureBaselineScreens(prototype);
                string owner;
                string method;
                O11Observation observation = DiscoverLiveObservation(out owner, out method);
                if (observation != null) WriteJson("O11-live-observation.json", observation);
                Report report = Evaluate(observation, owner, method);
                WriteJson("O11-product-report.json", report);
                WriteReportText(report);
                SessionState.SetBool(PassedKey, true);
                SessionState.SetString(MessageKey, report.overall + " · " + report.expectedGaps + " expected gaps, " + report.failed + " failures");
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, exception.ToString());
            }
            StopPlayMode();
        }

        private static O11Observation DiscoverLiveObservation(out string owner, out string methodName)
        {
            owner = string.Empty;
            methodName = string.Empty;
            foreach (MonoBehaviour component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (component == null) continue;
                foreach (MethodInfo method in component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (method.IsSpecialName || method.GetParameters().Length != 0 || method.ReturnType == typeof(void) ||
                        method.ReturnType.IsPrimitive || method.ReturnType == typeof(string)) continue;
                    HashSet<string> members = new HashSet<string>(
                        method.ReturnType.GetMembers(BindingFlags.Instance | BindingFlags.Public).Select(value => value.Name),
                        StringComparer.OrdinalIgnoreCase);
                    int semanticGroups = new[] { "Placements", "Launches", "Reactions", "Layouts", "Pacing", "RouteBurdens", "AssetBindings" }
                        .Count(members.Contains);
                    if (semanticGroups < 5) continue;
                    object raw = method.Invoke(component, null);
                    if (raw == null) continue;
                    string json = JsonUtility.ToJson(raw, true);
                    O11Observation candidate = JsonUtility.FromJson<O11Observation>(json);
                    if (candidate == null) continue;
                    owner = component.GetType().FullName;
                    methodName = method.Name;
                    File.WriteAllText(Path.Combine(EvidenceFolder, "O11-live-observation-raw.json"), json + Environment.NewLine, Utf8NoBom);
                    return candidate;
                }
            }
            return null;
        }

        private static Report Evaluate(O11Observation observation, string owner, string method)
        {
            List<Check> checks = new List<Check>();
            bool cheatFree = observation != null && !observation.Grant && !observation.Warp && !observation.Skip;
            AddProduct(checks, "O11-P0-001", "P0",
                "Four general facilities place and relocate in start/upper/basement, exact StableRoomId/X restores, and collision/entrance/path attempts reject atomically.",
                ValidatePlacement(observation, cheatFree, out string placementActual), placementActual,
                "Complete both modules; place then relocate workbench/rain collector/bed/sofa in each room; save, reload, and attempt collision/entrance/path positions.",
                "12 unique facility-room observations plus exact restore coordinates and canonical rejection reasons.");
            AddProduct(checks, "O11-P0-002", "P0",
                "Launch availability and confirmation agree; impossible/duplicate input is locked with a reason and zero mutation; possible confirmation succeeds in one transaction.",
                ValidateLaunch(observation, cheatFree, out string launchActual), launchActual,
                "At shore launch, exercise impossible, same-day duplicate, and possible cases while recording button state and before/after ledgers.",
                "All three cases are observed through the live popup with zero failed-case deltas and one possible-case terminal commit.");
            AddProduct(checks, "O11-P1-001", "P1",
                "After upper and basement construction reactions, Kim returns to idle and subsequently moves.",
                ValidateReaction(observation, cheatFree, out string reactionActual), reactionActual,
                "Commit upper and basement modules, wait for each reaction, then issue ordinary movement.",
                "Both room traces contain reaction then idle and live movement without grant/warp/skip.");
            AddProduct(checks, "O11-P1-002", "P1",
                "Adopted V2 skin is runtime-bound and KO/EN bag, popup, and world do not overlap at 1280x800 or 1920x1080.",
                ValidateUi(observation, out string uiActual), uiActual,
                "Open bag and representative facility/escape popups in KO and EN at both target resolutions.",
                "Runtime asset binding to ui.gamejam.style-benchmark/job_20260828122852_c9ccf2aa and four zero-overlap/overflow captures.");
            AddProduct(checks, "O11-P1-003", "P1",
                "Three consecutive searches and recovery methods produce deterministic stamina pacing.",
                ValidatePacing(observation, cheatFree, out string pacingActual), pacingActual,
                "Follow one natural seed through search-return-recovery three times and repeat from a fresh identical seed.",
                "Three ordered live costs, effective recovery, positive next-search windows, and byte-equal repeat fingerprints.");
            AddProduct(checks, "O11-P1-004", "P1",
                "Representative seeds keep raft, smoke, and radio feasible; raft is never 25% or more easier than both alternatives.",
                ValidateRouteBurden(observation, cheatFree, out string routeActual), routeActual,
                "For each representative seed derive the natural resource/research/part/time burden of raft, smoke, and radio.",
                "At least three seeds x three feasible routes and raft burden >= 75% of min(smoke, radio) for every seed.");
            AddProduct(checks, "O11-P1-005", "P1",
                "Seven production search-region visuals and Kim idle/walk/search/ladder/swim use real runtime GUIDs/clips, not placeholders or review-only art.",
                ValidateAssets(observation, out string assetActual), assetActual,
                "Visit all seven regions and exercise all five Kim states while recording the rendered asset GUID/clip.",
                "Seven region IDs and five Kim state IDs have runtime-observed non-placeholder, non-review GUID/clip bindings.");

            int passed = checks.Count(value => value.status == "PASS");
            int gaps = checks.Count(value => value.status == "EXPECTED_GAP");
            int failed = checks.Count(value => value.status == "FAIL");
            return new Report
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                completedUtc = DateTime.UtcNow.ToString("O"),
                overall = failed > 0 ? "FAIL" : gaps > 0 ? "RED" : "GREEN",
                productOverall = failed > 0 ? "FAIL" : gaps > 0 ? "RED_EXPECTED_GAP" : "PASS",
                passed = passed,
                expectedGaps = gaps,
                failed = failed,
                observationOwner = owner,
                observationMethod = method,
                checks = checks.ToArray()
            };
        }

        private static bool ValidatePlacement(O11Observation value, bool cheatFree, out string actual)
        {
            PlacementObservation[] rows = value == null ? Array.Empty<PlacementObservation>() : value.Placements ?? Array.Empty<PlacementObservation>();
            PlacementRejectionObservation[] rejects = value == null ? Array.Empty<PlacementRejectionObservation>() : value.PlacementRejections ?? Array.Empty<PlacementRejectionObservation>();
            string[] facilities = { "structure.workbench", "structure.rain_collector", "structure.bed", "structure.sofa" };
            string[] rooms = { "room.start", "room.upper.standard", "room.basement.standard" };
            bool matrix = facilities.All(facility => rooms.All(room => rows.Any(row => Eq(row.FacilityId, facility) && Eq(row.StableRoomId, room) &&
                row.PlacementCommitted && row.RelocationCommitted && row.SnapshotRestored && !Mathf.Approximately(row.InitialX, row.RelocatedX) &&
                Mathf.Approximately(row.RelocatedX, row.RestoredX))));
            string[] reasons = { "placement.overlap", "placement.blocks_entrance", "placement.blocks_path" };
            bool rejectionMatrix = rooms.All(room => reasons.All(reason => rejects.Any(row => Eq(row.StableRoomId, room) && Eq(row.ReasonId, reason) &&
                row.Rejected && Eq(row.BeforeFingerprint, row.AfterFingerprint))));
            actual = "observation=" + (value != null) + "; placementRows=" + rows.Length + "; rejectionRows=" + rejects.Length +
                     "; matrix=" + matrix + "; rejectionMatrix=" + rejectionMatrix + "; cheatFree=" + cheatFree;
            return value != null && cheatFree && matrix && rejectionMatrix;
        }

        private static bool ValidateLaunch(O11Observation value, bool cheatFree, out string actual)
        {
            LaunchObservation[] rows = value == null ? Array.Empty<LaunchObservation>() : value.Launches ?? Array.Empty<LaunchObservation>();
            LaunchObservation impossible = rows.FirstOrDefault(row => Eq(row.CaseId, "launch.impossible"));
            LaunchObservation duplicate = rows.FirstOrDefault(row => Eq(row.CaseId, "launch.duplicate_same_day"));
            LaunchObservation possible = rows.FirstOrDefault(row => Eq(row.CaseId, "launch.possible"));
            bool immutableImpossible = immutable(impossible) && !impossible.AvailabilityDisplayed && !impossible.ButtonInteractable && !string.IsNullOrWhiteSpace(impossible.ReasonId);
            bool immutableDuplicate = immutable(duplicate) && !duplicate.ButtonInteractable && !string.IsNullOrWhiteSpace(duplicate.ReasonId);
            bool possibleAtomic = possible != null && possible.AvailabilityDisplayed && possible.ButtonInteractable && possible.Confirmed &&
                                  possible.SameTransaction && possible.CommitCount == 1 && possible.TerminalCount == 1;
            actual = "cases=" + rows.Length + "; impossible=" + immutableImpossible + "; duplicate=" + immutableDuplicate +
                     "; possibleAtomic=" + possibleAtomic + "; cheatFree=" + cheatFree;
            return value != null && cheatFree && immutableImpossible && immutableDuplicate && possibleAtomic;
        }

        private static bool immutable(LaunchObservation row)
        {
            return row != null && row.FoodBefore == row.FoodAfter && Eq(row.ResourcesBefore, row.ResourcesAfter) &&
                   Eq(row.ProgressBefore, row.ProgressAfter) && row.CommitCount == 0 && row.TerminalCount == 0 && !row.Confirmed;
        }

        private static bool ValidateReaction(O11Observation value, bool cheatFree, out string actual)
        {
            ReactionObservation[] rows = value == null ? Array.Empty<ReactionObservation>() : value.Reactions ?? Array.Empty<ReactionObservation>();
            string[] rooms = { "room.upper.standard", "room.basement.standard" };
            bool pass = rooms.All(room => rows.Any(row => Eq(row.StableRoomId, room) && HasReactionThenIdle(row.StateSequence) && row.MovementObservedAfterReaction));
            actual = "rows=" + rows.Length + "; upper+basement=" + pass + "; cheatFree=" + cheatFree;
            return value != null && cheatFree && pass;
        }

        private static bool HasReactionThenIdle(string[] sequence)
        {
            string[] values = sequence ?? Array.Empty<string>();
            int reaction = Array.FindIndex(values, item => ContainsAny(item, "surprise", "build", "construction"));
            int idle = Array.FindIndex(values, item => ContainsAny(item, "idle"));
            int move = Array.FindLastIndex(values, item => ContainsAny(item, "move", "walk"));
            return reaction >= 0 && idle > reaction && move > idle;
        }

        private static bool ValidateUi(O11Observation value, out string actual)
        {
            LayoutObservation[] rows = value == null ? Array.Empty<LayoutObservation>() : value.Layouts ?? Array.Empty<LayoutObservation>();
            AssetBindingObservation[] bindings = value == null ? Array.Empty<AssetBindingObservation>() : value.AssetBindings ?? Array.Empty<AssetBindingObservation>();
            bool style = bindings.Any(row => Eq(row.StableId, "ui.gamejam.style-benchmark") && row.RuntimeObserved && !row.Placeholder &&
                !row.ReviewOnly && !string.IsNullOrWhiteSpace(row.Guid) && !string.IsNullOrWhiteSpace(row.AssetPath));
            bool matrix = new[] { "ko", "en" }.All(locale => new[] { new Vector2Int(1280, 800), new Vector2Int(1920, 1080) }.All(size =>
                rows.Any(row => Eq(row.Locale, locale) && row.Width == size.x && row.Height == size.y && row.OverflowCount == 0 &&
                    row.OffscreenCount == 0 && row.BagPopupOverlapCount == 0 && row.WorldOcclusionCount == 0 && File.Exists(row.Screenshot))));
            actual = "styleRuntime=" + style + "; layouts=" + rows.Length + "; ko/en@2=" + matrix;
            return value != null && style && matrix;
        }

        private static bool ValidatePacing(O11Observation value, bool cheatFree, out string actual)
        {
            PacingObservation[] rows = value == null ? Array.Empty<PacingObservation>() : value.Pacing ?? Array.Empty<PacingObservation>();
            bool three = Enumerable.Range(1, 3).All(index => rows.Any(row => row.SearchIndex == index && row.EnergyBefore > row.EnergyAfter &&
                !string.IsNullOrWhiteSpace(row.RecoveryMethodId) && row.RecoveryAmount > 0f && row.NextSearchAvailableSeconds > 0f));
            bool deterministic = value != null && !string.IsNullOrWhiteSpace(value.PacingFirstFingerprint) &&
                                 Eq(value.PacingFirstFingerprint, value.PacingRepeatFingerprint);
            actual = "rows=" + rows.Length + "; three=" + three + "; deterministic=" + deterministic + "; cheatFree=" + cheatFree;
            return value != null && cheatFree && three && deterministic;
        }

        private static bool ValidateRouteBurden(O11Observation value, bool cheatFree, out string actual)
        {
            RouteBurdenObservation[] rows = value == null ? Array.Empty<RouteBurdenObservation>() : value.RouteBurdens ?? Array.Empty<RouteBurdenObservation>();
            int[] seeds = rows.Select(row => row.Seed).Distinct().ToArray();
            bool pass = seeds.Length >= 3 && seeds.All(seed =>
            {
                RouteBurdenObservation raft = rows.FirstOrDefault(row => row.Seed == seed && Eq(row.EscapeId, "escape.raft"));
                RouteBurdenObservation smoke = rows.FirstOrDefault(row => row.Seed == seed && Eq(row.EscapeId, "escape.smoke"));
                RouteBurdenObservation radio = rows.FirstOrDefault(row => row.Seed == seed && Eq(row.EscapeId, "escape.radio"));
                return ValidRoute(raft) && ValidRoute(smoke) && ValidRoute(radio) &&
                       raft.BurdenScore >= 0.75f * Mathf.Min(smoke.BurdenScore, radio.BurdenScore);
            });
            actual = "rows=" + rows.Length + "; seeds=" + seeds.Length + "; feasible+band=" + pass + "; cheatFree=" + cheatFree;
            return value != null && cheatFree && pass;
        }

        private static bool ValidRoute(RouteBurdenObservation row)
        {
            return row != null && row.Feasible && row.BurdenScore > 0f && !string.IsNullOrWhiteSpace(row.ResourceFingerprint) &&
                   !string.IsNullOrWhiteSpace(row.ProtectedPartId);
        }

        private static bool ValidateAssets(O11Observation value, out string actual)
        {
            AssetBindingObservation[] rows = value == null ? Array.Empty<AssetBindingObservation>() : value.AssetBindings ?? Array.Empty<AssetBindingObservation>();
            string[] regions =
            {
                "region.coast.beach", "region.sea.shallows", "region.forest.grove", "region.ridge.highland",
                "region.cave.island", "region.cove.wreck", "region.ruins.relay"
            };
            string[] states = { "kim.idle", "kim.walk", "kim.search", "kim.ladder", "kim.swim" };
            bool regionPass = regions.All(id => rows.Any(row => ValidAsset(row, id, false)));
            bool statePass = states.All(id => rows.Any(row => ValidAsset(row, id, true)));
            actual = "rows=" + rows.Length + "; regions7=" + regionPass + "; kim5=" + statePass;
            return value != null && regionPass && statePass;
        }

        private static bool ValidAsset(AssetBindingObservation row, string id, bool clipRequired)
        {
            if (row == null || !Eq(row.StableId, id) || !row.RuntimeObserved || row.Placeholder || row.ReviewOnly ||
                string.IsNullOrWhiteSpace(row.Guid) || string.IsNullOrWhiteSpace(row.AssetPath)) return false;
            string resolved = AssetDatabase.GUIDToAssetPath(row.Guid);
            return !string.IsNullOrWhiteSpace(resolved) && Eq(resolved, row.AssetPath) && (!clipRequired || !string.IsNullOrWhiteSpace(row.ClipName));
        }

        private static void AddProduct(List<Check> checks, string id, string severity, string expected, bool passed,
            string actual, string reproduction, string greenCondition)
        {
            checks.Add(new Check
            {
                id = id,
                severity = severity,
                status = passed ? "PASS" : IsRedBaseline ? "EXPECTED_GAP" : "FAIL",
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                greenCondition = greenCondition
            });
        }

        private static void CaptureBaselineScreens(KimSurvivalPrototype prototype)
        {
            FieldInfo localizationField = prototype.GetType().GetField("localization", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo refresh = prototype.GetType().GetMethod(
                "RefreshAll",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            PrototypeLocalization localization = localizationField == null ? null : localizationField.GetValue(prototype) as PrototypeLocalization;
            if (localization == null || refresh == null) throw new InvalidOperationException("Live localization/refresh surface is unavailable.");
            foreach (string locale in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode })
            {
                localization.SetLocale(locale, false);
                refresh.Invoke(prototype, null);
                Canvas.ForceUpdateCanvases();
                foreach (Vector2Int size in new[] { new Vector2Int(1280, 800), new Vector2Int(1920, 1080) })
                {
                    string path = Path.Combine(EvidenceFolder, "O11-baseline-camp-" + locale + "-" + size.x + "x" + size.y + ".png");
                    prototype.CaptureVerificationPng(path, size.x, size.y);
                }
            }
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            refresh.Invoke(prototype, null);
        }

        private static void WriteReportText(Report report)
        {
            IEnumerable<string> lines = new[]
            {
                "O11=" + report.overall,
                "PRODUCT=" + report.productOverall,
                "INFRASTRUCTURE=" + report.infrastructureOverall,
                "BASELINE=" + report.baselineCommit,
                "OBSERVATION=" + (string.IsNullOrWhiteSpace(report.observationOwner) ? "MISSING" : report.observationOwner + "." + report.observationMethod),
                "PASS/EXPECTED_GAP/FAIL=" + report.passed + "/" + report.expectedGaps + "/" + report.failed
            }.Concat(report.checks.Select(check => check.id + "=" + check.status + " · " + check.actual));
            File.WriteAllText(Path.Combine(EvidenceFolder, "O11-product-report.txt"), string.Join(Environment.NewLine, lines) + Environment.NewLine, Utf8NoBom);
        }

        private static void WriteJson(string fileName, object value)
        {
            File.WriteAllText(Path.Combine(EvidenceFolder, fileName), JsonUtility.ToJson(value, true) + Environment.NewLine, Utf8NoBom);
        }

        private static bool Eq(string left, string right) { return string.Equals(left, right, StringComparison.Ordinal); }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return needles.Any(needle => value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void StopPlayMode()
        {
            if (tickAttached)
            {
                EditorApplication.update -= Tick;
                tickAttached = false;
            }
            EditorApplication.isPlaying = false;
        }

        private static void FinishAndExit()
        {
            bool passed = SessionState.GetBool(PassedKey, false);
            string message = SessionState.GetString(MessageKey, "No O11 result.");
            File.WriteAllText(Path.Combine(EvidenceFolder, "O11-play-infrastructure.txt"),
                (passed ? "PASS" : "FAIL") + Environment.NewLine + message + Environment.NewLine + DateTime.UtcNow.ToString("O") + Environment.NewLine,
                Utf8NoBom);
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(PassedKey);
            SessionState.EraseString(MessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("O11 infrastructure assertion failed: " + message);
        }
    }
}
