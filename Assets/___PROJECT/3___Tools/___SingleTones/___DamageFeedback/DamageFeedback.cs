using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어가 대미지를 받으면 피드백을 재생하는 모듈  // 씬 전환 시 인스턴스 삭제 됨
/// </summary>
public class DamageFeedback : MonoBehaviour
{
    public static DamageFeedback Inst;

    private Image img;
    private float opacity = 0f;

    void Awake()
    {
        if(Inst && Inst != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        img = GetComponentInChildren<Image>();
        SetOpacity(opacity);
        Inst = this;
    }

    void OnDestroy()
    {
        Inst = null;    
    }

    void SetOpacity(float val)
    {
        var color = img.color;
        color.a = val;
        img.color = color;
    }
    

    void Update()
    {
        opacity -= Time.deltaTime * 3f;
        opacity = Mathf.Clamp(opacity, 0f, 1f);
        SetOpacity(opacity);
    }

    public void SetFeedback()
    {
        opacity = 0.8f;
    }
}
