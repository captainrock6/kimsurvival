using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeO11ExpeditionLedger
    {
        public string ProfileId = string.Empty;
        public float ExpeditionEnergyCost;
        public float[] StartEnergy = Array.Empty<float>();
        public float[] ReturnEnergy = Array.Empty<float>();
        public float FourthExpeditionStartEnergy;
    }

    [Serializable]
    public sealed class PrototypeO11RouteBurden
    {
        public string EscapeId = string.Empty;
        public int CommonResourceUnits;
        public int FoodUnits;
        public int AdvancedResourceUnits;
        public int KeyPartCount;
        public int RequiredRegionCount;
        public int ResearchActionCount;
        public int ProjectCommitCount;
        public int PreparationDays;
        public int WaitDays;
        public float BurdenScore;
    }

    [Serializable]
    public sealed class PrototypeO11RouteBand
    {
        public string ProfileId = string.Empty;
        public PrototypeO11RouteBurden[] Representative = Array.Empty<PrototypeO11RouteBurden>();
        public float MinimumShortestRatio;
        public float MinimumMedianRatio;
        public bool Passes;
    }

    public enum PrototypeO11RaftCostProfile
    {
        Unknown,
        O10Pending,
        O11Applied
    }

    /// <summary>
    /// O11 owns the small, explainable survival correction and the read-only
    /// three-route burden contract. Escape state machines remain owned by their
    /// route implementations; the raft proposal here is deliberately data-only.
    /// </summary>
    public static class PrototypeO11BalanceConfig
    {
        public const string ContractId = "gamejam.o11.survival-route-balance.v1";

        // Measured O10 baseline. Kept beside the replacement values so the
        // deterministic probe can report a reproducible before/after ledger.
        public const float BaselineLandMovingEnergyPerSecond = 0.18f;
        public const float BaselineSwimmingMovingEnergyPerSecond = 0.65f;
        public const float BaselineSwimmingIdleEnergyPerSecond = 0.22f;
        public const float BaselineForcedReturnEnergyCost = 22f;
        public const float BaselineMealEnergyRecovery = 0f;
        public const float BaselineNextDayBaseRecovery = 20f;
        public const float BaselineCampfireRecovery = 18f;
        public const float BaselineRainCollectorRecovery = 10f;
        public const float BaselineBedRecovery = 20f;
        public const float BaselineSofaRecovery = 8f;

        // O11 red-first correction. Integer recovery values are intentionally
        // simple enough to expose verbatim in a contextual facility popup.
        public const float LandMovingEnergyPerSecond = 0.15f;
        public const float SwimmingMovingEnergyPerSecond = 0.35f;
        public const float SwimmingIdleEnergyPerSecond = 0.12f;
        public const float ForcedReturnEnergyCost = 12f;
        public const float MealHungerRecovery = 35f;
        public const float MealEnergyRecovery = 15f;
        public const float DailyHungerCost = 35f;
        public const float StarvationEnergyCost = 35f;
        public const float NextDayBaseRecovery = 35f;
        public const float CampfireRecovery = 10f;
        public const float RainCollectorRecovery = 5f;
        public const float BedRecovery = 20f;
        public const float SofaRecovery = 10f;

        // Daylight and discrete search costs are intentionally unchanged.
        public const float LandDaylightPerSecond = 0.75f;
        public const float SwimmingDaylightPerSecond = 1.15f;
        public const int LandNodeEnergyCost = 7;
        public const int LandNodeDaylightCost = 14;
        public const int WaterNodeEnergyCost = 9;
        public const int WaterNodeDaylightCost = 18;
        public const int LegacyLandGatherEnergyCost = 6;
        public const int LegacyWaterGatherEnergyCost = 9;

        // Representative novice expedition: movement plus four deliberate
        // searches, not a debug grant/warp shortcut.
        public const float RepresentativeLandMovingSeconds = 20f;
        public const float RepresentativeSwimmingMovingSeconds = 10f;
        public const int RepresentativeLandNodeCount = 3;
        public const int RepresentativeWaterNodeCount = 1;
        public const float MinimumFourthExpeditionStartEnergy = 70f;

        // Burden proxy is SAMPLE_ONLY. A protected part is weighted as two
        // reveals plus travel; advanced stock is scarcer than common stock.
        public const float CommonResourceWeight = 1f;
        public const float FoodWeight = 1.25f;
        public const float AdvancedResourceWeight = 1.75f;
        public const float ProtectedPartWeight = 8f;
        public const float RequiredRegionWeight = 3f;
        public const float ResearchActionWeight = 2f;
        public const float ProjectCommitWeight = 2f;
        public const float WaitDayWeight = 2f;
        public const float MinimumRouteBurdenRatio = 0.75f;

        // Exact integration-only raft patch. PrototypeRaftEscape.cs is not an
        // O11-owned file, so these values are verified here but not applied.
        public const int ProposedRaftHullWoodCost = 3;
        public const int ProposedRaftHullSalvageCost = 2;
        public const int ProposedRaftSailWoodCost = 2;
        public const int ProposedRaftSailSalvageCost = 1;
        public const int ProposedRaftSuppliesFoodCost = 3;

        private static readonly int[] RepresentativeSeeds =
        {
            PrototypeExpeditionRegionCatalog.DefaultRunSeed,
            180018,
            220026,
            420042,
            110011
        };

        public static PrototypeO11ExpeditionLedger SimulateThreeExpeditions(bool revised)
        {
            float landRate = revised ? LandMovingEnergyPerSecond : BaselineLandMovingEnergyPerSecond;
            float swimRate = revised ? SwimmingMovingEnergyPerSecond : BaselineSwimmingMovingEnergyPerSecond;
            float meal = revised ? MealEnergyRecovery : BaselineMealEnergyRecovery;
            float rest = revised ? NextDayBaseRecovery : BaselineNextDayBaseRecovery;
            float cost = RepresentativeLandMovingSeconds * landRate +
                         RepresentativeSwimmingMovingSeconds * swimRate +
                         RepresentativeLandNodeCount * LandNodeEnergyCost +
                         RepresentativeWaterNodeCount * WaterNodeEnergyCost;
            float energy = 100f;
            float[] start = new float[3];
            float[] returned = new float[3];
            for (int day = 0; day < 3; day += 1)
            {
                start[day] = energy;
                energy = Math.Max(0f, energy - cost);
                returned[day] = energy;
                energy = Math.Min(100f, energy + meal + rest);
            }
            return new PrototypeO11ExpeditionLedger
            {
                ProfileId = revised ? "o11.revised" : "o10.measured",
                ExpeditionEnergyCost = cost,
                StartEnergy = start,
                ReturnEnergy = returned,
                FourthExpeditionStartEnergy = energy
            };
        }

        public static PrototypeO11ExpeditionLedger SimulateThreeExpeditionsWithoutFood()
        {
            float cost = RepresentativeLandMovingSeconds * LandMovingEnergyPerSecond +
                         RepresentativeSwimmingMovingSeconds * SwimmingMovingEnergyPerSecond +
                         RepresentativeLandNodeCount * LandNodeEnergyCost +
                         RepresentativeWaterNodeCount * WaterNodeEnergyCost;
            float energy = 100f;
            float hunger = 70f;
            float[] start = new float[3];
            float[] returned = new float[3];
            for (int day = 0; day < 3; day += 1)
            {
                start[day] = energy;
                energy = Math.Max(0f, energy - cost);
                returned[day] = energy;
                hunger = Math.Max(0f, hunger - DailyHungerCost);
                if (hunger <= 0f) energy = Math.Max(0f, energy - StarvationEnergyCost);
                energy = Math.Min(100f, energy + NextDayBaseRecovery);
            }
            return new PrototypeO11ExpeditionLedger
            {
                ProfileId = "o11.no-food-pressure",
                ExpeditionEnergyCost = cost,
                StartEnergy = start,
                ReturnEnergy = returned,
                FourthExpeditionStartEnergy = energy
            };
        }

        public static PrototypeO11RouteBand CaptureRouteBand(bool proposedRaft)
        {
            PrototypeO11RaftCostProfile liveProfile = DetectRaftCostProfile();
            bool useO11RaftCosts = proposedRaft || liveProfile == PrototypeO11RaftCostProfile.O11Applied;
            var all = new Dictionary<string, List<PrototypeO11RouteBurden>>(StringComparer.Ordinal);
            foreach (string route in new[] { "escape.raft", "escape.smoke", "escape.radio" })
                all.Add(route, new List<PrototypeO11RouteBurden>());
            foreach (int seed in RepresentativeSeeds)
            {
                foreach (string route in all.Keys.ToArray())
                    all[route].Add(BuildRouteBurden(route, seed, useO11RaftCosts));
            }

            float[] shortest = all.Values.Select(values => values.Min(value => value.BurdenScore)).ToArray();
            float[] medians = all.Values.Select(values => Median(values.Select(value => value.BurdenScore))).ToArray();
            float shortestRatio = shortest.Min() / shortest.Max();
            float medianRatio = medians.Min() / medians.Max();
            int representativeSeed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            return new PrototypeO11RouteBand
            {
                ProfileId = proposedRaft
                    ? "o11.raft-patch-proposed"
                    : liveProfile == PrototypeO11RaftCostProfile.O11Applied ? "o11.live" : "o10.live",
                Representative = all.Keys.Select(route => BuildRouteBurden(route, representativeSeed, useO11RaftCosts)).ToArray(),
                MinimumShortestRatio = shortestRatio,
                MinimumMedianRatio = medianRatio,
                Passes = shortestRatio >= MinimumRouteBurdenRatio && medianRatio >= MinimumRouteBurdenRatio
            };
        }

        public static bool RunContractProbe(out string detail)
        {
            PrototypeO11ExpeditionLedger before = SimulateThreeExpeditions(false);
            PrototypeO11ExpeditionLedger after = SimulateThreeExpeditions(true);
            PrototypeO11ExpeditionLedger noFood = SimulateThreeExpeditionsWithoutFood();
            PrototypeO11RouteBand live = CaptureRouteBand(false);
            PrototypeO11RouteBand proposal = CaptureRouteBand(true);
            bool baselineReproduced = Approximately(before.ExpeditionEnergyCost, 40.1f) &&
                                      Approximately(before.FourthExpeditionStartEnergy, 39.7f);
            bool survivalPass = after.FourthExpeditionStartEnergy >= MinimumFourthExpeditionStartEnergy &&
                                noFood.FourthExpeditionStartEnergy < MinimumFourthExpeditionStartEnergy;
            PrototypeO11RaftCostProfile raftCostProfile = DetectRaftCostProfile();
            bool routeIntegrationPass = RouteIntegrationSatisfied(raftCostProfile, live.Passes, proposal.Passes);
            bool bothIntegrationBranchesCovered =
                RouteIntegrationSatisfied(PrototypeO11RaftCostProfile.O10Pending, false, proposal.Passes) &&
                RouteIntegrationSatisfied(PrototypeO11RaftCostProfile.O11Applied, proposal.Passes, proposal.Passes) &&
                !RouteIntegrationSatisfied(PrototypeO11RaftCostProfile.Unknown, true, true) &&
                !RouteIntegrationSatisfied(PrototypeO11RaftCostProfile.O10Pending, true, true) &&
                !RouteIntegrationSatisfied(PrototypeO11RaftCostProfile.O11Applied, false, true);
            bool finiteStockPass = VerifyFiniteStockAndProtectedParts();
            bool catalogPass = VerifyMeasuredRouteCatalog(raftCostProfile);
            bool sessionWiringPass = VerifyGameSessionSurvivalWiring(out string sessionWiringDetail);
            string routeDetail = string.Join(",", live.Representative.Select((value, index) =>
                value.EscapeId + ":" + value.BurdenScore.ToString("0.00", CultureInfo.InvariantCulture) +
                ">" + proposal.Representative[index].BurdenScore.ToString("0.00", CultureInfo.InvariantCulture) +
                "@" + value.PreparationDays + "+" + value.WaitDays));
            bool passed = baselineReproduced && survivalPass && routeIntegrationPass &&
                          bothIntegrationBranchesCovered && finiteStockPass && catalogPass && sessionWiringPass;
            detail = string.Format(
                CultureInfo.InvariantCulture,
                "contract={0}; energy=before(cost{1:0.0},d4{2:0.0})/after(cost{3:0.0},d4{4:0.0})/noFood(d4{5:0.0}); route=profile{6}/live(short{7:0.000},median{8:0.000})/proposal(short{9:0.000},median{10:0.000})[{11}]; integration={12}/branches={13}; stockParts={14}; catalog={15}; session={16}",
                ContractId,
                before.ExpeditionEnergyCost,
                before.FourthExpeditionStartEnergy,
                after.ExpeditionEnergyCost,
                after.FourthExpeditionStartEnergy,
                noFood.FourthExpeditionStartEnergy,
                raftCostProfile,
                live.MinimumShortestRatio,
                live.MinimumMedianRatio,
                proposal.MinimumShortestRatio,
                proposal.MinimumMedianRatio,
                routeDetail,
                routeIntegrationPass,
                bothIntegrationBranchesCovered,
                finiteStockPass,
                catalogPass,
                sessionWiringDetail);
            return passed;
        }

        public static PrototypeO11RaftCostProfile DetectRaftCostProfile()
        {
            bool o10 = PrototypeRaftEscapeConfig.HullWoodCost == 2 &&
                       PrototypeRaftEscapeConfig.HullSalvageCost == 1 &&
                       PrototypeRaftEscapeConfig.SailWoodCost == 1 &&
                       PrototypeRaftEscapeConfig.SailSalvageCost == 1 &&
                       PrototypeRaftEscapeConfig.SuppliesFoodCost == 2;
            if (o10) return PrototypeO11RaftCostProfile.O10Pending;

            bool o11 = PrototypeRaftEscapeConfig.HullWoodCost == ProposedRaftHullWoodCost &&
                       PrototypeRaftEscapeConfig.HullSalvageCost == ProposedRaftHullSalvageCost &&
                       PrototypeRaftEscapeConfig.SailWoodCost == ProposedRaftSailWoodCost &&
                       PrototypeRaftEscapeConfig.SailSalvageCost == ProposedRaftSailSalvageCost &&
                       PrototypeRaftEscapeConfig.SuppliesFoodCost == ProposedRaftSuppliesFoodCost;
            return o11 ? PrototypeO11RaftCostProfile.O11Applied : PrototypeO11RaftCostProfile.Unknown;
        }

        public static bool RouteIntegrationSatisfied(
            PrototypeO11RaftCostProfile profile,
            bool livePasses,
            bool proposalPasses)
        {
            switch (profile)
            {
                case PrototypeO11RaftCostProfile.O10Pending:
                    return !livePasses && proposalPasses;
                case PrototypeO11RaftCostProfile.O11Applied:
                    return livePasses;
                default:
                    return false;
            }
        }

        private static bool VerifyGameSessionSurvivalWiring(out string detail)
        {
            GameSession fed = new GameSession();
            GameSession noFood = new GameSession();
            bool passed = true;
            for (int day = 0; day < 3; day += 1)
            {
                passed &= ExecuteRepresentativeExpedition(fed, true) && fed.UseFood() &&
                          fed.EndDay(false, false, false, false);
                passed &= ExecuteRepresentativeExpedition(noFood, false) &&
                          noFood.EndDay(false, false, false, false);
            }
            passed &= fed.Day == 4 && Approximately(fed.Energy, 100f) &&
                      noFood.Day == 4 && Approximately(noFood.Energy, 35f);
            detail = "fedDay" + fed.Day + "/energy" + fed.Energy.ToString("0.0", CultureInfo.InvariantCulture) +
                     ",noFoodDay" + noFood.Day + "/energy" + noFood.Energy.ToString("0.0", CultureInfo.InvariantCulture);
            return passed;
        }

        private static bool ExecuteRepresentativeExpedition(GameSession session, bool gatherMeal)
        {
            if (!session.BeginSearch(PrototypeExpeditionRegionId.Beach)) return false;
            session.TickSearch(RepresentativeLandMovingSeconds, true);
            if (!session.SetSwimming(true)) return false;
            session.TickSearch(RepresentativeSwimmingMovingSeconds, true);
            if (!session.SetSwimming(false)) return false;
            for (int node = 0; node < RepresentativeLandNodeCount; node += 1)
            {
                if (!session.TryApplySearchNodeCost(LandNodeEnergyCost, LandNodeDaylightCost)) return false;
            }
            if (!session.TryApplySearchNodeCost(WaterNodeEnergyCost, WaterNodeDaylightCost)) return false;
            if (gatherMeal && session.TryGather(ResourceKind.Food, 1) != GatherResult.Added) return false;
            return session.ReturnToCamp(false);
        }

        private static PrototypeO11RouteBurden BuildRouteBurden(string route, int seed, bool proposedRaft)
        {
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(route);
            int common;
            int food;
            int advanced;
            int commits;
            if (string.Equals(route, "escape.raft", StringComparison.Ordinal))
            {
                common = proposedRaft ? 20 : 16;
                food = proposedRaft ? ProposedRaftSuppliesFoodCost : PrototypeRaftEscapeConfig.SuppliesFoodCost;
                advanced = 0;
                commits = 5;
            }
            else if (string.Equals(route, "escape.smoke", StringComparison.Ordinal))
            {
                common = 26;
                food = 0;
                advanced = 4;
                commits = 3;
            }
            else
            {
                common = 13;
                food = 0;
                advanced = 5;
                commits = 3;
            }
            int openDay = string.Equals(route, "escape.raft", StringComparison.Ordinal)
                ? PrototypeRaftLaunchWindowResolver.FindNextOpenDay(seed, definition.PreparationDays)
                : PrototypeSignalEscapeWindowResolver.NextAllowedDay(route, seed, definition.PreparationDays);
            int wait = Math.Max(0, openDay - definition.PreparationDays);
            float score = common * CommonResourceWeight + food * FoodWeight + advanced * AdvancedResourceWeight +
                          definition.RequiredKeyPartIds.Length * ProtectedPartWeight +
                          definition.RegionIds.Length * RequiredRegionWeight +
                          definition.ResearchIds.Length * ResearchActionWeight +
                          commits * ProjectCommitWeight + wait * WaitDayWeight;
            return new PrototypeO11RouteBurden
            {
                EscapeId = route,
                CommonResourceUnits = common,
                FoodUnits = food,
                AdvancedResourceUnits = advanced,
                KeyPartCount = definition.RequiredKeyPartIds.Length,
                RequiredRegionCount = definition.RegionIds.Length,
                ResearchActionCount = definition.ResearchIds.Length,
                ProjectCommitCount = commits,
                PreparationDays = definition.PreparationDays,
                WaitDays = wait,
                BurdenScore = score
            };
        }

        private static bool VerifyMeasuredRouteCatalog(PrototypeO11RaftCostProfile raftCostProfile)
        {
            PrototypeEscapeProjectDefinition smoke = PrototypeEscapeProjectCatalog.Get("escape.smoke");
            PrototypeEscapeProjectDefinition radio = PrototypeEscapeProjectCatalog.Get("escape.radio");
            return raftCostProfile != PrototypeO11RaftCostProfile.Unknown &&
                   PrototypeRaftEscapeConfig.LaunchAttemptFoodCost == 0 &&
                   smoke.StableCosts.Sum(value => value.Amount) == 16 &&
                   radio.StableCosts.Sum(value => value.Amount) == 5 &&
                   smoke.RequiredKeyPartIds.Length == 1 && radio.RequiredKeyPartIds.Length == 3;
        }

        private static bool VerifyFiniteStockAndProtectedParts()
        {
            bool seedsPass = RepresentativeSeeds.All(seed =>
                PrototypeSearchRegionCatalog.GeneralStockUnitsForSeed(seed) == PrototypeO7SearchBalance.ExpectedGeneralStockUnits &&
                PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                    seed,
                    PrototypeSearchRegionCatalog.ContractRevision).Length == 5);
            PrototypeO7WoodRouteBudget raftBudget = PrototypeO7SearchBalance
                .BuildRepresentativeWoodBudgets(PrototypeExpeditionRegionCatalog.DefaultRunSeed)
                .First(value => string.Equals(value.EscapeId, "escape.raft", StringComparison.Ordinal));
            int proposedExtraWood = Math.Max(0, ProposedRaftHullWoodCost - PrototypeRaftEscapeConfig.HullWoodCost) +
                                    Math.Max(0, ProposedRaftSailWoodCost - PrototypeRaftEscapeConfig.SailWoodCost);
            return seedsPass && raftBudget.SpareWood - proposedExtraWood >= PrototypeO7SearchBalance.MinimumRouteSpareWood;
        }

        private static float Median(IEnumerable<float> values)
        {
            float[] sorted = values.OrderBy(value => value).ToArray();
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) * 0.5f : sorted[middle];
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= 0.01f;
        }
    }
}
