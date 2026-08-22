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
        private const string AssetHud = "ui.survival-hud";
        private const string AssetIcons = "icon.resource-tool-set";
        private const string AssetComedy = "effect.comedy-feedback";

        [SerializeField] private GameObject playerVisualPrefab;

        private sealed class NodeView
        {
            public ResourceKind Kind;
            public int Amount;
            public float X;
            public bool Water;
            public GameObject Root;
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

            RectTransform top = CreatePanel("상태 HUD", canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -118f), new Vector2(-24f, -20f), new Color(0.05f, 0.09f, 0.12f, 0.92f));
            statusText = CreateText("날짜·상태", top, new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Vector2(24f, 8f), new Vector2(-8f, -8f), 30, TextAnchor.MiddleLeft, Color.white);
            resourceText = CreateText("보유 자원", top, new Vector2(0.55f, 0f), new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(-24f, -8f), 27, TextAnchor.MiddleRight, new Color(1f, 0.9f, 0.52f));

            RectTransform message = CreatePanel("김씨 독백 · 배치 상태 · " + AssetComedy, canvas.transform, new Vector2(0.2f, 0.74f), new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.07f, 0.88f));
            messagePanelImage = message.GetComponent<Image>();
            messageText = CreateText("김씨 독백 또는 배치 상태", message, Vector2.zero, Vector2.one, new Vector2(26f, 10f), new Vector2(-26f, -10f), 29, TextAnchor.MiddleCenter, Color.white);

            RectTransform controlPanel = CreatePanel("조작 안내", canvas.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 20f), new Vector2(-24f, 103f), new Color(0.05f, 0.09f, 0.12f, 0.92f));
            controlsText = CreateText("조작", controlPanel, Vector2.zero, new Vector2(0.81f, 1f), new Vector2(22f, 4f), new Vector2(-10f, -4f), 25, TextAnchor.MiddleCenter, Color.white);
            languageButton = CreateButton("언어 설정", controlPanel, new Vector2(0.82f, 0.12f), new Vector2(0.985f, 0.88f), string.Empty, delegate { localization.CycleLocale(); });

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
            eatButton = CreateActionButton(campActions.transform, 8, string.Empty, delegate { session.UseFood(); RefreshAll(); });
            phaseButton = CreateActionButton(campActions.transform, 9, string.Empty, HandlePhaseButton);

            bagPanel = CreatePanel("가방 · " + AssetIcons, canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-455f, 130f), new Vector2(-30f, 715f), new Color(0.09f, 0.11f, 0.12f, 0.92f)).gameObject;
            bagTitleText = CreateText("가방 제목", bagPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -72f), new Vector2(-18f, -12f), 32, TextAnchor.MiddleCenter, new Color(1f, 0.91f, 0.5f));
            for (int i = 0; i < GameSession.BagSlotCount; i += 1)
            {
                int capturedIndex = i;
                Button slot = CreateBagButton(bagPanel.transform, i, delegate { session.ReplaceBagSlot(capturedIndex); RefreshAll(); });
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
            messageText.fontSize = 29f;
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
                bagTitleText.text = localization.Format("bag.camp");
            }
            else if (session.Phase == GamePhase.Exploring)
            {
                controlsText.text = localization.Format("controls.explore", device);
                bagTitleText.text = localization.Format(session.HasPendingLoot ? "bag.pending" : "bag.exploring");
            }

            RefreshBagButtons();
        }

        private void ApplyPlacementGuidance(PrototypeInputDevice device)
        {
            bool valid = campPlacement.CurrentValidity == CampPlacementValidity.Valid;
            string state = localization.Format(valid ? "placement.state.valid" : "placement.state.invalid");
            messageText.text = localization.Format("placement.summary", state, localization.Format(campPlacement.CurrentFeedback));
            messageText.fontSize = 32f;
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
            SetButton(signalButton, session.SignalStage >= 2 ? localization.Format("button.signal.done") : localization.Format("button.signal.progress", session.SignalStage), available && session.CanUpgradeSignal());
            SetButton(eatButton, localization.Format("button.eat", session.GetStorage(ResourceKind.Food)), available && session.GetStorage(ResourceKind.Food) > 0 && session.Hunger < 100f);
            string phaseButtonKey = session.ExpeditionCompleted ? (session.Day >= GameSession.FinalDay ? "button.day.final" : "button.day.next") : "button.search.start";
            SetButton(phaseButton, localization.Format(phaseButtonKey), available);
        }

        private void RefreshBagButtons()
        {
            for (int i = 0; i < bagButtons.Count; i += 1)
            {
                BagStack stack = session.GetBagSlot(i);
                TMP_Text label = bagButtons[i].GetComponentInChildren<TMP_Text>();
                label.text = stack.IsEmpty
                    ? localization.Format("bag.slot.empty", i + 1)
                    : localization.Format("bag.slot.stack", i + 1, stack.Kind, stack.Amount);
                bagButtons[i].interactable = session.Phase == GamePhase.Exploring && session.HasPendingLoot;
                Image image = bagButtons[i].GetComponent<Image>();
                image.color = stack.IsEmpty ? new Color(0.18f, 0.22f, 0.22f, 0.95f) : ResourceColor(stack.Kind, 0.95f);
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
            CreateRect("하늘 · " + AssetCampBackground, new Vector2(0f, 1.4f), new Vector2(20f, 8.5f), new Color(0.36f, 0.77f, 0.9f), -20);
            CreateRect("바다", new Vector2(0f, -1.2f), new Vector2(20f, 2.7f), new Color(0.13f, 0.55f, 0.75f), -15);
            CreateRect("모래", new Vector2(0f, -3.25f), new Vector2(20f, 1.9f), new Color(0.91f, 0.75f, 0.43f), -10);
            float buildWidth = PrototypeCampPlacement.BuildMaximumX - PrototypeCampPlacement.BuildMinimumX;
            float buildCenter = (PrototypeCampPlacement.BuildMinimumX + PrototypeCampPlacement.BuildMaximumX) * 0.5f;
            CreateRect("호환 건설 구역", new Vector2(buildCenter, -2.68f), new Vector2(buildWidth, 0.42f), new Color(0.16f, 0.72f, 0.38f, 0.62f), -5);
            CreateRect("건설 구역 왼쪽 경계", new Vector2(PrototypeCampPlacement.BuildMinimumX, -2.38f), new Vector2(0.1f, 1.02f), new Color(0.75f, 1f, 0.72f, 0.92f), -3);
            CreateRect("건설 구역 오른쪽 경계", new Vector2(PrototypeCampPlacement.BuildMaximumX, -2.38f), new Vector2(0.1f, 1.02f), new Color(0.75f, 1f, 0.72f, 0.92f), -3);
            CreateWorldBadge("호환 건설 구역 안내", localization.Format("world.build_zone"), new Vector2(2.85f, -3.45f), new Vector2(3.6f, 0.68f), new Color(0.04f, 0.25f, 0.13f, 0.96f), Color.white);
            CreateReservedCampStrip("world.entrance", PrototypeCampPlacement.EntranceMinimumX, PrototypeCampPlacement.EntranceMaximumX, new Color(0.95f, 0.38f, 0.18f, 0.72f));
            CreateReservedCampStrip("world.required_path", PrototypeCampPlacement.RequiredPathMinimumX, PrototypeCampPlacement.RequiredPathMaximumX, new Color(1f, 0.72f, 0.16f, 0.72f));
            CreateSun(new Vector2(6.9f, 3.55f));
            CreatePalm(new Vector2(-7.1f, -2.25f), 1.2f);
            CreatePalm(new Vector2(7.5f, -2.35f), 0.9f);
            CreateKim(new Vector2(-5f, -2.18f));

            CreatePlacedStructure(StructureKind.Campfire, new Color(1f, 0.43f, 0.14f));
            CreatePlacedStructure(StructureKind.Workbench, new Color(0.48f, 0.26f, 0.12f));
            CreatePlacedStructure(StructureKind.RainCollector, new Color(0.27f, 0.7f, 0.86f));

            Color signalColor = session.SignalStage == 0 ? new Color(0.38f, 0.42f, 0.4f, 0.55f) : session.SignalStage == 1 ? new Color(0.86f, 0.5f, 0.16f) : new Color(1f, 0.88f, 0.2f);
            CreateRect("구조 신호대 · " + AssetStructures, new Vector2(6.1f, -1.2f), new Vector2(0.45f, session.SignalStage == 0 ? 2.7f : 4.1f), signalColor, 2);
            CreateWorldBadge("구조 신호대 전용 앵커 안내", localization.Format("world.signal_anchor", session.SignalStage), new Vector2(6.45f, -3.45f), new Vector2(3f, 0.68f), new Color(0.16f, 0.17f, 0.18f, 0.96f), new Color(1f, 0.88f, 0.38f));
            if (campPlacement.IsActive)
            {
                CreatePlacementGhost();
            }
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

            Color barrierColor = session.HasRope ? new Color(0.25f, 0.7f, 0.3f, 0.35f) : new Color(0.2f, 0.42f, 0.17f, 0.95f);
            GameObject barrier = CreateRect("밧줄 필요 숲길", new Vector2(8.7f, -0.75f), new Vector2(1.25f, 5f), barrierColor, 1);
            CreateWorldLabel(barrier.transform, localization.Format(session.HasRope ? "world.rope.pass" : "world.rope.need"), new Vector3(0f, 2.9f, -0.1f), 38, Color.black);

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
            PrototypePlayerActions actions = playerInput.ReadActions(session.HasPendingLoot);
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
                messageText.text = localization.Format("message.rope.blocked");
            }

            playerPresentation.Apply(traversalStep.Presentation);

            float targetCameraX = Mathf.Clamp(playerTraversal.X + 2.5f, -6.5f, 12.5f);
            Vector3 cameraPosition = worldCamera.transform.position;
            cameraPosition.x = Mathf.Lerp(cameraPosition.x, targetCameraX, Time.deltaTime * 4f);
            worldCamera.transform.position = cameraPosition;

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

        public string RunAutomatedVerification(string explorationScreenshotPath, string swimmingScreenshotPath, string placementKoreanScreenshotPath, string placementEnglishScreenshotPath)
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
            Require(session.HasAxe && session.HasRope, "제작·연구 UI 경로");

            phaseButton.onClick.Invoke();
            Require(session.Phase == GamePhase.Exploring, "수색 시작 UI 경로");
            Require(nodes.Count >= 10, "10개 이상 채집 지점");
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
            RefreshHud();
            if (!string.IsNullOrWhiteSpace(swimmingScreenshotPath))
            {
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
            RefreshHud();
            if (!string.IsNullOrWhiteSpace(explorationScreenshotPath))
            {
                Require(languageButton.GetComponentInChildren<TMP_Text>().text == localization.Format("ui.language.switch.ko"), "수색 중 언어 설정 문구 유지");
                CaptureVerificationPng(explorationScreenshotPath, 1280, 800);
            }

            Require(session.ReturnToCamp(false), "캠프 귀환");
            RefreshAll();
            phaseButton.onClick.Invoke();
            Require(session.Day == 2, "하루 정산 UI 경로");
            signalButton.onClick.Invoke();
            Require(session.SignalStage == 1, "구조 신호대 1단계 UI 경로");
            Require(session.Day == 2 && session.Phase == GamePhase.Camp, "2일차 캠프 상태");
            RefreshAll();
            return "PASS · ko/en 즉시 전환·한국어 폴백·TMP 폰트, 1280x800 배치 상태 카드·월드 배지·장치별 안내·비가림 패널, UI 자유 배치·유령·경계/겹침/출입구/통로·무료 재배치, 제작·연구, 10개 수색 지점, 해안 입수·수영 점프 금지·수영 비용·연안 채집·육지 복귀, 월드 이동·4종 채집, 가방·귀환·정산·전용 신호대 확인";
        }

        private void RequireReadablePlacementUi()
        {
            messageText.ForceMeshUpdate(true, true);
            controlsText.ForceMeshUpdate(true, true);
            placementGhostLabel.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            Require(messageText.fontSize >= 32f && !messageText.isTextOverflowing, "1280x800 배치 상태 카드 잘림 없음");
            Require(!controlsText.isTextOverflowing, "1280x800 현재 장치 조작 안내 잘림 없음");
            Require(placementGhostBadgeRenderer != null && placementGhostOutlineRenderers.Count == 4, "배치 유령 발자국·상태 배지 표현");
        }

        public void CaptureVerificationPng(string absolutePath, int width, int height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            CreateWorldLabel(root.transform, localization.Format(water ? "world.resource.water" : "world.resource.land", kind, amount), new Vector3(0f, 0.9f, -0.1f), 42, water ? Color.white : Color.black);
            nodes.Add(new NodeView { Kind = kind, Amount = amount, X = x, Water = water, Root = root, Collected = false });
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
            CreateWorldBadge(localizationKey + " 안내", localization.Format(localizationKey), new Vector2(center, -3.45f), new Vector2(2f, 0.68f), badgeColor, Color.white);
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
            CreateRect(structure.transform, "임시 설비 실루엣", Vector2.zero, size, color, 3);
            CreateFootprintOutline(structure.transform, size, new Color(1f, 0.94f, 0.63f, 0.95f), null);
            CreateWorldBadge(
                structure.transform,
                kind + " 재배치 안내",
                localization.Format("world.structure.relocate", kind),
                new Vector2(0f, size.y * 0.5f + 0.56f),
                new Vector2(Mathf.Max(2.65f, size.x + 0.5f), 0.88f),
                new Color(0.07f, 0.1f, 0.09f, 0.95f),
                Color.white,
                out _);
            structureViews[kind] = structure;
        }

        private void CreatePlacementGhost()
        {
            Vector2 size = PrototypeCampPlacement.GetStructureSize(campPlacement.SelectedKind);
            placementGhost = new GameObject("배치 유령 · " + AssetStructures);
            placementGhost.transform.SetParent(worldRoot, false);
            placementGhost.transform.position = campPlacement.CandidatePosition;
            GameObject fill = CreateRect(placementGhost.transform, "설비 발자국", Vector2.zero, size, Color.white, 6);
            placementGhostRenderer = fill.GetComponent<SpriteRenderer>();
            CreateFootprintOutline(placementGhost.transform, size, Color.white, placementGhostOutlineRenderers);
            placementGhostLabel = CreateWorldBadge(
                placementGhost.transform,
                "배치 판정",
                string.Empty,
                new Vector2(0f, size.y * 0.5f + 0.58f),
                new Vector2(Mathf.Max(3.15f, size.x + 0.7f), 0.88f),
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
            const float thickness = 0.09f;
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            GameObject[] edges =
            {
                CreateRect(parent, "발자국 위", new Vector2(0f, halfHeight), new Vector2(size.x + thickness, thickness), color, 8),
                CreateRect(parent, "발자국 아래", new Vector2(0f, -halfHeight), new Vector2(size.x + thickness, thickness), color, 8),
                CreateRect(parent, "발자국 왼쪽", new Vector2(-halfWidth, 0f), new Vector2(thickness, size.y + thickness), color, 8),
                CreateRect(parent, "발자국 오른쪽", new Vector2(halfWidth, 0f), new Vector2(thickness, size.y + thickness), color, 8)
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

        private TMP_Text CreateWorldBadge(Transform parent, string name, string value, Vector2 position, Vector2 size, Color background, Color foreground, out SpriteRenderer backgroundRenderer)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(position.x, position.y, -0.15f);
            GameObject backgroundObject = CreateRect(root.transform, "안내 배경", Vector2.zero, size, background, 18);
            backgroundRenderer = backgroundObject.GetComponent<SpriteRenderer>();

            GameObject labelObject = new GameObject("안내 문구");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            const float textScale = 0.075f;
            labelObject.transform.localScale = Vector3.one * textScale;
            TextMeshPro mesh = labelObject.AddComponent<TextMeshPro>();
            mesh.text = value;
            mesh.fontSize = 30f;
            mesh.enableAutoSizing = true;
            mesh.fontSizeMin = 17f;
            mesh.fontSizeMax = 30f;
            mesh.fontStyle = FontStyles.Bold;
            mesh.alignment = TextAlignmentOptions.Center;
            mesh.textWrappingMode = TextWrappingModes.Normal;
            mesh.overflowMode = TextOverflowModes.Overflow;
            mesh.color = foreground;
            mesh.rectTransform.sizeDelta = new Vector2((size.x - 0.18f) / textScale, (size.y - 0.12f) / textScale);
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
            int column = index % 2;
            int row = index / 2;
            float left = 20f + column * 355f;
            float top = -82f - row * 94f;
            return CreateButton("행동 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), label, callback, new Vector2(left, top - 80f), new Vector2(left + 335f, top));
        }

        private Button CreateBagButton(Transform parent, int index, UnityEngine.Events.UnityAction callback)
        {
            float top = -100f - index * 105f;
            return CreateButton("가방 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), localization.Format("bag.slot.empty", index + 1), callback, new Vector2(22f, top - 84f), new Vector2(403f, top));
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
