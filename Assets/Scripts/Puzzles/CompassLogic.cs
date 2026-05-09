using UnityEngine;

public class CompassLogic : MonoBehaviour
{
    [Header("References")]
    public Transform needle;
    
    [Header("Settings")]
    public Vector3 northDirection = Vector3.forward; // Unity +Z is North

    void Update()
    {
        if (needle == null) return;

        // In a simple compass, the needle always points to world North.
        // We need the needle's rotation to align with the northDirection in world space,
        // but it's constrained to its local axis (usually Y).
        
        Vector3 worldNorth = northDirection;
        // Project onto the plane of the compass if necessary, 
        // but for a simple escape room, pointing at world Z is usually enough.
        
        Quaternion targetRot = Quaternion.LookRotation(worldNorth, transform.up);
        needle.rotation = targetRot;
    }
}
