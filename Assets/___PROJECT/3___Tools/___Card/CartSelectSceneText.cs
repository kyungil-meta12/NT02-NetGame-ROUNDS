using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CartSelectSceneText : MonoBehaviour
{
    private TextMeshProUGUI text;
    private float heightOffset = 0f;
    private float rotationOffset = 0f;
    private RectTransform rt;
    private float sinNum = 0f;
    private float sinNum2 = Mathf.PI * 0.5f;
    private Vector2 basePos = new();

    public string winnerText;
    public string loserText;
    public float moveScale;
    public float moveSpeed;
    public float rotateScale;
    public float rotateSpeed;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        rt = text.GetComponent<RectTransform>();
        basePos = rt.position;
    }

    // 승자, 패자에 따라 텍스트가 달라짐
    void Start()
    {
        bool isLoser = GameManager.Inst.loserClientId.Value == NetworkManager.Singleton.LocalClientId;
        text.text = isLoser ? loserText : winnerText;
        if(!isLoser) // 승자라면 텍스트를 중앙으로 이동시킨다.
        {
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            rt.position = screenCenter;
            basePos = screenCenter;
        }
    }

    // 위아래로 조금씩 움직이고 흔들리는 애니메이션
    void Update()
    {
        sinNum += Time.deltaTime * moveSpeed;
        sinNum2 += Time.deltaTime * rotateSpeed;
        heightOffset = Mathf.Sin(sinNum) * moveScale;
        rotationOffset = Mathf.Sin(sinNum2) * rotateScale;
        rt.position = new Vector2(basePos.x, basePos.y + heightOffset);
        rt.rotation = Quaternion.Euler(0f, 0f, rotationOffset);
    }
}
