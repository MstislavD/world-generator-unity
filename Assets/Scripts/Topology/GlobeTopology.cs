using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public static class GlobeTopology
{
    static int[,] verticesData =
     {
        { 0, 1, 2, 3, 4 },
        { 0, 4, 14, 15, 10 },
        { 1, 0, 10, 16, 11 },
        { 2, 1, 11, 17, 12 },
        { 3, 2, 12, 18, 13 },
        { 4, 3, 13, 19, 14 },
        { 9, 8, 7, 6, 5 },
        { 18, 12, 17, 9, 5 },
        { 19, 13, 18, 5, 6 },
        { 15, 14, 19, 6, 7 },
        { 16, 10, 15, 7, 8 },
        { 17, 11, 16, 8, 9 }
    };

    static int[,] bandsData =
    {
        { 4, 0, 2, 0 },
        { 0, 1, 2, 0 },
        { 1, 2, 2, 0 },
        { 2, 3, 2, 0 },
        { 3, 4, 2, 0 },
        { 9, 8, 2, 0 },
        { 8, 7, 2, 0 },
        { 7, 6, 2, 0 },
        { 6, 5, 2, 0 },
        { 5, 9, 2, 0 },
        { 0, 10, 1, 1 },
        { 1, 11, 1, 1 },
        { 2, 12, 1, 1 },
        { 3, 13, 1, 1 },
        { 4, 14, 1, 1 },
        { 5, 18, 1, 1 },
        { 6, 19, 1, 1 },
        { 7, 15, 1, 1 },
        { 8, 16, 1, 1 },
        { 9, 17, 1, 1 },
        { 10, 16, 0, 0 },
        { 11, 17, 0, 0 },
        { 12, 18, 0, 0 },
        { 13, 19, 0, 0 },
        { 14, 15, 0, 0 },
        { 15, 10, 2, 2 },
        { 16, 11, 2, 2 },
        { 17, 12, 2, 2 },
        { 18, 13, 2, 2 },
        { 19, 14, 2, 2 }
    };

    static int[,] zonesData =
    {
        { 0, 1, 2, 0, 10, 1 },
        { 0, 2, 3, 1, 11, 2 },
        { 0, 3, 4, 2, 12, 3 },
        { 0, 4, 5, 3, 13, 4 },
        { 0, 5, 1, 4, 14, 0 },
        { 6, 8, 7, 8, 15, 9 },
        { 6, 9, 8, 7, 16, 8 },
        { 6, 10, 9, 6, 17, 7 },
        { 6, 11, 10, 5, 18, 6 },
        { 6, 7, 11, 9, 19, 5 },
        { 10, 2, 1, 20, 10, 25 },
        { 11, 3, 2, 21, 11, 26 },
        { 7, 4, 3, 22, 12, 27 },
        { 8, 5, 4, 23, 13, 28 },
        { 9, 1, 5, 24, 14, 29 },
        { 1, 9, 10, 24, 17, 25 },
        { 2, 10, 11, 20, 18, 26 },
        { 3, 11, 7, 21, 19, 27 },
        { 4, 7, 8, 22, 15, 28  },
        { 5, 8, 9, 23, 16, 29 }
    };

    static Vector3[] vertices = generateDodecahedronVertices();

    public static Vector3 GetVectorFromZone(int zoneIndex, int direction) => vertices[zonesData[zoneIndex, direction]];

    public static int GetVertexFromZone(int zoneIndex, int direction) => zonesData[zoneIndex, direction];

    public static int GetZoneFromVertex(int vertexIndex, int direction) => verticesData[vertexIndex, direction];

    public static int GetZoneFromBand(int bandIndex, int zoneIndex) => bandsData[bandIndex, zoneIndex];

    public static int GetBandFromZone(int zoneIndex, int direction) => zonesData[zoneIndex, direction + 3];

    public static int GetDirectionFromBand(int bandIndex, int directionIndex) => bandsData[bandIndex, directionIndex + 2];

    public static int GetDirectionFromVertex(int vertexIndex, int zoneIndex)
    {
        for (int i = 0; i < 5; i++)
        {
            if (GetZoneFromVertex(vertexIndex, i) == zoneIndex)
            {
                return i;
            }
        }
        return -1;
    }

    public static Vector3 GetVertex(int vertexIndex) => vertices[vertexIndex];

    static Vector3[] generateDodecahedronVertices()
    {
        Vector3[] vertices = new Vector3[12];
        float angle = 1f - 116.56505f / 180f;
        quaternion angleRotation = Unity.Mathematics.quaternion.RotateX(angle * PI);

        for (int i = 0; i < 2; i++)
        {
            quaternion hemisphereRotation = Unity.Mathematics.quaternion.RotateX(PI * i);
            vertices[i * 6] = mul(hemisphereRotation, up());

            for (int j = 0; j < 5; j++)
            {
                quaternion rotation = Unity.Mathematics.quaternion.RotateY(0.2f * PI * (1f + 2f * j));
                rotation = mul(mul(rotation, angleRotation), hemisphereRotation);
                vertices[i * 6 + j + 1] = mul(rotation, up());
            }
        }

        return vertices;
    }

}
