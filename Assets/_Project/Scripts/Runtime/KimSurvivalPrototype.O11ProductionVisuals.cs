using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private const float O11BagWidth = 520f;
        private const float O11BagHeight = 248f;
        private const float O11BagSlotSize = 88f;
        private const float O11BagSlotGap = 8f;
        private bool o11ProductionVisualsInitialized;
        private Texture2D o11KimAtlasTexture;
        private Sprite o11KimAtlasRuntimeSprite;
        private PrototypePlayerPresentation o11ConfiguredPlayerPresentation;
        private Transform o11RegionSpriteRoot;
        private PrototypeExpeditionRegionId? o11RegionSpriteId;
        private Sprite o11RegionBackgroundSprite;
        private Sprite o11RegionForegroundSprite;

        public bool O11ProductionVisualsReady
        {
            get { return o11ProductionVisualsInitialized && canvas != null && bagButtons.Count == GameSession.MaximumBagSlotCount; }
        }

        /// <summary>
        /// Called by the late-running O11 bootstrap. It intentionally runs after
        /// legacy layout policies so the adopted production grammar wins without
        /// changing the protected prototype controller.
        /// </summary>
        public void RefreshO11ProductionVisuals()
        {
            if (canvas == null || session == null || statusText == null || bagPanel == null)
            {
                return;
            }

            if (!o11ProductionVisualsInitialized)
            {
                InitializeO11ProductionVisuals();
            }

            EnsureO11KimRuntimePresentation();
            EnsureO11RegionRuntimePresentation();
            EnsureO11SearchNodeRuntimePresentation();
            ApplyO11HudLayout();
            ApplyO11CompactBagLayout();
            ApplyO11ContextPanelSkin();
        }

        private void InitializeO11ProductionVisuals()
        {
            o11ProductionVisualsInitialized = true;
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                PrototypeO11ProductionSkin.ApplyButton(button);
            }

            PrototypeO11ProductionSkin.ApplyPanel(campInteractionPopup, 0.97f);
            PrototypeO11ProductionSkin.ApplyPanel(searchLootTrayPanel, 0.97f);
            PrototypeO11ProductionSkin.ApplyPanel(expeditionMapPanel, 0.98f);
            PrototypeO11ProductionSkin.ApplyPanel(endingAlbumPanel, 0.98f);
            PrototypeO11ProductionSkin.ApplyPanel(resultPanel, 0.98f);
            PrototypeO11ProductionSkin.ApplyPanel(bagPanel, 0.94f);
        }

        private void ApplyO11HudLayout()
        {
            RectTransform top = statusText.transform.parent as RectTransform;
            PrototypeO11ProductionSkin.SetStretch(top, new Vector2(0.018f, 0.915f), new Vector2(0.982f, 0.982f));
            PrototypeO11ProductionSkin.ApplyPanel(top == null ? null : top.gameObject, 0.94f);
            VerticalLayoutGroup legacyTopLayout = top == null ? null : top.GetComponent<VerticalLayoutGroup>();
            if (legacyTopLayout != null)
            {
                legacyTopLayout.enabled = false;
            }

            ConfigureO11HudText(statusText, new Vector2(0.018f, 0.08f), new Vector2(0.56f, 0.92f), TextAlignmentOptions.MidlineLeft, 15f, 22f);
            ConfigureO11HudText(resourceText, new Vector2(0.57f, 0.08f), new Vector2(0.982f, 0.92f), TextAlignmentOptions.MidlineRight, 14f, 20f);

            RectTransform message = messageText.transform.parent as RectTransform;
            PrototypeO11ProductionSkin.SetStretch(message, new Vector2(0.37f, 0.825f), new Vector2(0.79f, 0.895f));
            if (messagePanelImage != null)
            {
                messagePanelImage.color = new Color(PrototypeO11ProductionSkin.Ink.r, PrototypeO11ProductionSkin.Ink.g, PrototypeO11ProductionSkin.Ink.b, 0.90f);
            }
            messageText.fontSizeMin = 14f;
            messageText.fontSizeMax = 20f;
            messageText.maxVisibleLines = 2;
            messageText.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform controls = controlsText.transform.parent as RectTransform;
            PrototypeO11ProductionSkin.SetStretch(controls, new Vector2(0.34f, 0.016f), new Vector2(0.982f, 0.076f));
            PrototypeO11ProductionSkin.ApplyPanel(controls == null ? null : controls.gameObject, 0.90f);
            controlsText.fontSizeMin = 12f;
            controlsText.fontSizeMax = 18f;
            controlsText.maxVisibleLines = 2;
            controlsText.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void ConfigureO11HudText(
            TMP_Text text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAlignmentOptions alignment,
            float fontMin,
            float fontMax)
        {
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontMin;
            text.fontSizeMax = fontMax;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.maxVisibleLines = 2;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void ApplyO11CompactBagLayout()
        {
            RectTransform bagRect = bagPanel.GetComponent<RectTransform>();
            bagRect.anchorMin = Vector2.zero;
            bagRect.anchorMax = Vector2.zero;
            bagRect.pivot = Vector2.zero;
            bagRect.anchoredPosition = new Vector2(24f, 88f);
            bagRect.sizeDelta = new Vector2(O11BagWidth, O11BagHeight);
            PrototypeO11ProductionSkin.ApplyPanel(bagPanel, 0.94f);

            RectTransform titleRect = bagTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);
            titleRect.sizeDelta = new Vector2(-32f, 42f);
            bagTitleText.fontSizeMin = 12f;
            bagTitleText.fontSizeMax = 18f;
            bagTitleText.maxVisibleLines = 1;
            bagTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            bagTitleText.overflowMode = TextOverflowModes.Ellipsis;
            bagTitleText.color = PrototypeO11ProductionSkin.Amber;

            bool pseudoLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;
            for (int index = 0; index < bagButtons.Count; index += 1)
            {
                int column = index % 5;
                int row = index / 5;
                RectTransform buttonRect = bagButtons[index].GetComponent<RectTransform>();
                buttonRect.anchorMin = Vector2.zero;
                buttonRect.anchorMax = Vector2.zero;
                buttonRect.pivot = Vector2.zero;
                buttonRect.anchoredPosition = new Vector2(
                    20f + column * (O11BagSlotSize + O11BagSlotGap),
                    14f + (1 - row) * (O11BagSlotSize + O11BagSlotGap));
                buttonRect.sizeDelta = new Vector2(O11BagSlotSize, O11BagSlotSize);
                PrototypeO11ProductionSkin.ApplyButton(bagButtons[index]);

                TMP_Text label = bagButtons[index].GetComponentInChildren<TMP_Text>(true);
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 0.40f);
                label.rectTransform.offsetMin = new Vector2(3f, 2f);
                label.rectTransform.offsetMax = new Vector2(-3f, -1f);
                label.alignment = TextAlignmentOptions.Center;
                label.fontSizeMin = pseudoLong ? 7f : 9f;
                label.fontSizeMax = pseudoLong ? 10f : 13f;
                label.maxVisibleLines = 2;
                label.textWrappingMode = TextWrappingModes.Normal;

                if (index < bagButtonIcons.Count && bagButtonIcons[index] != null)
                {
                    RectTransform iconRect = bagButtonIcons[index].rectTransform;
                    iconRect.anchorMin = new Vector2(0.5f, 0.68f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.68f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = Vector2.zero;
                    iconRect.sizeDelta = new Vector2(44f, 44f);
                }
            }
        }

        private void EnsureO11KimRuntimePresentation()
        {
            if (playerPresentation == null || playerRoot == null)
            {
                return;
            }

            if (o11ConfiguredPlayerPresentation == playerPresentation)
            {
                SpriteRenderer[] configuredRenderers = playerRoot.GetComponentsInChildren<SpriteRenderer>(true);
                if (configuredRenderers.Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy) == 1 &&
                    configuredRenderers.Any(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy &&
                        renderer.sprite != null && renderer.sprite.name.EndsWith("-adopted", StringComparison.Ordinal)))
                {
                    return;
                }
            }

            if (o11KimAtlasTexture == null)
            {
                o11KimAtlasTexture = LoadO11Texture("O11/mr-kim-core-atlas");
            }
            if (o11KimAtlasTexture == null)
            {
                return;
            }

            if (kimIdleSprite == null)
            {
                o11KimAtlasRuntimeSprite = Sprite.Create(
                    o11KimAtlasTexture,
                    new Rect(0f, 0f, o11KimAtlasTexture.width, o11KimAtlasTexture.height),
                    new Vector2(0.5f, 0.5f),
                    128f,
                    0u,
                    SpriteMeshType.FullRect);
                o11KimAtlasRuntimeSprite.name = "O11 adopted Mr. Kim atlas · " + PrototypeO11ProductionSkin.AdoptedStyleJobId;
                kimAtlasSprite = o11KimAtlasRuntimeSprite;
                PrepareKimSprites();
            }

            Transform visual = playerRoot.childCount > 0 ? playerRoot.GetChild(0) : playerRoot;
            SpriteRenderer runtimeRenderer = null;
            foreach (SpriteRenderer renderer in playerRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject.name == "O11 Adopted Mr. Kim Runtime Sprite")
                {
                    runtimeRenderer = renderer;
                }
                else if (runtimeRenderer == null && renderer.sprite != null &&
                         renderer.sprite.name.EndsWith("-adopted", StringComparison.Ordinal))
                {
                    runtimeRenderer = renderer;
                    renderer.gameObject.name = "O11 Adopted Mr. Kim Runtime Sprite";
                }
                else
                {
                    renderer.enabled = false;
                }
            }
            if (runtimeRenderer == null)
            {
                var spriteObject = new GameObject("O11 Adopted Mr. Kim Runtime Sprite");
                spriteObject.transform.SetParent(visual, false);
                runtimeRenderer = spriteObject.AddComponent<SpriteRenderer>();
            }

            runtimeRenderer.enabled = true;
            runtimeRenderer.sprite = kimIdleSprite;
            runtimeRenderer.sortingOrder = 8;
            float kimScale = 2.45f / Mathf.Max(0.01f, kimIdleSprite.bounds.size.y);
            runtimeRenderer.transform.localPosition = Vector3.zero;
            runtimeRenderer.transform.localRotation = Quaternion.identity;
            runtimeRenderer.transform.localScale = new Vector3(kimScale, kimScale, 1f);

            playerPresentation.Configure(visual, false);
            playerPresentation.ConfigureSpriteStates(
                runtimeRenderer,
                kimIdleSprite,
                kimWalkSprite,
                kimSwimSprite,
                kimClimbSprite,
                kimSearchSprite,
                kimFacilityUseSprite,
                kimHurtSprite,
                kimRestSprite);
            PrototypePlayerPresentationState current = session.Phase == GamePhase.Exploring
                ? playerTraversal.CurrentPresentation(session.IsSwimming)
                : new PrototypePlayerPresentationState(
                    playerRoot.position.x,
                    playerRoot.position.y,
                    campUse.FacingDirection,
                    0f,
                    false,
                    true);
            playerPresentation.Apply(current);
            o11ConfiguredPlayerPresentation = playerPresentation;
        }

        private void EnsureO11RegionRuntimePresentation()
        {
            if (worldRoot == null || session.Phase != GamePhase.Exploring || !session.SelectedRegionId.HasValue)
            {
                if (o11RegionSpriteRoot != null)
                {
                    o11RegionSpriteRoot.gameObject.SetActive(false);
                }
                return;
            }

            PrototypeExpeditionRegionId region = session.SelectedRegionId.Value;
            if (o11RegionSpriteRoot != null && o11RegionSpriteRoot.parent == worldRoot &&
                o11RegionSpriteId == region)
            {
                o11RegionSpriteRoot.gameObject.SetActive(true);
                DisableLegacyO11RegionGeometry();
                return;
            }

            string resourceKey = O11RegionResourceKey(region);
            Texture2D backgroundTexture = LoadO11Texture(
                "O11/Regions/o11-region-" + resourceKey + "-background");
            Texture2D foregroundTexture = LoadO11Texture(
                "O11/Regions/o11-region-" + resourceKey + "-foreground");
            if (backgroundTexture == null || foregroundTexture == null)
            {
                return;
            }

            if (o11RegionSpriteRoot != null)
            {
                Destroy(o11RegionSpriteRoot.gameObject);
            }
            if (o11RegionBackgroundSprite != null)
            {
                Destroy(o11RegionBackgroundSprite);
            }
            if (o11RegionForegroundSprite != null)
            {
                Destroy(o11RegionForegroundSprite);
            }

            var root = new GameObject("O11 Production Region Sprites · " + region);
            root.transform.SetParent(worldRoot, false);
            o11RegionSpriteRoot = root.transform;
            o11RegionSpriteId = region;
            o11RegionBackgroundSprite = CreateO11RegionSprite(backgroundTexture, resourceKey + "-background");
            o11RegionForegroundSprite = CreateO11RegionSprite(foregroundTexture, resourceKey + "-foreground");

            SpriteRenderer background = CreateO11RegionRenderer(
                o11RegionSpriteRoot,
                "O11 Region Background Sprite",
                o11RegionBackgroundSprite,
                -20);
            SpriteRenderer foreground = CreateO11RegionRenderer(
                o11RegionSpriteRoot,
                "O11 Region Foreground Sprite",
                o11RegionForegroundSprite,
                2);

            float targetHeight = PrototypeO6WorldPresentationConfig.ExplorationOrthographicSize * 2f + 0.5f;
            float scale = targetHeight / Mathf.Max(0.01f, o11RegionBackgroundSprite.bounds.size.y);
            Vector3 placement = new Vector3(8f, -PrototypeO6WorldPresentationConfig.ExplorationOrthographicSize, 0f);
            background.transform.localPosition = placement;
            foreground.transform.localPosition = placement;
            background.transform.localScale = new Vector3(scale, scale, 1f);
            foreground.transform.localScale = new Vector3(scale, scale, 1f);
            DisableLegacyO11RegionGeometry();
        }

        private void DisableLegacyO11RegionGeometry()
        {
            if (worldRoot == null)
            {
                return;
            }

            for (int index = 0; index < worldRoot.childCount; index += 1)
            {
                Transform child = worldRoot.GetChild(index);
                if (child != null && child != o11RegionSpriteRoot &&
                    child.name.StartsWith("Expedition Region Art · ", StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static Sprite CreateO11RegionSprite(Texture2D texture, string name)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = "O11 production " + name;
            return sprite;
        }

        private static Texture2D LoadO11Texture(string resourcesPath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture != null)
            {
                return texture;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcesPath);
            return sprite == null ? null : sprite.texture;
        }

        private static SpriteRenderer CreateO11RegionRenderer(
            Transform parent,
            string name,
            Sprite sprite,
            int sortingOrder)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static string O11RegionResourceKey(PrototypeExpeditionRegionId region)
        {
            switch (region)
            {
                case PrototypeExpeditionRegionId.Beach:
                    return "beach";
                case PrototypeExpeditionRegionId.Shallows:
                    return "shallows";
                case PrototypeExpeditionRegionId.Forest:
                    return "forest";
                case PrototypeExpeditionRegionId.RidgeHighland:
                    return "ridge-highland";
                case PrototypeExpeditionRegionId.CaveIsland:
                    return "island-cave";
                case PrototypeExpeditionRegionId.CoveWreck:
                    return "wreck-cove";
                case PrototypeExpeditionRegionId.RuinsRelay:
                    return "ruins-relay";
                default:
                    throw new ArgumentOutOfRangeException(nameof(region), region, "Unsupported O11 expedition region.");
            }
        }

        private void ApplyO11ContextPanelSkin()
        {
            PrototypeO11ProductionSkin.ApplyPanel(campInteractionPopup, 0.97f);
            PrototypeO11ProductionSkin.ApplyPanel(searchLootTrayPanel, 0.97f);
            PrototypeO11ProductionSkin.ApplyPanel(expeditionMapPanel, 0.98f);
            PrototypeO11ProductionSkin.ApplyPanel(endingAlbumPanel, 0.98f);
            PrototypeO11ProductionSkin.ApplyPanel(resultPanel, 0.98f);

            if (campInteractionPopup != null)
            {
                PrototypeO11ProductionSkin.SetStretch(
                    campInteractionPopup.GetComponent<RectTransform>(),
                    new Vector2(0.60f, 0.17f),
                    new Vector2(0.975f, 0.82f));
            }
            if (searchLootTrayPanel != null)
            {
                PrototypeO11ProductionSkin.SetStretch(
                    searchLootTrayPanel.GetComponent<RectTransform>(),
                    new Vector2(0.55f, 0.16f),
                    new Vector2(0.985f, 0.84f));
            }

            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                PrototypeO11ProductionSkin.ApplyButton(button);
            }
        }

        public bool RunO11ProductionVisualContract(out string detail)
        {
            RefreshO11ProductionVisuals();
            Canvas.ForceUpdateCanvases();

            var checks = new List<string>();
            RectTransform top = statusText == null ? null : statusText.transform.parent as RectTransform;
            bool thinHud = top != null && top.anchorMin.y >= 0.91f && top.anchorMax.y <= 0.985f;
            checks.Add("thin-hud=" + thinHud);

            RectTransform bagRect = bagPanel == null ? null : bagPanel.GetComponent<RectTransform>();
            bool compactBag = bagRect != null && bagRect.anchorMax == Vector2.zero &&
                              bagRect.sizeDelta.x <= 540f && bagRect.anchoredPosition.x <= 32f;
            bool squareSlots = bagButtons.Count == GameSession.MaximumBagSlotCount && bagButtons.All(button =>
            {
                Rect rect = button.GetComponent<RectTransform>().rect;
                return rect.width >= PrototypeO11ProductionSkin.MinimumFocusPixels && Mathf.Abs(rect.width - rect.height) <= 0.5f;
            });
            checks.Add("bottom-left-square-bag=" + compactBag + "/" + squareSlots);

            string animationDetail = "player presentation unavailable";
            bool animation = playerPresentation != null && playerPresentation.RunO11AnimationContractProbe(out animationDetail);
            checks.Add("animation=" + animation + "(" + animationDetail + ")");
            bool regionCatalog = PrototypeResourcePresentation.RunO11RegionContractProbe(out string regionDetail);
            bool regionSprites = RunO11RegionSpriteContract(out string spriteDetail);
            bool region = regionCatalog && regionSprites;
            checks.Add("regions=" + region + "(" + regionDetail + "; " + spriteDetail + ")");
            bool searchNodes = RunO11SearchNodeVisualContract(out string searchNodeDetail);
            checks.Add("search-nodes=" + searchNodes + "(" + searchNodeDetail + ")");

            bool passed = thinHud && compactBag && squareSlots && animation && region && searchNodes;
            detail = PrototypeO11ProductionSkin.AdoptedStyleJobId + "; " + string.Join("; ", checks.ToArray());
            return passed;
        }

        private bool RunO11RegionSpriteContract(out string detail)
        {
            string[] resourceKeys =
            {
                "beach",
                "shallows",
                "forest",
                "ridge-highland",
                "island-cave",
                "wreck-cove",
                "ruins-relay"
            };
            int loadedPairs = 0;
            bool productionSizes = true;
            for (int index = 0; index < resourceKeys.Length; index += 1)
            {
                Texture2D background = LoadO11Texture(
                    "O11/Regions/o11-region-" + resourceKeys[index] + "-background");
                Texture2D foreground = LoadO11Texture(
                    "O11/Regions/o11-region-" + resourceKeys[index] + "-foreground");
                if (background != null && foreground != null)
                {
                    loadedPairs += 1;
                    productionSizes &= background.width >= 1024 && background.height >= 180 &&
                                       foreground.width == background.width && foreground.height == background.height;
                }
            }

            SpriteRenderer[] playerRenderers = playerRoot == null
                ? Array.Empty<SpriteRenderer>()
                : playerRoot.GetComponentsInChildren<SpriteRenderer>(true);
            int enabledPlayerRenderers = playerRenderers.Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
            bool inkKimOnly = enabledPlayerRenderers == 1 && playerRenderers.Any(renderer =>
                renderer.enabled && renderer.gameObject.activeInHierarchy &&
                renderer.sprite != null && renderer.sprite.name.EndsWith("-adopted", StringComparison.Ordinal));

            bool activeRegionSprites = session.Phase != GamePhase.Exploring ||
                                       (o11RegionSpriteRoot != null && o11RegionSpriteRoot.gameObject.activeInHierarchy &&
                                        o11RegionSpriteRoot.GetComponentsInChildren<SpriteRenderer>(true).Length == 2);
            bool legacyGeometryHidden = worldRoot == null || worldRoot.GetComponentsInChildren<Transform>(true).All(value =>
                !value.name.StartsWith("Expedition Region Art · ", StringComparison.Ordinal) ||
                !value.gameObject.activeInHierarchy);

            bool passed = loadedPairs == resourceKeys.Length && productionSizes && inkKimOnly &&
                          activeRegionSprites && legacyGeometryHidden;
            detail = "sprite-pairs=" + loadedPairs + "/7; sizes=" + productionSizes +
                     "; ink-kim-only=" + inkKimOnly + "; active-bg-fg=" + activeRegionSprites +
                     "; legacy-geometry-hidden=" + legacyGeometryHidden;
            return passed;
        }

        public bool PrepareO11CaptureScenario(string scenario, string localeCode, out string detail)
        {
            RestartSession();
            submissionShellState = SubmissionShellState.Playing;
            localization.SetLocale(localeCode, false);
            o7InitialGuideDismissed = true;
            if (o7SurvivalHelpPanel != null)
            {
                o7SurvivalHelpPanel.SetActive(false);
            }
            RefreshO9O10Presentation();
            RefreshAll();

            bool prepared;
            switch ((scenario ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "camp":
                    prepared = session.Phase == GamePhase.Camp;
                    break;
                case "search":
                    prepared = session.BeginSearch(PrototypeExpeditionRegionId.Beach);
                    RefreshAll();
                    break;
                case "bag":
                    prepared = session.BeginSearch(PrototypeExpeditionRegionId.Beach);
                    RefreshAll();
                    NodeView node = nodes.FirstOrDefault(view =>
                        PrototypeSearchRegionCatalog.StartingExpeditionFor(view.Definition.RegionId) ==
                        PrototypeExpeditionRegionId.Beach);
                    if (prepared && node != null)
                    {
                        playerPresentation.Apply(playerTraversal.Warp(node.X, PrototypePlayerTraversal.LandY, false));
                        prepared = searchNodeRuntime.TryOpen(node.Definition, session) == PrototypeSearchOpenResult.Opened;
                        RefreshAll(true);
                    }
                    else
                    {
                        prepared = false;
                    }
                    break;
                case "facility-popup":
                    prepared = OpenO11CapturePopup(PrototypeCampInteractionTargetKind.Workbench);
                    break;
                case "escape-popup":
                    prepared = OpenO11CapturePopup(PrototypeCampInteractionTargetKind.ShoreLaunch);
                    break;
                default:
                    prepared = false;
                    break;
            }

            RefreshO11ProductionVisuals();
            Canvas.ForceUpdateCanvases();
            detail = scenario + "; locale=" + localeCode + "; phase=" + session.Phase +
                     "; popup=" + campInteraction.OpenPopupKind +
                     "; bag=" + (bagPanel != null && bagPanel.activeSelf) +
                     "; tray=" + (searchLootTrayPanel != null && searchLootTrayPanel.activeSelf);
            return prepared;
        }

        public bool PrepareO11RegionCapture(PrototypeExpeditionRegionId region, string localeCode, out string detail)
        {
            RestartSession();
            submissionShellState = SubmissionShellState.Playing;
            localization.SetLocale(localeCode, false);
            RefreshO9O10Presentation();
            RefreshAll();

            GameSessionStableState state = session.CaptureStableState();
            state.MaxUnlockedExpeditionOrdinal = (int)region;
            if (!session.RestoreStableState(state))
            {
                detail = "failed to prepare review-only region unlock fixture";
                return false;
            }

            bool prepared = session.BeginSearch(region);
            RefreshAll();
            RefreshO11ProductionVisuals();
            Canvas.ForceUpdateCanvases();
            detail = region + "; profile=" + session.ActiveRegionProfileId +
                     "; nodes=" + nodes.Count(view =>
                         PrototypeSearchRegionCatalog.StartingExpeditionFor(view.Definition.RegionId) == region);
            return prepared;
        }

        public bool PrepareO11AnimationCapture(
            string animationState,
            float phaseSeconds,
            string localeCode,
            out string detail)
        {
            RestartSession();
            submissionShellState = SubmissionShellState.Playing;
            localization.SetLocale(localeCode, false);
            o7InitialGuideDismissed = true;
            RefreshO9O10Presentation();
            RefreshAll();

            bool prepared = session.BeginSearch(PrototypeExpeditionRegionId.Beach);
            RefreshAll();
            RefreshO11ProductionVisuals();
            if (!prepared || playerPresentation == null || playerTraversal == null)
            {
                detail = "animation fixture unavailable";
                return false;
            }

            float now = Time.unscaledTime;
            float sample = now + Mathf.Max(0f, phaseSeconds);
            PrototypePlayerPresentationState idle = playerTraversal.Warp(-2.2f, PrototypePlayerTraversal.LandY, false);
            string canonical = (animationState ?? string.Empty).Trim().ToLowerInvariant();
            switch (canonical)
            {
                case "idle":
                    playerPresentation.Apply(idle, now);
                    playerPresentation.Apply(idle, sample);
                    break;
                case "walk":
                    var walk = new PrototypePlayerPresentationState(
                        idle.X, idle.Y, 1f, 1f, false, true);
                    playerPresentation.Apply(walk, now);
                    playerPresentation.Apply(walk, sample);
                    break;
                case "search":
                    playerPresentation.PlayAction(PrototypePlayerActionPose.Search, 2f);
                    playerPresentation.Apply(idle, now);
                    playerPresentation.Apply(idle, sample);
                    break;
                case "ladder":
                    playerPresentation.PlayAction(PrototypePlayerActionPose.Climb, 2f);
                    playerPresentation.Apply(idle, now);
                    playerPresentation.Apply(idle, sample);
                    break;
                case "swim":
                    PrototypePlayerPresentationState swim = playerTraversal.Warp(
                        PrototypePlayerTraversal.CoastlineX - 1.4f,
                        PrototypePlayerTraversal.WaterY,
                        true);
                    playerPresentation.Apply(swim, now);
                    playerPresentation.Apply(swim, sample);
                    break;
                default:
                    detail = "unsupported animation state " + canonical;
                    return false;
            }

            SpriteRenderer renderer = playerRoot.GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(value => value.enabled && value.gameObject.activeInHierarchy);
            detail = canonical + "; production-state=" + playerPresentation.ActiveProductionState +
                     "; frame=" + playerPresentation.ActiveProductionFrame +
                     "; sprite=" + (renderer == null || renderer.sprite == null ? "missing" : renderer.sprite.name) +
                     "; local-pos=" + (renderer == null ? "missing" : renderer.transform.localPosition.ToString("F3")) +
                     "; local-rot-z=" + (renderer == null ? "missing" : renderer.transform.localEulerAngles.z.ToString("F2"));
            Canvas.ForceUpdateCanvases();
            return renderer != null && renderer.sprite != null &&
                   renderer.sprite.name.EndsWith("-adopted", StringComparison.Ordinal) &&
                   playerPresentation.ActiveProductionState == canonical;
        }

        private bool OpenO11CapturePopup(PrototypeCampInteractionTargetKind target)
        {
            if (target == PrototypeCampInteractionTargetKind.Workbench)
            {
                session.Grant(ResourceKind.Wood, 10);
                session.Grant(ResourceKind.Salvage, 10);
                session.TryBuild(StructureKind.Workbench);
                campPlacement.EnsureInstalled(StructureKind.Workbench);
            }
            else if (target == PrototypeCampInteractionTargetKind.ShoreLaunch)
            {
                session.Grant(ResourceKind.Wood, 10);
                session.Grant(ResourceKind.Salvage, 10);
                hazardEscapeEndingRuntime.EscapeDirector.TryBuildFacility(session, PrototypeRaftEscapeConfig.EscapeId);
            }
            RefreshAll();
            campUse.Warp(GetCampInteractionTargetPosition(target));
            RefreshAll();
            bool opened = TryOpenCampPopup();
            RefreshAll(true);
            return opened && campInteraction.IsPopupOpen && campInteraction.OpenPopupKind == target;
        }
    }
}
