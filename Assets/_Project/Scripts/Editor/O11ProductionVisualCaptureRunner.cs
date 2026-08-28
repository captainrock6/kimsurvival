using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KimSurvival;
using KimSurvival.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KimSurvivalEditor
{
    [InitializeOnLoad]
    public static class O11ProductionVisualCaptureRunner
    {
        private const string RunningKey = "KimSurvival.O11VisualCapture.Running";
        private const string PassedKey = "KimSurvival.O11VisualCapture.Passed";
        private const string MessageKey = "KimSurvival.O11VisualCapture.Message";
        private const string CaptureFolder = "Artifacts/ParallelQA/20260829T004300Z_o11_production_visuals_green";
        private static bool tickAttached;
        private static double earliestRunTime;
        private static double timeoutAt;

        [Serializable]
        private sealed class CaptureRecord
        {
            public string scenario = string.Empty;
            public string locale = string.Empty;
            public string resolution = string.Empty;
            public string path = string.Empty;
            public bool prepared;
            public string detail = string.Empty;
        }

        [Serializable]
        private sealed class ValidationReport
        {
            public string schema = "kim-survival.o11-production-visual-validation.v2";
            public string adoptedStyleJob = PrototypeO11ProductionSkin.AdoptedStyleJobId;
            public string reviewArtJob = "job_20260828150559_41c64580";
            public string adoptedCharacterJob = "job_20260822085926_374033c5";
            public string regionSourceJob = "job_20260826165624_448aecdc";
            public bool fullScreenRasterLoadedAtRuntime;
            public bool contractPassed;
            public string contractDetail = string.Empty;
            public CaptureRecord[] captures = Array.Empty<CaptureRecord>();
            public string completedUtc = string.Empty;
        }

        static O11ProductionVisualCaptureRunner()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Attach();
            }
        }

        public static void RunFromCommandLine()
        {
            Directory.CreateDirectory(CaptureFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "O11 capture did not complete.");
            Attach();
            EditorSceneManager.OpenScene(PrototypeProjectBuilder.ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void Attach()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (!tickAttached)
            {
                EditorApplication.update += Tick;
                tickAttached = true;
            }
            earliestRunTime = EditorApplication.timeSinceStartup + 2d;
            timeoutAt = EditorApplication.timeSinceStartup + 90d;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                earliestRunTime = EditorApplication.timeSinceStartup + 2d;
                timeoutAt = EditorApplication.timeSinceStartup + 90d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishAndExit();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup < earliestRunTime)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup > timeoutAt)
            {
                SessionState.SetString(MessageKey, "FAIL · timed out waiting for the O11 runtime");
                StopPlayMode();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null)
            {
                return;
            }

            try
            {
                prototype.RefreshO11ProductionVisuals();
                var captures = new List<CaptureRecord>();
                string[] locales = { "ko", "en" };
                string[] scenarios = { "camp", "search", "bag", "facility-popup", "escape-popup" };
                Vector2Int[] resolutions = { new Vector2Int(1280, 800), new Vector2Int(1920, 1080) };

                foreach (string locale in locales)
                {
                    foreach (string scenario in scenarios)
                    {
                        bool prepared = prototype.PrepareO11CaptureScenario(scenario, locale, out string detail);
                        foreach (Vector2Int resolution in resolutions)
                        {
                            string fileName = "o11-" + scenario + "-" + locale + "-" +
                                              resolution.x + "x" + resolution.y + ".png";
                            string relativePath = Path.Combine(CaptureFolder, fileName).Replace('\\', '/');
                            string absolutePath = Path.GetFullPath(relativePath);
                            prototype.CaptureVerificationPng(absolutePath, resolution.x, resolution.y);
                            captures.Add(new CaptureRecord
                            {
                                scenario = scenario,
                                locale = locale,
                                resolution = resolution.x + "x" + resolution.y,
                                path = relativePath,
                                prepared = prepared,
                                detail = detail
                            });
                        }
                    }
                }

                foreach (PrototypeExpeditionRegionId region in Enum.GetValues(typeof(PrototypeExpeditionRegionId)).Cast<PrototypeExpeditionRegionId>())
                {
                    bool prepared = prototype.PrepareO11RegionCapture(region, "ko", out string detail);
                    string fileName = "o11-region-" + region.ToString().ToLowerInvariant() + "-ko-1280x800.png";
                    string relativePath = Path.Combine(CaptureFolder, fileName).Replace('\\', '/');
                    prototype.CaptureVerificationPng(Path.GetFullPath(relativePath), 1280, 800);
                    captures.Add(new CaptureRecord
                    {
                        scenario = "region-" + region,
                        locale = "ko",
                        resolution = "1280x800",
                        path = relativePath,
                        prepared = prepared,
                        detail = detail
                    });
                }

                string[] animationStates = { "idle", "walk", "search", "ladder", "swim" };
                float[] animationPhases = { 0.05f, 0.32f };
                for (int stateIndex = 0; stateIndex < animationStates.Length; stateIndex += 1)
                {
                    for (int phaseIndex = 0; phaseIndex < animationPhases.Length; phaseIndex += 1)
                    {
                        bool prepared = prototype.PrepareO11AnimationCapture(
                            animationStates[stateIndex],
                            animationPhases[phaseIndex],
                            "ko",
                            out string detail);
                        string fileName = "o11-animation-" + animationStates[stateIndex] +
                                          "-frame-" + phaseIndex + "-ko-1280x800.png";
                        string relativePath = Path.Combine(CaptureFolder, fileName).Replace('\\', '/');
                        prototype.CaptureVerificationPng(Path.GetFullPath(relativePath), 1280, 800);
                        captures.Add(new CaptureRecord
                        {
                            scenario = "animation-" + animationStates[stateIndex] + "-frame-" + phaseIndex,
                            locale = "ko",
                            resolution = "1280x800",
                            path = relativePath,
                            prepared = prepared,
                            detail = detail
                        });
                    }
                }

                bool contract = prototype.RunO11ProductionVisualContract(out string contractDetail);
                bool capturesPassed = captures.All(record => record.prepared && File.Exists(record.path));
                var report = new ValidationReport
                {
                    fullScreenRasterLoadedAtRuntime = false,
                    contractPassed = contract && capturesPassed,
                    contractDetail = contractDetail,
                    captures = captures.ToArray(),
                    completedUtc = DateTime.UtcNow.ToString("O")
                };
                string reportPath = Path.Combine(CaptureFolder, "o11-runtime-visual-validation.json");
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + Environment.NewLine);
                SessionState.SetBool(PassedKey, report.contractPassed);
                SessionState.SetString(
                    MessageKey,
                    (report.contractPassed ? "PASS" : "FAIL") + " · " + contractDetail +
                    " · captures=" + captures.Count + " · report=" + Path.GetFullPath(reportPath));
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, "FAIL · " + exception);
            }

            StopPlayMode();
        }

        private static void StopPlayMode()
        {
            if (tickAttached)
            {
                EditorApplication.update -= Tick;
                tickAttached = false;
            }
            EditorApplication.isPlaying = false;
        }

        private static void FinishAndExit()
        {
            bool passed = SessionState.GetBool(PassedKey, false);
            string message = SessionState.GetString(MessageKey, "No O11 capture result.");
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(
                Path.Combine(CaptureFolder, "o11-capture-run.txt"),
                (passed ? "PASS" : "FAIL") + Environment.NewLine + message + Environment.NewLine);
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(PassedKey);
            SessionState.EraseString(MessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.Log("[Kim Survival] O11 production visual capture " + (passed ? "passed" : "failed") + ": " + message);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }
}
