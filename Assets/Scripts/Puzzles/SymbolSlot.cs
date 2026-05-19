using UnityEngine;
using Fusion.XR.Shared.Grabbing;
using System.Collections.Generic;

public class SymbolSlot : MonoBehaviour
{
    public Transform snapPoint;
    public float snapSmoothing = 20f;
    public float snapDistanceThreshold = 0.6f; // Large threshold for ease of use
    
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
            if (snappedObject.currentGrabber != null)
            {
                Unsnap();
            }
        }
        else
        {
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
            // Lock position and rotation directly to snap point transforms
            float effectiveSmoothing = snapSmoothing * Time.deltaTime;
            snappedObject.transform.position = Vector3.Lerp(snappedObject.transform.position, snapPoint.position, effectiveSmoothing);
            snappedObject.transform.rotation = Quaternion.Slerp(snappedObject.transform.rotation, snapPoint.rotation, effectiveSmoothing);

            // Hard snap if close
            if (Vector3.Distance(snappedObject.transform.position, snapPoint.position) < 0.005f && 
                Quaternion.Angle(snappedObject.transform.rotation, snapPoint.rotation) < 0.5f)
            {
                snappedObject.transform.position = snapPoint.position;
                snappedObject.transform.rotation = snapPoint.rotation;
            }

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
            if (g.currentGrabber != null) continue;
            if (g.GetComponent<SymbolTablet>() == null) continue;
            if (IsObjectSnappedElsewhere(g)) continue;

            // Use direct transform distance
            float d = Vector3.Distance(g.transform.position, snapPoint.position);
            
            if (d > snapDistanceThreshold) continue;

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
            if (slot == this || slot.snappedObject != null) continue;
            if (Vector3.Distance(g.transform.position, slot.snapPoint.position) < myDist) return false;
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
            if (rb != null) rb.isKinematic = false;
            snappedObject = null;
            snappedTablet = null;
        }
    }

    public bool IsCorrect(char expectedSymbol)
    {
        return snappedTablet != null && snappedTablet.symbol == expectedSymbol;
    }
}

