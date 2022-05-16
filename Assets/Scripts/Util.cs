using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    public static Vector3 WithX(this Vector3 v, float x)
    {
        return new Vector3(x, v.y, v.z);
    }

    public static Vector3 WithY(this Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    public static Vector3 WithZ(this Vector3 v, float z)
    {
        return new Vector3(v.x, v.y, z);
    }

    public static Vector3 ToHorizontal(this Vector3 v)
    {
        return Vector3.ProjectOnPlane(v, Gravity.Down);
    }

	public static float VerticalComponent(this Vector3 v)
    {
        return Vector3.Dot(v, Gravity.Up);
    }
	
    public static Vector3 TransformDirectionHorizontal(this Transform t, Vector3 v)
    {
        return t.TransformDirection(v).ToHorizontal().normalized;
    }

    public static Vector3 InverseTransformDirectionHorizontal(this Transform t, Vector3 v)
    {
        return t.InverseTransformDirection(v).ToHorizontal().normalized;
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            (list[n], list[k]) = (list[k], list[n]); // Swap
        }
    }
}
