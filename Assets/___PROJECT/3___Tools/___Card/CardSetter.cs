using System;
using System.Linq;
using TMPro;
using UltimateClean;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

//카드셀렉트씬 카드 배치용 클래스
public class CardSetter : MonoBehaviour
{
    public TextMeshProUGUI selectedNumberText;
    public SceneRelay sceneRelay;
    private SceneTransition sceneTransition;
    
    [Tooltip("4장의 카드 UI 세팅 필요")]
    public CardUI[] cardUI;
    
    [Tooltip("최소 4장 이상의 카드 정보 세팅 필요")]
    public CardInfo[] cards;

    private int[] randomIndices;
    private int selectedIndex = -1;

    private void Awake()
    {
        randomIndices = new int[cardUI.Length];
    }

    /// <summary>
    /// 카드 배열에서 4개를 선택해서 랜덤배치
    /// </summary>
    public void Start()
    {
        sceneTransition = sceneRelay.GetComponent<SceneTransition>();
        
        if (cards == null || cards.Length < 4) return;
        
        randomIndices = Enumerable.Range(0, cards.Length)
            .OrderBy(x => Random.value)
            .Take(4)
            .ToArray();
        
        for (int i = 0; i < randomIndices.Length; i++)
        {
            cardUI[i].image.sprite = cards[randomIndices[i]].image;
            cardUI[i].titleText.text = cards[randomIndices[i]].titleText;
            cardUI[i].descriptionText.text = cards[randomIndices[i]].descriptionText;
        }
    }

    //카드 선택
    public void SelectCard(int index)
    {
        selectedIndex = index;
        selectedNumberText.text = $"{index + 1} 번째 카드";
    }

    //카드 확정
    public void ConfirmCard()
    {
        //카드 선택
        if(selectedIndex == -1) return; // 카드 미선택 방지
        Debug.Log($"선택된 카드는 {cards[randomIndices[selectedIndex]].titleText}");
        //카드별 능력 적용
        switch (randomIndices[selectedIndex])
        {
            case 0:
                PlayerManager.Inst.IncreaseJumpCount();
                break;
            case 1:
                PlayerManager.Inst.IncreaseTotalAmmo();
                break;
            case 2:
                PlayerManager.Inst.IncreaseMoveSpeed();
                break;
            case 3:
                PlayerManager.Inst.IncreaseDamage();
                break;
            case 4:
                PlayerManager.Inst.IncreaseAmmoSpeed();
                break;
            case 5:
                PlayerManager.Inst.DecreaseFireInterval();
                break;
            case 6:
                PlayerManager.Inst.IncreaseMultiShellCount();
                break;
            case 7:
                PlayerManager.Inst.ReplaceTheGunToShotgun();
                break;
            case 8:
                PlayerManager.Inst.ReplaceTheGunToAR();
                break;
            case 9:
                PlayerManager.Inst.ReplaceTheGunToSMG();
                break;
            case 10:
                PlayerManager.Inst.ReplaceTheGunToSniper();
                break;
        }

        // 다음 게임 씬 정보 가져오기
        // GameManager의 currentRound를 증가시키고 SceneRelay에서 다음 씬 이름을 가져옴
        int nextRound = GameManager.Inst.currentRound + 1;

        // SceneRelay에서 다음 씬 이름을 미리 세팅하거나 가져옴.
        if(nextRound < sceneRelay.sceneNames.Length)
        {
            string nextSceneName = sceneRelay.sceneNames[nextRound];

            // [수정] 내가 서버(Host)라면 즉시 실행, 클라이언트라면 서버에 요청하는 로직이 필요하지만
            // 현재 NetworkPacketManager에 ServerRpc가 없으므로 
            // 일단 서버(Host)인 플레이어만 씬 전환 버튼을 누를 수 있도록 하거나, 
            // NetworkPacketManager에 서버 전용 씬 전환 RPC를 추가해야 함.

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkPacketManager.Inst.TransitionToCardSelectRpc(nextSceneName);
                GameManager.Inst.currentRound = nextRound;
            }
        }
        else
        {
            Debug.Log($"다음 스테이지가 sceneName 배열에 없습니다!");
        }
    }
}

//카드 UI
[Serializable]
public class CardUI
{
    public Image image;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
}

//카드 정보
[Serializable]
public class CardInfo
{
    public Sprite image;
    public string titleText;
    public string descriptionText;
}