using Fusion;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CrackImageSequence
{
    public Texture Child_01;
    public Texture Child_02;
    public Texture Child_03;
}

public class Crack_Break_Glass : NetworkBehaviour
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

    [Header("Explosion Settings")]
    public float explosionForce = 10f;
    public float explosionRadius = 2f;
    public float upwardModifier = 0.5f;
    public Transform explosionCenter;

    [Networked] public float ElapsedTime { get; set; }
    [Networked] public int CurrentStep { get; set; }
    [Networked] public NetworkBool IsBroken { get; set; }

    public float SyncedElapsedTime => Object != null && Object.IsValid ? ElapsedTime : _localElapsedTime;

    private float _localElapsedTime;
    private int _localCurrentStep;
    private bool _localBroken;
    private Material crackedMatInstance;
    private Coroutine crackSequenceCoroutine;
    private ChangeDetector _changeDetector;
    private int _lastRenderedStep;
    private bool _breakVisualsApplied;

    private const int TOTAL_STEPS = 9;

    private void Awake()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        InitializeMaterials();
        ApplyStateFromNetwork(force: true);
    }

    private void Start()
    {
        if (Object == null || !Object.IsValid)
        {
            InitializeMaterials();

            if (breakGlassMesh != null)
                breakGlassMesh.SetActive(false);

            if (objectToShowAfterComplete != null)
                objectToShowAfterComplete.SetActive(false);
        }

        if (debugSlider != null)
        {
            debugSlider.onValueChanged.AddListener(OnSliderChanged);
            debugSpeedMultiplier = Mathf.Max(1f, debugSlider.value);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsBroken)
            return;

        if (Object.HasStateAuthority)
        {
            ElapsedTime += Runner.DeltaTime * debugSpeedMultiplier;

            int targetStep = GetStepForElapsedTime(ElapsedTime);
            if (targetStep > CurrentStep)
            {
                CurrentStep = targetStep;

                if (CurrentStep >= TOTAL_STEPS)
                    IsBroken = true;
            }
        }
    }

    public override void Render()
    {
        ApplyStateFromNetwork(force: false);
    }

    private void Update()
    {
        if (Object != null && Object.IsValid)
            return;

        if (_localBroken)
            return;

        _localElapsedTime += Time.deltaTime * debugSpeedMultiplier;

        int targetStep = GetStepForElapsedTime(_localElapsedTime);
        if (targetStep > _localCurrentStep)
        {
            _localCurrentStep = targetStep;

            if (_localCurrentStep >= TOTAL_STEPS)
            {
                _localBroken = true;
                ApplyBreakVisuals();
            }
            else
            {
                TriggerCrackEvent(_localCurrentStep - 1);
            }
        }
    }

    private void InitializeMaterials()
    {
        if (glassRenderer == null && intactGlassMesh != null)
            glassRenderer = intactGlassMesh.GetComponent<Renderer>();

        if (glassRenderer == null)
        {
            Debug.LogWarning("[Crack_Break_Glass] No Renderer assigned.");
            return;
        }

        if (crackedMatInstance != null)
            return;

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

    private void ApplyStateFromNetwork(bool force)
    {
        if (IsBroken)
        {
            if (!_breakVisualsApplied || force)
                ApplyBreakVisuals();
            return;
        }

        if (force)
        {
            for (int step = 1; step <= CurrentStep && step < TOTAL_STEPS; step++)
                ApplyCrackFrameInstant(step - 1);

            _lastRenderedStep = CurrentStep;
            return;
        }

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CurrentStep) && CurrentStep > _lastRenderedStep)
            {
                if (CurrentStep < TOTAL_STEPS)
                    TriggerCrackEvent(CurrentStep - 1);

                _lastRenderedStep = CurrentStep;
            }
        }
    }

    private int GetStepForElapsedTime(float elapsed)
    {
        for (int step = TOTAL_STEPS; step >= 1; step--)
        {
            if (elapsed >= stepInterval * step)
                return step;
        }

        return 0;
    }

    private void TriggerCrackEvent(int groupIndex)
    {
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
                crackedMatInstance.SetTexture(crackTextureProperty, tex);

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void ApplyCrackFrameInstant(int groupIndex)
    {
        if (ImageSequence == null || groupIndex >= ImageSequence.Length || crackedMatInstance == null)
            return;

        Texture finalFrame = ImageSequence[groupIndex].Child_03
            ?? ImageSequence[groupIndex].Child_02
            ?? ImageSequence[groupIndex].Child_01;

        if (finalFrame != null)
            crackedMatInstance.SetTexture(crackTextureProperty, finalFrame);
    }

    private void ApplyBreakVisuals()
    {
        if (_breakVisualsApplied)
            return;

        _breakVisualsApplied = true;

        if (intactGlassMesh != null)
            intactGlassMesh.SetActive(false);

        if (breakGlassMesh != null)
        {
            breakGlassMesh.SetActive(true);
            ApplyBreakForce();
        }

        if (audioSource != null && breakSound != null)
            audioSource.PlayOneShot(breakSound);

        if (objectToShowAfterComplete != null)
            objectToShowAfterComplete.SetActive(true);

        onBreak?.Invoke();
    }

    private void OnSliderChanged(float value)
    {
        if (Object != null && Object.IsValid && !Object.HasStateAuthority)
            return;

        debugSpeedMultiplier = Mathf.Max(1f, value);
    }

    private void OnDestroy()
    {
        if (debugSlider != null)
            debugSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void ApplyBreakForce()
    {
        if (breakGlassMesh == null)
            return;

        int seed = Object != null && Object.IsValid ? (int)Runner.Tick : 0;
        var rng = new System.Random(seed);

        Rigidbody[] rigidbodies = breakGlassMesh.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = false;

            Vector3 forceDir = -breakGlassMesh.transform.right;
            forceDir += RandomUnitSphere(rng) * 0.5f;
            forceDir += Vector3.up * 0.2f;

            rb.AddForce(forceDir.normalized * 8f, ForceMode.Impulse);
            rb.AddTorque(RandomUnitSphere(rng) * 5f, ForceMode.Impulse);
        }
    }

    private static Vector3 RandomUnitSphere(System.Random rng)
    {
        float u = (float)rng.NextDouble();
        float v = (float)rng.NextDouble();
        float theta = 2f * Mathf.PI * u;
        float phi = Mathf.Acos(2f * v - 1f);
        float sinPhi = Mathf.Sin(phi);

        return new Vector3(
            sinPhi * Mathf.Cos(theta),
            sinPhi * Mathf.Sin(theta),
            Mathf.Cos(phi)
        );
    }
}
