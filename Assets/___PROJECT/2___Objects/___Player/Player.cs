using UnityEngine;
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
    public CrossHair crossHair;

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

    private LayerMask groundMask;

    private Matrix4x4 handMat = new();
    private Matrix4x4 gunMat = new();
    private Matrix4x4 firePointMat = new();

    #endregion

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 시작 시 외형 랜덤 지정(타 플레이어와 겹치지 않도록)
        faceRenderer.sprite = faces[Random.Range(0, faces.Length)];
        int colorIndex = Random.Range(0, hands.Length);
        bodyRenderer.sprite = bodies[colorIndex];
        foreach (var hr in handRenderer) { hr.sprite = hands[colorIndex]; }

        // GroundMask 미리 저장
        groundMask = LayerMask.GetMask("Ground");

        // 현재 들고있는 총의 스펙 값을 기반으로 값 지정
        SetGun(currentGunType);
    }

    void Update()
    {
        InputMove();
        UpdateBodyRotation();
        UpdateGunRotation();
        UpdateHandRotation();
        UpdateFirePoint();
    }

    void FixedUpdate()
    {
        UpdateMove();
    }

    void InputMove()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;
        jumpInput = Keyboard.current.spaceKey.isPressed;
    }

    void UpdateMove()
    {
        // 좌우 이동
        if(moveLeft != moveRight)
        {
            if(moveLeft)
            {
                rb.AddForce(Vector2.left * moveForce, ForceMode2D.Force);
            }
            else if(moveRight)
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
        if(jumpAvailable && jumpInput)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpAvailable = false;
        }
    }

    void OnCollisionStay2D(Collision2D c)
    {
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

    // 총 회전 업데이트
    void UpdateGunRotation()
    {
        gunRotation = Mathf.Rad2Deg * Mathf.Atan2(MouseManager.Inst.worldPos.y - body.position.y, MouseManager.Inst.worldPos.x - body.position.x);
        mat.Identity(ref gunMat);
        mat.Translate(ref gunMat, body.position);
        mat.Rotate(ref gunMat, new Vector3(lookingLeft ? 180f : 0f, 0f, lookingLeft ? -gunRotation : gunRotation));
        mat.Translate(ref gunMat, gunOffset);
        mat.Scale(ref gunMat, Vector2.one * gunScale);
        mat.Dispatch(guns[gunIndex].transform, ref gunMat);
    }

    // 손 회전 업데이트
    void UpdateHandRotation()
    {
        mat.Identity(ref handMat);
        mat.Translate(ref handMat, body.position);
        mat.Rotate(ref handMat, new Vector3(lookingLeft ? 180f : 0f, 0f, lookingLeft ? -gunRotation : gunRotation));
        mat.Translate(ref handMat, handOffset);
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
        // 크로스헤어에 현재 들고 있는 총의 총구 위치 전달
        crossHair.InputOriginPoint(mat.WorldPos(ref firePointMat));
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
    }
}
