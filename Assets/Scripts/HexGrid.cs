using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public int width = 6;
    public int height = 6;

    public HexCell cellPrefab;

    private HexCell[] cells;

    private void Awake()
    {
        cells = new HexCell[height * width];

        for(int z = 0,i=0;z<height;z++)
        {
            for(int x = 0;x<width;x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x,int z,int i)
    {
        Vector3 position = new Vector3
        {
            x = x * 10f,
            y = 0f,
            z = z * 10f
        };

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
    }
}
