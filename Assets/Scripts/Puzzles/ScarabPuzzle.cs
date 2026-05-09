using UnityEngine;
using System.Collections.Generic;

public class ScarabPuzzle : MonoBehaviour
{
    [Header("Settings")]
    public int totalScarabs = 20;
    public int targetCount = 6;
    public float eastAngle = 90f; // Y rotation for East

    [Header("References")]
    public GameObject scarabPrefab;
    public Transform gridRoot;
    public Material scarabMaterial;
    public Vector2 spacing = new Vector2(0.3f, 0.25f);
    public int columns = 5;

    [SerializeField]
    private List<GameObject> scarabs = new List<GameObject>();

    public int GetCorrectCount() => targetCount;

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        // Clear existing
        if (gridRoot == null)
        {
            GameObject go = new GameObject("GridRoot");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            gridRoot = go.transform;
        }

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridRoot.GetChild(i).gameObject);
        }
        scarabs.Clear();

        if (scarabPrefab == null)
        {
            Debug.LogError("Scarab Prefab is missing!");
            return;
        }

        // Randomly pick indices for East facing
        HashSet<int> eastIndices = new HashSet<int>();
        // To make it deterministic for now as per design "6 facing east"
        // In a real game we might want this fixed or random but consistent.
        // For the sake of the exercise, let's use a fixed seed or just pick 6.
        Random.InitState(42); 
        while (eastIndices.Count < targetCount)
        {
            eastIndices.Add(Random.Range(0, totalScarabs));
        }

        for (int i = 0; i < totalScarabs; i++)
        {
            int row = i / columns;
            int col = i % columns;

            // Positioning in a grid on the wall (X-Y plane locally)
            Vector3 pos = new Vector3(col * spacing.x, -row * spacing.y, 0);
            GameObject scarab = Instantiate(scarabPrefab, gridRoot);
            scarab.transform.localPosition = pos;
            scarab.name = "Scarab_" + i;

            // Set rotation
            float yRotation = 0;
            if (eastIndices.Contains(i))
            {
                yRotation = eastAngle;
            }
            else
            {
                // Randomly set to North (0), West (270), or South (180)
                float[] otherAngles = { 0, 180, 270 };
                yRotation = otherAngles[Random.Range(0, otherAngles.Length)];
            }

            // Adjusting for FBX default orientation (assuming it needs -90 on X to stand on wall)
            scarab.transform.localRotation = Quaternion.Euler(-90, yRotation, 0);
            
            if (scarabMaterial != null)
            {
                foreach (var renderer in scarab.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = scarabMaterial;
                }
            }
            
            scarabs.Add(scarab);
        }
    }

    private void OnValidate()
    {
        if (gridRoot != null && gridRoot.childCount == 0 && scarabPrefab != null)
        {
            // Note: Instantiate/Destroy doesn't work well in OnValidate for permanent scene objects
            // Use Context Menu instead
        }
    }
}

