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
        public const int LegacyMaxCommittedExpansion = 1;
        public const int MaxCommittedExpansion = 3;

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
            CampModuleResourceCost verticalExpansionCost = new CampModuleResourceCost(1, 0, 0, 0, true);
            CampModuleResourceCost sideExpansionCost = new CampModuleResourceCost(2, 0, 0, 1, true);
            return new PrototypeCampModuleExpansionConfig(
                new CampModuleUnlockRequirement(true, true, 1),
                new Dictionary<CampModuleArchetype, CampModuleResourceCost>
                {
                    { CampModuleArchetype.Upper, verticalExpansionCost },
                    { CampModuleArchetype.Side, sideExpansionCost },
                    { CampModuleArchetype.Basement, verticalExpansionCost }
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
        public const string VisiblePlanningPointId = "storage.planning";
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

    [Serializable]
    public sealed class CampModuleCommittedRoomSnapshot
    {
        public CampModuleArchetype Archetype;
        public string RoomId = string.Empty;
        public string StartSlotId = string.Empty;
        public string ReciprocalSlotId = string.Empty;
        public CampModuleConnectorKind ConnectorKind;
        public int CommitSequence;

        public CampModuleCommittedRoomSnapshot Clone()
        {
            return new CampModuleCommittedRoomSnapshot
            {
                Archetype = Archetype,
                RoomId = RoomId,
                StartSlotId = StartSlotId,
                ReciprocalSlotId = ReciprocalSlotId,
                ConnectorKind = ConnectorKind,
                CommitSequence = CommitSequence
            };
        }
    }

    [Serializable]
    public sealed class PrototypeCampModuleExpansionSnapshot
    {
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;

        // Legacy singular surface. Old saves restore this into the first committed room.
        public bool HasCommittedModule;
        public CampModuleArchetype CommittedArchetype;
        public string CommittedRoomId = string.Empty;

        public CampModuleCommittedRoomSnapshot[] CommittedRooms = Array.Empty<CampModuleCommittedRoomSnapshot>();

        public PrototypeCampModuleExpansionSnapshot Clone()
        {
            CampModuleCommittedRoomSnapshot[] source = CommittedRooms ?? Array.Empty<CampModuleCommittedRoomSnapshot>();
            CampModuleCommittedRoomSnapshot[] rooms = new CampModuleCommittedRoomSnapshot[source.Length];
            for (int index = 0; index < source.Length; index += 1)
            {
                rooms[index] = source[index] == null ? null : source[index].Clone();
            }
            return new PrototypeCampModuleExpansionSnapshot
            {
                SchemaVersion = SchemaVersion,
                HasCommittedModule = HasCommittedModule,
                CommittedArchetype = CommittedArchetype,
                CommittedRoomId = CommittedRoomId,
                CommittedRooms = rooms
            };
        }
    }

    public sealed class PrototypeCampModuleExpansion
    {
        private readonly PrototypeCampModuleExpansionConfig config;
        private readonly bool[] seenCandidates = new bool[3];
        private readonly List<CampModuleCommittedRoomSnapshot> committedRooms =
            new List<CampModuleCommittedRoomSnapshot>();
        private CampModuleReturnSnapshot returnSnapshot;
        private CampModuleArchetype selectedArchetype;
        // Kept as the first committed module for legacy singular callers and saves.
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
        public bool HasCommittedModule { get { return committedRooms.Count > 0; } }
        public CampModuleArchetype CommittedArchetype { get { return committedArchetype.GetValueOrDefault(); } }
        public string CommittedRoomId { get { return HasCommittedModule ? PrototypeCampModuleCatalog.Get(CommittedArchetype).RoomId : string.Empty; } }
        public int CommittedModuleCount { get { return committedRooms.Count; } }
        public bool HasUpperAndBasementCommitted
        {
            get { return IsCommitted(CampModuleArchetype.Upper) && IsCommitted(CampModuleArchetype.Basement); }
        }
        public IReadOnlyList<CampModuleCommittedRoomSnapshot> CommittedRooms
        {
            get
            {
                CampModuleCommittedRoomSnapshot[] result = new CampModuleCommittedRoomSnapshot[committedRooms.Count];
                for (int index = 0; index < committedRooms.Count; index += 1)
                {
                    result[index] = committedRooms[index].Clone();
                }
                return result;
            }
        }
        public CampModuleReturnSnapshot ReturnSnapshot { get { return returnSnapshot; } }

        public void Reset()
        {
            Array.Clear(seenCandidates, 0, seenCandidates.Length);
            selectedArchetype = CampModuleArchetype.Upper;
            committedArchetype = null;
            committedRooms.Clear();
            returnSnapshot = default(CampModuleReturnSnapshot);
            IsPreviewActive = false;
            TransactionGuard = CampModuleTransactionGuard.Idle;
        }

        public bool IsCommitted(CampModuleArchetype archetype)
        {
            for (int index = 0; index < committedRooms.Count; index += 1)
            {
                if (committedRooms[index].Archetype == archetype) return true;
            }
            return false;
        }

        public bool IsRoomCommitted(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId)) return false;
            for (int index = 0; index < committedRooms.Count; index += 1)
            {
                if (string.Equals(committedRooms[index].RoomId, roomId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public bool IsConnectorCommitted(string connectorId)
        {
            if (string.IsNullOrWhiteSpace(connectorId)) return false;
            for (int index = 0; index < committedRooms.Count; index += 1)
            {
                CampModuleCommittedRoomSnapshot room = committedRooms[index];
                if (string.Equals(room.StartSlotId, connectorId, StringComparison.Ordinal) ||
                    string.Equals(room.ReciprocalSlotId, connectorId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetCommittedRoom(
            CampModuleArchetype archetype,
            out CampModuleCommittedRoomSnapshot committedRoom)
        {
            for (int index = 0; index < committedRooms.Count; index += 1)
            {
                if (committedRooms[index].Archetype != archetype) continue;
                committedRoom = committedRooms[index].Clone();
                return true;
            }
            committedRoom = null;
            return false;
        }

        public bool TryGetCommittedRoom(
            string stableRoomId,
            out CampModuleCommittedRoomSnapshot committedRoom)
        {
            if (!string.IsNullOrWhiteSpace(stableRoomId))
            {
                for (int index = 0; index < committedRooms.Count; index += 1)
                {
                    if (!string.Equals(committedRooms[index].RoomId, stableRoomId, StringComparison.Ordinal)) continue;
                    committedRoom = committedRooms[index].Clone();
                    return true;
                }
            }

            committedRoom = null;
            return false;
        }

        public bool TryGetCommittedRoomByConnector(
            string stableConnectorId,
            out CampModuleCommittedRoomSnapshot committedRoom)
        {
            if (!string.IsNullOrWhiteSpace(stableConnectorId))
            {
                for (int index = 0; index < committedRooms.Count; index += 1)
                {
                    CampModuleCommittedRoomSnapshot room = committedRooms[index];
                    if (!string.Equals(room.StartSlotId, stableConnectorId, StringComparison.Ordinal) &&
                        !string.Equals(room.ReciprocalSlotId, stableConnectorId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    committedRoom = room.Clone();
                    return true;
                }
            }

            committedRoom = null;
            return false;
        }

        public bool TryResolveConnectionDestination(
            string currentRoomId,
            string stableConnectorId,
            out string destinationRoomId)
        {
            destinationRoomId = string.Empty;
            if (string.IsNullOrWhiteSpace(currentRoomId) || string.IsNullOrWhiteSpace(stableConnectorId))
            {
                return false;
            }

            for (int index = 0; index < committedRooms.Count; index += 1)
            {
                CampModuleCommittedRoomSnapshot room = committedRooms[index];
                if (string.Equals(currentRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal) &&
                    string.Equals(stableConnectorId, room.StartSlotId, StringComparison.Ordinal))
                {
                    destinationRoomId = room.RoomId;
                    return true;
                }
                if (string.Equals(currentRoomId, room.RoomId, StringComparison.Ordinal) &&
                    string.Equals(stableConnectorId, room.ReciprocalSlotId, StringComparison.Ordinal))
                {
                    destinationRoomId = PrototypeCampModuleCatalog.StartRoomId;
                    return true;
                }
            }

            return false;
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
            return Evaluate(session, context, selectedArchetype);
        }

        public CampModuleEvaluation Evaluate(
            GameSession session,
            CampModuleValidationContext context,
            CampModuleArchetype archetype)
        {
            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(archetype);
            CampModuleGeometryStatus geometry = EvaluateGeometry(definition, context);
            CampModuleResourceCost cost = config.GetCost(archetype);
            CampModuleEconomyStatus economy;
            if (IsCommitted(archetype) ||
                committedRooms.Count >= PrototypeCampModuleExpansionConfig.MaxCommittedExpansion)
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

            CampModuleCommittedRoomSnapshot committedRoom = CreateCommittedRoom(
                evaluation.Definition,
                committedRooms.Count);
            committedRooms.Add(committedRoom);
            if (!committedArchetype.HasValue) committedArchetype = selectedArchetype;
            IsPreviewActive = false;
            // The synchronous commit has closed its preview. Leaving Committed
            // latched leaked the build-reaction state into the next movement frame;
            // IsPreviewActive still supplies duplicate-submit rejection.
            TransactionGuard = CampModuleTransactionGuard.Idle;
            return CampModuleCommitStatus.Succeeded;
        }

        public PrototypeCampModuleExpansionSnapshot CaptureSnapshot()
        {
            CampModuleCommittedRoomSnapshot[] rooms = new CampModuleCommittedRoomSnapshot[committedRooms.Count];
            for (int index = 0; index < committedRooms.Count; index += 1)
            {
                rooms[index] = committedRooms[index].Clone();
            }
            return new PrototypeCampModuleExpansionSnapshot
            {
                SchemaVersion = PrototypeCampModuleExpansionSnapshot.CurrentSchemaVersion,
                HasCommittedModule = HasCommittedModule,
                CommittedArchetype = CommittedArchetype,
                CommittedRoomId = CommittedRoomId,
                CommittedRooms = rooms
            };
        }

        public bool RestoreSnapshot(PrototypeCampModuleExpansionSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SchemaVersion < 1 ||
                snapshot.SchemaVersion > PrototypeCampModuleExpansionSnapshot.CurrentSchemaVersion)
            {
                return false;
            }

            CampModuleCommittedRoomSnapshot[] source = snapshot.CommittedRooms ??
                                                       Array.Empty<CampModuleCommittedRoomSnapshot>();
            var candidateRooms = new List<CampModuleCommittedRoomSnapshot>();
            if (source.Length == 0 && snapshot.HasCommittedModule)
            {
                if (!Enum.IsDefined(typeof(CampModuleArchetype), snapshot.CommittedArchetype)) return false;
                CampModuleDefinition legacyDefinition = PrototypeCampModuleCatalog.Get(snapshot.CommittedArchetype);
                if (!string.IsNullOrWhiteSpace(snapshot.CommittedRoomId) &&
                    !string.Equals(snapshot.CommittedRoomId, legacyDefinition.RoomId, StringComparison.Ordinal))
                {
                    return false;
                }
                candidateRooms.Add(CreateCommittedRoom(legacyDefinition, 0));
            }
            else
            {
                if (source.Length > PrototypeCampModuleExpansionConfig.MaxCommittedExpansion) return false;
                var seenArchetypes = new HashSet<CampModuleArchetype>();
                var seenRoomIds = new HashSet<string>(StringComparer.Ordinal);
                var seenStartSlotIds = new HashSet<string>(StringComparer.Ordinal);
                var seenReciprocalSlotIds = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < source.Length; index += 1)
                {
                    CampModuleCommittedRoomSnapshot room = source[index];
                    if (!IsValidCommittedRoom(
                            room,
                            index,
                            seenArchetypes,
                            seenRoomIds,
                            seenStartSlotIds,
                            seenReciprocalSlotIds))
                    {
                        return false;
                    }
                    candidateRooms.Add(room.Clone());
                }
            }

            if (candidateRooms.Count == 0 && snapshot.HasCommittedModule) return false;
            if (candidateRooms.Count > 0)
            {
                CampModuleCommittedRoomSnapshot first = candidateRooms[0];
                if (!snapshot.HasCommittedModule ||
                    snapshot.CommittedArchetype != first.Archetype ||
                    (!string.IsNullOrWhiteSpace(snapshot.CommittedRoomId) &&
                     !string.Equals(snapshot.CommittedRoomId, first.RoomId, StringComparison.Ordinal)))
                {
                    return false;
                }
            }

            committedRooms.Clear();
            for (int index = 0; index < candidateRooms.Count; index += 1)
            {
                committedRooms.Add(candidateRooms[index].Clone());
            }
            committedArchetype = candidateRooms.Count == 0
                ? (CampModuleArchetype?)null
                : candidateRooms[0].Archetype;
            Array.Clear(seenCandidates, 0, seenCandidates.Length);
            selectedArchetype = committedArchetype.GetValueOrDefault(CampModuleArchetype.Upper);
            returnSnapshot = default(CampModuleReturnSnapshot);
            IsPreviewActive = false;
            TransactionGuard = CampModuleTransactionGuard.Idle;
            return true;
        }

        /// <summary>
        /// O7 production entry point. Expansion preview is intentionally gated by the
        /// visible storage/expansion planning marker instead of an invisible world
        /// coordinate or an unbuilt connector hotspot.
        /// </summary>
        public bool BeginPreviewFromVisiblePlanningPoint(
            CampModuleReturnSnapshot snapshot,
            string stablePlanningPointId,
            CampModuleArchetype initialArchetype)
        {
            if (!string.Equals(
                    stablePlanningPointId,
                    PrototypeCampModuleCatalog.VisiblePlanningPointId,
                    StringComparison.Ordinal) ||
                !string.Equals(snapshot.RoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
            {
                return false;
            }

            return BeginPreview(snapshot, initialArchetype);
        }

        private static CampModuleCommittedRoomSnapshot CreateCommittedRoom(
            CampModuleDefinition definition,
            int commitSequence)
        {
            return new CampModuleCommittedRoomSnapshot
            {
                Archetype = definition.Archetype,
                RoomId = definition.RoomId,
                StartSlotId = definition.StartSlotId,
                ReciprocalSlotId = definition.ReciprocalSlotId,
                ConnectorKind = definition.ConnectorKind,
                CommitSequence = commitSequence
            };
        }

        private static bool IsValidCommittedRoom(
            CampModuleCommittedRoomSnapshot room,
            int expectedSequence,
            ISet<CampModuleArchetype> seenArchetypes,
            ISet<string> seenRoomIds,
            ISet<string> seenStartSlotIds,
            ISet<string> seenReciprocalSlotIds)
        {
            if (room == null ||
                !Enum.IsDefined(typeof(CampModuleArchetype), room.Archetype) ||
                room.CommitSequence != expectedSequence)
            {
                return false;
            }

            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(room.Archetype);
            return string.Equals(room.RoomId, definition.RoomId, StringComparison.Ordinal) &&
                   string.Equals(room.StartSlotId, definition.StartSlotId, StringComparison.Ordinal) &&
                   string.Equals(room.ReciprocalSlotId, definition.ReciprocalSlotId, StringComparison.Ordinal) &&
                   room.ConnectorKind == definition.ConnectorKind &&
                   seenArchetypes.Add(room.Archetype) &&
                   seenRoomIds.Add(room.RoomId) &&
                   seenStartSlotIds.Add(room.StartSlotId) &&
                   seenReciprocalSlotIds.Add(room.ReciprocalSlotId);
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
