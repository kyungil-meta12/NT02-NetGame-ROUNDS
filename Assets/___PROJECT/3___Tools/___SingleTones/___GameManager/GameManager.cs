using UnityEngine;

/// <summary>
/// 게임 매니저 싱글톤 오브젝트 // 씬 전환 시 인스턴스 삭제됨
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Inst;

    [HideInInspector]
    public bool isPaused = false;
    [HideInInspector]
    public bool isGameEnd = false;
    [HideInInspector]
    public int currentRound = 1; // 1부터 시작

    void Awake(){ if(Inst && Inst != this) { DestroyImmediate(this); return; } Inst = this; }
    void OnDestroy() { Inst = null; }

    /// <summary>
    /// 일시정지 상태 활성화 / 비활성화
    /// </summary>
    /// <param name="flag"></param>
    public void SetPaused(bool flag)
    {
        isPaused = flag;
    }

    /// <summary>
    /// 게임 종료 상태 활성화
    /// </summary>
    public void SetGameEnd()
    {
        isGameEnd = true;
    }

    /// <summary>
    /// 라운드 증가
    /// </summary>
    public void IncreaseRound()
    {
        currentRound++;
    }
}
