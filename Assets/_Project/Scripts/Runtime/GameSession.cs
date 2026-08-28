using System;
using System.Collections.Generic;
using System.Linq;

namespace KimSurvival
{
    public enum GamePhase
    {
        Camp,
        Exploring,
        Result
    }

    public enum RunResult
    {
        None,
        Rescued,
        Exhausted,
        Deadline
    }

    public enum PrototypeSessionSettlementOutcome
    {
        Continue,
        EarlyEscape,
        Exhausted,
        LongStay
    }

    [Serializable]
    public sealed class PrototypeSessionFlowProfile
    {
        public string StableId;
        public int SettlementDay;
        public int TunableMinimumDay;
        public int TunableMaximumDay;
        public bool IsProvisional;
        public string LongStayResultCode;
        public string LongStayTitleKey;
        public string LongStayDetailKey;

        public PrototypeSessionFlowProfile()
        {
            StableId = string.Empty;
            LongStayResultCode = string.Empty;
            LongStayTitleKey = string.Empty;
            LongStayDetailKey = string.Empty;
        }

        public PrototypeSessionFlowProfile(
            string stableId,
            int settlementDay,
            int tunableMinimumDay,
            int tunableMaximumDay,
            bool isProvisional,
            string longStayResultCode,
            string longStayTitleKey,
            string longStayDetailKey)
        {
            StableId = stableId;
            SettlementDay = settlementDay;
            TunableMinimumDay = tunableMinimumDay;
            TunableMaximumDay = tunableMaximumDay;
            IsProvisional = isProvisional;
            LongStayResultCode = longStayResultCode;
            LongStayTitleKey = longStayTitleKey;
            LongStayDetailKey = longStayDetailKey;
        }
    }

    [Serializable]
    public sealed class PrototypeSessionFlowVerification
    {
        public string StandardProfileId;
        public int StandardSettlementDay;
        public string GameJamProfileId;
        public int GameJamSettlementDay;
        public int GameJamTunableMinimumDay;
        public int GameJamTunableMaximumDay;
        public PrototypeSessionSettlementOutcome StandardDayTwentyOutcome;
        public PrototypeSessionSettlementOutcome StandardDayFiftyOutcome;
        public PrototypeSessionSettlementOutcome GameJamDayNineteenOutcome;
        public PrototypeSessionSettlementOutcome GameJamDayTwentyOutcome;
        public PrototypeSessionSettlementOutcome GameJamDayTwentyEscapeOutcome;
        public bool ContractSatisfied;
    }

    public static class PrototypeSessionFlowProfileCatalog
    {
        public const string StandardProfileId = "session.profile.standard.day50";
        public const string GameJamProvisionalProfileId = "session.profile.gamejam.provisional-day20";
        public const int GameJamProvisionalSettlementDay = 20;
        public const int GameJamTunableMinimumDay = 15;
        public const int GameJamTunableMaximumDay = 20;
        public const string StandardLongStayResultCode = "settlement.standard.day50";
        public const string GameJamLongStayResultCode = "settlement.gamejam.long-stay.provisional";

        private static readonly PrototypeSessionFlowProfile StandardProfile = new PrototypeSessionFlowProfile(
            StandardProfileId,
            GameSession.FinalDay,
            GameSession.FinalDay,
            GameSession.FinalDay,
            false,
            StandardLongStayResultCode,
            "result.title.deadline",
            "result.detail.deadline");

        private static readonly PrototypeSessionFlowProfile GameJamProfile = new PrototypeSessionFlowProfile(
            GameJamProvisionalProfileId,
            GameJamProvisionalSettlementDay,
            GameJamTunableMinimumDay,
            GameJamTunableMaximumDay,
            true,
            GameJamLongStayResultCode,
            "result.title.gamejam_long_stay",
            "result.detail.gamejam_long_stay");

        private static readonly PrototypeSessionFlowProfile[] Profiles =
        {
            StandardProfile,
            GameJamProfile
        };

        public static IReadOnlyList<PrototypeSessionFlowProfile> All
        {
            get { return Profiles.Select(Clone).ToArray(); }
        }

        public static PrototypeSessionFlowProfile Standard
        {
            get { return Clone(StandardProfile); }
        }

        public static PrototypeSessionFlowProfile GameJamProvisional
        {
            get { return Clone(GameJamProfile); }
        }

        public static bool TryGet(string stableId, out PrototypeSessionFlowProfile profile)
        {
            PrototypeSessionFlowProfile definition = Profiles.FirstOrDefault(candidate =>
                string.Equals(candidate.StableId, stableId, StringComparison.Ordinal));
            profile = definition == null ? null : Clone(definition);
            return definition != null;
        }

        private static PrototypeSessionFlowProfile Clone(PrototypeSessionFlowProfile source)
        {
            return new PrototypeSessionFlowProfile(
                source.StableId,
                source.SettlementDay,
                source.TunableMinimumDay,
                source.TunableMaximumDay,
                source.IsProvisional,
                source.LongStayResultCode,
                source.LongStayTitleKey,
                source.LongStayDetailKey);
        }

        public static PrototypeSessionSettlementOutcome ResolveSettlement(
            string profileId,
            int day,
            bool earlyEscapeCompleted,
            bool exhausted)
        {
            if (!TryGet(profileId, out PrototypeSessionFlowProfile profile))
            {
                throw new ArgumentException("Unknown session profile: " + profileId, nameof(profileId));
            }

            if (earlyEscapeCompleted) return PrototypeSessionSettlementOutcome.EarlyEscape;
            if (exhausted) return PrototypeSessionSettlementOutcome.Exhausted;
            return day >= profile.SettlementDay
                ? PrototypeSessionSettlementOutcome.LongStay
                : PrototypeSessionSettlementOutcome.Continue;
        }

        public static PrototypeSessionFlowVerification CaptureVerification()
        {
            PrototypeSessionSettlementOutcome standardDayTwenty = ResolveSettlement(
                StandardProfileId, GameJamProvisionalSettlementDay, false, false);
            PrototypeSessionSettlementOutcome standardDayFifty = ResolveSettlement(
                StandardProfileId, GameSession.FinalDay, false, false);
            PrototypeSessionSettlementOutcome gameJamDayNineteen = ResolveSettlement(
                GameJamProvisionalProfileId, GameJamProvisionalSettlementDay - 1, false, false);
            PrototypeSessionSettlementOutcome gameJamDayTwenty = ResolveSettlement(
                GameJamProvisionalProfileId, GameJamProvisionalSettlementDay, false, false);
            PrototypeSessionSettlementOutcome gameJamDayTwentyEscape = ResolveSettlement(
                GameJamProvisionalProfileId, GameJamProvisionalSettlementDay, true, true);

            return new PrototypeSessionFlowVerification
            {
                StandardProfileId = StandardProfileId,
                StandardSettlementDay = StandardProfile.SettlementDay,
                GameJamProfileId = GameJamProvisionalProfileId,
                GameJamSettlementDay = GameJamProfile.SettlementDay,
                GameJamTunableMinimumDay = GameJamProfile.TunableMinimumDay,
                GameJamTunableMaximumDay = GameJamProfile.TunableMaximumDay,
                StandardDayTwentyOutcome = standardDayTwenty,
                StandardDayFiftyOutcome = standardDayFifty,
                GameJamDayNineteenOutcome = gameJamDayNineteen,
                GameJamDayTwentyOutcome = gameJamDayTwenty,
                GameJamDayTwentyEscapeOutcome = gameJamDayTwentyEscape,
                ContractSatisfied = StandardProfile.SettlementDay == GameSession.FinalDay &&
                                    GameJamProfile.SettlementDay == GameJamProvisionalSettlementDay &&
                                    GameJamProfile.TunableMinimumDay == GameJamTunableMinimumDay &&
                                    GameJamProfile.TunableMaximumDay == GameJamTunableMaximumDay &&
                                    standardDayTwenty == PrototypeSessionSettlementOutcome.Continue &&
                                    standardDayFifty == PrototypeSessionSettlementOutcome.LongStay &&
                                    gameJamDayNineteen == PrototypeSessionSettlementOutcome.Continue &&
                                    gameJamDayTwenty == PrototypeSessionSettlementOutcome.LongStay &&
                                    gameJamDayTwentyEscape == PrototypeSessionSettlementOutcome.EarlyEscape
            };
        }
    }

