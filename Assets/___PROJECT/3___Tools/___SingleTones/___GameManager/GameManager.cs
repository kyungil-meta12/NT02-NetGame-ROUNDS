using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 매니저 싱글톤 오브젝트 // 씬 전환 시 인스턴스 유지됨
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Inst;

    [HideInInspector]
    public bool isPaused = false;

    [HideInInspector]
    public bool isGameEnd = false;

    [HideInInspector]
    public int maxRound = 3;

    [HideInInspector]
    public bool serverRunning = false; // 서버 동작 여부

    [HideInInspector]
    public string localIP;

    // 패배한 플레이어의 CliendId를 저장 (기본값 999)
    // Server만 작성 가능, Evryone 읽기가능
    public NetworkVariable<ulong> loserClientId = new NetworkVariable<ulong>(999,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 서버에서 관리하는 현재 라운드
    public NetworkVariable<int>currentRound = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public TMPro.TMP_InputField ipInputField;

    void Awake()
    { 
        if(Inst && Inst != this) 
        { 
            DestroyImmediate(this); 
            return; 
        } 
        Inst = this; 
        DontDestroyOnLoad(gameObject);
    }

    public void SetupResultUI(GameObject victoryUI, GameObject defeatUI)
    {
        if (NetworkManager.Singleton == null) return;

        ulong myId = NetworkManager.Singleton.LocalClientId;
        ulong loserId = loserClientId.Value;

        // 패널 초기화 (둘 다 끄기)
        victoryUI.SetActive(false);
        defeatUI.SetActive(false);

        // 승패 판정 후 해당 패널만 활성화
        if (myId == loserId)
        {
            defeatUI.SetActive(true);
            Debug.Log($"결과 화면 : 패배 패널 활성화");
        }
        else
        {
            victoryUI.SetActive(true);
            Debug.Log("결과 화면 : 승리 패널 활성화");
        }
    }

    /// <summary>
    /// 게임매니저 객체 및 인스턴스 삭제
    /// </summary>
    public void Destroy()
    {
        Inst = null;
        Destroy(gameObject);
    }

    // 호스트 버튼 연결용
    public void StartHost()
    {
        if (serverRunning) return;

        // 호스트 시작
        if (NetworkManager.Singleton.StartHost())
        {
            serverRunning = true;
            Debug.Log($"호스트 시작 성공!");

            NetworkManager.Singleton.SceneManager.LoadScene("Round1", LoadSceneMode.Single);
        }
        else
        {
            Debug.Log($"호스트 시작 실패!");
        }       
    }

    // 조인 버튼 연결용
    public void StartClient()
    {
        if(NetworkManager.Singleton == null)
        {
            Debug.Log($"NetworkManager가 씬에 없습니다.");
            return;
        }

        if(ipInputField == null)
        {
            Debug.Log($"InputField가 연결되지 않았습니다! 인스펙터에서 드래그해주세요.");
            return;
        }

        if (serverRunning) return;

        // IP 주소 설정
        // 입력창이 비어있으면 로컬(127.0.0.1), 적혀있으면 해당 IP 사용
        string targetIP = string.IsNullOrEmpty(ipInputField.text) ? "127.0.0.1" : ipInputField.text;

        // NetworkManager에 붙어있는 Transport 컴포넌트를 가져와서 주소값 변경
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = targetIP;

        // 클라이언트 시작
        if (NetworkManager.Singleton.StartClient())
        {
            serverRunning = true;
            Debug.Log($"{targetIP}로 접속을 시도합니다...");
        }
        else
        {
            Debug.Log($"클라이언트 시작 실패!");
        }
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
    public void SetGameEnd(bool flag)
    {
        isGameEnd = flag;
    }

    /// <summary>
    /// 라운드 증가
    /// </summary>
    // public void IncreaseRound()
    // {
    //     currentRound++;
    // }

    public void ResetGameStatus()
    {
        isGameEnd = false;
        if (IsServer)
        {
            loserClientId.Value = 999;
        }
    }

    /// <summary>
    /// 서버 호스팅 시작
    /// </summary>
    public void StartServerHost()
    {
        if(serverRunning)
        {
            return;
        }

        localIP = GetLocalIPAddress();
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(localIP, 7777);
        if (NetworkManager.Singleton.StartHost())
        {
            print($"Host started on IP: {localIP}");
            serverRunning = true;
        }
        else
        {
            Debug.LogError("Host failed to start.");
        }
    }

    /// <summary>
    /// 서버 접속
    /// </summary>
    /// <param name="serverIP"></param>
    public void ConnectServer(string serverIP)
    {
        if(serverRunning)
        {
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(serverIP, 7777);
        if(NetworkManager.Singleton.StartClient())
        {
            print($"Connecting to {serverIP}:{7777}.");
            serverRunning = true;
        }
        else
        {
            Debug.LogError($"Failed to connect to {serverIP}:{7777}.");
        }
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

#if UNITY_EDITOR
    void Update()
    {
        // 에디터에서만 실행하는 키
        // F1: 서버로 시작
        // F2: 클라이언트로 시작
        InputNetworkKey();
    }

    void InputNetworkKey()
    {
        if (serverRunning)
        {
            return;
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            NetworkManager.Singleton.StartHost();
            serverRunning = true;
        }
        else if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            NetworkManager.Singleton.StartClient();
            serverRunning = true;
        }
    }
#endif

    // 현재 PC의 로컬 IP 얻기
    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}
