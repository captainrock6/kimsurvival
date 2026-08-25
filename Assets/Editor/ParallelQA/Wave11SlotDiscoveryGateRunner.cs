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
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Wave 11 direct module-slot RED-first contract gate.
    ///
    /// The product baseline may legitimately lack direct slot targets. Missing
    /// product behavior is recorded as EXPECTED_FAIL while runner/build failures
    /// remain INFRA_FAIL. All discovery uses canonical slot IDs, so the same gate
    /// turns green when the runtime registers and owns those targets.
    /// </summary>
    public static class Wave11SlotDiscoveryGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string DirectActionKey = "ui.module.expand";
        private const string QpsLongLocaleCode = "qps-long";
        private const string PlayRunningKey = "ParallelQA.Wave11.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave11.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave11.PlayMessage";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
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
            public string overall;
            public string productOverall;
            public string infrastructureOverall;
            public int passed;
            public int expectedFailed;
            public int productFailed;
            public int infrastructureFailed;
            public int unverified;
            public ContractCheck[] checks;
        }

        [Serializable]
        private sealed class SlotObservation
        {
            public string slotId;
            public string archetype;
            public string locale;
            public bool registered;
            public bool selectedNear;
            public bool singlePrompt;
            public bool popupOpened;
            public bool singleExpandAction;
            public bool firstCandidateMatches;
            public bool cancelSnapshotRestored;
            public bool walkingPathClear;
            public string activeTargetId;
            public string firstCandidate;
            public string approachScreenshot;
            public string previewScreenshot;
            public string promptScreenRect;
            public string playerScreenRect;
            public string targetScreenRect;
            public string walkingBandScreenRect;
            public string capturePixelRect;
            public float canvasScaleFactor;
            public string layoutFailureReasons;
            public string actual;
        }

        private sealed class PromptLayoutResult
        {
            public bool Passed;
            public Rect Prompt;
            public Rect Player;
            public Rect Target;
            public Rect WalkingBand;
            public Rect CapturePixels;
            public float CanvasScaleFactor;
            public string FailureReasons;
        }

        [Serializable]
        private sealed class SlotMatrixEvidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string directActionKey = DirectActionKey;
            public string directDiscovery;
            public string auxiliaryStoragePlanning;
            public string candidatesReasonsEconomy;
            public string transactionAtomicity;
            public string snapshotPersistence;
            public string keyboardSyntheticGamepad;
            public string localization;
            public string fullRegression;
            public string layout;
            public SlotObservation[] slots;
            public string[] screenshots;
            public string[] joystickNames;
            public string physicalGamepad = "UNVERIFIED";
        }

        private sealed class DirectObservationResult
        {
            public bool Passed;
            public bool LayoutPassed;
            public string Detail;
            public string LayoutDetail;
            public string AuxiliaryDetail;
            public SlotObservation[] Slots = Array.Empty<SlotObservation>();
            public string[] Screenshots = Array.Empty<string>();
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
                return string.IsNullOrWhiteSpace(value) ? "manual-wave11" : Sanitize(value);
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
            SlotMatrixEvidence evidence = NewEvidence();

            Product(checks, "W11-E01.canonical_slot_catalog", "slot identity", "P0",
                "The start room exposes canonical upper, side, and basement slot IDs with distinct reciprocal connectors",
                VerifyCanonicalSlotCatalog,
                "Enumerate PrototypeCampModuleCatalog.All and compare StartSlotId/ReciprocalSlotId.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs");

            bool directSurfaceReady;
            string directSurfaceDetail = InspectDirectSurface(out directSurfaceReady);
            ExpectedProduct(checks, "W11-E02.direct_slot_runtime_surface", "direct discovery surface", "P0",
                "Runtime selection owns the three catalog StartSlotId values and the canonical ui.module.expand action key",
                directSurfaceReady, directSurfaceDetail,
                "Inspect the camp target registration and canonical localization table, then run the Play slot approach matrix.",
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeCampInteraction.cs; Assets/_Project/Scripts/Localization/**");

            evidence.candidatesReasonsEconomy = Product(checks, "W11-E03.candidates_reasons_economy", "candidate/geometry/economy", "P0",
                "All three candidates cycle and expose canonical geometry/economy reason IDs, workbench LOCKED, W2/D1 SHORT, and exact READY",
                VerifyCandidatesReasonsAndEconomy,
                "Cycle Upper/Side/Basement, evaluate each geometry reason, then evaluate before/after workbench with normalized resources.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");

            evidence.transactionAtomicity = Product(checks, "W11-E04.same_run_atomicity_and_duplicate", "transaction", "P0",
                "Cancel and failed submits spend nothing; Upper and Basement commit in one run for W2/D1 each; duplicate Upper spends zero",
                VerifyTransactionAtomicity,
                "Fingerprint storage before/after cancel, invalid, short, Upper success, duplicate Upper, and Basement success.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs; Assets/_Project/Scripts/Runtime/GameSession.cs");

            evidence.keyboardSyntheticGamepad = Product(checks, "W11-E05.keyboard_synthetic_gamepad", "input parity", "P1",
                "Keyboard/mouse and synthetic gamepad map to equivalent interact, cycle, confirm, and cancel semantics",
                VerifyInputParity,
                "Feed equivalent raw device states through PrototypePlayerActions and PrototypeCampModulePreviewActions.",
                "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");

            bool localeReady;
            evidence.localization = InspectLocalizationContract(out localeReady);
            ExpectedProduct(checks, "W11-E06.ko_en_qps_direct_action", "localization", "P1",
                "ui.module.expand and module names/reasons have non-fallback ko/en/qps-long values with the same canonical placeholders",
                localeReady, evidence.localization,
                "Load the TSV/table rows for ui.module.expand and the module reason IDs in all three locales.",
                "Assets/_Project/Scripts/Localization/**");

            evidence.snapshotPersistence = Product(checks, "W11-E07.module_snapshot_v2_v1_atomic_reset", "persistence", "P0",
                "v2 captures and restores Upper+Basement stable identities; v1 singular saves migrate; invalid restore is atomic; Reset returns new-game state",
                VerifyModuleSnapshotPersistence,
                "Commit Upper+Basement, capture/restore v2, restore a v1 singular fixture, reject a corrupted room identity, then Reset.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs");

            Product(checks, "W11-E08.multi_room_placement_use_snapshots", "camp-space persistence", "P0",
                "Placement v1 preserves Upper+Basement facility room/zone IDs atomically and camp-use v1 preserves the active room and day benefits",
                VerifyCampSpaceModelSnapshots,
                "Run the placement contract probe, then JSON roundtrip a Basement camp-use state and reject an invalid room atomically.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampPlacement.cs; Assets/_Project/Scripts/Runtime/PrototypeCampUse.cs");

            WriteJson("wave11-slot-edit-evidence.json", evidence);
            WriteReport("wave11-slot-edit-contracts", "Wave 11 direct slot Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Wave 11 Play contracts did not complete.");
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
            playTimeoutAt = EditorApplication.timeSinceStartup + 300d;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRunningKey, false))
            {
                return;
            }
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 2d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 300d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishPlayContracts();
            }
        }

        private static void PlayTick()
        {
            if (!SessionState.GetBool(PlayRunningKey, false) || !EditorApplication.isPlaying)
            {
                return;
            }
            double now = EditorApplication.timeSinceStartup;
            if (now < playEarliestRunTime)
            {
                return;
            }
            if (now > playTimeoutAt)
            {
                WritePlayInfrastructureFailure(new TimeoutException("Timed out waiting for the Wave 11 playable scene."));
                StopPlayContracts();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null)
            {
                return;
            }

            DateTime started = DateTime.UtcNow;
            List<ContractCheck> checks = new List<ContractCheck>();
            SlotMatrixEvidence evidence = NewEvidence();
            try
            {
                DirectObservationResult direct = ObserveDirectSlots(prototype);
                evidence.directDiscovery = direct.Detail;
                evidence.layout = direct.LayoutDetail;
                evidence.auxiliaryStoragePlanning = direct.AuxiliaryDetail;
                evidence.slots = direct.Slots;
                evidence.screenshots = direct.Screenshots;

                ExpectedProduct(checks, "W11-P01.direct_slot_discovery_popup_and_cancel", "direct slot flow", "P0",
                    "Far shows zero direct prompts; each slot is an independent near target with one prompt, a one-action ui.module.expand popup, matching first candidate, and exact cancel snapshot restoration",
                    direct.Passed, direct.Detail,
                    "At room.start, move far away, then approach slot.start.upper/side/basement individually; open, preview, and cancel each.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeCampInteraction.cs");

                ExpectedProduct(checks, "W11-P02.direct_slot_1280_layout", "1280x800 layout", "P1",
                    "Each direct approach and preview capture is 1280x800, on-screen, and does not cover Kim, its slot, or the required walking band",
                    direct.LayoutPassed, direct.LayoutDetail,
                    "Open the six direct-slot PNGs at 1:1 and compare prompt/popup bounds with the player, slot, and lower walking band.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                Product(checks, "W11-P03.storage_planning_auxiliary_regression", "auxiliary path", "P0",
                    "storage.planning still opens module preview, cycles all candidates, and cancels neutrally, but is not counted as direct discovery",
                    () => RequireDetail(!string.IsNullOrWhiteSpace(direct.AuxiliaryDetail) && direct.AuxiliaryDetail.StartsWith("PASS", StringComparison.Ordinal), direct.AuxiliaryDetail),
                    "Use the storage.planning popup separately and cycle Upper/Side/Basement before cancel.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                evidence.fullRegression = Product(checks, "W11-P04.full_survival_regression", "full playable regression", "P0",
                    "Prompt, placement, bag, signal, search, swim, land return, same-run Upper+Basement traversal/facility use/save restore, and natural three-day survival/rescue regression remains PASS",
                    () => RunFullRegression(prototype),
                    "Run KimSurvivalPrototype.RunAutomatedVerification from a fresh Play scene.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                string[] joystickNames = Input.GetJoystickNames().Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
                evidence.joystickNames = joystickNames;
                Unverified(checks, "W11-HW01.physical_gamepad", "input hardware", "P1",
                    "A human approaches all three slots, previews/cancels, and commits with a physical gamepad",
                    joystickNames.Length == 0
                        ? "no non-empty joystick name exposed to Unity batch Play Mode"
                        : "device name detected, but no human actuation evidence was captured",
                    "Run the Windows development build on hardware and record device name/VID/PID plus human actuation.",
                    "manual release-candidate hardware evidence");

                WriteJson("wave11-slot-play-evidence.json", evidence);
                ContractReport report = WriteReport("wave11-slot-play-contracts", "Wave 11 direct slot Play contracts", started, checks);
                bool runnerPassed = report.infrastructureOverall == "PASS" && report.productFailed == 0;
                SessionState.SetBool(PlayExitPassKey, runnerPassed);
                SessionState.SetString(PlayMessageKey,
                    "Product=" + report.productOverall + " · Infrastructure=" + report.infrastructureOverall +
                    " · PhysicalGamepad=UNVERIFIED · Evidence=" + Path.Combine(EvidenceFolder, "wave11-slot-play-contracts.json"));
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static DirectObservationResult ObserveDirectSlots(KimSurvivalPrototype prototype)
        {
            PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
            PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
            PrototypeCampInteraction interaction = GetField<PrototypeCampInteraction>(prototype, "campInteraction");
            PrototypeCampModuleExpansion expansion = GetField<PrototypeCampModuleExpansion>(prototype, "campModuleExpansion");
            List<PrototypeCampInteractionTarget> targets = GetField<List<PrototypeCampInteractionTarget>>(prototype, "campInteractionTargets");
            GameObject proximity = GetField<GameObject>(prototype, "campProximityPrompt");
            GameObject popup = GetField<GameObject>(prototype, "campInteractionPopup");
            Button moduleButton = GetField<Button>(prototype, "modulePreviewButton");
            Button cancelButton = GetField<Button>(prototype, "cancelPopupButton");
            List<Button> popupButtons = GetField<List<Button>>(prototype, "campPopupButtons");
            Camera camera = GetField<Camera>(prototype, "worldCamera");

            Invoke(prototype, "RefreshAll");
            campUse.Warp(new Vector2(-10f, PrototypeCampPlacement.FloorY));
            Invoke(prototype, "RefreshAll");
            bool farPass = !interaction.HasProximityPrompt && !proximity.activeSelf;

            List<SlotObservation> observations = new List<SlotObservation>();
            List<string> screenshots = new List<string>();
            string[] locales = { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode, QpsLongLocaleCode };
            CampModuleDefinition[] definitions = PrototypeCampModuleCatalog.All.ToArray();

            for (int index = 0; index < definitions.Length; index += 1)
            {
                CampModuleDefinition definition = definitions[index];
                string locale = locales[index];
                if (locale == QpsLongLocaleCode)
                {
                    localization.SetQaLocale(QpsLongLocaleCode);
                }
                else
                {
                    localization.SetLocale(locale, false);
                }
                Invoke(prototype, "RefreshAll");

                PrototypeCampInteractionTarget target = targets.FirstOrDefault(item => item.Id == definition.StartSlotId);
                bool registered = !string.IsNullOrWhiteSpace(target.Id);
                Vector2 expectedPosition = registered
                    ? target.Position
                    : new Vector2(definition.StartConnectorDisplayX, PrototypeCampPlacement.FloorY);
                campUse.Warp(expectedPosition);
                Invoke(prototype, "RefreshAll");
                string activeId = ActiveTargetId(interaction);
                bool selectedNear = registered && activeId == definition.StartSlotId;
                bool singlePrompt = selectedNear && interaction.HasProximityPrompt && proximity.activeSelf && !popup.activeSelf;
                PromptLayoutResult promptLayout = MeasurePromptLayout(prototype, proximity, camera, target.Position);
                bool walkingClear = singlePrompt && promptLayout.Passed;

                string approachName = "wave11-slot-" + definition.Archetype.ToString().ToLowerInvariant() + "-" + locale + "-approach-1280x800.png";
                prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, approachName), 1280, 800);
                screenshots.Add(approachName);

                bool popupOpened = false;
                bool singleAction = false;
                bool firstCandidate = false;
                bool cancelRestored = false;
                string firstCandidateName = "not observed";
                string previewName = string.Empty;

                if (selectedNear)
                {
                    Invoke(prototype, "UseNearestCampTarget");
                    popupOpened = interaction.IsPopupOpen && popup.activeSelf && !proximity.activeSelf;
                    List<Button> visibleActions = popupButtons
                        .Where(button => button != null && button != cancelButton && button.gameObject.activeInHierarchy && button.interactable)
                        .ToList();
                    string expectedLabel = localization.Format(DirectActionKey);
                    TMP_Text buttonLabel = moduleButton == null ? null : moduleButton.GetComponentInChildren<TMP_Text>();
                    singleAction = popupOpened && visibleActions.Count == 1 && visibleActions[0] == moduleButton &&
                                   buttonLabel != null && !IsMissingLocalization(expectedLabel) && buttonLabel.text == expectedLabel;

                    Vector2 snapshotPosition = campUse.PlayerPosition;
                    float snapshotFacing = campUse.FacingDirection;
                    string snapshotRoom = campUse.CurrentRoomId;
                    if (moduleButton != null && moduleButton.gameObject.activeInHierarchy && moduleButton.interactable)
                    {
                        moduleButton.onClick.Invoke();
                    }
                    firstCandidateName = expansion.SelectedArchetype.ToString();
                    firstCandidate = expansion.IsPreviewActive && expansion.SelectedArchetype == definition.Archetype;
                    if (expansion.IsPreviewActive)
                    {
                        previewName = "wave11-slot-" + definition.Archetype.ToString().ToLowerInvariant() + "-" + locale + "-preview-1280x800.png";
                        prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, previewName), 1280, 800);
                        screenshots.Add(previewName);
                        CampModuleArchetype selectedBeforeCancel = expansion.SelectedArchetype;
                        Invoke(prototype, "CancelCampModulePreview", true);
                        cancelRestored = campUse.PlayerPosition == snapshotPosition &&
                                         Mathf.Approximately(campUse.FacingDirection, snapshotFacing) &&
                                         campUse.CurrentRoomId == snapshotRoom &&
                                         expansion.SelectedArchetype == selectedBeforeCancel &&
                                         interaction.IsPopupOpen && ActiveTargetId(interaction) == definition.StartSlotId;
                    }
                    if (interaction.IsPopupOpen)
                    {
                        Invoke(prototype, "CancelCampPopup");
                    }
                }

                observations.Add(new SlotObservation
                {
                    slotId = definition.StartSlotId,
                    archetype = definition.Archetype.ToString(),
                    locale = locale,
                    registered = registered,
                    selectedNear = selectedNear,
                    singlePrompt = singlePrompt,
                    popupOpened = popupOpened,
                    singleExpandAction = singleAction,
                    firstCandidateMatches = firstCandidate,
                    cancelSnapshotRestored = cancelRestored,
                    walkingPathClear = walkingClear,
                    activeTargetId = activeId,
                    firstCandidate = firstCandidateName,
                    approachScreenshot = approachName,
                    previewScreenshot = previewName,
                    promptScreenRect = FormatRect(promptLayout.Prompt),
                    playerScreenRect = FormatRect(promptLayout.Player),
                    targetScreenRect = FormatRect(promptLayout.Target),
                    walkingBandScreenRect = FormatRect(promptLayout.WalkingBand),
                    capturePixelRect = FormatRect(promptLayout.CapturePixels),
                    canvasScaleFactor = promptLayout.CanvasScaleFactor,
                    layoutFailureReasons = promptLayout.FailureReasons,
                    actual = registered
                        ? "active=" + activeId + ", prompt=" + singlePrompt + ", popup=" + popupOpened + ", action=" + singleAction + ", first=" + firstCandidateName + ", cancel=" + cancelRestored
                        : "canonical catalog slot exists, but no independent proximity target was registered"
                });
            }

            string auxiliary = VerifyAuxiliaryStoragePlanning(prototype, localization, campUse, interaction, expansion, screenshots);
            bool directPass = farPass && observations.Count == 3 && observations.All(item =>
                item.registered && item.selectedNear && item.singlePrompt && item.popupOpened &&
                item.singleExpandAction && item.firstCandidateMatches && item.cancelSnapshotRestored);
            bool layoutPass = directPass && observations.All(item => item.walkingPathClear) &&
                              observations.All(item => VerifyPng(item.approachScreenshot) && VerifyPng(item.previewScreenshot));
            string detail = "farZero=" + farPass + "; registered=" + observations.Count(item => item.registered) + "/3; near=" +
                            observations.Count(item => item.selectedNear && item.singlePrompt) + "/3; popup/action/first/cancel=" +
                            observations.Count(item => item.popupOpened && item.singleExpandAction && item.firstCandidateMatches && item.cancelSnapshotRestored) + "/3; " +
                            string.Join(" | ", observations.Select(item => item.slotId + "{" + item.actual + "}"));
            string layout = "directScreens=" + screenshots.Count(name => name.Contains("wave11-slot-")) + "/6; clear=" +
                            observations.Count(item => item.walkingPathClear) + "/3; " +
                            string.Join(" | ", observations.Select(item => item.slotId + "{prompt=" + item.promptScreenRect +
                                ", player=" + item.playerScreenRect + ", target=" + item.targetScreenRect +
                                ", walking=" + item.walkingBandScreenRect + ", capture=" + item.capturePixelRect +
                                ", scale=" + item.canvasScaleFactor.ToString("0.###") +
                                ", failures=" + item.layoutFailureReasons + "}"));
            return new DirectObservationResult
            {
                Passed = directPass,
                LayoutPassed = layoutPass,
                Detail = detail,
                LayoutDetail = layout,
                AuxiliaryDetail = auxiliary,
                Slots = observations.ToArray(),
                Screenshots = screenshots.ToArray()
            };
        }

        private static string VerifyAuxiliaryStoragePlanning(
            KimSurvivalPrototype prototype,
            PrototypeLocalization localization,
            PrototypeCampUse campUse,
            PrototypeCampInteraction interaction,
            PrototypeCampModuleExpansion expansion,
            List<string> screenshots)
        {
            if (interaction.IsPopupOpen)
            {
                Invoke(prototype, "CancelCampPopup");
            }
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            Invoke(prototype, "RefreshAll");
            List<PrototypeCampInteractionTarget> targets = GetField<List<PrototypeCampInteractionTarget>>(prototype, "campInteractionTargets");
            PrototypeCampInteractionTarget storage = targets.FirstOrDefault(item => item.Kind == PrototypeCampInteractionTargetKind.StoragePlanning);
            Require(!string.IsNullOrWhiteSpace(storage.Id), "storage.planning target remains registered");
            campUse.Warp(storage.Position);
            Invoke(prototype, "RefreshAll");
            Require(interaction.ActiveTargetKind == PrototypeCampInteractionTargetKind.StoragePlanning, "storage.planning selected");
            Invoke(prototype, "UseNearestCampTarget");
            Button moduleButton = GetField<Button>(prototype, "modulePreviewButton");
            Require(moduleButton.gameObject.activeInHierarchy && moduleButton.interactable, "storage.planning owns auxiliary module action");
            CampModuleReturnSnapshot snapshot = new CampModuleReturnSnapshot(campUse.PlayerPosition, campUse.FacingDirection, campUse.CurrentRoomId);
            moduleButton.onClick.Invoke();
            Require(expansion.IsPreviewActive && expansion.SelectedArchetype == CampModuleArchetype.Upper, "aux starts Upper");
            foreach (CampModuleArchetype expected in new[] { CampModuleArchetype.Upper, CampModuleArchetype.Side, CampModuleArchetype.Basement })
            {
                while (expansion.SelectedArchetype != expected)
                {
                    expansion.Cycle(1);
                }
                string locale = expected == CampModuleArchetype.Upper ? "ko" : expected == CampModuleArchetype.Side ? "en" : QpsLongLocaleCode;
                if (locale == QpsLongLocaleCode) { localization.SetQaLocale(); } else { localization.SetLocale(locale, false); }
                Invoke(prototype, "RefreshAll");
                string fileName = "wave11-storage-planning-aux-" + expected.ToString().ToLowerInvariant() + "-" + locale + "-preview-1280x800.png";
                prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, fileName), 1280, 800);
                screenshots.Add(fileName);
            }
            Require(expansion.HasSeenAllCandidates, "aux cycles all candidates");
            Invoke(prototype, "CancelCampModulePreview", true);
            Require(campUse.PlayerPosition == snapshot.Position && campUse.CurrentRoomId == snapshot.RoomId &&
                    interaction.OpenPopupKind == PrototypeCampInteractionTargetKind.StoragePlanning,
                "aux cancel restores storage.planning popup snapshot");
            Invoke(prototype, "CancelCampPopup");
            return "PASS · storage.planning retained; three candidates cycled; cancel restored; explicitly excluded from direct-slot numerator";
        }

        private static string RunFullRegression(KimSurvivalPrototype prototype)
        {
            string prefix = "wave11-regression-";
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
            Require(result.StartsWith("PASS", StringComparison.Ordinal), "full regression pass marker");
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave11-full-regression.txt"), result + Environment.NewLine, Utf8NoBom);
            return result;
        }

        private static string VerifyCanonicalSlotCatalog()
        {
            string[] expectedRooms = { "room.upper.standard", "room.side.standard", "room.basement.standard" };
            string[] expectedStartSlots = { "slot.start.upper", "slot.start.side", "slot.start.basement" };
            string[] expectedReciprocalSlots = { "slot.upper.down", "slot.side.left", "slot.basement.up" };
            CampModuleConnectorKind[] expectedConnectors =
            {
                CampModuleConnectorKind.Ladder,
                CampModuleConnectorKind.Door,
                CampModuleConnectorKind.Ladder
            };
            CampModuleDefinition[] definitions = PrototypeCampModuleCatalog.All.ToArray();
            Require(definitions.Length == 3, "three definitions");
            Require(PrototypeCampModuleCatalog.StartRoomId == "room.start", "canonical start room ID");
            Require(definitions.Select(item => item.RoomId).SequenceEqual(expectedRooms), "canonical room IDs and order");
            Require(definitions.Select(item => item.StartSlotId).SequenceEqual(expectedStartSlots), "canonical start connector IDs and order");
            Require(definitions.Select(item => item.ReciprocalSlotId).SequenceEqual(expectedReciprocalSlots), "canonical reciprocal connector IDs and order");
            Require(definitions.Select(item => item.ConnectorKind).SequenceEqual(expectedConnectors), "canonical connector kinds and order");
            Require(definitions.All(item => item.StartSlotId != item.ReciprocalSlotId), "distinct reciprocal IDs");
            return string.Join(", ", definitions.Select(item => item.RoomId + ":" + item.StartSlotId + "<->" + item.ReciprocalSlotId));
        }

        private static string InspectDirectSurface(out bool passed)
        {
            string runtimePath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs");
            string tsvPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            string runtime = File.ReadAllText(runtimePath);
            string tsv = File.ReadAllText(tsvPath);
            bool catalogDrivenRegistration = runtime.Contains("StartSlotId") && runtime.Contains("campInteractionTargets.Add");
            bool directKey = tsv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(line => line.StartsWith(DirectActionKey + "\t", StringComparison.Ordinal));
            passed = catalogDrivenRegistration && directKey;
            return "catalogDrivenRegistration=" + catalogDrivenRegistration + "; canonicalActionKey=" + directKey +
                   "; Play behavior remains authoritative";
        }

        private static string VerifyCandidatesReasonsAndEconomy()
        {
            string[] reasonKeys =
            {
                "module.geometry.valid", "module.geometry.noconnectionslot", "module.geometry.overlap",
                "module.geometry.terrainblocked", "module.geometry.pathblocked", "module.economy.locked",
                "module.economy.short", "module.economy.ready", "module.economy.prototypelimit"
            };
            string tsv = File.ReadAllText(Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv"));
            Require(reasonKeys.All(key => tsv.Contains(key + "\t")), "all canonical reason IDs exist");

            PrototypeCampModuleExpansion expansion = NewExpansion();
            Require(expansion.BeginPreview(DefaultSnapshot()), "begin preview");
            CampModuleArchetype[] expected = { CampModuleArchetype.Upper, CampModuleArchetype.Side, CampModuleArchetype.Basement };
            foreach (CampModuleArchetype archetype in expected)
            {
                while (expansion.SelectedArchetype != archetype) { expansion.Cycle(1); }
                CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(archetype);
                CampModuleValidationContext valid = new CampModuleValidationContext();
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, valid) == CampModuleGeometryStatus.Valid, archetype + " valid");
                CampModuleValidationContext missing = valid.Clone(); missing.HasMatchingConnectionSlot = false;
                CampModuleValidationContext overlap = valid.Clone(); overlap.OccupiedRoomBounds.Add(definition.Bounds);
                CampModuleValidationContext terrain = valid.Clone(); terrain.TerrainAllowsCandidate = false;
                CampModuleValidationContext path = valid.Clone(); path.RequiredPathClear = false;
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, missing) == CampModuleGeometryStatus.NoConnectionSlot, archetype + " no-slot");
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, overlap) == CampModuleGeometryStatus.Overlap, archetype + " overlap");
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, terrain) == CampModuleGeometryStatus.TerrainBlocked, archetype + " terrain");
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, path) == CampModuleGeometryStatus.PathBlocked, archetype + " path");
            }
            Require(expansion.HasSeenAllCandidates, "three candidate cycle");

            PrototypeCampModuleExpansion economy = NewExpansion();
            economy.BeginPreview(DefaultSnapshot());
            GameSession locked = new GameSession();
            SetStorage(locked, ResourceKind.Wood, 2); SetStorage(locked, ResourceKind.Salvage, 1);
            Require(economy.Evaluate(locked, new CampModuleValidationContext()).Economy == CampModuleEconomyStatus.Locked, "LOCKED before workbench");
            GameSession shortSession = NewWorkbenchSession(1, 1);
            Require(economy.Evaluate(shortSession, new CampModuleValidationContext()).Economy == CampModuleEconomyStatus.Short, "SHORT at W1/D1");
            GameSession ready = NewWorkbenchSession(2, 1);
            Require(economy.Evaluate(ready, new CampModuleValidationContext()).Economy == CampModuleEconomyStatus.Ready, "READY at W2/D1");
            return "cycle=Upper>Side>Basement; reasonIds=9; economy=LOCKED>SHORT(W1/D1)>READY(W2/D1)";
        }

        private static string VerifyTransactionAtomicity()
        {
            PrototypeCampModuleExpansion expansion = NewExpansion();
            GameSession session = NewWorkbenchSession(2, 1);
            string initial = StorageFingerprint(session);
            expansion.BeginPreview(DefaultSnapshot());
            expansion.CancelPreview();
            Require(StorageFingerprint(session) == initial, "cancel neutral");

            expansion.BeginPreview(DefaultSnapshot());
            CampModuleValidationContext invalid = new CampModuleValidationContext { RequiredPathClear = false };
            Require(expansion.TryCommit(session, invalid) == CampModuleCommitStatus.InvalidGeometry && StorageFingerprint(session) == initial, "invalid neutral");
            expansion.CancelPreview();

            SetStorage(session, ResourceKind.Wood, 1);
            string shortBefore = StorageFingerprint(session);
            expansion.BeginPreview(DefaultSnapshot());
            Require(expansion.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.Short && StorageFingerprint(session) == shortBefore, "short neutral");
            expansion.CancelPreview();

            SetStorage(session, ResourceKind.Wood, 4); SetStorage(session, ResourceKind.Salvage, 2);
            Require(expansion.BeginPreview(DefaultSnapshot(), CampModuleArchetype.Upper), "begin Upper");
            Require(expansion.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.Succeeded, "Upper commit succeeds");
            Require(session.GetStorage(ResourceKind.Wood) == 2 && session.GetStorage(ResourceKind.Salvage) == 1 &&
                    expansion.CommittedModuleCount == 1 && expansion.IsCommitted(CampModuleArchetype.Upper),
                "Upper spends W2/D1 exactly once");
            string afterUpper = StorageFingerprint(session);
            Require(expansion.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.NotPreviewing &&
                    StorageFingerprint(session) == afterUpper,
                "duplicate submit after Upper is neutral");

            Require(expansion.BeginPreview(DefaultSnapshot(), CampModuleArchetype.Upper), "begin duplicate Upper");
            Require(expansion.Evaluate(session, new CampModuleValidationContext()).Economy == CampModuleEconomyStatus.PrototypeLimit &&
                    expansion.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.PrototypeLimit &&
                    StorageFingerprint(session) == afterUpper && expansion.CommittedModuleCount == 1,
                "duplicate Upper spends zero and adds no room");
            expansion.CancelPreview();

            Require(expansion.BeginPreview(DefaultSnapshot(), CampModuleArchetype.Basement), "begin Basement");
            Require(expansion.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.Succeeded,
                "Basement commits in the same run");
            Require(session.GetStorage(ResourceKind.Wood) == 0 && session.GetStorage(ResourceKind.Salvage) == 0 &&
                    expansion.CommittedModuleCount == 2 && expansion.HasUpperAndBasementCommitted,
                "Upper+Basement spend W4/D2 total and coexist");
            return "cancel/invalid/short neutral; Upper+Basement committed same-run; duplicate Upper delta W0/D0";
        }

        private static string VerifyModuleSnapshotPersistence()
        {
            PrototypeCampModuleExpansion source = NewExpansion();
            GameSession session = NewWorkbenchSession(4, 2);
            Require(source.BeginPreview(DefaultSnapshot(), CampModuleArchetype.Upper) &&
                    source.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.Succeeded,
                "v2 fixture Upper commit");
            Require(source.BeginPreview(DefaultSnapshot(), CampModuleArchetype.Basement) &&
                    source.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.Succeeded,
                "v2 fixture Basement commit");

            PrototypeCampModuleExpansionSnapshot v2 = source.CaptureSnapshot();
            Require(v2.SchemaVersion == PrototypeCampModuleExpansionSnapshot.CurrentSchemaVersion &&
                    v2.SchemaVersion == 2 && v2.HasCommittedModule && v2.CommittedRooms.Length == 2,
                "v2 captures two committed rooms");
            string v2Fingerprint = ModuleSnapshotFingerprint(v2);
            PrototypeCampModuleExpansion restored = NewExpansion();
            Require(restored.RestoreSnapshot(v2) && ModuleSnapshotFingerprint(restored.CaptureSnapshot()) == v2Fingerprint &&
                    restored.HasUpperAndBasementCommitted,
                "v2 restores Upper+Basement exactly");
            Require(restored.IsRoomCommitted("room.upper.standard") && restored.IsRoomCommitted("room.basement.standard") &&
                    restored.IsConnectorCommitted("slot.start.upper") && restored.IsConnectorCommitted("slot.upper.down") &&
                    restored.IsConnectorCommitted("slot.start.basement") && restored.IsConnectorCommitted("slot.basement.up"),
                "v2 restores stable room and connector IDs");

            PrototypeCampModuleExpansionSnapshot legacyV1 = new PrototypeCampModuleExpansionSnapshot
            {
                SchemaVersion = 1,
                HasCommittedModule = true,
                CommittedArchetype = CampModuleArchetype.Basement,
                CommittedRoomId = "room.basement.standard",
                CommittedRooms = Array.Empty<CampModuleCommittedRoomSnapshot>()
            };
            PrototypeCampModuleExpansion migrated = NewExpansion();
            Require(migrated.RestoreSnapshot(legacyV1) && migrated.CommittedModuleCount == 1 &&
                    migrated.IsCommitted(CampModuleArchetype.Basement) &&
                    migrated.IsRoomCommitted("room.basement.standard") &&
                    migrated.IsConnectorCommitted("slot.start.basement") &&
                    migrated.IsConnectorCommitted("slot.basement.up"),
                "v1 singular save migrates to canonical Basement room");

            string beforeFailedRestore = ModuleSnapshotFingerprint(restored.CaptureSnapshot());
            PrototypeCampModuleExpansionSnapshot corrupted = v2.Clone();
            corrupted.CommittedRooms[1].RoomId = "room.basement.corrupted";
            Require(!restored.RestoreSnapshot(corrupted) &&
                    ModuleSnapshotFingerprint(restored.CaptureSnapshot()) == beforeFailedRestore,
                "failed restore preserves the prior committed state atomically");

            restored.Reset();
            PrototypeCampModuleExpansionSnapshot reset = restored.CaptureSnapshot();
            Require(!restored.HasCommittedModule && restored.CommittedModuleCount == 0 &&
                    !restored.HasUpperAndBasementCommitted && restored.CommittedRooms.Count == 0 &&
                    !restored.IsPreviewActive && restored.TransactionGuard == CampModuleTransactionGuard.Idle &&
                    restored.SelectedArchetype == CampModuleArchetype.Upper &&
                    !restored.HasSeen(CampModuleArchetype.Upper) && !restored.HasSeen(CampModuleArchetype.Side) &&
                    !restored.HasSeen(CampModuleArchetype.Basement) &&
                    reset.SchemaVersion == 2 && !reset.HasCommittedModule && reset.CommittedRooms.Length == 0,
                "Reset restores a clean new-game module state");
            return "v2=Upper+Basement exact; v1=Basement migrated; failedRestore=atomic; reset=clean";
        }

        private static string VerifyCampSpaceModelSnapshots()
        {
            Require(PrototypeCampPlacement.RunSnapshotContractProbe(out string placementDetail), placementDetail);

            PrototypeCampUse sourceUse = new PrototypeCampUse();
            sourceUse.EnterRoom("room.basement.standard", -2f);
            Require(sourceUse.TryPrepareDayBenefit(StructureKind.Campfire, sourceUse.PlayerPosition),
                "Basement campfire day benefit fixture");
            PrototypeCampUseSnapshot captured = sourceUse.CaptureSnapshot();
            string capturedJson = JsonUtility.ToJson(captured);
            PrototypeCampUse restoredUse = new PrototypeCampUse();
            Require(restoredUse.RestoreSnapshot(JsonUtility.FromJson<PrototypeCampUseSnapshot>(capturedJson)) &&
                    restoredUse.CurrentRoomId == "room.basement.standard" &&
                    restoredUse.PlayerPosition == sourceUse.PlayerPosition &&
                    restoredUse.IsDayBenefitPrepared(StructureKind.Campfire),
                "camp-use v1 JSON roundtrip preserves room, position, and benefit");

            PrototypeCampUseSnapshot invalid = captured.Clone();
            invalid.StableRoomId = "room.invalid";
            string beforeRejectedRestore = JsonUtility.ToJson(restoredUse.CaptureSnapshot());
            Require(!restoredUse.RestoreSnapshot(invalid) &&
                    JsonUtility.ToJson(restoredUse.CaptureSnapshot()) == beforeRejectedRestore,
                "invalid camp-use room is rejected atomically");

            restoredUse.Reset();
            Require(restoredUse.CurrentRoomId == PrototypeCampModuleCatalog.StartRoomId &&
                    !restoredUse.IsDayBenefitPrepared(StructureKind.Campfire) &&
                    !restoredUse.IsDayBenefitPrepared(StructureKind.RainCollector),
                "camp-use Reset restores new-game room and benefits");
            return placementDetail + " campUse=v1 JSON exact; invalid room atomic; reset=clean";
        }

        private static string VerifyInputParity()
        {
            PrototypePlayerActions keyboardInteract = PrototypePlayerActions.FromRaw(new PrototypeRawInput { KeyboardInteract = true });
            PrototypePlayerActions gamepadInteract = PrototypePlayerActions.FromRaw(new PrototypeRawInput { GamepadInteract = true });
            Require(keyboardInteract.InteractPressed && gamepadInteract.InteractPressed, "interact parity");
            PrototypeCampModulePreviewActions keyboard = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                KeyboardNext = true, KeyboardConfirm = true, KeyboardCancel = true
            });
            PrototypeCampModulePreviewActions gamepad = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                HorizontalAxis = 1f, GamepadConfirm = true, GamepadCancel = true
            });
            Require(keyboard.CycleDirection == gamepad.CycleDirection && keyboard.ConfirmPressed == gamepad.ConfirmPressed &&
                    keyboard.CancelPressed == gamepad.CancelPressed, "preview parity");
            Require(PrototypeInputPromptKeys.CampProximity(PrototypeInputDevice.KeyboardMouse) == "camp.interaction.prompt.keyboard_mouse" &&
                    PrototypeInputPromptKeys.CampProximity(PrototypeInputDevice.Gamepad) == "camp.interaction.prompt.gamepad", "device prompt IDs");
            return "interact=true; cycle=+1; confirm=true; cancel=true; device prompt keys remain distinct with equivalent meaning";
        }

        private static string InspectLocalizationContract(out bool passed)
        {
            string[] required =
            {
                DirectActionKey, "module.name.upper", "module.name.side", "module.name.basement",
                "module.geometry.valid", "module.geometry.noconnectionslot", "module.geometry.overlap",
                "module.geometry.terrainblocked", "module.geometry.pathblocked", "module.economy.locked",
                "module.economy.short", "module.economy.ready", "module.economy.prototypelimit"
            };
            Dictionary<string, string[]> rows = ReadTsv();
            List<string> missing = new List<string>();
            foreach (string key in required)
            {
                string[] columns;
                if (!rows.TryGetValue(key, out columns) || columns.Length < 4 || columns.Skip(1).Any(string.IsNullOrWhiteSpace))
                {
                    missing.Add(key);
                }
            }
            bool placeholders = rows.ContainsKey("module.economy.short") && rows["module.economy.short"].Skip(1).All(value =>
                value.Contains("{0}") && value.Contains("{1}") && value.Contains("{2}"));
            bool qpsWrapped = required.Where(key => key != DirectActionKey).All(key => rows.ContainsKey(key) && rows[key][3].StartsWith("⟦", StringComparison.Ordinal));
            passed = missing.Count == 0 && placeholders && qpsWrapped;
            return "required=" + required.Length + "; missing=" + (missing.Count == 0 ? "none" : string.Join(",", missing)) +
                   "; namedCanonicalPlaceholders={0},{1},{2}:" + placeholders + "; qpsWrapped=" + qpsWrapped;
        }

        private static Dictionary<string, string[]> ReadTsv()
        {
            string path = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            return File.ReadAllLines(path, Encoding.UTF8).Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('\t'))
                .Where(columns => columns.Length > 0)
                .ToDictionary(columns => columns[0], columns => columns, StringComparer.Ordinal);
        }

        private static PromptLayoutResult MeasurePromptLayout(KimSurvivalPrototype prototype, GameObject prompt, Camera camera, Vector2 slot)
        {
            RectTransform rectTransform = prompt == null ? null : prompt.GetComponent<RectTransform>();
            if (rectTransform == null || camera == null)
            {
                return new PromptLayoutResult { FailureReasons = "missing prompt RectTransform or camera" };
            }

            Canvas canvas = prompt.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return new PromptLayoutResult { FailureReasons = "missing parent Canvas" };
            }
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture captureTarget = RenderTexture.GetTemporary(1280, 800, 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture = captureTarget;
                Canvas.ForceUpdateCanvases();
                Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Rect sourcePixels = canvas.pixelRect;
                Rect promptRect = RectTransformScreenRect(rectTransform, canvasCamera, sourcePixels);
                Transform playerRoot = GetField<Transform>(prototype, "playerRoot");
                PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
                Rect playerRect = RendererScreenRect(playerRoot == null ? null : playerRoot.gameObject, camera, sourcePixels,
                    new Rect(campUse.PlayerPosition.x - 0.45f, PrototypeCampPlacement.FloorY - 0.05f, 0.9f, 1.7f));
                Rect targetRect = WorldRectToScreenRect(
                    new Rect(slot.x - 0.55f, PrototypeCampPlacement.FloorY - 0.1f, 1.1f, 1.45f), camera, sourcePixels);
                float captureAspect = sourcePixels.height > 0.01f ? sourcePixels.width / sourcePixels.height : 1.6f;
                float halfWidth = camera.orthographicSize * captureAspect;
                Rect walkingWorld = new Rect(
                    camera.transform.position.x - halfWidth,
                    PrototypeCampPlacement.FloorY - 0.25f,
                    halfWidth * 2f,
                    1.15f);
                Rect walkingRect = WorldRectToScreenRect(walkingWorld, camera, sourcePixels);

                List<string> failures = new List<string>();
                if (promptRect.xMin < 0f || promptRect.yMin < 0f || promptRect.xMax > 1280f || promptRect.yMax > 800f)
                {
                    failures.Add("screen-bounds");
                }
                if (promptRect.Overlaps(playerRect)) failures.Add("player-silhouette");
                if (promptRect.Overlaps(targetRect)) failures.Add("target-silhouette");
                if (promptRect.Overlaps(walkingRect)) failures.Add("walking-band");

                return new PromptLayoutResult
                {
                    Passed = failures.Count == 0,
                    Prompt = promptRect,
                    Player = playerRect,
                    Target = targetRect,
                    WalkingBand = walkingRect,
                    CapturePixels = sourcePixels,
                    CanvasScaleFactor = canvas.scaleFactor,
                    FailureReasons = failures.Count == 0 ? "none" : string.Join(",", failures)
                };
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Canvas.ForceUpdateCanvases();
                RenderTexture.ReleaseTemporary(captureTarget);
            }
        }

        private static Rect RectTransformScreenRect(RectTransform rectTransform, Camera camera, Rect sourcePixelRect)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2[] points = corners.Select(corner => RectTransformUtility.WorldToScreenPoint(camera, corner)).ToArray();
            return NormalizeToCapture(Rect.MinMaxRect(
                points.Min(point => point.x), points.Min(point => point.y),
                points.Max(point => point.x), points.Max(point => point.y)), sourcePixelRect);
        }

        private static Rect RendererScreenRect(GameObject root, Camera camera, Rect sourcePixelRect, Rect fallbackWorld)
        {
            Renderer[] renderers = root == null ? Array.Empty<Renderer>() : root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return WorldRectToScreenRect(fallbackWorld, camera, sourcePixelRect);
            }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i += 1) bounds.Encapsulate(renderers[i].bounds);
            return WorldRectToScreenRect(new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y), camera, sourcePixelRect);
        }

        private static Rect WorldRectToScreenRect(Rect worldRect, Camera camera, Rect sourcePixelRect)
        {
            Vector3[] points =
            {
                camera.WorldToScreenPoint(new Vector3(worldRect.xMin, worldRect.yMin, 0f)),
                camera.WorldToScreenPoint(new Vector3(worldRect.xMin, worldRect.yMax, 0f)),
                camera.WorldToScreenPoint(new Vector3(worldRect.xMax, worldRect.yMin, 0f)),
                camera.WorldToScreenPoint(new Vector3(worldRect.xMax, worldRect.yMax, 0f))
            };
            return NormalizeToCapture(Rect.MinMaxRect(
                points.Min(point => point.x), points.Min(point => point.y),
                points.Max(point => point.x), points.Max(point => point.y)), sourcePixelRect);
        }

        private static Rect NormalizeToCapture(Rect rect, Rect sourcePixelRect)
        {
            float sourceWidth = sourcePixelRect.width > 0.01f ? sourcePixelRect.width : Mathf.Max(1f, Screen.width);
            float sourceHeight = sourcePixelRect.height > 0.01f ? sourcePixelRect.height : Mathf.Max(1f, Screen.height);
            return new Rect(
                (rect.x - sourcePixelRect.x) * 1280f / sourceWidth,
                (rect.y - sourcePixelRect.y) * 800f / sourceHeight,
                rect.width * 1280f / sourceWidth,
                rect.height * 800f / sourceHeight);
        }

        private static string FormatRect(Rect rect)
        {
            return "x=" + rect.x.ToString("0.0") + ",y=" + rect.y.ToString("0.0") +
                   ",w=" + rect.width.ToString("0.0") + ",h=" + rect.height.ToString("0.0");
        }

        private static bool VerifyPng(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) { return false; }
            string path = Path.Combine(EvidenceFolder, fileName);
            if (!File.Exists(path) || new FileInfo(path).Length == 0) { return false; }
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                return texture.LoadImage(File.ReadAllBytes(path), false) && texture.width == 1280 && texture.height == 800;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ActiveTargetId(PrototypeCampInteraction interaction)
        {
            FieldInfo field = typeof(PrototypeCampInteraction).GetField("activeTarget", InstanceFlags);
            if (field == null) { return string.Empty; }
            object target = field.GetValue(interaction);
            PropertyInfo id = target == null ? null : target.GetType().GetProperty("Id", InstanceFlags);
            return id == null ? string.Empty : Convert.ToString(id.GetValue(target));
        }

        private static PrototypeCampModuleExpansion NewExpansion()
        {
            return new PrototypeCampModuleExpansion(PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance());
        }

        private static CampModuleReturnSnapshot DefaultSnapshot()
        {
            return new CampModuleReturnSnapshot(new Vector2(PrototypeCampUse.PlayerStartX, PrototypeCampUse.PlayerFloorY), 1f, PrototypeCampModuleCatalog.StartRoomId);
        }

        private static GameSession NewWorkbenchSession(int wood, int salvage)
        {
            GameSession session = new GameSession();
            SetStorage(session, ResourceKind.Wood, 2); SetStorage(session, ResourceKind.Salvage, 1);
            Require(session.TryBuild(StructureKind.Workbench), "workbench fixture build");
            SetStorage(session, ResourceKind.Wood, wood); SetStorage(session, ResourceKind.Salvage, salvage);
            return session;
        }

        private static void SetStorage(GameSession session, ResourceKind kind, int exact)
        {
            session.Grant(kind, exact - session.GetStorage(kind));
            Require(session.GetStorage(kind) == exact, kind + " normalized to " + exact);
        }

        private static string StorageFingerprint(GameSession session)
        {
            return "W" + session.GetStorage(ResourceKind.Wood) + "/S" + session.GetStorage(ResourceKind.Stone) +
                   "/F" + session.GetStorage(ResourceKind.Food) + "/D" + session.GetStorage(ResourceKind.Salvage);
        }

        private static string ModuleSnapshotFingerprint(PrototypeCampModuleExpansionSnapshot snapshot)
        {
            CampModuleCommittedRoomSnapshot[] rooms = snapshot == null || snapshot.CommittedRooms == null
                ? Array.Empty<CampModuleCommittedRoomSnapshot>()
                : snapshot.CommittedRooms;
            return snapshot == null
                ? "null"
                : snapshot.SchemaVersion + "|" + snapshot.HasCommittedModule + "|" + snapshot.CommittedArchetype + "|" +
                  snapshot.CommittedRoomId + "|" + string.Join(";", rooms.Select(room => room == null
                      ? "null"
                      : room.CommitSequence + ":" + room.Archetype + ":" + room.RoomId + ":" + room.StartSlotId +
                        ":" + room.ReciprocalSlotId + ":" + room.ConnectorKind));
        }

        private static bool IsMissingLocalization(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value.StartsWith("⟦MISSING:", StringComparison.Ordinal);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, InstanceFlags);
            if (field == null) { throw new MissingFieldException(target.GetType().FullName, name); }
            return (T)field.GetValue(target);
        }

        private static object Invoke(object target, string name, params object[] arguments)
        {
            object[] supplied = arguments ?? Array.Empty<object>();
            MethodInfo method = target.GetType().GetMethods(InstanceFlags)
                .Where(candidate => candidate.Name == name)
                .FirstOrDefault(candidate => ParametersAccept(candidate.GetParameters(), supplied));
            if (method == null) { throw new MissingMethodException(target.GetType().FullName, name); }
            try { return method.Invoke(target, supplied); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static bool ParametersAccept(ParameterInfo[] parameters, object[] arguments)
        {
            if (parameters.Length != arguments.Length) return false;
            for (int index = 0; index < parameters.Length; index += 1)
            {
                object value = arguments[index];
                Type parameterType = parameters[index].ParameterType;
                if (value == null)
                {
                    if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null) return false;
                    continue;
                }
                if (!parameterType.IsInstanceOfType(value)) return false;
            }
            return true;
        }

        private static string RequireDetail(bool condition, string detail)
        {
            Require(condition, detail);
            return detail;
        }

        private static string Product(List<ContractCheck> checks, string id, string matrix, string severity, string expected,
            Func<string> verification, string reproduction, string files)
        {
            try
            {
                string actual = verification();
                checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, actual, reproduction, files));
                return actual;
            }
            catch (Exception exception)
            {
                string actual = exception.GetType().Name + ": " + exception.Message;
                checks.Add(NewCheck(id, matrix, "FAIL", "PRODUCT_REGRESSION", severity, expected, actual, reproduction, files));
                return actual;
            }
        }

        private static void ExpectedProduct(List<ContractCheck> checks, string id, string matrix, string severity, string expected,
            bool passed, string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, passed ? "PASS" : "EXPECTED_FAIL", passed ? "NONE" : "PRODUCT_EXPECTED_GAP",
                severity, expected, actual, reproduction, files));
        }

        private static void Unverified(List<ContractCheck> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", "HARDWARE_GAP", severity, expected, actual, reproduction, files));
        }

        private static ContractCheck NewCheck(string id, string matrix, string status, string classification, string severity,
            string expected, string actual, string reproduction, string files)
        {
            return new ContractCheck
            {
                id = id, matrix = matrix, status = status, classification = classification, severity = severity,
                expected = expected, actual = actual, reproduction = reproduction, recommendedFiles = files
            };
        }

        private static ContractReport WriteReport(string stem, string title, DateTime started, List<ContractCheck> checks)
        {
            ContractReport report = new ContractReport
            {
                title = title, runId = RunId, baselineCommit = BaselineCommit, unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"), completedUtc = DateTime.UtcNow.ToString("O"),
                passed = checks.Count(check => check.status == "PASS"),
                expectedFailed = checks.Count(check => check.status == "EXPECTED_FAIL"),
                productFailed = checks.Count(check => check.status == "FAIL"),
                infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"), checks = checks.ToArray()
            };
            report.productOverall = report.productFailed > 0 ? "FAIL" : report.expectedFailed > 0 ? "RED_EXPECTED_FAIL" : "PASS";
            report.infrastructureOverall = report.infrastructureFailed > 0 ? "FAIL" : "PASS";
            report.overall = report.infrastructureOverall == "FAIL" || report.productOverall == "FAIL" ? "FAIL" :
                report.productOverall == "RED_EXPECTED_FAIL" ? "RED" : "PASS";
            WriteJson(stem + ".json", report);
            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Unity: " + Application.unityVersion);
            text.AppendLine("Overall/Product/Infrastructure: " + report.overall + "/" + report.productOverall + "/" + report.infrastructureOverall);
            text.AppendLine("PASS/EXPECTED_FAIL/FAIL/INFRA_FAIL/UNVERIFIED: " + report.passed + "/" + report.expectedFailed + "/" + report.productFailed + "/" + report.infrastructureFailed + "/" + report.unverified);
            foreach (ContractCheck check in checks)
            {
                text.AppendLine(check.id + " | " + check.status + " | " + check.classification + " | " + check.actual);
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, stem + ".txt"), text.ToString(), Utf8NoBom);
            return report;
        }

        private static SlotMatrixEvidence NewEvidence()
        {
            return new SlotMatrixEvidence
            {
                runId = RunId, baselineCommit = BaselineCommit, unityVersion = Application.unityVersion,
                slots = Array.Empty<SlotObservation>(), screenshots = Array.Empty<string>(), joystickNames = Array.Empty<string>()
            };
        }

        private static void WriteJson(string fileName, object value)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, fileName), JsonUtility.ToJson(value, true) + Environment.NewLine, Utf8NoBom);
        }

        private static void WritePlayInfrastructureFailure(Exception exception)
        {
            List<ContractCheck> checks = new List<ContractCheck>
            {
                NewCheck("W11-I99.play_runner", "infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Wave 11 Play runner produces parseable evidence", exception.ToString(),
                    "Run the Wave 11 Play execute method outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave11SlotDiscoveryGateRunner.cs")
            };
            WriteReport("wave11-slot-play-contracts", "Wave 11 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · " + exception);
        }

        private static void StopPlayContracts()
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; }
        }

        private static void FinishPlayContracts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (playTickAttached)
            {
                EditorApplication.update -= PlayTick;
                playTickAttached = false;
            }
            bool passed = SessionState.GetBool(PlayExitPassKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL · missing Wave 11 Play result");
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayExitPassKey);
            SessionState.EraseString(PlayMessageKey);
            Debug.Log(message);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) { value = value.Replace(invalid, '_'); }
            return value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) { throw new InvalidOperationException(message); }
        }
    }
}
