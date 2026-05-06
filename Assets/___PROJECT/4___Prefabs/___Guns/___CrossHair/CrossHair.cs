using Unity.VisualScripting;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
    public float lineWeight;
    public float startOffset;
    public float endOffset;

    private Vector2 origin;
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
        var direction = Vector2.ClampMagnitude(origin - mousePos, 1f);
        transform.position = mousePos;
        line.SetPosition(0, origin + direction * startOffset);
        line.SetPosition(1, mousePos + direction * endOffset);
    }

    public void InputOriginPoint(Vector2 point)
    {
        origin = point;
    }
}
