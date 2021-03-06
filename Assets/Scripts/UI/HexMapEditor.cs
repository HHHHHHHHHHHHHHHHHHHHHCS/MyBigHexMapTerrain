﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 地形的编辑器
/// </summary>
public class HexMapEditor : MonoBehaviour
{
    private enum OptionalToggle
    {
        Ignore,
        Yes,
        No
    }

    public Material terrainMaterial;

    public HexMapEditor Instance { private set; get; }

    private HexGrid hexGrid;
    private Camera mainCam;

    private int activeTerrainTypeIndex;
    private int activeElevation;
    private int activeWaterLevel;
    private int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialLevel;
    private int brushSize;
    private bool applyColor;
    private bool applyElevation = true;
    private bool applyWaterLevel = true;
    private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialLevel;
    private OptionalToggle riverMode = OptionalToggle.Ignore;
    private OptionalToggle roadMode = OptionalToggle.Ignore;
    private OptionalToggle walledMode = OptionalToggle.Ignore;

    private bool isDrag;
    private HexDirection dragDirection;

    private HexCell previousCell;
    //private HexCell  searchFromCell, searchToCell;

    private GameObject leftEditorBg, rightEditorBg;
    private NewMapUI newMapUI;
    private SaveLoadUI saveLoadUI;
    private Toggle editModeToggle;

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();

        #region Get Init Component

        MyU.GetCom(out editModeToggle, "Toggle_EditMode", transform);

        MyU.GetGo(out leftEditorBg, "Editor_LeftBg", transform);
        MyU.BeginParent(leftEditorBg);
        MyU.GetCom(out ToggleGroup colorToggleGroup, "ToggleGroup_Color");
        MyU.GetCom(out Toggle elevationToggle, "Toggle_Elevation");
        MyU.GetCom(out Slider elevationSlider, "Slider_Elevation");
        MyU.GetCom(out Toggle waterToggle, "Toggle_Water");
        MyU.GetCom(out Slider waterSlider, "Slider_Water");
        MyU.GetCom(out ToggleGroup riverToggleGroup, "ToggleGroup_River");
        MyU.GetCom(out ToggleGroup roadToggleGroup, "ToggleGroup_Road");
        MyU.GetCom(out Slider brushSlider, "Slider_BrustSize");
        MyU.GetCom(out Toggle gridToggle, "Toggle_Grid");

        MyU.GetGo(out rightEditorBg, "Editor_RightBg", transform);
        MyU.BeginParent(rightEditorBg);
        MyU.GetCom(out Toggle urbanToggle, "Toggle_Urban");
        MyU.GetCom(out Slider urbanSlider, "Slider_Urban");
        MyU.GetCom(out Toggle farmToggle, "Toggle_Farm");
        MyU.GetCom(out Slider farmSlider, "Slider_Farm");
        MyU.GetCom(out Toggle plantToggle, "Toggle_Plant");
        MyU.GetCom(out Slider plantSlider, "Slider_Plant");
        MyU.GetCom(out Toggle specialToggle, "Toggle_Special");
        MyU.GetCom(out Slider specialSlider, "Slider_Special");
        MyU.GetCom(out ToggleGroup walledToggleGroup, "ToggleGroup_Walled");
        MyU.GetCom(out Transform fileBg, "Bg_File");
        MyU.GetCom(out saveLoadUI, fileBg);
        MyU.GetCom(out newMapUI, fileBg);


        var colorToggles = colorToggleGroup.GetComponentsInChildren<Toggle>();
        var riverToggles = riverToggleGroup.GetComponentsInChildren<Toggle>();
        var roadToggles = roadToggleGroup.GetComponentsInChildren<Toggle>();
        var walledToggles = walledToggleGroup.GetComponentsInChildren<Toggle>();

        MyU.AddValChange(plantSlider, val => activePlantLevel = (int) val);
        MyU.AddValChange(editModeToggle, SetEditMode);

        MyU.AddValChange(elevationToggle, bo => applyElevation = bo);
        MyU.AddValChange(elevationSlider, val => activeElevation = (int) val);
        MyU.AddValChange(waterToggle, bo => applyWaterLevel = bo);
        MyU.AddValChange(waterSlider, val => activeWaterLevel = (int) val);
        MyU.AddValChange(brushSlider, val => brushSize = (int) val);
        MyU.AddValChange(gridToggle, ShowGrid);

