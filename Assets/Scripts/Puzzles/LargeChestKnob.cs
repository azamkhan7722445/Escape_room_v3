using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;

public class LargeChestKnob : NetworkBehaviour
{
    [Header("Settings")]
    public int maxDigits = 10;
    public Vector3 rotationAxis = Vector3.forward;
    public Transform visualTransform;
    
    [Header("Grab Settings")]
    public float grabSensitivity = 1.0f;
    public float minGrabDistance = 0.01f;
    public float hapticAmplitude = 0.1f;
    public float hapticDuration = 0.05f;

    [Header("Networking")]
    [Networked]
    public int CurrentDigit { get; set; }

    private Grabbable _grabbable;
    private NetworkGrabbable _networkGrabbable;
    
    private float _lastHandAngle;
    private float _cumulativeAngleDelta;
    private int _startDigit;
    private bool _isLocalGrabbing;
    private Quaternion _initialLocalRotation;
    private bool _initialRotationCaptured = false;

    [Header("Audio")]
    public AudioClip rotateSound;
    private AudioSource _audioSource;

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _networkGrabbable = GetComponent<NetworkGrabbable>();
        _audioSource = GetComponent<AudioSource>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

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
        // onGrab is only called for the local player who performs the grab.
        // We set _isLocalGrabbing to true to start tracking hand movement.
        _isLocalGrabbing = true;
        _lastHandAngle = GetHandAngle();
        _cumulativeAngleDelta = 0;
        _startDigit = CurrentDigit;
    }

    private void OnUngrab()
    {
        _isLocalGrabbing = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (_isLocalGrabbing)
        {
            // Even if we don't have authority yet, we track the delta.
            // Authority is usually acquired a few frames after grab by NetworkGrabbable.
            float currentAngle = GetHandAngle();
            float delta = Mathf.DeltaAngle(_lastHandAngle, currentAngle);
            _lastHandAngle = currentAngle;

            if (GetHandDistance() > minGrabDistance)
            {
                _cumulativeAngleDelta += delta;

                // We only apply the change to the networked property if we have authority.
                if (Object.HasStateAuthority)
                {
                    // Convert angle delta to digit change
                    int digitDelta = Mathf.RoundToInt((_cumulativeAngleDelta * grabSensitivity) / (360f / maxDigits));
                    int newDigit = (_startDigit - digitDelta + (maxDigits * 100)) % maxDigits;
                    
                    if (newDigit != CurrentDigit)
                    {
                        CurrentDigit = newDigit;
                        SendHapticFeedback();
                    }
                }
            }
        }
    }

    private void SendHapticFeedback()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return;
        
        // Find hardware hand to send haptics
        var hand = _grabbable.currentGrabber.GetComponentInParent<HardwareHand>();
        if (hand != null)
        {
            hand.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CurrentDigit))
            {
                PlayRotateSound();
            }
        }

        if (visualTransform != null && _initialRotationCaptured)
        {
            // Rotate negatively so that increasing CurrentDigit (1, 2, 3) 
            // results in clockwise rotation, showing those numbers on the mesh.
            float targetAngle = -CurrentDigit * (360f / maxDigits);
            visualTransform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);
        }
    }

    private void PlayRotateSound()
    {
        if (_audioSource != null && rotateSound != null)
        {
            _audioSource.PlayOneShot(rotateSound);
        }
    }

    private float GetHandAngle()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return 0;

        Vector3 handPos = _grabbable.currentGrabber.transform.position;
        
        // Use parent space to avoid feedback loop. 
        // If we use 'transform.InverseTransformPoint', rotating the knob 
        // would move the hand in local space, causing it to spin wildly.
        Vector3 localHandPos;
        if (transform.parent != null)
        {
            localHandPos = transform.parent.InverseTransformPoint(handPos) - transform.localPosition;
        }
        else
        {
            localHandPos = transform.InverseTransformPoint(handPos);
        }
        
        // Use a robust projection based on rotationAxis
        Vector3 projected = Vector3.ProjectOnPlane(localHandPos, rotationAxis);
        
        // Define coordinate system on the plane based on initial rotation
        Vector3 right, up;
        if (Mathf.Abs(Vector3.Dot(rotationAxis, Vector3.up)) < 0.9f)
        {
            right = Vector3.Cross(rotationAxis, Vector3.up).normalized;
            up = Vector3.Cross(right, rotationAxis).normalized;
        }
        else
        {
            right = Vector3.Cross(rotationAxis, Vector3.forward).normalized;
            up = Vector3.Cross(right, rotationAxis).normalized;
        }

        float x = Vector3.Dot(projected, right);
        float y = Vector3.Dot(projected, up);

        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    private float GetHandDistance()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return 0;
        Vector3 handPos = _grabbable.currentGrabber.transform.position;
        
        Vector3 localHandPos;
        if (transform.parent != null)
            localHandPos = transform.parent.InverseTransformPoint(handPos) - transform.localPosition;
        else
            localHandPos = transform.InverseTransformPoint(handPos);
            
        return Vector3.ProjectOnPlane(localHandPos, rotationAxis).magnitude;
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
            // If we don't have authority, request it via RPC
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
            // If we don't have authority, request it via RPC
            RPC_RequestRotation(-1);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRotation(int direction)
    {
        CurrentDigit = (CurrentDigit + direction + maxDigits) % maxDigits;
    }
}

