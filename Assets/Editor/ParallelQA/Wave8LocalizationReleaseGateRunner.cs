using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using KimSurvival;
using TMPro;
using UnityEngine;

namespace ParallelQA
{
    /// <summary>
    /// Independent, non-shipping Wave 8 localization release contracts.
    /// The runner deliberately does not repair product localization data. Missing
    /// product capabilities are emitted as NOT_IMPLEMENTED so a later integration
    /// can rerun the same command without weakening the gate.
    /// </summary>
    internal static class Wave8LocalizationReleaseGateRunner
    {
        private const int ForgeCanonicalKeyCount = 138;
        private const string QpsLongLocaleCode = "qps-long";
        private const string SourceRelativePath = "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv";
        private const string FontProfileRelativePath = "Assets/_Project/Scripts/Localization/Resources/PrototypeLocaleFontProfile.asset";
        private const string RuntimeLocalizationRelativePath = "Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs";
        private const string InputRelativePath = "Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs";
        private const string BuilderRelativePath = "Assets/_Project/Scripts/Editor/PrototypeLocalizationAssetBuilder.cs";

        [Serializable]
        private sealed class ContractCheck
        {
            public string id;
            public string status;
            public string classification;
            public string severity;
            public string expected;
            public string actual;
            public string reproduction;
            public string recommendedFiles;
        }

        [Serializable]
        private sealed class ContractReport
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string command;
            public string productOverall;
            public string infrastructureOverall;
            public string overall;
            public int passed;
            public int failed;
            public int notImplemented;
            public int unverified;
            public int infrastructureFailed;
            public ContractCheck[] checks;
        }

        [Serializable]
        private sealed class BagRecord
        {
            public string kind;
            public int amount;
        }

        [Serializable]
        private sealed class PlacementRecord
        {
            public string kind;
            public float x;
        }

        [Serializable]
        private sealed class ProgressSnapshot
        {
            public int day;
            public float hunger;
            public float energy;
            public float daylight;
            public string phase;
            public string result;
            public bool expeditionCompleted;
            public bool swimming;
            public int signalStage;
            public int activeBagSlots;
            public int[] storage;
            public BagRecord[] bag;
            public bool[] structures;
            public bool[] researched;
            public bool[] crafted;
            public PlacementRecord[] placements;

            public string Fingerprint()
            {
                StringBuilder value = new StringBuilder();
                value.Append("day=").Append(day)
                    .Append(";hunger=").Append(hunger.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append(";energy=").Append(energy.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append(";daylight=").Append(daylight.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append(";phase=").Append(phase)
                    .Append(";result=").Append(result)
                    .Append(";expeditionCompleted=").Append(expeditionCompleted)
                    .Append(";swimming=").Append(swimming)
                    .Append(";signal=").Append(signalStage)
                    .Append(";bagCapacity=").Append(activeBagSlots)
                    .Append(";storage=").Append(string.Join(",", storage ?? Array.Empty<int>()))
                    .Append(";bag=").Append(string.Join(",", (bag ?? Array.Empty<BagRecord>()).Select(slot => slot.kind + ":" + slot.amount)))
                    .Append(";structures=").Append(string.Join(",", structures ?? Array.Empty<bool>()))
                    .Append(";researched=").Append(string.Join(",", researched ?? Array.Empty<bool>()))
                    .Append(";crafted=").Append(string.Join(",", crafted ?? Array.Empty<bool>()))
                    .Append(";placements=").Append(string.Join(",", (placements ?? Array.Empty<PlacementRecord>())
                        .OrderBy(item => item.kind, StringComparer.Ordinal)
                        .Select(item => item.kind + ":" + item.x.ToString("0.000", CultureInfo.InvariantCulture))));
                return value.ToString();
            }
        }

        [Serializable]
        private sealed class LocaleRestoreObservation
        {
            public string requestedLocale;
            public string observedLocale;
            public bool selected;
            public bool restoredFingerprintMatches;
            public bool formatPreservedFingerprint;
            public string fingerprint;
        }

        [Serializable]
        private sealed class SnapshotEvidence
        {
            public int schemaVersion = 1;
            public string runId;
            public string baselineCommit;
            public string unityVersion;
            public ProgressSnapshot newGameFourSlot;
            public ProgressSnapshot progressedSixSlot;
            public string canonicalFingerprint;
            public LocaleRestoreObservation[] localeRestores;
        }

        private sealed class SourceTable
        {
            internal string[] Headers;
            internal readonly List<string[]> Rows = new List<string[]>();
            internal int KeyIndex;
            internal int KoIndex;
            internal int EnIndex;
            internal int QpsIndex;

            internal string Value(string[] row, int index)
            {
                return index >= 0 && index < row.Length ? Decode(row[index]) : string.Empty;
            }
        }

        private static string ProjectRoot
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); }
        }

