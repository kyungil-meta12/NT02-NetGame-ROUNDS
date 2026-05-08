using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

//카드셀렉트씬 카드 배치용 클래스
public class CardSetter : MonoBehaviour
{
    [Tooltip("4장의 카드 UI 세팅 필요")]
    public CardUI[] cardUI;
    
    [Tooltip("최소 4장 이상의 카드 정보 세팅 필요")]
    public CardInfo[] cards;
    
    /// <summary>
    /// 카드 배열에서 4개를 선택해서 랜덤배치
    /// </summary>
    public void Start()
    {
        if (cards == null || cards.Length < 4) return;
        
        int[] selectedIndices = Enumerable.Range(0, cards.Length)
            .OrderBy(x => Random.value)
            .Take(4)
            .ToArray();
        
        for (int i = 0; i < selectedIndices.Length; i++)
        {
            cardUI[i].image.sprite = cards[selectedIndices[i]].image;
            cardUI[i].titleText.text = cards[selectedIndices[i]].titleText;
            cardUI[i].descriptionText.text = cards[selectedIndices[i]].descriptionText;
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