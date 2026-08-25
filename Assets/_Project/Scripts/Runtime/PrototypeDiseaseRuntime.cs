using System;
using System.Collections.Generic;
using System.Linq;

namespace KimSurvival
{
    public enum PrototypeDiseasePhase
    {
        Healthy,
        Telegraph,
        Exposed,
        Effect,
        Worsened,
        Mitigated,
        Recovered
    }

    public static class PrototypeDiseaseConfig
    {
        public const string StableId = "disease.waterborne-fever";
        public const string ExposureHazardId = "hazard.disease";
        public const string TreatmentResourceId = "resource.medicine";
        public const int TreatmentCost = 1;
        public const int EffectVitalityCost = 8;
        public const int WorseningVitalityCost = 7;
        public const int TreatmentVitalityRecovery = 10;
    }

    [Serializable]
    public sealed class PrototypeDiseaseSnapshot
    {
        public int RunSeed;
        public string DiseaseId = PrototypeDiseaseConfig.StableId;
        public string ExposureInstanceId = string.Empty;
        public string SourceRegionId = string.Empty;
        public string SourceNodeId = string.Empty;
        public int ExposureDay;
        public int LastProcessedDay;
        public PrototypeDiseasePhase Phase;
        public int Vitality = 100;
        public int MedicineUnits;
        public bool ForcedReturn;
        public string[] ProcessedTransactionIds = Array.Empty<string>();
        public string[] EventIds = Array.Empty<string>();

        public PrototypeDiseaseSnapshot Clone()
        {
            return new PrototypeDiseaseSnapshot
            {
                RunSeed = RunSeed,
                DiseaseId = DiseaseId,
                ExposureInstanceId = ExposureInstanceId,
                SourceRegionId = SourceRegionId,
                SourceNodeId = SourceNodeId,
                ExposureDay = ExposureDay,
                LastProcessedDay = LastProcessedDay,
                Phase = Phase,
                Vitality = Vitality,
                MedicineUnits = MedicineUnits,
                ForcedReturn = ForcedReturn,
                ProcessedTransactionIds = ProcessedTransactionIds == null
                    ? Array.Empty<string>()
                    : ProcessedTransactionIds.ToArray(),
                EventIds = EventIds == null ? Array.Empty<string>() : EventIds.ToArray()
            };
        }
    }

    public sealed class PrototypeDiseaseRuntime
    {
        private readonly HashSet<string> processedTransactions = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> eventIds = new List<string>();
        private PrototypeDiseaseSnapshot state;

        public PrototypeDiseaseRuntime(int runSeed)
        {
            Reset(runSeed);
        }

        public PrototypeDiseasePhase Phase { get { return state.Phase; } }
        public int Vitality { get { return state.Vitality; } }
        public int MedicineUnits { get { return state.MedicineUnits; } }
        public bool IsActive
        {
            get { return state.Phase != PrototypeDiseasePhase.Healthy && state.Phase != PrototypeDiseasePhase.Recovered; }
        }
        public bool CanTreat
        {
            get
            {
                return (state.Phase == PrototypeDiseasePhase.Effect ||
                        state.Phase == PrototypeDiseasePhase.Worsened ||
                        state.Phase == PrototypeDiseasePhase.Mitigated) &&
                       state.MedicineUnits >= PrototypeDiseaseConfig.TreatmentCost;
            }
        }

        public void Reset(int runSeed)
        {
            processedTransactions.Clear();
            eventIds.Clear();
            state = new PrototypeDiseaseSnapshot { RunSeed = runSeed, Phase = PrototypeDiseasePhase.Healthy };
        }

        public bool TryTelegraph(PrototypeSearchNodeDefinition definition, int day)
        {
            if (!IsDiseaseSource(definition)) return false;
            if (state.Phase != PrototypeDiseasePhase.Healthy && state.Phase != PrototypeDiseasePhase.Telegraph)
            {
                return false;
            }
            string exposureId = ExposureId(definition);
            if (!Commit(exposureId + ":telegraph", "disease.telegraph")) return state.Phase == PrototypeDiseasePhase.Telegraph;
            state.ExposureInstanceId = exposureId;
            state.SourceRegionId = definition.RegionId;
            state.SourceNodeId = definition.NodeId;
            state.ExposureDay = Math.Max(1, day);
            state.LastProcessedDay = state.ExposureDay;
            state.Phase = PrototypeDiseasePhase.Telegraph;
            return true;
        }

