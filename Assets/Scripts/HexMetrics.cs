using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class HexMetrics
{
    public const float outerRadius = 10f;
    public const float outerLength = 0.866025404f;//根号(3)/2
    public const float innerRadius = outerRadius * outerLength;

    public static Vector3[] corners =
    {
        new Vector3(0f,0f,outerRadius),
        new Vector3(innerRadius,0f,0.5f*outerRadius),
        new Vector3(innerRadius,0f,-0.5f*outerRadius),
        new Vector3(0f,0f,-outerRadius),
        new Vector3(-innerRadius,0f,-0.5f*outerRadius),
        new Vector3(-innerRadius,0f,0.5f*outerRadius),
        new Vector3(0f,0f,outerRadius),
    };
}
