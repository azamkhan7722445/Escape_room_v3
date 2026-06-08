using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Samples.IndustriesComponents;
using Fusion.Addons.HapticAndAudioFeedback;
using Fusion.XR.Shared;

namespace Fusion.Samples.IndustriesComponents
{
    public class BackToAvatarLoader : MonoBehaviour
    {
        public async void ReturnToAvatarSelection()
        {
            Managers managers = Managers.FindInstance();
            if (managers == null)
            {
                Debug.LogError("Managers instance not found.");
                return;
            }

            if (managers.applicationManager != null)
            {
                managers.applicationManager.isQuitting = true;
            }

            if (managers.soundManager != null)
            {
                managers.soundManager.PlayOneShot("OnSceneSwitch");
            }

            if (managers.runner != null)
            {
                await managers.runner.Shutdown(true);
            }

            Debug.Log("Loading AvatarSelection scene...");
            SceneManager.LoadScene("AvatarSelection", LoadSceneMode.Single);
        }
    }
}
