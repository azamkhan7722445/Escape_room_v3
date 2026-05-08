using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * 
 * DJPadVolumeSlider asks the DJPadManager to change the volume when the player touch the slider.
 * Also, it update the slider position when a remote user touched the volume slider
 * 
 **/

public class DJPadVolumeSlider : MonoBehaviour, DJPadManager.IVolumeManager
{
    [SerializeField] private Slider slider;
    bool requestingValueChange = false;
    [SerializeField] private DJPadManager padManager;

    private void Awake()
    {
        if (padManager == null) padManager = GetComponentInParent<DJPadManager>();
        if (slider == null) slider = GetComponentInChildren<Slider>();

        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    // OnSliderChanged is called when the local user touch the slider
    private void OnSliderChanged(float level)
    {
        if (requestingValueChange) return;
        RequestVolumeChange(level);
    }

    // Ask the DJPadManager to change the volume
    void RequestVolumeChange(float volume)
    {
        padManager.ChangeVolume(volume);
    }

    // Update the slider position
    void ChangeSliderValue(float value)
    {
        requestingValueChange = true;
        slider.value = value;
        requestingValueChange = false;
    }

    // OnVolumeChanged is called by the DJPadManager when the volume changes
    public void OnVolumeChanged(DJPadManager bPMClipsPlayer, float volume)
    {
        ChangeSliderValue(volume);
    }
}
