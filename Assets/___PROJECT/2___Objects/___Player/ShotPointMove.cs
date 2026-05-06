using UnityEngine;

public class ShotPointMove : MonoBehaviour
{
    void Update()
    {
        Vector2 currentMousePosition = MouseManager.Inst.worldPos;
        transform.position = currentMousePosition;
    }
}
