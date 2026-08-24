using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace KimSurvival.EditorTools
{
    public static class Wave16ExpeditionMapRuntimeGate
    {
        private const string DefaultEvidenceFolder = "Artifacts/Verification/wave16-expedition-map-a";
        private const string CandidatePath = "Assets/_Project/Art/Generated/ui_set/job_20260823150636_e3b39abc/candidate-a-right-rail-1280x800.png";
        private const string ManifestPath = "Assets/_Project/Art/Generated/ui_set/job_20260823150636_e3b39abc/wave15-expedition-map-a-selected-only-manifest.json";
        private const string ScenePath = "Assets/_Project/Scenes/KimSurvivalPrototype.unity";
        private const string RuntimePath = "Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs";
        private const string BuilderPath = "Assets/_Project/Scripts/Editor/PrototypeProjectBuilder.cs";
        private const string LocalizationPath = "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv";
        private const string CandidateGuid = "ae09637f2b24aa14295b1f9a5b4fde1c";

        [MenuItem("Kim Survival/Run Wave 16 Expedition Map A Contracts")]
        public static void RunContracts()
        {
            string evidenceFolder = Environment.GetEnvironmentVariable("KIM_SURVIVAL_VERIFICATION_FOLDER");
            if (string.IsNullOrWhiteSpace(evidenceFolder))
            {
                evidenceFolder = DefaultEvidenceFolder;
            }

            Directory.CreateDirectory(evidenceFolder);
            List<string> failures = new List<string>();
            string manifest = File.ReadAllText(ManifestPath);
            string scene = File.ReadAllText(ScenePath);
            string runtime = File.ReadAllText(RuntimePath);
            string builder = File.ReadAllText(BuilderPath);
            string localization = File.ReadAllText(LocalizationPath);

            Check(manifest.Contains("\"selectedCandidateId\": \"ui.expedition-map.right-rail-a\"") &&
                  manifest.Contains("\"selectedOnlyJobId\": \"job_20260823150636_e3b39abc\"") &&
                  manifest.Contains("\"runtimeConnected\": false"),
                "selected_only_manifest_is_canonical", failures);
            Check(AssetDatabase.AssetPathToGUID(CandidatePath) == CandidateGuid,
                "candidate_a_guid_preserved", failures);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CandidatePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CandidatePath);
            TextureImporter importer = AssetImporter.GetAtPath(CandidatePath) as TextureImporter;
            Check(texture != null && texture.width == 1280 && texture.height == 800 && sprite != null,
                "candidate_a_1280x800_sprite_imported", failures);
            Check(importer != null && importer.textureType == TextureImporterType.Sprite &&
                  importer.spriteImportMode == SpriteImportMode.Single &&
                  Mathf.Approximately(importer.spritePixelsPerUnit, 100f) &&
                  importer.filterMode == FilterMode.Bilinear && !importer.mipmapEnabled &&
                  !importer.alphaIsTransparency && importer.maxTextureSize == 2048,
                "candidate_a_forge_import_contract", failures);

            Check(scene.Contains(CandidateGuid), "active_scene_serializes_candidate_a_guid", failures);
            Check(runtime.Contains("ui.expedition-map.right-rail-a") &&
                  builder.Contains(CandidatePath) && builder.Contains("ConfigureExpeditionMapArt"),
                "runtime_connects_only_adopted_a", failures);

            string runtimeAndScene = runtime + "\n" + scene;
            Check(!runtimeAndScene.Contains("candidate-b-bottom-drawer", StringComparison.OrdinalIgnoreCase) &&
                  !runtimeAndScene.Contains("candidate-c-compact-right", StringComparison.OrdinalIgnoreCase) &&
                  !runtimeAndScene.Contains("expedition-map-review-board", StringComparison.OrdinalIgnoreCase) &&
                  !runtimeAndScene.Contains("expedition-map-state-comparison-board", StringComparison.OrdinalIgnoreCase) &&
                  !runtimeAndScene.Contains("expedition-map-input-focus-comparison", StringComparison.OrdinalIgnoreCase) &&
                  !runtimeAndScene.Contains("icon.expedition-resource-risk-set", StringComparison.OrdinalIgnoreCase),
                "review_and_unselected_assets_not_loaded", failures);

            Assembly runtimeAssembly = typeof(GameSession).Assembly;
            Type visualState = runtimeAssembly.GetType("KimSurvival.PrototypeExpeditionRegionVisualState");
            string[] requiredStates =
            {
                "Default", "Selected", "Locked", "RiskWarning",
                "EquipmentMissing", "DepartureReady", "Unknown"
            };
            bool hasAllStates = visualState != null && visualState.IsEnum;
            for (int index = 0; hasAllStates && index < requiredStates.Length; index += 1)
            {
                hasAllStates &= Enum.IsDefined(visualState, requiredStates[index]);
            }
            Check(hasAllStates, "seven_color_independent_region_states", failures);

            Type selection = runtimeAssembly.GetType("KimSurvival.PrototypeExpeditionMapSelection");
            Check(selection != null &&
                  selection.GetMethod("GetRegionState", BindingFlags.Public | BindingFlags.Instance) != null &&
                  selection.GetMethod("SetRegionStateForVerification", BindingFlags.Public | BindingFlags.Instance) != null &&
                  selection.GetMethod("CanDepartFocusedRegion", BindingFlags.Public | BindingFlags.Instance) != null,
                "region_state_model_and_verification_transition", failures);

            string[] stateKeys =
            {
                "expedition.map.state.default", "expedition.map.state.selected",
                "expedition.map.state.locked", "expedition.map.state.risk",
                "expedition.map.state.equipment_missing", "expedition.map.state.ready",
                "expedition.map.state.unknown"
            };
            bool localizedStates = true;
            for (int index = 0; index < stateKeys.Length; index += 1)
            {
                localizedStates &= localization.Contains(stateKeys[index] + "\t");
            }
            Check(localizedStates, "ko_en_qps_region_state_keys", failures);

            bool passed = failures.Count == 0;
            StringBuilder report = new StringBuilder();
            report.AppendLine(passed ? "PASS" : "EXPECTED_RED");
            report.AppendLine("Wave: 16 expedition map right-rail A runtime");
            report.AppendLine("Unity: " + Application.unityVersion);
            report.AppendLine("Baseline: 635725b3e2679a7d6d4f66c09b137575bac374c8");
            report.AppendLine("Candidate: ui.expedition-map.right-rail-a / job_20260823150636_e3b39abc");
            report.AppendLine("Contracts: selected-only scene GUID, Forge import, no B/C/review/icon runtime load, seven non-color states, localization, verification transitions");
            report.AppendLine("Failure count: " + failures.Count);
            for (int index = 0; index < failures.Count; index += 1)
            {
                report.AppendLine("EXPECTED_GAP: " + failures[index]);
            }

            string evidencePath = Path.Combine(evidenceFolder, "wave16-expedition-map-a-contracts.txt");
            File.WriteAllText(evidencePath, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            if (!passed)
            {
                throw new InvalidOperationException("Wave 16 expedition map A contracts are RED. See " + evidencePath);
            }
        }

        private static void Check(bool condition, string id, ICollection<string> failures)
        {
            if (!condition)
            {
                failures.Add(id);
            }
        }
    }
}
