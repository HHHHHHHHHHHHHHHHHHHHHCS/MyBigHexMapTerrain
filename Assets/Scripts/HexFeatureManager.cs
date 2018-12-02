using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollections
        , farmCollections, plantCollections;
    public HexMesh walls;

    private Transform container;

    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform);
        walls.Clear();
    }

    public void Apply()
    {
        walls.Apply();
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        var prefab = PickPrefab(urbanCollections
            , cell.UrbanLevel, hash.a, hash.d);
        var otherPrefab = PickPrefab(farmCollections
            , cell.FarmLevel, hash.b, hash.d);

        float usedHash = hash.a;
        if (prefab)
        {
            if (otherPrefab && hash.b < usedHash)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.b;
        }

        otherPrefab = PickPrefab(plantCollections
                    , cell.PlantLevel, hash.c, hash.d);
        if (prefab)
        {
            if (otherPrefab && hash.c < usedHash)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }

        var instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        instance.SetParent(container);
    }

    /// <summary>
    /// 根据传入的指定装饰物集合,装饰物等级,随机值,装饰物的外貌
    /// 获得 prefab
    /// </summary>
    /// <param name="level"></param>
    /// <param name="hash"></param>
    /// <param name="choice"></param>
    /// <returns></returns>
    private Transform PickPrefab(HexFeatureCollection[] collection
        , int level, float hash, float choice)
    {
        if (level > 0)
        {
            var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 添加墙(两个cell)
    /// </summary>
    /// <param name="near"></param>
    /// <param name="nearCell"></param>
    /// <param name="far"></param>
    /// <param name="farCell"></param>
    public void AddWall(EdgeVertices near, HexCell nearCell
        ,EdgeVertices far, HexCell farCell)
    {
        if (nearCell.Walled != farCell.Walled)
        {
            AddWallSegment(near.v1, far.v1, near.v2, far.v2);
            AddWallSegment(near.v2, far.v2, near.v3, far.v3);
            AddWallSegment(near.v3, far.v3, near.v4, far.v4);
            AddWallSegment(near.v4, far.v4, near.v5, far.v5);
        }
    }

    /// <summary>
    /// 添加墙(三个cell)
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="cell1"></param>
    /// <param name="c2"></param>
    /// <param name="cell2"></param>
    /// <param name="c3"></param>
    /// <param name="cell3"></param>
    public void AddWall(
        Vector3 c1, HexCell cell1
        , Vector3 c2, HexCell cell2
        , Vector3 c3, HexCell cell3)
    {
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled)
        {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }

    /// <summary>
    /// 添加墙体的四边块mesh
    /// </summary>
    /// <param name="nearLeft"></param>
    /// <param name="farLeft"></param>
    /// <param name="nearRight"></param>
    /// <param name="farRight"></param>
    private void AddWallSegment(Vector3 nearLeft, Vector3 farLeft
        , Vector3 nearRight, Vector3 farRight)
    {
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftThicknessOffset =
            HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset =
            HexMetrics.WallThicknessOffset(nearRight, farRight);

        float leftTop = left.y + HexMetrics.wallHeight;
        float rightTop = right.y + HexMetrics.wallHeight;

        Vector3 v1, v2, v3, v4;

        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v2, v1, v4, v3);

        walls.AddQuadUnperturbed(t1, t2, v3, v4);
    }

    /// <summary>
    /// 添加墙体的中间的小三角
    /// </summary>
    /// <param name="nearLeft"></param>
    /// <param name="farLeft"></param>
    /// <param name="nearRight"></param>
    /// <param name="farRight"></param>
    private void AddWallSegment(
        Vector3 pivot, HexCell pivotCell
        , Vector3 left, HexCell leftCell
        , Vector3 right, HexCell rightCell)
    {
        AddWallSegment(pivot, left, pivot, right);
    }


}
