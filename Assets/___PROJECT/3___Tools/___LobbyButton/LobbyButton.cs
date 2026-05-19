using Unity.Netcode;
using UnityEngine;

public class LobbyButton : MonoBehaviour
{
    // 모든 상태를 완전히 초기화한다.
    public void OnLobbyButtonClick()
    {
        NetworkManager.Singleton.Shutdown();
        NetworkPacketManager.Inst?.Destroy();
        GameManager.Inst?.Destroy();
        PlayerManager.Inst.Destroy();
    }
}
