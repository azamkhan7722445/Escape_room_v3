using UnityEngine;
using Fusion.XR.Shared.Grabbing;

[RequireComponent(typeof(AudioSource))]
public class ParchmentSoundFeedback : MonoBehaviour
{
    public AudioClip grabSound;
    public AudioClip impactSound;

    private AudioSource audioSource;
    private NetworkGrabbable networkGrabbable;
    private Rigidbody rb;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        networkGrabbable = GetComponent<NetworkGrabbable>();
        rb = GetComponent<Rigidbody>();

        if (networkGrabbable != null)
        {
            networkGrabbable.onDidGrab.AddListener(OnGrab);
        }
    }

    private void OnGrab(NetworkGrabber grabber)
    {
        if (grabSound != null)
        {
            audioSource.PlayOneShot(grabSound);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't play impact sound if it's currently being grabbed
        if (networkGrabbable != null && networkGrabbable.IsGrabbed)
            return;

        // Only play if the relative velocity is high enough to avoid sound spamming on tiny movements
        if (collision.relativeVelocity.magnitude > 0.5f)
        {
            if (impactSound != null)
            {
                audioSource.PlayOneShot(impactSound);
            }
        }
    }
}
