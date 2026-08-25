using System;
using System.Collections.Generic;
using UnityEngine;

namespace KimSurvival
{
    public enum CampPlacementValidity
    {
        Valid,
        OutsideCampBounds,
        WrongZone,
        OverlapsStructure,
        BlocksEntrance,
        BlocksRequiredPath
    }

    public enum CampPlacementZone
    {
        GeneralGround,
        OpenSkyGround,
        SignalAnchor
    }

    public readonly struct CampPlacementRoomZone
    {
        public CampPlacementRoomZone(
            string roomId,
            float buildMinimumX,
            float buildMaximumX,
            bool allowsOpenSky,
            float openSkyMinimumX,
            float openSkyMaximumX,
            float entranceMinimumX,
            float entranceMaximumX,
            float requiredPathMinimumX,
            float requiredPathMaximumX)
        {
            RoomId = string.IsNullOrWhiteSpace(roomId) ? PrototypeCampModuleCatalog.StartRoomId : roomId;
            BuildMinimumX = buildMinimumX;
            BuildMaximumX = buildMaximumX;
            AllowsOpenSky = allowsOpenSky;
            OpenSkyMinimumX = openSkyMinimumX;
            OpenSkyMaximumX = openSkyMaximumX;
            EntranceMinimumX = entranceMinimumX;
            EntranceMaximumX = entranceMaximumX;
            RequiredPathMinimumX = requiredPathMinimumX;
            RequiredPathMaximumX = requiredPathMaximumX;
        }

        public string RoomId { get; }
        public float BuildMinimumX { get; }
        public float BuildMaximumX { get; }
        public bool AllowsOpenSky { get; }
        public float OpenSkyMinimumX { get; }
        public float OpenSkyMaximumX { get; }
        public float EntranceMinimumX { get; }
        public float EntranceMaximumX { get; }
        public float RequiredPathMinimumX { get; }
        public float RequiredPathMaximumX { get; }

        public static CampPlacementRoomZone StartRoom
        {
            get
            {
                return new CampPlacementRoomZone(
                    PrototypeCampModuleCatalog.StartRoomId,
                    PrototypeCampPlacement.BuildMinimumX,
                    PrototypeCampPlacement.BuildMaximumX,
                    true,
                    PrototypeCampPlacement.OpenSkyMinimumX,
                    PrototypeCampPlacement.OpenSkyMaximumX,
                    PrototypeCampPlacement.EntranceMinimumX,
                    PrototypeCampPlacement.EntranceMaximumX,
                    PrototypeCampPlacement.RequiredPathMinimumX,
                    PrototypeCampPlacement.RequiredPathMaximumX);
            }
        }
    }

    public readonly struct CampInstalledStructurePlacement
    {
        public CampInstalledStructurePlacement(string roomId, float x)
            : this(roomId, PrototypeCampPlacement.GetZoneId(CampPlacementZone.GeneralGround), x)
        {
        }

        public CampInstalledStructurePlacement(string roomId, string stablePlacementZoneId, float x)
        {
            RoomId = string.IsNullOrWhiteSpace(roomId) ? PrototypeCampModuleCatalog.StartRoomId : roomId;
            StablePlacementZoneId = stablePlacementZoneId ?? string.Empty;
            X = x;
        }

        public string RoomId { get; }
        public string StablePlacementZoneId { get; }
        public float X { get; }
    }

    [Serializable]
    public sealed class CampInstalledStructurePlacementSnapshot
    {
        public string StableStructureId = string.Empty;
        public StructureKind Structure;
        public string StablePlacementZoneId = string.Empty;
        public string StableRoomId = string.Empty;
        public float X;

        public CampInstalledStructurePlacementSnapshot Clone()
        {
            return new CampInstalledStructurePlacementSnapshot
            {
                StableStructureId = StableStructureId,
                Structure = Structure,
                StablePlacementZoneId = StablePlacementZoneId,
                StableRoomId = StableRoomId,
                X = X
            };
        }
    }

