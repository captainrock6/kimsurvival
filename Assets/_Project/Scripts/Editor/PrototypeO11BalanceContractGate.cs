using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KimSurvival.EditorTools
{
    public static class PrototypeO11BalanceContractGate
    {
        [MenuItem("Kim Survival/QA/Run O11 Balance Contracts")]
        public static void Run()
        {
            var failures = new List<string>();
            if (!PrototypeO11BalanceConfig.RunContractProbe(out string balanceDetail))
                failures.Add("O11 balance: " + balanceDetail);
            if (!PrototypeO7SearchBalance.RunContractProbe(out string searchDetail))
                failures.Add("O7 finite search: " + searchDetail);

            PrototypeContractProbe raft = PrototypeRaftRuntimeContract.VerifyAtomicFailureRetrySnapshotFixture();
            PrototypeContractProbe smoke = PrototypeEscapeProjectDirector.VerifyEscapeSmokeProgressCompleteFixture();
            PrototypeContractProbe radio = PrototypeEscapeProjectDirector.VerifyEscapeRadioProgressCompleteFixture();
            PrototypeContractProbe windows = PrototypeSignalEscapeWindowResolver.VerifyDeterministicRetryWindowContract();
            if (!raft.Success) failures.Add("raft atomic fixture: " + raft.Detail);
            if (!smoke.Success) failures.Add("smoke natural fixture: " + smoke.Detail);
            if (!radio.Success) failures.Add("radio natural fixture: " + radio.Detail);
            if (!windows.Success) failures.Add("signal window fixture: " + windows.Detail);

            if (failures.Count > 0)
                throw new InvalidOperationException("O11_BALANCE_FAIL\n" + string.Join("\n", failures));

            Debug.Log("O11_BALANCE_PASS | " + balanceDetail + " | " + searchDetail +
                      " | routes=raft,smoke,radio atomic-natural fixtures PASS");
        }
    }
}
