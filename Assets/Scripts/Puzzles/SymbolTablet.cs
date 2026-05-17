using UnityEngine;
using Fusion.XR.Shared.Grabbing;

public class SymbolTablet : MonoBehaviour
{
    public char symbol;
    private Grabbable grabbable;

    private void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        if (grabbable != null)
        {
            grabbable.onGrab.AddListener(OnGrab);
        }
    }

    private void OnGrab()
    {
        // When grabbed, if it was snapped to a slot, the slot should clear itself.
        // This is handled by the SymbolSlot listening or checking.
    }
}
