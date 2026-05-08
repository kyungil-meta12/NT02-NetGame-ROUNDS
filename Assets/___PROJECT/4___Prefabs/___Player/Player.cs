using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using mat = MatrixTransform;

public enum GunType {
    Pistol = 0,
    Smg = 1,
    Shotgun = 2,
    AR = 3,
    Sniper = 4
}

public class Player : MonoBehaviour
{
    #region VALUES

    public bool controllable = true;

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
    private float currDamageFaceTime;

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

    private int groundLayer;
    private int faceIndex;

    private Matrix4x4 handMat = new();
    private Matrix4x4 gunMat = new();
    private Matrix4x4 firePointMat = new();

    private NetworkVariable<float> newtGunRotaion = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // 네트워크상에서 체력을 동기화 (선택 사항 : UI 등에 표시할 때 유용)
    private NetworkVariable<int> netCurrHP = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #endregion

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currHP = totalHP;
    }

    void Start()
    {
        // 시작 시 외형 랜덤 지정(타 플레이어와 겹치지 않도록)
        faceIndex = Random.Range(0, faces.Length);
        faceRenderer.sprite = faces[faceIndex];
        int colorIndex = Random.Range(0, hands.Length);
        bodyRenderer.sprite = bodies[colorIndex];
        foreach (var hr in handRenderer) { hr.sprite = hands[colorIndex]; }

        // 땅 레이어 저장
        groundLayer = LayerMask.NameToLayer("Ground");

        // 현재 들고있는 총의 스펙 값을 기반으로 값 지정
        SetGun(currentGunType);
    }

    void Update()
    {
        if (controllable) 
        {
            InputControl();
        }
        UpdateBodyRotation();
        UpdateGunPosition();
        UpdateHandPosition();
        UpdateFirePoint();
        UpdateDamageFace();
    }

    void FixedUpdate()
    {
        UpdateMove();
    }

    void InputControl()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;
        jumpInput = jumpAvailable && Keyboard.current.spaceKey.wasPressedThisFrame;
        gunRotation = Mathf.Rad2Deg * Mathf.Atan2(MouseManager.Inst.worldPos.y - body.position.y, MouseManager.Inst.worldPos.x - body.position.x);

        // 총 방아쇠 당기기/놓기
        gunController.PullTrigger(Mouse.current.leftButton.isPressed);
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

    // 대미지 부여
    public void GiveDamage(int dmg)
    {
        currHP -= dmg;
        currHP = Mathf.Clamp(currHP, 0, totalHP);

        // 대미지를 받은 표정으로 변경한다
        currDamageFaceTime = damageFaceDuration;

        // 체력이 완전히 떨어지게 되면 파티클을 생성한 후 삭제된다
        if(currHP == 0)
        {
            Instantiate(deathParticlePrefab, transform.position,Quaternion.identity);
            Destroy(gameObject);
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
        if (currDamageFaceTime > 0f)
        {
            currDamageFaceTime -= Time.deltaTime;
            if (faceRenderer.sprite != damageFace)
            {
                faceRenderer.sprite = damageFace;
            }
        }
        else
        {
            if (faceRenderer.sprite != faces[faceIndex])
            {
                faceRenderer.sprite = faces[faceIndex];
            }
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
}
