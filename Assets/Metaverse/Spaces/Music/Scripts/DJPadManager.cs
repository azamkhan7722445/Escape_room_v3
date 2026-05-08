using Fusion;
using Fusion.Samples.IndustriesComponents;
using Fusion.XR.Shared;
using System.Collections.Generic;
using UnityEngine;

/**
 * 
 * DJPadManager manages the overall function of the music pad.
 * 
 * At start, it registers all pad buttons into a dictionnary.
 *  
 *  When the local player push a button, the DJPadManager is informed by ChangeAudioSourceState()
 *  Then the networked dictionnary PadsStatus is updated,
 *  
 *  Audio sources and button UI are updated on a regular basis with RefreshPads(), SyncAudioSource() & OnAudioStatusChanged() methods
 *  
 **/

public class DJPadManager : NetworkBehaviour
{
    [Networked]
    public float MasterVolume { get; set; } = 1;

    [Range(0f, 1f)]
    public float localMasterVolume = 1;

    [Networked]
    [Capacity(50)]
    public NetworkDictionary<int, NetworkBool> PadsStatus { get; }

    [SerializeField] private Dictionary<int, IAudioSourceManager> audioSourceManagers = new Dictionary<int, IAudioSourceManager>();
    [SerializeField] private IVolumeManager volumeManager;

    Dictionary<AudioSource, bool> audioSourceRecentlyEnabled = new Dictionary<AudioSource, bool>();

    [SerializeField] private int bpm = 123;
    [SerializeField] private int audioFramesBetweencheck = 5;

    [SerializeField] private AudioSettingsManager audioSettingsManager;

    bool looping = false;

    ChangeDetector changeDetector;
    public interface IAudioSourceManager {
        void OnAudioSourceStatusChanged(DJPadManager bPMClipsPlayer, bool status);
        int AudioSourceIndex { get; }
        AudioSource AudioSource { get; }
    }

    public interface IVolumeManager
    {
        void OnVolumeChanged(DJPadManager bPMClipsPlayer, float volume);
    }

    void Awake()
    {
        if (volumeManager == null) volumeManager = GetComponentInChildren<IVolumeManager>();

        foreach (var manager in GetComponentsInChildren<IAudioSourceManager>(true))
        {
            audioSourceManagers[manager.AudioSourceIndex] = manager;
        }

        if (audioSettingsManager == null)
        {
            audioSettingsManager = FindObjectOfType<AudioSettingsManager>(true);
            if (audioSettingsManager == null)
            {
                Debug.LogError("Audio Settings Manager not set");
                return;
            }
        }
    }

    #region Interface
    public async void ChangeAudioSourceState(IAudioSourceManager audioSourceManager, bool isPlaying)
    {
        if (!Object.HasStateAuthority)
        {
            await Object.WaitForStateAuthority();
        }

        PadsStatus.Set(audioSourceManager.AudioSourceIndex, isPlaying);
    }

    float lastVolumeRequest;
    public async void ChangeVolume(float volume)
    {
        // We use an attribute, so if another volume is requested while taking the authority, the last volume request is the one executed
        lastVolumeRequest = volume;
        if (!Object.HasStateAuthority)
        {
            await Object.WaitForStateAuthority();
        }

        MasterVolume = lastVolumeRequest;
    }
    #endregion

    private void OnDestroy()
    {
        looping = false;
    }
    public override void Spawned()
    {
        base.Spawned();
        AudioLoop();

        changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        OnMasterVolumeChanged();
    }

    public override void Render()
    {
        base.Render();
        foreach (var changedVar in changeDetector.DetectChanges(this))
        {
            if (changedVar == nameof(MasterVolume))
            {
                OnMasterVolumeChanged();
            }
        }
    }

    // AudioLoop checks the pad status on a regular basis (BPM)
    async private void AudioLoop()
    {
        int checkDelay = (int)(audioFramesBetweencheck * 1000f / bpm);
        looping = true;
        while (looping && enabled)
        {
            RefreshPads();
            await AsyncTask.Delay(checkDelay);
        }
    }

    // RefreshPads, for each key, is in charge to :
    //  - update the associated audiosource according to the volume slider value
    //  - update the pad dictionnary if the key status has changed 
    //  - inform the key about the new status in order to update the key colors
    void RefreshPads()
    {

        foreach (var status in PadsStatus)
        {
            if (!audioSourceManagers.ContainsKey(status.Key))
            {
                Debug.LogError($"status.Key : {status.Key} not found");
                continue;
            }

            var manager = audioSourceManagers[status.Key];
            var source = manager.AudioSource;
            source.volume = MasterVolume * localMasterVolume;
            audioSettingsManager.SetMusicVolume(source.volume);

            var wasAudioSourcePlaying = audioSourceRecentlyEnabled.ContainsKey(source) ? audioSourceRecentlyEnabled[source] : false;
            var audioSourceNetworkCommand = status.Value;
            var newAudioSourceNetworkCommand = SyncAudioSource(source, wasAudioSourcePlaying, audioSourceNetworkCommand);

            // Cache current state
            audioSourceRecentlyEnabled[source] = newAudioSourceNetworkCommand;

            // To check if audio clip has finished
            if (newAudioSourceNetworkCommand != audioSourceNetworkCommand && Object.HasStateAuthority)
            {
                // update network status
                PadsStatus.Set(status.Key, newAudioSourceNetworkCommand);
            }

            // Check if audioSource status must be changed
            if (newAudioSourceNetworkCommand != wasAudioSourcePlaying)
            {
                OnAudioStatusChanged(status.Key, newAudioSourceNetworkCommand);
            }
        }
    }

    // OnAudioStatusChanged is in charge to inform the key about the new status in order to update the key colors
    void OnAudioStatusChanged(int padTouchId, bool status)
    {
        //Debug.Log("OnAudioStatusChanged for  : " + padTouchId + " new status=" + status);
        audioSourceManagers[padTouchId].OnAudioSourceStatusChanged(this,status);
    }

    // SyncAudioSource is in charge to play or stop the audiosource according to :
    // - the previous status
    // - the new value
    // - the loop parameter set on each audio source
    private bool SyncAudioSource(AudioSource source, bool wasValid, bool valid)
    {
        if (valid && source.isPlaying == false && source.loop)
        {
            // Start loop
            if (!source.gameObject.activeSelf)
                source.gameObject.SetActive(true);
            source.Play();
        }
        if (valid && source.isPlaying == false && source.loop == false && wasValid == false)
        {
            // Start one shot sound
            if (!source.gameObject.activeSelf)
                source.gameObject.SetActive(true);
            source.Play();
        }
        else if (valid && source.isPlaying == false && source.loop == false && wasValid == true)
        {
            // Disable a one shot once which is now finished
            valid = false;
        }
        else if (valid == false && source.isPlaying)
        {
            // Stop a playing sound
            source.Stop();
            if (source.gameObject.activeSelf)
                source.gameObject.SetActive(false);
        }
        return valid;
    }

    // OnMasterVolumeChanged is called when the volume changed
    // Then it notifies the slider in order to update the slider position
    void OnMasterVolumeChanged()
    {
        if(volumeManager != null) volumeManager.OnVolumeChanged(this, MasterVolume);
    }

}