        MyU.AddValChange(urbanToggle, bo => applyUrbanLevel = bo);
        MyU.AddValChange(urbanSlider, val => activeUrbanLevel = (int) val);
        MyU.AddValChange(farmToggle, bo => applyFarmLevel = bo);
        MyU.AddValChange(farmSlider, val => activeFarmLevel = (int) val);
        MyU.AddValChange(specialToggle, bo => applySpecialLevel = bo);
        MyU.AddValChange(specialSlider, val => activeSpecialLevel = (int) val);
        MyU.AddValChange(plantToggle, bo => applyPlantLevel = bo);

        InitToggles(colorToggles, SetColor);
        InitToggles(riverToggles, SetRiverMode);
        InitToggles(roadToggles, SetRoadMode);
        InitToggles(walledToggles, SetWalledMode);

        #endregion

        saveLoadUI.Init(hexGrid);
        newMapUI.Init(hexGrid);
    }

    private void Start()
    {
        SetEditMode(editModeToggle.isOn);

        editModeToggle.isOn = true;
        SetEditMode(true);
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButton(0))
            {
                HandleInput();
                return;
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                CreateUnit();
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift)
                && Input.GetKeyDown(KeyCode.D))
            {
                DestroyUnit();
                return;
            }
        }

        if (previousCell)
        {
            previousCell = null;
        }
    }


    private HexCell GetCellUnderCursor()
    {
        return hexGrid.GetCell(
            mainCam.ScreenPointToRay(Input.mousePosition));
    }

    private void HandleInput()
    {
        HexCell currentCell = GetCellUnderCursor();
        if (currentCell)
        {
            if (previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }

            EditCells(currentCell);


            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }

    private void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell cell)
    {
        if (!cell) return;
        if (applyColor)
        {
            cell.TerrainTypeIndex = activeTerrainTypeIndex;
        }

        if (applyElevation)
        {
            cell.Elevation = activeElevation;
        }

        if (applyWaterLevel)
        {
            cell.WaterLevel = activeWaterLevel;
        }

        if (applyUrbanLevel)
        {
            cell.UrbanLevel = activeUrbanLevel;
        }

        if (applyFarmLevel)
        {
            cell.FarmLevel = activeFarmLevel;
        }

        if (applyPlantLevel)
        {
            cell.PlantLevel = activePlantLevel;
        }

        if (riverMode == OptionalToggle.No)
        {
            cell.RemoveRiver();
        }

        if (roadMode == OptionalToggle.No)
        {
            cell.RemoveRoads();
        }

        if (walledMode != OptionalToggle.Ignore)
        {
            cell.Walled = walledMode == OptionalToggle.Yes;
        }

        if (applySpecialLevel)
        {
            cell.SpecialIndex = activeSpecialLevel;
        }

        if (isDrag)
        {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell)
            {
                if (riverMode == OptionalToggle.Yes)
                {
                    previousCell.SetOutgoingRiver(dragDirection);
                }

                if (roadMode == OptionalToggle.Yes)
                {
                    otherCell.AddRoad(dragDirection);
                }
            }
        }
    }

    public void InitToggles(Toggle[] toogles, Action<int> clickAction)
    {
        for (int i = 0; i < toogles.Length; i++)
        {
            int temp = i;
            toogles[i].isOn = false;
            toogles[i].onValueChanged.AddListener(bo =>
            {
                if (bo)
                {
                    clickAction(temp);
                }
            });
        }

        toogles[0].isOn = true;
        clickAction(0);
    }

    public void SetColor(int index)
    {
        applyColor = index > 0;
        if (!applyColor)
        {
            return;
        }

        activeTerrainTypeIndex = index - 1;
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle) mode;
    }

    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle) mode;
    }

    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle) mode;
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++)
        {
            if (previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }

        isDrag = false;
    }


    /// <summary>
    /// 显示格子
    /// </summary>
    /// <param name="visible"></param>
    public void ShowGrid(bool visible)
    {
        const string key = "GRID_ON";
        if (visible)
        {
            terrainMaterial.EnableKeyword(key);
        }
        else
        {
            terrainMaterial.DisableKeyword(key);
        }
    }

    public void SetEditMode(bool val)
    {
        enabled = val;
        hexGrid.ShowUI(!val);
        hexGrid.ClearPath();
        if (val)
        {
            Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        }
        else
        {
            Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
        }
    }

    private void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            hexGrid.AddUnit(
                Instantiate(HexUnit.unitPrefab), cell, Random.Range(0, 360f));
        }
    }

    private void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }
}