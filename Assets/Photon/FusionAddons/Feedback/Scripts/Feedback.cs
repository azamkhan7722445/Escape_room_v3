using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using UnityEngine;

namespace Fusion.Addons.HapticAndAudioFeedback
{
    /***
     * 
     * Feedback manages the audio and haptic feedbacks for NetworkGrabbable
     * It provides methods to :
     *  - start/pause/stop playing audio feedback only
     *  - start playing audio and haptic feeback in the same time
     * If the audio source is not defined or not find on the object, Feedback uses the SoundManager audio source.
     * 
     ***/
    public class Feedback : MonoBehaviour, IFeedbackHandler
    {
        public bool EnableAudioFeedback = true;
        public bool EnableHapticFeedback = true;

        public AudioSource audioSource;
        private SoundManager soundManager;

        [Header("Haptic feedback")]
        public float defaultHapticAmplitude = 0.2f;
        public float defaultHapticDuration = 0.05f;

        NetworkGrabbable grabbable;
        public bool IsGrabbed => grabbable.IsGrabbed;
        public bool IsGrabbedByLocalPLayer => IsGrabbed && grabbable.CurrentGrabber.Object.StateAuthority == grabbable.CurrentGrabber.Object.Runner.LocalPlayer;

        private void Awake()
        {
            grabbable = GetComponent<NetworkGrabbable>();
        }

        void Start()
        {
            if (soundManager == null) soundManager = SoundManager.FindInstance();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null && soundManager)
                audioSource = soundManager.GetComponent<AudioSource>();
            if (audioSource == null)
                Debug.LogError("AudioSource not found");
        }

        HardwareHand GrabbingHand()
        {
            if (grabbable != null)
            {
                if (IsGrabbedByLocalPLayer && grabbable.CurrentGrabber.hand && grabbable.CurrentGrabber.hand.LocalHardwareHand != null)
                {
                    return grabbable.CurrentGrabber.hand.LocalHardwareHand;
                }
            }
            return null;
        }

        #region IFeedbackHandler
        public void PlayAudioAndHapticFeeback(string audioType = null, float hapticAmplitude = -1, float hapticDuration = -1, HardwareHand hardwareHand = null, FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic, bool audioOverwrite = true)
        {
            if ((feedbackMode & FeedbackMode.Audio) != 0)
            {
                if (IsAudioFeedbackIsPlaying() == false || audioOverwrite == true)
                    PlayAudioFeeback(audioType);
            }

            if ((feedbackMode & FeedbackMode.Haptic) != 0)
            {
                PlayHapticFeedback(hardwareHand, hapticAmplitude, hapticDuration);
            }
        }

        public void StopAudioAndHapticFeeback(HardwareHand hardwareHand = null)
        {
            StopAudioFeeback();
            StopHapticFeedback(hardwareHand);
        }
        #endregion

        #region IAudioFeedbackHandler
        public void PlayAudioFeeback(string audioType)
        {
            if (EnableAudioFeedback == false) return;

            if (audioSource && audioSource.isPlaying == false && soundManager)
                soundManager.Play(audioType, audioSource);
        }

        public void StopAudioFeeback()
        {
            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();
        }

        public void PauseAudioFeeback()
        {
            if (audioSource && audioSource.isPlaying)
                audioSource.Pause();
        }

        public bool IsAudioFeedbackIsPlaying()
        {
            return audioSource && audioSource.isPlaying;
        }
        #endregion

        #region IHapticFeedbackHandler
        public void PlayHapticFeedback(HardwareHand hardwareHand = null, float hapticAmplitude = -1, float hapticDuration = -1)
        {
            if (hapticAmplitude == IFeedbackHandler.USE_DEFAULT_VALUES) hapticAmplitude = defaultHapticAmplitude;
            if (hapticDuration == IFeedbackHandler.USE_DEFAULT_VALUES) hapticDuration = defaultHapticDuration;
            if (hardwareHand == null)
            {
                hardwareHand = GrabbingHand();

            }
            if (EnableHapticFeedback == false || hardwareHand == null) return;
            hardwareHand.SendHapticImpulse(amplitude: hapticAmplitude, duration: hapticDuration);
        }

        public void StopHapticFeedback(HardwareHand hardwareHand = null)
        {
            if (hardwareHand == null) return;

            hardwareHand.StopHaptics();
        }
        #endregion
    }
}
