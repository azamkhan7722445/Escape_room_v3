using Fusion;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


/**
 * 
 * PaintBlaster is in charge of spawning a projectile when the player fires.
 * To do so, it maintain two networked lists :
 *      - list a shots
 *      - list of impacts
 * Because the shots list is networked, all players receive updated data regardless of which player fired. 
 * So, players just have to check in `Render()` if new data has been received in order to spawn a local graphical object when a new shot occurs (`CheckShoots()`).
 * The player having the gun state authority checks the impact list in order to request the object texture modification when a new impact has been added to the list. 
 * 
 **/

[DefaultExecutionOrder(PaintBlaster.EXECUTION_ORDER)]
public class PaintBlaster : NetworkBehaviour, IStateAuthorityChanged
{
    public const int EXECUTION_ORDER = NetworkHand.EXECUTION_ORDER + 10;

    [System.Serializable]
    public struct BulletShoot : INetworkStruct
    {
        public Vector3 initialPosition;
        public Quaternion initialRotation;
        public float shootTime;
        public int bulletPrefabIndex;
        public PlayerRef source;
    }

    [System.Serializable]
    public struct ImpactInfo : INetworkStruct
    {
        public Vector2 uv;
        public Color color;
        public float sizeModifier;
        public NetworkBehaviourId networkProjectionPainterId;
        public float impactTime;
        public PlayerRef source;
    }

    public int bulletcount = 0;
    const int MAX_BULLET_SHOOT = 50;
    [Networked]
    [Capacity(MAX_BULLET_SHOOT)]
    // Allows to warn other player that a ball is shoot to spawn a graphical object
    public NetworkLinkedList<BulletShoot> BulletShoots { get; }

    const int MAX_IMPACTS = 50;
    [Networked]
    [Capacity(MAX_IMPACTS)]
    // Allows to warn other player that a new impact occurs
    public NetworkLinkedList<ImpactInfo> RecentImpacts { get; }



    [SerializeField] private Transform projectileOriginPoint;
    [SerializeField] private GameObject defaultProjectilePrefab;
    [SerializeField] private List<GameObject> projectilePrefabs = new List<GameObject>();
    [SerializeField] private bool useRandomColor;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float shootCoolDown = 1f;
    float lastShootTime = 0f;
    public NetworkGrabbable grabbable;
    public InputActionProperty leftUseAction;
    public InputActionProperty rightUseAction;

    BulletShoot lastBullet;

    public float lastSpawnedBulletTime = -1;
    public float lastRecentImpactTime = -1;

    float minAction = 0.05f;

    InputActionProperty UseAction => grabbable != null && grabbable.IsGrabbed && grabbable.CurrentGrabber.hand.side == RigPart.LeftController ? leftUseAction : rightUseAction;
    public bool IsGrabbed => grabbable.IsGrabbed;
    public bool IsGrabbedByLocalPLayer => IsGrabbed && grabbable.CurrentGrabber.Object.StateAuthority == Runner.LocalPlayer;

    public bool IsUsed
    {
        get
        {
            return UseAction.action.ReadValue<float>() > minAction;
        }
    }

    [Header("Haptic feedback")]
    public float defaultHapticAmplitude = 0.2f;
    public float defaultHapticDuration = 0.05f;

    private void Awake()
    {
        grabbable = GetComponent<NetworkGrabbable>();
        audioSource = GetComponent<AudioSource>();
        leftUseAction.action.Enable();
        rightUseAction.action.Enable();
    }

    public bool autofire = false;
    bool autofireActivated = false;
    public bool wasUsed = false;
    public bool useInput = true;


    public override void Render()
    {
        base.Render();
        CheckShoots();
        CheckRecentImpacts();
    }

    void IStateAuthorityChanged.StateAuthorityChanged()
    {
        // We clean the cache of time based entries, as the new master probably does not have the same time reference
        BulletShoots.Clear();
        RecentImpacts.Clear();
        lastRecentImpactTime = -1;
        lastSpawnedBulletTime = -1;
    }

    // We check is the player fire
    public void LateUpdate()
    {
        try
        {
            if (Object == null || Object.HasStateAuthority == false) return;

            if (useInput) // VR
            {
                if (IsGrabbedByLocalPLayer)
                {
                    TriggerPressed(IsUsed);
                }
                else
                    wasUsed = false;
            }

            if (autofire && autofireActivated)
                Shoot();

            PurgeBulletShoots();
            PurgeRecentImpacts();
        }
        catch (Exception e) {
            Debug.LogError($"Error [{name}]:"+e.Message);
            Debug.LogError(e);
        }
    }

