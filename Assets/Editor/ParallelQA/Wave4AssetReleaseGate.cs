using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ParallelQA
{
    /// <summary>
    /// Non-shipping Wave 4 contract gate for adopted Forge packages, Unity
    /// importer policy, Addressables linker stability, and Windows release
    /// evidence. It never changes runtime, scene, or art source assets.
    /// </summary>
    public static class Wave4AssetReleaseGate
    {
        private const string AssetLedgerPath = ".forge/assets.json";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string LinkPath = "Assets/AddressableAssetsData/link.xml";
        private const string LinkMetaPath = "Assets/AddressableAssetsData/link.xml.meta";
        private const string BackgroundAssetId = "background.island-camp";
        private const string StructureAssetId = "object.camp-structures";

        [Serializable]
        private sealed class AssetLedger
        {
            public LedgerAsset[] assets;
        }

        [Serializable]
        private sealed class LedgerAsset
        {
            public string id;
            public string assetType;
            public string status;
            public string currentJobId;
            public LedgerEngine engine;
            public LedgerArtifact[] artifacts;
        }

        [Serializable]
        private sealed class LedgerEngine
        {
            public string kind;
            public string packagePath;
            public string manifest;
        }

        [Serializable]
        private sealed class LedgerArtifact
        {
            public string kind;
            public string fileName;
            public long bytes;
        }

        [Serializable]
        private sealed class ForgeImportManifest
        {
            public string assetId;
            public string jobId;
            public string assetType;
            public string[] sourceFiles;
            public string[] rejectFiles;
            public ImportPolicy import;
        }

        [Serializable]
        private sealed class ImportPolicy
        {
            public string textureType;
            public string spriteMode;
            public float pixelsPerUnit;
            public string filterMode;
            public string compression;
            public int maxSize;
            public bool mipmaps;
            public bool alphaIsTransparency;
        }

        [Serializable]
        private sealed class QualityReport
        {
            public string grade;
            public float score;
            public string jobId;
            public string assetType;
            public QualitySummary summary;
            public QualityFile[] files;
        }

        [Serializable]
        private sealed class QualitySummary
        {
            public int files;
            public int errors;
            public int warnings;
        }

        [Serializable]
        private sealed class QualityFile
        {
            public string fileName;
            public string kind;
            public int width;
            public int height;
            public bool hasAlpha;
            public QualityIssue[] issues;
        }

        [Serializable]
        private sealed class QualityIssue
        {
            public string code;
            public string severity;
        }

        [Serializable]
        private sealed class StructureMetadata
        {
            public string assetId;
            public string jobId;
            public float pixelsPerUnit;
            public StructurePart[] parts;
        }

        [Serializable]
        private sealed class StructurePart
        {
            public string id;
            public string file;
            public float[] pivotNormalized;
        }

        [Serializable]
        private sealed class LayerManifest
        {
            public string assetId;
            public string jobId;
            public LayerCanvas canvas;
            public LayerEntry[] layers;
        }

        [Serializable]
        private sealed class LayerCanvas
        {
            public int width;
            public int height;
            public string origin;
        }

        [Serializable]
        private sealed class LayerEntry
        {
            public string file;
            public int order;
            public bool opaque;
        }

        [Serializable]
        private sealed class CheckRecord
        {
            public string id;
            public string category;
            public string status;
            public string severity;
            public string expected;
            public string actual;
            public string path;
        }

        [Serializable]
        private sealed class FileRecord
        {
            public string assetId;
            public string jobId;
            public string path;
            public long bytes;
            public string sha256;
            public string guid;
        }

        [Serializable]
        private sealed class AssetContractReport
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string command;
            public string overall;
            public int passed;
            public int failed;
            public int unverified;
            public string physicalGamepad;
            public string[] joystickNames;
            public CheckRecord[] checks;
            public FileRecord[] files;
            public AddressSnapshot addressablesAtContract;
        }

        [Serializable]
        private sealed class PreflightReport
        {
            public int schemaVersion;
            public string runId;
            public string baselineCommit;
            public AddressSnapshot addressables;
            public VisualBaseline visualGate;
        }

        [Serializable]
        private sealed class VisualBaseline
        {
            public string sourcePath;
            public string sourceSha256;
            public string reportPath;
            public string reportSha256;
            public string overall;
            public string thresholds;
        }

        [Serializable]
        private sealed class VisualFact
        {
            public string status;
            public int targets;
            public int failures;
            public string evidenceLine;
        }

        [Serializable]
        private sealed class CurrentVisualFacts
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string observedUtc;
            public string source;
            public string standardKoEnOverall;
            public string qpsLongOverall;
            public string overall;
            public VisualFact placement;
            public VisualFact explorationSwimming;
            public VisualFact searchTray;
            public VisualFact qpsLong;
        }

        [Serializable]
        public sealed class AddressSnapshot
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
        private sealed class AddressBuildContract
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string overall;
            public bool preflightToBeforeStable;
            public bool beforeToAfterStable;
            public bool temporaryCopyCleanupPassed;
            public string temporaryCopyCleanupEvidence;
            public AddressSnapshot preflight;
            public AddressSnapshot beforeBuild;
            public AddressSnapshot afterBuild;
        }

        [Serializable]
        private sealed class BuildEvidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string command;
            public string target;
            public string options;
            public string result;
            public ulong totalSizeBytes;
            public double durationSeconds;
            public int errors;
            public int warnings;
            public string[] warningInventory;
            public string executable;
            public bool executableExists;
            public long executableBytes;
            public string executableSha256;
            public string addressablesLinkContract;
        }

        [Serializable]
        private sealed class TemporaryCleanupEvidence
        {
            public string overall;
        }

        [Serializable]
        private sealed class SteamArea
        {
            public string area;
            public string status;
            public string evidence;
        }

        [Serializable]
        private sealed class SteamReadiness
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string observedUtc;
            public string overall;
            public string scanScope;
            public string[] matchedFiles;
            public SteamArea[] areas;
        }

        private static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave4" : Sanitize(value);
            }
        }

        private static string BaselineCommit
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_BASELINE");
                return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
            }
        }

        private static string EvidenceFolder
        {
            get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); }
        }

        private static string WorkFolder
        {
            get { return Path.Combine(ProjectRoot, "work", "ParallelQA", RunId); }
        }

        private static string BuildFolder
        {
            get { return Path.Combine(ProjectRoot, "work", "ParallelQA", "StableWindowsBuild"); }
        }

        public static void RunAssetContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            Directory.CreateDirectory(WorkFolder);
            List<CheckRecord> checks = new List<CheckRecord>();
            List<FileRecord> files = new List<FileRecord>();

            PreflightReport preflight = ReadJson<PreflightReport>(Path.Combine(EvidenceFolder, "wave5-preflight.json"));
            AddressSnapshot addressAtContract = CaptureAddressSnapshot();
            bool addressMatchesPreflight = preflight != null && SameAddress(preflight.addressables, addressAtContract);
            AddCheck(checks, "addressables.preflight_stability", "addressables", addressMatchesPreflight, "P1",
                "Addressables-owned temporary link.xml is absent and its empty SHA/GUID state is unchanged after Editor load",
                AddressComparison(preflight == null ? null : preflight.addressables, addressAtContract), LinkPath);

            string visualGatePath = ToFull("Assets/Editor/ParallelQA/Wave3VisualGate.cs");
            bool visualSourceStable = preflight != null && preflight.visualGate != null && File.Exists(visualGatePath) &&
                                      string.Equals(Sha256(visualGatePath), preflight.visualGate.sourceSha256, StringComparison.OrdinalIgnoreCase);
            AddCheck(checks, "visual.wave3_threshold_source_unchanged", "baseline-preservation", visualSourceStable, "P1",
                "Wave3VisualGate.cs SHA-256 unchanged from preflight", visualSourceStable ? "unchanged" : "changed or preflight missing", "Assets/Editor/ParallelQA/Wave3VisualGate.cs");
            AuditCurrentVisualFacts(checks);

            string ledgerFullPath = ToFull(AssetLedgerPath);
            AssetLedger ledger = ReadJson<AssetLedger>(ledgerFullPath);
            AddCheck(checks, "ledger.parse", "forge-ledger", ledger != null && ledger.assets != null, "P0",
                "parseable .forge/assets.json with assets", ledger == null ? "parse failed" : "assets=" + (ledger.assets == null ? 0 : ledger.assets.Length), AssetLedgerPath);

            List<LedgerAsset> engineReady = ledger == null || ledger.assets == null
                ? new List<LedgerAsset>()
                : ledger.assets.Where(asset => asset != null && asset.status == "engine_ready").ToList();
            AddCheck(checks, "ledger.engine_ready_count", "forge-ledger", engineReady.Count > 0, "P0",
                "at least one engine_ready asset", "count=" + engineReady.Count, AssetLedgerPath);

            Dictionary<string, ForgeImportManifest> manifests = new Dictionary<string, ForgeImportManifest>();
            Dictionary<string, QualityReport> qualityReports = new Dictionary<string, QualityReport>();
            foreach (LedgerAsset asset in engineReady)
            {
                AuditEngineReadyAsset(asset, checks, files, manifests, qualityReports);
            }

            AuditBackgroundImporter(engineReady.FirstOrDefault(asset => asset.id == BackgroundAssetId), manifests, qualityReports, checks);
            AuditStructureImporter(engineReady.FirstOrDefault(asset => asset.id == StructureAssetId), manifests, qualityReports, checks);
            AuditBuildReachability(engineReady, manifests, checks);

            string[] joysticks = Input.GetJoystickNames() ?? Array.Empty<string>();
            string[] activeJoysticks = joysticks.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            AddRecord(checks, "input.physical_gamepad", "input-hardware", "UNVERIFIED", "P2",
                "human physical-gamepad actuation required for PASS",
                activeJoysticks.Length == 0 ? "no non-empty joystick name exposed to Unity batch mode" : "device name observed but no human actuation captured: " + string.Join(" | ", activeJoysticks), string.Empty);

            AssetContractReport report = new AssetContractReport
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                command = CommandLine(),
                passed = checks.Count(check => check.status == "PASS"),
                failed = checks.Count(check => check.status == "FAIL"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"),
                physicalGamepad = "UNVERIFIED",
                joystickNames = activeJoysticks,
                checks = checks.ToArray(),
                files = files.OrderBy(file => file.assetId).ThenBy(file => file.path).ToArray(),
                addressablesAtContract = addressAtContract
            };
            report.overall = report.failed == 0 ? "PASS" : "FAIL";
            WriteJson(Path.Combine(EvidenceFolder, "asset-contracts.json"), report);
            WriteContractText(report);
            WriteJson(Path.Combine(EvidenceFolder, "addressables-link-contract-snapshot.json"), addressAtContract);
            File.WriteAllLines(Path.Combine(EvidenceFolder, "asset-files.sha256"), report.files.Select(file => file.sha256 + "  " + file.path), new UTF8Encoding(false));

            if (report.failed > 0)
            {
                throw new InvalidOperationException("Wave 5 asset/release contracts failed. See " + Path.Combine(EvidenceFolder, "asset-contracts.json"));
            }
        }

        public static void BuildWindowsDevelopmentPlayer()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            if (Directory.Exists(BuildFolder))
            {
                Directory.Delete(BuildFolder, true);
            }
            Directory.CreateDirectory(BuildFolder);
            PreflightReport preflight = ReadJson<PreflightReport>(Path.Combine(EvidenceFolder, "wave5-preflight.json"));
            AddressSnapshot before = CaptureAddressSnapshot();
            string executable = Path.Combine(BuildFolder, "KimSurvivalIsland.exe");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AddressSnapshot after = CaptureAddressSnapshot();
            bool preflightStable = preflight != null && SameAddress(preflight.addressables, before);
            bool buildStable = SameAddress(before, after);
            string cleanupEvidencePath = Path.Combine(EvidenceFolder, "addressables-generated-link-cleanup.json");
            TemporaryCleanupEvidence cleanupEvidence = ReadJson<TemporaryCleanupEvidence>(cleanupEvidencePath);
            bool cleanupPassed = cleanupEvidence != null && cleanupEvidence.overall == "PASS";
            AddressBuildContract addressContract = new AddressBuildContract
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                overall = preflightStable && buildStable && cleanupPassed ? "PASS" : "FAIL",
                preflightToBeforeStable = preflightStable,
                beforeToAfterStable = buildStable,
                temporaryCopyCleanupPassed = cleanupPassed,
                temporaryCopyCleanupEvidence = cleanupEvidencePath,
                preflight = preflight == null ? null : preflight.addressables,
                beforeBuild = before,
                afterBuild = after
            };
            WriteJson(Path.Combine(EvidenceFolder, "addressables-link-build-contract.json"), addressContract);

            string[] warnings = report.steps
                .SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Warning)
                .Select(message => Normalize(message.content))
                .Distinct()
                .OrderBy(message => message)
                .ToArray();
            BuildEvidence evidence = new BuildEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                command = CommandLine(),
                target = "StandaloneWindows64",
                options = "Development",
                result = summary.result.ToString(),
                totalSizeBytes = summary.totalSize,
                durationSeconds = summary.totalTime.TotalSeconds,
                errors = (int)summary.totalErrors,
                warnings = (int)summary.totalWarnings,
                warningInventory = warnings,
                executable = executable,
                executableExists = File.Exists(executable),
                executableBytes = File.Exists(executable) ? new FileInfo(executable).Length : 0,
                executableSha256 = File.Exists(executable) ? Sha256(executable) : string.Empty,
                addressablesLinkContract = addressContract.overall
            };
            WriteJson(Path.Combine(EvidenceFolder, "windows-development-build.json"), evidence);
            WriteBuildText(evidence, addressContract);
            WriteSteamReadiness();

            if (summary.result != BuildResult.Succeeded || !File.Exists(executable) || addressContract.overall != "PASS")
            {
                throw new InvalidOperationException("Wave 5 Windows build or Addressables link ownership contract failed. See " + EvidenceFolder);
            }
        }

        private static void AuditEngineReadyAsset(
            LedgerAsset asset,
            List<CheckRecord> checks,
            List<FileRecord> files,
            Dictionary<string, ForgeImportManifest> manifests,
            Dictionary<string, QualityReport> qualityReports)
        {
            string prefix = "ledger." + asset.id;
            bool jobPresent = !string.IsNullOrWhiteSpace(asset.currentJobId);
            AddCheck(checks, prefix + ".current_job", "forge-ledger", jobPresent, "P0", "non-empty currentJobId", asset.currentJobId ?? "<null>", AssetLedgerPath);
            string packagePath = asset.engine == null ? string.Empty : NormalizeAssetPath(asset.engine.packagePath);
            bool packageMatches = jobPresent && !string.IsNullOrWhiteSpace(packagePath) &&
                                  string.Equals(Path.GetFileName(packagePath), asset.currentJobId, StringComparison.Ordinal);
            AddCheck(checks, prefix + ".package_job_match", "forge-ledger", packageMatches, "P0",
                "engine.packagePath directory name equals currentJobId", packagePath, AssetLedgerPath);
            bool packageExists = Directory.Exists(ToFull(packagePath));
            AddCheck(checks, prefix + ".package_exists", "forge-package", packageExists, "P0", "engine-ready package directory exists", packageExists ? "present" : "missing", packagePath);

            string manifestPath = asset.engine == null ? string.Empty : NormalizeAssetPath(asset.engine.manifest);
            bool manifestInsidePackage = !string.IsNullOrWhiteSpace(manifestPath) &&
                                         string.Equals(Path.GetDirectoryName(manifestPath)?.Replace('\\', '/'), packagePath, StringComparison.OrdinalIgnoreCase);
            AddCheck(checks, prefix + ".manifest_path", "forge-package", manifestInsidePackage, "P0", "engine manifest resides in current package", manifestPath, AssetLedgerPath);
            ForgeImportManifest manifest = ReadJson<ForgeImportManifest>(ToFull(manifestPath));
            bool manifestIdentity = manifest != null && manifest.assetId == asset.id && manifest.jobId == asset.currentJobId && manifest.assetType == asset.assetType;
            AddCheck(checks, prefix + ".manifest_identity", "forge-package", manifestIdentity, "P0",
                "manifest assetId/jobId/assetType match ledger", manifest == null ? "missing or invalid" : manifest.assetId + " | " + manifest.jobId + " | " + manifest.assetType, manifestPath);
            if (manifest != null)
            {
                manifests[asset.id] = manifest;
            }

            string qualityPath = packagePath + "/quality-report.json";
            QualityReport quality = ReadJson<QualityReport>(ToFull(qualityPath));
            bool qualityIdentity = quality != null && quality.jobId == asset.currentJobId && quality.assetType == asset.assetType;
            bool wholeJobQualityPass = qualityIdentity && string.Equals(quality.grade, "pass", StringComparison.OrdinalIgnoreCase) &&
                                       quality.summary != null && quality.summary.errors == 0;
            string[] selectedSources = manifest == null || manifest.sourceFiles == null ? Array.Empty<string>() : manifest.sourceFiles;
            HashSet<string> rejectedFiles = new HashSet<string>(
                manifest == null || manifest.rejectFiles == null ? Array.Empty<string>() : manifest.rejectFiles,
                StringComparer.OrdinalIgnoreCase);
            QualityFile[] qualityFiles = quality == null || quality.files == null ? Array.Empty<QualityFile>() : quality.files;
            QualityFile[] filesWithErrors = qualityFiles.Where(file => file != null && file.issues != null &&
                file.issues.Any(issue => issue != null && string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase))).ToArray();
            int enumeratedErrors = qualityFiles.Where(file => file != null && file.issues != null)
                .SelectMany(file => file.issues)
                .Count(issue => issue != null && string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase));
            bool selectedSourcesClean = selectedSources.Length > 0 && selectedSources.All(fileName =>
            {
                QualityFile file = qualityFiles.FirstOrDefault(candidate => candidate != null &&
                    string.Equals(candidate.fileName, fileName, StringComparison.OrdinalIgnoreCase));
                return file != null && (file.issues == null || file.issues.All(issue => issue == null ||
                    !string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase)));
            });
            bool rejectedReviewOnlyErrors = qualityIdentity && quality.summary != null && quality.summary.errors > 0 &&
                                            quality.summary.errors == enumeratedErrors && filesWithErrors.Length > 0 &&
                                            filesWithErrors.All(file => rejectedFiles.Contains(file.fileName));
            bool selectedPackageQualityPass = selectedSourcesClean && rejectedReviewOnlyErrors;
            bool qualityPass = wholeJobQualityPass || selectedPackageQualityPass;
            AddCheck(checks, prefix + ".quality_report", "forge-package", qualityPass, "P0",
                "quality identity matches and either the whole job passes or every selected package source is error-free while all errors are confined to manifest rejectFiles",
                quality == null ? "missing or invalid" : quality.grade + " | errors=" + (quality.summary == null ? -1 : quality.summary.errors) +
                " | selectedScope=" + (selectedPackageQualityPass ? "PASS" : "N/A"), qualityPath);
            if (quality != null)
            {
                qualityReports[asset.id] = quality;
            }

            string[] sourceFiles = manifest == null || manifest.sourceFiles == null ? Array.Empty<string>() : manifest.sourceFiles;
            string[] pngSourceFiles = sourceFiles.Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToArray();
            AddCheck(checks, prefix + ".source_png_count", "forge-package", pngSourceFiles.Length > 0, "P0",
                "manifest declares one or more required PNG sourceFiles; editable/non-raster sources may coexist",
                "png=" + pngSourceFiles.Length + "/all=" + sourceFiles.Length + " · " + string.Join(" | ", sourceFiles), manifestPath);
            foreach (string fileName in sourceFiles)
            {
                string assetPath = packagePath + "/" + fileName;
                bool exists = File.Exists(ToFull(assetPath));
                AddCheck(checks, prefix + ".source." + Sanitize(fileName), "forge-package", exists, "P0", "declared source exists", exists ? "present" : "missing", assetPath);
                if (exists)
                {
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    files.Add(new FileRecord
                    {
                        assetId = asset.id,
                        jobId = asset.currentJobId,
                        path = assetPath,
                        bytes = new FileInfo(ToFull(assetPath)).Length,
                        sha256 = Sha256(ToFull(assetPath)),
                        guid = guid
                    });
                    AddCheck(checks, prefix + ".source_guid." + Sanitize(fileName), "unity-import", !string.IsNullOrWhiteSpace(guid), "P0", "declared source has a Unity GUID", string.IsNullOrWhiteSpace(guid) ? "missing" : guid, assetPath + ".meta");
                }
            }

            string[] required = asset.id == BackgroundAssetId
                ? new[] { "background_opaque.png", "gameplay_ground_alpha.png", "foreground_alpha.png" }
                : asset.id == StructureAssetId
                    ? new[] { "camp_structures_atlas_alpha.png", "campfire.png", "workbench.png", "rain_collector.png", "rescue_signal.png" }
                    : Array.Empty<string>();
            foreach (string requiredFile in required)
            {
                AddCheck(checks, prefix + ".required." + Sanitize(requiredFile), "forge-package", sourceFiles.Contains(requiredFile), "P0",
                    "required adopted PNG is declared by manifest", sourceFiles.Contains(requiredFile) ? "declared" : "missing", manifestPath);
            }

            if (asset.artifacts != null)
            {
                foreach (LedgerArtifact artifact in asset.artifacts.Where(item => item != null && item.kind == "image"))
                {
                    string artifactPath = packagePath + "/" + artifact.fileName;
                    long actualBytes = File.Exists(ToFull(artifactPath)) ? new FileInfo(ToFull(artifactPath)).Length : -1;
                    AddCheck(checks, prefix + ".ledger_image_bytes." + Sanitize(artifact.fileName), "forge-ledger", actualBytes == artifact.bytes, "P1",
                        "binary image bytes match .forge/assets.json", "ledger=" + artifact.bytes + " actual=" + actualBytes, artifactPath);
                }
            }
        }

        private static void AuditBackgroundImporter(
            LedgerAsset asset,
            Dictionary<string, ForgeImportManifest> manifests,
            Dictionary<string, QualityReport> qualityReports,
            List<CheckRecord> checks)
        {
            if (asset == null || !manifests.TryGetValue(asset.id, out ForgeImportManifest manifest))
            {
                AddCheck(checks, "import.background.target", "unity-import", false, "P0", "engine-ready background manifest available", "missing", AssetLedgerPath);
                return;
            }

            string package = NormalizeAssetPath(asset.engine.packagePath);
            string layerManifestPath = package + "/island-camp-layer-manifest.json";
            LayerManifest layers = ReadJson<LayerManifest>(ToFull(layerManifestPath));
            bool layerIdentity = layers != null && layers.assetId == asset.id && layers.jobId == asset.currentJobId && layers.canvas != null && layers.canvas.width > 0 && layers.canvas.height > 0;
            AddCheck(checks, "import.background.layer_manifest", "background-layers", layerIdentity, "P0",
                "layer manifest identity and canvas are valid", layers == null ? "missing" : layers.assetId + " | " + layers.jobId + " | " + layers.canvas.width + "x" + layers.canvas.height, layerManifestPath);

            QualityReport quality = qualityReports.TryGetValue(asset.id, out QualityReport value) ? value : null;
            Vector2? firstPivot = null;
            foreach (string sourceFile in manifest.sourceFiles ?? Array.Empty<string>())
            {
                string assetPath = package + "/" + sourceFile;
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                AuditImporterCommon(asset.id, sourceFile, assetPath, importer, manifest.import, checks);
                if (importer != null)
                {
                    bool centerPivot = Approximately(importer.spritePivot, new Vector2(0.5f, 0.5f));
                    AddCheck(checks, "import.background.pivot." + Sanitize(sourceFile), "background-layers", centerPivot, "P1", "center pivot (0.5,0.5)", Format(importer.spritePivot), assetPath);
                    if (!firstPivot.HasValue) firstPivot = importer.spritePivot;
                    AddCheck(checks, "import.background.pivot_consistency." + Sanitize(sourceFile), "background-layers", !firstPivot.HasValue || Approximately(importer.spritePivot, firstPivot.Value), "P1",
                        "all background layers share one pivot", Format(importer.spritePivot), assetPath);
                }

                QualityFile qualityFile = quality == null || quality.files == null ? null : quality.files.FirstOrDefault(file => file.fileName == sourceFile);
                bool dimensionsMatch = qualityFile != null && layers != null && layers.canvas != null && qualityFile.width == layers.canvas.width && qualityFile.height == layers.canvas.height;
                AddCheck(checks, "import.background.canvas." + Sanitize(sourceFile), "background-layers", dimensionsMatch, "P0",
                    "quality report PNG dimensions match layer canvas", qualityFile == null ? "quality entry missing" : qualityFile.width + "x" + qualityFile.height, assetPath);
            }

            bool declaredLayers = layers != null && layers.layers != null && manifest.sourceFiles != null &&
                                  layers.layers.Select(layer => layer.file).OrderBy(name => name).SequenceEqual(manifest.sourceFiles.OrderBy(name => name));
            AddCheck(checks, "import.background.layer_sources", "background-layers", declaredLayers, "P0",
                "layer manifest and forge-import sourceFiles match", declaredLayers ? "matched" : "mismatch", layerManifestPath);
        }

        private static void AuditStructureImporter(
            LedgerAsset asset,
            Dictionary<string, ForgeImportManifest> manifests,
            Dictionary<string, QualityReport> qualityReports,
            List<CheckRecord> checks)
        {
            if (asset == null || !manifests.TryGetValue(asset.id, out ForgeImportManifest manifest))
            {
                AddCheck(checks, "import.structures.target", "unity-import", false, "P0", "engine-ready structures manifest available", "missing", AssetLedgerPath);
                return;
            }

            string package = NormalizeAssetPath(asset.engine.packagePath);
            string metadataPath = package + "/camp-structures-metadata.json";
            StructureMetadata metadata = ReadJson<StructureMetadata>(ToFull(metadataPath));
            bool metadataIdentity = metadata != null && metadata.assetId == asset.id && metadata.jobId == asset.currentJobId && metadata.parts != null && metadata.parts.Length >= 4;
            AddCheck(checks, "import.structures.metadata", "structure-sprites", metadataIdentity, "P0",
                "structure metadata identity with at least four parts", metadata == null ? "missing" : metadata.assetId + " | " + metadata.jobId + " | parts=" + (metadata.parts == null ? 0 : metadata.parts.Length), metadataPath);
            QualityReport quality = qualityReports.TryGetValue(asset.id, out QualityReport value) ? value : null;

            foreach (string sourceFile in manifest.sourceFiles ?? Array.Empty<string>())
            {
                string assetPath = package + "/" + sourceFile;
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                AuditImporterCommon(asset.id, sourceFile, assetPath, importer, manifest.import, checks);
                QualityFile qualityFile = quality == null || quality.files == null ? null : quality.files.FirstOrDefault(file => file.fileName == sourceFile);
                bool transparentSource = qualityFile != null && qualityFile.hasAlpha && importer != null && importer.DoesSourceTextureHaveAlpha() && importer.alphaIsTransparency;
                AddCheck(checks, "import.structures.alpha." + Sanitize(sourceFile), "structure-sprites", transparentSource, "P0",
                    "quality report and Unity source confirm alpha; alphaIsTransparency=true",
                    qualityFile == null || importer == null ? "missing quality/importer" : "qualityAlpha=" + qualityFile.hasAlpha + " sourceAlpha=" + importer.DoesSourceTextureHaveAlpha() + " alphaIsTransparency=" + importer.alphaIsTransparency, assetPath);

                StructurePart part = metadata == null || metadata.parts == null ? null : metadata.parts.FirstOrDefault(item => item.file == sourceFile);
                Vector2 expectedPivot = part != null && part.pivotNormalized != null && part.pivotNormalized.Length >= 2
                    ? new Vector2(part.pivotNormalized[0], part.pivotNormalized[1])
                    : new Vector2(0.5f, 0.5f);
                bool pivotPass = importer != null && Approximately(importer.spritePivot, expectedPivot);
                AddCheck(checks, "import.structures.pivot." + Sanitize(sourceFile), "structure-sprites", pivotPass, "P1",
                    "TextureImporter pivot matches camp-structures-metadata bottom-center policy", importer == null ? "importer missing" : "expected=" + Format(expectedPivot) + " actual=" + Format(importer.spritePivot), assetPath);
            }
        }

        private static void AuditImporterCommon(
            string assetId,
            string sourceFile,
            string assetPath,
            TextureImporter importer,
            ImportPolicy policy,
            List<CheckRecord> checks)
        {
            string prefix = "import." + assetId + "." + Sanitize(sourceFile);
            AddCheck(checks, prefix + ".exists", "unity-import", importer != null, "P0", "TextureImporter exists", importer == null ? "missing" : "present", assetPath);
            if (importer == null || policy == null)
            {
                return;
            }

            TextureImporterCompression expectedCompression = policy.compression == "CompressedHQ"
                ? TextureImporterCompression.CompressedHQ
                : policy.compression == "Uncompressed" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            SpriteImportMode expectedMode = policy.spriteMode == "Multiple" ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            FilterMode expectedFilter = policy.filterMode == "Point" ? FilterMode.Point : policy.filterMode == "Trilinear" ? FilterMode.Trilinear : FilterMode.Bilinear;
            AddCheck(checks, prefix + ".sprite_type", "unity-import", importer.textureType == TextureImporterType.Sprite, "P0", "TextureImporterType.Sprite", importer.textureType.ToString(), assetPath);
            AddCheck(checks, prefix + ".sprite_mode", "unity-import", importer.spriteImportMode == expectedMode, "P0", expectedMode.ToString(), importer.spriteImportMode.ToString(), assetPath);
            AddCheck(checks, prefix + ".ppu", "unity-import", Mathf.Approximately(importer.spritePixelsPerUnit, policy.pixelsPerUnit), "P1", policy.pixelsPerUnit.ToString("0.###"), importer.spritePixelsPerUnit.ToString("0.###"), assetPath);
            AddCheck(checks, prefix + ".filter", "unity-import", importer.filterMode == expectedFilter, "P1", expectedFilter.ToString(), importer.filterMode.ToString(), assetPath);
            AddCheck(checks, prefix + ".mipmaps", "unity-import", importer.mipmapEnabled == policy.mipmaps, "P1", policy.mipmaps.ToString(), importer.mipmapEnabled.ToString(), assetPath);
            AddCheck(checks, prefix + ".alpha_policy", "unity-import", importer.alphaIsTransparency == policy.alphaIsTransparency, "P1", policy.alphaIsTransparency.ToString(), importer.alphaIsTransparency.ToString(), assetPath);
            AddCheck(checks, prefix + ".compression", "unity-import", importer.textureCompression == expectedCompression, "P1", expectedCompression.ToString(), importer.textureCompression.ToString(), assetPath);
            AddCheck(checks, prefix + ".max_size", "unity-import", importer.maxTextureSize == policy.maxSize, "P1", policy.maxSize.ToString(), importer.maxTextureSize.ToString(), assetPath);
        }

        private static void AuditBuildReachability(
            List<LedgerAsset> engineReady,
            Dictionary<string, ForgeImportManifest> manifests,
            List<CheckRecord> checks)
        {
            HashSet<string> dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            {
                foreach (string dependency in AssetDatabase.GetDependencies(scene.path, true))
                {
                    dependencies.Add(NormalizeAssetPath(dependency));
                }
            }

            string addressablesText = string.Join("\n", Directory.GetFiles(ToFull("Assets/AddressableAssetsData"), "*.asset", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
            foreach (string assetId in new[] { BackgroundAssetId, StructureAssetId })
            {
                LedgerAsset asset = engineReady.FirstOrDefault(item => item.id == assetId);
                ForgeImportManifest manifest = manifests.TryGetValue(assetId, out ForgeImportManifest value) ? value : null;
                string package = asset == null || asset.engine == null ? string.Empty : NormalizeAssetPath(asset.engine.packagePath);
                string[] sources = manifest == null || manifest.sourceFiles == null
                    ? Array.Empty<string>()
                    : manifest.sourceFiles.Select(file => package + "/" + file).ToArray();
                List<string> reachable = new List<string>();
                foreach (string source in sources)
                {
                    string guid = AssetDatabase.AssetPathToGUID(source);
                    if (dependencies.Contains(source) || (!string.IsNullOrWhiteSpace(guid) && addressablesText.IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        reachable.Add(source);
                    }
                }
                AddCheck(checks, "build_reachability." + assetId, "windows-build-input", reachable.Count > 0, "P1",
                    "at least one currentJobId source is referenced by an enabled build scene or Addressables",
                    reachable.Count == 0 ? "none of " + sources.Length + " current sources reachable" : string.Join(" | ", reachable), package);
            }
        }

        private static void AuditCurrentVisualFacts(List<CheckRecord> checks)
        {
            string reportPath = Path.Combine(EvidenceFolder, "wave3-visual-gate.txt");
            string[] lines = File.Exists(reportPath) ? File.ReadAllLines(reportPath) : Array.Empty<string>();
            VisualFact placement = ParseVisualFact(lines, "PLACEMENT_GATE");
            VisualFact exploration = ParseVisualFact(lines, "EXPLORATION_SWIMMING_GATE");
            VisualFact searchTray = ParseVisualFact(lines, "SEARCH_TRAY_GATE");
            VisualFact qpsLong = ParseVisualFact(lines, "PSEUDO_LONG_GATE");
            bool baselineIdentity = lines.Any(line => string.Equals(line.Trim(), "Baseline commit: " + BaselineCommit, StringComparison.Ordinal));
            bool placementPass = placement.status == "PASS" && placement.targets == 4 && placement.failures == 0;
            bool explorationPass = exploration.status == "PASS" && exploration.targets == 4 && exploration.failures == 0;
            bool searchTrayPass = searchTray.status == "PASS" && searchTray.targets == 16 && searchTray.failures == 0;
            bool qpsPass = qpsLong.status == "PASS" && qpsLong.targets == 37 && qpsLong.failures == 0;

            AddCheck(checks, "visual.current_baseline_identity", "current-integrated-visual", File.Exists(reportPath) && baselineIdentity, "P1",
                "fresh visual gate evidence identifies the current 671c4e9 baseline", File.Exists(reportPath) ? lines.FirstOrDefault(line => line.StartsWith("Baseline commit:", StringComparison.Ordinal)) ?? "baseline line missing" : "report missing", reportPath);
            AddCheck(checks, "visual.current_normal_ko_en_placement", "current-integrated-visual", placementPass, "P1",
                "normal ko/en placement 4/4 PASS", placement.evidenceLine, reportPath);
            AddCheck(checks, "visual.current_normal_ko_en_exploration_swimming", "current-integrated-visual", explorationPass, "P1",
                "normal ko/en nearest-node exploration/swimming 4/4 PASS", exploration.evidenceLine, reportPath);
            AddCheck(checks, "visual.current_normal_ko_en_search_tray", "current-integrated-visual", searchTrayPass, "P1",
                "normal ko/en compact search tray 16/16 PASS", searchTray.evidenceLine, reportPath);
            AddRecord(checks, "visual.current_qps_long", "current-integrated-visual", qpsPass ? "PASS" : "FAIL", "P1",
                "qps-long fresh-pity production scenes 37/37 PASS are required for future-locale release readiness; protected-part trays remain a separate Wave B contract",
                qpsLong.evidenceLine, reportPath);

            CurrentVisualFacts facts = new CurrentVisualFacts
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                observedUtc = DateTime.UtcNow.ToString("O"),
                source = reportPath,
                standardKoEnOverall = placementPass && explorationPass && searchTrayPass ? "PASS" : "FAIL",
                qpsLongOverall = qpsPass ? "PASS" : "FAIL",
                overall = placementPass && explorationPass && searchTrayPass && qpsPass ? "PASS" : "FAIL",
                placement = placement,
                explorationSwimming = exploration,
                searchTray = searchTray,
                qpsLong = qpsLong
            };
            WriteJson(Path.Combine(EvidenceFolder, "wave5-current-visual-facts.json"), facts);
            StringBuilder text = new StringBuilder();
            text.AppendLine("Wave 5 current integrated 1280x800 visual facts");
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Normal ko/en: " + facts.standardKoEnOverall);
            text.AppendLine("Placement: " + placement.evidenceLine);
            text.AppendLine("Exploration/swimming: " + exploration.evidenceLine);
            text.AppendLine("Search tray: " + searchTray.evidenceLine);
            text.AppendLine("qps-long: " + qpsLong.evidenceLine);
            text.AppendLine("qps-long classification: " + (qpsPass ? "PASS" : "FAIL · retained as Unity system-work input"));
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave5-current-visual-facts.txt"), text.ToString(), new UTF8Encoding(false));
        }

        private static VisualFact ParseVisualFact(string[] lines, string gateName)
        {
            string line = lines.FirstOrDefault(value => value.StartsWith(gateName + ":", StringComparison.Ordinal)) ?? "MISSING";
            Match match = Regex.Match(line, @"^[^:]+:\s+(PASS|FAIL)\s+·\s+targets=(\d+)\s+·\s+failures=(\d+)");
            return new VisualFact
            {
                status = match.Success ? match.Groups[1].Value : "MISSING",
                targets = match.Success ? int.Parse(match.Groups[2].Value) : -1,
                failures = match.Success ? int.Parse(match.Groups[3].Value) : -1,
                evidenceLine = line
            };
        }

        private static void WriteSteamReadiness()
        {
            string[] roots = { "Assets", "Packages", "ProjectSettings" };
            string[] extensions = { ".cs", ".json", ".asset", ".txt", ".vdf", ".xml", ".asmdef", ".asmref" };
            List<string> candidateFiles = roots.SelectMany(root => Directory.GetFiles(ToFull(root), "*", SearchOption.AllDirectories))
                .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .Where(path => path.IndexOf(Path.Combine("Assets", "Editor", "ParallelQA"), StringComparison.OrdinalIgnoreCase) < 0)
                .Where(path => path.IndexOf(Path.Combine("Assets", "_Project", "Art", "Generated"), StringComparison.OrdinalIgnoreCase) < 0)
                .ToList();
            Dictionary<string, string[]> tokens = new Dictionary<string, string[]>
            {
                { "Steamworks SDK/API", new[] { "steam_api", "Steamworks.NET", "Steamworks.SteamClient", "ISteamClient" } },
                { "Steam App ID", new[] { "steam_appid", "SteamAppId", "SteamAppID" } },
                { "Depot/upload", new[] { "DepotID", "build_app", "build_depot", "ContentRoot" } },
                { "Steam Input", new[] { "SteamInput", "input_action_manifest" } },
                { "Steam Cloud", new[] { "ISteamRemoteStorage", "SteamRemoteStorage" } },
                { "Steam Achievements", new[] { "SetAchievement", "ISteamUserStats", "SteamAchievement" } }
            };
            List<string> matched = new List<string>();
            List<SteamArea> areas = new List<SteamArea>();
            foreach (KeyValuePair<string, string[]> area in tokens)
            {
                List<string> areaMatches = new List<string>();
                foreach (string path in candidateFiles)
                {
                    string fileName = Path.GetFileName(path);
                    string content = File.ReadAllText(path);
                    if (area.Value.Any(token => fileName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 || content.IndexOf(token, StringComparison.Ordinal) >= 0))
                    {
                        areaMatches.Add(Path.GetRelativePath(ProjectRoot, path));
                    }
                }
                matched.AddRange(areaMatches);
                areas.Add(new SteamArea
                {
                    area = area.Key,
                    status = areaMatches.Count == 0 ? "NOT_READY" : "REQUIRES_MANUAL_VALIDATION",
                    evidence = areaMatches.Count == 0 ? "no targeted repository match" : string.Join(" | ", areaMatches.Distinct())
                });
            }

            SteamReadiness report = new SteamReadiness
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                observedUtc = DateTime.UtcNow.ToString("O"),
                overall = areas.All(area => area.status == "NOT_READY") ? "NOT_READY" : "REQUIRES_MANUAL_VALIDATION",
                scanScope = "Assets, Packages, ProjectSettings excluding QA harness and generated Forge art text",
                matchedFiles = matched.Distinct().OrderBy(path => path).ToArray(),
                areas = areas.ToArray()
            };
            WriteJson(Path.Combine(EvidenceFolder, "steam-readiness.json"), report);
            StringBuilder text = new StringBuilder();
            text.AppendLine("Wave 4 Steam readiness");
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Overall: " + report.overall);
            foreach (SteamArea area in report.areas)
            {
                text.AppendLine(area.status + " · " + area.area + " · " + area.evidence);
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, "steam-readiness.txt"), text.ToString(), new UTF8Encoding(false));
        }

        private static AddressSnapshot CaptureAddressSnapshot()
        {
            string link = ToFull(LinkPath);
            string meta = ToFull(LinkMetaPath);
            return new AddressSnapshot
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

        private static bool SameAddress(AddressSnapshot left, AddressSnapshot right)
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

        private static string AddressComparison(AddressSnapshot expected, AddressSnapshot actual)
        {
            if (expected == null) return "preflight missing";
            if (actual == null) return "current snapshot missing";
            return "expected exists=" + expected.linkExists + "/" + expected.metaExists + " link=" + expected.linkSha256 + " meta=" + expected.metaSha256 + " guid=" + expected.metaGuid +
                   " | actual exists=" + actual.linkExists + "/" + actual.metaExists + " link=" + actual.linkSha256 + " meta=" + actual.metaSha256 + " guid=" + actual.metaGuid + " assetDatabaseGuid=" + actual.assetDatabaseGuid;
        }

        private static void WriteContractText(AssetContractReport report)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Wave 5 integrated asset and release contracts");
            text.AppendLine("Run ID: " + report.runId);
            text.AppendLine("Baseline: " + report.baselineCommit);
            text.AppendLine("Unity: " + report.unityVersion);
            text.AppendLine("Command: " + report.command);
            text.AppendLine("Overall: " + report.overall);
            text.AppendLine("Passed: " + report.passed + " · Failed: " + report.failed + " · Unverified: " + report.unverified);
            text.AppendLine("Physical gamepad: " + report.physicalGamepad + " · names=" + (report.joystickNames.Length == 0 ? "<none>" : string.Join(" | ", report.joystickNames)));
            foreach (CheckRecord check in report.checks)
            {
                text.AppendLine(check.status + " · " + check.severity + " · " + check.id + " · expected=" + check.expected + " · actual=" + check.actual + " · " + check.path);
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, "asset-contracts.txt"), text.ToString(), new UTF8Encoding(false));
        }

        private static void WriteBuildText(BuildEvidence evidence, AddressBuildContract addressContract)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Wave 5 Windows x64 Development Build");
            text.AppendLine("Run ID: " + evidence.runId);
            text.AppendLine("Baseline: " + evidence.baselineCommit);
            text.AppendLine("Unity: " + evidence.unityVersion);
            text.AppendLine("Command: " + evidence.command);
            text.AppendLine("Build options: " + evidence.options);
            text.AppendLine("Result: " + evidence.result);
            text.AppendLine("Errors: " + evidence.errors);
            text.AppendLine("Warnings: " + evidence.warnings);
            text.AppendLine("Addressables link contract: " + addressContract.overall);
            text.AppendLine("Preflight -> before build stable: " + addressContract.preflightToBeforeStable);
            text.AppendLine("Before -> after build stable: " + addressContract.beforeToAfterStable);
            text.AppendLine("Generated temporary copy cleanup: " + addressContract.temporaryCopyCleanupPassed);
            text.AppendLine("Generated temporary copy evidence: " + addressContract.temporaryCopyCleanupEvidence);
            text.AppendLine("Executable: " + evidence.executable);
            text.AppendLine("Executable exists: " + evidence.executableExists);
            text.AppendLine("Executable bytes: " + evidence.executableBytes);
            text.AppendLine("Executable SHA-256: " + evidence.executableSha256);
            foreach (string warning in evidence.warningInventory)
            {
                text.AppendLine("WARNING · " + warning);
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, "windows-development-build.txt"), text.ToString(), new UTF8Encoding(false));
        }

        private static void AddCheck(List<CheckRecord> checks, string id, string category, bool passed, string severity, string expected, string actual, string path)
        {
            AddRecord(checks, id, category, passed ? "PASS" : "FAIL", severity, expected, actual, path);
        }

        private static void AddRecord(List<CheckRecord> checks, string id, string category, string status, string severity, string expected, string actual, string path)
        {
            checks.Add(new CheckRecord
            {
                id = id,
                category = category,
                status = status,
                severity = severity,
                expected = expected,
                actual = actual,
                path = path
            });
        }

        private static T ReadJson<T>(string path) where T : class
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }
            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        private static void WriteJson(string path, object value)
        {
            File.WriteAllText(path, JsonUtility.ToJson(value, true) + Environment.NewLine, new UTF8Encoding(false));
        }

        private static string ToFull(string projectRelative)
        {
            if (string.IsNullOrWhiteSpace(projectRelative)) return string.Empty;
            return Path.GetFullPath(Path.Combine(ProjectRoot, projectRelative.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        }

        private static string ParseGuid(string meta)
        {
            foreach (string line in (meta ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
            {
                if (line.StartsWith("guid:", StringComparison.Ordinal))
                {
                    return line.Substring("guid:".Length).Trim();
                }
            }
            return string.Empty;
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Abs(left.x - right.x) <= 0.0002f && Mathf.Abs(left.y - right.y) <= 0.0002f;
        }

        private static string Format(Vector2 value)
        {
            return "(" + value.x.ToString("0.#####") + "," + value.y.ToString("0.#####") + ")";
        }

        private static string CommandLine()
        {
            return string.Join(" ", Environment.GetCommandLineArgs().Select(argument => argument.IndexOf(' ') >= 0 ? "\"" + argument + "\"" : argument));
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        private static string Sanitize(string value)
        {
            string sanitized = value ?? string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars()) sanitized = sanitized.Replace(invalid, '_');
            return sanitized.Replace('.', '_').Replace(' ', '_');
        }
    }
}
