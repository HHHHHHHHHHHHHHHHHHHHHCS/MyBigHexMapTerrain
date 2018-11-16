using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexCell : MonoBehaviour
{

    public HexCoordinates coordinates;

    public Color color;

    private HexCell[] neightbors = new HexCell[6];

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neightbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neightbors[(int)direction] = cell;
        cell.neightbors[(int)direction.Opposite()] = this;
    }
}
