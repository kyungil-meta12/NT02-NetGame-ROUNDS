using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
    #region VALUES

    // 플레이어 자신이 조작하는 오브젝트인지?
    // [삭제] public bool isOwner = true; 내장 속성인 IsOwner 를 사용하여 네트워크 권한을 판별.
    public Transform body;
    public Transform gunHand;
    public Transform gunAxis;

    [Space(10)]
    public GameObject deathParticlePrefab;

    [Space(10)]
    public int totalHP;

    [Space(10)]
    public HpBar hpBar;

    [Space(10)]
   // public GunType currentGunType;
    public GameObject[] guns;

    [Space(10)]
    public SpriteRenderer faceRenderer;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer[] handRenderer;

    [Space(10)]
    public float moveForce;
    public float moveSpeed;
    public float jumpForce;

    [Space(10)]
    public Sprite[] faces;
    public Sprite damageFace;
    public Sprite[] bodies;
    public Sprite[] hands;

    [Space(10)]
    public float damageFaceDuration;


    private Transform gripPoint;
    private Rigidbody2D rb;
    private GunController gunController;
    private float bodyRotation;
    private float gunAxisRotation;
    private int gunIndex = -1;
    private bool lookingLeft;
    private bool isDespawning = false;

    #endregion

    #region INPUTS

    private bool moveLeft = false, moveRight = false;
   // private bool jumpAvailable = false;
    private bool jumpInput = false;
    private int jumpCount = 0;
    private Vector2 mouseWorldPos;

    // CardSelectScene에서는 비활성화함
    private bool controllable = true;

    #endregion

    #region ETC

    private int groundLayer;
    private int faceIndex;
    private int bodyIndex;
    private Color deathParticleColor;
    private DeltaTimer faceTimer = new();

    // [변경] 조준 각도는 즉각적인 반응을 위해 NetworkVariable 유지 (나머지 RPC는 매니저로 이동)
    private NetworkVariable<float> netGunRotaion = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> netLookingLeft = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> netCurrHP;
    private NetworkVariable<GunType> netGunType = new(GunType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> netFaceIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> netBodyIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    #endregion

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        faceTimer.SetRunningState(false);
        netCurrHP = new(totalHP, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }
    
    // [변경] Start 대신 OnNetworkSpawn 사용 (네트워크 오브젝트가 활성화될 때 호출)
    public override void OnNetworkSpawn()
    {
        // 씬 전환 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        // NetworkVariable 변경 이벤트 구독
        netFaceIndex.OnValueChanged += OnFaceIndexChanged;
        netBodyIndex.OnValueChanged += OnBodyIndexChanged;
        netGunType.OnValueChanged += OnGunTypeChanged;
        netCurrHP.OnValueChanged += OnHPChanged;

        hpBar.SetTotalHp(totalHP);

        // [변경] 물리 및 권한 설정 (클라이언트 이동 보장)
        if (IsOwner)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            // PlayerManager에 외모 데이터를 저장한 후 ThisAppearance로부터 인덱스 정보를 불러온다.
            // 초기 한 번만 결정되고 이후에는 Appearance의 데이터가 변경되지 않는다.
            var bodyIndex = Random.Range(0, bodies.Length);
            var faceIndex = Random.Range(0, faces.Length);
            PlayerManager.Inst.SaveAppearance(bodyIndex, faceIndex, bodies[bodyIndex]);
            netBodyIndex.Value = PlayerManager.Inst.Appearance.bodyIndex;
            netFaceIndex.Value = PlayerManager.Inst.Appearance.faceIndex;

            // Stat으로부터 현재의 총 타입을 불러온다.
            netGunType.Value = PlayerManager.Inst.Stat.gunType;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            if(netBodyIndex.Value != -1)
            {
                OnBodyIndexChanged(-1, netBodyIndex.Value);
            }
            if(netFaceIndex.Value != -1)
            {
                OnFaceIndexChanged(-1, netFaceIndex.Value);
            }
            if(netGunType.Value != GunType.None)
            {
                OnGunTypeChanged(GunType.None, netGunType.Value);
            }
        }

        // 땅 레이어 저장
        groundLayer = LayerMask.NameToLayer("Ground");

        // 플레이어 상태 세팅
        SetPlayerState();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetPlayerState();
    }

    public override void OnNetworkDespawn()
    {
        netFaceIndex.OnValueChanged -= OnFaceIndexChanged;
        netBodyIndex.OnValueChanged -= OnBodyIndexChanged;
        netGunType.OnValueChanged -= OnGunTypeChanged;
        netCurrHP.OnValueChanged -= OnHPChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetPlayerState()
    {
        // 플레이어가 필요없는 씬일 경우 컨트롤 입력 비활성화
        controllable = SceneManager.GetActiveScene().name != "CardSelectScene";

        // 스폰포인트로 이동
        var spawnPoint = GameObject.FindWithTag("SpawnPoint").transform.position;
        transform.position = spawnPoint;
        rb.position = spawnPoint;

        // 시네머신 그룹에 추가
        if (IsOwner && controllable)
        {
            var targetGroup = GameObject.Find("Target Group").GetComponent<CinemachineTargetGroup>();
            targetGroup.AddMember(transform, 1f, 1f);
        }

        // 플레이어 체력 초기화
        if(IsServer)
        {
            netCurrHP.Value = totalHP;
        }

        // 플레이어 얼굴 초기화
        faceTimer.SetRunningState(false);
        faceTimer.Reset();
    }

    private void OnGunTypeChanged(GunType oldValue, GunType newValue)
    {
        if(newValue != GunType.None)
        {
            SetGun(newValue);
        }
    }

    private void OnFaceIndexChanged(int oldValue, int newValue)
    {
        if(newValue != -1)
        {
            faceIndex = newValue;
            faceRenderer.sprite = faces[faceIndex];
        }
    }

    private void OnBodyIndexChanged(int oldValue, int newValue)
    {
        if(newValue != -1)
        {
            bodyIndex = newValue;
            bodyRenderer.sprite = bodies[bodyIndex];
            foreach (var hr in handRenderer)
            {
                hr.sprite = hands[bodyIndex];
            }
            deathParticleColor = GetBodyColor(bodies[bodyIndex]);
        }
    }

    // 서버에서 데미지 계산 및 사망/승리 판정 수행
    public void OnDamageCalculated(int dmg)
    {
        if (!IsServer || isDespawning)
        {
            return;
        }

        NetworkPacketManager.Inst.PlayDamageEffectRpc(NetworkObject);

        netCurrHP.Value -= dmg;

        if (netCurrHP.Value <= 0)
        {
            isDespawning = true;

            // 사망 연출 전파
            GameManager.Inst.loserClientId.Value = OwnerClientId;
            GameManager.Inst.SetGameEnd(true);
            NetworkPacketManager.Inst.PerformDeathRpc(NetworkObject, deathParticleColor);

            // 사망 시 카드 선택 씬으로 전환
            Debug.Log("승리자 발생! 씬 전환을 시작합니다.");
        }
    }

    public void OnHPChanged(int prevValue, int newValue)
    {
        hpBar.SetCurrentHp(prevValue, newValue);
    }

    void Update()
    {
        if (!controllable) // Player가 필요없는 씬에서는 업데이트를 하지 않음
        {
            return;
        }

        if (IsOwner)
        {
            InputControl();
            netGunRotaion.Value = gunAxisRotation;
            netLookingLeft.Value = lookingLeft;
        }
        else
        {
            gunAxisRotation = netGunRotaion.Value;
            lookingLeft = netLookingLeft.Value;
        }

        // 시각적 업데이트는 공통 실행
        UpdateBody();
        UpdateGunAxis();
        UpdateGunHand();
        UpdateDamageFace();
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            UpdateMove();
        }
    }

    // isOwner == true일 때 입력 받기
    void InputControl()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;

        // [변경] wasPressedThisFrame 입력을 FixedUpdate에서 유실하지 않도록 플래그로 저장
        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpCount < 1 + PlayerManager.Inst.Stat.jumpLevel)
        {
            jumpInput = true;
        }

        // 마우스 위치 얻기
        mouseWorldPos = MouseManager.Inst.worldPos;
        gunAxisRotation = Mathf.Rad2Deg * Mathf.Atan2(mouseWorldPos.y - body.position.y, mouseWorldPos.x - body.position.x);

        // 뱡향 지정
        lookingLeft = mouseWorldPos.x < transform.position.x;

        // [수정] 총 컨트롤 (방아쇠 당기기/놓기, 재장전, 방향입력)
        if (gunController)
        {
            gunController.PullTrigger(MouseManager.Inst.IsLeftPressing());
            gunController.InputDirection(lookingLeft);
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                gunController.ReloadGun();
            }
        }
    }

    void UpdateMove()
    {
        // 좌우 이동
        if (moveLeft != moveRight)
        {
            var speed = moveSpeed + (moveSpeed * 0.2f * PlayerManager.Inst.Stat.moveSpeedLevel);
            rb.AddForce(new Vector2(moveLeft ? -moveForce : moveForce, 0f), ForceMode2D.Force);
            rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -speed, speed), rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0f, Time.fixedDeltaTime * 10f), rb.linearVelocity.y);
        }

        // 점프 // 땅에 닿으면 점프 가능
        if (jumpInput)
        {
            rb.linearVelocityY = 0f;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++; // 점프 카운트 증가
            jumpInput = false; // 사용 후 즉시 리셋 (입력 중복 방지)
        }
    }

    // 총 쥐는 손 업데이트
    void UpdateGunHand()
    {
        // gripPoint나 gunHand가 할당되지 않았다면 함수를 실행하지 않고 나감
        if (gripPoint == null || gunHand == null) return;
        gunHand.position = gripPoint.position;
        gunHand.rotation = gripPoint.rotation;
    }

    // [추가] PacketManager가 호출하는 연출 실행 함수들
    public void ExecuteDamageEffect()
    {
        faceTimer.Reset();
        faceTimer.SetRunningState(true);
    }

    public void ExecuteDeathEffect(Color deathColor)
    {
        var newParticle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        var playerDeathEffect = newParticle.GetComponent<PlayerDeath>();
        playerDeathEffect.createColor = deathColor;
    }

    #region Colison & Visual

    void OnCollisionEnter2D(Collision2D c)
    {
        // 땅 위에 있을 때 점프 가능
        if (c.collider.gameObject.layer == groundLayer)
        {
            foreach (var contact in c.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    jumpCount = 0;
                    return;
                }
            }
        }
    }

    // 몸통 업데이트
    void UpdateBody()
    {
        bodyRotation = Mathf.Lerp(bodyRotation, lookingLeft ? 180f : 0f, Time.deltaTime * 10f);
        body.rotation = Quaternion.Euler(new Vector3(0f, bodyRotation, 0f));
    }

    // 총  업데이트
    void UpdateGunAxis()
    {
        gunAxis.position = body.position;
        gunAxis.rotation = Quaternion.Euler(lookingLeft ? 180f : 0f, 0f, lookingLeft ? -gunAxisRotation : gunAxisRotation);
    }

    // 대미지를 받을 시 dmageFaceDuration 동안 대미지를 입는 표정을 짓는다
    void UpdateDamageFace()
    {
        faceTimer.Update();
        if(faceTimer.isRunning && !faceTimer.CheckTime(damageFaceDuration, CheckOption.StopReset))
        {
            faceRenderer.sprite = damageFace;
        }
        else
        {
            faceRenderer.sprite = faces[faceIndex];
        }
    }

    // 해당 타입의 총기로 설정
    void SetGun(GunType type)
    {
        // 먼저 모든 총 비활성화
        foreach(var gun in guns)
        {
            gun.gameObject.SetActive(false);
        }

        // type 파라미터에 따라 다른 총을 선택
        var selectedGun = guns[(int)type];
        selectedGun.SetActive(true);

        // 선택된 총이 가지는 GunSpec 컴포넌트에서 스펙을 불러와 적용
        var spec = selectedGun.GetComponentInChildren<GunSpec>().spec;
        gunIndex = (int)type;

        // 총의 GunController의 값 설정
        gunController = selectedGun.GetComponentInChildren<GunController>();
        gunController.InputSpec(spec, type);
        gripPoint = guns[gunIndex].transform.Find("Body").transform.Find("GripPoint").transform;
    }
    #endregion

    private Color GetBodyColor(Sprite sprite)
    {
        var texture = sprite.texture;
        int x = (texture.width / 2) - 2;
        int y = (texture.height / 2) - 2;

        // 중앙 4 x 4 픽셀을 샘플링하여 PlayerDeath 파티클 생성 시 해당 색상으로 설정
        Color[] pixels = sprite.texture.GetPixels(x, y, 4, 4);
        Color sumColor = new Color(0, 0, 0, 0);

        foreach (Color pixel in pixels)
        {
            sumColor += pixel;
        }

        float totalPixels = pixels.Length;
        Color avgColor = sumColor / totalPixels;

        return avgColor;
    }
}
