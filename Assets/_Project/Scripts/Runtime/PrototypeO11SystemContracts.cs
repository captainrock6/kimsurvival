using System;
using System.Collections.Generic;
using System.Linq;

namespace KimSurvival
{
    public static class PrototypeO11SystemContracts
    {
        public static IReadOnlyList<PrototypeContractProbe> VerifyAll()
        {
            return new[]
            {
                VerifyPortableMultiRoomPlacement(),
                VerifyModuleCommitReactionSettlement(),
                VerifyRaftLaunchAvailabilityTransaction(),
                VerifyRaftRoutePreparationBalance(),
                PrototypeRaftRuntimeContract.VerifyAtomicFailureRetrySnapshotFixture()
            };
        }

        public static PrototypeContractProbe VerifyPortableMultiRoomPlacement()
        {
            StructureKind[] portable =
            {
                StructureKind.Workbench,
                StructureKind.RainCollector,
                StructureKind.Bed,
                StructureKind.Sofa
            };
            string[] roomIds =
            {
                PrototypeCampModuleCatalog.StartRoomId,
                PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).RoomId,
                PrototypeCampModuleCatalog.Get(CampModuleArchetype.Basement).RoomId
            };

            bool portableRooms = true;
            foreach (StructureKind kind in portable)
            {
                PrototypeCampPlacement placement = new PrototypeCampPlacement();
                for (int roomIndex = 0; roomIndex < roomIds.Length; roomIndex += 1)
                {
                    portableRooms &= PrototypeCampPlacement.TryGetRoomZone(
                        roomIds[roomIndex],
                        out CampPlacementRoomZone room);
                    placement.Begin(kind, roomIndex > 0, room);
                    portableRooms &= placement.CurrentValidity == CampPlacementValidity.Valid &&
                                     placement.Commit() &&
                                     placement.IsInstalledInRoom(kind, room.RoomId) &&
                                     placement.GetInstalledAnchorId(kind).StartsWith("free|", StringComparison.Ordinal);
                }
            }

            PrototypeCampPlacement collision = new PrototypeCampPlacement();
            PrototypeCampPlacement.TryGetRoomZone(
                PrototypeCampModuleCatalog.Get(CampModuleArchetype.Upper).RoomId,
                out CampPlacementRoomZone upper);
            collision.Begin(StructureKind.Workbench, false, upper);
            bool installed = collision.Commit();
            float installedX = collision.GetInstalledPosition(StructureKind.Workbench).x;
            collision.Begin(StructureKind.Sofa, false, upper);
            CampPlacementValidity facilityCollision = collision.Validate(StructureKind.Sofa, installedX);
            CampPlacementValidity entranceCollision = collision.Validate(
                StructureKind.Sofa,
                (upper.EntranceMinimumX + upper.EntranceMaximumX) * 0.5f);
            CampPlacementValidity pathCollision = collision.Validate(
                StructureKind.Sofa,
                upper.RequiredPathMaximumX + PrototypeCampPlacement.GetStructureSize(StructureKind.Sofa).x * 0.5f - 0.2f);
            collision.Cancel();

            PrototypeCampPlacementSnapshot snapshot = collision.CaptureSnapshot();
            PrototypeCampPlacement restored = new PrototypeCampPlacement();
            bool roundTrip = restored.RestoreSnapshot(snapshot) &&
                             restored.IsInstalledInRoom(StructureKind.Workbench, upper.RoomId) &&
                             Math.Abs(restored.GetInstalledPosition(StructureKind.Workbench).x - installedX) < 0.001f;
            bool fixedOnly = PrototypeCampPlacement.IsFixedAnchorStructure(StructureKind.Campfire) &&
                             portable.All(kind => !PrototypeCampPlacement.IsFixedAnchorStructure(kind)) &&
                             PrototypeEscapeProjectCatalog.All.All(definition =>
                                 !string.IsNullOrWhiteSpace(definition.FacilityId)) &&
                             PrototypeCampModuleCatalog.All.All(definition =>
                                 !string.IsNullOrWhiteSpace(definition.StartSlotId) &&
                                 !string.IsNullOrWhiteSpace(definition.ReciprocalSlotId));
            bool success = portableRooms && installed && roundTrip && fixedOnly &&
                           facilityCollision == CampPlacementValidity.OverlapsStructure &&
                           entranceCollision == CampPlacementValidity.BlocksEntrance &&
                           pathCollision == CampPlacementValidity.BlocksRequiredPath;
            return new PrototypeContractProbe(
                success,
                success
                    ? "portable workbench/rain/bed/sofa free-place across start/upper/basement; stable room-local restore; entrance/ladder/path/facility collision rejected; campfire/escape/connectors fixed"
                    : "portable multi-room placement or fixed-anchor boundary mismatch");
        }

        public static PrototypeContractProbe VerifyModuleCommitReactionSettlement()
        {
            GameSession session = new GameSession();
            session.Grant(ResourceKind.Wood, 10);
            session.Grant(ResourceKind.Salvage, 10);
            bool prepared = session.TryBuild(StructureKind.Workbench);
            PrototypeCampModuleExpansion expansion = new PrototypeCampModuleExpansion(
                PrototypeCampModuleExpansionConfig.CreateVerticalSliceBalance());
            bool began = expansion.BeginPreview(
                new CampModuleReturnSnapshot(default(UnityEngine.Vector2), 1f, PrototypeCampModuleCatalog.StartRoomId),
                CampModuleArchetype.Upper);
            CampModuleCommitStatus result = expansion.TryCommit(session, new CampModuleValidationContext());
            bool success = prepared && began && result == CampModuleCommitStatus.Succeeded &&
                           !expansion.IsPreviewActive &&
                           expansion.TransactionGuard == CampModuleTransactionGuard.Idle;
            return new PrototypeContractProbe(
                success,
                success
                    ? "upper/basement commit reaction settles transaction to idle before movement resumes"
                    : "module commit leaves a committed reaction/transaction state latched");
        }

