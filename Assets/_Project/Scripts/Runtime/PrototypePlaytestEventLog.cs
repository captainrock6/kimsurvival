using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace KimSurvival
{
    public static class PrototypePlaytestEventNames
    {
        public const string LogStarted = "log.started";
        public const string LogStopped = "log.stopped";
        public const string SessionStarted = "session.started";
        public const string DayChanged = "day.changed";
        public const string DaySurvived = "day.survived";
        public const string PhaseChanged = "phase.changed";
        public const string ResourceChanged = "resource.changed";
        public const string FacilityProximityEntered = "facility.proximity.entered";
        public const string FacilityProximityExited = "facility.proximity.exited";
        public const string FacilityPopupOpened = "facility.popup.opened";
        public const string FacilityPopupClosed = "facility.popup.closed";
        public const string FacilityActionCompleted = "facility.action.completed";
        public const string FacilityActionRejected = "facility.action.rejected";
        public const string CraftingCompleted = "crafting.completed";
        public const string ResearchCompleted = "research.completed";
        public const string BagCapacityUpgraded = "bag.capacity.upgraded";
        public const string SwimmingEntered = "swimming.entered";
        public const string SwimmingExited = "swimming.exited";
        public const string VineBarrierBlocked = "vine_barrier.blocked";
        public const string VineBarrierCleared = "vine_barrier.cleared";
        public const string SignalStageOneCompleted = "signal.stage1.completed";
        public const string SignalStageTwoCompleted = "signal.stage2.completed";
        public const string ExpeditionRegionSelected = "expedition.region.selected";
        public const string ExpeditionStarted = "expedition.started";
        public const string ExpeditionResultResolved = "expedition.result.resolved";
        public const string RunCompleted = "run.completed";
    }

    [Serializable]
    public sealed class PrototypePlaytestStateFingerprint
    {
        public string fingerprint = string.Empty;
        public int day;
        public string phase = string.Empty;
        public string result = string.Empty;
        public float hunger;
        public float energy;
        public float daylight;
        public bool expedition_completed;
        public bool swimming;
        public int signal_stage;
        public int active_bag_slots;
        public int storage_wood;
        public int storage_stone;
        public int storage_food;
        public int storage_salvage;
        public int bag_wood;
        public int bag_stone;
        public int bag_food;
        public int bag_salvage;
        public bool campfire;
        public bool workbench;
        public bool rain_collector;
        public bool research_stone_axe;
        public bool research_rope;
        public bool crafted_stone_axe;
        public bool crafted_rope;
        public string pending_resource = string.Empty;
        public int pending_amount;
        public int run_seed;
        public string region_id = string.Empty;
        public string profile_id = string.Empty;
        public string expedition_result_id = string.Empty;

        public static PrototypePlaytestStateFingerprint Capture(GameSession session)
        {
            PrototypePlaytestStateFingerprint state = new PrototypePlaytestStateFingerprint
            {
                day = session.Day,
                phase = StableName(session.Phase),
                result = StableName(session.Result),
                hunger = session.Hunger,
                energy = session.Energy,
                daylight = session.Daylight,
                expedition_completed = session.ExpeditionCompleted,
                swimming = session.IsSwimming,
                signal_stage = session.SignalStage,
                active_bag_slots = session.ActiveBagSlotCount,
                storage_wood = session.GetStorage(ResourceKind.Wood),
                storage_stone = session.GetStorage(ResourceKind.Stone),
                storage_food = session.GetStorage(ResourceKind.Food),
                storage_salvage = session.GetStorage(ResourceKind.Salvage),
                campfire = session.HasStructure(StructureKind.Campfire),
                workbench = session.HasStructure(StructureKind.Workbench),
                rain_collector = session.HasStructure(StructureKind.RainCollector),
                research_stone_axe = session.HasResearched(TechKind.StoneAxe),
                research_rope = session.HasResearched(TechKind.Rope),
                crafted_stone_axe = session.HasCrafted(TechKind.StoneAxe),
                crafted_rope = session.HasCrafted(TechKind.Rope),
                pending_resource = session.PendingKind.HasValue ? StableName(session.PendingKind.Value) : string.Empty,
                pending_amount = session.PendingAmount,
                run_seed = session.RunSeed,
                region_id = session.SelectedRegionId.HasValue
                    ? PrototypeExpeditionRegionCatalog.Get(session.SelectedRegionId.Value).StableId
                    : string.Empty,
                profile_id = session.ActiveRegionProfileId,
                expedition_result_id = session.LastExpeditionResultId
            };

            for (int index = 0; index < session.ActiveBagSlotCount; index += 1)
            {
                BagStack stack = session.GetBagSlot(index);
                if (stack.IsEmpty)
                {
                    continue;
                }

                switch (stack.Kind)
                {
                    case ResourceKind.Wood:
                        state.bag_wood += stack.Amount;
                        break;
                    case ResourceKind.Stone:
                        state.bag_stone += stack.Amount;
                        break;
                    case ResourceKind.Food:
                        state.bag_food += stack.Amount;
                        break;
                    case ResourceKind.Salvage:
                        state.bag_salvage += stack.Amount;
                        break;
                }
            }

            state.fingerprint = ComputeFingerprint(state.CanonicalValue());
            return state;
        }

        private string CanonicalValue()
        {
            return string.Join("|", new[]
            {
                day.ToString(CultureInfo.InvariantCulture), phase, result,
                hunger.ToString("0.###", CultureInfo.InvariantCulture),
                energy.ToString("0.###", CultureInfo.InvariantCulture),
                daylight.ToString("0.###", CultureInfo.InvariantCulture),
                expedition_completed ? "1" : "0", swimming ? "1" : "0",
                signal_stage.ToString(CultureInfo.InvariantCulture),
                active_bag_slots.ToString(CultureInfo.InvariantCulture),
                storage_wood.ToString(CultureInfo.InvariantCulture),
                storage_stone.ToString(CultureInfo.InvariantCulture),
                storage_food.ToString(CultureInfo.InvariantCulture),
                storage_salvage.ToString(CultureInfo.InvariantCulture),
                bag_wood.ToString(CultureInfo.InvariantCulture),
                bag_stone.ToString(CultureInfo.InvariantCulture),
                bag_food.ToString(CultureInfo.InvariantCulture),
                bag_salvage.ToString(CultureInfo.InvariantCulture),
                campfire ? "1" : "0", workbench ? "1" : "0", rain_collector ? "1" : "0",
                research_stone_axe ? "1" : "0", research_rope ? "1" : "0",
                crafted_stone_axe ? "1" : "0", crafted_rope ? "1" : "0",
                pending_resource, pending_amount.ToString(CultureInfo.InvariantCulture),
                run_seed.ToString(CultureInfo.InvariantCulture), region_id, profile_id, expedition_result_id
            });
        }

        private static string ComputeFingerprint(string canonical)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                StringBuilder result = new StringBuilder(hash.Length * 2);
                for (int index = 0; index < hash.Length; index += 1)
                {
                    result.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
                }
                return result.ToString();
            }
        }

        internal static string StableName<T>(T value)
        {
            return value.ToString().ToLowerInvariant();
        }
    }

    [Serializable]
    public sealed class PrototypePlaytestEventRecord
    {
        public int schema_version = 1;
        public int sequence;
        public string run_id = string.Empty;
        public string utc = string.Empty;
        public string event_name = string.Empty;
        public string locale = string.Empty;
        public string input_device = string.Empty;
        public string target_kind = string.Empty;
        public string target_id = string.Empty;
        public string action = string.Empty;
        public string outcome = string.Empty;
        public string resource = string.Empty;
        public string resource_location = string.Empty;
        public int delta;
        public int run_seed;
        public string region_id = string.Empty;
        public string profile_id = string.Empty;
        public string result_id = string.Empty;
        public PrototypePlaytestStateFingerprint state_before;
        public PrototypePlaytestStateFingerprint state_after;
    }

    public sealed class PrototypePlaytestEventRecorder : IDisposable
    {
        private interface ILineSink : IDisposable
        {
            void WriteLine(string line);
        }

        private sealed class MemoryLineSink : ILineSink
        {
            public readonly List<string> Lines = new List<string>();

            public void WriteLine(string line)
            {
                Lines.Add(line);
            }

            public void Dispose()
            {
            }
        }

#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        private sealed class FileLineSink : ILineSink
        {
            private readonly StreamWriter writer;

            public FileLineSink(string path)
            {
                writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                writer.AutoFlush = true;
            }

            public void WriteLine(string line)
            {
                writer.WriteLine(line);
            }

            public void Dispose()
            {
                writer.Dispose();
            }
        }
#endif

        private readonly GameSession session;
        private readonly Func<string> localeProvider;
        private readonly Func<PrototypeInputDevice> inputDeviceProvider;
        private readonly Func<DateTime> utcProvider;
        private readonly ILineSink sink;
        private readonly string runId;
        private readonly MemoryLineSink verificationSink;
        private PrototypePlaytestStateFingerprint observedState;
        private string observedTargetKind = string.Empty;
        private string observedTargetId = string.Empty;
        private int sequence;
        private bool disposed;
        private bool sinkFailed;

        private PrototypePlaytestEventRecorder(
            GameSession session,
            Func<string> localeProvider,
            Func<PrototypeInputDevice> inputDeviceProvider,
            Func<DateTime> utcProvider,
            ILineSink sink,
            MemoryLineSink verificationSink,
            string runId)
        {
            this.session = session;
            this.localeProvider = localeProvider;
            this.inputDeviceProvider = inputDeviceProvider;
            this.utcProvider = utcProvider;
            this.sink = sink;
            this.verificationSink = verificationSink;
            this.runId = runId;
            observedState = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.LogStarted, observedState, observedState);
        }

        public static bool ProductionEnabled
        {
            get
            {
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static PrototypePlaytestEventRecorder CreateDevelopment(
            GameSession session,
            Func<string> localeProvider,
            Func<PrototypeInputDevice> inputDeviceProvider)
        {
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "PlaytestLogs");
                Directory.CreateDirectory(directory);
                string runId = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture) + "-" +
                               Guid.NewGuid().ToString("N").Substring(0, 8);
                string path = Path.Combine(directory, "kim-survival-playtest-" + runId + ".jsonl");
                PrototypePlaytestEventRecorder recorder = new PrototypePlaytestEventRecorder(
                    session,
                    localeProvider,
                    inputDeviceProvider,
                    delegate { return DateTime.UtcNow; },
                    new FileLineSink(path),
                    null,
                    runId);
                Debug.Log("[Kim Survival Playtest] Development-only local JSONL: " + path);
                return recorder;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Kim Survival Playtest] Local log disabled after file initialization failed: " + exception.Message);
                return null;
            }
