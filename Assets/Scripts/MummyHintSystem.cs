using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.Touch;

public class MummyHintSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject canvasRoot;
    public GameObject hintPanel;
    public TextMeshProUGUI puzzleTitleText; // New field for Puzzle Number
    public TextMeshProUGUI hintText;
    public UnityEngine.UI.Button[] puzzleButtons;

    [Header("Settings")]
    public float interactionDistance = 2.0f; // Distance to auto-enable UI
    public Transform playerTransform;

    [Header("Hint Data")]
    [TextArea(3, 10)]
    public string[] puzzleHints = new string[6]
    {
        "The Pharaoh's sacred beetle always faces the rising sun... look for those facing East.",
        "SEEK THE EYE OF HORUS UPON THE FOUR PILLARS... one pillar holds the secret number.",
        "Balance the Scale of Ma'at: Hearts on the left, feathers on the right... pure hearts are lighter than they appear.",
        "Mathematical tablet: Count the base blocks of all 4 sides, then divide by the pyramids on the plateau...",
        "The Soul of Osiris (Orion) points the way... count the stars in his belt to find the direction.",
        "The embalmers had a sacred order: Liver, Lungs, Stomach, then Intestines. Sequence them correctly."
    };

    private void Start()
    {
        // Hide UI by default
        if (canvasRoot != null) canvasRoot.SetActive(false);

        // Assign button listeners
        for (int i = 0; i < puzzleButtons.Length; i++)
        {
            int index = i;
            puzzleButtons[i].onClick.AddListener(() => ShowHint(index));
        }

        // Add interaction to mummy if present (for VR touch)
        Touchable touchable = GetComponent<Touchable>();
        if (touchable != null)
        {
            touchable.onTouch.AddListener(OpenUI);
        }

        // Try to find player if not assigned
        if (playerTransform == null)
        {
            GameObject rig = GameObject.Find("HardwareRig");
            if (rig != null) playerTransform = rig.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        
        // Auto-enable UI when close, disable when far
        if (dist <= interactionDistance)
        {
            if (canvasRoot != null && !canvasRoot.activeSelf)
            {
                OpenUI();
            }
        }
        else
        {
            if (canvasRoot != null && canvasRoot.activeSelf)
            {
                ClosePanel();
            }
        }
    }

    public void OpenUI()
    {
        if (canvasRoot != null) canvasRoot.SetActive(true);
        if (puzzleTitleText != null) puzzleTitleText.text = "Ancient Guide";
        hintText.text = "Greetings, traveler. I hold the secrets of the pyramid. Choose a puzzle to learn more...";
    }

    public void ShowHint(int index)
    {
        if (index >= 0 && index < puzzleHints.Length)
        {
            if (puzzleTitleText != null) puzzleTitleText.text = "Puzzle " + (index + 1);
            hintText.text = puzzleHints[index];
            hintPanel.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }
}
