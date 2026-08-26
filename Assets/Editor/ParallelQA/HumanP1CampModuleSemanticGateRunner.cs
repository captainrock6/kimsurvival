using System;
using KimSurvival;
using UnityEditor;
using UnityEngine;

namespace KimSurvival.EditorQA
{
    public static class HumanP1CampModuleSemanticGateRunner
    {
        [MenuItem("Kim Survival/QA/Human P1/Camp Module Semantic Contract")]
        public static void RunFromMenu()
        {
            if (!PrototypeCampModuleExpansionPresenter.RunContractProbe(out string detail))
            {
                throw new InvalidOperationException("Human P1 camp-module semantic contract failed: " + detail);
            }

            Debug.Log("Human P1 camp-module semantic contract: " + detail);
        }
    }
}
