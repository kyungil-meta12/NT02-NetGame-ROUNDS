using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GunSpec))]
public class GunController : MonoBehaviour
{
    public PoolObject flashPrefab;
    public PoolObject bulletPrefab;
    public PoolObject shellPrefab;

    public float multiShotSpread; // 여러발 발사 시 흩어지는 각도
    public int multiShellCount; // 한 번에 여러발을 발사할 때 발사되는 총알 개수
    
    [HideInInspector]
    private GunType gunType; // 현재 사용 중인 총 타입

    [HideInInspector]
    public bool isMultiShot = false; // 샷건 등의 한 번에 여러발을 발사해야할 경우 활성화

    private SpriteRenderer sr;
    private Transform firePoint;
    private Transform shellPoint;

    private int totalAmmo;
    private int currAmmo;
    private float ammoSpeed;
    private int damage;

    private float fireInterval;
    private DeltaTimer fireTimer = new();

    private float reloadDuration;
    private float currReloadTime = 0f;
    private bool reloadState = false;

    private float recoilRecoverySpeed;

    private bool triggerPulled;
    private bool lookingLeft;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
       UpdateFire();
       UpdateReload();
       UpdateRecoilOffset();
    }

    // 총 발사 업데이트
    void UpdateFire()
    {
        // fireInterval 간격으로 발사
        fireTimer.Update();
        if (fireTimer.CheckTime(fireInterval, CheckOption.Stop) && triggerPulled && !reloadState && currAmmo > 0)
        {
            var firePointRot = lookingLeft ? -firePoint.rotation.eulerAngles.z + 180f : firePoint.rotation.eulerAngles.z;

            // 새로운 총구 화염 인스턴스를 메모리 풀로부터 생성
            var muzzleFire = MemoryPool.Inst.GetInstance<MuzzleFire>(flashPrefab);
            muzzleFire.Init(firePoint.position, firePointRot);

            // 새로운 총알 인스턴스를 메모리 풀로부터 생성
            if(!isMultiShot)
            {
                CreateAmmo(firePointRot);
            }
            else // isMultiShot이 true일 경우 한 번에 multiShellCount개의 인스턴스를 생성
            {
                for(int i = 0; i < multiShellCount; i ++)
                {
                    CreateAmmo(Random.Range(firePointRot - multiShotSpread, firePointRot + multiShotSpread));
                }
            }

            // 탄피 생성
            var newShell = MemoryPool.Inst.GetInstance<Shell>(shellPrefab);
            newShell.Init(shellPoint.position, lookingLeft);

            // 반동 위치 오프셋 지정
            transform.localPosition = new Vector2(-0.6f, 0f);

            // 현재 장탄수를 인디케이터로 전달
            AmmoIndicator.Inst.InputAmmo(--currAmmo);

            // 발사 타이머 갱신 후 다시 시작
            fireTimer.Reset();
            fireTimer.SetRunningState(true);
        }
    }

    // 재장전 업데이트
    void UpdateReload()
    {
        if(reloadState)
        {
            currReloadTime += Time.deltaTime;
            if(currReloadTime >= reloadDuration)
            {
                currReloadTime = 0f;
                currAmmo = totalAmmo;
                AmmoIndicator.Inst.InputAmmo(currAmmo);
                reloadState = false;
            }
            AmmoIndicator.Inst.InputReloadTime(currReloadTime, reloadDuration);
        }
    }

    // 반동 오프셋 업데이트
    void UpdateRecoilOffset()
    {
        transform.localPosition = Vector2.Lerp(transform.localPosition, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
    }

    // 방아쇠 당기기/놓기
    public void PullTrigger(bool flag)
    {
        triggerPulled = flag;
    }

    public void ReloadGun()
    {
        if(!reloadState && currAmmo < totalAmmo)
        {
            reloadState = true;
            triggerPulled = false;
        }
    }

    // 방향 입력
    public void InputDirection(bool isLeft)
    {
        lookingLeft = isLeft;
    }

    // 총기 값 설정
    public void InputSpec(GunSpecValue spec, GunType type)
    {
        gunType = type;

        totalAmmo = spec.maxAmmo;
        currAmmo = spec.maxAmmo;
        ammoSpeed = spec.ammoSpeed;
        damage = spec.damage;
        fireInterval = spec.fireInterval;
        reloadDuration = spec.reloadTime;
        recoilRecoverySpeed = spec.recoilRecoverySpeed;

        isMultiShot = type == GunType.Shotgun;

        firePoint = transform.Find("FirePoint");
        shellPoint = transform.Find("ShellPoint");
        AmmoIndicator.Inst.InitAmmo(currAmmo);
        AmmoIndicator.Inst.InputGunType(type);

        fireTimer.SetTime(fireInterval);
    }

    private void CreateAmmo(float rotation)
    {
        var newBullet = MemoryPool.Inst.GetInstance<Bullet>(bulletPrefab);
        newBullet.Init(firePoint.position, rotation, ammoSpeed, damage);
    }
}
