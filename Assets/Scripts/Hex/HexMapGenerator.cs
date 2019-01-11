using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级生成地形
/// </summary>
public class HexMapGenerator : MonoBehaviour
{
    /// <summary>
    /// 用于天气
    /// </summary>
    private struct ClimateData
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
    /// 地球模式
    /// </summary>
    public enum HemisphereMode
    {
        Both, //中间
        North, //北半球
        South, //南半球
    }

    /// <summary>
    /// 陆地生成的XZ的边界
    /// </summary>
    private struct MapRegion
    {
        public int xMin, xMax, zMin, zMax;
    }

    /// <summary>
    /// 地形索引
    /// </summary>
    private struct Biome
    {
        public int terrain, plant;

        public Biome(int terrain, int plant)
        {
            this.terrain = terrain;
            this.plant = plant;
        }
    }

    //生成生物的概率
    //温度带越大,水分带越大,地形矩阵随着变化
    //0:沙漠 1:草地 2:泥土 3:石头 4:雪山
    //
    //0.6
    //温 
    //度  地形矩阵
    //带
    //0/0 水分带 0.85


    /// <summary>
    /// 温度带
    /// </summary>
    private static float[] temperatureBands = {0.1f, 0.3f, 0.6f};

    /// <summary>
    /// 水分带
    /// </summary>
    private static float[] moistureBands = {0.12f, 0.28f, 0.85f};

