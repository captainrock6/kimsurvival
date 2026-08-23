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
    /// Independent, non-shipping Wave 6 red-first contracts for progression,
    /// gating, balance, localized feedback, and exact 1280x800 readability.
    /// Runtime state is exercised through public APIs; no product file is edited.
    /// </summary>
    public static class Wave6ProgressionRegressionRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave6.Play.Running";
        private const string PlayPassedKey = "ParallelQA.Wave6.Play.Passed";
        private const string PlayMessageKey = "ParallelQA.Wave6.Play.Message";
        private const float BarrierLockedMaximumX = 8f;
        private static bool playTickAttached;
        private static double playEarliestRunTime;
        private static double playTimeoutAt;

        [Serializable]
        public sealed class ContractCheck
        {
            public string id;
            public string matrixItem;
            public string status;
            public string classification;
            public string severity;
            public string expected;
            public string actual;
            public string reproduction;
            public string recommendedFiles;
        }

        [Serializable]
        public sealed class ContractReport
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string command;
            public string overall;
            public string productOverall;
            public string infrastructureOverall;
            public int passed;
            public int failed;
            public int unverified;
            public int infrastructureFailed;
            public ContractCheck[] checks;
        }

        private sealed class Observation
        {
            public bool Passed;
            public string Actual;

            public static Observation Pass(string actual)
            {
                return new Observation { Passed = true, Actual = actual };
            }

            public static Observation Product(bool passed, string actual)
            {
                return new Observation { Passed = passed, Actual = actual };
            }
        }

        private static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave6" : Sanitize(value);
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

        private static string EvidenceFolder
        {
            get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); }
        }

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<ContractCheck> checks = new List<ContractCheck>();

            Product(checks, "W6-01.signal.stage1", "1", "P0",
                "Workbench + wood 2 + salvage 2, without rope, upgrades signal stage 0 -> 1 exactly once",
                VerifySignalStageOne,
                "Create an exact stage-0 fixture, call GameSession.TryUpgradeSignal once, and inspect stage/resources/result.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");

            Product(checks, "W6-02.signal.stage2.requires_rope", "2", "P0",
                "Stage 1 + wood 2 + salvage 2 without rope rejects stage 2 and reports rope required without spending",
                VerifySignalStageTwoRequiresRope,
                "Create stage 1, add exact materials without rope, call TryUpgradeSignal, inspect LastMessage and resources.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            Product(checks, "W6-03.signal.stage2.rescue", "3", "P0",
                "Rope + wood 2 + salvage 2 at stage 1 completes stage 2 and reaches Rescued",
                VerifySignalStageTwoRescue,
                "Create stage 1, research/craft rope while leaving exact final materials, then call TryUpgradeSignal.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");

            Product(checks, "W6-04a.signal.shortage.workbench", "4", "P1",
                "Missing workbench feedback identifies the workbench requirement in ko and en",
                () => VerifyShortageFeedback("workbench"),
                "Attempt stage 1 with wood 2/salvage 2 and no workbench; localize LastMessage to ko/en.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");
            Product(checks, "W6-04b.signal.shortage.wood", "4", "P1",
                "Missing wood feedback identifies wood in ko and en",
                () => VerifyShortageFeedback("wood"),
                "Attempt stage 1 with workbench/salvage 2 and wood 0; localize LastMessage to ko/en.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");
            Product(checks, "W6-04c.signal.shortage.salvage", "4", "P1",
                "Missing salvage feedback identifies salvage in ko and en",
                () => VerifyShortageFeedback("salvage"),
                "Attempt stage 1 with workbench/wood 2 and salvage 0; localize LastMessage to ko/en.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            Product(checks, "W6-05.barrier.rope_only_blocked", "5", "P0",
                "Rope without a stone axe cannot move beyond the vine/wood barrier",
                VerifyRopeOnlyBarrier,
                "Craft rope only, begin search, step right from x=7.7, and inspect the traversal clamp/blocked notice.",
                "Assets/_Project/Scripts/Runtime/PrototypePlayerTraversal.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            Product(checks, "W6-06.barrier.axe_only_passes", "6", "P0",
                "A stone axe without rope can move beyond the vine/wood barrier",
                VerifyAxeOnlyBarrier,
                "Craft stone axe only, begin search, step right from x=7.7, and inspect traversal x/blocked notice.",
                "Assets/_Project/Scripts/Runtime/PrototypePlayerTraversal.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            Product(checks, "W6-07a.axe.gather.plus_one", "7", "P0",
                "Stone axe changes a base wood gather by exactly +1, not a multiplier",
                VerifyAxeGatherAmount,
                "Compare bag wood after baseAmount=1 with no tool and stone axe only.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");
            Product(checks, "W6-07b.axe.copy.no_double_claim", "7", "P1",
                "Player-facing ko/en stone-axe copy contains no 2x, twice, double, 2배, or 두 배 claim",
                VerifyAxeCopy,
                "Format the axe research/craft/gather strings in ko/en and scan deterministic forbidden multiplier terms.",
                "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            Product(checks, "W6-08a.signal.ko_en_semantics", "8", "P1",
                "ko/en signal progression exposes the same workbench, wood 2, salvage 2, and final-rope meaning",
                VerifySignalLocalizationSemantics,
                "Format stage progress, material feedback, and rope feedback in both locales and compare required concepts.",
                "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            Product(checks, "W6-09a.balance.f0_h70_day35", "9", "P0",
                "Reset starts Food 0 / Hunger 70 and one settlement drains Hunger by exactly 35",
                VerifyBalanceProfile,
                "Reset GameSession, observe food/hunger, complete an empty expedition, and compare pre/post EndDay hunger.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");
            Product(checks, "W6-09b.natural.rescue_no_grant_warp", "9", "P0",
                "Day 1-3 natural resource route reaches rescue with no Grant or Warp call",
                VerifyNaturalRescueNoCheats,
                "Run the deterministic three-day model route and source-audit this route method for Grant/Warp calls.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");
            Product(checks, "W6-09c.natural.exhaustion", "9", "P0",
                "Natural search energy depletion reaches Exhausted",
                VerifyExhaustion,
                "Begin a swimming search and advance moving search time until energy reaches zero.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");
            Product(checks, "W6-09d.natural.deadline", "9", "P0",
                "Three completed days without signal reach Deadline",
                VerifyDeadline,
                "Complete search/return/settlement without signal for each of the three days.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");

            ContractReport report = WriteReport("wave6-edit-contracts", "Wave 6 progression Edit contracts", started, checks);
            if (report.overall != "PASS")
            {
                throw new InvalidOperationException("Wave 6 red-first Edit contracts are not green. See " + Path.Combine(EvidenceFolder, "wave6-edit-contracts.json"));
            }
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayPassedKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Wave 6 Play contracts did not complete.");
            AttachPlayCallbacks();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void ResumePlayContracts()
        {
            if (SessionState.GetBool(PlayRunningKey, false))
            {
                AttachPlayCallbacks();
            }
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
            playTimeoutAt = EditorApplication.timeSinceStartup + 90d;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 2d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 90d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishPlayContracts();
            }
        }

        private static void PlayTick()
        {
            if (!SessionState.GetBool(PlayRunningKey, false) || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (now < playEarliestRunTime) return;
            if (now > playTimeoutAt)
            {
                SessionState.SetBool(PlayPassedKey, false);
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Timed out waiting for KimSurvivalPrototype.");
                StopPlayContracts();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;

            try
            {
                DateTime started = DateTime.UtcNow;
                List<ContractCheck> checks = new List<ContractCheck>();
                RunLocalizedVisualContracts(prototype, checks);
                string[] joysticks = Input.GetJoystickNames() ?? Array.Empty<string>();
                string[] active = joysticks.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
                checks.Add(new ContractCheck
                {
                    id = "W6-HW.physical_gamepad",
                    matrixItem = "10",
                    status = "UNVERIFIED",
                    classification = "HARDWARE_GAP",
                    severity = "P2",
                    expected = "Human actuation on a physical gamepad",
                    actual = active.Length == 0 ? "no non-empty joystick name exposed to Unity batch Play Mode" : "device name observed but no human actuation captured: " + string.Join(" | ", active),
                    reproduction = "Run the Windows build with a physical controller and actuate movement, interaction, language, and progression controls.",
                    recommendedFiles = "manual release-candidate hardware evidence"
                });

                ContractReport report = WriteReport("wave6-play-contracts", "Wave 6 ko/en progression Play contracts", started, checks);
                bool passed = report.infrastructureOverall == "PASS" && report.productOverall == "PASS";
                SessionState.SetBool(PlayPassedKey, passed);
                SessionState.SetString(PlayMessageKey,
                    "Product=" + report.productOverall + " · Infrastructure=" + report.infrastructureOverall +
                    " · PhysicalGamepad=UNVERIFIED · Evidence=" + Path.Combine(EvidenceFolder, "wave6-play-contracts.json"));
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PlayPassedKey, false);
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · " + exception);
                WriteInfrastructureFailure("wave6-play-contracts", exception);
            }

            StopPlayContracts();
        }

        private static void StopPlayContracts()
        {
            if (playTickAttached)
            {
                EditorApplication.update -= PlayTick;
                playTickAttached = false;
            }
            EditorApplication.isPlaying = false;
        }

        private static void FinishPlayContracts()
        {
            bool passed = SessionState.GetBool(PlayPassedKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL · no Play result");
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave6-play-exit.txt"), message + Environment.NewLine, new UTF8Encoding(false));
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayPassedKey);
            SessionState.EraseString(PlayMessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void RunLocalizedVisualContracts(KimSurvivalPrototype prototype, List<ContractCheck> checks)
        {
            PrototypeLocalization localization = GetPrivateField<PrototypeLocalization>(prototype, "localization");
            CaptureSignalStageTwo(prototype, localization, PrototypeLocalization.KoreanLocaleCode, checks);
            CaptureSignalStageTwo(prototype, localization, PrototypeLocalization.EnglishLocaleCode, checks);
            CaptureBarrier(prototype, localization, PrototypeLocalization.KoreanLocaleCode, checks);
            CaptureBarrier(prototype, localization, PrototypeLocalization.EnglishLocaleCode, checks);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            InvokePrivate(prototype, "RefreshAll");
        }

        private static void CaptureSignalStageTwo(KimSurvivalPrototype prototype, PrototypeLocalization localization, string localeCode, List<ContractCheck> checks)
        {
            GameSession session = prototype.Session;
            PrepareStageOne(session);
            session.Grant(ResourceKind.Wood, 2);
            session.Grant(ResourceKind.Salvage, 2);
            bool upgraded = session.TryUpgradeSignal();
            localization.SetLocale(localeCode, false);
            InvokePrivate(prototype, "RefreshAll");

            TMP_Text message = GetPrivateField<TMP_Text>(prototype, "messageText");
            Button signalButton = GetPrivateField<Button>(prototype, "signalButton");
            TMP_Text buttonText = signalButton.GetComponentInChildren<TMP_Text>();
            string screenshot = "wave6-" + localeCode + "-signal-stage2-1280x800.png";
            string path = Path.Combine(EvidenceFolder, screenshot);
            prototype.CaptureVerificationPng(path, Wave3VisualGate.Width, Wave3VisualGate.Height);

            string combined = message.text + " | " + buttonText.text;
            bool semantic = !upgraded && session.SignalStage == 1 &&
                            (localeCode == PrototypeLocalization.KoreanLocaleCode
                                ? ContainsAll(combined, "밧줄", "나무", "표류물", "2")
                                : ContainsAllIgnoreCase(combined, "rope", "wood", "salvage", "2"));
            AddObservedProduct(checks, "W6-08." + localeCode + ".signal.semantic", "8", "P1",
                "Stage-2 signal feedback visibly communicates rope plus wood 2/salvage 2 in " + localeCode,
                semantic, "upgraded=" + upgraded + " · stage=" + session.SignalStage + " · visible=" + Normalize(combined),
                "Capture the stage-2 no-rope fixture in " + localeCode + " and inspect the signal button plus feedback card.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");
            AddReadability(checks, "W6-08." + localeCode + ".signal.readability", new[] { message, buttonText }, path,
                "Stage-2 signal requirements are readable at 1280x800 in " + localeCode);
        }

        private static void CaptureBarrier(KimSurvivalPrototype prototype, PrototypeLocalization localization, string localeCode, List<ContractCheck> checks)
        {
            GameSession session = prototype.Session;
            session.Reset();
            RequireSetup(session.BeginSearch(), "barrier visual fixture begins search");
            localization.SetLocale(localeCode, false);
            InvokePrivate(prototype, "RefreshAll");
            Camera camera = GetPrivateField<Camera>(prototype, "worldCamera");
            camera.transform.position = new Vector3(8f, 0f, -10f);
            InvokePrivate(prototype, "UpdateResourceLabelLayout");

            TMP_Text barrierText = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude)
                .FirstOrDefault(text => text is TextMeshPro &&
                                        (HasAncestorContaining(text.transform, "숲길") ||
                                         ContainsAnyIgnoreCase(text.text, "rope", "axe", "밧줄", "도끼")));
            RequireSetup(barrierText != null, "localized barrier text is present");
            string screenshot = "wave6-" + localeCode + "-axe-barrier-1280x800.png";
            string path = Path.Combine(EvidenceFolder, screenshot);
            prototype.CaptureVerificationPng(path, Wave3VisualGate.Width, Wave3VisualGate.Height);

            string visible = barrierText.text;
            bool semantic = localeCode == PrototypeLocalization.KoreanLocaleCode
                ? ContainsAny(visible, "돌도끼", "도끼") && !ContainsAny(visible, "밧줄")
                : ContainsAllIgnoreCase(visible, "axe") && !ContainsAnyIgnoreCase(visible, "rope");
            AddObservedProduct(checks, "W6-08." + localeCode + ".barrier.semantic", "8", "P0",
                "The vine/wood barrier visibly requires a stone axe, not rope, in " + localeCode,
                semantic, "visible=" + Normalize(visible),
                "Begin search without tools, frame the barrier at 1280x800, and read its world label.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");
            AddReadability(checks, "W6-08." + localeCode + ".barrier.readability", new[] { barrierText }, path,
                "Barrier requirement is readable at 1280x800 in " + localeCode);
        }

        private static void AddReadability(List<ContractCheck> checks, string id, TMP_Text[] targets, string screenshotPath, string expected)
        {
            try
            {
                Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
                Wave3VisualGate.FrameResult frame = Wave3VisualGate.Analyze(id, screenshotPath, camera, targets);
                List<Wave3VisualGate.TextMetric> metrics = frame.Metrics.ToList();
                bool passed = metrics.Count == targets.Length && metrics.All(metric =>
                    metric.GlyphMedianPixels >= 18f &&
                    metric.Bounds.xMin >= 4f && metric.Bounds.yMin >= 4f &&
                    metric.Bounds.xMax <= Wave3VisualGate.Width - 4f && metric.Bounds.yMax <= Wave3VisualGate.Height - 4f &&
                    !metric.Overflow && metric.ContrastRatio >= (metric.GlyphMedianPixels >= 24f ? 3f : 4.5f));
                string actual = string.Join(" | ", metrics.Select(metric =>
                    Normalize(metric.Value) + " glyph=" + metric.GlyphMedianPixels.ToString("0.0") +
                    "px bounds=" + metric.Bounds + " contrast=" + metric.ContrastRatio.ToString("0.0") +
                    " overflow=" + metric.Overflow));
                AddObservedProduct(checks, id, "8", "P1", expected, passed, actual,
                    "Open the corresponding exact 1280x800 screenshot and compare the recorded projected TMP bounds.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");
            }
            catch (Exception exception)
            {
                checks.Add(InfrastructureFailure(id, "8", "P1", expected, exception,
                    "Open the scene and rerun Wave6ProgressionRegressionRunner.RunPlayContracts."));
            }
        }

        private static Observation VerifySignalStageOne()
        {
            GameSession session = PrepareWorkbench(2, 2);
            bool upgraded = session.TryUpgradeSignal();
            bool passed = upgraded && session.SignalStage == 1 && !session.HasRope &&
                          session.GetStorage(ResourceKind.Wood) == 0 && session.GetStorage(ResourceKind.Salvage) == 0 &&
                          session.Result == RunResult.None && session.LastMessage.Key == "message.signal.stage1";
            return Observation.Product(passed,
                "upgraded=" + upgraded + " stage=" + session.SignalStage + " rope=" + session.HasRope +
                " wood=" + session.GetStorage(ResourceKind.Wood) + " salvage=" + session.GetStorage(ResourceKind.Salvage) +
                " result=" + session.Result + " message=" + session.LastMessage.Key);
        }

        private static Observation VerifySignalStageTwoRequiresRope()
        {
            GameSession session = PrepareStageOne();
            session.Grant(ResourceKind.Wood, 2);
            session.Grant(ResourceKind.Salvage, 2);
            int woodBefore = session.GetStorage(ResourceKind.Wood);
            int salvageBefore = session.GetStorage(ResourceKind.Salvage);
            bool upgraded = session.TryUpgradeSignal();
            bool passed = !upgraded && session.SignalStage == 1 && !session.HasRope &&
                          session.LastMessage.Key == "message.signal.rope" &&
                          session.GetStorage(ResourceKind.Wood) == woodBefore && session.GetStorage(ResourceKind.Salvage) == salvageBefore;
            return Observation.Product(passed,
                "upgraded=" + upgraded + " stage=" + session.SignalStage + " rope=" + session.HasRope +
                " wood=" + session.GetStorage(ResourceKind.Wood) + " salvage=" + session.GetStorage(ResourceKind.Salvage) +
                " message=" + session.LastMessage.Key);
        }

        private static Observation VerifySignalStageTwoRescue()
        {
            GameSession session = PrepareStageOne();
            session.Grant(ResourceKind.Wood, 3);
            session.Grant(ResourceKind.Salvage, 4);
            RequireSetup(session.TryResearch(TechKind.Rope), "rope research fixture");
            RequireSetup(session.TryCraft(TechKind.Rope), "rope craft fixture");
            int woodBefore = session.GetStorage(ResourceKind.Wood);
            int salvageBefore = session.GetStorage(ResourceKind.Salvage);
            bool upgraded = session.TryUpgradeSignal();
            bool passed = woodBefore == 2 && salvageBefore == 2 && upgraded && session.SignalStage == 2 &&
                          session.HasRope && session.Result == RunResult.Rescued && session.Phase == GamePhase.Result &&
                          session.GetStorage(ResourceKind.Wood) == 0 && session.GetStorage(ResourceKind.Salvage) == 0;
            return Observation.Product(passed,
                "before wood/salvage=" + woodBefore + "/" + salvageBefore + " upgraded=" + upgraded +
                " stage=" + session.SignalStage + " rope=" + session.HasRope + " result=" + session.Result + " phase=" + session.Phase);
        }

        private static Observation VerifyShortageFeedback(string shortage)
        {
            GameSession session;
            if (shortage == "workbench")
            {
                session = new GameSession();
                session.Grant(ResourceKind.Salvage, 2);
            }
            else if (shortage == "wood")
            {
                session = PrepareWorkbench(0, 2);
            }
            else
            {
                session = PrepareWorkbench(2, 0);
            }

            bool upgraded = session.TryUpgradeSignal();
            string ko;
            string en;
            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                ko = localization.Format(session.LastMessage);
                localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                en = localization.Format(session.LastMessage);
            }

            bool identifies = shortage == "workbench"
                ? ContainsAny(ko, "작업대") && ContainsAnyIgnoreCase(en, "workbench")
                : shortage == "wood"
                    ? ContainsAny(ko, "나무") && ContainsAnyIgnoreCase(en, "wood")
                    : ContainsAny(ko, "표류물") && ContainsAnyIgnoreCase(en, "salvage");
            return Observation.Product(!upgraded && identifies,
                "shortage=" + shortage + " upgraded=" + upgraded + " messageKey=" + session.LastMessage.Key +
                " ko=" + Normalize(ko) + " en=" + Normalize(en));
        }

        private static Observation VerifyRopeOnlyBarrier()
        {
            GameSession session = PrepareTool(TechKind.Rope);
            RequireSetup(session.BeginSearch(), "rope-only search begins");
            PrototypePlayerTraversal traversal = new PrototypePlayerTraversal();
            traversal.Reset(7.7f, PrototypePlayerTraversal.LandY);
            PrototypeTraversalStep step = traversal.Step(new PrototypePlayerActions(1f, false, false, false, false, -1), 1f, 0f, session);
            bool passed = session.HasRope && !session.HasAxe && traversal.X <= BarrierLockedMaximumX + 0.001f && step.ReachedBlockedPath;
            return Observation.Product(passed,
                "rope=" + session.HasRope + " axe=" + session.HasAxe + " x=" + traversal.X.ToString("0.00") + " blockedNotice=" + step.ReachedBlockedPath);
        }

        private static Observation VerifyAxeOnlyBarrier()
        {
            GameSession session = PrepareTool(TechKind.StoneAxe);
            RequireSetup(session.BeginSearch(), "axe-only search begins");
            PrototypePlayerTraversal traversal = new PrototypePlayerTraversal();
            traversal.Reset(7.7f, PrototypePlayerTraversal.LandY);
            PrototypeTraversalStep step = traversal.Step(new PrototypePlayerActions(1f, false, false, false, false, -1), 1f, 0f, session);
            bool passed = session.HasAxe && !session.HasRope && traversal.X > BarrierLockedMaximumX + 0.001f && !step.ReachedBlockedPath;
            return Observation.Product(passed,
                "rope=" + session.HasRope + " axe=" + session.HasAxe + " x=" + traversal.X.ToString("0.00") + " blockedNotice=" + step.ReachedBlockedPath);
        }

        private static Observation VerifyAxeGatherAmount()
        {
            GameSession plain = new GameSession();
            RequireSetup(plain.BeginSearch(), "plain gather begins");
            GatherResult plainResult = plain.TryGather(ResourceKind.Wood, 1);
            int plainWood = BagTotal(plain, ResourceKind.Wood);

            GameSession axe = PrepareTool(TechKind.StoneAxe);
            RequireSetup(axe.BeginSearch(), "axe gather begins");
            GatherResult axeResult = axe.TryGather(ResourceKind.Wood, 1);
            int axeWood = BagTotal(axe, ResourceKind.Wood);
            bool passed = plainResult == GatherResult.Added && axeResult == GatherResult.Added && plainWood == 1 && axeWood == 2 && axeWood - plainWood == 1;
            return Observation.Product(passed,
                "baseAmount=1 plain=" + plainWood + " axe=" + axeWood + " delta=" + (axeWood - plainWood) +
                " messages=" + plain.LastMessage.Key + "/" + axe.LastMessage.Key);
        }

        private static Observation VerifyAxeCopy()
        {
            string[] keys = { "button.research.axe", "button.craft.axe", "message.research.axe", "message.craft.axe", "message.gather.axe" };
            List<string> samples = new List<string>();
            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                foreach (string locale in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode })
                {
                    localization.SetLocale(locale, false);
                    samples.AddRange(keys.Select(key => locale + ":" + key + "=" + localization.Format(key)));
                }
            }

            Regex forbidden = new Regex(@"(?:2\s*배|두\s*배|\b2\s*x\b|\btwice\b|\bdouble\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            string[] matches = samples.Where(sample => forbidden.IsMatch(sample)).ToArray();
            return Observation.Product(matches.Length == 0,
                matches.Length == 0 ? "no multiplicative claim in " + samples.Count + " localized axe strings" : "forbidden=" + string.Join(" | ", matches.Select(Normalize)));
        }

        private static Observation VerifySignalLocalizationSemantics()
        {
            string koProgress;
            string enProgress;
            string koMaterials;
            string enMaterials;
            string koRope;
            string enRope;
            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                koProgress = localization.Format("button.signal.progress", 1);
                koMaterials = localization.Format("message.signal.materials");
                koRope = localization.Format("message.signal.rope");
                localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                enProgress = localization.Format("button.signal.progress", 1);
                enMaterials = localization.Format("message.signal.materials");
                enRope = localization.Format("message.signal.rope");
            }

            bool passed = ContainsAll(koProgress + koMaterials, "나무", "표류물", "2") &&
                          ContainsAllIgnoreCase(enProgress + enMaterials, "wood", "salvage", "2") &&
                          ContainsAny(koMaterials, "작업대") && ContainsAnyIgnoreCase(enMaterials, "workbench") &&
                          ContainsAny(koRope, "밧줄") && ContainsAnyIgnoreCase(enRope, "rope");
            return Observation.Product(passed,
                "ko=" + Normalize(koProgress + " | " + koMaterials + " | " + koRope) +
                " en=" + Normalize(enProgress + " | " + enMaterials + " | " + enRope));
        }

        private static Observation VerifyBalanceProfile()
        {
            GameSession session = new GameSession();
            int food = session.GetStorage(ResourceKind.Food);
            float hungerStart = session.Hunger;
            RequireSetup(session.BeginSearch(), "balance probe search begins");
            RequireSetup(session.ReturnToCamp(false), "balance probe returns");
            RequireSetup(session.EndDay(), "balance probe settlement");
            float drain = hungerStart - session.Hunger;
            bool passed = food == 0 && Mathf.Approximately(hungerStart, 70f) && Mathf.Approximately(drain, 35f);
            return Observation.Product(passed,
                "food=" + food + " hungerStart=" + hungerStart.ToString("0.0") + " dayDrain=" + drain.ToString("0.0") + " hungerAfter=" + session.Hunger.ToString("0.0"));
        }

        private static Observation VerifyNaturalRescueNoCheats()
        {
            string source = File.ReadAllText(Path.Combine(ProjectRoot, "Assets", "Editor", "ParallelQA", "Wave6ProgressionRegressionRunner.cs"));
            string methodSource = ExtractMethodSource(source, "private static Observation VerifyNaturalRescueNoCheats()", "private static Observation VerifyExhaustion()");
            string grantCall = "." + "Grant" + "(";
            string warpCall = "." + "Warp" + "(";
            bool sourceClean = !methodSource.Contains(grantCall, StringComparison.Ordinal) && !methodSource.Contains(warpCall, StringComparison.Ordinal);

            GameSession session = new GameSession();
            RequireSetup(session.BeginSearch(), "day 1 search");
            RequireSetup(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 1 wood");
            RequireSetup(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 1 salvage A");
            RequireSetup(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 1 salvage B");
            RequireSetup(session.TryGather(ResourceKind.Stone, 2) == GatherResult.Added, "day 1 stone");
            RequireSetup(session.ReturnToCamp(false), "day 1 return");
            RequireSetup(session.TryBuild(StructureKind.Workbench), "workbench");
            RequireSetup(session.TryResearch(TechKind.Rope) && session.TryCraft(TechKind.Rope), "rope");
            RequireSetup(session.TryResearch(TechKind.StoneAxe) && session.TryCraft(TechKind.StoneAxe), "stone axe");
            RequireSetup(session.EndDay(), "day 1 settlement");

            RequireSetup(session.BeginSearch(), "day 2 search");
            RequireSetup(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 2 wood A");
            RequireSetup(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 2 wood B");
            RequireSetup(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 2 salvage A");
            RequireSetup(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.PendingSwap, "day 2 overflow");
            RequireSetup(session.ReplaceBagSlot(0), "day 2 replace");
            RequireSetup(session.ReturnToCamp(false) && session.EndDay(), "day 2 return/settlement");

            RequireSetup(session.BeginSearch(), "day 3 search");
            RequireSetup(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 3 wood A");
            RequireSetup(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 3 wood B");
            RequireSetup(session.TryGather(ResourceKind.Stone, 2) == GatherResult.Added, "day 3 stone");
            RequireSetup(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.PendingSwap, "day 3 overflow");
            RequireSetup(session.ReplaceBagSlot(0), "day 3 replace");
            RequireSetup(session.ReturnToCamp(false), "day 3 return");
            RequireSetup(session.TryBuild(StructureKind.Campfire), "campfire");
            RequireSetup(session.TryBuild(StructureKind.RainCollector), "rain collector");
            RequireSetup(session.TryUpgradeSignal() && session.TryUpgradeSignal(), "signal stages");
            bool passed = sourceClean && session.Day == 3 && session.Result == RunResult.Rescued && session.Phase == GamePhase.Result;
            return Observation.Product(passed,
                "sourceGrantWarpFree=" + sourceClean + " day=" + session.Day + " result=" + session.Result + " phase=" + session.Phase);
        }

        private static Observation VerifyExhaustion()
        {
            GameSession session = new GameSession();
            RequireSetup(session.BeginSearch(), "exhaustion search");
            RequireSetup(session.SetSwimming(true), "exhaustion swimming");
            session.TickSearch(200f, true);
            return Observation.Product(session.Result == RunResult.Exhausted && session.Phase == GamePhase.Result,
                "result=" + session.Result + " phase=" + session.Phase + " energy=" + session.Energy.ToString("0.0"));
        }

        private static Observation VerifyDeadline()
        {
            GameSession session = new GameSession();
            for (int day = 1; day <= GameSession.FinalDay; day += 1)
            {
                RequireSetup(session.BeginSearch(), "deadline search " + day);
                RequireSetup(session.ReturnToCamp(false), "deadline return " + day);
                RequireSetup(session.EndDay(), "deadline settlement " + day);
            }
            return Observation.Product(session.Result == RunResult.Deadline && session.Phase == GamePhase.Result,
                "day=" + session.Day + " result=" + session.Result + " phase=" + session.Phase);
        }

        private static GameSession PrepareWorkbench(int finalWood, int finalSalvage)
        {
            GameSession session = new GameSession();
            session.Grant(ResourceKind.Salvage, finalSalvage + 1);
            RequireSetup(session.TryBuild(StructureKind.Workbench), "workbench fixture");
            if (finalWood > 0) session.Grant(ResourceKind.Wood, finalWood);
            RequireSetup(session.GetStorage(ResourceKind.Wood) == finalWood && session.GetStorage(ResourceKind.Salvage) == finalSalvage,
                "workbench fixture exact storage");
            return session;
        }

        private static GameSession PrepareStageOne()
        {
            GameSession session = PrepareWorkbench(2, 2);
            RequireSetup(session.TryUpgradeSignal() && session.SignalStage == 1, "stage-one fixture");
            return session;
        }

        private static void PrepareStageOne(GameSession session)
        {
            session.Reset();
            session.Grant(ResourceKind.Salvage, 3);
            RequireSetup(session.TryBuild(StructureKind.Workbench), "stage-one visual workbench");
            session.Grant(ResourceKind.Wood, 2);
            RequireSetup(session.GetStorage(ResourceKind.Wood) == 2 && session.GetStorage(ResourceKind.Salvage) == 2, "stage-one visual materials");
            RequireSetup(session.TryUpgradeSignal() && session.SignalStage == 1, "stage-one visual upgrade");
        }

        private static GameSession PrepareTool(TechKind tool)
        {
            GameSession session = new GameSession();
            if (tool == TechKind.Rope)
            {
                session.Grant(ResourceKind.Salvage, 3);
                RequireSetup(session.TryBuild(StructureKind.Workbench), "rope fixture workbench");
                session.Grant(ResourceKind.Wood, 1);
            }
            else
            {
                session.Grant(ResourceKind.Salvage, 2);
                session.Grant(ResourceKind.Stone, 1);
                RequireSetup(session.TryBuild(StructureKind.Workbench), "axe fixture workbench");
                session.Grant(ResourceKind.Wood, 1);
            }
            RequireSetup(session.TryResearch(tool), tool + " fixture research");
            RequireSetup(session.TryCraft(tool), tool + " fixture craft");
            return session;
        }

        private static int BagTotal(GameSession session, ResourceKind kind)
        {
            int total = 0;
            for (int i = 0; i < GameSession.BagSlotCount; i += 1)
            {
                BagStack stack = session.GetBagSlot(i);
                if (!stack.IsEmpty && stack.Kind == kind) total += stack.Amount;
            }
            return total;
        }

        private static void Product(List<ContractCheck> checks, string id, string matrixItem, string severity, string expected,
            Func<Observation> action, string reproduction, string recommendedFiles)
        {
            try
            {
                Observation observation = action();
                AddObservedProduct(checks, id, matrixItem, severity, expected, observation.Passed, observation.Actual, reproduction, recommendedFiles);
            }
            catch (Exception exception)
            {
                checks.Add(new ContractCheck
                {
                    id = id,
                    matrixItem = matrixItem,
                    status = "FAIL",
                    classification = "PRODUCT_DEFECT",
                    severity = severity,
                    expected = expected,
                    actual = "fixture/action failed: " + exception.GetType().Name + " · " + exception.Message,
                    reproduction = reproduction,
                    recommendedFiles = recommendedFiles
                });
            }
        }

        private static void AddObservedProduct(List<ContractCheck> checks, string id, string matrixItem, string severity,
            string expected, bool passed, string actual, string reproduction, string recommendedFiles)
        {
            checks.Add(new ContractCheck
            {
                id = id,
                matrixItem = matrixItem,
                status = passed ? "PASS" : "FAIL",
                classification = passed ? "NONE" : "PRODUCT_DEFECT",
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = recommendedFiles
            });
        }

        private static ContractCheck InfrastructureFailure(string id, string matrixItem, string severity, string expected, Exception exception, string reproduction)
        {
            return new ContractCheck
            {
                id = id,
                matrixItem = matrixItem,
                status = "INFRA_FAIL",
                classification = "TEST_INFRASTRUCTURE",
                severity = severity,
                expected = expected,
                actual = exception.GetType().Name + " · " + exception.Message,
                reproduction = reproduction,
                recommendedFiles = "Assets/Editor/ParallelQA/Wave6ProgressionRegressionRunner.cs"
            };
        }

        private static ContractReport WriteReport(string prefix, string title, DateTime started, List<ContractCheck> checks)
        {
            Directory.CreateDirectory(EvidenceFolder);
            ContractReport report = new ContractReport
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                command = string.Join(" ", Environment.GetCommandLineArgs().Select(Quote)),
                passed = checks.Count(check => check.status == "PASS"),
                failed = checks.Count(check => check.status == "FAIL"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"),
                infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL"),
                checks = checks.ToArray()
            };
            report.productOverall = report.failed == 0 ? "PASS" : "FAIL";
            report.infrastructureOverall = report.infrastructureFailed == 0 ? "PASS" : "FAIL";
            report.overall = report.productOverall == "PASS" && report.infrastructureOverall == "PASS" ? "PASS" : "FAIL";
            File.WriteAllText(Path.Combine(EvidenceFolder, prefix + ".json"), JsonUtility.ToJson(report, true) + Environment.NewLine, new UTF8Encoding(false));

            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + report.runId);
            text.AppendLine("Baseline: " + report.baselineCommit);
            text.AppendLine("Unity: " + report.unityVersion);
            text.AppendLine("Product: " + report.productOverall);
            text.AppendLine("Infrastructure: " + report.infrastructureOverall);
            text.AppendLine("Counts: PASS=" + report.passed + " FAIL=" + report.failed + " INFRA_FAIL=" + report.infrastructureFailed + " UNVERIFIED=" + report.unverified);
            foreach (ContractCheck check in checks)
            {
                text.AppendLine(check.status + " · " + check.classification + " · " + check.severity + " · " + check.id + " · " + Normalize(check.actual));
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, prefix + ".txt"), text.ToString(), new UTF8Encoding(false));
            return report;
        }

        private static void WriteInfrastructureFailure(string prefix, Exception exception)
        {
            List<ContractCheck> checks = new List<ContractCheck>
            {
                InfrastructureFailure("W6-INFRA.play_execution", "8", "P0", "Play Mode scene and capture runner completes", exception,
                    "Run Wave6ProgressionRegressionRunner.RunPlayContracts outside the Codex sandbox.")
            };
            WriteReport(prefix, "Wave 6 Play infrastructure failure", DateTime.UtcNow, checks);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            RequireSetup(field != null, "private field exists: " + fieldName);
            object value = field.GetValue(target);
            RequireSetup(value is T, "private field type: " + fieldName + " -> " + typeof(T).Name);
            return (T)value;
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            RequireSetup(method != null, "private method exists: " + methodName);
            try
            {
                method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static bool HasAncestorContaining(Transform transform, string value)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                current = current.parent;
            }
            return false;
        }

        private static string ExtractMethodSource(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, StringComparison.Ordinal);
            int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
            RequireSetup(start >= 0 && end > start, "natural-route source markers");
            return source.Substring(start, end - start);
        }

        private static bool ContainsAll(string value, params string[] terms)
        {
            return terms.All(term => (value ?? string.Empty).Contains(term, StringComparison.Ordinal));
        }

        private static bool ContainsAllIgnoreCase(string value, params string[] terms)
        {
            return terms.All(term => (value ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            return terms.Any(term => (value ?? string.Empty).Contains(term, StringComparison.Ordinal));
        }

        private static bool ContainsAnyIgnoreCase(string value, params string[] terms)
        {
            return terms.Any(term => (value ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void RequireSetup(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string Quote(string value)
        {
            return value.IndexOf(' ') >= 0 ? "\"" + value.Replace("\"", "\\\"") + "\"" : value;
        }

        private static string Sanitize(string value)
        {
            string result = value ?? string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars()) result = result.Replace(invalid, '_');
            return result.Replace('.', '_').Replace(' ', '_');
        }
    }
}
