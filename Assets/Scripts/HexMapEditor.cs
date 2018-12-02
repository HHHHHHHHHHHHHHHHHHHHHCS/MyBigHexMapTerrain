using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 地形的编辑器
/// </summary>
public class HexMapEditor : MonoBehaviour
{
    private enum OptionalToggle
    {
        Ignore, Yes, No
    }

    public Color[] colors;

    private HexGrid hexGrid;
    private Camera mainCam;
    private Transform root;

    private Color activeColor;
    private int activeElevation;
    private int activeWaterLevel;
    private int activeUrbanLevel, activeFarmLevel, activePlantLevel;
    private int brushSize;
    private bool applyColor;
    private bool applyElevation = true;
    private bool applyWaterLevel = true;
    private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel;
    private OptionalToggle riverMode = OptionalToggle.Ignore;
    private OptionalToggle roadMode = OptionalToggle.Ignore;
    private OptionalToggle walledMode = OptionalToggle.Ignore;


    private bool isDrag;
    private HexDirection dragDirection;
    private HexCell previousCell;

    private void Awake()
    {
        mainCam = Camera.main;
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();

        root = transform.Find("LeftBg");
        FindComponent(out ToggleGroup colorToggleGroup, "ToggleGroup_Color");
        FindComponent(out Toggle elevationToggle, "Toggle_Elevation");
        FindComponent(out Slider elevationSlider, "Slider_Elevation");
        FindComponent(out Toggle waterToggle, "Toggle_Water");
        FindComponent(out Slider waterSlider, "Slider_Water");
        FindComponent(out Slider brushSlider, "Slider_BrustSize");
        FindComponent(out Toggle labelsToggle, "Toggle_Labels");
        FindComponent(out ToggleGroup riverToggleGroup, "ToggleGroup_River");
        FindComponent(out ToggleGroup roadToggleGroup, "ToggleGroup_Road");

        root = transform.Find("RightBg");
        FindComponent(out Toggle urbanToggle, "Toggle_Urban");
        FindComponent(out Slider urbanSlider, "Slider_Urban");
        FindComponent(out Toggle farmToggle, "Toggle_Farm");
        FindComponent(out Slider farmSlider, "Slider_Farm");
        FindComponent(out Toggle plantToggle, "Toggle_Plant");
        FindComponent(out Slider plantSlider, "Slider_Plant");
        FindComponent(out ToggleGroup walledToggleGroup, "ToggleGroup_Walled");

        var colorToggles = colorToggleGroup.GetComponentsInChildren<Toggle>();
        var riverToggles = riverToggleGroup.GetComponentsInChildren<Toggle>();
        var roadToggles = roadToggleGroup.GetComponentsInChildren<Toggle>();
        var walledToggles = walledToggleGroup.GetComponentsInChildren<Toggle>();

        elevationToggle.onValueChanged.AddListener(bo => applyElevation = bo);
        elevationSlider.onValueChanged.AddListener(val => activeElevation = (int)val);
        waterToggle.onValueChanged.AddListener(bo => applyWaterLevel = bo);
        waterSlider.onValueChanged.AddListener(val => activeWaterLevel = (int)val);
        brushSlider.onValueChanged.AddListener(val => brushSize = (int)val);
        labelsToggle.onValueChanged.AddListener(ShowUI);

        urbanToggle.onValueChanged.AddListener(bo => applyUrbanLevel = bo);
        urbanSlider.onValueChanged.AddListener(val => activeUrbanLevel = (int)val);
        farmToggle.onValueChanged.AddListener(bo => applyFarmLevel = bo);
        farmSlider.onValueChanged.AddListener(val => activeFarmLevel = (int)val);
        plantToggle.onValueChanged.AddListener(bo => applyPlantLevel = bo);
        plantSlider.onValueChanged.AddListener(val => activePlantLevel = (int)val);
        

        InitToggles(colorToggles, SetColor);
        InitToggles(riverToggles, SetRiverMode);
        InitToggles(roadToggles, SetRoadMode);
        InitToggles(walledToggles, SetWalledMode);
    }

    private void FindComponent<T>(out T obj, string path, Transform parent = null)
    {
        parent = parent ?? root;
        obj = parent.Find(path).GetComponent<T>();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0)
            && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        else
        {
            previousCell = null;
        }
    }

    private void HandleInput()
    {
        Ray inputRay = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(inputRay, out RaycastHit hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);
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
        if (cell)
        {
            if (applyColor)
            {
                cell.Color = activeColor;
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
            if(walledMode!=OptionalToggle.Ignore)
            {
                cell.Walled = walledMode == OptionalToggle.Yes ;
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

        index -= 1;
        if (index < colors.Length)
        {
            activeColor = colors[index];
        }
        else
        {
            Debug.Log("Error:Colors Length Out");
        }
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
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

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }
}
