using System;
using UltimateClean;
using UnityEngine;

public class SceneRelay : MonoBehaviour
{
    public string[] sceneNames;
    
    private SceneTransition transition;

    private void Awake()
    {
        transition = GetComponent<SceneTransition>();
    }

    private void Start()
    {
        SetNextScene(GameManager.Inst.currentRound);
    }

    //현재 라운드의 인덱스로 다음 라운드 씬 연결
    public void SetNextScene(int index)
    {
        transition.scene = sceneNames[index];
    }
}
