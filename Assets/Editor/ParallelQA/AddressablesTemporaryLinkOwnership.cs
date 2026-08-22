using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ParallelQA
{
    /// <summary>
    /// Addressables generates this linker file under Library, temporarily copies
    /// it into AddressableAssetSettings.ConfigFolder for a Player build, and
    /// removes it on the next Editor load. The ConfigFolder copy is therefore a
    /// generated build input, not repository-owned source.
    /// </summary>
    internal static class AddressablesTemporaryLinkOwnership
    {
        internal const string LinkPath = "Assets/AddressableAssetsData/link.xml";
        internal const string LinkMetaPath = "Assets/AddressableAssetsData/link.xml.meta";

        [Serializable]
        internal sealed class Snapshot
        {
            public string observedUtc;
            public bool linkExists;
            public long linkBytes;
            public string linkSha256;
            public bool metaExists;
            public long metaBytes;
            public string metaSha256;
            public string metaGuid;
            public string assetDatabaseGuid;
        }

        [Serializable]
        private sealed class CleanupEvidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string observedUtc;
            public string phase;
            public string ownership;
            public string officialBehavior;
            public bool deleteSucceeded;
            public string overall;
            public Snapshot generatedTemporaryCopy;
            public Snapshot canonicalAfterCleanup;
        }

        internal static Snapshot Capture()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string link = Path.Combine(root, LinkPath.Replace('/', Path.DirectorySeparatorChar));
            string meta = Path.Combine(root, LinkMetaPath.Replace('/', Path.DirectorySeparatorChar));
            return new Snapshot
            {
                observedUtc = DateTime.UtcNow.ToString("O"),
                linkExists = File.Exists(link),
                linkBytes = File.Exists(link) ? new FileInfo(link).Length : 0,
                linkSha256 = File.Exists(link) ? Sha256(link) : string.Empty,
                metaExists = File.Exists(meta),
                metaBytes = File.Exists(meta) ? new FileInfo(meta).Length : 0,
                metaSha256 = File.Exists(meta) ? Sha256(meta) : string.Empty,
                metaGuid = File.Exists(meta) ? ParseGuid(File.ReadAllText(meta)) : string.Empty,
                assetDatabaseGuid = AssetDatabase.AssetPathToGUID(
                    LinkPath,
                    AssetPathToGUIDOptions.OnlyExistingAssets)
            };
        }

        internal static void RemoveGeneratedTemporaryCopy(string phase)
        {
            Snapshot before = Capture();
            bool deleted = true;
            string root = Directory.GetParent(Application.dataPath).FullName;
            string link = Path.Combine(root, LinkPath.Replace('/', Path.DirectorySeparatorChar));
            string meta = Path.Combine(root, LinkMetaPath.Replace('/', Path.DirectorySeparatorChar));
            string guid = AssetDatabase.AssetPathToGUID(
                LinkPath,
                AssetPathToGUIDOptions.OnlyExistingAssets);

            if (!string.IsNullOrEmpty(guid))
            {
                deleted = AssetDatabase.DeleteAsset(LinkPath);
            }
            else
            {
                if (File.Exists(link)) File.Delete(link);
                if (File.Exists(meta)) File.Delete(meta);
            }

            Snapshot after = Capture();
            bool filesAbsent = !after.linkExists && !after.metaExists &&
                               string.IsNullOrEmpty(after.linkSha256) &&
                               string.IsNullOrEmpty(after.metaSha256) &&
                               string.IsNullOrEmpty(after.metaGuid);

            string runId = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
            if (!string.IsNullOrWhiteSpace(runId))
            {
                string evidenceFolder = Path.Combine(root, "Artifacts", "ParallelQA", Sanitize(runId));
                Directory.CreateDirectory(evidenceFolder);
                CleanupEvidence evidence = new CleanupEvidence
                {
                    runId = Sanitize(runId),
                    baselineCommit = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_BASELINE") ?? "unknown",
                    unityVersion = Application.unityVersion,
                    observedUtc = DateTime.UtcNow.ToString("O"),
                    phase = phase,
                    ownership = "ADDRESSABLES_GENERATED_TEMPORARY",
                    officialBehavior = "AddressablesPlayerBuildProcessor copies Library AddressablesLink/link.xml into ConfigFolder for the Player build and removes that temporary copy on Editor load.",
                    deleteSucceeded = deleted,
                    overall = deleted && filesAbsent ? "PASS" : "FAIL",
                    generatedTemporaryCopy = before,
                    canonicalAfterCleanup = after
                };
                string path = Path.Combine(evidenceFolder, "addressables-generated-link-cleanup.json");
                File.WriteAllText(path, JsonUtility.ToJson(evidence, true) + Environment.NewLine, new UTF8Encoding(false));
            }

            if (!deleted || !filesAbsent)
            {
                throw new BuildFailedException("Addressables temporary link.xml cleanup failed after Player build.");
            }
        }

        internal static bool Equivalent(Snapshot left, Snapshot right)
        {
            if (left == null || right == null) return false;
            bool leftAbsent = !left.linkExists && !left.metaExists;
            bool rightAbsent = !right.linkExists && !right.metaExists;
            if (leftAbsent || rightAbsent)
            {
                return leftAbsent && rightAbsent &&
                       string.IsNullOrEmpty(left.linkSha256) && string.IsNullOrEmpty(right.linkSha256) &&
                       string.IsNullOrEmpty(left.metaSha256) && string.IsNullOrEmpty(right.metaSha256) &&
                       string.IsNullOrEmpty(left.metaGuid) && string.IsNullOrEmpty(right.metaGuid) &&
                       string.IsNullOrEmpty(left.assetDatabaseGuid) && string.IsNullOrEmpty(right.assetDatabaseGuid);
            }

            return left.linkExists && right.linkExists && left.metaExists && right.metaExists &&
                   string.Equals(left.linkSha256, right.linkSha256, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.metaSha256, right.metaSha256, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.metaGuid, right.metaGuid, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.assetDatabaseGuid, right.assetDatabaseGuid, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(right.metaGuid, right.assetDatabaseGuid, StringComparison.OrdinalIgnoreCase);
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }

        private static string ParseGuid(string meta)
        {
            foreach (string line in (meta ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
            {
                if (line.StartsWith("guid:", StringComparison.Ordinal)) return line.Substring(5).Trim();
            }
            return string.Empty;
        }

        private static string Sanitize(string value)
        {
            string sanitized = value ?? string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars()) sanitized = sanitized.Replace(invalid, '_');
            return sanitized.Replace('.', '_').Replace(' ', '_');
        }
    }

    /// <summary>
    /// Removes only the Addressables-owned temporary ConfigFolder copy after it
    /// has served as Unity linker input. Runtime assets and Library content are
    /// not modified.
    /// </summary>
    internal sealed class AddressablesTemporaryLinkCleanupHook : IPostprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return int.MaxValue; }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            AddressablesTemporaryLinkOwnership.RemoveGeneratedTemporaryCopy("IPostprocessBuildWithReport");
        }
    }
}
