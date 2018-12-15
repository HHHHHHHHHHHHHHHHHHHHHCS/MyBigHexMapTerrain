#define _test//去掉_ 进行测试模式
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
#if test
    public const float cellPerturbStrength = 0f;
#else
    public const float cellPerturbStrength = 4f;
#endif

    /// <summary>
    /// 噪音 噪音的缩放
    /// </summary>
    public const float noiseScale = 0.003f;

    /// <summary>
    /// 噪音 小地形块的高度强度
    /// </summary>
#if test
    public const float elevationPerturbStrength = 0f;
#else
    public const float elevationPerturbStrength = 1.5f;
#endif


    /// <summary>
    /// 地形小块的尺寸
    /// </summary>
    public const int chunkSizeX = 5, chunkSizeZ = 5;

    /// <summary>
    /// 河底的凹陷程度 单位格  最后要乘以step
    /// </summary>
    public const float streamBedElevationOffset = -1.75f;

    /// <summary>
    /// 水表面的凹陷程度 单位格  最后要乘以step
    /// </summary>
    public const float waterElevationOffset = -0.5f;

    /// <summary>
    /// 道路高度差,大于这个就不能有道路了
    /// </summary>
    public const int roadDifferceHeight = 1;

    /// <summary>
    /// 水的混合度
    /// </summary>
    public const float waterFacctor = 0.6f;

    /// <summary>
    /// 1-水的混合度
    /// </summary>
    public const float waterBlendFactor = 1 - waterFacctor;

    /// <summary>
    /// 格子的hash 数量
    /// </summary>
    public const int hashGridSize = 256;

    /// <summary>
    /// 哈希网格的尺寸缩放
    /// </summary>
    public const float hashGridScale = 0.25f;

    /// <summary>
    /// 墙体的高度
    /// </summary>
    public const float wallHeight = 4f;

    /// <summary>
    /// 墙体的高度偏移
    /// </summary>
    public const float wallYOffset = -1f;

    /// <summary>
    /// 墙体的厚度
    /// </summary>
    public const float wallThickness = 0.75f;

    /// <summary>
    /// 墙体的高度偏移
    /// </summary>
    public const float wallElevationOffset = verticalTerraceStepSize;

    /// <summary>
    /// 塔墙的生成阀值
    /// </summary>
    public const float wallTowerThreshold = 0.5f;

    /// <summary>
    /// 桥的设计长度
    /// </summary>
    public const float bridgeDesignLength = 7f;

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
    /// 装饰物的随机概率
    /// </summary>
    private static float[][] featureThresholds ={
        new float[]{0.0f,0.0f,0.4f},
        new float[]{0.0f,0.4f,0.6f},
        new float[]{0.4f,0.6f,0.8f},};


    /// <summary>
    /// 格子的hash
    /// </summary>
    private static HexHash[] hashGrid;

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

    /// <summary>
    /// 得到方向所对应的位置 * 水的混合度
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return GetFirstCorner(direction) * waterFacctor;
    }


    /// <summary>
    /// 得到下一个方向所对应的位置 * 水的混合度
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return GetSecondCorner(direction) * waterFacctor;
    }

    /// <summary>
    /// 得到水的桥的offset
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (GetFirstCorner(direction) + GetSecondCorner(direction))
            * waterBlendFactor;
    }

    /// <summary>
    /// 初始化格子的hash
    /// </summary>
    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }

    /// <summary>
    /// 计算hash 格子
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0)
        {
            x += hashGridSize;
        }
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0)
        {
            z += hashGridSize;
        }

        return hashGrid[x + z * hashGridSize];
    }

    /// <summary>
    /// 根据装饰物的等级得到装饰物的随机概率
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }

    /// <summary>
    /// 得到墙体厚度的偏差(一半)
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <returns></returns>
    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0f;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness * 0.5f);
    }

    /// <summary>
    /// 得到墙体的高度(插入土地用)
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <returns></returns>
    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v =
            near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        near.y += (far.y - near.y) * v+wallYOffset;
        return near;
    }
}
