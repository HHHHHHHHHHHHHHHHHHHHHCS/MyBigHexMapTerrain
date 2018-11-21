using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 大块地形 由N个hexcell 组成
/// </summary>
public class HexGrid : MonoBehaviour
{
    public int width = 6;
    public int height = 6;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    public Color defaultColor = Color.white;
    public Color touchedColor = Color.red;
    public Texture2D noiseSource;

    private HexCell[] cells;
    private Canvas gridCanvas;
    private HexMesh hexMesh;

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();
        HexMetrics.noiseSource = noiseSource;

        cells = new HexCell[height * width];

        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }

    private void Start()
    {
        hexMesh.Triangulate(cells);
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
        cell.transform.SetParent(hexMesh.transform, false);
  
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                if (x < width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        //坐标的显示
        Text label = Instantiate(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition
            = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();
        cell.uiRect = label.rectTransform;

        cell.Elevation = 0;
    }

    public HexCell GetCell (Vector3 position)
    {
        position = hexMesh.transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        //Debug.Log("touched at:" + coordinates);//坐标
        return cells[index];
    }

    public void ColorCell(Vector3 position, Color color)
    {
        HexCell cell =GetCell(position);
        cell.color = color;
        Refresh();
    }

    public void Refresh()
    {
        hexMesh.Triangulate(cells);
    }
}
