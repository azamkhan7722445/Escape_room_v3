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
                _grabbable.onWillGrab.AddListener(OnWillGrab);
            }
        }

        private void OnWillGrab(Grabber grabber)
        {
            if (grabber != null)
            {
                // Find the HardwareHand associated with the grabber
                var hardwareHand = grabber.GetComponentInParent<HardwareHand>();
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
                _grabbable.onWillGrab.RemoveListener(OnWillGrab);
            }
        }
    }
}
