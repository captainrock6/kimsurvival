using System;
using UnityEngine;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeCampUseSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string StableRoomId = PrototypeCampModuleCatalog.StartRoomId;
        public float PlayerX = PrototypeCampUse.PlayerStartX;
        public float PlayerY = PrototypeCampUse.PlayerFloorY;
        public float FacingDirection = 1f;
        public bool CampfirePrepared;
        public bool RainCollectorPrepared;

        public PrototypeCampUseSnapshot Clone()
        {
            return new PrototypeCampUseSnapshot
            {
                SchemaVersion = SchemaVersion,
                StableRoomId = StableRoomId,
                PlayerX = PlayerX,
                PlayerY = PlayerY,
                FacingDirection = FacingDirection,
                CampfirePrepared = CampfirePrepared,
                RainCollectorPrepared = RainCollectorPrepared
            };
        }
    }

    public sealed class PrototypeCampUse
    {
        public const float UseRange = 1.25f;
        public const float PlayerStartX = -5f;
        public const float PlayerFloorY = PrototypeCampPlacement.FloorY;
        public const float PlayerMinimumX = -5.6f;
        public const float PlayerMaximumX = 8.6f;

        private const float MovementSpeed = 3f;

        private bool campfirePrepared;
        private bool rainCollectorPrepared;

        public PrototypeCampUse()
        {
            Reset();
        }

        public Vector2 PlayerPosition { get; private set; }
        public float FacingDirection { get; private set; }
        public string CurrentRoomId { get; private set; }

        public void Reset()
        {
            PlayerPosition = new Vector2(PlayerStartX, PlayerFloorY);
            FacingDirection = 1f;
            CurrentRoomId = PrototypeCampModuleCatalog.StartRoomId;
            ClearDayBenefits();
        }

        public void Step(PrototypePlayerActions actions, float deltaTime)
        {
            if (Mathf.Abs(actions.Horizontal) > 0.01f)
            {
                FacingDirection = actions.Horizontal < 0f ? -1f : 1f;
            }

            float x = Mathf.Clamp(
                PlayerPosition.x + actions.Horizontal * MovementSpeed * Mathf.Max(0f, deltaTime),
                PlayerMinimumX,
                PlayerMaximumX);
            PlayerPosition = new Vector2(x, PlayerFloorY);
        }

        public void Warp(float worldX)
        {
            PrototypeProductionActionCounters.RecordWarp();
            PlayerPosition = new Vector2(Mathf.Clamp(worldX, PlayerMinimumX, PlayerMaximumX), PlayerFloorY);
        }

        public void Warp(Vector2 position)
        {
            PrototypeProductionActionCounters.RecordWarp();
            PlayerPosition = new Vector2(Mathf.Clamp(position.x, PlayerMinimumX, PlayerMaximumX), PlayerFloorY);
        }

        public void EnterRoom(string roomId, float landingX)
        {
            CurrentRoomId = string.IsNullOrWhiteSpace(roomId) ? PrototypeCampModuleCatalog.StartRoomId : roomId;
            PlayerPosition = new Vector2(Mathf.Clamp(landingX, PlayerMinimumX, PlayerMaximumX), PlayerFloorY);
        }

        public void Restore(CampModuleReturnSnapshot snapshot)
        {
            CurrentRoomId = string.IsNullOrWhiteSpace(snapshot.RoomId)
                ? PrototypeCampModuleCatalog.StartRoomId
                : snapshot.RoomId;
            FacingDirection = snapshot.FacingDirection < 0f ? -1f : 1f;
            PlayerPosition = new Vector2(
                Mathf.Clamp(snapshot.Position.x, PlayerMinimumX, PlayerMaximumX),
                PlayerFloorY);
        }

        public PrototypeCampUseSnapshot CaptureSnapshot()
        {
            return new PrototypeCampUseSnapshot
            {
                SchemaVersion = PrototypeCampUseSnapshot.CurrentSchemaVersion,
                StableRoomId = CurrentRoomId,
                PlayerX = PlayerPosition.x,
                PlayerY = PlayerPosition.y,
                FacingDirection = FacingDirection,
                CampfirePrepared = campfirePrepared,
                RainCollectorPrepared = rainCollectorPrepared
            };
        }

        public bool RestoreSnapshot(PrototypeCampUseSnapshot snapshot)
        {
            if (snapshot == null ||
                snapshot.SchemaVersion != PrototypeCampUseSnapshot.CurrentSchemaVersion ||
                !PrototypeCampPlacement.TryGetRoomZone(snapshot.StableRoomId, out _) ||
                !IsFinite(snapshot.PlayerX) ||
                !IsFinite(snapshot.PlayerY) ||
                !IsFinite(snapshot.FacingDirection) ||
                snapshot.PlayerX < PlayerMinimumX ||
                snapshot.PlayerX > PlayerMaximumX ||
                !Mathf.Approximately(Mathf.Abs(snapshot.FacingDirection), 1f))
            {
                return false;
            }

            CurrentRoomId = snapshot.StableRoomId;
            PlayerPosition = new Vector2(snapshot.PlayerX, PlayerFloorY);
            FacingDirection = snapshot.FacingDirection;
            campfirePrepared = snapshot.CampfirePrepared;
            rainCollectorPrepared = snapshot.RainCollectorPrepared;
            return true;
        }

        public bool IsWithinUseRange(Vector2 targetPosition)
        {
            return Vector2.Distance(PlayerPosition, targetPosition) <= UseRange + 0.0001f;
        }

        public bool TryPrepareDayBenefit(StructureKind kind, Vector2 targetPosition)
        {
            if (!IsWithinUseRange(targetPosition))
            {
                return false;
            }

            switch (kind)
            {
                case StructureKind.Campfire:
                    campfirePrepared = true;
                    return true;
                case StructureKind.RainCollector:
                    rainCollectorPrepared = true;
                    return true;
                default:
                    return false;
            }
        }

        public bool IsDayBenefitPrepared(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire:
                    return campfirePrepared;
                case StructureKind.RainCollector:
                    return rainCollectorPrepared;
                default:
                    return false;
            }
        }

        public void ClearDayBenefits()
        {
            campfirePrepared = false;
            rainCollectorPrepared = false;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
