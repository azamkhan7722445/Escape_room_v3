using UnityEngine;
using UnityEngine.UI;

/**
 * 
 * DJPadTouch is in charge to inform the DJPadManager when a user touchs a key.
 * The DJPadManager calls the OnAudioSourceStatusChanged method to update the button color if a remote player touch it
 * 
 **/

public class DJPadTouch : MonoBehaviour, DJPadManager.IAudioSourceManager
{
    [SerializeField] private DJPadManager padManager;
    [SerializeField] private int audiopadNumber;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Image image;
    [SerializeField] private Color disabledColor;
    [SerializeField] private Color enabledColor;
    [SerializeField] private Color enabledColorForLoop;
    [SerializeField] private Color playingColor;
    [SerializeField] private Color playingColorForLoop;

    [SerializeField] private Slider slider;
    [SerializeField] private float audioClipDuration = 0f;

   bool isPlaying = false;

    // Method to get the audio source index
    public int AudioSourceIndex { 
        get
        {
            return audiopadNumber;
        }
    }

    // Method to get the audio source
    public AudioSource AudioSource
    {
        get
        {
            return audioSource;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!padManager)
            padManager = GetComponentInParent<DJPadManager>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);
        if (!image)
            image = GetComponentInChildren<Image>(true);
        if (!slider)
            slider = GetComponentInChildren<Slider>(true);
        if (slider)
            slider.value = 0f;

        if (audioSource.clip)
        {
            if (audioSource.loop)
                image.color = enabledColorForLoop;
            else
                image.color = enabledColor;

            audioClipDuration = audioSource.clip.length;
            
        }
    }

    // UpdatePadStatus() is called when the player touchs a button
    // It ask the DJPadManager to change the audio source status
    public void UpdatePadStatus()
    {
        if (audioSource.clip)
        { 
            isPlaying = !isPlaying;
            padManager.ChangeAudioSourceState(this, isPlaying);
        }
    }

    // OnAudioSourceStatusChanged is called by the DJPadManager in order to update the button colors if a remote player touch it
    public void OnAudioSourceStatusChanged(DJPadManager padManager, bool isEnabled)
    {
        if (isEnabled)
        {
            if (audioSource.loop)
                image.color = playingColorForLoop;
            else
                image.color = playingColor;
        }
        else
        {
            if (audioSource.loop)
                image.color = enabledColorForLoop;
            else
                image.color = enabledColor;
        }
        isPlaying = isEnabled;
    }

    private void Update()
    {
        UpdateSlider();
    }

    bool sliderWasReset= false;
    
    // Update the progress bar located at the bottom of the button
    private void UpdateSlider()
    {
        if (slider && isPlaying)
        {
            sliderWasReset = false;
            slider.value = audioSource.time / audioClipDuration;
        }

        if (audioSource.time == audioClipDuration && !sliderWasReset )
        {
            slider.value = 0;
            sliderWasReset = true;
        }
    }
}


