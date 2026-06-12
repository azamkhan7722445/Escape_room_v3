using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Fusion.XR.Shared.Rig;

namespace Fusion.XR.Shared.Locomotion
{
    [RequireComponent(typeof(HardwareRig))]
    public class SmoothLocomotion : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        [Header("Joystick Input")]
        public InputActionProperty leftControllerMove;
        public InputActionProperty rightControllerMove;
#endif

        [Header("Controller Selection")]
        public bool useLeftController = true;
        public bool useRightController = false;

        [Header("Movement Settings")]
        public float moveSpeed = 1.5f;
        public float deadZone = 0.15f;

        [Header("Ground Validation (same as teleport)")]
        public LayerMask locomotionLayerMask;
        public float groundCheckDistance = 1.5f;

        [Header("NavMesh Validation")]
        public float navMeshDistanceTolerance = 0.5f;

        HardwareRig rig;
        NavMeshAgent agent;

        void Awake()
        {
            rig = GetComponent<HardwareRig>();
            agent = GetComponent<NavMeshAgent>();

            if (agent)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
            }

#if ENABLE_INPUT_SYSTEM
            var bindings = new System.Collections.Generic.List<string> { "joystick" };
            leftControllerMove.EnableWithDefaultXRBindings(leftBindings: bindings);
            rightControllerMove.EnableWithDefaultXRBindings(rightBindings: bindings);
#endif
        }

        void Update()
        {
            Vector2 input = Vector2.zero;

            if (WebXR.FusionBridge.WebXRFusionBridge.Active)
            {
                input = WebXR.FusionBridge.WebXRFusionBridge.GetMoveInput(useLeftController, useRightController);
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                if (useLeftController)
                    input += leftControllerMove.action.ReadValue<Vector2>();

                if (useRightController)
                    input += rightControllerMove.action.ReadValue<Vector2>();
#endif
            }

            if (input.magnitude < deadZone)
                return;

            TryMove(input);
        }

        void TryMove(Vector2 input)
        {
            Vector3 forward = rig.headset.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = rig.headset.transform.right;
            right.y = 0;
            right.Normalize();

            Vector3 delta = (forward * input.y + right * input.x) * moveSpeed * Time.deltaTime;

            Vector3 targetPosition = rig.transform.position + delta;

            // Headset target position
            Vector3 move = targetPosition - rig.transform.position;
            Vector3 newHeadsetPosition = rig.headset.transform.position + move;

            // Check headset position validity
            if (!IsValidHeadPosition(newHeadsetPosition))
                return;

            // Ground validation
            if (!IsValidGround(targetPosition))
                return;

            rig.transform.position = targetPosition;
        }

        bool IsValidGround(Vector3 targetPosition)
        {
            Vector3 rayOrigin = targetPosition + Vector3.up * 0.5f;

            return Physics.Raycast(
                rayOrigin,
                Vector3.down,
                groundCheckDistance,
                locomotionLayerMask
            );
        }

        bool IsValidHeadPosition(Vector3 targetPos)
        {
            // Collider check
            Collider[] headColliders = Physics.OverlapBox(
                targetPos,
                0.2f * Vector3.one,
                Quaternion.identity
            );

            foreach (var c in headColliders)
            {
                if (!c.isTrigger)
                    return false;
            }

            // NavMesh validation
            if (agent)
            {
                Ray ray = new Ray(targetPos, Vector3.down);

                if (Physics.Raycast(ray, out var hit, 100f, locomotionLayerMask))
                {
                    if (NavMesh.SamplePosition(
                        hit.point + Vector3.up * 0.1f,
                        out var navHit,
                        navMeshDistanceTolerance,
                        NavMesh.AllAreas))
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return true;
        }
    }
}