using UnityEngine;
using Fusion;
using TMPro;
using System.Collections;

public class PuzzleTimer : NetworkBehaviour
{
    [Networked] public float RemainingTime { get; set; } = 2700f; // 45 minutes
    [Networked] public NetworkBool IsTimerRunning { get; set; } = true;
    [Networked] public NetworkBool IsGameOver { get; set; } = false;

    public TextMeshProUGUI timerTextUGUI;
    public TextMeshPro timerTextWorld;

    public override void FixedUpdateNetwork()
    {
        if (IsGameOver) return;

        if (Object != null && Object.HasStateAuthority)
        {
            if (IsTimerRunning)
            {
                RemainingTime -= Runner.DeltaTime;
                if (RemainingTime <= 0)
                {
                    RemainingTime = 0;
                    IsTimerRunning = false;
                    IsGameOver = true;
                    RPC_OnTimerEnd();
                }
            }

            // Check if final puzzle is completed to stop timer
            if (PuzzleManager.Instance != null && PuzzleManager.Instance.puzzleSevenCompleted)
            {
                IsTimerRunning = false;
            }
        }
    }

    public override void Render()
    {
        string timeStr = FormatTime(RemainingTime);
        if (timerTextUGUI != null) timerTextUGUI.text = timeStr;
        if (timerTextWorld != null) timerTextWorld.text = timeStr;
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnTimerEnd()
    {
        Debug.Log("Time is up! Game Over.");
        
        var appManager = UnityEngine.Object.FindAnyObjectByType<Fusion.Samples.IndustriesComponents.ApplicationManager>();
        if (appManager != null)
        {
            var updateMethod = appManager.GetType().GetMethod("UpdateErrorMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cleanUpMethod = appManager.GetType().GetMethod("CleanUpScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (updateMethod != null)
            {
                updateMethod.Invoke(appManager, new object[] { "Time Up! You failed to escape the pyramid." });
            }

            if (cleanUpMethod != null)
            {
                appManager.StartCoroutine((IEnumerator)cleanUpMethod.Invoke(appManager, null));
            }
        }
        else
        {
            Application.Quit();
        }
    }
}
