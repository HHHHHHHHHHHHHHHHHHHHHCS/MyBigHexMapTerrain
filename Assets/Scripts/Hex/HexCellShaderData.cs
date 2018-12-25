using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexCellShaderData : MonoBehaviour
{
    private const float transitionSpeed = 255f; //视野转换的时间

    /// <summary>
    /// 视野是否立即显示隐藏模式
    /// </summary>
    public bool ImmediateMode { get; set; }

    private Texture2D cellTexture; //图片
    private Color32[] cellTextureData; //图片的数据
    private List<HexCell> transitioningCells = new List<HexCell>(); //全部的cell

    private bool needsVisibilityReset; //视野是否改变

    public void Initialize(int x, int z)
    {
        if (cellTexture)
        {
            cellTexture.Resize(x, z);
        }
        else
        {
            cellTexture = new Texture2D(
                x, z, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }

        Shader.SetGlobalVector("_HexCellData_TexcelSize"
            , new Vector4(1 / x, 1 / z, x, z));


        if (cellTextureData == null || cellTextureData.Length != x * z)
        {
            cellTextureData = new Color32[x * z];
        }
        else
        {
            for (int i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        transitioningCells.Clear();
        enabled = true;
    }

    private void LateUpdate()
    {
        if (needsVisibilityReset)
        {
            needsVisibilityReset = false;
            HexGrid.Instance.ResetVisibility();
        }

        int delta = (int) (Time.deltaTime * transitionSpeed);
        if (delta == 0)
        {
            delta = 1;
        }

        for (int i = 0; i < transitioningCells.Count; i++)
        {
            if (!UpdateCellData(transitioningCells[i], delta))
            {
                //不删除当前索引是因为删除当前的会移动全部后面的
                //所以不如直接换成最后的,删除最后的
                transitioningCells[i--] =
                    transitioningCells[transitioningCells.Count - 1];
                transitioningCells.RemoveAt(transitioningCells.Count - 1);
            }
        }

        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        enabled = transitioningCells.Count > 0;
    }

    public void RefreshTerrain(HexCell cell)
    {
        cellTextureData[cell.Index].a = (byte) cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility(HexCell cell)
    {
        int index = cell.Index;
        if (ImmediateMode)
        {
            cellTextureData[index].r = cell.IsVisible ? (byte) 255 : (byte) 0;
            cellTextureData[index].g = cell.IsExplored ? (byte) 255 : (byte) 0;
        }
        else if(cellTextureData[index].b!=255)
        {
            cellTextureData[index].b = 255;
            transitioningCells.Add(cell);
        }

        enabled = true;
    }

    private bool UpdateCellData(HexCell cell, int delta)
    {
        int index = cell.Index;
        Color32 data = cellTextureData[index];
        bool stillUpdating = false;

        if (cell.IsExplored && data.g < 255)
        {
            stillUpdating = true;
            int t = data.g + delta;
            data.g = t >= 255 ? (byte) 255 : (byte) t;
        }


        if (cell.IsVisible)
        {
            //渐变变亮
            if (data.r < 255)
            {
                stillUpdating = true;
                int t = data.r + delta;
                data.r = t >= 255 ? (byte) 255 : (byte) t;
            }
        }
        else if (data.r > 0)
        {
            //渐变变黑
            stillUpdating = true;
            int t = data.r - delta;
            data.r = t < 0 ? (byte) 0 : (byte) t;
        }

        if (!stillUpdating)
        {
            data.b = 0;
        }
        cellTextureData[index] = data;
        return stillUpdating;
    }

    /// <summary>
    /// 高度视野改变
    /// </summary>
    public void ViewElevationChanged()
    {
        needsVisibilityReset = true;
        enabled = true;
    }
}