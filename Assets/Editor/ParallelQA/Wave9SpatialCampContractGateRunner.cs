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
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Red-first, non-shipping Wave 9 spatial-camp contract gate.
    /// Product gaps are EXPECTED_FAIL and never masquerade as test-infrastructure
    /// failures. The same reflected UI contracts become PASS after the spatial
    /// facility-popup and module-expansion implementation is integrated.
    /// </summary>
    internal static class Wave9SpatialCampContractGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave9.Play.Running";
        private const string PlayInfraPassedKey = "ParallelQA.Wave9.Play.InfraPassed";
        private const string PlayMessageKey = "ParallelQA.Wave9.Play.Message";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool playTickAttached;
        private static double playEarliestRunTime;
        private static double playTimeoutAt;

        [Serializable]
        private sealed class ContractCheck
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
        private sealed class ContractReport
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
            public ContractCheck[] checks;
        }

        [Serializable]
        private sealed class TargetObservation
        {
            public string target;
            public float x;
            public float y;
            public int nearPromptCount;
            public string[] nearPrompts;
            public int popupCountAfterInteract;
            public string[] popupNamesAfterInteract;
            public string feedbackKey;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public bool globalCampActionsActive;
            public bool largeBagPanelActive;
            public int farPromptCount;
            public int farPopupCount;
            public TargetObservation[] targets;
            public string[] screenshots;
            public int activeTmpCount;
            public int overflowingTmpCount;
            public int outOfBoundsTmpCount;
            public float minimumRenderedTextPixels;
            public bool koreanHeaderTextOverlap;
            public bool englishHeaderTextOverlap;
            public string[] joystickNames;
            public string physicalGamepad;
        }

        [Serializable]
        private sealed class ModuleDiscovery
        {
            public string[] candidateTypes;
            public bool hasCandidateKind;
            public bool hasValidityReason;
            public bool hasConnectionSlots;
            public bool hasCost;
            public bool hasOverlapRule;
            public bool hasRequiredRouteRule;

            public string Description()
            {
                return "types=" + Join(candidateTypes) +
                       " kinds=" + hasCandidateKind +
                       " validityReason=" + hasValidityReason +
                       " connectionSlots=" + hasConnectionSlots +
                       " cost=" + hasCost +
                       " overlap=" + hasOverlapRule +
                       " requiredRoute=" + hasRequiredRouteRule;
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
                return string.IsNullOrWhiteSpace(value) ? "manual-wave9" : Sanitize(value);
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
            string prototypePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs");
            string prototypeSource = File.ReadAllText(prototypePath);

            bool legacyDashboardRemoved =
                prototypeSource.Contains("campActions.SetActive(false)") &&
                !prototypeSource.Contains("campActions.SetActive(camp");
            ProductExpected(checks, "W9-E01.no_global_camp_dashboard", "camp/base-state", "P0",
                "Normal camp does not construct or activate the legacy global campActions dashboard",
                legacyDashboardRemoved,
                "legacyCampActionsSourcePresent=" + (!legacyDashboardRemoved),
                "Open a normal camp at 1280x800 and source-audit campActions construction/activation.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            bool largeBagRemoved = prototypeSource.Contains(
                "bagPanel.SetActive(session.Phase == GamePhase.Exploring && !placing)");
            ProductExpected(checks, "W9-E02.no_large_persistent_bag_panel", "camp/base-state", "P1",
                "Normal spatial camp has no persistent large inventory panel obscuring the world",
                largeBagRemoved,
                "legacyLargeBagRectPresent=" + (!largeBagRemoved) + " bagPanelConstruction=" + prototypeSource.Contains("bagPanel = CreatePanel"),
                "Open a normal camp before placement and inspect the right-side inventory footprint.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            Type prototypeType = typeof(KimSurvivalPrototype);
            FieldInfo[] fields = prototypeType.GetFields(InstanceFlags);
            string[] promptMembers = fields.Where(field => Semantic(field.Name, "prompt", "hint", "nearby", "focus"))
                .Select(field => field.FieldType.Name + ":" + field.Name).OrderBy(value => value).ToArray();
            string[] popupMembers = fields.Where(field => Semantic(field.Name, "popup", "modal", "facilitypanel", "interactionpanel"))
                .Select(field => field.FieldType.Name + ":" + field.Name).OrderBy(value => value).ToArray();
            ProductExpected(checks, "W9-E03.context_prompt_and_facility_popup_api", "far/near/interact", "P0",
                "Runtime exposes contextual near-target prompt state and a facility-specific popup/modal state",
                promptMembers.Length > 0 && popupMembers.Length > 0,
                "promptMembers=" + Join(promptMembers) + " popupMembers=" + Join(popupMembers),
                "Merge the Wave 9 spatial interaction implementation and rerun this unchanged reflected contract.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeCampUse.cs");

            string[] facilityTokens = { "campfire", "workbench", "rain", "signal" };
            MethodInfo ownershipMethod = typeof(PrototypeCampInteractionCatalog).GetMethod(
                "OwnsAction", StaticFlags);
            PrototypeCampInteractionTargetKind[] ownedTargets =
            {
                PrototypeCampInteractionTargetKind.Campfire,
                PrototypeCampInteractionTargetKind.Workbench,
                PrototypeCampInteractionTargetKind.RainCollector,
                PrototypeCampInteractionTargetKind.RescueSignal
            };
            bool targetOwnershipApi = ownershipMethod != null && ownedTargets.All(target =>
                Enum.GetValues(typeof(PrototypeCampInteractionAction)).Cast<PrototypeCampInteractionAction>()
                    .Any(action => PrototypeCampInteractionCatalog.OwnsAction(target, action, true)));
            ProductExpected(checks, "W9-E04.target_action_ownership", "facility ownership", "P0",
                "Campfire, workbench, rain collector, and rescue signal each own their visible actions through the approached target",
                targetOwnershipApi,
                "discoverableFacilityPopupOwnership=" + targetOwnershipApi + " targets=" + string.Join(",", facilityTokens),
                "Approach each target, interact, and compare the popup identity/action inventory before confirming an action.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            Type interactionType = typeof(PrototypeCampInteraction);
            bool modalLockApi = interactionType.GetProperties(InstanceFlags)
                .Any(property => property.PropertyType == typeof(bool) && Semantic(property.Name, "modal", "movementlock", "inputlock"));
            bool modalReturnApi = prototypeType.GetMethods(InstanceFlags)
                .Any(method => Semantic(method.Name, "cancelcamppopup")) &&
                prototypeType.GetMethods(InstanceFlags)
                    .Any(method => Semantic(method.Name, "executeconfirmedpopupaction", "executeconfirmedpopuptransition"));
            ProductExpected(checks, "W9-E05.modal_lock_and_return_api", "modal", "P0",
                "Facility modal state explicitly locks camp movement and exposes confirm/cancel return paths",
                modalLockApi && modalReturnApi,
                "movementLockMember=" + modalLockApi + " confirmCancelMethod=" + modalReturnApi,
                "Open a facility popup, hold movement, then confirm and cancel in separate fresh snapshots.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");

            Infrastructure(checks, "W9-I01.proximity_model", "far/near", "P0",
                "The shared camp movement model distinguishes outside versus inside the 1.25-unit use range",
                VerifyProximityModel,
                "Construct PrototypeCampUse, measure a far point, then move both synthetic keyboard and gamepad actions into range.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampUse.cs; Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");

            Infrastructure(checks, "W9-I02.resource_atomicity_primitives", "resource atomicity", "P0",
                "Existing build/session primitives spend once on success and never on rejection or a duplicate attempt",
                VerifyAtomicBuildPrimitive,
                "Attempt a build without materials, then with materials, then repeat it while comparing all storage values.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");

            string inputPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "PrototypePlayerInput.cs");
            string inputSource = File.ReadAllText(inputPath);
            bool sharedInput = inputSource.Contains("KeyCode.E") &&
                               inputSource.Contains("KeyCode.JoystickButton2") &&
                               inputSource.Contains("PrototypePlayerActions") &&
                               inputSource.Contains("InteractPressed");
            Infrastructure(checks, "W9-I03.keyboard_gamepad_code_paths", "input", "P1",
                "Keyboard E and gamepad X feed the same shared InteractPressed action path",
                () => sharedInput ? "keyboard=KeyCode.E gamepad=JoystickButton2 shared=InteractPressed" : throw new InvalidOperationException("shared interact mapping missing"),
                "Source-audit PrototypePlayerInput and compare the produced PrototypePlayerActions.",
                "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");

            ModuleDiscovery modules = DiscoverModuleContract();
            ProductExpected(checks, "W9-M01.upper_side_basement_candidates", "module candidates", "P1",
                "Module expansion exposes upper-floor, side-room, and basement candidates",
                modules.hasCandidateKind,
                modules.Description(),
                "Open module expansion at each connection slot and enumerate candidate kinds.",
                "future Wave 9 camp module runtime and localization implementation");
            ProductExpected(checks, "W9-M02.valid_invalid_reasons", "module validity", "P1",
                "Every module candidate reports valid or a player-readable invalid reason",
                modules.hasValidityReason,
                modules.Description(),
                "Move each candidate through valid and invalid cells and capture the reason key.",
                "future Wave 9 camp module runtime and localization implementation");
            ProductExpected(checks, "W9-M03.connection_slots_and_cost", "module slots/cost", "P1",
                "Module candidates are bound to explicit connection slots and show a deterministic cost before confirmation",
                modules.hasConnectionSlots && modules.hasCost,
                modules.Description(),
                "Inspect available connection slots, open a candidate, cancel, then confirm with exact resources.",
                "future Wave 9 camp module runtime and localization implementation");
            ProductExpected(checks, "W9-M04.overlap_and_required_route", "module collision/route", "P0",
                "Module validation rejects overlap and preserves the camp entrance plus every required route",
                modules.hasOverlapRule && modules.hasRequiredRouteRule,
                modules.Description(),
                "Try overlapping an existing module and blocking the required traversal path.",
                "future Wave 9 camp module runtime and navigation implementation");

            WriteReport("wave9-edit-contracts", "Wave 9 spatial camp Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayInfraPassedKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Wave 9 Play contracts did not complete.");
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
            playTimeoutAt = EditorApplication.timeSinceStartup + 180d;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 2d;
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
            if (now < playEarliestRunTime) return;
            if (now > playTimeoutAt)
            {
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · timed out waiting for playable scene.");
                StopPlayContracts();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;

            try
            {
                DateTime started = DateTime.UtcNow;
                List<ContractCheck> checks = new List<ContractCheck>();
                PlayEvidence evidence = ExecuteSpatialPlayContracts(prototype, checks);
                WriteJson("wave9-spatial-play-evidence.json", evidence);

                Infrastructure(checks, "W9-I04.approach_first_full_regression", "legacy regressions", "P0",
                    "The current approach-first automated verification completes placement, bag, rescue, search, swimming, and land return",
                    () => RunApproachFirstRegression(prototype),
                    "Run KimSurvivalPrototype.RunAutomatedVerification through the Wave 9 Play gate; do not press distant legacy dashboard buttons.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/Editor/ParallelQA/Wave9SpatialCampContractGateRunner.cs");

                ContractReport report = WriteReport("wave9-play-contracts", "Wave 9 spatial camp Play contracts", started, checks);
                bool infrastructurePassed = report.infrastructureOverall == "PASS";
                SessionState.SetBool(PlayInfraPassedKey, infrastructurePassed);
                SessionState.SetString(PlayMessageKey,
                    "Product=" + report.productOverall + " · Infrastructure=" + report.infrastructureOverall +
                    " · PhysicalGamepad=UNVERIFIED · Evidence=" + Path.Combine(EvidenceFolder, "wave9-play-contracts.json"));
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PlayInfraPassedKey, false);
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · " + exception);
                List<ContractCheck> failure = new List<ContractCheck>();
                Infrastructure(failure, "W9-I99.play_runner", "infrastructure", "P0",
                    "Wave 9 Play runner produces parseable evidence", () => throw exception,
                    "Run the Wave 9 Play execute method outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave9SpatialCampContractGateRunner.cs");
                WriteReport("wave9-play-contracts", "Wave 9 Play infrastructure failure", DateTime.UtcNow, failure);
            }

            StopPlayContracts();
        }

        private static PlayEvidence ExecuteSpatialPlayContracts(KimSurvivalPrototype prototype, List<ContractCheck> checks)
        {
            GameSession session = prototype.Session;
            PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampPlacement placement = GetField<PrototypeCampPlacement>(prototype, "campPlacement");
            PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction campInteraction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            GameObject campActions = GetField<GameObject>(prototype, "campActions");
            GameObject bagPanel = GetField<GameObject>(prototype, "bagPanel");
            List<string> screenshots = new List<string>();

            session.Reset();
            placement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            Invoke(prototype, "RefreshAll");

            campUse.Warp(PrototypeCampUse.PlayerStartX);
            InvokeOptionalPromptRefresh(prototype);
            string[] farPrompts = ActiveContextPrompts(prototype);
            string[] farPopups = ActiveFacilityPopups(prototype, campInteraction);
            bool globalActive = campActions != null && campActions.activeInHierarchy;
            bool largeBagActive = bagPanel != null && bagPanel.activeInHierarchy && RectPixelArea(bagPanel) > 140000f;

            ProductExpected(checks, "W9-P01.normal_camp_world_first", "camp/base-state", "P0",
                "Normal camp is world-first with neither the global campActions dashboard nor a large persistent bag panel",
                !globalActive && !largeBagActive,
                "campActionsActive=" + globalActive + " largeBagActive=" + largeBagActive + " bagPixelArea=" + RectPixelArea(bagPanel).ToString("0"),
                "Launch the scene, remain at the far-left camp start, and inspect the first 1280x800 frame.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");
            ProductExpected(checks, "W9-P02.far_state_is_silent", "far", "P0",
                "Outside use range there is no contextual target prompt and no facility popup",
                farPrompts.Length == 0 && farPopups.Length == 0,
                "farPromptCount=" + farPrompts.Length + " farPopupCount=" + farPopups.Length + " prompts=" + Join(farPrompts) + " popups=" + Join(farPopups),
                "Keep the player at PlayerStartX and enumerate active contextual prompt/popup roots.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            string koNormal = "wave9-ko-normal-camp-1280x800.png";
            Capture(prototype, koNormal);
            screenshots.Add(koNormal);
            LayoutObservation koNormalLayout = MeasureLayout(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude), 1280, 800);
            bool koreanHeaderOverlap = HeaderTextOverlaps(prototype);
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            Invoke(prototype, "RefreshAll");
            string enNormal = "wave9-en-normal-camp-1280x800.png";
            Capture(prototype, enNormal);
            screenshots.Add(enNormal);
            LayoutObservation enNormalLayout = MeasureLayout(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude), 1280, 800);
            bool englishHeaderOverlap = HeaderTextOverlaps(prototype);
            ProductExpected(checks, "W9-P08.ko_en_1280_readability", "ko/en 1280x800", "P1",
                "Korean and English normal-camp headers have no rendered-text overlap or TMP overflow at 1280x800",
                !koreanHeaderOverlap && !englishHeaderOverlap && koNormalLayout.overflow == 0 && enNormalLayout.overflow == 0,
                "koHeaderOverlap=" + koreanHeaderOverlap + " enHeaderOverlap=" + englishHeaderOverlap +
                " koOverflow=" + koNormalLayout.overflow + " enOverflow=" + enNormalLayout.overflow,
                "Open both fresh normal-camp PNGs at 1:1; compare the status/resource header text and all visible panels.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

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
            Invoke(prototype, "RefreshAll");

            List<TargetObservation> observations = new List<TargetObservation>();
            observations.Add(ObserveTarget(prototype, localization, campUse, campInteraction, "Campfire", placement.GetInstalledPosition(StructureKind.Campfire)));
            observations.Add(ObserveTarget(prototype, localization, campUse, campInteraction, "Workbench", placement.GetInstalledPosition(StructureKind.Workbench)));
            observations.Add(ObserveTarget(prototype, localization, campUse, campInteraction, "RainCollector", placement.GetInstalledPosition(StructureKind.RainCollector)));
            Vector2 signalPosition = InvokeResult<Vector2>(prototype, "GetCampArtPoint",
                GetStatic<float>(typeof(KimSurvivalPrototype), "CampSignalAnchorNormalizedX"),
                GetStatic<float>(typeof(KimSurvivalPrototype), "CampSignalAnchorNormalizedY"));
            observations.Add(ObserveTarget(prototype, localization, campUse, campInteraction, "RescueSignal", signalPosition));

            bool oneNearPrompt = observations.All(item => item.nearPromptCount == 1);
            bool correctPopup = observations.All(item => item.popupCountAfterInteract == 1 &&
                item.popupNamesAfterInteract.Any(name => NameMatchesTarget(name, item.target)));
            ProductExpected(checks, "W9-P03.single_near_target_prompt", "near", "P0",
                "Each approached facility exposes exactly one contextual prompt for the nearest target",
                oneNearPrompt,
                string.Join(" | ", observations.Select(item => item.target + "=" + item.nearPromptCount + ":" + Join(item.nearPrompts))),
                "Approach each facility separately without interacting and count contextual prompt roots.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");
            ProductExpected(checks, "W9-P04.popup_only_after_interact", "interact", "P0",
                "Interact opens exactly one popup whose identity matches the approached facility",
                correctPopup,
                string.Join(" | ", observations.Select(item => item.target + "=" + item.popupCountAfterInteract + ":" + Join(item.popupNamesAfterInteract))),
                "Approach each facility, record the pre-interact prompt, invoke shared Interact, and enumerate popup roots.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            campUse.Warp(placement.GetInstalledPosition(StructureKind.Workbench));
            InvokeOptionalPromptRefresh(prototype);
            Invoke(prototype, "UseNearestCampTarget");
            Vector2 modalPosition = campUse.PlayerPosition;
            bool popupOpened = campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.Workbench;
            bool modalLock = popupOpened && campInteraction.MovementLocked;
            Invoke(prototype, "ProcessCampActions",
                new PrototypePlayerActions(1f, false, false, false, false, -1), 0.5f);
            modalLock = modalLock && campUse.PlayerPosition == modalPosition;
            Button cancelPopup = GetField<Button>(prototype, "cancelPopupButton");
            bool cancelAvailable = cancelPopup.gameObject.activeInHierarchy && cancelPopup.interactable;
            cancelPopup.onClick.Invoke();
            bool cancelReturn = !campInteraction.IsPopupOpen && campInteraction.HasProximityPrompt &&
                                campUse.PlayerPosition == modalPosition;
            Invoke(prototype, "UseNearestCampTarget");
            Button repair = GetField<Button>(prototype, "repairButton");
            bool confirmAvailable = repair.gameObject.activeInHierarchy && repair.interactable;
            repair.onClick.Invoke();
            bool confirmReturn = !campInteraction.IsPopupOpen && campInteraction.HasProximityPrompt &&
                                 campUse.PlayerPosition == modalPosition;
            bool confirmCancel = cancelAvailable && cancelReturn && confirmAvailable && confirmReturn;
            ProductExpected(checks, "W9-P05.modal_movement_lock", "modal", "P0",
                "An open facility popup locks camp movement until a modal decision is made",
                modalLock,
                "discoverableActiveMovementLock=" + modalLock,
                "Open a facility popup, hold left/right input, and compare player position before and after multiple frames.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");
            ProductExpected(checks, "W9-P06.confirm_cancel_return", "modal", "P0",
                "The facility popup exposes confirm and cancel paths that return to the same nearby world target",
                confirmCancel,
                "popupOpened=" + popupOpened + " cancelReturn=" + cancelReturn + " confirmReturn=" + confirmReturn,
                "Open the same facility twice; cancel once and confirm once, checking focus and player position after each.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            ProductExpected(checks, "W9-P07.per_target_action_ownership", "facility ownership", "P0",
                "Workbench/campfire/rain/signal actions are reachable only through their matching approached-target popup",
                correctPopup && !globalActive,
                "correctTargetPopups=" + correctPopup + " globalDashboardActive=" + globalActive,
                "Compare each popup action inventory and verify no equivalent global remote action remains visible.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            Infrastructure(checks, "W9-I05.far_action_resource_atomicity", "resource atomicity", "P0",
                "A remote workbench action is rejected without resource spend; a nearby action uses the existing atomic session action",
                () => VerifyFarNearWorkbenchAtomicity(prototype, session, placement, campUse),
                "Move outside 1.25 units, submit research, compare resources, then approach the workbench and retry.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/GameSession.cs");

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            campUse.Warp(placement.GetInstalledPosition(StructureKind.Workbench));
            InvokeOptionalPromptRefresh(prototype);
            Invoke(prototype, "UseNearestCampTarget");
            string koInteract = "wave9-ko-workbench-after-interact-1280x800.png";
            Capture(prototype, koInteract);
            screenshots.Add(koInteract);
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            Invoke(prototype, "RefreshAll");
            campUse.Warp(placement.GetInstalledPosition(StructureKind.RainCollector));
            InvokeOptionalPromptRefresh(prototype);
            Invoke(prototype, "UseNearestCampTarget");
            string enInteract = "wave9-en-rain-after-interact-1280x800.png";
            Capture(prototype, enInteract);
            screenshots.Add(enInteract);

            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);
            LayoutObservation layout = MeasureLayout(texts, 1280, 800);
            Infrastructure(checks, "W9-I06.capture_and_layout_infrastructure", "ko/en 1280x800", "P0",
                "Fresh ko/en 1280x800 captures and machine-readable TMP bounds are generated",
                () => screenshots.All(file => File.Exists(Path.Combine(EvidenceFolder, file))) && layout.active > 0
                    ? "screenshots=" + screenshots.Count + " activeTMP=" + layout.active + " overflow=" + layout.overflow + " outOfBounds=" + layout.outOfBounds + " minPx=" + layout.minimumPixels.ToString("0.0")
                    : throw new InvalidOperationException("capture or layout metrics missing"),
                "Run the Wave 9 Play contract and inspect the four fresh PNG files plus wave9-spatial-play-evidence.json.",
                "Assets/Editor/ParallelQA/Wave9SpatialCampContractGateRunner.cs");

            string[] joystickNames = (Input.GetJoystickNames() ?? Array.Empty<string>()).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            checks.Add(new ContractCheck
            {
                id = "W9-HW01.physical_gamepad",
                matrix = "input",
                status = "UNVERIFIED",
                classification = "HARDWARE_GAP",
                severity = "P1",
                expected = "A human completes approach, prompt, popup, confirm/cancel, and module placement on a physical gamepad",
                actual = joystickNames.Length == 0 ? "no non-empty joystick name exposed to Unity batch Play Mode" : "device name observed but no human actuation captured: " + Join(joystickNames),
                reproduction = "Run the Windows development build with a physical gamepad and record device name/VID/PID plus human actuation.",
                recommendedFiles = "manual release-candidate hardware evidence"
            });

            return new PlayEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                globalCampActionsActive = globalActive,
                largeBagPanelActive = largeBagActive,
                farPromptCount = farPrompts.Length,
                farPopupCount = farPopups.Length,
                targets = observations.ToArray(),
                screenshots = screenshots.ToArray(),
                activeTmpCount = layout.active,
                overflowingTmpCount = layout.overflow,
                outOfBoundsTmpCount = layout.outOfBounds,
                minimumRenderedTextPixels = layout.minimumPixels,
                koreanHeaderTextOverlap = koreanHeaderOverlap,
                englishHeaderTextOverlap = englishHeaderOverlap,
                joystickNames = joystickNames,
                physicalGamepad = "UNVERIFIED"
            };
        }

        private static TargetObservation ObserveTarget(KimSurvivalPrototype prototype, PrototypeLocalization localization, PrototypeCampUse campUse, PrototypeCampInteraction campInteraction, string target, Vector2 position)
        {
            campUse.Warp(position);
            InvokeOptionalPromptRefresh(prototype);
            string[] prompts = ActiveContextPrompts(prototype);
            Invoke(prototype, "UseNearestCampTarget");
            string[] popups = ActiveFacilityPopups(prototype, campInteraction);
            object feedback = GetField<object>(prototype, "campFeedback");
            string feedbackKey = ReadStringMember(feedback, "Key");
            TargetObservation observation = new TargetObservation
            {
                target = target,
                x = position.x,
                y = position.y,
                nearPromptCount = prompts.Length,
                nearPrompts = prompts,
                popupCountAfterInteract = popups.Length,
                popupNamesAfterInteract = popups,
                feedbackKey = feedbackKey
            };
            Invoke(prototype, "CancelCampPopup");
            return observation;
        }

        private static string RunApproachFirstRegression(KimSurvivalPrototype prototype)
        {
            string prefix = "wave9-regression-";
            string result = prototype.RunAutomatedVerification(
                Path.Combine(EvidenceFolder, prefix + "exploration-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "swimming-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "placement-ko-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "placement-en-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "signal-ko-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "signal-en-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "bag-locked-ko-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "bag-upgraded-en-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "bag-locked-ko-1920x1080.png"),
                Path.Combine(EvidenceFolder, prefix + "bag-upgraded-en-1920x1080.png"),
                Path.Combine(EvidenceFolder, prefix + "camp-far-ko-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "camp-proximity-ko-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "camp-workbench-en-1280x800.png"),
                Path.Combine(EvidenceFolder, prefix + "camp-campfire-ko-1280x800.png"));
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave9-approach-first-regression.txt"), result + Environment.NewLine, new UTF8Encoding(false));
            return result;
        }

        private static string VerifyProximityModel()
        {
            PrototypeCampUse keyboard = new PrototypeCampUse();
            PrototypeCampUse gamepad = new PrototypeCampUse();
            Vector2 target = new Vector2(-1.5f, PrototypeCampUse.PlayerFloorY);
            Require(!keyboard.IsWithinUseRange(target), "start is outside range");
            PrototypePlayerActions move = new PrototypePlayerActions(1f, false, false, false, false, -1);
            keyboard.Step(move, 0.75f);
            gamepad.Step(move, 0.75f);
            Require(keyboard.PlayerPosition == gamepad.PlayerPosition, "shared keyboard/gamepad action state converges");
            keyboard.Warp(target);
            gamepad.Warp(target);
            Require(keyboard.IsWithinUseRange(target) && gamepad.IsWithinUseRange(target), "both paths reach same use range");
            return "useRange=" + PrototypeCampUse.UseRange + " sharedPosition=" + keyboard.PlayerPosition;
        }

        private static string VerifyAtomicBuildPrimitive()
        {
            GameSession session = new GameSession();
            session.Grant(ResourceKind.Wood, -999);
            session.Grant(ResourceKind.Stone, -999);
            session.Grant(ResourceKind.Salvage, -999);
            string beforeRejected = StorageFingerprint(session);
            Require(!session.TryBuild(StructureKind.Workbench), "insufficient build rejected");
            Require(StorageFingerprint(session) == beforeRejected, "rejected build spends nothing");
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 20);
            session.Grant(ResourceKind.Salvage, 20);
            string beforeSuccess = StorageFingerprint(session);
            Require(session.TryBuild(StructureKind.Workbench), "funded build succeeds");
            string afterSuccess = StorageFingerprint(session);
            Require(afterSuccess != beforeSuccess, "successful build spends once");
            Require(!session.TryBuild(StructureKind.Workbench), "duplicate build rejected");
            Require(StorageFingerprint(session) == afterSuccess, "duplicate rejection spends nothing");
            return "beforeSuccess=" + beforeSuccess + " afterSuccess=" + afterSuccess + " duplicateStable=true";
        }

        private static string VerifyFarNearWorkbenchAtomicity(KimSurvivalPrototype prototype, GameSession session, PrototypeCampPlacement placement, PrototypeCampUse campUse)
        {
            Button research = GetField<Button>(prototype, "researchAxeButton");
            PrototypeCampInteraction campInteraction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            if (session.HasResearched(TechKind.StoneAxe))
            {
                session.Reset();
                placement.Reset();
                campUse.Reset();
                campInteraction.Reset();
                session.Grant(ResourceKind.Wood, 30);
                session.Grant(ResourceKind.Stone, 20);
                session.Grant(ResourceKind.Salvage, 30);
                Require(session.TryBuild(StructureKind.Workbench), "fresh workbench fixture");
                placement.EnsureInstalled(StructureKind.Workbench);
                Invoke(prototype, "RefreshAll");
            }
            campUse.Warp(PrototypeCampUse.PlayerStartX);
            InvokeOptionalPromptRefresh(prototype);
            string farBefore = StorageFingerprint(session);
            research.onClick.Invoke();
            Require(!session.HasResearched(TechKind.StoneAxe), "far research rejected");
            Require(StorageFingerprint(session) == farBefore, "far rejection is atomic");
            campUse.Warp(placement.GetInstalledPosition(StructureKind.Workbench));
            InvokeOptionalPromptRefresh(prototype);
            Invoke(prototype, "UseNearestCampTarget");
            Require(campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.Workbench,
                "near workbench popup opened");
            research.onClick.Invoke();
            Require(session.HasResearched(TechKind.StoneAxe), "near research succeeds");
            return "farStable=" + farBefore + " nearResearched=" + session.HasResearched(TechKind.StoneAxe);
        }

        private static ModuleDiscovery DiscoverModuleContract()
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes)
                .Where(type => type.Namespace == null || type.Namespace.IndexOf("ParallelQA", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(type => Semantic(type.FullName ?? type.Name, "campmodule", "basecampmodule", "moduleexpansion", "roomexpansion", "spatialmodule"))
                .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
            MemberInfo[] members = types.SelectMany(type => type.GetMembers(InstanceFlags | StaticFlags)).ToArray();
            string all = string.Join(" ", types.Select(type => type.FullName).Concat(members.Select(member => member.Name))).ToLowerInvariant();
            return new ModuleDiscovery
            {
                candidateTypes = types.Select(type => type.FullName).ToArray(),
                hasCandidateKind = ContainsAllGroups(all, new[] { "upper", "upstairs", "secondfloor" }, new[] { "side", "sideroom", "adjacent" }, new[] { "basement", "cellar", "underground" }),
                hasValidityReason = Semantic(all, "validity", "invalidreason", "failurekey", "placementreason"),
                hasConnectionSlots = Semantic(all, "connectionslot", "attachslot", "moduleslot", "socket"),
                hasCost = Semantic(all, "cost", "price", "requirement"),
                hasOverlapRule = Semantic(all, "overlap", "collision", "occupied"),
                hasRequiredRouteRule = Semantic(all, "requiredpath", "requiredroute", "entrancepath", "navigationroute")
            };
        }

        private struct LayoutObservation
        {
            public int active;
            public int overflow;
            public int outOfBounds;
            public float minimumPixels;
        }

        private static LayoutObservation MeasureLayout(TMP_Text[] texts, int width, int height)
        {
            LayoutObservation result = new LayoutObservation { minimumPixels = float.MaxValue };
            foreach (TMP_Text text in texts)
            {
                if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text)) continue;
                result.active += 1;
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing) result.overflow += 1;
                float pixels = text is TextMeshProUGUI
                    ? Mathf.Abs(text.textBounds.size.y) * Mathf.Sqrt((width / 1920f) * (height / 1080f))
                    : Mathf.Abs(text.textBounds.size.y * text.transform.lossyScale.y) * height / (2f * 5.625f);
                result.minimumPixels = Mathf.Min(result.minimumPixels, pixels);
                RectTransform rect = text.rectTransform;
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                TextMeshProUGUI ui = text as TextMeshProUGUI;
                Canvas ownerCanvas = ui != null ? ui.canvas : null;
                Camera camera = ownerCanvas != null && ownerCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? ownerCanvas.worldCamera : null;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                if (a.x < -0.5f || a.y < -0.5f || b.x > width + 0.5f || b.y > height + 0.5f) result.outOfBounds += 1;
            }
            if (result.active == 0) result.minimumPixels = 0f;
            return result;
        }

        private static bool HeaderTextOverlaps(KimSurvivalPrototype prototype)
        {
            TMP_Text status = GetField<TMP_Text>(prototype, "statusText");
            TMP_Text resources = GetField<TMP_Text>(prototype, "resourceText");
            return RenderedTextRect(status).Overlaps(RenderedTextRect(resources));
        }

        private static Rect RenderedTextRect(TMP_Text text)
        {
            text.ForceMeshUpdate(true, true);
            Bounds bounds = text.textBounds;
            Vector3[] points =
            {
                text.transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, 0f)),
                text.transform.TransformPoint(new Vector3(bounds.max.x, bounds.min.y, 0f)),
                text.transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, 0f)),
                text.transform.TransformPoint(new Vector3(bounds.min.x, bounds.max.y, 0f))
            };
            return Rect.MinMaxRect(
                points.Min(point => point.x),
                points.Min(point => point.y),
                points.Max(point => point.x),
                points.Max(point => point.y));
        }

        private static string[] ActiveContextPrompts(KimSurvivalPrototype prototype)
        {
            GameObject root = GetField<GameObject>(prototype, "campProximityPrompt");
            TMP_Text text = GetField<TMP_Text>(prototype, "campProximityText");
            return root != null && root.activeInHierarchy && text != null
                ? new[] { "CampProximityPrompt=" + Normalize(text.text) }
                : Array.Empty<string>();
        }

        private static string[] ActiveFacilityPopups(KimSurvivalPrototype prototype, PrototypeCampInteraction campInteraction)
        {
            GameObject root = GetField<GameObject>(prototype, "campInteractionPopup");
            return root != null && root.activeInHierarchy && campInteraction.IsPopupOpen
                ? new[] { campInteraction.OpenPopupKind.ToString() }
                : Array.Empty<string>();
        }

        private static bool HasActiveConfirmAndCancelButtons()
        {
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            bool confirm = buttons.Any(button => button.interactable && Semantic(Hierarchy(button.transform), "confirm", "apply", "build", "craft", "use"));
            bool cancel = buttons.Any(button => button.interactable && Semantic(Hierarchy(button.transform), "cancel", "close", "back"));
            return confirm && cancel;
        }

        private static bool ReadSemanticBool(object target, params string[] names)
        {
            if (target == null) return false;
            Type type = target.GetType();
            foreach (FieldInfo field in type.GetFields(InstanceFlags).Where(field => field.FieldType == typeof(bool) && Semantic(field.Name, names)))
            {
                if ((bool)field.GetValue(target)) return true;
            }
            foreach (PropertyInfo property in type.GetProperties(InstanceFlags).Where(property => property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0 && Semantic(property.Name, names)))
            {
                if ((bool)property.GetValue(target, null)) return true;
            }
            return false;
        }

        private static void InvokeOptionalPromptRefresh(object target)
        {
            MethodInfo[] methods = target.GetType().GetMethods(InstanceFlags)
                .Where(candidate => candidate.ReturnType == typeof(void) && candidate.GetParameters().Length == 0)
                .ToArray();
            MethodInfo selection = methods.FirstOrDefault(candidate => candidate.Name == "RefreshCampInteractionSelection");
            MethodInfo ui = methods.FirstOrDefault(candidate => candidate.Name == "RefreshCampInteractionUi");
            if (selection != null) InvokeMethod(target, selection, Array.Empty<object>());
            if (ui != null) InvokeMethod(target, ui, Array.Empty<object>());
            Canvas.ForceUpdateCanvases();
        }

        private static void Capture(KimSurvivalPrototype prototype, string fileName)
        {
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, fileName), 1280, 800);
        }

        private static float RectPixelArea(GameObject value)
        {
            if (value == null || !value.activeInHierarchy) return 0f;
            RectTransform rect = value.GetComponent<RectTransform>();
            if (rect == null) return 0f;
            // CaptureVerificationPng renders through a temporary target while
            // Screen.width/height can remain 1x1 in batch mode. Reference-canvas
            // area is therefore the reproducible size discriminator here.
            return Mathf.Abs(rect.rect.width * rect.rect.height);
        }

        private static void FinishPlayContracts()
        {
            bool infraPassed = SessionState.GetBool(PlayInfraPassedKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL · no Play result");
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave9-play-exit.txt"), message + Environment.NewLine, new UTF8Encoding(false));
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

        private static ContractReport WriteReport(string prefix, string title, DateTime started, List<ContractCheck> checks)
        {
            ContractReport report = new ContractReport
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
            foreach (ContractCheck check in checks)
            {
                text.AppendLine(check.status + " · " + check.classification + " · " + check.severity + " · " + check.id + " · " + Normalize(check.actual));
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, prefix + ".txt"), text.ToString(), new UTF8Encoding(false));
            return report;
        }

        private static void ProductExpected(List<ContractCheck> checks, string id, string matrix, string severity, string expected, bool passed, string actual, string reproduction, string files)
        {
            checks.Add(new ContractCheck
            {
                id = id,
                matrix = matrix,
                status = passed ? "PASS" : "EXPECTED_FAIL",
                classification = passed ? "NONE" : "PRODUCT_EXPECTED_GAP",
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = files
            });
        }

        private static void Infrastructure(List<ContractCheck> checks, string id, string matrix, string severity, string expected, Func<string> action, string reproduction, string files)
        {
            string status = "PASS";
            string actual;
            try { actual = action(); }
            catch (Exception exception)
            {
                status = "INFRA_FAIL";
                actual = exception.GetType().Name + ": " + exception.Message;
            }
            checks.Add(new ContractCheck
            {
                id = id,
                matrix = matrix,
                status = status,
                classification = status == "PASS" ? "NONE" : "TEST_INFRASTRUCTURE",
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = files
            });
        }

        private static void WriteJson(string fileName, object value)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, fileName), JsonUtility.ToJson(value, true) + Environment.NewLine, new UTF8Encoding(false));
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, InstanceFlags);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return (T)field.GetValue(target);
        }

        private static T GetStatic<T>(Type type, string name)
        {
            FieldInfo field = type.GetField(name, StaticFlags);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return (T)field.GetValue(null);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), name, arguments);
            InvokeMethod(target, method, arguments);
        }

        private static T InvokeResult<T>(object target, string name, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), name, arguments);
            return (T)InvokeMethod(target, method, arguments);
        }

        private static MethodInfo FindMethod(Type type, string name, object[] arguments)
        {
            MethodInfo method = type.GetMethods(InstanceFlags)
                .FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            return method;
        }

        private static object InvokeMethod(object target, MethodInfo method, object[] arguments)
        {
            try { return method.Invoke(target, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static string ReadStringMember(object target, string name)
        {
            if (target == null) return string.Empty;
            PropertyInfo property = target.GetType().GetProperty(name, InstanceFlags);
            if (property != null) return Convert.ToString(property.GetValue(target, null));
            FieldInfo field = target.GetType().GetField(name, InstanceFlags);
            return field == null ? string.Empty : Convert.ToString(field.GetValue(target));
        }

        private static Type[] SafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException exception) { return exception.Types.Where(type => type != null).ToArray(); }
        }

        private static bool ContainsAllGroups(string value, params string[][] groups)
        {
            return groups.All(group => group.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static bool Semantic(string value, params string[] tokens)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string compact = new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
            return tokens.Any(token => compact.Contains(new string(token.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray())));
        }

        private static bool NameMatchesTarget(string value, string target)
        {
            if (target == "RainCollector") return Semantic(value, "rain", "watercollector");
            if (target == "RescueSignal") return Semantic(value, "signal", "rescue");
            return Semantic(value, target);
        }

        private static string StorageFingerprint(GameSession session)
        {
            return string.Join(",", Enum.GetValues(typeof(ResourceKind)).Cast<ResourceKind>().Select(kind => kind + "=" + session.GetStorage(kind)));
        }

        private static string Hierarchy(Transform transform)
        {
            List<string> parts = new List<string>();
            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Join(IEnumerable<string> values)
        {
            string[] materialized = (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return materialized.Length == 0 ? "<none>" : string.Join(" | ", materialized);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
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
