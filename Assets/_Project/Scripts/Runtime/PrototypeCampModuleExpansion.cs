using System;
using System.Collections.Generic;
using UnityEngine;

namespace KimSurvival
{
    public enum CampModuleArchetype
    {
        Upper,
        Side,
        Basement
    }

    public enum CampModuleGeometryStatus
    {
        Valid,
        NoConnectionSlot,
        SlotUnavailable,
        Overlap,
        TerrainBlocked,
        PathBlocked
    }

    public enum CampModuleEconomyStatus
    {
        CostUnset,
        Locked,
        Short,
        Ready,
        PrototypeLimit
    }

    public enum CampModuleCommitStatus
    {
        Succeeded,
        NotPreviewing,
        InvalidGeometry,
        CostUnset,
        Locked,
        Short,
        PrototypeLimit,
        DuplicateSubmit
    }

    public enum CampModuleConnectorKind
    {
        Ladder,
        Door
    }

    public enum CampModuleTransactionGuard
    {
        Idle,
        Validating,
        Committed
    }

    public readonly struct CampModuleResourceCost
    {
        public CampModuleResourceCost(int wood, int stone, int food, int salvage, bool configured)
        {
            Wood = Mathf.Max(0, wood);
            Stone = Mathf.Max(0, stone);
            Food = Mathf.Max(0, food);
            Salvage = Mathf.Max(0, salvage);
            IsConfigured = configured;
        }

        public int Wood { get; }
        public int Stone { get; }
        public int Food { get; }
        public int Salvage { get; }
        public bool IsConfigured { get; }

        public bool CanAfford(GameSession session)
        {
            return IsConfigured && session != null && session.CanAffordResources(Wood, Stone, Food, Salvage);
        }
    }

    public readonly struct CampModuleUnlockRequirement
    {
        public CampModuleUnlockRequirement(bool configured, bool requiresWorkbench, int minimumDay)
        {
            IsConfigured = configured;
            RequiresWorkbench = requiresWorkbench;
            MinimumDay = Mathf.Max(1, minimumDay);
        }

        public bool IsConfigured { get; }
        public bool RequiresWorkbench { get; }
        public int MinimumDay { get; }

        public bool IsMet(GameSession session)
        {
            return IsConfigured && session != null && session.Day >= MinimumDay &&
                   (!RequiresWorkbench || session.HasStructure(StructureKind.Workbench));
        }
    }

    public sealed class PrototypeCampModuleExpansionConfig
    {
        public const string BalanceStatus = "WAVE9_V0_2";
        public const int MaxCommittedExpansion = 1;

        private readonly Dictionary<CampModuleArchetype, CampModuleResourceCost> costs;

        public PrototypeCampModuleExpansionConfig(
            CampModuleUnlockRequirement unlockRequirement,
            IDictionary<CampModuleArchetype, CampModuleResourceCost> configuredCosts,
            bool provisional)
        {
            UnlockRequirement = unlockRequirement;
            IsProvisional = provisional;
            costs = configuredCosts == null
                ? new Dictionary<CampModuleArchetype, CampModuleResourceCost>()
                : new Dictionary<CampModuleArchetype, CampModuleResourceCost>(configuredCosts);
        }

        public CampModuleUnlockRequirement UnlockRequirement { get; }
        public bool IsProvisional { get; }

        public CampModuleResourceCost GetCost(CampModuleArchetype archetype)
        {
            return costs.TryGetValue(archetype, out CampModuleResourceCost cost)
                ? cost
                : new CampModuleResourceCost(0, 0, 0, 0, false);
        }

