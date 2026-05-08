using UnityEngine;
using UnityEngine.UI;

/**
 *
 *  LightPadTouch manages light buttons.
 *  UpdatePadStatus() is called when the player touch the button. It informes the PadManager that the state must be changed.
 *  OnLightStatusChanged() is called by the PadManager when the status changed so the button UI must be updated
 *  
 **/

public class LightPadTouch : MonoBehaviour, LightPadManager.ILightManager
{

    [SerializeField] private LightPadManager padManager;
    [SerializeField] private int lightIndex = -1;
    [SerializeField] private Image image;
    [SerializeField] private Color disabledColor;
    [SerializeField] private Color onOffButtonEnabled;
    [SerializeField] private Color onOffButtonOn;
    [SerializeField] private Color movementButtonEnabled;
    [SerializeField] private Color movementButtonOn;
    [SerializeField] private LightPadManager.LightManagerType lightButtonType;

    bool isLightOn = false;
    bool isMovementOn = false;

    // Each button reference a light index, so that the PadManager knows which button controls each light,
    // and knows which buttons should be notified when a light state changed.
    public int LightIndex => lightIndex;

    // There are different kinds of light buttons : on/off, movement, intensity
    public LightPadManager.LightManagerType Type => lightButtonType;


    void Start()
    {
        if (!padManager)
            padManager = GetComponentInParent<LightPadManager>();

        if (!image)
            image = GetComponentInChildren<Image>(true);

        if (lightIndex != -1)
        {
            if (lightButtonType == LightPadManager.LightManagerType.OnOff)
                image.color = onOffButtonEnabled;
            else if (lightButtonType == LightPadManager.LightManagerType.Movement)
                image.color = disabledColor;
        }
    }

    // UpdatePadStatus() is called when the player touchs a button
    // It ask the LightPadManager to change the light status
    public void UpdatePadStatus()
    {
        if (lightIndex != -1)
        {
            if (lightButtonType == LightPadManager.LightManagerType.OnOff)
            {
                isLightOn = !isLightOn;

                // Stop movement if light is off
                if (!isLightOn)
                {
                    isMovementOn = false;
                }
                padManager.ChangeLightState(this, isLightOn);
            }
            else if (lightButtonType == LightPadManager.LightManagerType.Movement)
            {
                if (isLightOn)
                {
                    isMovementOn = !isMovementOn;
                    padManager.ChangeMovementState(this, isMovementOn);
                }
            }
        }
    }

    // OnAudioSourceStatusChanged is called by the LightPadManager in order to update the button colors if a remote player touch it
    public void OnLightStatusChanged(LightPadManager padManager, bool isLightEnabled, bool isMoving, float intensity)
    {
        if (isLightEnabled)
        {
            if (lightButtonType == LightPadManager.LightManagerType.OnOff)
            {
                image.color = onOffButtonOn;
            }
            else if (lightButtonType == LightPadManager.LightManagerType.Movement)
            {
                if (isMoving)
                    image.color = movementButtonOn;
                else
                    image.color = movementButtonEnabled;
            }
        }
        else
        {
            if (lightButtonType == LightPadManager.LightManagerType.OnOff)
            {
                image.color = onOffButtonEnabled;
            }
            else if (lightButtonType == LightPadManager.LightManagerType.Movement)
            {
                image.color = disabledColor;
            }
        }
        isLightOn = isLightEnabled;
        isMovementOn = isMoving;
    }
}