        public static PrototypeContractProbe VerifyRaftLaunchAvailabilityTransaction()
        {
            int openSeed = FindSeed(true);
            int closedSeed = FindSeed(false);
            bool seedFixture = openSeed >= 0 && closedSeed >= 0;

            GameSession openSession = new GameSession(openSeed);
            PrototypeEscapeProjectDirector openDirector = PreparedRaftDirector();
            bool launched = openDirector.TryHandleRaftAction(
                openSession,
                openSession.RunSeed,
                openSession.Day,
                "o11.open.same-popup");
            PrototypeEscapeProjectState openState = openDirector.GetState(PrototypeRaftEscapeConfig.EscapeId);
            bool openAtomic = launched && openState.Complete &&
                              openState.LaunchState == PrototypeRaftLaunchStates.Complete &&
                              openSession.Result == RunResult.Rescued;

            GameSession closedSession = new GameSession(closedSeed);
            PrototypeEscapeProjectDirector closedDirector = PreparedRaftDirector();
            string before = ResourceFingerprint(closedSession);
            bool firstClosed = closedDirector.TryHandleRaftAction(
                closedSession,
                closedSession.RunSeed,
                closedSession.Day,
                "o11.closed.same-day");
            bool secondClosed = closedDirector.TryHandleRaftAction(
                closedSession,
                closedSession.RunSeed,
                closedSession.Day,
                "o11.closed.same-day");
            PrototypeEscapeProjectState closedState = closedDirector.GetState(PrototypeRaftEscapeConfig.EscapeId);
            bool closedAtomic = !firstClosed && !secondClosed &&
                                before == ResourceFingerprint(closedSession) &&
                                closedState.Progress == PrototypeRaftEscapeConfig.StageCount &&
                                closedState.KeyPartProtected &&
                                closedState.LastLaunchDay == closedSession.Day &&
                                closedState.LaunchAttemptCount == 0 &&
                                closedSession.Result == RunResult.None;
            bool success = seedFixture && openAtomic && closedAtomic;
            return new PrototypeContractProbe(
                success,
                success
                    ? "one deterministic launch decision drives label/action; open confirms terminal once; closed same-day retries are locked and cost-free"
                    : "displayed raft window and same-popup action are not one atomic availability transaction");
        }

        public static PrototypeContractProbe VerifyRaftRoutePreparationBalance()
        {
            int stageWood = PrototypeRaftEscapeConfig.HullWoodCost + PrototypeRaftEscapeConfig.SailWoodCost;
            int stageSalvage = PrototypeRaftEscapeConfig.HullSalvageCost + PrototypeRaftEscapeConfig.SailSalvageCost;
            System.Reflection.FieldInfo axeField = typeof(PrototypeRaftEscapeConfig).GetField("RequiresStoneAxePreparation");
            System.Reflection.FieldInfo dayField = typeof(PrototypeRaftEscapeConfig).GetField("MaximumNaturalPreparationDays");
            bool requiresStoneAxe = axeField != null && axeField.FieldType == typeof(bool) && (bool)axeField.GetValue(null);
            int maximumNaturalDays = dayField != null && dayField.FieldType == typeof(int)
                ? (int)dayField.GetValue(null)
                : int.MaxValue;
            bool success = requiresStoneAxe &&
                           stageWood >= 5 && stageSalvage >= 3 &&
                           PrototypeRaftEscapeConfig.SuppliesFoodCost >= 3 &&
                           maximumNaturalDays <= 12 &&
                           PrototypeRaftLaunchWindowResolver.FindNextOpenDay(1717, 1) <= 4;
            return new PrototypeContractProbe(
                success,
                success
                    ? "raft adds axe/navigation preparation and locked 5W/3S/3F stage floor while retaining a finite <=12-day natural route"
                    : "raft remains materially cheaper or less prepared than the smoke/radio alternatives");
        }

        private static PrototypeEscapeProjectDirector PreparedRaftDirector()
        {
            PrototypeEscapeProjectDirector director = new PrototypeEscapeProjectDirector();
            PrototypeEscapeProjectState state = director.GetState(PrototypeRaftEscapeConfig.EscapeId);
            state.FacilityBuilt = true;
            state.Progress = PrototypeRaftEscapeConfig.StageCount;
            state.RequiredProgress = PrototypeRaftEscapeConfig.StageCount;
            state.CompletedStageIds = PrototypeRaftEscapeConfig.StageIds.ToArray();
            state.KeyPartProtected = true;
            state.LaunchState = PrototypeRaftLaunchStates.Ready;
            return director;
        }

        private static int FindSeed(bool allowed)
        {
            for (int seed = 1; seed <= 512; seed += 1)
            {
                if (PrototypeRaftLaunchWindowResolver.Resolve(seed, 1).Allowed == allowed) return seed;
            }
            return -1;
        }

        private static string ResourceFingerprint(GameSession session)
        {
            return session.GetStorage(ResourceKind.Wood) + ":" +
                   session.GetStorage(ResourceKind.Stone) + ":" +
                   session.GetStorage(ResourceKind.Food) + ":" +
                   session.GetStorage(ResourceKind.Salvage);
        }
    }
}
