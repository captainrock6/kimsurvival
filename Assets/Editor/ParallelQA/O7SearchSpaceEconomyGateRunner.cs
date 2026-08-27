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
    public static class O7SearchSpaceEconomyGateRunner
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
            public string ContractId = PrototypeO7SearchBalance.ContractId;
            public string UnityVersion = string.Empty;
            public bool Passed;
            public Check[] Checks = Array.Empty<Check>();
            public PrototypeO7WoodRouteBudget[] WoodBudgets = Array.Empty<PrototypeO7WoodRouteBudget>();
        }

        [MenuItem("Kim Survival/QA/O7 Search Space Economy Gate")]
        public static void RunFromMenu() { Run(false); }

        public static void RunFromCommandLine() { Run(true); }

        private static void Run(bool exitEditor)
        {
            var checks = new List<Check>();
            var report = new Report { UnityVersion = Application.unityVersion };
            try
            {
                VerifyShapeAndStableIdentity(checks);
                VerifySpacing(checks);
                VerifyWeightedPlausiblePools(checks);
                report.WoodBudgets = VerifyRepresentativeWoodBudget(checks);
                VerifyFinitePersistence(checks);
                VerifyProtectedPartsAndCoreContract(checks);
                bool aggregate = PrototypeO7SearchBalance.RunContractProbe(out string detail);
                Add(checks, "O7-S07", aggregate, detail);

                report.Checks = checks.ToArray();
                report.Passed = checks.All(check => check.Passed);
                WriteReport(report);
                if (!report.Passed)
                {
                    throw new InvalidOperationException("O7 search-space/economy gate failed: " +
                                                        string.Join(",", checks.Where(check => !check.Passed)
                                                            .Select(check => check.Id)));
                }
                Debug.Log("[ParallelQA] PASS · O7 search space and economy gate");
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

        private static void VerifyShapeAndStableIdentity(ICollection<Check> checks)
        {
            PrototypeSearchNodeDefinition[] nodes = PrototypeSearchRegionCatalog.Nodes.ToArray();
            PrototypeO7SearchLayoutEntry[] layout = PrototypeSearchRegionCatalog.All
                .SelectMany(PrototypeO7SearchBalance.BuildLayout).ToArray();
            var catalogIds = new HashSet<string>(nodes.Select(node => node.NodeId), StringComparer.Ordinal);
            bool passed = PrototypeSearchRegionCatalog.All.Count == 7 && nodes.Length == 84 &&
                          PrototypeSearchRegionCatalog.All.All(region => region.Nodes.Count == 12) &&
                          catalogIds.Count == 84 && layout.Length == 84 &&
                          layout.Select(entry => entry.NodeId).Distinct(StringComparer.Ordinal).Count() == 84 &&
                          PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.All(catalogIds.Contains) &&
                          PrototypeSearchRegionCatalog.NewWaveBNodeIds.All(catalogIds.Contains) &&
                          PrototypeSearchRegionCatalog.O6ExpandedNodeIds.Count == 42;
            Add(checks, "O7-S01", passed,
                "regions=" + PrototypeSearchRegionCatalog.All.Count + "; nodes=" + nodes.Length +
                "; layoutIds=" + layout.Select(entry => entry.NodeId).Distinct(StringComparer.Ordinal).Count() +
                "; preserved=" + PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.Count + "+" +
                PrototypeSearchRegionCatalog.NewWaveBNodeIds.Count + "+42");
        }

        private static void VerifySpacing(ICollection<Check> checks)
        {
            var gaps = new List<float>();
            var spans = new List<float>();
            foreach (PrototypeSearchRegionDefinition region in PrototypeSearchRegionCatalog.All)
            {
                PrototypeO7SearchLayoutEntry[] entries = PrototypeO7SearchBalance.BuildLayout(region);
                spans.Add(entries.Max(entry => entry.WorldX) - entries.Min(entry => entry.WorldX));
                foreach (bool water in new[] { false, true })
                {
                    float[] lane = entries.Where(entry => entry.WaterLane == water)
                        .Select(entry => entry.WorldX).OrderBy(value => value).ToArray();
                    for (int index = 1; index < lane.Length; index += 1) gaps.Add(lane[index] - lane[index - 1]);
                }
            }
            float spaciousRatio = gaps.Count(gap => gap >= PrototypeO7SearchBalance.SpaciousGapThreshold) /
                                  (float)Math.Max(1, gaps.Count);
            int clusters = gaps.Count(gap => gap < PrototypeO7SearchBalance.SpaciousGapThreshold);
            bool passed = gaps.Min() >= PrototypeO7SearchBalance.MinimumGap &&
                          spaciousRatio >= PrototypeO7SearchBalance.MinimumSpaciousGapRatio &&
                          clusters >= 8 && clusters <= 20 &&
                          spans.All(span => span >= PrototypeO7SearchBalance.LandLaneMaximumX -
                              PrototypeO7SearchBalance.LandLaneMinimumX - 0.01f);
            Add(checks, "O7-S02", passed,
                "gaps=" + gaps.Count + "; min=" + gaps.Min().ToString("0.00") +
                "; spacious=" + (spaciousRatio * 100f).ToString("0.0") + "%" +
                "; clusters=" + clusters + "; spans=" + string.Join(",", spans.Select(value => value.ToString("0.0"))));
        }

        private static void VerifyWeightedPlausiblePools(ICollection<Check> checks)
        {
            const int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            Dictionary<string, string[]> pools = PrototypeSearchRegionCatalog.All.ToDictionary(
                region => region.StableId,
                region => region.Nodes.SelectMany(node => node.FiniteYield)
                    .Select(item => item.StableResourceId).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
            string[] common = { "resource.wood", "resource.salvage", "resource.stone" };
            string[] electronicsRegions = PrototypeSearchRegionCatalog.Nodes
                .Where(node => node.FiniteYield.Any(item => item.StableResourceId == "resource.electronics"))
                .Select(node => node.RegionId).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            bool weighted = PrototypeO7SearchBalance.RegionTotal(seed, "region.cove.wreck", "resource.salvage") >
                            PrototypeO7SearchBalance.RegionTotal(seed, "region.cove.wreck", "resource.stone") &&
                            PrototypeO7SearchBalance.RegionTotal(seed, "region.ruins.relay", "resource.electronics") >
                            PrototypeO7SearchBalance.RegionTotal(seed, "region.ruins.relay", "resource.salvage");
            bool passed = pools.Values.All(pool => pool.Length >= 3) &&
                          common.All(pools["region.cove.wreck"].Contains) &&
                          common.All(pools["region.ruins.relay"].Contains) &&
                          electronicsRegions.SequenceEqual(new[] { "region.cove.wreck", "region.ruins.relay" }) && weighted;
            Add(checks, "O7-S03", passed,
                "poolSizes=" + string.Join(",", pools.Select(pair => pair.Key + ":" + pair.Value.Length)) +
                "; commonWreckRelay=" + common.All(pools["region.cove.wreck"].Contains) + "/" +
                common.All(pools["region.ruins.relay"].Contains) +
                "; electronics=" + string.Join(",", electronicsRegions) + "; weighted=" + weighted);
        }

        private static PrototypeO7WoodRouteBudget[] VerifyRepresentativeWoodBudget(ICollection<Check> checks)
        {
            const int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            PrototypeO7WoodRouteBudget[] budgets = PrototypeO7SearchBalance.BuildRepresentativeWoodBudgets(seed);
            int totalWood = PrototypeSearchRegionCatalog.Nodes.Sum(node =>
                PrototypeSearchNodeLootResolver.ResolveGeneralStock(seed, node)
                    .Where(item => item.StableResourceId == "resource.wood").Sum(item => item.Amount));
            int woodRegions = PrototypeSearchRegionCatalog.All.Count(region =>
                PrototypeO7SearchBalance.RegionTotal(seed, region.StableId, "resource.wood") > 0);
            bool passed = totalWood == PrototypeO7SearchBalance.ExpectedWoodStockUnits && woodRegions >= 5 &&
                          budgets.Length == 3 && budgets.All(budget => budget.SpareWood >=
                              PrototypeO7SearchBalance.MinimumRouteSpareWood);
            Add(checks, "O7-S04", passed,
                "wood=" + totalWood + "@" + woodRegions + "regions; budgets=" +
                string.Join(",", budgets.Select(budget => budget.EscapeId + ":" + budget.RequiredWood + "/" +
                    (budget.StartingWood + budget.SearchWood) + "/spare" + budget.SpareWood)));
            return budgets;
        }

        private static void VerifyFinitePersistence(ICollection<Check> checks)
        {
            const int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            var ledger = new PrototypeSearchNodeLedger(seed);
            PrototypeSearchNodeDefinition node = PrototypeSearchRegionCatalog.Get("region.cove.wreck").Nodes.First();
            ledger.Reveal(node);
            PrototypeSearchNodeSnapshot before = ledger.GetOrCreate(node).Clone();
            PrototypeSearchLootEntry item = before.Remaining.First(value => !value.IsProtectedPart);
            int totalBefore = ledger.GeneralRemainingAmount;
            bool consumed = ledger.Consume(node.NodeId, item.StableItemId, 1);
            ledger.MarkBarrierBroken(node.RegionId);
            ledger.MarkPermanentHazardRemoved(node.RegionId, node.HazardId);
            PrototypeSearchRunSnapshot snapshot = ledger.CaptureSnapshot();
            var restored = PrototypeSearchNodeLedger.CreateForRestore(seed);
            bool restoredOk = restored.RestoreSnapshot(snapshot);
            PrototypeSearchNodeSnapshot restoredNode = restored.GetOrCreate(node);
            PrototypeSearchRunSnapshot o6Snapshot = JsonUtility.FromJson<PrototypeSearchRunSnapshot>(
                JsonUtility.ToJson(snapshot));
            o6Snapshot.LootTableRevision = "gamejam.o6.loot.84-nodes-mixed-density-432.v1";
            o6Snapshot.CatalogRevision = "gamejam.o6.7r21a84i.v1";
            var migrated = PrototypeSearchNodeLedger.CreateForRestore(seed);
            bool migratedOk = migrated.RestoreSnapshot(o6Snapshot) &&
                              migrated.GeneralRemainingAmount == totalBefore - 1 &&
                              migrated.StockGenerationEvents.Contains("migration-o7-preserve-o6-finite-stock");
            bool passed = consumed && restoredOk && migratedOk && snapshot.Nodes.Length == 84 && snapshot.Regions.Length == 7 &&
                          restored.GeneralRemainingAmount == totalBefore - 1 &&
                          restoredNode.SearchCount == 1 && restoredNode.RemainingAmount == before.RemainingAmount - 1 &&
                          restored.IsBarrierBroken(node.RegionId) &&
                          restored.IsPermanentHazardRemoved(node.RegionId, node.HazardId);
            Add(checks, "O7-S05", passed,
                "remaining=" + totalBefore + "->" + restored.GeneralRemainingAmount +
                "; nodes=" + snapshot.Nodes.Length + "; regions=" + snapshot.Regions.Length +
                "; barrier=" + restored.IsBarrierBroken(node.RegionId) +
                "; hazard=" + restored.IsPermanentHazardRemoved(node.RegionId, node.HazardId) +
                "; o6Migration=" + migratedOk);
        }

        private static void VerifyProtectedPartsAndCoreContract(ICollection<Check> checks)
        {
            PrototypeEscapeResourceSeedAuditResult[] seedAudits =
                PrototypeEscapeResourceSeedAuditor.AuditRepresentativeSeeds();
            PrototypeSearchNodeContractResult runtime = PrototypeSearchNodeRuntimeContract.Verify();
            bool passed = runtime.Passed && seedAudits.Length ==
                          PrototypeEscapeResourceSeedAuditor.RepresentativeSeeds.Count &&
                          seedAudits.All(audit => audit.ProtectedPartUnits == 5 &&
                              audit.ProtectedAssignmentsValid && audit.AllPlayableRoutesCompletable && audit.NoSoftlock);
            Add(checks, "O7-S06", passed,
                "runtime=" + runtime.Passed + "; seeds=" + string.Join(",", seedAudits.Select(audit =>
                    audit.Seed + ":parts" + audit.ProtectedPartUnits + "/routes" + audit.AllPlayableRoutesCompletable)) +
                "; runtimeDetail=" + runtime.Detail);
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
                ? Path.Combine(root, "Artifacts", "ParallelQA", "O7SearchSpaceEconomy")
                : configured;
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "o7-search-space-economy-report.json"),
                JsonUtility.ToJson(report, true) + Environment.NewLine, Utf8NoBom);
        }
    }
}
