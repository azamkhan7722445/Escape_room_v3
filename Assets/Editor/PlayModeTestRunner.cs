using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 5);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 15.0f);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 100;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            switch (state)
            {
                case "WaitingForCompile":
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                        EditorApplication.isPlaying = true;
                    };
                    break;
                case "EnteringPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;
                case "InPlayMode":
                    if (EditorApplication.isPlaying) EditorApplication.update += WaitFramesThenRun;
                    break;
                case "Done":
                    Debug.Log(SentinelLog);
                    EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += WaitFramesThenRun;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;
            if (_testDone) return;

            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try { Setup(); }
                catch (System.Exception e) { FinishTest(true, e.Message); }
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;
            try
            {
                bool complete = Tick(elapsed);
                if (complete || timedOut) FinishTest(timedOut && !complete, timedOut ? "Timed out" : null);
            }
            catch (System.Exception e) { FinishTest(true, e.Message); }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;
            string resultJson = GetResult();
            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath))
                AssetDatabase.DeleteAsset(scriptPath);
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        [System.Serializable]
        private class TestResult
        {
            public bool success;
            public string error;
            public string[] logs;
            public string findings;
        }

        private static string _findings = "";

        private static void Setup()
        {
            Debug.Log("[Test] Starting Knob UI Diagnostic...");
            
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null) { Debug.LogError("[Test] EventSystem is MISSING!"); return; }
            Debug.Log("[Test] EventSystem found: " + es.name);

            var cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) { Debug.LogError("[Test] Camera is MISSING!"); return; }
            Debug.Log("[Test] Camera found: " + cam.name);

            // Find a Knob UI
            var digit1 = GameObject.Find("Large Chest/Digit 1");
            if (digit1 == null) { Debug.LogError("[Test] Large Chest/Digit 1 not found!"); return; }
            
            var knobUI = digit1.GetComponentInChildren<Canvas>(true);
            if (knobUI == null) { Debug.LogError("[Test] KnobUI Canvas not found under Digit 1!"); return; }
            Debug.Log("[Test] Inspecting " + knobUI.name + " on Digit 1");

            var raycaster = knobUI.GetComponent<GraphicRaycaster>();
            if (raycaster == null) Debug.LogWarning("[Test] KnobUI is missing GraphicRaycaster!");

            // Check if blocked
            var leftBtn = knobUI.transform.Find("Left");
            if (leftBtn == null) { Debug.LogError("[Test] Left button not found!"); return; }
            
            Vector3 worldPos = leftBtn.position;
            Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
            Debug.Log("[Test] Left Button Screen Pos: " + screenPos);

            PointerEventData pointerData = new PointerEventData(es);
            pointerData.position = screenPos;
            List<RaycastResult> results = new List<RaycastResult>();
            es.RaycastAll(pointerData, results);

            _findings += "Raycast results at " + screenPos + ":\n";
            foreach (var res in results)
            {
                _findings += " - Hit: " + res.gameObject.name + " (Layer: " + LayerMask.LayerToName(res.gameObject.layer) + ")\n";
            }

            if (results.Count == 0)
            {
                _findings += "WARNING: No UI hits at button position!\n";
            }
            else if (results[0].gameObject.name != "Left")
            {
                _findings += "WARNING: 'Left' button is BLOCKED by " + results[0].gameObject.name + "\n";
            }
            else
            {
                _findings += "SUCCESS: 'Left' button is at the top of the raycast stack.\n";
            }

            // Compare with EraseCanvas if possible
            var eraseCanvas = GameObject.Find("EraseCanvas");
            if (eraseCanvas != null)
            {
                _findings += "\nComparing with EraseCanvas:\n";
                var eraseRaycaster = eraseCanvas.GetComponent<GraphicRaycaster>();
                _findings += " - Raycaster: " + (eraseRaycaster != null ? "Yes" : "No") + "\n";
                _findings += " - Layer: " + LayerMask.LayerToName(eraseCanvas.layer) + "\n";
            }
        }

        private static bool Tick(float elapsed)
        {
            return elapsed >= 2.0f;
        }

        private static string GetResult()
        {
            return JsonUtility.ToJson(new TestResult
            {
                success = true,
                findings = _findings,
                logs = _capturedLogs.ToArray()
            });
        }
    }
}
