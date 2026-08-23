using System;
using System.Collections.Generic;

namespace KimSurvival
{
    public enum PrototypeExpeditionRegionId
    {
        Beach,
        Forest,
        Shallows
    }

    public readonly struct PrototypeExpeditionNodeResult
    {
        public PrototypeExpeditionNodeResult(string actionId, ResourceKind resource, int amount, bool water, string resultId)
        {
            ActionId = actionId ?? string.Empty;
            Resource = resource;
            Amount = amount;
            Water = water;
            ResultId = resultId ?? string.Empty;
        }

        public string ActionId { get; }
        public ResourceKind Resource { get; }
        public int Amount { get; }
        public bool Water { get; }
        public string ResultId { get; }
    }

    public sealed class PrototypeExpeditionRegionProfile
    {
        private readonly ResourceKind[] resourcePattern;

        public PrototypeExpeditionRegionProfile(
            PrototypeExpeditionRegionId id,
            string stableId,
            string nameKey,
            string summaryKey,
            string resourceForecastKey,
            int travelMinutes,
            string riskKey,
            string weatherKey,
            string equipmentKey,
            string specialDiscoveryKey,
            int waterNodeCount,
            params ResourceKind[] resourcePattern)
        {
            Id = id;
            StableId = stableId ?? string.Empty;
            NameKey = nameKey ?? string.Empty;
            SummaryKey = summaryKey ?? string.Empty;
            ResourceForecastKey = resourceForecastKey ?? string.Empty;
            TravelMinutes = travelMinutes;
            RiskKey = riskKey ?? string.Empty;
            WeatherKey = weatherKey ?? string.Empty;
            EquipmentKey = equipmentKey ?? string.Empty;
            SpecialDiscoveryKey = specialDiscoveryKey ?? string.Empty;
            WaterNodeCount = Math.Max(0, waterNodeCount);
            this.resourcePattern = resourcePattern ?? Array.Empty<ResourceKind>();
        }

        public PrototypeExpeditionRegionId Id { get; }
        public string StableId { get; }
        public string NameKey { get; }
        public string SummaryKey { get; }
        public string ResourceForecastKey { get; }
        public int TravelMinutes { get; }
        public string RiskKey { get; }
        public string WeatherKey { get; }
        public string EquipmentKey { get; }
        public string SpecialDiscoveryKey { get; }
        public int WaterNodeCount { get; }
        public int NodeCount { get { return 10; } }