    /// <summary>
    /// 地形矩阵
    /// </summary>
    private static Biome[] biomes =
    {
        new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
        new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
        new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
        new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3),
    };

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
    /// 生成孤立悬崖的概率,即高度+1/+2
    /// </summary>
    [Range(0f, 1f)] public float highRiseProbability = 0.25f;

    /// <summary>
    /// 地形下降的概率
    /// </summary>
    [Range(0, 0.4f)] public float sinkProbability = 0.2f;

    /// <summary>
    /// 陆地的块的百分比
    /// </summary>
    [Range(0.05f, 0.95f)] public float landPercentage = 0.5f;


    /// <summary>
    /// 水平面的高度
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
    [Range(0, 100)] public int erosionPercentage = 50;

    /// <summary>
    /// 土地初始的水分
    /// </summary>
    [Range(0f, 1f)] public float startingMoisture = 0.1f;

    /// <summary>
    /// 蒸发多少水分
    /// </summary>
    [Range(0, 1f)] public float evaporationFactor = 0.5f;

    /// <summary>
    /// 形成云后降雨的量
    /// </summary>
    [Range(0f, 1f)] public float precipitationFactor = 0.25f;

    /// <summary>
    /// 水土流失(高低地)
    /// </summary>
    [Range(0f, 1f)] public float runoffFactor = 0.25f;

    /// <summary>
    /// 渗漏(平级)
    /// </summary>
    [Range(0f, 1f)] public float seepageFactor = 0.125f;

    /// <summary>
    /// 风的方向
    /// </summary>
    public HexDirection windDirection = HexDirection.NW;

    /// <summary>
    /// 风的等级
    /// </summary>
    public float windStrength = 4f;

    /// <summary>
    /// 河流的预算
    /// </summary>
    [Range(0, 20)] public float riverPercentage = 10;

    /// <summary>
    /// 生成湖泊的概率
    /// </summary>
    [Range(0f, 1f)] public float extraLakeProbability = 0.25f;

    /// <summary>
    /// 最低温度
    /// </summary>
    [Range(0f, 1f)] public float lowTemperature = 0f;

    /// <summary>
    /// 最高温度
    /// </summary>
    [Range(0f, 1f)] public float highTemperature = 1f;

    /// <summary>
    /// 区域在那个半球
    /// </summary>
    public HemisphereMode hemisphere;

    /// <summary>
    /// 温度波动率
    /// </summary>
    [Range(0f, 1f)] public float temperatureJitter = 0.1f;


    private int cellCount, landCells; //一共有几个细胞,多少个土地细胞
    private HexCellPriorityQueue searchFrontier; //随机生成寻路队列
    private int searchFrontierPhase; //随机生成寻路值
    private List<MapRegion> regions; //陆地生成的XZ的边界
    private List<ClimateData> climate = new List<ClimateData>(); //天气系统
    private List<ClimateData> nextClimate = new List<ClimateData>(); //下一次的天启系统
    private List<HexDirection> flowDirections = new List<HexDirection>(); //河流储存的方向
    private int temperatureJitterChannel; //天启系统用的随机通道

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
            seed ^= (int) System.DateTime.Now.Ticks;
            seed ^= (int) Time.unscaledTime;
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

        //创建河流系统
        CreateRivers();

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
        landCells = landBudget;
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
            landCells -= landBudget;
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
        int targetErodibleCount = (int) (erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count);
            HexCell cell = erodibleCells[index]; //当前侵蚀的cell
            HexCell targetCell = GetErosionTarget(cell); //被转移的目标

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
        {
            //是水,水分为1,形成云
            cellClimate.moisture = 1f;
            cellClimate.clouds += evaporationFactor;
        }
        else
        {
            //是陆地,计算土里水分*挥发比例
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

            nextClimate[neighbor.Index] = neighborClimate; //设置邻居
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
        temperatureJitterChannel = Random.Range(0, 4);
        //half 高度
        int rockDesertElevation =
            elevationMaximum - (elevationMaximum - waterLevel) / 2;
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = HexGrid.Instance.GetCell(i);
            float temperature = DetermineTemperature(cell);
            float moisture = climate[i].moisture;
            if (!cell.IsUnderwater)
            {
                int t = 0;
                for (; t < temperatureBands.Length; t++)
                {
                    if (temperature < temperatureBands[t])
                    {
                        break;
                    }
                }

                int m = 0;
                for (; m < moistureBands.Length; m++)
                {
                    if (moisture < moistureBands[m])
                    {
                        break;
                    }
                }

                Biome cellBiome = biomes[t * 4 + m];

                if (cellBiome.terrain == 0
                    && cell.Elevation >= rockDesertElevation)
                {
                    //如果是高处的沙漠,则变成岩石沙漠

                    cellBiome.terrain = 3;
                }
                else if (cell.Elevation == elevationMaximum)
                {
                    //如果是最高处的非沙漠则是雪山
                    cellBiome.terrain = 4;
                }


                if (cellBiome.terrain == 4)
                {
                    //是雪山地形,植物为0
                    cellBiome.plant = 0;
                }
                else if (cellBiome.plant < 3 && cell.HasRiver)
                {
                    //如果旁边有水源,植物等级+1
                    cellBiome.plant += 1;
                }

                cell.TerrainTypeIndex = cellBiome.terrain;
                cell.PlantLevel = cellBiome.plant;
            }
            else
            {
                int terrain;
                if (cell.Elevation == waterLevel - 1)
                {
                    //细胞在水平面的下面一格
                    //判断周围的格子如果比自己高一个,则是坡
                    //高N格,则是悬崖
                    int cliffs = 0, slopes = 0;
                    for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                    {
                        HexCell neighbor = cell.GetNeighbor(d);
                        if (!neighbor)
                        {
                            continue;
                        }

                        int delta = neighbor.Elevation - cell.WaterLevel;
                        if (delta == 0)
                        {
                            slopes += 1;
                        }
                        else if (delta > 0)
                        {
                            cliffs += 1;
                        }
                    }

                    if (cliffs + slopes > 3)
                    {
                        //悬崖和坡混合过高,是草地
                        terrain = 1;
                    }
                    else if (cliffs > 0)
                    {
                        //如果是悬崖,则是演示
                        terrain = 3;
                    }
                    else if (slopes > 0)
                    {
                        //缓坡,则是沙子
                        terrain = 0;
                    }
                    else
                    {
                        //否则还是草地
                        terrain = 1;
                    }
                }
                else if (cell.Elevation >= waterLevel)
                {
                    //细胞在水下但是高于水平面,是草地
                    terrain = 1;
                }
                else if (cell.Elevation < 0)
                {
                    //比地平线还低,则是岩石
                    terrain = 3;
                }
                else
                {
                    //最后则是水下泥土
                    terrain = 2;
                }

                //如果是草地,且温度过低,则退化成泥土
                if (terrain == 1 && temperature < temperatureBands[0])
                {
                    terrain = 2;
                }

                cell.TerrainTypeIndex = terrain;
            }

            cell.SetMapData(moisture);
        }
    }

    /// <summary>
    /// 创建河流
    /// </summary>
    private void CreateRivers()
    {
        var riverOrigins = ListPool<HexCell>.Get();
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = HexGrid.Instance.GetCell(i);
            if (cell.IsUnderwater)
            {
                continue;
            }

            ClimateData data = climate[i];
            float weight = data.moisture * (cell.Elevation - waterLevel)
                           / (elevationMaximum - waterLevel);
            //权重越高随机到的概率越高
            if (weight > 0.75f)
            {
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }

            if (weight > 0.5f)
            {
                riverOrigins.Add(cell);
            }

            if (weight > 0.25f)
            {
                riverOrigins.Add(cell);
            }
        }

        //生成河流
        int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);
        while (riverBudget > 0 && riverOrigins.Count > 0)
        {
            int index = Random.Range(0, riverOrigins.Count);
            int lastIndex = riverOrigins.Count - 1;
            HexCell origin = riverOrigins[index];
            //删除
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);

            if (!origin.HasRiver)
            {
                bool isValidOrign = true;
                //避免河流挤在一起
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = origin.GetNeighbor(d);
                    if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater))
                    {
                        isValidOrign = false;
                        break;
                    }
                }

                if (isValidOrign)
                {
                    riverBudget -= CreateRiver(origin);
                }
            }
        }

        if (riverBudget > 0)
        {
            Debug.Log("河流的方块不够");
        }

        ListPool<HexCell>.Add(riverOrigins);
    }

    /// <summary>
    /// 创建河流
    /// </summary>
    private int CreateRiver(HexCell origin)
    {
        int length = 1;
        HexDirection direction = HexDirection.NE;
        HexCell cell = origin;
        while (!cell.IsUnderwater)
        {
            int minNeighborElevation = int.MaxValue;
            flowDirections.Clear();
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (!neighbor)
                {
                    continue;
                }

                if (neighbor.Elevation < minNeighborElevation)
                {
                    minNeighborElevation = neighbor.Elevation;
                }

                if (neighbor == origin || neighbor.HasIncomingRiver)
                {
                    continue;
                }

                int delta = neighbor.Elevation - cell.Elevation;
                if (delta > 0)
                {
                    continue;
                }

                //头尾河流合并
                if (neighbor.HasOutgoingRiver)
                {
                    cell.SetOutgoingRiver(d);
                    return length;
                }

                if (delta < 0)
                {
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                }

                if (length == 1 || (d != direction.Next2() && d != direction.Previous2()))
                {
                    flowDirections.Add(d);
                }

                flowDirections.Add(d);
            }


            //如果周围都是水
            if (flowDirections.Count == 0)
            {
                if (length == 1)
                {
                    return 0;
                }

                //形成湖泊
                if (minNeighborElevation >= cell.Elevation
                    && Random.value < extraLakeProbability)
                {
                    cell.WaterLevel = minNeighborElevation;
                    if (minNeighborElevation == cell.Elevation)
                    {
                        cell.Elevation = minNeighborElevation - 1;
                    }
                }

                break;
            }


            direction = flowDirections[Random.Range(0, flowDirections.Count - 1)];

            cell.SetOutgoingRiver(direction);
            length += 1;

            //要么自己单独形成湖泊
            if (minNeighborElevation >= cell.Elevation)
            {
                cell.WaterLevel = cell.Elevation;
                cell.Elevation -= 1;
            }

            cell = cell.GetNeighbor(direction);
        }

        return length;
    }

    /// <summary>
    /// 创建经纬度温度
    /// </summary>
    private float DetermineTemperature(HexCell cell)
    {
        float latitude = (float) cell.coordinates.Z / HexGrid.Instance.cellCountZ;

        //半球
        if (hemisphere == HemisphereMode.Both)
        {
            latitude *= 2f;
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }
        }
        else if (hemisphere == HemisphereMode.North)
        {
            latitude = 1f - latitude;
        }

        //最高最低的温度
        float temperature = Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

        //高度温度
        temperature *= 1f - (cell.ViewElevation - waterLevel) /
                       (elevationMaximum - waterLevel + 1f);

        //随机温度波动
        float jitter = HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];

        //正负温度波动
        temperature += (jitter * 2f - 1f) * temperatureJitter;

        return temperature;
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