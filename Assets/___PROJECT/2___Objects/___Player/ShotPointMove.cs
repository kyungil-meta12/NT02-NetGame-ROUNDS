using UnityEngine;

public class ShotPointMove : MonoBehaviour
{
    void Update()
    {
        // 플레이어가 없으면 카메라를 움직이지 않는다.
        if(GameManager.Inst.serverRunning)
        {
            Vector2 currentMousePosition = MouseManager.Inst.worldPos;
            transform.position = currentMousePosition; 
        }
    }
}
