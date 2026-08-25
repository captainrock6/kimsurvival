using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeEndingAlbumUnlockRecord
    {
        public string ending_id = string.Empty;
        public string achievement_mapping_id = string.Empty;
        public int first_unlocked_day;
        public string first_unlocked_utc = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeEndingAlbumSaveData
    {
        public int schema_version = 1;
        public List<PrototypeEndingAlbumUnlockRecord> unlocks = new List<PrototypeEndingAlbumUnlockRecord>();
    }

    public readonly struct PrototypeEndingAlbumEntry
    {
        public PrototypeEndingAlbumEntry(
            PrototypeEndingDefinition definition,
            bool unlocked,
            int firstUnlockedDay,
            string firstUnlockedUtc)
        {
            Definition = definition;
            Unlocked = unlocked;
            FirstUnlockedDay = firstUnlockedDay;
            FirstUnlockedUtc = firstUnlockedUtc ?? string.Empty;
        }

        public PrototypeEndingDefinition Definition { get; }
        public bool Unlocked { get; }
        public int FirstUnlockedDay { get; }
        public string FirstUnlockedUtc { get; }
        public string AchievementMappingId { get { return Definition.AchievementMappingId; } }
        public string TitleKey { get { return Unlocked ? Definition.StableId + ".title" : "ending.album.locked.title"; } }
        public string DetailKey { get { return Unlocked ? Definition.StableId + ".summary" : Definition.StableId + ".hint"; } }
    }

    internal interface IPrototypeEndingAlbumStorage
    {
        string Load();
        void Save(string json);
    }

    internal sealed class PrototypePlayerPrefsEndingAlbumStorage : IPrototypeEndingAlbumStorage
    {
        public const string PreferenceKey = "kim_survival.ending_album.v1";

        public string Load()
        {
            return PlayerPrefs.GetString(PreferenceKey, string.Empty);
        }

        public void Save(string json)
        {
            PlayerPrefs.SetString(PreferenceKey, json ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    public sealed class PrototypeEndingAlbumCollection
    {
        private readonly IPrototypeEndingAlbumStorage storage;
        private readonly Dictionary<string, PrototypeEndingAlbumUnlockRecord> records =
            new Dictionary<string, PrototypeEndingAlbumUnlockRecord>(StringComparer.Ordinal);

        private PrototypeEndingAlbumCollection(IPrototypeEndingAlbumStorage storage, string json)
        {
            this.storage = storage;
            RestoreSnapshot(json, false);
        }

        public int EndingCount { get { return PrototypeEndingCatalog.All.Count; } }
        public int UnlockedCount { get { return records.Count; } }
        public bool PersistenceEnabled { get; set; } = true;

        public static PrototypeEndingAlbumCollection LoadDefault()
        {
            PrototypePlayerPrefsEndingAlbumStorage storage = new PrototypePlayerPrefsEndingAlbumStorage();
            return new PrototypeEndingAlbumCollection(storage, storage.Load());
        }

        public static PrototypeEndingAlbumCollection CreateTransient(string json = "")
        {
            return new PrototypeEndingAlbumCollection(null, json);
        }

        public PrototypeEndingAlbumEntry GetEntry(int index)
        {
            PrototypeEndingDefinition definition = PrototypeEndingCatalog.All[Mathf.Clamp(index, 0, EndingCount - 1)];
            records.TryGetValue(definition.StableId, out PrototypeEndingAlbumUnlockRecord record);
            return new PrototypeEndingAlbumEntry(
                definition,
                record != null,
                record == null ? 0 : record.first_unlocked_day,
                record == null ? string.Empty : record.first_unlocked_utc);
        }

        public bool IsUnlocked(string stableId)
        {
            return !string.IsNullOrWhiteSpace(stableId) && records.ContainsKey(stableId);
        }

        public int FirstUnlockedIndexOrZero()
        {
            for (int index = 0; index < EndingCount; index += 1)
            {
                if (IsUnlocked(PrototypeEndingCatalog.All[index].StableId))
                {
                    return index;
                }
            }
            return 0;
        }

        public bool Unlock(string stableId, int day)
        {
            return Unlock(stableId, day, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), true);
        }

        public bool UnlockForVerification(string stableId, int day, string firstUnlockedUtc)
        {
            return Unlock(stableId, day, firstUnlockedUtc, false);
        }

        public string CaptureSnapshot()
        {
            PrototypeEndingAlbumSaveData data = new PrototypeEndingAlbumSaveData
            {
                unlocks = records.Values
                    .OrderBy(value => value.ending_id, StringComparer.Ordinal)
                    .Select(CloneRecord)
                    .ToList()
            };
            return JsonUtility.ToJson(data);
        }

        public void RestoreTransientSnapshot(string json)
        {
            RestoreSnapshot(json, false);
        }

        private bool Unlock(string stableId, int day, string firstUnlockedUtc, bool persist)
        {
            PrototypeEndingDefinition definition = PrototypeEndingCatalog.All.FirstOrDefault(
                value => string.Equals(value.StableId, stableId, StringComparison.Ordinal));
            if (definition == null || records.ContainsKey(definition.StableId))
            {
                return false;
            }

            records.Add(definition.StableId, new PrototypeEndingAlbumUnlockRecord
            {
                ending_id = definition.StableId,
                achievement_mapping_id = definition.AchievementMappingId,
                first_unlocked_day = Mathf.Max(1, day),
                first_unlocked_utc = firstUnlockedUtc ?? string.Empty
            });
            if (persist && PersistenceEnabled && storage != null)
            {
                storage.Save(CaptureSnapshot());
            }
            return true;
        }

        private void RestoreSnapshot(string json, bool persist)
        {
            records.Clear();
            PrototypeEndingAlbumSaveData data = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    data = JsonUtility.FromJson<PrototypeEndingAlbumSaveData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[Kim Survival] Ending album data was ignored: " + exception.Message);
                }
            }

            if (data != null && data.unlocks != null)
            {
                for (int index = 0; index < data.unlocks.Count; index += 1)
                {
                    PrototypeEndingAlbumUnlockRecord source = data.unlocks[index];
                    PrototypeEndingDefinition definition = PrototypeEndingCatalog.All.FirstOrDefault(
                        value => source != null && string.Equals(value.StableId, source.ending_id, StringComparison.Ordinal));
                    if (definition == null || records.ContainsKey(definition.StableId))
                    {
                        continue;
                    }
                    records.Add(definition.StableId, new PrototypeEndingAlbumUnlockRecord
                    {
                        ending_id = definition.StableId,
                        achievement_mapping_id = definition.AchievementMappingId,
                        first_unlocked_day = Mathf.Max(1, source.first_unlocked_day),
                        first_unlocked_utc = source.first_unlocked_utc ?? string.Empty
                    });
                }
            }

            if (persist && PersistenceEnabled && storage != null)
            {
                storage.Save(CaptureSnapshot());
            }
        }

        private static PrototypeEndingAlbumUnlockRecord CloneRecord(PrototypeEndingAlbumUnlockRecord source)
        {
            return new PrototypeEndingAlbumUnlockRecord
            {
                ending_id = source.ending_id,
                achievement_mapping_id = source.achievement_mapping_id,
                first_unlocked_day = source.first_unlocked_day,
                first_unlocked_utc = source.first_unlocked_utc
            };
        }
    }

    public sealed class PrototypeEndingAlbumSelection
    {
        public bool IsOpen { get; private set; }
        public int FocusedIndex { get; private set; }

        public void Open(int focusedIndex)
        {
            FocusedIndex = Wrap(focusedIndex);
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public bool SetFocusedIndex(int index)
        {
            int next = Wrap(index);
            if (next == FocusedIndex)
            {
                return false;
            }
            FocusedIndex = next;
            return true;
        }

        public bool StepFocus(int direction)
        {
            return direction == 0 ? false : SetFocusedIndex(FocusedIndex + (direction < 0 ? -1 : 1));
        }

        private static int Wrap(int index)
        {
            int count = PrototypeEndingCatalog.All.Count;
            return count == 0 ? 0 : (index % count + count) % count;
        }
    }

    public static class PrototypeEndingAlbumContract
    {
        public static PrototypeContractProbe VerifyCatalogUnlockAndSelectionFixture()
        {
            IReadOnlyList<PrototypeEndingDefinition> definitions = PrototypeEndingCatalog.All;
            bool catalog = definitions.Count == 21 &&
                           definitions.Select(value => value.StableId).Distinct(StringComparer.Ordinal).Count() == 21 &&
                           definitions.Select(value => value.AchievementMappingId).Distinct(StringComparer.Ordinal).Count() == 21 &&
                           definitions.Count(value => value.Category == "escape") == 5 &&
                           definitions.Count(value => value.Category == "comic") == 5 &&
                           definitions.Count(value => value.Category == "rare") == 4 &&
                           definitions.Count(value => value.Category == "gamejam-stay") == 2 &&
                           definitions.Count(value => value.Category == "day50") == 5;

            PrototypeEndingAlbumCollection collection = PrototypeEndingAlbumCollection.CreateTransient();
            string sampleId = "ending.escape.smoke.seen-from-afar";
            bool firstUnlock = collection.UnlockForVerification(sampleId, 12, "2026-08-25T00:00:00.0000000Z");
            bool duplicateRejected = !collection.UnlockForVerification(sampleId, 40, "2026-08-25T01:00:00.0000000Z");
            int sampleIndex = definitions.ToList().FindIndex(value => value.StableId == sampleId);
            PrototypeEndingAlbumEntry unlocked = collection.GetEntry(sampleIndex);
            PrototypeEndingAlbumEntry locked = collection.GetEntry(10);
            string snapshot = collection.CaptureSnapshot();
            PrototypeEndingAlbumCollection restored = PrototypeEndingAlbumCollection.CreateTransient(snapshot);

            PrototypeEndingAlbumSelection selection = new PrototypeEndingAlbumSelection();
            selection.Open(0);
            selection.StepFocus(-1);
            bool wrapped = selection.FocusedIndex == 20;
            selection.StepFocus(1);
            bool success = catalog && firstUnlock && duplicateRejected && collection.UnlockedCount == 1 &&
                           unlocked.Unlocked && unlocked.FirstUnlockedDay == 12 &&
                           unlocked.AchievementMappingId == PrototypeEndingCatalog.Get(sampleId).AchievementMappingId &&
                           unlocked.TitleKey == sampleId + ".title" && unlocked.DetailKey == sampleId + ".summary" &&
                           !locked.Unlocked && locked.TitleKey == "ending.album.locked.title" &&
                           locked.DetailKey == locked.Definition.StableId + ".hint" &&
                           restored.IsUnlocked(sampleId) && restored.UnlockedCount == 1 && wrapped && selection.FocusedIndex == 0;
            return new PrototypeContractProbe(
                success,
                success
                    ? "ending album PASS catalog=19 categories=5/5/4/5 stable achievement mapping local unlock idempotent spoiler-safe locked hint selection wrap"
                    : "ending album contract mismatch");
        }
    }
}