        public static PrototypeCampModuleExpansionConfig CreateVerticalSliceBalance()
        {
            CampModuleResourceCost verticalSliceCost = new CampModuleResourceCost(2, 0, 0, 1, true);
            return new PrototypeCampModuleExpansionConfig(
                new CampModuleUnlockRequirement(true, true, 1),
                new Dictionary<CampModuleArchetype, CampModuleResourceCost>
                {
                    { CampModuleArchetype.Upper, verticalSliceCost },
                    { CampModuleArchetype.Side, verticalSliceCost },
                    { CampModuleArchetype.Basement, verticalSliceCost }
                },
                false);
        }
    }

    public readonly struct CampModuleDefinition
    {
        public CampModuleDefinition(
            CampModuleArchetype archetype,
            string roomId,
            Vector2 origin,
            Vector2 size,
            string startSlotId,
            string reciprocalSlotId,
            CampModuleConnectorKind connectorKind,
            float generalFloorMinimumX,
            float generalFloorMaximumX,
            float startConnectorDisplayX,
            float moduleConnectorDisplayX)
        {
            Archetype = archetype;
            RoomId = roomId ?? string.Empty;
            Origin = origin;
            Size = size;
            StartSlotId = startSlotId ?? string.Empty;
            ReciprocalSlotId = reciprocalSlotId ?? string.Empty;
            ConnectorKind = connectorKind;
            GeneralFloorMinimumX = generalFloorMinimumX;
            GeneralFloorMaximumX = generalFloorMaximumX;
            StartConnectorDisplayX = startConnectorDisplayX;
            ModuleConnectorDisplayX = moduleConnectorDisplayX;
        }

        public CampModuleArchetype Archetype { get; }
        public string RoomId { get; }
        public Vector2 Origin { get; }
        public Vector2 Size { get; }
        public string StartSlotId { get; }
        public string ReciprocalSlotId { get; }
        public CampModuleConnectorKind ConnectorKind { get; }
        public float GeneralFloorMinimumX { get; }
        public float GeneralFloorMaximumX { get; }
        public float StartConnectorDisplayX { get; }
        public float ModuleConnectorDisplayX { get; }

        public Rect Bounds
        {
            get { return new Rect(Origin, Size); }
        }

        public float GeneralFloorDisplayMinimumX
        {
            get { return GeneralFloorMinimumX - 6f; }
        }

        public float GeneralFloorDisplayMaximumX
        {
            get { return GeneralFloorMaximumX - 6f; }
        }
    }

    public static class PrototypeCampModuleCatalog
    {
        public const string StartRoomId = "room.start";
        public static readonly Rect StartRoomBounds = new Rect(0f, 0f, 18f, 5f);

        private static readonly CampModuleDefinition[] Definitions =
        {
            new CampModuleDefinition(
                CampModuleArchetype.Upper,
                "room.upper.standard",
                new Vector2(0f, 5f),
                new Vector2(12f, 5f),
                "slot.start.upper",
                "slot.upper.down",
                CampModuleConnectorKind.Ladder,
                3f,
                11f,
                -4f,
                -4f),
            new CampModuleDefinition(
                CampModuleArchetype.Side,
                "room.side.standard",
                new Vector2(18f, 0f),
                new Vector2(12f, 5f),
                "slot.start.side",
                "slot.side.left",
                CampModuleConnectorKind.Door,
                2f,
                11f,
                8.1f,
                -4.8f),
            new CampModuleDefinition(
                CampModuleArchetype.Basement,
                "room.basement.standard",
                new Vector2(0f, -5f),
                new Vector2(12f, 5f),
                "slot.start.basement",
                "slot.basement.up",
                CampModuleConnectorKind.Ladder,
                1f,
                7.75f,
                2.5f,
                3f)
        };

        public static IReadOnlyList<CampModuleDefinition> All
        {
            get { return Definitions; }
        }

        public static CampModuleDefinition Get(CampModuleArchetype archetype)
        {
            return Definitions[(int)archetype];
        }

        public static bool TryGetByStartSlotId(string startSlotId, out CampModuleDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i += 1)
            {
                if (string.Equals(Definitions[i].StartSlotId, startSlotId, StringComparison.Ordinal))
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default(CampModuleDefinition);
            return false;
        }
    }

