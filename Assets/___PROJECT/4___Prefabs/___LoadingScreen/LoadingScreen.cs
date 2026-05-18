using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    private TextMeshProUGUI text;
    private Image img;
    private float opacity = 1f;
    private bool loadCompleted = false;

    private float sinVal = 0f;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        img = GetComponentInChildren<Image>();
    }

    void Update()
    {
        sinVal += Time.deltaTime * 4f;
        var result = Mathf.Sin(sinVal) * 0.2f;
        text.rectTransform.localScale = new Vector2(1f + result, 1f - result);

        if(!loadCompleted)
        {
            if(GameManager.Inst.sceneLoadCompleted)
            {
                loadCompleted = true;
                GameManager.Inst.sceneLoadCompleted = false;
            }
        }
        else // 씬 로딩 완료 이벤트가 발생하면 로딩 스크린이 사라지기 시작한다
        {
            opacity -= Time.deltaTime * 2f;

            var textColor = text.color;
            textColor.a = opacity;
            text.color = textColor;

            var backColor = img.color;
            backColor.a = opacity;
            img.color = backColor;

            // 완전히 사라지면 삭제
            if(opacity <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
