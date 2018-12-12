using System.Collections;
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
    private static WaitForSeconds delay = new WaitForSeconds(1 / 60f);

    public Color searchFromColor = Color.blue
        , searchToColor = Color.red, searchPathColor = Color.white;
    public int cellCountX = 20, cellCountZ = 15; //一共有几个六边形
    public HexCell cellPrefab;
    public HexGridChunk chunkPrefab;
    public Text cellLabelPrefab;
    public Texture2D noiseSource;
    public int seed;

    private HexCell[] cells;
    private HexGridChunk[] chunks;
    private int chunkCountX, chunkCountZ; //有几个地形块
    private Coroutine coroutine;
    private HexCellPriorityQueue searchFrontier;

    private void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        CreateMap(cellCountX, cellCountZ);
    }

    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }

    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0
                   || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
        {
            Debug.Log("输入的cell count 不能被整除或者小于等于0");
            return false;
        }

        if (chunks != null)
        {
            foreach (var t in chunks)
            {
                Destroy(t.gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
        CreateChunks();
        CreateCells();
        return true;
    }


    private void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position = new Vector3
        {
            x = (x + z % 2 * 0.5f) * (HexMetrics.innerRadius * 2f),
            y = 0f,
            z = z * (HexMetrics.outerRadius * 1.5f)
        };

        HexCell cell = cells[i] = Instantiate(cellPrefab);

        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        //cell.Color = defaultColor;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }

        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        //坐标的显示
        Text label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition
            = new Vector2(position.x, position.z);
        //label.text = cell.coordinates.ToStringOnSeparateLines();
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }

        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }

        return cells[x + z * cellCountX];
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        //Debug.Log("touched at:" + coordinates);//坐标
        return cells[index];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        foreach (var item in cells)
        {
            item.Save(writer);
        }

        foreach (var item in cells)
        {
            item.Refresh();
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if (x != cellCountX || z != cellCountZ)
        {
            if (!CreateMap(x, z))
            {
                return;
            }
        }

        foreach (var item in cells)
        {
            item.Load(reader);
        }

        foreach (var item in cells)
        {
            item.RefreshPosition();
            item.Refresh();
        }
    }

    public void FindPath(HexCell fromCell, HexCell toCell)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        coroutine = StartCoroutine(Search(fromCell, toCell));
    }

    private IEnumerator Search(HexCell fromCell, HexCell toCell)
    {
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        foreach (var nowCell in cells)
        {
            nowCell.Distance = int.MaxValue;
            if (nowCell != fromCell && nowCell != toCell)
            {
                nowCell.DisableHighlight();
            }
        }

        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        while (searchFrontier.Count > 0)
        {
            yield return delay;
            var current = searchFrontier.Dequeue();

            if (current == toCell)
            {
                current = current.PathFrom;
                while (current != fromCell)
                {
                    current.EnableHighlight(searchPathColor);
                    current = current.PathFrom;
                }
                break;
            }

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = current.GetNeighbor(d);
                if (neighbor == null)
                {
                    continue;
                }

                if (neighbor.IsUnderwater)
                {
                    continue;
                }

                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }

                var distance = current.Distance;
                if (current.HasRoadThroughEdge(d))
                {
                    distance += 1;
                }
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    distance += edgeType == HexEdgeType.Flat ? 5 : 10;
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel
                                                    + neighbor.PlantLevel;
                }

                if (neighbor.Distance == int.MaxValue)
                {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates
                        .DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (neighbor.Distance > distance)
                {
                    int oldPriority = neighbor.SearchPrioty;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        coroutine = null;
    }
}