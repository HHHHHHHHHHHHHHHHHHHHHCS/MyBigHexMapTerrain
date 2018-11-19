using System.Collections;
using System.Collections.Generic;
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
    Flat,Slope, Cliff//平地,临界坡,大坡
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

}

