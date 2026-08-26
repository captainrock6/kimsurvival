using System;
using System.IO;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParallelQA
{
    public static class O6SearchBagPlayGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RunningKey = "O6SearchBagPlay.Running";
        private const string PassedKey = "O6SearchBagPlay.Passed";
        private const string MessageKey = "O6SearchBagPlay.Message";
        private static double observeAfter;

        private static string EvidenceFolder
        {
            get
            {
                string runId = Environment.GetEnvironmentVariable("KIM_O6_RUN_ID");
                if (string.IsNullOrWhiteSpace(runId)) runId = "o6-search-bag-play";
                return Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory,
                    "Artifacts", "ParallelQA", runId);
            }
        }

        public static void RunFromCommandLine()
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
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
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
                bool passed = prototype.CaptureO6SearchBagObservation(EvidenceFolder, out string message);
                SessionState.SetBool(PassedKey, passed);
                SessionState.SetString(MessageKey, message);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, exception.ToString());
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, "o6-search-bag-play.txt"),
                (SessionState.GetBool(PassedKey, false) ? "PASS" : "FAIL") + Environment.NewLine +
                SessionState.GetString(MessageKey, string.Empty) + Environment.NewLine);
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false) || state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.SetBool(RunningKey, false);
            EditorApplication.playModeStateChanged -= OnPlayMode;
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(PassedKey, false) ? 0 : 1);
        }
    }
}
