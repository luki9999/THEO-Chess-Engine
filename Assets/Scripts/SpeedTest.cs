using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChessBoard;

public class SpeedTest
{

    static float TimeFunction(System.Action method, int iterations)
    {
        float start = Time.realtimeSinceStartup;
        for (int i = 0; i < iterations; i++)
        {
            method();
        }
        return Time.realtimeSinceStartup - start;
    }

    public static void TestFunctionSpeed(System.Action method, int iterations)
    {
        Debug.Log(iterations.ToString() + " iterations of " + method.ToString() + " took " + TimeFunction(method, iterations).ToString() + ".");
    }
}

