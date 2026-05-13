using System;
using UnityEngine;

/// <summary>
/// 각 플레이어가 가지는 스탯 데이터
/// </summary>
public struct StatData
{
    // 예시
    // 추가 이동 속도, 추가 연사 딜레이 감소 등...
    // 이 값들은 카드 선택 시 변경할 수 있음 // 각 능력치 마다 메서드를 추가하여 변경하는 코드를 작성
    
    //플레이어 스탯 변수
    public float moveSpeedMultiply;         //이동속도
    public int jumpCountPlus;               //점프 횟수
    //총 스탯 변수
    public GunType gunType;                 //총 타입
    public float multiShotSpreadMultiply;   //멀티샷 퍼짐 정도
    public int multiShellCountPlus;         //멀티샷 개수
    public bool isMultiShot;                //멀티샷용 bool변수
    public int totalAmmoPlus;               //탄알집 용량
    public float ammoSpeedMultiply;         //탄환 속도
    public int damagePlus;                  //탄환 데미지
    public float fireIntervalMultiply;      //연사 속도
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
    private bool appearanceCreated;

    /// <summary>
    /// 나의 스탯
    /// </summary>
    public StatData Stat;
    public AppearanceData Appearance;

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

    private void Start()
    {
        Stat = new StatData
        {
            //플레이어 스탯
            moveSpeedMultiply = 1,
            jumpCountPlus = 0,
            //총 스탯
            gunType = GunType.Pistol,
            multiShotSpreadMultiply = 0,
            multiShellCountPlus = 0,
            isMultiShot = false,
            totalAmmoPlus = 0,
            ammoSpeedMultiply = 1,
            damagePlus = 0,
            fireIntervalMultiply = 1,
        };
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
    public void SaveAppearance(int bodyIndex, int faceIndex)
    {
        if(appearanceCreated)
        {
            return;
        }
        Appearance.bodyIndex = bodyIndex;
        Appearance.faceIndex = faceIndex;
        appearanceCreated = true;
    }

    //이동 속도 0.2 퍼센트 증가
    public void IncreaseMoveSpeed()
    {
        Stat.moveSpeedMultiply += 0.2f;
    }
    //점프 카운트 1 증가
    public void IncreaseJumpCount()
    {
        Stat.jumpCountPlus++;
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
    //멀티샷 카운트 1 증가
    public void IncreaseMultiShellCount()
    {
        Stat.multiShellCountPlus++;
        Stat.multiShotSpreadMultiply += 15f;
        Stat.isMultiShot = true;
    }
    //탄알집 용량 10 증가
    public void IncreaseTotalAmmo()
    {
        Stat.totalAmmoPlus += 10;
    }
    //탄환속도 0.2 퍼센트 증가
    public void IncreaseAmmoSpeed()
    {
        Stat.ammoSpeedMultiply += 0.2f;
    }

    public void IncreaseDamage()
    {
        Stat.damagePlus += 10;
    }

    public void DecreaseFireInterval()
    {
        if (Stat.fireIntervalMultiply > 0.2f)
        {
            Stat.fireIntervalMultiply -= 0.2f;
        }
    }
}