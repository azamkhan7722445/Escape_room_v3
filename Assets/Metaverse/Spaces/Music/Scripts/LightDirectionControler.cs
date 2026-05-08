using UnityEngine;

/**
 * 
 * LightDirectionControler class controls the rotator script which rotates the light game object.
 * The LightDirectionControler component is enabled/disabled by the LightSystem
 * 
 **/
public class LightDirectionControler : MonoBehaviour
{

    [SerializeField] GameObject targetObject;
    [SerializeField] float rotationSpeed = 5f;
    [SerializeField] Rotator rotator;

    void Update()
    {
        Quaternion desiredRotation = Quaternion.LookRotation(targetObject.transform.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);
    }

    private void OnEnable()
    {
        if (rotator)
            rotator.enabled = true;
    }
    private void OnDisable()
    {
        if (rotator)
            rotator.enabled = false;
    }
}
