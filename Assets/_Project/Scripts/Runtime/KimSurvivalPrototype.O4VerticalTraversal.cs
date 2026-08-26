using System;
using UnityEngine;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private readonly PrototypeCampVerticalTraversal campVerticalTraversal = new PrototypeCampVerticalTraversal();
        private readonly PrototypeCampVerticalCamera campVerticalCamera = new PrototypeCampVerticalCamera();

        private bool ProcessO4CampVerticalTraversal(PrototypePlayerActions actions, float deltaTime)
        {
            CampVerticalTraversalStep step = campVerticalTraversal.Step(
                campUse,
                campModuleExpansion,
                actions.Vertical,
                deltaTime);
            if (!step.ConsumedMovement)
            {
                return false;
            }

            // A ladder is continuous world traversal. No camp interaction, popup, or
            // horizontal locomotion is allowed until an exact floor endpoint is reached.
            campInteractionTargets.Clear();
            campInteraction.UpdateSelection(campUse.PlayerPosition, campUse.FacingDirection, campInteractionTargets);
            ApplyO4CampPlayerPresentation(true);
            RefreshCampInteractionUi();
            return true;
        }

        private void ApplyO4CampPlayerPresentation(bool climbing)
        {
            if (playerRoot == null)
            {
                return;
            }

            playerPresentation.Apply(new PrototypePlayerPresentationState(
                campUse.PlayerPosition.x,
                campUse.PlayerPosition.y,
                campUse.FacingDirection,
                climbing ? 1f : 0f,
                false,
                true));
        }

        private void UpdateO4CampWorldCamera(float deltaTime)
        {
            if (worldCamera == null || session == null || session.Phase != GamePhase.Camp)
            {
                return;
            }

            PrototypeCampVerticalLayout.BuiltCameraRange(campModuleExpansion, out float minimumY, out float maximumY);
            float y = campVerticalCamera.Step(
                campUse.PlayerPosition.y + PrototypeCampVerticalLayout.CameraFramingOffsetY,
                minimumY,
                maximumY,
                deltaTime);
            Vector3 position = worldCamera.transform.position;
            position.y = y;
            worldCamera.transform.position = position;
        }

        private void PrepareO4CampWorldCamera(bool hasVerticalExpansion)
        {
            worldCamera.orthographicSize = hasVerticalExpansion
                ? PrototypeO6WorldPresentationConfig.ExpandedCampOrthographicSize
                : PrototypeO6WorldPresentationConfig.CampOrthographicSize;
            if (!campVerticalCamera.IsInitialized)
            {
                campVerticalCamera.Reset(campUse.PlayerPosition.y + PrototypeCampVerticalLayout.CameraFramingOffsetY);
            }
            UpdateO4CampWorldCamera(0f);
            Vector3 position = worldCamera.transform.position;
            position.x = 0f;
            position.z = -10f;
            worldCamera.transform.position = position;
        }

        private void ResetO4CampVerticalSystems()
        {
            campVerticalTraversal.Reset();
            campVerticalCamera.Reset(campUse.PlayerPosition.y + PrototypeCampVerticalLayout.CameraFramingOffsetY);
        }

        private void CreateO4CampLadders()
        {
            CreateO4CampLadder(CampModuleArchetype.Upper);
            CreateO4CampLadder(CampModuleArchetype.Basement);
        }

        private void CreateO4CampLadder(CampModuleArchetype archetype)
        {
            if (!campModuleExpansion.IsCommitted(archetype))
            {
                return;
            }

            CampVerticalLadderDefinition ladder = PrototypeCampVerticalLayout.Ladder(archetype);
            GameObject root = new GameObject("고정 사다리 · " + ladder.StableLadderId);
            root.transform.SetParent(worldRoot, false);
            float centerY = (ladder.LowerFloorY + ladder.UpperFloorY) * 0.5f;
            float height = ladder.UpperFloorY - ladder.LowerFloorY;
            root.transform.position = new Vector3(ladder.X, centerY, 0f);
            Color timber = new Color(0.30f, 0.18f, 0.08f, 1f);
            CreateRect(root.transform, "사다리 왼쪽 난간", new Vector2(-0.28f, 0f), new Vector2(0.12f, height + 0.38f), timber, 5);
            CreateRect(root.transform, "사다리 오른쪽 난간", new Vector2(0.28f, 0f), new Vector2(0.12f, height + 0.38f), timber, 5);
            int rungCount = Mathf.Max(3, Mathf.CeilToInt(height / 0.42f));
            for (int index = 0; index <= rungCount; index += 1)
            {
                float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, index / (float)rungCount);
                CreateRect(root.transform, "사다리 발판 " + index, new Vector2(0f, y), new Vector2(0.66f, 0.09f), timber, 6);
            }
        }

        private Vector2 O4WorldRoomPosition(string stableRoomId, Vector2 roomLocalPosition)
        {
            return PrototypeCampVerticalLayout.ToWorldPosition(stableRoomId, roomLocalPosition);
        }

        private bool IsO4VerticalConnector(CampModuleArchetype archetype)
        {
            return archetype == CampModuleArchetype.Upper || archetype == CampModuleArchetype.Basement;
        }

        private bool CompleteO4VerticalTraversalForVerification(CampModuleArchetype archetype, float verticalInput)
        {
            CampVerticalLadderDefinition ladder = PrototypeCampVerticalLayout.Ladder(archetype);
            campVerticalTraversal.Reset();
            campUse.Warp(ladder.X);
            int guard = 80;
            CampVerticalTraversalStep step = default(CampVerticalTraversalStep);
            while (!step.Completed && guard-- > 0)
            {
                step = campVerticalTraversal.Step(campUse, campModuleExpansion, verticalInput, 0.1f);
                if (!step.ConsumedMovement)
                {
                    return false;
                }
            }
            return step.Completed;
        }

        public static bool RunO4CampVerticalTraversalContracts(out string detail)
        {
            if (!PrototypeCampVerticalTraversal.RunContractProbe(out string traversalDetail))
            {
                detail = traversalDetail;
                return false;
            }
            if (!PrototypeCampVerticalCamera.RunContractProbe(out string cameraDetail))
            {
                detail = cameraDetail;
                return false;
            }

            var keyboardUp = PrototypePlayerActions.FromRaw(new PrototypeRawInput { KeyboardUp = true });
            var keyboardDown = PrototypePlayerActions.FromRaw(new PrototypeRawInput { KeyboardDown = true });
            var gamepadUp = PrototypePlayerActions.FromRaw(new PrototypeRawInput { VerticalAxis = 0.8f });
            if (keyboardUp.Vertical < 0.99f || keyboardDown.Vertical > -0.99f || gamepadUp.Vertical < 0.79f)
            {
                detail = "Keyboard/gamepad vertical action mapping failed.";
                return false;
            }
            if (!PrototypeCampInteraction.RunDistanceFirstSelectionProbe())
            {
                detail = "Installed-facility interaction priority regression probe failed.";
                return false;
            }

            detail = traversalDetail + " " + cameraDetail +
                     " PASS keyboard/gamepad common vertical action and nearby facility priority.";
            return true;
        }

        public bool CaptureO4CampVerticalTraversalObservation(string absolutePngPath, out string detail)
        {
            if (string.IsNullOrWhiteSpace(absolutePngPath))
            {
                detail = "Screenshot path is empty.";
                return false;
            }

            session.Reset(PrototypeExpeditionRegionCatalog.CreateRuntimeSeed());
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetO4CampVerticalSystems();
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Salvage, 6);
            if (!session.TryBuild(StructureKind.Workbench))
            {
                detail = "Could not build the workbench prerequisite.";
                return false;
            }

            CampModuleReturnSnapshot snapshot = new CampModuleReturnSnapshot(
                campUse.PlayerPosition,
                campUse.FacingDirection,
                PrototypeCampModuleCatalog.StartRoomId);
            CampModuleArchetype[] fixtures = { CampModuleArchetype.Upper, CampModuleArchetype.Basement };
            for (int index = 0; index < fixtures.Length; index += 1)
            {
                if (!campModuleExpansion.BeginPreview(snapshot, fixtures[index]) ||
                    campModuleExpansion.TryCommit(session, campModuleValidation) != CampModuleCommitStatus.Succeeded)
                {
                    detail = "Could not commit vertical room fixture: " + fixtures[index];
                    return false;
                }
            }

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            CampVerticalLadderDefinition upper = PrototypeCampVerticalLayout.Ladder(CampModuleArchetype.Upper);
            campUse.Warp(upper.X);
            CampVerticalTraversalStep climb = default(CampVerticalTraversalStep);
            for (int index = 0; index < 8; index += 1)
            {
                climb = campVerticalTraversal.Step(campUse, campModuleExpansion, 1f, 0.1f);
            }
            if (!campVerticalTraversal.IsClimbing || climb.Completed ||
                !string.Equals(campUse.CurrentRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
            {
                detail = "Mid-ladder fixture did not retain in-progress StableRoomId semantics.";
                return false;
            }

            renderedPhase = (GamePhase)(-1);
            RefreshAll();
            UpdateO4CampWorldCamera(1f);
            controlsText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            CaptureVerificationPng(absolutePngPath, 1280, 800);

            bool noConnectorPrompt = campInteraction.ActiveTargetKind != PrototypeCampInteractionTargetKind.ModuleConnector &&
                                     !campInteraction.IsPopupOpen;
            bool cameraFollowed = Mathf.Abs(worldCamera.transform.position.y) > 0.05f;
            bool exactX = Mathf.Approximately(campUse.PlayerPosition.x, upper.X);
            bool controlsDiscoverable = controlsText.text.Contains("사다리") &&
                                        (controlsText.text.Contains("W/S") || controlsText.text.Contains("↑↓")) &&
                                        !controlsText.isTextOverflowing;
            bool allRoutesDiscoverable = escapeRouteWorldLabels.Count == 3 &&
                                         escapeRouteWorldLabels.ContainsKey("escape.smoke") &&
                                         escapeRouteWorldLabels.ContainsKey("escape.radio") &&
                                         escapeRouteWorldLabels.ContainsKey(PrototypeRaftEscapeConfig.EscapeId);
            if (!noConnectorPrompt || !cameraFollowed || !exactX || !controlsDiscoverable || !allRoutesDiscoverable)
            {
                detail = "Ladder prompt suppression, camera follow, horizontal lock, controls hint, or three-route discovery observation failed.";
                return false;
            }

            detail = "PASS KO 1280x800 mid-climb capture; direct ladder, locked X, no popup, world-camera follow, readable W/S hint, three escape labels.";
            return true;
        }
    }
}
