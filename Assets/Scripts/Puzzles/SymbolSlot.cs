using UnityEngine;
using Fusion.XR.Shared.Grabbing;
using System.Collections.Generic;

public class SymbolSlot : MonoBehaviour
{
    public Transform snapPoint;
    public float snapSmoothing = 20f;
    
    [Header("Current State")]
    public SymbolTablet snappedTablet;
    
    private static List<SymbolSlot> allSlots = new List<SymbolSlot>();
    private List<SymbolTablet> hoveredTablets = new List<SymbolTablet>();

    private void OnEnable() => allSlots.Add(this);
    private void OnDisable() => allSlots.Remove(this);

    private void OnTriggerEnter(Collider other)
    {
        SymbolTablet tablet = other.GetComponentInParent<SymbolTablet>();
        if (tablet != null && !hoveredTablets.Contains(tablet))
        {
            hoveredTablets.Add(tablet);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        SymbolTablet tablet = other.GetComponentInParent<SymbolTablet>();
        if (tablet != null)
        {
            hoveredTablets.Remove(tablet);
        }
    }

    private void Update()
    {
        if (snappedTablet != null)
        {
            Grabbable g = snappedTablet.GetComponent<Grabbable>();
            // If the player grabs the object again, it should detach immediately
            if (g != null && g.currentGrabber != null)
            {
                Unsnap();
            }
        }
        else
        {
            // Try to find the best candidate among tablets released inside the trigger
            SymbolTablet bestTablet = GetClosestReleasedTablet();
            if (bestTablet != null)
            {
                Snap(bestTablet);
            }
        }
    }

    private void LateUpdate()
    {
        if (snappedTablet != null)
        {
            // Lock position and rotation to snap point
            snappedTablet.transform.position = Vector3.Lerp(snappedTablet.transform.position, snapPoint.position, Time.deltaTime * snapSmoothing);
            snappedTablet.transform.rotation = Quaternion.Slerp(snappedTablet.transform.rotation, snapPoint.rotation, Time.deltaTime * snapSmoothing);

            // Ensure it is locked kinematic to the slot while not grabbed
            Rigidbody rb = snappedTablet.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private SymbolTablet GetClosestReleasedTablet()
    {
        SymbolTablet best = null;
        float minCDist = float.MaxValue;

        for (int i = hoveredTablets.Count - 1; i >= 0; i--)
        {
            SymbolTablet t = hoveredTablets[i];
            if (t == null) { hoveredTablets.RemoveAt(i); continue; }

            Grabbable g = t.GetComponent<Grabbable>();
            // Only snap tablets that are NOT currently being held
            if (g == null || g.currentGrabber != null) continue;

            // Check if tablet is already claimed by another slot
            if (IsTabletSnappedElsewhere(t)) continue;

            float d = Vector3.Distance(t.transform.position, snapPoint.position);
            
            // Only claim if this specific slot is the absolute closest one to the tablet
            if (d < minCDist && IsThisTheClosestSlot(t, d))
            {
                minCDist = d;
                best = t;
            }
        }
        return best;
    }

    private bool IsThisTheClosestSlot(SymbolTablet t, float myDist)
    {
        foreach (var slot in allSlots)
        {
            if (slot == this) continue;
            // If the other slot is already occupied, it doesn't compete for this tablet
            if (slot.snappedTablet != null) continue;

            float otherDist = Vector3.Distance(t.transform.position, slot.snapPoint.position);
            if (otherDist < myDist) return false;
        }
        return true;
    }

    private bool IsTabletSnappedElsewhere(SymbolTablet t)
    {
        foreach (var slot in allSlots)
        {
            if (slot != this && slot.snappedTablet == t) return true;
        }
        return false;
    }

    private void Snap(SymbolTablet t)
    {
        snappedTablet = t;
        Rigidbody rb = t.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Unsnap()
    {
        if (snappedTablet != null)
        {
            Rigidbody rb = snappedTablet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            snappedTablet = null;
        }
    }

    public bool IsCorrect(char expectedSymbol)
    {
        return snappedTablet != null && snappedTablet.symbol == expectedSymbol;
    }
}
