using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;

[DefaultExecutionOrder(30000)] // Run very late to override XR Rig updates
public class LargeChestKnob : NetworkBehaviour
{
    [Header("Settings")]
    public int maxDigits = 10;
    public Vector3 rotationAxis = Vector3.forward;
    public Transform visualTransform;
    
    [Header("Grab Settings")]
    public float grabSensitivity = 1.0f;
    public float minGrabDistance = 0.001f;
    public float hapticAmplitude = 0.1f;
    public float hapticDuration = 0.05f;
    public bool snapHandToKnob = true;
    public bool snapToCenter = true;
    
    [Header("UI Click Cooldown")]
    public float buttonCooldown = 0.25f;
    private float _lastRotateTime = -999f;

    [Header("Networking")]
    [Networked, OnChangedRender(nameof(OnDigitChanged))]
    public int CurrentDigit { get; set; }

    private Grabbable _grabbable;
    private float _lastHandAngle;
    private float _cumulativeAngleDelta;
    private int _startDigit;
    private bool _isLocalGrabbing;
    private Quaternion _initialLocalRotation;
    private bool _initialRotationCaptured = false;

    // Hand Snapping
    private Transform _localHandVisual;
    private Vector3 _handVisualInitialLocalPos;
    private Quaternion _handVisualInitialLocalRot;
    private Vector3 _grabPointOffset;
    private Quaternion _grabPointRotationOffset;

    [Header("Audio")]
    public AudioClip rotateSound;
    private AudioSource _audioSource;

    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _audioSource = GetComponent<AudioSource>();

