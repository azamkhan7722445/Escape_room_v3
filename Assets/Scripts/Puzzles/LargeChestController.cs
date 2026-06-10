using UnityEngine;
using Fusion;

public class LargeChestController : NetworkBehaviour
{
    [Header("Components")]
    public LargeChestKnob[] knobs;
    public Transform chestLid;
    public AudioSource audioSource;

    [Header("Sounds")]
    public AudioClip chestOpenSound;
    
    [Header("Settings")]
    public int[] correctCombination = new int[6];
    public Vector3 openRotation = new Vector3(-90, 0, 0);
    public float openSpeed = 2f;

    [Networked, OnChangedRender(nameof(OnIsOpenChanged))]
    public NetworkBool IsOpen { get; set; }

    private Quaternion _closedRotation;

    public override void Spawned()
    {
        if (chestLid != null)
        {
            _closedRotation = chestLid.localRotation;
        }
    }

    void OnIsOpenChanged()
    {
        if (IsOpen && audioSource != null && chestOpenSound != null)
        {
            audioSource.PlayOneShot(chestOpenSound);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && !IsOpen)
        {
            if (CheckCombination())
            {
                IsOpen = true;
                Debug.Log("Chest Unlocked!");
            }
        }
    }

    private bool CheckCombination()
    {
        if (knobs == null || knobs.Length != correctCombination.Length) return false;

        for (int i = 0; i < knobs.Length; i++)
        {
            if (knobs[i] == null || knobs[i].CurrentDigit != correctCombination[i])
            {
                return false;
            }
        }
        return true;
    }

    public override void Render()
    {
        if (chestLid != null)
        {
            Quaternion targetRot = IsOpen ? _closedRotation * Quaternion.Euler(openRotation) : _closedRotation;
            chestLid.localRotation = Quaternion.Slerp(chestLid.localRotation, targetRot, Time.deltaTime * openSpeed);
        }
    }
}
