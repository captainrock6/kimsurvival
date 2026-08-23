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
        public const string CampBackgroundPath = "Assets/_Project/Art/Generated/background/job_20260822130341_c082e4b6/background_opaque.png";
        public const string CampGameplayGroundPath = "Assets/_Project/Art/Generated/background/job_20260822130341_c082e4b6/gameplay_ground_alpha.png";
        public const string CampForegroundPath = "Assets/_Project/Art/Generated/background/job_20260822130341_c082e4b6/foreground_alpha.png";
        public const string CampfirePath = "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/campfire.png";
        public const string WorkbenchPath = "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/workbench.png";
        public const string RainCollectorPath = "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/rain_collector.png";
        public const string RescueSignalPath = "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/rescue_signal.png";
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
            KimSurvivalPrototype prototype = root.AddComponent<KimSurvivalPrototype>();
            prototype.ConfigureCampBackgroundLayers(
                LoadRequiredSprite(CampBackgroundPath),
                LoadRequiredSprite(CampGameplayGroundPath),
                LoadRequiredSprite(CampForegroundPath));
            prototype.ConfigureCampStructureArt(
                LoadRequiredSprite(CampfirePath),
                LoadRequiredSprite(WorkbenchPath),
                LoadRequiredSprite(RainCollectorPath),
                LoadRequiredSprite(RescueSignalPath));
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
                "Adopted camp background layers: job_20260822130341_c082e4b6\n" +
                "Layer order: background_opaque -> gameplay_ground_alpha -> foreground_alpha\n" +
                "Canvas contract: 1672x941, walkable baseline top Y=721, signal anchor top Y=596\n" +
                "Adopted camp structures: job_20260822130400_6d786a69\n" +
                "Remaining placeholder asset IDs stay wired in KimSurvivalPrototype.cs\n");
            Debug.Log("[Kim Survival] Prototype scene created: " + ScenePath);
        }

        [MenuItem("Kim Survival/Run Edit Checks")]
        public static void RunEditChecks()
        {
            Directory.CreateDirectory(VerificationFolder);
            DateTime started = DateTime.UtcNow;
            PrototypeLocalizationAssetBuilder.SyncAssets();

            AssertCampBackgroundLayerImport(CampBackgroundPath, "background");
            AssertCampBackgroundLayerImport(CampGameplayGroundPath, "gameplay ground");
            AssertCampBackgroundLayerImport(CampForegroundPath, "foreground");
            AssertStructureSpriteImport(CampfirePath, new Vector2(0.5f, 0.07494f));
            AssertStructureSpriteImport(WorkbenchPath, new Vector2(0.5f, 0.09846f));
            AssertStructureSpriteImport(RainCollectorPath, new Vector2(0.5f, 0.05112f));
            AssertStructureSpriteImport(RescueSignalPath, new Vector2(0.5f, 0.0401f));
            string sceneText = File.ReadAllText(ScenePath);
            Assert(sceneText.Contains(AssetDatabase.AssetPathToGUID(CampBackgroundPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(CampGameplayGroundPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(CampForegroundPath)), "Prototype scene serializes all three adopted camp background layers");
            Assert(sceneText.Contains(AssetDatabase.AssetPathToGUID(CampfirePath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(WorkbenchPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(RainCollectorPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(RescueSignalPath)), "Prototype scene serializes all four adopted camp structure sprites");

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

            GameSession balance = new GameSession();
            Assert(balance.GetStorage(ResourceKind.Food) == 0 && Mathf.Approximately(balance.Hunger, 70f), "Balance v0.2 starts with food 0 and hunger 70");
            Assert(balance.BeginSearch() && balance.ReturnToCamp(false) && balance.EndDay(), "Balance v0.2 first day settles");
            Assert(Mathf.Approximately(balance.Hunger, 35f), "Balance v0.2 settlement drains 35 hunger");
            Assert(balance.BeginSearch() && balance.ReturnToCamp(false) && balance.EndDay(), "Balance v0.2 second day settles");
            Assert(Mathf.Approximately(balance.Hunger, 0f), "Balance v0.2 hunger reaches zero after two uneaten settlements");

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

            PrototypeSystemActions keyboardSystemActions = PrototypeSystemActions.FromRaw(new PrototypeRawSystemInput { KeyboardLanguage = true });
            PrototypeSystemActions gamepadSystemActions = PrototypeSystemActions.FromRaw(new PrototypeRawSystemInput { GamepadLanguage = true });
            Assert(keyboardSystemActions.LanguagePressed && gamepadSystemActions.LanguagePressed, "Keyboard and gamepad share the language action");

            PrototypeInputDeviceTracker deviceTracker = new PrototypeInputDeviceTracker();
            deviceTracker.Update(new PrototypeInputActivity(false, true));
            Assert(deviceTracker.ActiveDevice == PrototypeInputDevice.Gamepad, "Gamepad activity switches the active input prompt");
            deviceTracker.Update(new PrototypeInputActivity(true, false));
            Assert(deviceTracker.ActiveDevice == PrototypeInputDevice.KeyboardMouse, "Keyboard or mouse activity restores the active input prompt");
            Assert(PrototypeInputPromptKeys.Placement(PrototypeInputDevice.KeyboardMouse) == "controls.placement.keyboard_mouse" &&
                   PrototypeInputPromptKeys.Placement(PrototypeInputDevice.Gamepad) == "controls.placement.gamepad", "Placement prompt selection follows the active device");

            bool hadLocalePreference = PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey);
            string originalLocalePreference = PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode);
            PrototypeLocalization localization = new PrototypeLocalization();
            string originalLocale = localization.CurrentLocaleCode;
            try
            {
                Assert(localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false), "English locale is selectable");
                Assert(localization.Format("ui.camp.title") == "Base Camp · Craft / Build / Research", "English String Table is active immediately");
                Assert(localization.Format("hud.status.camp", 1, 3, "Camp", 70, 100).Contains("Hunger 70"), "Smart String arguments format in English");
                Assert(localization.Format("controls.placement.gamepad", localization.DeviceName(PrototypeInputDevice.Gamepad)).Contains("left stick"), "English gamepad placement prompt is localized");
                Assert(localization.Format("world.barrier.axe.need").Contains("Stone Axe Required"), "English forest barrier names the stone axe requirement");
                Assert(localization.Format("dev.fallback_probe") == "한국어 폴백 확인", "Missing English translation falls back to Korean");
                Assert(localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false), "Korean locale is selectable");
                Assert(localization.Format("ui.camp.title") == "베이스캠프 · 제작 / 건설 / 연구", "Korean source string restores immediately");
                Assert(localization.Format("controls.placement.keyboard_mouse", localization.DeviceName(PrototypeInputDevice.KeyboardMouse)).Contains("마우스로 위치 이동"), "Korean keyboard and mouse placement prompt is localized");
                Assert(localization.Format("world.barrier.axe.need").Contains("돌도끼 필요"), "Korean forest barrier names the stone axe requirement");
                Assert(localization.ResolveStartupLocale("es") == PrototypeLocalization.KoreanLocaleCode, "Unsupported saved locale resolves to Korean");
                Assert(localization.SetLocale(PrototypeLocalization.EnglishLocaleCode), "Locale preference can be persisted");
                Assert(PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey) == PrototypeLocalization.EnglishLocaleCode, "Persisted locale is available to the next launch");

                PrototypeLocaleFontProfile fontProfile = Resources.Load<PrototypeLocaleFontProfile>("PrototypeLocaleFontProfile");
                Assert(fontProfile != null && fontProfile.Find("ko") != null && fontProfile.Find("en") != null, "Locale-specific TMP primary and fallback mappings are data assets");
                Assert(fontProfile != null && Mathf.Approximately(fontProfile.Find("ko").WorldTextScale, 1f) && fontProfile.Find("en").WorldTextScale > 1f,
                    "Locale-specific world typography scale is data-driven");
            }
            finally
            {
                localization.SetLocale(originalLocale, false);
                localization.Dispose();
                if (hadLocalePreference)
                {
                    PlayerPrefs.SetString(PrototypeLocalization.PreferenceKey, originalLocalePreference);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PrototypeLocalization.PreferenceKey);
                }
                PlayerPrefs.Save();
            }

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

            GameSession ropeOnlyTraversalSession = new GameSession();
            ropeOnlyTraversalSession.Grant(ResourceKind.Wood, 10);
            ropeOnlyTraversalSession.Grant(ResourceKind.Salvage, 10);
            Assert(ropeOnlyTraversalSession.TryBuild(StructureKind.Workbench), "Rope-only barrier scenario builds workbench");
            Assert(ropeOnlyTraversalSession.TryResearch(TechKind.Rope) && ropeOnlyTraversalSession.TryCraft(TechKind.Rope), "Rope-only barrier scenario crafts rope");
            Assert(ropeOnlyTraversalSession.HasRope && !ropeOnlyTraversalSession.HasAxe && ropeOnlyTraversalSession.BeginSearch(), "Rope-only barrier scenario starts without axe");
            PrototypePlayerTraversal ropeOnlyTraversal = new PrototypePlayerTraversal();
            ropeOnlyTraversal.Reset(7.7f, PrototypePlayerTraversal.LandY);
            PrototypeTraversalStep ropeBlocked = ropeOnlyTraversal.Step(new PrototypePlayerActions(1f, false, false, false, false, -1), 0.2f, 0f, ropeOnlyTraversalSession);
            Assert(ropeBlocked.ReachedBlockedPath && ropeOnlyTraversal.X <= 8f, "Rope alone cannot cross the vine and wood barrier");

            GameSession axeOnlyTraversalSession = new GameSession();
            axeOnlyTraversalSession.Grant(ResourceKind.Wood, 10);
            axeOnlyTraversalSession.Grant(ResourceKind.Stone, 10);
            axeOnlyTraversalSession.Grant(ResourceKind.Salvage, 10);
            Assert(axeOnlyTraversalSession.TryBuild(StructureKind.Workbench), "Axe-only barrier scenario builds workbench");
            Assert(axeOnlyTraversalSession.TryResearch(TechKind.StoneAxe) && axeOnlyTraversalSession.TryCraft(TechKind.StoneAxe), "Axe-only barrier scenario crafts stone axe");
            Assert(axeOnlyTraversalSession.HasAxe && !axeOnlyTraversalSession.HasRope && axeOnlyTraversalSession.BeginSearch(), "Axe-only barrier scenario starts without rope");
            PrototypePlayerTraversal axeOnlyTraversal = new PrototypePlayerTraversal();
            axeOnlyTraversal.Reset(7.7f, PrototypePlayerTraversal.LandY);
            PrototypeTraversalStep axePasses = axeOnlyTraversal.Step(new PrototypePlayerActions(1f, false, false, false, false, -1), 0.2f, 0f, axeOnlyTraversalSession);
            Assert(!axePasses.ReachedBlockedPath && axeOnlyTraversal.X > 8f, "Stone axe alone crosses the vine and wood barrier");
            Assert(axeOnlyTraversalSession.TryGather(ResourceKind.Wood, 1) == GatherResult.Added && axeOnlyTraversalSession.GetBagSlot(0).Amount == 2, "Stone axe keeps the wood gathering plus-one bonus");

            GameSession signalFeedback = new GameSession();
            signalFeedback.Grant(ResourceKind.Wood, 10);
            signalFeedback.Grant(ResourceKind.Salvage, 10);
            Assert((signalFeedback.GetSignalUpgradeBlockers() & SignalUpgradeBlockers.MissingWorkbench) != 0 && !signalFeedback.TryUpgradeSignal() && signalFeedback.LastMessage.Key == "message.signal.workbench", "Signal stage one reports a missing workbench");
            Assert(signalFeedback.TryBuild(StructureKind.Workbench), "Signal feedback scenario builds workbench");
            Assert(!signalFeedback.HasRope && signalFeedback.TryUpgradeSignal() && signalFeedback.SignalStage == 1, "Signal stage one succeeds without rope");
            Assert((signalFeedback.GetSignalUpgradeBlockers() & SignalUpgradeBlockers.MissingRope) != 0 && !signalFeedback.TryUpgradeSignal() && signalFeedback.LastMessage.Key == "message.signal.rope", "Signal stage two clearly rejects missing rope");
            Assert(signalFeedback.TryResearch(TechKind.Rope) && signalFeedback.TryCraft(TechKind.Rope), "Signal feedback scenario crafts rope");
            Assert(signalFeedback.TryUpgradeSignal() && signalFeedback.Result == RunResult.Rescued, "Signal stage two succeeds with rope and materials");

            GameSession signalWoodShortage = new GameSession();
            signalWoodShortage.Grant(ResourceKind.Salvage, 3);
            Assert(signalWoodShortage.TryBuild(StructureKind.Workbench) && !signalWoodShortage.TryUpgradeSignal() && signalWoodShortage.LastMessage.Key == "message.signal.wood", "Signal feedback distinguishes a wood shortage");
            GameSession signalSalvageShortage = new GameSession();
            signalSalvageShortage.Grant(ResourceKind.Wood, 2);
            signalSalvageShortage.Grant(ResourceKind.Salvage, 1);
            Assert(signalSalvageShortage.TryBuild(StructureKind.Workbench) && !signalSalvageShortage.TryUpgradeSignal() && signalSalvageShortage.LastMessage.Key == "message.signal.salvage", "Signal feedback distinguishes a salvage shortage");

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
                "Checks: balance v0.2 food/hunger/settlement, signal stage-one workbench and stage-two rope/material blockers with selectable feedback, axe-only forest barrier and wood plus-one, adopted 1672x941 three-layer camp background and four camp structure sprite imports, layer/source metadata, structure pivots, serialized scene references, inventory overflow/swap, shared keyboard/gamepad actions including language, deterministic active-device prompt switching, ko/en Unity String Tables and placement prompts, Smart Strings, Korean fallback logging, locale persistence, TMP locale font mappings, limited free placement input/state, grid snap, camp bounds, entrance/path protection, structure overlap, free repositioning, shore transitions, swimming jump suppression, swimming costs, water gathering, camp structures, research, crafting, rescue success, deadline failure\n";
            File.WriteAllText(Path.Combine(VerificationFolder, "editmode-checks.txt"), report);
            Debug.Log("[Kim Survival] " + report.Replace('\n', ' '));
        }

        [MenuItem("Kim Survival/Build Windows Prototype")]
        public static void BuildWindows()
        {
            PrototypeLocalizationAssetBuilder.SyncAssets();
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

        private static Sprite LoadRequiredSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException("Adopted camp structure could not be loaded: " + path);
            }
            return sprite;
        }

        private static void AssertCampBackgroundLayerImport(string path, string layerName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert(sprite != null, "Adopted camp " + layerName + " imports as a Unity sprite: " + path);
            Assert(importer != null && importer.textureType == TextureImporterType.Sprite,
                "Adopted camp " + layerName + " keeps sprite import settings: " + path);
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Assert(sourceWidth == 1672 && sourceHeight == 941,
                "Adopted camp " + layerName + " preserves the 1672x941 source canvas (source " +
                sourceWidth + "x" + sourceHeight + "): " + path);
            Assert(sprite != null && Mathf.Abs(sprite.rect.width / sprite.rect.height - 1672f / 941f) < 0.001f,
                "Adopted camp " + layerName + " keeps the shared canvas aspect after Forge import scaling: " + path);
        }

        private static void AssertStructureSpriteImport(string path, Vector2 expectedPivot)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert(sprite != null, "Adopted camp structure imports as a Unity sprite: " + path);
            TextureImporterSettings settings = new TextureImporterSettings();
            if (importer != null)
            {
                importer.ReadTextureSettings(settings);
            }
            Assert(importer != null && importer.textureType == TextureImporterType.Sprite && settings.spriteAlignment == (int)SpriteAlignment.Custom,
                "Adopted camp structure keeps custom sprite import settings: " + path);
            Assert(importer != null && Vector2.Distance(settings.spritePivot, expectedPivot) < 0.0001f,
                "Adopted camp structure keeps metadata bottom pivot: " + path);
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
                string explorationScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave6-exploration-ko-1280x800.png"));
                string swimmingScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave6-swimming-en-1280x800.png"));
                string placementKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave6-placement-ko-invalid-1280x800.png"));
                string placementEnglishScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave6-placement-en-valid-gamepad-1280x800.png"));
                string signalKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave6-signal-stage1-missing-ko-1280x800.png"));
                string signalEnglishScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave6-signal-stage2-missing-en-1280x800.png"));
                string result = prototype.RunAutomatedVerification(explorationScreenshot, swimmingScreenshot, placementKoreanScreenshot, placementEnglishScreenshot, signalKoreanScreenshot, signalEnglishScreenshot);
                string screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave6-camp-en-1280x800.png"));
                prototype.CaptureVerificationPng(screenshot, 1280, 800);
                SessionState.SetBool(PassedKey, true);
                SessionState.SetString(MessageKey, result + "\nSignal stage one missing/workbench Korean screenshot: " + signalKoreanScreenshot + "\nSignal stage two missing/rope English screenshot: " + signalEnglishScreenshot + "\nPlacement Korean screenshot: " + placementKoreanScreenshot + "\nPlacement English/gamepad screenshot: " + placementEnglishScreenshot + "\nSwimming screenshot: " + swimmingScreenshot + "\nExploration screenshot: " + explorationScreenshot + "\nCamp screenshot: " + screenshot);
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
