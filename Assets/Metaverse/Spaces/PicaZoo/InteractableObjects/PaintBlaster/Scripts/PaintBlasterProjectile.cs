using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * 
 * PaintBlasterProjectile moves the projectile and check if it is going to collide with a compatible object
 * If the projectile doesn't collide with an object, it will be despawned after a predefined timer by the StateAuthority.
 * If the projectile collides with an object:
 *  - the impact position is computed and saved in the networked `RecentImpacts` list 
 *  - the projectile is despawned 
 *  - an impact prefab with a particle system is spawned to generate a small visual effect
 *  
 *  FBX Meshes must have Read/Write flag enable !
 *  
 **/

public class PaintBlasterProjectile : MonoBehaviour
{
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifeDuration = 3f;
    [SerializeField] private Rigidbody projectileRigidbody;
    [SerializeField] private GameObject impactObjectPrefab;
    [SerializeField] private float projectileRaycastMaxDistance = 0.3f;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Color projectileColor;
    public PaintBlaster sourceBlaster;
    Vector3 initialPosition;
    Ray ray;
    RaycastHit hit;
    Vector3 currentPosition;

    bool alive = true;

    float endOfLifeTime = -1;
    void Start()
    {
        initialPosition = transform.position;

        if (!projectileRigidbody)
            projectileRigidbody = GetComponent<Rigidbody>();
        projectileRigidbody.linearVelocity = transform.forward * projectileSpeed;
        endOfLifeTime = Time.time + projectileLifeDuration;

        if (!meshRenderer)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (meshRenderer)
            projectileColor = meshRenderer.material.color;
    }

    // despwan the projectile if it doesn't collide with an object
    private void Update()
    {
        if (Time.time > endOfLifeTime)
        {
            Destroy(gameObject);
        }
    }

    // move the projectile and check if it is going to collide with a compatible object
    private void FixedUpdate()
    {
        if (alive == false) return;

        currentPosition = transform.position;

        transform.LookAt(currentPosition + projectileRigidbody.linearVelocity);
        Vector3 direction = currentPosition - initialPosition;


        ray = new Ray(currentPosition, direction);
        if (Physics.Raycast(ray, out hit, projectileRaycastMaxDistance, layerMask))
        {
            if (sourceBlaster != null && sourceBlaster.Object != null && sourceBlaster.Object.HasStateAuthority)
            {
                ProjectionPainter projectionPainter = hit.collider.gameObject.GetComponent<ProjectionPainter>();
                if (projectionPainter)
                {
                    var networkProjectionPainter = projectionPainter.GetComponentInParent<NetworkProjectionPainter>();
                    if (networkProjectionPainter)
                    {
                        var impact = new PaintBlaster.ImpactInfo
                        {
                            uv = hit.textureCoord,
                            color = projectileColor,
                            sizeModifier = 1,
                            networkProjectionPainterId = networkProjectionPainter.Id,
                            impactTime = Time.time,
                            source = sourceBlaster.Object.StateAuthority
                        };
                        sourceBlaster.OnBulletImpact(impact);
                    }
                }
            }
            SpawnImpact(hit.point, transform.rotation);
            Despawn();
        }
    }

    void Despawn()
    {
        alive = false;
        GetComponentInChildren<Renderer>().enabled = false;
        endOfLifeTime = Time.time + 1;
    }


    private void SpawnImpact(Vector3 position, Quaternion rotation)
    {
        Instantiate(impactObjectPrefab, position, rotation);
    }
}
