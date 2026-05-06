using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 동작을 제어하는 싱글톤 모듈
/// </summary>
public class MouseManager : MonoBehaviour
{
    public static MouseManager Inst;
    [HideInInspector]
    public Vector2 worldPos;
    [HideInInspector]
    public Vector2 viewPos;
    [HideInInspector]
    public Vector2 screenPos;

    void Awake()
    {
        if (Inst && Inst != this)
        {
            DestroyImmediate(this);
            return;
        }

        Inst = this;
        DontDestroyOnLoad(this);
    }

    void Update()
    {
        // 마우스가 스크린 내부에 있을 때만 위치 반영
        var mousePos = Mouse.current.position.ReadValue();
        bool isInside = mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height;
        if(isInside) 
        {
            screenPos = mousePos;
            worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            viewPos = Camera.main.ScreenToViewportPoint(mousePos);
        }
    }

    /// <summary>
    /// 마우스 커서 숨김/표시 설정
    /// </summary>
    /// <param name="flag"></param>
    public void SetCursorVisibility(bool flag)
    {
        Cursor.visible = flag;
    }

    /// <summary>
    /// 마우스 커서 잠금/해제
    /// </summary>
    /// <param name="flag"></param>
    public void SetCursorLock(bool flag)
    {
        Cursor.lockState = flag ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
