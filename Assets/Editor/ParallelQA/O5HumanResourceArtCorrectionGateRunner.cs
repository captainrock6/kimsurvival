using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace ParallelQA
{
    public static class O5HumanResourceArtCorrectionGateRunner
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        [Serializable]
        private sealed class Check
        {
            public string Id = string.Empty;
            public bool Passed;
            public string Detail = string.Empty;
        }

        [Serializable]
        private sealed class Report
        {
            public int SchemaVersion = 1;
            public string Title = "O5 human resource and art correction gate";
            public string UnityVersion = string.Empty;
            public bool Passed;
            public Check[] Checks = Array.Empty<Check>();
        }

        [MenuItem("Kim Survival/QA/O5 Human Resource Art Correction Gate")]
        public static void RunFromMenu() { Run(false); }

        public static void RunFromCommandLine() { Run(true); }

        private static void Run(bool exitEditor)
        {
            var checks = new List<Check>();
            var report = new Report { UnityVersion = Application.unityVersion };
            try
            {
                VerifyResourceAndRegionLedger(checks);
                VerifyFacilityConstructionAndSnapshot(checks);
                VerifyPresentationBoundary(checks);
                report.Checks = checks.ToArray();
                report.Passed = checks.All(check => check.Passed);
                WriteReport(report);
                if (!report.Passed) throw new InvalidOperationException(
                    "O5 gate failed: " + string.Join(",", checks.Where(check => !check.Passed).Select(check => check.Id)));
                Debug.Log("[ParallelQA] PASS · O5 human resource/art correction");
                if (exitEditor) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                report.Checks = checks.ToArray();
                report.Passed = false;
                WriteReport(report);
                Debug.LogException(exception);
                if (exitEditor) EditorApplication.Exit(1); else throw;
            }
        }

        private static void VerifyResourceAndRegionLedger(ICollection<Check> checks)
        {
            var ledger = new PrototypeSearchNodeLedger(180018);
            Add(checks, "O5-R01",
                ledger.GeneralRemainingAmount == 432 &&
                PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits == 432 &&
                PrototypeEscapeResourceSeedAuditor.ExpectedStableTotals().Values.Sum() == 432,
                "general=" + ledger.GeneralRemainingAmount + "; expected=432");

            bool sevenAtHundred = PrototypeSearchRegionCatalog.All.Count == 7 &&
                                  PrototypeSearchRegionCatalog.All.All(region =>
                                      ledger.GetRegionInitialGeneralAmount(region.StableId) > 0 &&
                                      ledger.GetRegionRemainingGeneralAmount(region.StableId) == ledger.GetRegionInitialGeneralAmount(region.StableId) &&
                                      ledger.GetRegionRemainingPercent(region.StableId) == 100);
            Add(checks, "O5-R02", sevenAtHundred,
                string.Join(" | ", PrototypeSearchRegionCatalog.All.Select(region =>
                    region.StableId + "=" + ledger.GetRegionRemainingGeneralAmount(region.StableId) + "/" +
                    ledger.GetRegionInitialGeneralAmount(region.StableId) + "/" + ledger.GetRegionRemainingPercent(region.StableId) + "%")));
        }

        private static void VerifyFacilityConstructionAndSnapshot(ICollection<Check> checks)
        {
            var session = new GameSession(180018);
            GameSessionStableState resources = session.CaptureStableState();
            resources.Storage = GameSession.GetStableResourceCatalog().Select(entry => new StableResourceAmount(
                entry.StableResourceId, entry.LegacyKind,
                entry.StableResourceId == "resource.wood" || entry.StableResourceId == "resource.salvage" || entry.StableResourceId == "resource.stone" ? 20 : entry.Amount)).ToArray();
            bool restoredResources = session.RestoreStableState(resources);
            var director = new PrototypeEscapeProjectDirector();
            bool absent = new[] { "escape.raft", "escape.smoke", "escape.radio" }
                .All(id => !director.GetState(id).FacilityBuilt);
            bool built = restoredResources && director.TryBuildFacility(session, "escape.smoke") &&
                         director.GetState("escape.smoke").FacilityBuilt &&
                         !director.GetState("escape.raft").FacilityBuilt &&
                         !director.GetState("escape.radio").FacilityBuilt;
            var restored = new PrototypeEscapeProjectDirector();
            bool snapshot = restored.RestoreSnapshot(director.CaptureSnapshot()) &&
                            restored.GetState("escape.smoke").FacilityBuilt &&
                            !restored.GetState("escape.raft").FacilityBuilt;
            Add(checks, "O5-E01", absent && built && snapshot,
                "absent=" + absent + "; smokeBuilt=" + built + "; snapshot=" + snapshot);
        }

        private static void VerifyPresentationBoundary(ICollection<Check> checks)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            string runtime = File.ReadAllText(Path.Combine(root, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs"));
            string marker = File.ReadAllText(Path.Combine(root, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.O5InteractionAffordance.cs"));
            string strings = File.ReadAllText(Path.Combine(root, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv"));
            Add(checks, "O5-U01",
                !runtime.Contains("RegisterEscapeRouteWorldLabel(escapeId") &&
                !runtime.Contains("RegisterEscapeRouteWorldLabel(PrototypeRaftEscapeConfig") &&
                marker.Contains("CreateO5InteractableMarker") && marker.Contains("상호작용 아이콘"),
                "persistent route-label calls absent; icon-first marker source present");
            Add(checks, "O5-U02",
                strings.Contains("expedition.map.node.state_remaining") &&
                strings.Contains("expedition.map.rail.resources_remaining") &&
                runtime.Contains("GetRegionRemainingPercent"),
                "map card/detail remaining-resource semantics present");
        }

        private static void Add(ICollection<Check> checks, string id, bool passed, string detail)
        {
            checks.Add(new Check { Id = id, Passed = passed, Detail = detail ?? string.Empty });
        }

        private static void WriteReport(Report report)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            string configured = Environment.GetEnvironmentVariable("KIM_QA_ARTIFACTS");
            string output = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(projectRoot, "Artifacts", "ParallelQA", "O5HumanResourceArtCorrection")
                : configured;
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "o5-human-resource-art-correction-report.json"),
                JsonUtility.ToJson(report, true) + Environment.NewLine, Utf8NoBom);
        }
    }
}
