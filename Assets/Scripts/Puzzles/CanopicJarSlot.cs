using UnityEngine;
using System.Collections.Generic;

public class CanopicJarSlot : MonoBehaviour
{
    public CanopicJarType requiredJarType;
    public List<CanopicJar> jarsInSlot = new List<CanopicJar>();
    public CanopicJar currentJar => jarsInSlot.Count > 0 ? jarsInSlot[0] : null;

    private void OnTriggerEnter(Collider other)
    {
        CanopicJar jar = other.GetComponentInParent<CanopicJar>();
        if (jar != null && !jarsInSlot.Contains(jar))
        {
            jarsInSlot.Add(jar);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CanopicJar jar = other.GetComponentInParent<CanopicJar>();
        if (jar != null && jarsInSlot.Contains(jar))
        {
            jarsInSlot.Remove(jar);
        }
    }

    public bool IsCorrect()
    {
        return currentJar != null && currentJar.jarType == requiredJarType;
    }
}
