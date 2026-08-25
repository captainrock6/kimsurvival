using System;
using System.Collections.Generic;
using UnityEngine;

namespace KimSurvival
{
    public enum PrototypeCampInteractionTargetKind
    {
        None,
        Campfire,
        Workbench,
        RainCollector,
        RescueSignal,
        StoragePlanning,
        ExpeditionMap,
        EndingAlbum,
        ModuleExpansionSlot,
        ModuleConnector,
        SmokeBeacon,
        RadioBench,
        ShoreLaunch
    }

    public enum PrototypeCampInteractionAction
    {
        BuildOrRelocate,
        Eat,
        PrepareSurvival,
        ResearchStoneAxe,
        CraftStoneAxe,
        ResearchRope,
        CraftRope,
        Repair,
        UpgradeBag,
        CollectRain,
        UpgradeSignal,
        OpenExpeditionMap,
        OpenEndingAlbum,
        PreviewModule,
        ProgressSmokeEscape,
        ProgressRadioEscape,
        ProgressRaftEscape
    }

    public interface IPrototypeCampInteractionTarget
    {
        string Id { get; }
        PrototypeCampInteractionTargetKind Kind { get; }
        Vector2 Position { get; }
        bool IsEnabled { get; }
        int SelectionPriority { get; }
    }

    public readonly struct PrototypeCampInteractionTarget : IPrototypeCampInteractionTarget
    {
        public PrototypeCampInteractionTarget(
            string id,
            PrototypeCampInteractionTargetKind kind,
            Vector2 position,
            bool isEnabled = true,
            int selectionPriority = 0)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Position = position;
            IsEnabled = isEnabled;
            SelectionPriority = Mathf.Max(0, selectionPriority);
        }

