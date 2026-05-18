
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    public Slider hpSlider;

    /// <summary>
    /// 최대 체력을 설정한다.
    /// </summary>
    /// <param name="hp"></param>
    public void SetTotalHp(int hp)
    {
        hpSlider.maxValue = hp;
        hpSlider.value = hp;
    }

    // 슬라이더를 최초 값으로 리셋한다.
    public void Reset()
    {
        hpSlider.value = hpSlider.maxValue;
    }

    /// <summary>
    /// 현재 체력을 설정한다.
    /// </summary>
    /// <param name="prev"></param>
    /// <param name="curr"></param>
    public void SetHp(int val)
    {
        hpSlider.value = val;
    }
}
