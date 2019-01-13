using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Gravity
{
    public static Vector3 Down { get; private set; }
    public static Vector3 Forward { get; private set; }
    public static Vector3 Up { get { return -Down; } }

    static Gravity()
    {
        Down = Vector3.down;
        Forward = Vector3.forward;
    }

    public static void Set(Vector3 down)
    {
        // Gravity will rotate around this axis with this amount
        Vector3 axis = Vector3.Cross(Down, down);
        float angle = Vector3.Angle(Down, down);

        Down = down;
        Forward = Quaternion.AngleAxis(angle, axis) * Forward;
    }
}
