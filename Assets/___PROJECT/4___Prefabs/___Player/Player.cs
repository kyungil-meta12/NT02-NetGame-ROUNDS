using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using mat = MatrixTransform;

public enum GunType
{
    Pistol = 0,
    Smg = 1,
    Shotgun = 2,
    AR = 3,
    Sniper = 4
}

public class Player : NetworkBehaviour
{
    #region VALUES

    public Transform body;
    public Transform hand;

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

    private Rigidbody2D rb;
    private GunController gunController;
    private float bodyRotation;
    private float gunRotation;
    private bool lookingLeft;
    private Vector2 gunOffset;
    private Vector2 handOffset;
    private Vector2 firePointOffset;
    private int gunIndex = -1;
    private float gunScale;

    private bool moveLeft = false, moveRight = false;
    private bool jumpAvailable = false;
    private bool jumpInput = false;
    private Vector2 recoilOffset;

    private LayerMask groundMask;

    private Matrix4x4 handMat = new();
    private Matrix4x4 gunMat = new();
    private Matrix4x4 firePointMat = new();

    // 네트워크 동기화를 위한 변수 (총기 회전값 전달용)
    private NetworkVariable<float> netGunRotaion = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    #endregion


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 네트워크 오브젝트가 생성될 때 실행되는 초기화 함수
    public override void OnNetworkSpawn()
    {
        // 시작 시 외형 랜덤 지정 (구조 유지)
        faceRenderer.sprite = faces[Random.Range(0, faces.Length)];
        int colorIndex = Random.Range(0, hands.Length);
        bodyRenderer.sprite = bodies[colorIndex];
        foreach (var hr in handRenderer)
        {
            hr.sprite = hands[colorIndex];
        }

        // GroundMask 미리 저장
        groundMask = LayerMask.GetMask("Ground");

        // 현재 들고있는 총의 스펙 값을 기반으로 값 지정
        SetGun(currentGunType);

        // 내 캐릭터가 아니면 물리 연산을 비활성화하여 위치 동기화 간섭 방지
        if (!IsOwner)
        {
            rb.simulated = false;
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            InputControl();
            UpdateBodyRotation();

            // 내 총의 회전값을 네트워크 변수에 기록 (타인에게 전송됨)
            netGunRotaion.Value = gunRotation;
        }
        else
        {
            // [타인 캐릭터 동기화]
            // 네트워크로부터 회전값을 받아와서 적용
            gunRotation = netGunRotaion.Value;

            // 받아온 회전값으로 바라보는 방향 판정
            lookingLeft = (gunRotation > 90 || gunRotation < -90);

            // 타인의 몸통 회전 부드럽게 동기화
            bodyRotation = Mathf.Lerp(bodyRotation, lookingLeft ? 180f : 0f, Time.deltaTime * 5f);
            body.rotation = Quaternion.Euler(new Vector3(0f, bodyRotation, 0f));
        }

        // 행렬 연산은 모든 클라이언트에서 매 프레임 실행 (부드러운 움직임)
        UpdateGunPosition();
        UpdateHandPosition();
        UpdateFirePoint();
    }

    void FixedUpdate()
    {
        // 물리 이동은 내 캐릭터(Owner)만 수행
        if (!IsOwner) return;
        UpdateMove();
    }

    void InputControl()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;

        // 점프 입력 체크
        if (jumpAvailable && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpInput = true;
        }

        // 총 방아쇠 당기기/놓기
        if (gunController != null)
        {
            gunController.PullTrigger(Mouse.current.leftButton.isPressed);
        }
    }

    // --- ServerRpc : 클라이언트가 서버에 발사 요청 ---
    [ServerRpc]
    void RequestFireServerRpc()
    {
        // 서버가 모든 클라이언트에게 발사 실행 명령
        ExecuteFireClientRpc();
    }

    // --- ClientRpc : 서버가 모든 클라이언트에게 발사 시각화 명령 ---
    [ClientRpc]
    void ExecuteFireClientRpc()
    {
        // 내 캐릭터가 아닐 때만 수동으로 이펙트를 실행하거나, 
        // 전체에게 사운드/이펙트를 출력하는 로직을 여기에 넣습니다.
        Debug.Log($"Player {OwnerClientId} Fired!");
        // 예: gunController.ShootEffect(); 
    }

    void UpdateMove()
    {
        // 좌우 이동
        if (moveLeft != moveRight)
        {
            if (moveLeft)
            {
                rb.AddForce(Vector2.left * moveForce, ForceMode2D.Force);
            }
            else if (moveRight)
            {
                rb.AddForce(Vector2.right * moveForce, ForceMode2D.Force);
            }
            // 속도 제한
            rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -moveSpeed, moveSpeed);
        }
        else
        {
            rb.linearVelocityX = Mathf.Lerp(rb.linearVelocityX, 0f, Time.fixedDeltaTime * 10f);
        }

        // 점프 // 땅에 닿으면 점프 가능
        if (jumpAvailable && jumpInput)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpAvailable = false;
            jumpInput = false;
        }
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (!IsOwner) return;

        // 땅 위에 있을 때 점프 가능
        if ((groundMask & (1 << c.gameObject.layer)) != 0)
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
        if (!IsOwner) return;

        // 땅에서 떨어지면 점프 불가능
        if ((groundMask & (1 << c.gameObject.layer)) != 0)
        {
            jumpAvailable = false;
        }
    }

    // 몸통 좌우 회전 업데이트
    void UpdateBodyRotation()
    {
        lookingLeft = MouseManager.Inst.worldPos.x < transform.position.x;
        bodyRotation = Mathf.Lerp(bodyRotation, lookingLeft ? 180f : 0f, Time.deltaTime * 5f);
        body.rotation = Quaternion.Euler(new Vector3(0f, bodyRotation, 0f));
    }

    // 총 위치 업데이트
    void UpdateGunPosition()
    {
        gunRotation = Mathf.Rad2Deg * Mathf.Atan2(MouseManager.Inst.worldPos.y - body.position.y, MouseManager.Inst.worldPos.x - body.position.x);
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

    // 해당 타입의 총기로 설정
    void SetGun(GunType type)
    {
        // 이전에 선택했었던 총이 있었다면 해당 총은 비활성화
        if (gunIndex >= 0)
        {
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
}