        public bool TryExpose(PrototypeSearchNodeDefinition definition, int day)
        {
            if (!IsDiseaseSource(definition)) return false;
            TryTelegraph(definition, day);
            string exposureId = ExposureId(definition);
            if (!string.Equals(state.ExposureInstanceId, exposureId, StringComparison.Ordinal)) return false;
            if (!Commit(exposureId + ":exposure", "disease.exposure"))
            {
                return (int)state.Phase >= (int)PrototypeDiseasePhase.Exposed;
            }
            state.Phase = PrototypeDiseasePhase.Exposed;
            return true;
        }

        public bool ObserveStoredResource(string resourceId, string stableItemId, int amount)
        {
            if (!string.Equals(resourceId, PrototypeDiseaseConfig.TreatmentResourceId, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(stableItemId) || amount <= 0)
            {
                return false;
            }
            if (!Commit("medicine:" + stableItemId, "disease.medicine.stored")) return true;
            state.MedicineUnits += amount;
            return true;
        }

        public bool ResolveReturn(bool forced, int day)
        {
            if (state.Phase != PrototypeDiseasePhase.Exposed && state.Phase != PrototypeDiseasePhase.Effect &&
                state.Phase != PrototypeDiseasePhase.Worsened)
            {
                return false;
            }
            state.LastProcessedDay = Math.Max(state.LastProcessedDay, day);
            if (state.Phase == PrototypeDiseasePhase.Exposed &&
                Commit(state.ExposureInstanceId + ":effect", "disease.effect"))
            {
                state.Vitality = Math.Max(1, state.Vitality - PrototypeDiseaseConfig.EffectVitalityCost);
                state.Phase = PrototypeDiseasePhase.Effect;
            }
            if (forced && state.Phase == PrototypeDiseasePhase.Effect &&
                Commit(state.ExposureInstanceId + ":forced-return", "disease.forced-return"))
            {
                state.ForcedReturn = true;
                ApplyWorsening("forced-return");
            }
            return true;
        }

        public bool AdvanceUntreatedDay(int day)
        {
            if (day <= state.LastProcessedDay) return false;
            state.LastProcessedDay = day;
            if (state.Phase != PrototypeDiseasePhase.Effect) return false;
            return ApplyWorsening("day." + day);
        }

        public bool TryMitigate()
        {
            if (state.Phase != PrototypeDiseasePhase.Effect && state.Phase != PrototypeDiseasePhase.Worsened)
            {
                return state.Phase == PrototypeDiseasePhase.Mitigated;
            }
            if (!Commit(state.ExposureInstanceId + ":mitigation", "disease.mitigation")) return true;
            state.Phase = PrototypeDiseasePhase.Mitigated;
            return true;
        }

        public bool CancelTreatment()
        {
            if (state.Phase != PrototypeDiseasePhase.Effect && state.Phase != PrototypeDiseasePhase.Worsened &&
                state.Phase != PrototypeDiseasePhase.Mitigated)
            {
                return false;
            }
            Commit(state.ExposureInstanceId + ":treatment-cancel", "disease.treatment.cancelled");
            return true;
        }

        public bool TryTreat()
        {
            if (state.Phase == PrototypeDiseasePhase.Recovered)
            {
                return processedTransactions.Contains(state.ExposureInstanceId + ":treatment");
            }
            if (!CanTreat) return false;
            string transactionId = state.ExposureInstanceId + ":treatment";
            if (!Commit(transactionId, "disease.treatment.completed")) return true;
            state.MedicineUnits -= PrototypeDiseaseConfig.TreatmentCost;
            state.Vitality = Math.Min(100, state.Vitality + PrototypeDiseaseConfig.TreatmentVitalityRecovery);
            state.Phase = PrototypeDiseasePhase.Recovered;
            return true;
        }

        public PrototypeDiseaseSnapshot CaptureSnapshot()
        {
            PrototypeDiseaseSnapshot snapshot = state.Clone();
            snapshot.ProcessedTransactionIds = processedTransactions.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            snapshot.EventIds = eventIds.ToArray();
            return snapshot;
        }

        public bool RestoreSnapshot(PrototypeDiseaseSnapshot snapshot)
        {
            if (snapshot == null || snapshot.RunSeed != state.RunSeed ||
                string.IsNullOrEmpty(snapshot.DiseaseId) || snapshot.Vitality < 1 || snapshot.MedicineUnits < 0)
            {
                return false;
            }
            state = snapshot.Clone();
            processedTransactions.Clear();
            foreach (string id in snapshot.ProcessedTransactionIds ?? Array.Empty<string>())
            {
                if (!string.IsNullOrEmpty(id)) processedTransactions.Add(id);
            }
            eventIds.Clear();
            eventIds.AddRange((snapshot.EventIds ?? Array.Empty<string>()).Where(id => !string.IsNullOrEmpty(id)));
            return true;
        }

        private bool ApplyWorsening(string cause)
        {
            if (!Commit(state.ExposureInstanceId + ":worsening:" + cause, "disease.worsening"))
            {
                return state.Phase == PrototypeDiseasePhase.Worsened;
            }
            state.Vitality = Math.Max(1, state.Vitality - PrototypeDiseaseConfig.WorseningVitalityCost);
            state.Phase = PrototypeDiseasePhase.Worsened;
            return true;
        }

        private bool Commit(string transactionId, string eventId)
        {
            if (string.IsNullOrEmpty(transactionId) || !processedTransactions.Add(transactionId)) return false;
            eventIds.Add(eventId);
            return true;
        }

        private static bool IsDiseaseSource(PrototypeSearchNodeDefinition definition)
        {
            return definition != null && string.Equals(
                definition.HazardId,
                PrototypeDiseaseConfig.ExposureHazardId,
                StringComparison.Ordinal);
        }

        private static string ExposureId(PrototypeSearchNodeDefinition definition)
        {
            return PrototypeDiseaseConfig.StableId + "@" + definition.NodeId;
        }
    }

