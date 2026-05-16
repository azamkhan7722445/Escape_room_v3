using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class PuzzleSixController : NetworkBehaviour
{
    public List<CanopicJarSlot> slots;
    public GameObject successObject; // The number 7 or the panel to open
    public GameObject panelToOpen;   // The panel that should open

    [Networked] public NetworkBool isCompleted { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (isCompleted) return;

        if (Object != null && Object.HasStateAuthority)
        {
            if (CheckCompletion())
            {
                isCompleted = true;
                PuzzleManager.Instance.CompletePuzzleSix();
                RPC_OnSuccess();
            }
        }
    }

    private bool CheckCompletion()
    {
        if (slots == null || slots.Count < 4) return false;

        foreach (var slot in slots)
        {
            if (!slot.IsCorrect()) return false;
        }
        return true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnSuccess()
    {
        if (successObject != null) successObject.SetActive(true);
        if (panelToOpen != null) panelToOpen.SetActive(false); // Or trigger animation
        Debug.Log("Puzzle 6 Solved!");
    }
}
