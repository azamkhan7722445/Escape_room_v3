using UnityEngine;
using System.Collections.Generic;

public class ScalePan : MonoBehaviour
{
    public List<WeightedObject> objectsOnPan = new List<WeightedObject>();
    public float TotalWeight { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        WeightedObject wo = other.GetComponent<WeightedObject>();
        if (wo != null && !objectsOnPan.Contains(wo))
        {
            objectsOnPan.Add(wo);
            RecalculateWeight();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WeightedObject wo = other.GetComponent<WeightedObject>();
        if (wo != null && objectsOnPan.Contains(wo))
        {
            objectsOnPan.Remove(wo);
            RecalculateWeight();
        }
    }

    public void RecalculateWeight()
    {
        TotalWeight = 0;
        foreach (var obj in objectsOnPan)
        {
            TotalWeight += obj.weight;
        }
    }
}