        public string Id { get; }
        public PrototypeCampInteractionTargetKind Kind { get; }
        public Vector2 Position { get; }
        public bool IsEnabled { get; }
        public int SelectionPriority { get; }
    }

    public static class PrototypeCampInteractionCatalog
    {
        public static bool OwnsAction(PrototypeCampInteractionTargetKind target, PrototypeCampInteractionAction action, bool isBuilt)
        {
            if (target == PrototypeCampInteractionTargetKind.None)
            {
                return false;
            }

            if (!isBuilt && target != PrototypeCampInteractionTargetKind.RescueSignal)
            {
                return action == PrototypeCampInteractionAction.BuildOrRelocate ||
                       (target == PrototypeCampInteractionTargetKind.Workbench && action == PrototypeCampInteractionAction.UpgradeBag);
            }

            switch (target)
            {
                case PrototypeCampInteractionTargetKind.StoragePlanning:
                    return action == PrototypeCampInteractionAction.BuildOrRelocate ||
                           action == PrototypeCampInteractionAction.PreviewModule;
                case PrototypeCampInteractionTargetKind.ExpeditionMap:
                    return action == PrototypeCampInteractionAction.OpenExpeditionMap;
                case PrototypeCampInteractionTargetKind.EndingAlbum:
                    return action == PrototypeCampInteractionAction.OpenEndingAlbum;
                case PrototypeCampInteractionTargetKind.ModuleExpansionSlot:
                    return action == PrototypeCampInteractionAction.PreviewModule;
                case PrototypeCampInteractionTargetKind.ModuleConnector:
                    return false;
                case PrototypeCampInteractionTargetKind.SmokeBeacon:
                    return action == PrototypeCampInteractionAction.ProgressSmokeEscape;
                case PrototypeCampInteractionTargetKind.RadioBench:
                    return action == PrototypeCampInteractionAction.ProgressRadioEscape;
                case PrototypeCampInteractionTargetKind.ShoreLaunch:
                    return action == PrototypeCampInteractionAction.ProgressRaftEscape;
                case PrototypeCampInteractionTargetKind.Campfire:
                    return action == PrototypeCampInteractionAction.BuildOrRelocate ||
                           action == PrototypeCampInteractionAction.Eat ||
                           action == PrototypeCampInteractionAction.PrepareSurvival;
                case PrototypeCampInteractionTargetKind.Workbench:
                    return action == PrototypeCampInteractionAction.BuildOrRelocate ||
                           action == PrototypeCampInteractionAction.ResearchStoneAxe ||
                           action == PrototypeCampInteractionAction.CraftStoneAxe ||
                           action == PrototypeCampInteractionAction.ResearchRope ||
                           action == PrototypeCampInteractionAction.CraftRope ||
                           action == PrototypeCampInteractionAction.Repair ||
                           action == PrototypeCampInteractionAction.UpgradeBag;
                case PrototypeCampInteractionTargetKind.RainCollector:
                    return action == PrototypeCampInteractionAction.BuildOrRelocate ||
                           action == PrototypeCampInteractionAction.CollectRain;
                case PrototypeCampInteractionTargetKind.RescueSignal:
                    return action == PrototypeCampInteractionAction.UpgradeSignal;
                default:
                    return false;
            }
        }
    }

    public sealed class PrototypeCampInteraction
    {
        private const float FacingPenalty = 0.28f;
        private const float CurrentTargetHysteresis = 0.08f;
        private const float SelectionPriorityBias = 0.12f;
        private const float DirectionEpsilon = 0.05f;
        private const float DistanceTieEpsilon = 0.01f;
        private const float ScoreEpsilon = 0.0001f;

        private PrototypeCampInteractionTarget activeTarget;
        private PrototypeCampInteractionTarget openPopupTarget;
        private bool confirmationConsumed;

        public PrototypeCampInteractionTargetKind ActiveTargetKind
        {
            get { return activeTarget.Kind; }
        }

        public string ActiveTargetId
        {
            get { return activeTarget.Id ?? string.Empty; }
        }

        public PrototypeCampInteractionTargetKind OpenPopupKind
        {
            get { return openPopupTarget.Kind; }
        }

        public string OpenPopupTargetId
        {
            get { return openPopupTarget.Id ?? string.Empty; }
        }

        public bool HasProximityPrompt
        {
            get { return !IsPopupOpen && ActiveTargetKind != PrototypeCampInteractionTargetKind.None; }
        }

        public bool IsPopupOpen
        {
            get { return OpenPopupKind != PrototypeCampInteractionTargetKind.None; }
        }

        public bool MovementLocked
        {
            get { return IsPopupOpen; }
        }

        public void Reset()
        {
            activeTarget = default(PrototypeCampInteractionTarget);
            openPopupTarget = default(PrototypeCampInteractionTarget);
            confirmationConsumed = false;
        }

        public void UpdateSelection(Vector2 playerPosition, float facingDirection, IReadOnlyList<PrototypeCampInteractionTarget> targets)
        {
            if (IsPopupOpen)
            {
                return;
            }

            PrototypeCampInteractionTarget best = default(PrototypeCampInteractionTarget);
            float bestDistance = float.MaxValue;
            float bestTieBreakScore = float.MaxValue;
            float normalizedFacing = facingDirection < 0f ? -1f : 1f;
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i += 1)
                {
                    PrototypeCampInteractionTarget candidate = targets[i];
                    if (!candidate.IsEnabled || candidate.Kind == PrototypeCampInteractionTargetKind.None)
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(playerPosition, candidate.Position);
                    if (distance > PrototypeCampUse.UseRange + ScoreEpsilon)
                    {
                        continue;
                    }

                    float horizontalOffset = candidate.Position.x - playerPosition.x;
                    bool behind = Mathf.Abs(horizontalOffset) > DirectionEpsilon && Mathf.Sign(horizontalOffset) != normalizedFacing;
                    float tieBreakScore = (behind ? FacingPenalty : 0f) -
                                          candidate.SelectionPriority * SelectionPriorityBias;
                    if (candidate.Kind == activeTarget.Kind && string.Equals(candidate.Id, activeTarget.Id, StringComparison.Ordinal))
                    {
                        tieBreakScore -= CurrentTargetHysteresis;
                    }

                    bool meaningfullyCloser = distance < bestDistance - DistanceTieEpsilon;
                    bool nearlySameDistance = Mathf.Abs(distance - bestDistance) <= DistanceTieEpsilon;
                    bool betterTieBreak = nearlySameDistance && tieBreakScore < bestTieBreakScore - ScoreEpsilon;
                    bool deterministicTie = nearlySameDistance &&
                                            Mathf.Abs(tieBreakScore - bestTieBreakScore) <= ScoreEpsilon &&
                                            string.CompareOrdinal(candidate.Id, best.Id) < 0;
                    if (meaningfullyCloser || betterTieBreak || deterministicTie)
                    {
                        best = candidate;
                        bestDistance = distance;
                        bestTieBreakScore = tieBreakScore;
                    }
                }
            }

            activeTarget = best;
        }

        public static bool RunDistanceFirstSelectionProbe()
        {
            PrototypeCampInteraction interaction = new PrototypeCampInteraction();
            List<PrototypeCampInteractionTarget> targets = new List<PrototypeCampInteractionTarget>
            {
                new PrototypeCampInteractionTarget(
                    "slot.start.upper",
                    PrototypeCampInteractionTargetKind.ModuleExpansionSlot,
                    new Vector2(-4f, PrototypeCampUse.PlayerFloorY),
                    true,
                    1),
                new PrototypeCampInteractionTarget(
                    "storage.planning",
                    PrototypeCampInteractionTargetKind.StoragePlanning,
                    new Vector2(-3.8f, PrototypeCampUse.PlayerFloorY)),
                new PrototypeCampInteractionTarget(
                    "slot.start.basement",
                    PrototypeCampInteractionTargetKind.ModuleExpansionSlot,
                    new Vector2(2.5f, PrototypeCampUse.PlayerFloorY),
                    true,
                    1),
                new PrototypeCampInteractionTarget(
                    "camp.Workbench",
                    PrototypeCampInteractionTargetKind.Workbench,
                    new Vector2(1.5f, PrototypeCampUse.PlayerFloorY),
                    true,
                    2),
                new PrototypeCampInteractionTarget(
                    "camp.expedition-map",
                    PrototypeCampInteractionTargetKind.ExpeditionMap,
                    new Vector2(5.25f, PrototypeCampUse.PlayerFloorY))
            };

            interaction.UpdateSelection(targets[0].Position, 1f, targets);
            if (interaction.ActiveTargetId != targets[0].Id)
            {
                return false;
            }
            interaction.UpdateSelection(targets[1].Position, 1f, targets);
            if (interaction.ActiveTargetId != targets[1].Id)
            {
                return false;
            }
            interaction.UpdateSelection(targets[2].Position, -1f, targets);
            if (interaction.ActiveTargetId != targets[2].Id)
            {
                return false;
            }
            interaction.UpdateSelection(targets[3].Position, -1f, targets);
            if (interaction.ActiveTargetId != targets[3].Id)
            {
                return false;
            }
            interaction.UpdateSelection(targets[4].Position, -1f, targets);
            if (interaction.ActiveTargetId != targets[4].Id || !interaction.TryOpenPopup())
            {
                return false;
            }
            interaction.UpdateSelection(targets[1].Position, -1f, targets);
            bool popupLatched = interaction.ActiveTargetId == targets[4].Id &&
                                interaction.OpenPopupTargetId == targets[4].Id;
            interaction.ClosePopup();

            List<PrototypeCampInteractionTarget> sharedAnchor = new List<PrototypeCampInteractionTarget>
            {
                new PrototypeCampInteractionTarget(
                    "blueprint",
                    PrototypeCampInteractionTargetKind.StoragePlanning,
                    Vector2.zero,
                    true,
                    0),
                new PrototypeCampInteractionTarget(
                    "installed",
                    PrototypeCampInteractionTargetKind.Workbench,
                    Vector2.zero,
                    true,
                    2)
            };
            interaction.Reset();
            interaction.UpdateSelection(Vector2.zero, 1f, sharedAnchor);
            bool installedWinsSharedAnchor = interaction.ActiveTargetId == "installed";

            List<PrototypeCampInteractionTarget> stableTie = new List<PrototypeCampInteractionTarget>
            {
                new PrototypeCampInteractionTarget(
                    "zeta",
                    PrototypeCampInteractionTargetKind.ModuleExpansionSlot,
                    Vector2.zero),
                new PrototypeCampInteractionTarget(
                    "alpha",
                    PrototypeCampInteractionTargetKind.StoragePlanning,
                    Vector2.zero)
            };
            interaction.Reset();
            interaction.UpdateSelection(Vector2.zero, 1f, stableTie);
            bool deterministicTie = interaction.ActiveTargetId == "alpha";

            interaction.Reset();
            interaction.UpdateSelection(Vector2.zero, 1f, new[] { stableTie[0] });
            interaction.UpdateSelection(Vector2.zero, 1f, stableTie);
            bool hysteresisRetainsExactTie = interaction.ActiveTargetId == "zeta";

            List<PrototypeCampInteractionTarget> facingTie = new List<PrototypeCampInteractionTarget>
            {
                new PrototypeCampInteractionTarget(
                    "left",
                    PrototypeCampInteractionTargetKind.StoragePlanning,
                    new Vector2(-1f, 0f)),
                new PrototypeCampInteractionTarget(
                    "right",
                    PrototypeCampInteractionTargetKind.ExpeditionMap,
                    new Vector2(1f, 0f))
            };
            interaction.Reset();
            interaction.UpdateSelection(Vector2.zero, 1f, facingTie);
            bool facingBreaksDistanceTie = interaction.ActiveTargetId == "right";

            return popupLatched && installedWinsSharedAnchor && deterministicTie &&
                   hysteresisRetainsExactTie && facingBreaksDistanceTie;
        }

        public bool TryOpenPopup()
        {
            if (IsPopupOpen || ActiveTargetKind == PrototypeCampInteractionTargetKind.None)
            {
                return false;
            }

            openPopupTarget = activeTarget;
            confirmationConsumed = false;
            return true;
        }

        public bool TryConfirmAction()
        {
            if (!IsPopupOpen || confirmationConsumed)
            {
                return false;
            }

            confirmationConsumed = true;
            return true;
        }

        public void ClosePopup()
        {
            openPopupTarget = default(PrototypeCampInteractionTarget);
            confirmationConsumed = false;
        }

        public bool PrepareOpenPopupForReturn()
        {
            if (!IsPopupOpen)
            {
                return false;
            }

            confirmationConsumed = false;
            return true;
        }
    }
}
