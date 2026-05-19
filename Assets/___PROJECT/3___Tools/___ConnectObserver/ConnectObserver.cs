using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectObserver : MonoBehaviour
{
    public Button startButton;

    // 버튼은 기본적으로 비활성화
    void Start()
    {
        startButton.gameObject.SetActive(false);
    }

    // 플레이어가 접속했을 때만 버튼을 활성화 한다
    // 호스트에서만 동작한다
    void Update()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                bool canStart = NetworkManager.Singleton.ConnectedClientsList.Count >= 2;
                startButton.gameObject.SetActive(canStart);
            }
        }
    }
}
