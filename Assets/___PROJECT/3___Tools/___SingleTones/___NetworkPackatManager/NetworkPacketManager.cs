using Unity.Netcode;
using UnityEngine;

public class NetworkPacketManager : NetworkBehaviour
{
    // [싱글톤] 어디서든 NetworkPacketManager.Inst 로 접근 가능
    public static NetworkPacketManager Inst { get; private set; }

    void Awake()
    {
        // 싱글톤 중복 생성 방지 로직
        if (Inst != null && Inst != this)
        {
            Destroy(gameObject);
            return;
        }
        Inst = this;
        DontDestroyOnLoad(gameObject);
    }

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