    public enum ResourceKind
    {
        Wood = 0,
        Stone = 1,
        Food = 2,
        Salvage = 3
    }

    public enum StructureKind
    {
        Campfire,
        Workbench,
        RainCollector,
        Bed,
        Sofa
    }

    public enum TechKind
    {
        StoneAxe,
        Rope
    }

    public enum GatherResult
    {
        Rejected,
        Added,
        PendingSwap
    }

    [Flags]
    public enum SignalUpgradeBlockers
    {
        None = 0,
        NotAtCamp = 1 << 0,
        Complete = 1 << 1,
        MissingWorkbench = 1 << 2,
        MissingRope = 1 << 3,
        MissingWood = 1 << 4,
        MissingSalvage = 1 << 5
    }

    [Flags]
    public enum BagCapacityUpgradeBlockers
    {
        None = 0,
        NotAtCamp = 1 << 0,
        MissingWorkbench = 1 << 1,
        Complete = 1 << 2,
        MissingWood = 1 << 3,
        MissingSalvage = 1 << 4
    }

    [Serializable]
    public struct BagStack
    {
        public ResourceKind Kind;
        public int Amount;
        public string StableResourceId;

        public BagStack(ResourceKind kind, int amount)
            : this(GameSession.StableResourceIdForLegacy(kind), kind, amount)
        {
        }

        public BagStack(string stableResourceId, ResourceKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
            StableResourceId = string.IsNullOrWhiteSpace(stableResourceId)
                ? GameSession.StableResourceIdForLegacy(kind)
                : stableResourceId;
        }

        public bool IsEmpty
        {
            get { return Amount <= 0; }
        }
    }

    [Serializable]
    public struct StableResourceAmount
    {
        public string StableResourceId;
        public ResourceKind LegacyKind;
        public int Amount;

        public StableResourceAmount(string stableResourceId, ResourceKind legacyKind, int amount)
        {
            StableResourceId = stableResourceId ?? string.Empty;
            LegacyKind = legacyKind;
            Amount = amount;
        }
    }

    [Serializable]
    public sealed class GameSessionStableState
    {
        public int MaxUnlockedExpeditionOrdinal;
        public int ActiveBagSlotCount = GameSession.DefaultBagSlotCount;
        public BagStack[] Bag = Array.Empty<BagStack>();
        public StableResourceAmount[] Storage = Array.Empty<StableResourceAmount>();
        public bool HasPendingLoot;
        public string PendingStableResourceId = string.Empty;
        public ResourceKind PendingKind;
        public int PendingAmount;
        public int Health = 100;
        public string[] AppliedHealthTransactionIds = Array.Empty<string>();
    }

    public sealed partial class GameSession
    {
        public const int DefaultBagSlotCount = 4;
        public const int MaximumBagSlotCount = 10;
        public const int BagSlotCount = MaximumBagSlotCount;
        public const int StackLimit = 2;
        public const int BagUpgradeWoodCost = 2;
        public const int BagUpgradeSalvageCost = 1;
        public const int FinalDay = 50;

        private static readonly StableResourceAmount[] StableResourceCatalog =
        {
            new StableResourceAmount("resource.wood", ResourceKind.Wood, 0),
            new StableResourceAmount("resource.salvage", ResourceKind.Salvage, 0),
            new StableResourceAmount("resource.food", ResourceKind.Food, 0),
            new StableResourceAmount("resource.fabric", ResourceKind.Salvage, 0),
            new StableResourceAmount("resource.fiber", ResourceKind.Wood, 0),
            new StableResourceAmount("resource.medicine", ResourceKind.Food, 0),
            new StableResourceAmount("resource.stone", ResourceKind.Stone, 0),
            new StableResourceAmount("resource.metal", ResourceKind.Stone, 0),
            new StableResourceAmount("resource.wire", ResourceKind.Salvage, 0),
            new StableResourceAmount("resource.fuel", ResourceKind.Salvage, 0),
            new StableResourceAmount("resource.chemicals", ResourceKind.Salvage, 0),
            new StableResourceAmount("resource.electronics", ResourceKind.Salvage, 0)
        };

        private readonly Dictionary<string, int> stableStorage = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly BagStack[] bag = new BagStack[BagSlotCount];
        private readonly bool[] structures = new bool[Enum.GetValues(typeof(StructureKind)).Length];
        private readonly bool[] researched = new bool[2];
        private readonly bool[] craftedTools = new bool[2];
        private readonly HashSet<string> appliedHealthTransactionIds = new HashSet<string>(StringComparer.Ordinal);
        private PrototypeSessionFlowProfile sessionProfile;

        public int Day { get; private set; }
        public float Hunger { get; private set; }
        public float Energy { get; private set; }
        public float Daylight { get; private set; }
        public int Health { get; private set; }
        public GamePhase Phase { get; private set; }
        public RunResult Result { get; private set; }
        public bool ExpeditionCompleted { get; private set; }
        public bool IsSwimming { get; private set; }
        public int SignalStage { get; private set; }
        public int ActiveBagSlotCount { get; private set; }
        public PrototypeLocalizedText LastMessage { get; private set; }
        public ResourceKind? PendingKind { get; private set; }
        public string PendingStableResourceId { get; private set; }
        public int PendingAmount { get; private set; }
        public int RunSeed { get; private set; }
        public int MaxUnlockedExpeditionOrdinal { get; private set; }
        public PrototypeExpeditionRegionId? SelectedRegionId { get; private set; }
        public string ActiveRegionProfileId { get; private set; }
        public string LastExpeditionResultId { get; private set; }
        public string CompletedEscapeId { get; private set; }
        public string SessionProfileId { get { return sessionProfile.StableId; } }
        public int SettlementDay { get { return sessionProfile.SettlementDay; } }
        public bool IsProvisionalSessionProfile { get { return sessionProfile.IsProvisional; } }
        public string TerminalSettlementCode { get; private set; }
        public int TerminalCommitCount { get; private set; }

        public bool HasPendingLoot
        {
            get { return PendingKind.HasValue && PendingAmount > 0; }
        }

