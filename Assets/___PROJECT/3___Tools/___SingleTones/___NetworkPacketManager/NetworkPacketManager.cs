using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///  NetCode Rpc를 관리하는 싱글톤 모듈 // 씬 전환 시 인스턴스 유지됨
/// </summary>
public class NetworkPacketManager : NetworkBehaviour
{
    public static NetworkPacketManager Inst { get; private set; }

    /// <summary>
    /// 네트워크 매니저가 씬을 전환중인지 확인하는 플래그
    /// </summary>
    public bool sceneSwitching{ get; private set; } = false;

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

    /// <summary>
    /// 네트워크 패킷 매니저 객체 및 인스턴스 삭제
    /// </summary>
    public void Destroy()
    {
        Inst = null;
        Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.Load || sceneEvent.SceneEventType == SceneEventType.Unload)
        {
            sceneSwitching = true;
        }
        else if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            sceneSwitching = false;
        }
    }

    private void HandleSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        GameManager.Inst.sceneLoadCompleted = true;
        GameManager.Inst.controllable = true;
        print("Scene load completed");
    }

    #region Scene Management (씬 관리)

    // [수정] 서버가 모든 클라이언트를 데리고 특정 씬으로 이동하게 만드는 RPC
    [Rpc(SendTo.Server)]
    public void RequestNextStageServerRpc(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"서버가 다음 씬으로 이동을 승인함: {sceneName}");

            // 모든 플레이어 일괄 체력 리셋
            if (IsServer)
            {
                ResetAllPlayerHp();
            }
            else
            {
                ResetAllPlayerHpServerRpc();
            }

            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("전달된 씬 이름이 비어있습니다!");
        }
    }

    /// <summary>
    /// 클라이언트(호스트X) -> 서버 모든 플레이어 체력 리셋 요청
    /// </summary>
    [Rpc(SendTo.Server)]
    private void ResetAllPlayerHpServerRpc()
    {
        ResetAllPlayerHp();
    }

    /// <summary>
    /// 접속해 있는 모든 클라이언트의 Player HP를 최대값으로 초기화한다.
    /// </summary>
    private void ResetAllPlayerHp()
    {
        // 플레이어들을 일괄 체력 리셋
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (netObj.TryGetComponent<Player>(out var player))
                {
                    player.netCurrHP.Value = player.totalHP;
                }
            }
        }
    }

    // [씬 전환] 서버가 모든 클라이언트를 특정 씬으로 이동시킴
    [Rpc(SendTo.Everyone)]
    public void TransitionToCardSelectRpc(string sceneName)
    {
        if (sceneSwitching)
        {
            return;
        }
        // Netcode for GameObjects에서는 서버가 SceneManager를 통해 씬을 로드하면
        // 연결된 모든 클라이언트가 자동으로 해당 씬을 함께 로드합니다.
        if (IsServer)
        {
            // 서버 공유 라운드 값 증가
            GameManager.Inst.currentRound.Value++;
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    #endregion

    #region Player Packets (플레이어 관련)

    // [데미지 요청] 클라이언트 -> 서버
    [Rpc(SendTo.Server)]
    public void RequestDamageServerRpc(NetworkObjectReference targetRef, int dmg)
    {
        if (sceneSwitching)
        {
            return;
        }
        if (targetRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.IsSpawned)
            {
                // 서버에서 실제 체력 계산 후 결과 전파 (핵 방지를 위해 서버에서 처리)
                netObj.GetComponent<Player>().OnDamageCalculated(dmg);
            }
        }
    }

    // 피격 연출을 모든 클라이언트에 전파
    [Rpc(SendTo.Everyone)]
    public void PlayDamageEffectRpc(NetworkObjectReference playerRef)
    {
        if (sceneSwitching)
        {
            return;
        }
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.IsSpawned)
            {
                netObj.GetComponent<Player>().ExecuteDamageEffect();
            }
        }
    }

    // [사망 효과] 서버 -> 모든 클라이언트
    [Rpc(SendTo.Everyone)]
    public void PerformDeathRpc(NetworkObjectReference playerRef, Color deathColor)
    {
        if (sceneSwitching)
        {
            return;
        }
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            // 플레이어 각자 라운드를 1씩 올리도록 한다
            netObj.GetComponent<Player>().ExecuteDeathEffect(deathColor);

            if (IsServer)
            {
                GameManager.Inst.loserClientId.Value = netObj.OwnerClientId;
                GameManager.Inst.SetGameEnd(true);
            }
           
            if (IsServer)
            {
                // 마지막 라운드가 끝났다면 결과 씬으로 바로 이동
                if (GameManager.Inst.currentRound.Value == GameManager.Inst.maxRound)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("ResultScene", LoadSceneMode.Single);
                }
                else
                {
                    TransitionToCardSelectRpc("CardSelectScene");
                }
            }
        }
    }
    #endregion

    #region Gun Packets (총기 관련)
    // [발사 요청] 클라이언트 -> 서버
    [Rpc(SendTo.Server)]
    public void RequestCreateFireEffectServerRpc(NetworkObjectReference playerRef, Vector2 firePos, Vector2 shellPos, float rotation, bool isLeft)
    {
        if (sceneSwitching)
        {
            return;
        }
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.IsSpawned) {
                FireEffectRpc(playerRef, firePos, shellPos, rotation, isLeft);
            }
        }
    }

    // [발사 연출] 서버 -> 모든 클라이언트
    [Rpc(SendTo.Everyone)]
    public void FireEffectRpc(NetworkObjectReference playerRef, Vector2 firePos, Vector2 shellPos, float rotation, bool isLeft)
    {
        if (sceneSwitching)
        {
            return;
        }
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.IsSpawned)
            {
                netObj.GetComponentInChildren<GunController>().ExecuteCreateFireEffects(firePos, shellPos, rotation, isLeft);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestCreateBulletServerRpc(NetworkObjectReference playerRef, Vector2 firePos, float rotation, float ammoSpeed, int dmg)
    {
        if (sceneSwitching)
        {
            return;
        }
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.IsSpawned) {
                CreateBulletRpc(playerRef, firePos, rotation, ammoSpeed, dmg);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void CreateBulletRpc(NetworkObjectReference playerRef, Vector2 firePos, float rotation, float ammoSpeed, int dmg) 
    {
        if (sceneSwitching)
        {
            return;
        }
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.IsSpawned)
            {
                netObj.GetComponentInChildren<GunController>().ExecuteCreateBullets(firePos, rotation, ammoSpeed, dmg);
            }
        }
    }
    #endregion

}