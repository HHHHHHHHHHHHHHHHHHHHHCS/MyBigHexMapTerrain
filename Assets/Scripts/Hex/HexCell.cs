using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地形的最小块
/// </summary>
public class HexCell : MonoBehaviour
{
    private const string nullString = "";

    public HexCoordinates coordinates;
    public RectTransform uiRect;
    public HexGridChunk chunk;


    private int terrainTypeIndex;
    private HexCell[] neighbors = new HexCell[6];
    private int elevation = int.MinValue;
    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;
    private bool[] roads = new bool[6];
    private int waterLevel;
    private int urbanLevel, farmLevel, plantLevel;
    private bool walled;
    private int specialIndex;
    private int visibility;

    /// <summary>
    /// 细胞格子的位置
    /// </summary>
    //[field:SerializeField]//可以在编辑面板显示get set
    public int Index { get; set; }

    public bool HasIncomingRiver
    {
        get => hasIncomingRiver;
        private set => hasIncomingRiver = value;
    }

    public bool HasOutgoingRiver
    {
        get => hasOutgoingRiver;
        private set => hasOutgoingRiver = value;
    }

    public HexDirection IncomingRiver
    {
        get => incomingRiver;
        private set => incomingRiver = value;
    }

    public HexDirection OutgoingRiver
    {
        get => outgoingRiver;
        private set => outgoingRiver = value;
    }

    public bool HasRiver => HasIncomingRiver || HasOutgoingRiver;

    public bool HasRiverBeginOrEnd => HasIncomingRiver != HasOutgoingRiver;

