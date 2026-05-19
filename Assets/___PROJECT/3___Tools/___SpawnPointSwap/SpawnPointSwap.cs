using UnityEngine;


// 스폰포인트를 서로 바꾼다.
public class SpwanPointSwap : MonoBehaviour
{
    public Transform otherPoint;

    void Awake()
    {
        int rand = Random.Range(0, 2);
        if(rand == 1) // 바꾸거나 안바꾸거나 둘 중 하나
        {
            var temp = otherPoint.position;
            otherPoint.position = transform.position;
            transform.position = temp;
        }
    }
}
