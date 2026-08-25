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
                applied = ApplyWaveCSave(desired) &&
                          string.Equals(
                              CaptureWaveCSaveFingerprint(),
                              desired.Root.PayloadFingerprint,
                              StringComparison.Ordinal);
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
            if (root == null || root.SchemaVersion != PrototypeWaveCSaveRoot.CurrentSchemaVersion ||
                string.IsNullOrWhiteSpace(root.SessionJson) ||
                string.IsNullOrWhiteSpace(root.SearchLedgerJson) ||
                string.IsNullOrWhiteSpace(root.EscapeDirectorJson) ||
                string.IsNullOrWhiteSpace(root.EndingAlbumJson) ||
                string.IsNullOrWhiteSpace(root.CampSpaceJson) ||
                string.IsNullOrWhiteSpace(root.CurrentRoomId) ||
                string.IsNullOrWhiteSpace(root.PayloadFingerprint) ||
                !string.Equals(root.PayloadFingerprint, PrototypeWaveCSaveFingerprint.Compute(root), StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryParseCanonical(root.SessionJson, out PrototypeGameSessionWaveCSnapshot sessionSnapshot) ||
                !GameSession.TryCreateFromWaveCSnapshot(sessionSnapshot, out GameSession stagedSession) ||
                !string.Equals(root.SessionJson, JsonUtility.ToJson(stagedSession.CaptureWaveCSnapshot()), StringComparison.Ordinal))
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
                EndingAlbumJson = root.EndingAlbumJson,
                CampSpace = campSnapshot
            };
            return true;
        }

        private bool ApplyWaveCSave(DecodedWaveCSave decoded)
        {
            if (decoded == null ||
                !session.RestoreWaveCSnapshot(decoded.Session) ||
                !searchNodeRuntime.RestoreSnapshot(decoded.SearchLedger) ||
                !hazardEscapeEndingRuntime.EscapeDirector.RestoreSnapshot(decoded.EscapeDirector))
            {
                return false;
            }
            endingAlbumCollection.RestoreTransientSnapshot(decoded.EndingAlbumJson);
            return RestoreCampSpaceSnapshot(decoded.CampSpace);
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
