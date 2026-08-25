using System;
using System.Collections.Generic;
using System.Linq;

namespace KimSurvival
{
    public enum PrototypeDiseasePhase
    {
        Clear,
        Telegraphed,
        Exposed,
        Symptomatic,
        Aggravated,
        Recovering,
        Cleared
    }

    [Serializable]
    public sealed class PrototypeDiseaseSnapshot
    {
        public int RunSeed;
        public string ConditionId = PrototypeDiseaseRuntime.StableId;
        public string StageId = string.Empty;
        public string StableId = PrototypeDiseaseRuntime.StableId;
        public PrototypeDiseasePhase Phase;
        public string RegionId = string.Empty;
        public string NodeId = string.Empty;
        public string[] ExposedNodeIds = Array.Empty<string>();
        public string[] UniqueExposureNodeIds = Array.Empty<string>();
        public int ExposureCount;
        public int ExposureApplyCount;
        public int EffectCount;
        public int WorsenCount;
        public int ForcedReturnCount;
        public int TreatmentAttemptCount;
        public int TreatmentPaidCount;
        public int HealthDeltaTotal;
        public int SettlementCount;
        public int Severity;
        public int LastSettlementDay = -1;
        public int LastTransitionDay = -1;
        public int TreatmentCommittedDay = -1;
        public bool TreatmentCommitted;
        public bool NewExposureSinceTreatment;
        public string LastResultCode = string.Empty;
        public string LastTransactionId = string.Empty;
        public string[] CommittedTransactionIds = Array.Empty<string>();
        public string TreatmentResult = string.Empty;
        public string[] Trace = Array.Empty<string>();

        public PrototypeDiseaseSnapshot Clone()
        {
            return new PrototypeDiseaseSnapshot
            {
                RunSeed = RunSeed,
                ConditionId = ConditionId,
                StageId = StageId,
                StableId = StableId,
                Phase = Phase,
                RegionId = RegionId,
                NodeId = NodeId,
                ExposedNodeIds = ExposedNodeIds == null ? Array.Empty<string>() : ExposedNodeIds.ToArray(),
                UniqueExposureNodeIds = UniqueExposureNodeIds == null ? Array.Empty<string>() : UniqueExposureNodeIds.ToArray(),
                ExposureCount = ExposureCount,
                ExposureApplyCount = ExposureApplyCount,
                EffectCount = EffectCount,
                WorsenCount = WorsenCount,
                ForcedReturnCount = ForcedReturnCount,
                TreatmentAttemptCount = TreatmentAttemptCount,
                TreatmentPaidCount = TreatmentPaidCount,
                HealthDeltaTotal = HealthDeltaTotal,
                SettlementCount = SettlementCount,
                Severity = Severity,
                LastSettlementDay = LastSettlementDay,
                LastTransitionDay = LastTransitionDay,
                TreatmentCommittedDay = TreatmentCommittedDay,
                TreatmentCommitted = TreatmentCommitted,
                NewExposureSinceTreatment = NewExposureSinceTreatment,
                LastResultCode = LastResultCode,
                LastTransactionId = LastTransactionId,
                CommittedTransactionIds = CommittedTransactionIds == null
                    ? Array.Empty<string>()
                    : CommittedTransactionIds.ToArray(),
                TreatmentResult = TreatmentResult,
                Trace = Trace == null ? Array.Empty<string>() : Trace.ToArray()
            };
        }
    }

