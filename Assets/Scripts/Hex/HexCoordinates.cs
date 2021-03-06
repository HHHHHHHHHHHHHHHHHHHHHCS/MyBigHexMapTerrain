﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 单个cell 的坐标数据
/// </summary>
[System.Serializable]
public struct HexCoordinates
{
    [SerializeField] private int x, z;

    public int X
    {
        get { return x; }
    }

    public int Y
    {
        get { return -x-z; }
    }

    public int Z
    {
        get { return z; }
    }

    public HexCoordinates(int x, int z)
    {
        if (HexMetrics.Wrapping)
        {//正确循环尺寸
            int oX = x + z / 2;
            if (oX < 0)
            {
                x += HexMetrics.wrapSize;
            }
            else if (oX >= HexMetrics.wrapSize)
            {
                x -= HexMetrics.wrapSize;
            }
        }

        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo(HexCoordinates other)
    {
        int _x = x < other.x ? other.x - x : x - other.x;
        int _y = Y < other.Y ? other.Y - Y : Y - other.Y;

        int _xy = _x + _y;

        if (HexMetrics.Wrapping)
        {//地形循环

            //如果左侧到右侧的寻路
            other.x += HexMetrics.wrapSize;
            _x = x < other.x ? other.x - x : x - other.x;
            _y = Y < other.Y ? other.Y - Y : Y - other.Y;
            var xyWrapped = _x + _y;
            if (xyWrapped < _xy)
            {
                _xy = xyWrapped;
            }
            else
            {//因为上面加了HexMetrics.wrapSize 所以这里减两倍
                other.x -= 2 * HexMetrics.wrapSize;
                _x = x < other.x ? other.x - x : x - other.x;
                _y = Y < other.Y ? other.Y - Y : Y - other.Y;

                xyWrapped = _x + _y;
                if (xyWrapped < _xy)
                {
                    _xy = xyWrapped;
                }
            }
        }

        int _z = z < other.z ? other.z - z : z - other.z;
        //除以2 是因为x+y+z=0 但是我们这边取abs 了 所以除以2
        return (_xy + _z) / 2;
    }

    public void Save(MyWriter writer)
    {
        writer.Write(x);
        writer.Write(z);
    }

    public static HexCoordinates Load(MyReader reader)
    {
        int x = reader.ReadInt32();
        int z = reader.ReadInt32();
        HexCoordinates c = new HexCoordinates(x, z);

        return c;
    }

    public override string ToString()
    {
        return $"({X},{Y},{Z})";
    }

    public string ToStringOnSeparateLines()
    {
        return $"{X}\n{Y}\n{Z}";
    }
}