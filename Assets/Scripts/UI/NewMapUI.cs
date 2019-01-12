using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新地图的UI
/// </summary>
public class NewMapUI : MonoBehaviour
{
    private Transform createNewMapBg;
    private HexGrid hexGrid;
    private bool isGenerate = true;
    private bool isWrapping = true;

    public void Init(HexGrid _hexGrid)
    {
        hexGrid = _hexGrid;

        MyU.GetCom(out Button newMapButton, "Button_New", transform);
        MyU.GetCom(out createNewMapBg, "Bg_CreateNewMap", transform);
        MyU.GetCom(out Toggle generateToggle, "Toggle_Generate", createNewMapBg);
        MyU.GetCom(out Toggle wrappingToggle, "Toggle_Wrapping", createNewMapBg);
        MyU.GetCom(out Button smallButton, "Button_Small", createNewMapBg);
        MyU.GetCom(out Button mediumButton, "Button_Medium", createNewMapBg);
        MyU.GetCom(out Button largeButton, "Button_Large", createNewMapBg);
        MyU.GetCom(out Button cancelButton, "Button_Cancel", createNewMapBg);

        MyU.AddValChange(generateToggle, val => isGenerate = val);
        MyU.AddValChange(wrappingToggle, val => isWrapping = val);
        MyU.AddClick(newMapButton, ShowHideCreateNewMapBg, true);
        MyU.AddClick(cancelButton, ShowHideCreateNewMapBg, false);
        MyU.AddClick(smallButton, CreateNewMap, 0);
        MyU.AddClick(mediumButton, CreateNewMap, 1);
        MyU.AddClick(largeButton, CreateNewMap, 2);
    }

    /// <summary>
    /// 显示隐藏当前的UI
    /// </summary>
    public void ShowHideCreateNewMapBg(bool isShow)
    {
        createNewMapBg.gameObject.SetActive(isShow);
        HexMapCamera.Instance.Locked = isShow;
    }

    /// <summary>
    /// 创建新的地图
    /// </summary>
    public void CreateNewMap(int size)
    {
        int x, z;
        switch (size)
        {
            case 0:
                x = 20;
                z = 15;
                break;
            case 1:
                x = 40;
                z = 30;
                break;
            case 2:
                x = 80;
                z = 60;
                break;
            default:
                x = hexGrid.cellCountX;
                z = hexGrid.cellCountZ;
                break;
        }

        if (isGenerate)
        {
            HexMapGenerator.Instance.GenerateMap(x, z,isWrapping);
        }
        else
        {
            hexGrid.CreateMap(x, z, isWrapping);
        }

        HexMapCamera.Instance.ValidatePosition();
        ShowHideCreateNewMapBg(false);
    }
}