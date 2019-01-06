using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级生成地形
/// </summary>
public class HexMapGenerator : MonoBehaviour
{
    //用于天气
    public struct ClimateData
    {
        /// <summary>
        /// 云
        /// </summary>
        public float clouds;
        /// <summary>
        /// 湿气
        /// </summary>
        public float moisture;
    }

    /// <summary>
    /// 陆地生成的XZ的边界
    /// </summary>
    private struct MapRegion
    {
        public int xMin, xMax, zMin, zMax;
    }


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
    /// 随机生成地形的圆形程度,越小越圆
    /// </summary>
    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;

    /// <summary>
    /// 生成随机地形的最每块的最小的cell的数量
    /// </summary>
    [Range(20, 200)]
    public int chunkSizeMin = 30;

    /// <summary>
    /// 生成随机地形的每块最大的cell数量
    /// </summary>
    [Range(20, 200)]
    public int chunkSizeMax = 100;

    /// <summary>
    /// 生成孤立悬崖的概率,即高度+1/+2
    /// </summary>
    [Range(0f, 1f)]
    public float highRiseProbability = 0.25f;

    /// <summary>
    /// 地形下降的概率
    /// </summary>
    [Range(0, 0.4f)] public float sinkProbability = 0.2f;

    /// <summary>
    /// 陆地的块的百分比
    /// </summary>
    [Range(0.05f, 0.95f)] public float landPercentage = 0.5f;


    /// <summary>
    /// 水的高度
    /// </summary>
    [Range(1, 5)] public int waterLevel = 3;

    /// <summary>
    /// 最低的的高度
    /// </summary>
    [Range(-4, 0)] public int elevationMinimum = -2;

    /// <summary>
    /// 最高的高度
    /// </summary>
    [Range(6, 10)] public int elevationMaximum = 8;

    /// <summary>
    /// 陆地生成的X边界
    /// </summary>
    [Range(0, 10)] public int mapBorderX = 5;

    /// <summary>
    /// 陆地生成的Z边界
    /// </summary>
    [Range(0, 10)] public int mapBorderZ = 5;

    /// <summary>
    /// 每小个地形块的边界
    /// </summary>
    [Range(0, 10)] public int regionBorder = 5;

    /// <summary>
    /// 最多生成几个区域块
    /// </summary>
    [Range(1, 4)] public int regionCount = 1;

    /// <summary>
    /// 侵蚀打磨程度
    /// </summary>
    [Range(0, 100)]
    public int erosionPercentage = 50;

    /// <summary>
    /// 土地初始的水分
    /// </summary>
    [Range(0f, 1f)]
    public float startingMoisture = 0.1f;

    /// <summary>
    /// 蒸发多少水分
    /// </summary>
    [Range(0, 1f)]
    public float evaporationFactor = 0.5f;

    /// <summary>
    /// 形成云后降雨的量
    /// </summary>
    [Range(0f, 1f)]
    public float precipitationFactor = 0.25f;

    /// <summary>
    /// 水土流失(高低地)
    /// </summary>
    [Range(0f, 1f)]
    public float runoffFactor = 0.25f;

    /// <summary>
    /// 渗漏(平级)
    /// </summary>
    [Range(0f, 1f)]
    public float seepageFactor = 0.125f;

    /// <summary>
    /// 风的方向
    /// </summary>
    public HexDirection windDirection = HexDirection.NW;

    /// <summary>
    /// 风的等级
    /// </summary>
    public float windStrength = 4f;