    /// <summary>
    /// GAME JAM jungle-fever lifecycle. Two distinct forest deadfalls create the
    /// condition; camp entry applies -10 once, untreated settlement applies -15,
    /// and one naturally collected medicine atomically enters recovery.
    /// </summary>
    public sealed class PrototypeDiseaseRuntime
    {
        public const string StableId = "hazard-profile.disease.jungle-fever";
        public const string TriggerHazardId = "hazard.disease";
        public const string MedicineResourceId = "resource.medicine";
        public const int RequiredUniqueExposureCount = 2;
        public const int SymptomHealthDelta = -10;
        public const int AggravationHealthDelta = -15;
        public const int RecoveryHealthDelta = 5;
        public const int TreatmentMedicineCost = 1;
        public const int SymptomaticSearchEnergyPenalty = 2;
        public const int AggravatedSearchEnergyPenalty = 4;
        public const string FailureResultCode = "failure.disease.jungle-fever";
        public const string TelegraphResultCode = "disease.result.telegraphed";
        public const string ExposureRecordedResultCode = "disease.result.exposure-recorded";
        public const string ExposedResultCode = "disease.result.exposed";
        public const string SymptomaticResultCode = "disease.result.symptomatic";
        public const string AggravatedResultCode = "disease.result.aggravated";
        public const string TreatmentCommittedResultCode = "disease.result.treatment-committed";
        public const string RecoveryDelayedResultCode = "disease.result.recovery-delayed-new-exposure";
        public const string ClearedResultCode = "disease.result.cleared";
        public const string PartialExposureDecayedResultCode = "disease.result.partial-exposure-decayed";

        private readonly List<string> trace = new List<string>();
        private readonly HashSet<string> exposedNodeIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> committedTransactionIds = new HashSet<string>(StringComparer.Ordinal);

        public PrototypeDiseaseRuntime(int runSeed)
        {
            Reset(runSeed);
        }

        public int RunSeed { get; private set; }
        public PrototypeDiseasePhase Phase { get; private set; }
        public string RegionId { get; private set; }
        public string NodeId { get; private set; }
        public int ExposureCount { get { return exposedNodeIds.Count; } }
        public int ExposureApplyCount { get; private set; }
        public int EffectCount { get; private set; }
        public int WorsenCount { get; private set; }
        public int ForcedReturnCount { get; private set; }
        public int TreatmentAttemptCount { get; private set; }
        public int TreatmentPaidCount { get; private set; }
        public int HealthDeltaTotal { get; private set; }
        public int SettlementCount { get; private set; }
        public int Severity { get; private set; }
        public int LastSettlementDay { get; private set; }
        public int LastTransitionDay { get; private set; }
        public int TreatmentCommittedDay { get; private set; }
        public bool TreatmentCommitted { get; private set; }
        public bool NewExposureSinceTreatment { get; private set; }
        public string LastResultCode { get; private set; }
        public string LastTransactionId { get; private set; }
        public string TreatmentResult { get; private set; }
        public IReadOnlyList<string> Trace { get { return trace; } }
        public IReadOnlyCollection<string> ExposedNodeIds { get { return exposedNodeIds; } }
        public IReadOnlyCollection<string> CommittedTransactionIds { get { return committedTransactionIds; } }

        public string ConditionId { get { return StableId; } }
        public string StageId { get { return StageIdFor(Phase); } }

        public bool HasHistory
        {
            get { return Phase != PrototypeDiseasePhase.Clear || trace.Count > 0; }
        }

        public bool IsActive
        {
            get
            {
                return Phase == PrototypeDiseasePhase.Telegraphed ||
                       Phase == PrototypeDiseasePhase.Exposed ||
                       Phase == PrototypeDiseasePhase.Symptomatic ||
                       Phase == PrototypeDiseasePhase.Aggravated ||
                       Phase == PrototypeDiseasePhase.Recovering;
            }
        }

        public bool IsTreatable
        {
            get
            {
                return Phase == PrototypeDiseasePhase.Symptomatic ||
                       Phase == PrototypeDiseasePhase.Aggravated;
            }
        }

        public int ActiveSearchEnergyPenalty
        {
            get
            {
                switch (Phase)
                {
                    case PrototypeDiseasePhase.Symptomatic:
                        return SymptomaticSearchEnergyPenalty;
                    case PrototypeDiseasePhase.Aggravated:
                        return AggravatedSearchEnergyPenalty;
                    case PrototypeDiseasePhase.Recovering:
                        return 1;
                    default:
                        return 0;
                }
            }
        }

        public string PhaseLocalizationKey
        {
            get
            {
                return Phase == PrototypeDiseasePhase.Clear
                    ? "disease.stage.clear"
                    : "disease.stage." + Phase.ToString().ToLowerInvariant();
            }
        }

