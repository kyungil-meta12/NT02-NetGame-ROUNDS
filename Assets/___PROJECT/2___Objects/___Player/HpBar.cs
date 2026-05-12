using MoreMountains.Tools;
using UnityEngine;

public class HpBar : MMProgressBar
{
    float maxValue;

    public void SetTotalHp(int hp)
    {
        float inputHp = (float)hp;
        maxValue = inputHp;
        SetBar(inputHp, 0f, inputHp);
    }

    public void SetCurrentHp(int prev, int curr)
    {
        float diff = (float)(prev - curr);
        float newProgress = BarTarget - diff / maxValue;
        newProgress = Mathf.Clamp(newProgress, 0f, 1f);
        UpdateBar01(newProgress);
    }

    public void TakeDamage(int damage)
    {
        float newProgress = BarTarget - (float)damage / maxValue;
        newProgress = Mathf.Clamp(newProgress, 0f, 1f);
        UpdateBar01(newProgress);
    }
}
