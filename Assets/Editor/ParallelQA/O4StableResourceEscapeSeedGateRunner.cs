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
    public static class O4StableResourceEscapeSeedGateRunner
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
        private sealed class LedgerEvidence
        {
            public int CanonicalFoodBefore;
            public int SpendableFoodBefore;
            public int LegacyFoodAggregateBefore;
            public int MedicineBefore;
            public int CanonicalFoodAfterMeal;
            public int LegacyFoodAggregateAfterMeal;
            public int MedicineAfterMeal;
            public int StableCatalogCount;
            public StableResourceAmount[] StableStorageAfter = Array.Empty<StableResourceAmount>();
        }

        [Serializable]
        private sealed class Report
        {
            public int SchemaVersion = 1;
            public string Title = "O4 stable resource and escape seed gate";
            public string UnityVersion = string.Empty;
            public string StartedUtc = string.Empty;
            public string CompletedUtc = string.Empty;
            public bool Passed;
            public Check[] Checks = Array.Empty<Check>();
            public LedgerEvidence Ledger = new LedgerEvidence();
            public PrototypeEscapeResourceSeedAuditResult[] SeedAudits =
                Array.Empty<PrototypeEscapeResourceSeedAuditResult>();
        }

        [MenuItem("Kim Survival/QA/O4 Stable Resource Escape Seed Gate")]
        public static void RunFromMenu()
        {
            Run(false);
        }

        public static void RunFromCommandLine()
        {
            Run(true);
        }

        private static void Run(bool exitEditor)
        {
            DateTime started = DateTime.UtcNow;
            Report report = new Report
            {
                UnityVersion = Application.unityVersion,
                StartedUtc = started.ToString("O")
            };
            var checks = new List<Check>();
            try
            {
                VerifyStableLedger(report, checks);

                PrototypeSearchNodeContractResult searchContract = PrototypeSearchNodeRuntimeContract.Verify();
                Add(checks, "O4-S00", searchContract.Passed, searchContract.Detail);

                report.SeedAudits = PrototypeEscapeResourceSeedAuditor.AuditRepresentativeSeeds();
                bool exactStock = report.SeedAudits.Length ==
                                  PrototypeEscapeResourceSeedAuditor.RepresentativeSeeds.Count &&
                                  report.SeedAudits.All(audit =>
                                      audit.RegionCount == 7 &&
                                      audit.NodeCount == PrototypeSearchRegionCatalog.SearchNodeCount &&
                                      audit.GeneralStockUnits == PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                                      audit.ProtectedPartUnits == 5 && audit.ExactStableStock && audit.StableCatalogComplete);
                Add(checks, "O4-S01", exactStock,
                    SeedSummary(report.SeedAudits, audit =>
                        $"{audit.Seed}:regions={audit.RegionCount},nodes={audit.NodeCount},general={audit.GeneralStockUnits},parts={audit.ProtectedPartUnits},exact={audit.ExactStableStock}"));

                bool protectedParts = report.SeedAudits.All(audit =>
                    audit.ProtectedAssignmentsValid && audit.RadioPartsUseDistinctRegions &&
                    audit.ProtectedAssignments.Length == 5);
                Add(checks, "O4-S02", protectedParts,
                    SeedSummary(report.SeedAudits, audit =>
                        $"{audit.Seed}:assignments={audit.ProtectedAssignments.Length},radioDistinct={audit.RadioPartsUseDistinctRegions}"));

                string[] routeIds = { PrototypeRaftEscapeConfig.EscapeId, "escape.smoke", "escape.radio" };
                bool allRoutesAffordable = report.SeedAudits.All(audit =>
                    routeIds.All(routeId => audit.Routes.Any(route =>
                        string.Equals(route.EscapeId, routeId, StringComparison.Ordinal) &&
                        route.ResourceAffordable && route.ProtectedPartsAvailable && route.NaturallyCompletable &&
                        route.RemainingAvailableUnits > 0)));
                Add(checks, "O4-R01", allRoutesAffordable,
                    SeedSummary(report.SeedAudits, audit => audit.Seed + ":" + string.Join(",",
                        audit.Routes.Select(route =>
                            route.EscapeId + "=" + route.RequiredGeneralUnits + "/" + route.RemainingAvailableUnits + "/" + route.NaturallyCompletable))));

                bool noSoftlock = report.SeedAudits.All(audit =>
                    audit.NoSoftlock && audit.AtLeastOneRouteCompletable && audit.AllPlayableRoutesCompletable);
                Add(checks, "O4-R02", noSoftlock,
                    SeedSummary(report.SeedAudits, audit =>
                        $"{audit.Seed}:any={audit.AtLeastOneRouteCompletable},all={audit.AllPlayableRoutesCompletable},noSoftlock={audit.NoSoftlock}"));

                PrototypeContractProbe smoke = PrototypeEscapeProjectDirector.VerifyEscapeSmokeProgressCompleteFixture();
                PrototypeContractProbe radio = PrototypeEscapeProjectDirector.VerifyEscapeRadioProgressCompleteFixture();
                PrototypeContractProbe raft = PrototypeRaftRuntimeContract.VerifyAtomicFailureRetrySnapshotFixture();
                Add(checks, "O4-R03", smoke.Success && radio.Success && raft.Success,
                    "smoke=" + smoke.Success + " " + smoke.Detail +
                    " | radio=" + radio.Success + " " + radio.Detail +
                    " | raft=" + raft.Success + " " + raft.Detail);

                report.Checks = checks.ToArray();
                report.Passed = report.Checks.All(check => check.Passed);
                report.CompletedUtc = DateTime.UtcNow.ToString("O");
                WriteReport(report);
                if (!report.Passed)
                {
                    throw new InvalidOperationException("O4 stable resource and escape seed gate failed: " +
                                                        string.Join(",", report.Checks.Where(check => !check.Passed)
                                                            .Select(check => check.Id)));
                }

                Debug.Log("[ParallelQA] PASS · O4 stable ledger + finite seed route affordability");
                if (exitEditor) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                report.Checks = checks.ToArray();
                report.Passed = false;
                report.CompletedUtc = DateTime.UtcNow.ToString("O");
                WriteReport(report);
                Debug.LogException(exception);
                if (exitEditor) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void VerifyStableLedger(Report report, ICollection<Check> checks)
        {
            GameSession session = new GameSession(180018);
            GameSessionStableState state = session.CaptureStableState();
            StableResourceAmount[] catalog = GameSession.GetStableResourceCatalog();
            state.Storage = catalog.Select(entry => new StableResourceAmount(
                entry.StableResourceId,
                entry.LegacyKind,
                string.Equals(entry.StableResourceId, "resource.food", StringComparison.Ordinal) ? 2 :
                string.Equals(entry.StableResourceId, "resource.medicine", StringComparison.Ordinal) ? 3 :
                string.Equals(entry.StableResourceId, "resource.wood", StringComparison.Ordinal) ? 2 : 0)).ToArray();
            Require(session.RestoreStableState(state), "stable ledger fixture restore rejected");

            PrototypePlaytestStateFingerprint before = PrototypePlaytestStateFingerprint.Capture(session);
            report.Ledger = new LedgerEvidence
            {
                CanonicalFoodBefore = session.GetStorage(ResourceKind.Food),
                SpendableFoodBefore = session.GetSpendableLegacyStorage(ResourceKind.Food),
                LegacyFoodAggregateBefore = session.GetLegacyAggregateStorage(ResourceKind.Food),
                MedicineBefore = session.GetStableStorage("resource.medicine"),
                StableCatalogCount = catalog.Length
            };
            bool displayDecisionMatch = report.Ledger.CanonicalFoodBefore == 2 &&
                                        report.Ledger.SpendableFoodBefore == 2 &&
                                        session.CanAffordStableResource("resource.food", 2) &&
                                        before.storage_food == 5 &&
                                        before.stable_storage.Single(entry =>
                                            string.Equals(entry.StableResourceId, "resource.food", StringComparison.Ordinal)).Amount == 2;
            Add(checks, "O4-L01", displayDecisionMatch,
                $"canonical/spendable={report.Ledger.CanonicalFoodBefore}/{report.Ledger.SpendableFoodBefore}; legacy-derived={before.storage_food}; medicine={report.Ledger.MedicineBefore}");

            bool mealAccepted = session.UseFood();
            report.Ledger.CanonicalFoodAfterMeal = session.GetStorage(ResourceKind.Food);
            report.Ledger.LegacyFoodAggregateAfterMeal = session.GetLegacyAggregateStorage(ResourceKind.Food);
            report.Ledger.MedicineAfterMeal = session.GetStableStorage("resource.medicine");
            report.Ledger.StableStorageAfter = session.GetStableStorageEntries();
            Add(checks, "O4-L02", mealAccepted && report.Ledger.CanonicalFoodAfterMeal == 1 &&
                                      report.Ledger.LegacyFoodAggregateAfterMeal == 4 &&
                                      report.Ledger.MedicineAfterMeal == 3,
                $"meal={mealAccepted}; canonical={report.Ledger.CanonicalFoodAfterMeal}; aggregate={report.Ledger.LegacyFoodAggregateAfterMeal}; medicine={report.Ledger.MedicineAfterMeal}");

            string[] expectedIds = PrototypeEscapeResourceSeedAuditor.ExpectedStableTotals().Keys
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] actualIds = catalog.Select(entry => entry.StableResourceId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Add(checks, "O4-L03", catalog.Length == 12 && actualIds.SequenceEqual(expectedIds) &&
                                      session.IsStableStorageSynchronized() &&
                                      session.GetStableStorageEntries().All(entry => entry.Amount >= 0),
                "catalog=" + catalog.Length + "; ids=" + string.Join(",", actualIds));

            bool duplicateAtomicSpend = session.TrySpendStableResources(new[]
            {
                new StableResourceAmount("resource.wood", ResourceKind.Wood, 1),
                new StableResourceAmount("resource.wood", ResourceKind.Wood, 1)
            });
            int woodAfterSpend = session.GetStableStorage("resource.wood");
            int foodBeforeRejectedSpend = session.GetStableStorage("resource.food");
            bool rejected = !session.TrySpendStableResources(new[]
            {
                new StableResourceAmount("resource.food", ResourceKind.Food, 2),
                new StableResourceAmount("resource.unknown", ResourceKind.Salvage, 1)
            });
            Add(checks, "O4-L04", duplicateAtomicSpend && woodAfterSpend == 0 && rejected &&
                                      session.GetStableStorage("resource.food") == foodBeforeRejectedSpend &&
                                      session.IsStableStorageSynchronized(),
                $"duplicateSpend={duplicateAtomicSpend}; wood={woodAfterSpend}; rejected={rejected}; foodUnchanged={session.GetStableStorage("resource.food")}");
        }

        private static void Add(ICollection<Check> checks, string id, bool passed, string detail)
        {
            checks.Add(new Check { Id = id, Passed = passed, Detail = detail ?? string.Empty });
        }

        private static string SeedSummary(
            IEnumerable<PrototypeEscapeResourceSeedAuditResult> audits,
            Func<PrototypeEscapeResourceSeedAuditResult, string> describe)
        {
            return string.Join(" | ", audits.Select(describe));
        }

        private static void WriteReport(Report report)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            string configuredRoot = Environment.GetEnvironmentVariable("KIM_QA_ARTIFACTS");
            string outputRoot = string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(projectRoot, "Artifacts", "ParallelQA", "O4StableResourceEscapeSeed")
                : configuredRoot;
            Directory.CreateDirectory(outputRoot);
            string path = Path.Combine(outputRoot, "o4-stable-resource-escape-seed-report.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true) + Environment.NewLine, Utf8NoBom);
            Debug.Log("[ParallelQA] O4 report: " + path);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
