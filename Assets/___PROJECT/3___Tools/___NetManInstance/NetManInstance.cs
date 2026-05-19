using UnityEngine;

// 네트워크 매니저가 중복 생성되지 않도록 하는 모듈
public class NetManInstance : MonoBehaviour
{
    public static NetManInstance Inst;

    void Awake()
    {
        if(Inst && Inst != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Inst = this;
    }

    public void Destroy()
    {
        Inst = null;
    }
}
