using UnityEngine;

/**
 * 
 * The LightSystem class inherits from the abstract class EffectSystem.
 * LightSystem is in charge to change the light state according to the parameters received in ChangeState method :
 *      - light enabled (on/off)
 *      - light movement enabled (on/off)
 *      - light intensity (float)
 * 
 **/

public class LightSystem : EffectSystem
{
    [SerializeField] private Light controlledLight;
    [SerializeField] private float maxIntensity;
    [SerializeField] private float maxGlassIntensity;
    [SerializeField] private LightDirectionControler lightDirectionControler;
    [SerializeField] private GameObject lightGlass;

    private Material lightGlassMaterial;

    protected virtual void Awake()
    {
        if(!controlledLight)
        {
            controlledLight = GetComponent<Light>();
        }

        if (!lightDirectionControler)
        {
            lightDirectionControler = GetComponent<LightDirectionControler>();
        }

        if (!lightGlass)
        {
            Debug.LogError("lightGlass has not been set");
        }
        else
        {
            lightGlassMaterial = lightGlass.GetComponent<MeshRenderer>().material;
            lightGlassMaterial.SetColor("_EmissionColor", controlledLight.color);
        }

    }
    public override void ChangeState(bool isEnable, bool isMoving, float intensity)
    {
        controlledLight.enabled= isEnable;
        lightGlass.SetActive(isEnable);
        controlledLight.intensity = intensity * maxIntensity;
        lightGlassMaterial.SetColor("_EmissionColor", controlledLight.color * intensity * maxGlassIntensity);
        lightDirectionControler.enabled = isMoving;
    }
}
