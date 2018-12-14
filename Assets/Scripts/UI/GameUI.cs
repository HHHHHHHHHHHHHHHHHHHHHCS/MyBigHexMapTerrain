using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameUI : MonoBehaviour
{
    public HexGrid hexGrid;

    private Camera mainCam;
    private HexCell currentCell;
    private HexUnit selectedUnit;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButton(0))
            {
                DoSelection();
            }
            else if (Input.GetMouseButton(1))
            {
                DoMove();
            }
            else if (selectedUnit)
            {
                DoPathFinding();
            }
        }
    }

    private bool UpdateCurrentCell()
    {
        HexCell cell =
            hexGrid.GetCell(mainCam.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell)
        {
            currentCell = cell;
            return true;
        }

        return false;
    }

    private void DoSelection()
    {
        hexGrid.ClearPath();
        UpdateCurrentCell();
        if (currentCell)
        {
            selectedUnit = currentCell.Unit;
        }
    }

    private void DoPathFinding()
    {
        if (UpdateCurrentCell())
        {
            if (currentCell && IsValidDestination(currentCell))
            {
                hexGrid.FindPath(selectedUnit.Location, currentCell, 24);
            }
            else
            {
                hexGrid.ClearPath();
            }
        }
    }

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }

    private void DoMove()
    {
        if (hexGrid.HasPath)
        {
            selectedUnit.Location = currentCell;
            hexGrid.ClearPath();
        }
    }
}