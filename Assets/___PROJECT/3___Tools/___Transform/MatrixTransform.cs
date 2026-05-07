using UnityEngine;

public static class MatrixTransform
{
    public static void Identity(ref Matrix4x4 T)
    {
        T = Matrix4x4.identity;
    }

    public static void Translate(ref Matrix4x4 T, Vector2 val)
    {
        T *= Matrix4x4.Translate((Vector3)val);
    }

    public static void Rotate(ref Matrix4x4 T, float val)
    {
        T *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, val));
    }

    public static void Rotate(ref Matrix4x4 T, Vector3 val)
    {
        T *= Matrix4x4.Rotate(Quaternion.Euler(val.x, val.y, val.z));
    }

    public static void Scale(ref Matrix4x4 T, Vector2 val)
    {
        T *= Matrix4x4.Scale((Vector3)val);
    }

    public static void Dispatch(Transform target, ref Matrix4x4 T)
    {
        target.position = T.GetColumn(3);
        target.rotation = T.rotation; 
        target.localScale = new Vector3(
            T.GetColumn(0).magnitude,
            T.GetColumn(1).magnitude,
            T.GetColumn(2).magnitude
        );
    }

    public static Vector2 WorldPos(ref Matrix4x4 T)
    {
        return T.GetColumn(3);
    }
}