    public void TriggerPressed(bool isPressed)
    {

        if (autofire)
        {
            if (isPressed && !wasUsed)
                autofireActivated = !autofireActivated;
        }
        else
        {
            if (isPressed)
                Shoot();
        }
        wasUsed = isPressed;

    }

    #region Shoot management

    void CheckShoots()
    {
        foreach (var bullet in BulletShoots)
        {
            if (bullet.shootTime > lastSpawnedBulletTime && bullet.source == Object.StateAuthority)
            {
                var bulletPrefab = (bullet.bulletPrefabIndex == -1) ? defaultProjectilePrefab : projectilePrefabs[bullet.bulletPrefabIndex];
                var projectileGO = GameObject.Instantiate(bulletPrefab, bullet.initialPosition, bullet.initialRotation);
                var projectile = projectileGO.GetComponent<PaintBlasterProjectile>();
                projectile.sourceBlaster = this;
                lastSpawnedBulletTime = bullet.shootTime;
            } 
        }
    }

    void PurgeBulletShoots()
    {
        foreach (var bullet in BulletShoots)
        {
            const int shootDuration = 5;
            if ((Time.time - bullet.shootTime) > shootDuration || bullet.source != Object.StateAuthority)
            {
                BulletShoots.Remove(bullet);
                break;
            }
        }
    }

    public void Shoot()
    {
        OnShooting(projectileOriginPoint.transform.position, projectileOriginPoint.transform.rotation);
    }

    // spawn the projectile network prefab on shooting
    private void OnShooting(Vector3 position, Quaternion rotation)
    {
        if (BulletShoots.Count > MAX_BULLET_SHOOT)
        {
            Debug.LogError("Unable to shoot");
            return;
        }
        if (lastShootTime + shootCoolDown < Time.time)
        {
            SendHapticFeedback();
            audioSource.Play();
            lastShootTime = Time.time;
            var bulletPrefabIndex = -1;
            if (useRandomColor)
            {
                bulletPrefabIndex = UnityEngine.Random.Range(0, projectilePrefabs.Count);
            }

            lastBullet.initialPosition = position;
            lastBullet.initialRotation = rotation;
            lastBullet.shootTime = Time.time;
            lastBullet.bulletPrefabIndex = bulletPrefabIndex;
            lastBullet.source = Object.StateAuthority;
            BulletShoots.Add(lastBullet);
        }
    }
    #endregion

    #region Impact management
    public void OnBulletImpact(ImpactInfo impact)
    {
        if (RecentImpacts.Count > MAX_IMPACTS)
        {
            Debug.LogError($"{name} Unable to display impact");
            return;
        }
        RecentImpacts.Add(impact);
    }

    void PurgeRecentImpacts()
    {

        foreach (var impact in RecentImpacts)
        {
            const int impactDuration = 5;
            if ((Time.time - impact.impactTime) > impactDuration || impact.source != Object.StateAuthority)
            {
                RecentImpacts.Remove(impact);
                break;
            }
        }
    }

    void CheckRecentImpacts()
    {
        foreach (var impact in RecentImpacts)
        {
            if (impact.impactTime > lastRecentImpactTime && impact.source == Object.StateAuthority)
            {
                lastRecentImpactTime = impact.impactTime;
                if (Runner.TryFindBehaviour<NetworkProjectionPainter>(impact.networkProjectionPainterId, out var networkProjectionPainter))
                {
                    // Only the stateAuthority of the projection painter has to apply the impact to do the painting on the object
                    // The stateAuthority of the gun also needs to prepaint so that there is no delay of the impact
                    if (networkProjectionPainter.HasStateAuthority || Object.HasStateAuthority)
                    {
                        networkProjectionPainter.PaintAtUV(impact.uv, impact.sizeModifier, impact.color);
                    }
                }
            }
        }
    }
    #endregion

    private void SendHapticFeedback()
    {
        if (grabbable == null) return;
        if (!IsGrabbedByLocalPLayer || grabbable.CurrentGrabber.hand.LocalHardwareHand == null) return;

        grabbable.CurrentGrabber.hand.LocalHardwareHand.SendHapticImpulse(amplitude: defaultHapticAmplitude, duration: defaultHapticDuration);
    }
}
