using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 게임 매니저 싱글톤 오브젝트 // 씬 전환 시 인스턴스 유지됨
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

    void Awake(){ 
        if(Inst && Inst != this) 
        { 
            DestroyImmediate(this); 
            return; 
        } 
        Inst = this; 
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 게임매니저 객체 및 인스턴스 삭제
    /// </summary>
    public void Destroy()
    {
        Inst = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// 일시정지 상태 활성화 / 비활성화
    /// </summary>
    /// <param name="flag"></param>
    public void SetPaused(bool flag)
    {
        isPaused = flag;
    }

    /// <summary>
    /// 게임 종료 상태를 활성화
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
    
    /// <summary>
    /// 게임을 완전히 종료
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");

#if UNITY_EDITOR
        // 유니티 에디터용
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드용
        Application.Quit();
#endif
    }

    // 에디터에서 게임 모드 종료 시 네트워크 매니저를 확실하게 종료하도록 한다
    void OnApplicationQuit()
    {
#if UNITY_EDITOR
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Network Shutdown Complete");
        }
#endif
    }
}
