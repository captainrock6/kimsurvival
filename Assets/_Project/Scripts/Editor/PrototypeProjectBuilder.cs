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
        private const string DefaultVerificationFolder = "Artifacts/Verification";

        private static string VerificationFolder
        {
            get
            {
                string overridePath = Environment.GetEnvironmentVariable("KIM_SURVIVAL_VERIFICATION_FOLDER");
                return string.IsNullOrWhiteSpace(overridePath) ? DefaultVerificationFolder : overridePath;
            }
        }

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

            GameSession landTravel = new GameSession();
            Assert(landTravel.BeginSearch(), "Land travel scenario begins search");
            landTravel.TickSearch(10f, true);
            float landEnergyCost = 100f - landTravel.Energy;
            float landDaylightCost = 100f - landTravel.Daylight;

            GameSession swimTravel = new GameSession();
            Assert(swimTravel.BeginSearch(), "Swimming scenario begins search");
            Assert(swimTravel.TryGather(ResourceKind.Salvage, 1, true) == GatherResult.Rejected, "Water node rejects land interaction");
            Assert(swimTravel.SetSwimming(true) && swimTravel.IsSwimming, "Shore entry enables swimming");
            swimTravel.TickSearch(10f, true);
            Assert(100f - swimTravel.Energy > landEnergyCost, "Swimming costs more energy than land movement");
            Assert(100f - swimTravel.Daylight > landDaylightCost, "Swimming costs more daylight than land movement");
            Assert(swimTravel.TryGather(ResourceKind.Salvage, 1, true) == GatherResult.Added, "Water node can be searched while swimming");
            Assert(swimTravel.SetSwimming(false) && !swimTravel.IsSwimming, "Shore exit restores land state");

            PrototypePlayerActions keyboardActions = PrototypePlayerActions.FromRaw(new PrototypeRawInput
            {
                KeyboardLeft = true,
                KeyboardJump = true,
                KeyboardInteract = true,
                KeyboardReturn = true,
                KeyboardCancel = true,
                BagSlotIndex = 2
            });
            PrototypePlayerActions gamepadActions = PrototypePlayerActions.FromRaw(new PrototypeRawInput
            {
                HorizontalAxis = -0.8f,
                GamepadJump = true,
                GamepadInteract = true,
                GamepadReturn = true,
                GamepadCancel = true,
                BagSlotIndex = -1
            });
            Assert(keyboardActions.Horizontal < 0f && gamepadActions.Horizontal < 0f, "Keyboard and gamepad share the move action");
            Assert(keyboardActions.JumpPressed && gamepadActions.JumpPressed, "Keyboard and gamepad share the jump action");
            Assert(keyboardActions.InteractPressed && gamepadActions.InteractPressed, "Keyboard and gamepad share the interact action");
            Assert(keyboardActions.ReturnPressed && gamepadActions.ReturnPressed, "Keyboard and gamepad share the return action");
            Assert(keyboardActions.CancelPressed && gamepadActions.CancelPressed, "Keyboard and gamepad share the cancel action");
            Assert(keyboardActions.BagSlotIndex == 2, "Keyboard loot slot maps into the shared action snapshot");

            PrototypeCampPlacementActions mousePlacementActions = PrototypeCampPlacementActions.FromRaw(new PrototypeRawCampPlacementInput
            {
                UsePointer = true,
                PointerWorldX = 1.5f,
                MouseConfirm = true,
                MouseCancel = true
            });
            PrototypeCampPlacementActions gamepadPlacementActions = PrototypeCampPlacementActions.FromRaw(new PrototypeRawCampPlacementInput
            {
                HorizontalAxis = 1f,
                GamepadConfirm = true,
                GamepadCancel = true
            });
            PrototypeCampPlacement mousePlacement = new PrototypeCampPlacement();
            PrototypeCampPlacement gamepadPlacement = new PrototypeCampPlacement();
            mousePlacement.Begin(StructureKind.Campfire, false);
            gamepadPlacement.Begin(StructureKind.Campfire, false);
            mousePlacement.Update(mousePlacementActions, 1f);
            gamepadPlacement.Update(gamepadPlacementActions, 1f);
            Assert(Mathf.Approximately(mousePlacement.CandidateX, gamepadPlacement.CandidateX), "Mouse and gamepad drive the same placement state");
            Assert(mousePlacementActions.ConfirmPressed && gamepadPlacementActions.ConfirmPressed, "Mouse and gamepad share placement confirm");
            Assert(mousePlacementActions.CancelPressed && gamepadPlacementActions.CancelPressed, "Mouse and gamepad share placement cancel");

            GameSession placementSession = new GameSession();
            PrototypeCampPlacement placement = new PrototypeCampPlacement();
            placement.Begin(StructureKind.Campfire, false);
            placement.SetCandidateX(1.26f);
            Assert(Mathf.Approximately(placement.CandidateX, 1.5f), "Placement snaps to the 0.5 metre floor grid");
            placement.SetCandidateX(-5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.OutsideCampBounds, "Camp bounds reject placement");
            placement.SetCandidateX(-2.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.BlocksEntrance, "Camp entrance rejects placement");
            placement.SetCandidateX(0f);
            Assert(placement.CurrentValidity == CampPlacementValidity.BlocksRequiredPath, "Required travel path rejects placement");
            placement.SetCandidateX(-1.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.Valid, "Campfire has a valid snapped location");
            Assert(placementSession.TryBuild(StructureKind.Campfire) && placement.Commit(), "Campfire placement spends build cost once");

            placementSession.Grant(ResourceKind.Wood, 2);
            placementSession.Grant(ResourceKind.Salvage, 1);
            placement.Begin(StructureKind.Workbench, false);
            placement.SetCandidateX(-1.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.OverlapsStructure, "Installed structure overlap is rejected");
            placement.SetCandidateX(1.5f);
            Assert(placementSession.TryBuild(StructureKind.Workbench) && placement.Commit(), "Workbench uses the shared placement rules");
            int woodBeforeMove = placementSession.GetStorage(ResourceKind.Wood);
            int stoneBeforeMove = placementSession.GetStorage(ResourceKind.Stone);
            int salvageBeforeMove = placementSession.GetStorage(ResourceKind.Salvage);
            placement.Begin(StructureKind.Workbench, true);
            placement.SetCandidateX(3.5f);
            Assert(placement.Commit(), "Installed workbench can be repositioned");
            Assert(placementSession.GetStorage(ResourceKind.Wood) == woodBeforeMove &&
                   placementSession.GetStorage(ResourceKind.Stone) == stoneBeforeMove &&
                   placementSession.GetStorage(ResourceKind.Salvage) == salvageBeforeMove, "Repositioning consumes no resources");

            GameSession shoreline = new GameSession();
            Assert(shoreline.BeginSearch(), "Traversal scenario begins search");
            PrototypePlayerTraversal traversal = new PrototypePlayerTraversal();
            traversal.Reset(PrototypePlayerTraversal.CoastlineX + 0.05f, PrototypePlayerTraversal.LandY);
            PrototypeTraversalStep enteredWater = traversal.Step(new PrototypePlayerActions(-1f, false, false, false, false, -1), 0.1f, 0f, shoreline);
            Assert(shoreline.IsSwimming && enteredWater.Presentation.IsSwimming, "Crossing the coastline enters swimming");
            PrototypeTraversalStep blockedSwimJump = traversal.Step(new PrototypePlayerActions(0f, true, false, false, false, -1), 0.1f, 0.5f, shoreline);
            Assert(blockedSwimJump.Presentation.IsSwimming && blockedSwimJump.Presentation.IsGrounded, "Jump is suppressed while swimming");
            traversal.Warp(PrototypePlayerTraversal.CoastlineX - 0.05f, PrototypePlayerTraversal.WaterY, true);
            PrototypeTraversalStep returnedToShore = traversal.Step(new PrototypePlayerActions(1f, false, false, false, false, -1), 0.1f, 1f, shoreline);
            Assert(!shoreline.IsSwimming && !returnedToShore.Presentation.IsSwimming, "Crossing back over the coastline exits swimming");
            Assert(Mathf.Approximately(traversal.Y, PrototypePlayerTraversal.LandY), "Shore return restores land height");

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
                "Checks: inventory overflow/swap, shared keyboard/gamepad actions, limited free placement input/state, grid snap, camp bounds, entrance/path protection, structure overlap, free repositioning, shore transitions, swimming jump suppression, swimming costs, water gathering, camp structures, research, crafting, rescue success, deadline failure\n";
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
        private const string DefaultVerificationFolder = "Artifacts/Verification";
        private static double earliestRunTime;
        private static double timeoutAt;
        private static bool tickAttached;

        private static string VerificationFolder
        {
            get
            {
                string overridePath = Environment.GetEnvironmentVariable("KIM_SURVIVAL_VERIFICATION_FOLDER");
                return string.IsNullOrWhiteSpace(overridePath) ? DefaultVerificationFolder : overridePath;
            }
        }

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
                string swimmingScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-swimming-1280x800.png"));
                string result = prototype.RunAutomatedVerification(explorationScreenshot, swimmingScreenshot);
                string screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-playmode-1280x800.png"));
                prototype.CaptureVerificationPng(screenshot, 1280, 800);
                SessionState.SetBool(PassedKey, true);
                SessionState.SetString(MessageKey, result + "\nSwimming screenshot: " + swimmingScreenshot + "\nExploration screenshot: " + explorationScreenshot + "\nCamp screenshot: " + screenshot);
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
