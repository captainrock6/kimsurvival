using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace ParallelQA
{
    public static class O6RaftTerminalPartsGateRunner
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        [Serializable]
        private sealed class Check
        {
            public string Id = string.Empty;
            public bool Passed;
            public string Detail = string.Empty;
        }

        [Serializable]
        private sealed class Report
        {
            public int SchemaVersion = 1;
            public string Title = "O6 raft terminal action and protected-part readability gate";
            public string UnityVersion = string.Empty;
            public bool Passed;
            public Check[] Checks = Array.Empty<Check>();
        }

        [MenuItem("Kim Survival/QA/O6 Raft Terminal Parts Gate")]
        public static void RunFromMenu() { Run(false); }

        public static void RunFromCommandLine() { Run(true); }

        private static void Run(bool exitEditor)
        {
            var checks = new List<Check>();
            var report = new Report { UnityVersion = Application.unityVersion };
            try
            {
                VerifyClosedWindowHasNoRepeatCost(checks);
                VerifyNaturalRaftRoute(checks);
                VerifyPartCountersAndLocalizedNextAction(checks);
                report.Checks = checks.ToArray();
                report.Passed = checks.All(check => check.Passed);
                WriteReport(report);
                if (!report.Passed)
                {
                    throw new InvalidOperationException("O6 raft gate failed: " +
                        string.Join(",", checks.Where(check => !check.Passed).Select(check => check.Id)));
                }
                Debug.Log("[ParallelQA] PASS · O6 raft terminal action and protected-part readability");
                if (exitEditor) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                report.Checks = checks.ToArray();
                report.Passed = false;
                WriteReport(report);
                Debug.LogException(exception);
                if (exitEditor) EditorApplication.Exit(1); else throw;
            }
        }

        private static void VerifyClosedWindowHasNoRepeatCost(ICollection<Check> checks)
        {
            bool hasClosedWindow = Enumerable.Range(1, 12)
                .Any(day => !PrototypeRaftLaunchWindowResolver.Resolve(PrototypeExpeditionRegionCatalog.DefaultRunSeed, day).Allowed);
            Add(checks, "O6-RAFT-01",
                PrototypeRaftEscapeConfig.LaunchAttemptFoodCost == 0 && hasClosedWindow,
                "launchAttemptFood=" + PrototypeRaftEscapeConfig.LaunchAttemptFoodCost + "; closedWindowFixture=" + hasClosedWindow);
        }

        private static void VerifyNaturalRaftRoute(ICollection<Check> checks)
        {
            PrototypeNaturalEscapeRouteResult result = PrototypeRaftRuntimeContract.RunNaturalRoute(null);
            bool noCostFailure = result.FailureAtomic && result.UnsafeWindowRejected &&
                                 result.InteractionTrace.Contains("raft.closed-window.no-cost");
            bool terminal = result.Success && result.Completed && result.Terminal &&
                            result.AllowedWindowLaunched && result.ResultCode == "escape_complete";
            Add(checks, "O6-RAFT-02", noCostFailure,
                "failureAtomic=" + result.FailureAtomic + "; trace=" + string.Join("|", result.InteractionTrace));
            Add(checks, "O6-RAFT-03", terminal,
                "success=" + result.Success + "; terminal=" + result.Terminal + "; day=" + result.Day);
        }

        private static void VerifyPartCountersAndLocalizedNextAction(ICollection<Check> checks)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            string main = File.ReadAllText(Path.Combine(root, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.cs"));
            string hardening = File.ReadAllText(Path.Combine(root, "Assets", "_Project", "Scripts", "Runtime", "KimSurvivalPrototype.GameJamSubmissionHardening.cs"));
            string stringsPath = Path.Combine(root, "Assets", "_Project", "Scripts", "Localization", "PrototypeStrings.tsv");
            string[] rows = File.ReadAllLines(stringsPath, Encoding.UTF8);
            var localizedKeys = new HashSet<string>(rows.Skip(1)
                .Where(row => !string.IsNullOrWhiteSpace(row))
                .Select(row => row.Split('\t'))
                .Where(columns => columns.Length == 4)
                .Select(columns => columns[0]), StringComparer.Ordinal);

            string[] gameJamPlayableEscapeIds = { "escape.raft", "escape.smoke", "escape.radio" };
            string[] requiredParts = PrototypeEscapeProjectCatalog.All
                .Where(definition => gameJamPlayableEscapeIds.Contains(definition.StableId))
                .SelectMany(definition => definition.RequiredKeyPartIds)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            bool allPartNamesLocalized = requiredParts.All(part => localizedKeys.Contains("search." + part));
            bool counters = main.Contains("? \"1/1\" : \"0/1\"") &&
                            hardening.Contains("? \"1/1\" : \"0/1\"");
            bool nextActions = localizedKeys.Contains("escape.raft.next.launch_now") &&
                               localizedKeys.Contains("escape.raft.next.wait_next_day") &&
                               hardening.Contains("escape.raft.next.launch_now") &&
                               hardening.Contains("escape.raft.next.wait_next_day");
            Add(checks, "O6-ESCAPE-01", allPartNamesLocalized && counters,
                "parts=" + string.Join(",", requiredParts) + "; localized=" + allPartNamesLocalized + "; counters=" + counters);
            Add(checks, "O6-ESCAPE-02", nextActions,
                "localized explicit open/closed next-action copy=" + nextActions);
        }

        private static void Add(ICollection<Check> checks, string id, bool passed, string detail)
        {
            checks.Add(new Check { Id = id, Passed = passed, Detail = detail ?? string.Empty });
        }

        private static void WriteReport(Report report)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            string configured = Environment.GetEnvironmentVariable("KIM_QA_ARTIFACTS");
            string output = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(projectRoot, "Artifacts", "ParallelQA", "O6RaftTerminalParts")
                : configured;
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "o6-raft-terminal-parts-report.json"),
                JsonUtility.ToJson(report, true) + Environment.NewLine, Utf8NoBom);
        }
    }
}
