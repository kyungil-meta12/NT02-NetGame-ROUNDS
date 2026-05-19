using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 각 플레이어가 가지는 스탯 데이터
/// INetworkSerializble을 상속받아 RPC로 전송할 수 있게 만듭니다.
/// </summary>
[Serializable]
public struct StatData
{
    // 예시
    // 추가 이동 속도, 추가 연사 딜레이 감소 등...
    // 이 값들은 카드 선택 시 변경할 수 있음 // 각 능력치 마다 메서드를 추가하여 변경하는 코드를 작성
    
    //플레이어 스탯 변수
    public int moveSpeedLevel;         //이동속도
    public int jumpLevel;               //점프 횟수
    //총 스탯 변수
    [Space(10)]
    public GunType gunType;                 //총 타입
    public bool isMultiShot;                //멀티샷용 bool변수
    public int multiShotSpreadLevel;   //멀티샷 퍼짐 정도
    public int multiShellCountLevel;         //멀티샷 개수
    public int totalAmmoLevel;               //탄알집 용량
    public int ammoSpeedLevel;         //탄환 속도
    public int damageLevel;                  //탄환 데미지
    public int fireSpeedLevel;      //연사 속도
}

/// <summary>
/// 플레이어의 외모 스프라이트 인덱스
/// </summary>
public struct AppearanceData
{
    public int bodyIndex;
    public int faceIndex;
}

/// <summary>
/// 플레이어 관련 데이터를 관리하는 싱글톤 모듈 // 씬 전환 시 인스턴스 유지됨
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Inst;

    [Header("테스트 모드 사용 여부")]
    public bool testMode;

    [Header("테스트 모드 값(런타임에 변경 불가)")]
    public StatData TestStat;

    /// <summary>
    /// 나의 스탯
    /// </summary>
    [HideInInspector]
    public StatData Stat;
    public AppearanceData Appearance = new();
    private bool appearanceCreated = false;

    void Awake()
    {
        if(Inst && Inst != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        Inst = this;
        DontDestroyOnLoad(gameObject);

        // Awake에서 Player의 OnNetworkSpawn보다 확실하게 먼저 초기화되도록 한다
        if(!testMode) 
        {
            Stat = new StatData
            {
                //플레이어 스탯
                moveSpeedLevel = 0,
                jumpLevel = 0,

                //총 스탯
                gunType = GunType.Pistol,
                multiShotSpreadLevel = 0,
                multiShellCountLevel = 0,
                isMultiShot = false,
                totalAmmoLevel = 0,
                ammoSpeedLevel = 0,
                fireSpeedLevel = 0,
                damageLevel = 0,
            };
        }
        else // 테스트 모드일 경우 테스트 값을 사용
        {
            Stat = TestStat;
        }
    }

    /// <summary>
    /// 플레이어 매니저 객체 및 인스턴스 삭제
    /// </summary>
    public void Destroy()
    {
        Inst = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// 플레이어의 외모 데이터를 저장한다.
    /// 초기에 한 번만 설정되고, 이후의 호출은 무시된다.
    /// </summary>
    /// <param name="bodyIndex"></param>
    /// <param name="faceIndex"></param>
    public void SaveAppearance(int bodyIndex, int faceIndex, Sprite bodySprite)
    {
        if(appearanceCreated)
        {
            return;
        }
        Appearance.bodyIndex = bodyIndex;
        Appearance.faceIndex = faceIndex;
        appearanceCreated = true;
    }

    //이동 속도 20퍼센트 증가
    public void IncreaseMoveSpeed()
    {
        Stat.moveSpeedLevel++;
    }

    //점프 카운트 1 증가
    public void IncreaseJumpCount()
    {
        Stat.jumpLevel++;
    }


    //샷건으로 교체
    public void ReplaceTheGunToShotgun()
    {
        Stat.gunType = GunType.Shotgun;
    }

    //AR로 교체
    public void ReplaceTheGunToAR()
    {
        Stat.gunType = GunType.AR;
    }

    //SMG로 교체
    public void ReplaceTheGunToSMG()
    {
        Stat.gunType = GunType.Smg;
    }

    //스나이퍼로 교체
    public void ReplaceTheGunToSniper()
    {
        Stat.gunType = GunType.Sniper;
    }


    // 멀티샷 카운트 1 증가, 멀티샷 퍼짐 50% 증가
    public void IncreaseMultiShellCount()
    {
        Stat.multiShellCountLevel++;
        Stat.multiShotSpreadLevel++;
        Stat.isMultiShot = true;
    }

    // 탄알집 용량 50% 증가
    // 실제 용량은 GunController에서 계산
    public void IncreaseTotalAmmo()
    {
        Stat.totalAmmoLevel++;
    }

    // 탄환 속도 50% 증가
    // 실제 탄환 속도는 GunController에서 계산
    public void IncreaseAmmoSpeed()
    {
        Stat.ammoSpeedLevel++;
    }

    // 대미지 25% 증가
    // 실제 대미지는 GunController에서 계산
    public void IncreaseDamage()
    {
        Stat.damageLevel++;
    }

    // 연사 속도 25% 중가
    // 실제 연사속도는 GunController에서 계산
    public void DecreaseFireInterval()
    {
        Stat.fireSpeedLevel++;
    }
}