using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class SphereMeshGenerator
{
    const float edgeRadius = 1.001f;
    float width;
    protected PolygonSphere sphere;

    public SphereMeshGenerator(PolygonSphere sphere)
    {
        this.sphere = sphere;
        width = min(0.1f + sphere.BandSize * (0.4f / 31f), 0.5f);
    }

    public Vector3[] GetPolygonVertices(int polygonIndex)
    {
        int sides = sphere.GetSides(polygonIndex);
        Vector3[] vertices = new Vector3[sides + 1];

        int count = 1;
        foreach(Vector3 vertex in getPolygonVertices(polygonIndex))
        {
            vertices[count++] = vertex;
            vertices[0] += vertex;
        }

        vertices[0] /= sides;

        return vertices;
    }

    public Vector3[] GetEdgeLine(int edgeIndex)
    {
        Vector3[] vertices = getEdgeVertices(edgeIndex);
        return new Vector3[] { vertices[0] * edgeRadius, vertices[1] * edgeRadius };
    }

    public Vector3[] GetEdgeBand(int edgeIndex)
    {
        Vector3[] bandVertices = new Vector3[6];
        Vector3 c1 = sphere.GetCenter(sphere.GetEdgePolygon1(edgeIndex));
        Vector3 c2 = sphere.GetCenter(sphere.GetEdgePolygon2(edgeIndex));
        Vector3[] edgeVertices = getEdgeVertices(edgeIndex);

        bandVertices[0] = edgeVertices[0] * edgeRadius;
        bandVertices[3] = edgeVertices[1] * edgeRadius;
        bandVertices[1] = Vector3.Lerp(bandVertices[0], c1, width) * edgeRadius;
        bandVertices[2] = Vector3.Lerp(bandVertices[3], c1, width) * edgeRadius;
        bandVertices[4] = Vector3.Lerp(bandVertices[3], c2, width) * edgeRadius;
        bandVertices[5] = Vector3.Lerp(bandVertices[0], c2, width) * edgeRadius;

        return bandVertices;
    }

    public Vector3[] GetEdgeBandSmooth(int edgeIndex)
    {
        Vector3[] vertices = new Vector3[10];
        Vector3[] vv = GetEdgeBand(edgeIndex);
        vv.CopyTo(vertices, 0);

        int p1 = sphere.GetEdgePolygon1(edgeIndex);
        int p2 = sphere.GetEdgePolygon2(edgeIndex);

        float w = width * 0.5f;
        vertices[6] = Vector3.Lerp(vertices[0], getNextVertex(edgeIndex, p1, PolygonSphere.Direction.Counterclockwise) * edgeRadius, w);
        vertices[7] = Vector3.Lerp(vertices[3], getNextVertex(edgeIndex, p1, PolygonSphere.Direction.Clockwise) * edgeRadius, w);
        vertices[8] = Vector3.Lerp(vertices[3], getNextVertex(edgeIndex, p2, PolygonSphere.Direction.Counterclockwise) * edgeRadius, w);
        vertices[9] = Vector3.Lerp(vertices[0], getNextVertex(edgeIndex, p2, PolygonSphere.Direction.Clockwise) * edgeRadius, w);

        return vertices;
    }

    public Vector3[] NormalsFlat(int pIndex) => Vector3.Normalize(sphere.GetCenter(pIndex)).Populate(sphere.GetSides(pIndex) + 1);

    public Vector3[] NormalsSphere(int polygonIndex)
    {
        Vector3[] normals = new Vector3[sphere.GetSides(polygonIndex) + 1];
        normals[0] = normalize(sphere.GetCenter(polygonIndex));

        int count = 1;
        foreach (Vector3 vertex in sphere.GetPolygonVertices(polygonIndex))
        {
            normals[count++] = normalize(vertex);
        }

        return normals;
    }

    public int[] GetEdgeTriangles(int startingIndex)
    {
        int[] triangles = new int[] { 0, 3, 1, 1, 3, 2, 0, 5, 3, 3, 5, 4 };
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] += startingIndex;
        }
        return triangles;
    }

    public int[] GetEdgeTrianglesSmooth(int startingIndex)
    {
        int[] triangles = new int[] { 0, 3, 1, 1, 3, 2, 0, 5, 3, 3, 5, 4, 0, 1, 6, 3, 7, 2, 3, 4, 8, 0, 9, 5 };
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] += startingIndex;
        }
        return triangles;
    }

    public int[] GetPolygonTriangles(int startingIndex, int sides)
    {
        int[] triangles = new int[sides * 3];
        for (int i = 0; i < sides; i++)
        {
            triangles[i * 3] = startingIndex;
            triangles[i * 3 + 1] = i + 1 + startingIndex;
            triangles[i * 3 + 2] = (i + 1) % sides + 1 + startingIndex;
        }
        return triangles;
    }

    protected virtual IEnumerable<Vector3> getPolygonVertices(int polygonIndex) => sphere.GetPolygonVertices(polygonIndex);

    protected virtual Vector3[] getEdgeVertices(int edgeIndex) => sphere.GetEdgeVertices(edgeIndex);

    protected virtual Vector3 getNextVertex(int edgeIndex, int polygonIndex, PolygonSphere.Direction direction) =>
        sphere.GetNextVertex(edgeIndex, polygonIndex, direction);
}

public class SphereMeshGeneratorSmoothed: SphereMeshGenerator
{
    const float smoothingRatio = 0.25f;

    Delegates.RegionBorderCheck borderCheck;

    public SphereMeshGeneratorSmoothed(PolygonSphere sphere, Delegates.RegionBorderCheck borderCheck) : base(sphere)
    {
        this.borderCheck = borderCheck;
    }

    protected override IEnumerable<Vector3> getPolygonVertices(int polygonIndex)
    {
        foreach(VertexContext context in sphere.GetPolygonVerticesContext(polygonIndex))
        {
            yield return getVectorFromContext(context);
        }
    }

    protected override Vector3[] getEdgeVertices(int edgeIndex)
    {
        VertexContext[] context = sphere.GetEdgeVerticesContext(edgeIndex);
        return new Vector3[] { getVectorFromContext(context[0]), getVectorFromContext(context[1]) };
    }

    protected override Vector3 getNextVertex(int edgeIndex, int polygonIndex, PolygonSphere.Direction direction)
    {
        VertexContext context = sphere.GetNextVertexContext(edgeIndex, polygonIndex, direction);
        return getVectorFromContext(context);
    }

    Vector3 getVectorFromContext(VertexContext context)
    {
        int p1 = context.Polygon1;
        int p2 = context.Polygon2;
        int p3 = context.Polygon3;
        
        if (borderCheck(p1, p2) && borderCheck(p1, p3) && !borderCheck(p2, p3))
        {
            return Vector3.Lerp(context.Vertex, sphere.GetCenter(p1), smoothingRatio);
        }
        else if (!borderCheck(p1, p2) && borderCheck(p1, p3) && borderCheck(p2, p3))
        {
            return Vector3.Lerp(context.Vertex, sphere.GetCenter(p3), smoothingRatio);
        }
        else if (borderCheck(p1, p2) && !borderCheck(p1, p3) && borderCheck(p2, p3))
        {
            return Vector3.Lerp(context.Vertex, sphere.GetCenter(p2), smoothingRatio);
        }
        else
        {
            return context.Vertex;
        }
    }
}
