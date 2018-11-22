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
    public Color[] colors;

    private Toggle[] toggles;
    private ToggleGroup toggleGroup;
    private Toggle elevationToggle;
    private Slider elevationSlider;
    private Slider brushSlider;
    private Toggle labelsToggle;
    private HexGrid hexGrid;
    private Color activeColor;
    private Camera mainCam;
    private int activeElevation;
    private int brushSize;
    private bool applyColor;
    private bool applyElevation = true;


    private void Awake()
    {
        mainCam = Camera.main;
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
        Transform root = transform.Find("Bg");
        toggleGroup = root.Find("ToggleGroup_Color").GetComponent<ToggleGroup>();
        elevationToggle = root.Find("Toggle_Elevation").GetComponent<Toggle>();
        elevationSlider = root.Find("Slider_Elevation").GetComponent<Slider>();
        brushSlider = root.Find("Slider_BrustSize").GetComponent<Slider>();
        labelsToggle = root.Find("Toggle_Labels").GetComponent<Toggle>();
        toggles = toggleGroup.GetComponentsInChildren<Toggle>();

        elevationToggle.onValueChanged.AddListener(bo => applyElevation = bo);
        elevationSlider.onValueChanged.AddListener(SetElevation);
        brushSlider.onValueChanged.AddListener(val => brushSize = (int)val);
        labelsToggle.onValueChanged.AddListener(ShowUI);
        ResetColor();
        foreach (var item in toggles)
        {
            item.onValueChanged.AddListener(ChangeToggle);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(0)
            && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        Ray inputRay = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(inputRay, out RaycastHit hit))
        {
            EditCells(hexGrid.GetCell(hit.point));
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
        }
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void ResetColor()
    {
        foreach (var item in toggles)
        {
            item.isOn = false;
        }
        toggles[0].isOn = true;
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

    private void ChangeToggle(bool bo)
    {
        foreach (var toggle in toggleGroup.ActiveToggles())
        {
            if (toggle.isOn)
            {
                var index = toggle.transform.GetSiblingIndex();
                SelectColor(index);
                break;
            }
        }
    }

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }
}
