using UnityEngine;

namespace KimSurvival
{
    public enum PrototypePlayerActionPose
    {
        None,
        Search,
        FacilityUse,
        Climb,
        Hurt,
        Rest,
        Eat
    }

    public sealed class PrototypePlayerPresentation : MonoBehaviour
    {
        private const float WalkFrameRate = 7f;
        private const float IdleBreathSpeed = 2.4f;
        private const float IdleBreathAmount = 0.018f;
        private const float WalkLeanDegrees = 1.35f;
        private const float BodyWidth = 0.72f;
        private const float BodyHeight = 2.15f;

        private static readonly int MoveSpeedParameter = Animator.StringToHash("MoveSpeed");
        private static readonly int SwimmingParameter = Animator.StringToHash("IsSwimming");
        private static readonly int GroundedParameter = Animator.StringToHash("IsGrounded");

        private Transform visualRoot;
        private Transform swimWakeRoot;
        private Animator animator;
        private SpriteRenderer stateRenderer;
        private Sprite idleSprite;
        private Sprite walkSprite;
        private Sprite swimSprite;
        private Sprite climbSprite;
        private Sprite searchSprite;
        private Sprite facilityUseSprite;
        private Sprite hurtSprite;
        private Sprite restSprite;
        private Vector3 baseScale = Vector3.one;
        private Quaternion baseRotation = Quaternion.identity;
        private Vector3 basePosition = Vector3.zero;
        private Vector3 stateRendererBaseScale = Vector3.one;
        private Vector3 stateRendererBasePosition = Vector3.zero;
        private Quaternion stateRendererBaseRotation = Quaternion.identity;
        private bool usePlaceholderPose;
        private bool hasMoveSpeedParameter;
        private bool hasSwimmingParameter;
        private bool hasGroundedParameter;
        private bool wasSpriteMoving;
        private float spriteMovementStartedAt;
        private PrototypePlayerActionPose actionPose;
        private PrototypePlayerActionPose conditionPose;
        private float actionUntil;

        public void Configure(Transform visual, bool placeholderPose)
        {
            visualRoot = visual;
            usePlaceholderPose = placeholderPose;
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            baseRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
            basePosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>() : null;
            CacheAnimatorParameters();
            ConfigureBodyCollider();
        }

        public void SetSwimWake(Transform wakeRoot)
        {
            swimWakeRoot = wakeRoot;
        }

        public void ConfigureSpriteStates(SpriteRenderer renderer, Sprite idle, Sprite walk, Sprite swim)
        {
            ConfigureSpriteStates(renderer, idle, walk, swim, idle, idle, idle, idle, idle);
        }

        public void ConfigureSpriteStates(
            SpriteRenderer renderer,
            Sprite idle,
            Sprite walk,
            Sprite swim,
            Sprite climb,
            Sprite search,
            Sprite facilityUse,
            Sprite hurt,
            Sprite rest)
        {
            stateRenderer = renderer;
            idleSprite = idle;
            walkSprite = walk != null ? walk : idle;
            swimSprite = swim != null ? swim : idle;
            climbSprite = climb != null ? climb : idle;
            searchSprite = search != null ? search : idle;
            facilityUseSprite = facilityUse != null ? facilityUse : idle;
            hurtSprite = hurt != null ? hurt : idle;
            restSprite = rest != null ? rest : idle;
            if (stateRenderer != null)
            {
                stateRendererBaseScale = stateRenderer.transform.localScale;
                stateRendererBasePosition = stateRenderer.transform.localPosition;
                stateRendererBaseRotation = stateRenderer.transform.localRotation;
            }
            ConfigureBodyCollider();
        }

        public void PlayAction(PrototypePlayerActionPose pose, float duration)
        {
            actionPose = pose;
            actionUntil = Time.unscaledTime + Mathf.Max(0.05f, duration);
            wasSpriteMoving = false;
        }

        public void SetConditionPose(PrototypePlayerActionPose pose)
        {
            conditionPose = pose;
        }

        public void Apply(PrototypePlayerPresentationState state)
        {
            Apply(state, Time.unscaledTime);
        }

        public void Apply(PrototypePlayerPresentationState state, float animationTime)
        {
            transform.position = new Vector3(state.X, state.Y, 0f);
            if (visualRoot != null)
            {
                float facing = state.Facing < 0f ? -1f : 1f;
                float horizontalScale = Mathf.Abs(baseScale.x) * facing;
                visualRoot.localPosition = basePosition;
                if (usePlaceholderPose)
                {
                    visualRoot.localScale = new Vector3(horizontalScale * (state.IsSwimming ? 1.25f : 1f), baseScale.y * (state.IsSwimming ? 0.72f : 1f), baseScale.z);
                    visualRoot.localRotation = state.IsSwimming ? Quaternion.Euler(0f, 0f, -68f) : baseRotation;
                }
                else
                {
                    visualRoot.localScale = new Vector3(horizontalScale, baseScale.y, baseScale.z);
                    visualRoot.localRotation = baseRotation;
                }
            }

            if (animator != null)
            {
                if (hasMoveSpeedParameter)
                {
                    animator.SetFloat(MoveSpeedParameter, state.MoveAmount);
                }

                if (hasSwimmingParameter)
                {
                    animator.SetBool(SwimmingParameter, state.IsSwimming);
                }

                if (hasGroundedParameter)
                {
                    animator.SetBool(GroundedParameter, state.IsGrounded);
                }
            }

            else if (stateRenderer != null)
            {
                ApplySpriteAnimation(state, animationTime);
            }

            if (swimWakeRoot != null)
            {
                swimWakeRoot.gameObject.SetActive(state.IsSwimming);
                swimWakeRoot.position = new Vector3(state.X, PrototypePlayerTraversal.WaterY - 0.25f, 0f);
                swimWakeRoot.localScale = new Vector3(state.Facing, 1f, 1f);
            }
        }

