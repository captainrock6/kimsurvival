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
        {
            RoomId = string.IsNullOrWhiteSpace(roomId) ? PrototypeCampModuleCatalog.StartRoomId : roomId;
            X = x;
        }

        public string RoomId { get; }
        public float X { get; }
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

            installedPlacements[selectedKind] = new CampInstalledStructurePlacement(activeRoomZone.RoomId, candidateX);
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
            IsActive = false;
            IsRelocating = false;
            selectedKind = default(StructureKind);
            cursorX = 0f;
            candidateX = 0f;
            activeRoomZone = CampPlacementRoomZone.StartRoom;
        }

        public void EnsureInstalled(StructureKind kind)
        {
            if (!installedPlacements.ContainsKey(kind))
            {
                installedPlacements[kind] = new CampInstalledStructurePlacement(
                    PrototypeCampModuleCatalog.StartRoomId,
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

        public CampPlacementValidity Validate(StructureKind kind, float worldX)
        {
            Vector2 size = GetStructureSize(kind);
            float halfWidth = size.x * 0.5f;
            float left = worldX - halfWidth;
            float right = worldX + halfWidth;
            if (left < activeRoomZone.BuildMinimumX || right > activeRoomZone.BuildMaximumX)
            {
                return CampPlacementValidity.OutsideCampBounds;
            }

            if (GetRequiredZone(kind) == CampPlacementZone.OpenSkyGround &&
                (!activeRoomZone.AllowsOpenSky || left < activeRoomZone.OpenSkyMinimumX || right > activeRoomZone.OpenSkyMaximumX))
            {
                return CampPlacementValidity.WrongZone;
            }

            if (Intersects(left, right, activeRoomZone.EntranceMinimumX, activeRoomZone.EntranceMaximumX))
            {
                return CampPlacementValidity.BlocksEntrance;
            }

            if (Intersects(left, right, activeRoomZone.RequiredPathMinimumX, activeRoomZone.RequiredPathMaximumX))
            {
                return CampPlacementValidity.BlocksRequiredPath;
            }

            foreach (KeyValuePair<StructureKind, CampInstalledStructurePlacement> installed in installedPlacements)
            {
                if (installed.Key == kind || installed.Value.RoomId != activeRoomZone.RoomId)
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
