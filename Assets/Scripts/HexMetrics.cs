using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形的数据配置
/// </summary>
public sealed class HexMetrics
{
    /// <summary>
    /// 半径
    /// </summary>
    public const float outerRadius = 10f;

    /// <summary>
    /// 斜边率
    /// </summary>
    public const float outerLength = 0.866025404f;//根号(3)/2 

    /// <summary>
    /// 斜边长
    /// </summary>
    public const float innerRadius = outerRadius * outerLength;

    /// <summary>
    /// 混合度
    /// </summary>
    public const float solidFactor = 0.8f;

    /// <summary>
    /// 混合度差
    /// </summary>
    public const float blendFactor = 1f - solidFactor;

    /// <summary>
    /// 高度步长
    /// </summary>
    public const float elevationStep = 3f;

    /// <summary>
    /// 地形有几个台阶
    /// </summary>
    public const int terracesPerSlope = 2;

    /// <summary>
    /// 同上(terracesPerslope),分成几块的百分比
    /// </summary>
    public const float horizontalTerraceStepSize = 1f / terraceSteps;

    /// <summary>
    /// 进行台阶合成用,把最高点最低点的X轴分成几块
    /// </summary>
    public const int terraceSteps = terracesPerSlope * 2 + 1;

    /// <summary>
    /// 同上(terraceSteps),分成几块的百分比
    /// </summary>
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    /// <summary>
    /// 噪音 地形凹凸的强度
    /// </summary>
    public const float cellPerturbStrength = 5f;

    /// <summary>
    /// 噪音 噪音的跨度
    /// </summary>
    public const float noiseScale = 0.003f;

    /// <summary>
    /// 小地形块的强度
    /// </summary>
    public const float elevationPerturbStrength = 1.5f;

    /// <summary>
    /// 地形小块的尺寸
    /// </summary>
    public const int chunkSizeX = 5, chunkSizeZ = 5;

    /// <summary>
    /// 地形的噪音图
    /// </summary>
    public static Texture2D noiseSource;

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

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }

        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1)
        {
            return HexEdgeType.Slope;
        }
        return HexEdgeType.Cliff;
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.y * noiseScale);
    }
}
