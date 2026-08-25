using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeGameSessionWaveCSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion;
        public int RunSeed;
        public GameSessionStableState StableState;
        public int Day;
        public float Hunger;
        public float Energy;
        public float Daylight;
        public GamePhase Phase;
        public RunResult Result;
        public bool ExpeditionCompleted;
        public bool IsSwimming;
        public int SignalStage;
        public bool[] Structures;
        public bool[] Researched;
        public bool[] CraftedTools;
        public bool HasSelectedRegion;
        public PrototypeExpeditionRegionId SelectedRegionId;
        public string ActiveRegionProfileId;
        public string LastExpeditionResultId;
        public string CompletedEscapeId;
        public string LastMessageKey;
        public string[] LastMessageArguments;
    }

    [Serializable]
    public sealed class PrototypeWaveCSaveRoot
    {
        public const int LegacySchemaVersion = 1;
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion;
        public string SessionJson;
        public string SearchLedgerJson;
        public string EscapeDirectorJson;
        public string WaveRuntimeJson;
        public string EndingAlbumJson;
        public string CampSpaceJson;
        public string CurrentRoomId;
        public string PayloadFingerprint;
    }

    public static class PrototypeWaveCSaveFingerprint
    {
        public static string Compute(PrototypeWaveCSaveRoot root)
        {
            if (root == null) return string.Empty;
            string canonical = "wave-c-save-v" + root.SchemaVersion + "|" +
                               Part(root.SessionJson) + "|" +
                               Part(root.SearchLedgerJson) + "|" +
                               Part(root.EscapeDirectorJson) + "|" +
                               (root.SchemaVersion >= PrototypeWaveCSaveRoot.CurrentSchemaVersion
                                   ? Part(root.WaveRuntimeJson) + "|"
                                   : string.Empty) +
                               Part(root.EndingAlbumJson) + "|" +
                               Part(root.CampSpaceJson) + "|" +
                               Part(root.CurrentRoomId);
            return Hash128.Compute(canonical).ToString();
        }

        private static string Part(string value)
        {
            string stable = value ?? string.Empty;
            return stable.Length.ToString(CultureInfo.InvariantCulture) + ":" + stable;
        }
    }

    public sealed partial class GameSession
    {
        public PrototypeGameSessionWaveCSnapshot CaptureWaveCSnapshot()
        {
            return new PrototypeGameSessionWaveCSnapshot
            {
                SchemaVersion = PrototypeGameSessionWaveCSnapshot.CurrentSchemaVersion,
                RunSeed = RunSeed,
                StableState = CaptureStableState(),
                Day = Day,
                Hunger = Hunger,
                Energy = Energy,
                Daylight = Daylight,
                Phase = Phase,
                Result = Result,
                ExpeditionCompleted = ExpeditionCompleted,
                IsSwimming = IsSwimming,
                SignalStage = SignalStage,
                Structures = Enum.GetValues(typeof(StructureKind)).Cast<StructureKind>()
                    .Select(HasStructure).ToArray(),
                Researched = Enum.GetValues(typeof(TechKind)).Cast<TechKind>()
                    .Select(HasResearched).ToArray(),
                CraftedTools = Enum.GetValues(typeof(TechKind)).Cast<TechKind>()
                    .Select(HasCrafted).ToArray(),
                HasSelectedRegion = SelectedRegionId.HasValue,
                SelectedRegionId = SelectedRegionId.GetValueOrDefault(),
                ActiveRegionProfileId = ActiveRegionProfileId ?? string.Empty,
                LastExpeditionResultId = LastExpeditionResultId ?? string.Empty,
                CompletedEscapeId = CompletedEscapeId ?? string.Empty,
                LastMessageKey = LastMessage.Key ?? string.Empty,
                LastMessageArguments = (LastMessage.Arguments ?? Array.Empty<object>())
                    .Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
                    .ToArray()
            };
        }

        public bool RestoreWaveCSnapshot(PrototypeGameSessionWaveCSnapshot snapshot)
        {
            if (!ValidateWaveCSnapshot(snapshot) || !RestoreStableState(snapshot.StableState))
            {
                return false;
            }

            RunSeed = snapshot.RunSeed;
            Day = snapshot.Day;
            Hunger = snapshot.Hunger;
            Energy = snapshot.Energy;
            Daylight = snapshot.Daylight;
            Phase = snapshot.Phase;
            Result = snapshot.Result;
            ExpeditionCompleted = snapshot.ExpeditionCompleted;
            IsSwimming = snapshot.IsSwimming;
            SignalStage = snapshot.SignalStage;
            Array.Copy(snapshot.Structures, structures, structures.Length);
            Array.Copy(snapshot.Researched, researched, researched.Length);
            Array.Copy(snapshot.CraftedTools, craftedTools, craftedTools.Length);
            SelectedRegionId = snapshot.HasSelectedRegion
                ? snapshot.SelectedRegionId
                : (PrototypeExpeditionRegionId?)null;
            ActiveRegionProfileId = snapshot.ActiveRegionProfileId;
            LastExpeditionResultId = snapshot.LastExpeditionResultId;
            CompletedEscapeId = snapshot.CompletedEscapeId;
            LastMessage = new PrototypeLocalizedText(
                snapshot.LastMessageKey,
                snapshot.LastMessageArguments.Cast<object>().ToArray());
            return true;
        }

        public static bool TryCreateFromWaveCSnapshot(
            PrototypeGameSessionWaveCSnapshot snapshot,
            out GameSession restored)
        {
            restored = null;
            if (snapshot == null) return false;
            var candidate = new GameSession(snapshot.RunSeed);
            if (!candidate.RestoreWaveCSnapshot(snapshot)) return false;
            restored = candidate;
            return true;
        }

        private static bool ValidateWaveCSnapshot(PrototypeGameSessionWaveCSnapshot snapshot)
        {
            int structureCount = Enum.GetValues(typeof(StructureKind)).Length;
            int techCount = Enum.GetValues(typeof(TechKind)).Length;
            if (snapshot == null ||
                snapshot.SchemaVersion != PrototypeGameSessionWaveCSnapshot.CurrentSchemaVersion ||
                snapshot.StableState == null ||
                snapshot.Day < 1 || snapshot.Day > FinalDay ||
                !InUnitRange(snapshot.Hunger) ||
                !InUnitRange(snapshot.Energy) ||
                !InUnitRange(snapshot.Daylight) ||
                !Enum.IsDefined(typeof(GamePhase), snapshot.Phase) ||
                !Enum.IsDefined(typeof(RunResult), snapshot.Result) ||
                snapshot.SignalStage < 0 || snapshot.SignalStage > 2 ||
                snapshot.Structures == null || snapshot.Structures.Length != structureCount ||
                snapshot.Researched == null || snapshot.Researched.Length != techCount ||
                snapshot.CraftedTools == null || snapshot.CraftedTools.Length != techCount ||
                snapshot.ActiveRegionProfileId == null ||
                snapshot.LastExpeditionResultId == null ||
                snapshot.CompletedEscapeId == null ||
                snapshot.LastMessageKey == null ||
                snapshot.LastMessageArguments == null ||
                snapshot.LastMessageArguments.Any(value => value == null))
            {
                return false;
            }

            bool terminal = snapshot.Result != RunResult.None;
            if (terminal != (snapshot.Phase == GamePhase.Result) ||
                (snapshot.Phase == GamePhase.Exploring &&
                 (!snapshot.HasSelectedRegion || snapshot.ExpeditionCompleted)) ||
                (snapshot.Phase != GamePhase.Exploring && snapshot.IsSwimming) ||
                (snapshot.StableState.HasPendingLoot && snapshot.Phase != GamePhase.Exploring))
            {
                return false;
            }

            if (snapshot.HasSelectedRegion)
            {
                if (!Enum.IsDefined(typeof(PrototypeExpeditionRegionId), snapshot.SelectedRegionId) ||
                    !string.Equals(
                        PrototypeExpeditionRegionCatalog.Get(snapshot.SelectedRegionId).StableId,
                        snapshot.ActiveRegionProfileId,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(snapshot.ActiveRegionProfileId))
            {
                return false;
            }

            bool rescued = snapshot.Result == RunResult.Rescued;
            bool knownEscape = PrototypeEscapeProjectCatalog.All.Any(value =>
                string.Equals(value.StableId, snapshot.CompletedEscapeId, StringComparison.Ordinal));
            if (rescued != knownEscape || (!rescued && !string.IsNullOrEmpty(snapshot.CompletedEscapeId)))
            {
                return false;
            }

            int workbenchIndex = (int)StructureKind.Workbench;
            for (int index = 0; index < techCount; index += 1)
            {
                if ((snapshot.Researched[index] && !snapshot.Structures[workbenchIndex]) ||
                    (snapshot.CraftedTools[index] && !snapshot.Researched[index]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool InUnitRange(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 100f;
        }
    }
}
