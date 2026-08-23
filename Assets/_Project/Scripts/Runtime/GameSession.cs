using System;

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

    public enum ResourceKind
    {
        Wood,
        Stone,
        Food,
        Salvage
    }

    public enum StructureKind
    {
        Campfire,
        Workbench,
        RainCollector
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

        public BagStack(ResourceKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }

        public bool IsEmpty
        {
            get { return Amount <= 0; }
        }
    }

    public sealed class GameSession
    {
        public const int DefaultBagSlotCount = 4;
        public const int MaximumBagSlotCount = 6;
        public const int BagSlotCount = MaximumBagSlotCount;
        public const int StackLimit = 2;
        public const int BagUpgradeWoodCost = 2;
        public const int BagUpgradeSalvageCost = 1;
        public const int FinalDay = 5;

        private readonly int[] storage = new int[4];
        private readonly BagStack[] bag = new BagStack[BagSlotCount];
        private readonly bool[] structures = new bool[3];
        private readonly bool[] researched = new bool[2];
        private readonly bool[] craftedTools = new bool[2];

        public int Day { get; private set; }
        public float Hunger { get; private set; }
        public float Energy { get; private set; }
        public float Daylight { get; private set; }
        public GamePhase Phase { get; private set; }
        public RunResult Result { get; private set; }
        public bool ExpeditionCompleted { get; private set; }
        public bool IsSwimming { get; private set; }
        public int SignalStage { get; private set; }
        public int ActiveBagSlotCount { get; private set; }
        public PrototypeLocalizedText LastMessage { get; private set; }
        public ResourceKind? PendingKind { get; private set; }
        public int PendingAmount { get; private set; }

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

        public GameSession()
        {
            Reset();
        }

        public void Reset()
        {
            Array.Clear(storage, 0, storage.Length);
            Array.Clear(bag, 0, bag.Length);
            Array.Clear(structures, 0, structures.Length);
            Array.Clear(researched, 0, researched.Length);
            Array.Clear(craftedTools, 0, craftedTools.Length);

            storage[(int)ResourceKind.Wood] = 2;
            storage[(int)ResourceKind.Stone] = 1;
            storage[(int)ResourceKind.Food] = 0;

            Day = 1;
            Hunger = 70f;
            Energy = 100f;
            Daylight = 100f;
            Phase = GamePhase.Camp;
            Result = RunResult.None;
            ExpeditionCompleted = false;
            IsSwimming = false;
            SignalStage = 0;
            ActiveBagSlotCount = DefaultBagSlotCount;
            PendingKind = null;
            PendingAmount = 0;
            LastMessage = Text("message.reset");
        }

        public int GetStorage(ResourceKind kind)
        {
            return storage[(int)kind];
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
            storage[(int)kind] = Math.Max(0, storage[(int)kind] + amount);
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

            if (storage[(int)ResourceKind.Wood] < BagUpgradeWoodCost)
            {
                blockers |= BagCapacityUpgradeBlockers.MissingWood;
            }

            if (storage[(int)ResourceKind.Salvage] < BagUpgradeSalvageCost)
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
                    LastMessage = Text("message.bag_upgrade.complete");
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
                    LastMessage = Text("message.bag_upgrade.materials", storage[(int)ResourceKind.Wood], storage[(int)ResourceKind.Salvage]);
                }
                else if ((blockers & BagCapacityUpgradeBlockers.MissingWood) != 0)
                {
                    LastMessage = Text("message.bag_upgrade.wood", storage[(int)ResourceKind.Wood]);
                }
                else
                {
                    LastMessage = Text("message.bag_upgrade.salvage", storage[(int)ResourceKind.Salvage]);
                }

                return false;
            }

            Spend(BagUpgradeWoodCost, 0, 0, BagUpgradeSalvageCost);
            ActiveBagSlotCount = MaximumBagSlotCount;
            LastMessage = Text("message.bag_upgrade.success");
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

            if (storage[(int)ResourceKind.Wood] < 2)
            {
                blockers |= SignalUpgradeBlockers.MissingWood;
            }

            if (storage[(int)ResourceKind.Salvage] < 2)
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
                    LastMessage = Text("message.signal.materials", storage[(int)ResourceKind.Wood], storage[(int)ResourceKind.Salvage]);
                }
                else if ((blockers & SignalUpgradeBlockers.MissingWood) != 0)
                {
                    LastMessage = Text("message.signal.wood", storage[(int)ResourceKind.Wood]);
                }
                else
                {
                    LastMessage = Text("message.signal.salvage", storage[(int)ResourceKind.Salvage]);
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
            if (Phase != GamePhase.Camp || storage[(int)ResourceKind.Food] <= 0 || Hunger >= 100f)
            {
                LastMessage = Text(storage[(int)ResourceKind.Food] <= 0 ? "message.food.none" : "message.food.full");
                return false;
            }

            storage[(int)ResourceKind.Food] -= 1;
            Hunger = Math.Min(100f, Hunger + 35f);
            LastMessage = Text("message.food.eaten");
            return true;
        }

        public bool BeginSearch()
        {
            if (Phase != GamePhase.Camp || ExpeditionCompleted || Result != RunResult.None)
            {
                LastMessage = Text(ExpeditionCompleted ? "message.search.finished" : "message.search.unavailable");
                return false;
            }

            ClearBag();
            Daylight = 100f;
            IsSwimming = false;
            Phase = GamePhase.Exploring;
            LastMessage = Text("message.search.begin");
            return true;
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

            float daylightDrain = IsSwimming ? 1.15f : 0.75f;
            float energyDrain = IsSwimming ? (moving ? 0.65f : 0.22f) : (moving ? 0.18f : 0f);
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

        public GatherResult TryGather(ResourceKind kind, int baseAmount, bool waterSearch = false)
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
            Energy = Math.Max(0f, Energy - (waterSearch ? 9f : 6f));
            int remaining = AddToBag(kind, amount);
            if (Energy <= 0f)
            {
                Finish(RunResult.Exhausted);
                return GatherResult.Rejected;
            }

            if (remaining > 0)
            {
                PendingKind = kind;
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
            if (!HasPendingLoot || !IsBagSlotActive(index))
            {
                return false;
            }

            BagStack discarded = bag[index];
            bag[index] = new BagStack(PendingKind.Value, Math.Min(StackLimit, PendingAmount));
            PendingKind = null;
            PendingAmount = 0;
            LastMessage = discarded.IsEmpty ? Text("message.bag.empty_fill") : Text("message.bag.replace", discarded.Kind);
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
                    storage[(int)bag[i].Kind] += bag[i].Amount;
                }
            }

            ClearBag();
            ExpeditionCompleted = true;
            IsSwimming = false;
            Phase = GamePhase.Camp;
            if (forced)
            {
                Energy = Math.Max(1f, Energy - 22f);
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
            return EndDay(true, true);
        }

        public bool EndDay(bool campfirePrepared, bool rainCollectorPrepared)
        {
            if (Phase != GamePhase.Camp || !ExpeditionCompleted || Result != RunResult.None)
            {
                LastMessage = Text("message.endday.search");
                return false;
            }

            Hunger = Math.Max(0f, Hunger - 35f);
            if (Hunger <= 0f)
            {
                Energy = Math.Max(0f, Energy - 35f);
            }

            float rest = campfirePrepared && HasStructure(StructureKind.Campfire) ? 38f : 20f;
            if (rainCollectorPrepared && HasStructure(StructureKind.RainCollector))
            {
                rest += 10f;
            }

            Energy = Math.Min(100f, Energy + rest);

            if (Energy <= 0f)
            {
                Finish(RunResult.Exhausted);
                return true;
            }

            if (Day >= FinalDay)
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

        public PrototypeLocalizedText ResultTitle()
        {
            switch (Result)
            {
                case RunResult.Rescued:
                    return Text("result.title.rescued");
                case RunResult.Exhausted:
                    return Text("result.title.exhausted");
                case RunResult.Deadline:
                    return Text("result.title.deadline");
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
                    return Text("result.detail.deadline");
                default:
                    return PrototypeLocalizedText.Empty;
            }
        }

        private void Finish(RunResult result)
        {
            IsSwimming = false;
            Result = result;
            Phase = GamePhase.Result;
            LastMessage = ResultDetail();
        }

        private bool CanAfford(int wood, int stone, int food, int salvage)
        {
            return storage[(int)ResourceKind.Wood] >= wood &&
                   storage[(int)ResourceKind.Stone] >= stone &&
                   storage[(int)ResourceKind.Food] >= food &&
                   storage[(int)ResourceKind.Salvage] >= salvage;
        }

        private void Spend(int wood, int stone, int food, int salvage)
        {
            storage[(int)ResourceKind.Wood] -= wood;
            storage[(int)ResourceKind.Stone] -= stone;
            storage[(int)ResourceKind.Food] -= food;
            storage[(int)ResourceKind.Salvage] -= salvage;
        }

        private int AddToBag(ResourceKind kind, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < ActiveBagSlotCount && remaining > 0; i += 1)
            {
                if (!bag[i].IsEmpty && bag[i].Kind == kind && bag[i].Amount < StackLimit)
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
                    bag[i] = new BagStack(kind, accepted);
                    remaining -= accepted;
                }
            }

            return remaining;
        }

        private void ClearBag()
        {
            Array.Clear(bag, 0, bag.Length);
            PendingKind = null;
            PendingAmount = 0;
        }

        private static PrototypeLocalizedText Text(string key, params object[] arguments)
        {
            return new PrototypeLocalizedText(key, arguments);
        }
    }
}
