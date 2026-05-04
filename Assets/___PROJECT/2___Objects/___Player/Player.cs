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

    private float bodyRotation;
    private float gunRotation;
    private bool lookingLeft;
    private Vector2 gunOffset;
    private Vector2 handOffset;

    private Matrix4x4 handMat = new();
    private Matrix4x4 gunMat = new();

    void Start()
    {
        // 시작 시 외형 랜덤 지정(타 플레이어와 겹치지 않도록)
        faceRenderer.sprite = faces[Random.Range(0, faces.Length)];
        bodyRenderer.sprite = bodies[Random.Range(0, bodies.Length)];
        int handIndex = Random.Range(0, hands.Length);
        foreach (var hr in handRenderer) { hr.sprite = hands[handIndex]; }

        // 총 프리펩이 가지는 GunSpec 컴포넌트에서 스펙을 불러와 적용한다
        var spec = gun.GetComponent<GunSpec>().spec;
        gunOffset = spec.gunPositionOffset;
        handOffset = spec.handPositionOffset;
    }

    void Update()
    {
        UpdateBodyRotation();
        UpdateGunRotation();
        UpdateHandRotation();
    }

    // 몸통 좌우 회전 업데이트
    void UpdateBodyRotation()
    {
        lookingLeft = MouseManager.Inst.worldPos.x < transform.position.x;
        bodyRotation = Mathf.Lerp(bodyRotation, lookingLeft ? 180f : 0f, Time.deltaTime * 10f);
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
