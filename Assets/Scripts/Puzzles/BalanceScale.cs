using UnityEngine;
using Fusion;

public class BalanceScale : NetworkBehaviour
{
    public ScalePlate leftPlate;
    public ScalePlate rightPlate;
    public Transform beam;
    public float maxTiltAngle = 15f;
    public float sensitivity = 1.5f;
    public float damping = 0.9f;
    public float inertia = 0.8f;

    public int requiredHearts = 5;
    public int requiredFeathers = 6;

    [Networked] private float Angle { get; set; }
    [Networked] private float Velocity { get; set; }
    
    [Networked] public float LeftWeight { get; set; }
    [Networked] public float RightWeight { get; set; }

    private Quaternion initialRotation;

    public override void Spawned()
    {
        if (beam != null)
        {
            initialRotation = beam.localRotation;
            // Synchronize visual state if already tilted (Rotation on Z axis)
            beam.localRotation = initialRotation * Quaternion.Euler(0, 0, Angle);
        }
        SetupPhysicsMaterials();
    }

    private void SetupPhysicsMaterials()
    {
        PhysicsMaterial highFriction = new PhysicsMaterial("ScaleFriction")
        {
            staticFriction = 1f,
            dynamicFriction = 1f,
            frictionCombine = PhysicsMaterialCombine.Maximum
        };
        
        if (leftPlate != null) SetMaterialRecursive(leftPlate.transform, highFriction);
        if (rightPlate != null) SetMaterialRecursive(rightPlate.transform, highFriction);
    }

    private void SetMaterialRecursive(Transform t, PhysicsMaterial mat)
    {
        var colliders = t.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
        {
            if (!c.isTrigger) c.sharedMaterial = mat;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            UpdateAuthorityLogic();
        }

        // Apply rotation on all clients. 
        if (beam != null)
        {
            beam.localRotation = initialRotation * Quaternion.Euler(0, 0, Angle);
            UpdatePlatesRotation();
        }
        
        // Apply stickiness to objects on all clients (if they own the object)
        ApplyStickiness(leftPlate);
        ApplyStickiness(rightPlate);

        if (Object.HasStateAuthority)
        {
            CheckWinCondition();
        }
    }

    private void UpdatePlatesRotation()
    {
        if (leftPlate != null)
        {
            var ku = leftPlate.GetComponent<KeepUpright>();
            if (ku != null) leftPlate.transform.rotation = ku.initialWorldRotation;
        }
        if (rightPlate != null)
        {
            var ku = rightPlate.GetComponent<KeepUpright>();
            if (ku != null) rightPlate.transform.rotation = ku.initialWorldRotation;
        }
    }

    private void UpdateAuthorityLogic()
    {
        if (leftPlate == null || rightPlate == null) return;

        LeftWeight = leftPlate.totalWeight;
        RightWeight = rightPlate.totalWeight;

        // Inverted: Right - Left so that increasing Left weight tilts it to the visual left.
        float weightDifference = RightWeight - LeftWeight;
        float torque = weightDifference * sensitivity;
        
        // Centering force to make it return to neutral when weights are equal
        float centeringTorque = -Angle * 0.5f; 
        float totalTorque = torque + centeringTorque;
        
        float angularAcceleration = totalTorque / inertia;
        
        Velocity += angularAcceleration * Runner.DeltaTime;
        Velocity *= damping;
        
        float nextAngle = Angle + Velocity * Runner.DeltaTime;
        
        if (nextAngle > maxTiltAngle)
        {
            nextAngle = maxTiltAngle;
            Velocity *= -0.2f; // Bounce
        }
        else if (nextAngle < -maxTiltAngle)
        {
            nextAngle = -maxTiltAngle;
            Velocity *= -0.2f; // Bounce
        }
        
        Angle = nextAngle;
    }

    private void ApplyStickiness(ScalePlate plate)
    {
        if (plate == null) return;
        
        foreach (var wo in plate.weightsOnPlate)
        {
            if (wo == null) continue;
            
            // Check if the object is being grabbed
            var grabbable = wo.GetComponent<Fusion.XR.Shared.Grabbing.Grabbable>();
            bool isGrabbed = grabbable != null && grabbable.currentGrabber != null;

            if (isGrabbed)
            {
                // If grabbed, update the offset so it sticks where it's dropped later
                plate.RefreshOffset(wo);
                continue;
            }

            var no = wo.GetComponent<NetworkObject>();
            // Only the owner of the object (or the server if no owner) should move it
            if (no != null && no.HasStateAuthority)
            {
                Rigidbody rb = wo.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Move the object to match the plate's movement exactly
                    if (plate.localOffsets.TryGetValue(wo, out Vector3 offset))
                    {
                        Vector3 targetWorldPos = plate.transform.TransformPoint(offset);
                        
                        // Use MovePosition for Rigidbody interpolation or set position directly
                        // Setting position directly is more stable for fast Transform-based movements
                        rb.isKinematic = true; 
                        wo.transform.position = targetWorldPos;
                        
                        // Maintain level rotation for the object as well
                        wo.transform.rotation = Quaternion.identity;
                    }
                }
            }
        }
    }

    private void CheckWinCondition()
    {
        if (PuzzleManager.Instance == null || PuzzleManager.Instance.puzzleThreeCompleted) return;

        int heartsOnLeft = 0;
        foreach(var wo in leftPlate.weightsOnPlate)
        {
            if(wo.objectType == "Heart") heartsOnLeft++;
        }
            
        int feathersOnRight = 0;
        foreach(var wo in rightPlate.weightsOnPlate)
        {
            if(wo.objectType == "Feather") feathersOnRight++;
        }

        if (Mathf.Abs(LeftWeight - RightWeight) < 0.1f && LeftWeight > 0)
        {
             if (Mathf.Abs(Velocity) < 0.1f && Mathf.Abs(Angle) < 1f)
             {
                 if(heartsOnLeft == requiredHearts && feathersOnRight == requiredFeathers)
                 {
                     PuzzleManager.Instance.CompletePuzzleThree();
                 }
             }
        }
    }
}
