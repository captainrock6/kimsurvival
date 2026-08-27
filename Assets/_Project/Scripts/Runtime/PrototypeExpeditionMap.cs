using System;
using System.Collections.Generic;

namespace KimSurvival
{
    public enum PrototypeExpeditionRegionId
    {
        Beach = 0,
        Forest = 1,
        Shallows = 2,
        RidgeHighland = 3,
        CaveIsland = 4,
        CoveWreck = 5,
        RuinsRelay = 6
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
        public string ResourceCategoryKey { get { return ResourceForecastKey; } }
        public string RelativeAbundanceKey { get { return ResourceForecastKey; } }
        public int TravelMinutes { get; }
        public string RiskKey { get; }
        public string WeatherKey { get; }
        public string EquipmentKey { get; }
        public string SpecialDiscoveryKey { get; }
        public string UnknownDiscoveryStateKey { get { return "expedition.map.unknown"; } }
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
                "region.coast.beach",
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
                "region.forest.grove",
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
                "region.sea.shallows",
                "expedition.region.shallows.name",
                "expedition.region.shallows.summary",
                "expedition.region.shallows.resources",
                30,
                "expedition.region.shallows.risk",
                "expedition.region.shallows.weather",
                "expedition.region.shallows.equipment",
                "expedition.region.shallows.special",
                2,
                ResourceKind.Salvage, ResourceKind.Food, ResourceKind.Salvage, ResourceKind.Stone, ResourceKind.Wood),
            new PrototypeExpeditionRegionProfile(
                PrototypeExpeditionRegionId.RidgeHighland,
                "region.ridge.highland",
                "expedition.region.ridge_highland.name",
                "expedition.region.ridge_highland.summary",
                "expedition.region.ridge_highland.resources",
                45,
                "expedition.region.ridge_highland.risk",
                "expedition.region.ridge_highland.weather",
                "expedition.region.ridge_highland.equipment",
                "expedition.region.ridge_highland.special",
                0,
                ResourceKind.Stone, ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Salvage, ResourceKind.Wood),
            new PrototypeExpeditionRegionProfile(
                PrototypeExpeditionRegionId.CaveIsland,
                "region.cave.island",
                "expedition.region.cave_island.name",
                "expedition.region.cave_island.summary",
                "expedition.region.cave_island.resources",
                40,
                "expedition.region.cave_island.risk",
                "expedition.region.cave_island.weather",
                "expedition.region.cave_island.equipment",
                "expedition.region.cave_island.special",
                0,
                ResourceKind.Stone, ResourceKind.Salvage, ResourceKind.Stone, ResourceKind.Food, ResourceKind.Salvage),
            new PrototypeExpeditionRegionProfile(
                PrototypeExpeditionRegionId.CoveWreck,
                "region.cove.wreck",
                "expedition.region.cove_wreck.name",
                "expedition.region.cove_wreck.summary",
                "expedition.region.cove_wreck.resources",
                50,
                "expedition.region.cove_wreck.risk",
                "expedition.region.cove_wreck.weather",
                "expedition.region.cove_wreck.equipment",
                "expedition.region.cove_wreck.special",
                2,
                ResourceKind.Salvage, ResourceKind.Stone, ResourceKind.Salvage, ResourceKind.Wood, ResourceKind.Salvage),
            new PrototypeExpeditionRegionProfile(
                PrototypeExpeditionRegionId.RuinsRelay,
                "region.ruins.relay",
                "expedition.region.ruins_relay.name",
                "expedition.region.ruins_relay.summary",
                "expedition.region.ruins_relay.resources",
                55,
                "expedition.region.ruins_relay.risk",
                "expedition.region.ruins_relay.weather",
                "expedition.region.ruins_relay.equipment",
                "expedition.region.ruins_relay.special",
                0,
                ResourceKind.Salvage, ResourceKind.Stone, ResourceKind.Salvage, ResourceKind.Salvage, ResourceKind.Stone)
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

