using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * 
 * LightIntensitySlider asks the LightPadManager to change the light intensity when the player touch the slider.
 * Also, it update the slider position when a remote user touched the slider
 * 
 **/
public class LightIntensitySlider : MonoBehaviour, LightPadManager.ILightManager
{
    [SerializeField] private Slider slider;
    bool requestingValueChange = false;
    [SerializeField] private LightPadManager padManager;
    [SerializeField] private int lightIndex;

    public int LightIndex => lightIndex;

    public LightPadManager.LightManagerType Type => LightPadManager.LightManagerType.Intensity;


    private void Awake()
    {
        if (padManager == null) padManager = GetComponentInParent<LightPadManager>();
        if (slider == null) slider = GetComponentInChildren<Slider>();

        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    // OnSliderChanged is called when the local user touch the slider
    private void OnSliderChanged(float level)
    {
        if (requestingValueChange) return;
        RequestIntensityChange(level);
    }

    // Ask the LightPadManager to change the intensity
    void RequestIntensityChange(float intensity)
    {
        padManager.ChangeIntensity(LightIndex, intensity);
    }

    // Update the slider position
    void ChangeSliderValue(float value)
    {
        requestingValueChange = true;
        slider.value = value;
        requestingValueChange = false;
    }

    // OnLightStatusChanged is called by the LightPadManager when the light parameters change
    public void OnLightStatusChanged(LightPadManager lightPadManager, bool isEnable, bool isMoving, float intensity)
    {
        ChangeSliderValue(intensity);
    }
}