    public sealed class CampModuleValidationContext
    {
        public CampModuleValidationContext()
        {
            OccupiedRoomBounds = new List<Rect> { PrototypeCampModuleCatalog.StartRoomBounds };
            HasMatchingConnectionSlot = true;
            ConnectionSlotAvailable = true;
            TerrainAllowsCandidate = true;
            ConnectorClear = true;
            RequiredPathClear = true;
        }

        public List<Rect> OccupiedRoomBounds { get; }
        public bool HasMatchingConnectionSlot { get; set; }
        public bool ConnectionSlotAvailable { get; set; }
        public bool TerrainAllowsCandidate { get; set; }
        public bool ConnectorClear { get; set; }
        public bool RequiredPathClear { get; set; }

        public CampModuleValidationContext Clone()
        {
            CampModuleValidationContext clone = new CampModuleValidationContext
            {
                HasMatchingConnectionSlot = HasMatchingConnectionSlot,
                ConnectionSlotAvailable = ConnectionSlotAvailable,
                TerrainAllowsCandidate = TerrainAllowsCandidate,
                ConnectorClear = ConnectorClear,
                RequiredPathClear = RequiredPathClear
            };
            clone.OccupiedRoomBounds.Clear();
            clone.OccupiedRoomBounds.AddRange(OccupiedRoomBounds);
            return clone;
        }
    }

    public readonly struct CampModuleEvaluation
    {
        public CampModuleEvaluation(
            CampModuleDefinition definition,
            CampModuleGeometryStatus geometry,
            CampModuleEconomyStatus economy,
            CampModuleResourceCost cost)
        {
            Definition = definition;
            Geometry = geometry;
            Economy = economy;
            Cost = cost;
        }

        public CampModuleDefinition Definition { get; }
        public CampModuleGeometryStatus Geometry { get; }
        public CampModuleEconomyStatus Economy { get; }
        public CampModuleResourceCost Cost { get; }

        public bool CanCommit
        {
            get { return Geometry == CampModuleGeometryStatus.Valid && Economy == CampModuleEconomyStatus.Ready; }
        }
    }

    public static class PrototypeCampModuleReasonKeys
    {
        public static string Geometry(CampModuleGeometryStatus status)
        {
            switch (status)
            {
                case CampModuleGeometryStatus.NoConnectionSlot:
                    return "interaction.module.no_slot";
                case CampModuleGeometryStatus.SlotUnavailable:
                    return "interaction.module.slot_unavailable";
                case CampModuleGeometryStatus.Overlap:
                    return "interaction.module.overlap";
                case CampModuleGeometryStatus.TerrainBlocked:
                    return "interaction.module.terrain_blocked";
                case CampModuleGeometryStatus.PathBlocked:
                    return "interaction.module.path_blocked";
                default:
                    return string.Empty;
            }
        }

        public static string Economy(CampModuleEconomyStatus status)
        {
            switch (status)
            {
                case CampModuleEconomyStatus.Locked:
                    return "interaction.module.locked_workbench";
                case CampModuleEconomyStatus.Short:
                    return "interaction.module.missing";
                case CampModuleEconomyStatus.PrototypeLimit:
                    return "interaction.module.prototype_limit";
                case CampModuleEconomyStatus.Ready:
                    return "interaction.module.ready";
                default:
                    return "module.economy.costunset";
            }
        }

        public static string Primary(CampModuleEvaluation evaluation)
        {
            if (evaluation.Economy == CampModuleEconomyStatus.PrototypeLimit)
            {
                return Economy(evaluation.Economy);
            }

            string geometry = Geometry(evaluation.Geometry);
            return string.IsNullOrEmpty(geometry) ? Economy(evaluation.Economy) : geometry;
        }
    }

