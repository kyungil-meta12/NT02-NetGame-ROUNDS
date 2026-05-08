using Unity.VisualScripting;
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

    // 플레이어 자신이 조작하는 오브젝트인지?
    public bool isOwner = true;

    public Transform body;
    public Transform gunHand;
    public Transform gunAxis;

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
    private Color deathParticleColor;
    private DeltaTimer faceTimer = new();

    #endregion


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currHP = totalHP;
        faceTimer.SetRunningState(false);
    }

    void Start()
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
    }

    void Update()
    {
        if (isOwner) // 자신이 조작하는 경우에만 입력 받음
        {
            InputControl();
        }
        else // 아니라면 서버로부터 패킷을 받아 처리(위치, 손의 회전 각도 등)
        {
            InputPacket();
        }
        UpdateBody();
        UpdateGunAxis();
        UpdateGunHand();
        UpdateDamageFace();
    }

    void FixedUpdate()
    {
        UpdateMove();
    }

    // isOwner == true일 때 입력 받기
    void InputControl()
    {
        moveLeft = Keyboard.current.aKey.isPressed;
        moveRight = Keyboard.current.dKey.isPressed;
        jumpInput = jumpAvailable && Keyboard.current.spaceKey.wasPressedThisFrame;
        gunAxisRotation = Mathf.Rad2Deg * Mathf.Atan2(MouseManager.Inst.worldPos.y - body.position.y, MouseManager.Inst.worldPos.x - body.position.x);

        // 마우스 위치 얻기
        mouseWorldPos = MouseManager.Inst.worldPos;

        // 뱡향 지정
        lookingLeft = mouseWorldPos.x < transform.position.x;
        
        // 총 방아쇠 당기기/놓기
        gunController.PullTrigger(MouseManager.Inst.IsLeftPressing());

        // 방향 입력
        gunController.InputDirection(lookingLeft);
    }

    // isOwner == false일 때 패킷 처리하기
    void InputPacket()
    {
        
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
        // 현재 체력에서 dmg 만큼 차감
        currHP -= dmg;
        currHP = Mathf.Clamp(currHP, 0, totalHP);

        // 대미지를 받은 표정으로 변경한다
        faceTimer.Reset();
        faceTimer.SetRunningState(true);

        // 체력이 완전히 떨어지게 되면 파티클을 생성한 후 삭제된다
        if(currHP == 0)
        {
            // 사망 파티클의 색상이 플레이어의 body 스프라이트 색상에 맞춰짐
            var newParticle = Instantiate(deathParticlePrefab, transform.position,Quaternion.identity);
            newParticle.GetComponent<PlayerDeath>().createColor = deathParticleColor;
            Destroy(gameObject);
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

    // 총 쥐는 손 업데이트
    void UpdateGunHand()
    {
        gunHand.position = gripPoint.position;
        gunHand.rotation = gripPoint.rotation;
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
}
