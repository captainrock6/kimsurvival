using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private enum SubmissionShellState
        {
            Title,
            Opening,
            Help,
            Credits,
            Playing
        }

        private static readonly Color O9Ink = new Color(0.035f, 0.075f, 0.078f, 0.98f);
        private static readonly Color O9Paper = new Color(0.91f, 0.84f, 0.66f, 1f);
        private static readonly Color O9Amber = new Color(0.96f, 0.61f, 0.18f, 1f);
        private static readonly Color O9Teal = new Color(0.11f, 0.52f, 0.54f, 1f);

        private SubmissionShellState submissionShellState;
        private GameObject submissionShellRoot;
        private GameObject submissionTitlePage;
        private GameObject submissionOpeningPage;
        private GameObject submissionInfoPage;
        private TMP_Text submissionTitleText;
        private TMP_Text submissionTaglineText;
        private TMP_Text submissionLanguageText;
        private TMP_Text openingChapterText;
        private TMP_Text openingBodyText;
        private TMP_Text openingCounterText;
        private TMP_Text infoTitleText;
        private TMP_Text infoBodyText;
        private TMP_Text firstObjectiveText;
        private GameObject firstObjectiveRoot;
        private Image openingIllustrationSurface;
        private Image openingKimImage;
        private Image[] openingIllustrationParts = Array.Empty<Image>();
        private Button submissionStartButton;
        private Button submissionHelpButton;
        private Button submissionCreditsButton;
        private Button submissionExitButton;
        private Button openingPreviousButton;
        private Button openingNextButton;
        private Button openingSkipButton;
        private Button submissionBackButton;
        private int openingBeatIndex;
        private PrototypeGameJamAudio submissionAudio;
        private GamePhase lastAudioPhase = (GamePhase)(-1);
        private readonly Dictionary<string, Sprite> o10ItemIcons = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly List<Texture2D> o10ItemIconTextures = new List<Texture2D>();

        private void BuildO9O10Presentation()
        {
            ApplyO9RuntimeSkin();
            BuildO9FirstObjective();
            AttachO10ToolIcon(researchAxeButton, "tool.stone-axe", ResourceKind.Stone);
            AttachO10ToolIcon(craftAxeButton, "tool.stone-axe", ResourceKind.Stone);
            AttachO10ToolIcon(researchRopeButton, "tool.rope", ResourceKind.Wood);
            AttachO10ToolIcon(craftRopeButton, "tool.rope", ResourceKind.Wood);
            BuildO9SubmissionShell();

            submissionAudio = gameObject.AddComponent<PrototypeGameJamAudio>();
            submissionAudio.Initialize(worldCamera);
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                Button captured = button;
                captured.onClick.AddListener(delegate
                {
                    if (submissionAudio != null) submissionAudio.Play(PrototypeGameJamCue.UiConfirm);
                });
            }

            bool skipShell = Application.isBatchMode || Environment.GetCommandLineArgs()
                .Any(value => string.Equals(value, "-kim-survival-skip-opening", StringComparison.OrdinalIgnoreCase));
            submissionShellState = skipShell ? SubmissionShellState.Playing : SubmissionShellState.Title;
            RefreshO9O10Presentation();
        }

        private void ApplyO9RuntimeSkin()
        {
            RectTransform top = statusText == null ? null : statusText.transform.parent as RectTransform;
            if (top != null)
            {
                top.anchorMin = new Vector2(0.018f, 0.89f);
                top.anchorMax = new Vector2(0.982f, 0.985f);
                top.offsetMin = Vector2.zero;
                top.offsetMax = Vector2.zero;
                Image surface = top.GetComponent<Image>();
                if (surface != null) surface.color = O9Ink;
                VerticalLayoutGroup vertical = top.GetComponent<VerticalLayoutGroup>();
                if (vertical != null)
                {
                    vertical.enabled = true;
                    vertical.padding = new RectOffset(34, 34, 3, 3);
                    vertical.spacing = 0f;
                    vertical.childControlHeight = true;
                    vertical.childForceExpandHeight = false;
                }
                LayoutElement statusLayout = statusText.GetComponent<LayoutElement>();
                LayoutElement resourceLayout = resourceText.GetComponent<LayoutElement>();
                if (statusLayout != null)
                {
                    statusLayout.ignoreLayout = false;
                    statusLayout.minHeight = 44f;
                    statusLayout.preferredHeight = 44f;
                    statusLayout.flexibleHeight = 0f;
                }
                if (resourceLayout != null)
                {
                    resourceLayout.ignoreLayout = false;
                    resourceLayout.minHeight = 50f;
                    resourceLayout.preferredHeight = 50f;
                    resourceLayout.flexibleHeight = 0f;
                }
                statusText.alignment = TextAlignmentOptions.MidlineLeft;
                resourceText.alignment = TextAlignmentOptions.MidlineRight;
                statusText.textWrappingMode = TextWrappingModes.Normal;
                statusText.maxVisibleLines = 2;
                resourceText.textWrappingMode = TextWrappingModes.Normal;
                resourceText.maxVisibleLines = 2;
                statusText.fontSizeMax = 28f;
                statusText.fontSizeMin = 22f;
                resourceText.fontSizeMax = 23f;
                resourceText.fontSizeMin = 18f;
            }

            RectTransform message = messageText == null ? null : messageText.transform.parent as RectTransform;
            if (message != null)
            {
                message.anchorMin = new Vector2(0.52f, 0.74f);
                message.anchorMax = new Vector2(0.975f, 0.825f);
                message.offsetMin = Vector2.zero;
                message.offsetMax = Vector2.zero;
                messageText.fontSize = 20f;
                messageText.fontSizeMax = 20f;
                messageText.fontSizeMin = 15f;
            }

            RectTransform controls = controlsText == null ? null : controlsText.transform.parent as RectTransform;
            if (controls != null)
            {
                controls.anchorMin = new Vector2(0.018f, 0.015f);
                controls.anchorMax = new Vector2(0.66f, 0.087f);
                controls.offsetMin = Vector2.zero;
                controls.offsetMax = Vector2.zero;
                Image surface = controls.GetComponent<Image>();
                if (surface != null) surface.color = new Color(O9Ink.r, O9Ink.g, O9Ink.b, 0.92f);
                HorizontalLayoutGroup layout = controls.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.padding = new RectOffset(22, 22, 3, 3);
                    layout.spacing = 8f;
                }
                controlsText.fontSizeMax = 18f;
                controlsText.fontSizeMin = 13f;
            }

            if (campInteractionPopupFrameImage != null)
            {
                campInteractionPopupFrameImage.color = new Color(O9Ink.r, O9Ink.g, O9Ink.b, 0.97f);
            }

            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                ApplyO9ButtonSkin(button);
            }
        }

        private static void ApplyO9ButtonSkin(Button button)
        {
            if (button == null) return;
            Image surface = button.GetComponent<Image>();
            if (surface != null && surface.sprite == null)
            {
                surface.color = new Color(0.10f, 0.22f, 0.20f, 0.98f);
            }
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.95f, 0.91f, 1f);
            colors.selectedColor = O9Teal;
            colors.pressedColor = O9Amber;
            colors.disabledColor = new Color(0.34f, 0.36f, 0.34f, 0.66f);
            button.colors = colors;
        }

        private void BuildO9FirstObjective()
        {
            firstObjectiveRoot = CreatePanel(
                "O9 First Loop Objective",
                canvas.transform,
                new Vector2(0.025f, 0.74f),
                new Vector2(0.49f, 0.825f),
                Vector2.zero,
                Vector2.zero,
                new Color(O9Ink.r, O9Ink.g, O9Ink.b, 0.91f)).gameObject;
            Image image = firstObjectiveRoot.GetComponent<Image>();
            Outline outline = firstObjectiveRoot.AddComponent<Outline>();
            outline.effectColor = new Color(O9Amber.r, O9Amber.g, O9Amber.b, 0.82f);
            outline.effectDistance = new Vector2(2f, -2f);
            if (image != null) image.raycastTarget = false;
            firstObjectiveText = CreateText(
                "First Loop Copy",
                firstObjectiveRoot.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(22f, 8f),
                new Vector2(-22f, -8f),
                20,
                TextAnchor.MiddleLeft,
                Color.white);
            firstObjectiveText.enableAutoSizing = true;
            firstObjectiveText.fontSizeMin = 14f;
            firstObjectiveText.fontSizeMax = 20f;
            firstObjectiveText.maxVisibleLines = 2;
            firstObjectiveText.raycastTarget = false;
        }

        private void BuildO9SubmissionShell()
        {
            submissionShellRoot = CreatePanel(
                "O9 Submission Shell",
                canvas.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.025f, 0.065f, 0.07f, 1f)).gameObject;
            submissionShellRoot.transform.SetAsLastSibling();

            submissionTitlePage = CreatePanel(
                "Title Page",
                submissionShellRoot.transform,
                new Vector2(0.045f, 0.07f),
                new Vector2(0.955f, 0.93f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.06f, 0.15f, 0.15f, 1f)).gameObject;
            BuildO9TitleIllustration(submissionTitlePage.transform);
            submissionTitleText = CreateText(
                "Game Title",
                submissionTitlePage.transform,
                new Vector2(0.055f, 0.56f),
                new Vector2(0.56f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                76,
                TextAnchor.LowerLeft,
                O9Paper);
            submissionTitleText.fontStyle = FontStyles.Bold;
            submissionTitleText.enableAutoSizing = true;
            submissionTitleText.fontSizeMin = 46f;
            submissionTitleText.fontSizeMax = 76f;
            submissionTaglineText = CreateText(
                "Game Tagline",
                submissionTitlePage.transform,
                new Vector2(0.06f, 0.42f),
                new Vector2(0.55f, 0.56f),
                Vector2.zero,
                Vector2.zero,
                25,
                TextAnchor.UpperLeft,
                Color.white);
            submissionTaglineText.enableAutoSizing = true;
            submissionTaglineText.fontSizeMin = 18f;
            submissionTaglineText.fontSizeMax = 25f;
            submissionStartButton = CreateO9ShellButton("Start", submissionTitlePage.transform, new Vector2(0.06f, 0.28f), new Vector2(0.29f, 0.37f), BeginO9Opening);
            submissionHelpButton = CreateO9ShellButton("How To Play", submissionTitlePage.transform, new Vector2(0.31f, 0.28f), new Vector2(0.54f, 0.37f), delegate { OpenO9Info(false); });
            submissionCreditsButton = CreateO9ShellButton("Credits", submissionTitlePage.transform, new Vector2(0.06f, 0.17f), new Vector2(0.29f, 0.26f), delegate { OpenO9Info(true); });
            Button language = CreateO9ShellButton("Language", submissionTitlePage.transform, new Vector2(0.31f, 0.17f), new Vector2(0.54f, 0.26f), delegate { localization.CycleLocale(); });
            submissionLanguageText = language.GetComponentInChildren<TMP_Text>();
            submissionExitButton = CreateO9ShellButton("Exit", submissionTitlePage.transform, new Vector2(0.06f, 0.06f), new Vector2(0.29f, 0.15f), QuitO9Submission);

            submissionOpeningPage = CreatePanel(
                "Opening Comic",
                submissionShellRoot.transform,
                new Vector2(0.055f, 0.055f),
                new Vector2(0.945f, 0.945f),
                Vector2.zero,
                Vector2.zero,
                O9Paper).gameObject;
            openingIllustrationSurface = CreatePanel(
                "Opening Illustration",
                submissionOpeningPage.transform,
                new Vector2(0.045f, 0.34f),
                new Vector2(0.955f, 0.94f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.16f, 0.42f, 0.49f, 1f)).GetComponent<Image>();
            BuildO9OpeningInk(openingIllustrationSurface.transform);
            openingKimImage = CreateO9SpriteImage(
                openingIllustrationSurface.transform,
                "Opening Kim Ink",
                new Vector2(0.43f, 0.18f),
                new Vector2(0.59f, 0.62f),
                kimIdleSprite);
            openingChapterText = CreateText("Opening Chapter", submissionOpeningPage.transform, new Vector2(0.055f, 0.23f), new Vector2(0.43f, 0.32f), Vector2.zero, Vector2.zero, 31, TextAnchor.MiddleLeft, O9Ink);
            openingChapterText.fontStyle = FontStyles.Bold;
            openingBodyText = CreateText("Opening Body", submissionOpeningPage.transform, new Vector2(0.055f, 0.08f), new Vector2(0.70f, 0.23f), Vector2.zero, Vector2.zero, 25, TextAnchor.UpperLeft, O9Ink);
            openingBodyText.enableAutoSizing = true;
            openingBodyText.fontSizeMin = 18f;
            openingBodyText.fontSizeMax = 25f;
            openingCounterText = CreateText("Opening Counter", submissionOpeningPage.transform, new Vector2(0.78f, 0.24f), new Vector2(0.94f, 0.31f), Vector2.zero, Vector2.zero, 24, TextAnchor.MiddleRight, O9Ink);
            openingPreviousButton = CreateO9ShellButton("Previous", submissionOpeningPage.transform, new Vector2(0.72f, 0.10f), new Vector2(0.82f, 0.19f), PreviousO9Opening);
            openingNextButton = CreateO9ShellButton("Next", submissionOpeningPage.transform, new Vector2(0.83f, 0.10f), new Vector2(0.94f, 0.19f), NextO9Opening);
            openingSkipButton = CreateO9ShellButton("Skip", submissionOpeningPage.transform, new Vector2(0.72f, 0.02f), new Vector2(0.94f, 0.085f), FinishO9Opening);

            submissionInfoPage = CreatePanel(
                "Submission Information",
                submissionShellRoot.transform,
                new Vector2(0.18f, 0.16f),
                new Vector2(0.82f, 0.84f),
                Vector2.zero,
                Vector2.zero,
                O9Paper).gameObject;
            infoTitleText = CreateText("Info Title", submissionInfoPage.transform, new Vector2(0.07f, 0.76f), new Vector2(0.93f, 0.91f), Vector2.zero, Vector2.zero, 42, TextAnchor.MiddleLeft, O9Ink);
            infoTitleText.fontStyle = FontStyles.Bold;
            infoBodyText = CreateText("Info Body", submissionInfoPage.transform, new Vector2(0.07f, 0.22f), new Vector2(0.93f, 0.74f), Vector2.zero, Vector2.zero, 25, TextAnchor.UpperLeft, O9Ink);
            infoBodyText.enableAutoSizing = true;
            infoBodyText.fontSizeMin = 17f;
            infoBodyText.fontSizeMax = 25f;
            submissionBackButton = CreateO9ShellButton("Back", submissionInfoPage.transform, new Vector2(0.64f, 0.07f), new Vector2(0.92f, 0.18f), ReturnToO9Title);
        }

        private void BuildO9TitleIllustration(Transform parent)
        {
            RectTransform art = CreatePanel("Title Storm Drift", parent, new Vector2(0.59f, 0.06f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.40f, 0.48f, 1f));
            CreateO9Shape(art, "Storm Cloud", new Vector2(0.05f, 0.72f), new Vector2(0.94f, 0.93f), new Color(0.035f, 0.07f, 0.09f, 1f), 0f);
            CreateO9Shape(art, "Rain A", new Vector2(0.14f, 0.38f), new Vector2(0.18f, 0.74f), new Color(0.74f, 0.91f, 0.93f, 0.76f), -15f);
            CreateO9Shape(art, "Rain B", new Vector2(0.42f, 0.42f), new Vector2(0.46f, 0.79f), new Color(0.74f, 0.91f, 0.93f, 0.76f), -15f);
            CreateO9Shape(art, "Rain C", new Vector2(0.72f, 0.34f), new Vector2(0.76f, 0.72f), new Color(0.74f, 0.91f, 0.93f, 0.76f), -15f);
            CreateO9Shape(art, "Wave", new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.35f), O9Teal, -5f);
            CreateO9Shape(art, "Broken Boat", new Vector2(0.33f, 0.24f), new Vector2(0.77f, 0.34f), new Color(0.27f, 0.15f, 0.075f, 1f), 8f);
            CreateO9SpriteImage(art, "Title Kim Ink", new Vector2(0.42f, 0.22f), new Vector2(0.67f, 0.64f), kimHurtSprite != null ? kimHurtSprite : kimIdleSprite);
        }

        private void BuildO9OpeningInk(Transform parent)
        {
            openingIllustrationParts = new Image[10];
            for (int index = 0; index < openingIllustrationParts.Length; index += 1)
            {
                openingIllustrationParts[index] = CreateO9Shape(parent, "Ink Part " + index, Vector2.zero, Vector2.one, O9Ink, 0f);
            }
        }

        private static Image CreateO9Shape(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, float rotation)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            Image image = root.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateO9SpriteImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = root.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Button CreateO9ShellButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction callback)
        {
            Button button = CreateButton(name, parent, anchorMin, anchorMax, string.Empty, callback);
            ApplyO9ButtonSkin(button);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = 23f;
            label.maxVisibleLines = 2;
            return button;
        }

        private bool UpdateO9O10Presentation()
        {
            if (submissionShellState == SubmissionShellState.Playing) return false;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (submissionShellState == SubmissionShellState.Title) QuitO9Submission();
                else ReturnToO9Title();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (submissionShellState == SubmissionShellState.Title) BeginO9Opening();
                else if (submissionShellState == SubmissionShellState.Opening) NextO9Opening();
            }
            return true;
        }

        private void BeginO9Opening()
        {
            RestartSession();
            openingBeatIndex = 0;
            submissionShellState = SubmissionShellState.Opening;
            RefreshO9O10Presentation();
        }

        private void PreviousO9Opening()
        {
            openingBeatIndex = Mathf.Max(0, openingBeatIndex - 1);
            RefreshO9O10Presentation();
        }

        private void NextO9Opening()
        {
            if (openingBeatIndex >= PrototypeGameJamNarrative.Opening.Length - 1)
            {
                FinishO9Opening();
                return;
            }
            openingBeatIndex += 1;
            RefreshO9O10Presentation();
        }

        private void FinishO9Opening()
        {
            submissionShellState = SubmissionShellState.Playing;
            RefreshO9O10Presentation();
            if (submissionAudio != null) submissionAudio.Play(PrototypeGameJamCue.StoryAdvance);
        }

        private void OpenO9Info(bool credits)
        {
            submissionShellState = credits ? SubmissionShellState.Credits : SubmissionShellState.Help;
            RefreshO9O10Presentation();
        }

        private void ReturnToO9Title()
        {
            submissionShellState = SubmissionShellState.Title;
            RefreshO9O10Presentation();
        }

        private static void QuitO9Submission()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshO9O10Presentation()
        {
            if (submissionShellRoot == null || localization == null || session == null) return;
            bool Korean = string.Equals(localization.CurrentLocaleCode, PrototypeLocalization.KoreanLocaleCode, StringComparison.Ordinal);
            submissionShellRoot.SetActive(submissionShellState != SubmissionShellState.Playing);
            submissionTitlePage.SetActive(submissionShellState == SubmissionShellState.Title);
            submissionOpeningPage.SetActive(submissionShellState == SubmissionShellState.Opening);
            submissionInfoPage.SetActive(submissionShellState == SubmissionShellState.Help || submissionShellState == SubmissionShellState.Credits);

            submissionTitleText.text = Korean ? "김씨 생존기\n<color=#F49B2E>무인도</color>" : "KIM'S SURVIVAL\n<color=#F49B2E>DESERT ISLAND</color>";
            submissionTaglineText.text = Korean
                ? "폭풍에 떠밀린 평범한 김씨의\n수색 · 건설 · 탈출 생존기"
                : "An ordinary man, one violent storm,\nand a search · build · escape loop.";
            submissionLanguageText.text = Korean ? "English" : "한국어";
            SetButton(submissionStartButton, Korean ? "새로 시작" : "NEW GAME", true);
            SetButton(submissionHelpButton, Korean ? "플레이 방법" : "HOW TO PLAY", true);
            SetButton(submissionCreditsButton, Korean ? "제작 정보" : "CREDITS", true);
            SetButton(submissionExitButton, Korean ? "종료" : "EXIT", true);
            SetButton(openingSkipButton, Korean ? "도입 건너뛰기" : "SKIP INTRO", true);
            SetButton(submissionBackButton, Korean ? "돌아가기" : "BACK", true);

            if (submissionShellState == SubmissionShellState.Opening)
            {
                ShowO9OpeningBeat();
            }
            else if (submissionShellState == SubmissionShellState.Help)
            {
                infoTitleText.text = Korean ? "생존 요령" : "HOW TO SURVIVE";
                infoBodyText.text = Korean
                    ? "A/D 이동 · E 상호작용 · 방향키/Enter 선택 · Esc 닫기\n\n1. 캠프의 지도대에서 수색 지역을 선택합니다.\n2. ◇ 표시가 있는 환경 오브젝트를 뒤져 필요한 물건만 가방에 넣습니다.\n3. 캠프로 돌아와 설비에 직접 다가가 연구·제작·건설합니다.\n4. 뗏목, 대형 연기 신호, 무전기 중 한 경로를 완성해 탈출합니다.\n\n김씨가 자주 한 행동은 마지막 코믹북 엔딩에 남습니다."
                    : "A/D move · E interact · Arrows/Enter choose · Esc close\n\n1. Choose a search region at the camp map.\n2. Search environmental objects marked ◇ and pack only what you need.\n3. Return and approach facilities to research, craft, and build.\n4. Complete the raft, smoke signal, or radio route.\n\nKim's repeated habits become part of the final comic ending.";
            }
            else if (submissionShellState == SubmissionShellState.Credits)
            {
                infoTitleText.text = Korean ? "제작 정보" : "CREDITS";
                infoBodyText.text = Korean
                    ? "《김씨 생존기: 무인도》 게임잼 프로토타입\n\n기획·디렉션: captainrock6\n엔진: Unity 2D\n제작 지원: OpenAI Codex + Forge\n\n모든 게임 내 콘텐츠는 오리지널 프로토타입 자산으로 구성됩니다."
                    : "KIM'S SURVIVAL: DESERT ISLAND · GAME JAM PROTOTYPE\n\nDesign & Direction: captainrock6\nEngine: Unity 2D\nProduction Support: OpenAI Codex + Forge\n\nAll in-game content uses original prototype assets.";
            }

            string objectiveId = CurrentO9ObjectiveId();
            firstObjectiveText.text = PrototypeGameJamNarrative.Objective(objectiveId).Resolve(localization.CurrentLocaleCode);
            bool modalOpen = campInteraction.IsPopupOpen || campPlacement.IsActive || campModuleExpansion.IsPreviewActive ||
                             (searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen);
            firstObjectiveRoot.SetActive(submissionShellState == SubmissionShellState.Playing &&
                                         session.Phase != GamePhase.Result && !modalOpen);
            submissionShellRoot.transform.SetAsLastSibling();
        }

        private void ShowO9OpeningBeat()
        {
            int index = Mathf.Clamp(openingBeatIndex, 0, PrototypeGameJamNarrative.Opening.Length - 1);
            PrototypeOpeningBeat beat = PrototypeGameJamNarrative.Opening[index];
            string locale = localization.CurrentLocaleCode;
            openingChapterText.text = beat.Chapter.Resolve(locale);
            openingBodyText.text = beat.Body.Resolve(locale);
            openingCounterText.text = (index + 1) + " / " + PrototypeGameJamNarrative.Opening.Length;
            SetButton(openingPreviousButton, string.Equals(locale, PrototypeLocalization.KoreanLocaleCode, StringComparison.Ordinal) ? "이전" : "BACK", index > 0);
            SetButton(openingNextButton, string.Equals(locale, PrototypeLocalization.KoreanLocaleCode, StringComparison.Ordinal)
                ? (index == PrototypeGameJamNarrative.Opening.Length - 1 ? "해변에서 시작" : "다음")
                : (index == PrototypeGameJamNarrative.Opening.Length - 1 ? "WAKE ON SHORE" : "NEXT"), true);
            ApplyO9OpeningIllustration(index);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(openingNextButton.gameObject);
        }

        private void ApplyO9OpeningIllustration(int beat)
        {
            if (openingIllustrationParts.Length < 10) return;
            Color[] skies =
            {
                new Color(0.46f, 0.72f, 0.78f, 1f),
                new Color(0.08f, 0.16f, 0.21f, 1f),
                new Color(0.06f, 0.24f, 0.31f, 1f),
                new Color(0.60f, 0.82f, 0.82f, 1f),
                new Color(0.88f, 0.68f, 0.36f, 1f)
            };
            openingIllustrationSurface.color = skies[Mathf.Clamp(beat, 0, skies.Length - 1)];
            openingKimImage.sprite = beat == 1 ? kimHurtSprite
                : beat == 2 ? kimSwimSprite
                : beat == 3 ? kimRestSprite
                : beat == 4 ? kimFacilityUseSprite
                : kimIdleSprite;
            openingKimImage.enabled = openingKimImage.sprite != null;
            RectTransform kimRect = openingKimImage.rectTransform;
            kimRect.anchorMin = beat == 2 ? new Vector2(0.42f, 0.24f) : new Vector2(0.43f, 0.16f);
            kimRect.anchorMax = beat == 2 ? new Vector2(0.60f, 0.58f) : new Vector2(0.59f, 0.64f);
            kimRect.offsetMin = Vector2.zero;
            kimRect.offsetMax = Vector2.zero;
            for (int i = 0; i < openingIllustrationParts.Length; i += 1)
            {
                openingIllustrationParts[i].gameObject.SetActive(false);
            }

            if (beat == 0)
            {
                SetOpeningPart(0, new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.24f), O9Teal, -2f);
                SetOpeningPart(1, new Vector2(0.30f, 0.30f), new Vector2(0.72f, 0.41f), new Color(0.28f, 0.16f, 0.08f, 1f), 2f);
                SetOpeningPart(2, new Vector2(0.47f, 0.41f), new Vector2(0.51f, 0.72f), O9Ink, -4f);
                SetOpeningPart(3, new Vector2(0.51f, 0.51f), new Vector2(0.69f, 0.69f), O9Paper, -10f);
                SetOpeningPart(4, new Vector2(0.66f, 0.68f), new Vector2(0.94f, 0.88f), O9Ink, 0f);
            }
            else if (beat == 1)
            {
                SetOpeningPart(0, new Vector2(0.03f, 0.69f), new Vector2(0.97f, 0.96f), O9Ink, 0f);
                SetOpeningPart(1, new Vector2(0.03f, 0.08f), new Vector2(0.96f, 0.38f), O9Teal, -6f);
                SetOpeningPart(2, new Vector2(0.35f, 0.26f), new Vector2(0.74f, 0.36f), new Color(0.27f, 0.14f, 0.07f, 1f), 18f);
                SetOpeningPart(3, new Vector2(0.18f, 0.33f), new Vector2(0.23f, 0.82f), O9Paper, -18f);
                SetOpeningPart(4, new Vector2(0.48f, 0.30f), new Vector2(0.53f, 0.80f), O9Paper, -18f);
                SetOpeningPart(5, new Vector2(0.78f, 0.24f), new Vector2(0.83f, 0.73f), O9Paper, -18f);
            }
            else if (beat == 2)
            {
                SetOpeningPart(0, new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.35f), new Color(0.07f, 0.45f, 0.56f, 1f), -2f);
                SetOpeningPart(3, new Vector2(0.12f, 0.46f), new Vector2(0.34f, 0.50f), O9Paper, 6f);
                SetOpeningPart(4, new Vector2(0.66f, 0.55f), new Vector2(0.91f, 0.59f), O9Paper, -7f);
            }
            else if (beat == 3)
            {
                SetOpeningPart(0, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.27f), new Color(0.76f, 0.61f, 0.34f, 1f), 0f);
                SetOpeningPart(3, new Vector2(0.77f, 0.24f), new Vector2(0.81f, 0.78f), new Color(0.26f, 0.15f, 0.07f, 1f), -5f);
                SetOpeningPart(4, new Vector2(0.63f, 0.65f), new Vector2(0.94f, 0.73f), new Color(0.08f, 0.34f, 0.18f, 1f), -12f);
                SetOpeningPart(5, new Vector2(0.69f, 0.70f), new Vector2(0.93f, 0.78f), new Color(0.10f, 0.42f, 0.22f, 1f), 14f);
            }
            else
            {
                SetOpeningPart(0, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.18f), new Color(0.74f, 0.57f, 0.31f, 1f), 0f);
                SetOpeningPart(1, new Vector2(0.16f, 0.18f), new Vector2(0.40f, 0.48f), O9Ink, 0f);
                SetOpeningPart(2, new Vector2(0.55f, 0.18f), new Vector2(0.82f, 0.43f), new Color(0.31f, 0.18f, 0.08f, 1f), 0f);
                SetOpeningPart(5, new Vector2(0.23f, 0.53f), new Vector2(0.27f, 0.88f), new Color(0.23f, 0.13f, 0.065f, 1f), 0f);
                SetOpeningPart(6, new Vector2(0.12f, 0.75f), new Vector2(0.38f, 0.82f), new Color(0.08f, 0.36f, 0.18f, 1f), -13f);
            }
        }

        private void SetOpeningPart(int index, Vector2 minimum, Vector2 maximum, Color color, float rotation)
        {
            Image image = openingIllustrationParts[index];
            RectTransform rect = image.rectTransform;
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            image.color = color;
            image.gameObject.SetActive(true);
        }

        private string CurrentO9ObjectiveId()
        {
            if (session.Phase == GamePhase.Result) return "objective.result";
            if (session.Phase == GamePhase.Exploring)
            {
                if (searchNodeRuntime != null && searchNodeRuntime.IsTrayOpen) return "objective.search.choose";
                return Enumerable.Range(0, session.ActiveBagSlotCount).Any(index => session.GetBagSlot(index).Amount > 0)
                    ? "objective.search.return"
                    : "objective.search.inspect";
            }
            if (campInteraction.IsPopupOpen && campInteraction.OpenPopupKind == PrototypeCampInteractionTargetKind.ExpeditionMap)
            {
                return "objective.map.choose";
            }
            if (session.ExpeditionCompleted) return "objective.camp.invest";
            if (session.Day > 1) return "objective.escape";
            return "objective.camp.map";
        }

        private void PlayO9SearchCue(bool hurt)
        {
            if (submissionAudio == null) return;
            submissionAudio.Play(hurt ? PrototypeGameJamCue.Hazard : PrototypeGameJamCue.Search);
            if (playerPresentation != null)
            {
                playerPresentation.PlayAction(hurt ? PrototypePlayerActionPose.Hurt : PrototypePlayerActionPose.Search, 0.7f);
            }
        }

        private void PlayO9CampAction(string actionName, bool succeeded)
        {
            if (submissionAudio != null)
            {
                submissionAudio.Play(succeeded ? PrototypeGameJamCue.Craft : PrototypeGameJamCue.Reject);
            }
            if (playerPresentation == null) return;
            PrototypePlayerActionPose pose = !succeeded
                ? PrototypePlayerActionPose.Hurt
                : string.Equals(actionName, "survival.eat", StringComparison.Ordinal)
                    ? PrototypePlayerActionPose.Eat
                    : actionName.IndexOf("bed", StringComparison.Ordinal) >= 0 || actionName.IndexOf("sofa", StringComparison.Ordinal) >= 0
                        ? PrototypePlayerActionPose.Rest
                        : PrototypePlayerActionPose.FacilityUse;
            playerPresentation.PlayAction(pose, 0.75f);
        }

        private void RefreshO9AudioState()
        {
            if (submissionAudio == null || session == null) return;
            submissionAudio.SetGameplayActive(submissionShellState == SubmissionShellState.Playing && session.Phase != GamePhase.Result);
            if (lastAudioPhase != session.Phase && session.Phase == GamePhase.Result)
            {
                submissionAudio.Play(PrototypeGameJamCue.Ending);
            }
            lastAudioPhase = session.Phase;
        }

        private void DestroyO9O10Presentation()
        {
            if (submissionAudio != null)
            {
                submissionAudio.DisposeClips();
                submissionAudio = null;
            }
            foreach (Sprite sprite in o10ItemIcons.Values)
            {
                if (sprite != null) Destroy(sprite);
            }
            foreach (Texture2D texture in o10ItemIconTextures)
            {
                if (texture != null) Destroy(texture);
            }
            o10ItemIcons.Clear();
            o10ItemIconTextures.Clear();
        }

        private Sprite GetO10ItemIconSprite(string stableItemId, ResourceKind legacyKind, bool protectedPart)
        {
            string id = string.IsNullOrWhiteSpace(stableItemId)
                ? GameSession.StableResourceIdForLegacy(legacyKind)
                : stableItemId;
            if (o10ItemIcons.TryGetValue(id, out Sprite cached)) return cached;

            const int size = 48;
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 ink = new Color32(12, 32, 31, 255);
            Color accentColor = protectedPart
                ? O9Amber
                : PrototypeResourcePresentation.Accent(id, legacyKind);
            Color32 accent = accentColor;
            Color32 paper = new Color32(232, 216, 174, 255);
            Color32[] pixels = Enumerable.Repeat(clear, size * size).ToArray();
            PaintCircle(pixels, size, 24, 24, 20, ink);
            PaintCircle(pixels, size, 24, 24, 17, paper);

            int motif = StableMotif(id);
            if (protectedPart)
            {
                PaintRect(pixels, size, 12, 20, 36, 28, ink);
                PaintRect(pixels, size, 20, 12, 28, 36, ink);
                PaintCircle(pixels, size, 24, 24, 8, accent);
                PaintCircle(pixels, size, 24, 24, 3, ink);
                if ((motif & 1) == 0)
                {
                    PaintRect(pixels, size, 10, 10 + motif, 16, 16 + motif, accent);
                    PaintRect(pixels, size, 32, 32 - motif, 38, 38 - motif, accent);
                }
                else
                {
                    PaintRect(pixels, size, 10 + motif, 32, 16 + motif, 38, accent);
                    PaintRect(pixels, size, 32 - motif, 10, 38 - motif, 16, accent);
                }
            }
            else if (motif == 0)
            {
                PaintRect(pixels, size, 10, 19, 38, 28, ink);
                PaintRect(pixels, size, 12, 21, 36, 26, accent);
                PaintRect(pixels, size, 17, 14, 21, 33, ink);
                PaintRect(pixels, size, 28, 14, 32, 33, ink);
            }
            else if (motif == 1)
            {
                PaintCircle(pixels, size, 24, 24, 13, ink);
                PaintCircle(pixels, size, 24, 24, 10, accent);
                PaintRect(pixels, size, 22, 10, 26, 38, paper);
                PaintRect(pixels, size, 10, 22, 38, 26, paper);
            }
            else if (motif == 2)
            {
                PaintRect(pixels, size, 12, 12, 36, 36, ink);
                PaintRect(pixels, size, 16, 16, 32, 32, accent);
                for (int line = 0; line < 4; line += 1)
                {
                    PaintRect(pixels, size, 18 + line * 4, 8, 20 + line * 4, 13, ink);
                    PaintRect(pixels, size, 18 + line * 4, 35, 20 + line * 4, 40, ink);
                }
            }
            else if (motif == 3)
            {
                for (int stripe = 0; stripe < 4; stripe += 1)
                {
                    int y = 14 + stripe * 6;
                    PaintRect(pixels, size, 10 + stripe, y, 38 - stripe, y + 3, stripe % 2 == 0 ? ink : accent);
                }
            }
            else if (motif == 4)
            {
                PaintRect(pixels, size, 21, 9, 27, 38, ink);
                PaintRect(pixels, size, 12, 18, 36, 24, ink);
                PaintRect(pixels, size, 15, 20, 33, 22, accent);
                PaintCircle(pixels, size, 24, 31, 6, accent);
            }
            else
            {
                PaintCircle(pixels, size, 24, 27, 13, ink);
                PaintCircle(pixels, size, 24, 27, 10, accent);
                PaintRect(pixels, size, 21, 8, 27, 20, ink);
                PaintRect(pixels, size, 18, 8, 30, 12, accent);
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "O10 Item Icon · " + id,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = "O10 Item Icon · " + id;
            o10ItemIconTextures.Add(texture);
            o10ItemIcons.Add(id, sprite);
            return sprite;
        }

        private void AttachO10ToolIcon(Button button, string stableItemId, ResourceKind legacyKind)
        {
            if (button == null) return;
            GameObject root = new GameObject("O10 Tool Icon · " + stableItemId);
            root.transform.SetParent(button.transform, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(34f, 0f);
            rect.sizeDelta = new Vector2(42f, 42f);
            Image image = root.AddComponent<Image>();
            image.sprite = GetO10ItemIconSprite(stableItemId, legacyKind, false);
            image.preserveAspect = true;
            image.raycastTarget = false;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.rectTransform.offsetMin = new Vector2(62f, label.rectTransform.offsetMin.y);
        }

        private static int StableMotif(string stableId)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < stableId.Length; i += 1) hash = hash * 31 + stableId[i];
                return Math.Abs(hash % 6);
            }
        }

        private static void PaintRect(Color32[] pixels, int size, int xMin, int yMin, int xMax, int yMax, Color32 color)
        {
            xMin = Mathf.Clamp(xMin, 0, size - 1);
            xMax = Mathf.Clamp(xMax, 0, size);
            yMin = Mathf.Clamp(yMin, 0, size - 1);
            yMax = Mathf.Clamp(yMax, 0, size);
            for (int y = yMin; y < yMax; y += 1)
            {
                for (int x = xMin; x < xMax; x += 1) pixels[y * size + x] = color;
            }
        }

        private static void PaintCircle(Color32[] pixels, int size, int centerX, int centerY, int radius, Color32 color)
        {
            int radiusSquared = radius * radius;
            for (int y = Mathf.Max(0, centerY - radius); y <= Mathf.Min(size - 1, centerY + radius); y += 1)
            {
                for (int x = Mathf.Max(0, centerX - radius); x <= Mathf.Min(size - 1, centerX + radius); x += 1)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSquared) pixels[y * size + x] = color;
                }
            }
        }

        public static bool RunO9O10PresentationContracts(out string detail)
        {
            bool stableOpening = PrototypeGameJamNarrative.Opening.Length == 5 &&
                                 PrototypeGameJamNarrative.Opening.All(value => !string.IsNullOrWhiteSpace(value.StableId));
            bool sevenRegions = PrototypeGameJamNarrative.RegionIds.Length == 7 &&
                                PrototypeGameJamNarrative.RegionIds.Distinct(StringComparer.Ordinal).Count() == 7;
            bool fiveCoreEndings = PrototypeGameJamNarrative.CoreEndingIds.Length == 5 &&
                                   PrototypeGameJamNarrative.CoreEndingIds.Distinct(StringComparer.Ordinal).Count() == 5;
            bool nineteenItems = PrototypeGameJamNarrative.RequiredItemIds.Length == 19 &&
                                  PrototypeGameJamNarrative.RequiredItemIds.Distinct(StringComparer.Ordinal).Count() == 19;
            bool threePlayableEscapeEndings = PrototypeGameJamNarrative.CoreEndingIds.Count(value => value.StartsWith("ending.escape.", StringComparison.Ordinal)) == 3;
            bool lockedDirection = string.Equals(PrototypeGameJamNarrative.StoryCauseId, "story.stranding.storm-drift", StringComparison.Ordinal) &&
                                   string.Equals(PrototypeGameJamNarrative.ArtDirectionId, "art.ink-kim.simplified-world", StringComparison.Ordinal);
            bool pass = stableOpening && sevenRegions && nineteenItems && fiveCoreEndings && threePlayableEscapeEndings && lockedDirection;
            detail = pass
                ? "PASS O9/O10: storm-drift lock, five opening beats, seven regions, nineteen items, three playable escapes and five core endings."
                : "FAIL O9/O10 presentation content contract.";
            return pass;
        }
    }

    public enum PrototypeGameJamCue
    {
        UiConfirm,
        StoryAdvance,
        Search,
        Craft,
        Reject,
        Hazard,
        Ending
    }

    public sealed class PrototypeGameJamAudio : MonoBehaviour
    {
        private AudioSource ambience;
        private AudioSource oneShot;
        private AudioClip ambienceClip;
        private AudioClip clickClip;
        private AudioClip storyClip;
        private AudioClip searchClip;
        private AudioClip craftClip;
        private AudioClip rejectClip;
        private AudioClip hazardClip;
        private AudioClip endingClip;

        public void Initialize(Camera camera)
        {
            if (Application.isBatchMode) return;
            if (camera != null && camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
            ambience = gameObject.AddComponent<AudioSource>();
            oneShot = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.volume = 0.16f;
            oneShot.playOnAwake = false;
            oneShot.volume = 0.34f;
            ambienceClip = CreateNoiseLoop("Island Surf Loop", 4f, 0.045f, 0.22f);
            clickClip = CreateTone("UI Wood Click", 0.06f, 520f, 0.20f);
            storyClip = CreateTone("Story Page", 0.14f, 330f, 0.19f);
            searchClip = CreateNoiseLoop("Search Rustle", 0.24f, 0.11f, 0.63f);
            craftClip = CreateTone("Craft Tap", 0.18f, 210f, 0.24f);
            rejectClip = CreateTone("Reject Knock", 0.16f, 110f, 0.20f);
            hazardClip = CreateNoiseLoop("Hazard Sting", 0.28f, 0.16f, 0.88f);
            endingClip = CreateChord("Ending Chime", 0.85f, new[] { 261.63f, 329.63f, 392f }, 0.17f);
            ambience.clip = ambienceClip;
        }

        public void SetGameplayActive(bool active)
        {
            if (ambience == null || ambienceClip == null) return;
            if (active && !ambience.isPlaying) ambience.Play();
            else if (!active && ambience.isPlaying) ambience.Pause();
        }

        public void Play(PrototypeGameJamCue cue)
        {
            if (oneShot == null) return;
            AudioClip clip = cue == PrototypeGameJamCue.UiConfirm ? clickClip
                : cue == PrototypeGameJamCue.StoryAdvance ? storyClip
                : cue == PrototypeGameJamCue.Search ? searchClip
                : cue == PrototypeGameJamCue.Craft ? craftClip
                : cue == PrototypeGameJamCue.Reject ? rejectClip
                : cue == PrototypeGameJamCue.Hazard ? hazardClip
                : endingClip;
            if (clip != null) oneShot.PlayOneShot(clip);
        }

        public void DisposeClips()
        {
            DestroyClip(ambienceClip);
            DestroyClip(clickClip);
            DestroyClip(storyClip);
            DestroyClip(searchClip);
            DestroyClip(craftClip);
            DestroyClip(rejectClip);
            DestroyClip(hazardClip);
            DestroyClip(endingClip);
        }

        private static AudioClip CreateTone(string name, float duration, float frequency, float amplitude)
        {
            const int rate = 22050;
            int count = Mathf.Max(1, Mathf.CeilToInt(duration * rate));
            float[] data = new float[count];
            for (int i = 0; i < count; i += 1)
            {
                float t = i / (float)rate;
                float envelope = 1f - i / (float)count;
                data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * amplitude * envelope;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChord(string name, float duration, float[] frequencies, float amplitude)
        {
            const int rate = 22050;
            int count = Mathf.Max(1, Mathf.CeilToInt(duration * rate));
            float[] data = new float[count];
            for (int i = 0; i < count; i += 1)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Sin(Mathf.Clamp01(i / (count * 0.16f)) * Mathf.PI * 0.5f) * (1f - i / (float)count);
                float sample = 0f;
                for (int f = 0; f < frequencies.Length; f += 1) sample += Mathf.Sin(t * frequencies[f] * Mathf.PI * 2f);
                data[i] = sample / frequencies.Length * amplitude * envelope;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateNoiseLoop(string name, float duration, float amplitude, float density)
        {
            const int rate = 22050;
            int count = Mathf.Max(1, Mathf.CeilToInt(duration * rate));
            float[] data = new float[count];
            uint state = 0x6D2B79F5u;
            float filtered = 0f;
            for (int i = 0; i < count; i += 1)
            {
                state = state * 1664525u + 1013904223u;
                float raw = ((state >> 8) / 16777215f) * 2f - 1f;
                filtered = Mathf.Lerp(filtered, raw, density);
                float wave = 0.45f + Mathf.Sin(i / (float)rate * Mathf.PI * 0.42f) * 0.20f;
                data[i] = filtered * amplitude * wave;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void DestroyClip(AudioClip clip)
        {
            if (clip != null) Destroy(clip);
        }
    }
}
