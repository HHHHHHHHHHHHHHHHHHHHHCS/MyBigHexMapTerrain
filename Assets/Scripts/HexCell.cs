﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地形的单个
/// </summary>
public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public RectTransform uiRect;
    public HexGridChunk chunk;

    private Color color = new Color(0, 0, 0, 0);
    private HexCell[] neighbors = new HexCell[6];
    private int elevation = int.MinValue;
    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    public bool HasIncomingRiver { get => hasIncomingRiver; private set => hasIncomingRiver = value; }
    public bool HasOutgoingRiver { get => hasOutgoingRiver; private set => hasOutgoingRiver = value; }
    public HexDirection IncomingRiver { get => incomingRiver; private set => incomingRiver = value; }
    public HexDirection OutgoingRiver { get => outgoingRiver; private set => outgoingRiver = value; }
    public bool HasRiver { get => HasIncomingRiver || HasOutgoingRiver; }
    public bool HasRiverBeginOrEnd { get => HasIncomingRiver != HasOutgoingRiver; }

    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            if (elevation == value)
            {
                return;
            }

            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f)
                * HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;

            if (HasOutgoingRiver && Elevation < GetNeighbor(outgoingRiver).Elevation)
            {
                RemoveOutgoingRiver();
            }
            if (HasIncomingRiver && Elevation > GetNeighbor(IncomingRiver).Elevation)
            {
                RemoveIncomingRiver();
            }

            Refresh();
        }
    }

    public Color Color
    {
        get
        {
            return color;
        }

        set
        {
            if (color == value)
            {
                return;
            }
            color = value;
            Refresh();
        }
    }

    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public float StreamBedY
    {
        get
        {
            return (elevation + HexMetrics.streamBedElevationOffset)
                * HexMetrics.elevationStep;
        }
    }

    public float RiverSurfaceY
    {
        get
        {
            return (elevation + HexMetrics.riverSurfaceElevationOffset)
                * HexMetrics.elevationStep;
        }
    }


    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    private void Refresh()
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
        if(!HasIncomingRiver)
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
        if(HasOutgoingRiver&&OutgoingRiver==direction)
        {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if(!neighbor||Elevation<neighbor.Elevation)
        {
            return;
        }

        RemoveOutgoingRiver();
        if(HasIncomingRiver&&IncomingRiver==direction)
        {
            RemoveIncomingRiver();
        }

        HasOutgoingRiver = true;
        OutgoingRiver = direction;
        RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor.HasIncomingRiver = true;
        neighbor.IncomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }
}
