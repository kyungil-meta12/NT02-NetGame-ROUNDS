using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    // Round1 씬 로드
    public void OnStartClick()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Round1", LoadSceneMode.Single);
    }
}
