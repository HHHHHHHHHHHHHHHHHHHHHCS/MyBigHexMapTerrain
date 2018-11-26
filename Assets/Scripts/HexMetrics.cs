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
    /// 到边的率
    /// </summary>
    public const float outerInner = 0.866025404f;//根号(3)/2 

    /// <summary>
    /// 到顶点的率
    /// </summary>
    public const float innerToOuter = 1f / outerInner;

    /// <summary>
    /// 六边形到边的长度
    /// </summary>
    public const float innerRadius = outerRadius * outerInner;

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
    /// 噪音 地形的左右偏移
    /// </summary>
    public const float cellPerturbStrength = 4f;

    /// <summary>
    /// 噪音 噪音的缩放
    /// </summary>
    public const float noiseScale = 0.003f;

    /// <summary>
    /// 噪音 小地形块的高度强度
    /// </summary>
    public const float elevationPerturbStrength = 1.5f;

    /// <summary>
    /// 地形小块的尺寸
    /// </summary>
    public const int chunkSizeX = 5, chunkSizeZ = 5;

    /// <summary>
    /// 河底的凹陷程度 单位格  最后要乘以step
    /// </summary>
    public const float streamBedElevationOffset = -1.75f;

    /// <summary>
    /// 河表面的凹陷程度 单位格  最后要乘以step
    /// </summary>
    public const float riverSurfaceElevationOffset = -0.5f;

    /// <summary>
    /// 道路高度差,大于这个就不能有道路了
    /// </summary>
    public const int roadDifferceHeight = 1;

    /// <summary>
    /// 地形的噪音图
    /// </summary>
    public static Texture2D noiseSource;

    /// <summary>
    /// 六边形的offset
    /// </summary>
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

    /// <summary>
    /// 得到方向所对应的位置
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }

    /// <summary>
    /// 得到下一个方向所对应的位置
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[(int)direction + 1];
    }

    /// <summary>
    /// 得到方向所对应的位置*混合度
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return GetFirstCorner(direction) * solidFactor;
    }

    /// <summary>
    /// 得到下个方向所对应的位置*混合度
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return GetSecondCorner(direction) * solidFactor;
    }

    /// <summary>
    /// 得到两个方向的中心点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1])
            * (0.5f * solidFactor);
    }

    /// <summary>
    /// 得到桥的offset
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1])
                * blendFactor;
    }

    /// <summary>
    /// 顶点的梯度lerp
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    /// <summary>
    /// 颜色的梯度lerp
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    /// <summary>
    /// 根据高度,判断两个cell,之间的关系
    /// </summary>
    /// <param name="elevation1"></param>
    /// <param name="elevation2"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 得到噪音的某个point
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.y * noiseScale);
    }

    /// <summary>
    /// 地形的顶点的噪音偏移
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        //position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }
}