    public readonly struct PrototypeRegionLootRoll
    {
        public PrototypeRegionLootRoll(
            int runSeed,
            string regionId,
            string actionId,
            PrototypeExpeditionNodeResult node,
            PrototypeExpeditionSeedManifest manifest)
        {
            RunSeed = runSeed;
            RegionId = regionId ?? string.Empty;
            ActionId = actionId ?? string.Empty;
            Resource = node.Resource;
            Amount = node.Amount;
            MinimumAmount = 1;
            MaximumAmount = 2;
            ResultId = node.ResultId;
            ViableEscapeRouteCount = manifest == null ? 0 : manifest.Guarantees.Count;
            CriticalPartGuaranteed = manifest != null && manifest.HasMinimumSoftlockProtection;
            AlternativeAcquisitionAvailable = CriticalPartGuaranteed;
            LongMissingProtectionActive = CriticalPartGuaranteed;
        }

        public int RunSeed { get; }
        public string RegionId { get; }
        public string ActionId { get; }
        public ResourceKind Resource { get; }
        public int Amount { get; }
        public int MinimumAmount { get; }
        public int MaximumAmount { get; }
        public string ResultId { get; }
        public int ViableEscapeRouteCount { get; }
        public bool CriticalPartGuaranteed { get; }
        public bool AlternativeAcquisitionAvailable { get; }
        public bool LongMissingProtectionActive { get; }
        public bool WithinDeclaredBounds { get { return Amount >= MinimumAmount && Amount <= MaximumAmount; } }
    }

    public static class PrototypeRegionLootRng
    {
        public static PrototypeRegionLootRoll Generate(int runSeed, string regionId, string actionId)
        {
            PrototypeExpeditionRegionProfile profile = ResolveProfile(regionId);
            int nodeIndex = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(runSeed, profile.StableId, actionId ?? string.Empty),
                profile.NodeCount);
            PrototypeExpeditionNodeResult node = profile.ResolveNode(runSeed, nodeIndex);
            return new PrototypeRegionLootRoll(
                runSeed,
                profile.StableId,
                actionId,
                node,
                PrototypeExpeditionRegionCatalog.BuildSeedManifest(runSeed));
        }

