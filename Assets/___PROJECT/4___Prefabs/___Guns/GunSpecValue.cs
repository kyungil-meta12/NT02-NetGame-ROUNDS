using UnityEngine;

[CreateAssetMenu(fileName = "GunSpecValue", menuName = "Scriptable Objects/GunSpecValue")]
public class GunSpecValue : ScriptableObject
{
    public int maxAmmo;
    public int damage;
    public float ammoSpeed;
    public float reloadTime;
    public float fireInterval;
    public float recoilRecoverySpeed;
}
