using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    public Quaternion initialWorldRotation;

    void Awake()
    {
        initialWorldRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        transform.rotation = initialWorldRotation;
    }

    void LateUpdate()
    {
        transform.rotation = initialWorldRotation;
    }
}
