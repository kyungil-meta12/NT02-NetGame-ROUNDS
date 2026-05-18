using UltimateClean;
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

    #region Scene Management (씬 관리)

    // [추가] 클라이언트(패배자)가 서버에게 다음 스테이지 시작을 요청
    [Rpc(SendTo.Server)]
    public void RequestNextStageServerRpc(string sceneName)
    {
        // 현재 라운드가 마지막 라운드(3)라면 카드 선택 없이 바로 결과창으로!
        if (GameManager.Inst.currentRound >= GameManager.Inst.maxRound)
        {
            Debug.Log($"마지막 스테이지 종료. 결과창으로 즉시 이동합니다.");
            NetworkManager.Singleton.SceneManager.LoadScene("ResultScene", LoadSceneMode.Single);
        }
        else
        {
            // 아직 스테이지가 남았다면 라운드를 올리고 카드 선택 씬으로 이동.
            GameManager.Inst.currentRound++;
            TransitionToCardSelectRpc("CardSelectScene");
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
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            sceneSwitching = true;
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
            if (IsServer)
            {
                GameManager.Inst.loserClientId.Value = netObj.OwnerClientId;
                GameManager.Inst.SetGameEnd(true);
            }

            netObj.GetComponent<Player>().ExecuteDeathEffect(deathColor);

            if (IsServer)
            {
                TransitionToCardSelectRpc("CardSelectScene");
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