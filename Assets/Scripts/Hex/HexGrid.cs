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
        CreateMap(cellCountX, cellCountZ);
    }

    /// <summary>
    /// 重新编译的时候用
    /// </summary>
    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            Init();
        }
    }

    private void Init()
    {
        Instance = this;
        HexUnit.unitPrefab = unitPrefab;
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
    }

    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0
                   || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
        {
            Debug.Log("输入的cell count 不能被整除或者小于等于0");
            return false;
        }

        ClearMap();
        if (chunks != null)
        {
            foreach (var item in chunks)
            {
                Destroy(item.gameObject);
            }
        }


        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
        cellShaderData.Initialize(cellCountX, cellCountZ);
        CreateChunks();
        CreateCells();
        return true;
    }


    private void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        for (var x = 0; x < chunkCountX; x++)
        {
            var chunk = chunks[i++] = Instantiate(chunkPrefab);
            chunk.transform.SetParent(transform);
        }
    }

    private void CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        for (var x = 0; x < cellCountX; x++)
            CreateCell(x, z, i++);
    }

    private void CreateCell(int x, int z, int i)
    {
        var position = new Vector3
        {
            x = (x + z % 2 * 0.5f) * (HexMetrics.innerRadius * 2f),
            y = 0f,
            z = z * (HexMetrics.outerRadius * 1.5f)
        };

        var cell = cells[i] = Instantiate(cellPrefab);

        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Index = i;
        cell.ShaderData = cellShaderData;

        if (x > 0) cell.SetNeighbor(HexDirection.W, cells[i - 1]);

        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0) cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1) cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
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

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        var chunkX = x / HexMetrics.chunkSizeX;
        var chunkZ = z / HexMetrics.chunkSizeZ;
        var chunk = chunks[chunkX + chunkZ * chunkCountX];

        var localX = x - chunkX * HexMetrics.chunkSizeX;
        var localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;
        if (z < 0 || z >= cellCountZ) return null;

        var x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX) return null;

        return cells[x + z * cellCountX];
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        var coordinates = HexCoordinates.FromPosition(position);
        var index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        //Debug.Log("touched at:" + coordinates);//坐标
        return cells[index];
    }


    public HexCell GetCell(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return GetCell(hit.point);
        }

        return null;
    }

    public void ShowUI(bool visible)
    {
        for (var i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    #region SaveLoad

    public void Save(MyWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
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

        if (x != cellCountX || z != cellCountZ)
            if (!CreateMap(x, z))
                return;

        foreach (var item in cells)
        {
            item.Load(reader,header);
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
    }

    public void ClearMap()
    {
        ClearPath();
        ClearUnits();
    }

    #endregion

    #region A* Search Path

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
                    var oldPriority = neighbor.SearchPrioty;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        //coroutine = null;
        return false;
    }

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

    private void ClearUnits()
    {
        foreach (var item in units)
        {
            item.Die();
        }

        units.Clear();
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }

    /// <summary>
    /// 得到可见的视野
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    /// <returns></returns>
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

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = current.GetNeighbor(d);
                if (!neighbor || neighbor.SearchPhase > searchFrontierPhase)
                {
                    continue;
                }

                int distance = current.Distance + 1;
                if (distance > range)
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
                    int oldPriority = neighbor.SearchPrioty;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return visibleCells;
    }


    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        var cells = GetVisibleCells(fromCell, range);
        foreach (var item in cells)
        {
            item.IncreaseVisibility();
        }

        ListPool<HexCell>.Add(cells);
    }

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
}