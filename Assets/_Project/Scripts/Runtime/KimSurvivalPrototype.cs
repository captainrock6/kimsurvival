using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KimSurvival
{
    public sealed class KimSurvivalPrototype : MonoBehaviour
    {
        private const string AssetCampBackground = "background.island-camp";
        private const string AssetSearchBackground = "background.coast-forest";
        private const string AssetKim = "character.mr-kim";
        private const string AssetSwim = "animation.mr-kim.swim";
        private const string AssetStructures = "object.camp-structures";
        private const string AssetVineBarrier = "object.vine-wood-barrier";
        private const string AssetHud = "ui.survival-hud";
        private const string AssetIcons = "icon.resource-tool-set";
        private const string AssetComedy = "effect.comedy-feedback";

        private const float CampBackgroundWorldWidth = 20f;
        private const float CampCanvasWidthPixels = 1672f;
        private const float CampCanvasHeightPixels = 941f;
        private const float CampWalkableBaselineTopPixels = 721f;
        private const float CampSignalAnchorTopPixels = 596f;
        private const float CampBackgroundGroundNormalizedY = (CampCanvasHeightPixels - CampWalkableBaselineTopPixels) / CampCanvasHeightPixels;
        private const float CampSignalAnchorNormalizedX = 0.86f;
        private const float CampSignalAnchorNormalizedY = (CampCanvasHeightPixels - CampSignalAnchorTopPixels) / CampCanvasHeightPixels;
        private const float CampSignalLabelX = 5.1f;
        private const float ResourceLabelWidth = 4.35f;
        private const float ResourceLabelHeight = 1.55f;
        private const float ResourceLabelViewportPadding = 0.22f;
        private const float ResourceLabelSafeViewportRight = 0.74f;
        private const float MinimumSupportedAspect = 1.6f;

        [SerializeField] private GameObject playerVisualPrefab;
        [SerializeField] private Sprite campBackgroundSprite;
        [SerializeField] private Sprite campGameplayGroundSprite;
        [SerializeField] private Sprite campForegroundSprite;
        [SerializeField] private Sprite campfireSprite;
        [SerializeField] private Sprite workbenchSprite;
        [SerializeField] private Sprite rainCollectorSprite;
        [SerializeField] private Sprite rescueSignalSprite;
        [SerializeField] private Sprite vineBarrierBlockedSprite;
        [SerializeField] private Sprite vineBarrierInteractableSprite;
        [SerializeField] private Sprite vineBarrierClearedSprite;

        private sealed class NodeView
        {
            public ResourceKind Kind;
            public int Amount;
            public float X;
            public bool Water;
            public GameObject Root;
            public Transform LabelRoot;
            public TMP_Text Label;
            public SpriteRenderer LabelBackground;
            public bool Collected;
        }

        private readonly List<NodeView> nodes = new List<NodeView>();
        private readonly List<Button> bagButtons = new List<Button>();
        private readonly List<SpriteRenderer> placementGhostOutlineRenderers = new List<SpriteRenderer>();
        private readonly Dictionary<StructureKind, GameObject> structureViews = new Dictionary<StructureKind, GameObject>();
        private readonly LegacyPrototypePlayerInput playerInput = new LegacyPrototypePlayerInput();
        private readonly PrototypePlayerTraversal playerTraversal = new PrototypePlayerTraversal();
        private readonly PrototypeCampPlacement campPlacement = new PrototypeCampPlacement();

        private GameSession session;
        private PrototypeLocalization localization;
        private Camera worldCamera;
        private Canvas canvas;
        private Sprite squareSprite;
        private Transform worldRoot;
        private SpriteRenderer campBackgroundRenderer;
        private SpriteRenderer campGameplayGroundRenderer;
        private SpriteRenderer campForegroundRenderer;
        private SpriteRenderer rescueSignalRenderer;
        private SpriteRenderer vineBarrierRenderer;
        private Transform playerRoot;
        private PrototypePlayerPresentation playerPresentation;
        private GameObject placementGhost;
        private SpriteRenderer placementGhostRenderer;
        private SpriteRenderer placementGhostBadgeRenderer;
        private TMP_Text placementGhostLabel;
        private Image messagePanelImage;
        private TMP_Text statusText;
        private TMP_Text resourceText;
        private TMP_Text messageText;
        private TMP_Text controlsText;
        private TMP_Text bagTitleText;
        private TMP_Text actionTitleText;
        private GameObject campActions;
        private GameObject bagPanel;
        private GameObject resultPanel;
        private TMP_Text resultTitleText;
        private TMP_Text resultDetailText;
        private Button campfireButton;
        private Button workbenchButton;
        private Button rainButton;
        private Button researchAxeButton;
        private Button craftAxeButton;
        private Button researchRopeButton;
        private Button craftRopeButton;
        private Button signalButton;
        private Button bagUpgradeButton;
        private Button eatButton;
        private Button phaseButton;
        private Button restartButton;
        private Button languageButton;
        private GamePhase renderedPhase;

        public GameSession Session
        {
            get { return session; }
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            session = new GameSession();
            localization = new PrototypeLocalization();
            localization.LocaleChanged += HandleLocaleChanged;
            squareSprite = MakeSquareSprite();
            BuildCamera();
            BuildEventSystem();
            BuildUi();
            renderedPhase = (GamePhase)(-1);
            RefreshAll();
        }

        private void Update()
        {
            playerInput.PollActiveDevice();
            if (playerInput.ReadSystemActions().LanguagePressed)
            {
                localization.CycleLocale();
            }

            if (session.Phase == GamePhase.Exploring)
            {
                UpdateExploration();
            }
            else if (session.Phase == GamePhase.Camp && campPlacement.IsActive)
            {
                UpdateCampPlacement();
            }

            if (renderedPhase != session.Phase)
            {
                RefreshAll();
            }
            else
            {
                RefreshHud();
            }
        }

        public void ConfigureCampBackgroundLayers(Sprite background, Sprite gameplayGround, Sprite foreground)
        {
            campBackgroundSprite = background;
            campGameplayGroundSprite = gameplayGround;
            campForegroundSprite = foreground;
        }

        public void ConfigureCampStructureArt(Sprite campfire, Sprite workbench, Sprite rainCollector, Sprite rescueSignal)
        {
            campfireSprite = campfire;
            workbenchSprite = workbench;
            rainCollectorSprite = rainCollector;
            rescueSignalSprite = rescueSignal;
        }

        public void ConfigureExplorationArt(Sprite vineBarrierBlocked, Sprite vineBarrierInteractable, Sprite vineBarrierCleared)
        {
            vineBarrierBlockedSprite = vineBarrierBlocked;
            vineBarrierInteractableSprite = vineBarrierInteractable;
            vineBarrierClearedSprite = vineBarrierCleared;
        }

        private void OnDestroy()
        {
            if (localization != null)
            {
                localization.LocaleChanged -= HandleLocaleChanged;
                localization.Dispose();
            }
        }

        private void HandleLocaleChanged()
        {
            RefreshAll();
        }

        private void BuildCamera()
        {
            GameObject cameraObject = new GameObject("Prototype Camera");
            cameraObject.transform.SetParent(transform, false);
            worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 5.625f;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.35f, 0.76f, 0.88f);
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
            worldCamera.nearClipPlane = 0.1f;
            worldCamera.farClipPlane = 100f;
            worldCamera.tag = "MainCamera";
        }

        private void BuildEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventObject = new GameObject("EventSystem");
            eventObject.transform.SetParent(transform, false);
            eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<StandaloneInputModule>();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("Canvas · " + AssetHud);
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = worldCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform top = CreatePanel("상태 HUD", canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -190f), new Vector2(-24f, -20f), new Color(0.05f, 0.09f, 0.12f, 0.92f));
            VerticalLayoutGroup topLayout = top.gameObject.AddComponent<VerticalLayoutGroup>();
            topLayout.padding = new RectOffset(160, 160, 8, 8);
            topLayout.spacing = 0f;
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = true;
            statusText = CreateText("날짜·상태", top, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 32, TextAnchor.MiddleLeft, Color.white);
            ConfigureLayout(statusText.gameObject, 1f, 1f, 0f, 68f);
            resourceText = CreateText("보유 자원", top, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 32, TextAnchor.MiddleRight, new Color(1f, 0.9f, 0.52f));
            ConfigureLayout(resourceText.gameObject, 1f, 1f, 0f, 68f);

            RectTransform message = CreatePanel("김씨 독백 · 배치 상태 · " + AssetComedy, canvas.transform, new Vector2(0.18f, 0.65f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.07f, 0.88f));
            messagePanelImage = message.GetComponent<Image>();
            messageText = CreateText("김씨 독백 또는 배치 상태", message, Vector2.zero, Vector2.one, new Vector2(26f, 10f), new Vector2(-26f, -10f), 29, TextAnchor.MiddleCenter, Color.white);

            RectTransform controlPanel = CreatePanel("조작 안내", canvas.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 20f), new Vector2(-24f, 165f), new Color(0.05f, 0.09f, 0.12f, 0.92f));
            HorizontalLayoutGroup controlLayout = controlPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            controlLayout.padding = new RectOffset(110, 110, 8, 8);
            controlLayout.spacing = 16f;
            controlLayout.childAlignment = TextAnchor.MiddleCenter;
            controlLayout.childControlWidth = true;
            controlLayout.childControlHeight = true;
            controlLayout.childForceExpandWidth = true;
            controlLayout.childForceExpandHeight = true;
            controlsText = CreateText("조작", controlPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 32, TextAnchor.MiddleCenter, Color.white);
            ConfigureLayout(controlsText.gameObject, 4f, 1f, 760f, 0f);
            languageButton = CreateButton("언어 설정", controlPanel, Vector2.zero, Vector2.one, string.Empty, delegate { localization.CycleLocale(); });
            ConfigureLayout(languageButton.gameObject, 1.45f, 1f, 270f, 0f);
            languageButton.GetComponentInChildren<TMP_Text>().fontSize = 32f;

            campActions = CreatePanel("캠프 행동", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 130f), new Vector2(765f, 715f), new Color(0.06f, 0.12f, 0.11f, 0.91f)).gameObject;
            actionTitleText = CreateText("캠프 행동 제목", campActions.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -64f), new Vector2(-18f, -12f), 32, TextAnchor.MiddleLeft, new Color(1f, 0.91f, 0.5f));

            campfireButton = CreateActionButton(campActions.transform, 0, string.Empty, delegate { BeginCampPlacement(StructureKind.Campfire); });
            workbenchButton = CreateActionButton(campActions.transform, 1, string.Empty, delegate { BeginCampPlacement(StructureKind.Workbench); });
            rainButton = CreateActionButton(campActions.transform, 2, string.Empty, delegate { BeginCampPlacement(StructureKind.RainCollector); });
            researchAxeButton = CreateActionButton(campActions.transform, 3, string.Empty, delegate { session.TryResearch(TechKind.StoneAxe); RefreshAll(); });
            craftAxeButton = CreateActionButton(campActions.transform, 4, string.Empty, delegate { session.TryCraft(TechKind.StoneAxe); RefreshAll(); });
            researchRopeButton = CreateActionButton(campActions.transform, 5, string.Empty, delegate { session.TryResearch(TechKind.Rope); RefreshAll(); });
            craftRopeButton = CreateActionButton(campActions.transform, 6, string.Empty, delegate { session.TryCraft(TechKind.Rope); RefreshAll(); });
            signalButton = CreateActionButton(campActions.transform, 7, string.Empty, delegate { session.TryUpgradeSignal(); RefreshAll(); });
            signalButton.GetComponentInChildren<TMP_Text>().fontSize = 36f;
            eatButton = CreateActionButton(campActions.transform, 8, string.Empty, delegate { session.UseFood(); RefreshAll(); });
            phaseButton = CreateActionButton(campActions.transform, 9, string.Empty, HandlePhaseButton);

            bagPanel = CreatePanel("가방 · " + AssetIcons, canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-455f, 130f), new Vector2(-30f, 715f), new Color(0.09f, 0.11f, 0.12f, 0.92f)).gameObject;
            bagTitleText = CreateText("가방 제목", bagPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -88f), new Vector2(-18f, -8f), 34, TextAnchor.MiddleCenter, new Color(1f, 0.91f, 0.5f));
            bagUpgradeButton = CreateButton("가방 용량 확장", bagPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), string.Empty, delegate { session.TryUpgradeBagCapacity(); RefreshAll(); }, new Vector2(22f, -210f), new Vector2(403f, -98f));
            bagUpgradeButton.GetComponentInChildren<TMP_Text>().fontSize = 34f;
            for (int i = 0; i < GameSession.MaximumBagSlotCount; i += 1)
            {
                int capturedIndex = i;
                Button slot = CreateBagButton(bagPanel.transform, i, delegate { session.ReplaceBagSlot(capturedIndex); RefreshAll(); });
                slot.GetComponentInChildren<TMP_Text>().fontSize = 34f;
                bagButtons.Add(slot);
            }

            resultPanel = CreatePanel("결과", canvas.transform, new Vector2(0.24f, 0.22f), new Vector2(0.76f, 0.73f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.08f, 0.09f, 0.96f)).gameObject;
            resultTitleText = CreateText("결과 제목", resultPanel.transform, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero, 56, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.35f));
            resultDetailText = CreateText("결과 설명", resultPanel.transform, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.66f), Vector2.zero, Vector2.zero, 30, TextAnchor.MiddleCenter, Color.white);
            restartButton = CreateButton("다시 시작", resultPanel.transform, new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.24f), string.Empty, delegate { session.Reset(); campPlacement.Reset(); RefreshAll(); });
        }

        private void HandlePhaseButton()
        {
            if (campPlacement.IsActive)
            {
                return;
            }

            if (session.ExpeditionCompleted)
            {
                session.EndDay();
            }
            else
            {
                session.BeginSearch();
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            renderedPhase = session.Phase;
            RebuildWorld();
            actionTitleText.text = localization.Format("ui.camp.title");
            SetButton(restartButton, localization.Format("ui.restart"), true);
            string languageKey = localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode ? "ui.language.switch.ko" : "ui.language.switch.en";
            SetButton(languageButton, localization.Format(languageKey), true);
            bool camp = session.Phase == GamePhase.Camp;
            bool result = session.Phase == GamePhase.Result;
            bool placing = camp && campPlacement.IsActive;
            campActions.SetActive(camp && !placing);
            bagPanel.SetActive(!result && !placing);
            bagUpgradeButton.gameObject.SetActive(camp && !placing);
            resultPanel.SetActive(result);
            if (result)
            {
                resultTitleText.text = localization.Format(session.ResultTitle());
                resultDetailText.text = localization.Format(session.ResultDetail());
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
            else if (camp)
            {
                UpdateCampButtons();
                EventSystem.current.SetSelectedGameObject(campPlacement.IsActive ? null : phaseButton.gameObject);
            }
            else if (session.HasPendingLoot && bagButtons.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(bagButtons[0].gameObject);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            RefreshHud();
            Canvas.ForceUpdateCanvases();
        }

        private void RefreshHud()
        {
            string phaseKey = session.Phase == GamePhase.Camp
                ? (session.ExpeditionCompleted ? "phase.camp.returned" : "phase.camp.preparing")
                : session.Phase == GamePhase.Exploring
                    ? (session.IsSwimming ? "phase.exploring.swimming" : "phase.exploring.land")
                    : "phase.result";
            string phaseName = localization.Format(phaseKey);
            statusText.text = session.Phase == GamePhase.Exploring
                ? localization.Format("hud.status.exploring", session.Day, GameSession.FinalDay, phaseName, Mathf.RoundToInt(session.Hunger), Mathf.RoundToInt(session.Energy), Mathf.RoundToInt(session.Daylight))
                : localization.Format("hud.status.camp", session.Day, GameSession.FinalDay, phaseName, Mathf.RoundToInt(session.Hunger), Mathf.RoundToInt(session.Energy));
            resourceText.text = localization.Format(
                "hud.resources",
                session.GetStorage(ResourceKind.Wood),
                session.GetStorage(ResourceKind.Stone),
                session.GetStorage(ResourceKind.Food),
                session.GetStorage(ResourceKind.Salvage),
                session.SignalStage,
                localization.Format(session.HasAxe ? "value.yes" : "value.no"),
                localization.Format(session.HasRope ? "value.yes" : "value.no"));
            messageText.text = localization.Format(session.LastMessage);
            string device = localization.DeviceName(playerInput.ActiveDevice);
            messageText.fontSize = session.LastMessage.Key.StartsWith("message.signal", StringComparison.Ordinal)
                ? 48f
                : session.LastMessage.Key.StartsWith("message.bag_upgrade", StringComparison.Ordinal) ? 40f : 29f;
            messageText.fontStyle = FontStyles.Normal;
            messagePanelImage.color = new Color(0.07f, 0.08f, 0.07f, 0.88f);

            if (session.Phase == GamePhase.Camp)
            {
                if (campPlacement.IsActive)
                {
                    ApplyPlacementGuidance(playerInput.ActiveDevice);
                }
                else
                {
                    controlsText.text = localization.Format(PrototypeInputPromptKeys.Camp(playerInput.ActiveDevice), device);
                }
                bagTitleText.text = localization.Format("bag.camp", session.ActiveBagSlotCount, GameSession.MaximumBagSlotCount);
            }
            else if (session.Phase == GamePhase.Exploring)
            {
                controlsText.text = localization.Format(PrototypeInputPromptKeys.Explore(playerInput.ActiveDevice), device, session.ActiveBagSlotCount);
                bagTitleText.text = localization.Format(session.HasPendingLoot ? "bag.pending" : "bag.exploring", session.ActiveBagSlotCount, GameSession.MaximumBagSlotCount);
            }

            RefreshBagButtons();
        }

        private void ApplyPlacementGuidance(PrototypeInputDevice device)
        {
            bool valid = campPlacement.CurrentValidity == CampPlacementValidity.Valid;
            string state = localization.Format(valid ? "placement.state.valid" : "placement.state.invalid");
            messageText.text = localization.Format("placement.summary", state, localization.Format(campPlacement.CurrentFeedback));
            messageText.fontSize = 36f;
            messageText.fontStyle = FontStyles.Bold;
            messagePanelImage.color = valid
                ? new Color(0.04f, 0.27f, 0.15f, 0.96f)
                : new Color(0.38f, 0.08f, 0.06f, 0.96f);
            controlsText.text = localization.Format(PrototypeInputPromptKeys.Placement(device), localization.DeviceName(device));
        }

        private void UpdateCampButtons()
        {
            bool available = !campPlacement.IsActive;
            SetButton(campfireButton, localization.Format(session.HasStructure(StructureKind.Campfire) ? "button.campfire.relocate" : "button.campfire.build"), available && (session.HasStructure(StructureKind.Campfire) || session.CanBuild(StructureKind.Campfire)));
            SetButton(workbenchButton, localization.Format(session.HasStructure(StructureKind.Workbench) ? "button.workbench.relocate" : "button.workbench.build"), available && (session.HasStructure(StructureKind.Workbench) || session.CanBuild(StructureKind.Workbench)));
            SetButton(rainButton, localization.Format(session.HasStructure(StructureKind.RainCollector) ? "button.rain.relocate" : "button.rain.build"), available && (session.HasStructure(StructureKind.RainCollector) || session.CanBuild(StructureKind.RainCollector)));
            SetButton(researchAxeButton, localization.Format(session.HasResearched(TechKind.StoneAxe) ? "button.research.axe.done" : "button.research.axe"), available && session.CanResearch(TechKind.StoneAxe));
            SetButton(craftAxeButton, localization.Format(session.HasAxe ? "button.craft.axe.done" : "button.craft.axe"), available && session.CanCraft(TechKind.StoneAxe));
            SetButton(researchRopeButton, localization.Format(session.HasResearched(TechKind.Rope) ? "button.research.rope.done" : "button.research.rope"), available && session.CanResearch(TechKind.Rope));
            SetButton(craftRopeButton, localization.Format(session.HasRope ? "button.craft.rope.done" : "button.craft.rope"), available && session.CanCraft(TechKind.Rope));
            SetButton(signalButton, FormatSignalButton(), available && session.SignalStage < 2);
            signalButton.GetComponentInChildren<TMP_Text>().fontSize = localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode ? 31f : 36f;
            UpdateBagUpgradeButton(available);
            SetButton(eatButton, localization.Format("button.eat", session.GetStorage(ResourceKind.Food)), available && session.GetStorage(ResourceKind.Food) > 0 && session.Hunger < 100f);
            string phaseButtonKey = session.ExpeditionCompleted ? (session.Day >= GameSession.FinalDay ? "button.day.final" : "button.day.next") : "button.search.start";
            SetButton(phaseButton, localization.Format(phaseButtonKey), available);
        }

        private void UpdateBagUpgradeButton(bool available)
        {
            if (session.HasBagCapacityUpgrade)
            {
                SetButton(bagUpgradeButton, localization.Format("button.bag_upgrade.complete", GameSession.MaximumBagSlotCount), false);
                return;
            }

            if (!session.HasStructure(StructureKind.Workbench))
            {
                SetButton(bagUpgradeButton, localization.Format("button.bag_upgrade.locked", GameSession.DefaultBagSlotCount, GameSession.MaximumBagSlotCount), false);
                return;
            }

            SetButton(
                bagUpgradeButton,
                localization.Format(
                    "button.bag_upgrade.available",
                    GameSession.DefaultBagSlotCount,
                    GameSession.MaximumBagSlotCount,
                    Mathf.Min(GameSession.BagUpgradeWoodCost, session.GetStorage(ResourceKind.Wood)),
                    GameSession.BagUpgradeWoodCost,
                    Mathf.Min(GameSession.BagUpgradeSalvageCost, session.GetStorage(ResourceKind.Salvage)),
                    GameSession.BagUpgradeSalvageCost),
                available);
        }

        private string FormatSignalButton()
        {
            if (session.SignalStage >= 2)
            {
                return localization.Format("button.signal.done");
            }

            string requirementState = localization.Format(
                session.SignalStage == 0
                    ? (session.HasStructure(StructureKind.Workbench) ? "value.yes" : "value.no")
                    : (session.HasRope ? "value.yes" : "value.no"));
            string key = session.SignalStage == 0 ? "button.signal.stage1" : "button.signal.stage2";
            return localization.Format(
                key,
                requirementState,
                Mathf.Min(2, session.GetStorage(ResourceKind.Wood)),
                Mathf.Min(2, session.GetStorage(ResourceKind.Salvage)));
        }

        private void RefreshBagButtons()
        {
            for (int i = 0; i < bagButtons.Count; i += 1)
            {
                bool active = session.IsBagSlotActive(i);
                BagStack stack = session.GetBagSlot(i);
                TMP_Text label = bagButtons[i].GetComponentInChildren<TMP_Text>();
                label.text = !active
                    ? localization.Format("bag.slot.locked", i + 1)
                    : stack.IsEmpty
                    ? localization.Format("bag.slot.empty", i + 1)
                    : localization.Format("bag.slot.stack", i + 1, stack.Kind, stack.Amount);
                bagButtons[i].interactable = active && session.Phase == GamePhase.Exploring && session.HasPendingLoot;
                Image image = bagButtons[i].GetComponent<Image>();
                image.color = !active
                    ? new Color(0.08f, 0.1f, 0.1f, 0.95f)
                    : stack.IsEmpty ? new Color(0.18f, 0.22f, 0.22f, 0.95f) : ResourceColor(stack.Kind, 0.95f);
            }
        }

        private void RebuildWorld()
        {
            if (worldRoot != null)
            {
                worldRoot.gameObject.SetActive(false);
                Destroy(worldRoot.gameObject);
            }

            GameObject root = new GameObject("Runtime Placeholder World");
            root.transform.SetParent(transform, false);
            worldRoot = root.transform;
            nodes.Clear();
            structureViews.Clear();
            playerRoot = null;
            playerPresentation = null;
            placementGhost = null;
            placementGhostRenderer = null;
            placementGhostBadgeRenderer = null;
            placementGhostLabel = null;
            campBackgroundRenderer = null;
            campGameplayGroundRenderer = null;
            campForegroundRenderer = null;
            rescueSignalRenderer = null;
            placementGhostOutlineRenderers.Clear();

            if (session.Phase == GamePhase.Exploring)
            {
                CreateSearchWorld();
            }
            else
            {
                CreateCampWorld();
            }
        }

        private void CreateCampWorld()
        {
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
            worldCamera.backgroundColor = new Color(0.36f, 0.77f, 0.9f);
            CreateCampBackground();
            if (campPlacement.IsActive)
            {
                float buildWidth = PrototypeCampPlacement.BuildMaximumX - PrototypeCampPlacement.BuildMinimumX;
                float buildCenter = (PrototypeCampPlacement.BuildMinimumX + PrototypeCampPlacement.BuildMaximumX) * 0.5f;
                CreateRect("호환 건설 구역", new Vector2(buildCenter, PrototypeCampPlacement.FloorY + 0.08f), new Vector2(buildWidth, 0.16f), new Color(0.16f, 0.72f, 0.38f, 0.78f), -5);
                CreateRect("건설 구역 왼쪽 경계", new Vector2(PrototypeCampPlacement.BuildMinimumX, PrototypeCampPlacement.FloorY + 0.48f), new Vector2(0.1f, 1.02f), new Color(0.75f, 1f, 0.72f, 0.92f), -3);
                CreateRect("건설 구역 오른쪽 경계", new Vector2(PrototypeCampPlacement.BuildMaximumX, PrototypeCampPlacement.FloorY + 0.48f), new Vector2(0.1f, 1.02f), new Color(0.75f, 1f, 0.72f, 0.92f), -3);
                CreateWorldBadge("호환 건설 구역 안내", localization.Format("world.build_zone"), new Vector2(2.5f, -3.15f), new Vector2(4.8f, 1.65f), new Color(0.04f, 0.25f, 0.13f, 0.96f), Color.white);
                CreateReservedCampStrip("world.entrance", PrototypeCampPlacement.EntranceMinimumX, PrototypeCampPlacement.EntranceMaximumX, new Color(0.95f, 0.38f, 0.18f, 0.72f));
                CreateReservedCampStrip("world.required_path", PrototypeCampPlacement.RequiredPathMinimumX, PrototypeCampPlacement.RequiredPathMaximumX, new Color(1f, 0.72f, 0.16f, 0.72f));
            }
            CreateKim(new Vector2(-5f, -2.18f));

            CreatePlacedStructure(StructureKind.Campfire, new Color(1f, 0.43f, 0.14f));
            CreatePlacedStructure(StructureKind.Workbench, new Color(0.48f, 0.26f, 0.12f));
            CreatePlacedStructure(StructureKind.RainCollector, new Color(0.27f, 0.7f, 0.86f));

            Vector2 signalAnchor = GetCampArtPoint(CampSignalAnchorNormalizedX, CampSignalAnchorNormalizedY);
            CreateRescueSignal(signalAnchor);
            if (campPlacement.IsActive)
            {
                CreateFootprintOutline(worldRoot, new Vector2(2.25f, 0.35f), new Color(1f, 0.88f, 0.38f, 0.95f), null, signalAnchor);
                CreateWorldBadge("구조 신호대 전용 앵커 안내", localization.Format("world.signal_anchor", session.SignalStage), new Vector2(CampSignalLabelX, signalAnchor.y - 0.78f), new Vector2(3.8f, 1.55f), new Color(0.16f, 0.17f, 0.18f, 0.96f), new Color(1f, 0.88f, 0.38f));
                CreatePlacementGhost();
            }
        }

        private void CreateCampBackground()
        {
            if (campBackgroundSprite == null || campGameplayGroundSprite == null || campForegroundSprite == null)
            {
                CreateRect("캠프 배경 레이어 누락 · " + AssetCampBackground, Vector2.zero, new Vector2(20f, 11.25f), new Color(0.36f, 0.77f, 0.9f), -30);
                Debug.LogError("[Kim Survival] Adopted three-layer camp background is not fully assigned: " + AssetCampBackground);
                return;
            }

            GameObject backgroundRoot = new GameObject("채택 캠프 3레이어 · " + AssetCampBackground);
            backgroundRoot.transform.SetParent(worldRoot, false);
            float scale = CampBackgroundWorldWidth / campBackgroundSprite.bounds.size.x;
            float sourceGroundY = Mathf.Lerp(campBackgroundSprite.bounds.min.y, campBackgroundSprite.bounds.max.y, CampBackgroundGroundNormalizedY);
            backgroundRoot.transform.localScale = new Vector3(scale, scale, 1f);
            backgroundRoot.transform.localPosition = new Vector3(0f, PrototypeCampPlacement.FloorY - sourceGroundY * scale, 0f);
            campBackgroundRenderer = CreateCampBackgroundLayer(backgroundRoot.transform, "배경", campBackgroundSprite, -30);
            campGameplayGroundRenderer = CreateCampBackgroundLayer(backgroundRoot.transform, "게임플레이 지면", campGameplayGroundSprite, -20);
            campForegroundRenderer = CreateCampBackgroundLayer(backgroundRoot.transform, "전경", campForegroundSprite, 12);
        }

        private static SpriteRenderer CreateCampBackgroundLayer(Transform parent, string name, Sprite sprite, int sortingOrder)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private Vector2 GetCampArtPoint(float normalizedX, float normalizedY)
        {
            if (campBackgroundRenderer == null || campBackgroundSprite == null)
            {
                return new Vector2(7.2f, -1.32f);
            }

            Bounds bounds = campBackgroundSprite.bounds;
            Vector3 localPoint = new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedX),
                Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY),
                0f);
            return campBackgroundRenderer.transform.TransformPoint(localPoint);
        }

        private void BeginCampPlacement(StructureKind kind)
        {
            bool relocating = session.HasStructure(kind);
            if (!relocating && !session.CanBuild(kind))
            {
                return;
            }

            campPlacement.Begin(kind, relocating);
            RefreshAll();
        }

        private void UpdateCampPlacement()
        {
            PrototypeCampPlacementActions actions = playerInput.ReadCampPlacementActions(worldCamera);
            campPlacement.Update(actions, Time.deltaTime);
            UpdatePlacementGhost();

            if (actions.CancelPressed)
            {
                campPlacement.Cancel();
                RefreshAll();
                return;
            }

            if (actions.ConfirmPressed)
            {
                ConfirmCampPlacement();
            }
        }

        private bool ConfirmCampPlacement()
        {
            if (!campPlacement.IsActive || campPlacement.CurrentValidity != CampPlacementValidity.Valid)
            {
                RefreshHud();
                return false;
            }

            StructureKind kind = campPlacement.SelectedKind;
            bool relocating = session.HasStructure(kind);
            if (!relocating && !session.TryBuild(kind))
            {
                RefreshHud();
                return false;
            }

            if (!campPlacement.Commit())
            {
                throw new InvalidOperationException("유효한 캠프 배치를 확정하지 못했습니다.");
            }

            RefreshAll();
            return true;
        }

        private void CreateSearchWorld()
        {
            playerTraversal.Reset();
            worldCamera.transform.position = new Vector3(-3.8f, 0f, -10f);
            worldCamera.backgroundColor = new Color(0.35f, 0.74f, 0.9f);
            CreateRect("하늘 · " + AssetSearchBackground, new Vector2(4f, 1.5f), new Vector2(36f, 8.2f), new Color(0.35f, 0.74f, 0.9f), -20);
            CreateRect("얕은 연안", new Vector2(-8f, -1.15f), new Vector2(10f, 3f), new Color(0.12f, 0.55f, 0.76f), -15);
            CreateRect("연안 모래 바닥", new Vector2(-8f, -3.55f), new Vector2(10f, 1.3f), new Color(0.66f, 0.57f, 0.34f), -12);
            CreateRect("해변과 숲 바닥", new Vector2(8.5f, -3.25f), new Vector2(25f, 1.9f), new Color(0.87f, 0.68f, 0.34f), -10);
            CreateRect("해안선", new Vector2(PrototypePlayerTraversal.CoastlineX, -2.35f), new Vector2(0.28f, 1.25f), new Color(0.86f, 0.94f, 0.86f), -4);
            CreateSun(new Vector2(-1f, 3.6f));
            for (int i = 0; i < 7; i += 1)
            {
                CreatePalm(new Vector2(2.8f + i * 2.35f, -2.28f), 0.75f + (i % 2) * 0.14f);
            }

            GameObject returnFlag = CreateRect("귀환 지점", new Vector2(-2.7f, -1.25f), new Vector2(0.18f, 2.6f), new Color(0.35f, 0.2f, 0.08f), 2);
            CreateRect("귀환 깃발", new Vector2(-2.15f, -0.35f), new Vector2(1.1f, 0.65f), new Color(1f, 0.48f, 0.16f), 3);
            CreateWorldLabel(returnFlag.transform, localization.Format("world.return"), new Vector3(0.6f, 1.7f, -0.1f), 45, Color.black);

            CreateVineBarrier();
            CreateWorldBadge(
                worldRoot,
                "숲길 장벽 안내",
                localization.Format(session.HasAxe ? "world.barrier.axe.pass" : "world.barrier.axe.need"),
                new Vector2(8.7f, 1.65f),
                new Vector2(5.8f, 1.55f),
                new Color(0.03f, 0.09f, 0.07f, 0.97f),
                Color.white,
                out _,
                0.085f,
                36f,
                36f);

            SpawnNode(-8.2f, ResourceKind.Salvage, 2, true);
            SpawnNode(-5.8f, ResourceKind.Food, 2, true);
            SpawnNode(-1.1f, ResourceKind.Wood, 2);
            SpawnNode(1.5f, ResourceKind.Stone, 2);
            SpawnNode(4.1f, ResourceKind.Food, 2);
            SpawnNode(6.8f, ResourceKind.Salvage, 2);
            SpawnNode(10.2f, ResourceKind.Wood, 2);
            SpawnNode(12.8f, ResourceKind.Salvage, 2);
            SpawnNode(15.2f, ResourceKind.Stone, 2);
            SpawnNode(17.7f, ResourceKind.Salvage, 2);
            CreateKim(new Vector2(playerTraversal.X, playerTraversal.Y));
            CreateSwimWake();
        }

        private void UpdateExploration()
        {
            PrototypePlayerActions actions = playerInput.ReadActions(session.HasPendingLoot, session.ActiveBagSlotCount);
            if (session.HasPendingLoot)
            {
                if (actions.BagSlotIndex >= 0)
                {
                    session.ReplaceBagSlot(actions.BagSlotIndex);
                    RefreshAll();
                    return;
                }

                if (actions.CancelPressed)
                {
                    session.DiscardPendingLoot();
                    RefreshAll();
                }
                return;
            }

            PrototypeTraversalStep traversalStep = playerTraversal.Step(actions, Time.deltaTime, Time.time, session);
            if (traversalStep.ReachedBlockedPath)
            {
                messageText.text = localization.Format("message.barrier.axe_blocked");
            }

            playerPresentation.Apply(traversalStep.Presentation);

            float targetCameraX = Mathf.Clamp(playerTraversal.X + 2.5f, -6.5f, 12.5f);
            Vector3 cameraPosition = worldCamera.transform.position;
            cameraPosition.x = Mathf.Lerp(cameraPosition.x, targetCameraX, Time.deltaTime * 4f);
            worldCamera.transform.position = cameraPosition;
            UpdateResourceLabelLayout();

            if (actions.InteractPressed)
            {
                GatherNearestNode();
            }

            if (actions.ReturnPressed)
            {
                session.ReturnToCamp(false);
                RefreshAll();
                return;
            }

            session.TickSearch(Time.deltaTime, Mathf.Abs(actions.Horizontal) > 0.05f);
            if (session.Phase != GamePhase.Exploring)
            {
                RefreshAll();
            }
        }

        private void GatherNearestNode()
        {
            NodeView nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < nodes.Count; i += 1)
            {
                if (nodes[i].Collected)
                {
                    continue;
                }

                float distance = Mathf.Abs(nodes[i].X - playerTraversal.X);
                if (distance < 1.35f && distance < nearestDistance)
                {
                    nearest = nodes[i];
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
            {
                messageText.text = localization.Format("message.nothing_near");
                return;
            }

            GatherResult result = session.TryGather(nearest.Kind, nearest.Amount, nearest.Water);
            if (result != GatherResult.Rejected)
            {
                nearest.Collected = true;
                nearest.Root.SetActive(false);
                if (result == GatherResult.PendingSwap)
                {
                    RefreshAll();
                }
                else
                {
                    RefreshHud();
                }
            }
        }

        public string RunAutomatedVerification(
            string explorationScreenshotPath,
            string swimmingScreenshotPath,
            string placementKoreanScreenshotPath,
            string placementEnglishScreenshotPath,
            string signalKoreanScreenshotPath,
            string signalEnglishScreenshotPath,
            string bagLockedKorean1280ScreenshotPath,
            string bagUpgradedEnglish1280ScreenshotPath,
            string bagLockedKorean1920ScreenshotPath,
            string bagUpgradedEnglish1920ScreenshotPath)
        {
            session.Reset();
            campPlacement.Reset();
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            RefreshAll();

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            languageButton.onClick.Invoke();
            Require(localization.CurrentLocaleCode == PrototypeLocalization.EnglishLocaleCode &&
                    actionTitleText.text == "Base Camp · Craft / Build / Research", "언어 버튼의 즉시 영어 전환");
            Require(localization.Format("dev.fallback_probe") == "한국어 폴백 확인", "영어 누락 번역의 한국어 폴백");
            languageButton.onClick.Invoke();
            Require(localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode &&
                    actionTitleText.text == "베이스캠프 · 제작 / 건설 / 연구", "언어 버튼의 즉시 한국어 전환");
            Require(statusText.font != null && messageText.font != null, "로케일별 TMP 폰트 매핑 적용");
            RequireCampBackgroundAlignment();
            RequireCampStructureArt();

            TMP_Text signalLabel = signalButton.GetComponentInChildren<TMP_Text>();
            Require(signalButton.interactable && signalLabel.text.Contains("작업대") && signalLabel.text.Contains("없음"), "재료가 부족해도 선택 가능한 1단계 작업대 요구 표시");
            signalButton.onClick.Invoke();
            Require(session.SignalStage == 0 && session.LastMessage.Key == "message.signal.workbench" && messageText.text.Contains("작업대가 없다"), "1단계 작업대 없음 실패 피드백");
            RequireReadableSignalFeedback();
            if (!string.IsNullOrWhiteSpace(signalKoreanScreenshotPath))
            {
                CaptureVerificationPng(signalKoreanScreenshotPath, 1280, 800);
            }

            Require(session.TryBuild(StructureKind.Workbench), "신호대 단계 검증용 작업대 건설");
            RefreshAll();
            signalButton.onClick.Invoke();
            Require(session.SignalStage == 1 && !session.HasRope, "밧줄 없이 가능한 구조 신호대 1단계 UI 경로");
            Require(signalButton.interactable && signalLabel.text.Contains("밧줄") && signalLabel.text.Contains("없음"), "재료가 부족해도 선택 가능한 2단계 밧줄 요구 표시");
            signalButton.onClick.Invoke();
            Require(session.SignalStage == 1 && session.LastMessage.Key == "message.signal.rope", "밧줄 없는 구조 신호대 2단계의 명확한 거절");
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            Require(signalLabel.text.Contains("Rope") && signalLabel.text.Contains("None") && messageText.text.Contains("No rope"), "영어 2단계 요구조건과 부족 사유 즉시 전환");
            RequireReadableSignalFeedback();
            if (!string.IsNullOrWhiteSpace(signalEnglishScreenshotPath))
            {
                CaptureVerificationPng(signalEnglishScreenshotPath, 1280, 800);
            }

            Require(session.TryResearch(TechKind.Rope) && session.TryCraft(TechKind.Rope), "재료 부족 UI 검증용 밧줄 제작");
            session.Grant(ResourceKind.Wood, -999);
            session.Grant(ResourceKind.Salvage, -999);
            RefreshAll();
            Require(signalButton.interactable, "나무·표류물 부족 상태에서도 구조 신호대 행동 선택 가능");
            signalButton.onClick.Invoke();
            Require(session.SignalStage == 1 && session.LastMessage.Key == "message.signal.materials" && messageText.text.Contains("Wood and salvage are short"), "나무·표류물 동시 부족 UI 피드백");

            session.Reset();
            campPlacement.Reset();
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();

            TMP_Text bagUpgradeLabel = bagUpgradeButton.GetComponentInChildren<TMP_Text>();
            Require(session.ActiveBagSlotCount == GameSession.DefaultBagSlotCount && bagButtons.Count == GameSession.MaximumBagSlotCount, "새 게임 4칸·물리 최대 6칸 UI");
            Require(!session.IsBagSlotActive(4) && !session.IsBagSlotActive(5) &&
                    bagButtons[4].GetComponentInChildren<TMP_Text>().text.Contains("잠김") &&
                    bagButtons[5].GetComponentInChildren<TMP_Text>().text.Contains("잠김"), "업그레이드 전 5·6번 슬롯 잠금 표시");
            Require(!bagUpgradeButton.interactable && bagUpgradeLabel.text.Contains("작업대 필요"), "작업대 없는 가방 확장 잠금 표시");
            RequireReadableBagUi();
            if (!string.IsNullOrWhiteSpace(bagLockedKorean1280ScreenshotPath))
            {
                CaptureVerificationPng(bagLockedKorean1280ScreenshotPath, 1280, 800);
            }
            if (!string.IsNullOrWhiteSpace(bagLockedKorean1920ScreenshotPath))
            {
                CaptureVerificationPng(bagLockedKorean1920ScreenshotPath, 1920, 1080);
            }

            Require(session.TryBuild(StructureKind.Workbench), "가방 확장 UI 검증용 작업대 건설");
            RefreshAll();
            int woodBeforeBagUpgrade = session.GetStorage(ResourceKind.Wood);
            int salvageBeforeBagUpgrade = session.GetStorage(ResourceKind.Salvage);
            Require(bagUpgradeButton.interactable && bagUpgradeLabel.text.Contains("4→6") && bagUpgradeLabel.text.Contains("나무 2/2") && bagUpgradeLabel.text.Contains("표류물 1/1"), "가방 확장 비용과 4→6 표시");
            bagUpgradeButton.onClick.Invoke();
            Require(session.ActiveBagSlotCount == GameSession.MaximumBagSlotCount &&
                    session.GetStorage(ResourceKind.Wood) == woodBeforeBagUpgrade - GameSession.BagUpgradeWoodCost &&
                    session.GetStorage(ResourceKind.Salvage) == salvageBeforeBagUpgrade - GameSession.BagUpgradeSalvageCost, "가방 확장 UI의 원자적 1회 비용과 6칸 활성화");
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            Require(!bagUpgradeButton.interactable && bagUpgradeLabel.text.Contains("Complete") && bagTitleText.text.Contains("Bag 6/6"), "영어 가방 확장 완료·6칸 표시");
            RequireReadableBagUi();
            if (!string.IsNullOrWhiteSpace(bagUpgradedEnglish1280ScreenshotPath))
            {
                CaptureVerificationPng(bagUpgradedEnglish1280ScreenshotPath, 1280, 800);
            }
            if (!string.IsNullOrWhiteSpace(bagUpgradedEnglish1920ScreenshotPath))
            {
                CaptureVerificationPng(bagUpgradedEnglish1920ScreenshotPath, 1920, 1080);
            }

            phaseButton.onClick.Invoke();
            Require(session.Phase == GamePhase.Exploring, "가방 6칸 UI 검증 수색 시작");
            Require(session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                    session.TryGather(ResourceKind.Stone, 2) == GatherResult.Added &&
                    session.TryGather(ResourceKind.Food, 2) == GatherResult.Added &&
                    session.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added &&
                    session.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                    session.TryGather(ResourceKind.Stone, 1) == GatherResult.Added &&
                    session.TryGather(ResourceKind.Stone, 1) == GatherResult.Added, "5·6번 슬롯 획득과 6번 슬롯 중첩");
            Require(session.GetBagSlot(4).Kind == ResourceKind.Wood && session.GetBagSlot(4).Amount == 2 &&
                    session.GetBagSlot(5).Kind == ResourceKind.Stone && session.GetBagSlot(5).Amount == 2, "5·6번 슬롯 데이터 경로");
            Require(session.TryGather(ResourceKind.Food, 1) == GatherResult.PendingSwap, "6칸 가득 참 이후 pending swap");
            RefreshAll();
            Require(EventSystem.current.currentSelectedGameObject == bagButtons[0].gameObject && bagButtons[4].interactable && bagButtons[5].interactable, "6칸 교체 창의 활성 슬롯 포커스");
            MoveUiSelection(MoveDirection.Down);
            MoveUiSelection(MoveDirection.Down);
            MoveUiSelection(MoveDirection.Right);
            Require(EventSystem.current.currentSelectedGameObject == bagButtons[5].gameObject, "게임패드 방향 입력으로 6번 슬롯 도달");
            SubmitUiSelection();
            Require(!session.HasPendingLoot && session.GetBagSlot(5).Kind == ResourceKind.Food, "게임패드 Submit으로 6번 슬롯 교체");
            Require(session.TryGather(ResourceKind.Stone, 1) == GatherResult.PendingSwap, "5번 슬롯 마우스 교체 준비");
            RefreshAll();
            bagButtons[4].onClick.Invoke();
            Require(!session.HasPendingLoot && session.GetBagSlot(4).Kind == ResourceKind.Stone, "마우스로 5번 슬롯 교체");
            Require(session.TryGather(ResourceKind.Salvage, 1) == GatherResult.PendingSwap, "6칸 pending 포기 준비");
            session.DiscardPendingLoot();
            Require(!session.HasPendingLoot, "6칸 pending 자원 포기");
            Require(session.ReturnToCamp(false) && session.ActiveBagSlotCount == GameSession.MaximumBagSlotCount, "5·6번 슬롯 귀환 이전과 용량 지속");
            Require(session.EndDay() && session.ActiveBagSlotCount == GameSession.MaximumBagSlotCount, "날짜 전환 뒤 6칸 지속");

            session.Reset();
            campPlacement.Reset();
            Require(session.ActiveBagSlotCount == GameSession.DefaultBagSlotCount && !session.HasPendingLoot && !session.IsBagSlotActive(4), "새 게임 초기화의 4칸·잠금·pending 정리");
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();

            campfireButton.onClick.Invoke();
            Require(campPlacement.IsActive && placementGhost != null, "모닥불 배치 유령 UI");
            Require(placementGhostLabel != null && placementGhostLabel.font != null, "월드 배치 유령의 TMP 폰트 매핑");
            Require(!campActions.activeSelf && !bagPanel.activeSelf, "배치 중 관리 패널을 숨겨 월드 시야 확보");
            campPlacement.SetCandidateX(-5f);
            UpdatePlacementGhost();
            Require(campPlacement.CurrentValidity == CampPlacementValidity.OutsideCampBounds && !ConfirmCampPlacement(), "캠프 경계 밖 배치 거부");
            campPlacement.SetCandidateX(-2.5f);
            UpdatePlacementGhost();
            Require(campPlacement.CurrentValidity == CampPlacementValidity.BlocksEntrance && !ConfirmCampPlacement(), "출입구 차단 배치 거부");
            campPlacement.SetCandidateX(0f);
            UpdatePlacementGhost();
            Require(campPlacement.CurrentValidity == CampPlacementValidity.BlocksRequiredPath && !ConfirmCampPlacement(), "필수 통로 차단 배치 거부");
            ApplyPlacementGuidance(PrototypeInputDevice.KeyboardMouse);
            Require(controlsText.text.Contains(localization.DeviceName(PrototypeInputDevice.KeyboardMouse)) && controlsText.text.Contains("마우스로 위치 이동"), "키보드·마우스 배치 안내 전환");
            RequireReadablePlacementUi();
            if (!string.IsNullOrWhiteSpace(placementKoreanScreenshotPath))
            {
                CaptureVerificationPng(placementKoreanScreenshotPath, 1280, 800);
            }

            campPlacement.SetCandidateX(-1.5f);
            UpdatePlacementGhost();
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            ApplyPlacementGuidance(PrototypeInputDevice.Gamepad);
            Require(campPlacement.CurrentValidity == CampPlacementValidity.Valid &&
                    controlsText.text.Contains(localization.DeviceName(PrototypeInputDevice.Gamepad)) &&
                    controlsText.text.Contains("left stick"), "게임패드 배치 안내 전환");
            RequireReadablePlacementUi();
            if (!string.IsNullOrWhiteSpace(placementEnglishScreenshotPath))
            {
                CaptureVerificationPng(placementEnglishScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            campPlacement.SetCandidateX(-1.5f);
            UpdatePlacementGhost();
            Require(campPlacement.CurrentValidity == CampPlacementValidity.Valid && ConfirmCampPlacement(), "모닥불 스냅 배치 확정");

            workbenchButton.onClick.Invoke();
            campPlacement.SetCandidateX(1.5f);
            Require(ConfirmCampPlacement(), "작업대 스냅 배치 확정");
            int woodBeforeRelocation = session.GetStorage(ResourceKind.Wood);
            int stoneBeforeRelocation = session.GetStorage(ResourceKind.Stone);
            int salvageBeforeRelocation = session.GetStorage(ResourceKind.Salvage);
            workbenchButton.onClick.Invoke();
            Require(campPlacement.IsRelocating, "건설된 작업대 재배치 진입");
            campPlacement.SetCandidateX(3.5f);
            Require(ConfirmCampPlacement(), "작업대 무료 재배치 확정");
            Require(Mathf.Approximately(campPlacement.GetInstalledPosition(StructureKind.Workbench).x, 3.5f), "작업대 위치 변경");
            Require(session.GetStorage(ResourceKind.Wood) == woodBeforeRelocation &&
                    session.GetStorage(ResourceKind.Stone) == stoneBeforeRelocation &&
                    session.GetStorage(ResourceKind.Salvage) == salvageBeforeRelocation, "재배치 추가 자원 비용 없음");

            rainButton.onClick.Invoke();
            Require(campPlacement.CurrentValidity == CampPlacementValidity.OverlapsStructure && !ConfirmCampPlacement(), "설비 겹침 배치 거부");
            campPlacement.SetCandidateX(1.5f);
            Require(ConfirmCampPlacement(), "빗물받이 스냅 배치 확정");
            researchAxeButton.onClick.Invoke();
            craftAxeButton.onClick.Invoke();
            researchRopeButton.onClick.Invoke();
            craftRopeButton.onClick.Invoke();
            Require(session.HasStructure(StructureKind.Campfire), "모닥불 UI 건설");
            Require(session.HasStructure(StructureKind.Workbench), "작업대 UI 건설");
            Require(session.HasStructure(StructureKind.RainCollector), "빗물받이 UI 건설");
            RequireInstalledStructureArt();
            Require(session.HasAxe && session.HasRope, "제작·연구 UI 경로");

            phaseButton.onClick.Invoke();
            Require(session.Phase == GamePhase.Exploring, "수색 시작 UI 경로");
            Require(nodes.Count >= 10, "10개 이상 채집 지점");
            RequireExplorationBarrierArt();
            UpdateResourceLabelLayout();
            RequireReadableResourceLabels(PrototypeLocalization.KoreanLocaleCode);
            if (!string.IsNullOrWhiteSpace(explorationScreenshotPath))
            {
                CaptureVerificationPng(explorationScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            Require(session.Phase == GamePhase.Exploring && nodes.Count >= 10, "수색 중 영어 즉시 전환");
            RequireReadableResourceLabels(PrototypeLocalization.EnglishLocaleCode);
            string keyboardExplorePrompt = localization.Format(PrototypeInputPromptKeys.Explore(PrototypeInputDevice.KeyboardMouse), localization.DeviceName(PrototypeInputDevice.KeyboardMouse), session.ActiveBagSlotCount);
            string gamepadExplorePrompt = localization.Format(PrototypeInputPromptKeys.Explore(PrototypeInputDevice.Gamepad), localization.DeviceName(PrototypeInputDevice.Gamepad), session.ActiveBagSlotCount);
            Require(keyboardExplorePrompt.Contains(localization.DeviceName(PrototypeInputDevice.KeyboardMouse)) &&
                    keyboardExplorePrompt.Contains("1–4") &&
                    gamepadExplorePrompt.Contains(localization.DeviceName(PrototypeInputDevice.Gamepad)) &&
                    gamepadExplorePrompt.Contains("D-pad+A"), "수색 키보드·게임패드 가방 조작 안내");
            NodeView waterNode = nodes.Find(node => node.Water);
            Require(waterNode != null && nodes.FindAll(node => node.Water).Count >= 2, "얕은 연안 수색 지점 2개");
            playerTraversal.Warp(PrototypePlayerTraversal.CoastlineX + 0.05f, PrototypePlayerTraversal.LandY, false);
            PrototypePlayerActions enterWater = new PrototypePlayerActions(-1f, false, false, false, false, -1);
            PrototypeTraversalStep waterEntryStep = playerTraversal.Step(enterWater, 0.1f, 0f, session);
            Require(session.IsSwimming && waterEntryStep.Presentation.IsSwimming, "해안 입수 전환");
            PrototypePlayerActions swimJump = new PrototypePlayerActions(0f, true, false, false, false, -1);
            PrototypeTraversalStep swimJumpStep = playerTraversal.Step(swimJump, 0.1f, 0.5f, session);
            Require(swimJumpStep.Presentation.IsSwimming && swimJumpStep.Presentation.IsGrounded, "수영 중 점프 금지");

            float waterDaylightBefore = session.Daylight;
            float waterEnergyBefore = session.Energy;
            PrototypePlayerPresentationState waterNodePresentation = playerTraversal.Warp(waterNode.X, PrototypePlayerTraversal.WaterY, true);
            session.TickSearch(1f, true);
            Require(waterDaylightBefore - session.Daylight > 0.9f && waterEnergyBefore - session.Energy > 0.5f, "수영의 추가 일광·체력 소모");
            playerPresentation.Apply(waterNodePresentation);
            worldCamera.transform.position = new Vector3(Mathf.Clamp(playerTraversal.X + 2.5f, -6.5f, 12.5f), 0f, -10f);
            UpdateResourceLabelLayout();
            RefreshHud();
            if (!string.IsNullOrWhiteSpace(swimmingScreenshotPath))
            {
                RequireReadableResourceLabels(PrototypeLocalization.EnglishLocaleCode);
                CaptureVerificationPng(swimmingScreenshotPath, 1280, 800);
            }

            GatherNearestNode();
            Require(waterNode.Collected, "수영 중 연안 자원 수색");
            HashSet<ResourceKind> gatheredKinds = new HashSet<ResourceKind> { waterNode.Kind };
            playerTraversal.Warp(PrototypePlayerTraversal.CoastlineX - 0.05f, PrototypePlayerTraversal.WaterY, true);
            PrototypePlayerActions leaveWater = new PrototypePlayerActions(1f, false, false, false, false, -1);
            PrototypeTraversalStep shoreReturnStep = playerTraversal.Step(leaveWater, 0.1f, 1f, session);
            Require(!session.IsSwimming && !shoreReturnStep.Presentation.IsSwimming && Mathf.Approximately(playerTraversal.Y, PrototypePlayerTraversal.LandY), "해안 이탈 전환과 육지 높이 복귀");
            playerPresentation.Apply(playerTraversal.Warp(-1.1f, PrototypePlayerTraversal.LandY, false));
            for (int i = 0; i < nodes.Count && gatheredKinds.Count < 4; i += 1)
            {
                NodeView node = nodes[i];
                if (node.Water || gatheredKinds.Contains(node.Kind))
                {
                    continue;
                }

                playerPresentation.Apply(playerTraversal.Warp(node.X, PrototypePlayerTraversal.LandY, false));
                GatherNearestNode();
                gatheredKinds.Add(node.Kind);
                if (session.HasPendingLoot)
                {
                    bagButtons[0].onClick.Invoke();
                }
            }

            Require(gatheredKinds.Count == 4, "수색 월드의 네 자원 채집");
            float daylightBeforeMovement = session.Daylight;
            float energyBeforeMovement = session.Energy;
            session.TickSearch(1f, true);
            Require(session.Daylight < daylightBeforeMovement && session.Energy < energyBeforeMovement, "이동 중 일광·체력 소모");
            worldCamera.transform.position = new Vector3(Mathf.Clamp(playerTraversal.X + 2.5f, -6.5f, 12.5f), 0f, -10f);
            UpdateResourceLabelLayout();
            RefreshHud();
            Require(languageButton.GetComponentInChildren<TMP_Text>().text == localization.Format("ui.language.switch.en"), "수색 중 언어 설정 문구 유지");

            Require(session.ReturnToCamp(false), "캠프 귀환");
            RefreshAll();
            phaseButton.onClick.Invoke();
            Require(session.Day == 2, "하루 정산 UI 경로");
            signalButton.onClick.Invoke();
            Require(session.SignalStage == 1, "구조 신호대 1단계 UI 경로");
            Require(session.Day == 2 && session.Phase == GamePhase.Camp, "2일차 캠프 상태");
            RefreshAll();
            return "PASS · 가방 4→6 원자적 확장·잠긴 슬롯·5/6 획득/중첩/교체/포기/귀환·키보드/마우스/게임패드 포커스·1280x800/1920x1080 ko/en UI, ko/en 신호대 1·2단계 요구조건·선택 가능한 부족 피드백, 채택 캠프 배경·구조물 아트·바닥선·전용 신호대 앵커, ko/en 즉시 전환·한국어 폴백·TMP 폰트, 배치·수영·장벽·제작·연구·가방·귀환·정산 회귀 확인";
        }

        private void RequireReadableBagUi()
        {
            bagTitleText.ForceMeshUpdate(true, true);
            bagUpgradeButton.GetComponentInChildren<TMP_Text>().ForceMeshUpdate(true, true);
            for (int i = 0; i < bagButtons.Count; i += 1)
            {
                bagButtons[i].GetComponentInChildren<TMP_Text>().ForceMeshUpdate(true, true);
            }
            Canvas.ForceUpdateCanvases();
            Require(!bagTitleText.isTextOverflowing, "가방 용량 문구 잘림 없음");
            Require(!bagUpgradeButton.GetComponentInChildren<TMP_Text>().isTextOverflowing, "가방 업그레이드 문구 잘림 없음");
            Require(bagButtons.TrueForAll(button => !button.GetComponentInChildren<TMP_Text>().isTextOverflowing), "2열 4/6칸 가방 라벨 잘림 없음");
        }

        private static void MoveUiSelection(MoveDirection direction)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            AxisEventData eventData = new AxisEventData(EventSystem.current)
            {
                moveDir = direction,
                moveVector = direction == MoveDirection.Down
                    ? Vector2.down
                    : direction == MoveDirection.Right ? Vector2.right : Vector2.zero
            };
            ExecuteEvents.Execute(selected, eventData, ExecuteEvents.moveHandler);
        }

        private static void SubmitUiSelection()
        {
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        private void RequireReadableSignalFeedback()
        {
            TMP_Text signalLabel = signalButton.GetComponentInChildren<TMP_Text>();
            signalLabel.ForceMeshUpdate(true, true);
            messageText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            Require(signalLabel.fontSize >= 23f && !signalLabel.isTextOverflowing, "1280x800 신호대 단계·요구조건 라벨 잘림 없음");
            Require(messageText.fontSize >= 29f && !messageText.isTextOverflowing, "1280x800 신호대 부족 사유 잘림 없음");
        }

        private void RequireReadablePlacementUi()
        {
            messageText.ForceMeshUpdate(true, true);
            controlsText.ForceMeshUpdate(true, true);
            placementGhostLabel.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            Require(messageText.fontSize >= 36f && !messageText.isTextOverflowing, "1280x800 배치 상태 카드 18px 대응·잘림 없음");
            Require(!controlsText.isTextOverflowing, "1280x800 현재 장치 조작 안내 잘림 없음");
            Require(placementGhostLabel.fontSizeMin >= 30f && placementGhostBadgeRenderer != null && placementGhostOutlineRenderers.Count == 4, "배치 유령 아트·16px 상태 배지·발자국 표현");
        }

        private void RequireCampBackgroundAlignment()
        {
            Require(campBackgroundSprite != null && campGameplayGroundSprite != null && campForegroundSprite != null &&
                    campBackgroundRenderer != null && campBackgroundRenderer.sprite == campBackgroundSprite &&
                    campGameplayGroundRenderer != null && campGameplayGroundRenderer.sprite == campGameplayGroundSprite &&
                    campForegroundRenderer != null && campForegroundRenderer.sprite == campForegroundSprite,
                "채택 캠프 3레이어 런타임 연결");
            Require(campGameplayGroundSprite.rect == campBackgroundSprite.rect &&
                    campForegroundSprite.rect == campBackgroundSprite.rect,
                "캠프 3레이어 공유 캔버스");
            Require(Mathf.Abs(campBackgroundSprite.rect.width / campBackgroundSprite.rect.height - CampCanvasWidthPixels / CampCanvasHeightPixels) < 0.001f,
                "캠프 3레이어 1672x941 원본 캔버스 비율");
            Require(campBackgroundRenderer.sortingOrder < campGameplayGroundRenderer.sortingOrder &&
                    campGameplayGroundRenderer.sortingOrder < campForegroundRenderer.sortingOrder,
                "캠프 배경→게임플레이 지면→전경 렌더 순서");
            Vector2 mappedFloor = GetCampArtPoint(0.5f, CampBackgroundGroundNormalizedY);
            Vector2 signalAnchor = GetCampArtPoint(CampSignalAnchorNormalizedX, CampSignalAnchorNormalizedY);
            Require(Mathf.Abs(mappedFloor.y - PrototypeCampPlacement.FloorY) < 0.01f, "채택 배경 지면선과 건설 바닥 정렬");
            float expectedSignalAnchorY = PrototypeCampPlacement.FloorY +
                                          (CampWalkableBaselineTopPixels - CampSignalAnchorTopPixels) * CampBackgroundWorldWidth / CampCanvasWidthPixels;
            Require(Mathf.Abs(signalAnchor.y - expectedSignalAnchorY) < 0.01f, "채택 배경 신호대 top Y=596 앵커 정렬");
            Require(signalAnchor.x > PrototypeCampPlacement.BuildMaximumX && signalAnchor.y > PrototypeCampPlacement.FloorY, "우측 바위 턱의 전용 신호대 앵커 정렬");
        }

        private void RequireCampStructureArt()
        {
            Require(campfireSprite != null && workbenchSprite != null && rainCollectorSprite != null && rescueSignalSprite != null, "채택 구조물 패키지 4종 직렬화");
            Require(rescueSignalRenderer != null && rescueSignalRenderer.sprite == rescueSignalSprite, "고정 앵커 구조 신호대 아트 연결");
            Require(vineBarrierBlockedSprite != null && vineBarrierInteractableSprite != null && vineBarrierClearedSprite != null, "채택 덩굴·나무 장벽 3상태 직렬화");
        }

        private void RequireExplorationBarrierArt()
        {
            Sprite expected = session.HasAxe ? vineBarrierClearedSprite : vineBarrierBlockedSprite;
            Require(vineBarrierRenderer != null && vineBarrierRenderer.sprite == expected, "돌도끼 보유 상태에 맞는 채택 장벽 아트 연결");
        }

        private void RequireInstalledStructureArt()
        {
            StructureKind[] kinds = { StructureKind.Campfire, StructureKind.Workbench, StructureKind.RainCollector };
            for (int i = 0; i < kinds.Length; i += 1)
            {
                StructureKind kind = kinds[i];
                GameObject view;
                Require(structureViews.TryGetValue(kind, out view), kind + " 런타임 표현 생성");
                SpriteRenderer[] renderers = view.GetComponentsInChildren<SpriteRenderer>(true);
                bool found = false;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex += 1)
                {
                    if (renderers[rendererIndex].sprite == GetStructureSprite(kind))
                    {
                        found = true;
                        break;
                    }
                }
                Require(found, kind + " 채택 스프라이트 연결");
            }
        }

        private void RequireReadableResourceLabels(string localeCode)
        {
            UpdateResourceLabelLayout();
            float halfWidth = worldCamera.orthographicSize * MinimumSupportedAspect;
            float left = worldCamera.transform.position.x - halfWidth;
            float right = worldCamera.transform.position.x + halfWidth;
            float safeRight = Mathf.Lerp(left, right, ResourceLabelSafeViewportRight);
            List<NodeView> visible = new List<NodeView>();
            for (int i = 0; i < nodes.Count; i += 1)
            {
                NodeView node = nodes[i];
                if (node.Collected || node.LabelRoot == null || !node.LabelRoot.gameObject.activeSelf || node.X < left || node.X > right)
                {
                    continue;
                }

                node.Label.ForceMeshUpdate(true, true);
                Bounds bounds = node.LabelBackground.bounds;
                Require(node.Label.font != null && node.Label.fontSizeMin >= 36f, localeCode + " 자원 라벨 18px 대응·폰트");
                Require(node.LabelBackground.color.a >= 0.95f && node.Label.color.grayscale >= 0.9f, localeCode + " 자원 라벨 배경 대비");
                Require(bounds.min.x >= left - 0.01f && bounds.max.x <= safeRight + 0.01f, localeCode + " 화면 가장자리·가방 패널 자원 라벨 클램프");
                visible.Add(node);
            }

            Require(visible.Count >= 2, localeCode + " 화면 내 자원 라벨 표본");
            for (int first = 0; first < visible.Count; first += 1)
            {
                for (int second = first + 1; second < visible.Count; second += 1)
                {
                    Require(!visible[first].LabelBackground.bounds.Intersects(visible[second].LabelBackground.bounds), localeCode + " 자원 라벨 겹침 방지");
                }
            }
        }

        public void CaptureVerificationPng(string absolutePath, int width, int height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            for (int pass = 0; pass < 2; pass += 1)
            {
                for (int i = 0; i < texts.Length; i += 1)
                {
                    texts[i].ForceMeshUpdate(true, true);
                }
            }
            Canvas.ForceUpdateCanvases();
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = worldCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            worldCamera.targetTexture = target;
            RenderTexture.active = target;
            worldCamera.Render();
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            worldCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Destroy(target);
            Destroy(image);
        }

        private void SpawnNode(float x, ResourceKind kind, int amount, bool water = false)
        {
            GameObject root = new GameObject((water ? "Water Search · " : "Gather · ") + kind);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(x, water ? -1.72f : -2.25f, 0f);
            GameObject marker = CreateRect(root.transform, "자원", Vector2.zero, new Vector2(0.95f, 0.95f), ResourceColor(kind, 1f), 4);
            marker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            float laneY = (water ? 1.18f : 1.25f) + (nodes.Count % 2) * 1.65f;
            SpriteRenderer labelBackground;
            TMP_Text label = CreateWorldBadge(
                root.transform,
                "자원 안내",
                localization.Format(water ? "world.resource.water" : "world.resource.land", kind, amount),
                new Vector2(0f, laneY),
                new Vector2(ResourceLabelWidth, ResourceLabelHeight),
                water ? new Color(0.02f, 0.16f, 0.28f, 0.96f) : new Color(0.12f, 0.1f, 0.06f, 0.96f),
                Color.white,
                out labelBackground,
                0.085f,
                36f,
                36f);
            nodes.Add(new NodeView
            {
                Kind = kind,
                Amount = amount,
                X = x,
                Water = water,
                Root = root,
                LabelRoot = label.transform.parent,
                Label = label,
                LabelBackground = labelBackground,
                Collected = false
            });
            UpdateResourceLabelLayout();
        }

        private void UpdateResourceLabelLayout()
        {
            if (worldCamera == null)
            {
                return;
            }

            float halfWidth = worldCamera.orthographicSize * MinimumSupportedAspect;
            float left = worldCamera.transform.position.x - halfWidth;
            float right = worldCamera.transform.position.x + halfWidth;
            float safeRight = Mathf.Lerp(left, right, ResourceLabelSafeViewportRight);
            float labelHalfWidth = ResourceLabelWidth * 0.5f;
            for (int i = 0; i < nodes.Count; i += 1)
            {
                NodeView node = nodes[i];
                if (node.LabelRoot == null)
                {
                    continue;
                }

                bool labelVisible = !node.Collected && node.X <= safeRight;
                node.LabelRoot.gameObject.SetActive(labelVisible);
                if (!labelVisible)
                {
                    continue;
                }

                float labelX = node.X;
                bool markerNearViewport = node.X >= left - labelHalfWidth && node.X <= right + labelHalfWidth;
                if (markerNearViewport)
                {
                    labelX = Mathf.Clamp(
                        labelX,
                        left + labelHalfWidth + ResourceLabelViewportPadding,
                        safeRight - labelHalfWidth - ResourceLabelViewportPadding);
                }

                Vector3 localPosition = node.LabelRoot.localPosition;
                localPosition.x = labelX - node.X;
                node.LabelRoot.localPosition = localPosition;
            }
        }

        private void CreateSwimWake()
        {
            GameObject root = new GameObject("수영 임시 표현 · " + AssetSwim);
            root.transform.SetParent(worldRoot, false);
            CreateRect(root.transform, "앞 물결", new Vector2(0.7f, 0f), new Vector2(1.15f, 0.12f), new Color(0.82f, 0.96f, 1f, 0.9f), 11);
            CreateRect(root.transform, "뒤 물결", new Vector2(-0.8f, -0.18f), new Vector2(1.55f, 0.1f), new Color(0.72f, 0.91f, 1f, 0.78f), 11);
            root.SetActive(false);
            playerPresentation.SetSwimWake(root.transform);
            playerPresentation.Apply(playerTraversal.CurrentPresentation(session.IsSwimming));
        }

        private void CreateReservedCampStrip(string localizationKey, float minimumX, float maximumX, Color color)
        {
            float width = maximumX - minimumX;
            float center = (minimumX + maximumX) * 0.5f;
            CreateRect(localizationKey, new Vector2(center, -2.62f), new Vector2(width, 0.62f), color, -2);
            CreateRect(localizationKey + " 왼쪽 경계", new Vector2(minimumX, -2.36f), new Vector2(0.08f, 1.08f), color, -1);
            CreateRect(localizationKey + " 오른쪽 경계", new Vector2(maximumX, -2.36f), new Vector2(0.08f, 1.08f), color, -1);
            Color badgeColor = localizationKey == "world.entrance"
                ? new Color(0.46f, 0.09f, 0.05f, 0.96f)
                : new Color(0.43f, 0.25f, 0.03f, 0.96f);
            bool entrance = localizationKey == "world.entrance";
            float badgeCenter = entrance ? -3.4f : -1.1f;
            float badgeY = entrance ? -2.8f : -3.15f;
            float badgeWidth = entrance ? 3.2f : 2.8f;
            float badgeHeight = entrance ? 2.5f : 1.65f;
            CreateWorldBadge(localizationKey + " 안내", localization.Format(localizationKey), new Vector2(badgeCenter, badgeY), new Vector2(badgeWidth, badgeHeight), badgeColor, Color.white);
        }

        private void CreatePlacedStructure(StructureKind kind, Color color)
        {
            if (!session.HasStructure(kind))
            {
                return;
            }

            campPlacement.EnsureInstalled(kind);
            Vector2 size = PrototypeCampPlacement.GetStructureSize(kind);
            Vector2 position = campPlacement.GetInstalledPosition(kind);
            GameObject structure = new GameObject(kind + " · " + AssetStructures);
            structure.transform.SetParent(worldRoot, false);
            structure.transform.position = position;
            CreateStructureVisual(structure.transform, kind, GetStructureSprite(kind), size, color, 3, out _);
            structureViews[kind] = structure;
        }

        private void CreatePlacementGhost()
        {
            Vector2 size = PrototypeCampPlacement.GetStructureSize(campPlacement.SelectedKind);
            placementGhost = new GameObject("배치 유령 · " + AssetStructures);
            placementGhost.transform.SetParent(worldRoot, false);
            placementGhost.transform.position = campPlacement.CandidatePosition;
            float visualTop;
            placementGhostRenderer = CreateStructureVisual(
                placementGhost.transform,
                campPlacement.SelectedKind,
                GetStructureSprite(campPlacement.SelectedKind),
                size,
                Color.white,
                6,
                out visualTop);
            CreateFootprintOutline(placementGhost.transform, size, Color.white, placementGhostOutlineRenderers);
            placementGhostLabel = CreateWorldBadge(
                placementGhost.transform,
                "배치 판정",
                string.Empty,
                new Vector2(0f, visualTop + 0.58f),
                new Vector2(Mathf.Max(4.35f, size.x + 0.7f), 1.65f),
                Color.black,
                Color.white,
                out placementGhostBadgeRenderer);
            UpdatePlacementGhost();
        }

        private void UpdatePlacementGhost()
        {
            if (placementGhost == null)
            {
                return;
            }

            bool valid = campPlacement.CurrentValidity == CampPlacementValidity.Valid;
            placementGhost.transform.position = campPlacement.CandidatePosition;
            Color footprintColor = valid ? new Color(0.18f, 0.92f, 0.38f, 0.58f) : new Color(1f, 0.2f, 0.12f, 0.7f);
            Color outlineColor = valid ? new Color(0.72f, 1f, 0.66f, 1f) : new Color(1f, 0.78f, 0.68f, 1f);
            placementGhostRenderer.color = footprintColor;
            for (int i = 0; i < placementGhostOutlineRenderers.Count; i += 1)
            {
                placementGhostOutlineRenderers[i].color = outlineColor;
            }
            placementGhostBadgeRenderer.color = valid
                ? new Color(0.03f, 0.34f, 0.15f, 0.98f)
                : new Color(0.5f, 0.05f, 0.04f, 0.98f);
            placementGhostLabel.text = localization.Format(valid ? "world.placement.valid" : "world.placement.invalid", campPlacement.SelectedKind);
            placementGhostLabel.color = Color.white;
        }

        private Sprite GetStructureSprite(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Campfire:
                    return campfireSprite;
                case StructureKind.Workbench:
                    return workbenchSprite;
                case StructureKind.RainCollector:
                    return rainCollectorSprite;
                default:
                    return null;
            }
        }

        private GameObject CreateVineBarrier()
        {
            GameObject root = new GameObject("덩굴·나무 장벽 · " + AssetVineBarrier);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(8.7f, -2.35f, 0f);

            Sprite sprite = session.HasAxe ? vineBarrierClearedSprite : vineBarrierBlockedSprite;
            if (sprite == null || sprite.bounds.size.x <= 0f)
            {
                Color fallbackColor = session.HasAxe ? new Color(0.25f, 0.7f, 0.3f, 0.35f) : new Color(0.2f, 0.42f, 0.17f, 0.95f);
                GameObject fallback = CreateRect(root.transform, "장벽 아트 누락", new Vector2(0f, 1.6f), new Vector2(1.25f, 5f), fallbackColor, 1);
                vineBarrierRenderer = fallback.GetComponent<SpriteRenderer>();
                return root;
            }

            GameObject visual = new GameObject(session.HasAxe ? "제거된 장벽 아트" : "막힌 장벽 아트");
            visual.transform.SetParent(root.transform, false);
            float scale = 4.1f / sprite.bounds.size.x;
            visual.transform.localScale = new Vector3(scale, scale, 1f);
            vineBarrierRenderer = visual.AddComponent<SpriteRenderer>();
            vineBarrierRenderer.sprite = sprite;
            vineBarrierRenderer.color = Color.white;
            vineBarrierRenderer.sortingOrder = 1;
            return root;
        }

        private SpriteRenderer CreateStructureVisual(
            Transform parent,
            StructureKind kind,
            Sprite sprite,
            Vector2 footprint,
            Color fallbackColor,
            int sortingOrder,
            out float visualTop)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f)
            {
                GameObject fallback = CreateRect(parent, "설비 아트 누락", Vector2.zero, footprint, fallbackColor, sortingOrder);
                visualTop = footprint.y * 0.5f;
                return fallback.GetComponent<SpriteRenderer>();
            }

            GameObject visual = new GameObject("채택 구조물 아트 · " + kind);
            visual.transform.SetParent(parent, false);
            float scale = (footprint.x + 0.25f) / sprite.bounds.size.x;
            visual.transform.localPosition = new Vector3(0f, -footprint.y * 0.5f, 0f);
            visual.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            visualTop = visual.transform.localPosition.y + sprite.bounds.max.y * scale;
            return renderer;
        }

        private void CreateRescueSignal(Vector2 anchor)
        {
            Color stageColor = session.SignalStage == 0
                ? new Color(0.55f, 0.6f, 0.58f, 0.58f)
                : session.SignalStage == 1
                    ? new Color(1f, 0.82f, 0.56f, 0.9f)
                    : Color.white;
            if (rescueSignalSprite == null || rescueSignalSprite.bounds.size.x <= 0f)
            {
                GameObject fallback = CreateRect("구조 신호대 아트 누락", new Vector2(anchor.x, anchor.y + 2.05f), new Vector2(0.45f, 4.1f), stageColor, 2);
                rescueSignalRenderer = fallback.GetComponent<SpriteRenderer>();
                return;
            }

            GameObject signal = new GameObject("채택 구조 신호대 아트 · " + AssetStructures);
            signal.transform.SetParent(worldRoot, false);
            signal.transform.localPosition = anchor;
            float scale = 2.25f / rescueSignalSprite.bounds.size.x;
            signal.transform.localScale = new Vector3(scale, scale, 1f);
            rescueSignalRenderer = signal.AddComponent<SpriteRenderer>();
            rescueSignalRenderer.sprite = rescueSignalSprite;
            rescueSignalRenderer.color = stageColor;
            rescueSignalRenderer.sortingOrder = 2;
        }

        private void CreateKim(Vector2 position)
        {
            GameObject root = new GameObject("김씨 · " + AssetKim);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = position;
            playerRoot = root.transform;

            GameObject visual = new GameObject("김씨 표현 · " + AssetSwim);
            visual.transform.SetParent(playerRoot, false);
            bool placeholderPose = playerVisualPrefab == null;
            if (placeholderPose)
            {
                CreateRect(visual.transform, "몸", new Vector2(0f, 0.55f), new Vector2(0.85f, 1.35f), new Color(0.96f, 0.48f, 0.16f), 8);
                CreateRect(visual.transform, "배낭", new Vector2(-0.46f, 0.52f), new Vector2(0.38f, 0.85f), new Color(0.22f, 0.36f, 0.27f), 7);
                CreateCircle(visual.transform, "머리", new Vector2(0f, 1.47f), 0.78f, new Color(0.94f, 0.72f, 0.54f), 9);
                CreateRect(visual.transform, "머리카락", new Vector2(0f, 1.77f), new Vector2(0.78f, 0.25f), new Color(0.11f, 0.08f, 0.06f), 10);
            }
            else
            {
                Instantiate(playerVisualPrefab, visual.transform, false);
            }

            playerPresentation = root.AddComponent<PrototypePlayerPresentation>();
            playerPresentation.Configure(visual.transform, placeholderPose);
            PrototypePlayerPresentationState initialPresentation = session.Phase == GamePhase.Exploring
                ? playerTraversal.CurrentPresentation(session.IsSwimming)
                : new PrototypePlayerPresentationState(position.x, position.y, 1f, 0f, false, true);
            playerPresentation.Apply(initialPresentation);
        }

        private void CreatePalm(Vector2 position, float scale)
        {
            GameObject root = new GameObject("야자수");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = position;
            CreateRect(root.transform, "줄기", new Vector2(0f, 1.25f * scale), new Vector2(0.38f * scale, 3f * scale), new Color(0.43f, 0.25f, 0.1f), -1);
            for (int i = 0; i < 5; i += 1)
            {
                GameObject leaf = CreateRect(root.transform, "잎", new Vector2(0f, 2.65f * scale), new Vector2(2.3f * scale, 0.34f * scale), new Color(0.14f, 0.55f, 0.22f), 0);
                leaf.transform.localRotation = Quaternion.Euler(0f, 0f, i * 36f);
            }
        }

        private void CreateSun(Vector2 position)
        {
            GameObject sun = new GameObject("해");
            sun.transform.SetParent(worldRoot, false);
            sun.transform.position = position;
            SpriteRenderer renderer = sun.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(1f, 0.88f, 0.35f);
            renderer.sortingOrder = -8;
            sun.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            sun.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private GameObject CreateRect(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            return CreateRect(worldRoot, name, position, size, color, order);
        }

        private GameObject CreateRect(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(position.x, position.y, 0f);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return item;
        }

        private void CreateCircle(Transform parent, string name, Vector2 position, float size, Color color, int order)
        {
            GameObject item = CreateRect(parent, name, position, new Vector2(size, size), color, order);
            item.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void CreateFootprintOutline(Transform parent, Vector2 size, Color color, List<SpriteRenderer> output)
        {
            CreateFootprintOutline(parent, size, color, output, Vector2.zero);
        }

        private void CreateFootprintOutline(Transform parent, Vector2 size, Color color, List<SpriteRenderer> output, Vector2 center)
        {
            const float thickness = 0.09f;
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            GameObject[] edges =
            {
                CreateRect(parent, "발자국 위", center + new Vector2(0f, halfHeight), new Vector2(size.x + thickness, thickness), color, 8),
                CreateRect(parent, "발자국 아래", center + new Vector2(0f, -halfHeight), new Vector2(size.x + thickness, thickness), color, 8),
                CreateRect(parent, "발자국 왼쪽", center + new Vector2(-halfWidth, 0f), new Vector2(thickness, size.y + thickness), color, 8),
                CreateRect(parent, "발자국 오른쪽", center + new Vector2(halfWidth, 0f), new Vector2(thickness, size.y + thickness), color, 8)
            };

            if (output == null)
            {
                return;
            }

            for (int i = 0; i < edges.Length; i += 1)
            {
                output.Add(edges[i].GetComponent<SpriteRenderer>());
            }
        }

        private TMP_Text CreateWorldBadge(string name, string value, Vector2 position, Vector2 size, Color background, Color foreground)
        {
            SpriteRenderer unused;
            return CreateWorldBadge(worldRoot, name, value, position, size, background, foreground, out unused);
        }

        private TMP_Text CreateWorldBadge(
            Transform parent,
            string name,
            string value,
            Vector2 position,
            Vector2 size,
            Color background,
            Color foreground,
            out SpriteRenderer backgroundRenderer,
            float textScale = 0.084f,
            float minimumFontSize = 30f,
            float maximumFontSize = 32f)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(position.x, position.y, -0.15f);
            GameObject backgroundObject = CreateRect(root.transform, "안내 배경", Vector2.zero, size, background, 18);
            backgroundRenderer = backgroundObject.GetComponent<SpriteRenderer>();

            GameObject labelObject = new GameObject("안내 문구");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            float localizedTextScale = textScale * localization.CurrentWorldTextScale;
            labelObject.transform.localScale = Vector3.one * localizedTextScale;
            TextMeshPro mesh = labelObject.AddComponent<TextMeshPro>();
            mesh.text = value;
            mesh.fontSize = maximumFontSize;
            mesh.enableAutoSizing = true;
            mesh.fontSizeMin = minimumFontSize;
            mesh.fontSizeMax = maximumFontSize;
            mesh.fontStyle = FontStyles.Bold;
            mesh.alignment = TextAlignmentOptions.Center;
            mesh.textWrappingMode = TextWrappingModes.Normal;
            mesh.overflowMode = TextOverflowModes.Overflow;
            mesh.color = foreground;
            mesh.rectTransform.sizeDelta = new Vector2((size.x - 0.18f) / localizedTextScale, (size.y - 0.12f) / localizedTextScale);
            localization.Register(mesh);
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 20;
            return mesh;
        }

        private TMP_Text CreateWorldLabel(Transform parent, string value, Vector3 localPosition, int size, Color color)
        {
            GameObject labelObject = new GameObject("라벨");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localScale = Vector3.one * 0.035f;
            TextMeshPro mesh = labelObject.AddComponent<TextMeshPro>();
            mesh.text = value;
            mesh.fontSize = size;
            mesh.alignment = TextAlignmentOptions.Center;
            mesh.textWrappingMode = TextWrappingModes.NoWrap;
            mesh.overflowMode = TextOverflowModes.Overflow;
            mesh.color = color;
            mesh.rectTransform.sizeDelta = new Vector2(80f, 12f);
            localization.Register(mesh);
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 20;
            return mesh;
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static void ConfigureLayout(GameObject target, float flexibleWidth, float flexibleHeight, float minimumWidth, float minimumHeight)
        {
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            element.minWidth = minimumWidth;
            element.minHeight = minimumHeight;
        }

        private TMP_Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = ConvertAlignment(alignment);
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            localization.Register(text);
            return text;
        }

        private Button CreateActionButton(Transform parent, int index, string label, UnityEngine.Events.UnityAction callback)
        {
            if (index == 7)
            {
                return CreateButton("행동 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), label, callback, new Vector2(20f, -452f), new Vector2(710f, -354f));
            }

            if (index >= 6)
            {
                int compactColumn = index == 6 ? 0 : index - 7;
                float compactLeft = 20f + compactColumn * 230f;
                float compactRight = compactColumn == 2 ? 710f : compactLeft + 215f;
                return CreateButton("행동 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), label, callback, new Vector2(compactLeft, -538f), new Vector2(compactRight, -458f));
            }

            int column = index % 2;
            int row = index / 2;
            float left = 20f + column * 355f;
            float top = -82f - row * 94f;
            return CreateButton("행동 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), label, callback, new Vector2(left, top - 80f), new Vector2(left + 335f, top));
        }

        private Button CreateBagButton(Transform parent, int index, UnityEngine.Events.UnityAction callback)
        {
            int column = index % 2;
            int row = index / 2;
            float left = 22f + column * 196f;
            float right = left + 185f;
            float top = -222f - row * 105f;
            return CreateButton("가방 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), localization.Format("bag.slot.empty", index + 1), callback, new Vector2(left, top - 88f), new Vector2(right, top));
        }

        private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string label, UnityEngine.Events.UnityAction callback)
        {
            return CreateButton(name, parent, anchorMin, anchorMax, label, callback, Vector2.zero, Vector2.zero);
        }

        private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string label, UnityEngine.Events.UnityAction callback, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.34f, 0.29f, 0.98f);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.34f, 0.29f, 1f);
            colors.highlightedColor = new Color(0.28f, 0.58f, 0.38f, 1f);
            colors.selectedColor = new Color(0.93f, 0.63f, 0.18f, 1f);
            colors.pressedColor = new Color(1f, 0.75f, 0.2f, 1f);
            colors.disabledColor = new Color(0.13f, 0.16f, 0.15f, 0.72f);
            button.colors = colors;
            button.onClick.AddListener(callback);
            TMP_Text text = CreateText("라벨", buttonObject.transform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f), 23, TextAnchor.MiddleCenter, Color.white);
            text.text = label;
            return button;
        }

        private static void SetButton(Button button, string label, bool interactable)
        {
            button.GetComponentInChildren<TMP_Text>().text = label;
            button.interactable = interactable;
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }

        private static Color ResourceColor(ResourceKind kind, float alpha)
        {
            switch (kind)
            {
                case ResourceKind.Wood:
                    return new Color(0.55f, 0.3f, 0.12f, alpha);
                case ResourceKind.Stone:
                    return new Color(0.48f, 0.52f, 0.55f, alpha);
                case ResourceKind.Food:
                    return new Color(0.35f, 0.72f, 0.25f, alpha);
                case ResourceKind.Salvage:
                    return new Color(0.95f, 0.57f, 0.16f, alpha);
                default:
                    return Color.white;
            }
        }

        private static void Require(bool condition, string label)
        {
            if (!condition)
            {
                throw new InvalidOperationException("자동 검증 실패: " + label);
            }
        }

        private static Sprite MakeSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Runtime Placeholder Pixel";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
