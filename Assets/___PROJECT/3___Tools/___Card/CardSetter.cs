using System;
using System.Linq;
using TMPro;
using UltimateClean;
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
        //다음 게임 씬으로 연결
        sceneRelay.SetNextScene(GameManager.Inst.currentRound + 1);
        sceneTransition.PerformTransition();
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