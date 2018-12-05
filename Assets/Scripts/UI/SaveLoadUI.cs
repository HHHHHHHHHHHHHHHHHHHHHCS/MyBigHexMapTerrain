using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadUI : MonoBehaviour
{
    private HexGrid hexGrid;

    public void Init(HexGrid _hexGrid)
    {
        hexGrid = _hexGrid;
    }

    public void Save()
    {
        SaveLoadModule.Save(hexGrid);
    }

    public void Load()
    {
        SaveLoadModule.Load(hexGrid);
    }
}
