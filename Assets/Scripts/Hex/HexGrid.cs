using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地形的manager
/// 有全部的HexGridChunk 和 HexCell
/// </summary>
public class HexGrid : MonoBehaviour
{
    public static HexGrid Instance { get; private set; }

    public Texture2D noiseSource;
    public int cellCountX = 20, cellCountZ = 15; //一共有几个六边形
    public bool wrapping; //是否循环地形
    public HexGridChunk chunkPrefab;
    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    public HexUnit unitPrefab;
    public Color searchFromColor = Color.blue, searchToColor = Color.red, searchPathColor = Color.white;


    private HexCell[] cells;
    private int chunkCountX, chunkCountZ; //有几个地形块
    private HexGridChunk[] chunks;
    private bool currentPathExists;
    private HexCell currentPathFrom, currentPathTo;
    private List<HexUnit> units = new List<HexUnit>();
    private HexCellShaderData cellShaderData;
    private Transform[] columns; //一共有几个列
    private int currentCenterColumnIndex = -1; //当前居中的列

    //private static WaitForSeconds delay = new WaitForSeconds(1 / 60f);
    //private Coroutine coroutine;

    private HexCellPriorityQueue searchFrontier;
    private int searchFrontierPhase;
    public int seed;

    public bool HasPath => currentPathExists;

    private void Awake()
    {
        Init();
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        CreateMap(cellCountX, cellCountZ, wrapping);
    }

    /// <summary>
    /// 重新编译的时候用
    /// </summary>
    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            Init();
            ResetVisibility();
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Init()
    {
        Instance = this;
        HexUnit.unitPrefab = unitPrefab;
        HexMetrics.wrapSize = wrapping ? cellCountX : 0;
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
    }

    /// <summary>
    /// 创建地图
    /// </summary>
    /// <param name="x">地图横有几个Cell</param>
    /// <param name="z">地图竖有几个Cell</param>
    /// <param name="isWrapping">是否循环</param>
    /// <returns></returns>
    public bool CreateMap(int x, int z, bool isWrapping)
    {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0
                   || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
        {
            Debug.Log("输入的cell count 不能被整除或者小于等于0");
            return false;
        }

        ClearMap();
        if (columns != null)
        {
            foreach (var item in columns)
            {
                Destroy(item.gameObject);
            }
        }


        cellCountX = x;
        cellCountZ = z;
        wrapping = isWrapping;
        currentCenterColumnIndex = -1;
        HexMetrics.wrapSize = wrapping ? cellCountX : 0;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
        cellShaderData.Initialize(cellCountX, cellCountZ);
        CreateChunks();
        CreateCells();
        return true;
    }

    /// <summary>
    /// 创建地图块
    /// </summary>
    private void CreateChunks()
    {
        columns = new Transform[chunkCountX];
        for (int x = 0; x < chunkCountX; x++)
        {
            columns[x] = new GameObject("Column").transform;
            columns[x].SetParent(transform, false);
        }


        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        for (var x = 0; x < chunkCountX; x++)
        {
            var chunk = chunks[i++] = Instantiate(chunkPrefab);
            chunk.transform.SetParent(columns[x]);
        }
    }

