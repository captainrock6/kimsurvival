using System;
using UnityEngine;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private sealed class DecodedWaveCSave
        {
            public PrototypeWaveCSaveRoot Root;
            public PrototypeGameSessionWaveCSnapshot Session;
            public PrototypeSearchRunSnapshot SearchLedger;
            public PrototypeEscapeProjectSaveSnapshot EscapeDirector;
            public PrototypeWaveRuntimeSnapshot WaveRuntime;
            public string EndingAlbumJson;
            public PrototypeCampSpaceSnapshot CampSpace;
        }

        public PrototypeWaveCSaveRoot CaptureWaveCSaveSnapshot()
        {
            if (session == null || searchNodeRuntime == null || hazardEscapeEndingRuntime == null ||
                endingAlbumCollection == null || campModuleExpansion == null || campPlacement == null || campUse == null)
            {
                throw new InvalidOperationException("Wave C save owners are not initialized.");
            }

            var root = new PrototypeWaveCSaveRoot
            {
                SchemaVersion = PrototypeWaveCSaveRoot.CurrentSchemaVersion,
                SessionJson = JsonUtility.ToJson(session.CaptureWaveCSnapshot()),
                SearchLedgerJson = JsonUtility.ToJson(searchNodeRuntime.Ledger.CaptureSnapshot()),
                EscapeDirectorJson = JsonUtility.ToJson(hazardEscapeEndingRuntime.EscapeDirector.CaptureSnapshot()),
                WaveRuntimeJson = JsonUtility.ToJson(hazardEscapeEndingRuntime.CaptureWaveRuntimeSnapshot()),
                EndingAlbumJson = endingAlbumCollection.CaptureSnapshot(),
                CampSpaceJson = JsonUtility.ToJson(CaptureCampSpaceSnapshot()),
                CurrentRoomId = CurrentCampRoomId
            };
            root.PayloadFingerprint = PrototypeWaveCSaveFingerprint.Compute(root);
            return root;
        }

        public string CaptureWaveCSaveJson()
        {
            return JsonUtility.ToJson(CaptureWaveCSaveSnapshot());
        }

        public string CaptureWaveCSaveFingerprint()
        {
            return CaptureWaveCSaveSnapshot().PayloadFingerprint;
        }

        public bool TryRestoreWaveCSaveJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                return TryRestoreWaveCSaveSnapshot(JsonUtility.FromJson<PrototypeWaveCSaveRoot>(json));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryRestoreWaveCSaveSnapshot(PrototypeWaveCSaveRoot root)
        {
            DecodedWaveCSave desired;
            try
            {
                if (!TryDecodeWaveCSave(root, out desired)) return false;
            }
            catch (Exception)
            {
                return false;
            }

            PrototypeWaveCSaveRoot rollbackRoot;
            DecodedWaveCSave rollback;
            try
            {
                rollbackRoot = CaptureWaveCSaveSnapshot();
            }
            catch (Exception)
            {
                return false;
            }
            if (!TryDecodeWaveCSave(rollbackRoot, out rollback)) return false;

            bool applied;
            try
            {
                applied = ApplyWaveCSave(desired) && AppliedWaveCSaveMatches(desired);
            }
            catch (Exception)
            {
                applied = false;
            }
            if (!applied)
            {
                try
                {
                    ApplyWaveCSave(rollback);
                }
                catch (Exception)
                {
                    // The rollback payload was captured and staged immediately before apply.
                    // Preserve the original failure result if an unexpected owner exception escapes.
                }
                return false;
            }
            return true;
        }

        private bool TryDecodeWaveCSave(PrototypeWaveCSaveRoot root, out DecodedWaveCSave decoded)
        {
            decoded = null;
            bool legacy = root != null && root.SchemaVersion == PrototypeWaveCSaveRoot.LegacySchemaVersion;
            bool current = root != null && root.SchemaVersion == PrototypeWaveCSaveRoot.CurrentSchemaVersion;
            if ((!legacy && !current) ||
                string.IsNullOrWhiteSpace(root.SessionJson) ||
                string.IsNullOrWhiteSpace(root.SearchLedgerJson) ||
                string.IsNullOrWhiteSpace(root.EscapeDirectorJson) ||
                (current && string.IsNullOrWhiteSpace(root.WaveRuntimeJson)) ||
                string.IsNullOrWhiteSpace(root.EndingAlbumJson) ||
                string.IsNullOrWhiteSpace(root.CampSpaceJson) ||
                string.IsNullOrWhiteSpace(root.CurrentRoomId) ||
                string.IsNullOrWhiteSpace(root.PayloadFingerprint) ||
                !string.Equals(root.PayloadFingerprint, PrototypeWaveCSaveFingerprint.Compute(root), StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryParseGameSessionSnapshot(root.SessionJson, out PrototypeGameSessionWaveCSnapshot sessionSnapshot) ||
                (legacy && sessionSnapshot.SchemaVersion != PrototypeGameSessionWaveCSnapshot.LegacySchemaVersion) ||
                (current && sessionSnapshot.SchemaVersion != PrototypeGameSessionWaveCSnapshot.CurrentSchemaVersion) ||
                !GameSession.TryCreateFromWaveCSnapshot(sessionSnapshot, out GameSession stagedSession) ||
                (current && !string.Equals(
                    root.SessionJson,
                    JsonUtility.ToJson(stagedSession.CaptureWaveCSnapshot()),
                    StringComparison.Ordinal)))
            {
                return false;
            }

            if (!TryParseCanonical(root.SearchLedgerJson, out PrototypeSearchRunSnapshot searchSnapshot) ||
                searchSnapshot.Nodes == null || searchSnapshot.Nodes.Length != PrototypeSearchRegionCatalog.Nodes.Count ||
                searchSnapshot.Regions == null || searchSnapshot.Regions.Length != PrototypeSearchRegionCatalog.All.Count)
            {
                return false;
            }
            PrototypeSearchNodeLedger stagedLedger = PrototypeSearchNodeLedger.CreateForRestore(searchSnapshot.RunSeed);
            if (!stagedLedger.RestoreSnapshot(searchSnapshot) ||
                !string.Equals(root.SearchLedgerJson, JsonUtility.ToJson(stagedLedger.CaptureSnapshot()), StringComparison.Ordinal) ||
                sessionSnapshot.RunSeed != searchSnapshot.RunSeed)
            {
                return false;
            }

            if (!TryParseCanonical(root.EscapeDirectorJson, out PrototypeEscapeProjectSaveSnapshot escapeSnapshot))
            {
                return false;
            }

            PrototypeWaveRuntimeSnapshot waveRuntimeSnapshot = null;
            if (current &&
                (!TryParseCanonical(root.WaveRuntimeJson, out waveRuntimeSnapshot) ||
                 waveRuntimeSnapshot.SchemaVersion != PrototypeWaveRuntimeSnapshot.CurrentSchemaVersion ||
                 waveRuntimeSnapshot.RunSeed != sessionSnapshot.RunSeed ||
                 !string.Equals(
                     root.EscapeDirectorJson,
                     JsonUtility.ToJson(waveRuntimeSnapshot.EscapeDirector),
                     StringComparison.Ordinal)))
            {
                return false;
            }
            var stagedEscape = new PrototypeEscapeProjectDirector();
            if (!stagedEscape.RestoreSnapshot(escapeSnapshot) ||
                !string.Equals(root.EscapeDirectorJson, JsonUtility.ToJson(stagedEscape.CaptureSnapshot()), StringComparison.Ordinal))
            {
                return false;
            }

            PrototypeEndingAlbumCollection stagedAlbum = PrototypeEndingAlbumCollection.CreateTransient(root.EndingAlbumJson);
            if (!string.Equals(root.EndingAlbumJson, stagedAlbum.CaptureSnapshot(), StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryParseCanonical(root.CampSpaceJson, out PrototypeCampSpaceSnapshot campSnapshot) ||
                !TryStageCampSpaceSnapshot(campSnapshot, out _) ||
                campSnapshot.CampUse == null ||
                !string.Equals(root.CurrentRoomId, campSnapshot.CampUse.StableRoomId, StringComparison.Ordinal))
            {
                return false;
            }

            decoded = new DecodedWaveCSave
            {
                Root = root,
                Session = sessionSnapshot,
                SearchLedger = searchSnapshot,
                EscapeDirector = escapeSnapshot,
                WaveRuntime = waveRuntimeSnapshot,
                EndingAlbumJson = root.EndingAlbumJson,
                CampSpace = campSnapshot
            };
            return true;
        }

        private bool ApplyWaveCSave(DecodedWaveCSave decoded)
        {
            if (decoded == null ||
                !session.RestoreWaveCSnapshot(decoded.Session) ||
                !searchNodeRuntime.RestoreSnapshot(decoded.SearchLedger))
            {
                return false;
            }

            bool persistenceEnabled = endingAlbumCollection.PersistenceEnabled;
            bool observationIsolation = hazardEscapeEndingRuntime.SetCompositeBranchObservationIsolation(true);
            bool campRestored;
            endingAlbumCollection.PersistenceEnabled = false;
            try
            {
                campRestored = RestoreCampSpaceSnapshot(decoded.CampSpace);
            }
            finally
            {
                hazardEscapeEndingRuntime.SetCompositeBranchObservationIsolation(observationIsolation);
                endingAlbumCollection.PersistenceEnabled = persistenceEnabled;
            }
            if (!campRestored) return false;

            if (decoded.WaveRuntime != null)
            {
                if (!hazardEscapeEndingRuntime.RestoreWaveRuntimeSnapshot(decoded.WaveRuntime)) return false;
            }
            else
            {
                hazardEscapeEndingRuntime.ResetRuntime();
                if (!hazardEscapeEndingRuntime.EscapeDirector.RestoreSnapshot(decoded.EscapeDirector)) return false;
                hazardEscapeEndingRuntime.RestoreProtectedPartPitySnapshots(
                    decoded.SearchLedger.ProtectedPartPity ?? Array.Empty<PrototypeProtectedPartPitySnapshot>());
            }
            endingAlbumCollection.RestoreTransientSnapshot(decoded.EndingAlbumJson);
            return true;
        }

        private bool AppliedWaveCSaveMatches(DecodedWaveCSave desired)
        {
            PrototypeWaveCSaveRoot actual = CaptureWaveCSaveSnapshot();
            if (desired.Root.SchemaVersion == PrototypeWaveCSaveRoot.CurrentSchemaVersion)
            {
                return string.Equals(actual.PayloadFingerprint, desired.Root.PayloadFingerprint, StringComparison.Ordinal);
            }

            actual.SchemaVersion = PrototypeWaveCSaveRoot.LegacySchemaVersion;
            actual.SessionJson = JsonUtility.ToJson(session.CaptureLegacyWaveCSnapshot());
            actual.WaveRuntimeJson = string.Empty;
            actual.PayloadFingerprint = PrototypeWaveCSaveFingerprint.Compute(actual);
            return string.Equals(actual.PayloadFingerprint, desired.Root.PayloadFingerprint, StringComparison.Ordinal);
        }

        private static bool TryParseGameSessionSnapshot(
            string json,
            out PrototypeGameSessionWaveCSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                PrototypeGameSessionWaveCSnapshot envelope =
                    JsonUtility.FromJson<PrototypeGameSessionWaveCSnapshot>(json);
                if (envelope == null) return false;
                if (envelope.SchemaVersion == PrototypeGameSessionWaveCSnapshot.CurrentSchemaVersion)
                {
                    if (!string.Equals(json, JsonUtility.ToJson(envelope), StringComparison.Ordinal)) return false;
                    snapshot = envelope;
                    return true;
                }

                if (envelope.SchemaVersion != PrototypeGameSessionWaveCSnapshot.LegacySchemaVersion ||
                    !TryParseCanonical(json, out PrototypeGameSessionWaveCLegacySnapshot legacy))
                {
                    return false;
                }

                snapshot = GameSession.MigrateLegacyWaveCSnapshot(legacy);
                return snapshot != null;
            }
            catch (Exception)
            {
                snapshot = null;
                return false;
            }
        }

        private static bool TryParseCanonical<T>(string json, out T value) where T : class
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                value = JsonUtility.FromJson<T>(json);
                return value != null && string.Equals(json, JsonUtility.ToJson(value), StringComparison.Ordinal);
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }
    }
}
