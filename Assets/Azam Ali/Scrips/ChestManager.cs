using System.Collections;
using UnityEngine;

public class ChestManager : MonoBehaviour
{
    [Header("Rotators")]
    public Transform[] rotators = new Transform[6];

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip openDoorSound;

    [Header("Lock Settings")]
    public string unlockCode = "123456";

    [Header("Chest")]
    public Transform chestDoor;

    private readonly int[] currentDigits = new int[6];
    private bool isUnlocked = false;

    private const float DegreesPerStep = 36f; // 360 / 10 digits

    public void OnRotatorClicked(int rotatorIndex)
    {
        if (isUnlocked) return;

        currentDigits[rotatorIndex] = (currentDigits[rotatorIndex] + 1) % 10;
        RotateRotator(rotatorIndex);
        PlayClick();
        CheckCode();
    }

    private void RotateRotator(int index)
    {
        if (rotators[index] == null) return;
        StartCoroutine(AnimateRotation(rotators[index], DegreesPerStep));
    }

    private void PlayClick()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    private void CheckCode()
    {
        if (unlockCode == null || unlockCode.Length != 6) return;

        for (int i = 0; i < 6; i++)
        {
            if (!char.IsDigit(unlockCode[i])) return;
            if (currentDigits[i] != (unlockCode[i] - '0')) return;
        }

        UnlockChest();
    }

    private void UnlockChest()
    {
        isUnlocked = true;

        if (chestDoor != null)
            StartCoroutine(AnimateRotation(chestDoor, 90f));

        if (audioSource != null && openDoorSound != null)
            audioSource.PlayOneShot(openDoorSound);
    }

    private IEnumerator AnimateRotation(Transform target, float degrees)
    {
        float elapsed = 0f;
        float duration = 0.3f;
        Quaternion startRot = target.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(degrees, 0f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.localRotation = Quaternion.Lerp(startRot, endRot, elapsed / duration);
            yield return null;
        }

        target.localRotation = endRot;
    }
}
