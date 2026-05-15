using System;
using Fusion.XR.Shared.Rig;

namespace Fusion.Addons.HapticAndAudioFeedback
{
    [Flags]
    public enum FeedbackMode
    {
        None = 0,
        Audio = 1,
        Haptic = 2,
        AudioAndHaptic = Audio | Haptic
    }

    public interface IFeedbackHandler
    {
        public const float USE_DEFAULT_VALUES = -1;
        void PlayAudioAndHapticFeeback(string audioType = null, float hapticAmplitude = -1, float hapticDuration = -1, HardwareHand hardwareHand = null, FeedbackMode feedbackMode = FeedbackMode.AudioAndHaptic, bool audioOverwrite = true);
        void StopAudioAndHapticFeeback(HardwareHand hardwareHand = null);
        void PlayAudioFeeback(string audioType);
        void StopAudioFeeback();
        void PlayHapticFeedback(HardwareHand hardwareHand = null, float hapticAmplitude = -1, float hapticDuration = -1);
    }
}