        public bool HasAxe
        {
            get { return craftedTools[(int)TechKind.StoneAxe]; }
        }

        public bool HasRope
        {
            get { return craftedTools[(int)TechKind.Rope]; }
        }

        public GameSession(int runSeed = PrototypeExpeditionRegionCatalog.DefaultRunSeed)
            : this(runSeed, PrototypeSessionFlowProfileCatalog.StandardProfileId)
        {
        }

        public GameSession(int runSeed, string sessionProfileId)
        {
            if (!PrototypeSessionFlowProfileCatalog.TryGet(sessionProfileId, out sessionProfile))
            {
                throw new ArgumentException("Unknown session profile: " + sessionProfileId, nameof(sessionProfileId));
            }
            RunSeed = runSeed;
            Reset();
        }

        public void Reset()
        {
            ResetStableStorage();
            Array.Clear(bag, 0, bag.Length);
            Array.Clear(structures, 0, structures.Length);
            Array.Clear(researched, 0, researched.Length);
            Array.Clear(craftedTools, 0, craftedTools.Length);

            AddStableStorage("resource.wood", ResourceKind.Wood, 2);
            AddStableStorage("resource.stone", ResourceKind.Stone, 1);

            Day = 1;
            Hunger = 70f;
            Energy = 100f;
            Daylight = 100f;
            Health = 100;
            appliedHealthTransactionIds.Clear();
            Phase = GamePhase.Camp;
            Result = RunResult.None;
            ExpeditionCompleted = false;
            IsSwimming = false;
            SignalStage = 0;
            ActiveBagSlotCount = DefaultBagSlotCount;
            MaxUnlockedExpeditionOrdinal = 0;
            PendingKind = null;
            PendingStableResourceId = string.Empty;
            PendingAmount = 0;
            SelectedRegionId = null;
            ActiveRegionProfileId = string.Empty;
            LastExpeditionResultId = string.Empty;
            CompletedEscapeId = string.Empty;
            TerminalSettlementCode = string.Empty;
            TerminalCommitCount = 0;
            LastMessage = Text("message.reset");
        }

        public void Reset(int runSeed)
        {
            RunSeed = runSeed;
            Reset();
        }

        public void Reset(int runSeed, string sessionProfileId)
        {
            if (!PrototypeSessionFlowProfileCatalog.TryGet(sessionProfileId, out PrototypeSessionFlowProfile profile))
            {
                throw new ArgumentException("Unknown session profile: " + sessionProfileId, nameof(sessionProfileId));
            }

            sessionProfile = profile;
            RunSeed = runSeed;
            Reset();
        }

        public PrototypeSessionSettlementOutcome EvaluateSettlement(
            bool earlyEscapeCompleted,
            bool exhausted)
        {
            return PrototypeSessionFlowProfileCatalog.ResolveSettlement(
                SessionProfileId,
                Day,
                earlyEscapeCompleted,
                exhausted);
        }

        public int GetStorage(ResourceKind kind)
        {
            return GetStableStorage(StableResourceIdForLegacy(kind));
        }

        public int GetSpendableLegacyStorage(ResourceKind kind)
        {
            return GetStorage(kind);
        }

