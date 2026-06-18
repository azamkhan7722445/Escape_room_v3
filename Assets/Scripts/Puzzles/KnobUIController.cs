using UnityEngine;
using UnityEngine.UI;

public class KnobUIController : MonoBehaviour
{
    public Button leftButton;
    public Button rightButton;
    public LargeChestKnob knob;

    private void Awake()
    {
        FindKnob();
    }

    private void Start()
    {
        FindKnob();
    }

    private void FindKnob()
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
    }

    private void OnEnable()
    {
        FindKnob();
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(OnLeftButtonClicked);
            leftButton.onClick.AddListener(OnLeftButtonClicked);
        }
        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(OnRightButtonClicked);
            rightButton.onClick.AddListener(OnRightButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(OnLeftButtonClicked);
        }
        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(OnRightButtonClicked);
        }
    }

    private void OnLeftButtonClicked()
    {
        if (knob != null)
        {
            knob.RotateRight();
        }
    }

    private void OnRightButtonClicked()
    {
        if (knob != null)
        {
            knob.RotateLeft();
        }
    }
}
