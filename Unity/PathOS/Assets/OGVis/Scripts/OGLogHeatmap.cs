﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OGVis;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class OGLogHeatmap : MonoBehaviour
{
    private Gradient gradient;

    private Vector3[] vertices;
    private Vector2[] uv;
    private int[] triangles;

    private float displayHeight;

    private Mesh mesh;
    private MeshFilter filter;
    private MeshRenderer rend;

    private Material mat;
    private Texture2D tex;

    private Vector3 origin;
    private Vector3 gridSize;
    private float tileWidth;

    private int[,] tileCounts;

    public void Initialize(Extents extents, Gradient gradient, float displayHeight, float tileWidth)
    {
        filter = GetComponent<MeshFilter>();
        rend = GetComponent<MeshRenderer>();

        this.displayHeight = displayHeight;
        this.gradient = gradient;
        
        if(null == mesh)
            mesh = new Mesh();

        filter.mesh = mesh;

        DestroyImmediate(mat);

        mat = new Material(Shader.Find("Unlit/Transparent"));

        if (null == tex)
            tex = new Texture2D(0, 0);

        tex.filterMode = FilterMode.Point;
        mat.mainTexture = tex;
        rend.material = mat;

        //Order: Bottom left, top left, top right, bottom right.
        uv = new Vector2[4];
        uv[0] = new Vector2(0.0f, 0.0f);
        uv[1] = new Vector2(0.0f, 1.0f);
        uv[2] = new Vector2(1.0f, 1.0f);
        uv[3] = new Vector2(1.0f, 0.0f);

        triangles = new[] { 0, 1, 2, 0, 2, 3 };
        vertices = new Vector3[4];

        this.tileWidth = tileWidth;
        UpdateExtents(extents);
    }

    private void UpdateExtents(Extents extents)
    {
        if (tileWidth <= 0)
            return;

        //What is the position of our heatmap in the scene?
        //Centre of all extents.
        origin.x = 0.5f * (extents.min.x + extents.max.x);
        origin.y = displayHeight;
        origin.z = 0.5f * (extents.min.z + extents.max.z);

        transform.position = origin;

        //Grid size should be symmetrical about the origin of the heatmap.
        gridSize.x = 2 * Mathf.Ceil((0.5f * (extents.max.x - extents.min.x)) / tileWidth);
        gridSize.y = 0;
        gridSize.z = 2 * Mathf.Ceil((0.5f * (extents.max.z - extents.min.z)) / tileWidth);

        tileCounts = new int[(int)gridSize.x, (int)gridSize.z];

        vertices[0] = new Vector3(-0.5f * gridSize.x * tileWidth, 0.0f, -0.5f * gridSize.z * tileWidth);
        vertices[1] = new Vector3(-0.5f * gridSize.x * tileWidth, 0.0f,  0.5f * gridSize.z * tileWidth);
        vertices[2] = new Vector3( 0.5f * gridSize.x * tileWidth, 0.0f,  0.5f * gridSize.z * tileWidth);
        vertices[3] = new Vector3( 0.5f * gridSize.x * tileWidth, 0.0f, -0.5f * gridSize.z * tileWidth);

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        tex.Resize((int)gridSize.x, (int)gridSize.z);
        ClearTex();
        tex.Apply();
    }

    private void ClearTex()
    {
        Color32 white = new Color32(255, 255, 255, 128);
        Color32[] resetArray = tex.GetPixels32();

        for(int i = 0; i < resetArray.Length; ++i)
        {
            resetArray[i] = white;
        }

        int x = 0;
        int z = 0;

        resetArray[(int)(z * gridSize.x + x)] = new Color32(255, 0, 0, 128);

        z = 1;
        resetArray[(int)(z * gridSize.x + x)] = new Color32(0, 255, 0, 128);

        z = 0;
        x = 29;
        resetArray[(int)(z * gridSize.x + x)] = new Color32(0, 0, 255, 128);

        tex.SetPixels32(resetArray);
    }

    private void UpdateData(List<PlayerLog> logs, bool enabledOnly, bool windowOnly)
    {
        float minX = -0.5f * gridSize.x * tileWidth;
        float minZ = -0.5f * gridSize.z * tileWidth;

        float fac = 1.0f / tileWidth;

        int maxGridX = tileCounts.GetLength(0);
        int maxGridZ = tileCounts.GetLength(1);

        System.Array.Clear(tileCounts, 0, tileCounts.Length);

        int xGrid, zGrid = 0;

        for (int i = 0; i < logs.Count; ++i)
        {
            PlayerLog log = logs[i];

            if (enabledOnly && !log.visInclude)
                continue;

            int minIndex = (windowOnly) ? log.displayStartIndex : 0;
            int maxIndex = (windowOnly) ? log.displayEndIndex : log.pathPoints.Count - 1;

            for(int j = minIndex; j <= maxIndex; ++j)
            {
                xGrid = (int)((log.pathPoints[j].x - minX) * fac);
                zGrid = (int)((log.pathPoints[j].z - minZ) * fac);

                if (xGrid > 0 && xGrid < maxGridX
                    && zGrid > 0 && zGrid < maxGridZ)
                    ++tileCounts[xGrid, zGrid];
            }
        }

        int maxCount = 0;

        for(int x = 0; x < maxGridX; ++x)
        {
            for(int z = 0; z < maxGridZ; ++z)
            {
                if (tileCounts[x, z] > maxCount)
                    maxCount = tileCounts[x, z];
            }
        }

        int levels = Mathf.Min(10, Mathf.Max(maxCount, 2));
        float colorStep = 1.0f / (levels - 1);

        List<Color32> colors = new List<Color32>();

        for(int i = 0; i < levels; ++i)
        {
            colors.Add(gradient.Evaluate(i * colorStep));
        }

        colors.Add(gradient.Evaluate(1.0f));

        float binSize = (float)maxCount / levels;
        float binSizeFac = 1.0f / binSize;

        Color32[] heatmapArray = tex.GetPixels32();

        int rowSize = (int)gridSize.x;

        for (int x = 0; x < maxGridX; ++x)
        {
            for(int z = 0; z < maxGridZ; ++z)
            {
                heatmapArray[(int)(z * rowSize + x)] =
                    colors[Mathf.RoundToInt(tileCounts[x, z] * binSizeFac)];
            }
        }

        tex.SetPixels32(heatmapArray);
        tex.Apply();
    }
}
