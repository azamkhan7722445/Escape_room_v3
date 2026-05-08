using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintBlasterProjectileExplosion : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float despawnDelay = 2f;
    private float despawnTimer;

    void Start()
    {
        if(!audioSource)
            audioSource = GetComponent<AudioSource>();
    }


    private void Awake()
    {
        despawnTimer = Time.time + despawnDelay;
        Explode();
    }

    private void Explode()
    {

       // Debug.LogError($"Explode !");
    }

    public void FixedUpdate()
    {
        if (Time.time < despawnTimer)
            return;

        Destroy(gameObject);
    }
}
