using System;
using System.Collections;
using System.Linq;
using TMPro;
using UltimateClean;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

//카드셀렉트씬 카드 배치용 클래스
public class CardSetter : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject cardUIPanel;
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

        if (sceneRelay != null)
        {
            sceneTransition = sceneRelay.GetComponent<SceneTransition>();
        }
    }

    /// <summary>
    /// 카드 배열에서 4개를 선택해서 랜덤배치
    /// </summary>
    public IEnumerator Start()
    {
        Debug.Log("카드 세터 시작됨");

        Debug.Log($"대기 시작... 현재 패배자 ID: {GameManager.Inst.loserClientId.Value}");
        // 패배자 ID 동기화 대기
        while (GameManager.Inst.loserClientId.Value == 999)
        {
            yield return null; // 값이 바뀔 때까지 프레임 대기
        }

        Debug.Log($"대기 종료! 확정된 패배자 ID: {GameManager.Inst.loserClientId.Value}");

        bool isLoser = (GameManager.Inst.loserClientId.Value == NetworkManager.Singleton.LocalClientId);

        // 권한에 따른 화면 구성
        if (isLoser)
        {
            // [패배자] 카드 선택 UI를 활성화
            //if (cardUIPanel != null) cardUIPanel.SetActive(true);
            if (selectedNumberText != null) selectedNumberText.text = "카드를 선택하세요!";
            InitCards(); // 카드 랜덤 배치 함수 (기존 로직)
        }
        else
        { // [승리자] 카드 선택 UI를 비활성화
            if (cardUIPanel != null)
            {
                cardUIPanel.SetActive(false);
            }
            // [승리자] "상대가 카드를 선택 중입니다" 문구 표시
            if (selectedNumberText != null)
                selectedNumberText.text = "상대가 카드를 선택 중입니다...";
        }
    }

    private void InitCards()
    {
        if (cards == null || cards.Length < 4)
        {
            Debug.LogError("Card Info 데이터가 부족합니다!");
            return;
        }

        // 랜덤 인덱스 추출
        randomIndices = Enumerable.Range(0, cards.Length)
            .OrderBy(x => Random.value)
            .Take(4)
            .ToArray();

        for (int i = 0; i < randomIndices.Length; i++)
        {
            int dataIdx = randomIndices[i];

            // 데이터 할당
            cardUI[i].image.sprite = cards[dataIdx].image;
            cardUI[i].titleText.text = cards[dataIdx].titleText;
            cardUI[i].descriptionText.text = cards[dataIdx].descriptionText;

            // [확인] 개별 카드 오브젝트가 꺼져 있을 수 있으므로 명시적으로 켬
            // cardUI[i].image가 속한 게임 오브젝트 자체를 활성화
            cardUI[i].image.gameObject.SetActive(true);
            // 만약 카드 전체를 담은 부모가 따로 있다면 그 부모를 켜야 합니다.
            cardUI[i].image.transform.parent.gameObject.SetActive(true);
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
        if (selectedIndex == -1) return;

        // 1. 카드 효과 적용 (로컬에서 즉시 적용)
        ApplyEffect(randomIndices[selectedIndex]);

        // 2. 다음으로 이동할 씬 이름 결정 (SceneRelay에 설정된 이름 가져오기)
        string nextSceneName = sceneRelay.GetComponent<UltimateClean.SceneTransition>().scene;

        // [중요] 서버와 클라이언트 구분 처리
        if (NetworkManager.Singleton.IsServer)
        {
            // 내가 호스트라면 직접 로드
            Debug.Log($"호스트가 다음 스테이지({nextSceneName})를 직접 로드합니다.");
            NetworkManager.Singleton.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
        else
        {
            // 내가 클라이언트(패배자)라면 서버에게 씬 전환을 요청 (RPC 호출)
            Debug.Log($"클라이언트가 서버에게 {nextSceneName} 로드를 요청합니다.");
            NetworkPacketManager.Inst.RequestNextStageServerRpc(nextSceneName);
        }
    }

    // 효과 적용 부분만 따로 관리
    private void ApplyEffect(int cardIndex)
    {
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
}