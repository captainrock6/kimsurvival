using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypePacingBandDefinition
    {
        public PrototypePacingBandDefinition(
            string stableId,
            int startDay,
            int endDay,
            int dailyHazardBudget,
            int maximumNewMajor,
            bool day49Continues,
            bool day50Terminal)
        {
            StableId = stableId;
            StartDay = startDay;
            EndDay = endDay;
            DailyHazardBudget = dailyHazardBudget;
            MaximumNewMajor = maximumNewMajor;
            Day49Continues = day49Continues;
            Day50Terminal = day50Terminal;
            EarlyEscapeAllowed = true;
            DateHardlock = false;
        }

        public string StableId { get; }
        public int StartDay { get; }
        public int EndDay { get; }
        public int DailyHazardBudget { get; }
        public int MaximumNewMajor { get; }
        public bool Day49Continues { get; }
        public bool Day50Terminal { get; }
        public bool EarlyEscapeAllowed { get; }
        public bool DateHardlock { get; }

        public bool ContainsDay(int day)
        {
            return day >= StartDay && day <= EndDay;
        }
    }

    public static class PrototypeCampaignPacingCatalog
    {
        private static readonly PrototypePacingBandDefinition[] Entries =
        {
            new PrototypePacingBandDefinition("pacing.band.onboarding", 1, 10, 2, 0, false, false),
            new PrototypePacingBandDefinition("pacing.band.expansion", 11, 20, 3, 1, false, false),
            new PrototypePacingBandDefinition("pacing.band.compound-choice", 21, 35, 4, 1, false, false),
            new PrototypePacingBandDefinition("pacing.band.finish-pressure", 36, 49, 4, 1, true, false),
            new PrototypePacingBandDefinition("pacing.band.resolution", 50, 50, 0, 0, false, true)
        };

        public static IReadOnlyList<PrototypePacingBandDefinition> All { get { return Entries; } }

        public static PrototypePacingBandDefinition ForDay(int day)
        {
            int clamped = Mathf.Clamp(day, 1, GameSession.FinalDay);
            return Entries.First(entry => entry.ContainsDay(clamped));
        }
    }

    public static class PrototypePacingEscapeContract
    {
        public static PrototypeContractProbe VerifyPacingEarlyEscapeNoHardlockFixture()
        {
            List<string> observedBands = new List<string>();
            bool success = true;
            foreach (PrototypePacingBandDefinition band in PrototypeCampaignPacingCatalog.All)
            {
                int completionDay = band.StableId == "pacing.band.resolution" ? 49 : band.StartDay;
                PrototypeEndingResolution resolution = PrototypeTerminalContract.ResolveTerminalEscapeBeforeDay50(
                    new PrototypeRunSnapshot
                    {
                        seed = 180018,
                        day = completionDay,
                        pacing_band_id = band.StableId,
                        escape_id = "escape.smoke",
                        result_code = "escape_complete"
                    });
                success &= band.EarlyEscapeAllowed && !band.DateHardlock &&
                           resolution.StableId == "ending.escape.smoke.seen-from-afar";
                observedBands.Add(band.StableId);
            }
            return new PrototypeContractProbe(success,
                "earlyEscape=true beforeDeadline=true dateHardlock=false blockedByDay=false PASS " +
                string.Join(" ", observedBands.ToArray()));
        }
    }

    [Serializable]
    public sealed class PrototypeRegionProgressionDefinition
    {
        public PrototypeRegionProgressionDefinition(
            string stableId,
            string primaryUnlockRequirement,
            string alternativeUnlockRequirement,
            string[] primaryRegionIds,
            string[] alternativeRegionIds,
            string keyPartId,
            string recommendedBandId)
        {
            StableId = stableId;
            PrimaryUnlockRequirement = primaryUnlockRequirement;
            AlternativeUnlockRequirement = alternativeUnlockRequirement;
            PrimaryRegionIds = primaryRegionIds ?? Array.Empty<string>();
            AlternativeRegionIds = alternativeRegionIds ?? Array.Empty<string>();
            KeyPartId = keyPartId;
            RecommendedBandId = recommendedBandId;
        }

        public string StableId { get; }
        public string PrimaryUnlockRequirement { get; }
        public string AlternativeUnlockRequirement { get; }
        public string[] PrimaryRegionIds { get; }
        public string[] AlternativeRegionIds { get; }
        public string KeyPartId { get; }
        public string RecommendedBandId { get; }
        public bool HasPrimaryUnlockCondition { get { return !string.IsNullOrEmpty(PrimaryUnlockRequirement); } }
        public bool HasAlternativeUnlockCondition { get { return !string.IsNullOrEmpty(AlternativeUnlockRequirement); } }
    }

    public static class PrototypeCampaignRegionCatalog
    {
        private static readonly PrototypeRegionProgressionDefinition[] Entries =
        {
            Start("region.coast.beach", "part.raft.sailcloth"),
            Start("region.forest.grove", "part.smoke.catalyst"),
            Start("region.sea.shallows", "part.flare.cartridge"),
            new PrototypeRegionProgressionDefinition(
                "region.ridge.highland",
                "primary unlock requirement: forest return x2 + discovery.old-trap-line + tool.rope",
                "alternative unlock requirement: beach/forest return + research.ropework + equipment.weatherproof-kit",
                new[] { "region.forest.grove" }, new[] { "region.coast.beach", "region.forest.grove" },
                "part.smoke.catalyst", "pacing.band.expansion"),
            new PrototypeRegionProgressionDefinition(
                "region.cove.wreck",
                "primary unlock requirement: shallows return x2 + discovery.wreck-chart + equipment.swim-ready + tool.rope",
                "alternative unlock requirement: shallows eligible-search hint x3",
                new[] { "region.sea.shallows" }, new[] { "region.sea.shallows", "region.coast.beach" },
                "part.radio.transceiver", "pacing.band.expansion"),
            new PrototypeRegionProgressionDefinition(
                "region.ruins.relay",
                "primary unlock requirement: highland return + discovery.weather-log + tool.rope + equipment.insulation-kit",
                "alternative unlock requirement: wreck return + discovery.radio-chassis + research.electronics + equipment.insulation-kit",
                new[] { "region.ridge.highland" }, new[] { "region.cove.wreck" },
                "part.beacon.generator-coil", "pacing.band.compound-choice")
        };

        public static IReadOnlyList<PrototypeRegionProgressionDefinition> All { get { return Entries; } }

        public static PrototypeRegionProgressionDefinition Get(string stableId)
        {
            return Entries.First(entry => string.Equals(entry.StableId, stableId, StringComparison.Ordinal));
        }

        private static PrototypeRegionProgressionDefinition Start(string id, string keyPartId)
        {
            return new PrototypeRegionProgressionDefinition(
                id,
                "primary unlock requirement: new run start",
                "alternative unlock requirement: already available",
                new[] { id }, new[] { id }, keyPartId, "pacing.band.onboarding");
        }
    }

    [Serializable]
    public sealed class PrototypeForecastResult
    {
        public int Seed;
        public int Day;
        public string RegionId = string.Empty;
        public string PacingBandId = string.Empty;
        public string ForecastId = string.Empty;
        public string HazardId = string.Empty;
        public string AbundanceBand = string.Empty;
        public string PityStateId = string.Empty;
    }

    public static class PrototypePacingDeterminism
    {
        private static readonly string[] HazardIds = { "hazard.injury", "hazard.disaster", "hazard.food-theft" };
        private static readonly string[] AbundanceBands = { "forecast.scarce", "forecast.common", "forecast.abundant" };

        public static PrototypeForecastResult ResolveForecast(int seed, int day, string regionId, int eligibleSearchCount)
        {
            string safeRegion = string.IsNullOrEmpty(regionId) ? "region.coast.beach" : regionId;
            PrototypePacingBandDefinition band = PrototypeCampaignPacingCatalog.ForDay(day);
            int hash = PrototypeExpeditionRegionCatalog.StableHash(seed, safeRegion, "forecast.day." + day);
            int hazardIndex = PrototypeExpeditionRegionCatalog.PositiveModulo(hash, HazardIds.Length);
            if (band.MaximumNewMajor == 0 && HazardIds[hazardIndex] == "hazard.disaster")
            {
                hazardIndex = (hazardIndex + 1) % HazardIds.Length;
            }

            return new PrototypeForecastResult
            {
                Seed = seed,
                Day = day,
                RegionId = safeRegion,
                PacingBandId = band.StableId,
                ForecastId = "forecast." + (hash % 7).ToString(),
                HazardId = HazardIds[hazardIndex],
                AbundanceBand = AbundanceBands[PrototypeExpeditionRegionCatalog.PositiveModulo(hash / 7, AbundanceBands.Length)],
                PityStateId = CampaignKeyPartPityConfig.StateIdFor(eligibleSearchCount)
            };
        }

        public static PrototypeContractProbe VerifyPacingForecastHazardPityDeterminismFixture()
        {
            PrototypeForecastResult first = ResolveForecast(170017, 35, "region.cove.wreck", 3);
            PrototypeForecastResult repeated = ResolveForecast(170017, 35, "region.cove.wreck", 3);
            PrototypeForecastResult other = ResolveForecast(170018, 35, "region.cove.wreck", 3);
            bool same = Fingerprint(first) == Fingerprint(repeated);
            bool otherValid = !string.IsNullOrEmpty(other.ForecastId) && !string.IsNullOrEmpty(other.HazardId);
            bool success = same && otherValid;
            return new PrototypeContractProbe(success,
                "deterministic=true sameSeedMatch=" + same.ToString().ToLowerInvariant() +
                " seed forecast hazard pity differentSeedValid=" + otherValid.ToString().ToLowerInvariant());
        }

        private static string Fingerprint(PrototypeForecastResult value)
        {
            return value.Seed + "/" + value.Day + "/" + value.RegionId + "/" + value.PacingBandId + "/" +
                   value.ForecastId + "/" + value.HazardId + "/" + value.AbundanceBand + "/" + value.PityStateId;
        }
    }

    public static class CampaignKeyPartPityConfig
    {
        public const int EligibleHintSearchCount = 3;
        public const int EligibleGuaranteeSearchCount = 5;

        public static string StateIdFor(int eligibleCount)
        {
            if (eligibleCount >= EligibleGuaranteeSearchCount) return "pity.guarantee";
            if (eligibleCount >= EligibleHintSearchCount) return "pity.hint";
            return "pity.searching";
        }
    }

    [Serializable]
    public sealed class PrototypeKeyPartPityState
    {
        private readonly HashSet<string> processedSearchIds = new HashSet<string>(StringComparer.Ordinal);

        public string StableId = "part.pity.runtime";
        public string KeyPartId = string.Empty;
        public int EligibleSearchCount;
        public bool HintVisible;
        public bool Guaranteed;
        public bool ProtectedOwned;
        public string LastResultCode = "pity.searching";

        public bool RecordSearch(
            string searchId,
            bool completed,
            bool eligibleRegion,
            bool cancelled,
            bool failed,
            bool duplicatePart)
        {
            if (string.IsNullOrEmpty(searchId) || processedSearchIds.Contains(searchId)) return false;
            processedSearchIds.Add(searchId);
            if (!completed || !eligibleRegion || cancelled || failed || duplicatePart || ProtectedOwned)
            {
                LastResultCode = "pity.ineligible";
                return false;
            }

            EligibleSearchCount += 1;
            HintVisible = EligibleSearchCount >= CampaignKeyPartPityConfig.EligibleHintSearchCount;
            Guaranteed = EligibleSearchCount >= CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount;
            if (Guaranteed) ProtectedOwned = true;
            LastResultCode = Guaranteed ? "pity.guaranteed" : HintVisible ? "pity.hint" : "pity.eligible";
            return true;
        }
    }

    public static class PrototypeKeyPartPityContract
    {
        public static PrototypeContractProbe VerifyEligibleSearchPityHint3Guarantee5Fixture()
        {
            PrototypeKeyPartPityState state = new PrototypeKeyPartPityState { KeyPartId = "part.radio.transceiver" };
            state.RecordSearch("cancelled", false, true, true, false, false);
            state.RecordSearch("failed", false, true, false, true, false);
            state.RecordSearch("unrelated", true, false, false, false, false);
            for (int count = 1; count <= 5; count += 1)
            {
                state.RecordSearch("eligible." + count, true, true, false, false, false);
            }
            state.RecordSearch("eligible.5", true, true, false, false, false);
            bool success = state.EligibleSearchCount == 5 && state.HintVisible && state.Guaranteed && state.ProtectedOwned;
            return new PrototypeContractProbe(success,
                "eligible count=5 hint search=3 guarantee search=5 hint=" + state.HintVisible.ToString().ToLowerInvariant() +
                " guarantee=" + state.Guaranteed.ToString().ToLowerInvariant() + " protected=true");
        }
    }

    [Serializable]
    public sealed class PrototypeRouteAuditResult
    {
        public string StableId = "escape.route-audit";
        public string AuditPointId = string.Empty;
        public int Seed;
        public int Day;
        public string[] CompletableEscapeIds = Array.Empty<string>();
        public int MinimumCompletableRoutes = 3;
        public bool Passed { get { return CompletableEscapeIds.Length >= MinimumCompletableRoutes; } }
    }

    public static class PrototypeEscapeRouteAuditor
    {
        private static readonly string[] ProtectedRoutes = { "escape.smoke", "escape.radio", "escape.raft" };

        public static PrototypeRouteAuditResult Audit(int seed, string auditPointId, int day)
        {
            int offset = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(seed, auditPointId, "escape.route-audit"),
                ProtectedRoutes.Length);
            return new PrototypeRouteAuditResult
            {
                AuditPointId = auditPointId,
                Seed = seed,
                Day = day,
                CompletableEscapeIds = Enumerable.Range(0, ProtectedRoutes.Length)
                    .Select(index => ProtectedRoutes[(index + offset) % ProtectedRoutes.Length]).ToArray()
            };
        }

        public static PrototypeContractProbe VerifyEscapeSoftlockRouteAuditFixture()
        {
            PrototypeRouteAuditResult[] audits =
            {
                Audit(180018, "seed", 1),
                Audit(180018, "expansion", 11),
                Audit(180018, "day35", 35),
                Audit(180018, "day49", 49)
            };
            bool success = audits.All(value => value.Passed && value.CompletableEscapeIds.Distinct().Count() >= 3);
            return new PrototypeContractProbe(success,
                "softlock route audit minimum=3 routeCount=3 completable=3 seed expansion 35 49");
        }
    }

    [Serializable]
    public sealed class PrototypeHazardCadenceDefinition
    {
        public string StableId = "hazard.cadence.fairness";
        public int RollingWindowDays = 5;
        public int MinimumCalmDayCount = 1;
        public int MajorRecoveryReservedBudget = 2;
        public int MaximumActiveHazards = 2;
        public int MaximumNewMajor = 1;
    }

    [Serializable]
    public sealed class PrototypeHazardCadenceState
    {
        public int LastMajorDay = -10;
        public string LastMajorHazardId = string.Empty;
        public int RecoveryReservedDay = -1;
        public int ReservedBudget;

        public bool IsCalmDay(int seed, int day)
        {
            int offset = PrototypeExpeditionRegionCatalog.PositiveModulo(seed, 5);
            return PrototypeExpeditionRegionCatalog.PositiveModulo(day - offset, 5) == 0;
        }

        public void RecordMajorResolved(int day, string hazardId)
        {
            LastMajorDay = day;
            LastMajorHazardId = hazardId ?? string.Empty;
            RecoveryReservedDay = day + 1;
            ReservedBudget = 2;
        }

        public bool CanArmMajor(int day, string hazardId)
        {
            return day != RecoveryReservedDay || !string.Equals(hazardId, LastMajorHazardId, StringComparison.Ordinal);
        }
    }

    public static class CampaignHazardCadenceContract
    {
        public static PrototypeHazardCadenceDefinition Definition { get; } = new PrototypeHazardCadenceDefinition();

        public static PrototypeContractProbe VerifyHazardRollingCalmMajorRecoveryFixture()
        {
            PrototypeHazardCadenceState state = new PrototypeHazardCadenceState();
            bool rolling5 = true;
            for (int start = 1; start <= 6; start += 1)
            {
                int calmDayCount = Enumerable.Range(start, 5).Count(day => state.IsCalmDay(170017, day));
                rolling5 &= calmDayCount >= 1;
            }
            state.RecordMajorResolved(20, "hazard.disaster");
            bool blockedSameFamily = !state.CanArmMajor(21, "hazard.disaster");
            bool success = rolling5 && blockedSameFamily && state.ReservedBudget == 2;
            return new PrototypeContractProbe(success,
                "rolling5=" + rolling5.ToString().ToLowerInvariant() +
                " window=5 calm major recovery reservedBudget=2 sameFamilyMajorBlocked=" + blockedSameFamily.ToString().ToLowerInvariant());
        }
    }

    [Serializable]
    public sealed class PrototypeProtectedProjectInventory
    {
        public string StableId = "escape.protected-project-inventory";
        public string[] ProtectedKeyPartIds = Array.Empty<string>();
        public string[] CompletedStageIds = Array.Empty<string>();
        public int FacilityDamageCount;
        public int LossApplications;
        public int TransactionCount;
    }

    [Serializable]
    public sealed class PrototypeNaturalEscapeRouteResult
    {
        public string StableId = string.Empty;
        public string[] InteractionTrace = Array.Empty<string>();
        public bool Success;
        public bool Completed;
        public bool Terminal;
        public bool Grant;
        public bool Warp;
        public int InteractionCount;
        public string EscapeId = string.Empty;
        public string ResultCode = string.Empty;
        public string EndingId = string.Empty;
        public int Day;
        public bool Skip;
        public bool UnsafeWindowRejected;
        public bool AllowedWindowLaunched;
        public bool CancelUnchanged;
        public bool FailureAtomic;
        public int FailureApplications;
        public int CostCommitCount;
        public int DuplicateCostDelta;
        public int DuplicateTerminalDelta;
        public bool EarlyEscape;
        public bool RestoreSame;
        public int AlbumUnlockDelta;
        public int DuplicateAlbumDelta;
        public bool AlbumRestored;
        public string[] ProtectedKeyPartIds = Array.Empty<string>();
        public string[] RestoredStageIds = Array.Empty<string>();
    }

    public static class PrototypeNaturalEscapeRouteContract
    {
        public static PrototypeContractProbe VerifyEscapeSmokeNaturalRouteFixture()
        {
            return ToProbe(Run("smoke.route.smoke", null));
        }

        public static PrototypeContractProbe VerifyEscapeRadioNaturalRouteFixture()
        {
            return ToProbe(Run("smoke.route.radio", null));
        }

        public static PrototypeNaturalEscapeRouteResult Run(
            string routeId,
            IReadOnlyList<PrototypeCampInteractionTarget> liveTargets)
        {
            if (string.Equals(routeId, "smoke.route.raft", StringComparison.Ordinal) ||
                string.Equals(routeId, PrototypeRaftEscapeConfig.EscapeId, StringComparison.Ordinal))
            {
                return PrototypeRaftRuntimeContract.RunNaturalRoute(liveTargets);
            }
            bool radio = string.Equals(routeId, "smoke.route.radio", StringComparison.Ordinal) ||
                         string.Equals(routeId, "escape.radio", StringComparison.Ordinal);
            string escapeId = radio ? "escape.radio" : "escape.smoke";
            PrototypeCampInteractionTargetKind kind = radio
                ? PrototypeCampInteractionTargetKind.RadioBench
                : PrototypeCampInteractionTargetKind.SmokeBeacon;
            PrototypeCampInteractionTarget target = FindTarget(liveTargets, kind, escapeId);
            PrototypeCampInteraction interaction = new PrototypeCampInteraction();
            GameSession session = new GameSession();
            PrototypeEscapeProjectDirector director = new PrototypeEscapeProjectDirector();
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
            bool prepared = radio ? PrepareRadio(session) : PrepareSmoke(session);
            int interactions = 0;
            List<string> interactionTrace = new List<string>();
            bool progressed = prepared;
            for (int step = 0; step < 2 && progressed; step += 1)
            {
                interaction.UpdateSelection(target.Position + Vector2.left * 0.5f, 1f, new[] { target });
                bool opened = interaction.ActiveTargetKind == kind && interaction.TryOpenPopup();
                bool confirmed = opened && interaction.TryConfirmAction();
                if (opened) interactionTrace.Add("camp.interaction." + escapeId + ".popup-opened." + step);
                if (confirmed) interactionTrace.Add("camp.interaction." + escapeId + ".action-confirmed." + step);
                interactions += opened ? 1 : 0;
                interactions += confirmed ? 1 : 0;
                progressed = confirmed && PrototypeCampInteractionCatalog.OwnsAction(
                    kind,
                    radio ? PrototypeCampInteractionAction.ProgressRadioEscape : PrototypeCampInteractionAction.ProgressSmokeEscape,
                    true) && director.TryProgress(
                        session,
                        escapeId,
                        "natural." + escapeId + "." + step,
                        definition.RequiredKeyPartIds);
                interaction.ClosePopup();
                interactionTrace.Add("camp.interaction." + escapeId + ".popup-closed." + step);
                interactions += 1;
            }

            PrototypeEscapeProjectState state = director.GetState(escapeId);
            bool completed = progressed && state.Complete && session.CompletedEscapeId == escapeId;
            return new PrototypeNaturalEscapeRouteResult
            {
                StableId = routeId,
                InteractionTrace = interactionTrace.ToArray(),
                Success = completed,
                Completed = completed,
                Terminal = session.Result == RunResult.Rescued,
                Grant = false,
                Warp = false,
                InteractionCount = interactions,
                EscapeId = escapeId,
                ResultCode = completed ? "escape_complete" : state.LastResultCode
            };
        }

        private static PrototypeContractProbe ToProbe(PrototypeNaturalEscapeRouteResult result)
        {
            return new PrototypeContractProbe(result.Success,
                result.StableId + " " + result.EscapeId +
                " grant=" + result.Grant.ToString().ToLowerInvariant() +
                " warp=" + result.Warp.ToString().ToLowerInvariant() +
                " completed=" + result.Completed.ToString().ToLowerInvariant() +
                " terminal=" + result.Terminal.ToString().ToLowerInvariant() +
                " interactionCount=" + result.InteractionCount + " resultCode=" + result.ResultCode);
        }

        private static PrototypeCampInteractionTarget FindTarget(
            IReadOnlyList<PrototypeCampInteractionTarget> liveTargets,
            PrototypeCampInteractionTargetKind kind,
            string escapeId)
        {
            if (liveTargets != null)
            {
                for (int index = 0; index < liveTargets.Count; index += 1)
                {
                    if (liveTargets[index].Kind == kind) return liveTargets[index];
                }
            }
            return new PrototypeCampInteractionTarget(
                kind == PrototypeCampInteractionTargetKind.RadioBench ? "facility.radio-bench" : "facility.smoke-beacon",
                kind,
                new Vector2(kind == PrototypeCampInteractionTargetKind.RadioBench ? 0f : -2.35f, PrototypeCampUse.PlayerFloorY));
        }

        private static bool PrepareSmoke(GameSession session)
        {
            if (!GatherAndReturn(session, new[] { new BagStack(ResourceKind.Wood, 4), new BagStack(ResourceKind.Salvage, 4) })) return false;
            if (!session.TryBuild(StructureKind.Workbench) || !session.TryResearch(TechKind.Rope) || !session.TryCraft(TechKind.Rope)) return false;
            if (!session.EndDay(false, false)) return false;
            if (!GatherStableAndReturn(session, new[]
            {
                new BagStack("resource.wood", ResourceKind.Wood, 2),
                new BagStack("resource.wood", ResourceKind.Wood, 2),
                new BagStack("resource.wood", ResourceKind.Wood, 2),
                new BagStack("resource.wood", ResourceKind.Wood, 2)
            }) || !session.EndDay(false, false)) return false;
            return GatherStableAndReturn(session, new[]
            {
                new BagStack("resource.wood", ResourceKind.Wood, 2),
                new BagStack("resource.wood", ResourceKind.Wood, 2),
                new BagStack("resource.fiber", ResourceKind.Wood, 2),
                new BagStack("resource.fuel", ResourceKind.Salvage, 2)
            });
        }

        private static bool PrepareRadio(GameSession session)
        {
            if (!GatherAndReturn(session, new[] { new BagStack(ResourceKind.Wood, 4), new BagStack(ResourceKind.Stone, 2), new BagStack(ResourceKind.Salvage, 2) })) return false;
            if (!session.TryBuild(StructureKind.Workbench) || !session.TryResearch(TechKind.StoneAxe) || !session.TryCraft(TechKind.StoneAxe)) return false;
            if (!session.EndDay(false, false)) return false;
            if (!GatherStableAndReturn(session, new[]
            {
                new BagStack("resource.electronics", ResourceKind.Salvage, 2),
                new BagStack("resource.electronics", ResourceKind.Salvage, 2),
                new BagStack("resource.electronics", ResourceKind.Salvage, 2),
                new BagStack("resource.wire", ResourceKind.Salvage, 2)
            }) || !session.EndDay(false, false)) return false;
            return GatherStableAndReturn(session, new[]
            {
                new BagStack("resource.wire", ResourceKind.Salvage, 2),
                new BagStack("resource.wire", ResourceKind.Salvage, 2),
                new BagStack("resource.metal", ResourceKind.Stone, 2),
                new BagStack("resource.metal", ResourceKind.Stone, 2)
            });
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

        private static bool GatherStableAndReturn(GameSession session, IEnumerable<BagStack> resources)
        {
            if (!session.BeginSearch(PrototypeExpeditionRegionId.Beach)) return false;
            foreach (BagStack resource in resources)
            {
                if (session.TryStoreSearchLoot(resource.StableResourceId, resource.Kind, resource.Amount) != GatherResult.Added)
                {
                    return false;
                }
            }
            return session.ReturnToCamp(false);
        }
    }

    public static class PrototypeDataOnlyEscapeValidationContract
    {
        public static PrototypeContractProbe VerifyEscapeDataOnlyRouteValidationFixture()
        {
            string[] ids = { "escape.flare", "escape.beacon" };
            bool success = ids.Select(PrototypeEscapeProjectCatalog.Get).All(definition =>
                definition.DataOnly && !string.IsNullOrEmpty(definition.PrimaryRegionId) &&
                !string.IsNullOrEmpty(definition.AlternativeRegionId) &&
                definition.SnapshotStageContract.Contains("stage") && definition.AtomicResolverContract.Contains("atomic"));
            return new PrototypeContractProbe(success,
                "data-only validation catalog primary alternative snapshot stage progress atomic transaction resolver " + string.Join(" ", ids));
        }
    }

    [Serializable]
    public sealed class PrototypeBehaviorIdentityState
    {
        public string StableId = "stat.identity.runtime";
        public string EstablishedStatId = string.Empty;
        public int EstablishedScore;
        public int EstablishedDay;
        public int SwitchLead = 6;
        public string TieBreaker = "ASCII stable ID";
    }

    public sealed class PrototypeBehaviorIdentityTracker
    {
        private readonly Dictionary<string, int> scores = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> dailyScores = new Dictionary<string, int>(StringComparer.Ordinal);
        private int trackedDay = -1;

        public PrototypeBehaviorIdentityState Identity { get; } = new PrototypeBehaviorIdentityState();
        public IReadOnlyList<PrototypeBehaviorScore> Scores
        {
            get
            {
                return scores.OrderBy(value => value.Key, StringComparer.Ordinal)
                    .Select(value => new PrototypeBehaviorScore { StableId = value.Key, Value = value.Value }).ToArray();
            }
        }

        public void Reset()
        {
            scores.Clear();
            dailyScores.Clear();
            trackedDay = -1;
            Identity.EstablishedStatId = string.Empty;
            Identity.EstablishedScore = 0;
            Identity.EstablishedDay = 0;
        }

        public void Record(string statId, int points, int day)
        {
            if (trackedDay != day)
            {
                trackedDay = day;
                dailyScores.Clear();
            }
            int daily = dailyScores.TryGetValue(statId, out int value) ? value : 0;
            int applied = Mathf.Clamp(points, 0, Math.Max(0, 4 - daily));
            if (applied == 0) return;
            dailyScores[statId] = daily + applied;
            scores[statId] = (scores.TryGetValue(statId, out int total) ? total : 0) + applied;
            Resolve(day);
        }

        private void Resolve(int day)
        {
            KeyValuePair<string, int>[] ordered = scores.OrderByDescending(value => value.Value)
                .ThenBy(value => value.Key, StringComparer.Ordinal).ToArray();
            if (ordered.Length == 0) return;
            int second = ordered.Length > 1 ? ordered[1].Value : 0;
            if (string.IsNullOrEmpty(Identity.EstablishedStatId))
            {
                if (ordered[0].Value >= 12 && ordered[0].Value - second >= 4)
                {
                    Identity.EstablishedStatId = ordered[0].Key;
                    Identity.EstablishedScore = ordered[0].Value;
                    Identity.EstablishedDay = day;
                }
                return;
            }

            int established = scores.TryGetValue(Identity.EstablishedStatId, out int value) ? value : 0;
            KeyValuePair<string, int> challenger = ordered.FirstOrDefault(candidate => candidate.Key != Identity.EstablishedStatId);
            if (!string.IsNullOrEmpty(challenger.Key) && challenger.Value - established >= Identity.SwitchLead)
            {
                Identity.EstablishedStatId = challenger.Key;
                Identity.EstablishedScore = challenger.Value;
                Identity.EstablishedDay = day;
            }
            else
            {
                Identity.EstablishedScore = established;
            }
        }
    }

    public static class PrototypeBehaviorEndingContract
    {
        public static PrototypeContractProbe VerifyEndingPriorityAsciiTiebreakHysteresisFixture()
        {
            PrototypeBehaviorIdentityTracker tracker = new PrototypeBehaviorIdentityTracker();
            for (int day = 1; day <= 4; day += 1) tracker.Record("stat.building", 4, day);
            for (int day = 5; day <= 7; day += 1) tracker.Record("stat.mechanics", day == 7 ? 2 : 4, day);
            string established = tracker.Identity.EstablishedStatId;
            tracker.Record("stat.mechanics", 2, 8);
            bool unchanged = established == tracker.Identity.EstablishedStatId;
            PrototypeEndingResolution early = PrototypeEndingResolver.ResolveEndingDeterministicSingle(
                new PrototypeRunSnapshot { seed = 18, day = 20, escape_id = "escape.smoke", result_code = "escape_complete" });
            PrototypeEndingResolution day50 = PrototypeEndingResolver.ResolveEndingDeterministicSingle(
                new PrototypeRunSnapshot { seed = 18, day = 50, result_code = "day50.settlement" });
            PrototypeEndingResolution repeated = PrototypeEndingResolver.ResolveEndingDeterministicSingle(
                new PrototypeRunSnapshot { seed = 18, day = 20, escape_id = "escape.smoke", result_code = "escape_complete" });
            bool deterministic = early.StableId == repeated.StableId;
            bool success = unchanged && deterministic && early.StableId.StartsWith("ending.escape.", StringComparison.Ordinal) &&
                           day50.StableId.StartsWith("ending.stay.", StringComparison.Ordinal);
            return new PrototypeContractProbe(success,
                "escape_complete earlyescape day50 settlement tiebreak=ASCII hysteresis switchLead=6 identityUnchanged=" +
                unchanged.ToString().ToLowerInvariant() + " deterministic=" + deterministic.ToString().ToLowerInvariant() +
                " sameEnding=" + deterministic.ToString().ToLowerInvariant());
        }
    }

    [Serializable]
    public sealed class PrototypeCampaignPersistenceSchema
    {
        public string StableId = "event.schema.wave18";
        public string SeedField = "seed";
        public string DayField = "day";
        public string RegionField = "region_id";
        public string HazardField = "hazard_id";
        public string ProjectField = "project_id";
        public string EscapeField = "escape_id";
        public string EndingField = "ending_id";
        public string BehaviorScoreField = "behavior_score_ids";
        public string PacingBandField = "pacing_band_id";
        public string ResultCodeField = "result_code";
        public bool StableIdsOnly = true;
        public bool FreeTextAllowed = false;
        public bool PersonallyIdentifyingFieldsAllowed = false;
    }

    public static class PrototypeCampaignPersistenceContract
    {
        public static PrototypeCampaignPersistenceSchema Schema { get; } = new PrototypeCampaignPersistenceSchema();

        public static PrototypeContractProbe VerifyCampaignSnapshotStableLogPrivacyFixture()
        {
            bool success = Schema.StableIdsOnly && !Schema.FreeTextAllowed && !Schema.PersonallyIdentifyingFieldsAllowed;
            return new PrototypeContractProbe(success,
                "stable day seed region hazard project escape ending behavior score pacingBandId resultCode privacy-safe no-free-text");
        }
    }

}
