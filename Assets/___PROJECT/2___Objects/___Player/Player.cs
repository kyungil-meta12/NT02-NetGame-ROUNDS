using UnityEngine;
using UnityEngine.InputSystem;
using mat = MatrixTransform;

public class Player : MonoBehaviour
{
    public Transform body;
    public Transform hand;
    public GameObject gun;
    public SpriteRenderer faceRenderer;
    public Sprite[] faces;
    public Sprite damageFace;

    [Space(10)]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer[] handRenderer;
    public Sprite[] bodies;
    public Sprite[] hands;

    
    public float moveForce;
    public float moveSpeed;
    public float jumpForce;

    private Rigidbody2D rb;
    private float circleColliderRadius;

    private float bodyRotation;
    private float gunRotation;
    private bool lookingLeft;
    private Vector2 gunOffset;
    private Vector2 handOffset;

    private bool moveLeft = false, moveRight = false;
    private bool jump = false;

    private Matrix4x4 handMat = new();
    private Matrix4x4 gunMat = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 총 프리펩이 가지는 GunSpec 컴포넌트에서 스펙을 불러와 적용한다
        var spec = gun.GetComponent<GunSpec>().spec;
        gunOffset = spec.gunPositionOffset;
        handOffset = spec.handPositionOffset;

        // 점프 가능 판정에 사용되는 원 콜라이더 반지름
        circleColliderRadius = GetComponent<CircleCollider2D>().radius;
    }

    void Start()
    {
        // 시작 시 외형 랜덤 지정(타 플레이어와 겹치지 않도록)
        faceRenderer.sprite = faces[Random.Range(0, faces.Length)];
        int colorIndex = Random.Range(0, hands.Length);
        bodyRenderer.sprite = bodies[colorIndex];
        foreach (var hr in handRenderer) { hr.sprite = hands[colorIndex]; }
    }

    void Update()
    {
        InputMove();
        UpdateBodyRotation();
        UpdateGunRotation();
        UpdateHandRotation();
    }

    void FixedUpdate()
    {
        UpdateMove();
    }

    void InputMove()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;
        jump = Keyboard.current.spaceKey.isPressed;
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
            rb.linearVelocityX = Mathf.Lerp(rb.linearVelocityX, 0f, Time.fixedDeltaTime * 5f);
        }

        // 점프 // 땅에 닿으면 점프 가능
        if(jump)
        {
            var groundMask = 1 << LayerMask.NameToLayer("Ground");
            if (Physics2D.Raycast(body.position, Vector2.down, circleColliderRadius + 0.01f, groundMask))
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
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
        mat.Dispatch(gun.transform, ref gunMat);
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
}
