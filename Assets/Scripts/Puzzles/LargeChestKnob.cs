using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Grabbing;

public class LargeChestKnob : NetworkBehaviour
{
    [Header("Settings")]
    public int maxDigits = 10;
    public Vector3 rotationAxis = Vector3.right;
    public Transform visualTransform;
    
    [Header("Networking")]
    [Networked]
    public int CurrentDigit { get; set; }

    private Grabbable _grabbable;
    private NetworkGrabbable _networkGrabbable;
    
    private float _startHandAngle;
    private int _startDigit;
    private bool _isLocalGrabbing;
    private Quaternion _initialLocalRotation;
    private bool _initialRotationCaptured = false;

    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _networkGrabbable = GetComponent<NetworkGrabbable>();

        if (visualTransform != null && !_initialRotationCaptured)
        {
            _initialLocalRotation = visualTransform.localRotation;
            _initialRotationCaptured = true;
        }

        if (_grabbable != null)
        {
            _grabbable.onGrab.AddListener(OnGrab);
            _grabbable.onUngrab.AddListener(OnUngrab);
        }
    }

    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.onGrab.RemoveListener(OnGrab);
            _grabbable.onUngrab.RemoveListener(OnUngrab);
        }
    }

    private void OnGrab()
    {
        if (_networkGrabbable != null && _networkGrabbable.Object.HasStateAuthority)
        {
            _isLocalGrabbing = true;
            _startDigit = CurrentDigit;
            _startHandAngle = GetHandAngle();
        }
    }

    private void OnUngrab()
    {
        _isLocalGrabbing = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (_isLocalGrabbing)
        {
            float currentAngle = GetHandAngle();
            float angleDelta = currentAngle - _startHandAngle;
            
            // Convert angle delta to digit change
            // A positive angle delta (counter-clockwise) should decrement the visual digit on a clockwise mesh
            int digitDelta = Mathf.RoundToInt(angleDelta / (360f / maxDigits));
            int newDigit = (_startDigit - digitDelta + maxDigits) % maxDigits;
            
            if (newDigit != CurrentDigit)
            {
                CurrentDigit = newDigit;
            }
        }
    }

    public override void Render()
    {
        if (visualTransform != null && _initialRotationCaptured)
        {
            // Rotate negatively so that increasing CurrentDigit (1, 2, 3) 
            // results in clockwise rotation, showing those numbers on the mesh.
            float targetAngle = -CurrentDigit * (360f / maxDigits);
            visualTransform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);
        }
    }

    private float GetHandAngle()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return 0;

        Vector3 handPos = _grabbable.currentGrabber.transform.position;
        Vector3 localHandPos = transform.InverseTransformPoint(handPos);
        
        // Project onto the plane perpendicular to rotationAxis (assuming rotationAxis is Z)
        // We use Vector3.right and Vector3.up for the projection plane
        float x = Vector3.Dot(localHandPos, Vector3.right);
        float y = Vector3.Dot(localHandPos, Vector3.up);

        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    // PC Interaction
    public void RotateRight()
    {
        if (Object.HasStateAuthority)
        {
            // Right button increments visual digit: 0 -> 1 -> 2
            CurrentDigit = (CurrentDigit + 1) % maxDigits;
        }
        else
        {
            RPC_RequestRotation(1);
        }
    }

    public void RotateLeft()
    {
        if (Object.HasStateAuthority)
        {
            // Left button decrements visual digit: 0 -> 9 -> 8
            CurrentDigit = (CurrentDigit - 1 + maxDigits) % maxDigits;
        }
        else
        {
            RPC_RequestRotation(-1);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRotation(int direction)
    {
        CurrentDigit = (CurrentDigit + direction + maxDigits) % maxDigits;
    }
}