        public int GetLegacyAggregateStorage(ResourceKind kind)
        {
            long total = 0;
            for (int index = 0; index < StableResourceCatalog.Length; index += 1)
            {
                StableResourceAmount definition = StableResourceCatalog[index];
                if (definition.LegacyKind == kind)
                {
                    total += GetStableStorage(definition.StableResourceId);
                }
            }
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        public int GetStableStorage(string stableResourceId)
        {
            return !string.IsNullOrWhiteSpace(stableResourceId) &&
                   stableStorage.TryGetValue(stableResourceId, out int amount)
                ? amount
                : 0;
        }

        public StableResourceAmount[] GetStableStorageEntries()
        {
            StableResourceAmount[] entries = new StableResourceAmount[StableResourceCatalog.Length];
            for (int index = 0; index < StableResourceCatalog.Length; index += 1)
            {
                StableResourceAmount definition = StableResourceCatalog[index];
                entries[index] = new StableResourceAmount(
                    definition.StableResourceId,
                    definition.LegacyKind,
                    GetStableStorage(definition.StableResourceId));
            }
            return entries;
        }

        public static StableResourceAmount[] GetStableResourceCatalog()
        {
            return StableResourceCatalog
                .Select(definition => new StableResourceAmount(
                    definition.StableResourceId,
                    definition.LegacyKind,
                    0))
                .ToArray();
        }

        public bool IsStableStorageSynchronized()
        {
            if (stableStorage.Count != StableResourceCatalog.Length)
            {
                return false;
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (StableResourceAmount definition in StableResourceCatalog)
            {
                if (string.IsNullOrWhiteSpace(definition.StableResourceId) ||
                    !seenIds.Add(definition.StableResourceId) ||
                    !stableStorage.TryGetValue(definition.StableResourceId, out int amount) || amount < 0)
                {
                    return false;
                }
            }
            return stableStorage.Keys.All(seenIds.Contains);
        }

        public static string StableResourceIdForLegacy(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Wood:
                    return "resource.wood";
                case ResourceKind.Stone:
                    return "resource.stone";
                case ResourceKind.Food:
                    return "resource.food";
                case ResourceKind.Salvage:
                    return "resource.salvage";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown legacy resource kind.");
            }
        }

        public static bool TryGetLegacyResourceKind(string stableResourceId, out ResourceKind legacyKind)
        {
            for (int index = 0; index < StableResourceCatalog.Length; index += 1)
            {
                StableResourceAmount definition = StableResourceCatalog[index];
                if (string.Equals(definition.StableResourceId, stableResourceId, StringComparison.Ordinal))
                {
                    legacyKind = definition.LegacyKind;
                    return true;
                }
            }

            legacyKind = default(ResourceKind);
            return false;
        }

        public bool CanAffordStableResource(string stableResourceId, int amount)
        {
            return amount >= 0 && TryGetLegacyResourceKind(stableResourceId, out _) &&
                   GetStableStorage(stableResourceId) >= amount;
        }

        public bool CanAffordStableResources(IEnumerable<StableResourceAmount> costs)
        {
            return TryNormalizeStableCosts(costs, out Dictionary<string, int> normalized) &&
                   normalized.All(cost => GetStableStorage(cost.Key) >= cost.Value);
        }

        public bool TrySpendStableResources(IEnumerable<StableResourceAmount> costs)
        {
            if (Phase != GamePhase.Camp || Result != RunResult.None ||
                !IsStableStorageSynchronized() ||
                !TryNormalizeStableCosts(costs, out Dictionary<string, int> normalized) ||
                normalized.Any(cost => GetStableStorage(cost.Key) < cost.Value))
            {
                return false;
            }

            foreach (KeyValuePair<string, int> cost in normalized)
            {
                stableStorage[cost.Key] -= cost.Value;
            }
            if (!IsStableStorageSynchronized())
            {
                throw new InvalidOperationException("Stable resource ledger became invalid.");
            }
            return true;
        }

        public bool TrySpendStableResource(string stableResourceId, int amount)
        {
            if (Phase != GamePhase.Camp || Result != RunResult.None ||
                !IsStableStorageSynchronized() ||
                !CanAffordStableResource(stableResourceId, amount))
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            stableStorage[stableResourceId] -= amount;
            if (!IsStableStorageSynchronized())
            {
                throw new InvalidOperationException("Stable resource ledger became invalid.");
            }
            return true;
        }

        public bool ApplyHealthDelta(string transactionId, int delta)
        {
            if (string.IsNullOrWhiteSpace(transactionId) ||
                !appliedHealthTransactionIds.Add(transactionId))
            {
                return false;
            }

            Health = Math.Max(0, Math.Min(100, Health + delta));
            return true;
        }

        public bool HasAppliedHealthDelta(string transactionId)
        {
            return !string.IsNullOrWhiteSpace(transactionId) &&
                   appliedHealthTransactionIds.Contains(transactionId);
        }

        public GameSessionStableState CaptureStableState()
        {
            BagStack[] capturedBag = new BagStack[BagSlotCount];
            for (int index = 0; index < bag.Length; index += 1)
            {
                BagStack stack = bag[index];
                capturedBag[index] = stack.IsEmpty
                    ? default(BagStack)
                    : new BagStack(StableResourceIdOrLegacy(stack), stack.Kind, stack.Amount);
            }

            return new GameSessionStableState
            {
                MaxUnlockedExpeditionOrdinal = MaxUnlockedExpeditionOrdinal,
                ActiveBagSlotCount = ActiveBagSlotCount,
                Bag = capturedBag,
                Storage = GetStableStorageEntries(),
                HasPendingLoot = HasPendingLoot,
                PendingStableResourceId = HasPendingLoot
                    ? PendingStableResourceId
                    : string.Empty,
                PendingKind = HasPendingLoot ? PendingKind.Value : default(ResourceKind),
                PendingAmount = HasPendingLoot ? PendingAmount : 0,
                Health = Health,
                AppliedHealthTransactionIds = appliedHealthTransactionIds
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray()
            };
        }

        public bool RestoreStableState(GameSessionStableState state)
        {
            if (state == null || state.MaxUnlockedExpeditionOrdinal < 0 ||
                state.MaxUnlockedExpeditionOrdinal >= PrototypeExpeditionRegionCatalog.All.Count ||
                state.ActiveBagSlotCount < DefaultBagSlotCount ||
                state.ActiveBagSlotCount > MaximumBagSlotCount ||
                (state.ActiveBagSlotCount - DefaultBagSlotCount) % 2 != 0)
            {
                return false;
            }

            BagStack[] sourceBag = state.Bag ?? Array.Empty<BagStack>();
            if (sourceBag.Length > BagSlotCount)
            {
                return false;
            }

            BagStack[] restoredBag = new BagStack[BagSlotCount];
            for (int index = 0; index < sourceBag.Length; index += 1)
            {
                BagStack source = sourceBag[index];
                if (source.Amount < 0 || source.Amount > StackLimit ||
                    (index >= state.ActiveBagSlotCount && !source.IsEmpty))
                {
                    return false;
                }
                if (source.IsEmpty)
                {
                    continue;
                }

                string stableResourceId = StableResourceIdOrLegacy(source);
                if (!IsValidStableResource(stableResourceId, source.Kind))
                {
                    return false;
                }
                restoredBag[index] = new BagStack(stableResourceId, source.Kind, source.Amount);
            }

            Dictionary<string, int> restoredStorage = CreateEmptyStableStorage();
            HashSet<string> restoredStorageIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (StableResourceAmount entry in state.Storage ?? Array.Empty<StableResourceAmount>())
            {
                if (entry.Amount < 0 || !IsValidStableResource(entry.StableResourceId, entry.LegacyKind) ||
                    !restoredStorageIds.Add(entry.StableResourceId))
                {
                    return false;
                }
                restoredStorage[entry.StableResourceId] = entry.Amount;
            }

            string restoredPendingStableResourceId = string.Empty;
            if (state.HasPendingLoot)
            {
                restoredPendingStableResourceId = string.IsNullOrWhiteSpace(state.PendingStableResourceId)
                    ? StableResourceIdForLegacy(state.PendingKind)
                    : state.PendingStableResourceId;
                if (state.PendingAmount <= 0 ||
                    !IsValidStableResource(restoredPendingStableResourceId, state.PendingKind))
                {
                    return false;
                }
            }

            HashSet<string> restoredTransactions = new HashSet<string>(StringComparer.Ordinal);
            foreach (string transactionId in state.AppliedHealthTransactionIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(transactionId) || !restoredTransactions.Add(transactionId))
                {
                    return false;
                }
            }

            stableStorage.Clear();
            foreach (StableResourceAmount definition in StableResourceCatalog)
            {
                int amount = restoredStorage[definition.StableResourceId];
                stableStorage.Add(definition.StableResourceId, amount);
            }

            Array.Clear(bag, 0, bag.Length);
            Array.Copy(restoredBag, bag, restoredBag.Length);
            ActiveBagSlotCount = state.ActiveBagSlotCount;
            MaxUnlockedExpeditionOrdinal = state.MaxUnlockedExpeditionOrdinal;
            PendingKind = state.HasPendingLoot ? state.PendingKind : (ResourceKind?)null;
            PendingStableResourceId = restoredPendingStableResourceId;
            PendingAmount = state.HasPendingLoot ? state.PendingAmount : 0;
            Health = Math.Max(0, Math.Min(100, state.Health));
            appliedHealthTransactionIds.Clear();
            appliedHealthTransactionIds.UnionWith(restoredTransactions);
            return true;
        }

        public BagStack GetBagSlot(int index)
        {
            return IsBagSlotActive(index) ? bag[index] : default(BagStack);
        }

        public bool IsBagSlotActive(int index)
        {
            return index >= 0 && index < ActiveBagSlotCount;
        }

        public bool HasBagCapacityUpgrade
        {
            get { return ActiveBagSlotCount >= MaximumBagSlotCount; }
        }

        public int NextBagSlotCount
        {
            get { return Math.Min(MaximumBagSlotCount, ActiveBagSlotCount + 2); }
        }

        public bool HasStructure(StructureKind kind)
        {
            return structures[(int)kind];
        }

        public bool HasResearched(TechKind kind)
        {
            return researched[(int)kind];
        }

        public bool HasCrafted(TechKind kind)
        {
            return craftedTools[(int)kind];
        }

        public void Grant(ResourceKind kind, int amount)
        {
            PrototypeProductionActionCounters.RecordGrant();
            int current = GetSpendableLegacyStorage(kind);
            int target = Math.Max(0, current + amount);
            if (target > current)
            {
                AddStableStorage(StableResourceIdForLegacy(kind), kind, target - current);
            }
            else if (target < current)
            {
                ConsumeLegacyStorage(kind, current - target);
            }
        }

        public bool CanAffordResources(int wood, int stone, int food, int salvage)
        {
            return wood >= 0 && stone >= 0 && food >= 0 && salvage >= 0 &&
                   CanAfford(wood, stone, food, salvage);
        }

        public bool TrySpendResources(int wood, int stone, int food, int salvage)
        {
            if (Phase != GamePhase.Camp || Result != RunResult.None ||
                !CanAffordResources(wood, stone, food, salvage))
            {
                return false;
            }

            Spend(wood, stone, food, salvage);
            return true;
        }