        private void ApplySpriteAnimation(PrototypePlayerPresentationState state, float animationTime)
        {
            bool moving = state.MoveAmount > 0.05f;
            Transform spriteTransform = stateRenderer.transform;
            spriteTransform.localPosition = stateRendererBasePosition;
            spriteTransform.localRotation = stateRendererBaseRotation;
            spriteTransform.localScale = stateRendererBaseScale;

            if (actionPose != PrototypePlayerActionPose.None && animationTime <= actionUntil)
            {
                ApplyActionSprite(actionPose, spriteTransform, animationTime);
                return;
            }
            actionPose = PrototypePlayerActionPose.None;

            if (state.IsSwimming)
            {
                stateRenderer.sprite = swimSprite;
                wasSpriteMoving = false;
                return;
            }

            if (moving)
            {
                if (!wasSpriteMoving)
                {
                    spriteMovementStartedAt = animationTime;
                }
                wasSpriteMoving = true;
                float movementTime = Mathf.Max(0f, animationTime - spriteMovementStartedAt);
                int frame = Mathf.FloorToInt(movementTime * WalkFrameRate) & 1;
                stateRenderer.sprite = frame == 0 ? walkSprite : idleSprite;
                float stride = Mathf.Sin(movementTime * WalkFrameRate * Mathf.PI);
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, stride * WalkLeanDegrees);
                return;
            }

            wasSpriteMoving = false;
            if (conditionPose != PrototypePlayerActionPose.None)
            {
                ApplyActionSprite(conditionPose, spriteTransform, animationTime);
                return;
            }
            stateRenderer.sprite = idleSprite;
            float breath = Mathf.Sin(animationTime * IdleBreathSpeed);
            spriteTransform.localScale = new Vector3(
                stateRendererBaseScale.x * (1f - breath * IdleBreathAmount * 0.45f),
                stateRendererBaseScale.y * (1f + breath * IdleBreathAmount),
                stateRendererBaseScale.z);
        }

        private void ApplyActionSprite(PrototypePlayerActionPose pose, Transform spriteTransform, float animationTime)
        {
            if (pose == PrototypePlayerActionPose.Search)
            {
                stateRenderer.sprite = searchSprite;
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(animationTime * 16f) * 2.4f);
            }
            else if (pose == PrototypePlayerActionPose.FacilityUse || pose == PrototypePlayerActionPose.Eat)
            {
                stateRenderer.sprite = facilityUseSprite;
                float work = Mathf.Abs(Mathf.Sin(animationTime * 13f));
                spriteTransform.localPosition = stateRendererBasePosition + new Vector3(0f, -work * 0.035f, 0f);
            }
            else if (pose == PrototypePlayerActionPose.Climb)
            {
                stateRenderer.sprite = climbSprite;
                float climb = Mathf.Sin(animationTime * 12f);
                spriteTransform.localPosition = stateRendererBasePosition + new Vector3(0f, climb * 0.045f, 0f);
            }
            else if (pose == PrototypePlayerActionPose.Hurt)
            {
                stateRenderer.sprite = hurtSprite;
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, -5f);
            }
            else
            {
                stateRenderer.sprite = restSprite;
                spriteTransform.localScale = new Vector3(stateRendererBaseScale.x, stateRendererBaseScale.y * 0.96f, stateRendererBaseScale.z);
            }
        }

        private void ConfigureBodyCollider()
        {
            CapsuleCollider2D body = GetComponent<CapsuleCollider2D>();
            if (body == null)
            {
                body = gameObject.AddComponent<CapsuleCollider2D>();
            }

            body.isTrigger = true;
            body.direction = CapsuleDirection2D.Vertical;
            body.size = new Vector2(BodyWidth, BodyHeight);
            body.offset = new Vector2(0f, BodyHeight * 0.5f);
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i += 1)
            {
                AnimatorControllerParameter parameter = parameters[i];
                hasMoveSpeedParameter |= parameter.nameHash == MoveSpeedParameter && parameter.type == AnimatorControllerParameterType.Float;
                hasSwimmingParameter |= parameter.nameHash == SwimmingParameter && parameter.type == AnimatorControllerParameterType.Bool;
                hasGroundedParameter |= parameter.nameHash == GroundedParameter && parameter.type == AnimatorControllerParameterType.Bool;
            }
        }
    }
}
