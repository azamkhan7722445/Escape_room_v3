using UnityEngine;
using System.Collections.Generic;

public class ScalePlate : MonoBehaviour
{
    public List<WeightObject> weightsOnPlate = new List<WeightObject>();
    public float totalWeight = 0f;

    private void OnTriggerEnter(Collider other)
    {
        WeightObject wo = other.GetComponent<WeightObject>();
        if (wo != null && !weightsOnPlate.Contains(wo))
        {
            weightsOnPlate.Add(wo);
            UpdateWeight();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WeightObject wo = other.GetComponent<WeightObject>();
        if (wo != null && weightsOnPlate.Contains(wo))
        {
            weightsOnPlate.Remove(wo);
            UpdateWeight();
        }
    }

    private void UpdateWeight()
    {
        totalWeight = 0f;
        foreach (var wo in weightsOnPlate)
        {
            totalWeight += wo.weight;
        }
    }
}
