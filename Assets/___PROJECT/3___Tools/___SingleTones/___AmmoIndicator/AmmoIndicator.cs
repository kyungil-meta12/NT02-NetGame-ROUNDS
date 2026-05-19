using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 총의 장탄수를 보여주는 싱글톤 모듈
/// 씬 전환 시 인스턴스 삭제 됨
/// </summary>
public class AmmoIndicator : MonoBehaviour
{
    public static AmmoIndicator Inst;

    public TextMeshProUGUI numText;
    public Image icon;
    public Image reloadBar;
    public Sprite[] shellIcons;

    private int prevAmmo;
    private int currAmmo;

    private RectTransform numTextRt;
    private Vector2 originNumTextScale;
    private Vector2 numTextScaleOffset;
    private RectTransform reloadBarRt;
    private Vector2 originReloadBarScale;
    private Vector2 reloadBarScale;

    void Awake()
    {
        if(Inst && Inst != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        numTextRt = numText.GetComponent<RectTransform>();
        reloadBarRt = reloadBar.GetComponent<RectTransform>();
        originNumTextScale = numTextRt.localScale;
        originReloadBarScale = reloadBarRt.localScale;
        Inst = this;
    }

    void OnDestroy()
    {
        Inst = null;
    }

    void Update()
    {
        // 장탄수에 변화가 발생할 경우 피드백을 재생한다
        numTextScaleOffset -= Vector2.one * Time.deltaTime * 10f;
        numTextScaleOffset = VectorClamp.MonoLimit(numTextScaleOffset, Vector2.zero, ClampDir.Min);
        numTextRt.localScale = originNumTextScale + numTextScaleOffset;

        // 재장전 바를 업데이트 한다
        reloadBarRt.localScale = reloadBarScale;
    }

    /// <summary>
    /// 장탄수를 초기설정한다
    /// </summary>
    /// <param name="ammo"></param>
    public void InitAmmo(int ammo)
    {
        numText.text = ammo.ToString();
        numText.color = Color.white;
        prevAmmo = ammo;
        currAmmo = ammo;
    }

    /// <summary>
    ///  현재 장탄수를 입력한다
    /// </summary>
    /// <param name="ammo"></param>
    public void InputAmmo(int ammo)
    {
        numText.text = ammo > 0 ? ammo.ToString() : "R";
        numText.color = ammo > 0 ? Color.white : Color.red;
        currAmmo = ammo;
        if(prevAmmo != currAmmo)
        {
            numTextScaleOffset = Vector2.one;
            prevAmmo = currAmmo;
        }
    }

    /// <summary>
    /// 재장전 시간을 입력한다
    /// </summary>
    /// <param name="time"></param>
    public void InputReloadTime(float currTime, float duration)
    {
        reloadBarScale.y = originReloadBarScale.y;
        reloadBarScale.x = originReloadBarScale.x * currTime / duration;
    }

    /// <summary>
    /// 총 타입을 입력한다
    /// </summary>
    /// <param name="type"></param>
    public void InputGunType(GunType type)
    {
        icon.sprite = shellIcons[(int)type];
    }
}
