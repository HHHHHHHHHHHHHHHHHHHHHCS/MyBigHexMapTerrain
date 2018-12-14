using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using System.Linq;

public static class SaveLoadModule
{
    public const int version_1 = 1;//可以载入自定义地图大小
    public const int version_2 = 2;//可以载入单位

    private const int nowVersionHeader = 2;
    private const string saveDir = @"../SaveMap";
    private const string saveFileEnd = ".saveMap";

    private static readonly string savePath;

    static SaveLoadModule()
    {
        if (!string.IsNullOrEmpty(savePath)) return;
        savePath = Path.Combine(Application.dataPath, saveDir);
        Directory.CreateDirectory(savePath);
    }

    public static void Save(string fileName, HexGrid hexGrid)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.Log("Save fileName is null or empty");
            return;
        }

        var filePath = Path.Combine(savePath, fileName + saveFileEnd);
        using (var writer = new BinaryWriter(
            File.Open(filePath, FileMode.Create)))
        {
            writer.Write(nowVersionHeader);
            hexGrid.Save(writer);
        }
    }

    public static void Load(string fileName, HexGrid hexGrid)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.Log("Load fileName is null or empty");
            return;
        }

        var filePath = Path.Combine(savePath, fileName + saveFileEnd);
        if (!File.Exists(filePath))
        {
            return;
        }

        using (var reader = new BinaryReader(
            File.Open(filePath, FileMode.Open)))
        {
            var header = reader.ReadInt32();
            if (header <= nowVersionHeader)
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


    public static void Delete(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.Log("Delete fileName is null or empty");
            return;
        }

        var filePath = Path.Combine(savePath, fileName + saveFileEnd);
        if (!File.Exists(filePath))
        {
            return;
        }

        File.Delete(filePath);
    }

    public static List<string> GetAllFile()
    {
        DirectoryInfo dirInfo = new DirectoryInfo(savePath);
        var tempFiles = dirInfo.GetFiles();
        var fileEndLeng = saveFileEnd.Length;

        return tempFiles.Where(item =>
                item.Name.Length - item.Name.LastIndexOf(saveFileEnd)
                == fileEndLeng)
            .Select(x => x.Name.Replace(saveFileEnd, "")).ToList();
    }
}