using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatelliteDishRotator : MonoBehaviour
{
    [SerializeField] private float speedRotation = 0.4f;

    private void Update()
    {
        transform.Rotate(transform.up * speedRotation * Time.deltaTime);
    }
}
