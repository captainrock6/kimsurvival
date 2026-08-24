using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParallelQA
{
    /// <summary>
    /// Wave 18 transition gate. It retains the fresh Wave 17 ID matrix while
    /// correcting three gate defects: serialized-schema privacy scope, live
    /// Play observation, and selected-only art connection states.
    /// </summary>
    public static class Wave18GreenTransitionHardeningRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave18.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave18.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave18.PlayMessage";
        private static readonly BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private static readonly BindingFlags SerializableFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private static readonly string[] BaselineFailureIds =
        {
            "W17-T01.day_band_boundaries",
            "W17-T02.early_escape_no_hardlock",
            "W17-R01.six_region_primary_alternative",
            "W17-R02.seed_forecast_hazard_pity_determinism",
            "W17-R03.eligible_search_hint3_guarantee5",
            "W17-R04.minimum_three_completable_paths",
            "W17-H02.rolling_calm_and_major_recovery",
            "W17-H03.atomic_retry_loss_and_keypart_protection",
            "W17-E02.smoke_radio_natural_interaction_routes",
            "W17-E03.raft_flare_beacon_data_only",
            "W17-O01.snapshot_and_private_log",
            "W17-N02.priority_tiebreak_and_hysteresis",
            "W17-P01.live_hazard_lifecycle",
            "W17-P02.live_smoke_radio_natural_paths",
            "W17-P03.live_terminal_priority_and_three_panels"
        };

        private static readonly string[] RegressionLockIds =
        {
            "W17-H01.three_hazard_four_phase_lifecycle",
            "W17-E01.five_escape_ids_and_two_axes",
            "W17-N01.ending_catalog_19_and_samples",
            "W17-A01.selection_gate_not_runtime_referenced",
            "W17-A02.selection_gate_not_runtime_referenced",
            "W17-A03.selection_gate_not_runtime_referenced",
            "W17-P04.ko_en_qps_1280_layout",
            "W17-P05.keyboard_synthetic_gamepad_parity"
        };

        private static readonly string[] StaticTransitionIds = BaselineFailureIds
            .Where(id => !id.StartsWith("W17-P", StringComparison.Ordinal) && id != "W17-O01.snapshot_and_private_log")
            .ToArray();

        private static readonly string[] StaticPassLocks =
        {
            "W17-H01.three_hazard_four_phase_lifecycle",
            "W17-E01.five_escape_ids_and_two_axes",
            "W17-N01.ending_catalog_19_and_samples"
        };

        private static readonly ArtCandidate[] ArtCandidates =
        {
            new ArtCandidate(
                "effect.survival-hazards.phase-silhouette-a",
                "job_20260823160305_ef04b0f3",
                "Assets/_Project/Art/Generated/effect/job_20260823160305_ef04b0f3/hazard-phase-atlas.png"),
            new ArtCandidate(
                "ui.escape-project-progress.route-signature-a",
                "job_20260823160324_1de3b748",
                "Assets/_Project/Art/Generated/ui_set/job_20260823160324_1de3b748/escape-project-route-signature-a-1280x800.png"),
            new ArtCandidate(
                "ui.ending-comic.triptych-a",
                "job_20260823160342_eceb3933",
                "Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png")
        };

        private static readonly HashSet<string> ForbiddenPrivateFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "account", "accountid", "useraccount", "userid", "email", "emailaddress",
            "ip", "ipaddress", "host", "hostname", "freetext", "rawtext", "playername", "personname"
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
        private sealed class PriorReport
        {
            public string runId;
            public string baselineCommit;
            public string infrastructureOverall;
            public Check[] checks;
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
            public int productFailed;
            public int infrastructureFailed;
            public int unverified;
            public string greenCompletionCondition;
            public string physicalGamepad = "UNVERIFIED";
            public string steamReadiness = "NOT_READY";
            public Check[] checks;
        }

        [Serializable]
        private sealed class SchemaEvidence
        {
            public string runId;
            public string baselineCommit;
            public string policy;
            public string[] snapshotSchemas;
            public string[] logSchemas;
            public string[] forbiddenFields;
            public string result;
        }

        [Serializable]
        private sealed class ArtEvidence
        {
            public string runId;
            public string baselineCommit;
            public string mode;
            public string[] selectedStableIds;
            public string[] selectedPrimaryGuids;
            public string[] selectedReferences;
            public string[] forbiddenReferences;
            public string[] allowlistReferences;
            public string result;
        }

        [Serializable]
        private sealed class HazardTrace
        {
            public int ownerInstanceId;
            public string ownerScene;
            public string ownerType;
            public string hazardId;
            public string eventKey;
            public string[] phases;
            public string[] resultCodes;
            public bool retryIdempotent;
            public string result;
        }

        [Serializable]
        private sealed class RouteTrace
        {
            public string routeId;
            public int ownerInstanceId;
            public string ownerScene;
            public string ownerType;
            public string method;
            public int interactionCount;
            public bool grantFlagFound;
            public bool grantUsed;
            public bool warpFlagFound;
            public bool warpUsed;
            public bool completed;
            public string traceFingerprint;
            public string result;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId;
            public string baselineCommit;
            public string discoveryPolicy;
            public HazardTrace[] hazardTraces;
            public RouteTrace smoke;
            public RouteTrace radio;
            public int ignoredStaticRouteFixtures;
            public string[] liveEndingObservations;
            public string[] joystickNames;
        }

        private sealed class PrivacyAudit
        {
            public bool Passed;
            public string Detail;
            public string[] SnapshotSchemas;
            public string[] LogSchemas;
            public string[] ForbiddenFields;
        }

        private sealed class ArtAudit
        {
            public bool Passed;
            public string Detail;
            public string Mode;
            public string[] SelectedGuids;
            public string[] SelectedReferences;
            public string[] ForbiddenReferences;
            public string[] AllowlistReferences;
        }

        private sealed class ArtCandidate
        {
            public readonly string StableId;
            public readonly string JobId;
            public readonly string PrimaryPath;

            public ArtCandidate(string stableId, string jobId, string primaryPath)
            {
                StableId = stableId;
                JobId = jobId;
                PrimaryPath = primaryPath;
            }
        }

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string EvidenceFolder { get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); } }
        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave18" : Sanitize(value);
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

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<Check> checks = new List<Check>();
            PriorReport prior = ReadPrior("wave17-edit-contracts.json");

            Infrastructure(checks, "W18-I01.fresh_wave17_edit", "fresh prerequisite", "P0",
                "The same RunId contains a parseable fresh Wave 17 Edit report with infrastructure PASS",
                delegate
                {
                    Require(prior != null && prior.checks != null && prior.infrastructureOverall == "PASS", "fresh Wave 17 Edit report missing or infrastructure FAIL");
                    Require(prior.runId == RunId && prior.baselineCommit == BaselineCommit, "fresh Wave 17 Edit identity mismatch");
                    return "runId=" + prior.runId + "; baseline=" + prior.baselineCommit;
                },
                "Run Invoke-Wave18GreenTransitionGate.ps1 with a fresh RunId.",
                "Assets/Editor/ParallelQA/Invoke-Wave18GreenTransitionGate.ps1");

            foreach (string id in StaticTransitionIds)
                CopyPriorProduct(checks, prior, id, "Wave 17 transition contract remains machine-readable and cannot disappear during hardening");
            foreach (string id in StaticPassLocks)
                CopyPriorProduct(checks, prior, id, "Previously passing public-data contract remains locked");

            PrivacyAudit privacy = AuditSerializedPrivacySchemas();
            Product(checks, "W17-O01.snapshot_and_private_log", "serialized persistence/privacy", "P0",
                "Serializable game snapshot and event-log schemas preserve stable campaign fields and contain no account/email/IP/host/free-text fields",
                privacy.Passed, privacy.Detail,
                "Enumerate non-Unity [Serializable] runtime schemas and inspect only fields Unity serializes; add an explicit PII field to confirm a failing mutation.",
                "runtime snapshot/log schema selected by the implementation owner");

            ArtAudit art = AuditArtConnection();
            for (int index = 0; index < ArtCandidates.Length; index++)
            {
                ArtCandidate candidate = ArtCandidates[index];
                Product(checks, "W17-A0" + (index + 1) + ".selection_gate_not_runtime_referenced", "selected-only art", "P0",
                    "Current explicit-adopted/unconnected state or future exact-three selected-only connected state is valid; review boards and unselected files/GUIDs never connect",
                    art.Passed, candidate.StableId + "; " + art.Detail,
                    "Search Runtime, Scenes, and Addressables for all selected and unselected package tokens and GUIDs.",
                    "Docs/Art/Wave17/wave17-adoption-record.json and product-owned connection manifest selected by the implementation owner");
            }

            Unverified(checks, "W17-HW01.physical_gamepad", "hardware", "P1",
                "A person completes both natural routes on a physically connected gamepad",
                "No physical device evidence is created by this automated gate.",
                "Run the manual physical gamepad checklist with device identity and observer notes.",
                "Docs/QA manual hardware evidence");
            Unverified(checks, "W17-S01.steam_release", "external release", "P0",
                "Steam App ID, SDK, depot, Input, Cloud, achievements, and partner permissions have independent evidence",
                "Steam remains NOT_READY and is never inferred from a Windows build.",
                "Run a separately authorized Steam release audit.",
                "Steamworks configuration and partner account evidence");

            WriteJson("wave18-privacy-schema-evidence.json", new SchemaEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                policy = "Only fields serialized by non-Unity [Serializable] snapshot/log schemas are checked. UnityEngine.Object.name and generic event_name are not PII.",
                snapshotSchemas = privacy.SnapshotSchemas,
                logSchemas = privacy.LogSchemas,
                forbiddenFields = privacy.ForbiddenFields,
                result = privacy.Passed ? "PASS" : "FAIL"
            });
            WriteJson("wave18-art-connection-evidence.json", new ArtEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                mode = art.Mode,
                selectedStableIds = ArtCandidates.Select(value => value.StableId).ToArray(),
                selectedPrimaryGuids = art.SelectedGuids,
                selectedReferences = art.SelectedReferences,
                forbiddenReferences = art.ForbiddenReferences,
                allowlistReferences = art.AllowlistReferences,
                result = art.Passed ? "PASS" : "FAIL"
            });
            WriteReport("wave18-edit-contracts", "Wave 18 green-transition Edit hardening", started, checks);
        }

        public static void RunPlayContracts()
        {
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "Wave 18 Play runner did not complete");
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
            if (!EditorApplication.isPlaying) return;
            playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
            playTimeoutAt = EditorApplication.timeSinceStartup + 30d;
            if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 30d;
                if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
            }
            else if (state == PlayModeStateChange.EnteredEditMode) FinishPlayContracts();
        }

        private static void PlayTick()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup > playTimeoutAt)
            {
                WritePlayInfrastructureFailure(new TimeoutException("Wave 18 Play fixture timed out."));
                StopPlayContracts();
                return;
            }
            if (EditorApplication.timeSinceStartup < playEarliestRunTime) return;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }

            try
            {
                DateTime started = DateTime.UtcNow;
                PriorReport prior = ReadPrior("wave17-play-contracts.json");
                if (prior == null || prior.checks == null || prior.infrastructureOverall != "PASS")
                    throw new InvalidOperationException("Fresh Wave 17 Play report is missing or infrastructure FAIL.");
                if (prior.runId != RunId || prior.baselineCommit != BaselineCommit)
                    throw new InvalidOperationException("Fresh Wave 17 Play identity mismatch.");

                MonoBehaviour[] live = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                    .Where(IsLiveProductBehaviour).ToArray();
                if (live.Length == 0) throw new InvalidOperationException("No live KimSurvival MonoBehaviour exists in the loaded Play scene.");

                HazardTrace[] hazards = ObserveLiveHazards(live);
                RouteTrace smoke = ObserveLiveRoute(live, "escape.smoke");
                RouteTrace radio = ObserveLiveRoute(live, "escape.radio");
                int ignoredStatic = CountStaticRouteFixtures();
                string[] endings = ObserveLiveEndingPriority(live);
                bool endingPass = endings.Any(IsStrictEndingPriorityObservation);

                List<Check> checks = new List<Check>();
                Infrastructure(checks, "W18-I02.actual_play_scene", "Play infrastructure", "P0",
                    "The runner observes active KimSurvival components with valid Scene instance IDs",
                    delegate { return "liveProductComponents=" + live.Length + "; scene=" + live[0].gameObject.scene.path; },
                    "Run the Wave 18 Play execute method through the PowerShell entry point.",
                    ScenePath);
                Product(checks, "W17-P01.live_hazard_lifecycle", "actual Play hazard", "P0",
                    "All three hazard stable IDs traverse telegraph, occurrence, mitigation, recovery on actual Play objects and retry idempotently",
                    hazards.Length == 3 && hazards.All(value => value.result == "PASS"),
                    hazards.Length == 0 ? "no compatible live hazard interaction surface" : string.Join(" | ", hazards.Select(DescribeHazard).ToArray()),
                    "Load the prototype Scene in Play Mode and invoke the public phase-shaped methods on the live owner while observing its state collection.",
                    "live hazard interaction owner selected by the implementation owner");
                Product(checks, "W17-P02.live_smoke_radio_natural_paths", "actual Play escape", "P0",
                    "Smoke and radio produce distinct stable-ID interaction traces from live Play owners with grant=false, warp=false, and terminal completion",
                    IsStrictRoute(smoke, "escape.smoke") && IsStrictRoute(radio, "escape.radio") && smoke.traceFingerprint != radio.traceFingerprint,
                    "smoke=" + DescribeRoute(smoke) + "; radio=" + DescribeRoute(radio) + "; ignoredStaticFixtures=" + ignoredStatic,
                    "Expose or drive separate live interaction traces for smoke and radio; static Edit fixtures are deliberately ignored.",
                    "live smoke/radio interaction owner selected by the implementation owner");
                Product(checks, "W17-P03.live_terminal_priority_and_three_panels", "actual Play ending", "P0",
                    "An actual Play object proves early-escape priority, Day 50 no-escape resolution, deterministic tie-break, and exactly three panels",
                    endingPass,
                    endings.Length == 0 ? "no compatible live ending-priority observation" : string.Join(" | ", endings),
                    "Observe both terminal scenarios through a public instance result on an active Scene object, then inspect the three-panel presentation.",
                    "live ending/terminal owner selected by the implementation owner");
                CopyPriorProduct(checks, prior, "W17-P04.ko_en_qps_1280_layout", "Fresh ko/en/qps-long 1280x800 three-panel layout remains locked");
                CopyPriorProduct(checks, prior, "W17-P05.keyboard_synthetic_gamepad_parity", "Fresh keyboard/synthetic-gamepad semantic state remains locked");

                WriteJson("wave18-play-observation-evidence.json", new PlayEvidence
                {
                    runId = RunId,
                    baselineCommit = BaselineCommit,
                    discoveryPolicy = "Active KimSurvival Scene objects, public method/result shape, stable IDs, and observed state only. Static Edit fixtures cannot satisfy P01-P03.",
                    hazardTraces = hazards,
                    smoke = smoke,
                    radio = radio,
                    ignoredStaticRouteFixtures = ignoredStatic,
                    liveEndingObservations = endings,
                    joystickNames = Input.GetJoystickNames() ?? Array.Empty<string>()
                });
                WriteReport("wave18-play-contracts", "Wave 18 green-transition Play hardening", started, checks);
                SessionState.SetBool(PlayExitPassKey, true);
                SessionState.SetString(PlayMessageKey, "Wave 18 Play evidence completed");
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static PrivacyAudit AuditSerializedPrivacySchemas()
        {
            List<string> snapshots = new List<string>();
            List<string> logs = new List<string>();
            List<string> forbidden = new List<string>();
            foreach (Type type in typeof(GameSession).Assembly.GetTypes().Where(type => type.Namespace == "KimSurvival"))
            {
                if (!type.IsSerializable || typeof(UnityEngine.Object).IsAssignableFrom(type)) continue;
                FieldInfo[] fields = type.GetFields(SerializableFields).Where(IsUnitySerializedField).ToArray();
                if (fields.Length == 0) continue;
                string[] names = fields.Select(field => Normalize(field.Name)).ToArray();
                string descriptor = type.FullName + "{" + string.Join(",", fields.Select(field => field.Name).OrderBy(value => value).ToArray()) + "}";
                string typeToken = Normalize(type.Name);
                if (typeToken.Contains("snapshot") || typeToken.Contains("fingerprint")) snapshots.Add(descriptor);
                if (typeToken.Contains("eventrecord") || typeToken.Contains("logrecord") || typeToken.Contains("telemetryrecord")) logs.Add(descriptor);
                foreach (FieldInfo field in fields)
                {
                    string normalized = Normalize(field.Name);
                    if (ForbiddenPrivateFields.Contains(normalized)) forbidden.Add(type.FullName + "." + field.Name);
                }
            }

            bool snapshotPass = snapshots.Any(value =>
            {
                string token = Normalize(value);
                return token.Contains("seed") && token.Contains("region") && token.Contains("hazard") &&
                       token.Contains("project") && token.Contains("behavior");
            });
            bool logPass = logs.Any(value =>
            {
                string token = Normalize(value);
                return token.Contains("sequence") && token.Contains("runid") && token.Contains("utc") &&
                       token.Contains("eventname") && token.Contains("locale") && token.Contains("inputdevice") &&
                       token.Contains("runseed") && token.Contains("regionid") && token.Contains("hazardid") &&
                       token.Contains("projectid") && token.Contains("escapeid") && token.Contains("endingid") &&
                       token.Contains("behaviorscoreids") && token.Contains("resultcode");
            });
            bool passed = snapshotPass && logPass && forbidden.Count == 0;
            return new PrivacyAudit
            {
                Passed = passed,
                SnapshotSchemas = snapshots.OrderBy(value => value).ToArray(),
                LogSchemas = logs.OrderBy(value => value).ToArray(),
                ForbiddenFields = forbidden.OrderBy(value => value).ToArray(),
                Detail = "snapshotSchema=" + snapshotPass + "; logSchema=" + logPass + "; forbiddenSerializedFields=" +
                         (forbidden.Count == 0 ? "none" : string.Join(",", forbidden.ToArray())) +
                         "; generic UnityEngine.Object.name excluded by policy"
            };
        }

        private static bool IsUnitySerializedField(FieldInfo field)
        {
            return !field.IsStatic && !field.IsNotSerialized &&
                   (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length > 0);
        }

        private static ArtAudit AuditArtConnection()
        {
            string adoptionPath = Path.Combine(ProjectRoot, "Docs", "Art", "Wave17", "wave17-adoption-record.json");
            if (!File.Exists(adoptionPath))
                return FailedArt("MISSING_ADOPTION_RECORD", "explicit adoption record missing");
            string adoption = File.ReadAllText(adoptionPath);
            bool adopted = ArtCandidates.All(value => adoption.Contains(value.StableId) && adoption.Contains(value.JobId)) &&
                           adoption.Contains("\"decisionSource\": \"explicit-user-message\"") &&
                           adoption.Contains("\"decision\": \"adopted\"");

            List<string> selectedNeedles = new List<string>();
            List<string> selectedGuids = new List<string>();
            List<string> forbiddenNeedles = new List<string>();
            foreach (ArtCandidate candidate in ArtCandidates)
            {
                selectedNeedles.Add(candidate.StableId);
                selectedNeedles.Add(Path.GetFileName(candidate.PrimaryPath));
                string selectedGuid = AssetDatabase.AssetPathToGUID(candidate.PrimaryPath);
                selectedGuids.Add(selectedGuid);
                if (!string.IsNullOrWhiteSpace(selectedGuid)) selectedNeedles.Add(selectedGuid);

                string folder = Path.GetDirectoryName(candidate.PrimaryPath).Replace('\\', '/');
                foreach (string assetGuid in AssetDatabase.FindAssets(string.Empty, new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (string.Equals(path, candidate.PrimaryPath, StringComparison.OrdinalIgnoreCase)) continue;
                    string extension = Path.GetExtension(path).ToLowerInvariant();
                    if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".svg") continue;
                    forbiddenNeedles.Add(Path.GetFileName(path));
                    forbiddenNeedles.Add(assetGuid);
                }
            }

            string[] selectedReferences = FindProductReferences(selectedNeedles.Distinct(StringComparer.OrdinalIgnoreCase));
            string[] forbiddenReferences = FindProductReferences(forbiddenNeedles.Distinct(StringComparer.OrdinalIgnoreCase));
            string[] allowlistReferences = FindAllowlistReferences(ArtCandidates.Select(value => value.StableId).Concat(selectedGuids));
            bool recordConnected = Regex.IsMatch(adoption, "\\\"runtimeConnected\\\"\\s*:\\s*true", RegexOptions.IgnoreCase);
            bool noSelectedReferences = selectedReferences.Length == 0;
            bool allSelectedReferenced = ArtCandidates.All(candidate => selectedReferences.Any(value =>
                value.IndexOf(candidate.StableId, StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf(Path.GetFileName(candidate.PrimaryPath), StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrWhiteSpace(AssetDatabase.AssetPathToGUID(candidate.PrimaryPath)) &&
                 value.IndexOf(AssetDatabase.AssetPathToGUID(candidate.PrimaryPath), StringComparison.OrdinalIgnoreCase) >= 0)));
            bool connected = recordConnected || selectedReferences.Length > 0;
            bool passed = adopted && forbiddenReferences.Length == 0 &&
                          ((!connected && noSelectedReferences) ||
                           (connected && allSelectedReferenced && allowlistReferences.Length > 0));
            string mode = connected ? "SELECTED_THREE_CONNECTED" : "EXPLICIT_ADOPTED_UNCONNECTED";
            return new ArtAudit
            {
                Passed = passed,
                Mode = mode,
                SelectedGuids = selectedGuids.ToArray(),
                SelectedReferences = selectedReferences,
                ForbiddenReferences = forbiddenReferences,
                AllowlistReferences = allowlistReferences,
                Detail = "mode=" + mode + "; adopted=" + adopted + "; selectedRefs=" + selectedReferences.Length +
                         "; allowlistRefs=" + allowlistReferences.Length + "; forbiddenRefs=" + forbiddenReferences.Length
            };
        }

        private static ArtAudit FailedArt(string mode, string detail)
        {
            return new ArtAudit
            {
                Passed = false, Mode = mode, Detail = detail,
                SelectedGuids = Array.Empty<string>(), SelectedReferences = Array.Empty<string>(),
                ForbiddenReferences = Array.Empty<string>(), AllowlistReferences = Array.Empty<string>()
            };
        }

        private static string[] FindProductReferences(IEnumerable<string> needles)
        {
            string[] roots =
            {
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime"),
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scenes"),
                Path.Combine(ProjectRoot, "Assets", "AddressableAssetsData")
            };
            string[] values = needles.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
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
                    foreach (string needle in values.Where(value => text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0))
                        results.Add(needle + "@" + Relative(file));
                }
            }
            return results.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static string[] FindAllowlistReferences(IEnumerable<string> selectedTokens)
        {
            HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] tokens = selectedTokens.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            foreach (string root in new[]
            {
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime"),
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scenes"),
                Path.Combine(ProjectRoot, "Assets", "AddressableAssetsData")
            })
            {
                if (!Directory.Exists(root)) continue;
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (!new[] { ".cs", ".unity", ".asset", ".json" }.Contains(extension)) continue;
                    string text;
                    try { text = File.ReadAllText(file); } catch { continue; }
                    if (text.IndexOf("allowlist", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string[] found = tokens.Where(value => text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0).Distinct().ToArray();
                    if (found.Length == ArtCandidates.Length) results.Add(Relative(file) + "{" + string.Join(",", found) + "}");
                }
            }
            return results.OrderBy(value => value).ToArray();
        }

        private static HazardTrace[] ObserveLiveHazards(IEnumerable<MonoBehaviour> live)
        {
            MonoBehaviour owner = live.FirstOrDefault(value => FindHazardMethods(value) != null);
            if (owner == null) return Array.Empty<HazardTrace>();
            MethodInfo[] methods = FindHazardMethods(owner);
            MethodInfo reset = owner.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.GetParameters().Length == 0 && method.ReturnType == typeof(void) &&
                                          method.Name.IndexOf("reset", StringComparison.OrdinalIgnoreCase) >= 0);
            if (reset != null) reset.Invoke(owner, null);

            List<HazardTrace> traces = new List<HazardTrace>();
            string[] ids = { "hazard.injury", "hazard.disaster", "hazard.food-theft" };
            for (int index = 0; index < ids.Length; index++)
            {
                string id = ids[index];
                string key = "qa.wave18." + id;
                List<string> phases = new List<string>();
                List<string> codes = new List<string>();
                bool calls = InvokeBool(methods[0], owner, key, id, 11 + index) && CaptureHazardState(owner, key, phases, codes) &&
                             InvokeBool(methods[1], owner, key) && CaptureHazardState(owner, key, phases, codes) &&
                             InvokeBool(methods[2], owner, key) && CaptureHazardState(owner, key, phases, codes) &&
                             InvokeBool(methods[3], owner, key) && CaptureHazardState(owner, key, phases, codes);
                string before = HazardMutationFingerprint(owner);
                bool retry = InvokeBool(methods[3], owner, key);
                string after = HazardMutationFingerprint(owner);
                bool sequence = phases.SequenceEqual(new[] { "Telegraph", "Occurrence", "Mitigation", "Recovery" });
                traces.Add(new HazardTrace
                {
                    ownerInstanceId = owner.GetInstanceID(),
                    ownerScene = owner.gameObject.scene.path,
                    ownerType = owner.GetType().FullName,
                    hazardId = id,
                    eventKey = key,
                    phases = phases.ToArray(),
                    resultCodes = codes.ToArray(),
                    retryIdempotent = retry && before == after,
                    result = calls && sequence && retry && before == after ? "PASS" : "FAIL"
                });
            }
            return traces.ToArray();
        }

        private static MethodInfo[] FindHazardMethods(MonoBehaviour behaviour)
        {
            MethodInfo[] methods = behaviour.GetType().GetMethods(PublicInstance);
            MethodInfo telegraph = methods.FirstOrDefault(method => method.ReturnType == typeof(bool) &&
                Contains(method.Name, "telegraph") && Parameters(method, typeof(string), typeof(string), typeof(int)));
            MethodInfo occurrence = methods.FirstOrDefault(method => method.ReturnType == typeof(bool) &&
                Contains(method.Name, "hazard") && ContainsAny(method.Name, "occurrence", "resolve") && Parameters(method, typeof(string)));
            MethodInfo mitigation = methods.FirstOrDefault(method => method.ReturnType == typeof(bool) &&
                ContainsAny(method.Name, "mitigate", "mitigation") && Parameters(method, typeof(string)));
            MethodInfo recovery = methods.FirstOrDefault(method => method.ReturnType == typeof(bool) &&
                ContainsAny(method.Name, "recover", "recovery") && Parameters(method, typeof(string)));
            return telegraph != null && occurrence != null && mitigation != null && recovery != null
                ? new[] { telegraph, occurrence, mitigation, recovery }
                : null;
        }

        private static bool CaptureHazardState(object owner, string eventKey, List<string> phases, List<string> codes)
        {
            object state = FindObjectByMemberValue(owner, "EventKey", eventKey, 3, new HashSet<object>());
            if (state == null) return false;
            phases.Add(Convert.ToString(ReadMember(state, "Phase")));
            codes.Add(Convert.ToString(ReadMember(state, "ResultCode")));
            return true;
        }

        private static object FindObjectByMemberValue(object value, string memberName, string expected, int depth, HashSet<object> visited)
        {
            if (value == null || depth < 0 || value is string) return null;
            Type type = value.GetType();
            if (!type.IsValueType && !visited.Add(value)) return null;
            object direct = ReadMember(value, memberName);
            if (direct != null && string.Equals(Convert.ToString(direct), expected, StringComparison.Ordinal)) return value;
            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    object found = FindObjectByMemberValue(item, memberName, expected, depth - 1, visited);
                    if (found != null) return found;
                }
                return null;
            }
            foreach (PropertyInfo property in type.GetProperties(PublicInstance).Where(CanReadSafely).Take(32))
            {
                object nested;
                try { nested = property.GetValue(value, null); } catch { continue; }
                object found = FindObjectByMemberValue(nested, memberName, expected, depth - 1, visited);
                if (found != null) return found;
            }
            return null;
        }

        private static string HazardMutationFingerprint(object owner)
        {
            List<string> values = new List<string>();
            foreach (string propertyName in new[] { "HazardDirector", "HazardLedger", "CampaignEvents" })
            {
                object value = ReadMember(owner, propertyName);
                values.Add(propertyName + "=" + CompactDescribe(value, 3));
            }
            return string.Join("|", values.ToArray());
        }

        private static RouteTrace ObserveLiveRoute(IEnumerable<MonoBehaviour> live, string routeId)
        {
            foreach (MonoBehaviour owner in live)
            {
                foreach (MethodInfo method in owner.GetType().GetMethods(PublicInstance))
                {
                    if (method.ReturnType == typeof(void) || method.ReturnType == typeof(bool) || method.ReturnType == typeof(string)) continue;
                    if (!ContainsAny(method.Name, "natural", "interaction", "trace", "route", "escape")) continue;
                    if (!ReturnShapeHas(method.ReturnType, "trace", "interaction") ||
                        !ReturnShapeHas(method.ReturnType, "grant") || !ReturnShapeHas(method.ReturnType, "warp")) continue;
                    object[] arguments = BuildSemanticArguments(method.GetParameters(), routeId);
                    if (arguments == null) continue;
                    object result;
                    try { result = method.Invoke(owner, arguments); } catch { continue; }
                    RouteTrace trace = AuditRouteResult(owner, method, routeId, result);
                    if (trace != null) return trace;
                }
            }
            return new RouteTrace { routeId = routeId, result = "FAIL_NO_LIVE_INTERACTION_RESULT", traceFingerprint = string.Empty };
        }

        private static RouteTrace AuditRouteResult(MonoBehaviour owner, MethodInfo method, string routeId, object result)
        {
            if (result == null) return null;
            string observedId = FirstStringMember(result, "RouteStableId", "RouteId", "EscapeStableId", "EscapeId", "StableId");
            object traceValue = FirstMember(result, "InteractionTrace", "Interactions", "Trace", "ActionTrace");
            List<string> interactions = new List<string>();
            if (traceValue is IEnumerable enumerable && !(traceValue is string))
                foreach (object item in enumerable) interactions.Add(CompactDescribe(item, 3));
            bool foundGrant;
            bool grant = FirstBoolMember(result, out foundGrant, "UsedGrant", "GrantUsed", "Grant");
            bool foundWarp;
            bool warp = FirstBoolMember(result, out foundWarp, "UsedWarp", "WarpUsed", "Warp");
            bool foundComplete;
            bool complete = FirstBoolMember(result, out foundComplete, "TerminalComplete", "Completed", "Complete", "Success");
            string fingerprint = routeId + "|" + string.Join("|", interactions.ToArray());
            bool stableTrace = interactions.Count > 0 && interactions.Any(value => Regex.IsMatch(value, "[a-z]+\\.[a-z0-9._-]+", RegexOptions.IgnoreCase));
            bool passed = string.Equals(observedId, routeId, StringComparison.Ordinal) && stableTrace && foundGrant && !grant &&
                          foundWarp && !warp && foundComplete && complete;
            return new RouteTrace
            {
                routeId = routeId,
                ownerInstanceId = owner.GetInstanceID(),
                ownerScene = owner.gameObject.scene.path,
                ownerType = owner.GetType().FullName,
                method = method.Name,
                interactionCount = interactions.Count,
                grantFlagFound = foundGrant,
                grantUsed = grant,
                warpFlagFound = foundWarp,
                warpUsed = warp,
                completed = foundComplete && complete,
                traceFingerprint = fingerprint,
                result = passed ? "PASS" : "FAIL_CONTRACT"
            };
        }

        private static int CountStaticRouteFixtures()
        {
            return typeof(GameSession).Assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                .Count(method => ContainsAny(method.Name, "smoke", "radio", "route", "escape") &&
                                 ContainsAny(method.Name, "fixture", "probe", "verify"));
        }

        private static string[] ObserveLiveEndingPriority(IEnumerable<MonoBehaviour> live)
        {
            List<string> values = new List<string>();
            foreach (MonoBehaviour owner in live)
            {
                foreach (MethodInfo method in owner.GetType().GetMethods(PublicInstance))
                {
                    if (method.ReturnType == typeof(void) || method.ReturnType == typeof(bool) || method.ReturnType == typeof(string)) continue;
                    if (!ContainsAny(method.Name, "ending", "terminal", "priority")) continue;
                    object[] arguments = BuildSemanticArguments(method.GetParameters(), "ending.priority");
                    if (arguments == null) continue;
                    try
                    {
                        object result = method.Invoke(owner, arguments);
                        if (result != null)
                            values.Add("instance=" + owner.GetInstanceID() + ";scene=" + owner.gameObject.scene.path + ";type=" +
                                       owner.GetType().FullName + ";method=" + method.Name + ";result=" + CompactDescribe(result, 5));
                    }
                    catch { }
                }
            }
            return values.Take(32).ToArray();
        }

        private static bool IsStrictEndingPriorityObservation(string value)
        {
            string token = Normalize(value);
            return token.Contains("early") && token.Contains("escape") && token.Contains("day50") &&
                   token.Contains("noescape") && ContainsAny(token, "escapewins", "earlyescapeprioritytrue") &&
                   token.Contains("deterministic") && token.Contains("tiebreak") &&
                   ContainsAny(token, "panelcount3", "panels3", "threepanels");
        }

        private static bool IsStrictRoute(RouteTrace trace, string expectedId)
        {
            return trace != null && trace.result == "PASS" && trace.routeId == expectedId && trace.ownerInstanceId != 0 &&
                   !string.IsNullOrWhiteSpace(trace.ownerScene) && trace.interactionCount > 0 && trace.grantFlagFound &&
                   !trace.grantUsed && trace.warpFlagFound && !trace.warpUsed && trace.completed;
        }

        private static bool IsLiveProductBehaviour(MonoBehaviour behaviour)
        {
            return behaviour != null && behaviour.gameObject.scene.IsValid() && behaviour.gameObject.scene.isLoaded &&
                   behaviour.GetType().Namespace == "KimSurvival";
        }

        private static object[] BuildSemanticArguments(ParameterInfo[] parameters, string stableId)
        {
            object[] values = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                Type type = parameters[index].ParameterType;
                if (type == typeof(string)) values[index] = stableId;
                else if (type == typeof(int)) values[index] = 1818;
                else if (type == typeof(bool)) values[index] = false;
                else if (type.IsEnum) values[index] = Enum.GetValues(type).GetValue(0);
                else if (parameters[index].HasDefaultValue) values[index] = parameters[index].DefaultValue;
                else return null;
            }
            return values;
        }

        private static bool ReturnShapeHas(Type type, params string[] tokens)
        {
            return type.GetMembers(PublicInstance).Any(member => tokens.Any(token => Contains(member.Name, token)));
        }

        private static bool InvokeBool(MethodInfo method, object owner, params object[] arguments)
        {
            try { return (bool)method.Invoke(owner, arguments); } catch { return false; }
        }

        private static bool Parameters(MethodInfo method, params Type[] expected)
        {
            return method.GetParameters().Select(value => value.ParameterType).SequenceEqual(expected);
        }

        private static object FirstMember(object value, params string[] names)
        {
            foreach (string name in names)
            {
                object member = ReadMember(value, name);
                if (member != null) return member;
            }
            return null;
        }

        private static string FirstStringMember(object value, params string[] names)
        {
            object member = FirstMember(value, names);
            return member == null ? string.Empty : Convert.ToString(member);
        }

        private static bool FirstBoolMember(object value, out bool found, params string[] names)
        {
            foreach (string name in names)
            {
                object member = ReadMember(value, name);
                if (member is bool)
                {
                    found = true;
                    return (bool)member;
                }
            }
            found = false;
            return false;
        }

        private static object ReadMember(object value, string name)
        {
            if (value == null) return null;
            Type type = value.GetType();
            PropertyInfo property = type.GetProperty(name, PublicInstance | BindingFlags.IgnoreCase);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                try { return property.GetValue(value, null); } catch { }
            }
            FieldInfo field = type.GetField(name, PublicInstance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                try { return field.GetValue(value); } catch { }
            }
            return null;
        }

        private static bool CanReadSafely(PropertyInfo property)
        {
            return property.CanRead && property.GetIndexParameters().Length == 0 && property.Name != "Item";
        }

        private static string CompactDescribe(object value, int depth)
        {
            if (value == null) return "null";
            if (value is string) return (string)value;
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal) return Convert.ToString(value);
            if (depth <= 0) return type.Name;
            if (value is IEnumerable enumerable)
            {
                List<string> items = new List<string>();
                foreach (object item in enumerable) { items.Add(CompactDescribe(item, depth - 1)); if (items.Count >= 32) break; }
                return "[" + string.Join(",", items.ToArray()) + "]";
            }
            List<string> members = new List<string>();
            foreach (FieldInfo field in type.GetFields(PublicInstance).Take(24))
            {
                try { members.Add(field.Name + "=" + CompactDescribe(field.GetValue(value), depth - 1)); } catch { }
            }
            foreach (PropertyInfo property in type.GetProperties(PublicInstance).Where(CanReadSafely).Take(24))
            {
                try { members.Add(property.Name + "=" + CompactDescribe(property.GetValue(value, null), depth - 1)); } catch { }
            }
            string result = type.Name + "{" + string.Join(";", members.ToArray()) + "}";
            return result.Length <= 1800 ? result : result.Substring(0, 1800) + "...[truncated]";
        }

        private static string DescribeHazard(HazardTrace value)
        {
            return value.hazardId + "@" + value.ownerInstanceId + "[" + string.Join(",", value.phases) + "] retry=" + value.retryIdempotent;
        }

        private static string DescribeRoute(RouteTrace value)
        {
            if (value == null) return "missing";
            return value.result + "@" + value.ownerInstanceId + "/" + value.routeId + "/interactions=" + value.interactionCount +
                   "/grant=" + (value.grantFlagFound ? value.grantUsed.ToString() : "MISSING") +
                   "/warp=" + (value.warpFlagFound ? value.warpUsed.ToString() : "MISSING") + "/complete=" + value.completed;
        }

        private static PriorReport ReadPrior(string fileName)
        {
            string path = Path.Combine(EvidenceFolder, fileName);
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<PriorReport>(File.ReadAllText(path)); } catch { return null; }
        }

        private static void CopyPriorProduct(List<Check> checks, PriorReport prior, string id, string expected)
        {
            Check source = prior == null || prior.checks == null ? null : prior.checks.FirstOrDefault(value => value.id == id);
            if (source == null)
            {
                checks.Add(NewCheck(id, "fresh Wave 17 transition", "INFRA_FAIL", "INFRASTRUCTURE", "P0", expected,
                    "fresh Wave 17 check missing", "Run the Wave 18 entry point so it creates Wave 17 evidence first.",
                    "Assets/Editor/ParallelQA/Invoke-Wave18GreenTransitionGate.ps1"));
                return;
            }
            Product(checks, id, source.matrix, source.severity, expected, source.status == "PASS",
                "freshWave17Status=" + source.status + "; " + Trim(source.actual), source.reproduction, source.recommendedFiles);
        }

        private static void Product(List<Check> checks, string id, string matrix, string severity, string expected,
            bool passed, string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, passed ? "PASS" : "FAIL", passed ? "NONE" : "PRODUCT_FAILURE",
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
            return new Check
            {
                id = id, matrix = matrix, status = status, classification = classification, severity = severity,
                expected = expected, actual = actual, reproduction = reproduction, recommendedFiles = files
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
                productFailed = checks.Count(value => value.status == "FAIL"),
                infrastructureFailed = checks.Count(value => value.status == "INFRA_FAIL"),
                unverified = checks.Count(value => value.status == "UNVERIFIED"),
                greenCompletionCondition = "Fresh Wave 15/16 GREEN, infrastructure locks PASS, all 15 transition IDs and eight regression locks present, and zero product FAIL; hardware/Steam remain external.",
                checks = checks.ToArray()
            };
            report.productOverall = report.productFailed == 0 ? "PASS" : "FAIL";
            report.infrastructureOverall = report.infrastructureFailed == 0 ? "PASS" : "FAIL";
            report.overall = report.infrastructureOverall == "FAIL" ? "FAIL" : report.productOverall == "PASS" ? "GREEN" : "RED";
            WriteJson(stem + ".json", report);
            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Unity: " + Application.unityVersion);
            text.AppendLine("Overall/Product/Infrastructure: " + report.overall + "/" + report.productOverall + "/" + report.infrastructureOverall);
            text.AppendLine("PASS/FAIL/INFRA_FAIL/UNVERIFIED: " + report.passed + "/" + report.productFailed + "/" + report.infrastructureFailed + "/" + report.unverified);
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
                NewCheck("W18-I99.play_runner", "infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Wave 18 Play runner emits parseable evidence", exception.ToString(),
                    "Run the Play execute method outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave18GreenTransitionHardeningRunner.cs")
            };
            WriteReport("wave18-play-contracts", "Wave 18 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: " + exception);
        }

        private static void StopPlayContracts() { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; }

        private static void FinishPlayContracts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }
            bool passed = SessionState.GetBool(PlayExitPassKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing Wave 18 Play result");
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

        private static string Relative(string path)
        {
            return path.Substring(ProjectRoot.Length + 1).Replace('\\', '/');
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(value ?? string.Empty, "[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase).ToLowerInvariant();
        }

        private static bool Contains(string value, string token)
        {
            return value != null && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            return tokens.Any(token => Contains(value, token));
        }

        private static string Trim(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "missing";
            return value.Length <= 1200 ? value : value.Substring(0, 1200) + "...[truncated]";
        }

        private static string Sanitize(string value)
        {
            return Regex.Replace(value, "[^A-Za-z0-9._-]", "_");
        }
    }
}