        public string FeedbackLocalizationKey
        {
            get
            {
                switch (Phase)
                {
                    case PrototypeDiseasePhase.Telegraphed:
                        return StableId + ".telegraph";
                    case PrototypeDiseasePhase.Exposed:
                        return StableId + ".exposed";
                    case PrototypeDiseasePhase.Symptomatic:
                        return StableId + ".symptomatic";
                    case PrototypeDiseasePhase.Aggravated:
                        return StableId + ".aggravated";
                    case PrototypeDiseasePhase.Recovering:
                        return StableId + ".recovering";
                    case PrototypeDiseasePhase.Cleared:
                        return StableId + ".cleared";
                    default:
                        return string.Empty;
                }
            }
        }

        public void Reset(int runSeed)
        {
            RunSeed = runSeed;
            Phase = PrototypeDiseasePhase.Clear;
            RegionId = string.Empty;
            NodeId = string.Empty;
            exposedNodeIds.Clear();
            EffectCount = 0;
            ExposureApplyCount = 0;
            WorsenCount = 0;
            ForcedReturnCount = 0;
            TreatmentAttemptCount = 0;
            TreatmentPaidCount = 0;
            HealthDeltaTotal = 0;
            SettlementCount = 0;
            Severity = 0;
            LastSettlementDay = -1;
            LastTransitionDay = -1;
            TreatmentCommittedDay = -1;
            TreatmentCommitted = false;
            NewExposureSinceTreatment = false;
            LastResultCode = string.Empty;
            LastTransactionId = string.Empty;
            TreatmentResult = string.Empty;
            committedTransactionIds.Clear();
            trace.Clear();
        }

        public static string StageIdFor(PrototypeDiseasePhase phase)
        {
            return "disease.stage." + phase.ToString().ToLowerInvariant();
        }

        public static string TransactionIdFor(int runSeed, string stageId, int day)
        {
            return "disease.tx." + runSeed + "." + StableId + "." +
                   (stageId ?? string.Empty) + "." + day;
        }

        private bool IsTransactionCommitted(string transactionId)
        {
            return !string.IsNullOrWhiteSpace(transactionId) && committedTransactionIds.Contains(transactionId);
        }

        private void CommitTransaction(string transactionId, string resultCode)
        {
            if (!string.IsNullOrWhiteSpace(transactionId)) committedTransactionIds.Add(transactionId);
            LastTransactionId = transactionId ?? string.Empty;
            LastResultCode = resultCode ?? string.Empty;
        }

        public bool TryTelegraph(string regionId, string nodeId)
        {
            if ((Phase != PrototypeDiseasePhase.Clear && Phase != PrototypeDiseasePhase.Telegraphed) ||
                string.IsNullOrWhiteSpace(regionId) || string.IsNullOrWhiteSpace(nodeId))
            {
                return false;
            }
            if (Phase == PrototypeDiseasePhase.Telegraphed)
            {
                return false;
            }

            RegionId = regionId;
            NodeId = nodeId;
            Phase = PrototypeDiseasePhase.Telegraphed;
            LastResultCode = TelegraphResultCode;
            trace.Add("disease.telegraph:" + StableId + ":" + nodeId);
            return true;
        }

