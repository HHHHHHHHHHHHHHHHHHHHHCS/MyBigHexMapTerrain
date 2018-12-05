using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewMapUI : MonoBehaviour
{
    private Transform createNewMapBg;
    private HexGrid hexGrid;

    public void Init(HexGrid _hexGrid)
    {
        hexGrid = _hexGrid;

        MyU.BeginParent(transform);
        MyU.GetCom(out Button newMapButton, "");
        MyU.GetCom(out createNewMapBg, "Bg_CreateNewMap");
        MyU.GetCom(out Button samallButton, "Button_Small", createNewMapBg);
        MyU.GetCom(out Button mediumButton, "Button_Medium", createNewMapBg);
        MyU.GetCom(out Button largeButton, "Button_Large", createNewMapBg);
        MyU.GetCom(out Button cancelButton, "Button_Cancel", createNewMapBg);

        newMapButton.onClick.AddListener(() => ShowHideCreateNewMapBg(true));
        cancelButton.onClick.AddListener(() => ShowHideCreateNewMapBg(false));
        samallButton.onClick.AddListener(() => CreateNewMap(0));
        mediumButton.onClick.AddListener(() => CreateNewMap(1));
        largeButton.onClick.AddListener(() => CreateNewMap(2));
    }


    public void ShowHideCreateNewMapBg(bool isShow)
    {
        createNewMapBg.gameObject.SetActive(isShow);
        HexMapCamera.Instance.Locked = isShow;
    }

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

        hexGrid.CreateMap(x, z);
        HexMapCamera.Instance.ValidatePosition();
        ShowHideCreateNewMapBg(false);
    }
}