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
    public static class O6SearchBagResourceGateRunner
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
            public string Title = "O6 search, bag, and finite resource gate";
            public string UnityVersion = string.Empty;
            public bool Passed;
            public Check[] Checks = Array.Empty<Check>();
        }

        [MenuItem("Kim Survival/QA/O6 Search Bag Resource Gate")]
        public static void RunFromMenu() { Run(false); }

        public static void RunFromCommandLine() { Run(true); }

        private static void Run(bool exitEditor)
        {
            var checks = new List<Check>();
            var report = new Report { UnityVersion = Application.unityVersion };
            try
            {
                VerifyCatalogDistribution(checks);
                VerifyProtectedPartVisibility(checks);
                VerifyFinitePersistence(checks);
                VerifyRuntimeAndSeedContracts(checks);
                VerifyBagUpgradeLadder(checks);
                VerifyUiContract(checks);
                report.Checks = checks.ToArray();
                report.Passed = checks.All(check => check.Passed);
                WriteReport(report);
                if (!report.Passed)
                {
                    throw new InvalidOperationException("O6 search/bag gate failed: " +
                                                        string.Join(",", checks.Where(check => !check.Passed)
                                                            .Select(check => check.Id)));
                }
                Debug.Log("[ParallelQA] PASS · O6 search/bag/resource gate");
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

        private static void VerifyCatalogDistribution(ICollection<Check> checks)
        {
            const int seed = 180018;
            IReadOnlyList<PrototypeSearchNodeDefinition> nodes = PrototypeSearchRegionCatalog.Nodes;
            int total = PrototypeSearchRegionCatalog.GeneralStockUnitsForSeed(seed);
            int[] nodeTotals = nodes.Select(node => PrototypeSearchNodeLootResolver.ResolveGeneralStock(seed, node)
                .Sum(item => Math.Max(0, item.Amount))).OrderBy(value => value).ToArray();
            bool shape = nodes.Count == PrototypeSearchRegionCatalog.SearchNodeCount &&
                         PrototypeSearchRegionCatalog.All.All(region =>
                             region.Nodes.Count == PrototypeSearchRegionCatalog.NodesPerRegion) &&
                         PrototypeSearchRegionCatalog.O6ExpandedNodeIds.Count == 42;
            bool mixed = nodeTotals.Distinct().Count() >= 3 && nodeTotals.First() < nodeTotals[nodeTotals.Length / 2] &&
                         nodeTotals[nodeTotals.Length / 2] < nodeTotals.Last();
            Add(checks, "O6-S01", shape && total == PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits && mixed,
                "nodes=" + nodes.Count + "; perRegion=" + string.Join(",", PrototypeSearchRegionCatalog.All
                    .Select(region => region.Nodes.Count)) + "; total=" + total + "; density=" +
                nodeTotals.First() + "/" + nodeTotals[nodeTotals.Length / 2] + "/" + nodeTotals.Last());

            int maxRows = nodes.Max(node => PrototypeSearchNodeLootResolver.Resolve(seed, node).Length);
            bool noTripledRows = nodes.All(node => PrototypeSearchNodeLootResolver.ResolveGeneralStock(seed, node)
                .Select(item => item.StableResourceId).Distinct(StringComparer.Ordinal).Count() ==
                PrototypeSearchNodeLootResolver.ResolveGeneralStock(seed, node).Length);
            Add(checks, "O6-S02", maxRows <= 8 && noTripledRows,
                "maxDiscoveryRows=" + maxRows + "; duplicateResourceRows=" + (!noTripledRows));
        }

        private static void VerifyProtectedPartVisibility(ICollection<Check> checks)
        {
            const int seed = 180018;
            PrototypeProtectedPartAssignmentSnapshot[] assignments =
                PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                    seed, PrototypeSearchRegionCatalog.ContractRevision);
            bool visibleFirst = assignments.Length == 5 && assignments.All(assignment =>
            {
                PrototypeSearchNodeDefinition node = PrototypeSearchRegionCatalog.Nodes.First(definition =>
                    string.Equals(definition.NodeId, assignment.AssignedNodeId, StringComparison.Ordinal));
                PrototypeSearchLootEntry[] contents = PrototypeSearchNodeLootResolver.Resolve(seed, node);
                return contents.Length <= 8 && contents.Length > 0 && contents[0].IsProtectedPart &&
                       string.Equals(contents[0].ProtectedPartId, assignment.PartId, StringComparison.Ordinal);
            });
            Add(checks, "O6-S03", visibleFirst,
                "protectedParts=" + assignments.Length + "; allLeadDiscoveryTray=" + visibleFirst);
        }

        private static void VerifyFinitePersistence(ICollection<Check> checks)
        {
            const int seed = 180018;
            var ledger = new PrototypeSearchNodeLedger(seed);
            PrototypeSearchNodeDefinition node = PrototypeSearchRegionCatalog.Nodes.First(definition =>
                PrototypeSearchNodeLootResolver.Resolve(seed, definition).Any(item => !item.IsProtectedPart));
            ledger.Reveal(node);
            PrototypeSearchLootEntry item = ledger.GetOrCreate(node).Remaining.First(value => !value.IsProtectedPart);
            int consumedAmount = item.Amount;
            int totalBefore = ledger.GeneralRemainingAmount;
            bool consumed = ledger.Consume(node.NodeId, item.StableItemId, consumedAmount);
            int totalAfter = ledger.GeneralRemainingAmount;
            PrototypeSearchRunSnapshot snapshot = ledger.CaptureSnapshot();
            var restored = PrototypeSearchNodeLedger.CreateForRestore(seed);
            bool restore = restored.RestoreSnapshot(snapshot);
            bool persistent = consumed && restore && totalAfter == totalBefore - consumedAmount &&
                              restored.GeneralRemainingAmount == totalAfter &&
                              restored.GetOrCreate(node).SearchCount == 1 &&
                              restored.CaptureSnapshot().Nodes.Length == PrototypeSearchRegionCatalog.SearchNodeCount;
            Add(checks, "O6-S04", persistent,
                "before=" + totalBefore + "; after=" + totalAfter + "; restored=" +
                restored.GeneralRemainingAmount + "; nodes=" + restored.CaptureSnapshot().Nodes.Length);
        }

        private static void VerifyBagUpgradeLadder(ICollection<Check> checks)
        {
            var session = new GameSession(180018);
            session.Grant(ResourceKind.Wood, 12);
            session.Grant(ResourceKind.Salvage, 8);
            bool workbench = session.TryBuild(StructureKind.Workbench);
            var capacities = new List<int> { session.ActiveBagSlotCount };
            int woodBefore = session.GetSpendableLegacyStorage(ResourceKind.Wood);
            int salvageBefore = session.GetSpendableLegacyStorage(ResourceKind.Salvage);
            bool upgrade1 = session.TryUpgradeBagCapacity();
            capacities.Add(session.ActiveBagSlotCount);
            bool upgrade2 = session.TryUpgradeBagCapacity();
            capacities.Add(session.ActiveBagSlotCount);
            bool upgrade3 = session.TryUpgradeBagCapacity();
            capacities.Add(session.ActiveBagSlotCount);
            bool repeatRejected = !session.TryUpgradeBagCapacity();
            bool exactCosts = session.GetSpendableLegacyStorage(ResourceKind.Wood) ==
                                  woodBefore - GameSession.BagUpgradeWoodCost * 3 &&
                              session.GetSpendableLegacyStorage(ResourceKind.Salvage) ==
                                  salvageBefore - GameSession.BagUpgradeSalvageCost * 3;
            GameSessionStableState snapshot = session.CaptureStableState();
            var restored = new GameSession(180018);
            bool restore = restored.RestoreStableState(snapshot) &&
                           restored.ActiveBagSlotCount == GameSession.MaximumBagSlotCount;
            snapshot.ActiveBagSlotCount = 9;
            bool invalidOddRejected = !new GameSession(180018).RestoreStableState(snapshot);
            bool passed = workbench && upgrade1 && upgrade2 && upgrade3 && repeatRejected && exactCosts && restore &&
                          invalidOddRejected && capacities.SequenceEqual(new[] { 4, 6, 8, 10 });
            Add(checks, "O6-B01", passed,
                "ladder=" + string.Join("->", capacities) + "; costs=" + exactCosts +
                "; restore10=" + restore + "; reject9=" + invalidOddRejected);
        }

        private static void VerifyRuntimeAndSeedContracts(ICollection<Check> checks)
        {
            PrototypeSearchNodeContractResult runtime = PrototypeSearchNodeRuntimeContract.Verify();
            Add(checks, "O6-S05", runtime.Passed, runtime.Detail);

            PrototypeEscapeResourceSeedAuditResult[] audits =
                PrototypeEscapeResourceSeedAuditor.AuditRepresentativeSeeds();
            bool representativeSeedsPass = audits.Length ==
                                           PrototypeEscapeResourceSeedAuditor.RepresentativeSeeds.Count &&
                                           audits.All(audit =>
                                               audit.RegionCount == 7 &&
                                               audit.NodeCount == PrototypeSearchRegionCatalog.SearchNodeCount &&
                                               audit.GeneralStockUnits ==
                                               PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                                               audit.ProtectedPartUnits == 5 &&
                                               audit.ExactStableStock &&
                                               audit.StableCatalogComplete &&
                                               audit.ProtectedAssignmentsValid &&
                                               audit.AtLeastOneRouteCompletable &&
                                               audit.AllPlayableRoutesCompletable &&
                                               audit.NoSoftlock);
            Add(checks, "O6-S06", representativeSeedsPass,
                string.Join(" | ", audits.Select(audit =>
                    audit.Seed + ":nodes=" + audit.NodeCount + ",general=" + audit.GeneralStockUnits +
                    ",routes=" + audit.AllPlayableRoutesCompletable + ",safe=" + audit.NoSoftlock)));
        }

        private static void VerifyUiContract(ICollection<Check> checks)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            string runtime = File.ReadAllText(Path.Combine(root, "Assets", "_Project", "Scripts", "Runtime",
                "KimSurvivalPrototype.cs"));
            bool allRows = runtime.Contains("SearchLootVisibleEntryCapacity = 8") &&
                           runtime.Contains("index < SearchLootVisibleEntryCapacity");
            bool bagAlwaysLeft = runtime.Contains("bagPanel.SetActive(session.Phase == GamePhase.Exploring && !placing)") &&
                                 runtime.Contains("ApplyBagPanelLayoutPolicy(searchTray)");
            Add(checks, "O6-U01", allRows && bagAlwaysLeft,
                "discoveryRows=" + allRows + "; alwaysVisibleBag=" + bagAlwaysLeft);
        }

        private static void Add(ICollection<Check> checks, string id, bool passed, string detail)
        {
            checks.Add(new Check { Id = id, Passed = passed, Detail = detail ?? string.Empty });
        }

        private static void WriteReport(Report report)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            string configured = Environment.GetEnvironmentVariable("KIM_QA_ARTIFACTS");
            string output = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(root, "Artifacts", "ParallelQA", "O6SearchBagResource")
                : configured;
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "o6-search-bag-resource-report.json"),
                JsonUtility.ToJson(report, true) + Environment.NewLine, Utf8NoBom);
        }
    }
}
