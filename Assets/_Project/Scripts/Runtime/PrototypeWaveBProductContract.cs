using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    public static class PrototypeWaveBProductContract
    {
        public const int ExpectedRegionCount = 7;
        public const int ExpectedArchetypeCount = 21;
        public const int ExpectedNodeCount = 42;
        public const int ExpectedFiniteGeneralResourceUnits = 144;

        private static readonly string[] LegacyNodeIds =
        {
            "node.coast.beach.drift-pile.01",
            "node.coast.beach.grass-patch.01",
            "node.coast.beach.rock-crevice.01",
            "node.coast.beach.tree-hollow.01",
            "node.sea.shallows.drift-pile.01",
            "node.sea.shallows.rock-crevice.01",
            "node.sea.shallows.grass-patch.01",
            "node.sea.shallows.wreck-locker.01",
            "node.forest.grove.tree-hollow.01",
            "node.forest.grove.grass-patch.01",
            "node.forest.grove.rock-crevice.01",
            "node.forest.grove.drift-pile.01",
            "node.ridge.highland.rock-crevice.01",
            "node.ridge.highland.grass-patch.01",
            "node.ridge.highland.tree-hollow.01",
            "node.ridge.highland.facility-cabinet.01",
            "node.cave.island.rock-crevice.01",
            "node.cave.island.drift-pile.01",
            "node.cave.island.tree-hollow.01",
            "node.cave.island.facility-cabinet.01",
            "node.cove.wreck.wreck-locker.01",
            "node.cove.wreck.drift-pile.01",
            "node.cove.wreck.rock-crevice.01",
            "node.cove.wreck.grass-patch.01",
            "node.ruins.relay.facility-cabinet.01",
            "node.ruins.relay.facility-cabinet.02",
            "node.ruins.relay.rock-crevice.01",
            "node.ruins.relay.grass-patch.01"
        };

        public static PrototypeContractProbe Verify()
        {
            IReadOnlyList<PrototypeSearchRegionDefinition> regions = PrototypeSearchRegionCatalog.All;
            IReadOnlyList<PrototypeSearchArchetypeDefinition> catalogArchetypes = PrototypeSearchRegionCatalog.Archetypes;
            PrototypeSearchNodeDefinition[] nodes = regions.SelectMany(region => region.Nodes).ToArray();
            int archetypes = catalogArchetypes.Count;
            int finiteCatalogUnits = catalogArchetypes.Sum(archetype => archetype.FiniteGeneralUnits);
            int finiteUnits = nodes.Sum(node => PrototypeSearchNodeLootResolver
                .Resolve(PrototypeExpeditionRegionCatalog.DefaultRunSeed, node)
                .Where(item => !item.IsProtectedPart)
                .Sum(item => Math.Max(0, item.Amount)));
            bool stableIds = nodes.Select(node => node.NodeId).Distinct(StringComparer.Ordinal).Count() == nodes.Length;
            HashSet<string> nodeIds = new HashSet<string>(nodes.Select(node => node.NodeId), StringComparer.Ordinal);
            int existingNodes = LegacyNodeIds.Count(nodeIds.Contains);
            int newNodes = nodes.Length - existingNodes;
            int removedLegacy = LegacyNodeIds.Count(nodeId => !nodeIds.Contains(nodeId));
            bool legacyCatalogPreserved = existingNodes == 28 && newNodes == 14 && removedLegacy == 0;
            bool twoInstances = nodes.GroupBy(node => node.ArchetypeId, StringComparer.Ordinal).All(group => group.Count() == 2);
            bool publicArchetypeInstances = catalogArchetypes.All(archetype => archetype.Instances.Count == 2) &&
                                            catalogArchetypes.Select(archetype => archetype.StableId)
                                                .Distinct(StringComparer.Ordinal).Count() == catalogArchetypes.Count;
            bool sixPerRegion = regions.All(region => region.Nodes.Count == 6);

            PrototypeSearchRegionDefinition forest = regions.Single(region => region.StableId == "region.forest.grove");
            PrototypeSearchNodeDefinition treeHollow = forest.Nodes.Single(node => node.NodeId == "node.forest.grove.tree-hollow.01");
            PrototypeSearchNodeDefinition driftPile = forest.Nodes.Single(node => node.NodeId == "node.forest.grove.drift-pile.01");
            bool diseaseSources = treeHollow.HazardId == PrototypeDiseaseConfig.ExposureHazardId &&
                                  driftPile.HazardId == PrototypeDiseaseConfig.ExposureHazardId;
            string[] medicineNodeIds =
            {
                "node.forest.grove.grass-patch.01",
                "node.forest.grove.grass-patch.02"
            };
            bool medicineSources = medicineNodeIds.All(nodeId =>
                PrototypeSearchNodeLootResolver.Resolve(
                        PrototypeExpeditionRegionCatalog.DefaultRunSeed,
                        forest.Nodes.Single(node => node.NodeId == nodeId))
                    .Any(item => item.ResourceId == PrototypeDiseaseConfig.TreatmentResourceId && item.Amount > 0));

            int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            string first = Fingerprint(nodes, seed);
            string repeat = Fingerprint(nodes, seed);
            bool deterministic = string.Equals(first, repeat, StringComparison.Ordinal);
            bool seedVariation = Enumerable.Range(seed + 1, 8)
                .Select(otherSeed => Fingerprint(nodes, otherSeed))
                .Any(value => !string.Equals(first, value, StringComparison.Ordinal));

            bool diseaseRuntimePresent = Type.GetType("KimSurvival.PrototypeDiseaseRuntime, Assembly-CSharp") != null;
            PrototypeContractProbe diseaseTrace = diseaseRuntimePresent
                ? PrototypeDiseaseRuntimeContract.VerifyNaturalAtomicTrace()
                : new PrototypeContractProbe(false, "disease runtime missing");
            PrototypeSearchNodeContractResult searchRegression = PrototypeSearchNodeRuntimeContract.Verify();
            PrototypeDiseaseActions keyboardDiseaseAction = PrototypeDiseaseActions.FromRaw(
                new PrototypeRawDiseaseInput { KeyboardTreat = true, KeyboardCancel = true });
            PrototypeDiseaseActions gamepadDiseaseAction = PrototypeDiseaseActions.FromRaw(
                new PrototypeRawDiseaseInput { GamepadTreat = true, GamepadCancel = true });
            bool inputParity = keyboardDiseaseAction.TreatPressed == gamepadDiseaseAction.TreatPressed &&
                               keyboardDiseaseAction.CancelPressed == gamepadDiseaseAction.CancelPressed;
            bool success = regions.Count == ExpectedRegionCount && archetypes == ExpectedArchetypeCount &&
                           nodes.Length == ExpectedNodeCount && finiteUnits == ExpectedFiniteGeneralResourceUnits &&
                           finiteCatalogUnits == ExpectedFiniteGeneralResourceUnits && stableIds && legacyCatalogPreserved &&
                           diseaseSources && medicineSources && twoInstances &&
                           publicArchetypeInstances && sixPerRegion && deterministic && seedVariation &&
                           diseaseRuntimePresent && diseaseTrace.Success && searchRegression.Passed && inputParity;
            return new PrototypeContractProbe(
                success,
                "regions=" + regions.Count +
                " archetypes=" + archetypes +
                " nodes=" + nodes.Length +
                " finiteGeneralUnits=" + finiteUnits +
                " archetypeFiniteUnits=" + finiteCatalogUnits +
                " stableIds=" + stableIds.ToString().ToLowerInvariant() +
                " existing=" + existingNodes +
                " new=" + newNodes +
                " removedLegacy=" + removedLegacy +
                " instancesPerArchetype=" + twoInstances.ToString().ToLowerInvariant() +
                " nodesPerRegion=" + (sixPerRegion ? "6" : "mismatch") +
                " diseaseSources=" + diseaseSources.ToString().ToLowerInvariant() +
                " medicineSources=" + medicineSources.ToString().ToLowerInvariant() +
                " deterministic=" + deterministic.ToString().ToLowerInvariant() +
                " seedVariation=" + seedVariation.ToString().ToLowerInvariant() +
                " diseaseRuntime=" + diseaseRuntimePresent.ToString().ToLowerInvariant() +
                " diseaseTrace={" + diseaseTrace.Detail + "}" +
                " searchRegression=" + searchRegression.Passed.ToString().ToLowerInvariant() +
                " inputParity=" + inputParity.ToString().ToLowerInvariant());
        }

        private static string Fingerprint(IEnumerable<PrototypeSearchNodeDefinition> nodes, int seed)
        {
            return string.Join("|", nodes.OrderBy(node => node.NodeId, StringComparer.Ordinal).Select(node =>
                node.NodeId + ":" + JsonUtility.ToJson(new PrototypeSearchNodeSnapshot
                {
                    Remaining = PrototypeSearchNodeLootResolver.Resolve(seed, node)
                })));
        }
    }
}