        public bool TryExposeFromSearch(PrototypeSearchNodeDefinition definition)
        {
            if (definition == null ||
                !string.Equals(definition.RegionId, "region.forest.grove", StringComparison.Ordinal) ||
                !string.Equals(definition.HazardId, TriggerHazardId, StringComparison.Ordinal) ||
                (Phase != PrototypeDiseasePhase.Clear && Phase != PrototypeDiseasePhase.Telegraphed &&
                 Phase != PrototypeDiseasePhase.Recovering))
            {
                return false;
            }

            bool recoveringExposure = Phase == PrototypeDiseasePhase.Recovering;
            if (Phase == PrototypeDiseasePhase.Clear)
            {
                TryTelegraph(definition.RegionId, definition.NodeId);
            }
            if (!exposedNodeIds.Add(definition.NodeId))
            {
                return false;
            }

            RegionId = definition.RegionId;
            NodeId = definition.NodeId;
            if (recoveringExposure) NewExposureSinceTreatment = true;
            LastResultCode = ExposureRecordedResultCode;
            trace.Add("disease.exposure:" + StableId + ":" + definition.NodeId + ":" +
                      ExposureCount + "/" + RequiredUniqueExposureCount);
            if (ExposureCount >= RequiredUniqueExposureCount)
            {
                Phase = PrototypeDiseasePhase.Exposed;
                Severity = 1;
                ExposureApplyCount += 1;
                TreatmentCommitted = false;
                LastResultCode = ExposedResultCode;
                trace.Add("disease.reexposure:" +
                          (recoveringExposure ? "recovery-interrupted" : "condition-created"));
            }
            return true;
        }

        public bool TryEnterCamp(GameSession session, bool forced)
        {
            if (session == null || session.Phase != GamePhase.Camp || Phase != PrototypeDiseasePhase.Exposed)
            {
                return false;
            }
            int day = session.Day;
            string transactionId = TransactionIdFor(
                RunSeed,
                StageIdFor(PrototypeDiseasePhase.Symptomatic),
                day);
            if (IsTransactionCommitted(transactionId)) return false;
            if (!session.ApplyHealthDelta(transactionId, SymptomHealthDelta)) return false;

            Phase = PrototypeDiseasePhase.Symptomatic;
            LastTransitionDay = day;
            Severity = 1;
            EffectCount += 1;
            HealthDeltaTotal += SymptomHealthDelta;
            if (forced) ForcedReturnCount += 1;
            CommitTransaction(
                transactionId,
                session.Health <= 0 ? FailureResultCode : SymptomaticResultCode);
            trace.Add("disease.effect:symptom=" + SymptomHealthDelta + ":return=" +
                      (forced ? "forced" : "voluntary") + ":day=" + day);
            return true;
        }

        public bool TrySettleDay(GameSession session)
        {
            if (session == null || session.Phase != GamePhase.Camp) return false;
            int day = session.Day;
            if (day <= LastSettlementDay) return false;
            if (Phase == PrototypeDiseasePhase.Telegraphed && ExposureCount < RequiredUniqueExposureCount)
            {
                string transactionId = TransactionIdFor(
                    RunSeed,
                    StageIdFor(PrototypeDiseasePhase.Clear),
                    day);
                if (IsTransactionCommitted(transactionId)) return false;
                exposedNodeIds.Clear();
                Phase = PrototypeDiseasePhase.Clear;
                RegionId = string.Empty;
                NodeId = string.Empty;
                Severity = 0;
                SettlementCount += 1;
                LastSettlementDay = day;
                LastTransitionDay = day;
                CommitTransaction(transactionId, PartialExposureDecayedResultCode);
                trace.Add("disease.exposure-decayed:day=" + day);
                return true;
            }
            if (Phase == PrototypeDiseasePhase.Symptomatic || Phase == PrototypeDiseasePhase.Aggravated)
            {
                if (day <= LastTransitionDay) return false;
                string transactionId = TransactionIdFor(
                    RunSeed,
                    StageIdFor(PrototypeDiseasePhase.Aggravated),
                    day);
                if (IsTransactionCommitted(transactionId)) return false;
                if (!session.ApplyHealthDelta(transactionId, AggravationHealthDelta)) return false;
                Phase = PrototypeDiseasePhase.Aggravated;
                Severity = 2;
                WorsenCount += 1;
                SettlementCount += 1;
                LastSettlementDay = day;
                LastTransitionDay = day;
                HealthDeltaTotal += AggravationHealthDelta;
                CommitTransaction(
                    transactionId,
                    session.Health <= 0 ? FailureResultCode : AggravatedResultCode);
                trace.Add("disease.worsen:settlement=" + AggravationHealthDelta + ":day=" + day);
                return true;
            }
            if (Phase == PrototypeDiseasePhase.Recovering && day > TreatmentCommittedDay)
            {
                if (NewExposureSinceTreatment)
                {
                    string delayedTransactionId = TransactionIdFor(
                        RunSeed,
                        StageIdFor(PrototypeDiseasePhase.Recovering),
                        day);
                    if (IsTransactionCommitted(delayedTransactionId)) return false;

                    exposedNodeIds.Clear();
                    NewExposureSinceTreatment = false;
                    SettlementCount += 1;
                    LastSettlementDay = day;
                    LastTransitionDay = day;
                    CommitTransaction(delayedTransactionId, RecoveryDelayedResultCode);
                    trace.Add("disease.recovery-delayed:new-exposure:day=" + day);
                    return true;
                }

                string transactionId = TransactionIdFor(
                    RunSeed,
                    StageIdFor(PrototypeDiseasePhase.Cleared),
                    day);
                if (IsTransactionCommitted(transactionId)) return false;
                if (!session.ApplyHealthDelta(transactionId, RecoveryHealthDelta)) return false;
                Phase = PrototypeDiseasePhase.Cleared;
                Severity = 0;
                SettlementCount += 1;
                LastSettlementDay = day;
                LastTransitionDay = day;
                HealthDeltaTotal += RecoveryHealthDelta;
                exposedNodeIds.Clear();
                NewExposureSinceTreatment = false;
                CommitTransaction(transactionId, ClearedResultCode);
                trace.Add("disease.cleared:recovery=" + RecoveryHealthDelta + ":day=" + day);
                return true;
            }
            return false;
        }

