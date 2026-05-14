using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    public bool puzzleOneCompleted = false;

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
        puzzleOneCompleted = true;
        Debug.Log("Puzzle One Completed!");
    }
}
