using System;
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
    /// Independent red-first gate for the compact camp interaction prompt.
    /// Product runtime is observed through reflection; this runner never changes
    /// scenes, runtime assets, localization tables, or progression data.
    /// </summary>
    internal static class Wave9InteractionPromptLayoutGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string QpsLongLocaleCode = "qps-long";
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 800;
        private const float MaximumPromptWidth = CaptureWidth * 0.40f;
        private const float MaximumPromptHeight = 50f;
        private const float MinimumNarrationGap = 8f;
        private const float MaximumNarrationGap = 32f;
        private const string PlayRunningKey = "ParallelQA.Wave9Prompt.Play.Running";
        private const string PlayInfraPassedKey = "ParallelQA.Wave9Prompt.Play.InfraPassed";
        private const string PlayMessageKey = "ParallelQA.Wave9Prompt.Play.Message";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool playTickAttached;
        private static double earliestPlayTime;
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
            public string command;
            public string productOverall;
            public string infrastructureOverall;
            public string overall;
            public int passed;
            public int expectedFailed;
            public int productFailed;
            public int infrastructureFailed;
            public int unverified;
            public Check[] checks;
        }

        [Serializable]
        private sealed class PixelRect
        {
            public float x;
            public float y;
            public float width;
            public float height;

            public static PixelRect From(Rect value)
            {
                return new PixelRect
                {
                    x = value.x,
                    y = value.y,
                    width = value.width,
                    height = value.height
                };
            }

            public override string ToString()
            {
                return "x=" + x.ToString("0.0") + " y=" + y.ToString("0.0") +
                       " w=" + width.ToString("0.0") + " h=" + height.ToString("0.0");
            }
        }

        [Serializable]
        private sealed class TargetStateEvidence
        {
            public string target;
            public int farPromptCount;
            public int nearPromptCount;
            public int popupOpenPromptCount;
            public int popupCount;
            public int restoredPromptCount;
            public string nearTargetKind;
            public string restoredTargetKind;
            public string promptText;
        }

        [Serializable]
        private sealed class LocaleLayoutEvidence
        {
            public string locale;
            public string device;
            public bool actualLocale;
            public bool syntheticStress;
            public string target;
            public string promptText;
            public string screenshot;
            public PixelRect promptRect;
            public PixelRect narrationRect;
            public PixelRect topHudRect;
            public PixelRect bottomHelpRect;
            public PixelRect playerRect;
            public PixelRect traversalPathRect;
            public PixelRect[] facilityRects;
            public float narrationGap;
            public bool belowNarration;
            public bool adjacentToNarration;
            public bool withinSizeCap;
            public bool insideScreen;
            public bool tmpOverflow;
            public bool overlapsTopHud;
            public bool overlapsBottomHelp;
            public bool overlapsPlayer;
            public bool overlapsFacility;
            public bool overlapsTraversalPath;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public int width = CaptureWidth;
            public int height = CaptureHeight;
            public float maximumPromptWidth = MaximumPromptWidth;
            public float maximumPromptHeight = MaximumPromptHeight;
            public float minimumNarrationGap = MinimumNarrationGap;
            public float maximumNarrationGap = MaximumNarrationGap;
            public TargetStateEvidence[] targets;
            public LocaleLayoutEvidence[] layouts;
            public string[] joystickNames;
            public string physicalGamepad;
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
                return string.IsNullOrWhiteSpace(value) ? "manual-wave9-prompt" : Sanitize(value);
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
            List<Check> checks = new List<Check>();

            string runtimePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs");
            string inputPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "PrototypePlayerInput.cs");
            string tablePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            string runtime = File.ReadAllText(runtimePath);
            string input = File.ReadAllText(inputPath);
            string[] rows = File.ReadAllLines(tablePath);

            bool stateSurface = typeof(PrototypeCampInteraction).GetProperty("HasProximityPrompt") != null &&
                                typeof(PrototypeCampInteraction).GetProperty("IsPopupOpen") != null &&
                                typeof(PrototypeCampInteraction).GetMethod("ClosePopup") != null;
            Product(checks, "W9P-E01.prompt_state_surface", "far/near/popup/close", "P0",
                "The runtime exposes prompt, popup, and close/restore states without locale text as identity",
                stateSurface,
                "HasProximityPrompt/IsPopupOpen/ClosePopup discoverable=" + stateSurface,
                "Reflect PrototypeCampInteraction and inspect target identity before and after ClosePopup.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampInteraction.cs");

            bool sharedAction = input.Contains("KeyboardInteract = Input.GetKeyDown(KeyCode.E)") &&
                                input.Contains("GamepadInteract = Input.GetKeyDown(KeyCode.JoystickButton2)") &&
                                input.Contains("KeyboardInteract || raw.GamepadInteract");
            Infrastructure(checks, "W9P-E02.shared_input_action", "keyboard/gamepad", "P0",
                "Keyboard E and gamepad X converge on the same InteractPressed action",
                () => sharedAction ? "keyboard=E gamepad=X shared=InteractPressed" : throw new InvalidOperationException("shared input path missing"),
                "Source-audit PrototypePlayerInput and construct equivalent raw keyboard/gamepad actions.",
                "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");

            string keyboardRow = rows.FirstOrDefault(row => row.StartsWith("camp.interaction.prompt.keyboard_mouse\t", StringComparison.Ordinal));
            string gamepadRow = rows.FirstOrDefault(row => row.StartsWith("camp.interaction.prompt.gamepad\t", StringComparison.Ordinal));
            bool localizedRows = HasPromptColumns(keyboardRow, "[E]") && HasPromptColumns(gamepadRow, "[X]") &&
                                 CountTokens(keyboardRow) >= 2 && CountTokens(gamepadRow) >= 2;
            Product(checks, "W9P-E03.localized_prompt_keys", "ko/en/input", "P0",
                "Stable keyboard and gamepad prompt keys preserve one target placeholder in Korean and English",
                localizedRows,
                "keyboardRow=" + Normalize(keyboardRow) + " gamepadRow=" + Normalize(gamepadRow),
                "Inspect the two camp.interaction.prompt rows and compare locale placeholder sets.",
                "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv; Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");

            bool actualQpsData = rows.Length > 0 && rows[0].Split('\t').Any(value => value == QpsLongLocaleCode);
            Expected(checks, "W9P-E04.actual_qps_long_locale", "qps-long", "P1",
                "A non-shipping qps-long locale exists as data and can render the prompt without runtime branching",
                actualQpsData,
                actualQpsData ? "qps-long column present" : "qps-long column absent; synthetic stress cannot substitute for an actual locale",
                "Inspect the localization source header and AvailableLocales, then select qps-long in the Play gate.",
                "Assets/_Project/Scripts/Localization/**; future locale configuration");

            bool currentLayoutIsLegacyLarge = runtime.Contains("new Vector2(0.29f, 0.25f)") &&
                                              runtime.Contains("new Vector2(0.71f, 0.36f)") &&
                                              runtime.Contains(", 34, TextAnchor.MiddleCenter");
            Infrastructure(checks, "W9P-E05.red_first_discriminator", "gate", "P0",
                "The gate contains an independent discriminator for the reported large lower-world prompt",
                () => currentLayoutIsLegacyLarge
                    ? "baseline source signature observed; Play gate must report expected RED on measured pixels"
                    : "baseline source signature changed; Play measurements decide PASS/FAIL",
                "Run the Play contract at 1280x800 and inspect prompt-layout-evidence.json.",
                "Assets/Editor/ParallelQA/Wave9InteractionPromptLayoutGateRunner.cs");

            WriteReport("prompt-layout-edit-contracts", "Wave 9 interaction prompt Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayInfraPassedKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · prompt Play contract did not complete");
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
            earliestPlayTime = EditorApplication.timeSinceStartup + 2d;
            playTimeoutAt = EditorApplication.timeSinceStartup + 180d;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                earliestPlayTime = EditorApplication.timeSinceStartup + 2d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 180d;
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
            if (now < earliestPlayTime) return;
            if (now > playTimeoutAt)
            {
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · timed out waiting for playable scene");
                StopPlayContracts();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;

            try
            {
                DateTime started = DateTime.UtcNow;
                List<Check> checks = new List<Check>();
                PlayEvidence evidence = ExecutePlayContracts(prototype, checks);
                WriteJson("prompt-layout-evidence.json", evidence);
                Report report = WriteReport("prompt-layout-play-contracts", "Wave 9 interaction prompt Play contracts", started, checks);
                bool infraPassed = report.infrastructureOverall == "PASS";
                SessionState.SetBool(PlayInfraPassedKey, infraPassed);
                SessionState.SetString(PlayMessageKey,
                    "Product=" + report.productOverall + " Infrastructure=" + report.infrastructureOverall +
                    " PhysicalGamepad=UNVERIFIED Evidence=" + Path.Combine(EvidenceFolder, "prompt-layout-evidence.json"));
            }
            catch (Exception exception)
            {
                List<Check> failures = new List<Check>();
                Infrastructure(failures, "W9P-I99.play_runner", "infrastructure", "P0",
                    "The prompt Play runner produces parseable evidence",
                    () => throw exception,
                    "Run the prompt Play method outside the Codex sandbox and inspect its Unity log.",
                    "Assets/Editor/ParallelQA/Wave9InteractionPromptLayoutGateRunner.cs");
                WriteReport("prompt-layout-play-contracts", "Wave 9 prompt Play infrastructure failure", DateTime.UtcNow, failures);
                SessionState.SetBool(PlayInfraPassedKey, false);
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · " + exception);
            }

            StopPlayContracts();
        }

        private static PlayEvidence ExecutePlayContracts(KimSurvivalPrototype prototype, List<Check> checks)
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
            session.Grant(ResourceKind.Wood, 30);
            session.Grant(ResourceKind.Stone, 20);
            session.Grant(ResourceKind.Salvage, 30);
            session.Grant(ResourceKind.Food, 10);
            Require(session.TryBuild(StructureKind.Campfire), "fixture campfire built");
            Require(session.TryBuild(StructureKind.Workbench), "fixture workbench built");
            Require(session.TryBuild(StructureKind.RainCollector), "fixture rain collector built");
            placement.EnsureInstalled(StructureKind.Campfire);
            placement.EnsureInstalled(StructureKind.Workbench);
            placement.EnsureInstalled(StructureKind.RainCollector);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            SetDevice(prototype, PrototypeInputDevice.KeyboardMouse);
            Invoke(prototype, "RefreshAll");

            campUse.Warp(PrototypeCampUse.PlayerStartX);
            Invoke(prototype, "RefreshAll");
            int farPromptCount = ActivePromptCount(prototype);
            int farPopupCount = ActivePopupCount(prototype, interaction);
            Product(checks, "W9P-P01.far_silent", "far", "P0",
                "Outside use range there are zero proximity prompts and zero facility popups",
                farPromptCount == 0 && farPopupCount == 0,
                "farPromptCount=" + farPromptCount + " farPopupCount=" + farPopupCount,
                "Reset the camp, warp to PlayerStartX, refresh, and enumerate active prompt/popup roots.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeCampInteraction.cs");

            PrototypeCampInteractionTargetKind[] kinds =
            {
                PrototypeCampInteractionTargetKind.Campfire,
                PrototypeCampInteractionTargetKind.Workbench,
                PrototypeCampInteractionTargetKind.RainCollector,
                PrototypeCampInteractionTargetKind.RescueSignal
            };
            List<TargetStateEvidence> targets = new List<TargetStateEvidence>();
            foreach (PrototypeCampInteractionTargetKind kind in kinds)
            {
                Vector2 position = InvokeResult<Vector2>(prototype, "GetCampInteractionTargetPosition", kind);
                campUse.Warp(PrototypeCampUse.PlayerStartX);
                Invoke(prototype, "RefreshAll");
                int targetFar = ActivePromptCount(prototype);
                campUse.Warp(position);
                Invoke(prototype, "RefreshAll");
                int near = ActivePromptCount(prototype);
                string nearKind = interaction.ActiveTargetKind.ToString();
                string promptText = GetField<TMP_Text>(prototype, "campProximityText").text;
                Invoke(prototype, "UseNearestCampTarget");
                int popupPrompt = ActivePromptCount(prototype);
                int popupCount = ActivePopupCount(prototype, interaction);
                Invoke(prototype, "CancelCampPopup");
                int restored = ActivePromptCount(prototype);
                string restoredKind = interaction.ActiveTargetKind.ToString();
                targets.Add(new TargetStateEvidence
                {
                    target = kind.ToString(),
                    farPromptCount = targetFar,
                    nearPromptCount = near,
                    popupOpenPromptCount = popupPrompt,
                    popupCount = popupCount,
                    restoredPromptCount = restored,
                    nearTargetKind = nearKind,
                    restoredTargetKind = restoredKind,
                    promptText = promptText
                });
            }

            bool allNear = targets.All(item => item.farPromptCount == 0 && item.nearPromptCount == 1 && item.nearTargetKind == item.target);
            bool allPopup = targets.All(item => item.popupOpenPromptCount == 0 && item.popupCount == 1);
            bool allRestore = targets.All(item => item.restoredPromptCount == 1 && item.restoredTargetKind == item.target);
            Product(checks, "W9P-P02.four_single_near_prompts", "near", "P0",
                "Campfire, workbench, rain collector, and rescue signal each expose exactly one same-target prompt only when near",
                allNear,
                Join(targets.Select(item => item.target + " far=" + item.farPromptCount + " near=" + item.nearPromptCount + " kind=" + item.nearTargetKind)),
                "Approach each facility separately and count active prompt roots before interacting.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");
            Product(checks, "W9P-P03.popup_hides_prompt", "popup-open", "P0",
                "Opening a facility popup hides the proximity prompt and exposes one matching popup",
                allPopup,
                Join(targets.Select(item => item.target + " prompt=" + item.popupOpenPromptCount + " popup=" + item.popupCount)),
                "From each near state invoke shared Interact and enumerate prompt/popup roots.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");
            Product(checks, "W9P-P04.cancel_restores_same_target", "cancel/close", "P0",
                "Cancel/close restores exactly one prompt for the same nearby target",
                allRestore,
                Join(targets.Select(item => item.target + " restored=" + item.restoredPromptCount + " kind=" + item.restoredTargetKind)),
                "Open each target popup, cancel it, and compare the restored stable target kind.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeCampInteraction.cs");

            Vector2 campfirePosition = InvokeResult<Vector2>(prototype, "GetCampInteractionTargetPosition", PrototypeCampInteractionTargetKind.Campfire);
            List<LocaleLayoutEvidence> layouts = new List<LocaleLayoutEvidence>();
            layouts.Add(CaptureLayout(prototype, localization, campUse, interaction, campfirePosition,
                PrototypeLocalization.KoreanLocaleCode, PrototypeInputDevice.KeyboardMouse, false, "prompt-near-campfire-ko-keyboard-1280x800.png"));
            layouts.Add(CaptureLayout(prototype, localization, campUse, interaction, campfirePosition,
                PrototypeLocalization.EnglishLocaleCode, PrototypeInputDevice.Gamepad, false, "prompt-near-campfire-en-gamepad-1280x800.png"));

            localization.SetLocale(QpsLongLocaleCode, false);
            bool actualQps = localization.CurrentLocaleCode == QpsLongLocaleCode;
            layouts.Add(CaptureLayout(prototype, localization, campUse, interaction, campfirePosition,
                actualQps ? QpsLongLocaleCode : PrototypeLocalization.EnglishLocaleCode,
                PrototypeInputDevice.KeyboardMouse,
                !actualQps,
                "prompt-near-campfire-qps-long-1280x800.png"));

            bool sizeAndPlacement = layouts.All(layout => layout.withinSizeCap && layout.belowNarration && layout.adjacentToNarration);
            bool clearWorld = layouts.All(layout => !layout.overlapsTopHud && !layout.overlapsBottomHelp &&
                                                     !layout.overlapsPlayer && !layout.overlapsFacility && !layout.overlapsTraversalPath);
            bool textFit = layouts.All(layout => layout.insideScreen && !layout.tmpOverflow);
            Expected(checks, "W9P-P05.compact_below_narration", "1280x800 layout", "P1",
                "Prompt is directly below the narration card with an 8-32px gap, width <=512px, and height <=50px",
                sizeAndPlacement,
                Join(layouts.Select(LayoutActual)),
                "Open each 1280x800 prompt capture at 1:1 and compare prompt/narration pixel rectangles in prompt-layout-evidence.json.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs (BuildUi prompt anchors/font size)");
            Expected(checks, "W9P-P06.world_and_hud_clearance", "1280x800 occlusion", "P1",
                "Prompt overlaps neither upper HUD, lower help, player, facilities, nor the required traversal band",
                clearWorld,
                Join(layouts.Select(layout => layout.locale + "/" + layout.device +
                    " hud=" + layout.overlapsTopHud + " help=" + layout.overlapsBottomHelp +
                    " player=" + layout.overlapsPlayer + " facility=" + layout.overlapsFacility +
                    " path=" + layout.overlapsTraversalPath)),
                "Compare prompt Rect with UI and camera-projected world bounds in prompt-layout-evidence.json.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs (BuildUi prompt layout)");
            Expected(checks, "W9P-P07.locale_text_fit", "ko/en/qps-long", "P1",
                "KO, EN, and actual qps-long prompts remain inside screen with no TMP overflow or clipping",
                textFit && actualQps,
                "actualQps=" + actualQps + " layouts=" + Join(layouts.Select(layout => layout.locale + ":overflow=" + layout.tmpOverflow + ",inside=" + layout.insideScreen)),
                "Select ko, en, then qps-long on the same near-target state; inspect TMP overflow and screen bounds.",
                "Assets/_Project/Scripts/Localization/**; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            string koTarget = localizationTarget(prototype, localization, PrototypeLocalization.KoreanLocaleCode, PrototypeInputDevice.KeyboardMouse, campfirePosition);
            string enKeyboardTarget = localizationTarget(prototype, localization, PrototypeLocalization.EnglishLocaleCode, PrototypeInputDevice.KeyboardMouse, campfirePosition);
            string enGamepadTarget = localizationTarget(prototype, localization, PrototypeLocalization.EnglishLocaleCode, PrototypeInputDevice.Gamepad, campfirePosition);
            bool promptMeaning = koTarget.Contains("Campfire|Campfire|[E]") &&
                                 enKeyboardTarget.Contains("Campfire|Campfire|[E]") &&
                                 enGamepadTarget.Contains("Campfire|Campfire|[X]");
            Product(checks, "W9P-P08.locale_device_semantics", "locale/device", "P0",
                "Locale/device changes alter glyph/text only while preserving the Campfire target and Interact action",
                promptMeaning,
                "ko=" + koTarget + " enKeyboard=" + enKeyboardTarget + " enGamepad=" + enGamepadTarget,
                "Keep the same campfire snapshot, switch locale/device, and compare stable target kind plus action marker.",
                "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs; Assets/_Project/Scripts/Localization/**");

            string[] joysticks = (Input.GetJoystickNames() ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            checks.Add(new Check
            {
                id = "W9P-HW01.physical_gamepad",
                matrix = "hardware input",
                status = "UNVERIFIED",
                classification = "HARDWARE_GAP",
                severity = "P1",
                expected = "A person approaches all four targets and opens/closes their prompts with a physical gamepad",
                actual = joysticks.Length == 0 ? "no non-empty joystick name exposed to Unity batch Play Mode" : "device name observed but no human actuation evidence: " + Join(joysticks),
                reproduction = "Run the Windows development build with a physical gamepad and record device/actuation evidence.",
                recommendedFiles = "manual release-candidate hardware evidence"
            });

            Infrastructure(checks, "W9P-I01.fresh_captures_and_metrics", "evidence", "P0",
                "Fresh 1280x800 KO/EN/qps stress captures and machine-readable pixel metrics are generated",
                () => layouts.All(layout => File.Exists(Path.Combine(EvidenceFolder, layout.screenshot)))
                    ? "captures=" + layouts.Count + " metrics=" + layouts.Count
                    : throw new InvalidOperationException("one or more fresh captures are missing"),
                "Run the Play contract with a fresh run ID and inspect the evidence directory.",
                "Assets/Editor/ParallelQA/Wave9InteractionPromptLayoutGateRunner.cs");

            return new PlayEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                targets = targets.ToArray(),
                layouts = layouts.ToArray(),
                joystickNames = joysticks,
                physicalGamepad = "UNVERIFIED"
            };
        }

        private static LocaleLayoutEvidence CaptureLayout(
            KimSurvivalPrototype prototype,
            PrototypeLocalization localization,
            PrototypeCampUse campUse,
            PrototypeCampInteraction interaction,
            Vector2 targetPosition,
            string localeCode,
            PrototypeInputDevice device,
            bool syntheticStress,
            string screenshot)
        {
            if (interaction.IsPopupOpen) Invoke(prototype, "CancelCampPopup");
            localization.SetLocale(localeCode, false);
            bool actualLocale = localization.CurrentLocaleCode == localeCode && !syntheticStress;
            SetDevice(prototype, device);
            campUse.Warp(targetPosition);
            Invoke(prototype, "RefreshAll");
            TMP_Text promptText = GetField<TMP_Text>(prototype, "campProximityText");
            if (syntheticStress)
            {
                promptText.text = Wave3VisualGate.ExpandPseudoLong(promptText.text);
            }
            promptText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();

            GameObject prompt = GetField<GameObject>(prototype, "campProximityPrompt");
            Rect promptRect = UiPixelRect(prompt.GetComponent<RectTransform>());
            Rect narrationRect = UiPixelRect(GetField<UnityEngine.UI.Image>(prototype, "messagePanelImage").rectTransform);
            Rect topHudRect = UiPixelRect(GetField<TMP_Text>(prototype, "statusText").transform.parent.GetComponent<RectTransform>());
            Rect bottomHelpRect = UiPixelRect(GetField<TMP_Text>(prototype, "controlsText").transform.parent.GetComponent<RectTransform>());
            Rect playerRect = WorldObjectPixelRect(GetField<Transform>(prototype, "playerRoot"), GetField<Camera>(prototype, "worldCamera"));
            Rect pathRect = TraversalPathRect(GetField<Camera>(prototype, "worldCamera"));
            Rect[] facilityRects = FacilityPixelRects(prototype);
            float gap = narrationRect.yMin - promptRect.yMax;

            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, screenshot), CaptureWidth, CaptureHeight);
            return new LocaleLayoutEvidence
            {
                locale = syntheticStress ? QpsLongLocaleCode : localeCode,
                device = device.ToString(),
                actualLocale = actualLocale,
                syntheticStress = syntheticStress,
                target = interaction.ActiveTargetKind.ToString(),
                promptText = promptText.text,
                screenshot = screenshot,
                promptRect = PixelRect.From(promptRect),
                narrationRect = PixelRect.From(narrationRect),
                topHudRect = PixelRect.From(topHudRect),
                bottomHelpRect = PixelRect.From(bottomHelpRect),
                playerRect = PixelRect.From(playerRect),
                traversalPathRect = PixelRect.From(pathRect),
                facilityRects = facilityRects.Select(PixelRect.From).ToArray(),
                narrationGap = gap,
                belowNarration = promptRect.yMax <= narrationRect.yMin - MinimumNarrationGap,
                adjacentToNarration = gap >= MinimumNarrationGap && gap <= MaximumNarrationGap,
                withinSizeCap = promptRect.width <= MaximumPromptWidth + 0.5f && promptRect.height <= MaximumPromptHeight + 0.5f,
                insideScreen = promptRect.xMin >= -0.5f && promptRect.yMin >= -0.5f && promptRect.xMax <= CaptureWidth + 0.5f && promptRect.yMax <= CaptureHeight + 0.5f,
                tmpOverflow = promptText.isTextOverflowing,
                overlapsTopHud = promptRect.Overlaps(topHudRect),
                overlapsBottomHelp = promptRect.Overlaps(bottomHelpRect),
                overlapsPlayer = promptRect.Overlaps(playerRect),
                overlapsFacility = facilityRects.Any(promptRect.Overlaps),
                overlapsTraversalPath = promptRect.Overlaps(pathRect)
            };
        }

        private static Rect UiPixelRect(RectTransform transform)
        {
            Canvas rootCanvas = transform.GetComponentInParent<Canvas>().rootCanvas;
            RectTransform canvasTransform = rootCanvas.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            Vector3 lowerLeft = canvasTransform.InverseTransformPoint(corners[0]);
            Vector3 upperRight = canvasTransform.InverseTransformPoint(corners[2]);
            Rect canvasRect = canvasTransform.rect;
            float xMin = Mathf.InverseLerp(canvasRect.xMin, canvasRect.xMax, lowerLeft.x) * CaptureWidth;
            float yMin = Mathf.InverseLerp(canvasRect.yMin, canvasRect.yMax, lowerLeft.y) * CaptureHeight;
            float xMax = Mathf.InverseLerp(canvasRect.xMin, canvasRect.xMax, upperRight.x) * CaptureWidth;
            float yMax = Mathf.InverseLerp(canvasRect.yMin, canvasRect.yMax, upperRight.y) * CaptureHeight;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect WorldObjectPixelRect(Transform root, Camera camera)
        {
            if (root == null) return Rect.zero;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return Rect.zero;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i += 1) bounds.Encapsulate(renderers[i].bounds);
            return WorldBoundsPixelRect(bounds, camera);
        }

        private static Rect WorldBoundsPixelRect(Bounds bounds, Camera camera)
        {
            Vector3 min = camera.WorldToViewportPoint(bounds.min);
            Vector3 max = camera.WorldToViewportPoint(bounds.max);
            return Rect.MinMaxRect(
                Mathf.Min(min.x, max.x) * CaptureWidth,
                Mathf.Min(min.y, max.y) * CaptureHeight,
                Mathf.Max(min.x, max.x) * CaptureWidth,
                Mathf.Max(min.y, max.y) * CaptureHeight);
        }

        private static Rect[] FacilityPixelRects(KimSurvivalPrototype prototype)
        {
            Camera camera = GetField<Camera>(prototype, "worldCamera");
            Dictionary<StructureKind, GameObject> structures = GetField<Dictionary<StructureKind, GameObject>>(prototype, "structureViews");
            List<Rect> rects = new List<Rect>();
            foreach (GameObject structure in structures.Values)
            {
                Rect rect = WorldObjectPixelRect(structure.transform, camera);
                if (rect.width > 0f && rect.height > 0f) rects.Add(rect);
            }
            SpriteRenderer signal = GetField<SpriteRenderer>(prototype, "rescueSignalRenderer");
            if (signal != null) rects.Add(WorldBoundsPixelRect(signal.bounds, camera));
            return rects.ToArray();
        }

        private static Rect TraversalPathRect(Camera camera)
        {
            float floorY = camera.WorldToViewportPoint(new Vector3(0f, PrototypeCampUse.PlayerFloorY, 0f)).y * CaptureHeight;
            return Rect.MinMaxRect(0f, floorY - 34f, CaptureWidth, floorY + 54f);
        }

        private static string localizationTarget(KimSurvivalPrototype prototype, PrototypeLocalization localization, string locale, PrototypeInputDevice device, Vector2 position)
        {
            PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            if (interaction.IsPopupOpen) Invoke(prototype, "CancelCampPopup");
            localization.SetLocale(locale, false);
            SetDevice(prototype, device);
            campUse.Warp(position);
            Invoke(prototype, "RefreshAll");
            string prompt = GetField<TMP_Text>(prototype, "campProximityText").text;
            string marker = prompt.Contains("[X]") ? "[X]" : prompt.Contains("[E]") ? "[E]" : "<missing-action-marker>";
            return interaction.ActiveTargetKind + "|" + interaction.ActiveTargetKind + "|" + marker + "|" + Normalize(prompt);
        }

        private static string LayoutActual(LocaleLayoutEvidence layout)
        {
            return layout.locale + "/" + layout.device + " prompt=" + layout.promptRect +
                   " gap=" + layout.narrationGap.ToString("0.0") +
                   " sizePass=" + layout.withinSizeCap + " adjacent=" + layout.adjacentToNarration;
        }

        private static void SetDevice(KimSurvivalPrototype prototype, PrototypeInputDevice device)
        {
            object input = GetField<object>(prototype, "playerInput");
            FieldInfo trackerField = input.GetType().GetField("deviceTracker", InstanceFlags);
            if (trackerField == null) throw new MissingFieldException(input.GetType().FullName, "deviceTracker");
            object tracker = trackerField.GetValue(input);
            MethodInfo update = tracker.GetType().GetMethod("Update", InstanceFlags);
            if (update == null) throw new MissingMethodException(tracker.GetType().FullName, "Update");
            PrototypeInputActivity activity = device == PrototypeInputDevice.Gamepad
                ? new PrototypeInputActivity(false, true)
                : new PrototypeInputActivity(true, false);
            update.Invoke(tracker, new object[] { activity });
        }

        private static bool HasPromptColumns(string row, string marker)
        {
            if (string.IsNullOrEmpty(row)) return false;
            string[] columns = row.Split('\t');
            return columns.Length >= 3 && columns[1].Contains(marker) && columns[2].Contains(marker) &&
                   columns[1].Contains("{0}") && columns[2].Contains("{0}");
        }

        private static int CountTokens(string row)
        {
            return string.IsNullOrEmpty(row) ? 0 : row.Split('\t').Count(column => column.Contains("{0}"));
        }

        private static int ActivePromptCount(KimSurvivalPrototype prototype)
        {
            GameObject root = GetField<GameObject>(prototype, "campProximityPrompt");
            TMP_Text text = GetField<TMP_Text>(prototype, "campProximityText");
            return root != null && root.activeInHierarchy && text != null && !string.IsNullOrWhiteSpace(text.text) ? 1 : 0;
        }

        private static int ActivePopupCount(KimSurvivalPrototype prototype, PrototypeCampInteraction interaction)
        {
            GameObject root = GetField<GameObject>(prototype, "campInteractionPopup");
            return root != null && root.activeInHierarchy && interaction.IsPopupOpen ? 1 : 0;
        }

        private static void FinishPlayContracts()
        {
            bool infraPassed = SessionState.GetBool(PlayInfraPassedKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL · no Play result");
            File.WriteAllText(Path.Combine(EvidenceFolder, "prompt-layout-play-exit.txt"), message + Environment.NewLine, new UTF8Encoding(false));
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayInfraPassedKey);
            SessionState.EraseString(PlayMessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (Application.isBatchMode) EditorApplication.Exit(infraPassed ? 0 : 1);
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

        private static Report WriteReport(string prefix, string title, DateTime started, List<Check> checks)
        {
            Report report = new Report
            {
                title = title,
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                command = string.Join(" ", Environment.GetCommandLineArgs().Select(Quote)),
                passed = checks.Count(check => check.status == "PASS"),
                expectedFailed = checks.Count(check => check.status == "EXPECTED_FAIL"),
                productFailed = checks.Count(check => check.status == "FAIL"),
                infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"),
                checks = checks.ToArray()
            };
            report.productOverall = report.productFailed > 0 ? "FAIL" : report.expectedFailed > 0 ? "RED_EXPECTED_FAIL" : "PASS";
            report.infrastructureOverall = report.infrastructureFailed > 0 ? "FAIL" : "PASS";
            report.overall = report.infrastructureOverall == "FAIL" || report.productOverall == "FAIL"
                ? "FAIL"
                : report.productOverall == "RED_EXPECTED_FAIL" ? "RED" : "PASS";
            WriteJson(prefix + ".json", report);

            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + report.runId);
            text.AppendLine("Baseline: " + report.baselineCommit);
            text.AppendLine("Unity: " + report.unityVersion);
            text.AppendLine("Overall/Product/Infrastructure: " + report.overall + "/" + report.productOverall + "/" + report.infrastructureOverall);
            text.AppendLine("PASS/EXPECTED_FAIL/FAIL/INFRA_FAIL/UNVERIFIED: " + report.passed + "/" + report.expectedFailed + "/" + report.productFailed + "/" + report.infrastructureFailed + "/" + report.unverified);
            foreach (Check check in checks)
            {
                text.AppendLine(check.status + " · " + check.classification + " · " + check.severity + " · " + check.id + " · " + Normalize(check.actual));
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, prefix + ".txt"), text.ToString(), new UTF8Encoding(false));
            return report;
        }

        private static void Product(List<Check> checks, string id, string matrix, string severity, string expected, bool passed, string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, passed ? "PASS" : "FAIL", passed ? "NONE" : "PRODUCT_REGRESSION", severity, expected, actual, reproduction, files));
        }

        private static void Expected(List<Check> checks, string id, string matrix, string severity, string expected, bool passed, string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, passed ? "PASS" : "EXPECTED_FAIL", passed ? "NONE" : "PRODUCT_EXPECTED_GAP", severity, expected, actual, reproduction, files));
        }

        private static void Infrastructure(List<Check> checks, string id, string matrix, string severity, string expected, Func<string> action, string reproduction, string files)
        {
            try
            {
                checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, action(), reproduction, files));
            }
            catch (Exception exception)
            {
                checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "TEST_INFRASTRUCTURE", severity, expected,
                    exception.GetType().Name + ": " + exception.Message, reproduction, files));
            }
        }

        private static Check NewCheck(string id, string matrix, string status, string classification, string severity, string expected, string actual, string reproduction, string files)
        {
            return new Check
            {
                id = id,
                matrix = matrix,
                status = status,
                classification = classification,
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = files
            };
        }

        private static void WriteJson(string name, object value)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, name), JsonUtility.ToJson(value, true) + Environment.NewLine, new UTF8Encoding(false));
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, InstanceFlags);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return (T)field.GetValue(target);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            InvokeResult<object>(target, name, arguments);
        }

        private static T InvokeResult<T>(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethods(InstanceFlags)
                .FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            try
            {
                object result = method.Invoke(target, arguments);
                return result == null ? default(T) : (T)result;
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static string Join(IEnumerable<string> values)
        {
            string[] materialized = (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return materialized.Length == 0 ? "<none>" : string.Join(" | ", materialized);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("\t", " ↹ ").Trim();
        }

        private static string Sanitize(string value)
        {
            return string.Concat(value.Select(character => char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '_'));
        }

        private static string Quote(string value)
        {
            return value.IndexOf(' ') >= 0 ? "\"" + value.Replace("\"", "\\\"") + "\"" : value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
