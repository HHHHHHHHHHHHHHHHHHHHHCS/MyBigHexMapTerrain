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

    private ToggleGroup toggleGroup;
    private Slider elevationSlider;
    private Toggle[] toggles;
    private HexGrid hexGrid;
    private Color activeColor;
    private Camera mainCam;
    private int activeElevation;


    private void Awake()
    {
        mainCam = Camera.main;
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
        Transform root = transform;
        toggleGroup = root.Find("ToggleGroup_Color").GetComponent<ToggleGroup>();
        elevationSlider = root.Find("Slider_Elevation").GetComponent<Slider>();
        toggles = toggleGroup.GetComponentsInChildren<Toggle>();




        elevationSlider.onValueChanged.AddListener(SetElevation);
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
            EditCell(hexGrid.GetCell(hit.point));
        }
    }

    private void EditCell(HexCell cell)
    {
        cell.Color = activeColor;
        cell.Elevation = activeElevation;
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
        //elevationSlider.value = activeElevation / elevationSlider.maxValue;
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
}
