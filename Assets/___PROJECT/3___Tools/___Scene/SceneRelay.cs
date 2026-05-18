using System;
using UltimateClean;
using UnityEngine;

public class SceneRelay : MonoBehaviour
{
    public string[] sceneNames;
    
    //private SceneTransition transition;

    // private void Awake()
    // {
    //     transition = GetComponent<SceneTransition>();
    // }

    // private void Start()
    // {
    //     // GameManager.currentRound 2라면, 배열의 1번 인덱스(Stage2)를 가리켜야 함.
    //     int targetIdx = GameManager.Inst.currentRound - 1;
    //     SetNextScene(targetIdx);
    // }

    // //현재 라운드의 인덱스로 다음 라운드 씬 연결
    // public void SetNextScene(int index)
    // {
    //     if (index >= 0 && index < sceneNames.Length)
    //     {
    //         transition.scene = sceneNames[index];
    //     }
    //     else
    //     {
    //         // 예외 상황 시 결과창으로 보냄.
    //         transition.scene = "ResultScene";
    //     }
    // }
}
