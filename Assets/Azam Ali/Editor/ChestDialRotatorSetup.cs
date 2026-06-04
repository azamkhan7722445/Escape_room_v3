using UnityEngine;
using UnityEditor;
using Fusion.XR.Shared.Grabbing;

/// <summary>
/// One-click setup: Tools > Setup Chest Dial Rotators
/// Swaps Grabbable → ChestDialRotator on all 6 rotators and wires up ChestManager.
/// Safe to run multiple times.
/// </summary>
public static class ChestDialRotatorSetup
{
    [MenuItem("Tools/Setup Chest Dial Rotators")]
    public static void Setup()
    {
        string prefabPath = "Assets/Azam Ali/___Chest____.prefab";
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[ChestSetup] Could not load prefab at: " + prefabPath);
            return;
        }

        var chestManager = prefabRoot.GetComponentInChildren<ChestManager>(true);
        if (chestManager == null)
        {
            Debug.LogError("[ChestSetup] ChestManager not found in prefab.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        var rotatorContainer = prefabRoot.transform.Find("RotatorContainer");
        if (rotatorContainer == null)
        {
            Debug.LogError("[ChestSetup] RotatorContainer not found in prefab.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        for (int i = 0; i < rotatorContainer.childCount; i++)
        {
            var go = rotatorContainer.GetChild(i).gameObject;

            // Remove plain Grabbable if it's not already our subclass
            var existing = go.GetComponent<Grabbable>();
            if (existing != null && existing.GetType() == typeof(Grabbable))
            {
                Object.DestroyImmediate(existing, true);
                Debug.Log($"[ChestSetup] Removed Grabbable from {go.name}");
            }

            // Add ChestDialRotator if missing
            var dial = go.GetComponent<ChestDialRotator>();
            if (dial == null)
            {
                dial = go.AddComponent<ChestDialRotator>();
                Debug.Log($"[ChestSetup] Added ChestDialRotator to {go.name}");
            }

            dial.chestManager = chestManager;
            dial.rotatorIndex  = i;
            dial.expectedIsKinematic = true;
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.Refresh();
        Debug.Log("[ChestSetup] Done! All rotators updated and prefab saved.");
    }
}
