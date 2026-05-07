using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDeathTest : MonoBehaviour
{
    public GameObject deathEffectPrefab;

    void Update()
    {
        if(Keyboard.current.mKey.wasPressedThisFrame)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
