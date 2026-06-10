using UnityEngine;
using Fusion.XR.Shared.Grabbing;

public enum CanopicJarType
{
    Imsety,     // Liver
    Hapi,       // Lungs
    Duamutef,   // Stomach
    Qebehsenuef // Intestines
}

public class CanopicJar : MonoBehaviour
{
    public CanopicJarType jarType;

    private Grabbable      grabbable;
    private Rigidbody      rb;
    private CanopicJarSlot currentSlot;

    private void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        rb        = GetComponent<Rigidbody>();

        if (grabbable != null)
            grabbable.onUngrab.AddListener(OnUngrab);
    }

    private void OnDestroy()
    {
        if (grabbable != null)
            grabbable.onUngrab.RemoveListener(OnUngrab);
    }

    // ── Trigger tracking ──────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        CanopicJarSlot slot = other.GetComponentInParent<CanopicJarSlot>();
        if (slot != null)
        {
            currentSlot = slot;
            Debug.Log($"[CanopicJar:{name}] Entered slot trigger: {slot.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CanopicJarSlot slot = other.GetComponentInParent<CanopicJarSlot>();
        if (slot != null && slot == currentSlot)
        {
            currentSlot = null;
            Debug.Log($"[CanopicJar:{name}] Left slot trigger: {slot.name}");
        }
    }

    // ── Ungrab callback ───────────────────────────────────────────────────────

    private void OnUngrab()
    {
        if (currentSlot == null)
        {
            Debug.Log($"[CanopicJar:{name}] Ungrabbed — not inside any slot, no snap.");
            return;
        }

        Vector3 snapPos = currentSlot.transform.position;
        snapPos.y += 0.2f;

        transform.position = snapPos;
        transform.rotation = currentSlot.transform.rotation;

        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[CanopicJar:{name}] Ungrabbed inside slot '{currentSlot.name}' — snapped to {snapPos}");
    }
}
