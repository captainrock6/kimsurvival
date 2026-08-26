using System;
using System.Collections.Generic;
using UnityEngine;

namespace KimSurvival
{
    public enum CampModulePresentationState
    {
        Locked,
        Available,
        InProgress,
        Completed
    }

    public readonly struct CampModuleResourceShortage
    {
        public CampModuleResourceShortage(int wood, int stone, int food, int salvage)
        {
            Wood = Mathf.Max(0, wood);
            Stone = Mathf.Max(0, stone);
            Food = Mathf.Max(0, food);
            Salvage = Mathf.Max(0, salvage);
        }

        public int Wood { get; }
        public int Stone { get; }
        public int Food { get; }
        public int Salvage { get; }

        public int Total
        {
            get { return Wood + Stone + Food + Salvage; }
        }

        public bool Any
        {
            get { return Total > 0; }
        }
    }

    public readonly struct CampModuleSemanticDefinition
    {
        public CampModuleSemanticDefinition(
            CampModuleArchetype archetype,
            string stableId,
            string nameKey,
            string purposeKey,
            string suggestedUseKey,
            string placementCapacityKey,
            int estimatedGeneralFacilityCapacity)
        {
            Archetype = archetype;
            StableId = stableId ?? string.Empty;
            NameKey = nameKey ?? string.Empty;
            PurposeKey = purposeKey ?? string.Empty;
            SuggestedUseKey = suggestedUseKey ?? string.Empty;
            PlacementCapacityKey = placementCapacityKey ?? string.Empty;
            EstimatedGeneralFacilityCapacity = Mathf.Max(0, estimatedGeneralFacilityCapacity);
        }

        public CampModuleArchetype Archetype { get; }
        public string StableId { get; }
        public string NameKey { get; }
        public string PurposeKey { get; }
        public string SuggestedUseKey { get; }
        public string PlacementCapacityKey { get; }
        public int EstimatedGeneralFacilityCapacity { get; }
    }

    public static class PrototypeCampModuleSemanticCatalog
    {
        private static readonly CampModuleSemanticDefinition[] Definitions =
        {
            new CampModuleSemanticDefinition(
                CampModuleArchetype.Upper,
                "module.upper",
                "module.name.upper",
                "module.purpose.upper",
                "module.suggested_use.upper",
                "module.capacity.general_sheltered",
                2),
            new CampModuleSemanticDefinition(
                CampModuleArchetype.Side,
                "module.side",
                "module.name.side",
                "module.purpose.side",
                "module.suggested_use.side",
                "module.capacity.general_sheltered",
                2),
            new CampModuleSemanticDefinition(
                CampModuleArchetype.Basement,
                "module.basement",
                "module.name.basement",
                "module.purpose.basement",
                "module.suggested_use.basement",
                "module.capacity.general_sheltered",
                2)
        };

        public static IReadOnlyList<CampModuleSemanticDefinition> All
        {
            get { return Definitions; }
        }

        public static CampModuleSemanticDefinition Get(CampModuleArchetype archetype)
        {
            return Definitions[(int)archetype];
        }
    }

    public readonly struct CampModuleExpansionOptionViewModel
    {
        public CampModuleExpansionOptionViewModel(
            CampModuleSemanticDefinition semantic,
            CampModuleEvaluation evaluation,
            CampModulePresentationState state,
            string stateKey,
            string prerequisiteKey,
            string reasonKey,
            CampModuleResourceShortage shortage,
            int minimumDay,
            bool requiresWorkbench,
            bool prerequisitesMet,
            float usablePlacementWidth,
            bool allowsOpenSkyFacilities)
        {
            Semantic = semantic;
            Evaluation = evaluation;
            State = state;
            StateKey = stateKey ?? string.Empty;
            PrerequisiteKey = prerequisiteKey ?? string.Empty;
            ReasonKey = reasonKey ?? string.Empty;
            Shortage = shortage;
            MinimumDay = Mathf.Max(1, minimumDay);
            RequiresWorkbench = requiresWorkbench;
            PrerequisitesMet = prerequisitesMet;
            UsablePlacementWidth = Mathf.Max(0f, usablePlacementWidth);
            AllowsOpenSkyFacilities = allowsOpenSkyFacilities;
        }

        public CampModuleSemanticDefinition Semantic { get; }
        public CampModuleEvaluation Evaluation { get; }
        public CampModulePresentationState State { get; }
        public string StateKey { get; }
        public string PrerequisiteKey { get; }
        public string ReasonKey { get; }
        public CampModuleResourceShortage Shortage { get; }
        public int MinimumDay { get; }
        public bool RequiresWorkbench { get; }
        public bool PrerequisitesMet { get; }
        public float UsablePlacementWidth { get; }
        public bool AllowsOpenSkyFacilities { get; }

        public CampModuleArchetype Archetype
        {
            get { return Semantic.Archetype; }
        }

        public CampModuleResourceCost Cost
        {
            get { return Evaluation.Cost; }
        }

        public int EstimatedGeneralFacilityCapacity
        {
            get { return Semantic.EstimatedGeneralFacilityCapacity; }
        }

        public bool CanCommit
        {
            get { return State != CampModulePresentationState.Completed && Evaluation.CanCommit; }
        }
    }

