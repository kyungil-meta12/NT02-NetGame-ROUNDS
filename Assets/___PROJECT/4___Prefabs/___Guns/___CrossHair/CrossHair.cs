using Unity.VisualScripting;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
    public Transform origin;
    public float lineWeight;
    public float startOffset;
    public float endOffset;
    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    void Start()
    {
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.startWidth = lineWeight;
        line.endWidth = lineWeight;
        line.positionCount = 2;
    }

    void Update()
    {
        var mousePos = MouseManager.Inst.worldPos;
        var direction = Vector2.ClampMagnitude((Vector2)origin.position - mousePos, 1f);
        transform.position = mousePos;
        line.SetPosition(0, (Vector2)origin.position + direction * startOffset);
        line.SetPosition(1, mousePos + direction * endOffset);
    }
}
