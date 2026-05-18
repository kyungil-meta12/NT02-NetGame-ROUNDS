using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(GunSpec))]
public class GunController : MonoBehaviour
{
    public PoolObject flashPrefab;
    public PoolObject bulletPrefab;
    public PoolObject shellPrefab;

    public float multiShotSpread; // 여러발 발사 시 흩어지는 각도
    public int multiShellCount; // 한 번에 여러발을 발사할 때 발사되는 총알 개수

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
    private float currFireTime;

    private float reloadDuration;
    private float currReloadTime = 0f;
    private bool reloadState = false;

    private float recoilRecoverySpeed;

    private bool triggerPulled;
    private bool lookingLeft;

    public bool isDespwaning;

    private NetworkObject netObject;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        netObject = transform.root.GetComponent<NetworkObject>();
    }

    void Update()
    {
        if (NetworkPacketManager.Inst.sceneSwitching)
        {
            return;
        }

        // [변경] 소유자(Owner)만 발사 로직을 계산하고 서버에 요청함
        if (netObject.IsOwner)
        {
           UpdateFire();
           UpdateReload();
        }
       UpdateRecoilOffset();
    }

    // 총 발사 업데이트
    void UpdateFire()
    {
        // fireInterval 간격으로 발사
        currFireTime -= Time.deltaTime;
        currFireTime = Mathf.Clamp(currFireTime, 0f, fireInterval);

        if (triggerPulled && !reloadState && currFireTime <= 0f && currAmmo > 0)
        {
            var firePointRot = lookingLeft ? -firePoint.rotation.eulerAngles.z + 180f : firePoint.rotation.eulerAngles.z;

            // [변경] 직접 생성하지 않고 서버에 발사 요청
            NetworkPacketManager.Inst.RequestCreateFireEffectServerRpc(netObject, firePoint.position, shellPoint.position, firePointRot, lookingLeft);

            if (isMultiShot)
            {
                var shellCount = multiShellCount + PlayerManager.Inst.Stat.multiShellCountLevel;
                for (int i = 0; i < shellCount; i++)
                {
                    var spread = multiShotSpread + (multiShotSpread * 0.5f * PlayerManager.Inst.Stat.multiShotSpreadLevel);
                    var randomRot = Random.Range(firePointRot - spread, firePointRot + spread);
                    NetworkPacketManager.Inst.RequestCreateBulletServerRpc(netObject, firePoint.position, randomRot, ammoSpeed, damage);
                }
            }
            else
            {
                NetworkPacketManager.Inst.RequestCreateBulletServerRpc(netObject, firePoint.position, firePointRot, ammoSpeed, damage);
            }

            // [변경] 로컬 클라이언트에서 즉시 처리할 내용 (장탄수 UI 등)
            currAmmo--;
            if(AmmoIndicator.Inst)
            {
                AmmoIndicator.Inst.InputAmmo(currAmmo);
            }
            currFireTime = fireInterval;

            // 로컬 반동 연출 (반응성을 위해 즉시 실행)
            transform.localPosition = new Vector2(-0.6f, 0f);
        }
    }

    // [추가] 서버로부터 발사 승인을 받았을 때 모든 클라이언트에서 실행될 로직 (총알/이펙트 생성)
    public void ExecuteCreateFireEffects(Vector2 firePos, Vector2 shellPos, float rotation, bool isLeft)
    {
        if (!MemoryPool.Inst)
        {
            return;
        }

        // 총구 화염 및 탄피 생성 (시간 효과)
        var muzzleFire = MemoryPool.Inst.GetInstance<MuzzleFire>(flashPrefab);
        muzzleFire.Init(firePos, rotation);
        var newShell = MemoryPool.Inst.GetInstance<Shell>(shellPrefab);
        newShell.Init(shellPos, isLeft);

        // 비소유자 화면에서도 반동이 보이도록 설정
        transform.localPosition = new Vector2(-0.6f, 0f);
    }

    public void ExecuteCreateBullets(Vector2 pos, float rotation, float ammoSpeed, int dmg)
    {
        if(!MemoryPool.Inst)
        {
            return;
        }
        var newBullet = MemoryPool.Inst.GetInstance<Bullet>(bulletPrefab);
        newBullet.Init(pos, rotation, ammoSpeed, dmg);
        newBullet.isOwner = netObject.IsOwner;
    }

    void UpdateReload()
    {
        if (reloadState)
        {
            currReloadTime += Time.deltaTime;
            if(currReloadTime >= reloadDuration)
            {
                currReloadTime = 0f;
                currAmmo = totalAmmo;
                if (AmmoIndicator.Inst)
                {
                    AmmoIndicator.Inst.InputAmmo(currAmmo);
                }
                reloadState = false;
            }
            if (AmmoIndicator.Inst)
            {
                AmmoIndicator.Inst.InputReloadTime(currReloadTime, reloadDuration);
            }
        }
    }

    public void PullTrigger(bool flag)
    {
        triggerPulled = flag;
    }
        

    public void ReloadGun() { 
        if (!reloadState && currAmmo < totalAmmo) 
        { 
            reloadState = true; 
            triggerPulled = false; 
        } 
    }

    public void InputDirection(bool isLeft)
    {
        lookingLeft = isLeft;
    }

    public void UpdateRecoilOffset()
    {
        transform.localPosition = Vector2.Lerp(transform.localPosition, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
    }

    public void InputSpec(GunSpecValue spec, GunType type)
    {
        var ammoSize = spec.maxAmmo + (int)(spec.maxAmmo * 0.5f * PlayerManager.Inst.Stat.totalAmmoLevel);
        totalAmmo = ammoSize; 
        currAmmo = ammoSize;
        damage = spec.damage + (int)(spec.damage * 0.25f * PlayerManager.Inst.Stat.damageLevel);
        ammoSpeed = spec.ammoSpeed + (spec.ammoSpeed * 0.5f * PlayerManager.Inst.Stat.ammoSpeedLevel);
        fireInterval = spec.fireInterval - (spec.fireInterval * 0.25f * PlayerManager.Inst.Stat.fireSpeedLevel);
        isMultiShot = type == GunType.Shotgun || PlayerManager.Inst.Stat.isMultiShot;

        reloadDuration = spec.reloadTime;
        recoilRecoverySpeed = spec.recoilRecoverySpeed;

        firePoint = transform.Find("FirePoint"); 
        shellPoint = transform.Find("ShellPoint");

        if(netObject.IsOwner) {
            if (AmmoIndicator.Inst)
            {
                AmmoIndicator.Inst.InitAmmo(currAmmo); 
                AmmoIndicator.Inst.InputGunType(type); 
            }
        }
    }

    // 현재 상태 초기화
    public void ResetGun()
    {
        currAmmo = totalAmmo;
        currFireTime = 0f;
        reloadState = false;
        currReloadTime = 0f;

        if (netObject.IsOwner)
        {
            if (AmmoIndicator.Inst)
            {
                AmmoIndicator.Inst.InitAmmo(totalAmmo);
                AmmoIndicator.Inst.InputReloadTime(currReloadTime, reloadDuration);
            }
        }
    }
}
