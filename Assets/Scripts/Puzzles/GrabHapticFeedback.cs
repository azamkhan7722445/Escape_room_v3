using UnityEngine;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;

namespace Fusion.XR.Shared.Puzzles
{
    [RequireComponent(typeof(Grabbable))]
    public class GrabHapticFeedback : MonoBehaviour
    {
        [Header("Haptic Settings")]
        public float amplitude = 0.4f;
        public float duration = 0.08f;

        private Grabbable _grabbable;

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            if (_grabbable != null)
            {
                _grabbable.onGrab.AddListener(OnGrabbed);
            }
        }

        private void OnGrabbed()
        {
            if (_grabbable != null && _grabbable.currentGrabber != null)
            {
                // Find the HardwareHand associated with the grabber
                // Note: Grabber.cs keeps 'hand' private, so we look for it in the parent
                var hardwareHand = _grabbable.currentGrabber.GetComponentInParent<HardwareHand>();
                if (hardwareHand != null)
                {
                    hardwareHand.SendHapticImpulse(amplitude, duration);
                }
            }
        }

        private void OnDestroy()
        {
            if (_grabbable != null)
            {
                _grabbable.onGrab.RemoveListener(OnGrabbed);
            }
        }
    }
}
