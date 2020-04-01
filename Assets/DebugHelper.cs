using Unity.Mathematics;
using UnityEngine;

public static class DebugHelper
{
    public static Vector3 ToVector3(this float2 v)
    {
        return new Vector3(v.x, 0, v.y);
    }
    
    public static Color SetAlpha(this Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }
}