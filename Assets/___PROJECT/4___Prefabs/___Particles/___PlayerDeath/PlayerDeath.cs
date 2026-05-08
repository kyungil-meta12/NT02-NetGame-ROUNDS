using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    public GameObject[] particlePrefabs;
    public Color createColor;

    // 파티클들의 인스턴스를 생성하는 역할만 하고 삭제된다
    void Start()
    {
        foreach(var p in particlePrefabs)
        {
            var main = Instantiate(p, transform.position, Quaternion.identity).GetComponent<ParticleSystem>().main;
            main.startColor = createColor;
        }
        Destroy(gameObject);
    }
}