    /// <summary>
    /// 创建细胞
    /// </summary>
    private void CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        for (var x = 0; x < cellCountX; x++)
            CreateCell(x, z, i++);
    }

    /// <summary>
    /// 创建一个细胞
    /// </summary>
    private void CreateCell(int x, int z, int i)
    {
        var position = new Vector3
        {
            x = (x + z % 2 * 0.5f) * (HexMetrics.innerDiameter),
            y = 0f,
            z = z * (HexMetrics.outerRadius * 1.5f)
        };

        var cell = cells[i] = Instantiate(cellPrefab);

        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Index = i;
        cell.ShaderData = cellShaderData;
        cell.Exploration = x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
            if(wrapping && x == cellCountX - 1)
            {//循环收尾相接
                cell.SetNeighbor(HexDirection.E,cells[i - x]);
            }
        }

        if (z > 0)
        {
            if ((z & 1) == 0)
            {//奇偶判断
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
                else if (wrapping)
                {//循环收尾相接
                    cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);

                }
                else if (wrapping)
                {//循环收尾相接
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX * 2 + 1]);
                }
            }
        }

        //坐标的显示
        var label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition
            = new Vector2(position.x, position.z);
        //label.text = cell.coordinates.ToStringOnSeparateLines();
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    /// <summary>
    /// 添加细胞到地图块
    /// </summary>
    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        var chunkX = x / HexMetrics.chunkSizeX;
        var chunkZ = z / HexMetrics.chunkSizeZ;
        var chunk = chunks[chunkX + chunkZ * chunkCountX];

        var localX = x - chunkX * HexMetrics.chunkSizeX;
        var localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    /// <summary>
    /// 得到细胞,根据cell 坐标数据
    /// </summary>
    public HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;
        if (z < 0 || z >= cellCountZ) return null;

        var x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX) return null;

        return cells[x + z * cellCountX];
    }

    /// <summary>
    /// 得到细胞,根据位置
    /// </summary>
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        //这里是循环坐标,所以用重新计算coordinates,
        var coordinates = HexCoordinates.FromPosition(position);
        return GetCell(coordinates);
    }

    /// <summary>
    /// 得到细胞根据射线
    /// </summary>
    public HexCell GetCell(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return GetCell(hit.point);
        }

        return null;
    }

    /// <summary>
    /// 得到细胞,根据横竖轴
    /// </summary>
    public HexCell GetCell(int xOffset, int zOffset)
    {
        return cells[xOffset + zOffset * cellCountX];
    }

    /// <summary>
    /// 得到细胞根据index
    /// </summary>
    public HexCell GetCell(int cellIndex)
    {
        return cells[cellIndex];
    }

    /// <summary>
    /// 显示UI
    /// </summary>
    public void ShowUI(bool visible)
    {
        foreach (var item in chunks)
        {
            item.ShowUI(visible);
        }
    }

    /// <summary>
    /// 重置视野
    /// </summary>
    public void ResetVisibility()
    {
        foreach (var item in cells)
        {
            item.ResetVisibility();
        }

        foreach (var item in units)
        {
            IncreaseVisibility(item.Location, item.VisionRange);
        }
    }

    #region SaveLoad

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="writer"></param>
    public void Save(MyWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        writer.Write(wrapping);
        foreach (var item in cells)
        {
            item.Save(writer);
        }

        writer.Write(units.Count);
        foreach (var item in units)
        {
            item.Save(writer);
        }
    }

    /// <summary>
    /// 读取
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="header"></param>
    public void Load(MyReader reader, int header)
    {
        //if (coroutine != null)
        //{
        //    StopCoroutine(coroutine);
        //}
        ClearMap();
        int x = 20, z = 15;
        if (header >= SaveLoadModule.version_1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        bool isWrapping = header >= SaveLoadModule.version_5 && reader.ReadBoolean();

        if (x != cellCountX || z != cellCountZ || wrapping != isWrapping)
        {
            if (!CreateMap(x, z, isWrapping))
            {
                return;
            }
        }


        var originalImmediateMode = cellShaderData.ImmediateMode;
        cellShaderData.ImmediateMode = true;
        foreach (var item in cells)
        {
            item.Load(reader, header);
        }

        foreach (var item in cells)
        {
            item.RefreshPosition();
            item.Refresh();
        }

        if (header >= SaveLoadModule.version_2)
        {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }

        cellShaderData.ImmediateMode = originalImmediateMode;
    }

    /// <summary>
    /// 清除地图上的数据
    /// </summary>
    public void ClearMap()
    {
        ClearPath();
        ClearUnits();
    }

    #endregion

    #region A* Search Path

    /// <summary>
    /// 寻路
    /// </summary>
    /// <param name="fromCell">来的cell</param>
    /// <param name="toCell">要去的cell</param>
    /// <param name="unit">要移动的单位</param>
    public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        //if (coroutine != null)
        //{
        //    StopCoroutine(coroutine);
        //}

        //coroutine = StartCoroutine(Search(fromCell, toCell, speed));

        //Stopwatch sw = new Stopwatch();
        //sw.Start();
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, unit);
        ShowPath(unit.Speed);
        //sw.Stop();
        //Debug.Log(sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// 搜索
    /// </summary>
    /// <param name="fromCell">来的cell</param>
    /// <param name="toCell">要去的cell</param>
    /// <param name="unit">要移动的单位</param>
    /// <returns>是否可以寻路</returns>
    private /*IEnumerator*/ bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        int speed = unit.Speed;
        searchFrontierPhase += 2;
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

        /*foreach (var nowCell in cells)
        {
        if (nowCell != fromCell && nowCell != toCell)
        {
        nowCell.DisableHighlight();
        }
    
        nowCell.SetLabel(null);
        }*/

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        while (searchFrontier.Count > 0)
        {
            //yield return delay;
            var current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell) return true;

            var currentTurn = (current.Distance - 1) / speed;

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = current.GetNeighbor(d);
                if (neighbor == null
                    || neighbor.SearchPhase > searchFrontierPhase)
                {
                    continue;
                }

                if (!unit.IsValidDestination(neighbor))
                {
                    continue;
                }

                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                {
                    continue;
                }

                var distance = current.Distance + moveCost;
                var turn = (distance - 1) / speed;
                if (turn > currentTurn) distance = turn * speed + moveCost;

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates
                        .DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (neighbor.Distance > distance)
                {
                    var oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        //coroutine = null;
        return false;
    }

    /// <summary>
    /// 显示路径
    /// </summary>
    /// <param name="speed"></param>
    private void ShowPath(int speed)
    {
        if (currentPathExists)
        {
            var current = currentPathTo;
            while (current != currentPathFrom)
            {
                var turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(searchPathColor);
                current = current.PathFrom;
            }

            currentPathFrom.EnableHighlight(searchFromColor);
            currentPathTo.EnableHighlight(searchToColor);
        }
    }

    /// <summary>
    /// 清除路径
    /// </summary>
    public void ClearPath()
    {
        if (currentPathExists)
        {
            var current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }

            current.DisableHighlight();
            currentPathExists = false;
        }

        currentPathFrom = currentPathTo = null;
    }

    /// <summary>
    /// 得到路径
    /// </summary>
    /// <returns></returns>
    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }

        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }

        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }

    #endregion

    #region Units

    /// <summary>
    /// 清除单位
    /// </summary>
    private void ClearUnits()
    {
        foreach (var item in units)
        {
            item.Die();
        }

        units.Clear();
    }

    /// <summary>
    /// 添加单位
    /// </summary>
    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    /// <summary>
    /// 移除单位
    /// </summary>
    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }

    /// <summary>
    /// 得到可见的视野
    /// </summary>
    private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        range += fromCell.ViewElevation;
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        var fromCoordinates = fromCell.coordinates;
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = current.GetNeighbor(d);
                if (!neighbor || neighbor.SearchPhase > searchFrontierPhase
                              || !neighbor.Exploration)
                {
                    continue;
                }

                int distance = current.Distance + 1;
                if (distance + neighbor.ViewElevation > range
                    || distance > fromCoordinates.DistanceTo(neighbor.coordinates))
                {
                    continue;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return visibleCells;
    }

    /// <summary>
    /// 增加视野
    /// </summary>
    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        var cells = GetVisibleCells(fromCell, range);
        foreach (var item in cells)
        {
            item.IncreaseVisibility();
        }

        ListPool<HexCell>.Add(cells);
    }

    /// <summary>
    /// 减少视野
    /// </summary>
    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        var cells = GetVisibleCells(fromCell, range);
        foreach (var item in cells)
        {
            item.DecreaseVisibility();
        }

        ListPool<HexCell>.Add(cells);
    }

    #endregion

    #region Wrapping

    /// <summary>
    /// 居中地图
    /// </summary>
    public void CenterMap(float xPosition)
    {
        var chunkSizeX = (HexMetrics.innerDiameter * HexMetrics.chunkSizeX);

        int centerColumnIndex = (int) (xPosition / chunkSizeX);

        if (centerColumnIndex == currentCenterColumnIndex)
        {
            return;
        }

        currentCenterColumnIndex = centerColumnIndex;

        int halfColumn = chunkCountX / 2;
        int minColumnIndex = centerColumnIndex - halfColumn;
        int maxColumnIndex = centerColumnIndex + halfColumn;

        Vector3 position;
        position.y = position.z = 0f;

        for (int i = 0; i < columns.Length; i++)
        {
            position.x = 0f;

            //刚开始整个地图的中心点是0
            //往左边,则直接为左边大块的中心点,右同理
            if (i < minColumnIndex)
            {
                position.x = chunkCountX * chunkSizeX;
            }
            else if (i > maxColumnIndex)
            {
                position.x = chunkCountX * -chunkSizeX;
            }
            else
            {
                position.x = 0f;
            }

            columns[i].localPosition = position;
        }
    }

    #endregion
}