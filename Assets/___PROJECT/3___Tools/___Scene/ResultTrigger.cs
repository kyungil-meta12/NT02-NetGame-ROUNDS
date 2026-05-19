using TMPro;
using System.Collections;
using UnityEngine;

public class ResultTrigger : MonoBehaviour
{
    public GameObject victoryUI;
    public GameObject defeatUI;
    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI loserNameText;

    void Start()
    {
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        yield return new WaitForSeconds(0.2f);

        if (GameManager.Inst != null)
        {
            Debug.Log($"ResultTrigger : 결과 화면 UI 설정을 시작합니다.");
            GameManager.Inst.SetupResultUI(victoryUI, defeatUI, winnerNameText, loserNameText);
        }
        else
        {
            Debug.Log($"ResultTrigger : GameManager 인스턴스를 찾을 수 없습니다!");
        }
    }
}
