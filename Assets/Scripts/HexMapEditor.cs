using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;

    private ToggleGroup toggleGroup;
    private Toggle[] toggles;
    private HexGrid hexGrid;
    private Color activeColor;
    private Camera mainCam;


    private void Awake()
    {
        mainCam = Camera.main;
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
        toggleGroup = transform.Find("ToggleGroup_Color").GetComponent<ToggleGroup>();
        toggles = toggleGroup.GetComponentsInChildren<Toggle>();
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
            hexGrid.ColorCell(hit.point, activeColor);
        }
    }

    public void ResetColor()
    {
        toggleGroup.SetAllTogglesOff();
        toggles[0].isOn = true;
        SelectColor(0);
    }

    public void SelectColor(int index)
    {
        if(index<colors.Length)
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
