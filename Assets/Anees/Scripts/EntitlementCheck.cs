using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

public class EntitlementCheck : MonoBehaviour
{
    void Start()
    {
        try
        {
            Core.AsyncInitialize().OnComplete(OnPlatformInitialized);
        }
        catch (UnityException e)
        {
            Debug.LogError($"Exception occurred during Oculus Platform initialization: {e.Message}");
        }
    }

    void OnPlatformInitialized(Message<PlatformInitialize> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError($"Oculus Platform initialization failed: {msg.GetError().Message}");
            return;
        }

        try
        {
            Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception occurred during Entitlement Check: {e.Message}");
        }
    }

    void EntitlementCallback(Message msg)
    {
        if (msg.IsError)
        {
            Debug.Log("User is NOT entitled.");
            // Application.Quit();
        }
        else
        {
            Debug.Log("User is entitled.");
        }
    }
}