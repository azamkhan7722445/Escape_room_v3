using Fusion;
using Fusion.Samples.IndustriesComponents;
using Fusion.XR.Shared.Desktop;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[DefaultExecutionOrder(DesktopAim.EXECUTION_ORDER)]
public class DesktopAim : NetworkBehaviour
{
    public const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 10;

    NetworkGrabbable grabbable;
    RigInfo rigInfo;
    NetworkGrabber grabber;
    MouseTeleport mouseTeleport;
    MouseCamera mouseCamera;
    NetworkTransform networkTransform;
    PaintBlaster blaster;
    MenuManager menuManager;

    private void Awake()
    {
        grabbable = GetComponent<NetworkGrabbable>();
        menuManager = FindObjectOfType<MenuManager>(true);
        blaster = GetComponent<PaintBlaster>();
        networkTransform = GetComponent<NetworkTransform>();
        grabbable.onDidGrab.AddListener(OnDidGrab);
        grabbable.onDidUngrab.AddListener(OnDidUngrab);
    }

    private void OnDidUngrab()
    {
        if (rigInfo == null) rigInfo = RigInfo.FindRigInfo(runner: Runner);
        if (rigInfo.localHardwareRigKind != RigInfo.RigKind.Desktop) return;
        blaster.useInput = true;
        // We reenable the normal desktop controls
        if (grabber.HasInputAuthority) AdaptLocalControls(false);
        grabber = null;
    }

    private void OnDidGrab(NetworkGrabber g)
    {
        if (rigInfo == null) rigInfo = RigInfo.FindRigInfo(runner: Runner);
        if(rigInfo.localHardwareRigKind != RigInfo.RigKind.Desktop) return;
        blaster.useInput = false;
        // If the object was not ungrab by the local user before a new grab, we reenable the normal desktop controls
        if (grabber && grabber.HasInputAuthority) AdaptLocalControls(false);

        grabber = g;
        if (grabber.HasInputAuthority)
        {
            AdaptLocalControls(true);
        }
    }

    void AdaptLocalControls(bool isGrabbing)
    {
        if (mouseTeleport == null) mouseTeleport = rigInfo.localHardwareRig.GetComponentInChildren<MouseTeleport>();
        if (mouseCamera == null) mouseCamera = rigInfo.localHardwareRig.GetComponentInChildren<MouseCamera>();
        if (mouseTeleport)
        {
            mouseTeleport.enabled = !isGrabbing;
        }
        if (mouseCamera)
        {
            mouseCamera.forceRotation = isGrabbing;
        }
        if (menuManager)
        {
            if (isGrabbing)
                menuManager.DisableMenu();
            else
                menuManager.EnableMenu();
        }
        if (isGrabbing)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void LateUpdate()
    {
        if (!grabber) return;
        if (grabber.HasStateAuthority == false) return;
        
        blaster.TriggerPressed(Mouse.current.leftButton.isPressed);

        if (Keyboard.current.escapeKey.isPressed)
        {
            AdaptLocalControls(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        AdaptHandsPosition(useInterpolationTarget: false);
    }

    public override void Render()
    {
        base.Render();
        AdaptHandsPosition(useInterpolationTarget: true);
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    void AdaptHandsPosition(bool useInterpolationTarget)
    {
        if (!grabber) return;
        if (grabber.HasStateAuthority == false) return;
        if (rigInfo == null) rigInfo = RigInfo.FindRigInfo(runner: Runner);
        if (rigInfo.localHardwareRigKind != RigInfo.RigKind.Desktop) return;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        

        var head = rigInfo.localHardwareRig.headset.transform;
        var leftHand = rigInfo.localHardwareRig.leftHand.transform;
        var rightHand = rigInfo.localHardwareRig.rightHand.transform;
        var aimPosition = transform;
        if (useInterpolationTarget)
        {
            //head = rigInfo.localNetworkedRig.headset.networkTransform.transform;
            leftHand = rigInfo.localNetworkedRig.leftHand.transform;
            rightHand = rigInfo.localNetworkedRig.rightHand.transform;
            aimPosition = transform;
        }

        Transform pointingTransform;
        // We add a lateral offset to avoid having the gun in front of the aiming ray
        Vector3 pointingOffset;
        if (rigInfo.localNetworkedRig.leftHand == grabber.hand)
        {
            pointingTransform = leftHand;
            rightHand.rotation = head.rotation * mouseTeleport.defaultRightHandRotation;
            pointingOffset = new Vector3(-0.15f,0,0);
        } else
        {
            pointingTransform = rightHand;
            leftHand.rotation = head.rotation * mouseTeleport.defaultLeftHandRotation;
            pointingOffset = new Vector3(0.15f, 0, 0);
        }

        var lookPoint = ray.origin + ray.direction * 100f;
        if (false && Physics.Raycast(ray, out var hit))
        {
            lookPoint = hit.point;
        }

        var leftHandPosition = head.TransformPoint(mouseTeleport.defaultLeftHandPosition + pointingOffset + Vector3.forward * MouseTeleport.HAND_RANGE);
        var rightHandPosition = head.TransformPoint(mouseTeleport.defaultRightHandPosition + pointingOffset);
        leftHand.position = leftHandPosition;
        rightHand.position = rightHandPosition;
        
        // Find aiming object rotation
        var targetAimObjectRotation = Quaternion.LookRotation(aimPosition.position - lookPoint);
        // Find and apply grabbing hand rotation to obtain aim rotation
        var targetRotation = targetAimObjectRotation * Quaternion.Inverse(grabbable.LocalRotationOffset);
        pointingTransform.rotation = targetRotation;
        // Apply normal grabbable offset
        aimPosition.position = pointingTransform.TransformPoint(grabbable.LocalPositionOffset);
        aimPosition.rotation = pointingTransform.rotation * grabbable.LocalRotationOffset;
    }
}
