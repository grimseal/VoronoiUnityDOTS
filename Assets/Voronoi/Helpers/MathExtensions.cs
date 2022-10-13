using Unity.Mathematics;
using UnityEngine;

public static class MathExtensions
{
    public static Vector3 ToVector3(this float2 that)
    {
        return new Vector3(that.x, 0, that.y);
    }
}