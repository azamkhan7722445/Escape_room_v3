using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    private Quaternion initialWorldRotation;

    void Start()
    {
        initialWorldRotation = transform.rotation;
    }

    void LateUpdate()
    {
        transform.rotation = initialWorldRotation;
    }
}
