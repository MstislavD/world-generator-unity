using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using System;

public class AdvancedMultiStreamProceduralMesh
{
    public static Mesh GenerateMesh(PolygonSphere sphere)
    {
        int vertexAttributeCount = 2;
        int vertexCount = (sphere.PolygonCount - 12) * 7 + 12 * 6;
        int triangleIndexCount = ((sphere.PolygonCount - 12) * 6 + 12 * 5) * 3;

        Debug.Log($"Triangles: {triangleIndexCount}");

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1);
        //vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float16, 4, 2);

        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();

        NativeArray<float3> positions = meshData.GetVertexData<float3>();

        for (int polygonIndex = 0, count = 0; polygonIndex < sphere.PolygonCount; polygonIndex++)
        {
            int sides = sphere.GetSides(polygonIndex);
            positions[count++] = sphere.GetCenter(polygonIndex);
            for (int side = 0; side < sides; side++)
            {
                positions[count++] = sphere.GetPolygonVertex(polygonIndex, side);
            }
        }

        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);

        for (int polygonIndex = 0, count = 0; polygonIndex < sphere.PolygonCount; polygonIndex++)
        {
            Vector3 normal = Vector3.Normalize(sphere.GetCenter(polygonIndex));
            int sides = sphere.GetSides(polygonIndex);
            for (int side = 0; side < sides + 1; side++)
            {
                normals[count++] = normal;
            }
        }

        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);

        NativeArray<int> triangleIndices = meshData.GetIndexData<int>();

        for (int polygonIndex = 0, vertex_count = 0, index_count = 0; polygonIndex < sphere.PolygonCount; polygonIndex++)
        {
            int sides = sphere.GetSides(polygonIndex);
            for (int side = 0; side < sides; side++)
            {
                triangleIndices[index_count++] = vertex_count;
                triangleIndices[index_count++] = vertex_count + side + 1;
                triangleIndices[index_count++] = vertex_count + (side + 1) % sides + 1;
                //triangleIndices[index_count++] = vertex_count + (side + 1) % sides + 1;
            }
            vertex_count += sides + 1;
        }

        var bounds = new Bounds(Vector3.zero, Vector3.one);
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount) { bounds = bounds, vertexCount = vertexCount }, MeshUpdateFlags.DontRecalculateBounds);

        var mesh = new Mesh() { name = "Procedural Mesh", bounds = bounds };
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        return mesh;
    }
}
