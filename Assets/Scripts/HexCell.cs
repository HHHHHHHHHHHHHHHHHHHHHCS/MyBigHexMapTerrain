using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexCell : MonoBehaviour
{
    public enum HexDirection
    {
        NE,E,SE,SW,W,NW
    }


    public HexCoordinates coordinates;

    public Color color;

    [SerializeField]
    private HexCell[] neightbors;

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neightbors[(int)direction];
    }

    public void SetNeightbor(HexDirection direction,HexCell cell)
    {
        neightbors[(int)direction] = cell;
        cell.neightbors[(int)direction.Opposite()] = this;
    }
}
