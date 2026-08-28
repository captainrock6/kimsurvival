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
    public static class O11SearchNodeVisualCaptureRunner
    {
        private const string RunningKey = "KimSurvival.O11SearchNodeCapture.Running";
        private const string PassedKey = "KimSurvival.O11SearchNodeCapture.Passed";
        private const string MessageKey = "KimSurvival.O11SearchNodeCapture.Message";
        private const string CaptureFolder =
            "Artifacts/ParallelQA/20260829T031500Z_o11_search_node_visuals_green";
        private static bool tickAttached;
        private static double earliestRunTime;
        private static double timeoutAt;

        [Serializable]
        private sealed class CaptureRecord
        {
            public string scenario = string.Empty;
            public string locale = string.Empty;
            public string state = string.Empty;
            public string path = string.Empty;
            public bool prepared;
            public string detail = string.Empty;
        }

        [Serializable]
        private sealed class ValidationReport
        {
            public string schema = "kim-survival.o11-search-node-visual-validation.v1";
            public string jobId = "job_20260825150605_49020784";
            public string candidateId = "object.searchable-resource-node-kit.state-language-a";
            public string decision = "review";
            public string selectedCandidate = null;
            public string[] runtimeAllowlist = Array.Empty<string>();
            public bool packageAllowed;
            public bool formalRuntimeConnectAllowed;
            public bool provisionalReviewBuildConnection = true;
            public bool contractPassed;
            public string contractDetail = string.Empty;
            public CaptureRecord[] captures = Array.Empty<CaptureRecord>();
            public string completedUtc = string.Empty;
        }

        static O11SearchNodeVisualCaptureRunner()
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
            SessionState.SetString(MessageKey, "O11 search-node capture did not complete.");
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
            timeoutAt = EditorApplication.timeSinceStartup + 150d;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                earliestRunTime = EditorApplication.timeSinceStartup + 2d;
                timeoutAt = EditorApplication.timeSinceStartup + 150d;
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
                SessionState.SetString(MessageKey, "FAIL · timed out waiting for O11 search-node capture");
                StopPlayMode();
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;

            try
            {
                var captures = new List<CaptureRecord>();
                foreach (string locale in new[] { "ko", "en" })
                {
                    foreach (PrototypeExpeditionRegionId region in
                             Enum.GetValues(typeof(PrototypeExpeditionRegionId)).Cast<PrototypeExpeditionRegionId>())
                    {
                        bool prepared = prototype.PrepareO11RegionCapture(region, locale, out string detail);
                        string fileName = "o11-search-node-region-" + region.ToString().ToLowerInvariant() +
                                          "-" + locale + "-1280x800.png";
                        Capture(prototype, captures, "region-" + region, locale, "hidden", fileName, prepared, detail);
                    }
                }

                PrototypeSearchNodeKind[] kinds =
                    (PrototypeSearchNodeKind[])Enum.GetValues(typeof(PrototypeSearchNodeKind));
                PrototypeSearchNodeState[] states =
                    (PrototypeSearchNodeState[])Enum.GetValues(typeof(PrototypeSearchNodeState));
                for (int kindIndex = 0; kindIndex < kinds.Length; kindIndex += 1)
                {
                    for (int stateIndex = 0; stateIndex < states.Length; stateIndex += 1)
                    {
                        bool prepared = prototype.PrepareO11SearchNodeStateCapture(
                            kinds[kindIndex], states[stateIndex], "ko", out string detail);
                        string fileName = "o11-search-node-" + kinds[kindIndex].ToString().ToLowerInvariant() +
                                          "-" + states[stateIndex].ToString().ToLowerInvariant() +
                                          "-ko-1280x800.png";
                        Capture(prototype, captures, kinds[kindIndex].ToString(), "ko",
                            states[stateIndex].ToString(), fileName, prepared, detail);
                    }
                }

                bool nodeContract = prototype.RunO11SearchNodeVisualContract(out string nodeDetail);
                bool fullContract = prototype.RunO11ProductionVisualContract(out string fullDetail);
                bool capturesPassed = captures.All(record => record.prepared && File.Exists(record.path));
                var report = new ValidationReport
                {
                    contractPassed = nodeContract && fullContract && capturesPassed,
                    contractDetail = nodeDetail + "; full-o11=" + fullContract + "(" + fullDetail + ")",
                    captures = captures.ToArray(),
                    completedUtc = DateTime.UtcNow.ToString("O")
                };
                string reportPath = Path.Combine(CaptureFolder, "o11-search-node-visual-validation.json");
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + Environment.NewLine);
                SessionState.SetBool(PassedKey, report.contractPassed);
                SessionState.SetString(MessageKey,
                    (report.contractPassed ? "PASS" : "FAIL") + " · " + report.contractDetail +
                    " · captures=" + captures.Count + " · report=" + Path.GetFullPath(reportPath));
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, "FAIL · " + exception);
            }
            StopPlayMode();
        }

        private static void Capture(
            KimSurvivalPrototype prototype,
            ICollection<CaptureRecord> captures,
            string scenario,
            string locale,
            string state,
            string fileName,
            bool prepared,
            string detail)
        {
            string relativePath = Path.Combine(CaptureFolder, fileName).Replace('\\', '/');
            string absolutePath = Path.GetFullPath(relativePath);
            prototype.CaptureVerificationPng(absolutePath, 1280, 800);
            captures.Add(new CaptureRecord
            {
                scenario = scenario,
                locale = locale,
                state = state,
                path = absolutePath,
                prepared = prepared,
                detail = detail
            });
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
            string message = SessionState.GetString(MessageKey, "No O11 search-node capture result.");
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "o11-search-node-capture-run.txt"),
                (passed ? "PASS" : "FAIL") + Environment.NewLine + message + Environment.NewLine);
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(PassedKey);
            SessionState.EraseString(MessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.Log("[Kim Survival] O11 search-node capture " + (passed ? "passed" : "failed") + ": " + message);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }
}