    public readonly struct CampModuleRecommendationViewModel
    {
        public CampModuleRecommendationViewModel(
            bool hasRecommendation,
            CampModuleArchetype archetype,
            string nameKey,
            string reasonKey)
        {
            HasRecommendation = hasRecommendation;
            Archetype = archetype;
            NameKey = nameKey ?? string.Empty;
            ReasonKey = reasonKey ?? string.Empty;
        }

        public bool HasRecommendation { get; }
        public CampModuleArchetype Archetype { get; }
        public string NameKey { get; }
        public string ReasonKey { get; }
    }

    public readonly struct CampModuleExpansionOverviewViewModel
    {
        public CampModuleExpansionOverviewViewModel(
            CampModuleExpansionOptionViewModel[] options,
            CampModuleRecommendationViewModel recommendation,
            int completedCount)
        {
            Options = options ?? Array.Empty<CampModuleExpansionOptionViewModel>();
            Recommendation = recommendation;
            CompletedCount = Mathf.Max(0, completedCount);
        }

        public CampModuleExpansionOptionViewModel[] Options { get; }
        public CampModuleRecommendationViewModel Recommendation { get; }
        public int CompletedCount { get; }
    }

    public sealed class PrototypeCampModuleExpansionPresenter
    {
        private static readonly CampModuleArchetype[] RecommendationOrder =
        {
            CampModuleArchetype.Upper,
            CampModuleArchetype.Basement,
            CampModuleArchetype.Side
        };

        private readonly PrototypeCampModuleExpansion expansion;

        public PrototypeCampModuleExpansionPresenter(PrototypeCampModuleExpansion expansion)
        {
            this.expansion = expansion ?? throw new ArgumentNullException(nameof(expansion));
        }

        public CampModuleExpansionOptionViewModel BuildSelected(
            GameSession session,
            CampModuleValidationContext context)
        {
            return BuildOption(session, context, expansion.SelectedArchetype);
        }

        public CampModuleExpansionOptionViewModel BuildOption(
            GameSession session,
            CampModuleValidationContext context,
            CampModuleArchetype archetype)
        {
            CampModuleSemanticDefinition semantic = PrototypeCampModuleSemanticCatalog.Get(archetype);
            CampModuleEvaluation evaluation = expansion.Evaluate(session, context, archetype);
            CampModuleUnlockRequirement requirement = expansion.Config.UnlockRequirement;
            bool prerequisitesMet = requirement.IsMet(session);
            CampModulePresentationState state = ResolveState(archetype, evaluation);
            CampModuleDefinition definition = evaluation.Definition;

            return new CampModuleExpansionOptionViewModel(
                semantic,
                evaluation,
                state,
                StateKey(state),
                PrerequisiteKey(requirement, session, prerequisitesMet),
                ReasonKey(state, evaluation),
                CalculateShortage(session, evaluation.Cost),
                requirement.MinimumDay,
                requirement.RequiresWorkbench,
                prerequisitesMet,
                definition.GeneralFloorDisplayMaximumX - definition.GeneralFloorDisplayMinimumX,
                false);
        }

