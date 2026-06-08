using UnityEngine;

public class ParticleAutoPlay : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        ps.Clear();
        ps.Play();
    }
}
