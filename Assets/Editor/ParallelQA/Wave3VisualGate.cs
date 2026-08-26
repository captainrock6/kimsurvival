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
        private const float PlacementStatusMinimumGlyphPixels = 12f;
        private const float ExplorationWorldMinimumGlyphPixels = 12f;
        private const float SearchTrayMinimumGlyphPixels = 8f;
        private const float PseudoLongMinimumGlyphPixels = 8f;
        private const float QpsProximityMinimumGlyphPixels = 10f;
        private const float NormalTextContrast = 4.5f;
        private const float LargeTextContrast = 3f;
        private const float SignificantOverlapRatio = 0.15f;
        private const float SignificantOcclusionRatio = 0.20f;

        internal sealed class TextMetric
        {
            internal TMP_Text Source;
            public string Scenario;
            public string Screenshot;
            public string Category;
            public string Hierarchy;
            public string Value;
            public Rect Bounds;
            public float GlyphMedianPixels;
            public float BlockHeightPixels;
            public float FontSizePoints;
            public float FontSizeMinimumPoints;
            public float RequiredFontSizePoints;
            public float ContrastRatio;
            public string BackgroundSource;
            public bool IsWorldText;
            public int LineCount;
            public bool HasPlayerRegion;
            public Rect PlayerRegion;
            public Rect WalkingPathRegion;
            public float PlayerOcclusionRatio;
            public float WalkingPathOcclusionRatio;
            public bool HasPanelBounds;
            public Rect PanelBounds;
            public bool HasNodeVisualBounds;
            public Rect NodeVisualBounds;
            public float PanelNodeIntersectionPixels;
            public float PanelPlayerIntersectionPixels;
            public bool Overflow;
            public bool BoundsPass;
            public bool HeightPass;
            public bool FontSizePass;
            public bool ContrastPass;
            public bool OverlapPass = true;
            public bool OcclusionPass = true;
            public bool VisibilityPass = true;
            public bool WorldGeometryPass = true;
            public readonly List<string> Overlaps = new List<string>();
            public readonly List<string> Occlusions = new List<string>();
            public readonly List<string> WorldGeometryFailures = new List<string>();

            public bool IsGated
            {
                get
                {
                    return Category == "placement-status" ||
                           Category == "placement-world-badge" ||
                           Category == "exploration-world" ||
                           Category == "search-tray" ||
                           Category == "pseudo-long";
                }
            }

            public bool Passed
            {
                get { return !IsGated || (VisibilityPass && WorldGeometryPass && HeightPass && FontSizePass && BoundsPass && ContrastPass && !Overflow && OverlapPass && OcclusionPass); }
            }

            public string FailureSummary
            {
                get
                {
                    List<string> failures = new List<string>();
                    if (!VisibilityPass) failures.Add("forbidden-visible");
                    if (!WorldGeometryPass) failures.AddRange(WorldGeometryFailures);
                    if (!HeightPass) failures.Add("height");
                    if (!FontSizePass) failures.Add("font-size");
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

                CaptureVisiblePlacementWorldBadges(frame, camera);
                EvaluateTextOverlaps(frame.Metrics);
                EvaluateUiOcclusion(frame.Metrics, camera);
                MeasureProtectedWorldRegions(frame.Metrics, camera);
                EvaluateWorldGeometry(frame.Metrics, camera);
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
            List<TextMetric> placement = metrics.Where(metric => metric.Category == "placement-status" || metric.Category == "placement-world-badge").ToList();
            List<TextMetric> exploration = metrics.Where(metric => metric.Category == "exploration-world").ToList();
            List<TextMetric> searchTray = metrics.Where(metric => metric.Category == "search-tray").ToList();
            List<TextMetric> pseudo = metrics.Where(metric => metric.Category == "pseudo-long").ToList();

            bool placementPass = GroupPass(placement, 4);
            bool explorationPass = GroupPass(exploration, 4);
            bool searchTrayPass = GroupPass(searchTray, 16);
            bool pseudoPass = GroupPass(pseudo, 37);
            bool overallPass = placementPass && explorationPass && searchTrayPass && pseudoPass;

            StringBuilder report = new StringBuilder();
            report.AppendLine("Wave 3 1280x800 projected-text visual gate");
            report.AppendLine("Run ID: " + runId);
            report.AppendLine("Started UTC: " + started.ToString("O"));
            report.AppendLine("Completed UTC: " + DateTime.UtcNow.ToString("O"));
            report.AppendLine("Unity: " + unityVersion);
            report.AppendLine("Baseline commit: " + baselineCommit);
            report.AppendLine("Command: " + command);
            report.AppendLine("Method: visible TMP character quads from the current production hierarchy are projected with Camera.WorldToViewportPoint into an exact 1280x800 coordinate space; contrast uses the nearest rendered UI/badge background or a screenshot-border median sample. Source TMP font floors mirror the runtime contracts, while projected glyph floors guard against collapsed transforms. The placement validity world badge is forbidden-visible: validity belongs to the top status card plus placement outline. Environmental-search geometry projects the actual 안내 배경 SpriteRenderer and owning node-renderer union, not only the text bounds.");
            report.AppendLine("Target topology: placement 4 = ko/en valid+invalid status cards and zero visible world OK/× badges; exploration/swimming 4 = one nearest environmental-node detail in each ko/en swimming/exploration frame; normal search tray 16 = ko/en eight live tray labels; qps-long 37 = placement 5 + camp proximity 5 + module popup 8 + fresh-pity search tray 19. Protected-part trays are verified by the separate Wave B contract and must not leak across visual scenarios. The informational placement world badge is excluded and must stay hidden; the open tray intentionally hides the nearest detailed world-node label.");
            report.AppendLine("Thresholds: placement status source fontMin >=26 and projected glyph >=12px; environmental-search label source fontMin >=28 and projected glyph >=12px, with its actual background center left of the owning node center and direct background/node/player intersection exactly 0; compact search-tray title/status/bag/other runtime floors >=18/15/13/12.5 and projected glyph >=8px; qps-long search-tray HUD uses status/resources >=26 and controls/language/bag >=18; qps camp proximity uses source fontMin >=18 and projected glyph >=10px; other qps contract text uses its hierarchy-specific runtime floor and projected glyph >=8px; 4px viewport margin; contrast >=4.5:1 (<24px) or >=3.0:1 (>=24px); significant text overlap <15%; world-text UI occlusion <20%.");
            report.AppendLine("PLACEMENT_GATE: " + Status(placementPass, placement) + " · targets=" + placement.Count + " · failures=" + placement.Count(metric => !metric.Passed));
            report.AppendLine("EXPLORATION_SWIMMING_GATE: " + Status(explorationPass, exploration) + " · targets=" + exploration.Count + " · failures=" + exploration.Count(metric => !metric.Passed));
            report.AppendLine("SEARCH_TRAY_GATE: " + Status(searchTrayPass, searchTray) + " · targets=" + searchTray.Count + " · failures=" + searchTray.Count(metric => !metric.Passed));
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
                                  " · glyph=" + F(metric.GlyphMedianPixels) + "px · font=" + F(metric.FontSizePoints) +
                                  "/min=" + F(metric.FontSizeMinimumPoints) + "/required=" + F(metric.RequiredFontSizePoints) +
                                  "pt · block=" + F(metric.BlockHeightPixels) +
                                   "px · bounds=" + FormatBounds(metric.Bounds) + " · contrast=" + F(metric.ContrastRatio) +
                                   ":1 · panel=" + (metric.HasPanelBounds ? FormatBounds(metric.PanelBounds) : "UNAVAILABLE") +
                                   " · node=" + (metric.HasNodeVisualBounds ? FormatBounds(metric.NodeVisualBounds) : "UNAVAILABLE") +
                                   " · failures=" + metric.FailureSummary);
            }
            File.WriteAllText(Path.Combine(evidenceFolder, "wave3-visual-gate.txt"), report.ToString(), new UTF8Encoding(false));

            StringBuilder table = new StringBuilder();
            table.AppendLine("scenario\tscreenshot\tcategory\tstatus\tglyph_median_px\tfont_size_pt\tfont_size_min_pt\trequired_font_size_pt\tblock_height_px\tleft_px\tbottom_px\tright_px\ttop_px\tcontrast_ratio\tbackground\toverflow\ttext_overlaps\tui_occlusions\tline_count\tplayer_screen_rect\twalking_path_screen_rect\tplayer_occlusion_ratio\twalking_path_occlusion_ratio\tpanel_screen_rect\tnode_visual_screen_rect\tpanel_node_intersection_px2\tpanel_player_intersection_px2\tworld_geometry_failures\tfailures\thierarchy\ttext");
            foreach (TextMetric metric in metrics.OrderBy(metric => metric.Scenario).ThenBy(metric => metric.Category).ThenBy(metric => metric.Hierarchy))
            {
                table.AppendLine(string.Join("\t", new[]
                {
                    Tsv(metric.Scenario),
                    Tsv(metric.Screenshot),
                    metric.Category,
                    metric.Passed ? "PASS" : (metric.IsGated ? "FAIL" : "INFO"),
                    F(metric.GlyphMedianPixels),
                    F(metric.FontSizePoints),
                    F(metric.FontSizeMinimumPoints),
                    F(metric.RequiredFontSizePoints),
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
                    metric.HasPanelBounds ? FormatRect(metric.PanelBounds) : "UNAVAILABLE",
                    metric.HasNodeVisualBounds ? FormatRect(metric.NodeVisualBounds) : "UNAVAILABLE",
                    F(metric.PanelNodeIntersectionPixels),
                    F(metric.PanelPlayerIntersectionPixels),
                    Tsv(string.Join(" | ", metric.WorldGeometryFailures)),
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
            float minimumHeight = MinimumHeight(category, scenario, text);
            float requiredFontSize = RequiredFontSize(category, scenario, text);
            bool useConfiguredMinimum = category == "placement-status" || category == "exploration-world" ||
                                        (category == "pseudo-long" && UsesConfiguredMinimumFont(scenario, text));
            float observedFontSize = useConfiguredMinimum ? text.fontSizeMin : text.fontSize;
            float requiredContrast = medianGlyph >= 24f ? LargeTextContrast : NormalTextContrast;
            return new TextMetric
            {
                Source = text,
                Scenario = scenario,
                Screenshot = screenshot,
                Category = category,
                Hierarchy = HierarchyPath(text.transform),
                Value = text.text,
                Bounds = bounds,
                GlyphMedianPixels = medianGlyph,
                BlockHeightPixels = bounds.height,
                FontSizePoints = text.fontSize,
                FontSizeMinimumPoints = text.fontSizeMin,
                RequiredFontSizePoints = requiredFontSize,
                ContrastRatio = contrast,
                BackgroundSource = backgroundSource,
                IsWorldText = text is TextMeshPro,
                LineCount = Mathf.Max(1, text.textInfo.lineCount),
                Overflow = text.isTextOverflowing,
                BoundsPass = bounds.xMin >= ScreenMarginPixels && bounds.yMin >= ScreenMarginPixels && bounds.xMax <= Width - ScreenMarginPixels && bounds.yMax <= Height - ScreenMarginPixels,
                HeightPass = minimumHeight <= 0f || medianGlyph >= minimumHeight,
                FontSizePass = requiredFontSize <= 0f || observedFontSize + 0.01f >= requiredFontSize,
                ContrastPass = !IsGatedCategory(category) || contrast >= requiredContrast,
                VisibilityPass = category != "placement-world-badge"
            };
        }

        private static void CaptureVisiblePlacementWorldBadges(FrameResult frame, Camera camera)
        {
            TextMeshPro[] worldTexts = UnityEngine.Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);
            foreach (TextMeshPro label in worldTexts)
            {
                if (label == null || frame.Metrics.Any(metric => ReferenceEquals(metric.Source, label)))
                {
                    continue;
                }

                string hierarchy = HierarchyPath(label.transform);
                if (!hierarchy.Contains("배치 유령 · ") || !hierarchy.Contains("배치 판정/안내 문구"))
                {
                    continue;
                }

                Transform labelRoot = label.transform.parent;
                SpriteRenderer background = labelRoot == null
                    ? null
                    : labelRoot.GetComponentsInChildren<SpriteRenderer>(true)
                        .FirstOrDefault(renderer => renderer != null && renderer.gameObject.name == "안내 배경");
                bool visiblyActive = labelRoot != null && labelRoot.gameObject.activeInHierarchy &&
                                       background != null && background.enabled &&
                                       background.gameObject.activeInHierarchy && background.color.a > 0.01f;
                if (!visiblyActive)
                {
                    continue;
                }

                Rect bounds = ProjectBounds(camera, background.bounds);
                frame.Metrics.Add(new TextMetric
                {
                    Source = label,
                    Scenario = frame.Scenario,
                    Screenshot = frame.Screenshot,
                    Category = "placement-world-badge",
                    Hierarchy = hierarchy,
                    Value = string.IsNullOrWhiteSpace(label.text) ? "<empty visible badge>" : label.text,
                    Bounds = bounds,
                    BlockHeightPixels = bounds.height,
                    IsWorldText = true,
                    LineCount = 1,
                    VisibilityPass = false,
                    BoundsPass = bounds.xMin >= ScreenMarginPixels && bounds.yMin >= ScreenMarginPixels &&
                                 bounds.xMax <= Width - ScreenMarginPixels && bounds.yMax <= Height - ScreenMarginPixels,
                    HeightPass = true,
                    FontSizePass = true,
                    ContrastPass = true
                });
            }
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

        private static void EvaluateWorldGeometry(List<TextMetric> metrics, Camera camera)
        {
            foreach (TextMetric metric in metrics.Where(candidate =>
                         candidate.Category == "exploration-world" || candidate.Category == "placement-world-badge"))
            {
                TMP_Text source = metric.Source;
                Transform labelRoot = source == null ? null : source.transform.parent;
                SpriteRenderer background = labelRoot == null
                    ? null
                    : labelRoot.GetComponentsInChildren<SpriteRenderer>(false)
                        .FirstOrDefault(renderer => renderer != null && renderer.enabled && renderer.gameObject.name == "안내 배경");
                if (background == null)
                {
                    FailWorldGeometry(metric, metric.Category == "placement-world-badge"
                        ? "badge-background-unavailable"
                        : "panel-background-unavailable");
                }
                else
                {
                    metric.HasPanelBounds = true;
                    metric.PanelBounds = ProjectBounds(camera, background.bounds);
                    if (metric.HasPlayerRegion)
                    {
                        metric.PanelPlayerIntersectionPixels = IntersectionArea(metric.PanelBounds, metric.PlayerRegion);
                        if (metric.PanelPlayerIntersectionPixels > 0f)
                        {
                            FailWorldGeometry(metric, metric.Category == "placement-world-badge"
                                ? "badge-intersects-player"
                                : "panel-intersects-player");
                        }
                    }
                    else if (metric.Category == "exploration-world")
                    {
                        FailWorldGeometry(metric, "player-region-unavailable");
                    }
                }

                if (metric.Category != "exploration-world")
                {
                    continue;
                }

                Transform nodeRoot = labelRoot == null ? null : labelRoot.parent;
                while (nodeRoot != null && !nodeRoot.name.StartsWith("환경 수색 오브젝트 · ", StringComparison.Ordinal))
                {
                    nodeRoot = nodeRoot.parent;
                }

                Renderer[] nodeRenderers = nodeRoot == null
                    ? Array.Empty<Renderer>()
                    : nodeRoot.GetComponentsInChildren<Renderer>(false)
                        .Where(renderer => renderer != null && renderer.enabled &&
                                           (labelRoot == null || !renderer.transform.IsChildOf(labelRoot)))
                        .ToArray();
                if (nodeRenderers.Length == 0)
                {
                    FailWorldGeometry(metric, "node-visual-unavailable");
                    continue;
                }

                Bounds nodeBounds = nodeRenderers[0].bounds;
                for (int i = 1; i < nodeRenderers.Length; i += 1)
                {
                    nodeBounds.Encapsulate(nodeRenderers[i].bounds);
                }
                metric.HasNodeVisualBounds = true;
                metric.NodeVisualBounds = ProjectBounds(camera, nodeBounds);
                if (metric.HasPanelBounds)
                {
                    metric.PanelNodeIntersectionPixels = IntersectionArea(metric.PanelBounds, metric.NodeVisualBounds);
                    if (metric.PanelBounds.center.x >= metric.NodeVisualBounds.center.x)
                    {
                        FailWorldGeometry(metric, "panel-center-not-left-of-node-center");
                    }
                    if (metric.PanelNodeIntersectionPixels > 0f)
                    {
                        FailWorldGeometry(metric, "panel-intersects-node");
                    }
                }
            }
        }

        private static Rect ProjectBounds(Camera camera, Bounds bounds)
        {
            return ProjectWorldRect(camera, new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y));
        }

        private static float IntersectionArea(Rect left, Rect right)
        {
            Rect intersection = Intersect(left, right);
            return intersection.width <= 0f || intersection.height <= 0f
                ? 0f
                : intersection.width * intersection.height;
        }

        private static void FailWorldGeometry(TextMetric metric, string reason)
        {
            metric.WorldGeometryPass = false;
            if (!metric.WorldGeometryFailures.Contains(reason))
            {
                metric.WorldGeometryFailures.Add(reason);
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
            string hierarchy = HierarchyPath(text.transform);
            if (text is TextMeshPro && hierarchy.Contains("배치 유령 · ") &&
                hierarchy.Contains("배치 판정/안내 문구"))
            {
                return "placement-world-badge";
            }
            if (scenario.IndexOf("qps-long", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return text is TextMeshProUGUI && IsQpsContractText(scenario, hierarchy)
                    ? "pseudo-long"
                    : text is TextMeshProUGUI ? "ui-info" : "world-info";
            }

            if (scenario.IndexOf("placement", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (text is TextMeshProUGUI && text.name == "김씨 독백 또는 배치 상태")
                {
                    return "placement-status";
                }
            }

            if (text is TextMeshPro && hierarchy.Contains("환경 수색 오브젝트 · ") &&
                hierarchy.Contains("환경 수색 안내/안내 문구"))
            {
                return "exploration-world";
            }
            if (text is TextMeshProUGUI && hierarchy.Contains("환경 수색 발견물 compact tray placeholder/"))
            {
                return "search-tray";
            }
            return text is TextMeshProUGUI ? "ui-info" : "world-info";
        }

        private static bool IsQpsContractText(string scenario, string hierarchy)
        {
            bool controls = hierarchy.Contains("조작 안내/조작") || hierarchy.Contains("조작 안내/언어 설정/라벨");
            bool status = hierarchy.Contains("상태 HUD/날짜·상태") || hierarchy.Contains("상태 HUD/보유 자원");
            if (scenario.IndexOf("placement", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return controls || status || hierarchy.EndsWith("/김씨 독백 또는 배치 상태", StringComparison.Ordinal);
            }
            if (scenario.IndexOf("camp proximity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return controls || hierarchy.EndsWith("/김씨 독백 또는 배치 상태", StringComparison.Ordinal) ||
                       hierarchy.Contains("설비 근접 안내 · ");
            }
            if (scenario.IndexOf("camp popup", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return controls || status || hierarchy.Contains("설비 전용 소형 팝업/");
            }
            if (scenario.IndexOf("search tray", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return controls || status || hierarchy.Contains("가방 · icon.resource-tool-set/") ||
                       hierarchy.Contains("환경 수색 발견물 compact tray placeholder/");
            }
            return false;
        }

        private static float MinimumHeight(string category, string scenario, TMP_Text text)
        {
            if (category == "pseudo-long" &&
                scenario.IndexOf("camp proximity", StringComparison.OrdinalIgnoreCase) >= 0 &&
                HierarchyPath(text.transform).Contains("설비 근접 안내 · "))
            {
                return QpsProximityMinimumGlyphPixels;
            }
            switch (category)
            {
                case "placement-status": return PlacementStatusMinimumGlyphPixels;
                case "exploration-world": return ExplorationWorldMinimumGlyphPixels;
                case "search-tray": return SearchTrayMinimumGlyphPixels;
                case "pseudo-long": return PseudoLongMinimumGlyphPixels;
                default: return 0f;
            }
        }

        private static float RequiredFontSize(string category, string scenario, TMP_Text text)
        {
            if (category == "placement-status") return 26f;
            if (category == "exploration-world") return 28f;
            if (category == "search-tray") return SearchTrayRuntimeFontFloor(text);
            if (category != "pseudo-long") return 0f;

            string hierarchy = HierarchyPath(text.transform);
            bool searchTrayScenario = scenario.IndexOf("search tray", StringComparison.OrdinalIgnoreCase) >= 0;
            if (searchTrayScenario)
            {
                if (hierarchy.Contains("환경 수색 발견물 compact tray placeholder/")) return SearchTrayRuntimeFontFloor(text);
                if (hierarchy.Contains("상태 HUD/날짜·상태") || hierarchy.Contains("상태 HUD/보유 자원")) return 26f;
                if (hierarchy.Contains("조작 안내/조작") || hierarchy.Contains("조작 안내/언어 설정/라벨") ||
                    hierarchy.Contains("가방 · icon.resource-tool-set/")) return 18f;
            }
            if (hierarchy.Contains("설비 근접 안내 · ")) return 18f;
            if (hierarchy.EndsWith("/김씨 독백 또는 배치 상태", StringComparison.Ordinal))
            {
                return scenario.IndexOf("placement", StringComparison.OrdinalIgnoreCase) >= 0 ? 26f : 22f;
            }
            if (hierarchy.Contains("상태 HUD/날짜·상태") || hierarchy.Contains("상태 HUD/보유 자원")) return 28f;
            if (hierarchy.Contains("조작 안내/언어 설정/라벨")) return 30f;
            if (hierarchy.Contains("조작 안내/조작")) return 23f;
            if (hierarchy.Contains("설비 전용 소형 팝업/설비 팝업 제목")) return 14f;
            if (hierarchy.Contains("설비 전용 소형 팝업/설비 팝업 설명")) return 12f;
            if (hierarchy.Contains("설비 전용 소형 팝업/") && hierarchy.EndsWith("/라벨", StringComparison.Ordinal)) return 12f;
            return 0f;
        }

        private static float SearchTrayRuntimeFontFloor(TMP_Text text)
        {
            switch (text.name)
            {
                case "발견물 트레이 제목": return 18f;
                case "수색 비용·위험·잔량 상태": return 15f;
                case "현재 가방 요약": return 13f;
                default: return 12.5f;
            }
        }

        private static bool UsesConfiguredMinimumFont(string scenario, TMP_Text text)
        {
            string hierarchy = HierarchyPath(text.transform);
            return hierarchy.Contains("설비 근접 안내 · ") ||
                   hierarchy.EndsWith("/김씨 독백 또는 배치 상태", StringComparison.Ordinal) ||
                   hierarchy.Contains("상태 HUD/날짜·상태") ||
                   hierarchy.Contains("상태 HUD/보유 자원") ||
                   hierarchy.Contains("조작 안내/조작") ||
                   hierarchy.Contains("조작 안내/언어 설정/라벨") ||
                   (scenario.IndexOf("search tray", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    hierarchy.Contains("가방 · icon.resource-tool-set/")) ||
                   (scenario.IndexOf("camp popup", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    hierarchy.Contains("설비 전용 소형 팝업/"));
        }

        private static bool IsGatedCategory(string category)
        {
            return category == "placement-status" || category == "placement-world-badge" || category == "exploration-world" ||
                   category == "search-tray" || category == "pseudo-long";
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

        private static bool GroupPass(List<TextMetric> metrics, int expectedCount)
        {
            return metrics.Count == expectedCount && metrics.All(metric => metric.Passed);
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
