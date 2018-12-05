using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveLoadModule
{
    private const int versionHeader = 1;
    private const string saveFileName = "test.map";
    private const string saveDir = @"../SaveMap";

    private static string savePath;

    static SaveLoadModule()
    {
        if (!string.IsNullOrEmpty(savePath)) return;
        savePath = Path.Combine(Application.dataPath, saveDir);
        Directory.CreateDirectory(savePath);
        savePath = Path.Combine(savePath, saveFileName);
    }

    public static void Save(HexGrid hexGrid)
    {
        using (var writer = new BinaryWriter(
            File.Open(savePath, FileMode.Create)))
        {
            writer.Write(versionHeader);
            hexGrid.Save(writer);
        }
    }

    public static void Load(HexGrid hexGrid)
    {
        using (var reader = new BinaryReader(
            File.Open(savePath, FileMode.OpenOrCreate)))
        {
            var header = reader.ReadInt32();
            if (header <= versionHeader)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.Instance.ValidatePosition();
            }
            else
            {
                Debug.Log("map header version is error:" + header);
            }
        }
    }
}