using System;
using System.Collections.Generic;
using System.Linq;
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

    public readonly struct CampPlacementAnchor
    {
        public CampPlacementAnchor(string stableAnchorId, string roomId, float x, CampPlacementZone zone)
        {
            StableAnchorId = stableAnchorId ?? string.Empty;
            RoomId = roomId ?? PrototypeCampModuleCatalog.StartRoomId;
            X = x;
            Zone = zone;
        }

        public string StableAnchorId { get; }
        public string RoomId { get; }
        public float X { get; }
        public CampPlacementZone Zone { get; }
    }

    public readonly struct CampInstalledStructurePlacement
    {
        public CampInstalledStructurePlacement(string roomId, float x)
            : this(roomId, PrototypeCampPlacement.GetZoneId(CampPlacementZone.GeneralGround), string.Empty, x) { }

        public CampInstalledStructurePlacement(string roomId, string stablePlacementZoneId, float x)
            : this(roomId, stablePlacementZoneId, string.Empty, x) { }

        public CampInstalledStructurePlacement(string roomId, string stablePlacementZoneId, string stableAnchorId, float x)
        {
            RoomId = string.IsNullOrWhiteSpace(roomId) ? PrototypeCampModuleCatalog.StartRoomId : roomId;
            StablePlacementZoneId = stablePlacementZoneId ?? string.Empty;
            StableAnchorId = stableAnchorId ?? string.Empty;
            X = x;
        }

        public string RoomId { get; }
        public string StablePlacementZoneId { get; }
        public string StableAnchorId { get; }
        public float X { get; }
    }

    [Serializable]
    public sealed class CampInstalledStructurePlacementSnapshot
    {
        public string StableStructureId = string.Empty;
        public StructureKind Structure;
        public string StablePlacementZoneId = string.Empty;
        public string StableRoomId = string.Empty;
        public string StableAnchorId = string.Empty;
        public float X;

        public CampInstalledStructurePlacementSnapshot Clone()
        {
            return new CampInstalledStructurePlacementSnapshot
            {
                StableStructureId = StableStructureId,
                Structure = Structure,
                StablePlacementZoneId = StablePlacementZoneId,
                StableRoomId = StableRoomId,
                StableAnchorId = StableAnchorId,
                X = X
            };
        }
    }

    [Serializable]
    public sealed class PrototypeCampPlacementSnapshot
    {
        public const int LegacySchemaVersion = 1;
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;
        public CampInstalledStructurePlacementSnapshot[] Installed = Array.Empty<CampInstalledStructurePlacementSnapshot>();

        public PrototypeCampPlacementSnapshot Clone()
        {
            CampInstalledStructurePlacementSnapshot[] source = Installed ?? Array.Empty<CampInstalledStructurePlacementSnapshot>();
            CampInstalledStructurePlacementSnapshot[] copy = new CampInstalledStructurePlacementSnapshot[source.Length];
            for (int index = 0; index < source.Length; index += 1)
                copy[index] = source[index] == null ? null : source[index].Clone();
            return new PrototypeCampPlacementSnapshot { SchemaVersion = SchemaVersion, Installed = copy };
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

        private const float AnchorTolerance = 0.06f;
        private const float HorizontalEngageThreshold = 0.55f;
        private const float HorizontalReleaseThreshold = 0.2f;

        private readonly Dictionary<StructureKind, CampInstalledStructurePlacement> installedPlacements =
            new Dictionary<StructureKind, CampInstalledStructurePlacement>();
        private StructureKind selectedKind;
        private float candidateX;
        private string candidateAnchorId = string.Empty;
        private CampPlacementRoomZone activeRoomZone = CampPlacementRoomZone.StartRoom;
        private bool horizontalLatched;

        public bool IsActive { get; private set; }
        public bool IsRelocating { get; private set; }
        public int InstalledCount { get { return installedPlacements.Count; } }
        public StructureKind SelectedKind { get { return selectedKind; } }
        public float CandidateX { get { return candidateX; } }
        public string CandidateAnchorId { get { return candidateAnchorId; } }
        public string CandidateRoomId { get { return activeRoomZone.RoomId; } }
        public CampPlacementRoomZone ActiveRoomZone { get { return activeRoomZone; } }

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
            get { return IsActive ? ValidateCandidate() : CampPlacementValidity.Valid; }
        }

        public PrototypeLocalizedText CurrentFeedback
        {
            get
            {
                if (!IsActive) return PrototypeLocalizedText.Empty;
                switch (CurrentValidity)
                {
                    case CampPlacementValidity.Valid:
                        return new PrototypeLocalizedText(IsRelocating ? "placement.valid.relocate" : "placement.valid.build", selectedKind);
                    case CampPlacementValidity.WrongZone:
                        return new PrototypeLocalizedText("placement.wrong_zone", selectedKind);
                    case CampPlacementValidity.OverlapsStructure:
                        return new PrototypeLocalizedText("placement.overlap");
                    default:
                        return new PrototypeLocalizedText("placement.outside");
                }
            }
        }

        public void Begin(StructureKind kind, bool relocating)
        {
            Begin(kind, relocating, CampPlacementRoomZone.StartRoom);
        }

        public void Begin(StructureKind kind, bool relocating, CampPlacementRoomZone roomZone)
        {
            if (relocating) EnsureInstalled(kind);
            selectedKind = kind;
            IsRelocating = relocating;
            activeRoomZone = roomZone;
            horizontalLatched = false;
            IsActive = true;

            if (relocating && installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed) &&
                string.Equals(installed.RoomId, roomZone.RoomId, StringComparison.Ordinal) &&
                TryGetAnchor(roomZone.RoomId, installed.StableAnchorId, out CampPlacementAnchor installedAnchor))
            {
                SelectAnchor(installedAnchor);
                return;
            }

            CampPlacementAnchor selected = GetCompatibleAnchors(kind, roomZone.RoomId)
                .FirstOrDefault(anchor => !IsAnchorOccupied(anchor.StableAnchorId, kind));
            if (string.IsNullOrEmpty(selected.StableAnchorId))
                selected = GetCompatibleAnchors(kind, roomZone.RoomId).FirstOrDefault();
            if (!string.IsNullOrEmpty(selected.StableAnchorId)) SelectAnchor(selected);
            else
            {
                candidateAnchorId = string.Empty;
                candidateX = roomZone.BuildMinimumX;
            }
        }

        public void Update(PrototypeCampPlacementActions actions, float deltaTime)
        {
            if (!IsActive) return;
            if (actions.UsePointer)
            {
                SelectNearestCompatibleAnchor(actions.PointerWorldX);
                horizontalLatched = false;
                return;
            }

            float horizontal = actions.Horizontal;
            if (Mathf.Abs(horizontal) <= HorizontalReleaseThreshold) horizontalLatched = false;
            else if (!horizontalLatched && Mathf.Abs(horizontal) >= HorizontalEngageThreshold)
            {
                CycleAnchor(horizontal > 0f ? 1 : -1);
                horizontalLatched = true;
            }
        }

        public void SetCandidateX(float worldX)
        {
            if (!SelectNearestCompatibleAnchor(worldX))
            {
                candidateAnchorId = string.Empty;
                candidateX = worldX;
            }
        }

        public bool Commit()
        {
            if (!IsActive || CurrentValidity != CampPlacementValidity.Valid ||
                !TryGetAnchor(activeRoomZone.RoomId, candidateAnchorId, out CampPlacementAnchor anchor)) return false;
            installedPlacements[selectedKind] = new CampInstalledStructurePlacement(
                activeRoomZone.RoomId, GetZoneId(anchor.Zone), anchor.StableAnchorId, anchor.X);
            IsActive = false;
            IsRelocating = false;
            return true;
        }

        public void Cancel() { IsActive = false; IsRelocating = false; }

        public void Reset()
        {
            installedPlacements.Clear();
            ResetTransientState();
        }

        public void EnsureInstalled(StructureKind kind)
        {
            if (installedPlacements.ContainsKey(kind)) return;
            CampPlacementAnchor anchor = GetCompatibleAnchors(kind, PrototypeCampModuleCatalog.StartRoomId)
                .FirstOrDefault(candidate => !IsAnchorOccupied(candidate.RoomId, candidate.StableAnchorId, kind));
            if (string.IsNullOrEmpty(anchor.StableAnchorId))
                anchor = GetCompatibleAnchors(kind, PrototypeCampModuleCatalog.StartRoomId).FirstOrDefault();
            if (!string.IsNullOrEmpty(anchor.StableAnchorId))
                installedPlacements[kind] = new CampInstalledStructurePlacement(
                    anchor.RoomId, GetZoneId(anchor.Zone), anchor.StableAnchorId, anchor.X);
        }

        public bool HasInstalledPosition(StructureKind kind) { return installedPlacements.ContainsKey(kind); }

        public string GetInstalledRoomId(StructureKind kind)
        {
            return installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed)
                ? installed.RoomId : PrototypeCampModuleCatalog.StartRoomId;
        }

        public string GetInstalledPlacementZoneId(StructureKind kind)
        {
            return installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed)
                ? installed.StablePlacementZoneId : GetZoneId(GetRequiredZone(kind));
        }

        public string GetInstalledAnchorId(StructureKind kind)
        {
            return installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed)
                ? installed.StableAnchorId : string.Empty;
        }

        public bool IsInstalledInRoom(StructureKind kind, string roomId)
        {
            return installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed) &&
                   string.Equals(installed.RoomId, roomId, StringComparison.Ordinal);
        }

        public Vector2 GetInstalledPosition(StructureKind kind)
        {
            float x = installedPlacements.TryGetValue(kind, out CampInstalledStructurePlacement installed)
                ? installed.X : GetDefaultX(kind);
            Vector2 size = GetStructureSize(kind);
            return new Vector2(x, FloorY + size.y * 0.5f);
        }

        public PrototypeCampPlacementSnapshot CaptureSnapshot()
        {
            return new PrototypeCampPlacementSnapshot
            {
                SchemaVersion = PrototypeCampPlacementSnapshot.CurrentSchemaVersion,
                Installed = installedPlacements.OrderBy(entry => (int)entry.Key).Select(entry =>
                    new CampInstalledStructurePlacementSnapshot
                    {
                        StableStructureId = GetStructureId(entry.Key),
                        Structure = entry.Key,
                        StablePlacementZoneId = entry.Value.StablePlacementZoneId,
                        StableRoomId = entry.Value.RoomId,
                        StableAnchorId = entry.Value.StableAnchorId,
                        X = entry.Value.X
                    }).ToArray()
            };
        }

        public bool RestoreSnapshot(PrototypeCampPlacementSnapshot snapshot)
        {
            if (!TryBuildRestoredPlacements(snapshot, out Dictionary<StructureKind, CampInstalledStructurePlacement> restored))
                return false;
            installedPlacements.Clear();
            foreach (KeyValuePair<StructureKind, CampInstalledStructurePlacement> entry in restored)
                installedPlacements.Add(entry.Key, entry.Value);
            ResetTransientState();
            return true;
        }

        public CampPlacementValidity Validate(StructureKind kind, float worldX)
        {
            CampPlacementAnchor anchor = GetCompatibleAnchors(kind, activeRoomZone.RoomId)
                .OrderBy(candidate => Mathf.Abs(candidate.X - worldX)).FirstOrDefault();
            if (string.IsNullOrEmpty(anchor.StableAnchorId) || Mathf.Abs(anchor.X - worldX) > GridSize + AnchorTolerance)
                return CampPlacementValidity.OutsideCampBounds;
            return ValidateAnchor(kind, anchor, installedPlacements);
        }

        public static IReadOnlyList<CampPlacementAnchor> GetAnchorsForRoom(string roomId)
        {
            if (!TryGetRoomZone(roomId, out CampPlacementRoomZone zone)) return Array.Empty<CampPlacementAnchor>();
            List<CampPlacementAnchor> anchors = new List<CampPlacementAnchor>();
            if (string.Equals(roomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
            {
                anchors.Add(new CampPlacementAnchor("anchor.start.indoor.left", roomId, -1.5f, CampPlacementZone.GeneralGround));
                anchors.Add(new CampPlacementAnchor("anchor.start.indoor.center", roomId, 0f, CampPlacementZone.GeneralGround));
                anchors.Add(new CampPlacementAnchor("anchor.start.indoor.right", roomId, 1.5f, CampPlacementZone.GeneralGround));
                anchors.Add(new CampPlacementAnchor("anchor.start.outdoor.rain", roomId, 3.5f, CampPlacementZone.OpenSkyGround));
                return anchors;
            }

            float minimum = zone.BuildMinimumX + 1f;
            float maximum = zone.BuildMaximumX - 1f;
            float middle = (minimum + maximum) * 0.5f;
            string suffix = roomId.Replace("room.", string.Empty).Replace('.', '-');
            anchors.Add(new CampPlacementAnchor("anchor." + suffix + ".left", roomId, Snap(minimum), CampPlacementZone.GeneralGround));
            anchors.Add(new CampPlacementAnchor("anchor." + suffix + ".center", roomId, Snap(middle), CampPlacementZone.GeneralGround));
            anchors.Add(new CampPlacementAnchor("anchor." + suffix + ".right", roomId, Snap(maximum), CampPlacementZone.GeneralGround));
            return anchors;
        }

        public static string GetStructureId(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire: return "structure.campfire";
                case StructureKind.Workbench: return "structure.workbench";
                case StructureKind.RainCollector: return "structure.rain_collector";
                case StructureKind.Bed: return "structure.bed";
                case StructureKind.Sofa: return "structure.sofa";
                default: return string.Empty;
            }
        }

        public static CampPlacementZone GetRequiredZone(StructureKind kind)
        {
            return kind == StructureKind.RainCollector ? CampPlacementZone.OpenSkyGround : CampPlacementZone.GeneralGround;
        }

        public static string GetZoneId(CampPlacementZone zone)
        {
            switch (zone)
            {
                case CampPlacementZone.OpenSkyGround: return "camp.open-sky-ground";
                case CampPlacementZone.SignalAnchor: return "camp.signal-anchor";
                default: return "camp.general-ground";
            }
        }

        public static bool TryGetRoomZone(string stableRoomId, out CampPlacementRoomZone roomZone)
        {
            if (string.Equals(stableRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
            {
                roomZone = CampPlacementRoomZone.StartRoom;
                return true;
            }
            foreach (CampModuleDefinition definition in PrototypeCampModuleCatalog.All)
            {
                if (!string.Equals(stableRoomId, definition.RoomId, StringComparison.Ordinal)) continue;
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
                case StructureKind.Campfire: return new Vector2(1.5f, 0.9f);
                case StructureKind.Workbench: return new Vector2(2f, 1.2f);
                case StructureKind.RainCollector: return new Vector2(1.7f, 1.7f);
                case StructureKind.Bed: return new Vector2(2.2f, 1.05f);
                case StructureKind.Sofa: return new Vector2(2.1f, 1.25f);
                default: return Vector2.one;
            }
        }

        public static bool RunSnapshotContractProbe(out string detail)
        {
            if (!TryGetRoomZone("room.upper.standard", out CampPlacementRoomZone upperRoom) ||
                !TryGetRoomZone("room.basement.standard", out CampPlacementRoomZone basementRoom))
            {
                detail = "Known module room IDs did not resolve to fixed-anchor rooms.";
                return false;
            }

            PrototypeCampPlacement source = new PrototypeCampPlacement();
            source.Begin(StructureKind.Campfire, false, upperRoom);
            if (source.CurrentValidity != CampPlacementValidity.Valid || !source.Commit())
            {
                detail = "Upper-room campfire anchor could not be committed.";
                return false;
            }
            source.Begin(StructureKind.Workbench, false, basementRoom);
            if (source.CurrentValidity != CampPlacementValidity.Valid || !source.Commit())
            {
                detail = "Basement workbench anchor could not be committed.";
                return false;
            }

            PrototypeCampPlacementSnapshot captured = source.CaptureSnapshot();
            if (captured.SchemaVersion != PrototypeCampPlacementSnapshot.CurrentSchemaVersion ||
                captured.Installed.Length != 2 || captured.Installed.Any(entry => string.IsNullOrEmpty(entry.StableAnchorId)))
            {
                detail = "The v2 capture did not contain two stable anchor IDs.";
                return false;
            }

            PrototypeCampPlacement restored = new PrototypeCampPlacement();
            if (!restored.RestoreSnapshot(JsonUtility.FromJson<PrototypeCampPlacementSnapshot>(JsonUtility.ToJson(captured))) ||
                !restored.IsInstalledInRoom(StructureKind.Campfire, upperRoom.RoomId) ||
                !restored.IsInstalledInRoom(StructureKind.Workbench, basementRoom.RoomId))
            {
                detail = "The v2 fixed-anchor round trip failed.";
                return false;
            }

            PrototypeCampPlacementSnapshot legacy = captured.Clone();
            legacy.SchemaVersion = PrototypeCampPlacementSnapshot.LegacySchemaVersion;
            foreach (CampInstalledStructurePlacementSnapshot entry in legacy.Installed) entry.StableAnchorId = string.Empty;
            PrototypeCampPlacement migrated = new PrototypeCampPlacement();
            if (!migrated.RestoreSnapshot(legacy) ||
                string.IsNullOrEmpty(migrated.GetInstalledAnchorId(StructureKind.Campfire)) ||
                string.IsNullOrEmpty(migrated.GetInstalledAnchorId(StructureKind.Workbench)))
            {
                detail = "The v1 coordinate-to-anchor migration failed.";
                return false;
            }

            PrototypeCampPlacementSnapshot baseline = restored.CaptureSnapshot();
            PrototypeCampPlacementSnapshot invalid = baseline.Clone();
            invalid.Installed[0].StableAnchorId = "anchor.invalid";
            if (restored.RestoreSnapshot(invalid) || !SnapshotsEqual(baseline, restored.CaptureSnapshot()))
            {
                detail = "Invalid anchor rejection was not atomic.";
                return false;
            }

            restored.Reset();
            if (restored.InstalledCount != 0 || restored.IsActive || restored.IsRelocating)
            {
                detail = "Reset did not clear anchor placement state.";
                return false;
            }

            detail = "v2 room+anchor round trip, v1 coordinate migration, invalid-anchor atomic rejection and Reset passed.";
            return true;
        }

        public static void ExecuteSnapshotContractProbe()
        {
            if (!RunSnapshotContractProbe(out string detail))
                throw new InvalidOperationException("PrototypeCampPlacement snapshot probe failed: " + detail);
            Debug.Log("PrototypeCampPlacement snapshot probe passed: " + detail);
        }

        private CampPlacementValidity ValidateCandidate()
        {
            if (!TryGetAnchor(activeRoomZone.RoomId, candidateAnchorId, out CampPlacementAnchor anchor))
                return CampPlacementValidity.OutsideCampBounds;
            return ValidateAnchor(selectedKind, anchor, installedPlacements);
        }

        private static CampPlacementValidity ValidateAnchor(
            StructureKind kind,
            CampPlacementAnchor anchor,
            IReadOnlyDictionary<StructureKind, CampInstalledStructurePlacement> placements)
        {
            if (anchor.Zone != GetRequiredZone(kind)) return CampPlacementValidity.WrongZone;
            foreach (KeyValuePair<StructureKind, CampInstalledStructurePlacement> installed in placements)
            {
                if (installed.Key == kind) continue;
                if (string.Equals(installed.Value.RoomId, anchor.RoomId, StringComparison.Ordinal) &&
                    string.Equals(installed.Value.StableAnchorId, anchor.StableAnchorId, StringComparison.Ordinal))
                    return CampPlacementValidity.OverlapsStructure;
            }
            return CampPlacementValidity.Valid;
        }

        private static IReadOnlyList<CampPlacementAnchor> GetCompatibleAnchors(StructureKind kind, string roomId)
        {
            return GetAnchorsForRoom(roomId).Where(anchor => anchor.Zone == GetRequiredZone(kind)).ToArray();
        }

        private bool IsAnchorOccupied(string anchorId, StructureKind exceptKind)
        {
            return IsAnchorOccupied(activeRoomZone.RoomId, anchorId, exceptKind);
        }

        private bool IsAnchorOccupied(string roomId, string anchorId, StructureKind exceptKind)
        {
            return installedPlacements.Any(entry => entry.Key != exceptKind &&
                string.Equals(entry.Value.RoomId, roomId, StringComparison.Ordinal) &&
                string.Equals(entry.Value.StableAnchorId, anchorId, StringComparison.Ordinal));
        }

        private bool SelectNearestCompatibleAnchor(float preferredX)
        {
            CampPlacementAnchor anchor = GetCompatibleAnchors(selectedKind, activeRoomZone.RoomId)
                .OrderBy(candidate => Mathf.Abs(candidate.X - preferredX)).FirstOrDefault();
            if (string.IsNullOrEmpty(anchor.StableAnchorId)) return false;
            SelectAnchor(anchor);
            return true;
        }

        private void CycleAnchor(int direction)
        {
            CampPlacementAnchor[] anchors = GetCompatibleAnchors(selectedKind, activeRoomZone.RoomId)
                .OrderBy(anchor => anchor.X).ToArray();
            if (anchors.Length == 0) return;
            int current = Array.FindIndex(anchors, anchor => string.Equals(anchor.StableAnchorId, candidateAnchorId, StringComparison.Ordinal));
            int next = current < 0 ? 0 : (current + direction + anchors.Length) % anchors.Length;
            SelectAnchor(anchors[next]);
        }

        private void SelectAnchor(CampPlacementAnchor anchor)
        {
            candidateAnchorId = anchor.StableAnchorId;
            candidateX = anchor.X;
        }

        private static bool TryGetAnchor(string roomId, string anchorId, out CampPlacementAnchor anchor)
        {
            anchor = GetAnchorsForRoom(roomId).FirstOrDefault(candidate =>
                string.Equals(candidate.StableAnchorId, anchorId, StringComparison.Ordinal));
            return !string.IsNullOrEmpty(anchor.StableAnchorId);
        }

        private static bool TryBuildRestoredPlacements(
            PrototypeCampPlacementSnapshot snapshot,
            out Dictionary<StructureKind, CampInstalledStructurePlacement> restored)
        {
            restored = null;
            if (snapshot == null || snapshot.Installed == null ||
                (snapshot.SchemaVersion != PrototypeCampPlacementSnapshot.LegacySchemaVersion &&
                 snapshot.SchemaVersion != PrototypeCampPlacementSnapshot.CurrentSchemaVersion)) return false;

            bool legacy = snapshot.SchemaVersion == PrototypeCampPlacementSnapshot.LegacySchemaVersion;
            Dictionary<StructureKind, CampInstalledStructurePlacement> candidate =
                new Dictionary<StructureKind, CampInstalledStructurePlacement>();
            HashSet<string> stableIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> occupiedAnchors = new HashSet<string>(StringComparer.Ordinal);
            foreach (CampInstalledStructurePlacementSnapshot entry in snapshot.Installed)
            {
                if (entry == null || !Enum.IsDefined(typeof(StructureKind), entry.Structure) ||
                    !string.Equals(entry.StableStructureId, GetStructureId(entry.Structure), StringComparison.Ordinal) ||
                    !stableIds.Add(entry.StableStructureId) || !TryGetRoomZone(entry.StableRoomId, out _)) return false;

                CampPlacementAnchor anchor;
                if (legacy)
                {
                    anchor = GetAnchorsForRoom(entry.StableRoomId)
                        .Where(value => value.Zone == GetRequiredZone(entry.Structure) &&
                                        !occupiedAnchors.Contains(value.RoomId + "|" + value.StableAnchorId))
                        .OrderBy(value => Mathf.Abs(value.X - entry.X)).FirstOrDefault();
                }
                else if (!TryGetAnchor(entry.StableRoomId, entry.StableAnchorId, out anchor)) return false;

                string occupiedKey = anchor.RoomId + "|" + anchor.StableAnchorId;
                if (string.IsNullOrEmpty(anchor.StableAnchorId) || anchor.Zone != GetRequiredZone(entry.Structure) ||
                    !string.Equals(entry.StablePlacementZoneId, GetZoneId(anchor.Zone), StringComparison.Ordinal) ||
                    !occupiedAnchors.Add(occupiedKey)) return false;

                candidate.Add(entry.Structure, new CampInstalledStructurePlacement(
                    anchor.RoomId, GetZoneId(anchor.Zone), anchor.StableAnchorId, anchor.X));
            }
            restored = candidate;
            return true;
        }

        private static bool SnapshotsEqual(PrototypeCampPlacementSnapshot left, PrototypeCampPlacementSnapshot right)
        {
            if (left == null || right == null || left.SchemaVersion != right.SchemaVersion ||
                left.Installed == null || right.Installed == null || left.Installed.Length != right.Installed.Length) return false;
            for (int index = 0; index < left.Installed.Length; index += 1)
            {
                CampInstalledStructurePlacementSnapshot a = left.Installed[index];
                CampInstalledStructurePlacementSnapshot b = right.Installed[index];
                if (a == null || b == null || a.Structure != b.Structure ||
                    !string.Equals(a.StableStructureId, b.StableStructureId, StringComparison.Ordinal) ||
                    !string.Equals(a.StablePlacementZoneId, b.StablePlacementZoneId, StringComparison.Ordinal) ||
                    !string.Equals(a.StableRoomId, b.StableRoomId, StringComparison.Ordinal) ||
                    !string.Equals(a.StableAnchorId, b.StableAnchorId, StringComparison.Ordinal) ||
                    Mathf.Abs(a.X - b.X) > AnchorTolerance) return false;
            }
            return true;
        }

        private void ResetTransientState()
        {
            IsActive = false;
            IsRelocating = false;
            selectedKind = default(StructureKind);
            candidateX = 0f;
            candidateAnchorId = string.Empty;
            activeRoomZone = CampPlacementRoomZone.StartRoom;
            horizontalLatched = false;
        }

        private static float GetDefaultX(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire: return -1.5f;
                case StructureKind.Workbench: return 1.5f;
                case StructureKind.RainCollector: return 3.5f;
                case StructureKind.Bed: return -1.5f;
                case StructureKind.Sofa: return 1.5f;
                default: return 1.5f;
            }
        }

        private static float Snap(float value) { return Mathf.Round(value / GridSize) * GridSize; }
    }
}
