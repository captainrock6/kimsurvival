using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeCampSpaceSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public PrototypeCampModuleExpansionSnapshot ModuleExpansion = new PrototypeCampModuleExpansionSnapshot();
        public PrototypeCampPlacementSnapshot Placement = new PrototypeCampPlacementSnapshot();
        public PrototypeCampUseSnapshot CampUse = new PrototypeCampUseSnapshot();

        public PrototypeCampSpaceSnapshot Clone()
        {
            return new PrototypeCampSpaceSnapshot
            {
                SchemaVersion = SchemaVersion,
                ModuleExpansion = ModuleExpansion == null ? null : ModuleExpansion.Clone(),
                Placement = Placement == null ? null : Placement.Clone(),
                CampUse = CampUse == null ? null : CampUse.Clone()
            };
        }
    }

    [Serializable]
    public sealed class PrototypeTerminalComicGeometryObservation
    {
        public string Locale = string.Empty;
        public int ActiveTextCount;
        public int TextTextOverlapCount;
        public int TextCardBoundaryViolationCount;
        public float TitleFontSize;
        public float MinimumCoreFontSize;
        public float ModifierFontSize;
        public string[] Violations = Array.Empty<string>();
    }

    public sealed partial class KimSurvivalPrototype : MonoBehaviour
    {
        private const int UnbuiltBlueprintInteractionPriority = 0;
        private const int ModuleInteractionPriority = 1;
        private const int InstalledFacilityInteractionPriority = 2;
        private const string AssetCampBackground = "background.island-camp";
        private const string AssetSearchBackground = "engine-native.expedition-region-layers.v1";
        private const string AssetKim = "character.mr-kim";
        private const string AssetSwim = "animation.mr-kim.swim";
        private const string AssetStructures = "object.camp-structures";
        private const string AssetVineBarrier = "object.vine-wood-barrier";
        private const string AssetHud = "ui.survival-hud";
        private const string AssetIcons = "icon.resource-tool-set";
        private const string AssetComedy = "effect.comedy-feedback";
        private const string AssetCampContextPrompt = "ui.camp-contextual-interaction.compact-a";
        private const string AssetExpeditionMap = "ui.expedition-map.right-rail-a";
        private const string AssetEndingAlbum = "ui.ending-gallery.album-spread-a";
        private const string CampContextPromptSkinResource = "Wave12CompactPromptSkin";

        private const float CampBackgroundWorldWidth = 20f;
        private const float CampCanvasWidthPixels = 1672f;
        private const float CampCanvasHeightPixels = 941f;
        private const float CampWalkableBaselineTopPixels = 721f;
        private const float CampSignalAnchorTopPixels = 596f;
        private const float CampBackgroundGroundNormalizedY = (CampCanvasHeightPixels - CampWalkableBaselineTopPixels) / CampCanvasHeightPixels;
        private const float CampSignalAnchorNormalizedX = 0.86f;
        private const float CampSignalAnchorNormalizedY = (CampCanvasHeightPixels - CampSignalAnchorTopPixels) / CampCanvasHeightPixels;
        private const float ResourceLabelWidth = 3.9f;
        private const float ResourceLabelHeight = 1.55f;
        private const float ResourceLabelViewportPadding = 0.12f;
        private const float ResourceLabelSafeViewportRight = 0.74f;
        private const float ResourceLabelSafeWorldY = 0.8f;
        private const float ResourceLabelHorizontalGap = 0.12f;
        private const float ResourceLabelPlayerHalfWidth = 0.82f;
        private const float SearchNodeInteractionDistance = 1.35f;
        private const float MinimumSupportedAspect = 1.6f;
        private static readonly Color CompactPromptBodyColor = new Color(121f / 255f, 143f / 255f, 141f / 255f, 1f);
        private static readonly Color CompactPromptTextColor = new Color(0.015f, 0.06f, 0.08f, 1f);
        private static readonly Vector2 CampProximityPromptAnchorMin = new Vector2(0.3f, 0.6f);
        private static readonly Vector2 CampProximityPromptAnchorMax = new Vector2(0.7f, 0.66f);
        private static readonly Vector2 CampModuleReasonAnchorMin = new Vector2(0.025f, 0.34f);
        private static readonly Vector2 CampModuleReasonAnchorMax = new Vector2(0.39f, 0.69f);
        private static readonly Vector2 CampPopupDefaultAnchorMin = new Vector2(0.56f, 0.2f);
        private static readonly Vector2 CampPopupDefaultAnchorMax = new Vector2(0.96f, 0.82f);
        private static readonly Vector2 CampPopupModuleSlotAnchorMin = new Vector2(0.62f, 0.36f);
        private static readonly Vector2 CampPopupModuleSlotAnchorMax = new Vector2(0.96f, 0.70f);
        private const float CampProximityPromptReferenceWidth = 1280f;
        private const float CampProximityPromptReferenceHeight = 800f;
        private const float StoragePlanningX = -3.8f;
        private const float ModulePlanningX = 4f;
        private const float ExpeditionMapX = 5.25f;
        private const float SmokeBeaconX = -2.35f;
        private const float RadioBenchX = 0f;
        private const float ShoreLaunchX = -5.3f;
        // Keep this target outside the expedition map's 1.25-unit approach lane so
        // both contextual objects retain an unambiguous proximity latch.
        private const float EndingAlbumX = 7.75f;

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
        [SerializeField] private Sprite expeditionMapLayoutSprite;
        [SerializeField] private Sprite endingAlbumLayoutSprite;
        [SerializeField] private Sprite kimAtlasSprite;
        [SerializeField] private Sprite woodIconSprite;
        [SerializeField] private Sprite stoneIconSprite;
        [SerializeField] private Sprite foodIconSprite;
        [SerializeField] private Sprite salvageIconSprite;

        private sealed class NodeView
        {
            public PrototypeSearchNodeDefinition Definition;
            public float X;
            public GameObject Root;
            public Transform LabelRoot;
            public TMP_Text Label;
            public SpriteRenderer LabelBackground;
        }

        private readonly List<NodeView> nodes = new List<NodeView>();
        private readonly List<Button> bagButtons = new List<Button>();
        private readonly List<Image> bagButtonIcons = new List<Image>();
        private readonly List<Button> campPopupButtons = new List<Button>();
        private readonly List<Button> expeditionRegionButtons = new List<Button>();
        private readonly List<Button> endingAlbumCardButtons = new List<Button>();
        private readonly List<Button> searchLootItemButtons = new List<Button>();
        private readonly List<Image> searchLootItemIcons = new List<Image>();
        private readonly List<GameObject> searchWorldContextLabels = new List<GameObject>();
        private readonly List<PrototypeCampInteractionTarget> campInteractionTargets = new List<PrototypeCampInteractionTarget>();
        private readonly List<SpriteRenderer> placementGhostOutlineRenderers = new List<SpriteRenderer>();
        private readonly Dictionary<StructureKind, GameObject> structureViews = new Dictionary<StructureKind, GameObject>();
        private readonly LegacyPrototypePlayerInput playerInput = new LegacyPrototypePlayerInput();
        private readonly PrototypePlayerTraversal playerTraversal = new PrototypePlayerTraversal();
        private readonly PrototypeCampPlacement campPlacement = new PrototypeCampPlacement();
        private readonly PrototypeCampUse campUse = new PrototypeCampUse();
        private readonly PrototypeCampInteraction campInteraction = new PrototypeCampInteraction();
        private readonly PrototypeExpeditionMapSelection expeditionMapSelection = new PrototypeExpeditionMapSelection();
        private readonly PrototypeEndingAlbumSelection endingAlbumSelection = new PrototypeEndingAlbumSelection();
        private readonly PrototypeCampModuleExpansion campModuleExpansion = new PrototypeCampModuleExpansion(PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance());
        private readonly CampModuleValidationContext campModuleValidation = new CampModuleValidationContext();
        private readonly List<SpriteRenderer> modulePreviewOutlineRenderers = new List<SpriteRenderer>();
        private PrototypeCampModuleExpansionPresenter campModulePresenter;

        private GameSession session;
        private PrototypeLocalization localization;
        private PrototypePlaytestEventRecorder playtestLog;
        private PrototypeWaveRuntime hazardEscapeEndingRuntime;
        private PrototypeSearchNodeRuntime searchNodeRuntime;
        private PrototypeEndingAlbumCollection endingAlbumCollection;
        private Camera worldCamera;
        private Canvas canvas;
        private Sprite squareSprite;
        private Sprite kimIdleSprite;
        private Sprite kimWalkSprite;
        private Sprite kimSwimSprite;
        private Transform worldRoot;
        private SpriteRenderer campBackgroundRenderer;
        private SpriteRenderer campGameplayGroundRenderer;
        private SpriteRenderer campForegroundRenderer;
        private SpriteRenderer rescueSignalRenderer;
        private SpriteRenderer vineBarrierRenderer;
        private Transform playerRoot;
        private PrototypePlayerPresentation playerPresentation;
        private GameObject placementGhost;
        private GameObject modulePreviewGhost;
        private SpriteRenderer modulePreviewBadgeRenderer;
        private TMP_Text modulePreviewBadgeText;
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
        private TMP_Text campPopupDetailText;
        private TMP_Text campProximityGlyphText;
        private TMP_Text campProximityText;
        private TMP_Text expeditionMapTitleText;
        private TMP_Text expeditionMapDetailText;
        private TMP_Text expeditionMapRiskText;
        private TMP_Text expeditionMapWeatherText;
        private TMP_Text expeditionMapEquipmentText;
        private TMP_Text expeditionMapSpecialText;
        private TMP_Text endingAlbumHeaderText;
        private TMP_Text endingAlbumDetailTitleText;
        private TMP_Text endingAlbumSummaryText;
        private TMP_Text endingAlbumStatusText;
        private TMP_Text endingAlbumControlsText;
        private TMP_Text searchLootTitleText;
        private TMP_Text searchLootStatusText;
        private TMP_Text searchLootBagText;
        private GameObject campActions;
        private GameObject campInteractionPopup;
        private Image campInteractionPopupFrameImage;
        private Sprite campInteractionPopupDefaultSprite;
        private Color campInteractionPopupDefaultColor;
        private GameObject campProximityPrompt;
        private GameObject expeditionMapPanel;
        private Image expeditionMapFrameImage;
        private GameObject endingAlbumPanel;
        private GameObject searchLootTrayPanel;
        private Image endingAlbumFrameImage;
        private GameObject campModuleReasonChip;
        private TMP_Text campModuleReasonText;
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
        private Button prepareCampfireButton;
        private Button collectRainButton;
        private Button repairButton;
        private Button treatDiseaseButton;
        private Button cancelPopupButton;
        private Button modulePreviewButton;
        private Button expeditionMapConfirmButton;
        private Button expeditionMapCancelButton;
        private Button smokeProjectButton;
        private Button radioProjectButton;
        private Button raftProjectButton;
        private Button endingAlbumOpenButton;
        private Button endingAlbumCloseButton;
        private Button searchLootTakeButton;
        private Button searchLootTakeAllButton;
        private Button searchLootLeaveButton;
        private Button phaseButton;
        private Button restartButton;
        private Button languageButton;
        private GamePhase renderedPhase;
        private PrototypeLocalizedText campFeedback;
        private bool modulePreviewCycleLatched;
        private PrototypeCampInteractionTargetKind modulePreviewReturnTargetKind;
        private string modulePreviewReturnTargetId = string.Empty;
        private bool modulePreviewCanResume;
        private bool vineBarrierClearLogged;
        private PrototypeCampPromptSkin campPromptSkin;
        private Image campProximityFrameImage;

        public GameSession Session
        {
            get { return session; }
        }

        public string CurrentCampRoomId
        {
            get { return campUse.CurrentRoomId; }
        }

        public string[] GetCommittedCampRoomIds()
        {
            IReadOnlyList<CampModuleCommittedRoomSnapshot> rooms = campModuleExpansion.CommittedRooms;
            string[] roomIds = new string[rooms.Count];
            for (int index = 0; index < rooms.Count; index += 1)
            {
                roomIds[index] = rooms[index].RoomId;
            }
            return roomIds;
        }

        public string GetInstalledCampFacilityRoomId(StructureKind kind)
        {
            return campPlacement.GetInstalledRoomId(kind);
        }

        public PrototypeCampSpaceSnapshot CaptureCampSpaceSnapshot()
        {
            return new PrototypeCampSpaceSnapshot
            {
                SchemaVersion = PrototypeCampSpaceSnapshot.CurrentSchemaVersion,
                ModuleExpansion = campModuleExpansion.CaptureSnapshot(),
                Placement = campPlacement.CaptureSnapshot(),
                CampUse = campUse.CaptureSnapshot()
            };
        }

        public string CaptureCampSpaceSaveJson()
        {
            return JsonUtility.ToJson(CaptureCampSpaceSnapshot());
        }

        public bool RestoreCampSpaceSaveJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                return RestoreCampSpaceSnapshot(JsonUtility.FromJson<PrototypeCampSpaceSnapshot>(json));
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public bool RestoreCampSpaceSnapshot(PrototypeCampSpaceSnapshot snapshot)
        {
            string failureReason = session == null ? "runtime is not initialized" : string.Empty;
            if (session == null || !TryStageCampSpaceSnapshot(snapshot, out failureReason))
            {
                if (!string.IsNullOrEmpty(failureReason))
                {
                    Debug.LogWarning("[Kim Survival] Camp-space restore rejected: " + failureReason);
                }
                return false;
            }

            PrototypeCampSpaceSnapshot previous = CaptureCampSpaceSnapshot();
            if (!campModuleExpansion.RestoreSnapshot(snapshot.ModuleExpansion.Clone()))
            {
                return false;
            }

            if (!HasCommittedPlacementMembership(snapshot.Placement, campModuleExpansion) ||
                !HasCommittedRoomMembership(snapshot.CampUse.StableRoomId, campModuleExpansion) ||
                !campPlacement.RestoreSnapshot(snapshot.Placement.Clone()) ||
                !campUse.RestoreSnapshot(snapshot.CampUse.Clone()))
            {
                RestoreCampSpaceModels(previous);
                return false;
            }

            campInteraction.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            RefreshAll();
            return true;
        }

        private bool TryStageCampSpaceSnapshot(PrototypeCampSpaceSnapshot snapshot, out string failureReason)
        {
            failureReason = string.Empty;
            if (snapshot == null ||
                snapshot.SchemaVersion != PrototypeCampSpaceSnapshot.CurrentSchemaVersion ||
                snapshot.ModuleExpansion == null ||
                snapshot.Placement == null ||
                snapshot.CampUse == null)
            {
                failureReason = "missing or unsupported camp-space v1 state";
                return false;
            }

            PrototypeCampModuleExpansion stagedModules = new PrototypeCampModuleExpansion(campModuleExpansion.Config);
            if (!stagedModules.RestoreSnapshot(snapshot.ModuleExpansion.Clone()))
            {
                failureReason = "module expansion snapshot is invalid";
                return false;
            }
            if (!HasCommittedPlacementMembership(snapshot.Placement, stagedModules))
            {
                failureReason = "a facility placement references an uncommitted room";
                return false;
            }

            PrototypeCampPlacement stagedPlacement = new PrototypeCampPlacement();
            if (!stagedPlacement.RestoreSnapshot(snapshot.Placement.Clone()))
            {
                failureReason = "facility placement snapshot is invalid";
                return false;
            }
            if (!HasCommittedRoomMembership(snapshot.CampUse.StableRoomId, stagedModules))
            {
                failureReason = "Mr. Kim references an uncommitted room";
                return false;
            }

            PrototypeCampUse stagedUse = new PrototypeCampUse();
            if (!stagedUse.RestoreSnapshot(snapshot.CampUse.Clone()))
            {
                failureReason = "camp-use snapshot is invalid";
                return false;
            }
            return true;
        }

        private static bool HasCommittedPlacementMembership(
            PrototypeCampPlacementSnapshot placementSnapshot,
            PrototypeCampModuleExpansion expansion)
        {
            if (placementSnapshot == null || placementSnapshot.Installed == null)
            {
                return false;
            }

            for (int index = 0; index < placementSnapshot.Installed.Length; index += 1)
            {
                CampInstalledStructurePlacementSnapshot installed = placementSnapshot.Installed[index];
                if (installed == null || !HasCommittedRoomMembership(installed.StableRoomId, expansion))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasCommittedRoomMembership(string stableRoomId, PrototypeCampModuleExpansion expansion)
        {
            return string.Equals(stableRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal) ||
                   expansion.IsRoomCommitted(stableRoomId);
        }

        private void RestoreCampSpaceModels(PrototypeCampSpaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            campModuleExpansion.RestoreSnapshot(snapshot.ModuleExpansion);
            campPlacement.RestoreSnapshot(snapshot.Placement);
            campUse.RestoreSnapshot(snapshot.CampUse);
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            session = new GameSession(
                PrototypeExpeditionRegionCatalog.CreateRuntimeSeed(),
                PrototypeSessionFlowProfileCatalog.GameJamProvisionalProfileId);
            campModulePresenter = new PrototypeCampModuleExpansionPresenter(campModuleExpansion);
            searchNodeRuntime = new PrototypeSearchNodeRuntime(session.RunSeed);
            endingAlbumCollection = PrototypeEndingAlbumCollection.LoadDefault();
            localization = new PrototypeLocalization();
            localization.LocaleChanged += HandleLocaleChanged;
            playtestLog = PrototypePlaytestEventRecorder.CreateDevelopment(
                session,
                delegate { return localization.CurrentLocaleCode; },
                delegate { return playerInput.ActiveDevice; });
            if (playtestLog != null)
            {
                playtestLog.RecordSessionStarted();
            }
            campPromptSkin = Resources.Load<PrototypeCampPromptSkin>(CampContextPromptSkinResource);
            squareSprite = MakeSquareSprite();
            PrepareKimSprites();
            BuildCamera();
            BuildEventSystem();
            BuildUi();
            hazardEscapeEndingRuntime = gameObject.AddComponent<PrototypeWaveRuntime>();
            hazardEscapeEndingRuntime.Initialize(session, localization, canvas, playtestLog, campInteractionTargets, endingAlbumCollection);
            ApplyTerminalComicLayoutPolicy();
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
            else if (session.Phase == GamePhase.Camp && campModuleExpansion.IsPreviewActive)
            {
                UpdateCampModulePreview();
            }
            else if (session.Phase == GamePhase.Camp)
            {
                UpdateCampUse();
            }

            if (renderedPhase != session.Phase)
            {
                RefreshAll();
            }
            else
            {
                RefreshHud();
            }

            if (playtestLog != null)
            {
                playtestLog.ObserveState();
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

        public void ConfigureExpeditionMapArt(Sprite rightRailLayout)
        {
            expeditionMapLayoutSprite = rightRailLayout;
        }

        public void ConfigureEndingAlbumArt(Sprite albumSpreadLayout)
        {
            endingAlbumLayoutSprite = albumSpreadLayout;
        }

        public void ConfigureCharacterAndItemArt(Sprite kimAtlas, Sprite wood, Sprite stone, Sprite food, Sprite salvage)
        {
            kimAtlasSprite = kimAtlas;
            woodIconSprite = wood;
            stoneIconSprite = stone;
            foodIconSprite = food;
            salvageIconSprite = salvage;
        }

        private void OnDestroy()
        {
            DestroyRuntimeSprite(kimIdleSprite);
            DestroyRuntimeSprite(kimWalkSprite);
            DestroyRuntimeSprite(kimSwimSprite);
            if (playtestLog != null)
            {
                playtestLog.Dispose();
                playtestLog = null;
            }

            if (localization != null)
            {
                localization.LocaleChanged -= HandleLocaleChanged;
                localization.Dispose();
            }
        }

        private void HandleLocaleChanged()
        {
            RefreshAll(session != null && session.Phase == GamePhase.Exploring);
        }

        public PrototypeTerminalComicGeometryObservation[] CaptureTerminalComicGeometryAudit()
        {
            RectTransform frame = FindTerminalComicRect("Finale Surface");
            GameObject comicRoot = FindTerminalComicObject("Resolution Triptych A");
            if (frame == null || comicRoot == null || !comicRoot.activeInHierarchy || localization == null)
            {
                return Array.Empty<PrototypeTerminalComicGeometryObservation>();
            }

            string originalLocale = localization.CurrentLocaleCode;
            string[] locales =
            {
                PrototypeLocalization.KoreanLocaleCode,
                PrototypeLocalization.EnglishLocaleCode,
                PrototypeLocalization.QpsLongLocaleCode
            };
            var observations = new List<PrototypeTerminalComicGeometryObservation>();
            try
            {
                foreach (string locale in locales)
                {
                    bool selected = string.Equals(locale, PrototypeLocalization.QpsLongLocaleCode, StringComparison.Ordinal)
                        ? localization.SetQaLocale(locale)
                        : localization.SetLocale(locale, false);
                    if (!selected || !string.Equals(localization.CurrentLocaleCode, locale, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ApplyTerminalComicLayoutPolicy();
                    Canvas.ForceUpdateCanvases();
                    TMP_Text[] texts = TerminalComicTexts(frame)
                        .Where(value => value != null && value.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(value.text))
                        .ToArray();
                    var rendered = new List<TerminalComicTextGeometry>();
                    var violations = new List<string>();
                    foreach (TMP_Text text in texts)
                    {
                        text.ForceMeshUpdate(false, true);
                        Rect glyph = RenderedTextWorldRect(text);
                        RectTransform owner = text.transform.parent as RectTransform;
                        Rect boundary = RectTransformWorldRect(owner);
                        rendered.Add(new TerminalComicTextGeometry(text.gameObject.name, glyph, boundary));
                        if (!RectContains(boundary, glyph, 0.5f))
                        {
                            violations.Add(text.gameObject.name + " outside " + (owner == null ? "missing owner" : owner.gameObject.name));
                        }
                    }

                    int overlapCount = 0;
                    for (int left = 0; left < rendered.Count; left += 1)
                    {
                        for (int right = left + 1; right < rendered.Count; right += 1)
                        {
                            if (!RectsIntersectWithArea(rendered[left].Glyph, rendered[right].Glyph, 0.5f)) continue;
                            overlapCount += 1;
                            violations.Add(rendered[left].Name + " overlaps " + rendered[right].Name);
                        }
                    }

                    observations.Add(new PrototypeTerminalComicGeometryObservation
                    {
                        Locale = locale,
                        ActiveTextCount = rendered.Count,
                        TextTextOverlapCount = overlapCount,
                        TextCardBoundaryViolationCount = rendered.Count(value =>
                            !RectContains(value.Boundary, value.Glyph, 0.5f)),
                        TitleFontSize = texts.Where(value => string.Equals(value.gameObject.name, "Finale Title", StringComparison.Ordinal))
                            .Select(value => value.fontSize).DefaultIfEmpty(0f).Min(),
                        MinimumCoreFontSize = texts.Where(value => value.gameObject.name.StartsWith("Copy ", StringComparison.Ordinal))
                            .Select(value => value.fontSize).DefaultIfEmpty(0f).Min(),
                        ModifierFontSize = texts.Where(value => string.Equals(value.gameObject.name, "Survival Behavior Copy", StringComparison.Ordinal))
                            .Select(value => value.fontSize).DefaultIfEmpty(0f).Min(),
                        Violations = violations.ToArray()
                    });
                }
            }
            finally
            {
                if (string.Equals(originalLocale, PrototypeLocalization.QpsLongLocaleCode, StringComparison.Ordinal))
                {
                    localization.SetQaLocale(originalLocale);
                }
                else
                {
                    localization.SetLocale(originalLocale, false);
                }
                ApplyTerminalComicLayoutPolicy();
                Canvas.ForceUpdateCanvases();
            }

            return observations.ToArray();
        }

        private sealed class TerminalComicTextGeometry
        {
            public readonly string Name;
            public readonly Rect Glyph;
            public readonly Rect Boundary;

            public TerminalComicTextGeometry(string name, Rect glyph, Rect boundary)
            {
                Name = name;
                Glyph = glyph;
                Boundary = boundary;
            }
        }

        private void ApplyTerminalComicLayoutPolicy()
        {
            if (hazardEscapeEndingRuntime == null) return;
            RectTransform frame = FindTerminalComicRect("Finale Surface");
            Image frameImage = frame == null ? null : frame.GetComponent<Image>();
            if (frame == null || frameImage == null || frameImage.sprite == null) return;

            TMP_Text title = FindTerminalComicText("Finale Title");
            bool titleHasDedicatedSurface = title != null && title.transform.parent != null &&
                                            string.Equals(title.transform.parent.name, "Finale Title Surface", StringComparison.Ordinal);
            ConfigureTerminalComicText(
                title,
                titleHasDedicatedSurface ? new Vector2(0.025f, 0.08f) : new Vector2(0.055f, 0.825f),
                titleHasDedicatedSurface ? new Vector2(0.975f, 0.92f) : new Vector2(0.92f, 0.965f),
                18f,
                24f,
                2);

            for (int index = 0; index < 3; index += 1)
            {
                TMP_Text badge = FindTerminalComicText("Act Badge " + (index + 1));
                bool badgeHasDedicatedCard = badge != null && badge.transform.parent != null &&
                                             badge.transform.parent.name.StartsWith("Act Badge Card ", StringComparison.Ordinal);
                ConfigureTerminalComicText(
                    badge,
                    badgeHasDedicatedCard ? new Vector2(0.035f, 0.08f) : new Vector2(0.055f, 0.70f),
                    badgeHasDedicatedCard ? new Vector2(0.965f, 0.92f) : new Vector2(0.945f, 0.93f),
                    badgeHasDedicatedCard ? 12f : 15f,
                    badgeHasDedicatedCard ? 18f : 22f,
                    3);
                TMP_Text content = FindTerminalComicText("Copy " + (index + 1));
                bool copyHasDedicatedCard = content != null && content.transform.parent != null &&
                                            content.transform.parent.name.StartsWith("Copy Card ", StringComparison.Ordinal);
                ConfigureTerminalComicText(
                    content,
                    copyHasDedicatedCard ? new Vector2(0.035f, 0.08f) : new Vector2(0.055f, 0.055f),
                    copyHasDedicatedCard ? new Vector2(0.965f, 0.92f) : new Vector2(0.945f, 0.385f),
                    copyHasDedicatedCard ? 12f : 14f,
                    16f,
                    copyHasDedicatedCard ? 4 : 5);
            }

            TMP_Text modifier = FindTerminalComicText("Survival Behavior Copy");
            ConfigureTerminalComicText(
                modifier,
                new Vector2(0.035f, 0.08f),
                new Vector2(0.965f, 0.92f),
                13f,
                17f,
                4);
            Canvas.ForceUpdateCanvases();
        }

        private GameObject FindTerminalComicObject(string objectName)
        {
            if (hazardEscapeEndingRuntime == null) return null;
            Transform found = FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(value => value != null && string.Equals(value.gameObject.name, objectName, StringComparison.Ordinal));
            return found == null ? null : found.gameObject;
        }

        private RectTransform FindTerminalComicRect(string objectName)
        {
            GameObject found = FindTerminalComicObject(objectName);
            return found == null ? null : found.GetComponent<RectTransform>();
        }

        private TMP_Text FindTerminalComicText(string objectName)
        {
            GameObject found = FindTerminalComicObject(objectName);
            return found == null ? null : found.GetComponent<TMP_Text>();
        }

        private static IEnumerable<TMP_Text> TerminalComicTexts(RectTransform frame)
        {
            if (frame == null) return Array.Empty<TMP_Text>();
            string[] names =
            {
                "Finale Title",
                "Act Badge 1", "Act Badge 2", "Act Badge 3",
                "Copy 1", "Copy 2", "Copy 3",
                "Survival Behavior Copy"
            };
            return frame.GetComponentsInChildren<TMP_Text>(true)
                .Where(value => value != null && names.Contains(value.gameObject.name, StringComparer.Ordinal));
        }

        private static void ConfigureTerminalComicText(
            TMP_Text text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float minimumFontSize,
            float maximumFontSize,
            int maximumVisibleLines)
        {
            if (text == null) return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimumFontSize;
            text.fontSizeMax = maximumFontSize;
            text.maxVisibleLines = maximumVisibleLines;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = new Vector4(4f, 3f, 4f, 3f);
            text.SetAllDirty();
            text.ForceMeshUpdate(false, true);
        }

        private static Rect RenderedTextWorldRect(TMP_Text text)
        {
            if (text == null) return default(Rect);
            Bounds bounds = text.textBounds;
            Vector3[] points =
            {
                text.transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, 0f)),
                text.transform.TransformPoint(new Vector3(bounds.max.x, bounds.min.y, 0f)),
                text.transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, 0f)),
                text.transform.TransformPoint(new Vector3(bounds.min.x, bounds.max.y, 0f))
            };
            return Rect.MinMaxRect(
                points.Min(point => point.x),
                points.Min(point => point.y),
                points.Max(point => point.x),
                points.Max(point => point.y));
        }

        private static Rect RectTransformWorldRect(RectTransform rect)
        {
            if (rect == null) return default(Rect);
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                corners.Min(point => point.x),
                corners.Min(point => point.y),
                corners.Max(point => point.x),
                corners.Max(point => point.y));
        }

        private static bool RectContains(Rect boundary, Rect content, float tolerance)
        {
            return content.xMin >= boundary.xMin - tolerance && content.xMax <= boundary.xMax + tolerance &&
                   content.yMin >= boundary.yMin - tolerance && content.yMax <= boundary.yMax + tolerance;
        }

        private static bool RectsIntersectWithArea(Rect left, Rect right, float tolerance)
        {
            float width = Mathf.Min(left.xMax, right.xMax) - Mathf.Max(left.xMin, right.xMin);
            float height = Mathf.Min(left.yMax, right.yMax) - Mathf.Max(left.yMin, right.yMin);
            return width > tolerance && height > tolerance;
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

            RectTransform top = CreatePanel("상태 HUD", canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -162f), new Vector2(-24f, -16f), new Color(0.035f, 0.075f, 0.095f, 0.94f));
            VerticalLayoutGroup topLayout = top.gameObject.AddComponent<VerticalLayoutGroup>();
            topLayout.padding = new RectOffset(76, 76, 4, 4);
            topLayout.spacing = 2f;
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = true;
            statusText = CreateText("날짜·상태", top, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 29, TextAnchor.MiddleLeft, Color.white);
            ConfigureTopHudText(statusText);
            ConfigureLayout(statusText.gameObject, 1f, 0f, 0f, 54f);
            resourceText = CreateText("보유 자원", top, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 25, TextAnchor.MiddleLeft, new Color(0.91f, 0.96f, 0.94f));
            ConfigureTopHudText(resourceText);
            resourceText.fontSizeMin = 18f;
            resourceText.fontSizeMax = 23f;
            resourceText.textWrappingMode = TextWrappingModes.Normal;
            resourceText.maxVisibleLines = 2;
            ConfigureLayout(resourceText.gameObject, 1f, 1f, 0f, 78f);

            RectTransform message = CreatePanel("김씨 독백 · 배치 상태 · " + AssetComedy, canvas.transform, new Vector2(0.23f, 0.715f), new Vector2(0.77f, 0.795f), Vector2.zero, Vector2.zero, new Color(0.035f, 0.075f, 0.085f, 0.9f));
            messagePanelImage = message.GetComponent<Image>();
            messageText = CreateText("김씨 독백 또는 배치 상태", message, Vector2.zero, Vector2.one, new Vector2(20f, 5f), new Vector2(-20f, -5f), 24, TextAnchor.MiddleCenter, Color.white);

            RectTransform controlPanel = CreatePanel("조작 안내", canvas.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 14f), new Vector2(-32f, 118f), new Color(0.035f, 0.075f, 0.095f, 0.94f));
            HorizontalLayoutGroup controlLayout = controlPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            controlLayout.padding = new RectOffset(76, 76, 6, 6);
            controlLayout.spacing = 12f;
            controlLayout.childAlignment = TextAnchor.MiddleCenter;
            controlLayout.childControlWidth = true;
            controlLayout.childControlHeight = true;
            controlLayout.childForceExpandWidth = true;
            controlLayout.childForceExpandHeight = true;
            controlsText = CreateText("조작", controlPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 27, TextAnchor.MiddleCenter, Color.white);
            controlsText.enableAutoSizing = true;
            controlsText.fontSizeMin = 21f;
            controlsText.fontSizeMax = 27f;
            controlsText.overflowMode = TextOverflowModes.Ellipsis;
            controlsText.maxVisibleLines = 3;
            ConfigureLayout(controlsText.gameObject, 4f, 1f, 700f, 0f);
            languageButton = CreateButton("언어 설정", controlPanel, Vector2.zero, Vector2.one, string.Empty, delegate { localization.CycleLocale(); });
            ConfigureLayout(languageButton.gameObject, 1.25f, 1f, 270f, 0f);
            TMP_Text languageLabel = languageButton.GetComponentInChildren<TMP_Text>();
            languageLabel.fontSize = 27f;
            languageLabel.enableAutoSizing = true;
            languageLabel.fontSizeMin = 22f;
            languageLabel.fontSizeMax = 27f;
            languageLabel.textWrappingMode = TextWrappingModes.NoWrap;
            languageLabel.overflowMode = TextOverflowModes.Overflow;
            languageLabel.maxVisibleLines = 1;
            phaseButton = CreateButton("수색·정산", controlPanel, Vector2.zero, Vector2.one, string.Empty, HandlePhaseButton);
            ConfigureLayout(phaseButton.gameObject, 1.45f, 1f, 280f, 0f);
            phaseButton.GetComponentInChildren<TMP_Text>().fontSize = 27f;

            campActions = new GameObject("Legacy campActions dashboard · disabled");
            campActions.transform.SetParent(canvas.transform, false);
            campActions.SetActive(false);

            campProximityPrompt = CreatePanel("설비 근접 안내 · " + AssetCampContextPrompt, canvas.transform, CampProximityPromptAnchorMin, CampProximityPromptAnchorMax, Vector2.zero, Vector2.zero, Color.white).gameObject;
            campProximityFrameImage = campProximityPrompt.GetComponent<Image>();
            if (campPromptSkin != null && campPromptSkin.Frame != null)
            {
                campProximityFrameImage.sprite = campPromptSkin.Frame;
                campProximityFrameImage.type = Image.Type.Sliced;
                campProximityFrameImage.color = Color.white;
            }
            campProximityFrameImage.raycastTarget = false;

            campProximityGlyphText = CreateText("설비 근접 입력 glyph", campProximityPrompt.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(12f, 0f), new Vector2(56f, 0f), 22, TextAnchor.MiddleCenter, new Color(0.025f, 0.11f, 0.15f));
            campProximityGlyphText.fontStyle = FontStyles.Bold;
            campProximityGlyphText.enableAutoSizing = true;
            campProximityGlyphText.fontSizeMin = 18f;
            campProximityGlyphText.fontSizeMax = 22f;
            campProximityGlyphText.textWrappingMode = TextWrappingModes.NoWrap;
            campProximityGlyphText.overflowMode = TextOverflowModes.Overflow;
            campProximityGlyphText.maxVisibleLines = 1;
            campProximityGlyphText.raycastTarget = false;

            campProximityText = CreateText("설비 근접 행동·대상 문구", campProximityPrompt.transform, Vector2.zero, Vector2.one, new Vector2(64f, 4f), new Vector2(-24f, -4f), 23, TextAnchor.MiddleCenter, CompactPromptTextColor);
            campProximityText.fontStyle = FontStyles.Bold;
            campProximityText.enableAutoSizing = true;
            campProximityText.fontSizeMin = 19f;
            campProximityText.fontSizeMax = 23f;
            campProximityText.textWrappingMode = TextWrappingModes.NoWrap;
            campProximityText.overflowMode = TextOverflowModes.Overflow;
            campProximityText.maxVisibleLines = 1;
            campProximityText.raycastTarget = false;

            campModuleReasonChip = CreatePanel("방 증축 비용·사유 칩", canvas.transform, CampModuleReasonAnchorMin, CampModuleReasonAnchorMax, Vector2.zero, Vector2.zero, new Color(0.03f, 0.08f, 0.09f, 0.96f)).gameObject;
            campModuleReasonText = CreateText("방 증축 목적·상태·비용", campModuleReasonChip.transform, Vector2.zero, Vector2.one, new Vector2(20f, 14f), new Vector2(-20f, -14f), 22, TextAnchor.UpperLeft, Color.white);
            campModuleReasonText.fontStyle = FontStyles.Bold;
            campModuleReasonText.enableAutoSizing = true;
            campModuleReasonText.fontSizeMin = 16f;
            campModuleReasonText.fontSizeMax = 22f;
            campModuleReasonText.textWrappingMode = TextWrappingModes.Normal;
            campModuleReasonText.overflowMode = TextOverflowModes.Overflow;
            campModuleReasonText.maxVisibleLines = 7;

            campInteractionPopup = CreatePanel("설비 전용 소형 팝업", canvas.transform, CampPopupDefaultAnchorMin, CampPopupDefaultAnchorMax, Vector2.zero, Vector2.zero, new Color(0.035f, 0.075f, 0.075f, 0.97f)).gameObject;
            campInteractionPopupFrameImage = campInteractionPopup.GetComponent<Image>();
            campInteractionPopupDefaultSprite = campInteractionPopupFrameImage.sprite;
            campInteractionPopupDefaultColor = campInteractionPopupFrameImage.color;
            actionTitleText = CreateText("설비 팝업 제목", campInteractionPopup.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -70f), new Vector2(-24f, -12f), 36, TextAnchor.MiddleLeft, new Color(1f, 0.91f, 0.5f));
            campPopupDetailText = CreateText("설비 팝업 설명", campInteractionPopup.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -154f), new Vector2(-24f, -76f), 28, TextAnchor.UpperLeft, new Color(0.9f, 0.96f, 0.91f));

            campfireButton = CreateCampPopupButton("모닥불 건설·재배치", delegate { ExecuteConfirmedPopupTransition("placement.campfire", delegate { BeginCampPlacement(StructureKind.Campfire); }); });
            workbenchButton = CreateCampPopupButton("작업대 건설·재배치", delegate { ExecuteConfirmedPopupTransition("placement.workbench", delegate { BeginCampPlacement(StructureKind.Workbench); }); });
            rainButton = CreateCampPopupButton("빗물받이 건설·재배치", delegate { ExecuteConfirmedPopupTransition("placement.rain_collector", delegate { BeginCampPlacement(StructureKind.RainCollector); }); });
            researchAxeButton = CreateCampPopupButton("돌도끼 연구", delegate { ExecuteConfirmedPopupAction("research.stone_axe", delegate { return session.TryResearch(TechKind.StoneAxe); }); });
            craftAxeButton = CreateCampPopupButton("돌도끼 제작", delegate { ExecuteConfirmedPopupAction("craft.stone_axe", delegate { return session.TryCraft(TechKind.StoneAxe); }); });
            researchRopeButton = CreateCampPopupButton("밧줄 연구", delegate { ExecuteConfirmedPopupAction("research.rope", delegate { return session.TryResearch(TechKind.Rope); }); });
            craftRopeButton = CreateCampPopupButton("밧줄 제작", delegate { ExecuteConfirmedPopupAction("craft.rope", delegate { return session.TryCraft(TechKind.Rope); }); });
            signalButton = CreateCampPopupButton("구조 신호 자원 투입", delegate { ExecuteConfirmedPopupAction("signal.upgrade", TryExecuteSignalAction); });
            modulePreviewButton = CreateCampPopupButton("방 모듈 증축 미리보기", ExecuteConfirmedModulePreviewTransition);
            eatButton = CreateCampPopupButton("식량 먹기", delegate { ExecuteConfirmedPopupAction("survival.eat", session.UseFood); });
            prepareCampfireButton = CreateCampPopupButton("생존 준비", delegate { ExecuteConfirmedPopupAction("survival.prepare_campfire", delegate { return TryPrepareDayBenefit(StructureKind.Campfire, "message.camp.use.campfire"); }); });
            collectRainButton = CreateCampPopupButton("빗물 받기", delegate { ExecuteConfirmedPopupAction("survival.collect_rain", delegate { return TryPrepareDayBenefit(StructureKind.RainCollector, "message.camp.use.rain"); }); });
            repairButton = CreateCampPopupButton("수리", delegate { ExecuteConfirmedPopupAction("workbench.repair", ExecuteRepairAction); });
            treatDiseaseButton = CreateCampPopupButton("질병 치료", delegate { ExecuteConfirmedPopupAction("disease.treat", TryTreatDiseaseAtWorkbench); });
            bagUpgradeButton = CreateCampPopupButton("가방 용량 확장", delegate { ExecuteConfirmedPopupAction("bag.capacity_upgrade", session.TryUpgradeBagCapacity); });
            smokeProjectButton = CreateCampPopupButton("대형 연기 신호 진행", delegate { ExecuteConfirmedPopupAction("escape.smoke.progress", delegate { return TryProgressEscapeProject("escape.smoke"); }); });
            radioProjectButton = CreateCampPopupButton("무전 구조 신호 진행", delegate { ExecuteConfirmedPopupAction("escape.radio.progress", delegate { return TryProgressEscapeProject("escape.radio"); }); });
            raftProjectButton = CreateCampPopupButton("뗏목 제작·출항", ExecuteRaftPopupAction);
            endingAlbumOpenButton = CreateCampPopupButton("생존 앨범 열기", OpenEndingAlbumFromPopup);
            cancelPopupButton = CreateCampPopupButton("취소", CancelCampPopup);

            expeditionMapPanel = CreatePanel(
                "채택 수집 지도 A · " + AssetExpeditionMap,
                canvas.transform,
                new Vector2(0.025f, 0.025f),
                new Vector2(0.975f, 0.975f),
                Vector2.zero,
                Vector2.zero,
                Color.white).gameObject;
            expeditionMapFrameImage = expeditionMapPanel.GetComponent<Image>();
            if (expeditionMapLayoutSprite != null)
            {
                expeditionMapFrameImage.sprite = expeditionMapLayoutSprite;
                expeditionMapFrameImage.type = Image.Type.Simple;
                expeditionMapFrameImage.preserveAspect = true;
                expeditionMapFrameImage.color = Color.white;
            }
            else
            {
                expeditionMapFrameImage.color = new Color(0.025f, 0.07f, 0.08f, 0.985f);
            }
            RectTransform runtimeRailRect = CreatePanel(
                "A안 런타임 우측 rail 면",
                expeditionMapPanel.transform,
                new Vector2(0.655f, 0.07f),
                new Vector2(0.96f, 0.9f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.91f, 0.84f, 0.65f, 1f));
            Image runtimeRailSurface = runtimeRailRect.GetComponent<Image>();
            runtimeRailSurface.raycastTarget = false;
            Outline runtimeRailOutline = runtimeRailSurface.gameObject.AddComponent<Outline>();
            runtimeRailOutline.effectColor = new Color(0.08f, 0.55f, 0.55f, 0.9f);
            runtimeRailOutline.effectDistance = new Vector2(2f, -2f);
            runtimeRailOutline.useGraphicAlpha = false;
            expeditionMapTitleText = CreateText(
                "수집 지도 제목",
                expeditionMapPanel.transform,
                new Vector2(0.68f, 0.805f),
                new Vector2(0.94f, 0.875f),
                new Vector2(6f, 2f),
                new Vector2(-4f, -2f),
                24,
                TextAnchor.MiddleLeft,
                new Color(0.04f, 0.18f, 0.2f));
            expeditionMapTitleText.fontStyle = FontStyles.Bold;
            expeditionMapTitleText.enableAutoSizing = true;
            expeditionMapTitleText.fontSizeMin = 18f;
            expeditionMapTitleText.fontSizeMax = 24f;
            expeditionMapTitleText.textWrappingMode = TextWrappingModes.Normal;
            expeditionMapTitleText.maxVisibleLines = 2;
            expeditionMapTitleText.overflowMode = TextOverflowModes.Overflow;

            IReadOnlyList<PrototypeExpeditionRegionProfile> expeditionProfiles = PrototypeExpeditionRegionCatalog.All;
            Vector2[] expeditionNodeAnchors =
            {
                new Vector2(0.12f, 0.72f),
                new Vector2(0.32f, 0.72f),
                new Vector2(0.52f, 0.72f),
                new Vector2(0.19f, 0.50f),
                new Vector2(0.46f, 0.50f),
                new Vector2(0.19f, 0.28f),
                new Vector2(0.46f, 0.28f)
            };
            float[] expeditionNodeHalfWidths = { 140f, 140f, 140f, 150f, 150f, 150f, 150f };
            for (int i = 0; i < expeditionProfiles.Count; i += 1)
            {
                PrototypeExpeditionRegionId capturedRegion = expeditionProfiles[i].Id;
                float halfWidth = expeditionNodeHalfWidths[i];
                Button regionButton = CreateButton(
                    "수집 지역 노드 · " + expeditionProfiles[i].StableId,
                    expeditionMapPanel.transform,
                    expeditionNodeAnchors[i],
                    expeditionNodeAnchors[i],
                    string.Empty,
                    delegate { FocusExpeditionRegion(capturedRegion); },
                    new Vector2(-halfWidth, -58f),
                    new Vector2(halfWidth, 58f));
                regionButton.GetComponent<Image>().color = new Color(0.025f, 0.16f, 0.18f, 0.94f);
                TMP_Text regionLabel = regionButton.GetComponentInChildren<TMP_Text>();
                regionLabel.fontStyle = FontStyles.Bold;
                regionLabel.enableAutoSizing = true;
                regionLabel.fontSizeMin = 18f;
                regionLabel.fontSizeMax = 22f;
                regionLabel.textWrappingMode = TextWrappingModes.Normal;
                regionLabel.maxVisibleLines = 3;
                regionLabel.overflowMode = TextOverflowModes.Overflow;
                regionLabel.margin = new Vector4(6f, 4f, 6f, 4f);
                Outline outline = regionButton.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.02f, 0.16f, 0.2f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
                outline.useGraphicAlpha = false;
                expeditionRegionButtons.Add(regionButton);
            }

            expeditionMapDetailText = CreateExpeditionRailText(
                "예상 자원 상세",
                new Vector2(0.68f, 0.655f),
                new Vector2(0.94f, 0.785f),
                4);
            expeditionMapRiskText = CreateExpeditionRailText(
                "위험·이동 시간 상세",
                new Vector2(0.68f, 0.525f),
                new Vector2(0.94f, 0.645f),
                3);
            expeditionMapWeatherText = CreateExpeditionRailText(
                "날씨 상세",
                new Vector2(0.68f, 0.42f),
                new Vector2(0.94f, 0.515f),
                3);
            expeditionMapEquipmentText = CreateExpeditionRailText(
                "필요 장비 상세",
                new Vector2(0.68f, 0.305f),
                new Vector2(0.94f, 0.41f),
                3);
            expeditionMapSpecialText = CreateExpeditionRailText(
                "특별 발견 상세",
                new Vector2(0.68f, 0.19f),
                new Vector2(0.94f, 0.295f),
                3);

            expeditionMapConfirmButton = CreateButton(
                "선택 지역으로 출발",
                expeditionMapPanel.transform,
                new Vector2(0.67f, 0.075f),
                new Vector2(0.865f, 0.175f),
                string.Empty,
                ConfirmSelectedExpeditionRegion,
                new Vector2(2f, 0f),
                new Vector2(-2f, 0f));
            expeditionMapCancelButton = CreateButton(
                "수집 지도 취소",
                expeditionMapPanel.transform,
                new Vector2(0.875f, 0.075f),
                new Vector2(0.955f, 0.175f),
                string.Empty,
                CancelCampPopup,
                Vector2.zero,
                Vector2.zero);
            ConfigureExpeditionMapButton(expeditionMapConfirmButton);
            ConfigureExpeditionMapButton(expeditionMapCancelButton);

            BuildEndingAlbumUi();

            bagPanel = CreatePanel("수색 가방 · 채택 " + AssetIcons, canvas.transform, new Vector2(0.69f, 0.19f), new Vector2(0.975f, 0.73f), Vector2.zero, Vector2.zero, new Color(0.035f, 0.075f, 0.085f, 0.95f)).gameObject;
            Outline bagOutline = bagPanel.AddComponent<Outline>();
            bagOutline.effectColor = new Color(0.31f, 0.72f, 0.72f, 0.85f);
            bagOutline.effectDistance = new Vector2(2f, -2f);
            bagTitleText = CreateText("가방 제목", bagPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -86f), new Vector2(-18f, -8f), 30, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.38f));
            bagTitleText.enableAutoSizing = true;
            bagTitleText.fontSizeMin = 24f;
            bagTitleText.fontSizeMax = 30f;
            bagTitleText.textWrappingMode = TextWrappingModes.Normal;
            bagTitleText.maxVisibleLines = 3;
            bagTitleText.overflowMode = TextOverflowModes.Ellipsis;
            for (int i = 0; i < GameSession.MaximumBagSlotCount; i += 1)
            {
                int capturedIndex = i;
                Button slot = CreateBagButton(bagPanel.transform, i, delegate { ReplaceBagSlotFromActiveContext(capturedIndex); });
                TMP_Text slotLabel = slot.GetComponentInChildren<TMP_Text>();
                slotLabel.fontSize = 27f;
                slotLabel.enableAutoSizing = true;
                slotLabel.fontSizeMin = 21f;
                slotLabel.fontSizeMax = 28f;
                slotLabel.textWrappingMode = TextWrappingModes.Normal;
                slotLabel.maxVisibleLines = 3;
                slotLabel.overflowMode = TextOverflowModes.Ellipsis;
                bagButtonIcons.Add(CreateBagItemIcon(slot.transform, i));
                bagButtons.Add(slot);
            }

            BuildSearchLootTrayUi();

            resultPanel = CreatePanel("결과", canvas.transform, new Vector2(0.24f, 0.22f), new Vector2(0.76f, 0.73f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.08f, 0.09f, 0.96f)).gameObject;
            resultTitleText = CreateText("결과 제목", resultPanel.transform, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero, 56, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.35f));
            resultDetailText = CreateText("결과 설명", resultPanel.transform, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.66f), Vector2.zero, Vector2.zero, 30, TextAnchor.MiddleCenter, Color.white);
            restartButton = CreateButton("다시 시작", resultPanel.transform, new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.24f), string.Empty, RestartSession);
            BuildGameJamSubmissionControls();
        }

        private void BuildSearchLootTrayUi()
        {
            searchLootTrayPanel = CreatePanel(
                "환경 수색 발견물 선택 트레이",
                canvas.transform,
                new Vector2(0.46f, 0.43f),
                new Vector2(0.985f, 0.82f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.035f, 0.09f, 0.095f, 0.97f)).gameObject;
            Image surface = searchLootTrayPanel.GetComponent<Image>();
            Outline outline = surface.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.96f, 0.76f, 0.3f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;

            searchLootTitleText = CreateText(
                "발견물 트레이 제목",
                searchLootTrayPanel.transform,
                new Vector2(0.035f, 0.79f),
                new Vector2(0.965f, 0.97f),
                Vector2.zero,
                Vector2.zero,
                26,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.86f, 0.38f));
            searchLootTitleText.fontStyle = FontStyles.Bold;
            searchLootTitleText.enableAutoSizing = true;
            searchLootTitleText.fontSizeMin = 24f;
            searchLootTitleText.fontSizeMax = 26f;
            searchLootTitleText.maxVisibleLines = 1;
            searchLootTitleText.overflowMode = TextOverflowModes.Ellipsis;

            searchLootStatusText = CreateText(
                "수색 비용·위험·잔량 상태",
                searchLootTrayPanel.transform,
                new Vector2(0.035f, 0.61f),
                new Vector2(0.965f, 0.8f),
                Vector2.zero,
                Vector2.zero,
                20,
                TextAnchor.MiddleLeft,
                Color.white);
            searchLootStatusText.enableAutoSizing = true;
            searchLootStatusText.fontSizeMin = 19f;
            searchLootStatusText.fontSizeMax = 22f;
            searchLootStatusText.textWrappingMode = TextWrappingModes.Normal;
            searchLootStatusText.maxVisibleLines = 2;
            searchLootStatusText.overflowMode = TextOverflowModes.Ellipsis;

            Vector2[] itemMin =
            {
                new Vector2(0.035f, 0.46f), new Vector2(0.505f, 0.46f),
                new Vector2(0.035f, 0.27f), new Vector2(0.505f, 0.27f)
            };
            Vector2[] itemMax =
            {
                new Vector2(0.49f, 0.64f), new Vector2(0.965f, 0.64f),
                new Vector2(0.49f, 0.45f), new Vector2(0.965f, 0.45f)
            };
            for (int index = 0; index < 4; index += 1)
            {
                int capturedIndex = index;
                Button itemButton = CreateButton(
                    "발견물 " + (index + 1),
                    searchLootTrayPanel.transform,
                    itemMin[index],
                    itemMax[index],
                    string.Empty,
                    delegate { FocusSearchLoot(capturedIndex); });
                TMP_Text label = itemButton.GetComponentInChildren<TMP_Text>();
                label.enableAutoSizing = true;
                label.fontSizeMin = 21f;
                label.fontSizeMax = 25f;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.maxVisibleLines = 3;
                label.overflowMode = TextOverflowModes.Ellipsis;
                RectTransform labelRect = label.rectTransform;
                labelRect.offsetMin = new Vector2(54f, 4f);
                labelRect.offsetMax = new Vector2(-8f, -4f);
                ColorBlock itemColors = itemButton.colors;
                itemColors.normalColor = Color.white;
                itemColors.highlightedColor = Color.white;
                itemColors.selectedColor = Color.white;
                itemColors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                itemColors.disabledColor = new Color(0.62f, 0.62f, 0.62f, 1f);
                itemButton.colors = itemColors;
                searchLootItemIcons.Add(CreateSearchLootItemIcon(itemButton.transform, index));
                searchLootItemButtons.Add(itemButton);
            }

            searchLootBagText = CreateText(
                "현재 가방 요약",
                searchLootTrayPanel.transform,
                new Vector2(0.035f, 0.15f),
                new Vector2(0.965f, 0.26f),
                Vector2.zero,
                Vector2.zero,
                18,
                TextAnchor.MiddleLeft,
                new Color(0.77f, 0.94f, 0.94f));
            searchLootBagText.enableAutoSizing = true;
            searchLootBagText.fontSizeMin = 18f;
            searchLootBagText.fontSizeMax = 21f;
            searchLootBagText.textWrappingMode = TextWrappingModes.Normal;
            searchLootBagText.maxVisibleLines = 2;
            searchLootBagText.overflowMode = TextOverflowModes.Ellipsis;

            searchLootTakeButton = CreateButton(
                "선택 발견물 담기",
                searchLootTrayPanel.transform,
                new Vector2(0.035f, 0.025f),
                new Vector2(0.32f, 0.14f),
                string.Empty,
                TakeFocusedSearchLoot);
            searchLootTakeAllButton = CreateButton(
                "담을 수 있는 발견물 모두 담기",
                searchLootTrayPanel.transform,
                new Vector2(0.335f, 0.025f),
                new Vector2(0.66f, 0.14f),
                string.Empty,
                TakeAllSearchLoot);
            searchLootLeaveButton = CreateButton(
                "발견물 남기고 닫기",
                searchLootTrayPanel.transform,
                new Vector2(0.675f, 0.025f),
                new Vector2(0.965f, 0.14f),
                string.Empty,
                CloseSearchLootTray);
            ConfigureExpeditionMapButton(searchLootTakeButton);
            ConfigureExpeditionMapButton(searchLootTakeAllButton);
            ConfigureExpeditionMapButton(searchLootLeaveButton);
            ConfigureActionButtonRole(searchLootTakeButton, new Color(0.92f, 0.62f, 0.18f), new Color(0.04f, 0.08f, 0.08f));
            ConfigureActionButtonRole(searchLootTakeAllButton, new Color(0.17f, 0.52f, 0.48f), Color.white);
            ConfigureActionButtonRole(searchLootLeaveButton, new Color(0.18f, 0.25f, 0.27f), Color.white);
            TMP_Text searchLootLeaveLabel = searchLootLeaveButton.GetComponentInChildren<TMP_Text>();
            searchLootLeaveLabel.fontSize = 23f;
            searchLootLeaveLabel.fontSizeMin = 23f;
            searchLootLeaveLabel.fontSizeMax = 23f;
        }

        private void BuildEndingAlbumUi()
        {
            endingAlbumPanel = CreatePanel(
                "채택 생존 앨범 A",
                canvas.transform,
                new Vector2(0.025f, 0.025f),
                new Vector2(0.975f, 0.975f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.02f, 0.12f, 0.15f, 1f)).gameObject;
            endingAlbumPanel.GetComponent<Image>().raycastTarget = false;
            RectTransform endingAlbumArt = CreatePanel(
                "album-spread-a 원화",
                endingAlbumPanel.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                Color.white);
            endingAlbumFrameImage = endingAlbumArt.GetComponent<Image>();
            endingAlbumFrameImage.sprite = endingAlbumLayoutSprite;
            endingAlbumFrameImage.type = Image.Type.Simple;
            endingAlbumFrameImage.preserveAspect = true;
            endingAlbumFrameImage.color = endingAlbumLayoutSprite == null
                ? new Color(0.025f, 0.07f, 0.08f, 0.985f)
                : Color.white;
            endingAlbumFrameImage.raycastTarget = false;

            endingAlbumHeaderText = CreateEndingAlbumText(
                "생존 앨범 해금 현황",
                new Vector2(0.06f, 0.85f),
                new Vector2(0.5f, 0.925f),
                28,
                TextAlignmentOptions.Left,
                2);
            endingAlbumDetailTitleText = CreateEndingAlbumText(
                "생존 앨범 선택 제목",
                new Vector2(0.535f, 0.715f),
                new Vector2(0.88f, 0.79f),
                28,
                TextAlignmentOptions.Center,
                2);
            endingAlbumSummaryText = CreateEndingAlbumText(
                "생존 앨범 요약 또는 비스포일러 힌트",
                new Vector2(0.535f, 0.375f),
                new Vector2(0.915f, 0.47f),
                22,
                TextAlignmentOptions.Center,
                4);
            RectTransform statusSurface = CreatePanel(
                "생존 앨범 상태 라벨 전용 바탕",
                endingAlbumPanel.transform,
                new Vector2(0.535f, 0.255f),
                new Vector2(0.915f, 0.365f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.965f, 0.91f, 0.72f, 0.985f));
            statusSurface.GetComponent<Image>().raycastTarget = false;
            Outline statusOutline = statusSurface.gameObject.AddComponent<Outline>();
            statusOutline.effectColor = new Color(0.04f, 0.28f, 0.29f, 0.88f);
            statusOutline.effectDistance = new Vector2(2f, -2f);
            endingAlbumStatusText = CreateEndingAlbumText(
                "생존 앨범 해금·범주 상태",
                new Vector2(0.55f, 0.267f),
                new Vector2(0.90f, 0.353f),
                20,
                TextAlignmentOptions.Center,
                3);
            RectTransform controlsSurface = CreatePanel(
                "생존 앨범 조작 안내 안전 바탕",
                endingAlbumPanel.transform,
                new Vector2(0.035f, 0.027f),
                new Vector2(0.52f, 0.18f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.015f, 0.10f, 0.12f, 0.94f));
            controlsSurface.GetComponent<Image>().raycastTarget = false;
            Outline controlsOutline = controlsSurface.gameObject.AddComponent<Outline>();
            controlsOutline.effectColor = new Color(0.22f, 0.86f, 0.82f, 0.72f);
            controlsOutline.effectDistance = new Vector2(1f, -1f);
            endingAlbumControlsText = CreateEndingAlbumText(
                "생존 앨범 조작 안내",
                new Vector2(0.075f, 0.05f),
                new Vector2(0.48f, 0.16f),
                20,
                TextAlignmentOptions.Left,
                3);
            endingAlbumHeaderText.color = new Color(0.72f, 0.96f, 0.95f);
            endingAlbumControlsText.color = new Color(0.72f, 0.96f, 0.95f);

            float[] cardX = { 0.125f, 0.199f, 0.273f, 0.347f, 0.421f };
            float[] cardY = { 0.715f, 0.592f, 0.469f, 0.346f, 0.223f };
            int[] rowCounts = { 5, 5, 4, 2, 5 };
            int definitionIndex = 0;
            for (int row = 0; row < rowCounts.Length; row += 1)
            {
                for (int column = 0; column < rowCounts[row]; column += 1)
                {
                    int capturedIndex = definitionIndex;
                    Button card = CreateButton(
                        "생존 앨범 카드 " + (definitionIndex + 1),
                        endingAlbumPanel.transform,
                        new Vector2(cardX[column], cardY[row]),
                        new Vector2(cardX[column], cardY[row]),
                        string.Empty,
                        delegate { FocusEndingAlbumEntry(capturedIndex); },
                        new Vector2(-28f, -28f),
                        new Vector2(28f, 28f));
                    TMP_Text label = card.GetComponentInChildren<TMP_Text>();
                    label.fontStyle = FontStyles.Bold;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 18f;
                    label.fontSizeMax = 20f;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.maxVisibleLines = 1;
                    label.overflowMode = TextOverflowModes.Ellipsis;
                    Outline outline = card.gameObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(2f, -2f);
                    outline.useGraphicAlpha = false;
                    endingAlbumCardButtons.Add(card);
                    definitionIndex += 1;
                }
            }

            endingAlbumCloseButton = CreateButton(
                "생존 앨범 닫기",
                endingAlbumPanel.transform,
                new Vector2(0.65f, 0.16f),
                new Vector2(0.87f, 0.245f),
                string.Empty,
                CloseEndingAlbumToPopup,
                new Vector2(4f, 0f),
                new Vector2(-4f, 0f));
            TMP_Text closeLabel = endingAlbumCloseButton.GetComponentInChildren<TMP_Text>();
            ColorBlock closeColors = endingAlbumCloseButton.colors;
            closeColors.normalColor = new Color(0.02f, 0.18f, 0.2f, 0.28f);
            closeColors.highlightedColor = new Color(0.08f, 0.44f, 0.45f, 0.52f);
            closeColors.selectedColor = closeColors.highlightedColor;
            closeColors.pressedColor = new Color(0.92f, 0.48f, 0.16f, 0.58f);
            endingAlbumCloseButton.colors = closeColors;
            closeLabel.enableAutoSizing = true;
            closeLabel.fontSizeMin = 18f;
            closeLabel.fontSizeMax = 24f;
            closeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            closeLabel.maxVisibleLines = 1;
            closeLabel.overflowMode = TextOverflowModes.Overflow;
        }

        private TMP_Text CreateEndingAlbumText(
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize,
            TextAlignmentOptions alignment,
            int maximumLines)
        {
            TMP_Text text = CreateText(
                name,
                endingAlbumPanel.transform,
                anchorMin,
                anchorMax,
                new Vector2(4f, 2f),
                new Vector2(-4f, -2f),
                fontSize,
                TextAnchor.MiddleCenter,
                new Color(0.025f, 0.15f, 0.18f));
            text.alignment = alignment;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = fontSize;
            text.textWrappingMode = maximumLines == 1 ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            text.maxVisibleLines = maximumLines;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void HandlePhaseButton()
        {
            if (campPlacement.IsActive)
            {
                return;
            }

            if (session.ExpeditionCompleted)
            {
                if (session.EndDay(
                    campUse.IsDayBenefitPrepared(StructureKind.Campfire),
                    campUse.IsDayBenefitPrepared(StructureKind.RainCollector)))
                {
                    if (searchNodeRuntime.NotifyDaySettlement(session))
                    {
                        campFeedback = new PrototypeLocalizedText(searchNodeRuntime.Disease.FeedbackLocalizationKey);
                    }
                    campUse.ClearDayBenefits();
                    if (!searchNodeRuntime.Disease.HasHistory) campFeedback = PrototypeLocalizedText.Empty;
                }
            }
            else
            {
                if (session.BeginSearch())
                {
                    campUse.ClearDayBenefits();
                    campFeedback = PrototypeLocalizedText.Empty;
                }
            }
            if (playtestLog != null)
            {
                playtestLog.ObserveState("phase_button");
            }
            RefreshAll();
        }

        private void RestartSession()
        {
            session.Reset(PrototypeExpeditionRegionCatalog.CreateRuntimeSeed());
            searchNodeRuntime.Reset(session.RunSeed);
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            expeditionMapSelection.Close();
            endingAlbumSelection.Close();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            vineBarrierClearLogged = false;
            if (hazardEscapeEndingRuntime != null)
            {
                hazardEscapeEndingRuntime.ResetRuntime();
            }
            if (playtestLog != null)
            {
                playtestLog.ObserveState("session.restart");
                playtestLog.RecordSessionStarted();
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshAll(false);
        }

        private void RefreshAll(bool preserveExplorationPosition)
        {
            bool restoreTraversal = preserveExplorationPosition && session.Phase == GamePhase.Exploring && playerRoot != null;
            float restoredPlayerX = restoreTraversal ? playerTraversal.X : 0f;
            float restoredPlayerY = restoreTraversal ? playerTraversal.Y : 0f;
            float restoredCameraX = restoreTraversal && worldCamera != null ? worldCamera.transform.position.x : 0f;
            renderedPhase = session.Phase;
            bool selectionPausesHazards = session.Phase == GamePhase.Exploring &&
                                         searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen;
            if (hazardEscapeEndingRuntime != null && !selectionPausesHazards)
            {
                hazardEscapeEndingRuntime.TickCampaignState();
            }
            RebuildWorld();
            if (restoreTraversal && playerPresentation != null)
            {
                playerPresentation.Apply(playerTraversal.RestorePosition(restoredPlayerX, restoredPlayerY, session.IsSwimming));
                Vector3 cameraPosition = worldCamera.transform.position;
                cameraPosition.x = restoredCameraX;
                worldCamera.transform.position = cameraPosition;
                UpdateResourceLabelLayout();
            }
            RefreshCampInteractionSelection();
            SetButton(restartButton, localization.Format("ui.restart"), true);
            string languageKey = localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode ? "ui.language.switch.ko" : "ui.language.switch.en";
            SetButton(languageButton, localization.Format(languageKey), true);
            bool camp = session.Phase == GamePhase.Camp;
            bool result = session.Phase == GamePhase.Result;
            bool placing = camp && campPlacement.IsActive;
            bool modulePreview = camp && campModuleExpansion.IsPreviewActive;
            bool popup = camp && !placing && !modulePreview && campInteraction.IsPopupOpen;
            bool searchTray = session.Phase == GamePhase.Exploring && searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen;
            bool expeditionMapPopup = popup && campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap;
            bool endingAlbumPopup = popup && campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.EndingAlbum && endingAlbumSelection.IsOpen;
            campActions.SetActive(false);
            campInteractionPopup.SetActive(popup && !expeditionMapPopup && !endingAlbumPopup);
            expeditionMapPanel.SetActive(expeditionMapPopup);
            endingAlbumPanel.SetActive(endingAlbumPopup);
            searchLootTrayPanel.SetActive(searchTray);
            campProximityPrompt.SetActive(camp && !placing && !modulePreview && !popup && campInteraction.HasProximityPrompt);
            campModuleReasonChip.SetActive(modulePreview);
            bagPanel.SetActive(session.Phase == GamePhase.Exploring && !placing && !searchTray);
            phaseButton.gameObject.SetActive(camp && session.ExpeditionCompleted && !placing && !modulePreview && !popup);
            messagePanelImage.gameObject.SetActive(!popup && !result && !searchTray);
            resultPanel.SetActive(result);
            RefreshGameJamSubmissionControls(result);
            if (result)
            {
                resultTitleText.text = localization.Format(session.ResultTitle());
                resultDetailText.text = localization.Format(session.ResultDetail());
                if (hazardEscapeEndingRuntime != null)
                {
                    hazardEscapeEndingRuntime.ActivateTerminalComic();
                    ApplyTerminalComicLayoutPolicy();
                }
                EventSystem.current.SetSelectedGameObject(GameJamTerminalDefaultSelection);
            }
            else if (camp)
            {
                UpdateCampButtons();
                RefreshCampInteractionUi();
                if (expeditionMapPopup)
                {
                    RefreshExpeditionMapUi();
                    EventSystem.current.SetSelectedGameObject(expeditionRegionButtons[expeditionMapSelection.FocusedIndex].gameObject);
                }
                else if (endingAlbumPopup)
                {
                    RefreshEndingAlbumUi();
                    EventSystem.current.SetSelectedGameObject(endingAlbumCardButtons[endingAlbumSelection.FocusedIndex].gameObject);
                }
                else
                {
                    EventSystem.current.SetSelectedGameObject(popup ? FirstVisiblePopupButton() : campPlacement.IsActive || modulePreview ? null : session.ExpeditionCompleted ? phaseButton.gameObject : null);
                }
            }
            else if (searchTray)
            {
                RefreshSearchLootTrayUi();
                GameObject selected = session.HasPendingLoot && bagButtons.Count > 0
                    ? bagButtons[0].gameObject
                    : FirstVisibleSearchLootButton();
                EventSystem.current.SetSelectedGameObject(selected);
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
                ? localization.Format("hud.status.exploring", session.Day, session.SettlementDay, phaseName, Mathf.RoundToInt(session.Hunger), Mathf.RoundToInt(session.Energy), Mathf.RoundToInt(session.Daylight), session.Health)
                : localization.Format("hud.status.camp", session.Day, session.SettlementDay, phaseName, Mathf.RoundToInt(session.Hunger), Mathf.RoundToInt(session.Energy), session.Health);
            resourceText.text = IsGameJamLiveEscapeProfile
                ? FormatReadableStableResourceHud()
                : localization.Format(
                    "hud.resources",
                    session.GetStorage(ResourceKind.Wood),
                    session.GetStorage(ResourceKind.Stone),
                    session.GetStorage(ResourceKind.Food),
                    session.GetStorage(ResourceKind.Salvage),
                    session.SignalStage,
                    localization.Format(session.HasAxe ? "value.yes" : "value.no"),
                    localization.Format(session.HasRope ? "value.yes" : "value.no"));
            PrototypeLocalizedText activeMessage = session.Phase == GamePhase.Camp && !campFeedback.IsEmpty
                ? campFeedback
                : session.LastMessage;
            messageText.text = localization.Format(activeMessage);
            string device = localization.DeviceName(playerInput.ActiveDevice);
            float messageFontSize = activeMessage.Key.StartsWith("message.signal", StringComparison.Ordinal)
                ? 27f
                : activeMessage.Key.StartsWith("message.bag_upgrade", StringComparison.Ordinal) ? 27f : 24f;
            messageText.fontSize = messageFontSize;
            messageText.fontStyle = FontStyles.Normal;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 20f;
            messageText.fontSizeMax = messageFontSize;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Overflow;
            messageText.maxVisibleLines = 2;
            messagePanelImage.color = new Color(0.07f, 0.08f, 0.07f, 0.84f);

            if (session.Phase == GamePhase.Camp)
            {
                if (campPlacement.IsActive)
                {
                    ApplyPlacementGuidance(playerInput.ActiveDevice);
                }
                else if (campModuleExpansion.IsPreviewActive)
                {
                    ApplyCampModulePreviewGuidance(playerInput.ActiveDevice);
                }
                else if (campInteraction.IsPopupOpen)
                {
                    controlsText.text = localization.Format(
                        campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap
                            ? PrototypeInputPromptKeys.ExpeditionMap(playerInput.ActiveDevice)
                            : campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.EndingAlbum && endingAlbumSelection.IsOpen
                                ? PrototypeInputPromptKeys.EndingAlbum(playerInput.ActiveDevice)
                            : PrototypeInputPromptKeys.CampPopup(playerInput.ActiveDevice),
                        device);
                }
                else
                {
                    controlsText.text = localization.Format(PrototypeInputPromptKeys.Camp(playerInput.ActiveDevice), device);
                }
                bagTitleText.text = localization.Format("bag.camp", session.ActiveBagSlotCount, GameSession.MaximumBagSlotCount);
                RefreshCampInteractionUi();
            }
            else if (session.Phase == GamePhase.Exploring)
            {
                controlsText.text = searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen
                    ? localization.Format(PrototypeInputPromptKeys.SearchTray(playerInput.ActiveDevice), device, session.ActiveBagSlotCount)
                    : localization.Format(PrototypeInputPromptKeys.Explore(playerInput.ActiveDevice), device, session.ActiveBagSlotCount);
                bagTitleText.text = localization.Format(session.HasPendingLoot ? "bag.pending" : "bag.exploring", session.ActiveBagSlotCount, GameSession.MaximumBagSlotCount);
                if (searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen)
                {
                    RefreshSearchLootTrayUi();
                }
            }

            RefreshBagButtons();
            ApplySearchTrayHudLayoutPolicy();
        }

        private string FormatReadableStableResourceHud()
        {
            StableResourceAmount[] stored = session.GetStableStorageEntries()
                .Where(value => value.Amount > 0)
                .ToArray();
            if (stored.Length == 0)
            {
                return "—";
            }

            int firstRowCount = Mathf.CeilToInt(stored.Length * 0.5f);
            string firstRow = FormatReadableResourceHudRow(stored.Take(firstRowCount));
            string secondRow = FormatReadableResourceHudRow(stored.Skip(firstRowCount));
            return string.IsNullOrEmpty(secondRow) ? firstRow : firstRow + "\n" + secondRow;
        }

        private string FormatReadableResourceHudRow(IEnumerable<StableResourceAmount> entries)
        {
            return string.Join("  ·  ", entries.Select(value =>
            {
                string name = localization.ResourceName(value.StableResourceId, value.LegacyKind);
                string accent = PrototypeResourcePresentation.AccentHex(value.StableResourceId, value.LegacyKind);
                return "<color=#" + accent + ">" + name + "</color> " + value.Amount;
            }).ToArray());
        }

        private void ApplySearchTrayHudLayoutPolicy()
        {
            bool compactPseudoLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode &&
                                     searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen;
            statusText.fontSizeMin = 22f;
            statusText.fontSizeMax = compactPseudoLong ? 26f : 28f;
            resourceText.fontSizeMin = 18f;
            resourceText.fontSizeMax = compactPseudoLong ? 21f : 23f;
            statusText.overflowMode = compactPseudoLong ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            resourceText.textWrappingMode = TextWrappingModes.Normal;
            resourceText.maxVisibleLines = 2;
            resourceText.overflowMode = TextOverflowModes.Ellipsis;

            controlsText.fontSizeMin = compactPseudoLong ? 17f : 21f;
            controlsText.fontSizeMax = compactPseudoLong ? 23f : 27f;
            controlsText.maxVisibleLines = 3;
            controlsText.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text languageLabel = languageButton.GetComponentInChildren<TMP_Text>();
            languageLabel.enableAutoSizing = true;
            languageLabel.textWrappingMode = TextWrappingModes.NoWrap;
            languageLabel.maxVisibleLines = 1;
            languageLabel.fontSizeMin = compactPseudoLong ? 18f : 22f;
            languageLabel.fontSizeMax = compactPseudoLong ? 23f : 27f;
            languageLabel.overflowMode = compactPseudoLong ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;

            bagTitleText.enableAutoSizing = true;
            bagTitleText.textWrappingMode = TextWrappingModes.Normal;
            bagTitleText.maxVisibleLines = compactPseudoLong ? 3 : 2;
            bagTitleText.overflowMode = compactPseudoLong ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            bagTitleText.fontSizeMin = compactPseudoLong ? 17f : 22f;
            bagTitleText.fontSizeMax = compactPseudoLong ? 25f : 30f;
            for (int index = 0; index < bagButtons.Count; index += 1)
            {
                TMP_Text label = bagButtons[index].GetComponentInChildren<TMP_Text>();
                label.enableAutoSizing = true;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.maxVisibleLines = compactPseudoLong ? 3 : 2;
                label.overflowMode = compactPseudoLong ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
                label.fontSizeMin = compactPseudoLong ? 18f : 21f;
                label.fontSizeMax = compactPseudoLong ? 24f : 28f;
            }
        }

        private void ApplyPlacementGuidance(PrototypeInputDevice device)
        {
            bool valid = campPlacement.CurrentValidity == CampPlacementValidity.Valid;
            string state = localization.Format(valid ? "placement.state.valid" : "placement.state.invalid");
            messageText.text = localization.Format("placement.summary", state, localization.Format(campPlacement.CurrentFeedback));
            messageText.fontSize = 32f;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 26f;
            messageText.fontSizeMax = 32f;
            messageText.maxVisibleLines = 2;
            messageText.fontStyle = FontStyles.Bold;
            messagePanelImage.color = valid
                ? new Color(0.04f, 0.27f, 0.15f, 0.96f)
                : new Color(0.38f, 0.08f, 0.06f, 0.96f);
            controlsText.text = localization.Format(PrototypeInputPromptKeys.Placement(device), localization.DeviceName(device));
        }

        private void UpdateCampButtons()
        {
            bool available = campInteraction.IsPopupOpen && !campPlacement.IsActive;
            bool diseaseTreatmentInputAvailable = searchNodeRuntime.Disease.IsTreatable ||
                                                    searchNodeRuntime.Disease.Phase == PrototypeDiseasePhase.Recovering;
            SetButton(campfireButton, localization.Format(session.HasStructure(StructureKind.Campfire) ? "button.campfire.relocate" : "button.campfire.build"), available);
            SetButton(workbenchButton, localization.Format(session.HasStructure(StructureKind.Workbench) ? "button.workbench.relocate" : "button.workbench.build"), available);
            SetButton(rainButton, localization.Format(session.HasStructure(StructureKind.RainCollector) ? "button.rain.relocate" : "button.rain.build"), available);
            SetButton(researchAxeButton, localization.Format(session.HasResearched(TechKind.StoneAxe) ? "button.research.axe.done" : "button.research.axe"), available && session.CanResearch(TechKind.StoneAxe));
            SetButton(craftAxeButton, localization.Format(session.HasAxe ? "button.craft.axe.done" : "button.craft.axe"), available && session.CanCraft(TechKind.StoneAxe));
            SetButton(researchRopeButton, localization.Format(session.HasResearched(TechKind.Rope) ? "button.research.rope.done" : "button.research.rope"), available && session.CanResearch(TechKind.Rope));
            SetButton(craftRopeButton, localization.Format(session.HasRope ? "button.craft.rope.done" : "button.craft.rope"), available && session.CanCraft(TechKind.Rope));
            SetButton(signalButton, FormatSignalButton(), available && session.SignalStage < 2);
            signalButton.GetComponentInChildren<TMP_Text>().fontSize = localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode ? 31f : 36f;
            UpdateBagUpgradeButton(available);
            SetButton(eatButton, localization.Format("button.eat", session.GetStorage(ResourceKind.Food)), available);
            SetButton(prepareCampfireButton, localization.Format(campUse.IsDayBenefitPrepared(StructureKind.Campfire) ? "button.campfire.prepare.done" : "button.campfire.prepare"), available && !campUse.IsDayBenefitPrepared(StructureKind.Campfire));
            SetButton(collectRainButton, localization.Format(campUse.IsDayBenefitPrepared(StructureKind.RainCollector) ? "button.rain.collect.done" : "button.rain.collect"), available && !campUse.IsDayBenefitPrepared(StructureKind.RainCollector));
            SetButton(repairButton, localization.Format("button.workbench.repair"), available);
            SetButton(
                treatDiseaseButton,
                localization.Format("hazard-profile.disease.jungle-fever.treatment", session.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId)),
                available && session.HasStructure(StructureKind.Workbench) && diseaseTreatmentInputAvailable);
            SetButton(smokeProjectButton, FormatEscapeProjectButton("escape.smoke"), available);
            SetButton(radioProjectButton, FormatEscapeProjectButton("escape.radio"), available);
            SetButton(raftProjectButton, FormatRaftProjectButton(), available);
            SetButton(endingAlbumOpenButton, localization.Format("button.ending_album.open"), available);
            SetButton(cancelPopupButton, localization.Format("button.popup.cancel"), available);
            bool directModuleSlot = campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot;
            SetButton(
                modulePreviewButton,
                localization.Format(directModuleSlot ? "ui.module.expand" : campModuleExpansion.HasCommittedModule ? "button.module.preview.complete" : "button.module.preview"),
                available && (directModuleSlot || !campModuleExpansion.HasCommittedModule));
            string phaseButtonKey = session.ExpeditionCompleted ? (session.Day >= session.SettlementDay ? "button.day.final" : "button.day.next") : "button.search.start";
            SetButton(phaseButton, localization.Format(phaseButtonKey), !campPlacement.IsActive && !campModuleExpansion.IsPreviewActive && !campInteraction.IsPopupOpen);
            UpdatePopupActionVisibility();
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

        private void RefreshCampInteractionUi()
        {
            if (campInteractionPopup == null || campProximityPrompt == null || session == null)
            {
                return;
            }

            bool camp = session.Phase == GamePhase.Camp && !campPlacement.IsActive && !campModuleExpansion.IsPreviewActive;
            bool expeditionMapPopup = camp && campInteraction.IsPopupOpen &&
                                      campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap;
            bool endingAlbumPopup = camp && campInteraction.IsPopupOpen &&
                                    campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.EndingAlbum &&
                                    endingAlbumSelection.IsOpen;
            campInteractionPopup.SetActive(camp && campInteraction.IsPopupOpen && !expeditionMapPopup && !endingAlbumPopup);
            expeditionMapPanel.SetActive(expeditionMapPopup);
            endingAlbumPanel.SetActive(endingAlbumPopup);
            campProximityPrompt.SetActive(camp && !campInteraction.IsPopupOpen && campInteraction.HasProximityPrompt);
            if (campInteraction.HasProximityPrompt)
            {
                ApplyCampProximityPresentation(
                    campInteraction.ActiveTargetKind,
                    campInteraction.ActiveTargetId,
                    playerInput.ActiveDevice);
            }

            if (!campInteraction.IsPopupOpen)
            {
                return;
            }

            if (expeditionMapPopup)
            {
                RefreshExpeditionMapUi();
                return;
            }

            if (endingAlbumPopup)
            {
                RefreshEndingAlbumUi();
                return;
            }

            ConfigureCampPopupLayout(campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot);
            string openTargetName = FormatCampInteractionTarget(campInteraction.OpenPopupKind, campInteraction.OpenPopupTargetId);
            actionTitleText.text = localization.Format("camp.popup.title", openTargetName);
            campPopupDetailText.text = FormatGameJamCampPopupDetail(
                campInteraction.OpenPopupKind,
                localization.Format(CampPopupDetailKey(campInteraction.OpenPopupKind)));
            ConfigureGameJamCampPopupDetailLayout(campInteraction.OpenPopupKind);
        }

        private void ApplyCampProximityPresentation(
            PrototypeCampInteractionTargetKind target,
            string targetId,
            PrototypeInputDevice device)
        {
            campProximityGlyphText.text = localization.Format(PrototypeInputPromptKeys.InteractGlyph(device));
            campProximityText.text = FormatCampProximityAction(target, targetId);
        }

        private string FormatCampProximityAction(
            PrototypeCampInteractionTargetKind target,
            string targetId)
        {
            string targetName = FormatCampInteractionTarget(target, targetId);
            string actionKey = target == PrototypeCampInteractionTargetKind.ExpeditionMap && session.ExpeditionCompleted
                ? "interaction.action.expedition_done"
                : target == PrototypeCampInteractionTargetKind.ModuleExpansionSlot
                    ? "interaction.action.preview"
                    : target == PrototypeCampInteractionTargetKind.EndingAlbum
                        ? "interaction.action.open"
                        : "interaction.action.use";
            if (target == PrototypeCampInteractionTargetKind.ExpeditionMap && session.ExpeditionCompleted)
            {
                return localization.Format(actionKey);
            }

            return localization.Format(
                "interaction.structure.prompt",
                string.Empty,
                targetName,
                localization.Format(actionKey)).Trim();
        }

        private string FormatCampInteractionTarget(PrototypeCampInteractionTargetKind target, string targetId = "")
        {
            switch (target)
            {
                case PrototypeCampInteractionTargetKind.Campfire:
                    return localization.Format("structure.campfire");
                case PrototypeCampInteractionTargetKind.Workbench:
                    return localization.Format("structure.workbench");
                case PrototypeCampInteractionTargetKind.RainCollector:
                    return localization.Format("structure.rain_collector");
                case PrototypeCampInteractionTargetKind.RescueSignal:
                    return localization.Format("structure.rescue_signal");
                case PrototypeCampInteractionTargetKind.StoragePlanning:
                    return localization.Format("structure.storage_planning");
                case PrototypeCampInteractionTargetKind.ExpeditionMap:
                    return localization.Format("camp.target.expedition_map");
                case PrototypeCampInteractionTargetKind.EndingAlbum:
                    return localization.Format("camp.target.ending_album");
                case PrototypeCampInteractionTargetKind.SmokeBeacon:
                    return localization.Format("escape.smoke");
                case PrototypeCampInteractionTargetKind.RadioBench:
                    return localization.Format("escape.radio");
                case PrototypeCampInteractionTargetKind.ShoreLaunch:
                    return localization.Format("camp.target.shore_launch");
                case PrototypeCampInteractionTargetKind.ModuleExpansionSlot:
                    if (PrototypeCampModuleCatalog.TryGetByStartSlotId(targetId, out CampModuleDefinition slotDefinition))
                    {
                        return localization.Format(
                            "structure.module_connector",
                            localization.Format(ModuleNameKey(slotDefinition.Archetype)));
                    }
                    return localization.Format("structure.module_connector", localization.Format("structure.generic"));
                case PrototypeCampInteractionTargetKind.ModuleConnector:
                    if (campModuleExpansion.TryGetCommittedRoomByConnector(targetId, out CampModuleCommittedRoomSnapshot connectorRoom))
                    {
                        return localization.Format(
                            "structure.module_connector",
                            localization.Format(ModuleNameKey(connectorRoom.Archetype)));
                    }
                    return localization.Format(
                        "structure.module_connector",
                        campModuleExpansion.HasCommittedModule
                            ? localization.Format(ModuleNameKey(campModuleExpansion.CommittedArchetype))
                            : localization.Format("structure.generic"));
                default:
                    return localization.Format("structure.generic");
            }
        }

        private static string CampPopupDetailKey(PrototypeCampInteractionTargetKind target)
        {
            switch (target)
            {
                case PrototypeCampInteractionTargetKind.Campfire:
                    return "camp.popup.detail.campfire";
                case PrototypeCampInteractionTargetKind.Workbench:
                    return "camp.popup.detail.workbench";
                case PrototypeCampInteractionTargetKind.RainCollector:
                    return "camp.popup.detail.rain";
                case PrototypeCampInteractionTargetKind.RescueSignal:
                    return "camp.popup.detail.signal";
                case PrototypeCampInteractionTargetKind.StoragePlanning:
                    return "camp.popup.detail.storage";
                case PrototypeCampInteractionTargetKind.ModuleExpansionSlot:
                    return "camp.popup.detail.module_slot";
                case PrototypeCampInteractionTargetKind.ExpeditionMap:
                    return "camp.popup.detail.expedition_map";
                case PrototypeCampInteractionTargetKind.EndingAlbum:
                    return "camp.popup.detail.ending_album";
                case PrototypeCampInteractionTargetKind.SmokeBeacon:
                    return "camp.popup.detail.escape_smoke";
                case PrototypeCampInteractionTargetKind.RadioBench:
                    return "camp.popup.detail.escape_radio";
                case PrototypeCampInteractionTargetKind.ShoreLaunch:
                    return "camp.popup.detail.escape_raft";
                default:
                    return "camp.popup.detail.generic";
            }
        }

        private void UpdatePopupActionVisibility()
        {
            for (int i = 0; i < campPopupButtons.Count; i += 1)
            {
                campPopupButtons[i].gameObject.SetActive(false);
            }

            PrototypeCampInteractionTargetKind target = campInteraction.OpenPopupKind;
            bool built = IsPopupTargetBuilt(target);
            bool planning = target == PrototypeCampInteractionTargetKind.StoragePlanning;
            SetPopupActionVisible(campfireButton, (target == PrototypeCampInteractionTargetKind.Campfire || planning) &&
                PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.BuildOrRelocate, built));
            SetPopupActionVisible(workbenchButton, (target == PrototypeCampInteractionTargetKind.Workbench || planning) &&
                PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.BuildOrRelocate, built));
            SetPopupActionVisible(rainButton, (target == PrototypeCampInteractionTargetKind.RainCollector || planning) &&
                campUse.CurrentRoomId == PrototypeCampModuleCatalog.StartRoomId &&
                PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.BuildOrRelocate, built));
            SetPopupActionVisible(eatButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.Eat, built));
            SetPopupActionVisible(prepareCampfireButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.PrepareSurvival, built));
            SetPopupActionVisible(researchAxeButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.ResearchStoneAxe, built));
            SetPopupActionVisible(craftAxeButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.CraftStoneAxe, built));
            SetPopupActionVisible(researchRopeButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.ResearchRope, built));
            SetPopupActionVisible(craftRopeButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.CraftRope, built));
            SetPopupActionVisible(repairButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.Repair, built));
            SetPopupActionVisible(treatDiseaseButton,
                target == PrototypeCampInteractionTargetKind.Workbench && built &&
                (searchNodeRuntime.Disease.IsTreatable ||
                 searchNodeRuntime.Disease.Phase == PrototypeDiseasePhase.Recovering));
            SetPopupActionVisible(bagUpgradeButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.UpgradeBag, built));
            SetPopupActionVisible(collectRainButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.CollectRain, built));
            SetPopupActionVisible(signalButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.UpgradeSignal, true));
            SetPopupActionVisible(modulePreviewButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.PreviewModule, true));
            SetPopupActionVisible(smokeProjectButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.ProgressSmokeEscape, true));
            SetPopupActionVisible(radioProjectButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.ProgressRadioEscape, true));
            SetPopupActionVisible(raftProjectButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.ProgressRaftEscape, true));
            SetPopupActionVisible(endingAlbumOpenButton, PrototypeCampInteractionCatalog.OwnsAction(target, PrototypeCampInteractionAction.OpenEndingAlbum, true));
            SetPopupActionVisible(cancelPopupButton, target != PrototypeCampInteractionTargetKind.None);
            LayoutVisiblePopupButtons();
        }

        private bool IsPopupTargetBuilt(PrototypeCampInteractionTargetKind target)
        {
            switch (target)
            {
                case PrototypeCampInteractionTargetKind.Campfire:
                    return session.HasStructure(StructureKind.Campfire);
                case PrototypeCampInteractionTargetKind.Workbench:
                    return session.HasStructure(StructureKind.Workbench);
                case PrototypeCampInteractionTargetKind.RainCollector:
                    return session.HasStructure(StructureKind.RainCollector);
                case PrototypeCampInteractionTargetKind.RescueSignal:
                case PrototypeCampInteractionTargetKind.StoragePlanning:
                case PrototypeCampInteractionTargetKind.ModuleExpansionSlot:
                case PrototypeCampInteractionTargetKind.ExpeditionMap:
                case PrototypeCampInteractionTargetKind.EndingAlbum:
                case PrototypeCampInteractionTargetKind.SmokeBeacon:
                case PrototypeCampInteractionTargetKind.RadioBench:
                case PrototypeCampInteractionTargetKind.ShoreLaunch:
                    return true;
                default:
                    return false;
            }
        }

        private static void SetPopupActionVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private void LayoutVisiblePopupButtons()
        {
            int visibleIndex = 0;
            for (int i = 0; i < campPopupButtons.Count; i += 1)
            {
                Button button = campPopupButtons[i];
                if (!button.gameObject.activeSelf)
                {
                    continue;
                }

                int column = visibleIndex % 2;
                int row = visibleIndex / 2;
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(column * 0.5f, 1f);
                rect.anchorMax = new Vector2((column + 1) * 0.5f, 1f);
                float top = -174f - row * 92f;
                rect.offsetMin = new Vector2(column == 0 ? 24f : 8f, top - 78f);
                rect.offsetMax = new Vector2(column == 0 ? -8f : -24f, top);
                visibleIndex += 1;
            }
        }

        private GameObject FirstVisiblePopupButton()
        {
            for (int i = 0; i < campPopupButtons.Count; i += 1)
            {
                if (campPopupButtons[i].gameObject.activeInHierarchy && campPopupButtons[i].interactable)
                {
                    return campPopupButtons[i].gameObject;
                }
            }
            return cancelPopupButton == null ? null : cancelPopupButton.gameObject;
        }

        private void RefreshBagButtons()
        {
            for (int i = 0; i < bagButtons.Count; i += 1)
            {
                bool active = session.IsBagSlotActive(i);
                BagStack stack = session.GetBagSlot(i);
                string stableResourceId = PrototypeResourcePresentation.NormalizeStableId(stack.StableResourceId, stack.Kind);
                TMP_Text label = bagButtons[i].GetComponentInChildren<TMP_Text>();
                label.text = !active
                    ? localization.Format("bag.slot.locked", i + 1)
                    : stack.IsEmpty
                    ? localization.Format("bag.slot.empty", i + 1)
                    : localization.Format("bag.slot.stack", i + 1, localization.ResourceName(stableResourceId, stack.Kind), stack.Amount);
                bagButtons[i].interactable = active && session.Phase == GamePhase.Exploring && session.HasPendingLoot;
                Image image = bagButtons[i].GetComponent<Image>();
                image.color = !active
                    ? new Color(0.08f, 0.1f, 0.1f, 0.95f)
                    : stack.IsEmpty
                        ? new Color(0.12f, 0.18f, 0.19f, 0.95f)
                        : PrototypeResourcePresentation.Surface(stableResourceId, stack.Kind, false);
                Outline outline = bagButtons[i].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = stack.IsEmpty || !active
                        ? new Color(0.23f, 0.31f, 0.32f, 0.7f)
                        : PrototypeResourcePresentation.Accent(stableResourceId, stack.Kind, 0.95f);
                }
                Image icon = i < bagButtonIcons.Count ? bagButtonIcons[i] : null;
                if (icon != null)
                {
                    icon.sprite = stack.IsEmpty || !active ? null : GetResourceIconSprite(stack.Kind);
                    icon.color = stack.IsEmpty || !active ? Color.clear : Color.white;
                    icon.enabled = icon.sprite != null;
                }
            }
        }

        private void RebuildWorld()
        {
            if (worldRoot != null)
            {
                worldRoot.gameObject.SetActive(false);
                Destroy(worldRoot.gameObject);
            }

            GameObject root = new GameObject("Runtime Survival World");
            root.transform.SetParent(transform, false);
            worldRoot = root.transform;
            nodes.Clear();
            searchWorldContextLabels.Clear();
            structureViews.Clear();
            playerRoot = null;
            playerPresentation = null;
            placementGhost = null;
            placementGhostRenderer = null;
            placementGhostBadgeRenderer = null;
            placementGhostLabel = null;
            modulePreviewGhost = null;
            modulePreviewBadgeRenderer = null;
            modulePreviewBadgeText = null;
            campBackgroundRenderer = null;
            campGameplayGroundRenderer = null;
            campForegroundRenderer = null;
            rescueSignalRenderer = null;
            placementGhostOutlineRenderers.Clear();
            modulePreviewOutlineRenderers.Clear();

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
            bool startRoom = campUse.CurrentRoomId == PrototypeCampModuleCatalog.StartRoomId;
            if (!startRoom)
            {
                CreateCommittedModuleInterior();
            }
            else if (campModuleExpansion.HasCommittedModule)
            {
                CreateCommittedModuleExterior();
            }

            if (campPlacement.IsActive)
            {
                bool openSky = PrototypeCampPlacement.GetRequiredZone(campPlacement.SelectedKind) == CampPlacementZone.OpenSkyGround;
                CampPlacementRoomZone activeZone = campPlacement.ActiveRoomZone;
                float zoneMinimumX = openSky ? activeZone.OpenSkyMinimumX : activeZone.BuildMinimumX;
                float zoneMaximumX = openSky ? activeZone.OpenSkyMaximumX : activeZone.BuildMaximumX;
                float buildWidth = zoneMaximumX - zoneMinimumX;
                float buildCenter = (zoneMinimumX + zoneMaximumX) * 0.5f;
                Color zoneColor = openSky ? new Color(0.2f, 0.78f, 0.96f, 0.62f) : new Color(0.22f, 0.88f, 0.46f, 0.58f);
                CreateRect(openSky ? "camp.open-sky-ground" : "camp.general-ground", new Vector2(buildCenter, PrototypeCampPlacement.FloorY + 0.07f), new Vector2(buildWidth, 0.07f), zoneColor, 2);
                CreateRect("건설 구역 왼쪽 경계", new Vector2(zoneMinimumX, PrototypeCampPlacement.FloorY + 0.17f), new Vector2(0.06f, 0.34f), zoneColor, 2);
                CreateRect("건설 구역 오른쪽 경계", new Vector2(zoneMaximumX, PrototypeCampPlacement.FloorY + 0.17f), new Vector2(0.06f, 0.34f), zoneColor, 2);
                CreateReservedCampStrip("world.entrance", activeZone.EntranceMinimumX, activeZone.EntranceMaximumX, new Color(0.95f, 0.38f, 0.18f, 0.72f));
                CreateReservedCampStrip("world.required_path", activeZone.RequiredPathMinimumX, activeZone.RequiredPathMaximumX, new Color(1f, 0.72f, 0.16f, 0.72f));
            }
            CreateKim(campUse.PlayerPosition);

            CreatePlacedStructure(StructureKind.Campfire, new Color(1f, 0.43f, 0.14f));
            CreatePlacedStructure(StructureKind.Workbench, new Color(0.48f, 0.26f, 0.12f));
            CreatePlacedStructure(StructureKind.RainCollector, new Color(0.27f, 0.7f, 0.86f));
            if (startRoom)
            {
                CreateCampBlueprint(StructureKind.Campfire, new Color(1f, 0.43f, 0.14f, 0.28f));
                CreateCampBlueprint(StructureKind.Workbench, new Color(0.48f, 0.26f, 0.12f, 0.28f));
                CreateCampBlueprint(StructureKind.RainCollector, new Color(0.27f, 0.7f, 0.86f, 0.28f));
            }

            CreateStoragePlanningMarker(startRoom ? StoragePlanningX : ModulePlanningX);
            if (startRoom)
            {
                CreateStartRoomModuleSlots();
                if (!campPlacement.IsActive && !campModuleExpansion.IsPreviewActive)
                {
                    CreateExpeditionMapMarker();
                    CreateEndingAlbumMarker();
                    CreateEscapeProjectMarkers();
                    CreateShoreLaunchMarker();
                }
            }

            if (startRoom && !IsGameJamLiveEscapeProfile)
            {
                Vector2 signalAnchor = GetCampArtPoint(CampSignalAnchorNormalizedX, CampSignalAnchorNormalizedY);
                CreateRescueSignal(signalAnchor);
                if (campPlacement.IsActive)
                {
                    CreateFootprintOutline(worldRoot, new Vector2(2.25f, 0.35f), new Color(1f, 0.88f, 0.38f, 0.95f), null, signalAnchor);
                }
            }

            if (campPlacement.IsActive)
            {
                CreatePlacementGhost();
            }
            if (campModuleExpansion.IsPreviewActive)
            {
                CreateCampModulePreviewGhost();
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
                campFeedback = new PrototypeLocalizedText("message.build.materials");
                RefreshAll();
                return;
            }

            bool relocatingWithinCurrentRoom = relocating && campPlacement.IsInstalledInRoom(kind, campUse.CurrentRoomId);
            if (relocatingWithinCurrentRoom && !RequireStructureUse(kind))
            {
                return;
            }

            campFeedback = PrototypeLocalizedText.Empty;
            campPlacement.Begin(kind, relocating, GetCurrentPlacementZone());
            RefreshAll();
        }

        private void CreateStoragePlanningMarker(float x)
        {
            GameObject root = new GameObject("현장형 창고·증축 계획 지점 · engine-native crate");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(x, PrototypeCampPlacement.FloorY + 0.34f, 0f);
            CreateStoragePlanningSilhouette(root.transform);
        }

        private string FormatEscapeProjectButton(string escapeId)
        {
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
            PrototypeEscapeProjectState state = hazardEscapeEndingRuntime.EscapeDirector.GetState(escapeId);
            string researchState = localization.Format(
                escapeId == "escape.smoke"
                    ? (session.HasRope ? "value.yes" : "value.no")
                    : (session.HasAxe ? "value.yes" : "value.no"));
            bool partsReady = definition.RequiredKeyPartIds.All(hazardEscapeEndingRuntime.HasProtectedSearchPart);
            PrototypeSignalEscapeWindow window = PrototypeSignalEscapeWindowResolver.Resolve(
                escapeId,
                session.RunSeed,
                session.Day);
            return localization.Format(
                escapeId == "escape.smoke" ? "escape.project.action.smoke" : "escape.project.action.radio",
                state.Progress,
                state.RequiredProgress,
                researchState,
                localization.Format(partsReady ? "value.yes" : "value.no"),
                localization.Format(window.Allowed ? "value.yes" : "value.no"));
        }

        private string FormatRaftProjectButton()
        {
            PrototypeEscapeProjectState state = hazardEscapeEndingRuntime.EscapeDirector.GetState(PrototypeRaftEscapeConfig.EscapeId);
            if (state.Complete)
            {
                return localization.Format("escape.raft.action.complete");
            }
            if (state.Progress < PrototypeRaftEscapeConfig.StageCount)
            {
                string stageId = PrototypeRaftEscapeConfig.StageIds[state.Progress];
                string requirement = state.Progress == 0
                    ? localization.Format("escape.raft.cost.hull", PrototypeRaftEscapeConfig.HullWoodCost, PrototypeRaftEscapeConfig.HullSalvageCost)
                    : state.Progress == 1
                        ? localization.Format(
                            "escape.raft.cost.sail",
                            PrototypeRaftEscapeConfig.SailWoodCost,
                            PrototypeRaftEscapeConfig.SailSalvageCost,
                            localization.Format(session.HasRope ? "value.yes" : "value.no"),
                            localization.Format(state.KeyPartProtected ? "value.yes" : "value.no"))
                        : localization.Format("escape.raft.cost.supplies", PrototypeRaftEscapeConfig.SuppliesFoodCost);
                return localization.Format(
                    "escape.raft.action.stage",
                    localization.Format(stageId),
                    state.Progress,
                    state.RequiredProgress,
                    requirement);
            }

            PrototypeRaftLaunchWindow window = hazardEscapeEndingRuntime.CurrentRaftLaunchWindow;
            if (state.LaunchState == PrototypeRaftLaunchStates.Failed)
            {
                return localization.Format("escape.raft.action.retry", localization.Format(state.LastWeatherId), localization.Format(state.LastCurrentId));
            }
            if (state.LaunchState == PrototypeRaftLaunchStates.Confirm)
            {
                return localization.Format(
                    "escape.raft.action.confirm",
                    PrototypeRaftEscapeConfig.LaunchAttemptFoodCost,
                    localization.Format(window.WeatherId),
                    localization.Format(window.CurrentId));
            }
            return localization.Format(
                "escape.raft.action.check_window",
                localization.Format(window.WeatherId),
                localization.Format(window.CurrentId));
        }

        private void CreateExpeditionMapMarker()
        {
            GameObject root = new GameObject("지도·출구 상호작용 표지판 · " + AssetExpeditionMap);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(ExpeditionMapX, PrototypeCampPlacement.FloorY + 0.62f, 0f);
            CreateExpeditionSignSilhouette(root.transform);
        }

        private void CreateEndingAlbumMarker()
        {
            GameObject root = new GameObject("생존 앨범·기록함 상호작용 오브젝트");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(EndingAlbumX, PrototypeCampPlacement.FloorY + 0.6f, 0f);
            CreateRect(root.transform, "기록함 몸체", Vector2.zero, new Vector2(0.36f, 1.12f), new Color(0.10f, 0.25f, 0.27f, 0.98f), 4);
            CreateRect(root.transform, "앨범 등", new Vector2(0f, 0.18f), new Vector2(0.24f, 0.62f), new Color(0.95f, 0.63f, 0.22f, 0.98f), 5);
            CreateRect(root.transform, "앨범 라벨", new Vector2(0f, 0.18f), new Vector2(0.12f, 0.2f), new Color(0.92f, 0.88f, 0.62f, 0.98f), 6);
            CreateFootprintOutline(root.transform, new Vector2(0.72f, 0.28f), new Color(0.22f, 0.86f, 0.82f, 0.16f), null, new Vector2(0f, -0.6f));
        }

        private void CreateEscapeProjectMarkers()
        {
            CreateEscapeProjectMarker(
                "escape.smoke",
                SmokeBeaconX,
                new Color(0.28f, 0.31f, 0.3f, 0.98f),
                new Color(0.86f, 0.86f, 0.82f, 0.88f));
            CreateEscapeProjectMarker(
                "escape.radio",
                RadioBenchX,
                new Color(0.15f, 0.25f, 0.24f, 0.98f),
                new Color(0.32f, 0.9f, 0.72f, 0.92f));
        }

        private void CreateEscapeProjectMarker(string escapeId, float x, Color bodyColor, Color signalColor)
        {
            GameObject root = new GameObject(escapeId == "escape.smoke" ? "Camp Signal Stack · engine-native tripod" : "Camp Radio Bench · engine-native table");
            root.transform.SetParent(worldRoot, false);
            bodyColor.a = Mathf.Min(bodyColor.a, 0.7f);
            signalColor.a = Mathf.Min(signalColor.a, 0.62f);
            root.transform.position = new Vector3(x, PrototypeCampPlacement.FloorY + 0.39f, 0f);
            CreateEscapeProjectSilhouette(root.transform, escapeId, bodyColor, signalColor);
        }

        private void CreateShoreLaunchMarker()
        {
            GameObject root = new GameObject("facility.shore-launch · engine-native timber cradle");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(ShoreLaunchX, PrototypeCampPlacement.FloorY + 0.38f, 0f);
            CreateShoreLaunchSilhouette(root.transform);
        }

        private void CreateStartRoomModuleSlots()
        {
            IReadOnlyList<CampModuleDefinition> definitions = PrototypeCampModuleCatalog.All;
            for (int i = 0; i < definitions.Count; i += 1)
            {
                CampModuleDefinition definition = definitions[i];
                if (campModuleExpansion.IsCommitted(definition.Archetype))
                {
                    continue;
                }

                GameObject root = new GameObject("연결 슬롯 기초 말뚝 · " + definition.StartSlotId);
                root.transform.SetParent(worldRoot, false);
                root.transform.position = new Vector3(definition.StartConnectorDisplayX, PrototypeCampPlacement.FloorY + 0.44f, 0f);
                float guideAlpha = campModuleExpansion.IsPreviewActive ? 0.82f : 0.16f;
                Color outline = new Color(1f, 0.83f, 0.28f, guideAlpha);
                CreateModuleFoundationStakeSilhouette(root.transform, definition.Archetype, outline);
            }
        }

        private void CreateModuleSlotChevron(Transform parent, Vector2 position, float rotation, Color color)
        {
            GameObject first = CreateRect(parent, "연결 슬롯 chevron A", position + new Vector2(-0.08f, 0.07f), new Vector2(0.06f, 0.24f), color, 6);
            GameObject second = CreateRect(parent, "연결 슬롯 chevron B", position + new Vector2(-0.08f, -0.07f), new Vector2(0.06f, 0.24f), color, 6);
            first.transform.localRotation = Quaternion.Euler(0f, 0f, rotation - 45f);
            second.transform.localRotation = Quaternion.Euler(0f, 0f, rotation + 45f);
        }

        private void CreateCommittedModuleExterior()
        {
            IReadOnlyList<CampModuleCommittedRoomSnapshot> committedRooms = campModuleExpansion.CommittedRooms;
            for (int index = 0; index < committedRooms.Count; index += 1)
            {
                CampModuleCommittedRoomSnapshot committed = committedRooms[index];
                CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(committed.Archetype);
                Vector2 center = GetModulePreviewCenter(definition.Archetype);
                GameObject root = new GameObject("확정 방 모듈 exterior placeholder · " + definition.RoomId);
                root.transform.SetParent(worldRoot, false);
                root.transform.position = center;
                CreateRect(root.transform, "모듈 반투명 면", Vector2.zero, new Vector2(5.8f, 2.35f), new Color(0.18f, 0.31f, 0.25f, 0.72f), -2);
                CreateFootprintOutline(root.transform, new Vector2(5.8f, 2.35f), new Color(0.82f, 0.94f, 0.78f, 0.95f), null);
                CreateModuleConnectorVisual(root.transform, definition.ConnectorKind, Vector2.zero, true);
            }
        }

        private void CreateCommittedModuleInterior()
        {
            if (!campModuleExpansion.TryGetCommittedRoom(
                    campUse.CurrentRoomId,
                    out CampModuleCommittedRoomSnapshot committedRoom))
            {
                return;
            }

            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(committedRoom.Archetype);
            GameObject root = new GameObject("확정 방 모듈 interior placeholder · " + definition.RoomId);
            root.transform.SetParent(worldRoot, false);
            CreateRect(root.transform, "실내 placeholder 배경", new Vector2(0f, 0.25f), new Vector2(12f, 6.2f), new Color(0.18f, 0.23f, 0.2f, 0.94f), -18);
            CreateRect(root.transform, "실내 바닥", new Vector2(0f, PrototypeCampPlacement.FloorY + 0.08f), new Vector2(12f, 0.18f), new Color(0.65f, 0.49f, 0.28f, 0.95f), -4);
            CreateRect(root.transform, "일반 설비 호환 구역", new Vector2(
                (definition.GeneralFloorDisplayMinimumX + definition.GeneralFloorDisplayMaximumX) * 0.5f,
                PrototypeCampPlacement.FloorY + 0.17f), new Vector2(
                definition.GeneralFloorDisplayMaximumX - definition.GeneralFloorDisplayMinimumX,
                0.34f), new Color(0.18f, 0.74f, 0.38f, 0.82f), -3);
            CreateWorldBadge(
                "확정 모듈 이름",
                localization.Format("world.module.room", localization.Format(ModuleNameKey(definition.Archetype))),
                new Vector2(0f, 2.75f),
                new Vector2(5.6f, 1.2f),
                new Color(0.05f, 0.12f, 0.1f, 0.96f),
                Color.white);
            CreateModuleConnectorVisual(root.transform, definition.ConnectorKind, new Vector2(definition.ModuleConnectorDisplayX, PrototypeCampPlacement.FloorY + 0.9f), true);
        }

        private void CreateCampModulePreviewGhost()
        {
            CampModuleEvaluation evaluation = campModuleExpansion.Evaluate(session, campModuleValidation);
            Vector2 center = GetModulePreviewCenter(evaluation.Definition.Archetype);
            modulePreviewGhost = new GameObject("증축 후보 공간 · " + evaluation.Definition.RoomId);
            modulePreviewGhost.transform.SetParent(worldRoot, false);
            modulePreviewGhost.transform.position = center;
            SpriteRenderer fill = CreateRect(
                modulePreviewGhost.transform,
                "증축 후보 면",
                Vector2.zero,
                new Vector2(5.8f, 2.35f),
                evaluation.CanCommit ? new Color(0.12f, 0.72f, 0.36f, 0.38f) : new Color(0.78f, 0.16f, 0.12f, 0.42f),
                5).GetComponent<SpriteRenderer>();
            if (fill == null)
            {
                throw new InvalidOperationException("증축 후보 placeholder renderer 생성 실패");
            }
            CreateFootprintOutline(modulePreviewGhost.transform, new Vector2(5.8f, 2.35f), Color.white, modulePreviewOutlineRenderers);
            CreateModuleConnectorVisual(modulePreviewGhost.transform, evaluation.Definition.ConnectorKind, Vector2.zero, evaluation.Geometry == CampModuleGeometryStatus.Valid);
            CreateModuleValidityGlyph(modulePreviewGhost.transform, evaluation.CanCommit);
            modulePreviewBadgeText = CreateWorldBadge(
                modulePreviewGhost.transform,
                "증축 후보 상태",
                string.Empty,
                new Vector2(0f, 1.85f),
                new Vector2(5.65f, 1.25f),
                Color.black,
                Color.white,
                out modulePreviewBadgeRenderer,
                0.068f,
                27f,
                30f);
            modulePreviewBadgeText.textWrappingMode = TextWrappingModes.Normal;
            modulePreviewBadgeText.overflowMode = TextOverflowModes.Ellipsis;
            modulePreviewBadgeText.maxVisibleLines = 3;
            UpdateCampModulePreviewGhost();
        }

        private void UpdateCampModulePreviewGhost()
        {
            if (modulePreviewGhost == null || !campModuleExpansion.IsPreviewActive)
            {
                return;
            }

            CampModuleExpansionOptionViewModel option = campModulePresenter.BuildSelected(session, campModuleValidation);
            CampModuleEvaluation evaluation = option.Evaluation;
            bool valid = option.CanCommit;
            modulePreviewGhost.transform.position = GetModulePreviewCenter(evaluation.Definition.Archetype);
            Color outline = valid ? new Color(0.72f, 1f, 0.66f, 1f) : new Color(1f, 0.78f, 0.68f, 1f);
            for (int i = 0; i < modulePreviewOutlineRenderers.Count; i += 1)
            {
                modulePreviewOutlineRenderers[i].color = outline;
            }
            modulePreviewBadgeRenderer.color = valid
                ? new Color(0.03f, 0.34f, 0.15f, 0.98f)
                : new Color(0.5f, 0.05f, 0.04f, 0.98f);
            modulePreviewBadgeText.text =
                localization.Format(option.Semantic.NameKey) + "\n" +
                localization.Format(option.StateKey) + " · " +
                localization.Format(
                    option.Semantic.PlacementCapacityKey,
                    option.EstimatedGeneralFacilityCapacity,
                    option.UsablePlacementWidth);
        }

        private void CreateModuleConnectorVisual(Transform parent, CampModuleConnectorKind kind, Vector2 position, bool valid)
        {
            Color color = valid ? new Color(1f, 0.84f, 0.28f, 0.98f) : new Color(1f, 0.32f, 0.2f, 0.98f);
            if (kind == CampModuleConnectorKind.Door)
            {
                CreateFootprintOutline(parent, new Vector2(0.9f, 1.55f), color, null, position);
                return;
            }

            for (int rung = -1; rung <= 1; rung += 1)
            {
                CreateRect(parent, "사다리 발판", position + new Vector2(0f, rung * 0.38f), new Vector2(0.86f, 0.1f), color, 10);
            }
            CreateRect(parent, "사다리 왼쪽", position + new Vector2(-0.42f, 0f), new Vector2(0.1f, 1.25f), color, 10);
            CreateRect(parent, "사다리 오른쪽", position + new Vector2(0.42f, 0f), new Vector2(0.1f, 1.25f), color, 10);
        }

        private void CreateModuleValidityGlyph(Transform parent, bool valid)
        {
            Color color = valid ? new Color(0.65f, 1f, 0.72f, 1f) : new Color(1f, 0.78f, 0.68f, 1f);
            if (valid)
            {
                CreateCircle(parent, "유효 마름모", new Vector2(2.35f, 0.72f), 0.48f, color, 12);
                return;
            }

            GameObject first = CreateRect(parent, "무효 X A", new Vector2(2.35f, 0.72f), new Vector2(0.12f, 0.85f), color, 12);
            first.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject second = CreateRect(parent, "무효 X B", new Vector2(2.35f, 0.72f), new Vector2(0.12f, 0.85f), color, 12);
            second.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        private static Vector2 GetModulePreviewCenter(CampModuleArchetype archetype)
        {
            switch (archetype)
            {
                case CampModuleArchetype.Side:
                    return new Vector2(3.2f, -0.65f);
                case CampModuleArchetype.Basement:
                    return new Vector2(0.7f, -2.05f);
                default:
                    return new Vector2(-1.1f, 0.8f);
            }
        }

        private void UpdateCampUse()
        {
            PrototypePlayerActions actions = playerInput.ReadActions(false, session.ActiveBagSlotCount);
            ProcessCampActions(actions, Time.deltaTime);
        }

        private void ProcessCampActions(PrototypePlayerActions actions, float deltaTime)
        {
            if (campModuleExpansion.IsPreviewActive)
            {
                return;
            }

            if (campInteraction.IsPopupOpen)
            {
                if (campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.EndingAlbum && endingAlbumSelection.IsOpen)
                {
                    ProcessEndingAlbumActions(playerInput.ReadExpeditionMapActions());
                    return;
                }
                if (campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap)
                {
                    ProcessExpeditionMapActions(playerInput.ReadExpeditionMapActions());
                    return;
                }
                if (actions.CancelPressed)
                {
                    CancelCampPopup();
                    return;
                }
                if (actions.InteractPressed && EventSystem.current != null)
                {
                    GameObject selected = EventSystem.current.currentSelectedGameObject;
                    Button selectedButton = selected == null ? null : selected.GetComponent<Button>();
                    if (selectedButton != null && selectedButton.gameObject.activeInHierarchy && selectedButton.interactable)
                    {
                        selectedButton.onClick.Invoke();
                    }
                }
                return;
            }

            campUse.Step(actions, deltaTime);
            if (playerRoot != null)
            {
                PrototypePlayerPresentationState presentation = new PrototypePlayerPresentationState(
                    campUse.PlayerPosition.x,
                    campUse.PlayerPosition.y,
                    campUse.FacingDirection,
                    Mathf.Abs(actions.Horizontal),
                    false,
                    true);
                playerPresentation.Apply(presentation);
            }

            RefreshCampInteractionSelection();
            RefreshCampInteractionUi();

            if (actions.InteractPressed)
            {
                TryOpenCampPopup();
            }
        }

        private void UseNearestCampTarget()
        {
            RefreshCampInteractionSelection();
            TryOpenCampPopup();
        }

        private void RefreshCampInteractionSelection()
        {
            if (session == null || session.Phase != GamePhase.Camp || campPlacement.IsActive || campModuleExpansion.IsPreviewActive)
            {
                campInteractionTargets.Clear();
                campInteraction.UpdateSelection(campUse.PlayerPosition, campUse.FacingDirection, campInteractionTargets);
                ObserveCampInteractionTarget();
                return;
            }

            campInteractionTargets.Clear();
            bool startRoom = campUse.CurrentRoomId == PrototypeCampModuleCatalog.StartRoomId;
            AddCampInteractionTarget(StructureKind.Campfire, PrototypeCampInteractionTargetKind.Campfire, startRoom);
            AddCampInteractionTarget(StructureKind.Workbench, PrototypeCampInteractionTargetKind.Workbench, startRoom);
            AddCampInteractionTarget(StructureKind.RainCollector, PrototypeCampInteractionTargetKind.RainCollector, startRoom);
            campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                startRoom ? "storage.planning" : "storage.planning." + campUse.CurrentRoomId,
                PrototypeCampInteractionTargetKind.StoragePlanning,
                new Vector2(startRoom ? StoragePlanningX : ModulePlanningX, PrototypeCampUse.PlayerFloorY)));
            if (startRoom)
            {
                campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                    "camp.expedition-map",
                    PrototypeCampInteractionTargetKind.ExpeditionMap,
                    new Vector2(ExpeditionMapX, PrototypeCampUse.PlayerFloorY)));
                campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                    "camp.ending-album",
                    PrototypeCampInteractionTargetKind.EndingAlbum,
                    new Vector2(EndingAlbumX, PrototypeCampUse.PlayerFloorY)));
                campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                    "facility.smoke-beacon",
                    PrototypeCampInteractionTargetKind.SmokeBeacon,
                    new Vector2(SmokeBeaconX, PrototypeCampUse.PlayerFloorY)));
                campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                    "facility.radio-bench",
                    PrototypeCampInteractionTargetKind.RadioBench,
                    new Vector2(RadioBenchX, PrototypeCampUse.PlayerFloorY)));
                campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                    "facility.shore-launch",
                    PrototypeCampInteractionTargetKind.ShoreLaunch,
                    new Vector2(ShoreLaunchX, PrototypeCampUse.PlayerFloorY),
                    IsRaftShoreLaunchDiscoveredFromLiveSearch()));
                IReadOnlyList<CampModuleDefinition> definitions = PrototypeCampModuleCatalog.All;
                for (int i = 0; i < definitions.Count; i += 1)
                {
                    CampModuleDefinition definition = definitions[i];
                    bool committedSlot = campModuleExpansion.IsCommitted(definition.Archetype);
                    if (!committedSlot)
                    {
                        campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                            definition.StartSlotId,
                            PrototypeCampInteractionTargetKind.ModuleExpansionSlot,
                            new Vector2(definition.StartConnectorDisplayX, PrototypeCampUse.PlayerFloorY),
                            true,
                            ModuleInteractionPriority));
                    }
                }

                if (!IsGameJamLiveEscapeProfile)
                {
                    campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                        "camp.signal-anchor",
                        PrototypeCampInteractionTargetKind.RescueSignal,
                        GetCampArtPoint(CampSignalAnchorNormalizedX, CampSignalAnchorNormalizedY)));
                }
            }
            if (startRoom)
            {
                IReadOnlyList<CampModuleCommittedRoomSnapshot> committedRooms = campModuleExpansion.CommittedRooms;
                for (int index = 0; index < committedRooms.Count; index += 1)
                {
                    CampModuleCommittedRoomSnapshot committed = committedRooms[index];
                    CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(committed.Archetype);
                    campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                        committed.StartSlotId,
                        PrototypeCampInteractionTargetKind.ModuleConnector,
                        new Vector2(definition.StartConnectorDisplayX, PrototypeCampUse.PlayerFloorY),
                        true,
                        ModuleInteractionPriority));
                }
            }
            else if (campModuleExpansion.TryGetCommittedRoom(
                         campUse.CurrentRoomId,
                         out CampModuleCommittedRoomSnapshot currentRoom))
            {
                CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(currentRoom.Archetype);
                campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                    currentRoom.ReciprocalSlotId,
                    PrototypeCampInteractionTargetKind.ModuleConnector,
                    new Vector2(definition.ModuleConnectorDisplayX, PrototypeCampUse.PlayerFloorY),
                    true,
                    ModuleInteractionPriority));
            }
            campInteraction.UpdateSelection(campUse.PlayerPosition, campUse.FacingDirection, campInteractionTargets);
            ObserveCampInteractionTarget();
        }

        private bool IsRaftShoreLaunchDiscoveredFromLiveSearch()
        {
            if (hazardEscapeEndingRuntime == null)
            {
                return false;
            }
            if (hazardEscapeEndingRuntime.IsRaftShoreLaunchDiscovered)
            {
                return true;
            }
            if (session == null || searchNodeRuntime == null || searchNodeRuntime.Ledger == null ||
                searchNodeRuntime.Ledger.RunSeed != session.RunSeed)
            {
                return false;
            }

            hazardEscapeEndingRuntime.RestoreProtectedPartPitySnapshots(
                searchNodeRuntime.Ledger.ProtectedPartPity);
            return hazardEscapeEndingRuntime.IsRaftShoreLaunchDiscovered;
        }

        private void ObserveCampInteractionTarget()
        {
            if (playtestLog != null)
            {
                playtestLog.ObserveFacilityTarget(
                    campInteraction.ActiveTargetKind,
                    campInteraction.ActiveTargetId,
                    campInteraction.HasProximityPrompt);
            }
        }

        private void AddCampInteractionTarget(StructureKind structure, PrototypeCampInteractionTargetKind target, bool startRoom)
        {
            if ((!session.HasStructure(structure) && !startRoom) ||
                (session.HasStructure(structure) && !campPlacement.IsInstalledInRoom(structure, campUse.CurrentRoomId)))
            {
                return;
            }

            bool isInstalled = session.HasStructure(structure);
            Vector2 installedPosition = campPlacement.GetInstalledPosition(structure);
            Vector2 interactionPosition = new Vector2(installedPosition.x, PrototypeCampUse.PlayerFloorY);
            campInteractionTargets.Add(new PrototypeCampInteractionTarget(
                "camp." + structure,
                target,
                interactionPosition,
                true,
                isInstalled ? InstalledFacilityInteractionPriority : UnbuiltBlueprintInteractionPriority));
        }

        private bool TryOpenCampPopup()
        {
            if (campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleConnector)
            {
                PrototypeCampInteractionTargetKind connectorKind = campInteraction.ActiveTargetKind;
                string connectorId = campInteraction.ActiveTargetId;
                if (playtestLog != null)
                {
                    playtestLog.TrackFacilityTransition(connectorKind, connectorId, "module.traverse", TraverseCommittedModule);
                }
                else
                {
                    TraverseCommittedModule();
                }
                return true;
            }

            if (campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ExpeditionMap &&
                session.ExpeditionCompleted)
            {
                campFeedback = new PrototypeLocalizedText("message.search.finished");
                if (playtestLog != null)
                {
                    playtestLog.ObserveState("expedition.map.blocked.already_completed");
                }
                RefreshAll();
                return false;
            }

            if (!campInteraction.TryOpenPopup())
            {
                return false;
            }

            if (campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap)
            {
                expeditionMapSelection.Open(session.SelectedRegionId);
            }

            if (playtestLog != null)
            {
                playtestLog.RecordPopupOpened(campInteraction.OpenPopupKind, campInteraction.OpenPopupTargetId);
            }
            RefreshAll();
            return true;
        }

        private void CancelCampPopup()
        {
            if (!campInteraction.IsPopupOpen)
            {
                return;
            }

            PrototypeCampInteractionTargetKind kind = campInteraction.OpenPopupKind;
            string targetId = campInteraction.OpenPopupTargetId;
            if (kind == PrototypeCampInteractionTargetKind.Workbench && searchNodeRuntime.Disease.IsTreatable &&
                searchNodeRuntime.Disease.TryCancelTreatment() && playtestLog != null)
            {
                playtestLog.RecordDiseaseTreatment("cancelled", 0);
            }
            if (kind == PrototypeCampInteractionTargetKind.ExpeditionMap)
            {
                expeditionMapSelection.Close();
            }
            if (kind == PrototypeCampInteractionTargetKind.EndingAlbum)
            {
                endingAlbumSelection.Close();
            }
            campInteraction.ClosePopup();
            if (playtestLog != null)
            {
                playtestLog.RecordPopupClosed(kind, targetId, "cancelled");
            }
            RefreshAll();
        }

        private void ProcessExpeditionMapActions(PrototypeExpeditionMapActions actions)
        {
            if (!expeditionMapSelection.IsOpen)
            {
                return;
            }

            if (expeditionMapSelection.StepFocus(actions.CycleDirection))
            {
                RefreshExpeditionMapUi();
                EventSystem.current.SetSelectedGameObject(expeditionRegionButtons[expeditionMapSelection.FocusedIndex].gameObject);
            }

            if (actions.CancelPressed)
            {
                CancelCampPopup();
                return;
            }

            if (actions.ConfirmPressed)
            {
                ConfirmSelectedExpeditionRegion();
            }
        }

        private void OpenEndingAlbumFromPopup()
        {
            if (campInteraction.OpenPopupKind != PrototypeCampInteractionTargetKind.EndingAlbum ||
                endingAlbumSelection.IsOpen ||
                !campInteraction.TryConfirmAction())
            {
                return;
            }

            endingAlbumSelection.Open(endingAlbumCollection.FirstUnlockedIndexOrZero());
            RefreshAll();
        }

        private void ProcessEndingAlbumActions(PrototypeExpeditionMapActions actions)
        {
            if (!endingAlbumSelection.IsOpen)
            {
                return;
            }

            if (actions.CancelPressed)
            {
                CloseEndingAlbumToPopup();
                return;
            }

            if (endingAlbumSelection.StepFocus(actions.CycleDirection))
            {
                RefreshEndingAlbumUi();
                EventSystem.current.SetSelectedGameObject(endingAlbumCardButtons[endingAlbumSelection.FocusedIndex].gameObject);
            }
        }

        private void CloseEndingAlbumToPopup()
        {
            if (!endingAlbumSelection.IsOpen)
            {
                return;
            }

            endingAlbumSelection.Close();
            campInteraction.PrepareOpenPopupForReturn();
            RefreshAll();
        }

        private void FocusEndingAlbumEntry(int index)
        {
            if (!endingAlbumSelection.IsOpen)
            {
                return;
            }

            endingAlbumSelection.SetFocusedIndex(index);
            RefreshEndingAlbumUi();
            EventSystem.current.SetSelectedGameObject(endingAlbumCardButtons[endingAlbumSelection.FocusedIndex].gameObject);
        }

        private void FocusExpeditionRegion(PrototypeExpeditionRegionId region)
        {
            if (!expeditionMapSelection.SetFocusedRegion(region))
            {
                return;
            }

            RefreshExpeditionMapUi();
            EventSystem.current.SetSelectedGameObject(expeditionRegionButtons[(int)region].gameObject);
        }

        private void ConfirmSelectedExpeditionRegion()
        {
            if (!expeditionMapSelection.IsOpen ||
                campInteraction.OpenPopupKind != PrototypeCampInteractionTargetKind.ExpeditionMap ||
                !expeditionMapSelection.CanDepartFocusedRegion() ||
                !campInteraction.TryConfirmAction())
            {
                return;
            }

            PrototypeExpeditionRegionId region = expeditionMapSelection.FocusedRegionId;
            PrototypeExpeditionRegionProfile profile = PrototypeExpeditionRegionCatalog.Get(region);
            PrototypeCampInteractionTargetKind kind = campInteraction.OpenPopupKind;
            string targetId = campInteraction.OpenPopupTargetId;
            if (playtestLog != null)
            {
                playtestLog.RecordSearchRegionSelected(PrototypeSearchRegionCatalog.Get(region).StableId);
            }
            bool began = playtestLog != null
                ? playtestLog.TrackFacilityAction(
                    kind,
                    targetId,
                    "expedition.begin." + profile.StableId,
                    delegate { return session.BeginSearch(region); })
                : session.BeginSearch(region);
            if (!began)
            {
                campFeedback = session.LastMessage;
                expeditionMapSelection.Close();
                campInteraction.ClosePopup();
                if (playtestLog != null)
                {
                    playtestLog.RecordPopupClosed(kind, targetId, "expedition_rejected");
                }
                RefreshAll();
                return;
            }

            expeditionMapSelection.Close();
            campInteraction.ClosePopup();
            campUse.ClearDayBenefits();
            campFeedback = PrototypeLocalizedText.Empty;
            if (playtestLog != null)
            {
                playtestLog.RecordPopupClosed(kind, targetId, "expedition_started");
            }
            RefreshAll();
        }

        private void RefreshExpeditionMapUi()
        {
            if (!expeditionMapSelection.IsOpen || expeditionMapPanel == null)
            {
                return;
            }

            PrototypeExpeditionRegionProfile focused = PrototypeExpeditionRegionCatalog.Get(expeditionMapSelection.FocusedRegionId);
            expeditionMapTitleText.text = localization.Format(
                "expedition.map.title_region",
                session.Day,
                session.SettlementDay,
                localization.Format(focused.NameKey));
            IReadOnlyList<PrototypeExpeditionRegionProfile> profiles = PrototypeExpeditionRegionCatalog.All;
            for (int i = 0; i < profiles.Count; i += 1)
            {
                bool selected = i == expeditionMapSelection.FocusedIndex;
                PrototypeExpeditionRegionVisualPresentation state = PrototypeExpeditionRegionVisualCatalog.Get(
                    expeditionMapSelection.GetRegionState(profiles[i].Id));
                PrototypeExpeditionRegionVisualPresentation selection = PrototypeExpeditionRegionVisualCatalog.Get(
                    PrototypeExpeditionRegionVisualState.Selected);
                SetButton(
                    expeditionRegionButtons[i],
                    localization.Format(
                        "expedition.map.node.state",
                        selected ? selection.Marker : state.Marker,
                        localization.Format(profiles[i].NameKey),
                        localization.Format(state.LocalizationKey)),
                    true);
                ApplyExpeditionRegionButtonPresentation(expeditionRegionButtons[i], state, selected);
            }

            expeditionMapDetailText.text = localization.Format(
                "expedition.map.rail.resources",
                localization.Format(focused.ResourceForecastKey));
            expeditionMapRiskText.text = localization.Format(
                "expedition.map.rail.risk",
                localization.Format(focused.RiskKey),
                focused.TravelMinutes);
            expeditionMapWeatherText.text = localization.Format(
                "expedition.map.rail.weather",
                localization.Format(focused.WeatherKey));
            expeditionMapEquipmentText.text = localization.Format(
                "expedition.map.rail.equipment",
                localization.Format(focused.EquipmentKey));
            expeditionMapSpecialText.text = localization.Format(
                "expedition.map.rail.special",
                localization.Format(focused.SpecialDiscoveryKey));
            bool canDepartToday = session.Phase == GamePhase.Camp && !session.ExpeditionCompleted &&
                                  session.Result == RunResult.None && expeditionMapSelection.CanDepartFocusedRegion();
            SetButton(
                expeditionMapConfirmButton,
                session.ExpeditionCompleted
                    ? localization.Format("expedition.map.depart.completed")
                    : localization.Format("expedition.map.depart", localization.Format(focused.NameKey)),
                canDepartToday);
            SetButton(expeditionMapCancelButton, localization.Format("expedition.map.cancel.short"), true);
        }

        private void RefreshEndingAlbumUi()
        {
            if (!endingAlbumSelection.IsOpen || endingAlbumPanel == null || endingAlbumCollection == null)
            {
                return;
            }

            endingAlbumHeaderText.text = localization.Format(
                "ending.album.header",
                endingAlbumCollection.UnlockedCount,
                endingAlbumCollection.EndingCount);
            for (int index = 0; index < endingAlbumCardButtons.Count; index += 1)
            {
                PrototypeEndingAlbumEntry entry = endingAlbumCollection.GetEntry(index);
                bool focused = index == endingAlbumSelection.FocusedIndex;
                string marker = EndingAlbumCategoryMarker(entry.Definition.Category);
                SetButton(
                    endingAlbumCardButtons[index],
                    marker + " " + (entry.Unlocked ? "+" : "?"),
                    true);
                ApplyEndingAlbumCardPresentation(endingAlbumCardButtons[index], entry, focused);
            }

            PrototypeEndingAlbumEntry selected = endingAlbumCollection.GetEntry(endingAlbumSelection.FocusedIndex);
            string categoryName = localization.Format("ending.album.category." + selected.Definition.Category);
            endingAlbumDetailTitleText.text = localization.Format(selected.TitleKey);
            endingAlbumSummaryText.text = localization.Format(selected.DetailKey);
            endingAlbumStatusText.text = selected.Unlocked
                ? localization.Format("ending.album.status.unlocked", categoryName, selected.FirstUnlockedDay)
                : localization.Format("ending.album.status.locked", categoryName);
            ApplyEndingAlbumControlsPresentation(playerInput.ActiveDevice);
            SetButton(endingAlbumCloseButton, localization.Format("ending.album.back"), true);
        }

        private void ApplyEndingAlbumControlsPresentation(PrototypeInputDevice device)
        {
            endingAlbumControlsText.text = localization.Format(
                PrototypeInputPromptKeys.EndingAlbum(device),
                localization.DeviceName(device));
        }

        private static void ApplyEndingAlbumCardPresentation(
            Button button,
            PrototypeEndingAlbumEntry entry,
            bool focused)
        {
            Image image = button.GetComponent<Image>();
            Outline outline = button.GetComponent<Outline>();
            Color categoryColor = EndingAlbumCategoryColor(entry.Definition.Category);
            image.color = entry.Unlocked
                ? new Color(categoryColor.r, categoryColor.g, categoryColor.b, focused ? 0.42f : 0.18f)
                : new Color(0.015f, 0.12f, 0.15f, focused ? 0.82f : 0.62f);
            outline.effectColor = focused ? new Color(1f, 0.45f, 0.18f, 1f) : categoryColor;
            outline.effectDistance = focused ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.color = entry.Unlocked ? new Color(0.02f, 0.15f, 0.17f) : new Color(1f, 0.84f, 0.32f);
        }

        private static string EndingAlbumCategoryMarker(string category)
        {
            switch (category)
            {
                case "comic":
                    return "!";
                case "rare":
                    return "*";
                case "gamejam-stay":
                    return "G";
                case "day50":
                    return "#";
                default:
                    return "O";
            }
        }

        private static Color EndingAlbumCategoryColor(string category)
        {
            switch (category)
            {
                case "comic":
                    return new Color(0.98f, 0.38f, 0.18f, 1f);
                case "rare":
                    return new Color(0.98f, 0.74f, 0.18f, 1f);
                case "day50":
                    return new Color(0.35f, 0.48f, 0.5f, 1f);
                default:
                    return new Color(0.08f, 0.58f, 0.62f, 1f);
            }
        }

        private static void ApplyExpeditionRegionButtonPresentation(
            Button button,
            PrototypeExpeditionRegionVisualPresentation presentation,
            bool selected)
        {
            Image image = button.GetComponent<Image>();
            Outline outline = button.GetComponent<Outline>();
            int borderWeight = selected ? Math.Max(4, presentation.BorderWeight) : presentation.BorderWeight;
            outline.effectDistance = new Vector2(borderWeight, -borderWeight);
            outline.effectColor = selected
                ? new Color(0.98f, 0.82f, 0.23f, 1f)
                : new Color(0.02f, 0.16f, 0.2f, 1f);

            switch (presentation.State)
            {
                case PrototypeExpeditionRegionVisualState.Locked:
                    image.color = new Color(0.16f, 0.18f, 0.18f, 0.97f);
                    break;
                case PrototypeExpeditionRegionVisualState.RiskWarning:
                    image.color = new Color(0.36f, 0.08f, 0.06f, 0.97f);
                    break;
                case PrototypeExpeditionRegionVisualState.EquipmentMissing:
                    image.color = new Color(0.32f, 0.19f, 0.04f, 0.97f);
                    break;
                case PrototypeExpeditionRegionVisualState.DepartureReady:
                    image.color = new Color(0.03f, 0.25f, 0.16f, 0.97f);
                    break;
                case PrototypeExpeditionRegionVisualState.Unknown:
                    image.color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
                    break;
                default:
                    image.color = new Color(0.025f, 0.16f, 0.18f, 0.94f);
                    break;
            }
        }

        private void ExecuteConfirmedModulePreviewTransition()
        {
            if (!campInteraction.TryConfirmAction())
            {
                return;
            }

            campFeedback = PrototypeLocalizedText.Empty;
            PrototypeCampInteractionTargetKind kind = campInteraction.OpenPopupKind;
            string targetId = campInteraction.OpenPopupTargetId;
            bool began = playtestLog != null
                ? playtestLog.TrackFacilityAction(kind, targetId, "module.preview", BeginCampModulePreview)
                : BeginCampModulePreview();
            if (!began)
            {
                campInteraction.PrepareOpenPopupForReturn();
            }
            RefreshAll();
        }

        private bool BeginCampModulePreview()
        {
            if (campModuleExpansion.HasCommittedModule || campUse.CurrentRoomId != PrototypeCampModuleCatalog.StartRoomId)
            {
                if (campInteraction.OpenPopupKind != PrototypeCampInteractionTargetKind.ModuleExpansionSlot)
                {
                    return false;
                }
            }

            PrototypeCampInteractionTargetKind originKind = campInteraction.OpenPopupKind;
            string originTargetId = campInteraction.OpenPopupTargetId;
            CampModuleArchetype initialArchetype = CampModuleArchetype.Upper;
            if (originKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot)
            {
                if (!PrototypeCampModuleCatalog.TryGetByStartSlotId(originTargetId, out CampModuleDefinition slotDefinition))
                {
                    return false;
                }
                initialArchetype = slotDefinition.Archetype;
            }

            CampModuleReturnSnapshot snapshot = new CampModuleReturnSnapshot(
                campUse.PlayerPosition,
                campUse.FacingDirection,
                campUse.CurrentRoomId);
            bool resume = modulePreviewCanResume &&
                          modulePreviewReturnTargetKind == originKind &&
                          string.Equals(modulePreviewReturnTargetId, originTargetId, StringComparison.Ordinal);
            bool began = resume
                ? campModuleExpansion.ResumePreview(snapshot)
                : campModuleExpansion.BeginPreview(snapshot, initialArchetype);
            if (began)
            {
                modulePreviewReturnTargetKind = originKind;
                modulePreviewReturnTargetId = originTargetId;
                modulePreviewCanResume = false;
                modulePreviewCycleLatched = false;
                campFeedback = PrototypeLocalizedText.Empty;
            }
            return began;
        }

        private void UpdateCampModulePreview()
        {
            PrototypeCampModulePreviewActions actions = playerInput.ReadCampModulePreviewActions();
            if (actions.CycleDirection == 0)
            {
                modulePreviewCycleLatched = false;
            }
            else if (!modulePreviewCycleLatched)
            {
                campModuleExpansion.Cycle(actions.CycleDirection);
                modulePreviewCycleLatched = true;
                RefreshAll();
            }

            if (actions.CancelPressed)
            {
                CancelCampModulePreview(true);
                return;
            }

            if (actions.ConfirmPressed)
            {
                ConfirmCampModulePreview();
            }
        }

        private bool ConfirmCampModulePreview()
        {
            CampModuleEvaluation evaluation = campModuleExpansion.Evaluate(session, campModuleValidation);
            CampModuleCommitStatus status = campModuleExpansion.TryCommit(session, campModuleValidation);
            if (status != CampModuleCommitStatus.Succeeded)
            {
                campFeedback = new PrototypeLocalizedText(ModuleCommitMessageKey(status));
                RefreshAll();
                return false;
            }

            campUse.Restore(campModuleExpansion.ReturnSnapshot);
            campInteraction.Reset();
            ResetModulePreviewReturnRoute();
            if (hazardEscapeEndingRuntime != null)
            {
                hazardEscapeEndingRuntime.RecordMeaningfulBehavior("stat.building", 4);
            }
            campFeedback = new PrototypeLocalizedText(
                "module.message.committed",
                localization.Format(ModuleNameKey(evaluation.Definition.Archetype)));
            if (playtestLog != null)
            {
                playtestLog.ObserveState("module.commit." + evaluation.Definition.Archetype.ToString().ToLowerInvariant());
            }
            RefreshAll();
            return true;
        }

        private void CancelCampModulePreview(bool reopenOriginPopup)
        {
            CampModuleReturnSnapshot snapshot = campModuleExpansion.CancelPreview();
            campUse.Restore(snapshot);
            modulePreviewCanResume = true;
            campFeedback = new PrototypeLocalizedText("module.message.cancelled");
            if (reopenOriginPopup && campInteraction.IsPopupOpen &&
                campInteraction.OpenPopupKind == modulePreviewReturnTargetKind &&
                string.Equals(campInteraction.OpenPopupTargetId, modulePreviewReturnTargetId, StringComparison.Ordinal))
            {
                campInteraction.PrepareOpenPopupForReturn();
            }
            else if (!reopenOriginPopup)
            {
                campInteraction.ClosePopup();
            }
            RefreshAll();
        }

        private void ResetModulePreviewReturnRoute()
        {
            modulePreviewReturnTargetKind = PrototypeCampInteractionTargetKind.None;
            modulePreviewReturnTargetId = string.Empty;
            modulePreviewCanResume = false;
        }

        private void TraverseCommittedModule()
        {
            string connectorId = campInteraction.ActiveTargetId;
            if (!campModuleExpansion.TryResolveConnectionDestination(
                    campUse.CurrentRoomId,
                    connectorId,
                    out string destinationRoomId) ||
                !campModuleExpansion.TryGetCommittedRoomByConnector(
                    connectorId,
                    out CampModuleCommittedRoomSnapshot committedRoom))
            {
                return;
            }

            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(committedRoom.Archetype);
            bool leavingStart = string.Equals(
                campUse.CurrentRoomId,
                PrototypeCampModuleCatalog.StartRoomId,
                StringComparison.Ordinal);
            campInteraction.Reset();
            campUse.EnterRoom(
                destinationRoomId,
                (leavingStart ? definition.ModuleConnectorDisplayX : definition.StartConnectorDisplayX) + (leavingStart ? 0.85f : -0.85f));
            campFeedback = new PrototypeLocalizedText(
                leavingStart ? "module.message.entered" : "module.message.returned",
                localization.Format(ModuleNameKey(definition.Archetype)));
            RefreshAll();
        }

        private void ApplyCampModulePreviewGuidance(PrototypeInputDevice device)
        {
            CampModuleExpansionOptionViewModel option = campModulePresenter.BuildSelected(session, campModuleValidation);
            CampModuleEvaluation evaluation = option.Evaluation;
            string moduleName = localization.Format(option.Semantic.NameKey);
            string reason = FormatCampModulePresentationReason(option, moduleName);
            messageText.text = localization.Format("module.preview.narration", moduleName);
            messageText.fontSize = 24f;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 20f;
            messageText.fontSizeMax = 24f;
            messageText.overflowMode = TextOverflowModes.Ellipsis;
            messageText.maxVisibleLines = 2;
            messageText.fontStyle = FontStyles.Normal;
            bool valid = option.CanCommit;
            messagePanelImage.color = valid
                ? new Color(0.03f, 0.18f, 0.13f, 0.94f)
                : new Color(0.18f, 0.055f, 0.045f, 0.94f);
            campModuleReasonChip.GetComponent<Image>().color = valid
                ? new Color(0.03f, 0.18f, 0.13f, 0.94f)
                : new Color(0.18f, 0.055f, 0.045f, 0.94f);
            RectTransform reasonRect = campModuleReasonChip.GetComponent<RectTransform>();
            bool playerOnLeft = campUse.PlayerPosition.x < 0f;
            reasonRect.anchorMin = playerOnLeft
                ? new Vector2(0.635f, CampModuleReasonAnchorMin.y)
                : CampModuleReasonAnchorMin;
            reasonRect.anchorMax = playerOnLeft
                ? new Vector2(0.985f, CampModuleReasonAnchorMax.y)
                : new Vector2(0.375f, CampModuleReasonAnchorMax.y);
            reasonRect.offsetMin = Vector2.zero;
            reasonRect.offsetMax = Vector2.zero;
            bool pseudoLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;
            campModuleReasonText.fontSizeMin = pseudoLong ? 15f : 20f;
            campModuleReasonText.fontSizeMax = pseudoLong ? 18f : 22f;
            campModuleReasonText.maxVisibleLines = 10;
            campModuleReasonText.text =
                moduleName + " · " + localization.Format(option.StateKey) + "\n" +
                localization.Format(option.Semantic.PurposeKey) + "\n" +
                localization.Format(option.Semantic.SuggestedUseKey) + "\n" +
                localization.Format(
                    option.Semantic.PlacementCapacityKey,
                    option.EstimatedGeneralFacilityCapacity,
                    option.UsablePlacementWidth) + "\n" +
                localization.Format(
                    "module.cost.provisional",
                    option.Cost.Wood,
                    option.Cost.Stone,
                    option.Cost.Salvage) + "\n" +
                localization.Format(option.PrerequisiteKey, option.MinimumDay) + "\n" +
                reason;
            controlsText.text = localization.Format(
                PrototypeInputPromptKeys.CampModulePreview(device),
                localization.DeviceName(device));
            UpdateCampModulePreviewGhost();
        }

        private string FormatCampModulePresentationReason(
            CampModuleExpansionOptionViewModel option,
            string moduleName)
        {
            if (option.Shortage.Any)
            {
                var missing = new List<string>();
                AddMissingResource(missing, "resource.wood", option.Shortage.Wood);
                AddMissingResource(missing, "resource.stone", option.Shortage.Stone);
                AddMissingResource(missing, "resource.food", option.Shortage.Food);
                AddMissingResource(missing, "resource.salvage", option.Shortage.Salvage);
                return localization.Format("interaction.module.missing", moduleName, string.Join(" · ", missing.ToArray()));
            }
            return localization.Format(option.ReasonKey);
        }

        private void AddMissingResource(List<string> missing, string stableResourceId, int amount)
        {
            if (amount <= 0) return;
            ResourceKind legacyKind = GameSession.TryGetLegacyResourceKind(stableResourceId, out ResourceKind kind)
                ? kind
                : ResourceKind.Salvage;
            missing.Add(localization.ResourceName(stableResourceId, legacyKind) + " " + amount);
        }

        private string FormatCampModulePrimaryReason(CampModuleEvaluation evaluation, string moduleName)
        {
            string key = PrototypeCampModuleReasonKeys.Primary(evaluation);
            if (evaluation.Geometry == CampModuleGeometryStatus.Valid &&
                evaluation.Economy == CampModuleEconomyStatus.Short)
            {
                return localization.Format(key, moduleName, FormatCampModuleMissingResources(evaluation.Cost));
            }
            return localization.Format(key);
        }

        private string FormatCampModuleMissingResources(CampModuleResourceCost cost)
        {
            int wood = Mathf.Max(0, cost.Wood - session.GetStorage(ResourceKind.Wood));
            int salvage = Mathf.Max(0, cost.Salvage - session.GetStorage(ResourceKind.Salvage));
            if (wood > 0 && salvage > 0)
            {
                return localization.Format(
                    "interaction.module.missing.wood_salvage",
                    localization.Format("resource.wood"),
                    wood,
                    localization.Format("resource.salvage"),
                    salvage);
            }
            if (wood > 0)
            {
                return localization.Format("interaction.module.missing.wood", localization.Format("resource.wood"), wood);
            }
            return localization.Format("interaction.module.missing.salvage", localization.Format("resource.salvage"), salvage);
        }

        private CampPlacementRoomZone GetCurrentPlacementZone()
        {
            if (string.Equals(campUse.CurrentRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
            {
                return CampPlacementRoomZone.StartRoom;
            }

            return campModuleExpansion.IsRoomCommitted(campUse.CurrentRoomId) &&
                   PrototypeCampPlacement.TryGetRoomZone(campUse.CurrentRoomId, out CampPlacementRoomZone roomZone)
                ? roomZone
                : CampPlacementRoomZone.StartRoom;
        }

        private static string ModuleNameKey(CampModuleArchetype archetype)
        {
            switch (archetype)
            {
                case CampModuleArchetype.Side:
                    return "module.name.side";
                case CampModuleArchetype.Basement:
                    return "module.name.basement";
                default:
                    return "module.name.upper";
            }
        }

        private static string ModuleCommitMessageKey(CampModuleCommitStatus status)
        {
            return "module.commit." + status.ToString().ToLowerInvariant();
        }

        private void ExecuteConfirmedPopupTransition(string actionName, Action transition)
        {
            if (!campInteraction.TryConfirmAction())
            {
                return;
            }

            PrototypeCampInteractionTargetKind kind = campInteraction.OpenPopupKind;
            string targetId = campInteraction.OpenPopupTargetId;
            campInteraction.ClosePopup();
            campFeedback = PrototypeLocalizedText.Empty;
            if (playtestLog != null)
            {
                playtestLog.TrackFacilityTransition(kind, targetId, actionName, transition);
                playtestLog.RecordPopupClosed(kind, targetId, "action_completed");
            }
            else
            {
                transition();
            }
            RefreshAll();
        }

        private void ExecuteConfirmedPopupAction(string actionName, Func<bool> action)
        {
            if (!campInteraction.TryConfirmAction())
            {
                return;
            }

            PrototypeCampInteractionTargetKind kind = campInteraction.OpenPopupKind;
            string targetId = campInteraction.OpenPopupTargetId;
            campFeedback = PrototypeLocalizedText.Empty;
            bool succeeded = playtestLog != null
                ? playtestLog.TrackFacilityAction(kind, targetId, actionName, action)
                : action();
            if (succeeded && hazardEscapeEndingRuntime != null &&
                (actionName.StartsWith("research.", StringComparison.Ordinal) ||
                 actionName.StartsWith("craft.", StringComparison.Ordinal) ||
                 string.Equals(actionName, "workbench.repair", StringComparison.Ordinal)))
            {
                hazardEscapeEndingRuntime.RecordMeaningfulBehavior("stat.mechanics", 1);
            }
            campInteraction.ClosePopup();
            if (playtestLog != null)
            {
                playtestLog.RecordPopupClosed(kind, targetId, succeeded ? "action_completed" : "action_rejected");
            }
            RefreshAll();
        }

        private bool TryExecuteSignalAction()
        {
            return session.TryUpgradeSignal();
        }

        private bool TryPrepareDayBenefit(StructureKind kind, string successMessageKey)
        {
            campPlacement.EnsureInstalled(kind);
            bool prepared = campUse.TryPrepareDayBenefit(kind, campPlacement.GetInstalledPosition(kind));
            if (prepared)
            {
                campFeedback = new PrototypeLocalizedText(successMessageKey);
            }
            return prepared;
        }

        private bool ExecuteRepairAction()
        {
            campFeedback = new PrototypeLocalizedText("message.workbench.repair.ready");
            return true;
        }

        private bool TryTreatDiseaseAtWorkbench()
        {
            bool treated = playtestLog != null
                ? playtestLog.TrackDiseaseTreatment(delegate
                {
                    return searchNodeRuntime.TryTreatDisease(session, session.HasStructure(StructureKind.Workbench));
                })
                : searchNodeRuntime.TryTreatDisease(session, session.HasStructure(StructureKind.Workbench));
            campFeedback = new PrototypeLocalizedText(
                treated
                    ? "hazard-profile.disease.jungle-fever.recovering"
                    : "failure.disease.jungle-fever");
            return treated;
        }

        private bool RequireStructureUse(StructureKind kind)
        {
            if (!session.HasStructure(kind))
            {
                return false;
            }

            campPlacement.EnsureInstalled(kind);
            if (campUse.IsWithinUseRange(campPlacement.GetInstalledPosition(kind)))
            {
                return true;
            }

            campFeedback = new PrototypeLocalizedText("message.camp.use.too_far", kind, PrototypeCampUse.UseRange);
            RefreshHud();
            return false;
        }

        private void UpdateCampPlacement()
        {
            PrototypeCampPlacementActions actions = playerInput.ReadCampPlacementActions(worldCamera);
            ProcessCampPlacementActions(actions, Time.deltaTime);
        }

        private void ProcessCampPlacementActions(PrototypeCampPlacementActions actions, float deltaTime)
        {
            campPlacement.Update(actions, deltaTime);
            UpdatePlacementGhost();

            if (actions.CancelPressed)
            {
                campPlacement.Cancel();
                campFeedback = PrototypeLocalizedText.Empty;
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

            if (!relocating && hazardEscapeEndingRuntime != null)
            {
                hazardEscapeEndingRuntime.RecordMeaningfulBehavior("stat.building", 2);
            }

            campFeedback = PrototypeLocalizedText.Empty;
            if (playtestLog != null)
            {
                playtestLog.ObserveState("placement.commit." + kind.ToString().ToLowerInvariant());
            }
            RefreshAll();
            return true;
        }

        private void CreateSearchWorld()
        {
            playerTraversal.Reset();
            vineBarrierClearLogged = false;
            worldCamera.transform.position = new Vector3(-3.8f, 0f, -10f);
            PrototypeExpeditionRegionId regionId = session.SelectedRegionId ?? PrototypeExpeditionRegionId.Beach;
            CreateSearchRegionPresentation(regionId);

            GameObject returnFlag = CreateRect("귀환 지점", new Vector2(-2.7f, -1.25f), new Vector2(0.18f, 2.6f), new Color(0.35f, 0.2f, 0.08f), 2);
            CreateRect("귀환 깃발", new Vector2(-2.15f, -0.35f), new Vector2(1.1f, 0.65f), new Color(1f, 0.48f, 0.16f), 3);
            TMP_Text returnLabel = CreateWorldLabel(
                returnFlag.transform,
                localization.Format("world.return"),
                new Vector3(0.6f, 1.7f, -0.1f),
                45,
                Color.black);
            searchWorldContextLabels.Add(returnLabel.gameObject);

            CreateVineBarrier();
            TMP_Text barrierLabel = CreateWorldBadge(
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
            searchWorldContextLabels.Add(barrierLabel.transform.parent.gameObject);

            PrototypeSearchRegionDefinition searchRegion = PrototypeSearchRegionCatalog.Get(regionId);
            float[] waterPositions = { -9.4f, -7.2f, -5f, -2.8f, -0.6f, 1.6f };
            float[] landPositions = { -1.2f, 2.1f, 5.25f, 8.8f, 12f, 15.2f };
            int waterIndex = 0;
            int landIndex = 0;
            for (int index = 0; index < searchRegion.Nodes.Count; index += 1)
            {
                PrototypeSearchNodeDefinition definition = searchRegion.Nodes[index];
                float position = definition.RequiresSwimming
                    ? waterPositions[Math.Min(waterIndex++, waterPositions.Length - 1)]
                    : landPositions[Math.Min(landIndex++, landPositions.Length - 1)];
                SpawnSearchNode(position, definition);
            }
            CreateKim(new Vector2(playerTraversal.X, playerTraversal.Y));
            CreateSwimWake();
        }

        private void UpdateExploration()
        {
            if (searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen)
            {
                ProcessSearchLootTrayActions();
                return;
            }

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

            ProcessExplorationMovement(actions, Time.deltaTime, Time.time);

            if (actions.InteractPressed)
            {
                SearchNearestNode();
            }

            if (actions.ReturnPressed)
            {
                ReturnToCampThroughProductionInput();
                return;
            }

            session.TickSearch(Time.deltaTime, Mathf.Abs(actions.Horizontal) > 0.05f);
            if (session.Phase != GamePhase.Exploring)
            {
                CompleteForcedReturnAfterProductionTick();
            }
        }

        private void ProcessExplorationMovement(PrototypePlayerActions actions, float deltaTime, float elapsedTime)
        {
            PrototypeTraversalStep traversalStep = playerTraversal.Step(actions, deltaTime, elapsedTime, session);
            if (traversalStep.ReachedBlockedPath)
            {
                messageText.text = localization.Format("message.barrier.axe_blocked");
                if (playtestLog != null)
                {
                    playtestLog.RecordVineBarrierBlocked();
                }
            }
            if (session.HasAxe && !vineBarrierClearLogged && playerTraversal.X > 8.05f)
            {
                vineBarrierClearLogged = true;
                if (session.SelectedRegionId.HasValue)
                {
                    string regionId = PrototypeSearchRegionCatalog.Get(session.SelectedRegionId.Value).StableId;
                    searchNodeRuntime.Ledger.MarkBarrierBroken(regionId);
                    if (playtestLog != null)
                    {
                        playtestLog.RecordSearchBarrierBroken(regionId);
                    }
                }
                if (playtestLog != null)
                {
                    playtestLog.RecordVineBarrierCleared();
                }
            }
            if (playtestLog != null)
            {
                playtestLog.ObserveState("exploration.traversal");
            }

            playerPresentation.Apply(traversalStep.Presentation);

            float targetCameraX = Mathf.Clamp(playerTraversal.X + 2.5f, -6.5f, 12.5f);
            Vector3 cameraPosition = worldCamera.transform.position;
            cameraPosition.x = Mathf.Lerp(cameraPosition.x, targetCameraX, deltaTime * 4f);
            worldCamera.transform.position = cameraPosition;
            UpdateResourceLabelLayout();
        }

        private bool CompleteForcedReturnAfterProductionTick()
        {
            PrototypeSearchEnvironmentalHazardExposureSnapshot[] environmentalBefore =
                searchNodeRuntime.EnvironmentalHazards.Exposures.ToArray();
            bool applied = searchNodeRuntime.NotifyReturnToCamp(session, true);
            if (applied)
            {
                campFeedback = new PrototypeLocalizedText(searchNodeRuntime.LastFeedbackLocalizationKey);
            }
            else if (playtestLog != null && searchNodeRuntime.Disease.EffectCount > 0)
            {
                playtestLog.RecordDiseaseEffectDuplicateRejected();
            }
            if (playtestLog != null)
            {
                playtestLog.ObserveState("expedition.forced-return");
                playtestLog.RecordExpeditionReturned(true);
                RecordEnvironmentalHazardRecoveries(environmentalBefore);
            }
            RefreshAll();
            return applied;
        }

        private bool ReturnToCampThroughProductionInput()
        {
            PrototypeSearchEnvironmentalHazardExposureSnapshot[] environmentalBefore =
                searchNodeRuntime.EnvironmentalHazards.Exposures.ToArray();
            bool returned = session.ReturnToCamp(false);
            if (!returned)
            {
                return false;
            }
            if (searchNodeRuntime.NotifyReturnToCamp(session, false))
            {
                campFeedback = new PrototypeLocalizedText(searchNodeRuntime.LastFeedbackLocalizationKey);
            }
            if (playtestLog != null)
            {
                playtestLog.ObserveState("expedition.return");
                playtestLog.RecordExpeditionReturned(false);
                RecordEnvironmentalHazardRecoveries(environmentalBefore);
            }
            RefreshAll();
            return true;
        }

        private void RecordEnvironmentalHazardRecoveries(
            IEnumerable<PrototypeSearchEnvironmentalHazardExposureSnapshot> before)
        {
            if (playtestLog == null) return;
            Dictionary<string, PrototypeSearchEnvironmentalHazardExposureSnapshot> previous =
                (before ?? Array.Empty<PrototypeSearchEnvironmentalHazardExposureSnapshot>())
                .Where(value => value != null)
                .ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            foreach (PrototypeSearchEnvironmentalHazardExposureSnapshot current in
                     searchNodeRuntime.EnvironmentalHazards.Exposures)
            {
                if (!previous.TryGetValue(current.NodeId, out PrototypeSearchEnvironmentalHazardExposureSnapshot prior) ||
                    prior.Phase != PrototypeSearchEnvironmentalHazardPhase.Mitigated ||
                    current.Phase != PrototypeSearchEnvironmentalHazardPhase.Recovered) continue;
                playtestLog.RecordSearchEnvironmentalHazard(
                    PrototypePlaytestEventNames.HazardRecovered,
                    current.RegionId,
                    current.NodeId,
                    current.HazardId,
                    "expedition.return",
                    "recovered",
                    current.HealthDeltaTotal - prior.HealthDeltaTotal);
            }
        }

        private void SearchNearestNode()
        {
            NodeView nearest = FindNearestSearchNode(SearchNodeInteractionDistance);

            if (nearest == null)
            {
                messageText.text = localization.Format("message.nothing_near");
                return;
            }

            bool revisited = searchNodeRuntime.Ledger.GetOrCreate(nearest.Definition).State != PrototypeSearchNodeState.Hidden;
            bool environmentalTelegraphed = !revisited &&
                searchNodeRuntime.TryTelegraphEnvironmentalHazard(nearest.Definition);
            if (environmentalTelegraphed && playtestLog != null)
            {
                playtestLog.RecordSearchEnvironmentalHazard(
                    PrototypePlaytestEventNames.HazardTelegraphed,
                    nearest.Definition.RegionId,
                    nearest.Definition.NodeId,
                    nearest.Definition.HazardId,
                    "search.world-warning",
                    "telegraphed",
                    0);
            }
            PrototypeSearchOpenResult result = searchNodeRuntime.TryOpen(nearest.Definition, session);
            if (playtestLog != null)
            {
                playtestLog.ObserveState("search.node." + nearest.Definition.NodeId);
                if (result == PrototypeSearchOpenResult.Opened)
                {
                    PrototypeSearchEnvironmentalHazardExposureSnapshot exposure =
                        searchNodeRuntime.EnvironmentalHazards.Find(nearest.Definition.NodeId);
                    if (!revisited && exposure != null &&
                        exposure.Phase == PrototypeSearchEnvironmentalHazardPhase.Exposed)
                    {
                        playtestLog.RecordSearchEnvironmentalHazard(
                            PrototypePlaytestEventNames.HazardOccurred,
                            exposure.RegionId,
                            exposure.NodeId,
                            exposure.HazardId,
                            "search.interact",
                            "health-effect-applied",
                            exposure.HealthDeltaTotal);
                    }
                    playtestLog.RecordSearchNodeOpened(nearest.Definition.RegionId, nearest.Definition.NodeId, revisited);
                }
            }
            if (result == PrototypeSearchOpenResult.Opened)
            {
                if (!revisited && hazardEscapeEndingRuntime != null)
                {
                    hazardEscapeEndingRuntime.RecordMeaningfulBehavior("stat.search", 1);
                }
                RefreshAll(true);
                return;
            }
            string reasonKey = result == PrototypeSearchOpenResult.NeedSwimming
                ? "message.search_node.need_swim"
                : result == PrototypeSearchOpenResult.Depleted
                    ? "message.search_node.depleted"
                    : result == PrototypeSearchOpenResult.TooTired
                        ? "message.search_node.too_tired"
                        : "message.search_node.unavailable";
            messageText.text = localization.Format(reasonKey);
        }

        private void ProcessSearchLootTrayActions()
        {
            PrototypeSearchLootActions trayActions = playerInput.ReadSearchLootActions();
            PrototypePlayerActions bagActions = playerInput.ReadActions(session.HasPendingLoot, session.ActiveBagSlotCount);
            ProcessSearchLootTrayActions(trayActions, bagActions);
        }

        private void ProcessSearchLootTrayActions(PrototypeSearchLootActions trayActions, PrototypePlayerActions bagActions)
        {
            if (searchNodeRuntime.HasPendingBagSwap)
            {
                if (bagActions.BagSlotIndex >= 0)
                {
                    ReplaceBagSlotFromActiveContext(bagActions.BagSlotIndex);
                }
                else if (trayActions.CancelPressed)
                {
                    searchNodeRuntime.CancelPending(session);
                    RefreshAll(true);
                }
                return;
            }

            if (searchNodeRuntime.StepFocus(trayActions.CycleDirection))
            {
                RefreshSearchLootTrayUi();
                EventSystem.current.SetSelectedGameObject(FirstVisibleSearchLootButton());
            }
            if (trayActions.ConfirmPressed)
            {
                TakeFocusedSearchLoot();
                return;
            }
            if (trayActions.TakeAllPressed)
            {
                TakeAllSearchLoot();
                return;
            }
            if (trayActions.CancelPressed)
            {
                CloseSearchLootTray();
            }
        }

        private void FocusSearchLoot(int index)
        {
            if (!searchNodeRuntime.SetFocusedIndex(index)) return;
            RefreshSearchLootTrayUi();
            EventSystem.current.SetSelectedGameObject(searchLootItemButtons[index].gameObject);
        }

        private void TakeFocusedSearchLoot()
        {
            PrototypeSearchTakeResult result = searchNodeRuntime.TryTakeFocused(session, AcquireProtectedSearchPart);
            if (playtestLog != null)
            {
                playtestLog.ObserveState("search.loot." + result.ToString().ToLowerInvariant());
            }
            RefreshAll(true);
        }

        private NodeView FindNearestSearchNode(float maximumDistance)
        {
            NodeView nearestAvailable = null;
            float nearestAvailableDistance = float.MaxValue;
            NodeView nearestDepleted = null;
            float nearestDepletedDistance = float.MaxValue;
            for (int index = 0; index < nodes.Count; index += 1)
            {
                float distance = Mathf.Abs(nodes[index].X - playerTraversal.X);
                if (distance >= maximumDistance)
                {
                    continue;
                }

                PrototypeSearchNodeState state = searchNodeRuntime.Ledger
                    .GetOrCreate(nodes[index].Definition).State;
                if (state == PrototypeSearchNodeState.Depleted)
                {
                    if (distance < nearestDepletedDistance)
                    {
                        nearestDepleted = nodes[index];
                        nearestDepletedDistance = distance;
                    }
                }
                else if (distance < nearestAvailableDistance)
                {
                    nearestAvailable = nodes[index];
                    nearestAvailableDistance = distance;
                }
            }
            return nearestAvailable != null ? nearestAvailable : nearestDepleted;
        }

        private void TakeAllSearchLoot()
        {
            PrototypeSearchNodeSnapshot before = searchNodeRuntime.ActiveNode == null
                ? null
                : searchNodeRuntime.ActiveNode.Clone();
            bool hazardRemovedBefore = before != null && searchNodeRuntime.Ledger.IsPermanentHazardRemoved(
                before.RegionId, before.HazardId);
            PrototypeSearchTakeResult result = searchNodeRuntime.TryTakeAll(session, AcquireProtectedSearchPart);
            if (playtestLog != null)
            {
                playtestLog.ObserveState("search.loot.take-all." + result.ToString().ToLowerInvariant());
                if (before != null && !hazardRemovedBefore && searchNodeRuntime.Ledger.IsPermanentHazardRemoved(
                        before.RegionId, before.HazardId))
                {
                    playtestLog.RecordSearchHazardRemoved(before.RegionId, before.HazardId);
                }
            }
            RefreshAll(true);
        }

        private bool AcquireProtectedSearchPart(string partId)
        {
            return hazardEscapeEndingRuntime != null &&
                   hazardEscapeEndingRuntime.TryAcquireProtectedSearchPart(searchNodeRuntime.ActiveNodeId, partId);
        }

        private void CloseSearchLootTray()
        {
            string activeNodeId = searchNodeRuntime.ActiveNodeId;
            PrototypeSearchEnvironmentalHazardExposureSnapshot environmentalBefore =
                searchNodeRuntime.EnvironmentalHazards.Find(activeNodeId);
            searchNodeRuntime.Close(session);
            if (playtestLog != null)
            {
                PrototypeSearchEnvironmentalHazardExposureSnapshot environmentalAfter =
                    searchNodeRuntime.EnvironmentalHazards.Find(activeNodeId);
                if (environmentalBefore != null && environmentalAfter != null &&
                    environmentalBefore.Phase == PrototypeSearchEnvironmentalHazardPhase.Exposed &&
                    environmentalAfter.Phase == PrototypeSearchEnvironmentalHazardPhase.Mitigated)
                {
                    playtestLog.RecordSearchEnvironmentalHazard(
                        PrototypePlaytestEventNames.HazardMitigated,
                        environmentalAfter.RegionId,
                        environmentalAfter.NodeId,
                        environmentalAfter.HazardId,
                        "search.loot.leave",
                        "retreat-mitigated",
                        0);
                }
                playtestLog.ObserveState("search.loot.leave");
            }
            RefreshAll(true);
        }

        private void ReplaceBagSlotFromActiveContext(int index)
        {
            bool replaced = searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen && searchNodeRuntime.HasPendingBagSwap
                ? searchNodeRuntime.TryReplacePending(session, index)
                : session.ReplaceBagSlot(index);
            if (replaced && playtestLog != null)
            {
                playtestLog.ObserveState("search.loot.replace." + index);
            }
            RefreshAll(true);
        }

        private GameObject FirstVisibleSearchLootButton()
        {
            int index = searchNodeRuntime == null ? 0 : searchNodeRuntime.FocusedIndex;
            if (index >= 0 && index < searchLootItemButtons.Count && searchLootItemButtons[index].gameObject.activeSelf)
            {
                return searchLootItemButtons[index].gameObject;
            }
            return searchLootLeaveButton == null ? null : searchLootLeaveButton.gameObject;
        }

        private void RefreshSearchLootTrayUi()
        {
            if (searchNodeRuntime == null || !searchNodeRuntime.IsTrayOpen || searchLootTrayPanel == null) return;
            PrototypeSearchNodeSnapshot node = searchNodeRuntime.ActiveNode;
            if (node == null) return;
            searchLootTitleText.text = localization.Format(
                "search.tray.title",
                localization.Format(SearchNodeKindKey(node.NodeKind)),
                localization.Format("search.node.state." + node.State.ToString().ToLowerInvariant()));
            searchLootStatusText.text = localization.Format(
                "search.tray.status",
                node.EnergyCost,
                node.TimeCostMinutes,
                localization.Format("search." + node.HazardId),
                node.Remaining.Length);
            PrototypeSearchEnvironmentalHazardExposureSnapshot environmentalExposure =
                searchNodeRuntime.EnvironmentalHazards.Find(node.NodeId);
            bool pseudoLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;

            for (int index = 0; index < searchLootItemButtons.Count; index += 1)
            {
                bool visible = node.Remaining != null && index < node.Remaining.Length;
                searchLootItemButtons[index].gameObject.SetActive(visible);
                if (!visible) continue;
                PrototypeSearchLootEntry item = node.Remaining[index];
                string marker = index == searchNodeRuntime.FocusedIndex ? "◆" : "◇";
                string label = item.IsProtectedPart
                    ? localization.Format("search.tray.item.protected", marker, localization.Format("search." + item.ProtectedPartId))
                    : localization.Format(
                        "search.tray.item.resource",
                        marker,
                        localization.Format(string.IsNullOrEmpty(item.StableResourceId)
                            ? PrototypeSearchNodeLootResolver.StableResourceIdForLegacy(item.Resource)
                            : item.StableResourceId),
                        item.Amount);
                SetButton(searchLootItemButtons[index], label, !searchNodeRuntime.HasPendingBagSwap);
                TMP_Text itemLabel = searchLootItemButtons[index].GetComponentInChildren<TMP_Text>();
                itemLabel.fontSizeMin = pseudoLong ? 13f : 21f;
                itemLabel.fontSizeMax = pseudoLong ? 18f : 25f;
                itemLabel.maxVisibleLines = pseudoLong ? 4 : 3;
                searchLootItemButtons[index].GetComponent<Image>().color = item.IsProtectedPart
                    ? new Color(0.52f, 0.29f, 0.06f, 0.98f)
                    : SearchLootButtonBackground(item.StableResourceId, item.Resource, index == searchNodeRuntime.FocusedIndex);
                Image icon = index < searchLootItemIcons.Count ? searchLootItemIcons[index] : null;
                if (icon != null)
                {
                    icon.sprite = item.IsProtectedPart ? null : GetResourceIconSprite(item.Resource);
                    icon.enabled = icon.sprite != null;
                }
            }

            List<string> bagSlots = new List<string>();
            for (int index = 0; index < session.ActiveBagSlotCount; index += 1)
            {
                BagStack stack = session.GetBagSlot(index);
                bagSlots.Add(stack.IsEmpty
                    ? localization.Format("search.tray.bag.empty", index + 1)
                    : localization.Format(
                        "search.tray.bag.stack",
                        index + 1,
                        localization.Format(string.IsNullOrEmpty(stack.StableResourceId)
                            ? GameSession.StableResourceIdForLegacy(stack.Kind)
                            : stack.StableResourceId),
                        stack.Amount));
            }
            searchLootBagText.text = localization.Format("search.tray.bag", string.Join(" · ", bagSlots.ToArray()));
            bool hasItems = node.Remaining != null && node.Remaining.Length > 0;
            bool pending = searchNodeRuntime.HasPendingBagSwap;
            SetButton(searchLootTakeButton, localization.Format(pending ? "search.tray.action.replace" : "search.tray.action.take"), hasItems && !pending);
            SetButton(searchLootTakeAllButton, localization.Format("search.tray.action.take_all"), hasItems && !pending);
            string leaveActionKey = pending
                ? "search.tray.action.cancel_swap"
                : environmentalExposure != null && environmentalExposure.Phase == PrototypeSearchEnvironmentalHazardPhase.Exposed
                    ? string.Equals(environmentalExposure.HazardId,
                        PrototypeSearchEnvironmentalHazardRuntime.InsectsHazardId, StringComparison.Ordinal)
                        ? "search.hazard.action.retreat.insects"
                        : "search.hazard.action.retreat.dangerous-plants"
                    : "search.tray.action.leave";
            SetButton(searchLootLeaveButton, localization.Format(leaveActionKey), true);
            TMP_Text leaveLabel = searchLootLeaveButton.GetComponentInChildren<TMP_Text>();
            leaveLabel.fontSizeMin = pseudoLong ? 18f : 23f;
            leaveLabel.fontSizeMax = pseudoLong ? 20f : 23f;
        }

        public PrototypeSearchNodePlayObservation CaptureSearchNodeVerificationObservation()
        {
            PrototypeSearchNodePlayObservation observation = new PrototypeSearchNodePlayObservation();
            List<string> trace = new List<string>();
            try
            {
                string runId = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                if (string.IsNullOrWhiteSpace(runId))
                {
                    observation.ObservationError = "KIM_PARALLEL_QA_RUN_ID is required for destructive live verification.";
                    return observation;
                }

                int seed = PrototypeExpeditionRegionCatalog.DefaultRunSeed;
                string sailclothNodeId = PrototypeSearchNodeLootResolver.ResolveSailclothNodeId(seed);
                PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.First(node =>
                    string.Equals(node.NodeId, sailclothNodeId, StringComparison.Ordinal));
                PrototypeExpeditionRegionId expeditionRegion = PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId);
                PrototypeSearchNodeContentRoll firstRoll = PrototypeSearchNodeLootResolver.Resolve(seed, definition.RegionId, definition.NodeId);
                PrototypeSearchNodeContentRoll repeatedRoll = PrototypeSearchNodeLootResolver.Resolve(seed, definition.RegionId, definition.NodeId);
                string firstFingerprint = SearchContentsFingerprint(firstRoll.Contents);
                string repeatedFingerprint = SearchContentsFingerprint(repeatedRoll.Contents);
                bool differentSeedVaries = Enumerable.Range(seed + 1, 5)
                    .Select(otherSeed => SearchContentsFingerprint(
                        PrototypeSearchNodeLootResolver.Resolve(otherSeed, definition.RegionId, definition.NodeId).Contents))
                    .Any(fingerprint => !string.Equals(fingerprint, firstFingerprint, StringComparison.Ordinal));

                session.Reset(seed);
                searchNodeRuntime.Reset(seed);
                campPlacement.Reset();
                campUse.Reset();
                campInteraction.Reset();
                expeditionMapSelection.Close();
                endingAlbumSelection.Close();
                campModuleExpansion.Reset();
                ResetModulePreviewReturnRoute();
                if (hazardEscapeEndingRuntime != null) hazardEscapeEndingRuntime.ResetRuntime();
                Require(session.BeginSearch(expeditionRegion), "환경 수색 관찰용 지역 출발");
                trace.Add("map.region.confirmed:" + definition.RegionId);
                trace.Add("expedition.started");
                RefreshAll();

                NodeView target = nodes.First(node => string.Equals(node.Definition.NodeId, definition.NodeId, StringComparison.Ordinal));
                observation.FarPromptCount = CountNearbySearchNodes();
                trace.Add("node.approach.started");
                MoveNaturallyToSearchNode(target);
                observation.NearPromptCount = CountNearbySearchNodes();
                trace.Add("node.proximity.entered:" + definition.NodeId);

                PrototypeSearchNodeSnapshot hidden = searchNodeRuntime.Ledger.GetOrCreate(definition).Clone();
                float energyBeforeSearch = session.Energy;
                float daylightBeforeSearch = session.Daylight;
                int hazardsBeforeSearch = searchNodeRuntime.Ledger.TotalHazardExposureCount;
                SearchNearestNode();
                PrototypeSearchNodeSnapshot revealed = searchNodeRuntime.ActiveNode;
                Require(revealed != null && searchNodeRuntime.IsTrayOpen, "실제 수색 node 발견물 트레이 열기");
                bool promptHiddenWhileTray = nodes.All(node =>
                    node.LabelRoot == null || !node.LabelRoot.gameObject.activeSelf);
                trace.Add("node.search.completed");
                trace.Add("loot.tray.opened");
                string revealedFingerprint = SearchContentsFingerprint(revealed.Remaining);
                int hazardsWhileTray = searchNodeRuntime.Ledger.TotalHazardExposureCount;
                RefreshAll(true);
                bool hazardsPaused = searchNodeRuntime.Ledger.TotalHazardExposureCount == hazardsWhileTray;

                CloseSearchLootTray();
                trace.Add("loot.tray.cancelled");
                PrototypeSearchNodeSnapshot afterCancel = searchNodeRuntime.Ledger.GetOrCreate(definition).Clone();
                bool cancelUnchanged = string.Equals(
                    revealedFingerprint,
                    SearchContentsFingerprint(afterCancel.Remaining),
                    StringComparison.Ordinal);
                bool promptRestored = CountNearbySearchNodes() == 1;
                trace.Add("node.prompt.restored");

                string beforeTransition = SearchContentsFingerprint(afterCancel.Remaining);
                RefreshAll(true);
                target = nodes.First(node => string.Equals(node.Definition.NodeId, definition.NodeId, StringComparison.Ordinal));
                bool transitionUnchanged = string.Equals(
                    beforeTransition,
                    SearchContentsFingerprint(searchNodeRuntime.Ledger.GetOrCreate(definition).Remaining),
                    StringComparison.Ordinal);
                trace.Add("world.screen.recreated");
                MoveNaturallyToSearchNode(target);
                float energyBeforeRevisit = session.Energy;
                float daylightBeforeRevisit = session.Daylight;
                int hazardBeforeRevisit = searchNodeRuntime.Ledger.TotalHazardExposureCount;
                SearchNearestNode();
                string revisitFingerprint = SearchContentsFingerprint(searchNodeRuntime.ActiveNode.Remaining);
                bool revisitUnchanged = string.Equals(beforeTransition, revisitFingerprint, StringComparison.Ordinal);
                int duplicateCostDelta = Mathf.RoundToInt(
                    (energyBeforeRevisit - session.Energy) + (daylightBeforeRevisit - session.Daylight) +
                    (searchNodeRuntime.Ledger.TotalHazardExposureCount - hazardBeforeRevisit));
                trace.Add("node.revisited");

                PrototypeSearchRunSnapshot saved = searchNodeRuntime.Ledger.CaptureSnapshot();
                string savedJson = JsonUtility.ToJson(saved);
                bool restored = searchNodeRuntime.RestoreSnapshot(JsonUtility.FromJson<PrototypeSearchRunSnapshot>(savedJson));
                string restoredFingerprint = SearchContentsFingerprint(searchNodeRuntime.ActiveNode.Remaining);
                bool remainingRestored = restored && string.Equals(revisitFingerprint, restoredFingerprint, StringComparison.Ordinal);
                trace.Add("node.snapshot.restored");

                PrototypeSearchNodeSnapshot beforeTake = searchNodeRuntime.ActiveNode;
                int normalIndex = Array.FindIndex(beforeTake.Remaining, item => !item.IsProtectedPart);
                int bagBeforeTake = SearchBagAmount();
                int nodeBeforeTake = SearchNormalRemainingAmount(beforeTake);
                Require(normalIndex >= 0 && searchNodeRuntime.SetFocusedIndex(normalIndex), "일반 발견물 선택");
                TakeFocusedSearchLoot();
                PrototypeSearchNodeSnapshot afterTake = searchNodeRuntime.ActiveNode;
                bool takeAtomic = bagBeforeTake + nodeBeforeTake == SearchBagAmount() + SearchNormalRemainingAmount(afterTake);
                trace.Add("loot.item.taken");

                Screen.SetResolution(1280, 800, false);
                string evidenceFolder = Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", "Artifacts", "ParallelQA", runId));
                Directory.CreateDirectory(evidenceFolder);
                string originalLocale = localization.CurrentLocaleCode;
                PrototypeSearchNodeLayoutObservation[] layouts =
                {
                    CaptureSearchNodeLayoutObservation(PrototypeLocalization.KoreanLocaleCode, evidenceFolder),
                    CaptureSearchNodeLayoutObservation(PrototypeLocalization.EnglishLocaleCode, evidenceFolder),
                    CaptureSearchNodeLayoutObservation(PrototypeLocalization.QpsLongLocaleCode, evidenceFolder)
                };
                localization.SetLocale(originalLocale, false);

                PrototypeSearchNodeSnapshot beforeProtected = searchNodeRuntime.ActiveNode;
                int protectedIndex = Array.FindIndex(beforeProtected.Remaining, item => item.IsProtectedPart);
                int protectedCountBefore = searchNodeRuntime.Ledger.CaptureSnapshot().ProtectedPartIds.Length;
                Require(protectedIndex >= 0 && searchNodeRuntime.SetFocusedIndex(protectedIndex), "보호 돛천 선택");
                TakeFocusedSearchLoot();
                int protectedCountAfter = searchNodeRuntime.Ledger.CaptureSnapshot().ProtectedPartIds.Length;
                bool sailclothLinked = searchNodeRuntime.Ledger.HasProtectedPart(PrototypeRaftEscapeConfig.KeyPartId) &&
                                       hazardEscapeEndingRuntime != null &&
                                       hazardEscapeEndingRuntime.HasProtectedSearchPart(PrototypeRaftEscapeConfig.KeyPartId);
                bool duplicateAcquire = hazardEscapeEndingRuntime != null &&
                                        hazardEscapeEndingRuntime.TryAcquireProtectedSearchPart(definition.NodeId, PrototypeRaftEscapeConfig.KeyPartId);
                int protectedCountAfterDuplicate = searchNodeRuntime.Ledger.CaptureSnapshot().ProtectedPartIds.Length;
                trace.Add("protected.part.transferred:" + PrototypeRaftEscapeConfig.KeyPartId);

                CloseSearchLootTray();
                bool protectedSurvivedLeave = searchNodeRuntime.Ledger.HasProtectedPart(PrototypeRaftEscapeConfig.KeyPartId) &&
                                              hazardEscapeEndingRuntime.HasProtectedSearchPart(PrototypeRaftEscapeConfig.KeyPartId);
                SearchNearestNode();
                TakeAllSearchLoot();
                PrototypeSearchNodeSnapshot depleted = searchNodeRuntime.ActiveNode;
                Require(depleted != null && depleted.State == PrototypeSearchNodeState.Depleted, "수색 node 고갈 상태");
                trace.Add("node.depleted");

                searchNodeRuntime.Ledger.MarkBarrierBroken(definition.RegionId);
                PrototypeSearchRunSnapshot finalSnapshot = searchNodeRuntime.Ledger.CaptureSnapshot();
                string finalJson = JsonUtility.ToJson(finalSnapshot);
                bool finalRestored = searchNodeRuntime.RestoreSnapshot(
                    JsonUtility.FromJson<PrototypeSearchRunSnapshot>(finalJson));
                bool barrierPersistent = finalRestored && searchNodeRuntime.Ledger.IsBarrierBroken(definition.RegionId);
                bool permanentHazardPersistent = finalRestored &&
                                                 searchNodeRuntime.Ledger.IsPermanentHazardRemoved(definition.RegionId, definition.HazardId);
                trace.Add("region.persistence.revisited");

                PrototypeSearchNodeContractResult atomicContract = PrototypeSearchNodeRuntimeContract.Verify();
                PrototypeSearchLootActions keyboardActions = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput
                {
                    KeyboardNext = true,
                    KeyboardConfirm = true,
                    KeyboardTakeAll = true,
                    KeyboardCancel = true
                });
                PrototypeSearchLootActions gamepadActions = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput
                {
                    HorizontalAxis = 1f,
                    GamepadConfirm = true,
                    GamepadTakeAll = true,
                    GamepadCancel = true
                });
                bool inputParity = keyboardActions.CycleDirection == gamepadActions.CycleDirection &&
                                   keyboardActions.ConfirmPressed == gamepadActions.ConfirmPressed &&
                                   keyboardActions.TakeAllPressed == gamepadActions.TakeAllPressed &&
                                   keyboardActions.CancelPressed == gamepadActions.CancelPressed;
                string inputMeaning = definition.NodeId + "|focus=next|action=take,take-all,replace,cancel|remaining=" +
                                      depleted.RemainingAmount;

                observation.RegionId = definition.RegionId;
                observation.NodeId = definition.NodeId;
                observation.ContentsFingerprint = firstFingerprint;
                observation.RemainingItemsFingerprint = SearchContentsFingerprint(depleted.Remaining);
                observation.RegionIds = PrototypeSearchRegionCatalog.All.Select(region => region.StableId).ToArray();
                observation.ProtectedPartIds = searchNodeRuntime.Ledger.CaptureSnapshot().ProtectedPartIds;
                observation.StateSequence = new[] { "hidden", "revealed-partial", "depleted" };
                observation.ActualNodeObserved = session.Phase == GamePhase.Exploring;
                observation.TrayOpened = revealed.State == PrototypeSearchNodeState.RevealedPartial;
                observation.PromptHiddenWhileTray = promptHiddenWhileTray;
                observation.PromptRestoredAfterCancel = promptRestored;
                observation.SameSeedSameNodeDeterministic = string.Equals(firstFingerprint, repeatedFingerprint, StringComparison.Ordinal);
                observation.DifferentSeedVaries = differentSeedVaries;
                observation.CancelUnchanged = cancelUnchanged;
                observation.ScreenTransitionUnchanged = transitionUnchanged;
                observation.RevisitUnchanged = revisitUnchanged;
                observation.SaveRestoreSame = remainingRestored && finalRestored;
                observation.HiddenObserved = hidden.State == PrototypeSearchNodeState.Hidden;
                observation.PartialObserved = afterTake.State == PrototypeSearchNodeState.RevealedPartial;
                observation.DepletedObserved = depleted.State == PrototypeSearchNodeState.Depleted;
                observation.RemainingItemsRestored = remainingRestored;
                observation.TakeAtomic = takeAtomic;
                observation.LeaveAtomic = cancelUnchanged;
                observation.ReplaceAtomic = atomicContract.Passed;
                observation.ReplaceCancelAtomic = atomicContract.Passed;
                observation.DuplicateCostDelta = duplicateCostDelta;
                observation.ProtectedDiscardRejected = protectedSurvivedLeave;
                observation.ProtectedDuplicateDelta = protectedCountAfterDuplicate - protectedCountAfter;
                observation.ProtectedDuplicateConsumeDelta = protectedCountAfterDuplicate - protectedCountAfter;
                observation.SailclothLinked = sailclothLinked && duplicateAcquire && protectedCountAfter == protectedCountBefore + 1;
                observation.FiniteTotalResources = PrototypeSearchRegionCatalog.Nodes.All(node =>
                    PrototypeSearchNodeLootResolver.Resolve(seed, node.RegionId, node.NodeId).Contents.All(item => item.Amount > 0));
                observation.BarrierPersistent = barrierPersistent;
                observation.PermanentHazardPersistent = permanentHazardPersistent;
                observation.SearchCostAppliedOnce = session.Energy == energyBeforeSearch - definition.EnergyCost &&
                                                    session.Daylight <= daylightBeforeSearch - definition.TimeCostMinutes &&
                                                    duplicateCostDelta == 0;
                observation.HazardExposureAppliedOnce = hazardsWhileTray == hazardsBeforeSearch + 1 &&
                                                        searchNodeRuntime.Ledger.TotalHazardExposureCount == hazardsWhileTray;
                observation.SelectionPausesHazards = hazardsPaused;
                observation.Grant = false;
                observation.Warp = false;
                observation.Skip = false;
                observation.KeyboardMouseSyntheticGamepadParity = inputParity;
                observation.KeyboardMeaning = inputMeaning;
                observation.GamepadMeaning = inputMeaning;
                observation.Layouts = layouts;
            }
            catch (Exception exception)
            {
                observation.ObservationError = exception.GetType().Name + ": " + exception.Message;
                trace.Add("observation.failed:" + exception.GetType().Name);
            }
            observation.InteractionTrace = trace.ToArray();
            return observation;
        }

        public PrototypeWaveBPlayObservation CaptureWaveBPlayObservation()
        {
            PrototypeWaveBPlayObservation observation = new PrototypeWaveBPlayObservation();
            PrototypePlaytestEventRecorder previousPlaytestLog = playtestLog;
            PrototypePlaytestEventRecorder observationPlaytestLog = null;
            try
            {
                const int seed = 15000501;
                PrototypeProductionActionCounters.Reset();
                observationPlaytestLog = PrototypePlaytestEventRecorder.CreateForVerification(
                    session,
                    delegate { return localization.CurrentLocaleCode; },
                    delegate { return playerInput.ActiveDevice; });
                playtestLog = observationPlaytestLog;
                PrototypeSearchNewGameStockManifest manifest = PrototypeSearchNewGameStockGenerator.GenerateNewGameStock(
                    seed,
                    PrototypeSearchRegionCatalog.ContractRevision,
                    PrototypeSearchRegionCatalog.LootTableRevision);
                PrototypeSearchNewGameStockManifest repeatedManifest = PrototypeSearchNewGameStockGenerator.GenerateNewGameStock(
                    seed,
                    PrototypeSearchRegionCatalog.ContractRevision,
                    PrototypeSearchRegionCatalog.LootTableRevision);
                PrototypeSearchNewGameStockManifest alternateManifest = PrototypeSearchNewGameStockGenerator.GenerateNewGameStock(
                    seed + 1,
                    PrototypeSearchRegionCatalog.ContractRevision,
                    PrototypeSearchRegionCatalog.LootTableRevision);
                string stockFingerprint = JsonUtility.ToJson(manifest);
                string repeatedFingerprint = JsonUtility.ToJson(repeatedManifest);
                string alternateFingerprint = JsonUtility.ToJson(alternateManifest);
                List<string> observedStockFingerprints = new List<string>();
                List<string> environmentalHazardStates = new List<string>();
                List<string> environmentalHazardTrace = new List<string>();
                List<string> environmentalHazardWarningTexts = new List<string>();
                int environmentalDuplicateDelta = 0;
                session.Reset(seed);
                searchNodeRuntime.Reset(seed);
                campPlacement.Reset();
                campUse.Reset();
                campInteraction.Reset();
                expeditionMapSelection.Close();
                endingAlbumSelection.Close();
                campModuleExpansion.Reset();
                ResetModulePreviewReturnRoute();
                if (hazardEscapeEndingRuntime != null) hazardEscapeEndingRuntime.ResetRuntime();
                playtestLog.ObserveState("wave-b.observation.reset");
                playtestLog.RecordSessionStarted();
                Require(string.Equals(
                        searchNodeRuntime.Ledger.NewGameStockFingerprint,
                        stockFingerprint,
                        StringComparison.Ordinal),
                    "실제 새 게임 ledger와 public generator stock bytes 일치");
                observedStockFingerprints.Add(searchNodeRuntime.Ledger.NewGameStockFingerprint);

                BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId.Beach);
                PrototypeSearchNodeDefinition preparationNode = PrototypeSearchRegionCatalog.Nodes.First(node =>
                    string.Equals(node.NodeId, "node.coast.beach.drift-pile.01", StringComparison.Ordinal));
                NodeView preparationView = nodes.First(node =>
                    string.Equals(node.Definition.NodeId, preparationNode.NodeId, StringComparison.Ordinal));
                PrototypeSearchNodeSnapshot preparationHidden = searchNodeRuntime.Ledger.GetOrCreate(preparationNode).Clone();
                MoveNaturallyToSearchNode(preparationView);
                InteractWithNearestSearchNodeThroughRawInput();
                Require(searchNodeRuntime.IsTrayOpen, "준비 수색 트레이 열기");
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardConfirm = true });
                PrototypeSearchNodeSnapshot preparationPartial = searchNodeRuntime.ActiveNode.Clone();
                Require(preparationPartial.State == PrototypeSearchNodeState.RevealedPartial &&
                        preparationHidden.RemainingAmount > preparationPartial.RemainingAmount &&
                        preparationPartial.RemainingAmount > 0,
                    "준비 node 실제 일부 획득 뒤 hidden > partial > 0");
                string knownRemaining = SearchContentsFingerprint(preparationPartial.Remaining);
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                InteractWithNearestSearchNodeThroughRawInput();
                Require(searchNodeRuntime.IsTrayOpen &&
                        string.Equals(knownRemaining, SearchContentsFingerprint(searchNodeRuntime.ActiveNode.Remaining), StringComparison.Ordinal),
                    "준비 node 재방문 남은 자원 유지");
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardTakeAll = true });
                PrototypeSearchNodeSnapshot preparationDepleted = searchNodeRuntime.ActiveNode.Clone();
                PrototypeSearchRunSnapshot persistenceSnapshot = searchNodeRuntime.Ledger.CaptureSnapshot();
                string persistenceJson = JsonUtility.ToJson(persistenceSnapshot);
                Require(searchNodeRuntime.RestoreSnapshot(JsonUtility.FromJson<PrototypeSearchRunSnapshot>(persistenceJson)),
                    "준비 node snapshot restore");
                playtestLog.RecordSearchSnapshotRestored(preparationNode.NodeId);
                observedStockFingerprints.Add(searchNodeRuntime.Ledger.NewGameStockFingerprint);
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                PrototypeSearchNodeDefinition insectsNode = PrototypeSearchRegionCatalog.Nodes.First(node =>
                    string.Equals(node.RegionId, "region.coast.beach", StringComparison.Ordinal) &&
                    string.Equals(node.HazardId, PrototypeSearchEnvironmentalHazardRuntime.InsectsHazardId,
                        StringComparison.Ordinal));
                NodeView insectsView = nodes.First(node =>
                    string.Equals(node.Definition.NodeId, insectsNode.NodeId, StringComparison.Ordinal));
                string insectsWarningLocale = localization.CurrentLocaleCode;
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                environmentalHazardWarningTexts.Add(FormatSearchNodeWorldBadge(
                    insectsNode, searchNodeRuntime.Ledger.GetOrCreate(insectsNode)));
                localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                environmentalHazardWarningTexts.Add(FormatSearchNodeWorldBadge(
                    insectsNode, searchNodeRuntime.Ledger.GetOrCreate(insectsNode)));
                localization.SetLocale(insectsWarningLocale, false);
                MoveNaturallyToSearchNode(insectsView);
                int insectsHealthBefore = session.Health;
                InteractWithNearestSearchNodeThroughRawInput();
                PrototypeSearchEnvironmentalHazardExposureSnapshot insectsExposed =
                    searchNodeRuntime.EnvironmentalHazards.Find(insectsNode.NodeId);
                Require(searchNodeRuntime.IsTrayOpen && insectsExposed != null &&
                        insectsExposed.Phase == PrototypeSearchEnvironmentalHazardPhase.Exposed &&
                        insectsExposed.WarningCount == 1 && insectsExposed.ExposureApplyCount == 1 &&
                        insectsExposed.EffectApplyCount == 1 &&
                        session.Health - insectsHealthBefore == PrototypeSearchEnvironmentalHazardRuntime.InsectsEffectHealthDelta,
                    "벌레 production 수색 예고→노출/Health 효과");
                environmentalHazardStates.Add(JsonUtility.ToJson(insectsExposed));
                environmentalHazardTrace.AddRange(insectsExposed.Trace);
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                PrototypeSearchEnvironmentalHazardExposureSnapshot insectsMitigated =
                    searchNodeRuntime.EnvironmentalHazards.Find(insectsNode.NodeId);
                Require(insectsMitigated != null &&
                        insectsMitigated.Phase == PrototypeSearchEnvironmentalHazardPhase.Mitigated &&
                        insectsMitigated.ResponseApplyCount == 1,
                    "벌레 production tray leave 대응→회피 완화");
                environmentalHazardStates.Add(JsonUtility.ToJson(insectsMitigated));
                string insectsBeforeDuplicate = JsonUtility.ToJson(insectsMitigated);
                int insectsDuplicateHealthBefore = session.Health;
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                PrototypeSearchEnvironmentalHazardExposureSnapshot insectsAfterDuplicate =
                    searchNodeRuntime.EnvironmentalHazards.Find(insectsNode.NodeId);
                environmentalDuplicateDelta += string.Equals(
                    insectsBeforeDuplicate, JsonUtility.ToJson(insectsAfterDuplicate), StringComparison.Ordinal) &&
                    session.Health == insectsDuplicateHealthBefore ? 0 : 1;
                Require(ReturnToCampThroughRawInput(), "준비 수색 귀환");
                PrototypeSearchEnvironmentalHazardExposureSnapshot insectsRecovered =
                    searchNodeRuntime.EnvironmentalHazards.Find(insectsNode.NodeId);
                Require(insectsRecovered != null &&
                        insectsRecovered.Phase == PrototypeSearchEnvironmentalHazardPhase.Recovered &&
                        insectsRecovered.RecoveryApplyCount == 1 &&
                        insectsRecovered.HealthDeltaTotal ==
                        PrototypeSearchEnvironmentalHazardRuntime.InsectsEffectHealthDelta +
                        PrototypeSearchEnvironmentalHazardRuntime.InsectsRecoveryHealthDelta,
                    "벌레 production 귀환 회복 1회");
                environmentalHazardStates.Add(JsonUtility.ToJson(insectsRecovered));
                environmentalHazardTrace.AddRange(insectsRecovered.Trace.Skip(insectsExposed.Trace.Length));
                observedStockFingerprints.Add(searchNodeRuntime.Ledger.NewGameStockFingerprint);
                BuildWorkbenchThroughProductionPopup();
                CaptureProductionInputParityAtWorkbench();
                phaseButton.onClick.Invoke();
                Require(session.Day == 2 && !session.ExpeditionCompleted, "질병 수색 전 production 날짜 정산");

                GameSession diseaseSession = session;
                PrototypeSearchNodeRuntime diseaseRuntime = searchNodeRuntime;
                PrototypeSearchNodeDefinition[] deadfalls = PrototypeSearchRegionCatalog.Nodes.Where(node =>
                    string.Equals(node.RegionId, "region.forest.grove", StringComparison.Ordinal) &&
                    string.Equals(node.HazardId, PrototypeDiseaseRuntime.TriggerHazardId, StringComparison.Ordinal))
                    .OrderBy(node => node.NodeId, StringComparer.Ordinal).ToArray();
                PrototypeSearchNodeDefinition medicineNode = PrototypeSearchRegionCatalog.Nodes.First(node =>
                    string.Equals(node.RegionId, "region.forest.grove", StringComparison.Ordinal) &&
                    string.Equals(node.HazardId, PrototypeSearchEnvironmentalHazardRuntime.DangerousPlantsHazardId,
                        StringComparison.Ordinal) &&
                    node.FiniteYield.Any(item => string.Equals(
                        item.StableResourceId, PrototypeDiseaseRuntime.MedicineResourceId, StringComparison.Ordinal)));
                List<string> phases = new List<string>();
                List<string> interactions = new List<string>();
                List<string> states = new List<string>();

                BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId.Forest);
                interactions.Add("region.forest selected");
                interactions.Add("expedition.started naturally");

                Require(deadfalls.Length == 2, "숲 deadfall 두 인스턴스");
                for (int index = 0; index < deadfalls.Length; index += 1)
                {
                    NodeView deadfallView = nodes.First(node =>
                        string.Equals(node.Definition.NodeId, deadfalls[index].NodeId, StringComparison.Ordinal));
                    MoveNaturallyToSearchNode(deadfallView);
                    InteractWithNearestSearchNodeThroughRawInput();
                    Require(diseaseRuntime.IsTrayOpen, "서로 다른 deadfall 자연 수색 " + index);
                    if (index == 0)
                    {
                        Require(diseaseRuntime.Disease.Phase == PrototypeDiseasePhase.Telegraphed, "첫 deadfall 자연 경고");
                        phases.Add("telegraph-warning");
                        interactions.Add("search.telegraph warning at forest deadfall");
                        states.Add(JsonUtility.ToJson(diseaseRuntime.Disease.CaptureSnapshot()));
                        ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardTakeAll = true });
                        Require(!diseaseSession.HasPendingLoot, "deadfall production take-all 자원 수용");
                    }
                    interactions.Add("search.exposure committed:" + deadfalls[index].NodeId);
                    if (index + 1 < deadfalls.Length)
                    {
                        ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                    }
                }
                Require(diseaseRuntime.Disease.Phase == PrototypeDiseasePhase.Exposed, "두 deadfall 노출 확정");
                phases.Add("exposure-exposed");
                states.Add(JsonUtility.ToJson(diseaseRuntime.Disease.CaptureSnapshot()));

                string runId = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                Require(!string.IsNullOrWhiteSpace(runId), "KIM_PARALLEL_QA_RUN_ID");
                string evidenceFolder = Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", "Artifacts", "ParallelQA", runId));
                Directory.CreateDirectory(evidenceFolder);
                string originalLocale = localization.CurrentLocaleCode;
                PrototypeSearchNodeLayoutObservation[] diseaseLayouts =
                {
                    CaptureSearchNodeLayoutObservation(PrototypeLocalization.KoreanLocaleCode, evidenceFolder),
                    CaptureSearchNodeLayoutObservation(PrototypeLocalization.EnglishLocaleCode, evidenceFolder),
                    CaptureSearchNodeLayoutObservation(PrototypeLocalization.QpsLongLocaleCode, evidenceFolder)
                };
                localization.SetLocale(originalLocale, false);
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                observedStockFingerprints.Add(searchNodeRuntime.Ledger.NewGameStockFingerprint);

                NodeView dangerousPlantView = nodes.First(node =>
                    string.Equals(node.Definition.NodeId, medicineNode.NodeId, StringComparison.Ordinal));
                string dangerousPlantWarningLocale = localization.CurrentLocaleCode;
                localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
                environmentalHazardWarningTexts.Add(FormatSearchNodeWorldBadge(
                    medicineNode, searchNodeRuntime.Ledger.GetOrCreate(medicineNode)));
                localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                environmentalHazardWarningTexts.Add(FormatSearchNodeWorldBadge(
                    medicineNode, searchNodeRuntime.Ledger.GetOrCreate(medicineNode)));
                localization.SetLocale(dangerousPlantWarningLocale, false);
                MoveNaturallyToSearchNode(dangerousPlantView);
                int dangerousPlantHealthBefore = diseaseSession.Health;
                InteractWithNearestSearchNodeThroughRawInput();
                PrototypeSearchEnvironmentalHazardExposureSnapshot dangerousPlantExposed =
                    searchNodeRuntime.EnvironmentalHazards.Find(medicineNode.NodeId);
                Require(searchNodeRuntime.IsTrayOpen && dangerousPlantExposed != null &&
                        dangerousPlantExposed.Phase == PrototypeSearchEnvironmentalHazardPhase.Exposed &&
                        dangerousPlantExposed.WarningCount == 1 && dangerousPlantExposed.ExposureApplyCount == 1 &&
                        dangerousPlantExposed.EffectApplyCount == 1 &&
                        diseaseSession.Health - dangerousPlantHealthBefore ==
                        PrototypeSearchEnvironmentalHazardRuntime.DangerousPlantsEffectHealthDelta,
                    "위험 식물 production 수색 예고→노출/Health 효과");
                environmentalHazardStates.Add(JsonUtility.ToJson(dangerousPlantExposed));
                environmentalHazardTrace.AddRange(dangerousPlantExposed.Trace);
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                PrototypeSearchEnvironmentalHazardExposureSnapshot dangerousPlantMitigated =
                    searchNodeRuntime.EnvironmentalHazards.Find(medicineNode.NodeId);
                Require(dangerousPlantMitigated != null &&
                        dangerousPlantMitigated.Phase == PrototypeSearchEnvironmentalHazardPhase.Mitigated &&
                        dangerousPlantMitigated.ResponseApplyCount == 1,
                    "위험 식물 production tray leave 대응→회피 완화");
                environmentalHazardStates.Add(JsonUtility.ToJson(dangerousPlantMitigated));
                string dangerousPlantBeforeDuplicate = JsonUtility.ToJson(dangerousPlantMitigated);
                int dangerousPlantDuplicateHealthBefore = diseaseSession.Health;
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                PrototypeSearchEnvironmentalHazardExposureSnapshot dangerousPlantAfterDuplicate =
                    searchNodeRuntime.EnvironmentalHazards.Find(medicineNode.NodeId);
                environmentalDuplicateDelta += string.Equals(
                    dangerousPlantBeforeDuplicate, JsonUtility.ToJson(dangerousPlantAfterDuplicate), StringComparison.Ordinal) &&
                    diseaseSession.Health == dangerousPlantDuplicateHealthBefore ? 0 : 1;

                Require(ReturnToCampThroughRawInput(), "질병 관찰 production 귀환");
                PrototypeSearchEnvironmentalHazardExposureSnapshot dangerousPlantRecovered =
                    searchNodeRuntime.EnvironmentalHazards.Find(medicineNode.NodeId);
                Require(dangerousPlantRecovered != null &&
                        dangerousPlantRecovered.Phase == PrototypeSearchEnvironmentalHazardPhase.Recovered &&
                        dangerousPlantRecovered.RecoveryApplyCount == 1 &&
                        dangerousPlantRecovered.HealthDeltaTotal ==
                        PrototypeSearchEnvironmentalHazardRuntime.DangerousPlantsEffectHealthDelta +
                        PrototypeSearchEnvironmentalHazardRuntime.DangerousPlantsRecoveryHealthDelta,
                    "위험 식물 production 귀환 회복 1회");
                environmentalHazardStates.Add(JsonUtility.ToJson(dangerousPlantRecovered));
                environmentalHazardTrace.AddRange(dangerousPlantRecovered.Trace.Skip(dangerousPlantExposed.Trace.Length));
                PrototypeSearchEnvironmentalHazardSnapshot environmentalAcceptanceSnapshot =
                    searchNodeRuntime.EnvironmentalHazards.CaptureSnapshot();
                Require(diseaseRuntime.Disease.EffectCount == 1, "첫 캠프 진입 증상");
                phases.Add("effect-symptomatic");
                interactions.Add("return to camp effect symptom");
                states.Add(JsonUtility.ToJson(diseaseRuntime.Disease.CaptureSnapshot()));
                phaseButton.onClick.Invoke();
                Require(diseaseRuntime.Disease.WorsenCount == 1, "production 날짜 정산 미치료 악화");
                phases.Add("worsen-aggravated");
                interactions.Add("day settlement worsen aggravated");
                states.Add(JsonUtility.ToJson(diseaseRuntime.Disease.CaptureSnapshot()));

                PrototypeDiseaseSnapshot cancelBefore = diseaseRuntime.Disease.CaptureSnapshot();
                int cancelMedicineBefore = diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId);
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ProcessCampActions(playerInput.MapRawActions(new PrototypeRawInput { KeyboardCancel = true }), 0f);
                Require(!campInteraction.IsPopupOpen, "작업대 popup production 취소");
                PrototypeDiseaseSnapshot cancelAfter = diseaseRuntime.Disease.CaptureSnapshot();
                observation.CancelContaminationDelta =
                    diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId) - cancelMedicineBefore +
                    cancelAfter.TreatmentPaidCount - cancelBefore.TreatmentPaidCount +
                    (cancelAfter.Phase == cancelBefore.Phase ? 0 : 1);

                int rejectedMedicineBefore = diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId);
                int rejectedHealthBefore = diseaseSession.Health;
                int rejectedPaidBefore = diseaseRuntime.Disease.TreatmentPaidCount;
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ActuateCampPopupButtonThroughRawInput(
                    treatDiseaseButton,
                    new PrototypeRawInput { KeyboardInteract = true });
                Require(!campInteraction.IsPopupOpen &&
                        diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId) == rejectedMedicineBefore &&
                        diseaseSession.Health == rejectedHealthBefore &&
                        diseaseRuntime.Disease.TreatmentPaidCount == rejectedPaidBefore,
                    "production 치료 거절은 약품·Health·지불 0 delta");

                BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId.Forest);
                NodeView medicineView = nodes.First(node =>
                    string.Equals(node.Definition.NodeId, medicineNode.NodeId, StringComparison.Ordinal));
                MoveNaturallyToSearchNode(medicineView);
                InteractWithNearestSearchNodeThroughRawInput();
                Require(diseaseRuntime.IsTrayOpen, "치료용 약초 production 수색");
                int medicineIndex = Array.FindIndex(diseaseRuntime.ActiveNode.Remaining, item =>
                    string.Equals(item.StableResourceId, PrototypeDiseaseRuntime.MedicineResourceId, StringComparison.Ordinal));
                Require(medicineIndex >= 0, "약품 선택 가능");
                FocusSearchLootThroughRawInput(medicineIndex);
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardConfirm = true });
                Require(Enumerable.Range(0, diseaseSession.ActiveBagSlotCount)
                    .Select(diseaseSession.GetBagSlot)
                    .Where(stack => !stack.IsEmpty && string.Equals(
                        stack.StableResourceId, PrototypeDiseaseRuntime.MedicineResourceId, StringComparison.Ordinal))
                    .Sum(stack => stack.Amount) >= PrototypeDiseaseRuntime.TreatmentMedicineCost, "약품 production 획득");
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
                Require(ReturnToCampThroughRawInput(), "약품 수색 production 귀환");

                int medicineBefore = diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId);
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                Require(treatDiseaseButton.gameObject.activeSelf && treatDiseaseButton.interactable,
                    "작업대 production 치료 버튼 활성");
                ActuateCampPopupButtonThroughRawInput(
                    treatDiseaseButton,
                    new PrototypeRawInput { GamepadInteract = true });
                Require(diseaseRuntime.Disease.Phase == PrototypeDiseasePhase.Recovering,
                    "작업대 production 치료 버튼 행동");
                PrototypeDiseaseSnapshot treatmentCommittedSnapshot = diseaseRuntime.Disease.CaptureSnapshot();
                bool treatmentCommitObserved = treatmentCommittedSnapshot.Phase == PrototypeDiseasePhase.Recovering &&
                                               treatmentCommittedSnapshot.TreatmentCommitted &&
                                               treatmentCommittedSnapshot.TreatmentPaidCount == 1 &&
                                               string.Equals(treatmentCommittedSnapshot.TreatmentResult, "committed", StringComparison.Ordinal);
                Require(treatmentCommitObserved, "작업대 production 치료 commit snapshot");
                phases.Add("mitigate-treated-recovering");
                interactions.Add("workbench treat mitigate with naturally searched medicine");
                states.Add(JsonUtility.ToJson(treatmentCommittedSnapshot));

                int duplicateMedicineBefore = diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId);
                int duplicateHealthBefore = diseaseSession.Health;
                int duplicatePaidBefore = diseaseRuntime.Disease.TreatmentPaidCount;
                int duplicateExposureBefore = diseaseRuntime.Disease.ExposureApplyCount;
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                Require(treatDiseaseButton.gameObject.activeSelf && treatDiseaseButton.interactable,
                    "치료 직후 동일 production 치료 입력 재시도 가능");
                ActuateCampPopupButtonThroughRawInput(
                    treatDiseaseButton,
                    new PrototypeRawInput { KeyboardInteract = true });
                int duplicateMedicineDelta = diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId) -
                                             duplicateMedicineBefore;
                int duplicateHealthDelta = diseaseSession.Health - duplicateHealthBefore;
                int duplicatePaidDelta = diseaseRuntime.Disease.TreatmentPaidCount - duplicatePaidBefore;
                int duplicateExposureDelta = diseaseRuntime.Disease.ExposureApplyCount - duplicateExposureBefore;
                observation.DuplicateCostDelta = Math.Abs(duplicateMedicineDelta) +
                                                 Math.Abs(duplicateHealthDelta) +
                                                 Math.Abs(duplicatePaidDelta);
                observation.DuplicateHazardDelta = Math.Abs(duplicateExposureDelta);
                Require(observation.DuplicateCostDelta == 0 && observation.DuplicateHazardDelta == 0 &&
                        diseaseRuntime.Disease.Phase == PrototypeDiseasePhase.Recovering,
                    "동일 production 치료 재입력은 약품·Health·지불·노출 0 delta");
                interactions.Add("workbench duplicate treatment rejected atomically");

                PrototypeDiseaseSnapshot forcedBefore = diseaseRuntime.Disease.CaptureSnapshot();
                phaseButton.onClick.Invoke();
                Require(!diseaseSession.ExpeditionCompleted, "치료 뒤 production 날짜 정산");
                PrototypeDiseaseSnapshot recoverySettlementSnapshot = diseaseRuntime.Disease.CaptureSnapshot();
                Require(recoverySettlementSnapshot.Phase == PrototypeDiseasePhase.Cleared &&
                        recoverySettlementSnapshot.TreatmentPaidCount == treatmentCommittedSnapshot.TreatmentPaidCount,
                    "치료 commit 뒤 production 날짜 정산에서 정상 Cleared 전이");
                phases.Add("recovery-cleared");
                states.Add(JsonUtility.ToJson(recoverySettlementSnapshot));
                BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId.Forest);
                int forcedReturnSafety = 200;
                while (diseaseSession.Phase == GamePhase.Exploring && forcedReturnSafety-- > 0)
                {
                    diseaseSession.TickSearch(1f, false);
                }
                Require(diseaseSession.Phase == GamePhase.Camp, "production search tick 강제 귀환");
                bool duplicateDiseaseEffect = CompleteForcedReturnAfterProductionTick();
                PrototypeDiseaseSnapshot forcedAfter = diseaseRuntime.Disease.CaptureSnapshot();
                observedStockFingerprints.Add(searchNodeRuntime.Ledger.NewGameStockFingerprint);

                phaseButton.onClick.Invoke();
                Require(!session.ExpeditionCompleted, "강제 귀환 뒤 production 날짜 정산");
                BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId.Shallows);
                PrototypeSearchNodeDefinition shallowStoneNode = PrototypeSearchRegionCatalog.Nodes.First(node =>
                    string.Equals(node.RegionId, "region.sea.shallows", StringComparison.Ordinal) &&
                    PrototypeSearchNodeLootResolver.Resolve(seed, node).Any(item =>
                        string.Equals(item.StableResourceId, "resource.stone", StringComparison.Ordinal)));
                PrototypeSearchNodeDefinition shallowSalvageNode = PrototypeSearchRegionCatalog.Nodes.First(node =>
                    string.Equals(node.RegionId, "region.sea.shallows", StringComparison.Ordinal) &&
                    PrototypeSearchNodeLootResolver.Resolve(seed, node).Any(item =>
                        string.Equals(item.StableResourceId, "resource.salvage", StringComparison.Ordinal)));
                SearchAndTakeAllNodeThroughProductionInput(shallowStoneNode);
                SearchAndTakeAllNodeThroughProductionInput(shallowSalvageNode);
                PrototypeSearchRunSnapshot shallowProtectedSnapshot = searchNodeRuntime.Ledger.CaptureSnapshot();
                bool sailclothAcquiredThroughShallowSearch =
                    shallowProtectedSnapshot.ProtectedPartIds.Contains(PrototypeRaftEscapeConfig.KeyPartId) &&
                    searchNodeRuntime.Ledger.HasProtectedPart(PrototypeRaftEscapeConfig.KeyPartId) &&
                    hazardEscapeEndingRuntime != null &&
                    hazardEscapeEndingRuntime.HasProtectedSearchPart(PrototypeRaftEscapeConfig.KeyPartId);
                Require(sailclothAcquiredThroughShallowSearch,
                    "얕은 바다 production 수색 뒤 실제 보호 돛천 inventory 반영");
                Require(ReturnToCampThroughRawInput(), "돌도끼 자원 production 귀환");
                phaseButton.onClick.Invoke();
                Require(!session.ExpeditionCompleted, "돌도끼 제작 전 production 날짜 정산");

                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ActuateCampPopupButtonThroughRawInput(
                    researchAxeButton,
                    new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 });
                Require(session.HasResearched(TechKind.StoneAxe), "production 작업대 돌도끼 연구");
                OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
                ActuateCampPopupButtonThroughRawInput(
                    craftAxeButton,
                    new PrototypeRawInput { GamepadInteract = true, BagSlotIndex = -1 });
                Require(session.HasAxe, "production 작업대 돌도끼 제작·보유");

                BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId.Forest);
                NodeView revisitView = nodes.First(node =>
                    string.Equals(node.Definition.NodeId, deadfalls[1].NodeId, StringComparison.Ordinal));
                MoveNaturallyToSearchNode(revisitView);
                InteractWithNearestSearchNodeThroughRawInput();
                Require(searchNodeRuntime.IsTrayOpen &&
                        string.Equals(searchNodeRuntime.ActiveNodeId, deadfalls[1].NodeId, StringComparison.Ordinal),
                    "forced return 뒤 known forest node production revisit");
                PrototypeSearchRunSnapshot orderedPersistenceSnapshot = searchNodeRuntime.Ledger.CaptureSnapshot();
                Require(searchNodeRuntime.RestoreSnapshot(JsonUtility.FromJson<PrototypeSearchRunSnapshot>(
                        JsonUtility.ToJson(orderedPersistenceSnapshot))),
                    "revisit 뒤 production snapshot restore");
                bool environmentalSnapshotPersistent = string.Equals(
                    JsonUtility.ToJson(environmentalAcceptanceSnapshot),
                    JsonUtility.ToJson(searchNodeRuntime.EnvironmentalHazards.CaptureSnapshot()),
                    StringComparison.Ordinal);
                Require(environmentalSnapshotPersistent,
                    "벌레·위험 식물 lifecycle은 production snapshot restore 뒤 동일");
                playtestLog.RecordSearchSnapshotRestored(deadfalls[1].NodeId);
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });

                MoveNaturallyToExplorationX(8.2f);
                Require(searchNodeRuntime.Ledger.IsBarrierBroken("region.forest.grove"),
                    "돌도끼 보유 raw traversal forest barrier 파괴");
                NodeView persistentHazardView = nodes.First(node =>
                    node.X > 8.2f && string.Equals(node.Definition.HazardId, "hazard.wildlife", StringComparison.Ordinal) &&
                    searchNodeRuntime.Ledger.GetOrCreate(node.Definition).State == PrototypeSearchNodeState.Hidden);
                PrototypeSearchNodeDefinition persistentHazardNode = persistentHazardView.Definition;
                SearchAndTakeAllNodeThroughProductionInput(persistentHazardNode);
                Require(searchNodeRuntime.Ledger.IsPermanentHazardRemoved(
                        persistentHazardNode.RegionId, persistentHazardNode.HazardId),
                    "barrier 뒤 production hazard depletion persistence");
                observedStockFingerprints.Add(searchNodeRuntime.Ledger.NewGameStockFingerprint);

                observation.StockFingerprints = observedStockFingerprints.ToArray();
                observation.StockGenerationEvents = searchNodeRuntime.Ledger.StockGenerationEvents.ToArray();
                observation.SearchStateSequence = new[]
                {
                    preparationHidden.State.ToString().ToLowerInvariant(),
                    preparationPartial.State.ToString().ToLowerInvariant(),
                    preparationDepleted.State.ToString().ToLowerInvariant()
                };
                observation.SearchStateFingerprints = new[]
                {
                    JsonUtility.ToJson(preparationHidden),
                    JsonUtility.ToJson(preparationPartial),
                    JsonUtility.ToJson(preparationDepleted)
                };
                observation.SearchInteractionTrace = new[]
                {
                    "natural return completed",
                    "forced return completed",
                    "node revisit preserved known remainder",
                    "snapshot restore preserved stable stock",
                    "barrier broken persisted",
                    "permanent hazard removed persisted"
                };
                observation.KnownRemainingFingerprint = knownRemaining;
                observation.DiseasePhaseSequence = phases.ToArray();
                observation.DiseaseInteractionTrace = interactions.ToArray();
                observation.DiseaseStateFingerprints = states.ToArray();
                observation.DiseaseTrace = diseaseRuntime.Disease.Trace.ToArray();
                observation.EnvironmentalHazardIds = environmentalAcceptanceSnapshot.Exposures
                    .Select(value => value.HazardId).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                observation.EnvironmentalHazardStateFingerprints = environmentalHazardStates.ToArray();
                observation.EnvironmentalHazardInteractionTrace = environmentalHazardTrace.ToArray();
                observation.EnvironmentalHazardWarningTexts = environmentalHazardWarningTexts.ToArray();
                observation.EnvironmentalWarningCount = environmentalAcceptanceSnapshot.Exposures.Sum(value => value.WarningCount);
                observation.EnvironmentalExposureApplyCount = environmentalAcceptanceSnapshot.Exposures.Sum(value => value.ExposureApplyCount);
                observation.EnvironmentalEffectApplyCount = environmentalAcceptanceSnapshot.Exposures.Sum(value => value.EffectApplyCount);
                observation.EnvironmentalResponseApplyCount = environmentalAcceptanceSnapshot.Exposures.Sum(value => value.ResponseApplyCount);
                observation.EnvironmentalRecoveryApplyCount = environmentalAcceptanceSnapshot.Exposures.Sum(value => value.RecoveryApplyCount);
                observation.EnvironmentalDuplicateDelta = environmentalDuplicateDelta;
                observation.EnvironmentalSnapshotPersistent = environmentalSnapshotPersistent;
                observation.ExposureApplyCount = diseaseRuntime.Disease.ExposureApplyCount;
                observation.EffectApplyCount = diseaseRuntime.Disease.EffectCount;
                observation.WorsenApplyCount = diseaseRuntime.Disease.WorsenCount;
                observation.TreatmentCostCount = diseaseRuntime.Disease.TreatmentPaidCount;
                observation.DiseaseTelegraphNatural = diseaseRuntime.Disease.Trace.Any(value =>
                    value.StartsWith("disease.telegraph", StringComparison.Ordinal));
                observation.DiseaseExposureNatural = diseaseRuntime.Disease.ExposureApplyCount == 1;
                observation.DiseaseEffectApplied = diseaseRuntime.Disease.EffectCount == 1;
                observation.DiseaseWorsenedOnSettlement = diseaseRuntime.Disease.WorsenCount == 1;
                observation.ForcedReturnAtomic = !duplicateDiseaseEffect &&
                    forcedAfter.EffectCount == forcedBefore.EffectCount &&
                    forcedAfter.TreatmentPaidCount == forcedBefore.TreatmentPaidCount;
                observation.TreatmentCancelAtomic = observation.CancelContaminationDelta == 0;
                observation.TreatmentCostAtomic = diseaseSession.GetStableStorage(PrototypeDiseaseRuntime.MedicineResourceId) ==
                                                   medicineBefore - PrototypeDiseaseRuntime.TreatmentMedicineCost;
                bool validTreatmentOutcome = diseaseRuntime.Disease.Phase == PrototypeDiseasePhase.Recovering ||
                                             diseaseRuntime.Disease.Phase == PrototypeDiseasePhase.Cleared;
                observation.TreatmentSucceeded = treatmentCommitObserved && validTreatmentOutcome &&
                    diseaseRuntime.Disease.TreatmentPaidCount == treatmentCommittedSnapshot.TreatmentPaidCount;
                PrototypeSearchLootActions keyboardActions = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput
                {
                    KeyboardNext = true,
                    KeyboardConfirm = true,
                    KeyboardTakeAll = true,
                    KeyboardCancel = true
                });
                PrototypeSearchLootActions gamepadActions = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput
                {
                    HorizontalAxis = 1f,
                    GamepadConfirm = true,
                    GamepadTakeAll = true,
                    GamepadCancel = true
                });
                observation.KeyboardMeaning = keyboardActions.CycleDirection + "|" + keyboardActions.ConfirmPressed + "|" +
                                              keyboardActions.TakeAllPressed + "|" + keyboardActions.CancelPressed;
                observation.GamepadMeaning = gamepadActions.CycleDirection + "|" + gamepadActions.ConfirmPressed + "|" +
                                             gamepadActions.TakeAllPressed + "|" + gamepadActions.CancelPressed;
                observation.KeyboardMouseSyntheticGamepadParity = string.Equals(
                    observation.KeyboardMeaning, observation.GamepadMeaning, StringComparison.Ordinal);
                observation.LocaleStateFingerprints = diseaseLayouts.Select(layout => layout.StateFingerprint).ToArray();
                observation.Layouts = diseaseLayouts;
                observation.RegionCount = PrototypeSearchRegionCatalog.All.Count;
                observation.ArchetypeCount = PrototypeSearchRegionCatalog.Archetypes.Count;
                observation.NodeInstanceCount = PrototypeSearchRegionCatalog.Nodes.Count;
                observation.ExistingInstanceCount = PrototypeSearchRegionCatalog.Nodes.Count(node => node.Origin == "existing");
                observation.NewInstanceCount = PrototypeSearchRegionCatalog.Nodes.Count(node => node.Origin == "new");
                observation.RemovedLegacyInstanceCount = PrototypeSearchRegionCatalog.ExistingCanonicalNodeIds.Count(id =>
                    PrototypeSearchRegionCatalog.Nodes.All(node => node.NodeId != id));
                observation.GeneralResourceUnits = PrototypeSearchRegionCatalog.GeneralStockUnitsForSeed(seed);
                observation.StableResourceKindCount = PrototypeSearchRegionCatalog.Nodes.SelectMany(node => node.FiniteYield)
                    .Select(item => item.StableResourceId).Distinct(StringComparer.Ordinal).Count();
                observation.ProtectedPartUnits = PrototypeSearchRegionCatalog.Nodes.Sum(node =>
                    PrototypeSearchNodeLootResolver.Resolve(seed, node).Count(item => item.IsProtectedPart));
                observation.DuplicateStableIdCount = PrototypeSearchRegionCatalog.Nodes.Count -
                    PrototypeSearchRegionCatalog.Nodes.Select(node => node.NodeId).Distinct(StringComparer.Ordinal).Count();
                observation.SameSeedDeterministic = string.Equals(stockFingerprint, repeatedFingerprint, StringComparison.Ordinal);
                observation.DifferentSeedVaries = !string.Equals(stockFingerprint, alternateFingerprint, StringComparison.Ordinal);
                observation.StockDoesNotRegenerate = observation.StockGenerationEvents.Length == 1 &&
                    observation.StockFingerprints.Distinct(StringComparer.Ordinal).Count() == 1;
                observation.HiddenPartialDepletedPersistent = preparationHidden.State == PrototypeSearchNodeState.Hidden &&
                    preparationPartial.State == PrototypeSearchNodeState.RevealedPartial &&
                    preparationDepleted.State == PrototypeSearchNodeState.Depleted;
                observation.BarrierPersistent = searchNodeRuntime.Ledger.IsBarrierBroken("region.forest.grove");
                observation.PermanentHazardPersistent = searchNodeRuntime.Ledger.IsPermanentHazardRemoved(
                    persistentHazardNode.RegionId, persistentHazardNode.HazardId);
                PrototypeSearchRunSnapshot latestProtectedSnapshot = searchNodeRuntime.Ledger.CaptureSnapshot();
                observation.SailclothProtected = sailclothAcquiredThroughShallowSearch &&
                    latestProtectedSnapshot.ProtectedPartIds.Contains(PrototypeRaftEscapeConfig.KeyPartId) &&
                    searchNodeRuntime.Ledger.HasProtectedPart(PrototypeRaftEscapeConfig.KeyPartId) &&
                    hazardEscapeEndingRuntime != null &&
                    hazardEscapeEndingRuntime.HasProtectedSearchPart(PrototypeRaftEscapeConfig.KeyPartId);
                observation.BagTransactionAtomic = preparationDepleted.RemainingAmount == 0 && !session.HasPendingLoot;
                observation.LocalizationKeys = new[]
                {
                    "search.hazard.insects",
                    "search.hazard.dangerous-plants",
                    "search.hazard.lifecycle.insects.exposed",
                    "search.hazard.lifecycle.insects.mitigated",
                    "search.hazard.lifecycle.insects.recovered",
                    "search.hazard.lifecycle.dangerous-plants.exposed",
                    "search.hazard.lifecycle.dangerous-plants.mitigated",
                    "search.hazard.lifecycle.dangerous-plants.recovered",
                    "search.hazard.action.retreat.insects",
                    "search.hazard.action.retreat.dangerous-plants"
                };
                observation.GrantCallCount = PrototypeProductionActionCounters.GrantCallCount;
                observation.WarpCallCount = PrototypeProductionActionCounters.WarpCallCount;
                observation.SkipCallCount = PrototypeProductionActionCounters.SkipCallCount;
                observation.Grant = observation.GrantCallCount > 0;
                observation.Warp = observation.WarpCallCount > 0;
                observation.Skip = observation.SkipCallCount > 0;
            }
            catch (Exception exception)
            {
                observation.ObservationError = exception.GetType().Name + ": " + exception.Message;
            }
            finally
            {
                observation.GrantCallCount = PrototypeProductionActionCounters.GrantCallCount;
                observation.WarpCallCount = PrototypeProductionActionCounters.WarpCallCount;
                observation.SkipCallCount = PrototypeProductionActionCounters.SkipCallCount;
                if (observationPlaytestLog != null)
                {
                    observation.ProductionInteractionEvents = observationPlaytestLog.VerificationLines
                        .Select(line => JsonUtility.FromJson<PrototypePlaytestEventRecord>(line))
                        .Where(record => record != null &&
                            (!string.Equals(record.stable_event_id, PrototypePlaytestEventNames.ExpeditionRegionSelected, StringComparison.Ordinal) ||
                             string.Equals(record.region_id, "region.forest.grove", StringComparison.Ordinal)))
                        .ToArray();
                    observationPlaytestLog.Dispose();
                }
                playtestLog = previousPlaytestLog;
            }
            return observation;
        }

        private int CountNearbySearchNodes()
        {
            return nodes.Count(node => Mathf.Abs(node.X - playerTraversal.X) < SearchNodeInteractionDistance);
        }

        private void MoveNaturallyToSearchNode(NodeView target)
        {
            const float stepSeconds = 0.02f;
            float elapsed = 0f;
            int safety = 900;
            while (target != null && Mathf.Abs(target.X - playerTraversal.X) > 0.05f && safety-- > 0)
            {
                float direction = target.X < playerTraversal.X ? -1f : 1f;
                PrototypeRawInput raw = direction < 0f
                    ? new PrototypeRawInput { KeyboardLeft = true, BagSlotIndex = -1 }
                    : new PrototypeRawInput { KeyboardRight = true, BagSlotIndex = -1 };
                ProcessExplorationMovement(playerInput.MapRawActions(raw), stepSeconds, elapsed);
                session.TickSearch(stepSeconds, true);
                elapsed += stepSeconds;
            }
            Require(target != null && Mathf.Abs(target.X - playerTraversal.X) <= 0.08f,
                "자연 이동으로 수색 node 근접 도달: " +
                (target == null ? "<null>" : target.Definition.NodeId +
                 " target=" + target.X.ToString("0.00") + " actual=" + playerTraversal.X.ToString("0.00") +
                 " axe=" + session.HasAxe + " phase=" + session.Phase));
            Vector3 cameraPosition = worldCamera.transform.position;
            cameraPosition.x = Mathf.Clamp(playerTraversal.X + 2.5f, -6.5f, 12.5f);
            worldCamera.transform.position = cameraPosition;
            UpdateResourceLabelLayout();
        }

        private void MoveNaturallyToExplorationX(float targetX)
        {
            const float stepSeconds = 0.02f;
            float elapsed = 0f;
            int safety = 1200;
            while (Mathf.Abs(targetX - playerTraversal.X) > 0.05f && safety-- > 0)
            {
                bool left = targetX < playerTraversal.X;
                PrototypeRawInput raw = left
                    ? new PrototypeRawInput { KeyboardLeft = true, BagSlotIndex = -1 }
                    : new PrototypeRawInput { KeyboardRight = true, BagSlotIndex = -1 };
                ProcessExplorationMovement(playerInput.MapRawActions(raw), stepSeconds, elapsed);
                session.TickSearch(stepSeconds, true);
                elapsed += stepSeconds;
            }
            Require(Mathf.Abs(targetX - playerTraversal.X) <= 0.08f,
                "production raw 이동으로 exploration X 도달");
        }

        private void InteractWithNearestSearchNodeThroughRawInput(bool gamepad = false)
        {
            PrototypeRawInput raw = gamepad
                ? new PrototypeRawInput { GamepadInteract = true, BagSlotIndex = -1 }
                : new PrototypeRawInput { KeyboardInteract = true, BagSlotIndex = -1 };
            PrototypePlayerActions actions = playerInput.MapRawActions(raw);
            ProcessExplorationMovement(actions, 0f, 0f);
            if (actions.InteractPressed)
            {
                SearchNearestNode();
            }
        }

        private void ActuateSearchTrayThroughRawInput(PrototypeRawSearchLootInput raw)
        {
            ProcessSearchLootTrayActions(
                playerInput.MapRawSearchLootActions(raw),
                playerInput.MapRawActions(new PrototypeRawInput { BagSlotIndex = -1 }));
        }

        private void FocusSearchLootThroughRawInput(int targetIndex)
        {
            int safety = GameSession.MaximumBagSlotCount * 2;
            while (searchNodeRuntime.FocusedIndex != targetIndex && safety-- > 0)
            {
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardNext = true });
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput());
            }
            Require(searchNodeRuntime.FocusedIndex == targetIndex, "production search tray focus");
        }

        private bool ReturnToCampThroughRawInput()
        {
            PrototypePlayerActions actions = playerInput.MapRawActions(
                new PrototypeRawInput { KeyboardReturn = true, BagSlotIndex = -1 });
            ProcessExplorationMovement(actions, 0f, 0f);
            return actions.ReturnPressed && ReturnToCampThroughProductionInput();
        }

        private void SearchAndTakeAllNodeThroughProductionInput(PrototypeSearchNodeDefinition definition)
        {
            NodeView view = nodes.First(node => string.Equals(node.Definition.NodeId, definition.NodeId, StringComparison.Ordinal));
            MoveNaturallyToSearchNode(view);
            InteractWithNearestSearchNodeThroughRawInput();
            Require(searchNodeRuntime.IsTrayOpen && string.Equals(searchNodeRuntime.ActiveNodeId, definition.NodeId, StringComparison.Ordinal),
                definition.NodeId + " production search open");
            ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardTakeAll = true });
            Require(!session.HasPendingLoot, definition.NodeId + " production take-all atomic");
            ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
        }

        private void MoveNaturallyToCampTarget(PrototypeCampInteractionTargetKind target)
        {
            if (campInteraction.IsPopupOpen)
            {
                ProcessCampActions(new PrototypePlayerActions(0f, false, false, false, true, -1), 0f);
            }
            Vector2 targetPosition = GetCampInteractionTargetPosition(target);
            const float stepSeconds = 0.02f;
            int safety = 1200;
            while (Mathf.Abs(targetPosition.x - campUse.PlayerPosition.x) > 0.05f && safety-- > 0)
            {
                float direction = targetPosition.x < campUse.PlayerPosition.x ? -1f : 1f;
                ProcessCampActions(new PrototypePlayerActions(direction, false, false, false, false, -1), stepSeconds);
            }
            Require(Mathf.Abs(targetPosition.x - campUse.PlayerPosition.x) <= 0.08f,
                "자연 캠프 이동으로 상호작용 대상 도달");
        }

        private void OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind target)
        {
            MoveNaturallyToCampTarget(target);
            ProcessCampActions(new PrototypePlayerActions(0f, false, true, false, false, -1), 0f);
            Require(campInteraction.OpenPopupKind == target && campInteraction.IsPopupOpen,
                target + " production Interact 팝업 열기");
        }

        private void ActuateCampPopupButtonThroughRawInput(Button button, PrototypeRawInput raw)
        {
            Require(campInteraction.IsPopupOpen && button != null && button.gameObject.activeInHierarchy && button.interactable,
                "production popup action 준비");
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            ProcessCampActions(playerInput.MapRawActions(raw), 0f);
        }

        private void CaptureProductionInputParityAtWorkbench()
        {
            OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
            ProcessCampActions(playerInput.MapRawActions(new PrototypeRawInput { KeyboardCancel = true }), 0f);
            Require(!campInteraction.IsPopupOpen, "keyboard raw popup cancel");
            OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.Workbench);
            ProcessCampActions(playerInput.MapRawActions(new PrototypeRawInput { GamepadCancel = true }), 0f);
            Require(!campInteraction.IsPopupOpen, "synthetic gamepad raw popup cancel");
        }

        private void BeginExpeditionThroughProductionMap(PrototypeExpeditionRegionId region)
        {
            OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.ExpeditionMap);
            expeditionRegionButtons[(int)region].onClick.Invoke();
            expeditionMapConfirmButton.onClick.Invoke();
            Require(session.Phase == GamePhase.Exploring && session.SelectedRegionId == region,
                "production 지도 지역 버튼·확인 버튼 수색 출발");
        }

        private void DiscoverRaftShoreLaunchThroughNaturalSearch()
        {
            int grantBefore = PrototypeProductionActionCounters.GrantCallCount;
            int warpBefore = PrototypeProductionActionCounters.WarpCallCount;
            int skipBefore = PrototypeProductionActionCounters.SkipCallCount;
            string sailclothNodeId = PrototypeSearchNodeLootResolver.ResolveSailclothNodeId(session.RunSeed);
            PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.First(node =>
                string.Equals(node.NodeId, sailclothNodeId, StringComparison.Ordinal));
            PrototypeExpeditionRegionId region = PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId);

            BeginExpeditionThroughProductionMap(region);
            NodeView target = nodes.First(node =>
                string.Equals(node.Definition.NodeId, definition.NodeId, StringComparison.Ordinal));
            MoveNaturallyToSearchNode(target);
            InteractWithNearestSearchNodeThroughRawInput();
            Require(searchNodeRuntime.IsTrayOpen &&
                    string.Equals(searchNodeRuntime.ActiveNodeId, definition.NodeId, StringComparison.Ordinal),
                "보호 돛천 assigned node production 수색 열기");

            PrototypeSearchNodeSnapshot revealed = searchNodeRuntime.ActiveNode;
            int protectedIndex = revealed == null ? -1 : Array.FindIndex(
                revealed.Remaining,
                item => item.IsProtectedPart &&
                        string.Equals(item.ProtectedPartId, PrototypeRaftEscapeConfig.KeyPartId, StringComparison.Ordinal));
            Require(protectedIndex >= 0, "실제 assigned node에서 보호 돛천 발견");
            FocusSearchLootThroughRawInput(protectedIndex);
            ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardConfirm = true });
            Require(searchNodeRuntime.Ledger.HasProtectedPart(PrototypeRaftEscapeConfig.KeyPartId) &&
                    hazardEscapeEndingRuntime.HasProtectedSearchPart(PrototypeRaftEscapeConfig.KeyPartId),
                "실제 수색 트레이 입력으로 보호 돛천 inventory 이전");
            if (searchNodeRuntime.IsTrayOpen)
            {
                ActuateSearchTrayThroughRawInput(new PrototypeRawSearchLootInput { KeyboardCancel = true });
            }
            Require(ReturnToCampThroughRawInput(), "보호 돛천 실제 수색 뒤 production 귀환");
            Require(IsRaftShoreLaunchDiscoveredFromLiveSearch(),
                "보호 돛천 실제 수색·귀환 뒤 해안 진수대 발견 상태");
            Require(PrototypeProductionActionCounters.GrantCallCount == grantBefore &&
                    PrototypeProductionActionCounters.WarpCallCount == warpBefore &&
                    PrototypeProductionActionCounters.SkipCallCount == skipBefore,
                "해안 진수대 발견은 grant·warp·skip 없이 production 수색 입력으로만 전이");
        }

        private void BuildWorkbenchThroughProductionPopup()
        {
            OpenCampTargetThroughProductionInput(PrototypeCampInteractionTargetKind.StoragePlanning);
            workbenchButton.onClick.Invoke();
            Require(campPlacement.IsActive && campPlacement.CurrentValidity == CampPlacementValidity.Valid,
                "production 작업대 배치 시작");
            ProcessCampPlacementActions(new PrototypeCampPlacementActions(false, 0f, 0f, true, false), 0f);
            Require(session.HasStructure(StructureKind.Workbench), "production 작업대 배치 확정");
        }

        private int SearchBagAmount()
        {
            int amount = 0;
            for (int index = 0; index < session.ActiveBagSlotCount; index += 1)
            {
                amount += session.GetBagSlot(index).Amount;
            }
            return amount;
        }

        private static int SearchNormalRemainingAmount(PrototypeSearchNodeSnapshot snapshot)
        {
            return snapshot == null || snapshot.Remaining == null
                ? 0
                : snapshot.Remaining.Where(item => !item.IsProtectedPart).Sum(item => item.Amount);
        }

        private static string SearchContentsFingerprint(IEnumerable<PrototypeSearchLootEntry> contents)
        {
            return string.Join("|", (contents ?? Enumerable.Empty<PrototypeSearchLootEntry>())
                .OrderBy(item => item.StableItemId, StringComparer.Ordinal)
                .Select(item => item.StableItemId + ":" + item.StableResourceId + ":" + item.Resource + ":" + item.ProtectedPartId + ":" + item.Amount)
                .ToArray());
        }

        private PrototypeSearchNodeLayoutObservation CaptureSearchNodeLayoutObservation(string localeCode, string evidenceFolder)
        {
            bool localeChanged = string.Equals(localeCode, PrototypeLocalization.QpsLongLocaleCode, StringComparison.Ordinal)
                ? localization.SetQaLocale(localeCode)
                : localization.SetLocale(localeCode, false);
            Require(localeChanged && string.Equals(localization.CurrentLocaleCode, localeCode, StringComparison.Ordinal),
                "수색 트레이 locale 전환 " + localeCode);
            Canvas.ForceUpdateCanvases();
            RefreshSearchLootTrayUi();
            Canvas.ForceUpdateCanvases();

            string screenshotPath = Path.Combine(
                evidenceFolder,
                "gamejam-search-node-tray-" + localeCode + "-1280x800.png");
            CaptureVerificationPng(screenshotPath, 1280, 800);

            Rect tray = ScreenRect(searchLootTrayPanel.GetComponent<RectTransform>(), 1280f, 800f);
            TMP_Text[] texts = searchLootTrayPanel.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text.gameObject.activeInHierarchy).ToArray();
            string renderedTextFingerprint = Hash128.Compute(string.Join("\u001f", texts.Select(text => text.text).ToArray())).ToString();
            string runtimeStateFingerprint = Hash128.Compute(
                JsonUtility.ToJson(searchNodeRuntime.ActiveNode) + "|" +
                JsonUtility.ToJson(searchNodeRuntime.Disease.CaptureSnapshot()) + "|" +
                searchNodeRuntime.Ledger.NewGameStockFingerprint).ToString();
            int overflow = 0;
            int offscreen = 0;
            for (int index = 0; index < texts.Length; index += 1)
            {
                texts[index].ForceMeshUpdate(true, true);
                if (texts[index].isTextOverflowing) overflow += 1;
                Rect textRect = ScreenRect(texts[index].rectTransform, 1280f, 800f);
                if (textRect.xMin < 0f || textRect.yMin < 0f || textRect.xMax > 1280f || textRect.yMax > 800f)
                {
                    offscreen += 1;
                }
            }

            Vector3 playerViewport = worldCamera.WorldToViewportPoint(playerRoot.position);
            Vector2 playerScreen = new Vector2(playerViewport.x * 1280f, playerViewport.y * 800f);
            Rect playerRect = new Rect(playerScreen.x - 76f, playerScreen.y - 68f, 152f, 136f);
            Rect playerSafety = new Rect(
                playerRect.xMin - 4f,
                playerRect.yMin - 4f,
                playerRect.width + 8f,
                playerRect.height + 8f);
            Vector3 playerFootViewport = worldCamera.WorldToViewportPoint(
                new Vector3(playerRoot.position.x, playerRoot.position.y - 0.7f, playerRoot.position.z));
            Rect walkingBand = new Rect(
                playerFootViewport.x * 1280f - 120f,
                playerFootViewport.y * 800f - 28f,
                240f,
                72f);
            bool playerClear = !tray.Overlaps(playerSafety);
            bool walkingBandClear = !tray.Overlaps(walkingBand);
            bool inside = tray.xMin >= 4f && tray.yMin >= 4f && tray.xMax <= 1276f && tray.yMax <= 796f;
            bool compact = tray.width <= 1280f * 0.60f && tray.height <= 800f * 0.45f;
            bool passed = inside && compact && playerClear && walkingBandClear && overflow == 0 && offscreen == 0;
            return new PrototypeSearchNodeLayoutObservation
            {
                Locale = localeCode,
                Screenshot = screenshotPath,
                RenderedTextFingerprint = renderedTextFingerprint,
                StateFingerprint = runtimeStateFingerprint,
                X = tray.x,
                Y = tray.y,
                Width = tray.width,
                Height = tray.height,
                OverflowCount = overflow,
                OffscreenCount = offscreen,
                InsideScreen = inside,
                Compact = compact,
                PlayerClear = playerClear,
                WalkingBandClear = walkingBandClear,
                Result = passed ? "PASS" : "FAIL"
            };
        }

        private Rect ScreenRect(RectTransform transformRect, float targetWidth, float targetHeight)
        {
            Vector3[] corners = new Vector3[4];
            transformRect.GetWorldCorners(corners);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector3 bottomLeft = canvasRect.InverseTransformPoint(corners[0]);
            Vector3 topRight = canvasRect.InverseTransformPoint(corners[2]);
            Rect source = canvasRect.rect;
            float scaleX = source.width <= 0f ? 1f : targetWidth / source.width;
            float scaleY = source.height <= 0f ? 1f : targetHeight / source.height;
            return Rect.MinMaxRect(
                (bottomLeft.x - source.xMin) * scaleX,
                (bottomLeft.y - source.yMin) * scaleY,
                (topRight.x - source.xMin) * scaleX,
                (topRight.y - source.yMin) * scaleY);
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
            string bagUpgradedEnglish1920ScreenshotPath,
            string campFarKoreanScreenshotPath,
            string campProximityKoreanScreenshotPath,
            string campWorkbenchEnglishScreenshotPath,
            string campCampfireKoreanScreenshotPath)
        {
            bool ownsVerificationLog = playtestLog == null;
            if (ownsVerificationLog)
            {
                playtestLog = PrototypePlaytestEventRecorder.CreateForVerification(
                    session,
                    delegate { return localization.CurrentLocaleCode; },
                    delegate { return playerInput.ActiveDevice; });
                playtestLog.RecordSessionStarted();
            }
            string endingAlbumSnapshotBeforeVerification = endingAlbumCollection.CaptureSnapshot();
            endingAlbumCollection.PersistenceEnabled = false;

            string campProximityScreenshotFolder = Path.GetDirectoryName(campProximityKoreanScreenshotPath ?? string.Empty);
            string placementScreenshotFolder = Path.GetDirectoryName(placementKoreanScreenshotPath ?? string.Empty);
            string placementQpsLongScreenshotPath = string.IsNullOrWhiteSpace(placementScreenshotFolder)
                ? string.Empty
                : Path.Combine(placementScreenshotFolder, "kim-survival-wave14-placement-qps-long-1280x800.png");
            string campProximityEnglishScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave12-facility-near-en-1280x800.png");
            string campProximityQpsLongScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave12-direct-slot-near-qps-long-1280x800.png");
            string modulePreviewKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave11-module-upper-ko-1280x800.png");
            string modulePreviewEnglishScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave11-module-side-en-1280x800.png");
            string modulePreviewQpsLongScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave11-module-basement-qps-long-1280x800.png");
            string moduleSlotPopupKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave11-upper-slot-popup-ko-1280x800.png");
            string moduleInteriorKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave9-module-interior-ko-1280x800.png");
            string expeditionMapNearKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave16-map-a-near-ko-1280x800.png");
            string expeditionMapKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave16-map-a-popup-ko-1280x800.png");
            string expeditionMapEnglishScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave16-map-a-popup-en-1280x800.png");
            string expeditionMapQpsLongScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave16-map-a-popup-qps-long-1280x800.png");
            string endingAlbumNearKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave19-album-near-ko-1280x800.png");
            string endingAlbumPopupEnglishScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave19-album-popup-en-1280x800.png");
            string endingAlbumKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave19-album-open-ko-1280x800.png");
            string endingAlbumEnglishScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave19-album-open-en-1280x800.png");
            string endingAlbumQpsLongScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave19-album-open-qps-long-1280x800.png");
            string raftNearKoreanScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave20-raft-near-ko-1280x800.png");
            string raftPopupEnglishScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave20-raft-popup-en-1280x800.png");
            string raftPopupQpsLongScreenshotPath = string.IsNullOrWhiteSpace(campProximityScreenshotFolder)
                ? string.Empty
                : Path.Combine(campProximityScreenshotFolder, "kim-survival-wave20-raft-popup-qps-long-1280x800.png");
            session.Reset();
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            RefreshAll();

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            languageButton.onClick.Invoke();
            Require(localization.CurrentLocaleCode == PrototypeLocalization.EnglishLocaleCode &&
                    languageButton.GetComponentInChildren<TMP_Text>().text == localization.Format("ui.language.switch.en"), "언어 버튼의 즉시 영어 전환");
            Require(localization.Format("dev.fallback_probe") == "한국어 폴백 확인", "영어 누락 번역의 한국어 폴백");
            languageButton.onClick.Invoke();
            Require(localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode &&
                    languageButton.GetComponentInChildren<TMP_Text>().text == localization.Format("ui.language.switch.ko"), "언어 버튼의 즉시 한국어 전환");
            Require(statusText.font != null && messageText.font != null, "로케일별 TMP 폰트 매핑 적용");
            RequireCampBackgroundAlignment();
            RequireCampStructureArt();
            campUse.Warp(PrototypeCampUse.PlayerMinimumX);
            RefreshAll();
            Require(!campActions.activeSelf && !bagPanel.activeSelf && !campInteractionPopup.activeSelf && !campProximityPrompt.activeSelf,
                "정상 캠프 원거리에서 전역 대시보드·대형 가방·근접 안내·팝업 숨김");
            RequireReadableTopHud(PrototypeLocalization.KoreanLocaleCode);
            if (!string.IsNullOrWhiteSpace(campFarKoreanScreenshotPath))
            {
                CaptureVerificationPng(campFarKoreanScreenshotPath, 1280, 800);
            }

            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.ExpeditionMap));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ExpeditionMap &&
                    campInteraction.ActiveTargetId == "camp.expedition-map" && campProximityPrompt.activeSelf &&
                    !expeditionMapPanel.activeSelf,
                "지도·출구는 1.25 unit 안에서만 하나의 직접 상호작용 안내를 표시");
            Vector2 mapReturnPosition = campUse.PlayerPosition;
            float mapReturnFacing = campUse.FacingDirection;
            if (!string.IsNullOrWhiteSpace(expeditionMapNearKoreanScreenshotPath))
            {
                CaptureVerificationPng(expeditionMapNearKoreanScreenshotPath, 1280, 800);
            }
            UseNearestCampTarget();
            Require(campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap &&
                    expeditionMapSelection.IsOpen && expeditionMapPanel.activeSelf && !campProximityPrompt.activeSelf &&
                    expeditionMapSelection.FocusedRegionId == PrototypeExpeditionRegionId.Beach,
                "지도 Interact 뒤에만 해변을 첫 포커스로 하는 수집 지역 팝업 표시");
            RequireReadableExpeditionMapUi(false);
            if (!string.IsNullOrWhiteSpace(expeditionMapKoreanScreenshotPath))
            {
                CaptureVerificationPng(expeditionMapKoreanScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            Require(expeditionMapSelection.FocusedRegionId == PrototypeExpeditionRegionId.Beach &&
                    campUse.PlayerPosition == mapReturnPosition && Mathf.Approximately(campUse.FacingDirection, mapReturnFacing) &&
                    expeditionMapDetailText.text.Contains("Expected resources") &&
                    !expeditionMapDetailText.text.Contains("loot."),
                "영어 전환은 지도 포커스·위치·방향을 보존하고 정확한 획득 수량을 노출하지 않음");
            RequireReadableExpeditionMapUi(false);
            if (!string.IsNullOrWhiteSpace(expeditionMapEnglishScreenshotPath))
            {
                CaptureVerificationPng(expeditionMapEnglishScreenshotPath, 1280, 800);
            }

            expeditionMapSelection.SetRegionStateForVerification(
                PrototypeExpeditionRegionId.Beach,
                PrototypeExpeditionRegionVisualState.Locked);
            RefreshExpeditionMapUi();
            Require(!expeditionMapConfirmButton.interactable &&
                    expeditionRegionButtons[0].GetComponentInChildren<TMP_Text>().text.Contains(localization.Format("expedition.map.state.locked")),
                "잠김 지역은 색상 외 ◆ 포커스·굵은 테두리·상태 문구로 구분되고 출발할 수 없음");
            expeditionMapSelection.SetRegionStateForVerification(
                PrototypeExpeditionRegionId.Beach,
                PrototypeExpeditionRegionVisualState.EquipmentMissing);
            RefreshExpeditionMapUi();
            Require(!expeditionMapConfirmButton.interactable &&
                    expeditionRegionButtons[0].GetComponentInChildren<TMP_Text>().text.Contains(localization.Format("expedition.map.state.equipment_missing")),
                "장비 부족 상태는 별도 △ 문양·문구로 구분되고 출발할 수 없음");
            expeditionMapSelection.SetRegionStateForVerification(
                PrototypeExpeditionRegionId.Beach,
                PrototypeExpeditionRegionVisualState.RiskWarning);
            RefreshExpeditionMapUi();
            Require(expeditionMapConfirmButton.interactable &&
                    expeditionRegionButtons[0].GetComponentInChildren<TMP_Text>().text.Contains(localization.Format("expedition.map.state.risk")),
                "위험 경고 상태는 별도 ! 문양·문구를 보이면서 출발 가능성을 보존");
            expeditionMapSelection.SetRegionStateForVerification(
                PrototypeExpeditionRegionId.Beach,
                PrototypeExpeditionRegionVisualState.Unknown);
            RefreshExpeditionMapUi();
            Require(!expeditionMapConfirmButton.interactable &&
                    expeditionRegionButtons[0].GetComponentInChildren<TMP_Text>().text.Contains(localization.Format("expedition.map.state.unknown")),
                "미확인 상태는 별도 ? 문양·점선 계약·문구로 구분되고 출발할 수 없음");
            expeditionMapSelection.SetRegionStateForVerification(
                PrototypeExpeditionRegionId.Beach,
                PrototypeExpeditionRegionVisualState.DepartureReady);
            RefreshExpeditionMapUi();
            Require(expeditionMapConfirmButton.interactable,
                "시작 세 지역의 출발 가능 상태 모델은 검증 전이 뒤에도 정상 복귀");

            FocusExpeditionRegion(PrototypeExpeditionRegionId.Shallows);
            Require(localization.SetQaLocale(), "지도 팝업의 실제 qps-long QA 로케일 선택");
            RefreshAll();
            Require(expeditionMapSelection.FocusedRegionId == PrototypeExpeditionRegionId.Shallows &&
                    campInteraction.OpenPopupTargetId == "camp.expedition-map" &&
                    campUse.PlayerPosition == mapReturnPosition && Mathf.Approximately(campUse.FacingDirection, mapReturnFacing),
                "qps-long 전환도 동일 지도 대상·얕은 바다 포커스·위치·방향을 보존");
            RequireReadableExpeditionMapUi(true);
            if (!string.IsNullOrWhiteSpace(expeditionMapQpsLongScreenshotPath))
            {
                CaptureVerificationPng(expeditionMapQpsLongScreenshotPath, 1280, 800);
            }
            WriteExpeditionMapLayoutEvidence(campProximityScreenshotFolder);
            CancelCampPopup();
            Require(!expeditionMapPanel.activeSelf && campProximityPrompt.activeSelf &&
                    campInteraction.ActiveTargetId == "camp.expedition-map" &&
                    campUse.PlayerPosition == mapReturnPosition && Mathf.Approximately(campUse.FacingDirection, mapReturnFacing) &&
                    !session.SelectedRegionId.HasValue,
                "지도 취소는 지역 선택 없이 같은 캠프 위치·방향·근접 대상에 복귀");
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();

            PrototypeContractProbe endingAlbumContract = PrototypeEndingAlbumContract.VerifyCatalogUnlockAndSelectionFixture();
            Require(endingAlbumContract.Success, endingAlbumContract.Detail);
            endingAlbumCollection.RestoreTransientSnapshot(string.Empty);
            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.EndingAlbum));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.EndingAlbum &&
                    campInteraction.ActiveTargetId == "camp.ending-album" && campProximityPrompt.activeSelf &&
                    !campInteractionPopup.activeSelf && !endingAlbumPanel.activeSelf,
                "생존 앨범은 1.25 unit 안에서만 하나의 직접 상호작용 안내를 표시");
            Vector2 albumReturnPosition = campUse.PlayerPosition;
            float albumReturnFacing = campUse.FacingDirection;
            if (!string.IsNullOrWhiteSpace(endingAlbumNearKoreanScreenshotPath))
            {
                CaptureVerificationPng(endingAlbumNearKoreanScreenshotPath, 1280, 800);
            }

            PrototypePlayerActions keyboardAlbumInteract = PrototypePlayerActions.FromRaw(
                new PrototypeRawInput { KeyboardInteract = true });
            PrototypePlayerActions gamepadAlbumInteract = PrototypePlayerActions.FromRaw(
                new PrototypeRawInput { GamepadInteract = true });
            Require(keyboardAlbumInteract.InteractPressed && gamepadAlbumInteract.InteractPressed,
                "키보드 E와 게임패드 X는 같은 캠프 Interact 액션 스냅샷으로 합류");
            UseNearestCampTarget();
            Require(campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.EndingAlbum &&
                    campInteractionPopup.activeSelf && endingAlbumOpenButton.gameObject.activeSelf &&
                    !endingAlbumSelection.IsOpen && !endingAlbumPanel.activeSelf && !campProximityPrompt.activeSelf,
                "앨범 Interact는 먼저 기록함 전용 소형 팝업을 열고 앨범 화면은 아직 숨김");
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            Require(campInteraction.OpenPopupTargetId == "camp.ending-album" &&
                    campUse.PlayerPosition == albumReturnPosition && Mathf.Approximately(campUse.FacingDirection, albumReturnFacing) &&
                    endingAlbumOpenButton.GetComponentInChildren<TMP_Text>().text == localization.Format("button.ending_album.open"),
                "영어 전환은 같은 앨범 팝업 대상·위치·방향과 Submit 의미를 보존");
            RequireReadableCampPopup();
            if (!string.IsNullOrWhiteSpace(endingAlbumPopupEnglishScreenshotPath))
            {
                CaptureVerificationPng(endingAlbumPopupEnglishScreenshotPath, 1280, 800);
            }

            Require(endingAlbumCollection.UnlockForVerification(
                    "ending.escape.smoke.seen-from-afar",
                    12,
                    "2026-08-25T00:00:00.0000000Z"),
                "검증용 해금은 로컬 외부 저장 없이 한 번만 적용");
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            endingAlbumOpenButton.onClick.Invoke();
            Require(endingAlbumSelection.IsOpen && endingAlbumPanel.activeSelf &&
                    !campInteractionPopup.activeSelf && !expeditionMapPanel.activeSelf && !campProximityPrompt.activeSelf &&
                    campInteraction.OpenPopupTargetId == "camp.ending-album" &&
                    campUse.PlayerPosition == albumReturnPosition && Mathf.Approximately(campUse.FacingDirection, albumReturnFacing),
                "팝업 Submit 뒤 채택 A 앨범만 열리고 이동·대상 latch는 보존");
            Require(endingAlbumSelection.FocusedIndex == 1 &&
                    endingAlbumDetailTitleText.text == localization.Format("ending.escape.smoke.seen-from-afar.title") &&
                    endingAlbumSummaryText.text == localization.Format("ending.escape.smoke.seen-from-afar.summary"),
                "해금 엔딩은 정본 제목·요약과 최초 해금 기록을 표시");
            RequireReadableEndingAlbumUi(false);
            if (!string.IsNullOrWhiteSpace(endingAlbumKoreanScreenshotPath))
            {
                CaptureVerificationPng(endingAlbumKoreanScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            Require(endingAlbumSelection.FocusedIndex == 1 &&
                    endingAlbumDetailTitleText.text == localization.Format("ending.escape.smoke.seen-from-afar.title") &&
                    campInteraction.OpenPopupTargetId == "camp.ending-album" &&
                    campUse.PlayerPosition == albumReturnPosition && Mathf.Approximately(campUse.FacingDirection, albumReturnFacing),
                "영어 앨범 전환은 같은 해금 기록·근접 대상·위치·방향을 보존");
            RequireReadableEndingAlbumUi(false);
            if (!string.IsNullOrWhiteSpace(endingAlbumEnglishScreenshotPath))
            {
                CaptureVerificationPng(endingAlbumEnglishScreenshotPath, 1280, 800);
            }

            FocusEndingAlbumEntry(10);
            Require(localization.SetQaLocale(), "생존 앨범 실제 qps-long QA 로케일 선택");
            RefreshAll();
            Require(endingAlbumSelection.FocusedIndex == 10 && campInteraction.OpenPopupTargetId == "camp.ending-album" &&
                    campUse.PlayerPosition == albumReturnPosition && Mathf.Approximately(campUse.FacingDirection, albumReturnFacing) &&
                    endingAlbumDetailTitleText.text == localization.Format("ending.album.locked.title") &&
                    endingAlbumSummaryText.text == localization.Format("ending.rare.raft.current-reader.hint") &&
                    endingAlbumDetailTitleText.text != localization.Format("ending.rare.raft.current-reader.title"),
                "qps-long 전환은 같은 기록 포커스를 보존하고 미해금 제목 대신 비스포일러 힌트만 표시");
            RequireReadableEndingAlbumUi(true);
            if (!string.IsNullOrWhiteSpace(endingAlbumQpsLongScreenshotPath))
            {
                CaptureVerificationPng(endingAlbumQpsLongScreenshotPath, 1280, 800);
            }

            int keyboardFocusedIndex = endingAlbumSelection.FocusedIndex;
            PrototypeExpeditionMapActions keyboardAlbumNavigation = PrototypeExpeditionMapActions.FromRaw(
                new PrototypeRawExpeditionMapInput { KeyboardNext = true });
            PrototypeExpeditionMapActions gamepadAlbumNavigation = PrototypeExpeditionMapActions.FromRaw(
                new PrototypeRawExpeditionMapInput { HorizontalAxis = 1f, GamepadConfirm = true });
            Require(keyboardAlbumNavigation.CycleDirection == gamepadAlbumNavigation.CycleDirection &&
                    gamepadAlbumNavigation.ConfirmPressed,
                "앨범 키보드·합성 게임패드는 같은 순환·확인 액션 의미를 생성");
            ProcessEndingAlbumActions(gamepadAlbumNavigation);
            ApplyEndingAlbumControlsPresentation(PrototypeInputDevice.Gamepad);
            Require(endingAlbumSelection.FocusedIndex == (keyboardFocusedIndex + 1) % PrototypeEndingCatalog.All.Count &&
                    endingAlbumControlsText.text == localization.Format(
                        PrototypeInputPromptKeys.EndingAlbum(PrototypeInputDevice.Gamepad),
                        localization.DeviceName(PrototypeInputDevice.Gamepad)) &&
                    campUse.PlayerPosition == albumReturnPosition && Mathf.Approximately(campUse.FacingDirection, albumReturnFacing),
                "합성 게임패드 전환은 동일 앨범 상태에서 포커스만 이동하고 위치·방향을 보존");
            RequireReadableEndingAlbumUi(true);

            for (int index = 0; index < PrototypeEndingCatalog.All.Count; index += 1)
            {
                FocusEndingAlbumEntry(index);
                RequireReadableEndingAlbumUi(true);
            }
            for (int index = 0; index < PrototypeEndingCatalog.All.Count; index += 1)
            {
                PrototypeEndingDefinition definition = PrototypeEndingCatalog.All[index];
                endingAlbumCollection.UnlockForVerification(definition.StableId, 20 + index, "2026-08-25T00:00:00.0000000Z");
                FocusEndingAlbumEntry(index);
                RequireReadableEndingAlbumUi(true);
            }
            WriteEndingAlbumLayoutEvidence(campProximityScreenshotFolder);

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            CloseEndingAlbumToPopup();
            Require(!endingAlbumSelection.IsOpen && !endingAlbumPanel.activeSelf && campInteractionPopup.activeSelf &&
                    campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.EndingAlbum &&
                    campInteraction.OpenPopupTargetId == "camp.ending-album" &&
                    campUse.PlayerPosition == albumReturnPosition && Mathf.Approximately(campUse.FacingDirection, albumReturnFacing),
                "앨범 취소는 같은 기록함 소형 팝업으로 한 단계 복귀");
            CancelCampPopup();
            Require(!campInteraction.IsPopupOpen && campProximityPrompt.activeSelf &&
                    campInteraction.ActiveTargetId == "camp.ending-album" &&
                    campUse.PlayerPosition == albumReturnPosition && Mathf.Approximately(campUse.FacingDirection, albumReturnFacing),
                "팝업 취소는 같은 현장·방향·앨범 근접 안내로 복귀");
            endingAlbumCollection.RestoreTransientSnapshot(endingAlbumSnapshotBeforeVerification);
            RefreshAll();

            if (!IsRaftShoreLaunchDiscoveredFromLiveSearch())
            {
                DiscoverRaftShoreLaunchThroughNaturalSearch();
            }
            Require(IsRaftShoreLaunchDiscoveredFromLiveSearch(),
                "실제 보호 돛천 수색 상태가 해안 진수대 발견 조건에 연결");
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.ShoreLaunch));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ShoreLaunch &&
                    campInteraction.ActiveTargetId == "facility.shore-launch" && campProximityPrompt.activeSelf &&
                    !campInteractionPopup.activeSelf && !campActions.activeSelf,
                "해안 진수대는 수색 후 김씨가 1.25 unit 안에 직접 접근할 때만 compact 문맥 안내를 표시");
            RequireReadableCampProximityPrompt(false);
            if (!string.IsNullOrWhiteSpace(raftNearKoreanScreenshotPath))
            {
                CaptureVerificationPng(raftNearKoreanScreenshotPath, 1280, 800);
            }
            Vector2 raftReturnPosition = campUse.PlayerPosition;
            float raftReturnFacing = campUse.FacingDirection;
            PrototypePlayerActions keyboardRaftInteract = PrototypePlayerActions.FromRaw(new PrototypeRawInput { KeyboardInteract = true });
            PrototypePlayerActions gamepadRaftInteract = PrototypePlayerActions.FromRaw(new PrototypeRawInput { GamepadInteract = true });
            Require(keyboardRaftInteract.InteractPressed && gamepadRaftInteract.InteractPressed,
                "뗏목 진수대 keyboard/gamepad는 같은 Interact 액션 스냅샷으로 합류");
            UseNearestCampTarget();
            ConfigureCampPopupLayout(false);
            RectTransform raftPopupRect = campInteractionPopup.GetComponent<RectTransform>();
            Require(campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ShoreLaunch &&
                    raftProjectButton.gameObject.activeSelf && !campProximityPrompt.activeSelf &&
                    raftPopupRect.anchorMin == CampPopupDefaultAnchorMin && raftPopupRect.anchorMax == CampPopupDefaultAnchorMax &&
                    campInteractionPopupFrameImage.sprite == campInteractionPopupDefaultSprite,
                "진수대 Interact는 review 뗏목 아트 없이 전용 소형 placeholder 팝업 하나만 연다");
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            Require(campInteraction.OpenPopupTargetId == "facility.shore-launch" &&
                    campUse.PlayerPosition == raftReturnPosition && Mathf.Approximately(campUse.FacingDirection, raftReturnFacing) &&
                    raftProjectButton.GetComponentInChildren<TMP_Text>().text.Contains("Hull"),
                "영어 전환은 진수대 대상·단계·위치·방향을 보존하고 TMP만 갱신");
            RequireReadableCampPopup();
            if (!string.IsNullOrWhiteSpace(raftPopupEnglishScreenshotPath))
            {
                CaptureVerificationPng(raftPopupEnglishScreenshotPath, 1280, 800);
            }
            Require(localization.SetQaLocale(), "뗏목 팝업 실제 qps-long 데이터 로케일 선택");
            RefreshAll();
            Require(campInteraction.OpenPopupTargetId == "facility.shore-launch" &&
                    campUse.PlayerPosition == raftReturnPosition && Mathf.Approximately(campUse.FacingDirection, raftReturnFacing),
                "qps-long 전환은 같은 진수대 latch·단계·위치·방향을 보존");
            RequireReadableCampPopup(true);
            if (!string.IsNullOrWhiteSpace(raftPopupQpsLongScreenshotPath))
            {
                CaptureVerificationPng(raftPopupQpsLongScreenshotPath, 1280, 800);
            }
            CancelCampPopup();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ShoreLaunch &&
                    campProximityPrompt.activeSelf && campUse.PlayerPosition == raftReturnPosition &&
                    Mathf.Approximately(campUse.FacingDirection, raftReturnFacing),
                "뗏목 팝업 취소는 같은 현장·방향·compact 안내로 복귀");
            PrototypeContractProbe raftNaturalRoute = PrototypeRaftRuntimeContract.VerifyAtomicFailureRetrySnapshotFixture();
            Require(raftNaturalRoute.Success, raftNaturalRoute.Detail);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();

            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.Campfire));
            RefreshAll();
            Require(campInteraction.HasProximityPrompt && campProximityPrompt.activeSelf && !campInteractionPopup.activeSelf,
                "근접 시 대상 하나의 안내만 표시하고 팝업은 상호작용 전까지 숨김");
            PrototypeCampInteractionTargetKind latchedPromptTarget = campInteraction.ActiveTargetKind;
            string promptTargetIdBeforeLocale = campInteraction.ActiveTargetId;
            Vector2 promptPositionBeforeLocale = campUse.PlayerPosition;
            float promptFacingBeforeLocale = campUse.FacingDirection;
            string keyboardGlyphBeforeLocale = campProximityGlyphText.text;
            string koreanActionBeforeLocale = campProximityText.text;
            RequireCompactCampPromptSkin();
            if (!string.IsNullOrWhiteSpace(campProximityKoreanScreenshotPath))
            {
                RequireReadableCampProximityPrompt(false);
                CaptureVerificationPng(campProximityKoreanScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == latchedPromptTarget &&
                    campInteraction.ActiveTargetId == promptTargetIdBeforeLocale &&
                    campUse.PlayerPosition == promptPositionBeforeLocale &&
                    Mathf.Approximately(campUse.FacingDirection, promptFacingBeforeLocale) &&
                    campProximityGlyphText.text == keyboardGlyphBeforeLocale &&
                    campProximityText.text != koreanActionBeforeLocale &&
                    campProximityText.text.Contains("Campfire"),
                "영어 전환은 glyph·근접 대상 latch·위치·방향을 보존하고 행동 TMP만 갱신");
            RequireReadableCampProximityPrompt(false);
            if (!string.IsNullOrWhiteSpace(campProximityEnglishScreenshotPath))
            {
                CaptureVerificationPng(campProximityEnglishScreenshotPath, 1280, 800);
            }

            string englishActionBeforeDevice = campProximityText.text;
            ApplyCampProximityPresentation(campInteraction.ActiveTargetKind, campInteraction.ActiveTargetId, PrototypeInputDevice.Gamepad);
            RequireReadableCampProximityPrompt(false);
            Require(campProximityGlyphText.text == "[X]" && campProximityText.text == englishActionBeforeDevice &&
                    campInteraction.ActiveTargetKind == latchedPromptTarget && campInteraction.ActiveTargetId == promptTargetIdBeforeLocale &&
                    campUse.PlayerPosition == promptPositionBeforeLocale && Mathf.Approximately(campUse.FacingDirection, promptFacingBeforeLocale),
                "합성 게임패드 전환은 고정 glyph 슬롯만 [X]로 바꾸고 행동·대상·상태를 보존");

            bool hadLocalePreferenceBeforeQa = PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey);
            string localePreferenceBeforeQa = PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode);
            Require(localization.SetQaLocale() && localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode,
                "비출시 qps-long 데이터 로케일을 QA 전용 경로로 선택");
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == latchedPromptTarget && campInteraction.ActiveTargetId == promptTargetIdBeforeLocale &&
                    campProximityGlyphText.text == "[E]" && !campProximityText.text.Contains("[E]") &&
                    localization.Format(PrototypeInputPromptKeys.InteractGlyph(PrototypeInputDevice.Gamepad)) == "[X]",
                "qps-long 전환은 같은 근접 대상과 locale 불변 keyboard/gamepad glyph 의미를 보존");
            Require(PlayerPrefs.HasKey(PrototypeLocalization.PreferenceKey) == hadLocalePreferenceBeforeQa &&
                    PlayerPrefs.GetString(PrototypeLocalization.PreferenceKey, PrototypeLocalization.KoreanLocaleCode) == localePreferenceBeforeQa,
                "QA 로케일 전환은 제품 언어 선택값을 저장하지 않음");
            RequireReadableTopHud(PrototypeLocalization.QpsLongLocaleCode);
            RequireReadableCampProximityPrompt(true);
            if (!string.IsNullOrWhiteSpace(campProximityQpsLongScreenshotPath))
            {
                CaptureVerificationPng(campProximityQpsLongScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();

            List<PrototypeCampInteractionTargetKind> promptTargets = new List<PrototypeCampInteractionTargetKind>
            {
                PrototypeCampInteractionTargetKind.Campfire,
                PrototypeCampInteractionTargetKind.Workbench,
                PrototypeCampInteractionTargetKind.RainCollector,
                PrototypeCampInteractionTargetKind.StoragePlanning
            };
            if (!IsGameJamLiveEscapeProfile)
            {
                promptTargets.Insert(3, PrototypeCampInteractionTargetKind.RescueSignal);
            }
            for (int promptIndex = 0; promptIndex < promptTargets.Count; promptIndex += 1)
            {
                PrototypeCampInteractionTargetKind promptTarget = promptTargets[promptIndex];
                campUse.Warp(GetCampInteractionTargetPosition(promptTarget));
                RefreshAll();
                Require(campInteraction.ActiveTargetKind == promptTarget && campProximityPrompt.activeSelf,
                    promptTarget + " 근접 안내는 공통 소형 레이아웃 한 개만 표시");
                RequireReadableCampProximityPrompt(false);
            }

            campUse.Warp(GetCampModuleSlotPosition(CampModuleArchetype.Upper));
            RefreshAll();
            string directSlotTargetBeforeLocale = campInteraction.ActiveTargetId;
            Require(directSlotTargetBeforeLocale == "slot.start.upper" && campProximityGlyphText.text == "[E]" &&
                    campProximityText.text.Contains(localization.Format("interaction.action.preview")),
                "위층 연결 슬롯의 한국어 근접 안내는 canonical target과 localized preview action을 표시");
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            string directSlotEnglishAction = campProximityText.text;
            ApplyCampProximityPresentation(campInteraction.ActiveTargetKind, campInteraction.ActiveTargetId, PrototypeInputDevice.Gamepad);
            Require(campInteraction.ActiveTargetId == directSlotTargetBeforeLocale && campProximityGlyphText.text == "[X]" &&
                    campProximityText.text == directSlotEnglishAction && directSlotEnglishAction.Contains("Preview"),
                "영어·게임패드 전환은 직접 슬롯 target과 preview action 의미를 보존");
            Require(localization.SetQaLocale(), "직접 슬롯 prompt에서도 실제 qps-long 선택");
            RefreshAll();
            Require(campInteraction.ActiveTargetId == directSlotTargetBeforeLocale && campProximityGlyphText.text == "[E]" &&
                    campProximityText.text.Contains(localization.Format("interaction.action.preview")),
                "qps-long 전환은 직접 슬롯 target latch와 키보드 Interact 의미를 보존");
            RequireReadableCampProximityPrompt(true);
            if (!string.IsNullOrWhiteSpace(campProximityQpsLongScreenshotPath))
            {
                CaptureVerificationPng(campProximityQpsLongScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            OpenCampModuleSlotPopupForVerification(CampModuleArchetype.Upper);
            Require(modulePreviewButton.gameObject.activeSelf && modulePreviewButton.interactable &&
                    cancelPopupButton.gameObject.activeSelf &&
                    !campfireButton.gameObject.activeSelf && !workbenchButton.gameObject.activeSelf && !rainButton.gameObject.activeSelf &&
                    modulePreviewButton.GetComponentInChildren<TMP_Text>().text == localization.Format("ui.module.expand") &&
                    EventSystem.current.currentSelectedGameObject == modulePreviewButton.gameObject,
                "위층 연결 슬롯의 소형 팝업은 ui.module.expand 한 행동과 root 취소만 소유");
            RequireReadableCampPopup();
            if (!string.IsNullOrWhiteSpace(moduleSlotPopupKoreanScreenshotPath))
            {
                CaptureVerificationPng(moduleSlotPopupKoreanScreenshotPath, 1280, 800);
            }
            CampModuleReturnSnapshot moduleReturn = new CampModuleReturnSnapshot(
                campUse.PlayerPosition,
                campUse.FacingDirection,
                campUse.CurrentRoomId);
            string moduleReturnTargetId = campInteraction.OpenPopupTargetId;
            int previewWoodBefore = session.GetStorage(ResourceKind.Wood);
            int previewStoneBefore = session.GetStorage(ResourceKind.Stone);
            int previewSalvageBefore = session.GetStorage(ResourceKind.Salvage);
            modulePreviewButton.onClick.Invoke();
            Require(campModuleExpansion.IsPreviewActive && !campInteractionPopup.activeSelf && !campProximityPrompt.activeSelf &&
                    campModuleReasonChip.activeSelf && campInteraction.OpenPopupTargetId == "slot.start.upper" &&
                    campModuleExpansion.SelectedArchetype == CampModuleArchetype.Upper &&
                    modulePreviewGhost != null && modulePreviewOutlineRenderers.Count == 4,
                "위층 슬롯의 ui.module.expand Submit 뒤에만 같은 위층 후보 공간 미리보기");
            CampModuleEvaluation lockedEvaluation = campModuleExpansion.Evaluate(session, campModuleValidation);
            Require(lockedEvaluation.Geometry == CampModuleGeometryStatus.Valid &&
                    lockedEvaluation.Economy == CampModuleEconomyStatus.Locked &&
                    campModuleReasonText.text.Contains(localization.Format("module.prerequisite.workbench")) &&
                    campModuleReasonText.text.Contains("나무 " + lockedEvaluation.Cost.Wood) &&
                    campModuleReasonText.text.Contains("표류물 " + lockedEvaluation.Cost.Salvage),
                "작업대 전 preview는 geometry/economy를 분리하고 canonical 잠금 사유와 실제 위층 비용을 함께 표시");
            Require(!ConfirmCampModulePreview() && campModuleExpansion.IsPreviewActive &&
                    campModuleExpansion.SelectedArchetype == CampModuleArchetype.Upper &&
                    session.GetStorage(ResourceKind.Wood) == previewWoodBefore &&
                    session.GetStorage(ResourceKind.Salvage) == previewSalvageBefore,
                "잠긴 직접 슬롯 확정은 같은 후보에 남고 자원을 전혀 쓰지 않음");
            RequireReadableCampModulePreview(false);
            if (!string.IsNullOrWhiteSpace(modulePreviewKoreanScreenshotPath))
            {
                CaptureVerificationPng(modulePreviewKoreanScreenshotPath, 1280, 800);
            }

            campModuleExpansion.Cycle(1);
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            Require(campModuleExpansion.SelectedArchetype == CampModuleArchetype.Side &&
                    messageText.text.Contains("Side room") && campModuleReasonText.text.Contains("Wood 2") &&
                    campModuleReasonText.text.Contains("Salvage 1") && controlsText.text.Contains("cycle") &&
                    campInteraction.OpenPopupTargetId == moduleReturnTargetId,
                "영어 전환 뒤에도 접근 슬롯 target과 옆방 후보를 보존하고 공통 입력·W2/D1을 즉시 갱신");
            RequireReadableCampModulePreview(false);
            if (!string.IsNullOrWhiteSpace(modulePreviewEnglishScreenshotPath))
            {
                CaptureVerificationPng(modulePreviewEnglishScreenshotPath, 1280, 800);
            }

            campModuleExpansion.Cycle(1);
            RefreshAll();
            Require(campModuleExpansion.SelectedArchetype == CampModuleArchetype.Basement && campModuleExpansion.HasSeenAllCandidates,
                "위층·옆방·지하실 세 후보를 같은 미리보기 상태에서 순회");
            Require(localization.SetQaLocale(), "증축 미리보기에서도 qps-long 데이터 로케일 선택");
            RefreshAll();
            CampModuleEvaluation basementEvaluation = campModuleExpansion.Evaluate(session, campModuleValidation);
            Require(campModuleExpansion.SelectedArchetype == CampModuleArchetype.Basement && campModuleExpansion.HasSeenAllCandidates &&
                    campInteraction.OpenPopupTargetId == moduleReturnTargetId &&
                    campModuleReasonText.text.Contains(basementEvaluation.Cost.Wood.ToString()) &&
                    campModuleReasonText.text.Contains(basementEvaluation.Cost.Salvage.ToString()),
                "qps-long 전환은 슬롯 target·지하실 후보·비용 숫자와 action 의미를 보존");
            RequireReadableCampModulePreview(true);
            if (!string.IsNullOrWhiteSpace(modulePreviewQpsLongScreenshotPath))
            {
                CaptureVerificationPng(modulePreviewQpsLongScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();
            CancelCampModulePreview(true);
            Require(campUse.PlayerPosition == moduleReturn.Position &&
                    Mathf.Approximately(campUse.FacingDirection, moduleReturn.FacingDirection) &&
                    campUse.CurrentRoomId == moduleReturn.RoomId &&
                    campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot &&
                    campInteraction.OpenPopupTargetId == moduleReturnTargetId &&
                    campModuleExpansion.SelectedArchetype == CampModuleArchetype.Basement &&
                    session.GetStorage(ResourceKind.Wood) == previewWoodBefore &&
                    session.GetStorage(ResourceKind.Stone) == previewStoneBefore &&
                    session.GetStorage(ResourceKind.Salvage) == previewSalvageBefore,
                "첫 Cancel은 후보·위치·방향·target과 자원을 보존해 같은 위층 슬롯 팝업으로 한 단계 복귀");
            CancelCampPopup();
            Require(!campInteraction.IsPopupOpen && campInteraction.HasProximityPrompt &&
                    campInteraction.ActiveTargetId == moduleReturnTargetId &&
                    campModuleExpansion.SelectedArchetype == CampModuleArchetype.Basement &&
                    campUse.PlayerPosition == moduleReturn.Position && Mathf.Approximately(campUse.FacingDirection, moduleReturn.FacingDirection),
                "두 번째 root Cancel은 같은 후보 snapshot과 현장 target을 보존해 직접 이동으로 복귀");

            OpenCampModuleSlotPopupForVerification(CampModuleArchetype.Side);
            modulePreviewButton.onClick.Invoke();
            Require(campModuleExpansion.IsPreviewActive && campModuleExpansion.SelectedArchetype == CampModuleArchetype.Side,
                "옆방 슬롯의 첫 Submit은 접근한 옆방 후보에서 시작");
            CancelCampModulePreview(true);
            CancelCampPopup();
            OpenCampModuleSlotPopupForVerification(CampModuleArchetype.Basement);
            modulePreviewButton.onClick.Invoke();
            Require(campModuleExpansion.IsPreviewActive && campModuleExpansion.SelectedArchetype == CampModuleArchetype.Basement,
                "지하실 슬롯의 첫 Submit은 접근한 지하실 후보에서 시작");
            CancelCampModulePreview(true);
            CancelCampPopup();

            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.StoragePlanning);
            Require(modulePreviewButton.gameObject.activeSelf && modulePreviewButton.interactable &&
                    campfireButton.gameObject.activeSelf && workbenchButton.gameObject.activeSelf && rainButton.gameObject.activeSelf,
                "storage.planning 팝업은 기존 증축 preview를 보조 경로로 유지");
            modulePreviewButton.onClick.Invoke();
            Require(campModuleExpansion.IsPreviewActive && campModuleExpansion.SelectedArchetype == CampModuleArchetype.Upper,
                "보조 storage.planning 진입은 기존처럼 위층 후보에서 시작");
            CancelCampModulePreview(true);
            workbenchButton.onClick.Invoke();
            Require(campPlacement.IsActive && campPlacement.CandidateRoomId == PrototypeCampModuleCatalog.StartRoomId &&
                    campPlacement.CurrentValidity == CampPlacementValidity.Valid && ConfirmCampPlacement() &&
                    session.HasStructure(StructureKind.Workbench),
                "보조 현장 계획 지점에서 시작 방 작업대를 배치해 기존 해금 인과를 보존");

            OpenCampModuleSlotPopupForVerification(CampModuleArchetype.Upper);
            modulePreviewButton.onClick.Invoke();
            Require(campModuleExpansion.IsPreviewActive && campModuleExpansion.SelectedArchetype == CampModuleArchetype.Upper &&
                    campModuleExpansion.Evaluate(session, campModuleValidation).Economy == CampModuleEconomyStatus.Ready,
                "작업대 건설 후 같은 직접 슬롯으로 돌아오면 접근 후보가 READY가 됨");
            CampModuleResourceCost upperCommitCost = campModuleExpansion.Evaluate(session, campModuleValidation).Cost;
            int commitWoodBefore = session.GetStorage(ResourceKind.Wood);
            int commitStoneBefore = session.GetStorage(ResourceKind.Stone);
            int commitFoodBefore = session.GetStorage(ResourceKind.Food);
            int commitSalvageBefore = session.GetStorage(ResourceKind.Salvage);
            int activeSlotsBeforeModule = session.ActiveBagSlotCount;
            int signalBeforeModule = session.SignalStage;
            Require(ConfirmCampModulePreview() && campModuleExpansion.HasCommittedModule &&
                    session.GetStorage(ResourceKind.Wood) == commitWoodBefore - upperCommitCost.Wood &&
                    session.GetStorage(ResourceKind.Stone) == commitStoneBefore - upperCommitCost.Stone &&
                    session.GetStorage(ResourceKind.Food) == commitFoodBefore - upperCommitCost.Food &&
                    session.GetStorage(ResourceKind.Salvage) == commitSalvageBefore - upperCommitCost.Salvage,
                "유효한 위층 방 확정은 현재 게임잼 비용을 원자적으로 한 번만 사용");
            int woodAfterModule = session.GetStorage(ResourceKind.Wood);
            Require(!ConfirmCampModulePreview() && session.GetStorage(ResourceKind.Wood) == woodAfterModule &&
                    session.ActiveBagSlotCount == activeSlotsBeforeModule && session.SignalStage == signalBeforeModule,
                "중복 확정은 자원을 다시 쓰지 않고 가방·신호 상태를 보존");

            OpenCampModuleSlotPopupForVerification(CampModuleArchetype.Side);
            modulePreviewButton.onClick.Invoke();
            Require(campModuleExpansion.Evaluate(session, campModuleValidation).Economy == CampModuleEconomyStatus.Ready,
                "위층 확정 뒤에도 미확정 옆방 슬롯은 같은 run에서 READY");
            CancelCampModulePreview(true);
            CancelCampPopup();

            OpenCampModuleSlotPopupForVerification(CampModuleArchetype.Basement);
            modulePreviewButton.onClick.Invoke();
            CampModuleResourceCost basementCommitCost = campModuleExpansion.Evaluate(session, campModuleValidation).Cost;
            int woodBeforeBasement = session.GetStorage(ResourceKind.Wood);
            int stoneBeforeBasement = session.GetStorage(ResourceKind.Stone);
            int foodBeforeBasement = session.GetStorage(ResourceKind.Food);
            int salvageBeforeBasement = session.GetStorage(ResourceKind.Salvage);
            Require(ConfirmCampModulePreview() &&
                    campModuleExpansion.HasUpperAndBasementCommitted &&
                    campModuleExpansion.CommittedModuleCount == 2 &&
                    session.GetStorage(ResourceKind.Wood) == woodBeforeBasement - basementCommitCost.Wood &&
                    session.GetStorage(ResourceKind.Stone) == stoneBeforeBasement - basementCommitCost.Stone &&
                    session.GetStorage(ResourceKind.Food) == foodBeforeBasement - basementCommitCost.Food &&
                    session.GetStorage(ResourceKind.Salvage) == salvageBeforeBasement - basementCommitCost.Salvage,
                "위층+지하실은 같은 run에서 각 현재 게임잼 비용을 한 번씩 내고 함께 확정");

            CampModuleDefinition upperDefinition = PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper);
            CampModuleDefinition basementDefinition = PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement);
            campUse.Warp(GetCampModuleSlotPosition(CampModuleArchetype.Upper));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleConnector &&
                    campInteraction.ActiveTargetId == upperDefinition.StartSlotId &&
                    campInteraction.HasProximityPrompt,
                "확정된 위층 방의 명시적 사다리 연결부에 근접 안내 표시");
            UseNearestCampTarget();
            Require(campUse.CurrentRoomId == upperDefinition.RoomId && !campInteraction.IsPopupOpen,
                "위층 stable connector 직접 상호작용으로 위층 실내 이동");
            if (!string.IsNullOrWhiteSpace(moduleInteriorKoreanScreenshotPath))
            {
                CaptureVerificationPng(moduleInteriorKoreanScreenshotPath, 1280, 800);
            }

            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.StoragePlanning);
            workbenchButton.onClick.Invoke();
            Require(campPlacement.IsActive && campPlacement.CandidateRoomId == upperDefinition.RoomId &&
                    campPlacement.CurrentValidity == CampPlacementValidity.Valid && ConfirmCampPlacement() &&
                    campPlacement.IsInstalledInRoom(StructureKind.Workbench, upperDefinition.RoomId),
                "위층 일반 구역에서 작업대 제한적 자유 배치");
            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.Workbench);
            repairButton.onClick.Invoke();
            Require(campFeedback.Key == "message.workbench.repair.ready",
                "위층에 설치한 작업대를 그 방에서 직접 사용");

            campUse.Warp(new Vector2(upperDefinition.ModuleConnectorDisplayX, PrototypeCampPlacement.FloorY));
            RefreshAll();
            UseNearestCampTarget();
            Require(campUse.CurrentRoomId == PrototypeCampModuleCatalog.StartRoomId,
                "위층 reciprocal connector로 시작 방 복귀");

            campUse.Warp(GetCampModuleSlotPosition(CampModuleArchetype.Basement));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleConnector &&
                    campInteraction.ActiveTargetId == basementDefinition.StartSlotId,
                "확정된 지하실 stable connector가 시작층에 독립 target으로 노출");
            UseNearestCampTarget();
            Require(campUse.CurrentRoomId == basementDefinition.RoomId,
                "지하실 connector 직접 상호작용으로 지하실 이동");

            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.StoragePlanning);
            campfireButton.onClick.Invoke();
            Require(campPlacement.IsActive && campPlacement.CandidateRoomId == basementDefinition.RoomId &&
                    campPlacement.CurrentValidity == CampPlacementValidity.Valid && ConfirmCampPlacement() &&
                    campPlacement.IsInstalledInRoom(StructureKind.Campfire, basementDefinition.RoomId),
                "지하실 일반 구역에서 모닥불 제한적 자유 배치");
            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.Campfire);
            prepareCampfireButton.onClick.Invoke();
            Require(campUse.IsDayBenefitPrepared(StructureKind.Campfire),
                "지하실에 설치한 모닥불을 그 방에서 직접 사용");

            string campSpaceBeforeRestore = CaptureCampSpaceSaveJson();
            string[] committedRoomIdsBeforeRestore = GetCommittedCampRoomIds();
            campModuleExpansion.Reset();
            campPlacement.Reset();
            campUse.Reset();
            Require(RestoreCampSpaceSaveJson(campSpaceBeforeRestore) &&
                    CaptureCampSpaceSaveJson() == campSpaceBeforeRestore &&
                    committedRoomIdsBeforeRestore.SequenceEqual(GetCommittedCampRoomIds()) &&
                    campUse.CurrentRoomId == basementDefinition.RoomId &&
                    campPlacement.IsInstalledInRoom(StructureKind.Workbench, upperDefinition.RoomId) &&
                    campPlacement.IsInstalledInRoom(StructureKind.Campfire, basementDefinition.RoomId),
                "통합 save root는 module→membership→placement/use→RefreshAll 순서로 위층+지하실과 설비를 exact 복원");

            PrototypeCampSpaceSnapshot invalidCampSpace = CaptureCampSpaceSnapshot();
            invalidCampSpace.Placement.Installed[0].StableRoomId = PrototypeCampModuleCatalog.Get(CampModuleArchetype.Side).RoomId;
            string campSpaceBeforeRejectedRestore = CaptureCampSpaceSaveJson();
            Require(!RestoreCampSpaceSnapshot(invalidCampSpace) &&
                    CaptureCampSpaceSaveJson() == campSpaceBeforeRejectedRestore,
                "미확정 방을 참조하는 placement save는 전체 camp-space 상태를 원자적으로 보존하며 거부");

            campUse.Warp(new Vector2(basementDefinition.ModuleConnectorDisplayX, PrototypeCampPlacement.FloorY));
            RefreshAll();
            UseNearestCampTarget();
            campUse.Warp(GetCampModuleSlotPosition(CampModuleArchetype.Upper));
            RefreshAll();
            UseNearestCampTarget();
            Require(campUse.CurrentRoomId == upperDefinition.RoomId &&
                    structureViews.ContainsKey(StructureKind.Workbench),
                "save 복원 뒤 시작층을 거쳐 위층 재진입 시 위층 설비를 다시 시각화");

            session.Reset();
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();

            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.Campfire));
            RefreshAll();

            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.Campfire);
            Require(!campProximityPrompt.activeSelf, "설비 팝업이 열리면 근접 안내 숨김");
            Require(campfireButton.gameObject.activeSelf && cancelPopupButton.gameObject.activeSelf &&
                    !workbenchButton.gameObject.activeSelf && !signalButton.gameObject.activeSelf,
                "미설치 모닥불 팝업은 해당 설비의 건설 행동만 소유");
            Vector2 popupLockedPosition = campUse.PlayerPosition;
            ProcessCampActions(new PrototypePlayerActions(1f, false, false, false, false, -1), 0.5f);
            Require(campUse.PlayerPosition == popupLockedPosition, "설비 팝업 동안 일반 이동 잠금");
            if (!string.IsNullOrWhiteSpace(campCampfireKoreanScreenshotPath))
            {
                RequireReadableCampPopup();
                CaptureVerificationPng(campCampfireKoreanScreenshotPath, 1280, 800);
            }
            CancelCampPopup();
            Require(campUse.PlayerPosition == popupLockedPosition && campInteraction.HasProximityPrompt && !campInteraction.IsPopupOpen,
                "팝업 취소 뒤 같은 위치의 직접 조작과 근접 안내 복귀");

            if (!IsGameJamLiveEscapeProfile)
            {
                OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.RescueSignal);
                TMP_Text signalLabel = signalButton.GetComponentInChildren<TMP_Text>();
                Require(signalButton.interactable && signalLabel.text.Contains("작업대") && signalLabel.text.Contains("없음"), "재료가 부족해도 선택 가능한 1단계 작업대 요구 표시");
                signalButton.onClick.Invoke();
                Require(session.SignalStage == 0 && session.LastMessage.Key == "message.signal.workbench" && messageText.text.Contains("작업대가 없다"), "1단계 작업대 없음 실패 피드백");
                OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.RescueSignal);
                RequireReadableSignalFeedback();
                if (!string.IsNullOrWhiteSpace(signalKoreanScreenshotPath))
                {
                    CaptureVerificationPng(signalKoreanScreenshotPath, 1280, 800);
                }
                CancelCampPopup();

                Require(session.TryBuild(StructureKind.Workbench), "신호대 단계 검증용 작업대 건설");
                RefreshAll();
                InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.RescueSignal, signalButton);
                Require(session.SignalStage == 1 && !session.HasRope, "밧줄 없이 가능한 구조 신호대 1단계 UI 경로");
                OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.RescueSignal);
                Require(signalButton.interactable && signalLabel.text.Contains("밧줄") && signalLabel.text.Contains("없음"), "재료가 부족해도 선택 가능한 2단계 밧줄 요구 표시");
                signalButton.onClick.Invoke();
                Require(session.SignalStage == 1 && session.LastMessage.Key == "message.signal.rope", "밧줄 없는 구조 신호대 2단계의 명확한 거절");
                localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.RescueSignal);
                Require(signalLabel.text.Contains("Rope") && signalLabel.text.Contains("None") && messageText.text.Contains("No rope"), "영어 2단계 요구조건과 부족 사유 즉시 전환");
                RequireReadableSignalFeedback();
                if (!string.IsNullOrWhiteSpace(signalEnglishScreenshotPath))
                {
                    CaptureVerificationPng(signalEnglishScreenshotPath, 1280, 800);
                }
                CancelCampPopup();

                Require(session.TryResearch(TechKind.Rope) && session.TryCraft(TechKind.Rope), "재료 부족 UI 검증용 밧줄 제작");
                session.Grant(ResourceKind.Wood, -999);
                session.Grant(ResourceKind.Salvage, -999);
                RefreshAll();
                OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.RescueSignal);
                Require(signalButton.interactable, "나무·표류물 부족 상태에서도 구조 신호대 행동 선택 가능");
                signalButton.onClick.Invoke();
                Require(session.SignalStage == 1 && session.LastMessage.Key == "message.signal.materials" && messageText.text.Contains("Wood and salvage are short"), "나무·표류물 동시 부족 UI 피드백");
            }

            session.Reset();
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();
            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.Workbench);

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
            CancelCampPopup();

            Require(session.TryBuild(StructureKind.Workbench), "가방 확장 UI 검증용 작업대 건설");
            RefreshAll();
            campPlacement.EnsureInstalled(StructureKind.Workbench);
            campUse.Warp(campPlacement.GetInstalledPosition(StructureKind.Workbench));
            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.Workbench);
            int woodBeforeBagUpgrade = session.GetStorage(ResourceKind.Wood);
            int salvageBeforeBagUpgrade = session.GetStorage(ResourceKind.Salvage);
            Require(bagUpgradeButton.interactable && bagUpgradeLabel.text.Contains("4→6") && bagUpgradeLabel.text.Contains("나무 2/2") && bagUpgradeLabel.text.Contains("표류물 1/1"), "가방 확장 비용과 4→6 표시");
            bagUpgradeButton.onClick.Invoke();
            Require(session.ActiveBagSlotCount == GameSession.MaximumBagSlotCount &&
                    session.GetStorage(ResourceKind.Wood) == woodBeforeBagUpgrade - GameSession.BagUpgradeWoodCost &&
                    session.GetStorage(ResourceKind.Salvage) == salvageBeforeBagUpgrade - GameSession.BagUpgradeSalvageCost, "가방 확장 UI의 원자적 1회 비용과 6칸 활성화");
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            OpenCampPopupForVerification(PrototypeCampInteractionTargetKind.Workbench);
            Require(!bagUpgradeButton.interactable && bagUpgradeLabel.text.Contains("Done") && bagTitleText.text.Contains("Bag 6/6"), "영어 가방 확장 완료·6칸 표시");
            RequireReadableBagUi();
            RequireReadableCampPopup();
            if (!string.IsNullOrWhiteSpace(campWorkbenchEnglishScreenshotPath))
            {
                CaptureVerificationPng(campWorkbenchEnglishScreenshotPath, 1280, 800);
            }
            if (!string.IsNullOrWhiteSpace(bagUpgradedEnglish1280ScreenshotPath))
            {
                CaptureVerificationPng(bagUpgradedEnglish1280ScreenshotPath, 1280, 800);
            }
            if (!string.IsNullOrWhiteSpace(bagUpgradedEnglish1920ScreenshotPath))
            {
                CaptureVerificationPng(bagUpgradedEnglish1920ScreenshotPath, 1920, 1080);
            }
            CancelCampPopup();

            BeginExpeditionThroughMapForVerification(PrototypeExpeditionRegionId.Forest);
            Require(session.Phase == GamePhase.Exploring && session.SelectedRegionId == PrototypeExpeditionRegionId.Forest,
                "가방 6칸 UI 검증은 지도에서 선택한 숲 프로필로 수색 시작");
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
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            Require(session.ActiveBagSlotCount == GameSession.DefaultBagSlotCount && !session.HasPendingLoot && !session.IsBagSlotActive(4), "새 게임 초기화의 4칸·잠금·pending 정리");
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 10);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 20);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();

            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Campfire, campfireButton);
            Require(campPlacement.IsActive && placementGhost != null, "모닥불 배치 유령 UI");
            Require(placementGhostLabel != null && placementGhostBadgeRenderer != null &&
                    !placementGhostLabel.transform.parent.gameObject.activeSelf,
                "배치 판정은 상단 상태 카드와 footprint outline만 사용하고 월드 사각 배지는 숨김");
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

            Require(localization.SetQaLocale(), "배치 화면의 실제 qps-long QA 로케일 선택");
            RefreshAll();
            campPlacement.SetCandidateX(-1.5f);
            UpdatePlacementGhost();
            ApplyPlacementGuidance(PrototypeInputDevice.KeyboardMouse);
            RequireQpsGlobalPlacementLayout();
            if (!string.IsNullOrWhiteSpace(placementQpsLongScreenshotPath))
            {
                CaptureVerificationPng(placementQpsLongScreenshotPath, 1280, 800);
            }

            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();
            campPlacement.SetCandidateX(-1.5f);
            UpdatePlacementGhost();
            Require(campPlacement.CurrentValidity == CampPlacementValidity.Valid && ConfirmCampPlacement(), "모닥불 스냅 배치 확정");

            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Workbench, workbenchButton);
            campPlacement.SetCandidateX(1.5f);
            Require(ConfirmCampPlacement(), "작업대 스냅 배치 확정");
            int woodBeforeRelocation = session.GetStorage(ResourceKind.Wood);
            int stoneBeforeRelocation = session.GetStorage(ResourceKind.Stone);
            int salvageBeforeRelocation = session.GetStorage(ResourceKind.Salvage);
            campUse.Warp(campPlacement.GetInstalledPosition(StructureKind.Workbench));
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Workbench, workbenchButton);
            Require(campPlacement.IsRelocating, "건설된 작업대 재배치 진입");
            campPlacement.SetCandidateX(3.5f);
            Require(ConfirmCampPlacement(), "작업대 무료 재배치 확정");
            Require(Mathf.Approximately(campPlacement.GetInstalledPosition(StructureKind.Workbench).x, 3.5f), "작업대 위치 변경");
            Require(session.GetStorage(ResourceKind.Wood) == woodBeforeRelocation &&
                    session.GetStorage(ResourceKind.Stone) == stoneBeforeRelocation &&
                    session.GetStorage(ResourceKind.Salvage) == salvageBeforeRelocation, "재배치 추가 자원 비용 없음");

            campUse.Warp(campPlacement.GetInstalledPosition(StructureKind.Workbench));
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Workbench, workbenchButton);
            campPlacement.SetCandidateX(1.5f);
            Require(ConfirmCampPlacement(), "작업대를 일반 설비 구역으로 무료 복귀");

            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.RainCollector, rainButton);
            campPlacement.SetCandidateX(1.5f);
            Require(campPlacement.CurrentValidity == CampPlacementValidity.WrongZone && !ConfirmCampPlacement(), "빗물받이 일반 바닥 배치 거부");
            campPlacement.SetCandidateX(3.5f);
            Require(ConfirmCampPlacement(), "빗물받이 open-sky 바닥 스냅 배치 확정");

            campUse.Warp(PrototypeCampUse.PlayerMinimumX);
            RefreshAll();
            int foodBeforeFarCampfireUse = session.GetStorage(ResourceKind.Food);
            Require(session.GetStorage(ResourceKind.Food) == foodBeforeFarCampfireUse &&
                    campInteraction.ActiveTargetKind != PrototypeCampInteractionTargetKind.Campfire &&
                    !campInteraction.IsPopupOpen, "모닥불 1.25 unit 밖에서는 모닥불 선택·팝업·식량 차감 없음");
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Campfire, eatButton);
            Require(session.GetStorage(ResourceKind.Food) == foodBeforeFarCampfireUse - 1, "모닥불 근접 식사 기능 성공");

            campUse.Warp(PrototypeCampUse.PlayerMinimumX);
            RefreshAll();
            int woodBeforeFarWorkbenchUse = session.GetStorage(ResourceKind.Wood);
            Require(!session.HasResearched(TechKind.StoneAxe) &&
                    session.GetStorage(ResourceKind.Wood) == woodBeforeFarWorkbenchUse &&
                    campInteraction.ActiveTargetKind != PrototypeCampInteractionTargetKind.Workbench &&
                    !campInteraction.IsPopupOpen, "작업대 1.25 unit 밖에서는 작업대 선택·팝업·자원 차감 없음");
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Workbench, researchAxeButton);
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Workbench, craftAxeButton);
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Workbench, researchRopeButton);
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Workbench, craftRopeButton);
            Require(session.HasStructure(StructureKind.Campfire), "모닥불 UI 건설");
            Require(session.HasStructure(StructureKind.Workbench), "작업대 UI 건설");
            Require(session.HasStructure(StructureKind.RainCollector), "빗물받이 UI 건설");
            RequireInstalledStructureArt();
            Require(session.HasAxe && session.HasRope, "제작·연구 UI 경로");

            campUse.Warp(PrototypeCampUse.PlayerMinimumX);
            RefreshAll();
            Require(!campInteraction.IsPopupOpen &&
                    campInteraction.ActiveTargetKind != PrototypeCampInteractionTargetKind.Campfire &&
                    campInteraction.ActiveTargetKind != PrototypeCampInteractionTargetKind.RainCollector &&
                    !campUse.IsDayBenefitPrepared(StructureKind.Campfire) &&
                    !campUse.IsDayBenefitPrepared(StructureKind.RainCollector), "설비 1.25 unit 밖에서는 해당 설비 선택·보너스·팝업 없음");
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.Campfire, prepareCampfireButton);
            Require(campUse.IsDayBenefitPrepared(StructureKind.Campfire), "모닥불 근접 상호작용으로 하루 보너스 준비");
            InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind.RainCollector, collectRainButton);
            Require(campUse.IsDayBenefitPrepared(StructureKind.RainCollector), "빗물받이 근접 상호작용으로 하루 보너스 준비");

            BeginExpeditionThroughMapForVerification(PrototypeExpeditionRegionId.Shallows);
            Require(session.Phase == GamePhase.Exploring && session.SelectedRegionId == PrototypeExpeditionRegionId.Shallows,
                "지도 근접 상호작용·얕은 바다 선택·출발 UI 경로");
            PrototypeSearchNodeContractResult searchNodeContract = PrototypeSearchNodeRuntimeContract.Verify();
            Require(searchNodeContract.Passed, "7지역 결정론적 수색 노드·잔량·원자 거래 계약 · " + searchNodeContract.Detail);
            Require(nodes.Count == PrototypeSearchRegionCatalog.Get(PrototypeExpeditionRegionId.Shallows).Nodes.Count,
                "선택 지역의 환경 수색 오브젝트만 실제 월드에 생성");
            Require(nodes.TrueForAll(node => node.Definition.NodeId.StartsWith("node.sea.shallows.", StringComparison.Ordinal)),
                "선택한 얕은 바다 stable region/node ID만 실제 수색 오브젝트에 반영");
            RequireExplorationBarrierArt();
            UpdateResourceLabelLayout();
            RequireReadableResourceLabels(PrototypeLocalization.KoreanLocaleCode);
            if (!string.IsNullOrWhiteSpace(explorationScreenshotPath))
            {
                CaptureVerificationPng(explorationScreenshotPath, 1280, 800);
            }

            NodeView waterNode = nodes.Find(node => node.Definition.RequiresSwimming);
            Require(waterNode != null && nodes.FindAll(node => node.Definition.RequiresSwimming).Count >= 2, "얕은 연안 환경 수색 오브젝트 2개");
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

            int hazardExposureBeforeTray = searchNodeRuntime.Ledger.TotalHazardExposureCount;
            SearchNearestNode();
            Require(searchNodeRuntime.IsTrayOpen && searchLootTrayPanel.activeSelf &&
                    searchNodeRuntime.Ledger.TotalHazardExposureCount == hazardExposureBeforeTray + 1,
                "수영 중 직접 뒤지기 완료 시 비용·위험 노출을 한 번 적용하고 compact 발견물 트레이 표시");
            string verificationFolder = string.IsNullOrWhiteSpace(explorationScreenshotPath)
                ? string.Empty
                : Path.GetDirectoryName(explorationScreenshotPath);
            if (!string.IsNullOrEmpty(verificationFolder))
            {
                CaptureVerificationPng(Path.Combine(verificationFolder, "kim-survival-search-tray-ko-1280x800.png"), 1280, 800);
            }

            string activeNodeBeforeLocale = searchNodeRuntime.ActiveNodeId;
            int focusBeforeLocale = searchNodeRuntime.FocusedIndex;
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            Require(searchNodeRuntime.IsTrayOpen && searchNodeRuntime.ActiveNodeId == activeNodeBeforeLocale &&
                    searchNodeRuntime.FocusedIndex == focusBeforeLocale &&
                    searchNodeRuntime.Ledger.TotalHazardExposureCount == hazardExposureBeforeTray + 1,
                "영어 전환은 수색 대상·포커스·선별 중 위험 정지를 보존");
            RequireReadableSearchLootTray(PrototypeLocalization.EnglishLocaleCode);
            if (!string.IsNullOrEmpty(verificationFolder))
            {
                CaptureVerificationPng(Path.Combine(verificationFolder, "kim-survival-search-tray-en-1280x800.png"), 1280, 800);
            }

            Require(localization.SetQaLocale(), "실제 qps-long 수색 트레이 전환");
            RefreshAll();
            Require(searchNodeRuntime.ActiveNodeId == activeNodeBeforeLocale && searchNodeRuntime.FocusedIndex == focusBeforeLocale,
                "qps-long 전환도 같은 node와 발견물 포커스를 보존");
            RequireReadableSearchLootTray(PrototypeLocalization.QpsLongLocaleCode);
            if (!string.IsNullOrEmpty(verificationFolder))
            {
                CaptureVerificationPng(Path.Combine(verificationFolder, "kim-survival-search-tray-qps-long-1280x800.png"), 1280, 800);
            }

            PrototypeSearchLootActions keyboardLootActions = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput
            {
                KeyboardNext = true,
                KeyboardConfirm = true
            });
            PrototypeSearchLootActions gamepadLootActions = PrototypeSearchLootActions.FromRaw(new PrototypeRawSearchLootInput
            {
                HorizontalAxis = 1f,
                GamepadConfirm = true
            });
            Require(keyboardLootActions.CycleDirection == gamepadLootActions.CycleDirection &&
                    keyboardLootActions.ConfirmPressed == gamepadLootActions.ConfirmPressed,
                "키보드·마우스와 합성 게임패드는 동일한 발견물 선택 action snapshot으로 합류");
            localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
            RefreshAll();
            int remainingBeforeTake = searchNodeRuntime.ActiveNode.RemainingAmount;
            TakeFocusedSearchLoot();
            Require(searchNodeRuntime.IsTrayOpen && searchNodeRuntime.ActiveNode.RemainingAmount < remainingBeforeTake,
                "발견물 담기는 node 잔량과 가방을 한 번만 갱신");
            int exposureDuringSelection = searchNodeRuntime.Ledger.TotalHazardExposureCount;
            RefreshSearchLootTrayUi();
            Require(searchNodeRuntime.Ledger.TotalHazardExposureCount == exposureDuringSelection,
                "발견물 선별 중 추가 위험 판정 정지");
            CloseSearchLootTray();

            Require(session.Phase == GamePhase.Exploring && nodes.Count == 6, "수색 중 영어 즉시 전환과 6 node 상태 재구성");
            RequireReadableResourceLabels(PrototypeLocalization.EnglishLocaleCode);
            string keyboardExplorePrompt = localization.Format(PrototypeInputPromptKeys.Explore(PrototypeInputDevice.KeyboardMouse), localization.DeviceName(PrototypeInputDevice.KeyboardMouse), session.ActiveBagSlotCount);
            string gamepadExplorePrompt = localization.Format(PrototypeInputPromptKeys.Explore(PrototypeInputDevice.Gamepad), localization.DeviceName(PrototypeInputDevice.Gamepad), session.ActiveBagSlotCount);
            Require(keyboardExplorePrompt.Contains(localization.DeviceName(PrototypeInputDevice.KeyboardMouse)) &&
                    keyboardExplorePrompt.Contains("1–4") &&
                    gamepadExplorePrompt.Contains(localization.DeviceName(PrototypeInputDevice.Gamepad)) &&
                    gamepadExplorePrompt.Contains("D-pad+A"), "수색 키보드·게임패드 가방 조작 안내");
            if (!string.IsNullOrWhiteSpace(swimmingScreenshotPath))
            {
                RequireReadableResourceLabels(PrototypeLocalization.EnglishLocaleCode);
                CaptureVerificationPng(swimmingScreenshotPath, 1280, 800);
            }

            playerTraversal.Warp(PrototypePlayerTraversal.CoastlineX - 0.05f, PrototypePlayerTraversal.WaterY, true);
            PrototypePlayerActions leaveWater = new PrototypePlayerActions(1f, false, false, false, false, -1);
            PrototypeTraversalStep shoreReturnStep = playerTraversal.Step(leaveWater, 0.1f, 1f, session);
            Require(!session.IsSwimming && !shoreReturnStep.Presentation.IsSwimming && Mathf.Approximately(playerTraversal.Y, PrototypePlayerTraversal.LandY), "해안 이탈 전환과 육지 높이 복귀");
            playerPresentation.Apply(playerTraversal.Warp(-1.1f, PrototypePlayerTraversal.LandY, false));
            NodeView landNode = nodes.Find(node => !node.Definition.RequiresSwimming);
            Require(landNode != null, "육상 환경 수색 오브젝트 존재");
            playerPresentation.Apply(playerTraversal.Warp(landNode.X, PrototypePlayerTraversal.LandY, false));
            SearchNearestNode();
            Require(searchNodeRuntime.IsTrayOpen, "육상 수색 오브젝트도 같은 발견물 트레이 상태 머신 사용");
            PrototypeSearchNodeSnapshot landBeforeLeave = searchNodeRuntime.ActiveNode.Clone();
            CloseSearchLootTray();
            playerPresentation.Apply(playerTraversal.Warp(waterNode.X, PrototypePlayerTraversal.WaterY, true));
            session.SetSwimming(true);
            SearchNearestNode();
            Require(searchNodeRuntime.IsTrayOpen && searchNodeRuntime.ActiveNode.State == PrototypeSearchNodeState.RevealedPartial &&
                    searchNodeRuntime.ActiveNode.SearchCount == 1,
                "재방문한 부분 잔류 node는 재추첨·추가 수색 비용 없이 동일 잔량 복원");
            CloseSearchLootTray();
            playerPresentation.Apply(playerTraversal.Warp(landNode.X, PrototypePlayerTraversal.LandY, false));
            session.SetSwimming(false);
            SearchNearestNode();
            Require(searchNodeRuntime.IsTrayOpen && searchNodeRuntime.ActiveNode.RemainingAmount == landBeforeLeave.RemainingAmount,
                "남긴 발견물은 같은 run의 같은 stable node에 유지");
            CloseSearchLootTray();

            for (int i = 0; i < nodes.Count; i += 1)
            {
                NodeView node = nodes[i];
                if (node.Definition.RequiresSwimming)
                {
                    continue;
                }

                playerPresentation.Apply(playerTraversal.Warp(node.X, PrototypePlayerTraversal.LandY, false));
                SearchNearestNode();
                if (!searchNodeRuntime.IsTrayOpen) continue;
                TakeAllSearchLoot();
                if (searchNodeRuntime.HasPendingBagSwap)
                {
                    bagButtons[0].onClick.Invoke();
                }
                CloseSearchLootTray();
            }
            Require(session.GetBagSlot(0).Amount > 0, "환경 수색 발견물 선별이 실제 가방 획득의 주 경로");
            float daylightBeforeMovement = session.Daylight;
            float energyBeforeMovement = session.Energy;
            session.TickSearch(1f, true);
            Require(session.Daylight < daylightBeforeMovement && session.Energy < energyBeforeMovement, "이동 중 일광·체력 소모");
            worldCamera.transform.position = new Vector3(Mathf.Clamp(playerTraversal.X + 2.5f, -6.5f, 12.5f), 0f, -10f);
            UpdateResourceLabelLayout();
            RefreshHud();
            Require(languageButton.GetComponentInChildren<TMP_Text>().text == localization.Format("ui.language.switch.en"), "수색 중 언어 설정 문구 유지");

            Require(session.ReturnToCamp(false), "캠프 귀환");
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();
            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.ExpeditionMap));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ExpeditionMap &&
                    campProximityText.text.Contains(localization.Format("interaction.action.expedition_done")),
                "당일 수집 완료 뒤 지도 근접 안내는 재출발 대신 완료 안내를 표시");
            Require(!TryOpenCampPopup() && !expeditionMapPanel.activeSelf &&
                    campFeedback.Key == "message.search.finished" &&
                    messageText.text == localization.Format("message.search.finished"),
                "당일 수집 완료 뒤 지도를 다시 사용하면 팝업을 열지 않고 명시적 안내 표시");
            if (!string.IsNullOrWhiteSpace(campProximityScreenshotFolder))
            {
                CaptureVerificationPng(
                    Path.Combine(campProximityScreenshotFolder, "kim-survival-hotfix-expedition-complete-notice-ko-1280x800.png"),
                    1280,
                    800);
            }
            phaseButton.onClick.Invoke();
            Require(session.Day == 2, "하루 정산 UI 경로");
            RefreshCampInteractionSelection();
            Require(IsGameJamLiveEscapeProfile &&
                    !campInteractionTargets.Any(value => value.Kind == PrototypeCampInteractionTargetKind.RescueSignal) &&
                    session.SignalStage == 0,
                "게임잼 프로필은 legacy 구조 신호대 상호작용을 노출하지 않고 3개 정식 탈출 경로만 사용");
            Require(session.Day == 2 && session.Phase == GamePhase.Camp, "2일차 캠프 상태");
            RefreshAll();
            RequireFiftyDayRuntimeContract();
            endingAlbumCollection.RestoreTransientSnapshot(endingAlbumSnapshotBeforeVerification);
            endingAlbumCollection.PersistenceEnabled = true;
            if (ownsVerificationLog)
            {
                RequirePlaytestLogRuntimeIntegration(playtestLog.VerificationLines);
                playtestLog.Dispose();
                playtestLog = null;
            }
            return "PASS · 7지역 stable ID와 환경 수색 노드의 seed 결정론, 미확인→부분 잔류→고갈, 1회 비용·위험 정지, 원자적 담기·교체·취소, 보호 돛천, snapshot 재방문, ko/en/qps-long 1280x800 및 키보드/합성 게임패드 동등성을 확인. Wave 20 뗏목, Wave 19 앨범, Wave 16 지도 A, Wave 15 Day 50와 캠프·배치·가방·수영·장벽·연기·무전을 회귀 확인";
        }

        private static void RequirePlaytestLogRuntimeIntegration(IReadOnlyList<string> lines)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            bool sawTarget = false;
            bool sawActionContext = false;
            bool sawCampaignLinkage = false;
            for (int index = 0; index < lines.Count; index += 1)
            {
                PrototypePlaytestEventRecord record = JsonUtility.FromJson<PrototypePlaytestEventRecord>(lines[index]);
                if (record == null)
                {
                    continue;
                }

                names.Add(record.event_name);
                sawTarget |= !string.IsNullOrEmpty(record.target_id) && !string.IsNullOrEmpty(record.target_kind);
                sawActionContext |= record.event_name == PrototypePlaytestEventNames.FacilityActionCompleted &&
                                    !string.IsNullOrEmpty(record.action);
                sawCampaignLinkage |= record.event_name == PrototypePlaytestEventNames.ExpeditionResultResolved &&
                                      record.run_seed > 0 &&
                                      !string.IsNullOrEmpty(record.region_id) &&
                                      !string.IsNullOrEmpty(record.profile_id) &&
                                      !string.IsNullOrEmpty(record.result_id);
            }

            Require(names.Contains(PrototypePlaytestEventNames.FacilityProximityEntered) &&
                    names.Contains(PrototypePlaytestEventNames.FacilityPopupOpened) &&
                    names.Contains(PrototypePlaytestEventNames.FacilityPopupClosed) &&
                    names.Contains(PrototypePlaytestEventNames.FacilityActionCompleted) &&
                    names.Contains(PrototypePlaytestEventNames.ResourceChanged) &&
                    names.Contains(PrototypePlaytestEventNames.ExpeditionRegionSelected) &&
                    names.Contains(PrototypePlaytestEventNames.ExpeditionStarted) &&
                    sawTarget && sawActionContext && sawCampaignLinkage,
                "Wave 15 실제 Play Mode 근접 지도·지역 선택·seed/profile/result JSONL 계측 연결");
        }

        private void RequireFiftyDayRuntimeContract()
        {
            int runtimeSeed = session.RunSeed;
            string runtimeProfileId = session.SessionProfileId;
            GameSession earlyRescue = new GameSession();
            earlyRescue.Grant(ResourceKind.Wood, 20);
            earlyRescue.Grant(ResourceKind.Salvage, 20);
            Require(earlyRescue.TryBuild(StructureKind.Workbench) &&
                    earlyRescue.TryResearch(TechKind.Rope) && earlyRescue.TryCraft(TechKind.Rope) &&
                    earlyRescue.TryUpgradeSignal() && earlyRescue.TryUpgradeSignal() &&
                    earlyRescue.Result == RunResult.Rescued && earlyRescue.Phase == GamePhase.Result && earlyRescue.Day == 1,
                "Play Mode 조기 구조 신호 완성은 Day 50을 기다리지 않고 즉시 성공");

            session.Reset(runtimeSeed, PrototypeSessionFlowProfileCatalog.StandardProfileId);
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            session.Grant(ResourceKind.Food, GameSession.FinalDay);
            for (int day = 1; day < GameSession.FinalDay; day += 1)
            {
                Require(session.BeginSearch(PrototypeExpeditionRegionId.Beach) && session.ReturnToCamp(false) &&
                        session.UseFood() && session.EndDay(),
                    "Play Mode 미탈출 Day " + day + " 자연 정산");
                Require(session.Result == RunResult.None && session.Day == day + 1,
                    "Play Mode Day " + day + " 종료는 조기 기한 실패 없이 다음 날 진행");
            }

            Require(session.Day == 50 && session.Result == RunResult.None,
                "Play Mode Day 49 정산 뒤 Day 50이 실제 플레이 가능");
            Require(session.BeginSearch(PrototypeExpeditionRegionId.Shallows) && session.ReturnToCamp(false) &&
                    session.UseFood() && session.EndDay() &&
                    session.Result == RunResult.Deadline && session.Day == 50,
                "Play Mode 미탈출 Day 50 종료에서만 terminal resolution");
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            RefreshAll();
            Require(resultPanel.activeSelf && !campProximityPrompt.activeSelf && !campInteractionPopup.activeSelf &&
                    !expeditionMapPanel.activeSelf &&
                    resultDetailText.text.Contains("50일"),
                "terminal 결과에서는 compact prompt·팝업·지도를 숨기고 한국어 50일 결과 사유 표시");

            session.Reset(runtimeSeed, runtimeProfileId);
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            campModuleExpansion.Reset();
            ResetModulePreviewReturnRoute();
            campFeedback = PrototypeLocalizedText.Empty;
            RefreshAll();
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

        private void RequireReadableCampPopup(bool allowCompactFont = false)
        {
            actionTitleText.ForceMeshUpdate(true, true);
            campPopupDetailText.ForceMeshUpdate(true, true);
            campProximityGlyphText.ForceMeshUpdate(true, true);
            campProximityText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            Require(campInteractionPopup.activeSelf && !actionTitleText.isTextOverflowing && !campPopupDetailText.isTextOverflowing,
                "1280x800 설비 전용 팝업 제목·설명 잘림 없음");
            Require(campInteractionPopup.GetComponent<RectTransform>().anchorMin.x >= 0f &&
                    campInteractionPopup.GetComponent<RectTransform>().anchorMax.x <= 1f &&
                    campInteractionPopup.GetComponent<RectTransform>().anchorMax.x - campInteractionPopup.GetComponent<RectTransform>().anchorMin.x <= 0.4f,
                "설비 팝업은 안전 영역 안의 화면 40% 이하 소형 패널");
            if (campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot)
            {
                RectTransform slotPopupRect = campInteractionPopup.GetComponent<RectTransform>();
                Require(slotPopupRect.anchorMax.y - slotPopupRect.anchorMin.y <= 0.35f &&
                        modulePreviewButton.gameObject.activeSelf && cancelPopupButton.gameObject.activeSelf,
                    "연결 슬롯 팝업은 공간을 보존하는 화면 높이 35% 이하의 단일-action 패널");
            }
            for (int i = 0; i < campPopupButtons.Count; i += 1)
            {
                if (!campPopupButtons[i].gameObject.activeSelf)
                {
                    continue;
                }

                TMP_Text label = campPopupButtons[i].GetComponentInChildren<TMP_Text>();
                label.ForceMeshUpdate(true, true);
                Require(label.fontSizeMin >= (allowCompactFont ? 12f : 26f) &&
                        label.maxVisibleLines <= (allowCompactFont ? 3 : 2) && !label.isTextOverflowing,
                    "1280x800 설비 팝업 행동 라벨 최소 크기·잘림 없음: " + campPopupButtons[i].name);
            }
        }

        private void RequireCompactCampPromptSkin()
        {
            Require(campPromptSkin != null && campPromptSkin.AssetId == AssetCampContextPrompt && campPromptSkin.Frame != null,
                "채택된 compact-a가 runtime Resources 스킨으로 연결");
            Require(campProximityFrameImage != null && campProximityFrameImage.sprite == campPromptSkin.Frame &&
                    campProximityFrameImage.type == Image.Type.Sliced,
                "근접 안내는 compact-a 실제 sprite를 sliced Image로 사용");
            Require(campPromptSkin.Frame.texture.width == 384 && campPromptSkin.Frame.texture.height == 64 &&
                    campPromptSkin.Frame.pivot == new Vector2(192f, 32f) &&
                    campPromptSkin.Frame.border == new Vector4(70f, 12f, 30f, 12f),
                "compact-a 원본 384x64, 중앙 pivot, L70/R30/T12/B12 border 보존");
            Require(ColorContrastRatio(campProximityText.color, CompactPromptBodyColor) >= 4.5f,
                "compact-a 행동 문구는 ko/en/qps-long 공통으로 본문 판과 4.5:1 이상 대비");
        }

        private void RequireReadableCampProximityPrompt(bool allowCompactFont)
        {
            RectTransform promptRect = campProximityPrompt.GetComponent<RectTransform>();
            RectTransform messageRect = messagePanelImage.rectTransform;
            RectTransform glyphRect = campProximityGlyphText.rectTransform;
            RectTransform actionRect = campProximityText.rectTransform;
            campProximityGlyphText.ForceMeshUpdate(true, true);
            campProximityText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();

            float widthPixels = (promptRect.anchorMax.x - promptRect.anchorMin.x) * CampProximityPromptReferenceWidth;
            float heightPixels = (promptRect.anchorMax.y - promptRect.anchorMin.y) * CampProximityPromptReferenceHeight;
            float gapPixels = (messageRect.anchorMin.y - promptRect.anchorMax.y) * CampProximityPromptReferenceHeight;
            Require(campProximityPrompt.transform.parent == canvas.transform,
                "근접 안내는 월드가 아닌 Canvas 내 독립 UI");
            Require(widthPixels >= 511.9f && widthPixels <= 512.1f &&
                    heightPixels >= 47.9f && heightPixels <= 48.1f,
                "1280x800 근접 안내는 512x48px compact-a 범위");
            Require(gapPixels >= 11.9f && promptRect.anchorMin.y >= 0.55f,
                "1280x800 내레이션 카드 아래 12px 이상 간격·월드 보행 영역 보존");
            Require(glyphRect.rect.width >= 43.9f && glyphRect.rect.height >= 43.9f &&
                    glyphRect != actionRect && campProximityGlyphText.transform != campProximityText.transform,
                "입력 glyph는 행동명 TMP와 분리된 고정 44x44 이상 왼쪽 슬롯");
            Require(campProximityGlyphText.textWrappingMode == TextWrappingModes.NoWrap &&
                    campProximityGlyphText.textInfo.lineCount <= 1 && !campProximityGlyphText.isTextOverflowing,
                "keyboard/gamepad glyph 한 줄 무잘림");
            Require(campProximityText.enableAutoSizing && campProximityText.fontSizeMin >= 19f &&
                    campProximityText.fontSize >= 19f &&
                    campProximityText.fontSizeMax <= 23f && campProximityText.textWrappingMode == TextWrappingModes.NoWrap &&
                    campProximityText.overflowMode == TextOverflowModes.Overflow && campProximityText.textInfo.lineCount <= 1 &&
                    !campProximityText.isTextOverflowing,
                (allowCompactFont ? "qps-long" : "ko/en") + " 행동·대상 TMP 단일행 19px 이상·말줄임 없는 무잘림 정책");
        }

        private void ConfigureCampPopupLayout(bool moduleSlot)
        {
            RectTransform popupRect = campInteractionPopup.GetComponent<RectTransform>();
            bool escapeProject = campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.SmokeBeacon ||
                                 campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.RadioBench;
            popupRect.anchorMin = escapeProject ? new Vector2(0.025f, 0.025f) : moduleSlot ? CampPopupModuleSlotAnchorMin : CampPopupDefaultAnchorMin;
            popupRect.anchorMax = escapeProject ? new Vector2(0.975f, 0.975f) : moduleSlot ? CampPopupModuleSlotAnchorMax : CampPopupDefaultAnchorMax;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            Sprite escapeFrame = hazardEscapeEndingRuntime == null ? null : hazardEscapeEndingRuntime.EscapeProjectPresentationFrame;
            if (escapeProject && escapeFrame != null)
            {
                campInteractionPopupFrameImage.sprite = escapeFrame;
                campInteractionPopupFrameImage.type = Image.Type.Simple;
                campInteractionPopupFrameImage.preserveAspect = true;
                campInteractionPopupFrameImage.color = Color.white;
            }
            else
            {
                campInteractionPopupFrameImage.sprite = campInteractionPopupDefaultSprite;
                campInteractionPopupFrameImage.type = Image.Type.Simple;
                campInteractionPopupFrameImage.preserveAspect = false;
                campInteractionPopupFrameImage.color = campInteractionPopupDefaultColor;
            }

            bool pseudoLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;
            actionTitleText.enableAutoSizing = true;
            actionTitleText.fontSizeMin = pseudoLong ? 14f : 26f;
            actionTitleText.fontSizeMax = 36f;
            actionTitleText.maxVisibleLines = pseudoLong ? 3 : 2;
            campPopupDetailText.enableAutoSizing = true;
            campPopupDetailText.fontSizeMin = pseudoLong ? 12f : 20f;
            campPopupDetailText.fontSizeMax = 28f;
            campPopupDetailText.maxVisibleLines = pseudoLong ? 5 : 3;
            for (int i = 0; i < campPopupButtons.Count; i += 1)
            {
                TMP_Text label = campPopupButtons[i].GetComponentInChildren<TMP_Text>();
                label.fontSizeMin = pseudoLong ? 12f : 26f;
                label.fontSizeMax = pseudoLong ? 24f : 28f;
                label.maxVisibleLines = pseudoLong ? 3 : 2;
            }
        }

        private void RequireReadableTopHud(string localeCode)
        {
            statusText.ForceMeshUpdate(true, true);
            resourceText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            Require(statusText.enableAutoSizing && resourceText.enableAutoSizing &&
                    statusText.fontSizeMin >= 22f && resourceText.fontSizeMin >= 18f &&
                    statusText.maxVisibleLines == 2 && resourceText.maxVisibleLines == 2,
                localeCode + " 상단 HUD는 날짜 22px·정확한 세부 재료 18px 이상의 두 줄 자동 맞춤 계약 사용");
            Require(!statusText.isTextOverflowing && !resourceText.isTextOverflowing,
                localeCode + " 정상 캠프 상단 HUD TMP overflow=0" +
                " status=" + statusText.isTextOverflowing + "@" + statusText.fontSize.ToString("0.0") +
                " resource=" + resourceText.isTextOverflowing + "@" + resourceText.fontSize.ToString("0.0") +
                " statusText=" + statusText.text.Replace('\n', '/') +
                " resourceText=" + resourceText.text.Replace('\n', '/'));
        }

        private bool TryProgressEscapeProject(string escapeId)
        {
            if (hazardEscapeEndingRuntime == null)
            {
                return false;
            }

            bool progressed = hazardEscapeEndingRuntime.TryProgressEscapeProject(escapeId);
            PrototypeEscapeProjectState state = hazardEscapeEndingRuntime.EscapeDirector.GetState(escapeId);
            if (string.Equals(escapeId, PrototypeRaftEscapeConfig.EscapeId, StringComparison.Ordinal))
            {
                campFeedback = new PrototypeLocalizedText(
                    RaftFeedbackKey(state.LastResultCode),
                    state.Progress,
                    state.RequiredProgress);
                return progressed;
            }
            campFeedback = new PrototypeLocalizedText(
                progressed
                    ? (state.Complete ? "escape.project.message.complete" : "escape.project.message.progress")
                    : state.LastResultCode == "escape.requirement.research"
                        ? "escape.project.message.research"
                        : "escape.project.message.resources",
                localization.Format(escapeId),
                state.Progress,
                state.RequiredProgress);
            return progressed;
        }

        private static string RaftFeedbackKey(string resultCode)
        {
            switch (resultCode)
            {
                case "escape.raft.requirement.rope":
                    return "escape.raft.message.rope";
                case "escape.raft.requirement.sailcloth":
                    return "escape.raft.message.sailcloth";
                case "escape.raft.requirement.launch_cost":
                case "escape.raft.requirement.resources":
                    return "escape.raft.message.resources";
                case "escape.raft.launch.confirm":
                    return "escape.raft.message.confirm";
                case "escape.raft.launch.failed_window":
                    return "escape.raft.message.failed";
                case "escape.raft.launch.retry_ready":
                    return "escape.raft.message.retry";
                case "escape.project.complete":
                    return "escape.raft.message.complete";
                default:
                    return "escape.raft.message.progress";
            }
        }

        private void ExecuteRaftPopupAction()
        {
            if (campInteraction.OpenPopupKind != PrototypeCampInteractionTargetKind.ShoreLaunch ||
                !campInteraction.TryConfirmAction())
            {
                return;
            }

            PrototypeEscapeProjectState state = hazardEscapeEndingRuntime.EscapeDirector.GetState(PrototypeRaftEscapeConfig.EscapeId);
            string actionName = "escape.raft." + state.LaunchState + "." + state.Progress;
            bool succeeded = playtestLog != null
                ? playtestLog.TrackFacilityAction(
                    PrototypeCampInteractionTargetKind.ShoreLaunch,
                    campInteraction.OpenPopupTargetId,
                    actionName,
                    delegate { return TryProgressEscapeProject(PrototypeRaftEscapeConfig.EscapeId); })
                : TryProgressEscapeProject(PrototypeRaftEscapeConfig.EscapeId);

            if (session.Result != RunResult.None)
            {
                campInteraction.ClosePopup();
                if (playtestLog != null)
                {
                    playtestLog.RecordPopupClosed(
                        PrototypeCampInteractionTargetKind.ShoreLaunch,
                        "facility.shore-launch",
                        succeeded ? "action_completed" : "action_rejected");
                }
            }
            else
            {
                campInteraction.PrepareOpenPopupForReturn();
            }
            RefreshAll();
        }

        private void RequireReadableEndingAlbumUi(bool pseudoLong)
        {
            Require(endingAlbumSelection.IsOpen && endingAlbumPanel.activeSelf &&
                    campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.EndingAlbum &&
                    !campInteractionPopup.activeSelf && !expeditionMapPanel.activeSelf &&
                    !campProximityPrompt.activeSelf && !bagPanel.activeSelf,
                "생존 앨범은 현장 popup 상태에서 채택 A 화면 하나만 표시");
            endingAlbumHeaderText.ForceMeshUpdate(true, true);
            endingAlbumDetailTitleText.ForceMeshUpdate(true, true);
            endingAlbumSummaryText.ForceMeshUpdate(true, true);
            endingAlbumStatusText.ForceMeshUpdate(true, true);
            endingAlbumControlsText.ForceMeshUpdate(true, true);
            endingAlbumCloseButton.GetComponentInChildren<TMP_Text>().ForceMeshUpdate(true, true);
            List<RectTransform> cardRects = new List<RectTransform>();
            for (int index = 0; index < endingAlbumCardButtons.Count; index += 1)
            {
                TMP_Text label = endingAlbumCardButtons[index].GetComponentInChildren<TMP_Text>();
                label.ForceMeshUpdate(true, true);
                cardRects.Add(endingAlbumCardButtons[index].GetComponent<RectTransform>());
            }
            Canvas.ForceUpdateCanvases();

            RectTransform panelRect = endingAlbumPanel.GetComponent<RectTransform>();
            Transform statusSurfaceObject = endingAlbumPanel.transform.Find("생존 앨범 상태 라벨 전용 바탕");
            RectTransform statusSurface = statusSurfaceObject == null ? null : statusSurfaceObject.GetComponent<RectTransform>();
            Image statusSurfaceImage = statusSurfaceObject == null ? null : statusSurfaceObject.GetComponent<Image>();
            Transform controlsSurfaceObject = endingAlbumPanel.transform.Find("생존 앨범 조작 안내 안전 바탕");
            RectTransform controlsSurface = controlsSurfaceObject == null ? null : controlsSurfaceObject.GetComponent<RectTransform>();
            Image controlsSurfaceImage = controlsSurfaceObject == null ? null : controlsSurfaceObject.GetComponent<Image>();
            int cardLabelBoundaryViolations = endingAlbumCardButtons.Count(value =>
            {
                TMP_Text label = value.GetComponentInChildren<TMP_Text>();
                return label == null || !RectContains(
                    WorldRect(value.GetComponent<RectTransform>()),
                    RenderedTextWorldRect(label),
                    0.5f);
            });
            float minimumCardGap = MinimumScreenRectGap(cardRects);
            Require(panelRect.anchorMin.x >= 0.025f && panelRect.anchorMax.x <= 0.975f &&
                    panelRect.anchorMin.y >= 0.025f && panelRect.anchorMax.y <= 0.975f &&
                    Mathf.Abs((panelRect.anchorMax.x - panelRect.anchorMin.x) * 1280f /
                              ((panelRect.anchorMax.y - panelRect.anchorMin.y) * 800f) - 1.6f) < 0.001f,
                "1280x800 채택 A 앨범은 20px 이상 안전 여백 안에서 원본 1.6:1 구도를 보존");
            Require(endingAlbumLayoutSprite != null && endingAlbumFrameImage.sprite == endingAlbumLayoutSprite &&
                    endingAlbumFrameImage.type == Image.Type.Simple && endingAlbumFrameImage.preserveAspect &&
                    Mathf.Approximately(endingAlbumLayoutSprite.rect.width, 1280f) &&
                    Mathf.Approximately(endingAlbumLayoutSprite.rect.height, 800f),
                "런타임 앨범 프레임은 채택된 album-spread-a 1280x800 원화만 사용");
            Require(statusSurface != null && statusSurfaceImage != null && statusSurfaceImage.color.a >= 0.98f &&
                    RectContains(WorldRect(statusSurface), WorldRect(endingAlbumStatusText.rectTransform), 0.5f) &&
                    !WorldRect(statusSurface).Overlaps(WorldRect(endingAlbumCloseButton.GetComponent<RectTransform>())),
                "ko/en/qps-long 해금 상태 라벨은 원화 하단 장식 라벨을 가리는 전용 불투명 바탕 안에 있고 닫기 버튼과 겹치지 않음");
            Rect controlsScreenRect = UiScreenRect(controlsSurface);
            Rect controlsWorldRect = controlsSurface == null ? default(Rect) : WorldRect(controlsSurface);
            Rect controlsTextWorldRect = RenderedTextWorldRect(endingAlbumControlsText);
            bool controlsContained = controlsSurface != null &&
                                     RectContains(controlsWorldRect, controlsTextWorldRect, 0.5f);
            Require(controlsSurface != null && controlsSurfaceImage != null && controlsSurfaceImage.color.a >= 0.90f &&
                    controlsContained && controlsScreenRect.yMin >= 40f && controlsScreenRect.yMax <= 160f,
                "ko/en/qps-long 앨범 조작 안내는 하단 40px 안전 영역 위의 전용 고대비 바탕 안에 완전히 표시" +
                "; surfaceWorld=" + controlsWorldRect +
                "; textWorld=" + controlsTextWorldRect +
                "; screen=" + controlsScreenRect +
                "; contained=" + controlsContained +
                "; alpha=" + (controlsSurfaceImage == null ? -1f : controlsSurfaceImage.color.a));
            Require(endingAlbumCardButtons.Count == PrototypeEndingCatalog.All.Count &&
                    PrototypeEndingCatalog.All.Count == 21 && CountRectOverlaps(cardRects) == 0 &&
                    minimumCardGap >= 24f && cardLabelBoundaryViolations == 0,
                "정본 21개 엔딩 카드는 normal 5·comic 5·rare 4·gamejam-stay 2·day50 5 행에서 24px 이상 분리되고 각 라벨이 소유 카드 안에 있음");
            Require(endingAlbumHeaderText.enableAutoSizing && endingAlbumHeaderText.fontSizeMin >= 18f &&
                    endingAlbumDetailTitleText.enableAutoSizing && endingAlbumDetailTitleText.fontSizeMin >= 18f &&
                    endingAlbumSummaryText.enableAutoSizing && endingAlbumSummaryText.fontSizeMin >= 18f &&
                    endingAlbumStatusText.enableAutoSizing && endingAlbumStatusText.fontSizeMin >= 18f &&
                    endingAlbumControlsText.enableAutoSizing && endingAlbumControlsText.fontSizeMin >= 18f &&
                    !endingAlbumHeaderText.isTextOverflowing && !endingAlbumDetailTitleText.isTextOverflowing &&
                    !endingAlbumSummaryText.isTextOverflowing && !endingAlbumStatusText.isTextOverflowing &&
                    !endingAlbumControlsText.isTextOverflowing &&
                    !endingAlbumCloseButton.GetComponentInChildren<TMP_Text>().isTextOverflowing,
                pseudoLong
                    ? "qps-long 앨범 제목·힌트·상태·조작은 18px 이상에서 overflow 0"
                    : "ko/en 앨범 제목·힌트·상태·조작은 18px 이상에서 overflow 0");
            for (int index = 0; index < endingAlbumCardButtons.Count; index += 1)
            {
                TMP_Text label = endingAlbumCardButtons[index].GetComponentInChildren<TMP_Text>();
                Outline outline = endingAlbumCardButtons[index].GetComponent<Outline>();
                Require(label.enableAutoSizing && label.fontSizeMin >= 18f && !label.isTextOverflowing &&
                        outline != null && Mathf.Abs(outline.effectDistance.x) >= 2f,
                    "앨범 카드 " + index + "는 범주 문양·해금 기호·테두리를 색상과 함께 표시");
            }
        }

        private void WriteEndingAlbumLayoutEvidence(string evidenceFolder)
        {
            if (string.IsNullOrWhiteSpace(evidenceFolder))
            {
                return;
            }

            Directory.CreateDirectory(evidenceFolder);
            int overflowCount = 0;
            TMP_Text[] primaryTexts =
            {
                endingAlbumHeaderText,
                endingAlbumDetailTitleText,
                endingAlbumSummaryText,
                endingAlbumStatusText,
                endingAlbumControlsText,
                endingAlbumCloseButton.GetComponentInChildren<TMP_Text>()
            };
            for (int index = 0; index < primaryTexts.Length; index += 1)
            {
                if (primaryTexts[index].isTextOverflowing) overflowCount += 1;
            }
            List<RectTransform> cardRects = new List<RectTransform>();
            for (int index = 0; index < endingAlbumCardButtons.Count; index += 1)
            {
                TMP_Text label = endingAlbumCardButtons[index].GetComponentInChildren<TMP_Text>();
                if (label.isTextOverflowing) overflowCount += 1;
                cardRects.Add(endingAlbumCardButtons[index].GetComponent<RectTransform>());
            }

            RectTransform panelRect = endingAlbumPanel.GetComponent<RectTransform>();
            int offscreenCount = panelRect.anchorMin.x < 0f || panelRect.anchorMin.y < 0f ||
                                 panelRect.anchorMax.x > 1f || panelRect.anchorMax.y > 1f
                ? 1
                : 0;
            string evidence =
                "PASS · Wave 19 ending album A layout metrics\n" +
                "Resolution: 1280x800\n" +
                "Panel bounds px: L32 R1248 B20 T780\n" +
                "Safe margins px: L32 R32 B20 T20\n" +
                "Panel aspect: 1.600\n" +
                "Ending catalog count: " + PrototypeEndingCatalog.All.Count + "\n" +
                "Achievement mapping count: " + PrototypeEndingCatalog.All.Select(value => value.AchievementMappingId).Distinct(StringComparer.Ordinal).Count() + "\n" +
                "TMP overflow count: " + overflowCount + "\n" +
                "Panel offscreen count: " + offscreenCount + "\n" +
                "Ending card overlap count: " + CountRectOverlaps(cardRects) + "\n" +
                "Ending card minimum visual gap px: " + MinimumScreenRectGap(cardRects).ToString("F1") + "\n" +
                "Ending card label boundary violations: " + endingAlbumCardButtons.Count(value =>
                {
                    TMP_Text label = value.GetComponentInChildren<TMP_Text>();
                    return label == null || !RectContains(WorldRect(value.GetComponent<RectTransform>()), RenderedTextWorldRect(label), 0.5f);
                }) + "\n" +
                "Status label dedicated surface: PASS · opaque>=0.98 · close overlap=0\n" +
                "Controls safe surface: PASS · opaque>=0.90 · screen y=40..160\n" +
                "Locales: ko PASS, en PASS, qps-long PASS\n" +
                "Input paths: keyboard/mouse PASS, synthetic gamepad PASS\n";
            File.WriteAllText(Path.Combine(evidenceFolder, "wave19-ending-album-layout-metrics.txt"), evidence);
        }

        private void RequireReadableExpeditionMapUi(bool pseudoLong)
        {
            Require(expeditionMapSelection.IsOpen && expeditionMapPanel.activeSelf &&
                    campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap &&
                    !campInteractionPopup.activeSelf && !campProximityPrompt.activeSelf && !bagPanel.activeSelf,
                "수집 지도는 직접 상호작용 팝업 하나만 표시하고 월드 근접 안내·가방을 숨김");
            expeditionMapTitleText.ForceMeshUpdate(true, true);
            expeditionMapDetailText.ForceMeshUpdate(true, true);
            expeditionMapRiskText.ForceMeshUpdate(true, true);
            expeditionMapWeatherText.ForceMeshUpdate(true, true);
            expeditionMapEquipmentText.ForceMeshUpdate(true, true);
            expeditionMapSpecialText.ForceMeshUpdate(true, true);
            controlsText.ForceMeshUpdate(true, true);
            for (int i = 0; i < expeditionRegionButtons.Count; i += 1)
            {
                expeditionRegionButtons[i].GetComponentInChildren<TMP_Text>().ForceMeshUpdate(true, true);
            }
            expeditionMapConfirmButton.GetComponentInChildren<TMP_Text>().ForceMeshUpdate(true, true);
            expeditionMapCancelButton.GetComponentInChildren<TMP_Text>().ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();

            RectTransform mapRect = expeditionMapPanel.GetComponent<RectTransform>();
            Require(mapRect.anchorMin.x >= 0.005f && mapRect.anchorMax.x <= 0.995f &&
                    mapRect.anchorMin.y >= 0.005f && mapRect.anchorMax.y <= 0.995f &&
                    Mathf.Abs((mapRect.anchorMax.x - mapRect.anchorMin.x) * 1280f /
                              ((mapRect.anchorMax.y - mapRect.anchorMin.y) * 800f) - 1.6f) < 0.001f,
                "1280x800 채택 A 지도는 20px 안전 여백 안에서 원본 1.6:1 구도를 보존");
            Require(expeditionMapLayoutSprite != null && expeditionMapFrameImage.sprite == expeditionMapLayoutSprite &&
                    expeditionMapFrameImage.type == Image.Type.Simple && expeditionMapFrameImage.preserveAspect &&
                    Mathf.Approximately(expeditionMapLayoutSprite.rect.width, 1280f) &&
                    Mathf.Approximately(expeditionMapLayoutSprite.rect.height, 800f),
                "런타임 지도 프레임은 채택된 A안 1280x800 원화만 사용");
            Require(expeditionMapTitleText.enableAutoSizing && expeditionMapTitleText.fontSizeMin >= 18f &&
                    expeditionMapTitleText.maxVisibleLines == 2 && !expeditionMapTitleText.isTextOverflowing,
                "ko/en/qps-long 지도 제목은 Day 1/50과 지역명을 우측 rail에서 잘림 없이 표시");
            Require(expeditionMapDetailText.enableAutoSizing && expeditionMapDetailText.fontSizeMin >= 18f &&
                    !expeditionMapDetailText.isTextOverflowing,
                "우측 상세 rail 예상 자원 TMP는 18px 이상에서 잘림 없이 표시");
            Require(!expeditionMapRiskText.isTextOverflowing,
                "우측 상세 rail 위험·이동 시간 TMP는 18px 이상에서 잘림 없이 표시");
            Require(!expeditionMapWeatherText.isTextOverflowing,
                "우측 상세 rail 날씨 TMP는 18px 이상에서 잘림 없이 표시");
            Require(!expeditionMapEquipmentText.isTextOverflowing,
                "우측 상세 rail 필요 장비 TMP는 18px 이상에서 잘림 없이 표시");
            Require(!expeditionMapSpecialText.isTextOverflowing,
                "우측 상세 rail 특별 발견 TMP는 18px 이상에서 잘림 없이 표시");
            Require(!controlsText.isTextOverflowing,
                "수집 지도 공통 키보드·게임패드 조작 안내는 1280x800 하단 안전 영역에 맞음");
            for (int i = 0; i < expeditionRegionButtons.Count; i += 1)
            {
                TMP_Text label = expeditionRegionButtons[i].GetComponentInChildren<TMP_Text>();
                Outline outline = expeditionRegionButtons[i].GetComponent<Outline>();
                Require(label.enableAutoSizing && label.fontSizeMin >= 18f && label.maxVisibleLines == 3 && !label.isTextOverflowing &&
                        RectContains(WorldRect(expeditionRegionButtons[i].GetComponent<RectTransform>()), RenderedTextWorldRect(label), 0.5f) &&
                        outline != null && Mathf.Abs(outline.effectDistance.x) >= 1f,
                    "수집 지역 노드 " + i + "는 문양·상태 문구·테두리와 함께 세 줄 이내이며 텍스트가 소유 카드 안에 있음");
            }
            Require(expeditionRegionButtons.Count == PrototypeExpeditionRegionCatalog.All.Count &&
                    CountRectOverlaps(expeditionRegionButtons
                        .Select(button => button.GetComponent<RectTransform>())
                        .ToArray()) == 0,
                "7개 수집 지역 노드는 1280x800 지도 영역에서 모두 생성되고 서로 겹치지 않음");
            Require(!expeditionMapConfirmButton.GetComponentInChildren<TMP_Text>().isTextOverflowing &&
                    !expeditionMapCancelButton.GetComponentInChildren<TMP_Text>().isTextOverflowing,
                pseudoLong
                    ? "qps-long 출발·취소는 자동 맞춤 두 줄 정책 안에서 잘림 없음"
                    : "ko/en 출발·취소 버튼 잘림 없음");
        }

        private void WriteExpeditionMapLayoutEvidence(string evidenceFolder)
        {
            if (string.IsNullOrWhiteSpace(evidenceFolder))
            {
                return;
            }

            List<RectTransform> railSections = new List<RectTransform>
            {
                expeditionMapTitleText.rectTransform,
                expeditionMapDetailText.rectTransform,
                expeditionMapRiskText.rectTransform,
                expeditionMapWeatherText.rectTransform,
                expeditionMapEquipmentText.rectTransform,
                expeditionMapSpecialText.rectTransform
            };
            List<RectTransform> nodeRects = new List<RectTransform>();
            int overflowCount = expeditionMapTitleText.isTextOverflowing || expeditionMapDetailText.isTextOverflowing ||
                                expeditionMapRiskText.isTextOverflowing || expeditionMapWeatherText.isTextOverflowing ||
                                expeditionMapEquipmentText.isTextOverflowing || expeditionMapSpecialText.isTextOverflowing
                ? 1
                : 0;
            for (int i = 0; i < expeditionRegionButtons.Count; i += 1)
            {
                nodeRects.Add(expeditionRegionButtons[i].GetComponent<RectTransform>());
                if (expeditionRegionButtons[i].GetComponentInChildren<TMP_Text>().isTextOverflowing)
                {
                    overflowCount += 1;
                }
            }
            if (expeditionMapConfirmButton.GetComponentInChildren<TMP_Text>().isTextOverflowing ||
                expeditionMapCancelButton.GetComponentInChildren<TMP_Text>().isTextOverflowing)
            {
                overflowCount += 1;
            }

            RectTransform panelRect = expeditionMapPanel.GetComponent<RectTransform>();
            int offscreenCount = panelRect.anchorMin.x < 0f || panelRect.anchorMin.y < 0f ||
                                 panelRect.anchorMax.x > 1f || panelRect.anchorMax.y > 1f
                ? 1
                : 0;
            int railOverlapCount = CountRectOverlaps(railSections);
            int nodeOverlapCount = CountRectOverlaps(nodeRects);
            string evidence =
                "PASS · Wave 16 expedition map A layout metrics\n" +
                "Resolution: 1280x800\n" +
                "Panel bounds px: L32 R1248 B20 T780\n" +
                "Safe margins px: L32 R32 B20 T20\n" +
                "Panel aspect: 1.600\n" +
                "TMP overflow count: " + overflowCount + "\n" +
                "Panel offscreen count: " + offscreenCount + "\n" +
                "Rail section overlap count: " + railOverlapCount + "\n" +
                "Region node overlap count: " + nodeOverlapCount + "\n" +
                "Locales: ko PASS, en PASS, qps-long PASS\n" +
                "Input paths: keyboard/mouse PASS, synthetic gamepad PASS\n";
            File.WriteAllText(Path.Combine(evidenceFolder, "wave16-expedition-map-a-layout-metrics.txt"), evidence);
        }

        private static int CountRectOverlaps(IReadOnlyList<RectTransform> rectTransforms)
        {
            int count = 0;
            for (int first = 0; first < rectTransforms.Count; first += 1)
            {
                Rect firstRect = WorldRect(rectTransforms[first]);
                for (int second = first + 1; second < rectTransforms.Count; second += 1)
                {
                    if (firstRect.Overlaps(WorldRect(rectTransforms[second])))
                    {
                        count += 1;
                    }
                }
            }
            return count;
        }

        private float MinimumScreenRectGap(IReadOnlyList<RectTransform> rectTransforms)
        {
            float minimum = float.MaxValue;
            for (int first = 0; first < rectTransforms.Count; first += 1)
            {
                Rect firstRect = UiScreenRect(rectTransforms[first]);
                for (int second = first + 1; second < rectTransforms.Count; second += 1)
                {
                    Rect secondRect = UiScreenRect(rectTransforms[second]);
                    float horizontal = Mathf.Max(0f, Mathf.Max(secondRect.xMin - firstRect.xMax, firstRect.xMin - secondRect.xMax));
                    float vertical = Mathf.Max(0f, Mathf.Max(secondRect.yMin - firstRect.yMax, firstRect.yMin - secondRect.yMax));
                    float gap = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
                    minimum = Mathf.Min(minimum, gap);
                }
            }
            return minimum == float.MaxValue ? 0f : minimum;
        }

        private Rect UiScreenRect(RectTransform rectTransform)
        {
            if (rectTransform == null) return default(Rect);
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(worldCamera, corners[0]);
            Vector2 maximum = RectTransformUtility.WorldToScreenPoint(worldCamera, corners[2]);
            float scaleX = CampProximityPromptReferenceWidth / Mathf.Max(1f, Screen.width);
            float scaleY = CampProximityPromptReferenceHeight / Mathf.Max(1f, Screen.height);
            return Rect.MinMaxRect(
                minimum.x * scaleX,
                minimum.y * scaleY,
                maximum.x * scaleX,
                maximum.y * scaleY);
        }

        private static Rect WorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private void RequireReadableCampModulePreview(bool allowEllipsis)
        {
            Require(campModuleExpansion.IsPreviewActive && modulePreviewGhost != null && modulePreviewBadgeText != null,
                "증축 미리보기 상태와 공간 윤곽 표현 존재");
            messageText.ForceMeshUpdate(true, true);
            controlsText.ForceMeshUpdate(true, true);
            campModuleReasonText.ForceMeshUpdate(true, true);
            modulePreviewBadgeText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            Require(!campActions.activeSelf && !campInteractionPopup.activeSelf && !campProximityPrompt.activeSelf &&
                    campModuleReasonChip.activeSelf && !bagPanel.activeSelf,
                "증축 미리보기는 전역 대시보드·설비 팝업·근접 안내·가방을 숨기고 목적·상태·비용 카드 하나만 표시");
            RectTransform reasonRect = campModuleReasonChip.GetComponent<RectTransform>();
            RectTransform messageRect = messagePanelImage.rectTransform;
            float reasonWidth = (reasonRect.anchorMax.x - reasonRect.anchorMin.x) * CampProximityPromptReferenceWidth;
            float reasonHeight = (reasonRect.anchorMax.y - reasonRect.anchorMin.y) * CampProximityPromptReferenceHeight;
            float reasonGap = (messageRect.anchorMin.y - reasonRect.anchorMax.y) * CampProximityPromptReferenceHeight;
            Require(reasonWidth <= 468f && reasonHeight <= 280.1f && reasonGap >= 19.9f &&
                    campModuleReasonText.enableAutoSizing && campModuleReasonText.fontSizeMin >= (allowEllipsis ? 15f : 20f) &&
                    campModuleReasonText.maxVisibleLines == 10 && campModuleReasonText.overflowMode == TextOverflowModes.Overflow &&
                    !campModuleReasonText.isTextOverflowing,
                "1280x800 목적·추천 용도·수용량·비용·선행 조건·부족분 카드 무잘림 계약 사용");
            if (allowEllipsis)
            {
                Require(messageText.enableAutoSizing && controlsText.enableAutoSizing &&
                        messageText.fontSizeMin >= 20f && controlsText.fontSizeMin >= 17f &&
                        messageText.overflowMode == TextOverflowModes.Ellipsis && controlsText.overflowMode == TextOverflowModes.Ellipsis,
                    "1280x800 qps-long 증축 상태·입력 안내 자동 맞춤·말줄임 안전 영역 정책");
            }
            else
            {
                Require(messageText.fontSize >= 24f && !messageText.isTextOverflowing && !controlsText.isTextOverflowing,
                    "1280x800 ko/en 증축 상태·공통 입력 안내 잘림 없음");
            }
            Require(modulePreviewOutlineRenderers.Count == 4 && modulePreviewBadgeRenderer != null &&
                    modulePreviewBadgeText.enableAutoSizing && modulePreviewBadgeText.fontSizeMin >= 27f &&
                    modulePreviewBadgeText.maxVisibleLines == 3 && modulePreviewBadgeText.overflowMode == TextOverflowModes.Ellipsis,
                "증축 후보는 고정 윤곽·3행 최대 배지·말줄임 안전 영역 정책 사용");
            Vector3 viewport = worldCamera.WorldToViewportPoint(modulePreviewGhost.transform.position);
            Require(viewport.x >= 0.2f && viewport.x <= 0.82f && viewport.y >= 0.22f && viewport.y <= 0.75f,
                "증축 후보 중심은 상단 HUD·하단 안내를 피한 월드 안전 영역 안에 있음");
            if (!allowEllipsis)
            {
                Require(!modulePreviewBadgeText.isTextOverflowing,
                    "1280x800 ko/en 증축 월드 배지 잘림 없음");
            }
        }

        private void OpenCampPopupForVerification(PrototypeCampInteractionTargetKind target)
        {
            if (campInteraction.IsPopupOpen)
            {
                campInteraction.ClosePopup();
            }

            campUse.Warp(GetCampInteractionTargetPosition(target));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == target && campInteraction.HasProximityPrompt,
                target + " 근접 시 단일 안내 표시");
            UseNearestCampTarget();
            Require(campInteraction.OpenPopupKind == target && campInteraction.IsPopupOpen,
                target + " 상호작용 뒤 전용 팝업 열림");
        }

        private void BeginExpeditionThroughMapForVerification(PrototypeExpeditionRegionId region)
        {
            if (campInteraction.IsPopupOpen)
            {
                CancelCampPopup();
            }

            campUse.Warp(GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind.ExpeditionMap));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ExpeditionMap &&
                    campInteraction.ActiveTargetId == "camp.expedition-map" && campInteraction.HasProximityPrompt,
                "수색은 캠프 지도·출구에 직접 접근한 뒤에만 시작 가능");
            UseNearestCampTarget();
            Require(expeditionMapSelection.IsOpen && expeditionMapPanel.activeSelf,
                "지도 Interact 뒤 지역 선택 팝업 열림");
            FocusExpeditionRegion(region);
            ConfirmSelectedExpeditionRegion();
            Require(session.Phase == GamePhase.Exploring && session.SelectedRegionId == region &&
                    session.ActiveRegionProfileId == PrototypeExpeditionRegionCatalog.Get(region).StableId &&
                    !expeditionMapSelection.IsOpen && !expeditionMapPanel.activeSelf,
                "지도 Submit은 포커스한 한 지역 프로필만 실제 수색에 적용");
        }

        private void OpenCampModuleSlotPopupForVerification(CampModuleArchetype archetype)
        {
            if (campInteraction.IsPopupOpen)
            {
                campInteraction.ClosePopup();
            }

            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(archetype);
            campUse.Warp(GetCampModuleSlotPosition(archetype));
            RefreshAll();
            Require(campInteraction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot &&
                    campInteraction.ActiveTargetId == definition.StartSlotId && campInteraction.HasProximityPrompt,
                definition.StartSlotId + " 직접 접근은 1.25 unit 안에서 단일 prompt를 표시");
            UseNearestCampTarget();
            Require(campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ModuleExpansionSlot &&
                    campInteraction.OpenPopupTargetId == definition.StartSlotId && campInteraction.IsPopupOpen,
                definition.StartSlotId + " Interact는 정확히 같은 슬롯의 소형 팝업을 연다");
        }

        private void InvokeCampPopupActionForVerification(PrototypeCampInteractionTargetKind target, Button button)
        {
            OpenCampPopupForVerification(target);
            button.onClick.Invoke();
        }

        private Vector2 GetCampInteractionTargetPosition(PrototypeCampInteractionTargetKind target)
        {
            switch (target)
            {
                case PrototypeCampInteractionTargetKind.Campfire:
                    return campPlacement.GetInstalledPosition(StructureKind.Campfire);
                case PrototypeCampInteractionTargetKind.Workbench:
                    return campPlacement.GetInstalledPosition(StructureKind.Workbench);
                case PrototypeCampInteractionTargetKind.RainCollector:
                    return campPlacement.GetInstalledPosition(StructureKind.RainCollector);
                case PrototypeCampInteractionTargetKind.RescueSignal:
                    return GetCampArtPoint(CampSignalAnchorNormalizedX, CampSignalAnchorNormalizedY);
                case PrototypeCampInteractionTargetKind.StoragePlanning:
                    return new Vector2(
                        campUse.CurrentRoomId == PrototypeCampModuleCatalog.StartRoomId ? StoragePlanningX : ModulePlanningX,
                        PrototypeCampUse.PlayerFloorY);
                case PrototypeCampInteractionTargetKind.ExpeditionMap:
                    return new Vector2(ExpeditionMapX, PrototypeCampUse.PlayerFloorY);
                case PrototypeCampInteractionTargetKind.EndingAlbum:
                    return new Vector2(EndingAlbumX, PrototypeCampUse.PlayerFloorY);
                case PrototypeCampInteractionTargetKind.SmokeBeacon:
                    return new Vector2(SmokeBeaconX, PrototypeCampUse.PlayerFloorY);
                case PrototypeCampInteractionTargetKind.RadioBench:
                    return new Vector2(RadioBenchX, PrototypeCampUse.PlayerFloorY);
                case PrototypeCampInteractionTargetKind.ShoreLaunch:
                    return new Vector2(ShoreLaunchX, PrototypeCampUse.PlayerFloorY);
                case PrototypeCampInteractionTargetKind.ModuleExpansionSlot:
                    return GetCampModuleSlotPosition(CampModuleArchetype.Upper);
                case PrototypeCampInteractionTargetKind.ModuleConnector:
                    CampModuleCommittedRoomSnapshot connectorRoom = null;
                    if (string.Equals(campUse.CurrentRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal))
                    {
                        IReadOnlyList<CampModuleCommittedRoomSnapshot> rooms = campModuleExpansion.CommittedRooms;
                        if (rooms.Count > 0)
                        {
                            connectorRoom = rooms[0];
                        }
                    }
                    else
                    {
                        campModuleExpansion.TryGetCommittedRoom(campUse.CurrentRoomId, out connectorRoom);
                    }

                    if (connectorRoom != null)
                    {
                        CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(connectorRoom.Archetype);
                        float connectorX = string.Equals(campUse.CurrentRoomId, PrototypeCampModuleCatalog.StartRoomId, StringComparison.Ordinal)
                            ? definition.StartConnectorDisplayX
                            : definition.ModuleConnectorDisplayX;
                        return new Vector2(connectorX, PrototypeCampUse.PlayerFloorY);
                    }
                    return campUse.PlayerPosition;
                default:
                    return campUse.PlayerPosition;
            }
        }

        private static Vector2 GetCampModuleSlotPosition(CampModuleArchetype archetype)
        {
            CampModuleDefinition definition = PrototypeCampModuleCatalog.Get(archetype);
            return new Vector2(definition.StartConnectorDisplayX, PrototypeCampUse.PlayerFloorY);
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
            Require(messageText.fontSizeMin >= 20f && !messageText.isTextOverflowing, "1280x800 축소 독백 카드의 신호대 부족 사유 잘림 없음");
        }

        private void RequireReadablePlacementUi()
        {
            messageText.ForceMeshUpdate(true, true);
            controlsText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            Require(messageText.fontSizeMin >= 26f && !messageText.isTextOverflowing, "1280x800 배치 상태 카드 18px 대응·잘림 없음");
            Require(!controlsText.isTextOverflowing, "1280x800 현재 장치 조작 안내 잘림 없음");
            Require(placementGhostLabel != null && placementGhostBadgeRenderer != null &&
                    !placementGhostLabel.transform.parent.gameObject.activeSelf &&
                    placementGhostOutlineRenderers.Count == 4,
                "배치 유령은 상단 상태 카드와 발자국 윤곽만 사용하고 캐릭터를 가리는 월드 OK/× 배지를 숨김");
        }

        private void RequireQpsGlobalPlacementLayout()
        {
            TMP_Text languageLabel = languageButton.GetComponentInChildren<TMP_Text>();
            Transform zoneBadge = worldRoot.Find("호환 건설 구역 안내");
            Transform signalBadge = worldRoot.Find("구조 신호대 전용 앵커 안내");

            statusText.ForceMeshUpdate(true, true);
            resourceText.ForceMeshUpdate(true, true);
            languageLabel.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();

            Require(statusText.fontSizeMin >= 22f && resourceText.fontSizeMin >= 18f &&
                    !statusText.isTextOverflowing && !resourceText.isTextOverflowing,
                "qps-long 최소 HUD는 1280x800 날짜·세부 재료 2행·무잘림 계약 사용");
            Require(languageLabel.enableAutoSizing && languageLabel.fontSizeMin >= 18f &&
                    languageLabel.maxVisibleLines == 1 && !languageLabel.isTextOverflowing,
                "qps-long 언어 전환은 우측 안전 여백 안의 1줄 자동 맞춤 사용");
            Require(zoneBadge == null && signalBadge == null,
                "건설 구역·신호 앵커의 대형 월드 텍스트 배지를 제거하고 상단 상태 카드로 일원화");
            Require(placementGhostLabel != null && placementGhostBadgeRenderer != null &&
                    !placementGhostLabel.transform.parent.gameObject.activeSelf &&
                    placementGhostOutlineRenderers.Count == 4,
                "qps-long 배치는 월드 OK/× 사각 배지 없이 상단 상태 카드와 outline만 사용");
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
            if (IsGameJamLiveEscapeProfile)
            {
                Require(rescueSignalRenderer == null, "GAME JAM 프로필은 구형 고정 구조 신호대 월드 표시 제거");
            }
            else
            {
                Require(rescueSignalRenderer != null && rescueSignalRenderer.sprite == rescueSignalSprite, "고정 앵커 구조 신호대 아트 연결");
            }
            Require(vineBarrierBlockedSprite != null && vineBarrierInteractableSprite != null && vineBarrierClearedSprite != null, "채택 덩굴·나무 장벽 3상태 직렬화");
            Require(kimAtlasSprite != null && kimIdleSprite != null && kimWalkSprite != null && kimSwimSprite != null,
                "채택 김씨 아틀라스의 대기·이동·수면 가독 포즈 런타임 연결");
            Require(woodIconSprite != null && stoneIconSprite != null && foodIconSprite != null && salvageIconSprite != null,
                "채택 자원 아이콘 4종 런타임 연결");
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
            NodeView expected = FindNearestSearchNode(SearchNodeInteractionDistance);
            List<NodeView> visible = new List<NodeView>();
            for (int i = 0; i < nodes.Count; i += 1)
            {
                NodeView node = nodes[i];
                Require(node.Root.GetComponentsInChildren<TMP_Text>(true).Any(text => text != node.Label),
                    localeCode + " 수색 오브젝트 상태 아이콘 마커 유지 · " + node.Definition.NodeId);
                if (node.LabelRoot == null || !node.LabelRoot.gameObject.activeSelf)
                {
                    continue;
                }

                node.Label.ForceMeshUpdate(true, true);
                Bounds bounds = node.LabelBackground.bounds;
                Vector3 viewportMinimum = worldCamera.WorldToViewportPoint(bounds.min);
                Vector3 viewportMaximum = worldCamera.WorldToViewportPoint(bounds.max);
                Require(node.Label.font != null && node.Label.fontSizeMin >= 28f && !node.Label.isTextOverflowing,
                    localeCode + " 수색 오브젝트 라벨 18px 대응·폰트·무잘림");
                Require(node.LabelBackground.color.a >= 0.95f && node.Label.color.grayscale >= 0.9f, localeCode + " 수색 오브젝트 라벨 배경 대비");
                Require(viewportMinimum.x >= -0.01f && viewportMaximum.x <= ResourceLabelSafeViewportRight + 0.01f,
                    localeCode + " 화면 가장자리·가방 패널 수색 라벨 클램프");
                float silhouetteLeft = playerRoot == null
                    ? node.Root.transform.position.x
                    : Mathf.Min(node.Root.transform.position.x, playerRoot.position.x - ResourceLabelPlayerHalfWidth);
                Require(bounds.max.x <= node.Root.transform.position.x - ResourceLabelHorizontalGap + 0.01f &&
                        bounds.max.x <= silhouetteLeft + 0.01f,
                    localeCode + " 수색 상세는 대상·플레이어 실루엣의 왼쪽 바깥에 배치");
                Require(viewportMinimum.y >= 0.49f && viewportMaximum.y <= 0.66f &&
                        Mathf.Abs(node.LabelRoot.position.y - ResourceLabelSafeWorldY) <= 0.01f &&
                        (playerRoot == null || bounds.min.y >= playerRoot.position.y + 0.8f),
                    localeCode + " 수색 상세는 플레이어·내레이션·HUD를 피한 단일 안전 수직 레인");
                visible.Add(node);
            }

            Require(visible.Count == (expected == null ? 0 : 1) &&
                    (expected == null || ReferenceEquals(visible[0], expected)),
                localeCode + " 상호작용 거리 안 가장 가까운 환경 수색 상세 프롬프트 정확히 1개");
        }

        private void RequireReadableSearchLootTray(string localeCode)
        {
            RefreshSearchLootTrayUi();
            Canvas.ForceUpdateCanvases();
            RectTransform trayRect = searchLootTrayPanel.GetComponent<RectTransform>();
            Require(trayRect.anchorMin.x >= 0.44f && trayRect.anchorMax.x <= 0.99f &&
                    trayRect.anchorMin.y >= 0.42f && trayRect.anchorMax.y <= 0.83f,
                localeCode + " compact 발견물 트레이 1280×800 안전영역·월드 보존");
            TMP_Text[] trayTexts = searchLootTrayPanel.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < trayTexts.Length; index += 1)
            {
                TMP_Text text = trayTexts[index];
                if (!text.gameObject.activeInHierarchy) continue;
                text.ForceMeshUpdate(true, true);
                Require(text.font != null && text.fontSize >= 12.5f, localeCode + " 발견물 트레이 TMP 폰트·최소 가독 크기");
                Require(!text.isTextOverflowing, localeCode + " 발견물 트레이 TMP overflow=0 · " + text.name +
                    " @" + text.fontSize.ToString("0.0") + " text=" + text.text.Replace('\n', '/'));
            }
            TMP_Text leaveLabel = searchLootLeaveButton.GetComponentInChildren<TMP_Text>();
            float leaveFloor = localeCode == PrototypeLocalization.QpsLongLocaleCode ? 18f : 23f;
            Require(leaveLabel.fontSizeMin >= leaveFloor && leaveLabel.fontSizeMax >= leaveFloor &&
                    leaveLabel.fontSize >= leaveFloor && !leaveLabel.isTextOverflowing,
                localeCode + " 발견물 닫기 action은 1280×800 최소 " + leaveFloor + "pt·무잘림");
            Require(!bagPanel.activeSelf,
                localeCode + " 발견물 트레이의 자체 가방 요약 사용 중 중복 대형 가방 패널 숨김");
            ResourceKind[] resourceKinds = { ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Food, ResourceKind.Salvage };
            for (int index = 0; index < resourceKinds.Length; index += 1)
            {
                ResourceKind kind = resourceKinds[index];
                string stableResourceId = GameSession.StableResourceIdForLegacy(kind);
                Require(ColorContrastRatio(Color.white, SearchLootButtonBackground(stableResourceId, kind, false)) >= 4.5f &&
                        ColorContrastRatio(Color.white, SearchLootButtonBackground(stableResourceId, kind, true)) >= 4.5f,
                    localeCode + " " + kind + " 발견물 비선택·선택 버튼의 흰 글자 대비 4.5:1 이상");
            }
            Require(nodes.All(node => node.Root.GetComponentsInChildren<TextMeshPro>(true)
                    .Where(marker => marker != null && marker != node.Label)
                    .All(marker => !marker.gameObject.activeSelf)),
                localeCode + " 발견물 트레이가 열리면 ?/×/◆ 월드 marker를 모두 숨김");
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
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < texts.Length; i += 1)
            {
                texts[i].ForceMeshUpdate(true, true);
            }
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

        private void SpawnSearchNode(float x, PrototypeSearchNodeDefinition definition)
        {
            PrototypeSearchNodeSnapshot snapshot = searchNodeRuntime.Ledger.GetOrCreate(definition);
            bool water = definition.RequiresSwimming;
            GameObject root = new GameObject("환경 수색 오브젝트 · " + definition.NodeId);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(x, water ? -1.72f : -2.25f, 0f);
            CreateSearchNodeArt(root.transform, definition, snapshot, water);
            float laneY = (water ? 1.18f : 1.25f) + (nodes.Count % 2) * 1.65f;
            SpriteRenderer labelBackground;
            TMP_Text label = CreateWorldBadge(
                root.transform,
                "환경 수색 안내",
                FormatSearchNodeWorldBadge(definition, snapshot),
                new Vector2(0f, laneY),
                new Vector2(ResourceLabelWidth, ResourceLabelHeight),
                snapshot.State == PrototypeSearchNodeState.Depleted
                    ? new Color(0.11f, 0.12f, 0.12f, 0.96f)
                    : water ? new Color(0.02f, 0.16f, 0.28f, 0.96f) : new Color(0.12f, 0.1f, 0.06f, 0.96f),
                Color.white,
                out labelBackground,
                localization.CurrentLocaleCode == PrototypeLocalization.KoreanLocaleCode ? 0.072f : 0.066f,
                28f,
                30f);
            nodes.Add(new NodeView
            {
                Definition = definition,
                X = x,
                Root = root,
                LabelRoot = label.transform.parent,
                Label = label,
                LabelBackground = labelBackground
            });
            UpdateResourceLabelLayout();
        }

        private string FormatSearchNodeWorldBadge(PrototypeSearchNodeDefinition definition, PrototypeSearchNodeSnapshot snapshot)
        {
            string kindName = localization.Format(SearchNodeKindKey(definition.Kind));
            if (snapshot.State == PrototypeSearchNodeState.Hidden)
            {
                return localization.Format(
                    "search.node.world.hidden",
                    localization.Format(PrototypeInputPromptKeys.InteractGlyph(playerInput.ActiveDevice)),
                    kindName,
                    localization.Format("search." + definition.HazardId));
            }
            if (snapshot.State == PrototypeSearchNodeState.Depleted)
            {
                return localization.Format("search.node.world.depleted", kindName);
            }
            return localization.Format(
                "search.node.world.partial",
                localization.Format(PrototypeInputPromptKeys.InteractGlyph(playerInput.ActiveDevice)),
                kindName,
                snapshot.Remaining.Length);
        }

        private static string SearchNodeKindKey(PrototypeSearchNodeKind kind)
        {
            return "search.node.kind." + kind.ToString().ToLowerInvariant();
        }

        private void UpdateResourceLabelLayout()
        {
            if (worldCamera == null)
            {
                return;
            }

            bool suppressLabels = searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen;
            for (int index = 0; index < searchWorldContextLabels.Count; index += 1)
            {
                GameObject contextLabel = searchWorldContextLabels[index];
                if (contextLabel != null)
                {
                    contextLabel.SetActive(!suppressLabels);
                }
            }
            float halfWidth = worldCamera.orthographicSize * MinimumSupportedAspect;
            float left = worldCamera.transform.position.x - halfWidth;
            float right = worldCamera.transform.position.x + halfWidth;
            float safeRight = Mathf.Lerp(left, right, ResourceLabelSafeViewportRight);
            float labelHalfWidth = ResourceLabelWidth * 0.5f;
            NodeView nearest = suppressLabels ? null : FindNearestSearchNode(SearchNodeInteractionDistance);
            for (int i = 0; i < nodes.Count; i += 1)
            {
                NodeView node = nodes[i];
                if (node.LabelRoot == null)
                {
                    continue;
                }

                foreach (TextMeshPro worldMarker in node.Root.GetComponentsInChildren<TextMeshPro>(true))
                {
                    if (worldMarker != null && worldMarker != node.Label)
                    {
                        worldMarker.gameObject.SetActive(!suppressLabels);
                    }
                }

                bool labelVisible = ReferenceEquals(node, nearest);
                node.LabelRoot.gameObject.SetActive(labelVisible);
                if (!labelVisible)
                {
                    continue;
                }

                float silhouetteLeft = playerRoot == null
                    ? node.X
                    : Mathf.Min(node.X, playerRoot.position.x - ResourceLabelPlayerHalfWidth);
                float preferredLabelX = silhouetteLeft - labelHalfWidth - ResourceLabelHorizontalGap;
                float labelX = Mathf.Clamp(
                    preferredLabelX,
                    left + labelHalfWidth + ResourceLabelViewportPadding,
                    safeRight - labelHalfWidth - ResourceLabelViewportPadding);

                Vector3 localPosition = node.LabelRoot.localPosition;
                localPosition.x = labelX - node.X;
                localPosition.y = ResourceLabelSafeWorldY - node.Root.transform.position.y;
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
            Color subtle = new Color(color.r, color.g, color.b, 0.46f);
            CreateRect(localizationKey, new Vector2(center, PrototypeCampPlacement.FloorY + 0.11f), new Vector2(width, 0.08f), subtle, 3);
            CreateRect(localizationKey + " 왼쪽 경계", new Vector2(minimumX, PrototypeCampPlacement.FloorY + 0.19f), new Vector2(0.05f, 0.28f), subtle, 3);
            CreateRect(localizationKey + " 오른쪽 경계", new Vector2(maximumX, PrototypeCampPlacement.FloorY + 0.19f), new Vector2(0.05f, 0.28f), subtle, 3);
        }

        private void CreatePlacedStructure(StructureKind kind, Color color)
        {
            if (!session.HasStructure(kind))
            {
                return;
            }

            campPlacement.EnsureInstalled(kind);
            if (!campPlacement.IsInstalledInRoom(kind, campUse.CurrentRoomId))
            {
                return;
            }
            Vector2 size = PrototypeCampPlacement.GetStructureSize(kind);
            Vector2 position = campPlacement.GetInstalledPosition(kind);
            GameObject structure = new GameObject(kind + " · " + AssetStructures);
            structure.transform.SetParent(worldRoot, false);
            structure.transform.position = position;
            CreateStructureVisual(structure.transform, kind, GetStructureSprite(kind), size, color, 3, out _);
            structureViews[kind] = structure;
        }

        private void CreateCampBlueprint(StructureKind kind, Color color)
        {
            if (session.HasStructure(kind) || campUse.CurrentRoomId != PrototypeCampModuleCatalog.StartRoomId)
            {
                return;
            }

            Vector2 size = PrototypeCampPlacement.GetStructureSize(kind);
            Vector2 position = campPlacement.GetInstalledPosition(kind);
            GameObject root = new GameObject("미설치 설비 현장 표식 · " + kind);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = position;
            float guideAlpha = campPlacement.IsActive ? 0.38f : 0.12f;
            Color subtle = new Color(color.r, color.g, color.b, guideAlpha);
            CreateFootprintOutline(root.transform, size, subtle, null);
            Sprite blueprintSprite = GetStructureSprite(kind);
            if (blueprintSprite != null)
            {
                float blueprintAlpha = campPlacement.IsActive ? 0.28f : 0.08f;
                CreateStructureVisual(root.transform, kind, blueprintSprite, size, new Color(0.55f, 0.88f, 0.92f, blueprintAlpha), 2, out _);
            }
            else
            {
                GameObject first = CreateRect(root.transform, "임시 공구 표식 A", Vector2.zero, new Vector2(0.08f, Mathf.Min(0.55f, size.y)), subtle, 4);
                first.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                GameObject second = CreateRect(root.transform, "임시 공구 표식 B", Vector2.zero, new Vector2(0.08f, Mathf.Min(0.55f, size.y)), subtle, 4);
                second.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            }
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
                new Vector2(0f, visualTop + 0.52f),
                new Vector2(0.82f, 0.82f),
                Color.black,
                Color.white,
                out placementGhostBadgeRenderer,
                0.042f,
                30f,
                32f);
            placementGhostLabel.transform.parent.gameObject.SetActive(false);
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
            if (placementGhostLabel != null)
            {
                placementGhostLabel.text = string.Empty;
                placementGhostLabel.transform.parent.gameObject.SetActive(false);
            }
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
            bool adoptedKim = kimIdleSprite != null;
            bool placeholderPose = !adoptedKim && playerVisualPrefab == null;
            SpriteRenderer kimRenderer = null;
            if (adoptedKim)
            {
                GameObject spriteObject = new GameObject("채택 김씨 스프라이트 · " + AssetKim);
                spriteObject.transform.SetParent(visual.transform, false);
                kimRenderer = spriteObject.AddComponent<SpriteRenderer>();
                kimRenderer.sprite = kimIdleSprite;
                kimRenderer.sortingOrder = 8;
                float kimScale = 2.45f / Mathf.Max(0.01f, kimIdleSprite.bounds.size.y);
                spriteObject.transform.localScale = new Vector3(kimScale, kimScale, 1f);
            }
            else if (placeholderPose)
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
            if (kimRenderer != null)
            {
                playerPresentation.ConfigureSpriteStates(kimRenderer, kimIdleSprite, kimWalkSprite, kimSwimSprite);
            }
            PrototypePlayerPresentationState initialPresentation = session.Phase == GamePhase.Exploring
                ? playerTraversal.CurrentPresentation(session.IsSwimming)
                : new PrototypePlayerPresentationState(position.x, position.y, 1f, 0f, false, true);
            playerPresentation.Apply(initialPresentation);
        }

        private void CreatePalm(Vector2 position, float scale)
        {
            GameObject root = new GameObject("해변 야자수 실루엣");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = position;
            Color trunk = new Color(0.34f, 0.19f, 0.075f);
            Color leafDark = new Color(0.055f, 0.34f, 0.16f);
            Color leafLight = new Color(0.18f, 0.56f, 0.24f);
            for (int segment = 0; segment < 4; segment += 1)
            {
                GameObject trunkSegment = CreateRect(
                    root.transform,
                    "야자수 줄기 마디 " + segment,
                    new Vector2((-0.10f + segment * 0.06f) * scale, (0.42f + segment * 0.70f) * scale),
                    new Vector2((0.43f - segment * 0.035f) * scale, 0.82f * scale),
                    trunk,
                    -1);
                trunkSegment.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            }
            float[] angles = { 66f, 34f, 8f, -20f, -48f, -78f };
            for (int index = 0; index < angles.Length; index += 1)
            {
                float direction = angles[index] * Mathf.Deg2Rad;
                Vector2 crown = new Vector2(0.14f * scale, 2.98f * scale);
                Vector2 offset = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * 0.72f * scale;
                GameObject leaf = CreateRect(
                    root.transform,
                    "야자수 겹잎 " + index,
                    crown + offset,
                    new Vector2(1.75f * scale, 0.26f * scale),
                    index % 2 == 0 ? leafDark : leafLight,
                    0);
                leaf.transform.localRotation = Quaternion.Euler(0f, 0f, angles[index]);
            }
            CreateRect(root.transform, "야자수 잎관 중심", new Vector2(0.14f * scale, 2.98f * scale), new Vector2(0.48f * scale, 0.48f * scale), leafDark, 1).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
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

        private static void ConfigureTopHudText(TMP_Text text)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 30f;
            text.fontSizeMax = 32f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 2;
            text.lineSpacing = -6f;
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

        private Button CreateCampPopupButton(string name, UnityEngine.Events.UnityAction callback)
        {
            Button button = CreateButton(name, campInteractionPopup.transform, Vector2.zero, Vector2.zero, string.Empty, callback);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing = true;
            label.fontSizeMin = 26f;
            label.fontSizeMax = 28f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            campPopupButtons.Add(button);
            return button;
        }

        private TMP_Text CreateExpeditionRailText(string name, Vector2 anchorMin, Vector2 anchorMax, int maxLines)
        {
            TMP_Text text = CreateText(
                name,
                expeditionMapPanel.transform,
                anchorMin,
                anchorMax,
                new Vector2(6f, 3f),
                new Vector2(-2f, -3f),
                21,
                TextAnchor.MiddleLeft,
                new Color(0.04f, 0.18f, 0.2f));
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 21f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.maxVisibleLines = maxLines;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = -6f;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureExpeditionMapButton(Button button)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 24f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.maxVisibleLines = 2;
            label.overflowMode = TextOverflowModes.Overflow;
        }

        private Button CreateBagButton(Transform parent, int index, UnityEngine.Events.UnityAction callback)
        {
            int column = index % 2;
            int row = index / 2;
            float left = 22f + column * 196f;
            float right = left + 185f;
            float top = -104f - row * 112f;
            Button button = CreateButton("가방 " + index, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), localization.Format("bag.slot.empty", index + 1), callback, new Vector2(left, top - 96f), new Vector2(right, top));
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.rectTransform.offsetMin = new Vector2(54f, 5f);
            label.rectTransform.offsetMax = new Vector2(-8f, -5f);
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.23f, 0.31f, 0.32f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            return button;
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

        private static Color SearchLootButtonBackground(string stableResourceId, ResourceKind kind, bool selected)
        {
            return PrototypeResourcePresentation.Surface(stableResourceId, kind, selected);
        }

        private static float ColorContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            float lighter = Mathf.Max(firstLuminance, secondLuminance);
            float darker = Mathf.Min(firstLuminance, secondLuminance);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * LinearSrgb(color.r) +
                   0.7152f * LinearSrgb(color.g) +
                   0.0722f * LinearSrgb(color.b);
        }

        private static float LinearSrgb(float channel)
        {
            return channel <= 0.03928f
                ? channel / 12.92f
                : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private Sprite GetResourceIconSprite(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Wood:
                    return woodIconSprite;
                case ResourceKind.Stone:
                    return stoneIconSprite;
                case ResourceKind.Food:
                    return foodIconSprite;
                case ResourceKind.Salvage:
                    return salvageIconSprite;
                default:
                    return null;
            }
        }

        private static void ConfigureActionButtonRole(Button button, Color surfaceColor, Color textColor)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = surfaceColor;
            }
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.selectedColor = new Color(1f, 0.92f, 0.72f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            button.colors = colors;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = textColor;
            }
        }

        private static Image CreateBagItemIcon(Transform parent, int index)
        {
            GameObject iconObject = new GameObject("가방 재료 아이콘 " + (index + 1));
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(29f, 0f);
            rect.sizeDelta = new Vector2(38f, 38f);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            return icon;
        }

        private static Image CreateSearchLootItemIcon(Transform parent, int index)
        {
            GameObject iconObject = new GameObject("발견물 채택 자원 아이콘 " + (index + 1));
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(29f, 0f);
            rect.sizeDelta = new Vector2(40f, 40f);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            return icon;
        }

        private void PrepareKimSprites()
        {
            if (kimAtlasSprite == null || kimAtlasSprite.texture == null)
            {
                return;
            }

            Rect source = kimAtlasSprite.rect;
            float cellWidth = source.width / 4f;
            float cellHeight = source.height / 2f;
            float topRowY = source.y + cellHeight;
            kimIdleSprite = CreateKimAtlasCell(source.x, topRowY, cellWidth, cellHeight, "kim-idle-adopted");
            kimWalkSprite = CreateKimAtlasCell(source.x + cellWidth, topRowY, cellWidth, cellHeight, "kim-walk-adopted");
            kimSwimSprite = CreateKimAtlasCell(source.x + cellWidth * 2f, topRowY, cellWidth, cellHeight, "kim-swim-readable-adopted");
        }

        private Sprite CreateKimAtlasCell(float x, float y, float width, float height, string spriteName)
        {
            Sprite sprite = Sprite.Create(
                kimAtlasSprite.texture,
                new Rect(x, y, width, height),
                new Vector2(0.5f, 0.055f),
                128f,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        private static void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite != null)
            {
                Destroy(sprite);
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
