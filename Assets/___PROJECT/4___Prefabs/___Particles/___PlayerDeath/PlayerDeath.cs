using System.Collections.Generic;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    public GameObject[] particlePrefabs;

    // 파티클들의 인스턴스를 생성하는 역할만 하고 삭제된다
    void Start()
    {
        foreach(var p in particlePrefabs)
        {
            Instantiate(p, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
