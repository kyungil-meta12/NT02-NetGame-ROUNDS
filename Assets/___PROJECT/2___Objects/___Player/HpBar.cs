using MoreMountains.Tools;
using UnityEngine;

public class HpBar : MMProgressBar
{
    public void TakeDamage(float damage)
    {
        float newProgress = BarTarget - damage;
        newProgress = Mathf.Clamp(newProgress, 0f, 1f);
        UpdateBar01(newProgress);
    }
}
