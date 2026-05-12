using UnityEngine;

public enum GunType
{
    Pistol = 0,
    Smg = 1,
    Shotgun = 2,
    AR = 3,
    Sniper = 4
}

public class GunSpec : MonoBehaviour
{
    public GunSpecValue spec;
}