    public readonly struct CampModuleReturnSnapshot
    {
        public CampModuleReturnSnapshot(Vector2 position, float facingDirection, string roomId)
        {
            Position = position;
            FacingDirection = facingDirection < 0f ? -1f : 1f;
            RoomId = string.IsNullOrWhiteSpace(roomId) ? PrototypeCampModuleCatalog.StartRoomId : roomId;
        }

        public Vector2 Position { get; }
        public float FacingDirection { get; }
        public string RoomId { get; }
    }

    public sealed class PrototypeCampModuleExpansion
    {
        private readonly PrototypeCampModuleExpansionConfig config;
        private readonly bool[] seenCandidates = new bool[3];
        private CampModuleReturnSnapshot returnSnapshot;
        private CampModuleArchetype selectedArchetype;
        private CampModuleArchetype? committedArchetype;

        public PrototypeCampModuleExpansion(PrototypeCampModuleExpansionConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            Reset();
        }

        public PrototypeCampModuleExpansionConfig Config
        {
            get { return config; }
        }

        public bool IsPreviewActive { get; private set; }
        public CampModuleTransactionGuard TransactionGuard { get; private set; }
        public CampModuleArchetype SelectedArchetype { get { return selectedArchetype; } }
        public bool HasCommittedModule { get { return committedArchetype.HasValue; } }
        public CampModuleArchetype CommittedArchetype { get { return committedArchetype.GetValueOrDefault(); } }
        public string CommittedRoomId { get { return HasCommittedModule ? PrototypeCampModuleCatalog.Get(CommittedArchetype).RoomId : string.Empty; } }
        public CampModuleReturnSnapshot ReturnSnapshot { get { return returnSnapshot; } }

        public void Reset()
        {
            Array.Clear(seenCandidates, 0, seenCandidates.Length);
            selectedArchetype = CampModuleArchetype.Upper;
            committedArchetype = null;
            returnSnapshot = default(CampModuleReturnSnapshot);
            IsPreviewActive = false;
            TransactionGuard = CampModuleTransactionGuard.Idle;
        }

        public bool BeginPreview(CampModuleReturnSnapshot snapshot)
        {
            return BeginPreview(snapshot, CampModuleArchetype.Upper);
        }

        public bool BeginPreview(CampModuleReturnSnapshot snapshot, CampModuleArchetype initialArchetype)
        {
            if (IsPreviewActive || TransactionGuard == CampModuleTransactionGuard.Validating)
            {
                return false;
            }

            returnSnapshot = snapshot;
            selectedArchetype = initialArchetype;
            seenCandidates[(int)selectedArchetype] = true;
            IsPreviewActive = true;
            TransactionGuard = CampModuleTransactionGuard.Idle;
            return true;
        }

        public bool ResumePreview(CampModuleReturnSnapshot snapshot)
        {
            return BeginPreview(snapshot, selectedArchetype);
        }

        public void Cycle(int direction)
        {
            if (!IsPreviewActive || direction == 0)
            {
                return;
            }

            int count = Enum.GetValues(typeof(CampModuleArchetype)).Length;
            int next = ((int)selectedArchetype + (direction < 0 ? -1 : 1) + count) % count;
            selectedArchetype = (CampModuleArchetype)next;
            seenCandidates[next] = true;
        }

        public bool HasSeen(CampModuleArchetype archetype)
        {
            return seenCandidates[(int)archetype];
        }

