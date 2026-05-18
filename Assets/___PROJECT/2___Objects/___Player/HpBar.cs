
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    public Slider hpSlider;
    
    float maxValue;

    public void SetTotalHp(int hp)
    {
        float inputHp = (float)hp;
        maxValue = inputHp;
        
        //SetBar(inputHp, 0f, inputHp);
        hpSlider.maxValue = maxValue;
        hpSlider.value = inputHp;
    }

    public void SetCurrentHp(int prev, int curr)
    {
        float diff = (float)(prev - curr);
        
        // float newProgress = BarTarget - diff / maxValue;
        // newProgress = Mathf.Clamp(newProgress, 0f, 1f);
        // UpdateBar01(newProgress);
        hpSlider.value = maxValue - diff / maxValue;
    }
    
    [ContextMenu("데미지 테스트")]
    public void TakeDamage()
    {
        // float newProgress = BarTarget - 0.1f;
        // newProgress = Mathf.Clamp(newProgress, 0f, 1f);
        // UpdateBar01(newProgress);
        hpSlider.value -= 0.1f;
    }
}
