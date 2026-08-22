using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
            get { return Path.GetFullPath(Path.Combine(ProjectRoot, "work", "ParallelQA", RunId, "WindowsBuild")); }
        }

        private static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
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

            string report = Header("Deterministic Edit Check", started) +
                            string.Join(Environment.NewLine, results) + Environment.NewLine +
                            "Overall: " + (results.All(line => line.StartsWith("PASS", StringComparison.Ordinal)) ? "PASS" : "FAIL") + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "edit-checks.txt"), report, new UTF8Encoding(false));

            WriteInputCodePathAudit(started);
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
            Directory.CreateDirectory(BuildFolder);
            DateTime started = DateTime.UtcNow;
            string executable = Path.Combine(BuildFolder, "KimSurvivalIsland.exe");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
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
                          "BuildOptions: Development, AllowDebugging" + Environment.NewLine +
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
            session.Reset();
            InvokePrivate(prototype, "RefreshAll");
            Require(EventSystem.current != null, "EventSystem exists");
            Require(EventSystem.current.currentSelectedGameObject != null, "Camp UI has a selected control");
            Require(EventSystem.current.currentSelectedGameObject.name == "행동 9", "Expedition button receives initial focus");
            int initialReachableButtons = VerifyDirectionalNavigationFromCurrentSelection();

            string[] joysticks = Input.GetJoystickNames() ?? Array.Empty<string>();
            int activeJoysticks = joysticks.Count(name => !string.IsNullOrWhiteSpace(name));
            float uiScale1280x800 = Mathf.Sqrt((1280f / 1920f) * (800f / 1080f));
            float minimumUiTextPixels = 23f * uiScale1280x800;
            float nominalWorldTextPixels = 1f * 0.02f * 800f / (2f * 5.625f);

            // Day 1: start through UI Submit, enter/exit the water, and gather with no grants.
            Submit(GetButton(prototype, "phaseButton"));
            Require(session.Day == 1 && session.Phase == GamePhase.Exploring, "Day 1 started via UI Submit");
            float energyBeforeLandGather = session.Energy;
            GatherAt(prototype, -1.1f, false);
            float landGatherCost = energyBeforeLandGather - session.Energy;
            float energyBeforeWaterGather = session.Energy;
            GatherAt(prototype, -8.2f, true);
            float waterGatherCost = energyBeforeWaterGather - session.Energy;
            Require(waterGatherCost > landGatherCost, "Water gather costs more energy than land gather");
            Capture(prototype, "playmode-day1-swimming-1280x800.png");
            GatherAt(prototype, 6.8f, false);
            GatherAt(prototype, 1.5f, false);
            Require(session.ReturnToCamp(false), "Day 1 returned to camp");
            InvokePrivate(prototype, "RefreshAll");
            int dayOneReachableButtons = VerifyDirectionalNavigationFromCurrentSelection();
            Submit(GetButton(prototype, "workbenchButton"));
            Submit(GetButton(prototype, "researchRopeButton"));
            Submit(GetButton(prototype, "craftRopeButton"));
            Submit(GetButton(prototype, "researchAxeButton"));
            Submit(GetButton(prototype, "craftAxeButton"));
            Require(session.HasAxe && session.HasRope, "Both tools crafted through UI Submit");
            Submit(GetButton(prototype, "phaseButton"));
            Require(session.Day == 2 && session.Phase == GamePhase.Camp, "Day 1 settlement advanced to Day 2");

            // Day 2: exploit both tool benefits and resolve overflow through selected bag UI.
            Submit(GetButton(prototype, "phaseButton"));
            GatherAt(prototype, -1.1f, false);
            GatherAt(prototype, 10.2f, false);
            GatherAt(prototype, -8.2f, true);
            GatherAt(prototype, 6.8f, false);
            Require(session.HasPendingLoot, "Day 2 overflow reached pending choice");
            InvokePrivate(prototype, "RefreshAll");
            Require(EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.name == "가방 0", "Bag slot receives focus for gamepad/UI replacement");
            Submit(EventSystem.current.currentSelectedGameObject.GetComponent<Button>());
            Require(!session.HasPendingLoot, "Pending loot replaced through UI Submit");
            Capture(prototype, "playmode-day2-exploration-1280x800.png");
            Require(session.ReturnToCamp(false), "Day 2 returned to camp");
            InvokePrivate(prototype, "RefreshAll");
            Submit(GetButton(prototype, "phaseButton"));
            Require(session.Day == 3, "Day 2 settlement advanced to Day 3");

            // Day 3: gather the exact remaining resources and finish the rescue signal.
            Submit(GetButton(prototype, "phaseButton"));
            GatherAt(prototype, -1.1f, false);
            GatherAt(prototype, 10.2f, false);
            GatherAt(prototype, 1.5f, false);
            GatherAt(prototype, 6.8f, false);
            Require(session.HasPendingLoot, "Day 3 overflow reached pending choice");
            InvokePrivate(prototype, "RefreshAll");
            Submit(EventSystem.current.currentSelectedGameObject.GetComponent<Button>());
            Require(session.ReturnToCamp(false), "Day 3 returned to camp");
            InvokePrivate(prototype, "RefreshAll");
            Submit(GetButton(prototype, "campfireButton"));
            Submit(GetButton(prototype, "rainButton"));
            Submit(GetButton(prototype, "signalButton"));
            Submit(GetButton(prototype, "signalButton"));
            Require(session.Result == RunResult.Rescued && session.Phase == GamePhase.Result, "Natural three-day Play Mode route reaches rescue result");
            Capture(prototype, "playmode-rescue-result-1280x800.png");

            return Header("Play Mode natural full-loop verification", started) +
                   "PASS · No Grant calls used by the Play Mode route." + Environment.NewLine +
                   "PASS · Day 1-3 camp/search/return/settlement and rescue result." + Environment.NewLine +
                   "PASS · Shore entry, water gather, shore exit, and higher water-gather energy cost." + Environment.NewLine +
                   "PASS · Camp actions and bag replacement invoked through EventSystem Submit." + Environment.NewLine +
                   "PASS · Directional navigation reached all enabled camp buttons (initial " + initialReachableButtons + ", after Day 1 " + dayOneReachableButtons + ")." + Environment.NewLine +
                   "PASS · 1280x800 render-target captures produced." + Environment.NewLine +
                   "Screen reported by Unity: " + Screen.width + "x" + Screen.height + Environment.NewLine +
                   "Estimated minimum uGUI text size at 1280x800: " + minimumUiTextPixels.ToString("0.0") + " px." + Environment.NewLine +
                   "FAIL (visual): nominal world TextMesh character height at 1280x800 is approximately " + nominalWorldTextPixels.ToString("0.0") + " px; inspect captures." + Environment.NewLine +
                   "Detected non-empty joystick names: " + activeJoysticks + Environment.NewLine +
                   "Joystick names: " + (activeJoysticks == 0 ? "<none>" : string.Join(" | ", joysticks.Where(name => !string.IsNullOrWhiteSpace(name)))) + Environment.NewLine +
                   "Physical gamepad execution: " + (activeJoysticks == 0 ? "UNVERIFIED (no device exposed to Unity batch Play Mode)" : "UNVERIFIED (device detected, no human actuation captured)") + Environment.NewLine;
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

        private static void WriteInputCodePathAudit(DateTime started)
        {
            string runtimePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs");
            string inputPath = Path.Combine(ProjectRoot, "ProjectSettings", "InputManager.asset");
            string runtime = File.ReadAllText(runtimePath);
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
                Contains(input, "m_Name: Submit", "uGUI Submit axis exists"),
                Contains(input, "altPositiveButton: joystick button 0", "uGUI gamepad submit mapping"),
                Contains(input, "m_Name: Cancel", "uGUI Cancel axis exists"),
                Contains(input, "positiveButton: joystick button 1", "uGUI gamepad cancel mapping"),
                Contains(input, "type: 2\n    axis: 0", "legacy joystick horizontal axis mapping"),
                Contains(input, "type: 2\n    axis: 1", "legacy joystick vertical axis mapping")
            };

            bool dpadAxesPresent = input.Contains("axis: 5") || input.Contains("axis: 6") || input.Contains("axis: 7");
            string report = Header("Keyboard and gamepad code-path audit", started) +
                            string.Join(Environment.NewLine, checks) + Environment.NewLine +
                            (dpadAxesPresent
                                ? "PASS · Additional legacy joystick axes that may cover D-pad are configured."
                                : "FAIL · No additional legacy joystick axes are configured for the documented D-pad path; only primary axes 0/1 are mapped.") + Environment.NewLine +
                            "Execution scope: UI Submit is exercised in Play Mode; physical keyboard/gamepad actuation is reported separately in playmode-full-loop.txt." + Environment.NewLine;
            File.WriteAllText(Path.Combine(EvidenceFolder, "input-code-path-audit.txt"), report, new UTF8Encoding(false));
        }

        private static void GatherAt(KimSurvivalPrototype prototype, float x, bool swimming)
        {
            GameSession session = prototype.Session;
            if (swimming)
            {
                Require(session.SetSwimming(true), "enter water at " + x);
            }
            else if (session.IsSwimming)
            {
                Require(session.SetSwimming(false), "exit water at " + x);
            }

            SetPrivateField(prototype, "playerX", x);
            SetPrivateField(prototype, "playerY", swimming ? -1.88f : -2.15f);
            InvokePrivate(prototype, "ApplyPlayerPresentation", swimming ? -1f : 1f);
            InvokePrivate(prototype, "GatherNearestNode");
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
                   "Project: " + ProjectRoot + Environment.NewLine +
                   "Command: " + string.Join(" ", Environment.GetCommandLineArgs().Select(Quote)) + Environment.NewLine;
        }

        private static string Quote(string value)
        {
            return value.IndexOf(' ') >= 0 ? "\"" + value + "\"" : value;
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