        public bool CanTreat(GameSession session, bool hasWorkbench)
        {
            return session != null && session.Phase == GamePhase.Camp && hasWorkbench &&
                   IsTreatable && session.GetStableStorage(MedicineResourceId) >= TreatmentMedicineCost;
        }

        public bool TryTreat(GameSession session, bool hasWorkbench)
        {
            if (!CanTreat(session, hasWorkbench)) return false;
            string transactionId = TransactionIdFor(
                RunSeed,
                StageIdFor(PrototypeDiseasePhase.Recovering),
                session.Day);
            if (IsTransactionCommitted(transactionId)) return false;
            if (!session.TrySpendStableResource(MedicineResourceId, TreatmentMedicineCost)) return false;

            TreatmentAttemptCount += 1;
            Phase = PrototypeDiseasePhase.Recovering;
            Severity = 1;
            TreatmentPaidCount += 1;
            TreatmentCommitted = true;
            TreatmentCommittedDay = session.Day;
            LastTransitionDay = session.Day;
            exposedNodeIds.Clear();
            NewExposureSinceTreatment = false;
            TreatmentResult = "committed";
            CommitTransaction(transactionId, TreatmentCommittedResultCode);
            trace.Add("disease.mitigate:medicine=" + TreatmentMedicineCost);
            trace.Add("disease.treat:stage=recovering");
            return true;
        }

        public bool TryCancelTreatment()
        {
            return IsTreatable;
        }

        public PrototypeDiseaseSnapshot CaptureSnapshot()
        {
            return new PrototypeDiseaseSnapshot
            {
                RunSeed = RunSeed,
                ConditionId = ConditionId,
                StageId = StageId,
                StableId = StableId,
                Phase = Phase,
                RegionId = RegionId,
                NodeId = NodeId,
                ExposedNodeIds = exposedNodeIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                UniqueExposureNodeIds = exposedNodeIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                ExposureCount = ExposureCount,
                ExposureApplyCount = ExposureApplyCount,
                EffectCount = EffectCount,
                WorsenCount = WorsenCount,
                ForcedReturnCount = ForcedReturnCount,
                TreatmentAttemptCount = TreatmentAttemptCount,
                TreatmentPaidCount = TreatmentPaidCount,
                HealthDeltaTotal = HealthDeltaTotal,
                SettlementCount = SettlementCount,
                Severity = Severity,
                LastSettlementDay = LastSettlementDay,
                LastTransitionDay = LastTransitionDay,
                TreatmentCommittedDay = TreatmentCommittedDay,
                TreatmentCommitted = TreatmentCommitted,
                NewExposureSinceTreatment = NewExposureSinceTreatment,
                LastResultCode = LastResultCode,
                LastTransactionId = LastTransactionId,
                CommittedTransactionIds = committedTransactionIds
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray(),
                TreatmentResult = TreatmentResult,
                Trace = trace.ToArray()
            };
        }

