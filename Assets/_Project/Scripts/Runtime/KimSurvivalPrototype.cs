using System;
using System.Collections.Generic;
using System.IO;
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
        private const float CoastlineX = -4.2f;
        private const float LandY = -2.15f;
        private const float WaterY = -1.88f;

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
        private readonly Dictionary<StructureKind, GameObject> structureViews = new Dictionary<StructureKind, GameObject>();

        private GameSession session;
        private Camera worldCamera;
        private Canvas canvas;
        private Font font;
        private Sprite squareSprite;
        private Transform worldRoot;
        private Transform playerRoot;
        private Transform swimWakeRoot;
        private Text statusText;
        private Text resourceText;
        private Text messageText;
        private Text controlsText;
        private Text bagTitleText;
        private GameObject campActions;
        private GameObject bagPanel;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultDetailText;
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
        private GamePhase renderedPhase;
        private string activeDevice = "키보드·마우스";
        private float playerX;
        private float playerY;
        private float verticalVelocity;
        private bool grounded = true;
        private bool barrierMessageShown;

        public GameSession Session
        {
            get { return session; }
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            session = new GameSession();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            squareSprite = MakeSquareSprite();
            BuildCamera();
            BuildEventSystem();
            BuildUi();
            renderedPhase = (GamePhase)(-1);
            RefreshAll();
        }

        private void Update()
        {
            DetectActiveDevice();
            if (session.Phase == GamePhase.Exploring)
            {
                UpdateExploration();
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

            RectTransform message = CreatePanel("김씨 독백 · " + AssetComedy, canvas.transform, new Vector2(0.22f, 0.77f), new Vector2(0.78f, 0.89f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.07f, 0.84f));
            messageText = CreateText("김씨 독백", message, Vector2.zero, Vector2.one, new Vector2(22f, 8f), new Vector2(-22f, -8f), 28, TextAnchor.MiddleCenter, Color.white);

            RectTransform controlPanel = CreatePanel("조작 안내", canvas.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 20f), new Vector2(-24f, 103f), new Color(0.05f, 0.09f, 0.12f, 0.92f));
            controlsText = CreateText("조작", controlPanel, Vector2.zero, Vector2.one, new Vector2(22f, 4f), new Vector2(-22f, -4f), 25, TextAnchor.MiddleCenter, Color.white);

            campActions = CreatePanel("캠프 행동", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 130f), new Vector2(765f, 715f), new Color(0.06f, 0.12f, 0.11f, 0.91f)).gameObject;
            Text actionTitle = CreateText("캠프 행동 제목", campActions.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -64f), new Vector2(-18f, -12f), 32, TextAnchor.MiddleLeft, new Color(1f, 0.91f, 0.5f));
            actionTitle.text = "베이스캠프 · 제작 / 건설 / 연구";

            campfireButton = CreateActionButton(campActions.transform, 0, "모닥불 건설", delegate { session.TryBuild(StructureKind.Campfire); RefreshAll(); });
            workbenchButton = CreateActionButton(campActions.transform, 1, "작업대 건설", delegate { session.TryBuild(StructureKind.Workbench); RefreshAll(); });
            rainButton = CreateActionButton(campActions.transform, 2, "빗물받이 건설", delegate { session.TryBuild(StructureKind.RainCollector); RefreshAll(); });
            researchAxeButton = CreateActionButton(campActions.transform, 3, "돌도끼 연구", delegate { session.TryResearch(TechKind.StoneAxe); RefreshAll(); });
            craftAxeButton = CreateActionButton(campActions.transform, 4, "돌도끼 제작", delegate { session.TryCraft(TechKind.StoneAxe); RefreshAll(); });
            researchRopeButton = CreateActionButton(campActions.transform, 5, "밧줄 연구", delegate { session.TryResearch(TechKind.Rope); RefreshAll(); });
            craftRopeButton = CreateActionButton(campActions.transform, 6, "밧줄 제작", delegate { session.TryCraft(TechKind.Rope); RefreshAll(); });
            signalButton = CreateActionButton(campActions.transform, 7, "구조 신호대", delegate { session.TryUpgradeSignal(); RefreshAll(); });
            eatButton = CreateActionButton(campActions.transform, 8, "식량 먹기", delegate { session.UseFood(); RefreshAll(); });
            phaseButton = CreateActionButton(campActions.transform, 9, "수색 출발", HandlePhaseButton);

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
            restartButton = CreateButton("다시 시작", resultPanel.transform, new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.24f), "다시 시작", delegate { session.Reset(); RefreshAll(); });
        }

        private void HandlePhaseButton()
        {
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
            bool camp = session.Phase == GamePhase.Camp;
            bool result = session.Phase == GamePhase.Result;
            campActions.SetActive(camp);
            bagPanel.SetActive(!result);
            resultPanel.SetActive(result);
            if (result)
            {
                resultTitleText.text = session.ResultTitle();
                resultDetailText.text = session.ResultDetail();
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
            else if (camp)
            {
                UpdateCampButtons();
                EventSystem.current.SetSelectedGameObject(phaseButton.gameObject);
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
            string phaseName = session.Phase == GamePhase.Camp ? (session.ExpeditionCompleted ? "귀환 후 정비" : "캠프 준비") : session.Phase == GamePhase.Exploring ? (session.IsSwimming ? "얕은 연안 수영" : "섬 수색") : "결과";
            statusText.text = "DAY " + session.Day + "/" + GameSession.FinalDay + "  ·  " + phaseName + "\n허기 " + Mathf.RoundToInt(session.Hunger) + "  |  체력 " + Mathf.RoundToInt(session.Energy) + (session.Phase == GamePhase.Exploring ? "  |  일광 " + Mathf.RoundToInt(session.Daylight) : string.Empty);
            resourceText.text = "나무 " + session.GetStorage(ResourceKind.Wood) + "   돌 " + session.GetStorage(ResourceKind.Stone) + "   식량 " + session.GetStorage(ResourceKind.Food) + "   표류물 " + session.GetStorage(ResourceKind.Salvage) + "\n신호대 " + session.SignalStage + "/2   도끼 " + YesNo(session.HasAxe) + "   밧줄 " + YesNo(session.HasRope);
            messageText.text = session.LastMessage;

            if (session.Phase == GamePhase.Camp)
            {
                controlsText.text = activeDevice + " · 버튼을 선택해 캠프를 정비하세요 · Tab/방향키 이동 · Enter/A 선택";
                bagTitleText.text = "캠프 창고\n(수색 중에는 4칸 가방)";
            }
            else if (session.Phase == GamePhase.Exploring)
            {
                controlsText.text = activeDevice + " · A/D 또는 스틱 이동 · 해안에서 자동 수영 · Space/A 점프 · E/X 수색 · R/B 귀환";
                bagTitleText.text = session.HasPendingLoot ? "가방이 꽉 찼습니다\n버릴 슬롯을 선택" : "수색 가방 4칸\n한 묶음 최대 2개";
            }

            RefreshBagButtons();
        }

        private void UpdateCampButtons()
        {
            SetButton(campfireButton, session.HasStructure(StructureKind.Campfire) ? "✓ 모닥불" : "모닥불 건설  나무2·돌1", session.CanBuild(StructureKind.Campfire));
            SetButton(workbenchButton, session.HasStructure(StructureKind.Workbench) ? "✓ 작업대" : "작업대 건설  나무2·표류물1", session.CanBuild(StructureKind.Workbench));
            SetButton(rainButton, session.HasStructure(StructureKind.RainCollector) ? "✓ 빗물받이" : "빗물받이  나무2·돌1·표류물1", session.CanBuild(StructureKind.RainCollector));
            SetButton(researchAxeButton, session.HasResearched(TechKind.StoneAxe) ? "✓ 돌도끼 연구" : "돌도끼 연구  돌1·표류물1", session.CanResearch(TechKind.StoneAxe));
            SetButton(craftAxeButton, session.HasAxe ? "✓ 돌도끼 보유" : "돌도끼 제작  나무1·돌1", session.CanCraft(TechKind.StoneAxe));
            SetButton(researchRopeButton, session.HasResearched(TechKind.Rope) ? "✓ 밧줄 연구" : "밧줄 연구  표류물1", session.CanResearch(TechKind.Rope));
            SetButton(craftRopeButton, session.HasRope ? "✓ 밧줄 보유" : "밧줄 제작  나무1·표류물1", session.CanCraft(TechKind.Rope));
            SetButton(signalButton, session.SignalStage >= 2 ? "✓ 구조 신호 발신" : "구조 신호대 " + session.SignalStage + "/2  나무2·표류물2", session.CanUpgradeSignal());
            SetButton(eatButton, "식량 먹기  보유 " + session.GetStorage(ResourceKind.Food), session.GetStorage(ResourceKind.Food) > 0 && session.Hunger < 100f);
            SetButton(phaseButton, session.ExpeditionCompleted ? (session.Day >= GameSession.FinalDay ? "마지막 날 정산" : "다음 날로") : "섬 수색 출발", true);
        }

        private void RefreshBagButtons()
        {
            for (int i = 0; i < bagButtons.Count; i += 1)
            {
                BagStack stack = session.GetBagSlot(i);
                Text label = bagButtons[i].GetComponentInChildren<Text>();
                label.text = stack.IsEmpty ? (i + 1) + ". 빈칸" : (i + 1) + ". " + GameSession.ResourceName(stack.Kind) + " ×" + stack.Amount;
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
            swimWakeRoot = null;

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
            CreateSun(new Vector2(6.9f, 3.55f));
            CreatePalm(new Vector2(-7.1f, -2.25f), 1.2f);
            CreatePalm(new Vector2(7.5f, -2.35f), 0.9f);
            CreateKim(new Vector2(-5f, -2.18f));

            CreateStructurePlaceholder(StructureKind.Campfire, new Vector2(-2.3f, -2.45f), new Vector2(1.5f, 0.9f), new Color(1f, 0.43f, 0.14f), "모닥불");
            CreateStructurePlaceholder(StructureKind.Workbench, new Vector2(0.3f, -2.2f), new Vector2(2f, 1.2f), new Color(0.48f, 0.26f, 0.12f), "작업대");
            CreateStructurePlaceholder(StructureKind.RainCollector, new Vector2(3.2f, -1.95f), new Vector2(1.7f, 1.7f), new Color(0.27f, 0.7f, 0.86f), "빗물받이");

            Color signalColor = session.SignalStage == 0 ? new Color(0.38f, 0.42f, 0.4f, 0.55f) : session.SignalStage == 1 ? new Color(0.86f, 0.5f, 0.16f) : new Color(1f, 0.88f, 0.2f);
            GameObject signal = CreateRect("구조 신호대 · " + AssetStructures, new Vector2(6.1f, -1.2f), new Vector2(0.45f, session.SignalStage == 0 ? 2.7f : 4.1f), signalColor, 2);
            CreateWorldLabel(signal.transform, "신호대 " + session.SignalStage + "/2", new Vector3(0f, 1.45f, -0.1f), 50, Color.black);
        }

        private void CreateSearchWorld()
        {
            playerX = -3f;
            playerY = LandY;
            verticalVelocity = 0f;
            grounded = true;
            barrierMessageShown = false;
            worldCamera.transform.position = new Vector3(-3.8f, 0f, -10f);
            worldCamera.backgroundColor = new Color(0.35f, 0.74f, 0.9f);
            CreateRect("하늘 · " + AssetSearchBackground, new Vector2(4f, 1.5f), new Vector2(36f, 8.2f), new Color(0.35f, 0.74f, 0.9f), -20);
            CreateRect("얕은 연안", new Vector2(-8f, -1.15f), new Vector2(10f, 3f), new Color(0.12f, 0.55f, 0.76f), -15);
            CreateRect("연안 모래 바닥", new Vector2(-8f, -3.55f), new Vector2(10f, 1.3f), new Color(0.66f, 0.57f, 0.34f), -12);
            CreateRect("해변과 숲 바닥", new Vector2(8.5f, -3.25f), new Vector2(25f, 1.9f), new Color(0.87f, 0.68f, 0.34f), -10);
            CreateRect("해안선", new Vector2(CoastlineX, -2.35f), new Vector2(0.28f, 1.25f), new Color(0.86f, 0.94f, 0.86f), -4);
            CreateSun(new Vector2(-1f, 3.6f));
            for (int i = 0; i < 7; i += 1)
            {
                CreatePalm(new Vector2(2.8f + i * 2.35f, -2.28f), 0.75f + (i % 2) * 0.14f);
            }

            GameObject returnFlag = CreateRect("귀환 지점", new Vector2(-2.7f, -1.25f), new Vector2(0.18f, 2.6f), new Color(0.35f, 0.2f, 0.08f), 2);
            CreateRect("귀환 깃발", new Vector2(-2.15f, -0.35f), new Vector2(1.1f, 0.65f), new Color(1f, 0.48f, 0.16f), 3);
            CreateWorldLabel(returnFlag.transform, "CAMP", new Vector3(0.6f, 1.7f, -0.1f), 45, Color.black);

            Color barrierColor = session.HasRope ? new Color(0.25f, 0.7f, 0.3f, 0.35f) : new Color(0.2f, 0.42f, 0.17f, 0.95f);
            GameObject barrier = CreateRect("밧줄 필요 숲길", new Vector2(8.7f, -0.75f), new Vector2(1.25f, 5f), barrierColor, 1);
            CreateWorldLabel(barrier.transform, session.HasRope ? "밧줄로 통과" : "밧줄 필요", new Vector3(0f, 2.9f, -0.1f), 38, Color.black);

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
            CreateKim(new Vector2(playerX, playerY));
            CreateSwimWake();
        }

        private void UpdateExploration()
        {
            if (session.HasPendingLoot)
            {
                for (int i = 0; i < GameSession.BagSlotCount; i += 1)
                {
                    KeyCode code = (KeyCode)((int)KeyCode.Alpha1 + i);
                    if (Input.GetKeyDown(code))
                    {
                        session.ReplaceBagSlot(i);
                        RefreshAll();
                        return;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
                {
                    session.DiscardPendingLoot();
                    RefreshAll();
                }
                return;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontal) < 0.01f)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
            }

            float moveSpeed = session.IsSwimming ? 2.65f : 4.2f;
            playerX += horizontal * moveSpeed * Time.deltaTime;
            float maximumX = session.HasRope ? 19f : 8.0f;
            playerX = Mathf.Clamp(playerX, -10.5f, maximumX);
            if (!session.HasRope && playerX > 7.75f && !barrierMessageShown)
            {
                barrierMessageShown = true;
                messageText.text = "숲이 너무 빽빽하다. 밧줄을 만들면 넘어갈 방법이 생길 것 같다.";
            }

            bool wasSwimming = session.IsSwimming;
            bool shouldSwim = playerX < CoastlineX;
            session.SetSwimming(shouldSwim);

            bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.JoystickButton0);
            if (session.IsSwimming)
            {
                grounded = true;
                verticalVelocity = 0f;
                playerY = WaterY + Mathf.Sin(Time.time * 4.2f) * 0.08f;
            }
            else if (jumpPressed && grounded)
            {
                grounded = false;
                verticalVelocity = 6.5f;
            }

            if (!session.IsSwimming && wasSwimming)
            {
                playerY = LandY;
                verticalVelocity = 0f;
                grounded = true;
            }

            if (!session.IsSwimming && !grounded)
            {
                verticalVelocity -= 18f * Time.deltaTime;
                playerY += verticalVelocity * Time.deltaTime;
                if (playerY <= LandY)
                {
                    playerY = LandY;
                    verticalVelocity = 0f;
                    grounded = true;
                }
            }

            ApplyPlayerPresentation(horizontal);

            float targetCameraX = Mathf.Clamp(playerX + 2.5f, -6.5f, 12.5f);
            Vector3 cameraPosition = worldCamera.transform.position;
            cameraPosition.x = Mathf.Lerp(cameraPosition.x, targetCameraX, Time.deltaTime * 4f);
            worldCamera.transform.position = cameraPosition;

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton2))
            {
                GatherNearestNode();
            }

            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                session.ReturnToCamp(false);
                RefreshAll();
                return;
            }

            session.TickSearch(Time.deltaTime, Mathf.Abs(horizontal) > 0.05f);
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

                float distance = Mathf.Abs(nodes[i].X - playerX);
                if (distance < 1.35f && distance < nearestDistance)
                {
                    nearest = nodes[i];
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
            {
                messageText.text = "손을 뻗어 봤지만 잡히는 건 공기뿐이다.";
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

        private void DetectActiveDevice()
        {
            bool gamepad = false;
            for (int i = 0; i <= 15; i += 1)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    gamepad = true;
                    break;
                }
            }

            bool keyboard = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
            if (gamepad)
            {
                activeDevice = "게임패드";
            }
            else if (keyboard)
            {
                activeDevice = "키보드·마우스";
            }
        }

        public string RunAutomatedVerification(string explorationScreenshotPath, string swimmingScreenshotPath)
        {
            session.Reset();
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            RefreshAll();

            campfireButton.onClick.Invoke();
            workbenchButton.onClick.Invoke();
            rainButton.onClick.Invoke();
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
            float waterDaylightBefore = session.Daylight;
            float waterEnergyBefore = session.Energy;
            playerX = waterNode.X;
            playerY = WaterY;
            Require(session.SetSwimming(true), "해안 입수 전환");
            session.TickSearch(1f, true);
            Require(waterDaylightBefore - session.Daylight > 0.9f && waterEnergyBefore - session.Energy > 0.5f, "수영의 추가 일광·체력 소모");
            ApplyPlayerPresentation(-1f);
            worldCamera.transform.position = new Vector3(Mathf.Clamp(playerX + 2.5f, -6.5f, 12.5f), 0f, -10f);
            RefreshHud();
            if (!string.IsNullOrWhiteSpace(swimmingScreenshotPath))
            {
                CaptureVerificationPng(swimmingScreenshotPath, 1280, 800);
            }

            GatherNearestNode();
            Require(waterNode.Collected, "수영 중 연안 자원 수색");
            HashSet<ResourceKind> gatheredKinds = new HashSet<ResourceKind> { waterNode.Kind };
            Require(session.SetSwimming(false), "해안 이탈 전환");
            playerX = -1.1f;
            playerY = LandY;
            ApplyPlayerPresentation(1f);
            for (int i = 0; i < nodes.Count && gatheredKinds.Count < 4; i += 1)
            {
                NodeView node = nodes[i];
                if (node.Water || gatheredKinds.Contains(node.Kind))
                {
                    continue;
                }

                playerX = node.X;
                playerY = LandY;
                ApplyPlayerPresentation(1f);
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
            worldCamera.transform.position = new Vector3(Mathf.Clamp(playerX + 2.5f, -6.5f, 12.5f), 0f, -10f);
            RefreshHud();
            if (!string.IsNullOrWhiteSpace(explorationScreenshotPath))
            {
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
            return "PASS · UI 건설·제작·연구, 10개 수색 지점, 해안 입수·수영 비용·연안 채집·육지 복귀, 월드 이동·4종 채집, 가방·귀환·정산·신호대 확인";
        }

        public void CaptureVerificationPng(string absolutePath, int width, int height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
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
            GameObject root = new GameObject((water ? "연안 수색 · " : "채집 · ") + GameSession.ResourceName(kind));
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(x, water ? -1.72f : -2.25f, 0f);
            GameObject marker = CreateRect(root.transform, "자원", Vector2.zero, new Vector2(0.95f, 0.95f), ResourceColor(kind, 1f), 4);
            marker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreateWorldLabel(root.transform, (water ? "헤엄쳐 수색\n" : string.Empty) + GameSession.ResourceName(kind) + " ×" + amount, new Vector3(0f, 0.9f, -0.1f), 42, water ? Color.white : Color.black);
            nodes.Add(new NodeView { Kind = kind, Amount = amount, X = x, Water = water, Root = root, Collected = false });
        }

        private void CreateSwimWake()
        {
            GameObject root = new GameObject("수영 임시 표현 · " + AssetSwim);
            root.transform.SetParent(worldRoot, false);
            CreateRect(root.transform, "앞 물결", new Vector2(0.7f, 0f), new Vector2(1.15f, 0.12f), new Color(0.82f, 0.96f, 1f, 0.9f), 11);
            CreateRect(root.transform, "뒤 물결", new Vector2(-0.8f, -0.18f), new Vector2(1.55f, 0.1f), new Color(0.72f, 0.91f, 1f, 0.78f), 11);
            swimWakeRoot = root.transform;
            swimWakeRoot.gameObject.SetActive(false);
        }

        private void ApplyPlayerPresentation(float horizontal)
        {
            if (playerRoot == null)
            {
                return;
            }

            float currentSign = Mathf.Sign(playerRoot.localScale.x);
            float facing = horizontal < -0.01f ? -1f : horizontal > 0.01f ? 1f : (Mathf.Abs(currentSign) < 0.01f ? 1f : currentSign);
            playerRoot.position = new Vector3(playerX, playerY, 0f);
            playerRoot.localScale = session.IsSwimming ? new Vector3(facing * 1.25f, 0.72f, 1f) : new Vector3(facing, 1f, 1f);
            playerRoot.localRotation = Quaternion.Euler(0f, 0f, session.IsSwimming ? -68f : 0f);

            if (swimWakeRoot != null)
            {
                swimWakeRoot.gameObject.SetActive(session.IsSwimming);
                swimWakeRoot.position = new Vector3(playerX, WaterY - 0.25f, 0f);
                swimWakeRoot.localScale = new Vector3(facing, 1f, 1f);
            }
        }

        private void CreateStructurePlaceholder(StructureKind kind, Vector2 position, Vector2 size, Color color, string label)
        {
            bool built = session.HasStructure(kind);
            GameObject structure = CreateRect(label + " · " + AssetStructures, position, size, built ? color : new Color(0.3f, 0.34f, 0.32f, 0.48f), built ? 3 : 0);
            CreateWorldLabel(structure.transform, built ? label : label + " 자리", new Vector3(0f, size.y * 0.68f, -0.1f), 42, built ? Color.black : new Color(0.15f, 0.18f, 0.17f));
            structureViews[kind] = structure;
        }

        private void CreateKim(Vector2 position)
        {
            GameObject root = new GameObject("김씨 · " + AssetKim);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = position;
            CreateRect(root.transform, "몸", new Vector2(0f, 0.55f), new Vector2(0.85f, 1.35f), new Color(0.96f, 0.48f, 0.16f), 8);
            CreateRect(root.transform, "배낭", new Vector2(-0.46f, 0.52f), new Vector2(0.38f, 0.85f), new Color(0.22f, 0.36f, 0.27f), 7);
            CreateCircle(root.transform, "머리", new Vector2(0f, 1.47f), 0.78f, new Color(0.94f, 0.72f, 0.54f), 9);
            CreateRect(root.transform, "머리카락", new Vector2(0f, 1.77f), new Vector2(0.78f, 0.25f), new Color(0.11f, 0.08f, 0.06f), 10);
            CreateWorldLabel(root.transform, "김씨", new Vector3(0f, 2.2f, -0.1f), 42, Color.black);
            playerRoot = root.transform;
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

        private void CreateWorldLabel(Transform parent, string value, Vector3 localPosition, int size, Color color)
        {
            GameObject labelObject = new GameObject("라벨");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localScale = Vector3.one * 0.02f;
            TextMesh mesh = labelObject.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.font = font;
            mesh.fontSize = size;
            mesh.characterSize = 1f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 20;
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

        private Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
            return CreateButton("가방 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), (index + 1) + ". 빈칸", callback, new Vector2(22f, top - 84f), new Vector2(403f, top));
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
            Text text = CreateText("라벨", buttonObject.transform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f), 23, TextAnchor.MiddleCenter, Color.white);
            text.text = label;
            return button;
        }

        private static void SetButton(Button button, string label, bool interactable)
        {
            button.GetComponentInChildren<Text>().text = label;
            button.interactable = interactable;
        }

        private static string YesNo(bool value)
        {
            return value ? "보유" : "없음";
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
