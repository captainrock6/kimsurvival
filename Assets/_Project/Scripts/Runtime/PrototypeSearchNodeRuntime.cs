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
        public ResourceKind Resource;
        public int Amount;
        public string ProtectedPartId = string.Empty;

        public bool IsProtectedPart
        {
            get { return !string.IsNullOrEmpty(ProtectedPartId); }
        }

        public PrototypeSearchLootEntry Clone()
        {
            return new PrototypeSearchLootEntry
            {
                StableItemId = StableItemId,
                Resource = Resource,
                Amount = Amount,
                ProtectedPartId = ProtectedPartId
            };
        }
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
                Remaining = Remaining == null
                    ? Array.Empty<PrototypeSearchLootEntry>()
                    : Remaining.Select(item => item.Clone()).ToArray()
            };
        }
    }

    [Serializable]
    public sealed class PrototypeSearchRunSnapshot
    {
        public int RunSeed;
        public PrototypeSearchNodeSnapshot[] Nodes = Array.Empty<PrototypeSearchNodeSnapshot>();
        public string[] ProtectedPartIds = Array.Empty<string>();
    }

    public sealed class PrototypeSearchNodeDefinition
    {
        public PrototypeSearchNodeDefinition(
            string regionId,
            string nodeId,
            PrototypeSearchNodeKind kind,
            bool requiresSwimming,
            int energyCost,
            int timeCostMinutes,
            string hazardId)
        {
            RegionId = regionId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            Kind = kind;
            RequiresSwimming = requiresSwimming;
            EnergyCost = Math.Max(1, energyCost);
            TimeCostMinutes = Math.Max(1, timeCostMinutes);
            HazardId = hazardId ?? string.Empty;
        }

        public string RegionId { get; }
        public string NodeId { get; }
        public PrototypeSearchNodeKind Kind { get; }
        public bool RequiresSwimming { get; }
        public int EnergyCost { get; }
        public int TimeCostMinutes { get; }
        public string HazardId { get; }
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
        private static PrototypeSearchNodeDefinition Node(
            string regionId,
            string suffix,
            PrototypeSearchNodeKind kind,
            bool water,
            string hazardId)
        {
            return new PrototypeSearchNodeDefinition(
                regionId,
                "node." + regionId.Substring("region.".Length) + "." + suffix,
                kind,
                water,
                water ? 9 : 7,
                water ? 18 : 14,
                hazardId);
        }

        private static readonly PrototypeSearchRegionDefinition[] Regions =
        {
            new PrototypeSearchRegionDefinition(
                "region.coast.beach",
                Node("region.coast.beach", "drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf"),
                Node("region.coast.beach", "grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects"),
                Node("region.coast.beach", "rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"),
                Node("region.coast.beach", "tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.wildlife")),
            new PrototypeSearchRegionDefinition(
                "region.sea.shallows",
                Node("region.sea.shallows", "drift-pile.01", PrototypeSearchNodeKind.DriftPile, true, "hazard.high-surf"),
                Node("region.sea.shallows", "rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, true, "hazard.injury"),
                Node("region.sea.shallows", "grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects"),
                Node("region.sea.shallows", "wreck-locker.01", PrototypeSearchNodeKind.WreckLocker, false, "hazard.disaster")),
            new PrototypeSearchRegionDefinition(
                "region.forest.grove",
                Node("region.forest.grove", "tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.wildlife"),
                Node("region.forest.grove", "grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants"),
                Node("region.forest.grove", "rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"),
                Node("region.forest.grove", "drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.insects")),
            new PrototypeSearchRegionDefinition(
                "region.ridge.highland",
                Node("region.ridge.highland", "rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"),
                Node("region.ridge.highland", "grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.high-wind"),
                Node("region.ridge.highland", "tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.wildlife"),
                Node("region.ridge.highland", "facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster")),
            new PrototypeSearchRegionDefinition(
                "region.cave.island",
                Node("region.cave.island", "rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"),
                Node("region.cave.island", "drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.disease"),
                Node("region.cave.island", "tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.wildlife"),
                Node("region.cave.island", "facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster")),
            new PrototypeSearchRegionDefinition(
                "region.cove.wreck",
                Node("region.cove.wreck", "wreck-locker.01", PrototypeSearchNodeKind.WreckLocker, false, "hazard.injury"),
                Node("region.cove.wreck", "drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf"),
                Node("region.cove.wreck", "rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.disaster"),
                Node("region.cove.wreck", "grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects")),
            new PrototypeSearchRegionDefinition(
                "region.ruins.relay",
                Node("region.ruins.relay", "facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disease"),
                Node("region.ruins.relay", "facility-cabinet.02", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster"),
                Node("region.ruins.relay", "rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury"),
                Node("region.ruins.relay", "grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants"))
        };

        public static IReadOnlyList<PrototypeSearchRegionDefinition> All { get { return Regions; } }

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
            ResourceKind[] pattern = Pattern(definition.Kind);
            int offset = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, definition.NodeId, "resource-order"),
                pattern.Length);
            List<PrototypeSearchLootEntry> contents = new List<PrototypeSearchLootEntry>();
            for (int index = 0; index < 2; index += 1)
            {
                ResourceKind resource = pattern[(offset + index) % pattern.Length];
                int amount = resource == ResourceKind.Wood
                    ? 1
                    : 1 + PrototypeExpeditionRegionCatalog.PositiveModulo(
                        PrototypeExpeditionRegionCatalog.StableHash(runSeed, definition.NodeId, "amount." + index), 2);
                contents.Add(new PrototypeSearchLootEntry
                {
                    StableItemId = definition.NodeId + ".loot." + index,
                    Resource = resource,
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

        private static ResourceKind[] Pattern(PrototypeSearchNodeKind kind)
        {
            switch (kind)
            {
                case PrototypeSearchNodeKind.GrassPatch:
                    return new[] { ResourceKind.Food, ResourceKind.Wood, ResourceKind.Stone };
                case PrototypeSearchNodeKind.RockCrevice:
                    return new[] { ResourceKind.Stone, ResourceKind.Salvage, ResourceKind.Food };
                case PrototypeSearchNodeKind.DriftPile:
                    return new[] { ResourceKind.Salvage, ResourceKind.Wood, ResourceKind.Food };
                case PrototypeSearchNodeKind.TreeHollow:
                    return new[] { ResourceKind.Wood, ResourceKind.Food, ResourceKind.Stone };
                case PrototypeSearchNodeKind.WreckLocker:
                case PrototypeSearchNodeKind.FacilityCabinet:
                    return new[] { ResourceKind.Salvage, ResourceKind.Stone, ResourceKind.Food };
                default:
                    return new[] { ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Food, ResourceKind.Salvage };
            }
        }
    }

    public sealed class PrototypeSearchNodeLedger
    {
        private readonly Dictionary<string, PrototypeSearchNodeSnapshot> nodes =
            new Dictionary<string, PrototypeSearchNodeSnapshot>(StringComparer.Ordinal);
        private readonly HashSet<string> protectedPartIds = new HashSet<string>(StringComparer.Ordinal);

        public PrototypeSearchNodeLedger(int runSeed)
        {
            RunSeed = runSeed;
        }

        public int RunSeed { get; private set; }
        public int TotalHazardExposureCount { get { return nodes.Values.Sum(node => node.HazardExposureCount); } }

        public PrototypeSearchNodeSnapshot GetOrCreate(PrototypeSearchNodeDefinition definition)
        {
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
            }
            return snapshot;
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
            nodes.Clear();
            foreach (PrototypeSearchNodeSnapshot node in source)
            {
                nodes.Add(node.NodeId, node.Clone());
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
        }

        public PrototypeSearchNodeLedger Ledger { get; private set; }
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
            activeNodeId = string.Empty;
            pendingItemId = string.Empty;
            FocusedIndex = 0;
            cycleLatched = false;
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
