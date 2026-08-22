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
        public const int BagSlotCount = 4;
        public const int StackLimit = 2;
        public const int FinalDay = 3;

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
        public string LastMessage { get; private set; }
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
            storage[(int)ResourceKind.Food] = 1;

            Day = 1;
            Hunger = 75f;
            Energy = 100f;
            Daylight = 100f;
            Phase = GamePhase.Camp;
            Result = RunResult.None;
            ExpeditionCompleted = false;
            IsSwimming = false;
            SignalStage = 0;
            PendingKind = null;
            PendingAmount = 0;
            LastMessage = "파도는 열심히 친다. 김씨도 일단 뭐라도 해보기로 했다.";
        }

        public int GetStorage(ResourceKind kind)
        {
            return storage[(int)kind];
        }

        public BagStack GetBagSlot(int index)
        {
            return index >= 0 && index < bag.Length ? bag[index] : default(BagStack);
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
                LastMessage = HasStructure(kind) ? "이미 지어 둔 물건이다. 김씨도 같은 걸 두 번 만들 만큼 한가하지 않다." : "재료가 모자란다. 주머니를 털어도 모래만 나온다.";
                return false;
            }

            switch (kind)
            {
                case StructureKind.Campfire:
                    Spend(2, 1, 0, 0);
                    LastMessage = "모닥불 완성. 불은 문명이고, 연기는 눈물이다.";
                    break;
                case StructureKind.Workbench:
                    Spend(2, 0, 0, 1);
                    LastMessage = "작업대 완성. 수평은 아니지만 물건은 올라간다.";
                    break;
                case StructureKind.RainCollector:
                    Spend(2, 1, 0, 1);
                    LastMessage = "빗물받이 완성. 비가 오면 김씨가 이긴다.";
                    break;
            }

            structures[(int)kind] = true;
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
                LastMessage = !HasStructure(StructureKind.Workbench) ? "연구하려면 먼저 작업대가 필요하다." : "연구 재료가 부족하거나 이미 알아낸 방법이다.";
                return false;
            }

            if (kind == TechKind.StoneAxe)
            {
                Spend(0, 1, 0, 1);
                LastMessage = "연구 완료: 돌과 나무를 묶으면 제법 도끼처럼 보인다.";
            }
            else
            {
                Spend(0, 0, 0, 1);
                LastMessage = "연구 완료: 줄은 묶는 법보다 안 풀리게 하는 법이 중요했다.";
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
                LastMessage = "제작법이나 재료를 다시 확인해야 한다.";
                return false;
            }

            if (kind == TechKind.StoneAxe)
            {
                Spend(1, 1, 0, 0);
                LastMessage = "돌도끼 완성. 나무가 두 배로 억울해진다.";
            }
            else
            {
                Spend(1, 0, 0, 1);
                LastMessage = "밧줄 완성. 이제 숲 안쪽도 김씨 관할이다.";
            }

            craftedTools[(int)kind] = true;
            return true;
        }

        public bool CanUpgradeSignal()
        {
            if (Phase != GamePhase.Camp || SignalStage >= 2 || !HasStructure(StructureKind.Workbench))
            {
                return false;
            }

            if (SignalStage == 0)
            {
                return CanAfford(2, 0, 0, 2);
            }

            return HasRope && CanAfford(2, 0, 0, 2);
        }

        public bool TryUpgradeSignal()
        {
            if (!CanUpgradeSignal())
            {
                LastMessage = SignalStage == 1 && !HasRope ? "마지막 안테나를 세우려면 밧줄이 필요하다." : "작업대와 나무 2, 표류물 2가 필요하다.";
                return false;
            }

            Spend(2, 0, 0, 2);
            SignalStage += 1;
            LastMessage = SignalStage == 1 ? "구조 신호대 골격 완성. 멀리서 보면 꽤 그럴듯하다." : "구조 신호 발신! 김씨의 엉성함이 마침내 주파수를 탔다.";

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
                LastMessage = storage[(int)ResourceKind.Food] <= 0 ? "먹을 것이 없다. 코코넛 그림이라도 그려 볼까." : "지금은 배가 충분히 부르다.";
                return false;
            }

            storage[(int)ResourceKind.Food] -= 1;
            Hunger = Math.Min(100f, Hunger + 35f);
            LastMessage = "식사 완료. 메뉴 이름은 '그냥 익힌 것'이다.";
            return true;
        }

        public bool BeginSearch()
        {
            if (Phase != GamePhase.Camp || ExpeditionCompleted || Result != RunResult.None)
            {
                LastMessage = ExpeditionCompleted ? "오늘 수색은 끝났다. 캠프를 정리하고 다음 날로 넘어가자." : "지금은 수색을 시작할 수 없다.";
                return false;
            }

            ClearBag();
            Daylight = 100f;
            IsSwimming = false;
            Phase = GamePhase.Exploring;
            LastMessage = "김씨 출발. 해 지기 전에는 돌아오는 것이 소박한 목표다.";
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
            LastMessage = swimming
                ? "김씨 입수. 물은 생각보다 차갑고 가방은 생각보다 무겁다."
                : "육지 복귀. 땅이 이렇게 믿음직스러울 줄은 몰랐다.";
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
                LastMessage = "저 물건은 물에 떠 있다. 발만 담가서는 닿지 않는다.";
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
                LastMessage = "가방이 꽉 찼다. 하나를 버려야 새 물건을 챙길 수 있다.";
                return GatherResult.PendingSwap;
            }

            LastMessage = waterSearch
                ? "파도와 씨름해 " + ResourceName(kind) + "을(를) 건졌다. 체력도 같이 떠내려갔다."
                : kind == ResourceKind.Wood && HasAxe
                    ? "돌도끼가 활약했다. 나무를 하나 더 챙겼다."
                    : ResourceName(kind) + "을(를) 챙겼다.";
            return GatherResult.Added;
        }

        public bool ReplaceBagSlot(int index)
        {
            if (!HasPendingLoot || index < 0 || index >= bag.Length)
            {
                return false;
            }

            BagStack discarded = bag[index];
            bag[index] = new BagStack(PendingKind.Value, Math.Min(StackLimit, PendingAmount));
            PendingKind = null;
            PendingAmount = 0;
            LastMessage = discarded.IsEmpty ? "빈칸에 물건을 넣었다." : ResourceName(discarded.Kind) + "을(를) 두고 새 물건을 챙겼다.";
            return true;
        }

        public void DiscardPendingLoot()
        {
            if (!HasPendingLoot)
            {
                return;
            }

            LastMessage = ResourceName(PendingKind.Value) + "은(는) 아쉽지만 두고 간다.";
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
            for (int i = 0; i < bag.Length; i += 1)
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
                LastMessage = "해가 져서 뛰어 돌아왔다. 게 한 마리가 끝까지 응원했다. 아마도.";
            }
            else
            {
                LastMessage = "무사 귀환. 가방은 무겁고 김씨의 표정은 가볍지 않다.";
            }

            return true;
        }

        public bool EndDay()
        {
            if (Phase != GamePhase.Camp || !ExpeditionCompleted || Result != RunResult.None)
            {
                LastMessage = "수색을 마쳐야 오늘을 정산할 수 있다.";
                return false;
            }

            Hunger = Math.Max(0f, Hunger - 25f);
            if (Hunger <= 0f)
            {
                Energy = Math.Max(0f, Energy - 35f);
            }

            float rest = HasStructure(StructureKind.Campfire) ? 38f : 20f;
            if (HasStructure(StructureKind.RainCollector))
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
            LastMessage = Day + "일차 아침. 김씨는 아직도 섬이고, 섬도 아직 김씨다.";
            return true;
        }

        public string ResultTitle()
        {
            switch (Result)
            {
                case RunResult.Rescued:
                    return "구조 성공!";
                case RunResult.Exhausted:
                    return "김씨, 잠시 누움";
                case RunResult.Deadline:
                    return "구조 신호 미완성";
                default:
                    return string.Empty;
            }
        }

        public string ResultDetail()
        {
            switch (Result)
            {
                case RunResult.Rescued:
                    return "급조 안테나가 기적처럼 작동했다. 김씨는 구조대보다 먼저 사진부터 찍었다.";
                case RunResult.Exhausted:
                    return "체력을 모두 소진했다. 다음에는 먹고 쉬고, 해 지기 전에 돌아오자.";
                case RunResult.Deadline:
                    return "3일 안에 구조 신호를 완성하지 못했다. 필요한 표류물과 밧줄을 더 일찍 준비해야 한다.";
                default:
                    return string.Empty;
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
            for (int i = 0; i < bag.Length && remaining > 0; i += 1)
            {
                if (!bag[i].IsEmpty && bag[i].Kind == kind && bag[i].Amount < StackLimit)
                {
                    int accepted = Math.Min(StackLimit - bag[i].Amount, remaining);
                    bag[i].Amount += accepted;
                    remaining -= accepted;
                }
            }

            for (int i = 0; i < bag.Length && remaining > 0; i += 1)
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

        public static string ResourceName(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Wood:
                    return "나무";
                case ResourceKind.Stone:
                    return "돌";
                case ResourceKind.Food:
                    return "식량";
                case ResourceKind.Salvage:
                    return "표류물";
                default:
                    return kind.ToString();
            }
        }
    }
}
