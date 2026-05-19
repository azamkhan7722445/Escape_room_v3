using UnityEngine;
using Fusion.XR.Shared.Grabbing;
using System.Collections.Generic;

public class SymbolSlot : MonoBehaviour
{
    public Transform snapPoint;
    public float snapSmoothing = 20f;
    public float snapDistanceThreshold = 0.15f; // Threshold for snapping distance
    
    [Header("Current State")]
    public Grabbable snappedObject;
    public SymbolTablet snappedTablet;
    
    private static List<SymbolSlot> allSlots = new List<SymbolSlot>();
    private List<Grabbable> hoveredObjects = new List<Grabbable>();

    private void OnEnable() => allSlots.Add(this);
    private void OnDisable() => allSlots.Remove(this);

    private void OnTriggerEnter(Collider other)
    {
        Grabbable g = other.GetComponentInParent<Grabbable>();
        if (g != null && !hoveredObjects.Contains(g))
        {
            hoveredObjects.Add(g);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Grabbable g = other.GetComponentInParent<Grabbable>();
        if (g != null)
        {
            hoveredObjects.Remove(g);
        }
    }

    private void Update()
    {
        if (snappedObject != null)
        {
            // If the player grabs the object again, it should detach immediately
            if (snappedObject.currentGrabber != null)
            {
                Unsnap();
            }
        }
        else
        {
            // Try to find the best candidate among objects released inside the trigger
            Grabbable bestObj = GetClosestReleasedObject();
            if (bestObj != null)
            {
                Snap(bestObj);
            }
        }
    }

    private void LateUpdate()
    {
        if (snappedObject != null)
        {
            // Lock position and rotation to snap point with smoothing
            snappedObject.transform.position = Vector3.Lerp(snappedObject.transform.position, snapPoint.position, Time.deltaTime * snapSmoothing);
            snappedObject.transform.rotation = Quaternion.Slerp(snappedObject.transform.rotation, snapPoint.rotation, Time.deltaTime * snapSmoothing);

            // Once it is extremely close, hard-snap to prevent jitter or micro-movements
            if (Vector3.Distance(snappedObject.transform.position, snapPoint.position) < 0.001f)
            {
                snappedObject.transform.position = snapPoint.position;
                snappedObject.transform.rotation = snapPoint.rotation;
            }

            // Ensure it is locked kinematic to the slot while not grabbed
            Rigidbody rb = snappedObject.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private Grabbable GetClosestReleasedObject()
    {
        Grabbable best = null;
        float minCDist = float.MaxValue;

        for (int i = hoveredObjects.Count - 1; i >= 0; i--)
        {
            Grabbable g = hoveredObjects[i];
            if (g == null) { hoveredObjects.RemoveAt(i); continue; }

            // Only snap objects that are NOT currently being held
            if (g.currentGrabber != null) continue;

            // Check if object is already claimed by another slot
            if (IsObjectSnappedElsewhere(g)) continue;

            float d = Vector3.Distance(g.transform.position, snapPoint.position);
            
            // Check threshold for realistic "correct placement"
            if (d > snapDistanceThreshold) continue;

            // Only claim if this specific slot is the absolute closest one to the object
            if (d < minCDist && IsThisTheClosestSlot(g, d))
            {
                minCDist = d;
                best = g;
            }
        }
        return best;
    }

    private bool IsThisTheClosestSlot(Grabbable g, float myDist)
    {
        foreach (var slot in allSlots)
        {
            if (slot == this) continue;
            // If the other slot is already occupied, it doesn't compete for this object
            if (slot.snappedObject != null) continue;

            float otherDist = Vector3.Distance(g.transform.position, slot.snapPoint.position);
            if (otherDist < myDist) return false;
        }
        return true;
    }

    private bool IsObjectSnappedElsewhere(Grabbable g)
    {
        foreach (var slot in allSlots)
        {
            if (slot != this && slot.snappedObject == g) return true;
        }
        return false;
    }

    private void Snap(Grabbable g)
    {
        snappedObject = g;
        snappedTablet = g.GetComponent<SymbolTablet>();
        
        Rigidbody rb = g.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Unsnap()
    {
        if (snappedObject != null)
        {
            Rigidbody rb = snappedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            snappedObject = null;
            snappedTablet = null;
        }
    }

    public bool IsCorrect(char expectedSymbol)
    {
        return snappedTablet != null && snappedTablet.symbol == expectedSymbol;
    }
}

