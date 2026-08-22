using UnityEngine;

namespace KimSurvival
{
    public sealed class PrototypePlayerPresentation : MonoBehaviour
    {
        private static readonly int MoveSpeedParameter = Animator.StringToHash("MoveSpeed");
        private static readonly int SwimmingParameter = Animator.StringToHash("IsSwimming");
        private static readonly int GroundedParameter = Animator.StringToHash("IsGrounded");

        private Transform visualRoot;
        private Transform swimWakeRoot;
        private Animator animator;
        private Vector3 baseScale = Vector3.one;
        private Quaternion baseRotation = Quaternion.identity;
        private bool usePlaceholderPose;
        private bool hasMoveSpeedParameter;
        private bool hasSwimmingParameter;
        private bool hasGroundedParameter;

        public void Configure(Transform visual, bool placeholderPose)
        {
            visualRoot = visual;
            usePlaceholderPose = placeholderPose;
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            baseRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
            animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>() : null;
            CacheAnimatorParameters();
        }

        public void SetSwimWake(Transform wakeRoot)
        {
            swimWakeRoot = wakeRoot;
        }

        public void Apply(PrototypePlayerPresentationState state)
        {
            transform.position = new Vector3(state.X, state.Y, 0f);
            if (visualRoot != null)
            {
                float horizontalScale = Mathf.Abs(baseScale.x) * state.Facing;
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

            if (swimWakeRoot != null)
            {
                swimWakeRoot.gameObject.SetActive(state.IsSwimming);
                swimWakeRoot.position = new Vector3(state.X, PrototypePlayerTraversal.WaterY - 0.25f, 0f);
                swimWakeRoot.localScale = new Vector3(state.Facing, 1f, 1f);
            }
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
