using UnityEngine;
using Fusion;

public class BalanceScale : NetworkBehaviour
{
    public ScalePlate leftPlate;
    public ScalePlate rightPlate;
    public Transform beam;
    public float maxTiltAngle = 15f;
    public float sensitivity = 2f;
    public float damping = 0.95f;
    public float inertia = 0.1f;

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
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            UpdateAuthorityLogic();
        }

        // Apply rotation on all clients. 
        // Based on local coordinates, the beam length is along X, so tilt is around Z.
        if (beam != null)
        {
            beam.localRotation = initialRotation * Quaternion.Euler(0, 0, Angle);
        }
        
        if (Object.HasStateAuthority)
        {
            CheckWinCondition();
        }
    }

    private void UpdateAuthorityLogic()
    {
        if (leftPlate == null || rightPlate == null) return;

        LeftWeight = leftPlate.totalWeight;
        RightWeight = rightPlate.totalWeight;

        // Inverted: Right - Left so that increasing Left weight tilts it to the visual left.
        // This accounts for the 180-degree initial rotation of the beam.
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
