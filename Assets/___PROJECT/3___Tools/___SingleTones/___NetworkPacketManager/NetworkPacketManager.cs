using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///  NetCode Rpc를 관리하는 싱글톤 모듈 // 씬 전환 시 인스턴스 유지됨
/// </summary>
public class NetworkPacketManager : NetworkBehaviour
{
    public static NetworkPacketManager Inst { get; private set; }

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

    #region Scene Management (씬 관리)

    // [씬 전환] 서버가 모든 클라이언트를 특정 씬으로 이동시킴
    [Rpc(SendTo.Everyone)]
    public void TransitionToCardSelectRpc(string sceneName)
    {
        // Netcode for GameObjects에서는 서버가 SceneManager를 통해 씬을 로드하면
        // 연결된 모든 클라이언트가 자동으로 해당 씬을 함께 로드합니다.
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    #endregion

    #region Player Packets (플레이어 관련)

    // [데미지 요청] 클라이언트 -> 서버
    [Rpc(SendTo.Server)]
    public void RequestDamageServerRpc(NetworkObjectReference targetRef, int dmg)
    {
        if(targetRef.TryGet(out NetworkObject netObj))
        {
            // 서버에서 실제 체력 계산 후 결과 전파 (핵 방지를 위해 서버에서 처리)
            netObj.GetComponent<Player>().OnDamageCalculated(dmg);
        }
    }

    // 피격 연출을 모든 클라이언트에 전파
    [Rpc(SendTo.Everyone)]
    public void PlayDamageEffectRpc(NetworkObjectReference playerRef)
    {
        if (playerRef.TryGet(out NetworkObject netObj))
            netObj.GetComponent<Player>().ExecuteDamageEffect();
    }

    // [사망 효과] 서버 -> 모든 클라이언트
    [Rpc(SendTo.Everyone)]
    public void PerformDeathRpc(NetworkObjectReference playerRef, Color deathColor)
    {
        if(playerRef.TryGet(out NetworkObject netObj))
        {
            netObj.GetComponent<Player>().ExecuteDeathEffect(deathColor);
        }
    }
    #endregion

    #region Gun Packets (총기 관련)
    // [발사 요청] 클라이언트 -> 서버
    [Rpc(SendTo.Server)]
    public void RequestCreateFireEffectServerRpc(NetworkObjectReference playerRef, Vector2 pos, float rotation, bool isLeft)
    {
        // 서버 검증 후 전파
        FireEffectRpc(playerRef, pos, rotation, isLeft);
    }

    // [발사 연출] 서버 -> 모든 클라이언트
    [Rpc(SendTo.Everyone)]
    public void FireEffectRpc(NetworkObjectReference playerRef, Vector2 pos, float rotation, bool isLeft)
    {
        if(playerRef.TryGet(out NetworkObject netObj))
        {
            netObj.GetComponentInChildren<GunController>().ExecuteCreateFireEffects(pos, rotation, isLeft);
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestCreateBulletServerRpc(NetworkObjectReference playerRef, Vector2 pos, float rotation, float ammoSpeed, int dmg)
    {
        CreateBulletRpc(playerRef, pos, rotation, ammoSpeed, dmg);
    }

    [Rpc(SendTo.Everyone)]
    public void CreateBulletRpc(NetworkObjectReference playerRef, Vector2 pos, float rotation, float ammoSpeed, int dmg) { 
        if(playerRef.TryGet(out NetworkObject netObj))
        {
            netObj.GetComponentInChildren<GunController>().ExecuteCreateBullets(pos, rotation, ammoSpeed, dmg);
        }
    }
    #endregion
}