using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private const string O11SearchNodeJobId = "job_20260825150605_49020784";
        private const string O11SearchNodeCandidateId = "object.searchable-resource-node-kit.state-language-a";
        private const int O11SearchNodeCellPixels = 384;
        private const float O11SearchNodePixelsPerUnit = 100f;
        private const float O11SearchNodeGroundPivotY = 0.0625f;
        private readonly Dictionary<string, Texture2D> o11SearchNodeTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> o11SearchNodeSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<EntityId, O11SearchNodeBinding> o11SearchNodeBindings =
            new Dictionary<EntityId, O11SearchNodeBinding>();

        private sealed class O11SearchNodeBinding
        {
            public SpriteRenderer Renderer;
            public PrototypeSearchNodeKind Kind;
            public PrototypeSearchNodeState State;
        }

        private void EnsureO11SearchNodeRuntimePresentation()
        {
            if (worldRoot == null || searchNodeRuntime == null || session.Phase != GamePhase.Exploring)
            {
                return;
            }

            EntityId[] staleIds = o11SearchNodeBindings
                .Where(pair => pair.Value == null || pair.Value.Renderer == null)
                .Select(pair => pair.Key)
                .ToArray();
            for (int index = 0; index < staleIds.Length; index += 1)
            {
                o11SearchNodeBindings.Remove(staleIds[index]);
            }

            for (int index = 0; index < nodes.Count; index += 1)
            {
                NodeView node = nodes[index];
                if (node == null || node.Root == null || node.Definition == null)
                {
                    continue;
                }

                PrototypeSearchNodeSnapshot snapshot = searchNodeRuntime.Ledger.GetOrCreate(node.Definition);
                Sprite sprite = GetO11SearchNodeStateSprite(node.Definition.Kind, snapshot.State);
                if (sprite == null)
                {
                    continue;
                }

                EntityId instanceId = node.Root.GetEntityId();
                if (!o11SearchNodeBindings.TryGetValue(instanceId, out O11SearchNodeBinding binding) ||
                    binding == null || binding.Renderer == null)
                {
                    var spriteObject = new GameObject(
                        "O11 Review Search Node Sprite · " + O11SearchNodeResourceKey(node.Definition.Kind));
                    spriteObject.transform.SetParent(node.Root.transform, false);
                    SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 4;
                    binding = new O11SearchNodeBinding { Renderer = renderer };
                    o11SearchNodeBindings[instanceId] = binding;
                }

                binding.Renderer.enabled = true;
                binding.Renderer.sprite = sprite;
                binding.Renderer.color = Color.white;
                binding.Renderer.transform.localPosition = Vector3.zero;
                binding.Renderer.transform.localRotation = Quaternion.identity;
                float targetHeight = node.Definition.RequiresSwimming ? 1.50f : 1.80f;
                float scale = targetHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
                binding.Renderer.transform.localScale = new Vector3(scale, scale, 1f);
                binding.Kind = node.Definition.Kind;
                binding.State = snapshot.State;

                DisableLegacyO11SearchNodeArt(node, binding.Renderer);
            }
        }

        private Sprite GetO11SearchNodeStateSprite(
            PrototypeSearchNodeKind kind,
            PrototypeSearchNodeState state)
        {
            string resourceKey = O11SearchNodeResourceKey(kind);
            string cacheKey = resourceKey + ":" + state;
            if (o11SearchNodeSprites.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            if (!o11SearchNodeTextures.TryGetValue(resourceKey, out Texture2D texture) || texture == null)
            {
                texture = LoadO11Texture(
                    "O11/SearchNodes/search-node-" + resourceKey + "-states");
                if (texture == null)
                {
                    return null;
                }
                o11SearchNodeTextures[resourceKey] = texture;
            }

            int stateIndex = O11SearchNodeStateIndex(state);
            var rect = new Rect(
                stateIndex * O11SearchNodeCellPixels,
                0f,
                O11SearchNodeCellPixels,
                O11SearchNodeCellPixels);
            Sprite sprite = Sprite.Create(
                texture,
                rect,
                new Vector2(0.5f, O11SearchNodeGroundPivotY),
                O11SearchNodePixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = O11SearchNodeCandidateId + " · " + resourceKey + " · " + state;
            o11SearchNodeSprites[cacheKey] = sprite;
            return sprite;
        }

        private static int O11SearchNodeStateIndex(PrototypeSearchNodeState state)
        {
            switch (state)
            {
                case PrototypeSearchNodeState.Hidden:
                    return 0;
                case PrototypeSearchNodeState.RevealedPartial:
                    return 1;
                case PrototypeSearchNodeState.Depleted:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported search-node state.");
            }
        }

        private static string O11SearchNodeResourceKey(PrototypeSearchNodeKind kind)
        {
            switch (kind)
            {
                case PrototypeSearchNodeKind.GrassPatch:
                    return "grass-thicket";
                case PrototypeSearchNodeKind.RockCrevice:
                    return "rock-crevice";
                case PrototypeSearchNodeKind.DriftPile:
                    return "drift-pile";
                case PrototypeSearchNodeKind.TreeHollow:
                    return "tree-hollow";
                case PrototypeSearchNodeKind.WreckLocker:
                    return "wreck-chest";
                case PrototypeSearchNodeKind.FacilityCabinet:
                    return "facility-cabinet";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported search-node kind.");
            }
        }

        private static void DisableLegacyO11SearchNodeArt(NodeView node, SpriteRenderer productionRenderer)
        {
            SpriteRenderer[] renderers = node.Root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index += 1)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null || renderer == productionRenderer ||
                    IsUnderNamedO11Root(renderer.transform, node.Root.transform, "상호작용 아이콘 · ") ||
                    (node.LabelRoot != null && renderer.transform.IsChildOf(node.LabelRoot)) ||
                    renderer.gameObject.name.StartsWith("채택 자원 문장 바탕", StringComparison.Ordinal) ||
                    renderer.gameObject.name.StartsWith("채택 자원 아이콘", StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsLegacyO11SearchNodeRenderer(renderer.gameObject.name))
                {
                    renderer.enabled = false;
                }
            }
        }

        private static bool IsUnderNamedO11Root(Transform value, Transform stop, string prefix)
        {
            for (Transform current = value; current != null && current != stop; current = current.parent)
            {
                if (current.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsLegacyO11SearchNodeRenderer(string objectName)
        {
            string[] prefixes =
            {
                "수색 풀잎",
                "수색 암반",
                "수색 바위틈",
                "수색 표류목",
                "수색 고목",
                "수색 난파함",
                "수색 폐시설",
                "수색 캐비닛",
                "수색 물결"
            };
            return prefixes.Any(prefix => objectName.StartsWith(prefix, StringComparison.Ordinal));
        }

        public bool RunO11SearchNodeVisualContract(out string detail)
        {
            EnsureO11SearchNodeRuntimePresentation();
            PrototypeSearchNodeKind[] kinds =
                (PrototypeSearchNodeKind[])Enum.GetValues(typeof(PrototypeSearchNodeKind));
            PrototypeSearchNodeState[] states =
                (PrototypeSearchNodeState[])Enum.GetValues(typeof(PrototypeSearchNodeState));

            bool sourceTextures = true;
            bool trueAlpha = true;
            bool allSlices = true;
            for (int kindIndex = 0; kindIndex < kinds.Length; kindIndex += 1)
            {
                string key = O11SearchNodeResourceKey(kinds[kindIndex]);
                Texture2D texture = LoadO11Texture("O11/SearchNodes/search-node-" + key + "-states");
                sourceTextures &= texture != null && texture.width == 1152 && texture.height == 384 && texture.isReadable;
                if (texture != null && texture.isReadable)
                {
                    Color32[] pixels = texture.GetPixels32();
                    trueAlpha &= pixels.Any(pixel => pixel.a == 0) && pixels.Any(pixel => pixel.a == 255);
                }
                else
                {
                    trueAlpha = false;
                }

                for (int stateIndex = 0; stateIndex < states.Length; stateIndex += 1)
                {
                    allSlices &= GetO11SearchNodeStateSprite(kinds[kindIndex], states[stateIndex]) != null;
                }
            }

            bool activeBindings = session.Phase != GamePhase.Exploring ||
                                  (nodes.Count > 0 && nodes.All(node =>
                                      node != null && node.Root != null &&
                                      o11SearchNodeBindings.TryGetValue(
                                          node.Root.GetEntityId(), out O11SearchNodeBinding binding) &&
                                      binding != null && binding.Renderer != null && binding.Renderer.enabled));
            bool legacyHidden = session.Phase != GamePhase.Exploring || nodes.All(node =>
                node.Root.GetComponentsInChildren<SpriteRenderer>(true).All(renderer =>
                    !IsLegacyO11SearchNodeRenderer(renderer.gameObject.name) || !renderer.enabled));
            bool markersPreserved = session.Phase != GamePhase.Exploring || nodes.All(node =>
                node.Root.GetComponentsInChildren<Transform>(true).Any(value =>
                    value.name.StartsWith("상호작용 아이콘 · ", StringComparison.Ordinal)));
            bool stateOrder = O11SearchNodeStateIndex(PrototypeSearchNodeState.Hidden) == 0 &&
                              O11SearchNodeStateIndex(PrototypeSearchNodeState.RevealedPartial) == 1 &&
                              O11SearchNodeStateIndex(PrototypeSearchNodeState.Depleted) == 2;
            bool passed = kinds.Length == 6 && sourceTextures && trueAlpha && allSlices &&
                          activeBindings && legacyHidden && markersPreserved && stateOrder;
            detail = O11SearchNodeJobId + "; candidate=" + O11SearchNodeCandidateId +
                     "; textures=" + sourceTextures + "; true-alpha=" + trueAlpha +
                     "; slices=18/18:" + allSlices + "; active-bindings=" + activeBindings +
                     "; legacy-hidden=" + legacyHidden + "; markers=" + markersPreserved +
                     "; state-order=" + stateOrder + "; adopted/formal=true";
            return passed;
        }

        public bool PrepareO11SearchNodeStateCapture(
            PrototypeSearchNodeKind kind,
            PrototypeSearchNodeState state,
            string localeCode,
            out string detail)
        {
            RestartSession();
            submissionShellState = SubmissionShellState.Playing;
            localization.SetLocale(localeCode, false);
            o7InitialGuideDismissed = true;
            RefreshO9O10Presentation();
            RefreshAll();

            PrototypeExpeditionRegionId region = O11SearchNodeCaptureRegion(kind);
            GameSessionStableState stableState = session.CaptureStableState();
            stableState.MaxUnlockedExpeditionOrdinal = (int)region;
            if (!session.RestoreStableState(stableState) || !session.BeginSearch(region))
            {
                detail = "failed to enter search-node capture region " + region;
                return false;
            }

            PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Get(region).Nodes
                .First(value => value.Kind == kind);
            PrototypeSearchNodeSnapshot snapshot = searchNodeRuntime.Ledger.GetOrCreate(definition);
            snapshot.State = state;
            if (state == PrototypeSearchNodeState.Depleted)
            {
                snapshot.Remaining = Array.Empty<PrototypeSearchLootEntry>();
            }
            RefreshAll();
            RefreshO11ProductionVisuals();

            NodeView node = nodes.FirstOrDefault(value =>
                string.Equals(value.Definition.NodeId, definition.NodeId, StringComparison.Ordinal));
            if (node == null)
            {
                detail = "capture node missing after world refresh";
                return false;
            }

            float y = definition.RequiresSwimming ? PrototypePlayerTraversal.WaterY : PrototypePlayerTraversal.LandY;
            playerPresentation.Apply(playerTraversal.Warp(node.X - 0.9f, y, definition.RequiresSwimming));
            Vector3 cameraPosition = worldCamera.transform.position;
            cameraPosition.x = PrototypeO7SearchBalance.CameraTargetX(node.X);
            worldCamera.transform.position = cameraPosition;
            UpdateResourceLabelLayout();
            EnsureO11SearchNodeRuntimePresentation();
            Canvas.ForceUpdateCanvases();
            detail = "kind=" + kind + "; state=" + state + "; locale=" + localeCode +
                     "; region=" + region + "; node=" + definition.NodeId +
                     "; candidate=" + O11SearchNodeCandidateId + "; adopted/formal=true";
            return true;
        }

        private static PrototypeExpeditionRegionId O11SearchNodeCaptureRegion(PrototypeSearchNodeKind kind)
        {
            switch (kind)
            {
                case PrototypeSearchNodeKind.GrassPatch:
                case PrototypeSearchNodeKind.TreeHollow:
                    return PrototypeExpeditionRegionId.Forest;
                case PrototypeSearchNodeKind.RockCrevice:
                    return PrototypeExpeditionRegionId.RidgeHighland;
                case PrototypeSearchNodeKind.DriftPile:
                    return PrototypeExpeditionRegionId.Beach;
                case PrototypeSearchNodeKind.WreckLocker:
                    return PrototypeExpeditionRegionId.CoveWreck;
                case PrototypeSearchNodeKind.FacilityCabinet:
                    return PrototypeExpeditionRegionId.RuinsRelay;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported search-node kind.");
            }
        }
    }
}
