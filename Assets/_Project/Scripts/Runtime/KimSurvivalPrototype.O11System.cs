namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private void RestoreO11PlayerMovementPresentation()
        {
            if (playerPresentation == null || campUse == null)
            {
                return;
            }

            // Module construction is an instantaneous reaction. Clear only its
            // transient action pose; health/hunger condition poses stay authoritative.
            playerPresentation.PlayAction(PrototypePlayerActionPose.None, 0.05f);
            playerPresentation.Apply(new PrototypePlayerPresentationState(
                campUse.PlayerPosition.x,
                campUse.PlayerPosition.y,
                campUse.FacingDirection,
                0f,
                false,
                true));
        }
    }
}
