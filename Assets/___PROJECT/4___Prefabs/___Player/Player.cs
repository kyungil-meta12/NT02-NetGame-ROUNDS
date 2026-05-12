using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public int totalHP = 100;

    public NetworkVariable<int> currHP = new NetworkVariable<int>(100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Space(10)]
    public HpBar hpBar;

    [Space(10)]
    public GunType currentGunType;
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

    #endregion

    #region INPUTS

    private bool moveLeft = false, moveRight = false;
    private bool jumpAvailable = false;
    private bool jumpInput = false;
    private Vector2 mouseWorldPos;

    #endregion

    #region ETC

    private int groundLayer;
    private int faceIndex;
    private int bodyIndex;
    private Color deathParticleColor;
    private DeltaTimer faceTimer = new();

    // [변경] 조준 각도는 즉각적인 반응을 위해 NetworkVariable 유지 (나머지 RPC는 매니저로 이동)
    private NetworkVariable<float> netGunRotaion = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> netLookingLeft = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> netFaceIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> netBodyIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    #endregion

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        //currHP = totalHP;
        faceTimer.SetRunningState(false);
    }
    
    // [변경] Start 대신 OnNetworkSpawn 사용 (네트워크 오브젝트가 활성화될 때 호출)
    public override void OnNetworkSpawn()
    {
        // HP 변경 이벤트 구독
        currHP.OnValueChanged += OnHpChanged;
        netFaceIndex.OnValueChanged += OnFaceIndexChanged;
        netBodyIndex.OnValueChanged += OnBodyIndexChanged;

        // 서버라면 초기 HP 설정
        if (IsServer)
        {
            currHP.Value = totalHP;
        }

        // UI 초기화 (현재 HP 비율로)
        UpdateHpUI(currHP.Value);

        // [변경] 물리 및 권한 설정 (클라이언트 이동 보장)
        if (IsOwner)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            netFaceIndex.Value = Random.Range(0, faces.Length);
            netBodyIndex.Value = Random.Range(0, bodies.Length);
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            ApplyFaceSprite(netFaceIndex.Value);
            ApplyBodySprite(netBodyIndex.Value);
        }

        groundLayer = LayerMask.NameToLayer("Ground");
        SetGun(currentGunType);
    }

    public override void OnNetworkDespawn()
    {
        // 3. [수정] 등록했던 기명 함수를 정확히 해제합니다.
        netFaceIndex.OnValueChanged -= OnFaceIndexChanged;
        netBodyIndex.OnValueChanged -= OnBodyIndexChanged;
        currHP.OnValueChanged -= OnHpChanged;
    }

    // NetworkVariable의 값이 변경되었을 때 실행되는 콜백
    private void OnHpChanged(int previousValue, int newValue)
    {
        if (hpBar == null)
        {
            return;
        }

        float currentRatio = (float)newValue / totalHP;

        float damageDelta = hpBar.BarTarget - currentRatio;

        if (damageDelta > 0)
        {
            // 데미지를 입었을 때만 TakeDamage 호출 (부드러운 감소 애니메이션)
            hpBar.TakeDamage(damageDelta);
        }
        else
        {
            // 회복되었거나 값이 초기화된 경우 즉시 반영
            hpBar.UpdateBar01(currentRatio);
        }
    }

    // 실제 HpBar UI를 갱신하는 함수
    private void UpdateHpUI(int health)
    {
        if (hpBar != null)
        {
            float ratio = (float)health / totalHP;
            hpBar.UpdateBar01(ratio); // MMProgressBar의 기능 사용
        }
    }
    // 4. [추가] 람다식 대신 사용할 콜백 메서드를 명시적으로 선언합니다.
    private void OnFaceIndexChanged(int oldValue, int newValue)
    {
        ApplyFaceSprite(newValue);
    }

    private void OnBodyIndexChanged(int oldValue, int newValue)
    {
        ApplyBodySprite(newValue);
    }

    public void ApplyFaceSprite(int index)
    {
        faceIndex = index;
        faceRenderer.sprite = faces[faceIndex];
    }

    public void ApplyBodySprite(int index)
    {
        bodyIndex = index;
        bodyRenderer.sprite = bodies[bodyIndex];
        foreach (var hr in handRenderer)
        {
            hr.sprite = hands[bodyIndex];
        }
        deathParticleColor = GetBodyColor(bodies[bodyIndex]);
    }

    void Update()
    {
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
        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpAvailable)
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
            rb.AddForce(new Vector2(moveLeft ? -moveForce : moveForce, 0f), ForceMode2D.Force);
            // [변경] linearVelocityX 대신 호환성을 위해 linearVelocity 사용
            rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -moveSpeed, moveSpeed), rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0f, Time.fixedDeltaTime * 10f), rb.linearVelocity.y);
        }

        // 점프 // 땅에 닿으면 점프 가능
        if (jumpInput)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpAvailable = false;
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

    public void OnDamageCalculated(int dmg)
    {
        if (!IsServer)
        {
            return;
        }

        currHP.Value -= dmg;
        //currHP = Mathf.Clamp(currHP, 0, totalHP);

        // [변경] 피격 연출 명령을 PacketManager를 통해 전파
        NetworkPacketManager.Inst.PlayDamageEffectRpc(NetworkObject);

        if(currHP.Value <= 0)
        {
            // [변경] 사망 연출 명령 전파 후 서버에서 제거
            NetworkPacketManager.Inst.PerformDeathRpc(NetworkObject, deathParticleColor);
            GetComponent<NetworkObject>().Despawn();
        }
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
    void OnCollisionStay2D(Collision2D c)
    {
        // 땅 위에 있을 때 점프 가능
        if (c.collider.gameObject.layer == groundLayer) 
        {
            foreach (var contact in c.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    jumpAvailable = true;
                    return;
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D c)
    {
        // 땅에서 떨어지면 점프 불가능
        if (c.collider.gameObject.layer == groundLayer)
        {
            jumpAvailable = false;
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

    Color GetBodyColor(Sprite sprite)
    {
        var texture = sprite.texture;
        int x = (texture.width / 2) - 5;
        int y = (texture.height / 2) - 5;

        // 중앙 10 x 10 픽셀을 샘플링하여 PlayerDeath 파티클 생성 시 해당 색상으로 설정
        Color[] pixels = sprite.texture.GetPixels(x, y, 10, 10);
        Color sumColor = new Color(0, 0, 0, 0);

        foreach (Color pixel in pixels)
        {
            sumColor += pixel;
        }

        float totalPixels = pixels.Length;
        Color avgColor = sumColor / totalPixels;

        return avgColor;
    }
    #endregion
}
