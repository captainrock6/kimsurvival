using UnityEngine;

namespace KimSurvival
{
    public readonly struct PrototypePlayerPresentationState
    {
        public PrototypePlayerPresentationState(float x, float y, float facing, float moveAmount, bool swimming, bool grounded)
        {
            X = x;
            Y = y;
            Facing = facing;
            MoveAmount = moveAmount;
            IsSwimming = swimming;
            IsGrounded = grounded;
        }

        public float X { get; }
        public float Y { get; }
        public float Facing { get; }
        public float MoveAmount { get; }
        public bool IsSwimming { get; }
        public bool IsGrounded { get; }
    }

    public readonly struct PrototypeTraversalStep
    {
        public PrototypeTraversalStep(PrototypePlayerPresentationState presentation, bool reachedBlockedPath)
        {
            Presentation = presentation;
            ReachedBlockedPath = reachedBlockedPath;
        }

        public PrototypePlayerPresentationState Presentation { get; }
        public bool ReachedBlockedPath { get; }
    }

    public sealed class PrototypePlayerTraversal
    {
        public const float CoastlineX = -4.2f;
        public const float LandY = -2.15f;
        public const float WaterY = -1.88f;

        private const float LandMoveSpeed = 4.2f;
        private const float SwimMoveSpeed = 2.65f;
        private const float MinimumX = PrototypeO7SearchBalance.PlayerMinimumX;
        private const float LockedMaximumX = PrototypeO7SearchBalance.PlayerLockedMaximumX;
        private const float UnlockedMaximumX = PrototypeO7SearchBalance.PlayerUnlockedMaximumX;
        private const float BarrierNoticeX = 7.75f;
        private const float JumpSpeed = 6.5f;
        private const float Gravity = 18f;

        private float x;
        private float y;
        private float verticalVelocity;
        private float facing = 1f;
        private bool grounded = true;
        private bool barrierNoticeEmitted;

        public float X
        {
            get { return x; }
        }

        public float Y
        {
            get { return y; }
        }

        public void Reset(float startX = -3f, float startY = LandY)
        {
            x = startX;
            y = startY;
            verticalVelocity = 0f;
            facing = 1f;
            grounded = true;
            barrierNoticeEmitted = false;
        }

        public PrototypeTraversalStep Step(PrototypePlayerActions actions, float deltaTime, float elapsedTime, GameSession session)
        {
            float horizontal = actions.Horizontal;
            float moveSpeed = session.IsSwimming ? SwimMoveSpeed : LandMoveSpeed;
            x += horizontal * moveSpeed * deltaTime;
            x = Mathf.Clamp(x, MinimumX, session.HasAxe ? UnlockedMaximumX : LockedMaximumX);

            bool reachedBlockedPath = !session.HasAxe && x > BarrierNoticeX && !barrierNoticeEmitted;
            if (reachedBlockedPath)
            {
                barrierNoticeEmitted = true;
            }

            bool wasSwimming = session.IsSwimming;
            session.SetSwimming(x < CoastlineX);

            if (session.IsSwimming)
            {
                grounded = true;
                verticalVelocity = 0f;
                y = WaterY + Mathf.Sin(elapsedTime * 4.2f) * 0.08f;
            }
            else if (actions.JumpPressed && grounded)
            {
                grounded = false;
                verticalVelocity = JumpSpeed;
            }

            if (!session.IsSwimming && wasSwimming)
            {
                y = LandY;
                verticalVelocity = 0f;
                grounded = true;
            }

            if (!session.IsSwimming && !grounded)
            {
                verticalVelocity -= Gravity * deltaTime;
                y += verticalVelocity * deltaTime;
                if (y <= LandY)
                {
                    y = LandY;
                    verticalVelocity = 0f;
                    grounded = true;
                }
            }

            if (horizontal < -0.01f)
            {
                facing = -1f;
            }
            else if (horizontal > 0.01f)
            {
                facing = 1f;
            }

            return new PrototypeTraversalStep(CreatePresentation(horizontal, session.IsSwimming), reachedBlockedPath);
        }

        public PrototypePlayerPresentationState Warp(float targetX, float targetY, bool swimming)
        {
            x = targetX;
            y = targetY;
            verticalVelocity = 0f;
            grounded = true;
            return CreatePresentation(0f, swimming);
        }

        public PrototypePlayerPresentationState RestorePosition(float restoredX, float restoredY, bool swimming)
        {
            x = Mathf.Clamp(restoredX, MinimumX, UnlockedMaximumX);
            y = restoredY;
            verticalVelocity = 0f;
            grounded = true;
            return CreatePresentation(0f, swimming);
        }

        public PrototypePlayerPresentationState CurrentPresentation(bool swimming, float horizontal = 0f)
        {
            return CreatePresentation(horizontal, swimming);
        }

        private PrototypePlayerPresentationState CreatePresentation(float horizontal, bool swimming)
        {
            return new PrototypePlayerPresentationState(x, y, facing, Mathf.Abs(horizontal), swimming, grounded);
        }
    }
}