        public bool CanBuild(StructureKind kind)
        {
            if (Phase != GamePhase.Camp || HasStructure(kind))
            {
                return false;
            }

            switch (kind)
            {
                case StructureKind.Campfire:
                    return CanAfford(2, 1, 0, 0);
                case StructureKind.Workbench:
                    return CanAfford(2, 0, 0, 1);
                case StructureKind.RainCollector:
                    return CanAfford(2, 1, 0, 1);
                case StructureKind.Bed:
                    return CanAfford(3, 0, 0, 1);
                case StructureKind.Sofa:
                    return CanAfford(2, 0, 0, 2);
                default:
                    return false;
            }
        }

        public bool TryBuild(StructureKind kind)
        {
            if (!CanBuild(kind))
            {
                LastMessage = Text(HasStructure(kind) ? "message.build.already" : "message.build.materials");
                return false;
            }

            switch (kind)
            {
                case StructureKind.Campfire:
                    Spend(2, 1, 0, 0);
                    LastMessage = Text("message.build.campfire");
                    break;
                case StructureKind.Workbench:
                    Spend(2, 0, 0, 1);
                    LastMessage = Text("message.build.workbench");
                    break;
                case StructureKind.RainCollector:
                    Spend(2, 1, 0, 1);
                    LastMessage = Text("message.build.rain");
                    break;
                case StructureKind.Bed:
                    Spend(3, 0, 0, 1);
                    LastMessage = Text("message.build.bed");
                    break;
                case StructureKind.Sofa:
                    Spend(2, 0, 0, 2);
                    LastMessage = Text("message.build.sofa");
                    break;
            }

            structures[(int)kind] = true;
            return true;
        }

        public bool CanUpgradeBagCapacity()
        {
            return GetBagCapacityUpgradeBlockers() == BagCapacityUpgradeBlockers.None;
        }

        public BagCapacityUpgradeBlockers GetBagCapacityUpgradeBlockers()
        {
            BagCapacityUpgradeBlockers blockers = BagCapacityUpgradeBlockers.None;
            if (Phase != GamePhase.Camp)
            {
                blockers |= BagCapacityUpgradeBlockers.NotAtCamp;
            }

            if (!HasStructure(StructureKind.Workbench))
            {
                blockers |= BagCapacityUpgradeBlockers.MissingWorkbench;
            }

            if (HasBagCapacityUpgrade)
            {
                blockers |= BagCapacityUpgradeBlockers.Complete;
            }

            if (GetSpendableLegacyStorage(ResourceKind.Wood) < BagUpgradeWoodCost)
            {
                blockers |= BagCapacityUpgradeBlockers.MissingWood;
            }

            if (GetSpendableLegacyStorage(ResourceKind.Salvage) < BagUpgradeSalvageCost)
            {
                blockers |= BagCapacityUpgradeBlockers.MissingSalvage;
            }

            return blockers;
        }

        public bool TryUpgradeBagCapacity()
        {
            BagCapacityUpgradeBlockers blockers = GetBagCapacityUpgradeBlockers();
            if (blockers != BagCapacityUpgradeBlockers.None)
            {
                if ((blockers & BagCapacityUpgradeBlockers.Complete) != 0)
                {
                    LastMessage = Text("message.bag_upgrade.complete", MaximumBagSlotCount);
                }
                else if ((blockers & BagCapacityUpgradeBlockers.NotAtCamp) != 0)
                {
                    LastMessage = Text("message.bag_upgrade.camp");
                }
                else if ((blockers & BagCapacityUpgradeBlockers.MissingWorkbench) != 0)
                {
                    LastMessage = Text("message.bag_upgrade.workbench");
                }
                else if ((blockers & BagCapacityUpgradeBlockers.MissingWood) != 0 &&
                         (blockers & BagCapacityUpgradeBlockers.MissingSalvage) != 0)
                {
                    LastMessage = Text("message.bag_upgrade.materials", GetSpendableLegacyStorage(ResourceKind.Wood), GetSpendableLegacyStorage(ResourceKind.Salvage));
                }
                else if ((blockers & BagCapacityUpgradeBlockers.MissingWood) != 0)
                {
                    LastMessage = Text("message.bag_upgrade.wood", GetSpendableLegacyStorage(ResourceKind.Wood));
                }
                else
                {
                    LastMessage = Text("message.bag_upgrade.salvage", GetSpendableLegacyStorage(ResourceKind.Salvage));
                }

                return false;
            }

            Spend(BagUpgradeWoodCost, 0, 0, BagUpgradeSalvageCost);
            ActiveBagSlotCount = NextBagSlotCount;
            LastMessage = Text("message.bag_upgrade.success", ActiveBagSlotCount);
            return true;
        }

        public bool CanResearch(TechKind kind)
        {
            return Phase == GamePhase.Camp && HasStructure(StructureKind.Workbench) && !HasResearched(kind) &&
                   (kind == TechKind.StoneAxe ? CanAfford(0, 1, 0, 1) : CanAfford(0, 0, 0, 1));
        }

        public bool TryResearch(TechKind kind)
        {
            if (!CanResearch(kind))
            {
                LastMessage = Text(!HasStructure(StructureKind.Workbench) ? "message.research.workbench" : "message.research.unavailable");
                return false;
            }

            if (kind == TechKind.StoneAxe)
            {
                Spend(0, 1, 0, 1);
                LastMessage = Text("message.research.axe");
            }
            else
            {
                Spend(0, 0, 0, 1);
                LastMessage = Text("message.research.rope");
            }

            researched[(int)kind] = true;
            return true;
        }

        public bool CanCraft(TechKind kind)
        {
            if (Phase != GamePhase.Camp || !HasResearched(kind) || HasCrafted(kind))
            {
                return false;
            }

            return kind == TechKind.StoneAxe ? CanAfford(1, 1, 0, 0) : CanAfford(1, 0, 0, 1);
        }

        public bool TryCraft(TechKind kind)
        {
            if (!CanCraft(kind))
            {
                LastMessage = Text("message.craft.unavailable");
                return false;
            }

            if (kind == TechKind.StoneAxe)
            {
                Spend(1, 1, 0, 0);
                LastMessage = Text("message.craft.axe");
            }
            else
            {
                Spend(1, 0, 0, 1);
                LastMessage = Text("message.craft.rope");
            }

            craftedTools[(int)kind] = true;
            return true;
        }

        public bool CanUpgradeSignal()
        {
            return GetSignalUpgradeBlockers() == SignalUpgradeBlockers.None;
        }

        public SignalUpgradeBlockers GetSignalUpgradeBlockers()
        {
            SignalUpgradeBlockers blockers = SignalUpgradeBlockers.None;
            if (Phase != GamePhase.Camp)
            {
                blockers |= SignalUpgradeBlockers.NotAtCamp;
            }

            if (SignalStage >= 2)
            {
                blockers |= SignalUpgradeBlockers.Complete;
            }

            if (SignalStage == 0 && !HasStructure(StructureKind.Workbench))
            {
                blockers |= SignalUpgradeBlockers.MissingWorkbench;
            }
            else if (SignalStage == 1 && !HasRope)
            {
                blockers |= SignalUpgradeBlockers.MissingRope;
            }

            if (GetSpendableLegacyStorage(ResourceKind.Wood) < 2)
            {
                blockers |= SignalUpgradeBlockers.MissingWood;
            }

            if (GetSpendableLegacyStorage(ResourceKind.Salvage) < 2)
            {
                blockers |= SignalUpgradeBlockers.MissingSalvage;
            }

            return blockers;
        }

