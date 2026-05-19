using System.Collections;
using TMPro;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
    public Transform body;
    public Transform gunHand;
    public Transform gunAxis;

    [Space(10)]
    public GameObject deathParticlePrefab;

    [Space(10)]
    public static int totalHP = 100;

    [Space(10)]
    public HpBar hpBar;

    [Space(10)]
    public TextMeshProUGUI nickNameText;

    [Space(10)]
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


    private bool moveLeft = false;
    private bool moveRight = false;
    private bool jumpInput = false;
    private int jumpCount = 0;
    private Vector2 mouseWorldPos;
    private Transform camTarget;

    private int groundLayer;
    private int faceIndex;
    private int bodyIndex;
    private Color deathParticleColor;
    private DeltaTimer faceTimer = new();


    // [변경] 조준 각도는 즉각적인 반응을 위해 NetworkVariable 유지 (나머지 RPC는 매니저로 이동)
    private NetworkVariable<float> netGunRotaion = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> netLookingLeft = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> netCurrHP = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<GunType> netGunType = new(GunType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> netFaceIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> netBodyIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<FixedString64Bytes> netNickname = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool isLobbyScene = false;
    private bool isRoundScene = false;

    public string Nickname => netNickname.Value.ToString();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        faceTimer.SetRunningState(false);
    }
    
    public override void OnNetworkSpawn()
    {
        // NetworkVariable 변경 이벤트 구독
        netFaceIndex.OnValueChanged += OnFaceIndexChanged;
        netBodyIndex.OnValueChanged += OnBodyIndexChanged;
        netGunType.OnValueChanged += OnGunTypeChanged;
        netCurrHP.OnValueChanged += OnHPChanged;
        netNickname.OnValueChanged += OnNameChanged;

        // 씬 전환 이벤트 구독
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneLoaded;

        hpBar.SetTotalHp(totalHP);
        if (IsServer)
        {
            netCurrHP.Value = totalHP;
        }

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

            // PlayerManager에서 접속할 때 입력했던 이름을 불러온다
            netNickname.Value = PlayerManager.Inst.myName;

            print($"This Player network spwaned: id: {NetworkObject.OwnerClientId}");

            // 플레이어 상태 세팅
            if(PlayerManager.Inst.testMode)
            {
                SetPlayerState();
            }

            // 첫 스폰 때는 컨트롤 활성화
            GameManager.Inst.controllable = true;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            OnBodyIndexChanged(-1, netBodyIndex.Value);
            OnFaceIndexChanged(-1, netFaceIndex.Value);
            OnGunTypeChanged(GunType.None, netGunType.Value);
            print($"Other Player network spwaned: id: {NetworkObject.OwnerClientId}");
        }

        // 땅 레이어 저장
        groundLayer = LayerMask.NameToLayer("Ground");
    }

    // 네트워크 디스폰
    public override void OnNetworkDespawn()
    {
        netFaceIndex.OnValueChanged -= OnFaceIndexChanged;
        netBodyIndex.OnValueChanged -= OnBodyIndexChanged;
        netGunType.OnValueChanged -= OnGunTypeChanged;
        netCurrHP.OnValueChanged -= OnHPChanged;
        netNickname.OnValueChanged -= OnNameChanged;

        if (IsOwner)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneLoaded;
        }
    }

    void Update()
    {
        if (!IsSpawned || !isRoundScene || !GameManager.Inst.controllable || NetworkPacketManager.Inst.sceneSwitching) // Player가 필요없는 씬에서는 업데이트를 하지 않음
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
        if (!IsSpawned || !isRoundScene || !GameManager.Inst.controllable || NetworkPacketManager.Inst.sceneSwitching) // Player가 필요없는 씬에서는 업데이트를 하지 않음
        {
            return;
        }
        
        if (IsOwner)
        {
            UpdateMove();
        }
    }

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

    // Player는 씬 전환 시 삭제되지 않고 현재의 인스턴스가 그대로 다음 씬으로 이동하기 때문에, 
    // 씬 전환 이벤트를 통해 플레이어의 상태를 초기화한다.
    void OnSceneLoaded(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete || sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
        {
            // 현재 씬이 라운드/로비 씬이 아닐 때만 조작 가능하도록 설정
            string currentScene = SceneManager.GetActiveScene().name;
            isRoundScene = currentScene != "CardSelectScene" && currentScene != "ResultScene";
            isLobbyScene = currentScene == "LobbyScene";

            if (IsOwner)
            {
                SetPlayerState();
            }
        }
    }

    // 플레이어 상태 설정
    private void SetPlayerState()
    {
        // 기초 보안 체크 (PlayerManager가 없으면 중단)
        if(PlayerManager.Inst == null)
        {
            Debug.Log($"PlayerManager를 찾을 수 없어 설정을 중단합니다.");
            return;
        }

        // 스폰 위치로 이동
        // 씬마다 "PlayerN_pawnPoint" 태그가 달린 오브젝트가 있어야함.
        // 호스트는 좌측, 클라이언트는 우측에 배치
        GameObject spawnObj = GameObject.Find(IsHost ? "Player1_SpawnPoint" : "Player2_SpawnPoint");
        if(spawnObj)
        {
            Vector2 spawnPos = spawnObj.transform.position;
            transform.position = spawnPos;
            if (rb != null)
            {
                rb.position = spawnPos;
                rb.linearVelocity = Vector2.zero;
            }
        
            // 물리 및 입력 초기화
            jumpCount = 0;
            jumpInput = false;

            // 조작 가능한 플레이 씬(스테이지)일 경우 처리
            if (isRoundScene)
            {
                // 시네머신 타겟 그룹에 나를 추가 (카메라가 추적하도록)
                GameObject tragetGroundObj = GameObject.Find("Target Group");
                if(tragetGroundObj != null)
                {
                    var targetGroup = tragetGroundObj.GetComponent<CinemachineTargetGroup>();
                    if (targetGroup != null)
                    {
                        // 이미 추가되어 있는지 확인 후 추가 (중복 방지)
                        if(targetGroup.FindMember(transform) < 0)
                        {
                            targetGroup.AddMember(transform, 1f, 1f);
                            Debug.Log("시네머신 타겟 그룹에 플레이어 추가 완료");
                        }
                    }
                }

                // 카메라 타겟 찾기
                GameObject camTargetObj = GameObject.Find("CameraTargetShotPoint");
                if(camTargetObj)
                {
                    camTarget = camTargetObj.transform;
                }
                else
                {
                    camTarget = null;
                }

                // 총기 상태 초기화 (총기 컨트롤러가 있을 때만 동작)
                if(gunController)
                {
                    netGunType.Value = PlayerManager.Inst.Stat.gunType;
                    gunController.ResetGun();
                }

                // 얼굴 타이머 초기화 (변수가 있을 때만)
                if(faceTimer != null)
                {
                    faceTimer.SetRunningState(false);
                    faceTimer.Reset();
                }
            }

            // PlayerManager 데이터 기반으로 외형/총기 최종 적용
            ApplyPlayerManagerData();
        }

        else
        {
            Debug.LogError("스폰 포인트를 찾을 수 없음");
        }
    }

    // 가독성을 위해 데이터 적용 부분만 분리
    private void ApplyPlayerManagerData()
    {
        var stat = PlayerManager.Inst.Stat;
        var apperarance = PlayerManager.Inst.Appearance;

        if (bodies != null && apperarance.bodyIndex < bodies.Length)
            bodyRenderer.sprite = bodies[apperarance.bodyIndex];

        if (guns != null && guns.Length > 0)
            SetGun(stat.gunType);
    }

    // 총 타입 변경 이벤트
    private void OnGunTypeChanged(GunType oldValue, GunType newValue)
    {
        if(newValue != GunType.None)
        {
            SetGun(newValue);
        }
    }

    // 얼굴 인덱스 변경 이벤트
    private void OnFaceIndexChanged(int oldValue, int newValue)
    {
        if(newValue != -1)
        {
            faceIndex = newValue;
            faceRenderer.sprite = faces[faceIndex];
        }
    }

    // 몸 인덱스 변경 이벤트
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

    // 대미지 발생 이벤트 // 서버에서 대미지 계산 및 사망/승리 판정 수행
    public void OnDamageCalculated(int dmg)
    {
        if (!IsServer)
        {
            return;
        }

        NetworkPacketManager.Inst.PlayDamageEffectRpc(NetworkObject);

        if(netCurrHP.Value <= 0)
        {
            return;
        }

        netCurrHP.Value -= dmg;
        if (netCurrHP.Value <= 0)
        {
            // 사망 연출 전파
            GameManager.Inst.loserClientId.Value = OwnerClientId;
            GameManager.Inst.SetGameEnd(true);
            NetworkPacketManager.Inst.PerformDeathRpc(NetworkObject, deathParticleColor);

            // 사망 시 카드 선택 씬으로 전환
            Debug.Log("승리자 발생! 씬 전환을 시작합니다.");
        }
    }

    // HP 변경 이벤트
    public void OnHPChanged(int prevValue, int newValue)
    {
        if(prevValue > newValue)
        {
            if(IsOwner && DamageFeedback.Inst)
            {
                DamageFeedback.Inst.SetFeedback();
            }
        }
        hpBar.SetHp(newValue); // 슬라이더 값 변경
    }

    // 이름 변경 이벤트
    public void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes curr)
    {
        nickNameText.text = curr.ToString();
    }

    // 대미지를 받으면 얼굴 표정이 바뀐다.
    public void ExecuteDamageEffect()
    {
        faceTimer.Reset();
        faceTimer.SetRunningState(true);
    }

    // 죽으면 파티클을 생성한다
    public void ExecuteDeathEffect(Color deathColor)
    {
        var newParticle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        var playerDeathEffect = newParticle.GetComponent<PlayerDeath>();
        playerDeathEffect.createColor = deathColor;
    }
    
    // isOwner == true일 때 입력 받기
    void InputControl()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;

        // 마우스 위치 얻기
        mouseWorldPos = MouseManager.Inst.worldPos;
        
        // 뱡향 지정
        lookingLeft = mouseWorldPos.x < transform.position.x;

        // 카메라 타겟에 위치 지정
        // 마우스가 있는 방향으로 약간의 오프셋을 주도록 한다
        var vec2Pos = (Vector2)transform.position;
        if(camTarget)
        {
            camTarget.position = vec2Pos + (mouseWorldPos - vec2Pos) * 0.5f;
        }

        // [변경] wasPressedThisFrame 입력을 FixedUpdate에서 유실하지 않도록 플래그로 저장
        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpCount < 1 + PlayerManager.Inst.Stat.jumpLevel)
        {
            jumpInput = true;
        }

        gunAxisRotation = Mathf.Rad2Deg * Mathf.Atan2(mouseWorldPos.y - body.position.y, mouseWorldPos.x - body.position.x);

        if(gunController)
        {
            gunController.InputDirection(lookingLeft);
        }

        // [수정] 총 컨트롤 (방아쇠 당기기/놓기, 재장전, 방향입력)
        // 로비에서는 총이 동작하지 않도록 한다
        if (!isLobbyScene)
        {
            if(gunController)
            {
                gunController.PullTrigger(MouseManager.Inst.IsLeftPressing());
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    gunController.ReloadGun();
                }
            }
        }
    }

    // 움직임 업데이트
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

    // 몸통 업데이트
    void UpdateBody()
    {
        bodyRotation = Mathf.Lerp(bodyRotation, lookingLeft ? 180f : 0f, Time.deltaTime * 10f);
        body.rotation = Quaternion.Euler(new Vector3(0f, bodyRotation, 0f));
    }

    // 총 회전축 업데이트
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

    // 몸 색상 스프라이트 샘플링하여 색상 얻기
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
