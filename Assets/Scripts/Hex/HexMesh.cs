using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// mesh生成器
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    public bool useCollider, useCellData, useUVCoordinates, useUV2Coordinates;

    [NonSerialized] List<Vector3> vertices, cellIndices; //顶点,三边地形的类型(uv2)
    [NonSerialized] List<Color> cellWeights; //顶点颜色 判断用哪边的颜色
    [NonSerialized] List<Vector2> uv0s, uv1s; //uv0,uv1
    [NonSerialized] List<int> triangles; //mesh的需要的顶点所对应的index

    Mesh hexMesh;
    MeshCollider meshCollider;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        if (useCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        hexMesh.name = "Hex Mesh";
    }

    /// <summary>
    /// 清理生成mesh 顶点 颜色 uv
    /// </summary>
    public void Clear()
    {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get();
        if (useCellData)
        {
            cellWeights = ListPool<Color>.Get();
            cellIndices = ListPool<Vector3>.Get();
        }

        if (useUVCoordinates)
        {
            uv0s = ListPool<Vector2>.Get();
        }

        if (useUV2Coordinates)
        {
            uv1s = ListPool<Vector2>.Get();
        }

        triangles = ListPool<int>.Get();
    }

    /// <summary>
    /// 设置生成mesh 顶点 颜色 uv
    /// </summary>
    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);
        if (useCellData)
        {
            hexMesh.SetColors(cellWeights);
            ListPool<Color>.Add(cellWeights);
            hexMesh.SetUVs(2, cellIndices);
            ListPool<Vector3>.Add(cellIndices);
        }

        if (useUVCoordinates)
        {
            hexMesh.SetUVs(0, uv0s);
            ListPool<Vector2>.Add(uv0s);
        }

        if (useUV2Coordinates)
        {
            hexMesh.SetUVs(1, uv1s);
            ListPool<Vector2>.Add(uv1s);
        }

        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);
        hexMesh.RecalculateNormals();
        if (useCollider)
        {
            meshCollider.sharedMesh = hexMesh;
        }
    }

    /// <summary>
    /// 添加三角面片(有噪音)
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    /// <summary>
    /// 添加三角面片(没有噪音)
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    /// <summary>
    /// 添加三角面片UV
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        uv0s.Add(uv1);
        uv0s.Add(uv2);
        uv0s.Add(uv3);
    }

    /// <summary>
    /// 添加四边形面片(有噪音)
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        AddQuadUnperturbed(v1, v2, v3, v4);
    }


    /// <summary>
    /// 添加四边形面片(无噪音)
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    public void AddQuadUnperturbed(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    /// <summary>
    /// 添加四边形UV
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    /// <param name="uv4"></param>
    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        uv0s.Add(uv1);
        uv0s.Add(uv2);
        uv0s.Add(uv3);
        uv0s.Add(uv4);
    }

    /// <summary>
    /// 添加四边形UV
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uv0s.Add(new Vector2(uMin, vMin));
        uv0s.Add(new Vector2(uMax, vMin));
        uv0s.Add(new Vector2(uMin, vMax));
        uv0s.Add(new Vector2(uMax, vMax));
    }

    /// <summary>
    /// uv2 添加三角形UV
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        uv1s.Add(uv1);
        uv1s.Add(uv2);
        uv1s.Add(uv3);
    }

    /// <summary>
    /// uv2 添加四边形UV
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    /// <param name="uv4"></param>
    public void AddQuadUV2(
        Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        uv1s.Add(uv1);
        uv1s.Add(uv2);
        uv1s.Add(uv3);
        uv1s.Add(uv4);
    }

    /// <summary>
    /// uv2 添加四边形UV
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV2(
        float uMin, float uMax, float vMin, float vMax)
    {
        uv1s.Add(new Vector2(uMin, vMin));
        uv1s.Add(new Vector2(uMax, vMin));
        uv1s.Add(new Vector2(uMin, vMax));
        uv1s.Add(new Vector2(uMax, vMax));
    }

    /// <summary>
    /// 添加三角形,三边地形权重和颜色
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights1"></param>
    /// <param name="weights2"></param>
    /// <param name="weights3"></param>
    public void AddTriangleCellData(
        Vector3 indices, Color weights1, Color weights2, Color weights3)
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
    }

    /// <summary>
    /// 添加三角形,三边地形权重和颜色
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights"></param>
    public void AddTriangleCellData(Vector3 indices, Color weights)
    {
        AddTriangleCellData(indices, weights, weights, weights);
    }

    /// <summary>
    /// 添加四边形,三边地形权重和颜色
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights1"></param>
    /// <param name="weights2"></param>
    /// <param name="weights3"></param>
    /// <param name="weights4"></param>
    public void AddQuadCellData(Vector3 indices
        , Color weights1, Color weights2, Color weights3, Color weights4)
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
        cellWeights.Add(weights4);
    }

    /// <summary>
    /// 添加四边形,三边地形权重和颜色
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights1"></param>
    /// <param name="weights2"></param>
    public void AddQuadCellData(
        Vector3 indices,Color weights1,Color weights2)
    {
        AddQuadCellData(indices, weights1, weights1, weights2, weights2);
    }

    /// <summary>
    /// 添加四边形,三边地形权重和颜色
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights"></param>
    public void AddQuadCellData(Vector3 indices, Color weights)
    {
        AddQuadCellData(indices, weights, weights, weights, weights);
    }
}