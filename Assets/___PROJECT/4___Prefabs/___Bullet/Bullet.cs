using UnityEngine;

public class Bullet : PoolObject
{
    public PoolObject bulletHitPrefab;
    private Rigidbody2D rb;
    private Vector2 startPoint;
    private LayerMask layerMask;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // 부딫히지 않고 멀리 날아갈 경우 스스로 인스턴스 반환
        var movedDist = (rb.position - startPoint).magnitude;
        if(movedDist >= 100f)
        {
            ReturnInstance();
        }
    }

    // 충돌하면 인스턴스 반환
    void OnCollisionEnter2D(Collision2D collision)
    {

        // 새로운 충돌 파티클 인스턴스를 메모리 풀로부터 생성
        var newHit = MemoryPool.Inst.GetInstance<BulletHit>(bulletHitPrefab);
        newHit.Init(collision.contacts[0].point);
        ReturnInstance();
    }

    private Vector2 GetDirectionFromAngle(float angleDegrees)
    {
        // 1. 도(Degrees)를 라디안(Radians)으로 변환
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        // 2. Cos, Sin을 이용해 x, y 성분 계산
        float x = Mathf.Cos(angleRadians);
        float y = Mathf.Sin(angleRadians);

        // 3. 방향 벡터 반환 (크기가 1인 단위 벡터)
        return new Vector2(x, y);
    }

    public void Init(Vector2 firePoint, float rotation, float speed)
    {
        var direction = GetDirectionFromAngle(rotation);
        transform.position = firePoint;
        transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        rb.position = firePoint;
        rb.rotation = rotation;
        rb.linearVelocity = direction * speed;
        rb.angularVelocity = 0f;
        startPoint = firePoint;
    }
}
