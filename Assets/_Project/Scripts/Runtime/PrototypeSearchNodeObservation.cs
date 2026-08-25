using System;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeSearchNodeLayoutObservation
    {
        public string Locale = string.Empty;
        public string Screenshot = string.Empty;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public int OverflowCount;
        public int OffscreenCount;
        public bool InsideScreen;
        public bool Compact;
        public bool PlayerClear;
        public bool WalkingBandClear;
        public string Result = string.Empty;
    }

    [Serializable]
    public sealed class PrototypeSearchNodePlayObservation
    {
        public string ObservationError = string.Empty;
        public string RegionId = string.Empty;
        public string NodeId = string.Empty;
        public string ContentsFingerprint = string.Empty;
        public string RemainingItemsFingerprint = string.Empty;
        public string[] InteractionTrace = Array.Empty<string>();
        public string[] RegionIds = Array.Empty<string>();
        public string[] ProtectedPartIds = Array.Empty<string>();
        public string[] StateSequence = Array.Empty<string>();
        public int FarPromptCount;
        public int NearPromptCount;
        public bool ActualNodeObserved;
        public bool TrayOpened;
        public bool PromptHiddenWhileTray;
        public bool PromptRestoredAfterCancel;
        public bool SameSeedSameNodeDeterministic;
        public bool DifferentSeedVaries;
        public bool CancelUnchanged;
        public bool ScreenTransitionUnchanged;
        public bool RevisitUnchanged;
        public bool SaveRestoreSame;
        public bool HiddenObserved;
        public bool PartialObserved;
        public bool DepletedObserved;
        public bool RemainingItemsRestored;
        public bool TakeAtomic;
        public bool LeaveAtomic;
        public bool ReplaceAtomic;
        public bool ReplaceCancelAtomic;
        public int DuplicateCostDelta;
        public bool ProtectedDiscardRejected;
        public int ProtectedDuplicateDelta;
        public int ProtectedDuplicateConsumeDelta;
        public bool SailclothLinked;
        public bool FiniteTotalResources;
        public bool BarrierPersistent;
        public bool PermanentHazardPersistent;
        public bool SearchCostAppliedOnce;
        public bool HazardExposureAppliedOnce;
        public bool SelectionPausesHazards;
        public bool Grant;
        public bool Warp;
        public bool Skip;
        public bool KeyboardMouseSyntheticGamepadParity;
        public string KeyboardMeaning = string.Empty;
        public string GamepadMeaning = string.Empty;
        public PrototypeSearchNodeLayoutObservation[] Layouts = Array.Empty<PrototypeSearchNodeLayoutObservation>();
    }
}
