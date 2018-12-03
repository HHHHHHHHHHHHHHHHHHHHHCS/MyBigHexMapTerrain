using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class CombineMesh : MonoBehaviour
{
    private void Awake()
    {
        DoCombineMesh();
    }

    private void DoCombineMesh(bool isOneMat = true, bool isDestory = true)
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length - 1];
        Material[] mats = new Material[meshFilters.Length - 1];
        Matrix4x4 matrix = transform.worldToLocalMatrix;

        int nowIndex = 0;
        foreach (var mf in meshFilters)
        {
            if (mf.gameObject.GetInstanceID() == gameObject.GetInstanceID())
            {
                continue;
            }
            var mr = mf.GetComponent<MeshRenderer>();
            if (!mr)
            {
                continue;
            }
            combine[nowIndex].mesh = mf.sharedMesh;
            combine[nowIndex].transform = matrix * mf.transform.localToWorldMatrix;
            mats[nowIndex] = mr.sharedMaterial;

            if (isDestory)
            {
                Destroy(mf.gameObject);
            }
            else
            {
                mr.gameObject.SetActive(false);
            }
            nowIndex++;
        }

        var myFilter = GetComponent<MeshFilter>();
        var mesh = new Mesh();
        mesh.name = "Combine Mesh";
        myFilter.mesh = mesh;
        mesh.CombineMeshes(combine, isOneMat);
        //利用这个可以保存mesh 避免每次动态合并 
        //AssetDatabase.CreateAsset(mesh, path);
        var myRender = GetComponent<MeshRenderer>();
        if (isOneMat)
        {
            myRender.sharedMaterial = mats[0];
        }
        else
        {
            myRender.sharedMaterials = mats;
        }
    }

}
