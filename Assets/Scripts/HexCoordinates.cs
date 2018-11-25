using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个cell 的坐标数据
/// </summary>
[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int x, y, z;

    public int X { get { return x; } }
    public int Y { get { return y; } }
    public int Z { get { return z; } }

    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.y = -x - z;
        this.z = z;
    }

    public HexCoordinates(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
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

    public override string ToString()
    {
        return string.Format("({0},{1},{2})", X, Y, Z);
    }

    public string ToStringOnSeparateLines()
    {
        return string.Format("{0}\n{1}\n{2}", X, Y, Z);
    }
}
