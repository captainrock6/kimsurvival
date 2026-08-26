using System;
using System.Collections.Generic;
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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Wave 12 current contract for the adopted compact-a contextual prompt,
    /// standard Day 50 flow, provisional Game Jam Day 20 flow, and early-escape
    /// priority. Runner/build failures remain INFRA_FAIL in orchestration.
    /// </summary>
    public static class Wave12FiveDayCompactUiGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PromptAssetId = "ui.camp-contextual-interaction";
        private const string PromptJobId = "job_20260823073121_f5da3402";
        private const string CompactAPath = "Assets/_Project/Art/Generated/ui_set/job_20260823073121_f5da3402/compact-a.png";
        private const string CompactAGuid = "070048b5b443d5d4a9c757c871873eb3";
        private const string CompactBGuid = "5ad6ee05d5b5e774cb0d4cf95d990d1e";
        private const string CompactCGuid = "98f737e76e45ef34bbaedf8f954a46fa";
        private const string QpsLong = "qps-long";
        private const string PlayRunningKey = "ParallelQA.Wave12.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave12.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave12.PlayMessage";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
            public int expectedFailed;
            public int productFailed;
            public int infrastructureFailed;
            public int unverified;
            public Check[] checks;
        }

        [Serializable]
        private sealed class Evidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string sessionFlow;
            public string adoptedPackage;
            public string runtimeFrame;
            public string inputLocaleIndependence;
            public string captureLayout;
            public string freshWave11Layout;
            public string freshWave3Visual;
            public string[] screenshots;
            public string[] joystickNames;
            public string physicalGamepad = "UNVERIFIED";
        }

        private sealed class Observation
        {
            public bool Passed;
            public string Detail;
        }

        private sealed class CaptureObservation
        {
            public bool Passed;
            public string Detail;
            public string[] Screenshots = Array.Empty<string>();
        }

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string EvidenceFolder { get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); } }
        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave12" : Sanitize(value);
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
            Evidence evidence = NewEvidence();

            Observation sessionFlow = ObserveSessionFlowProfiles();
            evidence.sessionFlow = sessionFlow.Detail;
            Product(checks, "W12-D01.session_flow_profiles", "session flow profiles", "P0",
                "The standard profile settles on Day 50, the provisional Game Jam profile settles on Day 20 within its 15..20 tuning range, and early escape wins on Day 1 and at settlement",
                () => RequireDetail(sessionFlow.Passed, sessionFlow.Detail),
                "Capture PrototypeSessionFlowProfileCatalog verification and complete the signal on Day 1 in a fresh provisional-profile GameSession.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");

            Observation package = ObserveAdoptedPackage();
            evidence.adoptedPackage = package.Detail;
            Product(checks, "W12-A01.compact_a_adopted_package", "adopted art contract", "P0",
                "The engine-ready contextual prompt resolves to compact-a GUID 070048..., manifest border L70/R30/T12/B12, and B/C have no runtime reference",
                () => RequireDetail(package.Passed, package.Detail),
                "Audit Forge's adopted package read-only, the compact-a meta GUID, the 9-slice manifest, and enabled runtime dependency text.",
                ".forge/assets.json; .forge/feedback.json; Assets/_Project/Art/Generated/ui_set/job_20260823073121_f5da3402/**");

            Observation staticRuntime = ObserveStaticRuntimeConnection();
            Product(checks, "W12-A02.compact_a_static_runtime_reference", "runtime art connection", "P0",
                "Runtime/scene dependencies reference compact-a by its stable GUID/path and do not reference compact-b or compact-c",
                () => RequireDetail(staticRuntime.Passed, staticRuntime.Detail),
                "Search the enabled scene, Runtime source, Resources, and Addressables settings for the adopted A GUID/path and B/C GUIDs.",
                "Assets/_Project/Scripts/Runtime/**; Assets/_Project/Scenes/**; Assets/AddressableAssetsData/**");

            WriteJson("wave12-edit-evidence.json", evidence);
            WriteReport("wave12-edit-contracts", "Wave 12 session-profile/compact-a Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Wave 12 Play contracts did not complete.");
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
                WriteInfrastructureFailure(new TimeoutException("Timed out waiting for the Wave 12 scene."));
                StopPlayContracts();
                return;
            }
            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;

            DateTime started = DateTime.UtcNow;
            List<Check> checks = new List<Check>();
            Evidence evidence = NewEvidence();
            try
            {
                Observation runtimeFrame = ObserveRuntimeFrame(prototype);
                evidence.runtimeFrame = runtimeFrame.Detail;
                Product(checks, "W12-P01.compact_a_frame_and_glyph_split", "runtime prompt", "P0",
                    "The actual prompt uses compact-a as a sliced Image with border L70/R30/T12/B12 and separates the input glyph from TMP action text",
                    () => RequireDetail(runtimeFrame.Passed, runtimeFrame.Detail),
                    "Approach a direct slot, inspect the prompt Image/Sprite/GUID/type/border and its descendant glyph/action components.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scenes/KimSurvivalPrototype.unity");

                Observation independence = ObserveInputLocaleIndependence(prototype);
                evidence.inputLocaleIndependence = independence.Detail;
                Product(checks, "W12-P02.device_locale_target_independence", "input/localization state", "P0",
                    "Synthetic device changes update only glyph semantics and locale changes update only text; neither changes the direct-slot target or progress state",
                    () => RequireDetail(independence.Passed, independence.Detail),
                    "Latch slot.start.upper, switch keyboard→synthetic gamepad, then ko→en→qps-long while fingerprinting target, day, resources, signal, and bag.",
                    "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                CaptureObservation captures = CaptureStateMatrix(prototype);
                evidence.captureLayout = captures.Detail;
                evidence.screenshots = captures.Screenshots;
                Product(checks, "W12-P03.compact_a_locale_capture_layout", "1280x800 visual", "P1",
                    "ko/en/qps-long far/near/popup/direct-slot captures are 1280x800; compact-a prompt is 512×48px, uses source fontMin >=18, stays 12px below narration and clear of world silhouettes/path, with active TMP overflow 0",
                    () => RequireDetail(captures.Passed, captures.Detail),
                    "Open all twelve Wave 12 captures at 1:1 and compare the recorded prompt/narration Rects and overflow counts.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                Observation wave11Layout = ObserveFreshWave11Layout();
                evidence.freshWave11Layout = wave11Layout.Detail;
                Infrastructure(checks, "W12-I01.fresh_wave11_canvas_layout", "QA infrastructure", "P0",
                    "Fresh Wave 11 evidence reports direct slot walkingPathClear 3/3 with actual prompt/player/target/walking Rects and no failure reasons",
                    () => RequireDetail(wave11Layout.Passed, wave11Layout.Detail),
                    "Run Wave11SlotDiscoveryGateRunner in the same RunId before Wave 12 and inspect its serialized Rect evidence.",
                    "Assets/Editor/ParallelQA/Wave11SlotDiscoveryGateRunner.cs");

                evidence.freshWave3Visual = Infrastructure(checks, "W12-I02.fresh_wave3_visual_report", "QA infrastructure", "P0",
                    "The current Play session generates a fresh Wave 3 visual report for this RunId before asset/release audit",
                    () => GenerateFreshWave3VisualEvidence(prototype),
                    "Run the Wave 12 Play contract and verify wave3-visual-gate.txt has this RunId and baseline SHA.",
                    "Assets/Editor/ParallelQA/ParallelQaRunner.cs; Assets/Editor/ParallelQA/Wave3VisualGate.cs");

                Observation currentVisual = ObserveFreshWave3NormalVisual();
                Product(checks, "W12-P04.current_normal_wave3_visual", "1280x800 regression", "P1",
                    "Fresh current hierarchy reports placement 4/4, exploration/swimming 4/4, normal search tray 16/16, and fresh-pity qps-long production scenes 37/37 PASS; protected-part trays remain a separate Wave B contract",
                    () => RequireDetail(currentVisual.Passed, currentVisual.Detail),
                    "Open the fresh Wave 3 TSV/PNGs at 1:1 and inspect every placement, search prompt, compact tray, and qps production-scene failure row.",
                    "Assets/Editor/ParallelQA/Wave12FiveDayCompactUiGateRunner.cs; Assets/Editor/ParallelQA/Wave3VisualGate.cs");

                string[] joysticks = Input.GetJoystickNames().Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
                evidence.joystickNames = joysticks;
                Unverified(checks, "W12-HW01.physical_gamepad", "input hardware", "P1",
                    "A human completes near/popup/direct-slot, an early escape, and the current session-profile loop with a physical gamepad",
                    joysticks.Length == 0 ? "no non-empty joystick name exposed to Unity batch Play Mode" : "device detected, but no human actuation evidence was captured",
                    "Run the Windows development build with a physical gamepad and record device identity plus human actuation.",
                    "manual release-candidate hardware evidence");

                WriteJson("wave12-play-evidence.json", evidence);
                Report report = WriteReport("wave12-play-contracts", "Wave 12 session-profile/compact-a Play contracts", started, checks);
                bool runnerPassed = report.infrastructureOverall == "PASS" && report.productFailed == 0;
                SessionState.SetBool(PlayExitPassKey, runnerPassed);
                SessionState.SetString(PlayMessageKey,
                    "Product=" + report.productOverall + " · Infrastructure=" + report.infrastructureOverall +
                    " · PhysicalGamepad=UNVERIFIED · Evidence=" + Path.Combine(EvidenceFolder, "wave12-play-contracts.json"));
            }
            catch (Exception exception)
            {
                WriteInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static Observation ObserveSessionFlowProfiles()
        {
            PrototypeSessionFlowVerification verification = PrototypeSessionFlowProfileCatalog.CaptureVerification();
            PrototypeSessionSettlementOutcome standardDayOneEscape = PrototypeSessionFlowProfileCatalog.ResolveSettlement(
                PrototypeSessionFlowProfileCatalog.StandardProfileId, 1, true, true);
            PrototypeSessionSettlementOutcome gameJamDayOneEscape = PrototypeSessionFlowProfileCatalog.ResolveSettlement(
                PrototypeSessionFlowProfileCatalog.GameJamProvisionalProfileId, 1, true, true);

            GameSession early = new GameSession(
                PrototypeExpeditionRegionCatalog.DefaultRunSeed,
                PrototypeSessionFlowProfileCatalog.GameJamProvisionalProfileId);
            early.Grant(ResourceKind.Wood, 20);
            early.Grant(ResourceKind.Salvage, 20);
            Require(early.TryBuild(StructureKind.Workbench), "early workbench");
            Require(early.TryUpgradeSignal(), "early signal stage 1");
            Require(early.TryResearch(TechKind.Rope) && early.TryCraft(TechKind.Rope), "early rope");
            Require(early.TryUpgradeSignal(), "early signal stage 2");
            bool earlyRescue = early.Day == 1 &&
                               early.SessionProfileId == PrototypeSessionFlowProfileCatalog.GameJamProvisionalProfileId &&
                               early.Phase == GamePhase.Result && early.Result == RunResult.Rescued;
            bool passed = verification.ContractSatisfied &&
                          verification.StandardProfileId == PrototypeSessionFlowProfileCatalog.StandardProfileId &&
                          verification.StandardSettlementDay == GameSession.FinalDay &&
                          verification.GameJamProfileId == PrototypeSessionFlowProfileCatalog.GameJamProvisionalProfileId &&
                          verification.GameJamSettlementDay == PrototypeSessionFlowProfileCatalog.GameJamProvisionalSettlementDay &&
                          verification.GameJamTunableMinimumDay == PrototypeSessionFlowProfileCatalog.GameJamTunableMinimumDay &&
                          verification.GameJamTunableMaximumDay == PrototypeSessionFlowProfileCatalog.GameJamTunableMaximumDay &&
                          verification.StandardDayTwentyOutcome == PrototypeSessionSettlementOutcome.Continue &&
                          verification.StandardDayFiftyOutcome == PrototypeSessionSettlementOutcome.LongStay &&
                          verification.GameJamDayNineteenOutcome == PrototypeSessionSettlementOutcome.Continue &&
                          verification.GameJamDayTwentyOutcome == PrototypeSessionSettlementOutcome.LongStay &&
                          verification.GameJamDayTwentyEscapeOutcome == PrototypeSessionSettlementOutcome.EarlyEscape &&
                          standardDayOneEscape == PrototypeSessionSettlementOutcome.EarlyEscape &&
                          gameJamDayOneEscape == PrototypeSessionSettlementOutcome.EarlyEscape && earlyRescue;
            return new Observation
            {
                Passed = passed,
                Detail = "contractSatisfied=" + verification.ContractSatisfied +
                         "; standard=" + verification.StandardProfileId + "@D" + verification.StandardSettlementDay +
                         " outcomes(D20/D50)=" + verification.StandardDayTwentyOutcome + "/" + verification.StandardDayFiftyOutcome +
                         "; gameJam=" + verification.GameJamProfileId + "@D" + verification.GameJamSettlementDay +
                         " tune=" + verification.GameJamTunableMinimumDay + ".." + verification.GameJamTunableMaximumDay +
                         " outcomes(D19/D20/escape)=" + verification.GameJamDayNineteenOutcome + "/" +
                         verification.GameJamDayTwentyOutcome + "/" + verification.GameJamDayTwentyEscapeOutcome +
                         "; day1Priority(std/gamejam)=" + standardDayOneEscape + "/" + gameJamDayOneEscape +
                         "; actualGameJamDay1SignalRescue=" + earlyRescue
            };
        }

        private static Observation ObserveAdoptedPackage()
        {
            string ledger = File.ReadAllText(Path.Combine(ProjectRoot, ".forge", "assets.json"));
            string feedback = File.ReadAllText(Path.Combine(ProjectRoot, ".forge", "feedback.json"));
            string manifest = File.ReadAllText(Path.Combine(ProjectRoot, Path.GetDirectoryName(CompactAPath), "compact-interaction-9slice.json"));
            string meta = File.ReadAllText(Path.Combine(ProjectRoot, CompactAPath + ".meta"));
            bool ledgerReady = AssetBlock(ledger, PromptAssetId).Contains("\"status\": \"engine_ready\"") &&
                               AssetBlock(ledger, PromptAssetId).Contains("\"currentJobId\": \"" + PromptJobId + "\"");
            bool adopted = feedback.Contains("ui.camp-contextual-interaction.compact-a") && feedback.Contains("compact-a-approved");
            bool fileGuid = File.Exists(Path.Combine(ProjectRoot, CompactAPath)) && meta.Contains("guid: " + CompactAGuid);
            bool borders = Regex.IsMatch(manifest, "\\\"left\\\"\\s*:\\s*70") && Regex.IsMatch(manifest, "\\\"right\\\"\\s*:\\s*30") &&
                           Regex.IsMatch(manifest, "\\\"top\\\"\\s*:\\s*12") && Regex.IsMatch(manifest, "\\\"bottom\\\"\\s*:\\s*12") &&
                           manifest.Contains("\"recommended\": \"compact-a\"");
            string dependencyText = RuntimeDependencyText();
            bool bAndCUnreferenced = dependencyText.IndexOf(CompactBGuid, StringComparison.OrdinalIgnoreCase) < 0 &&
                                    dependencyText.IndexOf(CompactCGuid, StringComparison.OrdinalIgnoreCase) < 0 &&
                                    !dependencyText.Contains("compact-b.png") && !dependencyText.Contains("compact-c.png");
            return new Observation
            {
                Passed = ledgerReady && adopted && fileGuid && borders && bAndCUnreferenced,
                Detail = "ledgerEngineReady=" + ledgerReady + "; adoptedCompactA=" + adopted + "; fileGuid=" + fileGuid +
                         "(" + CompactAGuid + "); border=L70/R30/T12/B12:" + borders + "; B/C runtime refs=" + !bAndCUnreferenced
            };
        }

        private static Observation ObserveStaticRuntimeConnection()
        {
            string dependencyText = RuntimeDependencyText();
            bool aReferenced = dependencyText.IndexOf(CompactAGuid, StringComparison.OrdinalIgnoreCase) >= 0 || dependencyText.Contains("compact-a.png");
            bool bReferenced = dependencyText.IndexOf(CompactBGuid, StringComparison.OrdinalIgnoreCase) >= 0 || dependencyText.Contains("compact-b.png");
            bool cReferenced = dependencyText.IndexOf(CompactCGuid, StringComparison.OrdinalIgnoreCase) >= 0 || dependencyText.Contains("compact-c.png");
            return new Observation
            {
                Passed = aReferenced && !bReferenced && !cReferenced,
                Detail = "compactAReferenced=" + aReferenced + "; compactBReferenced=" + bReferenced + "; compactCReferenced=" + cReferenced
            };
        }

        private static Observation ObserveRuntimeFrame(KimSurvivalPrototype prototype)
        {
            GameObject prompt = GetField<GameObject>(prototype, "campProximityPrompt");
            TMP_Text actionText = GetField<TMP_Text>(prototype, "campProximityText");
            Image frame = prompt.GetComponent<Image>();
            Sprite sprite = frame == null ? null : frame.sprite;
            string path = sprite == null ? string.Empty : AssetDatabase.GetAssetPath(sprite);
            string guid = string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            Vector4 border = sprite == null ? Vector4.zero : sprite.border;
            bool exactFrame = frame != null && sprite != null && string.Equals(guid, CompactAGuid, StringComparison.OrdinalIgnoreCase) &&
                              frame.type == Image.Type.Sliced && Mathf.Approximately(border.x, 70f) && Mathf.Approximately(border.z, 30f) &&
                              Mathf.Approximately(border.w, 12f) && Mathf.Approximately(border.y, 12f);
            TMP_Text[] texts = prompt.GetComponentsInChildren<TMP_Text>(true);
            Image[] images = prompt.GetComponentsInChildren<Image>(true);
            bool separateGlyph = texts.Any(text => text != actionText && text.name.IndexOf("glyph", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 images.Any(image => image != frame && image.name.IndexOf("glyph", StringComparison.OrdinalIgnoreCase) >= 0 && image.sprite != null);
            bool actionExcludesGlyph = actionText != null && actionText.text.IndexOf("[E]", StringComparison.OrdinalIgnoreCase) < 0 &&
                                       actionText.text.IndexOf("[X]", StringComparison.OrdinalIgnoreCase) < 0;
            return new Observation
            {
                Passed = exactFrame && separateGlyph && actionExcludesGlyph,
                Detail = "spritePath=" + (string.IsNullOrWhiteSpace(path) ? "<none>" : path) + "; guid=" + (string.IsNullOrWhiteSpace(guid) ? "<none>" : guid) +
                         "; imageType=" + (frame == null ? "<none>" : frame.type.ToString()) + "; border=" + border +
                         "; separateGlyph=" + separateGlyph + "; actionExcludesGlyph=" + actionExcludesGlyph
            };
        }

        private static Observation ObserveInputLocaleIndependence(KimSurvivalPrototype prototype)
        {
            PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
            LegacyPrototypePlayerInput input = GetField<LegacyPrototypePlayerInput>(prototype, "playerInput");
            PrototypeInputDeviceTracker tracker = GetField<PrototypeInputDeviceTracker>(input, "deviceTracker");
            PrototypeCampUse use = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            List<PrototypeCampInteractionTarget> targets = GetField<List<PrototypeCampInteractionTarget>>(prototype, "campInteractionTargets");
            Invoke(prototype, "RefreshAll");
            PrototypeCampInteractionTarget upper = targets.First(item => item.Id == "slot.start.upper");
            use.Warp(upper.Position);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            tracker.Update(new PrototypeInputActivity(true, false));
            Invoke(prototype, "RefreshAll");
            string target = interaction.ActiveTargetId;
            string progress = ProgressFingerprint(prototype.Session);
            tracker.Update(new PrototypeInputActivity(false, true));
            Invoke(prototype, "RefreshAll");
            bool deviceOnly = input.ActiveDevice == PrototypeInputDevice.Gamepad && localization.CurrentLocaleCode == "ko" &&
                              interaction.ActiveTargetId == target && ProgressFingerprint(prototype.Session) == progress;
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            Invoke(prototype, "RefreshAll");
            bool enOnly = input.ActiveDevice == PrototypeInputDevice.Gamepad && interaction.ActiveTargetId == target && ProgressFingerprint(prototype.Session) == progress;
            bool qpsSet = localization.SetQaLocale();
            Invoke(prototype, "RefreshAll");
            bool qpsOnly = qpsSet && input.ActiveDevice == PrototypeInputDevice.Gamepad && interaction.ActiveTargetId == target && ProgressFingerprint(prototype.Session) == progress;
            tracker.Update(new PrototypeInputActivity(true, false));
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            Invoke(prototype, "RefreshAll");
            return new Observation
            {
                Passed = target == "slot.start.upper" && deviceOnly && enOnly && qpsOnly,
                Detail = "target=" + target + "; progress=" + progress + "; syntheticGamepadOnly=" + deviceOnly +
                         "; enOnly=" + enOnly + "; qpsOnly=" + qpsOnly
            };
        }

        private static CaptureObservation CaptureStateMatrix(KimSurvivalPrototype prototype)
        {
            PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
            LegacyPrototypePlayerInput input = GetField<LegacyPrototypePlayerInput>(prototype, "playerInput");
            PrototypeInputDeviceTracker tracker = GetField<PrototypeInputDeviceTracker>(input, "deviceTracker");
            PrototypeCampUse use = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            PrototypeCampModuleExpansion expansion = GetField<PrototypeCampModuleExpansion>(prototype, "campModuleExpansion");
            List<PrototypeCampInteractionTarget> targets = GetField<List<PrototypeCampInteractionTarget>>(prototype, "campInteractionTargets");
            Button moduleButton = GetField<Button>(prototype, "modulePreviewButton");
            GameObject prompt = GetField<GameObject>(prototype, "campProximityPrompt");
            Image message = GetField<Image>(prototype, "messagePanelImage");
            Canvas canvas = GetField<Canvas>(prototype, "canvas");
            Camera worldCamera = GetField<Camera>(prototype, "worldCamera");
            List<string> screenshots = new List<string>();
            List<string> rows = new List<string>();
            bool allStates = true;
            string[] locales = { "ko", "en", QpsLong };
            foreach (string locale in locales)
            {
                prototype.Session.Reset();
                use.Reset();
                interaction.Reset();
                expansion.Reset();
                tracker.Update(new PrototypeInputActivity(locale == "ko", locale != "ko"));
                if (locale == QpsLong) localization.SetQaLocale(); else localization.SetLocale(locale, false);
                Invoke(prototype, "RefreshAll");

                use.Warp(new Vector2(-10f, PrototypeCampPlacement.FloorY));
                Invoke(prototype, "RefreshAll");
                bool far = !prompt.activeSelf && !interaction.HasProximityPrompt;
                string farName = Capture(prototype, locale, "far"); screenshots.Add(farName);

                targets = GetField<List<PrototypeCampInteractionTarget>>(prototype, "campInteractionTargets");
                PrototypeCampInteractionTarget upper = targets.First(item => item.Id == "slot.start.upper");
                use.Warp(upper.Position);
                Invoke(prototype, "RefreshAll");
                bool near = prompt.activeSelf && interaction.ActiveTargetId == upper.Id;
                string nearName = Capture(prototype, locale, "near"); screenshots.Add(nearName);
                Rect[] measuredRects = MeasureCaptureRects(prompt.GetComponent<RectTransform>(), message.rectTransform, canvas, worldCamera);
                Rect promptRect = measuredRects[0];
                Rect narrationRect = measuredRects[1];
                float gap = narrationRect.yMin - promptRect.yMax;
                TMP_Text[] promptTexts = prompt.GetComponentsInChildren<TMP_Text>(true);
                bool promptFontFloor = promptTexts.Length >= 2 && promptTexts.All(text => text.fontSizeMin + 0.01f >= 18f);
                int nearOverflow = CountVisibleOverflow();

                Invoke(prototype, "UseNearestCampTarget");
                bool popup = interaction.IsPopupOpen && !prompt.activeSelf;
                string popupName = Capture(prototype, locale, "popup"); screenshots.Add(popupName);
                int popupOverflow = CountVisibleOverflow();
                moduleButton.onClick.Invoke();
                bool preview = expansion.IsPreviewActive && expansion.SelectedArchetype == CampModuleArchetype.Upper;
                string previewName = Capture(prototype, locale, "direct-slot"); screenshots.Add(previewName);
                int previewOverflow = CountVisibleOverflow();
                Invoke(prototype, "CancelCampModulePreview", true);
                if (interaction.IsPopupOpen) Invoke(prototype, "CancelCampPopup");

                bool compactGeometry = promptRect.width >= 511f && promptRect.width <= 513f &&
                                       promptRect.height >= 47f && promptRect.height <= 49f && gap >= 11.9f;
                bool overflowZero = nearOverflow == 0 && popupOverflow == 0 && previewOverflow == 0;
                bool localePass = far && near && popup && preview && overflowZero && compactGeometry && promptFontFloor;
                allStates &= localePass;
                rows.Add(locale + "{far=" + far + ",near=" + near + ",popup=" + popup + ",preview=" + preview +
                         ",prompt=" + FormatRect(promptRect) + ",narration=" + FormatRect(narrationRect) + ",gap=" + gap.ToString("0.0") +
                         ",overflow=" + nearOverflow + "/" + popupOverflow + "/" + previewOverflow +
                         ",compact=" + compactGeometry + ",fontMin18=" + promptFontFloor + "}");
            }

            Observation wave11 = ObserveFreshWave11Layout();
            bool pngs = screenshots.Count == 12 && screenshots.All(VerifyPng);
            return new CaptureObservation
            {
                Passed = allStates && wave11.Passed && pngs,
                Detail = "captures=" + screenshots.Count + "/12; png1280x800=" + pngs + "; wave11WorldClear=" + wave11.Passed + "; " + string.Join(" | ", rows),
                Screenshots = screenshots.ToArray()
            };
        }

        private static Observation ObserveFreshWave11Layout()
        {
            string path = Path.Combine(EvidenceFolder, "wave11-slot-play-evidence.json");
            if (!File.Exists(path)) return new Observation { Detail = "fresh wave11 evidence missing: " + path };
            string json = File.ReadAllText(path);
            int clear = Regex.Matches(json, "\\\"walkingPathClear\\\"\\s*:\\s*true", RegexOptions.IgnoreCase).Count;
            int rects = Regex.Matches(json, "\\\"promptScreenRect\\\"\\s*:\\s*\\\"x=", RegexOptions.IgnoreCase).Count;
            int noFailures = Regex.Matches(json, "\\\"layoutFailureReasons\\\"\\s*:\\s*\\\"none\\\"", RegexOptions.IgnoreCase).Count;
            return new Observation
            {
                Passed = clear == 3 && rects == 3 && noFailures == 3,
                Detail = "walkingPathClear=" + clear + "/3; promptRects=" + rects + "/3; failureReasonsNone=" + noFailures + "/3; source=" + Path.GetFileName(path)
            };
        }

        private static string GenerateFreshWave3VisualEvidence(KimSurvivalPrototype prototype)
        {
            DateTime started = DateTime.UtcNow;
            List<string> layoutAudit = new List<string>();
            List<Wave3VisualGate.FrameResult> frames = new List<Wave3VisualGate.FrameResult>();
            AddWave3PlacementFrames(prototype, PrototypeLocalization.KoreanLocaleCode,
                PrototypeInputDevice.KeyboardMouse, false, layoutAudit, frames);
            AddWave3PlacementFrames(prototype, PrototypeLocalization.EnglishLocaleCode,
                PrototypeInputDevice.Gamepad, false, layoutAudit, frames);
            AddWave3PlacementFrames(prototype, QpsLong, PrototypeInputDevice.Gamepad, true, layoutAudit, frames);
            AddWave3ExplorationFrames(prototype, PrototypeLocalization.KoreanLocaleCode, "ko", layoutAudit, frames);
            AddWave3ExplorationFrames(prototype, PrototypeLocalization.EnglishLocaleCode, "en", layoutAudit, frames);
            AddWave3QpsProductionFrames(prototype, layoutAudit, frames);
            bool visualPass = Wave3VisualGate.WriteReports(EvidenceFolder, RunId, BaselineCommit,
                Application.unityVersion, string.Join(" ", Environment.GetCommandLineArgs()), started, frames);
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave3-layout-audit.txt"),
                string.Join(Environment.NewLine, layoutAudit) + Environment.NewLine, Utf8NoBom);
            string reportPath = Path.Combine(EvidenceFolder, "wave3-visual-gate.txt");
            Require(File.Exists(reportPath), "fresh Wave 3 report exists: " + reportPath);
            string report = File.ReadAllText(reportPath);
            Require(report.Contains("Run ID: " + RunId), "fresh Wave 3 report RunId matches");
            Require(report.Contains("Baseline commit: " + BaselineCommit), "fresh Wave 3 report baseline matches");
            return "generated=true; runId/baseline matched; visualOverall=" +
                   (visualPass ? "PASS" : "FAIL_REVIEW_REPORT") + "; frames=" + frames.Count +
                   "; report=" + Path.GetFileName(reportPath);
        }

        private static void AddWave3PlacementFrames(KimSurvivalPrototype prototype, string localeCode,
            PrototypeInputDevice device, bool qpsOnly, List<string> layoutAudit, List<Wave3VisualGate.FrameResult> frames)
        {
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetField<PrototypeCampPlacement>(prototype, "campPlacement");
            PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            session.Reset();
            placement.Reset();
            campUse.Reset();
            interaction.Reset();
            session.Grant(ResourceKind.Wood, 10);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Salvage, 10);
            if (qpsOnly) Require(localization.SetQaLocale(), "qps-long visual fixture locale");
            else localization.SetLocale(localeCode, false);
            Invoke(prototype, "RefreshAll");
            List<PrototypeCampInteractionTarget> targets = GetField<List<PrototypeCampInteractionTarget>>(prototype, "campInteractionTargets");
            PrototypeCampInteractionTarget campfire = targets.First(target => target.Id == "camp.Campfire");
            campUse.Warp(campfire.Position);
            Invoke(prototype, "RefreshAll");
            Require(interaction.ActiveTargetId == campfire.Id, localeCode + " spatial campfire target selected");
            Invoke(prototype, "UseNearestCampTarget");
            Button campfireAction = GetField<Button>(prototype, "campfireButton");
            Require(interaction.IsPopupOpen && campfireAction.gameObject.activeInHierarchy && campfireAction.interactable,
                localeCode + " spatial campfire popup action available");
            campfireAction.onClick.Invoke();
            Require(placement.IsActive && placement.SelectedKind == StructureKind.Campfire,
                localeCode + " spatial popup starts placement");
            Invoke(prototype, "ApplyPlacementGuidance", device);
            placement.SetCandidateX(-1.5f);
            Invoke(prototype, "UpdatePlacementGhost");
            Invoke(prototype, "RefreshHud");
            Require(placement.CurrentValidity == CampPlacementValidity.Valid, localeCode + " direct visual placement valid");
            AddWave3Frame(prototype, "playmode-" + localeCode + "-placement-valid-1280x800.png",
                localeCode + " placement valid", layoutAudit, frames);
            if (!qpsOnly)
            {
                placement.SetCandidateX(-5f);
                Invoke(prototype, "UpdatePlacementGhost");
                Invoke(prototype, "RefreshHud");
                Require(placement.CurrentValidity == CampPlacementValidity.OutsideCampBounds, localeCode + " direct visual placement invalid");
                AddWave3Frame(prototype, "playmode-" + localeCode + "-placement-invalid-1280x800.png",
                    localeCode + " placement invalid", layoutAudit, frames);
            }
            placement.Cancel();
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            Invoke(prototype, "RefreshAll");
        }

        private static void AddWave3ExplorationFrames(KimSurvivalPrototype prototype, string localeCode, string prefix,
            List<string> layoutAudit, List<Wave3VisualGate.FrameResult> frames)
        {
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetField<PrototypeCampPlacement>(prototype, "campPlacement");
            PrototypeSearchNodeRuntime searchRuntime = GetField<PrototypeSearchNodeRuntime>(prototype, "searchNodeRuntime");
            PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            PrototypeExpeditionMapSelection mapSelection = GetField<PrototypeExpeditionMapSelection>(prototype, "expeditionMapSelection");
            session.Reset();
            searchRuntime.Reset(session.RunSeed);
            placement.Reset();
            campUse.Reset();
            interaction.Reset();
            mapSelection.Close();
            localization.SetLocale(localeCode, false);
            Invoke(prototype, "RefreshAll");
            int grantBefore = PrototypeProductionActionCounters.GrantCallCount;
            int warpBefore = PrototypeProductionActionCounters.WarpCallCount;
            int skipBefore = PrototypeProductionActionCounters.SkipCallCount;

            ProductionSearchNodeQaDriver.BeginExpedition(
                prototype, PrototypeExpeditionRegionId.Shallows, prefix + " fresh visual");
            ProductionSearchNodeQaDriver.Target firstLand = ProductionSearchNodeQaDriver.MoveToNext(
                prototype, false, prefix + " first land node");
            ProductionSearchNodeQaDriver.Open(prototype, firstLand, prefix + " first land node");
            AddWave3Frame(prototype, "playmode-" + prefix + "-search-loot-tray-1280x800.png",
                prefix + " production search loot tray", layoutAudit, frames);
            ProductionSearchNodeQaDriver.TakeAllAndClose(prototype, prefix + " first land node");

            ProductionSearchNodeQaDriver.Target water = ProductionSearchNodeQaDriver.MoveToNext(
                prototype, true, prefix + " water node");
            AddWave3Frame(prototype, "playmode-" + prefix + "-day1-swimming-1280x800.png",
                prefix + " day1 swimming", layoutAudit, frames);
            ProductionSearchNodeQaDriver.Open(prototype, water, prefix + " water node");
            ProductionSearchNodeQaDriver.TakeAllAndClose(prototype, prefix + " water node");
            ProductionSearchNodeQaDriver.SearchAndTakeAllNext(prototype, false, prefix + " second land node");
            ProductionSearchNodeQaDriver.SearchAndTakeAllNext(prototype, false, prefix + " third land node");
            AddWave3Frame(prototype, "playmode-" + prefix + "-day2-exploration-1280x800.png",
                prefix + " day2 exploration", layoutAudit, frames);
            Require(PrototypeProductionActionCounters.GrantCallCount == grantBefore &&
                    PrototypeProductionActionCounters.WarpCallCount == warpBefore &&
                    PrototypeProductionActionCounters.SkipCallCount == skipBefore,
                prefix + " normal production search scenes use no Grant, Warp, or Skip");
        }

        private static void AddWave3QpsProductionFrames(
            KimSurvivalPrototype prototype,
            List<string> layoutAudit,
            List<Wave3VisualGate.FrameResult> frames)
        {
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetField<PrototypeCampPlacement>(prototype, "campPlacement");
            PrototypeSearchNodeRuntime searchRuntime = GetField<PrototypeSearchNodeRuntime>(prototype, "searchNodeRuntime");
            PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            PrototypeExpeditionMapSelection mapSelection = GetField<PrototypeExpeditionMapSelection>(prototype, "expeditionMapSelection");
            object hazardRuntime = GetField<object>(prototype, "hazardEscapeEndingRuntime");

            session.Reset();
            searchRuntime.Reset(session.RunSeed);
            placement.Reset();
            campUse.Reset();
            interaction.Reset();
            mapSelection.Close();
            Require(localization.SetQaLocale(), "qps-long production scene locale");
            Invoke(prototype, "RefreshAll");
            int grantBefore = PrototypeProductionActionCounters.GrantCallCount;
            int warpBefore = PrototypeProductionActionCounters.WarpCallCount;
            int skipBefore = PrototypeProductionActionCounters.SkipCallCount;

            Invoke(prototype, "MoveNaturallyToCampTarget", PrototypeCampInteractionTargetKind.ModuleExpansionSlot);
            Invoke(prototype, "RefreshAll");
            Require(interaction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot &&
                    interaction.HasProximityPrompt,
                "qps-long production movement selects the module-expansion proximity target");
            AddWave3Frame(prototype, "playmode-qps-long-camp-proximity-1280x800.png",
                "qps-long camp proximity", layoutAudit, frames);

            Invoke(prototype, "OpenCampTargetThroughProductionInput", PrototypeCampInteractionTargetKind.ModuleExpansionSlot);
            Require(interaction.IsPopupOpen &&
                    GetField<GameObject>(prototype, "campInteractionPopup").activeSelf,
                "qps-long mapped Interact opens the compact production popup");
            AddWave3Frame(prototype, "playmode-qps-long-camp-popup-1280x800.png",
                "qps-long camp popup", layoutAudit, frames);
            interaction.ClosePopup();

            session.Reset();
            Invoke(hazardRuntime, "ResetRuntime");
            searchRuntime.Reset(session.RunSeed);
            placement.Reset();
            campUse.Reset();
            interaction.Reset();
            mapSelection.Close();
            Invoke(prototype, "RefreshAll");
            ProductionSearchNodeQaDriver.BeginExpedition(
                prototype, PrototypeExpeditionRegionId.Beach, "qps-long production search");
            ProductionSearchNodeQaDriver.Target target = ProductionSearchNodeQaDriver.MoveToNextWithoutProtectedPart(
                prototype, false, "qps-long production search node");
            ProductionSearchNodeQaDriver.Open(prototype, target, "qps-long production search node");
            AddWave3Frame(prototype, "playmode-qps-long-search-tray-1280x800.png",
                "qps-long search tray", layoutAudit, frames);
            ProductionSearchNodeQaDriver.TakeAllAndClose(prototype, "qps-long production search node");

            Require(PrototypeProductionActionCounters.GrantCallCount == grantBefore &&
                    PrototypeProductionActionCounters.WarpCallCount == warpBefore &&
                    PrototypeProductionActionCounters.SkipCallCount == skipBefore,
                "qps-long production proximity, popup, and search tray use no Grant, Warp, or Skip");
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            Invoke(prototype, "RefreshAll");
        }

        private static void AddWave3Frame(KimSurvivalPrototype prototype, string fileName, string scenario,
            List<string> layoutAudit, List<Wave3VisualGate.FrameResult> frames)
        {
            string path = Path.Combine(EvidenceFolder, fileName);
            prototype.CaptureVerificationPng(path, 1280, 800);
            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);
            Camera camera = GetField<Camera>(prototype, "worldCamera");
            Wave3VisualGate.FrameResult frame = Wave3VisualGate.Analyze(scenario, path, camera, texts);
            frames.Add(frame);
            layoutAudit.Add((frame.Passed ? "PASS" : "FAIL") + " · " + scenario + " · gated=" +
                            frame.GatedMetrics.Count() + " · screenshot=" + fileName);
        }

        private static Observation ObserveFreshWave3NormalVisual()
        {
            string path = Path.Combine(EvidenceFolder, "wave3-visual-gate.txt");
            if (!File.Exists(path)) return new Observation { Detail = "fresh Wave 3 report missing" };
            string report = File.ReadAllText(path);
            Match placement = Regex.Match(report, @"PLACEMENT_GATE:\s+(PASS|FAIL)\s+·\s+targets=(\d+)\s+·\s+failures=(\d+)");
            Match exploration = Regex.Match(report, @"EXPLORATION_SWIMMING_GATE:\s+(PASS|FAIL)\s+·\s+targets=(\d+)\s+·\s+failures=(\d+)");
            Match searchTray = Regex.Match(report, @"SEARCH_TRAY_GATE:\s+(PASS|FAIL)\s+·\s+targets=(\d+)\s+·\s+failures=(\d+)");
            Match pseudo = Regex.Match(report, @"PSEUDO_LONG_GATE:\s+(PASS|FAIL)\s+·\s+targets=(\d+)\s+·\s+failures=(\d+)");
            bool passed = ExactVisualFact(placement, 4) && ExactVisualFact(exploration, 4) &&
                          ExactVisualFact(searchTray, 16) && ExactVisualFact(pseudo, 37);
            return new Observation
            {
                Passed = passed,
                Detail = "placement=" + (placement.Success ? placement.Value : "MISSING") + "; exploration=" +
                         (exploration.Success ? exploration.Value : "MISSING") + "; searchTray=" +
                         (searchTray.Success ? searchTray.Value : "MISSING") + "; qps=" +
                         (pseudo.Success ? pseudo.Value : "MISSING") + "; source=" + Path.GetFileName(path)
            };
        }

        private static bool ExactVisualFact(Match match, int expectedTargets)
        {
            return match.Success && match.Groups[1].Value == "PASS" &&
                   match.Groups[2].Value == expectedTargets.ToString() && match.Groups[3].Value == "0";
        }

        private static string Capture(KimSurvivalPrototype prototype, string locale, string state)
        {
            string name = "wave12-" + locale + "-" + state + "-1280x800.png";
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, name), 1280, 800);
            return name;
        }

        private static Rect ScreenRect(RectTransform transform, Canvas canvas)
        {
            Canvas.ForceUpdateCanvases();
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            Vector2[] points = corners.Select(corner => RectTransformUtility.WorldToScreenPoint(eventCamera, corner)).ToArray();
            Rect raw = Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
            Rect source = canvas.pixelRect;
            float width = source.width > 0.01f ? source.width : Mathf.Max(1f, Screen.width);
            float height = source.height > 0.01f ? source.height : Mathf.Max(1f, Screen.height);
            return new Rect((raw.x - source.x) * 1280f / width, (raw.y - source.y) * 800f / height,
                raw.width * 1280f / width, raw.height * 800f / height);
        }

        private static Rect[] MeasureCaptureRects(RectTransform prompt, RectTransform narration, Canvas canvas, Camera worldCamera)
        {
            RenderTexture previousTarget = worldCamera.targetTexture;
            RenderTexture captureTarget = RenderTexture.GetTemporary(1280, 800, 24, RenderTextureFormat.ARGB32);
            try
            {
                worldCamera.targetTexture = captureTarget;
                Canvas.ForceUpdateCanvases();
                return new[] { ScreenRect(prompt, canvas), ScreenRect(narration, canvas) };
            }
            finally
            {
                worldCamera.targetTexture = previousTarget;
                Canvas.ForceUpdateCanvases();
                RenderTexture.ReleaseTemporary(captureTarget);
            }
        }

        private static int CountVisibleOverflow()
        {
            int count = 0;
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude))
            {
                if (text == null || !text.gameObject.activeInHierarchy) continue;
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing) count += 1;
            }
            return count;
        }

        private static bool VerifyPng(string fileName)
        {
            string path = Path.Combine(EvidenceFolder, fileName);
            if (!File.Exists(path) || new FileInfo(path).Length == 0) return false;
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try { return texture.LoadImage(File.ReadAllBytes(path), false) && texture.width == 1280 && texture.height == 800; }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static string RuntimeDependencyText()
        {
            List<string> roots = new List<string>
            {
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scenes"),
                Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime"),
                Path.Combine(ProjectRoot, "Assets", "_Project", "Resources"),
                Path.Combine(ProjectRoot, "Assets", "AddressableAssetsData")
            };
            StringBuilder text = new StringBuilder();
            foreach (string root in roots.Where(Directory.Exists))
            {
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                    .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                                   path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                                   path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
                {
                    try { text.AppendLine(File.ReadAllText(file)); } catch (IOException) { }
                }
            }
            return text.ToString();
        }

        private static string AssetBlock(string ledger, string assetId)
        {
            int start = ledger.IndexOf("\"id\": \"" + assetId + "\"", StringComparison.Ordinal);
            if (start < 0) return string.Empty;
            int next = ledger.IndexOf("\n    {", start + assetId.Length + 8, StringComparison.Ordinal);
            return next < 0 ? ledger.Substring(start) : ledger.Substring(start, next - start);
        }

        private static string ProgressFingerprint(GameSession session)
        {
            return "D" + session.Day + "/W" + session.GetStorage(ResourceKind.Wood) + "/S" + session.GetStorage(ResourceKind.Stone) +
                   "/F" + session.GetStorage(ResourceKind.Food) + "/D" + session.GetStorage(ResourceKind.Salvage) +
                   "/signal" + session.SignalStage + "/bag" + session.ActiveBagSlotCount + "/" + session.Phase + "/" + session.Result;
        }

        private static string FormatRect(Rect rect)
        {
            return "x=" + rect.x.ToString("0.0") + ",y=" + rect.y.ToString("0.0") +
                   ",w=" + rect.width.ToString("0.0") + ",h=" + rect.height.ToString("0.0");
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, InstanceFlags);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return (T)field.GetValue(target);
        }

        private static object Invoke(object target, string name, params object[] arguments)
        {
            object[] supplied = arguments ?? Array.Empty<object>();
            MethodInfo method = target.GetType().GetMethods(InstanceFlags)
                .Where(candidate => candidate.Name == name)
                .FirstOrDefault(candidate => ParametersAccept(candidate.GetParameters(), supplied));
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            try { return method.Invoke(target, supplied); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static bool ParametersAccept(ParameterInfo[] parameters, object[] arguments)
        {
            if (parameters.Length != arguments.Length) return false;
            for (int index = 0; index < parameters.Length; index += 1)
            {
                object value = arguments[index];
                Type parameterType = parameters[index].ParameterType;
                if (value == null)
                {
                    if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null) return false;
                    continue;
                }
                if (!parameterType.IsInstanceOfType(value)) return false;
            }
            return true;
        }

        private static string RequireDetail(bool condition, string detail)
        {
            Require(condition, detail);
            return detail;
        }

        private static string Product(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> verification, string reproduction, string files)
        {
            try
            {
                string actual = verification();
                checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, actual, reproduction, files));
                return actual;
            }
            catch (Exception exception)
            {
                string actual = exception.GetType().Name + ": " + exception.Message;
                checks.Add(NewCheck(id, matrix, "FAIL", "PRODUCT_REGRESSION", severity, expected, actual, reproduction, files));
                return actual;
            }
        }

        private static string Infrastructure(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> verification, string reproduction, string files)
        {
            try
            {
                string actual = verification();
                checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, actual, reproduction, files));
                return actual;
            }
            catch (Exception exception)
            {
                string actual = exception.GetType().Name + ": " + exception.Message;
                checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected, actual, reproduction, files));
                return actual;
            }
        }

        private static void Unverified(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", "HARDWARE_GAP", severity, expected, actual, reproduction, files));
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
                title = title, runId = RunId, baselineCommit = BaselineCommit, unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"), completedUtc = DateTime.UtcNow.ToString("O"),
                passed = checks.Count(check => check.status == "PASS"),
                expectedFailed = checks.Count(check => check.status == "EXPECTED_FAIL"),
                productFailed = checks.Count(check => check.status == "FAIL"),
                infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"), checks = checks.ToArray()
            };
            report.productOverall = report.productFailed > 0 ? "FAIL" : report.expectedFailed > 0 ? "RED_EXPECTED_FAIL" : "PASS";
            report.infrastructureOverall = report.infrastructureFailed > 0 ? "FAIL" : "PASS";
            report.overall = report.infrastructureOverall == "FAIL" || report.productOverall == "FAIL" ? "FAIL" :
                report.productOverall == "RED_EXPECTED_FAIL" ? "RED" : "PASS";
            WriteJson(stem + ".json", report);
            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Unity: " + Application.unityVersion);
            text.AppendLine("Overall/Product/Infrastructure: " + report.overall + "/" + report.productOverall + "/" + report.infrastructureOverall);
            text.AppendLine("PASS/EXPECTED_FAIL/FAIL/INFRA_FAIL/UNVERIFIED: " + report.passed + "/" + report.expectedFailed + "/" + report.productFailed + "/" + report.infrastructureFailed + "/" + report.unverified);
            foreach (Check check in checks) text.AppendLine(check.id + " | " + check.status + " | " + check.classification + " | " + check.actual);
            File.WriteAllText(Path.Combine(EvidenceFolder, stem + ".txt"), text.ToString(), Utf8NoBom);
            return report;
        }

        private static Evidence NewEvidence()
        {
            return new Evidence { runId = RunId, baselineCommit = BaselineCommit, unityVersion = Application.unityVersion,
                screenshots = Array.Empty<string>(), joystickNames = Array.Empty<string>() };
        }

        private static void WriteJson(string fileName, object value)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, fileName), JsonUtility.ToJson(value, true) + Environment.NewLine, Utf8NoBom);
        }

        private static void WriteInfrastructureFailure(Exception exception)
        {
            List<Check> checks = new List<Check>
            {
                NewCheck("W12-I99.play_runner", "infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Wave 12 Play runner produces parseable evidence", exception.ToString(),
                    "Run the Wave 12 Play execute method outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave12FiveDayCompactUiGateRunner.cs")
            };
            WriteReport("wave12-play-contracts", "Wave 12 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · " + exception);
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
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL · missing Wave 12 Play result");
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

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
