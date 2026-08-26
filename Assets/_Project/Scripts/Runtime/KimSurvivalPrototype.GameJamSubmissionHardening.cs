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
        private const int GameJamTerminalControlSortingOrder = 140;

        private GameObject gameJamTerminalControlRoot;
        private Canvas gameJamTerminalControlCanvas;
        private GraphicRaycaster gameJamTerminalControlRaycaster;
        private Button gameJamTerminalRestartButton;
        private Button gameJamTerminalBackButton;

        private bool IsGameJamLiveEscapeProfile
        {
            get { return session != null && session.IsProvisionalSessionProfile; }
        }

        private void BuildGameJamSubmissionControls()
        {
            GameObject root = new GameObject("GAME JAM 엔딩 조작 오버레이");
            root.transform.SetParent(transform, false);
            gameJamTerminalControlRoot = root;
            gameJamTerminalControlCanvas = root.AddComponent<Canvas>();
            gameJamTerminalControlCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            gameJamTerminalControlCanvas.worldCamera = worldCamera;
            gameJamTerminalControlCanvas.planeDistance = 0.35f;
            gameJamTerminalControlCanvas.sortingOrder = GameJamTerminalControlSortingOrder;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;
            gameJamTerminalControlRaycaster = root.AddComponent<GraphicRaycaster>();

            gameJamTerminalBackButton = CreateButton(
                "엔딩 만화에서 결과로 돌아가기",
                root.transform,
                new Vector2(0.61f, 0.105f),
                new Vector2(0.79f, 0.185f),
                string.Empty,
                ReturnFromGameJamTerminalComic);
            gameJamTerminalRestartButton = CreateButton(
                "엔딩 만화에서 새 게임 시작",
                root.transform,
                new Vector2(0.805f, 0.105f),
                new Vector2(0.97f, 0.185f),
                string.Empty,
                RestartSession);
            ConfigureGameJamTerminalButton(gameJamTerminalBackButton);
            ConfigureGameJamTerminalButton(gameJamTerminalRestartButton);

            Navigation backNavigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = gameJamTerminalRestartButton,
                selectOnRight = gameJamTerminalRestartButton,
                selectOnUp = gameJamTerminalRestartButton,
                selectOnDown = gameJamTerminalRestartButton
            };
            Navigation restartNavigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = gameJamTerminalBackButton,
                selectOnRight = gameJamTerminalBackButton,
                selectOnUp = gameJamTerminalBackButton,
                selectOnDown = gameJamTerminalBackButton
            };
            gameJamTerminalBackButton.navigation = backNavigation;
            gameJamTerminalRestartButton.navigation = restartNavigation;
            root.SetActive(false);
        }

        private static void ConfigureGameJamTerminalButton(Button button)
        {
            Image surface = button.GetComponent<Image>();
            surface.color = new Color(0.025f, 0.16f, 0.18f, 0.98f);
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.025f, 0.16f, 0.18f, 0.98f);
            colors.highlightedColor = new Color(0.07f, 0.42f, 0.42f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.90f, 0.43f, 0.14f, 1f);
            colors.disabledColor = new Color(0.06f, 0.09f, 0.10f, 0.76f);
            button.colors = colors;
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.28f, 0.98f);
            outline.effectDistance = new Vector2(2f, -2f);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing = true;
            label.fontSizeMin = 17f;
            label.fontSizeMax = 24f;
            label.maxVisibleLines = 2;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private void RefreshGameJamSubmissionControls(bool result)
        {
            if (gameJamTerminalControlRoot == null) return;
            bool active = result && IsGameJamLiveEscapeProfile;
            gameJamTerminalControlRoot.SetActive(active);
            if (!active) return;
            SetButton(gameJamTerminalBackButton, localization.Format("ending.album.back"), true);
            SetButton(gameJamTerminalRestartButton, localization.Format("ui.restart"), true);
        }

        private GameObject GameJamTerminalDefaultSelection
        {
            get
            {
                return gameJamTerminalRestartButton != null && gameJamTerminalRestartButton.gameObject.activeInHierarchy
                    ? gameJamTerminalRestartButton.gameObject
                    : restartButton.gameObject;
            }
        }

        private void ReturnFromGameJamTerminalComic()
        {
            if (hazardEscapeEndingRuntime != null) hazardEscapeEndingRuntime.DeactivateComic();
            if (gameJamTerminalControlRoot != null) gameJamTerminalControlRoot.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(true);
            if (EventSystem.current != null && restartButton != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        private string FormatGameJamCampPopupDetail(
            PrototypeCampInteractionTargetKind target,
            string baseDetail)
        {
            if (!IsGameJamLiveEscapeProfile) return baseDetail;
            switch (target)
            {
                case PrototypeCampInteractionTargetKind.StoragePlanning:
                    return baseDetail + "\n" + FormatGameJamStableResourceStock();
                case PrototypeCampInteractionTargetKind.SmokeBeacon:
                    return baseDetail + "\n" + FormatGameJamEscapeRequirements("escape.smoke");
                case PrototypeCampInteractionTargetKind.RadioBench:
                    return baseDetail + "\n" + FormatGameJamEscapeRequirements("escape.radio");
                default:
                    return baseDetail;
            }
        }

        private void ConfigureGameJamCampPopupDetailLayout(PrototypeCampInteractionTargetKind target)
        {
            if (!IsGameJamLiveEscapeProfile || campPopupDetailText == null) return;
            bool detailed = target == PrototypeCampInteractionTargetKind.StoragePlanning ||
                            target == PrototypeCampInteractionTargetKind.SmokeBeacon ||
                            target == PrototypeCampInteractionTargetKind.RadioBench;
            if (!detailed) return;
            bool qpsLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;
            if (target == PrototypeCampInteractionTargetKind.StoragePlanning)
            {
                RectTransform detailRect = campPopupDetailText.rectTransform;
                detailRect.offsetMin = new Vector2(24f, -222f);
                detailRect.offsetMax = new Vector2(-24f, -76f);
            }
            campPopupDetailText.enableAutoSizing = true;
            campPopupDetailText.fontSizeMin = qpsLong ? 12f : 18f;
            campPopupDetailText.fontSizeMax = qpsLong ? 20f : 24f;
            campPopupDetailText.maxVisibleLines = target == PrototypeCampInteractionTargetKind.StoragePlanning ? 7 : 5;
            campPopupDetailText.textWrappingMode = TextWrappingModes.Normal;
            campPopupDetailText.overflowMode = TextOverflowModes.Overflow;
        }

        private string FormatGameJamStableResourceStock()
        {
            StableResourceAmount[] entries = session.GetStableStorageEntries();
            var rows = new List<string>();
            const int columns = 4;
            for (int index = 0; index < entries.Length; index += columns)
            {
                rows.Add(string.Join(" · ", entries.Skip(index).Take(columns)
                    .Select(value => localization.Format(value.StableResourceId) + " " + value.Amount)
                    .ToArray()));
            }
            return localization.Format("value.yes") + " · " + string.Join("\n", rows.ToArray());
        }

        private string FormatGameJamEscapeResourceHud()
        {
            string[] firstRow = { "resource.wood", "resource.fiber", "resource.fuel" };
            string[] secondRow = { "resource.electronics", "resource.wire", "resource.metal" };
            return string.Join(" · ", firstRow.Select(value =>
                       localization.Format(value) + " " + session.GetStableStorage(value)).ToArray()) +
                   "\n" +
                   string.Join(" · ", secondRow.Select(value =>
                       localization.Format(value) + " " + session.GetStableStorage(value)).ToArray());
        }

        private string FormatGameJamEscapeRequirements(string escapeId)
        {
            PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
            string[] resourceStatus = definition.StableCosts
                .Select(value => localization.Format(value.StableResourceId) + " " +
                                 session.GetStableStorage(value.StableResourceId) + "/" + value.Amount)
                .ToArray();
            string[] missingParts = definition.RequiredKeyPartIds
                .Where(value => !hazardEscapeEndingRuntime.HasProtectedSearchPart(value))
                .Select(value => localization.Format("search." + value))
                .ToArray();
            string[] missingResources = definition.StableCosts
                .Where(value => session.GetStableStorage(value.StableResourceId) < value.Amount)
                .Select(value => localization.Format(value.StableResourceId) + " " +
                                 (value.Amount - session.GetStableStorage(value.StableResourceId)))
                .ToArray();
            string missing = string.Join(" · ", missingResources.Concat(missingParts).ToArray());
            string status = string.Join(" · ", resourceStatus);
            return string.IsNullOrEmpty(missing)
                ? localization.Format("value.yes") + " · " + status
                : localization.Format("interaction.module.missing", localization.Format(escapeId), missing) + "\n" + status;
        }

        private string[] CaptureGameJamStableResourceLocaleEvidence()
        {
            string original = localization.CurrentLocaleCode;
            var evidence = new List<string>();
            try
            {
                foreach (string locale in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode })
                {
                    localization.SetLocale(locale, false);
                    evidence.Add(locale + "=" + FormatGameJamStableResourceStock());
                }
            }
            finally
            {
                localization.SetLocale(original, false);
            }
            return evidence.ToArray();
        }

        private string[] CaptureGameJamEscapeShortageLocaleEvidence()
        {
            string original = localization.CurrentLocaleCode;
            var evidence = new List<string>();
            try
            {
                foreach (string locale in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode })
                {
                    localization.SetLocale(locale, false);
                    evidence.Add(locale + ".smoke=" + FormatGameJamEscapeRequirements("escape.smoke"));
                    evidence.Add(locale + ".radio=" + FormatGameJamEscapeRequirements("escape.radio"));
                }
            }
            finally
            {
                localization.SetLocale(original, false);
            }
            return evidence.ToArray();
        }

        private PrototypeGameJamTerminalControlObservation CaptureAndExerciseGameJamTerminalControls()
        {
            var observation = new PrototypeGameJamTerminalControlObservation();
            if (session == null || session.Phase != GamePhase.Result || gameJamTerminalControlRoot == null)
            {
                return observation;
            }

            RefreshGameJamSubmissionControls(true);
            Canvas.ForceUpdateCanvases();
            observation.ActionIds = new[] { "ending.back", "session.restart" };
            observation.LocalizedLabels = new[]
            {
                gameJamTerminalBackButton.GetComponentInChildren<TMP_Text>().text,
                gameJamTerminalRestartButton.GetComponentInChildren<TMP_Text>().text
            };
            observation.SortingOrder = gameJamTerminalControlCanvas.sortingOrder;
            GameObject comicRoot = FindTerminalComicObject("Resolution Triptych A");
            Canvas comicCanvas = comicRoot == null ? null : comicRoot.GetComponent<Canvas>();
            observation.ActiveAboveComic = gameJamTerminalControlRoot.activeInHierarchy &&
                                             comicCanvas != null && comicRoot.activeInHierarchy &&
                                             gameJamTerminalControlCanvas.sortingOrder > comicCanvas.sortingOrder;
            observation.MouseRaycastReady = IsGameJamTerminalButtonRaycastable(gameJamTerminalBackButton) &&
                                             IsGameJamTerminalButtonRaycastable(gameJamTerminalRestartButton);
            observation.ExplicitNavigationReady = HasExplicitBidirectionalNavigation(
                gameJamTerminalBackButton,
                gameJamTerminalRestartButton);

            EventSystem eventSystem = EventSystem.current;
            eventSystem.SetSelectedGameObject(gameJamTerminalBackButton.gameObject);
            observation.KeyboardSubmitObserved = ExecuteEvents.Execute(
                gameJamTerminalBackButton.gameObject,
                new BaseEventData(eventSystem),
                ExecuteEvents.submitHandler);
            observation.BackTransitionObserved = resultPanel.activeInHierarchy &&
                                                 (comicRoot == null || !comicRoot.activeInHierarchy) &&
                                                 eventSystem.currentSelectedGameObject == restartButton.gameObject;

            RefreshAll();
            eventSystem.SetSelectedGameObject(gameJamTerminalRestartButton.gameObject);
            observation.GamepadSubmitObserved = ExecuteEvents.Execute(
                gameJamTerminalRestartButton.gameObject,
                new BaseEventData(eventSystem),
                ExecuteEvents.submitHandler);
            observation.RestartTransitionObserved = session.Phase == GamePhase.Camp &&
                                                    session.Result == RunResult.None &&
                                                    !gameJamTerminalControlRoot.activeInHierarchy;
            return observation;
        }

        private bool IsGameJamTerminalButtonRaycastable(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable ||
                gameJamTerminalControlRaycaster == null || !gameJamTerminalControlRaycaster.enabled)
            {
                return false;
            }
            RectTransform rect = button.GetComponent<RectTransform>();
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(
                gameJamTerminalControlCanvas.worldCamera,
                rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screenPoint };
            var hits = new List<RaycastResult>();
            gameJamTerminalControlRaycaster.Raycast(pointer, hits);
            return hits.Any(value => value.gameObject == button.gameObject ||
                                     value.gameObject.transform.IsChildOf(button.transform));
        }

        private static bool HasExplicitBidirectionalNavigation(Button back, Button restart)
        {
            Navigation backNavigation = back.navigation;
            Navigation restartNavigation = restart.navigation;
            return backNavigation.mode == Navigation.Mode.Explicit &&
                   restartNavigation.mode == Navigation.Mode.Explicit &&
                   (backNavigation.selectOnRight == restart || backNavigation.selectOnLeft == restart) &&
                   (restartNavigation.selectOnLeft == back || restartNavigation.selectOnRight == back);
        }
    }
}
