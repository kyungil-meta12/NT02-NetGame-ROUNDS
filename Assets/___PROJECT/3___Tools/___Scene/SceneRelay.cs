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

    //현재 라운드의 인덱스로 다음 라운드 씬 연결
    //todo 게임 매니저를 통해 라운드 관리 및 메서드 호출 필요
    public void SetNextScene(int index)
    {
        transition.scene = sceneNames[index];
    }
}
