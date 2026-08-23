using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KimSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Non-shipping Wave 3 visual regression gate. It projects generated TMP
    /// character quads into the exact 1280x800 capture viewport and evaluates
    /// pixel height, viewport bounds, contrast, text overlap, and UI occlusion.
    /// </summary>
    internal static class Wave3VisualGate
    {
        internal const int Width = 1280;
        internal const int Height = 800;
        private const float ScreenMarginPixels = 4f;
        private const float PlacementStatusMinimumGlyphPixels = 18f;
        private const float PlacementWorldMinimumGlyphPixels = 16f;
        private const float ExplorationWorldMinimumGlyphPixels = 18f;
        private const float PseudoLongMinimumGlyphPixels = 16f;
        private const float NormalTextContrast = 4.5f;
        private const float LargeTextContrast = 3f;
        private const float SignificantOverlapRatio = 0.15f;
        private const float SignificantOcclusionRatio = 0.20f;

        internal sealed class TextMetric
        {
            public string Scenario;
            public string Screenshot;
            public string Category;
            public string Hierarchy;
            public string Value;
            public Rect Bounds;
            public float GlyphMedianPixels;
            public float BlockHeightPixels;
            public float ContrastRatio;
            public string BackgroundSource;
            public bool IsWorldText;
            public int LineCount;
            public bool HasPlayerRegion;
            public Rect PlayerRegion;
            public Rect WalkingPathRegion;
            public float PlayerOcclusionRatio;
            public float WalkingPathOcclusionRatio;
            public bool Overflow;
            public bool BoundsPass;
            public bool HeightPass;
            public bool ContrastPass;
            public bool OverlapPass = true;
            public bool OcclusionPass = true;
            public readonly List<string> Overlaps = new List<string>();
            public readonly List<string> Occlusions = new List<string>();

            public bool IsGated
            {
                get
                {
                    return Category == "placement-status" ||
                           Category == "placement-world" ||
                           Category == "exploration-world" ||
                           Category == "pseudo-long";
                }
            }

            public bool Passed
            {
                get { return !IsGated || (HeightPass && BoundsPass && ContrastPass && !Overflow && OverlapPass && OcclusionPass); }
            }

            public string FailureSummary
            {
                get
                {
                    List<string> failures = new List<string>();
                    if (!HeightPass) failures.Add("height");
                    if (!BoundsPass) failures.Add("bounds");
                    if (!ContrastPass) failures.Add("contrast");
                    if (Overflow) failures.Add("overflow");
                    if (!OverlapPass) failures.Add("text-overlap");
                    if (!OcclusionPass) failures.Add("ui-occlusion");
                    return failures.Count == 0 ? "none" : string.Join(",", failures);
                }
            }
        }

        internal sealed class FrameResult
        {
            public string Scenario;
            public string Screenshot;
            public readonly List<TextMetric> Metrics = new List<TextMetric>();

            public IEnumerable<TextMetric> GatedMetrics
            {
                get { return Metrics.Where(metric => metric.IsGated); }
            }

            public bool Passed
            {
                get { return GatedMetrics.Any() && GatedMetrics.All(metric => metric.Passed); }
            }
        }

        internal static FrameResult Analyze(string scenario, string screenshotPath, Camera camera, TMP_Text[] texts)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture analysisTarget = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = null;
            try
            {
                camera.targetTexture = analysisTarget;
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < texts.Length; i += 1)
                {
                    if (texts[i] != null) texts[i].ForceMeshUpdate(true, true);
                }
                screenshot = LoadScreenshot(screenshotPath);
                FrameResult frame = new FrameResult
                {
                    Scenario = scenario,
                    Screenshot = Path.GetFileName(screenshotPath)
                };

                for (int i = 0; i < texts.Length; i += 1)
                {
                    TextMetric metric = MeasureText(scenario, frame.Screenshot, camera, texts[i], screenshot);
                    if (metric != null)
                    {
                        frame.Metrics.Add(metric);
                    }
                }

                EvaluateTextOverlaps(frame.Metrics);
                EvaluateUiOcclusion(frame.Metrics, camera);
                MeasureProtectedWorldRegions(frame.Metrics, camera);
                return frame;
            }
            finally
            {
                if (screenshot != null) UnityEngine.Object.DestroyImmediate(screenshot);
                camera.targetTexture = previousTarget;
                Canvas.ForceUpdateCanvases();
                RenderTexture.ReleaseTemporary(analysisTarget);
            }
        }

        internal static string ExpandPseudoLong(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            const string accents = "áéíóúüñ";
            int targetLength = Mathf.Max(value.Length + 4, Mathf.CeilToInt(value.Length * 1.42f));
            StringBuilder builder = new StringBuilder(targetLength + 4);
            builder.Append("[[");
            int accentIndex = 0;
            for (int i = 0; i < value.Length; i += 1)
            {
                char current = value[i];
                builder.Append(current);
                if (char.IsLetter(current) && builder.Length < targetLength - 1 && (i % 2 == 0 || builder.Length + (value.Length - i) < targetLength))
                {
                    builder.Append(accents[accentIndex % accents.Length]);
                    accentIndex += 1;
                }
            }

            while (builder.Length < targetLength - 1)
            {
                builder.Append(accents[accentIndex % accents.Length]);
                accentIndex += 1;
            }
            builder.Append("]]");
            return builder.ToString();
        }

        internal static bool WriteReports(
            string evidenceFolder,
            string runId,
            string baselineCommit,
            string unityVersion,
            string command,
            DateTime started,
            IReadOnlyList<FrameResult> frames)
        {
            Directory.CreateDirectory(evidenceFolder);
            List<TextMetric> metrics = frames.SelectMany(frame => frame.Metrics).ToList();
            List<TextMetric> placement = metrics.Where(metric => metric.Category == "placement-status" || metric.Category == "placement-world").ToList();
            List<TextMetric> exploration = metrics.Where(metric => metric.Category == "exploration-world").ToList();
            List<TextMetric> pseudo = metrics.Where(metric => metric.Category == "pseudo-long").ToList();

            bool placementPass = GroupPass(placement);
            bool explorationPass = GroupPass(exploration);
            bool pseudoPass = GroupPass(pseudo);
            bool overallPass = placementPass && explorationPass && pseudoPass;

            StringBuilder report = new StringBuilder();
            report.AppendLine("Wave 3 1280x800 projected-text visual gate");
            report.AppendLine("Run ID: " + runId);
            report.AppendLine("Started UTC: " + started.ToString("O"));
            report.AppendLine("Completed UTC: " + DateTime.UtcNow.ToString("O"));
            report.AppendLine("Unity: " + unityVersion);
            report.AppendLine("Baseline commit: " + baselineCommit);
            report.AppendLine("Command: " + command);
            report.AppendLine("Method: visible TMP character quads projected with Camera.WorldToViewportPoint into an exact 1280x800 coordinate space; contrast uses the nearest rendered UI/badge background or a screenshot-border median sample. Line count plus actual player-renderer and full-width camp walking-band Rect overlap are serialized for Wave 14 without changing the legacy Wave 3 pass/fail thresholds.");
            report.AppendLine("Thresholds: placement status >=18px; placement world badge >=16px; exploration/swimming world label >=18px; pseudo-long >=16px; 4px viewport margin; WCAG-style contrast >=4.5:1 (<24px) or >=3.0:1 (>=24px); significant text overlap <15%; world-text UI occlusion <20%.");
            report.AppendLine("PLACEMENT_GATE: " + Status(placementPass, placement) + " · targets=" + placement.Count + " · failures=" + placement.Count(metric => !metric.Passed));
            report.AppendLine("EXPLORATION_SWIMMING_GATE: " + Status(explorationPass, exploration) + " · targets=" + exploration.Count + " · failures=" + exploration.Count(metric => !metric.Passed));
            report.AppendLine("PSEUDO_LONG_GATE: " + Status(pseudoPass, pseudo) + " · targets=" + pseudo.Count + " · failures=" + pseudo.Count(metric => !metric.Passed));
            report.AppendLine("OVERALL: " + (overallPass ? "PASS" : "FAIL"));
            foreach (FrameResult frame in frames)
            {
                int gatedCount = frame.GatedMetrics.Count();
                string frameStatus = gatedCount == 0 ? "NOT_APPLICABLE" : (frame.Passed ? "PASS" : "FAIL");
                report.AppendLine(frameStatus + " · " + frame.Scenario + " · gated=" + gatedCount + " · screenshot=" + frame.Screenshot);
            }

            foreach (TextMetric metric in metrics.Where(metric => metric.IsGated && !metric.Passed))
            {
                report.AppendLine("  FAIL · " + metric.Category + " · " + metric.Scenario + " · " + Normalize(metric.Value) +
                                  " · glyph=" + F(metric.GlyphMedianPixels) + "px · block=" + F(metric.BlockHeightPixels) +
                                  "px · bounds=" + FormatBounds(metric.Bounds) + " · contrast=" + F(metric.ContrastRatio) +
                                  ":1 · failures=" + metric.FailureSummary);
            }
            File.WriteAllText(Path.Combine(evidenceFolder, "wave3-visual-gate.txt"), report.ToString(), new UTF8Encoding(false));

            StringBuilder table = new StringBuilder();
            table.AppendLine("scenario\tscreenshot\tcategory\tstatus\tglyph_median_px\tblock_height_px\tleft_px\tbottom_px\tright_px\ttop_px\tcontrast_ratio\tbackground\toverflow\ttext_overlaps\tui_occlusions\tline_count\tplayer_screen_rect\twalking_path_screen_rect\tplayer_occlusion_ratio\twalking_path_occlusion_ratio\tfailures\thierarchy\ttext");
            foreach (TextMetric metric in metrics.OrderBy(metric => metric.Scenario).ThenBy(metric => metric.Category).ThenBy(metric => metric.Hierarchy))
            {
                table.AppendLine(string.Join("\t", new[]
                {
                    Tsv(metric.Scenario),
                    Tsv(metric.Screenshot),
                    metric.Category,
                    metric.Passed ? "PASS" : (metric.IsGated ? "FAIL" : "INFO"),
                    F(metric.GlyphMedianPixels),
                    F(metric.BlockHeightPixels),
                    F(metric.Bounds.xMin),
                    F(metric.Bounds.yMin),
                    F(metric.Bounds.xMax),
                    F(metric.Bounds.yMax),
                    F(metric.ContrastRatio),
                    Tsv(metric.BackgroundSource),
                    metric.Overflow ? "1" : "0",
                    Tsv(string.Join(" | ", metric.Overlaps)),
                    Tsv(string.Join(" | ", metric.Occlusions)),
                    metric.LineCount.ToString(CultureInfo.InvariantCulture),
                    metric.HasPlayerRegion ? FormatRect(metric.PlayerRegion) : "UNAVAILABLE",
                    FormatRect(metric.WalkingPathRegion),
                    metric.HasPlayerRegion ? Ratio(metric.PlayerOcclusionRatio) : "-1.0000",
                    Ratio(metric.WalkingPathOcclusionRatio),
                    metric.FailureSummary,
                    Tsv(metric.Hierarchy),
                    Tsv(Normalize(metric.Value))
                }));
            }
            File.WriteAllText(Path.Combine(evidenceFolder, "wave3-visual-metrics.tsv"), table.ToString(), new UTF8Encoding(false));
            return overallPass;
        }

        private static TextMetric MeasureText(string scenario, string screenshot, Camera camera, TMP_Text text, Texture2D image)
        {
            if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text))
            {
                return null;
            }

            text.ForceMeshUpdate(true, true);
            List<float> glyphHeights = new List<float>();
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < text.textInfo.characterCount; i += 1)
            {
                TMP_CharacterInfo character = text.textInfo.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                Vector2 bottomLeft = Project(camera, text.transform.TransformPoint(character.bottomLeft));
                Vector2 topLeft = Project(camera, text.transform.TransformPoint(character.topLeft));
                Vector2 topRight = Project(camera, text.transform.TransformPoint(character.topRight));
                Vector2 bottomRight = Project(camera, text.transform.TransformPoint(character.bottomRight));
                Vector2[] points = { bottomLeft, topLeft, topRight, bottomRight };
                float glyphMinY = points.Min(point => point.y);
                float glyphMaxY = points.Max(point => point.y);
                glyphHeights.Add(glyphMaxY - glyphMinY);
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex += 1)
                {
                    minX = Mathf.Min(minX, points[pointIndex].x);
                    minY = Mathf.Min(minY, points[pointIndex].y);
                    maxX = Mathf.Max(maxX, points[pointIndex].x);
                    maxY = Mathf.Max(maxY, points[pointIndex].y);
                }
            }

            if (glyphHeights.Count == 0 || maxX <= 0f || maxY <= 0f || minX >= Width || minY >= Height)
            {
                return null;
            }

            glyphHeights.Sort();
            float medianGlyph = glyphHeights[glyphHeights.Count / 2];
            Rect bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
            Color background = ResolveBackground(text, image, bounds, out string backgroundSource);
            Color foreground = Composite(text.color, background);
            float contrast = Contrast(foreground, background);
            string category = Category(scenario, text);
            float minimumHeight = MinimumHeight(category);
            float requiredContrast = medianGlyph >= 24f ? LargeTextContrast : NormalTextContrast;
            return new TextMetric
            {
                Scenario = scenario,
                Screenshot = screenshot,
                Category = category,
                Hierarchy = HierarchyPath(text.transform),
                Value = text.text,
                Bounds = bounds,
                GlyphMedianPixels = medianGlyph,
                BlockHeightPixels = bounds.height,
                ContrastRatio = contrast,
                BackgroundSource = backgroundSource,
                IsWorldText = text is TextMeshPro,
                LineCount = Mathf.Max(1, text.textInfo.lineCount),
                Overflow = text.isTextOverflowing,
                BoundsPass = bounds.xMin >= ScreenMarginPixels && bounds.yMin >= ScreenMarginPixels && bounds.xMax <= Width - ScreenMarginPixels && bounds.yMax <= Height - ScreenMarginPixels,
                HeightPass = minimumHeight <= 0f || medianGlyph >= minimumHeight,
                ContrastPass = !IsGatedCategory(category) || contrast >= requiredContrast
            };
        }

        private static void EvaluateTextOverlaps(List<TextMetric> metrics)
        {
            for (int i = 0; i < metrics.Count; i += 1)
            {
                for (int j = i + 1; j < metrics.Count; j += 1)
                {
                    TextMetric left = metrics[i];
                    TextMetric right = metrics[j];
                    Rect intersection = Intersect(left.Bounds, right.Bounds);
                    if (intersection.width <= 0f || intersection.height <= 0f)
                    {
                        continue;
                    }

                    float denominator = Mathf.Max(1f, Mathf.Min(left.Bounds.width * left.Bounds.height, right.Bounds.width * right.Bounds.height));
                    float ratio = intersection.width * intersection.height / denominator;
                    if (ratio < SignificantOverlapRatio)
                    {
                        continue;
                    }

                    string leftDescription = right.Category + ":" + Normalize(right.Value) + "=" + F(ratio * 100f) + "%";
                    string rightDescription = left.Category + ":" + Normalize(left.Value) + "=" + F(ratio * 100f) + "%";
                    left.Overlaps.Add(leftDescription);
                    right.Overlaps.Add(rightDescription);
                    if (left.IsGated) left.OverlapPass = false;
                    if (right.IsGated) right.OverlapPass = false;
                }
            }
        }

        private static void EvaluateUiOcclusion(List<TextMetric> metrics, Camera camera)
        {
            Image[] images = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude);
            List<Tuple<string, Rect>> panels = new List<Tuple<string, Rect>>();
            foreach (Image image in images)
            {
                if (image == null || !image.gameObject.activeInHierarchy || image.color.a < 0.5f)
                {
                    continue;
                }

                RectTransform rect = image.rectTransform;
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector2[] projected = corners.Select(corner => Project(camera, corner)).ToArray();
                panels.Add(Tuple.Create(HierarchyPath(image.transform), Rect.MinMaxRect(
                    projected.Min(point => point.x),
                    projected.Min(point => point.y),
                    projected.Max(point => point.x),
                    projected.Max(point => point.y))));
            }

            foreach (TextMetric metric in metrics.Where(metric => metric.IsGated && metric.IsWorldText))
            {
                foreach (Tuple<string, Rect> panel in panels)
                {
                    Rect intersection = Intersect(metric.Bounds, panel.Item2);
                    if (intersection.width <= 0f || intersection.height <= 0f)
                    {
                        continue;
                    }

                    float ratio = intersection.width * intersection.height / Mathf.Max(1f, metric.Bounds.width * metric.Bounds.height);
                    if (ratio < SignificantOcclusionRatio)
                    {
                        continue;
                    }

                    metric.Occlusions.Add(panel.Item1 + "=" + F(ratio * 100f) + "%");
                    metric.OcclusionPass = false;
                }
            }
        }

        private static void MeasureProtectedWorldRegions(List<TextMetric> metrics, Camera camera)
        {
            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            FieldInfo playerRootField = prototype == null ? null : typeof(KimSurvivalPrototype).GetField(
                "playerRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Transform playerRoot = playerRootField == null ? null : playerRootField.GetValue(prototype) as Transform;
            Renderer[] playerRenderers = playerRoot == null
                ? Array.Empty<Renderer>()
                : playerRoot.GetComponentsInChildren<Renderer>(false).Where(renderer => renderer != null && renderer.enabled).ToArray();
            bool hasPlayer = playerRenderers.Length > 0;
            Rect playerRect = new Rect();
            if (hasPlayer)
            {
                Bounds bounds = playerRenderers[0].bounds;
                for (int i = 1; i < playerRenderers.Length; i += 1)
                {
                    bounds.Encapsulate(playerRenderers[i].bounds);
                }
                playerRect = ProjectWorldRect(camera,
                    new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y));
            }

            float halfWidth = camera.orthographicSize * camera.aspect;
            Rect walkingWorld = new Rect(
                camera.transform.position.x - halfWidth,
                PrototypeCampPlacement.FloorY - 0.25f,
                halfWidth * 2f,
                1.15f);
            Rect walkingRect = ProjectWorldRect(camera, walkingWorld);

            foreach (TextMetric metric in metrics)
            {
                metric.HasPlayerRegion = hasPlayer;
                metric.PlayerRegion = playerRect;
                metric.WalkingPathRegion = walkingRect;
                metric.PlayerOcclusionRatio = hasPlayer ? IntersectionRatio(metric.Bounds, playerRect, playerRect) : -1f;
                metric.WalkingPathOcclusionRatio = IntersectionRatio(metric.Bounds, walkingRect, walkingRect);
            }
        }

        private static Rect ProjectWorldRect(Camera camera, Rect worldRect)
        {
            Vector2[] points =
            {
                Project(camera, new Vector3(worldRect.xMin, worldRect.yMin, 0f)),
                Project(camera, new Vector3(worldRect.xMin, worldRect.yMax, 0f)),
                Project(camera, new Vector3(worldRect.xMax, worldRect.yMin, 0f)),
                Project(camera, new Vector3(worldRect.xMax, worldRect.yMax, 0f))
            };
            return Rect.MinMaxRect(
                points.Min(point => point.x),
                points.Min(point => point.y),
                points.Max(point => point.x),
                points.Max(point => point.y));
        }

        private static float IntersectionRatio(Rect subject, Rect protectedRegion, Rect denominatorRegion)
        {
            Rect intersection = Intersect(subject, protectedRegion);
            if (intersection.width <= 0f || intersection.height <= 0f)
            {
                return 0f;
            }
            return intersection.width * intersection.height /
                   Mathf.Max(1f, denominatorRegion.width * denominatorRegion.height);
        }

        private static Texture2D LoadScreenshot(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Wave 3 screenshot is missing", path);
            }

            Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!image.LoadImage(File.ReadAllBytes(path)) || image.width != Width || image.height != Height)
            {
                UnityEngine.Object.DestroyImmediate(image);
                throw new InvalidDataException("Wave 3 screenshot must be exactly 1280x800: " + path);
            }
            return image;
        }

        private static Color ResolveBackground(TMP_Text text, Texture2D image, Rect bounds, out string source)
        {
            Transform cursor = text.transform.parent;
            while (cursor != null)
            {
                Image panel = cursor.GetComponent<Image>();
                if (panel != null && panel.color.a >= 0.5f)
                {
                    source = "ui-image:" + cursor.name;
                    return panel.color;
                }

                if (cursor == text.transform.parent)
                {
                    SpriteRenderer[] renderers = cursor.GetComponentsInChildren<SpriteRenderer>(true);
                    SpriteRenderer badge = renderers.FirstOrDefault(renderer => renderer != null && renderer.gameObject.name == "안내 배경" && renderer.color.a >= 0.5f);
                    if (badge != null)
                    {
                        source = "world-badge:" + cursor.name;
                        return badge.color;
                    }
                }
                cursor = cursor.parent;
            }

            source = "screenshot-border-median";
            return SampleBorderMedian(image, bounds);
        }

        private static Color SampleBorderMedian(Texture2D image, Rect bounds)
        {
            List<Color> samples = new List<Color>();
            int left = Mathf.Clamp(Mathf.FloorToInt(bounds.xMin) - 3, 0, Width - 1);
            int right = Mathf.Clamp(Mathf.CeilToInt(bounds.xMax) + 3, 0, Width - 1);
            int bottom = Mathf.Clamp(Mathf.FloorToInt(bounds.yMin) - 3, 0, Height - 1);
            int top = Mathf.Clamp(Mathf.CeilToInt(bounds.yMax) + 3, 0, Height - 1);
            for (int i = 0; i < 12; i += 1)
            {
                float t = i / 11f;
                int x = Mathf.RoundToInt(Mathf.Lerp(left, right, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(bottom, top, t));
                samples.Add(image.GetPixel(x, bottom));
                samples.Add(image.GetPixel(x, top));
                samples.Add(image.GetPixel(left, y));
                samples.Add(image.GetPixel(right, y));
            }
            return new Color(Median(samples.Select(color => color.r)), Median(samples.Select(color => color.g)), Median(samples.Select(color => color.b)), 1f);
        }

        private static float Median(IEnumerable<float> values)
        {
            float[] sorted = values.OrderBy(value => value).ToArray();
            return sorted.Length == 0 ? 0f : sorted[sorted.Length / 2];
        }

        private static Color Composite(Color foreground, Color background)
        {
            return new Color(
                foreground.r * foreground.a + background.r * (1f - foreground.a),
                foreground.g * foreground.a + background.g * (1f - foreground.a),
                foreground.b * foreground.a + background.b * (1f - foreground.a),
                1f);
        }

        private static float Contrast(Color foreground, Color background)
        {
            float lighter = Mathf.Max(Luminance(foreground), Luminance(background));
            float darker = Mathf.Min(Luminance(foreground), Luminance(background));
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float Luminance(Color color)
        {
            return 0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static Vector2 Project(Camera camera, Vector3 world)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            return new Vector2(viewport.x * Width, viewport.y * Height);
        }

        private static string Category(string scenario, TMP_Text text)
        {
            if (scenario.IndexOf("qps-long", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "pseudo-long";
            }

            string hierarchy = HierarchyPath(text.transform);
            if (scenario.IndexOf("placement", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (text is TextMeshProUGUI && text.name == "김씨 독백 또는 배치 상태")
                {
                    return "placement-status";
                }
                if (text is TextMeshPro && (hierarchy.Contains("배치 판정") || hierarchy.Contains("안내")))
                {
                    return "placement-world";
                }
            }

            if (text is TextMeshPro && (hierarchy.Contains("Gather ·") || hierarchy.Contains("Water Search ·")))
            {
                return "exploration-world";
            }
            return text is TextMeshProUGUI ? "ui-info" : "world-info";
        }

        private static float MinimumHeight(string category)
        {
            switch (category)
            {
                case "placement-status": return PlacementStatusMinimumGlyphPixels;
                case "placement-world": return PlacementWorldMinimumGlyphPixels;
                case "exploration-world": return ExplorationWorldMinimumGlyphPixels;
                case "pseudo-long": return PseudoLongMinimumGlyphPixels;
                default: return 0f;
            }
        }

        private static bool IsGatedCategory(string category)
        {
            return category == "placement-status" || category == "placement-world" || category == "exploration-world" || category == "pseudo-long";
        }

        private static Rect Intersect(Rect left, Rect right)
        {
            float xMin = Mathf.Max(left.xMin, right.xMin);
            float yMin = Mathf.Max(left.yMin, right.yMin);
            float xMax = Mathf.Min(left.xMax, right.xMax);
            float yMax = Mathf.Min(left.yMax, right.yMax);
            return xMax <= xMin || yMax <= yMin ? new Rect() : Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static string HierarchyPath(Transform transform)
        {
            List<string> names = new List<string>();
            Transform cursor = transform;
            while (cursor != null)
            {
                names.Add(cursor.name);
                cursor = cursor.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }

        private static bool GroupPass(List<TextMetric> metrics)
        {
            return metrics.Count > 0 && metrics.All(metric => metric.Passed);
        }

        private static string Status(bool passed, List<TextMetric> metrics)
        {
            return metrics.Count == 0 ? "UNVERIFIED" : (passed ? "PASS" : "FAIL");
        }

        private static string FormatBounds(Rect bounds)
        {
            return "[" + F(bounds.xMin) + "," + F(bounds.yMin) + " -> " + F(bounds.xMax) + "," + F(bounds.yMax) + "]";
        }

        private static string FormatRect(Rect rect)
        {
            return "x=" + F(rect.x) + ",y=" + F(rect.y) + ",w=" + F(rect.width) + ",h=" + F(rect.height);
        }

        private static string F(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string Ratio(float value)
        {
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', '/').Replace('\t', ' ').Trim();
        }

        private static string Tsv(string value)
        {
            return Normalize(value);
        }
    }
}
