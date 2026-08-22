using System;
using System.IO;
using KimSurvival;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KimSurvival.EditorTools
{
    public static class PrototypeProjectBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string VerificationFolder = "Artifacts/Verification";

        [MenuItem("Kim Survival/Create Prototype Scene")]
        public static void CreateProject()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");
            Directory.CreateDirectory(VerificationFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("[BOOTSTRAP] 김씨 생존기: 무인도");
            root.AddComponent<KimSurvivalPrototype>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            PlayerSettings.companyName = "Kim Survival Studio";
            PlayerSettings.productName = "김씨 생존기: 무인도";
            PlayerSettings.bundleVersion = "0.1.0-prototype";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            QualitySettings.vSyncCount = 1;
            EditorSettings.enterPlayModeOptionsEnabled = false;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.WriteAllText(Path.Combine(VerificationFolder, "project-bootstrap.txt"),
                "PASS\n" +
                "Unity: " + Application.unityVersion + "\n" +
                "Scene: " + ScenePath + "\n" +
                "Resolution targets: 1920x1080, 1280x800\n" +
                "Placeholder asset IDs remain wired in KimSurvivalPrototype.cs\n");
            Debug.Log("[Kim Survival] Prototype scene created: " + ScenePath);
        }

        [MenuItem("Kim Survival/Run Edit Checks")]
        public static void RunEditChecks()
        {
            Directory.CreateDirectory(VerificationFolder);
            DateTime started = DateTime.UtcNow;

            GameSession inventory = new GameSession();
            Assert(inventory.BeginSearch(), "Inventory scenario begins search");
            Assert(inventory.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "Wood fills slot");
            Assert(inventory.TryGather(ResourceKind.Stone, 2) == GatherResult.Added, "Stone fills slot");
            Assert(inventory.TryGather(ResourceKind.Food, 2) == GatherResult.Added, "Food fills slot");
            Assert(inventory.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "Salvage fills slot");
            Assert(inventory.TryGather(ResourceKind.Wood, 1) == GatherResult.PendingSwap, "Full bag creates a real swap choice");
            Assert(inventory.HasPendingLoot, "Pending loot is recorded");
            Assert(inventory.ReplaceBagSlot(1), "Player can replace a selected slot");
            Assert(inventory.ReturnToCamp(false), "Bag transfers on return");
            Assert(inventory.GetStorage(ResourceKind.Wood) >= 3, "Returned wood reaches storage");

            GameSession progression = new GameSession();
            progression.Grant(ResourceKind.Wood, 20);
            progression.Grant(ResourceKind.Stone, 10);
            progression.Grant(ResourceKind.Food, 5);
            progression.Grant(ResourceKind.Salvage, 20);
            Assert(progression.TryBuild(StructureKind.Campfire), "Campfire builds once");
            Assert(progression.TryBuild(StructureKind.Workbench), "Workbench builds once");
            Assert(progression.TryBuild(StructureKind.RainCollector), "Rain collector builds once");
            Assert(progression.TryResearch(TechKind.StoneAxe), "Axe recipe researches");
            Assert(progression.TryCraft(TechKind.StoneAxe), "Axe crafts");
            Assert(progression.TryResearch(TechKind.Rope), "Rope recipe researches");
            Assert(progression.TryCraft(TechKind.Rope), "Rope crafts");
            Assert(progression.HasAxe && progression.HasRope, "Tools persist");
            Assert(progression.TryUpgradeSignal(), "Signal stage one builds");
            Assert(progression.TryUpgradeSignal(), "Signal stage two builds");
            Assert(progression.Result == RunResult.Rescued, "Signal completion wins the run");

            GameSession deadline = new GameSession();
            for (int day = 1; day <= GameSession.FinalDay; day += 1)
            {
                Assert(deadline.BeginSearch(), "Deadline scenario search day " + day);
                Assert(deadline.ReturnToCamp(false), "Deadline scenario returns day " + day);
                Assert(deadline.EndDay(), "Deadline scenario ends day " + day);
            }
            Assert(deadline.Result == RunResult.Deadline, "Third unfinished day fails with an explained deadline");

            string report =
                "PASS · deterministic edit checks\n" +
                "Started UTC: " + started.ToString("O") + "\n" +
                "Completed UTC: " + DateTime.UtcNow.ToString("O") + "\n" +
                "Checks: inventory overflow/swap, return transfer, camp structures, research, crafting, rescue success, deadline failure\n";
            File.WriteAllText(Path.Combine(VerificationFolder, "editmode-checks.txt"), report);
            Debug.Log("[Kim Survival] " + report.Replace('\n', ' '));
        }

        [MenuItem("Kim Survival/Build Windows Prototype")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
            {
                CreateProject();
            }

            Directory.CreateDirectory("Builds/Windows");
            Directory.CreateDirectory(VerificationFolder);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/KimSurvivalIsland.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string text =
                "Result: " + summary.result + "\n" +
                "Output: " + options.locationPathName + "\n" +
                "Size: " + summary.totalSize + " bytes\n" +
                "Duration: " + summary.totalTime + "\n" +
                "Errors: " + summary.totalErrors + "\n" +
                "Warnings: " + summary.totalWarnings + "\n";
            File.WriteAllText(Path.Combine(VerificationFolder, "windows-build.txt"), text);

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows build failed. " + text);
            }

            Debug.Log("[Kim Survival] Windows build succeeded: " + options.locationPathName);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("[Kim Survival check failed] " + message);
            }
        }
    }

    [InitializeOnLoad]
    public static class PrototypePlayModeVerifier
    {
        private const string RunningKey = "KimSurvival.PlayModeVerification.Running";
        private const string PassedKey = "KimSurvival.PlayModeVerification.Passed";
        private const string MessageKey = "KimSurvival.PlayModeVerification.Message";
        private const string VerificationFolder = "Artifacts/Verification";
        private static double earliestRunTime;
        private static double timeoutAt;
        private static bool tickAttached;

        static PrototypePlayModeVerifier()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Attach();
            }
        }

        [MenuItem("Kim Survival/Run Play Mode Verification")]
        public static void RunPlayModeVerification()
        {
            if (!File.Exists(PrototypeProjectBuilder.ScenePath))
            {
                PrototypeProjectBuilder.CreateProject();
            }

            Directory.CreateDirectory(VerificationFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "Verification did not complete.");
            Attach();
            EditorSceneManager.OpenScene(PrototypeProjectBuilder.ScenePath, OpenSceneMode.Single);
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
            timeoutAt = EditorApplication.timeSinceStartup + 45d;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

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
                SessionState.SetString(MessageKey, "FAIL · timed out waiting for the playable scene");
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
                string explorationScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-exploration-1280x800.png"));
                string result = prototype.RunAutomatedVerification(explorationScreenshot);
                string screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-playmode-1280x800.png"));
                prototype.CaptureVerificationPng(screenshot, 1280, 800);
                SessionState.SetBool(PassedKey, true);
                SessionState.SetString(MessageKey, result + "\nExploration screenshot: " + explorationScreenshot + "\nCamp screenshot: " + screenshot);
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
            Directory.CreateDirectory(VerificationFolder);
            File.WriteAllText(Path.Combine(VerificationFolder, "playmode-checks.txt"),
                (passed ? "PASS" : "FAIL") + "\n" + message + "\nCompleted UTC: " + DateTime.UtcNow.ToString("O") + "\n");

            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(PassedKey);
            SessionState.EraseString(MessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.Log("[Kim Survival] Play mode verification " + (passed ? "passed" : "failed") + ": " + message);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }
}
