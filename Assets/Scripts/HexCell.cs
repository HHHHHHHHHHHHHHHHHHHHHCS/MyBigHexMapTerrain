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

    public bool HasRiver
    {
        get => HasIncomingRiver || HasOutgoingRiver;
    }

    public bool HasRiverBeginOrEnd
    {
        get => HasIncomingRiver != HasOutgoingRiver;
    }

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


    public Color Color
    {
        get => HexMetrics.colors[terrainTypeIndex];
    }

    public int TerrainTypeIndex
    {
        get => terrainTypeIndex;
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }


    public Vector3 Position
    {
        get => transform.localPosition;
    }

    public float StreamBedY
    {
        get => (elevation + HexMetrics.streamBedElevationOffset)
               * HexMetrics.elevationStep;
    }

    public float RiverSurfaceY
    {
        get => (elevation + HexMetrics.waterElevationOffset)
               * HexMetrics.elevationStep;
    }

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
    {
        get => HasIncomingRiver ? incomingRiver : outgoingRiver;
    }

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

    public bool IsUnderwater
    {
        get => waterLevel > elevation;
    }

    public float WaterSurfaceY
    {
        get => (waterLevel + HexMetrics.waterElevationOffset)
               * HexMetrics.elevationStep;
    }

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

    public bool IsSpecial
    {
        get => specialIndex > 0;
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

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = Elevation - GetNeighbor(direction).Elevation;
        return difference >= 0 ? difference : -difference;
    }

    private bool IsVaildRiverDestination(HexCell neighbor)
    {
        return neighbor && (
                   elevation >= neighbor.elevation
                   || waterLevel == neighbor.elevation);
    }

    public void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();

            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    private void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return HasIncomingRiver && IncomingRiver == direction
               || HasOutgoingRiver && OutgoingRiver == direction;
    }

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int) direction];
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int) direction] && !HasRiverThroughEdge(direction)
                                    && !IsSpecial && !GetNeighbor(direction).IsSpecial
                                    && GetElevationDifference(direction) <= HexMetrics.roadDifferceHeight)
        {
            SetRoad((int) direction, true);
        }
    }

    private void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int) (((HexDirection) index).Opposite())] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
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

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

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


    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
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
            writer.Write((byte)(outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }
        writer.Write(hasIncomingRiver);
        writer.Write((byte) incomingRiver);
        writer.Write(hasOutgoingRiver);
        writer.Write((byte) outgoingRiver);
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

    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
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
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }
}