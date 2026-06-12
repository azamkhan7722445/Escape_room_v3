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

    [Header("Timer 3D Audio")]
    [Tooltip("Looping ticking sound. Plays as 3D spatial audio so it is only heard when a player is near the timer.")]
    public AudioClip tickClip;
    [Tooltip("AudioSource used for the ticking sound. If left empty, one is created automatically on this GameObject.")]
    public AudioSource tickAudioSource;
    [Tooltip("How loud the ticking sound is.")]
    [Range(0f, 1f)] public float tickVolume = 1f;
    [Tooltip("Distance (in meters) at which the ticking sound starts to fade out.")]
    public float tickMinDistance = 1.5f;
    [Tooltip("Distance (in meters) beyond which the ticking sound can no longer be heard.")]
    public float tickMaxDistance = 12f;

    public override void Spawned()
    {
        SetupTickAudio();
    }

    private void SetupTickAudio()
    {
        if (tickAudioSource == null)
            tickAudioSource = GetComponent<AudioSource>();
        if (tickAudioSource == null)
            tickAudioSource = gameObject.AddComponent<AudioSource>();

        // Prefer the explicitly assigned clip; otherwise keep whatever is already on the AudioSource.
        if (tickClip != null)
            tickAudioSource.clip = tickClip;
        tickAudioSource.loop = true;
        tickAudioSource.playOnAwake = false;
        tickAudioSource.volume = tickVolume;

        // 3D spatial sound: only audible when a player is near the timer.
        tickAudioSource.spatialBlend = 1f;
        tickAudioSource.rolloffMode = AudioRolloffMode.Linear;
        tickAudioSource.minDistance = tickMinDistance;
        tickAudioSource.maxDistance = tickMaxDistance;
        tickAudioSource.dopplerLevel = 0f;
    }

    private void UpdateTickAudio()
    {
        if (tickAudioSource == null) return;

        bool shouldPlay = IsTimerRunning && !IsGameOver && RemainingTime > 0;

        if (shouldPlay)
        {
            if (!tickAudioSource.isPlaying && tickAudioSource.clip != null)
                tickAudioSource.Play();
        }
        else
        {
            if (tickAudioSource.isPlaying)
                tickAudioSource.Stop();
        }
    }

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

        UpdateTickAudio();
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
        StartCoroutine(TimerEndSequenceCoroutine());
    }

    private IEnumerator TimerEndSequenceCoroutine()
    {
        var appManager = UnityEngine.Object.FindAnyObjectByType<Fusion.Samples.IndustriesComponents.ApplicationManager>();
        if (appManager != null)
        {
            var updateMethod = appManager.GetType().GetMethod("UpdateErrorMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (updateMethod != null)
            {
                updateMethod.Invoke(appManager, new object[] { "Time Up! Returning to Hub in 5 seconds..." });
            }

            if (appManager.desktopErrorMessageGO != null) appManager.desktopErrorMessageGO.SetActive(true);
            if (appManager.hardwareRigErrorMessageGO != null) appManager.hardwareRigErrorMessageGO.SetActive(true);
        }

        yield return new WaitForSeconds(5f);

        LoadHubScene();
    }

    private async void LoadHubScene()
    {
        Debug.Log("Loading Hub Scene...");

        var managers = Fusion.Samples.IndustriesComponents.Managers.FindInstance();
        var soundManager = Fusion.Addons.HapticAndAudioFeedback.SoundManager.FindInstance();

        if (managers != null)
        {
            if (managers.applicationManager != null)
            {
                managers.applicationManager.isQuitting = true;
            }

            if (soundManager != null)
            {
                soundManager.PlayOneShot("OnSceneSwitch");
            }

            if (Runner != null)
            {
                await Runner.Shutdown(true);
            }
            else if (managers.runner != null)
            {
                await managers.runner.Shutdown(true);
            }

            var spaceDescription = Fusion.Addons.Spaces.SpaceDescription.FindSpaceDescription("HubSpaceId");
            if (spaceDescription != null)
            {
                Fusion.Addons.Spaces.SpaceRoom.RegisterSpaceRequest(spaceDescription);
                UnityEngine.SceneManagement.SceneManager.LoadScene(spaceDescription.sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Hub", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
        else
        {
            if (Runner != null)
            {
                await Runner.Shutdown(true);
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Hub", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
