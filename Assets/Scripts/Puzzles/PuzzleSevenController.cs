using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Collections;

public class PuzzleSevenController : NetworkBehaviour
{
    public List<SymbolSlot> slots;
    public string correctWord = "ANKH";
    public GameObject finalDoor;
    public List<Light> roomLights;
    public AudioSource successAudioSource;
    public AudioClip alarmSound;
    public AudioClip announcementSound;

    [Networked] public NetworkBool isCompleted { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (isCompleted) return;

        if (Object != null && Object.HasStateAuthority)
        {
            if (CheckCompletion())
            {
                isCompleted = true;
                if (PuzzleManager.Instance != null) PuzzleManager.Instance.CompletePuzzleSeven();
                RPC_OnSuccess();
            }
        }
    }

    private bool CheckCompletion()
    {
        if (slots == null || slots.Count != 4) return false;

        for (int i = 0; i < 4; i++)
        {
            if (!slots[i].IsCorrect(correctWord[i])) return false;
        }
        return true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnSuccess()
    {
        if (finalDoor != null)
        {
            finalDoor.SetActive(false); 
        }

        foreach (var light in roomLights)
        {
            if (light != null)
            {
                light.color = new Color(1f, 0.8f, 0.4f); // Golden sunrise
                light.intensity *= 2f;
            }
        }

        if (successAudioSource != null)
        {
            StartCoroutine(PlaySuccessSequence());
        }

        Debug.Log("Puzzle 7 Solved!");
    }

    private IEnumerator PlaySuccessSequence()
    {
        if (alarmSound != null)
        {
            successAudioSource.PlayOneShot(alarmSound);
            yield return new WaitForSeconds(alarmSound.length);
        }
        if (announcementSound != null)
        {
            successAudioSource.PlayOneShot(announcementSound);
        }
    }
}
