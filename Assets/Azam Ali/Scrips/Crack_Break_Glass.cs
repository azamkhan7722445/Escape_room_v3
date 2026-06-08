using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CrackImageSequence
{
    public Texture Child_01;
    public Texture Child_02;
    public Texture Child_03;
}

public class Crack_Break_Glass : MonoBehaviour
{
    [Header("Crack Image Sequences (9 groups x 3 images each)")]
    public CrackImageSequence[] ImageSequence = new CrackImageSequence[9];

    [Header("Glass Material")]
    [Tooltip("Leave blank — auto-found by name 'Crack_ImagesSequance' on intactGlassMesh renderer")]
    public Renderer glassRenderer;
    public string crackMaterialName = "Crack_ImagesSequance";
    public string crackTextureProperty = "_BaseMap";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip crackSound;
    public AudioClip breakSound;

    [Header("Meshes")]
    public GameObject intactGlassMesh;
    public GameObject breakGlassMesh;

    [Header("Show After Completion")]
    public GameObject objectToShowAfterComplete;

    [Header("On Break Event")]
    public UnityEngine.Events.UnityEvent onBreak;

    [Header("Timer Settings")]
    [Tooltip("Seconds between each crack step (default 300 = 5 minutes real-time)")]
    public float stepInterval = 300f;

    [Header("Debug Speed")]
    public Slider debugSlider;

    [Range(1f, 200f)]
    [Tooltip("Time multiplier — also driven by the UI Slider above")]
    public float debugSpeedMultiplier = 1f;

    // Private State
    private float elapsedTime;
    private int currentStep;
    private Material crackedMatInstance;

    private const int TOTAL_STEPS = 9;
    private Coroutine crackSequenceCoroutine;


    [Header("Explosion Settings")]
    public float explosionForce = 10f;
    public float explosionRadius = 2f;
    public float upwardModifier = 0.5f;
    public Transform explosionCenter;

    private void Awake()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }
    }

    private void Start()
    {
        elapsedTime = 0f;
        currentStep = 0;

        // Auto assign renderer
        if (glassRenderer == null && intactGlassMesh != null)
            glassRenderer = intactGlassMesh.GetComponent<Renderer>();

        // Find crack material
        if (glassRenderer != null)
        {
            foreach (Material m in glassRenderer.materials)
            {
                string cleanName = m.name.Replace(" (Instance)", "").Trim();

                if (cleanName == crackMaterialName)
                {
                    crackedMatInstance = m;
                    break;
                }
            }

            if (crackedMatInstance == null)
            {
                Debug.LogWarning(
                    $"[Crack_Break_Glass] No material named '{crackMaterialName}' found."
                );
            }
        }
        else
        {
            Debug.LogWarning("[Crack_Break_Glass] No Renderer assigned.");
        }

        if (debugSlider != null)
        {
            debugSlider.onValueChanged.AddListener(OnSliderChanged);
            debugSpeedMultiplier = Mathf.Max(1f, debugSlider.value);
        }

        if (breakGlassMesh != null)
            breakGlassMesh.SetActive(false);

        if (objectToShowAfterComplete != null)
            objectToShowAfterComplete.SetActive(false);
    }

    private void Update()
    {
        if (currentStep >= TOTAL_STEPS)
            return;

        elapsedTime += Time.deltaTime * debugSpeedMultiplier;

        float stepThreshold = stepInterval * (currentStep + 1);

        if (elapsedTime >= stepThreshold)
        {
            currentStep++;

            if (currentStep >= TOTAL_STEPS)
            {
                BreakGlass();
            }
            else
            {
                TriggerCrackEvent(currentStep - 1);
            }
        }
    }

    private void TriggerCrackEvent(int groupIndex)
    {
        // Play crack sound when a new crack appears
        if (audioSource != null && crackSound != null)
            audioSource.PlayOneShot(crackSound);

        if (crackSequenceCoroutine != null)
            StopCoroutine(crackSequenceCoroutine);

        crackSequenceCoroutine = StartCoroutine(PlayCrackSequence(groupIndex));
    }

    private System.Collections.IEnumerator PlayCrackSequence(int groupIndex)
    {
        if (ImageSequence == null || groupIndex >= ImageSequence.Length)
            yield break;

        CrackImageSequence group = ImageSequence[groupIndex];

        Texture[] frames =
        {
            group.Child_01,
            group.Child_02,
            group.Child_03
        };

        foreach (Texture tex in frames)
        {
            if (tex != null && crackedMatInstance != null)
            {
                crackedMatInstance.SetTexture(crackTextureProperty, tex);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void BreakGlass()
    {
        if (intactGlassMesh != null)
            intactGlassMesh.SetActive(false);

       /* if (breakGlassMesh != null)
            breakGlassMesh.SetActive(true);*/

        if (breakGlassMesh != null)
        {
            breakGlassMesh.SetActive(true);
            ApplyBreakForce();
            // ApplyExplosionForce();
        }

        // Play final break sound
        if (audioSource != null && breakSound != null)
            audioSource.PlayOneShot(breakSound);

        // Show completion object
        if (objectToShowAfterComplete != null)
            objectToShowAfterComplete.SetActive(true);

        onBreak?.Invoke();

        enabled = false;
    }

    private void OnSliderChanged(float value)
    {
        debugSpeedMultiplier = Mathf.Max(1f, value);
    }

    private void OnDestroy()
    {
        if (debugSlider != null)
            debugSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }


    private void ApplyExplosionForce()
    {
        if (breakGlassMesh == null)
            return;

        Vector3 centerPos = explosionCenter != null
            ? explosionCenter.position
            : breakGlassMesh.transform.position;

        Rigidbody[] rigidbodies = breakGlassMesh.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = false; // Ensure physics is enabled
            rb.AddExplosionForce(
                explosionForce,
                centerPos,
                explosionRadius,
                upwardModifier,
                ForceMode.Impulse
            );
        }
    }
    private void ApplyBreakForce()
    {
        Rigidbody[] rigidbodies = breakGlassMesh.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = false;

            // Backward direction relative to the glass
            Vector3 forceDir = -breakGlassMesh.transform.right;

            // Add some randomness so pieces don't move identically
            forceDir += Random.insideUnitSphere * 0.5f;

            // Slight upward push
            forceDir += Vector3.up * 0.2f;

            rb.AddForce(forceDir.normalized * 8f, ForceMode.Impulse);

            // Optional rotation for better effect
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
    }
}