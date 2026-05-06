using UnityEngine;

public class BulletHit : PoolObject
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

    public void Init(Vector2 point)
    {
        transform.position = point;
        particle.Simulate(0f, true, true);
        particle.Play();
    }
}
