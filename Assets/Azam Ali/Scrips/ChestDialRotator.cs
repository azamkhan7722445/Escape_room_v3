using UnityEngine;
using Fusion.XR.Shared.Grabbing;

/// <summary>
/// Subclasses Grabbable so NetworkGrabbable keeps working,
/// but overrides Follow() to rotate the dial instead of moving it with the hand.
/// Grab and drag hand DOWN to increase digit, UP to decrease.
/// </summary>
public class ChestDialRotator : Grabbable
{
    [Header("Dial Settings")]
    public ChestManager chestManager;
    public int rotatorIndex;

    [Tooltip("Degrees of rotation per meter of vertical hand movement")]
    public float degreesPerMeter = 720f;

    private float m_GrabHandY;
    private float m_BaseDialAngle;
    private int m_LastStepDigit;

    private const float k_DegreesPerStep = 36f;

    protected override void Awake()
    {
        base.Awake();
        expectedIsKinematic = true;
        if (rb) rb.isKinematic = true;
    }

    void Start()
    {
        SnapToNearest();
    }

    public override void Grab(Grabber newGrabber, Transform grabPointTransform = null)
    {
        base.Grab(newGrabber, grabPointTransform);
        m_GrabHandY = newGrabber.transform.position.y;
        m_BaseDialAngle = GetCurrentAngle();
        m_LastStepDigit = AngleToDigit(m_BaseDialAngle);
    }

    public override void Ungrab()
    {
        SnapToNearest();
        chestManager?.SetDigit(rotatorIndex, AngleToDigit(GetCurrentAngle()));
        base.Ungrab();
    }

    // NetworkGrabbable calls this in FixedUpdateNetwork + Render; Grabbable calls it in Update.
    // We rotate instead of following position.
    public override void Follow(Transform followedTransform, Vector3 localPositionOffsetToFollowed, Quaternion localRotationOffsetToFollowed)
    {
        float deltaY = m_GrabHandY - followedTransform.position.y;
        float newAngle = m_BaseDialAngle + deltaY * degreesPerMeter;

        var euler = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(newAngle, euler.y, euler.z);

        int step = AngleToDigit(newAngle);
        if (step != m_LastStepDigit)
        {
            m_LastStepDigit = step;
            PlayClick();
        }
    }

    void SnapToNearest()
    {
        float snapped = Mathf.Round(GetCurrentAngle() / k_DegreesPerStep) * k_DegreesPerStep;
        var euler = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(snapped, euler.y, euler.z);
    }

    void PlayClick()
    {
        if (chestManager != null && chestManager.audioSource != null && chestManager.clickSound != null)
            chestManager.audioSource.PlayOneShot(chestManager.clickSound);
    }

    float GetCurrentAngle()
    {
        float angle = transform.localEulerAngles.x;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    int AngleToDigit(float angle)
    {
        float normalized = ((angle % 360f) + 360f) % 360f;
        return Mathf.RoundToInt(normalized / k_DegreesPerStep) % 10;
    }
}
