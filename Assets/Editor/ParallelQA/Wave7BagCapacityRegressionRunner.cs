using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using KimSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Independent, non-shipping Wave 7 red-first contracts. Product APIs are
    /// discovered by semantic reflection so the runner compiles before the bag
    /// upgrade exists and reports NOT_IMPLEMENTED instead of a compiler error.
    /// </summary>
    public static class Wave7BagCapacityRegressionRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave7.Play.Running";
        private const string PlayPassedKey = "ParallelQA.Wave7.Play.Passed";
        private const string PlayMessageKey = "ParallelQA.Wave7.Play.Message";
        private const int BaseCapacity = 4;
        private const int UpgradedCapacity = 6;
        private const int UpgradeWoodCost = 2;
        private const int UpgradeSalvageCost = 1;
        private const float ScreenMarginPixels = 4f;
        private const float MinimumGlyphPixels = 16f;
        private const float NormalContrast = 4.5f;
        private const float LargeContrast = 3f;
        private const float SignificantOverlap = 0.15f;

        private static bool playTickAttached;
        private static double playEarliestRunTime;
        private static double playTimeoutAt;

        [Serializable]
        public sealed class ContractCheck
        {
            public string id;
            public string matrixItem;
            public string status;
            public string classification;
            public string severity;
            public string expected;
            public string actual;
            public string reproduction;
            public string recommendedFiles;
        }

        [Serializable]
        public sealed class ContractReport
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string command;
            public string overall;
            public string productOverall;
            public string infrastructureOverall;
            public int passed;
            public int failed;
            public int notImplemented;
            public int unverified;
            public int infrastructureFailed;
            public ContractCheck[] checks;
        }

        [Serializable]
        public sealed class LayoutMetric
        {
            public string scenario;
            public string screenshot;
            public string hierarchy;
            public string text;
            public float glyphMedianPixels;
            public float left;
            public float bottom;
            public float right;
            public float top;
            public float contrastRatio;
            public bool boundsPass;
            public bool heightPass;
            public bool contrastPass;
            public bool overflowPass;
            public bool overlapPass = true;
            public string overlaps;

            public bool Passed
            {
                get { return boundsPass && heightPass && contrastPass && overflowPass && overlapPass; }
            }
        }

        [Serializable]
        public sealed class LayoutReport
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string implementationState;
            public string baselineOverall;
            public string upgradedOverall;
            public string overall;
            public int frames;
            public int metrics;
            public int failures;
            public LayoutMetric[] measurements;
        }

        private sealed class Observation
        {
            public bool Passed;
            public string Actual;

            public static Observation Product(bool passed, string actual)
            {
                return new Observation { Passed = passed, Actual = actual };
            }
        }

        private sealed class ResourceSnapshot
        {
            public int Wood;
            public int Salvage;
            public int Capacity;

            public override string ToString()
            {
                return "wood=" + Wood + " salvage=" + Salvage + " capacity=" + Capacity;
            }
        }

        private sealed class BagUpgradeApi
        {
            private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private readonly MemberInfo capacityMember;
            private readonly MethodInfo upgradeMethod;
            private readonly MethodInfo beginMethod;
            private readonly MethodInfo cancelMethod;

            public BagUpgradeApi()
            {
                Type type = typeof(GameSession);
                capacityMember = FindCapacityMember(type);
                upgradeMethod = FindBooleanMethod(type, name => Has(name, "bag") && Has(name, "upgrade"));
                beginMethod = FindMethod(type, name => Has(name, "bag") && Has(name, "upgrade") && HasAny(name, "begin", "open", "prepare"), upgradeMethod);
                cancelMethod = FindMethod(type, name => Has(name, "bag") && Has(name, "upgrade") && HasAny(name, "cancel", "abort"), null);
            }

            public bool Implemented
            {
                get { return capacityMember != null && upgradeMethod != null; }
            }

            public bool CancellationImplemented
            {
                get { return beginMethod != null && cancelMethod != null; }
            }

            public int Capacity(GameSession session)
            {
                if (capacityMember == null)
                {
                    return GameSession.BagSlotCount;
                }

                if (capacityMember is PropertyInfo property)
                {
                    return Convert.ToInt32(property.GetValue(session, null));
                }
                if (capacityMember is FieldInfo field)
                {
                    return Convert.ToInt32(field.GetValue(session));
                }
                if (capacityMember is MethodInfo method)
                {
                    return Convert.ToInt32(Invoke(method, session));
                }
                throw new InvalidOperationException("Unsupported capacity member: " + capacityMember);
            }

            public bool TryUpgrade(GameSession session)
            {
                Require(upgradeMethod != null, "bag upgrade action was discovered");
                return Convert.ToBoolean(Invoke(upgradeMethod, session));
            }

            public void BeginAndCancel(GameSession session)
            {
                Require(CancellationImplemented, "bag upgrade begin/cancel actions were discovered");
                Invoke(beginMethod, session);
                Invoke(cancelMethod, session);
            }

            public string Description
            {
                get
                {
                    return "capacity=" + Describe(capacityMember) +
                           " upgrade=" + Describe(upgradeMethod) +
                           " begin=" + Describe(beginMethod) +
                           " cancel=" + Describe(cancelMethod);
                }
            }

            private static MemberInfo FindCapacityMember(Type type)
            {
                List<MemberInfo> candidates = new List<MemberInfo>();
                candidates.AddRange(type.GetProperties(InstanceFlags)
                    .Where(property => property.PropertyType == typeof(int) && property.GetIndexParameters().Length == 0)
                    .Where(property => CapacityName(property.Name)));
                candidates.AddRange(type.GetFields(InstanceFlags)
                    .Where(field => field.FieldType == typeof(int))
                    .Where(field => CapacityName(field.Name)));
                candidates.AddRange(type.GetMethods(InstanceFlags)
                    .Where(method => method.ReturnType == typeof(int) && method.GetParameters().Length == 0)
                    .Where(method => CapacityName(method.Name)));
                return candidates.OrderBy(member => member.Name, StringComparer.Ordinal).FirstOrDefault();
            }

            private static bool CapacityName(string name)
            {
                return Has(name, "bag") && HasAny(name, "capacity", "slotcount", "slots");
            }

            private static MethodInfo FindBooleanMethod(Type type, Func<string, bool> predicate)
            {
                return type.GetMethods(InstanceFlags)
                    .Where(method => method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                    .Where(method => predicate(method.Name))
                    .Where(method => !method.Name.StartsWith("Can", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(method => method.Name.StartsWith("Try", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(method => method.Name, StringComparer.Ordinal)
                    .FirstOrDefault();
            }

            private static MethodInfo FindMethod(Type type, Func<string, bool> predicate, MethodInfo excluded)
            {
                return type.GetMethods(InstanceFlags)
                    .Where(method => method.GetParameters().Length == 0 && method != excluded)
                    .Where(method => predicate(method.Name))
                    .OrderBy(method => method.Name, StringComparer.Ordinal)
                    .FirstOrDefault();
            }

            private static object Invoke(MethodInfo method, object target)
            {
                try
                {
                    return method.Invoke(target, null);
                }
                catch (TargetInvocationException exception)
                {
                    throw exception.InnerException ?? exception;
                }
            }

            private static bool Has(string value, string token)
            {
                return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static bool HasAny(string value, params string[] tokens)
            {
                return tokens.Any(token => Has(value, token));
            }

            private static string Describe(MemberInfo member)
            {
                return member == null ? "<missing>" : member.MemberType + ":" + member.Name;
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
                return string.IsNullOrWhiteSpace(value) ? "manual-wave7" : Sanitize(value);
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
            BagUpgradeApi api = new BagUpgradeApi();

            Product(checks, "W7-01.new_game.base_capacity", "1", "P0",
                "A new game starts with 4 active slots and stack limit 2",
                () => VerifyBaseCapacity(api),
                "Create GameSession and inspect active capacity and stack limit.",
                "Assets/_Project/Scripts/Runtime/GameSession.cs");

            if (!api.Implemented)
            {
                NotImplemented(checks, "W7-API.capacity_upgrade", "2-7,9,11", "P0",
                    "Discoverable per-session active bag capacity and atomic bag upgrade action",
                    api.Description,
                    "Merge the Unity Wave 7 implementation, then rerun this command without editing the QA harness.");
                AddFeatureGapMatrix(checks, api.Description);
            }
            else
            {
                Product(checks, "W7-02.no_workbench.no_spend", "2", "P0",
                    "Exact upgrade resources without a built workbench reject the upgrade and spend nothing",
                    () => VerifyNoWorkbench(api),
                    "Create a camp session with wood 2/salvage 1 and no workbench, invoke the reflected upgrade action once.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");
                Product(checks, "W7-03a.insufficient.no_spend", "3", "P0",
                    "A built workbench with insufficient resources rejects the upgrade and spends nothing",
                    () => VerifyInsufficient(api),
                    "Build the workbench, leave wood 0/salvage 0, invoke upgrade, compare resources and capacity.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs");
                if (api.CancellationImplemented)
                {
                    Product(checks, "W7-03b.cancel.no_spend", "3", "P0",
                        "Opening and cancelling bag upgrade spends nothing and leaves capacity 4",
                        () => VerifyCancellation(api),
                        "Build the workbench, provide exact cost, invoke discovered begin then cancel actions.",
                        "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");
                }
                else
                {
                    NotImplemented(checks, "W7-03b.cancel.no_spend", "3", "P0",
                        "A discoverable begin/cancel path proves cancel is resource-invariant",
                        api.Description,
                        "Expose or retain a bag-upgrade confirmation begin/cancel path and rerun.");
                }
                Product(checks, "W7-04.exact_once.to_six", "4", "P0",
                    "Workbench + wood 2 + salvage 1 spends exactly once and activates exactly 6 slots",
                    () => VerifyExactUpgrade(api),
                    "Build workbench, provide exact cost, invoke upgrade once, compare resources and capacity.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs");
                Product(checks, "W7-05.repeat.no_spend", "5", "P0",
                    "A second upgrade attempt is rejected without spending or changing six-slot state",
                    () => VerifyRepeat(api),
                    "Complete one upgrade, add another exact cost, invoke upgrade again and compare snapshot.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs");
                Product(checks, "W7-06.persistence_and_reset", "6", "P0",
                    "Six slots survive a day transition while Reset/new game restores four slots",
                    () => VerifyPersistenceAndReset(api),
                    "Upgrade, complete an expedition/day transition, inspect capacity, then Reset and inspect again.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs");
                Product(checks, "W7-07a.locked_slots", "7", "P0",
                    "Slots 5 and 6 cannot receive or replace loot before upgrade",
                    () => VerifyLockedSlots(api),
                    "Fill four base slots, create pending loot, attempt replacement at indexes 4 and 5.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs");
                Product(checks, "W7-07b.extended_slot_operations", "7", "P0",
                    "Upgraded slots 5 and 6 support acquire, stack, replace, discard, and return transfer",
                    () => VerifyExtendedSlots(api),
                    "Upgrade, fill six slots deterministically, exercise pending replacement/discard, and return to camp.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs");
                Product(checks, "W7-08.localization_semantics", "8", "P1",
                    "Behavior-discovered upgrade keys have equivalent ko/en meaning and deterministic qps-long placeholder preservation",
                    () => VerifyLocalization(api),
                    "Trigger missing-workbench, insufficient, success, and repeat outcomes; format their keys in ko/en and pseudo-expand en.",
                    "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv; Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs");
                Product(checks, "W7-09.synthetic_dual_input", "9", "P1",
                    "Keyboard/mouse and synthetic gamepad actions can address all six slots with equivalent confirm/cancel semantics",
                    () => VerifySyntheticInput(api),
                    "Create shared action snapshots for keyboard and synthetic gamepad with slot index 5 and compare them.",
                    "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");
                Product(checks, "W7-11.natural_three_day_upgrade_rescue", "11", "P0",
                    "A grant/coordinate-warp-free natural Day 1-3 route buys the upgrade and reaches rescue",
                    () => VerifyNaturalThreeDayUpgradeRescue(api),
                    "Run the deterministic model route and source-audit this method for Grant/Warp calls.",
                    "Assets/_Project/Scripts/Runtime/GameSession.cs");
            }

            ContractReport report = WriteReport("wave7-edit-contracts", "Wave 7 bag capacity Edit contracts", started, checks);
            if (report.productOverall != "PASS" || report.infrastructureOverall != "PASS")
            {
                throw new InvalidOperationException("Wave 7 red-first Edit contracts are not green. See " + Path.Combine(EvidenceFolder, "wave7-edit-contracts.json"));
            }
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayPassedKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Wave 7 Play contracts did not complete.");
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
            playTimeoutAt = EditorApplication.timeSinceStartup + 120d;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 2d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 120d;
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
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · Timed out waiting for KimSurvivalPrototype.");
                StopPlayContracts();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;

            try
            {
                DateTime started = DateTime.UtcNow;
                List<ContractCheck> checks = new List<ContractCheck>();
                BagUpgradeApi api = new BagUpgradeApi();
                List<LayoutMetric> metrics = new List<LayoutMetric>();
                CaptureBaselineFrames(prototype, metrics);
                Product(checks, "W7-08a.baseline_4slot_layout", "8", "P2",
                    "Current four-slot ko/en bag text is at least 16 px and remains in bounds without overflow or overlap at 1280x800 and 1920x1080",
                    () => ObserveLayout(metrics, "baseline-4slot-"),
                    "Inspect the four fresh baseline-4slot PNGs and wave7-layout-metrics.json.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                if (!api.Implemented)
                {
                    NotImplemented(checks, "W7-08b.upgraded_bag_layout", "8", "P1",
                        "Pre/post upgrade bag UI passes ko/en/qps-long at 1280x800 and 1920x1080",
                        "bag upgrade API missing; baseline 4-slot captures recorded · " + api.Description,
                        "Merge the Wave 7 Unity implementation and rerun to activate upgraded layout measurements.");
                    NotImplemented(checks, "W7-09.ui_synthetic_gamepad", "9", "P1",
                        "Six visible bag slots support pointer click and synthetic gamepad submit",
                        "bag upgrade API missing; no six-slot UI state can be reached",
                        "Merge the Wave 7 Unity implementation and rerun.");
                }
                else
                {
                    Product(checks, "W7-08b.upgraded_bag_layout", "8", "P1",
                        "Pre/post upgrade bag UI passes ko/en/qps-long at 1280x800 and 1920x1080",
                        () => CaptureAndMeasureUpgradeFrames(prototype, api, metrics),
                        "Run Play contracts, inspect eight ko/en frames plus qps-long stress frames and wave7-layout-metrics.json.",
                        "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv");
                    Product(checks, "W7-09.ui_synthetic_gamepad", "9", "P1",
                        "Six visible bag slots support pointer click and synthetic gamepad submit",
                        () => VerifyPlayUiInput(prototype, api),
                        "Reach a six-slot full-bag state, click slot 5, then submit slot 6 through EventSystem ExecuteEvents.",
                        "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs");
                }

                string[] joystickNames = (Input.GetJoystickNames() ?? Array.Empty<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
                checks.Add(new ContractCheck
                {
                    id = "W7-HW.physical_gamepad",
                    matrixItem = "9",
                    status = "UNVERIFIED",
                    classification = "HARDWARE_GAP",
                    severity = "P1",
                    expected = "Human actuation on a physical gamepad completes upgrade, slots 5/6, discard, return, and rescue",
                    actual = joystickNames.Length == 0
                        ? "no non-empty joystick name exposed to Unity batch Play Mode"
                        : "device name observed but no human actuation captured: " + string.Join(" | ", joystickNames),
                    reproduction = "Run the Windows build with a physical controller and record device name/VID/PID plus human actuation.",
                    recommendedFiles = "manual release-candidate hardware evidence"
                });

                WriteLayoutReport(metrics, api.Implemented ? "MEASURED" : "NOT_IMPLEMENTED");
                ContractReport report = WriteReport("wave7-play-contracts", "Wave 7 bag capacity Play contracts", started, checks);
                bool passed = report.productOverall == "PASS" && report.infrastructureOverall == "PASS";
                SessionState.SetBool(PlayPassedKey, passed);
                SessionState.SetString(PlayMessageKey,
                    "Product=" + report.productOverall + " · Infrastructure=" + report.infrastructureOverall +
                    " · PhysicalGamepad=UNVERIFIED · Evidence=" + Path.Combine(EvidenceFolder, "wave7-play-contracts.json"));
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PlayPassedKey, false);
                SessionState.SetString(PlayMessageKey, "INFRA_FAIL · " + exception);
                WriteInfrastructureFailure(exception);
            }

            StopPlayContracts();
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

        private static void FinishPlayContracts()
        {
            bool passed = SessionState.GetBool(PlayPassedKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL · no Play result");
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave7-play-exit.txt"), message + Environment.NewLine, new UTF8Encoding(false));
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayPassedKey);
            SessionState.EraseString(PlayMessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static Observation VerifyBaseCapacity(BagUpgradeApi api)
        {
            GameSession session = new GameSession();
            int active = api.Capacity(session);
            bool passed = active == BaseCapacity && GameSession.StackLimit == 2;
            return Observation.Product(passed,
                "active=" + active + " legacyStaticSlots=" + GameSession.BagSlotCount + " stackLimit=" + GameSession.StackLimit + " · " + api.Description);
        }

        private static Observation VerifyNoWorkbench(BagUpgradeApi api)
        {
            GameSession session = new GameSession();
            session.Grant(ResourceKind.Salvage, UpgradeSalvageCost);
            ResourceSnapshot before = Snapshot(session, api);
            bool upgraded = api.TryUpgrade(session);
            ResourceSnapshot after = Snapshot(session, api);
            return Observation.Product(!upgraded && Same(before, after) && after.Capacity == BaseCapacity,
                "upgraded=" + upgraded + " before=" + before + " after=" + after + " message=" + session.LastMessage.Key);
        }

        private static Observation VerifyInsufficient(BagUpgradeApi api)
        {
            GameSession session = PrepareWorkbenchWithExactUpgradeCost(false);
            ResourceSnapshot before = Snapshot(session, api);
            bool upgraded = api.TryUpgrade(session);
            ResourceSnapshot after = Snapshot(session, api);
            return Observation.Product(!upgraded && Same(before, after) && after.Capacity == BaseCapacity,
                "upgraded=" + upgraded + " before=" + before + " after=" + after + " message=" + session.LastMessage.Key);
        }

        private static Observation VerifyCancellation(BagUpgradeApi api)
        {
            GameSession session = PrepareWorkbenchWithExactUpgradeCost(true);
            ResourceSnapshot before = Snapshot(session, api);
            api.BeginAndCancel(session);
            ResourceSnapshot after = Snapshot(session, api);
            return Observation.Product(Same(before, after) && after.Capacity == BaseCapacity,
                "before=" + before + " after=" + after + " message=" + session.LastMessage.Key + " · " + api.Description);
        }

        private static Observation VerifyExactUpgrade(BagUpgradeApi api)
        {
            GameSession session = PrepareWorkbenchWithExactUpgradeCost(true);
            ResourceSnapshot before = Snapshot(session, api);
            bool upgraded = api.TryUpgrade(session);
            ResourceSnapshot after = Snapshot(session, api);
            bool exact = before.Wood - after.Wood == UpgradeWoodCost && before.Salvage - after.Salvage == UpgradeSalvageCost;
            return Observation.Product(upgraded && exact && after.Capacity == UpgradedCapacity,
                "upgraded=" + upgraded + " before=" + before + " after=" + after + " message=" + session.LastMessage.Key);
        }

        private static Observation VerifyRepeat(BagUpgradeApi api)
        {
            GameSession session = PrepareUpgraded(api);
            session.Grant(ResourceKind.Wood, UpgradeWoodCost);
            session.Grant(ResourceKind.Salvage, UpgradeSalvageCost);
            ResourceSnapshot before = Snapshot(session, api);
            bool upgraded = api.TryUpgrade(session);
            ResourceSnapshot after = Snapshot(session, api);
            return Observation.Product(!upgraded && Same(before, after) && after.Capacity == UpgradedCapacity,
                "secondUpgrade=" + upgraded + " before=" + before + " after=" + after + " message=" + session.LastMessage.Key);
        }

        private static Observation VerifyPersistenceAndReset(BagUpgradeApi api)
        {
            GameSession session = PrepareUpgraded(api);
            Require(session.BeginSearch(), "persistence expedition begins");
            Require(session.ReturnToCamp(false), "persistence expedition returns");
            Require(session.EndDay(), "persistence day settles");
            int afterDay = api.Capacity(session);
            int day = session.Day;
            session.Reset();
            int afterReset = api.Capacity(session);
            return Observation.Product(day == 2 && afterDay == UpgradedCapacity && afterReset == BaseCapacity,
                "day=" + day + " capacityAfterDay=" + afterDay + " capacityAfterReset=" + afterReset);
        }

        private static Observation VerifyLockedSlots(BagUpgradeApi api)
        {
            GameSession session = new GameSession();
            Require(api.Capacity(session) == BaseCapacity, "locked-slot fixture starts at four");
            Require(session.BeginSearch(), "locked-slot expedition begins");
            Require(session.TryGather(ResourceKind.Wood, 4) == GatherResult.Added, "base slots wood");
            Require(session.TryGather(ResourceKind.Stone, 4) == GatherResult.Added, "base slots stone");
            GatherResult overflow = session.TryGather(ResourceKind.Food, 1);
            bool slotFive = session.ReplaceBagSlot(4);
            bool slotSix = session.ReplaceBagSlot(5);
            return Observation.Product(overflow == GatherResult.PendingSwap && session.HasPendingLoot && !slotFive && !slotSix,
                "overflow=" + overflow + " pending=" + session.HasPendingLoot + " replaceIndex4=" + slotFive + " replaceIndex5=" + slotSix);
        }

        private static Observation VerifyExtendedSlots(BagUpgradeApi api)
        {
            GameSession session = PrepareUpgraded(api);
            Require(session.BeginSearch(), "extended-slot expedition begins");
            Require(session.TryGather(ResourceKind.Wood, 4) == GatherResult.Added, "slots 1-2 wood");
            Require(session.TryGather(ResourceKind.Stone, 4) == GatherResult.Added, "slots 3-4 stone");
            Require(session.TryGather(ResourceKind.Food, 1) == GatherResult.Added, "slot 5 acquire");
            Require(session.TryGather(ResourceKind.Food, 1) == GatherResult.Added, "slot 5 stack");
            Require(session.TryGather(ResourceKind.Salvage, 1) == GatherResult.Added, "slot 6 acquire");
            Require(session.TryGather(ResourceKind.Salvage, 1) == GatherResult.Added, "slot 6 stack");

            BagStack slotFiveBefore = session.GetBagSlot(4);
            BagStack slotSixBefore = session.GetBagSlot(5);
            Require(session.TryGather(ResourceKind.Wood, 1) == GatherResult.PendingSwap, "full upgraded bag creates pending replacement");
            bool replaced = session.ReplaceBagSlot(4);
            BagStack slotFiveAfter = session.GetBagSlot(4);
            Require(session.TryGather(ResourceKind.Stone, 1) == GatherResult.PendingSwap, "second pending loot created");
            session.DiscardPendingLoot();
            bool discarded = !session.HasPendingLoot;

            int woodBefore = session.GetStorage(ResourceKind.Wood);
            int stoneBefore = session.GetStorage(ResourceKind.Stone);
            int salvageBefore = session.GetStorage(ResourceKind.Salvage);
            bool returned = session.ReturnToCamp(false);
            int woodDelta = session.GetStorage(ResourceKind.Wood) - woodBefore;
            int stoneDelta = session.GetStorage(ResourceKind.Stone) - stoneBefore;
            int salvageDelta = session.GetStorage(ResourceKind.Salvage) - salvageBefore;
            bool cleared = Enumerable.Range(0, UpgradedCapacity).All(index => session.GetBagSlot(index).IsEmpty);

            bool passed = slotFiveBefore.Kind == ResourceKind.Food && slotFiveBefore.Amount == 2 &&
                          slotSixBefore.Kind == ResourceKind.Salvage && slotSixBefore.Amount == 2 &&
                          replaced && slotFiveAfter.Kind == ResourceKind.Wood && slotFiveAfter.Amount == 1 &&
                          discarded && returned && cleared && woodDelta == 5 && stoneDelta == 4 && salvageDelta == 2;
            return Observation.Product(passed,
                "slot5=" + slotFiveBefore.Kind + "x" + slotFiveBefore.Amount + "->" + slotFiveAfter.Kind + "x" + slotFiveAfter.Amount +
                " slot6=" + slotSixBefore.Kind + "x" + slotSixBefore.Amount + " replaced=" + replaced + " discarded=" + discarded +
                " returnDelta=" + woodDelta + "/" + stoneDelta + "/" + salvageDelta + " cleared=" + cleared);
        }

        private static Observation VerifyLocalization(BagUpgradeApi api)
        {
            List<PrototypeLocalizedText> messages = new List<PrototypeLocalizedText>();

            GameSession noWorkbench = new GameSession();
            noWorkbench.Grant(ResourceKind.Salvage, 1);
            api.TryUpgrade(noWorkbench);
            messages.Add(noWorkbench.LastMessage);

            GameSession insufficient = PrepareWorkbenchWithExactUpgradeCost(false);
            api.TryUpgrade(insufficient);
            messages.Add(insufficient.LastMessage);

            GameSession success = PrepareWorkbenchWithExactUpgradeCost(true);
            Require(api.TryUpgrade(success), "localized success upgrade");
            messages.Add(success.LastMessage);
            api.TryUpgrade(success);
            messages.Add(success.LastMessage);

            string[] keys = messages.Select(message => message.Key).Where(key => !string.IsNullOrWhiteSpace(key)).Distinct().ToArray();
            Require(keys.Length >= 4, "four distinct behavior feedback keys");
            List<string> ko = new List<string>();
            List<string> en = new List<string>();
            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                foreach (PrototypeLocalizedText message in messages)
                {
                    localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                    ko.Add(localization.Format(message));
                    localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                    en.Add(localization.Format(message));
                }
            }

            string tsvPath = Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            Dictionary<string, string[]> rows = ReadTsv(tsvPath);
            bool parity = keys.All(key => rows.ContainsKey(key) && rows[key].Length >= 3 &&
                                         Placeholders(rows[key][1]).SetEquals(Placeholders(rows[key][2])));
            bool pseudo = keys.All(key => rows.ContainsKey(key) &&
                                         Placeholders(rows[key][2]).SetEquals(Placeholders(Wave3VisualGate.ExpandPseudoLong(rows[key][2]))) &&
                                         Wave3VisualGate.ExpandPseudoLong(rows[key][2]).Length >= Mathf.CeilToInt(rows[key][2].Length * 1.35f));
            bool semantic = ko.Any(value => value.Contains("작업대")) && en.Any(value => ContainsIgnoreCase(value, "workbench")) &&
                            ko.Any(value => value.Contains("나무") && value.Contains("표류물")) &&
                            en.Any(value => ContainsIgnoreCase(value, "wood") && ContainsIgnoreCase(value, "salvage")) &&
                            ko.Any(value => value.Contains("6")) && en.Any(value => value.Contains("6"));
            bool rawKeyAbsent = ko.Concat(en).All(value => !value.StartsWith("⟦", StringComparison.Ordinal));
            return Observation.Product(parity && pseudo && semantic && rawKeyAbsent,
                "keys=" + string.Join("|", keys) + " parity=" + parity + " qpsPlaceholder=" + pseudo + " semantic=" + semantic +
                " ko=" + Normalize(string.Join(" | ", ko)) + " en=" + Normalize(string.Join(" | ", en)));
        }

        private static Observation VerifySyntheticInput(BagUpgradeApi api)
        {
            GameSession session = PrepareUpgraded(api);
            PrototypePlayerActions keyboard = PrototypePlayerActions.FromRaw(new PrototypeRawInput
            {
                KeyboardInteract = true,
                KeyboardCancel = true,
                BagSlotIndex = 5
            });
            PrototypePlayerActions gamepad = PrototypePlayerActions.FromRaw(new PrototypeRawInput
            {
                GamepadInteract = true,
                GamepadCancel = true,
                BagSlotIndex = 5
            });
            bool passed = api.Capacity(session) == UpgradedCapacity &&
                          keyboard.BagSlotIndex == 5 && gamepad.BagSlotIndex == 5 &&
                          keyboard.InteractPressed && gamepad.InteractPressed &&
                          keyboard.CancelPressed && gamepad.CancelPressed;
            return Observation.Product(passed,
                "capacity=" + api.Capacity(session) + " keyboardSlot=" + keyboard.BagSlotIndex + " gamepadSlot=" + gamepad.BagSlotIndex +
                " interact=" + keyboard.InteractPressed + "/" + gamepad.InteractPressed +
                " cancel=" + keyboard.CancelPressed + "/" + gamepad.CancelPressed);
        }

        private static Observation VerifyNaturalThreeDayUpgradeRescue(BagUpgradeApi api)
        {
            string source = File.ReadAllText(Path.Combine(ProjectRoot, "Assets", "Editor", "ParallelQA", "Wave7BagCapacityRegressionRunner.cs"));
            string method = ExtractMethodSource(source,
                "private static Observation VerifyNaturalThreeDayUpgradeRescue(BagUpgradeApi api)",
                "private static GameSession PrepareWorkbenchWithExactUpgradeCost(bool provideUpgradeCost)");
            bool sourceClean = !method.Contains("." + "Grant" + "(", StringComparison.Ordinal) &&
                               !method.Contains("." + "Warp" + "(", StringComparison.Ordinal);

            GameSession session = new GameSession();
            Require(session.BeginSearch(), "day 1 search");
            Require(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 1 wood");
            Require(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 1 salvage");
            Require(session.TryGather(ResourceKind.Stone, 2) == GatherResult.Added, "day 1 stone");
            Require(session.ReturnToCamp(false), "day 1 return");
            Require(session.TryBuild(StructureKind.Workbench), "day 1 workbench");
            Require(api.TryUpgrade(session) && api.Capacity(session) == UpgradedCapacity, "day 1 bag upgrade");
            Require(session.EndDay(), "day 1 settlement");

            Require(session.BeginSearch(), "day 2 search");
            Require(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 2 wood A");
            Require(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 2 wood B");
            Require(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added, "day 2 wood C");
            Require(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 2 salvage A");
            Require(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 2 salvage B");
            Require(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "day 2 salvage C");
            Require(session.ReturnToCamp(false), "day 2 return");
            Require(session.TryResearch(TechKind.Rope) && session.TryCraft(TechKind.Rope), "day 2 rope");
            Require(session.TryResearch(TechKind.StoneAxe) && session.TryCraft(TechKind.StoneAxe), "day 2 axe");
            Require(session.TryUpgradeSignal() && session.SignalStage == 1, "day 2 signal stage 1");
            Require(session.EndDay(), "day 2 settlement");

            Require(session.BeginSearch(), "day 3 search");
            Require(session.TryGather(ResourceKind.Salvage, 1) == GatherResult.Added, "day 3 salvage");
            Require(session.ReturnToCamp(false), "day 3 return");
            Require(session.TryUpgradeSignal(), "day 3 signal stage 2");
            bool passed = sourceClean && session.Day == 3 && api.Capacity(session) == UpgradedCapacity &&
                          session.Result == RunResult.Rescued && session.Phase == GamePhase.Result;
            return Observation.Product(passed,
                "sourceGrantWarpFree=" + sourceClean + " day=" + session.Day + " capacity=" + api.Capacity(session) +
                " signal=" + session.SignalStage + " result=" + session.Result + " phase=" + session.Phase);
        }

        private static GameSession PrepareWorkbenchWithExactUpgradeCost(bool provideUpgradeCost)
        {
            GameSession session = new GameSession();
            session.Grant(ResourceKind.Salvage, 1);
            Require(session.TryBuild(StructureKind.Workbench), "workbench fixture");
            Require(session.GetStorage(ResourceKind.Wood) == 0 && session.GetStorage(ResourceKind.Salvage) == 0, "workbench leaves empty upgrade resources");
            if (provideUpgradeCost)
            {
                session.Grant(ResourceKind.Wood, UpgradeWoodCost);
                session.Grant(ResourceKind.Salvage, UpgradeSalvageCost);
            }
            return session;
        }

        private static GameSession PrepareUpgraded(BagUpgradeApi api)
        {
            GameSession session = PrepareWorkbenchWithExactUpgradeCost(true);
            Require(api.TryUpgrade(session), "upgrade fixture action");
            Require(api.Capacity(session) == UpgradedCapacity, "upgrade fixture capacity six");
            return session;
        }

        private static ResourceSnapshot Snapshot(GameSession session, BagUpgradeApi api)
        {
            return new ResourceSnapshot
            {
                Wood = session.GetStorage(ResourceKind.Wood),
                Salvage = session.GetStorage(ResourceKind.Salvage),
                Capacity = api.Capacity(session)
            };
        }

        private static bool Same(ResourceSnapshot left, ResourceSnapshot right)
        {
            return left.Wood == right.Wood && left.Salvage == right.Salvage && left.Capacity == right.Capacity;
        }

        private static void AddFeatureGapMatrix(List<ContractCheck> checks, string apiDescription)
        {
            string actual = "NOT_IMPLEMENTED at baseline · " + apiDescription;
            NotImplemented(checks, "W7-02.no_workbench.no_spend", "2", "P0", "No-workbench rejection and resource invariance", actual, "Merge implementation and rerun.");
            NotImplemented(checks, "W7-03.insufficient_cancel.no_spend", "3", "P0", "Insufficient-resource and cancel resource invariance", actual, "Merge implementation and rerun.");
            NotImplemented(checks, "W7-04.exact_once.to_six", "4", "P0", "Exact one-time wood 2/salvage 1 spend activates six slots", actual, "Merge implementation and rerun.");
            NotImplemented(checks, "W7-05.repeat.no_spend", "5", "P0", "Second attempt is rejected without spending", actual, "Merge implementation and rerun.");
            NotImplemented(checks, "W7-06.persistence_and_reset", "6", "P0", "Day persistence and new-game reset", actual, "Merge implementation and rerun.");
            NotImplemented(checks, "W7-07.extended_slot_operations", "7", "P0", "Locked pre-upgrade slots and full operations on slots 5/6", actual, "Merge implementation and rerun.");
            NotImplemented(checks, "W7-08.localization_semantics", "8", "P1", "ko/en/qps-long keys, placeholders, and meaning", actual, "Merge localization/UI implementation and rerun.");
            NotImplemented(checks, "W7-09.synthetic_dual_input", "9", "P1", "Keyboard/mouse and synthetic gamepad operate all six slots", actual, "Merge input/UI implementation and rerun.");
            NotImplemented(checks, "W7-11.natural_three_day_upgrade_rescue", "11", "P0", "Grant/warp-free three-day upgrade and rescue route", actual, "Merge implementation and rerun.");
        }

        private static void CaptureBaselineFrames(KimSurvivalPrototype prototype, List<LayoutMetric> metrics)
        {
            PrototypeLocalization localization = GetPrivateField<PrototypeLocalization>(prototype, "localization");
            GameObject bagPanel = GetPrivateField<GameObject>(prototype, "bagPanel");
            Camera camera = GetPrivateField<Camera>(prototype, "worldCamera");
            foreach (string locale in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode })
            {
                localization.SetLocale(locale, false);
                InvokePrivate(prototype, "RefreshAll");
                foreach (Tuple<int, int> size in CaptureSizes())
                {
                    string scenario = "baseline-4slot-" + locale + "-" + size.Item1 + "x" + size.Item2;
                    string screenshot = scenario + ".png";
                    string path = Path.Combine(EvidenceFolder, screenshot);
                    prototype.CaptureVerificationPng(path, size.Item1, size.Item2);
                    metrics.AddRange(MeasureTexts(scenario, screenshot, camera, RelevantBagTexts(bagPanel, null), size.Item1, size.Item2, true));
                }
            }
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            InvokePrivate(prototype, "RefreshAll");
        }

        private static Observation CaptureAndMeasureUpgradeFrames(KimSurvivalPrototype prototype, BagUpgradeApi api, List<LayoutMetric> metrics)
        {
            GameSession session = prototype.Session;
            session.Reset();
            session.Grant(ResourceKind.Salvage, 1);
            Require(session.TryBuild(StructureKind.Workbench), "visual workbench fixture");
            session.Grant(ResourceKind.Wood, UpgradeWoodCost);
            session.Grant(ResourceKind.Salvage, UpgradeSalvageCost);
            Require(api.TryUpgrade(session), "visual upgrade fixture");
            Require(api.Capacity(session) == UpgradedCapacity, "visual capacity six");

            PrototypeLocalization localization = GetPrivateField<PrototypeLocalization>(prototype, "localization");
            GameObject bagPanel = GetPrivateField<GameObject>(prototype, "bagPanel");
            Camera camera = GetPrivateField<Camera>(prototype, "worldCamera");
            Button upgradeButton = FindUpgradeButton(prototype);
            List<LayoutMetric> upgradedMetrics = new List<LayoutMetric>();

            foreach (string locale in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode })
            {
                localization.SetLocale(locale, false);
                InvokePrivate(prototype, "RefreshAll");
                foreach (Tuple<int, int> size in CaptureSizes())
                {
                    string scenario = "upgraded-6slot-" + locale + "-" + size.Item1 + "x" + size.Item2;
                    string screenshot = scenario + ".png";
                    prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, screenshot), size.Item1, size.Item2);
                    upgradedMetrics.AddRange(MeasureTexts(scenario, screenshot, camera, RelevantBagTexts(bagPanel, upgradeButton), size.Item1, size.Item2, true));
                }
            }

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            InvokePrivate(prototype, "RefreshAll");
            TMP_Text[] relevant = RelevantBagTexts(bagPanel, upgradeButton);
            Dictionary<TMP_Text, string> originals = relevant.ToDictionary(text => text, text => text.text);
            foreach (TMP_Text text in relevant)
            {
                text.text = Wave3VisualGate.ExpandPseudoLong(text.text);
            }
            foreach (Tuple<int, int> size in CaptureSizes())
            {
                string scenario = "upgraded-6slot-qps-long-" + size.Item1 + "x" + size.Item2;
                string screenshot = scenario + ".png";
                prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, screenshot), size.Item1, size.Item2);
                upgradedMetrics.AddRange(MeasureTexts(scenario, screenshot, camera, relevant, size.Item1, size.Item2, true));
            }
            foreach (KeyValuePair<TMP_Text, string> pair in originals) pair.Key.text = pair.Value;
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            InvokePrivate(prototype, "RefreshAll");

            metrics.AddRange(upgradedMetrics);
            bool enough = RelevantBagTexts(bagPanel, upgradeButton).Length >= UpgradedCapacity + 1;
            int failures = upgradedMetrics.Count(metric => !metric.Passed);
            return Observation.Product(enough && upgradedMetrics.Count > 0 && failures == 0,
                "relevantTexts=" + RelevantBagTexts(bagPanel, upgradeButton).Length + " metrics=" + upgradedMetrics.Count + " failures=" + failures +
                " upgradeButton=" + (upgradeButton == null ? "<missing>" : upgradeButton.name));
        }

        private static Observation VerifyPlayUiInput(KimSurvivalPrototype prototype, BagUpgradeApi api)
        {
            GameSession session = prototype.Session;
            session.Reset();
            session.Grant(ResourceKind.Salvage, 1);
            Require(session.TryBuild(StructureKind.Workbench), "UI input workbench fixture");
            session.Grant(ResourceKind.Wood, UpgradeWoodCost);
            session.Grant(ResourceKind.Salvage, UpgradeSalvageCost);
            Require(api.TryUpgrade(session), "UI input upgrade fixture");
            Require(session.BeginSearch(), "UI input search fixture");
            Require(session.TryGather(ResourceKind.Wood, 4) == GatherResult.Added, "UI input wood");
            Require(session.TryGather(ResourceKind.Stone, 4) == GatherResult.Added, "UI input stone");
            Require(session.TryGather(ResourceKind.Food, 2) == GatherResult.Added, "UI input food");
            Require(session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added, "UI input salvage");
            InvokePrivate(prototype, "RefreshAll");

            List<Button> buttons = GetPrivateField<List<Button>>(prototype, "bagButtons");
            Require(buttons.Count >= UpgradedCapacity, "six visible bag buttons");
            Require(session.TryGather(ResourceKind.Wood, 1) == GatherResult.PendingSwap, "pointer replacement pending");
            buttons[4].onClick.Invoke();
            bool pointer = !session.HasPendingLoot && session.GetBagSlot(4).Kind == ResourceKind.Wood;

            Require(session.TryGather(ResourceKind.Stone, 1) == GatherResult.PendingSwap, "synthetic gamepad replacement pending");
            EventSystem.current.SetSelectedGameObject(buttons[5].gameObject);
            ExecuteEvents.Execute(buttons[5].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            bool gamepad = !session.HasPendingLoot && session.GetBagSlot(5).Kind == ResourceKind.Stone;
            return Observation.Product(pointer && gamepad,
                "bagButtons=" + buttons.Count + " pointerSlot5=" + pointer + " syntheticGamepadSlot6=" + gamepad);
        }

        private static IEnumerable<Tuple<int, int>> CaptureSizes()
        {
            yield return Tuple.Create(1280, 800);
            yield return Tuple.Create(1920, 1080);
        }

        private static Button FindUpgradeButton(KimSurvivalPrototype prototype)
        {
            return prototype.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(field => typeof(Button).IsAssignableFrom(field.FieldType))
                .Where(field => field.Name.IndexOf("bag", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                field.Name.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(field => field.GetValue(prototype) as Button)
                .FirstOrDefault(button => button != null);
        }

        private static TMP_Text[] RelevantBagTexts(GameObject bagPanel, Button upgradeButton)
        {
            HashSet<TMP_Text> texts = new HashSet<TMP_Text>();
            if (bagPanel != null)
            {
                foreach (TMP_Text text in bagPanel.GetComponentsInChildren<TMP_Text>(true)) texts.Add(text);
            }
            if (upgradeButton != null)
            {
                foreach (TMP_Text text in upgradeButton.GetComponentsInChildren<TMP_Text>(true)) texts.Add(text);
            }
            return texts.Where(text => text != null && text.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(text.text)).ToArray();
        }

        private static List<LayoutMetric> MeasureTexts(string scenario, string screenshot, Camera camera, TMP_Text[] texts, int width, int height, bool gated)
        {
            List<LayoutMetric> metrics = new List<LayoutMetric>();
            float previousAspect = camera.aspect;
            camera.aspect = width / (float)height;
            try
            {
                foreach (TMP_Text text in texts)
                {
                    text.ForceMeshUpdate(true, true);
                    List<float> heights = new List<float>();
                    float minX = float.MaxValue;
                    float minY = float.MaxValue;
                    float maxX = float.MinValue;
                    float maxY = float.MinValue;
                    for (int index = 0; index < text.textInfo.characterCount; index += 1)
                    {
                        TMP_CharacterInfo character = text.textInfo.characterInfo[index];
                        if (!character.isVisible) continue;
                        Vector2[] points =
                        {
                            Project(camera, text.transform.TransformPoint(character.bottomLeft), width, height),
                            Project(camera, text.transform.TransformPoint(character.topLeft), width, height),
                            Project(camera, text.transform.TransformPoint(character.topRight), width, height),
                            Project(camera, text.transform.TransformPoint(character.bottomRight), width, height)
                        };
                        heights.Add(points.Max(point => point.y) - points.Min(point => point.y));
                        minX = Mathf.Min(minX, points.Min(point => point.x));
                        minY = Mathf.Min(minY, points.Min(point => point.y));
                        maxX = Mathf.Max(maxX, points.Max(point => point.x));
                        maxY = Mathf.Max(maxY, points.Max(point => point.y));
                    }
                    if (heights.Count == 0) continue;
                    heights.Sort();
                    float median = heights[heights.Count / 2];
                    Color background = NearestBackground(text);
                    Color foreground = Composite(text.color, background);
                    float contrast = Contrast(foreground, background);
                    float requiredContrast = median >= 24f ? LargeContrast : NormalContrast;
                    metrics.Add(new LayoutMetric
                    {
                        scenario = scenario,
                        screenshot = screenshot,
                        hierarchy = HierarchyPath(text.transform),
                        text = Normalize(text.text),
                        glyphMedianPixels = median,
                        left = minX,
                        bottom = minY,
                        right = maxX,
                        top = maxY,
                        contrastRatio = contrast,
                        boundsPass = !gated || (minX >= ScreenMarginPixels && minY >= ScreenMarginPixels && maxX <= width - ScreenMarginPixels && maxY <= height - ScreenMarginPixels),
                        heightPass = !gated || median >= MinimumGlyphPixels,
                        contrastPass = !gated || contrast >= requiredContrast,
                        overflowPass = !gated || !text.isTextOverflowing,
                        overlapPass = true,
                        overlaps = string.Empty
                    });
                }
            }
            finally
            {
                camera.aspect = previousAspect;
            }

            if (gated)
            {
                for (int first = 0; first < metrics.Count; first += 1)
                {
                    for (int second = first + 1; second < metrics.Count; second += 1)
                    {
                        Rect left = Rect.MinMaxRect(metrics[first].left, metrics[first].bottom, metrics[first].right, metrics[first].top);
                        Rect right = Rect.MinMaxRect(metrics[second].left, metrics[second].bottom, metrics[second].right, metrics[second].top);
                        Rect intersection = Intersect(left, right);
                        float denominator = Mathf.Max(1f, Mathf.Min(left.width * left.height, right.width * right.height));
                        float ratio = intersection.width * intersection.height / denominator;
                        if (ratio < SignificantOverlap) continue;
                        metrics[first].overlapPass = false;
                        metrics[second].overlapPass = false;
                        metrics[first].overlaps = Append(metrics[first].overlaps, metrics[second].hierarchy + "=" + (ratio * 100f).ToString("0.0") + "%");
                        metrics[second].overlaps = Append(metrics[second].overlaps, metrics[first].hierarchy + "=" + (ratio * 100f).ToString("0.0") + "%");
                    }
                }
            }
            return metrics;
        }

        private static void WriteLayoutReport(List<LayoutMetric> metrics, string implementationState)
        {
            LayoutMetric[] baseline = metrics.Where(metric => metric.scenario.StartsWith("baseline-4slot-", StringComparison.Ordinal)).ToArray();
            LayoutMetric[] upgraded = metrics.Where(metric => metric.scenario.StartsWith("upgraded-6slot-", StringComparison.Ordinal)).ToArray();
            string baselineOverall = baseline.Length > 0 && baseline.All(metric => metric.Passed) ? "PASS" : "FAIL";
            string upgradedOverall = implementationState == "NOT_IMPLEMENTED"
                ? "NOT_IMPLEMENTED"
                : (upgraded.Length > 0 && upgraded.All(metric => metric.Passed) ? "PASS" : "FAIL");
            LayoutReport report = new LayoutReport
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                implementationState = implementationState,
                baselineOverall = baselineOverall,
                upgradedOverall = upgradedOverall,
                overall = baselineOverall == "PASS" && upgradedOverall == "PASS"
                    ? "PASS"
                    : (baselineOverall == "PASS" && upgradedOverall == "NOT_IMPLEMENTED" ? "NOT_IMPLEMENTED" : "FAIL"),
                frames = metrics.Select(metric => metric.scenario).Distinct().Count(),
                metrics = metrics.Count,
                failures = metrics.Count(metric => !metric.Passed),
                measurements = metrics.ToArray()
            };
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave7-layout-metrics.json"), JsonUtility.ToJson(report, true) + Environment.NewLine, new UTF8Encoding(false));

            StringBuilder text = new StringBuilder();
            text.AppendLine("Wave 7 bag layout metrics");
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Unity: " + Application.unityVersion);
            text.AppendLine("Implementation: " + implementationState);
            text.AppendLine("Baseline 4-slot: " + baselineOverall);
            text.AppendLine("Upgraded 6-slot: " + upgradedOverall);
            text.AppendLine("Overall: " + report.overall);
            text.AppendLine("Frames: " + report.frames + " · Metrics: " + report.metrics + " · Failures: " + report.failures);
            text.AppendLine("Thresholds: >=16 px median glyph, 4 px viewport margin, contrast >=4.5:1 (<24 px) or >=3:1, significant text overlap <15%, TMP overflow false.");
            foreach (LayoutMetric metric in metrics.Where(metric => !metric.Passed))
            {
                text.AppendLine("FAIL · " + metric.scenario + " · " + metric.hierarchy + " · glyph=" + metric.glyphMedianPixels.ToString("0.0") +
                                " contrast=" + metric.contrastRatio.ToString("0.00") + " bounds=" + metric.left.ToString("0.0") + "," + metric.bottom.ToString("0.0") +
                                "-" + metric.right.ToString("0.0") + "," + metric.top.ToString("0.0") + " overlaps=" + metric.overlaps);
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave7-layout-metrics.txt"), text.ToString(), new UTF8Encoding(false));
        }

        private static Observation ObserveLayout(List<LayoutMetric> metrics, string scenarioPrefix)
        {
            LayoutMetric[] selected = metrics.Where(metric => metric.scenario.StartsWith(scenarioPrefix, StringComparison.Ordinal)).ToArray();
            LayoutMetric[] failures = selected.Where(metric => !metric.Passed).ToArray();
            string details = failures.Length == 0
                ? "none"
                : string.Join(" | ", failures.Select(metric => metric.scenario + ":" + metric.text +
                    " glyph=" + metric.glyphMedianPixels.ToString("0.0") + "px bounds=" + metric.boundsPass +
                    " contrast=" + metric.contrastRatio.ToString("0.00") + " overflow=" + metric.overflowPass + " overlap=" + metric.overlapPass));
            return Observation.Product(selected.Length > 0 && failures.Length == 0,
                "metrics=" + selected.Length + " failures=" + failures.Length + " · " + details);
        }

        private static Dictionary<string, string[]> ReadTsv(string path)
        {
            return File.ReadAllLines(path, Encoding.UTF8)
                .Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('\t'))
                .Where(columns => columns.Length >= 3)
                .GroupBy(columns => columns[0], StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static HashSet<string> Placeholders(string value)
        {
            return new HashSet<string>(Regex.Matches(value ?? string.Empty, @"\{[^{}]+\}").Cast<Match>().Select(match => match.Value), StringComparer.Ordinal);
        }

        private static void Product(List<ContractCheck> checks, string id, string matrixItem, string severity, string expected,
            Func<Observation> action, string reproduction, string recommendedFiles)
        {
            try
            {
                Observation observation = action();
                checks.Add(new ContractCheck
                {
                    id = id,
                    matrixItem = matrixItem,
                    status = observation.Passed ? "PASS" : "FAIL",
                    classification = observation.Passed ? "NONE" : "PRODUCT_DEFECT",
                    severity = severity,
                    expected = expected,
                    actual = observation.Actual,
                    reproduction = reproduction,
                    recommendedFiles = recommendedFiles
                });
            }
            catch (Exception exception)
            {
                checks.Add(new ContractCheck
                {
                    id = id,
                    matrixItem = matrixItem,
                    status = "FAIL",
                    classification = "PRODUCT_DEFECT",
                    severity = severity,
                    expected = expected,
                    actual = "fixture/action failed: " + exception.GetType().Name + " · " + exception.Message,
                    reproduction = reproduction,
                    recommendedFiles = recommendedFiles
                });
            }
        }

        private static void NotImplemented(List<ContractCheck> checks, string id, string matrixItem, string severity, string expected, string actual, string reproduction)
        {
            checks.Add(new ContractCheck
            {
                id = id,
                matrixItem = matrixItem,
                status = "NOT_IMPLEMENTED",
                classification = "PRODUCT_GAP",
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = "Assets/_Project/Scripts/Runtime/GameSession.cs; Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs; Assets/_Project/Scripts/Localization/PrototypeStrings.tsv"
            });
        }

        private static ContractReport WriteReport(string prefix, string title, DateTime started, List<ContractCheck> checks)
        {
            Directory.CreateDirectory(EvidenceFolder);
            ContractReport report = new ContractReport
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                command = string.Join(" ", Environment.GetCommandLineArgs().Select(Quote)),
                passed = checks.Count(check => check.status == "PASS"),
                failed = checks.Count(check => check.status == "FAIL"),
                notImplemented = checks.Count(check => check.status == "NOT_IMPLEMENTED"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"),
                infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL"),
                checks = checks.ToArray()
            };
            report.productOverall = report.failed == 0 && report.notImplemented == 0 ? "PASS" : "FAIL";
            report.infrastructureOverall = report.infrastructureFailed == 0 ? "PASS" : "FAIL";
            report.overall = report.productOverall == "PASS" && report.infrastructureOverall == "PASS" ? "PASS" : "FAIL";
            File.WriteAllText(Path.Combine(EvidenceFolder, prefix + ".json"), JsonUtility.ToJson(report, true) + Environment.NewLine, new UTF8Encoding(false));

            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + report.runId);
            text.AppendLine("Baseline: " + report.baselineCommit);
            text.AppendLine("Unity: " + report.unityVersion);
            text.AppendLine("Product: " + report.productOverall);
            text.AppendLine("Infrastructure: " + report.infrastructureOverall);
            text.AppendLine("Counts: PASS=" + report.passed + " FAIL=" + report.failed + " NOT_IMPLEMENTED=" + report.notImplemented +
                            " INFRA_FAIL=" + report.infrastructureFailed + " UNVERIFIED=" + report.unverified);
            foreach (ContractCheck check in checks)
            {
                text.AppendLine(check.status + " · " + check.classification + " · " + check.severity + " · " + check.id + " · " + Normalize(check.actual));
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, prefix + ".txt"), text.ToString(), new UTF8Encoding(false));
            return report;
        }

        private static void WriteInfrastructureFailure(Exception exception)
        {
            List<ContractCheck> checks = new List<ContractCheck>
            {
                new ContractCheck
                {
                    id = "W7-INFRA.play_execution",
                    matrixItem = "8-9",
                    status = "INFRA_FAIL",
                    classification = "TEST_INFRASTRUCTURE",
                    severity = "P0",
                    expected = "Play Mode scene, capture, and input runner completes",
                    actual = exception.GetType().Name + " · " + exception.Message,
                    reproduction = "Run Wave7BagCapacityRegressionRunner.RunPlayContracts outside the Codex sandbox.",
                    recommendedFiles = "Assets/Editor/ParallelQA/Wave7BagCapacityRegressionRunner.cs"
                }
            };
            WriteReport("wave7-play-contracts", "Wave 7 Play infrastructure failure", DateTime.UtcNow, checks);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Require(field != null, "private field exists: " + fieldName);
            object value = field.GetValue(target);
            Require(value is T, "private field type: " + fieldName + " -> " + typeof(T).Name);
            return (T)value;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Require(method != null, "method exists: " + methodName);
            try
            {
                method.Invoke(target, null);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static Vector2 Project(Camera camera, Vector3 world, int width, int height)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            return new Vector2(viewport.x * width, viewport.y * height);
        }

        private static Color NearestBackground(TMP_Text text)
        {
            Transform cursor = text.transform.parent;
            while (cursor != null)
            {
                Image image = cursor.GetComponent<Image>();
                if (image != null && image.color.a >= 0.5f) return image.color;
                cursor = cursor.parent;
            }
            return Color.black;
        }

        private static Color Composite(Color foreground, Color background)
        {
            return new Color(
                foreground.r * foreground.a + background.r * (1f - foreground.a),
                foreground.g * foreground.a + background.g * (1f - foreground.a),
                foreground.b * foreground.a + background.b * (1f - foreground.a),
                1f);
        }

        private static float Contrast(Color foreground, Color background)
        {
            float lighter = Mathf.Max(Luminance(foreground), Luminance(background));
            float darker = Mathf.Min(Luminance(foreground), Luminance(background));
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float Luminance(Color color)
        {
            return 0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static Rect Intersect(Rect left, Rect right)
        {
            float xMin = Mathf.Max(left.xMin, right.xMin);
            float yMin = Mathf.Max(left.yMin, right.yMin);
            float xMax = Mathf.Min(left.xMax, right.xMax);
            float yMax = Mathf.Min(left.yMax, right.yMax);
            return xMax <= xMin || yMax <= yMin ? new Rect() : Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static string HierarchyPath(Transform transform)
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

        private static string ExtractMethodSource(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, StringComparison.Ordinal);
            int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
            Require(start >= 0 && end > start, "source-audit method boundaries");
            return source.Substring(start, end - start);
        }

        private static bool ContainsIgnoreCase(string value, string token)
        {
            return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Append(string current, string value)
        {
            return string.IsNullOrEmpty(current) ? value : current + " | " + value;
        }

        private static string Quote(string value)
        {
            return value.IndexOf(' ') >= 0 ? "\"" + value.Replace("\"", "\\\"") + "\"" : value;
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
        }

        private static string Sanitize(string value)
        {
            return Regex.Replace(value, @"[^A-Za-z0-9_.-]", "_");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