        public bool RestoreSnapshot(PrototypeDiseaseSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Reset(RunSeed);
                return true;
            }
            string conditionId = string.IsNullOrWhiteSpace(snapshot.ConditionId)
                ? StableId
                : snapshot.ConditionId;
            if (!Enum.IsDefined(typeof(PrototypeDiseasePhase), snapshot.Phase)) return false;
            string expectedStageId = StageIdFor(snapshot.Phase);
            if (snapshot.RunSeed != RunSeed ||
                !string.Equals(conditionId, StableId, StringComparison.Ordinal) ||
                !string.Equals(snapshot.StableId, StableId, StringComparison.Ordinal) ||
                (!string.IsNullOrWhiteSpace(snapshot.StageId) &&
                 !string.Equals(snapshot.StageId, expectedStageId, StringComparison.Ordinal)) ||
                snapshot.ExposureCount < 0 || snapshot.ExposureApplyCount < 0 || snapshot.EffectCount < 0 || snapshot.WorsenCount < 0 ||
                snapshot.ForcedReturnCount < 0 || snapshot.TreatmentAttemptCount < 0 ||
                snapshot.TreatmentPaidCount < 0 || snapshot.SettlementCount < 0 || snapshot.Severity < 0 ||
                snapshot.LastSettlementDay < -1 || snapshot.LastTransitionDay < -1 || snapshot.TreatmentCommittedDay < -1)
            {
                return false;
            }

            string[] legacyExposureNodeIds = snapshot.ExposedNodeIds ?? Array.Empty<string>();
            string[] canonicalExposureNodeIds = snapshot.UniqueExposureNodeIds ?? Array.Empty<string>();
            string[] sourceExposureNodeIds = canonicalExposureNodeIds.Length > 0
                ? canonicalExposureNodeIds
                : legacyExposureNodeIds;
            var restoredExposureNodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string nodeId in sourceExposureNodeIds)
            {
                if (string.IsNullOrWhiteSpace(nodeId) || !restoredExposureNodeIds.Add(nodeId)) return false;
            }
            if (snapshot.ExposureCount != restoredExposureNodeIds.Count) return false;
            if (canonicalExposureNodeIds.Length > 0 && legacyExposureNodeIds.Length > 0)
            {
                var legacySet = new HashSet<string>(legacyExposureNodeIds, StringComparer.Ordinal);
                if (!legacySet.SetEquals(restoredExposureNodeIds)) return false;
            }

