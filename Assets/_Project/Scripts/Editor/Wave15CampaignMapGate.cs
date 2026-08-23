using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace KimSurvival.EditorTools
{
    public static class Wave15CampaignMapGate
    {
        private const string DefaultEvidenceFolder = "Artifacts/Verification/wave15-campaign-map";
        private const string LocalizationTablePath = "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv";

        [MenuItem("Kim Survival/Run Wave 15 Campaign Map Contracts")]
        public static void RunContracts()
        {
            string evidenceFolder = Environment.GetEnvironmentVariable("KIM_SURVIVAL_VERIFICATION_FOLDER");
            if (string.IsNullOrWhiteSpace(evidenceFolder))
            {
                evidenceFolder = DefaultEvidenceFolder;
            }

            Directory.CreateDirectory(evidenceFolder);
            List<string> failures = new List<string>();
            Check(GameSession.FinalDay == 50, "standard_deadline_day_50", failures);
            Check(Enum.IsDefined(typeof(PrototypeCampInteractionTargetKind), "ExpeditionMap"), "camp_map_interaction_target", failures);

            Assembly runtimeAssembly = typeof(GameSession).Assembly;
            Check(runtimeAssembly.GetType("KimSurvival.PrototypeExpeditionRegionCatalog") != null, "three_region_profile_catalog", failures);
            Check(runtimeAssembly.GetType("KimSurvival.PrototypeExpeditionMapActions") != null, "shared_map_action_snapshot", failures);
            Check(typeof(GameSession).GetProperty("RunSeed") != null, "run_seed_state", failures);
            Check(typeof(GameSession).GetProperty("SelectedRegionId") != null, "selected_region_state", failures);

            string localizationSource = File.ReadAllText(LocalizationTablePath);
            Check(localizationSource.Contains("camp.target.expedition_map\t"), "localized_map_target", failures);
            Check(localizationSource.Contains("expedition.region.beach.name\t") &&
                  localizationSource.Contains("expedition.region.forest.name\t") &&
                  localizationSource.Contains("expedition.region.shallows.name\t"), "localized_three_region_names", failures);

            GameSession deadline = new GameSession();
            deadline.Grant(ResourceKind.Food, GameSession.FinalDay);
            bool day49Continues = true;
            for (int day = 1; day < GameSession.FinalDay; day += 1)
            {
                day49Continues &= deadline.BeginSearch(PrototypeExpeditionRegionId.Beach) &&
                                  deadline.ReturnToCamp(false) && deadline.UseFood() && deadline.EndDay() &&
                                  deadline.Result == RunResult.None && deadline.Day == day + 1;
            }
            Check(day49Continues && deadline.Day == 50 && deadline.Result == RunResult.None,
                "day_49_settlement_continues", failures);
            Check(deadline.BeginSearch(PrototypeExpeditionRegionId.Shallows) && deadline.ReturnToCamp(false) &&
                  deadline.UseFood() && deadline.EndDay() && deadline.Day == 50 && deadline.Result == RunResult.Deadline,
                "day_50_settlement_terminal", failures);

            GameSession earlyRescue = new GameSession();
            earlyRescue.Grant(ResourceKind.Wood, 20);
            earlyRescue.Grant(ResourceKind.Salvage, 20);
            Check(earlyRescue.TryBuild(StructureKind.Workbench) && earlyRescue.TryResearch(TechKind.Rope) &&
                  earlyRescue.TryCraft(TechKind.Rope) && earlyRescue.TryUpgradeSignal() && earlyRescue.TryUpgradeSignal() &&
                  earlyRescue.Result == RunResult.Rescued && earlyRescue.Day == 1,
                "early_rescue_precedes_deadline", failures);

            IReadOnlyList<PrototypeExpeditionRegionProfile> profiles = PrototypeExpeditionRegionCatalog.All;
            Check(profiles.Count == 3 && profiles[0].Id == PrototypeExpeditionRegionId.Beach &&
                  profiles[1].Id == PrototypeExpeditionRegionId.Forest && profiles[2].Id == PrototypeExpeditionRegionId.Shallows,
                "three_regions_in_stable_order", failures);
            int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
            bool differentSeedVaries = false;
            for (int profileIndex = 0; profileIndex < profiles.Count; profileIndex += 1)
            {
                PrototypeExpeditionRegionProfile profile = profiles[profileIndex];
                for (int nodeIndex = 0; nodeIndex < profile.NodeCount; nodeIndex += 1)
                {
                    PrototypeExpeditionNodeResult first = profile.ResolveNode(seed, nodeIndex);
                    PrototypeExpeditionNodeResult second = profile.ResolveNode(seed, nodeIndex);
                    PrototypeExpeditionNodeResult alternate = profile.ResolveNode(seed + 1, nodeIndex);
                    Check(first.ActionId == second.ActionId && first.Resource == second.Resource && first.Amount == second.Amount &&
                          first.Water == second.Water && first.ResultId == second.ResultId,
                        "deterministic_seed_profile_action_" + profile.StableId + "_" + nodeIndex, failures);
                    differentSeedVaries |= first.Resource != alternate.Resource || first.Amount != alternate.Amount ||
                                           first.ResultId != alternate.ResultId;
                }
            }
            Check(differentSeedVaries, "different_seed_varies_within_profile", failures);

            PrototypeExpeditionSeedManifest manifest = PrototypeExpeditionRegionCatalog.BuildSeedManifest(seed);
            HashSet<string> routeIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> coreIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<PrototypeExpeditionRegionId> regions = new HashSet<PrototypeExpeditionRegionId>();
            for (int guaranteeIndex = 0; guaranteeIndex < manifest.Guarantees.Count; guaranteeIndex += 1)
            {
                PrototypeEscapeRouteGuarantee guarantee = manifest.Guarantees[guaranteeIndex];
                routeIds.Add(guarantee.EscapeRouteId);
                coreIds.Add(guarantee.CoreResultId);
                regions.Add(guarantee.Region);
                PrototypeExpeditionRegionProfile profile = PrototypeExpeditionRegionCatalog.Get(guarantee.Region);
                Check(profile.ResolveNode(seed, profile.NodeCount - 1).ResultId == guarantee.CoreResultId,
                    "guaranteed_core_reachable_" + guarantee.EscapeRouteId, failures);
            }
            Check(manifest.HasMinimumSoftlockProtection && routeIds.Count == 3 && coreIds.Count == 3 && regions.Count == 3,
                "three_escape_routes_softlock_protected", failures);

            PrototypeExpeditionMapActions keyboardMap = PrototypeExpeditionMapActions.FromRaw(new PrototypeRawExpeditionMapInput
            {
                KeyboardNext = true,
                KeyboardConfirm = true,
                KeyboardCancel = true
            });
            PrototypeExpeditionMapActions gamepadMap = PrototypeExpeditionMapActions.FromRaw(new PrototypeRawExpeditionMapInput
            {
                HorizontalAxis = 1f,
                GamepadConfirm = true,
                GamepadCancel = true
            });
            Check(keyboardMap.CycleDirection == gamepadMap.CycleDirection && keyboardMap.ConfirmPressed &&
                  gamepadMap.ConfirmPressed && keyboardMap.CancelPressed && gamepadMap.CancelPressed,
                "shared_keyboard_gamepad_map_actions", failures);

            PrototypeCampInteraction interaction = new PrototypeCampInteraction();
            Vector2 mapPosition = new Vector2(2f, PrototypeCampUse.PlayerFloorY);
            PrototypeCampInteractionTarget[] targets =
            {
                new PrototypeCampInteractionTarget("camp.expedition-map", PrototypeCampInteractionTargetKind.ExpeditionMap, mapPosition)
            };
            interaction.UpdateSelection(new Vector2(mapPosition.x - PrototypeCampUse.UseRange - 0.01f, mapPosition.y), 1f, targets);
            Check(!interaction.HasProximityPrompt && !interaction.IsPopupOpen, "map_hidden_outside_proximity", failures);
            interaction.UpdateSelection(new Vector2(mapPosition.x - PrototypeCampUse.UseRange, mapPosition.y), 1f, targets);
            Check(interaction.HasProximityPrompt && interaction.ActiveTargetId == "camp.expedition-map" && interaction.TryOpenPopup(),
                "map_opens_at_exact_proximity", failures);
            interaction.ClosePopup();
            interaction.UpdateSelection(new Vector2(mapPosition.x - PrototypeCampUse.UseRange, mapPosition.y), 1f, targets);
            Check(interaction.ActiveTargetId == "camp.expedition-map" && interaction.HasProximityPrompt,
                "map_cancel_restores_target", failures);

            bool passed = failures.Count == 0;
            StringBuilder report = new StringBuilder();
            report.AppendLine(passed ? "PASS" : "EXPECTED_RED");
            report.AppendLine("Wave: 15 campaign map foundation");
            report.AppendLine("Unity: " + Application.unityVersion);
            report.AppendLine("Contracts: Day 49 continues, Day 50 terminal, early rescue priority, proximity map, three region profiles, deterministic seed/action, three-route softlock protection, shared input, localization");
            report.AppendLine("Failure count: " + failures.Count);
            for (int i = 0; i < failures.Count; i += 1)
            {
                report.AppendLine("EXPECTED_GAP: " + failures[i]);
            }

            string evidencePath = Path.Combine(evidenceFolder, "wave15-campaign-map-contracts.txt");
            File.WriteAllText(evidencePath, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            if (!passed)
            {
                throw new InvalidOperationException("Wave 15 campaign map contracts are RED. See " + evidencePath);
            }
        }

        private static void Check(bool condition, string id, ICollection<string> failures)
        {
            if (!condition)
            {
                failures.Add(id);
            }
        }
    }
}
