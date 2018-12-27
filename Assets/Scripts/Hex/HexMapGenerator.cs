using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级生成地形
/// </summary>
public class HexMapGenerator : MonoBehaviour
{
    /// <summary>
    /// 单例化用
    /// </summary>
    public static HexMapGenerator Instance { get; private set; }

    /// <summary>
    /// 是否使用自己的种子
    /// </summary>
    public bool useFixedSeed;

    /// <summary>
    /// 随机种子
    /// </summary>
    public int seed;

    /// <summary>
    /// 水的高度
    /// </summary>
    [Range(1, 5)] public int waterLevel = 3;

    /// <summary>
    /// 最低的的高度
    /// </summary>
    [Range(-4, 0)]
    public int elevationMinimum = -2;

    /// <summary>
    /// 最高的高度
    /// </summary>
    [Range(6, 10)]
    public int elevationMaximum = 8;

    /// <summary>
    /// 生成孤立悬崖的概率,即高度+1/+2
    /// </summary>
    [Range(0f, 1f)] public float highRiseProbability = 0.25f;

    /// <summary>
    /// 地形下降的概率
    /// </summary>
    [Range(0, 0.4f)]
    public float sinkProbability = 0.2f;

    /// <summary>
    /// 随机生成地形的圆形程度,越小越圆
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
    /// 水的百分比
    /// </summary>
    [Range(0.05f, 0.95f)] public float landPercentage = 0.5f;

    private int cellCount;//一共有几个细胞
    private HexCellPriorityQueue searchFrontier;//随机生成寻路队列
    private int searchFrontierPhase;//随机生成寻路值

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 生成随机地形
    /// </summary>
    public void GenerateMap(int x, int z)
    {
        //-----随机种子-----
        Random.State originalRandomState = Random.state;
        if (!useFixedSeed)
        {
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int)System.DateTime.Now.Ticks;
            seed ^= (int)Time.unscaledTime;
            seed &= int.MaxValue;//强制归正数
        }
        Random.InitState(seed);

        //-----生成基础地形
        var grid = HexGrid.Instance;
        cellCount = x * z;
        grid.CreateMap(x, z);

        //-----随机生成寻路队列
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }

        //-----全部初始化为水
        for (int i = 0; i < cellCount; i++)
        {
            HexGrid.Instance.GetCell(i).WaterLevel = waterLevel;
        }

        //
        CreateLand();

        //
        SetTerrainType();

        //重置寻路值
        for (int i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }

        //重置种子
        Random.state = originalRandomState;
    }

    /// <summary>
    /// 上升地形
    /// </summary>
    /// <param name="chunkSize">要上升的块</param>
    /// <param name="budget">水的块数</param>
    /// <returns>还剩下多少水的块</returns>
    private int RaiseTerrain(int chunkSize, int budget)
    {
        //-----随机得到一个细胞
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.coordinates;

        //这次要上升几个高度
        int rise = Random.value < highRiseProbability ? 2 : 1;
        int size = 0;//已经上升的块
        //上升的块<要上升的块&&寻路队列里还有cell
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();

            int originalElevation = current.Elevation;
            int newElevation = originalElevation + rise;
            if (newElevation > elevationMaximum)
            {//如果新高度超过最高高度,就放弃
                continue;
            }
            current.Elevation = newElevation;

            //如果改变的这块高度出水了,则水的块-1
            //如果水的块为0,则跳出上升块的while
            if (originalElevation < waterLevel
                && newElevation >= waterLevel
                && --budget == 0)
            {
                break;
            }

            size += 1;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    //这里是生成的地形的圆形程度
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }

        //重置队列,返回水的块
        searchFrontier.Clear();
        return budget;
    }

    /// <summary>
    /// <see cref="RaiseTerrain"/>
    /// </summary>
    /// <param name="chunkSize">要下降的块</param>
    /// <param name="budget">水的块数</param>
    /// <returns>还剩下多少水的块</returns>
    private int SinkTerrain(int chunkSize, int budget)
    {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.coordinates;

        int sink = Random.value < highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = originalElevation - sink;
            if (newElevation < elevationMinimum)
            {
                continue;
            }

            current.Elevation = newElevation;
            if (originalElevation >= waterLevel
                && newElevation < waterLevel)
            {
                budget += 1;
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

    /// <summary>
    /// 得到一个随机的cell
    /// </summary>
    /// <returns></returns>
    private HexCell GetRandomCell()
    {
        var grid = HexGrid.Instance;
        return grid.GetCell(Random.Range(0, cellCount));
    }

    /// <summary>
    /// 创建岛屿,里面有上升块和下降块
    /// </summary>
    private void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage);
        while (landBudget > 0)
        {
            //这个块有几个cell
            int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);

            //根据概率是否要上升下降块,并且得到剩下的水的块
            landBudget = Random.value < sinkProbability 
                ? SinkTerrain(chunkSize, landBudget) 
                : RaiseTerrain(chunkSize, landBudget);
        }
    }

    /// <summary>
    /// 根据高度设置地形的种类
    /// </summary>
    private void SetTerrainType()
    {
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = HexGrid.Instance.GetCell(i);
            if (!cell.IsUnderwater)
            {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }
}