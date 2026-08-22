// Forge 0.4 - deterministic Game View capture helper.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Forge.Editor
{
    internal static class ForgeCapture
    {
        [MenuItem("Forge/Capture Game View")]
        public static void CaptureGameView()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Forge Capture", "동일한 게임 상태를 재현한 뒤 Play Mode에서 캡처해 주세요.", "확인");
                return;
            }
            CaptureForAutomation();
        }

        public static void CaptureForAutomation()
        {
            string folder = Path.Combine(Application.dataPath, "_Project", "Art", "ForgeCaptures");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "capture-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log("Forge capture queued: " + file);
        }
    }
}
