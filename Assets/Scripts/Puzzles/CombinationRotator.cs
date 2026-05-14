using UnityEngine;
using Fusion.XR.Shared.Grabbing;

public class CombinationRotator : MonoBehaviour
{
    public int digitIndex;
    public float snapAngle = 36f; // 10 digits = 36 degrees each
    public int currentValue = 0;

    private void Update()
    {
        // Detect current digit based on local X rotation (cylinder rolling)
        float angle = transform.localEulerAngles.x;
        int newValue = Mathf.RoundToInt(angle / snapAngle) % 10;
        if (newValue < 0) newValue += 10;

        if (newValue != currentValue)
        {
            currentValue = newValue;
        }
    }
}