        private static PrototypeExpeditionRegionProfile ResolveProfile(string regionId)
        {
            IReadOnlyList<PrototypeExpeditionRegionProfile> profiles = PrototypeExpeditionRegionCatalog.All;
            for (int i = 0; i < profiles.Count; i += 1)
            {
                if (string.Equals(profiles[i].StableId, regionId, StringComparison.OrdinalIgnoreCase))
                {
                    return profiles[i];
                }
            }
            return profiles[0];
        }
    }

    public enum PrototypeExpeditionRegionVisualState
    {
        Default,
        Selected,
        Locked,
        RiskWarning,
        EquipmentMissing,
        DepartureReady,
        Unknown
    }

    public readonly struct PrototypeExpeditionRegionVisualPresentation
    {
        public PrototypeExpeditionRegionVisualPresentation(
            PrototypeExpeditionRegionVisualState state,
            string marker,
            string pattern,
            string localizationKey,
            int borderWeight,
            bool canDepart)
        {
            State = state;
            Marker = marker ?? string.Empty;
            Pattern = pattern ?? string.Empty;
            LocalizationKey = localizationKey ?? string.Empty;
            BorderWeight = Math.Max(1, borderWeight);
            CanDepart = canDepart;
        }

        public PrototypeExpeditionRegionVisualState State { get; }
        public string Marker { get; }
        public string Pattern { get; }
        public string LocalizationKey { get; }
        public int BorderWeight { get; }
        public bool CanDepart { get; }
    }

    public static class PrototypeExpeditionRegionVisualCatalog
    {
        public static PrototypeExpeditionRegionVisualPresentation Get(PrototypeExpeditionRegionVisualState state)
        {
            switch (state)
            {
                case PrototypeExpeditionRegionVisualState.Selected:
                    return new PrototypeExpeditionRegionVisualPresentation(state, "◆", "double", "expedition.map.state.selected", 3, true);
                case PrototypeExpeditionRegionVisualState.Locked:
                    return new PrototypeExpeditionRegionVisualPresentation(state, "▦", "crosshatch", "expedition.map.state.locked", 2, false);
                case PrototypeExpeditionRegionVisualState.RiskWarning:
                    return new PrototypeExpeditionRegionVisualPresentation(state, "!", "warning-stripe", "expedition.map.state.risk", 3, true);
                case PrototypeExpeditionRegionVisualState.EquipmentMissing:
                    return new PrototypeExpeditionRegionVisualPresentation(state, "△", "diagonal-hatch", "expedition.map.state.equipment_missing", 2, false);
                case PrototypeExpeditionRegionVisualState.DepartureReady:
                    return new PrototypeExpeditionRegionVisualPresentation(state, "▶", "solid", "expedition.map.state.ready", 2, true);
                case PrototypeExpeditionRegionVisualState.Unknown:
                    return new PrototypeExpeditionRegionVisualPresentation(state, "?", "dashed", "expedition.map.state.unknown", 2, false);
                default:
                    return new PrototypeExpeditionRegionVisualPresentation(PrototypeExpeditionRegionVisualState.Default, "◇", "single", "expedition.map.state.default", 1, true);
            }
        }
    }

    public sealed class PrototypeExpeditionMapSelection
    {
        private bool cycleLatched;
        private readonly PrototypeExpeditionRegionVisualState[] regionStates = CreateInitialRegionStates();

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

        public PrototypeExpeditionRegionVisualState GetRegionState(PrototypeExpeditionRegionId region)
        {
            return regionStates[(int)region];
        }

        public PrototypeExpeditionRegionVisualState GetDisplayState(PrototypeExpeditionRegionId region)
        {
            return IsOpen && FocusedRegionId == region
                ? PrototypeExpeditionRegionVisualState.Selected
                : GetRegionState(region);
        }

        public bool SetRegionStateForVerification(PrototypeExpeditionRegionId region, PrototypeExpeditionRegionVisualState state)
        {
            if (!Enum.IsDefined(typeof(PrototypeExpeditionRegionId), region) ||
                !Enum.IsDefined(typeof(PrototypeExpeditionRegionVisualState), state))
            {
                return false;
            }

            regionStates[(int)region] = state;
            return true;
        }

        public bool CanDepartFocusedRegion()
        {
            if (!IsOpen)
            {
                return false;
            }

            return PrototypeExpeditionRegionVisualCatalog.Get(GetRegionState(FocusedRegionId)).CanDepart;
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

        public void SyncSequentialUnlock(int maxUnlockedOrdinal)
        {
            int frontier = Math.Max(0, Math.Min(regionStates.Length - 1, maxUnlockedOrdinal));
            for (int index = 0; index < regionStates.Length; index += 1)
            {
                regionStates[index] = index <= frontier
                    ? PrototypeExpeditionRegionVisualState.DepartureReady
                    : PrototypeExpeditionRegionVisualState.Locked;
            }
            if (FocusedIndex > frontier)
            {
                FocusedIndex = frontier;
            }
        }

        private static PrototypeExpeditionRegionVisualState[] CreateInitialRegionStates()
        {
            PrototypeExpeditionRegionVisualState[] states =
                new PrototypeExpeditionRegionVisualState[PrototypeExpeditionRegionCatalog.All.Count];
            for (int index = 0; index < states.Length; index += 1)
            {
                states[index] = index == 0
                    ? PrototypeExpeditionRegionVisualState.DepartureReady
                    : PrototypeExpeditionRegionVisualState.Locked;
            }
            return states;
        }
    }
}
