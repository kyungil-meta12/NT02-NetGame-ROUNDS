using UnityEngine;

public class MuzzleFire : PoolObject
{
    private ParticleSystem particle;
    private Vector2 firePoint;
    private float rotation;

    void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        transform.position = firePoint;
        transform.rotation = Quaternion.Euler(0f, 0f, rotation);

        if(particle.isStopped)
        {
            ReturnInstance();
        }
    }

    public void Init(Vector2 firePoint_, float rotation_)
    {
        particle.Simulate(0f, true, true);
        particle.Play();
        firePoint = firePoint_;
        rotation = rotation_;
    }
}