        public bool HasSeenAllCandidates
        {
            get
            {
                for (int i = 0; i < seenCandidates.Length; i += 1)
                {
                    if (!seenCandidates[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public CampModuleReturnSnapshot CancelPreview()
        {
            IsPreviewActive = false;
            TransactionGuard = CampModuleTransactionGuard.Idle;
            return returnSnapshot;
        }

        public CampModuleEvaluation Evaluate(GameSession session, CampModuleValidationContext context)
        {
            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(selectedArchetype);
            CampModuleGeometryStatus geometry = EvaluateGeometry(definition, context);
            CampModuleResourceCost cost = config.GetCost(selectedArchetype);
            CampModuleEconomyStatus economy;
            if (HasCommittedModule)
            {
                economy = CampModuleEconomyStatus.PrototypeLimit;
            }
            else if (!cost.IsConfigured || !config.UnlockRequirement.IsConfigured)
            {
                economy = CampModuleEconomyStatus.CostUnset;
            }
            else if (!config.UnlockRequirement.IsMet(session))
            {
                economy = CampModuleEconomyStatus.Locked;
            }
            else
            {
                economy = cost.CanAfford(session) ? CampModuleEconomyStatus.Ready : CampModuleEconomyStatus.Short;
            }

            return new CampModuleEvaluation(definition, geometry, economy, cost);
        }

        public CampModuleCommitStatus TryCommit(GameSession session, CampModuleValidationContext context)
        {
            if (!IsPreviewActive)
            {
                return CampModuleCommitStatus.NotPreviewing;
            }
            if (TransactionGuard != CampModuleTransactionGuard.Idle)
            {
                return CampModuleCommitStatus.DuplicateSubmit;
            }

            CampModuleEvaluation evaluation = Evaluate(session, context);
            if (evaluation.Geometry != CampModuleGeometryStatus.Valid)
            {
                return CampModuleCommitStatus.InvalidGeometry;
            }

            switch (evaluation.Economy)
            {
                case CampModuleEconomyStatus.CostUnset:
                    return CampModuleCommitStatus.CostUnset;
                case CampModuleEconomyStatus.Locked:
                    return CampModuleCommitStatus.Locked;
                case CampModuleEconomyStatus.Short:
                    return CampModuleCommitStatus.Short;
                case CampModuleEconomyStatus.PrototypeLimit:
                    return CampModuleCommitStatus.PrototypeLimit;
            }

            TransactionGuard = CampModuleTransactionGuard.Validating;
            CampModuleResourceCost cost = evaluation.Cost;
            if (!session.TrySpendResources(cost.Wood, cost.Stone, cost.Food, cost.Salvage))
            {
                TransactionGuard = CampModuleTransactionGuard.Idle;
                return CampModuleCommitStatus.Short;
            }

            committedArchetype = selectedArchetype;
            IsPreviewActive = false;
            TransactionGuard = CampModuleTransactionGuard.Committed;
            return CampModuleCommitStatus.Succeeded;
        }

        public static CampModuleGeometryStatus EvaluateGeometry(CampModuleDefinition definition, CampModuleValidationContext context)
        {
            if (context == null || !context.HasMatchingConnectionSlot ||
                string.IsNullOrWhiteSpace(definition.StartSlotId) || string.IsNullOrWhiteSpace(definition.ReciprocalSlotId))
            {
                return CampModuleGeometryStatus.NoConnectionSlot;
            }
            if (!context.ConnectionSlotAvailable)
            {
                return CampModuleGeometryStatus.SlotUnavailable;
            }

            Rect candidate = definition.Bounds;
            for (int i = 0; i < context.OccupiedRoomBounds.Count; i += 1)
            {
                if (PositiveAreaOverlap(candidate, context.OccupiedRoomBounds[i]))
                {
                    return CampModuleGeometryStatus.Overlap;
                }
            }

            if (!context.TerrainAllowsCandidate)
            {
                return CampModuleGeometryStatus.TerrainBlocked;
            }
            if (!context.ConnectorClear || !context.RequiredPathClear)
            {
                return CampModuleGeometryStatus.PathBlocked;
            }
            return CampModuleGeometryStatus.Valid;
        }

        private static bool PositiveAreaOverlap(Rect first, Rect second)
        {
            float overlapWidth = Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin);
            float overlapHeight = Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin);
            return overlapWidth > 0.0001f && overlapHeight > 0.0001f;
        }
    }
}
