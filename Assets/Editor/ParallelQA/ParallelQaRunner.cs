using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using KimSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Independent, non-shipping QA harness. All reports are isolated from
    /// Artifacts/Verification and all player binaries are emitted under ignored work/.
    /// </summary>
    public static class ParallelQaRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RunningKey = "ParallelQA.PlayMode.Running";
        private const string PassedKey = "ParallelQA.PlayMode.Passed";
        private const string MessageKey = "ParallelQA.PlayMode.Message";
        private static bool tickAttached;
        private static double earliestRunTime;
        private static double timeoutAt;

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual" : Sanitize(value);
            }
        }

        private static string EvidenceFolder
        {
            get { return Path.GetFullPath(Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId)); }
        }

        private static string BuildFolder
        {
            get { return Path.GetFullPath(Path.Combine(ProjectRoot, "work", "ParallelQA", "StableWindowsBuild")); }
        }

        private static string WorkFolder
        {
            get { return Path.GetFullPath(Path.Combine(ProjectRoot, "work", "ParallelQA", RunId)); }
        }

        private static string BaselineCommit
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_BASELINE");
                return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
            }
        }

        private static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        public static void RecordCompilePass()
        {
            Directory.CreateDirectory(EvidenceFolder);
            DateTime started = DateTime.UtcNow;
            string[] arguments = Environment.GetCommandLineArgs();
            int logIndex = Array.FindIndex(arguments, argument => string.Equals(argument, "-logFile", StringComparison.OrdinalIgnoreCase));
            string logPath = logIndex >= 0 && logIndex + 1 < arguments.Length ? arguments[logIndex + 1] : string.Empty;
            string[] compileLogs = Directory.Exists(WorkFolder)
                ? Directory.GetFiles(WorkFolder, "unity-compile*.log").OrderBy(path => path).ToArray()
                : Array.Empty<string>();
            if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath) && !compileLogs.Contains(logPath, StringComparer.OrdinalIgnoreCase))
            {
                compileLogs = compileLogs.Concat(new[] { logPath }).ToArray();
            }
            string log = string.Join(Environment.NewLine, compileLogs.Select(ReadSharedText));
            string[] compilerErrors = Regex.Matches(log, @"^.*\): error CS\d+:.*$", RegexOptions.Multiline)
                .Cast<Match>().Select(match => match.Value.Trim()).Distinct().ToArray();
            string[] compilerWarnings = Regex.Matches(log, @"^.*\): warning CS\d+:.*$", RegexOptions.Multiline)
                .Cast<Match>().Select(match => match.Value.Trim()).Distinct().ToArray();
            string report = Header("Unity script compilation", started) +
                            "Result: " + (compilerErrors.Length == 0 ? "PASS" : "FAIL") + Environment.NewLine +
                            "Compiler errors: " + compilerErrors.Length + Environment.NewLine +
                            "Compiler warnings: " + compilerWarnings.Length + Environment.NewLine +
                            "Raw logs: " + (compileLogs.Length == 0 ? "<none found>" : string.Join(" | ", compileLogs)) + Environment.NewLine +
                            (compilerWarnings.Length == 0 ? string.Empty : "Warning inventory:" + Environment.NewLine + string.Join(Environment.NewLine, compilerWarnings) + Environment.NewLine) +
                            "Scope: Unity reached the independent QA execute method after script compilation; diagnostics were counted from the command's raw Unity log." + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "compile-result.txt"), report, new UTF8Encoding(false));
            Require(compilerErrors.Length == 0, "Unity compiler errors are zero");
        }

        public static void PrepareLocalePersistenceProbe()
        {
            Directory.CreateDirectory(EvidenceFolder);
            Directory.CreateDirectory(WorkFolder);
            DateTime started = DateTime.UtcNow;
            bool hadPreference = PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey);
            string originalPreference = PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode);
            File.WriteAllText(
                Path.Combine(WorkFolder, "locale-preference-original.txt"),
                (hadPreference ? "1" : "0") + Environment.NewLine + originalPreference,
                new UTF8Encoding(false));

            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                Require(localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, true), "persist English for a new Unity process");
                Require(PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey) == PrototypeLocalization.EnglishLocaleCode, "English preference written");
            }

            string report = Header("Locale relaunch persistence stage 1", started) +
                            "PASS · English locale persisted for the next Unity process." + Environment.NewLine +
                            "Next expected locale: en" + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "locale-relaunch-stage1.txt"), report, new UTF8Encoding(false));
        }

        public static void VerifyLocalePersistenceProbe()
        {
            Directory.CreateDirectory(EvidenceFolder);
            DateTime started = DateTime.UtcNow;
            string originalPath = Path.Combine(WorkFolder, "locale-preference-original.txt");
            bool passed = false;
            string observedLocale = "<not initialized>";
            string observedTitle = "<not initialized>";
            try
            {
                using (PrototypeLocalization localization = new PrototypeLocalization())
                {
                    observedLocale = localization.CurrentLocaleCode;
                    observedTitle = localization.Format("ui.camp.title");
                    passed = observedLocale == PrototypeLocalization.EnglishLocaleCode &&
                             observedTitle == "Base Camp · Craft / Build / Research";
                }
            }
            finally
            {
                if (File.Exists(originalPath))
                {
                    string[] original = File.ReadAllLines(originalPath);
                    if (original.Length > 0 && original[0] == "1")
                    {
                        PlayerPrefs.SetString(PrototypeLocalization.PreferenceKey, original.Length > 1 ? original[1] : PrototypeLocalization.KoreanLocaleCode);
                    }
                    else
                    {
                        PlayerPrefs.DeleteKey(PrototypeLocalization.PreferenceKey);
                    }
                    PlayerPrefs.Save();
                }
            }

            string report = Header("Locale relaunch persistence stage 2", started) +
                            (passed ? "PASS" : "FAIL") + " · A fresh Unity process restored the saved English locale." + Environment.NewLine +
                            "Observed locale: " + observedLocale + Environment.NewLine +
                            "Observed camp title: " + observedTitle + Environment.NewLine +
                            "Scope: separate Unity Editor batch processes; Windows Player preference actuation remains a manual check." + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "locale-relaunch-persistence.txt"), report, new UTF8Encoding(false));
            Require(passed, "fresh Unity process restores persisted English locale");
        }

        [InitializeOnLoadMethod]
        private static void ResumePlayModeRun()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachPlayModeCallbacks();
            }
        }

        public static void RunEditChecks()
        {
            Directory.CreateDirectory(EvidenceFolder);
            DateTime started = DateTime.UtcNow;
            List<string> results = new List<string>();

            Check(results, "Natural three-day resource route can rescue without Grant cheats", VerifyNaturalThreeDayModel);
            Check(results, "Bag overflow creates an explicit replace-or-discard choice", VerifyBagChoice);
            Check(results, "Swimming entry, extra cost, water gather, and land exit work", VerifySwimmingModel);
            Check(results, "Exhaustion and day-three deadline both reach explained results", VerifyFailureOutcomes);
            Check(results, "Limited free placement enforces bounds, overlap, entrance, path, cancel, one-time cost, and free relocation", VerifyPlacementModel);
            Check(results, "Keyboard/mouse and gamepad raw inputs converge on shared player, placement, and language actions", VerifySharedInputModel);
            Check(results, "Korean default, immediate ko/en switching, Smart Strings, missing-key fallback/logging, and preference storage work", VerifyLocalizationModel);
            Check(results, "ko/en table parity, Smart entries, font mappings, and required glyph prerequisites are present", VerifyLocalizationAssets);
            Check(results, "Rescue signal remains on a dedicated anchor outside general facility placement", VerifyDedicatedSignalAnchor);

            string report = Header("Deterministic Edit Check", started) +
                            string.Join(Environment.NewLine, results) + Environment.NewLine +
                            "Overall: " + (results.All(line => line.StartsWith("PASS", StringComparison.Ordinal)) ? "PASS" : "FAIL") + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "edit-checks.txt"), report, new UTF8Encoding(false));

            WriteInputCodePathAudit(started);
            WriteHardcodedPlayerStringAudit(started);
            WriteLocaleExpansionReadiness(started);
            if (results.Any(line => line.StartsWith("FAIL", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Parallel QA deterministic Edit Check failed. See " + EvidenceFolder);
            }
        }

        public static void RunPlayModeVerification()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "FAIL · Play Mode verification did not complete.");
            AttachPlayModeCallbacks();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWindowsDevelopmentPlayer()
        {
            Directory.CreateDirectory(EvidenceFolder);
            if (Directory.Exists(BuildFolder))
            {
                Directory.Delete(BuildFolder, true);
            }
            Directory.CreateDirectory(BuildFolder);
            DateTime started = DateTime.UtcNow;
            string executable = Path.Combine(BuildFolder, "KimSurvivalIsland.exe");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            List<string> inventory = new List<string>();
            if (Directory.Exists(BuildFolder))
            {
                foreach (string path in Directory.GetFiles(BuildFolder, "*", SearchOption.AllDirectories).OrderBy(path => path))
                {
                    FileInfo info = new FileInfo(path);
                    inventory.Add(Path.GetRelativePath(BuildFolder, path) + " | " + info.Length + " bytes | sha256 " + Sha256(path));
                }
            }

            string text = Header("Windows x64 Development Build", started) +
                          "Result: " + summary.result + Environment.NewLine +
                          "BuildOptions: Development" + Environment.NewLine +
                          "Target: StandaloneWindows64" + Environment.NewLine +
                          "Output (ignored local binary): " + executable + Environment.NewLine +
                          "Total size: " + summary.totalSize + " bytes" + Environment.NewLine +
                          "Duration: " + summary.totalTime + Environment.NewLine +
                          "Errors: " + summary.totalErrors + Environment.NewLine +
                          "Warnings: " + summary.totalWarnings + Environment.NewLine +
                          "Executable exists: " + File.Exists(executable) + Environment.NewLine +
                          "File inventory:" + Environment.NewLine + string.Join(Environment.NewLine, inventory) + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "windows-development-build.txt"), text, new UTF8Encoding(false));

            if (summary.result != BuildResult.Succeeded || !File.Exists(executable))
            {
                throw new InvalidOperationException("Parallel QA Windows development build failed. See " + EvidenceFolder);
            }
        }

        private static void AttachPlayModeCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (!tickAttached)
            {
                EditorApplication.update += PlayModeTick;
                tickAttached = true;
            }

            earliestRunTime = EditorApplication.timeSinceStartup + 2d;
            timeoutAt = EditorApplication.timeSinceStartup + 90d;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                earliestRunTime = EditorApplication.timeSinceStartup + 2d;
                timeoutAt = EditorApplication.timeSinceStartup + 90d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishPlayModeRun();
            }
        }

        private static void PlayModeTick()
        {
            if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < earliestRunTime)
            {
                return;
            }

            if (now > timeoutAt)
            {
                SessionState.SetString(MessageKey, "FAIL · Timed out waiting for playable scene.");
                StopPlayMode();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(EvidenceFolder);
                string message = RunNaturalPlayModeLoop(prototype);
                SessionState.SetBool(PassedKey, true);
                SessionState.SetString(MessageKey, message);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, "FAIL · " + exception);
            }

            StopPlayMode();
        }

        private static string RunNaturalPlayModeLoop(KimSurvivalPrototype prototype)
        {
            DateTime started = DateTime.UtcNow;
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetPrivateField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetPrivateField<PrototypeCampPlacement>(prototype, "campPlacement");
            List<string> layoutAudit = new List<string>();
            List<Wave3VisualGate.FrameResult> wave3Frames = new List<Wave3VisualGate.FrameResult>();

            session.Reset();
            placement.Reset();
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            InvokePrivate(prototype, "RefreshAll");
            Require(EventSystem.current != null, "EventSystem exists");
            Require(EventSystem.current.currentSelectedGameObject != null, "Camp UI has a selected control");
            int initialReachableButtons = VerifyDirectionalNavigationFromCurrentSelection();

            Button languageButton = GetButton(prototype, "languageButton");
            TMP_Text actionTitle = GetPrivateField<TMP_Text>(prototype, "actionTitleText");
            Submit(languageButton);
            Require(localization.CurrentLocaleCode == PrototypeLocalization.EnglishLocaleCode && actionTitle.text == "Base Camp · Craft / Build / Research", "UI Submit switches to English immediately");
            Submit(languageButton);
            Require(localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode && actionTitle.text == "베이스캠프 · 제작 / 건설 / 연구", "UI Submit switches back to Korean immediately");

            RunPlacementVisualProbe(prototype, PrototypeLocalization.KoreanLocaleCode, PrototypeInputDevice.KeyboardMouse, layoutAudit, wave3Frames);
            RunPlacementVisualProbe(prototype, PrototypeLocalization.EnglishLocaleCode, PrototypeInputDevice.Gamepad, layoutAudit, wave3Frames);
            RunPseudoLongPlacementProbe(prototype, layoutAudit, wave3Frames);

            string koreanLoop = RunLocalizedNaturalLoop(prototype, PrototypeLocalization.KoreanLocaleCode, "ko", layoutAudit, wave3Frames);
            string englishLoop = RunLocalizedNaturalLoop(prototype, PrototypeLocalization.EnglishLocaleCode, "en", layoutAudit, wave3Frames);

            bool wave3VisualPass = Wave3VisualGate.WriteReports(
                EvidenceFolder,
                RunId,
                BaselineCommit,
                Application.unityVersion,
                string.Join(" ", Environment.GetCommandLineArgs().Select(Quote)),
                started,
                wave3Frames);

            string[] joysticks = Input.GetJoystickNames() ?? Array.Empty<string>();
            int activeJoysticks = joysticks.Count(name => !string.IsNullOrWhiteSpace(name));

            File.WriteAllText(
                Path.Combine(EvidenceFolder, "playmode-layout-metrics.txt"),
                Header("Play Mode 1280x800 text metrics", started) + string.Join(Environment.NewLine, layoutAudit) + Environment.NewLine,
                new UTF8Encoding(false));

            return Header("Play Mode natural full-loop verification", started) +
                   "PASS · No Grant, Warp, or Skip calls used by either localized natural full-loop route; resource grants are isolated to the placement-only visual fixture." + Environment.NewLine +
                   "PASS · Korean and English each completed Day 1-3 camp/search/return/settlement and rescue." + Environment.NewLine +
                   "PASS · Each locale exercised limited placement, crafting/research, overflow replacement, shore entry, water gather, shore exit, and signal completion." + Environment.NewLine +
                   "PASS · UI Submit switched language and invoked camp actions, placement entry, bag replacement, and result actions." + Environment.NewLine +
                   "PASS · Directional navigation reached every enabled initial camp control (" + initialReachableButtons + ")." + Environment.NewLine +
                   "PASS · ko/en placement, pseudo-long, and full-loop exact 1280x800 render-target captures produced." + Environment.NewLine +
                   "Wave 3 projected-text visual gate: " + (wave3VisualPass ? "PASS" : "FAIL (functional Play Mode route still completed; see wave3-visual-gate.txt)") + Environment.NewLine +
                   koreanLoop + Environment.NewLine +
                   englishLoop + Environment.NewLine +
                   "Screen reported by Unity: " + Screen.width + "x" + Screen.height + Environment.NewLine +
                   "Detected non-empty joystick names: " + activeJoysticks + Environment.NewLine +
                   "Joystick names: " + (activeJoysticks == 0 ? "<none>" : string.Join(" | ", joysticks.Where(name => !string.IsNullOrWhiteSpace(name)))) + Environment.NewLine +
                   "Automated gamepad/shared-action execution: PASS (raw action convergence, EventSystem Submit, directional navigation)." + Environment.NewLine +
                   "Physical gamepad execution: " + (activeJoysticks == 0 ? "UNVERIFIED (no device exposed to Unity batch Play Mode)" : "UNVERIFIED (device detected, no human actuation captured)") + Environment.NewLine;
        }

        private static void RunPlacementVisualProbe(
            KimSurvivalPrototype prototype,
            string localeCode,
            PrototypeInputDevice device,
            List<string> layoutAudit,
            List<Wave3VisualGate.FrameResult> wave3Frames)
        {
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetPrivateField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetPrivateField<PrototypeCampPlacement>(prototype, "campPlacement");
            session.Reset();
            placement.Reset();
            session.Grant(ResourceKind.Wood, 10);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Salvage, 10);
            localization.SetLocale(localeCode, false);
            InvokePrivate(prototype, "RefreshAll");

            Submit(GetButton(prototype, "campfireButton"));
            InvokePrivate(prototype, "ApplyPlacementGuidance", device);
            TMP_Text controls = GetPrivateField<TMP_Text>(prototype, "controlsText");
            Require(controls.text.Contains(localization.DeviceName(device)), localeCode + " placement prompt identifies " + device);
            if (device == PrototypeInputDevice.KeyboardMouse)
            {
                Require(controls.text.Contains("Enter") && controls.text.Contains("Esc"), localeCode + " keyboard/mouse placement prompt exposes confirm and cancel");
            }
            else
            {
                Require(controls.text.Contains("left stick") && controls.text.Contains("A") && controls.text.Contains("B"), localeCode + " gamepad placement prompt exposes move, confirm, and cancel");
            }
            placement.SetCandidateX(-1.5f);
            InvokePrivate(prototype, "UpdatePlacementGhost");
            InvokePrivate(prototype, "RefreshHud");
            Require(placement.CurrentValidity == CampPlacementValidity.Valid, localeCode + " placement valid probe");
            CaptureAndAudit(prototype, "playmode-" + localeCode + "-placement-valid-1280x800.png", localeCode + " placement valid", layoutAudit, wave3Frames);
            placement.SetCandidateX(-5f);
            InvokePrivate(prototype, "UpdatePlacementGhost");
            InvokePrivate(prototype, "RefreshHud");
            Require(placement.CurrentValidity == CampPlacementValidity.OutsideCampBounds, localeCode + " placement invalid probe");
            CaptureAndAudit(prototype, "playmode-" + localeCode + "-placement-invalid-1280x800.png", localeCode + " placement invalid", layoutAudit, wave3Frames);
            placement.Cancel();
            InvokePrivate(prototype, "RefreshAll");
            Require(!session.HasStructure(StructureKind.Campfire), localeCode + " placement cancel leaves no structure");
        }

        private static void RunPseudoLongPlacementProbe(
            KimSurvivalPrototype prototype,
            List<string> layoutAudit,
            List<Wave3VisualGate.FrameResult> wave3Frames)
        {
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetPrivateField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetPrivateField<PrototypeCampPlacement>(prototype, "campPlacement");
            PrototypeCampUse campUse = GetPrivateField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction campInteraction = GetPrivateField<PrototypeCampInteraction>(prototype, "campInteraction");
            PrototypeSearchNodeRuntime searchRuntime = GetPrivateField<PrototypeSearchNodeRuntime>(prototype, "searchNodeRuntime");
            PrototypeExpeditionMapSelection mapSelection = GetPrivateField<PrototypeExpeditionMapSelection>(prototype, "expeditionMapSelection");
            session.Reset();
            searchRuntime.Reset(session.RunSeed);
            placement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            session.Grant(ResourceKind.Wood, 10);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Salvage, 10);
            Require(localization.SetQaLocale(), "canonical qps-long locale is available for Wave 3 metrics");
            InvokePrivate(prototype, "RefreshAll");
            Submit(GetButton(prototype, "campfireButton"));
            InvokePrivate(prototype, "ApplyPlacementGuidance", PrototypeInputDevice.Gamepad);
            placement.SetCandidateX(-1.5f);
            InvokePrivate(prototype, "UpdatePlacementGhost");
            InvokePrivate(prototype, "RefreshHud");
            Require(placement.CurrentValidity == CampPlacementValidity.Valid, "pseudo-long placement fixture is valid");
            try
            {
                CaptureAndAudit(prototype, "playmode-qps-long-placement-valid-1280x800.png",
                    "qps-long placement valid", layoutAudit, wave3Frames);

                placement.Cancel();
                InvokePrivate(prototype, "RefreshAll");
                Require(campInteraction.HasProximityPrompt &&
                        GetPrivateField<GameObject>(prototype, "campProximityPrompt").activeSelf,
                    "qps-long compact camp proximity prompt is visible in the current hierarchy");
                CaptureAndAudit(prototype, "playmode-qps-long-camp-proximity-1280x800.png",
                    "qps-long camp proximity", layoutAudit, wave3Frames);

                Require(campInteraction.TryOpenPopup(), "qps-long current camp target opens the compact popup");
                InvokePrivate(prototype, "RefreshAll");
                Require(GetPrivateField<GameObject>(prototype, "campInteractionPopup").activeSelf,
                    "qps-long compact camp popup is visible in the current hierarchy");
                CaptureAndAudit(prototype, "playmode-qps-long-camp-popup-1280x800.png",
                    "qps-long camp popup", layoutAudit, wave3Frames);

                campInteraction.ClosePopup();
                session.Reset();
                searchRuntime.Reset(session.RunSeed);
                campUse.Reset();
                campInteraction.Reset();
                mapSelection.Close();
                InvokePrivate(prototype, "RefreshAll");
                ProductionSearchNodeQaDriver.BeginExpedition(
                    prototype, PrototypeExpeditionRegionId.Beach, "qps-long environmental search");
                ProductionSearchNodeQaDriver.Target target = ProductionSearchNodeQaDriver.MoveToNext(
                    prototype, false, "qps-long search node");
                ProductionSearchNodeQaDriver.Open(prototype, target, "qps-long search node");
                Require(searchRuntime.IsTrayOpen && GetPrivateField<GameObject>(prototype, "searchLootTrayPanel").activeSelf,
                    "qps-long production interaction opens the compact environmental-search tray");
                CaptureAndAudit(prototype, "playmode-qps-long-search-tray-1280x800.png",
                    "qps-long search tray", layoutAudit, wave3Frames);
            }
            finally
            {
                if (searchRuntime.IsTrayOpen) searchRuntime.Close(session);
                placement.Cancel();
                campInteraction.Reset();
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                InvokePrivate(prototype, "RefreshAll");
            }
        }

        private static string RunLocalizedNaturalLoop(
            KimSurvivalPrototype prototype,
            string localeCode,
            string prefix,
            List<string> layoutAudit,
            List<Wave3VisualGate.FrameResult> wave3Frames)
        {
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetPrivateField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetPrivateField<PrototypeCampPlacement>(prototype, "campPlacement");
            PrototypeSearchNodeRuntime searchRuntime = GetPrivateField<PrototypeSearchNodeRuntime>(prototype, "searchNodeRuntime");
            PrototypeCampUse campUse = GetPrivateField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetPrivateField<PrototypeCampInteraction>(prototype, "campInteraction");
            PrototypeExpeditionMapSelection mapSelection = GetPrivateField<PrototypeExpeditionMapSelection>(prototype, "expeditionMapSelection");
            session.Reset();
            searchRuntime.Reset(session.RunSeed);
            placement.Reset();
            campUse.Reset();
            interaction.Reset();
            mapSelection.Close();
            localization.SetLocale(localeCode, false);
            InvokePrivate(prototype, "RefreshAll");
            Require(localization.CurrentLocaleCode == localeCode, prefix + " locale active at loop start");
            int grantBefore = PrototypeProductionActionCounters.GrantCallCount;
            int warpBefore = PrototypeProductionActionCounters.WarpCallCount;
            int skipBefore = PrototypeProductionActionCounters.SkipCallCount;

            ProductionSearchNodeQaDriver.BeginExpedition(
                prototype, PrototypeExpeditionRegionId.Beach, prefix + " day 1 beach route");
            ProductionSearchNodeQaDriver.Target firstBeach = ProductionSearchNodeQaDriver.MoveToNext(
                prototype, false, prefix + " day 1 first beach node");
            ProductionSearchNodeQaDriver.Open(prototype, firstBeach, prefix + " day 1 first beach node");
            CaptureAndAudit(prototype, "playmode-" + prefix + "-search-loot-tray-1280x800.png",
                prefix + " production search loot tray", layoutAudit, wave3Frames);
            ProductionSearchNodeQaDriver.TakeAllAndClose(prototype, prefix + " day 1 first beach node");
            for (int index = 1; index < 6; index += 1)
            {
                ProductionSearchNodeQaDriver.SearchAndTakeAllNext(
                    prototype, false, prefix + " day 1 beach node " + (index + 1));
            }
            ProductionSearchNodeQaDriver.ReturnToCamp(prototype, prefix + " day 1");
            InvokePrivate(prototype, "RefreshAll");
            PlaceViaUi(prototype, "workbenchButton", StructureKind.Workbench, 1.5f, prefix + " workbench");
            Submit(GetButton(prototype, "researchRopeButton"));
            Submit(GetButton(prototype, "craftRopeButton"));
            Require(session.HasRope, prefix + " rope crafted on day 1");
            Submit(GetButton(prototype, "phaseButton"));
            Require(session.Day == 2 && session.Phase == GamePhase.Camp, prefix + " advances to day 2");

            ProductionSearchNodeQaDriver.BeginExpedition(
                prototype, PrototypeExpeditionRegionId.Shallows, prefix + " day 2 shallows route");
            float energyBeforeLandGather = session.Energy;
            ProductionSearchNodeQaDriver.Target firstLand = ProductionSearchNodeQaDriver.MoveToNext(
                prototype, false, prefix + " day 2 first land node");
            ProductionSearchNodeQaDriver.Open(prototype, firstLand, prefix + " day 2 first land node");
            float landGatherCost = energyBeforeLandGather - session.Energy;
            int day2ReplacementCount = ProductionSearchNodeQaDriver.TakeAllAndClose(
                prototype, prefix + " day 2 first land node");
            ProductionSearchNodeQaDriver.Target water = ProductionSearchNodeQaDriver.MoveToNext(
                prototype, true, prefix + " day 2 first water node");
            CaptureAndAudit(prototype, "playmode-" + prefix + "-day1-swimming-1280x800.png", prefix + " day2 swimming", layoutAudit, wave3Frames);
            float energyBeforeWaterGather = session.Energy;
            ProductionSearchNodeQaDriver.Open(prototype, water, prefix + " day 2 first water node");
            float waterGatherCost = energyBeforeWaterGather - session.Energy;
            Require(waterGatherCost > landGatherCost, prefix + " water gather costs more energy than land gather");
            day2ReplacementCount += ProductionSearchNodeQaDriver.TakeAllAndClose(
                prototype, prefix + " day 2 first water node");
            day2ReplacementCount += ProductionSearchNodeQaDriver.SearchAndTakeAllNext(
                prototype, false, prefix + " day 2 second land node");
            day2ReplacementCount += ProductionSearchNodeQaDriver.SearchAndTakeAllNext(
                prototype, false, prefix + " day 2 third land node");
            day2ReplacementCount += ProductionSearchNodeQaDriver.SearchAndTakeAllNext(
                prototype, true, prefix + " day 2 second water node");
            day2ReplacementCount += ProductionSearchNodeQaDriver.SearchAndTakeAllNext(
                prototype, true, prefix + " day 2 third water node");
            Require(day2ReplacementCount > 0 && !session.HasPendingLoot,
                prefix + " day 2 overflow replacement resolves through production bag controls");
            CaptureAndAudit(prototype, "playmode-" + prefix + "-day2-exploration-1280x800.png", prefix + " day2 exploration", layoutAudit, wave3Frames);
            ProductionSearchNodeQaDriver.ReturnToCamp(prototype, prefix + " day 2");
            InvokePrivate(prototype, "RefreshAll");
            Submit(GetButton(prototype, "researchAxeButton"));
            Submit(GetButton(prototype, "craftAxeButton"));
            Require(session.HasAxe && session.HasRope, prefix + " both tools crafted by day 2");
            Submit(GetButton(prototype, "phaseButton"));
            Require(session.Day == 3 && session.Phase == GamePhase.Camp, prefix + " advances to day 3");

            ProductionSearchNodeQaDriver.BeginExpedition(
                prototype, PrototypeExpeditionRegionId.Forest, prefix + " day 3 forest route");
            for (int index = 0; index < 6; index += 1)
            {
                ProductionSearchNodeQaDriver.SearchAndTakeAllNext(
                    prototype, false, prefix + " day 3 forest node " + (index + 1));
            }
            Require(!session.HasPendingLoot, prefix + " day 3 overflow transactions resolved");
            ProductionSearchNodeQaDriver.ReturnToCamp(prototype, prefix + " day 3");
            InvokePrivate(prototype, "RefreshAll");
            PlaceViaUi(prototype, "campfireButton", StructureKind.Campfire, -1.5f, prefix + " campfire");
            PlaceViaUi(prototype, "rainButton", StructureKind.RainCollector, 3.5f, prefix + " rain collector");
            Submit(GetButton(prototype, "signalButton"));
            Submit(GetButton(prototype, "signalButton"));
            Require(session.Result == RunResult.Rescued && session.Phase == GamePhase.Result, prefix + " natural route reaches rescue");
            CaptureAndAudit(prototype, "playmode-" + prefix + "-rescue-result-1280x800.png", prefix + " rescue result", layoutAudit, wave3Frames);
            Require(PrototypeProductionActionCounters.GrantCallCount == grantBefore &&
                    PrototypeProductionActionCounters.WarpCallCount == warpBefore &&
                    PrototypeProductionActionCounters.SkipCallCount == skipBefore,
                prefix + " natural full-loop route used no Grant, Warp, or Skip action");

            TMP_Text[] activeTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);
            Require(activeTexts.All(text => string.IsNullOrEmpty(text.text) || !text.text.Contains("⟦")), prefix + " exposes no raw localization key markers");
            return "PASS · " + prefix + " full loop reached rescue without Grant, Warp, or Skip; production map/search/tray, placement, tools, overflow, swimming, return, and result verified.";
        }

        private static void PlaceViaUi(KimSurvivalPrototype prototype, string buttonField, StructureKind kind, float x, string label)
        {
            Submit(GetButton(prototype, buttonField));
            PrototypeCampPlacement placement = GetPrivateField<PrototypeCampPlacement>(prototype, "campPlacement");
            Require(placement.IsActive && placement.SelectedKind == kind, label + " placement begins through UI Submit");
            placement.SetCandidateX(x);
            InvokePrivate(prototype, "UpdatePlacementGhost");
            Require(placement.CurrentValidity == CampPlacementValidity.Valid, label + " candidate is valid");
            bool confirmed = InvokePrivateResult<bool>(prototype, "ConfirmCampPlacement");
            Require(confirmed && prototype.Session.HasStructure(kind), label + " placement confirms exactly one built structure");
        }

        private static void CaptureAndAudit(
            KimSurvivalPrototype prototype,
            string fileName,
            string label,
            List<string> layoutAudit,
            List<Wave3VisualGate.FrameResult> wave3Frames)
        {
            Capture(prototype, fileName);
            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);
            Camera camera = GetPrivateField<Camera>(prototype, "worldCamera");
            wave3Frames.Add(Wave3VisualGate.Analyze(label, Path.Combine(EvidenceFolder, fileName), camera, texts));
            float canvasScale = Mathf.Sqrt((1280f / 1920f) * (800f / 1080f));
            float minimumPixelHeight = float.MaxValue;
            List<string> overflow = new List<string>();
            List<string> missingFont = new List<string>();
            List<string> missingGlyph = new List<string>();
            List<string> smallWorldText = new List<string>();
            for (int i = 0; i < texts.Length; i += 1)
            {
                TMP_Text text = texts[i];
                text.ForceMeshUpdate(true, true);
                if (text.font == null)
                {
                    missingFont.Add(text.name);
                    continue;
                }
                if (text.isTextOverflowing)
                {
                    overflow.Add(text.name + "=" + text.text.Replace('\n', '/'));
                }
                foreach (char character in text.text.Where(character => !char.IsWhiteSpace(character)).Distinct())
                {
                    if (!text.font.HasCharacter(character, true, false))
                    {
                        missingGlyph.Add(text.name + "=U+" + ((int)character).ToString("X4"));
                    }
                }

                float pixels = text is TextMeshProUGUI
                    ? Mathf.Abs(text.textBounds.size.y) * canvasScale
                    : Mathf.Abs(text.textBounds.size.y * text.transform.lossyScale.y) * 800f / (2f * 5.625f);
                if (!string.IsNullOrWhiteSpace(text.text))
                {
                    minimumPixelHeight = Mathf.Min(minimumPixelHeight, pixels);
                    if (text is TextMeshPro && pixels < 12f)
                    {
                        smallWorldText.Add(text.text.Replace('\n', '/') + "=" + pixels.ToString("0.0") + "px");
                    }
                }
            }

            Require(missingFont.Count == 0, label + " has fonts on all active TMP text: " + string.Join(", ", missingFont));
            Require(missingGlyph.Count == 0, label + " has no missing visible glyphs: " + string.Join(", ", missingGlyph.Distinct()));
            layoutAudit.Add((overflow.Count == 0 ? "PASS" : "METRIC-WARN") + " · " + label +
                            " · active TMP=" + texts.Length +
                            " · minimum rendered text bounds=" + minimumPixelHeight.ToString("0.0") + "px" +
                            " · TMP overflow=" + overflow.Count +
                            " · small world text(<12px)=" + smallWorldText.Count +
                            " · screenshot=" + fileName);
            if (overflow.Count > 0)
            {
                layoutAudit.Add("  overflow candidates: " + string.Join(" | ", overflow));
            }
            if (smallWorldText.Count > 0)
            {
                layoutAudit.Add("  small world text: " + string.Join(" | ", smallWorldText));
            }
        }

        private static void FinishPlayModeRun()
        {
            bool passed = SessionState.GetBool(PassedKey, false);
            string message = SessionState.GetString(MessageKey, "FAIL · No Play Mode result.");
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, "playmode-full-loop.txt"), message, new UTF8Encoding(false));
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(PassedKey);
            SessionState.EraseString(MessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }

        private static void StopPlayMode()
        {
            if (tickAttached)
            {
                EditorApplication.update -= PlayModeTick;
                tickAttached = false;
            }
            EditorApplication.isPlaying = false;
        }

        private static void VerifyNaturalThreeDayModel()
        {
            GameSession s = new GameSession();
            Require(s.BeginSearch(), "model day 1 search");
            Require(s.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 1 wood");
            Require(s.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 1 salvage A");
            Require(s.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 1 salvage B");
            Require(s.TryGather(ResourceKind.Stone, 2) == GatherResult.Added, "day 1 stone");
            Require(s.ReturnToCamp(false), "day 1 return");
            Require(s.TryBuild(StructureKind.Workbench), "workbench");
            Require(s.TryResearch(TechKind.Rope) && s.TryCraft(TechKind.Rope), "rope");
            Require(s.TryResearch(TechKind.StoneAxe) && s.TryCraft(TechKind.StoneAxe), "stone axe");
            Require(s.EndDay(), "day 1 settlement");

            Require(s.BeginSearch(), "model day 2 search");
            Require(s.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 2 wood A");
            Require(s.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 2 wood B");
            Require(s.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 2 salvage A");
            Require(s.TryGather(ResourceKind.Salvage, 2) == GatherResult.PendingSwap, "day 2 salvage overflow");
            Require(s.ReplaceBagSlot(0), "day 2 replace wood stack");
            Require(s.ReturnToCamp(false) && s.EndDay(), "day 2 return and settlement");

            Require(s.BeginSearch(), "model day 3 search");
            Require(s.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 3 wood A");
            Require(s.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 3 wood B");
            Require(s.TryGather(ResourceKind.Stone, 2) == GatherResult.Added, "day 3 stone");
            Require(s.TryGather(ResourceKind.Salvage, 2) == GatherResult.PendingSwap, "day 3 salvage overflow");
            Require(s.ReplaceBagSlot(0), "day 3 replace wood stack");
            Require(s.ReturnToCamp(false), "day 3 return");
            Require(s.TryBuild(StructureKind.Campfire), "campfire");
            Require(s.TryBuild(StructureKind.RainCollector), "rain collector");
            Require(s.TryUpgradeSignal() && s.TryUpgradeSignal(), "signal stages");
            Require(s.Result == RunResult.Rescued, "rescue result");
        }

        private static void VerifyBagChoice()
        {
            GameSession s = new GameSession();
            Require(s.BeginSearch(), "bag scenario starts");
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                Require(s.TryGather(kind, 2) == GatherResult.Added, "fill " + kind);
            }
            Require(s.TryGather(ResourceKind.Wood, 1) == GatherResult.PendingSwap, "overflow pending");
            Require(s.HasPendingLoot, "pending stored");
            s.DiscardPendingLoot();
            Require(!s.HasPendingLoot, "pending discarded");
        }

        private static void VerifySwimmingModel()
        {
            GameSession land = new GameSession();
            Require(land.BeginSearch(), "land search");
            land.TickSearch(10f, true);
            float landEnergy = 100f - land.Energy;
            float landDaylight = 100f - land.Daylight;

            GameSession swim = new GameSession();
            Require(swim.BeginSearch(), "swim search");
            Require(swim.TryGather(ResourceKind.Salvage, 2, true) == GatherResult.Rejected, "water rejects land gather");
            Require(swim.SetSwimming(true), "enter water");
            swim.TickSearch(10f, true);
            Require(100f - swim.Energy > landEnergy && 100f - swim.Daylight > landDaylight, "swimming costs more");
            Require(swim.TryGather(ResourceKind.Salvage, 2, true) == GatherResult.Added, "water gather");
            Require(swim.SetSwimming(false) && !swim.IsSwimming, "exit water");
        }

        private static void VerifyFailureOutcomes()
        {
            GameSession deadline = new GameSession();
            for (int day = 1; day <= GameSession.FinalDay; day += 1)
            {
                Require(deadline.BeginSearch(), "deadline begin " + day);
                Require(deadline.ReturnToCamp(false), "deadline return " + day);
                Require(deadline.EndDay(), "deadline settlement " + day);
            }
            Require(deadline.Result == RunResult.Deadline, "deadline result");

            GameSession exhausted = new GameSession();
            Require(exhausted.BeginSearch() && exhausted.SetSwimming(true), "exhaustion swim starts");
            exhausted.TickSearch(200f, true);
            Require(exhausted.Result == RunResult.Exhausted, "exhaustion result");
        }

        private static void VerifyPlacementModel()
        {
            GameSession session = new GameSession();
            PrototypeCampPlacement placement = new PrototypeCampPlacement();
            int initialWood = session.GetStorage(ResourceKind.Wood);
            int initialStone = session.GetStorage(ResourceKind.Stone);
            int initialSalvage = session.GetStorage(ResourceKind.Salvage);

            placement.Begin(StructureKind.Campfire, false);
            placement.SetCandidateX(-5f);
            Require(placement.CurrentValidity == CampPlacementValidity.OutsideCampBounds, "outside camp bounds rejected");
            placement.SetCandidateX(-2.5f);
            Require(placement.CurrentValidity == CampPlacementValidity.BlocksEntrance, "entrance reservation rejected");
            placement.SetCandidateX(0f);
            Require(placement.CurrentValidity == CampPlacementValidity.BlocksRequiredPath, "required path rejected");
            placement.SetCandidateX(-1.5f);
            Require(placement.CurrentValidity == CampPlacementValidity.Valid, "campfire valid position");
            placement.Cancel();
            Require(!placement.IsActive && !placement.HasInstalledPosition(StructureKind.Campfire), "cancel creates no facility");
            Require(session.GetStorage(ResourceKind.Wood) == initialWood &&
                    session.GetStorage(ResourceKind.Stone) == initialStone &&
                    session.GetStorage(ResourceKind.Salvage) == initialSalvage, "cancel preserves resources");

            placement.Begin(StructureKind.Campfire, false);
            placement.SetCandidateX(-1.5f);
            Require(session.TryBuild(StructureKind.Campfire), "campfire cost accepted once");
            Require(placement.Commit(), "campfire committed once");
            int woodAfterBuild = session.GetStorage(ResourceKind.Wood);
            int stoneAfterBuild = session.GetStorage(ResourceKind.Stone);
            int salvageAfterBuild = session.GetStorage(ResourceKind.Salvage);
            Require(initialWood - woodAfterBuild == 2 && initialStone - stoneAfterBuild == 1 && initialSalvage == salvageAfterBuild, "exact campfire cost deducted once");
            Require(!placement.Commit() && !session.TryBuild(StructureKind.Campfire), "repeat confirm cannot duplicate or charge again");
            Require(session.GetStorage(ResourceKind.Wood) == woodAfterBuild &&
                    session.GetStorage(ResourceKind.Stone) == stoneAfterBuild &&
                    session.GetStorage(ResourceKind.Salvage) == salvageAfterBuild, "repeat confirm preserves resources");

            session.Grant(ResourceKind.Wood, 2);
            session.Grant(ResourceKind.Salvage, 1);
            placement.Begin(StructureKind.Workbench, false);
            placement.SetCandidateX(-1.5f);
            Require(placement.CurrentValidity == CampPlacementValidity.OverlapsStructure, "installed structure overlap rejected");
            placement.SetCandidateX(1.5f);
            Require(session.TryBuild(StructureKind.Workbench) && placement.Commit(), "workbench committed at valid position");

            int woodBeforeMove = session.GetStorage(ResourceKind.Wood);
            int stoneBeforeMove = session.GetStorage(ResourceKind.Stone);
            int salvageBeforeMove = session.GetStorage(ResourceKind.Salvage);
            float workbenchBeforeMove = placement.GetInstalledPosition(StructureKind.Workbench).x;
            placement.Begin(StructureKind.Workbench, true);
            placement.SetCandidateX(3.5f);
            placement.Cancel();
            Require(Mathf.Approximately(placement.GetInstalledPosition(StructureKind.Workbench).x, workbenchBeforeMove), "relocation cancel preserves position");

            placement.Begin(StructureKind.Workbench, true);
            placement.SetCandidateX(3.5f);
            Require(placement.Commit(), "workbench relocation commits");
            Require(Mathf.Approximately(placement.GetInstalledPosition(StructureKind.Workbench).x, 3.5f), "relocation changes only position");
            Require(session.GetStorage(ResourceKind.Wood) == woodBeforeMove &&
                    session.GetStorage(ResourceKind.Stone) == stoneBeforeMove &&
                    session.GetStorage(ResourceKind.Salvage) == salvageBeforeMove, "relocation is free");

            int woodBeforeCampfireMove = session.GetStorage(ResourceKind.Wood);
            int stoneBeforeCampfireMove = session.GetStorage(ResourceKind.Stone);
            int salvageBeforeCampfireMove = session.GetStorage(ResourceKind.Salvage);
            placement.Begin(StructureKind.Campfire, true);
            placement.SetCandidateX(1.5f);
            Require(placement.CurrentValidity == CampPlacementValidity.Valid, "campfire second valid relocation position");
            Require(placement.Commit(), "campfire relocation commits");
            Require(Mathf.Approximately(placement.GetInstalledPosition(StructureKind.Campfire).x, 1.5f), "campfire relocation changes only position");
            Require(session.GetStorage(ResourceKind.Wood) == woodBeforeCampfireMove &&
                    session.GetStorage(ResourceKind.Stone) == stoneBeforeCampfireMove &&
                    session.GetStorage(ResourceKind.Salvage) == salvageBeforeCampfireMove, "second general-facility relocation is free");
        }

        private static void VerifySharedInputModel()
        {
            PrototypePlayerActions keyboard = PrototypePlayerActions.FromRaw(new PrototypeRawInput
            {
                KeyboardLeft = true,
                KeyboardJump = true,
                KeyboardInteract = true,
                KeyboardReturn = true,
                KeyboardCancel = true,
                BagSlotIndex = 2
            });
            PrototypePlayerActions gamepad = PrototypePlayerActions.FromRaw(new PrototypeRawInput
            {
                HorizontalAxis = -1f,
                GamepadJump = true,
                GamepadInteract = true,
                GamepadReturn = true,
                GamepadCancel = true,
                BagSlotIndex = 2
            });
            Require(Mathf.Approximately(keyboard.Horizontal, gamepad.Horizontal), "shared horizontal action");
            Require(keyboard.JumpPressed && gamepad.JumpPressed, "shared jump action");
            Require(keyboard.InteractPressed && gamepad.InteractPressed, "shared interact action");
            Require(keyboard.ReturnPressed && gamepad.ReturnPressed, "shared return action");
            Require(keyboard.CancelPressed && gamepad.CancelPressed, "shared cancel action");
            Require(keyboard.BagSlotIndex == gamepad.BagSlotIndex, "shared bag selection action");

            PrototypeCampPlacementActions pointer = PrototypeCampPlacementActions.FromRaw(new PrototypeRawCampPlacementInput
            {
                UsePointer = true,
                PointerWorldX = 1.5f,
                MouseConfirm = true,
                MouseCancel = true
            });
            PrototypeCampPlacementActions controller = PrototypeCampPlacementActions.FromRaw(new PrototypeRawCampPlacementInput
            {
                HorizontalAxis = 1f,
                GamepadConfirm = true,
                GamepadCancel = true
            });
            PrototypeCampPlacement pointerPlacement = new PrototypeCampPlacement();
            PrototypeCampPlacement controllerPlacement = new PrototypeCampPlacement();
            pointerPlacement.Begin(StructureKind.Campfire, false);
            controllerPlacement.Begin(StructureKind.Campfire, false);
            pointerPlacement.Update(pointer, 1f);
            controllerPlacement.Update(controller, 1f);
            Require(Mathf.Approximately(pointerPlacement.CandidateX, controllerPlacement.CandidateX), "pointer and gamepad reach the same snapped candidate");
            Require(pointer.ConfirmPressed && controller.ConfirmPressed, "shared placement confirm");
            Require(pointer.CancelPressed && controller.CancelPressed, "shared placement cancel");

            PrototypeSystemActions keyboardSystem = PrototypeSystemActions.FromRaw(new PrototypeRawSystemInput { KeyboardLanguage = true });
            PrototypeSystemActions gamepadSystem = PrototypeSystemActions.FromRaw(new PrototypeRawSystemInput { GamepadLanguage = true });
            Require(keyboardSystem.LanguagePressed && gamepadSystem.LanguagePressed, "shared language action");

            PrototypeInputDeviceTracker tracker = new PrototypeInputDeviceTracker();
            Require(tracker.ActiveDevice == PrototypeInputDevice.KeyboardMouse, "keyboard/mouse is the initial prompt device");
            tracker.Update(new PrototypeInputActivity(false, true));
            Require(tracker.ActiveDevice == PrototypeInputDevice.Gamepad, "gamepad activity switches prompt device");
            tracker.Update(new PrototypeInputActivity(true, false));
            Require(tracker.ActiveDevice == PrototypeInputDevice.KeyboardMouse, "keyboard/mouse activity switches prompt device back");
            Require(PrototypeInputPromptKeys.Camp(PrototypeInputDevice.KeyboardMouse) == "controls.camp.keyboard_mouse", "keyboard/mouse camp prompt key");
            Require(PrototypeInputPromptKeys.Camp(PrototypeInputDevice.Gamepad) == "controls.camp.gamepad", "gamepad camp prompt key");
            Require(PrototypeInputPromptKeys.Placement(PrototypeInputDevice.KeyboardMouse) == "controls.placement.keyboard_mouse", "keyboard/mouse placement prompt key");
            Require(PrototypeInputPromptKeys.Placement(PrototypeInputDevice.Gamepad) == "controls.placement.gamepad", "gamepad placement prompt key");
        }

        private static void VerifyLocalizationModel()
        {
            bool hadPreference = PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey);
            string originalPreference = PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode);
            List<string> missingWarnings = new List<string>();
            Application.LogCallback callback = delegate(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Warning && condition.Contains("[Kim Survival Localization]"))
                {
                    missingWarnings.Add(condition);
                }
            };
            Application.logMessageReceived += callback;
            try
            {
                PlayerPrefs.DeleteKey(PrototypeLocalization.PreferenceKey);
                PlayerPrefs.Save();
                using (PrototypeLocalization localization = new PrototypeLocalization())
                {
                    Require(localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode, "Korean is the no-preference default");
                    Require(localization.Format("ui.camp.title") == "베이스캠프 · 제작 / 건설 / 연구", "Korean source table renders");
                    string koreanKeyboardPrompt = localization.Format(
                        PrototypeInputPromptKeys.Placement(PrototypeInputDevice.KeyboardMouse),
                        localization.DeviceName(PrototypeInputDevice.KeyboardMouse));
                    Require(koreanKeyboardPrompt.Contains("마우스") && koreanKeyboardPrompt.Contains("Enter") && koreanKeyboardPrompt.Contains("Esc"), "Korean keyboard/mouse placement prompt renders device-specific actions");
                    bool eventRaised = false;
                    localization.LocaleChanged += delegate { eventRaised = true; };
                    Require(localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false), "English locale selectable");
                    Require(eventRaised, "locale-changed event raised immediately");
                    Require(localization.Format("ui.camp.title") == "Base Camp · Craft / Build / Research", "English table active immediately");
                    string englishGamepadPrompt = localization.Format(
                        PrototypeInputPromptKeys.Placement(PrototypeInputDevice.Gamepad),
                        localization.DeviceName(PrototypeInputDevice.Gamepad));
                    Require(englishGamepadPrompt.Contains("left stick") && englishGamepadPrompt.Contains("A confirm") && englishGamepadPrompt.Contains("B cancel") && englishGamepadPrompt.Contains("Y language"), "English gamepad placement prompt renders device-specific actions");
                    string smart = localization.Format("hud.status.camp", 1, 3, "Camp", 75, 100);
                    Require(smart.Contains("DAY 1/3") && smart.Contains("Hunger 75") && smart.Contains("Energy 100"), "English Smart String arguments render");
                    foreach (int count in new[] { 0, 1, 2, 9999 })
                    {
                        string quantity = localization.Format("world.resource.land", ResourceKind.Wood, count);
                        Require(quantity == "Wood ×" + count, "English neutral-noun quantity boundary renders: " + count);
                    }
                    string fallbackFirst = localization.Format("dev.fallback_probe");
                    string fallbackSecond = localization.Format("dev.fallback_probe");
                    Require(fallbackFirst == "한국어 폴백 확인" && fallbackSecond == fallbackFirst, "missing English key falls back to Korean");
                    Require(missingWarnings.Count(message => message.Contains("en:dev.fallback_probe")) == 1, "missing key warning is logged once per service instance");
                    Require(localization.ResolveStartupLocale("xx-invalid") == PrototypeLocalization.KoreanLocaleCode, "invalid saved locale resolves to Korean");
                    Require(localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, true), "English locale persisted");
                    Require(PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey) == PrototypeLocalization.EnglishLocaleCode, "persisted preference reads as English");
                }

                using (PrototypeLocalization relaunched = new PrototypeLocalization())
                {
                    Require(relaunched.CurrentLocaleCode == PrototypeLocalization.EnglishLocaleCode, "new localization service restores English preference");
                    Require(relaunched.Format("ui.camp.title") == "Base Camp · Craft / Build / Research", "restored locale formats English immediately");
                }
            }
            finally
            {
                Application.logMessageReceived -= callback;
                if (hadPreference)
                {
                    PlayerPrefs.SetString(PrototypeLocalization.PreferenceKey, originalPreference);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PrototypeLocalization.PreferenceKey);
                }
                PlayerPrefs.Save();
            }
        }

        private static void VerifyLocalizationAssets()
        {
            string sourcePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            string[] lines = File.ReadAllLines(sourcePath);
            Require(lines.Length > 100 && lines[0] == "Key\tko\ten", "localization TSV has ko/en schema and substantial coverage");
            HashSet<string> keys = new HashSet<string>();
            int smartRows = 0;
            for (int i = 1; i < lines.Length; i += 1)
            {
                string[] columns = lines[i].Split(new[] { '\t' }, StringSplitOptions.None);
                Require(columns.Length >= 3, "localization row has key, ko, and en at line " + (i + 1));
                Require(keys.Add(columns[0]), "localization key is unique: " + columns[0]);
                Require(!string.IsNullOrWhiteSpace(columns[1]), "Korean source is present: " + columns[0]);
                if (columns[0] != "dev.fallback_probe")
                {
                    Require(!string.IsNullOrWhiteSpace(columns[2]), "English translation is present: " + columns[0]);
                }

                string koreanTokens = PlaceholderSet(columns[1]);
                string englishTokens = PlaceholderSet(columns[2]);
                Require(columns[0] == "dev.fallback_probe" || koreanTokens == englishTokens, "format variable parity: " + columns[0]);
                if (!string.IsNullOrEmpty(koreanTokens) || !string.IsNullOrEmpty(englishTokens))
                {
                    smartRows += 1;
                }
            }
            Require(smartRows >= 10, "Smart String rows are present");

            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                StringTable korean = LocalizationSettings.StringDatabase.GetTable(PrototypeLocalization.TableName, LocalizationSettings.AvailableLocales.GetLocale("ko"));
                StringTable english = LocalizationSettings.StringDatabase.GetTable(PrototypeLocalization.TableName, LocalizationSettings.AvailableLocales.GetLocale("en"));
                Require(korean != null && english != null, "Unity ko/en String Tables load");
                Require(korean.GetEntry("hud.status.camp") != null && korean.GetEntry("hud.status.camp").IsSmart, "Korean Smart String entry is marked Smart");
                Require(english.GetEntry("hud.status.camp") != null && english.GetEntry("hud.status.camp").IsSmart, "English Smart String entry is marked Smart");
            }

            PrototypeLocaleFontProfile profile = Resources.Load<PrototypeLocaleFontProfile>("PrototypeLocaleFontProfile");
            Require(profile != null && profile.Find("ko") != null && profile.Find("en") != null, "ko/en font mappings are data driven");
            Font koreanFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 32);
            Font englishFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
            try
            {
                Require(koreanFont != null && koreanFont.HasCharacter('김') && koreanFont.HasCharacter('한'), "Korean system font covers representative Hangul");
                Require(englishFont != null && englishFont.HasCharacter('A') && englishFont.HasCharacter('z') && englishFont.HasCharacter('ñ'), "English font covers Latin and Spanish-extension probe");
            }
            finally
            {
                if (koreanFont != null) UnityEngine.Object.DestroyImmediate(koreanFont);
                if (englishFont != null) UnityEngine.Object.DestroyImmediate(englishFont);
            }
        }

        private static void VerifyDedicatedSignalAnchor()
        {
            Require(!Enum.GetNames(typeof(StructureKind)).Any(name => name.IndexOf("Signal", StringComparison.OrdinalIgnoreCase) >= 0), "signal is not a general freely placed structure kind");
            string runtime = File.ReadAllText(Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs"));
            Require(runtime.Contains("world.signal_anchor"), "dedicated signal anchor has localized world feedback");
            Require(
                runtime.Contains("private const float CampSignalAnchorNormalizedX") &&
                runtime.Contains("private const float CampSignalAnchorNormalizedY") &&
                runtime.Contains("GetCampArtPoint(CampSignalAnchorNormalizedX, CampSignalAnchorNormalizedY)"),
                "signal anchor has a dedicated background-relative world position");
            Require(runtime.Contains("signalAnchor.x > PrototypeCampPlacement.BuildMaximumX"), "signal anchor remains outside general facility placement bounds");
            Require(runtime.Contains("delegate { session.TryUpgradeSignal(); RefreshAll(); }"), "signal action upgrades the anchor rather than entering general placement");

            GameSession session = new GameSession();
            session.Grant(ResourceKind.Wood, 10);
            session.Grant(ResourceKind.Salvage, 10);
            Require(session.TryBuild(StructureKind.Workbench), "signal test workbench prerequisite");
            session.Grant(ResourceKind.Salvage, 1);
            Require(session.TryResearch(TechKind.Rope) && session.TryCraft(TechKind.Rope), "signal test rope prerequisite");
            Require(session.TryUpgradeSignal() && session.TryUpgradeSignal(), "dedicated signal anchor reaches both stages");
            Require(session.Result == RunResult.Rescued, "dedicated signal completion reaches rescue");
        }

        private static string PlaceholderSet(string value)
        {
            return string.Join(",", Regex.Matches(value ?? string.Empty, @"\{(\d+)(?:[^}]*)\}")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Distinct()
                .OrderBy(token => token));
        }

        private static void WriteInputCodePathAudit(DateTime started)
        {
            string runtimePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "PrototypePlayerInput.cs");
            string prototypePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs");
            string inputPath = Path.Combine(ProjectRoot, "ProjectSettings", "InputManager.asset");
            string runtime = File.ReadAllText(runtimePath);
            string prototype = File.ReadAllText(prototypePath);
            string input = File.ReadAllText(inputPath).Replace("\r", string.Empty);
            List<string> checks = new List<string>
            {
                Contains(runtime, "Input.GetAxisRaw(\"Horizontal\")", "keyboard/left-stick movement reads Horizontal"),
                Contains(runtime, "KeyCode.A", "keyboard A movement path"),
                Contains(runtime, "KeyCode.D", "keyboard D movement path"),
                Contains(runtime, "KeyCode.Space", "keyboard jump path"),
                Contains(runtime, "KeyCode.E", "keyboard interact path"),
                Contains(runtime, "KeyCode.R", "keyboard return path"),
                Contains(runtime, "KeyCode.JoystickButton0", "gamepad A/jump and submit code path"),
                Contains(runtime, "KeyCode.JoystickButton1", "gamepad B/cancel and return code path"),
                Contains(runtime, "KeyCode.JoystickButton2", "gamepad X/interact code path"),
                Contains(runtime, "KeyCode.JoystickButton3", "gamepad Y/language code path"),
                Contains(runtime, "MouseConfirm = Input.GetMouseButtonDown(0)", "mouse placement confirm path"),
                Contains(runtime, "KeyboardConfirm = Input.GetKeyDown(KeyCode.Return)", "keyboard placement confirm path"),
                Contains(runtime, "GamepadConfirm = Input.GetKeyDown(KeyCode.JoystickButton0)", "gamepad placement confirm path"),
                Contains(runtime, "GamepadCancel = Input.GetKeyDown(KeyCode.JoystickButton1)", "gamepad placement cancel path"),
                Contains(runtime, "KeyboardLanguage = Input.GetKeyDown(KeyCode.F1)", "keyboard language switch path"),
                Contains(runtime, "GamepadLanguage = Input.GetKeyDown(KeyCode.JoystickButton3)", "gamepad language switch path"),
                Contains(prototype, "playerInput.ReadCampPlacementActions(worldCamera)", "runtime consumes shared placement actions"),
                Contains(prototype, "playerInput.ReadSystemActions()", "runtime consumes shared language action"),
                Contains(input, "m_Name: Submit", "uGUI Submit axis exists"),
                Contains(input, "altPositiveButton: joystick button 0", "uGUI gamepad submit mapping"),
                Contains(input, "m_Name: Cancel", "uGUI Cancel axis exists"),
                Contains(input, "positiveButton: joystick button 1", "uGUI gamepad cancel mapping"),
                Contains(input, "type: 2\n    axis: 0", "legacy joystick horizontal axis mapping"),
                Contains(input, "type: 2\n    axis: 1", "legacy joystick vertical axis mapping")
            };

            bool allPassed = checks.All(line => line.StartsWith("PASS", StringComparison.Ordinal));
            string report = Header("Keyboard and gamepad code-path audit", started) +
                            string.Join(Environment.NewLine, checks) + Environment.NewLine +
                            "Overall code-path audit: " + (allPassed ? "PASS" : "FAIL") + Environment.NewLine +
                            "Execution scope: raw keyboard/mouse and gamepad actions, UI Submit, and directional navigation are automated; physical gamepad actuation is reported separately in playmode-full-loop.txt." + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "input-code-path-audit.txt"), report, new UTF8Encoding(false));
        }

        private static void WriteHardcodedPlayerStringAudit(DateTime started)
        {
            string gameSessionPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "GameSession.cs");
            string prototypePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs");
            string gameSession = File.ReadAllText(gameSessionPath);
            string prototype = File.ReadAllText(prototypePath);

            MatchCollection modelKorean = Regex.Matches(gameSession, "\"[^\"\\r\\n]*[가-힣][^\"\\r\\n]*\"");
            MatchCollection directTextAssignments = Regex.Matches(prototype, @"\.text\s*=\s*""[^""\r\n]+""");
            MatchCollection literalWorldLabels = Regex.Matches(prototype, @"CreateWorldLabel\([^,\r\n]+,\s*""[^""\r\n]+""");
            MatchCollection literalSetButtons = Regex.Matches(prototype, @"SetButton\([^,\r\n]+,\s*""[^""\r\n]+""");
            MatchCollection allPrototypeKorean = Regex.Matches(prototype, "\"[^\"\\r\\n]*[가-힣][^\"\\r\\n]*\"");

            bool passed = modelKorean.Count == 0 && directTextAssignments.Count == 0 && literalWorldLabels.Count == 0 && literalSetButtons.Count == 0;
            string report = Header("Player-facing hardcoded ko/en string sink audit", started) +
                            (passed ? "PASS" : "FAIL") + " · No unapproved literal flows directly into audited player-facing text sinks." + Environment.NewLine +
                            "GameSession Korean literal candidates: " + modelKorean.Count + Environment.NewLine +
                            "Direct TMP/uGUI .text literal assignments: " + directTextAssignments.Count + Environment.NewLine +
                            "Literal CreateWorldLabel values: " + literalWorldLabels.Count + Environment.NewLine +
                            "Literal SetButton labels: " + literalSetButtons.Count + Environment.NewLine +
                            "KimSurvivalPrototype Korean literal candidates outside those sinks: " + allPrototypeKorean.Count + Environment.NewLine +
                            "Allowlist classification: internal GameObject/component names, exception/assertion text, and the non-shipping RunAutomatedVerification diagnostics." + Environment.NewLine +
                            "English audit method: syntax/sink checks above plus table-key routing review; naive ASCII matching is not used as a release decision." + Environment.NewLine +
                            "Audited files: " + gameSessionPath + " | " + prototypePath + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "hardcoded-player-strings.txt"), report, new UTF8Encoding(false));
        }

        private static void WriteLocaleExpansionReadiness(DateTime started)
        {
            string localizationPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "PrototypeLocalization.cs");
            string prototypePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs");
            string builderPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Editor", "PrototypeLocalizationAssetBuilder.cs");
            string fontProfilePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "PrototypeLocaleFontProfile.cs");
            string stringsPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            string gatePath = Path.Combine(ProjectRoot, "Assets", "Editor", "ParallelQA", "Wave3VisualGate.cs");
            string localization = File.ReadAllText(localizationPath);
            string prototype = File.ReadAllText(prototypePath);
            string builder = File.ReadAllText(builderPath);
            string fontProfile = File.ReadAllText(fontProfilePath);
            string header = File.ReadLines(stringsPath).FirstOrDefault() ?? string.Empty;
            string gate = File.ReadAllText(gatePath);
            string[] localeCodes = LocalizationSettings.AvailableLocales.Locales
                .Where(locale => locale != null)
                .Select(locale => locale.Identifier.Code)
                .OrderBy(code => code)
                .ToArray();

            List<string> checks = new List<string>
            {
                (localeCodes.Contains("ko") && localeCodes.Contains("en") ? "PASS" : "FAIL") + " · Unity AvailableLocales contains ko/en · observed=" + string.Join(",", localeCodes),
                (header == "Key\tko\ten" ? "PASS" : "FAIL") + " · current localization source schema is exactly Key/ko/en · header=" + header.Replace('\t', '/'),
                (localization.Contains("SetLocale(string localeCode") && localization.Contains("AvailableLocales.GetLocale(localeCode)") ? "PARTIAL" : "NOT READY") + " · runtime SetLocale can resolve a configured locale by data code",
                (fontProfile.Contains("List<PrototypeLocaleFontMapping>") ? "PARTIAL" : "NOT READY") + " · font profile storage is list-based for future mappings",
                (localization.Contains("CurrentLocaleCode == KoreanLocaleCode ? EnglishLocaleCode : KoreanLocaleCode") ? "NOT READY" : "PASS") + " · language cycling is not multi-locale (binary ko/en implementation detected)",
                (builder.Contains("EnsureLocale(PrototypeLocalization.KoreanLocaleCode") && builder.Contains("EnsureLocale(PrototypeLocalization.EnglishLocaleCode") ? "NOT READY" : "PASS") + " · asset builder explicitly provisions only ko/en",
                (builder.Contains("lines[0] != \"Key\\tko\\ten\"") ? "NOT READY" : "PASS") + " · import schema explicitly rejects a third locale column",
                (prototype.Contains("CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode ? \"ui.language.switch.ko\" : \"ui.language.switch.en\"") ? "NOT READY" : "PASS") + " · language button label branches only ko/en",
                (gate.Contains("ExpandPseudoLong") && gate.Contains("1.42f") ? "PASS" : "FAIL") + " · non-shipping deterministic 142% pseudo-long fixture exists in QA harness"
            };
            bool currentKoEnPass = checks.Take(2).All(line => line.StartsWith("PASS", StringComparison.Ordinal));
            string report = Header("Locale expansion readiness audit", started) +
                            string.Join(Environment.NewLine, checks) + Environment.NewLine +
                            "Current ko/en readiness: " + (currentKoEnPass ? "PASS" : "FAIL") + Environment.NewLine +
                            "Pseudo-long stress fixture: " + (gate.Contains("ExpandPseudoLong") ? "PASS" : "FAIL") + Environment.NewLine +
                            "Third-locale shipping readiness: NOT READY" + Environment.NewLine +
                            "Decision: third locale requires schema, builder, cycle/selection UI, tables, and font mapping additions; the QA pseudo-locale is deliberately not a shipping locale." + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "localization-expansion-readiness.txt"), report, new UTF8Encoding(false));
        }

        private static void Capture(KimSurvivalPrototype prototype, string name)
        {
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, name), 1280, 800);
        }

        private static Button GetButton(KimSurvivalPrototype prototype, string fieldName)
        {
            FieldInfo field = typeof(KimSurvivalPrototype).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null, "button field " + fieldName + " exists");
            Button button = field.GetValue(prototype) as Button;
            Require(button != null, "button " + fieldName + " exists");
            return button;
        }

        private static void Submit(Button button)
        {
            Require(button != null && button.gameObject.activeInHierarchy && button.interactable, "submit target is active and interactable");
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            bool handled = ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Require(handled, "EventSystem submit handled by " + button.name);
        }

        private static int VerifyDirectionalNavigationFromCurrentSelection()
        {
            Selectable start = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            Require(start != null, "current selection is a Selectable");
            Selectable[] expected = Selectable.allSelectablesArray
                .Where(item => item != null && item.gameObject.activeInHierarchy && item.interactable)
                .ToArray();
            HashSet<Selectable> visited = new HashSet<Selectable>();
            Queue<Selectable> queue = new Queue<Selectable>();
            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Selectable current = queue.Dequeue();
                Selectable[] neighbors =
                {
                    current.FindSelectableOnLeft(),
                    current.FindSelectableOnRight(),
                    current.FindSelectableOnUp(),
                    current.FindSelectableOnDown()
                };
                foreach (Selectable neighbor in neighbors)
                {
                    if (neighbor != null && neighbor.gameObject.activeInHierarchy && neighbor.interactable && visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            string missing = string.Join(", ", expected.Where(item => !visited.Contains(item)).Select(item => item.name));
            Require(expected.All(visited.Contains), "directional navigation reaches every enabled button; missing: " + missing);
            return expected.Length;
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(method != null, "private method " + methodName + " exists");
            try
            {
                method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static T InvokePrivateResult<T>(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(method != null, "private method " + methodName + " exists");
            try
            {
                return (T)method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null, "private field " + fieldName + " exists");
            object value = field.GetValue(target);
            Require(value is T, "private field " + fieldName + " has expected type " + typeof(T).Name);
            return (T)value;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null, "private field " + fieldName + " exists");
            field.SetValue(target, value);
        }

        private static void Check(List<string> results, string name, Action action)
        {
            try
            {
                action();
                results.Add("PASS · " + name);
            }
            catch (Exception exception)
            {
                results.Add("FAIL · " + name + " · " + exception.Message);
            }
        }

        private static string Contains(string source, string token, string label)
        {
            return source.Contains(token) ? "PASS · " + label : "FAIL · " + label + " (missing token: " + token + ")";
        }

        private static string Header(string title, DateTime started)
        {
            return title + Environment.NewLine +
                   "Run ID: " + RunId + Environment.NewLine +
                   "Started UTC: " + started.ToString("O") + Environment.NewLine +
                   "Completed UTC: " + DateTime.UtcNow.ToString("O") + Environment.NewLine +
                   "Unity: " + Application.unityVersion + Environment.NewLine +
                   "Baseline commit: " + BaselineCommit + Environment.NewLine +
                   "Project: " + ProjectRoot + Environment.NewLine +
                   "Command: " + string.Join(" ", Environment.GetCommandLineArgs().Select(Quote)) + Environment.NewLine;
        }

        private static string Quote(string value)
        {
            return value.IndexOf(' ') >= 0 ? "\"" + value + "\"" : value;
        }

        private static string ReadSharedText(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value;
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Parallel QA assertion failed: " + message);
            }
        }
    }
}
