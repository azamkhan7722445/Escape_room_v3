using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.Touch;
using System.Collections;

public class MummyHintSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject canvasRoot;
    public GameObject hintPanel;
    public TextMeshProUGUI puzzleTitleText;
    public TextMeshProUGUI hintText;
    public UnityEngine.UI.Button[] puzzleButtons;

    [Header("Audio & Animation")]
    public AudioSource audioSource;
    public AudioClip[] hintClips;
    public Animator animator;
    public string idleStateName = "Standing Idle";
    public string talkStateName = "Talking";

    [Header("Settings")]
    public float interactionDistance = 2.0f;
    public Transform playerTransform;

    [Header("Hint Data")]
    [TextArea(3, 10)]
    public string[] puzzleHints = new string[6]
    {
        "The Pharaoh's sacred beetle always faces the rising sun... look for those facing East.",
        "Use the Rosetta Stone Card to unlock the symbols and find the hint sentence.",
        "Balance the Scale of Ma'at: Hearts on the left, feathers on the right... pure hearts are lighter than they appear.",
        "Mathematical tablet: Count the base blocks of all 4 sides, then divide by the pyramids on the plateau...",
        "The Soul of Osiris (Orion) points the way... count the stars in his belt to find the direction.",
        "The embalmers had a sacred order: Liver, Lungs, Stomach, then Intestines. Sequence them correctly."
    };

    private Coroutine hintCoroutine;
    private Transform hipsBone;
    private float initialHipsY;

    private void Start()
    {
        if (canvasRoot != null) canvasRoot.SetActive(false);

        // Find Hips bone to lock height
        hipsBone = FindChildRecursive(transform, "Hips");
        if (hipsBone != null)
        {
            initialHipsY = hipsBone.localPosition.y;
        }

        for (int i = 0; i < puzzleButtons.Length; i++)
{
            int index = i;
            puzzleButtons[i].onClick.AddListener(() => ShowHint(index));
        }

        Touchable touchable = GetComponent<Touchable>();
        if (touchable != null)
        {
            touchable.onTouch.AddListener(OpenUI);
        }

        if (playerTransform == null)
        {
            GameObject rig = GameObject.Find("HardwareRig");
            if (rig != null) playerTransform = rig.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        
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

    private void LateUpdate()
    {
        // Force Hips to stay at initial height to prevent animation sinking
        if (hipsBone != null)
        {
            Vector3 lp = hipsBone.localPosition;
            lp.y = initialHipsY;
            hipsBone.localPosition = lp;
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name)) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
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

            if (hintCoroutine != null) StopCoroutine(hintCoroutine);
            hintCoroutine = StartCoroutine(PlayHintSequence(index));
        }
    }

    private IEnumerator PlayHintSequence(int index)
    {
        if (audioSource != null && index < hintClips.Length && hintClips[index] != null)
        {
            audioSource.clip = hintClips[index];
            audioSource.Play();
            
            if (animator != null) animator.CrossFade(talkStateName, 0.2f);

            yield return new WaitForSeconds(audioSource.clip.length);

            if (animator != null) animator.CrossFade(idleStateName, 0.2f);
        }
    }

    public void ClosePanel()
    {
        if (canvasRoot != null) canvasRoot.SetActive(false);
        if (audioSource != null) audioSource.Stop();
        if (animator != null) animator.CrossFade(idleStateName, 0.2f);
        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
    }
}