        public CampModuleExpansionOverviewViewModel BuildOverview(
            GameSession session,
            CampModuleValidationContext context)
        {
            var options = new CampModuleExpansionOptionViewModel[RecommendationOrder.Length];
            for (int index = 0; index < RecommendationOrder.Length; index += 1)
            {
                options[index] = BuildOption(session, context, RecommendationOrder[index]);
            }

            CampModuleRecommendationViewModel recommendation = Recommend(options);
            return new CampModuleExpansionOverviewViewModel(
                options,
                recommendation,
                expansion.CommittedModuleCount);
        }

        private CampModulePresentationState ResolveState(
            CampModuleArchetype archetype,
            CampModuleEvaluation evaluation)
        {
            if (expansion.IsCommitted(archetype))
            {
                return CampModulePresentationState.Completed;
            }
            if (expansion.IsPreviewActive && expansion.SelectedArchetype == archetype)
            {
                return CampModulePresentationState.InProgress;
            }
            if (evaluation.Geometry != CampModuleGeometryStatus.Valid ||
                evaluation.Economy == CampModuleEconomyStatus.CostUnset ||
                evaluation.Economy == CampModuleEconomyStatus.Locked ||
                evaluation.Economy == CampModuleEconomyStatus.PrototypeLimit)
            {
                return CampModulePresentationState.Locked;
            }
            return CampModulePresentationState.Available;
        }

        private static string StateKey(CampModulePresentationState state)
        {
            switch (state)
            {
                case CampModulePresentationState.Available:
                    return "module.state.available";
                case CampModulePresentationState.InProgress:
                    return "module.state.in_progress";
                case CampModulePresentationState.Completed:
                    return "module.state.completed";
                default:
                    return "module.state.locked";
            }
        }

        private static string PrerequisiteKey(
            CampModuleUnlockRequirement requirement,
            GameSession session,
            bool prerequisitesMet)
        {
            if (prerequisitesMet)
            {
                return "module.prerequisite.met";
            }

            bool dayMissing = session == null || session.Day < requirement.MinimumDay;
            bool workbenchMissing = requirement.RequiresWorkbench &&
                                    (session == null || !session.HasStructure(StructureKind.Workbench));
            if (dayMissing && workbenchMissing)
            {
                return "module.prerequisite.workbench_day";
            }
            return workbenchMissing
                ? "module.prerequisite.workbench"
                : "module.prerequisite.day";
        }

        private static string ReasonKey(
            CampModulePresentationState state,
            CampModuleEvaluation evaluation)
        {
            if (state == CampModulePresentationState.Completed)
            {
                return "module.reason.completed";
            }

            string geometryReason = PrototypeCampModuleReasonKeys.Geometry(evaluation.Geometry);
            return string.IsNullOrEmpty(geometryReason)
                ? PrototypeCampModuleReasonKeys.Economy(evaluation.Economy)
                : geometryReason;
        }

        private static CampModuleResourceShortage CalculateShortage(
            GameSession session,
            CampModuleResourceCost cost)
        {
            return new CampModuleResourceShortage(
                cost.Wood - Storage(session, ResourceKind.Wood),
                cost.Stone - Storage(session, ResourceKind.Stone),
                cost.Food - Storage(session, ResourceKind.Food),
                cost.Salvage - Storage(session, ResourceKind.Salvage));
        }

        private static int Storage(GameSession session, ResourceKind kind)
        {
            return session == null ? 0 : session.GetStorage(kind);
        }

        private static CampModuleRecommendationViewModel Recommend(
            IReadOnlyList<CampModuleExpansionOptionViewModel> options)
        {
            int bestIndex = -1;
            int bestRank = int.MaxValue;
            for (int index = 0; index < options.Count; index += 1)
            {
                CampModuleExpansionOptionViewModel option = options[index];
                if (option.State == CampModulePresentationState.Completed)
                {
                    continue;
                }

                int rank = RecommendationRank(option);
                if (rank < bestRank)
                {
                    bestRank = rank;
                    bestIndex = index;
                }
            }

            if (bestIndex < 0)
            {
                return new CampModuleRecommendationViewModel(
                    false,
                    default(CampModuleArchetype),
                    string.Empty,
                    "module.recommend.none");
            }

            CampModuleExpansionOptionViewModel recommended = options[bestIndex];
            return new CampModuleRecommendationViewModel(
                true,
                recommended.Archetype,
                recommended.Semantic.NameKey,
                RecommendationReasonKey(recommended));
        }

