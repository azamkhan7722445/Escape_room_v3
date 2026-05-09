using UnityEngine;
using UnityEngine.Events;

public class ChestLock : MonoBehaviour
{
    [Header("Settings")]
    public string correctCombination = "6XXXXX"; // First digit is 6 from Puzzle 1
    public string currentEntry = "000000";

    [Header("Events")]
    public UnityEvent OnUnlocked;
    public UnityEvent OnFailed;

    public void SetDigit(int index, int value)
    {
        if (index < 0 || index >= currentEntry.Length) return;
        
        char[] digits = currentEntry.ToCharArray();
        digits[index] = value.ToString()[0];
        currentEntry = new string(digits);
        
        CheckCombination();
    }

    public void CheckCombination()
    {
        // Note: For now we only know the first digit is 6. 
        // We'll update this as more puzzles are completed.
        // In the final version, this will check against the full 6-digit code.
        if (currentEntry == "6XXXXX") // This is a placeholder logic
        {
            Debug.Log("First digit correct!");
        }

        // Final check (once all puzzles are known)
        // Correct code from doc: 6 (Scarab) - ? (Pillar) - 5 (Scale) - 9 (Maths) - 3 (Star) - 7 (Canopic)
        // wait, let me check the doc for the full code.
    }
}
