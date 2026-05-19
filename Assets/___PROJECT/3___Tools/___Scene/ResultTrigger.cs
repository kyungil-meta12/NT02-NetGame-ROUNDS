using TMPro;
using UnityEngine;

public class ResultTrigger : MonoBehaviour
{
    public GameObject victoryUI;
    public GameObject defeatUI;
    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI loserNameText;

    void Start()
    {
        // 이미 생성되어 있는 GameManager 인스턴스에 명령을 내립니다.
        if (GameManager.Inst != null)
        {
            GameManager.Inst.SetupResultUI(victoryUI, defeatUI, winnerNameText, loserNameText);
        }
    }
}
