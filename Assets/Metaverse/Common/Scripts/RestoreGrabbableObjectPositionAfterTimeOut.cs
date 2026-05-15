using Fusion;
using Fusion.XR.Shared.Grabbing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Automatically returns a grabbable object to its original position/rotation
// after it has been ungrabbed for longer than the specified timeout duration.
[RequireComponent(typeof(NetworkGrabbable))]
public class RestoreGrabbableObjectPositionAfterTimeOut : NetworkBehaviour
{
    // How many seconds after being ungrabbed before the object snaps back to its start transform
    [SerializeField] private float timeOut;

    // Tracks the Time.time value when the object was last released; 0 means not released yet
    private float lastUnGrabTime = 0f;

    // Reference to the Fusion grabbing component on this object
    NetworkGrabbable networkGrabbable;

    // Networked so all clients agree on the object's original spawn position
    [Networked]
    Vector3 InitialObjectPosition { get; set; }

    // Networked so all clients agree on the object's original spawn rotation
    [Networked]
    Quaternion InitialObjectRotation { get; set; }

    // Networked flag indicating whether the object is currently at its start transform
    [Networked]
    NetworkBool IsAtStartPosition { get; set; }

    // Cache the NetworkGrabbable component and warn if it is missing
    private void Awake()
    {
        networkGrabbable = GetComponent<NetworkGrabbable>();
        if (!networkGrabbable)
            Debug.LogError("NetworkGrabbable not found !");
    }

    // Capture the spawn transform on the State Authority so networked properties are set once
    public override void Spawned()
    {
        base.Spawned();

        if (networkGrabbable && networkGrabbable.Object && networkGrabbable.Object.HasStateAuthority)
        {
            InitialObjectPosition = transform.position;
            InitialObjectRotation = transform.rotation;
            IsAtStartPosition = true;
        }
    }

    // Only the State Authority runs the restore logic to avoid conflicting writes
    void Update()
    {
        if (networkGrabbable && networkGrabbable.Object && networkGrabbable.Object.HasStateAuthority)
        {
            if (networkGrabbable.IsGrabbed)
            {
                // While grabbed, clear the ungrab timer and mark object as displaced
                lastUnGrabTime = 0f;
                if (IsAtStartPosition == true)
                    IsAtStartPosition = false;
            }

            // Only count down when the object has been released away from its start position
            if (networkGrabbable.IsGrabbed == false && IsAtStartPosition == false)
            {
                if (lastUnGrabTime == 0f)
                {
                    // First frame after release — record the release time
                    lastUnGrabTime = Time.time;
                }
                else if (Time.time > (lastUnGrabTime + timeOut))
                {
                    // Timeout elapsed — snap back to the original transform
                    transform.position = InitialObjectPosition;
                    transform.rotation = InitialObjectRotation;
                    IsAtStartPosition = true;
                    lastUnGrabTime = 0f;
                }
            }
        }
    }
}
