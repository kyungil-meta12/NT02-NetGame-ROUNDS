using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 동작을 제어하는 싱글톤 모듈 // 씬 전환 시 인스턴스 유지 됨
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

    private bool isInside;

    void Awake()
    {
        if (Inst && Inst != this)
        {
            DestroyImmediate(this);
            return;
        }
        Inst = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // 마우스가 스크린 내부에 있을 때만 위치 반영
        var mousePos = Mouse.current.position.ReadValue();
        isInside = mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height;
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

    /// <summary>
    /// 커서가 잠김 상태인가?
    /// </summary>
    /// <returns></returns>
    public bool IsLocked()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    /// <summary>
    /// 커서가 숨김 상태인가?
    /// </summary>
    /// <returns></returns>
    public bool IsHided()
    {
        return Cursor.visible;
    }

    /// <summary>
    /// 마우스 왼쪽 버튼이 눌려있는 상태인가?
    /// </summary>
    /// <returns></returns>
    public bool IsLeftPressing()
    {
        return isInside ? Mouse.current.leftButton.isPressed : false;
    }

    /// <summary>
    /// 마우스 왼쪽 버튼이 눌렸는가?
    /// </summary>
    /// <returns></returns>
    public bool WasLeftPressed()
    {
        return isInside ? Mouse.current.leftButton.wasPressedThisFrame : false;
    }

    /// <summary>
    /// 마우스 왼쪽 버튼을 놓았는가?
    /// </summary>
    /// <returns></returns>
    public bool WasLeftReleased()
    {
        return isInside ? Mouse.current.leftButton.wasReleasedThisFrame : false;
    }

    /// <summary>
    /// 오른쪽 버튼이 눌려있는 상태인가?
    /// </summary>
    /// <returns></returns>
    public bool IsRightPressing()
    {
        return isInside ? Mouse.current.rightButton.isPressed : false;
    }

    /// <summary>
    /// 오른쪽 버튼이 눌렸는가?
    /// </summary>
    /// <returns></returns>
    public bool WasRightPressed()
    {
        return isInside ? Mouse.current.rightButton.wasPressedThisFrame : false;
    }

    /// <summary>
    /// 마우스 오른쪽 버튼을 놓았는가?
    /// </summary>
    /// <returns></returns>
    public bool WasRightReleased()
    {
        return isInside ? Mouse.current.rightButton.wasReleasedThisFrame : false;
    }
}
