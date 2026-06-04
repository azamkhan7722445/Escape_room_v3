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
    [Header("Crack Image Sequences  (9 groups x 3 images each)")]
    public CrackImageSequence[] ImageSequence = new CrackImageSequence[9];

    [Header("Glass Material")]
    [Tooltip("Leave blank — auto-found by name 'Crack_ImagesSequance' on intactGlassMesh renderer")]
    public Renderer glassRenderer;
    public string crackMaterialName   = "Crack_ImagesSequance";
    public string crackTextureProperty = "_BaseMap";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip crackSound;
    public AudioClip breakSound;

    [Header("Meshes")]
    public GameObject intactGlassMesh;
    public GameObject breakGlassMesh;

    [Header("On Break Event")]
    public UnityEngine.Events.UnityEvent onBreak;

    [Header("Timer Settings")]
    [Tooltip("Seconds between each crack step  (default 300 = 5 minutes real-time)")]
    public float stepInterval = 300f;

    [Header("Debug Speed")]
    public Slider debugSlider;
    [Range(1f, 200f)]
    [Tooltip("Time multiplier — also driven by the UI Slider above")]
    public float debugSpeedMultiplier = 1f;

    // ── private state ─────────────────────────────────────────────────────────
    private float    elapsedTime;
    private int      currentStep;
    private Material crackedMatInstance;   // runtime instance on the renderer

    private const int TOTAL_STEPS = 9;

    private Coroutine crackSequenceCoroutine;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Stop any audio immediately — Play On Awake fires here, before Start()
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }
    }

    void Start()
    {
        elapsedTime = 0f;
        currentStep = 0;

        // Auto-grab renderer from intactGlassMesh if not manually assigned
        if (glassRenderer == null && intactGlassMesh != null)
            glassRenderer = intactGlassMesh.GetComponent<Renderer>();

        // Find the material named "Crack_ImagesSequance" on the renderer
        // renderer.materials creates runtime instances so we are safe to modify them
        if (glassRenderer != null)
        {
            foreach (Material m in glassRenderer.materials)
            {
                // Unity appends " (Instance)" to instanced material names at runtime
                string cleanName = m.name.Replace(" (Instance)", "").Trim();
                if (cleanName == crackMaterialName)
                {
                    crackedMatInstance = m;
                    break;
                }
            }

            if (crackedMatInstance == null)
                Debug.LogWarning($"[Crack_Break_Glass] No material named '{crackMaterialName}' found on renderer. Check the name in the Inspector.");
        }
        else
        {
            Debug.LogWarning("[Crack_Break_Glass] No Renderer found. Assign intactGlassMesh or glassRenderer.");
        }

        if (debugSlider != null)
        {
            debugSlider.onValueChanged.AddListener(OnSliderChanged);
            debugSpeedMultiplier = Mathf.Max(1f, debugSlider.value);
        }

        if (breakGlassMesh != null) breakGlassMesh.SetActive(false);
    }

    void Update()
    {
        if (currentStep >= TOTAL_STEPS) return;

        elapsedTime += Time.deltaTime * debugSpeedMultiplier;

        float stepThreshold = stepInterval * (currentStep + 1);

        if (elapsedTime >= stepThreshold)
        {
            currentStep++;

            if (currentStep >= TOTAL_STEPS)
                BreakGlass();
            else
                TriggerCrackEvent(currentStep - 1);   // 0-based group index
        }
    }

    // ── crack event ───────────────────────────────────────────────────────────

    void TriggerCrackEvent(int groupIndex)
    {
        if (audioSource != null && crackSound != null)
            audioSource.PlayOneShot(crackSound);

        if (crackSequenceCoroutine != null)
            StopCoroutine(crackSequenceCoroutine);

        crackSequenceCoroutine = StartCoroutine(PlayCrackSequence(groupIndex));
    }

    System.Collections.IEnumerator PlayCrackSequence(int groupIndex)
    {
        if (ImageSequence == null || groupIndex >= ImageSequence.Length) yield break;

        CrackImageSequence group = ImageSequence[groupIndex];
        Texture[] frames = { group.Child_01, group.Child_02, group.Child_03 };

        foreach (Texture tex in frames)
        {
            if (tex != null && crackedMatInstance != null)
                crackedMatInstance.SetTexture(crackTextureProperty, tex);

            yield return new WaitForSeconds(0.5f);   // 3 frames x 0.5 s = 1.5 s sequence
        }
    }

    // ── final break ───────────────────────────────────────────────────────────

    void BreakGlass()
    {
        if (intactGlassMesh != null) intactGlassMesh.SetActive(false);
        if (breakGlassMesh  != null) breakGlassMesh.SetActive(true);

        if (audioSource != null && breakSound != null)
            audioSource.PlayOneShot(breakSound);

        onBreak?.Invoke();

        enabled = false;
    }

    // ── slider callback ───────────────────────────────────────────────────────

    void OnSliderChanged(float value)
    {
        debugSpeedMultiplier = Mathf.Max(1f, value);
    }

    void OnDestroy()
    {
        if (debugSlider != null)
            debugSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }
}
