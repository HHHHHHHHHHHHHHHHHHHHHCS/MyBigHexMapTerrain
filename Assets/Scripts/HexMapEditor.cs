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


    private ToggleGroup colorToggleGroup;
    private Toggle[] colorToggles;
    private ToggleGroup riverToggleGroup;
    private Toggle[] riverToggles;

    private HexGrid hexGrid;
    private Camera mainCam;

    private Color activeColor;
    private int activeElevation;
    private int brushSize;
    private bool applyColor;
    private bool applyElevation = true;
    private OptionalToggle riverMode = OptionalToggle.Ignore;

    private bool isDrag;
    private HexDirection dragDirection;
    private HexCell previousCell;

    private void Awake()
    {
        mainCam = Camera.main;
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
        Transform root = transform.Find("Bg");
        colorToggleGroup = root.Find("ToggleGroup_Color").GetComponent<ToggleGroup>();
        colorToggles = colorToggleGroup.GetComponentsInChildren<Toggle>();
        var elevationToggle = root.Find("Toggle_Elevation").GetComponent<Toggle>();
        var elevationSlider = root.Find("Slider_Elevation").GetComponent<Slider>();
        var brushSlider = root.Find("Slider_BrustSize").GetComponent<Slider>();
        var labelsToggle = root.Find("Toggle_Labels").GetComponent<Toggle>();
        riverToggleGroup = root.Find("ToggleGroup_River").GetComponent<ToggleGroup>();
        riverToggles = riverToggleGroup.GetComponentsInChildren<Toggle>();


        elevationToggle.onValueChanged.AddListener(bo => applyElevation = bo);
        elevationSlider.onValueChanged.AddListener(SetElevation);
        brushSlider.onValueChanged.AddListener(val => brushSize = (int)val);
        labelsToggle.onValueChanged.AddListener(ShowUI);
        ResetColor();
        for (int i = 0; i < colorToggles.Length; i++)
        {
            int temp = i;
            colorToggles[i].onValueChanged.AddListener(bo => ChangeColorToggle(bo, temp));
        }
        ResetRiver();
        for (int i = 0; i < riverToggles.Length; i++)
        {
            int temp = i;
            riverToggles[i].onValueChanged.AddListener(bo => ChangeRiverToggle(bo, temp));
        }
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
            else if (isDrag && riverMode == OptionalToggle.Yes)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    previousCell.SetOutgoingRiver(dragDirection);
                }

            }
        }
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void ResetColor()
    {
        foreach (var item in colorToggles)
        {
            item.isOn = false;
        }
        colorToggles[0].isOn = true;
        SelectColor(0);
    }

    public void SelectColor(int index)
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

    private void ChangeColorToggle(bool bo, int index)
    {
        if (bo)
        {
            SelectColor(index);
        }
    }

    private void ResetRiver()
    {
        foreach (var item in riverToggles)
        {
            item.isOn = false;
        }
        riverToggles[0].isOn = true;
        SetRiverMode(0);
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }

    private void ChangeRiverToggle(bool bo, int index)
    {
        if (bo)
        {
            SetRiverMode(index);
        }
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
