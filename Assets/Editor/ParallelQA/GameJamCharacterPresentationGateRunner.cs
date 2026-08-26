using System;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace ParallelQA
{
    public static class GameJamCharacterPresentationGateRunner
    {
        [MenuItem("Kim Survival/QA/Game Jam Character Presentation Gate")]
        public static void RunFromMenu()
        {
            Run(false);
        }

        public static void RunFromCommandLine()
        {
            Run(true);
        }

        private static void Run(bool exitEditor)
        {
            try
            {
                Require(Mathf.Approximately(PrototypeCampUse.PlayerFloorY, PrototypeCampPlacement.FloorY),
                    "camp character baseline must equal the visible construction floor");
                VerifyCampGrounding();
                VerifyTraversalGrounding();
                VerifySpritePresentation();
                Debug.Log("[ParallelQA] PASS · game jam character grounding and animation gate");
                if (exitEditor) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (exitEditor) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void VerifyCampGrounding()
        {
            PrototypeCampUse camp = new PrototypeCampUse();
            Require(Mathf.Approximately(camp.PlayerPosition.y, PrototypeCampPlacement.FloorY),
                "camp reset leaves Kim above the floor");
            camp.Step(new PrototypePlayerActions(1f, false, false, false, false, -1), 0.25f);
            Require(Mathf.Approximately(camp.PlayerPosition.y, PrototypeCampPlacement.FloorY),
                "camp locomotion changes Kim's foot baseline");
            camp.Warp(new Vector2(0f, 25f));
            Require(Mathf.Approximately(camp.PlayerPosition.y, PrototypeCampPlacement.FloorY),
                "camp warp may restore the legacy floating height");
        }

        private static void VerifyTraversalGrounding()
        {
            PrototypePlayerTraversal traversal = new PrototypePlayerTraversal();
            GameSession session = new GameSession();
            traversal.Reset();
            PrototypeTraversalStep land = traversal.Step(default(PrototypePlayerActions), 0.02f, 0f, session);
            Require(land.Presentation.IsGrounded && Mathf.Approximately(land.Presentation.Y, PrototypePlayerTraversal.LandY),
                "land traversal is not grounded at its authored baseline");

            PrototypePlayerPresentationState swimming = traversal.Warp(
                PrototypePlayerTraversal.CoastlineX - 1f,
                PrototypePlayerTraversal.WaterY,
                true);
            Require(swimming.IsSwimming && swimming.IsGrounded,
                "swimming traversal changed its established jump-suppression state");
        }

        private static void VerifySpritePresentation()
        {
            Texture2D texture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
            Sprite idle = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0f), 4f);
            Sprite walk = Sprite.Create(texture, new Rect(4f, 0f, 4f, 4f), new Vector2(0.5f, 0f), 4f);
            GameObject root = new GameObject("character-presentation-gate-root");
            GameObject visual = new GameObject("visual");
            GameObject spriteObject = new GameObject("sprite");
            try
            {
                visual.transform.SetParent(root.transform, false);
                spriteObject.transform.SetParent(visual.transform, false);
                SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = idle;

                PrototypePlayerPresentation presentation = root.AddComponent<PrototypePlayerPresentation>();
                presentation.Configure(visual.transform, false);
                presentation.ConfigureSpriteStates(renderer, idle, walk, idle);

                PrototypePlayerPresentationState moving = new PrototypePlayerPresentationState(2f, -3f, -1f, 1f, false, true);
                presentation.Apply(moving, 0f);
                Sprite firstFrame = renderer.sprite;
                presentation.Apply(moving, 0.2f);
                Require(renderer.sprite != firstFrame, "walk state does not cycle between authored key poses");
                Require(Mathf.Approximately(root.transform.position.y, -3f), "walk animation moves the foot root off the floor");
                Require(visual.transform.localScale.x < 0f, "left-facing movement does not flip the visual root");

                presentation.Apply(new PrototypePlayerPresentationState(2f, -3f, 1f, 0f, false, true), 0f);
                Vector3 idleScaleA = renderer.transform.localScale;
                presentation.Apply(new PrototypePlayerPresentationState(2f, -3f, 1f, 0f, false, true), 0.5f);
                Require(renderer.transform.localScale != idleScaleA, "idle state has no breathing animation");
                Require(Mathf.Approximately(root.transform.position.y, -3f), "idle animation moves the foot root off the floor");

                CapsuleCollider2D body = root.GetComponent<CapsuleCollider2D>();
                Require(body != null && body.isTrigger, "character body trigger is missing");
                Require(Mathf.Approximately(body.offset.y - body.size.y * 0.5f, 0f),
                    "character collider bottom does not meet the foot anchor");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(idle);
                UnityEngine.Object.DestroyImmediate(walk);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
