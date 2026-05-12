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

    // [외형 동기화] 서버가 결정한 외형 정보를 모든 클라이언트에 전파
    [Rpc(SendTo.Everyone)]
    public void SyncPlayerAppearanceRpc(NetworkObjectReference playerRef, int faceIdx, int colorIdx)
    {
        if(playerRef.TryGet(out NetworkObject netObj))
        {
            var player = netObj.GetComponent<Player>();
            if(player != null)
            {
                player.ApplyAppearance(faceIdx, colorIdx);
            }
        }
    }


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
    public void RequestFireServerRpc(NetworkObjectReference playerRef, float rotation, Vector2 pos, bool isLeft)
    {
        // 서버 검증 후 전파
        FireEffectsRpc(playerRef, rotation, pos, isLeft);
    }

    // [발사 연출] 서버 -> 모든 클라이언트
    [Rpc(SendTo.Everyone)]
    public void FireEffectsRpc(NetworkObjectReference playerRef, float rotation, Vector2 pos, bool isLeft)
    {
        if(playerRef.TryGet(out NetworkObject netObj))
        {
            netObj.GetComponentInChildren<GunController>().ExecuteFireEffects(rotation, pos, isLeft);
        }
    }
    #endregion
}