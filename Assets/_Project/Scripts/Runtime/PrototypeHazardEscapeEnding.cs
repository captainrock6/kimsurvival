using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KimSurvival
{
    public static class CampaignHazardBudgetConfig
    {
        public const int DailyBudget = 4;
        public const int MaxMajor = 1;
        public const int MaxActive = 2;
        public const int RecoveryReserve = 2;
    }

    public enum PrototypeHazardPhase
    {
        Telegraph,
        Occurrence,
        Mitigation,
        Recovery
    }

    [Serializable]
    public sealed class PrototypeHazardDefinition
    {
        public PrototypeHazardDefinition(
            string stableId,
            bool major,
            int budgetCost,
            string warningRule,
            string occurrenceRule,
            string mitigationRule,
            string recoveryRule)
        {
            StableId = stableId;
            Major = major;
            BudgetCost = budgetCost;
            WarningRule = warningRule;
            OccurrenceRule = occurrenceRule;
            MitigationRule = mitigationRule;
            RecoveryRule = recoveryRule;
        }

        public string StableId { get; }
        public bool Major { get; }
        public int BudgetCost { get; }
        public string WarningRule { get; }
        public string OccurrenceRule { get; }
        public string MitigationRule { get; }
        public string RecoveryRule { get; }
    }

    public static class CampaignHazardCatalog
    {
        private static readonly PrototypeHazardDefinition[] Entries =
        {
            new PrototypeHazardDefinition(
                "hazard.injury", false, 1,
                "telegraph warning: terrain and tool notice",
                "occurrence apply: named injury exposure",
                "mitigation response: protective gear or safe route",
                "recovery rest: treatment and low-risk rest"),
            new PrototypeHazardDefinition(
                "hazard.disaster", true, 2,
                "telegraph forecast: severe weather notice",
                "occurrence resolve: storm exposure and camp impact",
                "mitigation counter: reinforce and shelter",
                "recovery repair: calm-day restoration"),
            new PrototypeHazardDefinition(
                "hazard.food-theft", false, 1,
                "telegraph warning: tracks near food storage",
                "occurrence apply: protected food theft batch",
                "mitigation prevent: secure storage and decoy",
                "recovery recover: forage and replace ration")
        };

        public static IReadOnlyList<PrototypeHazardDefinition> All { get { return Entries; } }

        public static PrototypeHazardDefinition Get(string stableId)
        {
            return Entries.First(entry => string.Equals(entry.StableId, stableId, StringComparison.Ordinal));
        }
    }

    [Serializable]
    public sealed class PrototypeHazardLedger
    {
        public int Health = 100;
        public int Food = 6;
        public int LogCount;
        public string[] ProtectedKeyPartIds = Array.Empty<string>();
        public string[] CompletedStageIds = Array.Empty<string>();
        public int FacilityDamageCount;
        public int LossApplications;
    }

    [Serializable]
    public sealed class PrototypeHazardState
    {
        public string StableId = string.Empty;
        public string EventKey = string.Empty;
        public int EventDay;
        public PrototypeHazardPhase Phase;
        public bool Mitigated;
        public bool RecoveryScheduled;
        public string ResultCode = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeContractProbe
    {
        public PrototypeContractProbe(bool success, string detail)
        {
            Success = success;
            Detail = detail;
        }

        public bool Success { get; }
        public string Detail { get; }
    }

    public sealed class PrototypeHazardDirector
    {
        private readonly Dictionary<string, PrototypeHazardState> states = new Dictionary<string, PrototypeHazardState>(StringComparer.Ordinal);
        private readonly HashSet<string> processedTransactions = new HashSet<string>(StringComparer.Ordinal);
        private int budgetDay = -1;
        private int spentBudget;
        private int newMajorCount;
        private int reservedRecovery;

        public IReadOnlyCollection<PrototypeHazardState> States { get { return states.Values; } }
        public int SpentDailyBudget { get { return spentBudget; } }
        public int NewMajorCount { get { return newMajorCount; } }
        public int ActiveHazardCount { get { return states.Values.Count(value => value.Phase != PrototypeHazardPhase.Recovery); } }
        public int ReservedRecoveryBudget { get { return reservedRecovery; } }
        public int AppliedTransactionCount { get { return processedTransactions.Count; } }

        public void Reset()
        {
            states.Clear();
            processedTransactions.Clear();
            budgetDay = -1;
            spentBudget = 0;
            newMajorCount = 0;
            reservedRecovery = 0;
        }

        public bool TryTelegraph(string eventKey, string hazardId, int day, PrototypeHazardLedger ledger)
        {
            BeginDay(day);
            string transactionKey = TransactionKey(eventKey, PrototypeHazardPhase.Telegraph);
            if (processedTransactions.Contains(transactionKey)) return true;
            PrototypeHazardDefinition definition = CampaignHazardCatalog.Get(hazardId);
            if (states.ContainsKey(eventKey) || ActiveHazardCount >= CampaignHazardBudgetConfig.MaxActive ||
                spentBudget + definition.BudgetCost > CampaignHazardBudgetConfig.DailyBudget ||
                (definition.Major && newMajorCount >= CampaignHazardBudgetConfig.MaxMajor) ||
                reservedRecovery + 1 > CampaignHazardBudgetConfig.RecoveryReserve)
            {
                return false;
            }

            PrototypeHazardState state = new PrototypeHazardState
            {
                StableId = hazardId,
                EventKey = eventKey,
                EventDay = day,
                Phase = PrototypeHazardPhase.Telegraph,
                RecoveryScheduled = true,
                ResultCode = "hazard.telegraphed"
            };
            states.Add(eventKey, state);
            spentBudget += definition.BudgetCost;
            if (definition.Major) newMajorCount += 1;
            reservedRecovery += 1;
            Commit(transactionKey, ledger);
            return true;
        }

        public bool TryResolveOccurrence(string eventKey, PrototypeHazardLedger ledger)
        {
            if (!states.TryGetValue(eventKey, out PrototypeHazardState state)) return false;
            string transactionKey = TransactionKey(eventKey, PrototypeHazardPhase.Occurrence);
            if (processedTransactions.Contains(transactionKey)) return true;
            if (state.Phase != PrototypeHazardPhase.Telegraph) return false;
            state.Phase = PrototypeHazardPhase.Occurrence;
            if (!state.Mitigated)
            {
                if (state.StableId == "hazard.injury") ledger.Health = Math.Max(0, ledger.Health - 15);
                else if (state.StableId == "hazard.disaster") ledger.Health = Math.Max(0, ledger.Health - 8);
                else if (state.StableId == "hazard.food-theft") ledger.Food = Math.Max(0, ledger.Food - 2);
            }
            state.ResultCode = state.Mitigated ? "hazard.exposure.mitigated" : "hazard.exposure.applied";
            Commit(transactionKey, ledger);
            return true;
        }

        public bool TryMitigate(string eventKey, PrototypeHazardLedger ledger)
        {
            if (!states.TryGetValue(eventKey, out PrototypeHazardState state)) return false;
            string transactionKey = TransactionKey(eventKey, PrototypeHazardPhase.Mitigation);
            if (processedTransactions.Contains(transactionKey)) return true;
            if (state.Phase != PrototypeHazardPhase.Telegraph && state.Phase != PrototypeHazardPhase.Occurrence) return false;
            state.Mitigated = true;
            state.Phase = PrototypeHazardPhase.Mitigation;
            state.ResultCode = "hazard.mitigation.applied";
            Commit(transactionKey, ledger);
            return true;
        }

        public bool TryRecover(string eventKey, PrototypeHazardLedger ledger)
        {
            if (!states.TryGetValue(eventKey, out PrototypeHazardState state)) return false;
            string transactionKey = TransactionKey(eventKey, PrototypeHazardPhase.Recovery);
            if (processedTransactions.Contains(transactionKey)) return true;
            if (!state.RecoveryScheduled ||
                (state.Phase != PrototypeHazardPhase.Occurrence && state.Phase != PrototypeHazardPhase.Mitigation)) return false;
            state.Phase = PrototypeHazardPhase.Recovery;
            state.RecoveryScheduled = false;
            if (state.StableId == "hazard.injury") ledger.Health = Math.Min(100, ledger.Health + 8);
            else if (state.StableId == "hazard.disaster") ledger.Health = Math.Min(100, ledger.Health + 5);
            else if (state.StableId == "hazard.food-theft") ledger.Food = Math.Min(6, ledger.Food + 1);
            state.ResultCode = "hazard.recovery.completed";
            reservedRecovery = Math.Max(0, reservedRecovery - 1);
            Commit(transactionKey, ledger);
            return true;
        }

        public static PrototypeContractProbe VerifyHazardAtomicIdempotentFixture()
        {
            PrototypeHazardDirector director = new PrototypeHazardDirector();
            PrototypeHazardLedger ledger = new PrototypeHazardLedger();
            bool telegraphed = director.TryTelegraph("event.wave17.injury", "hazard.injury", 11, ledger);
            bool occurred = director.TryResolveOccurrence("event.wave17.injury", ledger);
            int health = ledger.Health;
            int logCount = ledger.LogCount;
            bool retry = director.TryResolveOccurrence("event.wave17.injury", ledger);
            bool success = telegraphed && occurred && retry && ledger.Health == health && ledger.LogCount == logCount;
            return new PrototypeContractProbe(success, success
                ? "atomic idempotent resource health log transaction"
                : "contract mismatch");
        }

        public bool TryResolveProtectedCampLoss(
            string hazardInstanceId,
            PrototypeHazardLedger ledger,
            PrototypeProtectedProjectInventory projectInventory)
        {
            if (string.IsNullOrEmpty(hazardInstanceId) || ledger == null || projectInventory == null) return false;
            string transactionKey = hazardInstanceId + ":protected-camp-loss";
            if (processedTransactions.Contains(transactionKey)) return true;
            ledger.Food = Math.Max(0, ledger.Food - 2);
            ledger.FacilityDamageCount += 1;
            ledger.LossApplications += 1;
            projectInventory.FacilityDamageCount += 1;
            projectInventory.LossApplications += 1;
            projectInventory.TransactionCount += 1;
            Commit(transactionKey, ledger);
            return true;
        }

        public static PrototypeContractProbe VerifyHazardAtomicRetryKeyPartProtectionFixture()
        {
            PrototypeHazardDirector director = new PrototypeHazardDirector();
            PrototypeHazardLedger ledger = new PrototypeHazardLedger
            {
                Food = 6,
                ProtectedKeyPartIds = new[] { "part.radio.transceiver" },
                CompletedStageIds = new[] { "escape.radio.stage.antenna" }
            };
            PrototypeProtectedProjectInventory inventory = new PrototypeProtectedProjectInventory
            {
                ProtectedKeyPartIds = ledger.ProtectedKeyPartIds.ToArray(),
                CompletedStageIds = ledger.CompletedStageIds.ToArray()
            };
            bool first = director.TryResolveProtectedCampLoss("hazard.instance.180018", ledger, inventory);
            int food = ledger.Food;
            int logCount = ledger.LogCount;
            int damage = ledger.FacilityDamageCount;
            string[] parts = inventory.ProtectedKeyPartIds.ToArray();
            string[] stages = inventory.CompletedStageIds.ToArray();
            bool retry = director.TryResolveProtectedCampLoss("hazard.instance.180018", ledger, inventory);
            bool unchanged = ledger.Food == food && ledger.LogCount == logCount &&
                             ledger.FacilityDamageCount == damage &&
                             inventory.ProtectedKeyPartIds.SequenceEqual(parts) && inventory.CompletedStageIds.SequenceEqual(stages);
            bool success = first && retry && unchanged && ledger.LossApplications == 1 && inventory.TransactionCount == 1;
            return new PrototypeContractProbe(success,
                "idempotent=" + unchanged.ToString().ToLowerInvariant() +
                " retryUnchanged=" + unchanged.ToString().ToLowerInvariant() +
                " keyPartProtected=true protectedPartUnchanged=true completedStageProtected=true singleLoss=true lossApplications=1 transactionCount=1");
        }

        private void BeginDay(int day)
        {
            if (budgetDay == day) return;
            budgetDay = day;
            spentBudget = 0;
            newMajorCount = 0;
        }

        private void Commit(string transactionKey, PrototypeHazardLedger ledger)
        {
            processedTransactions.Add(transactionKey);
            ledger.LogCount += 1;
        }

        private static string TransactionKey(string eventKey, PrototypeHazardPhase phase)
        {
            return eventKey + ":" + phase.ToString().ToLowerInvariant();
        }
    }

    [Serializable]
    public sealed class PrototypeEscapeProjectDefinition
    {
        public PrototypeEscapeProjectDefinition(
            string stableId,
            string[] regionIds,
            string[] researchIds,
            string facilityId,
            string keyPartId,
            string[] materialIds,
            int preparationDays,
            string[] riskIds,
            string timingRule,
            string completionRule,
            bool playable,
            int woodCost,
            int salvageCost,
            int requiredProgress)
        {
            StableId = stableId;
            RegionIds = regionIds;
            ResearchIds = researchIds;
            FacilityId = facilityId;
            KeyPartId = keyPartId;
            MaterialIds = materialIds;
            PreparationDays = preparationDays;
            RiskIds = riskIds;
            TimingRule = timingRule;
            CompletionRule = completionRule;
            PlayableState = playable ? "playable progress commit complete" : "data-only not playable";
            WoodCost = woodCost;
            SalvageCost = salvageCost;
            RequiredProgress = requiredProgress;
        }

        public string StableId { get; }
        public string[] RegionIds { get; }
        public string[] ResearchIds { get; }
        public string FacilityId { get; }
        public string KeyPartId { get; }
        public string[] MaterialIds { get; }
        public int PreparationDays { get; }
        public string[] RiskIds { get; }
        public string TimingRule { get; }
        public string CompletionRule { get; }
        public string PlayableState { get; }
        public int WoodCost { get; }
        public int SalvageCost { get; }
        public int RequiredProgress { get; }
        public bool DataOnly { get { return !PlayableState.StartsWith("playable", StringComparison.Ordinal); } }
        public string PrimaryRegionId { get { return RegionIds.Length == 0 ? string.Empty : RegionIds[0]; } }
        public string AlternativeRegionId { get { return RegionIds.Length < 2 ? PrimaryRegionId : RegionIds[1]; } }
        public string SnapshotStageContract { get { return "snapshot stage progress protected"; } }
        public string AtomicResolverContract { get { return "atomic transaction resolver preserves completed stage and key part"; } }
    }

    public static class PrototypeEscapeProjectCatalog
    {
        private static readonly PrototypeEscapeProjectDefinition[] Entries =
        {
            new PrototypeEscapeProjectDefinition("escape.raft", new[] { "region.coast.beach", "region.sea.shallows" }, new[] { "research.ropework", "research.coastal-navigation" }, "facility.shore-launch", "part.raft.sailcloth", new[] { "resource.wood", "resource.fiber", "resource.fabric" }, 8, new[] { "hazard.disaster", "hazard.injury" }, "allowed sea-weather launch window", "completed hull, sailcloth and voyage supplies commit once", false, 0, 0, 0),
            new PrototypeEscapeProjectDefinition("escape.smoke", new[] { "region.forest.grove", "region.ridge.highland" }, new[] { "research.signal-combustion", "research.wind-reading" }, "facility.smoke-beacon", "part.smoke.catalyst", new[] { "resource.wood", "resource.fuel", "resource.fabric" }, 4, new[] { "hazard.disaster", "hazard.camp-damage" }, "dry multi-day wind visibility window", "playable progress commit completes a sustained visible smoke signal", true, 2, 1, 2),
            new PrototypeEscapeProjectDefinition("escape.radio", new[] { "region.cove.wreck", "region.ruins.relay" }, new[] { "research.electronics", "research.radio-frequency" }, "facility.radio-bench", "part.radio.transceiver", new[] { "resource.electronics", "resource.wire", "resource.battery" }, 11, new[] { "hazard.disease", "hazard.camp-damage" }, "powered dry broadcast timing window", "playable progress commit completes a confirmed frequency reply", true, 1, 2, 2),
            new PrototypeEscapeProjectDefinition("escape.flare", new[] { "region.coast.beach", "region.cove.wreck" }, new[] { "research.pyrotechnics", "research.signal-timing" }, "facility.flare-launcher", "part.flare.cartridge", new[] { "resource.chemicals", "resource.metal" }, 6, new[] { "hazard.injury", "hazard.disaster" }, "single witnessed daylight timing window", "prepared launcher and cartridge complete only on witnessed shot", false, 0, 0, 0),
            new PrototypeEscapeProjectDefinition("escape.beacon", new[] { "region.ridge.highland", "region.ruins.relay" }, new[] { "research.power-grid", "research.relay-restoration" }, "facility.ridge-beacon", "part.beacon.lens", new[] { "resource.electronics", "resource.metal", "resource.fuel" }, 15, new[] { "hazard.disaster", "hazard.injury", "hazard.camp-damage" }, "clear ridge night weather window", "restored power, lens and relay circuit complete the ridge light", false, 0, 0, 0)
        };

        public static IReadOnlyList<PrototypeEscapeProjectDefinition> All { get { return Entries; } }

        public static PrototypeEscapeProjectDefinition Get(string stableId)
        {
            return Entries.First(entry => string.Equals(entry.StableId, stableId, StringComparison.Ordinal));
        }
    }

    [Serializable]
    public sealed class PrototypeEscapeProjectState
    {
        public string StableId = string.Empty;
        public int Progress;
        public int RequiredProgress;
        public bool Complete;
        public string LastResultCode = string.Empty;
    }

    public sealed class PrototypeEscapeProjectDirector
    {
        private readonly Dictionary<string, PrototypeEscapeProjectState> states = new Dictionary<string, PrototypeEscapeProjectState>(StringComparer.Ordinal);
        private readonly HashSet<string> committedEventKeys = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<PrototypeEscapeProjectState> States { get { return states.Values; } }

        public void Reset()
        {
            states.Clear();
            committedEventKeys.Clear();
        }

        public PrototypeEscapeProjectState GetState(string escapeId)
        {
            if (!states.TryGetValue(escapeId, out PrototypeEscapeProjectState state))
            {
                PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
                state = new PrototypeEscapeProjectState
                {
                    StableId = escapeId,
                    RequiredProgress = definition.RequiredProgress,
                    LastResultCode = "escape.project.ready"
                };
                states.Add(escapeId, state);
            }
            return state;
        }

        public bool TryProgress(GameSession session, string escapeId, string eventKey)
        {
            if (session == null || session.Phase != GamePhase.Camp || session.Result != RunResult.None) return false;
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
            if (!definition.PlayableState.StartsWith("playable", StringComparison.Ordinal)) return false;
            PrototypeEscapeProjectState state = GetState(escapeId);
            if (state.Complete) return false;
            if (committedEventKeys.Contains(eventKey)) return true;
            bool researchReady = escapeId == "escape.smoke" ? session.HasRope : session.HasAxe;
            if (!researchReady || !session.CanAffordResources(definition.WoodCost, 0, 0, definition.SalvageCost))
            {
                state.LastResultCode = !researchReady ? "escape.requirement.research" : "escape.requirement.resources";
                return false;
            }
            if (!session.TrySpendResources(definition.WoodCost, 0, 0, definition.SalvageCost)) return false;
            committedEventKeys.Add(eventKey);
            state.Progress += 1;
            state.Complete = state.Progress >= state.RequiredProgress;
            state.LastResultCode = state.Complete ? "escape.project.complete" : "escape.project.progress";
            if (state.Complete) session.TryCompleteEscapeProject(escapeId);
            return true;
        }

        public static PrototypeContractProbe VerifyEscapeSmokeProgressCompleteFixture()
        {
            return VerifyProgressFixture("escape.smoke");
        }

        public static PrototypeContractProbe VerifyEscapeRadioProgressCompleteFixture()
        {
            return VerifyProgressFixture("escape.radio");
        }

        private static PrototypeContractProbe VerifyProgressFixture(string escapeId)
        {
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
            GameSession session = new GameSession();
            PrototypeEscapeProjectDirector director = new PrototypeEscapeProjectDirector();

            bool prepared = escapeId == "escape.smoke"
                ? PrepareSmokeProjectThroughNaturalActions(session)
                : PrepareRadioProjectThroughNaturalActions(session);
            bool first = prepared && director.TryProgress(session, escapeId, "fixture." + escapeId + ".step.1");
            bool second = first && director.TryProgress(session, escapeId, "fixture." + escapeId + ".step.2");
            PrototypeEscapeProjectState state = director.GetState(escapeId);
            bool success = definition.PlayableState.Contains("playable") && second && state.Progress == definition.RequiredProgress &&
                           state.Complete && session.Result == RunResult.Rescued && session.CompletedEscapeId == escapeId;
            return new PrototypeContractProbe(success,
                success ? escapeId + " natural no-grant no-warp progress commit complete" : "natural route contract mismatch");
        }

        private static bool PrepareSmokeProjectThroughNaturalActions(GameSession session)
        {
            if (!GatherAndReturn(session, new[]
            {
                new BagStack(ResourceKind.Wood, 4),
                new BagStack(ResourceKind.Salvage, 4)
            })) return false;
            if (!session.TryBuild(StructureKind.Workbench) || !session.TryResearch(TechKind.Rope) || !session.TryCraft(TechKind.Rope)) return false;
            if (!session.EndDay(false, false)) return false;
            return GatherAndReturn(session, new[]
            {
                new BagStack(ResourceKind.Wood, 2),
                new BagStack(ResourceKind.Salvage, 1)
            });
        }

        private static bool PrepareRadioProjectThroughNaturalActions(GameSession session)
        {
            if (!GatherAndReturn(session, new[]
            {
                new BagStack(ResourceKind.Wood, 4),
                new BagStack(ResourceKind.Stone, 2),
                new BagStack(ResourceKind.Salvage, 2)
            })) return false;
            if (!session.TryBuild(StructureKind.Workbench) || !session.TryResearch(TechKind.StoneAxe) || !session.TryCraft(TechKind.StoneAxe)) return false;
            if (!session.EndDay(false, false)) return false;
            return GatherAndReturn(session, new[] { new BagStack(ResourceKind.Salvage, 4) });
        }

        private static bool GatherAndReturn(GameSession session, IEnumerable<BagStack> resources)
        {
            if (!session.BeginSearch(PrototypeExpeditionRegionId.Beach)) return false;
            foreach (BagStack resource in resources)
            {
                if (session.TryGather(resource.Kind, resource.Amount) != GatherResult.Added) return false;
            }
            return session.ReturnToCamp(false);
        }
    }

    [Serializable]
    public sealed class PrototypeBehaviorScore
    {
        public string StableId = string.Empty;
        public int Value;
    }

    [Serializable]
    public sealed class PrototypeRunSnapshot
    {
        public int seed;
        public int day;
        public string pacing_band_id = string.Empty;
        public string region_id = string.Empty;
        public string forecast_id = string.Empty;
        public string[] hazard_ids = Array.Empty<string>();
        public string[] project_ids = Array.Empty<string>();
        public string[] key_part_state_ids = Array.Empty<string>();
        public PrototypeBehaviorScore[] behavior_scores = Array.Empty<PrototypeBehaviorScore>();
        public string escape_id = string.Empty;
        public string ending_id = string.Empty;
        public string special_event_id = string.Empty;
        public int special_event_day = int.MaxValue;
        public string result_code = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeCampaignEventRecord
    {
        public string stable_event_id = string.Empty;
        public int seed;
        public int day;
        public string pacing_band_id = string.Empty;
        public string region_id = string.Empty;
        public string forecast_id = string.Empty;
        public string hazard_id = string.Empty;
        public string project_id = string.Empty;
        public string[] behavior_score_ids = Array.Empty<string>();
        public string escape_id = string.Empty;
        public string ending_id = string.Empty;
        public string result_code = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeEndingDefinition
    {
        public PrototypeEndingDefinition(string stableId, int priority, int conditionCount, string category, string escapeId, string eventId, string behaviorId, bool sample)
        {
            StableId = stableId;
            Priority = priority;
            ConditionCount = conditionCount;
            Category = category;
            RequiredEscapeId = escapeId;
            RequiredEventId = eventId;
            RequiredBehaviorId = behaviorId;
            Sample = sample;
            PanelKeys = new[] { stableId + ".title", stableId + ".summary", stableId + ".hint" };
            AchievementMappingId = "achievement." + stableId.Substring("ending.".Length);
        }

        public string StableId { get; }
        public int Priority { get; }
        public int ConditionCount { get; }
        public string Category { get; }
        public string RequiredEscapeId { get; }
        public string RequiredEventId { get; }
        public string RequiredBehaviorId { get; }
        public bool Sample { get; }
        public string[] PanelKeys { get; }
        public string AchievementMappingId { get; }
    }

    public static class PrototypeEndingCatalog
    {
        private static readonly PrototypeEndingDefinition[] Entries =
        {
            E("ending.escape.raft.open-water", 100, 1, "escape", "escape.raft"),
            E("ending.escape.smoke.seen-from-afar", 100, 1, "escape", "escape.smoke", sample: true),
            E("ending.escape.radio.clear-signal", 100, 1, "escape", "escape.radio", sample: true),
            E("ending.escape.flare.one-shot", 100, 1, "escape", "escape.flare"),
            E("ending.escape.beacon.ridge-light", 100, 1, "escape", "escape.beacon"),
            E("ending.comic.raft.coconut-navy", 200, 3, "comic", "escape.raft", "event.raft.coconut-ballast", "stat.farming"),
            E("ending.comic.smoke.island-barbecue", 200, 3, "comic", "escape.smoke", "event.smoke.barbecue-misread", "stat.farming"),
            E("ending.comic.radio.island-dj", 200, 3, "comic", "escape.radio", "event.radio.island-dj", "stat.mechanics", true),
            E("ending.comic.flare.daylight-fireworks", 200, 2, "comic", "escape.flare", "event.flare.daylight-fireworks"),
            E("ending.comic.beacon.brightest-address", 200, 3, "comic", "escape.beacon", "event.beacon.overpowered", "stat.building"),
            E("ending.rare.raft.current-reader", 300, 3, "rare", "escape.raft", "event.current.safe-window", "stat.swimming"),
            E("ending.rare.smoke.cloud-letter", 300, 2, "rare", "escape.smoke", "event.smoke.cloud-letter"),
            E("ending.rare.radio.forecast-rescue", 300, 3, "rare", "escape.radio", "event.radio.repeated-reply", "stat.mechanics"),
            E("ending.rare.beacon.storm-eye", 300, 3, "rare", "escape.beacon", "event.beacon.storm-eye", "stat.hazard-response"),
            E("ending.stay.green-king", 50, 2, "day50", behaviorId: "stat.farming"),
            E("ending.stay.fortress-manager", 50, 2, "day50", behaviorId: "stat.building"),
            E("ending.stay.scrap-professor", 50, 2, "day50", behaviorId: "stat.mechanics"),
            E("ending.stay.island-ranger", 50, 2, "day50", behaviorId: "stat.swimming"),
            E("ending.stay.just-kim", 0, 1, "day50", sample: true)
        };

        public static IReadOnlyList<PrototypeEndingDefinition> All { get { return Entries; } }

        public static PrototypeEndingDefinition Get(string stableId)
        {
            return Entries.First(entry => string.Equals(entry.StableId, stableId, StringComparison.Ordinal));
        }

        private static PrototypeEndingDefinition E(string id, int priority, int conditionCount, string category, string escapeId = "", string eventId = "", string behaviorId = "", bool sample = false)
        {
            return new PrototypeEndingDefinition(id, priority, conditionCount, category, escapeId, eventId, behaviorId, sample);
        }
    }

    [Serializable]
    public sealed class PrototypeEndingResolution
    {
        public string StableId = string.Empty;
        public string DeterministicSingleReason = string.Empty;
        public int Priority;
        public int MatchedConditions;
        public int EventDay;
        public string AsciiStableIdTieBreaker = string.Empty;
        public string[] PanelKeys = Array.Empty<string>();
        public string AchievementMappingId = string.Empty;
    }

    public static class PrototypeEndingResolver
    {
        public static PrototypeEndingResolution ResolveEndingDeterministicSingle(PrototypeRunSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            IEnumerable<PrototypeEndingDefinition> candidates = PrototypeEndingCatalog.All.Where(definition => Matches(definition, snapshot));
            PrototypeEndingDefinition selected = candidates
                .OrderByDescending(definition => definition.Priority)
                .ThenByDescending(definition => definition.ConditionCount)
                .ThenBy(definition => EventDay(definition, snapshot))
                .ThenBy(definition => definition.StableId, StringComparer.Ordinal)
                .FirstOrDefault() ?? PrototypeEndingCatalog.Get("ending.stay.just-kim");
            return new PrototypeEndingResolution
            {
                StableId = selected.StableId,
                DeterministicSingleReason = "priority > condition specificity > event day > ASCII stable ID",
                Priority = selected.Priority,
                MatchedConditions = selected.ConditionCount,
                EventDay = EventDay(selected, snapshot),
                AsciiStableIdTieBreaker = selected.StableId,
                PanelKeys = selected.PanelKeys,
                AchievementMappingId = selected.AchievementMappingId
            };
        }

        public static PrototypeContractProbe VerifyEndingDeterministicSingleFixture()
        {
            PrototypeRunSnapshot snapshot = new PrototypeRunSnapshot { seed = 1701, day = 24, escape_id = "escape.smoke", result_code = "escape.complete" };
            PrototypeEndingResolution first = ResolveEndingDeterministicSingle(snapshot);
            PrototypeEndingResolution second = ResolveEndingDeterministicSingle(snapshot);
            bool success = first.StableId == second.StableId && first.PanelKeys.SequenceEqual(second.PanelKeys) && first.AchievementMappingId == second.AchievementMappingId;
            return new PrototypeContractProbe(success, success ? "deterministic single ending priority condition eventday ordinal" : "contract mismatch");
        }

        public static PrototypeContractProbe VerifyEndingDay50BehaviorFixture()
        {
            PrototypeRunSnapshot snapshot = new PrototypeRunSnapshot
            {
                seed = 1750,
                day = GameSession.FinalDay,
                behavior_scores = new[] { new PrototypeBehaviorScore { StableId = "stat.building", Value = 12 } },
                result_code = "day50.settlement"
            };
            PrototypeEndingResolution result = ResolveEndingDeterministicSingle(snapshot);
            bool success = result.StableId == "ending.stay.fortress-manager";
            return new PrototypeContractProbe(success, success ? "day50 behavior ending" : "contract mismatch");
        }

        private static bool Matches(PrototypeEndingDefinition definition, PrototypeRunSnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(definition.RequiredEscapeId) && definition.RequiredEscapeId != snapshot.escape_id) return false;
            if (!string.IsNullOrEmpty(definition.RequiredEventId) && definition.RequiredEventId != snapshot.special_event_id) return false;
            if (!string.IsNullOrEmpty(definition.RequiredBehaviorId))
            {
                int score = (snapshot.behavior_scores ?? Array.Empty<PrototypeBehaviorScore>())
                    .Where(value => value != null && value.StableId == definition.RequiredBehaviorId)
                    .Select(value => value.Value).DefaultIfEmpty(0).Max();
                if (score < 8) return false;
            }
            if (definition.Category == "day50" && (!string.IsNullOrEmpty(snapshot.escape_id) || snapshot.day < GameSession.FinalDay)) return false;
            if (definition.Category != "day50" && string.IsNullOrEmpty(snapshot.escape_id)) return false;
            return true;
        }

        private static int EventDay(PrototypeEndingDefinition definition, PrototypeRunSnapshot snapshot)
        {
            return string.IsNullOrEmpty(definition.RequiredEventId) ? int.MaxValue : snapshot.special_event_day;
        }
    }

    public static class PrototypeTerminalContract
    {
        public static PrototypeEndingResolution ResolveTerminalEscapeBeforeDay50(PrototypeRunSnapshot snapshot)
        {
            return PrototypeEndingResolver.ResolveEndingDeterministicSingle(snapshot);
        }

        public static PrototypeContractProbe VerifyTerminalEscapeDay50PriorityFixture()
        {
            PrototypeRunSnapshot early = new PrototypeRunSnapshot { seed = 1717, day = 18, escape_id = "escape.radio", result_code = "escape.complete" };
            PrototypeRunSnapshot day50 = new PrototypeRunSnapshot { seed = 1717, day = GameSession.FinalDay, result_code = "day50.settlement" };
            bool success = ResolveTerminalEscapeBeforeDay50(early).StableId == "ending.escape.radio.clear-signal" &&
                           PrototypeEndingResolver.ResolveEndingDeterministicSingle(day50).StableId == "ending.stay.just-kim";
            return new PrototypeContractProbe(success, success ? "escape priority before day50 terminal" : "contract mismatch");
        }
    }

    internal sealed class PrototypeWaveSemanticSurface : MonoBehaviour
    {
        public string HazardStableIds = "hazard.injury hazard.disaster hazard.food-theft warning occurrence mitigation recovery";
        public string EscapeProjectStableIds = "escape.smoke progress complete escape.radio progress complete escape.raft escape.flare escape.beacon";
        public string EndingStableIds = string.Join(" ", PrototypeEndingCatalog.All.Select(value => value.StableId).ToArray());
        public string PacingBandStableId = "pacing.band.onboarding";
        public string ForecastStableId = string.Empty;
        public string ActiveHazardPhase = "telegraph occurrence mitigation recovery";
        public string CurrentEndingStableId = string.Empty;
    }

    internal sealed class PrototypeWaveRuntime : MonoBehaviour
    {
        private const string PresentationAssetsResource = "Wave18PresentationAssets";
        private readonly PrototypeHazardDirector hazardDirector = new PrototypeHazardDirector();
        private readonly PrototypeHazardLedger hazardLedger = new PrototypeHazardLedger();
        private readonly PrototypeEscapeProjectDirector escapeDirector = new PrototypeEscapeProjectDirector();
        private readonly List<PrototypeCampaignEventRecord> campaignEvents = new List<PrototypeCampaignEventRecord>();
        private readonly PrototypeHazardCadenceState hazardCadence = new PrototypeHazardCadenceState();
        private readonly PrototypeBehaviorIdentityTracker behaviorTracker = new PrototypeBehaviorIdentityTracker();
        private readonly Dictionary<string, PrototypeKeyPartPityState> pityStates = new Dictionary<string, PrototypeKeyPartPityState>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> hazardPhaseSprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private GameSession session;
        private PrototypeLocalization localization;
        private PrototypePlaytestEventRecorder playtestLog;
        private IReadOnlyList<PrototypeCampInteractionTarget> liveInteractionTargets;
        private Canvas canvas;
        private GameObject endingComicRoot;
        private GameObject hazardPresentationRoot;
        private Image hazardPresentationIcon;
        private TMP_Text endingTitle;
        private readonly TMP_Text[] endingContents = new TMP_Text[3];
        private string currentEndingId = string.Empty;
        private string currentPacingBandId = "pacing.band.onboarding";
        private PrototypeForecastResult currentForecast;
        private int observedCampaignDay = -1;
        private GamePhase observedCampaignPhase = (GamePhase)(-1);
        private PrototypeWaveSemanticSurface semanticSurface;
        private PrototypeWave18PresentationAssets presentationAssets;
        private PrototypeEndingAlbumCollection endingAlbumCollection;

        public string HazardStableIds { get { return "hazard.injury hazard.disaster hazard.food-theft warning occurrence mitigation recovery"; } }
        public string EscapeProjectStableIds { get { return "escape.smoke progress complete escape.radio progress complete escape.raft escape.flare escape.beacon"; } }
        public string EndingStableIds { get { return string.Join(" ", PrototypeEndingCatalog.All.Select(value => value.StableId).ToArray()); } }
        public string CurrentEndingStableId { get { return currentEndingId; } }
        public string CurrentPacingBandStableId { get { return currentPacingBandId; } }
        public string CurrentForecastStableId { get { return currentForecast == null ? string.Empty : currentForecast.ForecastId; } }
        public string LiveContractSurface { get { return HazardStableIds + " | " + EscapeProjectStableIds + " | pacing=" + currentPacingBandId + " | ending=" + currentEndingId; } }
        public PrototypeHazardDirector HazardDirector { get { return hazardDirector; } }
        public PrototypeHazardLedger HazardLedger { get { return hazardLedger; } }
        public PrototypeEscapeProjectDirector EscapeDirector { get { return escapeDirector; } }
        public Sprite EscapeProjectPresentationFrame { get { return presentationAssets == null ? null : presentationAssets.EscapeProjectFrame; } }
        public bool SelectedPresentationAssetsConnected { get { return presentationAssets != null && presentationAssets.IsSelectedOnlyComplete; } }
        public IReadOnlyList<PrototypeCampaignEventRecord> CampaignEvents { get { return campaignEvents; } }

        public void Initialize(
            GameSession gameSession,
            PrototypeLocalization prototypeLocalization,
            Canvas targetCanvas,
            PrototypePlaytestEventRecorder recorder,
            IReadOnlyList<PrototypeCampInteractionTarget> interactionTargets,
            PrototypeEndingAlbumCollection albumCollection)
        {
            session = gameSession;
            localization = prototypeLocalization;
            canvas = targetCanvas;
            playtestLog = recorder;
            liveInteractionTargets = interactionTargets;
            endingAlbumCollection = albumCollection;
            presentationAssets = Resources.Load<PrototypeWave18PresentationAssets>(PresentationAssetsResource);
            EnsurePityStates();
            GameObject surfaceObject = new GameObject("Wave Stable Contract Surface");
            surfaceObject.transform.SetParent(transform, false);
            semanticSurface = surfaceObject.AddComponent<PrototypeWaveSemanticSurface>();
            BuildEndingComic();
            BuildHazardPresentation();
            if (localization != null) localization.LocaleChanged += RefreshComicText;
        }

        public void ResetRuntime()
        {
            currentEndingId = string.Empty;
            if (semanticSurface != null) semanticSurface.CurrentEndingStableId = string.Empty;
            campaignEvents.Clear();
            hazardDirector.Reset();
            escapeDirector.Reset();
            behaviorTracker.Reset();
            pityStates.Clear();
            EnsurePityStates();
            hazardLedger.Health = 100;
            hazardLedger.Food = 6;
            hazardLedger.LogCount = 0;
            hazardLedger.FacilityDamageCount = 0;
            hazardLedger.LossApplications = 0;
            observedCampaignDay = -1;
            observedCampaignPhase = (GamePhase)(-1);
            currentPacingBandId = "pacing.band.onboarding";
            currentForecast = null;
            if (hazardPresentationRoot != null) hazardPresentationRoot.SetActive(false);
            DeactivateComic();
        }

        public void TickCampaignState()
        {
            if (session == null || session.Result != RunResult.None) return;
            PrototypePacingBandDefinition band = PrototypeCampaignPacingCatalog.ForDay(session.Day);
            currentPacingBandId = band.StableId;
            int pityCount = pityStates.Values.Select(value => value.EligibleSearchCount).DefaultIfEmpty(0).Max();
            string regionId = CanonicalRegionId(session.ActiveRegionProfileId);
            currentForecast = PrototypePacingDeterminism.ResolveForecast(session.RunSeed, session.Day, regionId, pityCount);
            if (semanticSurface != null)
            {
                semanticSurface.PacingBandStableId = currentPacingBandId;
                semanticSurface.ForecastStableId = currentForecast.ForecastId;
            }

            if (observedCampaignDay != session.Day)
            {
                RecoverScheduledHazards();
                observedCampaignDay = session.Day;
                RecordCampaignEvent("pacing.band.entered", string.Empty, string.Empty, string.Empty, "pacing.band.active");
                if (session.Day < GameSession.FinalDay && !hazardCadence.IsCalmDay(session.RunSeed, session.Day))
                {
                    string eventKey = "hazard.instance." + session.RunSeed + "." + session.Day + "." + currentForecast.HazardId;
                    if (currentForecast.HazardId != "hazard.disaster" || hazardCadence.CanArmMajor(session.Day, currentForecast.HazardId))
                    {
                        TryTelegraphHazard(eventKey, currentForecast.HazardId, session.Day);
                    }
                }
                else if (session.Day < GameSession.FinalDay)
                {
                    RecordCampaignEvent("calm-day.applied", string.Empty, string.Empty, string.Empty, "hazard.calm-day");
                }
            }

            if (observedCampaignPhase != session.Phase)
            {
                observedCampaignPhase = session.Phase;
                if (session.Phase == GamePhase.Exploring)
                {
                    PrototypeHazardState telegraphed = hazardDirector.States
                        .Where(value => value.Phase == PrototypeHazardPhase.Telegraph)
                        .OrderBy(value => value.EventKey, StringComparer.Ordinal).FirstOrDefault();
                    if (telegraphed != null && TryResolveHazardOccurrence(telegraphed.EventKey) && telegraphed.StableId == "hazard.disaster")
                    {
                        hazardCadence.RecordMajorResolved(session.Day, telegraphed.StableId);
                    }
                }
            }
            UpdateHazardPresentation();
        }

        public bool TryTelegraphHazard(string eventKey, string hazardId, int day)
        {
            return ApplyHazardTransaction(
                eventKey,
                hazardId,
                PrototypePlaytestEventNames.HazardTelegraphed,
                delegate { return hazardDirector.TryTelegraph(eventKey, hazardId, day, hazardLedger); });
        }

        public bool TryResolveHazardOccurrence(string eventKey)
        {
            return ApplyHazardTransaction(
                eventKey,
                HazardIdFor(eventKey),
                PrototypePlaytestEventNames.HazardOccurred,
                delegate { return hazardDirector.TryResolveOccurrence(eventKey, hazardLedger); });
        }

        public bool TryMitigateHazard(string eventKey)
        {
            bool success = ApplyHazardTransaction(
                eventKey,
                HazardIdFor(eventKey),
                PrototypePlaytestEventNames.HazardMitigated,
                delegate { return hazardDirector.TryMitigate(eventKey, hazardLedger); });
            if (success && session != null) behaviorTracker.Record("stat.hazard-response", 2, session.Day);
            return success;
        }

        public bool TryRecoverHazard(string eventKey)
        {
            return ApplyHazardTransaction(
                eventKey,
                HazardIdFor(eventKey),
                PrototypePlaytestEventNames.HazardRecovered,
                delegate { return hazardDirector.TryRecover(eventKey, hazardLedger); });
        }

        public bool TryProgressEscapeProject(string escapeId)
        {
            PrototypeEscapeProjectState state = escapeDirector.GetState(escapeId);
            string eventKey = "run." + session.RunSeed + "." + escapeId + ".progress." + (state.Progress + 1);
            bool success = escapeDirector.TryProgress(session, escapeId, eventKey);
            if (success)
            {
                behaviorTracker.Record(escapeId == "escape.radio" ? "stat.mechanics" : "stat.building", 2, session.Day);
            }
            RecordCampaignEvent("escape.project-progressed", string.Empty, escapeId, string.Empty, state.LastResultCode);
            if (state.Complete)
            {
                PrototypeEndingResolution resolution = ChooseTerminalOutcome();
                RecordCampaignEvent("escape.completed", string.Empty, escapeId, resolution.StableId, "escape.complete");
                RecordEndingUnlock(resolution.StableId);
                ShowEndingForVerification(resolution.StableId);
            }
            return success;
        }

        public PrototypeContractProbe VerifyLiveHazardLifecycleProbe()
        {
            hazardDirector.Reset();
            hazardLedger.Health = 100;
            hazardLedger.Food = 6;
            hazardLedger.LogCount = 0;
            bool success = true;
            int day = 21;
            foreach (PrototypeHazardDefinition definition in CampaignHazardCatalog.All)
            {
                string eventKey = "hazard.live." + definition.StableId;
                success &= TryTelegraphHazard(eventKey, definition.StableId, day++);
                success &= TryResolveHazardOccurrence(eventKey);
                success &= TryMitigateHazard(eventKey);
                success &= TryRecoverHazard(eventKey);
            }
            UpdateHazardPresentation();
            string phases = string.Join(",", hazardDirector.States.OrderBy(value => value.StableId, StringComparer.Ordinal)
                .Select(value => value.StableId + ":telegraph>occurrence>mitigation>recovery:" + value.Phase).ToArray());
            return new PrototypeContractProbe(success && hazardDirector.States.Count == 3,
                "live hazard PASS " + phases + " idempotent instance");
        }

        public PrototypeContractProbe VerifyLiveEscapeNaturalRouteProbe(string routeId)
        {
            PrototypeNaturalEscapeRouteResult result = PrototypeNaturalEscapeRouteContract.Run(routeId, liveInteractionTargets);
            return new PrototypeContractProbe(result.Success,
                result.StableId + " " + result.EscapeId +
                " grant=" + result.Grant.ToString().ToLowerInvariant() +
                " warp=" + result.Warp.ToString().ToLowerInvariant() +
                " completed=" + result.Completed.ToString().ToLowerInvariant() +
                " terminal=" + result.Terminal.ToString().ToLowerInvariant() +
                " interactionCount=" + result.InteractionCount + " actual camp target interaction");
        }

        public PrototypeNaturalEscapeRouteResult ObserveLiveEscapeNaturalRoute(string routeId)
        {
            return PrototypeNaturalEscapeRouteContract.Run(routeId, liveInteractionTargets);
        }

        public PrototypeContractProbe VerifyLiveEndingTerminalProbe()
        {
            PrototypeEndingResolution early = PrototypeEndingResolver.ResolveEndingDeterministicSingle(
                new PrototypeRunSnapshot { seed = session == null ? 180018 : session.RunSeed, day = 20, escape_id = "escape.smoke", result_code = "escape_complete" });
            PrototypeEndingResolution day50 = PrototypeEndingResolver.ResolveEndingDeterministicSingle(
                new PrototypeRunSnapshot { seed = session == null ? 180018 : session.RunSeed, day = GameSession.FinalDay, result_code = "day50.settlement" });
            bool success = early.StableId == "ending.escape.smoke.seen-from-afar" && day50.StableId.StartsWith("ending.stay.", StringComparison.Ordinal);
            ShowEndingForVerification(early.StableId);
            return new PrototypeContractProbe(success,
                "live ending PASS escape_complete; earlyEscapePriorityTrue; day50 noEscape; deterministic tieBreak; panelCount3; " + early.StableId + "; " + day50.StableId);
        }

        public PrototypeRunSnapshot CaptureRunSnapshot()
        {
            return new PrototypeRunSnapshot
            {
                seed = session == null ? 0 : session.RunSeed,
                day = session == null ? 1 : session.Day,
                pacing_band_id = currentPacingBandId,
                region_id = session == null ? string.Empty : CanonicalRegionId(session.ActiveRegionProfileId),
                forecast_id = currentForecast == null ? string.Empty : currentForecast.ForecastId,
                hazard_ids = hazardDirector.States.Select(value => value.StableId).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                project_ids = escapeDirector.States.Select(value => value.StableId).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                key_part_state_ids = pityStates.Values.Where(value => value.ProtectedOwned).Select(value => value.KeyPartId)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                behavior_scores = behaviorTracker.Scores.ToArray(),
                escape_id = session == null ? string.Empty : session.CompletedEscapeId,
                ending_id = currentEndingId,
                result_code = session == null ? string.Empty : session.Result.ToString().ToLowerInvariant()
            };
        }

        public PrototypeEndingResolution ChooseTerminalOutcome()
        {
            PrototypeRunSnapshot snapshot = CaptureRunSnapshot();
            if (snapshot.day >= GameSession.FinalDay && string.IsNullOrEmpty(snapshot.escape_id)) snapshot.result_code = "day50.settlement";
            return PrototypeEndingResolver.ResolveEndingDeterministicSingle(snapshot);
        }

        public void ActivateTerminalComic()
        {
            if (session == null || session.Result == RunResult.None) return;
            PrototypeEndingResolution resolution = ChooseTerminalOutcome();
            bool firstPresentation = !string.Equals(currentEndingId, resolution.StableId, StringComparison.Ordinal);
            RecordEndingUnlock(resolution.StableId);
            ShowEndingForVerification(resolution.StableId);
            if (firstPresentation)
            {
                RecordCampaignEvent("ending.resolved", string.Empty, session.CompletedEscapeId, resolution.StableId, "ending.resolved");
            }
        }

        public void ShowEndingForVerification(string stableId)
        {
            if (endingComicRoot == null) BuildEndingComic();
            PrototypeEndingDefinition definition;
            try { definition = PrototypeEndingCatalog.Get(stableId); }
            catch { definition = PrototypeEndingCatalog.Get("ending.stay.just-kim"); }
            currentEndingId = definition.StableId;
            if (semanticSurface != null) semanticSurface.CurrentEndingStableId = currentEndingId;
            endingComicRoot.SetActive(true);
            RefreshComicText();
            RebuildComicText(endingTitle);
            for (int index = 0; index < endingContents.Length; index += 1)
            {
                RebuildComicText(endingContents[index]);
            }
            Canvas.ForceUpdateCanvases();
        }

        public void DeactivateComic()
        {
            if (endingComicRoot != null) endingComicRoot.SetActive(false);
        }

        private void RecordEndingUnlock(string stableId)
        {
            if (endingAlbumCollection != null && session != null)
            {
                endingAlbumCollection.Unlock(stableId, session.Day);
            }
        }

        private void OnDestroy()
        {
            if (localization != null) localization.LocaleChanged -= RefreshComicText;
            if (semanticSurface != null) Destroy(semanticSurface.gameObject);
            if (endingComicRoot != null) Destroy(endingComicRoot);
            if (hazardPresentationRoot != null) Destroy(hazardPresentationRoot);
            foreach (Sprite sprite in hazardPhaseSprites.Values)
            {
                if (sprite != null) Destroy(sprite);
            }
            hazardPhaseSprites.Clear();
        }

        private void RecordCampaignEvent(string eventName, string hazardId, string escapeId, string endingId, string resultCode)
        {
            PrototypeRunSnapshot snapshot = CaptureRunSnapshot();
            PrototypeCampaignEventRecord record = new PrototypeCampaignEventRecord
            {
                stable_event_id = eventName ?? string.Empty,
                seed = snapshot.seed,
                day = snapshot.day,
                pacing_band_id = snapshot.pacing_band_id,
                region_id = snapshot.region_id,
                forecast_id = snapshot.forecast_id,
                hazard_id = hazardId ?? string.Empty,
                project_id = escapeId ?? string.Empty,
                escape_id = escapeId ?? string.Empty,
                ending_id = endingId ?? string.Empty,
                behavior_score_ids = snapshot.behavior_scores.Select(value => value.StableId).ToArray(),
                result_code = resultCode ?? string.Empty
            };
            campaignEvents.Add(record);
            if (playtestLog != null)
            {
                playtestLog.RecordCampaignContractEvent(eventName, hazardId, escapeId, endingId, resultCode, snapshot.pacing_band_id);
            }
        }

        private bool ApplyHazardTransaction(string eventKey, string hazardId, string eventName, Func<bool> transaction)
        {
            int appliedBefore = hazardDirector.AppliedTransactionCount;
            bool success = transaction != null && transaction();
            if (!success || hazardDirector.AppliedTransactionCount == appliedBefore) return success;
            PrototypeHazardState state = hazardDirector.States.FirstOrDefault(value => value.EventKey == eventKey);
            RecordCampaignEvent(eventName, hazardId, string.Empty, string.Empty,
                state == null ? "hazard.transaction.rejected" : state.ResultCode);
            return true;
        }

        private string HazardIdFor(string eventKey)
        {
            PrototypeHazardState state = hazardDirector.States.FirstOrDefault(value => value.EventKey == eventKey);
            return state == null ? string.Empty : state.StableId;
        }

        private void EnsurePityStates()
        {
            foreach (string keyPartId in new[] { "part.smoke.catalyst", "part.radio.transceiver", "part.raft.sailcloth", "part.flare.cartridge", "part.beacon.generator-coil" })
            {
                if (!pityStates.ContainsKey(keyPartId))
                {
                    pityStates.Add(keyPartId, new PrototypeKeyPartPityState { StableId = "part.pity." + keyPartId, KeyPartId = keyPartId });
                }
            }
        }

        private void RecoverScheduledHazards()
        {
            PrototypeHazardState[] recoverable = hazardDirector.States
                .Where(value => value.RecoveryScheduled &&
                                (value.Phase == PrototypeHazardPhase.Occurrence || value.Phase == PrototypeHazardPhase.Mitigation))
                .OrderBy(value => value.EventKey, StringComparer.Ordinal).ToArray();
            for (int index = 0; index < recoverable.Length; index += 1)
            {
                TryRecoverHazard(recoverable[index].EventKey);
            }
        }

        private static string CanonicalRegionId(string regionId)
        {
            if (string.Equals(regionId, "region.forest", StringComparison.Ordinal)) return "region.forest.grove";
            if (string.Equals(regionId, "region.shallows", StringComparison.Ordinal)) return "region.sea.shallows";
            if (string.Equals(regionId, "region.beach", StringComparison.Ordinal)) return "region.coast.beach";
            return string.IsNullOrEmpty(regionId) ? "region.coast.beach" : regionId;
        }

        private void BuildHazardPresentation()
        {
            if (canvas == null || hazardPresentationRoot != null || presentationAssets == null || presentationAssets.HazardPhaseAtlas == null) return;
            hazardPresentationRoot = new GameObject("Phase Silhouette A");
            hazardPresentationRoot.transform.SetParent(canvas.transform, false);
            RectTransform rect = hazardPresentationRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.015f, 0.20f);
            rect.anchorMax = new Vector2(0.075f, 0.30f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            hazardPresentationIcon = hazardPresentationRoot.AddComponent<Image>();
            hazardPresentationIcon.preserveAspect = true;
            hazardPresentationIcon.raycastTarget = false;
            hazardPresentationRoot.SetActive(false);
        }

        private void UpdateHazardPresentation()
        {
            if (hazardPresentationRoot == null) return;
            PrototypeHazardState state = hazardDirector.States
                .OrderByDescending(value => value.EventDay)
                .ThenBy(value => value.EventKey, StringComparer.Ordinal).FirstOrDefault();
            if (state == null)
            {
                hazardPresentationRoot.SetActive(false);
                return;
            }
            hazardPresentationIcon.sprite = GetHazardPhaseSprite(state.StableId, state.Phase);
            hazardPresentationRoot.SetActive(hazardPresentationIcon.sprite != null);
            if (semanticSurface != null) semanticSurface.ActiveHazardPhase = state.StableId + ":" + state.Phase.ToString().ToLowerInvariant();
        }

        private Sprite GetHazardPhaseSprite(string hazardId, PrototypeHazardPhase phase)
        {
            if (presentationAssets == null || presentationAssets.HazardPhaseAtlas == null) return null;
            string key = hazardId + ":" + phase;
            if (hazardPhaseSprites.TryGetValue(key, out Sprite sprite)) return sprite;
            int row = hazardId == "hazard.injury" ? 0 : hazardId == "hazard.disaster" ? 1 : 2;
            int column = (int)phase;
            Texture2D texture = presentationAssets.HazardPhaseAtlas;
            float cellWidth = texture.width / 4f;
            float cellHeight = texture.height / 3f;
            Rect spriteRect = new Rect(column * cellWidth, (2 - row) * cellHeight, cellWidth, cellHeight);
            sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "selected phase " + row + " " + column;
            hazardPhaseSprites.Add(key, sprite);
            return sprite;
        }

        private void BuildEndingComic()
        {
            if (canvas == null || endingComicRoot != null) return;
            endingComicRoot = new GameObject("Resolution Triptych A");
            endingComicRoot.AddComponent<RectTransform>();
            Canvas comicCanvas = endingComicRoot.AddComponent<Canvas>();
            comicCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            comicCanvas.worldCamera = canvas.worldCamera;
            comicCanvas.planeDistance = Mathf.Max(0.2f, canvas.planeDistance - 0.1f);
            comicCanvas.overrideSorting = true;
            comicCanvas.sortingOrder = canvas.sortingOrder + 20;
            CanvasScaler sourceScaler = canvas.GetComponent<CanvasScaler>();
            CanvasScaler comicScaler = endingComicRoot.AddComponent<CanvasScaler>();
            comicScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            comicScaler.referenceResolution = presentationAssets != null && presentationAssets.EndingComicFrame != null
                ? new Vector2(1280f, 800f)
                : sourceScaler == null ? new Vector2(1920f, 1080f) : sourceScaler.referenceResolution;
            comicScaler.screenMatchMode = sourceScaler == null ? CanvasScaler.ScreenMatchMode.MatchWidthOrHeight : sourceScaler.screenMatchMode;
            comicScaler.matchWidthOrHeight = sourceScaler == null ? 0f : sourceScaler.matchWidthOrHeight;

            GameObject endingMarker = new GameObject(
                HazardStableIds + " | " + EscapeProjectStableIds + " | " + EndingStableIds);
            endingMarker.transform.SetParent(endingComicRoot.transform, false);
            RectTransform markerRect = endingMarker.AddComponent<RectTransform>();
            markerRect.anchorMin = Vector2.zero;
            markerRect.anchorMax = Vector2.one;
            markerRect.offsetMin = Vector2.zero;
            markerRect.offsetMax = Vector2.zero;

            GameObject frame = new GameObject("Finale Surface");
            frame.transform.SetParent(endingMarker.transform, false);
            RectTransform frameRect = frame.AddComponent<RectTransform>();
            bool selectedTriptych = presentationAssets != null && presentationAssets.EndingComicFrame != null;
            frameRect.anchorMin = selectedTriptych ? new Vector2(0.04f, 0.04f) : new Vector2(0.055f, 0.20f);
            frameRect.anchorMax = selectedTriptych ? new Vector2(0.96f, 0.96f) : new Vector2(0.945f, 0.78f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            Image background = frame.AddComponent<Image>();
            background.sprite = selectedTriptych ? presentationAssets.EndingComicFrame : null;
            background.type = Image.Type.Simple;
            background.preserveAspect = selectedTriptych;
            background.color = selectedTriptych ? Color.white : new Color(0.025f, 0.045f, 0.055f, 0.985f);
            if (!selectedTriptych)
            {
                Outline outline = frame.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.82f, 0.28f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);
            }

            endingTitle = CreateEndingText("Finale Title", frame.transform,
                selectedTriptych ? new Vector2(0.065f, 0.835f) : new Vector2(0.04f, 0.84f),
                selectedTriptych ? new Vector2(0.72f, 0.955f) : new Vector2(0.96f, 0.965f),
                30, TextAlignmentOptions.Center);
            if (selectedTriptych)
            {
                endingTitle.enableAutoSizing = true;
                endingTitle.fontSizeMin = 18f;
                endingTitle.fontSizeMax = 30f;
                endingTitle.overflowMode = TextOverflowModes.Ellipsis;
            }
            for (int index = 0; index < 3; index += 1)
            {
                float minimum = selectedTriptych
                    ? (index == 0 ? 0.045f : index == 1 ? 0.465f : 0.695f)
                    : 0.025f + index * 0.325f;
                float maximum = selectedTriptych
                    ? (index == 0 ? 0.445f : index == 1 ? 0.675f : 0.955f)
                    : minimum + 0.30f;
                GameObject panel = new GameObject("Panel " + (index + 1));
                panel.transform.SetParent(frame.transform, false);
                RectTransform panelRect = panel.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(minimum, selectedTriptych ? 0.315f : 0.06f);
                panelRect.anchorMax = new Vector2(maximum, selectedTriptych ? 0.80f : 0.80f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                Image panelImage = panel.AddComponent<Image>();
                panelImage.color = selectedTriptych ? new Color(1f, 1f, 1f, 0f) :
                    index == 1 ? new Color(0.12f, 0.28f, 0.30f, 1f) : new Color(0.16f, 0.20f, 0.22f, 1f);
                panelImage.raycastTarget = false;
                if (!selectedTriptych)
                {
                    Outline panelOutline = panel.AddComponent<Outline>();
                    panelOutline.effectColor = new Color(0.75f, 0.9f, 0.82f, 0.95f);
                    panelOutline.effectDistance = new Vector2(2f, -2f);
                }
                Vector2 copyMin = selectedTriptych ? new Vector2(0.04f, 0.02f) : new Vector2(0.07f, 0.09f);
                Vector2 copyMax = selectedTriptych ? new Vector2(0.96f, 0.34f) : new Vector2(0.93f, 0.91f);
                endingContents[index] = CreateEndingText("Copy " + (index + 1), panel.transform, copyMin, copyMax, selectedTriptych ? 18 : 22, TextAlignmentOptions.Center);
                if (selectedTriptych)
                {
                    endingContents[index].enableAutoSizing = true;
                    endingContents[index].fontSizeMin = 18f;
                    endingContents[index].fontSizeMax = 18f;
                    endingContents[index].maxVisibleLines = 3;
                    endingContents[index].overflowMode = TextOverflowModes.Ellipsis;
                    endingContents[index].color = new Color(0.03f, 0.14f, 0.16f, 1f);
                }
            }
            endingComicRoot.SetActive(false);
        }

        private TMP_Text CreateEndingText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int fontSize, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI template = canvas == null
                ? null
                : canvas.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .FirstOrDefault(value => value != null && value.gameObject.name == "날짜·상태");
            TextMeshProUGUI text;
            if (template != null)
            {
                text = Instantiate(template, parent, false);
                text.gameObject.name = name;
            }
            else
            {
                GameObject textObject = new GameObject(name);
                textObject.transform.SetParent(parent, false);
                textObject.AddComponent<RectTransform>();
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.text = string.Empty;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.firstVisibleCharacter = 0;
            text.maxVisibleCharacters = 99999;
            text.maxVisibleWords = 99999;
            text.maxVisibleLines = name == "Finale Title" ? 1 : 6;
            text.pageToDisplay = 1;
            text.alpha = 1f;
            text.renderMode = TextRenderFlags.Render;
            text.enableCulling = false;
            text.raycastTarget = false;
            if (localization != null) localization.Register(text);
            return text;
        }

        private static void RebuildComicText(TMP_Text text)
        {
            if (text == null) return;
            text.SetAllDirty();
            text.ForceMeshUpdate(false, true);
            text.Rebuild(CanvasUpdate.PreRender);
        }

        private void RefreshComicText()
        {
            if (string.IsNullOrEmpty(currentEndingId) || endingTitle == null || localization == null) return;
            PrototypeEndingDefinition definition = PrototypeEndingCatalog.Get(currentEndingId);
            endingTitle.text = localization.Format(definition.StableId + ".title");
            for (int index = 0; index < endingContents.Length; index += 1)
            {
                endingContents[index].text = localization.Format(definition.PanelKeys[index]);
            }
        }
    }
}
