using UnityEngine;

namespace KimSurvival
{
    public sealed class PrototypeWave18PresentationAssets : ScriptableObject
    {
        [SerializeField] private Texture2D hazardPhaseAtlas;
        [SerializeField] private Sprite escapeProjectFrame;
        [SerializeField] private Sprite endingComicFrame;

        public Texture2D HazardPhaseAtlas { get { return hazardPhaseAtlas; } }
        public Sprite EscapeProjectFrame { get { return escapeProjectFrame; } }
        public Sprite EndingComicFrame { get { return endingComicFrame; } }
        public bool IsSelectedOnlyComplete
        {
            get { return hazardPhaseAtlas != null && escapeProjectFrame != null && endingComicFrame != null; }
        }
    }
}