        private static int RecommendationRank(CampModuleExpansionOptionViewModel option)
        {
            if (option.CanCommit) return 0;
            if (option.State == CampModulePresentationState.InProgress) return 1;
            if (option.State == CampModulePresentationState.Available) return 10 + option.Shortage.Total;
            return 100;
        }

        private static string RecommendationReasonKey(CampModuleExpansionOptionViewModel option)
        {
            if (option.CanCommit) return "module.recommend.ready";
            if (option.State == CampModulePresentationState.Locked) return "module.recommend.unlock";
            if (option.Shortage.Any) return "module.recommend.gather";
            return "module.recommend.resolve";
        }

        public static bool RunContractProbe(out string detail)
        {
            var expansion = new PrototypeCampModuleExpansion(
                PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance());
            var presenter = new PrototypeCampModuleExpansionPresenter(expansion);
            var context = new CampModuleValidationContext();
            var session = new GameSession();

            CampModuleExpansionOptionViewModel locked = presenter.BuildOption(
                session,
                context,
                CampModuleArchetype.Upper);
            if (locked.State != CampModulePresentationState.Locked ||
                locked.PrerequisiteKey != "module.prerequisite.workbench" ||
                locked.Cost.Wood != 1 ||
                locked.EstimatedGeneralFacilityCapacity != 2 ||
                !Mathf.Approximately(locked.UsablePlacementWidth, 8f))
            {
                detail = "Locked upper-room semantics did not expose prerequisite, cost, and capacity.";
                return false;
            }

            session.Grant(ResourceKind.Salvage, 1);
            if (!session.TryBuild(StructureKind.Workbench))
            {
                detail = "Probe could not build the workbench prerequisite.";
                return false;
            }
            session.Grant(ResourceKind.Wood, 3);

            CampModuleExpansionOptionViewModel available = presenter.BuildOption(
                session,
                context,
                CampModuleArchetype.Upper);
            if (available.State != CampModulePresentationState.Available ||
                !available.CanCommit || !available.PrerequisitesMet ||
                available.StateKey != "module.state.available")
            {
                detail = "Unlocked, affordable upper room did not surface an available state.";
                return false;
            }

            CampModuleReturnSnapshot snapshot = new CampModuleReturnSnapshot(
                Vector2.zero,
                1f,
                PrototypeCampModuleCatalog.StartRoomId);
            if (!expansion.BeginPreview(snapshot, CampModuleArchetype.Upper))
            {
                detail = "Probe could not begin upper-room preview.";
                return false;
            }

            CampModuleExpansionOptionViewModel preview = presenter.BuildSelected(session, context);
            if (preview.State != CampModulePresentationState.InProgress || !preview.CanCommit ||
                preview.StateKey != "module.state.in_progress")
            {
                detail = "Selected preview did not surface an in-progress, commit-ready state.";
                return false;
            }

            if (expansion.TryCommit(session, context) != CampModuleCommitStatus.Succeeded)
            {
                detail = "Probe could not commit the upper-room expansion.";
                return false;
            }

            CampModuleExpansionOptionViewModel completed = presenter.BuildOption(
                session,
                context,
                CampModuleArchetype.Upper);
            CampModuleExpansionOverviewViewModel overview = presenter.BuildOverview(session, context);
            if (completed.State != CampModulePresentationState.Completed || completed.CanCommit ||
                !overview.Recommendation.HasRecommendation ||
                overview.Recommendation.Archetype != CampModuleArchetype.Basement)
            {
                detail = "Completion state or deterministic next-expansion recommendation was incorrect.";
                return false;
            }

            var blockedContext = new CampModuleValidationContext { RequiredPathClear = false };
            CampModuleExpansionOptionViewModel blocked = presenter.BuildOption(
                session,
                blockedContext,
                CampModuleArchetype.Side);
            if (blocked.State != CampModulePresentationState.Locked ||
                blocked.ReasonKey != "interaction.module.path_blocked")
            {
                detail = "Invalid geometry did not surface a stable blocking-reason key.";
                return false;
            }

            detail = "PASS locked/available-preview/completed, cost/prerequisite/reason, capacity, recommendation.";
            return true;
        }
    }
}