    private int cellCount; //一共有几个细胞
    private HexCellPriorityQueue searchFrontier; //随机生成寻路队列
    private int searchFrontierPhase; //随机生成寻路值
    private List<MapRegion> regions; //陆地生成的XZ的边界
    private List<ClimateData> climate = new List<ClimateData>();//天气系统
    private List<ClimateData> nextClimate = new List<ClimateData>();//下一次的天启系统

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
            seed &= int.MaxValue; //强制归正数
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
            grid.GetCell(i).WaterLevel = waterLevel;
        }

        //陆地生成的边界
        CreateRegions();

        //创建岛屿
        CreateLand();

        //侵蚀打磨边缘
        ErodeLand();

        //创建天气系统
        CreateClimate();

        //创建地图类型
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
    /// 陆地生成的边界
    /// </summary>
    private void CreateRegions()
    {
        if (regions == null)
        {
            regions = new List<MapRegion>();
        }
        else
        {
            regions.Clear();
        }

        var grid = HexGrid.Instance;
        MapRegion region;
        int countX = grid.cellCountX, countZ = grid.cellCountZ;
        //-----地形的规则裁块-----
        switch (regionCount)
        {
            //1或者其他
            default:
                {
                    region.xMin = mapBorderX;
                    region.xMax = countX - mapBorderX;
                    region.zMin = mapBorderZ;
                    region.zMax = countZ - mapBorderZ;
                    regions.Add(region);
                    break;
                }
            case 2:
                {
                    if (Random.value < 0.5f)
                    {
                        var halfX = countX / 2;
                        region.xMin = mapBorderX;
                        region.xMax = halfX - regionBorder;
                        region.zMin = mapBorderZ;
                        region.zMax = countZ - mapBorderZ;
                        regions.Add(region);
                        region.xMin = halfX + regionBorder;
                        region.xMax = countX - mapBorderX;
                        regions.Add(region);
                    }
                    else
                    {
                        var halfZ = countZ / 2;
                        region.xMin = mapBorderX;
                        region.xMax = countX - mapBorderX;
                        region.zMin = mapBorderZ;
                        region.zMax = halfZ - regionBorder;
                        regions.Add(region);
                        region.zMin = halfZ + regionBorder;
                        region.zMax = countZ - mapBorderZ;
                        regions.Add(region);
                    }

                    break;
                }
            case 3:
                {
                    int x_1_3 = countX / 3, x_2_3 = x_1_3 * 2;
                    region.xMin = mapBorderX;
                    region.xMax = x_1_3 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = countZ - mapBorderZ;
                    regions.Add(region);
                    region.xMin = x_1_3 + regionBorder;
                    region.xMax = x_2_3 - regionBorder;
                    regions.Add(region);
                    region.xMin = x_2_3 + regionBorder;
                    region.xMax = countX - mapBorderX;
                    regions.Add(region);
                    break;
                }
            case 4:
                {
                    int halfX = countX / 2, halfZ = countZ / 2;
                    region.xMin = mapBorderX;
                    region.xMax = halfX - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = halfZ - regionBorder;
                    regions.Add(region);
                    region.xMin = halfX + regionBorder;
                    region.xMax = countX - mapBorderX;
                    regions.Add(region);
                    region.zMin = halfZ + regionBorder;
                    region.zMax = countZ - mapBorderZ;
                    regions.Add(region);
                    region.xMin = mapBorderX;
                    region.xMax = countX - regionBorder;
                    regions.Add(region);
                    break;
                }
        }
    }

    /// <summary>
    /// 创建岛屿,里面有上升块和下降块
    /// </summary>
    private void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage);
        for (int guard = 0; guard < 10000; guard++)
        {
            bool isSink = Random.value < sinkProbability;
            foreach (var region in regions)
            {
                //这个块有几个cell
                int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);

                //根据概率是否要上升下降块,并且得到剩下的陆地的块
                if (isSink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }
        }

        if (landBudget > 0)
        {
            Debug.Log("无法生成这么多的陆地的块,还剩下" + landBudget.ToString());
        }
    }

    /// <summary>
    /// 上升地形
    /// </summary>
    /// <param name="chunkSize">要上升的块</param>
    /// <param name="budget">水的块数</param>
    /// <returns>还剩下多少水的块</returns>
    private int RaiseTerrain(int chunkSize, int budget, MapRegion region)
    {
        //-----随机得到一个细胞
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.coordinates;

        //这次要上升几个高度
        int rise = Random.value < highRiseProbability ? 2 : 1;
        int size = 0; //已经上升的块
        //上升的块<要上升的块&&寻路队列里还有cell
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();

            int originalElevation = current.Elevation;
            int newElevation = originalElevation + rise;
            if (newElevation > elevationMaximum)
            {
                //如果新高度超过最高高度,就放弃
                continue;
            }

            current.Elevation = newElevation;

            //如果改变的这块高度出水了,则陆地的块-1
            //如果陆地的块为0,则跳出上升块的while
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
    private int SinkTerrain(int chunkSize, int budget, MapRegion region)
    {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
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
    /// 侵蚀打磨边缘
    /// </summary>
    private void ErodeLand()
    {
        var erodibleCells = ListPool<HexCell>.Get();
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = HexGrid.Instance.GetCell(i);
            if (IsErodible(cell))
            {
                erodibleCells.Add(cell);
            }
        }
        //要打磨的块的个数
        int targetErodibleCount = (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count);
            HexCell cell = erodibleCells[index];//当前侵蚀的cell
            HexCell targetCell = GetErosionTarget(cell);//被转移的目标

            cell.Elevation -= 1;
            targetCell.Elevation += 1;
            //放到最后移除,可以提升效率
            var maxIndex = erodibleCells.Count - 1;
            //如果高度还过高
            if (!IsErodible(cell))
            {
                erodibleCells[index] = erodibleCells[maxIndex];
                erodibleCells.RemoveAt(maxIndex);
            }

            //因为这个当前细胞的降低,导致的周围可能会高峰
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (neighbor && neighbor.Elevation == cell.Elevation + 2
                    && !erodibleCells.Contains(neighbor))
                {
                    erodibleCells.Add(neighbor);
                }
            }

            //因为转移Cell被抬高了,可能引起高峰
            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
            {
                erodibleCells.Add(targetCell);
            }

            //抬高了转移细胞,可能转移细胞周围的细胞就不会是高峰了
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = targetCell.GetNeighbor(d);
                if (neighbor && neighbor != cell
                    && neighbor.Elevation == targetCell.Elevation + 1
                    && !IsErodible(neighbor))
                {
                    erodibleCells.Remove(neighbor);
                }
            }
        }

        ListPool<HexCell>.Add(erodibleCells);
    }

    /// <summary>
    /// 寻找容易侵蚀的对象,比如高峰
    /// 只要比任意一处低 X 高峰
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    private bool IsErodible(HexCell cell)
    {
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 被侵蚀之后,转移的高度的cell
    /// </summary>
    private HexCell GetErosionTarget(HexCell cell)
    {
        var candidates = ListPool<HexCell>.Get();
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                candidates.Add(neighbor);
            }
        }

        HexCell target = candidates[Random.Range(0, candidates.Count)];
        ListPool<HexCell>.Add(candidates);
        return target;
    }


    /// <summary>
    /// 创建天气系统
    /// </summary>
    private void CreateClimate()
    {
        climate.Clear();
        nextClimate.Clear();
        ClimateData initialData = new ClimateData();
        initialData.moisture = startingMoisture;
        ClimateData clearData = new ClimateData();
        for (int i = 0; i < cellCount; i++)
        {
            climate.Add(initialData);
            nextClimate.Add(clearData);
        }
        for (int cycle = 0; cycle < 40; cycle++)
        {
            for (int i = 0; i < cellCount; i++)
            {
                EvolveClimate(i);
            }
            //交换天启系统
            var swap = climate;
            climate = nextClimate;
            nextClimate = swap;
        }

    }

    /// <summary>
    /// 蒸发水分和高低地的水土流失
    /// </summary>
    private void EvolveClimate(int cellIndex)
    {
        HexCell cell = HexGrid.Instance.GetCell(cellIndex);
        ClimateData cellClimate = climate[cellIndex];

        if (cell.IsUnderwater)
        {//是水,水分为1,形成云
            cellClimate.moisture = 1f;
            cellClimate.clouds += evaporationFactor;
        }
        else
        {//是陆地,计算土里水分*挥发比例
            float evaporation = cellClimate.moisture * evaporationFactor;
            cellClimate.moisture -= evaporation;
            cellClimate.clouds += evaporation;
        }

        //云的凝结成水分
        float precipitation = cellClimate.clouds * precipitationFactor;
        cellClimate.clouds -= precipitation;
        cellClimate.moisture += precipitation;

        //越高越容易凝结降水,所以雨水不能超过一定百分比高度
        float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
        if (cellClimate.clouds > cloudMaximum)
        {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum;
            cellClimate.clouds = cloudMaximum;
        }

        //云在风的反方向扩散
        HexDirection mainDispersalDirection = windDirection.Opposite();
        //云的转移
        float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
        //高地底的转移
        float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
        //平级的转移
        float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (!neighbor)
            {
                continue;
            }

            ClimateData neighborClimate = nextClimate[neighbor.Index];

            //根据风向转移云
            if (d == mainDispersalDirection)
            {
                neighborClimate.clouds += cloudDispersal * windStrength;
            }
            else
            {
                neighborClimate.clouds += cloudDispersal;
            }
            

            //根据视野高度,高低地的关系,高低转移,还是平级转移
            int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
            if (elevationDelta < 0)
            {
                cellClimate.moisture -= runoff;
                neighborClimate.moisture += runoff;
            }
            else if (elevationDelta == 0)
            {
                cellClimate.moisture -= seepage;
                neighborClimate.moisture += seepage;
            }

            nextClimate[neighbor.Index] = neighborClimate;//设置邻居
        }
        //设置湿气数据进下一次缓冲
        ClimateData nextCellClimate = nextClimate[cellIndex];
        nextCellClimate.moisture += cellClimate.moisture;
        if (nextCellClimate.moisture > 1f)
        {
            nextCellClimate.moisture = 1f;
        }
        nextClimate[cellIndex] = nextCellClimate;
        climate[cellIndex] = new ClimateData();
    }


    /// <summary>
    /// 根据高度设置地形的种类
    /// </summary>
    private void SetTerrainType()
    {
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = HexGrid.Instance.GetCell(i);
            float moisture = climate[i].moisture;
            int type = 0;
            if (!cell.IsUnderwater)
            {
                if (moisture < 0.05f)
                {//沙漠
                    type = 4;
                }
                else if (moisture < 0.12f)
                {//雪山
                    type = 0;
                }
                else if (moisture < 0.28f)
                {//石头
                    type = 3;
                }
                else if (moisture < 0.85f)
                {//草地
                    type = 1;
                }
                else
                {//泥土
                    type = 2;
                }
            }
            else
            {//水下泥土
                type = 2;
            }
            cell.TerrainTypeIndex = type;
            cell.SetMapData(moisture);
        }
    }

    /// <summary>
    /// 得到一个随机的cell
    /// </summary>
    /// <returns></returns>
    private HexCell GetRandomCell(MapRegion region)
    {
        var grid = HexGrid.Instance;

        return grid.GetCell(
            Random.Range(region.xMin, region.xMax)
            , Random.Range(region.zMin, region.zMax));
    }
}
