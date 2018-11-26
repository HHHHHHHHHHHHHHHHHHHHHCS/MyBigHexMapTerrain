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

    private Color activeColor;
    private int activeElevation;
    private int brushSize;
    private bool applyColor;
    private bool applyElevation = true;
    private OptionalToggle riverMode = OptionalToggle.Ignore;
    private OptionalToggle roadMode = OptionalToggle.Ignore;

    private bool isDrag;
    private HexDirection dragDirection;
    private HexCell previousCell;

    private void Awake()
    {
        mainCam = Camera.main;
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();

        Transform root = transform.Find("Bg");
        var colorToggleGroup = root.Find("ToggleGroup_Color").GetComponent<ToggleGroup>();
        var colorToggles = colorToggleGroup.GetComponentsInChildren<Toggle>();
        var elevationToggle = root.Find("Toggle_Elevation").GetComponent<Toggle>();
        var elevationSlider = root.Find("Slider_Elevation").GetComponent<Slider>();
        var brushSlider = root.Find("Slider_BrustSize").GetComponent<Slider>();
        var labelsToggle = root.Find("Toggle_Labels").GetComponent<Toggle>();
        var riverToggleGroup = root.Find("ToggleGroup_River").GetComponent<ToggleGroup>();
        var riverToggles = riverToggleGroup.GetComponentsInChildren<Toggle>();
        var roadToggleGroup = root.Find("ToggleGroup_Road").GetComponent<ToggleGroup>();
        var roadToggles = roadToggleGroup.GetComponentsInChildren<Toggle>();

        elevationToggle.onValueChanged.AddListener(bo => applyElevation = bo);
        elevationSlider.onValueChanged.AddListener(SetElevation);
        brushSlider.onValueChanged.AddListener(val => brushSize = (int)val);
        labelsToggle.onValueChanged.AddListener(ShowUI);

        InitToggles(colorToggles,SetColor);
        InitToggles(riverToggles,SetRiverMode);
        InitToggles(roadToggles, SetRoadMode);
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
            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            if(roadMode==OptionalToggle.No)
            {
                cell.RemoveRoads();
            }
            if (isDrag )
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if(riverMode==OptionalToggle.Yes)
                    {
                        previousCell.SetOutgoingRiver(dragDirection);
                    }
                    if(roadMode==OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
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
