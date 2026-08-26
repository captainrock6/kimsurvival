using System;
using System.IO;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParallelQA
{
    public static class O6WorldPresentationGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RunningKey = "O6WorldPresentation.Running";
        private const string PassedKey = "O6WorldPresentation.Passed";
        private const string MessageKey = "O6WorldPresentation.Message";
        private static double observeAfter;

        private static string EvidenceFolder
        {
            get
            {
                string runId = Environment.GetEnvironmentVariable("KIM_O6_WORLD_RUN_ID");
                if (string.IsNullOrWhiteSpace(runId)) runId = "o6-world-presentation";
                return Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory,
                    "Artifacts",
                    "ParallelQA",
                    runId);
            }
        }

        [MenuItem("Kim Survival/QA/O6 World Presentation Contracts")]
        public static void RunEditContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            bool passed = KimSurvivalPrototype.RunO6WorldPresentationContracts(out string detail);
            File.WriteAllText(
                Path.Combine(EvidenceFolder, "o6-world-presentation-edit.txt"),
                (passed ? "PASS" : "FAIL") + Environment.NewLine + detail + Environment.NewLine);
            if (!passed) throw new InvalidOperationException(detail);
            Debug.Log("[O6 World] " + detail);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "Play observation did not run.");
            Attach();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (SessionState.GetBool(RunningKey, false)) Attach();
        }

        private static void Attach()
        {
            observeAfter = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup < observeAfter) return;
            EditorApplication.update -= Tick;
            try
            {
                KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
                if (prototype == null) throw new InvalidOperationException("KimSurvivalPrototype missing in Play Mode.");
                bool passed = prototype.CaptureO6WorldPresentationObservation(EvidenceFolder, out string detail);
                SessionState.SetBool(PassedKey, passed);
                SessionState.SetString(MessageKey, detail);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, exception.ToString());
            }

            File.WriteAllText(
                Path.Combine(EvidenceFolder, "o6-world-presentation-play.txt"),
                (SessionState.GetBool(PassedKey, false) ? "PASS" : "FAIL") + Environment.NewLine +
                SessionState.GetString(MessageKey, string.Empty) + Environment.NewLine);
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false) || state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.SetBool(RunningKey, false);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(PassedKey, false) ? 0 : 1);
        }
    }
}
