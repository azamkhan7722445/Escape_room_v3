using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fusion.XR.Shared.Locomotion
{
    public struct RayData : INetworkStruct
    {
        public bool isRayEnabled;
        public Vector3 origin;
        public Vector3 target;
        public Color color;
    }

    public interface IRayDescriptor
    {
        public RayData Ray { get; }
    }

    public interface IRayValidator
    {
        // Called just after a successful raycast, and ray status change, allowing to disable/change the status if needed (for instance if we try to telport to an invalid target, ...)
        public void ValidateOnBeamerHit(RayBeamer beamer, RaycastHit hit);
    }

    /**
     * 
     * Display a line renderer when action input is pressed, and raycast other the selected layer mask to find a destination point
     * 
     **/

    public class RayBeamer : MonoBehaviour, IRayDescriptor
    {
        public HardwareHand hand;

        public bool useRayActionInput = true;
#if ENABLE_INPUT_SYSTEM
        public InputActionProperty rayAction;
#endif
        public Transform origin;
        public LayerMask targetLayerMask = ~0;
        public float maxDistance = 100f;

        [Header("Representation")]
        public LineRenderer lineRenderer;
        public float width = 0.02f;
        public Material lineMaterial;

        public Color hitColor = Color.green;
        public Color noHitColor = Color.red;

        [Header("Distance Grab")]
        [Tooltip("Beam color when pointing at a grabbable object")]
        public Color grabColor = Color.cyan;
        [Tooltip("Seconds for the object to fly to the hand")]
        public float flyDuration = 0.3f;
        [Tooltip("Distance threshold to hand off to near grabber")]
        public float grabHandoffDistance = 0.15f;

        Grabbable _targetGrabbable;
        Grabbable _grabbingGrabbable;
        Grabber _grabber;
        bool _isFlying;
        float _flyTimer;
        Vector3 _flyStartPos;
        Quaternion _flyStartRot;


        public UnityEvent<Collider, Vector3> onHitEnter = new UnityEvent<Collider, Vector3>();
        public UnityEvent<Collider, Vector3> onHitExit = new UnityEvent<Collider, Vector3>();
        public UnityEvent<Collider, Vector3> onRelease = new UnityEvent<Collider, Vector3>();

        // Define if the beamer ray is active this frame
        public bool isRayEnabled = false;

        public IRayValidator rayValidator;

        public enum Status
        {
            NoBeam,
            BeamNoHit,
            BeamHit
        }
        public Status status = Status.NoBeam;

        public RayData ray;
        [HideInInspector]
        public Vector3 lastHit;
        Collider lastHitCollider = null;

        public RayData Ray => ray;


        public virtual void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = lineMaterial;
                lineRenderer.numCapVertices = 4;
            }
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;

            if (origin == null) origin = transform;
            if (hand == null) hand = GetComponentInParent<HardwareHand>();
            if (_grabber == null) _grabber = GetComponent<Grabber>();
        }

        public virtual void Start()
        {
#if ENABLE_INPUT_SYSTEM
            rayAction.EnableWithDefaultXRBindings(hand.side, new List<string> { "thumbstickClicked", "primaryButton", "secondaryButton" });
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
        }

        public bool BeamCast(out RaycastHit hitInfo, Vector3 origin, Vector3 direction)
        {
            Ray handRay = new Ray(origin, direction);
            return Physics.Raycast(handRay, out hitInfo, maxDistance, targetLayerMask);
        }

        public bool BeamCast(out RaycastHit hitInfo)
        {
            return BeamCast(out hitInfo, ray.origin, origin.forward);
        }

        public void Update()
        {
            if (_isFlying)
            {
                UpdateFly();
                return;
            }

#if ENABLE_INPUT_SYSTEM
            // If useRayActionInput is true, we read the rayAction to determine isRayEnabled for this frame
            //  Usefull for the mouse teleporter of the desktop mode, which disables the action reading to have its own logic to enable the beamer
            if (useRayActionInput && rayAction != null && rayAction.action != null)
            {
                isRayEnabled = rayAction.action.ReadValue<float>() == 1;
            }
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
            ray.isRayEnabled = isRayEnabled;
            if (ray.isRayEnabled)
            {
                ray.origin = origin.position;
                if (BeamCast(out RaycastHit hit))
                {
                    // Check if we hit a grabbable for distance grab
                    var grabbable = hit.collider.GetComponentInParent<Grabbable>();
                    bool canDistanceGrab = grabbable != null
                        && grabbable != _grabber?.grabbedObject
                        && hand != null && hand.isGrabbing;

                    if (canDistanceGrab)
                    {
                        // Distance grab: grip press while pointing at grabbable
                        StartFlyToHand(grabbable);
                        return;
                    }

                    // Highlight / unhighlight grabbable targets
                    UpdateGrabbableTarget(grabbable);

                    if (status == Status.BeamHit)
                    {
                        if (lastHitCollider != hit.collider)
                        {
                            OnHitExit(lastHitCollider, lastHit);
                            OnHitEnter(hit.collider, lastHit);
                        }
                    }
                    else
                    {
                        OnHitEnter(hit.collider, lastHit);
                    }
                    lastHitCollider = hit.collider;
                    ray.target = hit.point;
                    // Change beam color if pointing at a grabbable
                    ray.color = grabbable != null ? grabColor : hitColor;
                    lastHit = hit.point;
                    status = Status.BeamHit;
                    if (rayValidator != null) rayValidator.ValidateOnBeamerHit(this, hit);
                }
                else
                {
                    if (status == Status.BeamHit && lastHitCollider != null)
                    {
                        OnHitExit(lastHitCollider, lastHit);
                    }
                    ClearGrabbableTarget();
                    lastHitCollider = null;
                    ray.target = ray.origin + origin.forward * maxDistance;
                    ray.color = noHitColor;
                    status = Status.BeamNoHit;
                }
            }
            else
            {
                if (status == Status.BeamHit)
                {
                    OnHitRelease(lastHitCollider, lastHit);
                }
                ClearGrabbableTarget();
                status = Status.NoBeam;
                lastHitCollider = null;
            }

            UpdateRay();
        }

        #region Callbacks
        void OnHitEnter(Collider hitCollider, Vector3 hitPosition)
        {
            if (onHitEnter != null) onHitEnter.Invoke(hitCollider, lastHit);
        }

        void OnHitExit(Collider lastHitCollider, Vector3 lastHitPosition)
        {
            if (onHitExit != null) onHitExit.Invoke(lastHitCollider, lastHit);
        }

        void OnHitRelease(Collider lastHitCollider, Vector3 lastHitPosition)
        {
            if (onRelease != null) onRelease.Invoke(lastHitCollider, lastHit);
        }
        #endregion

        public void CancelHit()
        {
            status = Status.NoBeam;
        }

        #region Distance Grab
        void UpdateGrabbableTarget(Grabbable grabbable)
        {
            if (grabbable == _targetGrabbable) return;
            ClearGrabbableTarget();
            if (grabbable != null)
            {
                _targetGrabbable = grabbable;
                _targetGrabbable.SetHighlight(true);
            }
        }

        void ClearGrabbableTarget()
        {
            if (_targetGrabbable != null)
            {
                _targetGrabbable.ClearHighlight();
                _targetGrabbable = null;
            }
        }

        void StartFlyToHand(Grabbable grabbable)
        {
            _grabbingGrabbable = grabbable;
            ClearGrabbableTarget();

            // Disable beam during fly
            lineRenderer.enabled = false;

            grabbable.SetHighlight(true);

            _flyStartPos = grabbable.transform.position;
            _flyStartRot = grabbable.transform.rotation;
            _flyTimer = 0f;
            _isFlying = true;
        }

        void UpdateFly()
        {
            if (_grabbingGrabbable == null)
            {
                FinishFly();
                return;
            }

            _flyTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_flyTimer / flyDuration);
            // Smoothstep easing
            t = t * t * (3f - 2f * t);

            Vector3 targetPos = hand.transform.position;
            _grabbingGrabbable.transform.position = Vector3.Lerp(_flyStartPos, targetPos, t);
            _grabbingGrabbable.transform.rotation = Quaternion.Slerp(_flyStartRot, hand.transform.rotation, t);

            if (t >= 1f || Vector3.Distance(_grabbingGrabbable.transform.position, targetPos) < grabHandoffDistance)
            {
                FinishFly();
            }
        }

        void FinishFly()
        {
            _isFlying = false;
            var grabbable = _grabbingGrabbable;
            _grabbingGrabbable = null;

            if (grabbable == null)
            {
                isRayEnabled = false;
                return;
            }

            grabbable.ClearHighlight();

            // Hand off to the near grabber
            if (_grabber != null && _grabber.grabbedObject == null)
            {
                _grabber.Grab(grabbable);
            }

            // Turn off ray after grab
            isRayEnabled = false;
        }
        #endregion

        void UpdateRay()
        {
            lineRenderer.enabled = ray.isRayEnabled;
            if (ray.isRayEnabled)
            {
                lineRenderer.SetPositions(new Vector3[] { ray.origin, ray.target });
                lineRenderer.positionCount = 2;
                lineRenderer.startColor = ray.color;
                lineRenderer.endColor = ray.color;
            }
        }
    }
}
