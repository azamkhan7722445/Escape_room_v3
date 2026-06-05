using UnityEngine;

using Fusion;
using Fusion.XR.Shared.Grabbing;

public enum CanopicJarType
{
    Imsety,     // Liver
    Hapi,       // Lungs
    Duamutef,   // Stomach
    Qebehsenuef // Intestines
}

public class CanopicJar : NetworkBehaviour
{
    public CanopicJarType jarType;

    public override void Spawned()
    {
        var grabbable = GetComponent<Grabbable>();
        if (grabbable != null)
        {
            grabbable.onGrab.AddListener(OnGrab);
        }
    }

    private void OnGrab()
    {
        if (Object != null && !Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        }
    }
}
