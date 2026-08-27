using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeO7SearchLayoutEntry
    {
        public string RegionId = string.Empty;
        public string NodeId = string.Empty;
        public bool WaterLane;
        public float WorldX;
    }

    [Serializable]
    public sealed class PrototypeO7WoodRouteBudget
    {
        public string EscapeId = string.Empty;
        public int StartingWood;
        public int SearchWood;
        public int SurvivalFacilityWood;
        public int BagUpgradeWood;
        public int EscapeRouteWood;
        public int RequiredWood;
        public int SpareWood;
        public string[] SourceRegionIds = Array.Empty<string>();
    }

    /// <summary>
    /// O7 deliberately changes only presentation coordinates and finite general
    /// stock composition. Stable node IDs, protected-part assignment, and ledger
    /// snapshot keys continue to use the existing production contracts.
    /// </summary>
    public static class PrototypeO7SearchBalance
    {
        public const string ContractId = "gamejam.o7.search-space-economy.v1";
        public const int ExpectedRegionCount = 7;
        public const int ExpectedNodeCount = 84;
        public const int ExpectedGeneralStockUnits = 432;
        public const int ExpectedWoodStockUnits = 84;

        public const float PlayerMinimumX = -16f;
        public const float PlayerLockedMaximumX = 8f;
        public const float PlayerUnlockedMaximumX = 33f;
        public const float LandLaneMinimumX = -1.8f;
        public const float LandLaneMaximumX = 31.8f;
        public const float WaterLaneMinimumX = -15.2f;
        public const float WaterLaneMaximumX = -4.9f;
        public const float CameraMinimumX = -11.5f;
        public const float CameraMaximumX = 26.5f;
        public const float CameraLookAhead = 2.5f;
        public const float SpaciousGapThreshold = 2f;
        public const float MinimumGap = 1.7f;
        public const float MinimumSpaciousGapRatio = 0.78f;

        public const int SurvivalFacilityWoodBudget = 2;
        public const int FirstBagUpgradeWoodBudget = GameSession.BagUpgradeWoodCost;
        public const int MinimumRouteSpareWood = 4;

        private static readonly float[] TwelveNodeRhythm =
        {
            0f, 0.09f, 0.18f, 0.235f, 0.36f, 0.46f,
            0.56f, 0.615f, 0.75f, 0.84f, 0.92f, 1f
        };

        private static readonly HashSet<string> CommonMaterialIds = new HashSet<string>(
            new[] { "resource.wood", "resource.salvage", "resource.stone" },
            StringComparer.Ordinal);

        private static readonly HashSet<string> TechnologyRegionIds = new HashSet<string>(
            new[] { "region.cove.wreck", "region.ruins.relay" },
            StringComparer.Ordinal);

        public static float WorldXForLane(bool waterLane, int index, int count)
        {
            float minimum = waterLane ? WaterLaneMinimumX : LandLaneMinimumX;
            float maximum = waterLane ? WaterLaneMaximumX : LandLaneMaximumX;
            if (count <= 1) return (minimum + maximum) * 0.5f;
            int clampedIndex = Mathf.Clamp(index, 0, count - 1);
            float normalized = count == TwelveNodeRhythm.Length
                ? TwelveNodeRhythm[clampedIndex]
                : clampedIndex / (count - 1f);
            return Mathf.Lerp(minimum, maximum, normalized);
        }

        public static float CameraTargetX(float playerX)
        {
            return Mathf.Clamp(playerX + CameraLookAhead, CameraMinimumX, CameraMaximumX);
        }

        public static PrototypeO7SearchLayoutEntry[] BuildLayout(PrototypeSearchRegionDefinition region)
        {
            if (region == null) return Array.Empty<PrototypeO7SearchLayoutEntry>();
            int waterCount = region.Nodes.Count(node => node.RequiresSwimming);
            int landCount = region.Nodes.Count - waterCount;
            int waterIndex = 0;
            int landIndex = 0;
            return region.Nodes.Select(node =>
            {
                bool water = node.RequiresSwimming;
                float x = water
                    ? WorldXForLane(true, waterIndex++, waterCount)
                    : WorldXForLane(false, landIndex++, landCount);
                return new PrototypeO7SearchLayoutEntry
                {
                    RegionId = region.StableId,
                    NodeId = node.NodeId,
                    WaterLane = water,
                    WorldX = x
                };
            }).ToArray();
        }

        public static PrototypeO7WoodRouteBudget[] BuildRepresentativeWoodBudgets(int seed)
        {
            return new[]
            {
                BuildWoodBudget(seed, PrototypeRaftEscapeConfig.EscapeId, 6,
                    "region.coast.beach", "region.sea.shallows"),
                BuildWoodBudget(seed, "escape.smoke", 15,
                    "region.forest.grove", "region.ridge.highland", "region.cave.island"),
                BuildWoodBudget(seed, "escape.radio", 3,
                    "region.cove.wreck", "region.ruins.relay", "region.ridge.highland")
            };
        }

        public static bool RunContractProbe(out string detail)
        {
            const int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            PrototypeSearchRegionDefinition[] regions = PrototypeSearchRegionCatalog.All.ToArray();
            PrototypeSearchNodeDefinition[] nodes = PrototypeSearchRegionCatalog.Nodes.ToArray();
            PrototypeO7SearchLayoutEntry[] layout = regions.SelectMany(BuildLayout).ToArray();

            bool exactShape = regions.Length == ExpectedRegionCount &&
                              nodes.Length == ExpectedNodeCount &&
                              regions.All(region => region.Nodes.Count == PrototypeSearchRegionCatalog.NodesPerRegion) &&
                              layout.Length == ExpectedNodeCount &&
                              layout.Select(entry => entry.NodeId).Distinct(StringComparer.Ordinal).Count() == ExpectedNodeCount;

            List<float> adjacentGaps = AdjacentLaneGaps(regions).ToList();
            float minimumGap = adjacentGaps.Count == 0 ? 0f : adjacentGaps.Min();
            int spaciousGaps = adjacentGaps.Count(gap => gap >= SpaciousGapThreshold);
            float spaciousRatio = adjacentGaps.Count == 0 ? 0f : spaciousGaps / (float)adjacentGaps.Count;
            int clusterGaps = adjacentGaps.Count(gap => gap < SpaciousGapThreshold);
            bool wideRegions = regions.All(region =>
            {
                PrototypeO7SearchLayoutEntry[] entries = BuildLayout(region);
                return entries.Length > 1 && entries.Max(entry => entry.WorldX) - entries.Min(entry => entry.WorldX) >=
                       LandLaneMaximumX - LandLaneMinimumX - 0.01f;
            });
            bool spacing = minimumGap >= MinimumGap && spaciousRatio >= MinimumSpaciousGapRatio &&
                           clusterGaps >= 8 && clusterGaps <= 20 && wideRegions;

            Dictionary<string, HashSet<string>> regionPools = regions.ToDictionary(
                region => region.StableId,
                region => new HashSet<string>(region.Nodes.SelectMany(node => node.FiniteYield)
                    .Where(item => item != null && item.Amount > 0)
                    .Select(item => item.StableResourceId), StringComparer.Ordinal),
                StringComparer.Ordinal);
            bool minimumDiversity = regionPools.Values.All(pool => pool.Count >= 3);
            bool wreckAndRelayCommon = CommonMaterialIds.All(regionPools["region.cove.wreck"].Contains) &&
                                       CommonMaterialIds.All(regionPools["region.ruins.relay"].Contains);
            string[] electronicsRegions = nodes
                .Where(node => node.FiniteYield.Any(item =>
                    string.Equals(item.StableResourceId, "resource.electronics", StringComparison.Ordinal)))
                .Select(node => node.RegionId).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            bool electronicsPlausible = TechnologyRegionIds.SetEquals(electronicsRegions);
            bool weightedSpecialties = RegionTotal(seed, "region.cove.wreck", "resource.salvage") >
                                       RegionTotal(seed, "region.cove.wreck", "resource.stone") &&
                                       RegionTotal(seed, "region.ruins.relay", "resource.electronics") >
                                       RegionTotal(seed, "region.ruins.relay", "resource.salvage") &&
                                       RegionTotal(seed, "region.ruins.relay", "resource.wire") >
                                       RegionTotal(seed, "region.ruins.relay", "resource.stone");

            int generalUnits = PrototypeSearchRegionCatalog.GeneralStockUnitsForSeed(seed);
            int woodUnits = nodes.Sum(node => PrototypeSearchNodeLootResolver.ResolveGeneralStock(seed, node)
                .Where(item => string.Equals(item.StableResourceId, "resource.wood", StringComparison.Ordinal))
                .Sum(item => Math.Max(0, item.Amount)));
            int woodRegionCount = regions.Count(region => RegionTotal(seed, region.StableId, "resource.wood") > 0);
            bool finiteStock = generalUnits == ExpectedGeneralStockUnits &&
                               generalUnits == PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                               woodUnits == ExpectedWoodStockUnits && woodRegionCount >= 5;

            PrototypeO7WoodRouteBudget[] budgets = BuildRepresentativeWoodBudgets(seed);
            bool representativeBudget = budgets.All(budget => budget.SpareWood >= MinimumRouteSpareWood);
            bool protectedParts = PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                                      seed, PrototypeSearchRegionCatalog.ContractRevision).Length == 5;

            bool passed = exactShape && spacing && minimumDiversity && wreckAndRelayCommon &&
                          electronicsPlausible && weightedSpecialties && finiteStock && representativeBudget &&
                          protectedParts;
            detail = string.Format(
                CultureInfo.InvariantCulture,
                "contract={0}; shape={1}r/{2}n; world={3:0.0}..{4:0.0}; gaps=min{5:0.00},spacious{6:0.0}%,clusters{7}/{8}; pools={9}; wreckRelayCommon={10}; electronicsRegions={11}; stock={12},wood={13}@{14}regions; budgets={15}; protected=5",
                ContractId,
                regions.Length,
                nodes.Length,
                PlayerMinimumX,
                PlayerUnlockedMaximumX,
                minimumGap,
                spaciousRatio * 100f,
                clusterGaps,
                adjacentGaps.Count,
                string.Join(",", regions.Select(region => region.StableId + ":" + regionPools[region.StableId].Count)),
                wreckAndRelayCommon,
                string.Join(",", electronicsRegions),
                generalUnits,
                woodUnits,
                woodRegionCount,
                string.Join(",", budgets.Select(budget => budget.EscapeId + ":" + budget.RequiredWood + "/" +
                    (budget.RequiredWood + budget.SpareWood) + "(+" + budget.SpareWood + ")")));
            return passed;
        }

        public static int RegionTotal(int seed, string regionId, string stableResourceId)
        {
            return PrototypeSearchRegionCatalog.Get(regionId).Nodes.Sum(node =>
                PrototypeSearchNodeLootResolver.ResolveGeneralStock(seed, node)
                    .Where(item => string.Equals(item.StableResourceId, stableResourceId, StringComparison.Ordinal))
                    .Sum(item => Math.Max(0, item.Amount)));
        }

        private static IEnumerable<float> AdjacentLaneGaps(IEnumerable<PrototypeSearchRegionDefinition> regions)
        {
            foreach (PrototypeSearchRegionDefinition region in regions)
            {
                PrototypeO7SearchLayoutEntry[] layout = BuildLayout(region);
                foreach (bool waterLane in new[] { false, true })
                {
                    float[] positions = layout.Where(entry => entry.WaterLane == waterLane)
                        .Select(entry => entry.WorldX).OrderBy(value => value).ToArray();
                    for (int index = 1; index < positions.Length; index += 1)
                    {
                        yield return positions[index] - positions[index - 1];
                    }
                }
            }
        }

        private static PrototypeO7WoodRouteBudget BuildWoodBudget(
            int seed,
            string escapeId,
            int escapeRouteWood,
            params string[] sourceRegionIds)
        {
            int startingWood = new GameSession(seed).GetStableStorage("resource.wood");
            int searchWood = (sourceRegionIds ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal)
                .Sum(regionId => RegionTotal(seed, regionId, "resource.wood"));
            int required = SurvivalFacilityWoodBudget + FirstBagUpgradeWoodBudget + escapeRouteWood;
            return new PrototypeO7WoodRouteBudget
            {
                EscapeId = escapeId,
                StartingWood = startingWood,
                SearchWood = searchWood,
                SurvivalFacilityWood = SurvivalFacilityWoodBudget,
                BagUpgradeWood = FirstBagUpgradeWoodBudget,
                EscapeRouteWood = escapeRouteWood,
                RequiredWood = required,
                SpareWood = startingWood + searchWood - required,
                SourceRegionIds = (sourceRegionIds ?? Array.Empty<string>()).ToArray()
            };
        }
    }
}
