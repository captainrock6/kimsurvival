using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using KimSurvival;
using KimSurvival.EditorTools;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Candidate gate for the O9/O10 submission presentation. This intentionally
    /// verifies the current spatial-camp flow instead of the retired global camp
    /// dashboard assumptions in the legacy full-loop fixture.
    /// </summary>
    [InitializeOnLoad]
    public static class O10CandidateGateRunner
    {
        private const string RunningKey = "KimSurvival.O10Candidate.Running";
        private const string PassedKey = "KimSurvival.O10Candidate.Passed";
        private const string MessageKey = "KimSurvival.O10Candidate.Message";
        private static double earliestRunTime;
        private static double timeoutAt;
        private static bool tickAttached;

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "o10-candidate" : value;
            }
        }

        private static string EvidenceFolder
        {
            get { return Path.GetFullPath(Path.Combine("Artifacts", "ParallelQA", RunId)); }
        }

        static O10CandidateGateRunner()
        {
            if (SessionState.GetBool(RunningKey, false)) Attach();
        }

        public static void RunEditContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            List<string> checks = new List<string>();
            string presentationDetail;
            Require(KimSurvivalPrototype.RunO9O10PresentationContracts(out presentationDetail), presentationDetail);
            checks.Add("PASS · " + presentationDetail);
            Require(File.Exists(PrototypeProjectBuilder.ScenePath), "playable scene exists");
            checks.Add("PASS · Playable scene exists: " + PrototypeProjectBuilder.ScenePath);
            Require(Application.unityVersion.StartsWith("6000.4.9", StringComparison.Ordinal), "expected Unity editor version");
            checks.Add("PASS · Unity editor version: " + Application.unityVersion);

            string styleJob = Path.Combine(
                "Assets", "_Project", "Art", "Generated", "ui_set",
                "job_20260828122852_c9ccf2aa", "job.json");
            Require(File.Exists(styleJob), "revised style benchmark job exists");
            string assetRegistryPath = Path.Combine(".forge", "assets.json");
            Require(File.Exists(assetRegistryPath), "Forge asset registry exists");
            string assetRegistry = File.ReadAllText(assetRegistryPath);
            int assetIndex = assetRegistry.IndexOf("\"id\": \"ui.gamejam.style-benchmark\"", StringComparison.Ordinal);
            Require(assetIndex >= 0, "revised style benchmark is registered");
            string assetRecord = assetRegistry.Substring(assetIndex, Math.Min(3000, assetRegistry.Length - assetIndex));
            Require(assetRecord.IndexOf("\"status\": \"review\"", StringComparison.Ordinal) >= 0 &&
                    assetRecord.IndexOf("job_20260828122852_c9ccf2aa", StringComparison.Ordinal) >= 0,
                "revised style benchmark remains review-only until user adoption");
            checks.Add("PASS · Revised style benchmark V2 exists and remains review-only.");

            WriteEvidence("o10-edit-contracts.txt", checks);
            Debug.Log("[O10 Candidate] Edit contracts passed.");
        }

        public static void RunPlayModeVerification()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "Verification did not complete.");
            Attach();
            EditorSceneManager.OpenScene(PrototypeProjectBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWindowsCandidate()
        {
            Directory.CreateDirectory(EvidenceFolder);
            string overrideFolder = Environment.GetEnvironmentVariable("KIM_O10_BUILD_FOLDER");
            string buildFolder = string.IsNullOrWhiteSpace(overrideFolder)
                ? Path.GetFullPath(Path.Combine("Builds", "O10Candidate-" + RunId))
                : Path.GetFullPath(overrideFolder);
            Directory.CreateDirectory(buildFolder);
            string executable = Path.Combine(buildFolder, "KimsSurvivalIsland.exe");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { PrototypeProjectBuilder.ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            List<string> lines = new List<string>
            {
                summary.result == BuildResult.Succeeded ? "PASS" : "FAIL",
                "Unity: " + Application.unityVersion,
                "Output: " + executable,
                "Result: " + summary.result,
                "Errors: " + summary.totalErrors,
                "Warnings: " + summary.totalWarnings,
                "Bytes: " + summary.totalSize,
                "Duration: " + summary.totalTime,
                "Completed UTC: " + DateTime.UtcNow.ToString("O")
            };
            if (File.Exists(executable)) lines.Add("SHA256: " + Sha256(executable));
            File.WriteAllLines(Path.Combine(EvidenceFolder, "windows-o10-build.txt"), lines, new UTF8Encoding(false));
            Require(summary.result == BuildResult.Succeeded && summary.totalErrors == 0 && File.Exists(executable),
                "Windows O10 candidate build succeeds");
            Debug.Log("[O10 Candidate] Windows build passed: " + executable);
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
            timeoutAt = EditorApplication.timeSinceStartup + 45d;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                earliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                timeoutAt = EditorApplication.timeSinceStartup + 45d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishAndExit();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (now < earliestRunTime) return;
            if (now > timeoutAt)
            {
                SessionState.SetString(MessageKey, "FAIL · timed out waiting for the playable scene");
                StopPlayMode();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;

            try
            {
                List<string> checks = new List<string>();
                string contentDetail;
                Require(KimSurvivalPrototype.RunO9O10PresentationContracts(out contentDetail), contentDetail);
                checks.Add("PASS · " + contentDetail);

                GameObject shell = GetField<GameObject>(prototype, "submissionShellRoot");
                GameObject objective = GetField<GameObject>(prototype, "firstObjectiveRoot");
                TMP_Text objectiveText = GetField<TMP_Text>(prototype, "firstObjectiveText");
                Require(shell != null && !shell.activeSelf, "batch candidate starts in Playing state");
                Require(objective != null && objective.activeSelf && !string.IsNullOrWhiteSpace(objectiveText.text),
                    "first objective is visible and localized");
                checks.Add("PASS · Spatial camp starts directly in batch verification with a visible first objective.");

                PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
                Button languageButton = GetField<Button>(prototype, "languageButton");
                TMP_Text actionTitle = GetField<TMP_Text>(prototype, "actionTitleText");
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                Invoke(prototype, "RefreshAll");
                languageButton.onClick.Invoke();
                Require(localization.CurrentLocaleCode == PrototypeLocalization.EnglishLocaleCode &&
                        actionTitle.text == "Base Camp · Craft / Build / Research",
                    "language switch updates hidden and visible UI immediately");
                checks.Add("PASS · Korean/English runtime language switch updates immediately.");

                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                Invoke(prototype, "RefreshAll");
                TMP_Text status = GetField<TMP_Text>(prototype, "statusText");
                TMP_Text resources = GetField<TMP_Text>(prototype, "resourceText");
                Canvas.ForceUpdateCanvases();
                status.ForceMeshUpdate(true, true);
                resources.ForceMeshUpdate(true, true);
                Require(!status.isTextOverflowing && !resources.isTextOverflowing,
                    "simplified top HUD fits at candidate resolution");
                foreach (string localeCode in new[]
                {
                    PrototypeLocalization.EnglishLocaleCode,
                    PrototypeLocalization.QpsLongLocaleCode
                })
                {
                    localization.SetLocale(localeCode, false);
                    Invoke(prototype, "RefreshAll");
                    Canvas.ForceUpdateCanvases();
                    status.ForceMeshUpdate(true, true);
                    resources.ForceMeshUpdate(true, true);
                    Require(!status.isTextOverflowing && !resources.isTextOverflowing,
                        "simplified top HUD fits locale " + localeCode);
                }
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                Invoke(prototype, "RefreshAll");
                checks.Add("PASS · Simplified top HUD has no KO/EN/qps-long TMP overflow.");

                string campCapture = Path.Combine(EvidenceFolder, "o10-camp-ko-1280x800.png");
                prototype.CaptureVerificationPng(campCapture, 1280, 800);
                checks.Add("PASS · Captured current spatial camp: " + campCapture);

                FieldInfo state = Field(prototype, "submissionShellState");
                object title = Enum.Parse(state.FieldType, "Title");
                state.SetValue(prototype, title);
                Invoke(prototype, "RefreshO9O10Presentation");
                Require(shell.activeSelf, "title shell can be opened");
                string titleCapture = Path.Combine(EvidenceFolder, "o10-title-ko-1280x800.png");
                prototype.CaptureVerificationPng(titleCapture, 1280, 800);
                checks.Add("PASS · Captured title shell: " + titleCapture);

                object playing = Enum.Parse(state.FieldType, "Playing");
                state.SetValue(prototype, playing);
                Invoke(prototype, "RefreshO9O10Presentation");
                Require(!shell.activeSelf, "title shell returns to playing state");

                WriteEvidence("o10-playmode.txt", checks);
                SessionState.SetBool(PassedKey, true);
                SessionState.SetString(MessageKey, string.Join(Environment.NewLine, checks));
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, "FAIL · " + exception);
            }

            StopPlayMode();
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
            string message = SessionState.GetString(MessageKey, "No verification message.");
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(
                Path.Combine(EvidenceFolder, "o10-playmode-result.txt"),
                (passed ? "PASS" : "FAIL") + Environment.NewLine + message + Environment.NewLine +
                "Completed UTC: " + DateTime.UtcNow.ToString("O") + Environment.NewLine,
                new UTF8Encoding(false));
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(PassedKey);
            SessionState.EraseString(MessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static FieldInfo Field(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return field;
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            return Field(target, name).GetValue(target) as T;
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            method.Invoke(target, null);
        }

        private static void WriteEvidence(string fileName, IEnumerable<string> lines)
        {
            File.WriteAllText(
                Path.Combine(EvidenceFolder, fileName),
                "PASS" + Environment.NewLine + string.Join(Environment.NewLine, lines) + Environment.NewLine +
                "Completed UTC: " + DateTime.UtcNow.ToString("O") + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("O10 candidate assertion failed: " + message);
        }
    }
}
