using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Collections;

public class PuzzleSevenController : NetworkBehaviour
{
    public List<SymbolSlot> slots;
    public string correctWord = "ANKH";
    public GameObject finalDoor,slot1,slot2,slot3,slot4,apoint,npoint,kpoint,hpoint;
    public List<Light> roomLights;
    public AudioSource successAudioSource;
    public AudioClip alarmSound;
    public AudioClip announcementSound;

    [Networked] public NetworkBool isCompleted { get; set; }

    public ZAxisDoor leftSideDoor;
    public ZAxisDoor rightSideDoor;

    public override void FixedUpdateNetwork()
    {
        if (isCompleted) return;

        if (Object != null && Object.HasStateAuthority)
        {
            if (CheckCompletion())
            {
                isCompleted = true;
                if (leftSideDoor != null) leftSideDoor.IsOpen = true;
                if (rightSideDoor != null) rightSideDoor.IsOpen = true;
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
            //finalDoor.SetActive(false); 
            slot1.SetActive(false);
            slot2.SetActive(false);
            slot3.SetActive(false);
            slot4.SetActive(false);
            apoint.SetActive(false);
            npoint.SetActive(false);
            kpoint.SetActive(false);
            hpoint.SetActive(false);
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
