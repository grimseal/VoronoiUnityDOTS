using System;
using Unity.Mathematics;
using UnityEngine;

public static class MathExtensions
{
    public const double EPSILON = double.Epsilon*1E100;
    
    public static Vector3 ToVector3(this float2 that)
    {
        return new Vector3(that.x, 0, that.y);
    }
    
    public static bool ApproxEqual(this float value1, float value2)
    {
        return math.abs(value1 - value2) <= float.Epsilon;
    }
    
    public static bool ApproxEqual(this double value1, double value2)
    {
        return Math.Abs(value1 - value2) <= EPSILON;
    }

    public static bool ApproxGreaterThanOrEqualTo(this float value1, float value2)
    {
        return value1 > value2 || value1.ApproxEqual(value2);
    }

    public static bool ApproxLessThanOrEqualTo(this float value1, float value2)
    {
        return value1 < value2 || value1.ApproxEqual(value2);
    }
}