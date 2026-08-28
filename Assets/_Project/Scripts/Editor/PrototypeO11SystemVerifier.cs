using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KimSurvival.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypeO11SystemVerifier
    {
        private const string PlayRunningKey = "KimSurvival.O11System.PlayRunning";
        private const string PlayPassedKey = "KimSurvival.O11System.PlayPassed";
        private const string PlayMessageKey = "KimSurvival.O11System.PlayMessage";
        private static double playReadyAt;
        private static double playTimeoutAt;
        private static bool playTickAttached;

        static PrototypeO11SystemVerifier()
        {
            if (SessionState.GetBool(PlayRunningKey, false)) AttachPlayCallbacks();
        }

        [MenuItem("Kim Survival/Run O11 System Contracts")]
        public static void RunEditContracts()
        {
            IReadOnlyList<PrototypeContractProbe> probes = PrototypeO11SystemContracts.VerifyAll();
            string[] failures = probes.Where(probe => !probe.Success).Select(probe => probe.Detail).ToArray();
            foreach (PrototypeContractProbe probe in probes)
            {
                Debug.Log("[O11 System] " + (probe.Success ? "PASS " : "FAIL ") + probe.Detail);
            }
            if (failures.Length > 0)
            {
                throw new InvalidOperationException("O11 system contracts failed: " + string.Join(" | ", failures));
            }
            Debug.Log("[O11 System] PASS all " + probes.Count + " contracts");
        }

        public static void RunPlayContracts()
        {
            SessionState.SetBool(PlayRunningKey, true);
            SessionState.SetBool(PlayPassedKey, false);
            SessionState.SetString(PlayMessageKey, "O11 PlayMode verification did not complete.");
            AttachPlayCallbacks();
            EditorSceneManager.OpenScene(PrototypeProjectBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void AttachPlayCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (!playTickAttached)
            {
                EditorApplication.update += TickPlayContracts;
                playTickAttached = true;
            }
            playReadyAt = EditorApplication.timeSinceStartup + 1d;
            playTimeoutAt = EditorApplication.timeSinceStartup + 45d;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayRunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playReadyAt = EditorApplication.timeSinceStartup + 1d;
                playTimeoutAt = EditorApplication.timeSinceStartup + 45d;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishPlayContracts();
            }
        }

        private static void TickPlayContracts()
        {
            if (!SessionState.GetBool(PlayRunningKey, false) || !EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup < playReadyAt) return;
            if (EditorApplication.timeSinceStartup > playTimeoutAt)
            {
                CompletePlayContracts(false, "FAIL · timed out waiting for the playable scene");
                return;
            }

            KimSurvivalPrototype prototype = UnityEngine.Object.FindAnyObjectByType<KimSurvivalPrototype>();
            if (prototype == null) return;
            try
            {
                IReadOnlyList<PrototypeContractProbe> probes = PrototypeO11SystemContracts.VerifyAll();
                string[] failures = probes.Where(probe => !probe.Success).Select(probe => probe.Detail).ToArray();
                if (failures.Length > 0) throw new InvalidOperationException(string.Join(" | ", failures));

                PrototypePlayerPresentation presentation = GetField<PrototypePlayerPresentation>(prototype, "playerPresentation");
                PrototypeCampUse campUse = GetField<PrototypeCampUse>(prototype, "campUse");
                presentation.PlayAction(PrototypePlayerActionPose.FacilityUse, 5f);
                Invoke(prototype, "RestoreO11PlayerMovementPresentation");
                PrototypePlayerActionPose actionPose = (PrototypePlayerActionPose)GetFieldInfo(
                    typeof(PrototypePlayerPresentation), "actionPose").GetValue(presentation);
                bool settled = actionPose == PrototypePlayerActionPose.None &&
                               Mathf.Abs(presentation.transform.position.x - campUse.PlayerPosition.x) < 0.001f &&
                               Mathf.Abs(presentation.transform.position.y - campUse.PlayerPosition.y) < 0.001f;
                if (!settled) throw new InvalidOperationException("module reaction did not restore the live Kim presentation");

                CompletePlayContracts(true,
                    "PASS · O11 5/5 runtime contracts and live module-reaction presentation settlement");
            }
            catch (Exception exception)
            {
                CompletePlayContracts(false, "FAIL · " + exception);
            }
        }

        private static void CompletePlayContracts(bool passed, string message)
        {
            SessionState.SetBool(PlayPassedKey, passed);
            SessionState.SetString(PlayMessageKey, message);
            if (playTickAttached)
            {
                EditorApplication.update -= TickPlayContracts;
                playTickAttached = false;
            }
            EditorApplication.isPlaying = false;
        }

        private static void FinishPlayContracts()
        {
            bool passed = SessionState.GetBool(PlayPassedKey, false);
            string message = SessionState.GetString(PlayMessageKey, "No O11 PlayMode result.");
            string folder = Path.GetFullPath(Path.Combine("work", "O11Verification"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(
                Path.Combine(folder, "o11-playmode-result.txt"),
                message + Environment.NewLine + "Completed UTC: " + DateTime.UtcNow.ToString("O") + Environment.NewLine,
                new UTF8Encoding(false));
            Debug.Log("[O11 System] " + message);
            SessionState.EraseBool(PlayRunningKey);
            SessionState.EraseBool(PlayPassedKey);
            SessionState.EraseString(PlayMessageKey);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static FieldInfo GetFieldInfo(Type type, string name)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            return GetFieldInfo(target.GetType(), name).GetValue(target) as T ??
                   throw new InvalidOperationException("Missing live O11 field: " + name);
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            method.Invoke(target, null);
        }
    }
}
