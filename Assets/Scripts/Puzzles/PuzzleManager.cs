using UnityEngine;
using Fusion;

public class PuzzleManager : NetworkBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Networked] public NetworkBool puzzleOneCompleted { get; set; }
    [Networked] public NetworkBool puzzleThreeCompleted { get; set; }
    [Networked] public NetworkBool puzzleSixCompleted { get; set; }
    [Networked] public NetworkBool puzzleSevenCompleted { get; set; }

    private void Awake()
{
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CompletePuzzleOne()
    {
        // Only the authority should update networked state
        if (Object != null && Object.HasStateAuthority)
        {
            puzzleOneCompleted = true;
            Debug.Log("Puzzle One Completed!");
        }
    }

    public void CompletePuzzleThree()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            puzzleThreeCompleted = true;
            Debug.Log("Puzzle Three Completed!");
        }
    }

    public void CompletePuzzleSix()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            puzzleSixCompleted = true;
            Debug.Log("Puzzle Six Completed!");
        }
    }

    public void CompletePuzzleSeven()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            puzzleSevenCompleted = true;
            Debug.Log("Puzzle Seven Completed!");
        }
    }
    }
