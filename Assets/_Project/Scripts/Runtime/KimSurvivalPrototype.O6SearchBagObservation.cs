using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        public bool CaptureO6SearchBagObservation(string evidenceFolder, out string detail)
        {
            Directory.CreateDirectory(evidenceFolder);
            localization.SetLocale(PrototypeLocalization.KoreanLocaleCode, false);
            PrototypeProtectedPartAssignmentSnapshot assignment =
                searchNodeRuntime.Ledger.ProtectedPartAssignments.First();
            PrototypeSearchNodeDefinition definition = PrototypeSearchRegionCatalog.Nodes.First(node =>
                string.Equals(node.NodeId, assignment.AssignedNodeId, StringComparison.Ordinal));
            bool began = session.BeginSearch(PrototypeSearchRegionCatalog.StartingExpeditionFor(definition.RegionId));
            if (definition.RequiresSwimming) session.SetSwimming(true);
            RefreshAll();
            PrototypeSearchOpenResult opened = searchNodeRuntime.TryOpen(definition, session);
            RefreshAll(true);
            ApplyO7CompactBagLayout();
            Canvas.ForceUpdateCanvases();

            PrototypeSearchNodeSnapshot active = searchNodeRuntime.ActiveNode;
            int visibleRows = searchLootItemButtons.Count(button => button.gameObject.activeSelf);
            bool allRowsVisible = active != null && visibleRows == active.Remaining.Length &&
                                  active.Remaining.Length <= SearchLootVisibleEntryCapacity;
            bool protectedFirst = active != null && active.Remaining.Length > 0 &&
                                  active.Remaining[0].IsProtectedPart &&
                                  searchLootItemButtons[0].GetComponentInChildren<TMPro.TMP_Text>().text.Contains(
                                      localization.Format("search." + assignment.PartId));
            bool bagLeft = bagPanel.activeSelf &&
                           !WorldRect(searchLootTrayPanel.GetComponent<RectTransform>()).Overlaps(
                               WorldRect(bagPanel.GetComponent<RectTransform>()));
            bool fourOfTen = bagButtons.Count == GameSession.MaximumBagSlotCount &&
                             Enumerable.Range(0, GameSession.MaximumBagSlotCount).All(index =>
                                 session.IsBagSlotActive(index) == (index < GameSession.DefaultBagSlotCount)) &&
                             bagButtons.Skip(GameSession.DefaultBagSlotCount).All(button =>
                                 button.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("잠김"));
            CaptureVerificationPng(Path.Combine(evidenceFolder,
                "o6-search-tray-bag-always-visible-ko-1920x1080.png"), 1920, 1080);

            GameSessionStableState expanded = session.CaptureStableState();
            expanded.ActiveBagSlotCount = GameSession.MaximumBagSlotCount;
            bool expandedRestored = session.RestoreStableState(expanded);
            RefreshAll(true);
            ApplyO7CompactBagLayout();
            Canvas.ForceUpdateCanvases();
            bool tenActive = expandedRestored && bagButtons.Count == 10 &&
                             bagButtons.All(button => !button.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("잠김"));
            CaptureVerificationPng(Path.Combine(evidenceFolder,
                "o6-search-tray-ten-slot-bag-ko-1920x1080.png"), 1920, 1080);

            bool passed = began && opened == PrototypeSearchOpenResult.Opened &&
                          searchLootTrayPanel.activeSelf && allRowsVisible && protectedFirst && bagLeft &&
                          fourOfTen && tenActive;
            detail = "began=" + began + " opened=" + opened + " rows=" + visibleRows + "/" +
                     (active == null ? -1 : active.Remaining.Length) + " protectedFirst=" + protectedFirst +
                     " bagLeft=" + bagLeft + " bag4of10=" + fourOfTen + " bag10=" + tenActive;
            return passed;
        }
    }
}
