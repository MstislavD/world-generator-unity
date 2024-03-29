using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ProceduralMeshes
{
    public interface IMeshStreams
    {
        void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount);
        void SetVertex(int index, Vertex data);
        void SetTriangle(int index, int3 triangle);
    }
}