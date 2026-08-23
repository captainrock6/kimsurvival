using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ParallelQA
{
    /// <summary>
    /// Evidence-only Wave 14 gate. A fresh Wave 12 Play run owns scene setup and
    /// captures; this runner independently evaluates its projected 1280x800
    /// metrics without changing runtime layout or the legacy Wave 3 thresholds.
    /// </summary>
    public static class Wave14QpsGlobalLayoutGateRunner
    {
        private const string Scenario = "qps-long placement valid";
        private const float ScreenMargin = 4f;
        private const float MinimumGlyphPixels = 16f;
        private const float NormalContrast = 4.5f;
        private const float LargeContrast = 3f;
        private const float MaximumPlayerOcclusionRatio = 0.05f;
        private const float MaximumWalkingPathOcclusionRatio = 0.20f;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private sealed class Contract
        {
            public string Id;
            public string Matrix;
            public string HierarchyNeedle;
            public int MaximumLines;
            public float MaximumBlockHeight;
            public string RecommendedFiles;
        }

        private sealed class MetricRow
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public string Get(string key) { return Values.TryGetValue(key, out string value) ? value : string.Empty; }
        }

        [Serializable]
        private sealed class TargetResult
        {
            public string id;
            public string matrix;
            public string status;
            public string classification;
            public string severity;
            public string screenshot;
            public string hierarchy;
            public string text;
            public float glyphMedianPx;
            public float blockHeightPx;
            public int lineCount;
            public int maximumLines;
            public float maximumBlockHeightPx;
            public float leftPx;
            public float bottomPx;
            public float rightPx;
            public float topPx;
            public float safeMarginPx;
            public float contrastRatio;
            public bool overflow;
            public string textOverlaps;
            public string uiOcclusions;
            public string playerScreenRect;
            public string walkingPathScreenRect;
            public float playerOcclusionRatio;
            public float maximumPlayerOcclusionRatio;
            public float walkingPathOcclusionRatio;
            public float maximumWalkingPathOcclusionRatio;
            public string[] failures;
            public string reproduction;
            public string recommendedFiles;
        }

        [Serializable]
        private sealed class Check
        {
            public string id;
            public string status;
            public string classification;
            public string severity;
            public string expected;
            public string actual;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion = 1;
            public string title = "Wave 14 qps-long global 1280x800 layout RED-first gate";
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string overall;
            public string productOverall;
            public string infrastructureOverall;
            public int targetCount;
            public int passedTargets;
            public int expectedGapTargets;
            public int unexpectedFailedTargets;
            public string[] expectedBaselineFailureIds;
            public string[] reproducedBaselineFailureIds;
            public bool exactSixOfTenBaselineReproduced;
            public string greenCompletionCondition;
            public string physicalGamepad;
            public string steamReadiness;
            public string sourceMetrics;
            public string sourceScreenshot;
            public string projectionCorrection;
            public TargetResult[] targets;
            public Check[] checks;
        }

        private static readonly Contract[] Contracts =
        {
            NewContract("W14-QPS-01.hud_day_status", "minimal HUD / day and status", "/상태 HUD/날짜·상태", 2, 64f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs"),
            NewContract("W14-QPS-02.hud_resources", "minimal HUD / resources", "/상태 HUD/보유 자원", 2, 64f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs"),
            NewContract("W14-QPS-03.language_button", "language button", "/언어 설정/라벨", 1, 32f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs"),
            NewContract("W14-QPS-04.bottom_help", "minimal HUD / bottom help", "/조작 안내/조작", 3, 96f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs"),
            NewContract("W14-QPS-05.world_entrance", "world badge / entrance", "/world.entrance 안내/", 3, 120f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs"),
            NewContract("W14-QPS-06.world_required_path", "world badge / required path", "/world.required_path 안내/", 2, 80f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs"),
            NewContract("W14-QPS-07.world_signal_anchor", "world badge / signal anchor", "/구조 신호대 전용 앵커 안내/", 3, 108f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs"),
            NewContract("W14-QPS-08.placement_status", "placement state", "/배치 판정/", 3, 108f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs; Assets/_Project/Scripts/Runtime/PrototypeCampPlacement.cs"),
            NewContract("W14-QPS-09.world_expansion_planning", "world badge / expansion planning", "/증축 계획 지점 안내/", 2, 64f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs"),
            NewContract("W14-QPS-10.world_general_floor", "world badge / general floor", "/호환 건설 구역 안내/", 4, 144f,
                "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs")
        };

        private static readonly HashSet<string> ExpectedBaselineFailures = new HashSet<string>(StringComparer.Ordinal)
        {
            "W14-QPS-01.hud_day_status",
            "W14-QPS-02.hud_resources",
            "W14-QPS-07.world_signal_anchor",
            "W14-QPS-08.placement_status",
            "W14-QPS-09.world_expansion_planning",
            "W14-QPS-10.world_general_floor"
        };

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave14" : Sanitize(value);
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
        private static string EvidenceFolder { get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); } }

        public static void RunEvidenceContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            string reportPath = Path.Combine(EvidenceFolder, "wave3-visual-gate.txt");
            string metricsPath = Path.Combine(EvidenceFolder, "wave3-visual-metrics.tsv");
            string screenshotName = "playmode-qps-long-placement-valid-1280x800.png";
            string screenshotPath = Path.Combine(EvidenceFolder, screenshotName);
            List<Check> checks = new List<Check>();
            List<TargetResult> targets = new List<TargetResult>();

            try
            {
                string wave3Report = File.Exists(reportPath) ? File.ReadAllText(reportPath, Encoding.UTF8) : string.Empty;
                bool identityPass = wave3Report.Contains("Run ID: " + RunId) &&
                                    wave3Report.Contains("Baseline commit: " + BaselineCommit);
                checks.Add(NewCheck("W14-I01.fresh_identity", identityPass ? "PASS" : "INFRA_FAIL",
                    identityPass ? "NONE" : "INFRASTRUCTURE", "P0",
                    "Fresh Wave 3 report belongs to this RunId and exact baseline",
                    "report=" + Path.GetFileName(reportPath) + "; runId/baselineMatch=" + identityPass));

                MetricRow[] rows = ReadMetrics(metricsPath, out string[] headers);
                string[] requiredHeaders =
                {
                    "scenario", "screenshot", "category", "glyph_median_px", "block_height_px", "left_px", "bottom_px",
                    "right_px", "top_px", "contrast_ratio", "overflow", "text_overlaps", "ui_occlusions", "line_count",
                    "player_screen_rect", "walking_path_screen_rect", "player_occlusion_ratio", "walking_path_occlusion_ratio",
                    "hierarchy", "text"
                };
                string[] missingHeaders = requiredHeaders.Where(required => !headers.Contains(required, StringComparer.OrdinalIgnoreCase)).ToArray();
                bool schemaPass = rows.Length > 0 && missingHeaders.Length == 0;
                checks.Add(NewCheck("W14-I02.metric_schema", schemaPass ? "PASS" : "INFRA_FAIL",
                    schemaPass ? "NONE" : "INFRASTRUCTURE", "P0",
                    "Projected metrics expose lines, screen Rects, and protected-region overlap ratios",
                    "rows=" + rows.Length + "; missingHeaders=" + (missingHeaders.Length == 0 ? "none" : string.Join(",", missingHeaders))));

                bool pngPass = VerifyPng(screenshotPath, 1280, 800);
                checks.Add(NewCheck("W14-I03.capture_1280x800", pngPass ? "PASS" : "INFRA_FAIL",
                    pngPass ? "NONE" : "INFRASTRUCTURE", "P0",
                    "The qps-long placement source capture is a non-empty 1280x800 PNG",
                    screenshotName + " exact1280x800=" + pngPass));

                Match placement = Regex.Match(wave3Report, @"PLACEMENT_GATE:\s+(PASS|FAIL)\s+·\s+targets=(\d+)\s+·\s+failures=(\d+)");
                Match exploration = Regex.Match(wave3Report, @"EXPLORATION_SWIMMING_GATE:\s+(PASS|FAIL)\s+·\s+targets=(\d+)\s+·\s+failures=(\d+)");
                bool normalLocalesPass = placement.Success && exploration.Success &&
                                         placement.Groups[1].Value == "PASS" && placement.Groups[2].Value == "24" && placement.Groups[3].Value == "0" &&
                                         exploration.Groups[1].Value == "PASS" && exploration.Groups[2].Value == "10" && exploration.Groups[3].Value == "0";
                checks.Add(NewCheck("W14-N01.ko_en_visual_lock", normalLocalesPass ? "PASS" : "FAIL",
                    normalLocalesPass ? "NONE" : "PRODUCT_REGRESSION", "P1",
                    "Normal ko/en placement remains 24/24 PASS and exploration/swimming remains 10/10 PASS",
                    "placement=" + (placement.Success ? placement.Value : "MISSING") + "; exploration=" +
                    (exploration.Success ? exploration.Value : "MISSING")));

                MetricRow[] qpsRows = rows.Where(row => string.Equals(row.Get("scenario"), Scenario, StringComparison.Ordinal) &&
                                                        string.Equals(row.Get("category"), "pseudo-long", StringComparison.Ordinal)).ToArray();
                bool projectionPass = qpsRows.Length == 10 && qpsRows.All(row =>
                    Regex.IsMatch(row.Get("walking_path_screen_rect"), @"^x=0\.0,y=[-0-9.]+,w=1280\.0,h=[0-9.]+$") &&
                    !string.Equals(row.Get("player_screen_rect"), "UNAVAILABLE", StringComparison.OrdinalIgnoreCase));
                checks.Add(NewCheck("W14-I04.exact_capture_projection", projectionPass ? "PASS" : "INFRA_FAIL",
                    projectionPass ? "NONE" : "INFRASTRUCTURE", "P0",
                    "All target Rects are projected while the camera is fixed to the exact 1280x800 capture target; walking band is x=0..1280 and current player Rect is available",
                    "qpsRows=" + qpsRows.Length + "; exactWalkingBandAndPlayer=" + projectionPass));
                foreach (Contract contract in Contracts)
                {
                    MetricRow[] matches = qpsRows.Where(row => row.Get("hierarchy").Contains(contract.HierarchyNeedle)).ToArray();
                    if (matches.Length != 1)
                    {
                        targets.Add(MissingTarget(contract, matches.Length));
                        continue;
                    }
                    targets.Add(Evaluate(contract, matches[0]));
                }

                string[] unmatched = qpsRows.Where(row => !Contracts.Any(contract => row.Get("hierarchy").Contains(contract.HierarchyNeedle)))
                    .Select(row => row.Get("hierarchy")).ToArray();
                bool discoveryPass = qpsRows.Length == 10 && targets.Count == 10 && unmatched.Length == 0;
                checks.Add(NewCheck("W14-I05.target_discovery", discoveryPass ? "PASS" : "INFRA_FAIL",
                    discoveryPass ? "NONE" : "INFRASTRUCTURE", "P0",
                    "Exactly ten canonical qps-long global targets are classified once",
                    "qpsRows=" + qpsRows.Length + "; canonicalResults=" + targets.Count + "; unmatched=" + unmatched.Length));

                checks.Add(NewCheck("W14-HW01.physical_gamepad", "UNVERIFIED", "HARDWARE_GAP", "P1",
                    "A human validates locale/layout and prompts using a connected physical gamepad",
                    "No physical-device human actuation evidence was collected by this automated gate."));
                checks.Add(NewCheck("W14-S01.steam_release", "UNVERIFIED", "EXTERNAL_RELEASE_GAP", "P0",
                    "Steamworks App ID, depot, Input, Cloud, Achievements, permissions, and store release evidence are configured and reviewed",
                    "Steam readiness is outside this gate and remains NOT_READY."));

                WriteReport(started, metricsPath, screenshotName, targets, checks);
            }
            catch (Exception exception)
            {
                checks.Add(NewCheck("W14-I99.runner", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "Wave 14 emits parseable evidence", exception.ToString()));
                WriteReport(started, metricsPath, screenshotName, targets, checks);
                throw;
            }
        }

        private static TargetResult Evaluate(Contract contract, MetricRow row)
        {
            float glyph = Float(row, "glyph_median_px");
            float block = Float(row, "block_height_px");
            float left = Float(row, "left_px");
            float bottom = Float(row, "bottom_px");
            float right = Float(row, "right_px");
            float top = Float(row, "top_px");
            float contrast = Float(row, "contrast_ratio");
            int lines = Int(row, "line_count");
            float playerRatio = Float(row, "player_occlusion_ratio");
            float walkingRatio = Float(row, "walking_path_occlusion_ratio");
            bool overflow = row.Get("overflow") == "1";
            List<string> failures = new List<string>();
            if (glyph < MinimumGlyphPixels) failures.Add("glyph<16px");
            if (block > contract.MaximumBlockHeight + 0.05f) failures.Add("block-height>" + F(contract.MaximumBlockHeight) + "px");
            if (lines < 1 || lines > contract.MaximumLines) failures.Add("lines>" + contract.MaximumLines);
            if (left < ScreenMargin || bottom < ScreenMargin || right > Wave3VisualGate.Width - ScreenMargin || top > Wave3VisualGate.Height - ScreenMargin)
                failures.Add("outside-4px-safe-area");
            float requiredContrast = glyph >= 24f ? LargeContrast : NormalContrast;
            if (contrast < requiredContrast) failures.Add("contrast<" + F(requiredContrast) + ":1");
            if (overflow) failures.Add("tmp-overflow/clipping");
            if (!string.IsNullOrWhiteSpace(row.Get("text_overlaps"))) failures.Add("text-overlap");
            if (!string.IsNullOrWhiteSpace(row.Get("ui_occlusions"))) failures.Add("ui-occlusion");
            if (playerRatio < 0f) failures.Add("player-rect-unavailable");
            else if (playerRatio > MaximumPlayerOcclusionRatio + 0.0005f) failures.Add("player-occlusion>5%");
            if (walkingRatio > MaximumWalkingPathOcclusionRatio + 0.0005f) failures.Add("walking-path-occlusion>20%");

            bool passed = failures.Count == 0;
            bool expectedGap = !passed && ExpectedBaselineFailures.Contains(contract.Id);
            return new TargetResult
            {
                id = contract.Id,
                matrix = contract.Matrix,
                status = passed ? "PASS" : expectedGap ? "EXPECTED_GAP" : "FAIL",
                classification = passed ? "NONE" : expectedGap ? "PRODUCT_EXPECTED_GAP" : "PRODUCT_REGRESSION",
                severity = "P1",
                screenshot = row.Get("screenshot"),
                hierarchy = row.Get("hierarchy"),
                text = row.Get("text"),
                glyphMedianPx = glyph,
                blockHeightPx = block,
                lineCount = lines,
                maximumLines = contract.MaximumLines,
                maximumBlockHeightPx = contract.MaximumBlockHeight,
                leftPx = left,
                bottomPx = bottom,
                rightPx = right,
                topPx = top,
                safeMarginPx = ScreenMargin,
                contrastRatio = contrast,
                overflow = overflow,
                textOverlaps = row.Get("text_overlaps"),
                uiOcclusions = row.Get("ui_occlusions"),
                playerScreenRect = row.Get("player_screen_rect"),
                walkingPathScreenRect = row.Get("walking_path_screen_rect"),
                playerOcclusionRatio = playerRatio,
                maximumPlayerOcclusionRatio = MaximumPlayerOcclusionRatio,
                walkingPathOcclusionRatio = walkingRatio,
                maximumWalkingPathOcclusionRatio = MaximumWalkingPathOcclusionRatio,
                failures = failures.ToArray(),
                reproduction = "Open " + row.Get("screenshot") + " at 1:1 and compare this row with wave14-qps-global-layout-targets.tsv.",
                recommendedFiles = contract.RecommendedFiles
            };
        }

        private static TargetResult MissingTarget(Contract contract, int count)
        {
            return new TargetResult
            {
                id = contract.Id,
                matrix = contract.Matrix,
                status = "FAIL",
                classification = "PRODUCT_REGRESSION",
                severity = "P1",
                safeMarginPx = ScreenMargin,
                maximumLines = contract.MaximumLines,
                maximumBlockHeightPx = contract.MaximumBlockHeight,
                maximumPlayerOcclusionRatio = MaximumPlayerOcclusionRatio,
                maximumWalkingPathOcclusionRatio = MaximumWalkingPathOcclusionRatio,
                failures = new[] { "canonical-target-count=" + count },
                reproduction = "Generate the qps-long placement-valid capture and inspect the canonical hierarchy target.",
                recommendedFiles = contract.RecommendedFiles
            };
        }

        private static void WriteReport(DateTime started, string metricsPath, string screenshotName,
            List<TargetResult> targets, List<Check> checks)
        {
            string[] reproduced = targets.Where(target => target.status == "EXPECTED_GAP")
                .Select(target => target.id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            string[] expected = ExpectedBaselineFailures.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            int infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL");
            int checkProductFailed = checks.Count(check => check.status == "FAIL" && check.classification == "PRODUCT_REGRESSION");
            int unexpectedTargets = targets.Count(target => target.status == "FAIL");
            int expectedTargets = targets.Count(target => target.status == "EXPECTED_GAP");
            bool exactBaseline = targets.Count == 10 && expectedTargets == 6 && unexpectedTargets == 0 && expected.SequenceEqual(reproduced);
            string infrastructureOverall = infrastructureFailed == 0 ? "PASS" : "FAIL";
            string productOverall = unexpectedTargets + checkProductFailed > 0 ? "FAIL" : expectedTargets > 0 ? "RED_EXPECTED_GAP" : "PASS";
            string overall = infrastructureOverall == "FAIL" || productOverall == "FAIL" ? "FAIL" :
                productOverall == "RED_EXPECTED_GAP" ? "RED" : "GREEN";
            Report report = new Report
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                overall = overall,
                productOverall = productOverall,
                infrastructureOverall = infrastructureOverall,
                targetCount = targets.Count,
                passedTargets = targets.Count(target => target.status == "PASS"),
                expectedGapTargets = expectedTargets,
                unexpectedFailedTargets = unexpectedTargets + checkProductFailed,
                expectedBaselineFailureIds = expected,
                reproducedBaselineFailureIds = reproduced,
                exactSixOfTenBaselineReproduced = exactBaseline,
                greenCompletionCondition = "GREEN requires infrastructure PASS, ko/en normal visual lock PASS, all 10 canonical qps-long targets PASS, and zero EXPECTED_GAP/FAIL targets.",
                physicalGamepad = "UNVERIFIED",
                steamReadiness = "NOT_READY",
                sourceMetrics = Path.GetFileName(metricsPath),
                sourceScreenshot = screenshotName,
                projectionCorrection = "Legacy post-capture 4:3 projection reported the language button outside bounds. Exact 1280x800 projection puts it inside the 4px safe area and separately detects facilities/expansion-planning player occlusion above 5%; current exact gate remains six RED targets.",
                targets = targets.ToArray(),
                checks = checks.ToArray()
            };
            string jsonPath = Path.Combine(EvidenceFolder, "wave14-qps-global-layout-gate.json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true) + Environment.NewLine, Utf8NoBom);

            StringBuilder text = new StringBuilder();
            text.AppendLine(report.title);
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Unity: " + Application.unityVersion);
            text.AppendLine("Overall/Product/Infrastructure: " + overall + "/" + productOverall + "/" + infrastructureOverall);
            text.AppendLine("Targets PASS/EXPECTED_GAP/FAIL: " + report.passedTargets + "/" + report.expectedGapTargets + "/" + report.unexpectedFailedTargets);
            text.AppendLine("Exact reported baseline 6/10 reproduced: " + exactBaseline);
            text.AppendLine("Projection correction: " + report.projectionCorrection);
            text.AppendLine("GREEN condition: " + report.greenCompletionCondition);
            text.AppendLine("Physical gamepad: UNVERIFIED");
            text.AppendLine("Steam: NOT_READY");
            foreach (TargetResult target in targets)
            {
                text.AppendLine(target.id + " | " + target.status + " | glyph=" + F(target.glyphMedianPx) +
                    "px block=" + F(target.blockHeightPx) + "/" + F(target.maximumBlockHeightPx) +
                    "px lines=" + target.lineCount + "/" + target.maximumLines + " bounds=[" + F(target.leftPx) + "," +
                    F(target.bottomPx) + " -> " + F(target.rightPx) + "," + F(target.topPx) + "] player=" +
                    Percent(target.playerOcclusionRatio) + " walking=" + Percent(target.walkingPathOcclusionRatio) +
                    " failures=" + (target.failures == null || target.failures.Length == 0 ? "none" : string.Join(",", target.failures)));
            }
            foreach (Check check in checks)
                text.AppendLine(check.id + " | " + check.status + " | " + check.classification + " | " + check.actual);
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave14-qps-global-layout-gate.txt"), text.ToString(), Utf8NoBom);

            StringBuilder table = new StringBuilder();
            table.AppendLine("id\tmatrix\tstatus\tglyph_px\tblock_height_px\tmax_block_height_px\tline_count\tmax_lines\tleft_px\tbottom_px\tright_px\ttop_px\tplayer_occlusion_ratio\twalking_path_occlusion_ratio\toverflow\tfailures\tscreenshot\thierarchy");
            foreach (TargetResult target in targets)
            {
                table.AppendLine(string.Join("\t", new[]
                {
                    target.id, Clean(target.matrix), target.status, F(target.glyphMedianPx), F(target.blockHeightPx),
                    F(target.maximumBlockHeightPx), target.lineCount.ToString(CultureInfo.InvariantCulture),
                    target.maximumLines.ToString(CultureInfo.InvariantCulture), F(target.leftPx), F(target.bottomPx), F(target.rightPx),
                    F(target.topPx), F(target.playerOcclusionRatio), F(target.walkingPathOcclusionRatio), target.overflow ? "1" : "0",
                    Clean(target.failures == null ? string.Empty : string.Join(",", target.failures)), Clean(target.screenshot), Clean(target.hierarchy)
                }));
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave14-qps-global-layout-targets.tsv"), table.ToString(), Utf8NoBom);
        }

        private static MetricRow[] ReadMetrics(string path, out string[] headers)
        {
            headers = Array.Empty<string>();
            if (!File.Exists(path)) return Array.Empty<MetricRow>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return Array.Empty<MetricRow>();
            headers = lines[0].Split('\t');
            List<MetricRow> rows = new List<MetricRow>();
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex += 1)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex])) continue;
                string[] values = lines[lineIndex].Split('\t');
                MetricRow row = new MetricRow();
                for (int column = 0; column < headers.Length; column += 1)
                    row.Values[headers[column]] = column < values.Length ? values[column] : string.Empty;
                rows.Add(row);
            }
            return rows.ToArray();
        }

        private static bool VerifyPng(string path, int width, int height)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0) return false;
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try { return texture.LoadImage(File.ReadAllBytes(path), false) && texture.width == width && texture.height == height; }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Check NewCheck(string id, string status, string classification, string severity, string expected, string actual)
        {
            return new Check { id = id, status = status, classification = classification, severity = severity, expected = expected, actual = actual };
        }

        private static Contract NewContract(string id, string matrix, string hierarchyNeedle, int maximumLines,
            float maximumBlockHeight, string recommendedFiles)
        {
            return new Contract { Id = id, Matrix = matrix, HierarchyNeedle = hierarchyNeedle, MaximumLines = maximumLines,
                MaximumBlockHeight = maximumBlockHeight, RecommendedFiles = recommendedFiles };
        }

        private static float Float(MetricRow row, string key)
        {
            return float.TryParse(row.Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : -1f;
        }

        private static int Int(MetricRow row, string key)
        {
            return int.TryParse(row.Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : -1;
        }

        private static string F(float value) { return value.ToString("0.000", CultureInfo.InvariantCulture); }
        private static string Percent(float ratio) { return ratio < 0f ? "UNAVAILABLE" : F(ratio * 100f) + "%"; }
        private static string Clean(string value) { return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim(); }
        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }
    }
}
