using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    public static HexMapGenerator Instance { get; private set; }

    /// <summary>
    /// 随机生成地形的连续扩张程度,值越小越紧密
    /// </summary>
    [Range(0f, 0.5f)] public float jitterProbability = 0.25f;

    /// <summary>
    /// 生成随机地形的最每块的最小的cell的数量
    /// </summary>
    [Range(20, 200)] public int chunkSizeMin = 30;

    /// <summary>
    /// 生成随机地形的每块最大的cell数量
    /// </summary>
    [Range(20, 200)] public int chunkSizeMax = 100;

    /// <summary>
    /// 陆地的百分比
    /// </summary>
    [Range(0.05f, 0.95f)] public float landPercentage = 0.5f;

    private int cellCount;
    private HexCellPriorityQueue searchFrontier;
    private int searchFrontierPhase;

    private void Awake()
    {
        Instance = this;
    }

    public void GenerateMap(int x, int z)
    {
        var grid = HexGrid.Instance;
        cellCount = x * z;
        grid.CreateMap(x, z);
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }

        CreateLand();

        for (int i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }
    }

    private int RaiseTerrain(int chunkSize, int budget)
    {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.coordinates;

        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();

            if (current.TerrainTypeIndex == 0)
            {
                current.TerrainTypeIndex = 1;
                if ((budget--) == 0)
                {
                    break;
                }
            }

            size += 1;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }

        searchFrontier.Clear();
        return budget;
    }

    private HexCell GetRandomCell()
    {
        var grid = HexGrid.Instance;
        return grid.GetCell(Random.Range(0, cellCount));
    }

    private void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage);
        while (landBudget > 0)
        {
            landBudget = RaiseTerrain(Random.Range(chunkSizeMin, chunkSizeMax + 1), landBudget);
        }
    }
}