            string transactionPrefix = "disease.tx." + RunSeed + "." + StableId + ".";
            string[] snapshotTransactionIds = snapshot.CommittedTransactionIds ?? Array.Empty<string>();
            var restoredTransactionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string transactionId in snapshotTransactionIds)
            {
                if (string.IsNullOrWhiteSpace(transactionId) ||
                    !transactionId.StartsWith(transactionPrefix, StringComparison.Ordinal) ||
                    !restoredTransactionIds.Add(transactionId))
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(snapshot.LastTransactionId) &&
                (!snapshot.LastTransactionId.StartsWith(transactionPrefix, StringComparison.Ordinal) ||
                 !restoredTransactionIds.Contains(snapshot.LastTransactionId)))
            {
                return false;
            }

            Phase = snapshot.Phase;
            RegionId = snapshot.RegionId ?? string.Empty;
            NodeId = snapshot.NodeId ?? string.Empty;
            exposedNodeIds.Clear();
            exposedNodeIds.UnionWith(restoredExposureNodeIds);
            ExposureApplyCount = snapshot.ExposureApplyCount;
            EffectCount = snapshot.EffectCount;
            WorsenCount = snapshot.WorsenCount;
            ForcedReturnCount = snapshot.ForcedReturnCount;
            TreatmentAttemptCount = snapshot.TreatmentAttemptCount;
            TreatmentPaidCount = snapshot.TreatmentPaidCount;
            HealthDeltaTotal = snapshot.HealthDeltaTotal;
            SettlementCount = snapshot.SettlementCount;
            Severity = snapshot.Severity;
            LastSettlementDay = snapshot.LastSettlementDay;
            LastTransitionDay = snapshot.LastTransitionDay;
            TreatmentCommittedDay = snapshot.TreatmentCommittedDay;
            TreatmentCommitted = snapshot.TreatmentCommitted;
            NewExposureSinceTreatment = snapshot.NewExposureSinceTreatment;
            LastResultCode = snapshot.LastResultCode ?? string.Empty;
            LastTransactionId = snapshot.LastTransactionId ?? string.Empty;
            committedTransactionIds.Clear();
            committedTransactionIds.UnionWith(restoredTransactionIds);
            TreatmentResult = snapshot.TreatmentResult ?? string.Empty;
            trace.Clear();
            trace.AddRange(snapshot.Trace ?? Array.Empty<string>());
            return true;
        }
    }

    [Serializable]
    public sealed class PrototypeWaveBPlayObservation
    {
        public int RegionCount;
        public int ArchetypeCount;
        public int NodeInstanceCount;
        public int ExistingInstanceCount;
        public int NewInstanceCount;
        public int RemovedLegacyInstanceCount;
        public int GeneralResourceUnits;
        public int StableResourceKindCount;
        public int ProtectedPartUnits;
        public int DuplicateStableIdCount;
        public bool SameSeedDeterministic;
        public bool DifferentSeedVaries;
        public bool StockDoesNotRegenerate;
        public bool HiddenPartialDepletedPersistent;
        public bool BarrierPersistent;
        public bool PermanentHazardPersistent;
        public bool SailclothProtected;
        public bool BagTransactionAtomic;
        public string DiseaseStableId = PrototypeDiseaseRuntime.StableId;
        public string[] DiseaseTrace = Array.Empty<string>();
        public bool DiseaseTelegraphNatural;
        public bool DiseaseExposureNatural;
        public bool DiseaseEffectApplied;
        public bool DiseaseWorsenedOnSettlement;
        public bool ForcedReturnAtomic;
        public bool TreatmentCancelAtomic;
        public bool TreatmentCostAtomic;
        public bool TreatmentSucceeded;
        public bool KeyboardMouseSyntheticGamepadParity;
        public string[] SearchStateSequence = Array.Empty<string>();
        public string[] SearchStateFingerprints = Array.Empty<string>();
        public string[] SearchInteractionTrace = Array.Empty<string>();
        public string KnownRemainingFingerprint = string.Empty;
        public string[] StockFingerprints = Array.Empty<string>();
        public string[] StockGenerationEvents = Array.Empty<string>();
        public string DiseaseId = PrototypeDiseaseRuntime.StableId;
        public string[] DiseasePhaseSequence = Array.Empty<string>();
        public string[] DiseaseInteractionTrace = Array.Empty<string>();
        public string[] DiseaseStateFingerprints = Array.Empty<string>();
        public int ExposureApplyCount;
        public int EffectApplyCount;
        public int WorsenApplyCount;
        public int TreatmentCostCount;
        public int DuplicateCostDelta;
        public int DuplicateHazardDelta;
        public int CancelContaminationDelta;
        public bool Grant;
        public bool Warp;
        public bool Skip;
        public int GrantCallCount;
        public int WarpCallCount;
        public int SkipCallCount;
        public PrototypePlaytestEventRecord[] ProductionInteractionEvents = Array.Empty<PrototypePlaytestEventRecord>();
        public string KeyboardMeaning = string.Empty;
        public string GamepadMeaning = string.Empty;
        public string[] LocaleStateFingerprints = Array.Empty<string>();
        public PrototypeSearchNodeLayoutObservation[] Layouts = Array.Empty<PrototypeSearchNodeLayoutObservation>();
        public string[] LocalizationKeys = Array.Empty<string>();
        public string ObservationError = string.Empty;
    }
}
