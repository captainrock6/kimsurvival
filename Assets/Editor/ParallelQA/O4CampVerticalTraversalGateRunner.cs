using System;
using System.IO;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParallelQA
{
    public static class O4CampVerticalTraversalGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RunningKey = "O4CampVerticalTraversal.Running";
        private const string PassedKey = "O4CampVerticalTraversal.Passed";
        private const string MessageKey = "O4CampVerticalTraversal.Message";
        private static double earliestObservationTime;

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_O4_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "o4-camp-vertical-traversal" : value;
            }
        }

        private static string EvidenceFolder
        {
            get { return Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Artifacts", "ParallelQA", RunId); }
        }

        public static void RunEditContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            bool passed = KimSurvivalPrototype.RunO4CampVerticalTraversalContracts(out string detail);
            File.WriteAllText(
                Path.Combine(EvidenceFolder, "o4-camp-vertical-edit-contracts.txt"),
                (passed ? "PASS" : "FAIL") + Environment.NewLine + detail + Environment.NewLine);
            if (!passed)
            {
                throw new InvalidOperationException(detail);
            }
            Debug.Log("[O4] " + detail);
        }

        public static void RunPlayContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PassedKey, false);
            SessionState.SetString(MessageKey, "Play observation did not run.");
            AttachCallbacks();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void ResumePlayContracts()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
            }
        }

        private static void AttachCallbacks()
        {
            earliestObservationTime = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update -= PlayTick;
            EditorApplication.update += PlayTick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void PlayTick()
        {
            if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup < earliestObservationTime)
            {
                return;
            }

            EditorApplication.update -= PlayTick;
            try
            {
                KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
                if (prototype == null)
                {
                    throw new InvalidOperationException("KimSurvivalPrototype was not found in Play Mode.");
                }

                string screenshot = Path.Combine(EvidenceFolder, "o4-ladder-climb-ko-1280x800.png");
                bool passed = prototype.CaptureO4CampVerticalTraversalObservation(screenshot, out string message) &&
                              VerifyPng(screenshot, 1280, 800);
                if (!passed && File.Exists(screenshot))
                {
                    message += " PNG dimensions/signature failed.";
                }
                SessionState.SetBool(PassedKey, passed);
                SessionState.SetString(MessageKey, message);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, exception.ToString());
            }

            bool playRunPassed = SessionState.GetBool(PassedKey, false);
            string playRunMessage = SessionState.GetString(MessageKey, string.Empty);
            File.WriteAllText(
                Path.Combine(EvidenceFolder, "o4-camp-vertical-play-contracts.txt"),
                (playRunPassed ? "PASS" : "FAIL") + Environment.NewLine + playRunMessage + Environment.NewLine);
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false) || state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }
            SessionState.SetBool(RunningKey, false);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(SessionState.GetBool(PassedKey, false) ? 0 : 1);
            }
        }

        private static bool VerifyPng(string path, int expectedWidth, int expectedHeight)
        {
            if (!File.Exists(path)) return false;
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47)
            {
                return false;
            }
            int width = ReadBigEndianInt(bytes, 16);
            int height = ReadBigEndianInt(bytes, 20);
            return width == expectedWidth && height == expectedHeight;
        }

        private static int ReadBigEndianInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }
    }
}