    public int Elevation
    {
        get => elevation;
        set
        {
            if (elevation == value)
            {
                return;
            }

            elevation = value;
            RefreshPosition();

            ValidateRivers();

            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection) i) > HexMetrics.roadDifferceHeight)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public int TerrainTypeIndex
    {
        get => terrainTypeIndex;
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                //Refresh();
                ShaderData.RefreshTerrain(this);
            }
        }
    }


    public Vector3 Position
    {
        get => transform.localPosition;
    }

    public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset)
                               * HexMetrics.elevationStep;

    public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset)
                                  * HexMetrics.elevationStep;

    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    return true;
                }
            }

            return false;
        }
    }

    public HexDirection RiverBeginOrEndDirection
        => HasIncomingRiver ? incomingRiver : outgoingRiver;

    public int WaterLevel
    {
        get => waterLevel;
        set
        {
            if (waterLevel == value)
            {
                return;
            }

            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderwater => waterLevel > elevation;

    public float WaterSurfaceY => (waterLevel + HexMetrics.waterElevationOffset)
                                  * HexMetrics.elevationStep;

    public int UrbanLevel
    {
        get => urbanLevel;
        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get => farmLevel;
        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get => plantLevel;
        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public bool Walled
    {
        get => walled;

        set
        {
            if (walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }

    public int SpecialIndex
    {
        get => specialIndex;
        set
        {
            if (specialIndex != value && !HasRiver)
            {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial => specialIndex > 0;

    public int Distance { get; set; }

    public int SearchPhase { get; set; }

    /// <summary>
    /// 寻路的父节点
    /// </summary>
    public HexCell PathFrom { get; set; }

    /// <summary>
    /// A*寻路的权重值
    /// </summary>
    public int SearchHeuristic { get; set; }

    /// <summary>
    /// A*寻路的排序值
    /// </summary>
    public int SearchPrioty => Distance + SearchHeuristic;

    /// <summary>
    /// A*寻路相同权值的队列储存
    /// </summary>
    public HexCell NextWithSamePriority { get; set; }

    /// <summary>
    /// 地图上的单位
    /// </summary>
    public HexUnit Unit { get; set; }

    /// <summary>
    /// 战争迷雾的数据
    /// </summary>
    public HexCellShaderData ShaderData { get; set; }

    /// <summary>
    /// 战争迷雾的可见
    /// </summary>
    public bool IsVisible => visibility > 0;

    private void UpdateDistanceLabel()
    {
        Text label = uiRect.GetComponent<Text>();
        label.text = Distance == int.MaxValue ? nullString : Distance.ToString();
    }

    public void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f)
                      * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int) direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int) direction] = cell;
        cell.neighbors[(int) direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int) direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    /// <summary>
    /// 得到高度差
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public int GetElevationDifference(HexDirection direction)
    {
        int difference = Elevation - GetNeighbor(direction).Elevation;
        return difference >= 0 ? difference : -difference;
    }

    /// <summary>
    /// 是否能生成河流
    /// </summary>
    /// <param name="neighbor"></param>
    /// <returns></returns>
    private bool IsVaildRiverDestination(HexCell neighbor)
    {
        return neighbor && (
                   elevation >= neighbor.elevation
                   || waterLevel == neighbor.elevation);
    }

    /// <summary>
    /// 刷新(包括周围的邻居)
    /// </summary>
    public void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();

            foreach (var neighbor in neighbors)
            {
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }

            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }

    /// <summary>
    /// 刷新自己
    /// </summary>
    private void RefreshSelfOnly()
    {
        chunk.Refresh();
        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }

    /// <summary>
    /// 是否有河流经过这个方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return HasIncomingRiver && IncomingRiver == direction
               || HasOutgoingRiver && OutgoingRiver == direction;
    }

    /// <summary>
    /// 是否有路经过这个方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int) direction];
    }

    /// <summary>
    /// 在某个方向添加路
    /// </summary>
    /// <param name="direction"></param>
    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int) direction] && !HasRiverThroughEdge(direction)
                                    && !IsSpecial && !GetNeighbor(direction).IsSpecial
                                    && GetElevationDifference(direction) <= HexMetrics.roadDifferceHeight)
        {
            SetRoad((int) direction, true);
        }
    }

    /// <summary>
    /// 在某个方向设置路是否有
    /// </summary>
    /// <param name="index"></param>
    /// <param name="state"></param>
    private void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int) (((HexDirection) index).Opposite())] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    /// <summary>
    /// 移除全部的路
    /// </summary>
    public void RemoveRoads()
    {
        for (var i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                roads[i] = false;
                neighbors[i].roads[(int) (((HexDirection) i).Opposite())] = false;
                neighbors[i].RefreshSelfOnly();
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// 移除出去的河流
    /// </summary>
    public void RemoveOutgoingRiver()
    {
        if (!HasOutgoingRiver)
        {
            return;
        }

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(OutgoingRiver);
        neighbor.HasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// 移除进来的河流
    /// </summary>
    public void RemoveIncomingRiver()
    {
        if (!HasIncomingRiver)
        {
            return;
        }

        HasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(IncomingRiver);
        neighbor.HasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// 移除进来和出去的河(即全部的河流)
    /// </summary>
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    /// <summary>
    /// 设置出去的河流
    /// </summary>
    /// <param name="direction"></param>
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (HasOutgoingRiver && OutgoingRiver == direction)
        {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsVaildRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();
        if (HasIncomingRiver && IncomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        HasOutgoingRiver = true;
        OutgoingRiver = direction;
        specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.HasIncomingRiver = true;
        neighbor.IncomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

        SetRoad((int) direction, false);
    }

    /// <summary>
    /// 验证是否能生成河流  然后移除河流
    /// </summary>
    private void ValidateRivers()
    {
        if (HasOutgoingRiver
            && !IsVaildRiverDestination((GetNeighbor(OutgoingRiver))))
        {
            RemoveOutgoingRiver();
        }

        if (HasIncomingRiver
            && !GetNeighbor(IncomingRiver).IsVaildRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }

    public void DisableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    public void SetLabel(string text)
    {
        Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }

    public void IncreaseVisibility()
    {
        visibility += 1;
        if (visibility == 1)
        {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility()
    {
        visibility -= 1;
        if (visibility == 0)
        {
            ShaderData.RefreshVisibility(this);
        }
    }

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="writer"></param>
    public void Save(MyWriter writer)
    {
        writer.Write((byte) terrainTypeIndex);
        writer.Write((byte) elevation);
        writer.Write((byte) waterLevel);
        writer.Write((byte) urbanLevel);
        writer.Write((byte) farmLevel);
        writer.Write((byte) plantLevel);
        writer.Write((byte) specialIndex);
        writer.Write(walled);
        if (hasIncomingRiver)
        {
            writer.Write((byte) (incomingRiver + 128));
        }
        else
        {
            writer.Write((byte) 0);
        }

        if (hasOutgoingRiver)
        {
            writer.Write((byte) (outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte) 0);
        }

        var roadFlags = 0;
        for (var i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;
            }
        }

        writer.Write((byte) roadFlags);
    }

    /// <summary>
    /// 读取
    /// </summary>
    /// <param name="reader"></param>
    public void Load(MyReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();
        var riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection) (riverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection) (riverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (var i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }
}