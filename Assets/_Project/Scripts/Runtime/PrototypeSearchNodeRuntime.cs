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
        public string StableResourceId = string.Empty;
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
                StableResourceId = StableResourceId,
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
        public bool BarrierBroken;
        public bool PermanentHazardRemoved;
        public PrototypeSearchLootEntry[] Remaining = Array.Empty<PrototypeSearchLootEntry>();

        public int RemainingAmount
        {
            get { return Remaining == null ? 0 : Remaining.Sum(item => Math.Max(0, item.Amount)); }
        }

        public int GeneralRemainingAmount
        {
            get
            {
                return Remaining == null
                    ? 0
                    : Remaining.Where(item => !item.IsProtectedPart).Sum(item => Math.Max(0, item.Amount));
            }
        }

        public int ProtectedRemainingAmount
        {
            get
            {
                return Remaining == null
                    ? 0
                    : Remaining.Where(item => item.IsProtectedPart).Sum(item => Math.Max(0, item.Amount));
            }
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
    public sealed class PrototypeProtectedPartAssignmentSnapshot
    {
        public string PartId = string.Empty;
        public string AssignedNodeId = string.Empty;
        public string SourceRegionId = string.Empty;
        public int AssignmentPass = -1;
        public string RepairState = string.Empty;

        public PrototypeProtectedPartAssignmentSnapshot Clone()
        {
            return new PrototypeProtectedPartAssignmentSnapshot
            {
                PartId = PartId,
                AssignedNodeId = AssignedNodeId,
                SourceRegionId = SourceRegionId,
                AssignmentPass = AssignmentPass,
                RepairState = RepairState
            };
        }
    }

    [Serializable]
    public sealed class PrototypeProtectedPartPitySnapshot
    {
        public string PartId = string.Empty;
        public string AssignedNodeId = string.Empty;
        public int EligibleMissCount;
        public string[] CountedNodeIds = Array.Empty<string>();
        public bool HintRevealed;
        public bool GuaranteeArmed;
        public bool Acquired;
        public string SourceNodeId = string.Empty;
        public string RepairState = string.Empty;

        public PrototypeProtectedPartPitySnapshot Clone()
        {
            return new PrototypeProtectedPartPitySnapshot
            {
                PartId = PartId,
                AssignedNodeId = AssignedNodeId,
                EligibleMissCount = EligibleMissCount,
                CountedNodeIds = (CountedNodeIds ?? Array.Empty<string>()).ToArray(),
                HintRevealed = HintRevealed,
                GuaranteeArmed = GuaranteeArmed,
                Acquired = Acquired,
                SourceNodeId = SourceNodeId,
                RepairState = RepairState
            };
        }
    }

    public enum PrototypeSearchEnvironmentalHazardPhase
    {
        Clear,
        Telegraphed,
        Exposed,
        Mitigated,
        Recovered
    }

    [Serializable]
    public sealed class PrototypeSearchEnvironmentalHazardExposureSnapshot
    {
        public int RunSeed;
        public string HazardId = string.Empty;
        public string RegionId = string.Empty;
        public string NodeId = string.Empty;
        public PrototypeSearchEnvironmentalHazardPhase Phase;
        public int WarningCount;
        public int ExposureApplyCount;
        public int EffectApplyCount;
        public int ResponseApplyCount;
        public int RecoveryApplyCount;
        public int HealthDeltaTotal;
        public string LastResultCode = string.Empty;
        public string LastTransactionId = string.Empty;
        public string[] Trace = Array.Empty<string>();

        public PrototypeSearchEnvironmentalHazardExposureSnapshot Clone()
        {
            return new PrototypeSearchEnvironmentalHazardExposureSnapshot
            {
                RunSeed = RunSeed,
                HazardId = HazardId,
                RegionId = RegionId,
                NodeId = NodeId,
                Phase = Phase,
                WarningCount = WarningCount,
                ExposureApplyCount = ExposureApplyCount,
                EffectApplyCount = EffectApplyCount,
                ResponseApplyCount = ResponseApplyCount,
                RecoveryApplyCount = RecoveryApplyCount,
                HealthDeltaTotal = HealthDeltaTotal,
                LastResultCode = LastResultCode,
                LastTransactionId = LastTransactionId,
                Trace = Trace == null ? Array.Empty<string>() : Trace.ToArray()
            };
        }
    }

    [Serializable]
    public sealed class PrototypeSearchEnvironmentalHazardSnapshot
    {
        public int RunSeed;
        public PrototypeSearchEnvironmentalHazardExposureSnapshot[] Exposures =
            Array.Empty<PrototypeSearchEnvironmentalHazardExposureSnapshot>();
    }

    /// <summary>
    /// Non-disease search hazards with a production-input lifecycle. Hidden-node
    /// labels telegraph the hazard, opening commits one health effect, leaving the
    /// tray records the player's retreat/avoidance, and camp return applies one
    /// bounded recovery. Every health mutation uses GameSession transaction IDs.
    /// </summary>
    public sealed class PrototypeSearchEnvironmentalHazardRuntime
    {
        public const string InsectsHazardId = "hazard.insects";
        public const string DangerousPlantsHazardId = "hazard.dangerous-plants";
        public const int InsectsEffectHealthDelta = -4;
        public const int DangerousPlantsEffectHealthDelta = -6;
        public const int InsectsRecoveryHealthDelta = 2;
        public const int DangerousPlantsRecoveryHealthDelta = 3;

        private readonly Dictionary<string, PrototypeSearchEnvironmentalHazardExposureSnapshot> exposures =
            new Dictionary<string, PrototypeSearchEnvironmentalHazardExposureSnapshot>(StringComparer.Ordinal);

        public PrototypeSearchEnvironmentalHazardRuntime(int runSeed)
        {
            RunSeed = runSeed;
        }

        public int RunSeed { get; private set; }
        public IReadOnlyList<PrototypeSearchEnvironmentalHazardExposureSnapshot> Exposures
        {
            get
            {
                return exposures.Values.OrderBy(value => value.NodeId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray();
            }
        }
        public int WarningCount { get { return exposures.Values.Sum(value => value.WarningCount); } }
        public int ExposureApplyCount { get { return exposures.Values.Sum(value => value.ExposureApplyCount); } }
        public int EffectApplyCount { get { return exposures.Values.Sum(value => value.EffectApplyCount); } }
        public int ResponseApplyCount { get { return exposures.Values.Sum(value => value.ResponseApplyCount); } }
        public int RecoveryApplyCount { get { return exposures.Values.Sum(value => value.RecoveryApplyCount); } }
        public string LastFeedbackLocalizationKey { get; private set; } = string.Empty;

        public static bool Supports(string hazardId)
        {
            return string.Equals(hazardId, InsectsHazardId, StringComparison.Ordinal) ||
                   string.Equals(hazardId, DangerousPlantsHazardId, StringComparison.Ordinal);
        }

        public PrototypeSearchEnvironmentalHazardExposureSnapshot Find(string nodeId)
        {
            return exposures.TryGetValue(nodeId ?? string.Empty, out PrototypeSearchEnvironmentalHazardExposureSnapshot value)
                ? value.Clone()
                : null;
        }

        public bool TryTelegraph(PrototypeSearchNodeDefinition definition)
        {
            if (definition == null || !Supports(definition.HazardId) ||
                exposures.ContainsKey(definition.NodeId)) return false;

            var exposure = new PrototypeSearchEnvironmentalHazardExposureSnapshot
            {
                RunSeed = RunSeed,
                HazardId = definition.HazardId,
                RegionId = definition.RegionId,
                NodeId = definition.NodeId,
                Phase = PrototypeSearchEnvironmentalHazardPhase.Telegraphed,
                WarningCount = 1,
                LastResultCode = "search-hazard.result.telegraphed",
                Trace = new[] { "search-hazard.telegraph:" + definition.HazardId + ":" + definition.NodeId }
            };
            exposures.Add(exposure.NodeId, exposure);
            LastFeedbackLocalizationKey = FeedbackKey(exposure);
            return true;
        }

        public bool TryExpose(PrototypeSearchNodeDefinition definition, GameSession session)
        {
            if (definition == null || session == null || !Supports(definition.HazardId)) return false;
            if (!exposures.TryGetValue(definition.NodeId, out PrototypeSearchEnvironmentalHazardExposureSnapshot exposure))
            {
                if (!TryTelegraph(definition) || !exposures.TryGetValue(
                        definition.NodeId, out exposure)) return false;
            }
            if (exposure.Phase != PrototypeSearchEnvironmentalHazardPhase.Telegraphed) return false;

            int healthDelta = EffectHealthDelta(definition.HazardId);
            string transactionId = TransactionId("effect", definition.NodeId);
            int healthBefore = session.Health;
            if (!session.ApplyHealthDelta(transactionId, healthDelta)) return false;

            exposure.Phase = PrototypeSearchEnvironmentalHazardPhase.Exposed;
            exposure.ExposureApplyCount = 1;
            exposure.EffectApplyCount = 1;
            exposure.HealthDeltaTotal += session.Health - healthBefore;
            exposure.LastResultCode = "search-hazard.result.exposed";
            exposure.LastTransactionId = transactionId;
            exposure.Trace = exposure.Trace.Concat(new[]
            {
                "search-hazard.exposure:" + definition.HazardId + ":" + definition.NodeId,
                "search-hazard.effect:" + healthDelta + ":" + transactionId
            }).ToArray();
            LastFeedbackLocalizationKey = FeedbackKey(exposure);
            return true;
        }

        public bool TryMitigateByLeaving(string nodeId)
        {
            if (!exposures.TryGetValue(nodeId ?? string.Empty, out PrototypeSearchEnvironmentalHazardExposureSnapshot exposure) ||
                exposure.Phase != PrototypeSearchEnvironmentalHazardPhase.Exposed) return false;

            exposure.Phase = PrototypeSearchEnvironmentalHazardPhase.Mitigated;
            exposure.ResponseApplyCount = 1;
            exposure.LastResultCode = "search-hazard.result.mitigated-retreat";
            exposure.Trace = exposure.Trace.Concat(new[]
            {
                "search-hazard.response:leave-and-retreat:" + exposure.NodeId,
                "search-hazard.mitigated:" + exposure.HazardId
            }).ToArray();
            LastFeedbackLocalizationKey = FeedbackKey(exposure);
            return true;
        }

        public bool TryRecoverOnCampReturn(GameSession session)
        {
            if (session == null || session.Phase != GamePhase.Camp) return false;
            bool changed = false;
            foreach (PrototypeSearchEnvironmentalHazardExposureSnapshot exposure in exposures.Values
                         .Where(value => value.Phase == PrototypeSearchEnvironmentalHazardPhase.Mitigated)
                         .OrderBy(value => value.NodeId, StringComparer.Ordinal))
            {
                int healthDelta = RecoveryHealthDelta(exposure.HazardId);
                string transactionId = TransactionId("recovery", exposure.NodeId);
                int healthBefore = session.Health;
                if (!session.ApplyHealthDelta(transactionId, healthDelta)) continue;

                exposure.Phase = PrototypeSearchEnvironmentalHazardPhase.Recovered;
                exposure.RecoveryApplyCount = 1;
                exposure.HealthDeltaTotal += session.Health - healthBefore;
                exposure.LastResultCode = "search-hazard.result.recovered";
                exposure.LastTransactionId = transactionId;
                exposure.Trace = exposure.Trace.Concat(new[]
                {
                    "search-hazard.recovery:" + healthDelta + ":" + transactionId
                }).ToArray();
                LastFeedbackLocalizationKey = FeedbackKey(exposure);
                changed = true;
            }
            return changed;
        }

        public PrototypeSearchEnvironmentalHazardSnapshot CaptureSnapshot()
        {
            return new PrototypeSearchEnvironmentalHazardSnapshot
            {
                RunSeed = RunSeed,
                Exposures = Exposures.ToArray()
            };
        }

        public bool RestoreSnapshot(PrototypeSearchEnvironmentalHazardSnapshot snapshot)
        {
            if (snapshot == null) return true;
            if (snapshot.RunSeed != RunSeed) return false;
            PrototypeSearchEnvironmentalHazardExposureSnapshot[] source =
                (snapshot.Exposures ?? Array.Empty<PrototypeSearchEnvironmentalHazardExposureSnapshot>())
                .Select(value => value == null ? null : value.Clone()).ToArray();
            if (source.Any(value => !IsValid(value)) ||
                source.Select(value => value.NodeId).Distinct(StringComparer.Ordinal).Count() != source.Length)
            {
                return false;
            }

            exposures.Clear();
            foreach (PrototypeSearchEnvironmentalHazardExposureSnapshot value in source)
            {
                exposures.Add(value.NodeId, value.Clone());
            }
            LastFeedbackLocalizationKey = source.Length == 0
                ? string.Empty
                : FeedbackKey(source.OrderBy(value => value.NodeId, StringComparer.Ordinal).Last());
            return true;
        }

        private bool IsValid(PrototypeSearchEnvironmentalHazardExposureSnapshot value)
        {
            if (value == null || value.RunSeed != RunSeed || !Supports(value.HazardId) ||
                string.IsNullOrWhiteSpace(value.RegionId) || string.IsNullOrWhiteSpace(value.NodeId) ||
                value.Phase == PrototypeSearchEnvironmentalHazardPhase.Clear || value.WarningCount != 1)
            {
                return false;
            }
            PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.FirstOrDefault(node =>
                string.Equals(node.NodeId, value.NodeId, StringComparison.Ordinal));
            if (definition == null || !string.Equals(definition.RegionId, value.RegionId, StringComparison.Ordinal) ||
                !string.Equals(definition.HazardId, value.HazardId, StringComparison.Ordinal)) return false;

            int phase = (int)value.Phase;
            return value.ExposureApplyCount == (phase >= (int)PrototypeSearchEnvironmentalHazardPhase.Exposed ? 1 : 0) &&
                   value.EffectApplyCount == value.ExposureApplyCount &&
                   value.ResponseApplyCount == (phase >= (int)PrototypeSearchEnvironmentalHazardPhase.Mitigated ? 1 : 0) &&
                   value.RecoveryApplyCount == (phase >= (int)PrototypeSearchEnvironmentalHazardPhase.Recovered ? 1 : 0) &&
                   (value.Trace ?? Array.Empty<string>()).Length >= phase;
        }

        private string TransactionId(string stage, string nodeId)
        {
            return "search-hazard." + stage + "." + RunSeed + "." + (nodeId ?? string.Empty);
        }

        private static int EffectHealthDelta(string hazardId)
        {
            return string.Equals(hazardId, InsectsHazardId, StringComparison.Ordinal)
                ? InsectsEffectHealthDelta
                : DangerousPlantsEffectHealthDelta;
        }

        private static int RecoveryHealthDelta(string hazardId)
        {
            return string.Equals(hazardId, InsectsHazardId, StringComparison.Ordinal)
                ? InsectsRecoveryHealthDelta
                : DangerousPlantsRecoveryHealthDelta;
        }

        private static string FeedbackKey(PrototypeSearchEnvironmentalHazardExposureSnapshot exposure)
        {
            if (exposure == null || !Supports(exposure.HazardId)) return string.Empty;
            string profile = string.Equals(exposure.HazardId, InsectsHazardId, StringComparison.Ordinal)
                ? "insects"
                : "dangerous-plants";
            return "search.hazard.lifecycle." + profile + "." + exposure.Phase.ToString().ToLowerInvariant();
        }
    }

    [Serializable]
    public sealed class PrototypeSearchRunSnapshot
    {
        public string ContractRevision = PrototypeSearchRegionCatalog.ContractRevision;
        public string LootTableRevision = PrototypeSearchRegionCatalog.LootTableRevision;
        public string CatalogRevision = PrototypeSearchRegionCatalog.CatalogRevision;
        public string NewGameStockGenerationEvent = PrototypeSearchRegionCatalog.NewGameStockGenerationEvent;
        public string NewGameStockFingerprint = string.Empty;
        public string[] StockGenerationEvents = { PrototypeSearchRegionCatalog.NewGameStockGenerationEvent };
        public int RunSeed;
        public PrototypeSearchNodeSnapshot[] Nodes = Array.Empty<PrototypeSearchNodeSnapshot>();
        public PrototypeSearchRegionSnapshot[] Regions = Array.Empty<PrototypeSearchRegionSnapshot>();
        public string[] ProtectedPartIds = Array.Empty<string>();
        public PrototypeProtectedPartAssignmentSnapshot[] ProtectedPartAssignments =
            Array.Empty<PrototypeProtectedPartAssignmentSnapshot>();
        public PrototypeProtectedPartPitySnapshot[] ProtectedPartPity =
            Array.Empty<PrototypeProtectedPartPitySnapshot>();
        public PrototypeDiseaseSnapshot Disease;
        public PrototypeSearchEnvironmentalHazardSnapshot EnvironmentalHazards;
    }

    public sealed class PrototypeSearchNodeDefinition
    {
        public PrototypeSearchNodeDefinition(
            string regionId,
            string archetypeId,
            string nodeId,
            int instanceOrdinal,
            string origin,
            PrototypeSearchNodeKind kind,
            bool requiresSwimming,
            int energyCost,
            int timeCostMinutes,
            string hazardId,
            params PrototypeSearchLootEntry[] finiteYield)
        {
            RegionId = regionId ?? string.Empty;
            ArchetypeId = archetypeId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            InstanceOrdinal = Math.Max(1, instanceOrdinal);
            Origin = origin ?? string.Empty;
            Kind = kind;
            RequiresSwimming = requiresSwimming;
            EnergyCost = Math.Max(1, energyCost);
            TimeCostMinutes = Math.Max(1, timeCostMinutes);
            HazardId = hazardId ?? string.Empty;
            FiniteYield = finiteYield == null
                ? Array.Empty<PrototypeSearchLootEntry>()
                : finiteYield.Select(item => item == null ? null : item.Clone()).Where(item => item != null).ToArray();
        }

        public string RegionId { get; }
        public string ArchetypeId { get; }
        public string NodeId { get; }
        public string InstanceId { get { return "node.instance." + NodeId.Substring("node.".Length); } }
        public int InstanceOrdinal { get; }
        public string Origin { get; }
        public PrototypeSearchNodeKind Kind { get; }
        public bool RequiresSwimming { get; }
        public int EnergyCost { get; }
        public int TimeCostMinutes { get; }
        public string HazardId { get; }
        public IReadOnlyList<PrototypeSearchLootEntry> FiniteYield { get; }
        public int GeneralStockUnits { get { return FiniteYield.Sum(item => Math.Max(0, item.Amount)); } }
        public string ProtectedPartId
        {
            get
            {
                PrototypeProtectedPartAssignmentSnapshot assignment =
                    PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                            PrototypeExpeditionRegionCatalog.DefaultRunSeed,
                            PrototypeSearchRegionCatalog.ContractRevision)
                        .FirstOrDefault(value =>
                            string.Equals(value.AssignedNodeId, NodeId, StringComparison.Ordinal));
                return assignment == null ? string.Empty : assignment.PartId;
            }
        }
        public IReadOnlyList<PrototypeSearchLootEntry> Contents
        {
            get { return PrototypeSearchNodeLootResolver.Resolve(PrototypeExpeditionRegionCatalog.DefaultRunSeed, this); }
        }

        public override string ToString()
        {
            return NodeId + "|region=" + RegionId + "|archetype=" + ArchetypeId +
                   "|instance=" + InstanceOrdinal + "|origin=" + Origin + "|generalStock=" + GeneralStockUnits +
                   "|protected=" + ProtectedPartId;
        }
    }

    public sealed class PrototypeSearchNodeArchetypeDefinition
    {
        private readonly PrototypeSearchNodeDefinition[] instances;

        public PrototypeSearchNodeArchetypeDefinition(
            string regionId,
            string stableId,
            string searchCostBand,
            PrototypeSearchNodeKind kind,
            params PrototypeSearchNodeDefinition[] instances)
        {
            RegionId = regionId ?? string.Empty;
            StableId = stableId ?? string.Empty;
            SearchCostBand = searchCostBand ?? string.Empty;
            Kind = kind;
            this.instances = instances ?? Array.Empty<PrototypeSearchNodeDefinition>();
        }

        public string RegionId { get; }
        public string StableId { get; }
        public string SearchCostBand { get; }
        public PrototypeSearchNodeKind Kind { get; }
        public IReadOnlyList<PrototypeSearchNodeDefinition> Instances { get { return instances; } }
    }

    public sealed class PrototypeSearchRegionDefinition
    {
        private readonly PrototypeSearchNodeArchetypeDefinition[] archetypes;
        private readonly PrototypeSearchNodeDefinition[] nodes;

        public PrototypeSearchRegionDefinition(
            string stableId,
            params PrototypeSearchNodeArchetypeDefinition[] archetypes)
        {
            StableId = stableId ?? string.Empty;
            this.archetypes = archetypes ?? Array.Empty<PrototypeSearchNodeArchetypeDefinition>();
            nodes = this.archetypes.SelectMany(archetype => archetype.Instances).ToArray();
        }

        public string StableId { get; }
        public IReadOnlyList<PrototypeSearchNodeArchetypeDefinition> Archetypes { get { return archetypes; } }
        public IReadOnlyList<PrototypeSearchNodeDefinition> Nodes { get { return nodes; } }
    }

    public static class PrototypeSearchRegionCatalog
    {
        public const string ContractRevision = "gamejam.wave-bc.catalog-disease-parts.v1";
        public const string LootTableRevision = "gamejam.o5.loot.same-run-432.v1";
        public const string CatalogRevision = "gamejam.wave-b.7r21a42i.v1";
        public const string NewGameStockGenerationEvent = "new-game-stock-generation";
        public const string BalanceStatus = "BALANCE_PROVISIONAL";
        public const int GameJamResourceYieldMultiplier = 3;
        public const int BalanceProvisionalGeneralStockUnits = 432;

        // Ordinals 0/1/2 are save-compatible with the original Beach/Forest/Shallows enum.
        private static readonly string[] StableRegionIdsByExpeditionOrdinal =
        {
            "region.coast.beach",
            "region.forest.grove",
            "region.sea.shallows",
            "region.ridge.highland",
            "region.cave.island",
            "region.cove.wreck",
            "region.ruins.relay"
        };

        private static readonly string[] LegacyCanonicalIds =
        {
            "node.coast.beach.drift-pile.01", "node.coast.beach.grass-patch.01",
            "node.coast.beach.rock-crevice.01", "node.coast.beach.tree-hollow.01",
            "node.sea.shallows.drift-pile.01", "node.sea.shallows.rock-crevice.01",
            "node.sea.shallows.grass-patch.01", "node.sea.shallows.wreck-locker.01",
            "node.forest.grove.tree-hollow.01", "node.forest.grove.grass-patch.01",
            "node.forest.grove.rock-crevice.01", "node.forest.grove.drift-pile.01",
            "node.ridge.highland.rock-crevice.01", "node.ridge.highland.grass-patch.01",
            "node.ridge.highland.tree-hollow.01", "node.ridge.highland.facility-cabinet.01",
            "node.cave.island.rock-crevice.01", "node.cave.island.drift-pile.01",
            "node.cave.island.tree-hollow.01", "node.cave.island.facility-cabinet.01",
            "node.cove.wreck.wreck-locker.01", "node.cove.wreck.drift-pile.01",
            "node.cove.wreck.rock-crevice.01", "node.cove.wreck.grass-patch.01",
            "node.ruins.relay.facility-cabinet.01", "node.ruins.relay.facility-cabinet.02",
            "node.ruins.relay.rock-crevice.01", "node.ruins.relay.grass-patch.01"
        };

        private static readonly string[] AddedWaveBIds =
        {
            "node.coast.beach.grass-patch.02", "node.coast.beach.rock-crevice.02",
            "node.sea.shallows.drift-pile.02", "node.sea.shallows.wreck-locker.02",
            "node.forest.grove.grass-patch.02", "node.forest.grove.rock-crevice.02",
            "node.ridge.highland.rock-crevice.02", "node.ridge.highland.facility-cabinet.02",
            "node.cave.island.rock-crevice.02", "node.cave.island.tree-hollow.02",
            "node.cove.wreck.grass-patch.02", "node.cove.wreck.rock-crevice.02",
            "node.ruins.relay.rock-crevice.02", "node.ruins.relay.grass-patch.02"
        };

        public static IReadOnlyList<string> ExistingCanonicalNodeIds { get { return LegacyCanonicalIds; } }
        public static IReadOnlyList<string> NewWaveBNodeIds { get { return AddedWaveBIds; } }

        private static PrototypeSearchLootEntry Yield(string stableResourceId, int amount)
        {
            return new PrototypeSearchLootEntry
            {
                StableResourceId = stableResourceId,
                Resource = LegacyFallback(stableResourceId),
                Amount = amount
            };
        }

        private static ResourceKind LegacyFallback(string stableResourceId)
        {
            switch (stableResourceId)
            {
                case "resource.wood":
                case "resource.fiber":
                    return ResourceKind.Wood;
                case "resource.stone":
                case "resource.metal":
                    return ResourceKind.Stone;
                case "resource.food":
                case "resource.medicine":
                    return ResourceKind.Food;
                default:
                    return ResourceKind.Salvage;
            }
        }

        private static PrototypeSearchNodeArchetypeDefinition Pair(
            string regionId,
            string role,
            string searchCostBand,
            string firstId,
            PrototypeSearchNodeKind firstKind,
            bool firstWater,
            string firstHazard,
            string secondId,
            PrototypeSearchNodeKind secondKind,
            bool secondWater,
            string secondHazard,
            params PrototypeSearchLootEntry[] finiteYield)
        {
            string regionSlug = regionId.Substring("region.".Length).Replace('.', '-');
            string archetypeId = "node.archetype." + regionSlug + "." + role;
            return new PrototypeSearchNodeArchetypeDefinition(
                regionId,
                archetypeId,
                searchCostBand,
                firstKind,
                Instance(regionId, archetypeId, firstId, 1, firstKind, firstWater, firstHazard, finiteYield),
                Instance(regionId, archetypeId, secondId, 2, secondKind, secondWater, secondHazard, finiteYield));
        }

        private static PrototypeSearchNodeDefinition Instance(
            string regionId,
            string archetypeId,
            string nodeId,
            int ordinal,
            PrototypeSearchNodeKind kind,
            bool water,
            string hazardId,
            PrototypeSearchLootEntry[] finiteYield)
        {
            bool legacy = LegacyCanonicalIds.Contains(nodeId, StringComparer.Ordinal);
            return new PrototypeSearchNodeDefinition(
                regionId,
                archetypeId,
                nodeId,
                ordinal,
                legacy ? "existing" : "new",
                kind,
                water,
                water ? 9 : 7,
                water ? 18 : 14,
                hazardId,
                finiteYield);
        }

        private static readonly PrototypeSearchRegionDefinition[] Regions =
        {
            new PrototypeSearchRegionDefinition(
                "region.coast.beach",
                Pair("region.coast.beach", "driftline", "low",
                    "node.coast.beach.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf",
                    "node.coast.beach.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.high-surf",
                    Yield("resource.salvage", 2), Yield("resource.wood", 1)),
                Pair("region.coast.beach", "tide-cache", "low",
                    "node.coast.beach.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects",
                    "node.coast.beach.grass-patch.02", PrototypeSearchNodeKind.GrassPatch, false, "hazard.insects",
                    Yield("resource.food", 1), Yield("resource.fabric", 1)),
                Pair("region.coast.beach", "storm-wrack", "medium",
                    "node.coast.beach.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    "node.coast.beach.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    Yield("resource.wood", 2), Yield("resource.salvage", 1))),
            new PrototypeSearchRegionDefinition(
                "region.sea.shallows",
                Pair("region.sea.shallows", "reef-pocket", "medium",
                    "node.sea.shallows.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, true, "hazard.injury",
                    "node.sea.shallows.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.injury",
                    Yield("resource.food", 2), Yield("resource.stone", 1)),
                Pair("region.sea.shallows", "submerged-crate", "medium",
                    "node.sea.shallows.drift-pile.01", PrototypeSearchNodeKind.DriftPile, true, "hazard.high-surf",
                    "node.sea.shallows.drift-pile.02", PrototypeSearchNodeKind.DriftPile, true, "hazard.high-surf",
                    Yield("resource.salvage", 2), Yield("resource.metal", 1)),
                Pair("region.sea.shallows", "wreck-scatter", "high",
                    "node.sea.shallows.wreck-locker.01", PrototypeSearchNodeKind.WreckLocker, false, "hazard.disaster",
                    "node.sea.shallows.wreck-locker.02", PrototypeSearchNodeKind.WreckLocker, false, "hazard.disaster",
                    Yield("resource.wire", 2), Yield("resource.salvage", 1))),
            new PrototypeSearchRegionDefinition(
                "region.forest.grove",
                Pair("region.forest.grove", "deadfall", "medium",
                    "node.forest.grove.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, PrototypeDiseaseRuntime.TriggerHazardId,
                    "node.forest.grove.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, PrototypeDiseaseRuntime.TriggerHazardId,
                    Yield("resource.wood", 4)),
                Pair("region.forest.grove", "forage-patch", "low",
                    "node.forest.grove.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants",
                    "node.forest.grove.grass-patch.02", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants",
                    Yield("resource.food", 2), Yield("resource.medicine", 1)),
                Pair("region.forest.grove", "vine-hollow", "medium",
                    "node.forest.grove.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.wildlife",
                    "node.forest.grove.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.wildlife",
                    Yield("resource.fiber", 3), Yield("resource.medicine", 1))),
            new PrototypeSearchRegionDefinition(
                "region.ridge.highland",
                Pair("region.ridge.highland", "rockfall", "high",
                    "node.ridge.highland.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    "node.ridge.highland.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    Yield("resource.stone", 4), Yield("resource.metal", 1)),
                Pair("region.ridge.highland", "windfall", "medium",
                    "node.ridge.highland.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.high-wind",
                    "node.ridge.highland.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.high-wind",
                    Yield("resource.wood", 3), Yield("resource.fiber", 1)),
                Pair("region.ridge.highland", "signal-overlook", "high",
                    "node.ridge.highland.facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster",
                    "node.ridge.highland.facility-cabinet.02", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster",
                    Yield("resource.fuel", 1), Yield("resource.food", 1))),
            new PrototypeSearchRegionDefinition(
                "region.cave.island",
                Pair("region.cave.island", "mineral-seam", "high",
                    "node.cave.island.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    "node.cave.island.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    Yield("resource.stone", 3), Yield("resource.metal", 1)),
                Pair("region.cave.island", "dry-cache", "medium",
                    "node.cave.island.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.insects",
                    "node.cave.island.facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.insects",
                    Yield("resource.chemicals", 2), Yield("resource.fuel", 1)),
                Pair("region.cave.island", "fungus-ledge", "high",
                    "node.cave.island.tree-hollow.01", PrototypeSearchNodeKind.TreeHollow, false, "hazard.dangerous-plants",
                    "node.cave.island.tree-hollow.02", PrototypeSearchNodeKind.TreeHollow, false, "hazard.dangerous-plants",
                    Yield("resource.stone", 1), Yield("resource.medicine", 1))),
            new PrototypeSearchRegionDefinition(
                "region.cove.wreck",
                Pair("region.cove.wreck", "cargo-locker", "medium",
                    "node.cove.wreck.wreck-locker.01", PrototypeSearchNodeKind.WreckLocker, false, "hazard.high-surf",
                    "node.cove.wreck.drift-pile.01", PrototypeSearchNodeKind.DriftPile, false, "hazard.high-surf",
                    Yield("resource.salvage", 3), Yield("resource.metal", 2)),
                Pair("region.cove.wreck", "rigging-locker", "medium",
                    "node.cove.wreck.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.injury",
                    "node.cove.wreck.grass-patch.02", PrototypeSearchNodeKind.GrassPatch, false, "hazard.injury",
                    Yield("resource.fabric", 2), Yield("resource.fiber", 1), Yield("resource.chemicals", 1)),
                Pair("region.cove.wreck", "engine-bay", "high",
                    "node.cove.wreck.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.disaster",
                    "node.cove.wreck.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.disaster",
                    Yield("resource.electronics", 2), Yield("resource.wood", 1))),
            new PrototypeSearchRegionDefinition(
                "region.ruins.relay",
                Pair("region.ruins.relay", "control-cabinet", "high",
                    "node.ruins.relay.facility-cabinet.01", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster",
                    "node.ruins.relay.facility-cabinet.02", PrototypeSearchNodeKind.FacilityCabinet, false, "hazard.disaster",
                    Yield("resource.electronics", 3), Yield("resource.wire", 1)),
                Pair("region.ruins.relay", "cable-duct", "high",
                    "node.ruins.relay.rock-crevice.01", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    "node.ruins.relay.rock-crevice.02", PrototypeSearchNodeKind.RockCrevice, false, "hazard.injury",
                    Yield("resource.wire", 3), Yield("resource.metal", 1)),
                Pair("region.ruins.relay", "generator-room", "high",
                    "node.ruins.relay.grass-patch.01", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants",
                    "node.ruins.relay.grass-patch.02", PrototypeSearchNodeKind.GrassPatch, false, "hazard.dangerous-plants",
                    Yield("resource.fuel", 2), Yield("resource.metal", 1), Yield("resource.electronics", 1)))
        };

        public static IReadOnlyList<PrototypeSearchRegionDefinition> All { get { return Regions; } }
        public static IReadOnlyList<PrototypeSearchNodeArchetypeDefinition> Archetypes
        {
            get { return Regions.SelectMany(region => region.Archetypes).ToArray(); }
        }
        public static IReadOnlyList<PrototypeSearchNodeDefinition> Nodes
        {
            get { return Regions.SelectMany(region => region.Nodes).ToArray(); }
        }

        public static int GeneralStockUnitsForSeed(int runSeed)
        {
            return Nodes.Sum(node => PrototypeSearchNodeLootResolver.Resolve(runSeed, node)
                .Where(item => !item.IsProtectedPart).Sum(item => Math.Max(0, item.Amount)));
        }

        public static PrototypeSearchRegionDefinition Get(string stableId)
        {
            return Regions.First(region => string.Equals(region.StableId, stableId, StringComparison.Ordinal));
        }

        public static PrototypeSearchRegionDefinition Get(PrototypeExpeditionRegionId region)
        {
            int ordinal = (int)region;
            if (ordinal < 0 || ordinal >= StableRegionIdsByExpeditionOrdinal.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(region), region, "Unknown expedition region enum value.");
            }
            return Get(StableRegionIdsByExpeditionOrdinal[ordinal]);
        }

        public static PrototypeExpeditionRegionId StartingExpeditionFor(string stableId)
        {
            int ordinal = Array.FindIndex(
                StableRegionIdsByExpeditionOrdinal,
                candidate => string.Equals(candidate, stableId, StringComparison.Ordinal));
            if (ordinal < 0)
            {
                throw new KeyNotFoundException("Unknown search region stable ID: " + (stableId ?? "<null>"));
            }
            return (PrototypeExpeditionRegionId)ordinal;
        }

        public static bool VerifyExactRegionRoundTrip()
        {
            if (StableRegionIdsByExpeditionOrdinal.Length != 7 || Regions.Length != 7 ||
                StableRegionIdsByExpeditionOrdinal.Distinct(StringComparer.Ordinal).Count() != 7)
            {
                return false;
            }

            var catalogIds = new HashSet<string>(Regions.Select(region => region.StableId), StringComparer.Ordinal);
            if (!catalogIds.SetEquals(StableRegionIdsByExpeditionOrdinal)) return false;
            for (int ordinal = 0; ordinal < StableRegionIdsByExpeditionOrdinal.Length; ordinal += 1)
            {
                var expedition = (PrototypeExpeditionRegionId)ordinal;
                if (!Enum.IsDefined(typeof(PrototypeExpeditionRegionId), expedition) ||
                    !string.Equals(Get(expedition).StableId, StableRegionIdsByExpeditionOrdinal[ordinal], StringComparison.Ordinal) ||
                    StartingExpeditionFor(StableRegionIdsByExpeditionOrdinal[ordinal]) != expedition)
                {
                    return false;
                }
            }
            return true;
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
    public sealed class PrototypeSearchGeneratedInstanceStock
    {
        public string RegionId = string.Empty;
        public string ArchetypeId = string.Empty;
        public string InstanceId = string.Empty;
        public string LegacyNodeId = string.Empty;
        public PrototypeSearchLootEntry[] FiniteStock = Array.Empty<PrototypeSearchLootEntry>();
    }

    [Serializable]
    public sealed class PrototypeSearchNewGameStockManifest
    {
        public int RunSeed;
        public string ContractRevision = string.Empty;
        public string LootTableRevision = string.Empty;
        public string NewGameStockGenerationEvent = PrototypeSearchRegionCatalog.NewGameStockGenerationEvent;
        public string[] RegionIds = Array.Empty<string>();
        public string[] ArchetypeIds = Array.Empty<string>();
        public PrototypeProtectedPartAssignmentSnapshot[] ProtectedPartAssignments =
            Array.Empty<PrototypeProtectedPartAssignmentSnapshot>();
        public PrototypeSearchGeneratedInstanceStock[] Instances = Array.Empty<PrototypeSearchGeneratedInstanceStock>();
    }

    public static class PrototypeSearchNewGameStockGenerator
    {
        public static PrototypeSearchNewGameStockManifest GenerateNewGameStock(
            int runSeed,
            string contractRevision,
            string lootTableRevision)
        {
            PrototypeProtectedPartAssignmentSnapshot[] protectedAssignments =
                PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(runSeed, contractRevision);
            return new PrototypeSearchNewGameStockManifest
            {
                RunSeed = runSeed,
                ContractRevision = contractRevision ?? string.Empty,
                LootTableRevision = lootTableRevision ?? string.Empty,
                NewGameStockGenerationEvent = PrototypeSearchRegionCatalog.NewGameStockGenerationEvent,
                RegionIds = PrototypeSearchRegionCatalog.All.Select(region => region.StableId).ToArray(),
                ArchetypeIds = PrototypeSearchRegionCatalog.Archetypes.Select(archetype => archetype.StableId).ToArray(),
                ProtectedPartAssignments = protectedAssignments.Select(value => value.Clone()).ToArray(),
                Instances = PrototypeSearchRegionCatalog.Nodes.Select(node => new PrototypeSearchGeneratedInstanceStock
                {
                    RegionId = node.RegionId,
                    ArchetypeId = node.ArchetypeId,
                    InstanceId = node.InstanceId,
                    LegacyNodeId = node.NodeId,
                    FiniteStock = PrototypeSearchNodeLootResolver.Resolve(runSeed, node, protectedAssignments)
                }).ToArray()
            };
        }
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
        public const string FlintPartId = "part.smoke.flint";
        public const string RadioTransceiverPartId = "part.radio.transceiver";
        public const string RadioCircuitBoardPartId = "part.radio.circuit-board";
        public const string RadioTransistorPartId = "part.radio.transistor";
        public const int AssignmentPassCount = 16;

        private static readonly string[] SailclothCandidateNodeIds =
        {
            "node.coast.beach.drift-pile.01",
            "node.sea.shallows.drift-pile.01",
            "node.forest.grove.tree-hollow.01"
        };

        private sealed class ProtectedPartDefinition
        {
            public ProtectedPartDefinition(string partId, bool radio, params string[] eligibleNodeIds)
            {
                PartId = partId;
                Radio = radio;
                EligibleNodeIds = eligibleNodeIds ?? Array.Empty<string>();
            }

            public string PartId { get; }
            public bool Radio { get; }
            public string[] EligibleNodeIds { get; }
        }

        private static readonly ProtectedPartDefinition[] WaveBCProtectedPartDefinitions =
        {
            new ProtectedPartDefinition(
                FlintPartId,
                false,
                "node.cave.island.drift-pile.01",
                "node.cave.island.facility-cabinet.01",
                "node.ridge.highland.grass-patch.01",
                "node.ridge.highland.tree-hollow.01",
                "node.forest.grove.tree-hollow.01",
                "node.forest.grove.drift-pile.01"),
            new ProtectedPartDefinition(
                RadioTransceiverPartId,
                true,
                "node.cove.wreck.rock-crevice.01",
                "node.cove.wreck.rock-crevice.02",
                "node.sea.shallows.drift-pile.01",
                "node.sea.shallows.drift-pile.02",
                "node.ruins.relay.grass-patch.01",
                "node.ruins.relay.grass-patch.02"),
            new ProtectedPartDefinition(
                RadioCircuitBoardPartId,
                true,
                "node.ruins.relay.facility-cabinet.01",
                "node.ruins.relay.facility-cabinet.02",
                "node.cove.wreck.rock-crevice.01",
                "node.cove.wreck.rock-crevice.02",
                "node.sea.shallows.wreck-locker.01",
                "node.sea.shallows.wreck-locker.02"),
            new ProtectedPartDefinition(
                RadioTransistorPartId,
                true,
                "node.ridge.highland.facility-cabinet.01",
                "node.ridge.highland.facility-cabinet.02",
                "node.cave.island.drift-pile.01",
                "node.cave.island.facility-cabinet.01",
                "node.ruins.relay.rock-crevice.01",
                "node.ruins.relay.rock-crevice.02")
        };

        public static IReadOnlyList<string> ProtectedPartIds
        {
            get
            {
                return new[]
                {
                    PrototypeRaftEscapeConfig.KeyPartId,
                    FlintPartId,
                    RadioTransceiverPartId,
                    RadioCircuitBoardPartId,
                    RadioTransistorPartId
                };
            }
        }

        public static IReadOnlyList<string> EligibleNodeIdsFor(string partId)
        {
            if (string.Equals(partId, PrototypeRaftEscapeConfig.KeyPartId, StringComparison.Ordinal))
            {
                return SailclothCandidateNodeIds.ToArray();
            }
            ProtectedPartDefinition definition = WaveBCProtectedPartDefinitions.FirstOrDefault(value =>
                string.Equals(value.PartId, partId, StringComparison.Ordinal));
            return definition == null ? Array.Empty<string>() : definition.EligibleNodeIds.ToArray();
        }

        public static string ResolveSailclothNodeId(int runSeed)
        {
            int index = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, PrototypeRaftEscapeConfig.KeyPartId, "search-node-placement"),
                SailclothCandidateNodeIds.Length);
            return SailclothCandidateNodeIds[index];
        }

        public static PrototypeProtectedPartAssignmentSnapshot[] ResolveProtectedPartAssignments(
            int runSeed,
            string contractRevision)
        {
            PrototypeProtectedPartAssignmentSnapshot sailcloth = CreateAssignment(
                PrototypeRaftEscapeConfig.KeyPartId,
                ResolveSailclothNodeId(runSeed),
                -1,
                "legacy-sailcloth");

            for (int passIndex = 0; passIndex < AssignmentPassCount; passIndex += 1)
            {
                var assignments = new List<PrototypeProtectedPartAssignmentSnapshot> { sailcloth.Clone() };
                var usedNodes = new HashSet<string>(StringComparer.Ordinal) { sailcloth.AssignedNodeId };
                var usedRadioRegions = new HashSet<string>(StringComparer.Ordinal);
                bool valid = true;
                for (int definitionIndex = 0;
                     definitionIndex < WaveBCProtectedPartDefinitions.Length;
                     definitionIndex += 1)
                {
                    ProtectedPartDefinition definition = WaveBCProtectedPartDefinitions[definitionIndex];
                    ulong hash = Hash64(
                        runSeed,
                        contractRevision,
                        definition.PartId,
                        passIndex,
                        "protected-part");
                    string nodeId = definition.EligibleNodeIds[(int)(hash % (ulong)definition.EligibleNodeIds.Length)];
                    string regionId = RegionIdForNode(nodeId);
                    if (usedNodes.Contains(nodeId) ||
                        (definition.Radio && usedRadioRegions.Contains(regionId)))
                    {
                        valid = false;
                        break;
                    }

                    assignments.Add(CreateAssignment(definition.PartId, nodeId, passIndex, "initial-pass"));
                    usedNodes.Add(nodeId);
                    if (definition.Radio) usedRadioRegions.Add(regionId);
                }
                if (valid) return assignments.ToArray();
            }

            return ResolveProtectedPartAssignmentsWithRepair(runSeed, contractRevision, sailcloth);
        }

        public static ulong Hash64(
            int runSeed,
            string contractRevision,
            string partId,
            int passIndex,
            string purpose)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                AppendHash(ref hash, runSeed);
                AppendHash(ref hash, contractRevision);
                AppendHash(ref hash, partId);
                AppendHash(ref hash, passIndex);
                AppendHash(ref hash, purpose);
                return hash;
            }
        }

        public static PrototypeSearchLootEntry[] Resolve(int runSeed, PrototypeSearchNodeDefinition definition)
        {
            return Resolve(
                runSeed,
                definition,
                ResolveProtectedPartAssignments(runSeed, PrototypeSearchRegionCatalog.ContractRevision));
        }

        public static PrototypeSearchLootEntry[] Resolve(
            int runSeed,
            PrototypeSearchNodeDefinition definition,
            IReadOnlyList<PrototypeProtectedPartAssignmentSnapshot> protectedAssignments)
        {
            List<PrototypeSearchLootEntry> contents = ResolveGeneralStock(runSeed, definition).ToList();
            if (definition == null) return contents.ToArray();

            PrototypeProtectedPartAssignmentSnapshot[] nodeAssignments = (protectedAssignments ??
                    Array.Empty<PrototypeProtectedPartAssignmentSnapshot>())
                .Where(value => value != null &&
                                string.Equals(value.AssignedNodeId, definition.NodeId, StringComparison.Ordinal))
                .ToArray();
            for (int assignmentIndex = 0; assignmentIndex < nodeAssignments.Length; assignmentIndex += 1)
            {
                PrototypeProtectedPartAssignmentSnapshot assignment = nodeAssignments[assignmentIndex];
                string suffix = string.Equals(
                    assignment.PartId,
                    PrototypeRaftEscapeConfig.KeyPartId,
                    StringComparison.Ordinal)
                    ? "sailcloth"
                    : assignment.PartId.Replace("part.", string.Empty).Replace('.', '-');
                contents.Add(new PrototypeSearchLootEntry
                {
                    StableItemId = definition.NodeId + ".protected." + suffix,
                    Amount = 1,
                    ProtectedPartId = assignment.PartId
                });
            }
            return contents.ToArray();
        }

        public static PrototypeSearchLootEntry[] ResolveGeneralStock(
            int runSeed,
            PrototypeSearchNodeDefinition definition)
        {
            if (definition == null)
            {
                return Array.Empty<PrototypeSearchLootEntry>();
            }
            PrototypeSearchLootEntry[] finiteYield = definition.FiniteYield
                .Where(item => item != null && item.Amount > 0 && !string.IsNullOrWhiteSpace(item.StableResourceId))
                .Select(item => item.Clone()).ToArray();
            int offset = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, definition.NodeId, "finite-yield-display-order"),
                Math.Max(1, finiteYield.Length));
            var contents = new List<PrototypeSearchLootEntry>();
            for (int index = 0; index < finiteYield.Length; index += 1)
            {
                PrototypeSearchLootEntry item = finiteYield[(offset + index) % finiteYield.Length];
                string resourceSuffix = item.StableResourceId.StartsWith("resource.", StringComparison.Ordinal)
                    ? item.StableResourceId.Substring("resource.".Length)
                    : item.StableResourceId.Replace('.', '-');
                for (int batch = 0; batch < PrototypeSearchRegionCatalog.GameJamResourceYieldMultiplier; batch += 1)
                {
                    contents.Add(new PrototypeSearchLootEntry
                    {
                        StableItemId = definition.NodeId + ".resource." + resourceSuffix + ".o5." + batch,
                        StableResourceId = item.StableResourceId,
                        Resource = item.Resource,
                        Amount = item.Amount
                    });
                }
            }

            return contents.ToArray();
        }

        public static string StableResourceIdForLegacy(ResourceKind resource)
        {
            return "resource." + resource.ToString().ToLowerInvariant();
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

        private static PrototypeProtectedPartAssignmentSnapshot[] ResolveProtectedPartAssignmentsWithRepair(
            int runSeed,
            string contractRevision,
            PrototypeProtectedPartAssignmentSnapshot sailcloth)
        {
            var assignments = new List<PrototypeProtectedPartAssignmentSnapshot> { sailcloth.Clone() };
            var usedNodes = new HashSet<string>(StringComparer.Ordinal) { sailcloth.AssignedNodeId };
            var usedRadioRegions = new HashSet<string>(StringComparer.Ordinal);
            for (int definitionIndex = 0;
                 definitionIndex < WaveBCProtectedPartDefinitions.Length;
                 definitionIndex += 1)
            {
                ProtectedPartDefinition definition = WaveBCProtectedPartDefinitions[definitionIndex];
                string nodeId = definition.EligibleNodeIds
                    .Where(candidate => !usedNodes.Contains(candidate) &&
                                        (!definition.Radio || !usedRadioRegions.Contains(RegionIdForNode(candidate))))
                    .OrderBy(candidate => RepairRank(runSeed, contractRevision, definition.PartId, candidate))
                    .ThenBy(candidate => candidate, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(nodeId))
                {
                    throw new InvalidOperationException("Protected-part deterministic repair has no valid candidate: " + definition.PartId);
                }

                PrototypeProtectedPartAssignmentSnapshot assignment = CreateAssignment(
                    definition.PartId,
                    nodeId,
                    AssignmentPassCount,
                    "deterministic-repair");
                assignments.Add(assignment);
                usedNodes.Add(nodeId);
                if (definition.Radio) usedRadioRegions.Add(assignment.SourceRegionId);
            }
            return assignments.ToArray();
        }

        private static PrototypeProtectedPartAssignmentSnapshot CreateAssignment(
            string partId,
            string nodeId,
            int passIndex,
            string repairState)
        {
            return new PrototypeProtectedPartAssignmentSnapshot
            {
                PartId = partId ?? string.Empty,
                AssignedNodeId = nodeId ?? string.Empty,
                SourceRegionId = RegionIdForNode(nodeId),
                AssignmentPass = passIndex,
                RepairState = repairState ?? string.Empty
            };
        }

        private static string RegionIdForNode(string nodeId)
        {
            PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.FirstOrDefault(value =>
                string.Equals(value.NodeId, nodeId, StringComparison.Ordinal));
            return definition == null ? string.Empty : definition.RegionId;
        }

        private static ulong RepairRank(int runSeed, string contractRevision, string partId, string nodeId)
        {
            ulong baseRank = Hash64(runSeed, contractRevision, partId, AssignmentPassCount, "protected-part");
            ulong nodeRank = Hash64(runSeed, contractRevision, nodeId, AssignmentPassCount, "protected-part-repair");
            return baseRank ^ ((nodeRank << 17) | (nodeRank >> 47));
        }

        private static void AppendHash(ref ulong hash, int value)
        {
            unchecked
            {
                uint bits = (uint)value;
                for (int shift = 0; shift < 32; shift += 8)
                {
                    hash = (hash ^ (byte)(bits >> shift)) * 1099511628211UL;
                }
                hash = (hash ^ 0xffUL) * 1099511628211UL;
            }
        }

        private static void AppendHash(ref ulong hash, string value)
        {
            unchecked
            {
                string stable = value ?? string.Empty;
                for (int index = 0; index < stable.Length; index += 1)
                {
                    char character = stable[index];
                    hash = (hash ^ (byte)character) * 1099511628211UL;
                    hash = (hash ^ (byte)(character >> 8)) * 1099511628211UL;
                }
                hash = (hash ^ 0xffUL) * 1099511628211UL;
            }
        }

    }

    public sealed class PrototypeSearchNodeLedger
    {
        private readonly Dictionary<string, PrototypeSearchNodeSnapshot> nodes =
            new Dictionary<string, PrototypeSearchNodeSnapshot>(StringComparer.Ordinal);
        private readonly HashSet<string> protectedPartIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, PrototypeSearchRegionSnapshot> regions =
            new Dictionary<string, PrototypeSearchRegionSnapshot>(StringComparer.Ordinal);
        private readonly Dictionary<string, PrototypeSearchLootEntry[]> generatedNewGameStock =
            new Dictionary<string, PrototypeSearchLootEntry[]>(StringComparer.Ordinal);
        private readonly Dictionary<string, PrototypeProtectedPartAssignmentSnapshot> protectedPartAssignments =
            new Dictionary<string, PrototypeProtectedPartAssignmentSnapshot>(StringComparer.Ordinal);
        private readonly Dictionary<string, PrototypeProtectedPartPitySnapshot> protectedPartPity =
            new Dictionary<string, PrototypeProtectedPartPitySnapshot>(StringComparer.Ordinal);
        private readonly List<string> stockGenerationEvents = new List<string>();
        private readonly bool allowNewGameStockGeneration;
        private string newGameStockFingerprint = string.Empty;

        public PrototypeSearchNodeLedger(int runSeed)
            : this(runSeed, true)
        {
        }

        private PrototypeSearchNodeLedger(int runSeed, bool initializeNewGameStock)
        {
            RunSeed = runSeed;
            Disease = new PrototypeDiseaseRuntime(runSeed);
            EnvironmentalHazards = new PrototypeSearchEnvironmentalHazardRuntime(runSeed);
            allowNewGameStockGeneration = initializeNewGameStock;
            if (!initializeNewGameStock) return;
            PrototypeSearchNewGameStockManifest manifest = PrototypeSearchNewGameStockGenerator.GenerateNewGameStock(
                runSeed,
                PrototypeSearchRegionCatalog.ContractRevision,
                PrototypeSearchRegionCatalog.LootTableRevision);
            foreach (PrototypeProtectedPartAssignmentSnapshot assignment in manifest.ProtectedPartAssignments)
            {
                protectedPartAssignments.Add(assignment.PartId, assignment.Clone());
            }
            InitializeProtectedPartPity(protectedPartAssignments.Values, Array.Empty<string>());
            foreach (PrototypeSearchGeneratedInstanceStock instance in manifest.Instances)
            {
                generatedNewGameStock[instance.LegacyNodeId] = instance.FiniteStock
                    .Select(item => item.Clone()).ToArray();
            }
            newGameStockFingerprint = JsonUtility.ToJson(manifest);
            stockGenerationEvents.Add(manifest.NewGameStockGenerationEvent);
            foreach (PrototypeSearchRegionDefinition region in PrototypeSearchRegionCatalog.All)
            {
                GetOrCreateRegion(region.StableId);
                foreach (PrototypeSearchNodeDefinition node in region.Nodes)
                {
                    GetOrCreate(node);
                }
            }
        }

        public int RunSeed { get; private set; }
        public PrototypeDiseaseRuntime Disease { get; private set; }
        public PrototypeSearchEnvironmentalHazardRuntime EnvironmentalHazards { get; private set; }
        public int TotalHazardExposureCount { get { return nodes.Values.Sum(node => node.HazardExposureCount); } }
        public int GeneralRemainingAmount { get { return nodes.Values.Sum(node => node.GeneralRemainingAmount); } }
        public int ProtectedRemainingAmount { get { return nodes.Values.Sum(node => node.ProtectedRemainingAmount); } }
        public IReadOnlyList<string> StockGenerationEvents { get { return stockGenerationEvents; } }
        public string NewGameStockFingerprint { get { return newGameStockFingerprint; } }
        public IReadOnlyList<PrototypeProtectedPartAssignmentSnapshot> ProtectedPartAssignments
        {
            get
            {
                return protectedPartAssignments.Values
                    .OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray();
            }
        }
        public IReadOnlyList<PrototypeProtectedPartPitySnapshot> ProtectedPartPity
        {
            get
            {
                return protectedPartPity.Values
                    .OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray();
            }
        }

        public static PrototypeSearchNodeLedger CreateForRestore(int runSeed)
        {
            return new PrototypeSearchNodeLedger(runSeed, false);
        }

        public int GetRegionInitialGeneralAmount(string regionId)
        {
            PrototypeSearchRegionDefinition region = PrototypeSearchRegionCatalog.Get(regionId);
            return region.Nodes.Sum(definition =>
                generatedNewGameStock.TryGetValue(definition.NodeId, out PrototypeSearchLootEntry[] stock)
                    ? stock.Where(item => !item.IsProtectedPart).Sum(item => Math.Max(0, item.Amount))
                    : PrototypeSearchNodeLootResolver.Resolve(RunSeed, definition)
                        .Where(item => !item.IsProtectedPart).Sum(item => Math.Max(0, item.Amount)));
        }

        public int GetRegionRemainingGeneralAmount(string regionId)
        {
            return nodes.Values
                .Where(node => string.Equals(node.RegionId, regionId, StringComparison.Ordinal))
                .Sum(node => node.GeneralRemainingAmount);
        }

        public int GetRegionRemainingPercent(string regionId)
        {
            int initial = GetRegionInitialGeneralAmount(regionId);
            if (initial <= 0) return 0;
            return Mathf.Clamp(Mathf.RoundToInt(GetRegionRemainingGeneralAmount(regionId) * 100f / initial), 0, 100);
        }

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
                    Remaining = generatedNewGameStock.TryGetValue(definition.NodeId, out PrototypeSearchLootEntry[] generated)
                        ? generated.Select(item => item.Clone()).ToArray()
                        : allowNewGameStockGeneration
                            ? PrototypeSearchNodeLootResolver.Resolve(RunSeed, definition)
                            : Array.Empty<PrototypeSearchLootEntry>()
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
            if (string.IsNullOrEmpty(partId) || !protectedPartIds.Add(partId)) return false;
            if (protectedPartPity.TryGetValue(partId, out PrototypeProtectedPartPitySnapshot pity))
            {
                pity.Acquired = true;
                pity.SourceNodeId = pity.AssignedNodeId;
                pity.RepairState = pity.GuaranteeArmed
                    ? "pity-guaranteed-next-eligible-node"
                    : "assigned-node-acquired";
            }
            return true;
        }

        public bool TryRecordEligibleProtectedPartNodeResult(
            string sourceNodeId,
            string partId,
            bool canCommitGuarantee,
            out PrototypeProtectedPartPitySnapshot result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(sourceNodeId) || string.IsNullOrWhiteSpace(partId) ||
                !protectedPartPity.TryGetValue(partId, out PrototypeProtectedPartPitySnapshot pity) ||
                pity.Acquired || protectedPartIds.Contains(partId) ||
                !PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(partId).Contains(sourceNodeId))
            {
                return false;
            }

            HashSet<string> counted = new HashSet<string>(pity.CountedNodeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (counted.Contains(sourceNodeId)) return false;
            if (pity.GuaranteeArmed)
            {
                if (!canCommitGuarantee || !TryAcquireProtectedPartFromPity(partId, sourceNodeId)) return false;
                pity = protectedPartPity[partId];
                result = pity.Clone();
                return true;
            }

            counted.Add(sourceNodeId);
            pity.CountedNodeIds = counted.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            pity.EligibleMissCount = Math.Min(
                CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount,
                pity.CountedNodeIds.Length);
            pity.HintRevealed = pity.EligibleMissCount >= CampaignKeyPartPityConfig.EligibleHintSearchCount;
            pity.GuaranteeArmed = pity.EligibleMissCount >= CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount;
            pity.RepairState = pity.GuaranteeArmed ? "pity-guarantee-armed" : pity.HintRevealed ? "pity-hint" : "pity-miss";
            result = pity.Clone();
            return true;
        }

        private bool TryAcquireProtectedPartFromPity(string partId, string sourceNodeId)
        {
            if (!protectedPartPity.TryGetValue(partId, out PrototypeProtectedPartPitySnapshot pity) ||
                pity.Acquired || protectedPartIds.Contains(partId))
            {
                return false;
            }
            foreach (PrototypeSearchNodeSnapshot node in nodes.Values)
            {
                node.Remaining = (node.Remaining ?? Array.Empty<PrototypeSearchLootEntry>())
                    .Where(item => item == null || !item.IsProtectedPart ||
                                   !string.Equals(item.ProtectedPartId, partId, StringComparison.Ordinal))
                    .ToArray();
                if (node.State != PrototypeSearchNodeState.Hidden && node.Remaining.Length == 0)
                {
                    node.State = PrototypeSearchNodeState.Depleted;
                    MarkPermanentHazardRemoved(node.RegionId, node.HazardId);
                }
            }
            if (!protectedPartIds.Add(partId)) return false;
            pity.Acquired = true;
            pity.SourceNodeId = sourceNodeId;
            pity.RepairState = "pity-guaranteed-next-eligible-node";
            return true;
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
                !item.IsProtectedPart &&
                string.Equals(
                    string.IsNullOrEmpty(item.StableResourceId)
                        ? PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(item.Resource)
                        : item.StableResourceId,
                    string.IsNullOrEmpty(displaced.StableResourceId)
                        ? PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(displaced.Kind)
                        : displaced.StableResourceId,
                    StringComparison.Ordinal));
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
                    StableResourceId = string.IsNullOrEmpty(displaced.StableResourceId)
                        ? PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(displaced.Kind)
                        : displaced.StableResourceId,
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
                ContractRevision = PrototypeSearchRegionCatalog.ContractRevision,
                LootTableRevision = PrototypeSearchRegionCatalog.LootTableRevision,
                CatalogRevision = PrototypeSearchRegionCatalog.CatalogRevision,
                NewGameStockGenerationEvent = stockGenerationEvents.FirstOrDefault() ?? string.Empty,
                NewGameStockFingerprint = newGameStockFingerprint,
                StockGenerationEvents = stockGenerationEvents.ToArray(),
                RunSeed = RunSeed,
                Nodes = nodes.Values.OrderBy(value => value.NodeId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray(),
                Regions = regions.Values.OrderBy(value => value.RegionId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray(),
                ProtectedPartIds = protectedPartIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                ProtectedPartAssignments = protectedPartAssignments.Values
                    .OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray(),
                ProtectedPartPity = protectedPartPity.Values
                    .OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray(),
                Disease = Disease.CaptureSnapshot(),
                EnvironmentalHazards = EnvironmentalHazards.CaptureSnapshot()
            };
        }

        public bool RestoreSnapshot(PrototypeSearchRunSnapshot snapshot)
        {
            if (snapshot == null || snapshot.RunSeed != RunSeed)
            {
                return false;
            }
            PrototypeSearchNodeSnapshot[] source = (snapshot.Nodes ?? Array.Empty<PrototypeSearchNodeSnapshot>())
                .Select(node => node == null ? null : node.Clone()).ToArray();
            HashSet<string> sourceIds = new HashSet<string>(
                source.Where(node => node != null).Select(node => node.NodeId),
                StringComparer.Ordinal);
            bool exactRevision =
                string.Equals(snapshot.ContractRevision, PrototypeSearchRegionCatalog.ContractRevision, StringComparison.Ordinal) &&
                string.Equals(snapshot.LootTableRevision, PrototypeSearchRegionCatalog.LootTableRevision, StringComparison.Ordinal) &&
                string.Equals(snapshot.CatalogRevision, PrototypeSearchRegionCatalog.CatalogRevision, StringComparison.Ordinal);
            bool migrateLegacy28 = source.Length == 28 &&
                PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.All(sourceIds.Contains) &&
                sourceIds.All(id => PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.Contains(id));
            if (!exactRevision && !migrateLegacy28) return false;
            HashSet<string> expectedNodeIds = new HashSet<string>(
                PrototypeSearchRegionCatalog.Nodes.Select(node => node.NodeId),
                StringComparer.Ordinal);
            if (source.Any(node => node == null || node.RunSeed != RunSeed || string.IsNullOrEmpty(node.NodeId)) ||
                source.Select(node => node.NodeId).Distinct(StringComparer.Ordinal).Count() != source.Length ||
                (!migrateLegacy28 && source.Length != expectedNodeIds.Count) ||
                source.Any(node => !expectedNodeIds.Contains(node.NodeId)))
            {
                return false;
            }
            PrototypeSearchRegionSnapshot[] regionSource = snapshot.Regions ?? Array.Empty<PrototypeSearchRegionSnapshot>();
            HashSet<string> expectedRegionIds = new HashSet<string>(
                PrototypeSearchRegionCatalog.All.Select(region => region.StableId),
                StringComparer.Ordinal);
            if (regionSource.Any(region => region == null || string.IsNullOrEmpty(region.RegionId)) ||
                regionSource.Select(region => region.RegionId).Distinct(StringComparer.Ordinal).Count() != regionSource.Length ||
                regionSource.Length != expectedRegionIds.Count ||
                regionSource.Any(region => !expectedRegionIds.Contains(region.RegionId)))
            {
                return false;
            }
            if (!TryBuildRestoredProtectedAssignments(snapshot, source, out PrototypeProtectedPartAssignmentSnapshot[] restoredAssignments))
            {
                return false;
            }
            if (!TryBuildRestoredProtectedPartPity(snapshot, restoredAssignments, out PrototypeProtectedPartPitySnapshot[] restoredPity))
            {
                return false;
            }
            var restoredDisease = new PrototypeDiseaseRuntime(RunSeed);
            if (!restoredDisease.RestoreSnapshot(snapshot.Disease)) return false;
            var restoredEnvironmentalHazards = new PrototypeSearchEnvironmentalHazardRuntime(RunSeed);
            if (!restoredEnvironmentalHazards.RestoreSnapshot(snapshot.EnvironmentalHazards)) return false;

            nodes.Clear();
            foreach (PrototypeSearchNodeSnapshot node in source)
            {
                foreach (PrototypeSearchLootEntry item in node.Remaining ?? Array.Empty<PrototypeSearchLootEntry>())
                {
                    if (!item.IsProtectedPart && string.IsNullOrEmpty(item.StableResourceId))
                    {
                        item.StableResourceId = PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(item.Resource);
                    }
                }
                nodes.Add(node.NodeId, node.Clone());
            }
            if (migrateLegacy28)
            {
                foreach (PrototypeSearchNodeDefinition definition in PrototypeSearchRegionCatalog.Nodes.Where(node =>
                             !nodes.ContainsKey(node.NodeId)))
                {
                    nodes.Add(definition.NodeId, new PrototypeSearchNodeSnapshot
                    {
                        RunSeed = RunSeed,
                        RegionId = definition.RegionId,
                        NodeId = definition.NodeId,
                        NodeKind = definition.Kind,
                        State = PrototypeSearchNodeState.Hidden,
                        TimeCostMinutes = definition.TimeCostMinutes,
                        EnergyCost = definition.EnergyCost,
                        HazardId = definition.HazardId,
                        Remaining = PrototypeSearchNodeLootResolver.ResolveGeneralStock(RunSeed, definition)
                    });
                }
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
            protectedPartAssignments.Clear();
            foreach (PrototypeProtectedPartAssignmentSnapshot assignment in restoredAssignments)
            {
                protectedPartAssignments.Add(assignment.PartId, assignment.Clone());
            }
            protectedPartPity.Clear();
            foreach (PrototypeProtectedPartPitySnapshot pity in restoredPity)
            {
                protectedPartPity.Add(pity.PartId, pity.Clone());
            }
            Disease = restoredDisease;
            EnvironmentalHazards = restoredEnvironmentalHazards;
            stockGenerationEvents.Clear();
            stockGenerationEvents.AddRange(snapshot.StockGenerationEvents ?? Array.Empty<string>());
            newGameStockFingerprint = snapshot.NewGameStockFingerprint ?? string.Empty;
            if (migrateLegacy28) stockGenerationEvents.Add("migration-add-wave-b-14-stock");
            return true;
        }

        private void InitializeProtectedPartPity(
            IEnumerable<PrototypeProtectedPartAssignmentSnapshot> assignments,
            IEnumerable<string> acquiredPartIds)
        {
            var acquired = new HashSet<string>(acquiredPartIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            protectedPartPity.Clear();
            foreach (PrototypeProtectedPartAssignmentSnapshot assignment in assignments ??
                     Array.Empty<PrototypeProtectedPartAssignmentSnapshot>())
            {
                protectedPartPity[assignment.PartId] = new PrototypeProtectedPartPitySnapshot
                {
                    PartId = assignment.PartId,
                    AssignedNodeId = assignment.AssignedNodeId,
                    Acquired = acquired.Contains(assignment.PartId),
                    SourceNodeId = acquired.Contains(assignment.PartId) ? assignment.AssignedNodeId : string.Empty,
                    RepairState = acquired.Contains(assignment.PartId) ? "legacy-acquired" : assignment.RepairState
                };
            }
        }

        private static bool TryBuildRestoredProtectedAssignments(
            PrototypeSearchRunSnapshot snapshot,
            IReadOnlyList<PrototypeSearchNodeSnapshot> source,
            out PrototypeProtectedPartAssignmentSnapshot[] restoredAssignments)
        {
            restoredAssignments = Array.Empty<PrototypeProtectedPartAssignmentSnapshot>();
            var expectedPartIds = new HashSet<string>(
                PrototypeSearchNodeLootResolver.ProtectedPartIds,
                StringComparer.Ordinal);
            string[] acquiredPartIds = snapshot.ProtectedPartIds ?? Array.Empty<string>();
            if (acquiredPartIds.Any(string.IsNullOrWhiteSpace) ||
                acquiredPartIds.Distinct(StringComparer.Ordinal).Count() != acquiredPartIds.Length ||
                acquiredPartIds.Any(partId => !expectedPartIds.Contains(partId)))
            {
                return false;
            }

            var acquired = new HashSet<string>(acquiredPartIds, StringComparer.Ordinal);
            var remainingAssignments = new Dictionary<string, PrototypeProtectedPartAssignmentSnapshot>(StringComparer.Ordinal);
            var occupiedNodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (PrototypeSearchNodeSnapshot node in source)
            {
                PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.FirstOrDefault(value =>
                    string.Equals(value.NodeId, node.NodeId, StringComparison.Ordinal));
                if (definition == null || !string.Equals(definition.RegionId, node.RegionId, StringComparison.Ordinal))
                {
                    return false;
                }

                PrototypeSearchLootEntry[] protectedItems = (node.Remaining ?? Array.Empty<PrototypeSearchLootEntry>())
                    .Where(item => item != null && item.IsProtectedPart).ToArray();
                if (protectedItems.Length > 1) return false;
                foreach (PrototypeSearchLootEntry item in protectedItems)
                {
                    string partId = item.ProtectedPartId ?? string.Empty;
                    if (item.Amount != 1 || !expectedPartIds.Contains(partId) || acquired.Contains(partId) ||
                        remainingAssignments.ContainsKey(partId) || !occupiedNodes.Add(node.NodeId) ||
                        !PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(partId).Contains(node.NodeId))
                    {
                        return false;
                    }
                    remainingAssignments.Add(partId, new PrototypeProtectedPartAssignmentSnapshot
                    {
                        PartId = partId,
                        AssignedNodeId = node.NodeId,
                        SourceRegionId = definition.RegionId,
                        AssignmentPass = -1,
                        RepairState = "legacy-snapshot"
                    });
                }
            }

            PrototypeProtectedPartAssignmentSnapshot[] savedAssignments =
                snapshot.ProtectedPartAssignments ?? Array.Empty<PrototypeProtectedPartAssignmentSnapshot>();
            if (savedAssignments.Length == 0)
            {
                restoredAssignments = remainingAssignments.Values
                    .OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => value.Clone()).ToArray();
                return true;
            }
            if (savedAssignments.Length != expectedPartIds.Count ||
                savedAssignments.Any(value => value == null || string.IsNullOrWhiteSpace(value.PartId) ||
                                              string.IsNullOrWhiteSpace(value.AssignedNodeId) ||
                                              string.IsNullOrWhiteSpace(value.SourceRegionId)) ||
                savedAssignments.Select(value => value.PartId).Distinct(StringComparer.Ordinal).Count() != expectedPartIds.Count ||
                savedAssignments.Select(value => value.AssignedNodeId).Distinct(StringComparer.Ordinal).Count() != expectedPartIds.Count ||
                savedAssignments.Any(value => !expectedPartIds.Contains(value.PartId)))
            {
                return false;
            }

            foreach (PrototypeProtectedPartAssignmentSnapshot assignment in savedAssignments)
            {
                PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.FirstOrDefault(value =>
                    string.Equals(value.NodeId, assignment.AssignedNodeId, StringComparison.Ordinal));
                bool stillInNode = remainingAssignments.TryGetValue(
                    assignment.PartId,
                    out PrototypeProtectedPartAssignmentSnapshot remainingAssignment);
                if (definition == null ||
                    !string.Equals(definition.RegionId, assignment.SourceRegionId, StringComparison.Ordinal) ||
                    !PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(assignment.PartId).Contains(assignment.AssignedNodeId) ||
                    (!stillInNode && !acquired.Contains(assignment.PartId)) ||
                    (stillInNode && !string.Equals(
                        remainingAssignment.AssignedNodeId,
                        assignment.AssignedNodeId,
                        StringComparison.Ordinal)))
                {
                    return false;
                }
            }

            string[] radioPartIds =
            {
                PrototypeSearchNodeLootResolver.RadioTransceiverPartId,
                PrototypeSearchNodeLootResolver.RadioCircuitBoardPartId,
                PrototypeSearchNodeLootResolver.RadioTransistorPartId
            };
            if (savedAssignments.Where(value => radioPartIds.Contains(value.PartId))
                    .Select(value => value.SourceRegionId).Distinct(StringComparer.Ordinal).Count() != radioPartIds.Length)
            {
                return false;
            }

            restoredAssignments = savedAssignments.OrderBy(value => value.PartId, StringComparer.Ordinal)
                .Select(value => value.Clone()).ToArray();
            return true;
        }

        private static bool TryBuildRestoredProtectedPartPity(
            PrototypeSearchRunSnapshot snapshot,
            IReadOnlyList<PrototypeProtectedPartAssignmentSnapshot> assignments,
            out PrototypeProtectedPartPitySnapshot[] restoredPity)
        {
            restoredPity = Array.Empty<PrototypeProtectedPartPitySnapshot>();
            var assignmentByPart = assignments.ToDictionary(value => value.PartId, StringComparer.Ordinal);
            var acquired = new HashSet<string>(snapshot.ProtectedPartIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            PrototypeProtectedPartPitySnapshot[] saved = snapshot.ProtectedPartPity ??
                                                          Array.Empty<PrototypeProtectedPartPitySnapshot>();
            if (saved.Length == 0)
            {
                restoredPity = assignments.OrderBy(value => value.PartId, StringComparer.Ordinal)
                    .Select(value => new PrototypeProtectedPartPitySnapshot
                    {
                        PartId = value.PartId,
                        AssignedNodeId = value.AssignedNodeId,
                        Acquired = acquired.Contains(value.PartId),
                        SourceNodeId = acquired.Contains(value.PartId) ? value.AssignedNodeId : string.Empty,
                        RepairState = acquired.Contains(value.PartId) ? "legacy-acquired" : value.RepairState
                    }).ToArray();
                return true;
            }

            if (saved.Length != assignmentByPart.Count || saved.Any(value => value == null) ||
                saved.Select(value => value.PartId).Distinct(StringComparer.Ordinal).Count() != assignmentByPart.Count)
            {
                return false;
            }

            foreach (PrototypeProtectedPartPitySnapshot pity in saved)
            {
                if (!assignmentByPart.TryGetValue(pity.PartId ?? string.Empty, out PrototypeProtectedPartAssignmentSnapshot assignment) ||
                    !string.Equals(pity.AssignedNodeId, assignment.AssignedNodeId, StringComparison.Ordinal) ||
                    pity.EligibleMissCount < 0 || pity.EligibleMissCount > CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount ||
                    pity.Acquired != acquired.Contains(pity.PartId))
                {
                    return false;
                }
                string[] counted = pity.CountedNodeIds ?? Array.Empty<string>();
                if (counted.Any(string.IsNullOrWhiteSpace) ||
                    counted.Distinct(StringComparer.Ordinal).Count() != counted.Length ||
                    counted.Any(nodeId => !PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(pity.PartId).Contains(nodeId)) ||
                    pity.EligibleMissCount != counted.Length ||
                    pity.HintRevealed != (pity.EligibleMissCount >= CampaignKeyPartPityConfig.EligibleHintSearchCount) ||
                    pity.GuaranteeArmed != (pity.EligibleMissCount >= CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount) ||
                    (pity.Acquired && (string.IsNullOrWhiteSpace(pity.SourceNodeId) ||
                                       !PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(pity.PartId).Contains(pity.SourceNodeId))) ||
                    (!pity.Acquired && !string.IsNullOrEmpty(pity.SourceNodeId)))
                {
                    return false;
                }
            }

            string[] radioPartIds =
            {
                PrototypeSearchNodeLootResolver.RadioTransceiverPartId,
                PrototypeSearchNodeLootResolver.RadioCircuitBoardPartId,
                PrototypeSearchNodeLootResolver.RadioTransistorPartId
            };
            string[] acquiredRadioRegions = saved.Where(value => value.Acquired && radioPartIds.Contains(value.PartId))
                .Select(value => PrototypeSearchRegionCatalog.Nodes.First(node =>
                    string.Equals(node.NodeId, value.SourceNodeId, StringComparison.Ordinal)).RegionId)
                .ToArray();
            if (acquiredRadioRegions.Distinct(StringComparer.Ordinal).Count() != acquiredRadioRegions.Length)
            {
                return false;
            }

            restoredPity = saved.OrderBy(value => value.PartId, StringComparer.Ordinal)
                .Select(value => value.Clone()).ToArray();
            return true;
        }
    }

    public sealed class PrototypeSearchNodeRuntime
    {
        private string activeNodeId = string.Empty;
        private string pendingItemId = string.Empty;
        private bool cycleLatched;
        private string lastFeedbackLocalizationKey = string.Empty;

        public PrototypeSearchNodeRuntime(int runSeed)
        {
            Ledger = new PrototypeSearchNodeLedger(runSeed);
        }

        private PrototypeSearchNodeRuntime(PrototypeSearchNodeLedger ledger)
        {
            Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        }

        public static PrototypeSearchNodeRuntime CreateForRestore(int runSeed)
        {
            return new PrototypeSearchNodeRuntime(PrototypeSearchNodeLedger.CreateForRestore(runSeed));
        }

        public static bool TryCreateFromSnapshot(
            PrototypeSearchRunSnapshot snapshot,
            out PrototypeSearchNodeRuntime runtime)
        {
            runtime = null;
            if (snapshot == null) return false;
            PrototypeSearchNodeRuntime candidate = CreateForRestore(snapshot.RunSeed);
            if (!candidate.RestoreSnapshot(snapshot)) return false;
            runtime = candidate;
            return true;
        }

        public PrototypeSearchNodeLedger Ledger { get; private set; }
        public PrototypeDiseaseRuntime Disease { get { return Ledger.Disease; } }
        public PrototypeSearchEnvironmentalHazardRuntime EnvironmentalHazards { get { return Ledger.EnvironmentalHazards; } }
        public string LastFeedbackLocalizationKey { get { return lastFeedbackLocalizationKey; } }
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
            lastFeedbackLocalizationKey = string.Empty;
            PrototypeProtectedPartPityRuntimeBridge.RestoreNaturalSearchPity(runSeed, Ledger.ProtectedPartPity);
        }

        public bool RestoreSnapshot(PrototypeSearchRunSnapshot snapshot)
        {
            if (snapshot == null) return false;
            PrototypeSearchNodeLedger candidate = PrototypeSearchNodeLedger.CreateForRestore(snapshot.RunSeed);
            if (candidate.StockGenerationEvents.Count != 0 || !string.IsNullOrEmpty(candidate.NewGameStockFingerprint) ||
                !candidate.RestoreSnapshot(snapshot))
            {
                return false;
            }

            string previousActiveNodeId = activeNodeId;
            Ledger = candidate;
            activeNodeId = snapshot.Nodes != null && snapshot.Nodes.Any(node =>
                    node != null && string.Equals(node.NodeId, previousActiveNodeId, StringComparison.Ordinal))
                ? previousActiveNodeId
                : string.Empty;
            pendingItemId = string.Empty;
            FocusedIndex = 0;
            cycleLatched = false;
            lastFeedbackLocalizationKey = string.Empty;
            ClampFocus();
            PrototypeProtectedPartPityRuntimeBridge.RestoreNaturalSearchPity(Ledger.RunSeed, Ledger.ProtectedPartPity);
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
                if (string.Equals(definition.HazardId, PrototypeDiseaseRuntime.TriggerHazardId, StringComparison.Ordinal))
                {
                    Disease.TryTelegraph(definition.RegionId, definition.NodeId);
                }
                else if (PrototypeSearchEnvironmentalHazardRuntime.Supports(definition.HazardId))
                {
                    EnvironmentalHazards.TryTelegraph(definition);
                }
                int appliedEnergyCost = definition.EnergyCost + Disease.ActiveSearchEnergyPenalty;
                if (!session.TryApplySearchNodeCost(appliedEnergyCost, definition.TimeCostMinutes))
                {
                    return PrototypeSearchOpenResult.TooTired;
                }
                snapshot.EnergyCost = appliedEnergyCost;
                Ledger.Reveal(definition);
                session.RecordSearchNodeResult(definition.NodeId);
                if (string.Equals(definition.HazardId, PrototypeDiseaseRuntime.TriggerHazardId, StringComparison.Ordinal))
                {
                    Disease.TryExposeFromSearch(definition);
                }
                else if (PrototypeSearchEnvironmentalHazardRuntime.Supports(definition.HazardId))
                {
                    EnvironmentalHazards.TryExpose(definition, session);
                    lastFeedbackLocalizationKey = EnvironmentalHazards.LastFeedbackLocalizationKey;
                }
            }
            activeNodeId = definition.NodeId;
            pendingItemId = string.Empty;
            FocusedIndex = 0;
            cycleLatched = false;
            return PrototypeSearchOpenResult.Opened;
        }

        public bool TryTelegraphDisease(PrototypeSearchNodeDefinition definition)
        {
            return definition != null &&
                   string.Equals(definition.HazardId, PrototypeDiseaseRuntime.TriggerHazardId, StringComparison.Ordinal) &&
                   Disease.TryTelegraph(definition.RegionId, definition.NodeId);
        }

        public bool TryTelegraphEnvironmentalHazard(PrototypeSearchNodeDefinition definition)
        {
            return definition != null && EnvironmentalHazards.TryTelegraph(definition);
        }

        public bool NotifyReturnToCamp(GameSession session, bool forced)
        {
            bool diseaseApplied = Disease.TryEnterCamp(session, forced);
            bool environmentalRecoveryApplied = EnvironmentalHazards.TryRecoverOnCampReturn(session);
            lastFeedbackLocalizationKey = diseaseApplied
                ? Disease.FeedbackLocalizationKey
                : environmentalRecoveryApplied
                    ? EnvironmentalHazards.LastFeedbackLocalizationKey
                    : string.Empty;
            return diseaseApplied || environmentalRecoveryApplied;
        }

        public bool NotifyDaySettlement(GameSession session)
        {
            return Disease.TrySettleDay(session);
        }

        public bool TryTreatDisease(GameSession session, bool hasWorkbench)
        {
            return Disease.TryTreat(session, hasWorkbench);
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
                ResolveCompletedNodeProtectedPartPity(node.NodeId);
                ClampFocus();
                return PrototypeSearchTakeResult.Protected;
            }

            int bagAmount = item.Amount;
            GatherResult result = session.TryStoreSearchLoot(item.StableResourceId, item.Resource, bagAmount);
            if (result == GatherResult.PendingSwap)
            {
                pendingItemId = item.StableItemId;
                return PrototypeSearchTakeResult.PendingSwap;
            }
            if (result != GatherResult.Added || !Ledger.Consume(node.NodeId, item.StableItemId, item.Amount))
            {
                return PrototypeSearchTakeResult.Rejected;
            }
            ResolveCompletedNodeProtectedPartPity(node.NodeId);
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
            ResolveCompletedNodeProtectedPartPity(node.NodeId);
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
            if (EnvironmentalHazards.TryMitigateByLeaving(activeNodeId))
            {
                lastFeedbackLocalizationKey = EnvironmentalHazards.LastFeedbackLocalizationKey;
            }
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

        private void ResolveCompletedNodeProtectedPartPity(string sourceNodeId)
        {
            PrototypeSearchNodeSnapshot completedNode = Ledger.CaptureSnapshot().Nodes.FirstOrDefault(node =>
                string.Equals(node.NodeId, sourceNodeId, StringComparison.Ordinal));
            if (completedNode == null || completedNode.State != PrototypeSearchNodeState.Depleted) return;

            PrototypeProtectedPartAssignmentSnapshot assignedAtNode = Ledger.ProtectedPartAssignments.FirstOrDefault(value =>
                string.Equals(value.AssignedNodeId, sourceNodeId, StringComparison.Ordinal));
            var acquiredRadioRegions = new HashSet<string>(
                Ledger.ProtectedPartPity.Where(value => value.Acquired && IsRadioPart(value.PartId))
                    .Select(value => PrototypeSearchRegionCatalog.Nodes.First(node =>
                        string.Equals(node.NodeId, value.SourceNodeId, StringComparison.Ordinal)).RegionId),
                StringComparer.Ordinal);
            foreach (string partId in PrototypeSearchNodeLootResolver.ProtectedPartIds)
            {
                if (Ledger.HasProtectedPart(partId) ||
                    !PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(partId).Contains(sourceNodeId))
                {
                    continue;
                }
                bool noProtectedCollision = assignedAtNode == null ||
                                            string.Equals(assignedAtNode.PartId, partId, StringComparison.Ordinal);
                bool distinctRadioRegion = !IsRadioPart(partId) || !acquiredRadioRegions.Contains(completedNode.RegionId);
                bool canCommitGuarantee = noProtectedCollision && distinctRadioRegion;
                if (!Ledger.TryRecordEligibleProtectedPartNodeResult(
                        sourceNodeId,
                        partId,
                        canCommitGuarantee,
                        out PrototypeProtectedPartPitySnapshot result))
                {
                    continue;
                }
                PrototypeProtectedPartPityRuntimeBridge.RecordNaturalSearchNodeResult(
                    Ledger.RunSeed,
                    sourceNodeId,
                    result,
                    canCommitGuarantee);
                if (result.Acquired && IsRadioPart(partId)) acquiredRadioRegions.Add(completedNode.RegionId);
            }
        }

        private static bool IsRadioPart(string partId)
        {
            return string.Equals(partId, PrototypeSearchNodeLootResolver.RadioTransceiverPartId, StringComparison.Ordinal) ||
                   string.Equals(partId, PrototypeSearchNodeLootResolver.RadioCircuitBoardPartId, StringComparison.Ordinal) ||
                   string.Equals(partId, PrototypeSearchNodeLootResolver.RadioTransistorPartId, StringComparison.Ordinal);
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

    [Serializable]
    public sealed class PrototypeSeedStableResourceAmount
    {
        public string StableResourceId = string.Empty;
        public int SearchStock;
        public int StartingStorage;
        public int TotalAvailable;
    }

    [Serializable]
    public sealed class PrototypeEscapeRouteSeedAudit
    {
        public string EscapeId = string.Empty;
        public int Seed;
        public bool ResourceAffordable;
        public bool ProtectedPartsAvailable;
        public bool NaturallyCompletable;
        public int RequiredGeneralUnits;
        public int RemainingAvailableUnits;
        public StableResourceAmount[] RequiredResources = Array.Empty<StableResourceAmount>();
        public string[] RequiredProtectedPartIds = Array.Empty<string>();
        public string[] ProtectedPartNodeIds = Array.Empty<string>();
        public string[] ProtectedPartRegionIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class PrototypeEscapeResourceSeedAuditResult
    {
        public int Seed;
        public int RegionCount;
        public int NodeCount;
        public int GeneralStockUnits;
        public int ProtectedPartUnits;
        public bool ExactStableStock;
        public bool StableCatalogComplete;
        public bool ProtectedAssignmentsValid;
        public bool RadioPartsUseDistinctRegions;
        public bool AtLeastOneRouteCompletable;
        public bool AllPlayableRoutesCompletable;
        public bool NoSoftlock;
        public PrototypeSeedStableResourceAmount[] StableResources = Array.Empty<PrototypeSeedStableResourceAmount>();
        public PrototypeProtectedPartAssignmentSnapshot[] ProtectedAssignments =
            Array.Empty<PrototypeProtectedPartAssignmentSnapshot>();
        public PrototypeEscapeRouteSeedAudit[] Routes = Array.Empty<PrototypeEscapeRouteSeedAudit>();
    }

    /// <summary>
    /// Audits route affordability against the finite node catalog, rather than synthetic
    /// fixture grants. Each route includes its current workbench/research/crafting setup
    /// and the protected parts actually placed into this seed's search nodes.
    /// </summary>
    public static class PrototypeEscapeResourceSeedAuditor
    {
        private sealed class RouteRequirement
        {
            public RouteRequirement(string escapeId, StableResourceAmount[] costs, params string[] protectedPartIds)
            {
                EscapeId = escapeId;
                Costs = costs ?? Array.Empty<StableResourceAmount>();
                ProtectedPartIds = protectedPartIds ?? Array.Empty<string>();
            }

            public string EscapeId { get; }
            public StableResourceAmount[] Costs { get; }
            public string[] ProtectedPartIds { get; }
        }

        private static readonly int[] AuditSeeds =
        {
            PrototypeExpeditionRegionCatalog.DefaultRunSeed,
            170017,
            180018,
            220026,
            420042
        };

        private static readonly Dictionary<string, int> ExactSearchStock =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "resource.wood", 66 }, { "resource.salvage", 54 }, { "resource.food", 36 },
                { "resource.fabric", 18 }, { "resource.fiber", 30 }, { "resource.medicine", 18 },
                { "resource.stone", 54 }, { "resource.metal", 42 }, { "resource.wire", 36 },
                { "resource.fuel", 24 }, { "resource.chemicals", 18 }, { "resource.electronics", 36 }
            };

        private static readonly RouteRequirement[] Routes =
        {
            // Workbench + rope research/craft + hull/sail/supplies + one launch attempt.
            new RouteRequirement(
                PrototypeRaftEscapeConfig.EscapeId,
                Costs(("resource.wood", 6), ("resource.salvage", 5), ("resource.food", 3)),
                PrototypeRaftEscapeConfig.KeyPartId),
            // Workbench + rope research/craft + the complete two-stage smoke project.
            new RouteRequirement(
                "escape.smoke",
                Costs(("resource.wood", 15), ("resource.salvage", 3), ("resource.fiber", 2), ("resource.fuel", 2)),
                PrototypeSearchNodeLootResolver.FlintPartId),
            // Workbench + axe research/craft + the complete two-stage radio project.
            new RouteRequirement(
                "escape.radio",
                Costs(("resource.wood", 3), ("resource.stone", 2), ("resource.salvage", 2),
                    ("resource.electronics", 2), ("resource.wire", 2), ("resource.metal", 1)),
                PrototypeSearchNodeLootResolver.RadioTransceiverPartId,
                PrototypeSearchNodeLootResolver.RadioCircuitBoardPartId,
                PrototypeSearchNodeLootResolver.RadioTransistorPartId)
        };

        public static IReadOnlyList<int> RepresentativeSeeds
        {
            get { return AuditSeeds.ToArray(); }
        }

        public static Dictionary<string, int> ExpectedStableTotals()
        {
            return new Dictionary<string, int>(ExactSearchStock, StringComparer.Ordinal);
        }

        public static PrototypeEscapeResourceSeedAuditResult[] AuditRepresentativeSeeds()
        {
            return AuditSeeds.Select(Audit).ToArray();
        }

        public static PrototypeEscapeResourceSeedAuditResult Audit(int seed)
        {
            IReadOnlyList<PrototypeSearchNodeDefinition> nodes = PrototypeSearchRegionCatalog.Nodes;
            var resolvedByNode = nodes.ToDictionary(
                node => node.NodeId,
                node => PrototypeSearchNodeLootResolver.Resolve(seed, node),
                StringComparer.Ordinal);
            Dictionary<string, int> searchStock = resolvedByNode.Values.SelectMany(value => value)
                .Where(item => !item.IsProtectedPart)
                .GroupBy(item => item.StableResourceId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Sum(item => Math.Max(0, item.Amount)), StringComparer.Ordinal);
            GameSession initialSession = new GameSession(seed);
            Dictionary<string, int> startingStorage = initialSession.GetStableStorageEntries()
                .ToDictionary(entry => entry.StableResourceId, entry => entry.Amount, StringComparer.Ordinal);
            string[] catalogIds = GameSession.GetStableResourceCatalog()
                .Select(entry => entry.StableResourceId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            PrototypeSeedStableResourceAmount[] stableAmounts = catalogIds.Select(stableResourceId =>
            {
                int stock = searchStock.TryGetValue(stableResourceId, out int amount) ? amount : 0;
                int start = startingStorage.TryGetValue(stableResourceId, out int initial) ? initial : 0;
                return new PrototypeSeedStableResourceAmount
                {
                    StableResourceId = stableResourceId,
                    SearchStock = stock,
                    StartingStorage = start,
                    TotalAvailable = stock + start
                };
            }).ToArray();
            Dictionary<string, int> totalAvailable = stableAmounts.ToDictionary(
                entry => entry.StableResourceId,
                entry => entry.TotalAvailable,
                StringComparer.Ordinal);

            PrototypeProtectedPartAssignmentSnapshot[] assignments =
                PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                    seed,
                    PrototypeSearchRegionCatalog.ContractRevision);
            bool assignmentsValid = assignments.Length == PrototypeSearchNodeLootResolver.ProtectedPartIds.Count &&
                                    assignments.Select(value => value.PartId).Distinct(StringComparer.Ordinal).Count() == assignments.Length &&
                                    assignments.Select(value => value.AssignedNodeId).Distinct(StringComparer.Ordinal).Count() == assignments.Length &&
                                    assignments.All(value =>
                                        resolvedByNode.TryGetValue(value.AssignedNodeId, out PrototypeSearchLootEntry[] loot) &&
                                        PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(value.PartId).Contains(value.AssignedNodeId) &&
                                        loot.Count(item => item.IsProtectedPart &&
                                                           string.Equals(item.ProtectedPartId, value.PartId, StringComparison.Ordinal) &&
                                                           item.Amount == 1) == 1);
            string[] radioPartIds =
            {
                PrototypeSearchNodeLootResolver.RadioTransceiverPartId,
                PrototypeSearchNodeLootResolver.RadioCircuitBoardPartId,
                PrototypeSearchNodeLootResolver.RadioTransistorPartId
            };
            bool radioDistinctRegions = assignments.Where(value => radioPartIds.Contains(value.PartId))
                .Select(value => value.SourceRegionId).Distinct(StringComparer.Ordinal).Count() == radioPartIds.Length;

            PrototypeEscapeRouteSeedAudit[] routeAudits = Routes.Select(route =>
            {
                PrototypeProtectedPartAssignmentSnapshot[] routeAssignments = route.ProtectedPartIds.Select(partId =>
                    assignments.FirstOrDefault(value => string.Equals(value.PartId, partId, StringComparison.Ordinal))).ToArray();
                bool affordable = route.Costs.All(cost =>
                    totalAvailable.TryGetValue(cost.StableResourceId, out int available) && available >= cost.Amount);
                bool partsAvailable = routeAssignments.All(value => value != null) && routeAssignments.All(value =>
                    resolvedByNode[value.AssignedNodeId].Any(item => item.IsProtectedPart &&
                        string.Equals(item.ProtectedPartId, value.PartId, StringComparison.Ordinal)));
                int requiredUnits = route.Costs.Sum(cost => cost.Amount);
                int remainingUnits = totalAvailable.Values.Sum() - requiredUnits;
                return new PrototypeEscapeRouteSeedAudit
                {
                    EscapeId = route.EscapeId,
                    Seed = seed,
                    ResourceAffordable = affordable,
                    ProtectedPartsAvailable = partsAvailable,
                    NaturallyCompletable = affordable && partsAvailable,
                    RequiredGeneralUnits = requiredUnits,
                    RemainingAvailableUnits = remainingUnits,
                    RequiredResources = route.Costs.Select(cost => new StableResourceAmount(
                        cost.StableResourceId,
                        cost.LegacyKind,
                        cost.Amount)).ToArray(),
                    RequiredProtectedPartIds = route.ProtectedPartIds.ToArray(),
                    ProtectedPartNodeIds = routeAssignments.Where(value => value != null)
                        .Select(value => value.AssignedNodeId).ToArray(),
                    ProtectedPartRegionIds = routeAssignments.Where(value => value != null)
                        .Select(value => value.SourceRegionId).ToArray()
                };
            }).ToArray();

            int generalUnits = searchStock.Values.Sum();
            int protectedUnits = resolvedByNode.Values.SelectMany(value => value)
                .Where(item => item.IsProtectedPart).Sum(item => Math.Max(0, item.Amount));
            bool exactStableStock = searchStock.Count == ExactSearchStock.Count &&
                                    ExactSearchStock.All(expected =>
                                        searchStock.TryGetValue(expected.Key, out int actual) && actual == expected.Value);
            bool stableCatalogComplete = catalogIds.SequenceEqual(
                searchStock.Keys.OrderBy(value => value, StringComparer.Ordinal));
            bool atLeastOneRoute = routeAudits.Any(route => route.NaturallyCompletable);
            bool allRoutes = routeAudits.Length == Routes.Length && routeAudits.All(route => route.NaturallyCompletable);
            bool exactFiniteShape = PrototypeSearchRegionCatalog.All.Count == 7 && nodes.Count == 42 &&
                                    generalUnits == PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                                    protectedUnits == PrototypeSearchNodeLootResolver.ProtectedPartIds.Count;
            return new PrototypeEscapeResourceSeedAuditResult
            {
                Seed = seed,
                RegionCount = PrototypeSearchRegionCatalog.All.Count,
                NodeCount = nodes.Count,
                GeneralStockUnits = generalUnits,
                ProtectedPartUnits = protectedUnits,
                ExactStableStock = exactStableStock,
                StableCatalogComplete = stableCatalogComplete,
                ProtectedAssignmentsValid = assignmentsValid,
                RadioPartsUseDistinctRegions = radioDistinctRegions,
                AtLeastOneRouteCompletable = atLeastOneRoute,
                AllPlayableRoutesCompletable = allRoutes,
                NoSoftlock = exactFiniteShape && exactStableStock && stableCatalogComplete && assignmentsValid &&
                             radioDistinctRegions && atLeastOneRoute,
                StableResources = stableAmounts,
                ProtectedAssignments = assignments.Select(value => value.Clone()).ToArray(),
                Routes = routeAudits
            };
        }

        private static StableResourceAmount[] Costs(params (string stableResourceId, int amount)[] costs)
        {
            return costs.Select(cost =>
            {
                if (!GameSession.TryGetLegacyResourceKind(cost.stableResourceId, out ResourceKind legacyKind))
                {
                    throw new InvalidOperationException("Unknown route audit resource: " + cost.stableResourceId);
                }
                return new StableResourceAmount(cost.stableResourceId, legacyKind, cost.amount);
            }).ToArray();
        }
    }

    public static class PrototypeSearchNodeRuntimeContract
    {
        public static PrototypeSearchNodeContractResult Verify()
        {
            int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            IReadOnlyList<PrototypeSearchRegionDefinition> regions = PrototypeSearchRegionCatalog.All;
            IReadOnlyList<PrototypeSearchNodeArchetypeDefinition> archetypes = PrototypeSearchRegionCatalog.Archetypes;
            IReadOnlyList<PrototypeSearchNodeDefinition> definitions = PrototypeSearchRegionCatalog.Nodes;
            bool sevenRegions = regions.Count == 7 &&
                                regions.Select(region => region.StableId).Distinct(StringComparer.Ordinal).Count() == 7;
            bool exactShape = regions.All(region => region.Archetypes.Count == 3 && region.Nodes.Count == 6) &&
                              archetypes.Count == 21 && archetypes.All(archetype => archetype.Instances.Count == 2) &&
                              definitions.Count == 42;
            string[] stableIds = regions.Select(region => region.StableId)
                .Concat(archetypes.Select(archetype => archetype.StableId))
                .Concat(definitions.Select(node => node.NodeId)).ToArray();
            int duplicateStableIds = stableIds.Length - stableIds.Distinct(StringComparer.Ordinal).Count();
            bool stableNodes = duplicateStableIds == 0 && definitions.All(node =>
                !string.IsNullOrWhiteSpace(node.RegionId) && !string.IsNullOrWhiteSpace(node.ArchetypeId) &&
                !string.IsNullOrWhiteSpace(node.NodeId) && (node.InstanceOrdinal == 1 || node.InstanceOrdinal == 2));
            HashSet<string> actualNodeIds = new HashSet<string>(definitions.Select(node => node.NodeId), StringComparer.Ordinal);
            bool legacyIdsPreserved = PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.Count == 28 &&
                                      PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.All(actualNodeIds.Contains) &&
                                      definitions.Count(node => string.Equals(node.Origin, "existing", StringComparison.Ordinal)) == 28;
            bool exactlyFourteenAdded = PrototypeSearchRegionCatalog.NewWaveBNodeIds.Count == 14 &&
                                        PrototypeSearchRegionCatalog.NewWaveBNodeIds.All(actualNodeIds.Contains) &&
                                        definitions.Count(node => string.Equals(node.Origin, "new", StringComparison.Ordinal)) == 14;
            Dictionary<string, int> stableResourceTotals = definitions
                .SelectMany(node => PrototypeSearchNodeLootResolver.Resolve(seed, node))
                .Where(item => !item.IsProtectedPart)
                .GroupBy(item => item.StableResourceId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount), StringComparer.Ordinal);
            Dictionary<string, int> expectedStableResourceTotals =
                PrototypeEscapeResourceSeedAuditor.ExpectedStableTotals();
            bool stableResourceTotalsMatch = stableResourceTotals.Count == expectedStableResourceTotals.Count &&
                                             expectedStableResourceTotals.All(expected =>
                                                 stableResourceTotals.TryGetValue(expected.Key, out int actual) && actual == expected.Value);
            int generalUnits = PrototypeSearchRegionCatalog.GeneralStockUnitsForSeed(seed);
            int protectedUnits = definitions.Sum(node => PrototypeSearchNodeLootResolver.Resolve(seed, node)
                .Where(item => item.IsProtectedPart).Sum(item => Math.Max(0, item.Amount)));
            PrototypeProtectedPartAssignmentSnapshot[] protectedAssignments =
                PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                    seed,
                    PrototypeSearchRegionCatalog.ContractRevision);
            string[] expectedProtectedPartIds = PrototypeSearchNodeLootResolver.ProtectedPartIds.ToArray();
            string[] radioPartIds =
            {
                PrototypeSearchNodeLootResolver.RadioTransceiverPartId,
                PrototypeSearchNodeLootResolver.RadioCircuitBoardPartId,
                PrototypeSearchNodeLootResolver.RadioTransistorPartId
            };
            bool protectedAssignmentContract = protectedAssignments.Length == 5 &&
                protectedAssignments.Select(value => value.PartId).Distinct(StringComparer.Ordinal).Count() == 5 &&
                expectedProtectedPartIds.All(partId => protectedAssignments.Any(value =>
                    string.Equals(value.PartId, partId, StringComparison.Ordinal))) &&
                protectedAssignments.Select(value => value.AssignedNodeId).Distinct(StringComparer.Ordinal).Count() == 5 &&
                protectedAssignments.All(value => PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(value.PartId)
                    .Contains(value.AssignedNodeId)) &&
                protectedAssignments.Where(value => radioPartIds.Contains(value.PartId))
                    .Select(value => value.SourceRegionId).Distinct(StringComparer.Ordinal).Count() == 3;
            bool exactFiniteBalance = generalUnits == PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                                      PrototypeSearchRegionCatalog.GeneralStockUnitsForSeed(seed + 1) ==
                                      PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                                      protectedUnits == 5 && stableResourceTotalsMatch && protectedAssignmentContract;
            string firstCatalogFingerprint = CatalogFingerprint(seed);
            bool deterministic = string.Equals(firstCatalogFingerprint, CatalogFingerprint(seed), StringComparison.Ordinal);
            bool differentSeedVaries = Enumerable.Range(seed + 1, 5)
                .Any(otherSeed => !string.Equals(firstCatalogFingerprint, CatalogFingerprint(otherSeed), StringComparison.Ordinal));

            var protectedNodeIds = new HashSet<string>(
                protectedAssignments.Select(value => value.AssignedNodeId),
                StringComparer.Ordinal);
            PrototypeSearchNodeDefinition definition = definitions.First(node =>
                !protectedNodeIds.Contains(node.NodeId) &&
                !string.Equals(node.HazardId, PrototypeDiseaseRuntime.TriggerHazardId, StringComparison.Ordinal));
            PrototypeSearchLootEntry[] first = PrototypeSearchNodeLootResolver.Resolve(seed, definition);
            GameSession session = new GameSession(seed);
            bool began = session.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId));
            if (definition.RequiresSwimming) session.SetSwimming(true);
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

            PrototypeSearchNodeSnapshot beforeTake = runtime.ActiveNode;
            int normalIndex = beforeTake == null ? -1 : Array.FindIndex(beforeTake.Remaining, item => !item.IsProtectedPart);
            bool selected = normalIndex >= 0 && runtime.SetFocusedIndex(normalIndex);
            int initialGeneral = runtime.Ledger.GeneralRemainingAmount;
            PrototypeSearchTakeResult takeResult = selected
                ? runtime.TryTakeFocused(session, delegate { return false; })
                : PrototypeSearchTakeResult.Rejected;
            int afterTakeGeneral = runtime.Ledger.GeneralRemainingAmount;
            runtime.Close(session);
            runtime.Ledger.MarkBarrierBroken(definition.RegionId);
            runtime.Ledger.MarkPermanentHazardRemoved(definition.RegionId, definition.HazardId);
            string json = JsonUtility.ToJson(runtime.Ledger.CaptureSnapshot());
            PrototypeSearchNodeLedger restored = PrototypeSearchNodeLedger.CreateForRestore(seed);
            bool restoreShellStartsEmpty = restored.StockGenerationEvents.Count == 0 &&
                                           string.IsNullOrEmpty(restored.NewGameStockFingerprint);
            bool restoredOk = restored.RestoreSnapshot(JsonUtility.FromJson<PrototypeSearchRunSnapshot>(json));
            bool productionRestoreOk = PrototypeSearchNodeRuntime.TryCreateFromSnapshot(
                JsonUtility.FromJson<PrototypeSearchRunSnapshot>(json),
                out PrototypeSearchNodeRuntime restoredRuntime) &&
                restoredRuntime.Ledger.StockGenerationEvents.SequenceEqual(runtime.Ledger.StockGenerationEvents) &&
                string.Equals(
                    restoredRuntime.Ledger.NewGameStockFingerprint,
                    runtime.Ledger.NewGameStockFingerprint,
                    StringComparison.Ordinal);
            PrototypeSearchNodeSnapshot restoredNode = restored.GetOrCreate(definition);
            bool persistence = restoredOk && restored.CaptureSnapshot().Nodes.Length == 42 &&
                               restored.CaptureSnapshot().Regions.Length == 7 &&
                               restoredNode.State == PrototypeSearchNodeState.RevealedPartial &&
                               restoredNode.SearchCount == 1 && restored.GeneralRemainingAmount == afterTakeGeneral &&
                               afterTakeGeneral < initialGeneral &&
                               restored.IsBarrierBroken(definition.RegionId) &&
                               restored.IsPermanentHazardRemoved(definition.RegionId, definition.HazardId);
            bool stockDoesNotRegenerate = restored.GetOrCreate(definition).GeneralRemainingAmount ==
                                          restoredNode.GeneralRemainingAmount &&
                                          restored.GeneralRemainingAmount == afterTakeGeneral;

            PrototypeSearchNodeDefinition depleteDefinition = definitions.First(node =>
                !string.Equals(node.NodeId, definition.NodeId, StringComparison.Ordinal) &&
                !protectedNodeIds.Contains(node.NodeId) &&
                node.FiniteYield.Count == 1);
            GameSession depleteSession = new GameSession(seed);
            GameSessionStableState depleteBagState = depleteSession.CaptureStableState();
            depleteBagState.ActiveBagSlotCount = GameSession.MaximumBagSlotCount;
            bool depleteBagPrepared = depleteSession.RestoreStableState(depleteBagState);
            bool depleteBegan = depleteSession.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(depleteDefinition.RegionId));
            if (depleteDefinition.RequiresSwimming) depleteSession.SetSwimming(true);
            PrototypeSearchNodeRuntime depleteRuntime = new PrototypeSearchNodeRuntime(seed);
            bool hiddenObserved = depleteRuntime.Ledger.GetOrCreate(depleteDefinition).State == PrototypeSearchNodeState.Hidden;
            bool depleteOpened = depleteRuntime.TryOpen(depleteDefinition, depleteSession) == PrototypeSearchOpenResult.Opened;
            bool partialObserved = depleteRuntime.ActiveNode != null &&
                                   depleteRuntime.ActiveNode.State == PrototypeSearchNodeState.RevealedPartial;
            PrototypeSearchTakeResult depleteResult = depleteRuntime.TryTakeAll(depleteSession, delegate { return false; });
            bool depletedObserved = depleteRuntime.ActiveNode != null &&
                                    depleteRuntime.ActiveNode.State == PrototypeSearchNodeState.Depleted &&
                                    depleteRuntime.ActiveNode.RemainingAmount == 0;

            PrototypeSearchNodeDefinition sailclothDefinition = definitions.First(node =>
                string.Equals(node.NodeId, PrototypeSearchNodeLootResolver.ResolveSailclothNodeId(seed), StringComparison.Ordinal));
            GameSession protectedSession = new GameSession(seed);
            bool protectedBegan = protectedSession.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(sailclothDefinition.RegionId));
            if (sailclothDefinition.RequiresSwimming) protectedSession.SetSwimming(true);
            PrototypeSearchNodeRuntime protectedRuntime = new PrototypeSearchNodeRuntime(seed);
            bool protectedOpened = protectedRuntime.TryOpen(sailclothDefinition, protectedSession) == PrototypeSearchOpenResult.Opened;
            PrototypeSearchNodeSnapshot protectedNode = protectedRuntime.ActiveNode;
            int protectedIndex = protectedNode == null ? -1 : Array.FindIndex(protectedNode.Remaining, item => item.IsProtectedPart);
            int protectedTransfers = 0;
            bool protectedTaken = protectedIndex >= 0 && protectedRuntime.SetFocusedIndex(protectedIndex) &&
                                  protectedRuntime.TryTakeFocused(protectedSession, delegate { protectedTransfers += 1; return true; }) == PrototypeSearchTakeResult.Protected;
            bool protectedUnique = protectedRuntime.Ledger.HasProtectedPart(PrototypeRaftEscapeConfig.KeyPartId) &&
                                   protectedTransfers == 1 && protectedRuntime.ActiveNode.Remaining.All(item => !item.IsProtectedPart) &&
                                   protectedRuntime.TryTakeFocused(protectedSession, delegate { protectedTransfers += 1; return true; }) != PrototypeSearchTakeResult.Protected &&
                                   protectedTransfers == 1;

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
            int pendingAmount = nonWoodIndex < 0 ? 0 : swapNode.Remaining[nonWoodIndex].Amount;
            BagStack slotBefore = fullBagSession.GetBagSlot(0);
            bool pending = nonWoodIndex >= 0 && swapRuntime.SetFocusedIndex(nonWoodIndex) &&
                           swapRuntime.TryTakeFocused(fullBagSession, delegate { return false; }) == PrototypeSearchTakeResult.PendingSwap;
            bool cancelAtomic = pending && swapRuntime.CancelPending(fullBagSession) &&
                                !fullBagSession.HasPendingLoot && swapRuntime.ActiveNode.RemainingAmount == nodeBefore &&
                                fullBagSession.GetBagSlot(0).Kind == slotBefore.Kind && fullBagSession.GetBagSlot(0).Amount == slotBefore.Amount;
            bool pendingAgain = swapRuntime.TryTakeFocused(fullBagSession, delegate { return false; }) == PrototypeSearchTakeResult.PendingSwap;
            bool replaceAtomic = pendingAgain && swapRuntime.TryReplacePending(fullBagSession, 0) &&
                                 !fullBagSession.HasPendingLoot && swapRuntime.ActiveNode.RemainingAmount ==
                                 nodeBefore - pendingAmount + slotBefore.Amount;

            PrototypeSearchNodeContractResult pityContract = VerifyProtectedPartPityNaturalResultContract(seed);
            PrototypeSearchNodeContractResult diseaseContract = VerifyNaturalDiseaseLifecycle(seed);
            bool passed = sevenRegions && exactShape && stableNodes && legacyIdsPreserved && exactlyFourteenAdded &&
                          exactFiniteBalance && deterministic &&
                          differentSeedVaries && began && costOnce && selected &&
                          (takeResult == PrototypeSearchTakeResult.Added || takeResult == PrototypeSearchTakeResult.Depleted) &&
                           persistence && restoreShellStartsEmpty && productionRestoreOk && stockDoesNotRegenerate &&
                           depleteBegan && depleteOpened && hiddenObserved &&
                          depleteBagPrepared && partialObserved && depletedObserved && depleteResult == PrototypeSearchTakeResult.Depleted &&
                          protectedBegan && protectedOpened && protectedTaken && protectedUnique &&
                          fullBagBegan && filled && swapOpened && cancelAtomic && replaceAtomic &&
                          pityContract.Passed && diseaseContract.Passed;
            return new PrototypeSearchNodeContractResult(
                passed,
                "catalog=" + PrototypeSearchRegionCatalog.CatalogRevision +
                " balance=" + PrototypeSearchRegionCatalog.BalanceStatus +
                " regions=" + regions.Count + " archetypes=" + archetypes.Count + " instances=" + definitions.Count +
                " generalUnits=" + generalUnits + " protectedUnits=" + protectedUnits +
                " duplicateStableIds=" + duplicateStableIds +
                " existing=28 new=14 removedLegacy=" +
                PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.Count(id => !actualNodeIds.Contains(id)) +
                " stableResources=12" +
                " protectedAssignments=" + protectedAssignments.Length +
                " radioRegions=" + protectedAssignments.Where(value => radioPartIds.Contains(value.PartId))
                    .Select(value => value.SourceRegionId).Distinct(StringComparer.Ordinal).Count() +
                " initialNodeCollisions=" + (protectedAssignments.Length - protectedAssignments
                    .Select(value => value.AssignedNodeId).Distinct(StringComparer.Ordinal).Count()) +
                " deterministic=" + deterministic + " differentSeedVaries=" + differentSeedVaries +
                " restoreShellGenerationEvents=0 productionRestore=" + productionRestoreOk +
                " stockDoesNotRegenerate=" + stockDoesNotRegenerate +
                " hidden-partial-depleted=" + (hiddenObserved && partialObserved && depletedObserved) +
                " barrierPersistent=" + persistence + " permanentHazardPersistent=" + persistence +
                " core=" + sevenRegions + "/" + exactShape + "/" + stableNodes + "/" +
                legacyIdsPreserved + "/" + exactlyFourteenAdded + "/" + exactFiniteBalance +
                " traversal=" + began + "/" + selected + "/" + takeResult +
                " deplete=" + depleteBegan + "/" + depleteOpened + "/" + depleteResult +
                " protected=" + protectedBegan + "/" + protectedOpened + "/" + protectedTaken +
                " swap=" + fullBagBegan + "/" + filled + "/" + swapOpened +
                " costOnce=" + costOnce + " selectionHazardPaused=true cancelAtomic=" + cancelAtomic +
                " replaceAtomic=" + replaceAtomic + " sailclothProtectedUnique=" + protectedUnique +
                " snapshotRestore=" + restoredOk +
                " pityPassed=" + pityContract.Passed + " diseasePassed=" + diseaseContract.Passed +
                " | " + pityContract.Detail + " | " + diseaseContract.Detail);
        }

        public static PrototypeSearchNodeContractResult VerifyProtectedPartPityNaturalResultContract(int seed)
        {
            const string partId = PrototypeSearchNodeLootResolver.FlintPartId;
            PrototypeSearchNodeLedger ledger = new PrototypeSearchNodeLedger(seed);
            PrototypeProtectedPartAssignmentSnapshot assignment = ledger.ProtectedPartAssignments.First(value =>
                string.Equals(value.PartId, partId, StringComparison.Ordinal));
            string[] misses = PrototypeSearchNodeLootResolver.EligibleNodeIdsFor(partId)
                .Where(nodeId => !string.Equals(nodeId, assignment.AssignedNodeId, StringComparison.Ordinal))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            int generalBefore = ledger.GeneralRemainingAmount;
            bool fiveUniqueMisses = misses.Length == CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount;
            for (int index = 0; index < misses.Length && fiveUniqueMisses; index += 1)
            {
                fiveUniqueMisses = ledger.TryRecordEligibleProtectedPartNodeResult(
                    misses[index],
                    partId,
                    true,
                    out PrototypeProtectedPartPitySnapshot result) &&
                    result.EligibleMissCount == index + 1 &&
                    result.HintRevealed == (index + 1 >= CampaignKeyPartPityConfig.EligibleHintSearchCount) &&
                    result.GuaranteeArmed == (index + 1 >= CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount) &&
                    !result.Acquired;
            }

            PrototypeProtectedPartPitySnapshot armed = ledger.ProtectedPartPity.First(value =>
                string.Equals(value.PartId, partId, StringComparison.Ordinal));
            string armedJson = JsonUtility.ToJson(ledger.CaptureSnapshot());
            bool armedRestored = PrototypeSearchNodeRuntime.TryCreateFromSnapshot(
                JsonUtility.FromJson<PrototypeSearchRunSnapshot>(armedJson),
                out PrototypeSearchNodeRuntime restoredRuntime);
            PrototypeSearchNodeLedger restored = armedRestored ? restoredRuntime.Ledger : null;
            bool guaranteedOnNext = restored != null && restored.TryRecordEligibleProtectedPartNodeResult(
                assignment.AssignedNodeId,
                partId,
                true,
                out PrototypeProtectedPartPitySnapshot acquired) &&
                acquired.Acquired && acquired.GuaranteeArmed &&
                acquired.EligibleMissCount == CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount &&
                string.Equals(acquired.SourceNodeId, assignment.AssignedNodeId, StringComparison.Ordinal) &&
                string.Equals(acquired.RepairState, "pity-guaranteed-next-eligible-node", StringComparison.Ordinal);
            string beforeDuplicate = restored == null ? string.Empty : JsonUtility.ToJson(restored.CaptureSnapshot());
            bool duplicateRejected = restored != null && !restored.TryRecordEligibleProtectedPartNodeResult(
                assignment.AssignedNodeId,
                partId,
                true,
                out PrototypeProtectedPartPitySnapshot ignored);
            string afterDuplicate = restored == null ? string.Empty : JsonUtility.ToJson(restored.CaptureSnapshot());
            int protectedConserved = restored == null
                ? -1
                : restored.ProtectedRemainingAmount + restored.CaptureSnapshot().ProtectedPartIds.Length;
            bool acquiredRestored = restored != null && PrototypeSearchNodeRuntime.TryCreateFromSnapshot(
                JsonUtility.FromJson<PrototypeSearchRunSnapshot>(afterDuplicate),
                out PrototypeSearchNodeRuntime acquiredRestoredRuntime) &&
                acquiredRestoredRuntime.Ledger.HasProtectedPart(partId) &&
                acquiredRestoredRuntime.Ledger.ProtectedPartPity.First(value =>
                    string.Equals(value.PartId, partId, StringComparison.Ordinal)).Acquired;
            bool stockSeparated = restored != null &&
                                  generalBefore == PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                                  restored.GeneralRemainingAmount == PrototypeSearchRegionCatalog.BalanceProvisionalGeneralStockUnits &&
                                  protectedConserved == 5 && restored.ProtectedPartPity.Count == 5;
            bool duplicateZeroDelta = duplicateRejected && string.Equals(beforeDuplicate, afterDuplicate, StringComparison.Ordinal);
            bool passed = fiveUniqueMisses && armed.EligibleMissCount == 5 && armed.GuaranteeArmed && !armed.Acquired &&
                          armedRestored && guaranteedOnNext && acquiredRestored && stockSeparated && duplicateZeroDelta;
            return new PrototypeSearchNodeContractResult(
                passed,
                "pityParts=5 eligibleMisses=" + armed.EligibleMissCount +
                " hint3=" + armed.HintRevealed + " arm5=" + armed.GuaranteeArmed +
                " nextEligibleGuaranteed=" + guaranteedOnNext + " generalUnits=" +
                (restored == null ? -1 : restored.GeneralRemainingAmount) +
                " protectedConserved=" + protectedConserved + " restore=" + armedRestored +
                " acquiredRestore=" + acquiredRestored + " duplicateZeroDelta=" + duplicateZeroDelta +
                " fiveUniqueMisses=" + fiveUniqueMisses + " armedUnacquired=" + (!armed.Acquired) +
                " stockSeparated=" + stockSeparated);
        }

        public static PrototypeSearchNodeContractResult VerifyNaturalDiseaseLifecycle(int seed)
        {
            PrototypeSearchNodeDefinition[] diseaseNodes = PrototypeSearchRegionCatalog.Nodes.Where(node =>
                string.Equals(node.RegionId, "region.forest.grove", StringComparison.Ordinal) &&
                string.Equals(node.HazardId, PrototypeDiseaseRuntime.TriggerHazardId, StringComparison.Ordinal))
                .OrderBy(node => node.NodeId, StringComparer.Ordinal).ToArray();
            PrototypeSearchNodeDefinition medicineNode = PrototypeSearchRegionCatalog.Nodes.First(node =>
                string.Equals(node.RegionId, "region.forest.grove", StringComparison.Ordinal) &&
                node.FiniteYield.Any(item => string.Equals(
                    item.StableResourceId, PrototypeDiseaseRuntime.MedicineResourceId, StringComparison.Ordinal)));

            GameSession forcedSession = new GameSession(seed);
            PrototypeSearchNodeRuntime forcedRuntime = new PrototypeSearchNodeRuntime(seed);
            bool forcedBegan = forcedSession.BeginSearch(PrototypeExpeditionRegionId.Forest);
            bool forcedOpened = diseaseNodes.All(node =>
            {
                bool opened = forcedRuntime.TryOpen(node, forcedSession) == PrototypeSearchOpenResult.Opened;
                forcedRuntime.Close(forcedSession);
                return opened;
            });
            bool forcedReturned = forcedSession.ReturnToCamp(true);
            float forcedEnergy = forcedSession.Energy;
            int forcedHealthBefore = forcedSession.Health;
            bool forcedEffect = forcedRuntime.NotifyReturnToCamp(forcedSession, true);
            bool duplicateEffectRejected = !forcedRuntime.NotifyReturnToCamp(forcedSession, true) &&
                                           forcedSession.Energy == forcedEnergy &&
                                           forcedSession.Health == forcedHealthBefore + PrototypeDiseaseRuntime.SymptomHealthDelta &&
                                           forcedRuntime.Disease.EffectCount == 1 &&
                                           forcedRuntime.Disease.ForcedReturnCount == 1;

            GameSession treatmentSession = new GameSession(seed);
            PrototypeSearchNodeRuntime treatmentRuntime = new PrototypeSearchNodeRuntime(seed);
            bool began = treatmentSession.BeginSearch(PrototypeExpeditionRegionId.Forest);
            bool medicineCollected = CollectNaturalMedicine(treatmentSession, treatmentRuntime, medicineNode);
            bool telegraph = treatmentRuntime.TryTelegraphDisease(diseaseNodes[0]);
            bool exposureNodesOpened = diseaseNodes.All(node =>
            {
                bool opened = treatmentRuntime.TryOpen(node, treatmentSession) == PrototypeSearchOpenResult.Opened;
                treatmentRuntime.Close(treatmentSession);
                return opened;
            });
            bool exposed = exposureNodesOpened && treatmentRuntime.Disease.Phase == PrototypeDiseasePhase.Exposed &&
                           treatmentRuntime.Disease.ExposureCount == PrototypeDiseaseRuntime.RequiredUniqueExposureCount &&
                           treatmentRuntime.Disease.EffectCount == 0;
            bool returned = treatmentSession.ReturnToCamp(false);
            int symptomHealthBefore = treatmentSession.Health;
            int environmentalRecoveryDelta = treatmentRuntime.EnvironmentalHazards.Exposures
                .Where(value => value.Phase == PrototypeSearchEnvironmentalHazardPhase.Mitigated)
                .Sum(value => string.Equals(
                    value.HazardId,
                    PrototypeSearchEnvironmentalHazardRuntime.InsectsHazardId,
                    StringComparison.Ordinal)
                    ? PrototypeSearchEnvironmentalHazardRuntime.InsectsRecoveryHealthDelta
                    : PrototypeSearchEnvironmentalHazardRuntime.DangerousPlantsRecoveryHealthDelta);
            bool effect = treatmentRuntime.NotifyReturnToCamp(treatmentSession, false) &&
                          treatmentRuntime.Disease.Phase == PrototypeDiseasePhase.Symptomatic &&
                          treatmentRuntime.Disease.EffectCount == 1 &&
                          treatmentSession.Health == symptomHealthBefore + PrototypeDiseaseRuntime.SymptomHealthDelta +
                                                     environmentalRecoveryDelta;
            int worsenHealthBefore = treatmentSession.Health;
            bool worsened = treatmentSession.EndDay() && treatmentRuntime.NotifyDaySettlement(treatmentSession) &&
                            treatmentRuntime.Disease.Phase == PrototypeDiseasePhase.Aggravated &&
                            treatmentRuntime.Disease.WorsenCount == 1 &&
                            treatmentSession.Health == worsenHealthBefore + PrototypeDiseaseRuntime.AggravationHealthDelta;
            PrototypeDiseaseSnapshot cancelBefore = treatmentRuntime.Disease.CaptureSnapshot();
            int cancelMedicineBefore = treatmentSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId);
            bool cancelAccepted = treatmentRuntime.Disease.TryCancelTreatment();
            PrototypeDiseaseSnapshot cancelAfter = treatmentRuntime.Disease.CaptureSnapshot();
            bool cancelAtomic = cancelAccepted && cancelBefore.Phase == cancelAfter.Phase &&
                                cancelBefore.Severity == cancelAfter.Severity &&
                                treatmentSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId) == cancelMedicineBefore &&
                                cancelBefore.TreatmentPaidCount == cancelAfter.TreatmentPaidCount;
            int medicineBefore = treatmentSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId);
            bool treated = treatmentRuntime.TryTreatDisease(treatmentSession, true);
            bool duplicateTreatRejected = !treatmentRuntime.TryTreatDisease(treatmentSession, true);
            bool treatmentAtomic = treated && duplicateTreatRejected &&
                                   treatmentRuntime.Disease.Phase == PrototypeDiseasePhase.Recovering &&
                                   treatmentRuntime.Disease.TreatmentPaidCount == 1 &&
                                   treatmentSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId) ==
                                   medicineBefore - PrototypeDiseaseRuntime.TreatmentMedicineCost;
            string[] requiredTraceTokens =
            {
                "disease.telegraph", "disease.exposure", "disease.effect",
                "disease.worsen", "disease.mitigate", "disease.treat"
            };
            string joinedTrace = string.Join("|", treatmentRuntime.Disease.Trace.ToArray());
            bool orderedTrace = requiredTraceTokens.All(token => joinedTrace.Contains(token)) &&
                                requiredTraceTokens.Select(token => joinedTrace.IndexOf(token, StringComparison.Ordinal))
                                    .SequenceEqual(requiredTraceTokens.Select(token => joinedTrace.IndexOf(token, StringComparison.Ordinal))
                                        .OrderBy(index => index));

            bool passed = diseaseNodes.Length == 2 && forcedBegan && forcedOpened && forcedReturned && forcedEffect &&
                          duplicateEffectRejected && began && medicineCollected && telegraph && exposed && returned &&
                          effect && worsened &&
                          cancelAtomic && treatmentAtomic && orderedTrace;
            return new PrototypeSearchNodeContractResult(
                passed,
                "disease=" + PrototypeDiseaseRuntime.StableId +
                " trace=telegraph>exposure>effect>worsen>mitigate>treat" +
                " forcedReturnAtomic=" + (forcedEffect && duplicateEffectRejected) +
                " cancelAtomic=" + cancelAtomic + " treatmentCostAtomic=" + treatmentAtomic +
                " setup=" + (diseaseNodes.Length == 2) + "/" + forcedBegan + "/" + forcedOpened + "/" + forcedReturned +
                "/" + began + "/" + medicineCollected + "/" + telegraph + "/" + exposed + "/" + returned +
                "/" + effect + "/" + worsened + "/" + orderedTrace +
                " environmentalRecoveryDelta=" + environmentalRecoveryDelta +
                " grant=false warp=false skip=false fixtureOnly=false");
        }

        private static bool CollectNaturalMedicine(
            GameSession session,
            PrototypeSearchNodeRuntime runtime,
            PrototypeSearchNodeDefinition medicineNode)
        {
            if (runtime.TryOpen(medicineNode, session) != PrototypeSearchOpenResult.Opened) return false;
            PrototypeSearchNodeSnapshot snapshot = runtime.ActiveNode;
            int medicineIndex = snapshot == null ? -1 : Array.FindIndex(snapshot.Remaining, item =>
                string.Equals(item.StableResourceId, PrototypeDiseaseRuntime.MedicineResourceId, StringComparison.Ordinal));
            bool acquired = medicineIndex >= 0 && runtime.SetFocusedIndex(medicineIndex);
            if (acquired)
            {
                PrototypeSearchTakeResult result = runtime.TryTakeFocused(session, delegate { return false; });
                acquired = result == PrototypeSearchTakeResult.Added || result == PrototypeSearchTakeResult.Depleted;
            }
            runtime.Close(session);
            int medicineInBag = 0;
            for (int index = 0; index < session.ActiveBagSlotCount; index += 1)
            {
                BagStack stack = session.GetBagSlot(index);
                if (!stack.IsEmpty && string.Equals(
                        stack.StableResourceId, PrototypeDiseaseRuntime.MedicineResourceId, StringComparison.Ordinal))
                {
                    medicineInBag += stack.Amount;
                }
            }
            return acquired && medicineInBag >= PrototypeDiseaseRuntime.TreatmentMedicineCost;
        }

        private static string CatalogFingerprint(int seed)
        {
            return string.Join("|", PrototypeSearchRegionCatalog.Nodes
                .OrderBy(node => node.NodeId, StringComparer.Ordinal)
                .Select(node => node.NodeId + "=" + string.Join(",", PrototypeSearchNodeLootResolver.Resolve(seed, node)
                    .Select(item => item.StableItemId + ":" + item.Resource + ":" + item.Amount + ":" + item.ProtectedPartId)
                    .ToArray()))
                .ToArray());
        }
    }
}
