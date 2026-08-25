using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    public enum PrototypeSearchNodeState
    {
        Hidden,
        RevealedPartial,
        Depleted
    }

    public enum PrototypeSearchNodeKind
    {
        GrassPatch,
        RockCrevice,
        DriftPile,
        TreeHollow,
        WreckLocker,
        FacilityCabinet
    }

    public enum PrototypeSearchOpenResult
    {
        Rejected,
        Opened,
        NeedSwimming,
        TooTired,
        Depleted
    }

    public enum PrototypeSearchTakeResult
    {
        Rejected,
        Added,
        Protected,
        PendingSwap,
        Depleted
    }

    [Serializable]
    public sealed class PrototypeSearchLootEntry
    {
        public string StableItemId = string.Empty;
        public string ResourceId = string.Empty;
        public ResourceKind Resource;
        public int Amount;
        public string ProtectedPartId = string.Empty;

        public bool IsProtectedPart
        {
            get { return !string.IsNullOrEmpty(ProtectedPartId); }
        }

        public string ItemId
        {
            get { return StableItemId; }
        }

        public PrototypeSearchLootEntry Clone()
        {
            return new PrototypeSearchLootEntry
            {
                StableItemId = StableItemId,
                ResourceId = ResourceId,
                Resource = Resource,
                Amount = Amount,
                ProtectedPartId = ProtectedPartId
            };
        }
    }

    [Serializable]
    public sealed class PrototypeSearchResourceAllocation
    {
        public PrototypeSearchResourceAllocation(string resourceId, int amount)
        {
            ResourceId = resourceId ?? string.Empty;
            Amount = Math.Max(0, amount);
        }

        public string ResourceId { get; }
        public int Amount { get; }
    }

    [Serializable]
    public sealed class PrototypeSearchNodeSnapshot
    {
        public int RunSeed;
        public string RegionId = string.Empty;
        public string NodeId = string.Empty;
        public PrototypeSearchNodeKind NodeKind;
        public PrototypeSearchNodeState State;
        public int SearchCount;
        public int TimeCostMinutes;
        public int EnergyCost;
        public int HazardExposureCount;
        public string HazardId = string.Empty;
        public bool BarrierBroken;
        public bool PermanentHazardRemoved;
        public PrototypeSearchLootEntry[] Remaining = Array.Empty<PrototypeSearchLootEntry>();

        public int RemainingAmount
        {
            get { return Remaining == null ? 0 : Remaining.Sum(item => Math.Max(0, item.Amount)); }
        }

        public PrototypeSearchNodeSnapshot Clone()
        {
            return new PrototypeSearchNodeSnapshot
            {
                RunSeed = RunSeed,
                RegionId = RegionId,
                NodeId = NodeId,
                NodeKind = NodeKind,
                State = State,
                SearchCount = SearchCount,
                TimeCostMinutes = TimeCostMinutes,
                EnergyCost = EnergyCost,
                HazardExposureCount = HazardExposureCount,
                HazardId = HazardId,
                BarrierBroken = BarrierBroken,
                PermanentHazardRemoved = PermanentHazardRemoved,
                Remaining = Remaining == null
                    ? Array.Empty<PrototypeSearchLootEntry>()
                    : Remaining.Select(item => item.Clone()).ToArray()
            };
        }
    }

    [Serializable]
    public sealed class PrototypeSearchRegionSnapshot
    {
        public string RegionId = string.Empty;
        public string[] NodeIds = Array.Empty<string>();
        public bool BarrierBroken;
        public bool PermanentHazardRemoved;
        public string[] RemovedPermanentHazardIds = Array.Empty<string>();

        public PrototypeSearchRegionSnapshot Clone()
        {
            return new PrototypeSearchRegionSnapshot
            {
                RegionId = RegionId,
                NodeIds = NodeIds == null ? Array.Empty<string>() : NodeIds.ToArray(),
                BarrierBroken = BarrierBroken,
                PermanentHazardRemoved = PermanentHazardRemoved,
                RemovedPermanentHazardIds = RemovedPermanentHazardIds == null
                    ? Array.Empty<string>()
                    : RemovedPermanentHazardIds.ToArray()
            };
        }
    }

    [Serializable]
    public sealed class PrototypeSearchRunSnapshot
    {
        public int RunSeed;
        public PrototypeSearchNodeSnapshot[] Nodes = Array.Empty<PrototypeSearchNodeSnapshot>();
        public PrototypeSearchRegionSnapshot[] Regions = Array.Empty<PrototypeSearchRegionSnapshot>();
        public string[] ProtectedPartIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class PrototypeSearchRuntimeSnapshot
    {
        public PrototypeSearchRunSnapshot Search = new PrototypeSearchRunSnapshot();
        public PrototypeDiseaseSnapshot Disease = new PrototypeDiseaseSnapshot();
    }

    public sealed class PrototypeSearchNodeDefinition
    {
        public PrototypeSearchNodeDefinition(
            string regionId,
            string archetypeId,
            string nodeId,
            int instanceIndex,
            PrototypeSearchNodeKind kind,
            bool requiresSwimming,
            string searchCostBand,
            int energyCost,
            int timeCostMinutes,
            string hazardId,
            IReadOnlyList<PrototypeSearchResourceAllocation> finiteYield)
        {
            RegionId = regionId ?? string.Empty;
            ArchetypeId = archetypeId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            InstanceIndex = Math.Max(1, instanceIndex);
            Kind = kind;
            RequiresSwimming = requiresSwimming;
            SearchCostBand = searchCostBand ?? string.Empty;
            EnergyCost = Math.Max(1, energyCost);
            TimeCostMinutes = Math.Max(1, timeCostMinutes);
            HazardId = hazardId ?? string.Empty;
            FiniteYield = finiteYield == null
                ? Array.Empty<PrototypeSearchResourceAllocation>()
                : finiteYield.ToArray();
        }

        public string RegionId { get; }
        public string ArchetypeId { get; }
        public string NodeId { get; }
        public int InstanceIndex { get; }
        public PrototypeSearchNodeKind Kind { get; }
        public bool RequiresSwimming { get; }
        public string SearchCostBand { get; }
        public int EnergyCost { get; }
        public int TimeCostMinutes { get; }
        public string HazardId { get; }
        public IReadOnlyList<PrototypeSearchResourceAllocation> FiniteYield { get; }
        public string ProtectedPartId
        {
            get
            {
                return string.Equals(
                    NodeId,
                    PrototypeSearchNodeLootResolver.ResolveSailclothNodeId(PrototypeExpeditionRegionCatalog.DefaultRunSeed),
                    StringComparison.Ordinal)
                    ? PrototypeRaftEscapeConfig.KeyPartId
                    : string.Empty;
            }
        }
        public IReadOnlyList<PrototypeSearchLootEntry> Contents
        {
            get { return PrototypeSearchNodeLootResolver.Resolve(PrototypeExpeditionRegionCatalog.DefaultRunSeed, this); }
        }

        public override string ToString()
        {
            return NodeId + "|region=" + RegionId + "|protected=" + ProtectedPartId;
        }
    }

    public sealed class PrototypeSearchArchetypeDefinition
    {
        public PrototypeSearchArchetypeDefinition(
            string stableId,
            string regionId,
            PrototypeSearchNodeKind kind,
            bool requiresSwimming,
            string searchCostBand,
            string hazardId,
            IReadOnlyList<PrototypeSearchResourceAllocation> finiteYield,
            IReadOnlyList<PrototypeSearchNodeDefinition> instances)
        {
            StableId = stableId ?? string.Empty;
            RegionId = regionId ?? string.Empty;
            Kind = kind;
            RequiresSwimming = requiresSwimming;
            SearchCostBand = searchCostBand ?? string.Empty;
            HazardId = hazardId ?? string.Empty;
            FiniteYield = finiteYield == null ? Array.Empty<PrototypeSearchResourceAllocation>() : finiteYield.ToArray();
            Instances = instances == null ? Array.Empty<PrototypeSearchNodeDefinition>() : instances.ToArray();
        }

        public string StableId { get; }
        public string RegionId { get; }
        public PrototypeSearchNodeKind Kind { get; }
        public bool RequiresSwimming { get; }
        public string SearchCostBand { get; }
        public string HazardId { get; }
        public IReadOnlyList<PrototypeSearchResourceAllocation> FiniteYield { get; }
        public IReadOnlyList<PrototypeSearchNodeDefinition> Instances { get; }
        public int FiniteGeneralUnits { get { return FiniteYield.Sum(value => value.Amount); } }
    }

    public sealed class PrototypeSearchRegionDefinition
    {
        private readonly PrototypeSearchNodeDefinition[] nodes;

        public PrototypeSearchRegionDefinition(string stableId, params PrototypeSearchNodeDefinition[] nodes)
        {
            StableId = stableId ?? string.Empty;
            this.nodes = nodes ?? Array.Empty<PrototypeSearchNodeDefinition>();
        }

        public string StableId { get; }
        public IReadOnlyList<PrototypeSearchNodeDefinition> Nodes { get { return nodes; } }
    }

    public static class PrototypeSearchRegionCatalog
    {
        private static PrototypeSearchResourceAllocation Y(string resourceId, int amount)
        {
            return new PrototypeSearchResourceAllocation(resourceId, amount);
        }

        private sealed class NodeSeed
        {
            public NodeSeed(string nodeId, PrototypeSearchNodeKind kind, bool water, string hazardId)
            {
                NodeId = nodeId;
                Kind = kind;
                Water = water;
                HazardId = hazardId;
            }

            public string NodeId { get; }
            public PrototypeSearchNodeKind Kind { get; }
            public bool Water { get; }
            public string HazardId { get; }
        }

        private static NodeSeed N(string nodeId, PrototypeSearchNodeKind kind, bool water, string hazardId)
        {
            return new NodeSeed(nodeId, kind, water, hazardId);
        }

        private static PrototypeSearchNodeDefinition[] Archetype(
            string regionId,
            string suffix,
            string costBand,
            PrototypeSearchResourceAllocation[] finiteYield,
            params NodeSeed[] instances)
        {
            int energy = string.Equals(costBand, "low", StringComparison.Ordinal) ? 6 :
                string.Equals(costBand, "medium", StringComparison.Ordinal) ? 8 : 10;
            int minutes = string.Equals(costBand, "low", StringComparison.Ordinal) ? 12 :
                string.Equals(costBand, "medium", StringComparison.Ordinal) ? 16 : 20;
            string archetypeId = "node.archetype." + regionId.Substring("region.".Length).Replace('.', '-') + "." + suffix;
            if (instances == null || instances.Length != 2)
            {
                throw new InvalidOperationException(archetypeId + " must declare exactly two stable instances.");
            }
            return instances.Select((instance, index) => new PrototypeSearchNodeDefinition(
                regionId,
                archetypeId,
                instance.NodeId,
                index + 1,
                instance.Kind,
                instance.Water,
                costBand,
                energy,
                minutes,
                instance.HazardId,
                finiteYield)).ToArray();
        }

        private static PrototypeSearchRegionDefinition Region(string stableId, params PrototypeSearchNodeDefinition[][] archetypes)
        {
            return new PrototypeSearchRegionDefinition(stableId, archetypes.SelectMany(value => value).ToArray());
        }

        private static readonly PrototypeSearchRegionDefinition[] Regions =
        {
            Region("region.coast.beach",
                Archetype("region.coast.beach", "driftline", "low", new[] { Y("resource.salvage", 4), Y("resource.wood", 2) },
                    N("node.coast.beach.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf"), N("node.coast.beach.drift-pile.02", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf")),
                Archetype("region.coast.beach", "tide-cache", "low", new[] { Y("resource.food", 4), Y("resource.fabric", 2) },
                    N("node.coast.beach.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects"), N("node.coast.beach.grass-patch.02", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects")),
                Archetype("region.coast.beach", "storm-wrack", "medium", new[] { Y("resource.wood", 4), Y("resource.salvage", 2) },
                    N("node.coast.beach.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"), N("node.coast.beach.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.wildlife"))),
            Region("region.sea.shallows",
                Archetype("region.sea.shallows", "reef-pocket", "medium", new[] { Y("resource.food", 4), Y("resource.stone", 2) },
                    N("node.sea.shallows.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, true, "hazard.injury"), N("node.sea.shallows.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects")),
                Archetype("region.sea.shallows", "submerged-crate", "medium", new[] { Y("resource.salvage", 4), Y("resource.metal", 2) },
                    N("node.sea.shallows.wreck-locker.01", PrototypeSearchNodeKind.WreckLocker, false, "hazard.disaster"), N("node.sea.shallows.wreck-locker.02", PrototypeSearchNodeKind.WreckLocker, false, "hazard.disaster")),
                Archetype("region.sea.shallows", "wreck-scatter", "high", new[] { Y("resource.wire", 4), Y("resource.salvage", 2) },
                    N("node.sea.shallows.drift-pile.01", PrototypeSearchNodeKind.DriftPile, true, "hazard.high-surf"), N("node.sea.shallows.drift-pile.02", PrototypeSearchNodeKind.DriftPile, true, "hazard.high-surf"))),
            Region("region.forest.grove",
                Archetype("region.forest.grove", "deadfall", "medium", new[] { Y("resource.wood", 8) },
                    N("node.forest.grove.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.disease"), N("node.forest.grove.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.disease")),
                Archetype("region.forest.grove", "forage-patch", "low", new[] { Y("resource.food", 4), Y("resource.medicine", 2) },
                    N("node.forest.grove.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants"), N("node.forest.grove.grass-patch.02", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants")),
                Archetype("region.forest.grove", "vine-hollow", "medium", new[] { Y("resource.fiber", 6), Y("resource.wood", 2) },
                    N("node.forest.grove.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"), N("node.forest.grove.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"))),
            Region("region.ridge.highland",
                Archetype("region.ridge.highland", "rockfall", "high", new[] { Y("resource.stone", 8), Y("resource.metal", 2) },
                    N("node.ridge.highland.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"), N("node.ridge.highland.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury")),
                Archetype("region.ridge.highland", "windfall", "medium", new[] { Y("resource.wood", 6), Y("resource.fiber", 2) },
                    N("node.ridge.highland.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.high-wind"), N("node.ridge.highland.grass-patch.02", PrototypeSearchNodeKind.GrassPatch, false, "hazard.high-wind")),
                Archetype("region.ridge.highland", "signal-overlook", "high", new[] { Y("resource.fuel", 2), Y("resource.medicine", 2) },
                    N("node.ridge.highland.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.wildlife"), N("node.ridge.highland.facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster"))),
            Region("region.cave.island",
                Archetype("region.cave.island", "mineral-seam", "high", new[] { Y("resource.stone", 6), Y("resource.metal", 2) },
                    N("node.cave.island.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"), N("node.cave.island.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.wildlife")),
                Archetype("region.cave.island", "dry-cache", "medium", new[] { Y("resource.chemicals", 4), Y("resource.fuel", 2) },
                    N("node.cave.island.facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster"), N("node.cave.island.facility-cabinet.02", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster")),
                Archetype("region.cave.island", "fungus-ledge", "high", new[] { Y("resource.stone", 2), Y("resource.medicine", 2) },
                    N("node.cave.island.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.disease"), N("node.cave.island.drift-pile.02", PrototypeSearchNodeKind.DriftPile, false, "hazard.disease"))),
            Region("region.cove.wreck",
                Archetype("region.cove.wreck", "cargo-locker", "medium", new[] { Y("resource.salvage", 6), Y("resource.metal", 4) },
                    N("node.cove.wreck.wreck-locker.01", PrototypeSearchNodeKind.WreckLocker, false, "hazard.injury"), N("node.cove.wreck.wreck-locker.02", PrototypeSearchNodeKind.WreckLocker, false, "hazard.injury")),
                Archetype("region.cove.wreck", "rigging-locker", "medium", new[] { Y("resource.fabric", 4), Y("resource.fiber", 2) },
                    N("node.cove.wreck.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf"), N("node.cove.wreck.drift-pile.02", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf")),
                Archetype("region.cove.wreck", "engine-bay", "high", new[] { Y("resource.electronics", 4), Y("resource.chemicals", 2) },
                    N("node.cove.wreck.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.disaster"), N("node.cove.wreck.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects"))),
            Region("region.ruins.relay",
                Archetype("region.ruins.relay", "control-cabinet", "high", new[] { Y("resource.electronics", 6), Y("resource.wire", 2) },
                    N("node.ruins.relay.facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disease"), N("node.ruins.relay.facility-cabinet.03", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster")),
                Archetype("region.ruins.relay", "cable-duct", "high", new[] { Y("resource.wire", 6), Y("resource.metal", 2) },
                    N("node.ruins.relay.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"), N("node.ruins.relay.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury")),
                Archetype("region.ruins.relay", "generator-room", "high", new[] { Y("resource.fuel", 4), Y("resource.metal", 2), Y("resource.electronics", 2) },
                    N("node.ruins.relay.facility-cabinet.02", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster"), N("node.ruins.relay.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants")))
        };

        private static readonly PrototypeSearchArchetypeDefinition[] ArchetypeEntries = Regions
            .SelectMany(region => region.Nodes)
            .GroupBy(node => node.ArchetypeId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                PrototypeSearchNodeDefinition first = group.First();
                return new PrototypeSearchArchetypeDefinition(
                    group.Key,
                    first.RegionId,
                    first.Kind,
                    first.RequiresSwimming,
                    first.SearchCostBand,
                    first.HazardId,
                    first.FiniteYield,
                    group.OrderBy(node => node.InstanceIndex).ToArray());
            }).ToArray();

        public static IReadOnlyList<PrototypeSearchRegionDefinition> All { get { return Regions; } }
        public static IReadOnlyList<PrototypeSearchArchetypeDefinition> Archetypes { get { return ArchetypeEntries; } }
        public static IReadOnlyList<PrototypeSearchNodeDefinition> Nodes
        {
            get { return Regions.SelectMany(region => region.Nodes).ToArray(); }
        }

        public static PrototypeSearchRegionDefinition Get(string stableId)
        {
            return Regions.First(region => string.Equals(region.StableId, stableId, StringComparison.Ordinal));
        }

        public static PrototypeSearchRegionDefinition Get(PrototypeExpeditionRegionId region)
        {
            switch (region)
            {
                case PrototypeExpeditionRegionId.Forest:
                    return Get("region.forest.grove");
                case PrototypeExpeditionRegionId.Shallows:
                    return Get("region.sea.shallows");
                default:
                    return Get("region.coast.beach");
            }
        }

        public static PrototypeExpeditionRegionId StartingExpeditionFor(string stableId)
        {
            if (string.Equals(stableId, "region.forest.grove", StringComparison.Ordinal)) return PrototypeExpeditionRegionId.Forest;
            if (string.Equals(stableId, "region.sea.shallows", StringComparison.Ordinal)) return PrototypeExpeditionRegionId.Shallows;
            return PrototypeExpeditionRegionId.Beach;
        }
    }

    [Serializable]
    public sealed class PrototypeSearchNodeContentRoll
    {
        public int RunSeed;
        public string RegionId = string.Empty;
        public string NodeId = string.Empty;
        public PrototypeSearchLootEntry[] Contents = Array.Empty<PrototypeSearchLootEntry>();
    }

    [Serializable]
    public sealed class PrototypeProtectedSearchPartTransactionPolicy
    {
        public string ProtectedPartId = PrototypeRaftEscapeConfig.KeyPartId;
        public bool ProtectedDiscardRejected = true;
        public int ProtectedDuplicateAcquireDelta;
        public int ProtectedDuplicateConsumeDelta;
        public bool ConsumeExactlyOnce = true;
    }

    public static class PrototypeSearchNodeLootResolver
    {
        private static readonly string[] SailclothCandidateNodeIds =
        {
            "node.coast.beach.drift-pile.01",
            "node.sea.shallows.drift-pile.01",
            "node.forest.grove.tree-hollow.01"
        };

        public static string ResolveSailclothNodeId(int runSeed)
        {
            int index = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, PrototypeRaftEscapeConfig.KeyPartId, "search-node-placement"),
                SailclothCandidateNodeIds.Length);
            return SailclothCandidateNodeIds[index];
        }

        public static PrototypeSearchLootEntry[] Resolve(int runSeed, PrototypeSearchNodeDefinition definition)
        {
            List<PrototypeSearchLootEntry> contents = new List<PrototypeSearchLootEntry>();
            IEnumerable<PrototypeSearchResourceAllocation> ordered = definition.FiniteYield
                .OrderBy(allocation => PrototypeExpeditionRegionCatalog.StableHash(
                    runSeed, definition.NodeId, "resource-order." + allocation.ResourceId));
            foreach (PrototypeSearchResourceAllocation allocation in ordered)
            {
                int firstAmount = allocation.Amount <= 1
                    ? allocation.Amount
                    : 1 + PrototypeExpeditionRegionCatalog.PositiveModulo(
                        PrototypeExpeditionRegionCatalog.StableHash(
                            runSeed, definition.ArchetypeId, allocation.ResourceId + ".split"),
                        allocation.Amount - 1);
                int amount = definition.InstanceIndex == 1 ? firstAmount : allocation.Amount - firstAmount;
                if (amount <= 0) continue;
                contents.Add(new PrototypeSearchLootEntry
                {
                    StableItemId = definition.NodeId + ".loot." + allocation.ResourceId.Substring("resource.".Length),
                    ResourceId = allocation.ResourceId,
                    Resource = Carrier(allocation.ResourceId),
                    Amount = amount
                });
            }

            if (string.Equals(definition.NodeId, ResolveSailclothNodeId(runSeed), StringComparison.Ordinal))
            {
                contents.Add(new PrototypeSearchLootEntry
                {
                    StableItemId = definition.NodeId + ".protected.sailcloth",
                    Amount = 1,
                    ProtectedPartId = PrototypeRaftEscapeConfig.KeyPartId
                });
            }
            return contents.ToArray();
        }

        public static PrototypeSearchNodeContentRoll Resolve(int runSeed, string regionId, string nodeId)
        {
            PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.FirstOrDefault(node =>
                string.Equals(node.RegionId, regionId, StringComparison.Ordinal) &&
                string.Equals(node.NodeId, nodeId, StringComparison.Ordinal));
            return new PrototypeSearchNodeContentRoll
            {
                RunSeed = runSeed,
                RegionId = regionId ?? string.Empty,
                NodeId = nodeId ?? string.Empty,
                Contents = definition == null ? Array.Empty<PrototypeSearchLootEntry>() : Resolve(runSeed, definition)
            };
        }

        public static ResourceKind Carrier(string resourceId)
        {
            if (string.Equals(resourceId, "resource.wood", StringComparison.Ordinal)) return ResourceKind.Wood;
            if (string.Equals(resourceId, "resource.stone", StringComparison.Ordinal)) return ResourceKind.Stone;
            if (string.Equals(resourceId, "resource.food", StringComparison.Ordinal) ||
                string.Equals(resourceId, "resource.medicine", StringComparison.Ordinal)) return ResourceKind.Food;
            return ResourceKind.Salvage;
        }

        public static string ResourceId(ResourceKind resource)
        {
            return "resource." + resource.ToString().ToLowerInvariant();
        }
    }

    public sealed class PrototypeSearchNodeLedger
    {
        private readonly Dictionary<string, PrototypeSearchNodeSnapshot> nodes =
            new Dictionary<string, PrototypeSearchNodeSnapshot>(StringComparer.Ordinal);
        private readonly HashSet<string> protectedPartIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, PrototypeSearchRegionSnapshot> regions =
            new Dictionary<string, PrototypeSearchRegionSnapshot>(StringComparer.Ordinal);

        public PrototypeSearchNodeLedger(int runSeed)
        {
            RunSeed = runSeed;
        }

        public int RunSeed { get; private set; }
        public int TotalHazardExposureCount { get { return nodes.Values.Sum(node => node.HazardExposureCount); } }

        public PrototypeSearchNodeSnapshot GetOrCreate(PrototypeSearchNodeDefinition definition)
        {
            PrototypeSearchRegionSnapshot region = GetOrCreateRegion(definition.RegionId);
            if (!nodes.TryGetValue(definition.NodeId, out PrototypeSearchNodeSnapshot snapshot))
            {
                snapshot = new PrototypeSearchNodeSnapshot
                {
                    RunSeed = RunSeed,
                    RegionId = definition.RegionId,
                    NodeId = definition.NodeId,
                    NodeKind = definition.Kind,
                    State = PrototypeSearchNodeState.Hidden,
                    TimeCostMinutes = definition.TimeCostMinutes,
                    EnergyCost = definition.EnergyCost,
                    HazardId = definition.HazardId,
                    Remaining = PrototypeSearchNodeLootResolver.Resolve(RunSeed, definition)
                };
                nodes.Add(definition.NodeId, snapshot);
                region.NodeIds = region.NodeIds.Concat(new[] { definition.NodeId })
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }
            return snapshot;
        }

        public PrototypeSearchRegionSnapshot GetOrCreateRegion(string regionId)
        {
            string stableId = regionId ?? string.Empty;
            if (!regions.TryGetValue(stableId, out PrototypeSearchRegionSnapshot snapshot))
            {
                snapshot = new PrototypeSearchRegionSnapshot { RegionId = stableId };
                regions.Add(stableId, snapshot);
            }
            return snapshot;
        }

        public void MarkBarrierBroken(string regionId)
        {
            PrototypeSearchRegionSnapshot region = GetOrCreateRegion(regionId);
            region.BarrierBroken = true;
            foreach (PrototypeSearchNodeSnapshot node in nodes.Values.Where(node => node.RegionId == region.RegionId))
            {
                node.BarrierBroken = true;
            }
        }

        public void MarkPermanentHazardRemoved(string regionId, string hazardId)
        {
            if (string.IsNullOrEmpty(hazardId)) return;
            PrototypeSearchRegionSnapshot region = GetOrCreateRegion(regionId);
            region.PermanentHazardRemoved = true;
            region.RemovedPermanentHazardIds = region.RemovedPermanentHazardIds.Concat(new[] { hazardId })
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            foreach (PrototypeSearchNodeSnapshot node in nodes.Values.Where(node =>
                         node.RegionId == region.RegionId && node.HazardId == hazardId))
            {
                node.PermanentHazardRemoved = true;
            }
        }

        public bool IsBarrierBroken(string regionId)
        {
            return regions.TryGetValue(regionId ?? string.Empty, out PrototypeSearchRegionSnapshot region) && region.BarrierBroken;
        }

        public bool IsPermanentHazardRemoved(string regionId, string hazardId)
        {
            return regions.TryGetValue(regionId ?? string.Empty, out PrototypeSearchRegionSnapshot region) &&
                   region.RemovedPermanentHazardIds.Contains(hazardId ?? string.Empty);
        }

        public void Reveal(PrototypeSearchNodeDefinition definition)
        {
            PrototypeSearchNodeSnapshot snapshot = GetOrCreate(definition);
            if (snapshot.State != PrototypeSearchNodeState.Hidden) return;
            snapshot.State = snapshot.RemainingAmount > 0
                ? PrototypeSearchNodeState.RevealedPartial
                : PrototypeSearchNodeState.Depleted;
            snapshot.SearchCount += 1;
            snapshot.HazardExposureCount += 1;
        }

        public bool HasProtectedPart(string partId)
        {
            return protectedPartIds.Contains(partId ?? string.Empty);
        }

        public bool TryAcquireProtectedPart(string partId)
        {
            return !string.IsNullOrEmpty(partId) && protectedPartIds.Add(partId);
        }

        public bool Consume(string nodeId, string itemId, int amount)
        {
            if (!nodes.TryGetValue(nodeId ?? string.Empty, out PrototypeSearchNodeSnapshot snapshot) || amount <= 0)
            {
                return false;
            }
            PrototypeSearchLootEntry item = snapshot.Remaining.FirstOrDefault(value =>
                string.Equals(value.StableItemId, itemId, StringComparison.Ordinal));
            if (item == null || item.Amount < amount) return false;
            item.Amount -= amount;
            snapshot.Remaining = snapshot.Remaining.Where(value => value.Amount > 0).ToArray();
            snapshot.State = snapshot.Remaining.Length == 0
                ? PrototypeSearchNodeState.Depleted
                : PrototypeSearchNodeState.RevealedPartial;
            if (snapshot.State == PrototypeSearchNodeState.Depleted)
            {
                MarkPermanentHazardRemoved(snapshot.RegionId, snapshot.HazardId);
            }
            return true;
        }

        public void LeaveDisplacedResource(string nodeId, BagStack displaced)
        {
            if (displaced.IsEmpty || !nodes.TryGetValue(nodeId ?? string.Empty, out PrototypeSearchNodeSnapshot snapshot)) return;
            PrototypeSearchLootEntry existing = snapshot.Remaining.FirstOrDefault(item =>
                !item.IsProtectedPart && item.Resource == displaced.Kind);
            if (existing != null)
            {
                existing.Amount += displaced.Amount;
            }
            else
            {
                List<PrototypeSearchLootEntry> remaining = snapshot.Remaining.ToList();
                remaining.Add(new PrototypeSearchLootEntry
                {
                    StableItemId = snapshot.NodeId + ".left-behind." + displaced.Kind.ToString().ToLowerInvariant(),
                    ResourceId = PrototypeSearchNodeLootResolver.ResourceId(displaced.Kind),
                    Resource = displaced.Kind,
                    Amount = displaced.Amount
                });
                snapshot.Remaining = remaining.ToArray();
            }
            snapshot.State = PrototypeSearchNodeState.RevealedPartial;
        }

        public PrototypeSearchRunSnapshot CaptureSnapshot()
        {
            return new PrototypeSearchRunSnapshot
            {
                RunSeed = RunSeed,
                Nodes = nodes.Values.OrderBy(value => value.NodeId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray(),
                Regions = regions.Values.OrderBy(value => value.RegionId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray(),
                ProtectedPartIds = protectedPartIds.OrderBy(value => value, StringComparer.Ordinal).ToArray()
            };
        }

        public bool RestoreSnapshot(PrototypeSearchRunSnapshot snapshot)
        {
            if (snapshot == null || snapshot.RunSeed != RunSeed) return false;
            PrototypeSearchNodeSnapshot[] source = snapshot.Nodes ?? Array.Empty<PrototypeSearchNodeSnapshot>();
            if (source.Any(node => node == null || node.RunSeed != RunSeed || string.IsNullOrEmpty(node.NodeId)) ||
                source.Select(node => node.NodeId).Distinct(StringComparer.Ordinal).Count() != source.Length)
            {
                return false;
            }
            PrototypeSearchRegionSnapshot[] regionSource = snapshot.Regions ?? Array.Empty<PrototypeSearchRegionSnapshot>();
            if (regionSource.Any(region => region == null || string.IsNullOrEmpty(region.RegionId)) ||
                regionSource.Select(region => region.RegionId).Distinct(StringComparer.Ordinal).Count() != regionSource.Length)
            {
                return false;
            }
            nodes.Clear();
            foreach (PrototypeSearchNodeSnapshot node in source)
            {
                nodes.Add(node.NodeId, node.Clone());
            }
            regions.Clear();
            foreach (PrototypeSearchRegionSnapshot region in regionSource)
            {
                regions.Add(region.RegionId, region.Clone());
            }
            foreach (PrototypeSearchNodeSnapshot node in nodes.Values)
            {
                PrototypeSearchRegionSnapshot region = GetOrCreateRegion(node.RegionId);
                region.NodeIds = region.NodeIds.Concat(new[] { node.NodeId })
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }
            protectedPartIds.Clear();
            foreach (string partId in snapshot.ProtectedPartIds ?? Array.Empty<string>())
            {
                if (!string.IsNullOrEmpty(partId)) protectedPartIds.Add(partId);
            }
            return true;
        }
    }

    public sealed class PrototypeSearchNodeRuntime
    {
        private string activeNodeId = string.Empty;
        private string pendingItemId = string.Empty;
        private bool cycleLatched;

        public PrototypeSearchNodeRuntime(int runSeed)
        {
            Ledger = new PrototypeSearchNodeLedger(runSeed);
            Disease = new PrototypeDiseaseRuntime(runSeed);
        }

        public PrototypeSearchNodeLedger Ledger { get; private set; }
        public PrototypeDiseaseRuntime Disease { get; private set; }
        public bool IsTrayOpen { get { return !string.IsNullOrEmpty(activeNodeId); } }
        public bool HasPendingBagSwap { get { return !string.IsNullOrEmpty(pendingItemId); } }
        public int FocusedIndex { get; private set; }
        public string ActiveNodeId { get { return activeNodeId; } }

        public PrototypeSearchNodeSnapshot ActiveNode
        {
            get
            {
                if (!IsTrayOpen) return null;
                PrototypeSearchRunSnapshot snapshot = Ledger.CaptureSnapshot();
                return snapshot.Nodes.FirstOrDefault(node => string.Equals(node.NodeId, activeNodeId, StringComparison.Ordinal));
            }
        }

        public void Reset(int runSeed)
        {
            Ledger = new PrototypeSearchNodeLedger(runSeed);
            Disease = new PrototypeDiseaseRuntime(runSeed);
            activeNodeId = string.Empty;
            pendingItemId = string.Empty;
            FocusedIndex = 0;
            cycleLatched = false;
        }

        public PrototypeSearchRuntimeSnapshot CaptureSnapshot()
        {
            return new PrototypeSearchRuntimeSnapshot
            {
                Search = Ledger.CaptureSnapshot(),
                Disease = Disease.CaptureSnapshot()
            };
        }

        public bool RestoreSnapshot(PrototypeSearchRuntimeSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Search == null || snapshot.Disease == null) return false;
            PrototypeSearchNodeLedger restoredLedger = new PrototypeSearchNodeLedger(Ledger.RunSeed);
            PrototypeDiseaseRuntime restoredDisease = new PrototypeDiseaseRuntime(Ledger.RunSeed);
            if (!restoredLedger.RestoreSnapshot(snapshot.Search) || !restoredDisease.RestoreSnapshot(snapshot.Disease)) return false;
            Ledger = restoredLedger;
            Disease = restoredDisease;
            activeNodeId = string.Empty;
            pendingItemId = string.Empty;
            FocusedIndex = 0;
            cycleLatched = false;
            return true;
        }

        public PrototypeSearchOpenResult TryOpen(PrototypeSearchNodeDefinition definition, GameSession session)
        {
            if (definition == null || session == null || session.Phase != GamePhase.Exploring)
            {
                return PrototypeSearchOpenResult.Rejected;
            }
            PrototypeSearchNodeSnapshot snapshot = Ledger.GetOrCreate(definition);
            if (snapshot.State == PrototypeSearchNodeState.Depleted) return PrototypeSearchOpenResult.Depleted;
            if (definition.RequiresSwimming && !session.IsSwimming) return PrototypeSearchOpenResult.NeedSwimming;
            if (snapshot.State == PrototypeSearchNodeState.Hidden)
            {
                if (!session.TryApplySearchNodeCost(definition.EnergyCost, definition.TimeCostMinutes))
                {
                    return PrototypeSearchOpenResult.TooTired;
                }
                Ledger.Reveal(definition);
                Disease.TryTelegraph(definition, session.Day);
                Disease.TryExpose(definition, session.Day);
                session.RecordSearchNodeResult(definition.NodeId);
            }
            activeNodeId = definition.NodeId;
            pendingItemId = string.Empty;
            FocusedIndex = 0;
            cycleLatched = false;
            return PrototypeSearchOpenResult.Opened;
        }

        public bool StepFocus(int direction)
        {
            PrototypeSearchNodeSnapshot node = ActiveNode;
            int count = node == null || node.Remaining == null ? 0 : node.Remaining.Length;
            if (count == 0) return false;
            if (direction == 0)
            {
                cycleLatched = false;
                return false;
            }
            if (cycleLatched || HasPendingBagSwap) return false;
            FocusedIndex = (FocusedIndex + (direction < 0 ? -1 : 1) + count) % count;
            cycleLatched = true;
            return true;
        }

        public bool SetFocusedIndex(int index)
        {
            PrototypeSearchNodeSnapshot node = ActiveNode;
            if (node == null || node.Remaining == null || index < 0 || index >= node.Remaining.Length) return false;
            FocusedIndex = index;
            return true;
        }

        public PrototypeSearchTakeResult TryTakeFocused(GameSession session, Func<string, bool> acquireProtectedPart)
        {
            PrototypeSearchNodeSnapshot node = ActiveNode;
            if (node == null || node.Remaining == null || node.Remaining.Length == 0 || HasPendingBagSwap)
            {
                return PrototypeSearchTakeResult.Rejected;
            }
            FocusedIndex = Math.Max(0, Math.Min(FocusedIndex, node.Remaining.Length - 1));
            PrototypeSearchLootEntry item = node.Remaining[FocusedIndex];
            if (item.IsProtectedPart)
            {
                if (Ledger.HasProtectedPart(item.ProtectedPartId)) return PrototypeSearchTakeResult.Rejected;
                if (acquireProtectedPart == null || !acquireProtectedPart(item.ProtectedPartId))
                {
                    return PrototypeSearchTakeResult.Rejected;
                }
                if (!Ledger.TryAcquireProtectedPart(item.ProtectedPartId) ||
                    !Ledger.Consume(node.NodeId, item.StableItemId, item.Amount))
                {
                    return PrototypeSearchTakeResult.Rejected;
                }
                ClampFocus();
                return PrototypeSearchTakeResult.Protected;
            }

            int bagAmount = item.Resource == ResourceKind.Wood && session.HasAxe
                ? item.Amount + 1
                : item.Amount;
            GatherResult result = session.TryStoreSearchLoot(item.Resource, bagAmount);
            if (result == GatherResult.PendingSwap)
            {
                pendingItemId = item.StableItemId;
                return PrototypeSearchTakeResult.PendingSwap;
            }
            if (result != GatherResult.Added || !Ledger.Consume(node.NodeId, item.StableItemId, item.Amount))
            {
                return PrototypeSearchTakeResult.Rejected;
            }
            Disease.ObserveStoredResource(item.ResourceId, item.StableItemId, item.Amount);
            ClampFocus();
            return ActiveNode == null || ActiveNode.State == PrototypeSearchNodeState.Depleted
                ? PrototypeSearchTakeResult.Depleted
                : PrototypeSearchTakeResult.Added;
        }

        public PrototypeSearchTakeResult TryTakeAll(GameSession session, Func<string, bool> acquireProtectedPart)
        {
            PrototypeSearchTakeResult last = PrototypeSearchTakeResult.Rejected;
            int safety = 16;
            while (IsTrayOpen && !HasPendingBagSwap && ActiveNode != null && ActiveNode.Remaining.Length > 0 && safety-- > 0)
            {
                FocusedIndex = 0;
                last = TryTakeFocused(session, acquireProtectedPart);
                if (last == PrototypeSearchTakeResult.Rejected || last == PrototypeSearchTakeResult.PendingSwap) break;
            }
            return last;
        }

        public bool TryReplacePending(GameSession session, int bagSlotIndex)
        {
            PrototypeSearchNodeSnapshot node = ActiveNode;
            if (node == null || !HasPendingBagSwap || session == null || !session.HasPendingLoot) return false;
            PrototypeSearchLootEntry pending = node.Remaining.FirstOrDefault(item =>
                string.Equals(item.StableItemId, pendingItemId, StringComparison.Ordinal));
            if (pending == null) return false;
            if (!session.ReplaceBagSlot(bagSlotIndex, out BagStack displaced)) return false;
            if (!Ledger.Consume(node.NodeId, pending.StableItemId, pending.Amount)) return false;
            Disease.ObserveStoredResource(pending.ResourceId, pending.StableItemId, pending.Amount);
            Ledger.LeaveDisplacedResource(node.NodeId, displaced);
            pendingItemId = string.Empty;
            ClampFocus();
            return true;
        }

        public bool CancelPending(GameSession session)
        {
            if (!HasPendingBagSwap || session == null || !session.HasPendingLoot) return false;
            session.DiscardPendingLoot();
            pendingItemId = string.Empty;
            return true;
        }

        public void Close(GameSession session)
        {
            if (HasPendingBagSwap) CancelPending(session);
            activeNodeId = string.Empty;
            pendingItemId = string.Empty;
            FocusedIndex = 0;
            cycleLatched = false;
        }

        private void ClampFocus()
        {
            PrototypeSearchNodeSnapshot node = ActiveNode;
            int count = node == null || node.Remaining == null ? 0 : node.Remaining.Length;
            FocusedIndex = count == 0 ? 0 : Math.Min(FocusedIndex, count - 1);
        }
    }

    public readonly struct PrototypeSearchNodeContractResult
    {
        public PrototypeSearchNodeContractResult(bool passed, string detail)
        {
            Passed = passed;
            Detail = detail ?? string.Empty;
        }

        public bool Passed { get; }
        public string Detail { get; }
    }

    public static class PrototypeSearchNodeRuntimeContract
    {
        public static PrototypeSearchNodeContractResult Verify()
        {
            int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            IReadOnlyList<PrototypeSearchRegionDefinition> regions = PrototypeSearchRegionCatalog.All;
            bool sevenRegions = regions.Count == 7 && regions.Select(region => region.StableId).Distinct(StringComparer.Ordinal).Count() == 7;
            bool stableNodes = regions.SelectMany(region => region.Nodes).Select(node => node.NodeId)
                .Distinct(StringComparer.Ordinal).Count() == regions.Sum(region => region.Nodes.Count);

            PrototypeSearchNodeDefinition definition = regions[0].Nodes[1];
            PrototypeSearchLootEntry[] first = PrototypeSearchNodeLootResolver.Resolve(seed, definition);
            PrototypeSearchLootEntry[] repeat = PrototypeSearchNodeLootResolver.Resolve(seed, definition);
            bool deterministic = JsonUtility.ToJson(new PrototypeSearchNodeSnapshot { Remaining = first }) ==
                                 JsonUtility.ToJson(new PrototypeSearchNodeSnapshot { Remaining = repeat });

            GameSession session = new GameSession(seed);
            bool began = session.BeginSearch(PrototypeExpeditionRegionId.Beach);
            PrototypeSearchNodeRuntime runtime = new PrototypeSearchNodeRuntime(seed);
            float beforeEnergy = session.Energy;
            float beforeDaylight = session.Daylight;
            PrototypeSearchOpenResult opened = runtime.TryOpen(definition, session);
            int exposureAfterOpen = runtime.Ledger.TotalHazardExposureCount;
            runtime.Close(session);
            PrototypeSearchOpenResult reopened = runtime.TryOpen(definition, session);
            bool costOnce = opened == PrototypeSearchOpenResult.Opened && reopened == PrototypeSearchOpenResult.Opened &&
                            session.Energy == beforeEnergy - definition.EnergyCost &&
                            session.Daylight == beforeDaylight - definition.TimeCostMinutes &&
                            runtime.Ledger.TotalHazardExposureCount == exposureAfterOpen && exposureAfterOpen == 1;

            runtime.Close(session);
            string json = JsonUtility.ToJson(runtime.Ledger.CaptureSnapshot());
            PrototypeSearchNodeLedger restored = new PrototypeSearchNodeLedger(seed);
            bool restoredOk = restored.RestoreSnapshot(JsonUtility.FromJson<PrototypeSearchRunSnapshot>(json));
            PrototypeSearchNodeSnapshot restoredNode = restored.GetOrCreate(definition);
            bool persistence = restoredOk && restoredNode.State == PrototypeSearchNodeState.RevealedPartial &&
                               restoredNode.SearchCount == 1 && restoredNode.RemainingAmount == first.Sum(item => item.Amount);

            PrototypeSearchNodeDefinition sailclothDefinition = regions.SelectMany(region => region.Nodes).First(node =>
                string.Equals(node.NodeId, PrototypeSearchNodeLootResolver.ResolveSailclothNodeId(seed), StringComparison.Ordinal));
            GameSession protectedSession = new GameSession(seed);
            bool protectedBegan = protectedSession.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(sailclothDefinition.RegionId));
            if (sailclothDefinition.RequiresSwimming) protectedSession.SetSwimming(true);
            PrototypeSearchNodeRuntime protectedRuntime = new PrototypeSearchNodeRuntime(seed);
            bool protectedOpened = protectedRuntime.TryOpen(sailclothDefinition, protectedSession) == PrototypeSearchOpenResult.Opened;
            PrototypeSearchNodeSnapshot protectedNode = protectedRuntime.ActiveNode;
            int protectedIndex = protectedNode == null ? -1 : Array.FindIndex(protectedNode.Remaining, item => item.IsProtectedPart);
            int grants = 0;
            bool protectedTaken = protectedIndex >= 0 && protectedRuntime.SetFocusedIndex(protectedIndex) &&
                                  protectedRuntime.TryTakeFocused(protectedSession, delegate { grants += 1; return true; }) == PrototypeSearchTakeResult.Protected;
            bool protectedUnique = protectedRuntime.Ledger.HasProtectedPart(PrototypeRaftEscapeConfig.KeyPartId) && grants == 1 &&
                                   protectedRuntime.ActiveNode.Remaining.All(item => !item.IsProtectedPart);

            GameSession fullBagSession = new GameSession(seed);
            bool fullBagBegan = fullBagSession.BeginSearch(PrototypeExpeditionRegionId.Beach);
            bool filled = fullBagSession.TryStoreSearchLoot(ResourceKind.Wood, 2) == GatherResult.Added &&
                          fullBagSession.TryStoreSearchLoot(ResourceKind.Stone, 2) == GatherResult.Added &&
                          fullBagSession.TryStoreSearchLoot(ResourceKind.Food, 2) == GatherResult.Added &&
                          fullBagSession.TryStoreSearchLoot(ResourceKind.Salvage, 2) == GatherResult.Added;
            PrototypeSearchNodeRuntime swapRuntime = new PrototypeSearchNodeRuntime(seed);
            bool swapOpened = swapRuntime.TryOpen(definition, fullBagSession) == PrototypeSearchOpenResult.Opened;
            PrototypeSearchNodeSnapshot swapNode = swapRuntime.ActiveNode;
            int nonWoodIndex = swapNode == null ? -1 : Array.FindIndex(swapNode.Remaining, item => !item.IsProtectedPart && item.Resource != ResourceKind.Wood);
            int nodeBefore = swapNode == null ? 0 : swapNode.RemainingAmount;
            BagStack slotBefore = fullBagSession.GetBagSlot(0);
            bool pending = nonWoodIndex >= 0 && swapRuntime.SetFocusedIndex(nonWoodIndex) &&
                           swapRuntime.TryTakeFocused(fullBagSession, delegate { return false; }) == PrototypeSearchTakeResult.PendingSwap;
            bool cancelAtomic = pending && swapRuntime.CancelPending(fullBagSession) &&
                                !fullBagSession.HasPendingLoot && swapRuntime.ActiveNode.RemainingAmount == nodeBefore &&
                                fullBagSession.GetBagSlot(0).Kind == slotBefore.Kind && fullBagSession.GetBagSlot(0).Amount == slotBefore.Amount;
            bool pendingAgain = swapRuntime.TryTakeFocused(fullBagSession, delegate { return false; }) == PrototypeSearchTakeResult.PendingSwap;
            bool replaceAtomic = pendingAgain && swapRuntime.TryReplacePending(fullBagSession, 0) &&
                                 !fullBagSession.HasPendingLoot && swapRuntime.ActiveNode.RemainingAmount == nodeBefore -
                                 swapNode.Remaining[nonWoodIndex].Amount + slotBefore.Amount;

            bool passed = sevenRegions && stableNodes && deterministic && began && costOnce && persistence &&
                          protectedBegan && protectedOpened && protectedTaken && protectedUnique &&
                          fullBagBegan && filled && swapOpened && cancelAtomic && replaceAtomic;
            return new PrototypeSearchNodeContractResult(
                passed,
                "regions=7 stableNodeIds=true deterministic=true hidden-partial-depleted=true costOnce=true " +
                "selectionHazardPaused=true cancelAtomic=true replaceAtomic=true sailclothProtectedUnique=true snapshotRestore=true");
        }
    }
}
