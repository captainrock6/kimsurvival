using System;
using System.Collections.Generic;
using UnityEngine;

namespace KimSurvival
{
    public enum CampVerticalTraversalState
    {
        Idle,
        Climbing
    }

    public readonly struct CampVerticalLadderDefinition
    {
        public CampVerticalLadderDefinition(
            string stableLadderId,
            CampModuleArchetype archetype,
            float x,
            string lowerRoomId,
            float lowerFloorY,
            string upperRoomId,
            float upperFloorY)
        {
            StableLadderId = stableLadderId ?? string.Empty;
            Archetype = archetype;
            X = x;
            LowerRoomId = lowerRoomId ?? string.Empty;
            LowerFloorY = lowerFloorY;
            UpperRoomId = upperRoomId ?? string.Empty;
            UpperFloorY = upperFloorY;
        }

        public string StableLadderId { get; }
        public CampModuleArchetype Archetype { get; }
        public float X { get; }
        public string LowerRoomId { get; }
        public float LowerFloorY { get; }
        public string UpperRoomId { get; }
        public float UpperFloorY { get; }
    }

    public static class PrototypeCampVerticalLayout
    {
        public const float UpperFloorY = 1.4f;
        public const float BasementFloorY = -5.45f;
        public const float LadderActivationHalfWidth = 0.68f;
        public const float VerticalInputThreshold = 0.45f;
        public const float ClimbSpeed = 2.45f;
        public const float CameraSmoothTime = 0.18f;
        public const float CameraMaximumSpeed = 7f;
        public const float ExpandedCameraOrthographicSize = 5.1f;
        // Preserve the established composition where Kim stands in the lower half
        // of the screen instead of placing the active floor through screen centre.
        public const float CameraFramingOffsetY = -PrototypeCampUse.PlayerFloorY;

