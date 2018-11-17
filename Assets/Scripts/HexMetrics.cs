using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class HexMetrics
{
    public const float outerRadius = 10f;
    public const float outerLength = 0.866025404f;//根号(3)/2
    public const float innerRadius = outerRadius * outerLength;
    public const float solidFactor = 0.75f;
    public const float blendFactor = 1f - solidFactor;


    private static Vector3[] corners =
    {
        new Vector3(0f,0f,outerRadius),
        new Vector3(innerRadius,0f,0.5f*outerRadius),
        new Vector3(innerRadius,0f,-0.5f*outerRadius),
        new Vector3(0f,0f,-outerRadius),
        new Vector3(-innerRadius,0f,-0.5f*outerRadius),
        new Vector3(-innerRadius,0f,0.5f*outerRadius),
        new Vector3(0f,0f,outerRadius),
    };

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSoliderCorner(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSoliderCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * solidFactor;
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1])
                * blendFactor;
    }
}
