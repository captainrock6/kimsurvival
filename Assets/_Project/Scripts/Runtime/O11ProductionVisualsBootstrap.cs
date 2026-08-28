using UnityEngine;

namespace KimSurvival
{
    [DefaultExecutionOrder(32000)]
    public sealed class O11ProductionVisualsBootstrap : MonoBehaviour
    {
        private KimSurvivalPrototype prototype;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindAnyObjectByType<O11ProductionVisualsBootstrap>() != null)
            {
                return;
            }

            var root = new GameObject("O11 Production Visuals · adopted V2 grammar");
            DontDestroyOnLoad(root);
            root.AddComponent<O11ProductionVisualsBootstrap>();
        }

        private void LateUpdate()
        {
            if (prototype == null)
            {
                prototype = Object.FindAnyObjectByType<KimSurvivalPrototype>();
            }

            if (prototype != null)
            {
                prototype.RefreshO11ProductionVisuals();
            }
        }
    }
}
