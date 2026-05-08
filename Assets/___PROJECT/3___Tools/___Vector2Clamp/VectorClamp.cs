using UnityEngine;

// Vector3 및 Vector2 타입에 대해 clamp를 하는 모듈.
// Mathf에 해당 타입에 대한 오버로드가 없어 별도로 작성하였다.

// 값을 clamp하는 방식 옵션
// Block: 범위 한계에 도달하면 그대로 clamp 된 채로 현재 값을 유지한다.
// Return: 범위 한계에 도달하면 현재 값을 반대 방향의 한계 값으로 변경한다.
// MonoLimit에서는 사용하지 않는다.
public enum ClampType
{
    Block,
    Return
}

// 값을 clamp하는 방향 옵션
// Min: 현재 값이 특정 값 미만으로 작아질 때만 clamp를 실행한다.
// Max: 현재 값이 특정 값 초과로 커질 때만 clamp를 실행한다.
// BiLimit에서는 사용하지 않는다.
public enum ClampDir
{
    Min,
    Max
}

public static class VectorClamp
{
    // 양방향 clamp
    public static Vector3 BiLimit(Vector3 val, Vector3 inclusiveMin, Vector3 inclusiveMax, ClampType clampType=ClampType.Block)
    {
        Vector3 retval = val;
        if (clampType == ClampType.Block)
        {
            retval.x = Mathf.Clamp(retval.x, inclusiveMin.x, inclusiveMax.x);
            retval.y = Mathf.Clamp(retval.y, inclusiveMin.y, inclusiveMax.y);
            retval.z = Mathf.Clamp(retval.z, inclusiveMin.z, inclusiveMax.z);
        }
        else if (clampType == ClampType.Return)
        {
            if(retval.x < inclusiveMin.x)
            {
                retval.x = inclusiveMax.x;
            }
            else if(retval.x > inclusiveMax.x)
            {
                retval.x = inclusiveMin.x;
            }

            if(retval.y < inclusiveMin.y)
            {
                retval.y = inclusiveMax.y;
            }
            else if(retval.y > inclusiveMax.y)
            {
                retval.y = inclusiveMin.y;
            }

            if(retval.z < inclusiveMin.z)
            {
                retval.z = inclusiveMax.z;
            }
            else if(retval.z > inclusiveMax.z)
            {
                retval.z = inclusiveMin.z;
            }
        }

        return retval;
    }

     // 양방향 clamp
    public static Vector2 BiLimit(Vector2 val, Vector2 inclusiveMin, Vector2 inclusiveMax, ClampType clampType=ClampType.Block)
    {
        Vector2 retval = val;
        if (clampType == ClampType.Block)
        {
            retval.x = Mathf.Clamp(retval.x, inclusiveMin.x, inclusiveMax.x);
            retval.y = Mathf.Clamp(retval.y, inclusiveMin.y, inclusiveMax.y);
        }
        else if (clampType == ClampType.Return)
        {
            if(retval.x < inclusiveMin.x)
            {
                retval.x = inclusiveMax.x;
            }
            else if(retval.x > inclusiveMax.x)
            {
                retval.x = inclusiveMin.x;
            }

            if(retval.y < inclusiveMin.y)
            {
                retval.y = inclusiveMax.y;
            }
            else if(retval.y > inclusiveMax.y)
            {
                retval.y = inclusiveMin.y;
            }
        }

        return retval;
    }

    // 단방향 clamp
    public static Vector3 MonoLimit(Vector3 val, Vector3 inclusiveLimit, ClampDir clampDir)
    {
        Vector3 retval = val;

        if (clampDir == ClampDir.Min)
        {
            if(retval.x < inclusiveLimit.x)
            {
                retval.x = inclusiveLimit.x;
            }
            if(retval.y < inclusiveLimit.y)
            {
                retval.y = inclusiveLimit.y;
            }
            if(retval.z < inclusiveLimit.z)
            {
                retval.z = inclusiveLimit.z;
            }
        }
        else if (clampDir == ClampDir.Max)
        {
            if(retval.x > inclusiveLimit.x)
            {
                retval.x = inclusiveLimit.x;
            }
            if(retval.y > inclusiveLimit.y)
            {
                retval.y = inclusiveLimit.y;
            }
            if(retval.z > inclusiveLimit.z)
            {
                retval.z = inclusiveLimit.z;
            }
        }

        return retval;
    }

    // 단방향 clamp
    public static Vector2 MonoLimit(Vector2 val, Vector2 inclusiveLimit, ClampDir clampDir)
    {
        Vector2 retval = val;

        if (clampDir == ClampDir.Min)
        {
            if(retval.x < inclusiveLimit.x)
            {
                retval.x = inclusiveLimit.x;
            }
            if(retval.y < inclusiveLimit.y)
            {
                retval.y = inclusiveLimit.y;
            }
        }
        else if (clampDir == ClampDir.Max)
        {
            if(retval.x > inclusiveLimit.x)
            {
                retval.x = inclusiveLimit.x;
            }
            if(retval.y > inclusiveLimit.y)
            {
                retval.y = inclusiveLimit.y;
            }
        }

        return retval;
    }
}
