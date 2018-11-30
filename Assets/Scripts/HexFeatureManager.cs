using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollections
        ,farmCollections,plantCollections;

    private Transform container;

    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform);
    }

    public void Apply()
    {

    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        var prefab = PickPrefab(urbanCollections
            ,cell.UrbanLevel, hash.a,hash.d);
        var otherPrefab = PickPrefab(farmCollections
            , cell.FarmLevel, hash.b, hash.d);

        float usedHash = hash.a;
        if(prefab)
        {
            if(otherPrefab&&hash.b< usedHash)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if(otherPrefab)
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
        ,int level, float hash,float choice)
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
}
