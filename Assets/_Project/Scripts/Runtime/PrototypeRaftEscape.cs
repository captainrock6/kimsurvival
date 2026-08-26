using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KimSurvival
{
    // Prototype-only SAMPLE values. Wave 20 intentionally keeps these outside the
    // campaign rules so final balance can replace them without rewriting the state machine.
    public static class PrototypeRaftEscapeConfig
    {
        public const string EscapeId = "escape.raft";
        public const string KeyPartId = "part.raft.sailcloth";
        public const int StageCount = 3;
        public const int HullWoodCost = 2;
        public const int HullSalvageCost = 1;
        public const int SailWoodCost = 1;
        public const int SailSalvageCost = 1;
        public const int SuppliesFoodCost = 2;
        public const int LaunchAttemptFoodCost = 1;

        public static readonly string[] StageIds =
        {
            "raft.stage.hull",
            "raft.stage.sail",
            "raft.stage.supplies"
        };
    }

    public static class PrototypeRaftLaunchStates
    {
        public const string Staging = "raft.launch.staging";
        public const string Ready = "raft.launch.ready";
        public const string Confirm = "raft.launch.confirm";
        public const string Failed = "raft.launch.failed";
        public const string Complete = "raft.launch.complete";
    }

    [Serializable]
    public sealed class PrototypeRaftLaunchWindow
    {
        public int Seed;
        public int Day;
        public string WeatherId = string.Empty;
        public string CurrentId = string.Empty;
        public bool Allowed;
        public string ResultCode = string.Empty;
    }

    public static class PrototypeRaftLaunchWindowResolver
    {
        public static PrototypeRaftLaunchWindow Resolve(int seed, int day)
        {
            int weatherHash = PrototypeExpeditionRegionCatalog.StableHash(seed, "escape.raft", "weather.day." + day);
            int currentOffset = PrototypeExpeditionRegionCatalog.PositiveModulo(seed, 3);
            bool outbound = PrototypeExpeditionRegionCatalog.PositiveModulo(day + currentOffset, 3) == 0;
            // An outbound window is always navigable; other days retain seed-driven sea state.
            // This guarantees a retry window within three days without removing forecast variance.
            bool calm = outbound || PrototypeExpeditionRegionCatalog.PositiveModulo(weatherHash, 3) != 0;
            return new PrototypeRaftLaunchWindow
            {
                Seed = seed,
                Day = day,
                WeatherId = calm ? "raft.weather.calm" : "raft.weather.rough",
                CurrentId = outbound ? "raft.current.outbound" : "raft.current.cross",
                Allowed = calm && outbound,
                ResultCode = calm && outbound ? "escape.raft.window.open" : "escape.raft.window.closed"
            };
        }

        public static int FindNextOpenDay(int seed, int startingDay)
        {
            for (int day = Math.Max(1, startingDay); day <= GameSession.FinalDay; day += 1)
            {
                if (Resolve(seed, day).Allowed) return day;
            }
            return -1;
        }
    }

    [Serializable]
    public sealed class PrototypeEscapeProjectSaveSnapshot
    {
        public int SchemaVersion = 1;
        public PrototypeEscapeProjectState[] Projects = Array.Empty<PrototypeEscapeProjectState>();
        public string[] CommittedEventKeys = Array.Empty<string>();
    }

    public sealed partial class PrototypeEscapeProjectDirector
    {
        public PrototypeEscapeProjectSaveSnapshot CaptureSnapshot()
        {
            return new PrototypeEscapeProjectSaveSnapshot
            {
                Projects = states.Values.OrderBy(value => value.StableId, StringComparer.Ordinal)
                    .Select(CloneState).ToArray(),
                CommittedEventKeys = committedEventKeys.OrderBy(value => value, StringComparer.Ordinal).ToArray()
            };
        }

        public bool RestoreSnapshot(PrototypeEscapeProjectSaveSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SchemaVersion != 1 || snapshot.Projects == null ||
                snapshot.Projects.Any(value => value == null || string.IsNullOrEmpty(value.StableId) ||
                    !PrototypeEscapeProjectCatalog.All.Any(definition => definition.StableId == value.StableId)))
            {
                return false;
            }

            states.Clear();
            committedEventKeys.Clear();
            foreach (PrototypeEscapeProjectState project in snapshot.Projects)
            {
                states[project.StableId] = CloneState(project);
            }
            if (snapshot.CommittedEventKeys != null)
            {
                foreach (string eventKey in snapshot.CommittedEventKeys.Where(value => !string.IsNullOrEmpty(value)))
                {
                    committedEventKeys.Add(eventKey);
                }
            }
            return true;
        }

        public void SynchronizeRaftSailcloth(bool protectedOwned)
        {
            PrototypeEscapeProjectState state = GetState(PrototypeRaftEscapeConfig.EscapeId);
            state.KeyPartProtected |= protectedOwned;
            if (state.KeyPartProtected && state.LastResultCode == "escape.raft.requirement.sailcloth")
            {
                state.LastResultCode = "escape.project.ready";
            }
        }

        public bool TryHandleRaftAction(GameSession session, int seed, int day, string eventKeyPrefix)
        {
            PrototypeEscapeProjectState state = GetState(PrototypeRaftEscapeConfig.EscapeId);
            if (state.Progress < PrototypeRaftEscapeConfig.StageCount)
            {
                return TryCommitRaftStage(session, state.Progress, eventKeyPrefix + ".stage." + state.Progress);
            }

            if (state.LaunchState == PrototypeRaftLaunchStates.Failed)
            {
                state.LaunchState = PrototypeRaftLaunchStates.Ready;
                state.LastResultCode = "escape.raft.launch.retry_ready";
                return true;
            }

            if (state.LaunchState == PrototypeRaftLaunchStates.Ready)
            {
                state.LaunchState = PrototypeRaftLaunchStates.Confirm;
                state.LastResultCode = "escape.raft.launch.confirm";
                return true;
            }

            if (state.LaunchState == PrototypeRaftLaunchStates.Confirm)
            {
                return TryConfirmRaftLaunch(
                    session,
                    seed,
                    day,
                    eventKeyPrefix + ".launch." + state.LaunchAttemptCount + ".day." + day);
            }
            return false;
        }

        public bool TryCommitRaftStage(GameSession session, int stageIndex, string eventKey)
        {
            PrototypeEscapeProjectState state = GetState(PrototypeRaftEscapeConfig.EscapeId);
            if (session == null || session.Phase != GamePhase.Camp || session.Result != RunResult.None ||
                stageIndex != state.Progress || stageIndex < 0 || stageIndex >= PrototypeRaftEscapeConfig.StageCount)
            {
                return false;
            }
            if (committedEventKeys.Contains(eventKey)) return true;

            int wood = stageIndex == 0 ? PrototypeRaftEscapeConfig.HullWoodCost :
                stageIndex == 1 ? PrototypeRaftEscapeConfig.SailWoodCost : 0;
            int salvage = stageIndex == 0 ? PrototypeRaftEscapeConfig.HullSalvageCost :
                stageIndex == 1 ? PrototypeRaftEscapeConfig.SailSalvageCost : 0;
            int food = stageIndex == 2 ? PrototypeRaftEscapeConfig.SuppliesFoodCost : 0;
            if (stageIndex == 1 && !session.HasRope)
            {
                state.LastResultCode = "escape.raft.requirement.rope";
                return false;
            }
            if (stageIndex == 1 && !state.KeyPartProtected)
            {
                state.LastResultCode = "escape.raft.requirement.sailcloth";
                return false;
            }
            if (!session.TrySpendResources(wood, 0, food, salvage))
            {
                state.LastResultCode = "escape.raft.requirement.resources";
                return false;
            }

            committedEventKeys.Add(eventKey);
            state.Progress += 1;
            state.CompletedStageIds = PrototypeRaftEscapeConfig.StageIds.Take(state.Progress).ToArray();
            state.LaunchState = state.Progress >= PrototypeRaftEscapeConfig.StageCount
                ? PrototypeRaftLaunchStates.Ready
                : PrototypeRaftLaunchStates.Staging;
            state.LastResultCode = state.Progress >= PrototypeRaftEscapeConfig.StageCount
                ? "escape.raft.stages.complete"
                : "escape.project.progress";
            return true;
        }

        public bool TryConfirmRaftLaunch(GameSession session, int seed, int day, string eventKey)
        {
            PrototypeEscapeProjectState state = GetState(PrototypeRaftEscapeConfig.EscapeId);
            if (!string.IsNullOrEmpty(eventKey) && committedEventKeys.Contains(eventKey)) return true;
            if (session == null || session.Phase != GamePhase.Camp || session.Result != RunResult.None ||
                state.Progress != PrototypeRaftEscapeConfig.StageCount || !state.KeyPartProtected ||
                state.LaunchState != PrototypeRaftLaunchStates.Confirm || day != session.Day)
            {
                return false;
            }
            if (!session.TrySpendResources(0, 0, PrototypeRaftEscapeConfig.LaunchAttemptFoodCost, 0))
            {
                state.LastResultCode = "escape.raft.requirement.launch_cost";
                return false;
            }

            committedEventKeys.Add(eventKey);
            PrototypeRaftLaunchWindow window = PrototypeRaftLaunchWindowResolver.Resolve(seed, day);
            state.LaunchAttemptCount += 1;
            state.LastLaunchDay = day;
            state.LastWeatherId = window.WeatherId;
            state.LastCurrentId = window.CurrentId;
            if (!window.Allowed)
            {
                state.LaunchState = PrototypeRaftLaunchStates.Failed;
                state.LastResultCode = "escape.raft.launch.failed_window";
                return false;
            }

            if (!session.TryCompleteEscapeProject(PrototypeRaftEscapeConfig.EscapeId))
            {
                state.LastResultCode = "escape.raft.launch.terminal_rejected";
                return false;
            }
            state.Complete = true;
            state.LaunchState = PrototypeRaftLaunchStates.Complete;
            state.LastResultCode = "escape.project.complete";
            return true;
        }

        private static PrototypeEscapeProjectState CloneState(PrototypeEscapeProjectState source)
        {
            return new PrototypeEscapeProjectState
            {
                StableId = source.StableId,
                Progress = source.Progress,
                RequiredProgress = source.RequiredProgress,
                Complete = source.Complete,
                LastResultCode = source.LastResultCode,
                CompletedStageIds = source.CompletedStageIds == null ? Array.Empty<string>() : source.CompletedStageIds.ToArray(),
                KeyPartProtected = source.KeyPartProtected,
                LaunchState = source.LaunchState,
                LaunchAttemptCount = source.LaunchAttemptCount,
                LastLaunchDay = source.LastLaunchDay,
                LastWeatherId = source.LastWeatherId,
                LastCurrentId = source.LastCurrentId
            };
        }
    }

    public static class PrototypeRaftRuntimeContract
    {
        public const string CancelAtomicityContract = "cancel preserves resources and completed stages";
        public const string FailureAtomicityContract = "failure applies one declared consequence";
        public const string DuplicateSuppressionContract = "duplicate cost and terminal submissions are idempotent";
        public const string SaveRestoreContract = "save restore preserves stages, protected sailcloth and launch window";

        public static PrototypeContractProbe VerifyAtomicFailureRetrySnapshotFixture()
        {
            PrototypeNaturalEscapeRouteResult result = RunNaturalRoute(null);
            bool success = result.Success && result.Completed && result.Terminal && !result.Grant && !result.Warp &&
                           result.ResultCode == "escape_complete" && result.InteractionTrace.Contains("raft.failure.cost.once") &&
                           result.InteractionTrace.Contains("raft.snapshot.restored") && result.InteractionTrace.Contains("raft.key-part.protected");
            return new PrototypeContractProbe(success,
                "escape.raft natural shore-launch hull sail supplies protected-sailcloth weather current " +
                "failure-cost-once snapshot-restore retry early-terminal grant=false warp=false result=" + result.ResultCode);
        }

        public static PrototypeNaturalEscapeRouteResult RunNaturalRoute(IReadOnlyList<PrototypeCampInteractionTarget> liveTargets)
        {
            GameSession session = new GameSession(PrototypeExpeditionRegionCatalog.DefaultRunSeed);
            PrototypeEscapeProjectDirector director = new PrototypeEscapeProjectDirector();
            PrototypeKeyPartPityState pity = new PrototypeKeyPartPityState
            {
                StableId = "part.pity." + PrototypeRaftEscapeConfig.KeyPartId,
                KeyPartId = PrototypeRaftEscapeConfig.KeyPartId
            };
            List<string> trace = new List<string>();
            bool prepared = PrepareNaturalMaterialsAndSailcloth(session, director, pity, trace);
            PrototypeCampInteractionTarget target = FindShoreLaunch(liveTargets);
            PrototypeCampInteraction interaction = new PrototypeCampInteraction();
            int interactions = 0;
            string beforeCancel = JsonUtility.ToJson(director.CaptureSnapshot());
            int cancelWood = session.GetStorage(ResourceKind.Wood);
            int cancelFood = session.GetStorage(ResourceKind.Food);
            bool cancelUnchanged = prepared && CancelInteract(interaction, target, trace, ref interactions) &&
                                   beforeCancel == JsonUtility.ToJson(director.CaptureSnapshot()) &&
                                   cancelWood == session.GetStorage(ResourceKind.Wood) &&
                                   cancelFood == session.GetStorage(ResourceKind.Food);
            int costCommitCount = 0;
            int duplicateCostDelta = 0;

            for (int stage = 0; stage < PrototypeRaftEscapeConfig.StageCount && prepared; stage += 1)
            {
                string eventKey = "natural.raft.stage." + stage;
                prepared = Interact(
                    interaction,
                    target,
                    director,
                    session,
                    eventKey,
                    trace,
                    ref interactions);
                if (prepared)
                {
                    costCommitCount += 1;
                    trace.Add(PrototypeRaftEscapeConfig.StageIds[stage]);
                    int afterWood = session.GetStorage(ResourceKind.Wood);
                    int afterSalvage = session.GetStorage(ResourceKind.Salvage);
                    int afterFood = session.GetStorage(ResourceKind.Food);
                    director.TryCommitRaftStage(session, stage, eventKey);
                    duplicateCostDelta += Math.Abs(session.GetStorage(ResourceKind.Wood) - afterWood) +
                                          Math.Abs(session.GetStorage(ResourceKind.Salvage) - afterSalvage) +
                                          Math.Abs(session.GetStorage(ResourceKind.Food) - afterFood);
                }
            }

            if (prepared && PrototypeRaftLaunchWindowResolver.Resolve(session.RunSeed, session.Day).Allowed)
            {
                prepared = AdvanceUntil(session, false);
            }
            prepared &= Interact(interaction, target, director, session, "natural.raft.arm.failure", trace, ref interactions);
            int foodBeforeFailure = session.GetStorage(ResourceKind.Food);
            string failureKey = "natural.raft.launch.failure.day." + session.Day;
            bool failedAttempt = prepared && !director.TryConfirmRaftLaunch(session, session.RunSeed, session.Day, failureKey);
            int foodAfterFailure = session.GetStorage(ResourceKind.Food);
            bool duplicateAccepted = director.TryConfirmRaftLaunch(session, session.RunSeed, session.Day, failureKey);
            int foodAfterDuplicate = session.GetStorage(ResourceKind.Food);
            PrototypeEscapeProjectState failedState = director.GetState(PrototypeRaftEscapeConfig.EscapeId);
            bool failureAtomic = failedAttempt && duplicateAccepted &&
                                 foodBeforeFailure - foodAfterFailure == PrototypeRaftEscapeConfig.LaunchAttemptFoodCost &&
                                 foodAfterDuplicate == foodAfterFailure && failedState.Progress == PrototypeRaftEscapeConfig.StageCount &&
                                 failedState.KeyPartProtected && failedState.LaunchState == PrototypeRaftLaunchStates.Failed;
            if (failureAtomic)
            {
                trace.Add("raft.weather.current.window.unsafe-rejected");
                trace.Add("raft.failure.cost.once");
            }

            string json = JsonUtility.ToJson(director.CaptureSnapshot());
            PrototypeEscapeProjectDirector restored = new PrototypeEscapeProjectDirector();
            bool restoredOk = restored.RestoreSnapshot(JsonUtility.FromJson<PrototypeEscapeProjectSaveSnapshot>(json));
            PrototypeEscapeProjectState restoredState = restored.GetState(PrototypeRaftEscapeConfig.EscapeId);
            restoredOk &= restoredState.Progress == PrototypeRaftEscapeConfig.StageCount && restoredState.KeyPartProtected &&
                          restoredState.LaunchAttemptCount == 1 && restoredState.LaunchState == PrototypeRaftLaunchStates.Failed;
            if (restoredOk) trace.Add("raft.snapshot.restored");

            bool advanced = failureAtomic && restoredOk && AdvanceUntil(session, true);
            bool retryReady = advanced && restored.TryHandleRaftAction(session, session.RunSeed, session.Day, "natural.raft.retry");
            bool confirmation = retryReady && restored.TryHandleRaftAction(session, session.RunSeed, session.Day, "natural.raft.confirm");
            bool launched = confirmation && restored.TryConfirmRaftLaunch(
                session,
                session.RunSeed,
                session.Day,
                "natural.raft.launch.success.day." + session.Day);
            PrototypeEscapeProjectState finalState = restored.GetState(PrototypeRaftEscapeConfig.EscapeId);
            bool complete = launched && finalState.Complete && finalState.LaunchState == PrototypeRaftLaunchStates.Complete &&
                            session.Result == RunResult.Rescued && session.CompletedEscapeId == PrototypeRaftEscapeConfig.EscapeId &&
                            session.Day < GameSession.FinalDay;
            if (complete) trace.Add("raft.weather.current.window.allowed-launch");

            int terminalDay = session.Day;
            string terminalEscapeId = session.CompletedEscapeId;
            bool duplicateTerminalAccepted = restored.TryConfirmRaftLaunch(
                session,
                session.RunSeed,
                session.Day,
                "natural.raft.launch.success.day." + session.Day);
            int duplicateTerminalDelta = duplicateTerminalAccepted && session.Day == terminalDay &&
                                         session.CompletedEscapeId == terminalEscapeId ? 0 : 1;

            const string raftEndingId = "ending.escape.raft.open-water";
            PrototypeEndingAlbumCollection album = PrototypeEndingAlbumCollection.CreateTransient();
            int beforeUnlock = album.UnlockedCount;
            bool firstUnlock = complete && album.UnlockForVerification(raftEndingId, session.Day, "2026-08-25T00:00:00.000Z");
            int albumUnlockDelta = album.UnlockedCount - beforeUnlock;
            int beforeDuplicateUnlock = album.UnlockedCount;
            album.UnlockForVerification(raftEndingId, session.Day, "2026-08-25T00:00:00.000Z");
            int duplicateAlbumDelta = album.UnlockedCount - beforeDuplicateUnlock;
            PrototypeEndingAlbumCollection restoredAlbum = PrototypeEndingAlbumCollection.CreateTransient(album.CaptureSnapshot());
            bool albumRestored = restoredAlbum.IsUnlocked(raftEndingId);
            return new PrototypeNaturalEscapeRouteResult
            {
                StableId = "smoke.route.raft",
                InteractionTrace = trace.ToArray(),
                Success = complete,
                Completed = complete,
                Terminal = session.Result == RunResult.Rescued,
                Grant = false,
                Warp = false,
                InteractionCount = interactions,
                EscapeId = PrototypeRaftEscapeConfig.EscapeId,
                ResultCode = complete ? "escape_complete" : finalState.LastResultCode,
                Progress = finalState.Progress,
                RequiredProgress = finalState.RequiredProgress,
                CompletedStageIds = finalState.CompletedStageIds == null
                    ? Array.Empty<string>()
                    : finalState.CompletedStageIds.ToArray(),
                EndingId = complete ? raftEndingId : string.Empty,
                Day = session.Day,
                Skip = false,
                UnsafeWindowRejected = failureAtomic,
                AllowedWindowLaunched = complete,
                CancelUnchanged = cancelUnchanged,
                FailureAtomic = failureAtomic,
                FailureApplications = failedAttempt ? 1 : 0,
                CostCommitCount = costCommitCount,
                DuplicateCostDelta = duplicateCostDelta,
                DuplicateTerminalDelta = duplicateTerminalDelta,
                EarlyEscape = complete && session.Day < GameSession.FinalDay,
                RestoreSame = restoredOk,
                AlbumUnlockDelta = firstUnlock ? albumUnlockDelta : 0,
                DuplicateAlbumDelta = duplicateAlbumDelta,
                AlbumRestored = albumRestored,
                ProtectedKeyPartIds = finalState.KeyPartProtected
                    ? new[] { PrototypeRaftEscapeConfig.KeyPartId }
                    : Array.Empty<string>(),
                RestoredStageIds = restoredState.CompletedStageIds == null
                    ? Array.Empty<string>()
                    : restoredState.CompletedStageIds.ToArray()
            };
        }

        private static bool PrepareNaturalMaterialsAndSailcloth(
            GameSession session,
            PrototypeEscapeProjectDirector director,
            PrototypeKeyPartPityState pity,
            ICollection<string> trace)
        {
            for (int search = 1; search <= CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount; search += 1)
            {
                if (!session.BeginSearch(PrototypeExpeditionRegionId.Shallows)) return false;
                bool gathered = search == 1
                    ? Gather(session, ResourceKind.Wood, 2) && Gather(session, ResourceKind.Salvage, 2) &&
                      Gather(session, ResourceKind.Salvage, 2) && Gather(session, ResourceKind.Food, 2)
                    : Gather(session, ResourceKind.Wood, 2) && Gather(session, ResourceKind.Wood, 2) &&
                      Gather(session, ResourceKind.Salvage, 2) && Gather(session, ResourceKind.Food, 2);
                if (!gathered || !session.ReturnToCamp(false)) return false;
                pity.RecordSearch("natural.raft.shallows." + search, true, true, false, false, pity.ProtectedOwned);
                director.SynchronizeRaftSailcloth(pity.ProtectedOwned);
                if (search == 1 &&
                    (!session.TryBuild(StructureKind.Workbench) || !session.TryResearch(TechKind.Rope) || !session.TryCraft(TechKind.Rope)))
                {
                    return false;
                }
                if (search < CampaignKeyPartPityConfig.EligibleGuaranteeSearchCount)
                {
                    session.UseFood();
                    if (!session.EndDay(false, false)) return false;
                }
            }
            if (pity.ProtectedOwned) trace.Add("raft.key-part.protected");
            return pity.ProtectedOwned && session.HasRope;
        }

        private static bool Gather(GameSession session, ResourceKind kind, int amount)
        {
            return session.TryGather(kind, amount) == GatherResult.Added;
        }

        private static bool Interact(
            PrototypeCampInteraction interaction,
            PrototypeCampInteractionTarget target,
            PrototypeEscapeProjectDirector director,
            GameSession session,
            string eventKey,
            ICollection<string> trace,
            ref int interactions)
        {
            interaction.UpdateSelection(target.Position + Vector2.left * 0.5f, 1f, new[] { target });
            bool opened = interaction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ShoreLaunch && interaction.TryOpenPopup();
            bool confirmed = opened && interaction.TryConfirmAction();
            bool action = confirmed && PrototypeCampInteractionCatalog.OwnsAction(
                PrototypeCampInteractionTargetKind.ShoreLaunch,
                PrototypeCampInteractionAction.ProgressRaftEscape,
                true) && director.TryHandleRaftAction(session, session.RunSeed, session.Day, eventKey);
            if (opened) { trace.Add("camp.interaction.escape.raft.popup-opened"); interactions += 1; }
            if (confirmed) { trace.Add("camp.interaction.escape.raft.action-confirmed"); interactions += 1; }
            interaction.ClosePopup();
            return action;
        }

        private static bool CancelInteract(
            PrototypeCampInteraction interaction,
            PrototypeCampInteractionTarget target,
            ICollection<string> trace,
            ref int interactions)
        {
            interaction.UpdateSelection(target.Position + Vector2.left * 0.5f, 1f, new[] { target });
            bool opened = interaction.ActiveTargetKind == PrototypeCampInteractionTargetKind.ShoreLaunch && interaction.TryOpenPopup();
            if (opened)
            {
                trace.Add("camp.interaction.escape.raft.popup-cancelled");
                interactions += 1;
            }
            interaction.ClosePopup();
            return opened && !interaction.IsPopupOpen;
        }

        private static PrototypeCampInteractionTarget FindShoreLaunch(IReadOnlyList<PrototypeCampInteractionTarget> liveTargets)
        {
            if (liveTargets != null)
            {
                for (int index = 0; index < liveTargets.Count; index += 1)
                {
                    if (liveTargets[index].Kind == PrototypeCampInteractionTargetKind.ShoreLaunch)
                    {
                        PrototypeCampInteractionTarget live = liveTargets[index];
                        return new PrototypeCampInteractionTarget(
                            live.Id,
                            live.Kind,
                            live.Position,
                            true,
                            live.SelectionPriority);
                    }
                }
            }
            return new PrototypeCampInteractionTarget(
                "facility.shore-launch",
                PrototypeCampInteractionTargetKind.ShoreLaunch,
                new Vector2(-5.3f, PrototypeCampUse.PlayerFloorY));
        }

        private static bool AdvanceUntil(GameSession session, bool requireOpen)
        {
            for (int guard = 0; guard < 12; guard += 1)
            {
                bool open = PrototypeRaftLaunchWindowResolver.Resolve(session.RunSeed, session.Day).Allowed;
                if (open == requireOpen) return true;
                session.UseFood();
                if (!session.EndDay(false, false) || session.Result != RunResult.None) return false;
                if (!session.BeginSearch(PrototypeExpeditionRegionId.Shallows) || !session.ReturnToCamp(false)) return false;
            }
            return false;
        }
    }
}
