using UnityEngine;

public class Shell : PoolObject
{
    private Rigidbody2D rb;
    private DeltaTimer deleteTimer = new();

    [HideInInspector]
    public bool isShotgunShell;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 탄피가 튕기다가 멈춘 이후 1초가 지나면 타이머를 리셋함과 동시에 인스턴스를 풀로 반환
    void FixedUpdate()
    {
        var linVel = rb.linearVelocity;
        var angVel = rb.angularVelocity;
        if(linVel.magnitude <= 0.1f && Mathf.Abs(angVel) <= 0.1f)
        {
            deleteTimer.Update();
            if(deleteTimer.CheckTime(1f, CheckOption.Reset)) {
                ReturnInstance();
            }
        }
    }

    public void Init(Vector2 point, bool lookLeft)
    {
        transform.position = point;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
        rb.position = transform.position;
        rb.rotation = transform.rotation.eulerAngles.z;

        // 좌측 위로 또는 우측 위로 impulse 가함
        Vector2 impulse = lookLeft ? new Vector2(Random.Range(8f, 10f), Random.Range(8f, 10f)) : new Vector2(Random.Range(-10f, -5f), Random.Range(5f, 10f));
        rb.AddForce(impulse, ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(0.1f, 0.5f), ForceMode2D.Impulse);
    }
}