        private static string RunId
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_RUN_ID");
                return string.IsNullOrWhiteSpace(value) ? "manual-wave8" : value;
            }
        }

        private static string BaselineCommit
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("KIM_PARALLEL_QA_BASELINE");
                return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
            }
        }

        private static string EvidenceFolder
        {
            get { return Path.Combine(ProjectRoot, "Artifacts", "ParallelQA", RunId); }
        }

        public static void RunEditContracts()
        {
            DateTime started = DateTime.UtcNow;
            Directory.CreateDirectory(EvidenceFolder);
            List<ContractCheck> checks = new List<ContractCheck>();
            SourceTable table = ReadSourceTable();

            AddProduct(checks, "W8-E01.canonical_key_count", "P1",
                "The versioned canonical localization contract contains exactly the Forge-specified 138 unique keys",
                table.Rows.Count == ForgeCanonicalKeyCount && DuplicateKeys(table).Length == 0,
                "rows=" + table.Rows.Count + " forgeExpected=" + ForgeCanonicalKeyCount + " delta=" + (table.Rows.Count - ForgeCanonicalKeyCount) +
                " duplicates=" + Join(DuplicateKeys(table)),
                "Import PrototypeStrings.tsv and compare its unique keys with task.qa.feature.localization.",
                SourceRelativePath + "; .forge/backlog.json");

            string[] emptyKo = EmptyKeys(table, table.KoIndex);
            string[] emptyEn = EmptyKeys(table, table.EnIndex).Where(key => key != "dev.fallback_probe").ToArray();
            AddProduct(checks, "W8-E02.ko_en_key_value_parity", "P1",
                "Every canonical key has Korean source and English content except the deliberate fallback probe",
                table.KeyIndex >= 0 && table.KoIndex >= 0 && table.EnIndex >= 0 && emptyKo.Length == 0 && emptyEn.Length == 0,
                "header=" + string.Join("/", table.Headers) + " emptyKo=" + Join(emptyKo) + " unexpectedEmptyEn=" + Join(emptyEn),
                "Run the Wave 8 Edit contract and inspect the source schema and empty-key inventory.",
                SourceRelativePath);

            string[] placeholderMismatches = PlaceholderMismatches(table, table.KoIndex, table.EnIndex);
            AddProduct(checks, "W8-E03.ko_en_placeholder_parity", "P1",
                "Korean and English use identical placeholder token sets for every canonical key",
                placeholderMismatches.Length == 0,
                "mismatches=" + Join(placeholderMismatches),
                "Compare placeholder sets per key in PrototypeStrings.tsv.",
                SourceRelativePath);

            string[] positionalKeys = PositionalPlaceholderKeys(table);
            int namedPlaceholderCount = NamedPlaceholderCount(table);
            AddProduct(checks, "W8-E04.named_placeholder_contract", "P1",
                "Player-facing formatted strings use stable named placeholders rather than positional {0} tokens",
                positionalKeys.Length == 0 && namedPlaceholderCount > 0,
                "positionalKeys=" + positionalKeys.Length + " namedPlaceholderTokens=" + namedPlaceholderCount + " examples=" + Join(positionalKeys.Take(12)),
                "Scan all ko/en/qps-long fields for positional placeholders and replace them through the product localization contract.",
                SourceRelativePath + "; " + RuntimeLocalizationRelativePath);

            bool qpsColumn = table.QpsIndex >= 0;
            bool qpsLocaleAsset = Directory.GetFiles(Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "Locales"), "*.asset")
                .Any(path => File.ReadAllText(path).IndexOf(QpsLongLocaleCode, StringComparison.OrdinalIgnoreCase) >= 0);
            bool qpsTableAsset = Directory.GetFiles(Path.Combine(ProjectRoot, "Assets", "_Project", "Scripts", "Localization", "Tables"), "*.asset")
                .Any(path => Path.GetFileName(path).IndexOf("qps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             File.ReadAllText(path).IndexOf("m_Code: " + QpsLongLocaleCode, StringComparison.OrdinalIgnoreCase) >= 0);
            string fontProfileSource = File.ReadAllText(Path.Combine(ProjectRoot, FontProfileRelativePath));
            bool qpsFontMapping = fontProfileSource.IndexOf("localeCode: " + QpsLongLocaleCode, StringComparison.OrdinalIgnoreCase) >= 0;
            AddNotImplementedOrPass(checks, "W8-E05.qps_long_data_registration", "P2",
                "qps-long is a non-shipping data locale with a full table and an explicit font mapping",
                qpsColumn && qpsLocaleAsset && qpsTableAsset && qpsFontMapping,
                "column=" + qpsColumn + " localeAsset=" + qpsLocaleAsset + " tableAsset=" + qpsTableAsset + " fontMapping=" + qpsFontMapping,
                "Add the non-shipping qps-long data assets without adding locale-specific gameplay branches.",
                SourceRelativePath + "; " + BuilderRelativePath + "; " + FontProfileRelativePath);

            QpsContract qps = EvaluateQps(table);
            AddNotImplementedOrPass(checks, "W8-E06.qps_long_expansion_tokens_glyphs", "P2",
                "Every qps-long entry expands English by 35-50%, preserves tokens/digits/tags, is wrapped, and contains the required extended glyph probe",
                qpsColumn && qps.Passed,
                qps.Actual,
                "Run the source-table contract over the qps-long column and inspect each failing key.",
                SourceRelativePath + "; " + FontProfileRelativePath);

            SnapshotEvidence snapshotEvidence = BuildSnapshotEvidence();
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave8-progress-snapshot.json"), JsonUtility.ToJson(snapshotEvidence, true) + Environment.NewLine, new UTF8Encoding(false));
            bool restoreInfrastructure = snapshotEvidence.localeRestores.All(item => item.restoredFingerprintMatches && item.formatPreservedFingerprint);
            AddInfrastructure(checks, "W8-I01.qa_snapshot_roundtrip", "P0",
                "The independent QA serializer restores Day/resources/research/facilities/signal/bag 4/6 state exactly",
                restoreInfrastructure,
                "baseCapacity=" + snapshotEvidence.newGameFourSlot.activeBagSlots + " progressedCapacity=" + snapshotEvidence.progressedSixSlot.activeBagSlots +
                " fingerprint=" + snapshotEvidence.canonicalFingerprint,
                "Run Wave8LocalizationReleaseGateRunner.RunEditContracts and compare wave8-progress-snapshot.json.",
                "Assets/Editor/ParallelQA/Wave8LocalizationReleaseGateRunner.cs");

            bool allLocalesSelected = snapshotEvidence.localeRestores.All(item => item.selected);
            AddProduct(checks, "W8-E07.locale_snapshot_invariance", "P1",
                "The same QA progress snapshot restores under ko, en, and qps-long without changing canonical progression state",
                restoreInfrastructure && allLocalesSelected,
                string.Join(" | ", snapshotEvidence.localeRestores.Select(item => item.requestedLocale + "->" + item.observedLocale +
                    " selected=" + item.selected + " state=" + item.restoredFingerprintMatches + "/" + item.formatPreservedFingerprint)),
                "Restore wave8-progress-snapshot.json once per locale and compare canonical fingerprints before and after formatting.",
                RuntimeLocalizationRelativePath + "; " + BuilderRelativePath + "; " + SourceRelativePath);

            InputObservation input = VerifySyntheticPromptSwitch();
            AddProduct(checks, "W8-E08.synthetic_device_prompt_state_invariance", "P1",
                "Synthetic keyboard/gamepad activity changes the prompt while locale and progression fingerprint remain unchanged",
                input.Passed,
                input.Actual,
                "Switch PrototypeInputDeviceTracker activity and format the shared camp prompt from the same restored snapshot.",
                InputRelativePath + "; " + RuntimeLocalizationRelativePath);

            ActionTokenObservation actionTokens = EvaluateActionTokens(table);
            AddProduct(checks, "W8-E09.action_placeholder_contract", "P1",
                "Input prompts are driven by named action placeholders instead of device-specific translated key branches and literal bindings",
                actionTokens.Passed,
                actionTokens.Actual,
                "Inspect controls.* source rows and PrototypeInputPromptKeys for named action tokens and locale-independent action IDs.",
                InputRelativePath + "; " + SourceRelativePath);

            FallbackObservation fallback = VerifyFallbackAndLog();
            AddProduct(checks, "W8-E10.missing_key_korean_fallback_log", "P1",
                "A missing English entry displays Korean and emits one identifiable development warning without mutating state",
                fallback.Passed,
                fallback.Actual,
                "Format dev.fallback_probe twice in en while capturing Application log callbacks and the progression fingerprint.",
                RuntimeLocalizationRelativePath + "; " + SourceRelativePath);

            StartupObservation startup = VerifyStartupFallback();
            AddProduct(checks, "W8-E11.default_and_invalid_locale_fallback", "P1",
                "No preference and invalid locale values resolve deterministically to Korean",
                startup.Passed,
                startup.Actual,
                "Call ResolveStartupLocale for ko and an unsupported locale and verify the result.",
                RuntimeLocalizationRelativePath);

            checks.Add(new ContractCheck
            {
                id = "W8-HW.physical_gamepad",
                status = "UNVERIFIED",
                classification = "HARDWARE_GAP",
                severity = "P1",
                expected = "Human actuation on a physical gamepad changes action prompts and completes locale settings plus the localized core loop",
                actual = "No human physical-device actuation is part of this batch contract; detection or synthetic actions cannot promote this gate.",
                reproduction = "Run the Windows release candidate with a physical controller and record device name/VID/PID plus human inputs.",
                recommendedFiles = "manual release-candidate hardware evidence"
            });

            ContractReport report = WriteReport(started, checks);
            if (report.infrastructureOverall != "PASS")
            {
                throw new InvalidOperationException("Wave 8 localization infrastructure failed. See " + Path.Combine(EvidenceFolder, "wave8-edit-contracts.json"));
            }
            if (report.productOverall != "PASS")
            {
                throw new InvalidOperationException("Wave 8 localization product contracts are not green. See " + Path.Combine(EvidenceFolder, "wave8-edit-contracts.json"));
            }
        }

        private sealed class QpsContract
        {
            internal bool Passed;
            internal string Actual;
        }

        private static QpsContract EvaluateQps(SourceTable table)
        {
            if (table.QpsIndex < 0)
            {
                return new QpsContract { Passed = false, Actual = "qps-long column is absent; actual locale expansion and glyph contract cannot execute" };
            }

            List<string> failures = new List<string>();
            List<float> ratios = new List<float>();
            foreach (string[] row in table.Rows)
            {
                string key = table.Value(row, table.KeyIndex);
                string english = table.Value(row, table.EnIndex);
                string pseudo = table.Value(row, table.QpsIndex);
                if (string.IsNullOrEmpty(english) && key == "dev.fallback_probe") continue;
                float ratio = english.Length == 0 ? 1f : pseudo.Length / (float)english.Length;
                ratios.Add(ratio);
                bool ratioPass = ratio >= 1.35f && ratio <= 1.50f;
                bool wrapper = pseudo.StartsWith("⟦", StringComparison.Ordinal) && pseudo.EndsWith("⟧", StringComparison.Ordinal);
                bool tokens = Placeholders(english).SetEquals(Placeholders(pseudo));
                bool digits = Sequence(Regex.Matches(english, @"\d+")).SequenceEqual(Sequence(Regex.Matches(pseudo, @"\d+")));
                bool tags = Sequence(Regex.Matches(english, @"<[^>]+>")).SequenceEqual(Sequence(Regex.Matches(pseudo, @"<[^>]+>")));
                if (!ratioPass || !wrapper || !tokens || !digits || !tags)
                {
                    failures.Add(key + " ratio=" + ratio.ToString("0.000", CultureInfo.InvariantCulture) + " wrapper=" + wrapper +
                                 " tokens=" + tokens + " digits=" + digits + " tags=" + tags);
                }
            }

            string allPseudo = string.Join("", table.Rows.Select(row => table.Value(row, table.QpsIndex)));
            const string requiredAccents = "áéíóúüñ¿¡";
            string missingGlyphProbe = new string(requiredAccents.Where(character => allPseudo.IndexOf(character) < 0).ToArray());
            bool passed = failures.Count == 0 && ratios.Count == table.Rows.Count - 1 && missingGlyphProbe.Length == 0;
            return new QpsContract
            {
                Passed = passed,
                Actual = "rows=" + ratios.Count + " ratioMin=" + (ratios.Count == 0 ? "n/a" : ratios.Min().ToString("0.000", CultureInfo.InvariantCulture)) +
                         " ratioMax=" + (ratios.Count == 0 ? "n/a" : ratios.Max().ToString("0.000", CultureInfo.InvariantCulture)) +
                         " missingRequiredGlyphs=" + (missingGlyphProbe.Length == 0 ? "none" : missingGlyphProbe) +
                         " failures=" + Join(failures.Take(12))
            };
        }

        private static SnapshotEvidence BuildSnapshotEvidence()
        {
            GameSession baseSession = new GameSession();
            PrototypeCampPlacement basePlacement = new PrototypeCampPlacement();
            ProgressSnapshot baseSnapshot = Capture(baseSession, basePlacement);

            GameSession progressed = BuildProgressedSession();
            PrototypeCampPlacement progressedPlacement = BuildProgressedPlacement();
            ProgressSnapshot canonical = Capture(progressed, progressedPlacement);
            List<LocaleRestoreObservation> observations = new List<LocaleRestoreObservation>();

            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                string originalLocale = localization.CurrentLocaleCode;
                foreach (string requested in new[] { PrototypeLocalization.KoreanLocaleCode, PrototypeLocalization.EnglishLocaleCode, QpsLongLocaleCode })
                {
                    Restore(canonical, out GameSession restoredSession, out PrototypeCampPlacement restoredPlacement);
                    string restoredFingerprint = Capture(restoredSession, restoredPlacement).Fingerprint();
                    localization.SetLocale(requested, false);
                    string beforeFormat = Capture(restoredSession, restoredPlacement).Fingerprint();
                    localization.Format("hud.status.camp", restoredSession.Day, GameSession.FinalDay, "phase", restoredSession.Hunger, restoredSession.Energy);
                    localization.Format("button.signal.progress", restoredSession.SignalStage);
                    localization.Format("button.bag_upgrade.complete", restoredSession.ActiveBagSlotCount);
                    string afterFormat = Capture(restoredSession, restoredPlacement).Fingerprint();
                    observations.Add(new LocaleRestoreObservation
                    {
                        requestedLocale = requested,
                        observedLocale = localization.CurrentLocaleCode,
                        selected = localization.CurrentLocaleCode == requested,
                        restoredFingerprintMatches = restoredFingerprint == canonical.Fingerprint(),
                        formatPreservedFingerprint = beforeFormat == afterFormat && afterFormat == canonical.Fingerprint(),
                        fingerprint = afterFormat
                    });
                }
                if (originalLocale == PrototypeLocalization.KoreanLocaleCode || originalLocale == PrototypeLocalization.EnglishLocaleCode)
                {
                    localization.SetLocale(originalLocale, false);
                }
            }

            return new SnapshotEvidence
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                newGameFourSlot = baseSnapshot,
                progressedSixSlot = canonical,
                canonicalFingerprint = canonical.Fingerprint(),
                localeRestores = observations.ToArray()
            };
        }

        private static GameSession BuildProgressedSession()
        {
            GameSession session = new GameSession();
            session.Grant(ResourceKind.Wood, 30);
            session.Grant(ResourceKind.Stone, 30);
            session.Grant(ResourceKind.Food, 5);
            session.Grant(ResourceKind.Salvage, 30);
            Require(session.TryBuild(StructureKind.Campfire), "progress fixture campfire");
            Require(session.TryBuild(StructureKind.Workbench), "progress fixture workbench");
            Require(session.TryBuild(StructureKind.RainCollector), "progress fixture rain collector");
            Require(session.TryResearch(TechKind.StoneAxe), "progress fixture axe research");
            Require(session.TryCraft(TechKind.StoneAxe), "progress fixture axe craft");
            Require(session.TryResearch(TechKind.Rope), "progress fixture rope research");
            Require(session.TryCraft(TechKind.Rope), "progress fixture rope craft");
            Require(session.TryUpgradeBagCapacity(), "progress fixture bag six");
            Require(session.TryUpgradeSignal(), "progress fixture signal stage one");
            Require(session.BeginSearch(), "progress fixture expedition");
            Require(session.ReturnToCamp(false), "progress fixture return");
            Require(session.EndDay(), "progress fixture day two");
            return session;
        }

        private static PrototypeCampPlacement BuildProgressedPlacement()
        {
            PrototypeCampPlacement placement = new PrototypeCampPlacement();
            Place(placement, StructureKind.Campfire, -1.5f);
            Place(placement, StructureKind.Workbench, 1.5f);
            Place(placement, StructureKind.RainCollector, 3.5f);
            return placement;
        }

        private static void Place(PrototypeCampPlacement placement, StructureKind kind, float x)
        {
            placement.Begin(kind, false);
            placement.SetCandidateX(x);
            Require(placement.Commit(), "progress fixture placement " + kind);
        }

        private static ProgressSnapshot Capture(GameSession session, PrototypeCampPlacement placement)
        {
            return new ProgressSnapshot
            {
                day = session.Day,
                hunger = session.Hunger,
                energy = session.Energy,
                daylight = session.Daylight,
                phase = session.Phase.ToString(),
                result = session.Result.ToString(),
                expeditionCompleted = session.ExpeditionCompleted,
                swimming = session.IsSwimming,
                signalStage = session.SignalStage,
                activeBagSlots = session.ActiveBagSlotCount,
                storage = Enum.GetValues(typeof(ResourceKind)).Cast<ResourceKind>().Select(session.GetStorage).ToArray(),
                bag = Enumerable.Range(0, GameSession.MaximumBagSlotCount).Select(index =>
                {
                    BagStack slot = GetRawBag(session, index);
                    return new BagRecord { kind = slot.Kind.ToString(), amount = slot.Amount };
                }).ToArray(),
                structures = Enum.GetValues(typeof(StructureKind)).Cast<StructureKind>().Select(session.HasStructure).ToArray(),
                researched = Enum.GetValues(typeof(TechKind)).Cast<TechKind>().Select(session.HasResearched).ToArray(),
                crafted = Enum.GetValues(typeof(TechKind)).Cast<TechKind>().Select(session.HasCrafted).ToArray(),
                placements = Enum.GetValues(typeof(StructureKind)).Cast<StructureKind>()
                    .Where(placement.HasInstalledPosition)
                    .Select(kind => new PlacementRecord { kind = kind.ToString(), x = placement.GetInstalledPosition(kind).x })
                    .ToArray()
            };
        }

        private static void Restore(ProgressSnapshot snapshot, out GameSession session, out PrototypeCampPlacement placement)
        {
            session = new GameSession();
            CopyIntoPrivateArray(session, "storage", snapshot.storage);
            BagStack[] bags = snapshot.bag.Select(record => new BagStack((ResourceKind)Enum.Parse(typeof(ResourceKind), record.kind), record.amount)).ToArray();
            CopyIntoPrivateArray(session, "bag", bags);
            CopyIntoPrivateArray(session, "structures", snapshot.structures);
            CopyIntoPrivateArray(session, "researched", snapshot.researched);
            CopyIntoPrivateArray(session, "craftedTools", snapshot.crafted);
            SetProperty(session, "Day", snapshot.day);
            SetProperty(session, "Hunger", snapshot.hunger);
            SetProperty(session, "Energy", snapshot.energy);
            SetProperty(session, "Daylight", snapshot.daylight);
            SetProperty(session, "Phase", (GamePhase)Enum.Parse(typeof(GamePhase), snapshot.phase));
            SetProperty(session, "Result", (RunResult)Enum.Parse(typeof(RunResult), snapshot.result));
            SetProperty(session, "ExpeditionCompleted", snapshot.expeditionCompleted);
            SetProperty(session, "IsSwimming", snapshot.swimming);
            SetProperty(session, "SignalStage", snapshot.signalStage);
            SetProperty(session, "ActiveBagSlotCount", snapshot.activeBagSlots);

            placement = new PrototypeCampPlacement();
            FieldInfo installedField = typeof(PrototypeCampPlacement).GetField("installedX", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(installedField != null, "placement snapshot reflection field");
            Dictionary<StructureKind, float> installed = (Dictionary<StructureKind, float>)installedField.GetValue(placement);
            foreach (PlacementRecord record in snapshot.placements)
            {
                installed[(StructureKind)Enum.Parse(typeof(StructureKind), record.kind)] = record.x;
            }
        }

        private static BagStack GetRawBag(GameSession session, int index)
        {
            FieldInfo field = typeof(GameSession).GetField("bag", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null, "bag snapshot reflection field");
            return ((BagStack[])field.GetValue(session))[index];
        }

        private static void CopyIntoPrivateArray<T>(object target, string fieldName, T[] values)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null, "snapshot field " + fieldName);
            T[] destination = (T[])field.GetValue(target);
            Require(destination.Length == values.Length, "snapshot field length " + fieldName);
            Array.Copy(values, destination, values.Length);
        }

        private static void SetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Require(property != null, "snapshot property " + name);
            property.SetValue(target, value, null);
        }

        private sealed class InputObservation
        {
            internal bool Passed;
            internal string Actual;
        }

        private static InputObservation VerifySyntheticPromptSwitch()
        {
            GameSession session = BuildProgressedSession();
            PrototypeCampPlacement placement = BuildProgressedPlacement();
            string before = Capture(session, placement).Fingerprint();
            PrototypeInputDeviceTracker tracker = new PrototypeInputDeviceTracker();
            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                string localeBefore = localization.CurrentLocaleCode;
                tracker.Update(new PrototypeInputActivity(true, false));
                string keyboard = localization.Format(PrototypeInputPromptKeys.Camp(tracker.ActiveDevice), localization.DeviceName(tracker.ActiveDevice));
                tracker.Update(new PrototypeInputActivity(false, true));
                string gamepad = localization.Format(PrototypeInputPromptKeys.Camp(tracker.ActiveDevice), localization.DeviceName(tracker.ActiveDevice));
                string after = Capture(session, placement).Fingerprint();
                bool passed = keyboard != gamepad && keyboard.Contains(localization.DeviceName(PrototypeInputDevice.KeyboardMouse)) &&
                              gamepad.Contains(localization.DeviceName(PrototypeInputDevice.Gamepad)) && localeBefore == localization.CurrentLocaleCode && before == after;
                return new InputObservation
                {
                    Passed = passed,
                    Actual = "keyboard=" + Normalize(keyboard) + " gamepad=" + Normalize(gamepad) + " locale=" + localeBefore + "->" +
                             localization.CurrentLocaleCode + " stateUnchanged=" + (before == after)
                };
            }
        }

        private sealed class ActionTokenObservation
        {
            internal bool Passed;
            internal string Actual;
        }

        private static ActionTokenObservation EvaluateActionTokens(SourceTable table)
        {
            Regex action = new Regex(@"\{(?:device|move|navigate|confirm|cancel|language|jump|gather|return|action)(?::[^{}]+)?\}", RegexOptions.IgnoreCase);
            int actionTokens = table.Rows.Sum(row => new[] { table.Value(row, table.KoIndex), table.Value(row, table.EnIndex), table.Value(row, table.QpsIndex) }
                .Sum(value => action.Matches(value).Count));
            string inputSource = File.ReadAllText(Path.Combine(ProjectRoot, InputRelativePath));
            string[] literalBindings = { "A/D", "Space", "Enter", "Esc", "left stick", "D-pad", "A ·", "B ·", "X ·", "Y ·" };
            string[] observedLiterals = literalBindings.Where(value => table.Rows.Any(row =>
                table.Value(row, table.KoIndex).Contains(value) || table.Value(row, table.EnIndex).Contains(value))).ToArray();
            bool deviceSpecificKeyBranches = inputSource.Contains("controls.camp.gamepad") && inputSource.Contains("controls.camp.keyboard_mouse");
            return new ActionTokenObservation
            {
                Passed = actionTokens > 0 && observedLiterals.Length == 0 && !deviceSpecificKeyBranches,
                Actual = "actionTokens=" + actionTokens + " literalBindings=" + Join(observedLiterals) + " deviceSpecificKeyBranches=" + deviceSpecificKeyBranches
            };
        }

        private sealed class FallbackObservation
        {
            internal bool Passed;
            internal string Actual;
        }

        private static FallbackObservation VerifyFallbackAndLog()
        {
            GameSession session = BuildProgressedSession();
            PrototypeCampPlacement placement = BuildProgressedPlacement();
            string before = Capture(session, placement).Fingerprint();
            List<string> warnings = new List<string>();
            Application.LogCallback callback = (condition, stackTrace, type) =>
            {
                if (type == LogType.Warning && condition.Contains("dev.fallback_probe")) warnings.Add(condition);
            };
            Application.logMessageReceived += callback;
            try
            {
                using (PrototypeLocalization localization = new PrototypeLocalization())
                {
                    localization.SetLocale(PrototypeLocalization.EnglishLocaleCode, false);
                    string first = localization.Format("dev.fallback_probe");
                    string second = localization.Format("dev.fallback_probe");
                    string after = Capture(session, placement).Fingerprint();
                    bool passed = first == "한국어 폴백 확인" && second == first && warnings.Count == 1 && before == after;
                    File.WriteAllLines(Path.Combine(EvidenceFolder, "wave8-missing-key-fallback-log.txt"), new[]
                    {
                        "Wave 8 missing-key Korean fallback probe",
                        "Run ID: " + RunId,
                        "Result: " + (passed ? "PASS" : "FAIL"),
                        "First: " + first,
                        "Second: " + second,
                        "Matching warning count: " + warnings.Count,
                        "Warnings: " + Join(warnings),
                        "Progress state unchanged: " + (before == after)
                    }, new UTF8Encoding(false));
                    return new FallbackObservation
                    {
                        Passed = passed,
                        Actual = "first=" + first + " second=" + second + " warningCount=" + warnings.Count + " stateUnchanged=" + (before == after)
                    };
                }
            }
            finally
            {
                Application.logMessageReceived -= callback;
            }
        }

        private sealed class StartupObservation
        {
            internal bool Passed;
            internal string Actual;
        }

        private static StartupObservation VerifyStartupFallback()
        {
            using (PrototypeLocalization localization = new PrototypeLocalization())
            {
                string clean = localization.ResolveStartupLocale(PrototypeLocalization.KoreanLocaleCode);
                string invalid = localization.ResolveStartupLocale("xx-invalid");
                return new StartupObservation
                {
                    Passed = clean == PrototypeLocalization.KoreanLocaleCode && invalid == PrototypeLocalization.KoreanLocaleCode,
                    Actual = "clean=" + clean + " invalid=" + invalid
                };
            }
        }

        private static SourceTable ReadSourceTable()
        {
            string path = Path.Combine(ProjectRoot, SourceRelativePath);
            Require(File.Exists(path), "localization source exists");
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            Require(lines.Length > 1, "localization source has rows");
            SourceTable table = new SourceTable { Headers = lines[0].Split('\t') };
            table.KeyIndex = Array.FindIndex(table.Headers, value => value == "Key");
            table.KoIndex = Array.FindIndex(table.Headers, value => value == PrototypeLocalization.KoreanLocaleCode);
            table.EnIndex = Array.FindIndex(table.Headers, value => value == PrototypeLocalization.EnglishLocaleCode);
            table.QpsIndex = Array.FindIndex(table.Headers, value => value == QpsLongLocaleCode);
            foreach (string line in lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                table.Rows.Add(line.Split(new[] { '\t' }, StringSplitOptions.None));
            }
            return table;
        }

        private static string[] DuplicateKeys(SourceTable table)
        {
            return table.Rows.GroupBy(row => table.Value(row, table.KeyIndex), StringComparer.Ordinal)
                .Where(group => group.Count() > 1).Select(group => group.Key).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static string[] EmptyKeys(SourceTable table, int localeIndex)
        {
            if (localeIndex < 0) return table.Rows.Select(row => table.Value(row, table.KeyIndex)).ToArray();
            return table.Rows.Where(row => string.IsNullOrWhiteSpace(table.Value(row, localeIndex)))
                .Select(row => table.Value(row, table.KeyIndex)).ToArray();
        }

        private static string[] PlaceholderMismatches(SourceTable table, int leftIndex, int rightIndex)
        {
            if (leftIndex < 0 || rightIndex < 0) return new[] { "missing locale column" };
            return table.Rows.Where(row => !Placeholders(table.Value(row, leftIndex)).SetEquals(Placeholders(table.Value(row, rightIndex))))
                .Select(row => table.Value(row, table.KeyIndex)).ToArray();
        }

        private static string[] PositionalPlaceholderKeys(SourceTable table)
        {
            Regex positional = new Regex(@"\{\d+(?:[^{}]*)\}");
            return table.Rows.Where(row => Enumerable.Range(0, table.Headers.Length)
                    .Where(index => index != table.KeyIndex)
                    .Any(index => positional.IsMatch(table.Value(row, index))))
                .Select(row => table.Value(row, table.KeyIndex)).Distinct(StringComparer.Ordinal).ToArray();
        }

        private static int NamedPlaceholderCount(SourceTable table)
        {
            Regex named = new Regex(@"\{[A-Za-z_][A-Za-z0-9_.-]*(?:[^{}]*)\}");
            return table.Rows.Sum(row => Enumerable.Range(0, table.Headers.Length)
                .Where(index => index != table.KeyIndex)
                .Sum(index => named.Matches(table.Value(row, index)).Count));
        }

        private static HashSet<string> Placeholders(string value)
        {
            return new HashSet<string>(Regex.Matches(value ?? string.Empty, @"\{[^{}]+\}").Cast<Match>().Select(match => match.Value), StringComparer.Ordinal);
        }

        private static IEnumerable<string> Sequence(MatchCollection matches)
        {
            return matches.Cast<Match>().Select(match => match.Value);
        }

        private static string Decode(string value)
        {
            return (value ?? string.Empty).Replace("\\n", "\n");
        }

        private static void AddProduct(List<ContractCheck> checks, string id, string severity, string expected, bool passed,
            string actual, string reproduction, string recommendedFiles)
        {
            checks.Add(new ContractCheck
            {
                id = id,
                status = passed ? "PASS" : "FAIL",
                classification = passed ? "NONE" : "PRODUCT_DEFECT",
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = recommendedFiles
            });
        }

        private static void AddNotImplementedOrPass(List<ContractCheck> checks, string id, string severity, string expected, bool passed,
            string actual, string reproduction, string recommendedFiles)
        {
            checks.Add(new ContractCheck
            {
                id = id,
                status = passed ? "PASS" : "NOT_IMPLEMENTED",
                classification = passed ? "NONE" : "PRODUCT_GAP",
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = recommendedFiles
            });
        }

        private static void AddInfrastructure(List<ContractCheck> checks, string id, string severity, string expected, bool passed,
            string actual, string reproduction, string recommendedFiles)
        {
            checks.Add(new ContractCheck
            {
                id = id,
                status = passed ? "PASS" : "INFRA_FAIL",
                classification = passed ? "NONE" : "TEST_INFRASTRUCTURE",
                severity = severity,
                expected = expected,
                actual = actual,
                reproduction = reproduction,
                recommendedFiles = recommendedFiles
            });
        }

        private static ContractReport WriteReport(DateTime started, List<ContractCheck> checks)
        {
            ContractReport report = new ContractReport
            {
                runId = RunId,
                baselineCommit = BaselineCommit,
                unityVersion = Application.unityVersion,
                startedUtc = started.ToString("O"),
                completedUtc = DateTime.UtcNow.ToString("O"),
                command = string.Join(" ", Environment.GetCommandLineArgs().Select(Quote)),
                passed = checks.Count(check => check.status == "PASS"),
                failed = checks.Count(check => check.status == "FAIL"),
                notImplemented = checks.Count(check => check.status == "NOT_IMPLEMENTED"),
                unverified = checks.Count(check => check.status == "UNVERIFIED"),
                infrastructureFailed = checks.Count(check => check.status == "INFRA_FAIL"),
                checks = checks.ToArray()
            };
            report.productOverall = report.failed == 0 && report.notImplemented == 0 ? "PASS" : "FAIL";
            report.infrastructureOverall = report.infrastructureFailed == 0 ? "PASS" : "FAIL";
            report.overall = report.productOverall == "PASS" && report.infrastructureOverall == "PASS" ? "PASS" : "FAIL";
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave8-edit-contracts.json"), JsonUtility.ToJson(report, true) + Environment.NewLine, new UTF8Encoding(false));

            StringBuilder text = new StringBuilder();
            text.AppendLine("Wave 8 independent localization release Edit contracts");
            text.AppendLine("Run ID: " + report.runId);
            text.AppendLine("Baseline: " + report.baselineCommit);
            text.AppendLine("Unity: " + report.unityVersion);
            text.AppendLine("Product: " + report.productOverall);
            text.AppendLine("Infrastructure: " + report.infrastructureOverall);
            text.AppendLine("Counts: PASS=" + report.passed + " FAIL=" + report.failed + " NOT_IMPLEMENTED=" + report.notImplemented +
                            " INFRA_FAIL=" + report.infrastructureFailed + " UNVERIFIED=" + report.unverified);
            foreach (ContractCheck check in checks)
            {
                text.AppendLine(check.status + " · " + check.classification + " · " + check.severity + " · " + check.id + " · " + Normalize(check.actual));
            }
            File.WriteAllText(Path.Combine(EvidenceFolder, "wave8-edit-contracts.txt"), text.ToString(), new UTF8Encoding(false));
            return report;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Join(IEnumerable<string> values)
        {
            string[] items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return items.Length == 0 ? "none" : string.Join(" | ", items);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        }

        private static string Quote(string value)
        {
            return string.IsNullOrEmpty(value) || value.All(character => !char.IsWhiteSpace(character)) ? value : "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