        public bool TryUpgradeSignal()
        {
            SignalUpgradeBlockers blockers = GetSignalUpgradeBlockers();
            if (blockers != SignalUpgradeBlockers.None)
            {
                if ((blockers & (SignalUpgradeBlockers.NotAtCamp | SignalUpgradeBlockers.Complete)) != 0)
                {
                    return false;
                }

                if ((blockers & SignalUpgradeBlockers.MissingWorkbench) != 0)
                {
                    LastMessage = Text("message.signal.workbench");
                }
                else if ((blockers & SignalUpgradeBlockers.MissingRope) != 0)
                {
                    LastMessage = Text("message.signal.rope");
                }
                else if ((blockers & SignalUpgradeBlockers.MissingWood) != 0 &&
                         (blockers & SignalUpgradeBlockers.MissingSalvage) != 0)
                {
                    LastMessage = Text("message.signal.materials", GetSpendableLegacyStorage(ResourceKind.Wood), GetSpendableLegacyStorage(ResourceKind.Salvage));
                }
                else if ((blockers & SignalUpgradeBlockers.MissingWood) != 0)
                {
                    LastMessage = Text("message.signal.wood", GetSpendableLegacyStorage(ResourceKind.Wood));
                }
                else
                {
                    LastMessage = Text("message.signal.salvage", GetSpendableLegacyStorage(ResourceKind.Salvage));
                }

                return false;
            }

            Spend(2, 0, 0, 2);
            SignalStage += 1;
            LastMessage = Text(SignalStage == 1 ? "message.signal.stage1" : "message.signal.stage2");

            if (SignalStage >= 2)
            {
                Finish(RunResult.Rescued);
            }

            return true;
        }

        public bool UseFood()
        {
            int spendableFood = GetSpendableLegacyStorage(ResourceKind.Food);
            if (Phase != GamePhase.Camp || spendableFood <= 0 || Hunger >= 100f)
            {
                LastMessage = Text(spendableFood <= 0 ? "message.food.none" : "message.food.full");
                return false;
            }

            ConsumeLegacyStorage(ResourceKind.Food, 1);
            Hunger = Math.Min(100f, Hunger + PrototypeO11BalanceConfig.MealHungerRecovery);
            Energy = Math.Min(100f, Energy + PrototypeO11BalanceConfig.MealEnergyRecovery);
            LastMessage = Text("message.food.eaten");
            return true;
        }

        public bool BeginSearch()
        {
            // Kept for deterministic legacy regression fixtures. The playable camp path
            // hides the global start button and enters through the proximity map target.
            return BeginSearch(PrototypeExpeditionRegionId.Beach);
        }

        public bool BeginSearch(PrototypeExpeditionRegionId region)
        {
            if (Phase != GamePhase.Camp || ExpeditionCompleted || Result != RunResult.None)
            {
                LastMessage = Text(ExpeditionCompleted ? "message.search.finished" : "message.search.unavailable");
                return false;
            }

            if (!Enum.IsDefined(typeof(PrototypeExpeditionRegionId), region))
            {
                LastMessage = Text("message.search.choose_region");
                return false;
            }

            if (!IsExpeditionRegionUnlocked(region))
            {
                LastMessage = Text("message.search.region_locked");
                return false;
            }

            ClearBag();
            Daylight = 100f;
            IsSwimming = false;
            SelectedRegionId = region;
            ActiveRegionProfileId = PrototypeExpeditionRegionCatalog.Get(region).StableId;
            LastExpeditionResultId = string.Empty;
            Phase = GamePhase.Exploring;
            int enteredOrdinal = (int)region;
            if (enteredOrdinal == MaxUnlockedExpeditionOrdinal &&
                MaxUnlockedExpeditionOrdinal < PrototypeExpeditionRegionCatalog.All.Count - 1)
            {
                MaxUnlockedExpeditionOrdinal += 1;
            }
            LastMessage = Text("message.search.begin_region", region);
            return true;
        }

        public bool IsExpeditionRegionUnlocked(PrototypeExpeditionRegionId region)
        {
            return Enum.IsDefined(typeof(PrototypeExpeditionRegionId), region) &&
                   (int)region <= MaxUnlockedExpeditionOrdinal;
        }

        public bool SetSwimming(bool swimming)
        {
            if (Phase != GamePhase.Exploring || Result != RunResult.None)
            {
                return false;
            }

            if (IsSwimming == swimming)
            {
                return true;
            }

            IsSwimming = swimming;
            LastMessage = Text(swimming ? "message.swim.enter" : "message.swim.exit");
            return true;
        }

        public void TickSearch(float deltaTime, bool moving)
        {
            if (Phase != GamePhase.Exploring || Result != RunResult.None)
            {
                return;
            }

            float daylightDrain = IsSwimming
                ? PrototypeO11BalanceConfig.SwimmingDaylightPerSecond
                : PrototypeO11BalanceConfig.LandDaylightPerSecond;
            float energyDrain = IsSwimming
                ? (moving
                    ? PrototypeO11BalanceConfig.SwimmingMovingEnergyPerSecond
                    : PrototypeO11BalanceConfig.SwimmingIdleEnergyPerSecond)
                : (moving ? PrototypeO11BalanceConfig.LandMovingEnergyPerSecond : 0f);
            Daylight = Math.Max(0f, Daylight - deltaTime * daylightDrain);
            if (energyDrain > 0f)
            {
                Energy = Math.Max(0f, Energy - deltaTime * energyDrain);
            }

            if (Energy <= 0f)
            {
                Finish(RunResult.Exhausted);
            }
            else if (Daylight <= 0f)
            {
                ReturnToCamp(true);
            }
        }

        public bool TryApplySearchNodeCost(int energyCost, int daylightCostMinutes)
        {
            if (Phase != GamePhase.Exploring || Result != RunResult.None || HasPendingLoot ||
                energyCost <= 0 || daylightCostMinutes <= 0 || Energy <= energyCost || Daylight <= daylightCostMinutes)
            {
                LastMessage = Text("message.search_node.too_tired");
                return false;
            }

            Energy = Math.Max(0f, Energy - energyCost);
            Daylight = Math.Max(0f, Daylight - daylightCostMinutes);
            LastMessage = Text("message.search_node.revealed", energyCost, daylightCostMinutes);
            return true;
        }

        public bool RecordSearchNodeResult(string stableNodeId)
        {
            if (Phase != GamePhase.Exploring || Result != RunResult.None ||
                string.IsNullOrWhiteSpace(stableNodeId) || string.IsNullOrWhiteSpace(ActiveRegionProfileId))
            {
                return false;
            }
            int variant = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(RunSeed, ActiveRegionProfileId, stableNodeId), 4);
            LastExpeditionResultId = "result.search-node." + variant;
            return true;
        }

