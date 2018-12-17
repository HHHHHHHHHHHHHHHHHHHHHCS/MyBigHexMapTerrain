#define _test //去掉_ 进行测试模式

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using System.Linq;
#if test
using System;
using System.Text;
#endif

public static class SaveLoadModule
{
    public const int version_1 = 1; //可以载入自定义地图大小
    public const int version_2 = 2; //可以载入单位

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
        using (var bw = new BinaryWriter(
            File.Open(filePath, FileMode.Create)))
        {
            var writer = new MyWriter(bw);
            writer.Write(nowVersionHeader);
            hexGrid.Save(writer);
#if test
            writer.PushText();
#endif
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
        if (!File.Exists(filePath)) return;

        using (var br = new BinaryReader(
            File.Open(filePath, FileMode.Open)))
        {
            var reader = new MyReader(br);
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
#if test
            reader.PushText();
#endif
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
        if (!File.Exists(filePath)) return;

        File.Delete(filePath);
    }

    public static List<string> GetAllFile()
    {
        var dirInfo = new DirectoryInfo(savePath);
        var tempFiles = dirInfo.GetFiles();
        var fileEndLeng = saveFileEnd.Length;

        return tempFiles.Where(item =>
                item.Name.Length - item.Name.LastIndexOf(saveFileEnd)
                == fileEndLeng)
            .Select(x => x.Name.Replace(saveFileEnd, "")).ToList();
    }
}

public class MyWriter
{
    private readonly BinaryWriter binWriter;
#if test
    private StringBuilder sb;
#endif

    public MyWriter(BinaryWriter binWriter)
    {
        this.binWriter = binWriter;
#if test
        sb = new StringBuilder();
#endif
    }

    public void Write(byte val)
    {
#if test
        sb.Append(val);
#endif
        binWriter.Write(val);
    }

    public void Write(int val)
    {
#if test
        sb.Append(val);
#endif
        binWriter.Write(val);
    }

    public void Write(float val)
    {
#if test
        sb.Append(val);
#endif
        binWriter.Write(val);
    }

    public void Write(bool val)
    {
#if test
        sb.Append(val);
#endif
        binWriter.Write(val);
    }

    public void Write(char val)
    {
#if test
        sb.Append(val);
#endif
        binWriter.Write(val);
    }

#if test
    public void PushText()
    {
        Debug.Log("Save:"+sb.ToString());
        sb.Clear();
    }
#endif
}

public class MyReader
{
    private readonly BinaryReader binReader;
#if test
    private StringBuilder sb;
#endif

    public MyReader(BinaryReader binReader)
    {
        this.binReader = binReader;
#if test
        sb = new StringBuilder();
#endif
    }

    public byte ReadByte()
    {
        var val = binReader.ReadByte();
#if test
        sb.Append(val);
#endif
        return val;
    }

    public int ReadInt32()
    {
        var val = binReader.ReadInt32();
#if test
        sb.Append(val);
#endif
        return val;
    }

    public float ReadSingle()
    {
        var val = binReader.ReadSingle();
#if test
        sb.Append(val);
#endif
        return val;
    }

    public bool ReadBoolean()
    {
        var val = binReader.ReadBoolean();
#if test
        sb.Append(val);
#endif
        return val;
    }

    public char ReadChar()
    {
        var val = binReader.ReadChar();
#if test
        sb.Append(val);
#endif
        return val;
    }

#if test
    public void PushText()
    {
        Debug.Log("Load:" + sb.ToString());
        sb.Clear();
    }
#endif
}