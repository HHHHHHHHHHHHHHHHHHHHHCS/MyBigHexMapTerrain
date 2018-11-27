using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 六边形方向
/// </summary>
public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

/// <summary>
/// 地形的种类
/// </summary>
public enum HexEdgeType
{
    Flat, Slope, Cliff//平地,临界坡,大坡
}

/// <summary>
/// 把两个顶点 按权切割成几个顶点
/// </summary>
public struct EdgeVertices
{
    public Vector3 v1, v2, v3, v4, v5;

    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, 0.25f);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 0.75f);
        v5 = corner2;
    }

    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
        v5 = corner2;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
        result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
        result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
        result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
        result.v5 = HexMetrics.TerraceLerp(a.v5, b.v5, step);
        return result;
    }
}


/// <summary>
/// 地形的数据扩展方法
/// </summary>
public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}

/// <summary>
/// 扩充unity 一些方法方便测试
/// </summary>
public static class MyUnity
{
    private static StringBuilder sb;
    private const string spiltStr = "___";

    public static void Log(params object[] objArr)
    {
        if (sb == null)
        {
            sb = new StringBuilder();
        }

        foreach (var item in objArr)
        {
            sb.Append(item.ToString());
            sb.Append(spiltStr);
        }

        Debug.Log(sb);
        sb.Clear();
    }
}
