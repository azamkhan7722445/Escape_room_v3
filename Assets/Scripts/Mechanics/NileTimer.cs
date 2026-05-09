using UnityEngine;

public class NileTimer : MonoBehaviour
{
    public float totalTime = 2700f; // 45 minutes
    public float currentTime;
    public Transform waterLevel;
    public float maxWaterHeight = 10f;
    public float minWaterHeight = 0f;

    void Start()
    {
        currentTime = totalTime;
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateWaterLevel();
        }
    }

    void UpdateWaterLevel()
    {
        if (waterLevel == null) return;
        
        float progress = 1f - (currentTime / totalTime);
        float height = Mathf.Lerp(minWaterHeight, maxWaterHeight, progress);
        
        Vector3 pos = waterLevel.localPosition;
        pos.y = height;
        waterLevel.localPosition = pos;
    }
}
