using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GunSpec))]
public class GunController : MonoBehaviour
{
    public PoolObject flashPrefab;
    public PoolObject bulletPrefab;

    [HideInInspector]
    public Vector2 recoilOffset;

    private int totalAmmo;
    private int currAmmo;
    private int damage;
    private float fireInterval;
    private float currFireTime;
    private float recoil;
    private float reloadTime;
    private float currReloadTime;

    private bool triggerPulled;

    private float rotation;
    private Vector2 firePoint;
    
    void Update()
    {
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * 20f);

        // fireInterval 간격으로 발사
        currFireTime -= Time.deltaTime;
        currFireTime = Mathf.Clamp(currFireTime, 0f, 10f);
        if(triggerPulled && currFireTime <= 0f && currAmmo > 0)
        {
            // 새로운 총구 화염 인스턴스를 메모리 풀로부터 생성
            var muzzleFire = MemoryPool.Inst.GetInstance<MuzzleFire>(flashPrefab);
            muzzleFire.Init(firePoint, rotation);

            // 새로운 총알 인스턴스를 메모리 풀로부터 생성
            var newBullet = MemoryPool.Inst.GetInstance<Bullet>(bulletPrefab);
            newBullet.Init(firePoint, rotation, 30f);

            // 반동 위치 오프셋 지정
            recoilOffset.x = 0.5f;

            currFireTime += fireInterval;
        }
    }

    // 방아쇠 당기기 놓기
    public void PullTrigger(bool flag)
    {
        triggerPulled = flag;
    }

    // 총기 값 설정
    public void InputSpec(GunSpecValue spec)
    {
        totalAmmo = spec.maxAmmo;
        currAmmo = spec.maxAmmo;
        fireInterval = spec.fireInterval;
        reloadTime = spec.reloadTime;
        damage = spec.damage;
    }

    // 회전 입력
    public void InputRotation(float val)
    {
        rotation = val;
    }

    // 총구 위치 입력
    public void InputFirePoint(Vector2 val)
    {
        firePoint = val;
    }
}
