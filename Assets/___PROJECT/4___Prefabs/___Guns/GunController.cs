using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GunSpec))]
public class GunController : MonoBehaviour
{
    public PoolObject flashPrefab;
    public PoolObject bulletPrefab;
    public float multiShotSpread; // 여러발 발사 시 흩어지는 각도
    public int multiShellCount; // 한 번에 여러발을 발사할 때 발사되는 총알 개수
    
    [HideInInspector]
    private GunType gunType; // 현재 사용 중인 총 타입

    [HideInInspector]
    public bool isMultiShot = false; // 샷건 등의 한 번에 여러발을 발사해야할 경우 활성화

    [HideInInspector]
    public Vector2 recoilOffset; // 렌더링으로 표현되는 반동 offset 값

    private int totalAmmo;
    private int currAmmo;
    private float ammoSpeed;
    private int damage;
    private float fireInterval;
    private float currFireTime;
    private float recoil;
    private float reloadTime;
    private float currReloadTime;
    private float recoilRecoverySpeed;

    private bool triggerPulled;

    private float rotation;
    private Vector2 firePoint;
    
    void Update()
    {
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);

        // fireInterval 간격으로 발사
        currFireTime -= Time.deltaTime;
        currFireTime = Mathf.Clamp(currFireTime, 0f, 10f);
        if(triggerPulled && currFireTime <= 0f && currAmmo > 0)
        {
            // 새로운 총구 화염 인스턴스를 메모리 풀로부터 생성
            var muzzleFire = MemoryPool.Inst.GetInstance<MuzzleFire>(flashPrefab);
            muzzleFire.Init(firePoint, rotation);

            // 새로운 총알 인스턴스를 메모리 풀로부터 생성
            if(!isMultiShot)
            {
                CreateAmmo(rotation);
            }
            else // isMultiShot이 true일 경우 한 번에 multiShellCount개의 인스턴스를 생성
            {
                for(int i = 0; i < multiShellCount; i ++)
                {
                    CreateAmmo(Random.Range(rotation - multiShotSpread, rotation + multiShotSpread));
                }
            }

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
    public void InputSpec(GunSpecValue spec, GunType type)
    {
        totalAmmo = spec.maxAmmo;
        currAmmo = spec.maxAmmo;
        ammoSpeed = spec.ammoSpeed;
        fireInterval = spec.fireInterval;
        reloadTime = spec.reloadTime;
        damage = spec.damage;
        recoilRecoverySpeed = spec.recoilRecoverySpeed;
        gunType = type;
        isMultiShot = type == GunType.Shotgun;
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

    private void CreateAmmo(float rotation)
    {
        var newBullet = MemoryPool.Inst.GetInstance<Bullet>(bulletPrefab);
        newBullet.Init(firePoint, rotation, ammoSpeed, damage);
    }
}
