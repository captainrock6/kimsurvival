using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public const string VineBarrierBlockedPath = "Assets/_Project/Art/Generated/separated_parts/job_20260822234631_ac651d92/blocked.png";
        public const string VineBarrierInteractablePath = "Assets/_Project/Art/Generated/separated_parts/job_20260822234631_ac651d92/interactable.png";
        public const string VineBarrierClearedPath = "Assets/_Project/Art/Generated/separated_parts/job_20260822234631_ac651d92/cleared.png";
        public const string CompactPromptFramePath = "Assets/_Project/Art/Generated/ui_set/job_20260823073121_f5da3402/compact-a.png";
        public const string CompactPromptSkinPath = "Assets/_Project/Scripts/Localization/Resources/Wave12CompactPromptSkin.asset";
        public const string CompactPromptAssetId = "ui.camp-contextual-interaction.compact-a";
        public const string ExpeditionMapLayoutPath = "Assets/_Project/Art/Generated/ui_set/job_20260823150636_e3b39abc/candidate-a-right-rail-1280x800.png";
        public const string ExpeditionMapAssetId = "ui.expedition-map.right-rail-a";
        public const string KimAtlasPath = "Assets/_Project/Art/Generated/sprite_animation/job_20260822085926_374033c5/exec-7c1f46d8-3b4f-4350-abc3-de6be9ebab6d.png";
        public const string WoodIconPath = "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/wood.png";
        public const string StoneIconPath = "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/stone.png";
        public const string FoodIconPath = "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/food.png";
        public const string SalvageIconPath = "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/scrap.png";
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
            SyncCompactPromptSkin();
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
            prototype.ConfigureExplorationArt(
                LoadRequiredSprite(VineBarrierBlockedPath),
                LoadRequiredSprite(VineBarrierInteractablePath),
                LoadRequiredSprite(VineBarrierClearedPath));
            prototype.ConfigureExpeditionMapArt(LoadRequiredSprite(ExpeditionMapLayoutPath));
            prototype.ConfigureCharacterAndItemArt(
                LoadRequiredSprite(KimAtlasPath),
                LoadRequiredSprite(WoodIconPath),
                LoadRequiredSprite(StoneIconPath),
                LoadRequiredSprite(FoodIconPath),
                LoadRequiredSprite(SalvageIconPath));
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
                "Adopted Mr. Kim atlas: job_20260822085926_374033c5\n" +
                "Adopted resource icons: job_20260822141317_caf8e11d\n" +
                "Adopted vine barrier states: job_20260822234631_ac651d92\n" +
                "Remaining placeholder asset IDs stay wired in KimSurvivalPrototype.cs\n");
            Debug.Log("[Kim Survival] Prototype scene created: " + ScenePath);
        }

        [MenuItem("Kim Survival/Run Edit Checks")]
        public static void RunEditChecks()
        {
            Directory.CreateDirectory(VerificationFolder);
            DateTime started = DateTime.UtcNow;
            PrototypeLocalizationAssetBuilder.SyncAssets();
            SyncCompactPromptSkin();

            AssertCampBackgroundLayerImport(CampBackgroundPath, "background");
            AssertCampBackgroundLayerImport(CampGameplayGroundPath, "gameplay ground");
            AssertCampBackgroundLayerImport(CampForegroundPath, "foreground");
            AssertStructureSpriteImport(CampfirePath, new Vector2(0.5f, 0.07494f));
            AssertStructureSpriteImport(WorkbenchPath, new Vector2(0.5f, 0.09846f));
            AssertStructureSpriteImport(RainCollectorPath, new Vector2(0.5f, 0.05112f));
            AssertStructureSpriteImport(RescueSignalPath, new Vector2(0.5f, 0.0401f));
            AssertStructureSpriteImport(VineBarrierBlockedPath, new Vector2(0.5f, 0.078125f));
            AssertStructureSpriteImport(VineBarrierInteractablePath, new Vector2(0.5f, 0.078125f));
            AssertStructureSpriteImport(VineBarrierClearedPath, new Vector2(0.5f, 0.078125f));
            AssertCompactPromptImport();
            AssertExpeditionMapImport();
            string sceneText = File.ReadAllText(ScenePath);
            Assert(sceneText.Contains(AssetDatabase.AssetPathToGUID(CampBackgroundPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(CampGameplayGroundPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(CampForegroundPath)), "Prototype scene serializes all three adopted camp background layers");
            Assert(sceneText.Contains(AssetDatabase.AssetPathToGUID(CampfirePath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(WorkbenchPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(RainCollectorPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(RescueSignalPath)), "Prototype scene serializes all four adopted camp structure sprites");
            Assert(sceneText.Contains(AssetDatabase.AssetPathToGUID(VineBarrierBlockedPath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(VineBarrierInteractablePath)) &&
                   sceneText.Contains(AssetDatabase.AssetPathToGUID(VineBarrierClearedPath)), "Prototype scene serializes all three adopted vine barrier states");
            Assert(sceneText.Contains(AssetDatabase.AssetPathToGUID(ExpeditionMapLayoutPath)),
                "Prototype scene serializes the selected-only expedition map A layout");

            Assert(Type.GetType("KimSurvival.PrototypeCampInteraction, Assembly-CSharp") != null,
                "Wave 9 contextual camp interaction state machine is present");

            GameSession inventory = new GameSession();
            Assert(GameSession.DefaultBagSlotCount == 4 && GameSession.MaximumBagSlotCount == 6 && GameSession.StackLimit == 2,
                "Bag contract keeps four default slots, six physical slots, and two items per stack");
            Assert(inventory.ActiveBagSlotCount == 4 && !inventory.IsBagSlotActive(4) && !inventory.IsBagSlotActive(5),
                "New games expose four slots and keep physical slots five and six locked");
            Assert(inventory.BeginSearch(), "Inventory scenario begins search");
            Assert(inventory.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "Wood fills slot");
            Assert(inventory.TryGather(ResourceKind.Stone, 2) == GatherResult.Added, "Stone fills slot");
            Assert(inventory.TryGather(ResourceKind.Food, 2) == GatherResult.Added, "Food fills slot");
            Assert(inventory.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "Salvage fills slot");
            Assert(inventory.TryGather(ResourceKind.Wood, 1) == GatherResult.PendingSwap, "Full bag creates a real swap choice");
            Assert(inventory.HasPendingLoot, "Pending loot is recorded");
            Assert(!inventory.ReplaceBagSlot(4) && inventory.HasPendingLoot, "Locked slot five cannot receive pending loot before the upgrade");
            Assert(inventory.ReplaceBagSlot(1), "Player can replace a selected slot");
            Assert(inventory.ReturnToCamp(false), "Bag transfers on return");
            Assert(inventory.GetStorage(ResourceKind.Wood) >= 3, "Returned wood reaches storage");

            GameSession bagUpgrade = new GameSession();
            bagUpgrade.Grant(ResourceKind.Wood, 4);
            bagUpgrade.Grant(ResourceKind.Salvage, 2);
            int preWorkbenchWood = bagUpgrade.GetStorage(ResourceKind.Wood);
            int preWorkbenchSalvage = bagUpgrade.GetStorage(ResourceKind.Salvage);
            Assert(!bagUpgrade.TryUpgradeBagCapacity() && bagUpgrade.LastMessage.Key == "message.bag_upgrade.workbench",
                "Bag upgrade clearly rejects a missing workbench");
            Assert(bagUpgrade.GetStorage(ResourceKind.Wood) == preWorkbenchWood && bagUpgrade.GetStorage(ResourceKind.Salvage) == preWorkbenchSalvage,
                "Missing-workbench failure spends no resources");
            Assert(bagUpgrade.TryBuild(StructureKind.Workbench), "Bag upgrade scenario builds the required workbench");
            int preUpgradeWood = bagUpgrade.GetStorage(ResourceKind.Wood);
            int preUpgradeSalvage = bagUpgrade.GetStorage(ResourceKind.Salvage);
            Assert(bagUpgrade.CanUpgradeBagCapacity() && bagUpgrade.CanUpgradeBagCapacity(), "Repeated upgrade previews remain side-effect free");
            Assert(bagUpgrade.GetStorage(ResourceKind.Wood) == preUpgradeWood && bagUpgrade.GetStorage(ResourceKind.Salvage) == preUpgradeSalvage,
                "Preview and cancellation path spend no resources");
            Assert(bagUpgrade.TryUpgradeBagCapacity() && bagUpgrade.ActiveBagSlotCount == 6,
                "Workbench upgrades the bag from four to six slots once");
            Assert(bagUpgrade.GetStorage(ResourceKind.Wood) == preUpgradeWood - GameSession.BagUpgradeWoodCost &&
                   bagUpgrade.GetStorage(ResourceKind.Salvage) == preUpgradeSalvage - GameSession.BagUpgradeSalvageCost,
                "Successful bag upgrade atomically spends exactly wood two and salvage one");
            int postUpgradeWood = bagUpgrade.GetStorage(ResourceKind.Wood);
            int postUpgradeSalvage = bagUpgrade.GetStorage(ResourceKind.Salvage);
            Assert(!bagUpgrade.TryUpgradeBagCapacity() && bagUpgrade.LastMessage.Key == "message.bag_upgrade.complete",
                "Repeated bag upgrade input is rejected as complete");
            Assert(bagUpgrade.GetStorage(ResourceKind.Wood) == postUpgradeWood && bagUpgrade.GetStorage(ResourceKind.Salvage) == postUpgradeSalvage,
                "Repeated upgrade input spends no resources");

            Assert(bagUpgrade.BeginSearch(), "Six-slot inventory scenario begins search");
            Assert(bagUpgrade.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                   bagUpgrade.TryGather(ResourceKind.Stone, 2) == GatherResult.Added &&
                   bagUpgrade.TryGather(ResourceKind.Food, 2) == GatherResult.Added &&
                   bagUpgrade.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added &&
                   bagUpgrade.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                   bagUpgrade.TryGather(ResourceKind.Stone, 1) == GatherResult.Added &&
                   bagUpgrade.TryGather(ResourceKind.Stone, 1) == GatherResult.Added,
                "Upgraded search acquires and stacks resources through slots five and six");
            Assert(bagUpgrade.GetBagSlot(4).Kind == ResourceKind.Wood && bagUpgrade.GetBagSlot(4).Amount == 2 &&
                   bagUpgrade.GetBagSlot(5).Kind == ResourceKind.Stone && bagUpgrade.GetBagSlot(5).Amount == 2,
                "Physical slots five and six participate in acquisition and stacking");
            Assert(bagUpgrade.TryGather(ResourceKind.Food, 1) == GatherResult.PendingSwap && bagUpgrade.ReplaceBagSlot(5),
                "Pending loot can replace slot six");
            Assert(bagUpgrade.TryGather(ResourceKind.Stone, 1) == GatherResult.PendingSwap && bagUpgrade.ReplaceBagSlot(4),
                "Pending loot can replace slot five");
            Assert(bagUpgrade.TryGather(ResourceKind.Salvage, 1) == GatherResult.PendingSwap, "Full six-slot bag still creates a pending choice");
            bagUpgrade.DiscardPendingLoot();
            Assert(!bagUpgrade.HasPendingLoot, "Six-slot pending loot can be explicitly discarded");
            int storageBeforeSixSlotReturn = bagUpgrade.GetStorage(ResourceKind.Food);
            Assert(bagUpgrade.ReturnToCamp(false) && bagUpgrade.GetStorage(ResourceKind.Food) > storageBeforeSixSlotReturn,
                "All active slots, including slot six, transfer on return");
            Assert(bagUpgrade.EndDay() && bagUpgrade.ActiveBagSlotCount == 6, "Bag capacity persists across day settlement");
            bagUpgrade.Reset();
            Assert(bagUpgrade.ActiveBagSlotCount == 4 && !bagUpgrade.HasBagCapacityUpgrade && !bagUpgrade.HasPendingLoot &&
                   !bagUpgrade.IsBagSlotActive(4) && bagUpgrade.GetBagSlot(4).IsEmpty,
                "New-game reset restores four slots and clears locked physical storage and pending loot");

            GameSession bagShortage = new GameSession();
            bagShortage.Grant(ResourceKind.Salvage, 1);
            Assert(bagShortage.TryBuild(StructureKind.Workbench), "Bag shortage scenario builds the workbench with exact materials");
            int shortageWood = bagShortage.GetStorage(ResourceKind.Wood);
            int shortageSalvage = bagShortage.GetStorage(ResourceKind.Salvage);
            Assert(!bagShortage.TryUpgradeBagCapacity() && bagShortage.LastMessage.Key == "message.bag_upgrade.materials",
                "Bag upgrade distinguishes combined material shortages");
            Assert(bagShortage.GetStorage(ResourceKind.Wood) == shortageWood && bagShortage.GetStorage(ResourceKind.Salvage) == shortageSalvage,
                "Material failure spends nothing");
            bagShortage.Grant(ResourceKind.Wood, 2);
            Assert(!bagShortage.TryUpgradeBagCapacity() && bagShortage.LastMessage.Key == "message.bag_upgrade.salvage",
                "Bag upgrade distinguishes a salvage shortage");
            Assert(bagShortage.GetStorage(ResourceKind.Wood) == 2 && bagShortage.GetStorage(ResourceKind.Salvage) == 0,
                "Single-material failure remains atomic");

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
            PrototypePlayerActions sixthSlotActions = PrototypePlayerActions.FromRaw(new PrototypeRawInput { BagSlotIndex = 5 });
            Assert(sixthSlotActions.BagSlotIndex == 5, "Slot six maps into the same keyboard/gamepad action snapshot");

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
            Assert(PrototypeInputPromptKeys.Explore(PrototypeInputDevice.KeyboardMouse) == "controls.explore.keyboard_mouse" &&
                   PrototypeInputPromptKeys.Explore(PrototypeInputDevice.Gamepad) == "controls.explore.gamepad", "Exploration and six-slot replacement prompts follow the active device");
            Assert(PrototypeInputPromptKeys.CampProximity(PrototypeInputDevice.KeyboardMouse) == "camp.interaction.prompt.keyboard_mouse" &&
                   PrototypeInputPromptKeys.CampProximity(PrototypeInputDevice.Gamepad) == "camp.interaction.prompt.gamepad" &&
                   PrototypeInputPromptKeys.CampPopup(PrototypeInputDevice.KeyboardMouse) == "controls.camp.popup.keyboard_mouse" &&
                   PrototypeInputPromptKeys.CampPopup(PrototypeInputDevice.Gamepad) == "controls.camp.popup.gamepad",
                "Keyboard/mouse and gamepad converge on the same contextual prompt and popup state machine");

            PrototypeCampModulePreviewActions keyboardModuleActions = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                KeyboardNext = true,
                KeyboardConfirm = true,
                KeyboardCancel = true
            });
            PrototypeCampModulePreviewActions gamepadModuleActions = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                HorizontalAxis = 1f,
                GamepadConfirm = true,
                GamepadCancel = true
            });
            Assert(keyboardModuleActions.CycleDirection == gamepadModuleActions.CycleDirection &&
                   keyboardModuleActions.ConfirmPressed && gamepadModuleActions.ConfirmPressed &&
                   keyboardModuleActions.CancelPressed && gamepadModuleActions.CancelPressed,
                "Keyboard/mouse and gamepad converge on one module-preview action snapshot");
            Assert(PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.KeyboardMouse) == "controls.module_preview.keyboard_mouse" &&
                   PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.Gamepad) == "controls.module_preview.gamepad",
                "Module-preview prompts follow the active device without changing the state machine");
            Assert(PrototypeInputPromptKeys.InteractGlyph(PrototypeInputDevice.KeyboardMouse) == "input.glyph.interact.keyboard_mouse" &&
                   PrototypeInputPromptKeys.InteractGlyph(PrototypeInputDevice.Gamepad) == "input.glyph.interact.gamepad",
                "Keyboard and gamepad interaction glyphs feed the same localized action pattern");

            PrototypeExpeditionMapActions keyboardMapActions = PrototypeExpeditionMapActions.FromRaw(new PrototypeRawExpeditionMapInput
            {
                KeyboardNext = true,
                KeyboardConfirm = true,
                KeyboardCancel = true
            });
            PrototypeExpeditionMapActions gamepadMapActions = PrototypeExpeditionMapActions.FromRaw(new PrototypeRawExpeditionMapInput
            {
                HorizontalAxis = 1f,
                GamepadConfirm = true,
                GamepadCancel = true
            });
            Assert(keyboardMapActions.CycleDirection == gamepadMapActions.CycleDirection &&
                   keyboardMapActions.ConfirmPressed && gamepadMapActions.ConfirmPressed &&
                   keyboardMapActions.CancelPressed && gamepadMapActions.CancelPressed,
                "Keyboard/mouse and gamepad converge on one expedition-map action snapshot");
            Assert(PrototypeInputPromptKeys.ExpeditionMap(PrototypeInputDevice.KeyboardMouse) == "controls.expedition_map.keyboard_mouse" &&
                   PrototypeInputPromptKeys.ExpeditionMap(PrototypeInputDevice.Gamepad) == "controls.expedition_map.gamepad",
                "Expedition-map prompts follow the active device without changing region focus");

            bool hadLocalePreference = PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey);
            string originalLocalePreference = PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode);
            PrototypeLocalization localization = new PrototypeLocalization();
            string originalLocale = localization.CurrentLocaleCode;
            try
            {
                Assert(localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false), "English locale is selectable");
                Assert(localization.Format("ui.camp.title") == "Base Camp · Craft / Build / Research", "English String Table is active immediately");
                Assert(localization.Format("hud.status.camp", 1, GameSession.FinalDay, "Camp", 70, 100).Contains("DAY 1/50") &&
                       localization.Format("hud.status.camp", 1, GameSession.FinalDay, "Camp", 70, 100).Contains("Hunger 70"),
                    "Fifty-day HUD Smart String arguments format in English");
                Assert(localization.Format("expedition.map.title", 1, GameSession.FinalDay).Contains("1/50") &&
                       localization.Format("expedition.map.detail", "Beach", "Summary", "Wood", 20, "Low", "Clear", "None", "Smoke").Contains("Travel time: about 20 min") &&
                       localization.Format("expedition.region.beach.resources").IndexOfAny("0123456789".ToCharArray()) < 0,
                    "Expedition map uses localized arguments while resource forecasts hide exact loot quantities");
                Assert(localization.Format("controls.placement.gamepad", localization.DeviceName(PrototypeInputDevice.Gamepad)).Contains("left stick"), "English gamepad placement prompt is localized");
                Assert(localization.Format("camp.interaction.prompt.gamepad", localization.Format("structure.workbench")).Contains("[X] Use Workbench"), "English gamepad proximity prompt is localized");
                Assert(localization.Format("camp.popup.detail.workbench").Contains("Craft, research, repair"), "English workbench popup owns the intended actions");
                Assert(localization.Format("controls.module_preview.gamepad", localization.DeviceName(PrototypeInputDevice.Gamepad)).Contains("Left stick cycle") &&
                       localization.Format("module.economy.short", 4, 2, 2).Contains("Wood 4") &&
                       localization.Format("world.module.preview.invalid", "Basement", "Path blocked", "Resources short").Contains("× Basement"),
                    "English module preview, invalid shape marker, and module cost arguments are localized");
                Assert(localization.Format(
                           "interaction.structure.prompt",
                           string.Empty,
                           localization.Format("structure.module_connector", localization.Format("module.name.side")),
                           localization.Format("interaction.action.preview")).Trim().Contains("Preview Side room entrance connector") &&
                       localization.Format("interaction.structure.prompt", string.Empty, localization.Format("structure.workbench"), localization.Format("interaction.action.use")).Trim().Contains("Use Workbench") &&
                       localization.Format(PrototypeInputPromptKeys.InteractGlyph(PrototypeInputDevice.Gamepad)) == "[X]" &&
                       localization.Format("ui.module.expand") == "Preview Room Expansion" &&
                       localization.Format("ui.module.preview.cost", "Side room", 2, 1, localization.Format("interaction.module.locked_workbench")).Contains("Wood 2 · Salvage 1"),
                    "English direct module slot prompt, popup action, and W2/D1 reason chip use stable String Table keys");
                Assert(localization.Format("world.barrier.axe.need").Contains("Stone Axe Required"), "English forest barrier names the stone axe requirement");
                Assert(localization.Format("dev.fallback_probe") == "한국어 폴백 확인", "Missing English translation falls back to Korean");
                Assert(localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false), "Korean locale is selectable");
                Assert(localization.Format("ui.camp.title") == "베이스캠프 · 제작 / 건설 / 연구", "Korean source string restores immediately");
                Assert(localization.Format("controls.placement.keyboard_mouse", localization.DeviceName(PrototypeInputDevice.KeyboardMouse)).Contains("마우스로 위치 이동"), "Korean keyboard and mouse placement prompt is localized");
                Assert(localization.Format("camp.interaction.prompt.keyboard_mouse", localization.Format("structure.campfire")).Contains("[E] 모닥불 사용"), "Korean keyboard proximity prompt is localized");
                Assert(localization.Format("controls.module_preview.keyboard_mouse", localization.DeviceName(PrototypeInputDevice.KeyboardMouse)).Contains("후보 순환") &&
                       localization.Format("module.cost.provisional", 2, 0, 1).Contains("나무 2") &&
                       localization.Format("world.module.preview.valid", "위층 방", "유효", "확정 가능").Contains("◇ 위층 방"),
                    "Korean module preview, valid shape marker, and locked Wave 9 cost are localized");
                Assert(localization.Format(
                           "interaction.structure.prompt",
                           string.Empty,
                           localization.Format("structure.module_connector", localization.Format("module.name.basement")),
                           localization.Format("interaction.action.preview")).Trim().Contains("지하실 출입 연결부 미리보기") &&
                       localization.Format("interaction.structure.prompt", string.Empty, localization.Format("structure.campfire"), localization.Format("interaction.action.use")).Trim().Contains("모닥불 사용") &&
                       localization.Format(PrototypeInputPromptKeys.InteractGlyph(PrototypeInputDevice.KeyboardMouse)) == "[E]" &&
                       localization.Format("interaction.module.no_slot").Contains("연결 슬롯") &&
                       localization.Format("interaction.module.slot_unavailable").Contains("사용할 수 없다"),
                    "Korean direct slot action and canonical connection reason keys resolve from data");
                Assert(localization.Format("world.barrier.axe.need").Contains("돌도끼 필요"), "Korean forest barrier names the stone axe requirement");
                Assert(localization.ResolveStartupLocale("es") == PrototypeLocalization.KoreanLocaleCode, "Unsupported saved locale resolves to Korean");
                Assert(!PrototypeLocalization.IsPlayerSelectableLocale(PrototypeLocalization.QpsLongLocaleCode) &&
                       localization.ResolveStartupLocale(PrototypeLocalization.QpsLongLocaleCode) == PrototypeLocalization.KoreanLocaleCode,
                    "qps-long is hidden from the player locale list and cannot restore as a saved product locale");

                PrototypeCampInteraction localeInvariantInteraction = new PrototypeCampInteraction();
                localeInvariantInteraction.UpdateSelection(
                    Vector2.zero,
                    1f,
                    new[] { new PrototypeCampInteractionTarget("campfire", PrototypeCampInteractionTargetKind.Campfire, new Vector2(0.5f, 0f)) });
                PrototypeCampInteractionTargetKind targetBeforeQaLocale = localeInvariantInteraction.ActiveTargetKind;
                bool hadPreferenceBeforeQaLocale = PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey);
                string preferenceBeforeQaLocale = PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode);
                string englishStressSource = localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false)
                    ? localization.Format("ui.camp.title")
                    : string.Empty;
                Assert(localization.SetQaLocale() && localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode,
                    "Actual qps-long String Table is selectable only through the QA path");
                string qpsStress = localization.Format("ui.camp.title");
                float qpsExpansion = qpsStress.Length / (float)englishStressSource.Length;
                string qpsKeyboardGlyph = localization.Format(PrototypeInputPromptKeys.InteractGlyph(PrototypeInputDevice.KeyboardMouse));
                string qpsGamepadGlyph = localization.Format(PrototypeInputPromptKeys.InteractGlyph(PrototypeInputDevice.Gamepad));
                string qpsDirectSlotAction = localization.Format(
                    "interaction.structure.prompt",
                    string.Empty,
                    localization.Format("structure.module_connector", localization.Format("module.name.basement")),
                    localization.Format("interaction.action.preview")).Trim();
                string qpsModuleReason = localization.Format(
                    "ui.module.preview.cost",
                    localization.Format("module.name.basement"),
                    2,
                    1,
                    localization.Format("interaction.module.locked_workbench"));
                Assert(qpsExpansion >= 1.35f && qpsExpansion <= 1.50f && qpsStress.StartsWith("⟦", StringComparison.Ordinal) && qpsStress.EndsWith("⟧", StringComparison.Ordinal),
                    "qps-long data expands a representative English string by 35-50 percent");
                Assert(qpsKeyboardGlyph == "[E]" && qpsGamepadGlyph == "[X]" &&
                       !qpsDirectSlotAction.Contains("[X]") && qpsDirectSlotAction.Contains(localization.Format("interaction.action.preview")) &&
                       qpsModuleReason.Contains("2") && qpsModuleReason.Contains("1") &&
                       localeInvariantInteraction.ActiveTargetKind == targetBeforeQaLocale,
                    "ko/en/qps-long switching preserves locale-invariant glyphs, the same target, direct-slot action semantics, and W2/D1 tokens");
                Assert(PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey) == hadPreferenceBeforeQaLocale &&
                       PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode) == preferenceBeforeQaLocale,
                    "QA locale selection does not overwrite the persisted ko/en preference");
                Assert(localization.Format("dev.fallback_probe") == "한국어 폴백 확인", "Missing qps-long translation falls back to Korean");
                localization.CycleLocale(false);
                Assert(localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode,
                    "Player language action exits QA locale into the official ko/en cycle without exposing qps-long");
                Assert(localization.SetLocale(PrototypeLocalization.EnglishLocaleCode), "Locale preference can be persisted");
                Assert(PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey) == PrototypeLocalization.EnglishLocaleCode, "Persisted locale is available to the next launch");

                PrototypeLocaleFontProfile fontProfile = Resources.Load<PrototypeLocaleFontProfile>("PrototypeLocaleFontProfile");
                Assert(fontProfile != null && fontProfile.Find("ko") != null && fontProfile.Find("en") != null &&
                       fontProfile.Find(PrototypeLocalization.QpsLongLocaleCode) != null,
                    "ko/en/qps-long TMP primary and fallback mappings are data assets");
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

            PrototypeCampUse keyboardCampUse = new PrototypeCampUse();
            PrototypeCampUse gamepadCampUse = new PrototypeCampUse();
            keyboardCampUse.Step(PrototypePlayerActions.FromRaw(new PrototypeRawInput { KeyboardRight = true }), 0.5f);
            gamepadCampUse.Step(PrototypePlayerActions.FromRaw(new PrototypeRawInput { HorizontalAxis = 1f }), 0.5f);
            Assert(Mathf.Approximately(keyboardCampUse.PlayerPosition.x, gamepadCampUse.PlayerPosition.x),
                "Keyboard and gamepad movement converge on the same camp action snapshot");
            Vector2 exactUseBoundary = keyboardCampUse.PlayerPosition + Vector2.right * PrototypeCampUse.UseRange;
            Assert(keyboardCampUse.IsWithinUseRange(exactUseBoundary), "Camp facilities are usable at the exact 1.25-unit boundary");
            Assert(!keyboardCampUse.IsWithinUseRange(exactUseBoundary + Vector2.right * 0.01f),
                "Camp facilities reject use beyond the 1.25-unit boundary");

            PrototypeCampInteraction contextualInteraction = new PrototypeCampInteraction();
            List<PrototypeCampInteractionTarget> contextualTargets = new List<PrototypeCampInteractionTarget>
            {
                new PrototypeCampInteractionTarget("left", PrototypeCampInteractionTargetKind.Campfire, new Vector2(-0.75f, 0f)),
                new PrototypeCampInteractionTarget("right", PrototypeCampInteractionTargetKind.Workbench, new Vector2(0.75f, 0f))
            };
            contextualInteraction.UpdateSelection(new Vector2(3f, 0f), 1f, contextualTargets);
            Assert(!contextualInteraction.HasProximityPrompt && !contextualInteraction.IsPopupOpen,
                "Contextual camp interaction hides prompt and popup outside 1.25 units");
            contextualInteraction.UpdateSelection(Vector2.zero, 1f, contextualTargets);
            Assert(contextualInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.Workbench && contextualInteraction.HasProximityPrompt,
                "Equal-distance candidates resolve to the facility in Mr. Kim's facing direction");
            Assert(contextualInteraction.TryOpenPopup() && contextualInteraction.MovementLocked && !contextualInteraction.HasProximityPrompt,
                "Interact opens one facility popup, hides the prompt, and locks movement");
            Assert(contextualInteraction.OpenPopupTargetId == "right",
                "The popup retains the exact latched target ID instead of only its facility kind");
            Assert(contextualInteraction.TryConfirmAction() && !contextualInteraction.TryConfirmAction(),
                "A popup confirmation can be consumed exactly once");
            contextualInteraction.ClosePopup();
            Assert(!contextualInteraction.MovementLocked && contextualInteraction.HasProximityPrompt,
                "Cancel or completion closes the popup and restores direct field interaction");
            contextualInteraction.Reset();
            contextualInteraction.UpdateSelection(Vector2.zero, -1f, contextualTargets);
            Assert(contextualInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.Campfire,
                "Facing left selects the left candidate through the same deterministic state machine");
            PrototypeCampInteraction directSlotInteraction = new PrototypeCampInteraction();
            List<PrototypeCampInteractionTarget> directSlotTargets = new List<PrototypeCampInteractionTarget>
            {
                new PrototypeCampInteractionTarget("slot.start.upper", PrototypeCampInteractionTargetKind.ModuleExpansionSlot, new Vector2(-4f, PrototypeCampUse.PlayerFloorY)),
                new PrototypeCampInteractionTarget("slot.start.side", PrototypeCampInteractionTargetKind.ModuleExpansionSlot, new Vector2(8.1f, PrototypeCampUse.PlayerFloorY)),
                new PrototypeCampInteractionTarget(
                    "slot.start.basement",
                    PrototypeCampInteractionTargetKind.ModuleExpansionSlot,
                    new Vector2(PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement).StartConnectorDisplayX, PrototypeCampUse.PlayerFloorY))
            };
            directSlotInteraction.UpdateSelection(new Vector2(8.1f + PrototypeCampUse.UseRange + 0.01f, PrototypeCampUse.PlayerFloorY), -1f, directSlotTargets);
            Assert(!directSlotInteraction.HasProximityPrompt && !directSlotInteraction.IsPopupOpen,
                "Direct module slots expose no prompt or popup beyond the shared 1.25-unit boundary");
            directSlotInteraction.UpdateSelection(new Vector2(8.1f, PrototypeCampUse.PlayerFloorY), -1f, directSlotTargets);
            Assert(directSlotInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot &&
                   directSlotInteraction.ActiveTargetId == "slot.start.side" && directSlotInteraction.TryOpenPopup() &&
                   directSlotInteraction.OpenPopupTargetId == "slot.start.side",
                "A direct slot approach latches exactly one canonical target and opens only its popup");
            Assert(directSlotInteraction.TryConfirmAction() && directSlotInteraction.PrepareOpenPopupForReturn() &&
                   directSlotInteraction.TryConfirmAction() && directSlotInteraction.OpenPopupTargetId == "slot.start.side",
                "Preview cancel can re-arm Submit while preserving the exact slot popup target");
            directSlotInteraction.ClosePopup();
            Assert(directSlotInteraction.HasProximityPrompt && directSlotInteraction.ActiveTargetId == "slot.start.side",
                "Root popup Cancel returns to the same direct field target without moving the player");
            PrototypeCampInteraction overlappingFacilityInteraction = new PrototypeCampInteraction();
            Vector2 basementInteractionPoint = new Vector2(1.5f, PrototypeCampUse.PlayerFloorY);
            List<PrototypeCampInteractionTarget> basementOnlyTargets = new List<PrototypeCampInteractionTarget>
            {
                new PrototypeCampInteractionTarget(
                    "slot.start.basement",
                    PrototypeCampInteractionTargetKind.ModuleExpansionSlot,
                    basementInteractionPoint,
                    true,
                    1)
            };
            overlappingFacilityInteraction.UpdateSelection(basementInteractionPoint, 1f, basementOnlyTargets);
            Assert(overlappingFacilityInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot,
                "An unobstructed basement slot remains directly selectable");
            List<PrototypeCampInteractionTarget> overlappingFacilityTargets = new List<PrototypeCampInteractionTarget>(basementOnlyTargets)
            {
                new PrototypeCampInteractionTarget(
                    "camp.Workbench",
                    PrototypeCampInteractionTargetKind.Workbench,
                    basementInteractionPoint,
                    true,
                    2)
            };
            overlappingFacilityInteraction.UpdateSelection(basementInteractionPoint, 1f, overlappingFacilityTargets);
            Assert(overlappingFacilityInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.Workbench &&
                   overlappingFacilityInteraction.ActiveTargetId == "camp.Workbench",
                "An installed workbench wins over a co-located basement preview even when the slot was previously latched");
            overlappingFacilityTargets[1] = new PrototypeCampInteractionTarget(
                "camp.Workbench",
                PrototypeCampInteractionTargetKind.Workbench,
                basementInteractionPoint + Vector2.right * 2f,
                true,
                2);
            overlappingFacilityInteraction.UpdateSelection(basementInteractionPoint, 1f, overlappingFacilityTargets);
            Assert(overlappingFacilityInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot &&
                   overlappingFacilityInteraction.ActiveTargetId == "slot.start.basement",
                "Relocating the workbench restores direct access to the basement expansion slot");
            Assert(PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.Workbench, PrototypeCampInteractionAction.Repair, true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.Workbench, PrototypeCampInteractionAction.UpgradeBag, true) &&
                   !PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.Workbench, PrototypeCampInteractionAction.Eat, true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.Campfire, PrototypeCampInteractionAction.Eat, true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.RainCollector, PrototypeCampInteractionAction.CollectRain, true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.RescueSignal, PrototypeCampInteractionAction.UpgradeSignal, true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.StoragePlanning, PrototypeCampInteractionAction.PreviewModule, true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.ModuleExpansionSlot, PrototypeCampInteractionAction.PreviewModule, true) &&
                   !PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.ModuleExpansionSlot, PrototypeCampInteractionAction.BuildOrRelocate, true) &&
                   !PrototypeCampInteractionCatalog.OwnsAction(PrototypeCampInteractionTargetKind.ModuleConnector, PrototypeCampInteractionAction.PreviewModule, true),
                "Facility popup catalog keeps facilities, direct expansion slots, secondary on-site planning, and traversal connectors separated");

            IReadOnlyList<CampModuleDefinition> moduleDefinitions = PrototypeCampModuleCatalog.All;
            Assert(moduleDefinitions.Count == 3 &&
                   PrototypeCampModuleCatalog.StartRoomBounds == new Rect(0f, 0f, 18f, 5f),
                "Spatial camp catalog keeps the canonical starting room and three module candidates");
            CampModuleDefinition upperDefinition = PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper);
            CampModuleDefinition sideDefinition = PrototypeCampModuleCatalog.Get(CampModuleArchetype.Side);
            CampModuleDefinition basementDefinition = PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement);
            Assert(Mathf.Approximately(basementDefinition.StartConnectorDisplayX, 2.5f),
                "The basement entry interaction stays separated from the default workbench at x=1.5");
            Assert(PrototypeCampModuleCatalog.TryGetByStartSlotId("slot.start.upper", out CampModuleDefinition mappedUpper) &&
                   mappedUpper.Archetype == CampModuleArchetype.Upper &&
                   PrototypeCampModuleCatalog.TryGetByStartSlotId("slot.start.side", out CampModuleDefinition mappedSide) &&
                   mappedSide.Archetype == CampModuleArchetype.Side &&
                   PrototypeCampModuleCatalog.TryGetByStartSlotId("slot.start.basement", out CampModuleDefinition mappedBasement) &&
                   mappedBasement.Archetype == CampModuleArchetype.Basement,
                "Canonical start-room slot IDs map deterministically to their first preview candidates");
            Assert(upperDefinition.Bounds == new Rect(0f, 5f, 12f, 5f) && upperDefinition.ConnectorKind == CampModuleConnectorKind.Ladder &&
                   sideDefinition.Bounds == new Rect(18f, 0f, 12f, 5f) && sideDefinition.ConnectorKind == CampModuleConnectorKind.Door &&
                   basementDefinition.Bounds == new Rect(0f, -5f, 12f, 5f) && basementDefinition.ConnectorKind == CampModuleConnectorKind.Ladder,
                "Upper, side, and basement logical coordinates and connector kinds match the Wave 9 spatial specification");
            Assert(Mathf.Approximately(upperDefinition.GeneralFloorMinimumX, 3f) && Mathf.Approximately(upperDefinition.GeneralFloorMaximumX, 11f) &&
                   Mathf.Approximately(sideDefinition.GeneralFloorMinimumX, 2f) && Mathf.Approximately(sideDefinition.GeneralFloorMaximumX, 11f) &&
                   Mathf.Approximately(basementDefinition.GeneralFloorMinimumX, 1f) && Mathf.Approximately(basementDefinition.GeneralFloorMaximumX, 7.75f),
                "Each module exposes its canonical limited-free-placement general floor zone");

            CampModuleValidationContext moduleGeometry = new CampModuleValidationContext();
            Assert(PrototypeCampModuleExpansion.EvaluateGeometry(upperDefinition, moduleGeometry) == CampModuleGeometryStatus.Valid,
                "A touching upper connection is valid and is not treated as positive-area overlap");
            CampModuleValidationContext missingSlot = moduleGeometry.Clone();
            missingSlot.HasMatchingConnectionSlot = false;
            Assert(PrototypeCampModuleExpansion.EvaluateGeometry(upperDefinition, missingSlot) == CampModuleGeometryStatus.NoConnectionSlot,
                "Module preview distinguishes a missing reciprocal connection slot");
            CampModuleValidationContext unavailableSlot = moduleGeometry.Clone();
            unavailableSlot.ConnectionSlotAvailable = false;
            Assert(PrototypeCampModuleExpansion.EvaluateGeometry(upperDefinition, unavailableSlot) == CampModuleGeometryStatus.SlotUnavailable,
                "Module preview distinguishes a defined but unavailable connection slot");
            CampModuleValidationContext overlap = moduleGeometry.Clone();
            overlap.OccupiedRoomBounds.Add(new Rect(1f, 6f, 2f, 2f));
            Assert(PrototypeCampModuleExpansion.EvaluateGeometry(upperDefinition, overlap) == CampModuleGeometryStatus.Overlap,
                "Module preview distinguishes positive-area room overlap");
            CampModuleValidationContext terrainBlocked = moduleGeometry.Clone();
            terrainBlocked.TerrainAllowsCandidate = false;
            Assert(PrototypeCampModuleExpansion.EvaluateGeometry(upperDefinition, terrainBlocked) == CampModuleGeometryStatus.TerrainBlocked,
                "Module preview distinguishes terrain obstruction");
            CampModuleValidationContext pathBlocked = moduleGeometry.Clone();
            pathBlocked.RequiredPathClear = false;
            Assert(PrototypeCampModuleExpansion.EvaluateGeometry(upperDefinition, pathBlocked) == CampModuleGeometryStatus.PathBlocked,
                "Module preview distinguishes connector and required-path obstruction");
            Assert(PrototypeCampModuleReasonKeys.Geometry(CampModuleGeometryStatus.NoConnectionSlot) == "interaction.module.no_slot" &&
                   PrototypeCampModuleReasonKeys.Geometry(CampModuleGeometryStatus.SlotUnavailable) == "interaction.module.slot_unavailable" &&
                   PrototypeCampModuleReasonKeys.Geometry(CampModuleGeometryStatus.Overlap) == "interaction.module.overlap" &&
                   PrototypeCampModuleReasonKeys.Geometry(CampModuleGeometryStatus.TerrainBlocked) == "interaction.module.terrain_blocked" &&
                   PrototypeCampModuleReasonKeys.Geometry(CampModuleGeometryStatus.PathBlocked) == "interaction.module.path_blocked" &&
                   PrototypeCampModuleReasonKeys.Economy(CampModuleEconomyStatus.Locked) == "interaction.module.locked_workbench" &&
                   PrototypeCampModuleReasonKeys.Economy(CampModuleEconomyStatus.Short) == "interaction.module.missing" &&
                   PrototypeCampModuleReasonKeys.Economy(CampModuleEconomyStatus.PrototypeLimit) == "interaction.module.prototype_limit",
                "Geometry and economy statuses remain separate and map only to canonical interaction.module reason keys");

            PrototypeCampModuleExpansionConfig moduleConfig = PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance();
            CampModuleResourceCost moduleCost = moduleConfig.GetCost(CampModuleArchetype.Upper);
            Assert(!moduleConfig.IsProvisional && PrototypeCampModuleExpansionConfig.BalanceStatus == "WAVE9_V0_2" &&
                   moduleConfig.UnlockRequirement.RequiresWorkbench &&
                   moduleCost.Wood == 2 && moduleCost.Stone == 0 && moduleCost.Food == 0 && moduleCost.Salvage == 1,
                "Wave 9 module economy uses the locked W2/D1 cost and workbench commit gate");
            Assert(PrototypeCampModuleReasonKeys.Primary(new CampModuleEvaluation(
                       upperDefinition,
                       CampModuleGeometryStatus.Overlap,
                       CampModuleEconomyStatus.PrototypeLimit,
                       moduleCost)) == "interaction.module.prototype_limit" &&
                   PrototypeCampModuleReasonKeys.Primary(new CampModuleEvaluation(
                       upperDefinition,
                       CampModuleGeometryStatus.Overlap,
                       CampModuleEconomyStatus.Locked,
                       moduleCost)) == "interaction.module.overlap",
                "Prototype limit precedes geometry after one commit; otherwise exact geometry precedes economy without concatenation");
            GameSession moduleSession = new GameSession();
            PrototypeCampModuleExpansion moduleExpansion = new PrototypeCampModuleExpansion(moduleConfig);
            CampModuleReturnSnapshot planningSnapshot = new CampModuleReturnSnapshot(new Vector2(-3.5f, PrototypeCampPlacement.FloorY), 1f, PrototypeCampModuleCatalog.StartRoomId);
            Assert(moduleExpansion.BeginPreview(planningSnapshot), "Direct planning point opens the module preview");
            int emptyWood = moduleSession.GetStorage(ResourceKind.Wood);
            Assert(moduleExpansion.TryCommit(moduleSession, moduleGeometry) == CampModuleCommitStatus.Locked &&
                   moduleSession.GetStorage(ResourceKind.Wood) == emptyWood && !moduleExpansion.HasCommittedModule,
                "Module preview stays available but commit is locked before the workbench without spending");
            moduleExpansion.Cycle(1);
            moduleExpansion.Cycle(1);
            Assert(moduleExpansion.HasSeenAllCandidates, "Starting room can cycle through upper, side, and basement candidates");
            CampModuleReturnSnapshot cancelledSnapshot = moduleExpansion.CancelPreview();
            Assert(cancelledSnapshot.Position == planningSnapshot.Position && !moduleExpansion.IsPreviewActive && !moduleExpansion.HasCommittedModule,
                "Cancelled module preview returns to the same field position without state changes");
            PrototypeCampModuleExpansion slotSeededExpansion = new PrototypeCampModuleExpansion(moduleConfig);
            CampModuleReturnSnapshot sideSlotSnapshot = new CampModuleReturnSnapshot(new Vector2(8.1f, PrototypeCampUse.PlayerFloorY), -1f, PrototypeCampModuleCatalog.StartRoomId);
            Assert(slotSeededExpansion.BeginPreview(sideSlotSnapshot, CampModuleArchetype.Side) &&
                   slotSeededExpansion.SelectedArchetype == CampModuleArchetype.Side,
                "Direct side-slot Submit seeds the first preview candidate from the approached slot");
            slotSeededExpansion.Cycle(1);
            Assert(slotSeededExpansion.SelectedArchetype == CampModuleArchetype.Basement,
                "Candidate navigation preserves the canonical upper-to-side-to-basement ring from any seed");
            slotSeededExpansion.CancelPreview();
            Assert(slotSeededExpansion.SelectedArchetype == CampModuleArchetype.Basement &&
                   slotSeededExpansion.ResumePreview(sideSlotSnapshot) &&
                   slotSeededExpansion.SelectedArchetype == CampModuleArchetype.Basement,
                "Preview-to-popup-to-preview preserves the selected candidate and return snapshot");
            slotSeededExpansion.CancelPreview();
            moduleSession.Grant(ResourceKind.Wood, 8);
            moduleSession.Grant(ResourceKind.Stone, 4);
            moduleSession.Grant(ResourceKind.Salvage, 4);
            Assert(moduleSession.TryBuild(StructureKind.Workbench), "Module commit scenario builds the required workbench");
            int moduleWoodBefore = moduleSession.GetStorage(ResourceKind.Wood);
            int moduleStoneBefore = moduleSession.GetStorage(ResourceKind.Stone);
            int moduleSalvageBefore = moduleSession.GetStorage(ResourceKind.Salvage);
            Assert(moduleExpansion.BeginPreview(planningSnapshot) && moduleExpansion.TryCommit(moduleSession, moduleGeometry) == CampModuleCommitStatus.Succeeded &&
                   moduleExpansion.HasCommittedModule && moduleExpansion.CommittedArchetype == CampModuleArchetype.Upper,
                "One valid module commit creates exactly the selected module");
            Assert(moduleSession.GetStorage(ResourceKind.Wood) == moduleWoodBefore - moduleCost.Wood &&
                   moduleSession.GetStorage(ResourceKind.Stone) == moduleStoneBefore - moduleCost.Stone &&
                   moduleSession.GetStorage(ResourceKind.Salvage) == moduleSalvageBefore - moduleCost.Salvage,
                "Successful module commit atomically spends the locked W2/D1 cost exactly once");
            int committedWood = moduleSession.GetStorage(ResourceKind.Wood);
            Assert(moduleExpansion.TryCommit(moduleSession, moduleGeometry) == CampModuleCommitStatus.NotPreviewing &&
                   moduleSession.GetStorage(ResourceKind.Wood) == committedWood,
                "Duplicate confirmation after success cannot spend resources twice");
            Assert(moduleExpansion.BeginPreview(planningSnapshot) &&
                   moduleExpansion.Evaluate(moduleSession, moduleGeometry).Economy == CampModuleEconomyStatus.PrototypeLimit &&
                   moduleExpansion.TryCommit(moduleSession, moduleGeometry) == CampModuleCommitStatus.PrototypeLimit &&
                   moduleSession.GetStorage(ResourceKind.Wood) == committedWood,
                "Vertical slice previews after completion but rejects a second committed module without spending");
            moduleExpansion.CancelPreview();

            PrototypeCampUse moduleTravel = new PrototypeCampUse();
            moduleTravel.Warp(planningSnapshot.Position);
            moduleTravel.EnterRoom(upperDefinition.RoomId, upperDefinition.ModuleConnectorDisplayX);
            Assert(moduleTravel.CurrentRoomId == upperDefinition.RoomId && Mathf.Approximately(moduleTravel.PlayerPosition.x, upperDefinition.ModuleConnectorDisplayX),
                "Explicit ladder path enters the committed upper room at its landing");
            moduleTravel.Restore(planningSnapshot);
            Assert(moduleTravel.CurrentRoomId == PrototypeCampModuleCatalog.StartRoomId && moduleTravel.PlayerPosition == planningSnapshot.Position,
                "Explicit connector return restores the starting room field position");

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
            Assert(PrototypeCampPlacement.GetRequiredZone(StructureKind.Campfire) == CampPlacementZone.GeneralGround &&
                   PrototypeCampPlacement.GetRequiredZone(StructureKind.Workbench) == CampPlacementZone.GeneralGround &&
                   PrototypeCampPlacement.GetRequiredZone(StructureKind.RainCollector) == CampPlacementZone.OpenSkyGround,
                "Campfire and workbench use camp.general-ground while the rain collector uses camp.open-sky-ground");
            placement.SetCandidateX(-1.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.Valid, "Campfire has a valid snapped location");
            Assert(placementSession.TryBuild(StructureKind.Campfire) && placement.Commit(), "Campfire placement spends build cost once");

            PrototypeCampPlacement autoPlacement = new PrototypeCampPlacement();
            autoPlacement.Begin(StructureKind.Campfire, false);
            autoPlacement.SetCandidateX(1.5f);
            Assert(autoPlacement.Commit(), "Auto-placement fixture reserves the workbench preferred point");
            autoPlacement.Begin(StructureKind.Workbench, false);
            Assert(autoPlacement.CurrentValidity == CampPlacementValidity.Valid &&
                   !Mathf.Approximately(autoPlacement.CandidateX, 1.5f),
                "New workbench placement automatically selects the nearest valid snapped point when its preferred point overlaps");

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
            placement.Begin(StructureKind.Workbench, true);
            placement.SetCandidateX(0f);
            Assert(placement.CurrentValidity == CampPlacementValidity.BlocksRequiredPath, "Relocation still preserves the required walking path");
            placement.Cancel();
            Assert(Mathf.Approximately(placement.GetInstalledPosition(StructureKind.Workbench).x, 3.5f),
                "Cancelled relocation restores the installed workbench position");
            placement.Begin(StructureKind.Workbench, true);
            placement.SetCandidateX(1.5f);
            Assert(placement.Commit(), "Workbench can return to its original general-ground position for later facilities");

            placement.Begin(StructureKind.RainCollector, false);
            placement.SetCandidateX(1.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.WrongZone,
                "Rain collector rejects camp.general-ground without spending resources");
            placement.SetCandidateX(3.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.Valid, "Rain collector accepts camp.open-sky-ground");
            placement.Cancel();

            CampPlacementRoomZone upperPlacementZone = new CampPlacementRoomZone(
                upperDefinition.RoomId,
                upperDefinition.GeneralFloorDisplayMinimumX,
                upperDefinition.GeneralFloorDisplayMaximumX,
                false,
                0f,
                0f,
                upperDefinition.ModuleConnectorDisplayX - 0.8f,
                upperDefinition.ModuleConnectorDisplayX + 0.8f,
                upperDefinition.ModuleConnectorDisplayX - 1.1f,
                upperDefinition.ModuleConnectorDisplayX + 1.1f);
            int moduleMoveWoodBefore = placementSession.GetStorage(ResourceKind.Wood);
            int moduleMoveStoneBefore = placementSession.GetStorage(ResourceKind.Stone);
            int moduleMoveSalvageBefore = placementSession.GetStorage(ResourceKind.Salvage);
            placement.Begin(StructureKind.Workbench, true, upperPlacementZone);
            placement.SetCandidateX(upperDefinition.ModuleConnectorDisplayX);
            Assert(placement.CurrentValidity == CampPlacementValidity.OutsideCampBounds ||
                   placement.CurrentValidity == CampPlacementValidity.BlocksEntrance ||
                   placement.CurrentValidity == CampPlacementValidity.BlocksRequiredPath,
                "Committed module general zone excludes and protects its explicit connector and required landing path");
            placement.SetCandidateX(2.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.Valid && placement.Commit() &&
                   placement.GetInstalledRoomId(StructureKind.Workbench) == upperDefinition.RoomId,
                "Workbench can be freely relocated into the committed module general-floor zone");
            Assert(placementSession.GetStorage(ResourceKind.Wood) == moduleMoveWoodBefore &&
                   placementSession.GetStorage(ResourceKind.Stone) == moduleMoveStoneBefore &&
                   placementSession.GetStorage(ResourceKind.Salvage) == moduleMoveSalvageBefore,
                "Cross-room facility relocation spends no resources");
            placement.Begin(StructureKind.RainCollector, false, upperPlacementZone);
            placement.SetCandidateX(2.5f);
            Assert(placement.CurrentValidity == CampPlacementValidity.WrongZone,
                "Module interior rejects the open-sky-only rain collector");
            placement.Cancel();

            placementSession.Grant(ResourceKind.Wood, 2);
            placementSession.Grant(ResourceKind.Stone, 1);
            placementSession.Grant(ResourceKind.Salvage, 3);
            Assert(placementSession.TryResearch(TechKind.StoneAxe) && placementSession.TryUpgradeSignal(),
                "Relocation preservation probe establishes research and signal progress");
            keyboardCampUse.Warp(placement.GetInstalledPosition(StructureKind.Campfire));
            Assert(keyboardCampUse.TryPrepareDayBenefit(StructureKind.Campfire, placement.GetInstalledPosition(StructureKind.Campfire)),
                "Campfire day benefit activates only through an in-range camp use");
            bool preparedBeforeRelocation = keyboardCampUse.IsDayBenefitPrepared(StructureKind.Campfire);
            int signalBeforeRelocation = placementSession.SignalStage;
            bool axeResearchBeforeRelocation = placementSession.HasResearched(TechKind.StoneAxe);
            placement.Begin(StructureKind.Workbench, true);
            placement.SetCandidateX(3.5f);
            Assert(placement.Commit(), "Workbench free relocation commits after progression is established");
            Assert(keyboardCampUse.IsDayBenefitPrepared(StructureKind.Campfire) == preparedBeforeRelocation &&
                   placementSession.SignalStage == signalBeforeRelocation &&
                   placementSession.HasResearched(TechKind.StoneAxe) == axeResearchBeforeRelocation,
                "Committed free relocation preserves research, signal, and current-day benefit state");

            GameSession unpreparedRest = CreateSpatialRestProbe();
            GameSession preparedRest = CreateSpatialRestProbe();
            Assert(unpreparedRest.EndDay(false, false) && preparedRest.EndDay(true, true) &&
                   preparedRest.Energy > unpreparedRest.Energy,
                "Campfire and rain-collector settlement benefits require explicit in-range preparation");

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

            GameSession naturalBagRoute = new GameSession();
            Assert(naturalBagRoute.BeginSearch(), "Natural bag route starts day one");
            Assert(naturalBagRoute.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                   naturalBagRoute.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                   naturalBagRoute.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added &&
                   naturalBagRoute.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added,
                "Natural bag route gathers day-one workbench and rope materials without grants");
            Assert(naturalBagRoute.ReturnToCamp(false) && naturalBagRoute.TryBuild(StructureKind.Workbench),
                "Natural bag route returns and builds the workbench on day one");
            Assert(naturalBagRoute.TryResearch(TechKind.Rope) && naturalBagRoute.TryCraft(TechKind.Rope),
                "Natural bag route researches and crafts rope on day one");
            Assert(naturalBagRoute.EndDay() && naturalBagRoute.Day == 2 && naturalBagRoute.ActiveBagSlotCount == 4,
                "Natural bag route keeps four slots into day two before upgrade");

            Assert(naturalBagRoute.BeginSearch(), "Natural bag route starts day two");
            Assert(naturalBagRoute.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                   naturalBagRoute.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                   naturalBagRoute.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added &&
                   naturalBagRoute.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added,
                "Natural bag route gathers day-two capacity and signal materials");
            Assert(naturalBagRoute.ReturnToCamp(false) && naturalBagRoute.TryUpgradeBagCapacity() && naturalBagRoute.ActiveBagSlotCount == 6,
                "Natural bag route purchases the exact one-time four-to-six upgrade on day two");
            Assert(naturalBagRoute.TryUpgradeSignal() && naturalBagRoute.SignalStage == 1,
                "Natural bag route completes signal stage one after the bag upgrade");
            Assert(naturalBagRoute.EndDay() && naturalBagRoute.Day == 3 && naturalBagRoute.ActiveBagSlotCount == 6,
                "Natural bag route persists six slots into day three");

            Assert(naturalBagRoute.BeginSearch(), "Natural bag route starts day three");
            Assert(naturalBagRoute.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                   naturalBagRoute.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added,
                "Natural bag route gathers final signal materials on day three");
            Assert(naturalBagRoute.ReturnToCamp(false) && naturalBagRoute.TryUpgradeSignal() && naturalBagRoute.Result == RunResult.Rescued,
                "Natural three-day route reaches rescue with the upgraded bag and no debug grants");

            Assert(GameSession.FinalDay == 50, "Wave 15 campaign tuning uses the shared fifty-day constant");
            GameSession deadline = new GameSession();
            deadline.Grant(ResourceKind.Food, GameSession.FinalDay);
            for (int day = 1; day < GameSession.FinalDay; day += 1)
            {
                Assert(deadline.BeginSearch(), "Deadline scenario search day " + day);
                Assert(deadline.ReturnToCamp(false), "Deadline scenario returns day " + day);
                Assert(deadline.UseFood(), "Deadline scenario consumes one existing food ration before settlement " + day);
                Assert(deadline.EndDay(), "Deadline scenario ends day " + day);
                Assert(deadline.Result == RunResult.None && deadline.Day == day + 1,
                    "Unfinished day " + day + " advances without an early deadline");
            }
            Assert(deadline.Day == 50 && deadline.Result == RunResult.None,
                "Day 49 settlement survives and reaches the playable fiftieth day");
            Assert(deadline.BeginSearch() && deadline.ReturnToCamp(false) && deadline.UseFood() && deadline.EndDay() &&
                   deadline.Result == RunResult.Deadline && deadline.Day == 50 &&
                   deadline.ResultDetail().Key == "result.detail.deadline",
                "Unfinished Day 50 fails only at settlement with an explained terminal resolution");

            GameSession earlyRescue = new GameSession();
            earlyRescue.Grant(ResourceKind.Wood, 20);
            earlyRescue.Grant(ResourceKind.Salvage, 20);
            Assert(earlyRescue.TryBuild(StructureKind.Workbench) &&
                   earlyRescue.TryResearch(TechKind.Rope) && earlyRescue.TryCraft(TechKind.Rope) &&
                   earlyRescue.TryUpgradeSignal() && earlyRescue.TryUpgradeSignal() &&
                   earlyRescue.Result == RunResult.Rescued && earlyRescue.Day == 1,
                "Completing the rescue signal before Day 50 succeeds immediately");

            VerifyCampaignMapContract();

            VerifyWave17HazardEscapeEndingContract();

            VerifyDevelopmentPlaytestLogContract();

            string report =
                "PASS · deterministic edit checks\n" +
                "Started UTC: " + started.ToString("O") + "\n" +
                "Completed UTC: " + DateTime.UtcNow.ToString("O") + "\n" +
                "Checks: Wave 20 natural shore-launch raft stages, protected sailcloth, weather/current launch window, atomic failure/retry and snapshot restore; Wave 17 four-phase hazard budget and idempotent transaction, five escape catalog entries with playable raft/smoke/radio, private stable-ID snapshot/log schema, nineteen deterministic endings and terminal priority; Wave 16 selected-only right-rail A import/GUID, seven non-color region states and playable verification transitions; Wave 15 fifty-day boundary (Day 49 continues, Day 50 settlement resolves, early signal wins), direct proximity expedition map, seven exact localized region profiles, deterministic seed/profile/action results, three-route softlock manifest, selected-region world profile and privacy-free development log linkage; Wave 13 local JSONL schema; compact-a, direct module slots, storage planning, placement, bag 4-to-6, swimming, barrier, signal and crafting regressions\n";
            File.WriteAllText(Path.Combine(VerificationFolder, "editmode-checks.txt"), report);
            Debug.Log("[Kim Survival] " + report.Replace('\n', ' '));
        }

        private static void VerifyWave17HazardEscapeEndingContract()
        {
            Assert(CampaignHazardCatalog.All.Count == 3 &&
                   CampaignHazardCatalog.All.All(value =>
                       !string.IsNullOrEmpty(value.WarningRule) && !string.IsNullOrEmpty(value.OccurrenceRule) &&
                       !string.IsNullOrEmpty(value.MitigationRule) && !string.IsNullOrEmpty(value.RecoveryRule)),
                "Wave 17 hazards expose telegraph, occurrence, mitigation, and recovery data");
            Assert(CampaignHazardBudgetConfig.DailyBudget == 4 && CampaignHazardBudgetConfig.MaxMajor == 1 &&
                   CampaignHazardBudgetConfig.MaxActive == 2 && CampaignHazardBudgetConfig.RecoveryReserve == 2,
                "Wave 17 hazard daily, major, concurrent, and recovery-reservation budgets are centralized");

            PrototypeHazardDirector hazard = new PrototypeHazardDirector();
            PrototypeHazardLedger ledger = new PrototypeHazardLedger();
            Assert(hazard.TryTelegraph("event.edit.injury", "hazard.injury", 11, ledger) &&
                   hazard.TryResolveOccurrence("event.edit.injury", ledger),
                "Hazard warning advances to occurrence");
            int healthAfterOccurrence = ledger.Health;
            int logAfterOccurrence = ledger.LogCount;
            Assert(hazard.TryResolveOccurrence("event.edit.injury", ledger) &&
                   ledger.Health == healthAfterOccurrence && ledger.LogCount == logAfterOccurrence,
                "Retrying the same hazard event and phase is idempotent for health and log state");
            Assert(hazard.TryMitigate("event.edit.injury", ledger) && hazard.TryRecover("event.edit.injury", ledger),
                "Hazard occurrence advances through mitigation and scheduled recovery");
            Assert(PrototypeHazardDirector.VerifyHazardAtomicIdempotentFixture().Success,
                "Public hazard atomic/idempotent fixture passes");

            PrototypeHazardDirector budgetHazards = new PrototypeHazardDirector();
            PrototypeHazardLedger budgetLedger = new PrototypeHazardLedger();
            Assert(budgetHazards.TryTelegraph("event.edit.budget.injury", "hazard.injury", 12, budgetLedger) &&
                   budgetHazards.TryMitigate("event.edit.budget.injury", budgetLedger) &&
                   budgetHazards.TryRecover("event.edit.budget.injury", budgetLedger) &&
                   budgetHazards.TryTelegraph("event.edit.budget.disaster", "hazard.disaster", 12, budgetLedger) &&
                   budgetHazards.TryResolveOccurrence("event.edit.budget.disaster", budgetLedger) &&
                   budgetHazards.TryRecover("event.edit.budget.disaster", budgetLedger) &&
                   budgetHazards.TryTelegraph("event.edit.budget.theft", "hazard.food-theft", 12, budgetLedger) &&
                   budgetHazards.TryResolveOccurrence("event.edit.budget.theft", budgetLedger) &&
                   budgetHazards.TryRecover("event.edit.budget.theft", budgetLedger) &&
                   budgetHazards.SpentDailyBudget == CampaignHazardBudgetConfig.DailyBudget &&
                   !budgetHazards.TryTelegraph("event.edit.budget.overflow", "hazard.injury", 12, budgetLedger),
                "Hazard daily budget reserves recovery and rejects overflow after four committed points");

            PrototypeHazardDirector concurrentHazards = new PrototypeHazardDirector();
            PrototypeHazardLedger concurrentLedger = new PrototypeHazardLedger();
            Assert(concurrentHazards.TryTelegraph("event.edit.concurrent.disaster", "hazard.disaster", 13, concurrentLedger) &&
                   !concurrentHazards.TryTelegraph("event.edit.concurrent.major", "hazard.disaster", 13, concurrentLedger) &&
                   concurrentHazards.TryTelegraph("event.edit.concurrent.injury", "hazard.injury", 13, concurrentLedger) &&
                   !concurrentHazards.TryTelegraph("event.edit.concurrent.theft", "hazard.food-theft", 13, concurrentLedger) &&
                   concurrentHazards.NewMajorCount == CampaignHazardBudgetConfig.MaxMajor &&
                   concurrentHazards.ActiveHazardCount == CampaignHazardBudgetConfig.MaxActive &&
                   concurrentHazards.ReservedRecoveryBudget == CampaignHazardBudgetConfig.RecoveryReserve,
                "Hazard major, concurrent-active, and recovery reservation caps reject excess scheduling");

            Assert(PrototypeEscapeProjectCatalog.All.Count == 5 &&
                   PrototypeEscapeProjectCatalog.All.Select(value => value.StableId).Distinct(StringComparer.Ordinal).Count() == 5,
                "Five stable escape methods are public runtime data");
            Assert(PrototypeEscapeProjectCatalog.Get("escape.smoke").PlayableState.StartsWith("playable", StringComparison.Ordinal) &&
                   PrototypeEscapeProjectCatalog.Get("escape.radio").PlayableState.StartsWith("playable", StringComparison.Ordinal) &&
                   PrototypeEscapeProjectCatalog.Get("escape.raft").PlayableState.StartsWith("playable", StringComparison.Ordinal) &&
                   PrototypeEscapeProjectCatalog.Get("escape.flare").PlayableState.StartsWith("data-only", StringComparison.Ordinal) &&
                   PrototypeEscapeProjectCatalog.Get("escape.beacon").PlayableState.StartsWith("data-only", StringComparison.Ordinal),
                "Raft/smoke/radio are playable while flare/beacon remain honestly data-only");
            PrototypeContractProbe smokeFixture = PrototypeEscapeProjectDirector.VerifyEscapeSmokeProgressCompleteFixture();
            PrototypeContractProbe radioFixture = PrototypeEscapeProjectDirector.VerifyEscapeRadioProgressCompleteFixture();
            PrototypeContractProbe raftFixture = PrototypeRaftRuntimeContract.VerifyAtomicFailureRetrySnapshotFixture();
            Assert(smokeFixture.Success && radioFixture.Success && smokeFixture.Detail.Contains("no-grant no-warp") &&
                   radioFixture.Detail.Contains("no-grant no-warp") && raftFixture.Success &&
                   raftFixture.Detail.Contains("grant=false warp=false"),
                "Raft, smoke and radio expose deterministic natural progress/complete fixtures without grant or warp");
            Assert(PrototypeCampInteractionCatalog.OwnsAction(
                       PrototypeCampInteractionTargetKind.SmokeBeacon,
                       PrototypeCampInteractionAction.ProgressSmokeEscape,
                       true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(
                       PrototypeCampInteractionTargetKind.RadioBench,
                       PrototypeCampInteractionAction.ProgressRadioEscape,
                       true) &&
                   PrototypeCampInteractionCatalog.OwnsAction(
                       PrototypeCampInteractionTargetKind.ShoreLaunch,
                       PrototypeCampInteractionAction.ProgressRaftEscape,
                       true),
                "Raft, smoke and radio are owned by distinct contextual camp interaction targets");
            Assert(PrototypeEscapeProjectCatalog.All.Select(value => value.FacilityId).Distinct(StringComparer.Ordinal).Count() == 5 &&
                   PrototypeEscapeProjectCatalog.All.Select(value => value.KeyPartId).Distinct(StringComparer.Ordinal).Count() == 5 &&
                   PrototypeEscapeProjectCatalog.All.Select(value => value.TimingRule).Distinct(StringComparer.Ordinal).Count() == 5,
                "Every escape method has distinct facility, core-part, and timing axes");

            Assert(PrototypeEndingCatalog.All.Count == 21 &&
                   PrototypeEndingCatalog.All.Count(value => value.Sample) == 4 &&
                   PrototypeEndingCatalog.All.Select(value => value.StableId).Distinct(StringComparer.Ordinal).Count() == 21,
                "Twenty-one stable endings and four sample endings are unique");
            Assert(PrototypeEndingResolver.VerifyEndingDeterministicSingleFixture().Success &&
                   PrototypeEndingResolver.VerifyGameJamLongStayEndingFixture().Success &&
                   PrototypeEndingResolver.VerifyEndingDay50BehaviorFixture().Success &&
                   PrototypeTerminalContract.VerifyTerminalEscapeDay50PriorityFixture().Success,
                "Ending resolution is deterministic and early escape precedes Game Jam Day 20 and Day 50 behavior resolution");

            PrototypeRunSnapshot snapshot = new PrototypeRunSnapshot
            {
                seed = 17017,
                day = 22,
                region_id = "region.forest.grove",
                hazard_ids = new[] { "hazard.injury" },
                project_ids = new[] { "escape.smoke" },
                behavior_scores = new[] { new PrototypeBehaviorScore { StableId = "stat.hazard-response", Value = 3 } },
                escape_id = "escape.smoke",
                ending_id = "ending.escape.smoke.seen-from-afar",
                result_code = "escape.complete"
            };
            string serialized = JsonUtility.ToJson(snapshot);
            Assert(serialized.Contains("17017") && serialized.Contains("region.forest.grove") &&
                   serialized.Contains("hazard.injury") && serialized.Contains("escape.smoke") &&
                   serialized.Contains("ending.escape.smoke.seen-from-afar") &&
                   !serialized.Contains("username") && !serialized.Contains("machine") && !serialized.Contains("email"),
                "Run snapshot serializes stable IDs and result codes without personal or free-input fields");
        }

        private static void VerifyCampaignMapContract()
        {
            IReadOnlyList<PrototypeExpeditionRegionProfile> profiles = PrototypeExpeditionRegionCatalog.All;
            PrototypeExpeditionRegionId[] expectedRegions =
            {
                PrototypeExpeditionRegionId.Beach,
                PrototypeExpeditionRegionId.Forest,
                PrototypeExpeditionRegionId.Shallows,
                PrototypeExpeditionRegionId.RidgeHighland,
                PrototypeExpeditionRegionId.CaveIsland,
                PrototypeExpeditionRegionId.CoveWreck,
                PrototypeExpeditionRegionId.RuinsRelay
            };
            string[] expectedStableIds =
            {
                "region.coast.beach",
                "region.forest.grove",
                "region.sea.shallows",
                "region.ridge.highland",
                "region.cave.island",
                "region.cove.wreck",
                "region.ruins.relay"
            };
            Assert(profiles.Count == expectedRegions.Length,
                "Campaign map exposes exactly seven region profiles");
            for (int regionIndex = 0; regionIndex < expectedRegions.Length; regionIndex += 1)
            {
                PrototypeExpeditionRegionProfile profile = profiles[regionIndex];
                Assert(profile.Id == expectedRegions[regionIndex] &&
                       string.Equals(profile.StableId, expectedStableIds[regionIndex], StringComparison.Ordinal) &&
                       ReferenceEquals(PrototypeExpeditionRegionCatalog.Get(expectedRegions[regionIndex]), profile),
                    "Campaign map preserves exact stable ID order and catalog roundtrip: " + expectedStableIds[regionIndex]);

                GameSession roundtrip = new GameSession(PrototypeExpeditionRegionCatalog.DefaultRunSeed);
                Assert(roundtrip.BeginSearch(expectedRegions[regionIndex]) &&
                       roundtrip.SelectedRegionId == expectedRegions[regionIndex] &&
                       string.Equals(roundtrip.ActiveRegionProfileId, expectedStableIds[regionIndex], StringComparison.Ordinal),
                    "Game session roundtrips the selected region stable ID: " + expectedStableIds[regionIndex]);
            }

            int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            bool differentSeedVariesWithinProfile = false;
            for (int profileIndex = 0; profileIndex < profiles.Count; profileIndex += 1)
            {
                PrototypeExpeditionRegionProfile profile = profiles[profileIndex];
                for (int nodeIndex = 0; nodeIndex < profile.NodeCount; nodeIndex += 1)
                {
                    PrototypeExpeditionNodeResult first = profile.ResolveNode(seed, nodeIndex);
                    PrototypeExpeditionNodeResult second = profile.ResolveNode(seed, nodeIndex);
                    PrototypeExpeditionNodeResult alternateSeed = profile.ResolveNode(seed + 1, nodeIndex);
                    Assert(first.ActionId == second.ActionId && first.Resource == second.Resource &&
                           first.Amount == second.Amount && first.Water == second.Water && first.ResultId == second.ResultId,
                        "Same seed, region and action resolve the same core result: " + profile.StableId + " node " + nodeIndex);
                    differentSeedVariesWithinProfile |= first.Resource != alternateSeed.Resource ||
                                                        first.Amount != alternateSeed.Amount ||
                                                        first.ResultId != alternateSeed.ResultId;
                }
            }
            Assert(differentSeedVariesWithinProfile,
                "Different run seeds vary at least one profile result within the allowed resource patterns");

            PrototypeExpeditionSeedManifest manifest = PrototypeExpeditionRegionCatalog.BuildSeedManifest(seed);
            HashSet<string> routeIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> coreIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<PrototypeExpeditionRegionId> assignedRegions = new HashSet<PrototypeExpeditionRegionId>();
            for (int guaranteeIndex = 0; guaranteeIndex < manifest.Guarantees.Count; guaranteeIndex += 1)
            {
                PrototypeEscapeRouteGuarantee guarantee = manifest.Guarantees[guaranteeIndex];
                routeIds.Add(guarantee.EscapeRouteId);
                coreIds.Add(guarantee.CoreResultId);
                assignedRegions.Add(guarantee.Region);
                PrototypeExpeditionRegionProfile profile = PrototypeExpeditionRegionCatalog.Get(guarantee.Region);
                Assert(profile.ResolveNode(seed, profile.NodeCount - 1).ResultId == guarantee.CoreResultId,
                    "Each guaranteed escape-route core is reachable from its assigned region profile");
            }
            Assert(manifest.HasMinimumSoftlockProtection && manifest.Guarantees.Count == 3 &&
                   routeIds.Count == 3 && coreIds.Count == 3 && assignedRegions.Count == 3 &&
                   manifest.GuaranteesRoute("escape.smoke") && manifest.GuaranteesRoute("escape.radio") && manifest.GuaranteesRoute("escape.raft"),
                "Every run seed reserves three distinct escape-route cores across the seven-region catalog");

            PrototypeCampUse campUse = new PrototypeCampUse();
            PrototypeCampInteraction interaction = new PrototypeCampInteraction();
            Vector2 mapPosition = new Vector2(2f, PrototypeCampUse.PlayerFloorY);
            PrototypeCampInteractionTarget[] targets =
            {
                new PrototypeCampInteractionTarget("camp.expedition-map", PrototypeCampInteractionTargetKind.ExpeditionMap, mapPosition)
            };
            campUse.Warp(new Vector2(mapPosition.x - PrototypeCampUse.UseRange - 0.01f, mapPosition.y));
            interaction.UpdateSelection(campUse.PlayerPosition, campUse.FacingDirection, targets);
            Assert(!interaction.HasProximityPrompt && !interaction.IsPopupOpen,
                "Expedition map has no prompt or popup outside the exact 1.25-unit range");
            campUse.Warp(new Vector2(mapPosition.x - PrototypeCampUse.UseRange, mapPosition.y));
            interaction.UpdateSelection(campUse.PlayerPosition, 1f, targets);
            Vector2 returnPosition = campUse.PlayerPosition;
            float returnFacing = campUse.FacingDirection;
            Assert(interaction.HasProximityPrompt && interaction.ActiveTargetId == "camp.expedition-map" && interaction.TryOpenPopup(),
                "Approaching and interacting with the map opens its contextual popup");
            interaction.ClosePopup();
            interaction.UpdateSelection(campUse.PlayerPosition, campUse.FacingDirection, targets);
            Assert(campUse.PlayerPosition == returnPosition && Mathf.Approximately(campUse.FacingDirection, returnFacing) &&
                   interaction.ActiveTargetId == "camp.expedition-map",
                "Cancelling the map restores the same camp position, facing and target");

            PrototypeExpeditionMapSelection selection = new PrototypeExpeditionMapSelection();
            selection.Open(null);
            Assert(selection.FocusedRegionId == PrototypeExpeditionRegionId.Beach && selection.StepFocus(1) &&
                   selection.FocusedRegionId == PrototypeExpeditionRegionId.Forest && !selection.StepFocus(1),
                "Map focus starts on beach and debounces shared directional input");
            selection.StepFocus(0);
            Assert(selection.StepFocus(1) && selection.FocusedRegionId == PrototypeExpeditionRegionId.Shallows,
                "Map focus cycles beach to forest to shallows after release");
            for (int regionIndex = 3; regionIndex < expectedRegions.Length; regionIndex += 1)
            {
                selection.StepFocus(0);
                Assert(selection.StepFocus(1) && selection.FocusedRegionId == expectedRegions[regionIndex],
                    "Map focus exposes the added region: " + expectedStableIds[regionIndex]);
            }
            selection.StepFocus(0);
            Assert(selection.StepFocus(1) && selection.FocusedRegionId == PrototypeExpeditionRegionId.Beach,
                "Map focus wraps from ruins relay to beach across all seven regions");

            Assert(expectedRegions.All(region => selection.GetRegionState(region) == PrototypeExpeditionRegionVisualState.DepartureReady) &&
                   selection.CanDepartFocusedRegion(),
                "All seven regions use the playable departure-ready state");
            PrototypeExpeditionRegionVisualState[] visualStates =
            {
                PrototypeExpeditionRegionVisualState.Default,
                PrototypeExpeditionRegionVisualState.Selected,
                PrototypeExpeditionRegionVisualState.Locked,
                PrototypeExpeditionRegionVisualState.RiskWarning,
                PrototypeExpeditionRegionVisualState.EquipmentMissing,
                PrototypeExpeditionRegionVisualState.DepartureReady,
                PrototypeExpeditionRegionVisualState.Unknown
            };
            HashSet<string> markers = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> patterns = new HashSet<string>(StringComparer.Ordinal);
            for (int stateIndex = 0; stateIndex < visualStates.Length; stateIndex += 1)
            {
                PrototypeExpeditionRegionVisualPresentation presentation =
                    PrototypeExpeditionRegionVisualCatalog.Get(visualStates[stateIndex]);
                Assert(presentation.State == visualStates[stateIndex] &&
                       !string.IsNullOrWhiteSpace(presentation.Marker) &&
                       !string.IsNullOrWhiteSpace(presentation.Pattern) &&
                       presentation.LocalizationKey.StartsWith("expedition.map.state.", StringComparison.Ordinal) &&
                       presentation.BorderWeight >= 1,
                    "Each expedition state exposes a non-color marker, pattern, localized text and border contract: " + visualStates[stateIndex]);
                markers.Add(presentation.Marker);
                patterns.Add(presentation.Pattern);
            }
            Assert(markers.Count == visualStates.Length && patterns.Count == visualStates.Length,
                "The seven expedition states have distinct visible marker and pattern identities");
            Assert(selection.SetRegionStateForVerification(PrototypeExpeditionRegionId.Shallows, PrototypeExpeditionRegionVisualState.Locked) &&
                   !selection.CanDepartFocusedRegion(),
                "Locked verification transition blocks departure without changing focus");
            Assert(selection.SetRegionStateForVerification(PrototypeExpeditionRegionId.Shallows, PrototypeExpeditionRegionVisualState.EquipmentMissing) &&
                   !selection.CanDepartFocusedRegion(),
                "Equipment-missing verification transition blocks departure");
            Assert(selection.SetRegionStateForVerification(PrototypeExpeditionRegionId.Shallows, PrototypeExpeditionRegionVisualState.RiskWarning) &&
                   selection.CanDepartFocusedRegion(),
                "Risk-warning verification transition remains playable and inspectable");
            Assert(selection.SetRegionStateForVerification(PrototypeExpeditionRegionId.Shallows, PrototypeExpeditionRegionVisualState.Unknown) &&
                   !selection.CanDepartFocusedRegion(),
                "Unknown verification transition blocks departure");
            Assert(selection.SetRegionStateForVerification(PrototypeExpeditionRegionId.Shallows, PrototypeExpeditionRegionVisualState.DepartureReady) &&
                   selection.CanDepartFocusedRegion(),
                "Verification transition restores the starting region to departure-ready");

            GameSession selected = new GameSession(seed);
            PrototypeExpeditionRegionProfile forest = PrototypeExpeditionRegionCatalog.Get(PrototypeExpeditionRegionId.Forest);
            PrototypeExpeditionNodeResult forestNode = forest.ResolveNode(seed, forest.WaterNodeCount);
            Assert(selected.BeginSearch(PrototypeExpeditionRegionId.Forest) &&
                   selected.SelectedRegionId == PrototypeExpeditionRegionId.Forest &&
                   selected.ActiveRegionProfileId == forest.StableId &&
                   selected.TryGather(forestNode.Resource, forestNode.Amount, forestNode.Water, forestNode.ActionId, forestNode.ResultId) == GatherResult.Added &&
                   selected.LastExpeditionResultId == forestNode.ResultId,
                "Only the selected region profile drives the active exploration node and logged result linkage");
        }

        private static void VerifyDevelopmentPlaytestLogContract()
        {
            Assert(!PrototypePlaytestEventRecorder.ProductionEnabled,
                "Editor and non-development compilation keep the production file logger disabled");
            Assert(PrototypePlaytestEventRecorder.CreateDevelopment(
                    new GameSession(),
                    delegate { return PrototypeLocalization.KoreanLocaleCode; },
                    delegate { return PrototypeInputDevice.KeyboardMouse; }) == null,
                "Non-development runtime creates no production logger or local file sink");

            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/PrototypePlaytestEventLog.cs");
            Assert(source.Contains("#if DEVELOPMENT_BUILD && !UNITY_EDITOR") &&
                   source.Contains("Application.persistentDataPath") &&
                   !source.Contains("UnityWebRequest") && !source.Contains("HttpClient") && !source.Contains("WebSocket"),
                "File I/O is development-player-only, persistentDataPath-local, and contains no network transport");

            GameSession session = new GameSession();
            string locale = PrototypeLocalization.KoreanLocaleCode;
            PrototypeInputDevice device = PrototypeInputDevice.KeyboardMouse;
            PrototypePlaytestEventRecorder recorder = PrototypePlaytestEventRecorder.CreateForVerification(
                session,
                delegate { return locale; },
                delegate { return device; });
            recorder.RecordSessionStarted();
            recorder.ObserveFacilityTarget(PrototypeCampInteractionTargetKind.Campfire, "camp.Campfire", true);
            recorder.RecordPopupOpened(PrototypeCampInteractionTargetKind.Campfire, "camp.Campfire");
            recorder.RecordPopupClosed(PrototypeCampInteractionTargetKind.Campfire, "camp.Campfire", "cancelled");

            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Salvage, 20);
            recorder.ObserveState("verification.grant");
            Assert(session.BeginSearch(), "Playtest log verification begins an expedition");
            recorder.ObserveState("verification.begin_search");
            PrototypeExpeditionRegionProfile selectedProfile = PrototypeExpeditionRegionCatalog.Get(session.SelectedRegionId.Value);
            PrototypeExpeditionNodeResult deterministicNode = selectedProfile.ResolveNode(session.RunSeed, selectedProfile.WaterNodeCount);
            Assert(session.TryGather(
                       deterministicNode.Resource,
                       deterministicNode.Amount,
                       deterministicNode.Water,
                       deterministicNode.ActionId,
                       deterministicNode.ResultId) == GatherResult.Added,
                "Playtest log verification resolves a deterministic region result");
            recorder.ObserveState(deterministicNode.ActionId);
            Assert(session.SetSwimming(true), "Playtest log verification enters swimming");
            recorder.ObserveState("verification.swim_enter");
            Assert(session.SetSwimming(false), "Playtest log verification returns to land");
            recorder.ObserveState("verification.swim_exit");
            recorder.RecordVineBarrierBlocked();
            recorder.RecordVineBarrierCleared();
            Assert(session.ReturnToCamp(false), "Playtest log verification returns to camp");
            recorder.ObserveState("verification.return");
            Assert(session.EndDay(), "Playtest log verification settles a survived day");
            recorder.ObserveState("verification.end_day");

            Assert(recorder.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.Workbench,
                    "camp.Workbench",
                    "build.workbench",
                    delegate { return session.TryBuild(StructureKind.Workbench); }),
                "Playtest log verification builds a workbench through the tracked facility boundary");
            Assert(recorder.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.Workbench,
                    "camp.Workbench",
                    "research.rope",
                    delegate { return session.TryResearch(TechKind.Rope); }) &&
                   recorder.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.Workbench,
                    "camp.Workbench",
                    "craft.rope",
                    delegate { return session.TryCraft(TechKind.Rope); }),
                "Playtest log verification tracks research and crafting");
            Assert(recorder.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.Workbench,
                    "camp.Workbench",
                    "bag.capacity_upgrade",
                    session.TryUpgradeBagCapacity),
                "Playtest log verification tracks the bag upgrade");
            Assert(!recorder.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.Workbench,
                    "camp.Workbench",
                    "bag.capacity_upgrade",
                    session.TryUpgradeBagCapacity),
                "Playtest log verification tracks a rejected repeated action without state mutation");

            locale = PrototypeLocalization.EnglishLocaleCode;
            device = PrototypeInputDevice.Gamepad;
            Assert(recorder.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.RescueSignal,
                    "camp.signal-anchor",
                    "signal.upgrade",
                    session.TryUpgradeSignal) &&
                   recorder.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.RescueSignal,
                    "camp.signal-anchor",
                    "signal.upgrade",
                    session.TryUpgradeSignal),
                "Playtest log verification tracks both rescue signal stages and the immediate result");
            recorder.ObserveFacilityTarget(PrototypeCampInteractionTargetKind.None, string.Empty, false);
            recorder.Dispose();

            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            bool sawEnglishGamepad = false;
            bool sawCampaignLinkage = false;
            int expectedSequence = 1;
            IReadOnlyList<string> lines = recorder.VerificationLines;
            Assert(lines.Count > 20, "JSONL verification produces one compact record per event");
            for (int index = 0; index < lines.Count; index += 1)
            {
                string line = lines[index];
                Assert(!string.IsNullOrWhiteSpace(line) && !line.Contains("\n") && line[0] == '{' && line[line.Length - 1] == '}',
                    "Each JSONL entry is exactly one JSON object line");
                PrototypePlaytestEventRecord record = JsonUtility.FromJson<PrototypePlaytestEventRecord>(line);
                Assert(record != null && record.sequence == expectedSequence++, "JSONL sequence is stable and monotonic");
                Assert(record.state_before != null && record.state_after != null &&
                       record.state_before.fingerprint.Length == 64 && record.state_after.fingerprint.Length == 64,
                    "Every event includes before and after SHA-256 state fingerprints");
                Assert(!line.Contains("persistentDataPath") && !line.Contains("user_name") && !line.Contains("joystick_name"),
                    "Event payload contains no local path, user name, or hardware identifier field");
                names.Add(record.event_name);
                sawEnglishGamepad |= record.locale == PrototypeLocalization.EnglishLocaleCode && record.input_device == "gamepad";
                sawCampaignLinkage |= record.run_seed == session.RunSeed &&
                                      record.region_id == selectedProfile.StableId &&
                                      record.profile_id == selectedProfile.StableId &&
                                      record.result_id == deterministicNode.ResultId;
            }

            string[] requiredNames =
            {
                PrototypePlaytestEventNames.LogStarted,
                PrototypePlaytestEventNames.LogStopped,
                PrototypePlaytestEventNames.SessionStarted,
                PrototypePlaytestEventNames.DayChanged,
                PrototypePlaytestEventNames.DaySurvived,
                PrototypePlaytestEventNames.PhaseChanged,
                PrototypePlaytestEventNames.ResourceChanged,
                PrototypePlaytestEventNames.FacilityProximityEntered,
                PrototypePlaytestEventNames.FacilityProximityExited,
                PrototypePlaytestEventNames.FacilityPopupOpened,
                PrototypePlaytestEventNames.FacilityPopupClosed,
                PrototypePlaytestEventNames.FacilityActionCompleted,
                PrototypePlaytestEventNames.FacilityActionRejected,
                PrototypePlaytestEventNames.CraftingCompleted,
                PrototypePlaytestEventNames.ResearchCompleted,
                PrototypePlaytestEventNames.BagCapacityUpgraded,
                PrototypePlaytestEventNames.SwimmingEntered,
                PrototypePlaytestEventNames.SwimmingExited,
                PrototypePlaytestEventNames.VineBarrierBlocked,
                PrototypePlaytestEventNames.VineBarrierCleared,
                PrototypePlaytestEventNames.SignalStageOneCompleted,
                PrototypePlaytestEventNames.SignalStageTwoCompleted,
                PrototypePlaytestEventNames.ExpeditionRegionSelected,
                PrototypePlaytestEventNames.ExpeditionStarted,
                PrototypePlaytestEventNames.ExpeditionResultResolved,
                PrototypePlaytestEventNames.RunCompleted
            };
            for (int index = 0; index < requiredNames.Length; index += 1)
            {
                Assert(names.Contains(requiredNames[index]), "Required stable playtest event is covered: " + requiredNames[index]);
            }
            Assert(sawEnglishGamepad, "Locale and input device are independent fields on the same event");
            Assert(sawCampaignLinkage, "Development log links seed, region, profile and deterministic result without personal data");
        }

        [MenuItem("Kim Survival/Build Windows Prototype")]
        public static void BuildWindows()
        {
            PrototypeLocalizationAssetBuilder.SyncAssets();
            SyncCompactPromptSkin();
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

        public static void BuildWindowsReleaseLogVerification()
        {
            PrototypeLocalizationAssetBuilder.SyncAssets();
            SyncCompactPromptSkin();
            if (!File.Exists(ScenePath))
            {
                CreateProject();
            }

            Directory.CreateDirectory("Builds/WindowsReleaseVerification");
            Directory.CreateDirectory(VerificationFolder);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/WindowsReleaseVerification/KimSurvivalIsland.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string text =
                "Result: " + summary.result + "\n" +
                "Output: " + options.locationPathName + "\n" +
                "Development: false\n" +
                "Size: " + summary.totalSize + " bytes\n" +
                "Duration: " + summary.totalTime + "\n" +
                "Errors: " + summary.totalErrors + "\n" +
                "Warnings: " + summary.totalWarnings + "\n";
            File.WriteAllText(Path.Combine(VerificationFolder, "windows-release-log-verification-build.txt"), text);

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows release verification build failed. " + text);
            }

            Debug.Log("[Kim Survival] Windows release log verification build succeeded: " + options.locationPathName);
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

        public static void SyncCompactPromptSkin()
        {
            Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(CompactPromptFramePath);
            if (frame == null)
            {
                throw new InvalidOperationException("Adopted compact-a frame could not be loaded: " + CompactPromptFramePath);
            }

            PrototypeCampPromptSkin skin = AssetDatabase.LoadAssetAtPath<PrototypeCampPromptSkin>(CompactPromptSkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<PrototypeCampPromptSkin>();
                skin.name = "Wave 12 Compact A Camp Prompt Skin";
                AssetDatabase.CreateAsset(skin, CompactPromptSkinPath);
            }

            skin.Configure(CompactPromptAssetId, frame);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
        }

        private static GameSession CreateSpatialRestProbe()
        {
            GameSession probe = new GameSession();
            probe.Grant(ResourceKind.Wood, 2);
            probe.Grant(ResourceKind.Stone, 1);
            probe.Grant(ResourceKind.Salvage, 1);
            Assert(probe.TryBuild(StructureKind.Campfire) && probe.TryBuild(StructureKind.RainCollector),
                "Spatial rest probe builds both day-benefit facilities");
            Assert(probe.BeginSearch() && probe.SetSwimming(true), "Spatial rest probe begins a deterministic high-cost expedition");
            probe.TickSearch(60f, true);
            Assert(probe.ReturnToCamp(false), "Spatial rest probe returns to camp for settlement");
            return probe;
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

        private static void AssertCompactPromptImport()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CompactPromptFramePath);
            TextureImporter importer = AssetImporter.GetAtPath(CompactPromptFramePath) as TextureImporter;
            TextureImporterSettings settings = new TextureImporterSettings();
            if (importer != null)
            {
                importer.ReadTextureSettings(settings);
                importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
                Assert(sourceWidth == 384 && sourceHeight == 64,
                    "Adopted compact-a preserves the 384x64 source canvas");
            }

            PrototypeCampPromptSkin skin = Resources.Load<PrototypeCampPromptSkin>("Wave12CompactPromptSkin");
            Assert(sprite != null && importer != null && importer.textureType == TextureImporterType.Sprite &&
                   importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled &&
                   importer.textureCompression == TextureImporterCompression.Uncompressed,
                "Adopted compact-a imports as one uncompressed mip-free sprite");
            Assert(settings.spriteAlignment == (int)SpriteAlignment.Center &&
                   Vector2.Distance(settings.spritePivot, new Vector2(0.5f, 0.5f)) < 0.0001f &&
                   settings.spriteBorder == new Vector4(70f, 12f, 30f, 12f),
                "Adopted compact-a keeps center pivot and L70/R30/T12/B12 9-slice border");
            Assert(skin != null && skin.AssetId == CompactPromptAssetId && skin.Frame == sprite,
                "Runtime Resources skin references only the adopted compact-a sprite");
        }

        private static void AssertExpeditionMapImport()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExpeditionMapLayoutPath);
            TextureImporter importer = AssetImporter.GetAtPath(ExpeditionMapLayoutPath) as TextureImporter;
            Assert(sprite != null && importer != null && importer.textureType == TextureImporterType.Sprite &&
                   importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled,
                "Selected-only expedition map A imports as one mip-free Unity sprite");
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Assert(sourceWidth == 1280 && sourceHeight == 800 &&
                   Mathf.Approximately(sprite.rect.width, 1280f) && Mathf.Approximately(sprite.rect.height, 800f),
                "Selected expedition map A preserves its 1280x800 source and sprite canvas");
            Assert(AssetDatabase.AssetPathToGUID(ExpeditionMapLayoutPath) == "ae09637f2b24aa14295b1f9a5b4fde1c",
                "Selected expedition map A preserves the canonical Unity GUID");
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
            PrototypeProjectBuilder.SyncCompactPromptSkin();
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
                string explorationScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-exploration-ko-1280x800.png"));
                string swimmingScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-swimming-en-1280x800.png"));
                string placementKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-placement-ko-invalid-1280x800.png"));
                string placementEnglishScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-placement-en-valid-gamepad-1280x800.png"));
                string signalKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-signal-stage1-missing-ko-1280x800.png"));
                string signalEnglishScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-signal-stage2-missing-en-1280x800.png"));
                string bagLockedKorean1280Screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-bag-locked-ko-1280x800.png"));
                string bagUpgradedEnglish1280Screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-bag-upgraded-en-1280x800.png"));
                string bagLockedKorean1920Screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-bag-locked-ko-1920x1080.png"));
                string bagUpgradedEnglish1920Screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave7-bag-upgraded-en-1920x1080.png"));
                string campFarKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave12-camp-far-ko-1280x800.png"));
                string campProximityKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave12-facility-near-ko-1280x800.png"));
                string campProximityEnglishScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave12-facility-near-en-1280x800.png"));
                string campProximityQpsLongScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave12-direct-slot-near-qps-long-1280x800.png"));
                string campWorkbenchEnglishScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave9-workbench-popup-en-1280x800.png"));
                string campCampfireKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave12-popup-open-ko-1280x800.png"));
                string moduleSlotPopupKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave11-upper-slot-popup-ko-1280x800.png"));
                string moduleUpperKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave11-module-upper-ko-1280x800.png"));
                string moduleSideEnglishScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave11-module-side-en-1280x800.png"));
                string moduleBasementQpsLongScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave11-module-basement-qps-long-1280x800.png"));
                string moduleInteriorKoreanScreenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave9-module-interior-ko-1280x800.png"));
                string result = prototype.RunAutomatedVerification(
                    explorationScreenshot,
                    swimmingScreenshot,
                    placementKoreanScreenshot,
                    placementEnglishScreenshot,
                    signalKoreanScreenshot,
                    signalEnglishScreenshot,
                    bagLockedKorean1280Screenshot,
                    bagUpgradedEnglish1280Screenshot,
                    bagLockedKorean1920Screenshot,
                    bagUpgradedEnglish1920Screenshot,
                    campFarKoreanScreenshot,
                    campProximityKoreanScreenshot,
                    campWorkbenchEnglishScreenshot,
                    campCampfireKoreanScreenshot);
                string screenshot = Path.GetFullPath(Path.Combine(VerificationFolder, "kim-survival-wave12-camp-reset-ko-1280x800.png"));
                prototype.CaptureVerificationPng(screenshot, 1280, 800);
                SessionState.SetBool(PassedKey, true);
                SessionState.SetString(MessageKey, result +
                    "\nBag locked Korean 1280x800: " + bagLockedKorean1280Screenshot +
                    "\nBag upgraded English 1280x800: " + bagUpgradedEnglish1280Screenshot +
                    "\nBag locked Korean 1920x1080: " + bagLockedKorean1920Screenshot +
                    "\nBag upgraded English 1920x1080: " + bagUpgradedEnglish1920Screenshot +
                    "\nWave 12 far camp Korean 1280x800: " + campFarKoreanScreenshot +
                    "\nWave 12 facility near Korean 1280x800: " + campProximityKoreanScreenshot +
                    "\nWave 12 facility near English 1280x800: " + campProximityEnglishScreenshot +
                    "\nWave 12 direct slot near qps-long 1280x800: " + campProximityQpsLongScreenshot +
                    "\nWave 9 workbench popup English 1280x800: " + campWorkbenchEnglishScreenshot +
                    "\nWave 12 popup-open Korean 1280x800: " + campCampfireKoreanScreenshot +
                    "\nWave 11 upper-slot popup Korean 1280x800: " + moduleSlotPopupKoreanScreenshot +
                    "\nWave 11 module upper Korean 1280x800: " + moduleUpperKoreanScreenshot +
                    "\nWave 11 module side English 1280x800: " + moduleSideEnglishScreenshot +
                    "\nWave 11 module basement qps-long 1280x800: " + moduleBasementQpsLongScreenshot +
                    "\nWave 9 module interior Korean 1280x800: " + moduleInteriorKoreanScreenshot +
                    "\nSignal stage one missing/workbench Korean screenshot: " + signalKoreanScreenshot +
                    "\nSignal stage two missing/rope English screenshot: " + signalEnglishScreenshot +
                    "\nPlacement Korean screenshot: " + placementKoreanScreenshot +
                    "\nPlacement English/gamepad screenshot: " + placementEnglishScreenshot +
                    "\nSwimming screenshot: " + swimmingScreenshot +
                    "\nExploration screenshot: " + explorationScreenshot +
                    "\nCamp screenshot: " + screenshot);
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
