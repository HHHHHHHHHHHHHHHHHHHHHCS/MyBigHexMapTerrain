using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地形的单个
/// </summary>
public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public RectTransform uiRect;
    public HexGridChunk chunk;

    private Color color = new Color(0, 0, 0, 0);
    private HexCell[] neightbors = new HexCell[6];
    private int elevation = int.MinValue;

    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            if (elevation == value)
            {
                return;
            }

            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f)
                * HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;

            Refresh();
        }
    }

    public Color Color
    {
        get
        {
            return color;
        }

        set
        {
            if (color == value)
            {
                return;
            }
            color = value;
            Refresh();
        }
    }


    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neightbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neightbors[(int)direction] = cell;
        cell.neightbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation, neightbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    private void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();

            for (int i = 0; i < neightbors.Length; i++)
            {
                HexCell neighbor = neightbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
}
