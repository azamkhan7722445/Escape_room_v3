using Fusion;
using Fusion.XR.Shared;
using System.Collections.Generic;
using UnityEngine;

/**
 * 
 * EffectSystem is an abstract class that every system changing the object parameters must implement
 * 
 **/
public abstract class EffectSystem : MonoBehaviour
{
    public abstract void ChangeState(bool isEnable, bool isMoving, float intensity);
}

/**
 * 
 *  LightPadManager references all lights object (LightInfo). This must be done in Unity Editor.
 *  
 *  At start, it registers all light buttons into a dictionnary.
 *  
 *  When the local player push a button, the LightPadManager is informed by ChangeLightState()/ChangeMovementState()/ChangeIntensity()
 *  Then : 
 *      - the associated networked dictionnary is updated,
 *      - thanks to Fusion callback, local & remote players can update lights & UI with Refresh() 
 *      
 *  When a player joins, its lights are updated with the networked dictionnaries.
 *  
 **/

public class LightPadManager : NetworkBehaviour
{

    // Interface that each light button must implement
    public interface ILightManager
    {
        void OnLightStatusChanged(LightPadManager lightPadManager, bool isEnable, bool isMoving, float intensity);
        int LightIndex { get; }
        LightManagerType Type { get; }
    }

    // There are several light buttons 
    public enum LightManagerType
    {
        OnOff,              // switch on/off the light
        Movement,           // switch on/off the movement
        Intensity           // slider to change the intensity
    }

    // Each light has an index and an effect system that can modify the light parameters
    [System.Serializable]
    public struct LightInfo
    {
        public int lightIndex;
        public EffectSystem effectSystem;
    }

    // we have a dedicated dictionnary for each kind of button
    [SerializeField] private Dictionary<int, ILightManager> lightActivationManagers = new Dictionary<int, ILightManager>();
    [SerializeField] private Dictionary<int, ILightManager> lightMovementManagers = new Dictionary<int, ILightManager>();
    [SerializeField] private Dictionary<int, ILightManager> lightIntensityManagers = new Dictionary<int, ILightManager>();

    // List & dictionnary of all light objects (LightInfo)
    [SerializeField] private List<LightInfo> lightInfos = new List<LightInfo>();
    private Dictionary<int, LightInfo> lightInfoByIndex = new Dictionary<int, LightInfo>();

    const int MAX_LIGHTS = 10;

    // local copy of light parameters
    bool isActivated = false;
    bool isMoving = false;
    float intensity = 1f;

    float lastIntensityRequest;


    // The on/off status of each light is recorded into a networked dictionnary, so OnLightStatusChanged() is called an all clients when the dictionnary changed
    [Networked]
    [Capacity(MAX_LIGHTS)]
    public NetworkDictionary<int, NetworkBool> LightStatus { get; }

    // The movement status of each light is recorded into a networked dictionnary, so OnLightMovementStatusChanged() is called an all clients when the dictionnary changed
    [Networked]
    [Capacity(MAX_LIGHTS)]
    public NetworkDictionary<int, NetworkBool> LightMovementStatus { get; }

    // The intensity of each light is recorded into a networked dictionnary, so OnLightIntensityChanged() is called an alls client when the dictionnary changed
    [Networked]
    [Capacity(MAX_LIGHTS)]
    public NetworkDictionary<int, float> LightIntensities { get; }

    ChangeDetector changeDetector;

    void Awake()
    {
        // all lights are added into a dictionnary at startup to facilitate data manipulation
        foreach (var lightInfo in lightInfos)
        {
            lightInfoByIndex[lightInfo.lightIndex] = lightInfo;
        }

        // Find all ILightManager (buttons) and save them into a local dedicated dictionnary
        foreach (var manager in GetComponentsInChildren<ILightManager>(true))
        {
            if (!lightInfoByIndex.ContainsKey(manager.LightIndex))
            {
                Debug.LogError($"Unknown light {manager.LightIndex}");
                continue;
            }

            // each button type has a dedicated dictionnary
            switch (manager.Type)
            {
                case LightManagerType.OnOff:
                    lightActivationManagers[manager.LightIndex] = manager;
                    break;

                case LightManagerType.Movement:
                    lightMovementManagers[manager.LightIndex] = manager;
                    break;

                case LightManagerType.Intensity:
                    lightIntensityManagers[manager.LightIndex] = manager;
                    break;
            }
        }
    }

