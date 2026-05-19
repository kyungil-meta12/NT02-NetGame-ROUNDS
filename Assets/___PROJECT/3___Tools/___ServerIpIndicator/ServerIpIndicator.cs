using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ServerIpIndicator : MonoBehaviour
{
    public TextMeshProUGUI text;

    // 현재 접속한 서버 주소를 보여준다.
    void Start()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            {
                string ipAddress = "서버IP: " + unityTransport.ConnectionData.Address;
                text.text = ipAddress;
            }
        }
    }
}