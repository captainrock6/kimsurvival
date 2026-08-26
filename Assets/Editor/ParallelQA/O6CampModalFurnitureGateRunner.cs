using System;
using System.IO;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParallelQA
{
    public static class O6CampModalFurnitureGateRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RunningKey = "O6CampModalFurniture.Running";
        private const string PassedKey = "O6CampModalFurniture.Passed";
        private const string MessageKey = "O6CampModalFurniture.Message";
        private static double earliestObservationTime;

        private static string EvidenceFolder
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable("KIM_QA_ARTIFACTS");
                return string.IsNullOrWhiteSpace(configured)
                    ? Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Artifacts", "ParallelQA", "o6-camp-modal-furniture")
                    : configured;
            }
        }

        public static void RunEditContracts()
        {
            Directory.CreateDirectory(EvidenceFolder);
            bool passed = KimSurvivalPrototype.RunO6CampDomainContracts(out string detail);
            File.WriteAllText(
                Path.Combine(EvidenceFolder, "o6-camp-modal-furniture-edit.txt"),
                (passed ? "PASS" : "FAIL") + Environment.NewLine + detail + Environment.NewLine);
            if (!passed) throw new InvalidOperationException(detail);
            Debug.Log("[O6 Camp] " + detail);
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
            if (SessionState.GetBool(RunningKey, false)) AttachCallbacks();
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
                if (prototype == null) throw new InvalidOperationException("KimSurvivalPrototype was not found in Play Mode.");
                string screenshot = Path.Combine(EvidenceFolder, "o6-basement-access-preview-ko-1280x800.png");
                bool renderedScreenshot = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
                bool passed = prototype.CaptureO6CampModalObservation(screenshot, out string message) &&
                              (!renderedScreenshot || VerifyPng(screenshot, 1280, 800));
                SessionState.SetBool(PassedKey, passed);
                SessionState.SetString(MessageKey, message);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(PassedKey, false);
                SessionState.SetString(MessageKey, exception.ToString());
            }

            bool passedResult = SessionState.GetBool(PassedKey, false);
            string messageResult = SessionState.GetString(MessageKey, string.Empty);
            File.WriteAllText(
                Path.Combine(EvidenceFolder, "o6-camp-modal-furniture-play.txt"),
                (passedResult ? "PASS" : "FAIL") + Environment.NewLine + messageResult + Environment.NewLine);
            EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false) || state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.SetBool(RunningKey, false);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(PassedKey, false) ? 0 : 1);
        }

        private static bool VerifyPng(string path, int expectedWidth, int expectedHeight)
        {
            if (!File.Exists(path)) return false;
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47) return false;
            int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return width == expectedWidth && height == expectedHeight;
        }
    }
}
