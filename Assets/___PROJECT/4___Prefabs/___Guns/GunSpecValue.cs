using UnityEngine;

[CreateAssetMenu(fileName = "GunSpecValue", menuName = "Scriptable Objects/GunSpecValue")]
public class GunSpecValue : ScriptableObject
{
    public Vector2 gunPositionOffset;
    public Vector2 handPositionOffset;
    public int maxAmmo;
    public int damage;
    public float reloadTime;
    public float fireInterval;
}
