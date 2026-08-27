using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        public const int O7SafeBedHealthRecovery = 5;

        private static readonly Vector2 O7CompactBagAnchorMin = new Vector2(0.012f, 0.105f);
        private static readonly Vector2 O7CompactBagAnchorMax = new Vector2(0.232f, 0.395f);

        private GameObject o7SurvivalHelpPanel;
        private Button o7SurvivalHelpButton;
        private TMP_Text o7SurvivalHelpTitle;
        private TMP_Text o7SurvivalHelpBody;
        private Button o7SurvivalHelpCloseButton;
        private bool o7InitialGuideDismissed;
        private bool o7SawTerminalResult;

        private void LateUpdate()
        {
            if (canvas == null || session == null)
            {
                return;
            }

            EnsureO7SurvivalGuidanceUi();
            ApplyO7CompactBagLayout();
            ApplyO7SafeRestRecovery();
            RefreshO7SurvivalGuidanceUi();
        }

        private void EnsureO7SurvivalGuidanceUi()
        {
            if (o7SurvivalHelpPanel != null)
            {
                return;
            }

            o7SurvivalHelpButton = CreateButton(
                "O7 상태 도움말 열기",
                canvas.transform,
                new Vector2(0.82f, 0.805f),
                new Vector2(0.975f, 0.865f),
                string.Empty,
                ToggleO7SurvivalGuidance);
            Outline buttonOutline = o7SurvivalHelpButton.gameObject.AddComponent<Outline>();
            buttonOutline.effectColor = new Color(0.95f, 0.75f, 0.28f, 0.88f);
            buttonOutline.effectDistance = new Vector2(2f, -2f);

            o7SurvivalHelpPanel = CreatePanel(
                "O7 첫 캠프 생존 상태 안내",
                canvas.transform,
                new Vector2(0.255f, 0.43f),
                new Vector2(0.745f, 0.705f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.025f, 0.07f, 0.08f, 0.975f)).gameObject;
            Outline panelOutline = o7SurvivalHelpPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.95f, 0.75f, 0.28f, 0.96f);
            panelOutline.effectDistance = new Vector2(3f, -3f);

            o7SurvivalHelpTitle = CreateText(
                "O7 생존 상태 안내 제목",
                o7SurvivalHelpPanel.transform,
                new Vector2(0.04f, 0.77f),
                new Vector2(0.96f, 0.96f),
                Vector2.zero,
                Vector2.zero,
                28,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.86f, 0.38f));
            o7SurvivalHelpTitle.fontStyle = FontStyles.Bold;
            o7SurvivalHelpTitle.enableAutoSizing = true;
            o7SurvivalHelpTitle.fontSizeMin = 18f;
            o7SurvivalHelpTitle.fontSizeMax = 28f;
            o7SurvivalHelpTitle.maxVisibleLines = 1;
            o7SurvivalHelpTitle.overflowMode = TextOverflowModes.Ellipsis;

            o7SurvivalHelpBody = CreateText(
                "O7 생존 상태 안내 본문",
                o7SurvivalHelpPanel.transform,
                new Vector2(0.04f, 0.20f),
                new Vector2(0.96f, 0.78f),
                Vector2.zero,
                Vector2.zero,
                21,
                TextAnchor.UpperLeft,
                Color.white);
            o7SurvivalHelpBody.enableAutoSizing = true;
            o7SurvivalHelpBody.fontSizeMin = 13f;
            o7SurvivalHelpBody.fontSizeMax = 21f;
            o7SurvivalHelpBody.textWrappingMode = TextWrappingModes.Normal;
            o7SurvivalHelpBody.maxVisibleLines = 8;
            o7SurvivalHelpBody.overflowMode = TextOverflowModes.Ellipsis;

            o7SurvivalHelpCloseButton = CreateButton(
                "O7 생존 상태 안내 닫기",
                o7SurvivalHelpPanel.transform,
                new Vector2(0.68f, 0.035f),
                new Vector2(0.96f, 0.19f),
                string.Empty,
                DismissO7SurvivalGuidance);
            o7SurvivalHelpPanel.SetActive(false);
        }

        private void ToggleO7SurvivalGuidance()
        {
            if (o7SurvivalHelpPanel == null)
            {
                return;
            }

            bool open = !o7SurvivalHelpPanel.activeSelf;
            o7SurvivalHelpPanel.SetActive(open);
            if (!open)
            {
                o7InitialGuideDismissed = true;
            }
        }

        private void DismissO7SurvivalGuidance()
        {
            o7InitialGuideDismissed = true;
            if (o7SurvivalHelpPanel != null)
            {
                o7SurvivalHelpPanel.SetActive(false);
            }
        }

        private void RefreshO7SurvivalGuidanceUi()
        {
            if (o7SurvivalHelpPanel == null || o7SurvivalHelpButton == null)
            {
                return;
            }

            if (session.Result != RunResult.None)
            {
                o7SawTerminalResult = true;
            }
            else if (o7SawTerminalResult)
            {
                o7SawTerminalResult = false;
                o7InitialGuideDismissed = false;
            }

            bool cleanCamp = session.Phase == GamePhase.Camp &&
                             !campPlacement.IsActive &&
                             !campModuleExpansion.IsPreviewActive &&
                             !campInteraction.IsPopupOpen;
            o7SurvivalHelpButton.gameObject.SetActive(cleanCamp);
            SetButton(o7SurvivalHelpButton, localization.Format("survival.help.button"), true);
            SetButton(o7SurvivalHelpCloseButton, localization.Format("survival.help.close"), true);
            o7SurvivalHelpTitle.text = localization.Format("survival.help.title");
            o7SurvivalHelpBody.text = localization.Format("survival.help.body");

            bool shouldPresentInitialGuide = cleanCamp && session.Day == 1 &&
                                             !session.ExpeditionCompleted &&
                                             !o7InitialGuideDismissed;
            if (!cleanCamp)
            {
                o7SurvivalHelpPanel.SetActive(false);
            }
            else if (shouldPresentInitialGuide && !o7SurvivalHelpPanel.activeSelf)
            {
                o7SurvivalHelpPanel.SetActive(true);
            }

            bool pseudoLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;
            TMP_Text buttonLabel = o7SurvivalHelpButton.GetComponentInChildren<TMP_Text>();
            buttonLabel.enableAutoSizing = true;
            buttonLabel.fontSizeMin = pseudoLong ? 14f : 18f;
            buttonLabel.fontSizeMax = pseudoLong ? 18f : 22f;
            buttonLabel.maxVisibleLines = 1;
            buttonLabel.overflowMode = TextOverflowModes.Ellipsis;
            o7SurvivalHelpBody.fontSizeMin = pseudoLong ? 11f : 13f;
            o7SurvivalHelpBody.fontSizeMax = pseudoLong ? 17f : 21f;
            o7SurvivalHelpBody.maxVisibleLines = pseudoLong ? 9 : 8;
        }

        private void ApplyO7CompactBagLayout()
        {
            if (bagPanel == null || !bagPanel.activeSelf || session.Phase != GamePhase.Exploring)
            {
                return;
            }

            RectTransform bagRect = bagPanel.GetComponent<RectTransform>();
            bagRect.anchorMin = O7CompactBagAnchorMin;
            bagRect.anchorMax = O7CompactBagAnchorMax;
            bagRect.offsetMin = Vector2.zero;
            bagRect.offsetMax = Vector2.zero;
            Image surface = bagPanel.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = new Color(0.025f, 0.07f, 0.08f, 0.92f);
            }

            RectTransform titleRect = bagTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.035f, 0.78f);
            titleRect.anchorMax = new Vector2(0.965f, 0.975f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            bool pseudoLong = localization.CurrentLocaleCode == PrototypeLocalization.QpsLongLocaleCode;
            bagTitleText.fontSizeMin = pseudoLong ? 10f : 12f;
            bagTitleText.fontSizeMax = pseudoLong ? 13f : 16f;
            bagTitleText.maxVisibleLines = 2;
            bagTitleText.overflowMode = TextOverflowModes.Ellipsis;

            for (int index = 0; index < bagButtons.Count; index += 1)
            {
                int column = index % 2;
                int row = index / 2;
                float left = 0.035f + column * 0.48f;
                float right = left + 0.45f;
                float top = 0.755f - row * 0.143f;
                float bottom = top - 0.118f;
                RectTransform buttonRect = bagButtons[index].GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(left, bottom);
                buttonRect.anchorMax = new Vector2(right, top);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;

                TMP_Text label = bagButtons[index].GetComponentInChildren<TMP_Text>();
                label.rectTransform.offsetMin = new Vector2(26f, 1f);
                label.rectTransform.offsetMax = new Vector2(-3f, -1f);
                label.enableAutoSizing = true;
                label.fontSizeMin = pseudoLong ? 7f : 9f;
                label.fontSizeMax = pseudoLong ? 10f : 13f;
                label.maxVisibleLines = 2;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Ellipsis;

                if (index < bagButtonIcons.Count && bagButtonIcons[index] != null)
                {
                    RectTransform iconRect = bagButtonIcons[index].rectTransform;
                    iconRect.anchorMin = new Vector2(0f, 0.5f);
                    iconRect.anchorMax = new Vector2(0f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(13f, 0f);
                    iconRect.sizeDelta = new Vector2(18f, 18f);
                }
            }
        }

        private void ApplyO7SafeRestRecovery()
        {
            if (session.Phase != GamePhase.Camp ||
                !session.HasStructure(StructureKind.Bed) ||
                !campUse.IsDayBenefitPrepared(StructureKind.Bed) ||
                session.Health >= 100)
            {
                return;
            }

            string transactionId = "o7.safe-bed-health.day." + session.Day;
            int healthBefore = session.Health;
            if (!session.ApplyHealthDelta(transactionId, O7SafeBedHealthRecovery))
            {
                return;
            }

            int recovered = session.Health - healthBefore;
            campFeedback = new PrototypeLocalizedText("message.camp.use.bed.health", recovered);
            RefreshHud();
        }

        public static bool RunO7BagSurvivalDomainContracts(out string detail)
        {
            var checks = new List<string>();
            bool layout = O7CompactBagAnchorMax.x <= 0.24f &&
                          O7CompactBagAnchorMin.x >= 0f &&
                          O7CompactBagAnchorMin.y >= 0.09f &&
                          O7CompactBagAnchorMax.y <= 0.41f;
            checks.Add("compact-bottom-left=" + layout);

            var bedSession = new GameSession();
            bedSession.Grant(ResourceKind.Wood, 3);
            bedSession.Grant(ResourceKind.Salvage, 1);
            bool bedBuilt = bedSession.TryBuild(StructureKind.Bed);
            bool began = bedSession.BeginSearch(PrototypeExpeditionRegionId.Beach);
            bool spent = bedSession.TryApplySearchNodeCost(44, 12);
            bool returned = bedSession.ReturnToCamp(false);
            float energyBefore = bedSession.Energy;
            bool settled = bedSession.EndDay(false, false, true, false);
            float energyDelta = bedSession.Energy - energyBefore;
            bool bedEnergy = bedBuilt && began && spent && returned && settled &&
                             Mathf.Approximately(energyDelta, 40f);
            checks.Add("bed-settlement-energy+40=" + bedEnergy);

            var sofaSession = new GameSession();
            sofaSession.Grant(ResourceKind.Wood, 2);
            sofaSession.Grant(ResourceKind.Salvage, 2);
            bool sofaBuilt = sofaSession.TryBuild(StructureKind.Sofa);
            bool sofaFlow = sofaSession.BeginSearch(PrototypeExpeditionRegionId.Beach) &&
                            sofaSession.TryApplySearchNodeCost(44, 12) &&
                            sofaSession.ReturnToCamp(false);
            float sofaBefore = sofaSession.Energy;
            bool sofaSettled = sofaSession.EndDay(false, false, false, true);
            bool sofaEnergy = sofaBuilt && sofaFlow && sofaSettled &&
                              Mathf.Approximately(sofaSession.Energy - sofaBefore, 28f);
            checks.Add("sofa-settlement-energy+28=" + sofaEnergy);

            var healthSession = new GameSession();
            GameSessionStableState healthState = healthSession.CaptureStableState();
            healthState.Health = 70;
            bool healthRestored = healthSession.RestoreStableState(healthState);
            bool firstHealth = healthSession.ApplyHealthDelta("o7.safe-bed-health.day.1", O7SafeBedHealthRecovery);
            bool duplicateRejected = !healthSession.ApplyHealthDelta(
                "o7.safe-bed-health.day.1",
                O7SafeBedHealthRecovery);
            bool safeHealth = healthRestored && firstHealth && duplicateRejected && healthSession.Health == 75;
            checks.Add("safe-rest-health-once+5=" + safeHealth);

            var daylightSession = new GameSession();
            bool daylightBegan = daylightSession.BeginSearch(PrototypeExpeditionRegionId.Beach);
            float daylightAtDeparture = daylightSession.Daylight;
            bool daylightSpent = daylightSession.TryApplySearchNodeCost(5, 18);
            float daylightInField = daylightSession.Daylight;
            bool daylightReset = daylightSession.ReturnToCamp(false) && daylightSession.EndDay(false, false) &&
                                 daylightSession.Daylight >= 99.9f;
            bool daylightContract = daylightBegan && daylightSpent &&
                                    daylightAtDeparture >= 99.9f && daylightInField <= 82.1f && daylightReset;
            checks.Add("daylight-time-budget-reset-next-day=" + daylightContract);

            bool passed = layout && bedEnergy && sofaEnergy && safeHealth && daylightContract;
            detail = string.Join("; ", checks.ToArray());
            return passed;
        }

        public bool CaptureO7BagSurvivalObservation(string evidenceFolder, out string detail)
        {
            Directory.CreateDirectory(evidenceFolder);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            session.Reset(PrototypeExpeditionRegionCatalog.CreateRuntimeSeed());
            searchNodeRuntime.Reset(session.RunSeed);
            campPlacement.Reset();
            campUse.Reset();
            campInteraction.Reset();
            RefreshAll();

            EnsureO7SurvivalGuidanceUi();
            RefreshO7SurvivalGuidanceUi();
            Canvas.ForceUpdateCanvases();
            bool guideVisible = o7SurvivalHelpPanel.activeSelf &&
                                o7SurvivalHelpBody.text.Contains("기력") &&
                                o7SurvivalHelpBody.text.Contains("체력") &&
                                o7SurvivalHelpBody.text.Contains("일광");
            CaptureVerificationPng(
                Path.Combine(evidenceFolder, "o7-first-camp-survival-guide-ko-1280x800.png"),
                1280,
                800);
            DismissO7SurvivalGuidance();

            PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.First();
            bool began = session.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId));
            if (definition.RequiresSwimming)
            {
                session.SetSwimming(true);
            }
            PrototypeSearchOpenResult opened = searchNodeRuntime.TryOpen(definition, session);
            RefreshAll(true);
            ApplyO7CompactBagLayout();
            Canvas.ForceUpdateCanvases();
            RectTransform bagRect = bagPanel.GetComponent<RectTransform>();
            bool compact = bagRect.anchorMin == O7CompactBagAnchorMin &&
                           bagRect.anchorMax == O7CompactBagAnchorMax &&
                           bagRect.anchorMax.x <= 0.32f;
            bool noTrayOverlap = !WorldRect(searchLootTrayPanel.GetComponent<RectTransform>()).Overlaps(
                WorldRect(bagRect));
            bool fourOfTen = bagButtons.Count == GameSession.MaximumBagSlotCount &&
                             Enumerable.Range(0, GameSession.MaximumBagSlotCount).All(index =>
                                 session.IsBagSlotActive(index) == (index < GameSession.DefaultBagSlotCount));
            CaptureVerificationPng(
                Path.Combine(evidenceFolder, "o7-compact-bag-search-tray-ko-1280x800.png"),
                1280,
                800);
            CaptureVerificationPng(
                Path.Combine(evidenceFolder, "o7-compact-bag-search-tray-ko-1920x1080.png"),
                1920,
                1080);

            GameSessionStableState expanded = session.CaptureStableState();
            expanded.ActiveBagSlotCount = GameSession.MaximumBagSlotCount;
            bool expandedRestored = session.RestoreStableState(expanded);
            RefreshAll(true);
            ApplyO7CompactBagLayout();
            Canvas.ForceUpdateCanvases();
            bool tenActive = expandedRestored && bagButtons.Count == 10 &&
                             Enumerable.Range(0, 10).All(session.IsBagSlotActive);
            CaptureVerificationPng(
                Path.Combine(evidenceFolder, "o7-compact-ten-slot-bag-ko-1920x1080.png"),
                1920,
                1080);

            bool domain = RunO7BagSurvivalDomainContracts(out string domainDetail);
            bool passed = guideVisible && began && opened == PrototypeSearchOpenResult.Opened && compact &&
                          noTrayOverlap && fourOfTen && tenActive && domain;
            detail = "guide=" + guideVisible + "; began=" + began + "; opened=" + opened +
                     "; compact=" + compact + "; noTrayOverlap=" + noTrayOverlap +
                     "; bag4of10=" + fourOfTen + "; bag10=" + tenActive + "; " + domainDetail;
            return passed;
        }
    }
}
