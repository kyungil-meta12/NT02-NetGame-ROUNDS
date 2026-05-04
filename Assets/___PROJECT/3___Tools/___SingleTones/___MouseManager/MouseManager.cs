using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 동작을 제어하는 싱글톤 모듈
/// </summary>
public class MouseManager : MonoBehaviour
{
    public static MouseManager Inst;
    public Vector2 worldPos;
    public Vector2 viewPos;
    public Vector2 screenPos;

    void Awake()
    {
        if(Inst && Inst != this)
        {
            DestroyImmediate(this);
            return;
        }

        Inst = this;
        DontDestroyOnLoad(this);
    }

    void Update()
    {
        screenPos = Mouse.current.position.ReadValue();
        worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        viewPos = Camera.main.ScreenToViewportPoint(screenPos);
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
