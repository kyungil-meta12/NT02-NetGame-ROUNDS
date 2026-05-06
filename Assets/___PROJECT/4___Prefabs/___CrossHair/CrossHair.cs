using Unity.VisualScripting;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        rt.position = MouseManager.Inst.screenPos;
    }
}