        public int GetBagRemainingCapacity(ResourceKind kind)
        {
            return GetBagRemainingCapacity(StableResourceIdForLegacy(kind), kind);
        }

        public int GetBagRemainingCapacity(string stableResourceId, ResourceKind legacyKind)
        {
            if (!IsValidStableResource(stableResourceId, legacyKind))
            {
                return 0;
            }

            int capacity = 0;
            for (int index = 0; index < ActiveBagSlotCount; index += 1)
            {
                if (bag[index].IsEmpty)
                {
                    capacity += StackLimit;
                }
                else if (string.Equals(
                    StableResourceIdOrLegacy(bag[index]),
                    stableResourceId,
                    StringComparison.Ordinal))
                {
                    capacity += Math.Max(0, StackLimit - bag[index].Amount);
                }
            }
            return capacity;
        }

        public GatherResult TryStoreSearchLoot(ResourceKind kind, int amount)
        {
            return TryStoreSearchLoot(StableResourceIdForLegacy(kind), kind, amount);
        }

        public GatherResult TryStoreSearchLoot(string stableResourceId, ResourceKind legacyKind, int amount)
        {
            if (Phase != GamePhase.Exploring || Result != RunResult.None || HasPendingLoot || amount <= 0 ||
                !IsValidStableResource(stableResourceId, legacyKind))
            {
                return GatherResult.Rejected;
            }

            if (GetBagRemainingCapacity(stableResourceId, legacyKind) < amount)
            {
                PendingKind = legacyKind;
                PendingStableResourceId = stableResourceId;
                PendingAmount = amount;
                LastMessage = Text("message.bag.full");
                return GatherResult.PendingSwap;
            }

            if (AddToBag(stableResourceId, legacyKind, amount) != 0)
            {
                throw new InvalidOperationException("Atomic search loot preflight did not match bag insertion.");
            }
            LastMessage = Text("message.search_node.taken", legacyKind, amount);
            return GatherResult.Added;
        }

        public GatherResult TryGather(
            ResourceKind kind,
            int baseAmount,
            bool waterSearch = false,
            string actionId = "",
            string resolvedResultId = "")
        {
            if (Phase != GamePhase.Exploring || HasPendingLoot || baseAmount <= 0)
            {
                return GatherResult.Rejected;
            }

            if (waterSearch && !IsSwimming)
            {
                LastMessage = Text("message.gather.need_swim");
                return GatherResult.Rejected;
            }

            int amount = kind == ResourceKind.Wood && HasAxe ? baseAmount + 1 : baseAmount;
            if (SelectedRegionId.HasValue && !string.IsNullOrWhiteSpace(actionId))
            {
                LastExpeditionResultId = !string.IsNullOrWhiteSpace(resolvedResultId)
                    ? resolvedResultId
                    : PrototypeExpeditionRegionCatalog.Get(SelectedRegionId.Value)
                        .ResolveActionResultId(RunSeed, actionId);
            }
            Energy = Math.Max(0f, Energy - (waterSearch
                ? PrototypeO11BalanceConfig.LegacyWaterGatherEnergyCost
                : PrototypeO11BalanceConfig.LegacyLandGatherEnergyCost));
            string stableResourceId = StableResourceIdForLegacy(kind);
            int remaining = AddToBag(stableResourceId, kind, amount);
            if (Energy <= 0f)
            {
                Finish(RunResult.Exhausted);
                return GatherResult.Rejected;
            }

            if (remaining > 0)
            {
                PendingKind = kind;
                PendingStableResourceId = stableResourceId;
                PendingAmount = remaining;
                LastMessage = Text("message.bag.full");
                return GatherResult.PendingSwap;
            }

            LastMessage = waterSearch
                ? Text("message.gather.water", kind)
                : kind == ResourceKind.Wood && HasAxe
                    ? Text("message.gather.axe")
                    : Text("message.gather.land", kind);
            return GatherResult.Added;
        }

        public bool ReplaceBagSlot(int index)
        {
            return ReplaceBagSlot(index, out _);
        }

        public bool ReplaceBagSlot(int index, out BagStack displaced)
        {
            displaced = default(BagStack);
            if (!HasPendingLoot || !IsBagSlotActive(index))
            {
                return false;
            }

            displaced = bag[index];
            bag[index] = new BagStack(
                PendingStableResourceId,
                PendingKind.Value,
                Math.Min(StackLimit, PendingAmount));
            PendingKind = null;
            PendingStableResourceId = string.Empty;
            PendingAmount = 0;
            LastMessage = displaced.IsEmpty ? Text("message.bag.empty_fill") : Text("message.bag.replace", displaced.Kind);
            return true;
        }

        public void DiscardPendingLoot()
        {
            if (!HasPendingLoot)
            {
                return;
            }

            LastMessage = Text("message.bag.discard", PendingKind.Value);
            PendingKind = null;
            PendingStableResourceId = string.Empty;
            PendingAmount = 0;
        }

        public bool ReturnToCamp(bool forced)
        {
            if (Phase != GamePhase.Exploring)
            {
                return false;
            }

            DiscardPendingLoot();
            for (int i = 0; i < ActiveBagSlotCount; i += 1)
            {
                if (!bag[i].IsEmpty)
                {
                    AddStableStorage(
                        StableResourceIdOrLegacy(bag[i]),
                        bag[i].Kind,
                        bag[i].Amount);
                }
            }

            ClearBag();
            ExpeditionCompleted = true;
            IsSwimming = false;
            Phase = GamePhase.Camp;
            if (forced)
            {
                Energy = Math.Max(1f, Energy - PrototypeO11BalanceConfig.ForcedReturnEnergyCost);
                LastMessage = Text("message.return.forced");
            }
            else
            {
                LastMessage = Text("message.return.safe");
            }

            return true;
        }

        public bool EndDay()
        {
            return EndDay(true, true, true, true);
        }

        public bool EndDay(bool campfirePrepared, bool rainCollectorPrepared)
        {
            return EndDay(campfirePrepared, rainCollectorPrepared, false, false);
        }

        public bool EndDay(bool campfirePrepared, bool rainCollectorPrepared, bool bedPrepared, bool sofaPrepared)
        {
            if (Phase != GamePhase.Camp || !ExpeditionCompleted || Result != RunResult.None)
            {
                LastMessage = Text("message.endday.search");
                return false;
            }

            Hunger = Math.Max(0f, Hunger - PrototypeO11BalanceConfig.DailyHungerCost);
            if (Hunger <= 0f)
            {
                Energy = Math.Max(0f, Energy - PrototypeO11BalanceConfig.StarvationEnergyCost);
            }

            float rest = PrototypeO11BalanceConfig.NextDayBaseRecovery;
            if (campfirePrepared && HasStructure(StructureKind.Campfire))
            {
                rest += PrototypeO11BalanceConfig.CampfireRecovery;
            }
            if (rainCollectorPrepared && HasStructure(StructureKind.RainCollector))
            {
                rest += PrototypeO11BalanceConfig.RainCollectorRecovery;
            }
            if (bedPrepared && HasStructure(StructureKind.Bed))
            {
                rest += PrototypeO11BalanceConfig.BedRecovery;
            }
            if (sofaPrepared && HasStructure(StructureKind.Sofa))
            {
                rest += PrototypeO11BalanceConfig.SofaRecovery;
            }

            Energy = Math.Min(100f, Energy + rest);

            PrototypeSessionSettlementOutcome settlement = EvaluateSettlement(
                !string.IsNullOrEmpty(CompletedEscapeId),
                Energy <= 0f);
            if (settlement == PrototypeSessionSettlementOutcome.EarlyEscape)
            {
                Finish(RunResult.Rescued);
                return true;
            }

            if (settlement == PrototypeSessionSettlementOutcome.Exhausted)
            {
                Finish(RunResult.Exhausted);
                return true;
            }

            if (settlement == PrototypeSessionSettlementOutcome.LongStay)
            {
                Finish(RunResult.Deadline);
                return true;
            }

            Day += 1;
            ExpeditionCompleted = false;
            Daylight = 100f;
            LastMessage = Text("message.day.start", Day);
            return true;
        }

