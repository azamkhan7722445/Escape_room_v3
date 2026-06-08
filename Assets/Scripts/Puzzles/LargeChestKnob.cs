using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;

[DefaultExecutionOrder(30000)]
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
    public bool snapHandToKnob = true;
    public bool snapToCenter = true;

    [Header("Networking")]
    [Networked]
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
    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _audioSource = GetComponent<AudioSource>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (visualTransform != null && !_initialRotationCaptured)
        {
            _initialLocalRotation = visualTransform.localRotation;
            _initialRotationCaptured = true;
        }
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
                        // Snap to a fixed point on the knob (e.g. at a reasonable distance from center)
                        Vector3 handPos = _grabbable.currentGrabber.transform.position;
                        Vector3 localHand = visualTransform.InverseTransformPoint(handPos);
                        // Flatten to plane and normalize to a fixed distance (e.g. 0.05 units)
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
            float currentAngle = GetHandAngle();
            float delta = Mathf.DeltaAngle(_lastHandAngle, currentAngle);
            _lastHandAngle = currentAngle;

            if (GetHandDistance() > minGrabDistance)
            {
                _cumulativeAngleDelta += delta;

                if (Object != null && Object.HasStateAuthority)
                {
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
        
        var hand = _grabbable.currentGrabber.GetComponentInParent<HardwareHand>();
        if (hand != null)
        {
            hand.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }
    }

    public override void Render()
    {
        if (_changeDetector != null)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                if (change == nameof(CurrentDigit))
                {
                    if (_audioSource != null && rotateSound != null)
                        _audioSource.PlayOneShot(rotateSound);
                }
            }
        }

        if (visualTransform != null && _initialRotationCaptured)
        {
            float targetAngle = -CurrentDigit * (360f / maxDigits);
            visualTransform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);
        }
    }

    private float GetHandAngle()
    {
        if (_grabbable == null || _grabbable.currentGrabber == null) return 0;

        Vector3 handPos = _grabbable.currentGrabber.transform.position;
        Vector3 localHandPos;
        if (transform.parent != null)
            localHandPos = transform.parent.InverseTransformPoint(handPos) - transform.localPosition;
        else
            localHandPos = transform.InverseTransformPoint(handPos);
        
        Vector3 projected = Vector3.ProjectOnPlane(localHandPos, rotationAxis);
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

    public void RotateRight()
    {
        if (Object != null && Object.HasStateAuthority)
            CurrentDigit = (CurrentDigit + 1) % maxDigits;
        else
            RPC_RequestRotation(1);
    }

    public void RotateLeft()
    {
        if (Object != null && Object.HasStateAuthority)
            CurrentDigit = (CurrentDigit - 1 + maxDigits) % maxDigits;
        else
            RPC_RequestRotation(-1);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRotation(int direction)
    {
        CurrentDigit = (CurrentDigit + direction + maxDigits) % maxDigits;
    }
}