        if (visualTransform != null && !_initialRotationCaptured)
        {
            _initialLocalRotation = visualTransform.localRotation;
            _initialRotationCaptured = true;
        }
    }

    void OnDigitChanged()
    {
        if (_audioSource != null && rotateSound != null)
            _audioSource.PlayOneShot(rotateSound);
    }

    private void Start()
    {
        if (_grabbable == null) _grabbable = GetComponent<Grabbable>();
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
        _isLocalGrabbing = true;
        _lastHandAngle = GetHandAngle();
        _cumulativeAngleDelta = 0;
        
        if (Object != null) _startDigit = CurrentDigit;
        else _startDigit = 0;

        if (snapHandToKnob && _grabbable != null && _grabbable.currentGrabber != null)
        {
            var hand = _grabbable.currentGrabber.GetComponentInParent<HardwareHand>();
            if (hand != null && hand.localRepresentation != null)
            {
                _localHandVisual = hand.localRepresentation.gameObject.transform;
                _handVisualInitialLocalPos = _localHandVisual.localPosition;
                _handVisualInitialLocalRot = _localHandVisual.localRotation;
                
                if (visualTransform != null)
                {
                    if (snapToCenter)
                    {
                        // Snap to a fixed point on the knob
                        Vector3 handPos = _grabbable.currentGrabber.transform.position;
                        Vector3 localHand = visualTransform.InverseTransformPoint(handPos);
                        Vector3 projected = Vector3.ProjectOnPlane(localHand, rotationAxis);
                        if (projected.magnitude < 0.01f) projected = Vector3.up * 0.05f;
                        else projected = projected.normalized * 0.05f;
                        
                        _grabPointOffset = projected;
                    }
                    else
                    {
                        _grabPointOffset = visualTransform.InverseTransformPoint(_grabbable.currentGrabber.transform.position);
                    }
                    
                    _grabPointRotationOffset = Quaternion.Inverse(visualTransform.rotation) * _grabbable.currentGrabber.transform.rotation;
                }
            }
        }
    }

    private void OnUngrab()
    {
        _isLocalGrabbing = false;
        
        if (_localHandVisual != null)
        {
            _localHandVisual.localPosition = _handVisualInitialLocalPos;
            _localHandVisual.localRotation = _handVisualInitialLocalRot;
            _localHandVisual = null;
        }
    }

    private void Update()
    {
        if (_isLocalGrabbing)
        {
            // Update rotation smoothly every frame for responsiveness
            float currentAngle = GetHandAngle();
            float delta = Mathf.DeltaAngle(_lastHandAngle, currentAngle);
            
            if (GetHandDistance() > minGrabDistance)
            {
                _cumulativeAngleDelta += delta;
                _lastHandAngle = currentAngle;
            }
        }
    }

    private void LateUpdate()
    {
        if (_isLocalGrabbing && _localHandVisual != null && visualTransform != null)
        {
            _localHandVisual.position = visualTransform.TransformPoint(_grabPointOffset);
            _localHandVisual.rotation = visualTransform.rotation * _grabPointRotationOffset;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_isLocalGrabbing)
        {
            if (Object != null && Object.HasStateAuthority)
            {
                // Update network state based on accumulated delta
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

    private void SendHapticFeedback()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return;
        
        var hand = _grabbable.currentGrabber.GetComponentInParent<HardwareHand>();
        if (hand != null)
        {
            hand.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }
    }

    public override void Render()
    {
        if (visualTransform != null && _initialRotationCaptured)
        {
            float targetAngle;
            if (_isLocalGrabbing)
            {
                // Smooth rotation following the hand for the local player
                targetAngle = -_startDigit * (360f / maxDigits) - (_cumulativeAngleDelta * grabSensitivity);
            }
            else
            {
                // Snap to current digit for everyone else or when not grabbing
                targetAngle = -CurrentDigit * (360f / maxDigits);
            }
            visualTransform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);
        }
    }

    private float GetHandAngle()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return 0;

        // 1. Use the knob's actual world position as the center.
        Vector3 center = transform.position;
        Vector3 handPos = _grabbable.currentGrabber.transform.position;
        Vector3 directionToHand = handPos - center;

        // 2. Use the knob's world-space rotation axis.
        Vector3 worldAxis = transform.TransformDirection(rotationAxis);

        // 3. Use stable world-space vectors for the projection plane (derived from parent).
        // Using transform.parent ensures these vectors don't rotate with the knob itself.
        Vector3 parentUp = transform.parent != null ? transform.parent.up : Vector3.up;
        Vector3 parentForward = transform.parent != null ? transform.parent.forward : Vector3.forward;

        Vector3 right = Vector3.Cross(worldAxis, parentUp).normalized;
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(worldAxis, parentForward).normalized;
        }
        Vector3 up = Vector3.Cross(right, worldAxis).normalized;

        // 4. Circular hand movement logic (Position-based angle)
        Vector3 projectedPos = Vector3.ProjectOnPlane(directionToHand, worldAxis);
        float planeAngle = 0;
        if (projectedPos.sqrMagnitude > 0.0001f)
        {
            planeAngle = Mathf.Atan2(Vector3.Dot(projectedPos, up), Vector3.Dot(projectedPos, right)) * Mathf.Rad2Deg;
        }

        // 7. Hand's rotation delta (twist) around the axis.
        // This adds a more natural feel by allowing the knob to respond to wrist rotation as well.
        Vector3 handUp = _grabbable.currentGrabber.transform.up;
        Vector3 projectedHandUp = Vector3.ProjectOnPlane(handUp, worldAxis);
        float twistAngle = 0;
        if (projectedHandUp.sqrMagnitude > 0.0001f)
        {
            twistAngle = Mathf.Atan2(Vector3.Dot(projectedHandUp, up), Vector3.Dot(projectedHandUp, right)) * Mathf.Rad2Deg;
        }

        // 6. Debugging lines (commented out)
        /*
        Debug.DrawRay(center, worldAxis * 0.2f, Color.blue);
        Debug.DrawRay(center, right * 0.2f, Color.red);
        Debug.DrawRay(center, up * 0.2f, Color.green);
        Debug.DrawLine(center, handPos, Color.yellow);
        */

        return planeAngle + twistAngle;
    }

    private float GetHandDistance()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return 0;
        
        Vector3 worldAxis = transform.TransformDirection(rotationAxis);
        Vector3 directionToHand = _grabbable.currentGrabber.transform.position - transform.position;
        return Vector3.ProjectOnPlane(directionToHand, worldAxis).magnitude;
    }

    public void RotateRight()
    {
        if (Time.time - _lastRotateTime < buttonCooldown) return;
        _lastRotateTime = Time.time;

        if (Object != null && Object.HasStateAuthority)
            CurrentDigit = (CurrentDigit + 1) % maxDigits;
        else
            RPC_RequestRotation(1);
    }

    public void RotateLeft()
    {
        if (Time.time - _lastRotateTime < buttonCooldown) return;
        _lastRotateTime = Time.time;

        if (Object != null && Object.HasStateAuthority)
            CurrentDigit = (CurrentDigit - 1 + maxDigits) % maxDigits;
        else
            RPC_RequestRotation(-1);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRotation(int direction)
    {
        if (Time.time - _lastRotateTime < buttonCooldown) return;
        _lastRotateTime = Time.time;

        CurrentDigit = (CurrentDigit + direction + maxDigits) % maxDigits;
    }
}