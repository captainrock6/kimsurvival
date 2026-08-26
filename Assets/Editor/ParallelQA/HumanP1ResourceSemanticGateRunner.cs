using System;
using System.IO;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace KimSurvival.EditorQA
{
    public static class HumanP1ResourceSemanticGateRunner
    {
        [MenuItem("Kim Survival/QA/Human P1/Resource Semantic Contract")]
        public static void RunFromMenu()
        {
            if (!PrototypeResourcePresentation.RunContractProbe(out string detail))
            {
                throw new InvalidOperationException("Human P1 resource semantic contract failed: " + detail);
            }

            const string sourcePath = "Assets/_Project/Scripts/Localization/PrototypeStrings.tsv";
            string source = File.ReadAllText(sourcePath);
            foreach (PrototypeResourceVisual visual in PrototypeResourcePresentation.All)
            {
                if (!source.Contains(visual.StableResourceId + "\t", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Human P1 resource semantic contract is missing localized material: " +
                        visual.StableResourceId);
                }
            }

            if (!PrototypeCampModuleExpansionPresenter.RunContractProbe(out string campDetail))
            {
                throw new InvalidOperationException("Human P1 camp-module semantic contract failed: " + campDetail);
            }

            Debug.Log(
                "Human P1 integrated semantic contract: " + detail +
                " KO/EN/qps source keys present. " + campDetail);
        }
    }
}
