using UnityEngine;

[CreateAssetMenu(fileName = "GunSpecValue", menuName = "Scriptable Objects/GunSpecValue")]
public class GunSpecValue : ScriptableObject
{
    public Vector2 gunPositionOffset;
    public Vector2 handPositionOffset;
    public Vector2 firePointOffset;
    public float globalScale;

    [Space(10)]
    public int maxAmmo;
    public int damage;
    public float reloadTime;
    public float fireInterval;
}
