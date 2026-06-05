using UnityEngine;
using System.Collections.Generic;
using Fusion.XR.Shared.Grabbing;

public class CanopicJarSlot : MonoBehaviour
{
    public CanopicJarType requiredJarType;
    public Transform snapPoint;
    public float snapSmoothing = 20f;
    public float snapDistanceThreshold = 0.5f;

    public List<CanopicJar> jarsInSlot = new List<CanopicJar>();
    public CanopicJar currentJar => jarsInSlot.Count > 0 ? jarsInSlot[0] : null;

    [Header("Current State")]
    public CanopicJar snappedJar;

    private void Update()
    {
        if (snappedJar != null)
        {
            // If the jar is grabbed again, unsnap it
            var grabbable = snappedJar.GetComponent<Grabbable>();
            if (grabbable != null && grabbable.currentGrabber != null)
            {
                Unsnap();
            }
        }
        else
        {
            // Look for a jar to snap
            CanopicJar bestJar = GetClosestReleasedJar();
            if (bestJar != null)
            {
                Snap(bestJar);
            }
        }
    }

    private void LateUpdate()
    {
        if (snappedJar != null)
        {
            Transform target = snapPoint != null ? snapPoint : transform;
            
            float effectiveSmoothing = snapSmoothing * Time.deltaTime;
            snappedJar.transform.position = Vector3.Lerp(snappedJar.transform.position, target.position, effectiveSmoothing);
            snappedJar.transform.rotation = Quaternion.Slerp(snappedJar.transform.rotation, target.rotation, effectiveSmoothing);

            // Hard snap if very close
            if (Vector3.Distance(snappedJar.transform.position, target.position) < 0.005f)
            {
                snappedJar.transform.position = target.position;
                snappedJar.transform.rotation = target.rotation;
            }

            Rigidbody rb = snappedJar.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private CanopicJar GetClosestReleasedJar()
    {
        CanopicJar best = null;
        float minDist = float.MaxValue;

        for (int i = jarsInSlot.Count - 1; i >= 0; i--)
        {
            CanopicJar jar = jarsInSlot[i];
            if (jar == null) { jarsInSlot.RemoveAt(i); continue; }

            var grabbable = jar.GetComponent<Grabbable>();
            if (grabbable != null && grabbable.currentGrabber != null) continue;

            float d = Vector3.Distance(jar.transform.position, transform.position);
            if (d < minDist && d < snapDistanceThreshold)
            {
                minDist = d;
                best = jar;
            }
        }
        return best;
    }

    private void Snap(CanopicJar jar)
    {
        snappedJar = jar;
        Rigidbody rb = jar.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Unsnap()
    {
        if (snappedJar != null)
        {
            Rigidbody rb = snappedJar.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
            snappedJar = null;
        }
    }

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
            if (snappedJar == jar) Unsnap();
        }
    }

    public bool IsCorrect()
    {
        return snappedJar != null && snappedJar.jarType == requiredJarType;
    }
}
