using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LightningMesh
{
    private const int NOT_INITIALIZED = -1;
    private Mesh _mesh;
    
    private int _totalPointsCount;

    private int _segmentResolution;
    
    private float _segmentRadius;

    public LightningMesh()
    {
        _mesh = null;
        _totalPointsCount = NOT_INITIALIZED;
        _segmentResolution = NOT_INITIALIZED;
        _segmentRadius = NOT_INITIALIZED;
    }

    public Mesh CreateLightningMesh(List<LightningBranch> lightningBranches, int segmentResolution, float segmentRadius)
    {
        // Calculate total points count. 
        int totalPointsCount = 0;
        foreach (LightningBranch lightningBranch in lightningBranches)
        {
            totalPointsCount += lightningBranch.LightningPoints.Count;
        }
        
        // Check if the mesh needs to be reconstructed or the already constructed mesh can be used. 
        if (NeedMeshReconstruction(totalPointsCount, segmentResolution))
        {
            // This will destroy the current mesh instance and create a new one. 
            ReconstructMesh(lightningBranches, totalPointsCount, segmentResolution, segmentRadius);
        }
    }

    private bool NeedMeshReconstruction(int pointsCount, int segmentResolution)
    {
        return _mesh == null || _totalPointsCount != pointsCount || _segmentResolution != segmentResolution;
    }

    private void ReconstructMesh(List<LightningBranch> lightningBranches, int pointsCount, int segmentResolution, float segmentRadius)
    {
        // Caching the latest mesh settings. 
        _totalPointsCount = pointsCount;
        _segmentResolution = segmentResolution;
        _segmentRadius = segmentRadius;

        DestroyMesh();
        CreateMesh(lightningBranches);
    }

    private void DestroyMesh()
    {
        if (_mesh != null)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                GameObject.DestroyImmediate(_mesh);
                _mesh = null;
                return;
                
            }
#endif
            GameObject.Destroy(_mesh);
            _mesh = null;
            
        }
    }

    private void CreateMesh(List<LightningBranch> lightningBranches)
    {
        _mesh = new Mesh();

        // Calculate total vertices and triangles count so that we can prevent continuous GC allocations. 
        int totalVerticesCount = 0;
        int totalTrianglesCount = 0;
        foreach (LightningBranch lightningBranch in lightningBranches)
        {
            int branchPointsCount = lightningBranch.LightningPoints.Count;
            // Total vertices count = number of segments * vertices count per segment. 
            int branchVerticesCount = _segmentResolution * branchPointsCount;
            // If there are X points, there are (X-1) segments connecting them. Each segment
            // requires 2 * verticesCount of triangles. 
            int branchTrianglesCount = 2 * _segmentResolution * (branchPointsCount - 1);

            totalVerticesCount += branchVerticesCount;
            totalTrianglesCount += branchTrianglesCount;
        }

        // Initializing data structures for the mesh data. 
        Vector3[] vertices = new Vector3[totalVerticesCount];
        Vector2[] uvs = new Vector2[totalVerticesCount];
        int[] triangleIndices = new int[totalTrianglesCount * 3];

        // Helper variables used to track offset in the vertices and triangles arrays.
        int totalFilledVertices = 0;
        int totalFilledTriangles = 0;
        foreach (LightningBranch lightningBranch in lightningBranches)
        {
            // fill in the vertices, UVs and triangle indices array with the data for a single lightning branch. 
            CreateMeshData(lightningBranch, ref vertices, ref uvs, ref triangleIndices, ref totalFilledVertices, ref totalFilledTriangles);
        }
        
        // filling the mesh with the generated data. 
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.vertices = vertices;
        _mesh.uv = uvs;
        _mesh.triangles = triangleIndices;
    }

    private void CreateMeshData(LightningBranch lightningBranch, ref Vector3[] vertices, ref Vector2[] uvs, ref int[] triangleIndices, ref int initialVerticesOffset, ref int initialTrianglesOffset)
    {
        // Helper variables.
        List<LightningPoint> lightningPoints = lightningBranch.LightningPoints;
        int pointsCount = lightningPoints.Count;
        int vertexOffset = initialVerticesOffset;
        int triangleOffset = initialTrianglesOffset;
        
        // UV coordinate that will be assigned to all UVs. 
        Vector2 uvCoordinate = new Vector2(lightningBranch.IntensityPercentage, 0f);

        for (int i = 0; i < pointsCount; i++)
        {
            for (int counter = 0; counter < _segmentResolution; counter++)
            {
                vertices[vertexOffset] = Vector3.zero;
                uvs[vertexOffset] = uvCoordinate;
                vertexOffset++;
            }
        }
    } 
}
