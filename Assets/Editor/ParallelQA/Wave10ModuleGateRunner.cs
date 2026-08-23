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
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace ParallelQA
{
    /// <summary>
    /// Independent Wave 10 room-module contract gate.
    ///
    /// This runner deliberately calls the shipped module model instead of
    /// guessing support from member names. Product failures stay product
    /// failures; missing qps-long and physical hardware remain separate gates.
    /// </summary>
    public static class Wave10ModuleGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string QpsLongLocaleCode = "qps-long";
        private const string PlayRunningKey = "ParallelQA.Wave10.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave10.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave10.PlayMessage";
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
        private sealed class ModuleMatrixEvidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string candidatesAndReasons;
            public string economyTransitions;
            public string transactionAtomicity;
            public string connectorsAndTraversal;
            public string moduleGeneralFloorPlacement;
            public string keyboardSyntheticGamepadParity;
            public string localization;
            public string normalCampKoreanOverflow;
            public string actualQpsLong;
            public string fullRegression;
            public string[] screenshots;
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
                return string.IsNullOrWhiteSpace(value) ? "manual-wave10" : Sanitize(value);
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
            ModuleMatrixEvidence evidence = NewEvidence();

            evidence.candidatesAndReasons = Product(
                checks,
                "W10-M01.candidates_and_reasons",
                "module preview/validity",
                "P0",
                "Upper, side, and basement definitions each evaluate READY geometry and expose every invalid geometry reason deterministically",
                VerifyCandidatesAndGeometryReasons,
                "Cycle the three candidates, then independently remove the slot, add overlap, block terrain, and block the required route.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs");

            evidence.economyTransitions = Product(
                checks,
                "W10-M02.locked_short_ready",
                "unlock/economy",
                "P0",
                "Without a workbench the module is LOCKED; after the workbench W2/D1 shortage is SHORT and exact W2/D1 is READY",
                VerifyLockedShortReady,
                "Evaluate a fresh upper-room preview before building the workbench, after normalizing below W2/D1, and after normalizing to exact W2/D1.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs; Assets/_Project/Scripts/Runtime/GameSession.cs");

            evidence.transactionAtomicity = Product(
                checks,
                "W10-M03.atomic_commit_and_limit",
                "transaction atomicity",
                "P0",
                "Cancel, invalid, short, and duplicate submits spend nothing; success spends W2/D1 once; a second room reports PROTOTYPE_LIMIT",
                VerifyAtomicCommitAndPrototypeLimit,
                "Compare all resource counters around cancel, invalid geometry, SHORT, successful commit, duplicate submit, and a second preview.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs; Assets/_Project/Scripts/Runtime/GameSession.cs");

            evidence.connectorsAndTraversal = Product(
                checks,
                "W10-M04.reciprocal_connectors_and_room_return",
                "connectors/traversal",
                "P0",
                "Every candidate has distinct start/reciprocal connector IDs and Kim can enter the committed room and return to the preserved starting room",
                VerifyConnectorsTraversalAndStartState,
                "Commit one module, approach its connector from both sides, and compare room identity plus the pre-expansion starting-room placement snapshot.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampModuleExpansion.cs; Assets/_Project/Scripts/Runtime/PrototypeCampUse.cs; Assets/_Project/Scripts/Runtime/PrototypeCampPlacement.cs");

            evidence.moduleGeneralFloorPlacement = Product(
                checks,
                "W10-M05.general_floor_placement",
                "module placement",
                "P0",
                "A general facility can be placed on each candidate room's general-floor zone without entering its connector or required-route band",
                VerifyGeneralFloorPlacement,
                "Create each module room zone and place a campfire on a valid snapped general-floor position.",
                "Assets/_Project/Scripts/Runtime/PrototypeCampPlacement.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

            evidence.keyboardSyntheticGamepadParity = Product(
                checks,
                "W10-M06.keyboard_synthetic_gamepad_parity",
                "input",
                "P1",
                "Keyboard/mouse and synthetic gamepad inputs produce the same cycle, confirm, and cancel module-preview actions",
                VerifyInputParity,
                "Feed equivalent keyboard/mouse and synthetic gamepad raw states through PrototypeCampModulePreviewActions.FromRaw.",
                "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");

            WriteJson("wave10-module-edit-evidence.json", evidence);
            WriteReport("wave10-module-edit-contracts", "Wave 10 room-module Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Wave 10 Play contracts did not complete.");
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
            playTimeoutAt = EditorApplication.timeSinceStartup + 240d;
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
                playTimeoutAt = EditorApplication.timeSinceStartup + 240d;
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
                WritePlayInfrastructureFailure(new TimeoutException("Timed out waiting for the Wave 10 playable scene."));
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
            ModuleMatrixEvidence evidence = NewEvidence();
            try
            {
                PrototypeLocalization localization = GetField<PrototypeLocalization>(prototype, "localization");
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                Invoke(prototype, "RefreshAll");

                int koOverflow = CountVisibleOverflowingText();
                evidence.normalCampKoreanOverflow = "visibleOverflow=" + koOverflow;
                KnownGap(
                    checks,
                    "W10-L01.normal_camp_ko_overflow",
                    "localization/layout",
                    "P1",
                    "The normal Korean camp frame has zero visible TMP overflow at 1280x800",
                    koOverflow == 0,
                    evidence.normalCampKoreanOverflow,
                    "Open the fresh Korean normal camp at 1280x800 and inspect every active TMP object after ForceMeshUpdate.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                evidence.localization = Product(
                    checks,
                    "W10-L02.ko_en_module_meaning",
                    "localization/input prompts",
                    "P0",
                    "KO and EN expose the same module names, geometry/economy reasons, W2/D1 cost, and keyboard/gamepad confirm/cancel semantics",
                    () => VerifyModuleLocalization(localization),
                    "Format the same canonical module keys in ko and en, including all invalid reasons and both device prompt keys.",
                    "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv; Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs");

                Locale qpsLocale = LocalizationSettings.AvailableLocales.GetLocale(QpsLongLocaleCode);
                string qpsDetail = "locale absent";
                bool qpsReady = qpsLocale != null && VerifyQpsModuleKeys(localization, out qpsDetail);
                evidence.actualQpsLong = qpsDetail;
                KnownGap(
                    checks,
                    "W10-L03.actual_qps_long_locale",
                    "localization/layout",
                    "P1",
                    "A real qps-long locale formats the module contract without fallback or missing keys",
                    qpsReady,
                    evidence.actualQpsLong,
                    "Select the actual qps-long locale and format/capture the module preview; do not substitute a synthetic text override.",
                    "Assets/_Project/Scripts/Localization; Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs");

                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                evidence.fullRegression = Product(
                    checks,
                    "W10-P01.approach_first_full_regression",
                    "full playable regression",
                    "P0",
                    "The approach-first loop completes module preview/commit/traversal/placement plus prompt, bag, signal, search, swim, return, and three-day rescue checks",
                    () => RunFullRegression(prototype),
                    "Run KimSurvivalPrototype.RunAutomatedVerification through the Wave 10 Play gate.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                string[] moduleScreenshots = CopyAndVerifyModuleScreenshots();
                evidence.screenshots = moduleScreenshots;
                Product(
                    checks,
                    "W10-P02.ko_en_1280_module_captures",
                    "localization/layout",
                    "P0",
                    "Fresh 1280x800 KO upper/interior, EN side, and actual qps-long module captures exist; older baselines may use an explicitly labelled synthetic fallback",
                    () => VerifyScreenshotDimensions(moduleScreenshots),
                    "Open each Wave 10 module PNG at 1:1 and compare the preview badge, status card, controls, connector, and world bounds.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/Editor/ParallelQA/Wave10ModuleGateRunner.cs");

                string[] joystickNames = Input.GetJoystickNames()
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToArray();
                evidence.joystickNames = joystickNames;
                evidence.physicalGamepad = "UNVERIFIED";
                Unverified(
                    checks,
                    "W10-HW01.physical_gamepad",
                    "input hardware",
                    "P1",
                    "A human completes module preview, commit, connector traversal, and general-floor placement on a physical gamepad",
                    joystickNames.Length == 0 ? "no non-empty joystick name exposed to Unity batch Play Mode" : "device name detected but no human actuation evidence was captured",
                    "Run the Windows development build with a physical gamepad and record device name/VID/PID plus human actuation.",
                    "manual release-candidate hardware evidence");

                WriteJson("wave10-module-play-evidence.json", evidence);
                ContractReport report = WriteReport("wave10-module-play-contracts", "Wave 10 room-module Play contracts", started, checks);
                bool runnerPassed = report.infrastructureOverall == "PASS" && report.productFailed == 0;
                SessionState.SetBool(PlayExitPassKey, runnerPassed);
                SessionState.SetString(
                    PlayMessageKey,
                    "Product=" + report.productOverall + " · Infrastructure=" + report.infrastructureOverall +
                    " · PhysicalGamepad=UNVERIFIED · Evidence=" + Path.Combine(EvidenceFolder, "wave10-module-play-contracts.json"));
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }

            StopPlayContracts();
        }

        private static void WritePlayInfrastructureFailure(Exception exception)
        {
            List<ContractCheck> checks = new List<ContractCheck>();
            Infrastructure(
                checks,
                "W10-I99.play_runner",
                "infrastructure",
                "P0",
                "Wave 10 Play runner produces parseable evidence",
                () => throw exception,
                "Run the Wave 10 Play execute method outside the Codex sandbox.",
                "Assets/Editor/ParallelQA/Wave10ModuleGateRunner.cs");
            WriteReport("wave10-module-play-contracts", "Wave 10 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · " + exception);
        }

        private static void StopPlayContracts()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
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
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL · missing Wave 10 Play result");
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayExitPassKey);
            SessionState.EraseString(PlayMessageKey);
            Debug.Log(message);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static string VerifyCandidatesAndGeometryReasons()
        {
            CampModuleArchetype[] expected =
            {
                CampModuleArchetype.Upper,
                CampModuleArchetype.Side,
                CampModuleArchetype.Basement
            };
            CampModuleDefinition[] definitions = PrototypeCampModuleCatalog.All.ToArray();
            Require(definitions.Length == expected.Length, "expected exactly three module definitions");
            Require(definitions.Select(item => item.Archetype).SequenceEqual(expected), "upper/side/basement ordering");

            List<string> rows = new List<string>();
            foreach (CampModuleDefinition definition in definitions)
            {
                Require(!string.IsNullOrWhiteSpace(definition.RoomId), definition.Archetype + " room id");
                Require(!string.IsNullOrWhiteSpace(definition.StartSlotId), definition.Archetype + " start slot");
                Require(!string.IsNullOrWhiteSpace(definition.ReciprocalSlotId), definition.Archetype + " reciprocal slot");
                Require(definition.StartSlotId != definition.ReciprocalSlotId, definition.Archetype + " reciprocal identity");

                CampModuleValidationContext valid = new CampModuleValidationContext();
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, valid) == CampModuleGeometryStatus.Valid, definition.Archetype + " valid");

                CampModuleValidationContext noSlot = valid.Clone();
                noSlot.HasMatchingConnectionSlot = false;
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, noSlot) == CampModuleGeometryStatus.NoConnectionSlot, definition.Archetype + " no-slot reason");

                CampModuleValidationContext overlap = valid.Clone();
                overlap.OccupiedRoomBounds.Add(definition.Bounds);
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, overlap) == CampModuleGeometryStatus.Overlap, definition.Archetype + " overlap reason");

                CampModuleValidationContext terrain = valid.Clone();
                terrain.TerrainAllowsCandidate = false;
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, terrain) == CampModuleGeometryStatus.TerrainBlocked, definition.Archetype + " terrain reason");

                CampModuleValidationContext connector = valid.Clone();
                connector.ConnectorClear = false;
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, connector) == CampModuleGeometryStatus.PathBlocked, definition.Archetype + " connector/path reason");

                CampModuleValidationContext route = valid.Clone();
                route.RequiredPathClear = false;
                Require(PrototypeCampModuleExpansion.EvaluateGeometry(definition, route) == CampModuleGeometryStatus.PathBlocked, definition.Archetype + " required-route reason");
                rows.Add(definition.Archetype + "=Valid/NoConnectionSlot/Overlap/TerrainBlocked/PathBlocked");
            }

            return string.Join(" | ", rows);
        }

        private static string VerifyLockedShortReady()
        {
            PrototypeCampModuleExpansion expansion = NewExpansion();
            GameSession session = new GameSession();
            CampModuleValidationContext context = new CampModuleValidationContext();
            Require(expansion.BeginPreview(DefaultSnapshot()), "begin locked preview");
            SetStorage(session, ResourceKind.Wood, 2);
            SetStorage(session, ResourceKind.Salvage, 1);
            CampModuleEvaluation locked = expansion.Evaluate(session, context);
            Require(locked.Geometry == CampModuleGeometryStatus.Valid && locked.Economy == CampModuleEconomyStatus.Locked, "pre-workbench LOCKED");
            expansion.CancelPreview();

            BuildWorkbench(session);
            SetStorage(session, ResourceKind.Wood, 1);
            SetStorage(session, ResourceKind.Salvage, 1);
            Require(expansion.BeginPreview(DefaultSnapshot()), "begin short preview");
            CampModuleEvaluation shortage = expansion.Evaluate(session, context);
            Require(shortage.Economy == CampModuleEconomyStatus.Short, "post-workbench W1/D1 SHORT");
            SetStorage(session, ResourceKind.Wood, 2);
            SetStorage(session, ResourceKind.Salvage, 1);
            CampModuleEvaluation ready = expansion.Evaluate(session, context);
            Require(ready.Economy == CampModuleEconomyStatus.Ready && ready.CanCommit, "exact W2/D1 READY");
            Require(ready.Cost.Wood == 2 && ready.Cost.Stone == 0 && ready.Cost.Food == 0 && ready.Cost.Salvage == 1, "locked W2/D1 cost");
            return "beforeWorkbench=" + locked.Economy + " afterWorkbenchW1D1=" + shortage.Economy + " exactW2D1=" + ready.Economy + " cost=W2/S0/F0/D1";
        }

        private static string VerifyAtomicCommitAndPrototypeLimit()
        {
            GameSession session = NewWorkbenchSession(2, 1);
            PrototypeCampModuleExpansion expansion = NewExpansion();
            CampModuleValidationContext valid = new CampModuleValidationContext();
            string initial = StorageFingerprint(session);

            Require(expansion.BeginPreview(DefaultSnapshot()), "begin cancel preview");
            expansion.CancelPreview();
            Require(StorageFingerprint(session) == initial && !expansion.HasCommittedModule, "cancel is resource/state neutral");

            Require(expansion.BeginPreview(DefaultSnapshot()), "begin invalid preview");
            CampModuleValidationContext invalid = valid.Clone();
            invalid.RequiredPathClear = false;
            CampModuleCommitStatus invalidStatus = expansion.TryCommit(session, invalid);
            Require(invalidStatus == CampModuleCommitStatus.InvalidGeometry && StorageFingerprint(session) == initial, "invalid geometry is neutral");
            expansion.CancelPreview();

            SetStorage(session, ResourceKind.Wood, 1);
            string shortBefore = StorageFingerprint(session);
            Require(expansion.BeginPreview(DefaultSnapshot()), "begin short preview");
            CampModuleCommitStatus shortStatus = expansion.TryCommit(session, valid);
            Require(shortStatus == CampModuleCommitStatus.Short && StorageFingerprint(session) == shortBefore, "short submit is neutral");
            expansion.CancelPreview();

            SetStorage(session, ResourceKind.Wood, 2);
            SetStorage(session, ResourceKind.Salvage, 1);
            string commitBefore = StorageFingerprint(session);
            Require(expansion.BeginPreview(DefaultSnapshot()), "begin funded preview");
            CampModuleCommitStatus success = expansion.TryCommit(session, valid);
            string commitAfter = StorageFingerprint(session);
            Require(success == CampModuleCommitStatus.Succeeded, "funded commit succeeds");
            Require(session.GetStorage(ResourceKind.Wood) == 0 && session.GetStorage(ResourceKind.Salvage) == 0, "exact W2/D1 charged once");
            CampModuleCommitStatus duplicate = expansion.TryCommit(session, valid);
            Require(duplicate == CampModuleCommitStatus.NotPreviewing || duplicate == CampModuleCommitStatus.DuplicateSubmit, "duplicate submit rejected");
            Require(StorageFingerprint(session) == commitAfter, "duplicate submit is neutral");

            SetStorage(session, ResourceKind.Wood, 2);
            SetStorage(session, ResourceKind.Salvage, 1);
            Require(expansion.BeginPreview(DefaultSnapshot()), "begin second-room preview");
            CampModuleEvaluation second = expansion.Evaluate(session, valid);
            string secondBefore = StorageFingerprint(session);
            CampModuleCommitStatus limit = expansion.TryCommit(session, valid);
            Require(second.Economy == CampModuleEconomyStatus.PrototypeLimit && limit == CampModuleCommitStatus.PrototypeLimit, "second room PROTOTYPE_LIMIT");
            Require(StorageFingerprint(session) == secondBefore, "prototype-limit submit is neutral");
            return "cancel=stable invalid=" + invalidStatus + " short=" + shortStatus + " success=" + success + " before=" + commitBefore + " after=" + commitAfter + " duplicate=" + duplicate + " second=" + limit;
        }

        private static string VerifyConnectorsTraversalAndStartState()
        {
            foreach (CampModuleDefinition definition in PrototypeCampModuleCatalog.All)
            {
                Require(!string.IsNullOrWhiteSpace(definition.StartSlotId) && !string.IsNullOrWhiteSpace(definition.ReciprocalSlotId), definition.Archetype + " reciprocal connector pair");
                Require(definition.StartSlotId != definition.ReciprocalSlotId, definition.Archetype + " connector endpoints differ");
                Require(definition.ConnectorKind == (definition.Archetype == CampModuleArchetype.Side ? CampModuleConnectorKind.Door : CampModuleConnectorKind.Ladder), definition.Archetype + " connector kind");
            }

            PrototypeCampPlacement placement = new PrototypeCampPlacement();
            placement.EnsureInstalled(StructureKind.Workbench);
            placement.EnsureInstalled(StructureKind.Campfire);
            string startWorkbenchRoom = placement.GetInstalledRoomId(StructureKind.Workbench);
            Vector2 startWorkbenchPosition = placement.GetInstalledPosition(StructureKind.Workbench);
            Vector2 startCampfirePosition = placement.GetInstalledPosition(StructureKind.Campfire);

            GameSession session = NewWorkbenchSession(2, 1);
            PrototypeCampModuleExpansion expansion = NewExpansion();
            Require(expansion.BeginPreview(DefaultSnapshot()), "begin traversal fixture");
            Require(expansion.TryCommit(session, new CampModuleValidationContext()) == CampModuleCommitStatus.Succeeded, "commit traversal fixture");
            CampModuleDefinition committed = PrototypeCampModuleCatalog.Get(expansion.CommittedArchetype);

            PrototypeCampUse use = new PrototypeCampUse();
            string startRoom = use.CurrentRoomId;
            use.EnterRoom(committed.RoomId, committed.ModuleConnectorDisplayX + 0.85f);
            Require(use.CurrentRoomId == committed.RoomId, "Kim enters committed room");
            use.EnterRoom(PrototypeCampModuleCatalog.StartRoomId, committed.StartConnectorDisplayX - 0.85f);
            Require(use.CurrentRoomId == startRoom, "Kim returns to start room");
            Require(placement.GetInstalledRoomId(StructureKind.Workbench) == startWorkbenchRoom &&
                    placement.GetInstalledPosition(StructureKind.Workbench) == startWorkbenchPosition &&
                    placement.GetInstalledPosition(StructureKind.Campfire) == startCampfirePosition,
                "module commit preserves starting-room facility state");
            return "pairs=" + string.Join(",", PrototypeCampModuleCatalog.All.Select(item => item.StartSlotId + "<->" + item.ReciprocalSlotId + ":" + item.ConnectorKind)) +
                   " travel=" + startRoom + "->" + committed.RoomId + "->" + use.CurrentRoomId + " startStatePreserved=true";
        }

        private static string VerifyGeneralFloorPlacement()
        {
            List<string> rows = new List<string>();
            foreach (CampModuleDefinition definition in PrototypeCampModuleCatalog.All)
            {
                PrototypeCampPlacement placement = new PrototypeCampPlacement();
                CampPlacementRoomZone room = ModuleRoomZone(definition);
                placement.Begin(StructureKind.Campfire, false, room);
                float validX = FindValidPlacementX(placement, StructureKind.Campfire, room);
                placement.SetCandidateX(validX);
                Require(placement.CurrentValidity == CampPlacementValidity.Valid, definition.Archetype + " general-floor validity");
                Require(placement.Commit(), definition.Archetype + " general-floor commit");
                Require(placement.IsInstalledInRoom(StructureKind.Campfire, definition.RoomId), definition.Archetype + " room ownership");
                rows.Add(definition.Archetype + "@" + validX.ToString("0.0"));
            }
            return "generalFloor=" + string.Join(",", rows);
        }

        private static string VerifyInputParity()
        {
            PrototypeCampModulePreviewActions keyboardConfirm = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                KeyboardNext = true,
                KeyboardConfirm = true
            });
            PrototypeCampModulePreviewActions gamepadConfirm = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                HorizontalAxis = 1f,
                GamepadConfirm = true
            });
            PrototypeCampModulePreviewActions keyboardCancel = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                KeyboardPrevious = true,
                KeyboardCancel = true
            });
            PrototypeCampModulePreviewActions gamepadCancel = PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                HorizontalAxis = -1f,
                GamepadCancel = true
            });
            Require(keyboardConfirm.CycleDirection == gamepadConfirm.CycleDirection && keyboardConfirm.ConfirmPressed == gamepadConfirm.ConfirmPressed, "cycle/confirm parity");
            Require(keyboardCancel.CycleDirection == gamepadCancel.CycleDirection && keyboardCancel.CancelPressed == gamepadCancel.CancelPressed, "cycle/cancel parity");
            Require(PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.KeyboardMouse) == "controls.module_preview.keyboard_mouse", "keyboard prompt key");
            Require(PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.Gamepad) == "controls.module_preview.gamepad", "gamepad prompt key");
            return "confirmCycle=" + keyboardConfirm.CycleDirection + " cancelCycle=" + keyboardCancel.CycleDirection + " sharedActions=true";
        }

        private static string VerifyModuleLocalization(PrototypeLocalization localization)
        {
            string[] keys =
            {
                "module.name.upper", "module.name.side", "module.name.basement",
                "module.geometry.valid", "module.geometry.noconnectionslot", "module.geometry.overlap", "module.geometry.terrainblocked", "module.geometry.pathblocked",
                "module.economy.locked", "module.economy.prototypelimit",
                "module.commit.invalidgeometry", "module.commit.short", "module.commit.prototypelimit", "module.commit.duplicatesubmit"
            };
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            string[] ko = keys.Select(key => localization.Format(key)).ToArray();
            string koShort = localization.Format("module.economy.short", 2, 0, 1);
            string koReady = localization.Format("module.economy.ready", 2, 0, 1);
            string koKeyboard = localization.Format(PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.KeyboardMouse), localization.DeviceName(PrototypeInputDevice.KeyboardMouse));
            string koGamepad = localization.Format(PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.Gamepad), localization.DeviceName(PrototypeInputDevice.Gamepad));

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            string[] en = keys.Select(key => localization.Format(key)).ToArray();
            string enShort = localization.Format("module.economy.short", 2, 0, 1);
            string enReady = localization.Format("module.economy.ready", 2, 0, 1);
            string enKeyboard = localization.Format(PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.KeyboardMouse), localization.DeviceName(PrototypeInputDevice.KeyboardMouse));
            string enGamepad = localization.Format(PrototypeInputPromptKeys.CampModulePreview(PrototypeInputDevice.Gamepad), localization.DeviceName(PrototypeInputDevice.Gamepad));

            Require(ko.All(IsLocalized) && en.All(IsLocalized), "all canonical module keys localize in ko/en");
            Require(IsLocalized(koShort) && IsLocalized(koReady) && IsLocalized(enShort) && IsLocalized(enReady), "cost placeholders format in ko/en");
            Require(koShort.Contains("2") && koShort.Contains("1") && enShort.Contains("2") && enShort.Contains("1"), "W2/D1 placeholders preserved");
            Require(koKeyboard.Contains("확정") && koKeyboard.Contains("취소") && koGamepad.Contains("확정") && koGamepad.Contains("취소"), "Korean device prompts preserve confirm/cancel meaning");
            Require(enKeyboard.IndexOf("confirm", StringComparison.OrdinalIgnoreCase) >= 0 && enKeyboard.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    enGamepad.IndexOf("confirm", StringComparison.OrdinalIgnoreCase) >= 0 && enGamepad.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0,
                "English device prompts preserve confirm/cancel meaning");
            return "keys=" + keys.Length + " koShort='" + koShort + "' enShort='" + enShort + "' keyboard/gamepad semantic parity=true";
        }

        private static bool VerifyQpsModuleKeys(PrototypeLocalization localization, out string detail)
        {
            string[] keys =
            {
                "module.name.upper", "module.name.side", "module.name.basement", "module.geometry.pathblocked",
                "module.economy.short", "module.economy.ready", "module.economy.prototypelimit",
                "controls.module_preview.keyboard_mouse", "controls.module_preview.gamepad"
            };
            if (!localization.SetQaLocale(QpsLongLocaleCode) || localization.CurrentLocaleCode != QpsLongLocaleCode)
            {
                detail = "qps-long selection failed or fell back to " + localization.CurrentLocaleCode;
                return false;
            }

            List<string> values = new List<string>();
            foreach (string key in keys)
            {
                string value = key.Contains("economy.short") || key.Contains("economy.ready")
                    ? localization.Format(key, 2, 0, 1)
                    : key.StartsWith("controls.", StringComparison.Ordinal)
                        ? localization.Format(key, localization.DeviceName(PrototypeInputDevice.Gamepad))
                        : localization.Format(key);
                values.Add(value);
            }
            bool passed = values.All(IsLocalized);
            detail = "locale=qps-long keys=" + values.Count + " localized=" + passed;
            return passed;
        }

        private static string RunFullRegression(KimSurvivalPrototype prototype)
        {
            string prefix = "wave10-regression-";
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
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave10-approach-first-regression.txt"), result + Environment.NewLine, Utf8NoBom);
            return result;
        }

        private static string[] CopyAndVerifyModuleScreenshots()
        {
            string actualQpsSource = "kim-survival-wave10-module-basement-qps-long-1280x800.png";
            string syntheticQpsSource = "kim-survival-wave9-module-basement-qps-long-1280x800.png";
            bool hasActualQps = File.Exists(Path.Combine(EvidenceFolder, actualQpsSource));
            string qpsSource = hasActualQps ? actualQpsSource : syntheticQpsSource;
            string qpsDestination = hasActualQps
                ? "wave10-module-basement-qps-long-1280x800.png"
                : "wave10-module-basement-synthetic-long-1280x800.png";
            Dictionary<string, string> copies = new Dictionary<string, string>
            {
                { "kim-survival-wave9-module-upper-ko-1280x800.png", "wave10-module-upper-ko-1280x800.png" },
                { "kim-survival-wave9-module-side-en-1280x800.png", "wave10-module-side-en-1280x800.png" },
                { qpsSource, qpsDestination },
                { "kim-survival-wave9-module-interior-ko-1280x800.png", "wave10-module-interior-ko-1280x800.png" }
            };
            foreach (KeyValuePair<string, string> copy in copies)
            {
                string source = Path.Combine(EvidenceFolder, copy.Key);
                string destination = Path.Combine(EvidenceFolder, copy.Value);
                Require(File.Exists(source), "source screenshot exists: " + copy.Key);
                File.Copy(source, destination, false);
            }
            return copies.Values.ToArray();
        }

        private static string VerifyScreenshotDimensions(string[] screenshots)
        {
            List<string> rows = new List<string>();
            foreach (string fileName in screenshots)
            {
                string path = Path.Combine(EvidenceFolder, fileName);
                Require(File.Exists(path) && new FileInfo(path).Length > 0, fileName + " exists");
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Require(texture.LoadImage(bytes, false), fileName + " decodes");
                    Require(texture.width == 1280 && texture.height == 800, fileName + " is 1280x800");
                    rows.Add(fileName + "=1280x800");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            return string.Join(" | ", rows) + " | synthetic-long is not an actual qps-long locale claim";
        }

        private static int CountVisibleOverflowingText()
        {
            int count = 0;
            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (TMP_Text text in texts)
            {
                if (text == null || !text.gameObject.activeInHierarchy)
                {
                    continue;
                }
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing)
                {
                    count += 1;
                }
            }
            return count;
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
            BuildWorkbench(session);
            SetStorage(session, ResourceKind.Wood, wood);
            SetStorage(session, ResourceKind.Stone, 0);
            SetStorage(session, ResourceKind.Food, 0);
            SetStorage(session, ResourceKind.Salvage, salvage);
            return session;
        }

        private static void BuildWorkbench(GameSession session)
        {
            if (session.HasStructure(StructureKind.Workbench))
            {
                return;
            }
            SetStorage(session, ResourceKind.Wood, 2);
            SetStorage(session, ResourceKind.Salvage, 1);
            Require(session.TryBuild(StructureKind.Workbench), "workbench fixture build");
        }

        private static void SetStorage(GameSession session, ResourceKind kind, int exact)
        {
            session.Grant(kind, exact - session.GetStorage(kind));
            Require(session.GetStorage(kind) == exact, kind + " normalized to " + exact);
        }

        private static string StorageFingerprint(GameSession session)
        {
            return "W" + session.GetStorage(ResourceKind.Wood) +
                   "/S" + session.GetStorage(ResourceKind.Stone) +
                   "/F" + session.GetStorage(ResourceKind.Food) +
                   "/D" + session.GetStorage(ResourceKind.Salvage);
        }

        private static CampPlacementRoomZone ModuleRoomZone(CampModuleDefinition definition)
        {
            float connectorX = definition.ModuleConnectorDisplayX;
            return new CampPlacementRoomZone(
                definition.RoomId,
                definition.GeneralFloorDisplayMinimumX,
                definition.GeneralFloorDisplayMaximumX,
                false,
                0f,
                0f,
                connectorX - 0.8f,
                connectorX + 0.8f,
                connectorX - 1.1f,
                connectorX + 1.1f);
        }

        private static float FindValidPlacementX(PrototypeCampPlacement placement, StructureKind kind, CampPlacementRoomZone room)
        {
            for (float x = room.BuildMinimumX; x <= room.BuildMaximumX + 0.001f; x += PrototypeCampPlacement.GridSize)
            {
                placement.SetCandidateX(x);
                if (placement.Validate(kind, placement.CandidateX) == CampPlacementValidity.Valid)
                {
                    return placement.CandidateX;
                }
            }
            throw new InvalidOperationException("No valid general-floor placement in " + room.RoomId);
        }

        private static bool IsLocalized(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.StartsWith("⟦", StringComparison.Ordinal);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, InstanceFlags);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, name);
            }
            return (T)field.GetValue(target);
        }

        private static object Invoke(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, InstanceFlags);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, name);
            }
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static string Product(
            List<ContractCheck> checks,
            string id,
            string matrix,
            string severity,
            string expected,
            Func<string> verification,
            string reproduction,
            string files)
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

        private static void KnownGap(
            List<ContractCheck> checks,
            string id,
            string matrix,
            string severity,
            string expected,
            bool passed,
            string actual,
            string reproduction,
            string files)
        {
            checks.Add(NewCheck(
                id,
                matrix,
                passed ? "PASS" : "EXPECTED_FAIL",
                passed ? "NONE" : "PRODUCT_EXPECTED_GAP",
                severity,
                expected,
                actual,
                reproduction,
                files));
        }

        private static void Infrastructure(
            List<ContractCheck> checks,
            string id,
            string matrix,
            string severity,
            string expected,
            Func<string> verification,
            string reproduction,
            string files)
        {
            try
            {
                checks.Add(NewCheck(id, matrix, "PASS", "NONE", severity, expected, verification(), reproduction, files));
            }
            catch (Exception exception)
            {
                checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected, exception.ToString(), reproduction, files));
            }
        }

        private static void Unverified(
            List<ContractCheck> checks,
            string id,
            string matrix,
            string severity,
            string expected,
            string actual,
            string reproduction,
            string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", "HARDWARE_GAP", severity, expected, actual, reproduction, files));
        }

        private static ContractCheck NewCheck(
            string id,
            string matrix,
            string status,
            string classification,
            string severity,
            string expected,
            string actual,
            string reproduction,
            string files)
        {
            return new ContractCheck
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

        private static ContractReport WriteReport(string stem, string title, DateTime started, List<ContractCheck> checks)
        {
            ContractReport report = new ContractReport
            {
                title = title,
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
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

        private static ModuleMatrixEvidence NewEvidence()
        {
            return new ModuleMatrixEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                screenshots = Array.Empty<string>(),
                joystickNames = Array.Empty<string>(),
                physicalGamepad = "UNVERIFIED"
            };
        }

        private static void WriteJson(string fileName, object value)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, fileName), JsonUtility.ToJson(value, true) + Environment.NewLine, Utf8NoBom);
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
