using System.Linq;
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
        private const float SwimCycleRate = 4.5f;
        private const float ActionCycleRate = 5.5f;
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
        private Sprite[] swimFrames = System.Array.Empty<Sprite>();
        private Sprite[] climbFrames = System.Array.Empty<Sprite>();
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

        public string ActiveProductionState { get; private set; } = "idle";
        public int ActiveProductionFrame { get; private set; }

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

        public void ConfigurePolishedTraversalSprites(Sprite[] polishedSwimFrames, Sprite[] polishedClimbFrames)
        {
            swimFrames = polishedSwimFrames == null
                ? System.Array.Empty<Sprite>()
                : polishedSwimFrames.Where(sprite => sprite != null).ToArray();
            climbFrames = polishedClimbFrames == null
                ? System.Array.Empty<Sprite>()
                : polishedClimbFrames.Where(sprite => sprite != null).ToArray();
            if (swimFrames.Length > 0) swimSprite = swimFrames[0];
            if (climbFrames.Length > 0) climbSprite = climbFrames[0];
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
                float wakePulse = state.IsSwimming ? 1f + Mathf.Sin(animationTime * SwimCycleRate * Mathf.PI) * 0.08f : 1f;
                swimWakeRoot.localScale = new Vector3(state.Facing * wakePulse, 1f / wakePulse, 1f);
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
                float cycle = animationTime * SwimCycleRate;
                ActiveProductionState = "swim";
                ActiveProductionFrame = Mathf.FloorToInt(cycle) & 3;
                stateRenderer.sprite = FrameOrFallback(swimFrames, ActiveProductionFrame, swimSprite);
                float stroke = Mathf.Sin(cycle * Mathf.PI * 0.5f);
                float kick = Mathf.Cos(cycle * Mathf.PI);
                spriteTransform.localPosition = stateRendererBasePosition + new Vector3(stroke * 0.018f, -0.08f + kick * 0.022f, 0f);
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, stroke * 0.8f);
                spriteTransform.localScale = new Vector3(
                    stateRendererBaseScale.x * (1f + kick * 0.008f),
                    stateRendererBaseScale.y * (1f - kick * 0.006f),
                    stateRendererBaseScale.z);
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
                int frame = Mathf.FloorToInt(movementTime * WalkFrameRate) & 3;
                ActiveProductionState = "walk";
                ActiveProductionFrame = frame;
                stateRenderer.sprite = frame == 0 || frame == 3 ? walkSprite : idleSprite;
                float stride = Mathf.Sin(movementTime * WalkFrameRate * Mathf.PI);
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, stride * WalkLeanDegrees);
                spriteTransform.localScale = new Vector3(
                    stateRendererBaseScale.x * (1f + Mathf.Abs(stride) * 0.012f),
                    stateRendererBaseScale.y * (1f - Mathf.Abs(stride) * 0.008f),
                    stateRendererBaseScale.z);
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
            ActiveProductionState = "idle";
            ActiveProductionFrame = Mathf.FloorToInt(animationTime * IdleBreathSpeed) & 3;
            spriteTransform.localScale = new Vector3(
                stateRendererBaseScale.x * (1f - breath * IdleBreathAmount * 0.45f),
                stateRendererBaseScale.y * (1f + breath * IdleBreathAmount),
                stateRendererBaseScale.z);
        }

        private void ApplyActionSprite(PrototypePlayerActionPose pose, Transform spriteTransform, float animationTime)
        {
            float cycle = animationTime * ActionCycleRate;
            ActiveProductionFrame = Mathf.FloorToInt(cycle) & 3;
            if (pose == PrototypePlayerActionPose.Search)
            {
                ActiveProductionState = "search";
                stateRenderer.sprite = searchSprite;
                float reach = Mathf.Sin(cycle * Mathf.PI * 0.5f);
                spriteTransform.localPosition = stateRendererBasePosition + new Vector3(reach * 0.025f, 0f, 0f);
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, reach * 3.2f);
            }
            else if (pose == PrototypePlayerActionPose.FacilityUse || pose == PrototypePlayerActionPose.Eat)
            {
                ActiveProductionState = pose == PrototypePlayerActionPose.Eat ? "rest-eat" : "facility-use";
                stateRenderer.sprite = facilityUseSprite;
                float work = Mathf.Abs(Mathf.Sin(cycle * Mathf.PI * 0.5f));
                spriteTransform.localPosition = stateRendererBasePosition + new Vector3(0f, -work * 0.035f, 0f);
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, work * 2.1f);
            }
            else if (pose == PrototypePlayerActionPose.Climb)
            {
                ActiveProductionState = "ladder";
                stateRenderer.sprite = FrameOrFallback(climbFrames, ActiveProductionFrame, climbSprite);
                float climb = Mathf.Sin(cycle * Mathf.PI * 0.5f);
                spriteTransform.localPosition = stateRendererBasePosition + new Vector3(climb * 0.010f, climb * 0.025f, 0f);
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, climb * 0.7f);
            }
            else if (pose == PrototypePlayerActionPose.Hurt)
            {
                ActiveProductionState = "hurt-sick";
                stateRenderer.sprite = hurtSprite;
                spriteTransform.localRotation = stateRendererBaseRotation * Quaternion.Euler(0f, 0f, -5f);
            }
            else
            {
                ActiveProductionState = "rest-eat";
                stateRenderer.sprite = restSprite;
                spriteTransform.localScale = new Vector3(stateRendererBaseScale.x, stateRendererBaseScale.y * 0.96f, stateRendererBaseScale.z);
            }
        }

        public bool RunO11AnimationContractProbe(out string detail)
        {
            Sprite[] required =
            {
                idleSprite,
                walkSprite,
                searchSprite,
                facilityUseSprite,
                climbSprite,
                swimSprite,
                hurtSprite,
                restSprite
            };
            if (stateRenderer == null || required.Any(sprite => sprite == null))
            {
                detail = "missing renderer or one of eight adopted atlas poses";
                return false;
            }

            Vector2 referencePivot = NormalizedPivot(required[0]);
            Sprite[] coreSprites = { idleSprite, walkSprite, searchSprite, facilityUseSprite, hurtSprite, restSprite };
            bool fixedCorePivot = coreSprites.All(sprite =>
                Vector2.Distance(referencePivot, NormalizedPivot(sprite)) <= 0.002f);
            bool fixedSwimPivot = HasConsistentPivot(swimFrames);
            bool fixedClimbPivot = HasConsistentPivot(climbFrames);
            bool bottomPivot = referencePivot.y <= 0.08f && Mathf.Abs(referencePivot.x - 0.5f) <= 0.01f;
            bool validScale = Mathf.Abs(baseScale.x) > 0.001f && Mathf.Abs(baseScale.y) > 0.001f;
            bool distinctAtlasCells = required.Select(sprite => sprite.name).Distinct().Count() == required.Length;

            float now = Time.unscaledTime;
            Vector3 originalPosition = transform.position;
            PrototypePlayerPresentationState idle = new PrototypePlayerPresentationState(
                originalPosition.x, originalPosition.y, 1f, 0f, false, true);
            PrototypePlayerPresentationState walk = new PrototypePlayerPresentationState(
                originalPosition.x, originalPosition.y, 1f, 1f, false, true);
            PrototypePlayerPresentationState swim = new PrototypePlayerPresentationState(
                originalPosition.x, originalPosition.y, 1f, 1f, true, false);

            actionPose = PrototypePlayerActionPose.None;
            conditionPose = PrototypePlayerActionPose.None;
            Apply(idle, now);
            Vector3 idleScaleA = stateRenderer.transform.localScale;
            int idleFrameA = ActiveProductionFrame;
            Apply(idle, now + 0.47f);
            bool idleMoves = PoseChanged(idleScaleA, stateRenderer.transform.localScale) || idleFrameA != ActiveProductionFrame;

            Apply(walk, now + 0.6f);
            Sprite walkSpriteA = stateRenderer.sprite;
            Quaternion walkRotationA = stateRenderer.transform.localRotation;
            int walkFrameA = ActiveProductionFrame;
            Apply(walk, now + 0.93f);
            bool walkMoves = walkSpriteA != stateRenderer.sprite || walkFrameA != ActiveProductionFrame ||
                             Quaternion.Angle(walkRotationA, stateRenderer.transform.localRotation) > 0.05f;

            Apply(swim, now + 1.0f);
            Vector3 swimPositionA = stateRenderer.transform.localPosition;
            Quaternion swimRotationA = stateRenderer.transform.localRotation;
            int swimFrameA = ActiveProductionFrame;
            Apply(swim, now + 1.31f);
            bool swimMoves = PoseChanged(swimPositionA, stateRenderer.transform.localPosition) ||
                             swimFrameA != ActiveProductionFrame ||
                             Quaternion.Angle(swimRotationA, stateRenderer.transform.localRotation) > 0.05f;

            PlayAction(PrototypePlayerActionPose.Search, 2f);
            Apply(idle, now + 0.05f);
            Vector3 searchPositionA = stateRenderer.transform.localPosition;
            Quaternion searchRotationA = stateRenderer.transform.localRotation;
            int searchFrameA = ActiveProductionFrame;
            Apply(idle, now + 0.36f);
            bool searchMoves = PoseChanged(searchPositionA, stateRenderer.transform.localPosition) ||
                               searchFrameA != ActiveProductionFrame ||
                               Quaternion.Angle(searchRotationA, stateRenderer.transform.localRotation) > 0.05f;

            PlayAction(PrototypePlayerActionPose.Climb, 2f);
            Apply(idle, now + 0.10f);
            Vector3 ladderPositionA = stateRenderer.transform.localPosition;
            Quaternion ladderRotationA = stateRenderer.transform.localRotation;
            int ladderFrameA = ActiveProductionFrame;
            Apply(idle, now + 0.41f);
            bool ladderMoves = PoseChanged(ladderPositionA, stateRenderer.transform.localPosition) ||
                               ladderFrameA != ActiveProductionFrame ||
                               Quaternion.Angle(ladderRotationA, stateRenderer.transform.localRotation) > 0.05f;

            actionPose = PrototypePlayerActionPose.None;
            conditionPose = PrototypePlayerActionPose.None;
            Apply(idle, now + 0.5f);

            bool polishedTraversalFrames = swimFrames.Length >= 4 && climbFrames.Length >= 4 &&
                                            swimFrames.Select(sprite => sprite.name).Distinct().Count() >= 4 &&
                                            climbFrames.Select(sprite => sprite.name).Distinct().Count() >= 4;
            bool productionCycles = idleMoves && walkMoves && searchMoves && ladderMoves && swimMoves;
            bool passed = fixedCorePivot && fixedSwimPivot && fixedClimbPivot && bottomPivot && validScale && distinctAtlasCells &&
                          polishedTraversalFrames && productionCycles;
            detail = "states=8; distinct-cells=" + distinctAtlasCells +
                     "; polished-traversal[swim/ladder]=" + swimFrames.Length + "/" + climbFrames.Length +
                     "; cycles[idle/walk/search/ladder/swim]=" +
                     idleMoves + "/" + walkMoves + "/" + searchMoves + "/" + ladderMoves + "/" + swimMoves +
                     "; pivot=" + referencePivot.ToString("F3") +
                     "; fixed[core/swim/ladder]=" + fixedCorePivot + "/" + fixedSwimPivot + "/" + fixedClimbPivot +
                     "; scale=" + validScale;
            return passed;
        }

        private static Sprite FrameOrFallback(Sprite[] frames, int frame, Sprite fallback)
        {
            return frames != null && frames.Length > 0 ? frames[Mathf.Abs(frame) % frames.Length] : fallback;
        }

        private static bool HasConsistentPivot(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0) return false;
            Vector2 pivot = NormalizedPivot(frames[0]);
            return frames.All(sprite => sprite != null &&
                Vector2.Distance(pivot, NormalizedPivot(sprite)) <= 0.002f);
        }

        private static bool PoseChanged(Vector3 first, Vector3 second)
        {
            return Vector3.Distance(first, second) > 0.0005f;
        }

        private static Vector2 NormalizedPivot(Sprite sprite)
        {
            Rect rect = sprite.rect;
            return new Vector2(sprite.pivot.x / rect.width, sprite.pivot.y / rect.height);
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
