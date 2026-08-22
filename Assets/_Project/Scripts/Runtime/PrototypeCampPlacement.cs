using System.Collections.Generic;
using UnityEngine;

namespace KimSurvival
{
    public enum CampPlacementValidity
    {
        Valid,
        OutsideCampBounds,
        OverlapsStructure,
        BlocksEntrance,
        BlocksRequiredPath
    }

    public sealed class PrototypeCampPlacement
    {
        public const float GridSize = 0.5f;
        public const float BuildMinimumX = -3.6f;
        public const float BuildMaximumX = 4.7f;
        public const float FloorY = -2.8f;
        public const float EntranceMinimumX = -3.6f;
        public const float EntranceMaximumX = -2.6f;
        public const float RequiredPathMinimumX = -0.4f;
        public const float RequiredPathMaximumX = 0.4f;

        private const float GamepadCursorSpeed = 3f;
        private const float OverlapTolerance = 0.001f;

        private readonly Dictionary<StructureKind, float> installedX = new Dictionary<StructureKind, float>();
        private StructureKind selectedKind;
        private float cursorX;
        private float candidateX;

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

        public string CurrentFeedback
        {
            get
            {
                if (!IsActive)
                {
                    return string.Empty;
                }

                string action = IsRelocating ? "재배치" : "배치";
                switch (CurrentValidity)
                {
                    case CampPlacementValidity.Valid:
                        return GetStructureName(selectedKind) + " " + action + " 가능 · 0.5m 바닥 스냅";
                    case CampPlacementValidity.OutsideCampBounds:
                        return "캠프의 호환 건설 구역 밖에는 놓을 수 없다.";
                    case CampPlacementValidity.OverlapsStructure:
                        return "다른 설비와 겹친다. 김씨도 물건을 포개 쓰지는 않는다.";
                    case CampPlacementValidity.BlocksEntrance:
                        return "캠프 출입구를 막을 수 없다.";
                    case CampPlacementValidity.BlocksRequiredPath:
                        return "필수 이동 통로를 비워 둬야 한다.";
                    default:
                        return "배치할 수 없는 위치다.";
                }
            }
        }

        public void Begin(StructureKind kind, bool relocating)
        {
            if (relocating)
            {
                EnsureInstalled(kind);
            }

            selectedKind = kind;
            IsRelocating = relocating;
            candidateX = relocating && installedX.ContainsKey(kind) ? installedX[kind] : GetDefaultX(kind);
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

            installedX[selectedKind] = candidateX;
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
            installedX.Clear();
            IsActive = false;
            IsRelocating = false;
            selectedKind = default(StructureKind);
            cursorX = 0f;
            candidateX = 0f;
        }

        public void EnsureInstalled(StructureKind kind)
        {
            if (!installedX.ContainsKey(kind))
            {
                installedX[kind] = GetDefaultX(kind);
            }
        }

        public bool HasInstalledPosition(StructureKind kind)
        {
            return installedX.ContainsKey(kind);
        }

        public Vector2 GetInstalledPosition(StructureKind kind)
        {
            float x = installedX.ContainsKey(kind) ? installedX[kind] : GetDefaultX(kind);
            Vector2 size = GetStructureSize(kind);
            return new Vector2(x, FloorY + size.y * 0.5f);
        }

        public CampPlacementValidity Validate(StructureKind kind, float worldX)
        {
            Vector2 size = GetStructureSize(kind);
            float halfWidth = size.x * 0.5f;
            float left = worldX - halfWidth;
            float right = worldX + halfWidth;
            if (left < BuildMinimumX || right > BuildMaximumX)
            {
                return CampPlacementValidity.OutsideCampBounds;
            }

            if (Intersects(left, right, EntranceMinimumX, EntranceMaximumX))
            {
                return CampPlacementValidity.BlocksEntrance;
            }

            if (Intersects(left, right, RequiredPathMinimumX, RequiredPathMaximumX))
            {
                return CampPlacementValidity.BlocksRequiredPath;
            }

            foreach (KeyValuePair<StructureKind, float> installed in installedX)
            {
                if (installed.Key == kind)
                {
                    continue;
                }

                float combinedHalfWidth = (size.x + GetStructureSize(installed.Key).x) * 0.5f;
                if (Mathf.Abs(worldX - installed.Value) < combinedHalfWidth - OverlapTolerance)
                {
                    return CampPlacementValidity.OverlapsStructure;
                }
            }

            return CampPlacementValidity.Valid;
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

        public static string GetStructureName(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire:
                    return "모닥불";
                case StructureKind.Workbench:
                    return "작업대";
                case StructureKind.RainCollector:
                    return "빗물받이";
                default:
                    return "설비";
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
