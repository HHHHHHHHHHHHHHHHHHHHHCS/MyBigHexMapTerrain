using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 大块地形 由N个hexcell 组成
/// </summary>
public class HexGrid : MonoBehaviour
{
    /// <summary>
    /// 地形大块  有几个地形小块
    /// </summary>
    public int chunkCountX = 4, chunkCountZ = 3;
    public HexCell cellPrefab;
    public HexGridChunk chunkPrefab;
    public Text cellLabelPrefab;
    public Color defaultColor = Color.white;
    public Color touchedColor = Color.red;
    public Texture2D noiseSource;

    private HexCell[] cells;
    private HexGridChunk[] chunks;
    private int cellCountX, cellCountZ;

    private void Awake()
    {
        HexMetrics.noiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    private void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }


    private void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for(int z = 0,i=0;z<chunkCountZ;z++)
        {
            for(int x=0;x<chunkCountX;x++)
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
        cell.Color = defaultColor;

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
        label.text = cell.coordinates.ToStringOnSeparateLines();
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x,int z ,HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell (Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        //Debug.Log("touched at:" + coordinates);//坐标
        return cells[index];
    }

    public void ColorCell(Vector3 position, Color color)
    {
        HexCell cell =GetCell(position);
        cell.Color = color;
    }

}
