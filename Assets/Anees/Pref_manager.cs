using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

public class Pref_manager : MonoBehaviour
{
    public static Pref_manager Instance;

    [DllImport("__Internal")]
    private static extern void TriggerVR();

    public bool vr, pc;
    private void Awake()
    {
        // If instance doesn't exist, set it
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If another instance exists, destroy this one
            Destroy(gameObject);
        }
    }

    // Example function
    public void pc_enable()
    {
               pc = true;
        vr = false;

        // PlayerPrefs.SetInt("mode", 0); // 0 for PC, 1 for VR
        SceneManager.LoadScene("AvatarSelection");
    }
    public void VR_enable()
    {
        pc = false;
        vr = true;
       // PlayerPrefs.SetInt("mode", 1); // 0 for PC, 1 for VR

        #if !UNITY_EDITOR && UNITY_WEBGL
        TriggerVR();
        #endif

        SceneManager.LoadScene("AvatarSelection");
    }
}
