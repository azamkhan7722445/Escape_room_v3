using UnityEngine;
using UnityEngine.UI;

public class PuzzleOneButton : MonoBehaviour
{
    public void OnDonePressed()
    {
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.CompletePuzzleOne();
        }
    }
}