#else
            return null;
#endif
        }

        public static PrototypePlaytestEventRecorder CreateForVerification(
            GameSession session,
            Func<string> localeProvider,
            Func<PrototypeInputDevice> inputDeviceProvider)
        {
            MemoryLineSink memory = new MemoryLineSink();
            return new PrototypePlaytestEventRecorder(
                session,
                localeProvider,
                inputDeviceProvider,
                delegate { return new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc); },
                memory,
                memory,
                "verification-run");
        }

        public IReadOnlyList<string> VerificationLines
        {
            get { return verificationSink == null ? Array.Empty<string>() : verificationSink.Lines; }
        }

        public void RecordSessionStarted()
        {
            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.SessionStarted, current, current);
            observedState = current;
        }

        public void ObserveState(string action = "")
        {
            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            EmitDerivedEvents(observedState, current, action);
            observedState = current;
        }

        public void ObserveFacilityTarget(PrototypeCampInteractionTargetKind kind, string targetId, bool hasPrompt)
        {
            string nextKind = hasPrompt ? PrototypePlaytestStateFingerprint.StableName(kind) : string.Empty;
            string nextId = hasPrompt ? targetId ?? string.Empty : string.Empty;
            if (string.Equals(nextKind, observedTargetKind, StringComparison.Ordinal) &&
                string.Equals(nextId, observedTargetId, StringComparison.Ordinal))
            {
                return;
            }

            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            if (!string.IsNullOrEmpty(observedTargetId))
            {
                Write(PrototypePlaytestEventNames.FacilityProximityExited, current, current, observedTargetKind, observedTargetId);
            }
            if (!string.IsNullOrEmpty(nextId))
            {
                Write(PrototypePlaytestEventNames.FacilityProximityEntered, current, current, nextKind, nextId);
            }

            observedTargetKind = nextKind;
            observedTargetId = nextId;
        }

        public void RecordPopupOpened(PrototypeCampInteractionTargetKind kind, string targetId)
        {
            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.FacilityPopupOpened, current, current,
                PrototypePlaytestStateFingerprint.StableName(kind), targetId);
            ObserveFacilityTarget(PrototypeCampInteractionTargetKind.None, string.Empty, false);
        }

        public void RecordPopupClosed(PrototypeCampInteractionTargetKind kind, string targetId, string outcome)
        {
            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.FacilityPopupClosed, current, current,
                PrototypePlaytestStateFingerprint.StableName(kind), targetId, string.Empty, outcome);
        }

        public bool TrackFacilityAction(
            PrototypeCampInteractionTargetKind kind,
            string targetId,
            string action,
            Func<bool> operation)
        {
            PrototypePlaytestStateFingerprint before = PrototypePlaytestStateFingerprint.Capture(session);
            bool succeeded = operation();
            PrototypePlaytestStateFingerprint after = PrototypePlaytestStateFingerprint.Capture(session);
            string outcome = succeeded ? "completed" : "rejected";
            Write(succeeded ? PrototypePlaytestEventNames.FacilityActionCompleted : PrototypePlaytestEventNames.FacilityActionRejected,
                before, after, PrototypePlaytestStateFingerprint.StableName(kind), targetId, action, outcome);
            EmitDerivedEvents(before, after, action);
            observedState = after;
            return succeeded;
        }

        public void TrackFacilityTransition(
            PrototypeCampInteractionTargetKind kind,
            string targetId,
            string action,
            Action operation)
        {
            PrototypePlaytestStateFingerprint before = PrototypePlaytestStateFingerprint.Capture(session);
            operation();
            PrototypePlaytestStateFingerprint after = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.FacilityActionCompleted, before, after,
                PrototypePlaytestStateFingerprint.StableName(kind), targetId, action, "completed");
            EmitDerivedEvents(before, after, action);
            observedState = after;
        }

        public void RecordVineBarrierBlocked()
        {
            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.VineBarrierBlocked, current, current,
                "vine_barrier", "exploration.vine_barrier", "move", "blocked");
        }

        public void RecordVineBarrierCleared()
        {
            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.VineBarrierCleared, current, current,
                "vine_barrier", "exploration.vine_barrier", "move", "cleared");
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            PrototypePlaytestStateFingerprint current = PrototypePlaytestStateFingerprint.Capture(session);
            Write(PrototypePlaytestEventNames.LogStopped, current, current);
            disposed = true;
            try
            {
                sink.Dispose();
            }
            catch (Exception)
            {
            }
        }

        private void EmitDerivedEvents(
            PrototypePlaytestStateFingerprint before,
            PrototypePlaytestStateFingerprint after,
            string action)
        {
            if (before.day != after.day)
            {
                Write(PrototypePlaytestEventNames.DayChanged, before, after, action: action, outcome: after.day.ToString(CultureInfo.InvariantCulture));
                if (after.day > before.day && after.result == "none")
                {
                    Write(PrototypePlaytestEventNames.DaySurvived, before, after, action: action, outcome: before.day.ToString(CultureInfo.InvariantCulture));
                }
            }
            if (!string.Equals(before.phase, after.phase, StringComparison.Ordinal))
            {
                Write(PrototypePlaytestEventNames.PhaseChanged, before, after, action: action, outcome: after.phase);
            }
            if (!string.Equals(before.region_id, after.region_id, StringComparison.Ordinal) && !string.IsNullOrEmpty(after.region_id))
            {
                Write(PrototypePlaytestEventNames.ExpeditionRegionSelected, before, after,
                    targetKind: "expedition_region", targetId: after.region_id, action: action, outcome: "selected");
            }
            if (before.phase != "exploring" && after.phase == "exploring" && !string.IsNullOrEmpty(after.profile_id))
            {
                Write(PrototypePlaytestEventNames.ExpeditionStarted, before, after,
                    targetKind: "expedition_region", targetId: after.region_id, action: action, outcome: after.profile_id);
            }
            if (!string.Equals(before.expedition_result_id, after.expedition_result_id, StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(after.expedition_result_id))
            {
                Write(PrototypePlaytestEventNames.ExpeditionResultResolved, before, after,
                    targetKind: "expedition_region", targetId: after.region_id, action: action, outcome: after.expedition_result_id);
            }

            EmitResourceDelta(before, after, "wood", "storage", before.storage_wood, after.storage_wood, action);
            EmitResourceDelta(before, after, "stone", "storage", before.storage_stone, after.storage_stone, action);
            EmitResourceDelta(before, after, "food", "storage", before.storage_food, after.storage_food, action);
            EmitResourceDelta(before, after, "salvage", "storage", before.storage_salvage, after.storage_salvage, action);
            EmitResourceDelta(before, after, "wood", "bag", before.bag_wood, after.bag_wood, action);
            EmitResourceDelta(before, after, "stone", "bag", before.bag_stone, after.bag_stone, action);
            EmitResourceDelta(before, after, "food", "bag", before.bag_food, after.bag_food, action);
            EmitResourceDelta(before, after, "salvage", "bag", before.bag_salvage, after.bag_salvage, action);

            if (before.swimming != after.swimming)
            {
                Write(after.swimming ? PrototypePlaytestEventNames.SwimmingEntered : PrototypePlaytestEventNames.SwimmingExited,
                    before, after, action: action, outcome: after.swimming ? "swimming" : "land");
            }
            if (!before.research_stone_axe && after.research_stone_axe)
            {
                Write(PrototypePlaytestEventNames.ResearchCompleted, before, after, action: action, outcome: "stone_axe");
            }
            if (!before.research_rope && after.research_rope)
            {
                Write(PrototypePlaytestEventNames.ResearchCompleted, before, after, action: action, outcome: "rope");
            }
            if (!before.crafted_stone_axe && after.crafted_stone_axe)
            {
                Write(PrototypePlaytestEventNames.CraftingCompleted, before, after, action: action, outcome: "stone_axe");
            }
            if (!before.crafted_rope && after.crafted_rope)
            {
                Write(PrototypePlaytestEventNames.CraftingCompleted, before, after, action: action, outcome: "rope");
            }
            if (before.active_bag_slots < after.active_bag_slots)
            {
                Write(PrototypePlaytestEventNames.BagCapacityUpgraded, before, after, action: action,
                    outcome: before.active_bag_slots + "_to_" + after.active_bag_slots);
            }
            if (before.signal_stage != after.signal_stage)
            {
                Write(after.signal_stage == 1 ? PrototypePlaytestEventNames.SignalStageOneCompleted : PrototypePlaytestEventNames.SignalStageTwoCompleted,
                    before, after, action: action, outcome: after.signal_stage.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.Equals(before.result, after.result, StringComparison.Ordinal) && after.result != "none")
            {
                Write(PrototypePlaytestEventNames.RunCompleted, before, after, action: action, outcome: after.result);
            }
        }

        private void EmitResourceDelta(
            PrototypePlaytestStateFingerprint before,
            PrototypePlaytestStateFingerprint after,
            string resource,
            string location,
            int beforeAmount,
            int afterAmount,
            string action)
        {
            int change = afterAmount - beforeAmount;
            if (change == 0)
            {
                return;
            }

            Write(PrototypePlaytestEventNames.ResourceChanged, before, after,
                action: action,
                outcome: change > 0 ? "increased" : "decreased",
                resource: resource,
                resourceLocation: location,
                delta: change);
        }

        private void Write(
            string eventName,
            PrototypePlaytestStateFingerprint before,
            PrototypePlaytestStateFingerprint after,
            string targetKind = "",
            string targetId = "",
            string action = "",
            string outcome = "",
            string resource = "",
            string resourceLocation = "",
            int delta = 0)
        {
            if (disposed || sinkFailed)
            {
                return;
            }

            PrototypePlaytestEventRecord record = new PrototypePlaytestEventRecord
            {
                sequence = ++sequence,
                run_id = runId,
                utc = utcProvider().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                event_name = eventName,
                locale = localeProvider() ?? string.Empty,
                input_device = inputDeviceProvider() == PrototypeInputDevice.Gamepad ? "gamepad" : "keyboard_mouse",
                target_kind = targetKind ?? string.Empty,
                target_id = targetId ?? string.Empty,
                action = action ?? string.Empty,
                outcome = outcome ?? string.Empty,
                resource = resource ?? string.Empty,
                resource_location = resourceLocation ?? string.Empty,
                delta = delta,
                run_seed = after.run_seed,
                region_id = after.region_id ?? string.Empty,
                profile_id = after.profile_id ?? string.Empty,
                result_id = after.expedition_result_id ?? string.Empty,
                state_before = before,
                state_after = after
            };

            try
            {
                sink.WriteLine(JsonUtility.ToJson(record));
            }
            catch (Exception)
            {
                sinkFailed = true;
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
                Debug.LogWarning("[Kim Survival Playtest] Local log disabled after a write failure.");
#endif
            }
        }
    }
}
