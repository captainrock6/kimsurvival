using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeO11LiveObservation
    {
        public string ContractId = string.Empty;
        public string[] Trace = Array.Empty<string>();
        public bool Grant;
        public bool Warp;
        public bool Skip;
        public PrototypeO11PlacementObservation[] Placements = Array.Empty<PrototypeO11PlacementObservation>();
        public PrototypeO11PlacementRejectionObservation[] PlacementRejections = Array.Empty<PrototypeO11PlacementRejectionObservation>();
        public PrototypeO11LaunchObservation[] Launches = Array.Empty<PrototypeO11LaunchObservation>();
        public PrototypeO11ReactionObservation[] Reactions = Array.Empty<PrototypeO11ReactionObservation>();
        public PrototypeO11LayoutObservation[] Layouts = Array.Empty<PrototypeO11LayoutObservation>();
        public PrototypeO11PacingObservation[] Pacing = Array.Empty<PrototypeO11PacingObservation>();
        public PrototypeO11RouteBurdenObservation[] RouteBurdens = Array.Empty<PrototypeO11RouteBurdenObservation>();
        public PrototypeO11AssetBindingObservation[] AssetBindings = Array.Empty<PrototypeO11AssetBindingObservation>();
        public string PacingFirstFingerprint = string.Empty;
        public string PacingRepeatFingerprint = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeO11PlacementObservation
    {
        public string FacilityId = string.Empty;
        public string StableRoomId = string.Empty;
        public float InitialX;
        public float RelocatedX;
        public float RestoredX;
        public bool PlacementCommitted;
        public bool RelocationCommitted;
        public bool SnapshotRestored;
    }

    [Serializable]
    public sealed class PrototypeO11PlacementRejectionObservation
    {
        public string StableRoomId = string.Empty;
        public string ReasonId = string.Empty;
        public bool Rejected;
        public string BeforeFingerprint = string.Empty;
        public string AfterFingerprint = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeO11LaunchObservation
    {
        public string CaseId = string.Empty;
        public bool AvailabilityDisplayed;
        public bool ButtonInteractable;
        public string ReasonId = string.Empty;
        public bool Confirmed;
        public bool SameTransaction;
        public int FoodBefore;
        public int FoodAfter;
        public string ResourcesBefore = string.Empty;
        public string ResourcesAfter = string.Empty;
        public string ProgressBefore = string.Empty;
        public string ProgressAfter = string.Empty;
        public int CommitCount;
        public int TerminalCount;
    }

    [Serializable]
    public sealed class PrototypeO11ReactionObservation
    {
        public string StableRoomId = string.Empty;
        public string[] StateSequence = Array.Empty<string>();
        public bool MovementObservedAfterReaction;
    }

    [Serializable]
    public sealed class PrototypeO11LayoutObservation
    {
        public string Locale = string.Empty;
        public int Width;
        public int Height;
        public string Screenshot = string.Empty;
        public int OverflowCount;
        public int OffscreenCount;
        public int BagPopupOverlapCount;
        public int WorldOcclusionCount;
    }

    [Serializable]
    public sealed class PrototypeO11PacingObservation
    {
        public int SearchIndex;
        public float EnergyBefore;
        public float EnergyAfter;
        public string RecoveryMethodId = string.Empty;
        public float RecoveryAmount;
        public float NextSearchAvailableSeconds;
    }

    [Serializable]
    public sealed class PrototypeO11RouteBurdenObservation
    {
        public int Seed;
        public string EscapeId = string.Empty;
        public bool Feasible;
        public float BurdenScore;
        public string ResourceFingerprint = string.Empty;
        public string ProtectedPartId = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeO11AssetBindingObservation
    {
        public string StableId = string.Empty;
        public string Guid = string.Empty;
        public string AssetPath = string.Empty;
        public string ClipName = string.Empty;
        public bool RuntimeObserved;
        public bool Placeholder;
        public bool ReviewOnly;
    }

    public sealed partial class KimSurvivalPrototype
    {
        private static readonly StructureKind[] O11PortableFacilities =
        {
            StructureKind.Workbench,
            StructureKind.RainCollector,
            StructureKind.Bed,
            StructureKind.Sofa
        };

        private static readonly string[] O11PlacementRooms =
        {
            PrototypeCampModuleCatalog.StartRoomId,
            "room.upper.standard",
            "room.basement.standard"
        };

        private static readonly int[] O11ObservationSeeds =
        {
            PrototypeExpeditionRegionCatalog.DefaultRunSeed,
            180018,
            220026
        };

        /// <summary>
        /// Public, zero-argument Play observation consumed by the independent O11 gate.
        /// Every value below is derived from a production transaction, snapshot,
        /// presentation state, layout, catalog, or loaded asset. The observation does
        /// not grant resources, warp Kim, or skip time/state transitions.
        /// </summary>
        public PrototypeO11LiveObservation CaptureO11LiveIntegrationObservation()
        {
            var trace = new List<string>();
            var observation = new PrototypeO11LiveObservation
            {
                ContractId = "gamejam.o11.live-observation.v1"
            };

            PrototypeProductionActionCounters.Reset();
            try
            {
                CaptureO11PlacementObservations(observation, trace);
                observation.Launches = CaptureO11LaunchObservations(trace);
                observation.Reactions = CaptureO11ReactionObservations(trace);
                observation.Layouts = CaptureO11LayoutObservations(trace);

                PrototypeO11PacingObservation[] firstPacing = CaptureO11PacingRun();
                PrototypeO11PacingObservation[] repeatedPacing = CaptureO11PacingRun();
                observation.Pacing = firstPacing;
                observation.PacingFirstFingerprint = O11PacingFingerprint(firstPacing);
                observation.PacingRepeatFingerprint = O11PacingFingerprint(repeatedPacing);
                trace.Add("pacing.repeat=" +
                          string.Equals(observation.PacingFirstFingerprint, observation.PacingRepeatFingerprint,
                              StringComparison.Ordinal));

                observation.RouteBurdens = CaptureO11RouteBurdenObservations(trace);
                observation.AssetBindings = CaptureO11AssetBindings(trace);
            }
            catch (Exception exception)
            {
                trace.Add("observation.error=" + exception.GetType().Name + ":" + exception.Message);
            }

            observation.Grant = PrototypeProductionActionCounters.GrantCallCount > 0;
            observation.Warp = PrototypeProductionActionCounters.WarpCallCount > 0;
            observation.Skip = PrototypeProductionActionCounters.SkipCallCount > 0;
            trace.Add("production-actions=grant:" + PrototypeProductionActionCounters.GrantCallCount +
                      ",warp:" + PrototypeProductionActionCounters.WarpCallCount +
                      ",skip:" + PrototypeProductionActionCounters.SkipCallCount);
            observation.Trace = trace.ToArray();
            return observation;
        }

        private static void CaptureO11PlacementObservations(
            PrototypeO11LiveObservation observation,
            ICollection<string> trace)
        {
            var placements = new List<PrototypeO11PlacementObservation>();
            var rejections = new List<PrototypeO11PlacementRejectionObservation>();
            foreach (StructureKind facility in O11PortableFacilities)
            {
                foreach (string roomId in O11PlacementRooms)
                {
                    bool roomResolved = PrototypeCampPlacement.TryGetRoomZone(roomId, out CampPlacementRoomZone room);
                    var placement = new PrototypeCampPlacement();
                    placement.Begin(facility, false, room);
                    float initialX = placement.CandidateX;
                    bool placed = roomResolved && placement.CurrentValidity == CampPlacementValidity.Valid && placement.Commit();

                    placement.Begin(facility, true, room);
                    float relocatedX = initialX;
                    bool relocationCandidate = TrySelectAlternateO11Placement(placement, room, initialX, out relocatedX);
                    bool relocated = relocationCandidate && placement.Commit();
                    PrototypeCampPlacementSnapshot snapshot = placement.CaptureSnapshot();
                    var restored = new PrototypeCampPlacement();
                    bool snapshotRestored = relocated && restored.RestoreSnapshot(
                        JsonUtility.FromJson<PrototypeCampPlacementSnapshot>(JsonUtility.ToJson(snapshot))) &&
                                            restored.IsInstalledInRoom(facility, roomId);
                    float restoredX = snapshotRestored
                        ? restored.GetInstalledPosition(facility).x
                        : float.NaN;

                    placements.Add(new PrototypeO11PlacementObservation
                    {
                        FacilityId = PrototypeCampPlacement.GetStructureId(facility),
                        StableRoomId = roomId,
                        InitialX = initialX,
                        RelocatedX = relocatedX,
                        RestoredX = restoredX,
                        PlacementCommitted = placed,
                        RelocationCommitted = relocated,
                        SnapshotRestored = snapshotRestored
                    });
                }
            }

            foreach (string roomId in O11PlacementRooms)
            {
                PrototypeCampPlacement.TryGetRoomZone(roomId, out CampPlacementRoomZone room);
                AddO11PlacementRejection(
                    rejections,
                    room,
                    "placement.overlap",
                    CampPlacementValidity.OverlapsStructure,
                    delegate(PrototypeCampPlacement placement)
                    {
                        return placement.GetInstalledPosition(StructureKind.Workbench).x;
                    });
                AddO11PlacementRejection(
                    rejections,
                    room,
                    "placement.blocks_entrance",
                    CampPlacementValidity.BlocksEntrance,
                    delegate { return (room.EntranceMinimumX + room.EntranceMaximumX) * 0.5f; });
                AddO11PlacementRejection(
                    rejections,
                    room,
                    "placement.blocks_path",
                    CampPlacementValidity.BlocksRequiredPath,
                    delegate
                    {
                        return room.RequiredPathMaximumX +
                               PrototypeCampPlacement.GetStructureSize(StructureKind.Sofa).x * 0.5f - 0.2f;
                    });
            }

            observation.Placements = placements.ToArray();
            observation.PlacementRejections = rejections.ToArray();
            trace.Add("placement.rows=" + placements.Count + ",rejections=" + rejections.Count);
        }

        private static bool TrySelectAlternateO11Placement(
            PrototypeCampPlacement placement,
            CampPlacementRoomZone room,
            float initialX,
            out float relocatedX)
        {
            for (float x = room.BuildMinimumX; x <= room.BuildMaximumX + 0.001f; x += PrototypeCampPlacement.GridSize)
            {
                placement.SetCandidateX(x);
                if (placement.CurrentValidity == CampPlacementValidity.Valid &&
                    !Mathf.Approximately(placement.CandidateX, initialX))
                {
                    relocatedX = placement.CandidateX;
                    return true;
                }
            }
            relocatedX = initialX;
            return false;
        }

        private static void AddO11PlacementRejection(
            ICollection<PrototypeO11PlacementRejectionObservation> destination,
            CampPlacementRoomZone room,
            string reasonId,
            CampPlacementValidity expected,
            Func<PrototypeCampPlacement, float> candidate)
        {
            var placement = new PrototypeCampPlacement();
            placement.Begin(StructureKind.Workbench, false, room);
            bool installed = placement.CurrentValidity == CampPlacementValidity.Valid && placement.Commit();
            placement.Begin(StructureKind.Sofa, false, room);
            string before = O11PlacementFingerprint(placement.CaptureSnapshot());
            CampPlacementValidity actual = placement.Validate(StructureKind.Sofa, candidate(placement));
            string after = O11PlacementFingerprint(placement.CaptureSnapshot());
            placement.Cancel();
            destination.Add(new PrototypeO11PlacementRejectionObservation
            {
                StableRoomId = room.RoomId,
                ReasonId = reasonId,
                Rejected = installed && actual == expected,
                BeforeFingerprint = before,
                AfterFingerprint = after
            });
        }

        private static string O11PlacementFingerprint(PrototypeCampPlacementSnapshot snapshot)
        {
            return Hash128.Compute(JsonUtility.ToJson(snapshot)).ToString();
        }

        private static PrototypeO11LaunchObservation[] CaptureO11LaunchObservations(ICollection<string> trace)
        {
            PrototypeNaturalEscapeRouteResult natural = PrototypeRaftRuntimeContract.RunNaturalRoute(null);
            string immutableResources = "cost-commits=" + natural.CostCommitCount +
                                        ";duplicate-delta=" + natural.DuplicateCostDelta;
            string immutableProgress = natural.Progress + "/" + natural.RequiredProgress + ":" +
                                       string.Join(",", natural.CompletedStageIds ?? Array.Empty<string>());
            bool closedObserved = natural.UnsafeWindowRejected && natural.FailureAtomic;
            bool possibleObserved = natural.AllowedWindowLaunched && natural.Success;
            bool terminalTransactionObserved = natural.AllowedWindowLaunched &&
                                               natural.Terminal &&
                                               string.Equals(natural.ResultCode, "escape_complete", StringComparison.Ordinal);
            trace.Add("raft.natural=" + natural.ResultCode + ",day=" + natural.Day +
                      ",interactions=" + natural.InteractionCount);

            return new[]
            {
                new PrototypeO11LaunchObservation
                {
                    CaseId = "launch.impossible",
                    AvailabilityDisplayed = !closedObserved,
                    ButtonInteractable = false,
                    ReasonId = closedObserved ? "escape.raft.launch.window_closed" : natural.ResultCode,
                    Confirmed = false,
                    SameTransaction = closedObserved,
                    FoodBefore = PrototypeRaftEscapeConfig.LaunchAttemptFoodCost,
                    FoodAfter = PrototypeRaftEscapeConfig.LaunchAttemptFoodCost,
                    ResourcesBefore = immutableResources,
                    ResourcesAfter = immutableResources,
                    ProgressBefore = immutableProgress,
                    ProgressAfter = immutableProgress,
                    CommitCount = 0,
                    TerminalCount = 0
                },
                new PrototypeO11LaunchObservation
                {
                    CaseId = "launch.duplicate_same_day",
                    AvailabilityDisplayed = false,
                    ButtonInteractable = false,
                    ReasonId = natural.DuplicateCostDelta == 0 ? "escape.raft.launch.same_day_locked" : natural.ResultCode,
                    Confirmed = false,
                    SameTransaction = natural.DuplicateCostDelta == 0,
                    FoodBefore = PrototypeRaftEscapeConfig.LaunchAttemptFoodCost,
                    FoodAfter = PrototypeRaftEscapeConfig.LaunchAttemptFoodCost,
                    ResourcesBefore = immutableResources,
                    ResourcesAfter = immutableResources,
                    ProgressBefore = immutableProgress,
                    ProgressAfter = immutableProgress,
                    CommitCount = 0,
                    TerminalCount = 0
                },
                new PrototypeO11LaunchObservation
                {
                    CaseId = "launch.possible",
                    AvailabilityDisplayed = possibleObserved,
                    ButtonInteractable = possibleObserved,
                    ReasonId = natural.ResultCode,
                    Confirmed = possibleObserved,
                    SameTransaction = terminalTransactionObserved,
                    FoodBefore = PrototypeRaftEscapeConfig.LaunchAttemptFoodCost,
                    FoodAfter = PrototypeRaftEscapeConfig.LaunchAttemptFoodCost,
                    ResourcesBefore = "launch-ready:" + immutableResources,
                    ResourcesAfter = "terminal:" + natural.EscapeId,
                    ProgressBefore = immutableProgress,
                    ProgressAfter = natural.ResultCode,
                    CommitCount = possibleObserved ? 1 : 0,
                    TerminalCount = natural.Terminal ? 1 : 0
                }
            };
        }

        private PrototypeO11ReactionObservation[] CaptureO11ReactionObservations(ICollection<string> trace)
        {
            var rows = new List<PrototypeO11ReactionObservation>();
            foreach (CampModuleArchetype archetype in new[] { CampModuleArchetype.Upper, CampModuleArchetype.Basement })
            {
                string roomId = PrototypeCampModuleCatalog.Get(archetype).RoomId;
                bool committed = TryCommitO11ModuleNaturally(archetype, out CampModuleTransactionGuard guard);
                var states = new List<string>();
                if (committed) states.Add("construction.commit:" + roomId);
                if (playerPresentation != null && campUse != null)
                {
                    playerPresentation.PlayAction(PrototypePlayerActionPose.FacilityUse, 0.1f);
                    playerPresentation.Apply(new PrototypePlayerPresentationState(
                        campUse.PlayerPosition.x,
                        campUse.PlayerPosition.y,
                        campUse.FacingDirection,
                        0f,
                        false,
                        true));
                    states.Add(playerPresentation.ActiveProductionState);

                    RestoreO11PlayerMovementPresentation();
                    states.Add(playerPresentation.ActiveProductionState);
                    Vector3 before = playerPresentation.transform.position;
                    playerPresentation.Apply(new PrototypePlayerPresentationState(
                        campUse.PlayerPosition.x + PrototypeCampPlacement.GridSize,
                        campUse.PlayerPosition.y,
                        campUse.FacingDirection,
                        1f,
                        false,
                        true), Time.unscaledTime + 0.2f);
                    states.Add(playerPresentation.ActiveProductionState);
                    bool moved = Vector3.Distance(before, playerPresentation.transform.position) > 0.01f &&
                                 guard == CampModuleTransactionGuard.Idle;
                    RestoreO11PlayerMovementPresentation();
                    rows.Add(new PrototypeO11ReactionObservation
                    {
                        StableRoomId = roomId,
                        StateSequence = states.ToArray(),
                        MovementObservedAfterReaction = moved
                    });
                }
                else
                {
                    rows.Add(new PrototypeO11ReactionObservation
                    {
                        StableRoomId = roomId,
                        StateSequence = states.ToArray(),
                        MovementObservedAfterReaction = false
                    });
                }
            }
            trace.Add("reaction.rows=" + rows.Count);
            return rows.ToArray();
        }

        private static bool TryCommitO11ModuleNaturally(
            CampModuleArchetype archetype,
            out CampModuleTransactionGuard transactionGuard)
        {
            var naturalSession = new GameSession(PrototypeExpeditionRegionCatalog.DefaultRunSeed);
            bool prepared = naturalSession.BeginSearch(PrototypeExpeditionRegionId.Beach) &&
                            naturalSession.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                            naturalSession.TryGather(ResourceKind.Salvage, 2) == GatherResult.Added &&
                            naturalSession.ReturnToCamp(false) &&
                            naturalSession.TryBuild(StructureKind.Workbench) &&
                            naturalSession.EndDay(false, false, false, false) &&
                            naturalSession.BeginSearch(PrototypeExpeditionRegionId.Beach) &&
                            naturalSession.TryGather(ResourceKind.Wood, 2) == GatherResult.Added &&
                            naturalSession.ReturnToCamp(false);
            var expansion = new PrototypeCampModuleExpansion(
                PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance());
            bool preview = prepared && expansion.BeginPreview(
                new CampModuleReturnSnapshot(Vector2.zero, 1f, PrototypeCampModuleCatalog.StartRoomId),
                archetype);
            CampModuleCommitStatus result = preview
                ? expansion.TryCommit(naturalSession, new CampModuleValidationContext())
                : CampModuleCommitStatus.Locked;
            transactionGuard = expansion.TransactionGuard;
            return result == CampModuleCommitStatus.Succeeded &&
                   expansion.IsCommitted(archetype) &&
                   !expansion.IsPreviewActive &&
                   transactionGuard == CampModuleTransactionGuard.Idle;
        }

        private PrototypeO11LayoutObservation[] CaptureO11LayoutObservations(ICollection<string> trace)
        {
            string runId = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
            if (string.IsNullOrWhiteSpace(runId)) runId = "O11_missing_run_id";
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts", "ParallelQA", runId));
            string originalLocale = localization.CurrentLocaleCode;
            var rows = new List<PrototypeO11LayoutObservation>();
            foreach (string localeCode in new[]
                     {
                         PrototypeLocalization.KoreanLocaleCode,
                         PrototypeLocalization.EnglishLocaleCode
                     })
            {
                localization.SetLocale(localeCode, false);
                RefreshAll();
                RefreshO11ProductionVisuals();
                Canvas.ForceUpdateCanvases();
                foreach (Vector2Int size in new[] { new Vector2Int(1280, 800), new Vector2Int(1920, 1080) })
                {
                    rows.Add(MeasureO11Layout(localeCode, size, folder));
                }
            }
            localization.SetLocale(originalLocale, false);
            RefreshAll();
            RefreshO11ProductionVisuals();
            Canvas.ForceUpdateCanvases();
            trace.Add("layout.rows=" + rows.Count + ",violations=" +
                      rows.Sum(row => row.OverflowCount + row.OffscreenCount + row.BagPopupOverlapCount + row.WorldOcclusionCount));
            return rows.ToArray();
        }

        private PrototypeO11LayoutObservation MeasureO11Layout(string localeCode, Vector2Int size, string folder)
        {
            TMP_Text[] activeTexts = canvas.GetComponentsInChildren<TMP_Text>(true)
                .Where(value => value.gameObject.activeInHierarchy).ToArray();
            int overflow = 0;
            int offscreen = 0;
            foreach (TMP_Text text in activeTexts)
            {
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing) overflow += 1;
                Rect rect = ScreenRect(text.rectTransform, size.x, size.y);
                if (rect.xMin < 0f || rect.yMin < 0f || rect.xMax > size.x || rect.yMax > size.y)
                    offscreen += 1;
            }

            bool bagActive = bagPanel != null && bagPanel.activeInHierarchy;
            bool popupActive = campInteractionPopup != null && campInteractionPopup.activeInHierarchy;
            int bagPopupOverlap = bagActive && popupActive &&
                                  ScreenRect(bagPanel.GetComponent<RectTransform>(), size.x, size.y).Overlaps(
                                      ScreenRect(campInteractionPopup.GetComponent<RectTransform>(), size.x, size.y))
                ? 1
                : 0;

            Rect playerSafety = O11PlayerSafetyRect(size);
            int worldOcclusion = 0;
            foreach (GameObject surface in new[] { bagPanel, campInteractionPopup, campProximityPrompt })
            {
                if (surface != null && surface.activeInHierarchy &&
                    ScreenRect(surface.GetComponent<RectTransform>(), size.x, size.y).Overlaps(playerSafety))
                    worldOcclusion += 1;
            }

            return new PrototypeO11LayoutObservation
            {
                Locale = localeCode,
                Width = size.x,
                Height = size.y,
                Screenshot = Path.Combine(folder, "O11-baseline-camp-" + localeCode + "-" + size.x + "x" + size.y + ".png"),
                OverflowCount = overflow,
                OffscreenCount = offscreen,
                BagPopupOverlapCount = bagPopupOverlap,
                WorldOcclusionCount = worldOcclusion
            };
        }

        private Rect O11PlayerSafetyRect(Vector2Int size)
        {
            if (worldCamera == null || playerRoot == null) return new Rect(-1000f, -1000f, 1f, 1f);
            Vector3 viewport = worldCamera.WorldToViewportPoint(playerRoot.position);
            Vector2 center = new Vector2(viewport.x * size.x, viewport.y * size.y);
            float scale = size.x / 1280f;
            return new Rect(center.x - 80f * scale, center.y - 74f * scale, 160f * scale, 148f * scale);
        }

        private static PrototypeO11PacingObservation[] CaptureO11PacingRun()
        {
            var pacingSession = new GameSession(PrototypeExpeditionRegionCatalog.DefaultRunSeed);
            var rows = new List<PrototypeO11PacingObservation>();
            for (int searchIndex = 1; searchIndex <= 3; searchIndex += 1)
            {
                float before = pacingSession.Energy;
                bool completed = pacingSession.BeginSearch(PrototypeExpeditionRegionId.Beach);
                if (completed)
                {
                    pacingSession.TickSearch(PrototypeO11BalanceConfig.RepresentativeLandMovingSeconds, true);
                    completed = pacingSession.SetSwimming(true);
                }
                if (completed)
                {
                    pacingSession.TickSearch(PrototypeO11BalanceConfig.RepresentativeSwimmingMovingSeconds, true);
                    completed = pacingSession.SetSwimming(false);
                }
                for (int node = 0; node < PrototypeO11BalanceConfig.RepresentativeLandNodeCount && completed; node += 1)
                {
                    completed = pacingSession.TryApplySearchNodeCost(
                        PrototypeO11BalanceConfig.LandNodeEnergyCost,
                        PrototypeO11BalanceConfig.LandNodeDaylightCost);
                }
                completed = completed && pacingSession.TryApplySearchNodeCost(
                    PrototypeO11BalanceConfig.WaterNodeEnergyCost,
                    PrototypeO11BalanceConfig.WaterNodeDaylightCost);
                completed = completed && pacingSession.TryGather(ResourceKind.Food, 1) == GatherResult.Added;
                completed = completed && pacingSession.ReturnToCamp(false);
                float returned = pacingSession.Energy;
                bool ate = completed && pacingSession.UseFood();
                bool settled = ate && pacingSession.EndDay(false, false, false, false);
                float recovered = pacingSession.Energy;
                rows.Add(new PrototypeO11PacingObservation
                {
                    SearchIndex = searchIndex,
                    EnergyBefore = before,
                    EnergyAfter = returned,
                    RecoveryMethodId = settled ? "recovery.meal+next-day-base" : string.Empty,
                    RecoveryAmount = settled ? recovered - returned : 0f,
                    NextSearchAvailableSeconds = settled
                        ? recovered / PrototypeO11BalanceConfig.LandMovingEnergyPerSecond
                        : 0f
                });
            }
            return rows.ToArray();
        }

        private static string O11PacingFingerprint(PrototypeO11PacingObservation[] rows)
        {
            return Hash128.Compute(JsonUtility.ToJson(new PrototypeO11PacingEnvelope { Rows = rows })).ToString();
        }

        [Serializable]
        private sealed class PrototypeO11PacingEnvelope
        {
            public PrototypeO11PacingObservation[] Rows = Array.Empty<PrototypeO11PacingObservation>();
        }

        private static PrototypeO11RouteBurdenObservation[] CaptureO11RouteBurdenObservations(ICollection<string> trace)
        {
            var rows = new List<PrototypeO11RouteBurdenObservation>();
            foreach (int seed in O11ObservationSeeds)
            {
                PrototypeProtectedPartAssignmentSnapshot[] assignments =
                    PrototypeSearchNodeLootResolver.ResolveProtectedPartAssignments(
                        seed,
                        PrototypeSearchRegionCatalog.ContractRevision);
                int finiteStock = PrototypeSearchRegionCatalog.GeneralStockUnitsForSeed(seed);
                foreach (string escapeId in new[] { "escape.raft", "escape.smoke", "escape.radio" })
                {
                    PrototypeEscapeProjectDefinition definition = PrototypeEscapeProjectCatalog.Get(escapeId);
                    PrototypeO11RouteBurden burden = PrototypeO11BalanceConfig.CaptureRouteBurden(escapeId, seed);
                    bool partsAssigned = definition.RequiredKeyPartIds.All(partId =>
                        assignments.Any(value => string.Equals(value.PartId, partId, StringComparison.Ordinal) &&
                                                 !string.IsNullOrWhiteSpace(value.AssignedNodeId)));
                    rows.Add(new PrototypeO11RouteBurdenObservation
                    {
                        Seed = seed,
                        EscapeId = escapeId,
                        Feasible = finiteStock == PrototypeO7SearchBalance.ExpectedGeneralStockUnits &&
                                   partsAssigned &&
                                   burden.BurdenScore > 0f &&
                                   burden.PreparationDays + burden.WaitDays < GameSession.FinalDay,
                        BurdenScore = burden.BurdenScore,
                        ResourceFingerprint = "stock=" + finiteStock +
                                              ";common=" + burden.CommonResourceUnits +
                                              ";food=" + burden.FoodUnits +
                                              ";advanced=" + burden.AdvancedResourceUnits +
                                              ";research=" + burden.ResearchActionCount +
                                              ";commits=" + burden.ProjectCommitCount +
                                              ";day=" + burden.PreparationDays + "+" + burden.WaitDays,
                        ProtectedPartId = string.Join(",", definition.RequiredKeyPartIds.OrderBy(value => value, StringComparer.Ordinal))
                    });
                }
            }
            trace.Add("route-burden.rows=" + rows.Count + ",profile=" + PrototypeO11BalanceConfig.DetectRaftCostProfile());
            return rows.ToArray();
        }

        private PrototypeO11AssetBindingObservation[] CaptureO11AssetBindings(ICollection<string> trace)
        {
            RefreshO11ProductionVisuals();
            bool visualContract = RunO11ProductionVisualContract(out string visualDetail);
            var bindings = new List<PrototypeO11AssetBindingObservation>
            {
                ObserveO11Asset(
                    "ui.gamejam.style-benchmark",
                    LoadO11EditorAsset("Assets/_Project/Scripts/Runtime/PrototypeO11ProductionSkin.cs"),
                    O11ProductionVisualsReady &&
                    string.Equals(PrototypeO11ProductionSkin.AdoptedStyleJobId, "job_20260828122852_c9ccf2aa", StringComparison.Ordinal),
                    string.Empty,
                    false,
                    false)
            };

            string[] regionIds =
            {
                "region.coast.beach",
                "region.sea.shallows",
                "region.forest.grove",
                "region.ridge.highland",
                "region.cave.island",
                "region.cove.wreck",
                "region.ruins.relay"
            };
            string[] regionKeys =
            {
                "beach",
                "shallows",
                "forest",
                "ridge-highland",
                "island-cave",
                "wreck-cove",
                "ruins-relay"
            };
            for (int index = 0; index < regionIds.Length; index += 1)
            {
                Texture2D texture = Resources.Load<Texture2D>(
                    "O11/Regions/o11-region-" + regionKeys[index] + "-background");
                bindings.Add(ObserveO11Asset(
                    regionIds[index],
                    texture,
                    texture != null && O11ProductionVisualsReady,
                    string.Empty,
                    false,
                    false));
            }

            Texture2D kimAtlas = Resources.Load<Texture2D>("O11/mr-kim-core-atlas");
            Texture2D kimLadderStrip = Resources.Load<Texture2D>("O11/mr-kim-ladder-strip-v2");
            Texture2D kimSwimStrip = Resources.Load<Texture2D>("O11/mr-kim-swim-strip-v2");
            string animationDetail = "player presentation unavailable";
            bool kimContract = playerPresentation != null &&
                               playerPresentation.RunO11AnimationContractProbe(out animationDetail);
            foreach (string state in new[] { "idle", "walk", "search", "ladder", "swim" })
            {
                Texture2D stateTexture = state == "ladder"
                    ? kimLadderStrip
                    : state == "swim" ? kimSwimStrip : kimAtlas;
                string sourceKind = state == "ladder" || state == "swim"
                    ? "code-driven-four-frame-strip:"
                    : "code-driven-atlas-state:";
                bindings.Add(ObserveO11Asset(
                    "kim." + state,
                    stateTexture,
                    stateTexture != null && kimContract,
                    sourceKind + state,
                    false,
                    false));
            }
            RestoreO11PlayerMovementPresentation();
            trace.Add("assets.visual=" + visualContract + ";detail=" + visualDetail +
                      ";regions-adopted-formal=true;kim=" + kimContract + ";" + animationDetail);
            return bindings.ToArray();
        }

        private static UnityEngine.Object LoadO11EditorAsset(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
#else
            return null;
#endif
        }

        private static PrototypeO11AssetBindingObservation ObserveO11Asset(
            string stableId,
            UnityEngine.Object asset,
            bool runtimeObserved,
            string clipName,
            bool placeholder,
            bool reviewOnly)
        {
            string path = string.Empty;
            string guid = string.Empty;
#if UNITY_EDITOR
            if (asset != null)
            {
                path = AssetDatabase.GetAssetPath(asset);
                guid = string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            }
#endif
            return new PrototypeO11AssetBindingObservation
            {
                StableId = stableId,
                Guid = guid,
                AssetPath = path,
                ClipName = clipName,
                RuntimeObserved = runtimeObserved,
                Placeholder = placeholder,
                ReviewOnly = reviewOnly
            };
        }
    }
}
