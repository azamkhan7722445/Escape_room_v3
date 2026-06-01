using UnityEngine;
using UnityEngine.UI;

public class KnobUIController : MonoBehaviour
{
    public Button leftButton;
    public Button rightButton;
    public LargeChestKnob knob;

    private void Start()
    {
        // If not assigned, try to find the knob by proximity or name
        if (knob == null)
        {
            knob = GetComponentInParent<LargeChestKnob>();
            if (knob == null)
            {
                // Try searching children of the chest if we are a child of the chest
                var chest = transform.GetComponentInParent<LargeChestController>();
                if (chest != null && chest.knobs != null)
                {
                    float minDist = float.MaxValue;
                    foreach (var k in chest.knobs)
                    {
                        if (k == null) continue;
                        float dist = Vector3.Distance(transform.position, k.transform.position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            knob = k;
                        }
                    }
                }
            }
        }
        
        if (leftButton != null && knob != null) leftButton.onClick.AddListener(() => knob.RotateRight());
        if (rightButton != null && knob != null) rightButton.onClick.AddListener(() => knob.RotateLeft());
    }
}