        public bool TryCompleteEscapeProject(string escapeId)
        {
            if (Phase != GamePhase.Camp || Result != RunResult.None ||
                string.IsNullOrWhiteSpace(escapeId) ||
                !PrototypeEscapeProjectCatalog.All.Any(project =>
                    string.Equals(project.StableId, escapeId, StringComparison.Ordinal)))
            {
                return false;
            }

            CompletedEscapeId = escapeId;
            Finish(RunResult.Rescued);
            return true;
        }

        public PrototypeLocalizedText ResultTitle()
        {
            switch (Result)
            {
                case RunResult.Rescued:
                    return Text("result.title.rescued");
                case RunResult.Exhausted:
                    return Text("result.title.exhausted");
                case RunResult.Deadline:
                    return Text(sessionProfile.LongStayTitleKey);
                default:
                    return PrototypeLocalizedText.Empty;
            }
        }

        public PrototypeLocalizedText ResultDetail()
        {
            switch (Result)
            {
                case RunResult.Rescued:
                    return Text("result.detail.rescued");
                case RunResult.Exhausted:
                    return Text("result.detail.exhausted");
                case RunResult.Deadline:
                    return Text(sessionProfile.LongStayDetailKey);
                default:
                    return PrototypeLocalizedText.Empty;
            }
        }

        private bool Finish(RunResult result)
        {
            if (result == RunResult.None || Result != RunResult.None)
            {
                return false;
            }

            IsSwimming = false;
            Result = result;
            Phase = GamePhase.Result;
            TerminalSettlementCode = result == RunResult.Deadline
                ? sessionProfile.LongStayResultCode
                : string.Empty;
            TerminalCommitCount = 1;
            LastMessage = ResultDetail();
            return true;
        }

        private bool CanAfford(int wood, int stone, int food, int salvage)
        {
            return GetSpendableLegacyStorage(ResourceKind.Wood) >= wood &&
                   GetSpendableLegacyStorage(ResourceKind.Stone) >= stone &&
                   GetSpendableLegacyStorage(ResourceKind.Food) >= food &&
                   GetSpendableLegacyStorage(ResourceKind.Salvage) >= salvage;
        }

        private void Spend(int wood, int stone, int food, int salvage)
        {
            ConsumeLegacyStorage(ResourceKind.Wood, wood);
            ConsumeLegacyStorage(ResourceKind.Stone, stone);
            ConsumeLegacyStorage(ResourceKind.Food, food);
            ConsumeLegacyStorage(ResourceKind.Salvage, salvage);
        }

        private int AddToBag(string stableResourceId, ResourceKind kind, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < ActiveBagSlotCount && remaining > 0; i += 1)
            {
                if (!bag[i].IsEmpty && bag[i].Amount < StackLimit &&
                    string.Equals(StableResourceIdOrLegacy(bag[i]), stableResourceId, StringComparison.Ordinal))
                {
                    int accepted = Math.Min(StackLimit - bag[i].Amount, remaining);
                    bag[i].Amount += accepted;
                    remaining -= accepted;
                }
            }

            for (int i = 0; i < ActiveBagSlotCount && remaining > 0; i += 1)
            {
                if (bag[i].IsEmpty)
                {
                    int accepted = Math.Min(StackLimit, remaining);
                    bag[i] = new BagStack(stableResourceId, kind, accepted);
                    remaining -= accepted;
                }
            }

            return remaining;
        }

        private void ClearBag()
        {
            Array.Clear(bag, 0, bag.Length);
            PendingKind = null;
            PendingStableResourceId = string.Empty;
            PendingAmount = 0;
        }

        private static bool IsValidStableResource(string stableResourceId, ResourceKind legacyKind)
        {
            return TryGetLegacyResourceKind(stableResourceId, out ResourceKind expectedKind) &&
                   expectedKind == legacyKind;
        }

        private static string StableResourceIdOrLegacy(BagStack stack)
        {
            return string.IsNullOrWhiteSpace(stack.StableResourceId)
                ? StableResourceIdForLegacy(stack.Kind)
                : stack.StableResourceId;
        }

        private static Dictionary<string, int> CreateEmptyStableStorage()
        {
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (StableResourceAmount definition in StableResourceCatalog)
            {
                result.Add(definition.StableResourceId, 0);
            }
            return result;
        }

        private static bool TryNormalizeStableCosts(
            IEnumerable<StableResourceAmount> costs,
            out Dictionary<string, int> normalized)
        {
            normalized = new Dictionary<string, int>(StringComparer.Ordinal);
            if (costs == null)
            {
                return false;
            }

            foreach (StableResourceAmount cost in costs)
            {
                if (cost.Amount < 0 || !IsValidStableResource(cost.StableResourceId, cost.LegacyKind))
                {
                    return false;
                }
                if (cost.Amount == 0)
                {
                    continue;
                }

                int previous = normalized.TryGetValue(cost.StableResourceId, out int amount) ? amount : 0;
                long combined = (long)previous + cost.Amount;
                if (combined > int.MaxValue)
                {
                    return false;
                }
                normalized[cost.StableResourceId] = (int)combined;
            }
            return true;
        }

        private void ResetStableStorage()
        {
            stableStorage.Clear();
            foreach (StableResourceAmount definition in StableResourceCatalog)
            {
                stableStorage.Add(definition.StableResourceId, 0);
            }
        }

        private void AddStableStorage(string stableResourceId, ResourceKind legacyKind, int amount)
        {
            if (amount <= 0 || !IsValidStableResource(stableResourceId, legacyKind))
            {
                return;
            }

            stableStorage[stableResourceId] += amount;
        }

        private void ConsumeLegacyStorage(ResourceKind legacyKind, int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            if (!IsStableStorageSynchronized())
            {
                throw new InvalidOperationException("Stable resource ledger is invalid.");
            }
            string canonicalId = StableResourceIdForLegacy(legacyKind);
            if (GetStableStorage(canonicalId) < amount)
            {
                throw new InvalidOperationException("Legacy resource spend exceeded canonical storage.");
            }

            stableStorage[canonicalId] -= amount;
            if (!IsStableStorageSynchronized())
            {
                throw new InvalidOperationException("Stable resource ledger became invalid.");
            }
        }

        private static PrototypeLocalizedText Text(string key, params object[] arguments)
        {
            return new PrototypeLocalizedText(key, arguments);
        }
    }
}
