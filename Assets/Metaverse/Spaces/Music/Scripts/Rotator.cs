using UnityEngine;

/**
 * 
 * The purpose of this class is to rotate an object to a certain angle and then rotate it in the other direction
 * 
 **/

public class Rotator : MonoBehaviour
{
    [SerializeField] Vector3 rotationSpeed;
    private int rotationDirection = 1;
    [SerializeField] float rotationAngle = 90f;
    private Quaternion startingRotation;

    private void Awake()
    {
        this.enabled = false;
    }


    void Start()
    {
        // Get the starting rotation of the object
        startingRotation = transform.rotation;
    }

    public void FixedUpdate()
    {
        // Rotate the object
        transform.Rotate(rotationSpeed * Time.fixedDeltaTime * rotationDirection);

        // Get the current rotation of the object
        Quaternion currentRotation = transform.rotation;

        // Check if the current rotation is equal to the target rotation
        if (Quaternion.Angle(startingRotation, currentRotation) >= rotationAngle)
        {
            // Change the rotation direction
            rotationDirection *= -1;
            startingRotation = transform.rotation;
        }
    }
}
