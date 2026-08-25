using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KimSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Independent resource-connection and visual regression gate. The gate
    /// observes actual Play-scene renderers and UI geometry; product assertions
    /// and static fixture strings are not accepted as connection evidence.
    /// </summary>
    public static class Wave19ResourceConnectionGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string PlayRunningKey = "ParallelQA.Wave19.PlayRunning";
        private const string PlayExitPassKey = "ParallelQA.Wave19.PlayExitPass";
        private const string PlayMessageKey = "ParallelQA.Wave19.PlayMessage";
        private const float CaptureWidth = 1280f;
        private const float CaptureHeight = 800f;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly AssetExpectation[] AssetExpectations =
        {
            new AssetExpectation("character.mr-kim.atlas", "cd48411681ba6264c9093f8b1cb1759a", "Assets/_Project/Art/Generated/sprite_animation/job_20260822085926_374033c5/exec-7c1f46d8-3b4f-4350-abc3-de6be9ebab6d.png", "kimAtlasSprite"),
            new AssetExpectation("resource.wood", "5ba05e4e569ab6745bff72d0b9ba9151", "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/wood.png", "woodIconSprite"),
            new AssetExpectation("resource.stone", "c881b7198e647ad40b32c63ce18e27a2", "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/stone.png", "stoneIconSprite"),
            new AssetExpectation("resource.food", "59695c50812722b458c210b4cfb02c12", "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/food.png", "foodIconSprite"),
            new AssetExpectation("resource.salvage", "cb8829a17d9cc9049aa16fbf4393097a", "Assets/_Project/Art/Generated/logo_icon/job_20260822141317_caf8e11d/scrap.png", "salvageIconSprite"),
            new AssetExpectation("structure.campfire", "aefba80bf6c588847ba3b66d017616cc", "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/campfire.png", "campfireSprite"),
            new AssetExpectation("structure.workbench", "f1920158f3a3ba14f8999ead8f869e30", "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/workbench.png", "workbenchSprite"),
            new AssetExpectation("structure.rain_collector", "0af1d92982f80ce4a89462c076305386", "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/rain_collector.png", "rainCollectorSprite"),
            new AssetExpectation("structure.rescue_signal", "00ff5be74a04dbe48a49fe2e1a673eec", "Assets/_Project/Art/Generated/separated_parts/job_20260822130400_6d786a69/rescue_signal.png", "rescueSignalSprite"),
            new AssetExpectation("ui.camp-contextual-interaction.compact-a", "070048b5b443d5d4a9c757c871873eb3", "Assets/_Project/Art/Generated/ui_set/job_20260823073121_f5da3402/compact-a.png", string.Empty),
            new AssetExpectation("ui.expedition-map.right-rail-a", "ae09637f2b24aa14295b1f9a5b4fde1c", "Assets/_Project/Art/Generated/ui_set/job_20260823150636_e3b39abc/candidate-a-right-rail-1280x800.png", "expeditionMapLayoutSprite"),
            new AssetExpectation("ui.ending-comic.triptych-a", "ba9091d85a3bddd4a8c8b90aa07d1b7c", "Assets/_Project/Art/Generated/ui_set/job_20260823160342_eceb3933/ending-comic-triptych-a-1280x800.png", string.Empty)
        };

        private static bool playTickAttached;
        private static double playEarliestRunTime;
        private static double playTimeoutAt;

        [Serializable]
        private sealed class Check
        {
            public string id;
            public string matrix;
            public string status;
            public string classification;
            public string severity;
            public string expected;
            public string actual;
            public string reproduction;
            public string recommendedFiles;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion = 1;
            public string title;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string overall;
            public string productOverall;
            public string infrastructureOverall;
            public int passed;
            public int productFailed;
            public int infrastructureFailed;
            public int unverified;
            public string physicalGamepad = "UNVERIFIED";
            public string steamReadiness = "NOT_READY";
            public Check[] checks;
        }

        [Serializable]
        private sealed class AssetEvidence
        {
            public string runId;
            public string baselineCommit;
            public AssetObservation[] assets;
        }

        [Serializable]
        private sealed class AssetObservation
        {
            public string stableId;
            public string expectedGuid;
            public string expectedPath;
            public string resolvedPath;
            public string sceneField;
            public string sceneGuid;
            public string result;
        }

        [Serializable]
        private sealed class PlayerStateObservation
        {
            public string state;
            public string spriteName;
            public string sourceGuid;
            public string sourcePath;
            public string rect;
            public string screenshot;
        }

        [Serializable]
        private sealed class ResourceObservation
        {
            public string kind;
            public int nodeCount;
            public string expectedGuid;
            public string[] observedGuids;
            public string[] rendererNames;
            public int fallbackCount;
            public string result;
        }

        [Serializable]
        private sealed class StructureObservation
        {
            public string kind;
            public string expectedGuid;
            public string observedGuid;
            public string rendererName;
            public int fallbackCount;
            public string result;
        }

        [Serializable]
        private sealed class PixelRectEvidence
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [Serializable]
        private sealed class LayoutObservation
        {
            public string surface;
            public string locale;
            public string screenshot;
            public PixelRectEvidence rect;
            public int activeTextCount;
            public int overflowCount;
            public int offscreenTextCount;
            public float screenCoverage;
            public float playerOcclusionRatio;
            public float walkingPathOcclusionRatio;
            public bool hiddenBefore;
            public bool hiddenAfter;
            public bool modal;
            public string sourceGuid;
            public string result;
            public string failureReason;
        }

        [Serializable]
        private sealed class PlayEvidence
        {
            public string runId;
            public string baselineCommit;
            public string scene;
            public string discoveryPolicy;
            public string prerequisiteResult;
            public PlayerStateObservation[] playerStates;
            public ResourceObservation[] resources;
            public StructureObservation[] structures;
            public LayoutObservation[] layouts;
            public string[] joystickNames;
        }

        private sealed class AssetExpectation
        {
            public readonly string StableId;
            public readonly string Guid;
            public readonly string Path;
            public readonly string SceneField;

            public AssetExpectation(string stableId, string guid, string path, string sceneField)
            {
                StableId = stableId;
                Guid = guid;
                Path = path;
                SceneField = sceneField;
            }
        }

        private struct PixelRect
        {
            public float X;
            public float Y;
            public float Width;
            public float Height;

            public float Area { get { return Mathf.Max(0f, Width) * Mathf.Max(0f, Height); } }
            public float Right { get { return X + Width; } }
            public float Top { get { return Y + Height; } }

            public PixelRectEvidence Evidence()
            {
                return new PixelRectEvidence { x = X, y = Y, width = Width, height = Height };
            }
        }

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string EvidenceFolder { get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); } }
        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave19" : Sanitize(value);
            }
        }

        private static string BaselineCommit
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_BASELINE");
                return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            }
        }

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<Check> checks = new List<Check>();
            List<AssetObservation> observations = new List<AssetObservation>();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                KimSurvivalPrototype prototype = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<KimSurvivalPrototype>(true)).FirstOrDefault();
                if (prototype == null) throw new InvalidOperationException("Prototype component is missing from the canonical scene.");
                SerializedObject serialized = new SerializedObject(prototype);

                foreach (AssetExpectation expected in AssetExpectations)
                {
                    string resolvedPath = AssetDatabase.GUIDToAssetPath(expected.Guid).Replace('\\', '/');
                    string sceneGuid = string.Empty;
                    if (!string.IsNullOrEmpty(expected.SceneField))
                    {
                        SerializedProperty property = serialized.FindProperty(expected.SceneField);
                        if (property != null && property.objectReferenceValue != null)
                        {
                            sceneGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(property.objectReferenceValue));
                        }
                    }

                    bool pathPass = string.Equals(resolvedPath, expected.Path, StringComparison.Ordinal);
                    bool scenePass = string.IsNullOrEmpty(expected.SceneField) || string.Equals(sceneGuid, expected.Guid, StringComparison.Ordinal);
                    observations.Add(new AssetObservation
                    {
                        stableId = expected.StableId,
                        expectedGuid = expected.Guid,
                        expectedPath = expected.Path,
                        resolvedPath = resolvedPath,
                        sceneField = expected.SceneField,
                        sceneGuid = sceneGuid,
                        result = pathPass && scenePass ? "PASS" : "FAIL"
                    });
                }

                Product(checks, "W19-E01.kim_adopted_atlas_guid", "Edit asset contract", "P0",
                    "The adopted Mr. Kim atlas GUID resolves to the packaged source and is serialized on the canonical scene",
                    delegate { return RequireAssetGroup(observations, "character.mr-kim.atlas"); },
                    "Open the canonical Scene and compare kimAtlasSprite with AssetDatabase.GUIDToAssetPath.",
                    "Assets/_Project/Scenes/KimSurvivalPrototype.unity; adopted Mr. Kim atlas .meta");
                Product(checks, "W19-E02.resource_icon_guids", "Edit asset contract", "P0",
                    "Wood, stone, food, and salvage use the four adopted icon GUIDs",
                    delegate { return RequireAssetPrefix(observations, "resource.", 4); },
                    "Inspect the four serialized icon fields and their .meta GUIDs.",
                    "Assets/_Project/Scenes/KimSurvivalPrototype.unity; adopted resource icon .meta files");
                Product(checks, "W19-E03.structure_sprite_guids", "Edit asset contract", "P0",
                    "Campfire, workbench, rain collector, and rescue signal use adopted separated structure GUIDs",
                    delegate { return RequireAssetPrefix(observations, "structure.", 4); },
                    "Inspect the four serialized structure fields and their .meta GUIDs.",
                    "Assets/_Project/Scenes/KimSurvivalPrototype.unity; adopted structure .meta files");
                Product(checks, "W19-E04.selected_ui_guid_contracts", "Edit asset contract", "P0",
                    "Compact A, expedition map A, and ending triptych A resolve only to their selected GUIDs",
                    delegate
                    {
                        RequireAssetGroup(observations, "ui.camp-contextual-interaction.compact-a");
                        RequireAssetGroup(observations, "ui.expedition-map.right-rail-a");
                        RequireAssetGroup(observations, "ui.ending-comic.triptych-a");
                        PrototypeCampPromptSkin prompt = Resources.Load<PrototypeCampPromptSkin>("Wave12CompactPromptSkin");
                        PrototypeWave18PresentationAssets presentation = Resources.Load<PrototypeWave18PresentationAssets>("Wave18PresentationAssets");
                        Require(prompt != null && SourceGuid(prompt.Frame) == ExpectedGuid("ui.camp-contextual-interaction.compact-a"), "compact A Resource skin GUID mismatch");
                        Require(presentation != null && SourceGuid(presentation.EndingComicFrame) == ExpectedGuid("ui.ending-comic.triptych-a"), "ending triptych Resource GUID mismatch");
                        return "compact-a/map-a/ending-triptych-a GUIDs resolve and Resources assets reference the selected sources";
                    },
                    "Load both Resources assets and compare object source GUIDs with the selected-only contracts.",
                    "Assets/_Project/Scripts/Localization/Resources/Wave12CompactPromptSkin.asset; Assets/_Project/Settings/Resources/Wave18PresentationAssets.asset");
            }
            catch (Exception exception)
            {
                Infrastructure(checks, "W19-I01.edit_runner", "Edit infrastructure", "P0",
                    "Canonical Scene and GUID contracts can be inspected", exception.ToString(),
                    "Run the Wave 19 entry point outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave19ResourceConnectionGateRunner.cs");
            }

            WriteJson("wave19-asset-guid-evidence.json", new AssetEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                assets = observations.ToArray()
            });
            WriteReport("wave19-edit-contracts", "Wave 19 resource connection Edit contracts", started, checks);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "Wave 19 Play runner did not complete");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AttachPlayCallbacks();
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void ResumePlayContracts()
        {
            if (SessionState.GetBool(PlayRunningKey, false)) AttachPlayCallbacks();
        }

        private static void AttachPlayCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (!EditorApplication.isPlaying) return;
            playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
            playTimeoutAt = EditorApplication.timeSinceStartup + 180d;
            if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playEarliestRunTime = EditorApplication.timeSinceStartup + 1.5d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 180d;
                if (!playTickAttached) { EditorApplication.update += PlayTick; playTickAttached = true; }
            }
            else if (state == PlayModeStateChange.EnteredEditMode) FinishPlayContracts();
        }

        private static void PlayTick()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup > playTimeoutAt)
            {
                WritePlayInfrastructureFailure(new TimeoutException("Wave 19 Play fixture timed out."));
                StopPlayContracts();
                return;
            }
            if (EditorApplication.timeSinceStartup < playEarliestRunTime) return;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }

            try
            {
                DateTime started = DateTime.UtcNow;
                KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
                if (prototype == null) throw new InvalidOperationException("No live KimSurvivalPrototype exists in the Play scene.");
                List<Check> checks = new List<Check>();
                List<PlayerStateObservation> playerStates = new List<PlayerStateObservation>();
                List<ResourceObservation> resources = new List<ResourceObservation>();
                List<StructureObservation> structures = new List<StructureObservation>();
                List<LayoutObservation> layouts = new List<LayoutObservation>();
                string prerequisiteResult = string.Empty;

                Product(checks, "W19-R01.current_green_play_regression", "existing GREEN regression", "P0",
                    "The canonical product Play verification still completes before resource-specific inspection",
                    delegate
                    {
                        prerequisiteResult = prototype.RunAutomatedVerification(
                            EvidencePath("wave19-prerequisite-exploration-1280x800.png"),
                            EvidencePath("wave19-prerequisite-swimming-1280x800.png"),
                            EvidencePath("wave19-prerequisite-placement-ko-1280x800.png"),
                            EvidencePath("wave19-prerequisite-placement-en-1280x800.png"),
                            EvidencePath("wave19-prerequisite-signal-ko-1280x800.png"),
                            EvidencePath("wave19-prerequisite-signal-en-1280x800.png"),
                            EvidencePath("wave19-prerequisite-bag-locked-ko-1280x800.png"),
                            EvidencePath("wave19-prerequisite-bag-upgraded-en-1280x800.png"),
                            EvidencePath("wave19-prerequisite-bag-locked-ko-1920x1080.png"),
                            EvidencePath("wave19-prerequisite-bag-upgraded-en-1920x1080.png"),
                            EvidencePath("wave19-prerequisite-camp-far-ko-1280x800.png"),
                            EvidencePath("wave19-prerequisite-camp-near-ko-1280x800.png"),
                            EvidencePath("wave19-prerequisite-camp-workbench-en-1280x800.png"),
                            EvidencePath("wave19-prerequisite-camp-campfire-ko-1280x800.png"));
                        return "product Play verification completed; responseLength=" + (prerequisiteResult ?? string.Empty).Length;
                    },
                    "Run the canonical product verification in a fresh Play scene, then inspect the actual objects.",
                    "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs");

                Product(checks, "W19-P03.camp_structures_adopted_sprites", "actual Play structures", "P0",
                    "Campfire, workbench, rain collector, and rescue signal render adopted sprites without structure fallbacks",
                    delegate { return ObserveStructures(prototype, structures); },
                    "After the full Play setup, inspect structureViews and rescueSignalRenderer on the active Scene instance.",
                    "runtime structure connection owner selected from the active Scene");

                Product(checks, "W19-P01.kim_atlas_idle_move_water", "actual Play player", "P0",
                    "The actual Mr. Kim SpriteRenderer uses the adopted atlas and visibly switches idle, move, and water cells",
                    delegate { return ObservePlayerStates(prototype, playerStates); },
                    "Enter a fresh exploration state and apply idle/move/swimming presentation states to the live renderer.",
                    "runtime player presentation owner selected from the active Scene");

                Product(checks, "W19-P02.resource_nodes_adopted_icons", "actual Play resources", "P0",
                    "Every wood, stone, food, and salvage node renders the matching adopted icon and no geometric resource fallback exists",
                    delegate { return ObserveResourceNodes(prototype, resources); },
                    "Inspect every live/inactive node root created by the exploration scene and its icon renderer.",
                    "runtime resource node owner selected from the active Scene");

                string[] locales = { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode, PrototypeLocalization.QpsLongLocaleCode };
                for (int index = 0; index < locales.Length; index += 1)
                {
                    string locale = locales[index];
                    AddLayoutChecks(prototype, locale, layouts, checks);
                }

                Product(checks, "W19-P04.selected_ui_sources_in_play", "actual Play selected UI", "P0",
                    "Prompt compact A, map A, and ending triptych A are observed on active Play UI Images",
                    delegate
                    {
                        string prompt = layouts.Where(value => value.surface == "camp-prompt").Select(value => value.sourceGuid).FirstOrDefault(value => !string.IsNullOrEmpty(value));
                        string map = layouts.Where(value => value.surface == "expedition-map-a").Select(value => value.sourceGuid).FirstOrDefault(value => !string.IsNullOrEmpty(value));
                        string ending = layouts.Where(value => value.surface == "ending-comic").Select(value => value.sourceGuid).FirstOrDefault(value => !string.IsNullOrEmpty(value));
                        Require(prompt == ExpectedGuid("ui.camp-contextual-interaction.compact-a"), "Play prompt source GUID mismatch: " + prompt);
                        Require(map == ExpectedGuid("ui.expedition-map.right-rail-a"), "Play map source GUID mismatch: " + map);
                        Require(ending == ExpectedGuid("ui.ending-comic.triptych-a"), "Play ending source GUID mismatch: " + ending);
                        return "prompt=" + prompt + "; map=" + map + "; ending=" + ending;
                    },
                    "Read active Play Image.sprite source GUIDs after each surface is shown.",
                    "actual Play UI Images selected by stable surface hierarchy");

                Unverified(checks, "W19-U01.physical_gamepad", "manual hardware", "P1",
                    "A human actuates a physical gamepad through the complete resource/UI path",
                    "Unity joystick names: " + string.Join(" | ", Input.GetJoystickNames() ?? Array.Empty<string>()),
                    "Repeat on a Windows machine with a physical controller and retain human evidence.",
                    "manual playtest evidence");
                NotReady(checks, "W19-U02.steam_release", "external release", "P0",
                    "Steamworks App ID, depot, Input, Cloud, achievements, and partner permissions have approved evidence",
                    "No Steam partner evidence is in Wave 19 scope.",
                    "Complete the separately approved Steam release workflow.",
                    "external Steam partner configuration");

                WriteJson("wave19-play-observation-evidence.json", new PlayEvidence
                {
                    runId = RunId,
                    baselineCommit = BaselineCommit,
                    scene = ScenePath,
                    discoveryPolicy = "Actual active Play-scene SpriteRenderer/Image/TMP/RectTransform observations. Product assertion strings alone cannot satisfy P01-P04.",
                    prerequisiteResult = prerequisiteResult,
                    playerStates = playerStates.ToArray(),
                    resources = resources.ToArray(),
                    structures = structures.ToArray(),
                    layouts = layouts.ToArray(),
                    joystickNames = Input.GetJoystickNames() ?? Array.Empty<string>()
                });
                Report report = WriteReport("wave19-play-contracts", "Wave 19 actual Play resource and visual contracts", started, checks);
                SessionState.SetBool(PlayExitPassKey, report.infrastructureOverall == "PASS");
                SessionState.SetString(PlayMessageKey, report.overall + " · Wave 19 Play evidence completed");
            }
            catch (Exception exception)
            {
                WritePlayInfrastructureFailure(exception);
            }
            StopPlayContracts();
        }

        private static string ObservePlayerStates(KimSurvivalPrototype prototype, List<PlayerStateObservation> output)
        {
            EnsureEndingComicHidden();
            GameSession session = prototype.Session;
            session.Reset();
            Require(session.BeginSearch(PrototypeExpeditionRegionId.Beach), "fresh exploration did not start");
            InvokePrivate(prototype, "RefreshAll");
            PrototypePlayerPresentation presentation = GetPrivateField<PrototypePlayerPresentation>(prototype, "playerPresentation");
            SpriteRenderer renderer = GetPrivateField<SpriteRenderer>(presentation, "stateRenderer");
            Require(renderer != null, "actual Mr. Kim state renderer is missing");

            PrototypePlayerPresentationState[] states =
            {
                new PrototypePlayerPresentationState(-1.5f, PrototypePlayerTraversal.LandY, 1f, 0f, false, true),
                new PrototypePlayerPresentationState(-0.5f, PrototypePlayerTraversal.LandY, 1f, 1f, false, true),
                new PrototypePlayerPresentationState(-7.5f, PrototypePlayerTraversal.WaterY, 1f, 1f, true, false)
            };
            string[] names = { "idle", "move", "water" };
            HashSet<Sprite> observed = new HashSet<Sprite>();
            for (int index = 0; index < states.Length; index += 1)
            {
                presentation.Apply(states[index]);
                Sprite sprite = renderer.sprite;
                Require(sprite != null, names[index] + " sprite is missing");
                string guid = SourceGuid(sprite);
                Require(guid == ExpectedGuid("character.mr-kim.atlas"), names[index] + " source GUID mismatch: " + guid);
                observed.Add(sprite);
                string screenshot = "wave19-kim-" + names[index] + "-ko-1280x800.png";
                prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, screenshot), 1280, 800);
                output.Add(new PlayerStateObservation
                {
                    state = names[index],
                    spriteName = sprite.name,
                    sourceGuid = guid,
                    sourcePath = SourcePath(sprite),
                    rect = sprite.rect.ToString(),
                    screenshot = screenshot
                });
            }
            Require(observed.Count == 3, "idle/move/water did not select three distinct sprites");
            return string.Join(" | ", output.Select(value => value.state + "=" + value.spriteName + "@" + value.sourceGuid).ToArray());
        }

        private static string ObserveResourceNodes(KimSurvivalPrototype prototype, List<ResourceObservation> output)
        {
            EnsureEndingComicHidden();
            object nodesObject = GetField(prototype, "nodes");
            IEnumerable nodes = nodesObject as IEnumerable;
            Require(nodes != null, "runtime node collection is unavailable");
            Dictionary<string, List<object>> byKind = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
            foreach (object node in nodes)
            {
                string kind = Convert.ToString(GetField(node, "Kind"));
                if (!byKind.ContainsKey(kind)) byKind.Add(kind, new List<object>());
                byKind[kind].Add(node);
            }

            string[] kinds = { "Wood", "Stone", "Food", "Salvage" };
            foreach (string kind in kinds)
            {
                string expectedStableId = "resource." + kind.ToLowerInvariant();
                string expectedGuid = ExpectedGuid(expectedStableId);
                List<object> matching;
                Require(byKind.TryGetValue(kind, out matching) && matching.Count > 0, "no " + kind + " node exists");
                List<string> guids = new List<string>();
                List<string> rendererNames = new List<string>();
                int fallbackCount = 0;
                foreach (object node in matching)
                {
                    GameObject root = GetField(node, "Root") as GameObject;
                    Require(root != null, kind + " node root is missing");
                    fallbackCount += root.GetComponentsInChildren<Transform>(true)
                        .Count(value => value.name.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0 || value.name.Contains("자원 placeholder"));
                    SpriteRenderer renderer = root.GetComponentsInChildren<SpriteRenderer>(true)
                        .FirstOrDefault(value => SourceGuid(value.sprite) == expectedGuid);
                    Require(renderer != null, kind + " node has no renderer using " + expectedGuid);
                    guids.Add(SourceGuid(renderer.sprite));
                    rendererNames.Add(renderer.gameObject.name);
                }
                bool passed = fallbackCount == 0 && guids.All(value => value == expectedGuid);
                output.Add(new ResourceObservation
                {
                    kind = kind,
                    nodeCount = matching.Count,
                    expectedGuid = expectedGuid,
                    observedGuids = guids.ToArray(),
                    rendererNames = rendererNames.ToArray(),
                    fallbackCount = fallbackCount,
                    result = passed ? "PASS" : "FAIL"
                });
                Require(passed, kind + " fallback/GUID contract failed");
            }
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, "wave19-resource-nodes-ko-1280x800.png"), 1280, 800);
            return string.Join(" | ", output.Select(value => value.kind + "=" + value.nodeCount + "@" + value.expectedGuid + "/fallback=" + value.fallbackCount).ToArray());
        }

        private static string ObserveStructures(KimSurvivalPrototype prototype, List<StructureObservation> output)
        {
            GameSession session = prototype.Session;
            PrototypeCampPlacement placement = GetPrivateField<PrototypeCampPlacement>(prototype, "campPlacement");
            session.Reset();
            placement.Reset();
            GetPrivateField<PrototypeCampUse>(prototype, "campUse").Reset();
            GetPrivateField<PrototypeCampInteraction>(prototype, "campInteraction").Reset();
            session.Grant(ResourceKind.Wood, 20);
            session.Grant(ResourceKind.Stone, 20);
            session.Grant(ResourceKind.Salvage, 20);
            InvokePrivate(prototype, "RefreshAll");
            StructureKind[] setupKinds = { StructureKind.Campfire, StructureKind.Workbench, StructureKind.RainCollector };
            float[] setupPositions = { -1.5f, 1.5f, 3.5f };
            for (int index = 0; index < setupKinds.Length; index += 1)
            {
                InvokePrivate(prototype, "BeginCampPlacement", setupKinds[index]);
                Require(placement.IsActive && placement.SelectedKind == setupKinds[index], "visual fixture did not enter " + setupKinds[index] + " placement");
                placement.SetCandidateX(setupPositions[index]);
                InvokePrivate(prototype, "UpdatePlacementGhost");
                Require(placement.CurrentValidity == CampPlacementValidity.Valid, setupKinds[index] + " visual fixture candidate is invalid");
                Require(InvokePrivateResult<bool>(prototype, "ConfirmCampPlacement"), setupKinds[index] + " visual fixture placement failed");
            }

            IDictionary views = GetField(prototype, "structureViews") as IDictionary;
            Require(views != null, "structureViews is unavailable");
            Dictionary<string, string> expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Campfire", ExpectedGuid("structure.campfire") },
                { "Workbench", ExpectedGuid("structure.workbench") },
                { "RainCollector", ExpectedGuid("structure.rain_collector") }
            };
            foreach (DictionaryEntry entry in views)
            {
                string kind = Convert.ToString(entry.Key);
                if (!expected.ContainsKey(kind)) continue;
                GameObject root = entry.Value as GameObject;
                Require(root != null, kind + " structure root is missing");
                int fallbacks = root.GetComponentsInChildren<Transform>(true).Count(value => value.name.Contains("설비 아트 누락"));
                SpriteRenderer renderer = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(value => SourceGuid(value.sprite) == expected[kind]);
                output.Add(new StructureObservation
                {
                    kind = kind,
                    expectedGuid = expected[kind],
                    observedGuid = renderer == null ? string.Empty : SourceGuid(renderer.sprite),
                    rendererName = renderer == null ? string.Empty : renderer.gameObject.name,
                    fallbackCount = fallbacks,
                    result = renderer != null && fallbacks == 0 ? "PASS" : "FAIL"
                });
            }

            SpriteRenderer signal = GetPrivateField<SpriteRenderer>(prototype, "rescueSignalRenderer");
            string signalGuid = signal == null ? string.Empty : SourceGuid(signal.sprite);
            int signalFallbacks = signal == null ? 1 : signal.transform.root.GetComponentsInChildren<Transform>(true).Count(value => value.name.Contains("구조 신호대 아트 누락"));
            output.Add(new StructureObservation
            {
                kind = "RescueSignal",
                expectedGuid = ExpectedGuid("structure.rescue_signal"),
                observedGuid = signalGuid,
                rendererName = signal == null ? string.Empty : signal.gameObject.name,
                fallbackCount = signalFallbacks,
                result = signalGuid == ExpectedGuid("structure.rescue_signal") && signalFallbacks == 0 ? "PASS" : "FAIL"
            });
            Require(expected.Keys.All(kind => output.Any(value => value.kind == kind && value.result == "PASS")), "one or more installed structures are missing adopted renderers");
            Require(output.Any(value => value.kind == "RescueSignal" && value.result == "PASS"), "rescue signal adopted renderer is missing");
            return string.Join(" | ", output.Select(value => value.kind + "=" + value.observedGuid + "/fallback=" + value.fallbackCount).ToArray());
        }

        private static void AddLayoutChecks(KimSurvivalPrototype prototype, string locale, List<LayoutObservation> output, List<Check> checks)
        {
            string suffix = locale.Replace("-", "_");
            Product(checks, "W19-V-" + suffix + "-01.camp_prompt", "1280x800 " + locale, "P1",
                "Compact prompt remains onscreen with overflow 0 and no player/walking-path occlusion",
                delegate { return CaptureCampSurface(prototype, locale, false, output); },
                "Approach the campfire at 1280x800 and inspect the active prompt Rect/TMP plus 1:1 capture.",
                "camp contextual presentation owner selected from active Play UI");
            Product(checks, "W19-V-" + suffix + "-02.camp_popup", "1280x800 " + locale, "P1",
                "Context popup remains onscreen with overflow 0, bounded coverage, and prompt hidden while open",
                delegate { return CaptureCampSurface(prototype, locale, true, output); },
                "Interact with the approached campfire and inspect the small popup at 1280x800.",
                "camp contextual presentation owner selected from active Play UI");
            Product(checks, "W19-V-" + suffix + "-03.expedition_map_a", "1280x800 " + locale, "P1",
                "Expedition map A stays within safe bounds, overflow 0, and is absent before/after its modal interaction",
                delegate { return CaptureMapSurface(prototype, locale, output); },
                "Approach the expedition-map target, interact, capture, cancel, and compare its actual Rect.",
                "expedition map presentation owner selected from active Play UI");
            Product(checks, "W19-V-" + suffix + "-04.ending_comic", "1280x800 " + locale, "P1",
                "Ending triptych A stays within safe bounds, overflow 0, bounded modal coverage, and hides after close",
                delegate { return CaptureEndingSurface(prototype, locale, output); },
                "Show a deterministic ending on the actual Play runtime, capture, then close it.",
                "ending presentation owner selected from active Play UI");
        }

        private static string CaptureCampSurface(KimSurvivalPrototype prototype, string locale, bool popup, List<LayoutObservation> output)
        {
            PrepareCampTarget(prototype, locale, PrototypeCampInteractionTargetKind.Campfire);
            GameObject prompt = GetPrivateField<GameObject>(prototype, "campProximityPrompt");
            GameObject popupObject = GetPrivateField<GameObject>(prototype, "campInteractionPopup");
            bool hiddenBefore = popup ? !popupObject.activeSelf : true;
            if (popup)
            {
                InvokePrivate(prototype, "UseNearestCampTarget");
                Require(popupObject.activeSelf && !prompt.activeSelf, "popup did not open exclusively");
            }
            else
            {
                Require(prompt.activeSelf && !popupObject.activeSelf, "prompt did not appear exclusively");
            }

            GameObject target = popup ? popupObject : prompt;
            string file = "wave19-" + (popup ? "camp-popup" : "camp-prompt") + "-" + locale + "-1280x800.png";
            LayoutObservation observation = MeasureLayout(prototype, popup ? "camp-popup" : "camp-prompt", locale, file, target, false,
                popup ? 0.26f : 0.025f, popup ? 0.45f : 0.01f, popup ? 0.05f : 0.01f, hiddenBefore);
            if (!popup)
            {
                Image frame = target.GetComponent<Image>();
                observation.sourceGuid = frame == null ? string.Empty : SourceGuid(frame.sprite);
                Require(observation.sourceGuid == ExpectedGuid("ui.camp-contextual-interaction.compact-a"), "compact A prompt GUID mismatch");
            }
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, file), 1280, 800);

            if (popup)
            {
                InvokePrivate(prototype, "CancelCampPopup");
                observation.hiddenAfter = !popupObject.activeSelf && prompt.activeSelf;
                Require(observation.hiddenAfter, "popup close did not restore the same nearby prompt");
            }
            else
            {
                observation.hiddenAfter = true;
            }
            FinishLayout(observation);
            output.Add(observation);
            Require(observation.result == "PASS", observation.failureReason);
            return DescribeLayout(observation);
        }

        private static string CaptureMapSurface(KimSurvivalPrototype prototype, string locale, List<LayoutObservation> output)
        {
            PrepareCampTarget(prototype, locale, PrototypeCampInteractionTargetKind.ExpeditionMap);
            GameObject map = GetPrivateField<GameObject>(prototype, "expeditionMapPanel");
            bool hiddenBefore = !map.activeSelf;
            InvokePrivate(prototype, "UseNearestCampTarget");
            Require(map.activeSelf, "map did not open after interaction");
            string file = "wave19-expedition-map-a-" + locale + "-1280x800.png";
            LayoutObservation observation = MeasureLayout(prototype, "expedition-map-a", locale, file, map, true, 0.91f, 1f, 1f, hiddenBefore);
            Image frame = GetPrivateField<Image>(prototype, "expeditionMapFrameImage");
            observation.sourceGuid = frame == null ? string.Empty : SourceGuid(frame.sprite);
            Require(observation.sourceGuid == ExpectedGuid("ui.expedition-map.right-rail-a"), "map A GUID mismatch");
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, file), 1280, 800);
            InvokePrivate(prototype, "CancelCampPopup");
            observation.hiddenAfter = !map.activeSelf;
            FinishLayout(observation);
            output.Add(observation);
            Require(observation.result == "PASS", observation.failureReason);
            return DescribeLayout(observation);
        }

        private static string CaptureEndingSurface(KimSurvivalPrototype prototype, string locale, List<LayoutObservation> output)
        {
            SetLocale(GetPrivateField<PrototypeLocalization>(prototype, "localization"), locale);
            MonoBehaviour runtime = FindEndingRuntime();
            Require(runtime != null, "live ending runtime was not discovered");
            InvokePublic(runtime, "DeactivateComic");
            GameObject root = GetField(runtime, "endingComicRoot") as GameObject;
            bool hiddenBefore = root != null && !root.activeSelf;
            InvokePublic(runtime, "ShowEndingForVerification", "ending.stay.just-kim");
            root = GetField(runtime, "endingComicRoot") as GameObject;
            Require(root != null && root.activeSelf, "ending comic did not activate");
            Transform frameTransform = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(value => value.name == "Finale Surface");
            Require(frameTransform != null, "Finale Surface is missing");
            string file = "wave19-ending-comic-" + locale + "-1280x800.png";
            LayoutObservation observation = MeasureLayout(prototype, "ending-comic", locale, file, frameTransform.gameObject, true, 0.86f, 1f, 1f, hiddenBefore);
            Image frame = frameTransform.GetComponent<Image>();
            observation.sourceGuid = frame == null ? string.Empty : SourceGuid(frame.sprite);
            Require(observation.sourceGuid == ExpectedGuid("ui.ending-comic.triptych-a"), "ending triptych A GUID mismatch");
            prototype.CaptureVerificationPng(Path.Combine(EvidenceFolder, file), 1280, 800);
            InvokePublic(runtime, "DeactivateComic");
            observation.hiddenAfter = !root.activeSelf;
            FinishLayout(observation);
            output.Add(observation);
            Require(observation.result == "PASS", observation.failureReason);
            return DescribeLayout(observation);
        }

        private static void PrepareCampTarget(KimSurvivalPrototype prototype, string locale, PrototypeCampInteractionTargetKind targetKind)
        {
            EnsureEndingComicHidden();
            GameSession session = prototype.Session;
            session.Reset();
            GetPrivateField<PrototypeCampPlacement>(prototype, "campPlacement").Reset();
            GetPrivateField<PrototypeCampUse>(prototype, "campUse").Reset();
            GetPrivateField<PrototypeCampInteraction>(prototype, "campInteraction").Reset();
            SetLocale(GetPrivateField<PrototypeLocalization>(prototype, "localization"), locale);
            InvokePrivate(prototype, "RefreshAll");
            Vector2 target = InvokePrivateResult<Vector2>(prototype, "GetCampInteractionTargetPosition", targetKind);
            GetPrivateField<PrototypeCampUse>(prototype, "campUse").Warp(target);
            InvokePrivate(prototype, "RefreshAll");
        }

        private static MonoBehaviour FindEndingRuntime()
        {
            return UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .FirstOrDefault(value => value != null && value.GetType().GetMethod(
                    "ShowEndingForVerification", BindingFlags.Instance | BindingFlags.Public) != null);
        }

        private static void EnsureEndingComicHidden()
        {
            MonoBehaviour runtime = FindEndingRuntime();
            Require(runtime != null, "live ending runtime was not discovered before world/UI capture isolation");
            InvokePublic(runtime, "DeactivateComic");
            GameObject root = GetField(runtime, "endingComicRoot") as GameObject;
            Require(root == null || !root.activeSelf, "ending comic overlay remained active before a non-ending capture");
        }

        private static LayoutObservation MeasureLayout(KimSurvivalPrototype prototype, string surface, string locale, string screenshot,
            GameObject target, bool modal, float maximumCoverage, float maximumWalkingOcclusion, float maximumPlayerOcclusion, bool hiddenBefore)
        {
            Camera camera = GetPrivateField<Camera>(prototype, "worldCamera");
            Canvas.ForceUpdateCanvases();
            TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true).Where(value => value.gameObject.activeInHierarchy).ToArray();
            int overflow = 0;
            int offscreenText = 0;
            foreach (TMP_Text text in texts)
            {
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing) overflow += 1;
                PixelRect textRect = RectTransformPixels(text.rectTransform, camera);
                if (!InsideScreen(textRect, 1f)) offscreenText += 1;
            }
            Canvas.ForceUpdateCanvases();
            RectTransform rectTransform = target.GetComponent<RectTransform>();
            Require(rectTransform != null, surface + " has no RectTransform");
            PixelRect rect = RectTransformPixels(rectTransform, camera);
            PixelRect walking = WalkingBand(camera);
            PixelRect player = PlayerRect(camera);
            float coverage = rect.Area / (CaptureWidth * CaptureHeight);
            float walkingRatio = IntersectionArea(rect, walking) / Mathf.Max(1f, walking.Area);
            float playerRatio = IntersectionArea(rect, player) / Mathf.Max(1f, player.Area);
            List<string> failures = new List<string>();
            if (!InsideScreen(rect, 1f)) failures.Add("surface offscreen");
            if (texts.Length == 0) failures.Add("no active TMP text");
            if (overflow != 0) failures.Add("TMP overflow=" + overflow);
            if (offscreenText != 0) failures.Add("offscreen TMP=" + offscreenText);
            if (coverage > maximumCoverage + 0.001f) failures.Add("coverage=" + coverage.ToString("0.000") + ">" + maximumCoverage.ToString("0.000"));
            if (walkingRatio > maximumWalkingOcclusion + 0.001f) failures.Add("walkingOcclusion=" + walkingRatio.ToString("0.000"));
            if (playerRatio > maximumPlayerOcclusion + 0.001f) failures.Add("playerOcclusion=" + playerRatio.ToString("0.000"));
            if (!hiddenBefore) failures.Add("surface was visible before its interaction/state");
            return new LayoutObservation
            {
                surface = surface,
                locale = locale,
                screenshot = screenshot,
                rect = rect.Evidence(),
                activeTextCount = texts.Length,
                overflowCount = overflow,
                offscreenTextCount = offscreenText,
                screenCoverage = coverage,
                playerOcclusionRatio = playerRatio,
                walkingPathOcclusionRatio = walkingRatio,
                hiddenBefore = hiddenBefore,
                hiddenAfter = false,
                modal = modal,
                result = failures.Count == 0 ? "PASS" : "FAIL",
                failureReason = string.Join("; ", failures)
            };
        }

        private static void FinishLayout(LayoutObservation observation)
        {
            List<string> failures = string.IsNullOrEmpty(observation.failureReason)
                ? new List<string>()
                : observation.failureReason.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!observation.hiddenAfter) failures.Add("surface did not hide/restore after close");
            observation.failureReason = string.Join("; ", failures);
            observation.result = failures.Count == 0 ? "PASS" : "FAIL";
        }

        private static PixelRect RectTransformPixels(RectTransform rect, Camera camera)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 first = camera.WorldToViewportPoint(corners[0]);
            float minX = first.x * CaptureWidth;
            float maxX = minX;
            float minY = first.y * CaptureHeight;
            float maxY = minY;
            for (int index = 1; index < corners.Length; index += 1)
            {
                Vector3 viewport = camera.WorldToViewportPoint(corners[index]);
                minX = Mathf.Min(minX, viewport.x * CaptureWidth);
                maxX = Mathf.Max(maxX, viewport.x * CaptureWidth);
                minY = Mathf.Min(minY, viewport.y * CaptureHeight);
                maxY = Mathf.Max(maxY, viewport.y * CaptureHeight);
            }
            return new PixelRect { X = minX, Y = minY, Width = maxX - minX, Height = maxY - minY };
        }

        private static PixelRect WalkingBand(Camera camera)
        {
            Vector3 low = camera.WorldToViewportPoint(new Vector3(camera.transform.position.x, PrototypeCampUse.PlayerFloorY - 0.25f, 0f));
            Vector3 high = camera.WorldToViewportPoint(new Vector3(camera.transform.position.x, PrototypeCampUse.PlayerFloorY + 0.75f, 0f));
            float y = Mathf.Min(low.y, high.y) * CaptureHeight;
            return new PixelRect { X = 0f, Y = y, Width = CaptureWidth, Height = Mathf.Abs(high.y - low.y) * CaptureHeight };
        }

        private static PixelRect PlayerRect(Camera camera)
        {
            SpriteRenderer renderer = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude)
                .FirstOrDefault(value => SourceGuid(value.sprite) == ExpectedGuid("character.mr-kim.atlas"));
            if (renderer == null) return new PixelRect();
            Bounds bounds = renderer.bounds;
            Vector3 min = camera.WorldToViewportPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.center.z));
            Vector3 max = camera.WorldToViewportPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.center.z));
            return new PixelRect
            {
                X = Mathf.Min(min.x, max.x) * CaptureWidth,
                Y = Mathf.Min(min.y, max.y) * CaptureHeight,
                Width = Mathf.Abs(max.x - min.x) * CaptureWidth,
                Height = Mathf.Abs(max.y - min.y) * CaptureHeight
            };
        }

        private static float IntersectionArea(PixelRect first, PixelRect second)
        {
            float width = Mathf.Max(0f, Mathf.Min(first.Right, second.Right) - Mathf.Max(first.X, second.X));
            float height = Mathf.Max(0f, Mathf.Min(first.Top, second.Top) - Mathf.Max(first.Y, second.Y));
            return width * height;
        }

        private static bool InsideScreen(PixelRect rect, float tolerance)
        {
            return rect.Width > 0f && rect.Height > 0f && rect.X >= -tolerance && rect.Y >= -tolerance &&
                   rect.Right <= CaptureWidth + tolerance && rect.Top <= CaptureHeight + tolerance;
        }

        private static string DescribeLayout(LayoutObservation value)
        {
            return value.surface + "/" + value.locale + " rect=" + value.rect.x.ToString("0.0") + "," + value.rect.y.ToString("0.0") + "," +
                   value.rect.width.ToString("0.0") + "x" + value.rect.height.ToString("0.0") + "; coverage=" + value.screenCoverage.ToString("0.000") +
                   "; overflow=" + value.overflowCount + "; offscreenText=" + value.offscreenTextCount +
                   "; player=" + value.playerOcclusionRatio.ToString("0.000") + "; walking=" + value.walkingPathOcclusionRatio.ToString("0.000") +
                   "; hiddenBefore/After=" + value.hiddenBefore + "/" + value.hiddenAfter;
        }

        private static void SetLocale(PrototypeLocalization localization, string locale)
        {
            bool changed = locale == PrototypeLocalization.QpsLongLocaleCode
                ? localization.SetQaLocale()
                : localization.SetLocale(locale, false);
            Require(changed && localization.CurrentLocaleCode == locale, "locale did not activate: " + locale);
        }

        private static string RequireAssetGroup(IEnumerable<AssetObservation> observations, string stableId)
        {
            AssetObservation observation = observations.FirstOrDefault(value => value.stableId == stableId);
            Require(observation != null && observation.result == "PASS", stableId + " asset contract failed");
            return stableId + "=" + observation.resolvedPath + "@" + observation.expectedGuid +
                   (string.IsNullOrEmpty(observation.sceneField) ? string.Empty : "; scene=" + observation.sceneGuid);
        }

        private static string RequireAssetPrefix(IEnumerable<AssetObservation> observations, string prefix, int expectedCount)
        {
            AssetObservation[] matching = observations.Where(value => value.stableId.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            Require(matching.Length == expectedCount && matching.All(value => value.result == "PASS"), prefix + " expected " + expectedCount + " PASS entries");
            return string.Join(" | ", matching.Select(value => value.stableId + "@" + value.expectedGuid).ToArray());
        }

        private static string ExpectedGuid(string stableId)
        {
            AssetExpectation value = AssetExpectations.FirstOrDefault(item => item.StableId == stableId);
            return value == null ? string.Empty : value.Guid;
        }

        private static string SourceGuid(Sprite sprite)
        {
            string path = SourcePath(sprite);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private static string SourcePath(Sprite sprite)
        {
            if (sprite == null) return string.Empty;
            string path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path) && sprite.texture != null) path = AssetDatabase.GetAssetPath(sprite.texture);
            return (path ?? string.Empty).Replace('\\', '/');
        }

        private static string EvidencePath(string fileName)
        {
            return Path.Combine(EvidenceFolder, fileName);
        }

        private static object GetField(object owner, string fieldName)
        {
            if (owner == null) return null;
            FieldInfo field = owner.GetType().GetField(fieldName, PrivateInstance | BindingFlags.Public);
            return field == null ? null : field.GetValue(owner);
        }

        private static T GetPrivateField<T>(object owner, string fieldName)
        {
            object value = GetField(owner, fieldName);
            Require(value is T, owner.GetType().Name + "." + fieldName + " is not " + typeof(T).Name);
            return (T)value;
        }

        private static void InvokePrivate(object owner, string methodName, params object[] arguments)
        {
            Invoke(owner, methodName, BindingFlags.Instance | BindingFlags.NonPublic, arguments);
        }

        private static T InvokePrivateResult<T>(object owner, string methodName, params object[] arguments)
        {
            return (T)Invoke(owner, methodName, BindingFlags.Instance | BindingFlags.NonPublic, arguments);
        }

        private static void InvokePublic(object owner, string methodName, params object[] arguments)
        {
            Invoke(owner, methodName, BindingFlags.Instance | BindingFlags.Public, arguments);
        }

        private static object Invoke(object owner, string methodName, BindingFlags flags, object[] arguments)
        {
            MethodInfo method = owner.GetType().GetMethods(flags)
                .FirstOrDefault(value => value.Name == methodName && value.GetParameters().Length == arguments.Length);
            Require(method != null, owner.GetType().Name + "." + methodName + " is missing");
            try { return method.Invoke(owner, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static void Product(List<Check> checks, string id, string matrix, string severity, string expected,
            Func<string> audit, string reproduction, string files)
        {
            try
            {
                checks.Add(NewCheck(id, matrix, "PASS", "PRODUCT", severity, expected, audit(), reproduction, files));
            }
            catch (Exception exception)
            {
                checks.Add(NewCheck(id, matrix, "FAIL", "PRODUCT", severity, expected, exception.Message, reproduction, files));
            }
        }

        private static void Infrastructure(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "INFRA_FAIL", "INFRASTRUCTURE", severity, expected, actual, reproduction, files));
        }

        private static void Unverified(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "UNVERIFIED", "HARDWARE", severity, expected, actual, reproduction, files));
        }

        private static void NotReady(List<Check> checks, string id, string matrix, string severity, string expected,
            string actual, string reproduction, string files)
        {
            checks.Add(NewCheck(id, matrix, "NOT_READY", "EXTERNAL", severity, expected, actual, reproduction, files));
        }

        private static Check NewCheck(string id, string matrix, string status, string classification, string severity,
            string expected, string actual, string reproduction, string files)
        {
            return new Check
            {
                id = id, matrix = matrix, status = status, classification = classification, severity = severity,
                expected = expected, actual = actual, reproduction = reproduction, recommendedFiles = files
            };
        }

        private static Report WriteReport(string stem, string title, DateTime started, List<Check> checks)
        {
            Report report = new Report
            {
                title = title,
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                passed = checks.Count(value => value.status == "PASS"),
                productFailed = checks.Count(value => value.status == "FAIL"),
                infrastructureFailed = checks.Count(value => value.status == "INFRA_FAIL"),
                unverified = checks.Count(value => value.status == "UNVERIFIED"),
                checks = checks.ToArray()
            };
            report.productOverall = report.productFailed == 0 ? "PASS" : "FAIL";
            report.infrastructureOverall = report.infrastructureFailed == 0 ? "PASS" : "FAIL";
            report.overall = report.infrastructureOverall == "FAIL" ? "FAIL" : report.productOverall == "PASS" ? "GREEN" : "RED";
            WriteJson(stem + ".json", report);
            StringBuilder text = new StringBuilder();
            text.AppendLine(title);
            text.AppendLine("Run ID: " + RunId);
            text.AppendLine("Baseline: " + BaselineCommit);
            text.AppendLine("Unity: " + Application.unityVersion);
            text.AppendLine("Overall/Product/Infrastructure: " + report.overall + "/" + report.productOverall + "/" + report.infrastructureOverall);
            text.AppendLine("PASS/FAIL/INFRA_FAIL/UNVERIFIED: " + report.passed + "/" + report.productFailed + "/" + report.infrastructureFailed + "/" + report.unverified);
            foreach (Check check in checks) text.AppendLine(check.id + " | " + check.status + " | " + check.actual);
            File.WriteAllText(Path.Combine(EvidenceFolder, stem + ".txt"), text.ToString(), Utf8NoBom);
            return report;
        }

        private static void WriteJson(string fileName, object value)
        {
            Directory.CreateDirectory(EvidenceFolder);
            File.WriteAllText(Path.Combine(EvidenceFolder, fileName), JsonUtility.ToJson(value, true) + Environment.NewLine, Utf8NoBom);
        }

        private static void WritePlayInfrastructureFailure(Exception exception)
        {
            List<Check> checks = new List<Check>
            {
                NewCheck("W19-I99.play_runner", "Play infrastructure", "INFRA_FAIL", "INFRASTRUCTURE", "P0",
                    "The Play runner emits parseable evidence", exception.ToString(),
                    "Run the Wave 19 entry point outside the Codex sandbox.",
                    "Assets/Editor/ParallelQA/Wave19ResourceConnectionGateRunner.cs")
            };
            WriteReport("wave19-play-contracts", "Wave 19 Play infrastructure failure", DateTime.UtcNow, checks);
            SessionState.SetBool(PlayExitPassKey, false);
            SessionState.SetString(PlayMessageKey, "INFRA_FAIL: " + exception);
        }

        private static void StopPlayContracts() { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; }

        private static void FinishPlayContracts()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (playTickAttached) { EditorApplication.update -= PlayTick; playTickAttached = false; }
            bool passed = SessionState.GetBool(PlayExitPassKey, false);
            string message = SessionState.GetString(PlayMessageKey, "INFRA_FAIL: missing Wave 19 Play result");
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayExitPassKey);
            SessionState.EraseString(PlayMessageKey);
            Debug.Log("[ParallelQA] " + message);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }
    }
}
