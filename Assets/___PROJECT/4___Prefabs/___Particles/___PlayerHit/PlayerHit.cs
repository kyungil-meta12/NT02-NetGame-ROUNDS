using UnityEngine;

public class PlayerHit : PoolObject
{
    private ParticleSystem particle;

    void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if(particle.isStopped)
        {
            ReturnInstance();
        }
    }

    public void Init(Vector2 point, float rotation)
    {
        transform.position = point;
        transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        particle.Simulate(0f, true, true);
        particle.Play();
    }
}
