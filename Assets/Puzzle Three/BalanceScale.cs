using UnityEngine;

public class BalanceScale : MonoBehaviour
{
    [Header("Components")]
    public Transform beam;
    public ScalePan leftPan;
    public ScalePan rightPan;

    [Header("Settings")]
    public float maxTiltAngle = 30f;
    public float sensitivity = 2f;
    public float lerpSpeed = 2f;

    [Header("Solution")]
    public float balancedTolerance = 0.1f;
    public ChestLock chestLock;
    public int lockDigitIndex = 2; // 3rd digit (0-indexed)

    private float targetTilt = 0f;
    private float currentTilt = 0f;

    void Update()
    {
        float leftWeight = leftPan != null ? leftPan.TotalWeight : 0;
        float rightWeight = rightPan != null ? rightPan.TotalWeight : 0;

        float weightDiff = leftWeight - rightWeight;
        // Invert weightDiff if necessary depending on axis. 
        // If left > right, tilt towards left (positive or negative Z?)
        targetTilt = Mathf.Clamp(weightDiff * sensitivity, -maxTiltAngle, maxTiltAngle);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * lerpSpeed);
        
        if (beam != null)
        {
            beam.localRotation = Quaternion.Euler(0, 0, currentTilt);
            
            // Keep pans upright
            if (leftPan != null) leftPan.transform.rotation = Quaternion.identity;
            if (rightPan != null) rightPan.transform.rotation = Quaternion.identity;
        }

        // Check if balanced
        if (Mathf.Abs(weightDiff) < balancedTolerance && leftWeight > 29f && rightWeight > 29f)
        {
            int heartCountOnLeft = 0;
            foreach(var obj in leftPan.objectsOnPan)
            {
                if(obj.objectType == "Heart") heartCountOnLeft++;
            }

            // Clue: 5 hearts on left = number 5
            if(heartCountOnLeft == 5)
            {
                if (chestLock != null)
                {
                    chestLock.SetDigit(lockDigitIndex, 5);
                }
            }
        }
    }
}
