using UnityEngine;
using System.Collections.Generic;

public class ScalePlate : MonoBehaviour
{
    public List<WeightObject> weightsOnPlate = new List<WeightObject>();
    public Dictionary<WeightObject, Vector3> localOffsets = new Dictionary<WeightObject, Vector3>();
    public float totalWeight = 0f;

    private void OnTriggerEnter(Collider other)
    {
        WeightObject wo = other.GetComponent<WeightObject>();
        if (wo != null && !weightsOnPlate.Contains(wo))
        {
            weightsOnPlate.Add(wo);
            localOffsets[wo] = transform.InverseTransformPoint(wo.transform.position);
            UpdateWeight();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WeightObject wo = other.GetComponent<WeightObject>();
        if (wo != null && weightsOnPlate.Contains(wo))
        {
            weightsOnPlate.Remove(wo);
            localOffsets.Remove(wo);
            
            // Restore physics when leaving the plate
            Rigidbody rb = wo.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Grabbable will handle its own kinematic state, but for non-grabbed objects we restore it
                var grabbable = wo.GetComponent<Fusion.XR.Shared.Grabbing.Grabbable>();
                if (grabbable == null || grabbable.currentGrabber == null)
                {
                    rb.isKinematic = false;
                }
            }
            
            UpdateWeight();
        }
    }

    private void UpdateWeight()
    {
        totalWeight = 0f;
        foreach (var wo in weightsOnPlate)
        {
            if (wo != null) totalWeight += wo.weight;
        }
    }

    public void RefreshOffset(WeightObject wo)
    {
        if (weightsOnPlate.Contains(wo))
        {
            localOffsets[wo] = transform.InverseTransformPoint(wo.transform.position);
        }
    }
}
