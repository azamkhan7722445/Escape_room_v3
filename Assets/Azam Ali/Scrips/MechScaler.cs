using UnityEngine;

public class MechScaler : MonoBehaviour
{
    [Header("Mech Object")]
    public GameObject mechObject;

    [Header("Y Rise Range")]
    [Tooltip("Starting local Y position")]
    public float minY = 0f;
    [Tooltip("Final local Y position reached at 45 minutes")]
    public float maxY = 5f;

    [Header("Glass Script Reference  (shares its speed multiplier)")]
    [Tooltip("Drag the GlassCrack_Manager here — MechScaler uses its Debug Speed Multiplier")]
    public Crack_Break_Glass glassScript;

    // ── private state ─────────────────────────────────────────────────────────
    private float elapsedTime;
    private bool  active = true;

    // Total duration is read from glassScript (stepInterval x 9 steps) so both are always in sync

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        elapsedTime = 0f;

        if (mechObject != null)
        {
            Vector3 pos = mechObject.transform.localPosition;
            pos.y = minY;
            mechObject.transform.localPosition = pos;
        }
    }

    void Update()
    {
        if (!active || mechObject == null) return;

        float multiplier = glassScript != null ? glassScript.debugSpeedMultiplier : 1f;

        elapsedTime += Time.deltaTime * multiplier;

        float totalDuration = glassScript != null ? glassScript.stepInterval * 9f : 2700f;
        float t = Mathf.Clamp01(elapsedTime / totalDuration);

        Vector3 pos = mechObject.transform.localPosition;
        pos.y = Mathf.Lerp(minY, maxY, t);
        mechObject.transform.localPosition = pos;
    }

    // Called by Crack_Break_Glass onBreak event
    public void StopScaling()
    {
        active = false;

        if (mechObject != null)
        {
            Vector3 pos = mechObject.transform.localPosition;
            pos.y = maxY;
            mechObject.transform.localPosition = pos;
        }
    }
}
