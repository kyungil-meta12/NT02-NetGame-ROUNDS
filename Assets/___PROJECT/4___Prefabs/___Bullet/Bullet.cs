using UnityEngine;

public class Bullet : PoolObject
{
    public PoolObject bulletHitPrefab;
    public PoolObject playerHitPrefab;

    private Rigidbody2D rb;
    private Vector2 startPoint;
    private int damage;
    private int groundLayer;
    private int playerLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.NameToLayer("Ground");
        playerLayer = LayerMask.NameToLayer("Player");
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
    void OnCollisionEnter2D(Collision2D c)
    {
        // 충돌 각도에 따라 파티클 방향이 달라짐
        var n = c.contacts[0].normal;
        var degrees = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
        var otherLayer = c.collider.gameObject.layer;

        // 땅 오브젝트와 충돌한 경우 불꽃 파티클 생성
        if(otherLayer == groundLayer) 
        {
            // 새로운 충돌 파티클 인스턴스를 메모리 풀로부터 생성
            var newHit = MemoryPool.Inst.GetInstance<BulletHit>(bulletHitPrefab);
            newHit.Init(c.contacts[0].point, degrees);
        }

        // 사람 오브젝트와 충돌한 경우 대미지를 가하고 파티클을 생성한 후 삭제
        else if(otherLayer == playerLayer) 
        {
            c.collider.gameObject.GetComponent<Player>().GiveDamage(damage);
            var newHit = MemoryPool.Inst.GetInstance<PlayerHit>(playerHitPrefab);
            newHit.Init(c.contacts[0].point, 0f);
        }
        
        ReturnInstance();
    }

    private Vector2 GetDirectionFromAngle(float rot)
    {
        float rad = rot * Mathf.Deg2Rad;
        float x = Mathf.Cos(rad);
        float y = Mathf.Sin(rad);
        return new Vector2(x, y);
    }

    public void Init(Vector2 firePoint, float rotation, float speed, int dmg)
    {
        var direction = GetDirectionFromAngle(rotation);
        transform.position = firePoint;
        transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        rb.position = firePoint;
        rb.rotation = rotation;
        rb.linearVelocity = direction * speed;
        rb.angularVelocity = 0f;
        startPoint = firePoint;
        damage = dmg;
    }
}