    public static class PrototypeDiseaseRuntimeContract
    {
        public static PrototypeContractProbe VerifyNaturalAtomicTrace()
        {
            int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            PrototypeSearchRegionDefinition forest = PrototypeSearchRegionCatalog.Get("region.forest.grove");
            PrototypeSearchNodeDefinition source = forest.Nodes.First(node => string.Equals(
                node.NodeId, "node.forest.grove.tree-hollow.01", StringComparison.Ordinal));
            PrototypeSearchNodeDefinition alternateSource = forest.Nodes.First(node => string.Equals(
                node.NodeId, "node.forest.grove.drift-pile.01", StringComparison.Ordinal));
            PrototypeSearchNodeDefinition medicineSource = forest.Nodes.First(node => string.Equals(
                node.NodeId, "node.forest.grove.grass-patch.01", StringComparison.Ordinal));
            GameSession session = new GameSession(seed);
            PrototypeSearchNodeRuntime search = new PrototypeSearchNodeRuntime(seed);
            List<string> trace = new List<string>();
            bool began = session.BeginSearch(PrototypeExpeditionRegionId.Forest);
            trace.Add("01.region.depart");
            bool telegraph = search.Disease.TryTelegraph(source, session.Day);
            trace.Add("02.disease.telegraph");
            bool opened = search.TryOpen(source, session) == PrototypeSearchOpenResult.Opened;
            trace.Add("03.node.search.exposure");
            search.Close(session);
            bool medicineOpened = search.TryOpen(medicineSource, session) == PrototypeSearchOpenResult.Opened;
            trace.Add("04.medicine-node.search");
            PrototypeSearchNodeSnapshot node = search.ActiveNode;
            int medicineIndex = node == null ? -1 : Array.FindIndex(node.Remaining, item =>
                string.Equals(item.ResourceId, PrototypeDiseaseConfig.TreatmentResourceId, StringComparison.Ordinal));
            bool medicineTaken = medicineIndex >= 0 && search.SetFocusedIndex(medicineIndex) &&
                search.TryTakeFocused(session, delegate { return false; }) != PrototypeSearchTakeResult.Rejected;
            trace.Add("05.loot.medicine.take");
            search.Close(session);
            bool effect = search.Disease.ResolveReturn(false, session.Day) &&
                          search.Disease.Phase == PrototypeDiseasePhase.Effect;
            trace.Add("06.return.effect");
            bool worsened = search.Disease.AdvanceUntreatedDay(session.Day + 1) &&
                            search.Disease.Phase == PrototypeDiseasePhase.Worsened;
            trace.Add("07.untreated.worsening");
            PrototypeSearchRuntimeSnapshot saved = search.CaptureSnapshot();
            PrototypeSearchNodeRuntime restoredSearch = new PrototypeSearchNodeRuntime(seed);
            bool restored = restoredSearch.RestoreSnapshot(saved) &&
                            restoredSearch.Disease.Phase == PrototypeDiseasePhase.Worsened &&
                            restoredSearch.Disease.Vitality == search.Disease.Vitality;
            trace.Add("08.snapshot.restore");
            int medicineBeforeCancel = search.Disease.MedicineUnits;
            int vitalityBeforeCancel = search.Disease.Vitality;
            bool cancelAtomic = search.Disease.CancelTreatment() &&
                                search.Disease.MedicineUnits == medicineBeforeCancel &&
                                search.Disease.Vitality == vitalityBeforeCancel &&
                                search.Disease.Phase == PrototypeDiseasePhase.Worsened;
            trace.Add("09.treatment.cancel");
            bool mitigated = search.Disease.TryMitigate() && search.Disease.Phase == PrototypeDiseasePhase.Mitigated;
            trace.Add("10.disease.mitigation");
            int medicineBeforeTreatment = search.Disease.MedicineUnits;
            bool treated = search.Disease.TryTreat() && search.Disease.Phase == PrototypeDiseasePhase.Recovered;
            int medicineAfterTreatment = search.Disease.MedicineUnits;
            int vitalityAfterTreatment = search.Disease.Vitality;
            bool retryIdempotent = search.Disease.TryTreat() &&
                                   search.Disease.MedicineUnits == medicineAfterTreatment &&
                                   search.Disease.Vitality == vitalityAfterTreatment;
            trace.Add("11.treatment.complete");

            GameSession forcedSession = new GameSession(seed + 1);
            PrototypeSearchNodeRuntime forcedSearch = new PrototypeSearchNodeRuntime(seed + 1);
            bool forcedBegan = forcedSession.BeginSearch(PrototypeExpeditionRegionId.Forest);
            bool forcedOpened = forcedSearch.TryOpen(source, forcedSession) == PrototypeSearchOpenResult.Opened;
            forcedSearch.Close(forcedSession);
            int forcedBefore = forcedSearch.Disease.Vitality;
            bool forcedResolved = forcedSearch.Disease.ResolveReturn(true, forcedSession.Day);
            int forcedAfter = forcedSearch.Disease.Vitality;
            bool forcedRetry = forcedSearch.Disease.ResolveReturn(true, forcedSession.Day) &&
                               forcedSearch.Disease.Vitality == forcedAfter;
            trace.Add("12.forced-return.atomic");

            bool exactForestSources = string.Equals(source.HazardId, PrototypeDiseaseConfig.ExposureHazardId, StringComparison.Ordinal) &&
                                      string.Equals(alternateSource.HazardId, PrototypeDiseaseConfig.ExposureHazardId, StringComparison.Ordinal) &&
                                      PrototypeSearchNodeLootResolver.Resolve(seed, medicineSource).Any(item =>
                                          string.Equals(item.ResourceId, PrototypeDiseaseConfig.TreatmentResourceId, StringComparison.Ordinal));
            bool success = began && telegraph && opened && medicineOpened && medicineTaken && exactForestSources &&
                           effect && worsened && restored && cancelAtomic &&
                           mitigated && treated && medicineBeforeTreatment - medicineAfterTreatment == PrototypeDiseaseConfig.TreatmentCost &&
                           retryIdempotent && forcedBegan && forcedOpened && forcedResolved && forcedRetry &&
                           forcedBefore - forcedAfter == PrototypeDiseaseConfig.EffectVitalityCost + PrototypeDiseaseConfig.WorseningVitalityCost;
            return new PrototypeContractProbe(
                success,
                "trace=" + string.Join(">", trace.ToArray()) +
                " treatmentCost=" + PrototypeDiseaseConfig.TreatmentCost +
                " forestExposure=tree-hollow.01,drift-pile.01 medicine=grass-patch.01,.02" +
                " snapshotRestore=" + restored.ToString().ToLowerInvariant() +
                " cancelAtomic=" + cancelAtomic.ToString().ToLowerInvariant() +
                " forcedReturnAtomic=" + (forcedResolved && forcedRetry).ToString().ToLowerInvariant() +
                " grant=false warp=false skip=false");
        }
    }
}