        public static float FloorY(string stableRoomId)
        {
            if (string.Equals(stableRoomId, PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).RoomId, StringComparison.Ordinal))
            {
                return UpperFloorY;
            }
            if (string.Equals(stableRoomId, PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement).RoomId, StringComparison.Ordinal))
            {
                return BasementFloorY;
            }
            return PrototypeCampUse.PlayerFloorY;
        }

        public static Vector2 ToWorldPosition(string stableRoomId, Vector2 roomLocalPosition)
        {
            return new Vector2(
                roomLocalPosition.x,
                roomLocalPosition.y + FloorY(stableRoomId) - PrototypeCampUse.PlayerFloorY);
        }

        public static CampVerticalLadderDefinition Ladder(CampModuleArchetype archetype)
        {
            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(archetype);
            if (archetype == CampModuleArchetype.Upper)
            {
                return new CampVerticalLadderDefinition(
                    "ladder.start.upper",
                    archetype,
                    definition.StartConnectorDisplayX,
                    PrototypeCampModuleCatalog.StartRoomId,
                    PrototypeCampUse.PlayerFloorY,
                    definition.RoomId,
                    UpperFloorY);
            }
            if (archetype == CampModuleArchetype.Basement)
            {
                return new CampVerticalLadderDefinition(
                    "ladder.start.basement",
                    archetype,
                    definition.StartConnectorDisplayX,
                    definition.RoomId,
                    BasementFloorY,
                    PrototypeCampModuleCatalog.StartRoomId,
                    PrototypeCampUse.PlayerFloorY);
            }
            throw new ArgumentOutOfRangeException(nameof(archetype), archetype, "Side rooms use a door, not a vertical ladder.");
        }

        public static void BuiltCameraRange(
            PrototypeCampModuleExpansion expansion,
            out float minimumY,
            out float maximumY)
        {
            float minimumFloor = expansion != null && expansion.IsCommitted(CampModuleArchetype.Basement)
                ? BasementFloorY
                : PrototypeCampUse.PlayerFloorY;
            float maximumFloor = expansion != null && expansion.IsCommitted(CampModuleArchetype.Upper)
                ? UpperFloorY
                : PrototypeCampUse.PlayerFloorY;
            minimumY = minimumFloor + CameraFramingOffsetY;
            maximumY = maximumFloor + CameraFramingOffsetY;
        }
    }

    public readonly struct CampVerticalTraversalStep
    {
        public CampVerticalTraversalStep(
            CampVerticalTraversalState state,
            bool consumedMovement,
            bool started,
            bool completed,
            string stableLadderId,
            string stableRoomId,
            Vector2 playerPosition)
        {
            State = state;
            ConsumedMovement = consumedMovement;
            Started = started;
            Completed = completed;
            StableLadderId = stableLadderId ?? string.Empty;
            StableRoomId = stableRoomId ?? string.Empty;
            PlayerPosition = playerPosition;
        }

        public CampVerticalTraversalState State { get; }
        public bool ConsumedMovement { get; }
        public bool Started { get; }
        public bool Completed { get; }
        public string StableLadderId { get; }
        public string StableRoomId { get; }
        public Vector2 PlayerPosition { get; }
    }

    public sealed class PrototypeCampVerticalTraversal
    {
        private CampVerticalLadderDefinition activeLadder;
        private string originRoomId = string.Empty;

        public CampVerticalTraversalState State { get; private set; }

        public bool IsClimbing
        {
            get { return State == CampVerticalTraversalState.Climbing; }
        }

        public string ActiveLadderId
        {
            get { return IsClimbing ? activeLadder.StableLadderId : string.Empty; }
        }

        public void Reset()
        {
            State = CampVerticalTraversalState.Idle;
            activeLadder = default(CampVerticalLadderDefinition);
            originRoomId = string.Empty;
        }

        public CampVerticalTraversalStep Step(
            PrototypeCampUse campUse,
            PrototypeCampModuleExpansion expansion,
            float verticalInput,
            float deltaTime)
        {
            if (campUse == null || expansion == null)
            {
                return IdleStep(campUse);
            }

            float input = Mathf.Abs(verticalInput) >= PrototypeCampVerticalLayout.VerticalInputThreshold
                ? Mathf.Sign(verticalInput)
                : 0f;
            bool started = false;
            if (!IsClimbing)
            {
                if (input == 0f || !TryFindStartLadder(campUse, expansion, input, out activeLadder))
                {
                    return IdleStep(campUse);
                }

                State = CampVerticalTraversalState.Climbing;
                originRoomId = campUse.CurrentRoomId;
                campUse.SetVerticalTraversalPosition(activeLadder.X, campUse.PlayerPosition.y);
                started = true;
            }

            if (input == 0f)
            {
                return ActiveStep(campUse, started, false);
            }

            float nextY = Mathf.Clamp(
                campUse.PlayerPosition.y + input * PrototypeCampVerticalLayout.ClimbSpeed * Mathf.Max(0f, deltaTime),
                activeLadder.LowerFloorY,
                activeLadder.UpperFloorY);
            campUse.SetVerticalTraversalPosition(activeLadder.X, nextY);

            bool reachedUpper = input > 0f && nextY >= activeLadder.UpperFloorY - 0.0001f;
            bool reachedLower = input < 0f && nextY <= activeLadder.LowerFloorY + 0.0001f;
            if (!reachedUpper && !reachedLower)
            {
                return ActiveStep(campUse, started, false);
            }

            string destination = reachedUpper ? activeLadder.UpperRoomId : activeLadder.LowerRoomId;
            float destinationFloor = reachedUpper ? activeLadder.UpperFloorY : activeLadder.LowerFloorY;
            campUse.CompleteVerticalTraversal(destination, activeLadder.X, destinationFloor);
            string completedLadderId = activeLadder.StableLadderId;
            Reset();
            return new CampVerticalTraversalStep(
                CampVerticalTraversalState.Idle,
                true,
                started,
                true,
                completedLadderId,
                campUse.CurrentRoomId,
                campUse.PlayerPosition);
        }

        private bool TryFindStartLadder(
            PrototypeCampUse campUse,
            PrototypeCampModuleExpansion expansion,
            float input,
            out CampVerticalLadderDefinition ladder)
        {
            CampModuleArchetype[] verticalArchetypes =
            {
                CampModuleArchetype.Upper,
                CampModuleArchetype.Basement
            };
            for (int index = 0; index < verticalArchetypes.Length; index += 1)
            {
                CampModuleArchetype archetype = verticalArchetypes[index];
                if (!expansion.IsCommitted(archetype)) continue;
                CampVerticalLadderDefinition candidate = PrototypeCampVerticalLayout.Ladder(archetype);
                if (Mathf.Abs(campUse.PlayerPosition.x - candidate.X) > PrototypeCampVerticalLayout.LadderActivationHalfWidth)
                {
                    continue;
                }

                bool fromLower = string.Equals(campUse.CurrentRoomId, candidate.LowerRoomId, StringComparison.Ordinal) && input > 0f;
                bool fromUpper = string.Equals(campUse.CurrentRoomId, candidate.UpperRoomId, StringComparison.Ordinal) && input < 0f;
                if (fromLower || fromUpper)
                {
                    ladder = candidate;
                    return true;
                }
            }

            ladder = default(CampVerticalLadderDefinition);
            return false;
        }

        private CampVerticalTraversalStep ActiveStep(PrototypeCampUse campUse, bool started, bool completed)
        {
            return new CampVerticalTraversalStep(
                State,
                true,
                started,
                completed,
                activeLadder.StableLadderId,
                campUse.CurrentRoomId,
                campUse.PlayerPosition);
        }

        private CampVerticalTraversalStep IdleStep(PrototypeCampUse campUse)
        {
            return new CampVerticalTraversalStep(
                CampVerticalTraversalState.Idle,
                false,
                false,
                false,
                string.Empty,
                campUse == null ? string.Empty : campUse.CurrentRoomId,
                campUse == null ? Vector2.zero : campUse.PlayerPosition);
        }

        public static bool RunContractProbe(out string detail)
        {
            var expansion = new PrototypeCampModuleExpansion(PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance());
            var session = new GameSession();
            session.Grant(ResourceKind.Salvage, 2);
            session.Grant(ResourceKind.Wood, 10);
            if (!session.TryBuild(StructureKind.Workbench))
            {
                detail = "Could not create workbench prerequisite.";
                return false;
            }
            CampModuleReturnSnapshot snapshot = new CampModuleReturnSnapshot(
                new Vector2(PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).StartConnectorDisplayX, PrototypeCampUse.PlayerFloorY),
                1f,
                PrototypeCampModuleCatalog.StartRoomId);
            if (!expansion.BeginPreview(snapshot, CampModuleArchetype.Upper) ||
                expansion.TryCommit(session, new CampModuleValidationContext()) != CampModuleCommitStatus.Succeeded ||
                !expansion.BeginPreview(snapshot, CampModuleArchetype.Basement) ||
                expansion.TryCommit(session, new CampModuleValidationContext()) != CampModuleCommitStatus.Succeeded)
            {
                detail = "Could not create upper and basement fixtures.";
                return false;
            }

            var campUse = new PrototypeCampUse();
            var traversal = new PrototypeCampVerticalTraversal();
            CampVerticalLadderDefinition upper = PrototypeCampVerticalLayout.Ladder(CampModuleArchetype.Upper);
            campUse.Warp(upper.X);
            CampVerticalTraversalStep began = traversal.Step(campUse, expansion, 1f, 0.1f);
            float xAfterStart = campUse.PlayerPosition.x;
            CampVerticalTraversalStep paused = traversal.Step(campUse, expansion, 0f, 0.5f);
            bool heldMovement = began.Started && began.State == CampVerticalTraversalState.Climbing &&
                                paused.State == CampVerticalTraversalState.Climbing &&
                                Mathf.Approximately(paused.PlayerPosition.y, began.PlayerPosition.y);
            int guard = 40;
            CampVerticalTraversalStep upperDone = paused;
            while (!upperDone.Completed && guard-- > 0)
            {
                upperDone = traversal.Step(campUse, expansion, 1f, 0.1f);
            }
            bool upperArrival = upperDone.Completed &&
                                string.Equals(campUse.CurrentRoomId, upper.UpperRoomId, StringComparison.Ordinal) &&
                                Mathf.Approximately(campUse.PlayerPosition.x, xAfterStart) &&
                                Mathf.Approximately(campUse.PlayerPosition.y, upper.UpperFloorY);

            guard = 40;
            CampVerticalTraversalStep startDone = default(CampVerticalTraversalStep);
            while (!startDone.Completed && guard-- > 0)
            {
                startDone = traversal.Step(campUse, expansion, -1f, 0.1f);
            }
            CampVerticalLadderDefinition basement = PrototypeCampVerticalLayout.Ladder(CampModuleArchetype.Basement);
            campUse.Warp(basement.X);
            guard = 40;
            CampVerticalTraversalStep basementDone = default(CampVerticalTraversalStep);
            while (!basementDone.Completed && guard-- > 0)
            {
                basementDone = traversal.Step(campUse, expansion, -1f, 0.1f);
            }
            bool basementArrival = basementDone.Completed &&
                                   string.Equals(campUse.CurrentRoomId, basement.LowerRoomId, StringComparison.Ordinal) &&
                                   Mathf.Approximately(campUse.PlayerPosition.y, basement.LowerFloorY);
            if (!heldMovement || !upperArrival || !startDone.Completed || !basementArrival)
            {
                detail = "Direct held-axis traversal, pause, endpoint room, or floor contract failed.";
                return false;
            }

            detail = "PASS direct upper/basement world climb, held input pause, exact StableRoomId/floor endpoints.";
            return true;
        }
    }

    public sealed class PrototypeCampVerticalCamera
    {
        private float velocityY;

        public bool IsInitialized { get; private set; }
        public float CurrentY { get; private set; }

        public void Reset(float y)
        {
            CurrentY = y;
            velocityY = 0f;
            IsInitialized = true;
        }

        public float Step(float targetY, float minimumY, float maximumY, float deltaTime)
        {
            if (!IsInitialized) Reset(targetY);
            float clampedTarget = Mathf.Clamp(targetY, minimumY, maximumY);
            if (deltaTime <= 0f)
            {
                CurrentY = Mathf.Clamp(CurrentY, minimumY, maximumY);
                return CurrentY;
            }
            CurrentY = Mathf.SmoothDamp(
                CurrentY,
                clampedTarget,
                ref velocityY,
                PrototypeCampVerticalLayout.CameraSmoothTime,
                PrototypeCampVerticalLayout.CameraMaximumSpeed,
                deltaTime);
            CurrentY = Mathf.Clamp(CurrentY, minimumY, maximumY);
            return CurrentY;
        }

        public static bool RunContractProbe(out string detail)
        {
            var camera = new PrototypeCampVerticalCamera();
            camera.Reset(PrototypeCampUse.PlayerFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY);
            float previous = camera.CurrentY;
            bool monotonic = true;
            bool bounded = true;
            for (int index = 0; index < 120; index += 1)
            {
                float current = camera.Step(
                    PrototypeCampVerticalLayout.UpperFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY,
                    PrototypeCampVerticalLayout.BasementFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY,
                    PrototypeCampVerticalLayout.UpperFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY,
                    1f / 60f);
                monotonic &= current >= previous - 0.0001f;
                bounded &= current >= PrototypeCampVerticalLayout.BasementFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY - 0.0001f &&
                           current <= PrototypeCampVerticalLayout.UpperFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY + 0.0001f;
                previous = current;
            }
            float top = camera.CurrentY;
            float clamped = camera.Step(
                100f,
                PrototypeCampUse.PlayerFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY,
                PrototypeCampVerticalLayout.UpperFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY,
                1f);
            if (!monotonic || !bounded || top <= PrototypeCampVerticalLayout.CameraFramingOffsetY + PrototypeCampUse.PlayerFloorY ||
                clamped > PrototypeCampVerticalLayout.UpperFloorY + PrototypeCampVerticalLayout.CameraFramingOffsetY + 0.0001f)
            {
                detail = "Camera smoothing, monotonicity, or built-shelter clamp failed.";
                return false;
            }
            detail = "PASS smooth monotonic world-camera follow and built-shelter clamp.";
            return true;
        }
    }
}