    [Serializable]
    public sealed class PrototypeCampPlacementSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public CampInstalledStructurePlacementSnapshot[] Installed = Array.Empty<CampInstalledStructurePlacementSnapshot>();

        public PrototypeCampPlacementSnapshot Clone()
        {
            CampInstalledStructurePlacementSnapshot[] source = Installed ?? Array.Empty<CampInstalledStructurePlacementSnapshot>();
            CampInstalledStructurePlacementSnapshot[] copy = new CampInstalledStructurePlacementSnapshot[source.Length];
            for (int index = 0; index < source.Length; index += 1)
            {
                copy[index] = source[index] == null ? null : source[index].Clone();
            }

            return new PrototypeCampPlacementSnapshot
            {
                SchemaVersion = SchemaVersion,
                Installed = copy
            };
        }
    }

    public sealed class PrototypeCampPlacement
    {
        public const float GridSize = 0.5f;
        public const float BuildMinimumX = -3.6f;
        public const float BuildMaximumX = 4.7f;
        public const float OpenSkyMinimumX = 2.6f;
        public const float OpenSkyMaximumX = 4.7f;
        public const float FloorY = -2.8f;
        public const float EntranceMinimumX = -3.6f;
        public const float EntranceMaximumX = -2.6f;
        public const float RequiredPathMinimumX = -0.4f;
        public const float RequiredPathMaximumX = 0.4f;

        private const float GamepadCursorSpeed = 3f;
        private const float OverlapTolerance = 0.001f;

        private readonly Dictionary<StructureKind, CampInstalledStructurePlacement> installedPlacements = new Dictionary<StructureKind, CampInstalledStructurePlacement>();
        private StructureKind selectedKind;
        private float cursorX;
        private float candidateX;
        private CampPlacementRoomZone activeRoomZone = CampPlacementRoomZone.StartRoom;

        public bool IsActive { get; private set; }
        public bool IsRelocating { get; private set; }

        public int InstalledCount
        {
            get { return installedPlacements.Count; }
        }

        public StructureKind SelectedKind
        {
            get { return selectedKind; }
        }

        public float CandidateX
        {
            get { return candidateX; }
        }

        public string CandidateRoomId
        {
            get { return activeRoomZone.RoomId; }
        }

        public CampPlacementRoomZone ActiveRoomZone
        {
            get { return activeRoomZone; }
        }

        public Vector2 CandidatePosition
        {
            get
            {
                Vector2 size = GetStructureSize(selectedKind);
                return new Vector2(candidateX, FloorY + size.y * 0.5f);
            }
        }

        public CampPlacementValidity CurrentValidity
        {
            get { return IsActive ? Validate(selectedKind, candidateX) : CampPlacementValidity.Valid; }
        }

        public PrototypeLocalizedText CurrentFeedback
        {
            get
            {
                if (!IsActive)
                {
                    return PrototypeLocalizedText.Empty;
                }

                switch (CurrentValidity)
                {
                    case CampPlacementValidity.Valid:
                        return new PrototypeLocalizedText(IsRelocating ? "placement.valid.relocate" : "placement.valid.build", selectedKind);
                    case CampPlacementValidity.OutsideCampBounds:
                        return new PrototypeLocalizedText("placement.outside");
                    case CampPlacementValidity.WrongZone:
                        return new PrototypeLocalizedText("placement.wrong_zone", selectedKind);
                    case CampPlacementValidity.OverlapsStructure:
                        return new PrototypeLocalizedText("placement.overlap");
                    case CampPlacementValidity.BlocksEntrance:
                        return new PrototypeLocalizedText("placement.entrance");
                    case CampPlacementValidity.BlocksRequiredPath:
                        return new PrototypeLocalizedText("placement.path");
                    default:
                        return new PrototypeLocalizedText("placement.invalid");
                }
            }
        }

        public void Begin(StructureKind kind, bool relocating)
        {
            Begin(kind, relocating, CampPlacementRoomZone.StartRoom);
        }

        public void Begin(StructureKind kind, bool relocating, CampPlacementRoomZone roomZone)
        {
            if (relocating)
            {
                EnsureInstalled(kind);
            }

            selectedKind = kind;
            IsRelocating = relocating;
            activeRoomZone = roomZone;
            CampInstalledStructurePlacement installed = default(CampInstalledStructurePlacement);
            bool relocatingWithinSameRoom = relocating && installedPlacements.TryGetValue(kind, out installed) &&
                                            installed.RoomId == activeRoomZone.RoomId;
            candidateX = relocatingWithinSameRoom ? installed.X : GetDefaultX(kind, activeRoomZone);
            cursorX = candidateX;
            IsActive = true;
            if (!relocatingWithinSameRoom && CurrentValidity != CampPlacementValidity.Valid)
            {
                SelectNearestValidCandidate(candidateX);
            }
        }

        private void SelectNearestValidCandidate(float preferredX)
        {
            float halfWidth = GetStructureSize(selectedKind).x * 0.5f;
            float minimum = activeRoomZone.BuildMinimumX + halfWidth;
            float maximum = activeRoomZone.BuildMaximumX - halfWidth;
            if (GetRequiredZone(selectedKind) == CampPlacementZone.OpenSkyGround)
            {
                minimum = Mathf.Max(minimum, activeRoomZone.OpenSkyMinimumX + halfWidth);
                maximum = Mathf.Min(maximum, activeRoomZone.OpenSkyMaximumX - halfWidth);
            }

            float bestX = candidateX;
            float bestDistance = float.MaxValue;
            for (float probe = Snap(minimum); probe <= maximum + OverlapTolerance; probe += GridSize)
            {
                float snapped = Snap(probe);
                if (Validate(selectedKind, snapped) != CampPlacementValidity.Valid)
                {
                    continue;
                }

                float distance = Mathf.Abs(snapped - preferredX);
                if (distance < bestDistance)
                {
                    bestX = snapped;
                    bestDistance = distance;
                }
            }

            if (bestDistance < float.MaxValue)
            {
                candidateX = bestX;
                cursorX = bestX;
            }
        }

        public void Update(PrototypeCampPlacementActions actions, float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            if (actions.UsePointer)
            {
                cursorX = actions.PointerWorldX;
            }
            else
            {
                cursorX += actions.Horizontal * GamepadCursorSpeed * deltaTime;
            }

            candidateX = Snap(cursorX);
        }

        public void SetCandidateX(float worldX)
        {
            cursorX = worldX;
            candidateX = Snap(worldX);
        }

        public bool Commit()
        {
            if (!IsActive || CurrentValidity != CampPlacementValidity.Valid)
            {
                return false;
            }

            installedPlacements[selectedKind] = new CampInstalledStructurePlacement(
                activeRoomZone.RoomId,
                GetZoneId(GetRequiredZone(selectedKind)),
                candidateX);
            IsActive = false;
            IsRelocating = false;
            return true;
        }

        public void Cancel()
        {
            IsActive = false;
            IsRelocating = false;
        }

        public void Reset()
        {
            installedPlacements.Clear();
            ResetTransientState();
        }

        public void EnsureInstalled(StructureKind kind)
        {
            if (!installedPlacements.ContainsKey(kind))
            {
                installedPlacements[kind] = new CampInstalledStructurePlacement(
                    PrototypeCampModuleCatalog.StartRoomId,
                    GetZoneId(GetRequiredZone(kind)),
                    GetDefaultX(kind));
            }
        }

        public bool HasInstalledPosition(StructureKind kind)
        {
            return installedPlacements.ContainsKey(kind);
        }

        public string GetInstalledRoomId(StructureKind kind)
        {
            return installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed)
                ? installed.RoomId
                : PrototypeCampModuleCatalog.StartRoomId;
        }

        public string GetInstalledPlacementZoneId(StructureKind kind)
        {
            return installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed) &&
                   !string.IsNullOrWhiteSpace(installed.StablePlacementZoneId)
                ? installed.StablePlacementZoneId
                : GetZoneId(GetRequiredZone(kind));
        }

        public bool IsInstalledInRoom(StructureKind kind, string roomId)
        {
            return string.Equals(GetInstalledRoomId(kind), roomId, System.StringComparison.Ordinal);
        }

        public Vector2 GetInstalledPosition(StructureKind kind)
        {
            float x = installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed)
                ? installed.X
                : GetDefaultX(kind);
            Vector2 size = GetStructureSize(kind);
            return new Vector2(x, FloorY + size.y * 0.5f);
        }

        public PrototypeCampPlacementSnapshot CaptureSnapshot()
        {
            List<CampInstalledStructurePlacementSnapshot> entries = new List<CampInstalledStructurePlacementSnapshot>();
            Array structureValues = Enum.GetValues(typeof(StructureKind));
            for (int index = 0; index < structureValues.Length; index += 1)
            {
                StructureKind kind = (StructureKind)structureValues.GetValue(index);
                if (!installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed))
                {
                    continue;
                }

                entries.Add(new CampInstalledStructurePlacementSnapshot
                {
                    StableStructureId = GetStructureId(kind),
                    Structure = kind,
                    StablePlacementZoneId = GetInstalledPlacementZoneId(kind),
                    StableRoomId = installed.RoomId,
                    X = installed.X
                });
            }

            return new PrototypeCampPlacementSnapshot
            {
                SchemaVersion = PrototypeCampPlacementSnapshot.CurrentSchemaVersion,
                Installed = entries.ToArray()
            };
        }

        public bool RestoreSnapshot(PrototypeCampPlacementSnapshot snapshot)
        {
            if (!TryBuildRestoredPlacements(snapshot, out Dictionary<StructureKind, CampInstalledStructurePlacement> restored))
            {
                return false;
            }

            installedPlacements.Clear();
            foreach (KeyValuePair<StructureKind, CampInstalledStructurePlacement> entry in restored)
            {
                installedPlacements.Add(entry.Key, entry.Value);
            }

            ResetTransientState();
            return true;
        }

        public CampPlacementValidity Validate(StructureKind kind, float worldX)
        {
            return ValidatePlacement(kind, worldX, activeRoomZone, installedPlacements);
        }

        private static CampPlacementValidity ValidatePlacement(
            StructureKind kind,
            float worldX,
            CampPlacementRoomZone roomZone,
            IReadOnlyDictionary<StructureKind, CampInstalledStructurePlacement> placements)
        {
            Vector2 size = GetStructureSize(kind);
            float halfWidth = size.x * 0.5f;
            float left = worldX - halfWidth;
            float right = worldX + halfWidth;
            if (left < roomZone.BuildMinimumX || right > roomZone.BuildMaximumX)
            {
                return CampPlacementValidity.OutsideCampBounds;
            }

            if (GetRequiredZone(kind) == CampPlacementZone.OpenSkyGround &&
                (!roomZone.AllowsOpenSky || left < roomZone.OpenSkyMinimumX || right > roomZone.OpenSkyMaximumX))
            {
                return CampPlacementValidity.WrongZone;
            }

            if (Intersects(left, right, roomZone.EntranceMinimumX, roomZone.EntranceMaximumX))
            {
                return CampPlacementValidity.BlocksEntrance;
            }

            if (Intersects(left, right, roomZone.RequiredPathMinimumX, roomZone.RequiredPathMaximumX))
            {
                return CampPlacementValidity.BlocksRequiredPath;
            }

            foreach (KeyValuePair<StructureKind, CampInstalledStructurePlacement> installed in placements)
            {
                if (installed.Key == kind || !string.Equals(installed.Value.RoomId, roomZone.RoomId, StringComparison.Ordinal))
                {
                    continue;
                }

                float combinedHalfWidth = (size.x + GetStructureSize(installed.Key).x) * 0.5f;
                if (Mathf.Abs(worldX - installed.Value.X) < combinedHalfWidth - OverlapTolerance)
                {
                    return CampPlacementValidity.OverlapsStructure;
                }
            }

            return CampPlacementValidity.Valid;
        }

        public static string GetStructureId(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire:
                    return "structure.campfire";
                case StructureKind.Workbench:
                    return "structure.workbench";
                case StructureKind.RainCollector:
                    return "structure.rain_collector";
                default:
                    return string.Empty;
            }
        }

        public static CampPlacementZone GetRequiredZone(StructureKind kind)
        {
            return kind == StructureKind.RainCollector
                ? CampPlacementZone.OpenSkyGround
                : CampPlacementZone.GeneralGround;
        }

        public static string GetZoneId(CampPlacementZone zone)
        {
            switch (zone)
            {
                case CampPlacementZone.OpenSkyGround:
                    return "camp.open-sky-ground";
                case CampPlacementZone.SignalAnchor:
                    return "camp.signal-anchor";
                default:
                    return "camp.general-ground";
            }
        }

        public static bool TryGetRoomZone(string stableRoomId, out CampPlacementRoomZone roomZone)
        {
            if (string.Equals(stableRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
            {
                roomZone = CampPlacementRoomZone.StartRoom;
                return true;
            }

            IReadOnlyList<CampModuleDefinition> definitions = PrototypeCampModuleCatalog.All;
            for (int index = 0; index < definitions.Count; index += 1)
            {
                CampModuleDefinition definition = definitions[index];
                if (!string.Equals(stableRoomId, definition.RoomId, StringComparison.Ordinal))
                {
                    continue;
                }

                float connectorX = definition.ModuleConnectorDisplayX;
                roomZone = new CampPlacementRoomZone(
                    definition.RoomId,
                    definition.GeneralFloorDisplayMinimumX,
                    definition.GeneralFloorDisplayMaximumX,
                    false,
                    0f,
                    0f,
                    connectorX - 0.8f,
                    connectorX + 0.8f,
                    connectorX - 1.1f,
                    connectorX + 1.1f);
                return true;
            }

            roomZone = default(CampPlacementRoomZone);
            return false;
        }

        public static Vector2 GetStructureSize(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire:
                    return new Vector2(1.5f, 0.9f);
                case StructureKind.Workbench:
                    return new Vector2(2f, 1.2f);
                case StructureKind.RainCollector:
                    return new Vector2(1.7f, 1.7f);
                default:
                    return Vector2.one;
            }
        }

        public static bool RunSnapshotContractProbe(out string detail)
        {
            if (!TryGetRoomZone("room.upper.standard", out CampPlacementRoomZone upperRoom) ||
                !TryGetRoomZone("room.basement.standard", out CampPlacementRoomZone basementRoom))
            {
                detail = "Known module room IDs did not resolve to placement zones.";
                return false;
            }

            PrototypeCampPlacement source = new PrototypeCampPlacement();
            source.Begin(StructureKind.Campfire, false, upperRoom);
            source.SetCandidateX(0f);
            if (source.CurrentValidity != CampPlacementValidity.Valid || !source.Commit())
            {
                detail = "Upper-room campfire placement could not be committed.";
                return false;
            }

            source.Begin(StructureKind.Workbench, false, basementRoom);
            source.SetCandidateX(-2f);
            if (source.CurrentValidity != CampPlacementValidity.Valid || !source.Commit())
            {
                detail = "Basement workbench placement could not be committed in the same run.";
                return false;
            }

            PrototypeCampPlacementSnapshot captured = source.CaptureSnapshot();
            if (captured.SchemaVersion != PrototypeCampPlacementSnapshot.CurrentSchemaVersion ||
                captured.Installed == null ||
                captured.Installed.Length != 2)
            {
                detail = "The v1 capture did not contain both installed structures.";
                return false;
            }

            string capturedJson = JsonUtility.ToJson(captured);
            PrototypeCampPlacementSnapshot roundTripped = JsonUtility.FromJson<PrototypeCampPlacementSnapshot>(capturedJson);
            PrototypeCampPlacement restored = new PrototypeCampPlacement();
            if (!restored.RestoreSnapshot(roundTripped) ||
                !restored.IsInstalledInRoom(StructureKind.Campfire, upperRoom.RoomId) ||
                !restored.IsInstalledInRoom(StructureKind.Workbench, basementRoom.RoomId) ||
                !string.Equals(restored.GetInstalledPlacementZoneId(StructureKind.Campfire), GetZoneId(CampPlacementZone.GeneralGround), StringComparison.Ordinal))
            {
                detail = "The v1 restore did not preserve structure, room, and placement-zone IDs.";
                return false;
            }

            PrototypeCampPlacementSnapshot baseline = restored.CaptureSnapshot();
            restored.Begin(StructureKind.RainCollector, false, CampPlacementRoomZone.StartRoom);
            restored.SetCandidateX(3.5f);
            PrototypeCampPlacementSnapshot invalidId = baseline.Clone();
            invalidId.Installed[0].StableRoomId = "room.invalid";
            if (restored.RestoreSnapshot(invalidId) ||
                !SnapshotsEqual(baseline, restored.CaptureSnapshot()) ||
                !restored.IsActive ||
                restored.SelectedKind != StructureKind.RainCollector ||
                !string.Equals(restored.CandidateRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal) ||
                Mathf.Abs(restored.CandidateX - 3.5f) > OverlapTolerance)
            {
                detail = "Invalid room ID rejection did not preserve installed and active-preview state atomically.";
                return false;
            }

            PrototypeCampPlacementSnapshot invalidZone = baseline.Clone();
            invalidZone.Installed[0].StablePlacementZoneId = GetZoneId(CampPlacementZone.OpenSkyGround);
            if (restored.RestoreSnapshot(invalidZone) || !SnapshotsEqual(baseline, restored.CaptureSnapshot()))
            {
                detail = "Invalid placement-zone ID rejection was not atomic.";
                return false;
            }

            PrototypeCampPlacementSnapshot duplicate = baseline.Clone();
            duplicate.Installed = new[]
            {
                baseline.Installed[0].Clone(),
                baseline.Installed[0].Clone()
            };
            if (restored.RestoreSnapshot(duplicate) || !SnapshotsEqual(baseline, restored.CaptureSnapshot()))
            {
                detail = "Duplicate stable structure ID rejection was not atomic.";
                return false;
            }

            restored.Reset();
            if (restored.InstalledCount != 0 || restored.IsActive || restored.IsRelocating)
            {
                detail = "Reset did not clear placement snapshot state for a new game.";
                return false;
            }

            detail = "v1 capture/restore preserved Upper+Basement placements; invalid and duplicate IDs were rejected atomically; Reset cleared state.";
            return true;
        }

        public static void ExecuteSnapshotContractProbe()
        {
            if (!RunSnapshotContractProbe(out string detail))
            {
                throw new InvalidOperationException("PrototypeCampPlacement snapshot probe failed: " + detail);
            }

            Debug.Log("PrototypeCampPlacement snapshot probe passed: " + detail);
        }

        private static bool TryBuildRestoredPlacements(
            PrototypeCampPlacementSnapshot snapshot,
            out Dictionary<StructureKind, CampInstalledStructurePlacement> restored)
        {
            restored = null;
            if (snapshot == null ||
                snapshot.SchemaVersion != PrototypeCampPlacementSnapshot.CurrentSchemaVersion ||
                snapshot.Installed == null)
            {
                return false;
            }

            Dictionary<StructureKind, CampInstalledStructurePlacement> candidate = new Dictionary<StructureKind, CampInstalledStructurePlacement>();
            HashSet<string> seenStableStructureIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < snapshot.Installed.Length; index += 1)
            {
                CampInstalledStructurePlacementSnapshot entry = snapshot.Installed[index];
                if (entry == null ||
                    !Enum.IsDefined(typeof(StructureKind), entry.Structure) ||
                    !string.Equals(entry.StableStructureId, GetStructureId(entry.Structure), StringComparison.Ordinal) ||
                    !seenStableStructureIds.Add(entry.StableStructureId) ||
                    candidate.ContainsKey(entry.Structure) ||
                    !string.Equals(entry.StablePlacementZoneId, GetZoneId(GetRequiredZone(entry.Structure)), StringComparison.Ordinal) ||
                    !TryGetRoomZone(entry.StableRoomId, out CampPlacementRoomZone roomZone) ||
                    float.IsNaN(entry.X) ||
                    float.IsInfinity(entry.X) ||
                    Mathf.Abs(Snap(entry.X) - entry.X) > OverlapTolerance ||
                    ValidatePlacement(entry.Structure, entry.X, roomZone, candidate) != CampPlacementValidity.Valid)
                {
                    return false;
                }

                candidate.Add(
                    entry.Structure,
                    new CampInstalledStructurePlacement(entry.StableRoomId, entry.StablePlacementZoneId, entry.X));
            }

            restored = candidate;
            return true;
        }

        private static bool SnapshotsEqual(PrototypeCampPlacementSnapshot left, PrototypeCampPlacementSnapshot right)
        {
            if (left == null || right == null ||
                left.SchemaVersion != right.SchemaVersion ||
                left.Installed == null ||
                right.Installed == null ||
                left.Installed.Length != right.Installed.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Installed.Length; index += 1)
            {
                CampInstalledStructurePlacementSnapshot leftEntry = left.Installed[index];
                CampInstalledStructurePlacementSnapshot rightEntry = right.Installed[index];
                if (leftEntry == null || rightEntry == null ||
                    leftEntry.Structure != rightEntry.Structure ||
                    !string.Equals(leftEntry.StableStructureId, rightEntry.StableStructureId, StringComparison.Ordinal) ||
                    !string.Equals(leftEntry.StablePlacementZoneId, rightEntry.StablePlacementZoneId, StringComparison.Ordinal) ||
                    !string.Equals(leftEntry.StableRoomId, rightEntry.StableRoomId, StringComparison.Ordinal) ||
                    Mathf.Abs(leftEntry.X - rightEntry.X) > OverlapTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResetTransientState()
        {
            IsActive = false;
            IsRelocating = false;
            selectedKind = default(StructureKind);
            cursorX = 0f;
            candidateX = 0f;
            activeRoomZone = CampPlacementRoomZone.StartRoom;
        }

        private static float GetDefaultX(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire:
                    return -1.5f;
                case StructureKind.Workbench:
                    return 1.5f;
                case StructureKind.RainCollector:
                    return 3.5f;
                default:
                    return 1.5f;
            }
        }

        private static float GetDefaultX(StructureKind kind, CampPlacementRoomZone roomZone)
        {
            if (roomZone.RoomId == PrototypeCampModuleCatalog.StartRoomId)
            {
                return GetDefaultX(kind);
            }

            float width = Mathf.Max(GridSize, roomZone.BuildMaximumX - roomZone.BuildMinimumX);
            float t = kind == StructureKind.Workbench ? 0.68f : kind == StructureKind.Campfire ? 0.32f : 0.5f;
            return Snap(roomZone.BuildMinimumX + width * t);
        }

        private static float Snap(float value)
        {
            return Mathf.Round(value / GridSize) * GridSize;
        }

        private static bool Intersects(float left, float right, float reservedLeft, float reservedRight)
        {
            return right > reservedLeft + OverlapTolerance && left < reservedRight - OverlapTolerance;
        }
    }
}
