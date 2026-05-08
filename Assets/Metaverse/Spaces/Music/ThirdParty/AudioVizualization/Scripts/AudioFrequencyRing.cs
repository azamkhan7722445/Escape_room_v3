using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFrequencyRing : MonoBehaviour
{

    public Runningtap.AnalyzeAudio audioData;

    public GameObject sampleCubePrefab;
    public Vector3 StartScale = new Vector3(1,1,1);
    private Vector3 centerPoint;
    public float Radius = 10f;
    public float Sensitivity = 2f;

    private GameObject[] sampleCube;

	void Start ()
    {
        sampleCube = new GameObject[audioData.FrequencyBands];
        float angleBetweenObjects = 360f / audioData.FrequencyBands;
        centerPoint = transform.position;

        for (int i = 0; i < audioData.FrequencyBands; i++)
        {
            float angle = i * angleBetweenObjects;
            Vector3 position = new Vector3(
               centerPoint.x + Radius * Mathf.Sin(angle * Mathf.Deg2Rad),
               centerPoint.y,
               centerPoint.z + Radius * Mathf.Cos(angle * Mathf.Deg2Rad)
           );

            GameObject instance = Instantiate(sampleCubePrefab, position, Quaternion.identity);


       //     GameObject instance = Instantiate(sampleCubePrefab);
        //    instance.transform.position = transform.position;
            instance.transform.parent = transform;
            instance.name = "SampleCube_" + i;
       //     transform.eulerAngles = new Vector3(0, -angle * i, 0);
       //     Debug.LogError($"Avant : Radius={Radius} instance.transform.position={instance.transform.position} ");
       //     instance.transform.position = Vector3.forward * Radius;
       //     Debug.LogError($"Arpès : instance.transform.position={instance.transform.position}");
            //instance.transform.eulerAngles = new Vector3(90, 0, 0);
            sampleCube[i] = instance;
        }
	}
	
	void Update ()
    {
		for(int i = 0; i < audioData.FrequencyBands; i++)
        {
            sampleCube[i].transform.localScale = new Vector3(StartScale.x, audioData.AudioBandBuffer[i] * Sensitivity + StartScale.y, StartScale.z);
        }
    
    }
}