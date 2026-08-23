using UnityEngine;

namespace KimSurvival
{
    public sealed class PrototypeCampPromptSkin : ScriptableObject
    {
        [SerializeField] private string assetId = string.Empty;
        [SerializeField] private Sprite frame;

        public string AssetId
        {
            get { return assetId; }
        }

        public Sprite Frame
        {
            get { return frame; }
        }

        public void Configure(string configuredAssetId, Sprite configuredFrame)
        {
            assetId = configuredAssetId ?? string.Empty;
            frame = configuredFrame;
        }
    }
}