        public PrototypeExpeditionNodeResult ResolveNode(int runSeed, int nodeIndex)
        {
            if (resourcePattern.Length == 0)
            {
                throw new InvalidOperationException("Expedition profile requires at least one resource pattern entry.");
            }

            int safeIndex = Math.Max(0, nodeIndex);
            int offset = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, StableId, "node_order"),
                resourcePattern.Length);
            ResourceKind resource = resourcePattern[(safeIndex + offset) % resourcePattern.Length];
            int amount = 1 + PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, StableId, "node_amount_" + safeIndex),
                2);
            string actionId = StableId + ".node." + safeIndex;
            string guaranteedCoreResult = safeIndex == NodeCount - 1
                ? PrototypeExpeditionRegionCatalog.ResolveGuaranteedCoreResult(runSeed, Id)
                : string.Empty;
            string resultId = string.IsNullOrEmpty(guaranteedCoreResult)
                ? "loot." + StableId + "." + resource.ToString().ToLowerInvariant() + "." + amount
                : guaranteedCoreResult;
            return new PrototypeExpeditionNodeResult(actionId, resource, amount, safeIndex < WaterNodeCount, resultId);
        }

        public string ResolveActionResultId(int runSeed, string actionId)
        {
            int variant = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, StableId, actionId ?? string.Empty),
                4);
            return "result." + StableId + "." + variant;
        }
    }

    public readonly struct PrototypeEscapeRouteGuarantee
    {
        public PrototypeEscapeRouteGuarantee(string escapeRouteId, string coreResultId, PrototypeExpeditionRegionId region)
        {
            EscapeRouteId = escapeRouteId ?? string.Empty;
            CoreResultId = coreResultId ?? string.Empty;
            Region = region;
        }

        public string EscapeRouteId { get; }
        public string CoreResultId { get; }
        public PrototypeExpeditionRegionId Region { get; }
    }

    public sealed class PrototypeExpeditionSeedManifest
    {
        private readonly PrototypeEscapeRouteGuarantee[] guarantees;

        public PrototypeExpeditionSeedManifest(int runSeed, PrototypeEscapeRouteGuarantee[] guarantees)
        {
            RunSeed = runSeed;
            this.guarantees = guarantees ?? Array.Empty<PrototypeEscapeRouteGuarantee>();
        }

        public int RunSeed { get; }
        public IReadOnlyList<PrototypeEscapeRouteGuarantee> Guarantees { get { return guarantees; } }
        public bool HasMinimumSoftlockProtection { get { return guarantees.Length >= 3; } }

        public bool GuaranteesRoute(string escapeRouteId)
        {
            for (int i = 0; i < guarantees.Length; i += 1)
            {
                if (string.Equals(guarantees[i].EscapeRouteId, escapeRouteId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static class PrototypeExpeditionRegionCatalog
    {
        public const int DefaultRunSeed = 15000501;

        private static readonly PrototypeExpeditionRegionProfile[] Profiles =
        {
            new PrototypeExpeditionRegionProfile(
                PrototypeExpeditionRegionId.Beach,
                "region.beach",
                "expedition.region.beach.name",
                "expedition.region.beach.summary",
                "expedition.region.beach.resources",
                20,
                "expedition.region.beach.risk",
                "expedition.region.beach.weather",
                "expedition.region.beach.equipment",
                "expedition.region.beach.special",
                2,
                ResourceKind.Salvage, ResourceKind.Food, ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Salvage),
            new PrototypeExpeditionRegionProfile(
                PrototypeExpeditionRegionId.Forest,
                "region.forest",
                "expedition.region.forest.name",
                "expedition.region.forest.summary",
                "expedition.region.forest.resources",
                35,
                "expedition.region.forest.risk",
                "expedition.region.forest.weather",
                "expedition.region.forest.equipment",
                "expedition.region.forest.special",
                1,
                ResourceKind.Wood, ResourceKind.Food, ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Wood),
            new PrototypeExpeditionRegionProfile(
                PrototypeExpeditionRegionId.Shallows,
                "region.shallows",
                "expedition.region.shallows.name",
                "expedition.region.shallows.summary",
                "expedition.region.shallows.resources",
                30,
                "expedition.region.shallows.risk",
                "expedition.region.shallows.weather",
                "expedition.region.shallows.equipment",
                "expedition.region.shallows.special",
                2,
                ResourceKind.Salvage, ResourceKind.Food, ResourceKind.Salvage, ResourceKind.Stone, ResourceKind.Wood)
        };

        private static readonly string[] EscapeRouteIds = { "escape.smoke", "escape.radio", "escape.raft" };
        private static readonly string[] CoreResultIds = { "core.dry_tinder", "core.radio_coil", "core.raft_fastener" };

        public static IReadOnlyList<PrototypeExpeditionRegionProfile> All { get { return Profiles; } }

        public static int CreateRuntimeSeed()
        {
            unchecked
            {
                long ticks = DateTime.UtcNow.Ticks;
                int seed = (int)(ticks ^ (ticks >> 32) ^ Environment.TickCount) & 0x7fffffff;
                return seed == 0 ? DefaultRunSeed : seed;
            }
        }

        public static PrototypeExpeditionRegionProfile Get(PrototypeExpeditionRegionId id)
        {
            return Profiles[(int)id];
        }

        public static PrototypeExpeditionSeedManifest BuildSeedManifest(int runSeed)
        {
            PrototypeEscapeRouteGuarantee[] guarantees = new PrototypeEscapeRouteGuarantee[EscapeRouteIds.Length];
            int regionOffset = PositiveModulo(StableHash(runSeed, "softlock", "route_region"), Profiles.Length);
            for (int i = 0; i < guarantees.Length; i += 1)
            {
                guarantees[i] = new PrototypeEscapeRouteGuarantee(
                    EscapeRouteIds[i],
                    CoreResultIds[i],
                    Profiles[(regionOffset + i) % Profiles.Length].Id);
            }
            return new PrototypeExpeditionSeedManifest(runSeed, guarantees);
        }

        public static string ResolveGuaranteedCoreResult(int runSeed, PrototypeExpeditionRegionId region)
        {
            IReadOnlyList<PrototypeEscapeRouteGuarantee> guarantees = BuildSeedManifest(runSeed).Guarantees;
            for (int i = 0; i < guarantees.Count; i += 1)
            {
                if (guarantees[i].Region == region)
                {
                    return guarantees[i].CoreResultId;
                }
            }

            return string.Empty;
        }

        internal static int StableHash(int runSeed, string profileId, string actionId)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)runSeed) * 16777619u;
                AppendStable(ref hash, profileId);
                AppendStable(ref hash, actionId);
                return (int)(hash & 0x7fffffff);
            }
        }

        internal static int PositiveModulo(int value, int divisor)
        {
            return divisor <= 0 ? 0 : ((value % divisor) + divisor) % divisor;
        }

        private static void AppendStable(ref uint hash, string value)
        {
            string safe = value ?? string.Empty;
            for (int i = 0; i < safe.Length; i += 1)
            {
                hash = (hash ^ safe[i]) * 16777619u;
            }
        }
    }

    public sealed class PrototypeExpeditionMapSelection
    {
        private bool cycleLatched;

        public bool IsOpen { get; private set; }
        public int FocusedIndex { get; private set; }

        public PrototypeExpeditionRegionId FocusedRegionId
        {
            get { return PrototypeExpeditionRegionCatalog.All[FocusedIndex].Id; }
        }

        public void Open(PrototypeExpeditionRegionId? selectedRegion)
        {
            IsOpen = true;
            FocusedIndex = selectedRegion.HasValue ? (int)selectedRegion.Value : 0;
            cycleLatched = false;
        }

        public void Close()
        {
            IsOpen = false;
            cycleLatched = false;
        }

        public bool SetFocusedRegion(PrototypeExpeditionRegionId region)
        {
            if (!IsOpen)
            {
                return false;
            }
            FocusedIndex = (int)region;
            return true;
        }

        public bool StepFocus(int cycleDirection)
        {
            if (!IsOpen)
            {
                return false;
            }
            if (cycleDirection == 0)
            {
                cycleLatched = false;
                return false;
            }
            if (cycleLatched)
            {
                return false;
            }

            int count = PrototypeExpeditionRegionCatalog.All.Count;
            FocusedIndex = (FocusedIndex + (cycleDirection < 0 ? -1 : 1) + count) % count;
            cycleLatched = true;
            return true;
        }
    }
}
