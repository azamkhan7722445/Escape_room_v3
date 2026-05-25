using UnityEngine;
using Fusion;

public class ZAxisDoor : NetworkBehaviour
{
    [SerializeField] private float openZOffset = 90f;
    [SerializeField] private float speed = 2f;

    [Networked] public NetworkBool IsOpen { get; set; }
    
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public override void Spawned()
    {
        // Capture the initial rotation as the "closed" state
        closedRotation = transform.localRotation;
        
        // Calculate the "open" state by adding the Z offset to the initial euler angles
        Vector3 baseEuler = closedRotation.eulerAngles;
        openRotation = Quaternion.Euler(baseEuler.x, baseEuler.y, baseEuler.z + openZOffset);
    }

    public override void Render()
    {
        Quaternion target = IsOpen ? openRotation : closedRotation;
        
        // Smoothly rotate towards the target
        if (Quaternion.Angle(transform.localRotation, target) > 0.01f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Runner.DeltaTime * speed);
        }
        else
        {
            transform.localRotation = target;
        }
    }

    [ContextMenu("Toggle Door")]
    public void Toggle()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            IsOpen = !IsOpen;
        }
    }
}
