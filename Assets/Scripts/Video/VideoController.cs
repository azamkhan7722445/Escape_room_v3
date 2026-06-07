using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

namespace EscapeRoom.Video
{
    public class VideoController : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private GameObject controlsContainer;
        [SerializeField] private float hideDelay = 3f;

        private Coroutine hideCoroutine;

        private void Awake()
        {
            if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
            UpdateIcon();
            
            // Start with controls hidden if desired, or let the user decide.
            // For "not continuously on screen", we start by showing then hiding, or just hidden.
            if (controlsContainer != null)
            {
                //controlsContainer.SetActive(false);
            }
        }

        public void TogglePlayPause()
        {
            if (videoPlayer == null) return;

            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            else
            {
                videoPlayer.Play();
            }
            UpdateIcon();
            //ResetHideTimer();
        }

        public void ShowControls()
        {
            if (controlsContainer == null) return;
            
            controlsContainer.SetActive(true);
            ResetHideTimer();
        }

        private void ResetHideTimer()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            //hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(hideDelay);
            if (controlsContainer != null)
            {
                controlsContainer.SetActive(false);
            }
            hideCoroutine = null;
        }

        private void UpdateIcon()
        {
            if (buttonImage == null || videoPlayer == null) return;
            
            if (videoPlayer.isPlaying)
            {
                if (pauseIcon != null) buttonImage.sprite = pauseIcon;
            }
            else
            {
                if (playIcon != null) buttonImage.sprite = playIcon;
            }
        }
    }
}