    // We have to update the local lights when a player join the room
    public override void Spawned()
    {
        base.Spawned();

        // Synchronise the local lights parameters according to the networked dictionnary
        foreach (var lightIndex in lightInfoByIndex.Keys)
        {
            UpdateLightsAndButtons(lightIndex);
        }

        changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        Refresh();
    }


    public override void Render()
    {
        base.Render();
        foreach (var changedVar in changeDetector.DetectChanges(this))
        {
            if (changedVar == nameof(LightStatus))
            {
                Refresh();
            }

            if (changedVar == nameof(LightMovementStatus))
            {
                Refresh();
            }

            if (changedVar == nameof(LightIntensities))
            {
                Refresh();
            }
        }
    }

    // GetLightsParameters is in charge to save the networked light parameters into local variables according to the lightIndex provided in parameter
    void GetLightsParameters(int lightIndex)
    {
        isActivated = LightStatus.ContainsKey(lightIndex) ? LightStatus[lightIndex] : false;
        isMoving = LightMovementStatus.ContainsKey(lightIndex) ? LightMovementStatus[lightIndex] : false;
        intensity = LightIntensities.ContainsKey(lightIndex) ? LightIntensities[lightIndex] : 1f;
    }

    // ChangeLightState is called by a button to turn off or on the light
    public async void ChangeLightState(LightPadTouch lightPadTouch, bool isLightOn)
    {
        if (!Object.HasStateAuthority)
        {
            await Object.WaitForStateAuthority();
        }

        // update network status
        LightStatus.Set(lightPadTouch.LightIndex, isLightOn);

        // movement must be stopped if the light is off
        if (!isLightOn)
            LightMovementStatus.Set(lightPadTouch.LightIndex, false);
    }

    // ChangeLightState is called by a button to start/stop the light movement
    public async void ChangeMovementState(LightPadTouch lightPadTouch, bool isMovementOn)
    {
        if (!Object.HasStateAuthority)
        {
            await Object.WaitForStateAuthority();
        }
        // update network status
        LightMovementStatus.Set(lightPadTouch.LightIndex, isMovementOn);
    }



    // ChangeIntensity is called by a light slider to change the light intensity
    public async void ChangeIntensity(int lightIndex, float intensity)
    {
        // We use an attribute, so if another intensity is requested while taking the authority, the last intensity request is the one executed
        lastIntensityRequest = intensity;
        if (!Object.HasStateAuthority)
        {
            await Object.WaitForStateAuthority();
        }
        LightIntensities.Set(lightIndex, lastIntensityRequest);
    }

    // OnStatusChanged is in charge to inform buttons that light parameters has changed (to update the UI)
    void OnStatusChanged(int lightIndex)
    {

        if (lightActivationManagers.ContainsKey(lightIndex))
        {
            lightActivationManagers[lightIndex].OnLightStatusChanged(this, isActivated, isMoving, intensity);
        }

        if (lightMovementManagers.ContainsKey(lightIndex))
        {
            lightMovementManagers[lightIndex].OnLightStatusChanged(this, isActivated, isMoving, intensity);
        }

        if (lightIntensityManagers.ContainsKey(lightIndex))
        {
            lightIntensityManagers[lightIndex].OnLightStatusChanged(this, isActivated, isMoving, intensity);
        }
    }

    // Refresh is in charge to update all lights and associated buttons
    void Refresh()
    {
        // browse the list of all lights
        foreach (var light in LightStatus)
        {
            var lightIndex = light.Key;
            UpdateLightsAndButtons(lightIndex);
        }

    }

    // UpdateLightObject is in charge to ask the effectSystem of each light to update the light's properties according to the new parameters
    void UpdateLightObject(int lightManagerIndex, bool isLightEnable, bool isMovementEnable, float intensity)
    {
        if (lightInfoByIndex.ContainsKey(lightManagerIndex))
        {
            var lightInfo = lightInfoByIndex[lightManagerIndex];
            lightInfo.effectSystem.ChangeState(isLightEnable, isMovementEnable, intensity);
        }
    }

    // UpdateLightsAndButtons updates the local copy of light paramters, then ask to update the light and the UI
    void UpdateLightsAndButtons(int lightIndex)
    {
        // get light parameters
        GetLightsParameters(lightIndex);

        // update the lights objects
        UpdateLightObject(lightIndex, isActivated, isMoving, intensity);

        // inform buttons managing this light to refresh the UI
        OnStatusChanged(lightIndex);
    }
}
