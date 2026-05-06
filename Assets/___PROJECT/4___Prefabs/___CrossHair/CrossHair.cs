using Unity.VisualScripting;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
    public float scale;
    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        rt.position = MouseManager.Inst.screenPos;
        rt.localScale = Vector2.one * scale;
    }
}
