using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using mat = MatrixTransform;

public enum GunType {
    Pistol = 0,
    Smg = 1,
    Shotgun = 2,
    AR = 3,
    Sniper = 4
}

public class Player : NetworkBehaviour
{
    #region VALUES

    // 플레이어 자신이 조작하는 오브젝트인지?
    // public bool isOwner = true; Netcode 내장 속성인 IsOwner 를 사용 하여 네트워크 권한 판별

    public Transform body;
    public Transform hand;

    [Space(10)]
    public GameObject deathParticlePrefab;

    [Space(10)]
    public int totalHP;
    private int currHP;

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

    private Rigidbody2D rb;
    private GunController gunController;
    private float bodyRotation;
    private float gunRotation;
    private bool lookingLeft;
    private Vector2 gunOffset;
    private Vector2 handOffset;
    private Vector2 firePointOffset;
    private Vector2 recoilOffset;
    private int gunIndex = -1;
    private float gunScale;

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

    private Matrix4x4 handMat = new();
    private Matrix4x4 gunMat = new();
    private Matrix4x4 firePointMat = new();

    private Color deathParticleColor;

    private DeltaTimer faceTimer = new();

    // 조준 각도를 동기화하기 위한 네트워크 변수
    private NetworkVariable<float> netGunRotaion = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    #endregion


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currHP = totalHP;
    }

    // [변경] Start 대신 OnNetworkSpawn 사용 (네트워크 오브젝트가 활성화될 때 호출)
    public override void OnNetworkSpawn()
    {
        // 외형 랜덤 지정
        faceIndex = Random.Range(0, faces.Length);
        faceRenderer.sprite = faces[faceIndex];
        int colorIndex = Random.Range(0, hands.Length);
        bodyRenderer.sprite = bodies[colorIndex];
        foreach (var hr in handRenderer) { hr.sprite = hands[colorIndex]; }

        // PlayerDeath 파티클에 사용할 색상 구하기 // 선택된 body 스프라이트를 샘플링하여 색상 구함
        deathParticleColor = GetBodyColor(bodies[colorIndex]);

        // 땅 레이어 저장
        groundLayer = LayerMask.NameToLayer("Ground");

        // 현재 들고있는 총의 스펙 값을 기반으로 값 지정
        SetGun(currentGunType);

        // [추가] 내가 아닌 캐릭터의 물리 연산 비활성화
        // 소유자가 아닌 클라이언트에서 물리 시뮬레이션이 돌면 서버 위치와 충돌하여 캐릭터가 떨림.
        if (!IsOwner)
        {
            rb.simulated = false;
        }
    }

    void Update()
    {
        if (IsOwner) // 자신이 조작하는 경우에만 입력 받음
        {
            InputControl();
            // 내 조준 각도를 네트워크 변수에 기록하여 다른 플레이어들에게 전송
            netGunRotaion.Value = gunRotation;
        }
        else // 아니라면 서버로부터 패킷을 받아 처리(위치, 손의 회전 각도 등)
        {
            // [통합] InputPacket(); 대신사용
            // 타인의 경우 네트워크 변수로부터 각도를 받아와 동기화
            gunRotation = netGunRotaion.Value;
            // 각도에 따라 마우스 위치를 가상으로 설정하여 lookingLeft 로직 유지
            mouseWorldPos = (gunRotation > 90 || gunRotation < -90) ?
                (Vector2)transform.position + Vector2.left :
                (Vector2)transform.position + Vector2.right;
        }

        UpdateBodyRotation();
        UpdateGunPosition();
        UpdateHandPosition();
        UpdateFirePoint();
        UpdateDamageFace();
    }

    void FixedUpdate()
    {
        // 물리 이동은 소유자(Owner)만 수행함.
        if (!IsOwner) return;
        UpdateMove();
    }

    // isOwner == true일 때 입력 받기
    void InputControl()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;
        jumpInput = jumpAvailable && Keyboard.current.spaceKey.wasPressedThisFrame;
        gunRotation = Mathf.Rad2Deg * Mathf.Atan2(MouseManager.Inst.worldPos.y - body.position.y, MouseManager.Inst.worldPos.x - body.position.x);

        // 마우스 위치 얻기
        mouseWorldPos = MouseManager.Inst.worldPos;

        // 총 방아쇠 당기기/놓기
        gunController.PullTrigger(MouseManager.Inst.IsLeftPressing());
    }

    //// isOwner == false일 때 패킷 처리하기
    //void InputPacket()
    //{

    //} [삭제] Update의 else 문으로 통합됨.

    // [RPC 추가 수정 : 데미지 및 사망로직] 
    public void GiveDamage(int dmg)
    {
        // 서버에게 데미지 계산을 요청
        RequestDamageServerRpc(dmg);
    }

    [Rpc(SendTo.Server)] // 서버에서 실행
    private void RequestDamageServerRpc(int dmg)
    {
        if (currHP <= 0) return;

        currHP -= dmg;
        currHP = Mathf.Clamp(currHP, 0, totalHP);

        // 모든 클라이언트에게 피격 연출 명령
        PlayDamageEffectRpc();

        if (currHP == 0)
        {
            // 모든 클라이언트에게 사망 효과 명령 후 오브젝트 제거
            PerformDeathRpc();
            GetComponent<NetworkObject>().Despawn(); // 서버에서 오브젝트 제거 (모든 클라이언트 동기화)
        }
    }

    [Rpc(SendTo.Everyone)] // 모든 클라이언트에서 실행
    private void PlayDamageEffectRpc()
    {
        faceTimer.Reset();
        faceTimer.SetRunningState(true);
    }

    [Rpc(SendTo.Everyone)]
    private void PerformDeathRpc()
    {
        var newParticle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        newParticle.GetComponent<PlayerDeath>().createColor = deathParticleColor;
        // [삭제] Destroy(gameObject) -> 서버의 Despawn()이 처리함.
    }

    void UpdateMove()
    {
        // 좌우 이동
        if(moveLeft != moveRight)
        {
            rb.AddForce(new Vector2(moveLeft ? -moveForce : moveForce, 0f), ForceMode2D.Force);
            // 속도 제한
            rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -moveSpeed, moveSpeed);
        }
        else
        {
            rb.linearVelocityX = Mathf.Lerp(rb.linearVelocityX, 0f, Time.fixedDeltaTime * 10f);
        }

        // 점프 // 땅에 닿으면 점프 가능
        if(jumpInput)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpAvailable = false;
            jumpInput = false;
        }
    }

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

    // 몸통 좌우 회전 업데이트
    void UpdateBodyRotation()
    {
        lookingLeft = mouseWorldPos.x < transform.position.x;
        bodyRotation = Mathf.Lerp(bodyRotation, lookingLeft ? 180f : 0f, Time.deltaTime * 10f);
        body.rotation = Quaternion.Euler(new Vector3(0f, bodyRotation, 0f));
    }

    // 총 위치 업데이트
    void UpdateGunPosition()
    {
        recoilOffset = gunController.recoilOffset;
        mat.Identity(ref gunMat);
        mat.Translate(ref gunMat, body.position);
        mat.Rotate(ref gunMat, new Vector3(lookingLeft ? 180f : 0f, 0f, lookingLeft ? -gunRotation : gunRotation));
        mat.Translate(ref gunMat, gunOffset - recoilOffset);
        mat.Scale(ref gunMat, Vector2.one * gunScale);
        mat.Dispatch(guns[gunIndex].transform, ref gunMat);
        gunController.InputRotation(gunRotation);
    }

    // 손 위치 업데이트
    void UpdateHandPosition()
    {
        mat.Identity(ref handMat);
        mat.Translate(ref handMat, body.position);
        mat.Rotate(ref handMat, new Vector3(lookingLeft ? 180f : 0f, 0f, lookingLeft ? -gunRotation : gunRotation));
        mat.Translate(ref handMat, handOffset - recoilOffset);
        mat.Scale(ref handMat, Vector2.one * 0.7f);
        mat.Dispatch(hand, ref handMat);
    }

    // 총 화염 위치 업데이트
    void UpdateFirePoint()
    {
        mat.Identity(ref firePointMat);
        mat.Translate(ref firePointMat, body.position);
        mat.Rotate(ref firePointMat, new Vector3(lookingLeft ? 180f : 0f, 0f, lookingLeft ? -gunRotation : gunRotation));
        mat.Translate(ref firePointMat, firePointOffset);
        gunController.InputFirePoint(mat.WorldPos(ref firePointMat));
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
        // 이전에 선택했었던 총이 있었다면 해당 총은 비활성화
        if(gunIndex >= 0) {
            guns[gunIndex].SetActive(false);
        }

        // type 파라미터에 따라 다른 총을 선택
        var selectedGun = guns[(int)type];
        selectedGun.SetActive(true);

        // 선택된 총이 가지는 GunSpec 컴포넌트에서 스펙을 불러와 적용
        var spec = selectedGun.GetComponent<GunSpec>().spec;
        gunIndex = (int)type;
        gunOffset = spec.gunPositionOffset;
        handOffset = spec.handPositionOffset;
        firePointOffset = spec.firePointOffset;
        gunScale = spec.globalScale;

        // 총의 GunController의 값 설정
        gunController = selectedGun.GetComponent<GunController>();
        gunController.InputSpec(spec, type);
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
}
